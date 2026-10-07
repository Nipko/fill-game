#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Búsqueda exhaustiva (BFS sobre estados canónicos) que sirve de oráculo al solver: no usa heurística ni el orden de
    /// A*, solo <see cref="PourRules"/>. Para tableros pequeños (tests compartidos y lotes de
    /// <c>dotnet/AF.Core.Tests/DotnetOnly</c>); asigna memoria libremente y no es para producción.
    /// </summary>
    internal static class ExhaustiveSearch
    {
        /// <summary>Clave canónica exacta (sin colisiones): los <see cref="Cuia.Packed"/> ordenados.</summary>
        public static string Key(BoardState state)
        {
            var packed = new ulong[state.Count];
            for (int i = 0; i < packed.Length; i++)
            {
                packed[i] = state[i].Packed;
            }

            Array.Sort(packed);
            var sb = new StringBuilder(packed.Length * 12);
            for (int i = 0; i < packed.Length; i++)
            {
                sb.Append(packed[i].ToString(CultureInfo.InvariantCulture)).Append(',');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Par por BFS: mínimo de vertidos hasta la victoria (R12) usando <c>movimientos_utiles</c> del modo; -1 si no hay
        /// solución. Con <paramref name="futileRuleEnabled"/> = false explora todos los vertidos legales (el par real).
        /// </summary>
        public static int Par(BoardState start, PourMode mode, bool futileRuleEnabled)
        {
            var seen = new HashSet<string>();
            seen.Add(Key(start));
            var frontier = new List<BoardState>();
            frontier.Add(start);
            var buffer = new Pour[PourRules.MaxPours];
            int depth = 0;
            while (frontier.Count > 0)
            {
                for (int i = 0; i < frontier.Count; i++)
                {
                    if (frontier[i].IsWon())
                    {
                        return depth;
                    }
                }

                var next = new List<BoardState>();
                for (int i = 0; i < frontier.Count; i++)
                {
                    BoardState s = frontier[i];
                    int count = PourRules.CollectUsefulMoves(s, futileRuleEnabled, mode, buffer);
                    for (int k = 0; k < count; k++)
                    {
                        BoardState t = PourRules.Apply(s, buffer[k].Source, buffer[k].Target, mode);
                        if (seen.Add(Key(t)))
                        {
                            next.Add(t);
                        }
                    }
                }

                frontier = next;
                depth++;
            }

            return -1;
        }

        /// <summary>
        /// Grafo de estados canónicos alcanzables desde <paramref name="start"/> con todos los vertidos legales del modo, y
        /// la distancia exacta de cada uno a la victoria (<see cref="int.MaxValue"/> si no tiene salida).
        /// </summary>
        public static Graph Explore(BoardState start, PourMode mode, int maxStates)
        {
            var index = new Dictionary<string, int>();
            var states = new List<BoardState>();
            var edges = new List<List<int>>();
            index.Add(Key(start), 0);
            states.Add(start);
            edges.Add(new List<int>());
            var buffer = new Pour[PourRules.MaxPours];
            for (int s = 0; s < states.Count; s++)
            {
                if (states.Count > maxStates)
                {
                    throw new InvalidOperationException("El tablero tiene más de " + maxStates + " estados alcanzables: no es un tablero pequeño.");
                }

                int count = PourRules.CollectUsefulMoves(states[s], false, mode, buffer);
                for (int k = 0; k < count; k++)
                {
                    BoardState t = PourRules.Apply(states[s], buffer[k].Source, buffer[k].Target, mode);
                    string key = Key(t);
                    if (!index.TryGetValue(key, out int target))
                    {
                        target = states.Count;
                        index.Add(key, target);
                        states.Add(t);
                        edges.Add(new List<int>());
                    }

                    edges[s].Add(target);
                }
            }

            // Distancia a la victoria: BFS hacia atrás desde los estados ganados.
            var reverse = new List<int>[states.Count];
            for (int s = 0; s < states.Count; s++)
            {
                reverse[s] = new List<int>();
            }

            for (int s = 0; s < states.Count; s++)
            {
                for (int e = 0; e < edges[s].Count; e++)
                {
                    reverse[edges[s][e]].Add(s);
                }
            }

            var distance = new int[states.Count];
            var queue = new Queue<int>();
            for (int s = 0; s < states.Count; s++)
            {
                distance[s] = int.MaxValue;
                if (states[s].IsWon())
                {
                    distance[s] = 0;
                    queue.Enqueue(s);
                }
            }

            while (queue.Count > 0)
            {
                int s = queue.Dequeue();
                for (int e = 0; e < reverse[s].Count; e++)
                {
                    int p = reverse[s][e];
                    if (distance[p] == int.MaxValue)
                    {
                        distance[p] = distance[s] + 1;
                        queue.Enqueue(p);
                    }
                }
            }

            return new Graph(states, edges, distance);
        }

        /// <summary>Estados alcanzables, aristas (vertidos legales) y distancia a la victoria.</summary>
        internal sealed class Graph
        {
            public Graph(List<BoardState> states, List<List<int>> edges, int[] distance)
            {
                States = states;
                Edges = edges;
                Distance = distance;
            }

            public List<BoardState> States { get; }

            public List<List<int>> Edges { get; }

            public int[] Distance { get; }
        }
    }

    /// <summary>
    /// Solver A* en los dos modos de vertido (D-001; docs/dev/decisiones.md §2.1.3 y criterio A4): el par de A*
    /// (w = 1) coincide con el de la búsqueda exhaustiva, con y sin la poda de fútiles; <c>H</c> y <c>H_single</c> son
    /// admisibles y consistentes en todos los estados alcanzables; <see cref="PourMode.Run"/> conserva el comportamiento
    /// anterior. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class AStarSolverPourModeTests
    {
        private const int MaxSmallStates = 5000;

        /// <summary>
        /// Tableros pequeños (cap 4) con su par en cada modo, comprobado en cada ejecución contra
        /// <see cref="ExhaustiveSearch"/>. -1 = irresoluble.
        /// </summary>
        private static readonly object[] SmallBoards =
        {
            // Nivel 1 de 02 §13 (índices de motor del ejemplo de v0): par 3 en los dos modos.
            new object[] { "tutorial_1", new[] { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, new int[0] }, 3, 3 },
            // Dos rachas de 3 cruzadas: en Run cada racha viaja en un vertido; en Single, dosis a dosis.
            new object[] { "rachas_3", new[] { new[] { 1, 2, 2, 2 }, new[] { 2, 1, 1, 1 }, new int[0] }, 3, 7 },
            // Alternado sin rachas al empezar: Run aprovecha las rachas que se van formando; Single necesita un vertido más.
            new object[] { "alternado", new[] { new[] { 1, 2, 1, 2 }, new[] { 2, 1, 2, 1 }, new int[0], new int[0] }, 7, 8 },
            // Sin salida en los dos modos (03 §5: agotar el espacio prueba la irresolubilidad).
            new object[] { "sin_salida", new[] { new[] { 2, 1, 1, 2 }, new[] { 2, 1, 1, 2 }, new int[0] }, -1, -1 },
            // Nivel 2 del tutorial v0 (racha de 3 sobre un hueco de 2).
            new object[] { "tutorial_2", new[] { new[] { 4, 8, 4, 4 }, new[] { 3, 3, 3, 4 }, new[] { 3, 8, 8, 8 }, new int[0], new int[0] }, 6, 9 },
        };

        private static BoardState FourCapBoard(int[][] cuias)
        {
            var built = new Cuia[cuias.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                built[i] = Cuia.Create(4, cuias[i]);
            }

            return new BoardState(built);
        }

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        /// <summary>Rehace la solución con <see cref="PourRules"/> en el modo dado y comprueba cantidades y victoria.</summary>
        private static void AssertSolutionWins(BoardState start, SolveResult result, PourMode mode)
        {
            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                Assert.That(PourRules.Amount(state, pour.Source, pour.Target, mode), Is.EqualTo(pour.Amount), "vertido " + i);
                if (mode == PourMode.Single)
                {
                    Assert.That(pour.Amount, Is.EqualTo(1), "D-001: vertido " + i);
                }

                state = PourRules.Apply(state, pour.Source, pour.Target, mode);
            }

            Assert.That(state.IsWon(), Is.True);
        }

        [TestCaseSource(nameof(SmallBoards))]
        public void SmallBoard_AStarParMatchesExhaustiveSearch_InBothModes(string name, int[][] cuias, int parRun, int parSingle)
        {
            // decisiones.md A4: par de A* (w = 1, con poda de fútiles) = par del BFS exhaustivo sin poda, en cada modo.
            BoardState start = FourCapBoard(cuias);
            var solver = new AStarSolver();
            for (int m = 0; m < 2; m++)
            {
                PourMode mode = m == 0 ? PourMode.Run : PourMode.Single;
                int expected = mode == PourMode.Run ? parRun : parSingle;

                int exhaustive = ExhaustiveSearch.Par(start, mode, false);
                int exhaustivePruned = ExhaustiveSearch.Par(start, mode, true);
                SolveResult pruned = solver.Solve(start, SolverOptions.ForPipeline(mode));
                SolveResult unpruned = solver.Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, mode));

                Assert.That(exhaustive, Is.EqualTo(expected), name + " " + mode + ": BFS");
                Assert.That(exhaustivePruned, Is.EqualTo(expected), name + " " + mode + ": BFS con poda");
                if (expected < 0)
                {
                    Assert.That(pruned.Status, Is.EqualTo(SolveStatus.Unsolvable), name + " " + mode);
                    Assert.That(unpruned.Status, Is.EqualTo(SolveStatus.Unsolvable), name + " " + mode);
                    continue;
                }

                Assert.That(pruned.MoveCount, Is.EqualTo(expected), name + " " + mode + ": A*");
                Assert.That(unpruned.MoveCount, Is.EqualTo(expected), name + " " + mode + ": A* sin poda");
                AssertSolutionWins(start, pruned, mode);
                AssertSolutionWins(start, unpruned, mode);
            }
        }

        [TestCaseSource(nameof(SmallBoards))]
        public void SmallBoard_HeuristicsAreAdmissibleAndConsistent_OnEveryReachableState(string name, int[][] cuias, int parRun, int parSingle)
        {
            // 03 §5 y decisiones.md §2.1.3: H ≤ distancia y H(S) ≤ 1 + H(S') en todos los estados alcanzables, en cada modo;
            // H_single solo con Single.
            BoardState start = FourCapBoard(cuias);
            for (int m = 0; m < 2; m++)
            {
                PourMode mode = m == 0 ? PourMode.Run : PourMode.Single;
                ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxSmallStates);
                int startDistance = graph.Distance[0];
                Assert.That(startDistance == int.MaxValue ? -1 : startDistance, Is.EqualTo(mode == PourMode.Run ? parRun : parSingle), name + " " + mode);
                for (int s = 0; s < graph.States.Count; s++)
                {
                    BoardState state = graph.States[s];
                    int h = SolverHeuristic.Estimate(state, mode);
                    if (graph.Distance[s] != int.MaxValue)
                    {
                        Assert.That(h, Is.LessThanOrEqualTo(graph.Distance[s]), name + " " + mode + " admisible: " + state);
                    }

                    for (int e = 0; e < graph.Edges[s].Count; e++)
                    {
                        BoardState next = graph.States[graph.Edges[s][e]];
                        Assert.That(h, Is.LessThanOrEqualTo(1 + SolverHeuristic.Estimate(next, mode)), name + " " + mode + " consistente: " + state);
                    }
                }
            }
        }

        [Test]
        public void Single_ParNeverExceedsTheDosesMovedByTheOptimalRunSolution()
        {
            // decisiones.md §2.1.3: un vertido Run de k dosis equivale a k vertidos Single seguidos, así que
            // par(Single) ≤ Σ cantidades de la solución óptima en Run, y resoluble en Run implica resoluble en Single.
            for (int b = 0; b < SmallBoards.Length; b++)
            {
                var row = (object[])SmallBoards[b];
                BoardState start = FourCapBoard((int[][])row[1]);
                SolveResult run = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
                SolveResult single = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));
                if (!run.IsSolved)
                {
                    continue;
                }

                int doses = 0;
                for (int i = 0; i < run.MoveCount; i++)
                {
                    doses += run.GetMove(i).Amount;
                }

                Assert.That(single.IsSolved, Is.True, (string)row[0]);
                Assert.That(single.MoveCount, Is.LessThanOrEqualTo(doses), (string)row[0]);
                Assert.That(single.MoveCount, Is.GreaterThanOrEqualTo(SolverHeuristic.Estimate(start, PourMode.Single)), (string)row[0]);
            }
        }

        [Test]
        public void Single_TutorialLevel1_ParThree_EveryPourMovesOneDose()
        {
            // 02 §13 nivel 1 y D-001: el par es 3 también en Single (1→2, 0→1, 2→0), cada vertido de una dosis.
            BoardState start = new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));

            Assert.That(result.MoveCount, Is.EqualTo(3));
            AssertSolutionWins(start, result, PourMode.Single);
        }

        [Test]
        public void Single_RunOfThree_IsMovedDoseByDose()
        {
            // D-001: con una racha de 3 sobre una vacía, la solución Single contiene 3 vertidos iguales seguidos donde Run
            // usa uno de 3 dosis.
            BoardState start = new BoardState(new[] { C(4, 1, 2, 2, 2), C(4, 2, 1, 1, 1), Cuia.Empty(4) });

            SolveResult run = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            SolveResult single = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));

            Assert.That(run.GetMove(0).Amount, Is.EqualTo(3));
            Assert.That(single.MoveCount, Is.EqualTo(7));
            int longestRepeat = 1;
            int current = 1;
            for (int i = 1; i < single.MoveCount; i++)
            {
                Pour previous = single.GetMove(i - 1);
                Pour pour = single.GetMove(i);
                current = previous.Source == pour.Source && previous.Target == pour.Target ? current + 1 : 1;
                longestRepeat = Math.Max(longestRepeat, current);
            }

            Assert.That(longestRepeat, Is.GreaterThanOrEqualTo(3));
            AssertSolutionWins(start, single, PourMode.Single);
        }

        [Test]
        public void LegacyPipeline_IsRun_SameMovesAndCounters()
        {
            // Run conserva el comportamiento anterior: SolverOptions.Pipeline (heredado) = ForPipeline(Run).
            BoardState start = FourCapBoard(new[]
            {
                new[] { 4, 4, 7, 7 }, new[] { 3, 1, 8, 7 }, new[] { 4, 4, 3, 1 }, new[] { 1, 8, 3, 7 }, new[] { 1, 3, 8, 8 },
                new int[0], new int[0],
            });

            SolveResult legacy = new AStarSolver().Solve(start, SolverOptions.Pipeline);
            SolveResult run = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            SolveResult legacyCtor = new AStarSolver().Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, true));

            Assert.That(SolverOptions.Pipeline.PourMode, Is.EqualTo(PourMode.Run));
            Assert.That(new SolverOptions(10, 1, true).PourMode, Is.EqualTo(PourMode.Run));
            Assert.That(run.CopyMoves(), Is.EqualTo(legacy.CopyMoves()));
            Assert.That(legacyCtor.CopyMoves(), Is.EqualTo(legacy.CopyMoves()));
            Assert.That(run.Expanded, Is.EqualTo(legacy.Expanded));
            Assert.That(run.StoredStates, Is.EqualTo(legacy.StoredStates));
        }

        [Test]
        public void ReusedSolver_SwitchingModes_MatchesFreshSolvers()
        {
            // El modo es de cada búsqueda: alternar Single y Run con la misma instancia da lo mismo que instancias nuevas.
            BoardState start = FourCapBoard(new[]
            {
                new[] { 3, 3, 4, 4 }, new[] { 8, 3, 8, 3 }, new[] { 8, 4, 4, 8 }, new int[0], new int[0],
            });
            var solver = new AStarSolver(8);

            SolveResult single1 = solver.Solve(start, SolverOptions.ForPipeline(PourMode.Single));
            SolveResult run = solver.Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            SolveResult single2 = solver.Solve(start, SolverOptions.ForPipeline(PourMode.Single));
            SolveResult freshSingle = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));
            SolveResult freshRun = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(single1.CopyMoves(), Is.EqualTo(freshSingle.CopyMoves()));
            Assert.That(single2.CopyMoves(), Is.EqualTo(freshSingle.CopyMoves()));
            Assert.That(single2.Expanded, Is.EqualTo(freshSingle.Expanded));
            Assert.That(run.CopyMoves(), Is.EqualTo(freshRun.CopyMoves()));
            Assert.That(run.Expanded, Is.EqualTo(freshRun.Expanded));
        }

        [Test]
        public void Single_WeightTwo_FindsAValidSolution_NeverShorterThanPar()
        {
            // 03 §5: w = 2 (pista en cliente) también en Single.
            BoardState start = FourCapBoard(new[]
            {
                new[] { 8, 4, 8, 8 }, new[] { 1, 4, 1, 1 }, new[] { 4, 3, 3, 4 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0],
            });

            SolveResult optimal = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));
            SolveResult hint = new AStarSolver().Solve(start, new SolverOptions(40000, 2, true, PourMode.Single));

            Assert.That(optimal.IsSolved, Is.True);
            Assert.That(hint.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(hint.MoveCount, Is.GreaterThanOrEqualTo(optimal.MoveCount));
            AssertSolutionWins(start, hint, PourMode.Single);
        }

        [Test]
        public void Single_KillSwitchOff_KeepsTheParOnUniformCapacities()
        {
            // decisiones.md §2.1.2: con capacidades uniformes la poda de fútiles no cambia el par en Single.
            BoardState start = FourCapBoard(new[]
            {
                new[] { 8, 1, 4, 4 }, new[] { 3, 8, 8, 1 }, new[] { 4, 3, 3, 1 }, new[] { 1, 4, 8, 3 }, new int[0],
            });

            SolveResult pruned = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Single));
            SolveResult classic = new AStarSolver().Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, PourMode.Single));

            Assert.That(pruned.IsSolved, Is.True);
            Assert.That(classic.MoveCount, Is.EqualTo(pruned.MoveCount));
            AssertSolutionWins(start, pruned, PourMode.Single);
            AssertSolutionWins(start, classic, PourMode.Single);
        }

        [Test]
        public void SolverOptions_PourMode_IsValidated()
        {
            SolverOptions single = SolverOptions.ForPipeline(PourMode.Single);

            Assert.That(single.PourMode, Is.EqualTo(PourMode.Single));
            Assert.That(single.NodeBudget, Is.EqualTo(SolverOptions.PipelineNodeBudget));
            Assert.That(single.Weight, Is.EqualTo(1));
            Assert.That(single.FutileRuleEnabled, Is.True);
            Assert.That(SolverOptions.ForPipeline(PourModes.Default).PourMode, Is.EqualTo(PourMode.Single));
            Assert.That(() => { new SolverOptions(10, 1, true, (PourMode)7); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { SolverOptions.ForPipeline((PourMode)(-1)); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void HeuristicSingle_CountsDosesAboveTheBottomRun()
        {
            // decisiones.md §2.1.3: H_single = max(Σ (dosis − racha del fondo), h2).
            Assert.That(SolverHeuristic.DosesAboveBottomRun(Cuia.Empty(4)), Is.EqualTo(0));
            Assert.That(SolverHeuristic.DosesAboveBottomRun(C(4, 1, 1, 1)), Is.EqualTo(0));
            Assert.That(SolverHeuristic.DosesAboveBottomRun(C(4, 1, 2, 2, 2)), Is.EqualTo(3));
            Assert.That(SolverHeuristic.DosesAboveBottomRun(C(4, 1, 1, 2, 1)), Is.EqualTo(2));
            Assert.That(SolverHeuristic.DosesAboveBottomRun(Cuia.Create(4, new[] { 1, 1, 2 }, 0x3)), Is.EqualTo(1));

            // Rachas de 3 cruzadas: h1 = 2, h2 = 2, h3 = 6 → H = 2 (Run) y H_single = 6 (par Single 7).
            var crossed = new BoardState(new[] { C(4, 1, 2, 2, 2), C(4, 2, 1, 1, 1), Cuia.Empty(4) });
            // Nivel 1: h1 = 2, h2 = 2, h3 = 2.
            var tutorial = new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });
            // Dispersión: h3 = 0, h2 = 2.
            var dispersed = new BoardState(new[] { C(4, 1), C(4, 1), C(4, 1, 1), Cuia.Empty(4) });

            Assert.That(SolverHeuristic.Estimate(crossed, PourMode.Run), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(crossed), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(crossed, PourMode.Single), Is.EqualTo(6));
            Assert.That(SolverHeuristic.Estimate(tutorial, PourMode.Single), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(dispersed, PourMode.Single), Is.EqualTo(2));

            var cuias = new[] { C(4, 9), C(4, 1, 2, 2, 2), C(4, 2, 1, 1, 1), Cuia.Empty(4), C(4, 9) };
            Assert.That(SolverHeuristic.Estimate(cuias, 1, 3, PourMode.Single), Is.EqualTo(6));
            Assert.That(SolverHeuristic.Estimate(cuias, 1, 3, PourMode.Run), Is.EqualTo(SolverHeuristic.Estimate(cuias, 1, 3)));
        }
    }
}
