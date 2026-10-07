#nullable enable
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>Layout del tablero (GDD 02 §5; 07 §4.1; guía visual v2, 3.2-3.3, VA-01 y VA-15).</summary>
    public sealed class BoardLayoutTests
    {
        // 360 × 780 con la zona segura de las capturas (32 dp arriba, 24 abajo): HUD hasta 88; bandeja en 660-740;
        // boardBottom = trayTop − 40 = 620 (guía 3.2).
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
            Assert.That(r.FootY[6], Is.LessThanOrEqualTo(Bottom - BoardLayout.ShelfDrop)); // la repisa cabe
            Assert.That(r.RowFootY[0], Is.EqualTo(r.FootY[0]).Within(0.01f));
            Assert.That(r.RowFootY[1], Is.EqualTo(r.FootY[6]).Within(0.01f));
        }

        [TestCase(3, 6f)]
        [TestCase(7, 6f)]
        [TestCase(14, 6f)]
        [TestCase(16, 6f)]
        [TestCase(14, 7f)]
        public void Block_IsCentredInTheBoardArea(int count, float heightU)
        {
            // VA-01 (D-003; guía 3.2): el hueco de arriba y el de abajo difieren ≤ 2 dp. Fin del anclaje inferior.
            BoardLayoutResult r = BoardLayout.Compute(count, heightU, 360f, Top, Bottom);
            float above = r.BlockTop - Top;
            float below = Bottom - r.BlockBottom;
            Assert.That(above, Is.GreaterThanOrEqualTo(0f));
            Assert.That(System.Math.Abs(above - below), Is.LessThanOrEqualTo(2f));

            // El bloque es: elevación de la fila 1 + filas + huecos + repisa de 15 dp bajo la última.
            float lastFoot = r.RowFootY[r.Rows - 1];
            Assert.That(r.BlockBottom, Is.EqualTo(lastFoot + BoardLayout.ShelfDrop).Within(0.01f));
            Assert.That(r.RowFootY[0] - r.CuiaHeight - BoardLayout.Lift, Is.EqualTo(r.BlockTop).Within(0.01f));
        }

        // Tabla 3.3 de la guía recalculada con el margen del panel de 22 dp (revisión visual del 2026-10-07).
        [TestCase(7, 6f, 152.5f, 555.5f)] // nivel 7 (4 y 3): banda libre de 104 dp hasta la bandeja (660)
        [TestCase(3, 6f, 244.5f, 463.5f)] // nivel 1
        [TestCase(14, 6f, 195.7f, 512.3f)] // estrés 2 × 7
        [TestCase(16, 6f, 100.1f, 607.9f)] // estrés 3 filas (6/5/5)
        [TestCase(14, 7f, 178.9f, 529.1f)] // estrés Cuia alta 2 × 7
        public void Panel_MatchesTheMeasuredCasesOfTheGuide_At360x780(int count, float heightU, float top, float bottom)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, heightU, 360f, Top, Bottom);
            Assert.That(r.PanelX, Is.EqualTo(8f).Within(0.01f));
            Assert.That(r.PanelWidth, Is.EqualTo(344f).Within(0.01f));
            Assert.That(r.PanelY, Is.EqualTo(top).Within(1f));
            Assert.That(r.PanelBottom, Is.EqualTo(bottom).Within(1f));

            // VA-15: ni el HUD ni la bandeja se solapan con el panel; el marco puede pasar 8 dp bajo el HUD.
            Assert.That(r.PanelY, Is.GreaterThanOrEqualTo(Top - 8f));
            Assert.That(r.PanelBottom, Is.LessThanOrEqualTo(Bottom + 40f));
        }

        [TestCase(3, 6f)]
        [TestCase(7, 6f)]
        [TestCase(14, 6f)]
        [TestCase(16, 6f)]
        [TestCase(14, 7f)]
        public void LiftedCuia_StaysAtLeast8dpBelowTheInnerEdgeOfTheFrame(int count, float heightU)
        {
            // Revisión visual del 2026-10-07 (03_selected_l7): con el marco tallado de 13,5 dp (1,5 + 12), la cuia elevada
            // de la fila 1 (BlockTop) queda ≥ 8 dp por debajo del canto interior del marco; con 14 dp de margen lo tocaba.
            const float frameDp = 13.5f;
            BoardLayoutResult r = BoardLayout.Compute(count, heightU, 360f, Top, Bottom);
            Assert.That(r.BlockTop - (r.PanelY + frameDp), Is.GreaterThanOrEqualTo(8f));
            Assert.That((r.PanelBottom - frameDp) - r.BlockBottom, Is.GreaterThanOrEqualTo(8f));
        }

        [Test]
        public void Tablets_LimitThePanelTo480dp()
        {
            BoardLayoutResult r = BoardLayout.Compute(7, 6f, 600f, Top, Bottom);
            Assert.That(r.PanelX, Is.EqualTo(68f).Within(0.01f)); // (600 − 480) / 2 + 8
            Assert.That(r.PanelWidth, Is.EqualTo(464f).Within(0.01f));
        }

        [Test]
        public void ShortScreens_ShrinkTheCuias()
        {
            // 16:9 (360 × 640): las tres filas tienen que caber con su repisa.
            BoardLayoutResult r = BoardLayout.Compute(16, 6f, 360f, 88f, 480f);
            Assert.That(r.FootY[15], Is.LessThanOrEqualTo(480f - BoardLayout.ShelfDrop + 0.01f));
            Assert.That(r.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(88f - 0.01f));
        }
    }
}
