#nullable enable
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Fondo de la parada (tarea M1.4; D-013, punto 9; 07 §2.1; propuesta del Mapa §10.2-§10.6): el héroe de cada parada (la
    /// silueta de respaldo por n, las piezas por código y las del kit), la compuerta del provisional (<see cref="MapArtRule"/>:
    /// sin silueta en release, el PNG final manda siempre), VA-23 (la banda del primer plano con el agua base del mundo en las 20
    /// paradas), VA-03 (el cielo tras la luz de la parada), los altos (≤ 48 dp sobre el horizonte y el reflejo ≤ 44 dp), la luz
    /// solo en <c>dusk</c> y <c>dawn</c>, el fundido y el corte, que nada reciba toques, el coste de componer y que
    /// <see cref="StopBackdrop.Tick"/> no asigne memoria.
    /// </summary>
    public sealed class StopBackdropTests
    {
        // Tablero de 2 filas a 360 × 780 (nivel 7: panel en 162,1, horizonte en 118; direccion-visual-v2 §3.3) y la viga del muelle.
        private const float Horizon2 = 118f;
        private const float PanelTop2 = 162.1f;
        private const float BandBottom2 = 560f;

        private GameObject _canvas = null!;
        private StopBackdrop _b = null!;
        private Texture2D? _pngTexture;
        private Sprite? _png;

        private static RiverStopCatalog Stops => MapTestCatalogs.Real;

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = null;
            StopBackdrop.HeroSpriteOverride = _ => null; // el arte procedural, aunque el catálogo de ranuras traiga PNG
            _canvas = new GameObject("StopBackdropTestCanvas", typeof(RectTransform), typeof(Canvas));
            var scene = new GameObject("Scene", typeof(RectTransform));
            scene.transform.SetParent(_canvas.transform, false);
            _b = new StopBackdrop((RectTransform)scene.transform, null);
            _b.Layout(360f, Horizon2, PanelTop2, true, BandBottom2);
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            StopBackdrop.HeroSpriteOverride = null;
            Object.DestroyImmediate(_canvas);
            if (_png != null)
            {
                Object.DestroyImmediate(_png);
            }

            if (_pngTexture != null)
            {
                Object.DestroyImmediate(_pngTexture);
            }
        }

        private static string WaterOf(int world)
        {
            return world == 2 ? "clear" : (world == 3 ? "black" : "white");
        }

        private void Show(int n)
        {
            RiverStop stop = Stops.At(n)!;
            _b.Apply(stop, WaterOf(stop.World), false, 0f);
        }

        [Test]
        public void ShapeOf_GivesTheFallbackOfEachStop()
        {
            string?[] expected =
            {
                StopShapes.TwoSpires, null, StopShapes.GenericNature, StopShapes.GenericNature, null, StopShapes.Dome,
                StopShapes.GenericWater, StopShapes.GenericWater, StopShapes.GenericCity, StopShapes.GenericCity,
                StopShapes.GenericNature, StopShapes.GenericWater, StopShapes.GenericWater, StopShapes.Beacon, StopShapes.GenericCity,
                StopShapes.Rafts, StopShapes.GenericNature, StopShapes.GenericCity, StopShapes.GenericWater, StopShapes.GenericWater,
            };
            Assert.That(Stops.Count, Is.EqualTo(expected.Length));
            for (int n = 0; n < Stops.Count; n++)
            {
                Assert.That(StopShapes.ShapeOf(Stops.At(n)!.Backdrop), Is.EqualTo(expected[n]), "parada " + n);
            }

            Assert.That(StopShapes.ShapeOf(null), Is.Null);
            Assert.That(StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w04", "bg_hero_nuevo", null, null)), Is.EqualTo(StopShapes.GenericNature));
            Assert.That(StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w04", null, "kit:otra_pieza", null)), Is.EqualTo(StopShapes.GenericWater));
            Assert.That(StopShapes.All.Length, Is.EqualTo(7));
            foreach (string shape in StopShapes.All)
            {
                Assert.That(StopShapes.IsKnown(shape), Is.True, shape);
                Assert.That(StopShapes.PrimitiveCount(shape), Is.GreaterThan(0), shape);
            }
        }

        [Test]
        public void Acceptance_TwoSpiresInStop0_LinesIn1And4_DomeIn5_BeaconIn13_RaftsIn15()
        {
            Assert.That(MapArtRule.ShowProvisional, Is.True, "los tests corren en el Editor");
            Show(0);
            Assert.That(_b.HeroSource, Is.EqualTo(StopBackdrop.HeroShape));
            Assert.That(_b.Shape, Is.EqualTo(StopShapes.TwoSpires), "1-10: dos agujas");
            Assert.That(_b.ShapeGraphic.IsEmpty, Is.False);

            Show(1);
            Assert.That(_b.Code, Is.EqualTo(StopBackdrop.CodeClearWhite), "11: la línea clara y blanca");
            Assert.That(_b.HeroSource, Is.EqualTo(StopBackdrop.HeroNone), "el héroe de Santarém es la pieza por código");
            Assert.That(_b.CodeGraphic.IsEmpty, Is.False);
            Assert.That(_b.ShapeGraphic.IsEmpty, Is.True);

            Show(4);
            Assert.That(_b.Code, Is.EqualTo(StopBackdrop.CodeBlackWhite), "41: la diagonal");
            Assert.That(_b.CodeGraphic.IsEmpty, Is.False);

            Show(5);
            Assert.That(_b.Shape, Is.EqualTo(StopShapes.Dome), "51: el domo");
            Show(13);
            Assert.That(_b.Shape, Is.EqualTo(StopShapes.Beacon), "131: la baliza (pieza del kit, provisional)");
            Assert.That(_b.Code, Is.EqualTo("kit:rio_ancho_con_baliza"));
            Assert.That(_b.CodeGraphic.IsEmpty, Is.True, "una pieza del kit no es una línea entre aguas");
            Show(15);
            Assert.That(_b.Shape, Is.EqualTo(StopShapes.Rafts), "151: las balsas");
        }

        [Test]
        public void AsInRelease_NoSilhouette_ButTheCodePiecesAndTheWater()
        {
            MapArtRule.ForceProvisional = false;
            for (int n = 0; n < Stops.Count; n++)
            {
                Show(n);
                Assert.That(_b.HeroSource, Is.EqualTo(StopBackdrop.HeroNone), "parada " + n + ": sin PNG ni provisional");
                Assert.That(_b.Shape, Is.Null);
                Assert.That(_b.ShapeGraphic.IsEmpty, Is.True, "parada " + n);
                Assert.That(_b.HeroRect.width, Is.EqualTo(0f), "sin héroe, HeroRect vacío");
                Assert.That(_b.BandGraphic.IsEmpty, Is.False, "la banda de agua sale siempre");
                bool lines = n == 1 || n == 4;
                Assert.That(_b.CodeGraphic.IsEmpty, Is.EqualTo(!lines), "parada " + n + ": las líneas entre aguas ya son definitivas");
            }
        }

        [Test]
        public void ThePngOfTheSlot_WinsEvenInRelease_WithItsReflection()
        {
            MapArtRule.ForceProvisional = false;
            _pngTexture = new Texture2D(720, 240, TextureFormat.RGBA32, false);
            _png = Sprite.Create(_pngTexture, new Rect(0f, 0f, 720f, 240f), new Vector2(0.5f, 0.5f), 300f);
            string? asked = null;
            StopBackdrop.HeroSpriteOverride = slot =>
            {
                asked = slot;
                return slot == "bg_hero_teatro_amazonas" ? _png : null;
            };

            Show(5);
            Assert.That(asked, Is.EqualTo("bg_hero_teatro_amazonas"));
            Assert.That(_b.HeroSource, Is.EqualTo(StopBackdrop.HeroPng));
            Assert.That(_b.ShapeGraphic.IsEmpty, Is.True, "con el PNG no se pinta la silueta");
            Assert.That(_b.HeroImage.gameObject.activeSelf, Is.True);
            Assert.That(_b.HeroImage.sprite, Is.SameAs(_png));
            Assert.That(_b.HeroImage.raycastTarget, Is.False);
            var hero = (RectTransform)_b.HeroImage.transform;
            Assert.That(hero.sizeDelta, Is.EqualTo(new Vector2(240f, 80f)));
            Assert.That(-hero.anchoredPosition.y, Is.EqualTo(Horizon2 - 48f).Within(0.001f), "48 dp sobre el horizonte");
            Assert.That(_b.HeroRect, Is.EqualTo(new Rect(60f, Horizon2 - 48f, 240f, 80f)));
            Assert.That(_b.HeroReflectionClip.gameObject.activeSelf, Is.True);
            Assert.That(_b.HeroReflectionClip.sizeDelta.y, Is.EqualTo(44f), "recortado a 44 dp");
            Assert.That(-_b.HeroReflectionClip.anchoredPosition.y, Is.EqualTo(Horizon2).Within(0.001f), "desde el horizonte");
            Assert.That(_b.HeroReflectionClip.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(_b.HeroReflection.color.a, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(_b.HeroReflection.transform.localScale.y, Is.EqualTo(-1f), "volteado");
            Assert.That(_b.HeroReflection.raycastTarget, Is.False);

            Show(4);
            Assert.That(_b.HeroImage.gameObject.activeSelf, Is.False, "el Encuentro no tiene PNG de héroe");
        }

        [Test]
        public void VA23_TheForegroundBandIsTheWorldPreset_InAllTwentyStops()
        {
            foreach ((float horizon, float panelTop) in new[] { (Horizon2, PanelTop2), (162.9f, 226.9f) })
            {
                _b.Layout(360f, horizon, panelTop, true, BandBottom2);
                for (int n = 0; n < Stops.Count; n++)
                {
                    Show(n);
                    Color preset = Palette.WaterBase(WaterOf(Stops.At(n)!.World));
                    StopShapeGraphic band = _b.BandGraphic;
                    int opaque = 0;
                    for (int i = 0; i < band.VertexCount; i++)
                    {
                        Vector2 p = band.PositionAt(i);
                        if (p.y < horizon + StopBackdrop.BandFadeDp)
                        {
                            continue; // la franja de junto al horizonte es la lejanía, con el reflejo del cielo
                        }

                        Color c = band.ColorAt(i);
                        Assert.That(ColorMath.DeltaE00(c, preset), Is.LessThanOrEqualTo(5f), "parada " + n + ", vértice " + i);
                        if (c.a > 0.999f)
                        {
                            opaque++;
                        }
                    }

                    Assert.That(opaque, Is.GreaterThanOrEqualTo(4), "parada " + n + ": la banda opaca");
                    Assert.That(band.MaxY, Is.GreaterThanOrEqualTo(BandBottom2 - 0.01f), "hasta la viga del muelle");

                    // Las líneas entre aguas quedan por encima del primer plano: en los 12 dp sobre el panel solo hay preset.
                    if (!_b.CodeGraphic.IsEmpty)
                    {
                        Assert.That(_b.CodeGraphic.MaxY, Is.LessThanOrEqualTo(panelTop - 12f), "parada " + n);
                    }
                }
            }
        }

        [Test]
        public void VA21_NothingContrastyInTheSixteenDpAboveThePanel()
        {
            for (int n = 0; n < Stops.Count; n++)
            {
                Show(n);
                Color preset = _b.BandColor;
                float presetL = ColorMath.LabLightness(preset);
                foreach (StopShapeGraphic g in new[] { _b.BandGraphic, _b.CodeGraphic, _b.ShapeGraphic })
                {
                    for (int i = 0; i < g.VertexCount; i++)
                    {
                        Vector2 p = g.PositionAt(i);
                        if (p.y < PanelTop2 - 16f || p.y > PanelTop2)
                        {
                            continue;
                        }

                        Color c = g.ColorAt(i);
                        Color seen = ColorMath.CompositeLinear(new Color(c.r, c.g, c.b, 1f), c.a, preset);
                        Assert.That(Mathf.Abs(ColorMath.LabLightness(seen) - presetL), Is.LessThanOrEqualTo(30f), "parada " + n + ", " + g.name);
                    }
                }
            }
        }

        [Test]
        public void RippleColor_StaysWithinFiveDeltaE_OfEachPreset()
        {
            foreach (string water in new[] { "white", "clear", "black" })
            {
                Color preset = Palette.WaterBase(water);
                Assert.That(ColorMath.DeltaE00(StopBackdrop.RippleColor(preset), preset), Is.LessThanOrEqualTo(5f), water);
            }
        }

        [Test]
        public void VA03_TheSkyStaysBright_AfterTheStopLight()
        {
            Color[] pano = { Palette.SkyTop, Palette.SkyMid, Palette.SkyHorizon };
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                StopBackdrop.SkyOf(stop, out Color top, out Color horizon, out float aTop, out float aHorizon);
                Assert.That(aTop, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha), "alfa ≤ 0,5");
                Assert.That(aHorizon, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha));
                if (stop.World == 1)
                {
                    Assert.That(aTop + aHorizon, Is.EqualTo(0f), "el mundo 1 va sin tinte (el panorama ya es su alba)");
                }

                Color lut = StopBackdrop.LutOf(stop);
                Color[] tint = { top, Color.Lerp(top, horizon, 0.5f), horizon };
                float[] alpha = { aTop, (aTop + aHorizon) * 0.5f, aHorizon };
                float s = 0f;
                float v = 0f;
                for (int i = 0; i < 3; i++)
                {
                    Color c = Color.Lerp(pano[i] * lut, tint[i], alpha[i]);
                    Color.RGBToHSV(c, out _, out float si, out float vi);
                    s += si / 3f;
                    v += vi / 3f;
                }

                Assert.That(v, Is.GreaterThanOrEqualTo(0.55f), "parada " + n + ": V medio");
                Assert.That(s, Is.GreaterThanOrEqualTo(0.30f), "parada " + n + ": S medio");
            }
        }

        [Test]
        public void World1_HasNoSkyTint_AndTheOthersDo()
        {
            Show(0);
            Assert.That(_b.SkyGraphic.IsEmpty, Is.True);
            Show(2);
            Assert.That(_b.SkyGraphic.IsEmpty, Is.False);
            Assert.That(_b.SkyGraphic.MaxY, Is.EqualTo(Horizon2).Within(0.001f), "del borde de arriba al horizonte");
        }

        [Test]
        public void TheLight_IsOnlyDuskIn9_AndDawnIn19()
        {
            for (int n = 0; n < Stops.Count; n++)
            {
                Color expected = n == 9 ? StopBackdrop.DuskLut : (n == 19 ? StopBackdrop.DawnLut : Color.white);
                Assert.That(StopBackdrop.LutOf(Stops.At(n)), Is.EqualTo(expected), "parada " + n);
                Show(n);
                Assert.That(_b.LutColor, Is.EqualTo(expected), "parada " + n);
            }

            Assert.That(StopBackdrop.LutOf(null), Is.EqualTo(Color.white));
            Assert.That(StopBackdrop.DuskLut, Is.Not.EqualTo(Color.white));
            Assert.That(StopBackdrop.DawnLut, Is.Not.EqualTo(Color.white));
        }

        [Test]
        public void Shapes_FitTheHeroBox_AtAnyWidth()
        {
            foreach (string shape in StopShapes.All)
            {
                Rect r = StopShapes.Bounds(shape);
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-StopShapes.WidthDp * 0.5f), shape);
                Assert.That(r.xMax, Is.LessThanOrEqualTo(StopShapes.WidthDp * 0.5f), shape);
                Assert.That(r.yMax, Is.LessThanOrEqualTo(StopShapes.MaxAboveDp), shape + ": ≤ 48 dp sobre el horizonte");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(-StopShapes.MaxBelowDp), shape + ": ≤ 32 dp de orilla");
                Assert.That(r.yMax, Is.GreaterThan(8f), shape + ": algo sobre el horizonte");
            }

            foreach (float w in new[] { 360f, 432f })
            {
                float k = w / 360f;
                _b.Layout(w, Horizon2 * k, PanelTop2 * k, true, BandBottom2 * k);
                for (int n = 0; n < Stops.Count; n++)
                {
                    Show(n);
                    StopShapeGraphic g = _b.ShapeGraphic;
                    if (g.IsEmpty)
                    {
                        continue;
                    }

                    float horizon = Horizon2 * k;
                    Assert.That(g.MinY, Is.GreaterThanOrEqualTo(horizon - (48f * k) - 0.01f), "parada " + n + ": ≤ 48 dp encima");
                    Assert.That(g.MaxY, Is.LessThanOrEqualTo(horizon + (44f * k) + 0.01f), "parada " + n + ": reflejo ≤ 44 dp");
                    Assert.That(g.MinX, Is.GreaterThanOrEqualTo(_b.HeroRect.xMin - 0.01f), "parada " + n);
                    Assert.That(g.MaxX, Is.LessThanOrEqualTo(_b.HeroRect.xMax + 0.01f), "parada " + n);
                    Assert.That(_b.HeroRect.width, Is.EqualTo(240f * k).Within(0.001f));
                    Assert.That(_b.HeroRect.height, Is.EqualTo(80f * k).Within(0.001f));
                }
            }
        }

        [Test]
        public void Crossfade_TakesTheGivenTime_AndCutHasNone()
        {
            Show(1);
            Assert.That(_b.Fading, Is.False, "corte");
            Assert.That(_b.FrontAlpha, Is.EqualTo(1f));
            Assert.That(_b.Root.gameObject.activeSelf, Is.True);

            RiverStop next = Stops.At(2)!;
            _b.Apply(next, "clear", true, 250f);
            Assert.That(_b.Fading, Is.True);
            Assert.That(_b.Stop, Is.SameAs(next));
            Assert.That(_b.FrontAlpha, Is.EqualTo(0f));
            Assert.That(_b.BackAlpha, Is.EqualTo(1f), "la vieja sigue entera al empezar");
            Assert.That(_b.Tick(125f), Is.True);
            Assert.That(_b.FrontAlpha, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(_b.BackAlpha, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(_b.FadeProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(_b.Tick(125f), Is.True, "el fotograma en que termina");
            Assert.That(_b.Fading, Is.False);
            Assert.That(_b.FrontAlpha, Is.EqualTo(1f));
            Assert.That(_b.BackAlpha, Is.EqualTo(0f), "la vieja se apaga");
            Assert.That(_b.Tick(16f), Is.False, "sin fundido, nada que hacer");
        }

        [Test]
        public void Crossfade_BlendsTheLight_FromDuskToNone()
        {
            Show(9);
            Assert.That(_b.DisplayedLutColor, Is.EqualTo(StopBackdrop.DuskLut));
            _b.Apply(Stops.At(10), "white", true, 200f);
            Assert.That(_b.LutColor, Is.EqualTo(Color.white));
            Assert.That(_b.DisplayedLutColor, Is.EqualTo(StopBackdrop.DuskLut), "al empezar, la de antes");
            _b.Tick(100f);
            Color mid = Color.Lerp(StopBackdrop.DuskLut, Color.white, 0.5f);
            Assert.That(_b.DisplayedLutColor.g, Is.EqualTo(mid.g).Within(0.001f));
            _b.Tick(100f);
            Assert.That(_b.DisplayedLutColor, Is.EqualTo(Color.white));
        }

        [Test]
        public void WithoutAStop_NothingShows_AsToday()
        {
            Show(0);
            _b.Apply(null, "white", false, 0f);
            Assert.That(_b.Root.gameObject.activeSelf, Is.False, "sin parada, el aspecto de hoy");
            Assert.That(_b.Stop, Is.Null);
            Assert.That(_b.HeroSource, Is.EqualTo(StopBackdrop.HeroNone));
            Assert.That(_b.LutColor, Is.EqualTo(Color.white));

            // Con fundido, la parada se va y después todo se apaga.
            Show(3);
            _b.Apply(null, "white", true, 250f);
            Assert.That(_b.Fading, Is.True);
            Assert.That(_b.Root.gameObject.activeSelf, Is.True, "mientras se va");
            _b.Tick(260f);
            Assert.That(_b.Root.gameObject.activeSelf, Is.False);

            // De ninguna a una parada no hay nada que fundir: corte.
            _b.Apply(Stops.At(2), "clear", true, 250f);
            Assert.That(_b.Fading, Is.False);
            Assert.That(_b.Root.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void TheSameComposition_IsNotRebuilt()
        {
            Show(5);
            Assert.That(_b.BuildMs, Is.GreaterThan(0d));
            _b.Apply(Stops.At(5), "black", true, 250f);
            Assert.That(_b.BuildMs, Is.EqualTo(0d));
            Assert.That(_b.Fading, Is.False, "nada cambia, nada funde");
        }

        [Test]
        public void BuildMs_StaysUnderEightMilliseconds()
        {
            Show(0); // calienta
            double worst = 0d;
            for (int n = 0; n < Stops.Count; n++)
            {
                Show(n);
                worst = System.Math.Max(worst, _b.BuildMs);
            }

            TestContext.WriteLine("build_ms máximo en el Editor: " + worst.ToString("0.###"));
            Assert.That(worst, Is.LessThanOrEqualTo(8d), "08 §5.4");
        }

        [Test]
        public void Tick_DoesNotAllocate_DuringAFadeOrWithout()
        {
            Show(1);
            _b.Tick(0f);
            TestDelegate tick = () => _b.Tick(16f);
            Assert.That(tick, Is.Not.AllocatingGCMemory(), "sin fundido");
            _b.Apply(Stops.At(2), "clear", true, 10000f);
            _b.Tick(1f);
            Assert.That(tick, Is.Not.AllocatingGCMemory(), "durante el fundido");
            Assert.That(_b.Fading, Is.True);
        }

        [Test]
        public void NothingOfTheBackdrop_ReceivesTouches()
        {
            Show(0);
            _b.Apply(Stops.At(1), "clear", true, 250f);
            Graphic[] graphics = _b.Root.GetComponentsInChildren<Graphic>(true);
            Assert.That(graphics.Length, Is.GreaterThanOrEqualTo(12), "dos juegos de capas");
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }

            foreach (CanvasGroup group in _b.Root.GetComponentsInChildren<CanvasGroup>(true))
            {
                Assert.That(group.blocksRaycasts, Is.False, group.name);
                Assert.That(group.interactable, Is.False, group.name);
            }

            Assert.That(_b.Root.GetComponent<Canvas>(), Is.Not.Null, "Canvas anidado propio (guía 8)");
            Assert.That(_b.Root.GetComponent<GraphicRaycaster>(), Is.Null);
        }

        [Test]
        public void StopForLevel_IsStopOf_OrTheWorldGate_AndNullWithoutStops()
        {
            Assert.That(GameScreen.StopForLevel(Stops, 21, true)!.N, Is.EqualTo(2));
            Assert.That(GameScreen.StopForLevel(Stops, 20, true)!.N, Is.EqualTo(1), "el 20 aún es de Santarém");
            Assert.That(GameScreen.StopForLevel(Stops, 200, true)!.N, Is.EqualTo(19));
            Assert.That(GameScreen.StopForLevel(Stops, 201, true), Is.Null, "del 201 en adelante, sin paradas");
            Assert.That(GameScreen.StopForLevel(null, 5, true), Is.Null, "sin catálogo");
            Assert.That(GameScreen.StopForLevel(Stops, 35, false)!.N, Is.EqualTo(1), "ui_bg_per_stop = false: la gran parada del mundo 2");
            Assert.That(GameScreen.StopForLevel(Stops, 199, false)!.N, Is.EqualTo(14), "Iquitos, la del mundo 5");
            Assert.That(GameScreen.StopForLevel(Stops, 7, false)!.N, Is.EqualTo(0));
        }
    }
}
