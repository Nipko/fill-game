#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Reglas de la Crecida y la Hoja escudo (B1a, A3; 06 §4.3 con su pseudocódigo; 05 §2.4; 04 §5.2, §6.2 <c>failHard</c> y §8;
    /// nota <c>docs/dev/crecida-canasta.md</c> §1.1): hitos 3/6/10 una vez por día local, tope de 600 con pago parcial, río
    /// crecido y su x2 decidido con el estado anterior, Hoja escudo diaria, el día que solo avanza, campaña desde el 25, y un
    /// oráculo: una copia de <c>on_win</c>/<c>on_fail</c> de <c>tools/econ_sim.py</c> sobre secuencias pseudoaleatorias fijas.
    /// </summary>
    public sealed class StreakRulesTests
    {
        private const int L = 25; // streak_unlock_level

        private static string Day(int n)
        {
            return new DateTime(2026, 10, 1).AddDays(n).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static StreakRules NewRules(StreakConfig? config = null)
        {
            var rules = new StreakRules(new StreakRecord(), config);
            rules.RollDay(Day(0));
            return rules;
        }

        private static StreakOutcome Wins(StreakRules rules, int count, ref int level)
        {
            StreakOutcome last = default;
            for (int i = 0; i < count; i++)
            {
                last = rules.OnVictory(level++, true);
            }

            return last;
        }

        // ---- Debut y alcance (06 §4.3; 05 §2.4; Q-C2; Q-C11) ----

        [Test]
        public void Level24_DoesNotCount_Level25_Does()
        {
            StreakRules rules = NewRules();

            StreakOutcome at24 = rules.OnVictory(24, true);
            Assert.That(at24.Counted, Is.False);
            Assert.That(at24.Ignore, Is.EqualTo(StreakIgnore.Locked));
            Assert.That(rules.Record.Wins, Is.EqualTo(0));
            Assert.That(rules.IsActive(24, true), Is.False);

            StreakOutcome at25 = rules.OnVictory(25, true);
            Assert.That(at25.Counted, Is.True);
            Assert.That(at25.Wins, Is.EqualTo(1));
            Assert.That(rules.IsActive(25, true), Is.True);

            rules.Record.Wins = 2;
            StreakOutcome fail24 = rules.OnHardFail(24, true);
            Assert.That(fail24.Ignore, Is.EqualTo(StreakIgnore.Locked));
            Assert.That(rules.Record.Wins, Is.EqualTo(2));
            Assert.That(rules.Record.ShieldAvailable, Is.True);
        }

        [Test]
        public void TheDailyChallenge_NeitherAdvancesNorBreaks_NorGetsTheFreeDouble()
        {
            StreakRules rules = NewRules();
            int level = L;
            Wins(rules, 10, ref level); // río crecido, barra a 0
            Wins(rules, 2, ref level);

            StreakOutcome win = rules.OnVictory(level, false);
            Assert.That(win.Ignore, Is.EqualTo(StreakIgnore.NotCampaign));
            Assert.That(win.FreeDoubleHarvest, Is.False, "Q-C2 (a): el Reto no lleva el x2 gratis del río");
            Assert.That(rules.FreeDoubleHarvestOn(level, false), Is.False);
            Assert.That(rules.FreeDoubleHarvestOn(level, true), Is.True);
            StreakOutcome fail = rules.OnHardFail(level, false);
            Assert.That(fail.Ignore, Is.EqualTo(StreakIgnore.NotCampaign));
            Assert.That(rules.Record.Wins, Is.EqualTo(2));
            Assert.That(rules.Record.RiverFull, Is.True);
            Assert.That(rules.Record.ShieldAvailable, Is.True);

            var into = new List<Transaction>();
            win.AppendTransactions(into, RewardTier.Normal);
            Assert.That(into, Is.Empty);
        }

        [Test]
        public void KillSwitchOff_FreezesTheStreak()
        {
            var config = new StreakConfig { BizFeatureWinStreak = false };
            StreakRules rules = NewRules(config);
            rules.Record.Wins = 4;
            rules.Record.RiverFull = true;

            Assert.That(rules.OnVictory(30, true).Ignore, Is.EqualTo(StreakIgnore.Disabled));
            Assert.That(rules.OnHardFail(30, true).Ignore, Is.EqualTo(StreakIgnore.Disabled));
            Assert.That(rules.Record.Wins, Is.EqualTo(4), "Q-C11 (a): se congela, no se borra");
            Assert.That(rules.Record.RiverFull, Is.True);
            Assert.That(rules.FreeDoubleHarvestOn(30, true), Is.False, "sin Crecida no hay x2 del río");
        }

        // ---- Hitos (05 §2.4; 06 §4.3) ----

        [Test]
        public void Milestones_3_6_10_PayTheLadder()
        {
            StreakRules rules = NewRules();
            int level = L;

            StreakOutcome one = rules.OnVictory(level++, true);
            Assert.That(one.Milestone, Is.EqualTo(0));
            Assert.That(one.MilestonePaid, Is.False);

            StreakOutcome three = Wins(rules, 2, ref level);
            Assert.That(three.Wins, Is.EqualTo(3));
            Assert.That(three.Milestone, Is.EqualTo(3));
            Assert.That(three.MilestonePaid, Is.True);
            Assert.That(three.HitSeeds, Is.EqualTo(300));
            Assert.That(three.MilestoneRewardType, Is.EqualTo("seeds"));
            Assert.That(three.MilestoneRewardValue, Is.EqualTo("300"));
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(300));

            StreakOutcome six = Wins(rules, 3, ref level); // gana el 30
            Assert.That(six.Milestone, Is.EqualTo(6));
            Assert.That(six.Basket.Kind, Is.EqualTo(BasketKind.Streak));
            Assert.That(six.Basket.Seeds, Is.EqualTo(50));
            Assert.That(six.Basket, Is.EqualTo(BasketRules.HitSix(30, BasketConfig.Defaults)), "Q-C3 (b): la próxima Canasta de nivel (40)");
            Assert.That(six.Basket.Stock, Is.EqualTo(StockDelta.Of(BoosterKind.ExtraCuia, 1)));
            Assert.That(six.MilestoneRewardType, Is.EqualTo("chest"));
            Assert.That(six.MilestoneRewardValue, Is.EqualTo("{\"kind\":\"streak\",\"seeds\":50,\"extra_cuia\":1}"));
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(350));

            StreakOutcome nine = Wins(rules, 3, ref level);
            Assert.That(nine.Wins, Is.EqualTo(9));
            Assert.That(nine.RiverFull, Is.False);

            StreakOutcome ten = rules.OnVictory(level++, true);
            Assert.That(ten.Milestone, Is.EqualTo(10));
            Assert.That(ten.MilestonePaid, Is.True);
            Assert.That(ten.ExtraCuia, Is.True);
            Assert.That(ten.BarEmptied, Is.True);
            Assert.That(ten.Wins, Is.EqualTo(0), "al pagar el hito 10 la barra vuelve a 0");
            Assert.That(ten.RiverFull, Is.True);
            Assert.That(ten.FreeDoubleHarvest, Is.False, "la victoria que paga el hito 10 no lleva el x2 (estado anterior)");
            Assert.That(ten.MilestoneRewardType, Is.EqualTo("extra_cuia+rio_crecido"));
            Assert.That(ten.SeedsPaid, Is.EqualTo(0), "el hito 10 no da semillas");
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(350), "ni la Cuia extra ni el río cuentan en el tope");

            StreakOutcome next = rules.OnVictory(level++, true);
            Assert.That(next.FreeDoubleHarvest, Is.True, "río crecido: Cosecha doble sin anuncio");
            Assert.That(next.Wins, Is.EqualTo(1));
            Assert.That(rules.Record.HitsToday, Is.EqualTo(new[] { 3, 6, 10 }));
        }

        [Test]
        public void Transactions_ArePaidThroughTheWallet()
        {
            var fx = new EconomyFixture();
            StreakRules rules = NewRules();
            int level = L;
            var all = new List<Transaction>();
            for (int i = 0; i < 12; i++)
            {
                rules.OnVictory(level++, true).AppendTransactions(all, RewardTier.Hard);
            }

            // 3 → 300 (streak) · 6 → Canasta 50 (streak) + comodín · 10 → Cuia extra · 11 y 12 → x2 del río (60 + 60, double_harvest).
            var earned = new Dictionary<SeedSource, int>();
            foreach (Transaction t in all)
            {
                TransactionResult r = fx.Wallet.Apply(t);
                Assert.That(r.IsApplied, Is.True, t.Kind.ToString());
                if (r.SeedsDelta > 0)
                {
                    SeedSource source = t.Kind == TransactionKind.DoubleHarvest ? SeedSource.DoubleHarvest : t.Source;
                    earned[source] = (earned.TryGetValue(source, out int n) ? n : 0) + r.SeedsDelta;
                }
            }

            Assert.That(earned[SeedSource.Streak], Is.EqualTo(350));
            Assert.That(earned[SeedSource.DoubleHarvest], Is.EqualTo(120), "x2 de dos victorias Difícil (60 + 60)");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(470));
            Assert.That(fx.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(1 + 1), "Canasta del hito 6 (ranura 40: Cuia extra) + hito 10");
        }

        [Test]
        public void EachMilestone_OncePerLocalDay_TheBarEmptiesAgainWithoutAnotherCuia()
        {
            StreakRules rules = NewRules();
            int level = L;
            Wins(rules, 10, ref level);

            StreakOutcome three = Wins(rules, 3, ref level);
            Assert.That(three.Milestone, Is.EqualTo(3));
            Assert.That(three.MilestonePaid, Is.False, "streak_hits_once_per_day: el hito 3 ya se pagó hoy");
            Assert.That(three.HitSeeds, Is.EqualTo(0));
            StreakOutcome six = Wins(rules, 3, ref level);
            Assert.That(six.MilestonePaid, Is.False);
            Assert.That(six.Basket.IsNone, Is.True);
            StreakOutcome ten = Wins(rules, 4, ref level);
            Assert.That(ten.BarEmptied, Is.True, "la barra vuelve a vaciarse");
            Assert.That(ten.ExtraCuia, Is.False, "sin otra Cuia extra el mismo día");
            Assert.That(ten.MilestonePaid, Is.False);
            Assert.That(ten.RiverFull, Is.True);
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(350));

            Assert.That(rules.RollDay(Day(1)), Is.True);
            Assert.That(rules.Record.Wins, Is.EqualTo(0), "el cambio de día no toca la barra");
            Assert.That(rules.Record.RiverFull, Is.True, "ni el río crecido: no expiran por reloj (04 §8)");
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(0));
            Assert.That(rules.Record.HitsToday, Is.Empty);
            StreakOutcome threeTomorrow = Wins(rules, 3, ref level);
            Assert.That(threeTomorrow.MilestonePaid, Is.True);
            Assert.That(threeTomorrow.HitSeeds, Is.EqualTo(300));
        }

        [Test]
        public void TheDailyCap_PaysPartially_WhenMilestonesRepeat()
        {
            var config = new StreakConfig { StreakHitsOncePerDay = false };
            StreakRules rules = NewRules(config);
            int level = L;

            Assert.That(Wins(rules, 3, ref level).HitSeeds, Is.EqualTo(300));
            Assert.That(Wins(rules, 3, ref level).Basket.Seeds, Is.EqualTo(50));
            StreakOutcome ten = Wins(rules, 4, ref level);
            Assert.That(ten.ExtraCuia, Is.True);
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(350));

            StreakOutcome three = Wins(rules, 3, ref level);
            Assert.That(three.MilestonePaid, Is.True, "con streak_hits_once_per_day = false el hito se repite");
            Assert.That(three.HitSeeds, Is.EqualTo(250), "Q-C10 (a): pago parcial hasta 600");
            Assert.That(three.CappedSeeds, Is.EqualTo(50));
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(600));

            StreakOutcome six = Wins(rules, 3, ref level);
            Assert.That(six.MilestonePaid, Is.True);
            Assert.That(six.Basket.Seeds, Is.EqualTo(0), "el tope ya está lleno");
            Assert.That(six.CappedSeeds, Is.EqualTo(50));
            Assert.That(six.Basket.Stock.IsZero, Is.False, "el comodín no cuenta en el tope");
            var into = new List<Transaction>();
            six.AppendTransactions(into, RewardTier.Normal);
            Assert.That(six.FreeDoubleHarvest, Is.True, "el río sigue crecido desde el hito 10");
            Assert.That(into.Count, Is.EqualTo(2), "el x2 del río y el comodín; ninguna semilla de la Crecida");
            Assert.That(into[0].Kind, Is.EqualTo(TransactionKind.DoubleHarvest));
            Assert.That(into[1].Kind, Is.EqualTo(TransactionKind.GrantStock));

            StreakOutcome tenAgain = Wins(rules, 4, ref level);
            Assert.That(tenAgain.ExtraCuia, Is.True, "repetible: otra Cuia extra");
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(600));
        }

        [Test]
        public void ASmallerCap_AndALowerHit3_ComeFromTheConfig()
        {
            var config = new StreakConfig { StreakDailyCap = 300, StreakMilestones = new StreakMilestones(280) };
            StreakRules rules = NewRules(config);
            int level = L;

            Assert.That(Wins(rules, 3, ref level).HitSeeds, Is.EqualTo(280));
            StreakOutcome six = Wins(rules, 3, ref level);
            Assert.That(six.Basket.Seeds, Is.EqualTo(20));
            Assert.That(six.CappedSeeds, Is.EqualTo(30));
        }

        // ---- Hoja escudo y fallo duro (04 §6.2 failHard; 05 §2.4 on_hard_fail; Q-C6; Q-C7) ----

        [Test]
        public void TheShield_CoversTheFirstHardFailOfTheDay()
        {
            StreakRules rules = NewRules();
            int level = L;
            Wins(rules, 2, ref level);

            StreakOutcome covered = rules.OnHardFail(level, true);
            Assert.That(covered.ShieldUsed, Is.True);
            Assert.That(covered.StreakBroken, Is.False);
            Assert.That(covered.WinsBefore, Is.EqualTo(2), "streak_at_use de af_streak_shield_used");
            Assert.That(rules.Record.Wins, Is.EqualTo(2), "la barra no cambia");
            Assert.That(rules.Record.ShieldAvailable, Is.False);

            StreakOutcome broken = rules.OnHardFail(level, true);
            Assert.That(broken.ShieldUsed, Is.False);
            Assert.That(broken.StreakLost, Is.True);
            Assert.That(broken.StreakBroken, Is.True);
            Assert.That(rules.Record.Wins, Is.EqualTo(0));

            Assert.That(rules.RollDay(Day(1)), Is.True);
            Assert.That(rules.Record.ShieldAvailable, Is.True, "una Hoja escudo por día local");
        }

        [Test]
        public void TwoShieldsPerDayInTheConfig_StillGiveOne()
        {
            var config = new StreakConfig { StreakShieldPerDay = 2 };
            config.Validate();
            StreakRules rules = NewRules(config);
            int level = L;
            Wins(rules, 4, ref level);

            Assert.That(rules.OnHardFail(level, true).ShieldUsed, Is.True);
            StreakOutcome second = rules.OnHardFail(level, true);
            Assert.That(second.ShieldUsed, Is.False, "Q-C6 (a): en la 1.0, una sola hoja al día con un bool");
            Assert.That(second.StreakBroken, Is.True);
        }

        [Test]
        public void WithTheBarAtZero_AFailEndsTheRiverWithoutSpendingTheShield()
        {
            StreakRules rules = NewRules();
            int level = L;
            Wins(rules, 10, ref level);
            Assert.That(rules.Record.Wins, Is.EqualTo(0));
            Assert.That(rules.Record.RiverFull, Is.True);

            StreakOutcome fail = rules.OnHardFail(level, true);
            Assert.That(fail.ShieldUsed, Is.False);
            Assert.That(fail.RiverEnded, Is.True);
            Assert.That(fail.StreakLost, Is.False);
            Assert.That(fail.StreakBroken, Is.True, "se perdió el río crecido (Q-C7 (a))");
            Assert.That(rules.Record.RiverFull, Is.False);
            Assert.That(rules.Record.ShieldAvailable, Is.True, "la hoja no se gasta con la barra a 0");
        }

        [Test]
        public void AFailWithNothingToLose_IsNotABrokenStreak()
        {
            StreakRules rules = NewRules();

            StreakOutcome fail = rules.OnHardFail(L, true);
            Assert.That(fail.Counted, Is.True);
            Assert.That(fail.ShieldUsed, Is.False);
            Assert.That(fail.StreakBroken, Is.False, "Q-C7 (a): streak_broken solo si se perdió la barra o el río");
            Assert.That(rules.Record.ShieldAvailable, Is.True);
        }

        [Test]
        public void TheRiverStaysWithTheShield_AndWithTheBarAtZeroAfterTheShield()
        {
            StreakRules rules = NewRules();
            int level = L;
            Wins(rules, 12, ref level); // río crecido y barra 2

            Assert.That(rules.OnHardFail(level, true).ShieldUsed, Is.True);
            Assert.That(rules.Record.RiverFull, Is.True, "la hoja protege la barra y el río");
            Assert.That(rules.OnVictory(level++, true).FreeDoubleHarvest, Is.True);
        }

        // ---- El día local (06 §4.3 on_app_open; 04 §10.1) ----

        [Test]
        public void TheDay_OnlyMovesForward()
        {
            var rules = new StreakRules(new StreakRecord());
            Assert.That(rules.Record.ShieldAvailable, Is.False, "sin día fijado aún");
            Assert.That(rules.RollDay(Day(5)), Is.True);
            Assert.That(rules.Record.DateLocal, Is.EqualTo("2026-10-06"));
            Assert.That(rules.RollDay(Day(5)), Is.False, "mismo día");

            rules.Record.Wins = 3;
            rules.OnHardFail(L, true);
            rules.Record.SeedsToday = 300;
            rules.Record.HitsToday.Add(3);

            Assert.That(rules.RollDay(Day(4)), Is.False, "reloj atrasado: no repone la hoja ni los hitos");
            Assert.That(rules.Record.ShieldAvailable, Is.False);
            Assert.That(rules.Record.SeedsToday, Is.EqualTo(300));
            Assert.That(rules.Record.HitsToday, Is.EqualTo(new[] { 3 }));
            Assert.That(rules.Record.DateLocal, Is.EqualTo("2026-10-06"));

            Assert.That(rules.RollDay(Day(40)), Is.True, "días sin jugar: la barra sigue (no expira por reloj)");
            Assert.That(rules.Record.Wins, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => rules.RollDay(""));
        }

        [Test]
        public void OnVictoryAndOnHardFail_DoNotAllocate()
        {
            var config = new StreakConfig { StreakHitsOncePerDay = false };
            var rules = new StreakRules(new StreakRecord(), config);
            string[] days = { Day(0), Day(1), Day(2) };
            int sink = 0;
            TestDelegate call = () =>
            {
                for (int d = 0; d < days.Length; d++)
                {
                    rules.RollDay(days[d]);
                    for (int i = 0; i < 40; i++)
                    {
                        StreakOutcome o = rules.OnVictory(L + i, true);
                        sink += o.SeedsPaid + o.Basket.Stock.Get(BoosterKind.Hint) + (o.FreeDoubleHarvest ? 1 : 0);
                        if (i % 7 == 6)
                        {
                            sink += rules.OnHardFail(L + i, true).ShieldUsed ? 1 : 0;
                        }
                    }
                }
            };
            call();
            rules.Record.DateLocal = null; // la segunda llamada vuelve a cambiar de día
            Assert.That(call, Is.Not.AllocatingGCMemory(), "sin LINQ ni asignaciones en OnVictory/OnHardFail");
            Assert.That(sink, Is.GreaterThan(0));
        }

        [Test]
        public void TheQueries_DoNotAllocate()
        {
            BasketConfig config = BasketConfig.Defaults;
            int sink = 0;
            TestDelegate call = () =>
            {
                for (int level = 1; level <= 400; level++)
                {
                    sink += BasketRules.For(level, 10, config).Seeds;
                    sink += BasketRules.HitSix(level, config).Stock.Get(BoosterKind.Undo);
                }
            };
            call();
            Assert.That(call, Is.Not.AllocatingGCMemory(), "BasketRules.For y HitSix no asignan memoria");
            Assert.That(sink, Is.GreaterThan(0));
        }

        // ---- Oráculo: tools/econ_sim.py (on_win / on_fail / pay_streak_hit / start_day) ----

        [TestCase(true, 600, 300)]
        [TestCase(false, 600, 300)]
        [TestCase(true, 300, 300)]
        [TestCase(false, 300, 200)]
        [TestCase(false, 900, 250)]
        public void MatchesTheEconSimLadder_OnFixedPseudoRandomSequences(bool oncePerDay, int cap, int hit3)
        {
            var config = new StreakConfig { StreakHitsOncePerDay = oncePerDay, StreakDailyCap = cap, StreakMilestones = new StreakMilestones(hit3) };
            for (uint seed = 1; seed <= 24; seed++)
            {
                var rng = new XorShift32(seed * 2654435761u);
                var rules = new StreakRules(new StreakRecord(), config);
                var sim = new EconSimStreak(oncePerDay, cap, hit3, 50);
                int day = 0;
                int level = L;
                rules.RollDay(Day(day));
                sim.StartDay();
                int paid = 0;
                int cuias = 0;
                int chests = 0;
                int doubles = 0;
                for (int step = 0; step < 600; step++)
                {
                    uint r = rng.Next() % 100;
                    string what;
                    if (r < 72)
                    {
                        what = "win";
                        StreakOutcome o = rules.OnVictory(level++, true);
                        sim.OnWin();
                        paid += o.SeedsPaid;
                        cuias += o.ExtraCuia ? 1 : 0;
                        chests += o.Basket.IsNone ? 0 : 1;
                        doubles += o.FreeDoubleHarvest ? 1 : 0;
                    }
                    else if (r < 92)
                    {
                        what = "fail";
                        rules.OnHardFail(level, true);
                        sim.OnFail();
                    }
                    else
                    {
                        what = "day";
                        day++;
                        rules.RollDay(Day(day));
                        sim.StartDay();
                    }

                    string at = "semilla " + seed + ", paso " + step + " (" + what + ")";
                    StreakRecord s = rules.Record;
                    Assert.That(s.Wins, Is.EqualTo(sim.Streak), at + ": barra");
                    Assert.That(s.RiverFull, Is.EqualTo(sim.RioCrecido), at + ": río crecido");
                    Assert.That(s.ShieldAvailable, Is.EqualTo(sim.ShieldAvailable), at + ": hoja escudo");
                    Assert.That(s.SeedsToday, Is.EqualTo(sim.StreakSeedsToday), at + ": semillas de hoy");
                    Assert.That(Mask(s.HitsToday), Is.EqualTo(sim.HitsMask), at + ": hitos de hoy");
                    Assert.That(s.HitsToday.Count, Is.EqualTo(sim.HitsCount), at + ": sin hitos repetidos");
                    Assert.That(paid, Is.EqualTo(sim.IncomeCrecida), at + ": semillas de la Crecida");
                    Assert.That(cuias, Is.EqualTo(sim.Cuias), at + ": Cuias extra");
                    Assert.That(chests, Is.EqualTo(sim.Chests), at + ": Canastas del hito 6");
                    Assert.That(doubles, Is.EqualTo(sim.Doubles), at + ": Cosechas dobles del río");
                }

                Assert.That(sim.Chests + sim.Cuias, Is.GreaterThan(0), "la secuencia llega a algún hito");
            }
        }

        private static int Mask(List<int> hits)
        {
            int mask = 0;
            foreach (int h in hits)
            {
                mask |= h == 3 ? 1 : h == 6 ? 2 : h == 10 ? 4 : 8;
            }

            return mask;
        }

        /// <summary>
        /// Copia de la Crecida de <c>tools/econ_sim.py</c> (escenario B: <c>streak_hit10</c> = 0, modelo <c>B-2026-10-06e</c>):
        /// <c>on_win</c> (el x2 del río va antes de sumar la racha), <c>pay_streak_hit</c>, <c>on_fail</c> y la parte de la
        /// Crecida de <c>start_day</c>, con el perfil siempre ≥ 25 (la simulación da la hoja solo si el nivel ya era ≥ 25 al
        /// empezar el día; el GDD la repone en cada día: nota §1.2). No copia la Canasta: su ciclo va desfasado (nota §1.2).
        /// </summary>
        private sealed class EconSimStreak
        {
            private readonly bool _once;
            private readonly int _cap;
            private readonly int _hit3;
            private readonly int _chestSeeds;
            private readonly HashSet<int> _hits = new HashSet<int>();

            public EconSimStreak(bool once, int cap, int hit3, int chestSeeds)
            {
                _once = once;
                _cap = cap;
                _hit3 = hit3;
                _chestSeeds = chestSeeds;
            }

            public int Streak { get; private set; }

            public bool RioCrecido { get; private set; }

            public bool ShieldAvailable { get; private set; }

            public int StreakSeedsToday { get; private set; }

            public int IncomeCrecida { get; private set; }

            public int Cuias { get; private set; }

            public int Chests { get; private set; }

            public int Doubles { get; private set; }

            public int HitsCount => _hits.Count;

            public int HitsMask => (_hits.Contains(3) ? 1 : 0) | (_hits.Contains(6) ? 2 : 0) | (_hits.Contains(10) ? 4 : 0);

            public void StartDay()
            {
                ShieldAvailable = true;
                StreakSeedsToday = 0;
                _hits.Clear();
            }

            public void OnWin()
            {
                if (RioCrecido)
                {
                    Doubles++;
                }

                Streak += 1;
                if (Streak == 3)
                {
                    PayStreakHit(3, _hit3);
                }
                else if (Streak == 6)
                {
                    if (!(_once && _hits.Contains(6)))
                    {
                        PayStreakHit(6, _chestSeeds);
                        Chests++;
                    }
                }
                else if (Streak >= 10)
                {
                    if (!(_once && _hits.Contains(10)))
                    {
                        _hits.Add(10);
                        Cuias++;
                    }

                    RioCrecido = true;
                    Streak = 0;
                }
            }

            public void OnFail()
            {
                if (Streak >= 1 && ShieldAvailable)
                {
                    ShieldAvailable = false;
                }
                else
                {
                    Streak = 0;
                    RioCrecido = false;
                }
            }

            private void PayStreakHit(int hit, int amount)
            {
                if (_once && _hits.Contains(hit))
                {
                    return;
                }

                int room = _cap - StreakSeedsToday;
                int amt = Math.Max(0, Math.Min(amount, room));
                if (amt > 0)
                {
                    IncomeCrecida += amt;
                    StreakSeedsToday += amt;
                }

                _hits.Add(hit);
            }
        }

        /// <summary>Generador determinista propio (xorshift32): las secuencias son siempre las mismas.</summary>
        private struct XorShift32
        {
            private uint _state;

            public XorShift32(uint seed)
            {
                _state = seed == 0 ? 0x9E3779B9u : seed;
            }

            public uint Next()
            {
                uint x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }
        }
    }
}
