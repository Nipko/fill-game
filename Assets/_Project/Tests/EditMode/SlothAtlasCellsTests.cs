#nullable enable
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El atlas provisional de TiTino se pinta con sus seis piezas a la vez en varios hilos (ProceduralArt.PaintSlothAtlas; pulido
    /// del 2026-10-25, D-052). Eso solo es seguro si cada pieza pinta dentro de su celda: si alguien agranda una pieza y se sale,
    /// estos tests lo detectan en lugar de dejar una carrera silenciosa.
    /// </summary>
    public sealed class SlothAtlasCellsTests
    {
        [Test]
        public void EachPiece_PaintsOnlyInsideItsCell()
        {
            for (int piece = 0; piece < ProceduralArt.SlothPieceCount; piece++)
            {
                var pc = new PixelCanvas(ProceduralArt.SlothAtlasPx, ProceduralArt.SlothAtlasH);
                ProceduralArt.DrawSlothPiece(pc, piece);
                RectInt cell = ProceduralArt.SlothPieceCell(piece);
                Color32[] px = pc.ToColor32();
                int inside = 0;
                int outside = 0;
                for (int y = 0; y < pc.Height; y++)
                {
                    for (int x = 0; x < pc.Width; x++)
                    {
                        if (px[(y * pc.Width) + x].a == 0)
                        {
                            continue;
                        }

                        if (cell.Contains(new Vector2Int(x, y)))
                        {
                            inside++;
                        }
                        else
                        {
                            outside++;
                        }
                    }
                }

                Assert.That(inside, Is.GreaterThan(0), "pieza " + piece + ": pinta algo en su celda " + cell);
                Assert.That(outside, Is.EqualTo(0), "pieza " + piece + ": ningún píxel fuera de su celda " + cell);
            }
        }

        [Test]
        public void TheCells_DoNotOverlap_AndFitTheAtlas()
        {
            var atlas = new RectInt(0, 0, ProceduralArt.SlothAtlasPx, ProceduralArt.SlothAtlasH);
            for (int a = 0; a < ProceduralArt.SlothPieceCount; a++)
            {
                RectInt ra = ProceduralArt.SlothPieceCell(a);
                Assert.That(ra.xMin >= atlas.xMin && ra.yMin >= atlas.yMin && ra.xMax <= atlas.xMax && ra.yMax <= atlas.yMax, Is.True, "celda " + a + " dentro del atlas");
                for (int b = a + 1; b < ProceduralArt.SlothPieceCount; b++)
                {
                    Assert.That(ra.Overlaps(ProceduralArt.SlothPieceCell(b)), Is.False, "celdas " + a + " y " + b + " sin solapar");
                }
            }
        }

        [Test]
        public void InParallel_TheAtlasIsTheSameAsInSequence()
        {
            var sequence = new PixelCanvas(ProceduralArt.SlothAtlasPx, ProceduralArt.SlothAtlasH);
            ProceduralArt.PaintSlothAtlas(sequence, false);
            var parallel = new PixelCanvas(ProceduralArt.SlothAtlasPx, ProceduralArt.SlothAtlasH);
            ProceduralArt.PaintSlothAtlas(parallel, true);
            Color32[] a = sequence.ToColor32();
            Color32[] b = parallel.ToColor32();
            Assert.That(b.Length, Is.EqualTo(a.Length));
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                {
                    diff++;
                }
            }

            Assert.That(diff, Is.EqualTo(0), "píxel a píxel");
        }
    }
}
