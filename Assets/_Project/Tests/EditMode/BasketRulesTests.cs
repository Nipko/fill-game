#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Reglas de la Canasta (B1a, A2; 06 §4.4; 05 §2.1; 04 §3; GDD.md §6.1 #13; nota <c>docs/dev/crecida-canasta.md</c> §1.2):
    /// cada 10 niveles desde <c>live_unlock_level_daily</c>, solo semillas en el 10, ciclo fijo desde el 20, Canasta grande en
    /// 100 y 200 en su ranura, la del hito 6 deducida de <c>max_level</c>, pago con la cartera y <c>contents_json</c> estable.
    /// </summary>
    public sealed class BasketRulesTests
    {
        private const int Unlock = 10; // live_unlock_level_daily (08 §9)

        [TestCase(1)]
        [TestCase(9)]
        [TestCase(11)]
        [TestCase(25)]
        [TestCase(99)]
        [TestCase(101)]
        public void NotAMultipleOfTen_HasNoBasket(int level)
        {
            Assert.That(BasketRules.For(level, Unlock, BasketConfig.Defaults).IsNone, Is.True);
        }

        [Test]
        public void Level10_IsOnly50Seeds()
        {
            BasketContents b = BasketRules.For(10, Unlock, BasketConfig.Defaults);

            Assert.That(b.Kind, Is.EqualTo(BasketKind.Normal));
            Assert.That(b.Level, Is.EqualTo(10));
            Assert.That(b.Seeds, Is.EqualTo(50), "06 §4.4: «nivel 10 → 50 semillas»");
            Assert.That(b.Stock.IsZero, Is.True);
            Assert.That(b.ContentsJson(), Is.EqualTo("{\"kind\":\"normal\",\"seeds\":50}"));
        }

        [TestCase(20, BoosterKind.Undo, 3)]
        [TestCase(30, BoosterKind.Hint, 1)]
        [TestCase(40, BoosterKind.ExtraCuia, 1)]
        [TestCase(50, BoosterKind.Undo, 3)]
        [TestCase(60, BoosterKind.Hint, 1)]
        [TestCase(70, BoosterKind.Undo, 3)]
        [TestCase(80, BoosterKind.Hint, 1)]
        [TestCase(90, BoosterKind.ExtraCuia, 1)]
        [TestCase(110, BoosterKind.Hint, 1)]
        [TestCase(120, BoosterKind.Undo, 3)]
        [TestCase(140, BoosterKind.ExtraCuia, 1)]
        [TestCase(190, BoosterKind.ExtraCuia, 1)]
        public void FromLevel20_50SeedsAndTheCycleBooster(int level, BoosterKind kind, int units)
        {
            BasketContents b = BasketRules.For(level, Unlock, BasketConfig.Defaults);

            Assert.That(b.Kind, Is.EqualTo(BasketKind.Normal));
            Assert.That(b.Seeds, Is.EqualTo(50));
            Assert.That(b.Stock, Is.EqualTo(StockDelta.Of(kind, units)), "06 §4.4: [3 Marea, Morpho, Cuia extra, 3 Marea, Morpho] desde el 20");
        }

        [Test]
        public void CuiaExtraIn40And90_MorphoIn30_60_80_OfEachHundred()
        {
            // 04 §3, columna «Gratis»: Cuia extra en «niveles 40 y 90 de cada 100»; Morpho en «30, 60, 80…».
            for (int hundred = 0; hundred < 300; hundred += 100)
            {
                var extra = new List<int>();
                var hint = new List<int>();
                for (int level = hundred + 10; level < hundred + 100; level += 10)
                {
                    BasketContents b = BasketRules.For(level, Unlock, BasketConfig.Defaults);
                    if (b.Stock.Get(BoosterKind.ExtraCuia) > 0)
                    {
                        extra.Add(level - hundred);
                    }

                    if (b.Stock.Get(BoosterKind.Hint) > 0)
                    {
                        hint.Add(level - hundred);
                    }
                }

                Assert.That(extra, Is.EqualTo(new[] { 40, 90 }), "centena " + hundred);
                Assert.That(hint, Is.EqualTo(hundred == 0 ? new[] { 30, 60, 80 } : new[] { 10, 30, 60, 80 }), "centena " + hundred);
            }
        }

        [TestCase(100)]
        [TestCase(200)]
        [TestCase(700)]
        public void EveryHundred_TheGrandBasketReplacesTheNormalOne(int level)
        {
            BasketContents b = BasketRules.For(level, Unlock, BasketConfig.Defaults);

            Assert.That(b.Kind, Is.EqualTo(BasketKind.Grand));
            Assert.That(b.Seeds, Is.EqualTo(150), "GDD.md §6.1 #13");
            Assert.That(b.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(1));
            Assert.That(b.Stock.Get(BoosterKind.Hint), Is.EqualTo(1));
            Assert.That(b.Stock.Get(BoosterKind.Undo), Is.EqualTo(3));
            Assert.That(b.Stock.Get(BoosterKind.Reveal) + b.Stock.Get(BoosterKind.Shuffle), Is.EqualTo(0));
            Assert.That(b.ContentsJson(), Is.EqualTo("{\"kind\":\"grand\",\"seeds\":150,\"undo\":3,\"hint\":1,\"extra_cuia\":1}"));
        }

        [Test]
        public void TheGrandBasket_TakesItsSlot_AndTheCycleDoesNotShift()
        {
            // Q-C4 (a): 100 sería 3 Marea (índice 3) y 110 sigue siendo Morpho (índice 4), como si el 100 fuera normal.
            Assert.That(BasketRules.CycleIndex(100, 5), Is.EqualTo(3));
            Assert.That(BasketRules.For(110, Unlock, BasketConfig.Defaults).Stock, Is.EqualTo(StockDelta.Of(BoosterKind.Hint, 1)));
            Assert.That(BasketRules.For(210, Unlock, BasketConfig.Defaults).Stock, Is.EqualTo(StockDelta.Of(BoosterKind.Hint, 1)));
        }

        [Test]
        public void BelowTheUnlockLevel_ThereIsNoBasket()
        {
            Assert.That(BasketRules.For(10, 12, BasketConfig.Defaults).IsNone, Is.True, "live_unlock_level_daily = 12: nada en el 10");
            BasketContents b20 = BasketRules.For(20, 12, BasketConfig.Defaults);
            Assert.That(b20.Kind, Is.EqualTo(BasketKind.Normal));
            Assert.That(b20.Stock, Is.EqualTo(StockDelta.Of(BoosterKind.Undo, 3)), "el ciclo va por nivel, no por Canastas cobradas");
            Assert.That(BasketRules.For(10, 10, BasketConfig.Defaults).IsNone, Is.False, "el nivel del desbloqueo ya la da");
            Assert.That(BasketRules.For(10, 8, BasketConfig.Defaults).Seeds, Is.EqualTo(50));
        }

        [Test]
        public void TheConfig_ChangesSeedsAndCycle()
        {
            var config = new BasketConfig { EcoBasketSeeds = 30, EcoBasketCycle = new[] { "reveal", "shuffle2" } };

            Assert.That(BasketRules.For(10, Unlock, config).Seeds, Is.EqualTo(30));
            Assert.That(BasketRules.For(20, Unlock, config).Stock, Is.EqualTo(StockDelta.Of(BoosterKind.Reveal, 1)));
            Assert.That(BasketRules.For(30, Unlock, config).Stock, Is.EqualTo(StockDelta.Of(BoosterKind.Shuffle, 2)));
            Assert.That(BasketRules.For(40, Unlock, config).Stock, Is.EqualTo(StockDelta.Of(BoosterKind.Reveal, 1)));
            Assert.That(BasketRules.For(100, Unlock, config).Seeds, Is.EqualTo(150), "la grande es constante [P] (Q-C12 (a))");

            config.EcoBasketCycle = new[] { "bogus" };
            Assert.Throws<InvalidOperationException>(() => BasketRules.For(20, Unlock, config));
            Assert.Throws<ArgumentOutOfRangeException>(() => BasketRules.For(0, Unlock, BasketConfig.Defaults));
        }

        [TestCase(25, 30, BoosterKind.Hint, 1)]
        [TestCase(29, 30, BoosterKind.Hint, 1)]
        [TestCase(30, 40, BoosterKind.ExtraCuia, 1)]
        [TestCase(36, 40, BoosterKind.ExtraCuia, 1)]
        [TestCase(45, 50, BoosterKind.Undo, 3)]
        [TestCase(95, 100, BoosterKind.Undo, 3)]
        [TestCase(100, 110, BoosterKind.Hint, 1)]
        [TestCase(5, 20, BoosterKind.Undo, 3)]
        public void HitSix_BringsTheBoosterOfTheNextLevelBasket(int maxLevel, int slot, BoosterKind kind, int units)
        {
            BasketContents b = BasketRules.HitSix(maxLevel, BasketConfig.Defaults);

            Assert.That(b.Kind, Is.EqualTo(BasketKind.Streak));
            Assert.That(b.Level, Is.EqualTo(slot), "Q-C5 (a): primer múltiplo de 10 mayor que max_level, como mínimo el 20");
            Assert.That(b.Seeds, Is.EqualTo(50), "06 §4.3: 50 semillas + 1 comodín del ciclo");
            Assert.That(b.Stock, Is.EqualTo(StockDelta.Of(kind, units)), "en el 100, el comodín de su ranura, nunca la grande");
        }

        [Test]
        public void Transactions_PayTheBasketThroughTheWallet()
        {
            var fx = new EconomyFixture();
            List<Transaction> paid = BasketRules.For(40, Unlock, BasketConfig.Defaults).ToTransactions();

            Assert.That(paid.Count, Is.EqualTo(2));
            Assert.That(paid[0].Kind, Is.EqualTo(TransactionKind.Earn));
            Assert.That(paid[0].Source, Is.EqualTo(SeedSource.Basket), "earn_virtual_currency source = basket (04 §11)");
            Assert.That(paid[0].Seeds, Is.EqualTo(50));
            Assert.That(paid[1].Kind, Is.EqualTo(TransactionKind.GrantStock));
            Assert.That(paid[1].Booster, Is.EqualTo(BoosterKind.ExtraCuia));
            foreach (Transaction t in paid)
            {
                Assert.That(fx.Wallet.Apply(t).IsApplied, Is.True);
            }

            foreach (Transaction t in BasketRules.For(100, Unlock, BasketConfig.Defaults).ToTransactions())
            {
                Assert.That(fx.Wallet.Apply(t).IsApplied, Is.True);
            }

            Assert.That(fx.Wallet.Seeds, Is.EqualTo(200));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(2));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(1));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(3), "undo3 / 3 Marea = 3 unidades de stock (06 §4.4)");

            Assert.That(BasketContents.None.ToTransactions(), Is.Empty);
            List<Transaction> streak = BasketRules.HitSix(25, BasketConfig.Defaults).WithSeeds(0).ToTransactions(SeedSource.Streak);
            Assert.That(streak.Count, Is.EqualTo(1), "sin semillas (tope), solo el comodín");
            Assert.That(streak[0].Kind, Is.EqualTo(TransactionKind.GrantStock));
        }

        [Test]
        public void ContentsJson_IsStable()
        {
            Assert.That(BasketRules.For(20, Unlock, BasketConfig.Defaults).ContentsJson(), Is.EqualTo("{\"kind\":\"normal\",\"seeds\":50,\"undo\":3}"));
            Assert.That(BasketRules.For(30, Unlock, BasketConfig.Defaults).ContentsJson(), Is.EqualTo("{\"kind\":\"normal\",\"seeds\":50,\"hint\":1}"));
            Assert.That(BasketRules.HitSix(30, BasketConfig.Defaults).ContentsJson(), Is.EqualTo("{\"kind\":\"streak\",\"seeds\":50,\"extra_cuia\":1}"));
            Assert.That(BasketRules.HitSix(30, BasketConfig.Defaults).WithSeeds(10).ContentsJson(), Is.EqualTo("{\"kind\":\"streak\",\"seeds\":10,\"extra_cuia\":1}"));
            Assert.That(BasketContents.None.ContentsJson(), Is.EqualTo("{}"));
            Assert.That(BasketRules.For(20, Unlock, BasketConfig.Defaults), Is.EqualTo(BasketRules.For(20, Unlock, BasketConfig.Defaults)), "determinista");
        }

        [Test]
        public void Tokens_AreBoosterIdsWithOptionalUnits()
        {
            Assert.That(BasketConfig.TryParseToken("undo3", out BoosterKind k, out int u) && k == BoosterKind.Undo && u == 3, Is.True);
            Assert.That(BasketConfig.TryParseToken("hint", out k, out u) && k == BoosterKind.Hint && u == 1, Is.True);
            Assert.That(BasketConfig.TryParseToken("extra_cuia", out k, out u) && k == BoosterKind.ExtraCuia && u == 1, Is.True);
            Assert.That(BasketConfig.TryParseToken("shuffle12", out k, out u) && k == BoosterKind.Shuffle && u == 12, Is.True);
            foreach (string bad in new[] { "", "undo0", "undo100", "undo-1", "skip", "extra", "hint ", " hint", "HINT" })
            {
                Assert.That(BasketConfig.TryParseToken(bad, out _, out _), Is.False, "«" + bad + "»");
            }

            Assert.That(BasketConfig.TryParseToken(null, out _, out _), Is.False);
        }
    }
}
