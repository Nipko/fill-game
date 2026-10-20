#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <see cref="StreakService"/> (B1a, A5) con <see cref="InMemoryStreakStore"/> y una <see cref="Wallet"/> real (06 §4.3-§4.4;
    /// 05 §2.1 y §2.4; 04 §2.6; nota <c>docs/dev/crecida-canasta.md</c> C1-C13 y K1-K9): hitos 3, 6 y 10 con sus pagos y fuentes,
    /// una vez al día, el tope de 600, la Hoja escudo, el cambio de día entre <c>Load</c> y <c>Save</c>, la Canasta del 10k
    /// saltado, fuera de campaña y antes del 25, y el kill switch.
    /// </summary>
    public sealed class StreakServiceTests
    {
        private const string Today = "2026-10-08"; // EconomyFixture.T0 en Bogotá
        private const string Yesterday = "2026-10-07";

        private sealed class Fx
        {
            public Fx(StreakRecord? initial = null, StreakConfig? config = null, EconomyFixture? economy = null)
            {
                Economy = economy ?? new EconomyFixture();
                Store = new InMemoryStreakStore(Economy.Clock, initial);
                Service = new StreakService(Store, Economy.Wallet, Economy.Clock, 10, config);
            }

            public EconomyFixture Economy { get; }

            public InMemoryStreakStore Store { get; }

            public StreakService Service { get; }

            public Wallet Wallet => Economy.Wallet;

            public int Stock(BoosterKind kind)
            {
                return Wallet.Stock.Get(kind);
            }

            /// <summary>Gana <paramref name="count"/> niveles seguidos desde <paramref name="level"/>, cada uno por primera vez.</summary>
            public StreakServiceResult Win(ref int level, int count = 1)
            {
                StreakServiceResult? last = null;
                for (int i = 0; i < count; i++)
                {
                    last = Service.ApplyWin(level, true, level - 1, RewardTier.Normal);
                    level++;
                }

                return last!;
            }
        }

        private static StreakRecord Record(string day, int wins, bool river = false, bool shield = true, int seeds = 0, params int[] hits)
        {
            var r = new StreakRecord { DateLocal = day, Wins = wins, RiverFull = river, ShieldAvailable = shield, SeedsToday = seeds };
            r.HitsToday.AddRange(hits);
            return r;
        }

        private static string[] Sources(StreakServiceResult r)
        {
            var ids = new List<string>();
            for (int i = 0; i < r.Transactions.Count; i++)
            {
                Transaction t = r.Transactions[i].Transaction;
                ids.Add(t.Kind == TransactionKind.GrantStock ? "stock:" + t.Booster.ToId() : t.Source.ToId());
            }

            return ids.ToArray();
        }

        // ---- Hitos 3, 6 y 10 (C2-C5, C8) ----

        [Test]
        public void Milestones3_6_10_PayThroughTheWallet_WithTheirSources()
        {
            var fx = new Fx();
            int level = 25;

            StreakServiceResult r27 = fx.Win(ref level, 3); // 25, 26, 27
            Assert.That(r27.Streak.Milestone, Is.EqualTo(3));
            Assert.That(r27.Has(StreakServiceEvents.MilestonePaid), Is.True);
            Assert.That(r27.StreakSeeds, Is.EqualTo(300), "C3: hito 3 → 300");
            Assert.That(Sources(r27), Is.EqualTo(new[] { "streak" }));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(300));

            StreakServiceResult r30 = fx.Win(ref level, 3); // 28, 29, 30
            Assert.That(r30.Streak.Milestone, Is.EqualTo(6));
            Assert.That(r30.Streak.Basket.Kind, Is.EqualTo(BasketKind.Streak));
            Assert.That(r30.StreakSeeds, Is.EqualTo(50), "C4: Canasta del hito 6, con source = streak (Q-C13)");
            Assert.That(r30.LevelBasket.Kind, Is.EqualTo(BasketKind.Normal), "Q-C13: en el 30 se pagan las dos");
            Assert.That(r30.BasketSeeds, Is.EqualTo(50));
            Assert.That(r30.Has(StreakServiceEvents.BasketOpened), Is.True);
            Assert.That(
                Sources(r30), Is.EqualTo(new[] { "streak", "stock:extra_cuia", "basket", "stock:hint" }),
                "hito 6: comodín de la Canasta del 40 (Cuia extra); Canasta del 30: Morpho (K3)");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(400));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(1));
            Assert.That(fx.Stock(BoosterKind.Hint), Is.EqualTo(1));

            StreakServiceResult r34 = fx.Win(ref level, 4); // 31-34
            Assert.That(r34.Streak.Milestone, Is.EqualTo(10));
            Assert.That(r34.Streak.ExtraCuia, Is.True);
            Assert.That(r34.FreeDoubleHarvest, Is.False, "C8: la victoria del hito 10 no lleva el x2");
            Assert.That(Sources(r34), Is.EqualTo(new[] { "stock:extra_cuia" }));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(2));
            Assert.That(fx.Store.Persisted.Wins, Is.EqualTo(0), "C5: la barra vuelve a 0");
            Assert.That(fx.Store.Persisted.RiverFull, Is.True);
            Assert.That(fx.Store.Persisted.HitsToday, Is.EqualTo(new[] { 3, 6, 10 }));
            Assert.That(fx.Store.Persisted.SeedsToday, Is.EqualTo(350), "el tope solo cuenta las 300 y las 50 de la Crecida");

            StreakServiceResult r35 = fx.Win(ref level);
            Assert.That(r35.FreeDoubleHarvest, Is.True, "río crecido: Cosecha doble sin anuncio (C8, Q-C1 (a))");
            Assert.That(r35.DoubleHarvest, Is.Not.Null);
            Assert.That(r35.DoubleHarvest!.IsApplied, Is.True);
            Assert.That(r35.DoubleHarvestSeeds, Is.EqualTo(20), "dobla la Cosecha de la etiqueta normal");
            Assert.That(r35.Has(StreakServiceEvents.FreeDoubleHarvest), Is.True);
            Assert.That(Sources(r35), Is.EqualTo(new[] { "double_harvest" }));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(420));
            Assert.That(r35.Streak.Wins, Is.EqualTo(1));
            Assert.That(r35.AllApplied, Is.True);
        }

        // ---- Una vez por día local (C6, C12) ----

        [Test]
        public void AMilestoneAlreadyPaidToday_IsNotPaidAgain()
        {
            var fx = new Fx(Record(Today, 2, hits: new[] { 3 }));
            int level = 41; // no es un 10k: sin Canasta de nivel

            StreakServiceResult r = fx.Win(ref level);

            Assert.That(r.Streak.Milestone, Is.EqualTo(3));
            Assert.That(r.Streak.MilestonePaid, Is.False);
            Assert.That(r.Has(StreakServiceEvents.MilestonePaid), Is.False);
            Assert.That(r.Has(StreakServiceEvents.StateChanged), Is.True);
            Assert.That(r.Transactions, Is.Empty);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Store.Persisted.Wins, Is.EqualTo(3), "la barra sí avanza");
        }

        [Test]
        public void AMilestonePaidYesterday_IsPaidAgainToday()
        {
            var fx = new Fx(Record(Yesterday, 2, shield: false, seeds: 600, hits: new[] { 3, 6 }));
            int level = 41;

            StreakServiceResult r = fx.Win(ref level);

            Assert.That(r.Streak.MilestonePaid, Is.True);
            Assert.That(r.StreakSeeds, Is.EqualTo(300), "el cambio de día vacía los hitos y las semillas de hoy");
            Assert.That(fx.Store.Persisted.DateLocal, Is.EqualTo(Today));
            Assert.That(fx.Store.Persisted.ShieldAvailable, Is.True);
            Assert.That(fx.Store.Persisted.HitsToday, Is.EqualTo(new[] { 3 }));
        }

        // ---- Tope diario (C7, Q-C10) ----

        [Test]
        public void TheDailyCap_PaysPartially_AndTheBasketBoosterIsWhole()
        {
            var fx = new Fx(Record(Today, 5, seeds: 580, hits: new[] { 3 }));
            int level = 41;

            StreakServiceResult r = fx.Win(ref level);

            Assert.That(r.Streak.Milestone, Is.EqualTo(6));
            Assert.That(r.Streak.MilestonePaid, Is.True);
            Assert.That(r.StreakSeeds, Is.EqualTo(20), "hueco 600 − 580");
            Assert.That(r.Streak.CappedSeeds, Is.EqualTo(30));
            Assert.That(fx.Stock(BoosterKind.Undo), Is.EqualTo(3), "Canasta del 50: 3 Marea, entera (Q-C10 (a))");
            Assert.That(fx.Store.Persisted.SeedsToday, Is.EqualTo(600));
        }

        [Test]
        public void TheDailyCap_Full_TheMilestoneIsPaidWithNoSeeds()
        {
            var fx = new Fx(Record(Today, 2, seeds: 600));
            int level = 41;

            StreakServiceResult r = fx.Win(ref level);

            Assert.That(r.Streak.MilestonePaid, Is.True, "queda cobrado hoy");
            Assert.That(r.StreakSeeds, Is.EqualTo(0));
            Assert.That(r.Transactions, Is.Empty, "sin Earn de 0 semillas");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Store.Persisted.HitsToday, Is.EqualTo(new[] { 3 }));
        }

        // ---- La cartera rechaza el pago del hito ----

        [Test]
        public void AMilestoneTheWalletRejects_StaysPaidToday_AndIsNotPaidAgain()
        {
            // Se guarda antes de pagar: si la cartera rechaza (aquí por desbordamiento), el hito queda cobrado igual.
            var fx = new Fx(Record(Today, 2, shield: false), economy: new EconomyFixture(seeds: int.MaxValue - 100));
            int level = 41;

            StreakServiceResult r = fx.Win(ref level);

            Assert.That(r.Streak.Milestone, Is.EqualTo(3));
            Assert.That(r.Streak.MilestonePaid, Is.True);
            Assert.That(r.Transactions.Count, Is.EqualTo(1));
            Assert.That(r.Transactions[0].Reason, Is.EqualTo(RejectReason.Overflow));
            Assert.That(r.AllApplied, Is.False);
            Assert.That(r.StreakSeeds, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(int.MaxValue - 100));
            Assert.That(fx.Store.Persisted.HitsToday, Is.EqualTo(new[] { 3 }));

            StreakServiceResult fail = fx.Service.ApplyHardFail(level, true);
            Assert.That(fail.Streak.StreakLost, Is.True, "sin hoja: la barra vuelve a 0");

            StreakServiceResult again = fx.Win(ref level, 3);
            Assert.That(again.Streak.Milestone, Is.EqualTo(3));
            Assert.That(again.Streak.MilestonePaid, Is.False, "C6: ya cobrado hoy, aunque la cartera lo rechazara");
            Assert.That(again.Transactions, Is.Empty);
            Assert.That(again.AllApplied, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(int.MaxValue - 100));
            Assert.That(fx.Store.Persisted.HitsToday, Is.EqualTo(new[] { 3 }));
        }

        // ---- x2 del río con la compuerta del vídeo por encima (Q-C14; sin el parche S4-wallet) ----

        [Test]
        public void RiverDouble_WithoutTheS4Patch_IsRejectedByTheVideoGate()
        {
            // Q-C14: sin el parche S4-wallet (E35), Wallet.Apply rechaza Transaction.DoubleHarvest por debajo de
            // ads_rv_double_harvest_from_level. S4-wallet quita este test: con él, el x2 del río se paga
            // (RiverDoubleHarvestTests.StreakService_PaysTheRiverDouble_EvenWithTheVideoGateAboveTheLevel).
            EconomyConfig gate = EconomyConfig.Defaults;
            gate.AdsRvDoubleHarvestFromLevel = 40;
            var fx = new Fx(Record(Today, 1, river: true), economy: new EconomyFixture(config: gate));

            StreakServiceResult r = fx.Service.ApplyWin(26, true, 25, RewardTier.Normal);

            Assert.That(r.FreeDoubleHarvest, Is.True);
            Assert.That(r.DoubleHarvest, Is.Not.Null);
            Assert.That(r.DoubleHarvest!.Reason, Is.EqualTo(RejectReason.NotUnlocked));
            Assert.That(r.Has(StreakServiceEvents.FreeDoubleHarvest), Is.False);
            Assert.That(r.DoubleHarvestSeeds, Is.EqualTo(0));
            Assert.That(r.AllApplied, Is.False);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
        }

        // ---- Comodín del hito 6 con max_level (Q-C5 (a)) ----

        [Test]
        public void HitSix_UsesTheMaxLevel_WhenALevelIsReplayed()
        {
            // 06 §4.5 no deja rejugar; aquí se fuerza: se gana el 32 con max_level = 45. Con el 32, el comodín sería el de la
            // Canasta del 40 (Cuia extra); con max_level = 45, el de la del 50 (3 Marea).
            var fx = new Fx(Record(Today, 5, hits: new[] { 3 }));

            StreakServiceResult r = fx.Service.ApplyWin(32, true, 45, RewardTier.Normal);

            Assert.That(r.Streak.Milestone, Is.EqualTo(6));
            Assert.That(r.Streak.MilestonePaid, Is.True);
            Assert.That(r.Streak.Basket.Kind, Is.EqualTo(BasketKind.Streak));
            Assert.That(r.Streak.Basket.Level, Is.EqualTo(50));
            Assert.That(Sources(r), Is.EqualTo(new[] { "streak", "stock:undo" }));
            Assert.That(fx.Stock(BoosterKind.Undo), Is.EqualTo(3));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(0));
            Assert.That(r.LevelBasket.IsNone, Is.True);
        }

        // ---- Hoja escudo (C9, C10) ----

        [Test]
        public void HardFail_SpendsTheShieldOnce_ThenBreaks()
        {
            var fx = new Fx(Record(Today, 4, river: true));

            StreakServiceResult first = fx.Service.ApplyHardFail(42, true);
            Assert.That(first.Streak.ShieldUsed, Is.True);
            Assert.That(first.Has(StreakServiceEvents.ShieldUsed), Is.True);
            Assert.That(first.Has(StreakServiceEvents.StreakFail), Is.True);
            Assert.That(first.Streak.StreakBroken, Is.False);
            Assert.That(first.Transactions, Is.Empty);
            Assert.That(fx.Store.Persisted.Wins, Is.EqualTo(4));
            Assert.That(fx.Store.Persisted.RiverFull, Is.True);
            Assert.That(fx.Store.Persisted.ShieldAvailable, Is.False);

            StreakServiceResult second = fx.Service.ApplyHardFail(42, true);
            Assert.That(second.Streak.ShieldUsed, Is.False);
            Assert.That(second.Has(StreakServiceEvents.ShieldUsed), Is.False);
            Assert.That(second.Streak.StreakBroken, Is.True);
            Assert.That(second.Streak.RiverEnded, Is.True);
            Assert.That(fx.Store.Persisted.Wins, Is.EqualTo(0));
            Assert.That(fx.Store.Persisted.RiverFull, Is.False);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void HardFail_TheNextDay_HasANewShield()
        {
            var fx = new Fx(Record(Yesterday, 4, shield: false));

            StreakServiceResult r = fx.Service.ApplyHardFail(42, true);

            Assert.That(r.Streak.ShieldUsed, Is.True, "C12: una hoja nueva cada día local");
            Assert.That(fx.Store.Persisted.Wins, Is.EqualTo(4));
        }

        // ---- Cambio de día entre Load y Save (IStreakStore: regla conservadora) ----

        /// <summary>Pasa la medianoche (y el día de <c>daily</c>, como haría la cartera) justo antes de cada <c>Save</c>.</summary>
        private sealed class MidnightStore : IStreakStore
        {
            private readonly InMemoryStreakStore _inner;
            private readonly ManualClock _clock;

            public MidnightStore(InMemoryStreakStore inner, ManualClock clock)
            {
                _inner = inner;
                _clock = clock;
            }

            public bool PendingSave => _inner.PendingSave;

            public StreakRecord Load()
            {
                return _inner.Load();
            }

            public void Save(StreakRecord record)
            {
                _clock.Advance(TimeSpan.FromMinutes(1));
                _inner.SimulateDayChangeByAnotherModule();
                _inner.Save(record);
            }

            public bool Flush()
            {
                return _inner.Flush();
            }
        }

        private static (EconomyFixture eco, InMemoryStreakStore inner, StreakService service) AtMidnight(StreakRecord initial)
        {
            var eco = new EconomyFixture();
            eco.Clock.Set(new DateTime(2026, 10, 9, 4, 59, 30, DateTimeKind.Utc)); // 23:59:30 del 8 en Bogotá
            var inner = new InMemoryStreakStore(eco.Clock, initial);
            var service = new StreakService(new MidnightStore(inner, eco.Clock), eco.Wallet, eco.Clock, 10);
            return (eco, inner, service);
        }

        [Test]
        public void DayChangeBetweenLoadAndSave_TheMilestonePaidYesterdayCountsToday_AndIsPaidOnce()
        {
            var (eco, inner, service) = AtMidnight(Record(Today, 2));

            StreakServiceResult r = service.ApplyWin(43, true, 42, RewardTier.Normal);

            Assert.That(r.StreakSeeds, Is.EqualTo(300));
            Assert.That(eco.Wallet.Seeds, Is.EqualTo(300), "pagado una sola vez");
            Assert.That(inner.Persisted.DateLocal, Is.EqualTo("2026-10-09"));
            Assert.That(inner.Persisted.HitsToday, Is.EqualTo(new[] { 3 }), "conservadora: cuenta también hoy");
            Assert.That(inner.Persisted.SeedsToday, Is.EqualTo(300));
            Assert.That(inner.Persisted.ShieldAvailable, Is.True, "la hoja no cambió desde la carga");
            Assert.That(inner.Persisted.Wins, Is.EqualTo(3));
        }

        [Test]
        public void DayChangeBetweenLoadAndSave_WithYesterdaysShieldSpent_TheHardFailBreaks_ButTodaysShieldStays()
        {
            // El coste documentado en IStreakStore: las reglas ven la hoja de ayer gastada.
            var (_, inner, service) = AtMidnight(Record(Today, 4, shield: false));

            StreakServiceResult r = service.ApplyHardFail(43, true);

            Assert.That(r.Streak.ShieldUsed, Is.False);
            Assert.That(r.Streak.StreakBroken, Is.True);
            Assert.That(inner.Persisted.Wins, Is.EqualTo(0));
            Assert.That(inner.Persisted.DateLocal, Is.EqualTo("2026-10-09"));
            Assert.That(inner.Persisted.ShieldAvailable, Is.True, "el escudo de hoy no se gasta");
        }

        // ---- Salto del Boto en un 10k (K9, E34) ----

        [Test]
        public void SkipOnA10k_PaysItsBasket_WithoutTouchingTheStreak()
        {
            var fx = new Fx(Record(Today, 5, river: true));

            StreakServiceResult r = fx.Service.ApplySkip(20, true);

            Assert.That(r.LevelBasket.Kind, Is.EqualTo(BasketKind.Normal));
            Assert.That(r.Has(StreakServiceEvents.BasketOpened), Is.True);
            Assert.That(r.Has(StreakServiceEvents.StateChanged), Is.False);
            Assert.That(r.Streak.Event, Is.EqualTo(StreakEvent.None));
            Assert.That(Sources(r), Is.EqualTo(new[] { "basket", "stock:undo" }), "K3: 20 → 50 semillas + 3 Marea");
            Assert.That(r.BasketSeeds, Is.EqualTo(50));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(50));
            Assert.That(fx.Stock(BoosterKind.Undo), Is.EqualTo(3));
            Assert.That(fx.Store.WriteCount, Is.EqualTo(0), "C11: el salto ni avanza ni rompe la Crecida");
            Assert.That(fx.Store.Current.Wins, Is.EqualTo(5));
        }

        [Test]
        public void SkipOnA100_PaysTheGrandBasket()
        {
            var fx = new Fx();

            StreakServiceResult r = fx.Service.ApplySkip(100, true);

            Assert.That(r.LevelBasket.Kind, Is.EqualTo(BasketKind.Grand));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(150));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(1));
            Assert.That(fx.Stock(BoosterKind.Hint), Is.EqualTo(1));
            Assert.That(fx.Stock(BoosterKind.Undo), Is.EqualTo(3));
        }

        [TestCase(21, true)]
        [TestCase(20, false)]
        [TestCase(5, true)]
        public void SkipOffA10k_OrOutsideTheCampaign_PaysNothing(int level, bool campaign)
        {
            var fx = new Fx();

            StreakServiceResult r = fx.Service.ApplySkip(level, campaign);

            Assert.That(r.LevelBasket.IsNone, Is.True);
            Assert.That(r.Transactions, Is.Empty);
            Assert.That(r.Events, Is.EqualTo(StreakServiceEvents.None));
        }

        // ---- Fuera de campaña y antes del 25 (C1, Q-C2) ----

        [Test]
        public void OutsideTheCampaign_NothingHappens()
        {
            var fx = new Fx(Record(Today, 9, river: true));

            StreakServiceResult win = fx.Service.ApplyWin(30, false, 29, RewardTier.Normal);
            StreakServiceResult fail = fx.Service.ApplyHardFail(30, false);

            Assert.That(win.Streak.Ignore, Is.EqualTo(StreakIgnore.NotCampaign));
            Assert.That(win.FreeDoubleHarvest, Is.False, "Q-C2 (a): el Reto no lleva el x2 del río");
            Assert.That(win.LevelBasket.IsNone, Is.True, "el Reto no da Canasta");
            Assert.That(fail.Streak.Ignore, Is.EqualTo(StreakIgnore.NotCampaign));
            Assert.That(win.Transactions, Is.Empty);
            Assert.That(win.Events | fail.Events, Is.EqualTo(StreakServiceEvents.None));
            Assert.That(fx.Store.WriteCount, Is.EqualTo(0));
            Assert.That(fx.Store.Current.Wins, Is.EqualTo(9));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void Below25_TheStreakDoesNothing_ButTheLevelBasketStillPays()
        {
            var fx = new Fx(Record(Today, 2));

            StreakServiceResult at24 = fx.Service.ApplyWin(24, true, 23, RewardTier.Normal);
            StreakServiceResult fail = fx.Service.ApplyHardFail(24, true);
            Assert.That(at24.Streak.Ignore, Is.EqualTo(StreakIgnore.Locked));
            Assert.That(fail.Streak.Ignore, Is.EqualTo(StreakIgnore.Locked));
            Assert.That(at24.Transactions, Is.Empty);
            Assert.That(fx.Store.WriteCount, Is.EqualTo(0));

            StreakServiceResult at20 = fx.Service.ApplyWin(20, true, 19, RewardTier.Normal);
            Assert.That(at20.Streak.Ignore, Is.EqualTo(StreakIgnore.Locked));
            Assert.That(at20.LevelBasket.Kind, Is.EqualTo(BasketKind.Normal), "K1: la Canasta empieza en el 10");
            Assert.That(Sources(at20), Is.EqualTo(new[] { "basket", "stock:undo" }));
            Assert.That(fx.Store.Current.Wins, Is.EqualTo(2));
            Assert.That(fx.Store.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void AReplayed10k_DoesNotPayItsBasketAgain()
        {
            var fx = new Fx();

            StreakServiceResult r = fx.Service.ApplyWin(30, true, 45, RewardTier.Normal);

            Assert.That(r.LevelBasket.IsNone, Is.True, "K8: se deduce de max_level");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "la Crecida cuenta (barra 1) pero sin hito ni Canasta");
            Assert.That(fx.Store.Persisted.Wins, Is.EqualTo(1));
        }

        // ---- Kill switch (C13, Q-C11) ----

        [Test]
        public void KillSwitch_FreezesTheStreak_ButNotTheLevelBasket()
        {
            var config = StreakConfig.Defaults;
            config.BizFeatureWinStreak = false;
            var fx = new Fx(Record(Today, 9, river: true), config);

            StreakSnapshot peek = fx.Service.Peek(50, true);
            Assert.That(peek.Enabled, Is.False);
            Assert.That(peek.Active, Is.False, "sin barra");
            Assert.That(peek.FreeDoubleHarvestNext, Is.False);

            StreakServiceResult win = fx.Service.ApplyWin(50, true, 49, RewardTier.Normal);
            Assert.That(win.Streak.Ignore, Is.EqualTo(StreakIgnore.Disabled));
            Assert.That(win.FreeDoubleHarvest, Is.False, "sin x2 del río");
            Assert.That(win.DoubleHarvest, Is.Null);
            Assert.That(win.LevelBasket.Kind, Is.EqualTo(BasketKind.Normal), "la Canasta no es de la Crecida");
            Assert.That(Sources(win), Is.EqualTo(new[] { "basket", "stock:undo" }));

            StreakServiceResult fail = fx.Service.ApplyHardFail(51, true);
            Assert.That(fail.Streak.Ignore, Is.EqualTo(StreakIgnore.Disabled));
            Assert.That(fx.Store.WriteCount, Is.EqualTo(0));
            Assert.That(fx.Store.Current.Wins, Is.EqualTo(9), "Q-C11 (a): el estado se conserva");
            Assert.That(fx.Store.Current.RiverFull, Is.True);
        }

        // ---- Peek ----

        [Test]
        public void Peek_RollsTheDay_AndReportsTheState()
        {
            var fx = new Fx(Record(Yesterday, 4, river: true, shield: false, seeds: 300, hits: new[] { 3 }));

            StreakSnapshot s = fx.Service.Peek(60, true);

            Assert.That(s.DateLocal, Is.EqualTo(Today));
            Assert.That(s.Wins, Is.EqualTo(4));
            Assert.That(s.RiverFull, Is.True);
            Assert.That(s.ShieldAvailable, Is.True);
            Assert.That(s.SeedsToday, Is.EqualTo(0));
            Assert.That(s.HitPaid(3), Is.False);
            Assert.That(s.Enabled, Is.True);
            Assert.That(s.Active, Is.True);
            Assert.That(s.FreeDoubleHarvestNext, Is.True);
            Assert.That(fx.Service.Peek(60, false).Active, Is.False);
            Assert.That(fx.Service.Peek(24, true).FreeDoubleHarvestNext, Is.False);
            Assert.That(fx.Store.WriteCount, Is.EqualTo(0), "Peek no guarda");
        }

        [Test]
        public void Peek_HitPaid_FollowsTheMilestonesOfToday()
        {
            var fx = new Fx(Record(Today, 7, seeds: 350, hits: new[] { 3, 6 }));

            StreakSnapshot s = fx.Service.Peek(60, true);

            Assert.That(s.HitPaid(3), Is.True);
            Assert.That(s.HitPaid(6), Is.True);
            Assert.That(s.HitPaid(10), Is.False);
            Assert.That(s.HitPaid(4), Is.False);
            Assert.That(s.HitsMask, Is.EqualTo(0b011));
        }

        [Test]
        public void Constructor_RequiresAValidUnlockLevel()
        {
            var eco = new EconomyFixture();
            var store = new InMemoryStreakStore(eco.Clock);

            Assert.Throws<ArgumentOutOfRangeException>(() => new StreakService(store, eco.Wallet, eco.Clock, 0));
            Assert.Throws<ArgumentNullException>(() => new StreakService(null!, eco.Wallet, eco.Clock, 10));
        }
    }
}
