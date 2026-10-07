#nullable enable
using AF.Core.Board;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>Heurística <c>H = max(cortes, dispersión)</c> de GDD 03 §5. Test compartido (NUnit 3.5, C# 9).</summary>
    [TestFixture]
    public sealed class SolverHeuristicTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        [Test]
        public void ColorBreaks_CountsSegmentsMinusOne()
        {
            Assert.That(SolverHeuristic.ColorBreaks(Cuia.Empty(4)), Is.EqualTo(0));
            Assert.That(SolverHeuristic.ColorBreaks(C(4, 1, 1, 1, 1)), Is.EqualTo(0));
            Assert.That(SolverHeuristic.ColorBreaks(C(4, 1, 1, 2, 2)), Is.EqualTo(1));
            Assert.That(SolverHeuristic.ColorBreaks(C(4, 1, 2, 1, 2)), Is.EqualTo(3));
        }

        [Test]
        public void ColorBreaks_LooksAtTheRealColorOfHiddenDoses()
        {
            Assert.That(SolverHeuristic.ColorBreaks(Cuia.Create(4, new[] { 1, 1, 1 }, 0x1)), Is.EqualTo(0));
        }

        [Test]
        public void ColorMask_HasOneBitPerColor()
        {
            Assert.That(SolverHeuristic.ColorMask(Cuia.Empty(4)), Is.EqualTo(0));
            Assert.That(SolverHeuristic.ColorMask(C(4, 1, 3, 3)), Is.EqualTo(0x5));
            Assert.That(SolverHeuristic.ColorMask(C(4, 16)), Is.EqualTo(0x8000));
        }

        [Test]
        public void Estimate_IsTheMaxOfBreaksAndDispersion()
        {
            // Nivel 1 de 02 §13: h1 = 1 + 1, h2 = (2 − 1) + (2 − 1).
            var tutorial = new BoardState(new[] { C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4) });
            // h1 = 0 (todas monocolor), h2 = 3 − 1 = 2: la dispersión domina.
            var dispersed = new BoardState(new[] { C(4, 1), C(4, 1), C(4, 1, 1), Cuia.Empty(4) });
            // h1 = 3 + 3 = 6, h2 = 1 + 1 = 2: los cortes dominan.
            var broken = new BoardState(new[] { C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1), Cuia.Empty(4) });

            Assert.That(SolverHeuristic.Estimate(tutorial), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(dispersed), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(broken), Is.EqualTo(6));
        }

        [Test]
        public void Estimate_OfAWonBoard_IsZero()
        {
            var won = new BoardState(new[] { C(4, 1, 1, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2) });

            Assert.That(SolverHeuristic.Estimate(won), Is.EqualTo(0));
        }

        [Test]
        public void Estimate_OverAnArraySlice_MatchesTheBoard()
        {
            var cuias = new[] { C(4, 9), C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4), C(4, 9) };

            Assert.That(SolverHeuristic.Estimate(cuias, 1, 3), Is.EqualTo(2));
            Assert.That(SolverHeuristic.Estimate(cuias, 0, 5), Is.EqualTo(3));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(0xFFFF, 16)]
        [TestCase(-1, 32)]
        public void PopCount_CountsSetBits(int value, int expected)
        {
            Assert.That(SolverHeuristic.PopCount(value), Is.EqualTo(expected));
        }
    }
}
