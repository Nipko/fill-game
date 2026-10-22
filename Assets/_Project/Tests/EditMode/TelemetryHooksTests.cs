#nullable enable
using System.Collections.Generic;
using AF.Analytics;
using AF.Economy;
using AF.Events;
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los ganchos del bloque B7 (parches B7-wallet y B7-levelflow): <c>Wallet.Apply</c> publica <c>SeedsEarned</c> y
    /// <c>SeedsSpent</c>; <c>LevelFlow</c>, el inicio, la victoria, el fallo y el abandono del nivel; <c>biz_feature_lives</c>
    /// apaga las canoas; y <see cref="AnalyticsService"/> los convierte en los eventos de 08 §8.2 sin violar el catálogo.
    /// </summary>
    public sealed class TelemetryHooksTests
    {
        private sealed class Recorder
        {
            public readonly List<string> Seen = new List<string>();
            public LevelStarted? Started;
            public LevelWon? Won;
            public LevelFailed? Failed;

            public Recorder(EventBus bus)
            {
                bus.Subscribe<SeedsEarned>(e => Seen.Add("earn:" + e.Source + ":" + e.Value + ":" + e.Balance));
                bus.Subscribe<SeedsSpent>(e => Seen.Add("spend:" + e.ItemName + ":" + e.Value + ":" + e.Balance));
                bus.Subscribe<LevelStarted>(e =>
                {
                    Started = e;
                    Seen.Add("start:" + e.Level);
                });
                bus.Subscribe<LevelWon>(e =>
                {
                    Won = e;
                    Seen.Add("win:" + e.Level);
                });
                bus.Subscribe<LevelFailed>(e =>
                {
                    Failed = e;
                    Seen.Add("fail:" + (e.Hard ? "hard" : "soft") + ":" + e.Level);
                });
                bus.Subscribe<LevelAbandoned>(e => Seen.Add("abandon:" + e.Reason + ":" + e.Level));
            }
        }

        [Test]
        public void WalletApply_PublishesEarnAndSpend_WithSourceAndSink()
        {
            var fx = new EconomyFixture(seeds: 500);
            var bus = new EventBus();
            var rec = new Recorder(bus);
            fx.Wallet.Events = bus;
            Assert.That(fx.Wallet.Apply(Transaction.Win(12, RewardTier.Hard)).IsApplied, Is.True);
            Assert.That(fx.Wallet.Apply(Transaction.Buy(SeedSink.UndoRefill)).IsApplied, Is.True);
            Assert.That(fx.Wallet.Apply(Transaction.Earn(SeedSource.Basket, 50)).IsApplied, Is.True);
            Assert.That(fx.Wallet.Apply(Transaction.Rescue(31, 300)).IsApplied, Is.True);
            Assert.That(fx.Wallet.Apply(Transaction.GrantStock(BoosterKind.Hint, 1)).IsApplied, Is.True, "sin semillas: nada");
            Assert.That(fx.Wallet.Apply(Transaction.Buy(SeedSink.Shuffle)).IsApplied, Is.False, "rechazada: nada");
            Assert.That(rec.Seen, Is.EqualTo(new[]
            {
                "earn:hard:60:560", "spend:undo_refill:30:530", "earn:basket:50:580", "spend:continue_cuia:300:280",
            }));
        }

        [Test]
        public void LivesOff_TheCanoesDoNotCount()
        {
            var fx = new EconomyFixture();
            for (int i = 0; i < 5; i++)
            {
                fx.Wallet.Apply(Transaction.HardFail(31)); // 5/5 → 0/5
            }

            Assert.That(fx.Wallet.CanStartLevel(31), Is.False, "con canoas, a 0/5 no se empieza el 31");
            fx.Wallet.LivesEnabled = false;
            Assert.That(fx.Wallet.CanoesActive(31), Is.False);
            Assert.That(fx.Wallet.CanStartLevel(31), Is.True, "biz_feature_lives = false: como antes del 31");
        }

        [Test]
        public void LevelFlow_PublishesStartWinAndTheHarvest()
        {
            var rig = new StreakFlowRig(offset: 10);
            var bus = new EventBus();
            var rec = new Recorder(bus);
            rig.Flow.Events = bus;
            rig.Economy.Wallet.Events = bus;
            rig.Start(12);
            Assert.That(rec.Started.HasValue, Is.True);
            LevelStarted s = rec.Started!.Value;
            Assert.That(s.LevelName, Is.EqualTo(rig.Flow.Level.Id));
            Assert.That(s.Variant, Is.EqualTo("base"));
            Assert.That(s.Attempt, Is.EqualTo(1));
            Assert.That(s.Band, Is.EqualTo(rig.Flow.Level.Band ?? string.Empty), "difficulty.band (Q-B7-23 a)");
            LevelStateTests.Win(rig.Flow);
            LevelWon w = rec.Won!.Value;
            Assert.That(w.Level, Is.EqualTo(12));
            Assert.That(w.Pours, Is.GreaterThan(0));
            Assert.That(w.RewardSeeds, Is.EqualTo(rig.Flow.HarvestSeeds));
            Assert.That(w.Rescued, Is.False);
            Assert.That(w.BoostersJson, Is.EqualTo("{}"));
            Assert.That(rec.Seen, Is.EqualTo(new[] { "start:12", "earn:win:" + rig.Flow.HarvestSeeds + ":" + rig.Economy.Wallet.Seeds, "win:12" }));
            Assert.That(rig.Flow.PublishAbandonIfUnresolved("map"), Is.False, "ganado: no es abandono");
        }

        [Test]
        public void LevelFlow_PublishesTheHardFail_AndTheAbandon()
        {
            var rig = new StreakFlowRig(offset: 4);
            var bus = new EventBus();
            var rec = new Recorder(bus);
            rig.Flow.Events = bus;
            rig.Start(10);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.Restart(), Is.True, "el pie del modal en 1-30: fallo duro sin canoa");
            LevelFailed f = rec.Failed!.Value;
            Assert.That(f.Hard, Is.True);
            Assert.That(f.CanoeLost, Is.False);
            Assert.That(f.Pours, Is.GreaterThan(0));
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rec.Started!.Value.Attempt, Is.EqualTo(2), "el segundo intento");
            Assert.That(rig.Flow.PublishAbandonIfUnresolved("map"), Is.True);
            Assert.That(rec.Seen, Is.EqualTo(new[] { "start:10", "fail:hard:10", "start:10", "abandon:map:10" }));
        }

        [Test]
        public void TheAnalytics_TurnsTheFlowIntoThe08_2Events()
        {
            var rig = new StreakFlowRig(offset: 10);
            var bus = new EventBus();
            var analytics = new AnalyticsService(bus);
            var sink = new FakeAnalyticsSink();
            analytics.AddSink(sink);
            rig.Flow.Events = bus;
            rig.Economy.Wallet.Events = bus;
            rig.Start(12);
            LevelStateTests.Win(rig.Flow);
            Assert.That(sink.Names, Is.EqualTo(new[] { "level_start", "earn_virtual_currency", "af_level_win", "level_end" }));
            Assert.That(sink.Last("level_end")!.Find("level_name")!.Value.Text, Is.EqualTo(rig.Flow.Level.Id));
            Assert.That(analytics.Violations, Is.Empty);
        }

        [Test]
        public void WithoutABus_NothingChanges()
        {
            var rig = new StreakFlowRig(offset: 10);
            rig.Start(12);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.PublishAbandonIfUnresolved("map"), Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(rig.Flow.HarvestSeeds + rig.Flow.LastStreakResult!.BasketSeeds));
        }
    }
}
