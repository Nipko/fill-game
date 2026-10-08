#nullable enable
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Comprobaciones de color de la dirección visual v2 (D-003; docs/art/direccion-visual-v2.md 2.4, 2.5, 4.2 y 9):
    /// contraste compuesto de las cuias (VA-06, umbrales [P] de P9), adiós al oliva (VA-04c), panel quieto (VA-04),
    /// repisas discretas (VA-04b), distancia de las superficies a los tintes (2.4, regla 1), brillos del líquido por su
    /// resultado (VA-05b) y texto ≥ 4,5:1 (VA-07). Cálculo en CIELAB D65 y WCAG 2.1, como la guía.
    /// </summary>
    public sealed class VisualContrastTests
    {
        // Los cinco tintes del Estuario que usa el tutorial (Assets/_Project/Data/Levels/tutorial_v0.json).
        private static readonly int[] EstuaryTints = { 1, 3, 4, 7, 8 };

        private static readonly int[] OliveSamples = { 0x313D2E, 0x263024, 0x2E392B, 0x333F30, 0x313C2F, 0x323D2F };

        [Test]
        public void DeltaE00_MatchesTheSharmaReferencePairs()
        {
            // Sharma, Wu y Dalal (2005), pares 1 y 17 de la tabla de prueba de CIEDE2000.
            Assert.That(ColorMath.DeltaE00Lab(new Vector3(50f, 2.6772f, -79.7751f), new Vector3(50f, 0f, -82.7485f)), Is.EqualTo(2.0425f).Within(0.001f));
            Assert.That(ColorMath.DeltaE00Lab(new Vector3(50f, 2.5f, 0f), new Vector3(73f, 25f, -18f)), Is.EqualTo(27.1492f).Within(0.001f));
        }

        [Test]
        public void EstuaryTints_StandOutFromTheEmptyCavity_ByAtLeast15LStar()
        {
            // VA-06 (D-003, umbral 1): cada tinte del Estuario frente a la cavidad vacía, ΔL* ≥ 15.
            float cavity = ColorMath.LabLightness(Palette.Panel.Cavity);
            for (int i = 0; i < EstuaryTints.Length; i++)
            {
                float tint = ColorMath.LabLightness(Palette.Tint(EstuaryTints[i]));
                Assert.That(Mathf.Abs(tint - cavity), Is.GreaterThanOrEqualTo(15f), "tinte " + EstuaryTints[i]);
            }
        }

        [Test]
        public void Guanabana_IsAtLeast50LStarAboveTheEmptyCavity()
        {
            // VA-06 (D-003, umbral 3).
            float cavity = ColorMath.LabLightness(Palette.Panel.Cavity);
            Assert.That(ColorMath.LabLightness(Palette.Tint(1)) - cavity, Is.GreaterThanOrEqualTo(50f));
        }

        [Test]
        public void GlassRim_OverThePanel_HasAtLeast3To1()
        {
            // VA-06 (D-003, umbral 2; WCAG 1.4.11): contorno vidrio_borde al 90 % compuesto en lineal frente al panel.
            PanelColors panel = Palette.Panel;
            foreach (Color fill in new[] { panel.Top, panel.Bottom })
            {
                Color rim = ColorMath.CompositeLinear(Palette.GlassRim, 0.90f, fill);
                Assert.That(ColorMath.ContrastRatio(rim, fill), Is.GreaterThanOrEqualTo(3f));
            }
        }

        [Test]
        public void PanelFill_IsDarkQuietAndFarFromTheV01Olive()
        {
            // Panel «azul petróleo de río» (D-004). VA-04: L* entre 15 y 30 y C* ≤ 16. VA-04c: ΔE00 ≥ 12 frente a las muestras del fondo de v0.1.
            PanelColors panel = Palette.Panel;
            foreach (Color fill in new[] { panel.Top, panel.Bottom })
            {
                Vector3 lab = ColorMath.ToLab(fill);
                Assert.That(lab.x, Is.InRange(15f, 30f));
                Assert.That(Mathf.Sqrt((lab.y * lab.y) + (lab.z * lab.z)), Is.LessThanOrEqualTo(16f));
                for (int i = 0; i < OliveSamples.Length; i++)
                {
                    Assert.That(ColorMath.DeltaE00(fill, Palette.Hex(OliveSamples[i])), Is.GreaterThanOrEqualTo(12f), "muestra " + i);
                }
            }
        }

        [Test]
        public void Shelves_AreDiscrete()
        {
            // VA-04b: cara superior L* ≤ 65 y frente ≤ 46.
            Assert.That(ColorMath.LabLightness(Palette.ShelfTop), Is.LessThanOrEqualTo(65f));
            Assert.That(ColorMath.LabLightness(Palette.ShelfFront), Is.LessThanOrEqualTo(46.5f));
            Assert.That(ColorMath.LabLightness(Palette.ShelfFrontLow), Is.LessThanOrEqualTo(46f));
        }

        [Test]
        public void SurfacesAroundTheCuias_AreAtLeast10DeltaE00FromEveryTint()
        {
            // Guía 2.4, regla 1: panel, cavidad, marco y repisas a ΔE00 ≥ 10 de los 14 tintes (el marco es el mínimo, 10,2).
            Color[] surfaces =
            {
                Palette.Panel.Top, Palette.Panel.Bottom, Palette.Panel.Cavity, Palette.Frame, Palette.ShelfTop,
                Palette.ShelfFront, Palette.ShelfFrontLow,
            };
            for (int s = 0; s < surfaces.Length; s++)
            {
                for (int t = 1; t <= 14; t++)
                {
                    Assert.That(ColorMath.DeltaE00(surfaces[s], Palette.Tint(t)), Is.GreaterThanOrEqualTo(10f), "superficie " + s + " frente al tinte " + t);
                }
            }
        }

        [TestCase(8)] // Açaí
        [TestCase(6)] // Camu camu
        public void LiquidHighlight_OnDarkTints_AddsAt12LStar_AndStaysClose(int tint)
        {
            // VA-05b (guía 4.2): brillo opaco precalculado, ΔL* +12 en los tintes de L* < 40 y ΔE00 ≤ 20 frente al HEX.
            Color dose = Palette.Tint(tint);
            Color lit = Palette.LiquidHighlight(tint, dose);
            float dl = ColorMath.LabLightness(lit) - ColorMath.LabLightness(dose);
            Assert.That(dl, Is.InRange(11f, 12.5f));
            Assert.That(ColorMath.DeltaE00(lit, dose), Is.LessThanOrEqualTo(20f));
        }

        [Test]
        public void Guanabana_HasNoHighlightStripe()
        {
            Color dose = Palette.Tint(1);
            Assert.That(Palette.HighlightDeltaL(1, dose), Is.EqualTo(0f));
            Assert.That(Palette.LiquidHighlight(1, dose), Is.EqualTo(dose));
        }

        [Test]
        public void UiText_MeetsFourAndAHalfToOne()
        {
            // VA-07 (guía 2.4, regla 2): texto ≥ 4,5:1 contra el relleno de la franja del 25-85 %; glifos ≥ 3:1.
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyLeaf.Base), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyLeaf.Deep), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWater.Base), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWater.Deep), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWood.Base), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.WoodDark), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Ink, Palette.PanelLightLow), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.FamilyWood.Base, Palette.PanelLightLow), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.DisabledText, Palette.Disabled), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.DisabledGlyph, Palette.Disabled), Is.GreaterThanOrEqualTo(3f));

            // Texto de botón: a partir del 25 % de la cara el degradado horneado ya da ≥ 4,5:1 (2.4).
            for (float t = 0.25f; t <= 1.0001f; t += 0.05f)
            {
                Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyLeaf.FaceAt(t)), Is.GreaterThanOrEqualTo(4.5f), "hoja al " + t);
                Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWater.FaceAt(t)), Is.GreaterThanOrEqualTo(4.5f), "agua al " + t);
            }
        }

        [Test]
        public void PanelLightPool_AddsThreeToFourLStar()
        {
            // Guía 4.6: charco de luz de +3 a +4 L* en su centro (alfa lineal ≈ 0,016 de cielo_horizonte).
            Color pooled = ColorMath.CompositeLinear(Palette.SkyHorizon, 0.016f, Palette.Panel.Top);
            float dl = ColorMath.LabLightness(pooled) - ColorMath.LabLightness(Palette.Panel.Top);
            Assert.That(dl, Is.InRange(2.5f, 4.5f));
        }
    }
}
