#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver con Barro (03 §9, M5: color 15 inerte, <c>espacio_efectivo + 1</c>; h1 cuenta el barro como corte y resta 1 por
    /// cada barro; h2 ignora el color 15): el par de A* coincide con el de una búsqueda exhaustiva en 32 tableros pequeños
    /// (capacidades uniformes y mixtas) en los dos modos, y la heurística ajustada es admisible y consistente en todos sus
    /// estados alcanzables. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class BarroSolverTests
    {
        private const int Mud = Cuia.MudColor;

        private static List<BoardState> Boards()
        {
            var boards = MechanicBoards.Generate(0xBA22_0001UL, 16, new BarroInjector(), false);
            boards.AddRange(MechanicBoards.Generate(0xBA22_0002UL, 16, new BarroInjector(), true));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M5_AStarPar_EqualsExhaustiveSearch(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, "barro " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M5_Heuristic_IsAdmissibleAndConsistent_039(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, "barro " + i + " " + boards[i]);
            }
        }

        [Test]
        public void M5_Heuristic_CountsMudAsABreakMinusOne_AndH2IgnoresColor15_039()
        {
            // 03 §9: [1, 15, 1] tiene 2 cortes − 1 barro = 1 (sin el ajuste serían 2); h2 = 0 (un solo color repartido).
            var breaks = new BoardState(new[] { Cuia.Create(4, new[] { 1, Mud, 1 }), Cuia.Create(4, new[] { 2, 2, 2, 2 }), Cuia.Empty(4) });
            Assert.That(SolverHeuristic.Estimate(breaks, PourMode.Run), Is.EqualTo(1));
            Assert.That(SolverHeuristic.Estimate(breaks, PourMode.Single), Is.EqualTo(1)); // h3 [P]: el barro no cuenta

            // h2 ignora el 15: los colores 1 y 2 están en 2 cuias cada uno (h2 = 2); con el barro como color serían 3.
            var twoMuds = new BoardState(new[] { Cuia.Create(4, new[] { 1, Mud }), Cuia.Create(4, new[] { 2, Mud }), Cuia.Create(4, new[] { 1, 1, 1 }), Cuia.Create(4, new[] { 2, 2, 2 }) });
            Assert.That(SolverHeuristic.Estimate(twoMuds, PourMode.Run), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(twoMuds, PourMode.Single), Is.EqualTo(2));

            // Barro expuesto: [1, 15] tiene 1 corte − 1 barro = 0; h2 = 1.
            var exposed = new BoardState(new[] { Cuia.Create(4, new[] { 1, Mud }), Cuia.Create(4, new[] { 1, 1, 1 }) });
            Assert.That(SolverHeuristic.Estimate(exposed, PourMode.Run), Is.EqualTo(1));
            Assert.That(SolverHeuristic.Estimate(exposed, PourMode.Single), Is.EqualTo(1));
        }

        [Test]
        public void M5_SolverMoves_DoNotDiscardTheExposedMud()
        {
            // El descarte rápido de destinos normales («llena u otra cima») no vale para barro_expuesto (02 §1.2).
            var cuias = new[] { Cuia.Create(4, new[] { 1, Mud }), Cuia.Create(4, new[] { 2, 3, 3, 1 }), Cuia.Create(4, new[] { 3, 2, 2, 2 }), Cuia.Create(4, new[] { 1, 1, 3 }) };
            int[] units = new BoardState(cuias).UnitsTable;
            var moves = new SolverMove[SolverMoves.MaxMoves];

            int count = SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Run, true, moves);

            Assert.That(count, Is.EqualTo(3));
            for (int m = 0; m < count; m++)
            {
                Assert.That(moves[m].Target, Is.EqualTo(0));
            }

            Assert.That(moves[1].Amount, Is.EqualTo(3)); // [3,2,2,2] → barro: min(3, 2 + 1)
        }

        [Test]
        public void M5_Solver_WashesTheMud_AndItsSolutionWins()
        {
            // El único vacío es el barro expuesto: la solución tiene que lavarlo.
            var start = new BoardState(new[] { Cuia.Create(4, new[] { 1, Mud }), Cuia.Create(4, new[] { 2, 3, 3, 1 }), Cuia.Create(4, new[] { 3, 2, 2, 2 }), Cuia.Create(4, new[] { 1, 1, 3 }) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            BoardState state = start;
            int washed = 0;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run, out PourEvents events);
                washed |= events.BarroWashedMask;
            }

            Assert.That(state.IsWon(), Is.True);
            Assert.That(washed, Is.EqualTo(1 << 0));
        }
    }
}
