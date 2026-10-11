#nullable enable
using System;
using AF.Save;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Ads.Tests
{
    /// <summary>
    /// Tabla de casos de B1-3 (05 §6.1-§6.3 con D-008; 04 §3): cuándo sale un intersticial y cuándo se puede ver un
    /// recompensado, con los defaults de 08 §9 y el reloj manual.
    /// </summary>
    public sealed class AdPolicyTests
    {
        // ------------------------------------------------------------------ intersticial

        [Test]
        public void Level21_NeverShowsAnInterstitial()
        {
            var fx = new AdsFixture();
            fx.Win(21);

            Assert.That(fx.Policy.CheckInterstitial(21, AdContext.VictoryScreen).Reason, Is.EqualTo(AdBlockReason.BelowFirstLevel));
            Assert.That(fx.Interstitial(21).Outcome, Is.EqualTo(AdOutcome.Blocked));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0));
        }

        [Test]
        public void Level22_InTheInstallSession_ShowsNone_AndTheNextSessionDoes()
        {
            var fx = new AdsFixture(installSession: true);
            fx.Win(22);

            Assert.That(fx.Policy.CheckInterstitial(22, AdContext.VictoryScreen).Reason, Is.EqualTo(AdBlockReason.InstallSession));

            fx.NewSession();
            Assert.That(fx.Policy.CheckInterstitial(22, AdContext.VictoryScreen).IsAllowed, Is.True);
        }

        [Test]
        public void From22To60_OneEveryThreeVictories()
        {
            var fx = new AdsFixture();
            fx.Win(22);
            AdResult first = fx.Interstitial(22);
            Assert.That(first.Outcome, Is.EqualTo(AdOutcome.Shown), "primer intersticial en la victoria del 22");
            Assert.That(fx.Presenter.Last!.Placement, Is.EqualTo(AdPlacement.InterstitialPostLevel));
            Assert.That(fx.Profile.Ads.LevelsSinceInterstitial, Is.EqualTo(0));

            fx.Clock.Advance(TimeSpan.FromMinutes(10));
            for (int level = 23; level <= 24; level++)
            {
                fx.Win(1);
                Assert.That(fx.Interstitial(level).Reason, Is.EqualTo(AdBlockReason.Cadence), "victoria " + (level - 22));
            }

            fx.Win(1);
            Assert.That(fx.Interstitial(25).Outcome, Is.EqualTo(AdOutcome.Shown), "3.ª victoria tras el anterior");
            Assert.That(fx.Profile.Daily.InterstitialsToday, Is.EqualTo(2));
        }

        [Test]
        public void From61_OneEveryTwoVictories()
        {
            var fx = new AdsFixture();
            fx.Win(1);
            Assert.That(fx.Interstitial(61).Reason, Is.EqualTo(AdBlockReason.Cadence));
            fx.Win(1);
            Assert.That(fx.Interstitial(61).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void LessThan150sSinceThePrevious_IsBlocked()
        {
            var fx = new AdsFixture();
            fx.Win(22);
            fx.Interstitial(22);
            fx.Win(3);

            fx.Clock.Advance(TimeSpan.FromSeconds(149));
            Assert.That(fx.Interstitial(25).Reason, Is.EqualTo(AdBlockReason.MinGap));
            fx.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.That(fx.Interstitial(25).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void TheFirst120sOfASession_AreAWarmup()
        {
            var fx = new AdsFixture(warmedUp: false);
            fx.Win(22);

            fx.Clock.Advance(TimeSpan.FromSeconds(119));
            Assert.That(fx.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.Warmup));
            fx.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.That(fx.Interstitial(22).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void SixPerSession_AndTwelvePerLocalDay()
        {
            var fx = new AdsFixture();
            for (int i = 0; i < 6; i++)
            {
                Assert.That(fx.NextDueInterstitial(30).Outcome, Is.EqualTo(AdOutcome.Shown), "sesión 1, n.º " + (i + 1));
            }

            Assert.That(fx.NextDueInterstitial(30).Reason, Is.EqualTo(AdBlockReason.SessionCap));

            fx.NewSession();
            for (int i = 0; i < 6; i++)
            {
                Assert.That(fx.NextDueInterstitial(30).Outcome, Is.EqualTo(AdOutcome.Shown), "sesión 2, n.º " + (i + 1));
            }

            fx.NewSession();
            Assert.That(fx.NextDueInterstitial(30).Reason, Is.EqualTo(AdBlockReason.DailyCap));
            Assert.That(fx.Saved().Daily.InterstitialsToday, Is.EqualTo(12));

            // Medianoche en Bogotá (05:00 UTC del día siguiente): el tope diario vuelve a 0.
            fx.Clock.Set(new DateTime(2026, 10, 9, 5, 0, 0, DateTimeKind.Utc));
            fx.NewSession();
            Assert.That(fx.NextDueInterstitial(30).Outcome, Is.EqualTo(AdOutcome.Shown));
            Assert.That(fx.Profile.Daily.InterstitialsToday, Is.EqualTo(1));
            Assert.That(fx.Profile.Daily.DateLocal, Is.EqualTo("2026-10-09"));
        }

        [Test]
        public void For24hAfterAnyPurchase_NoInterstitial()
        {
            var fx = new AdsFixture();
            fx.Win(22);
            fx.Profile.Ads.LastIap = fx.Clock.UtcNow;

            fx.Clock.Advance(TimeSpan.FromHours(24) - TimeSpan.FromSeconds(1));
            Assert.That(fx.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.AfterPurchase));
            fx.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.That(fx.Interstitial(22).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void LessThan60sAfterARewarded_NoInterstitial()
        {
            var fx = new AdsFixture();
            fx.Win(22);
            Assert.That(fx.Rewarded(AdPlacement.DoubleHarvest).IsRewarded, Is.True);

            fx.Clock.Advance(TimeSpan.FromSeconds(59));
            Assert.That(fx.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.AfterRewarded));
            fx.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.That(fx.Interstitial(22).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [TestCase(AdContext.LevelStart)]
        [TestCase(AdContext.Retry)]
        [TestCase(AdContext.Restart)]
        [TestCase(AdContext.StuckModal)]
        [TestCase(AdContext.GiveUpConfirm)]
        [TestCase(AdContext.LevelFailed)]
        [TestCase(AdContext.NoCanoes)]
        public void OutsideTheVictoryScreen_NeverAnInterstitial(AdContext context)
        {
            var fx = new AdsFixture();
            fx.Win(40);

            Assert.That(fx.Interstitial(40, context).Reason, Is.EqualTo(AdBlockReason.WrongContext));
            Assert.That(fx.Interstitial(40, AdContext.VictoryScreen).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void NoAds_RemovesInterstitials_ButKeepsRewarded()
        {
            var fx = new AdsFixture();
            fx.Win(22);
            fx.Profile.Entitlements.NoAds = true;

            Assert.That(fx.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.NoAds));
            Assert.That(fx.Rewarded(AdPlacement.ExtraCanoe).IsRewarded, Is.True, "Sin anuncios conserva los recompensados (05 §4.1)");
        }

        [Test]
        public void InfiniteCanoes_DoNotSuppressInterstitials_D008()
        {
            var fx = new AdsFixture();
            fx.Win(31);
            fx.Profile.Wallet.Canoes.InfiniteUntil = fx.Clock.UtcNow + TimeSpan.FromHours(1);

            Assert.That(fx.Interstitial(31).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        [Test]
        public void KillSwitches_TurnOffTheirAds()
        {
            var noAds = new AdsFixture(config: new AdsConfig { BizAdsEnabled = false });
            noAds.Win(22);
            Assert.That(noAds.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.AdsDisabled));
            Assert.That(noAds.Rewarded(AdPlacement.Hint).Reason, Is.EqualTo(AdBlockReason.AdsDisabled));

            var noInterstitials = new AdsFixture(config: new AdsConfig { BizInterstitialsEnabled = false });
            noInterstitials.Win(22);
            Assert.That(noInterstitials.Interstitial(22).Reason, Is.EqualTo(AdBlockReason.InterstitialsDisabled));
            Assert.That(noInterstitials.Rewarded(AdPlacement.Hint).IsRewarded, Is.True);

            var noRewarded = new AdsFixture(config: new AdsConfig { BizRewardedEnabled = false });
            noRewarded.Win(22);
            Assert.That(noRewarded.Rewarded(AdPlacement.Hint).Reason, Is.EqualTo(AdBlockReason.RewardedDisabled));
            Assert.That(noRewarded.Interstitial(22).Outcome, Is.EqualTo(AdOutcome.Shown));
        }

        // ------------------------------------------------------------------ recompensados

        [TestCase(AdPlacement.DoubleHarvest, 5)]
        [TestCase(AdPlacement.UndoRefill, 4)]
        [TestCase(AdPlacement.Hint, 3)]
        [TestCase(AdPlacement.Reveal, 3)]
        [TestCase(AdPlacement.ExtraCanoe, 3)]
        public void EachRewardedPlacement_HasItsDailyCap(AdPlacement placement, int cap)
        {
            var fx = new AdsFixture();
            for (int i = 0; i < cap; i++)
            {
                Assert.That(fx.Rewarded(placement).IsRewarded, Is.True, placement.ToId() + " n.º " + (i + 1));
                fx.Clock.Advance(fx.Config.RewardedCooldown(placement));
            }

            Assert.That(fx.Rewarded(placement).Reason, Is.EqualTo(AdBlockReason.PlacementCap));
            Profile saved = fx.Saved();
            Assert.That(fx.Controller.Ledger.RewardedToday(placement), Is.EqualTo(cap));
            Assert.That(saved.Daily.RvTotal, Is.EqualTo(cap));
            Assert.That(saved.Stats.RvWatched, Is.EqualTo(cap));
            Assert.That(saved.EconomyTs, Is.EqualTo(fx.Clock.UtcNow - fx.Config.RewardedCooldown(placement)), "08 §4.3: daily cambió");

            fx.Clock.Set(new DateTime(2026, 10, 9, 5, 0, 0, DateTimeKind.Utc));
            Assert.That(fx.Rewarded(placement).IsRewarded, Is.True, "al día local siguiente vuelve a haber cupo");
        }

        [TestCase(AdPlacement.Hint, 60)]
        [TestCase(AdPlacement.ExtraCanoe, 90)]
        public void Cooldown_PerPlacement(AdPlacement placement, int seconds)
        {
            var fx = new AdsFixture();
            fx.Rewarded(placement);

            fx.Clock.Advance(TimeSpan.FromSeconds(seconds - 1));
            Assert.That(fx.Rewarded(placement).Reason, Is.EqualTo(AdBlockReason.Cooldown));
            Assert.That(fx.Rewarded(AdPlacement.DoubleHarvest).IsRewarded, Is.True, "el cooldown es de cada ubicación");
            fx.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.That(fx.Rewarded(placement).IsRewarded, Is.True);
        }

        [Test]
        public void FifteenRewardedPerDay_InTotal()
        {
            var fx = new AdsFixture();
            var plan = new[] { (AdPlacement.DoubleHarvest, 5), (AdPlacement.UndoRefill, 4), (AdPlacement.Hint, 3), (AdPlacement.Reveal, 3) };
            foreach ((AdPlacement placement, int times) in plan)
            {
                for (int i = 0; i < times; i++)
                {
                    Assert.That(fx.Rewarded(placement).IsRewarded, Is.True);
                    fx.Clock.Advance(TimeSpan.FromSeconds(90));
                }
            }

            Assert.That(fx.Controller.Ledger.RewardedTotalToday, Is.EqualTo(15));
            Assert.That(fx.Controller.Ledger.RewardedToday(AdPlacement.ExtraCanoe), Is.EqualTo(0));
            Assert.That(fx.Rewarded(AdPlacement.ExtraCanoe).Reason, Is.EqualTo(AdBlockReason.GlobalCap));
        }

        [Test]
        public void DailyChest_IsOffIn1_0()
        {
            var fx = new AdsFixture();

            Assert.That(fx.Rewarded(AdPlacement.DailyChest).Reason, Is.EqualTo(AdBlockReason.PlacementOff));
            Assert.Throws<ArgumentException>(() => fx.Policy.CheckRewarded(AdPlacement.InterstitialPostLevel));
            Assert.Throws<NotSupportedException>(() => fx.Controller.Ledger.RecordRewarded(AdPlacement.DailyChest));
        }

        [Test]
        public void AskingForARewardedEveryFrame_DoesNotAllocate()
        {
            // La bandeja pregunta en cada fotograma si hay video de Marea cuando no queda cupo (BoosterTray.Refresh; 08 §5.4):
            // los contadores del día se leen con la clave de la fecha local, que solo se crea una vez por día.
            var fx = new AdsFixture();
            Assert.That(fx.Rewarded(AdPlacement.UndoRefill).IsRewarded, Is.True, "contadores de hoy en el perfil");
            fx.Clock.Advance(fx.Config.RewardedCooldown(AdPlacement.UndoRefill));
            void Ask60()
            {
                for (int i = 0; i < 60; i++)
                {
                    fx.Controller.CheckRewarded(AdPlacement.UndoRefill);
                }
            }

            Assert.That(fx.Controller.CheckRewarded(AdPlacement.UndoRefill).IsAllowed, Is.True);
            Assert.That(Ask60, Is.Not.AllocatingGCMemory(), "60 preguntas en el mismo día");

            // Pasada la medianoche local (Bogotá, UTC−5), el día nuevo crea su clave una vez y los contadores vuelven a 0.
            fx.Clock.Set(new DateTime(2026, 10, 9, 5, 0, 0, DateTimeKind.Utc));
            Assert.That(fx.Controller.Ledger.RewardedToday(AdPlacement.UndoRefill), Is.EqualTo(0), "día nuevo");
            Assert.That(Ask60, Is.Not.AllocatingGCMemory(), "60 preguntas el día siguiente");
            Assert.That(fx.Controller.CheckRewarded(AdPlacement.UndoRefill).IsAllowed, Is.True);
        }

        [Test]
        public void ARewardedClosedEarly_PaysNothing_ButStartsTheCooldown()
        {
            var fx = new AdsFixture();
            fx.Presenter.Close = SimulatedAdClose.ClosedEarly;

            AdResult result = fx.Rewarded(AdPlacement.UndoRefill);

            Assert.That(result.Outcome, Is.EqualTo(AdOutcome.Skipped));
            Assert.That(result.IsRewarded, Is.False);
            Assert.That(fx.Profile.Daily.RvUndoRefill, Is.EqualTo(0));
            Assert.That(fx.Profile.Stats.RvWatched, Is.EqualTo(0));
            fx.Presenter.Close = SimulatedAdClose.Completed;
            Assert.That(fx.Rewarded(AdPlacement.UndoRefill).Reason, Is.EqualTo(AdBlockReason.Cooldown));
        }
    }
}
