#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using AF.Solver.Tests;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Propiedades del modo de vertido (D-005: <see cref="PourMode.Run"/> por defecto y <see cref="PourMode.Single"/>, el
    /// modo de pruebas; docs/dev/decisiones.md §4, criterios A1-A4) sobre lotes de tableros aleatorios con semilla fija y
    /// sobre el pack del tutorial v0. Solo dotnet: lotes grandes y lectura de disco.
    /// El oráculo es <see cref="ExhaustiveSearch"/> (BFS sin heurística, en Tests/Editor/AF.Solver.Tests).
    /// </summary>
    [TestFixture]
    public sealed class PourModePropertyTests
    {
        private const int RulesCorpusSize = 400;
        private const int SolverCorpusSize = 240;
        private const int MaxStates = 200000;

        private static readonly PourMode[] Modes = { PourMode.Run, PourMode.Single };

        /// <summary>PRNG local de los tests (SplitMix64): determinista y sin <c>System.Random</c> (BannedSymbols).</summary>
        private struct SplitMix64
        {
            private ulong _state;

            public SplitMix64(ulong seed)
            {
                _state = seed;
            }

            public int Next(int bound)
            {
                unchecked
                {
                    _state += 0x9E3779B97F4A7C15UL;
                    ulong z = _state;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                    z ^= z >> 31;
                    return (int)(z % (ulong)bound);
                }
            }
        }

        /// <summary>
        /// Tablero de capacidad uniforme 4 con <c>U_c</c> = 4 (como todo v0): K colores, E ≥ 1 vacíos, T = K + E ≤ 6, y las
        /// dosis repartidas al azar entre cuias con hueco (también quedan cuias a medias).
        /// </summary>
        private static BoardState UniformBoard(ref SplitMix64 rng, int maxColors)
        {
            int colors = 1 + rng.Next(maxColors);
            int width = Math.Min(6, colors + 1 + rng.Next(2));
            var doses = new List<int>[width];
            for (int i = 0; i < width; i++)
            {
                doses[i] = new List<int>();
            }

            var pool = new List<int>();
            for (int c = 0; c < colors; c++)
            {
                int color = 1 + ((c * 5) % Cuia.MaxTintColor);
                for (int u = 0; u < 4; u++)
                {
                    pool.Add(color);
                }
            }

            Deal(ref rng, pool, doses, Fill(width, 4));
            var cuias = new Cuia[width];
            for (int i = 0; i < width; i++)
            {
                cuias[i] = Cuia.Create(4, doses[i].ToArray());
            }

            return new BoardState(cuias);
        }

        /// <summary>
        /// Tablero de reglas: capacidades 1-6, <c>U_c</c> de 1 a 6 y, si <paramref name="hidden"/>, dosis ocultas debajo
        /// de la cima (Aguas turbias). No tiene por qué ser resoluble: sirve para A1-A3.
        /// </summary>
        private static BoardState MixedBoard(ref SplitMix64 rng, bool hidden)
        {
            int width = 2 + rng.Next(5);
            var caps = new int[width];
            int capacity = 0;
            for (int i = 0; i < width; i++)
            {
                caps[i] = 1 + rng.Next(Cuia.MaxCapacity);
                capacity += caps[i];
            }

            var pool = new List<int>();
            int colors = 1 + rng.Next(4);
            for (int c = 0; c < colors && pool.Count < capacity; c++)
            {
                int units = 1 + rng.Next(6);
                for (int u = 0; u < units && pool.Count < capacity; u++)
                {
                    pool.Add(1 + c);
                }
            }

            var doses = new List<int>[width];
            for (int i = 0; i < width; i++)
            {
                doses[i] = new List<int>();
            }

            Deal(ref rng, pool, doses, caps);
            var cuias = new Cuia[width];
            for (int i = 0; i < width; i++)
            {
                int count = doses[i].Count;
                int mask = 0;
                if (hidden && count > 1)
                {
                    mask = rng.Next(1 << (count - 1)); // nunca la cima (R7)
                }

                cuias[i] = Cuia.Create(caps[i], doses[i].ToArray(), mask);
            }

            return new BoardState(cuias);
        }

        private static int[] Fill(int width, int value)
        {
            var values = new int[width];
            for (int i = 0; i < width; i++)
            {
                values[i] = value;
            }

            return values;
        }

        private static void Deal(ref SplitMix64 rng, List<int> pool, List<int>[] doses, int[] caps)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            for (int d = 0; d < pool.Count; d++)
            {
                int target = rng.Next(doses.Length);
                while (doses[target].Count >= caps[target])
                {
                    target = (target + 1) % doses.Length;
                }

                doses[target].Add(pool[d]);
            }
        }

        private static List<BoardState> RulesCorpus()
        {
            var rng = new SplitMix64(0xAF_D001_0001UL);
            var corpus = new List<BoardState>();
            for (int n = 0; n < RulesCorpusSize; n++)
            {
                corpus.Add(n % 3 == 0 ? UniformBoard(ref rng, 5) : MixedBoard(ref rng, n % 3 == 1));
            }

            return corpus;
        }

        private static List<BoardState> SolverCorpus()
        {
            var rng = new SplitMix64(0xAF_D001_0002UL);
            var corpus = new List<BoardState>();
            for (int n = 0; n < SolverCorpusSize; n++)
            {
                corpus.Add(UniformBoard(ref rng, 4));
            }

            return corpus;
        }

        [Test]
        public void A1_Amount_SingleIsOneIffLegal_RunMatchesTheLegacyOverloads()
        {
            int legalPairs = 0;
            foreach (BoardState state in RulesCorpus())
            {
                for (int a = 0; a < state.Count; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        bool legal = PourRules.IsLegal(state, a, b);
                        int single = PourRules.Amount(state, a, b, PourMode.Single);
                        int run = PourRules.Amount(state, a, b, PourMode.Run);
                        if (single != (legal ? 1 : 0) || run != PourRules.Amount(state, a, b))
                        {
                            Assert.Fail(state + " " + a + "->" + b + ": single " + single + ", run " + run);
                        }

                        if (legal)
                        {
                            legalPairs++;
                            if (!PourRules.Apply(state, a, b, PourMode.Run).Equals(PourRules.Apply(state, a, b)))
                            {
                                Assert.Fail(state + " " + a + "->" + b + ": Apply(Run) distinto de la sobrecarga heredada");
                            }
                        }
                    }
                }
            }

            Assert.That(legalPairs, Is.GreaterThan(1000));
        }

        [Test]
        public void A2_UsefulMoves_SamePairsInBothModes_AmountOneInSingle()
        {
            var single = new Pour[PourRules.MaxPours];
            var run = new Pour[PourRules.MaxPours];
            var legacy = new Pour[PourRules.MaxPours];
            foreach (BoardState state in RulesCorpus())
            {
                for (int f = 0; f < 2; f++)
                {
                    bool futile = f == 0;
                    int ns = PourRules.CollectUsefulMoves(state, futile, PourMode.Single, single);
                    int nr = PourRules.CollectUsefulMoves(state, futile, PourMode.Run, run);
                    int nl = PourRules.CollectUsefulMoves(state, futile, legacy);
                    Assert.That(ns, Is.EqualTo(nr), state.ToString());
                    Assert.That(nl, Is.EqualTo(nr), state.ToString());
                    for (int i = 0; i < nr; i++)
                    {
                        if (single[i].Source != run[i].Source || single[i].Target != run[i].Target || single[i].Amount != 1
                            || legacy[i] != run[i] || run[i].Amount != PourRules.Amount(state, run[i].Source, run[i].Target, PourMode.Run))
                        {
                            Assert.Fail(state + ": " + single[i] + " / " + run[i] + " / " + legacy[i]);
                        }
                    }
                }
            }
        }

        [Test]
        public void A3_ARunPourOfKDoses_EqualsKConsecutiveSinglePours()
        {
            int checkedRuns = 0;
            foreach (BoardState state in RulesCorpus())
            {
                for (int a = 0; a < state.Count; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        int k = PourRules.Amount(state, a, b, PourMode.Run);
                        if (k == 0)
                        {
                            continue;
                        }

                        BoardState expected = PourRules.Apply(state, a, b, PourMode.Run);
                        BoardState current = state;
                        for (int i = 0; i < k; i++)
                        {
                            if (PourRules.Amount(current, a, b, PourMode.Single) != 1)
                            {
                                Assert.Fail(state + " " + a + "->" + b + ": el vertido Single " + i + " de " + k + " no es legal");
                            }

                            current = PourRules.Apply(current, a, b, PourMode.Single);
                        }

                        if (!current.Equals(expected))
                        {
                            Assert.Fail(state + " " + a + "->" + b + ": " + current + " frente a " + expected);
                        }

                        checkedRuns += k > 1 ? 1 : 0;
                    }
                }
            }

            Assert.That(checkedRuns, Is.GreaterThan(100));
        }

        [Test]
        public void R16_Single_FutilePour_KeepsTheCanonicalState_OrSplitsAMonocolor_OnUniformCapacities()
        {
            // decisiones.md §2.1.2 (E1, E2) y R16 reformulada: con capacidades uniformes un fútil en Single deja el estado
            // canónico igual si A tenía una dosis, y si no parte la monocolor: la dispersión del color sube exactamente 1.
            int futiles = 0;
            int splits = 0;
            foreach (BoardState start in SolverCorpus())
            {
                ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, PourMode.Single, MaxStates);
                foreach (BoardState state in graph.States)
                {
                    for (int a = 0; a < state.Count; a++)
                    {
                        for (int b = 0; b < state.Count; b++)
                        {
                            if (a == b || !PourRules.IsLegal(state, a, b) || !PourRules.IsFutile(state, a, b))
                            {
                                continue;
                            }

                            futiles++;
                            BoardState after = PourRules.Apply(state, a, b, PourMode.Single);
                            int color = state[a].TopColor;
                            if (state[a].Count == 1)
                            {
                                Assert.That(ExhaustiveSearch.Key(after), Is.EqualTo(ExhaustiveSearch.Key(state)));
                            }
                            else
                            {
                                splits++;
                                Assert.That(CuiasContaining(after, color), Is.EqualTo(CuiasContaining(state, color) + 1));
                            }
                        }
                    }
                }
            }

            Assert.That(futiles, Is.GreaterThan(100));
            Assert.That(splits, Is.GreaterThan(10));
        }

        [Test]
        public void R13_UniformCapacities_FutileRuleNeverChangesTheVerdict_InBothModes()
        {
            // decisiones.md §2.1.2 (prueba de 4 pasos): con cap_i = U_c para todo i y c, IsStuck con y sin la regla de
            // fútiles coincide en todo estado alcanzable, en los dos modos.
            int states = 0;
            int stuck = 0;
            foreach (BoardState start in SolverCorpus())
            {
                foreach (PourMode mode in Modes)
                {
                    ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
                    foreach (BoardState state in graph.States)
                    {
                        states++;
                        bool withRule = PourRules.IsStuck(state, true);
                        if (withRule != PourRules.IsStuck(state, false))
                        {
                            Assert.Fail(mode + " " + state + ": el kill-switch cambia el veredicto");
                        }

                        stuck += withRule ? 1 : 0;
                    }
                }
            }

            Assert.That(states, Is.GreaterThan(10000));
            Assert.That(stuck, Is.GreaterThan(0));
        }

        [Test]
        public void A4_AStarPar_EqualsExhaustiveSearch_WithAndWithoutFutilePruning_AndHeuristicsAreAdmissible()
        {
            var solver = new AStarSolver();
            int solvedSingle = 0;
            int differentPar = 0;
            foreach (BoardState start in SolverCorpus())
            {
                int[] par = new int[2];
                int runDoses = -1;
                for (int m = 0; m < Modes.Length; m++)
                {
                    PourMode mode = Modes[m];
                    ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
                    int exhaustive = graph.Distance[0] == int.MaxValue ? -1 : graph.Distance[0];
                    int pruned = ExhaustiveSearch.Par(start, mode, true);
                    SolveResult astar = solver.Solve(start, SolverOptions.ForPipeline(mode));
                    int astarPar = astar.IsSolved ? astar.MoveCount : -1;
                    SolveResult classic = solver.Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, mode));
                    int classicPar = classic.IsSolved ? classic.MoveCount : -1;
                    string label = mode + " " + start;

                    Assert.That(pruned, Is.EqualTo(exhaustive), label + ": BFS con poda");
                    Assert.That(astarPar, Is.EqualTo(exhaustive), label + ": A*");
                    Assert.That(classicPar, Is.EqualTo(exhaustive), label + ": A* sin poda");
                    Assert.That(astar.Status, Is.Not.EqualTo(SolveStatus.BudgetExceeded), label);
                    if (astar.IsSolved)
                    {
                        AssertWins(start, astar, mode);
                        if (mode == PourMode.Run)
                        {
                            runDoses = 0;
                            for (int i = 0; i < astar.MoveCount; i++)
                            {
                                runDoses += astar.GetMove(i).Amount;
                            }
                        }
                    }

                    for (int s = 0; s < graph.States.Count; s++)
                    {
                        int h = SolverHeuristic.Estimate(graph.States[s], mode);
                        if (graph.Distance[s] != int.MaxValue && h > graph.Distance[s])
                        {
                            Assert.Fail(label + ": H no admisible en " + graph.States[s]);
                        }

                        for (int e = 0; e < graph.Edges[s].Count; e++)
                        {
                            if (h > 1 + SolverHeuristic.Estimate(graph.States[graph.Edges[s][e]], mode))
                            {
                                Assert.Fail(label + ": H no consistente en " + graph.States[s]);
                            }
                        }
                    }

                    par[m] = exhaustive;
                }

                // Resoluble en Run ⇒ resoluble en Single, con par(Single) ≤ dosis movidas por la solución óptima en Run.
                if (par[0] >= 0)
                {
                    Assert.That(par[1], Is.InRange(0, runDoses), start.ToString());
                }

                solvedSingle += par[1] >= 0 ? 1 : 0;
                differentPar += par[0] != par[1] ? 1 : 0;
            }

            Assert.That(solvedSingle, Is.GreaterThan(SolverCorpusSize / 2));
            Assert.That(differentPar, Is.GreaterThan(10));
        }

        [Test]
        public void A4_TutorialPack_AStarParEqualsExhaustiveSearch_InBothModes()
        {
            // Los 10 tableros del pack del tutorial (Assets/_Project/Data/Levels/tutorial_v0.json), sin fijar el par que trae
            // el pack (el generador lo calcula en el modo por defecto, Run desde D-005): A* (w = 1) = BFS exhaustivo en cada
            // modo, y H_single admisible.
            string path = Path.Combine(RepoRoot(), "Assets", "_Project", "Data", "Levels", "tutorial_v0.json");
            var solver = new AStarSolver();
            int levels = 0;
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path)))
            {
                foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                {
                    levels++;
                    int number = level.GetProperty("level").GetInt32();
                    BoardState start = BuildBoard(level);
                    var line = new System.Text.StringBuilder("nivel " + number.ToString(CultureInfo.InvariantCulture));
                    foreach (PourMode mode in Modes)
                    {
                        ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
                        SolveResult astar = solver.Solve(start, SolverOptions.ForPipeline(mode));
                        Assert.That(astar.IsSolved, Is.True, "nivel " + number + " " + mode);
                        Assert.That(astar.MoveCount, Is.EqualTo(graph.Distance[0]), "nivel " + number + " " + mode);
                        Assert.That(ExhaustiveSearch.Par(start, mode, true), Is.EqualTo(graph.Distance[0]), "nivel " + number + " " + mode);
                        AssertWins(start, astar, mode);
                        for (int s = 0; s < graph.States.Count; s++)
                        {
                            if (graph.Distance[s] != int.MaxValue && SolverHeuristic.Estimate(graph.States[s], mode) > graph.Distance[s])
                            {
                                Assert.Fail("nivel " + number + " " + mode + ": H no admisible en " + graph.States[s]);
                            }
                        }

                        int dead = 0;
                        for (int s = 0; s < graph.Distance.Length; s++)
                        {
                            dead += graph.Distance[s] == int.MaxValue ? 1 : 0;
                        }

                        line.Append(" | ").Append(mode).Append(": par ").Append(astar.MoveCount)
                            .Append(", alcanzables ").Append(graph.States.Count)
                            .Append(", sin salida ").Append(dead)
                            .Append(", expandidos ").Append(astar.Expanded);
                    }

                    TestContext.Out.WriteLine(line.ToString());
                }
            }

            Assert.That(levels, Is.EqualTo(10));
        }

        /// <summary>
        /// Tableros 1-10 del tutorial v0 tal como están en <c>main</c> (8854993), copiados aquí para que la medición no
        /// cambie cuando T2 regenere o rediseñe el pack.
        /// </summary>
        private static readonly int[][][] TutorialV0 =
        {
            new[] { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, new int[0] },
            new[] { new[] { 4, 8, 4, 4 }, new[] { 3, 3, 3, 4 }, new[] { 3, 8, 8, 8 }, new int[0], new int[0] },
            new[] { new[] { 3, 3, 4, 4 }, new[] { 8, 3, 8, 3 }, new[] { 8, 4, 4, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 1, 8, 1 }, new[] { 4, 8, 8, 4 }, new[] { 3, 3, 4, 4 }, new[] { 1, 1, 3, 3 }, new int[0], new int[0] },
            new[] { new[] { 8, 4, 8, 8 }, new[] { 1, 4, 1, 1 }, new[] { 4, 3, 3, 4 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 1, 4, 4 }, new[] { 3, 8, 8, 1 }, new[] { 4, 3, 3, 1 }, new[] { 1, 4, 8, 3 }, new int[0] },
            new[] { new[] { 8, 8, 7, 7 }, new[] { 3, 4, 4, 7 }, new[] { 1, 1, 8, 3 }, new[] { 3, 7, 8, 4 }, new[] { 4, 1, 1, 3 }, new int[0], new int[0] },
            new[] { new[] { 4, 4, 7, 7 }, new[] { 3, 1, 8, 7 }, new[] { 4, 4, 3, 1 }, new[] { 1, 8, 3, 7 }, new[] { 1, 3, 8, 8 }, new int[0], new int[0] },
            new[] { new[] { 1, 7, 7, 8 }, new[] { 4, 4, 8, 1 }, new[] { 3, 4, 1, 7 }, new[] { 7, 8, 4, 3 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 4, 3, 1 }, new[] { 7, 8, 4, 3 }, new[] { 1, 7, 8, 4 }, new[] { 3, 1, 7, 8 }, new[] { 4, 3, 1, 7 }, new int[0], new int[0] },
        };

        [TestCase(1, 3, 6, 0, 5, 3, 8, 0, 5)]
        [TestCase(2, 6, 42, 0, 25, 9, 114, 0, 74)]
        [TestCase(3, 7, 109, 0, 37, 10, 170, 0, 101)]
        [TestCase(4, 8, 179, 0, 52, 12, 375, 0, 222)]
        [TestCase(5, 10, 481, 0, 157, 16, 805, 0, 692)]
        [TestCase(6, 10, 36, 1, 25, 17, 72, 1, 40)]
        [TestCase(7, 11, 563, 0, 19, 15, 922, 0, 477)]
        [TestCase(8, 13, 742, 3, 319, 17, 1478, 3, 947)]
        [TestCase(9, 14, 1361, 4, 435, 16, 1961, 4, 462)]
        [TestCase(10, 16, 2479, 0, 78, 19, 3634, 0, 1176)]
        public void TutorialV0_ReproducesTheMeasurementsOfDecisions(
            int level, int parRun, int reachableRun, int deadRun, int expandedRun,
            int parSingle, int reachableSingle, int deadSingle, int expandedSingle)
        {
            // decisiones.md §2.1.2-§2.1.4 (criterio A5, parte del núcleo): pares, estados alcanzables, estados sin salida y
            // expandidos de A* (w = 1, con poda; H en Run y H_single en Single), medidos allí con un script externo.
            int[][] cuias = TutorialV0[level - 1];
            var built = new Cuia[cuias.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                built[i] = Cuia.Create(4, cuias[i]);
            }

            var start = new BoardState(built);
            for (int m = 0; m < Modes.Length; m++)
            {
                PourMode mode = Modes[m];
                bool run = mode == PourMode.Run;
                ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
                SolveResult astar = new AStarSolver().Solve(start, SolverOptions.ForPipeline(mode));
                int dead = 0;
                for (int s = 0; s < graph.Distance.Length; s++)
                {
                    dead += graph.Distance[s] == int.MaxValue ? 1 : 0;
                }

                Assert.That(astar.MoveCount, Is.EqualTo(run ? parRun : parSingle), mode + ": par");
                Assert.That(graph.Distance[0], Is.EqualTo(run ? parRun : parSingle), mode + ": par BFS");
                Assert.That(graph.States.Count, Is.EqualTo(run ? reachableRun : reachableSingle), mode + ": alcanzables");
                Assert.That(dead, Is.EqualTo(run ? deadRun : deadSingle), mode + ": sin salida");
                Assert.That(astar.Expanded, Is.EqualTo(run ? expandedRun : expandedSingle), mode + ": expandidos");
                AssertWins(start, astar, mode);
            }
        }

        private static void AssertWins(BoardState start, SolveResult result, PourMode mode)
        {
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                if (PourRules.Amount(state, pour.Source, pour.Target, mode) != pour.Amount || (mode == PourMode.Single && pour.Amount != 1))
                {
                    Assert.Fail(mode + " " + start + ": el vertido " + i + " (" + pour + ") no se puede rehacer");
                }

                state = PourRules.Apply(state, pour.Source, pour.Target, mode);
            }

            Assert.That(state.IsWon(), Is.True, mode + " " + start);
        }

        private static int CuiasContaining(BoardState state, int color)
        {
            int found = 0;
            for (int i = 0; i < state.Count; i++)
            {
                for (int k = 0; k < state[i].Count; k++)
                {
                    if (state[i].ColorAt(k) == color)
                    {
                        found++;
                        break;
                    }
                }
            }

            return found;
        }

        private static BoardState BuildBoard(JsonElement level)
        {
            JsonElement cuias = level.GetProperty("cuias");
            JsonElement caps = level.GetProperty("caps");
            var built = new Cuia[cuias.GetArrayLength()];
            for (int i = 0; i < built.Length; i++)
            {
                JsonElement doses = cuias[i];
                var colors = new int[doses.GetArrayLength()];
                for (int k = 0; k < colors.Length; k++)
                {
                    colors[k] = doses[k].GetInt32();
                }

                built[i] = Cuia.Create(caps[i].GetInt32(), colors);
            }

            return new BoardState(built);
        }

        private static string RepoRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "tools", "levelgen_proto.py")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "No se encontró la raíz del repositorio (tools/levelgen_proto.py) subiendo desde " + TestContext.CurrentContext.TestDirectory);
        }
    }
}
