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
    /// Solver con Lluvia (03 §9: <c>gotas_pendientes</c> y el contador de vertidos en la clave canónica; tick en <c>apply</c>
    /// tras el sellado y antes de la victoria; «una gota nunca elimina cortes», así que <c>H</c> sigue siendo cota): el par de
    /// A* coincide con el de una búsqueda exhaustiva en 32 tableros pequeños (capacidades uniformes y mixtas) en los dos modos,
    /// y la heurística es admisible y consistente en todos sus estados alcanzables. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LluviaSolverTests
    {
        /// <summary>El contador y las gotas multiplican los estados (×2-3, 03 §9): tableros de como mucho 5.000 en single.</summary>
        private const int MaxStates = 5000;

        private static List<BoardState> Boards()
        {
            var boards = MechanicBoards.Generate(0x1430_0001UL, 16, new LluviaInjector(), false, MaxStates);
            boards.AddRange(MechanicBoards.Generate(0x1430_0002UL, 16, new LluviaInjector(), true, MaxStates));
            return boards;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M6_AStarPar_EqualsExhaustiveSearch(PourMode mode)
        {
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertAStarMatchesExhaustiveSearch(boards[i], mode, "lluvia " + i + " " + boards[i]);
            }
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void M6_Heuristic_IsAdmissibleAndConsistent_039(PourMode mode)
        {
            // 03 §9: «h1/h2 sobre el estado actual (una gota nunca elimina cortes: cota válida)».
            List<BoardState> boards = Boards();
            for (int i = 0; i < boards.Count; i++)
            {
                MechanicBoards.AssertHeuristicAdmissibleAndConsistent(boards[i], mode, "lluvia " + i + " " + boards[i]);
            }
        }

        [Test]
        public void M6_Solver_WaitsForTheDrop_QBCounterexample()
        {
            // Contraejemplo de Q-B (LluviaTests): solo queda el agua en la Lluvia con 1 gota pendiente y k = 3. El solver tiene
            // que hacer pasar 3 vertidos útiles (el vaivén que Q-B hace útil) antes de juntar el agua: par 4.
            Cuia rain = Cuia.Create(4, new[] { 10, 10, 10 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 1, k: 3);
            var start = new BoardState(new[]
            {
                rain, Cuia.Empty(4), Cuia.Empty(4), Cuia.Create(4, new[] { 1, 1, 1, 1 }), Cuia.Create(4, new[] { 2, 2, 2, 2 }),
            });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));

            Assert.That(result.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(4));
            Assert.That(ExhaustiveSearch.Par(start, PourMode.Run, true), Is.EqualTo(4));
            BoardState state = start;
            int drops = 0;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                state = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run, out PourEvents events);
                drops += events.LluviaDropMask != 0 ? 1 : 0;
            }

            Assert.That(state.IsWon(), Is.True);
            Assert.That(drops, Is.EqualTo(1));
        }

        [Test]
        public void M6_CanonicalKey_IncludesThePendingDropsAndTheCounter_033()
        {
            // 03 §3 y §9: gotas pendientes y «vertidos mod k» van en la clave canónica.
            Cuia a = Cuia.Create(4, new[] { 2 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3);
            Cuia b = Cuia.Create(4, new[] { 2 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3, counter: 1);
            Cuia c = Cuia.Create(4, new[] { 2 }).WithMechanic(MechanicType.Lluvia, key: 10, n: 1, k: 3);
            Cuia rest = Cuia.Create(4, new[] { 1, 1, 2, 2 });

            ulong ha = new BoardState(new[] { a, rest }).CanonicalHash();
            Assert.That(new BoardState(new[] { b, rest }).CanonicalHash(), Is.Not.EqualTo(ha));
            Assert.That(new BoardState(new[] { c, rest }).CanonicalHash(), Is.Not.EqualTo(ha));
        }
    }
}
