#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Cartera de semillas y stock (B1-2; GDD 08 §2.2 <c>AF.Economy</c>; 04 §2-§6; 05 §2): fuentes y sumideros con las
    /// cifras de 08 §9, saldo nunca negativo, transacción rechazada sin efectos, precio del rescate por día local, compras
    /// y guardado tras cada mutación con <c>economy_ts</c>.
    /// </summary>
    public sealed class WalletTests
    {
        [Test]
        public void NewProfile_StartsWithNoSeeds_AndFullCanoes()
        {
            var fx = new EconomyFixture();

            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "el GDD no fija otro saldo inicial");
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5), "04 §7.1: debut con 5/5");
            Assert.That(fx.Wallet.Canoes.IsFull, Is.True);
            Assert.That(fx.Wallet.Canoes.NextRegenAt, Is.Null);
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                Assert.That(fx.Wallet.Stock.Get((BoosterKind)k), Is.EqualTo(0));
            }
        }

        [TestCase(RewardTier.Normal, 20, "win")]
        [TestCase(RewardTier.Hard, 60, "hard")]
        [TestCase(RewardTier.VeryHard, 80, "very_hard")]
        public void Win_PaysTheHarvestOfTheLabel(RewardTier tier, int seeds, string source)
        {
            var fx = new EconomyFixture();

            TransactionResult result = fx.Wallet.Apply(Transaction.Win(1, tier));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.SeedsDelta, Is.EqualTo(seeds));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(seeds));
            Assert.That(result.Transaction.Source.ToId(), Is.EqualTo(source));
        }

        [Test]
        public void DoubleHarvest_From11_PaysTheSameHarvestAgain_AndBeforeIsRejected()
        {
            var fx = new EconomyFixture();
            fx.Wallet.Apply(Transaction.Win(10, RewardTier.Normal));
            string before = fx.Json;

            TransactionResult early = fx.Wallet.Apply(Transaction.DoubleHarvest(10, RewardTier.Normal));
            Assert.That(early.Status, Is.EqualTo(TransactionStatus.Rejected));
            Assert.That(early.Reason, Is.EqualTo(RejectReason.NotUnlocked));
            Assert.That(fx.Json, Is.EqualTo(before));

            fx.Wallet.Apply(Transaction.Win(11, RewardTier.Hard));
            TransactionResult doubled = fx.Wallet.Apply(Transaction.DoubleHarvest(11, RewardTier.Hard));
            Assert.That(doubled.IsApplied, Is.True);
            Assert.That(doubled.SeedsDelta, Is.EqualTo(60), "60 → 120 (04 §2.8)");
            Assert.That(doubled.Transaction.Source.ToId(), Is.EqualTo("double_harvest"));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(20 + 60 + 60));
        }

        [TestCase(SeedSink.UndoRefill, 30)]
        [TestCase(SeedSink.ExtraCuia, 150)]
        [TestCase(SeedSink.Hint, 100)]
        [TestCase(SeedSink.Shuffle, 300)]
        [TestCase(SeedSink.Reveal, 120)]
        [TestCase(SeedSink.Skip, 300)]
        public void Buy_ChargesThePriceOf08_9(SeedSink sink, int price)
        {
            var fx = new EconomyFixture(seeds: 1000);
            Assert.That(fx.Wallet.Price(sink), Is.EqualTo(price));

            TransactionResult result = fx.Wallet.Apply(Transaction.Buy(sink));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.SeedsDelta, Is.EqualTo(-price));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000 - price));
            Assert.That(result.Transaction.Sink, Is.EqualTo(sink));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(sink == SeedSink.UndoRefill ? 3 : 0),
                "solo la recarga de Marea entra al stock: +3 (undo_refill_amount)");
        }

        [Test]
        public void RejectedTransaction_ChangesNothing()
        {
            var fx = new EconomyFixture(seeds: 299);
            int events = 0;
            fx.Wallet.Changed += _ => events++;
            string before = fx.Json;
            DateTime? ts = fx.Profile.EconomyTs;
            fx.Clock.Advance(TimeSpan.FromHours(30)); // cambio de día pendiente: tampoco se escribe

            TransactionResult shuffle = fx.Wallet.Apply(Transaction.Buy(SeedSink.Shuffle));
            Assert.That(shuffle.Status, Is.EqualTo(TransactionStatus.Rejected));
            Assert.That(shuffle.Reason, Is.EqualTo(RejectReason.InsufficientSeeds));
            Assert.That(shuffle.Missing, Is.EqualTo(1), "«Te faltan 1» (04 §1.1)");
            Assert.That(fx.Wallet.Missing(300), Is.EqualTo(1));

            TransactionResult use = fx.Wallet.Apply(Transaction.UseStock(BoosterKind.Hint));
            Assert.That(use.Reason, Is.EqualTo(RejectReason.InsufficientStock));

            TransactionResult canoe = fx.Wallet.Apply(Transaction.AddCanoe());
            Assert.That(canoe.Reason, Is.EqualTo(RejectReason.CanoesFull));

            Assert.That(fx.Json, Is.EqualTo(before));
            Assert.That(fx.Profile.EconomyTs, Is.EqualTo(ts));
            Assert.That(fx.Store.SaveCount, Is.EqualTo(0));
            Assert.That(events, Is.EqualTo(0));
            Assert.That(shuffle.Changed || use.Changed || canoe.Changed, Is.False);
        }

        [Test]
        public void Balance_NeverGoesNegative()
        {
            var fx = new EconomyFixture(seeds: 500);
            var sinks = new[] { SeedSink.Shuffle, SeedSink.Shuffle, SeedSink.ExtraCuia, SeedSink.Hint, SeedSink.Reveal, SeedSink.UndoRefill, SeedSink.Skip };
            var expected = new[] { 200, 200, 50, 50, 50, 20, 20 };
            for (int i = 0; i < sinks.Length; i++)
            {
                fx.Wallet.Apply(Transaction.Buy(sinks[i]));
                Assert.That(fx.Wallet.Seeds, Is.EqualTo(expected[i]), "compra " + i + ": " + sinks[i]);
                Assert.That(fx.Wallet.Seeds, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void Rescue_Costs300_Then600TheSameLocalDay_And300AgainTheNextLocalDay()
        {
            // T0 = 10:00 en Bogotá (UTC−5).
            var fx = new EconomyFixture(seeds: 2000);
            Assert.That(fx.Wallet.QuoteRescue(30), Is.Null, "antes del 31 no hay rescate (rescue_min_level)");

            RescueQuote first = fx.Wallet.QuoteRescue(31)!.Value;
            Assert.That(first.Price, Is.EqualTo(300), "primero del día: 600 × 0,5");
            Assert.That(first.BasePrice, Is.EqualTo(600));
            Assert.That(first.IsDiscounted, Is.True);
            Assert.That(fx.Wallet.Apply(Transaction.Rescue(31, first.Price)).SeedsDelta, Is.EqualTo(-300));

            RescueQuote second = fx.Wallet.QuoteRescue(45)!.Value;
            Assert.That(second.Price, Is.EqualTo(600));
            Assert.That(second.IsDiscounted, Is.False);
            Assert.That(fx.Wallet.Apply(Transaction.Rescue(45, second.Price)).SeedsDelta, Is.EqualTo(-600));
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(2));
            Assert.That(fx.Profile.Stats.Rescues, Is.EqualTo(2));

            fx.Clock.Advance(TimeSpan.FromHours(9.5)); // 19:30 en Bogotá: ya es otro día en UTC, no en local
            Assert.That(fx.Wallet.QuoteRescue(31)!.Value.Price, Is.EqualTo(600));

            fx.Clock.Set(new DateTime(2026, 10, 9, 4, 59, 0, DateTimeKind.Utc)); // 23:59 local
            Assert.That(fx.Wallet.QuoteRescue(31)!.Value.Price, Is.EqualTo(600));

            fx.Clock.Advance(TimeSpan.FromMinutes(1)); // 00:00 local del día siguiente
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(0));
            RescueQuote nextDay = fx.Wallet.QuoteRescue(31)!.Value;
            Assert.That(nextDay.Price, Is.EqualTo(300));
            fx.Wallet.Apply(Transaction.Rescue(31, nextDay.Price));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2000 - 300 - 600 - 300));
            Assert.That(fx.Profile.Daily.RescuesToday, Is.EqualTo(1));
            Assert.That(fx.Profile.Daily.DateLocal, Is.EqualTo("2026-10-09"));
        }

        [Test]
        public void Rescue_OnlyAcceptsAQuotedPrice_FromLevel31_AndWithEnoughSeeds()
        {
            var fx = new EconomyFixture(seeds: 250);

            Assert.That(fx.Wallet.Apply(Transaction.Rescue(30, 300)).Reason, Is.EqualTo(RejectReason.NotUnlocked));
            Assert.That(fx.Wallet.Apply(Transaction.Rescue(31, 200)).Reason, Is.EqualTo(RejectReason.InvalidPrice));

            TransactionResult poor = fx.Wallet.Apply(Transaction.Rescue(31, 300));
            Assert.That(poor.Reason, Is.EqualTo(RejectReason.InsufficientSeeds));
            Assert.That(poor.Missing, Is.EqualTo(50), "«Te faltan 50 semillas» (04 §6.6, wireframe B)");
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(0));

            // El precio no cambia con el modal abierto (04 §10.2): un 600 cotizado antes de medianoche sigue valiendo.
            fx.Wallet.Apply(Transaction.Earn(SeedSource.Iap, 1000));
            fx.Wallet.Apply(Transaction.Rescue(31, 300));
            Assert.That(fx.Wallet.QuoteRescue(31)!.Value.Price, Is.EqualTo(600));
            fx.Clock.Advance(TimeSpan.FromDays(1));
            Assert.That(fx.Wallet.Apply(Transaction.Rescue(31, 600)).SeedsDelta, Is.EqualTo(-600));
        }

        [Test]
        public void EveryMutation_IsSaved_WithEconomyTs()
        {
            var fx = new EconomyFixture(seeds: 400);
            var steps = new[]
            {
                Transaction.Win(5, RewardTier.Normal),
                Transaction.Buy(SeedSink.UndoRefill),
                Transaction.GrantStock(BoosterKind.ExtraCuia, 1),
                Transaction.UseStock(BoosterKind.ExtraCuia),
                Transaction.HardFail(31),
                Transaction.InfiniteCanoesFor(TimeSpan.FromMinutes(60)),
            };

            for (int i = 0; i < steps.Length; i++)
            {
                fx.Clock.Advance(TimeSpan.FromSeconds(7));
                TransactionResult result = fx.Wallet.Apply(steps[i]);

                Assert.That(result.IsApplied && result.Changed && result.Saved, Is.True, "paso " + i);
                Assert.That(fx.Store.SaveCount, Is.EqualTo(i + 1), "un guardado por mutación");
                Assert.That(fx.Profile.EconomyTs, Is.EqualTo(fx.Clock.UtcNow));
                Assert.That(fx.Store.Json, Is.EqualTo(fx.Json), "lo guardado es el perfil en memoria");
            }

            Profile reloaded = fx.Store.Load().Profile!;
            Assert.That(reloaded.Wallet.Seeds, Is.EqualTo(400 + 20 - 30));
            Assert.That(reloaded.Stock.Undo, Is.EqualTo(3));
            Assert.That(reloaded.Stock.ExtraCuia, Is.EqualTo(0));
            Assert.That(reloaded.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(reloaded.Wallet.Canoes.InfiniteUntil, Is.EqualTo(fx.Clock.UtcNow.AddMinutes(60)));
        }

        [Test]
        public void Purchase_CreditsItsExactContent()
        {
            var fx = new EconomyFixture(seeds: 100);

            // Balsa de rescate (05 §4.1): rescate aplicado + 1.000 semillas + 1 Cuia extra + Canoas infinitas 30 min.
            TransactionResult balsa = fx.Wallet.Apply(Transaction.Purchase(
                "af_rescue",
                seeds: 1000,
                stock: StockDelta.Of(BoosterKind.ExtraCuia, 1),
                infiniteCanoes: TimeSpan.FromMinutes(30),
                appliesRescue: true));
            Assert.That(balsa.IsApplied, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1100));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(1));
            Assert.That(fx.Wallet.Canoes.InfiniteRemaining, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(1), "el rescate de la Balsa cuenta: el siguiente cuesta 600");
            Assert.That(fx.Wallet.QuoteRescue(31)!.Value.Price, Is.EqualTo(600));

            // Sin anuncios: derecho + 500 semillas; el derecho persiste.
            fx.Wallet.Apply(Transaction.Purchase("af_noads", seeds: 500, grantsNoAds: true));
            Assert.That(fx.Profile.Entitlements.NoAds, Is.True);
            Assert.That(fx.Store.Load().Profile!.Entitlements.NoAds, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1600));

            // Puñado: 1.000 semillas.
            fx.Wallet.Apply(Transaction.Purchase("af_coins_2", seeds: 1000));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2600));
        }

        [Test]
        public void Stock_IsGrantedAndUsed_OneUnitAtATime()
        {
            var fx = new EconomyFixture();

            fx.Wallet.Apply(Transaction.GrantStock(BoosterKind.Hint, 3)); // 3 Morphos del nivel 12 (04 §2.3)
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(3));
            Assert.That(fx.Wallet.Stock.Has(BoosterKind.Hint), Is.True);

            for (int i = 0; i < 3; i++)
            {
                Assert.That(fx.Wallet.Apply(Transaction.UseStock(BoosterKind.Hint)).IsApplied, Is.True);
            }

            Assert.That(fx.Wallet.Stock.Has(BoosterKind.Hint), Is.False);
            Assert.That(fx.Wallet.Apply(Transaction.UseStock(BoosterKind.Hint)).Reason, Is.EqualTo(RejectReason.InsufficientStock));
            Assert.That(fx.Profile.Stock.Hint, Is.EqualTo(0));
        }

        [Test]
        public void FailedWrite_KeepsTheChangeInMemory_AndRetries()
        {
            var clock = new ManualClock(EconomyFixture.T0);
            var store = new FlakyStore { FailuresLeft = 1 };
            var wallet = new Wallet(Wallet.NewProfile(clock), store, clock);

            TransactionResult first = wallet.Apply(Transaction.Win(1, RewardTier.Normal));
            Assert.That(first.IsApplied, Is.True);
            Assert.That(first.Saved, Is.False);
            Assert.That(wallet.PendingSave, Is.True);
            Assert.That(wallet.Seeds, Is.EqualTo(20), "el juego sigue con el cambio en memoria");

            Assert.That(wallet.Flush(), Is.True);
            Assert.That(wallet.PendingSave, Is.False);
            Assert.That(store.Saved, Is.EqualTo(1));

            store.Blocked = true; // perfil de un esquema más nuevo: el almacén no deja escribir (IProfileStore.Save)
            Assert.That(wallet.Apply(Transaction.Win(2, RewardTier.Normal)).Saved, Is.False);
            Assert.That(wallet.PendingSave, Is.True);
        }

        [Test]
        public void Changed_IsRaisedOncePerAppliedChange()
        {
            var fx = new EconomyFixture();
            var seen = new List<TransactionKind>();
            fx.Wallet.Changed += r => seen.Add(r.Transaction.Kind);

            fx.Wallet.Apply(Transaction.Win(1, RewardTier.Normal));
            fx.Wallet.Apply(Transaction.Buy(SeedSink.Hint)); // rechazada
            TransactionResult freeFail = fx.Wallet.Apply(Transaction.HardFail(12)); // aplicada, sin cambios
            fx.Wallet.Apply(Transaction.HardFail(31));

            Assert.That(freeFail.IsApplied, Is.True);
            Assert.That(freeFail.Changed, Is.False);
            Assert.That(seen, Is.EqualTo(new[] { TransactionKind.Win, TransactionKind.HardFail }));
            Assert.That(fx.Store.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void Factories_RefuseAmountsThatThe08_9TableFixes()
        {
            Assert.Throws<ArgumentException>(() => Transaction.Earn(SeedSource.Win, 20));
            Assert.Throws<ArgumentException>(() => Transaction.Earn(SeedSource.DoubleHarvest, 20));
            Assert.Throws<ArgumentException>(() => Transaction.Buy(SeedSink.ContinueCuia));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.Earn(SeedSource.Basket, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.Rescue(31, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.GrantStock(BoosterKind.Undo, -3));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.Purchase("af_coins_2", seeds: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Transaction.Win(0, RewardTier.Normal));
        }

        [TestCase(null, RewardTier.Normal, true)]
        [TestCase("normal", RewardTier.Normal, true)]
        [TestCase("hard", RewardTier.Hard, true)]
        [TestCase("very_hard", RewardTier.VeryHard, true)]
        [TestCase("Hard", RewardTier.Normal, false)]
        public void RewardTier_ParsesThePackLabels(string? label, RewardTier expected, bool ok)
        {
            Assert.That(RewardTiers.TryParse(label, out RewardTier tier), Is.EqualTo(ok));
            Assert.That(tier, Is.EqualTo(expected));
        }

        [Test]
        public void DailyRollOver_ResetsTheWholeBlock_KeepsBalsaDeclines_AndNeverGoesBack()
        {
            var daily = new DailyData
            {
                DateLocal = "2026-10-08",
                RvUndoRefill = 4, RvHint = 3, RvReveal = 3, RvExtraCanoe = 3, RvDoubleHarvest = 5, RvTotal = 15,
                InterstitialsToday = 12, RescuesToday = 2, AfRescueShown = 1, AfRescueDeclinedDays = 2, ShufflesToday = 3,
                ShieldAvailable = false,
            };

            Assert.That(DailyCounters.RollOver(daily, "2026-10-08"), Is.False);
            Assert.That(DailyCounters.RollOver(daily, "2026-10-07"), Is.False, "el reloj hacia atrás no reabre los topes");
            Assert.That(daily.RescuesToday, Is.EqualTo(2));

            Assert.That(DailyCounters.RollOver(daily, "2026-10-09"), Is.True);
            Assert.That(daily.DateLocal, Is.EqualTo("2026-10-09"));
            Assert.That(daily.RvUndoRefill + daily.RvHint + daily.RvReveal + daily.RvExtraCanoe + daily.RvDoubleHarvest, Is.EqualTo(0));
            Assert.That(daily.RvTotal + daily.InterstitialsToday + daily.RescuesToday + daily.AfRescueShown + daily.ShufflesToday, Is.EqualTo(0));
            Assert.That(daily.ShieldAvailable, Is.True, "05 §2.4 reset_daily: una Hoja escudo al día");
            Assert.That(daily.AfRescueDeclinedDays, Is.EqualTo(2), "vuelve a 0 al comprar, no al cambiar de día (04 §6.2)");

            var fresh = new DailyData();
            Assert.That(DailyCounters.RollOver(fresh, "2026-10-09"), Is.True, "sin fecha: primer día");
        }

        [Test]
        public void AnalyticsIds_AreThoseOf04_11()
        {
            var sources = new[] { "win", "hard", "very_hard", "double_harvest", "streak", "calendar", "basket", "daily_challenge", "piggy", "iap", "refund", "event" };
            for (int i = 0; i < sources.Length; i++)
            {
                Assert.That(((SeedSource)i).ToId(), Is.EqualTo(sources[i]));
            }

            var sinks = new[] { "undo_refill", "extra_cuia", "hint", "shuffle", "reveal", "skip", "continue_cuia", "canoes_refill" };
            for (int i = 0; i < sinks.Length; i++)
            {
                Assert.That(((SeedSink)i).ToId(), Is.EqualTo(sinks[i]));
            }

            var boosters = new[] { "undo", "hint", "reveal", "extra_cuia", "shuffle" };
            for (int i = 0; i < boosters.Length; i++)
            {
                Assert.That(((BoosterKind)i).ToId(), Is.EqualTo(boosters[i]));
            }
        }

        /// <summary>Almacén que falla a demanda.</summary>
        private sealed class FlakyStore : IProfileStore
        {
            public int FailuresLeft { get; set; }

            public bool Blocked { get; set; }

            public int Saved { get; private set; }

            public ProfileLoadResult Load()
            {
                return new ProfileLoadResult(ProfileLoadStatus.NotFound, null);
            }

            public void Save(Profile profile)
            {
                if (Blocked)
                {
                    throw new InvalidOperationException("perfil protegido");
                }

                if (FailuresLeft > 0)
                {
                    FailuresLeft--;
                    throw new IOException("disco lleno");
                }

                Saved++;
            }

            public void Delete()
            {
            }
        }
    }

    /// <summary>Cartera de prueba: reloj manual en Bogotá (UTC−5), perfil nuevo y almacén en memoria.</summary>
    internal sealed class EconomyFixture
    {
        /// <summary>2026-10-08 15:00 UTC = 10:00 en Bogotá.</summary>
        public static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        public EconomyFixture(int seeds = 0, EconomyConfig? config = null)
        {
            Clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            Profile = Wallet.NewProfile(Clock, config);
            Profile.Wallet.Seeds = seeds;
            Store = new MemoryProfileStore();
            Wallet = new Wallet(Profile, Store, Clock, config);
        }

        public ManualClock Clock { get; }

        public Profile Profile { get; }

        public MemoryProfileStore Store { get; }

        public Wallet Wallet { get; }

        /// <summary>El perfil en memoria serializado.</summary>
        public string Json => ProfileJson.Serialize(Profile);
    }
}
