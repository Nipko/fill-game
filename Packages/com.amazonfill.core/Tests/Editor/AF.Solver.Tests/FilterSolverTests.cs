#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver con los filtros Brote (02 §9.1 M2; 03 §9: 1 bit, filtro en origen) y Matapi (D-007: nunca es origen): el
    /// par de A* coincide con el de una búsqueda exhaustiva en 32 tableros pequeños por mecánica (capacidades uniformes y
    /// mixtas) en los dos modos, y la heurística es admisible y consistente en todos sus estados alcanzables. Test
    /// compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class FilterSolverTests
    {
        private static List<BoardState> Boards(IMechanicInjector injector, ulong seed)
        {
            var boards = MechanicBoards.Generate(seed, 16, injector, false);
            boards.AddRange(MechanicBoards.Generate(seed + 1, 16, injector, true));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M2_Brote_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode)
        {
            List<BoardState> boards = Boards(new BroteInjector(), 0x1630_0001UL);
            for (int i = 0; i < boards.Count; i++)
            {
                string label = "brote " + i + " " + boards[i];
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, label);
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, label);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M10_Matapi_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode)
        {
            List<BoardState> boards = Boards(new MatapiInjector(), 0x0630_0001UL);
            for (int i = 0; i < boards.Count; i++)
            {
                string label = "matapi " + i + " " + boards[i];
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, label);
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, label);
            }
        }

        [Test]
        public void M10_Solver_NeverPoursFromAMatapi()
        {
            var matapi = Cuia.Empty(4).WithMechanic(AF.Core.Mechanics.MechanicType.Matapi);
            var start = new BoardState(new[] { Cuia.Create(4, new[] { 1, 2, 1, 2 }), Cuia.Create(4, new[] { 2, 1, 2, 1 }), matapi, Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            for (int i = 0; i < result.MoveCount; i++)
            {
                Assert.That(result.GetMove(i).Source, Is.Not.EqualTo(2));
            }
        }
    }
}
