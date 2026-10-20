#nullable enable
using System;
using AF.Economy;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Enganche E9: la Cosecha doble del Reto del río (<see cref="Transaction.DoubleHarvestDaily"/>; 04 §2.8; 06 §4.1, «Premio»).
    /// Paga otra vez las semillas del premio del día con <c>source</c> = <c>double_harvest</c>, se rechaza antes de
    /// <c>ads_rv_double_harvest_from_level</c> (11) sin tocar nada, y la de campaña sigue doblando la Cosecha de la etiqueta.
    /// </summary>
    public sealed class DailyDoubleHarvestTests
    {
        [Test]
        public void DoubleHarvestDaily_PaysThePrizeAgain_WithDoubleHarvestSource()
        {
            var fx = new EconomyFixture();
            fx.Wallet.Apply(Transaction.Earn(SeedSource.DailyChallenge, 60));

            TransactionResult doubled = fx.Wallet.Apply(Transaction.DoubleHarvestDaily(60, 26));

            Assert.That(doubled.IsApplied, Is.True);
            Assert.That(doubled.SeedsDelta, Is.EqualTo(60), "60 → 120 [P]");
            Assert.That(doubled.Transaction.Kind, Is.EqualTo(TransactionKind.DoubleHarvest));
            Assert.That(doubled.Transaction.Source.ToId(), Is.EqualTo("double_harvest"));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(120));
        }

        [Test]
        public void DoubleHarvestDaily_Before11_IsRejectedWithoutEffects()
        {
            var fx = new EconomyFixture();
            fx.Wallet.Apply(Transaction.Earn(SeedSource.DailyChallenge, 60));
            string before = fx.Json;

            TransactionResult early = fx.Wallet.Apply(Transaction.DoubleHarvestDaily(60, 10));

            Assert.That(early.Status, Is.EqualTo(TransactionStatus.Rejected));
            Assert.That(early.Reason, Is.EqualTo(RejectReason.NotUnlocked));
            Assert.That(fx.Json, Is.EqualTo(before));
            Assert.That(fx.Wallet.Apply(Transaction.DoubleHarvestDaily(60, 11)).IsApplied, Is.True, "desde el 11");
        }

        [Test]
        public void DoubleHarvestDaily_RefusesNonPositiveSeedsAndLevels()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.DoubleHarvestDaily(0, 26));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.DoubleHarvestDaily(-60, 26));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.DoubleHarvestDaily(60, 0));
        }

        [Test]
        public void CampaignDoubleHarvest_StillDoublesTheLabelHarvest()
        {
            var fx = new EconomyFixture();

            TransactionResult doubled = fx.Wallet.Apply(Transaction.DoubleHarvest(11, RewardTier.VeryHard));

            Assert.That(doubled.SeedsDelta, Is.EqualTo(80), "80 → 160 (04 §2.8)");
            Assert.That(doubled.Transaction.Seeds, Is.EqualTo(0));
        }
    }
}
