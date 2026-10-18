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
    /// <see cref="GC.GetAllocatedBytesForCurrentThread"/>, que no existe en el perfil de Unity. Todas las medidas pasan por
    /// <see cref="AllocationMeter"/> (FX9): varias pasadas de calentamiento del mismo bloque y el mínimo de hasta
    /// <see cref="AllocationMeter.MaxAttempts"/> medidas, con los mismos umbrales de siempre.
    /// </summary>
    [TestFixture]
    [NonParallelizable] // Fuera del turno paralelo de NUnit: las medidas, sin hilos de otros tests compitiendo por la máquina.
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

            int total = 0;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 1000; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, buffer);
                    total += PourRules.HasUsefulMove(state, true) ? 1 : 0;
                    total += PourRules.IsStuck(state, false) ? 1 : 0;
                    total += PourRules.IsFutile(state, 0, 12) ? 1 : 0;
                }
            }, 1);

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(1000 * (warm + 1)));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("reglas"));
        }

        [Test]
        public void UsefulMovesInRun_DoNotAllocate()
        {
            // D-005: Run, el modo por defecto, con el modo explícito (no las sobrecargas heredadas). La cantidad se calcula
            // en CuiaRules.Amount(from, to, mode), sin asignaciones.
            BoardState state = Board();
            var buffer = new Pour[PourRules.MaxPours];
            int warm = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);

            int total = 0;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 1000; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                    total += PourRules.CollectUsefulMoves(state, false, PourMode.Single, buffer);
                    total += PourRules.Amount(state, 0, 12, PourMode.Run);
                    total += CuiaRules.Amount(state[0], state[12], PourMode.Single);
                }
            }, 1);

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThan(1000 * warm));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("reglas en Run"));
        }

        [Test]
        public void UsefulMovesInSingle_DoNotAllocate()
        {
            // Single, el modo de pruebas (D-005): la cantidad del modo se calcula en CuiaRules.Amount(from, to, mode), sin
            // asignaciones.
            BoardState state = Board();
            var buffer = new Pour[PourRules.MaxPours];
            int warm = PourRules.CollectUsefulMoves(state, true, PourMode.Single, buffer);

            int total = 0;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 1000; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Single, buffer);
                    total += PourRules.CollectUsefulMoves(state, false, PourMode.Run, buffer);
                    total += PourRules.Amount(state, 0, 12, PourMode.Single);
                    total += CuiaRules.Amount(state[0], state[12], PourMode.Run);
                }
            }, 1);

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThan(1000 * warm));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("reglas en Single"));
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

            int total = 0;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 1000; repeat++)
                {
                    total += SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Run, true, buffer);
                    total += SolverMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Single, false, buffer);
                }
            }, 1);

            Assert.That(warm, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThanOrEqualTo(2000 * warm));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("MOVIMIENTOS"));
        }

        [Test]
        public void WarmCasualBot_DoesNotAllocate()
        {
            // C1-5: 0 bytes por paso del bot ya calentado (40 partidas de hasta 400 vertidos).
            BoardState state = Board();
            var bot = new CasualBot();
            BotResult warm = bot.FailRate(state, 11UL, PourMode.Run);

            BotResult result = warm;
            BotResult other = default;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                result = bot.FailRate(state, 11UL, PourMode.Run);
                other = bot.FailRate(state, 12UL, PourMode.Single);
            }, 1);

            Assert.That(result.Failures, Is.EqualTo(warm.Failures));
            Assert.That(other.Runs, Is.EqualTo(BotOptions.DefaultRuns));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("bot"));
        }

        [Test]
        public void WarmEarlyTraps_DoesNotAllocate()
        {
            BoardState state = Board();
            var traps = new EarlyTraps();
            int warm = traps.Count(state, PourMode.Run);

            int again = 0;
            AllocationReading allocated = AllocationMeter.Measure(
                () => again = traps.Count(state, PourMode.Run) + traps.CountUnavoidable(state, PourMode.Run), 1);

            Assert.That(again, Is.GreaterThanOrEqualTo(warm));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("trampas tempranas"));
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
            int moves = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);

            int total = 0;
            AllocationReading rulesAndBot = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 200; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                    total += PourRules.IsStuck(state, true) ? 1 : 0;
                }

                bot.FailRate(state, 3UL, PourMode.Run);
            }, 1);
            SolveResult result = warm;
            AllocationReading solve = AllocationMeter.Measure(() => result = solver.Solve(state, options), 1024);

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(200 * moves));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(warm.MoveCount));
            Assert.That(rulesAndBot.Bytes, Is.EqualTo(0), rulesAndBot.Describe("reglas y bot"));
            Assert.That(solve.Bytes, Is.LessThan(1024), solve.Describe("Solve"));
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

            int total = 0;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 1000; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                    total += PourRules.IsStuck(state, true) ? 1 : 0;
                    total += PourRules.HasUsefulMove(state, true) ? 0 : 1;
                    total += PourRules.IsFutile(state, 3, 5) ? 1 : 0;
                }
            }, 1);

            Assert.That(state.HiddenCount, Is.EqualTo(5));
            Assert.That(stuck, Is.False);
            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(1000 * moves));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("reglas con dosis ocultas"));
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
            AssertRulesBotAndSolverDoNotAllocate(state, 5UL, false);
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
            AssertRulesBotAndSolverDoNotAllocate(state, 7UL, false);
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
            AssertRulesBotAndSolverDoNotAllocate(state, 9UL, true);
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
            AssertRulesBotAndSolverDoNotAllocate(state, 13UL, true);
        }

        [Test]
        public void HojaAtadaBoard_HotLoop_DoesNotAllocate()
        {
            // K3a: la Hoja atada (filtro de destino persistente que sí es origen; 02 §1.2 y §1.4; propuesta de D-007 b.3) en
            // movimientos útiles, bloqueo, vertido aplicado (también el que entra en ella, sin abrirla), solver calentado y bot
            // calentado, sin asignaciones. Empieza con su fruta en el fondo.
            Cuia atada = Cuia.Create(4, new[] { 1 }).WithMechanic(MechanicType.HojaAtada, key: 1);
            var state = new BoardState(new[]
            {
                atada, Cuia.Create(4, new[] { 2, 3, 1, 4 }), Cuia.Create(4, new[] { 3, 2, 4, 1 }), Cuia.Create(4, new[] { 2, 4, 3, 1 }),
                Cuia.Create(4, new[] { 4, 2, 3 }), Cuia.Empty(4),
            });
            AssertRulesBotAndSolverDoNotAllocate(state, 19UL, true);
            AssertPourDoesNotAllocate(state, 2, 0, PourMode.Run, (in PourEvents e) => e.ChangedMask == ((1 << 0) | (1 << 2)) && e.OpenedMask == 0);
        }

        [Test]
        public void BroteDosHojasBoard_HotLoop_DoesNotAllocate()
        {
            // K3a: el Brote de dos hojas (02 R10 b; propuesta de D-007 b.3: resta las dosis recibidas en la etapa (b)) en
            // movimientos útiles, bloqueo, vertido aplicado (el que abre una hoja sin germinar, en single, y el tramo de 2 que lo
            // abre, en run), solver calentado y bot calentado, sin asignaciones.
            Cuia brote = Cuia.Create(4, new[] { 2, 1 }).WithMechanic(MechanicType.Brote, n: 2, flags: Cuia.FlagClosed);
            var state = new BoardState(new[]
            {
                brote, Cuia.Create(4, new[] { 3, 4, 1, 1 }), Cuia.Create(4, new[] { 4, 3, 2, 2 }), Cuia.Create(4, new[] { 3, 4 }),
                Cuia.Create(4, new[] { 2, 4, 3, 1 }), Cuia.Empty(4),
            });
            AssertRulesBotAndSolverDoNotAllocate(state, 23UL, true);
            AssertPourDoesNotAllocate(state, 1, 0, PourMode.Single, (in PourEvents e) => e.BroteLeafOpenedMask == 1 << 0);
            AssertPourDoesNotAllocate(state, 1, 0, PourMode.Run, (in PourEvents e) => e.BroteOpenedMask == 1 << 0);
        }

        [Test]
        public void FlorBoard_HotLoop_DoesNotAllocate()
        {
            // K3b: la Flor de vitória-régia (cerrada no es origen ni destino; se abre en la etapa (d) de R10 al haber selladas
            // sus dos frutas; 02 §1.2 y R10 d; propuesta de D-007 b.3) en movimientos útiles, bloqueo, vertido aplicado (también
            // el que sella la segunda fruta y la abre), solver calentado y bot calentado, sin asignaciones.
            Cuia flor = Cuia.Create(4, new[] { 3, 4 }).WithMechanic(MechanicType.Flor, key: 1, key2: 2);
            var state = new BoardState(new[]
            {
                flor, Cuia.Create(4, new[] { 2, 3, 1, 4 }), Cuia.Create(4, new[] { 3, 2, 4, 1 }), Cuia.Create(4, new[] { 2, 4, 3, 1 }),
                Cuia.Create(4, new[] { 1, 2 }), Cuia.Empty(4),
            });
            AssertRulesBotAndSolverDoNotAllocate(state, 29UL, true);

            Cuia closed = Cuia.Create(4, new[] { 3, 3 }).WithMechanic(MechanicType.Flor, key: 1, key2: 2);
            var opening = new BoardState(new[]
            {
                closed, Cuia.Create(4, new[] { 2 }), Cuia.Create(4, new[] { 1, 1, 1, 1 }), Cuia.Create(4, new[] { 2, 2, 2 }),
                Cuia.Create(4, new[] { 3, 3 }), Cuia.Empty(4),
            });
            AssertPourDoesNotAllocate(opening, 1, 3, PourMode.Run, (in PourEvents e) => e.ReleasedMask == 1 << 0 && e.WasSealed(3));
        }

        private delegate bool EventCheck(in PourEvents events);

        /// <summary>
        /// El vertido <paramref name="source"/> → <paramref name="target"/> aplicado 200 veces sobre una copia del tablero no
        /// asigna (<see cref="AllocationMeter"/>), y sus eventos son los esperados.
        /// </summary>
        private static void AssertPourDoesNotAllocate(BoardState state, int source, int target, PourMode mode, EventCheck expected)
        {
            int width = state.Count;
            var cuias = new Cuia[width];
            var work = new Cuia[width];
            for (int i = 0; i < width; i++)
            {
                cuias[i] = state[i];
            }

            int[] units = state.UnitsTable;
            bool futile = BoardRules.IsFutile(cuias, 0, width, units, source, target);
            var events = default(PourEvents);
            Array.Copy(cuias, work, width);
            BoardRules.ApplyInPlace(work, 0, width, units, source, target, mode, futile, ref events);
            Assert.That(expected(in events), Is.True, "eventos del vertido " + source + "→" + target + " en " + mode);

            AllocationReading bytes = AllocationMeter.Measure(() =>
            {
                for (int repeat = 0; repeat < 200; repeat++)
                {
                    Array.Copy(cuias, work, width);
                    BoardRules.ApplyInPlace(work, 0, width, units, source, target, mode, futile, ref events);
                }
            }, 1);

            Assert.That(bytes.Bytes, Is.EqualTo(0), bytes.Describe("vertido " + source + "→" + target + " en " + mode));
        }

        [Test]
        public void TinajaBoard_HotLoop_DoesNotAllocate()
        {
            // M13 (T55; propuesta de la tinaja, c.6): con la tinaja y su cuia (filtro de origen al principio de Accepts, suelta
            // en la etapa (f) de R10 y punto fijo (f′)), 0 bytes en movimientos útiles, bloqueo, vertido aplicado (también el que
            // vacía la tinaja y suelta su cuia) y bot calentado; menos de 1 KB en el solver calentado (sin poda de fútiles, b.2
            // R14) y por realidad en SafeMoves, también cuando no hay vertido seguro y explica por qué (Outcome).
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
            var events = default(PourEvents);
            Array.Copy(lastDose, work, width);
            BoardRules.ApplyInPlace(work, 0, width, units, 4, 5, PourMode.Run, false, ref events);
            Assert.That(events.CucharonReleasedMask, Is.EqualTo(1 << 5));

            int total = 0;
            AllocationReading rules = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 200; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                    total += PourRules.IsStuck(state, true) ? 1 : 0;
                    Array.Copy(cuias, work, width);
                    BoardRules.ApplyInPlace(work, 0, width, units, 4, 5, PourMode.Run, false, ref events);
                    Array.Copy(lastDose, work, width);
                    BoardRules.ApplyInPlace(work, 0, width, units, 4, 5, PourMode.Run, false, ref events);
                }
            }, 1);

            var bot = new CasualBot();
            AllocationReading botBytes = AllocationMeter.Measure(() => bot.FailRate(state, 17UL, PourMode.Run), 1);

            var solver = new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run, state);
            SolveResult warm = solver.Solve(state, options);
            AllocationReading solve = AllocationMeter.Measure(() => solver.Solve(state, options), 1024);

            var safe = new SafeMoves();
            SolverOptions realityOptions = SafeMoves.ForPipeline(PourMode.Run, state);
            safe.Find(state, realityOptions);
            AllocationReading safeBytes = AllocationMeter.Measure(() => safe.ClearMemo(), () => safe.Find(state, realityOptions), 1024L * safe.RealityCount);

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
            AllocationReading explainBytes = AllocationMeter.Measure(() => safe.ClearMemo(), () => safe.Find(lost, lostOptions), 1024L * safe.RealityCount);

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.GreaterThan(0));
            Assert.That(rules.Bytes, Is.EqualTo(0), rules.Describe("reglas"));
            Assert.That(botBytes.Bytes, Is.EqualTo(0), botBytes.Describe("bot"));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(options.FutileRuleEnabled, Is.False);
            Assert.That(solve.Bytes, Is.LessThan(1024), solve.Describe("Solve"));
            Assert.That(safeCount, Is.GreaterThan(0));
            Assert.That(found, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(safeBytes.Bytes, Is.LessThan(1024L * realities), safeBytes.Describe("SafeMoves"));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.NoExit));
            Assert.That(explainBytes.Bytes, Is.LessThan(1024L * safe.RealityCount), explainBytes.Describe("SafeMoves sin vertido seguro"));
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
            // SolverMoves, IsStuck y BoardRules.ApplyInPlace; menos de 1 KB por Solve y por partida del bot ya calentados (el
            // bot, de hecho, 0 bytes); menos de 1 KB por realidad en SafeMoves (tableros con Aguas turbias).
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
            var events = default(PourEvents);

            int total = 0;
            AllocationReading rules = AllocationMeter.Measure(() =>
            {
                total = 0;
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
            }, 1);

            var solver = new AStarSolver();
            SolverOptions options = new SolverOptions(Math.Min(board.ExpandedLimit, ReferenceSolveBudget), 1, true, PourMode.Run).ForLevel(state);
            SolveResult warm = solver.Solve(state, options);
            SolveResult again = warm;
            AllocationReading solve = AllocationMeter.Measure(() => again = solver.Solve(state, options), 1024);

            var bot = new CasualBot();
            BotResult games = default;
            AllocationReading botBytes = AllocationMeter.Measure(() => games = bot.FailRate(state, 5UL, PourMode.Run), 1);

            Assert.That(count, Is.GreaterThan(0), name);
            Assert.That(total, Is.GreaterThan(0), name);
            Assert.That(rules.Bytes, Is.EqualTo(0), rules.Describe(name + ": reglas"));
            Assert.That(again.Status, Is.EqualTo(warm.Status), name);
            Assert.That(solve.Bytes, Is.LessThan(1024), solve.Describe(name + ": Solve"));
            Assert.That(botBytes.Bytes, Is.LessThan(1024L * games.Runs), botBytes.Describe(name + ": bot"));
            Assert.That(botBytes.Bytes, Is.EqualTo(0), botBytes.Describe(name + ": bot"));

            if (state.HasHidden)
            {
                var safe = new SafeMoves();
                SolverOptions realityOptions = new SolverOptions(ReferenceRealityBudget, 1, true, PourMode.Run).ForLevel(state);
                RealityStatus status = safe.Find(state, realityOptions);
                AllocationReading safeBytes = AllocationMeter.Measure(
                    () => safe.ClearMemo(), () => status = safe.Find(state, realityOptions), 1024L * safe.RealityCount);

                Assert.That(status, Is.EqualTo(RealityStatus.Ok), name);
                Assert.That(safe.RealityCount, Is.InRange(1, 24), name);
                Assert.That(safeBytes.Bytes, Is.LessThan(1024L * safe.RealityCount), safeBytes.Describe(name + ": SafeMoves"));
            }
        }

        /// <summary>
        /// 0 bytes en movimientos útiles, bloqueo, <c>BoardRules.ApplyInPlace</c> (si <paramref name="applyPour"/>) y el bot
        /// calentado; menos de 1 KB en el solver calentado (solo la solución).
        /// </summary>
        private static void AssertRulesBotAndSolverDoNotAllocate(BoardState state, ulong seed, bool applyPour)
        {
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            var bot = new CasualBot();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            SolveResult warm = solver.Solve(state, options);
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

            int total = 0;
            AllocationReading rulesAndBot = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 200; repeat++)
                {
                    total += PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                    total += PourRules.IsStuck(state, true) ? 1 : 0;
                    if (applyPour)
                    {
                        Array.Copy(cuias, work, cuias.Length);
                        BoardRules.ApplyInPlace(work, 0, work.Length, units, first.Source, first.Target, PourMode.Run, futile, ref events);
                    }
                }

                bot.FailRate(state, seed, PourMode.Run);
            }, 1);
            SolveResult result = warm;
            AllocationReading solve = AllocationMeter.Measure(() => result = solver.Solve(state, options), 1024);

            Assert.That(moves, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(200 * moves));
            Assert.That(warm.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(warm.MoveCount));
            Assert.That(rulesAndBot.Bytes, Is.EqualTo(0), rulesAndBot.Describe("reglas y bot"));
            Assert.That(solve.Bytes, Is.LessThan(1024), solve.Describe("Solve"));
        }

        [Test]
        public void WarmDeadEndProbe_DoesNotAllocate()
        {
            // R13 ampliada (D-014; CLAUDE.md, regla dura 2): el cierre acotado de LevelSession no asigna una vez calentado, ni
            // cuando se recorre entero (nivel 66), ni cuando se llena (tablero de 14 cuias), ni con mecánicas (los 40 tableros de
            // referencia), en los dos modos.
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

            int total = 0;
            AllocationReading allocated = AllocationMeter.Measure(() =>
            {
                total = 0;
                for (int repeat = 0; repeat < 20; repeat++)
                {
                    for (int i = 0; i < boards.Length; i++)
                    {
                        total += probe.IsDeadEnd(boards[i], PourMode.Run) ? 1 : 0;
                        total += probe.IsDeadEnd(boards[i], PourMode.Single) ? 1 : 0;
                    }
                }
            }, 1);

            Assert.That(level66, Is.True);
            Assert.That(total, Is.EqualTo(20 * warm));
            Assert.That(allocated.Bytes, Is.EqualTo(0), allocated.Describe("cierre acotado"));
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

            SolveResult result = warm;
            AllocationReading allocated = AllocationMeter.Measure(() => result = solver.Solve(state, options), 1024);

            Assert.That(warm.MoveCount, Is.EqualTo(37));
            Assert.That(result.MoveCount, Is.EqualTo(37));
            Assert.That(result.Expanded, Is.GreaterThan(5000));
            Assert.That(allocated.Bytes, Is.LessThan(1024), allocated.Describe("Solve en Run"));
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

            SolveResult result = warm;
            AllocationReading allocated = AllocationMeter.Measure(() => result = solver.Solve(state, options), 1024);

            Assert.That(warm.MoveCount, Is.EqualTo(48));
            Assert.That(result.MoveCount, Is.EqualTo(48));
            Assert.That(result.Expanded, Is.GreaterThan(5000));
            Assert.That(allocated.Bytes, Is.LessThan(1024), allocated.Describe("Solve en Single"));
        }

        [Test]
        public void SolverCancellation_UnmarkedOrCutting_DoesNotAllocate()
        {
            // K2 (CLAUDE.md, regla dura 2): la consulta de la bandera cada 256 expansiones no asigna. Con la bandera sin marcar,
            // el solver calentado expande lo mismo que sin ella y solo asigna la solución; cortado a mitad (sin solución), 0
            // bytes; y SafeMoves cortado en su 3.ª búsqueda (Aguas turbias, opciones de Morpho), 0 bytes.
            BoardState state = Board();
            var flag = new SolverCancellation();
            var solver = new AStarSolver();
            SolverOptions plainOptions = SolverOptions.ForPipeline(PourMode.Run);
            SolverOptions options = plainOptions.WithCancellation(flag);
            SolveResult plain = solver.Solve(state, plainOptions);
            SolveResult warm = solver.Solve(state, options);
            SolveResult again = warm;
            AllocationReading watched = AllocationMeter.Measure(() => again = solver.Solve(state, options), 1024);

            SolveResult cut = default;
            AllocationReading cutBytes = AllocationMeter.Measure(
                () =>
                {
                    flag.Reset();
                    flag.RequestAtPoll(20);
                },
                () => cut = solver.Solve(state, options),
                1);

            BoardState murky = ReferenceBoards.Get("Tu").State;
            SolverOptions murkyOptions = new SolverOptions(40000, 2, true, PourMode.Run).ForLevel(murky).WithCancellation(flag);
            var safe = new SafeMoves();
            flag.Reset();
            safe.Find(murky, murkyOptions);
            int searches = safe.Searches;
            AllocationReading safeBytes = AllocationMeter.Measure(
                () =>
                {
                    safe.ClearMemo();
                    flag.Reset();
                    flag.RequestAtPoll(3);
                },
                () => safe.Find(murky, murkyOptions),
                1);

            Assert.That(plain.MoveCount, Is.EqualTo(37));
            Assert.That(warm.Expanded, Is.EqualTo(plain.Expanded));
            Assert.That(again.Expanded, Is.EqualTo(plain.Expanded));
            Assert.That(again.MoveCount, Is.EqualTo(37));
            Assert.That(again.WasCancelled, Is.False);
            Assert.That(watched.Bytes, Is.LessThan(1024), watched.Describe("Solve con la bandera sin marcar"));
            Assert.That(cut.WasCancelled, Is.True);
            Assert.That(cut.Expanded, Is.EqualTo(19 * SolverCancellation.PollInterval));
            Assert.That(cutBytes.Bytes, Is.EqualTo(0), cutBytes.Describe("Solve cortado"));
            Assert.That(searches, Is.GreaterThan(3));
            Assert.That(safe.WasCancelled, Is.True);
            Assert.That(safe.Searches, Is.EqualTo(3));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded));
            Assert.That(safeBytes.Bytes, Is.EqualTo(0), safeBytes.Describe("SafeMoves cortado"));
        }

        [Test]
        public void WarmSolver_OnlyAllocatesTheSolution()
        {
            // Unos 12.000 estados expandidos: si cada nodo asignara, serían cientos de KB.
            BoardState state = Board();
            var solver = new AStarSolver();
            SolveResult warm = solver.Solve(state, SolverOptions.Pipeline);

            SolveResult result = warm;
            AllocationReading allocated = AllocationMeter.Measure(() => result = solver.Solve(state, SolverOptions.Pipeline), 1024);

            Assert.That(warm.MoveCount, Is.EqualTo(37));
            Assert.That(result.Expanded, Is.GreaterThan(5000));
            Assert.That(allocated.Bytes, Is.LessThan(1024), allocated.Describe("Solve"));
        }
    }
}
