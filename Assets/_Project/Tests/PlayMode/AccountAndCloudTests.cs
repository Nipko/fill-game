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
    /// Bloque B8 en la escena <c>Game</c> real (parche <c>B8-ui.diff</c>): «Eliminar cuenta y datos» en Ajustes con confirmación en
    /// dos pasos y aviso de que las compras no se reembolsan, que borra la nube, el perfil y <c>PlayerPrefs</c> y vuelve a la
    /// primera ejecución (08 §11); el Atrás de Android y un borrado fallido no borran nada; y al arrancar, la fusión con la nube
    /// (08 §11) y la firma de cada escritura (08 §4.3), con el reloj de los topes (<see cref="GuardedClock"/>, 08 §10.1).
    /// </summary>
    public sealed class AccountAndCloudTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            GameBootstrap.ResetCloudOverrides();
            GameBootstrap.AnalyticsResetOverride = null;
        }

        private static Profile ProfileAt(int nextLevel, int seeds)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            return profile;
        }

        [UnityTest]
        public IEnumerator DeleteAccount_InTwoSteps_WipesCloudProfileAndPrefs_AndRestartsAsFirstRun()
        {
            var cloud = new FakeCloudSave();
            int wipes = 0;
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.WipePlayerPrefsForTests = () => wipes++;
            int analyticsResets = 0;
            GameBootstrap.AnalyticsResetOverride = () => analyticsResets++;
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(cloud.Blob, Is.Not.Null, "al arrancar con sesión y sin snapshot, se sube el local (08 §11)");
            Assert.That(boot.BootCloudSync!.Result, Is.EqualTo(CloudSyncResult.Ok));

            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.DeleteAccountButton, Is.Not.Null, "la fila, al final de Ajustes");
            Assert.That(settings.DeleteAccountLabel, Is.EqualTo(AccountText.DeleteAccount));

            // Paso 1: lo que se borra y que las compras no se reembolsan; «Conservar mis datos» cierra sin borrar.
            settings.DeleteAccountButton!.onClick.Invoke();
            DeleteAccountDialog dialog = boot.DeleteAccountView!;
            Assert.That(dialog.Visible && dialog.Step == DeleteAccountStep.Warn, Is.True);
            Assert.That(dialog.TitleText, Is.EqualTo(AccountText.DeleteAccount));
            Assert.That(dialog.WarningText, Is.EqualTo(AccountText.NoRefund), "08 §11: aviso de que las compras no se reembolsan");
            Assert.That(dialog.KeepButton.gameObject.activeSelf, Is.True);
            dialog.KeepButton.onClick.Invoke();
            Assert.That(dialog.Visible, Is.False);
            Assert.That(settings.Visible, Is.True, "Ajustes sigue como estaba");
            Assert.That(ProfileJson.Deserialize(store.Json!).Wallet.Seeds, Is.EqualTo(900));

            // Paso 1 → paso 2 → «Eliminar».
            settings.DeleteAccountButton.onClick.Invoke();
            dialog.AdvanceButton.onClick.Invoke();
            Assert.That(dialog.Step, Is.EqualTo(DeleteAccountStep.Confirm));
            Assert.That(dialog.TitleText, Is.EqualTo(AccountText.ConfirmTitle));
            Assert.That(dialog.AdvanceText, Is.EqualTo(AccountText.ConfirmCta));
            Assert.That(dialog.KeepButton.gameObject.activeSelf, Is.False, "en el paso 2 conserva la X");
            dialog.AdvanceButton.onClick.Invoke();
            Assert.That(cloud.DeleteCount, Is.EqualTo(1), "el snapshot af_main");
            Assert.That(cloud.Blob, Is.Null);
            Assert.That(store.Json, Is.Null, "profile.json y profile.bak (IProfileStore.Delete)");
            Assert.That(wipes, Is.EqualTo(1), "PlayerPrefs");
            Assert.That(analyticsResets, Is.EqualTo(1), "F3-6 (08 §13.3): FirebaseAnalyticsSink.ResetData, con un ID de instancia nuevo");

            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras eliminar la cuenta");
            boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "primera ejecución: directo al nivel 1 (08 §2.3)");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(1));
            Assert.That(boot.Profile!.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(boot.Profile.Progress.MaxLevel, Is.EqualTo(0));
            Assert.That(boot.Profile.FirstAppVersion, Is.Not.Null, "first_app_version de la nueva primera ejecución");
        }

        [UnityTest]
        public IEnumerator Back_ClosesTheDialog_AndAFailedDelete_KeepsEverything()
        {
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.OpenDeleteAccount(), Is.True);
            Assert.That(boot.OpenDeleteAccount(), Is.False, "ya abierto");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.DeleteAccountOpen, Is.False, "Atrás cierra el diálogo sin borrar");
            Assert.That(boot.SettingsView!.Visible, Is.True, "y deja Ajustes abierta");

            GameBootstrap.FailAccountDeleteForTests = true;
            boot.OpenDeleteAccount();
            Assert.That(boot.ConfirmDeleteAccount(), Is.False, "el paso 2 aún no está abierto");
            Assert.That(boot.ContinueDeleteAccount(), Is.True);
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            DeleteAccountDialog dialog = boot.DeleteAccountView!;
            Assert.That(dialog.Visible && dialog.Step == DeleteAccountStep.Confirm, Is.True, "sigue en el paso 2");
            Assert.That(dialog.WarningText, Is.EqualTo(AccountText.Failed));
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.MaxLevel, Is.EqualTo(39), "no se borró nada");
            Assert.That(GameBootstrap.Current, Is.SameAs(boot), "sin recarga");
            Assert.That(boot.CancelDeleteAccount(), Is.True);
        }

        [UnityTest]
        public IEnumerator Boot_ACloudWithMoreLevels_Wins_AndEverySaveIsSigned()
        {
            Profile remote = ProfileAt(121, 77);
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(ProfileJson.Serialize(remote)));
            GameBootstrap.CloudSaveOverride = cloud;
            GameBootstrap.DeviceIdentityOverride = new ManualDeviceIdentity("b8-test-phone");
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.BootCloudSync!.Result, Is.EqualTo(CloudSyncResult.ConflictCloudWon));
            Assert.That(boot.BootCloudSync.Reason, Is.EqualTo(CloudMergeReason.MaxLevel));
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(120), "08 §11: gana el mayor max_level");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(77), "con su cartera: los consumibles nunca se suman");
            Profile saved = ProfileJson.Deserialize(store.Json!);
            Assert.That(saved.Progress.MaxLevel, Is.EqualTo(120), "el fusionado se guarda en el dispositivo");
            Assert.That(ProfileIntegrity.Verify(saved), Is.EqualTo(IntegrityStatus.Valid), "y firmado (08 §4.3)");
            Assert.That(saved.Integrity.DeviceFirst, Is.EqualTo(ProfileHmac.DeviceFingerprint("b8-test-phone")));
            Assert.That(boot.CapsClock, Is.InstanceOf<GuardedClock>(), "anti-reloj de canoas y topes (08 §10.1)");
            Assert.That(((IClockTrust)boot.CapsClock!).DriftExceeded, Is.False, "sin backend, sin red");
        }
    }
}
