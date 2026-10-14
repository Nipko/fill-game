#nullable enable
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Solver en hilos (K2; 03 §5: «el cliente ejecuta el solver en un hilo y cancela por tiempo», <c>hint_timeout_ms</c> de
    /// 08 §9): una instancia de <see cref="AStarSolver"/> o <see cref="SafeMoves"/> por hilo, los <see cref="BoardState"/>
    /// (inmutables) y una <see cref="SolverCancellation"/> compartidos, y el mismo resultado que en serie; con Cancellation
    /// null, los tableros de <see cref="ReferenceBoards"/> dan los mismos expandidos y resultados; y un corte marcado de verdad
    /// desde otro hilo. Los casos deterministas (marcada antes, tras N expansiones) están en los tests compartidos
    /// <c>SolverCancellationTests</c>. Solo dotnet: usa <see cref="Thread"/>.
    /// </summary>
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public sealed class SolverThreadingTests
    {
        /// <summary><c>hint_budget_nodes</c> de Morpho (03 §5; 08 §9).</summary>
        private const int MorphoBudget = 40000;

        /// <summary>Peso de Morpho (03 §5).</summary>
        private const int MorphoWeight = 2;

        /// <summary>Hilos con su propia instancia.</summary>
        private const int Workers = 4;

        /// <summary>
        /// Ocho tableros de referencia baratos y variados: una o dos mecánicas, capacidades mixtas y tres con dosis ocultas
        /// (Aguas turbias y la Tinaja) para <see cref="SafeMoves"/>.
        /// </summary>
        private static readonly string[] EightBoards = { "Ch", "Al", "Ho", "Tu", "Ba", "Br", "Ho+Tu", "Ti" };

        private static readonly TimeSpan JoinTimeout = TimeSpan.FromMinutes(5);

        private static string[] Names()
        {
            return ReferenceBoards.Names();
        }

        private static SolverOptions MorphoFor(BoardState state)
        {
            return new SolverOptions(MorphoBudget, MorphoWeight, true, PourMode.Run).ForLevel(state);
        }

        /// <summary>
        /// Huella de las búsquedas de un tablero: A* del pipeline (w = 1, 300.000), A* de Morpho (w = 2, 40.000) y, con dosis
        /// ocultas, <see cref="SafeMoves"/> con las opciones de Morpho y el memo vacío (que el orden de los tableros no cuente).
        /// </summary>
        private static string Fingerprint(AStarSolver solver, SafeMoves safe, BoardState state, SolverCancellation? flag)
        {
            return Fingerprint(solver, safe, state, flag, out _);
        }

        private static string Fingerprint(AStarSolver solver, SafeMoves safe, BoardState state, SolverCancellation? flag, out SolveResult pipeline)
        {
            var text = new StringBuilder();
            pipeline = solver.Solve(state, SolverOptions.ForPipeline(PourMode.Run, state).WithCancellation(flag));
            Append(text, "pipeline", pipeline);
            Append(text, "morpho", solver.Solve(state, MorphoFor(state).WithCancellation(flag)));
            if (state.HasHidden)
            {
                safe.ClearMemo();
                RealityStatus status = safe.Find(state, MorphoFor(state).WithCancellation(flag));
                text.Append("safe ").Append(status).Append(' ').Append(safe.Outcome)
                    .Append(" R=").Append(safe.RealityCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" n=").Append(safe.Count.ToString(CultureInfo.InvariantCulture))
                    .Append(" searches=").Append(safe.Searches.ToString(CultureInfo.InvariantCulture))
                    .Append(" expanded=").Append(safe.Expanded.ToString(CultureInfo.InvariantCulture))
                    .Append(" memo=").Append(safe.MemoCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" cancelled=").Append(safe.WasCancelled ? "1" : "0").Append(':');
                for (int i = 0; i < safe.Count; i++)
                {
                    text.Append(' ').Append(safe.GetMove(i));
                }

                text.Append('\n');
            }

            return text.ToString();
        }

        private static void Append(StringBuilder text, string label, SolveResult result)
        {
            text.Append(label).Append(' ').Append(result.Status)
                .Append(" expanded=").Append(result.Expanded.ToString(CultureInfo.InvariantCulture))
                .Append(" stored=").Append(result.StoredStates.ToString(CultureInfo.InvariantCulture))
                .Append(" cancelled=").Append(result.WasCancelled ? "1" : "0").Append(':');
            for (int i = 0; i < result.MoveCount; i++)
            {
                text.Append(' ').Append(result.GetMove(i));
            }

            text.Append('\n');
        }

        [Test]
        public void FourInstancesInFourThreads_OverEightBoards_GiveTheSameAsInSeries()
        {
            // K2 (regla de hilos del README): cada hilo con su AStarSolver y su SafeMoves, reutilizados de un tablero a otro y
            // empezando cada uno por un tablero distinto; los tableros y una bandera sin marcar, compartidos. En serie, con una
            // sola instancia y sin bandera.
            var boards = new BoardState[EightBoards.Length];
            var expected = new string[EightBoards.Length];
            var serialSolver = new AStarSolver();
            var serialSafe = new SafeMoves();
            for (int b = 0; b < boards.Length; b++)
            {
                boards[b] = ReferenceBoards.Get(EightBoards[b]).State;
                expected[b] = Fingerprint(serialSolver, serialSafe, boards[b], null);
            }

            var flag = new SolverCancellation();
            var results = new string[Workers][];
            var errors = new Exception?[Workers];
            var threads = new Thread[Workers];
            using (var go = new ManualResetEventSlim(false))
            {
                for (int w = 0; w < Workers; w++)
                {
                    int worker = w;
                    results[worker] = new string[boards.Length];
                    threads[worker] = new Thread(() =>
                    {
                        try
                        {
                            var solver = new AStarSolver();
                            var safe = new SafeMoves();
                            go.Wait();
                            for (int k = 0; k < boards.Length; k++)
                            {
                                int b = (k + (2 * worker)) % boards.Length;
                                results[worker][b] = Fingerprint(solver, safe, boards[b], flag);
                            }
                        }
                        catch (Exception e)
                        {
                            errors[worker] = e;
                        }
                    });
                    threads[worker].IsBackground = true;
                    threads[worker].Start();
                }

                go.Set();
                for (int w = 0; w < Workers; w++)
                {
                    Assert.That(threads[w].Join(JoinTimeout), Is.True, "el hilo " + w + " no terminó");
                }
            }

            Assert.That(flag.IsRequested, Is.False);
            for (int w = 0; w < Workers; w++)
            {
                Assert.That(errors[w], Is.Null, "hilo " + w);
                for (int b = 0; b < boards.Length; b++)
                {
                    Assert.That(results[w][b], Is.EqualTo(expected[b]), "hilo " + w + ", tablero " + EightBoards[b]);
                }
            }
        }

        [TestCaseSource(nameof(Names))]
        public void NullCancellation_EveryReferenceBoard_GivesTheSameExpandedAndResults(string name)
        {
            // K2: con Cancellation null, el comportamiento de antes (par del tablero; expandidos dentro de lo fijado en
            // MechanicsPerformanceTests); con una bandera sin marcar, exactamente los mismos expandidos, estados, soluciones y
            // vertidos seguros: las consultas cada 256 expansiones no cambian la búsqueda.
            ReferenceBoard board = ReferenceBoards.Get(name);
            BoardState state = board.State;

            string plain = Fingerprint(new AStarSolver(), new SafeMoves(), state, null, out SolveResult pipeline);
            string watched = Fingerprint(new AStarSolver(), new SafeMoves(), state, new SolverCancellation());

            Assert.That(watched, Is.EqualTo(plain), name);
            Assert.That(pipeline.Status, Is.EqualTo(SolveStatus.Solved), name);
            Assert.That(pipeline.MoveCount, Is.EqualTo(board.Par), name);
            Assert.That(pipeline.Expanded, Is.LessThanOrEqualTo(board.ExpandedLimit), name);
            Assert.That(pipeline.WasCancelled, Is.False, name);
        }

        [Test]
        public void ACutMarkedFromAnotherThread_StopsTheSearch_AndTheSolverStaysReusable()
        {
            // 03 §5 y 08 §9: el hilo principal marca la bandera mientras otro hilo busca. Para que no dependa de los tiempos de la
            // máquina, el hilo de trabajo repite la búsqueda hasta ver el corte; el principal la marca en cuanto el otro empieza.
            ReferenceBoard board = ReferenceBoards.Get("Ma");
            BoardState state = board.State;
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run, state);
            var flag = new SolverCancellation();
            var solver = new AStarSolver();
            SolveResult last = default;
            int searches = 0;
            Exception? error = null;
            Thread worker;
            using (var started = new ManualResetEventSlim(false))
            {
                worker = new Thread(() =>
                {
                    try
                    {
                        started.Set();
                        do
                        {
                            last = solver.Solve(state, options.WithCancellation(flag));
                            searches++;
                        }
                        while (!last.WasCancelled && searches < 100000);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                });
                worker.IsBackground = true;
                worker.Start();
                Assert.That(started.Wait(JoinTimeout), Is.True);
                flag.Request();
                Assert.That(worker.Join(JoinTimeout), Is.True, "el hilo no vio el corte");
            }

            Assert.That(error, Is.Null);
            Assert.That(last.WasCancelled, Is.True);
            Assert.That(last.Status, Is.EqualTo(SolveStatus.BudgetExceeded));
            Assert.That(last.MoveCount, Is.EqualTo(0));
            Assert.That(last.Expanded % SolverCancellation.PollInterval, Is.EqualTo(0));
            Assert.That(last.Expanded, Is.LessThan(board.Expanded));

            // El mismo solver, ya en este hilo y con la bandera quitada, da la búsqueda entera.
            flag.Reset();
            SolveResult fresh = new AStarSolver().Solve(state, options);
            SolveResult again = solver.Solve(state, options.WithCancellation(flag));
            Assert.That(again.WasCancelled, Is.False);
            Assert.That(again.Expanded, Is.EqualTo(fresh.Expanded));
            Assert.That(again.CopyMoves(), Is.EqualTo(fresh.CopyMoves()));
            Assert.That(again.MoveCount, Is.EqualTo(board.Par));
        }

        [Test]
        public void ACutMarkedFromAnotherThread_StopsSafeMoves_WithTheFourthAnswer_AndTheMemoStaysClean()
        {
            // 02 T5 respuesta 4 (D-019) y 08 §9: SafeMoves en otro hilo (la Tinaja de referencia, con las opciones de Morpho),
            // cortado por el principal: sin vertidos, «No encuentro un camino desde aquí», sin cobrar. Lo que terminó antes del
            // corte se queda en el memo y vale; lo cortado, no.
            BoardState state = ReferenceBoards.Get("Ti").State;
            SolverOptions options = MorphoFor(state);
            var flag = new SolverCancellation();
            var safe = new SafeMoves();
            RealityStatus status = RealityStatus.Ok;
            int calls = 0;
            Exception? error = null;
            Thread worker;
            using (var started = new ManualResetEventSlim(false))
            {
                worker = new Thread(() =>
                {
                    try
                    {
                        started.Set();
                        do
                        {
                            safe.ClearMemo();
                            status = safe.Find(state, options.WithCancellation(flag));
                            calls++;
                        }
                        while (!safe.WasCancelled && calls < 100000);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                });
                worker.IsBackground = true;
                worker.Start();
                Assert.That(started.Wait(JoinTimeout), Is.True);
                flag.Request();
                Assert.That(worker.Join(JoinTimeout), Is.True, "el hilo no vio el corte");
            }

            Assert.That(error, Is.Null);
            Assert.That(status, Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.WasCancelled, Is.True);
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded));

            var fresh = new SafeMoves();
            fresh.Find(state, options);
            flag.Reset();
            safe.Find(state, options.WithCancellation(flag)); // con el memo de la llamada cortada
            Assert.That(safe.WasCancelled, Is.False);
            Assert.That(safe.Outcome, Is.EqualTo(fresh.Outcome));
            Assert.That(safe.Count, Is.EqualTo(fresh.Count));
            Assert.That(safe.MemoCount, Is.EqualTo(fresh.MemoCount));
            for (int i = 0; i < fresh.Count; i++)
            {
                Assert.That(safe.GetMove(i), Is.EqualTo(fresh.GetMove(i)), "vertido " + i);
            }
        }
    }
}
