#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Bot;
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

        /// <summary>Tablero de 10 cuias con Hoja, Brote, Matapi y Liana (y las mecánicas que se vayan portando).</summary>
        private static BoardState MechanicBoard()
        {
            var cuias = new Cuia[]
            {
                Cuia.Create(4, new[] { 1, 2, 3, 4 }), Cuia.Create(4, new[] { 5, 6, 1, 2 }), Cuia.Create(4, new[] { 3, 4, 5, 6 }),
                Cuia.Create(4, new[] { 6, 5, 4, 3 }), Cuia.Create(4, new[] { 2, 1, 6, 5 }).WithMechanic(MechanicType.Liana, key: 3),
                Cuia.Create(4, new[] { 4, 3, 2 }).WithMechanic(MechanicType.Brote, flags: Cuia.FlagClosed), Cuia.Create(4, new[] { 1 }),
                Cuia.Empty(4).WithMechanic(MechanicType.Hoja, key: 1, flags: Cuia.FlagClosed), Cuia.Empty(4),
                Cuia.Empty(4).WithMechanic(MechanicType.Matapi),
            };
            return new BoardState(cuias);
        }

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
        public void SolverMoves_DoNotAllocate()
        {
            // C1-4: MOVIMIENTOS de 03 §5 (la lista del solver, el bot y las métricas) sin asignaciones, en los dos modos.
            BoardState state = Board();
            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = state[i];
            }

            BoardCanon.Sort(cuias, 0, cuias.Length);
            int[] units = state.UnitsTable;
            var buffer = new SolverMove[SolverMoves.MaxMoves];
            int warm = SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Run, true, buffer);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 1000; repeat++)
            {
                total += SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Run, true, buffer);
                total += SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Single, false, buffer);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThanOrEqualTo(2000 * warm));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void WarmCasualBot_DoesNotAllocate()
        {
            // C1-5: 0 bytes por paso del bot ya calentado (40 partidas de hasta 400 vertidos).
            BoardState state = Board();
            var bot = new CasualBot();
            BotResult warm = bot.FailRate(state, 11UL, PourMode.Run);

            long before = GC.GetAllocatedBytesForCurrentThread();
            BotResult result = bot.FailRate(state, 11UL, PourMode.Run);
            BotResult other = bot.FailRate(state, 12UL, PourMode.Single);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(result.Failures, Is.EqualTo(warm.Failures));
            Assert.That(other.Runs, Is.EqualTo(BotOptions.DefaultRuns));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void WarmEarlyTraps_DoesNotAllocate()
        {
            BoardState state = Board();
            var traps = new EarlyTraps();
            int warm = traps.Count(state, PourMode.Run);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int again = traps.Count(state, PourMode.Run) + traps.CountUnavoidable(state, PourMode.Run);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(again, Is.GreaterThanOrEqualTo(warm));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void MechanicBoard_HotLoop_DoesNotAllocate()
        {
            // C1-7 a C1-9: las mecánicas se despachan por la tabla estática, sin asignaciones: movimientos útiles, bloqueo,
            // solver calentado y bot calentado sobre un tablero con Hoja.
            BoardState state = MechanicBoard();
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            var bot = new CasualBot();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            SolveResult warm = solver.Solve(state, options);
            bot.FailRate(state, 3UL, PourMode.Run);
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
            PourRules.IsStuck(state, true);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 200; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                total += PourRules.IsStuck(state, true) ? 1 : 0;
            }

            bot.FailRate(state, 3UL, PourMode.Run);
            long rulesAndBot = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, options);
            long solve = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(200 * moves));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(warm.MoveCount));
            Assert.That(rulesAndBot, Is.EqualTo(0));
            Assert.That(solve, Is.LessThan(1024));
        }

        [Test]
        public void HiddenDoses_StuckDetection_DoesNotAllocate()
        {
            // C2-1: con dosis ocultas (Aguas turbias, 02 §11) la detección de vertidos útiles y del bloqueo (R13; la excepción
            // de ocultos de 04 §5.1) no asigna memoria.
            var state = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 2, 3 }, 0x3), Cuia.Create(4, new[] { 2, 3, 1 }, 0x1), Cuia.Create(4, new[] { 3, 1, 2 }, 0x2),
                Cuia.Create(4, new[] { 2, 1, 1, 1 }, 0x1), Cuia.Create(4, new[] { 3, 2, 3 }), Cuia.Empty(4),
            });
            var buffer = new Pour[PourRules.MaxPours];
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
            bool stuck = PourRules.IsStuck(state, true);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 1000; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                total += PourRules.IsStuck(state, true) ? 1 : 0;
                total += PourRules.HasUsefulMove(state, true) ? 0 : 1;
                total += PourRules.IsFutile(state, 3, 5) ? 1 : 0;
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(state.HiddenCount, Is.EqualTo(5));
            Assert.That(stuck, Is.False);
            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(1000 * moves));
            Assert.That(allocated, Is.EqualTo(0));
        }

        [Test]
        public void BarroBoard_HotLoop_DoesNotAllocate()
        {
            // C2-3: el Barro (color 15) en movimientos útiles, bloqueo, solver calentado y bot calentado, sin asignaciones.
            var state = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, Cuia.MudColor, 2 }), Cuia.Create(4, new[] { 2, 3, Cuia.MudColor }), Cuia.Create(4, new[] { 3, 1, 4, 4 }),
                Cuia.Create(4, new[] { 4, 2, 1, 3 }), Cuia.Create(4, new[] { 1, 3, 2, 4 }), Cuia.Empty(4), Cuia.Empty(4),
            });
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            var bot = new CasualBot();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            SolveResult warm = solver.Solve(state, options);
            bot.FailRate(state, 5UL, PourMode.Run);
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 200; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                total += PourRules.IsStuck(state, true) ? 1 : 0;
            }

            bot.FailRate(state, 5UL, PourMode.Run);
            long rulesAndBot = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, options);
            long solve = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(200 * moves));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(warm.MoveCount));
            Assert.That(rulesAndBot, Is.EqualTo(0));
            Assert.That(solve, Is.LessThan(1024));
        }

        [Test]
        public void RemolinoBoard_HotLoop_DoesNotAllocate()
        {
            // C2-4: el Remolino (inserción por el fondo de la pareja, búsqueda de la pareja por id) en movimientos útiles,
            // bloqueo, solver calentado y bot calentado, sin asignaciones; con Barro en otra cuia.
            Cuia x = Cuia.Create(4, new[] { 3, 1 }).WithMechanic(MechanicType.Remolino, pairId: 0);
            Cuia y = Cuia.Create(4, new[] { 2 }).WithMechanic(MechanicType.Remolino, pairId: 0);
            var state = new BoardState(new[]
            {
                x, y, Cuia.Create(4, new[] { 1, 2, 3, 4 }), Cuia.Create(4, new[] { 4, Cuia.MudColor, 1, 2 }), Cuia.Create(4, new[] { 3, 4, 2, 1 }),
                Cuia.Create(4, new[] { 4, 3 }), Cuia.Empty(4),
            });
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            var bot = new CasualBot();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            SolveResult warm = solver.Solve(state, options);
            bot.FailRate(state, 7UL, PourMode.Run);
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 200; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                total += PourRules.IsStuck(state, true) ? 1 : 0;
            }

            bot.FailRate(state, 7UL, PourMode.Run);
            long rulesAndBot = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, options);
            long solve = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(200 * moves));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(warm.MoveCount));
            Assert.That(rulesAndBot, Is.EqualTo(0));
            Assert.That(solve, Is.LessThan(1024));
        }

        [Test]
        public void LluviaBoard_HotLoop_DoesNotAllocate()
        {
            // C3-1: la Lluvia (contador y gota en la etapa (e) de R10, punto fijo (f′)) en movimientos útiles, bloqueo, vertido
            // aplicado, solver calentado y bot calentado, sin asignaciones.
            Cuia rain = Cuia.Empty(4).WithMechanic(MechanicType.Lluvia, key: 10, n: 2, k: 3);
            var state = new BoardState(new[]
            {
                rain, Cuia.Create(4, new[] { 1, 2, 3, 10 }), Cuia.Create(4, new[] { 3, 1, 10, 2 }), Cuia.Create(4, new[] { 2, 3, 1, 1 }),
                Cuia.Create(4, new[] { 3, 2, 4, 4 }), Cuia.Create(4, new[] { 4, 4 }), Cuia.Empty(4), Cuia.Empty(4),
            });
            AssertRulesBotAndSolverDoNotAllocate(state, 9UL);
        }

        [Test]
        public void PiranaBoard_HotLoop_DoesNotAllocate()
        {
            // C3-2: la Piraña (contador, mordisco en la etapa (e) de R10 y punto fijo (f′)) en movimientos útiles, bloqueo,
            // vertido aplicado, solver calentado y bot calentado, sin asignaciones; con un Matapi (el mordisco se pierde).
            Cuia cave = Cuia.Empty(2).WithMechanic(MechanicType.Pirana, k: 3);
            var state = new BoardState(new[]
            {
                cave, Cuia.Create(4, new[] { 1, 2, 3, 4 }), Cuia.Create(4, new[] { 3, 1, 4, 2 }), Cuia.Create(4, new[] { 2, 3, 1, 1 }),
                Cuia.Create(4, new[] { 3, 2, 4, 4 }), Cuia.Empty(4).WithMechanic(MechanicType.Matapi), Cuia.Empty(4),
            });
            AssertRulesBotAndSolverDoNotAllocate(state, 13UL);
        }

        /// <summary>
        /// 0 bytes en movimientos útiles, bloqueo, <c>BoardRules.ApplyInPlace</c> y el bot calentado; menos de 1 KB en el solver
        /// calentado (solo la solución).
        /// </summary>
        private static void AssertRulesBotAndSolverDoNotAllocate(BoardState state, ulong seed)
        {
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            var bot = new CasualBot();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            SolveResult warm = solver.Solve(state, options);
            bot.FailRate(state, seed, PourMode.Run);
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
            var cuias = new Cuia[state.Count];
            var work = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = state[i];
            }

            int[] units = state.UnitsTable;
            Pour first = buffer[0];
            bool futile = BoardRules.IsFutile(cuias, 0, cuias.Length, units, first.Source, first.Target);
            var events = default(PourEvents);
            Array.Copy(cuias, work, cuias.Length);
            BoardRules.ApplyInPlace(work, 0, work.Length, units, first.Source, first.Target, PourMode.Run, futile, ref events);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 200; repeat++)
            {
                total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                total += PourRules.IsStuck(state, true) ? 1 : 0;
                Array.Copy(cuias, work, cuias.Length);
                BoardRules.ApplyInPlace(work, 0, work.Length, units, first.Source, first.Target, PourMode.Run, futile, ref events);
            }

            bot.FailRate(state, seed, PourMode.Run);
            long rulesAndBot = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            SolveResult result = solver.Solve(state, options);
            long solve = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(200 * moves));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(warm.MoveCount));
            Assert.That(rulesAndBot, Is.EqualTo(0));
            Assert.That(solve, Is.LessThan(1024));
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
