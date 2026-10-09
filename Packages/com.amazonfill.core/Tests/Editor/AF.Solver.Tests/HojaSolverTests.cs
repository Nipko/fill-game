#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver con Hoja (03 §9: 1 bit por Hoja y filtro en el destino; 02 §9.2): el par de A* coincide con el de una
    /// búsqueda exhaustiva en 32 tableros pequeños (capacidades uniformes y mixtas) en los dos modos, y la heurística es
    /// admisible y consistente en todos sus estados alcanzables. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class HojaSolverTests
    {
        private static List<BoardState> Boards()
        {
            var boards = MechanicBoards.Generate(0x4041_0001UL, 16, new HojaInjector(), false);
            boards.AddRange(MechanicBoards.Generate(0x4041_0002UL, 16, new HojaInjector(), true));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M1_AStarPar_EqualsExhaustiveSearch(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, "hoja " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M1_Heuristic_IsAdmissibleAndConsistent(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, "hoja " + i + " " + boards[i]);
            }
        }

        [Test]
        public void M1_Solver_UsesTheHoja_AndItsSolutionOpensIt()
        {
            // El único vacío general es la Hoja de 1: la solución tiene que verter 1 en ella.
            var hoja = Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 1, flags: Cuia.FlagClosed);
            var start = new BoardState(new[] { Cuia.Create(4, new[] { 2, 1, 1 }), hoja, Cuia.Create(4, new[] { 1, 1, 2, 2 }), Cuia.Create(2, new[] { 2 }) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            BoardState state = start;
            int opened = 0;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run, out PourEvents events);
                opened |= events.HojaOpenedMask;
            }

            Assert.That(state.IsWon(), Is.True);
            Assert.That(opened, Is.EqualTo(1 << 1));
        }
    }
}
