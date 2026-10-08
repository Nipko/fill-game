#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Levels;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los niveles 1-10 del paquete se cargan en una sesión jugable, no empiezan resueltos ni en Aguas estancadas
    /// (02 R12, R13) y el solver del núcleo los resuelve con el par del paquete (03 §5). El paquete no declara el modo de
    /// vertido: su par es el del modo por defecto, <see cref="PourModes.Default"/> = <see cref="PourMode.Run"/> (la racha
    /// que cabe, decisión D-005, que sustituye a D-001; <c>docs/dev/decisiones.md</c>). Además, lo que enseña cada fila de
    /// 02 §13 (y que comprueba <c>level-generator --handmade</c> al emitir el paquete): debuts y pares de riesgo del
    /// glosario §9, el vertido parcial 3 → 2 del nivel 2 (y, en el modo single de pruebas, "una dosis por vertido"), el
    /// color en 3 cuias del 5 y el vacío único del 6.
    /// </summary>
    public sealed class TutorialPackTests
    {
        // Glosario §9: ID → (índice de motor, nivel de debut) de los tintes del Estuario.
        private static readonly string[] TintIds = { "guanabana", "tapereba", "aguaje", "bacaba", "acai" };
        private static readonly int[] TintEngine = { 1, 3, 4, 7, 8 };
        private static readonly int[] TintDebut = { 4, 2, 1, 7, 1 };

        // Glosario §9: pares de riesgo (índices de motor); max_pares_riesgo = 0 en los niveles 1-40.
        private static readonly int[,] RiskPairs = { { 4, 5 }, { 5, 6 }, { 6, 13 }, { 13, 14 }, { 14, 4 }, { 3, 12 }, { 12, 11 }, { 11, 10 }, { 8, 9 }, { 1, 2 } };
        [Test]
        public void EveryLevel_StartsPlayable_AndSolverMatchesPar([Range(1, 10)] int level)
        {
            // El par del paquete se calculó en el modo por defecto (run, D-005); en el otro modo no vale (riesgo R-2).
            LevelDefinition def = TestPacks.Tutorial.Find(level)!;
            BoardState board = def.CreateBoard();
            var session = new LevelSession(board, true, PourModes.Default);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            SolveResult result = new AStarSolver().Solve(board, SolverOptions.ForPipeline(PourModes.Default));
            Assert.That(result.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(def.Par));

            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour move = result.GetMove(i);
                Assert.That(session.TryPour(move.Source, move.Target, out _), Is.True, "vertido " + i);
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.AppliedPours, Is.EqualTo(def.Par));
        }

        [Test]
        public void EveryLevel_UsesDebutedTints_AndNoRiskPair([Range(1, 10)] int level)
        {
            // Glosario §9: debut de cada tinte ≤ nivel; la paleta coincide con los colores de las cuias; 0 pares de riesgo.
            LevelDefinition def = TestPacks.Tutorial.Find(level)!;
            var colors = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < def.CuiaCount; i++)
            {
                foreach (int c in def.CopyDoses(i))
                {
                    colors.Add(c);
                }
            }

            var paletteColors = new System.Collections.Generic.HashSet<int>();
            foreach (string id in def.Palette)
            {
                int k = System.Array.IndexOf(TintIds, id);
                Assert.That(k, Is.GreaterThanOrEqualTo(0), "tinte del Estuario: " + id);
                Assert.That(TintDebut[k], Is.LessThanOrEqualTo(level), "debut de " + id);
                paletteColors.Add(TintEngine[k]);
            }

            Assert.That(paletteColors.SetEquals(colors), Is.True);
            int risk = 0;
            for (int p = 0; p < RiskPairs.GetLength(0); p++)
            {
                if (colors.Contains(RiskPairs[p, 0]) && colors.Contains(RiskPairs[p, 1]))
                {
                    risk++;
                }
            }

            Assert.That(risk, Is.EqualTo(0));
        }

        [Test]
        public void Level2_InSingleMode_TheOptimalSolutionRepeatsAPourThreeTimes()
        {
            // Modo de pruebas single (una dosis por vertido; D-005 lo deja solo para pruebas): una racha de 3 solo pasa de
            // una cuia a otra con 3 vertidos A→B, así que la solución óptima del solver repite 3 veces un mismo vertido. El
            // par del paquete es el de run (D-005): el de single es 9 (dato de tools/out/tutorial_v0_report.md, nivel 2).
            LevelDefinition def = TestPacks.Tutorial.Find(2)!;
            BoardState start = def.CreateBoard();
            int longestRun = 0;
            for (int i = 0; i < start.Count; i++)
            {
                longestRun = System.Math.Max(longestRun, start[i].TopRunLength);
            }

            Assert.That(longestRun, Is.EqualTo(3), "racha visible de 3 al empezar");
            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));
            Assert.That(result.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(9), "par single del informe del generador");
            Assert.That(result.MoveCount, Is.GreaterThan(def.Par), "single cuesta más vertidos que run");
            var uses = new int[start.Count * start.Count];
            int most = 0;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour move = result.GetMove(i);
                Assert.That(move.Amount, Is.EqualTo(1), "vertido " + i);
                int k = move.Source * start.Count + move.Target;
                uses[k]++;
                most = System.Math.Max(most, uses[k]);
            }

            Assert.That(most, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void Level2_TeachesThePartialPour_ThreeOntoTwo()
        {
            // 02 §13, nivel 2, en el modo por defecto run (D-005): vertido parcial de una racha de 3 sobre un hueco de 2,
            // disponible al empezar o tras un primer vertido óptimo, sin perder la resolubilidad (R9: el resto queda en el
            // origen). Independiente del camino que elija el solver: se prueba S0 y cada estado que deja un primer vertido
            // óptimo. El par del paquete es el de run.
            LevelDefinition def = TestPacks.Tutorial.Find(2)!;
            BoardState start = def.CreateBoard();
            SolveResult full = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            Assert.That(full.IsSolved, Is.True);
            Assert.That(full.MoveCount, Is.EqualTo(def.Par));
            bool found = OffersPartialThreeOntoTwo(start);
            for (int a = 0; a < start.Count && !found; a++)
            {
                for (int b = 0; b < start.Count && !found; b++)
                {
                    if (a == b || !PourRules.IsLegal(start, a, b))
                    {
                        continue;
                    }

                    BoardState next = PourRules.Apply(start, a, b, PourMode.Run);
                    SolveResult rest = new AStarSolver().Solve(next, SolverOptions.ForPipeline(PourMode.Run));
                    if (rest.IsSolved && rest.MoveCount == full.MoveCount - 1)
                    {
                        found = OffersPartialThreeOntoTwo(next);
                    }
                }
            }

            Assert.That(found, Is.True);
        }

        private static bool OffersPartialThreeOntoTwo(BoardState state)
        {
            for (int a = 0; a < state.Count; a++)
            {
                for (int b = 0; b < state.Count; b++)
                {
                    if (a == b || !PourRules.IsLegal(state, a, b) || state[a].TopRunLength != 3 || state[b].Space != 2)
                    {
                        continue;
                    }

                    BoardState after = PourRules.Apply(state, a, b, PourMode.Run);
                    if (after[a].Count == state[a].Count - 2
                        && new AStarSolver().Solve(after, SolverOptions.ForPipeline(PourMode.Run)).IsSolved)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        public void ColorSpread_FirstProblemColorArrivesInLevel5(int level, int maxSpread)
        {
            // 02 §13: el 5 trae el primer "color problema" repartido en 3 cuias (uno solo); antes, ninguno.
            LevelDefinition def = TestPacks.Tutorial.Find(level)!;
            int max = 0;
            int inThree = 0;
            for (int color = 1; color <= 14; color++)
            {
                int spread = 0;
                for (int i = 0; i < def.CuiaCount; i++)
                {
                    if (System.Array.IndexOf(def.CopyDoses(i), color) >= 0)
                    {
                        spread++;
                    }
                }

                max = System.Math.Max(max, spread);
                inThree += spread == 3 ? 1 : 0;
            }

            Assert.That(max, Is.EqualTo(maxSpread));
            Assert.That(inThree, Is.EqualTo(level == 5 ? 1 : 0));
        }

        [Test]
        public void Level6_HasASingleEmptyCuia()
        {
            // 02 §13: el 6 es la excepción tutorial con un solo vacío (5 = 4 + 1).
            LevelDefinition def = TestPacks.Tutorial.Find(6)!;
            int empties = 0;
            for (int i = 0; i < def.CuiaCount; i++)
            {
                empties += def.CopyDoses(i).Length == 0 ? 1 : 0;
            }

            Assert.That(def.CuiaCount, Is.EqualTo(5));
            Assert.That(empties, Is.EqualTo(1));
        }

        [TestCase(6, 3, 4)] // 02 §13, nivel 6: el taperebá suelto al único vacío lleva a Aguas estancadas en 1 vertido
        public void Level6_HasTheTutorialStuckPath(int level, int source, int target)
        {
            var session = new LevelSession(TestPacks.Tutorial.Find(level)!.CreateBoard(), true, PourModes.Default);
            Assert.That(session.TryPour(source, target, out PourResult r), Is.True);
            Assert.That(r.Status, Is.EqualTo(LevelStatus.Stuck));
        }
    }
}
