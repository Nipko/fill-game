#nullable enable
using AF.Presentation;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El tablero a 320 dp de ancho (02 §5, «Dispositivos»: «a 320 dp de ancho se admiten 6 cuias por fila (16 cuias = 3 filas
    /// 6/5/5)»; 02 §3.3: ranura ≥ 48 dp; tarea B11): <see cref="BoardLayout"/> con un lienzo de 320 dp, a los altos de 19,5:9
    /// (693 dp), 20:9 (711) y 16:9 (569, que el lienzo sube al mínimo de 640: 02 §5 y D-032, punto 5). Las cuias generadas van
    /// de 1 a 14 (02 R1) y, con la Cuia extra y la Cuia de rescate, hasta 16 en pantalla.
    /// </summary>
    public sealed class BoardLayout320Tests
    {
        private const float Width = 320f;
        private const float Top = 88f;

        // boardBottom = alto − 160 dp (bandeja de 80, margen de 40 y la zona segura de 24 de abajo con la orilla; los mismos 160
        // que BoardLayoutTests usa a 360: 780 → 620, 640 → 480).
        private static readonly float[] Bottoms = { 693f - 160f, 711f - 160f, 640f - 160f };

        private static readonly float H4 = CuiaGeometry.HeightU(4);

        [Test]
        public void At320_SixteenCuias_Are6_5_5()
        {
            for (int b = 0; b < Bottoms.Length; b++)
            {
                BoardLayoutResult r = BoardLayout.Compute(16, H4, Width, Top, Bottoms[b]);
                Assert.That(r.Rows, Is.EqualTo(3));
                Assert.That(r.PerRow, Is.EqualTo(new[] { 6, 5, 5 }), "02 §5: 16 cuias = 3 filas 6/5/5 (alto " + (Bottoms[b] + 160f) + ")");
            }
        }

        [Test]
        public void At320_GeneratedBoards_NeverPassSixPerRow_AndKeep48dpSlots()
        {
            for (int b = 0; b < Bottoms.Length; b++)
            {
                for (int count = 1; count <= 14; count++)
                {
                    BoardLayoutResult r = BoardLayout.Compute(count, H4, Width, Top, Bottoms[b]);
                    string where = count + " cuias, alto " + (Bottoms[b] + 160f);
                    for (int row = 0; row < r.Rows; row++)
                    {
                        Assert.That(r.PerRow[row], Is.LessThanOrEqualTo(6), "02 §5: como mucho 6 por fila a 320 dp (" + where + ")");
                    }

                    Assert.That(r.SlotWidth, Is.GreaterThanOrEqualTo(48f), "02 §3.3: ranura ≥ 48 dp (" + where + ")");
                    Assert.That(r.CuiaWidth, Is.GreaterThanOrEqualTo(28f), "02 §5: mínimo de 28 dp (" + where + ")");
                    Assert.That(r.PanelX, Is.GreaterThanOrEqualTo(0f), "el panel cabe en el ancho (" + where + ")");
                    Assert.That(r.PanelX + r.PanelWidth, Is.LessThanOrEqualTo(Width + 0.01f), "el panel cabe en el ancho (" + where + ")");
                    Assert.That(r.BlockTop, Is.GreaterThanOrEqualTo(Top - 0.01f), "el bloque cabe en la zona del tablero (" + where + ")");
                }
            }
        }

        [Test]
        public void At320_FromThirteenGenerated_AddedCuiasKeepSixPerRow()
        {
            // D-030 (P12 c): desde 13 generadas, 3 filas al cargar; con la Cuia extra y la Cuia de rescate (16 en pantalla) ninguna
            // fila pasa de 6, también a 320 dp.
            for (int b = 0; b < Bottoms.Length; b++)
            {
                for (int generated = 13; generated <= 14; generated++)
                {
                    for (int count = generated; count <= 16; count++)
                    {
                        BoardLayoutResult r = BoardLayout.Compute(count, generated, H4, Width, Top, Bottoms[b]);
                        Assert.That(r.Rows, Is.EqualTo(3));
                        for (int row = 0; row < r.Rows; row++)
                        {
                            Assert.That(r.PerRow[row], Is.LessThanOrEqualTo(6), generated + " + " + (count - generated) + " cuias, alto " + (Bottoms[b] + 160f));
                        }

                        Assert.That(r.SlotWidth, Is.GreaterThanOrEqualTo(48f));
                    }
                }
            }
        }
    }
}
