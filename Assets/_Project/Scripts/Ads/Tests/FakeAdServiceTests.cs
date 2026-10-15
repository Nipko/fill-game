#nullable enable
using System;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// Anuncios simulados (B1-3): pantalla «Anuncio simulado» con «Cerrar» a los 5 s y premio a los 3 s, modos
    /// <c>fill</c>, <c>no_fill</c> y error, un anuncio a la vez, y lo que se anota y guarda en el perfil.
    /// </summary>
    public sealed class FakeAdServiceTests
    {
        [Test]
        public void Fill_ShowsTheSimulatedScreen_WithItsTimes()
        {
            var fx = new AdsFixture();
            fx.Win(22);

            fx.Interstitial(22);
            SimulatedAd interstitial = fx.Presenter.Last!;
            Assert.That(interstitial.Format, Is.EqualTo(AdFormat.Interstitial));
            Assert.That(interstitial.CloseEnabledAfter, Is.EqualTo(TimeSpan.FromSeconds(5)), "05 §6.1: cierre a los 5 s");
            Assert.That(interstitial.RewardAfter, Is.Null);

            fx.Rewarded(AdPlacement.ExtraCanoe);
            SimulatedAd rewarded = fx.Presenter.Last!;
            Assert.That(rewarded.Placement.ToId(), Is.EqualTo("rv_extra_canoe"));
            Assert.That(rewarded.RewardAfter, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(SimulatedAd.LabelKey, Is.EqualTo("ad_simulated"), "el rótulo, por clave (D-018): el texto lo pone la UI");
            Assert.That(fx.Presenter.Presented, Is.EqualTo(2));
        }

        [Test]
        public void NoFill_HidesTheVideoButton_AndRecordsNothing()
        {
            var fx = new AdsFixture(fill: FakeAdFill.NoFill);
            fx.Win(22);
            string before = ProfileJson.Serialize(fx.Profile);

            Assert.That(fx.Controller.CheckRewarded(AdPlacement.DoubleHarvest).Reason, Is.EqualTo(AdBlockReason.NoFill));
            Assert.That(fx.Rewarded(AdPlacement.DoubleHarvest).Reason, Is.EqualTo(AdBlockReason.NoFill));
            Assert.That(fx.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.NoFill));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0));
            Assert.That(ProfileJson.Serialize(fx.Profile), Is.EqualTo(before));
        }

        [Test]
        public void AnError_EndsTheGate_WithoutRecordingAnything()
        {
            var fx = new AdsFixture(fill: FakeAdFill.Error);
            fx.Win(22);

            Assert.That(fx.Interstitial(22).Outcome, Is.EqualTo(AdOutcome.Failed), "la AD_GATE termina igual");
            Assert.That(fx.Rewarded(AdPlacement.Hint).Outcome, Is.EqualTo(AdOutcome.Failed));
            Assert.That(fx.Profile.Daily.InterstitialsToday, Is.EqualTo(0));
            Assert.That(fx.Profile.Daily.RvTotal, Is.EqualTo(0));
            Assert.That(fx.Profile.Ads.LevelsSinceInterstitial, Is.EqualTo(22), "la cadencia sigue pendiente");
            Assert.That(fx.Controller.IsShowing, Is.False);
        }

        [Test]
        public void OneAdAtATime_AndTheResultArrivesWhenTheScreenCloses()
        {
            var presenter = new HeldAdPresenter();
            var clock = new ManualClock(AdsFixture.T0);
            Profile profile = Profile.CreateNew(clock, 5);
            var store = new MemoryProfileStore();
            var service = new FakeAdService(presenter);
            AdController ads = AdController.Create(service, profile, store, clock, false);
            AdResult? result = null;

            ads.ShowRewarded(AdPlacement.UndoRefill, r => result = r);
            Assert.That(result, Is.Null, "esperando el cierre");
            Assert.That(ads.IsShowing, Is.True);
            Assert.That(ads.CheckRewarded(AdPlacement.Hint).Reason, Is.EqualTo(AdBlockReason.Busy));
            AdResult? second = null;
            ads.ShowRewarded(AdPlacement.Hint, r => second = r);
            Assert.That(second!.Reason, Is.EqualTo(AdBlockReason.Busy));

            clock.Advance(TimeSpan.FromSeconds(3));
            presenter.Close(SimulatedAdClose.Completed);
            presenter.Close(SimulatedAdClose.Completed);

            Assert.That(result!.IsRewarded, Is.True);
            Assert.That(ads.IsShowing, Is.False);
            Assert.That(profile.Daily.RvUndoRefill, Is.EqualTo(1), "un segundo cierre no cuenta dos veces");
            Assert.That(ads.Session.LastRewardedAtFor(AdPlacement.UndoRefill), Is.EqualTo(clock.UtcNow), "al cerrarse");
        }

        [Test]
        public void WhatIsRecorded_IsSavedInTheProfile()
        {
            var fx = new AdsFixture();
            fx.Win(22);
            fx.Interstitial(22);
            fx.Clock.Advance(TimeSpan.FromMinutes(5));
            fx.Rewarded(AdPlacement.DoubleHarvest);
            fx.Win(1);

            Profile saved = fx.Saved();
            Assert.That(saved.Daily.DateLocal, Is.EqualTo("2026-10-08"));
            Assert.That(saved.Daily.InterstitialsToday, Is.EqualTo(1));
            Assert.That(saved.Daily.RvDoubleHarvest, Is.EqualTo(1));
            Assert.That(saved.Daily.RvTotal, Is.EqualTo(1));
            Assert.That(saved.Stats.RvWatched, Is.EqualTo(1));
            Assert.That(saved.Ads.LevelsSinceInterstitial, Is.EqualTo(1));
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(0), "AF.Ads no aplica recompensas: lo hace quien llama con Wallet.Apply");
        }

        [Test]
        public void ANewDay_ResetsTheWholeDailyBlock_LikeTheWallet()
        {
            var fx = new AdsFixture();
            fx.Profile.Daily.DateLocal = "2026-10-07";
            fx.Profile.Daily.RescuesToday = 1;
            fx.Profile.Daily.RvHint = 3;
            fx.Profile.Daily.AfRescueDeclinedDays = 1;

            Assert.That(fx.Controller.Ledger.RewardedToday(AdPlacement.Hint), Is.EqualTo(0), "leer no cambia el perfil");
            Assert.That(fx.Profile.Daily.RvHint, Is.EqualTo(3));

            fx.Rewarded(AdPlacement.Reveal);

            Assert.That(fx.Profile.Daily.DateLocal, Is.EqualTo("2026-10-08"));
            Assert.That(fx.Profile.Daily.RescuesToday, Is.EqualTo(0), "DailyCounters reinicia el bloque entero");
            Assert.That(fx.Profile.Daily.RvHint, Is.EqualTo(0));
            Assert.That(fx.Profile.Daily.RvReveal, Is.EqualTo(1));
            Assert.That(fx.Profile.Daily.AfRescueDeclinedDays, Is.EqualTo(1), "los rechazos de la Balsa no son del día");
        }

        [Test]
        public void AFailedWrite_StaysPending_AndFlushRetries()
        {
            var clock = new ManualClock(AdsFixture.T0);
            Profile profile = Profile.CreateNew(clock, 5);
            var store = new FlakyStore { Broken = true };
            AdController ads = AdController.Create(new FakeAdService(new ImmediateAdPresenter()), profile, store, clock, false);

            AdResult result = Show(ads, AdPlacement.Hint);

            Assert.That(result.IsRewarded, Is.True, "el anuncio se vio aunque el disco falle");
            Assert.That(ads.Ledger.PendingSave, Is.True);
            Assert.That(profile.Daily.RvHint, Is.EqualTo(1));
            Assert.That(ads.Ledger.Flush(), Is.False);

            store.Broken = false;
            Assert.That(ads.Ledger.Flush(), Is.True);
            Assert.That(store.Load().Profile!.Daily.RvHint, Is.EqualTo(1));
        }

        private static AdResult Show(AdController ads, AdPlacement placement)
        {
            AdResult? result = null;
            ads.ShowRewarded(placement, r => result = r);
            return result!;
        }

        /// <summary>Pantalla que no se cierra hasta que el test lo dice.</summary>
        private sealed class HeldAdPresenter : ISimulatedAdPresenter
        {
            private Action<SimulatedAdClose>? _onClosed;

            public void Present(SimulatedAd ad, Action<SimulatedAdClose> onClosed)
            {
                _onClosed = onClosed;
            }

            public void Close(SimulatedAdClose close)
            {
                _onClosed?.Invoke(close);
            }
        }
    }
}
