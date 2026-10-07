#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Solver A* de GDD 03 §5 sobre tableros conocidos: el par coincide con el de <c>tools/levelgen_proto.py</c>
    /// (A* óptimo de referencia, mismo criterio de fútiles con capacidad 4) y la solución, rehecha con
    /// <see cref="PourRules"/>, gana el nivel. Incluye tableros de 14 cuias (02 R1). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class AStarSolverTests
    {
        /// <summary>
        /// Tableros de 14 cuias de capacidad 4 con su par según <c>levelgen_proto.astar</c> (w = 1), generados el
        /// 2026-10-06 con las semillas <c>amazonfill:test:{w14, d14, f14a, f14b, w14b}</c> (walk, deep, fill).
        /// </summary>
        private static readonly int[][][] FourteenCuiaBoards =
        {
            new[]
            {
                new int[0], new[] { 2, 6, 6, 10 }, new[] { 3, 3, 9, 2 }, new[] { 4, 4, 4, 1 }, new[] { 5, 5, 5, 1 },
                new[] { 6, 1, 8, 2 }, new[] { 7, 7, 7, 9 }, new[] { 8, 8, 8, 7 }, new[] { 5, 1, 10, 6 }, new int[0],
                new[] { 11, 11, 11, 9 }, new[] { 12, 12, 12, 11 }, new[] { 12, 9, 10, 2 }, new[] { 4, 3, 3, 10 },
            },
            new[]
            {
                new[] { 1, 1, 1, 3 }, new[] { 2, 11, 13, 2 }, new[] { 3, 1, 5, 6 }, new[] { 4, 4, 4, 12 }, new[] { 5, 6, 7, 8 },
                new[] { 6, 6, 8, 7 }, new[] { 7, 2, 13, 2 }, new[] { 8, 8, 11, 13 }, new[] { 9, 9, 9, 5 }, new[] { 10, 10, 10, 9 },
                new[] { 11, 11, 7, 10 }, new[] { 12, 12, 5, 13 }, new int[0], new[] { 3, 3, 12, 4 },
            },
            new[]
            {
                new[] { 9, 8, 5, 5 }, new[] { 6, 8, 6, 6 }, new[] { 4, 5, 12, 2 }, new[] { 1, 7, 10, 11 }, new[] { 12, 1, 12, 3 },
                new[] { 8, 2, 1, 3 }, new[] { 2, 3, 11, 5 }, new[] { 9, 11, 4, 4 }, new[] { 11, 8, 9, 12 }, new[] { 2, 3, 10, 7 },
                new[] { 9, 7, 1, 7 }, new[] { 10, 10, 6, 4 }, new int[0], new int[0],
            },
            new[]
            {
                new[] { 4, 3, 12, 1 }, new[] { 8, 3, 6, 12 }, new[] { 7, 9, 8, 2 }, new[] { 9, 9, 12, 8 }, new[] { 3, 4, 6, 7 },
                new[] { 5, 2, 5, 6 }, new[] { 10, 7, 5, 4 }, new[] { 6, 2, 7, 10 }, new[] { 2, 5, 1, 10 }, new[] { 4, 12, 11, 11 },
                new[] { 8, 9, 1, 10 }, new[] { 3, 11, 11, 1 }, new int[0], new int[0],
            },
            new[]
            {
                new[] { 1, 1, 1, 10 }, new int[0], new[] { 3, 3, 1, 9 }, new[] { 4, 4, 8, 6 }, new[] { 5, 5, 5, 7 },
                new int[0], new[] { 7, 7, 7, 8 }, new[] { 8, 2, 2, 6 }, new[] { 9, 9, 9, 6 }, new[] { 10, 10, 10, 2 },
                new[] { 11, 11, 11, 4 }, new[] { 12, 12, 4, 2 }, new[] { 11, 3, 3, 8 }, new[] { 12, 12, 5, 6 },
            },
        };

        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState FourCapBoard(int[][] cuias)
        {
            var built = new Cuia[cuias.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                built[i] = Cuia.Create(4, cuias[i]);
            }

            return new BoardState(built);
        }

        private static BoardState TutorialLevel1()
        {
            return new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });
        }

        /// <summary>Rehace la solución con <see cref="PourRules"/> y comprueba cantidades y victoria.</summary>
        private static void AssertSolutionWins(BoardState start, SolveResult result)
        {
            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                Assert.That(PourRules.Amount(state, pour.Source, pour.Target), Is.EqualTo(pour.Amount), "vertido " + i);
                state = PourRules.Apply(state, pour.Source, pour.Target);
            }

            Assert.That(state.IsWon(), Is.True);
        }

        [Test]
        public void TutorialLevel1_HasParThree()
        {
            // 02 §13: nivel 1, par 3.
            BoardState start = TutorialLevel1();

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.MoveCount, Is.EqualTo(3));
            AssertSolutionWins(start, result);
        }

        [TestCase(0, 25)]
        [TestCase(1, 28)]
        [TestCase(2, 37)]
        [TestCase(3, 37)]
        [TestCase(4, 20)]
        public void FourteenCuias_ParMatchesThePrototype(int board, int expectedPar)
        {
            BoardState start = FourCapBoard(FourteenCuiaBoards[board]);
            Assert.That(start.Count, Is.EqualTo(14));

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.MoveCount, Is.EqualTo(expectedPar));
            Assert.That(SolverHeuristic.Estimate(start), Is.LessThanOrEqualTo(expectedPar));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void Heuristic_IsConsistentAlongTheSolution()
        {
            // 03 §5: un vertido elimina a lo sumo un corte y saca un color de a lo sumo una cuia → H(S) ≤ 1 + H(S').
            BoardState state = FourCapBoard(FourteenCuiaBoards[2]);
            SolveResult result = new AStarSolver().Solve(state, SolverOptions.Pipeline);
            Assert.That(result.IsSolved, Is.True);

            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                BoardState next = PourRules.Apply(state, pour.Source, pour.Target);
                Assert.That(SolverHeuristic.Estimate(state), Is.LessThanOrEqualTo(1 + SolverHeuristic.Estimate(next)));
                state = next;
            }

            Assert.That(SolverHeuristic.Estimate(state), Is.EqualTo(0));
        }

        [Test]
        public void AlreadyWon_IsSolvedWithZeroMoves()
        {
            var start = new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.MoveCount, Is.EqualTo(0));
            Assert.That(result.Expanded, Is.EqualTo(0));
            Assert.That(result.CopyMoves(), Is.Empty);
        }

        [Test]
        public void NoLegalPours_IsUnsolvable()
        {
            var start = new BoardState(new[] { C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(result.Expanded, Is.EqualTo(1));
            Assert.That(result.MoveCount, Is.EqualTo(0));
        }

        [Test]
        public void SpaceExhausted_IsUnsolvable()
        {
            // 03 §5: agotar el espacio es la prueba de irresolubilidad (referencia: levelgen_proto, 4 expandidos).
            var start = new BoardState(new[] { C(4, 2, 1, 1, 2), C(4, 2, 1, 1, 2), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.That(result.Expanded, Is.GreaterThan(1));
        }

        [Test]
        public void TinyBudget_IsBudgetExceeded()
        {
            BoardState start = FourCapBoard(FourteenCuiaBoards[2]);

            SolveResult result = new AStarSolver().Solve(start, new SolverOptions(10, 1, true));

            Assert.That(result.Status, Is.EqualTo(SolveStatus.BudgetExceeded));
            Assert.That(result.Expanded, Is.EqualTo(11));
            Assert.That(result.MoveCount, Is.EqualTo(0));
        }

        [Test]
        public void PermutedBoard_HasTheSamePar_AndMovesUseRealIndices()
        {
            // 03 §3: el solver trabaja sobre el estado canónico, pero devuelve índices del tablero real.
            int[][] original = FourteenCuiaBoards[4];
            var reversed = new int[original.Length][];
            for (int i = 0; i < original.Length; i++)
            {
                reversed[i] = original[original.Length - 1 - i];
            }

            BoardState start = FourCapBoard(reversed);
            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.MoveCount, Is.EqualTo(20));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void SmallCuias_UseTheRightHomeBranch()
        {
            // 02 §1.4 y 04 §5.1: Cuia chica monocolor → estándar vacía no es fútil; sin esa rama no habría solución.
            var start = new BoardState(new[] { C(2, 1, 1), C(2, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.MoveCount, Is.EqualTo(2));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void SameCapacityMonocolorToEmpty_IsNeverExpanded()
        {
            // 04 §5.1 test de regresión (2) contra el solver, con el mismo estado que FutileRulesTests: [c, c] en una
            // estándar → otra estándar vacía es fútil (cap_A = cap_B = U_c). El solver no lo emite en ningún paso y el
            // par no cambia con el kill-switch apagado (R16).
            var start = new BoardState(new[] { C(4, 1, 1), C(4, 2, 2, 1, 1), Cuia.Empty(4), C(4, 2, 2) });
            Assert.That(PourRules.IsFutile(start, 0, 2), Is.True);

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);
            SolveResult classic = new AStarSolver().Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false));

            Assert.That(result.MoveCount, Is.EqualTo(2));
            Assert.That(classic.MoveCount, Is.EqualTo(2));
            AssertSolutionWins(start, result);
            BoardState state = start;
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour pour = result.GetMove(i);
                Assert.That(PourRules.IsFutile(state, pour.Source, pour.Target), Is.False, "vertido " + i);
                state = PourRules.Apply(state, pour.Source, pour.Target);
            }
        }

        [Test]
        public void HiddenMonocolorToEmpty_IsExpanded()
        {
            // 04 §5.1 test de regresión (3) contra el solver, con el mismo estado que FutileRulesTests: la cuia llena de c
            // con una dosis oculta no sella; su único vertido es "monocolor → vacía", que no es fútil porque revela la
            // dosis (R7). Sin la excepción el solver devolvería Unsolvable. LevelSession rechaza las ocultas en v0
            // (contrato a de docs/dev/prototipo-v0.md), así que esta regresión solo puede correr aquí y en las reglas.
            var start = new BoardState(new[] { Cuia.Create(4, new[] { 1, 1, 1, 1 }, 0x1), C(4, 2, 2, 2, 2), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.GetMove(0), Is.EqualTo(new Pour(0, 2, 3)));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void TallCuia_SealsOnlyWithItsAbundantFruit()
        {
            // 02 §8: Cuia alta de 5 con fruta abundante (U_c = 5).
            var start = new BoardState(new[] { C(5, 1, 1, 1, 1, 2), C(4, 2, 2, 2, 1), Cuia.Empty(4) });

            SolveResult result = new AStarSolver().Solve(start, SolverOptions.Pipeline);

            Assert.That(result.MoveCount, Is.EqualTo(3));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void KillSwitchOff_KeepsTheParOnUniformCapacities()
        {
            // R16: con capacidades iguales un fútil no cambia el estado canónico, así que el par no cambia.
            BoardState start = FourCapBoard(FourteenCuiaBoards[4]);

            SolveResult result = new AStarSolver().Solve(start, new SolverOptions(SolverOptions.PipelineNodeBudget, 1, false));

            Assert.That(result.MoveCount, Is.EqualTo(20));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void WeightTwo_FindsAValidSolution_NeverShorterThanPar()
        {
            // 03 §5: w = 2 es la pista en cliente (subóptima).
            BoardState start = FourCapBoard(FourteenCuiaBoards[2]);

            SolveResult result = new AStarSolver().Solve(start, new SolverOptions(40000, 2, true));

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.MoveCount, Is.GreaterThanOrEqualTo(37));
            AssertSolutionWins(start, result);
        }

        [Test]
        public void ReusedSolver_IsDeterministic()
        {
            var solver = new AStarSolver(16);
            BoardState first = FourCapBoard(FourteenCuiaBoards[3]);
            BoardState other = TutorialLevel1();

            SolveResult a = solver.Solve(first, SolverOptions.Pipeline);
            solver.Solve(other, SolverOptions.Pipeline);
            SolveResult b = solver.Solve(first, SolverOptions.Pipeline);
            SolveResult c = new AStarSolver().Solve(first, SolverOptions.Pipeline);

            Assert.That(b.CopyMoves(), Is.EqualTo(a.CopyMoves()));
            Assert.That(c.CopyMoves(), Is.EqualTo(a.CopyMoves()));
            Assert.That(b.Expanded, Is.EqualTo(a.Expanded));
            Assert.That(b.StoredStates, Is.EqualTo(a.StoredStates));
        }

        [Test]
        public void ReusedSolver_WiderBoardAfterNarrow_MatchesFreshSolver()
        {
            // Los nodos guardan T cuias (no 16): al pasar de un tablero de 3 a uno de 14 el solver redimensiona y da lo
            // mismo que uno nuevo.
            var solver = new AStarSolver(8);
            solver.Solve(TutorialLevel1(), SolverOptions.Pipeline);
            BoardState wide = FourCapBoard(FourteenCuiaBoards[0]);

            SolveResult reused = solver.Solve(wide, SolverOptions.Pipeline);
            SolveResult fresh = new AStarSolver(8).Solve(wide, SolverOptions.Pipeline);

            Assert.That(reused.CopyMoves(), Is.EqualTo(fresh.CopyMoves()));
            Assert.That(reused.Expanded, Is.EqualTo(fresh.Expanded));
            AssertSolutionWins(wide, reused);
        }

        [Test]
        public void DefaultOptions_AreRejected()
        {
            Assert.That(() => { new AStarSolver().Solve(TutorialLevel1(), default(SolverOptions)); }, Throws.TypeOf<ArgumentException>());
            Assert.That(() => { new SolverOptions(0, 1, true); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { new SolverOptions(10, 0, true); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GetMove_OutOfRange_Throws()
        {
            SolveResult result = new AStarSolver().Solve(TutorialLevel1(), SolverOptions.Pipeline);

            Assert.That(() => { result.GetMove(3); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
