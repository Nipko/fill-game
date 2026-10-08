#nullable enable
using System;
using AF.Economy;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Canoas (B1-2; GDD 04 §5.2, §7.1 y §7.3; D-008): <c>canoes_max</c> 5, una cada <c>canoes_regen_min</c> 25 min con el
    /// reloj inyectado, un solo ciclo de regeneración como Magic Sort (ficha §4.5), activas desde <c>rescue_min_level</c>
    /// 31, Canoas infinitas que se suman y evitan gastar canoa, y Llenar canoas por <c>canoes_refill_seed_price</c>.
    /// </summary>
    public sealed class CanoesTests
    {
        private static readonly TimeSpan Regen = TimeSpan.FromMinutes(25);

        [Test]
        public void SpendAtT0_Is4Of5_AndAtT0Plus25MinIs5Of5()
        {
            var fx = new EconomyFixture();
            DateTime t0 = fx.Clock.UtcNow;

            TransactionResult fail = fx.Wallet.Apply(Transaction.HardFail(31));
            Assert.That(fail.CanoeLost, Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.EqualTo(t0 + Regen));
            Assert.That(fx.Wallet.Canoes.TimeToNext, Is.EqualTo(Regen), "«Tu próxima canoa vuelve en 25:00»");

            fx.Clock.Advance(TimeSpan.FromMinutes(6) + TimeSpan.FromSeconds(40));
            Assert.That(fx.Wallet.Canoes.TimeToNext, Is.EqualTo(TimeSpan.FromMinutes(18) + TimeSpan.FromSeconds(20)));

            fx.Clock.Set(t0 + Regen - TimeSpan.FromSeconds(1));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));

            fx.Clock.Set(t0 + Regen);
            CanoesStatus canoes = fx.Wallet.Canoes;
            Assert.That(canoes.Count, Is.EqualTo(5));
            Assert.That(canoes.IsFull, Is.True);
            Assert.That(canoes.NextRegenAt, Is.Null, "llenas: el cronómetro se para");
            Assert.That(canoes.TimeToNext, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void TwoSpends_Then50Min_Is5Of5_WithASingleRunningCycle()
        {
            var fx = new EconomyFixture();
            DateTime t0 = fx.Clock.UtcNow;

            fx.Wallet.Apply(Transaction.HardFail(31));
            fx.Clock.Advance(TimeSpan.FromMinutes(10));
            fx.Wallet.Apply(Transaction.HardFail(40));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(3));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.EqualTo(t0 + Regen), "el cronómetro no se reinicia al gastar otra");

            fx.Clock.Set(t0 + Regen);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.EqualTo(t0 + Regen + Regen), "y vuelve a empezar al llegar una");

            fx.Clock.Set(t0 + TimeSpan.FromMinutes(50));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.Null);
        }

        [Test]
        public void LongAbsence_RegeneratesWhileClosed_WithoutPassingTheMax()
        {
            var fx = new EconomyFixture();
            DateTime t0 = fx.Clock.UtcNow;
            for (int i = 0; i < 5; i++)
            {
                fx.Wallet.Apply(Transaction.HardFail(31));
            }

            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(0));
            Assert.That(fx.Wallet.Apply(Transaction.HardFail(31)).CanoeLost, Is.False, "nunca por debajo de 0");
            Assert.That(fx.Profile.Wallet.Canoes.Count, Is.EqualTo(0));

            fx.Clock.Set(t0 + TimeSpan.FromMinutes(110)); // 4 ciclos y 10 min
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.EqualTo(t0 + TimeSpan.FromMinutes(125)));

            fx.Clock.Set(t0 + TimeSpan.FromHours(30));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.Null);
        }

        [Test]
        public void Before31_HardFailIsFree_AndCanoesDoNotBlock()
        {
            var fx = new EconomyFixture();

            Assert.That(fx.Wallet.CanoesActive(30), Is.False);
            Assert.That(fx.Wallet.CanoesActive(31), Is.True);
            TransactionResult fail = fx.Wallet.Apply(Transaction.HardFail(30));

            Assert.That(fail.IsApplied, Is.True);
            Assert.That(fail.CanoeLost, Is.False);
            Assert.That(fail.Changed, Is.False);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Store.SaveCount, Is.EqualTo(0));
        }

        [Test]
        public void ZeroCanoes_BlocksStartingFrom31_UntilOneComesBack()
        {
            var fx = new EconomyFixture();
            for (int i = 0; i < 5; i++)
            {
                fx.Wallet.Apply(Transaction.HardFail(31));
            }

            Assert.That(fx.Wallet.CanStartLevel(30), Is.True, "antes del 31 no hay canoas");
            Assert.That(fx.Wallet.CanStartLevel(31), Is.False, "04 §7.1: con 0 canoas se abre Sin canoas");

            TransactionResult extra = fx.Wallet.Apply(Transaction.AddCanoe()); // Canoa extra por video
            Assert.That(extra.IsApplied, Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(1));
            Assert.That(fx.Wallet.CanStartLevel(31), Is.True);
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.EqualTo(EconomyFixture.T0 + Regen), "el ciclo sigue");
        }

        [Test]
        public void InfiniteCanoes_HardFailDoesNotSpend_AndDurationsAdd()
        {
            var fx = new EconomyFixture();
            DateTime t0 = fx.Clock.UtcNow;

            fx.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromMinutes(30))); // Balsa de rescate
            fx.Clock.Advance(TimeSpan.FromMinutes(10));
            fx.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromMinutes(60))); // af_infinite_canoes_1h
            Assert.That(fx.Wallet.Canoes.InfiniteUntil, Is.EqualTo(t0 + TimeSpan.FromMinutes(90)), "se suman (ficha §4.5)");
            Assert.That(fx.Wallet.Canoes.InfiniteRemaining, Is.EqualTo(TimeSpan.FromMinutes(80)));

            TransactionResult fail = fx.Wallet.Apply(Transaction.HardFail(31));
            Assert.That(fail.CanoeLost, Is.False);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));

            fx.Clock.Set(t0 + TimeSpan.FromMinutes(90));
            Assert.That(fx.Wallet.Canoes.IsInfinite, Is.False);
            Assert.That(fx.Wallet.Apply(Transaction.HardFail(31)).CanoeLost, Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Profile.Wallet.Canoes.InfiniteUntil, Is.Null, "las vencidas se borran al escribir");
        }

        [Test]
        public void InfiniteCanoes_LetYouStartWithZeroCanoes()
        {
            var fx = new EconomyFixture();
            for (int i = 0; i < 5; i++)
            {
                fx.Wallet.Apply(Transaction.HardFail(31));
            }

            fx.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromHours(1)));
            Assert.That(fx.Wallet.CanStartLevel(31), Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(0));
        }

        [Test]
        public void Refill_FillsTo5For200Seeds_AndNeverChargesForNothing()
        {
            var fx = new EconomyFixture(seeds: 399);
            Assert.That(fx.Wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill)).Reason, Is.EqualTo(RejectReason.CanoesFull));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(399));

            for (int i = 0; i < 3; i++)
            {
                fx.Wallet.Apply(Transaction.HardFail(31));
            }

            TransactionResult refill = fx.Wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill));
            Assert.That(refill.SeedsDelta, Is.EqualTo(-200));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.Null);

            fx.Wallet.Apply(Transaction.HardFail(31));
            TransactionResult poor = fx.Wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill));
            Assert.That(poor.Reason, Is.EqualTo(RejectReason.InsufficientSeeds));
            Assert.That(poor.Missing, Is.EqualTo(1));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
        }

        [Test]
        public void Refill_CanBeSwitchedOffByConfig()
        {
            var config = EconomyConfig.Defaults;
            config.CanoesRefillSeedsEnabled = false;
            var fx = new EconomyFixture(seeds: 1000, config: config);
            fx.Wallet.Apply(Transaction.HardFail(31));

            Assert.That(fx.Wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill)).Reason, Is.EqualTo(RejectReason.Disabled));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
        }

        [Test]
        public void ReadingCanoes_DoesNotChangeTheProfile_UntilTheNextMutation()
        {
            var fx = new EconomyFixture();
            fx.Wallet.Apply(Transaction.HardFail(31));
            fx.Wallet.Apply(Transaction.HardFail(31));
            string saved = fx.Json;

            fx.Clock.Advance(TimeSpan.FromMinutes(26));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Json, Is.EqualTo(saved), "leer no escribe");
            Assert.That(fx.Profile.Wallet.Canoes.Count, Is.EqualTo(3));

            fx.Wallet.Apply(Transaction.Win(31, RewardTier.Normal));
            Assert.That(fx.Profile.Wallet.Canoes.Count, Is.EqualTo(4), "la regeneración se escribe con la siguiente mutación");
            Assert.That(fx.Store.Load().Profile!.Wallet.Canoes.Count, Is.EqualTo(4));
        }

        [Test]
        public void ClockGoingBackwards_RegeneratesNothing()
        {
            var fx = new EconomyFixture();
            DateTime t0 = fx.Clock.UtcNow;
            fx.Wallet.Apply(Transaction.HardFail(31));

            fx.Clock.Set(t0 - TimeSpan.FromHours(3));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.EqualTo(t0 + Regen));

            fx.Clock.Set(t0 + Regen);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
        }

        [Test]
        public void Peek_RepairsAMissingTimer()
        {
            var data = new CanoesData { Count = 2, NextRegenAt = null };
            DateTime now = EconomyFixture.T0;

            CanoesStatus status = CanoeRules.Peek(data, now, 5, Regen);

            Assert.That(status.Count, Is.EqualTo(2));
            Assert.That(status.NextRegenAt, Is.EqualTo(now + Regen));
            Assert.That(data.NextRegenAt, Is.Null, "Peek no escribe");
        }
    }
}
