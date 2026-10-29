#nullable enable
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Jornada 6, observación 12 (2026-10-29): el agujero del primer fotograma de la intro calca la forma de la máscara del sistema
    /// (en One UI, un cuadrado redondeado de 497 px en los ejes dentro de la caja de 504) y se abre al círculo sin que ningún píxel
    /// cambie de golpe (<see cref="IconMaskShape"/>, <see cref="BrandIntroMorph.ShapeAlpha"/>). Sin máscara, el círculo.
    /// </summary>
    public sealed class IconMaskShapeTests
    {
        // La caja del SM-S938B (504 px, centrada en 540, 1170) con una máscara squircle conocida: una superelipse n = 4 con los ejes a
        // 497/504 del medio lado.
        private const int BoxPx = 504;
        private const float KnownN = 4f;
        private static readonly float KnownAxis = 497f / 504f;
        private static readonly float KnownDiag = KnownAxis * Mathf.Sqrt(2f) * Mathf.Pow(2f, -1f / KnownN);

        private static SplashIconBox Squircle()
        {
            return new SplashIconBox(288, 918, BoxPx, BoxPx, 0, 0, 1080, 2340, Mathf.RoundToInt(KnownAxis * 10000f), Mathf.RoundToInt(KnownDiag * 10000f), 0);
        }

        [Test]
        public void TheKnownSquircle_FitsBackItsExponent_AndFrameZeroMatchesItOnAxesAndDiagonals()
        {
            IconMaskShape shape = IconMaskShape.From(Squircle());
            Assert.That(shape.FromMask, Is.True);
            Assert.That(shape.IsSquircle, Is.True);
            Assert.That(shape.Name, Is.EqualTo("squircle"));
            Assert.That(shape.Exponent, Is.EqualTo(KnownN).Within(0.02f), "la superelipse ajustada es la conocida");

            float rho0 = BoxPx * 0.5f;
            shape.At(rho0, rho0, out float axis, out float n, out float progress);
            Assert.That(progress, Is.EqualTo(0f), "el fotograma 0 es la forma del sistema");
            Assert.That(2f * IconMaskShape.Radius(axis, n, 0f), Is.EqualTo(497f).Within(1f), "los ejes: 497 px, como la ráfaga");
            float diag = KnownDiag * BoxPx;
            for (int k = 0; k < 4; k++)
            {
                float a = (Mathf.PI * 0.25f) + (k * Mathf.PI * 0.5f);
                Assert.That(2f * IconMaskShape.Radius(axis, n, a), Is.EqualTo(diag).Within(1f), "las diagonales, a ±1 px");
                Assert.That(2f * IconMaskShape.Radius(axis, n, k * Mathf.PI * 0.5f), Is.EqualTo(497f).Within(1f));
            }
        }

        [Test]
        public void TheTexture_DrawsTheSquircle_WithinOnePixelOnAxesAndDiagonals()
        {
            // La textura de 256 px se estira a la caja: su radio de 127 px es el del círculo de 504 (medio lado). Se busca el borde
            // (opacidad 0,5) en la fila del centro y en la diagonal y se pasa a px de pantalla.
            IconMaskShape shape = IconMaskShape.From(Squircle());
            float rho0 = BoxPx * 0.5f;
            shape.At(rho0, rho0, out float axis, out float n, out _);
            float axisTex = 127f * axis / rho0;
            float screenPerTex = rho0 / 127f;
            int half = BrandIntroMorph.IrisTextureSize / 2;

            float axisEdge = Edge(i => BrandIntroMorph.ShapePixelAlpha(half + i, half, axisTex, n), i => i + 0.5f);
            Assert.That(2f * axisEdge * screenPerTex, Is.EqualTo(497f).Within(1f), "los ejes");
            float diagEdge = Edge(i => BrandIntroMorph.ShapePixelAlpha(half + i, half + i, axisTex, n), i => (i + 0.5f) * Mathf.Sqrt(2f));
            Assert.That(2f * diagEdge * screenPerTex, Is.EqualTo(KnownDiag * BoxPx).Within(1f), "las diagonales");

            // El círculo de siempre sale con la misma cuenta (n = 2, radio 127): coincide con la textura del círculo.
            for (int x = 0; x < 256; x += 5)
            {
                Assert.That(Mathf.Abs(BrandIntroMorph.ShapePixelAlpha(x, 131, 127f, 2f) - BrandIntroMorph.RingPixel(false, x, 131).a), Is.LessThanOrEqualTo(1));
            }
        }

        [Test]
        public void TheOpening_EndsInTheCircle_WithoutAnyPixelJumping()
        {
            // Cada agujero contiene al anterior en todos los ángulos (cada 0,5 grados) y crece poco por paso: ningún píxel de icono
            // pasa a petróleo, y la forma acaba en el círculo antes de que el círculo doble su tamaño.
            IconMaskShape shape = IconMaskShape.From(Squircle());
            float rho0 = BoxPx * 0.5f;
            var prev = new float[91];
            bool reachedCircle = false;
            for (int step = 0; step <= 400; step++)
            {
                float rho = rho0 * (1f + (step / 400f));
                shape.At(rho0, rho, out float axis, out float n, out float progress);
                for (int k = 0; k <= 90; k++)
                {
                    float r = IconMaskShape.Radius(axis, n, k * 0.5f * Mathf.Deg2Rad);
                    if (step > 0)
                    {
                        Assert.That(r, Is.GreaterThanOrEqualTo(prev[k] - 0.01f), "el agujero nunca encoge (" + (k * 0.5f) + " grados, paso " + step + ")");
                        Assert.That(r - prev[k], Is.LessThan(3f), "ni salta");
                    }

                    prev[k] = r;
                }

                if (progress >= 1f)
                {
                    reachedCircle = true;
                    Assert.That(n, Is.EqualTo(2f));
                    Assert.That(axis, Is.EqualTo(rho).Within(1e-3f), "el círculo de la apertura");
                }
            }

            Assert.That(reachedCircle, Is.True);
            shape.At(rho0, rho0 * Mathf.Max(shape.Diag, 1f + IconMaskShape.MinMorphGrowth), out _, out float nEnd, out float pEnd);
            Assert.That(pEnd, Is.EqualTo(1f), "en cuanto el círculo pasa por las esquinas");
            Assert.That(nEnd, Is.EqualTo(2f));
        }

        [Test]
        public void WithoutAMask_OrOutOfRange_ItIsTheCircle()
        {
            Assert.That(IconMaskShape.From(SplashIconBox.None).FromMask, Is.False, "el Editor");
            Assert.That(IconMaskShape.From(new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340)).FromMask, Is.False, "el velo propio: sin máscara");
            Assert.That(IconMaskShape.From(new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340, 5000, 6000, 0)).FromMask, Is.False, "ejes demasiado pequeños");
            Assert.That(IconMaskShape.From(new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340, 9800, 9000, 0)).FromMask, Is.False, "diagonal por dentro de los ejes");
            Assert.That(IconMaskShape.From(new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340, 9800, 14000, 0)).FromMask, Is.False, "más que un cuadrado");
            Assert.That(IconMaskShape.From(new SplashIconBox(0, 0, 0, 0, 0, 0, 0, 0, 9861, 11300, 0)).FromMask, Is.False, "sin caja");
            IconMaskShape circle = IconMaskShape.Circle;
            Assert.That(circle.Name, Is.EqualTo("circle"));
            Assert.That(circle.Exponent, Is.EqualTo(2f));
            circle.At(252f, 252f, out float axis, out float n, out float progress);
            Assert.That(axis, Is.EqualTo(252f));
            Assert.That(n, Is.EqualTo(2f));
            Assert.That(progress, Is.EqualTo(1f));

            // Una máscara redonda (como la de un Pixel) también es el círculo para la sonda.
            IconMaskShape round = IconMaskShape.From(new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340, 10000, 10000, 10000));
            Assert.That(round.FromMask, Is.True);
            Assert.That(round.Name, Is.EqualTo("circle"));
            Assert.That(IconMaskShape.ExponentFor(1f, 1f), Is.EqualTo(2f));
        }

        [Test]
        public void ANearlySquareMask_IsDrawnWithItsCornersOrNotAtAll()
        {
            // Un rectángulo redondeado de esquina pequeña: diagonal / ejes = 1,39. Con el tope de antes (n = 20, hasta ≈ 1,367) se
            // aceptaba y sus esquinas se dibujaban por dentro (icono que pasa a petróleo). Ahora la superelipse llega a ellas.
            float axis = 0.99f;
            float diag = axis * 1.39f;
            IconMaskShape shape = IconMaskShape.From(new SplashIconBox(288, 918, BoxPx, BoxPx, 0, 0, 1080, 2340, Mathf.RoundToInt(axis * 10000f), Mathf.RoundToInt(diag * 10000f), 0));
            Assert.That(shape.FromMask, Is.True);
            Assert.That(shape.Exponent, Is.GreaterThan(20f).And.LessThanOrEqualTo(IconMaskShape.MaxExponent));
            float rho0 = BoxPx * 0.5f;
            shape.At(rho0, rho0, out float a, out float n, out _);
            float drawn = IconMaskShape.Radius(a, n, Mathf.PI * 0.25f);
            Assert.That(drawn, Is.GreaterThanOrEqualTo((shape.Diag * rho0) - 0.01f), "la diagonal dibujada nunca por dentro de la medida");
            Assert.That(2f * drawn, Is.EqualTo(diag * BoxPx).Within(1f));
            Assert.That(2f * IconMaskShape.Radius(a, n, 0f), Is.EqualTo(axis * BoxPx).Within(1f));

            // La textura, con n ≈ 40, sin desbordar: el borde a ±1 px en ejes y diagonales.
            float axisTex = 127f * a / rho0;
            float screenPerTex = rho0 / 127f;
            int half = BrandIntroMorph.IrisTextureSize / 2;
            float axisEdge = Edge(i => BrandIntroMorph.ShapePixelAlpha(half + i, half, axisTex, n), i => i + 0.5f);
            float diagEdge = Edge(i => BrandIntroMorph.ShapePixelAlpha(half + i, half + i, axisTex, n), i => (i + 0.5f) * Mathf.Sqrt(2f));
            Assert.That(2f * axisEdge * screenPerTex, Is.EqualTo(axis * BoxPx).Within(1f), "ejes, en la textura");
            Assert.That(2f * diagEdge * screenPerTex, Is.EqualTo(diag * BoxPx).Within(1f), "diagonales, en la textura");
            Assert.That(BrandIntroMorph.ShapePixelAlpha(half, half, axisTex, n), Is.EqualTo(0), "el centro, agujero");
            Assert.That(BrandIntroMorph.ShapePixelAlpha(0, 0, axisTex, n), Is.EqualTo(255), "la esquina de la textura, petróleo");

            // Más cuadrada de lo que la superelipse dibuja (≥ √2 · 2^(−1/80) ≈ 1,402): el círculo, nunca unas esquinas por dentro.
            foreach (float ratio in new[] { 1.403f, 1.409f })
            {
                IconMaskShape square = IconMaskShape.From(new SplashIconBox(288, 918, BoxPx, BoxPx, 0, 0, 1080, 2340, 9900, Mathf.RoundToInt(9900 * ratio), 0));
                Assert.That(square.FromMask, Is.False, "relación " + ratio);
            }

            Assert.That(IconMaskShape.MaxRatio, Is.EqualTo(Mathf.Sqrt(2f) * Mathf.Pow(2f, -1f / IconMaskShape.MaxExponent)).Within(1e-6f));
        }

        [Test]
        public void TheJavaArray_CarriesTheMaskRadii()
        {
            SplashIconBox b = SplashIconBox.FromArray(new[] { 288, 918, 504, 504, 0, 0, 1080, 2340, 9861, 11727, 10512 });
            Assert.That(b.Measured, Is.True);
            Assert.That(b.MaskAxis, Is.EqualTo(9861));
            Assert.That(b.MaskDiag, Is.EqualTo(11727));
            Assert.That(b.MaskMid, Is.EqualTo(10512));
            Assert.That(SplashIconBox.FromArray(new[] { 288, 918, 504, 504, 0, 0, 1080, 2340 }).MaskAxis, Is.EqualTo(0), "8 enteros: sin máscara");
            Assert.That(b.DiffersFrom(new SplashIconBox(288, 918, 504, 504, 0, 0, 1080, 2340), 1), Is.True, "llegar la máscara cuenta como cambio");
            Assert.That(SplashIconBox.ArrayLength, Is.EqualTo(11));
        }

        // El radio (en las unidades de radiusAt) donde la opacidad cruza 0,5 al alejarse del centro, interpolado entre dos píxeles.
        private static float Edge(System.Func<int, byte> alphaAt, System.Func<int, float> radiusAt)
        {
            for (int i = 1; i < 128; i++)
            {
                float a0 = alphaAt(i - 1) / 255f;
                float a1 = alphaAt(i) / 255f;
                if (a0 < 0.5f && a1 >= 0.5f)
                {
                    float t = (0.5f - a0) / (a1 - a0);
                    return Mathf.Lerp(radiusAt(i - 1), radiusAt(i), t);
                }
            }

            return -1f;
        }
    }
}
