#nullable enable
using AF.Core.Board;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Tarea DC3 (pregunta 19 (a) de <c>docs/GDD.md</c> §6.3; D-028; 02 §5 y §12): el lienzo del juego mide 360 dp de ancho y, si
    /// así quedaran menos de 640 dp de alto, manda el alto (<see cref="GameScreen.CanvasScale"/>), así que en 16:9 mide
    /// 360 × 640 dp y 1 sp de lienzo es 1 sp físico. Aquí, sin escena:
    /// <list type="bullet">
    /// <item><description>la escala en 16:9 (1080 × 1920 y 720 × 1280 px: ancho / 360 y 360 × 640 dp), 19,5:9 y 20:9 (sin
    /// cambios: 360 × 780 y 360 × 800) y en una tableta 4:3 (manda el alto: 640 dp);</description></item>
    /// <item><description>todos los tableros de 1-700 (base y alivios; la variante publicada y la «a» de 301-700, con la Tinaja) caben a 360 × 640 dp (tablero entre 88 y 480 dp,
    /// <c>BoardLayoutTests</c>) al cargar y con las dos cuias que se pueden añadir (Cuia extra y Cuia de rescate; 14 + 2 en 3
    /// filas, P12 de D-006, D-030), con como mucho 3 filas, la cuia ≥ 28 dp y todo el vidrio dentro del panel.</description></item>
    /// </list>
    /// La escena (texto ≥ 12 sp físicos, HUD, tablero y bandeja sin solapes, 14 + 2 y el 323) se prueba en
    /// <c>AF.Tests.PlayMode.Canvas16x9LayoutTests</c>.
    /// </summary>
    public sealed class Canvas16x9BoardFitTests
    {
        // 360 × 640 dp con la zona segura mínima (32 y 24 dp; 02 §5): tablero entre safeTop + 56 = 88 y
        // (640 − 24 − 16 − 80) − 40 = 480 (guía 3.2; BoardLayoutTests).
        private const float FitTop = 88f;
        private const float FitBottom = 480f;

        [TestCase(1080, 1920, 3f, 360f, 640f)] // 16:9 (02 §5), el caso de la pregunta 19
        [TestCase(720, 1280, 2f, 360f, 640f)] // 16:9 a 2 px/dp
        [TestCase(1080, 2160, 3f, 360f, 720f)] // 18:9: antes 390 × 780 (revisión de DC3: también cambia)
        [TestCase(1080, 2280, 3f, 360f, 760f)] // 19:9: antes 369,2 × 780
        [TestCase(1440, 2960, 4f, 360f, 740f)] // 18,5:9: antes 379,7 × 780
        [TestCase(1080, 2340, 3f, 360f, 780f)] // 19,5:9: igual que antes
        [TestCase(1080, 2400, 3f, 360f, 800f)] // 20:9: igual que antes
        [TestCase(720, 1600, 2f, 360f, 800f)]
        [TestCase(1536, 2048, 3.2f, 480f, 640f)] // tableta 4:3: manda el alto (el tablero se centra a 480 dp, 02 §5); antes 584 × 780
        public void CanvasScale_360Wide_AtLeast640High(int widthPx, int heightPx, float scale, float canvasW, float canvasH)
        {
            float k = GameScreen.CanvasScale(widthPx, heightPx);
            Assert.That(k, Is.EqualTo(scale).Within(1e-4f), "escala (px por dp)");
            Assert.That(widthPx / k, Is.EqualTo(canvasW).Within(0.01f), "ancho del lienzo");
            Assert.That(heightPx / k, Is.EqualTo(canvasH).Within(0.01f), "alto del lienzo");
            Assert.That(heightPx / k, Is.GreaterThanOrEqualTo(GameScreen.MinHeight - 0.01f));
        }

        [Test]
        public void CanvasScale_In16x9_OneCanvasSpIsOnePhysicalSp()
        {
            // 02 §12: los 12 sp son físicos también en 16:9. Con el lienzo de antes (min(ancho / 360, alto / 780)) el de
            // 1080 × 1920 medía 438,75 × 780 dp y 12 sp de lienzo eran 9,8 sp físicos.
            foreach (int widthPx in new[] { 720, 1080, 1440 })
            {
                int heightPx = widthPx * 16 / 9;
                float k = GameScreen.CanvasScale(widthPx, heightPx);
                float physicalSpPerCanvasSp = k / (widthPx / GameScreen.ReferenceWidth);
                Assert.That(physicalSpPerCanvasSp, Is.EqualTo(1f).Within(1e-4f), widthPx + " × " + heightPx);
                Assert.That(UiBuilder.MinTextSp * physicalSpPerCanvasSp, Is.GreaterThanOrEqualTo(12f - 1e-3f));
            }
        }

        [Test]
        public void EveryBoard1To700_AndWithTwoAddedCuias_FitsTheBoardAt360x640()
        {
            // La variante publicada de 1-700 y la «a» de 301-700 (la de la Tinaja, como CampaignPackTests).
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            Assert.That(catalog.LastLevel, Is.GreaterThanOrEqualTo(700), "condición del test: los packs 1-700");
            LevelCatalog variantA = LevelCatalog.Create(new[] { TestPacks.WorldVariantA(7), TestPacks.WorldVariantA(8), TestPacks.WorldVariantA(9) });
            int boards = 0;
            int fourteen = 0;
            int tinaja = 0;
            foreach ((LevelCatalog c, int first, string name) in new[] { (catalog, catalog.FirstLevel, LevelCatalog.PublishedVariant), (variantA, 301, LevelCatalog.VariantA) })
            {
                for (int level = first; level <= 700; level++)
                {
                    LevelDefinition? def = c.Find(level);
                    if (def == null)
                    {
                        continue;
                    }

                    string what = def.Id + " (variante " + name + ")";
                    AssertFits(def.CreateBoard(), what, ref boards, ref fourteen, ref tinaja);
                    for (int r = 0; r < def.Relief.Count; r++)
                    {
                        AssertFits(def.Relief[r].CreateBoard(), what + ".relief[" + r + "] " + def.Relief[r].Type, ref boards, ref fourteen, ref tinaja);
                    }
                }
            }

            Assert.That(boards, Is.GreaterThan(700), "condición del test: los tableros de 1-700 y sus alivios");
            Assert.That(fourteen, Is.GreaterThan(0), "condición del test: tableros de 14 cuias (14 + 2)");
            Assert.That(tinaja, Is.GreaterThan(0), "condición del test: tableros con la Tinaja");
        }

        private static void AssertFits(BoardState board, string what, ref int boards, ref int fourteen, ref int tinaja)
        {
            boards++;
            int generated = board.Count;
            fourteen += generated == 14 ? 1 : 0;
            int pair = BoardLayout.TinajaPairStart(board);
            tinaja += pair >= 0 ? 1 : 0;
            float heightU = CuiaGeometry.HeightU(MaxCapacity(board));
            int most = Mathf.Min(BoardState.MaxCuias, generated + 2);
            for (int count = generated; count <= most; count++)
            {
                string at = what + " (" + generated + (count > generated ? " + " + (count - generated) : string.Empty) + " cuias, 360 × 640 dp)";
                BoardLayoutResult layout = BoardLayout.Compute(count, generated, heightU, 360f, FitTop, FitBottom, pair);
                Assert.That(layout.Rows, Is.LessThanOrEqualTo(3), at + ": como mucho 3 filas (D-006)");
                Assert.That(layout.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth - 0.01f), at + ": cuia ≥ 28 dp");
                // VA-15 (BoardLayoutTests): el marco puede pasar hasta 8 dp bajo la fila del HUD; las cuias, nunca.
                Assert.That(layout.PanelY, Is.GreaterThanOrEqualTo(FitTop - 8f - 0.01f), at + ": el panel, como mucho 8 dp bajo el HUD");
                float left = layout.PanelX;
                float right = layout.PanelX + layout.PanelWidth;
                float half = 0.5f * layout.CuiaWidth;
                for (int i = 0; i < count; i++)
                {
                    if (layout.CenterX[i] - half < left - 0.01f || layout.CenterX[i] + half > right + 0.01f
                        || layout.FootY[i] > layout.PanelBottom + 0.01f || layout.FootY[i] - layout.CuiaHeight < layout.PanelY - 0.01f
                        || layout.FootY[i] > FitBottom - BoardLayout.ShelfDrop + 0.01f || layout.FootY[i] - layout.CuiaHeight < FitTop - 0.01f)
                    {
                        Assert.Fail(at + ": la cuia " + i + " se sale del panel o del tablero");
                    }
                }
            }
        }

        private static int MaxCapacity(BoardState board)
        {
            int max = 0;
            for (int i = 0; i < board.Count; i++)
            {
                max = Mathf.Max(max, board[i].Capacity);
            }

            return max;
        }
    }
}
