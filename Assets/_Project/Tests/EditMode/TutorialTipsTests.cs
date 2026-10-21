#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las ayudas del tutorial de la tarea B11 (02 §13; 06 §2.2): <see cref="TutorialTips"/> (tooltips del 6, el 8 y el 11 y el
    /// dedo de la Cuia extra del 8) y <see cref="TutorialFingerSetup"/> (el paso previo del dedo del 2). La tarjeta de las Canoas
    /// del 31, en <c>CanoesCardFlowTests</c> (parche B11-flow).
    /// </summary>
    public sealed class TutorialTipsTests
    {
        [Test]
        public void RestartTip_Levels6And8_Of02_13()
        {
            Assert.That(TutorialTips.RestartTipLevels, Is.EqualTo(new[] { 6, 8 }));
            for (int level = 1; level <= 40; level++)
            {
                Assert.That(TutorialTips.ShowsRestartTip(level), Is.EqualTo(level == 6 || level == 8), "nivel " + level);
            }
        }

        [Test]
        public void RestartTip_WithoutLives_DropsTheCanoeSentence_D038()
        {
            Assert.That(TutorialTips.RestartTipKey(true), Is.EqualTo(TutorialTips.RestartFreeCanoeKey), "04 §2.7: el texto canon entero");
            Assert.That(TutorialTips.RestartTipKey(false), Is.EqualTo(TutorialTips.RestartFreeKey), "D-038: sin canoas en los avisos");
        }

        [Test]
        public void DoubleHarvestTip_OnlyOnTheFirstLevelWithIt_06_2_2()
        {
            Assert.That(TutorialTips.ShowsDoubleHarvestTip(11, 11, false), Is.True, "06 §2.2, fila 11");
            Assert.That(TutorialTips.ShowsDoubleHarvestTip(12, 11, false), Is.False, "una sola vez");
            Assert.That(TutorialTips.ShowsDoubleHarvestTip(10, 11, false), Is.False);
            Assert.That(TutorialTips.ShowsDoubleHarvestTip(11, 11, true), Is.False, "el Reto del río no presenta nada");
            Assert.That(TutorialTips.ShowsDoubleHarvestTip(15, 15, false), Is.True, "sigue a ads_rv_double_harvest_from_level");
        }

        [Test]
        public void ExtraCuiaFinger_FirstUseInLevel8_04_2_2()
        {
            Assert.That(TutorialTips.ShowsExtraCuiaFinger(8, 8, false, 0, 1), Is.True, "02 §13 y 06 §2.2: dedo guía en su primer uso");
            Assert.That(TutorialTips.ShowsExtraCuiaFinger(8, 8, false, 1, 1), Is.False, "ya se usó en el nivel");
            Assert.That(TutorialTips.ShowsExtraCuiaFinger(8, 8, false, 0, 0), Is.False, "sin la del stock (la de Capi)");
            Assert.That(TutorialTips.ShowsExtraCuiaFinger(9, 8, false, 0, 1), Is.False, "solo en el nivel del desbloqueo");
            Assert.That(TutorialTips.ShowsExtraCuiaFinger(8, 8, true, 0, 1), Is.False, "no en el Reto");
        }

        [Test]
        public void Keys_AreUnique()
        {
            string[] keys = TutorialTips.Keys;
            Assert.That(keys, Is.Unique);
            Assert.That(keys, Has.Member(TutorialTips.RestartFreeCanoeKey));
            Assert.That(keys, Has.Member(TutorialTips.RestartFreeKey));
            Assert.That(keys, Has.Member(TutorialTips.DoubleHarvestKey));
        }

        [Test]
        public void FingerSetup_Level2_PointsAtThePourThatOpensTheLesson()
        {
            // 02 §13, fila 2: en el tablero inicial la lección (una racha de 3 sobre un hueco de 2) no es legal; el paso previo la
            // deja posible y, tras él, el dedo de la lección sí tiene vertido (D-005, modo run).
            LevelCatalog catalog = LevelCatalog.Create(new[] { TestPacks.Tutorial });
            BoardState start = catalog.Find(2)!.CreateBoard();
            var solver = new AStarSolver();
            Assert.That(TutorialFinger.TryChoose(FingerLesson.PartialRun, start, PourMode.Run, true, solver, out _), Is.False,
                "al empezar, la lección no es legal (prueba en el teléfono del 2026-10-20, observación 3)");
            Assert.That(TutorialFingerSetup.TryChoose(start, PourMode.Run, true, solver, out Pour setup), Is.True);
            Assert.That(PourRules.IsLegal(start, setup.Source, setup.Target), Is.True);
            BoardState next = PourRules.Apply(start, setup.Source, setup.Target, PourMode.Run);
            Assert.That(TutorialFinger.TryChoose(FingerLesson.PartialRun, next, PourMode.Run, true, solver, out Pour lesson), Is.True);
            Assert.That(TutorialInstruction.IsPartialRunLesson(next, lesson.Source, lesson.Target, PourMode.Run), Is.True);
            Assert.That(TutorialFingerSetup.TryChoose(next, PourMode.Run, true, solver, out _), Is.False, "con la lección legal, no hay paso previo");
        }
    }
}
