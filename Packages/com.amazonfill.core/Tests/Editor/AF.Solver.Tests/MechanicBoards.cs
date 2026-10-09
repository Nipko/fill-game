#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Pone una mecánica en un tablero base (capacidades, dosis y <c>attrs</c>) antes de construirlo: es el punto de
    /// extensión de los tests de propiedad y de los lotes pequeños de cada mecánica (02 R16: antes de su debut).
    /// </summary>
    internal interface IMechanicInjector
    {
        /// <summary>Nombre para los mensajes.</summary>
        string Name { get; }

        /// <summary>
        /// Pone la mecánica. Devuelve false si este tablero no la admite (se genera otro). <paramref name="doses"/> se puede
        /// reordenar entre cuias conservando los colores.
        /// </summary>
        bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs);
    }

    /// <summary>
    /// Tableros pequeños con una mecánica (≤ 6 cuias, 2-3 colores) para comprobar que el par de A* coincide con el de una
    /// búsqueda exhaustiva y que la heurística es admisible y consistente en todos los estados alcanzables (03 §5, §9).
    /// Test compartido (NUnit 3.5, C# 9): solo usa el PRNG del núcleo.
    /// </summary>
    internal static class MechanicBoards
    {
        private const int MaxStates = 50000;

        /// <summary>
        /// <paramref name="count"/> tableros con la mecánica de <paramref name="injector"/>; capacidades uniformes (4 y
        /// <c>U_c</c> = 4) o, con <paramref name="mixed"/>, con una Cuia chica o una Cuia alta con su fruta abundante.
        /// </summary>
        public static List<BoardState> Generate(ulong seed, int count, IMechanicInjector injector, bool mixed)
        {
            var rng = new Prng(seed);
            var boards = new List<BoardState>();
            for (int attempt = 0; attempt < count * 50 && boards.Count < count; attempt++)
            {
                BoardState? board = Start(rng, injector, mixed);
                if (board != null && !PourRules.IsStuck(board, true) && Fits(board))
                {
                    boards.Add(board);
                }
            }

            Assert.That(boards.Count, Is.EqualTo(count), injector.Name + ": no se generaron bastantes tableros");
            return boards;
        }

        /// <summary>Un tablero base con la mecánica, o null si el inyector no pudo ponerla.</summary>
        public static BoardState? Start(Prng rng, IMechanicInjector? injector, bool mixed)
        {
            var caps = new List<int>();
            var pool = new List<int>();
            int colors = 2 + rng.NextInt(2);
            int color = 1;
            if (mixed && rng.NextInt(2) == 0)
            {
                int tall = 5 + rng.NextInt(2);
                caps.Add(tall);
                AddUnits(pool, color++, tall);
                colors--;
            }

            for (int c = 0; c < colors; c++, color++)
            {
                caps.Add(4);
                AddUnits(pool, color, 4);
            }

            int empties = 1 + rng.NextInt(2);
            for (int e = 0; e < empties; e++)
            {
                caps.Add(mixed && e == 0 ? 1 + rng.NextInt(3) : 4);
            }

            if (mixed)
            {
                caps.Add(4); // la cuia donde puede ir la fruta que la Cuia chica no aloja
            }

            int width = caps.Count;
            var doses = new List<int>[width];
            for (int i = 0; i < width; i++)
            {
                doses[i] = new List<int>();
            }

            int[] capArray = caps.ToArray();
            Deal(rng, pool, doses, capArray);
            var attrs = new MechanicSpec[width];
            if (injector != null && !injector.Inject(rng, capArray, doses, attrs))
            {
                return null;
            }

            var cuias = new int[width][];
            for (int i = 0; i < width; i++)
            {
                cuias[i] = doses[i].ToArray();
            }

            BoardBuildResult built = BoardBuilder.Build(capArray, cuias, injector == null ? null : attrs);
            if (!built.IsValid)
            {
                throw new InvalidOperationException((injector == null ? "base" : injector.Name) + ": tablero inválido: " + built.Error);
            }

            return built.State;
        }

        /// <summary>
        /// A* (w = 1) da el par exacto: sin poda, el de la búsqueda exhaustiva con todos los legales; con la poda de fútiles
        /// (<c>MOVIMIENTOS</c> de 03 §5), el de la búsqueda exhaustiva con los mismos movimientos útiles. Con capacidades
        /// uniformes los dos coinciden; con capacidades mixtas la poda puede alargar el par o dejar el tablero sin camino útil
        /// a la victoria (ver <c>DotnetOnly/FutilePropertyTests.R16_MixedCapacities_PrunedParVersusExhaustivePar</c>), nunca
        /// acortarlo.
        /// </summary>
        public static void AssertAStarMatchesExhaustiveSearch(BoardState start, PourMode mode, string label)
        {
            ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
            int exhaustive = graph.Distance[0] == int.MaxValue ? -1 : graph.Distance[0];
            int exhaustivePruned = ExhaustiveSearch.Par(start, mode, true);
            var solver = new AStarSolver();
            SolveResult pruned = solver.Solve(start, SolverOptions.ForPipeline(mode));
            SolveResult classic = solver.Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false, mode));

            Assert.That(classic.IsSolved ? classic.MoveCount : -1, Is.EqualTo(exhaustive), label + ": A* sin poda");
            Assert.That(pruned.IsSolved ? pruned.MoveCount : -1, Is.EqualTo(exhaustivePruned), label + ": A* con poda");
            if (exhaustivePruned >= 0)
            {
                Assert.That(exhaustivePruned, Is.GreaterThanOrEqualTo(exhaustive), label + ": la poda no puede acortar el par");
            }

            if (IsUniform(start))
            {
                Assert.That(exhaustivePruned, Is.EqualTo(exhaustive), label + ": con capacidades uniformes la poda no cambia el par");
            }

            if (pruned.IsSolved)
            {
                BoardState state = start;
                for (int i = 0; i < pruned.MoveCount; i++)
                {
                    Pour pour = pruned.GetMove(i);
                    Assert.That(PourRules.Amount(state, pour.Source, pour.Target, mode), Is.EqualTo(pour.Amount), label + ": vertido " + i);
                    state = PourRules.Apply(state, pour.Source, pour.Target, mode);
                }

                Assert.That(state.IsWon(), Is.True, label + ": la solución no gana");
            }
        }

        /// <summary>La heurística del modo es admisible y consistente en todos los estados alcanzables (03 §5).</summary>
        public static void AssertHeuristicAdmissibleAndConsistent(BoardState start, PourMode mode, string label)
        {
            ExhaustiveSearch.Graph graph = ExhaustiveSearch.Explore(start, mode, MaxStates);
            for (int s = 0; s < graph.States.Count; s++)
            {
                int h = SolverHeuristic.Estimate(graph.States[s], mode);
                if (graph.Distance[s] != int.MaxValue)
                {
                    Assert.That(h, Is.LessThanOrEqualTo(graph.Distance[s]), label + ": H no admisible en " + graph.States[s]);
                }

                for (int e = 0; e < graph.Edges[s].Count; e++)
                {
                    int next = SolverHeuristic.Estimate(graph.States[graph.Edges[s][e]], mode);
                    Assert.That(h, Is.LessThanOrEqualTo(1 + next), label + ": H no consistente en " + graph.States[s]);
                }
            }
        }

        /// <summary>Índices de las cuias vacías y sin mecánica.</summary>
        public static List<int> PlainEmpties(int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            var empties = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (doses[i].Count == 0 && attrs[i].IsNone)
                {
                    empties.Add(i);
                }
            }

            return empties;
        }

        /// <summary>Colores presentes, en orden.</summary>
        public static List<int> Colors(List<int>[] doses)
        {
            var colors = new List<int>();
            for (int i = 0; i < doses.Length; i++)
            {
                for (int k = 0; k < doses[i].Count; k++)
                {
                    if (!colors.Contains(doses[i][k]))
                    {
                        colors.Add(doses[i][k]);
                    }
                }
            }

            colors.Sort();
            return colors;
        }

        private static bool IsUniform(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                int capacity = board[i].Capacity;
                for (int k = 0; k < board[i].Count; k++)
                {
                    if (board.UnitsOf(board[i].ColorAt(k)) != capacity)
                    {
                        return false;
                    }
                }

                if (capacity != board[0].Capacity)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Fits(BoardState board)
        {
            try
            {
                ExhaustiveSearch.Explore(board, PourMode.Single, MaxStates);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static void AddUnits(List<int> pool, int color, int units)
        {
            for (int u = 0; u < units; u++)
            {
                pool.Add(color);
            }
        }

        private static void Deal(Prng rng, List<int> pool, List<int>[] doses, int[] caps)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            for (int d = 0; d < pool.Count; d++)
            {
                int target = rng.NextInt(doses.Length);
                while (doses[target].Count >= caps[target])
                {
                    target = (target + 1) % doses.Length;
                }

                doses[target].Add(pool[d]);
            }
        }
    }

    /// <summary>Hoja (02 §9.1 M1): una cuia vacía pasa a ser Hoja con la llave de un color del tablero.</summary>
    internal sealed class HojaInjector : IMechanicInjector
    {
        public string Name => "hoja";

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            List<int> empties = MechanicBoards.PlainEmpties(caps, doses, attrs);
            List<int> colors = MechanicBoards.Colors(doses);
            if (empties.Count == 0 || colors.Count == 0)
            {
                return false;
            }

            int index = empties[rng.NextInt(empties.Count)];
            attrs[index] = new MechanicSpec(MechanicType.Hoja, key: colors[rng.NextInt(colors.Count)]);
            return true;
        }
    }

    /// <summary>Brote (02 §9.1 M2): una cuia con 1-3 dosis visibles y sin llenar pasa a tener el tapón cerrado.</summary>
    internal sealed class BroteInjector : IMechanicInjector
    {
        public string Name => "brote";

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            var candidates = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (attrs[i].IsNone && doses[i].Count >= 1 && doses[i].Count <= 3 && doses[i].Count < caps[i])
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            attrs[candidates[rng.NextInt(candidates.Count)]] = new MechanicSpec(MechanicType.Brote);
            return true;
        }
    }

    /// <summary>
    /// Matapi (D-007, regla 3): una cuia vacía pasa a ser Matapi o, si hay alguna, una con 1-3 dosis de un solo color y
    /// capacidad = U_c de ese color.
    /// </summary>
    internal sealed class MatapiInjector : IMechanicInjector
    {
        public string Name => "matapi";

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            var units = new int[Cuia.MaxColor + 1];
            for (int i = 0; i < doses.Length; i++)
            {
                for (int k = 0; k < doses[i].Count; k++)
                {
                    units[doses[i][k]]++;
                }
            }

            var withDoses = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                List<int> d = doses[i];
                if (!attrs[i].IsNone || d.Count < 1 || d.Count > 3 || caps[i] != units[d[0]])
                {
                    continue;
                }

                bool mono = true;
                for (int k = 1; k < d.Count; k++)
                {
                    mono &= d[k] == d[0];
                }

                if (mono)
                {
                    withDoses.Add(i);
                }
            }

            List<int> empties = MechanicBoards.PlainEmpties(caps, doses, attrs);
            bool useDoses = withDoses.Count > 0 && (empties.Count == 0 || rng.NextInt(2) == 0);
            if (!useDoses && empties.Count == 0)
            {
                return false;
            }

            int index = useDoses ? withDoses[rng.NextInt(withDoses.Count)] : empties[rng.NextInt(empties.Count)];
            attrs[index] = new MechanicSpec(MechanicType.Matapi);
            return true;
        }
    }
}
