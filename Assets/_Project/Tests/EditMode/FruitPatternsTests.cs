#nullable enable
using System.Collections.Generic;
using System.Reflection;
using AF.Core.Board;
using AF.GameFlow;
using AF.Levels;
using AF.Levels.Json;
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
    /// Patrones de fruta (T-E de la prueba del 2026-10-09; 07 §3, reglas 1, 2 y 5; 02 §12; 08 §9): la opacidad del patrón por
    /// nivel (0,15 hasta el 40 y 0,20 desde el 41, con los defaults de <c>ui_patterns_default_from_level</c> y
    /// <c>ui_pattern_opacity</c>) en la propiedad <c>_PatternOpacity</c> del material compartido y en
    /// <see cref="Palette.PatternMark"/>; el atlas de iconos de accesibilidad (uno por tinte, 07 §3); y en la malla del líquido,
    /// el sitio del icono en cada dosis visible, nunca en una oculta, en el Barro ni en el agua base, con Patrones de fruta o con
    /// la cuia elevada (regla 2), y el destello del sellado, que pasó a <c>TEXCOORD2.y</c>.
    /// </summary>
    public sealed class FruitPatternsTests
    {
        private static readonly MethodInfo PopulateMesh = typeof(LiquidGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private static float s_sink;

        private ProceduralArt _art = null!;
        private GameObject _canvas = null!;

        [OneTimeSetUp]
        public void CreateArt()
        {
            _art = new ProceduralArt("white");
            _canvas = new GameObject("FruitPatternsCanvas", typeof(RectTransform), typeof(Canvas));
        }

        [OneTimeTearDown]
        public void DisposeArt()
        {
            Object.DestroyImmediate(_canvas);
            _art.Dispose();
        }

        [TearDown]
        public void Clean()
        {
            LiquidGraphic.ApplyPatternOpacity(Palette.PatternOpacitySubtle); // sin nivel cargado, la sutil
            Transform root = _canvas.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        [Test]
        public void PatternOpacityFor_15UpTo40_20From41() // T-E.1; 07 §3, regla 1; 08 §9
        {
            Assert.That(Palette.PatternsDefaultFromLevel, Is.EqualTo(41), "ui_patterns_default_from_level (08 §9)");
            Assert.That(Palette.PatternOpacityOn, Is.EqualTo(0.20f), "ui_pattern_opacity (08 §9)");
            Assert.That(Palette.PatternOpacitySubtle, Is.EqualTo(0.15f), "el sutil de 07 §3, regla 1");
            Assert.That(Palette.PatternOpacityFor(1), Is.EqualTo(0.15f));
            Assert.That(Palette.PatternOpacityFor(40), Is.EqualTo(0.15f), "hasta el 40, sutil");
            Assert.That(Palette.PatternOpacityFor(41), Is.EqualTo(0.20f), "desde el 41, el 20 %");
            Assert.That(Palette.PatternOpacityFor(200), Is.EqualTo(0.20f));
            Assert.That(Palette.PatternsOnFor(40), Is.False, "el interruptor, apagado hasta el 40");
            Assert.That(Palette.PatternsOnFor(41), Is.True, "y encendido desde el 41 (T-E.6)");
            TestDelegate call = () => { s_sink = Palette.PatternOpacityFor(63) + (Palette.PatternsOnFor(63) ? 1f : 0f); };
            Assert.That(call, Is.Not.AllocatingGCMemory(), "sin asignaciones");
        }

        [Test]
        public void ApplyLevelPatterns_SetsMaterialPaletteAndViews([Values(40, 41)] int level) // T-E.1 y T-E.2
        {
            float expected = level >= 41 ? 0.20f : 0.15f;
            CuiaView a = NewView(0);
            CuiaView b = NewView(1);
            BoardAnimator animator = NewAnimator(new[] { a, b });
            LiquidGraphic.ApplyPatternOpacity(level >= 41 ? 0.15f : 0.20f); // empieza con la contraria
            animator.ApplyLevelPatterns(level);

            Material? mat = UiMaterials.Liquid;
            Assert.That(mat, Is.Not.Null, "el material compartido del líquido existe");
            Assert.That(mat!.HasProperty("_PatternOpacity"), Is.True, "el shader declara _PatternOpacity");
            Assert.That(mat.GetFloat("_PatternOpacity"), Is.EqualTo(expected).Within(1e-6f), "la propiedad del material tras cargar el " + level);
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(expected).Within(1e-6f));
            Assert.That(Palette.PatternOpacity, Is.EqualTo(expected).Within(1e-6f), "PatternMark usa la misma");
            Assert.That(a.Liquid.material, Is.SameAs(mat), "las cuias comparten ese material");

            // El bocado de la Piraña (Palette.PatternMark) mezcla con la misma opacidad que el shader.
            Color dose = Palette.Tint(5);
            Color mark = Palette.PatternMark(dose, 5);
            Color want = Color.Lerp(dose, Palette.PatternBase(5), expected);
            Assert.That(Mathf.Abs(mark.r - want.r) + Mathf.Abs(mark.g - want.g) + Mathf.Abs(mark.b - want.b), Is.LessThan(1e-5f), "PatternMark del nivel");

            bool on = level >= 41;
            Assert.That(animator.FruitPatterns, Is.EqualTo(on));
            Assert.That(a.FruitPatterns && b.FruitPatterns, Is.EqualTo(on), "el interruptor llega a cada cuia");
            Assert.That(a.FruitIconsOn, Is.EqualTo(on), "icono en cada dosis solo desde el 41");

            // Una cuia que se une después (Cuia extra, rescate) recibe el interruptor del nivel.
            CuiaView c = NewView(2);
            animator.Bind(new[] { a, b, c }, Timings, 2);
            Assert.That(c.FruitPatterns, Is.EqualTo(on), "la cuia añadida, con los Patrones de fruta del nivel");
        }

        [Test]
        public void AfStateLoad_CarriesPatterns([Values(40, 41)] int level) // T-E.7
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            var flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), new RecordingListener());
            flow.Load(level);
            LiquidGraphic.ApplyPatternOpacity(Palette.PatternOpacityFor(level)); // lo que hace la carga del nivel (BoardAnimator)
            var probe = new DevProbe();
            string? line = probe.FormatState("load", flow, int.MinValue, null);
            Assert.That(line, Is.Not.Null);
            bool on = level >= 41;
            string expected = on ? ",\"patterns\":{\"on\":true,\"opacity\":0.20}}" : ",\"patterns\":{\"on\":false,\"opacity\":0.15}}";
            Assert.That(line, Does.EndWith(expected), "al final de la línea load, con punto decimal");
            JsonValue value = JsonParser.Parse(line!.Substring("AF_STATE ".Length));
            JsonValue? patterns = value.Get("patterns");
            Assert.That(patterns, Is.Not.Null, "JSON válido con el campo patterns");
            Assert.That(patterns!.Get("on")!.AsBool(), Is.EqualTo(on));
            Assert.That(patterns.Get("opacity")!.AsDouble(), Is.EqualTo(on ? 0.20 : 0.15).Within(1e-9));
            Assert.That(probe.FormatState("ready", flow, int.MinValue, null), Does.Not.Contain("patterns"), "solo en load");
        }

        [Test]
        public void IconAtlas_OneSilhouettePerTint() // T-E.3; 07 §3, columna «Icono de accesibilidad»
        {
            Texture2D atlas = _art.FruitIconAtlas;
            Assert.That(atlas, Is.SameAs(_art.FruitIconAtlas), "se genera una vez");
            Assert.That(atlas.width, Is.EqualTo(ProceduralArt.FruitIconPx));
            Assert.That(atlas.height, Is.EqualTo(ProceduralArt.FruitIconPx * ProceduralArt.FruitIconCells), "una celda por índice de motor 0-15");
            Assert.That(atlas.mipmapCount, Is.EqualTo(1), "sin mipmaps (tex2Dlod en la rama)");
            Assert.That(ProceduralArt.FruitIconCells, Is.EqualTo(Cuia.MudColor + 1));
            Assert.That(ProceduralArt.HasFruitIcon(0), Is.False, "el agua base no tiene icono");
            Assert.That(ProceduralArt.HasFruitIcon(Cuia.MudColor), Is.False, "ni el Barro");
            Assert.That(Sum(ProceduralArt.FruitIconMask(0)) + Sum(ProceduralArt.FruitIconMask(Cuia.MudColor)), Is.EqualTo(0f));

            var masks = new float[Cuia.MaxTintColor + 1][];
            const int n = ProceduralArt.FruitIconPx;
            for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
            {
                Assert.That(ProceduralArt.HasFruitIcon(tint), Is.True, "tinte " + tint);
                float[] m = ProceduralArt.FruitIconMask(tint);
                masks[tint] = m;
                Bounds(m, out int x0, out int y0, out int x1, out int y1);
                float area = Sum(m) / (n * n);
                float height = (y1 - y0 + 1) / (float)n;
                TestContext.WriteLine("Icono " + ProceduralArt.TintIds[tint] + ": alto " + height.ToString("F2") + " de la celda, área " + area.ToString("F2"));
                Assert.That(height, Is.InRange(0.84f, 0.95f), "tinte " + tint + ": la silueta ocupa ≈ 0,9 de la caja (0,45 u de 0,5 u; 07 §3, regla 1)");
                Assert.That((x1 - x0 + 1) / (float)n, Is.InRange(0.45f, 0.95f), "tinte " + tint + ": ancho");
                Assert.That(x0 >= 1 && y0 >= 1 && x1 <= n - 2 && y1 <= n - 2, Is.True, "tinte " + tint + ": con borde transparente (el filtrado no sangra a la celda vecina)");
                Assert.That(area, Is.InRange(0.15f, 0.65f), "tinte " + tint + ": silueta llena, legible a 48 dp");
            }

            // Pares de riesgo de 07 §3 (y glosario §9): siluetas distintas (IoU de las máscaras binarias < 0,7).
            int[,] pairs = { { 4, 5 }, { 5, 6 }, { 6, 13 }, { 13, 14 }, { 14, 4 }, { 3, 12 }, { 12, 11 }, { 11, 10 }, { 8, 9 }, { 1, 2 } };
            for (int p = 0; p < pairs.GetLength(0); p++)
            {
                float iou = Iou(masks[pairs[p, 0]], masks[pairs[p, 1]]);
                Assert.That(iou, Is.LessThan(0.7f), "par de riesgo " + pairs[p, 0] + "-" + pairs[p, 1] + ": IoU " + iou.ToString("F2"));
            }
        }

        [Test]
        public void Mesh_IconOnEachVisibleDose_NeverHiddenMudOrWater() // T-E.4: con Patrones de fruta
        {
            CuiaView view = NewView(0);
            view.SetGeometry(Vector2.zero, 48f, 64f);
            Cuia cuia = Cuia.Create(5, new[] { Cuia.MudColor, 8, 4, 4 }, 0b0100);
            view.ShowContent(cuia, false);
            Assert.That(view.FruitIconsOn, Is.False, "sin Patrones de fruta ni elevación, sin iconos");
            Assert.That(CodedBands(view), Is.EqualTo(1 << 2), "solo la oculta lleva sitio de marca (su «?»)");

            view.SetFruitPatterns(true);
            Assert.That(view.FruitIconsOn, Is.True);
            Assert.That(view.Liquid.FruitIconDrawn(0), Is.False, "el Barro, sin icono (02 §9.1 M5; 07 §3)");
            Assert.That(view.Liquid.FruitIconDrawn(1), Is.True, "la dosis visible de Açaí");
            Assert.That(view.Liquid.FruitIconDrawn(2), Is.False, "la oculta, sin icono (07 §4.3)");
            Assert.That(view.Liquid.FruitIconDrawn(3), Is.True, "la de arriba");
            Assert.That(view.Liquid.FruitIconDrawn(4), Is.False, "la vacía");
            Assert.That(CodedBands(view), Is.EqualTo((1 << 1) | (1 << 2) | (1 << 3)), "sitio en la malla: las 2 visibles con icono y la oculta con su «?»");

            // En reposo, el icono va en el eje, a media banda entre sus arcos, con 0,45 u de alto (07 §3, regla 1).
            for (int k = 1; k <= 3; k += 2)
            {
                Assert.That(view.Liquid.FruitIconPlacement(k, out float cx, out float cy, out float size, out float room), Is.True);
                Assert.That(cx, Is.EqualTo(0f).Within(1e-4f), "dosis " + k + ": en el eje");
                Assert.That(cy, Is.EqualTo(CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU)).Within(0.02f), "dosis " + k + ": a media banda");
                Assert.That(size, Is.EqualTo(0.45f).Within(1e-4f), "dosis " + k + ": 0,45 u de alto");
                Assert.That(room, Is.EqualTo(1f).Within(1e-4f));
                foreach (UIVertex v in Band(view, k))
                {
                    Assert.That(v.uv1.w, Is.EqualTo(v.uv0.x - 1f).Within(1e-4f), "en reposo la x del icono es la de la cuia");
                }
            }

            // Agua base (tinte 0): sin icono, aunque se pinte como una dosis.
            view.SetDose(1, Palette.WaterWhite, 1f, 0);
            Assert.That(view.Liquid.FruitIconDrawn(1), Is.False, "el agua base no lleva icono");
            Assert.That(CodedBands(view) & (1 << 1), Is.EqualTo(0), "ni sitio en la malla");
        }

        [Test]
        public void Mesh_LiftShowsIcons_AnyLevel() // T-E.5; 07 §3, regla 2; D-002
        {
            CuiaView view = NewView(0);
            CuiaView other = NewView(1);
            view.SetGeometry(Vector2.zero, 48f, 64f);
            other.SetGeometry(new Vector2(60f, 0f), 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 3, 3, 7 }), false);
            other.ShowContent(Cuia.Create(4, new[] { 7, 3 }), false);
            Assert.That(view.FruitIconsOn || other.FruitIconsOn, Is.False, "nivel sin Patrones de fruta");

            view.SetLifted(true);
            Assert.That(view.FruitIconsOn, Is.True, "al levantarla, sus dosis muestran el icono");
            for (int k = 0; k < 3; k++)
            {
                Assert.That(view.Liquid.FruitIconDrawn(k), Is.True, "dosis " + k);
            }

            Assert.That(other.FruitIconsOn, Is.False, "nada cambia en las demás (D-002)");
            view.SetLifted(false);
            Assert.That(view.FruitIconsOn, Is.False, "al bajarla, desaparece");
            Assert.That(CodedBands(view), Is.EqualTo(0));

            // Con Patrones de fruta siguen al bajarla.
            view.SetFruitPatterns(true);
            view.SetLifted(true);
            view.SetLifted(false);
            Assert.That(view.FruitIconsOn, Is.True);
        }

        [Test]
        public void Mesh_IconFollowsTiltedBand() // T-E.4: sigue a la banda al inclinarse, sin salir de ella
        {
            CuiaView view = NewView(0);
            view.SetGeometry(Vector2.zero, 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }), false);
            view.SetFruitPatterns(true);
            LiquidGraphic liquid = view.Liquid;
            float width = 2f * CuiaGeometry.CavityHalfWidthU;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                int shown = 0;
                int off = 0;
                for (float deg = 1f; deg <= 92.01f; deg += 1.5f)
                {
                    float tilt = sign * deg;
                    liquid.SetTilt(tilt);
                    for (int k = 0; k < 3; k++)
                    {
                        string at = "dosis " + k + " a " + tilt.ToString("F1") + "°";
                        if (!liquid.FruitIconPlacement(k, out float cx, out float cy, out float size, out float room))
                        {
                            off++;
                            continue;
                        }

                        float lo = LiquidGraphic.LevelForVolume(k * width, tilt, 4);
                        float hi = LiquidGraphic.LevelForVolume((k + 1) * width, tilt, 4);
                        float d = LiquidGraphic.LineOffset(cx, cy, tilt);
                        if (liquid.FruitIconDrawn(k))
                        {
                            shown++;
                            Assert.That(size, Is.GreaterThanOrEqualTo((LiquidGraphic.GlyphMinU * ProceduralArt.FruitIconFill) - 1e-4f), at + ": de 0,27 u como mínimo");
                            Assert.That(size, Is.LessThanOrEqualTo(0.45f + 1e-4f), at + ": nunca más de 0,45 u");
                            Assert.That(d, Is.InRange(lo - (0.5f * CuiaGeometry.TopFaceU) - 0.02f, hi + 0.02f), at + ": su centro, dentro de su banda");
                        }
                        else
                        {
                            off++;
                            Assert.That(LiquidGraphic.GlyphFade(room), Is.LessThan(0.01f), at + ": se apaga solo si no cabe");
                        }
                    }
                }

                Assert.That(shown, Is.GreaterThan(30), "con la cuia inclinada, los iconos siguen a sus bandas");
                TestContext.WriteLine("Inclinación " + (sign < 0 ? "−" : "+") + ": " + shown + " iconos a la vista y " + off + " apagados o sin banda.");
            }

            liquid.SetTilt(0f);
        }

        [Test]
        public void Flash_PackedWithRipple_InSurfaceY() // el destello del sellado deja libre TEXCOORD1.w para la x del icono
        {
            for (int i = 0; i <= 15; i++)
            {
                float flash = i * 0.01f;
                for (int j = 0; j <= 6; j++)
                {
                    float amp = j * 0.005f;
                    LiquidGraphic.UnpackSurface(LiquidGraphic.PackSurface(amp, flash), out float a, out float f);
                    Assert.That(a, Is.EqualTo(amp).Within(1e-5f), "la onda");
                    Assert.That(f, Is.EqualTo(flash).Within(0.5f / LiquidGraphic.FlashPackSteps), "el destello");
                }
            }

            // Un ulp por debajo del entero (interpolación) no se convierte en una onda enorme.
            LiquidGraphic.UnpackSurface(38f - 1e-5f, out float amp2, out float flash2);
            Assert.That(amp2, Is.EqualTo(0f), "floor(y + 0,25)");
            Assert.That(flash2, Is.EqualTo(38f / LiquidGraphic.FlashPackSteps));

            CuiaView view = NewView(0);
            view.SetGeometry(Vector2.zero, 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 8, 8, 8, 8 }), false);
            view.SetFruitPatterns(true);
            view.SealAnimationAt(50f); // a mitad del destello: +15 %
            foreach (UIVertex v in Band(view, 3))
            {
                LiquidGraphic.UnpackSurface(v.uv2.y, out _, out float flash);
                Assert.That(flash, Is.EqualTo(0.15f).Within(0.5f / LiquidGraphic.FlashPackSteps), "el destello va en uv2.y");
                Assert.That(v.uv1.w, Is.EqualTo(v.uv0.x - 1f).Within(1e-4f), "y uv1.w sigue siendo la x del icono");
            }

            view.SetSealed(true);
        }

        private CuiaView NewView(int index, int capacity = 4)
        {
            return CuiaView.Create((RectTransform)_canvas.transform, index, capacity, _art, _ => { }, (_, __) => { });
        }

        private static readonly PourTimings Timings = new PourTimings(100f, 300f, 280f, 80f, 300f);

        private BoardAnimator NewAnimator(CuiaView[] views)
        {
            var streams = new GameObject("Streams", typeof(RectTransform));
            streams.transform.SetParent(_canvas.transform, false);
            var front = new GameObject("PourFront", typeof(RectTransform));
            front.transform.SetParent(_canvas.transform, false);
            var animator = new BoardAnimator((RectTransform)streams.transform, (RectTransform)front.transform);
            animator.Bind(views, Timings, 2);
            return animator;
        }

        // Bandas (bit k) que llevan un sitio de marca empaquetado sobre el alto de la cuia (uv0.w ≥ 16): el «?» de una oculta o
        // el icono de fruta.
        private static int CodedBands(CuiaView view)
        {
            int mask = 0;
            List<UIVertex> vertices = Mesh(view);
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex v = vertices[i];
                if (Mathf.Floor(v.uv0.w / LiquidGraphic.GlyphPackScale) > 0.5f)
                {
                    mask |= 1 << BandOf(v);
                }
            }

            return mask;
        }

        private static List<UIVertex> Band(CuiaView view, int k)
        {
            var band = new List<UIVertex>();
            List<UIVertex> vertices = Mesh(view);
            for (int i = 0; i < vertices.Count; i++)
            {
                if (BandOf(vertices[i]) == k && vertices[i].uv2.w - (4f * k) > -1.1f)
                {
                    band.Add(vertices[i]);
                }
            }

            Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), "la dosis " + k + " tiene banda");
            return band;
        }

        private static int BandOf(UIVertex v)
        {
            return Mathf.FloorToInt((v.uv2.w + 1.5f) / 4f);
        }

        private static List<UIVertex> Mesh(CuiaView view)
        {
            var list = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                PopulateMesh.Invoke(view.Liquid, new object[] { vh });
                var v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    list.Add(v);
                }
            }

            return list;
        }

        private static float Sum(float[] m)
        {
            float s = 0f;
            for (int i = 0; i < m.Length; i++)
            {
                s += m[i];
            }

            return s;
        }

        private static void Bounds(float[] m, out int x0, out int y0, out int x1, out int y1)
        {
            const int n = ProceduralArt.FruitIconPx;
            x0 = n;
            y0 = n;
            x1 = -1;
            y1 = -1;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    if (m[(y * n) + x] > 0.5f)
                    {
                        x0 = Mathf.Min(x0, x);
                        y0 = Mathf.Min(y0, y);
                        x1 = Mathf.Max(x1, x);
                        y1 = Mathf.Max(y1, y);
                    }
                }
            }
        }

        private static float Iou(float[] a, float[] b)
        {
            int inter = 0;
            int union = 0;
            for (int i = 0; i < a.Length; i++)
            {
                bool ia = a[i] > 0.5f;
                bool ib = b[i] > 0.5f;
                inter += ia && ib ? 1 : 0;
                union += ia || ib ? 1 : 0;
            }

            return union > 0 ? inter / (float)union : 0f;
        }
    }
}
