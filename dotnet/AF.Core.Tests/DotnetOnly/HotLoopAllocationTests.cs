#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Bucle caliente sin asignaciones (CLAUDE.md, regla dura 2; .claude/rules/core.md): detección de vertidos
    /// útiles y bloqueo, y el solver una vez calentado. Solo dotnet: usa
    /// <see cref="GC.GetAllocatedBytesForCurrentThread"/>, que no existe en el perfil de Unity.
    /// </summary>
    [TestFixture]
    public sealed class HotLoopAllocationTests
    {
        private static readonly int[][] FillBoard =
        {
            new[] { 9, 8, 5, 5 }, new[] { 6, 8, 6, 6 }, new[] { 4, 5, 12, 2 }, new[] { 1, 7, 10, 11 }, new[] { 12, 1, 12, 3 },
            new[] { 8, 2, 1, 3 }, new[] { 2, 3, 11, 5 }, new[] { 9, 11, 4, 4 }, new[] { 11, 8, 9, 12 }, new[] { 2, 3, 10, 7 },
            new[] { 9, 7, 1, 7 }, new[] { 10, 10, 6, 4 }, new int[0], new int[0],
        };

        private static BoardState Board()
        {
            var cuias = new Cuia[FillBoard.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = Cuia.Create(4, FillBoard[i]);
            }

            return new BoardState(cuias);
        }

        [Test]
        public void UsefulMovesAndStuckDetection_DoNotAllocate()
        {
            BoardState state = Board();
            var buffer = new Pour[PourRules.MaxPours];
            int warm = PourRules.CollectUsefulMoves(state, true, buffer);
            PourRules.IsStuck(state, true);
            PourRules.IsFutile(state, 0, 12);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 1000; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, buffer);
                total += PourRules.HasUsefulMove(state, true) ? 1 : 0;
                total += PourRules.IsStuck(state, false) ? 1 : 0;
                total += PourRules.IsFutile(state, 0, 12) ? 1 : 0;
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(1000 * (warm + 1)));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void UsefulMovesInRun_DoNotAllocate()
        {
            // D-005: Run, el modo por defecto, con el modo explícito (no las sobrecargas heredadas). La cantidad se calcula
            // en CuiaRules.Amount(from, to, mode), sin asignaciones.
            BoardState state = Board();
            var buffer = new Pour[PourRules.MaxPours];
            int warm = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
            PourRules.Amount(state, 0, 12, PourMode.Run);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 1000; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                total += PourRules.CollectUsefulMoves(state, false, PourMode.Single, buffer);
                total += PourRules.Amount(state, 0, 12, PourMode.Run);
                total += CuiaRules.Amount(state[0], state[12], PourMode.Single);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThan(1000 * warm));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void UsefulMovesInSingle_DoNotAllocate()
        {
            // Single, el modo de pruebas (D-005): la cantidad del modo se calcula en CuiaRules.Amount(from, to, mode), sin
            // asignaciones.
            BoardState state = Board();
            var buffer = new Pour[PourRules.MaxPours];
            int warm = PourRules.CollectUsefulMoves(state, true, PourMode.Single, buffer);
            PourRules.Amount(state, 0, 12, PourMode.Single);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 1000; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Single, buffer);
                total += PourRules.CollectUsefulMoves(state, false, PourMode.Run, buffer);
                total += PourRules.Amount(state, 0, 12, PourMode.Single);
                total += CuiaRules.Amount(state[0], state[12], PourMode.Run);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThan(1000 * warm));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void WarmSolverInRun_OnlyAllocatesTheSolution()
        {
            // D-005: 0 bytes en el bucle caliente en Run, el modo por defecto, con el modo explícito (16.114 estados
            // expandidos, par 37; README del paquete).
            BoardState state = Board();
            var solver = new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            SolveResult warm = solver.Solve(state, options);

            long before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, options);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm.MoveCount, Is.EqualTo(37));
            Assert.That(result.MoveCount, Is.EqualTo(37));
            Assert.That(result.Expanded, Is.GreaterThan(5000));
            Assert.That(allocated, Is.LessThan(1024));
        }

        [Test]
        public void WarmSolverInSingle_OnlyAllocatesTheSolution()
        {
            // decisiones.md A4: 0 bytes en el bucle caliente también en Single, el modo de pruebas (unos 17.000 estados
            // expandidos, par 48).
            BoardState state = Board();
            var solver = new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Single);
            SolveResult warm = solver.Solve(state, options);

            long before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, options);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm.MoveCount, Is.EqualTo(48));
            Assert.That(result.MoveCount, Is.EqualTo(48));
            Assert.That(result.Expanded, Is.GreaterThan(5000));
            Assert.That(allocated, Is.LessThan(1024));
        }

        [Test]
        public void WarmSolver_OnlyAllocatesTheSolution()
        {
            // Unos 12.000 estados expandidos: si cada nodo asignara, serían cientos de KB.
            BoardState state = Board();
            var solver = new AStarSolver();
            SolveResult warm = solver.Solve(state, SolverOptions.Pipeline);

            long before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, SolverOptions.Pipeline);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm.MoveCount, Is.EqualTo(37));
            Assert.That(result.Expanded, Is.GreaterThan(5000));
            Assert.That(allocated, Is.LessThan(1024));
        }
    }
}
