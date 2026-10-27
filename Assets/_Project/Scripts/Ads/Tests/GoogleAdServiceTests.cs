#nullable enable
using System;
using System.Collections.Generic;
using AF.Ads.Google;
using AF.Platform;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// AdMob directo con Google Mobile Ads detrás de <see cref="IAdService"/> con el SDK falso (D-049; 08 §2.3, §7 y §10.2): el orden
    /// edad → nuestro consentimiento → <c>SetRequestConfiguration</c> → UMP → <c>MobileAds.Initialize</c>; &lt;13 sin ninguna llamada;
    /// 13-17 con <c>TagForUnderAgeOfConsent</c> y <c>npa</c>; UMP decide en el EEE; el SSV (<c>user_id</c> y
    /// <c>custom_data = placement|nonce</c>); la recompensa una sola vez; los bloques de prueba en desarrollo.
    /// </summary>
    public sealed class GoogleAdServiceTests
    {
        private static readonly DateTime At = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        private const string Uid = "AbCdEfGhIjKlMnOpQrStUvWxYz12";
        private const string Nonce = "0f8fad5b-d9cb-469f-a165-70867728950e";

        private static ConsentFlags FlagsFor(AgeBand band, bool ads, string profile = LegalProfiles.BrLgpdEca)
        {
            return ConsentPolicy.Flags(new ConsentRecord(profile, 1, At, true, ads, band), ConsentConfig.Defaults);
        }

        private sealed class Rig
        {
            public readonly FakeGmaPort Port = new FakeGmaPort();
            public readonly ManualClock Clock = new ManualClock(At);
            public string? Uid = GoogleAdServiceTests.Uid;
            public int NextLevel = 1;
            public readonly GoogleAdService Google;
            public readonly ConsentAdService Consent;
            public readonly List<AdImpression> Impressions = new List<AdImpression>();

            public Rig()
            {
                Google = new GoogleAdService(Port, Clock, () => Uid, () => NextLevel, null, () => Nonce);
                Consent = new ConsentAdService(Google);
                Consent.Impression += Impressions.Add;
            }

            /// <summary>Nuestra pantalla decide; UMP responde y Google se inicia (salvo que se pida lo contrario).</summary>
            public void Decide(AgeBand band, bool ads, string profile = LegalProfiles.BrLgpdEca, bool resolveUmp = true, bool initialize = true)
            {
                Consent.Apply(FlagsFor(band, ads, profile));
                if (resolveUmp)
                {
                    Port.ResolveConsent(true);
                }

                if (initialize)
                {
                    Port.CompleteInitialization();
                }
            }
        }

        [Test]
        public void BeforeConsent_TheSdkIsNeverTouched()
        {
            var rig = new Rig();

            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False);
            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);

            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
            Assert.That(rig.Port.Calls, Is.Empty, "nada antes de la edad y el consentimiento (08 §2.3)");
            Assert.That(rig.Google.ConsentRequested, Is.False);
            Assert.That(rig.Google.InitRequested, Is.False);
        }

        [Test]
        public void Undecided_DoesNotTouchTheSdk()
        {
            var rig = new Rig();
            rig.Consent.Apply(ConsentPolicy.Undecided);

            Assert.That(rig.Port.Calls, Is.Empty);
        }

        [Test]
        public void Under13_NoCallAtAll_NotEvenUmp()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Under13, ads: true, resolveUmp: false, initialize: false);

            Assert.That(rig.Port.Calls, Is.Empty, "con <13 ningún SDK de anuncios se toca (D-049; D-033 B12)");
            Assert.That(rig.Port.ConsentRequests, Is.EqualTo(0));
            Assert.That(rig.Port.Initializations, Is.EqualTo(0));
            Assert.That(rig.Consent.IsReady(AdPlacement.DoubleHarvest), Is.False);
        }

        [Test]
        public void Adult_Order_IsConfig_ThenUmp_ThenInitialize_ThenLoad()
        {
            var rig = new Rig();
            rig.Consent.Apply(FlagsFor(AgeBand.Adult, ads: true));

            Assert.That(rig.Port.Calls, Is.EqualTo(new List<string> { "config:tfua=false", "ump:tfua=false" }), "sin Initialize hasta que UMP responda");
            Assert.That(rig.Google.InitRequested, Is.False);

            rig.Port.ResolveConsent(true);
            Assert.That(rig.Port.Calls[2], Is.EqualTo("init"));
            Assert.That(rig.Port.Count("load:"), Is.EqualTo(0), "no se carga antes de que Google avise de que está listo");

            rig.Port.CompleteInitialization();
            Assert.That(rig.Port.Calls, Is.EqualTo(new List<string> { "config:tfua=false", "ump:tfua=false", "init", "load:rewarded:npa=false" }));
        }

        [Test]
        public void UmpSaysCannotRequestAds_NoInitialize_AndItRetriesOnResume()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true, resolveUmp: false, initialize: false);
            rig.Port.ResolveConsent(false);

            Assert.That(rig.Port.Initializations, Is.EqualTo(0), "sin CanRequestAds no se inicia (Google: UMP antes de Initialize)");
            rig.Google.OnResume();
            Assert.That(rig.Port.ConsentRequests, Is.EqualTo(2), "al volver a la app se pregunta otra vez");
            rig.Port.ResolveConsent(true);
            Assert.That(rig.Port.Initializations, Is.EqualTo(1));
        }

        [Test]
        public void AdultWhoRejected_LoadsWithNpa()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: false);

            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=true"), "sin consentimiento de personalizados: npa (08 §7)");
        }

        [Test]
        public void Teen_TagsUnderAgeOfConsent_BeforeUmpAndInit_AndIsNonPersonalized()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Teen, ads: true);

            List<string> calls = rig.Port.Calls;
            Assert.That(calls.IndexOf("config:tfua=true"), Is.EqualTo(0), "13-17: TagForUnderAgeOfConsent antes de nada");
            Assert.That(calls.IndexOf("ump:tfua=true"), Is.InRange(1, calls.IndexOf("init") - 1), "y en UMP");
            Assert.That(calls, Does.Contain("load:rewarded:npa=true"), "13-17: npa aunque haya aceptado (biz_under18_no_personalization)");
            Assert.That(rig.Google.Consent!.MaxAdContentRating, Is.EqualTo("G"), "D-029");
        }

        [Test]
        public void UmpProfile_Adult_LetsUmpDecide_WithoutNpa()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: false, profile: LegalProfiles.Ump);

            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=false"), "en el EEE/UK/CH deciden las cadenas TCF de UMP");
            Assert.That(rig.Google.Consent!.UmpDecides, Is.True);
        }

        [Test]
        public void UmpProfile_Teen_NeverTouchesTheSdk_QMax6b()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Teen, ads: true, profile: LegalProfiles.Ump, resolveUmp: false, initialize: false);

            Assert.That(rig.Port.Calls, Is.Empty, "Q-MAX-6 (b), como con MAX: con 13-17 y el perfil ump, sin anuncios");
        }

        [Test]
        public void UmpProfile_AdultCorrectedToTeen_StopsLoadingAndShowing_D055()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true, profile: LegalProfiles.Ump);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.True);
            int calls = rig.Port.Calls.Count;

            rig.Consent.Apply(FlagsFor(AgeBand.Teen, ads: true, profile: LegalProfiles.Ump));

            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False, "D-055, punto 4: 13-17 en el EEE, el Reino Unido y Suiza, sin anuncios");
            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
            Assert.That(rig.Port.Calls.Count, Is.EqualTo(calls), "ni una llamada más al SDK");
        }

        /// <summary>
        /// D-055, punto 3: el ID de publicidad (permiso <c>AD_ID</c>) solo sirve para personalizar con 18+ y el consentimiento de
        /// anuncios personalizados (o, en el EEE, el Reino Unido y Suiza, con lo que decida UMP); con 13-17, sin consentimiento o sin
        /// país, cada petición lleva <c>npa=1</c>; con &lt;13 y con 13-17 en el perfil <c>ump</c>, ninguna llamada al SDK.
        /// </summary>
        [TestCase(AgeBand.Adult, true, LegalProfiles.BrLgpdEca, true, "load:rewarded:npa=false")]
        [TestCase(AgeBand.Adult, false, LegalProfiles.BrLgpdEca, true, "load:rewarded:npa=true")]
        [TestCase(AgeBand.Adult, true, LegalProfiles.Default, false, "load:rewarded:npa=true")]
        [TestCase(AgeBand.Adult, true, LegalProfiles.Default, true, "load:rewarded:npa=false")]
        [TestCase(AgeBand.Adult, false, LegalProfiles.Ump, true, "load:rewarded:npa=false")]
        [TestCase(AgeBand.Teen, true, LegalProfiles.BrLgpdEca, true, "load:rewarded:npa=true")]
        [TestCase(AgeBand.Teen, true, LegalProfiles.Default, false, "load:rewarded:npa=true")]
        [TestCase(AgeBand.Teen, true, LegalProfiles.Ump, true, null)]
        [TestCase(AgeBand.Under13, true, LegalProfiles.BrLgpdEca, true, null)]
        public void AdvertisingId_PersonalizedOnlyForAdultsWhoConsent_D055(AgeBand band, bool ads, string profile, bool countryKnown, string? expectedLoad)
        {
            var rig = new Rig();
            rig.Consent.Apply(ConsentPolicy.Flags(new ConsentRecord(profile, 1, At, true, ads, band), ConsentConfig.Defaults, null, countryKnown));
            if (rig.Port.ConsentRequests > 0)
            {
                rig.Port.ResolveConsent(true);
                rig.Port.CompleteInitialization();
            }

            if (expectedLoad == null)
            {
                Assert.That(rig.Port.Calls, Is.Empty, "sin anuncios: ni SetRequestConfiguration, ni UMP, ni Initialize");
                return;
            }

            Assert.That(rig.Port.Calls, Does.Contain(expectedLoad));
            Assert.That(rig.Port.Calls, Does.Contain(band == AgeBand.Teen ? "config:tfua=true" : "config:tfua=false"), "13-17: TagForUnderAgeOfConsent");
        }

        /// <summary>
        /// D-049, añadido del 2026-10-27 (Unity Ads como red de mediación): Unity Ads no lee nuestra pantalla, así que antes de UMP y de
        /// <c>MobileAds.Initialize</c> recibe por su <c>MetaData</c> los mismos flags que recibía MAX: <c>gdpr.consent</c> y
        /// <c>privacy.consent</c> (CCPA y estados de EE. UU.) en false con <c>npa</c> (13-17, también en EE. UU., sin consentimiento o
        /// sin país) y en true con el consentimiento de 18+; con el perfil <c>ump</c> de 18+ no se manda nada (TCF); con &lt;13 y con
        /// 13-17 en el perfil <c>ump</c>, nada.
        /// </summary>
        [TestCase(AgeBand.Adult, true, LegalProfiles.BrLgpdEca, true, "unity:gdpr=true:privacy=true:before-init")]
        [TestCase(AgeBand.Adult, false, LegalProfiles.BrLgpdEca, true, "unity:gdpr=false:privacy=false:before-init")]
        [TestCase(AgeBand.Adult, true, LegalProfiles.Default, true, "unity:gdpr=true:privacy=true:before-init")]
        [TestCase(AgeBand.Adult, true, LegalProfiles.Default, false, "unity:gdpr=false:privacy=false:before-init")]
        [TestCase(AgeBand.Adult, true, LegalProfiles.Ump, true, "unity:gdpr=-:privacy=-:before-init")]
        [TestCase(AgeBand.Teen, true, LegalProfiles.Default, true, "unity:gdpr=false:privacy=false:before-init")]
        [TestCase(AgeBand.Teen, true, LegalProfiles.MxLfpdppp, true, "unity:gdpr=false:privacy=false:before-init")]
        [TestCase(AgeBand.Teen, true, LegalProfiles.Ump, true, null)]
        [TestCase(AgeBand.Under13, true, LegalProfiles.BrLgpdEca, true, null)]
        public void UnityAds_GetsOurConsent_BeforeInitialize(AgeBand band, bool ads, string profile, bool countryKnown, string? expected)
        {
            var rig = new Rig();
            rig.Consent.Apply(ConsentPolicy.Flags(new ConsentRecord(profile, 1, At, true, ads, band), ConsentConfig.Defaults, null, countryKnown));
            if (rig.Port.ConsentRequests > 0)
            {
                rig.Port.ResolveConsent(true);
                rig.Port.CompleteInitialization();
            }

            if (expected == null)
            {
                Assert.That(rig.Port.MediationConsent, Is.Empty, "sin anuncios: tampoco el MetaData de Unity Ads");
                return;
            }

            Assert.That(rig.Port.MediationConsent, Is.EqualTo(new List<string> { expected }), "una vez, antes de Initialize");
            Assert.That(rig.Port.Initializations, Is.EqualTo(1));
        }

        [Test]
        public void UnityAds_ConsentWithdrawnInSettings_IsSentAgain_OnlyWhenItChanges()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Consent.Apply(FlagsFor(AgeBand.Adult, ads: true)); // la misma decisión: no se repite
            rig.Consent.Apply(FlagsFor(AgeBand.Adult, ads: false)); // retira el consentimiento en Ajustes

            Assert.That(rig.Port.MediationConsent, Is.EqualTo(new List<string>
            {
                "unity:gdpr=true:privacy=true:before-init",
                "unity:gdpr=false:privacy=false:after-init",
            }));
        }

        [Test]
        public void AgeCorrectedToUnder13_AfterInit_StopsLoadingAndShowing()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.True);
            int calls = rig.Port.Calls.Count;

            rig.Consent.Apply(FlagsFor(AgeBand.Under13, ads: false));

            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False);
            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
            Assert.That(rig.Port.Calls.Count, Is.EqualTo(calls), "ni una llamada más al SDK");
        }

        [Test]
        public void AdultCorrectedToTeen_ReappliesTheUnderAgeFlag()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);

            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            rig.Consent.Apply(FlagsFor(AgeBand.Teen, ads: true));

            Assert.That(rig.Port.Calls, Does.Contain("config:tfua=true"));
            Assert.That(rig.Port.Initializations, Is.EqualTo(1), "Initialize una sola vez por proceso");
            Assert.That(rig.Port.ConsentRequests, Is.EqualTo(1), "UMP no se vuelve a pedir (13-17 fuera de ump va con npa)");
            Assert.That(rig.Port.Calls[rig.Port.Calls.Count - 1], Is.EqualTo("load:rewarded:npa=true"), "el anuncio de adulto se descarta");
        }

        [Test]
        public void AgeCorrectedToUnder13_WhileUmpIsInFlight_NeverInitializes()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true, resolveUmp: false, initialize: false);
            int calls = rig.Port.Calls.Count;

            rig.Consent.Apply(FlagsFor(AgeBand.Under13, ads: false));
            rig.Port.ResolveConsent(true);
            rig.Google.OnResume();

            Assert.That(rig.Port.Initializations, Is.EqualTo(0), "con <13 nunca MobileAds.Initialize (08 §2.3; D-033 B12)");
            Assert.That(rig.Port.Calls.Count, Is.EqualTo(calls), "ninguna llamada más al SDK");
            Assert.That(rig.Port.ConsentRequests, Is.EqualTo(1), "ni se vuelve a pedir UMP");
        }

        [Test]
        public void Undecided_WhileUmpIsInFlight_NeverInitializes()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true, resolveUmp: false, initialize: false);

            rig.Consent.Apply(ConsentPolicy.Undecided);
            rig.Port.ResolveConsent(true);

            Assert.That(rig.Port.Initializations, Is.EqualTo(0));
        }

        [Test]
        public void Resume_RewardedStillOpen_WaitsTheGrace_ThenRewardedIfTheRewardArrived()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            var outcomes = new List<AdOutcome>();
            rig.Consent.Show(AdPlacement.Hint, outcomes.Add);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived);

            rig.Google.OnResume();
            rig.Clock.Advance(TimeSpan.FromSeconds(2));
            rig.Google.Tick();
            Assert.That(outcomes, Is.Empty, "dentro de ResumeGrace sigue pendiente");

            rig.Clock.Advance(TimeSpan.FromSeconds(1));
            rig.Google.Tick();
            Assert.That(outcomes, Is.EqualTo(new List<AdOutcome> { AdOutcome.Rewarded }));

            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed); // el cierre tardío no da un segundo resultado
            rig.Clock.Advance(TimeSpan.FromSeconds(10));
            rig.Google.Tick();
            Assert.That(outcomes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Resume_RewardedWithoutReward_IsSkipped_AfterTheGrace()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);

            rig.Google.OnResume();
            rig.Clock.Advance(GoogleAdService.ResumeGrace);
            rig.Google.Tick();

            Assert.That(outcome, Is.EqualTo(AdOutcome.Skipped));
        }

        [Test]
        public void Resume_InterstitialStillOpen_IsShown_AfterTheGrace()
        {
            var rig = new Rig { NextLevel = 30 };
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Interstitial, GmaAdEvent.Loaded);
            var outcomes = new List<AdOutcome>();
            rig.Consent.Show(AdPlacement.InterstitialPostLevel, outcomes.Add);

            rig.Google.OnResume();
            rig.Clock.Advance(GoogleAdService.ResumeGrace);
            rig.Google.Tick();
            rig.Port.Raise(GmaAdKind.Interstitial, GmaAdEvent.Closed);

            Assert.That(outcomes, Is.EqualTo(new List<AdOutcome> { AdOutcome.Shown }));
        }

        [Test]
        public void SwitchToNpa_DiscardsThePersonalizedAd_AndReloadsWithNpa()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.True);

            rig.Consent.Apply(FlagsFor(AgeBand.Adult, ads: false)); // Ajustes: retira los anuncios personalizados

            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False, "el anuncio personalizado ya cargado no se muestra");
            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=true"));
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.True);
        }

        [Test]
        public void SwitchToNpa_WhileLoading_DropsTheInFlightAd()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true); // carga personalizada en curso

            rig.Consent.Apply(FlagsFor(AgeBand.Adult, ads: false));
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded); // llega la personalizada

            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False);
            Assert.That(rig.Port.Calls[rig.Port.Calls.Count - 1], Is.EqualTo("load:rewarded:npa=true"), "se pide otra con npa");
        }

        [Test]
        public void Rewarded_SetsSsv_UserIdAndCustomData_AndRewardsOnce()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            var outcomes = new List<AdOutcome>();
            rig.Consent.Show(AdPlacement.Hint, outcomes.Add);

            Assert.That(rig.Port.LastUserId, Is.EqualTo(Uid), "SSV: user_id = UID (08 §10.2)");
            Assert.That(rig.Port.LastCustomData, Is.EqualTo("rv_hint|" + Nonce), "SSV: custom_data = placement|nonce");
            Assert.That(rig.Google.LastRewardNonce, Is.EqualTo(Nonce));

            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived);
            rig.Clock.Advance(TimeSpan.FromSeconds(10));
            rig.Google.Tick();

            Assert.That(outcomes, Is.EqualTo(new List<AdOutcome> { AdOutcome.Rewarded }), "premio una sola vez");
        }

        [Test]
        public void Rewarded_WithoutUid_SetsNoSsv_AndStillRewardsProvisionally()
        {
            var rig = new Rig { Uid = null };
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            var outcomes = new List<AdOutcome>();
            rig.Consent.Show(AdPlacement.Hint, outcomes.Add);

            Assert.That(rig.Port.Calls, Does.Contain("show:rewarded:nouser"));
            Assert.That(rig.Port.LastUserId, Is.Null, "sin UID no va user_id");
            Assert.That(rig.Port.LastCustomData, Is.Null, "sin UID no se fija SSV (D-049)");
            Assert.That(rig.Google.LastRewardNonce, Is.Null, "sin SSV no hay nonce que confirmar");
            Assert.That(rig.Google.LastRewardPlacement, Is.EqualTo(AdPlacement.Hint));

            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed);

            Assert.That(outcomes, Is.EqualTo(new List<AdOutcome> { AdOutcome.Rewarded }), "el premio sigue como hoy (provisional)");
        }

        [Test]
        public void Rewarded_EmptyUid_IsLikeNoUid()
        {
            var rig = new Rig { Uid = string.Empty };
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            rig.Consent.Show(AdPlacement.Hint, _ => { });

            Assert.That(rig.Port.LastUserId, Is.Null);
            Assert.That(rig.Port.LastCustomData, Is.Null);
        }

        [Test]
        public void Rewarded_UidThatArrivesLater_IsUsedOnTheNextShow()
        {
            var rig = new Rig { Uid = null };
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            rig.Consent.Show(AdPlacement.Hint, _ => { });
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed);
            Assert.That(rig.Port.LastCustomData, Is.Null);

            rig.Uid = "kT3x9QeVb2Rz8LmN4pWs7YcA1dF5";
            rig.Clock.Advance(TimeSpan.FromMinutes(10));
            rig.Google.Tick();
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            rig.Consent.Show(AdPlacement.Hint, _ => { });

            Assert.That(rig.Port.LastUserId, Is.EqualTo("kT3x9QeVb2Rz8LmN4pWs7YcA1dF5"), "el UID se lee en cada recompensado");
            Assert.That(rig.Port.LastCustomData, Is.EqualTo("rv_hint|" + Nonce));
            Assert.That(rig.Google.LastRewardNonce, Is.EqualTo(Nonce));
        }

        [Test]
        public void ClosedWithoutReward_IsSkipped_AfterTheGrace()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed);
            rig.Google.Tick();
            Assert.That(outcome, Is.Null);

            rig.Clock.Advance(GoogleAdService.RewardGrace);
            rig.Google.Tick();
            Assert.That(outcome, Is.EqualTo(AdOutcome.Skipped));
        }

        [Test]
        public void LateReward_WithinTheGrace_IsRewarded()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Closed);
            rig.Clock.Advance(TimeSpan.FromSeconds(1));
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived);

            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded));
        }

        [Test]
        public void Interstitial_LoadsOnlyNearLevel22_AndClosesAsShown()
        {
            var rig = new Rig { NextLevel = 5 };
            rig.Decide(AgeBand.Adult, ads: true);
            Assert.That(rig.Port.Count("load:interstitial"), Is.EqualTo(0));

            rig.NextLevel = 21;
            rig.Consent.IsReady(AdPlacement.InterstitialPostLevel);
            Assert.That(rig.Port.Count("load:interstitial"), Is.EqualTo(1));

            rig.Port.Raise(GmaAdKind.Interstitial, GmaAdEvent.Loaded);
            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.InterstitialPostLevel, o => outcome = o);
            Assert.That(rig.Port.Calls, Does.Contain("show:interstitial:nouser"), "el intersticial no lleva SSV");
            rig.Port.Raise(GmaAdKind.Interstitial, GmaAdEvent.Closed);
            Assert.That(outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void LoadFailure_RetriesWithBackoff()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.LoadFailed);

            rig.Consent.IsReady(AdPlacement.Hint);
            Assert.That(rig.Port.Count("load:rewarded"), Is.EqualTo(1), "espera antes de reintentar");
            rig.Clock.Advance(TimeSpan.FromSeconds(2));
            rig.Consent.IsReady(AdPlacement.Hint);
            Assert.That(rig.Port.Count("load:rewarded"), Is.EqualTo(2));
        }

        [Test]
        public void DisplayFailed_IsFailed()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.DisplayFailed);
            Assert.That(outcome, Is.EqualTo(AdOutcome.Failed));
        }

        [Test]
        public void Backoff_DoublesFrom2sUpTo64s()
        {
            // Antes, MaxAdService.Backoff (D-042); con MAX fuera del proyecto (añadido de D-049 del 2026-10-27), de GoogleAdService.
            Assert.That(GoogleAdService.Backoff(1), Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(GoogleAdService.Backoff(3), Is.EqualTo(TimeSpan.FromSeconds(8)));
            Assert.That(GoogleAdService.Backoff(6), Is.EqualTo(TimeSpan.FromSeconds(64)));
            Assert.That(GoogleAdService.Backoff(20), Is.EqualTo(TimeSpan.FromSeconds(64)));
            Assert.That(GoogleAdService.Backoff(0), Is.EqualTo(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void Paid_IsAnAdMobImpression_InUnits()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            rig.Consent.Show(AdPlacement.Reveal, _ => { });

            rig.Port.RaisePaid(GmaAdKind.Rewarded, "AdMob Network", 12_500, "USD");

            Assert.That(rig.Impressions.Count, Is.EqualTo(1));
            AdImpression i = rig.Impressions[0];
            Assert.That(i.AdPlatform, Is.EqualTo(AdImpression.PlatformAdMob));
            Assert.That(i.AdSource, Is.EqualTo("AdMob Network"));
            Assert.That(i.Value, Is.EqualTo(0.0125).Within(1e-12), "AdValue viene en micros");
            Assert.That(i.Placement, Is.EqualTo(AdPlacement.Reveal));
            Assert.That(i.AdFormat, Is.EqualTo("rewarded"));
        }

        [Test]
        public void PrivacyOptions_OnlyWhenUmpRequiresThem()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: false, profile: LegalProfiles.Ump);

            Assert.That(rig.Consent.ShowPrivacyOptions(), Is.False, "UMP no pide punto de entrada");
            rig.Port.PrivacyOptionsRequired = true;
            Assert.That(rig.Consent.ShowPrivacyOptions(), Is.True);
            Assert.That(rig.Port.Calls, Does.Contain("privacy"));
        }

        [Test]
        public void Tick_PumpsTheSdkQueue()
        {
            var rig = new Rig();
            rig.Google.Tick();
            rig.Google.Tick();
            Assert.That(rig.Port.Pumps, Is.EqualTo(2), "los avisos del SDK se entregan en el hilo principal cada fotograma");
        }

        [Test]
        public void Units_DevelopmentOrProvisionalId_AreGoogleTestUnits()
        {
            GoogleAdUnits dev = GoogleAdUnits.Choose(developmentBuild: true, "com.parallext.tintalu");
            GoogleAdUnits rehearsal = GoogleAdUnits.Choose(developmentBuild: false, GoogleAdUnits.ProvisionalApplicationId);
            GoogleAdUnits unknown = GoogleAdUnits.Choose(developmentBuild: false, null);

            foreach (GoogleAdUnits u in new[] { dev, rehearsal, unknown })
            {
                Assert.That(u.Production, Is.False);
                Assert.That(u.Rewarded, Is.EqualTo("ca-app-pub-3940256099942544/5224354917"), "bloque de prueba oficial de Google");
                Assert.That(u.Interstitial, Is.EqualTo("ca-app-pub-3940256099942544/1033173712"), "bloque de prueba oficial de Google");
            }
        }

        [Test]
        public void Units_ReleaseWithTheFinalId_AreProduction()
        {
            GoogleAdUnits release = GoogleAdUnits.Choose(developmentBuild: false, "com.parallext.tintalu");

            Assert.That(release.Production, Is.True);
            Assert.That(release.For(GmaAdKind.Rewarded), Is.EqualTo("ca-app-pub-2916158449453694/1182085615"));
            Assert.That(release.For(GmaAdKind.Interstitial), Is.EqualTo("ca-app-pub-2916158449453694/7187673451"));
            StringAssert.StartsWith("ca-app-pub-2916158449453694~", GoogleAdUnits.AndroidAppId);
        }

        // ---- T-L5: rdp=1 con 13-17 en EE. UU. ([F28]) ----

        private static void ApplyAndStart(Rig rig, ConsentFlags flags, string? country)
        {
            rig.Consent.Apply(flags, country);
            rig.Port.ResolveConsent(true);
            rig.Port.CompleteInitialization();
        }

        [Test]
        public void TeenInTheUs_EveryRequestCarriesRdp()
        {
            var rig = new Rig();
            ApplyAndStart(rig, FlagsFor(AgeBand.Teen, ads: true, profile: LegalProfiles.Default), "US");

            Assert.That(rig.Google.Consent!.RestrictedDataProcessing, Is.True);
            Assert.That(rig.Consent.RestrictedDataProcessing, Is.True);
            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=true:rdp=true"), "T-L5: rdp=1 además de npa=1");
            Assert.That(rig.Port.Calls, Has.No.Member("load:rewarded:npa=true"), "ninguna petición sin rdp");
        }

        [TestCase("BR")]
        [TestCase("MX")]
        [TestCase(null)]
        public void TeenOutsideTheUs_NoRdp(string? country)
        {
            var rig = new Rig();
            ApplyAndStart(rig, FlagsFor(AgeBand.Teen, ads: true, profile: LegalProfiles.Default), country);

            Assert.That(rig.Google.Consent!.RestrictedDataProcessing, Is.False);
            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=true"));
            Assert.That(rig.Port.Count("load:rewarded:npa=true:rdp"), Is.EqualTo(0));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AdultInTheUs_NoRdp(bool ads)
        {
            var rig = new Rig();
            ApplyAndStart(rig, FlagsFor(AgeBand.Adult, ads, profile: LegalProfiles.Default), "US");

            Assert.That(rig.Google.Consent!.RestrictedDataProcessing, Is.False, "RDP solo para 13-17 (T-L5)");
            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=" + (ads ? "false" : "true")));
        }

        [Test]
        public void TeenInTheUs_WithPersonalizationAllowedByConfig_StillCarriesRdp()
        {
            var config = new ConsentConfig { BizUnder18NoPersonalization = false };
            ConsentFlags flags = ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.Default, 1, At, true, true, AgeBand.Teen), config);
            var rig = new Rig();
            ApplyAndStart(rig, flags, "us");

            Assert.That(rig.Port.Calls, Does.Contain("load:rewarded:npa=false:rdp=true"), "rdp no depende de npa");
        }

        [Test]
        public void AdultInTheUs_CorrectedToTeenByTheAgeSignal_DiscardsTheLoadedAd_AndReloadsWithRdp()
        {
            var rig = new Rig();
            var record = new ConsentRecord(LegalProfiles.Default, 1, At, true, true, AgeBand.Adult);
            ApplyAndStart(rig, ConsentPolicy.Flags(record, ConsentConfig.Defaults), "US");
            rig.Port.Raise(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.True);

            // La señal de Play Age Signals llega tarde con 13-15 (T-L3): la más estricta, 13-17.
            ConsentFlags corrected = ConsentPolicy.Flags(record, ConsentConfig.Defaults, signalBand: AgeBand.Teen);
            Assert.That(corrected.BandFromSignal, Is.True);
            rig.Consent.Apply(corrected, "US");

            Assert.That(rig.Port.Calls[rig.Port.Calls.Count - 1], Is.EqualTo("load:rewarded:npa=true:rdp=true"),
                "el anuncio pedido sin rdp se descarta y se vuelve a pedir con rdp=1");
        }

        [Test]
        public void InTheEditor_TheProviderIsTheSimulatedOne()
        {
            IAdService? real = GoogleAdProvider.Create(new ManualClock(At), () => null, () => 1, null);

            Assert.That(real, Is.Null, "el editor y los tests usan los anuncios simulados (08 §2.1, principio 6)");
            Assert.That(GoogleAdProvider.LastReason, Is.EqualTo(GoogleAdProvider.ReasonEditor));
        }
    }
}
