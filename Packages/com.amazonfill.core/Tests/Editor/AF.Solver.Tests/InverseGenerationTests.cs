#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Generation;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Barajado inverso <c>WALK</c> y <c>DEEP</c> (03 §4.1-§4.2): movimientos inversos exactos, garantía por construcción
    /// (A* resuelve lo que devuelven), filtros (sin sellos, E vacíos, complejidad ≥ compl_min) y determinismo por semilla.
    /// Test compartido (NUnit 3.5, C# 9); los lotes grandes están en DotnetOnly/InverseGenerationPropertyTests.
    /// </summary>
    [TestFixture]
    public sealed class InverseGenerationTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        [Test]
        public void InverseMoves_FromTheSolvedBoard_AreTheExactInversesOfUsefulPours()
        {
            // 03 §4.1: desde [1,1,1,1] [2,2,2,2] [] solo se puede sacar 1, 2 o 3 dosis de un color a la vacía (sacar las 4
            // sería el inverso de un vertido fútil: monocolor entera a una vacía), ni ponerla sobre el otro color lleno.
            var cuias = new[] { C(4, 1, 1, 1, 1), C(4, 2, 2, 2, 2), Cuia.Empty(4) };
            int[] units = new BoardState(cuias).UnitsTable;
            var buffer = new InverseMove[InverseMoves.MaxMoves];

            int count = InverseMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Run, buffer, new Cuia[2]);

            Assert.That(count, Is.EqualTo(6));
            for (int m = 0; m < count; m++)
            {
                InverseMove move = buffer[m];
                Assert.That(move.Target, Is.EqualTo(2));
                Assert.That(move.Amount, Is.InRange(1, 3));
                var after = (Cuia[])cuias.Clone();
                InverseMoves.Apply(after, 0, move);
                Assert.That(BoardRules.IsLegal(after, 0, after.Length, units, move.Target, move.Source), Is.True);
                Assert.That(BoardRules.Amount(after, 0, after.Length, units, move.Target, move.Source, PourMode.Run), Is.EqualTo(move.Amount));
                Assert.That(BoardRules.IsFutile(after, 0, after.Length, units, move.Target, move.Source), Is.False);
            }
        }

        [Test]
        public void InverseMoves_InSingle_MoveOneDose()
        {
            var cuias = new[] { C(4, 1, 1, 1, 1), C(4, 2, 2, 2, 2), Cuia.Empty(4) };
            int[] units = new BoardState(cuias).UnitsTable;
            var buffer = new InverseMove[InverseMoves.MaxMoves];

            int count = InverseMoves.Collect(cuias, 0, cuias.Length, units, PourMode.Single, buffer, new Cuia[2]);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer[0].Amount, Is.EqualTo(1));
        }

        [TestCase(5, 2)]
        [TestCase(7, 1)]
        public void Walk_ReturnsAValidSolvableBoard_AndIsDeterministic(int colors, int empties)
        {
            InverseSpec spec = InverseSpec.Uniform(colors, empties);
            ulong seed = AF.Core.Determinism.Prng.LevelSeed(2, 1, 21);

            InverseResult first = new InverseWalk().Generate(spec, seed);
            InverseResult second = new InverseWalk().Generate(spec, seed);

            AssertValid(first, spec);
            Assert.That(second.State!.Equals(first.State), Is.True);
            Assert.That(second.Attempts, Is.EqualTo(first.Attempts));
        }

        [Test]
        public void Deep_ReturnsAValidSolvableBoard_AndIsDeterministic()
        {
            InverseSpec spec = InverseSpec.Uniform(6, 1);
            ulong seed = AF.Core.Determinism.Prng.LevelSeed(3, 1, 46);

            InverseResult first = new InverseDeep().Generate(spec, seed, 3000);
            InverseResult second = new InverseDeep().Generate(spec, seed, 3000);

            AssertValid(first, spec);
            Assert.That(second.State!.Equals(first.State), Is.True);
            Assert.That(first.Depth, Is.GreaterThan(0));
        }

        [Test]
        public void MixedCapacities_TallCuiaAndSmallCuia_AreHonoured()
        {
            // 02 §8: Cuia alta de 5 con su fruta abundante (U = 5) y Cuia chica de 2 como búfer.
            var spec = new InverseSpec(new[] { 5, 4, 4, 4, 2, 4 }, new[] { 5, 4, 4, 4 }, 2);

            InverseResult result = new InverseWalk().Generate(spec, 99UL);

            Assert.That(spec.Solved[0], Is.EqualTo(C(5, 1, 1, 1, 1, 1)));
            AssertValid(result, spec);
            Assert.That(result.State!.UnitsOf(1), Is.EqualTo(5));
        }

        [Test]
        public void SingleMode_WalkIsSolvableInSingle()
        {
            InverseSpec spec = InverseSpec.Uniform(5, 2, PourMode.Single);

            InverseResult result = new InverseWalk().Generate(spec, 5UL);

            AssertValid(result, spec);
        }

        [Test]
        public void Complexity_IsLaicwben2()
        {
            // 2·transiciones + 2·mezcladas + parciales: [1,2,2] (1 transición, mezclada, parcial), [3,3,3,3], [] → 2+2+1.
            var cuias = new[] { C(4, 1, 2, 2), C(4, 3, 3, 3, 3), Cuia.Empty(4) };

            Assert.That(Complexity.Of(cuias, 0, cuias.Length), Is.EqualTo(5));
        }

        private static void AssertValid(InverseResult result, InverseSpec spec)
        {
            Assert.That(result.Succeeded, Is.True);
            BoardState state = result.State!;
            int empties = 0;
            for (int i = 0; i < state.Count; i++)
            {
                Assert.That(state.IsSealed(i), Is.False, "cuia " + i + " sellada");
                empties += state[i].IsEmpty ? 1 : 0;
            }

            Assert.That(empties, Is.EqualTo(spec.Empties));
            Assert.That(Complexity.Of(state), Is.GreaterThanOrEqualTo(spec.MinComplexity));
            Assert.That(result.Complexity, Is.EqualTo(Complexity.Of(state)));
            SolveResult solved = new AStarSolver().Solve(state, SolverOptions.ForPipeline(spec.Mode));
            Assert.That(solved.IsSolved, Is.True, "A* no resuelve " + state);
        }
    }
}
