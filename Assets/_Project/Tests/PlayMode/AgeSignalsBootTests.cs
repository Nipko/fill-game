#nullable enable
using System;
using System.Collections;
using AF.Analytics;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.RemoteConfig;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Play Age Signals en la escena <c>Game</c> real con la señal falsa (T-L3, T-L4 y T-L5 de
    /// <c>docs/legal/investigacion-legal-mundial.md</c> §11; D-057 y su añadido del 2026-10-27): con la fuente
    /// <c>self_declared+play_age_signals</c>, una señal de 0-12 bloquea (también la escena recargada) sin guardar nada; una de 13-17
    /// aplica lo de 13-17 aunque se declarara 18+, sin mandar la banda a la analítica; con el default <c>self_declared</c> no se pide.
    /// </summary>
    public sealed class AgeSignalsBootTests
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
            GameBootstrap.ResetConsentOverrides(); // también la señal (ResetAgeSignals)
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetCloudOverrides();
            RC.Reset();
        }

        private static void WithSignalSource()
        {
            GameBootstrap.RemoteConfigBackendOverride = new FakeRemoteConfigBackend()
                .Set("consent_age_signal_source", ConsentConfig.SourceWithPlayAgeSignals)
                .StoreFetched();
        }

        private static Profile ProfileWith(ConsentRecord? consent)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 4;
            profile.Progress.MaxLevel = 3;
            profile.Consent = consent;
            return profile;
        }

        [UnityTest]
        public IEnumerator Signal0to12_BlocksTheGame_AndTheReloadedSceneStaysBlocked_WithoutSavingIt()
        {
            WithSignalSource();
            var signals = new FakeAgeSignals(AgeSignal.ForRange(0, 12));
            GameBootstrap.AgeSignalsOverride = signals;
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult)));

            yield return PlayModeHelpers.LoadAppWithConsent(store);
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.ConsentBlocked && GameBootstrap.Current.App == null,
                "el arranque bloqueado por la señal de 0-12");

            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Blocked), "«Pide a un adulto que instale el juego», como <13");
            Assert.That(signals.Requests, Is.EqualTo(1), "una petición por arranque (por proceso): la recarga usa la respuesta");
            Assert.That(GameBootstrap.LastAgeSignal!.Value.Band, Is.EqualTo(AgeBand.Under13));
            Assert.That(store.Load().Profile!.Consent!.AgeBand, Is.EqualTo(AgeBand.Adult), "la señal no se guarda en el perfil");
        }

        [UnityTest]
        public IEnumerator SignalTeen_DeclaredAdult_GetsTeenTreatment_AndAfConsentHasNoAgeBand()
        {
            WithSignalSource();
            GameBootstrap.AgeSignalsOverride = new FakeAgeSignals(AgeSignal.ForRange(13, 15));
            var sink = new FakeAnalyticsSink(requiresConsent: true);
            GameBootstrap.AnalyticsSinkOverride = sink;
            var store = new MemoryProfileStore();

            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return PlayModeHelpers.WaitFor(() => GameBootstrap.LastAgeSignal.HasValue, "la respuesta de la señal falsa");
            ConsentScreen view = boot.ConsentView!;
            view.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return null;
            Assert.That(view.AdsToggleShown, Is.False, "13-17 por la señal: sin el interruptor de anuncios personalizados");
            view.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.App!.Screen != AppScreen.Boot, "la primera pantalla tras el consentimiento");

            Assert.That(boot.Consent!.Flags.Band, Is.EqualTo(AgeBand.Teen));
            Assert.That(boot.ConsentAds!.NonPersonalized, Is.True, "npa con 13-17");
            Assert.That(boot.ConsentAds.RestrictedDataProcessing, Is.False, "rdp solo en US (T-L5)");
            Assert.That(store.Load().Profile!.Consent!.AgeBand, Is.EqualTo(AgeBand.Adult), "el registro guarda la autodeclarada");
            AnalyticsEvent? consent = sink.Last("af_consent");
            Assert.That(consent, Is.Not.Null);
            Assert.That(consent!.Find("age_band"), Is.Null, "T-L4: corregida por la señal, la banda no va a la analítica");
        }

        [UnityTest]
        public IEnumerator SignalTeen_InTheUs_EveryAdRequestCarriesRdp()
        {
            WithSignalSource();
            GameBootstrap.SimCountryOverride = new ManualSimCountry("US");
            GameBootstrap.AgeSignalsOverride = new FakeAgeSignals(AgeSignal.ForRange(16, 17));
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Default, 1, T0, true, true, AgeBand.Adult)));

            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return PlayModeHelpers.WaitFor(
                () => boot.ConsentAds != null && boot.ConsentAds.RestrictedDataProcessing, "los flags con la señal de 16-17 en US");

            Assert.That(boot.Consent!.Flags.Band, Is.EqualTo(AgeBand.Teen));
            Assert.That(boot.ConsentAds!.NonPersonalized, Is.True);
        }

        [UnityTest]
        public IEnumerator DefaultSource_NeverAsksForTheSignal()
        {
            var signals = new FakeAgeSignals(AgeSignal.ForRange(0, 12));
            GameBootstrap.AgeSignalsOverride = signals;
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult)));

            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(signals.Requests, Is.EqualTo(0), "08 §9: con self_declared no se llama a Play Age Signals");
            Assert.That(boot.ConsentBlocked, Is.False);
            Assert.That(boot.Consent!.Flags.Band, Is.EqualTo(AgeBand.Adult));
        }
    }
}
