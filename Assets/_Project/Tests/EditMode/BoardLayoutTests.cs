#nullable enable
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>Layout del tablero (GDD 02 §5; 07 §4.1).</summary>
    public sealed class BoardLayoutTests
    {
        private const float Top = 88f;
        private const float Bottom = 620f;

        [TestCase(3, 1, 3)]
        [TestCase(5, 1, 5)]
        [TestCase(6, 2, 3)]
        [TestCase(7, 2, 4)]
        [TestCase(14, 2, 7)]
        [TestCase(16, 3, 6)]
        public void Rows_FollowTheSplitOf02_5(int count, int rows, int firstRow)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, 6f, 360f, Top, Bottom);
            Assert.That(r.Rows, Is.EqualTo(rows));
            Assert.That(r.PerRow[0], Is.EqualTo(firstRow));
            int sum = 0;
            for (int i = 0; i < r.Rows; i++)
            {
                sum += r.PerRow[i];
            }

            Assert.That(sum, Is.EqualTo(count));
        }

        [Test]
        public void SevenPerRow_GivesSlot48AndCuia34By102()
        {
            // 02 §5: con 7 por fila, ranura 48, cuia 34 × 102 (0,70 × 48 = 33,6).
            BoardLayoutResult r = BoardLayout.Compute(14, 6f, 360f, Top, Bottom);
            Assert.That(r.SlotWidth, Is.EqualTo(48f).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.EqualTo(33.6f).Within(0.01f));
            Assert.That(r.CuiaHeight, Is.EqualTo(100.8f).Within(0.01f));
        }

        [Test]
        public void FewCuias_AreCappedAt48dp_AndRowsAreCentred()
        {
            BoardLayoutResult r = BoardLayout.Compute(3, 6f, 360f, Top, Bottom);
            Assert.That(r.CuiaWidth, Is.EqualTo(48f));
            Assert.That(r.CuiaHeight, Is.EqualTo(144f));
            Assert.That(r.SlotWidth, Is.EqualTo(112f).Within(0.01f));
            Assert.That(r.CenterX[1], Is.EqualTo(180f).Within(0.01f));
            Assert.That(r.CenterX[0] - 12f, Is.EqualTo(r.SlotWidth / 2f).Within(0.01f)); // margen de 12 dp por lado
        }

        [Test]
        public void RowPitch_IsHeightPlusLiftPlusGap()
        {
            BoardLayoutResult r = BoardLayout.Compute(7, 6f, 360f, Top, Bottom);
            Assert.That(r.FootY[4] - r.FootY[0], Is.EqualTo(r.CuiaHeight + 16f + 24f).Within(0.01f));
            Assert.That(r.FootY[0] - r.CuiaHeight, Is.GreaterThanOrEqualTo(Top + 16f)); // cabe elevada
            Assert.That(r.FootY[6], Is.LessThanOrEqualTo(Bottom));
        }

        [Test]
        public void LastRow_RestsOnTheFootLine_AndTheSlackGoesOnTop()
        {
            // 07 §7.4: banda de río al fondo del tablero y filas alineadas por el pie; el hueco sobrante queda arriba.
            BoardLayoutResult one = BoardLayout.Compute(3, 6f, 360f, Top, 590f);
            Assert.That(one.FootY[0], Is.EqualTo(590f).Within(0.01f));
            Assert.That(one.FootY[2], Is.EqualTo(590f).Within(0.01f));

            BoardLayoutResult two = BoardLayout.Compute(7, 6f, 360f, Top, 590f);
            Assert.That(two.FootY[6], Is.EqualTo(590f).Within(0.01f));
            Assert.That(two.FootY[0], Is.EqualTo(590f - (two.CuiaHeight + 16f + 24f)).Within(0.01f));
        }

        [Test]
        public void ShortScreens_ShrinkTheCuias()
        {
            // 16:9 (360 × 640): las tres filas tienen que caber.
            BoardLayoutResult r = BoardLayout.Compute(16, 6f, 360f, 88f, 480f);
            Assert.That(r.FootY[15], Is.LessThanOrEqualTo(480f + 0.01f));
            Assert.That(r.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth));
        }
    }
}
