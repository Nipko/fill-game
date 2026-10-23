#nullable enable
using System;
using System.Collections;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La nube de Play Games en la escena <c>Game</c> real (D-042; 08 §2.3 y §11; <c>docs/dev/play-games.md</c>), con la nube falsa en
    /// modo diferido (responde cuando el test lo dice, como Play): el arranque que espera a la nube, la fusión que llega tarde y
    /// recarga la escena con el perfil de la nube, sin red al arrancar (nada se sube hasta fusionar; se reintenta al volver),
    /// la edad (&lt;13 no toca la nube; 18+ la arranca tras la pantalla de edad) y el borrado de la cuenta (marca pendiente y sin
    /// escrituras).
    /// </summary>
    public sealed class PlayGamesCloudBootTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameBootstrap.ResetConsentOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetCloudOverrides();
        }

        private static Profile ProfileAt(int nextLevel, int seeds, ConsentRecord? consent = null)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            profile.Consent = consent;
            return profile;
        }

        private static byte[] Blob(Profile p) => Encoding.UTF8.GetBytes(ProfileJson.Serialize(p));

        [UnityTest]
        public IEnumerator Boot_WaitsForPlayGames_AndStartsWithTheCloudProfile()
        {
            var cloud = new FakeCloudSave(Blob(ProfileAt(121, 77))) { Deferred = true };
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.CloudBootWaitMsOverride = 60000f;
            var store = new MemoryProfileStore(ProfileAt(40, 900));

            // La nube responde mientras el arranque espera con la pantalla de arranque (como Play Games, unos fotogramas después).
            int frames = 0;
            bool waitedWithoutSession = false;
            yield return Drive(PlayModeHelpers.LoadApp(store), () =>
            {
                if (cloud.PendingCalls > 0 && ++frames > 3)
                {
                    waitedWithoutSession = GameBootstrap.Current != null && GameBootstrap.Current.App == null;
                    cloud.Flush();
                }
            });
            Assert.That(waitedWithoutSession, Is.True, "el arranque espera a la nube antes de crear la sesión de juego");

            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.BootCloudSync!.Result, Is.EqualTo(CloudSyncResult.ConflictCloudWon));
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(120), "08 §11: gana el mayor max_level");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(77));
            Assert.That(boot.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Synced));
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.MaxLevel, Is.EqualTo(120), "el fusionado, en el dispositivo");
        }

        [UnityTest]
        public IEnumerator ALateCloud_ReloadsTheSceneWithTheCloudProfile()
        {
            var cloud = new FakeCloudSave(Blob(ProfileAt(121, 77))) { Deferred = true };
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.CloudBootWaitMsOverride = 0f;
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.Profile!.Progress.MaxLevel, Is.EqualTo(39), "sin esperar: se juega con el local");
            Assert.That(first.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Starting));
            Assert.That(first.UploadCloud(), Is.False, "nada se sube antes de fusionar");

            cloud.Flush();
            Assert.That(first.CloudReplacePending, Is.True);
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "la recarga con el perfil de la nube");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(120));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(77), "consumibles del ganador, nunca sumados");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "vuelve al inicio, como al cambiar de idioma");
            Assert.That(ProfileIntegrity.Verify(ProfileJson.Deserialize(store.Json!)), Is.EqualTo(IntegrityStatus.Valid), "guardado y firmado");
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.MaxLevel, Is.EqualTo(120));
            Assert.That(boot.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Synced), "la sesión de nube sigue tras la recarga");
            Assert.That(boot.UploadCloud(), Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutNetworkAtBoot_NothingIsUploaded_UntilTheMergeIsRetried()
        {
            var cloud = new FakeCloudSave(Blob(ProfileAt(60, 5))) { FailNext = true };
            GameBootstrap.CloudSaveOverride = cloud;
            var store = new MemoryProfileStore(ProfileAt(10, 300));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.BootCloudSync!.Result, Is.EqualTo(CloudSyncResult.Failed));
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(9), "se sigue con el local");
            Assert.That(boot.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Unsynced));

            boot.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(cloud.SaveCount, Is.EqualTo(0), "al pasar a segundo plano no se pisa una nube con 60 niveles");

            boot.gameObject.SendMessage("OnApplicationPause", false); // vuelve con red: se reintenta la fusión
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "la recarga con el perfil de la nube");
            Assert.That(GameBootstrap.Current!.Profile!.Progress.MaxLevel, Is.EqualTo(59), "la de la nube (max_level 59)");
        }

        [UnityTest]
        public IEnumerator FirstRun_Under13_NeverTouchesTheCloud()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            var cloud = new FakeCloudSave(Blob(ProfileAt(30, 3)));
            GameBootstrap.CloudSaveOverride = cloud;
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(cloud.LoadCount, Is.EqualTo(0), "08 §2.3: sin edad declarada, sin Play Games");

            first.ConsentView!.AgeButton(AgeBand.Under13).onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.ConsentBlocked,
                "el arranque bloqueado tras elegir <13");
            Assert.That(cloud.LoadCount + cloud.SaveCount + cloud.DeleteCount, Is.EqualTo(0), "D-033: con <13 no arranca");
        }

        [UnityTest]
        public IEnumerator FirstRun_Adult_StartsTheCloudAfterTheAgeScreen_AndRestoresTheCloudProfile()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            var cloud = new FakeCloudSave(Blob(ProfileAt(30, 3)));
            GameBootstrap.CloudSaveOverride = cloud;
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(cloud.LoadCount, Is.EqualTo(0));

            first.ConsentView!.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return null;
            first.ConsentView.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => cloud.LoadCount > 0, "con 18+ declarada, la nube arranca (reinstalación: se recupera la partida)");
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "la recarga con el perfil de la nube");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(29));
            Assert.That(boot.Profile.Consent!.AgeBand, Is.EqualTo(AgeBand.Adult), "08 §11: la edad y el consentimiento, del dispositivo");
        }

        [UnityTest]
        public IEnumerator DeleteAccount_MarksThePendingCloudDelete_AndNothingIsUploadedAfterwards()
        {
            var cloud = new FakeCloudSave();
            var flags = new MemoryCloudSaveFlags();
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.CloudFlagsOverride = flags;
            GameBootstrap.WipePlayerPrefsForTests = () => { };
            GameBootstrap.AnalyticsResetOverride = () => { };
            DeleteAccountSnapshotHold();
            var store = new MemoryProfileStore(ProfileAt(25, 50));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(cloud.Blob, Is.Not.Null);
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.OpenDeleteAccount(), Is.True);
            Assert.That(boot.ContinueDeleteAccount(), Is.True);
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(cloud.Blob, Is.Null, "af_main borrado (08 §11)");
            Assert.That(flags.DeletePending, Is.True, "se comprueba en el arranque siguiente (por si Play no lo borró)");
            Assert.That(boot.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Closed));
            int saves = cloud.SaveCount;
            Assert.That(boot.UploadCloud(), Is.False);
            boot.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(cloud.SaveCount, Is.EqualTo(saves), "tras el borrado no se escribe (el lote está cerrado)");

            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras eliminar la cuenta");
            Assert.That(flags.DeletePending, Is.False, "el arranque siguiente comprueba que af_main quedó vacío");
            Assert.That(GameBootstrap.Current!.Profile!.Progress.MaxLevel, Is.EqualTo(0), "la cuenta borrada no vuelve de la nube");
        }

        // Recorre un coroutine con sus anidados aplanados y llama a eachFrame antes de cada fotograma (para responder por la nube
        // mientras el arranque espera).
        private static IEnumerator Drive(IEnumerator root, Action eachFrame)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }

                eachFrame();
                yield return top.Current;
            }
        }

        private static void DeleteAccountSnapshotHold()
        {
            GameBootstrap.DeleteDoneHoldMsOverride = 0f;
        }
    }
}
