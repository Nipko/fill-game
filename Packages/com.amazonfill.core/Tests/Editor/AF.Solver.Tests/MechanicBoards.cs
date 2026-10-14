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
            return Generate(seed, count, injector, mixed, MaxStates);
        }

        /// <summary>
        /// Como <see cref="Generate(ulong, int, IMechanicInjector, bool)"/> con tableros de como mucho
        /// <paramref name="maxStates"/> estados alcanzables en <c>single</c> (mecánicas que multiplican los estados, como el
        /// Remolino, para que el lote siga siendo rápido en Unity).
        /// </summary>
        public static List<BoardState> Generate(ulong seed, int count, IMechanicInjector injector, bool mixed, int maxStates)
        {
            var rng = new Prng(seed);
            var boards = new List<BoardState>();
            for (int attempt = 0; attempt < count * 50 && boards.Count < count; attempt++)
            {
                BoardState? board = Start(rng, injector, mixed);
                if (board != null && !PourRules.IsStuck(board, true) && Fits(board, maxStates))
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

            if (IsUniform(start) && !(mode == PourMode.Single && HasPirana(start)))
            {
                // Excepción medida en C3-3: con la Piraña en single, un vertido fútil a la cueva puede acortar la solución
                // (PiranaSolverTests.M7_Single_AFutilePourIntoTheCave_CanShortenTheSolution).
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

        private static bool HasPirana(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Mechanic == MechanicType.Pirana)
                {
                    return true;
                }
            }

            return false;
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

        private static bool Fits(BoardState board, int maxStates)
        {
            try
            {
                ExhaustiveSearch.Explore(board, PourMode.Single, maxStates);
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
    /// Brote de dos hojas (02 R10 b; propuesta de D-007 b.3): una cuia con 1-3 dosis visibles y espacio ≥ 2 pasa a tener el
    /// tapón cerrado con dos hojas (<c>n</c> = 2).
    /// </summary>
    internal sealed class BroteDosHojasInjector : IMechanicInjector
    {
        public string Name => "brote de dos hojas";

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            var candidates = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (attrs[i].IsNone && doses[i].Count >= 1 && doses[i].Count <= 3 && caps[i] - doses[i].Count >= 2)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            attrs[candidates[rng.NextInt(candidates.Count)]] = new MechanicSpec(MechanicType.Brote, n: 2);
            return true;
        }
    }

    /// <summary>
    /// Hoja atada (02 §1.2; propuesta de D-007 b.3): una cuia sin mecánica que no empieza sellada pasa a llevar la hoja atada
    /// de la fruta de su fondo o, si está vacía, de una fruta del tablero; en los dos casos, de una fruta cuya casa puede ser
    /// (capacidad = <c>U_c</c> de la llave, la regla de <c>HojaAtadaMechanic</c>, D-024, punto 1).
    /// </summary>
    internal sealed class HojaAtadaInjector : IMechanicInjector
    {
        public string Name => "hoja atada";

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

            List<int> colors = MechanicBoards.Colors(doses);
            var candidates = new List<int>();
            var keys = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                List<int> d = doses[i];
                if (!attrs[i].IsNone)
                {
                    continue;
                }

                if (d.Count > 0)
                {
                    bool sealedStart = d.Count == caps[i] && MechanicBoards.Colors(new[] { d }).Count == 1;
                    if (units[d[0]] == caps[i] && !sealedStart)
                    {
                        candidates.Add(i);
                        keys.Add(d[0]);
                    }

                    continue;
                }

                var homes = new List<int>();
                for (int c = 0; c < colors.Count; c++)
                {
                    if (units[colors[c]] == caps[i])
                    {
                        homes.Add(colors[c]);
                    }
                }

                if (homes.Count > 0)
                {
                    candidates.Add(i);
                    keys.Add(homes[rng.NextInt(homes.Count)]);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            int pick = rng.NextInt(candidates.Count);
            attrs[candidates[pick]] = new MechanicSpec(MechanicType.HojaAtada, key: keys[pick]);
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

    /// <summary>
    /// Liana (D-007): una cuia queda atada; con fruta (una llave que no está dentro y tiene casa fuera) o de cuenta
    /// (<c>n</c> = 1-2), según <see cref="WithFruit"/>.
    /// </summary>
    internal sealed class LianaInjector : IMechanicInjector
    {
        public LianaInjector(bool withFruit)
        {
            WithFruit = withFruit;
        }

        public bool WithFruit { get; }

        public string Name => WithFruit ? "liana con fruta" : "liana de cuenta";

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

            int index = rng.NextInt(caps.Length);
            List<int> tied = doses[index];
            if (!attrs[index].IsNone || (tied.Count == caps[index] && units[tied[0]] == caps[index] && MechanicBoards.Colors(new[] { tied }).Count == 1))
            {
                return false; // ya tiene mecánica, o empezaría sellada
            }

            if (!WithFruit)
            {
                attrs[index] = new MechanicSpec(MechanicType.Liana, n: 1 + rng.NextInt(2));
                return true;
            }

            var keys = new List<int>();
            List<int> colors = MechanicBoards.Colors(doses);
            for (int c = 0; c < colors.Count; c++)
            {
                int color = colors[c];
                if (tied.Contains(color))
                {
                    continue;
                }

                for (int j = 0; j < caps.Length; j++)
                {
                    if (j != index && caps[j] == units[color])
                    {
                        keys.Add(color);
                        break;
                    }
                }
            }

            if (keys.Count == 0)
            {
                return false;
            }

            attrs[index] = new MechanicSpec(MechanicType.Liana, key: keys[rng.NextInt(keys.Count)]);
            return true;
        }
    }

    /// <summary>
    /// Flor de vitória-régia (K3b; propuesta de D-007 b.3; 02 R10 d): una cuia sin mecánica que no empieza sellada queda bajo una
    /// flor con dos llaves distintas que no están dentro de ella y pueden sellar fuera a la vez (una cuia de capacidad
    /// <c>U_c</c> para cada una, dos si comparten <c>U_c</c>), como pide <c>FlorMechanic.ValidateInitial</c>.
    /// </summary>
    internal sealed class FlorInjector : IMechanicInjector
    {
        public string Name => "flor";

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

            int index = rng.NextInt(caps.Length);
            List<int> closed = doses[index];
            if (!attrs[index].IsNone || (closed.Count == caps[index] && units[closed[0]] == caps[index] && MechanicBoards.Colors(new[] { closed }).Count == 1))
            {
                return false; // ya tiene mecánica, o empezaría sellada
            }

            var keys = new List<int>();
            List<int> colors = MechanicBoards.Colors(doses);
            for (int c = 0; c < colors.Count; c++)
            {
                if (!closed.Contains(colors[c]))
                {
                    keys.Add(colors[c]);
                }
            }

            if (keys.Count < 2)
            {
                return false;
            }

            int first = keys[rng.NextInt(keys.Count)];
            keys.Remove(first);
            int second = keys[rng.NextInt(keys.Count)];
            int homes = 0;
            int homes2 = 0;
            for (int j = 0; j < caps.Length; j++)
            {
                if (j != index)
                {
                    homes += caps[j] == units[first] ? 1 : 0;
                    homes2 += caps[j] == units[second] ? 1 : 0;
                }
            }

            bool canSeal = units[first] == units[second] ? homes >= 2 : homes >= 1 && homes2 >= 1;
            if (!canSeal)
            {
                return false;
            }

            attrs[index] = new MechanicSpec(MechanicType.Flor, key: first, key2: second);
            return true;
        }
    }

    /// <summary>
    /// Barro (02 §9.1 M5; 03 §9: «superposición»): inserta una dosis de Barro (15) en la capa 2 o 3 (índice 1-2) de una cuia
    /// de 4 sin mecánica y con 1-3 dosis, sin quitar ninguna fruta (así cada color conserva su <c>U_c</c> y su casa).
    /// </summary>
    internal sealed class BarroInjector : IMechanicInjector
    {
        public string Name => "barro";

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            var candidates = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (attrs[i].IsNone && caps[i] == Cuia.DefaultCapacity && doses[i].Count >= 1 && doses[i].Count <= 3)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            List<int> cuia = doses[candidates[rng.NextInt(candidates.Count)]];
            int layer = cuia.Count == 1 ? 1 : 1 + rng.NextInt(2);
            cuia.Insert(layer, Cuia.MudColor);
            return true;
        }
    }

    /// <summary>
    /// Remolino (02 §9.1 M4): dos cuias sin mecánica pasan a ser un par (<c>partner</c> simétrico). Ninguna de las dos puede
    /// empezar sellada (03 §17 #5).
    /// </summary>
    internal sealed class RemolinoInjector : IMechanicInjector
    {
        public string Name => "remolino";

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

            var candidates = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                List<int> d = doses[i];
                bool sealedStart = d.Count == caps[i] && units[d[0]] == caps[i] && MechanicBoards.Colors(new[] { d }).Count == 1;
                if (attrs[i].IsNone && !sealedStart)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count < 2)
            {
                return false;
            }

            int first = candidates[rng.NextInt(candidates.Count)];
            candidates.Remove(first);
            int second = candidates[rng.NextInt(candidates.Count)];
            attrs[first] = new MechanicSpec(MechanicType.Remolino, partner: second);
            attrs[second] = new MechanicSpec(MechanicType.Remolino, partner: first);
            return true;
        }
    }

    /// <summary>
    /// Lluvia (02 §9.1 M6; 03 §9: «las gotas se generan hacia atrás»): un color de 4 dosis pasa a ser el agua; se quitan
    /// <c>n</c> = 1-2 de sus dosis del tablero (empezando por las de más arriba) y una cuia sin mecánica pasa a ser la Lluvia
    /// con esas gotas pendientes y <c>k</c> = 2-3, así que <c>U_agua</c> sigue siendo 4 (02 R3).
    /// </summary>
    internal sealed class LluviaInjector : IMechanicInjector
    {
        public string Name => "lluvia";

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

            var waters = new List<int>();
            List<int> colors = MechanicBoards.Colors(doses);
            for (int c = 0; c < colors.Count; c++)
            {
                if (units[colors[c]] == 4)
                {
                    waters.Add(colors[c]);
                }
            }

            var candidates = new List<int>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (attrs[i].IsNone)
                {
                    candidates.Add(i);
                }
            }

            if (waters.Count == 0 || candidates.Count == 0)
            {
                return false;
            }

            int water = waters[rng.NextInt(waters.Count)];
            int drops = 1 + rng.NextInt(2);
            for (int removed = 0, layer = MaxLayer(doses); removed < drops && layer >= 0; layer--)
            {
                for (int i = 0; i < doses.Length && removed < drops; i++)
                {
                    if (layer < doses[i].Count && doses[i][layer] == water)
                    {
                        doses[i].RemoveAt(layer);
                        removed++;
                    }
                }
            }

            int index = candidates[rng.NextInt(candidates.Count)];
            attrs[index] = new MechanicSpec(MechanicType.Lluvia, key: water, n: drops, k: 2 + rng.NextInt(2));
            return true;
        }

        private static int MaxLayer(List<int>[] doses)
        {
            int max = 0;
            for (int i = 0; i < doses.Length; i++)
            {
                max = Math.Max(max, doses[i].Count - 1);
            }

            return max;
        }
    }

    /// <summary>
    /// Piraña (02 §9.1 M7; 03 §9: «superposición (cueva = Cuia chica de 2 vacía)»): una cuia vacía y sin mecánica (la Cuia
    /// chica en los tableros mixtos) pasa a ser la cueva, con <c>k</c> = 2-4.
    /// </summary>
    internal sealed class PiranaInjector : IMechanicInjector
    {
        public string Name => "pirana";

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            List<int> empties = MechanicBoards.PlainEmpties(caps, doses, attrs);
            if (empties.Count == 0)
            {
                return false;
            }

            int index = empties[0];
            for (int e = 1; e < empties.Count; e++)
            {
                if (caps[empties[e]] < caps[index])
                {
                    index = empties[e]; // la más chica, como la cueva de 03 §9
                }
            }

            attrs[index] = new MechanicSpec(MechanicType.Pirana, k: 2 + rng.NextInt(3));
            return true;
        }
    }
}
