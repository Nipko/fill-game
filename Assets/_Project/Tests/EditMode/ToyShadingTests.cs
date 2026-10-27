#nullable enable
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El sombreado de juguete de los personajes (D-058; <see cref="ToyShading"/>; guía <c>docs/art/estilo-personajes-3d.md</c>):
    /// funciones puras, deterministas y con la luz arriba a la izquierda.
    /// </summary>
    public sealed class ToyShadingTests
    {
        private static readonly Color Fur = new Color(0x9A / 255f, 0x8F / 255f, 0x7A / 255f, 1f);

        [Test]
        public void KeyLight_ComesFromTheUpperLeft_AndFromTheFront()
        {
            Assert.That(ToyShading.KeyLight.x, Is.LessThan(0f));
            Assert.That(ToyShading.KeyLight.y, Is.GreaterThan(0f));
            Assert.That(ToyShading.KeyLight.z, Is.GreaterThan(0f));
            Assert.That(ToyShading.KeyLight.magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Tones_GoFromDarkToLight_AndTheOutlineIsTheDarkest()
        {
            ToyTones t = ToyShading.Tones(Fur);
            float shadow = ColorMath.LabLightness(t.Shadow);
            float mid = ColorMath.LabLightness(t.Mid);
            float light = ColorMath.LabLightness(t.Light);
            float outline = ColorMath.LabLightness(t.Outline);
            Assert.That(shadow, Is.LessThan(mid - 10f), "la sombra, ≥ 10 puntos de L* bajo el medio");
            Assert.That(light, Is.GreaterThan(mid + 4f), "la luz, por encima del medio");
            Assert.That(outline, Is.LessThan(shadow), "el contorno, más oscuro que la sombra");
            Assert.That(t.Outline.a, Is.EqualTo(ToyShading.OutlineAlpha));
        }

        [Test]
        public void Tones_TheShadowIsCoolerAndTheLightWarmer()
        {
            ToyTones t = ToyShading.Tones(Fur);
            Vector3 mid = ColorMath.ToLab(t.Mid);
            Vector3 shadow = ColorMath.ToLab(t.Shadow);
            Vector3 light = ColorMath.ToLab(t.Light);
            Assert.That(shadow.z, Is.LessThan(mid.z), "la sombra tira al azul (b* menor)");
            Assert.That(light.z, Is.GreaterThanOrEqualTo(mid.z - 0.5f), "la luz no se enfría");
        }

        [Test]
        public void Diffuse_IsHighestFacingTheLight_AndNeverBlack()
        {
            float toward = ToyShading.Diffuse(ToyShading.KeyLight);
            float front = ToyShading.Diffuse(new Vector3(0f, 0f, 1f));
            float away = ToyShading.Diffuse(-ToyShading.KeyLight);
            Assert.That(toward, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(front, Is.LessThan(toward).And.GreaterThan(away));
            Assert.That(away, Is.EqualTo(0f).Within(1e-4f));
            Vector3 side = new Vector3(0.6f, -0.8f, 0f);
            Assert.That(ToyShading.Diffuse(side), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ThreeTone_HitsTheThreeTones_AtTheEnds_AndIsMonotonic()
        {
            ToyTones t = ToyShading.Tones(Fur);
            Assert.That(ToyShading.ThreeTone(t, 0f), Is.EqualTo(t.Shadow));
            Assert.That(ToyShading.ThreeTone(t, 0.5f), Is.EqualTo(t.Mid));
            Assert.That(ToyShading.ThreeTone(t, 1f), Is.EqualTo(t.Light));
            float last = -1f;
            for (int i = 0; i <= 20; i++)
            {
                float l = ColorMath.LabLightness(ToyShading.ThreeTone(t, i / 20f));
                Assert.That(l, Is.GreaterThanOrEqualTo(last - 1e-3f), "L* no baja al subir la luz (" + i + ")");
                last = l;
            }
        }

        [Test]
        public void Varnish_HasASharpSmallSpot_AndABroadSoftOne()
        {
            Vector3 half = (ToyShading.KeyLight + new Vector3(0f, 0f, 1f)).normalized;
            Assert.That(ToyShading.VarnishSharp(half), Is.EqualTo(1f).Within(1e-4f), "el brillo nítido, en el semivector");
            Assert.That(ToyShading.VarnishBroad(half), Is.EqualTo(0.5f).Within(1e-4f));
            Vector3 off = ToyShading.Normal(0.3f, -0.3f);
            Assert.That(ToyShading.VarnishSharp(off), Is.EqualTo(0f), "el nítido es pequeño");
            Assert.That(ToyShading.VarnishBroad(off), Is.GreaterThan(0f), "el ancho llega más lejos");
        }

        [Test]
        public void Rim_IsOnTheEdges_FacingTheBackLight()
        {
            Assert.That(ToyShading.Rim(new Vector3(0f, 0f, 1f)), Is.EqualTo(0f), "de frente, sin luz de borde");
            float upRight = ToyShading.Rim(new Vector3(ToyShading.RimFrom.x, ToyShading.RimFrom.y, 0f));
            float downLeft = ToyShading.Rim(new Vector3(-ToyShading.RimFrom.x, -ToyShading.RimFrom.y, 0f));
            Assert.That(upRight, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(downLeft, Is.LessThan(upRight));
        }

        [Test]
        public void CreaseAO_IsOnlyWhereTwoSurfacesMeet()
        {
            Assert.That(ToyShading.CreaseAO(0f, 0f, 1f), Is.EqualTo(1f).Within(1e-4f), "en la unión");
            Assert.That(ToyShading.CreaseAO(-3f, 2f, 1f), Is.EqualTo(0f), "dentro de una pieza, lejos de la otra");
            Assert.That(ToyShading.CreaseAO(2f, 2f, 1f), Is.EqualTo(0f), "fuera de las dos");
            Assert.That(ToyShading.CreaseAO(0.2f, -0.2f, 1f), Is.GreaterThan(ToyShading.CreaseAO(0.8f, -0.8f, 1f)));
        }

        [Test]
        public void ContactShadow_IsDarkUnderneath_AndFadesOut()
        {
            Assert.That(ToyShading.ContactShadow(0f, 0f, 10f, 2f), Is.EqualTo(1f));
            Assert.That(ToyShading.ContactShadow(5f, 0f, 10f, 2f), Is.InRange(0.01f, 0.99f));
            Assert.That(ToyShading.ContactShadow(11f, 0f, 10f, 2f), Is.EqualTo(0f));
        }

        [Test]
        public void EdgeDome_LooksSidewaysOnTheEdge_AndFrontOnInside()
        {
            Vector3 edge = ToyShading.EdgeDome(Vector2.right, 0f, 10f);
            Vector3 inside = ToyShading.EdgeDome(Vector2.right, 20f, 10f);
            Assert.That(edge.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(inside.z, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(ToyShading.EdgeDome(Vector2.right, 5f, 10f).magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void FacingLight_IsSymmetric_SoARotatingLimbHasNoLitSide()
        {
            Assert.That(ToyShading.Diffuse(ToyShading.FacingLight(0f)), Is.EqualTo(1f).Within(1e-4f));
            for (int i = 1; i <= 10; i++)
            {
                float a = i / 10f;
                Assert.That(ToyShading.Diffuse(ToyShading.FacingLight(a)), Is.EqualTo(ToyShading.Diffuse(ToyShading.FacingLight(-a))).Within(1e-4f));
            }
        }

        [Test]
        public void Shade_AoDarkens_AndGlossBrightens_AndIsOpaque()
        {
            ToyTones t = ToyShading.Tones(Fur);
            Vector3 n = ToyShading.Normal(-0.2f, 0.3f);
            Color plain = ToyShading.Shade(t, n, 0f, 0f);
            Color occluded = ToyShading.Shade(t, n, 1f, 0f);
            Color glossy = ToyShading.Shade(t, n, 0f, 1f);
            Assert.That(ColorMath.LabLightness(occluded), Is.LessThan(ColorMath.LabLightness(plain)));
            Assert.That(ColorMath.LabLightness(glossy), Is.GreaterThan(ColorMath.LabLightness(plain)));
            Assert.That(plain.a, Is.EqualTo(1f));
            Assert.That(ToyShading.Shade(t, n, 0.3f, 0.5f), Is.EqualTo(ToyShading.Shade(t, n, 0.3f, 0.5f)), "determinista");
        }

        [Test]
        public void Shade_TheGlossNormalMovesOnlyTheVarnish()
        {
            ToyTones t = ToyShading.Tones(Fur);
            Vector3 half = (ToyShading.KeyLight + new Vector3(0f, 0f, 1f)).normalized;
            Vector3 n = ToyShading.Normal(0.4f, -0.4f);
            Assert.That(ToyShading.Shade(t, n, 0f, 0f, half), Is.EqualTo(ToyShading.Shade(t, n, 0f, 0f)), "sin barniz, la misma");
            Assert.That(ColorMath.LabLightness(ToyShading.Shade(t, n, 0f, 1f, half)), Is.GreaterThan(ColorMath.LabLightness(ToyShading.Shade(t, n, 0f, 1f))));
        }

        [Test]
        public void Iris_IsDarkInThePupil_LightestLowRight_AndDarkOnTheRing()
        {
            Color iris = new Color(0x6A / 255f, 0x47 / 255f, 0x30 / 255f, 1f);
            float pupil = ColorMath.LabLightness(ToyShading.Iris(0f, 0f, iris));
            float low = ColorMath.LabLightness(ToyShading.Iris(0.4f, -0.55f, iris));
            float high = ColorMath.LabLightness(ToyShading.Iris(-0.3f, 0.6f, iris));
            float ring = ColorMath.LabLightness(ToyShading.Iris(0f, -0.99f, iris));
            Assert.That(pupil, Is.LessThan(high));
            Assert.That(low, Is.GreaterThan(high), "más claro abajo a la derecha (la luz que entra)");
            Assert.That(ring, Is.LessThan(low));
        }

        [Test]
        public void PlushClumps_AreRoundBumps_DeterministicAndBounded()
        {
            float max = 0f;
            float min = 1f;
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    float h = ToyShading.PlushClumps(x * 0.5f, y * 0.5f, 4f, 17, out float sx, out float sy);
                    float h2 = ToyShading.PlushClumps(x * 0.5f, y * 0.5f, 4f, 17, out float sx2, out float sy2);
                    Assert.That(h, Is.EqualTo(h2));
                    Assert.That(sx, Is.EqualTo(sx2));
                    Assert.That(sy, Is.EqualTo(sy2));
                    Assert.That(h, Is.InRange(0f, 1f));
                    max = Mathf.Max(max, h);
                    min = Mathf.Min(min, h);
                }
            }

            Assert.That(max, Is.GreaterThan(0.8f), "lo alto de los mechones");
            Assert.That(min, Is.LessThan(max - 0.2f), "y sus surcos");
        }

        [Test]
        public void Outline_StaysVisible_OnTheLightJungle_AndOnThePanel()
        {
            // Alto contraste no cambia a los personajes [P]: sobre la selva clara los separa el contorno fino (≥ 3:1, el mínimo de
            // los glifos) y sobre el panel azul petróleo, el propio material (≥ 2:1).
            foreach (Color c in new[] { ProceduralArt.SlothFur, ProceduralArt.CapiFur, ProceduralArt.RosaPink, ProceduralArt.MorphoFlightBlue })
            {
                ToyTones t = ToyShading.Tones(c);
                Color outline = t.Outline;
                outline.a = 1f;
                Assert.That(ColorMath.ContrastRatio(outline, Palette.JungleLight), Is.GreaterThanOrEqualTo(3f), "contorno sobre la selva clara: " + c);
                Assert.That(ColorMath.ContrastRatio(t.Mid, Palette.Panel.Top), Is.GreaterThanOrEqualTo(2f), "material sobre el panel: " + c);
            }
        }
    }
}
