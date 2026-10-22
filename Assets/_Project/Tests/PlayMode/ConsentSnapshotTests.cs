#nullable enable
using System;
using System.Collections;
using System.IO;
using AF.Economy;
using AF.IAP;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de referencia de los textos de B12 (Q-B12-16 (a); D-033, añadido de 2026-10-21; <c>.claude/rules/localizacion.md</c>:
    /// cada cadena nueva con su captura), con la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp) al 130 %, en es-419
    /// (<c>Logs/snapshots/es-419/</c>) y en en-XA (<c>Logs/snapshots/en-XA/</c>): <c>b12_01_age</c> (la edad neutral, ninguna banda
    /// marcada), <c>b12_02_choices</c> (las finalidades con 18+ en Colombia), <c>b12_03_blocked</c> (el bloqueo de &lt;13),
    /// <c>b12_04_confirm</c> (la doble confirmación de 13-17 con el precio de la tienda), <c>b12_05_settings_privacy</c> (Ajustes con
    /// «Privacidad y datos» y «Corregir mi edad», desplazado hasta la sección) y <c>b12_06_settings_130_16x9</c> (lo mismo en un teléfono
    /// 16:9). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.ConsentSnapshotTests</c> sin
    /// <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class ConsentSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
            GameBootstrap.ResetConsentOverrides();
        }

        [UnityTest]
        public IEnumerator CaptureConsent_Es419()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir);
        }

        [UnityTest]
        public IEnumerator CaptureConsent_EnXA()
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return Capture(dir);
        }

        private static Profile ProfileWith(ConsentRecord? consent)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 4;
            profile.Progress.MaxLevel = 3;
            profile.Consent = consent;
            return profile;
        }

        private static void Prime()
        {
            LaunchOptions.ClockOverride = new ManualClock(T0, TimeSpan.FromHours(-5));
            LaunchOptions.FontScaleOverride = 1.3f;
        }

        private static IEnumerator Capture(string dir)
        {
            // La edad y las finalidades, la primera vez.
            Prime();
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            ConsentScreen view = boot.ConsentView!;
            SnapshotKit.AttachOverlay(view.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b12_01_age");

            view.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(view.View, Is.EqualTo(ConsentView.Choices));
            SnapshotKit.Save(dir, "b12_02_choices");

            // El bloqueo de <13 (en otra escena, como en el teléfono).
            Prime();
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            boot = GameBootstrap.Current!;
            boot.ConsentView!.AgeButton(AgeBand.Under13).onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => GameBootstrap.Current != null && GameBootstrap.Current.ConsentBlocked, "el bloqueo de <13");
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachOverlay(boot.ConsentView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(boot.ConsentView.View, Is.EqualTo(ConsentView.Blocked));
            SnapshotKit.Save(dir, "b12_03_blocked");

            // La doble confirmación de 13-17, encima del inicio.
            Prime();
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadAppWithConsent(
                new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, false, AgeBand.Teen))));
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            boot.Store!.Purchase(new PurchaseRequest("af_coins_2"), _ => { });
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Confirm));
            SnapshotKit.AttachOverlay(boot.ConsentView.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b12_04_confirm");
            boot.ConsentView.ConfirmCancelButton.onClick.Invoke();

            // Ajustes con la sección de privacidad, al 130 %.
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.PrivacyShown && settings.AgeShown, Is.True);
            SnapshotKit.AttachOverlay(settings.Canvas);
            settings.FontScale = 1.3f;
            settings.ScrollToPrivacy();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b12_05_settings_privacy");
            settings.FontScale = 1f;

            // 16:9 al 130 % (nota de B12, riesgos: «probar la fila de Privacidad al 130 % y en 16:9»).
            Prime();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return PlayModeHelpers.LoadAppWithConsent(
                new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult))));
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            settings = boot.SettingsView!;
            SnapshotKit.AttachOverlay(settings.Canvas);
            settings.FontScale = 1.3f;
            settings.ScrollToPrivacy();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, "b12_06_settings_130_16x9", (int)HudLabelTests.Screen640.y);
            Assert.That(File.Exists(Path.Combine(dir, "b12_06_settings_130_16x9.png")), Is.True);
            settings.FontScale = 1f;
        }
    }
}
