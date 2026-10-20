#nullable enable
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Cosecha doble del río crecido (enganche E35, parche <c>S4-wallet</c>; 04 §2.8; 05 §2.4; nota
    /// <c>docs/dev/crecida-canasta.md</c> C8 y Q-C14 (a) [P]): <see cref="Transaction.DoubleHarvestRiver"/> no pasa por la compuerta
    /// <c>ads_rv_double_harvest_from_level</c>, sale con <c>source</c> = <c>double_harvest</c> y no gasta el tope del vídeo
    /// (<c>ads_rv_cap_double_harvest</c>).
    /// </summary>
    public sealed class RiverDoubleHarvestTests
    {
        private static EconomyConfig GateAt(int level)
        {
            EconomyConfig config = EconomyConfig.Defaults;
            config.AdsRvDoubleHarvestFromLevel = level;
            return config;
        }

        [Test]
        public void RiverDouble_IsAcceptedBelowTheVideoGate_AndTheVideoOneIsNot()
        {
            var fx = new EconomyFixture(config: GateAt(40));

            TransactionResult video = fx.Wallet.Apply(Transaction.DoubleHarvest(25, RewardTier.Hard));
            Assert.That(video.Reason, Is.EqualTo(RejectReason.NotUnlocked), "el x2 por vídeo sigue con su compuerta");

            TransactionResult river = fx.Wallet.Apply(Transaction.DoubleHarvestRiver(25, RewardTier.Hard));
            Assert.That(river.IsApplied, Is.True);
            Assert.That(river.SeedsDelta, Is.EqualTo(60), "dobla la Cosecha de la etiqueta (Difícil: 60)");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(60));
            Assert.That(river.Transaction.RiverFull, Is.True);
            Assert.That(Transaction.DoubleHarvest(25, RewardTier.Hard).RiverFull, Is.False);
        }

        [Test]
        public void RiverDouble_IsEmittedWithSourceDoubleHarvest()
        {
            var fx = new EconomyFixture();
            var seen = new List<string>();
            fx.Wallet.Changed += r => seen.Add(r.Transaction.Source.ToId());

            TransactionResult river = fx.Wallet.Apply(Transaction.DoubleHarvestRiver(30, RewardTier.Normal));

            Assert.That(river.Transaction.Kind, Is.EqualTo(TransactionKind.DoubleHarvest));
            Assert.That(river.Transaction.Source.ToId(), Is.EqualTo("double_harvest"));
            Assert.That(seen, Is.EqualTo(new[] { "double_harvest" }));
        }

        [Test]
        public void RiverDouble_DoesNotSpendTheVideoCap()
        {
            var fx = new EconomyFixture();
            fx.Profile.Daily.DateLocal = fx.Clock.LocalDateKey();
            fx.Profile.Daily.RvDoubleHarvest = 4; // ads_rv_cap_double_harvest = 5
            fx.Profile.Daily.RvTotal = 4;

            for (int i = 0; i < 3; i++)
            {
                Assert.That(fx.Wallet.Apply(Transaction.DoubleHarvestRiver(30 + i, RewardTier.Normal)).IsApplied, Is.True);
            }

            Assert.That(fx.Profile.Daily.RvDoubleHarvest, Is.EqualTo(4), "el tope del vídeo es de AF.Ads y no se toca");
            Assert.That(fx.Profile.Daily.RvTotal, Is.EqualTo(4));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(60));
        }

        [Test]
        public void StreakService_PaysTheRiverDouble_EvenWithTheVideoGateAboveTheLevel()
        {
            var eco = new EconomyFixture(config: GateAt(40));
            var record = new StreakRecord { DateLocal = eco.Clock.LocalDateKey(), Wins = 1, RiverFull = true, ShieldAvailable = true };
            var store = new InMemoryStreakStore(eco.Clock, record);
            var service = new StreakService(store, eco.Wallet, eco.Clock, 10);

            StreakServiceResult r = service.ApplyWin(26, true, 25, RewardTier.Normal);

            Assert.That(r.FreeDoubleHarvest, Is.True);
            Assert.That(r.DoubleHarvest!.IsApplied, Is.True, "Q-C14 (a): sin la compuerta del vídeo");
            Assert.That(r.DoubleHarvest.Transaction.RiverFull, Is.True);
            Assert.That(r.DoubleHarvestSeeds, Is.EqualTo(20));
            Assert.That(eco.Wallet.Seeds, Is.EqualTo(20));
        }
    }
}
