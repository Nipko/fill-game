#nullable enable
using System.Collections.Generic;
using System.Reflection;
using AF.Core.Board;
using AF.GameFlow;
using AF.Levels;
using AF.Levels.Json;
using AF.Presentation;
using AF.Presentation.Mechanics;
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
    /// la cuia elevada (regla 2); en la que se revela, el icono entra con el patrón de su fruta y no salta al volver a ser normal
    /// (D-003); y el destello del sellado, que pasó a <c>TEXCOORD2.y</c>.
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
        public void PatternsSetting_AutoByLevel_OnAlways_OffNever() // L3; 02 §12: el interruptor existe siempre en Ajustes
        {
            Assert.That(PatternsResolver.IsOn(AF.Save.PatternsSetting.Auto, 40), Is.False, "auto: como antes, apagado hasta el 40");
            Assert.That(PatternsResolver.IsOn(AF.Save.PatternsSetting.Auto, 41), Is.True, "y encendido desde el 41");
            Assert.That(PatternsResolver.IsOn(AF.Save.PatternsSetting.On, 1), Is.True, "on: también en el 1");
            Assert.That(PatternsResolver.IsOn(AF.Save.PatternsSetting.Off, 200), Is.False, "off: tampoco en el 200");
            TestDelegate call = () => { s_sink = PatternsResolver.IsOn(AF.Save.PatternsSetting.Auto, 63) ? 1f : 0f; };
            Assert.That(call, Is.Not.AllocatingGCMemory(), "sin asignaciones");

            CuiaView a = NewView(0);
            CuiaView b = NewView(1);
            BoardAnimator animator = NewAnimator(new[] { a, b });
            animator.ApplyPatterns(PatternsResolver.IsOn(AF.Save.PatternsSetting.On, 40));
            Assert.That(animator.FruitPatterns && a.FruitPatterns && b.FruitPatterns, Is.True, "on en el 40: el icono en cada cuia");
            Assert.That(a.FruitIconsOn, Is.True);
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(Palette.PatternOpacityOn).Within(1e-6f), "y el patrón al 20 %");

            animator.ApplyPatterns(PatternsResolver.IsOn(AF.Save.PatternsSetting.Off, 41));
            Assert.That(animator.FruitPatterns || a.FruitPatterns || b.FruitPatterns, Is.False, "off en el 41: sin icono");
            Assert.That(a.FruitIconsOn, Is.False);
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(Palette.PatternOpacitySubtle).Within(1e-6f), "y el patrón sutil del 15 %");

            animator.ApplyLevelPatterns(41);
            Assert.That(animator.FruitPatterns, Is.True, "ApplyLevelPatterns sigue siendo el automático");
            TestDelegate apply = () => animator.ApplyPatterns(false);
            Assert.That(apply, Is.Not.AllocatingGCMemory(), "aplicar el ajuste no asigna");
        }

        [Test]
        public void AfStateLoad_CarriesThePatternsOfTheSetting([Values(40, 41)] int level) // L3 con T-E.7
        {
            var flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), new RecordingListener());
            flow.Load(level);
            var settings = new AF.Save.SettingsData { Patterns = level == 40 ? AF.Save.PatternsSetting.On : AF.Save.PatternsSetting.Off };
            bool on = level == 40;
            LiquidGraphic.ApplyPatternOpacity(on ? Palette.PatternOpacityOn : Palette.PatternOpacitySubtle);
            var probe = new DevProbe { Settings = settings };
            string? line = probe.FormatState("load", flow, int.MinValue, null);
            string expected = on ? ",\"patterns\":{\"on\":true,\"opacity\":0.20}}" : ",\"patterns\":{\"on\":false,\"opacity\":0.15}}";
            Assert.That(line, Does.EndWith(expected), "el interruptor del ajuste, no el del nivel");
            Assert.That(probe.FormatSettingsChange("patterns", "off", null), Does.Match("^AF_STATE \\{\"event\":\"settings_change\",\"t\":\\d+,\"key\":\"patterns\",\"value\":\"off\",\"on\":null,\"opacity\":0\\.\\d\\d\\}$"));
            Assert.That(probe.FormatSettingsChange("language", "en-XA", null), Does.EndWith(",\"key\":\"language\",\"value\":\"en-XA\"}"));
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

            // Pares de riesgo de 07 §3 (y glosario §9): siluetas distintas (IoU de las máscaras binarias < 0,7). Además, la
            // Guanábana frente al Lulo y al Aguaje (revisión de B3: el 1 y el 11 se confundían).
            int[,] pairs = { { 4, 5 }, { 5, 6 }, { 6, 13 }, { 13, 14 }, { 14, 4 }, { 3, 12 }, { 12, 11 }, { 11, 10 }, { 8, 9 }, { 1, 2 }, { 1, 11 }, { 1, 4 } };
            for (int p = 0; p < pairs.GetLength(0); p++)
            {
                float iou = Iou(masks[pairs[p, 0]], masks[pairs[p, 1]]);
                Assert.That(iou, Is.LessThan(0.7f), "par de riesgo " + pairs[p, 0] + "-" + pairs[p, 1] + ": IoU " + iou.ToString("F2"));
            }
        }

        [Test]
        public void GuanabanaIcon_HangsTilted_WithLooseHooks_NotAguaje() // revisión 3 de B3: a ≈ 10 dp, dos piñas de pino
        {
            const int n = ProceduralArt.FruitIconPx;
            float[] guanabana = ProceduralArt.FruitIconMask(1);
            float[] aguaje = ProceduralArt.FruitIconMask(4);

            // Siluetas a resolución completa y a 1 px/dp: la caja de 0,5 u mide ≈ 11,7 dp (FruitIconRimPx ≈ 1 dp).
            int dpCells = Mathf.RoundToInt(n / ProceduralArt.FruitIconRimPx);
            Assert.That(dpCells, Is.EqualTo(12), "1 px/dp: 12 × 12 px por celda");
            float full = Iou(guanabana, aguaje);
            float atDp = IouAt(guanabana, aguaje, dpCells);
            TestContext.WriteLine("IoU Guanábana-Aguaje: " + full.ToString("F2") + " a 64 px y " + atDp.ToString("F2") + " a 1 px/dp (antes, ≈ 0,55 y 0,66).");
            Assert.That(full, Is.LessThan(0.5f), "1-4 a resolución completa");
            Assert.That(atDp, Is.LessThan(0.55f), "1-4 a 1 px/dp");
            int[] others = { 2, 11 };
            for (int i = 0; i < others.Length; i++)
            {
                float iou = IouAt(guanabana, ProceduralArt.FruitIconMask(others[i]), dpCells);
                Assert.That(iou, Is.LessThan(0.7f), "par de riesgo 1-" + others[i] + " a 1 px/dp: IoU " + iou.ToString("F2"));
            }

            // Inclinación: cuelga girada unos 25° y ancha; el Aguaje sigue derecho y alargado.
            float tilt = AxisFromVertical(guanabana);
            float upright = AxisFromVertical(aguaje);
            Bounds(guanabana, out int gx0, out int gy0, out int gx1, out int gy1);
            Bounds(aguaje, out int ax0, out int ay0, out int ax1, out int ay1);
            float aspect = (gx1 - gx0 + 1) / (float)(gy1 - gy0 + 1);
            float aguajeAspect = (ax1 - ax0 + 1) / (float)(ay1 - ay0 + 1);
            TestContext.WriteLine("Eje principal a " + tilt.ToString("F1") + "° de la vertical (Aguaje, " + upright.ToString("F1") + "°); aspecto " + aspect.ToString("F2") + " (Aguaje, " + aguajeAspect.ToString("F2") + ").");
            Assert.That(tilt, Is.InRange(15f, 35f), "la Guanábana cuelga girada");
            Assert.That(upright, Is.LessThan(5f), "el Aguaje, derecho");
            Assert.That(aspect, Is.GreaterThanOrEqualTo(0.85f), "la Guanábana, ancha (aspecto ≥ 0,85)");
            Assert.That(aguajeAspect, Is.LessThan(0.75f), "el Aguaje, alargado");

            // Textura: 4-5 ganchos huecos sueltos (cerrados dentro del cuerpo), de ≥ 2 dp de largo y ≈ 1 dp de grueso, curvados
            // todos hacia el mismo lado y sin hileras ni columnas.
            List<List<int>> hooks = Holes(guanabana);
            Assert.That(hooks.Count, Is.InRange(4, 5), "4-5 ganchos sueltos");
            var centroids = new Vector2[hooks.Count];
            var bulges = new Vector2[hooks.Count];
            for (int h = 0; h < hooks.Count; h++)
            {
                List<int> hook = hooks[h];
                Assert.That(hook.Count, Is.GreaterThanOrEqualTo(60), "gancho " + h + ": área ≥ 2 dp²");
                Assert.That(HasDisc(hook, 2), Is.True, "gancho " + h + ": ≈ 1 dp de grueso (cabe un disco de 5 px)");
                float longest = -1f;
                Vector2 tipA = Vector2.zero;
                Vector2 tipB = Vector2.zero;
                Vector2 sum = Vector2.zero;
                for (int i = 0; i < hook.Count; i++)
                {
                    var p = new Vector2(hook[i] % n, hook[i] / n);
                    sum += p;
                    for (int j = i + 1; j < hook.Count; j++)
                    {
                        var q = new Vector2(hook[j] % n, hook[j] / n);
                        float d = (p - q).sqrMagnitude;
                        if (d > longest)
                        {
                            longest = d;
                            tipA = p;
                            tipB = q;
                        }
                    }
                }

                centroids[h] = sum / hook.Count;
                bulges[h] = centroids[h] - (0.5f * (tipA + tipB));
                Assert.That(Mathf.Sqrt(longest), Is.GreaterThanOrEqualTo(2f * ProceduralArt.FruitIconRimPx), "gancho " + h + ": ≥ 2 dp de largo");
                Assert.That(bulges[h].magnitude, Is.GreaterThan(0.5f), "gancho " + h + ": curvo");
            }

            for (int a = 0; a < hooks.Count; a++)
            {
                for (int b = a + 1; b < hooks.Count; b++)
                {
                    Assert.That(Mathf.Abs(centroids[a].x - centroids[b].x), Is.GreaterThanOrEqualTo(4f), "ganchos " + a + " y " + b + ": sin columnas");
                    Assert.That(Mathf.Abs(centroids[a].y - centroids[b].y), Is.GreaterThanOrEqualTo(4f), "ganchos " + a + " y " + b + ": sin hileras");
                    Assert.That(Vector2.Angle(bulges[a], bulges[b]), Is.LessThan(30f), "ganchos " + a + " y " + b + ": curvados hacia el mismo lado");
                }
            }
        }

        [Test]
        public void IconAtlas_GeneratedWithinBudget() // revisión de B3: el atlas se genera en la primera carga de nivel
        {
            Assert.That(_art.FruitIconAtlas, Is.Not.Null); // primera generación (incluye el JIT)
            double best = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var art = new ProceduralArt("white");
                try
                {
                    Assert.That(art.FruitIconAtlasMs, Is.EqualTo(0.0), "perezoso: 0 hasta que se pide");
                    Assert.That(art.FruitIconAtlas, Is.Not.Null);
                    best = System.Math.Min(best, art.FruitIconAtlasMs);
                }
                finally
                {
                    art.Dispose();
                }
            }

            TestContext.WriteLine("Atlas de iconos de fruta: " + best.ToString("F1") + " ms (mínimo de 3, tras el JIT).");
            Assert.That(best, Is.GreaterThan(0.0), "se mide");
            Assert.That(best, Is.LessThan(30.0), "14 siluetas y 2 contornos en menos de 30 ms (Editor, tras el JIT)");
        }

        [Test]
        public void IconRim_OnlyIvoryOnMidLightnessTints() // revisión de B3: marfil sobre L* 45-55 (Achiote y Río Negro)
        {
            const int n = ProceduralArt.FruitIconPx;
            for (int tint = 0; tint <= Cuia.MudColor; tint++)
            {
                bool expected = tint == 5 || tint == 14;
                Assert.That(ProceduralArt.FruitIconHasRim(tint), Is.EqualTo(expected), "tinte " + tint);
                float[] rim = ProceduralArt.FruitIconRimMask(tint);
                if (!expected)
                {
                    Assert.That(Sum(rim), Is.EqualTo(0f), "tinte " + tint + ": sin contorno (G = 0)");
                    continue;
                }

                Assert.That(Palette.PatternBase(tint), Is.EqualTo(Palette.UiPaper), "tinte " + tint + ": icono en marfil");
                float l = ColorMath.LabLightness(Palette.Tint(tint));
                Assert.That(l, Is.InRange(45f, 55f), "tinte " + tint + ": L* " + l.ToString("F1"));
                float[] m = ProceduralArt.FruitIconMask(tint);
                int widened = 0;
                for (int i = 0; i < m.Length; i++)
                {
                    Assert.That(rim[i], Is.GreaterThanOrEqualTo(m[i] - 1e-4f), "tinte " + tint + ": el contorno cubre la silueta");
                    if (rim[i] > 0.5f)
                    {
                        // Cada píxel del contorno está a ≈ 1 dp (FruitIconRimPx) de la silueta, como mucho.
                        int x = i % n;
                        int y = i / n;
                        bool near = false;
                        int reach = Mathf.CeilToInt(ProceduralArt.FruitIconRimPx) + 1;
                        for (int dy = -reach; dy <= reach && !near; dy++)
                        {
                            for (int dx = -reach; dx <= reach && !near; dx++)
                            {
                                int xx = x + dx;
                                int yy = y + dy;
                                near = xx >= 0 && yy >= 0 && xx < n && yy < n && m[(yy * n) + xx] > 0.5f && ((dx * dx) + (dy * dy)) <= (reach * reach);
                            }
                        }

                        Assert.That(near, Is.True, "tinte " + tint + ": contorno a ≤ 1 dp de la silueta en (" + x + ", " + y + ")");
                        widened += m[i] < 0.5f ? 1 : 0;
                    }
                }

                TestContext.WriteLine("Contorno del tinte " + tint + ": " + widened + " px fuera de la silueta.");
                Assert.That(widened, Is.GreaterThan(400), "tinte " + tint + ": el contorno rodea la silueta");
            }
        }

        [Test]
        public void Mesh_IconOnEachVisibleDose_NeverHiddenMudOrWater() // T-E.4: con Patrones de fruta
        {
            // Vista de capacidad 5, como la cuia: la dosis 4 existe y está vacía (con capacidad 4 quedaría fuera de rango).
            CuiaView view = NewView(0, 5);
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
            Assert.That(view.Capacity, Is.EqualTo(5), "la dosis 4 está dentro de la capacidad");
            Assert.That(cuia.Count, Is.EqualTo(4), "y vacía");
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
        public void RuleReveal_IconEntersWithPattern_NoJumpToNormal() // D-003; 07 §3: del 35 al 55 % del revelado, sin salto del 54 al 56 %
        {
            // Nivel turbio (desde el 100, con Patrones de fruta y el patrón al 20 %): la regla revela la dosis 1 (tinte 8) y la 0
            // sigue oculta. Fotogramas de 6 ms (200 en los 1.200 ms del revelado).
            LiquidGraphic.ApplyPatternOpacity(Palette.PatternOpacityFor(100));
            CuiaView view = NewView(0);
            view.SetGeometry(Vector2.zero, 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 4, 8 }, 0b0001), false);
            view.SetFruitPatterns(true);
            LiquidGraphic liquid = view.Liquid;
            const int layer = 1;
            const int hidden = 0;
            const float stepMs = 6f;
            float ms = view.PlayReveal(1 << layer, RevealStyle.Rule, 1f);
            Assert.That(ms, Is.EqualTo(MurkyMarks.RevealRuleMs), "1.200 ms a velocidad 1");
            Assert.That(liquid.FruitIconAmount(layer), Is.EqualTo(0f), "al empezar, turbia: sin icono");

            float[] marks = { 0.35f, 0.45f, 0.54f, 0.56f };
            var at = new float[marks.Length];
            var drawnAt = new bool[marks.Length];
            int next = 0;
            float previous = 0f;
            float maxStep = 0f;
            bool meshChecked = false;
            for (float elapsed = stepMs; view.Murky.IsAnimating && elapsed <= ms + stepMs; elapsed += stepMs)
            {
                view.Tick(stepMs);
                float t = elapsed / ms;
                float amount = liquid.FruitIconAmount(layer);
                LiquidGraphic.MurkyLook look = liquid.MurkyLookAt(layer);
                string label = "al " + (100f * t).ToString("F1") + " %";
                if (look == LiquidGraphic.MurkyLook.Dissolve)
                {
                    Assert.That(amount, Is.EqualTo(LiquidGraphic.FruitPatternWeight(look, liquid.MurkyProgressAt(layer))).Within(1e-6f), label + ": el icono entra con el patrón de su fruta (la misma curva)");
                }
                else
                {
                    Assert.That(look, Is.EqualTo(LiquidGraphic.MurkyLook.None), label + ": ya es la dosis normal");
                    Assert.That(amount, Is.EqualTo(1f), label + ": con su icono entero");
                }

                Assert.That(amount, Is.GreaterThanOrEqualTo(previous - 1e-6f), label + ": el icono nunca vuelve atrás");
                maxStep = Mathf.Max(maxStep, amount - previous);
                previous = amount;
                Assert.That(liquid.MurkyLookAt(hidden), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), label + ": la otra sigue oculta");
                Assert.That(liquid.FruitIconAmount(hidden), Is.EqualTo(0f), label + ": y sin icono");
                Assert.That(liquid.FruitIconDrawn(hidden), Is.False, label + ": nunca en una oculta (07 §4.3)");
                while (next < marks.Length && t >= marks[next] - 1e-4f)
                {
                    at[next] = amount;
                    drawnAt[next] = liquid.FruitIconDrawn(layer);
                    next++;
                }

                if (!meshChecked && look == LiquidGraphic.MurkyLook.Dissolve && amount > 0.2f)
                {
                    // El shader: la que se revela trae la marca del icono en uv3.w (su fila + 16) y el sitio de su «?», que es el
                    // del icono; la oculta, la fila turbia sin marca (peso 0: nunca icono).
                    meshChecked = true;
                    foreach (UIVertex v in Band(view, layer))
                    {
                        Assert.That(v.uv3.w, Is.EqualTo(LiquidGraphic.RevealRowCode(8, true)), "la que se revela pide su icono (marca en uv3.w)");
                        Assert.That(v.uv3.w, Is.GreaterThan(LiquidGraphic.RevealIconFlag - 0.25f), "por encima del umbral del shader");
                    }

                    foreach (UIVertex v in Band(view, hidden))
                    {
                        Assert.That(v.uv3.w, Is.EqualTo((float)ProceduralArt.MurkyPatternRow), "la oculta, con la fila turbia y sin marca");
                    }

                    Assert.That(CodedBands(view) & 0b11, Is.EqualTo(0b11), "las dos con el sitio de su marca empaquetado");
                }
            }

            TestContext.WriteLine("Icono de la dosis que se revela: " + at[0].ToString("F3") + " al 35 %, " + at[1].ToString("F3") + " al 45 %, "
                + at[2].ToString("F4") + " al 54 % y " + at[3].ToString("F4") + " al 56 %; el mayor paso entre fotogramas de 6 ms, " + maxStep.ToString("F3") + ".");
            Assert.That(meshChecked, Is.True, "se miró la malla con el icono entrando");
            Assert.That(next, Is.EqualTo(marks.Length), "pasó por el 35, el 45, el 54 y el 56 %");
            Assert.That(at[0], Is.LessThan(0.01f), "al 35 %, el icono aún no entra (con el patrón)");
            Assert.That(at[1], Is.InRange(0.1f, 0.9f), "al 45 %, a medio entrar");
            Assert.That(drawnAt[2], Is.True, "al 54 %, ya se dibuja");
            Assert.That(at[2], Is.GreaterThan(0.99f), "al 54 %, casi entero");
            Assert.That(drawnAt[3] && at[3] == 1f, Is.True, "al 56 %, la dosis normal con su icono entero");
            Assert.That(at[3] - at[2], Is.LessThan(0.01f), "del 54 al 56 % (el paso a la dosis normal) el icono no salta");
            Assert.That(maxStep, Is.LessThan(0.05f), "entra poco a poco (menos del 5 % por fotograma de 6 ms)");

            // El patrón usa la misma _PatternOpacity (0,20 en un nivel turbio) al revelarse y en la dosis normal, y su peso llega
            // a 1 con el del icono: tampoco pasa de 0,15 a 0,20 de golpe en el cambio.
            Assert.That(Palette.PatternOpacity, Is.EqualTo(0.20f), "nivel turbio: el patrón al 20 %");
            float patternAt54 = Palette.PatternOpacity * LiquidGraphic.FruitPatternWeight(LiquidGraphic.MurkyLook.Dissolve, 0.54f / MurkyMarks.RuleDissolveEndAt);
            Assert.That(patternAt54, Is.EqualTo(0.20f).Within(0.002f), "al 54 %, el patrón ya casi al 20 %");
            Assert.That(liquid.FruitIconDrawn(layer), Is.True, "revelada, con su icono");
        }

        [Test]
        public void FireflyReveal_IconUnderDisc_WholeWhenDiscFades() // D-003; 04 §2.5: al terminar la Luciérnaga el icono no salta
        {
            // Las 2 ocultas de una cuia turbia con Patrones de fruta, con la Luciérnaga en fotogramas de 2 ms: el icono de cada
            // banda entra con su patrón bajo el disco opaco del icono temporal (MurkyMarks), que solo se apaga cuando aquel ya
            // está entero, y el paso a la dosis normal no lo cambia.
            LiquidGraphic.ApplyPatternOpacity(Palette.PatternOpacityFor(100));
            CuiaView view = NewView(0);
            view.SetGeometry(Vector2.zero, 48f, 64f);
            Cuia cuia = Cuia.Create(4, new[] { 4, 8, 3 }, 0b0011);
            view.ShowContent(cuia, false);
            view.SetFruitPatterns(true);
            LiquidGraphic liquid = view.Liquid;
            float ms = view.PlayReveal(cuia.HiddenMask, RevealStyle.Firefly, 1f);
            Assert.That(ms, Is.EqualTo(MurkyMarks.RevealFireflyMs), "600 ms a velocidad 1");
            var last = new float[2];
            var switched = new bool[2];
            bool underDisc = false;
            for (float elapsed = 2f; view.Murky.IsAnimating && elapsed <= ms + 2f; elapsed += 2f)
            {
                view.Tick(2f);
                for (int k = 0; k < 2; k++)
                {
                    string label = "a los " + elapsed + " ms, dosis " + k;
                    LiquidGraphic.MurkyLook look = liquid.MurkyLookAt(k);
                    float amount = liquid.FruitIconAmount(k);
                    if (look == LiquidGraphic.MurkyLook.Turn)
                    {
                        Assert.That(amount, Is.EqualTo(LiquidGraphic.FruitPatternWeight(look, liquid.MurkyProgressAt(k))).Within(1e-6f), label + ": entra con su patrón");
                    }
                    else if (look == LiquidGraphic.MurkyLook.None && !switched[k])
                    {
                        switched[k] = true;
                        Assert.That(amount - last[k], Is.LessThan(0.01f), label + ": al pasar a la dosis normal, el icono no salta");
                    }

                    if (view.Murky.FruitVisible(k) && liquid.FruitIconDrawn(k))
                    {
                        bool opaque = view.Murky.FruitDisc(k).color.a >= 0.999f;
                        Assert.That(opaque || amount >= 0.999f, Is.True, label + ": bajo el disco opaco, o ya entero mientras el disco se apaga");
                        Assert.That(liquid.FruitIconPlacement(k, out float cx, out float cy, out _, out float room), Is.True);
                        float discY = CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU);
                        float halfDiagonal = 0.5f * LiquidGraphic.GlyphSizeU(room) * Mathf.Sqrt(2f);
                        Assert.That(cx, Is.EqualTo(0f).Within(1e-3f), label + ": en el eje, como el disco");
                        Assert.That(halfDiagonal + Mathf.Abs(cy - discY), Is.LessThanOrEqualTo(0.5f * MurkyMarks.FireflyDiscU), label + ": la caja del icono, dentro del disco");
                        underDisc |= opaque && amount > 0.001f && amount < 0.999f;
                    }

                    last[k] = amount;
                }
            }

            Assert.That(view.Murky.IsAnimating, Is.False, "terminó");
            Assert.That(switched[0] && switched[1], Is.True, "las 2 pasaron a su dosis normal");
            Assert.That(underDisc, Is.True, "el icono entró bajo el disco opaco");
            for (int k = 0; k < 2; k++)
            {
                Assert.That(liquid.FruitIconAmount(k), Is.EqualTo(1f), "dosis " + k + ": entero");
                Assert.That(liquid.FruitIconDrawn(k), Is.True, "dosis " + k + ": con su icono");
            }
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

        // IoU de las máscaras reducidas a cells × cells px (media por área, con pesos fraccionarios en los bordes).
        private static float IouAt(float[] a, float[] b, int cells)
        {
            return Iou(Downsample(a, cells), Downsample(b, cells));
        }

        private static float[] Downsample(float[] m, int cells)
        {
            const int n = ProceduralArt.FruitIconPx;
            float step = n / (float)cells;
            var dst = new float[cells * cells];
            for (int cy = 0; cy < cells; cy++)
            {
                float y0 = cy * step;
                float y1 = (cy + 1) * step;
                for (int cx = 0; cx < cells; cx++)
                {
                    float x0 = cx * step;
                    float x1 = (cx + 1) * step;
                    float acc = 0f;
                    float weight = 0f;
                    for (int y = Mathf.FloorToInt(y0); y < Mathf.CeilToInt(y1); y++)
                    {
                        float wy = Mathf.Min(y + 1, y1) - Mathf.Max(y, y0);
                        for (int x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x1); x++)
                        {
                            float w = wy * (Mathf.Min(x + 1, x1) - Mathf.Max(x, x0));
                            acc += m[(y * n) + x] * w;
                            weight += w;
                        }
                    }

                    dst[(cy * cells) + cx] = acc / weight;
                }
            }

            return dst;
        }

        // Ángulo (grados, 0-90) entre el eje principal de la silueta (momentos de segundo orden) y la vertical.
        private static float AxisFromVertical(float[] m)
        {
            const int n = ProceduralArt.FruitIconPx;
            double sx = 0;
            double sy = 0;
            int count = 0;
            for (int i = 0; i < m.Length; i++)
            {
                if (m[i] > 0.5f)
                {
                    sx += i % n;
                    sy += i / n;
                    count++;
                }
            }

            double mx = sx / count;
            double my = sy / count;
            double cxx = 0;
            double cyy = 0;
            double cxy = 0;
            for (int i = 0; i < m.Length; i++)
            {
                if (m[i] > 0.5f)
                {
                    double dx = (i % n) - mx;
                    double dy = (i / n) - my;
                    cxx += dx * dx;
                    cyy += dy * dy;
                    cxy += dx * dy;
                }
            }

            double fromX = 0.5 * System.Math.Atan2(2.0 * cxy, cxx - cyy) * (180.0 / System.Math.PI);
            return (float)(90.0 - System.Math.Abs(fromX));
        }

        // Huecos cerrados de la silueta: componentes (4-conexas) del fondo que no tocan el borde de la celda, como índices.
        private static List<List<int>> Holes(float[] m)
        {
            const int n = ProceduralArt.FruitIconPx;
            var seen = new bool[m.Length];
            var holes = new List<List<int>>();
            var stack = new Stack<int>();
            for (int start = 0; start < m.Length; start++)
            {
                if (seen[start] || m[start] > 0.5f)
                {
                    continue;
                }

                var component = new List<int>();
                bool open = false;
                seen[start] = true;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    component.Add(i);
                    int x = i % n;
                    int y = i / n;
                    open |= x == 0 || y == 0 || x == n - 1 || y == n - 1;
                    for (int k = 0; k < 4; k++)
                    {
                        int xx = x + (k == 0 ? 1 : k == 1 ? -1 : 0);
                        int yy = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        int j = (yy * n) + xx;
                        if (xx >= 0 && yy >= 0 && xx < n && yy < n && !seen[j] && m[j] <= 0.5f)
                        {
                            seen[j] = true;
                            stack.Push(j);
                        }
                    }
                }

                if (!open)
                {
                    holes.Add(component);
                }
            }

            return holes;
        }

        // El hueco contiene un disco de radio r px entero (su grosor llega a 2r + 1 px en algún sitio).
        private static bool HasDisc(List<int> hole, int r)
        {
            const int n = ProceduralArt.FruitIconPx;
            var set = new HashSet<int>(hole);
            for (int i = 0; i < hole.Count; i++)
            {
                int x = hole[i] % n;
                int y = hole[i] / n;
                bool inside = true;
                for (int dy = -r; dy <= r && inside; dy++)
                {
                    for (int dx = -r; dx <= r && inside; dx++)
                    {
                        inside = (dx * dx) + (dy * dy) > r * r || set.Contains(((y + dy) * n) + x + dx);
                    }
                }

                if (inside)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
