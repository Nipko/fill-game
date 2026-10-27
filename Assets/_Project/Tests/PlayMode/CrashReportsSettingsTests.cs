#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AF.Economy;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ajustes › «Enviar informes de fallos» en la escena <c>Game</c> real (T-L6 de <c>docs/legal/investigacion-legal-mundial.md</c>
    /// §11; D-057, añadido del 2026-10-27, que deja sin efecto la «línea sin interruptor» de Q-B12-4): activado por defecto, apagarlo
    /// apaga Crashlytics al momento y se guarda, el arranque siguiente lo respeta antes de encenderlo, con &lt;13 sigue sin encenderse,
    /// y la opción se lee con TalkBack y cabe al 130 %.
    /// </summary>
    public sealed class CrashReportsSettingsTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            LaunchOptions.ClockOverride = new ManualClock(T0, TimeSpan.FromHours(-5));
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameBootstrap.ResetConsentOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetCloudOverrides();
        }

        private static Profile Adult(bool crashReports)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 4;
            profile.Progress.MaxLevel = 3;
            profile.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult);
            profile.Settings.CrashReports = crashReports;
            return profile;
        }

        [UnityTest]
        public IEnumerator OnByDefault_TurningItOff_StopsCrashlytics_AndTheNextBootRespectsIt()
        {
            var crash = new List<bool>();
            GameBootstrap.CrashReportingOverride = on => crash.Add(on);
            var store = new MemoryProfileStore(Adult(true));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(crash, Is.EqualTo(new[] { true }), "activado por defecto: Crashlytics en el arranque (08 §2.3)");

            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.CrashReportsShown, Is.True, "en la sección de privacidad, con «Privacidad y datos»");
            Assert.That(settings.CrashReportsMarked, Is.True);
            Assert.That(settings.CrashReportsLabel, Is.EqualTo(ConsentText.T(ConsentText.Keys.SettingsCrashReports)));

            settings.CrashReportsButton!.onClick.Invoke();
            Assert.That(crash, Is.EqualTo(new[] { true, false }), "apagado al momento (CrashReporting.Stop: lo no enviado, borrado)");
            Assert.That(settings.CrashReportsMarked, Is.False);
            Assert.That(store.Load().Profile!.Settings.CrashReports, Is.False, "guardado con el resto de ajustes");

            crash.Clear();
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            Assert.That(crash, Is.EqualTo(new[] { false }), "el arranque lo apaga antes de encenderlo: nunca un true");

            boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            settings = boot.SettingsView!;
            Assert.That(settings.CrashReportsMarked, Is.False);
            settings.CrashReportsButton!.onClick.Invoke();
            Assert.That(crash, Is.EqualTo(new[] { false, true }), "encenderlo lo enciende al momento");
            Assert.That(store.Load().Profile!.Settings.CrashReports, Is.True);
        }

        [UnityTest]
        public IEnumerator Under13_StillNeverStartsCrashlytics_EvenWithTheSettingOn()
        {
            var crash = new List<bool>();
            GameBootstrap.CrashReportingOverride = on => crash.Add(on);
            Profile profile = Adult(true);
            profile.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, T0, false, false, AgeBand.Under13);
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentBlocked, Is.True);
            Assert.That(crash, Is.EqualTo(new[] { false }), "con <13 sin SDK: solo el apagado explícito");
            Assert.That(boot.ChooseCrashReports(true), Is.True);
            Assert.That(crash, Has.No.Member(true), "ni a mano se enciende con <13");
        }

        [UnityTest]
        public IEnumerator TurningItBackOn_UploadsToTheCloud_SoAnOldSnapshotDoesNotTurnItOffAgain()
        {
            // La fusión deja apagado lo que cualquiera de los dos apagó (CloudMerge): al cambiarlo, el perfil sube en el acto (D-042).
            var crash = new List<bool>();
            GameBootstrap.CrashReportingOverride = on => crash.Add(on);
            Profile local = Adult(false);
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(ProfileJson.Serialize(Adult(false))));
            GameBootstrap.CloudSaveOverride = cloud;
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore(local)); // con la edad ya declarada (18+), la nube arranca
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.CloudSessionNow!.State, Is.EqualTo(CloudSessionState.Synced));
            int saves = cloud.SaveCount;

            Assert.That(boot.ChooseCrashReports(true), Is.True);
            Assert.That(cloud.SaveCount, Is.EqualTo(saves + 1), "sube al cambiarlo");
            Profile uploaded = ProfileJson.Deserialize(Encoding.UTF8.GetString(cloud.Blob!));
            Assert.That(uploaded.Settings.CrashReports, Is.True, "la nube ya lo tiene encendido");
            Assert.That(CloudMerge.Resolve(boot.Profile!, uploaded).Merged.Settings.CrashReports, Is.True,
                "una fusión posterior no lo vuelve a apagar");

            Assert.That(boot.ChooseCrashReports(true), Is.True);
            Assert.That(cloud.SaveCount, Is.EqualTo(saves + 1), "sin cambio, sin subida");
        }

        [UnityTest]
        public IEnumerator TheRow_ReadsWithTalkBack_AndFitsAt130()
        {
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore(Adult(true)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            settings.FontScale = 1.3f;
            yield return null;
            SettingsScreenTests.AssertFits(settings, "130 %");
            Assert.That(settings.CrashReportsButton!.GetComponent<UnityEngine.RectTransform>().rect.height, Is.GreaterThanOrEqualTo(SettingsScreen.MinRowDp),
                "área táctil ≥ 48 dp (07 §7.8)");

            Assert.That(A11yLabels.TryGet(settings.CrashReportsButton.gameObject, out string label, out bool selected), Is.True,
                "TalkBack: la opción tiene etiqueta (02 §12)");
            Assert.That(label, Is.EqualTo(settings.CrashReportsLabel));
            Assert.That(selected, Is.True, "y dice que está marcada");
            settings.CrashReportsButton.onClick.Invoke();
            Assert.That(A11yLabels.SelectedOf(settings.CrashReportsButton.gameObject), Is.False);
        }
    }
}
