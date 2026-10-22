#nullable enable
using System.Collections.Generic;
using AF.Boosters;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La interfaz de la Crecida y la Canasta sin motor de UI (tarea B1b de la tanda 5): la línea de la Hoja escudo de Aguas
    /// estancadas (04 §6.5 y §6.6; <see cref="StuckModalModel.ShieldLineFor"/>), lo que llevan las barras del panel único de la
    /// victoria (<see cref="VictoryBarsPlan"/>; D-033, punto 10; 02 §6; nota B1a §6.1), su disposición por escalón
    /// (<see cref="VictoryBarsRule"/>), la tarjeta del 25 (04 §4; <see cref="LevelCardInfo.ForStreak"/>) y sus textos en es-419.
    /// </summary>
    public sealed class StreakUiTests
    {
        [SetUp]
        public void SetUp()
        {
            LocSetUpFixture.ConfigureEs419();
        }

        // ============================================================================================ Hoja escudo en el modal

        [Test]
        public void ShieldLine_ReadyOrUsed_OnlyWithTheStreakActiveAndAtLeastOneWin()
        {
            var config = new BoosterConfig();
            Assert.That(StuckModalModel.ShieldLineFor(Ctx(25, true, 4, true), config), Is.EqualTo(StuckShieldLine.Ready));
            Assert.That(StuckModalModel.ShieldLineFor(Ctx(25, true, 4, false), config), Is.EqualTo(StuckShieldLine.Used));
            Assert.That(StuckModalModel.ShieldLineFor(Ctx(25, true, 0, true), config), Is.EqualTo(StuckShieldLine.None), "con la barra a 0 no hay racha que romper ni hoja que gastar (06 §4.3)");
            Assert.That(StuckModalModel.ShieldLineFor(Ctx(24, false, 4, true), config), Is.EqualTo(StuckShieldLine.None), "antes del 25 (o en el Reto), sin Crecida");
            config.StuckShowShieldStatus = false;
            Assert.That(StuckModalModel.ShieldLineFor(Ctx(25, true, 4, true), config), Is.EqualTo(StuckShieldLine.None), "stuck_show_shield_status = false la apaga (04 §8)");
        }

        [Test]
        public void ShieldLine_GoesInTheModal_In1To30AndFrom31_WithTheWinsForTheUsedText()
        {
            var config = new BoosterConfig();
            StuckModalView early = StuckModalModel.Build(Ctx(27, true, 2, false), config);
            Assert.That(early.Footer, Is.EqualTo(StuckFooter.RestartFree), "en 1-30, el pie de siempre");
            Assert.That(early.ShieldLine, Is.EqualTo(StuckShieldLine.Used));
            Assert.That(early.ShieldWins, Is.EqualTo(2));

            StuckModalView late = StuckModalModel.Build(Ctx(34, true, 4, true), config);
            Assert.That(late.Footer, Is.EqualTo(StuckFooter.GiveUp));
            Assert.That(late.ShieldLine, Is.EqualTo(StuckShieldLine.Ready));

            StuckModalView none = StuckModalModel.Build(Ctx(34, true, 0, true), config);
            Assert.That(none.ShieldLine, Is.EqualTo(StuckShieldLine.None));
            Assert.That(none.ShieldWins, Is.EqualTo(0));
        }

        [Test]
        public void Texts_Es419_FromTheTable()
        {
            Assert.That(UiText.StuckShieldReady, Is.EqualTo("Hoja escudo lista: este fallo no rompe tu Crecida"), "04 §6.5");
            Assert.That(UiText.StuckShieldUsed(4), Is.EqualTo("Sin Hoja escudo hoy: este fallo rompe tu Crecida de 4"), "04 §6.5");
            Assert.That(UiText.ShieldToast, Is.EqualTo("Una hoja gigante te cubre: este fallo no rompe tu Crecida"), "04 §6.5");
            Assert.That(UiText.StreakBar(3), Is.EqualTo("Crecida 3/10"), "06 §4.3: «Crecida {n}/10»");
            Assert.That(UiText.BasketBar("Manaos", 7), Is.EqualTo("Canasta · Manaos 7/10"), "D-013: «Canasta · {sitio} k/10»");
            Assert.That(UiText.BasketBar(null, 7), Is.EqualTo("Canasta 7/10"));
            Assert.That(UiText.VictoryBarSeeds(300), Is.EqualTo("+300"));
            Assert.That(UiText.VictoryBarUnits(3), Is.EqualTo("×3"));
            Assert.That(UiText.Tutorial(LevelCardInfo.StreakIntro), Is.EqualTo(UiText.TutorialStreak));
            Assert.That(UiText.TutorialStreak, Does.StartWith("Cada victoria sube el río"));
        }

        // ============================================================================================ tarjeta del 25

        [Test]
        public void StreakCard_IsADebutCardWithTheSystemId_AndItsHeroIcon()
        {
            LevelCardInfo card = LevelCardInfo.ForStreak(25);
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.Level, Is.EqualTo(25));
            Assert.That(card.IntroMechanic, Is.EqualTo("win_streak"), "el ID del sistema (glosario §6.3)");
            Assert.That(LevelCardInfo.StreakIntro, Is.EqualTo(ProceduralArt.StreakHeroIconKey), "el icono grande de la tarjeta");
            Assert.That(LevelCardInfo.DebutMechanicType(card.IntroMechanic), Is.EqualTo(AF.Core.Mechanics.MechanicType.None), "no tapa ninguna cuia de estreno");
        }

        // ============================================================================================ barras de la victoria

        [Test]
        public void Bars_Before10_None_From10_Basket_From25_StreakToo_AndNeverInTheDaily()
        {
            var fx = new Fx();
            var plan = new VictoryBarsPlan();
            int level = 9;
            StreakServiceResult r = fx.Service.ApplyWin(level, true, level - 1, RewardTier.Normal);
            plan.Build(level, true, fx.Service.Peek(level, true), r, fx.Service.Config, fx.Service.Basket, 10, "Manaos");
            Assert.That(plan.BarCount, Is.EqualTo(0), "en 1-9, ninguna barra");

            level = 12;
            r = fx.Service.ApplyWin(level, true, level - 1, RewardTier.Normal);
            plan.Build(level, true, fx.Service.Peek(level, true), r, fx.Service.Config, fx.Service.Basket, 10, "Manaos");
            Assert.That(plan.Streak, Is.False, "la Crecida, desde el 25");
            Assert.That(plan.Basket, Is.True);
            Assert.That(plan.BasketCount, Is.EqualTo(2), "el 12 es el 2.º del tramo 11-20");
            Assert.That(plan.BasketOpens, Is.False);
            Assert.That(Icons(plan.BasketReward), Is.EqualTo(new[] { "Seed:50", "Undo:3" }), "la Canasta del 20, visible antes de abrir (06 §4.4)");

            level = 25;
            r = fx.Service.ApplyWin(level, true, level - 1, RewardTier.Normal);
            plan.Build(level, true, fx.Service.Peek(level, true), r, fx.Service.Config, fx.Service.Basket, 10, null);
            Assert.That(plan.Streak && plan.Basket, Is.True);
            Assert.That(plan.StreakFrom, Is.EqualTo(0));
            Assert.That(plan.StreakTo, Is.EqualTo(1));
            Assert.That(plan.StreakPaid, Is.False);
            Assert.That(plan.StreakMilestone, Is.EqualTo(3), "el chip: el siguiente hito y su premio (06 §4.3)");
            Assert.That(Icons(plan.StreakReward), Is.EqualTo(new[] { "Seed:300" }));
            Assert.That(plan.BasketPlace, Is.Null);

            plan.Build(level, false, fx.Service.Peek(level, false), r, fx.Service.Config, fx.Service.Basket, 10, "Manaos");
            Assert.That(plan.BarCount, Is.EqualTo(0), "el Reto del río no las lleva (Q-C2, E25)");
        }

        [Test]
        public void Bars_Hit3Paid_ChipFlies_TheLevelBasketOpensIn10k_AndPaidSeedsAreCounted()
        {
            var fx = new Fx();
            var plan = new VictoryBarsPlan();
            StreakServiceResult r = null!;
            for (int level = 25; level <= 27; level++)
            {
                r = fx.Service.ApplyWin(level, true, level - 1, RewardTier.Normal);
            }

            plan.Build(27, true, fx.Service.Peek(27, true), r, fx.Service.Config, fx.Service.Basket, 10, "Manaos");
            Assert.That(plan.StreakFrom, Is.EqualTo(2));
            Assert.That(plan.StreakTo, Is.EqualTo(3));
            Assert.That(plan.StreakPaid, Is.True);
            Assert.That(plan.StreakMilestone, Is.EqualTo(3));
            Assert.That(Icons(plan.StreakReward), Is.EqualTo(new[] { "Seed:300" }));
            Assert.That(plan.PaidSeeds, Is.EqualTo(300));
            Assert.That(Icons(plan.BasketReward), Is.EqualTo(new[] { "Seed:50", "Hint:1" }), "la Canasta del 30: Morpho (06 §4.4)");

            for (int level = 28; level <= 30; level++)
            {
                r = fx.Service.ApplyWin(level, true, level - 1, RewardTier.Normal);
            }

            plan.Build(30, true, fx.Service.Peek(30, true), r, fx.Service.Config, fx.Service.Basket, 10, "Manaos");
            Assert.That(plan.BasketCount, Is.EqualTo(10));
            Assert.That(plan.BasketOpens, Is.True, "en el 10k, la Canasta se abre sola (D-033, punto 8)");
            Assert.That(plan.StreakPaid, Is.True, "y el hito 6 (su Canasta) en la misma victoria (Q-C13)");
            Assert.That(Icons(plan.StreakReward), Is.EqualTo(new[] { "Seed:50", "ExtraCuia:1" }), "la Canasta del hito 6: la del 40 (Q-C3, Q-C5)");
            Assert.That(plan.PaidSeeds, Is.EqualTo(r.StreakSeeds + r.BasketSeeds));
            Assert.That(plan.PaidSeeds, Is.EqualTo(100));
        }

        [Test]
        public void Bars_Hit10_FillsTo10_AndTheNextChip_SkipsWhatTodayWouldNotPay()
        {
            var record = new StreakRecord { DateLocal = "2026-10-08", Wins = 9, ShieldAvailable = true, SeedsToday = 350 };
            record.HitsToday.Add(3);
            record.HitsToday.Add(6);
            var fx = new Fx(record);
            var plan = new VictoryBarsPlan();
            StreakServiceResult r = fx.Service.ApplyWin(40, true, 39, RewardTier.Normal);
            plan.Build(40, true, fx.Service.Peek(40, true), r, fx.Service.Config, fx.Service.Basket, 10, "Óbidos");
            Assert.That(plan.StreakFrom, Is.EqualTo(9));
            Assert.That(plan.StreakTo, Is.EqualTo(10), "la barra llega a 10 en la victoria; se vacía en el HUD siguiente [P]");
            Assert.That(plan.StreakPaid, Is.True);
            Assert.That(Icons(plan.StreakReward), Is.EqualTo(new[] { "ExtraCuia:1" }), "hito 10: 1 Cuia extra (05 §2.4)");
            Assert.That(plan.Shield, Is.True, "la hoja de hoy, al pie");

            // Con el 3 y el 6 cobrados hoy, el chip de una barra en 0 apunta al 10 (no promete lo que el día no paga; nota B1a §6.1).
            StreakSnapshot s = fx.Service.Peek(41, true);
            Assert.That(VictoryBarsPlan.NextMilestone(0, in s, fx.Service.Config, out _), Is.EqualTo(0), "los tres cobrados hoy: sin chip");
            var twoPaid = new StreakRecord { DateLocal = "2026-10-08", Wins = 0, ShieldAvailable = true, SeedsToday = 350 };
            twoPaid.HitsToday.Add(3);
            twoPaid.HitsToday.Add(6);
            s = new Fx(twoPaid).Service.Peek(41, true);
            Assert.That(VictoryBarsPlan.NextMilestone(0, in s, fx.Service.Config, out _), Is.EqualTo(10));
        }

        [Test]
        public void NextMilestone_Hit3_OnlyWithRoomUnderTheDailyCap()
        {
            var record = new StreakRecord { DateLocal = "2026-10-08", Wins = 1, ShieldAvailable = true, SeedsToday = 450 };
            var fx = new Fx(record);
            StreakSnapshot s = fx.Service.Peek(30, true);
            Assert.That(VictoryBarsPlan.NextMilestone(1, in s, fx.Service.Config, out int seeds), Is.EqualTo(3));
            Assert.That(seeds, Is.EqualTo(150), "600 − 450: el pago parcial del tope (Q-C10)");

            var full = new StreakRecord { DateLocal = "2026-10-08", Wins = 1, ShieldAvailable = true, SeedsToday = 600 };
            fx = new Fx(full);
            s = fx.Service.Peek(30, true);
            Assert.That(VictoryBarsPlan.NextMilestone(1, in s, fx.Service.Config, out _), Is.EqualTo(6), "con el tope lleno, el 3 no paga: el chip pasa al 6");
        }

        [Test]
        public void Rule_FullWidthUpToThree_HalfWidthFromStep2_TwoAndChevronInStep3()
        {
            Assert.That(VictoryBarsRule.HalfWidth(0, 2), Is.False);
            Assert.That(VictoryBarsRule.HalfWidth(1, 2), Is.False);
            Assert.That(VictoryBarsRule.HalfWidth(2, 2), Is.True, "escalón ≥ 2: Crecida y Canasta a media anchura (D-033, punto 10)");
            Assert.That(VictoryBarsRule.HalfWidth(3, 1), Is.False, "una sola barra va a lo ancho");
            Assert.That(VictoryBarsRule.Visible(0, 3), Is.EqualTo(3), "máximo 3 barras visibles (02 §6)");
            Assert.That(VictoryBarsRule.Chevron(0, 4), Is.True, "el resto, en un chevron");
            Assert.That(VictoryBarsRule.Visible(3, 3), Is.EqualTo(2), "escalón 3: 2 barras…");
            Assert.That(VictoryBarsRule.Chevron(3, 3), Is.True, "…y chevron");
            Assert.That(VictoryBarsRule.Chevron(3, 2), Is.False);
        }

        [Test]
        public void LogbookRow_IsTheThirdBar_AndCountsInTheWorstHeightOnlyWhereItIsVisible()
        {
            // Cierre de B11 (D-033, añadido del 2026-10-20, puntos 5 y 10): la fila «Bitácora · elige tu premio» es la tercera barra.
            var plan = new VictoryBarsPlan();
            plan.Build(26, true, null, null, null, null, 0, null);
            Assert.That(plan.BarCount, Is.EqualTo(0));
            plan.Logbook = true;
            Assert.That(plan.Extra, Is.EqualTo(1));
            Assert.That(plan.BarCount, Is.EqualTo(1));
            plan.Clear();
            Assert.That(plan.Logbook, Is.False, "se rearma en cada victoria");
            Assert.That(plan.Extra, Is.EqualTo(0));

            Assert.That(VictoryBars.LogbookRowDp, Is.GreaterThanOrEqualTo(48f), "un objetivo de 48 dp (02 §3.3)");
            for (int step = 0; step < GameScreen.VictoryCompactRow; step++)
            {
                Assert.That(VictoryBars.LogbookWorstExtra(step), Is.EqualTo(VictoryBars.LogbookRowDp + VictoryBars.GapDp), "escalón " + step);
            }

            Assert.That(VictoryBars.LogbookWorstExtra(GameScreen.VictoryCompactRow), Is.EqualTo(0f), "escalón 3: iría tras el chevron, no cuenta");
        }

        // ---- Ayudas ----

        private sealed class Fx
        {
            public Fx(StreakRecord? initial = null)
            {
                Economy = new EconomyFixture();
                Service = new StreakService(new InMemoryStreakStore(Economy.Clock, initial), Economy.Wallet, Economy.Clock, 10);
            }

            public EconomyFixture Economy { get; }

            public StreakService Service { get; }
        }

        private static string[] Icons(List<RewardItem> items)
        {
            var s = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                s[i] = items[i].Icon + ":" + items[i].Amount;
            }

            return s;
        }

        private static StuckContext Ctx(int level, bool active, int wins, bool shield)
        {
            return new StuckContext
            {
                Level = level,
                RescueMinLevel = 31,
                UndoFree = 3,
                HasHistory = true,
                Seeds = 1000,
                UndoRefillPrice = 30,
                UndoRefillAmount = 3,
                Canoes = 5,
                RescueMaxPerLevel = 1,
                RescueCheck = level >= 31 ? RescueCheck.Solvable : RescueCheck.NotChecked,
                RescuePrice = level >= 31 ? 600 : 0,
                RescueBasePrice = level >= 31 ? 600 : 0,
                BalsaOk = true,
                Geo = GeoTier.T3,
                StreakActive = active,
                StreakWins = wins,
                ShieldAvailable = shield,
            };
        }
    }
}
