#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Layout del tablero (GDD 02 §5; 07 §4.1; guía visual v2, 3.2-3.3, VA-01 y VA-15; D-004; D-006, punto 4, como Magic
    /// Sort): filas de 1, 2 o 3 según T, las 3 filas de ≤ 5 cuias desde 11 cuias cuando dan la cuia más ancha, filas fijas
    /// al cargar el nivel y cuias de 5,60 u de alto (<see cref="CuiaGeometry.HeightU"/> con 4 dosis).
    /// </summary>
    public sealed class BoardLayoutTests
    {
        // 360 × 780 con la zona segura de las capturas (32 dp arriba, 24 abajo): HUD hasta 88; bandeja en 660-740;
        // boardBottom = trayTop − 40 = 620 (guía 3.2). 360 × 800: 640. 360 × 640 (16:9): 480.
        private const float Top = 88f;
        private const float Bottom = 620f;
        private const float Bottom800 = 640f;
        private const float Bottom640 = 480f;

        // Marco tallado del panel: 1,5 de contorno + 12 de madera (guía 3.2).
        private const float FrameDp = 13.5f;

        private static readonly float H4 = CuiaGeometry.HeightU(4); // 5,60 u (D-006)
        private static readonly float H5 = CuiaGeometry.HeightU(5); // 6,60 u: Cuia alta de 5 dosis

        [TestCase(3, "3")]
        [TestCase(5, "5")]
        [TestCase(6, "3/3")]
        [TestCase(7, "4/3")]
        [TestCase(10, "5/5")]
        [TestCase(11, "4/4/3")]
        [TestCase(12, "4/4/4")]
        [TestCase(13, "5/4/4")]
        [TestCase(14, "5/5/4")]
        [TestCase(15, "5/5/5")]
        [TestCase(16, "6/5/5")]
        public void Rows_FollowTheSplitOf02_5_And_D006_At360x780(int count, string split)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom);
            Assert.That(Split(r), Is.EqualTo(split));
        }

        [TestCase(11, "6/5")]
        [TestCase(12, "6/6")]
        [TestCase(13, "5/4/4")]
        [TestCase(14, "5/5/4")]
        [TestCase(15, "5/5/5")]
        [TestCase(16, "6/5/5")]
        public void Rows_In16x9_TwoRowsWinIn11And12_ThreeFrom13(int count, string split)
        {
            // D-006: en 16:9 (360 × 640 dp) las 2 filas dan la cuia más ancha con T = 11-12; con 13-14, 3 filas al cargar
            // aunque 2 dieran la cuia más ancha (P12 de D-006, opción c; D-030).
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom640);
            Assert.That(Split(r), Is.EqualTo(split));
        }

        [Test]
        public void SevenPerRow_In16x9_GivesSlot48AndCuia33_6By94_08()
        {
            // 02 §5: con 7 por fila, ranura 48 y cuia de 0,70 × 48 = 33,6 dp; alto 5,60 u = 94,08 dp (D-006). Desde P12 (c) solo
            // se llega a 7 por fila con 12 generadas (6/6) y Cuia extra y Cuia de rescate.
            BoardLayoutResult r = BoardLayout.Compute(14, 12, H4, 360f, Top, Bottom640);
            Assert.That(Split(r), Is.EqualTo("7/7"));
            Assert.That(r.SlotWidth, Is.EqualTo(48f).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.EqualTo(33.6f).Within(0.01f));
            Assert.That(r.CuiaHeight, Is.EqualTo(94.08f).Within(0.01f));
        }

        // D-004 (C18): tope(T) = 64 / 56 / 48 dp para T ≤ 3 / 4-5 / ≥ 6; alto 5,60 u (D-006); u = ancho / 2. Margen de 12 dp.
        [TestCase(1, 64f, 179.2f, 336f)] // una cuia: centrada en 180
        [TestCase(3, 64f, 179.2f, 112f)] // nivel 1: centros 68 / 180 / 292
        [TestCase(4, 56f, 156.8f, 84f)] // centros 54 / 138 / 222 / 306
        [TestCase(5, 47.04f, 131.712f, 67.2f)] // niveles 2, 3 y 6: 0,70 × 67,2; el tope de 56 no actúa
        [TestCase(6, 48f, 134.4f, 112f)] // niveles 4 y 5: dos filas de 3
        [TestCase(7, 48f, 134.4f, 84f)] // niveles 7-10: filas de 4 y 3
        [TestCase(10, 47.04f, 131.712f, 67.2f)] // dos filas de 5
        public void CuiaWidth_FollowsTheCapOfD004_At360x780(int count, float width, float height, float slot)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom);
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

        [TestCase(11, 46.905f, 131.333f)]
        [TestCase(12, 46.905f, 131.333f)]
        [TestCase(13, 46.905f, 131.333f)] // Magic Sort con 13-14 botellas: 45 × 138 dp en nuestra escala
        [TestCase(14, 46.905f, 131.333f)]
        [TestCase(15, 46.905f, 131.333f)]
        [TestCase(16, 39.2f, 109.76f)] // 6/5/5: ranura de 56
        public void ThreeRows_From11Cuias_At360x780(int count, float width, float height)
        {
            // D-006: 3 filas desde 11 cuias; el bloque no supera boardBottom − boardTop − 27 = 505 dp.
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom);
            Assert.That(r.Rows, Is.EqualTo(3));
            Assert.That(r.CuiaWidth, Is.EqualTo(width).Within(0.01f));
            Assert.That(r.CuiaHeight, Is.EqualTo(height).Within(0.01f));
            Assert.That(r.BlockBottom - r.BlockTop, Is.LessThanOrEqualTo(Bottom - Top - BoardLayout.MinBlockSlack + 0.01f));
        }

        [TestCase(11, 48f)]
        [TestCase(12, 48f)]
        [TestCase(13, 47.04f)]
        [TestCase(14, 47.04f)]
        [TestCase(15, 47.04f)]
        public void ThreeRows_At360x800(int count, float width)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom800);
            Assert.That(r.Rows, Is.EqualTo(3));
            Assert.That(r.CuiaWidth, Is.EqualTo(width).Within(0.01f));
        }

        [TestCase(11, 2, 39.2f)]
        [TestCase(12, 2, 39.2f)]
        [TestCase(13, 3, 30.24f)] // P12 de D-006 (c): 3 filas con T ≥ 13 (antes, 7/6 en 2 filas de 33,6 dp)
        [TestCase(14, 3, 30.24f)]
        [TestCase(15, 3, 30.24f)]
        [TestCase(16, 3, 30.24f)]
        public void ShortScreens_360x640(int count, int rows, float width)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom640);
            Assert.That(r.Rows, Is.EqualTo(rows));
            Assert.That(r.CuiaWidth, Is.EqualTo(width).Within(0.01f));
            Assert.That(r.FootY[count - 1], Is.LessThanOrEqualTo(Bottom640 - BoardLayout.ShelfDrop + 0.01f));
        }

        [Test]
        public void FixedRows_ExtraCuiasGoToTheShortestRow_TheLowestOnATie()
        {
            // D-006: las filas se fijan con las cuias generadas; Cuia extra o de rescate van a la fila más corta (a igualdad,
            // la de más abajo [P]) y solo se recalcula el ancho.
            BoardLayoutResult ten = BoardLayout.Compute(11, 10, H4, 360f, Top, Bottom);
            Assert.That(Split(ten), Is.EqualTo("5/6"), "10 generadas (5/5) + 1: dos filas, la extra abajo");
            Assert.That(ten.CuiaWidth, Is.EqualTo(39.2f).Within(0.01f));

            BoardLayoutResult thirteen = BoardLayout.Compute(14, 13, H4, 360f, Top, Bottom);
            Assert.That(Split(thirteen), Is.EqualTo("5/4/5"));
            Assert.That(thirteen.CuiaWidth, Is.EqualTo(46.905f).Within(0.01f));

            BoardLayoutResult fourteen = BoardLayout.Compute(16, 14, H4, 360f, Top, Bottom);
            Assert.That(Split(fourteen), Is.EqualTo("5/5/6"));
            Assert.That(fourteen.CuiaWidth, Is.EqualTo(39.2f).Within(0.01f));

            // Sin cuias de más, el layout de siempre.
            Assert.That(Split(BoardLayout.Compute(10, 10, H4, 360f, Top, Bottom)), Is.EqualTo("5/5"));
            Assert.That(() => BoardLayout.Compute(10, 11, H4, 360f, Top, Bottom), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // Prueba del 2026-10-09, T-C: las generadas conservan su fila y cada cuia añadida (Cuia extra, rescate o Balsa), en
        // orden, va a la derecha de la fila más corta en ese momento (a igualdad, la de abajo [P]). Antes los índices se
        // asignaban fila a fila y, con dos cuias añadidas, una cuia generada saltaba de fila.
        [Test]
        public void TwoAdded_8Generated_NoGeneratedCuiaChangesRow()
        {
            BoardLayoutResult r = BoardLayout.Compute(10, 8, H4, 360f, Top, Bottom);
            Assert.That(Split(r), Is.EqualTo("5/5"));
            Assert.That(r.RowOf[4], Is.EqualTo(1), "la 5.ª generada sigue en la fila de abajo (4/4)");
            Assert.That(r.RowOf[8], Is.EqualTo(1), "la primera añadida, abajo (a igualdad, la de abajo)");
            Assert.That(r.RowOf[9], Is.EqualTo(0), "la segunda añadida, arriba (la más corta)");
            AssertRowsConsistent(r);
        }

        [Test]
        public void TwoAdded_10Generated_TheSecondGoesToTheRightOfTheTopRow()
        {
            BoardLayoutResult r = BoardLayout.Compute(12, 10, H4, 360f, Top, Bottom);
            Assert.That(Split(r), Is.EqualTo("6/6"));
            Assert.That(r.RowOf[5], Is.EqualTo(1), "la 6.ª generada sigue abajo");
            Assert.That(r.RowOf[10], Is.EqualTo(1));
            Assert.That(r.RowOf[11], Is.EqualTo(0));
            Assert.That(r.CenterX[11], Is.GreaterThan(r.CenterX[4]), "a la derecha de la última generada de la fila de arriba");
            AssertRowsConsistent(r);
        }

        [Test]
        public void TwoAdded_12Generated_InThreeRows()
        {
            BoardLayoutResult r = BoardLayout.Compute(14, 12, H4, 360f, Top, Bottom);
            Assert.That(Split(r), Is.EqualTo("4/5/5"));
            Assert.That(r.RowOf[8], Is.EqualTo(2), "la 9.ª generada sigue en la fila 3 (4/4/4)");
            Assert.That(r.RowOf[12], Is.EqualTo(2));
            Assert.That(r.RowOf[13], Is.EqualTo(1), "a igualdad entre la 1 y la 2, la de abajo");
            AssertRowsConsistent(r);
        }

        [Test]
        public void TwoAdded_13Generated_InThreeRows()
        {
            BoardLayoutResult r = BoardLayout.Compute(15, 13, H4, 360f, Top, Bottom);
            Assert.That(Split(r), Is.EqualTo("5/5/5"));
            Assert.That(r.RowOf[9], Is.EqualTo(2), "la 10.ª generada sigue en la fila 3 (5/4/4)");
            Assert.That(r.RowOf[13], Is.EqualTo(2));
            Assert.That(r.RowOf[14], Is.EqualTo(1));
            AssertRowsConsistent(r);
        }

        // P12 de D-006, opción c (D-030, 2026-10-19): con 13-14 generadas, 2 filas dejaban una de 8 cuias al añadir Cuia extra
        // y Cuia de rescate (ranura de 42 dp y cuia de 29,4 dp en 16:9, < 48 dp de 02 §3.3); se cargan en 3 filas en 16:9
        // (360 × 640) y en 19,5:9 (360 × 780), como T + 2 > 14 (BoardLayout.MaxTwoRowWithAdded).
        [TestCase(13, Bottom640)]
        [TestCase(14, Bottom640)]
        [TestCase(13, Bottom)]
        [TestCase(14, Bottom)]
        [TestCase(13, Bottom800)]
        [TestCase(14, Bottom800)]
        public void P12_ThirteenAndFourteenGenerated_LoadInThreeRows_In16x9And19_5x9(int generated, float bottom)
        {
            Assert.That(BoardLayout.RowsFor(generated, H4, 360f, Top, bottom), Is.EqualTo(3));
            Assert.That(BoardLayout.Compute(generated, H4, 360f, Top, bottom).Rows, Is.EqualTo(3));
            Assert.That(BoardLayout.Compute(System.Math.Min(16, generated + BoardLayout.AddedCuiasForRows), generated, H4, 360f, Top, bottom).Rows,
                Is.EqualTo(3), "con Cuia extra y Cuia de rescate, las mismas filas (D-006, punto 4)");
        }

        [TestCase(Bottom640)]
        [TestCase(Bottom)]
        public void P12_FourteenPlusTwo_NoRowLongerThanSix_AndTheCuiaAtLeastAsWideAsSixteenInThreeRows(float bottom)
        {
            // 14 generadas (5/5/4) + Cuia extra + Cuia de rescate: 5/5/5 y luego 5/5/6 (a igualdad, la de abajo [P]). La fila más
            // larga tiene 6 y la cuia mide al menos lo que con 16 generadas en 3 filas (6/5/5: 30,24 dp en 16:9, 02 §5).
            BoardLayoutResult r = BoardLayout.Compute(16, 14, H4, 360f, Top, bottom);
            Assert.That(r.Rows, Is.EqualTo(3));
            Assert.That(MaxPerRow(r), Is.EqualTo(6));
            Assert.That(Split(r), Is.EqualTo("5/5/6"));
            BoardLayoutResult sixteen = BoardLayout.Compute(16, H4, 360f, Top, bottom);
            Assert.That(r.CuiaWidth, Is.GreaterThanOrEqualTo(sixteen.CuiaWidth - 0.01f));
            if (bottom == Bottom640)
            {
                Assert.That(r.CuiaWidth, Is.GreaterThanOrEqualTo(30.24f - 0.01f), "30,2 dp en 16:9 (02 §5)");
            }

            for (int generated = 1; generated <= 14; generated++)
            {
                int count = System.Math.Min(16, generated + BoardLayout.AddedCuiasForRows);
                BoardLayoutResult added = BoardLayout.Compute(count, generated, H4, 360f, Top, bottom);
                Assert.That(MaxPerRow(added), Is.LessThanOrEqualTo(7), "generadas " + generated + " + 2: ninguna fila de 8 (P12)");
            }

            Assert.That(BoardLayout.RowsFor(12, H4, 360f, Top, Bottom640), Is.EqualTo(2), "11-12 siguen eligiendo la cuia más ancha");
        }

        [TestCase(Bottom)]
        [TestCase(Bottom800)]
        [TestCase(Bottom640)]
        public void AddingCuias_NeverMovesACuiaToAnotherRow_AndKeepsEachRowInIndexOrder(float bottom)
        {
            // Propiedad (T-C) a 360 × 780, 360 × 800 y 360 × 640: con 1-14 generadas y hasta dos añadidas (máximo 16, 02 R1),
            // RowOf de cada cuia es el mismo antes y después de añadir otra, y dentro de cada fila CenterX crece con el índice.
            for (int generated = 1; generated <= 14; generated++)
            {
                BoardLayoutResult before = BoardLayout.Compute(generated, generated, H4, 360f, Top, bottom);
                AssertRowsConsistent(before);
                int max = System.Math.Min(16, generated + 2);
                for (int count = generated + 1; count <= max; count++)
                {
                    BoardLayoutResult after = BoardLayout.Compute(count, generated, H4, 360f, Top, bottom);
                    string what = "generadas " + generated + ", en pantalla " + count + ", boardBottom " + bottom;
                    Assert.That(after.Rows, Is.EqualTo(before.Rows), what + ": las filas no cambian (D-006)");
                    for (int i = 0; i < count - 1; i++)
                    {
                        Assert.That(after.RowOf[i], Is.EqualTo(before.RowOf[i]), what + ": fila de la cuia " + i);
                    }

                    AssertRowsConsistent(after);
                    before = after;
                }
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

            // Una Cuia extra que lleva el tablero de 3 a 4 cuias baja el tope de 64 a 56 (re-layout de 300 ms, 02 §4.1) y
            // sigue en una fila (D-006).
            Assert.That(BoardLayout.Compute(3, H4, 360f, Top, Bottom).CuiaWidth, Is.EqualTo(64f).Within(0.01f));
            BoardLayoutResult extra = BoardLayout.Compute(3 + 1, 3, H4, 360f, Top, Bottom);
            Assert.That(extra.CuiaWidth, Is.EqualTo(56f).Within(0.01f));
            Assert.That(extra.Rows, Is.EqualTo(1));
        }

        [Test]
        public void RowPitch_IsHeightPlusLiftPlusGap()
        {
            BoardLayoutResult r = BoardLayout.Compute(7, H4, 360f, Top, Bottom);
            Assert.That(r.FootY[4] - r.FootY[0], Is.EqualTo(r.CuiaHeight + 16f + 24f).Within(0.01f));
            Assert.That(r.FootY[0] - r.CuiaHeight, Is.GreaterThanOrEqualTo(Top + 16f)); // cabe elevada
            Assert.That(r.FootY[6], Is.LessThanOrEqualTo(Bottom - BoardLayout.ShelfDrop)); // la repisa cabe
            Assert.That(r.RowFootY[0], Is.EqualTo(r.FootY[0]).Within(0.01f));
            Assert.That(r.RowFootY[1], Is.EqualTo(r.FootY[6]).Within(0.01f));

            // Tres filas (D-006): el hueco entre filas sigue en 40 dp (16 de elevación + 24), por la repisa de D-003.
            BoardLayoutResult three = BoardLayout.Compute(13, H4, 360f, Top, Bottom);
            Assert.That(three.RowFootY[1] - three.RowFootY[0] - three.CuiaHeight, Is.EqualTo(40f).Within(0.01f));
            Assert.That(three.RowFootY[2] - three.RowFootY[1] - three.CuiaHeight, Is.EqualTo(40f).Within(0.01f));
        }

        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(7, false)]
        [TestCase(13, false)]
        [TestCase(14, false)]
        [TestCase(16, false)]
        [TestCase(14, true)]
        public void Block_IsCentredInTheBoardArea(int count, bool tall)
        {
            // VA-01 (D-003; guía 3.2): el hueco de arriba y el de abajo difieren ≤ 2 dp. Fin del anclaje inferior.
            BoardLayoutResult r = BoardLayout.Compute(count, tall ? H5 : H4, 360f, Top, Bottom);
            float above = r.BlockTop - Top;
            float below = Bottom - r.BlockBottom;
            Assert.That(above, Is.GreaterThanOrEqualTo(0f));
            Assert.That(System.Math.Abs(above - below), Is.LessThanOrEqualTo(2f));

            // El bloque es: elevación de la fila 1 + filas + huecos + repisa de 15 dp bajo la última.
            float lastFoot = r.RowFootY[r.Rows - 1];
            Assert.That(r.BlockBottom, Is.EqualTo(lastFoot + BoardLayout.ShelfDrop).Within(0.01f));
            Assert.That(r.RowFootY[0] - r.CuiaHeight - BoardLayout.Lift, Is.EqualTo(r.BlockTop).Within(0.01f));
        }

        // Tabla 3.3 de la guía recalculada con el margen del panel de 22 dp en todos los tableros (D-006 retira los 36 dp de
        // una fila) y las cuias de 5,60 u (D-006).
        [TestCase(7, 162.1f, 545.9f)] // nivel 7 (4 y 3)
        [TestCase(3, 226.9f, 481.1f)] // nivel 1 (D-004: cuias de 64 dp)
        [TestCase(4, 238.1f, 469.9f)] // 4 cuias de 56 dp (D-004)
        [TestCase(5, 250.64f, 457.36f)] // niveles 2, 3 y 6 (5 cuias de 47 dp)
        [TestCase(6, 162.1f, 545.9f)] // niveles 4 y 5 (dos filas de 3, 48 dp)
        [TestCase(11, 80f, 628.5f)] // 3 filas: el panel se recorta 8 dp bajo el HUD
        [TestCase(14, 80f, 628.5f)]
        [TestCase(16, 111.86f, 596.14f)] // 3 filas (6/5/5)
        public void Panel_MatchesTheMeasuredCasesOfTheGuide_At360x780(int count, float top, float bottom)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom);
            Assert.That(r.PanelX, Is.EqualTo(8f).Within(0.01f));
            Assert.That(r.PanelWidth, Is.EqualTo(344f).Within(0.01f));
            Assert.That(r.PanelY, Is.EqualTo(top).Within(0.05f));
            Assert.That(r.PanelBottom, Is.EqualTo(bottom).Within(0.05f));

            // VA-15: ni el HUD ni la bandeja se solapan con el panel; el marco puede pasar 8 dp bajo el HUD.
            Assert.That(r.PanelY, Is.GreaterThanOrEqualTo(Top - 8f));
            Assert.That(r.PanelBottom, Is.LessThanOrEqualTo(Bottom + 40f));
        }

        [TestCase(3, 620f, false)]
        [TestCase(4, 620f, false)]
        [TestCase(5, 620f, false)]
        [TestCase(7, 620f, false)]
        [TestCase(11, 620f, false)]
        [TestCase(13, 620f, false)]
        [TestCase(14, 620f, false)]
        [TestCase(15, 620f, false)]
        [TestCase(16, 620f, false)]
        [TestCase(14, 620f, true)]
        [TestCase(13, 640f, false)]
        [TestCase(3, 480f, false)]
        [TestCase(7, 480f, false)]
        [TestCase(11, 480f, false)]
        [TestCase(13, 480f, false)]
        [TestCase(14, 480f, false)]
        [TestCase(15, 480f, false)]
        [TestCase(16, 480f, false)]
        public void LiftedCuia_StaysAtLeast8dpBelowTheInnerEdgeOfTheFrame(int count, float bottom, bool tall)
        {
            // Revisión visual del 2026-10-07 (03_selected_l7) y D-006 [P]: con el marco tallado de 13,5 dp (1,5 + 12), la cuia
            // elevada de la fila 1 (BlockTop) queda ≥ 8 dp por debajo del canto interior del marco, también cuando el panel
            // se recorta bajo el HUD (3 filas), y la última repisa, ≥ 8 dp por encima del de abajo.
            BoardLayoutResult r = BoardLayout.Compute(count, tall ? H5 : H4, 360f, Top, bottom);
            Assert.That(r.BlockTop - (r.PanelY + FrameDp), Is.GreaterThanOrEqualTo(8f - 0.01f));
            Assert.That((r.PanelBottom - FrameDp) - r.BlockBottom, Is.GreaterThanOrEqualTo(8f - 0.01f));
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(14)]
        public void PanelTopMargin_Is22InEveryBoard(int count)
        {
            // D-006: sin techo del vertido, el panel deja 22 dp (PanelMargin) sobre y bajo el bloque en todas las filas; por
            // arriba, salvo que se recorte en boardTop − 8 (3 filas a 360 × 780: 21,5 dp).
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 360f, Top, Bottom);
            Assert.That(r.PanelY, Is.EqualTo(System.Math.Max(r.BlockTop - BoardLayout.PanelMargin, Top - 8f)).Within(0.01f));
            Assert.That(r.BlockTop - r.PanelY, Is.LessThanOrEqualTo(BoardLayout.PanelMargin + 0.01f));
            Assert.That(r.PanelBottom - r.BlockBottom, Is.EqualTo(BoardLayout.PanelMargin).Within(0.01f));
            if (count <= 7)
            {
                Assert.That(r.BlockTop - r.PanelY, Is.EqualTo(BoardLayout.PanelMargin).Within(0.01f));
            }
        }

        [TestCase(3, 1, 64f)]
        [TestCase(4, 1, 56f)]
        [TestCase(5, 1, 56f)] // 0,70 × 91,2 = 63,84, recortado a 56
        [TestCase(6, 2, 48f)]
        [TestCase(7, 2, 48f)]
        [TestCase(11, 2, 48f)] // 6/5: 0,70 × 76 = 53,2 → 48; en 3 filas el alto la bajaría a 46,9
        [TestCase(12, 2, 48f)]
        [TestCase(13, 3, 46.905f)] // 2 filas de 7: 45,6; 3 filas: 48 → 46,9 por el alto
        [TestCase(14, 3, 46.905f)]
        [TestCase(15, 3, 46.905f)]
        [TestCase(16, 3, 46.905f)]
        public void Tablets_CapTheCuiaByT(int count, int rows, float width)
        {
            // 600 dp de ancho: tablero de 480 dp centrado y la cuia no supera tope(T) (02 §5; D-004; D-006).
            BoardLayoutResult r = BoardLayout.Compute(count, H4, 600f, Top, Bottom);
            Assert.That(r.Rows, Is.EqualTo(rows));
            Assert.That(r.CuiaWidth, Is.EqualTo(width).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.LessThanOrEqualTo(BoardLayout.MaxCuiaWidthFor(count)));
            Assert.That(r.PanelX, Is.EqualTo(68f).Within(0.01f)); // (600 − 480) / 2 + 8
            Assert.That(r.PanelWidth, Is.EqualTo(464f).Within(0.01f));
        }

        [Test]
        public void ShortScreens_ShrinkTheCuias()
        {
            // 16:9 (360 × 640): las tres filas tienen que caber con su repisa y la holgura del marco.
            BoardLayoutResult r = BoardLayout.Compute(16, H4, 360f, Top, Bottom640);
            Assert.That(r.FootY[15], Is.LessThanOrEqualTo(Bottom640 - BoardLayout.ShelfDrop + 0.01f));
            Assert.That(r.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth));
            Assert.That(r.CuiaWidth, Is.EqualTo(30.24f).Within(0.01f));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(Top + (BoardLayout.MinBlockSlack / 2f) - 0.01f));
        }

        [Test]
        public void ShortScreens_KeepThreeCuiasAt64dp()
        {
            // 360 × 640 con el nivel 1 (D-004): 64 × 179,2 cabe sin encoger; panel en 156,9-411,1 (22 dp por arriba, D-006).
            BoardLayoutResult r = BoardLayout.Compute(3, H4, 360f, Top, Bottom640);
            Assert.That(r.CuiaWidth, Is.EqualTo(64f).Within(0.01f));
            Assert.That(r.CuiaHeight, Is.EqualTo(179.2f).Within(0.01f));
            Assert.That(r.PanelY, Is.EqualTo(156.9f).Within(0.01f));
            Assert.That(r.PanelBottom, Is.EqualTo(411.1f).Within(0.01f));
        }

        [Test]
        public void ShortScreens_TallCuiasInTwoRows_FitBelowTheCap()
        {
            // 360 × 640, Cuia alta (5 dosis, 6,60 u) en dos filas: el encaje vertical manda sobre el tope de 48 (D-004):
            // 2 × (392 − 27 − 31 − 40) / (2 × 6,6) = 44,55 dp.
            BoardLayoutResult r = BoardLayout.Compute(7, H5, 360f, Top, Bottom640);
            Assert.That(r.CuiaWidth, Is.EqualTo(44.545f).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.LessThan(48f));
            Assert.That(r.FootY[6], Is.LessThanOrEqualTo(Bottom640 - BoardLayout.ShelfDrop + 0.01f));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(Top - 0.01f));
        }

        [Test]
        public void ShortBoard_OneRow_FitsBelowTheCapOf64()
        {
            // Caso sintético de una fila (8 u entre 88 y 330): el encaje vertical se aplica después del tope (D-004):
            // 2 × (242 − 27 − 31) / 8 = 46 dp.
            BoardLayoutResult r = BoardLayout.Compute(3, 8f, 360f, Top, 330f);
            Assert.That(r.CuiaWidth, Is.EqualTo(46f).Within(0.01f));
            Assert.That(r.CuiaWidth, Is.LessThan(64f));
            Assert.That(r.FootY[0], Is.LessThanOrEqualTo(315f + 0.01f));
            Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(Top - 0.01f));
        }

        // 02 §5 y 07 §4.5 (Tinaja, M13; D-019; propuesta de la Tinaja, c.6; TJ2): la tinaja y su cuia, en ranuras contiguas de la
        // misma fila. Si el reparto de D-006 pone el corte entre las dos, se mueve: la fila más larga, lo más corta posible; a
        // igualdad, el más parecido al de 02 §5 y con más cuias arriba [P].
        [TestCase(Bottom, 10, 4, "6/4")]
        [TestCase(Bottom, 10, 3, "5/5")]
        [TestCase(Bottom, 7, 3, "3/4")]
        [TestCase(Bottom, 11, 3, "3/4/4")]
        [TestCase(Bottom, 11, 7, "4/3/4")]
        [TestCase(Bottom, 16, 5, "5/6/5")]
        [TestCase(Bottom640, 11, 5, "5/6")]
        [TestCase(Bottom640, 14, 4, "4/5/5")] // P12 de D-006 (c): 14 en 3 filas también en 16:9
        [TestCase(Bottom640, 14, 9, "5/4/5")]
        public void TinajaPair_SameRow_MovesTheCut_At360x780And360x640(float bottom, int count, int pairStart, string split)
        {
            BoardLayoutResult r = BoardLayout.Compute(count, count, H5, 360f, Top, bottom, pairStart);
            Assert.That(Split(r), Is.EqualTo(split));
            Assert.That(r.RowOf[pairStart], Is.EqualTo(r.RowOf[pairStart + 1]), "la tinaja y su cuia, en la misma fila");
            Assert.That(r.CenterX[pairStart + 1] - r.CenterX[pairStart], Is.EqualTo(r.SlotWidth).Within(0.01f), "en ranuras contiguas");
            Assert.That(r.Rows, Is.EqualTo(BoardLayout.Compute(count, count, H5, 360f, Top, bottom).Rows), "el número de filas no cambia");
            AssertRowsConsistent(r);
        }

        [TestCase(Bottom)]
        [TestCase(Bottom640)]
        public void TinajaPair_AlwaysSameRow_EveryPositionAndCount_EvenWithAddedCuias(float bottom)
        {
            // Propiedad a 360 × 780 y 360 × 640 (TJ2: «probado a 360 × 780 y 360 × 640»), con la tinaja de 5 (6,60 u, la altura de
            // la Cuia alta de 5; 07 §4.5): con 2-16 generadas, la pareja en cualquier posición y hasta dos cuias añadidas (Cuia extra,
            // Cuia de rescate), las dos van en la misma fila y contiguas; las filas no cambian al añadir (D-006); la fila más
            // larga crece como mucho en una; y sin cortar la pareja el reparto es el de 02 §5.
            for (int generated = 2; generated <= 16; generated++)
            {
                BoardLayoutResult plain = BoardLayout.Compute(generated, generated, H5, 360f, Top, bottom);
                for (int pair = 0; pair + 1 < generated; pair++)
                {
                    BoardLayoutResult before = BoardLayout.Compute(generated, generated, H5, 360f, Top, bottom, pair);
                    string what = "generadas " + generated + ", pareja " + pair + "-" + (pair + 1) + ", boardBottom " + bottom;
                    Assert.That(before.Rows, Is.EqualTo(plain.Rows), what + ": las mismas filas");
                    Assert.That(MaxPerRow(before), Is.LessThanOrEqualTo(MaxPerRow(plain) + 1), what + ": la fila más larga, como mucho una más");
                    if (plain.RowOf[pair] == plain.RowOf[pair + 1])
                    {
                        Assert.That(Split(before), Is.EqualTo(Split(plain)), what + ": sin corte entre las dos, el reparto de 02 §5");
                    }

                    int max = System.Math.Min(16, generated + 2);
                    for (int count = generated; count <= max; count++)
                    {
                        BoardLayoutResult r = BoardLayout.Compute(count, generated, H5, 360f, Top, bottom, pair);
                        string at = what + ", en pantalla " + count;
                        Assert.That(r.RowOf[pair], Is.EqualTo(r.RowOf[pair + 1]), at + ": la tinaja y su cuia, en la misma fila");
                        Assert.That(r.CenterX[pair + 1] - r.CenterX[pair], Is.EqualTo(r.SlotWidth).Within(0.01f), at + ": contiguas");
                        for (int i = 0; i < generated; i++)
                        {
                            Assert.That(r.RowOf[i], Is.EqualTo(before.RowOf[i]), at + ": fila de la cuia " + i + " (D-006)");
                        }

                        AssertRowsConsistent(r);
                    }
                }
            }
        }

        [Test]
        public void TinajaPair_OutOfRangeOrMissing_IsIgnored()
        {
            Assert.That(Split(BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, -1)), Is.EqualTo("5/5"));
            Assert.That(Split(BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, 9)), Is.EqualTo("5/5"), "sin cuia detrás de la última");
            Assert.That(Split(BoardLayout.Compute(11, 10, H5, 360f, Top, Bottom, 9)), Is.EqualTo("5/6"), "la pareja tiene que ser de las generadas");
        }

        [TestCase(Bottom)]
        [TestCase(Bottom640)]
        public void TinajaAtARowEnd_SqueezesItsRow_SoTheJarAndTheJirauStayInsideTheGlassExtent(float bottom)
        {
            // TJ2 (revisión): la vasija (2,70 u frente a las 2 u del vidrio) y el poste del jirau de su cuia salen de su ranura. Si
            // caen en una punta de su fila, esa fila se aprieta hacia dentro: nada de la pareja pasa de lo que ocupa el vidrio en
            // una fila llena (el marco se adelgaza con ContentLeft igual que sin tinaja) y el jirau queda ≥ 2 dp dentro, sobre la
            // repisa. Las filas, el ancho de la cuia y las demás filas no cambian; dentro de la fila, nada se solapa.
            for (int generated = 2; generated <= 16; generated++)
            {
                for (int pair = 0; pair + 1 < generated; pair++)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        int tinaja = pair + side;
                        int partner = side == 0 ? pair + 1 : pair;
                        int max = System.Math.Min(16, generated + 2);
                        for (int count = generated; count <= max; count++)
                        {
                            string at = "generadas " + generated + ", en pantalla " + count + ", tinaja " + tinaja + ", su cuia " + partner + ", boardBottom " + bottom;
                            BoardLayoutResult plain = BoardLayout.Compute(count, generated, H5, 360f, Top, bottom, pair);
                            BoardLayoutResult r = BoardLayout.Compute(count, generated, H5, 360f, Top, bottom, pair, tinaja);
                            Assert.That(Split(r), Is.EqualTo(Split(plain)), at + ": las mismas filas");
                            Assert.That(r.CuiaWidth, Is.EqualTo(plain.CuiaWidth), at + ": el mismo ancho de cuia");
                            int row = r.RowOf[tinaja];
                            Assert.That(r.RowOf[partner], Is.EqualTo(row), at + ": la misma fila");
                            float last = float.MinValue;
                            for (int i = 0; i < count; i++)
                            {
                                Assert.That(r.RowOf[i], Is.EqualTo(plain.RowOf[i]), at + ": fila de la cuia " + i);
                                Assert.That(r.FootY[i], Is.EqualTo(plain.FootY[i]), at + ": pie de la cuia " + i);
                                if (r.RowOf[i] != row)
                                {
                                    Assert.That(r.CenterX[i], Is.EqualTo(plain.CenterX[i]), at + ": las otras filas no se mueven (cuia " + i + ")");
                                }
                                else
                                {
                                    if (last > float.MinValue)
                                    {
                                        Assert.That(r.CenterX[i] - last, Is.GreaterThanOrEqualTo(r.CuiaWidth - 0.01f), at + ": sin solaparse (cuia " + i + ")");
                                        Assert.That(r.CenterX[i] - last, Is.LessThanOrEqualTo(r.SlotWidth + 0.01f), at + ": como mucho una ranura (cuia " + i + ")");
                                    }

                                    last = r.CenterX[i];
                                }
                            }

                            float u = r.CuiaWidth / 2f;
                            float limit = System.Math.Min(plain.ContentLeft, plain.PanelX + BoardLayout.TinajaContentFromPanel);
                            Assert.That(r.ContentLeft, Is.GreaterThanOrEqualTo(limit - 0.01f), at + ": no pide más sitio que el vidrio");
                            float right = 360f - r.ContentLeft;
                            float half = 0.5f * ProceduralArt.TinajaBodyWidthU * u;
                            Assert.That(r.CenterX[tinaja] - half, Is.GreaterThanOrEqualTo(r.ContentLeft - 0.01f), at + ": la vasija, dentro");
                            Assert.That(r.CenterX[tinaja] + half, Is.LessThanOrEqualTo(right + 0.01f), at + ": la vasija, dentro");
                            float post = r.CenterX[partner] + ((partner < tinaja ? -1f : 1f) * TinajaOverlay.JirauCuiaExtentU * u);
                            float jirauL = System.Math.Min(post, r.CenterX[tinaja] - (TinajaOverlay.JirauTinajaExtentU * u));
                            float jirauR = System.Math.Max(post, r.CenterX[tinaja] + (TinajaOverlay.JirauTinajaExtentU * u));
                            Assert.That(jirauL, Is.GreaterThanOrEqualTo(r.ContentLeft + BoardLayout.TinajaJirauInsetDp - 0.01f), at + ": el jirau, sobre la repisa");
                            Assert.That(jirauR, Is.LessThanOrEqualTo(right - BoardLayout.TinajaJirauInsetDp + 0.01f), at + ": el jirau, sobre la repisa");
                        }
                    }
                }
            }
        }

        [Test]
        public void TinajaAtARowEnd_Case6x4_TheJarNoLongerReachesTheFrame()
        {
            // La captura tj2_09 (revisión de TJ2): la pareja en la 4 y la 5 (6/4), la tinaja al final de la fila 1. Sin apretar,
            // la vasija pasaba ≈ 0,35 u del vidrio de la punta y llegaba al marco; ahora acaba donde acaba el vidrio de la otra
            // punta. Sin tinaja en una punta (la pareja en la 1 y la 2), nada se mueve.
            BoardLayoutResult plain = BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, 4);
            BoardLayoutResult r = BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, 4, 5);
            Assert.That(Split(r), Is.EqualTo("6/4"));
            float u = r.CuiaWidth / 2f;
            float glassLeft = r.CenterX[0] - (0.5f * r.CuiaWidth);
            Assert.That(plain.CenterX[5] + (0.5f * ProceduralArt.TinajaBodyWidthU * u), Is.GreaterThan(360f - glassLeft + 1f), "antes se salía");
            Assert.That(r.CenterX[5] + (0.5f * ProceduralArt.TinajaBodyWidthU * u), Is.EqualTo(360f - glassLeft).Within(0.01f), "ahora acaba donde el vidrio de la otra punta");
            Assert.That(r.CenterX[0], Is.EqualTo(plain.CenterX[0]), "la otra punta no se mueve");
            Assert.That(r.ContentLeft, Is.EqualTo(glassLeft).Within(0.01f));

            BoardLayoutResult middle = BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, 1, 2);
            BoardLayoutResult middlePlain = BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, 1);
            for (int i = 0; i < 10; i++)
            {
                Assert.That(middle.CenterX[i], Is.EqualTo(middlePlain.CenterX[i]), "la pareja en medio de su fila: cuia " + i);
            }

            Assert.That(BoardLayout.Compute(10, 10, H5, 360f, Top, Bottom, 4, 7).CenterX[5], Is.EqualTo(plain.CenterX[5]), "un índice de tinaja fuera de la pareja no aprieta nada");
        }

        [Test]
        public void TinajaPairStart_FindsThePairOnTheInitialBoard()
        {
            // El 323 del paquete de muestra (pack_schema 3): su cuia en la 1 y la tinaja en la 2; y un tablero con la tinaja en la
            // 1 y su cuia a la derecha, en la 2. Tras la suelta (la cuia sin tipo), ya no hay pareja que buscar: por eso se mira
            // el tablero inicial.
            var tinaja = Cuia.Create(5, new[] { 9, 11, 9 }, 0b011).WithMechanic(MechanicType.Tinaja);
            Cuia cucharon = Cuia.Empty(2).WithMechanic(MechanicType.Cucharon);
            var board323 = new BoardState(new[] { Cuia.Create(4, new[] { 11, 11, 13 }), cucharon, tinaja, Cuia.Empty(4) });
            Assert.That(BoardLayout.TinajaPairStart(board323), Is.EqualTo(1));
            var board329 = new BoardState(new[] { Cuia.Empty(4), tinaja, cucharon, Cuia.Empty(4) });
            Assert.That(BoardLayout.TinajaPairStart(board329), Is.EqualTo(1), "su cuia a la derecha: la pareja empieza en la tinaja");
            var released = new BoardState(new[] { Cuia.Empty(4), Cuia.Empty(5).WithMechanic(MechanicType.Tinaja), Cuia.Empty(2), Cuia.Empty(4) });
            Assert.That(BoardLayout.TinajaPairStart(released), Is.EqualTo(-1));
            Assert.That(BoardLayout.TinajaPairStart(new BoardState(new[] { Cuia.Empty(4), Cuia.Empty(4) })), Is.EqualTo(-1));
        }

        private static int MaxPerRow(BoardLayoutResult r)
        {
            int max = 0;
            for (int i = 0; i < r.Rows; i++)
            {
                max = System.Math.Max(max, r.PerRow[i]);
            }

            return max;
        }

        private static string Split(BoardLayoutResult r)
        {
            var parts = new string[r.Rows];
            int sum = 0;
            for (int i = 0; i < r.Rows; i++)
            {
                parts[i] = r.PerRow[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
                sum += r.PerRow[i];
            }

            Assert.That(sum, Is.EqualTo(r.CenterX.Length), "todas las cuias tienen fila");
            return string.Join("/", parts);
        }

        // Cada fila tiene PerRow[r] cuias, con el pie en RowFootY[r], separadas una ranura y centradas en el lienzo, y su
        // CenterX crece con el índice (las añadidas, a la derecha).
        private static void AssertRowsConsistent(BoardLayoutResult r)
        {
            int count = r.CenterX.Length;
            for (int row = 0; row < r.Rows; row++)
            {
                int n = 0;
                float first = 0f;
                float last = float.MinValue;
                for (int i = 0; i < count; i++)
                {
                    if (r.RowOf[i] != row)
                    {
                        continue;
                    }

                    Assert.That(r.FootY[i], Is.EqualTo(r.RowFootY[row]), "pie de la cuia " + i);
                    if (n == 0)
                    {
                        first = r.CenterX[i];
                    }
                    else
                    {
                        Assert.That(r.CenterX[i] - last, Is.EqualTo(r.SlotWidth).Within(0.01f), "una ranura a la derecha de la anterior: cuia " + i);
                    }

                    last = r.CenterX[i];
                    n++;
                }

                Assert.That(n, Is.EqualTo(r.PerRow[row]), "cuias de la fila " + row);
                Assert.That(first + last, Is.EqualTo(360f).Within(0.01f), "fila " + row + " centrada en el lienzo de 360 dp");
            }
        }
    }
}
