#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Solver.Bot;
using AF.Solver.Murky;
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
    [NonParallelizable] // MechanicsPerformanceTests corre en paralelo: sus recolecciones suman ~8 KB a GetAllocatedBytesForCurrentThread.
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

        [Test]
        public void TinajaBoard_HotLoop_DoesNotAllocate()
        {
            // M13 (T55; propuesta de la tinaja, c.6): con la tinaja y su cuia (filtro de origen al principio de Accepts, suelta
            // en la etapa (f) de R10 y punto fijo (f′)), 0 bytes en movimientos útiles, bloqueo, vertido aplicado (también el que
            // vacía la tinaja y suelta su cuia) y bot calentado; menos de 1 KB en el solver calentado (sin poda de fútiles, b.2
            // R14) y por realidad en SafeMoves, también cuando no hay vertido seguro y explica por qué (Outcome). El mínimo de 3
            // intentos, como en ReferenceBoard_HotLoop_DoesNotAllocate.
            Cuia hung = Cuia.Empty(2).WithMechanic(MechanicType.Cucharon);
            Cuia[] cuias =
            {
                Cuia.Create(4, new[] { 1, 2, 3, 4 }), Cuia.Create(4, new[] { 3, 1, 4, 2 }), Cuia.Create(4, new[] { 2, 3, 4, 1 }),
                Cuia.Create(4, new[] { 4 }), Cuia.Create(5, new[] { 1, 2, 3 }, 0x3).WithMechanic(MechanicType.Tinaja), hung, Cuia.Empty(4),
            };
            var state = new BoardState(cuias);
            var lastDose = (Cuia[])cuias.Clone();
            lastDose[4] = Cuia.Create(5, new[] { 3 }).WithMechanic(MechanicType.Tinaja);
            int[] units = state.UnitsTable;
            int width = cuias.Length;
            var work = new Cuia[width];
            var buffer = new Pour[PourRules.MaxPours];
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
            PourRules.IsStuck(state, true);
            var events = default(PourEvents);
            Array.Copy(lastDose, work, width);
            BoardRules.ApplyInPlace(work, 0, width, units, 4, 5, PourMode.Run, false, ref events);
            Assert.That(events.CucharonReleasedMask, Is.EqualTo(1 << 5));

            long rules = long.MaxValue;
            int total = 0;
            for (int attempt = 0; attempt < 3 && rules != 0; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int repeat = 0; repeat < 200; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                    total += PourRules.IsStuck(state, true) ? 1 : 0;
                    Array.Copy(cuias, work, width);
                    BoardRules.ApplyInPlace(work, 0, width, units, 4, 5, PourMode.Run, false, ref events);
                    Array.Copy(lastDose, work, width);
                    BoardRules.ApplyInPlace(work, 0, width, units, 4, 5, PourMode.Run, false, ref events);
                }

                rules = Math.Min(rules, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            var bot = new CasualBot();
            bot.FailRate(state, 17UL, PourMode.Run);
            long botBytes = long.MaxValue;
            for (int attempt = 0; attempt < 3 && botBytes != 0; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                bot.FailRate(state, 17UL, PourMode.Run);
                botBytes = Math.Min(botBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            var solver = new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run, state);
            SolveResult warm = solver.Solve(state, options);
            long solve = long.MaxValue;
            for (int attempt = 0; attempt < 3 && solve >= 1024; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                solver.Solve(state, options);
                solve = Math.Min(solve, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            var safe = new SafeMoves();
            SolverOptions realityOptions = SafeMoves.ForPipeline(PourMode.Run, state);
            safe.Find(state, realityOptions);
            long safeBytes = long.MaxValue;
            for (int attempt = 0; attempt < 3 && safeBytes >= 1024L * safe.RealityCount; attempt++)
            {
                safe.ClearMemo();
                long before = GC.GetAllocatedBytesForCurrentThread();
                safe.Find(state, realityOptions);
                safeBytes = Math.Min(safeBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            int safeCount = safe.Count;
            SafeMovesOutcome found = safe.Outcome;
            int realities = safe.RealityCount;

            // Tablero perdido de b.2: ningún vertido seguro y Outcome = NoExit (Explain resuelve el estado en cada realidad).
            var lost = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 3, 3, 3 }), Cuia.Create(4, new[] { 1, 2, 1, 1 }), Cuia.Create(4, new[] { 2, 2, 2 }),
                Cuia.Create(5, new[] { 3, 1 }, 0x1).WithMechanic(MechanicType.Tinaja), hung,
            });
            SolverOptions lostOptions = SafeMoves.ForPipeline(PourMode.Run, lost);
            safe.Find(lost, lostOptions);
            long explainBytes = long.MaxValue;
            for (int attempt = 0; attempt < 3 && explainBytes >= 1024L * safe.RealityCount; attempt++)
            {
                safe.ClearMemo();
                long before = GC.GetAllocatedBytesForCurrentThread();
                safe.Find(lost, lostOptions);
                explainBytes = Math.Min(explainBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThan(0));
            Assert.That(rules, Is.EqualTo(0), "reglas");
            Assert.That(botBytes, Is.EqualTo(0), "bot");
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(options.FutileRuleEnabled, Is.False);
            Assert.That(solve, Is.LessThan(1024), "Solve");
            Assert.That(safeCount, Is.GreaterThan(0));
            Assert.That(found, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(safeBytes, Is.LessThan(1024L * realities), "SafeMoves");
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.NoExit));
            Assert.That(explainBytes, Is.LessThan(1024L * safe.RealityCount), "SafeMoves sin vertido seguro");
        }

        /// <summary>Presupuesto del solver en <see cref="ReferenceBoard_HotLoop_DoesNotAllocate"/>: el bucle es el mismo y la suite sigue rápida.</summary>
        private const int ReferenceSolveBudget = 5000;

        /// <summary>Presupuesto por realidad de <see cref="SafeMoves"/> en el mismo test (el de Morpho en el cliente sería mayor).</summary>
        private const int ReferenceRealityBudget = 500;

        private static string[] ReferenceNames()
        {
            return ReferenceBoards.Names();
        }

        [TestCaseSource(nameof(ReferenceNames))]
        public void ReferenceBoard_HotLoop_DoesNotAllocate(string name)
        {
            // C3-3 (CLAUDE.md, regla dura 2), con todas las mecánicas y sus combinaciones del calendario d.4: 0 bytes en
            // SolverMoves, IsStuck y BoardRules.ApplyInPlace; menos de 1 KB por Solve y por partida del bot ya calentados; menos
            // de 1 KB por realidad en SafeMoves (tableros con Aguas turbias). No se ejecuta en paralelo: con otros hilos
            // asignando, una recolección puede hacer saltar GetAllocatedBytesForCurrentThread unos 8 KB (el contexto de
            // asignación del hilo) sin que el código asigne nada.
            ReferenceBoard board = ReferenceBoards.Get(name);
            BoardState state = board.State;
            int width = state.Count;
            var cuias = new Cuia[width];
            var work = new Cuia[width];
            for (int i = 0; i < width; i++)
            {
                cuias[i] = state[i];
            }

            BoardCanon.Sort(cuias, 0, width);
            int[] units = state.UnitsTable;
            var moves = new SolverMove[SolverMoves.MaxMoves];
            var applied = new SolverMove[SolverMoves.MaxMoves];
            int count = SolverMoves.Collect(cuias, 0, width, units, PourMode.Run, true, applied);
            SolverMoves.Collect(cuias, 0, width, units, PourMode.Single, false, moves);
            BoardRules.IsStuck(cuias, 0, width, units, true);
            var events = default(PourEvents);
            Array.Copy(cuias, work, width);
            BoardRules.ApplyInPlace(work, 0, width, units, applied[0].Source, applied[0].Target, PourMode.Run, applied[0].IsFutile, ref events);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int total = 0;
            for (int repeat = 0; repeat < 50; repeat++)
            {
                total += SolverMoves.Collect(cuias, 0, width, units, PourMode.Run, true, moves);
                total += SolverMoves.Collect(cuias, 0, width, units, PourMode.Single, false, moves);
                total += BoardRules.IsStuck(cuias, 0, width, units, true) ? 1 : 0;
                for (int m = 0; m < count; m++)
                {
                    Array.Copy(cuias, work, width);
                    BoardRules.ApplyInPlace(work, 0, width, units, applied[m].Source, applied[m].Target, PourMode.Run, applied[m].IsFutile, ref events);
                }
            }

            long rules = GC.GetAllocatedBytesForCurrentThread() - before;

            // Solve y bot: el mínimo de hasta 3 mediciones. Con la máquina muy cargada (Unity y otros procesos compitiendo),
            // la promoción del JIT por niveles o una recolección pueden sumar unos 8 KB a una sola medición sin que el código
            // asigne; una asignación real del bucle se repite en cada intento, así que el mínimo la sigue detectando.
            var solver = new AStarSolver();
            SolverOptions options = new SolverOptions(Math.Min(board.ExpandedLimit, ReferenceSolveBudget), 1, true, PourMode.Run).ForLevel(state);
            SolveResult warm = solver.Solve(state, options);
            SolveResult again = warm;
            long solve = long.MaxValue;
            for (int attempt = 0; attempt < 3 && solve >= 1024; attempt++)
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                again = solver.Solve(state, options);
                solve = Math.Min(solve, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            var bot = new CasualBot();
            BotResult games = bot.FailRate(state, 5UL, PourMode.Run);
            long botBytes = long.MaxValue;
            for (int attempt = 0; attempt < 3 && botBytes != 0; attempt++)
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                games = bot.FailRate(state, 5UL, PourMode.Run);
                botBytes = Math.Min(botBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.That(count, Is.GreaterThan(0), name);
            Assert.That(total, Is.GreaterThan(0), name);
            Assert.That(rules, Is.EqualTo(0), name + ": reglas");
            Assert.That(again.Status, Is.EqualTo(warm.Status), name);
            Assert.That(solve, Is.LessThan(1024), name + ": Solve");
            Assert.That(botBytes, Is.LessThan(1024L * games.Runs), name + ": bot");
            Assert.That(botBytes, Is.EqualTo(0), name + ": bot");

            if (state.HasHidden)
            {
                var safe = new SafeMoves();
                SolverOptions realityOptions = new SolverOptions(ReferenceRealityBudget, 1, true, PourMode.Run).ForLevel(state);
                safe.Find(state, realityOptions);
                safe.ClearMemo();
                before = GC.GetAllocatedBytesForCurrentThread();
                RealityStatus status = safe.Find(state, realityOptions);
                long safeBytes = GC.GetAllocatedBytesForCurrentThread() - before;

                Assert.That(status, Is.EqualTo(RealityStatus.Ok), name);
                Assert.That(safe.RealityCount, Is.InRange(1, 24), name);
                Assert.That(safeBytes, Is.LessThan(1024L * safe.RealityCount), name + ": SafeMoves");
            }
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
        public void WarmDeadEndProbe_DoesNotAllocate()
        {
            // R13 ampliada (D-014; CLAUDE.md, regla dura 2): el cierre acotado de LevelSession no asigna una vez calentado, ni
            // cuando se recorre entero (nivel 66), ni cuando se llena (tablero de 14 cuias), ni con mecánicas (los 36 tableros de
            // referencia), en los dos modos. El mínimo de hasta 3 mediciones, como en ReferenceBoard_HotLoop_DoesNotAllocate:
            // una asignación real del recorrido se repite en cada intento.
            var boards = new BoardState[ReferenceBoards.All.Length + 2];
            boards[0] = Level66Board.Final(); // tras el bucle del dueño (D-014): el cierre se recorre entero y se declara
            boards[1] = Board();
            for (int i = 0; i < ReferenceBoards.All.Length; i++)
            {
                boards[i + 2] = ReferenceBoards.All[i].State;
            }

            var probe = new DeadEndProbe();
            int warm = 0;
            for (int i = 0; i < boards.Length; i++)
            {
                warm += probe.IsDeadEnd(boards[i], PourMode.Run) ? 1 : 0;
                warm += probe.IsDeadEnd(boards[i], PourMode.Single) ? 1 : 0;
            }

            bool level66 = probe.IsDeadEnd(boards[0], PourMode.Run);

            long allocated = long.MaxValue;
            int total = 0;
            for (int attempt = 0; attempt < 3 && allocated != 0; attempt++)
            {
                total = 0;
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int repeat = 0; repeat < 20; repeat++)
                {
                    for (int i = 0; i < boards.Length; i++)
                    {
                        total += probe.IsDeadEnd(boards[i], PourMode.Run) ? 1 : 0;
                        total += probe.IsDeadEnd(boards[i], PourMode.Single) ? 1 : 0;
                    }
                }

                allocated = Math.Min(allocated, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.That(level66, Is.True);
            Assert.That(total, Is.EqualTo(20 * warm));
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
        public void SolverCancellation_UnmarkedOrCutting_DoesNotAllocate()
        {
            // K2 (CLAUDE.md, regla dura 2): la consulta de la bandera cada 256 expansiones no asigna. Con la bandera sin marcar,
            // el solver calentado expande lo mismo que sin ella y solo asigna la solución; cortado a mitad (sin solución), 0
            // bytes; y SafeMoves cortado en su 3.ª búsqueda (Aguas turbias, opciones de Morpho), 0 bytes. El mínimo de hasta 3
            // mediciones, como en ReferenceBoard_HotLoop_DoesNotAllocate.
            BoardState state = Board();
            var flag = new SolverCancellation();
            var solver = new AStarSolver();
            SolverOptions plainOptions = SolverOptions.ForPipeline(PourMode.Run);
            SolverOptions options = plainOptions.WithCancellation(flag);
            SolveResult plain = solver.Solve(state, plainOptions);
            SolveResult warm = solver.Solve(state, options);
            SolveResult again = warm;
            long watched = long.MaxValue;
            for (int attempt = 0; attempt < 3 && watched >= 1024; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                again = solver.Solve(state, options);
                watched = Math.Min(watched, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            SolveResult cut = default;
            long cutBytes = long.MaxValue;
            for (int attempt = 0; attempt < 3 && cutBytes != 0; attempt++)
            {
                flag.Reset();
                flag.RequestAtPoll(20);
                long before = GC.GetAllocatedBytesForCurrentThread();
                cut = solver.Solve(state, options);
                cutBytes = Math.Min(cutBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            BoardState murky = ReferenceBoards.Get("Tu").State;
            SolverOptions murkyOptions = new SolverOptions(40000, 2, true, PourMode.Run).ForLevel(murky).WithCancellation(flag);
            var safe = new SafeMoves();
            flag.Reset();
            safe.Find(murky, murkyOptions);
            int searches = safe.Searches;
            long safeBytes = long.MaxValue;
            for (int attempt = 0; attempt < 3 && safeBytes != 0; attempt++)
            {
                safe.ClearMemo();
                flag.Reset();
                flag.RequestAtPoll(3);
                long before = GC.GetAllocatedBytesForCurrentThread();
                safe.Find(murky, murkyOptions);
                safeBytes = Math.Min(safeBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.That(plain.MoveCount, Is.EqualTo(37));
            Assert.That(warm.Expanded, Is.EqualTo(plain.Expanded));
            Assert.That(again.Expanded, Is.EqualTo(plain.Expanded));
            Assert.That(again.MoveCount, Is.EqualTo(37));
            Assert.That(again.WasCancelled, Is.False);
            Assert.That(watched, Is.LessThan(1024), "Solve con la bandera sin marcar");
            Assert.That(cut.WasCancelled, Is.True);
            Assert.That(cut.Expanded, Is.EqualTo(19 * SolverCancellation.PollInterval));
            Assert.That(cutBytes, Is.EqualTo(0), "Solve cortado");
            Assert.That(searches, Is.GreaterThan(3));
            Assert.That(safe.WasCancelled, Is.True);
            Assert.That(safe.Searches, Is.EqualTo(3));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded));
            Assert.That(safeBytes, Is.EqualTo(0), "SafeMoves cortado");
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
