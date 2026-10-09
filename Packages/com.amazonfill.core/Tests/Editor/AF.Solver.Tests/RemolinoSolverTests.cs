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
    /// Solver con Remolino (03 §9: transición distinta, inserción al fondo de la pareja; canon que conserva el emparejamiento;
    /// heurística sin cambio): el par de A* coincide con el de una búsqueda exhaustiva en 32 tableros pequeños (capacidades
    /// uniformes y mixtas) en los dos modos, y la heurística es admisible y consistente en todos sus estados alcanzables.
    /// Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class RemolinoSolverTests
    {
        /// <summary>
        /// El Remolino multiplica los estados alcanzables (la inserción por el fondo crea combinaciones nuevas): tableros de
        /// como mucho 5.000 estados en single, para que el lote sea rápido también en Unity.
        /// </summary>
        private const int MaxStates = 5000;

        private static List<BoardState> Boards()
        {
            var boards = MechanicBoards.Generate(0x2E30_0001UL, 16, new RemolinoInjector(), false, MaxStates);
            boards.AddRange(MechanicBoards.Generate(0x2E30_0002UL, 16, new RemolinoInjector(), true, MaxStates));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M4_AStarPar_EqualsExhaustiveSearch(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, "remolino " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M4_Heuristic_IsAdmissibleAndConsistent_039(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, "remolino " + i + " " + boards[i]);
            }
        }

        [Test]
        public void M4_SolverMoves_DoNotDiscardAFullRemolinoWithAnotherTop()
        {
            // El descarte rápido «llena u otra cima» no vale para el Remolino: la racha entra por el fondo de la pareja.
            Cuia full = Cuia.Create(4, new[] { 2, 3, 2, 3 }).WithMechanic(MechanicType.Remolino, pairId: 0);
            Cuia partner = Cuia.Create(4, new[] { 3 }).WithMechanic(MechanicType.Remolino, pairId: 0);
            var cuias = new[] { Cuia.Create(4, new[] { 2, 1, 1 }), full, partner, Cuia.Create(4, new[] { 2, 2, 1, 1 }) };
            int[] units = new BoardState(cuias).UnitsTable;
            var moves = new SolverMove[SolverMoves.MaxMoves];

            int count = SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Run, true, moves);

            bool intoFull = false;
            for (int m = 0; m < count; m++)
            {
                intoFull |= moves[m].Source == 0 && moves[m].Target == 1;
            }

            Assert.That(intoFull, Is.True);
        }

        [Test]
        public void M4_Solver_UsesThePortal_AndItsSolutionWins()
        {
            // Contraejemplo de Q-B: la única solución pasa por el Remolino (par 2).
            Cuia x = Cuia.Create(4, new[] { 1, 1 }).WithMechanic(MechanicType.Remolino, pairId: 0);
            var start = new BoardState(new[] { x, x, Cuia.Empty(4), Cuia.Create(4, new[] { 2, 2, 2, 2 }) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(2));
            BoardState state = start;
            bool portal = false;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run, out PourEvents events);
                portal |= events.ThroughRemolino;
            }

            Assert.That(state.IsWon(), Is.True);
            Assert.That(portal, Is.True);
        }
    }
}
