#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Corte cooperativo por tiempo del solver (K2): 08 §9 <c>hint_timeout_ms</c> («al agotarse no se cobra»), 03 §5 (el
    /// cliente busca en un hilo y cancela por tiempo), 04 §2.3 y 02 T5 (respuesta 4 de D-019: «No encuentro un camino desde
    /// aquí», sin cobrar). El núcleo no lee relojes: la bandera la marca otro hilo y aquí se simula con
    /// <see cref="SolverCancellation.RequestAtPoll"/> (marcada justo antes de la consulta k del solver, que llega con
    /// (k − 1) · 256 expansiones). Marcada antes, marcada tras N expansiones y null idéntico, en <see cref="AStarSolver"/> y en
    /// <see cref="SafeMoves"/>. Los hilos de verdad están en <c>DotnetOnly/SolverThreadingTests</c>. Test compartido (NUnit 3.5,
    /// C# 9).
    /// </summary>
    [TestFixture]
    public sealed class SolverCancellationTests
    {
        /// <summary>
        /// Tablero de 14 cuias de capacidad 4 (<c>AStarSolverTests.FourteenCuiaBoards[2]</c>, par 37): unos 16.000 expandidos
        /// en Run con el presupuesto del pipeline, de sobra para cortar a mitad.
        /// </summary>
        private static readonly int[][] FillBoard =
        {
            new[] { 9, 8, 5, 5 }, new[] { 6, 8, 6, 6 }, new[] { 4, 5, 12, 2 }, new[] { 1, 7, 10, 11 }, new[] { 12, 1, 12, 3 },
            new[] { 8, 2, 1, 3 }, new[] { 2, 3, 11, 5 }, new[] { 9, 11, 4, 4 }, new[] { 11, 8, 9, 12 }, new[] { 2, 3, 10, 7 },
            new[] { 9, 7, 1, 7 }, new[] { 10, 10, 6, 4 }, new int[0], new int[0],
        };

        /// <summary>Morpho: <c>hint_budget_nodes</c> = 40.000 y <c>hint_weight</c> = 2 (03 §5; 08 §9).</summary>
        private static readonly SolverOptions Morpho = new SolverOptions(40000, 2, true, PourMode.Run);

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia H(int capacity, int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop, hiddenMask);
        }

        private static BoardState Fill()
        {
            var cuias = new Cuia[FillBoard.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = Cuia.Create(4, FillBoard[i]);
            }

            return new BoardState(cuias);
        }

        private static BoardState TutorialLevel1()
        {
            return new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });
        }

        /// <summary>Aguas turbias con vertidos seguros (<c>MurkyTests.SafeBoard</c>): dos realidades, respuesta 1.</summary>
        private static BoardState SafeBoard()
        {
            return new BoardState(new[] { H(2, 0x1, 1, 3), H(2, 0x1, 2, 3), C(2, 1, 2), Cuia.Empty(2) });
        }

        /// <summary>Aguas turbias sin vertido seguro (<c>MurkyTests.NoSafeBoard</c>): explica por qué con más búsquedas.</summary>
        private static BoardState NoSafeBoard()
        {
            return new BoardState(new[] { H(3, 0x2, 3, 3, 1), H(3, 0x1, 2, 1), C(3, 3, 2, 2), C(3, 1) });
        }

        /// <summary>Primera consulta del solver que llega con N expansiones hechas o más: la k con (k − 1) · 256 ≥ N.</summary>
        private static int FirstPollAtOrAfter(int expansions)
        {
            int interval = SolverCancellation.PollInterval;
            return ((expansions + interval - 1) / interval) + 1;
        }

        private static void AssertSameSearch(SolveResult expected, SolveResult actual, string label)
        {
            Assert.That(actual.Status, Is.EqualTo(expected.Status), label);
            Assert.That(actual.Expanded, Is.EqualTo(expected.Expanded), label);
            Assert.That(actual.StoredStates, Is.EqualTo(expected.StoredStates), label);
            Assert.That(actual.CopyMoves(), Is.EqualTo(expected.CopyMoves()), label);
            Assert.That(actual.WasCancelled, Is.False, label);
        }

        private static Pour[] SafePours(SafeMoves safe)
        {
            var pours = new Pour[safe.Count];
            for (int i = 0; i < pours.Length; i++)
            {
                pours[i] = safe.GetMove(i);
            }

            return pours;
        }

        [Test]
        public void TheFlag_StartsClear_RequestMarksIt_AndResetClearsIt()
        {
            var flag = new SolverCancellation();
            Assert.That(flag.IsRequested, Is.False);

            flag.Request();
            flag.Request(); // idempotente
            Assert.That(flag.IsRequested, Is.True);

            flag.Reset();
            Assert.That(flag.IsRequested, Is.False);
            Assert.That(SolverCancellation.PollInterval, Is.EqualTo(256)); // [P] de K2
        }

        [Test]
        public void Options_WithoutCancellation_ByDefault_AndForLevelAndWithFutileRuleKeepIt()
        {
            // K2: null por defecto (el pipeline la deja así); las opciones derivadas no la pierden.
            var flag = new SolverCancellation();
            SolverOptions options = Morpho.WithCancellation(flag);

            Assert.That(Morpho.Cancellation, Is.Null);
            Assert.That(SolverOptions.ForPipeline(PourMode.Run).Cancellation, Is.Null);
            Assert.That(SafeMoves.ForPipeline(PourMode.Run).Cancellation, Is.Null);
            Assert.That(options.Cancellation, Is.SameAs(flag));
            Assert.That(options.NodeBudget, Is.EqualTo(Morpho.NodeBudget));
            Assert.That(options.Weight, Is.EqualTo(Morpho.Weight));
            Assert.That(options.FutileRuleEnabled, Is.EqualTo(Morpho.FutileRuleEnabled));
            Assert.That(options.PourMode, Is.EqualTo(Morpho.PourMode));
            Assert.That(options.WithFutileRule(false).Cancellation, Is.SameAs(flag));
            Assert.That(options.ForLevel(Fill()).Cancellation, Is.SameAs(flag));
            Assert.That(options.WithCancellation(null).Cancellation, Is.Null);
        }

        [Test]
        public void Solve_FlagMarkedBefore_IsBudgetExceededAndCancelled_WithoutExpandingAnything()
        {
            // 08 §9: al agotarse hint_timeout_ms no se cobra; 02 T5 respuesta 4. Marcada antes: como mucho 256 expansiones
            // (en la práctica, ninguna: la primera consulta va antes de expandir).
            var flag = new SolverCancellation();
            flag.Request();

            SolveResult result = new AStarSolver().Solve(Fill(), SolverOptions.ForPipeline(PourMode.Run).WithCancellation(flag));

            Assert.That(result.Status, Is.EqualTo(SolveStatus.BudgetExceeded));
            Assert.That(result.WasCancelled, Is.True);
            Assert.That(result.IsSolved, Is.False);
            Assert.That(result.MoveCount, Is.EqualTo(0));
            Assert.That(result.Expanded, Is.EqualTo(0));
            Assert.That(result.Expanded, Is.LessThanOrEqualTo(SolverCancellation.PollInterval));
        }

        [TestCase(1)]
        [TestCase(255)]
        [TestCase(256)]
        [TestCase(257)]
        [TestCase(1000)]
        [TestCase(5000)]
        public void Solve_FlagMarkedAfterNExpansions_StopsWithin256More(int expansions)
        {
            // K2: la bandera se consulta cada 256 expansiones [P]; marcada tras N, la búsqueda para como mucho 256 después.
            BoardState start = Fill();
            SolveResult full = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            Assert.That(full.Expanded, Is.GreaterThan(expansions + SolverCancellation.PollInterval), "el tablero no da para cortar");

            var flag = new SolverCancellation();
            flag.RequestAtPoll(FirstPollAtOrAfter(expansions));
            SolveResult result = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run).WithCancellation(flag));

            Assert.That(flag.IsRequested, Is.True);
            Assert.That(result.Status, Is.EqualTo(SolveStatus.BudgetExceeded));
            Assert.That(result.WasCancelled, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(0));
            Assert.That(result.Expanded, Is.GreaterThanOrEqualTo(expansions));
            Assert.That(result.Expanded, Is.LessThanOrEqualTo(expansions + SolverCancellation.PollInterval));
            Assert.That(result.Expanded % SolverCancellation.PollInterval, Is.EqualTo(0));
        }

        [Test]
        public void Solve_FlagMarkedAfterTheSearchDecided_ReturnsTheSolution_NotCancelled()
        {
            // El nivel 1 se resuelve antes de la segunda consulta (expansión 257): el corte llega tarde y no cambia nada.
            BoardState start = TutorialLevel1();
            SolveResult plain = new AStarSolver().Solve(start, SolverOptions.Pipeline);
            Assert.That(plain.Expanded, Is.LessThan(SolverCancellation.PollInterval));

            var flag = new SolverCancellation();
            flag.RequestAtPoll(2);
            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline.WithCancellation(flag));

            Assert.That(result.MoveCount, Is.EqualTo(3));
            AssertSameSearch(plain, result, "nivel 1");
        }

        [TestCase(PourMode.Run, 1)]
        [TestCase(PourMode.Single, 1)]
        [TestCase(PourMode.Run, 2)]
        [TestCase(PourMode.Single, 2)]
        public void Solve_NullCancellation_AndAnUnmarkedFlag_GiveTheSameSearch(PourMode mode, int weight)
        {
            // K2: con Cancellation null (o una bandera sin marcar), los mismos expandidos, estados y solución que antes.
            var flag = new SolverCancellation();
            var options = new SolverOptions(SolverOptions.PipelineNodeBudget, weight, true, mode);
            BoardState[] boards = { Fill(), TutorialLevel1(), SafeBoard(), NoSafeBoard() };
            var reused = new AStarSolver();
            for (int b = 0; b < boards.Length; b++)
            {
                SolveResult plain = new AStarSolver().Solve(boards[b], options);
                SolveResult watched = reused.Solve(boards[b], options.WithCancellation(flag));

                Assert.That(plain.WasCancelled, Is.False);
                AssertSameSearch(plain, watched, "tablero " + b);
            }
        }

        [Test]
        public void Solve_AfterACut_ResetAndTheSameSolver_GiveTheFullSearch()
        {
            BoardState start = Fill();
            SolveResult fresh = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            var flag = new SolverCancellation();
            var solver = new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run).WithCancellation(flag);
            flag.RequestAtPoll(5);
            Assert.That(solver.Solve(start, options).WasCancelled, Is.True);

            flag.Reset();
            SolveResult again = solver.Solve(start, options);

            Assert.That(again.MoveCount, Is.EqualTo(37));
            AssertSameSearch(fresh, again, "tras Reset");
        }

        [Test]
        public void SafeMoves_FlagMarkedBefore_IsBudgetExceededAndCancelled_WithoutSearching()
        {
            // 02 T5, respuesta 4 (D-019): «No encuentro un camino desde aquí» y no cobra. Las realidades sí se enumeran.
            var flag = new SolverCancellation();
            flag.Request();
            var safe = new SafeMoves();

            RealityStatus status = safe.Find(SafeBoard(), SafeMoves.ForPipeline(PourMode.Run).WithCancellation(flag));

            Assert.That(status, Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(2));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded));
            Assert.That(safe.WasCancelled, Is.True);
            Assert.That(safe.Searches, Is.EqualTo(0));
            Assert.That(safe.Expanded, Is.EqualTo(0));
            Assert.That(safe.MemoCount, Is.EqualTo(0));
        }

        [Test]
        public void SafeMoves_RejectedRealities_WinOverTheCut()
        {
            // Con la enumeración rechazada (03 §8: más de 24 realidades) no hay búsqueda que cortar: RealitiesRejected, como
            // sin bandera.
            var flag = new SolverCancellation();
            flag.Request();
            var safe = new SafeMoves();
            var tooMany = new BoardState(new[] { H(4, 0x7, 1, 2, 3, 9), H(4, 0x3, 4, 5, 10), Cuia.Empty(4) });

            RealityStatus status = safe.Find(tooMany, SafeMoves.ForPipeline(PourMode.Run).WithCancellation(flag));

            Assert.That(status, Is.EqualTo(RealityStatus.TooMany));
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.RealitiesRejected));
            Assert.That(safe.WasCancelled, Is.False);
        }

        [Test]
        public void SafeMoves_FlagMarkedInTheSecondSearch_StopsThere_AndTheCutDoesNotEnterTheMemo()
        {
            // K2: el corte en la búsqueda A* en curso para todo (sin Pista parcial) y lo cortado no entra en el memo; lo
            // terminado antes, sí. Tras Reset, la misma instancia da lo mismo que una nueva sin bandera.
            BoardState board = SafeBoard();
            SolverOptions options = SafeMoves.ForPipeline(PourMode.Run);
            var fresh = new SafeMoves();
            fresh.Find(board, options);
            Assert.That(fresh.Searches, Is.GreaterThan(2));
            Assert.That(fresh.Outcome, Is.EqualTo(SafeMovesOutcome.Found));

            var flag = new SolverCancellation();
            var safe = new SafeMoves();
            flag.RequestAtPoll(2); // cada búsqueda consulta antes de su primera expansión: la 2.ª se corta al empezar
            RealityStatus status = safe.Find(board, options.WithCancellation(flag));

            Assert.That(status, Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.WasCancelled, Is.True);
            Assert.That(safe.Count, Is.EqualTo(0));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded));
            Assert.That(safe.Searches, Is.EqualTo(2));
            Assert.That(safe.MemoCount, Is.EqualTo(1));

            flag.Reset();
            safe.Find(board, options.WithCancellation(flag));

            Assert.That(safe.WasCancelled, Is.False);
            Assert.That(safe.Outcome, Is.EqualTo(fresh.Outcome));
            Assert.That(SafePours(safe), Is.EqualTo(SafePours(fresh)));
            Assert.That(safe.MemoCount, Is.EqualTo(fresh.MemoCount));
            Assert.That(safe.Searches, Is.EqualTo(fresh.Searches + 1)); // la cortada se repite; la primera sale del memo
        }

        [Test]
        public void SafeMoves_ACutAtAnyPoll_IsAlwaysTheFourthAnswer_AndNeverPoisonsTheMemo()
        {
            // 02 T5 (orden de respuestas de D-019): un corte en cualquier consulta (vertidos candidatos o la explicación de por
            // qué no hay seguro) da siempre la respuesta 4 sin vertidos, y tras Reset la misma instancia responde como una nueva.
            BoardState[] boards = { SafeBoard(), NoSafeBoard() };
            SolverOptions options = SafeMoves.ForPipeline(PourMode.Run);
            for (int b = 0; b < boards.Length; b++)
            {
                var fresh = new SafeMoves();
                fresh.Find(boards[b], options);
                int cuts = 0;
                bool finished = false;
                for (int poll = 1; poll <= 10000 && !finished; poll++)
                {
                    var flag = new SolverCancellation();
                    var safe = new SafeMoves();
                    flag.RequestAtPoll(poll);
                    safe.Find(boards[b], options.WithCancellation(flag));
                    if (!safe.WasCancelled)
                    {
                        // Más consultas que las que hace la llamada entera: el resultado completo.
                        finished = true;
                        Assert.That(safe.Outcome, Is.EqualTo(fresh.Outcome), "tablero " + b);
                        Assert.That(SafePours(safe), Is.EqualTo(SafePours(fresh)), "tablero " + b);
                        continue;
                    }

                    cuts++;
                    string label = "tablero " + b + ", consulta " + poll;
                    Assert.That(safe.Count, Is.EqualTo(0), label);
                    Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded), label);

                    flag.Reset();
                    safe.Find(boards[b], options.WithCancellation(flag));
                    Assert.That(safe.WasCancelled, Is.False, label);
                    Assert.That(safe.Outcome, Is.EqualTo(fresh.Outcome), label);
                    Assert.That(SafePours(safe), Is.EqualTo(SafePours(fresh)), label);
                    Assert.That(safe.MemoCount, Is.EqualTo(fresh.MemoCount), label);
                }

                Assert.That(finished, Is.True, "tablero " + b);
                Assert.That(cuts, Is.GreaterThanOrEqualTo(fresh.Searches), "tablero " + b);
            }
        }

        [Test]
        public void SafeMoves_TrueParAfter_ACut_IsBudgetExceededAndCancelled_KeepsTheFindResult_AndTheNextCallClearsIt()
        {
            // K2: TrueParAfter usa la bandera de la última Find. Cortado, da BudgetExceeded sin guardarlo en el memo y
            // WasCancelled solo para esa llamada (par no demostrado); Count, Outcome y los vertidos siguen siendo los de Find
            // (la respuesta 4 de 02 T5 se lee solo tras Find). La llamada siguiente lo reinicia y da el par de una instancia nueva.
            BoardState board = SafeBoard();
            SolverOptions options = SafeMoves.ForPipeline(PourMode.Run);
            var fresh = new SafeMoves();
            fresh.Find(board, options);
            int expectedPar = fresh.TrueParAfter(0);
            Assert.That(expectedPar, Is.GreaterThan(0)); // seguro y sin ser victoria: hace falta una búsqueda
            Assert.That(fresh.WasCancelled, Is.False);

            var flag = new SolverCancellation();
            var safe = new SafeMoves();
            safe.Find(board, options.WithCancellation(flag)); // sin marcar: el resultado completo
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(safe.WasCancelled, Is.False);
            Pour[] pours = SafePours(safe);
            Assert.That(pours, Is.EqualTo(SafePours(fresh)));

            safe.ClearMemo(); // el par tras el vertido 0 ya no sale del memo
            flag.Request();
            int cut = safe.TrueParAfter(0);

            Assert.That(cut, Is.EqualTo(SafeMoves.BudgetExceeded));
            Assert.That(safe.WasCancelled, Is.True);
            Assert.That(safe.MemoCount, Is.EqualTo(0));
            Assert.That(safe.Searches, Is.EqualTo(1));
            Assert.That(safe.Expanded, Is.EqualTo(0)); // marcada antes: corta antes de la expansión 1
            Assert.That(safe.Count, Is.EqualTo(pours.Length));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(SafePours(safe), Is.EqualTo(pours));

            flag.Reset();
            int again = safe.TrueParAfter(0);

            Assert.That(again, Is.EqualTo(expectedPar));
            Assert.That(safe.WasCancelled, Is.False);
            Assert.That(safe.Searches, Is.EqualTo(2)); // la cortada se repite
            Assert.That(safe.MemoCount, Is.EqualTo(1));
            Assert.That(safe.Count, Is.EqualTo(pours.Length));
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.Found));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void SafeMoves_NullCancellation_AndAnUnmarkedFlag_GiveTheSameAnswer(int weight)
        {
            // K2: con Cancellation null (o una bandera sin marcar), los mismos vertidos seguros, la misma respuesta y las mismas
            // búsquedas. El memo es el mismo con y sin bandera (no cuenta como opción).
            var flag = new SolverCancellation();
            var options = new SolverOptions(SafeMoves.PipelineRealityBudget, weight, true, PourMode.Run);
            BoardState[] boards = { SafeBoard(), NoSafeBoard() };
            for (int b = 0; b < boards.Length; b++)
            {
                var plain = new SafeMoves();
                var watched = new SafeMoves();
                plain.Find(boards[b], options);
                watched.Find(boards[b], options.WithCancellation(flag));
                int searches = watched.Searches;
                watched.Find(boards[b], options); // sin bandera: todo sale del memo

                string label = "tablero " + b;
                Assert.That(watched.WasCancelled, Is.False, label);
                Assert.That(watched.Outcome, Is.EqualTo(plain.Outcome), label);
                Assert.That(SafePours(watched), Is.EqualTo(SafePours(plain)), label);
                Assert.That(searches, Is.EqualTo(plain.Searches), label);
                Assert.That(watched.Searches, Is.EqualTo(searches), label);
                Assert.That(watched.Expanded, Is.EqualTo(plain.Expanded), label);
                Assert.That(watched.MemoCount, Is.EqualTo(plain.MemoCount), label);
            }
        }
    }
}
