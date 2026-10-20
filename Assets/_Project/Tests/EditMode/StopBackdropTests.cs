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
                StopShapes.TwoSpires, null, StopShapes.AlterDoChao, StopShapes.FlonaTapajos, null, StopShapes.Dome,
                StopShapes.Anavilhanas, StopShapes.LagoTefe, StopShapes.Leticia, StopShapes.Leticia,
                StopShapes.Amacayacu, StopShapes.PuertoNarino, StopShapes.Tarapoto, StopShapes.Beacon, StopShapes.Iquitos,
                StopShapes.Rafts, StopShapes.Allpahuayo, StopShapes.Nauta, StopShapes.PacayaSamiria, StopShapes.PacayaSamiria,
            };
            Assert.That(Stops.Count, Is.EqualTo(expected.Length + 50), "1-200 y las 50 de 201-700 (M4.5a y M4.5b)");
            for (int n = 0; n < Stops.Count; n++)
            {
                // 201-700 (Q14-A): solo el kit, con piezas sin forma propia todavía: la genérica del agua de respaldo.
                string? want = n < expected.Length ? expected[n] : StopShapes.GenericWater;
                Assert.That(StopShapes.ShapeOf(Stops.At(n)!.Backdrop), Is.EqualTo(want), "parada " + n);
            }

            Assert.That(StopShapes.ShapeOf(null), Is.Null);
            Assert.That(StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w04", "bg_hero_nuevo", null, null)), Is.EqualTo(StopShapes.GenericNature));
            Assert.That(StopShapes.ShapeOf(new RiverStopBackdrop("bg_kit_w04", null, "kit:otra_pieza", null)), Is.EqualTo(StopShapes.GenericWater));
            Assert.That(StopShapes.All.Length, Is.EqualTo(20), "17 propias (M2.4) y 3 genéricas");
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
            Assert.That(_b.HeroReflection.gameObject.activeSelf, Is.True, "el reflejo se ve");
            Assert.That(_b.HeroReflection.color.a, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(_b.HeroReflectionFade.FadeDp, Is.EqualTo(44f).Within(0.0001f), "se apaga a los 44 dp");
            Assert.That(_b.HeroReflection.transform.localScale.y, Is.EqualTo(-1f), "volteado");
            Assert.That(_b.HeroReflection.raycastTarget, Is.False);

            Show(4);
            Assert.That(_b.HeroImage.gameObject.activeSelf, Is.False, "el Encuentro no tiene PNG de héroe");
        }

        [Test]
        public void VA21_ThePngReflection_FadesWithDepth_EvenForABlackAndWhiteHero()
        {
            // El peor héroe posible: la mitad blanca y la mitad negra, en columnas (cada fila tiene los dos).
            const int texW = 720;
            const int texH = 240;
            _pngTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            var pixels = new Color32[texW * texH];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (i % texW) < (texW / 2) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
            }

            _pngTexture.SetPixels32(pixels);
            _pngTexture.Apply();
            _png = Sprite.Create(_pngTexture, new Rect(0f, 0f, texW, texH), new Vector2(0.5f, 0.5f), 300f);
            StopBackdrop.HeroSpriteOverride = _ => _png;

            Color[] texels = { Color.white, Color.black };
            int checkedStops = 0;
            for (int n = 0; n < Stops.Count; n++)
            {
                Show(n);
                if (_b.HeroSource != StopBackdrop.HeroPng)
                {
                    continue; // las piezas por código no tienen PNG
                }

                checkedStops++;
                Image reflection = _b.HeroReflection;
                Assert.That(reflection.gameObject.activeInHierarchy, Is.True, "parada " + n);
                using (var vh = new VertexHelper())
                {
                    ReflectionMesh(reflection, vh);
                    Assert.That(vh.currentVertCount, Is.EqualTo(8), "parada " + n + ": cuatro filas (orilla, línea del agua, 44 dp y arriba)");
                    Assert.That(AlphaAtDepth(vh, 0f), Is.EqualTo(0.35f).Within(0.005f), "parada " + n + ": en la línea del agua");
                    Assert.That(AlphaAtDepth(vh, 44f), Is.EqualTo(0f).Within(0.005f), "parada " + n + ": a los 44 dp");

                    // VA-21: en los 16 dp de encima del panel, el blanco y el negro del reflejo sobre el agua, a ΔL* ≤ 30.
                    Color preset = _b.BandColor;
                    float presetL = ColorMath.LabLightness(preset);
                    foreach (float y in new[] { PanelTop2 - 16f, PanelTop2 - 8f, PanelTop2 })
                    {
                        float alpha = AlphaAtDepth(vh, y - Horizon2);
                        foreach (Color texel in texels)
                        {
                            Color seen = ColorMath.CompositeLinear(texel, alpha, preset);
                            Assert.That(Mathf.Abs(ColorMath.LabLightness(seen) - presetL), Is.LessThanOrEqualTo(30f),
                                "parada " + n + ", y " + y + ", alfa " + alpha);
                        }
                    }
                }
            }

            Assert.That(checkedStops, Is.EqualTo(Stops.Count - 4 - 50),
                "todas menos las dos piezas por código, las dos del kit y las 50 de 201-700, solo con el kit (Q14-A)");
            Assert.That(StopReflectionFade.DepthFactor(0f, 44f), Is.EqualTo(1f));
            Assert.That(StopReflectionFade.DepthFactor(22f, 44f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(StopReflectionFade.DepthFactor(50f, 44f), Is.EqualTo(0f));
        }

        // La malla de la imagen Simple del reflejo (el cuadrilátero de su rect, con su color) tras la atenuación por profundidad.
        private static void ReflectionMesh(Image image, VertexHelper vh)
        {
            Rect r = ((RectTransform)image.transform).rect;
            Color32 color = image.color;
            AddCorner(vh, r.xMin, r.yMin, 0f, 0f, color);
            AddCorner(vh, r.xMin, r.yMax, 0f, 1f, color);
            AddCorner(vh, r.xMax, r.yMax, 1f, 1f, color);
            AddCorner(vh, r.xMax, r.yMin, 1f, 0f, color);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
            image.GetComponent<StopReflectionFade>().ModifyMesh(vh);
        }

        private static void AddCorner(VertexHelper vh, float x, float y, float u, float v, Color32 color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y, 0f);
            vertex.color = color;
            vertex.uv0 = new Vector4(u, v, 0f, 0f);
            vh.AddVert(vertex);
        }

        // Opacidad del reflejo a depth dp bajo la línea del agua: la y local del reflejo volteado es la profundidad; entre filas,
        // lineal, como la interpola la GPU.
        private static float AlphaAtDepth(VertexHelper vh, float depth)
        {
            float loY = float.MinValue;
            float loA = 0f;
            float hiY = float.MaxValue;
            float hiA = 0f;
            UIVertex v = default;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float y = v.position.y;
                float a = v.color.a / 255f;
                if (y <= depth && y > loY)
                {
                    loY = y;
                    loA = a;
                }

                if (y >= depth && y < hiY)
                {
                    hiY = y;
                    hiA = a;
                }
            }

            return hiY - loY < 0.0001f ? loA : Mathf.Lerp(loA, hiA, (depth - loY) / (hiY - loY));
        }

        [Test]
        public void TheLinesBetweenWaters_SantaremIsWavyWithTheSun_AndTheMeetingStartsAtTheScreenEdge()
        {
            // Santarém: el Amazonas opaco desde el horizonte (con el reflejo de la orilla), ondas, una costura ondulada con la
            // cresta a 16 dp del panel y la columna del sol que cruza las dos aguas.
            Show(1);
            StopShapeGraphic code = _b.CodeGraphic;
            Color far = Palette.WaterWhite;
            Color sunColor = Palette.Hex(0xFFF4D6);
            Color shore = Color.Lerp(Palette.FarPalm, Palette.FarShore, 0.5f);
            float farTop = float.MaxValue;
            float farBottom = 0f;
            float seamTop = float.MaxValue;
            float seamBottom = 0f;
            int ripples = 0;
            bool shoreAtTheLine = false;
            bool sunOverWhite = false;
            bool sunOverClear = false;
            float sunX = StopBackdrop.SunColumnX(360f);
            for (int i = 0; i < code.VertexCount; i++)
            {
                Color c = code.ColorAt(i);
                Vector2 p = code.PositionAt(i);
                if (c.a > 0.999f && ColorMath.DeltaE00(c, far) < 0.5f)
                {
                    farTop = Mathf.Min(farTop, p.y);
                    farBottom = Mathf.Max(farBottom, p.y);
                }

                if (c.a > 0.999f && ColorMath.DeltaE00(c, StopBackdrop.RippleColor(far)) < 0.5f)
                {
                    ripples++;
                }

                if (Mathf.Abs(c.a - 0.6f) < 0.01f && ColorMath.DeltaE00(c, Palette.Hex(0xE8F0E8)) < 0.5f)
                {
                    seamTop = Mathf.Min(seamTop, p.y);
                    seamBottom = Mathf.Max(seamBottom, p.y);
                }

                if (Mathf.Abs(p.y - Horizon2) < 0.01f && Mathf.Abs(c.a - StopBackdrop.ShoreReflectionAlpha) < 0.01f
                    && ColorMath.DeltaE00(c, shore) < 1f)
                {
                    shoreAtTheLine = true;
                }

                if (ColorMath.DeltaE00(c, sunColor) < 1f && c.a > 0.01f)
                {
                    Assert.That(Mathf.Abs(p.x - sunX), Is.LessThanOrEqualTo((StopBackdrop.SunColumnDp * 0.5f) + 0.01f), "la columna, bajo el sol");
                    sunOverWhite |= Mathf.Abs(c.a - StopBackdrop.SunColumnAlphaFar) < 0.01f;
                    sunOverClear |= Mathf.Abs(c.a - StopBackdrop.SunColumnAlphaNear) < 0.01f;
                }
            }

            Assert.That(farTop, Is.EqualTo(Horizon2).Within(0.01f), "el agua blanca, opaca desde el horizonte");
            Assert.That(farBottom - Horizon2, Is.GreaterThanOrEqualTo(27.9f), "la franja de agua blanca llega a 28 dp en la cresta");
            Assert.That(farBottom, Is.LessThanOrEqualTo(PanelTop2 - 16f + 0.01f), "la cresta, a 16 dp del panel");
            Assert.That(seamBottom - seamTop, Is.GreaterThanOrEqualTo((2f * StopBackdrop.SeamWaveDp) + StopBackdrop.SeamDp - 0.2f),
                "la costura ondula (seno de 1,5 dp)");
            Assert.That(shoreAtTheLine, Is.True, "el reflejo de la orilla al 0,35 en la línea del agua");
            Assert.That(sunOverWhite, Is.True, "el sol, al 0,25 sobre el agua blanca");
            Assert.That(sunOverClear, Is.True, "y al 0,18 sobre la clara");
            Assert.That(ripples, Is.GreaterThanOrEqualTo(12), "con las ondas de la banda");
            Assert.That(code.MinX, Is.LessThanOrEqualTo(0.001f), "de borde a borde");
            Assert.That(code.MaxX, Is.GreaterThanOrEqualTo(360f - 0.001f), "de borde a borde");
            Assert.That(StopBackdrop.SeamWaveY(15f, 100f, 1f), Is.EqualTo(101.5f).Within(0.001f), "cresta a un cuarto del periodo");
            Assert.That(StopBackdrop.SeamWaveY(60f, 100f, 1f), Is.EqualTo(100f).Within(0.001f), "periodo de 60 dp");

            // El Encuentro: la cuña empieza en el borde izquierdo, junto al horizonte (sin arista vertical en la orilla).
            Show(4);
            code = _b.CodeGraphic;
            Assert.That(code.MinX, Is.LessThanOrEqualTo(0.001f), "desde el borde de la pantalla");
            Assert.That(code.MinY, Is.GreaterThanOrEqualTo(Horizon2 - 0.001f), "nada por encima del horizonte");
            for (int i = 0; i < code.VertexCount; i++)
            {
                Vector2 p = code.PositionAt(i);
                if (p.x < 0.001f)
                {
                    Assert.That(p.y, Is.LessThanOrEqualTo(Horizon2 + 8f), "en el borde izquierdo, solo el arranque de la cuña");
                }
            }
        }

        [Test]
        public void TheDawnMist_FadesIn_WithoutAHardTopEdge()
        {
            Show(19);
            StopShapeGraphic band = _b.BandGraphic;
            float top = band.MinY;
            Assert.That(top, Is.EqualTo(Horizon2 - 4f - StopBackdrop.MistFadeDp).Within(0.001f));
            for (int i = 0; i < band.VertexCount; i++)
            {
                if (Mathf.Abs(band.PositionAt(i).y - top) < 0.001f)
                {
                    Assert.That(band.ColorAt(i).a, Is.EqualTo(0f), "el borde de arriba de la bruma, transparente");
                }
            }
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
                        Assert.That(_b.CodeGraphic.MaxY, Is.LessThanOrEqualTo(panelTop - 12f + 0.01f), "parada " + n);
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
                Assert.That(aTop, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha), "parada " + n + ": arriba, alfa ≤ 0,5");
                Assert.That(aHorizon, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha),
                    "parada " + n + ": en el horizonte, alfa ≤ 0,5 también con dusk y dawn (D-030, pregunta 18 b)");
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

        // D-030, pregunta 18 (b): ninguna luz pasa de alfa 0,5 en ningún punto del cielo, ni en el tercio de abajo del atardecer
        // de la 9 ni del alba de la 19. Mira los vértices del cielo que se pinta, no solo SkyOf.
        [Test]
        public void TheSkyTint_NeverPassesSkyMaxAlpha_WithAnyLight()
        {
            Assert.That(StopBackdrop.SkyMaxAlpha, Is.LessThanOrEqualTo(0.5f), "plan del Mapa, M1.4, punto 1a");
            Assert.That(StopBackdrop.SkyLowMaxAlpha, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha), "el tercio de abajo, con el mismo tope");
            float vertexMax = (Mathf.Round(StopBackdrop.SkyMaxAlpha * 255f) / 255f) + 1e-6f; // el vértice guarda el alfa en un byte (128/255)
            int twilight = 0;
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                StopBackdrop.SkyOf(stop, out _, out _, out float aTop, out float aHorizon);
                Assert.That(aTop, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha), "parada " + n + ": arriba");
                Assert.That(aHorizon, Is.LessThanOrEqualTo(StopBackdrop.SkyMaxAlpha), "parada " + n + ": en el horizonte");
                Show(n);
                StopShapeGraphic sky = _b.SkyGraphic;
                for (int i = 0; i < sky.VertexCount; i++)
                {
                    Assert.That(sky.ColorAt(i).a, Is.LessThanOrEqualTo(vertexMax), "parada " + n + ", vértice " + i);
                }

                string? lut = stop.Backdrop.Lut;
                if (lut == StopBackdrop.LutDusk || lut == StopBackdrop.LutDawn)
                {
                    twilight++;
                    Assert.That(sky.IsEmpty, Is.False, "parada " + n + ": el atardecer y el alba llevan tinte");
                }
            }

            Assert.That(twilight, Is.EqualTo(2), "el atardecer de la 9 y el alba de la 19");
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
                Assert.That(r.yMax, Is.LessThanOrEqualTo(StopShapes.RecognizableDp), shape + ": cabe en los 30 dp que se ven con 2 filas");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(-StopShapes.MaxBelowDp), shape + ": ≤ 32 dp de orilla");
                // Algo que se vea: sobre el horizonte (8 dp como poco: Nauta, la más baja y lejana) o, en Tarapoto (revisión de
                // M2.4), los delfines en el agua.
                bool visible = shape == StopShapes.Tarapoto ? r.yMin <= -8f : r.yMax >= 8f;
                Assert.That(visible, Is.True, shape + ": algo sobre el horizonte (o en el agua, en Tarapoto)");
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
            Assert.That(_b.BackAlpha, Is.EqualTo(1f), "la vieja sigue entera debajo: su banda no deja ver el panorama");
            Assert.That(_b.BackBandAlpha, Is.EqualTo(1f));
            Assert.That(_b.BackDetailAlpha, Is.EqualTo(0.5f).Within(0.001f), "su cielo, sus piezas y su héroe se van");
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
            _b.Tick(125f);
            Assert.That(_b.BackBandAlpha, Is.EqualTo(0.5f).Within(0.001f), "sin parada nueva, también la banda se va");
            _b.Tick(135f);
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
        public void BuildMs_CountsTheMeshes_AndStaysUnderEightMilliseconds()
        {
            Show(0); // calienta
            double worst = 0d;
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                _b.Apply(null, "white", false, 0f); // cada parada se compone de cero
                int band = _b.BandGraphic.PopulateCount;
                int code = _b.CodeGraphic.PopulateCount;
                int shape = _b.ShapeGraphic.PopulateCount;
                _b.Apply(stop, WaterOf(stop.World), false, 0f);

                // Las mallas se generan dentro de Apply (y por tanto dentro de BuildMs), no en el rebuild del Canvas.
                Assert.That(_b.BandGraphic.PopulateCount, Is.GreaterThan(band), "parada " + n + ": la malla de la banda");
                Assert.That(_b.CodeGraphic.PopulateCount, Is.GreaterThan(code), "parada " + n + ": la de las piezas");
                Assert.That(_b.ShapeGraphic.PopulateCount, Is.GreaterThan(shape), "parada " + n + ": la de la silueta");
                worst = System.Math.Max(worst, _b.BuildMs);
            }

            TestContext.WriteLine("build_ms máximo en el Editor: " + worst.ToString("0.###"));
            Assert.That(worst, Is.LessThanOrEqualTo(8d), "08 §5.4");
        }

        // FX4 (observación 4 de la prueba de la ola 2 en el teléfono: el 71 en frío, 8,39 ms): el primer uso se prepara una sola
        // vez fuera de build_ms, en el juego de capas de detrás y sin que nada cambie en pantalla.
        [Test]
        public void Prewarm_PaysTheFirstUseOutsideBuildMs_WithoutChangingWhatIsDrawn()
        {
            var refScene = new GameObject("RefScene", typeof(RectTransform));
            refScene.transform.SetParent(_canvas.transform, false);
            var reference = new StopBackdrop((RectTransform)refScene.transform, null);
            reference.Layout(360f, Horizon2, PanelTop2, true, BandBottom2);
            RiverStop tefe = Stops.At(7)!;

            _b.Prewarm(tefe, "black");
            Assert.That(_b.Warmed, Is.True);
            Assert.That(_b.WarmMs, Is.GreaterThan(0d));
            Assert.That(_b.Stop, Is.Null, "no compone la parada que se ve");
            Assert.That(_b.Root.gameObject.activeSelf, Is.False, "sin parada, nada a la vista");
            Assert.That(_b.HeroSource, Is.EqualTo(StopBackdrop.HeroNone));
            Assert.That(_b.HeroRect.width, Is.EqualTo(0f));
            _b.Prewarm(tefe, "black");
            Assert.That(_b.WarmMs, Is.EqualTo(0d), "una sola vez");

            _b.Apply(tefe, "black", false, 0f);
            reference.Apply(tefe, "black", false, 0f);
            Assert.That(reference.Warmed, Is.False);
            AssertSameMesh(reference.SkyGraphic, _b.SkyGraphic, "cielo");
            AssertSameMesh(reference.BandGraphic, _b.BandGraphic, "banda");
            AssertSameMesh(reference.CodeGraphic, _b.CodeGraphic, "piezas");
            AssertSameMesh(reference.ShapeGraphic, _b.ShapeGraphic, "silueta");
            Assert.That(_b.HeroRect, Is.EqualTo(reference.HeroRect));
            Assert.That(_b.HeroSource, Is.EqualTo(reference.HeroSource));
            Assert.That(_b.LutColor, Is.EqualTo(reference.LutColor));
            Assert.That(_b.FrontAlpha, Is.EqualTo(1f));
            Assert.That(_b.BackAlpha, Is.EqualTo(0f), "el juego de capas de detrás, oculto");
            Assert.That(_b.LeavingStop, Is.Null);

            // Sin maquetación o sin parada no hace nada.
            var fresh = new StopBackdrop((RectTransform)refScene.transform, null);
            fresh.Prewarm(tefe, "black");
            Assert.That(fresh.Warmed, Is.False, "sin maquetación");
            fresh.Layout(360f, Horizon2, PanelTop2, true, BandBottom2);
            fresh.Prewarm(null, "white");
            Assert.That(fresh.Warmed, Is.False, "sin parada");
        }

        private static void AssertSameMesh(StopShapeGraphic expected, StopShapeGraphic actual, string what)
        {
            Assert.That(actual.VertexCount, Is.EqualTo(expected.VertexCount), what);
            Assert.That(actual.TriangleCount, Is.EqualTo(expected.TriangleCount), what);
            for (int i = 0; i < expected.VertexCount; i++)
            {
                Assert.That(actual.PositionAt(i), Is.EqualTo(expected.PositionAt(i)), what + " " + i);
                Assert.That(actual.ColorAt(i), Is.EqualTo(expected.ColorAt(i)), what + " " + i);
            }

            for (int i = 0; i < expected.TriangleCount * 3; i++)
            {
                Assert.That(actual.TriangleIndexAt(i), Is.EqualTo(expected.TriangleIndexAt(i)), what + " " + i);
            }
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
            Assert.That(GameScreen.StopForLevel(Stops, 201, true)!.N, Is.EqualTo(20), "M4.5a: Mazán");
            Assert.That(GameScreen.StopForLevel(Stops, 420, true)!.N, Is.EqualTo(41));
            Assert.That(GameScreen.StopForLevel(Stops, 421, true)!.N, Is.EqualTo(42), "M4.5b: Riberalta");
            Assert.That(GameScreen.StopForLevel(Stops, 700, true)!.N, Is.EqualTo(69));
            Assert.That(GameScreen.StopForLevel(Stops, 701, true), Is.Null, "del 701 en adelante, sin paradas");
            Assert.That(GameScreen.StopForLevel(Stops, 350, false)!.N, Is.EqualTo(30), "la gran parada del mundo 7");
            Assert.That(GameScreen.StopForLevel(null, 5, true), Is.Null, "sin catálogo");
            Assert.That(GameScreen.StopForLevel(Stops, 35, false)!.N, Is.EqualTo(1), "ui_bg_per_stop = false: la gran parada del mundo 2");
            Assert.That(GameScreen.StopForLevel(Stops, 199, false)!.N, Is.EqualTo(14), "Iquitos, la del mundo 5");
            Assert.That(GameScreen.StopForLevel(Stops, 7, false)!.N, Is.EqualTo(0));
        }

        [Test]
        public void ARelayoutDuringTheFade_RebuildsBothStops_AndTheFadeGoesOn()
        {
            Show(3); // Flona Tapajós, agua clara
            _b.Apply(Stops.At(4), "black", true, 250f); // al Encuentro, agua negra
            _b.Tick(100f);
            Assert.That(_b.LeavingStop!.N, Is.EqualTo(3));

            // Otra maquetación a mitad del fundido (otro horizonte, otra bandeja, otro hueco del panel).
            const float horizon = 162.9f;
            _b.Layout(360f, horizon, 226.9f, true, BandBottom2, new Rect(24f, 242.9f, 312f, 200f));
            Assert.That(_b.Fading, Is.True, "la maquetación no corta el fundido");
            Assert.That(_b.FadeProgress, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(_b.LeavingStop!.N, Is.EqualTo(3), "la que se va sigue siendo la suya");
            Assert.That(_b.BackBandAlpha, Is.EqualTo(1f), "su banda sigue entera debajo");
            Assert.That(_b.BackDetailAlpha, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(_b.LeavingBandGraphic.MinY, Is.EqualTo(horizon).Within(0.01f), "la que se va, con el horizonte nuevo");
            Assert.That(_b.BandGraphic.MinY, Is.EqualTo(horizon).Within(0.01f), "la que entra, también");
            Assert.That(ColorMath.DeltaE00(BandOpaqueColor(_b.LeavingBandGraphic), Palette.WaterClear), Is.LessThan(0.5f),
                "la que se va conserva su agua");
            Assert.That(ColorMath.DeltaE00(BandOpaqueColor(_b.BandGraphic), Palette.WaterBlack), Is.LessThan(0.5f), "y la que entra, la suya");

            Assert.That(_b.Tick(150f), Is.True);
            Assert.That(_b.Fading, Is.False, "termina a su tiempo");
            Assert.That(_b.FrontAlpha, Is.EqualTo(1f));
            Assert.That(_b.BackAlpha, Is.EqualTo(0f));
            Assert.That(_b.LeavingStop, Is.Null);
        }

        // El color del primer vértice opaco de una banda: su agua.
        private static Color BandOpaqueColor(StopShapeGraphic band)
        {
            for (int i = 0; i < band.VertexCount; i++)
            {
                Color c = band.ColorAt(i);
                if (c.a > 0.999f)
                {
                    return c;
                }
            }

            return Color.clear;
        }

        [Test]
        public void TheBand_LeavesOutWhatThePanelCovers_AndKeepsWhatShows()
        {
            // Panel de 8 a 352 dp y de 162,1 a 494 dp, menos 16 dp por lado.
            var hole = new Rect(24f, PanelTop2 + 16f, 312f, 300f);
            _b.Layout(360f, Horizon2, PanelTop2, true, BandBottom2, hole);
            Assert.That(_b.BandHole, Is.EqualTo(hole));
            for (int n = 0; n < Stops.Count; n++)
            {
                Show(n);
                StopShapeGraphic band = _b.BandGraphic;
                bool top = false;
                bool left = false;
                bool right = false;
                bool bottom = false;
                for (int t = 0; t + 2 < band.TriangleCount * 3; t += 3)
                {
                    Vector2 a = band.PositionAt(band.TriangleIndexAt(t));
                    Vector2 b = band.PositionAt(band.TriangleIndexAt(t + 1));
                    Vector2 c = band.PositionAt(band.TriangleIndexAt(t + 2));
                    Vector2 centroid = (a + b + c) / 3f;
                    Assert.That(hole.Contains(centroid), Is.False, "parada " + n + ": nada bajo el relleno del panel " + centroid);
                    top |= centroid.y > Horizon2 + StopBackdrop.BandFadeDp && centroid.y < hole.yMin;
                    left |= centroid.x < hole.xMin && centroid.y > hole.yMin && centroid.y < hole.yMax;
                    right |= centroid.x > hole.xMax && centroid.y > hole.yMin && centroid.y < hole.yMax;
                    bottom |= centroid.y > hole.yMax;
                }

                Assert.That(top && left && right && bottom, Is.True, "parada " + n + ": la franja de encima, los lados y el tramo de debajo");
                Assert.That(band.MaxY, Is.GreaterThanOrEqualTo(BandBottom2 - 0.01f), "hasta la viga del muelle");
            }

            // Sin tablero, la banda entera.
            _b.Layout(360f, Horizon2, PanelTop2, false, BandBottom2);
            Assert.That(_b.BandHole.width, Is.EqualTo(0f));
        }

        [Test]
        public void AtDuskAndDawn_TheSilhouetteIsBacklit_NotSlateGrey()
        {
            string city = StopShapes.Leticia;
            Assert.That(StopShapes.LitColor(city, StopShapes.ToneBody, StopBackdrop.DuskLut, StopBackdrop.LutDusk), Is.EqualTo(StopShapes.DuskBody));
            Assert.That(StopShapes.LitColor(city, StopShapes.ToneAccent, StopBackdrop.DuskLut, StopBackdrop.LutDusk), Is.EqualTo(StopShapes.DuskAccent));
            Color window = StopShapes.LitColor(city, StopShapes.ToneLight, StopBackdrop.DuskLut, StopBackdrop.LutDusk);
            Assert.That(new Color(window.r, window.g, window.b, 1f), Is.EqualTo(StopShapes.DuskWindow), "ventanas encendidas");
            Assert.That(window.a, Is.EqualTo(StopShapes.DuskWindowAlpha));
            Assert.That(StopShapes.LitColor(city, StopShapes.ToneBody, StopBackdrop.DawnLut, StopBackdrop.LutDawn), Is.EqualTo(StopShapes.DawnBody));
            Assert.That(StopShapes.LitColor(city, StopShapes.ToneAccent, StopBackdrop.DawnLut, StopBackdrop.LutDawn), Is.EqualTo(StopShapes.DawnAccent));
            Color day = StopShapes.ToneColor(city, StopShapes.ToneBody);
            Assert.That(StopShapes.LitColor(city, StopShapes.ToneBody, Color.white, null), Is.EqualTo(day), "de día, el tono tal cual");
            foreach (Color c in new[] { StopShapes.DuskBody, StopShapes.DuskAccent, StopShapes.DawnBody, StopShapes.DawnAccent })
            {
                Color.RGBToHSV(c, out _, out float s, out float v);
                Assert.That(v, Is.GreaterThanOrEqualTo(0.25f), "VA-03: nada por debajo de V 0,25");
                Assert.That(s, Is.GreaterThan(0.1f), "con color, no un gris neutro");
            }

            // Leticia al atardecer: la silueta en morado a contraluz, con ventanas cálidas, y nada del gris de pizarra.
            Show(9);
            Assert.That(_b.Shape, Is.EqualTo(city));
            StopShapeGraphic g = _b.ShapeGraphic;
            Color grey = new Color(day.r * StopBackdrop.DuskLut.r, day.g * StopBackdrop.DuskLut.g, day.b * StopBackdrop.DuskLut.b, 1f);
            bool body = false;
            bool lit = false;
            for (int i = 0; i < g.VertexCount; i++)
            {
                if (g.PositionAt(i).y > Horizon2)
                {
                    continue; // el reflejo
                }

                Color c = g.ColorAt(i);
                Assert.That(ColorMath.DeltaE00(c, grey), Is.GreaterThan(5f), "vértice " + i + ": sin el gris de pizarra");
                body |= ColorMath.DeltaE00(c, StopShapes.DuskBody) < 0.5f;
                lit |= ColorMath.DeltaE00(c, StopShapes.DuskWindow) < 0.5f && Mathf.Abs(c.a - StopShapes.DuskWindowAlpha) < 0.01f;
            }

            Assert.That(body && lit, Is.True, "cuerpo a contraluz y ventanas encendidas");
        }

        [Test]
        public void TheSilhouetteBase_FadesInAtTheEdgesOfTheBox()
        {
            // Lo que llega al borde de la caja de 240 dp (el zócalo, la arena, el malecón, la selva lejana) entra con un degradado:
            // ningún vértice de la silueta ni de su reflejo es opaco en el borde, y a los 16 dp ya lo es, también en Pacaya-Samiria,
            // cuya cocha es una lente que no llega a los bordes (M2.4: con las formas propias, la orilla no siempre es el tono que
            // llega al borde, así que se mira todo vértice).
            // Todas las paradas con silueta llegan a los dos bordes, sin excepción: desde la revisión de M2.4, también las balsas
            // del Itaya, con su orilla lejana; una forma que se separe del borde hace fallar el test.
            int checkedStops = 0;
            for (int n = 0; n < Stops.Count; n++)
            {
                Show(n);
                if (_b.HeroSource != StopBackdrop.HeroShape)
                {
                    continue;
                }

                StopShapeGraphic g = _b.ShapeGraphic;
                float left = _b.HeroRect.xMin;
                float right = _b.HeroRect.xMax;
                int edgesLeft = 0;
                int edgesRight = 0;
                bool inside = false;
                for (int i = 0; i < g.VertexCount; i++)
                {
                    Vector2 p = g.PositionAt(i);
                    Color c = g.ColorAt(i);
                    bool atLeft = Mathf.Abs(p.x - left) < 0.01f;
                    bool atRight = Mathf.Abs(p.x - right) < 0.01f;
                    if (atLeft || atRight)
                    {
                        edgesLeft += atLeft ? 1 : 0;
                        edgesRight += atRight ? 1 : 0;
                        Assert.That(c.a, Is.EqualTo(0f), "parada " + n + ": sin arista vertical en el borde de la caja");
                    }

                    inside |= Mathf.Abs(p.x - (left + StopShapes.EdgeFadeDp)) < 0.01f && c.a > 0.99f;
                }

                checkedStops++;
                Assert.That(edgesLeft, Is.GreaterThanOrEqualTo(2), "parada " + n + " (" + _b.Shape + "): llega al borde izquierdo");
                Assert.That(edgesRight, Is.GreaterThanOrEqualTo(2), "parada " + n + " (" + _b.Shape + "): llega al borde derecho");
                Assert.That(inside, Is.True, "parada " + n + ": opaco al acabar el degradado del borde");
            }

            Assert.That(checkedStops, Is.EqualTo(68), "las 70 paradas menos las dos líneas entre aguas");
        }

        [Test]
        public void TheProbeStrings_AreEscapedJson()
        {
            var sb = new System.Text.StringBuilder();
            GameScreen.AppendJsonString(sb, "id", "a\"b\\c\n");
            Assert.That(sb.ToString(), Is.EqualTo(",\"id\":\"a\\\"b\\\\c\\u000a\""));
            sb.Clear();
            GameScreen.AppendJsonString(sb, "id", null);
            Assert.That(sb.ToString(), Is.EqualTo(",\"id\":null"));
            sb.Clear();
            GameScreen.AppendJsonString(sb, "id", "leticia_atardecer");
            Assert.That(sb.ToString(), Is.EqualTo(",\"id\":\"leticia_atardecer\""));
        }

        [Test]
        public void HeroId_IsTheSlotOrTheKitPiece_OrNull()
        {
            Show(9);
            Assert.That(_b.HeroId, Is.EqualTo("bg_hero_leticia"));
            Show(13);
            Assert.That(_b.HeroId, Is.EqualTo("kit:rio_ancho_con_baliza"));
            Show(1);
            Assert.That(_b.HeroId, Is.Null, "la pieza por código no es un héroe");
            MapArtRule.ForceProvisional = false;
            _b.Apply(null, "white", false, 0f);
            Show(9);
            Assert.That(_b.HeroId, Is.Null, "en release, sin PNG, no se pinta ninguno");
        }
    }
}
