#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// AppLovin MAX detrás de <see cref="IAdService"/> con el SDK falso (D-042; 08 §2.3, §7 y §10.2): el orden consentimiento → flags →
    /// <c>InitializeSdk</c>, &lt;13 nunca inicia, 13-17 no personalizado, UMP decide en el EEE, la precarga según el nivel, el SSV
    /// (<c>SetUserId</c> y <c>custom_data = placement|nonce</c>) y la concesión provisional con <c>OnAdReceivedRewardEvent</c>.
    /// </summary>
    public sealed class MaxAdServiceTests
    {
        private static readonly DateTime At = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        private const string Uid = "AbCdEfGhIjKlMnOpQrStUvWxYz12";
        private const string Nonce = "0f8fad5b-d9cb-469f-a165-70867728950e";

        private static ConsentFlags FlagsFor(AgeBand band, bool ads, string profile = LegalProfiles.BrLgpdEca)
        {
            return ConsentPolicy.Flags(new ConsentRecord(profile, 1, At, true, ads, band), ConsentConfig.Defaults);
        }

        private sealed class Rig
        {
            public readonly FakeMaxSdkPort Port = new FakeMaxSdkPort();
            public readonly ManualClock Clock = new ManualClock(At);
            public string? Uid = MaxAdServiceTests.Uid;
            public int NextLevel = 1;
            public readonly MaxAdService Max;
            public readonly ConsentAdService Consent;

            public Rig()
            {
                Max = new MaxAdService(Port, Clock, () => Uid, () => NextLevel, null, () => Nonce);
                Consent = new ConsentAdService(Max);
            }

            public void Decide(AgeBand band, bool ads, string profile = LegalProfiles.BrLgpdEca, bool initialize = true)
            {
                Consent.Apply(FlagsFor(band, ads, profile));
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
            Assert.That(rig.Port.Calls, Is.Empty, "nada antes de consentir (08 §2.3)");
            Assert.That(rig.Max.InitRequested, Is.False);
        }

        [Test]
        public void Undecided_DoesNotInitialize()
        {
            var rig = new Rig();
            rig.Consent.Apply(ConsentPolicy.Undecided);

            Assert.That(rig.Port.Calls, Is.Empty);
            Assert.That(rig.Max.Consent!.MayInitialize, Is.False);
        }

        [Test]
        public void Under13_NeverInitializes()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Under13, ads: true, initialize: false);

            Assert.That(rig.Port.Initializations, Is.EqualTo(0), "con <13 MAX nunca se inicia (D-042)");
            Assert.That(rig.Port.Calls, Is.Empty);
            Assert.That(rig.Max.Consent!.ChildDirected, Is.True);
            Assert.That(rig.Consent.IsReady(AdPlacement.DoubleHarvest), Is.False);
        }

        [Test]
        public void Adult_ConsentFlagsGoBeforeInitialize()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);

            Assert.That(rig.Port.Calls, Is.EqualTo(new List<string> { "consent:true", "dns:false", "init", "load:rewarded" }));
            Assert.That(rig.Port.Initializations, Is.EqualTo(1));
        }

        [Test]
        public void AdultWhoRejected_IsNonPersonalized_BeforeInitialize()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: false);

            Assert.That(rig.Port.Calls.IndexOf("consent:false"), Is.LessThan(rig.Port.Calls.IndexOf("init")));
            Assert.That(rig.Port.Calls.IndexOf("dns:true"), Is.LessThan(rig.Port.Calls.IndexOf("init")));
        }

        [Test]
        public void Teen_IsNonPersonalized_EvenIfAccepted_AndRatedG()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Teen, ads: true);

            Assert.That(rig.Port.HasUserConsent, Is.False, "13-17: npa (biz_under18_no_personalization)");
            Assert.That(rig.Port.DoNotSell, Is.True);
            Assert.That(rig.Port.Calls.IndexOf("consent:false"), Is.LessThan(rig.Port.Calls.IndexOf("init")));
            Assert.That(rig.Max.Consent!.MaxAdContentRating, Is.EqualTo("G"), "D-029");
            Assert.That(rig.Max.Consent.NonPersonalized, Is.True);
        }

        [Test]
        public void UmpProfile_Adult_LetsUmpDecide()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: false, profile: LegalProfiles.Ump);

            Assert.That(rig.Port.HasUserConsent, Is.Null, "en el EEE/UK/CH lo decide el mensaje de UMP (08 §7 #2)");
            Assert.That(rig.Port.DoNotSell, Is.Null);
            Assert.That(rig.Port.Initializations, Is.EqualTo(1));
            Assert.That(rig.Max.Consent!.UmpDecides, Is.True);
        }

        [Test]
        public void UmpProfile_Teen_NeverInitializes_QMax6b()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Teen, ads: true, profile: LegalProfiles.Ump, initialize: false);

            Assert.That(rig.Port.Initializations, Is.EqualTo(0), "Q-MAX-6 (b): con 13-17 y UMP, sin MAX (no se enseña UMP a un menor)");
            Assert.That(rig.Port.Calls, Is.Empty);
            Assert.That(rig.Max.Consent!.MayInitialize, Is.False);
            Assert.That(rig.Max.Consent.ChildDirected, Is.False);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False);
            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
        }

        [Test]
        public void UmpProfile_Teen_EvenWithoutTheUnder18Switch_NeverInitializes()
        {
            var config = ConsentConfig.Defaults;
            config.BizUnder18NoPersonalization = false;
            ConsentFlags flags = ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.Ump, 1, At, true, true, AgeBand.Teen), config);
            Assert.That(flags.AdsByUmp, Is.True, "sin el interruptor, 13-17 con UMP como 18+");

            Assert.That(AdConsentSettings.From(flags).MayInitialize, Is.False);
        }

        [Test]
        public void LateReward_WithinTheGrace_IsRewarded()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden);
            rig.Max.Tick();
            Assert.That(outcome, Is.Null, "espera la recompensa RewardGrace");

            rig.Clock.Advance(TimeSpan.FromSeconds(1));
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.RewardReceived);
            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded));
        }

        [Test]
        public void NoRewardAfterTheGrace_IsSkipped()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden);
            rig.Clock.Advance(MaxAdService.RewardGrace);
            rig.Max.Tick();
            Assert.That(outcome, Is.EqualTo(AdOutcome.Skipped));

            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.RewardReceived); // demasiado tarde: no hay anuncio pendiente
            Assert.That(outcome, Is.EqualTo(AdOutcome.Skipped));
        }

        [Test]
        public void DuplicatedHidden_GrantsOnlyOnce()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            int rewarded = 0;
            rig.Consent.Show(AdPlacement.Hint, o => { if (o == AdOutcome.Rewarded) rewarded++; });
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.RewardReceived);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.RewardReceived);
            rig.Clock.Advance(TimeSpan.FromSeconds(10));
            rig.Max.Tick();

            Assert.That(rewarded, Is.EqualTo(1));
        }

        [Test]
        public void ResumeWithoutTheCloseCallback_ReleasesTheAd()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.RewardReceived);
            rig.Max.OnResume();
            rig.Max.Tick();
            Assert.That(outcome, Is.Null, "MAX aún puede avisar");

            rig.Clock.Advance(MaxAdService.ResumeGrace);
            rig.Max.Tick();
            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded), "la recompensa ya había llegado");
            Assert.That(rig.Consent.IsShowing, Is.False);

            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden); // el cierre tardío no cuenta otra vez
            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded));
        }

        [Test]
        public void ResumeWithoutReward_IsSkipped_AndResumeWithoutAnAd_DoesNothing()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Max.OnResume();
            rig.Max.Tick();
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Max.OnResume();
            rig.Clock.Advance(MaxAdService.ResumeGrace);
            rig.Max.Tick();
            Assert.That(outcome, Is.EqualTo(AdOutcome.Skipped));
        }

        [Test]
        public void ChangingConsentLater_UpdatesFlags_WithoutSecondInitialize()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Consent.Apply(FlagsFor(AgeBand.Adult, ads: false));

            Assert.That(rig.Port.Initializations, Is.EqualTo(1));
            Assert.That(rig.Port.HasUserConsent, Is.False);
            Assert.That(rig.Port.DoNotSell, Is.True);
        }

        [Test]
        public void AgeCorrectedToUnder13_AfterInit_StopsAds()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);
            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.True);

            rig.Consent.Apply(FlagsFor(AgeBand.Under13, ads: true));

            Assert.That(rig.Consent.IsReady(AdPlacement.Hint), Is.False);
            Assert.That(rig.Port.HasUserConsent, Is.False);
            Assert.That(rig.Port.DoNotSell, Is.True);
        }

        [Test]
        public void Interstitial_IsPreloaded_OnlyNearTheFirstInterstitialLevel()
        {
            var rig = new Rig { };
            rig.NextLevel = 10;
            rig.Decide(AgeBand.Adult, ads: true);
            Assert.That(rig.Port.Loads(MaxAdKind.Interstitial), Is.EqualTo(0), "lejos del nivel 22, sin intersticial");

            rig.NextLevel = 21; // ads_first_interstitial_level - 1
            rig.Consent.IsReady(AdPlacement.Hint);
            Assert.That(rig.Port.Loads(MaxAdKind.Interstitial), Is.EqualTo(1));
        }

        [Test]
        public void LoadFailure_RetriesWithBackoff()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.LoadFailed);

            rig.Consent.IsReady(AdPlacement.Hint);
            Assert.That(rig.Port.Loads(MaxAdKind.Rewarded), Is.EqualTo(1), "espera 2 s");

            rig.Clock.Advance(TimeSpan.FromSeconds(2));
            rig.Consent.IsReady(AdPlacement.Hint);
            Assert.That(rig.Port.Loads(MaxAdKind.Rewarded), Is.EqualTo(2));
            Assert.That(MaxAdService.Backoff(1), Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(MaxAdService.Backoff(3), Is.EqualTo(TimeSpan.FromSeconds(8)));
            Assert.That(MaxAdService.Backoff(20), Is.EqualTo(TimeSpan.FromSeconds(64)));
        }

        [Test]
        public void Rewarded_SetsUserIdAndCustomData_ForSsv()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.ExtraCanoe, o => outcome = o);

            Assert.That(rig.Port.UserId, Is.EqualTo(Uid), "SetUserId(UID) (08 §10.2)");
            Assert.That(rig.Port.LastCustomData, Is.EqualTo("rv_extra_canoe|" + Nonce), "custom_data = placement|nonce");
            Assert.That(rig.Port.LastPlacement, Is.EqualTo("rv_extra_canoe"));
            Assert.That(rig.Port.Calls.IndexOf("user"), Is.LessThan(rig.Port.Calls.IndexOf("show:rewarded:rv_extra_canoe")));
            Assert.That(rig.Max.LastRewardNonce, Is.EqualTo(Nonce));
            Assert.That(outcome, Is.Null, "hasta que se cierra");
            Assert.That(rig.Consent.IsShowing, Is.True);
        }

        [Test]
        public void Rewarded_WithoutUid_DoesNotSetUserId()
        {
            var rig = new Rig { Uid = null };
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);
            rig.Consent.Show(AdPlacement.Hint, _ => { });

            Assert.That(rig.Port.Calls, Does.Not.Contain("user"));
            Assert.That(rig.Port.LastCustomData, Is.EqualTo("rv_hint|" + Nonce));
        }

        [Test]
        public void Reward_IsGrantedOnlyWithTheRewardCallback()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.RewardReceived);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden);

            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded), "concesión provisional (08 §10.2)");
            Assert.That(rig.Consent.IsShowing, Is.False);
            Assert.That(rig.Port.Loads(MaxAdKind.Rewarded), Is.EqualTo(2), "carga el siguiente al cerrar");
        }

        [Test]
        public void ClosedWithoutReward_IsSkipped()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Hidden);
            rig.Clock.Advance(MaxAdService.RewardGrace); // el margen por si la recompensa llega tarde
            rig.Max.Tick();

            Assert.That(outcome, Is.EqualTo(AdOutcome.Skipped));
        }

        [Test]
        public void DisplayFailure_IsFailed_AndInterstitialHiddenIsShown()
        {
            var rig = new Rig { };
            rig.NextLevel = 30;
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.Loaded);
            rig.Port.Raise(MaxAdKind.Interstitial, MaxAdEvent.Loaded);

            AdOutcome? rewarded = null;
            rig.Consent.Show(AdPlacement.Hint, o => rewarded = o);
            rig.Port.Raise(MaxAdKind.Rewarded, MaxAdEvent.DisplayFailed);
            Assert.That(rewarded, Is.EqualTo(AdOutcome.Failed));

            AdOutcome? interstitial = null;
            rig.Consent.Show(AdPlacement.InterstitialPostLevel, o => interstitial = o);
            Assert.That(rig.Port.LastCustomData, Is.EqualTo("rv_hint|" + Nonce), "el intersticial no lleva custom_data");
            rig.Port.Raise(MaxAdKind.Interstitial, MaxAdEvent.Hidden);
            Assert.That(interstitial, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void NotLoaded_IsNoFill()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);

            AdOutcome? outcome = null;
            rig.Consent.Show(AdPlacement.Hint, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
        }

        [Test]
        public void AdsSwitchedOff_LoadNothing()
        {
            var config = AdsConfig.Defaults;
            config.BizAdsEnabled = false;
            var port = new FakeMaxSdkPort();
            var max = new MaxAdService(port, new ManualClock(At), () => null, () => 50, () => config, () => Nonce);
            max.ApplyConsent(AdConsentSettings.From(FlagsFor(AgeBand.Adult, true)));
            port.CompleteInitialization();

            Assert.That(port.Loads(MaxAdKind.Rewarded) + port.Loads(MaxAdKind.Interstitial), Is.EqualTo(0));
        }

        [Test]
        public void Revenue_IsReportedAsImpression()
        {
            var rig = new Rig();
            var seen = new List<AdImpression>();
            rig.Consent.Impression += seen.Add;
            rig.Decide(AgeBand.Adult, ads: true);
            rig.Port.RaiseRevenue(MaxAdKind.Interstitial, "AppLovin", 0.0123);

            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].AdPlatform, Is.EqualTo("appLovin"));
            Assert.That(seen[0].AdSource, Is.EqualTo("AppLovin"));
            Assert.That(seen[0].AdFormat, Is.EqualTo("interstitial"));
            Assert.That(seen[0].Placement, Is.EqualTo(AdPlacement.InterstitialPostLevel));
            Assert.That(seen[0].Currency, Is.EqualTo("USD"));
        }

        [Test]
        public void PrivacyOptions_ReachTheCmp_OnlyWithUmp()
        {
            var rig = new Rig();
            rig.Decide(AgeBand.Adult, ads: true);
            Assert.That(rig.Consent.ShowPrivacyOptions(), Is.False);
            Assert.That(rig.Port.Calls, Does.Not.Contain("cmp"));

            var ump = new Rig();
            ump.Decide(AgeBand.Adult, ads: false, profile: LegalProfiles.Ump);
            Assert.That(ump.Consent.ShowPrivacyOptions(), Is.True);
            Assert.That(ump.Port.Calls, Does.Contain("cmp"));

            ump.Port.HasCmp = false;
            Assert.That(ump.Consent.ShowPrivacyOptions(), Is.False, "devuelve lo que diga el proveedor");
        }

        [Test]
        public void CustomData_FollowsTheContract()
        {
            Assert.That(AdRewardCustomData.IsNonce(AdRewardCustomData.NewNonce()), Is.True);
            Assert.That(AdRewardCustomData.Format(AdPlacement.DoubleHarvest, Nonce), Is.EqualTo("rv_double_harvest|" + Nonce));
            Assert.Throws<ArgumentException>(() => AdRewardCustomData.Format(AdPlacement.InterstitialPostLevel, Nonce));
            Assert.Throws<ArgumentException>(() => AdRewardCustomData.Format(AdPlacement.Hint, "NOT-A-NONCE"));
        }

        [Test]
        public void ConsentSettings_MapEveryBand()
        {
            Assert.That(AdConsentSettings.From(ConsentPolicy.Undecided).MayInitialize, Is.False);
            AdConsentSettings under13 = AdConsentSettings.From(FlagsFor(AgeBand.Under13, true));
            Assert.That(under13.MayInitialize, Is.False);
            Assert.That(under13.ChildDirected, Is.True);
            AdConsentSettings teen = AdConsentSettings.From(FlagsFor(AgeBand.Teen, true));
            Assert.That(teen.MayInitialize && teen.NonPersonalized, Is.True);
            AdConsentSettings adult = AdConsentSettings.From(FlagsFor(AgeBand.Adult, true));
            Assert.That(adult.HasUserConsent, Is.True);
            Assert.That(adult.DoNotSell, Is.False);
            Assert.That(adult.MaxAdContentRating, Is.EqualTo("G"));
        }
    }
}
