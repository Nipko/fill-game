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
            foreach (Color c in new[]
            {
                ProceduralArt.SlothFur, ProceduralArt.CapiFur, ProceduralArt.RosaPink, ProceduralArt.MorphoFlightBlue,
                ProceduralArt.BufeoBody, ProceduralArt.PiranaFishSilver,
            })
            {
                ToyTones t = ToyShading.Tones(c);
                Color outline = Color.Lerp(Palette.JungleLight, t.Outline, t.Outline.a); // con su alfa real (0,92) sobre el fondo
                outline.a = 1f;
                Assert.That(t.Outline.a, Is.EqualTo(ToyShading.OutlineAlpha));
                Assert.That(ColorMath.ContrastRatio(outline, Palette.JungleLight), Is.GreaterThanOrEqualTo(3f), "contorno sobre la selva clara: " + c);
                Assert.That(ColorMath.ContrastRatio(t.Mid, Palette.Panel.Top), Is.GreaterThanOrEqualTo(2f), "material sobre el panel: " + c);
            }
        }

        [Test]
        public void ShadeCore_UsesTheSameLights_AsTheVectors()
        {
            // ShadeCore lleva las luces como constantes (se llama por píxel): las mismas que KeyLight, Half, RimFrom y los colores.
            Assert.That(ToyShading.KeyX, Is.EqualTo(ToyShading.KeyLight.x).Within(1e-6f));
            Assert.That(ToyShading.KeyY, Is.EqualTo(ToyShading.KeyLight.y).Within(1e-6f));
            Assert.That(ToyShading.KeyZ, Is.EqualTo(ToyShading.KeyLight.z).Within(1e-6f));
            Assert.That(ToyShading.HalfX, Is.EqualTo(ToyShading.Half.x).Within(1e-6f));
            Assert.That(ToyShading.HalfY, Is.EqualTo(ToyShading.Half.y).Within(1e-6f));
            Assert.That(ToyShading.HalfZ, Is.EqualTo(ToyShading.Half.z).Within(1e-6f));
            Assert.That(ToyShading.RimX, Is.EqualTo(ToyShading.RimFrom.x).Within(1e-6f));
            Assert.That(ToyShading.RimY, Is.EqualTo(ToyShading.RimFrom.y).Within(1e-6f));
            Assert.That(new Color(ToyShading.RimWarmR, ToyShading.RimWarmG, ToyShading.RimWarmB), Is.EqualTo(ToyShading.RimWarm));
            Assert.That(new Color(ToyShading.BounceR, ToyShading.BounceG, ToyShading.BounceB), Is.EqualTo(ToyShading.BounceCool));

            // Y ShadeCore es la cuenta de la guía §2-§3 hecha con las funciones públicas (Diffuse, ThreeTone, Rim, VarnishSharp y
            // VarnishBroad), en normales de todo el hemisferio, con y sin oclusión ni barniz.
            ToyTones t = ToyShading.Tones(Fur);
            for (float ny = -1.1f; ny <= 1.1f; ny += 0.137f)
            {
                for (float nx = -1.1f; nx <= 1.1f; nx += 0.151f)
                {
                    foreach (float ao in new[] { 0f, 0.3f, 1f })
                    {
                        foreach (float gloss in new[] { 0f, 0.6f, 1f })
                        {
                            Vector3 n = ToyShading.Normal(nx, ny);
                            Vector3 g = ToyShading.Normal(ny * 0.5f, nx * 0.5f);
                            Color expected = Reference(t, n, ao, gloss, g);
                            ToyShading.ShadeCore(t, n.x, n.y, n.z, ao, gloss, g.x, g.y, g.z, out float r, out float gg, out float b);
                            string what = "n (" + nx + ", " + ny + "), ao " + ao + ", barniz " + gloss;
                            Assert.That(r, Is.EqualTo(expected.r).Within(2e-5f), what);
                            Assert.That(gg, Is.EqualTo(expected.g).Within(2e-5f), what);
                            Assert.That(b, Is.EqualTo(expected.b).Within(2e-5f), what);
                        }
                    }
                }
            }

            Color c = ToyShading.Shade(t, ToyShading.Normal(-0.3f, 0.4f), 0.25f, 0.6f);
            Assert.That(c.a, Is.EqualTo(1f), "opaco");
        }

        // El sombreado de juguete escrito con las funciones públicas, paso a paso (la referencia de ShadeCore).
        private static Color Reference(in ToyTones t, Vector3 n, float ao, float gloss, Vector3 g)
        {
            float d = ToyShading.Diffuse(n) * (1f - (0.35f * ao));
            Color c = ToyShading.ThreeTone(t, d);
            c = Color.Lerp(c, ToyShading.BounceCool, Mathf.Clamp01(-n.y) * (1f - d) * 0.2f);
            c = Color.Lerp(c, Color.Lerp(t.Light, ToyShading.RimWarm, 0.55f), ToyShading.Rim(n) * 0.55f * (1f - ao));
            float dark = 1f - (0.5f * ao);
            c.r *= dark;
            c.g *= dark;
            c.b *= dark;
            float s = Mathf.Clamp01(gloss * ((0.85f * ToyShading.VarnishSharp(g)) + (0.6f * ToyShading.VarnishBroad(g))) * (1f - ao));
            return new Color(c.r + ((1f - c.r) * s), c.g + ((1f - c.g) * s), c.b + ((1f - c.b) * s), 1f);
        }

        [Test]
        public void CreaseAO_AndContactShadow_SurviveAZeroRadius()
        {
            // Guardas de división (revisión del piloto): un radio 0 no da NaN ni infinito.
            float ao = ToyShading.CreaseAO(0.1f, -0.1f, 0f);
            float shadow = ToyShading.ContactShadow(1f, 1f, 0f, 0f);
            Assert.That(float.IsNaN(ao) || float.IsInfinity(ao), Is.False);
            Assert.That(float.IsNaN(shadow) || float.IsInfinity(shadow), Is.False);
            Assert.That(ao, Is.InRange(0f, 1f));
            Assert.That(shadow, Is.InRange(0f, 1f));
        }

        [Test]
        public void ToyEllipse_HasTheThinColoredEdge_TheLightUpperLeft_AndTheCrease()
        {
            // Una pieza de juguete sobre un fondo opaco: el borde del color oscuro de la pieza en el canto, el relleno más claro
            // arriba a la izquierda que abajo a la derecha, y la oclusión oscurece lo de alrededor, no lo lejano.
            var pc = new PixelCanvas(64, 64);
            Color under = new Color(0.6f, 0.6f, 0.6f, 1f);
            pc.Fill(under);
            ToyTones t = ToyShading.Tones(Fur);
            ProceduralArt.ToyEllipse(pc, 32f, 32f, 20f, 20f, t, 0.5f, 1.5f, 4f);
            Color edge = pc.Get(32, 32 + 20); // en el canto de arriba (y hacia arriba)
            Assert.That(ColorMath.LabLightness(edge), Is.LessThan(ColorMath.LabLightness(t.Mid)), "el borde, oscuro");
            Color lit = pc.Get(32 - 9, 32 + 9);
            Color dark = pc.Get(32 + 12, 32 - 12);
            Assert.That(ColorMath.LabLightness(lit), Is.GreaterThan(ColorMath.LabLightness(dark)), "luz arriba a la izquierda");
            Color near = pc.Get(32 + 23, 32);
            Color far = pc.Get(2, 2);
            Assert.That(near.r, Is.LessThan(under.r), "la oclusión alrededor de la pieza");
            Assert.That(far, Is.EqualTo(under), "lejos, nada");
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    Color c = pc.Get(x, y);
                    Assert.That(float.IsNaN(c.r) || float.IsNaN(c.a), Is.False);
                }
            }

            pc.Release();
        }

        [Test]
        public void ToyCapsule_IsACylinder_LitTheSameAlongItsLength()
        {
            // Un cilindro: a lo largo del eje, el mismo tono; de un lado al otro, de la luz a la sombra.
            var pc = new PixelCanvas(48, 96);
            ToyTones t = ToyShading.Tones(Fur);
            ProceduralArt.ToyCapsule(pc, 24f, 20f, 24f, 76f, 12f, t, 0f, 1f);
            Color a = pc.Get(20, 40);
            Color b = pc.Get(20, 56);
            Assert.That(Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b), Is.LessThan(1e-4f), "a lo largo, igual");
            Assert.That(ColorMath.LabLightness(pc.Get(17, 48)), Is.GreaterThan(ColorMath.LabLightness(pc.Get(32, 48))), "la luz, a la izquierda");
            Assert.That(pc.Get(24, 2).a, Is.EqualTo(0f), "fuera, transparente");
            pc.Release();
        }

        [Test]
        public void TheLut_MatchesTheFullShading_AndIsCachedPerMaterial()
        {
            // La tabla de una pieza grande da casi lo mismo que la cuenta completa (media < 0,01 por canal y máximo < 0,08, en el
            // canto del brillo nítido); las piezas pequeñas sin tabla en caché van por la cuenta completa.
            ToyTones t = ToyShading.Tones(ProceduralArt.RosaPink);
            ToyLut lut = ToyLut.For(t, 0.9f, ToyLut.MinPixels)!;
            Assert.That(ToyLut.For(t, 0.9f, 0f), Is.SameAs(lut), "en caché");
            Assert.That(ToyLut.For(ToyShading.Tones(ProceduralArt.CapiFur), 0.37f, 10f), Is.Null, "pequeña y sin tabla: la cuenta completa");
            double sum = 0;
            float worst = 0f;
            int n = 0;
            for (float ny = -1.05f; ny <= 1.05f; ny += 0.037f)
            {
                for (float nx = -1.05f; nx <= 1.05f; nx += 0.041f)
                {
                    Color exact = ProceduralArt.ToyColor(t, 0.9f, null, nx, ny);
                    Color table = ProceduralArt.ToyColor(t, 0.9f, lut, nx, ny);
                    float d = Mathf.Max(Mathf.Abs(exact.r - table.r), Mathf.Max(Mathf.Abs(exact.g - table.g), Mathf.Abs(exact.b - table.b)));
                    sum += d;
                    worst = Mathf.Max(worst, d);
                    n++;
                }
            }

            Assert.That(sum / n, Is.LessThan(0.01));
            Assert.That(worst, Is.LessThan(0.08f));
        }
    }
}
