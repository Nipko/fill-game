#nullable enable
using System;
using System.Collections;
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
    /// La edad y el consentimiento en la escena <c>Game</c> real (bloque B12, parche <c>B12-ui.diff</c>; 08 §2.3 y §7; 05 §7; 06 §2.1,
    /// regla 3; Q-T3 (a) y Q-T4 (a)): la primera vez, la edad neutral y el consentimiento antes del nivel 1, con los flags a la
    /// analítica, los anuncios y la tienda antes de <c>AppFlow.Start</c>; &lt;13 bloquea y sigue bloqueado en el arranque siguiente;
    /// una decisión vigente no enseña nada; con 13-17, la doble confirmación antes de la hoja de compra; y en Ajustes, cambiar las
    /// finalidades y corregir la edad una vez.
    /// </summary>
    public sealed class ConsentBootTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);

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
        }

        private static Profile ProfileWith(ConsentRecord? consent, int nextLevel = 4)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Consent = consent;
            return profile;
        }

        [UnityTest]
        public IEnumerator FirstRun_AgeThenConsent_BeforeLevel1_WithTheFlagsAppliedFirst()
        {
            var store = new MemoryProfileStore();
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            ConsentScreen view = boot.ConsentView!;
            Assert.That(view.View, Is.EqualTo(ConsentView.Age), "06 §2.1, regla 3: antes del nivel 1");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Boot));
            Assert.That(boot.Analytics!.CollectionEnabled, Is.False, "08 §2.3: apagada hasta el consentimiento");
            Assert.That(boot.ConsentAds!.Initialized, Is.False, "MAX sin inicializar");
            Assert.That(boot.Consent!.Profile, Is.EqualTo(LegalProfiles.Co1581), "Q-T3 (a): el país de la SIM");
            Assert.That(boot.ConsentHoldsSession, Is.True, "sin sesión ni avisos locales hasta decidir");
            Assert.That(boot.PressAndroidBack(), Is.False, "Atrás no salta la pantalla");
            Assert.That(view.Visible, Is.True);

            view.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return null;
            Assert.That(view.View, Is.EqualTo(ConsentView.Choices));
            Assert.That(view.AdsToggleShown, Is.True);
            Assert.That(view.AnalyticsSelected, Is.False);

            view.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.App.Screen != AppScreen.Boot, "la primera pantalla tras el consentimiento");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level), "la primera vez, el nivel 1");
            Assert.That(boot.ConsentHoldsSession, Is.False);
            Assert.That(view.Visible, Is.False);
            Assert.That(boot.Analytics.CollectionEnabled, Is.True);
            Assert.That(boot.ConsentAds.Initialized, Is.True);
            Assert.That(boot.ConsentAds.NonPersonalized, Is.False);
            Assert.That(boot.ConsentAds.MaxAdContentRating, Is.EqualTo("G"));
            Profile saved = store.Load().Profile!;
            Assert.That(saved.Consent!.Profile, Is.EqualTo("co_1581"));
            Assert.That(saved.Consent.AgeBand, Is.EqualTo(AgeBand.Adult));
            Assert.That(saved.Consent.Analytics, Is.True);
            Assert.That(saved.Consent.At, Is.EqualTo(T0), "la prueba lleva la fecha (05 §7)");
        }

        [UnityTest]
        public IEnumerator Under13_Blocks_AndTheNextBootStaysBlocked_WithoutBuildingTheGame()
        {
            var store = new MemoryProfileStore();
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap first = GameBootstrap.Current!;
            first.ConsentView!.AgeButton(AgeBand.Under13).onClick.Invoke();
            yield return null; // el arranque, que espera la decisión, aplica los flags y bloquea
            Assert.That(first.ConsentBlocked, Is.True);
            Assert.That(first.Analytics!.CollectionEnabled, Is.False);
            Assert.That(first.ConsentAds!.ChildDirected, Is.True, "TFCD antes de bloquear (08 §7)");
            Assert.That(first.ConsentStore!.PurchasesAllowed, Is.False);
            Assert.That(store.Load().Profile!.Consent!.AgeBand, Is.EqualTo(AgeBand.Under13));

            // La escena se recarga y el arranque se para en el bloqueo, antes de crear el juego (sin sesión, audio ni avisos).
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.ConsentBlocked,
                "el arranque bloqueado tras elegir <13");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Blocked));
            Assert.That(boot.App, Is.Null, "no se crea sesión de juego");
            Assert.That(boot.Ads, Is.Null);
            Assert.That(boot.Store, Is.Null);
            Assert.That(boot.PressAndroidBack(), Is.False);
            Assert.That(boot.ConsentView.Visible, Is.True);

            yield return PlayModeHelpers.LoadAppWithConsent(store);
            boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentBlocked, Is.True, "en cualquier arranque posterior (08 §7)");
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Blocked));
            Assert.That(boot.App, Is.Null, "el arranque se para antes de crear el juego");
        }

        [UnityTest]
        public IEnumerator Settings_CorrectingTo13_Blocks_WithoutTheSession()
        {
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Teen)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.PressSettings(), Is.True);
            first.SettingsView!.AgeButton!.onClick.Invoke();
            first.ConsentView!.AgeButton(AgeBand.Under13).onClick.Invoke();

            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.ConsentBlocked,
                "el arranque bloqueado tras corregir a <13");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App, Is.Null, "la sesión de juego no sigue debajo");
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Blocked));
            ConsentRecord saved = store.Load().Profile!.Consent!;
            Assert.That(saved.AgeBand, Is.EqualTo(AgeBand.Under13));
            Assert.That(saved.AgeBandCorrected, Is.True);
        }

        [UnityTest]
        public IEnumerator UnknownCountry_IsStrict_AndUmp_LeavesTheAdsToUmp()
        {
            GameBootstrap.SimCountryOverride = ManualSimCountry.None;
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es-419");
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentCountryCode, Is.Null);
            boot.ConsentView!.AgeButton(AgeBand.Adult).onClick.Invoke();
            Assert.That(boot.ConsentView.AdsToggleShown, Is.False, "Q-T3 (a): sin país, lo más estricto");
            Assert.That(boot.ConsentView.AdsLineText, Is.EqualTo(ConsentText.T(ConsentText.Keys.AdsStrict)));
            boot.ConsentView.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.App!.Screen != AppScreen.Boot, "la primera pantalla");
            Assert.That(boot.ConsentAds!.NonPersonalized, Is.True);

            LaunchOptions.Reset();
            GameBootstrap.SimCountryOverride = new ManualSimCountry("DE");
            LaunchOptions.ClockOverride = new ManualClock(T0, TimeSpan.Zero);
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Ump, 1, T0, true, false, AgeBand.Adult)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentAds!.UmpDecides, Is.True);
            Assert.That(boot.ConsentAds.NonPersonalized, Is.False, "en el EEE decide UMP (08 §7 #2)");
            Assert.That(boot.PressSettings(), Is.True);
            boot.SettingsView!.PrivacyButton!.onClick.Invoke();
            Assert.That(boot.ConsentView!.AdsOptionsShown, Is.True, "revocación en Ajustes (05 §7)");
            boot.ConsentView.AdsOptionsButton.onClick.Invoke();
            Assert.That(boot.ConsentAds.PrivacyOptionsRequests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ToDecideAgain_HoldsTheSession_AndBackgroundSchedulesNothing()
        {
            // Sugerencia 1 de la revisión de B12 (nota §6), aplicada al integrar: con una decisión que ya no vale (textos de una
            // versión anterior), ni sesión ni avisos locales mientras se vuelve a decidir, aunque la app pase a segundo plano.
            var scheduler = new FakeNotificationScheduler(new ManualClock(T0), PushPermission.Allowed);
            GameBootstrap.NotificationSchedulerOverride = scheduler;
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 0, T0, true, true, AgeBand.Adult)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentView!.Visible, Is.True, "textos nuevos: se vuelve a preguntar (Q-B12-2: solo las finalidades)");
            Assert.That(boot.ConsentView.View, Is.EqualTo(ConsentView.Choices));
            Assert.That(boot.ConsentHoldsSession, Is.True);
            Assert.That(boot.Sessions, Is.Not.Null);

            boot.SessionsPaused(true);
            Assert.That(boot.Sessions!.InBackground, Is.False, "la sesión no cuenta");
            Assert.That(scheduler.Scheduled, Is.Empty, "sin avisos locales hasta decidir (08 §7)");

            boot.ConsentView.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.App!.Screen != AppScreen.Boot, "la primera pantalla tras volver a decidir");
            Assert.That(boot.ConsentHoldsSession, Is.False);
            boot.SessionsPaused(true);
            Assert.That(boot.Sessions.InBackground, Is.True, "decidido: la sesión vuelve a contar");
        }

        [UnityTest]
        public IEnumerator AValidDecision_ShowsNothing_AndAppliesItsFlags()
        {
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, false, true, AgeBand.Adult)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.ConsentView!.Visible, Is.False);
            Assert.That(boot.Analytics!.CollectionEnabled, Is.False, "rechazó la analítica");
            Assert.That(boot.ConsentAds!.Initialized, Is.True);
            Assert.That(boot.ConsentAds.NonPersonalized, Is.False);
        }

        [UnityTest]
        public IEnumerator Teen_ASecondConfirmation_BeforeTheStoreSheet()
        {
            var presenter = new ImmediatePurchasePresenter();
            LaunchOptions.PurchasePresenterOverride = presenter;
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, false, AgeBand.Teen)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentAds!.NonPersonalized, Is.True, "13-17: npa");

            PurchaseResult? result = null;
            boot.Store!.Purchase(new PurchaseRequest("af_coins_2"), r => result = r);
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Confirm), "Q-T4 (a): la segunda confirmación");
            Assert.That(presenter.Presented, Is.EqualTo(0), "la hoja de compra, después");
            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás la cancela");
            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(presenter.Presented, Is.EqualTo(0));

            result = null;
            boot.Store.Purchase(new PurchaseRequest("af_coins_2"), r => result = r);
            boot.ConsentView.ConfirmBuyButton.onClick.Invoke();
            Assert.That(presenter.Presented, Is.EqualTo(1));
            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Purchased));
        }

        [UnityTest]
        public IEnumerator Settings_ChangeTheChoices_AndCorrectTheAgeOnce()
        {
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Analytics!.CollectionEnabled, Is.True);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.PrivacyShown, Is.True);
            Assert.That(settings.AgeShown, Is.True);

            // «Privacidad y datos»: lo guardado marcado; apagar la analítica la apaga al momento (05 §7: revocación en Ajustes).
            settings.PrivacyButton!.onClick.Invoke();
            ConsentScreen view = boot.ConsentView!;
            Assert.That(view.View, Is.EqualTo(ConsentView.Choices));
            Assert.That(view.Closable, Is.True);
            Assert.That(view.AnalyticsSelected, Is.True);
            Assert.That(view.AdsSelected, Is.True);
            view.AnalyticsToggleButton.onClick.Invoke();
            view.SaveButton.onClick.Invoke();
            Assert.That(view.Visible, Is.False);
            Assert.That(settings.Visible, Is.True, "se vuelve a Ajustes");
            Assert.That(boot.Analytics.CollectionEnabled, Is.False);
            Assert.That(store.Load().Profile!.Consent!.Analytics, Is.False);

            // «Corregir mi edad» a 13-17: npa, la marca de corregida y el enlace desaparece.
            settings.AgeButton!.onClick.Invoke();
            Assert.That(view.View, Is.EqualTo(ConsentView.Age));
            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás la cierra sin cambiar nada");
            Assert.That(view.Visible, Is.False);
            Assert.That(settings.AgeShown, Is.True);

            settings.AgeButton.onClick.Invoke();
            view.AgeButton(AgeBand.Teen).onClick.Invoke();
            Assert.That(view.Visible, Is.False);
            ConsentRecord saved = store.Load().Profile!.Consent!;
            Assert.That(saved.AgeBand, Is.EqualTo(AgeBand.Teen));
            Assert.That(saved.AgeBandCorrected, Is.True);
            Assert.That(saved.AdsPersonalized, Is.False);
            Assert.That(boot.ConsentAds!.NonPersonalized, Is.True);
            Assert.That(settings.AgeShown, Is.False, "una sola vez (08 §7)");
            Assert.That(settings.PrivacyShown, Is.True);
        }
    }
}
