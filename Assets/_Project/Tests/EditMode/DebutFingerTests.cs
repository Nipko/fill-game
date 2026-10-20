#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Solver.Murky;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Dedo guía de los estrenos de la Hoja (43) y de la Tinaja (323) (tarea DC7; D-028 con P5 y D-029; 02 §9.1 M1: «dedo guía en
    /// el primer uso»; M13: «tinaja → su cuia y, después, su cuia → destino»): qué niveles lo llevan
    /// (<see cref="TutorialInstruction.FingerFor(int, string)"/>, por el <c>intro_mechanic</c>) en los paquetes reales, y qué
    /// vertido señala sobre los tableros reales del 43 y del 323 de la variante «a».
    /// </summary>
    public sealed class DebutFingerTests
    {
        [Test]
        public void FingerFor_GoesByTheDebut_NotByTheLevelNumber()
        {
            Assert.That(TutorialInstruction.FingerFor(43, TutorialInstruction.HojaIntroMechanic), Is.EqualTo(FingerLesson.HojaKey));
            Assert.That(TutorialInstruction.FingerFor(TinajaTutorial.DebutLevel, TinajaTutorial.IntroMechanic),
                Is.EqualTo(FingerLesson.TinajaScoop));
            Assert.That(TutorialInstruction.FingerFor(43, null), Is.EqualTo(FingerLesson.None), "sin el estreno, sin dedo");
            Assert.That(TutorialInstruction.FingerFor(TinajaTutorial.DebutLevel, null), Is.EqualTo(FingerLesson.None),
                "el 323 de la «b», sin tinaja");
            Assert.That(TutorialInstruction.FingerFor(283, "hoja_atada"), Is.EqualTo(FingerLesson.None), "la Hoja atada, sin dedo");
            Assert.That(TutorialInstruction.FingerFor(63, "matapi"), Is.EqualTo(FingerLesson.None));
            Assert.That(TutorialInstruction.FingerFor(1, null), Is.EqualTo(FingerLesson.FirstPour), "el tutorial sigue igual");
            Assert.That(TutorialInstruction.FingerFor(2, null), Is.EqualTo(FingerLesson.PartialRun));
            Assert.That(TutorialInstruction.FingerFor(12, null), Is.EqualTo(FingerLesson.None), "el 12, sin dedo (D-028, 61 a)");

            // HasFinger con el estreno dice lo mismo; HasFinger por número de nivel es solo el del tutorial (sin los estrenos).
            Assert.That(TutorialInstruction.HasFinger(43, TutorialInstruction.HojaIntroMechanic), Is.True);
            Assert.That(TutorialInstruction.HasFinger(TinajaTutorial.DebutLevel, TinajaTutorial.IntroMechanic), Is.True);
            Assert.That(TutorialInstruction.HasFinger(43, null) || TutorialInstruction.HasFinger(283, "hoja_atada"), Is.False);
            Assert.That(TutorialInstruction.HasFinger(1, null) && TutorialInstruction.HasFinger(2, null), Is.True);
            Assert.That(TutorialInstruction.HasFinger(43), Is.False, "por número de nivel, sin los estrenos");
        }

        [Test]
        public void InThePublishedPacks_OnlyLevels1_2And43_HaveAFinger_AndInVariantA_Also323()
        {
            int found43 = 0;
            foreach (LevelPack pack in TestPacks.Campaign)
            {
                foreach (LevelDefinition def in pack.Levels)
                {
                    FingerLesson lesson = TutorialInstruction.FingerFor(def.Level, def.IntroMechanic);
                    FingerLesson expected = def.Level == 1 ? FingerLesson.FirstPour
                        : def.Level == 2 ? FingerLesson.PartialRun
                        : def.Level == 43 ? FingerLesson.HojaKey
                        : def.Level == TinajaTutorial.DebutLevel && LevelCatalog.TinajaPublished ? FingerLesson.TinajaScoop
                        : FingerLesson.None;
                    Assert.That(lesson, Is.EqualTo(expected), "nivel " + def.Level);
                    found43 += def.Level == 43 ? 1 : 0;
                }
            }

            Assert.That(found43, Is.EqualTo(1), "el 43, una vez");
            int found323 = 0;
            foreach (LevelDefinition def in TestPacks.WorldVariantA(7).Levels)
            {
                FingerLesson lesson = TutorialInstruction.FingerFor(def.Level, def.IntroMechanic);
                Assert.That(lesson, Is.EqualTo(def.Level == TinajaTutorial.DebutLevel ? FingerLesson.TinajaScoop : FingerLesson.None),
                    "variante a, nivel " + def.Level + " (los niveles siguientes con tinaja, sin dedo)");
                found323 += def.Level == TinajaTutorial.DebutLevel ? 1 : 0;
            }

            Assert.That(found323, Is.EqualTo(1));
        }

        [Test]
        public void Level43_PointsAtAPourOfTheLeafFruitIntoTheLeafCuia()
        {
            LevelDefinition def = Find(43);
            BoardState board = def.CreateBoard();
            int hoja = IndexOf(board, MechanicType.Hoja);
            Assert.That(hoja, Is.GreaterThanOrEqualTo(0), "el 43 lleva la Hoja");
            var solver = new AStarSolver();
            Assert.That(TutorialFinger.TryChoose(FingerLesson.HojaKey, board, PourModes.Default, true, solver, out Pour pour), Is.True);
            Assert.That(pour.Target, Is.EqualTo(hoja), "a la cuia con hoja");
            Assert.That(board[pour.Source].TopColor, Is.EqualTo(board[hoja].MechanicKey), "de la fruta de la hoja");
            Assert.That(TutorialInstruction.IsHojaLesson(board, pour.Source, pour.Target), Is.True);
            Assert.That(pour.Amount, Is.EqualTo(PourRules.Amount(board, pour.Source, pour.Target, PourModes.Default)));

            // La abre y deja el tablero con salida.
            BoardState next = PourRules.Apply(board, pour.Source, pour.Target, PourModes.Default);
            Assert.That(next[hoja].Mechanic, Is.Not.EqualTo(MechanicType.Hoja), "la hoja se desprende");
            Assert.That(solver.Solve(next, SolverOptions.ForPipeline(PourModes.Default, next)).IsSolved, Is.True, "con salida");
            Assert.That(TutorialFinger.TryChoose(FingerLesson.HojaKey, next, PourModes.Default, true, solver, out _), Is.False,
                "abierta, no hay más lección");

            // Determinista.
            Assert.That(TutorialFinger.TryChoose(FingerLesson.HojaKey, board, PourModes.Default, true, new AStarSolver(), out Pour again), Is.True);
            Assert.That(again, Is.EqualTo(pour));
        }

        [Test]
        public void HojaLesson_OnlyALegalPourIntoACuiaThatStillHasTheLeaf()
        {
            LevelDefinition def = Find(43);
            BoardState board = def.CreateBoard();
            int hoja = IndexOf(board, MechanicType.Hoja);
            for (int s = 0; s < board.Count; s++)
            {
                for (int t = 0; t < board.Count; t++)
                {
                    bool expected = t == hoja && s != t && PourRules.IsLegal(board, s, t);
                    Assert.That(TutorialInstruction.IsHojaLesson(board, s, t), Is.EqualTo(expected), s + " → " + t);
                }
            }

            Assert.That(TutorialInstruction.IsHojaLesson(board, -1, hoja) || TutorialInstruction.IsHojaLesson(board, 0, 99), Is.False);
        }

        [Test]
        public void Level323VariantA_Step1ScoopsFromTheTinaja_Step2PoursFromItsCuia_BothSafeInEveryReality()
        {
            LevelDefinition def = TestPacks.WorldVariantA(7).Find(TinajaTutorial.DebutLevel)!;
            Assert.That(def.IntroMechanic, Is.EqualTo(TinajaTutorial.IntroMechanic));
            BoardState board = def.CreateBoard();
            int tinaja = IndexOf(board, MechanicType.Tinaja);
            int scoop = IndexOf(board, MechanicType.Cucharon);
            Assert.That(tinaja, Is.GreaterThanOrEqualTo(0));
            Assert.That(scoop, Is.GreaterThanOrEqualTo(0));
            var safe = new SafeMoves();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Assert.That(TutorialFinger.TryChooseTinaja(1, scoop, board, board, PourModes.Default, true, safe, out Pour scoopPour), Is.True);
            double step1Ms = watch.Elapsed.TotalMilliseconds;
            Assert.That(scoopPour.Source, Is.EqualTo(tinaja), "paso 1: de la tinaja");
            Assert.That(scoopPour.Target, Is.EqualTo(scoop), "a su cuia");
            Assert.That(scoopPour.Amount, Is.EqualTo(1), "sacar mueve una dosis");

            BoardState next = PourRules.Apply(board, scoopPour.Source, scoopPour.Target, PourModes.Default);
            watch.Restart();
            Assert.That(TutorialFinger.TryChooseTinaja(2, scoop, next, board, PourModes.Default, true, safe, out Pour pour), Is.True);
            double step2Ms = watch.Elapsed.TotalMilliseconds;
            Assert.That(pour.Source, Is.EqualTo(scoop), "paso 2: de su cuia");
            Assert.That(pour.Target, Is.Not.EqualTo(tinaja), "la tinaja nunca recibe");
            Assert.That(PourRules.IsLegal(next, pour.Source, pour.Target), Is.True);

            // Seguro en todas las realidades: tras él, SafeMoves aún encuentra vertidos seguros (o es victoria).
            BoardState after = PourRules.Apply(next, pour.Source, pour.Target, PourModes.Default);
            SolverOptions options = SafeMoves.ForPipeline(PourModes.Default, after);
            if (!after.IsWon())
            {
                var check = new SafeMoves();
                Assert.That(check.Find(after, options, board), Is.EqualTo(RealityStatus.Ok));
                Assert.That(check.Outcome, Is.EqualTo(SafeMovesOutcome.Found), "quedan vertidos seguros");
                Assert.That(check.Count, Is.GreaterThan(0));
            }
            TestContext.WriteLine("AF_FINGER 323: paso 1 " + scoopPour.Source + "→" + scoopPour.Target + "; paso 2 " + pour.Source
                + "→" + pour.Target + " (" + pour.Amount + "); elegirlos, " + step1Ms.ToString("0.0") + " y " + step2Ms.ToString("0.0")
                + " ms en el Editor (por eso va en un hilo: GameScreen.Cards)");

            // Sin el paso que toca (el paso 2 antes de sacar: su cuia está vacía), no hay dedo; ni con un paso que no existe.
            Assert.That(TutorialFinger.TryChooseTinaja(2, scoop, board, board, PourModes.Default, true, safe, out _), Is.False);
            Assert.That(TutorialFinger.TryChooseTinaja(3, scoop, board, board, PourModes.Default, true, safe, out _), Is.False);
            Assert.That(TutorialFinger.TryChoose(FingerLesson.TinajaScoop, board, PourModes.Default, true, new AStarSolver(), out _), Is.False,
                "la Tinaja no va por el A* sobre el tablero verdadero");
        }

        private static LevelDefinition Find(int level)
        {
            foreach (LevelPack pack in TestPacks.Campaign)
            {
                LevelDefinition? def = pack.Find(level);
                if (def != null)
                {
                    return def;
                }
            }

            Assert.Fail("sin el nivel " + level);
            return null!;
        }

        private static int IndexOf(BoardState board, MechanicType type)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Mechanic == type)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
