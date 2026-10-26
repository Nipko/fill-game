#nullable enable
using System;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las hojas procedurales (<c>ProceduralArt.DrawLeaf</c>, su sombra y <c>DrawLeafShape</c>) solo pintan dentro de su
    /// silueta y nunca dejan valores que no son números (añadido de D-051 del 2026-10-25): en float, <c>sin(π)</c> es un
    /// negativo diminuto, y la potencia fraccionaria de ese negativo daba NaN en la punta de las hojas lanceoladas y en
    /// bráctea; ese NaN borraba una línea a lo ancho de la hoja en todo lo pintado antes en la misma textura (y lo pintado
    /// después encima, porque mezclar con NaN da NaN).
    /// </summary>
    public sealed class LeafDrawingTests
    {
        private static readonly Color Background = new Color(0.2f, 0.45f, 0.8f, 1f);
        private static readonly Color Dark = new Color(0.08f, 0.35f, 0.2f, 1f);
        private static readonly Color Light = new Color(0.5f, 0.8f, 0.4f, 1f);

        private const int Size = 112;

        // Largo, ancho, curva y px por dp: hoja normal, foliolo muy fino, hoja ancha de nervios, hoja más ancha que larga,
        // muy curvada, muy pequeña y por debajo del píxel.
        private static readonly float[][] Cases =
        {
            new[] { 40f, 20f, 0.10f, 1f },
            new[] { 44f, 1.5f, -0.30f, 1f },
            new[] { 42f, 26f, 0.06f, 3f },
            new[] { 22f, 34f, -0.08f, 1f },
            new[] { 30f, 12f, 0.60f, 0.5f },
            new[] { 3f, 2f, 0f, 0.5f },
            new[] { 0.6f, 0.4f, 0.2f, 1f },
        };

        private static LeafShape[] Shapes => (LeafShape[])Enum.GetValues(typeof(LeafShape));

        [Test]
        public void LeafWidthAt_IsFiniteAndInRange_AtEveryU()
        {
            // Los alrededores del 0 y del 1 en float (donde sin(π · u^p) roza el cero por los dos lados) y fuera de [0, 1].
            float[] edges = { -1f, -1e-7f, 0f, 1e-7f, 1e-3f, 0.5f, 0.999f, 0.9999f, 0.99999f, 0.9999999f, 0.99999994f, 1f, 1.00000012f, 1.02f, 2f, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            foreach (LeafShape shape in Shapes)
            {
                for (int i = 0; i <= 20000; i++)
                {
                    AssertWidth(shape, i / 20000f);
                }

                foreach (float u in edges)
                {
                    AssertWidth(shape, u);
                }

                Assert.That(ProceduralArt.LeafWidthAt(shape, 1f), Is.EqualTo(0f), shape + ": la punta, sin ancho");
                Assert.That(ProceduralArt.LeafWidthAt(shape, 0.4f), Is.GreaterThan(0.5f), shape + ": ancha en el centro");
            }
        }

        [Test]
        public void DrawLeaf_EveryShapeAndAngle_OnlyPaintsInsideItsSilhouette()
        {
            int drawn = 0;
            foreach (LeafShape shape in Shapes)
            {
                foreach (float[] c in Cases)
                {
                    for (int deg = 0; deg < 360; deg += 15)
                    {
                        Vector2 dir = Dir(deg + 0.5f);
                        var basePx = new Vector2(Size * 0.5f, Size * 0.5f);
                        PixelCanvas pc = Canvas();
                        ProceduralArt.DrawLeaf(pc, basePx, dir, c[0], c[1], c[2], shape, Dark, Light, c[3], 7);
                        string what = shape + " largo " + c[0] + ", ancho " + c[1] + ", curva " + c[2] + ", " + deg + "°";
                        int changed = AssertInside(pc, basePx, dir, c[0], c[1], c[2], shape, -0.02f, 1.02f, 0.75f, what);
                        if (c[0] >= 3f)
                        {
                            Assert.That(changed, Is.GreaterThan(0), what + ": pinta algo");
                            drawn++;
                        }
                    }
                }
            }

            Assert.That(drawn, Is.GreaterThan(0));
        }

        [Test]
        public void DrawLeafShadow_EveryShapeAndAngle_OnlyPaintsInsideItsSilhouette()
        {
            foreach (LeafShape shape in Shapes)
            {
                foreach (float[] c in Cases)
                {
                    for (int deg = 0; deg < 360; deg += 15)
                    {
                        Vector2 dir = Dir(deg + 0.5f);
                        var basePx = new Vector2(Size * 0.5f, Size * 0.5f);
                        PixelCanvas pc = Canvas();
                        ProceduralArt.DrawLeafShadow(pc, basePx, dir, c[0], c[1], c[2], shape, c[3]);
                        string what = "sombra " + shape + " largo " + c[0] + ", ancho " + c[1] + ", " + deg + "°";
                        AssertInside(pc, basePx, dir, c[0], c[1], c[2], shape, -0.05f, 1.05f, 2f * c[3], what);
                    }
                }
            }
        }

        [Test]
        public void DrawLeafShape_EveryAngle_OnlyPaintsInsideItsSilhouette()
        {
            foreach (float[] c in Cases)
            {
                for (int deg = 0; deg < 360; deg += 15)
                {
                    Vector2 dir = Dir(deg + 0.5f);
                    var basePx = new Vector2(Size * 0.5f, Size * 0.5f);
                    PixelCanvas pc = Canvas();
                    ProceduralArt.DrawLeafShape(pc, basePx, dir, c[0], c[1], Dark, 1f);
                    AssertInside(pc, basePx, dir, c[0], c[1], 0f, LeafShape.Ovate, 0f, 1f, 0.5f, "silueta largo " + c[0] + ", " + deg + "°");
                }
            }
        }

        [Test]
        public void DegenerateLeaves_LeaveTheCanvasUntouched()
        {
            // Largo o ancho nulos, negativos o que no son números, dirección nula o no finita, curva o escala infinitas.
            var basePx = new Vector2(Size * 0.5f, Size * 0.5f);
            Vector2 up = Vector2.up;
            var bad = new (Vector2 dir, float length, float width, float bend, float k)[]
            {
                (up, 0f, 10f, 0.1f, 1f),
                (up, -20f, 10f, 0.1f, 1f),
                (up, float.NaN, 10f, 0.1f, 1f),
                (up, float.PositiveInfinity, 10f, 0.1f, 1f),
                (up, 20f, 0f, 0.1f, 1f),
                (up, 20f, -4f, 0.1f, 1f),
                (up, 20f, float.NaN, 0.1f, 1f),
                (Vector2.zero, 20f, 10f, 0.1f, 1f),
                (new Vector2(float.NaN, 1f), 20f, 10f, 0.1f, 1f),
                (up, 20f, 10f, float.PositiveInfinity, 1f),
                (up, 20f, 10f, float.NaN, 1f),
                (up, 20f, 10f, 0.1f, float.NaN),
            };
            foreach (LeafShape shape in Shapes)
            {
                for (int i = 0; i < bad.Length; i++)
                {
                    var b = bad[i];
                    PixelCanvas pc = Canvas();
                    ProceduralArt.DrawLeaf(pc, basePx, b.dir, b.length, b.width, b.bend, shape, Dark, Light, b.k, 1);
                    ProceduralArt.DrawLeafShadow(pc, basePx, b.dir, b.length, b.width, b.bend, shape, b.k);
                    if (shape == LeafShape.Ovate && Finite(b.bend) && Finite(b.k)) // la silueta no usa curva ni escala
                    {
                        ProceduralArt.DrawLeafShape(pc, basePx, b.dir, b.length, b.width, Dark, 1f);
                    }

                    AssertUntouched(pc, shape + ", caso " + i);
                }
            }

            PixelCanvas nanBase = Canvas();
            ProceduralArt.DrawLeaf(nanBase, new Vector2(float.NaN, 10f), up, 20f, 10f, 0.1f, LeafShape.Lanceolate, Dark, Light, 1f, 1);
            AssertUntouched(nanBase, "base no finita");
        }

        [Test]
        public void ArtWithLeaves_HasNoNonFinitePixels()
        {
            // Todo el arte que pinta hojas lanceoladas o en bráctea: follaje de las cuatro esquinas (con frondas de açaí y la
            // heliconia), manojo del extremo de la rama, pétalo de heliconia y atlas de frutas (taperebá y Río Negro).
            AssertFinite(ProceduralArt.BuildFoliageCanvas(0), "follaje arriba-izq.");
            AssertFinite(ProceduralArt.BuildFoliageCanvas(1), "follaje arriba-der.");
            AssertFinite(ProceduralArt.BuildFoliageCanvas(2), "follaje abajo-izq.");
            AssertFinite(ProceduralArt.BuildFoliageCanvas(3), "follaje abajo-der.");
            AssertFinite(ProceduralArt.BuildBranchEndFoliageCanvas(), "manojo de la rama");
            AssertFinite(ProceduralArt.BuildPetalCanvas(), "pétalo");
            AssertFinite(ProceduralArt.BuildFruitAtlasCanvas(), "atlas de frutas");
        }

        private static void AssertWidth(LeafShape shape, float u)
        {
            float w = ProceduralArt.LeafWidthAt(shape, u);
            Assert.That(float.IsNaN(w) || float.IsInfinity(w), Is.False, shape + ": ancho no finito en u = " + u.ToString("R"));
            Assert.That(w, Is.InRange(0f, 1f), shape + ": ancho fuera de [0, 1] en u = " + u.ToString("R"));
        }

        private static Vector2 Dir(float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        private static PixelCanvas Canvas()
        {
            var pc = new PixelCanvas(Size, Size);
            pc.Fill(Background);
            return pc;
        }

        // Cuenta los píxeles que cambian y falla si alguno no es finito o cae fuera de la silueta analítica: u en [uMin, uMax]
        // y a menos del medio ancho de la hoja en ese u más el borde suave (margin, px) del nervio curvado.
        private static int AssertInside(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float width, float bend, LeafShape shape, float uMin, float uMax, float margin, string what)
        {
            var n = new Vector2(-dir.y, dir.x);
            float bendPx = bend * length;
            int changed = 0;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color c = pc.Get(x, y);
                    Assert.That(IsFinite(c), Is.True, what + ": píxel no finito en (" + x + ", " + y + ")");
                    if (Same(c, Background))
                    {
                        continue;
                    }

                    changed++;
                    Vector2 rel = new Vector2(x + 0.5f, y + 0.5f) - basePx;
                    float u = Vector2.Dot(rel, dir) / length;
                    float across = Vector2.Dot(rel, n) - (bendPx * u * u);
                    float hw = width * 0.5f * ProceduralArt.LeafWidthAt(shape, u);
                    bool inside = u >= uMin - 1e-4f && u <= uMax + 1e-4f && Mathf.Abs(across) < hw + margin + 1e-3f;
                    Assert.That(inside, Is.True, what + ": píxel (" + x + ", " + y + ") alterado fuera de la hoja (u = " + u.ToString("F3") + ", a " + across.ToString("F2") + " px del nervio, medio ancho " + hw.ToString("F2") + ")");
                }
            }

            return changed;
        }

        private static void AssertUntouched(PixelCanvas pc, string what)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color c = pc.Get(x, y);
                    Assert.That(IsFinite(c) && Same(c, Background), Is.True, what + ": píxel (" + x + ", " + y + ") alterado: " + c);
                }
            }
        }

        private static void AssertFinite(PixelCanvas pc, string what)
        {
            int bad = 0;
            int firstX = -1;
            int firstY = -1;
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    if (!IsFinite(pc.Get(x, y)))
                    {
                        if (bad == 0)
                        {
                            firstX = x;
                            firstY = y;
                        }

                        bad++;
                    }
                }
            }

            Assert.That(bad, Is.EqualTo(0), what + ": " + bad + " píxeles no finitos (el primero en " + firstX + ", " + firstY + ")");
        }

        private static bool IsFinite(Color c)
        {
            return Finite(c.r) && Finite(c.g) && Finite(c.b) && Finite(c.a);
        }

        private static bool Finite(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }

        private static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 1e-6f && Mathf.Abs(a.g - b.g) < 1e-6f && Mathf.Abs(a.b - b.b) < 1e-6f && Mathf.Abs(a.a - b.a) < 1e-6f;
        }
    }
}
