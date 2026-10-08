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

        // D-004 (C18): tope(T) = 64 / 56 / 48 dp para T ≤ 3 / 4-5 / ≥ 6; alto 1:3; u = ancho / 2. Margen de 12 dp por lado.
        [TestCase(1, 64f, 192f, 336f)] // una cuia: centrada en 180
        [TestCase(3, 64f, 192f, 112f)] // nivel 1: centros 68 / 180 / 292
        [TestCase(4, 56f, 168f, 84f)] // centros 54 / 138 / 222 / 306
        [TestCase(5, 47.04f, 141.12f, 67.2f)] // niveles 2, 3 y 6: 0,70 × 67,2; el tope de 56 no actúa
        [TestCase(6, 48f, 144f, 112f)] // niveles 4 y 5: dos filas de 3
        [TestCase(7, 48f, 144f, 84f)] // niveles 7-10: filas de 4 y 3
        public void CuiaWidth_FollowsTheCapOfD004_At360x780(int count, float width, float height, float slot)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, 6f, 360f, Top, Bottom);
            Assert.That(r.CuiaWidth, Is.EqualTo(width).Within(0.01f));
            Assert.That(r.CuiaHeight, Is.EqualTo(height).Within(0.01f));
            Assert.That(r.SlotWidth, Is.EqualTo(slot).Within(0.01f));

            // u = ancho / 2 (07 §4.1) lo calcula CuiaView: lo comprueban PourPoseTests y VisualGuardTests (u = 32 en el nivel 1).
            for (int i = 0; i < r.PerRow[0]; i++)
            {
                // Fila 1 centrada: 12 dp de margen por lado.
                Assert.That(r.CenterX[i], Is.EqualTo(12f + ((i + 0.5f) * slot)).Within(0.01f), "cuia " + i);
            }

            if (count == 1)
            {
                Assert.That(r.CenterX[0], Is.EqualTo(180f).Within(0.01f));
            }
            else if (count == 3)
            {
                Assert.That(r.CenterX[0], Is.EqualTo(68f).Within(0.01f));
                Assert.That(r.CenterX[1], Is.EqualTo(180f).Within(0.01f));
                Assert.That(r.CenterX[2], Is.EqualTo(292f).Within(0.01f));
            }
            else if (count == 4)
            {
                Assert.That(r.CenterX[0], Is.EqualTo(54f).Within(0.01f));
                Assert.That(r.CenterX[1], Is.EqualTo(138f).Within(0.01f));
                Assert.That(r.CenterX[2], Is.EqualTo(222f).Within(0.01f));
                Assert.That(r.CenterX[3], Is.EqualTo(306f).Within(0.01f));
            }
        }

        [Test]
        public void MaxCuiaWidthFor_FollowsT()
        {
            // D-004 (C18): T cuenta las cuias del tablero en pantalla, incluidas Cuia extra y Cuia de rescate (02 R1).
            for (int t = 1; t <= 3; t++)
            {
                Assert.That(BoardLayout.MaxCuiaWidthFor(t), Is.EqualTo(64f), "T = " + t);
            }

            for (int t = 4; t <= 5; t++)
            {
                Assert.That(BoardLayout.MaxCuiaWidthFor(t), Is.EqualTo(56f), "T = " + t);
            }

            for (int t = 6; t <= 16; t++)
            {
                Assert.That(BoardLayout.MaxCuiaWidthFor(t), Is.EqualTo(48f), "T = " + t);
            }

            // Una Cuia extra que lleva el tablero de 3 a 4 cuias baja el tope de 64 a 56 (re-layout de 300 ms, 02 §4.1).
            Assert.That(BoardLayout.Compute(3, 6f, 360f, Top, Bottom).CuiaWidth, Is.EqualTo(64f).Within(0.01f));
            Assert.That(BoardLayout.Compute(3 + 1, 6f, 360f, Top, Bottom).CuiaWidth, Is.EqualTo(56f).Within(0.01f));
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
        [TestCase(4, 6f)]
        [TestCase(5, 6f)]
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

        // Tabla 3.3 de la guía recalculada con el margen del panel de 22 dp (revisión visual del 2026-10-07) y 36 dp por
        // arriba en los tableros de una fila (revisión de D-004).
        [TestCase(7, 6f, 152.5f, 555.5f)] // nivel 7 (4 y 3): banda libre de 104 dp hasta la bandeja (660)
        [TestCase(3, 6f, 206.5f, 487.5f)] // nivel 1 (D-004: cuias de 64 dp; banda libre de 172,5 dp)
        [TestCase(4, 6f, 218.5f, 475.5f)] // 4 cuias de 56 dp (D-004)
        [TestCase(5, 6f, 231.9f, 462.1f)] // niveles 2, 3 y 6 (5 cuias de 47 dp)
        [TestCase(6, 6f, 152.5f, 555.5f)] // niveles 4 y 5 (dos filas de 3, 48 dp)
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
        [TestCase(4, 6f)]
        [TestCase(5, 6f)]
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

        [TestCase(3, 6f, 36f)]
        [TestCase(5, 6f, 36f)]
        [TestCase(7, 6f, 22f)]
        [TestCase(14, 6f, 22f)]
        public void PanelTopMargin_Is36InOneRowBoards_And22Otherwise(int count, float heightU, float margin)
        {
            // Revisión de D-004: en una fila el panel sube 14 dp más para que el origen inclinado quepa bajo el canto
            // interior del marco; en dos filas, 36 estrecharía el río del nivel 7 de 34,5 a 20,5 dp.
            BoardLayoutResult r = BoardLayout.Compute(count, heightU, 360f, Top, Bottom);
            Assert.That(r.BlockTop - r.PanelY, Is.EqualTo(margin).Within(0.01f));
            Assert.That(r.PanelBottom - r.BlockBottom, Is.EqualTo(BoardLayout.PanelMargin).Within(0.01f));
        }

        // Techo del vertido (GameScreen.PourCeilingFor): 4 dp de aire bajo el canto interior del marco en una fila
        // (revisión de D-004: con < 1 dp la cuia parecía apoyarse en el marco), borde exterior del panel en dos filas
        // (allí la fila 1 queda a 8,5 dp del canto interior). Marco de 13,5 dp; en el nivel 2 el 9-slice lo adelgaza a
        // 0,45 × 13,5.
        [TestCase(3, 13.5f, true)] // nivel 1: cavidad de 22,5 dp
        [TestCase(5, 6.075f, true)] // nivel 2
        [TestCase(6, 13.5f, false)] // nivel 4
        [TestCase(7, 13.5f, false)] // nivel 7
        public void PourCeiling_Leaves4dpOfAirUnderTheFrame_InOneRowBoards(int count, float frame, bool inner)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, 6f, 360f, Top, Bottom);
            float ceiling = GameScreen.PourCeilingFor(r, frame, Top);
            Assert.That(GameScreen.PourCeilingAirDp, Is.GreaterThanOrEqualTo(4f));
            Assert.That(ceiling, Is.EqualTo(inner ? r.PanelY + frame + GameScreen.PourCeilingAirDp : r.PanelY).Within(0.01f));
            Assert.That(ceiling, Is.GreaterThanOrEqualTo(Top - 4f)); // nunca más de 4 dp dentro de la fila del HUD
        }

        [Test]
        public void PourCeiling_NeverRisesMoreThan4dpIntoTheHudRow()
        {
            // 16 cuias en 3 filas a 360 × 780: el panel se recorta 8 dp dentro de la fila del HUD y el techo se queda a 4.
            BoardLayoutResult r = BoardLayout.Compute(16, 6f, 360f, Top, Bottom);
            Assert.That(GameScreen.PourCeilingFor(r, 6f, Top), Is.GreaterThanOrEqualTo(Top - 4f));
            BoardLayoutResult clipped = BoardLayout.Compute(16, 6f, 360f, 88f, 480f);
            Assert.That(clipped.PanelY, Is.EqualTo(80f).Within(0.01f));
            Assert.That(GameScreen.PourCeilingFor(clipped, 6f, 88f), Is.EqualTo(84f).Within(0.01f));
        }

        [TestCase(3, 64f)]
        [TestCase(4, 56f)]
        [TestCase(5, 56f)] // 0,70 × 91,2 = 63,84, recortado a 56
        [TestCase(6, 48f)]
        [TestCase(7, 48f)]
        [TestCase(14, 45.6f)] // 0,70 × 65,14: el tope no actúa
        public void Tablets_CapTheCuiaByT(int count, float width)
        {
            // 600 dp de ancho: tablero de 480 dp centrado y la cuia no supera tope(T) (02 §5; D-004).
            BoardLayoutResult r = BoardLayout.Compute(count, 6f, 600f, Top, Bottom);
            Assert.That(r.CuiaWidth, Is.EqualTo(width).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.LessThanOrEqualTo(BoardLayout.MaxCuiaWidthFor(count)));
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
            Assert.That(r.CuiaWidth, Is.EqualTo(31.2f).Within(0.05f));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(88f - 0.01f));
        }

        [Test]
        public void ShortScreens_KeepThreeCuiasAt64dp()
        {
            // 360 × 640 con el nivel 1 (D-004): 64 × 192 cabe sin encoger; panel en 136,5-417,5.
            BoardLayoutResult r = BoardLayout.Compute(3, 6f, 360f, 88f, 480f);
            Assert.That(r.CuiaWidth, Is.EqualTo(64f).Within(0.01f));
            Assert.That(r.CuiaHeight, Is.EqualTo(192f).Within(0.01f));
            Assert.That(r.PanelY, Is.EqualTo(136.5f).Within(0.01f));
            Assert.That(r.PanelBottom, Is.EqualTo(417.5f).Within(0.01f));
        }

        [Test]
        public void ShortScreens_TallCuiasInTwoRows_FitBelowTheCap()
        {
            // 360 × 640, Cuia alta (7 u) en dos filas: el encaje vertical manda sobre el tope de 48 (D-004).
            BoardLayoutResult r = BoardLayout.Compute(7, 7f, 360f, 88f, 480f);
            Assert.That(r.CuiaWidth, Is.EqualTo(45.86f).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.LessThan(48f));
            Assert.That(r.FootY[6], Is.LessThanOrEqualTo(480f - BoardLayout.ShelfDrop + 0.01f));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(88f - 0.01f));
        }

        [Test]
        public void ShortBoard_OneRow_FitsBelowTheCapOf64()
        {
            // Caso sintético de una fila (8 u entre 88 y 330): el encaje vertical se aplica después del tope (D-004).
            BoardLayoutResult r = BoardLayout.Compute(3, 8f, 360f, 88f, 330f);
            Assert.That(r.CuiaWidth, Is.EqualTo(52.75f).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.LessThan(64f));
            Assert.That(r.FootY[0], Is.LessThanOrEqualTo(315f + 0.01f));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(88f - 0.01f));
        }
    }
}
