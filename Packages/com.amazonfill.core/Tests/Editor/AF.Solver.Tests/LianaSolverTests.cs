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
    /// Solver con Liana (03 §9: el disparador se deduce del estado; solo filtra origen y destino): el par de A* coincide con
    /// el de una búsqueda exhaustiva en 32 tableros pequeños por variante (con fruta y de cuenta; capacidades uniformes y
    /// mixtas) en los dos modos, y la heurística es admisible y consistente. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LianaSolverTests
    {
        private static List<BoardState> Boards(bool withFruit)
        {
            var injector = new LianaInjector(withFruit);
            ulong seed = withFruit ? 0x0830_0001UL : 0x2430_0001UL;
            var boards = MechanicBoards.Generate(seed, 16, injector, false);
            boards.AddRange(MechanicBoards.Generate(seed + 1, 16, injector, true));
            return boards;
        }

        [TestCase(PourMode.Run, true)]
        [TestCase(PourMode.Single, true)]
        [TestCase(PourMode.Run, false)]
        [TestCase(PourMode.Single, false)]
        public void M3_AStarPar_EqualsExhaustiveSearch_AndHeuristicHolds(PourMode mode, bool withFruit)
        {
            List<BoardState> boards = Boards(withFruit);
            for (int i = 0; i < boards.Count; i++)
            {
                string label = (withFruit ? "liana con fruta " : "liana de cuenta ") + i + " " + boards[i];
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, label);
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, label);
            }
        }

        [Test]
        public void M3_Solver_ReleasesTheLianaToUseItsContent()
        {
            // La Liana con fruta 1 guarda el 2 que le falta a la cuia de 2: la solución tiene que sellar 1 antes.
            var liana = Cuia.Create(4, new[] { 2 }).WithMechanic(MechanicType.Liana, key: 1);
            var start = new BoardState(new[] { liana, Cuia.Create(4, new[] { 1, 2, 2 }), Cuia.Create(4, new[] { 2, 1, 1, 1 }), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            BoardState state = start;
            int released = 0;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run, out PourEvents events);
                released |= events.LianaReleasedMask;
            }

            Assert.That(state.IsWon(), Is.True);
            Assert.That(released, Is.EqualTo(1));
        }
    }
}
