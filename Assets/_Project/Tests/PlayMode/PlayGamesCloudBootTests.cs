#nullable enable
using System;
using System.Collections;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
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
            GameBootstrap.ResetMarketOverrides();
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

        private static Profile Read(byte[] blob) => ProfileJson.Deserialize(Encoding.UTF8.GetString(blob));

        // Hoja de compra que no responde hasta que el test lo dice (la compra queda en vuelo).
        private sealed class HeldPurchasePresenter : ISimulatedPurchasePresenter
        {
            private Action<bool>? _onClosed;

            public void Present(SimulatedPurchase purchase, Action<bool> onClosed)
            {
                _onClosed = onClosed;
            }

            public void Close(bool confirmed)
            {
                Action<bool>? close = _onClosed;
                _onClosed = null;
                close?.Invoke(confirmed);
            }
        }

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
        public IEnumerator ALateCloud_WithAPurchaseInFlight_KeepsThePurchase_AndNeverUploadsTheUnmergedLocal()
        {
            // Revisión de D-042: la nube (con el mismo nivel y una economía más nueva) responde con una compra en vuelo. La fusión se
            // rehace con el perfil vivo al aplicarla: la compra (más nueva aún) no se pierde, y mientras no se sube el local sin fusionar.
            var held = new HeldPurchasePresenter();
            LaunchOptions.PurchasePresenterOverride = held;
            // economy_ts sale del reloj del teléfono (el del juego, aquí el TestClock), y el arranque ya lo marca: la nube va unos
            // segundos por delante de ese arranque (gana al llegar) y la compra se acredita después (gana al aplicar la fusión).
            TestClock clock = TestClock.Install();
            Profile remote = ProfileAt(26, 50);
            DateTime cloudTs = clock.UtcNow.AddSeconds(8);
            remote.EconomyTs = cloudTs;
            Profile local = ProfileAt(26, 0);
            var cloud = new FakeCloudSave(Blob(remote)) { Deferred = true };
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.CloudBootWaitMsOverride = 0f;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(local));
            GameBootstrap boot = GameBootstrap.Current!;

            PurchaseResult? bought = null;
            boot.Store!.Purchase(new PurchaseRequest("af_coins_2"), r => bought = r);
            Assert.That(boot.Store.IsPurchasing, Is.True, "la hoja de la compra, abierta");
            cloud.Flush(); // gana la nube por economy_ts (la compra aún no se acreditó)
            Assert.That(boot.CloudReplacePending, Is.True);
            int saves = cloud.SaveCount;
            yield return null;
            yield return null;
            Assert.That(GameBootstrap.Current, Is.SameAs(boot), "con una compra en vuelo no se recarga");

            clock.Advance(cloudTs.AddSeconds(1) - clock.UtcNow); // la compra, ya más nueva que la nube (sin esperar 9 s)
            yield return null;
            yield return null;

            Assert.That(GameBootstrap.Current, Is.SameAs(boot), "con la compra en vuelo sigue sin recargar");
            held.Close(true);
            Assert.That(bought!.IsPurchased, Is.True);
            Assert.That(boot.Profile!.Wallet.Seeds, Is.EqualTo(1000));
            var applied = new System.Collections.Generic.List<string>(boot.Profile.AppliedPurchases);
            Assert.That(cloud.SaveCount, Is.EqualTo(saves), "la subida tras la compra no sale: sería el local sin fusionar");
            Assert.That(boot.UploadCloud(), Is.False);

            yield return PlayModeHelpers.WaitFor(() => !boot.CloudReplacePending || GameBootstrap.Current != boot, "la fusión tardía, aplicada");
            yield return null;
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "la sesión tras la fusión");
            cloud.Flush();
            Profile final = GameBootstrap.Current!.Profile!;
            Assert.That(final.Wallet.Seeds, Is.EqualTo(1000), "la compra no se pierde (gana el perfil vivo, más nuevo)");
            Assert.That(final.AppliedPurchases, Is.SupersetOf(applied), "ni su applied_purchases");
            Assert.That(Read(cloud.Blob!).Wallet.Seeds, Is.EqualTo(1000), "la nube se pone al día con lo fusionado");
        }

        [UnityTest]
        public IEnumerator ALateCloud_WithAWinWhileWaiting_KeepsTheWin_AndNeverUploadsTheUnmergedLocal()
        {
            Profile remote = ProfileAt(26, 50);
            // El arranque marca economy_ts con el reloj del juego (el TestClock): la nube va un día por delante (con T0 fijo y el reloj
            // del sistema dejó de ganar el 24-10).
            remote.EconomyTs = TestClock.Install().UtcNow.AddDays(1);
            var cloud = new FakeCloudSave(Blob(remote)) { Deferred = true };
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.CloudBootWaitMsOverride = 0f;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26, 0)));
            GameBootstrap boot = GameBootstrap.Current!;

            cloud.Flush(); // gana la nube por economy_ts
            Assert.That(boot.CloudReplacePending, Is.True);
            int saves = cloud.SaveCount;

            // Una victoria antes de que se aplique (en el mismo fotograma): el nivel 26 superado, y tres victorias al bus.
            boot.Profile!.Progress.MaxLevel = 26;
            boot.Profile.Progress.NextLevel = 27;
            for (int i = 0; i < 3; i++)
            {
                boot.Events!.Publish(new AF.Events.LevelWon(26, "w03_l26", 10, 10, 0, "{}", 1000, 0, false, false));
            }

            Assert.That(cloud.SaveCount, Is.EqualTo(saves), "las victorias no suben el local sin fusionar");
            boot.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(cloud.SaveCount, Is.EqualTo(saves), "ni el paso a segundo plano");
            boot.gameObject.SendMessage("OnApplicationPause", false);

            yield return PlayModeHelpers.WaitFor(() => !boot.CloudReplacePending || GameBootstrap.Current != boot, "la fusión tardía, aplicada");
            yield return null;
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "la sesión tras la fusión");
            cloud.Flush();
            Assert.That(GameBootstrap.Current!.Profile!.Progress.MaxLevel, Is.EqualTo(26), "la victoria no se pierde (gana el mayor max_level)");
            Assert.That(Read(cloud.Blob!).Progress.MaxLevel, Is.EqualTo(26), "y la nube se pone al día");
        }

        [UnityTest]
        public IEnumerator Under13InSettings_WithTheCloudStarting_NeitherSavesNorReloads()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            var cloud = new FakeCloudSave(Blob(ProfileAt(80, 3))) { Deferred = true };
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.CloudBootWaitMsOverride = 0f;
            var store = new MemoryProfileStore(ProfileAt(10, 300, new ConsentRecord(LegalProfiles.Co1581, 1, T0, false, false, AgeBand.Adult)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Starting));

            Assert.That(first.OpenAgeCorrection(), Is.True);
            first.ConsentView!.AgeButton(AgeBand.Under13).onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.ConsentBlocked,
                "el arranque bloqueado tras corregir a <13");
            GameBootstrap blocked = GameBootstrap.Current!;

            cloud.Flush(); // la nube responde ahora (con más niveles: querría recargar y subir)
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.That(cloud.SaveCount, Is.EqualTo(0), "D-033: con <13 no se escribe en la nube");
            Assert.That(GameBootstrap.Current, Is.SameAs(blocked), "ni se recarga con el perfil de la nube");
            Assert.That(blocked.CloudReplacePending, Is.False);
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.MaxLevel, Is.EqualTo(9), "el perfil del teléfono no cambia");
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
