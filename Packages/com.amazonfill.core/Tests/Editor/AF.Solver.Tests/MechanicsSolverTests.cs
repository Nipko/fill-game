#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver con las combinaciones de mecánicas del calendario 1-200 (propuesta de D-007, d.4; C3-3): en tableros pequeños con
    /// dos mecánicas (o una mecánica y una capacidad), el par de A* coincide con el de una búsqueda exhaustiva, con y sin la
    /// poda de fútiles, y la heurística es admisible y consistente en todos los estados alcanzables (03 §5, §9). Las
    /// combinaciones con Aguas turbias no están aquí (el solver juega cada realidad como un tablero sin ocultos; las cubren
    /// <c>MurkyTests</c> y los tableros de referencia solo dotnet). Los tableros de 12-14 cuias, los tiempos y las asignaciones
    /// están en <c>dotnet/AF.Core.Tests/DotnetOnly</c> (<c>MechanicsPerformanceTests</c>, <c>HotLoopAllocationTests</c>).
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class MechanicsSolverTests
    {
        /// <summary>Tableros por combinación.</summary>
        private const int BoardsPerCombination = 8;

        /// <summary>Estados alcanzables por tablero en single, como los lotes de Remolino, Lluvia y Piraña.</summary>
        private const int MaxStates = 5000;

        /// <summary>
        /// Combinaciones del calendario d.4 sin Aguas turbias, con sus inyectores en el orden en que se superponen (la Lluvia
        /// primero, porque quita dosis; el Barro después, porque las inserta). <c>Ma+Li</c> y <c>Li+Ma</c> son la misma.
        /// </summary>
        private static readonly string[] Combinations =
        {
            "Ho+Ch", "Ma+Ho", "Li+Ho", "Re+Li", "Ba+Ma", "Ll+Ho", "Br+Ba", "Pi+Ma", "Ll+Re", "Ll+Ba",
            "Br+Ma", "Pi+Li", "Re+Ma", "Re+Ho", "Ba+Li", "Br+Ho", "Ba+Re", "Ma+Li", "Ll+Li+Al6", "Pi+Ba",
        };

        [TestCaseSource(nameof(Combinations))]
        public void CalendarCombination_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds_035_039(string combination)
        {
            List<BoardState> boards = Boards(combination);
            for (int i = 0; i < boards.Count; i++)
            {
                for (int mode = 0; mode < 2; mode++)
                {
                    PourMode pourMode = mode == 0 ? PourMode.Run : PourMode.Single;
                    string label = combination + " " + i + " " + pourMode + " " + boards[i];
                    MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], pourMode, label);
                    MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], pourMode, label);
                }
            }
        }

        [Test]
        public void Combinations_HaveBothMechanicsOnTheBoard()
        {
            // Los inyectores componen: cada tablero lleva las mecánicas de su combinación (y capacidades mixtas con Ch o Al6).
            for (int c = 0; c < Combinations.Length; c++)
            {
                List<BoardState> boards = Boards(Combinations[c]);
                string[] tags = Combinations[c].Split('+');
                for (int i = 0; i < boards.Count; i++)
                {
                    for (int t = 0; t < tags.Length; t++)
                    {
                        Assert.That(Has(boards[i], tags[t]), Is.True, Combinations[c] + " " + i + ": falta " + tags[t] + " en " + boards[i]);
                    }
                }
            }
        }

        private static List<BoardState> Boards(string combination)
        {
            string[] tags = combination.Split('+');
            bool mixed = false;
            var injectors = new List<IMechanicInjector>();
            string[] order = { "Ll", "Ba", "Br", "Li", "Ho", "Ma", "Re", "Pi" };
            for (int o = 0; o < order.Length; o++)
            {
                for (int t = 0; t < tags.Length; t++)
                {
                    if (tags[t] == order[o])
                    {
                        injectors.Add(Injector(order[o]));
                    }
                }
            }

            for (int t = 0; t < tags.Length; t++)
            {
                mixed |= tags[t] == "Ch" || tags[t] == "Al6";
            }

            ulong seed = Prng.SeedFrom("amazonfill:c3-3:" + combination);
            return MechanicBoards.Generate(seed, BoardsPerCombination, new CombinedInjector(combination, injectors), mixed, MaxStates);
        }

        private static IMechanicInjector Injector(string tag)
        {
            switch (tag)
            {
                case "Ll":
                    return new LluviaInjector();
                case "Ba":
                    return new BarroInjector();
                case "Br":
                    return new BroteInjector();
                case "Li":
                    return new LianaInjector(true);
                case "Ho":
                    return new HojaInjector();
                case "Ma":
                    return new MatapiInjector();
                case "Re":
                    return new RemolinoInjector();
                default:
                    return new PiranaInjector();
            }
        }

        private static bool Has(BoardState board, string tag)
        {
            for (int i = 0; i < board.Count; i++)
            {
                Cuia cuia = board[i];
                switch (tag)
                {
                    case "Ch":
                    case "Al6":
                        if (cuia.Capacity != Cuia.DefaultCapacity)
                        {
                            return true; // familia mixta de MechanicBoards: Cuia chica y, a veces, Cuia alta de 5 o 6
                        }

                        break;
                    case "Ba":
                        for (int k = 0; k < cuia.Count; k++)
                        {
                            if (cuia.ColorAt(k) == Cuia.MudColor)
                            {
                                return true;
                            }
                        }

                        break;
                    default:
                        if (cuia.Mechanic == TypeOf(tag))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private static MechanicType TypeOf(string tag)
        {
            switch (tag)
            {
                case "Ho":
                    return MechanicType.Hoja;
                case "Ma":
                    return MechanicType.Matapi;
                case "Li":
                    return MechanicType.Liana;
                case "Re":
                    return MechanicType.Remolino;
                case "Ll":
                    return MechanicType.Lluvia;
                case "Br":
                    return MechanicType.Brote;
                default:
                    return MechanicType.Pirana;
            }
        }
    }

    /// <summary>Superpone varias mecánicas en orden; falla si alguna no cabe en el tablero.</summary>
    internal sealed class CombinedInjector : IMechanicInjector
    {
        private readonly List<IMechanicInjector> _injectors;

        public CombinedInjector(string name, List<IMechanicInjector> injectors)
        {
            Name = name;
            _injectors = injectors;
        }

        public string Name { get; }

        public bool Inject(Prng rng, int[] caps, List<int>[] doses, MechanicSpec[] attrs)
        {
            for (int i = 0; i < _injectors.Count; i++)
            {
                if (!_injectors[i].Inject(rng, caps, doses, attrs))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
