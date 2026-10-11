#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AF.Core.Board;
using AF.Presentation;
using AF.Presentation.Mechanics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Contenido de la cuia con Aguas turbias y Barro (07 §4.3; 02 §11 T3; 02 §9.1 M5; 02 §4.2): una dosis oculta se pinta de
    /// pardo opaco #8A7B6A con su trama y su «?» (en el shader, T15) y nunca con su tinte real, y la cuia muestra «?×n»; tras
    /// revelarse, con su tinte; el Barro tiene color propio a ΔE00 ≥ 10 de los 14 tintes, de las turbias y de las 3 aguas base
    /// (direccion-visual-v2 §2.4, regla 1), con sus grumos. Además del color que lleva la vista, se mira lo que llega al shader
    /// en la malla de <see cref="LiquidGraphic"/>: la fila del atlas de cada banda (<c>uv3.w</c>, la del patrón para
    /// daltónicos o la turbia), su patrón (<c>uv2.w</c> − 4 · dosis) y el sitio de su «?» (<c>uv0.w</c>: isótropo, de 0,30 u
    /// como mínimo y sin tocar el vidrio, o apagado). Una dosis oculta con la fila o el patrón de su fruta la delataría aunque
    /// fuera parda. Además, que los umbrales del shader y sus espejos en CPU no se separen (se lee el texto del shader) y que el
    /// disco de la fruta de la Luciérnaga se lea frente a cualquier tinte. <see cref="CuiaView"/> en un Canvas de prueba; el
    /// arte procedural se crea una vez (≈ 300 ms en EditMode).
    /// </summary>
    public sealed class MurkyMudContentTests
    {
        private const string LiquidShaderPath = "_Project/Shaders/Resources/AF_UI_Liquid.shader";

        // Lo que se manda al shader para una dosis oculta (T15): la fila turbia del atlas (tras las 14 de las frutas) y el patrón
        // turbio (trama y «?»), iguales para todas las frutas: nunca el tinte real, ninguna fila de fruta y nada que dependa de la
        // fruta oculta. El Barro: tinte 0 (agua base) y sin marcas.
        private const int HiddenTint = ProceduralArt.MurkyPatternRow;
        private const int HiddenPattern = (int)LiquidGraphic.MurkyPatternMode;
        private const int MudTint = 0;
        private const int NoPattern = -1;
        private const int NoBand = int.MinValue;

        private static readonly MethodInfo PopulateMesh = typeof(LiquidGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        private ProceduralArt _art = null!;
        private GameObject _canvas = null!;

        [OneTimeSetUp]
        public void CreateArt()
        {
            _art = new ProceduralArt("white");
            _canvas = new GameObject("MurkyMudCanvas", typeof(RectTransform), typeof(Canvas));
        }

        [OneTimeTearDown]
        public void DisposeArt()
        {
            Object.DestroyImmediate(_canvas);
            _art.Dispose();
        }

        [TearDown]
        public void DestroyViews()
        {
            Transform root = _canvas.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        [Test]
        public void Hidden_PaintedMurkyWithMark() // 07 §4.3; 02 §11 T3
        {
            CuiaView view = NewView();
            Cuia cuia = Cuia.Create(4, new[] { 8, 4, 4, 3 }, 0b0011);
            view.ShowContent(cuia, false);
            for (int k = 0; k < 2; k++)
            {
                Assert.That(Same(view.DoseColor(k), Palette.Murky), Is.True, "dosis oculta " + k + " en pardo opaco");
                Assert.That(Same(view.DoseColor(k), Palette.Tint(cuia.ColorAt(k))), Is.False, "nunca su tinte real");
                Assert.That(view.Murky.MarkVisible(k), Is.True, "«?» en la dosis oculta " + k);
                Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), "el shader pinta la trama y el «?»");
            }

            for (int k = 2; k < 4; k++)
            {
                Assert.That(Same(view.DoseColor(k), Palette.Tint(cuia.ColorAt(k))), Is.True, "dosis visible " + k);
                Assert.That(view.Murky.MarkVisible(k), Is.False, "sin «?» en la dosis visible " + k);
                Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None));
            }

            Assert.That(view.LiquidLevel, Is.EqualTo(4f).Within(0.001f), "las ocultas cuentan para la altura");
            Assert.That(view.ShownCuia, Is.EqualTo(cuia));
            Assert.That(view.Murky.BadgeCount, Is.EqualTo(2), "la cuia muestra «?×2» (02 §11 T3)");
            Assert.That(view.Murky.BadgeVisible, Is.True);
            Assert.That(view.Murky.BadgeDigit.sprite, Is.SameAs(_art.MurkyBadgeDigit(2)));
            Assert.That(LooseQuestionMarks(view), Is.EqualTo(0), "ya no hay «?» sueltos en sprites: los pinta el shader");

            // Lo que llega al shader: la oculta no lleva el tinte ni el patrón de su fruta; las visibles, el suyo.
            var tints = new int[4];
            var patterns = new int[4];
            MeshDoses(view, tints, patterns);
            for (int k = 0; k < 2; k++)
            {
                AssertHiddenInMesh(tints[k], patterns[k], cuia.ColorAt(k), "dosis oculta " + k);
            }

            for (int k = 2; k < 4; k++)
            {
                Assert.That(tints[k], Is.EqualTo(cuia.ColorAt(k)), "dosis visible " + k + ": su tinte");
            }
        }

        [Test]
        public void Hidden_MeshDoesNotDependOnTheHiddenFruit() // 07 §4.3: nada de lo que se dibuja delata la fruta oculta
        {
            CuiaView a = NewView();
            a.ShowContent(Cuia.Create(4, new[] { 8, 4, 4, 3 }, 0b0011), false);
            CuiaView b = NewView();
            b.ShowContent(Cuia.Create(4, new[] { 5, 6, 4, 3 }, 0b0011), false);
            List<UIVertex> va = Mesh(a);
            List<UIVertex> vb = Mesh(b);
            Assert.That(va.Count, Is.GreaterThan(0));
            Assert.That(vb.Count, Is.EqualTo(va.Count), "las mismas bandas");
            for (int i = 0; i < va.Count; i++)
            {
                Assert.That(SameVertex(va[i], vb[i]), Is.True, "vértice " + i + ": posición, color, tinte, patrón y brillo idénticos");
            }
        }

        [Test]
        public void Hidden_MarkStaysInItsBandInFlight_BadgeHides()
        {
            CuiaView view = NewView();
            view.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }, 0b0001), false);
            var layer = new GameObject("PourLayer", typeof(RectTransform));
            layer.transform.SetParent(_canvas.transform, false);
            view.MoveBodyTo((RectTransform)layer.transform);
            Assert.That(view.InFlight, Is.True);
            Assert.That(view.Murky.MarkVisible(0), Is.True, "en vuelo la trama y el «?» siguen dentro de su banda (los pinta el shader)");
            Assert.That(view.Murky.BadgeVisible, Is.False, "la insignia «?×n» se oculta en vuelo");
            view.ReturnBody();
            Assert.That(view.Murky.MarkVisible(0), Is.True);
            Assert.That(view.Murky.BadgeVisible, Is.True, "al volver, la insignia reaparece");
            Assert.That(view.Murky.BadgeCount, Is.EqualTo(1));
        }

        [Test]
        public void Hidden_ShaderRow_GlyphIsHalfU_HatchIsDiagonal() // 07 §4.3: «?» marfil de 0,5 u y trama diagonal
        {
            Assert.That(MurkyMarks.MarkU, Is.EqualTo(0.5f), "07 §4.3");
            Assert.That(ProceduralArt.MurkyPatternRow, Is.GreaterThan(Cuia.MaxTintColor), "la fila turbia no es la de ninguna fruta");
            Assert.That(_art.PatternAtlas.height, Is.EqualTo((ProceduralArt.MurkyPatternRow + 1) * ProceduralArt.PatternRowPx), "una fila más en el atlas");

            // La fila turbia tal como la dibuja ProceduralArt (la textura no es legible en EditMode): R = «?» marfil, G = su halo
            // de tinta, A = trama. La fila mide 0,5 × 1 u (la dosis entera).
            object canvas = typeof(ProceduralArt).GetMethod("DrawMurkyRow", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
            System.Type type = canvas.GetType();
            int w = (int)type.GetProperty("Width")!.GetValue(canvas)!;
            int h = (int)type.GetProperty("Height")!.GetValue(canvas)!;
            MethodInfo get = type.GetMethod("Get")!;
            var px = new Color[w, h];
            var args = new object[2];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    args[0] = x;
                    args[1] = y;
                    px[x, y] = (Color)get.Invoke(canvas, args)!;
                }
            }

            Assert.That(w, Is.EqualTo(ProceduralArt.PatternTilePx));
            Assert.That(h, Is.EqualTo(ProceduralArt.PatternRowPx));
            float perU = h; // 1 u por fila
            int bottom = -1;
            int top = -1;
            int left = w;
            int right = -1;
            float edge = 0f;
            float hatch = 0f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = px[x, y];
                    if (c.r >= 0.9f)
                    {
                        bottom = bottom < 0 ? y : bottom;
                        top = y;
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                    }

                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        edge = Mathf.Max(edge, Mathf.Max(c.r, c.g));
                    }

                    hatch += c.a;
                    Assert.That(px[(x + 1) % w, (y + 1) % h].a, Is.EqualTo(c.a).Within(1e-4f), "trama diagonal en (" + x + ", " + y + ")");
                    Assert.That(px[(x + (int)ProceduralArt.MurkyHatchPeriodPx) % w, y].a, Is.EqualTo(c.a).Within(1e-4f), "repetible en x");
                }
            }

            Assert.That(bottom, Is.GreaterThanOrEqualTo(0), "hay «?» marfil");
            Assert.That((top - bottom + 1) / perU, Is.EqualTo(MurkyMarks.MarkU).Within(1.5f / perU), "alto del «?» marfil (u)");
            Assert.That((bottom + top + 1) * 0.5f, Is.EqualTo(h * 0.5f).Within(1f), "centrado en la banda");
            Assert.That((left + right + 1) * 0.5f, Is.EqualTo(w * 0.5f).Within(1.5f), "centrado a lo ancho de la ventana");
            Assert.That(edge, Is.LessThan(0.02f), "el «?» y su halo no tocan el borde de la fila (sin costuras al repetir)");
            float coverage = hatch / (w * h);
            Assert.That(coverage, Is.InRange(0.15f, 0.45f), "trama: rayas finas, no un relleno");

            // En la malla: la banda oculta lleva la fila turbia y la x de la «?» en uv1.w (en reposo, la x de la cuia).
            CuiaView view = NewView();
            view.SetGeometry(Vector2.zero, 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }, 0b0001), false);
            List<UIVertex> vertices = Mesh(view);
            int seen = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex v = vertices[i];
                if (BandOf(v) == 0)
                {
                    seen++;
                    Assert.That(v.uv3.w, Is.EqualTo((float)ProceduralArt.MurkyPatternRow), "fila turbia");
                    Assert.That(v.uv1.w, Is.EqualTo(v.uv0.x - 1f).Within(1e-4f), "en reposo la «?» va centrada en la cuia");
                }
            }

            Assert.That(seen, Is.GreaterThan(2), "la banda oculta tiene malla");
        }

        [Test]
        public void Badge_ColorsFarFromTints() // direccion-visual-v2 §2.4, regla 1: superficies de más de 2 dp junto a la cuia
        {
            for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
            {
                Assert.That(ColorMath.DeltaE00(ProceduralArt.MurkyBadgeFill, Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f), "insignia frente al tinte " + tint);
            }

            Assert.That(ColorMath.DeltaE00(ProceduralArt.MurkyBadgeFill, Palette.Murky), Is.GreaterThanOrEqualTo(10f), "insignia frente a las turbias");
        }

        [Test]
        public void Revealed_PaintedWithTint() // 02 §11 T3; 04 §2.5
        {
            // Referencia: la misma cuia sin turbias, tal como llega al shader.
            CuiaView plain = NewView();
            plain.ShowContent(Cuia.Create(4, new[] { 8, 4, 4, 3 }), false);
            var plainTints = new int[4];
            var plainPatterns = new int[4];
            MeshDoses(plain, plainTints, plainPatterns);
            var tints = new int[4];
            var patterns = new int[4];

            CuiaView view = NewView();
            Cuia cuia = Cuia.Create(4, new[] { 8, 4, 4, 3 }, 0b0011);
            view.ShowContent(cuia, false);
            Cuia revealed = cuia.WithRevealed(0b0010);
            view.ShowContent(revealed, false);
            Assert.That(Same(view.DoseColor(1), Palette.Tint(4)), Is.True, "revelada: su tinte");
            Assert.That(view.Murky.MarkVisible(1), Is.False, "sin «?» tras revelarse");
            Assert.That(Same(view.DoseColor(0), Palette.Murky), Is.True, "la otra sigue oculta");
            Assert.That(view.Murky.MarkVisible(0), Is.True);
            MeshDoses(view, tints, patterns);
            Assert.That(tints[1], Is.EqualTo(4), "revelada: vuelve su tinte al shader");
            Assert.That(patterns[1], Is.EqualTo(plainPatterns[1]), "y su patrón, como si nunca hubiera estado oculta");
            AssertHiddenInMesh(tints[0], patterns[0], cuia.ColorAt(0), "la otra, aún oculta");

            view.ShowContent(cuia.WithRevealed(0b0011), false);
            Assert.That(Same(view.DoseColor(0), Palette.Tint(8)), Is.True);
            Assert.That(view.Murky.MarkMask, Is.EqualTo(0), "ningún «?» con todo revelado");
            MeshDoses(view, tints, patterns);
            Assert.That(tints, Is.EqualTo(plainTints), "todo revelado: cada dosis con su tinte");
            Assert.That(patterns, Is.EqualTo(plainPatterns), "y con su patrón");
            Assert.That(tints[0], Is.EqualTo(8));
        }

        [Test]
        public void Mud_HasOwnColor_DeltaE00() // 02 §9.1 M5 y §4.2; direccion-visual-v2 §2.4, regla 1
        {
            for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
            {
                Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f), "Barro frente al tinte " + tint);
            }

            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.Murky), Is.GreaterThanOrEqualTo(10f), "Barro frente a las turbias");
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterBlack), Is.GreaterThanOrEqualTo(10f), "Barro frente al agua negra");
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterWhite), Is.GreaterThanOrEqualTo(10f), "Barro frente al agua blanca");
            Assert.That(ColorMath.DeltaE00(Palette.Mud, Palette.WaterClear), Is.GreaterThanOrEqualTo(10f), "Barro frente al agua clara");

            CuiaView view = NewView();
            view.ShowContent(Cuia.Create(4, new[] { Cuia.MudColor, 8, 8 }), false);
            Assert.That(Same(view.DoseColor(0), Palette.Mud), Is.True, "la dosis de Barro, con su color");
            Assert.That(view.Mud.LumpsVisible(0), Is.True, "con sus grumos");
            Assert.That(view.Mud.LumpsVisible(1), Is.False);
            Assert.That(view.Murky.MarkMask, Is.EqualTo(0), "el Barro no es una dosis oculta");
            var tints = new int[4];
            var patterns = new int[4];
            MeshDoses(view, tints, patterns);
            Assert.That(tints[0], Is.EqualTo(MudTint), "el Barro no lleva tinte de fruta al shader");
            Assert.That(patterns[0], Is.EqualTo(NoPattern), "ni patrón");
            Assert.That(tints[1], Is.EqualTo(8), "la fruta de encima, con el suyo");
            Assert.That(tints[3], Is.EqualTo(NoBand), "la dosis vacía no tiene banda");

            view.ShowContent(Cuia.Create(4, new[] { 8, 8, 8 }), false);
            Assert.That(view.Mud.LumpMask, Is.EqualTo(0), "sin Barro, sin grumos");
        }

        [Test]
        public void ShaderConstants_MatchLiquidGraphic() // hallazgo 3: los umbrales del shader y sus espejos en CPU no se separan
        {
            // Solo el código: un #define o un uso que solo quede en un comentario no cuenta (revisión de T15, n.º 2).
            string code = WithoutComments(File.ReadAllText(Path.Combine(Application.dataPath, LiquidShaderPath)));
            var defines = new Dictionary<string, float>();
            foreach (Match m in Regex.Matches(code, @"^\s*#define\s+(AF_[A-Z0-9_]+)\s+(-?[0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.Multiline))
            {
                defines[m.Groups[1].Value] = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            }

            var mirrors = new (string Name, float Value)[]
            {
                ("AF_PATTERN_OPACITY", Palette.PatternOpacity),
                ("AF_MATTE_MODE", LiquidGraphic.MattePatternMode),
                ("AF_MURKY_MODE", LiquidGraphic.MurkyPatternMode),
                ("AF_MURKY_SPAN", LiquidGraphic.MurkyProgressSpan),
                ("AF_HATCH_OPACITY", LiquidGraphic.MurkyHatchOpacity),
                ("AF_HATCH_FADE_FROM", LiquidGraphic.HatchFadeFrom),
                ("AF_HATCH_FADE_TO", LiquidGraphic.HatchFadeTo),
                ("AF_FRUIT_PATTERN_FROM", LiquidGraphic.FruitPatternFrom),
                ("AF_FRUIT_PATTERN_TO", LiquidGraphic.FruitPatternTo),
                ("AF_MARK_FADE_FROM", LiquidGraphic.MarkFadeFrom),
                ("AF_MARK_FADE_TO", LiquidGraphic.MarkFadeTo),
                ("AF_MARK_HALO", LiquidGraphic.MarkHaloOpacity),
                ("AF_MARK_HALO_FADE_FROM", LiquidGraphic.MarkHaloFadeFrom),
                ("AF_MARK_HALO_FADE_TO", LiquidGraphic.MarkHaloFadeTo),
                ("AF_MARK_WHITE_FROM", LiquidGraphic.MarkWhiteFrom),
                ("AF_MARK_WHITE_TO", LiquidGraphic.MarkWhiteTo),
                ("AF_MARK_WOBBLE_U", LiquidGraphic.MarkWobbleU),
                ("AF_GLYPH_MIN_U", LiquidGraphic.GlyphMinU),
                ("AF_GLYPH_MAX_U", LiquidGraphic.GlyphMaxU),
                ("AF_GLYPH_ROOM_RATIO", LiquidGraphic.GlyphRoomRatio),
                ("AF_GLYPH_FADE_FROM", LiquidGraphic.GlyphFadeFrom),
                ("AF_GLYPH_FADE_TO", LiquidGraphic.GlyphFadeTo),
                ("AF_GLYPH_PACK_SCALE", LiquidGraphic.GlyphPackScale),
                ("AF_GLYPH_CENTER_STEPS", LiquidGraphic.GlyphCenterSteps),
                ("AF_GLYPH_CENTER_PER_U", LiquidGraphic.GlyphCenterPerU),
                ("AF_GLYPH_CENTER_OFFSET", LiquidGraphic.GlyphCenterOffset),
                ("AF_GLYPH_ROOM_PER_U", LiquidGraphic.GlyphRoomPerU),
                ("AF_FIREFLY_FRONT_SPAN", LiquidGraphic.FireflyFrontSpan),
                ("AF_FIREFLY_EDGE_U", LiquidGraphic.FireflyEdgeU),
                ("AF_FIREFLY_TURN_FROM", LiquidGraphic.FireflyTurnFrom),
                ("AF_FIREFLY_TURN_TO", LiquidGraphic.FireflyTurnTo),
                ("AF_FIREFLY_TURN_OFF", LiquidGraphic.FireflyTurnOff),
                ("AF_FIREFLY_PATTERN_FROM", LiquidGraphic.FireflyPatternFrom),
                ("AF_FIREFLY_PATTERN_TO", LiquidGraphic.FireflyPatternTo),
            };

            var mirrored = new HashSet<string>();
            foreach ((string name, float value) in mirrors)
            {
                mirrored.Add(name);
                Assert.That(defines.ContainsKey(name), Is.True, "el shader define " + name);
                Assert.That(defines[name], Is.EqualTo(value).Within(1e-6f), name + ": el shader y LiquidGraphic dicen lo mismo");
                string uses = Regex.Replace(code, @"^\s*#define\s+" + name + @"\b[^\r\n]*", string.Empty, RegexOptions.Multiline);
                Assert.That(Regex.Matches(uses, @"\b" + name + @"\b").Count, Is.GreaterThanOrEqualTo(1), name + ": el shader lo usa fuera de su #define");
            }

            foreach (string name in defines.Keys)
            {
                Assert.That(mirrored.Contains(name), Is.True, name + ": toda constante AF_ del shader tiene su espejo en C# (y su test)");
            }

            Assert.That(LiquidGraphic.FireflyEdgeU, Is.LessThanOrEqualTo(0.15f), "borde del frente de luz estrecho (≤ 0,15 u)");
            Assert.That(LiquidGraphic.GlyphMaxU, Is.EqualTo(MurkyMarks.MarkU), "en reposo, el «?» de 0,5 u de 07 §4.3");
            Assert.That(LiquidGraphic.HatchFadeTo, Is.LessThan(LiquidGraphic.FruitPatternFrom), "la trama se va antes de que entre el patrón");
            Assert.That(LiquidGraphic.FireflyFrontSpan, Is.LessThan(LiquidGraphic.FireflyPatternFrom), "con la Luciérnaga, también");
            Assert.That(CuiaGeometry.HeightU(Cuia.MaxCapacity), Is.LessThan(LiquidGraphic.GlyphPackScale), "el alto de la cuia cabe bajo el empaquetado");
        }

        [Test]
        public void Hidden_GlyphPackedOnHeight_AtRest() // hallazgo 14; 07 §4.3: en reposo, el «?» de 0,5 u a media banda
        {
            CuiaView view = NewView();
            view.SetGeometry(Vector2.zero, 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }, 0b0001), false);
            float height = CuiaGeometry.HeightU(4);
            float center = 0.5f - (0.5f * CuiaGeometry.TopFaceU);
            int code = LiquidGraphic.PackGlyph(center, 1f);
            LiquidGraphic.UnpackGlyph(code, out float centerBack, out float roomBack);
            Assert.That(centerBack, Is.EqualTo(center).Within(1e-6f), "0,3 u de capa se empaqueta exacto");
            Assert.That(roomBack, Is.EqualTo(1f).Within(1e-6f), "y el hueco de 1 u");
            Assert.That(LiquidGraphic.GlyphSizeU(roomBack), Is.EqualTo(MurkyMarks.MarkU), "«?» de 0,5 u");
            Assert.That(LiquidGraphic.GlyphFade(roomBack), Is.EqualTo(1f));

            List<UIVertex> vertices = Mesh(view);
            int hidden = 0;
            int normal = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex v = vertices[i];
                if (BandOf(v) == 0)
                {
                    hidden++;
                    Assert.That(v.uv0.w, Is.EqualTo(height + (LiquidGraphic.GlyphPackScale * code)), "la banda turbia lleva el sitio de su «?» sobre el alto");
                    float packed = Mathf.Floor(v.uv0.w / LiquidGraphic.GlyphPackScale);
                    Assert.That(v.uv0.w - (LiquidGraphic.GlyphPackScale * packed), Is.EqualTo(height).Within(4e-3f), "y el shader recupera el alto (a medio ulp de float, ≤ 0,004 u)");
                }
                else
                {
                    normal++;
                    Assert.That(v.uv0.w, Is.EqualTo(height), "las bandas normales, sin cambios");
                }
            }

            Assert.That(hidden, Is.GreaterThan(2));
            Assert.That(normal, Is.GreaterThan(2));
            Assert.That(view.Liquid.MurkyGlyphPlacement(0, out float cx, out float cy, out float size, out float room), Is.True);
            Assert.That(cx, Is.EqualTo(0f).Within(1e-5f), "en el eje");
            Assert.That(cy, Is.EqualTo(CuiaGeometry.CavityBottomU + center).Within(1e-4f), "a media banda entre sus arcos (MurkyMarks)");
            Assert.That(size, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(room, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Hidden_GlyphTilted_IsotropicMinSizeOrOff() // hallazgo 14: inclinada, ≥ 0,30 u en la cuña sin tocar el vidrio, o apagado
        {
            CuiaView view = NewView();
            view.SetGeometry(Vector2.zero, 48f, 64f);
            view.ShowContent(Cuia.Create(4, new[] { 8, 4 }, 0b0001), false);
            int shown = 0;
            int off = 0;
            float maxJump = 0f;
            SweepTilt(view, -1, ref shown, ref off, ref maxJump);
            SweepTilt(view, 1, ref shown, ref off, ref maxJump);
            TestContext.WriteLine("Cuia de 4 inclinada de 0,5° a 92° a cada lado: «?» a la vista en " + shown + " pasos y apagado en " + off
                + "; el salto mayor del centro entre pasos de 0,5° es de " + maxJump.ToString("F3") + " u.");
            Assert.That(shown, Is.GreaterThan(200), "a la vista casi todo el giro");
            Assert.That(maxJump, Is.LessThan(0.3f), "el «?» se mueve sin saltos al inclinarse");
        }

        [Test]
        public void Turn_CarriesFruitRowAndTint() // hallazgos 2, 9 y 13: con la Luciérnaga entra el patrón de la fruta
        {
            CuiaView view = NewView();
            view.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }, 0b0001), false);
            view.Liquid.SetDose(0, Palette.Tint(8), 1f, 8, LiquidGraphic.MurkyLook.Hidden);
            Assert.That(view.Liquid.DoseTintAt(0), Is.EqualTo(0), "oculta: sin tinte");
            view.Liquid.SetDose(0, Palette.Murky, 1f, 8, LiquidGraphic.MurkyLook.Turn, 0.7f);
            Assert.That(view.Liquid.DoseTintAt(0), Is.EqualTo(8), "con la Luciérnaga: el tinte de la fruta que se revela");
            List<UIVertex> vertices = Mesh(view);
            int seen = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (BandOf(vertices[i]) == 0)
                {
                    seen++;
                    Assert.That(vertices[i].uv3.w, Is.EqualTo(LiquidGraphic.FruitRowCode(8)), "la fila de la fruta (+ 0,5: marcas en marfil)");
                }
            }

            Assert.That(seen, Is.GreaterThan(2));
            Assert.That(LiquidGraphic.FruitPatternWeight(LiquidGraphic.MurkyLook.Turn, 0f), Is.EqualTo(0f));
            Assert.That(LiquidGraphic.FruitPatternWeight(LiquidGraphic.MurkyLook.Turn, 1f), Is.EqualTo(1f));
            Assert.That(LiquidGraphic.MurkyHatchAmount(LiquidGraphic.MurkyLook.Turn, 0f), Is.EqualTo(LiquidGraphic.MurkyHatchOpacity), "al empezar, como oculta");
            Assert.That(LiquidGraphic.MurkyHatchAmount(LiquidGraphic.MurkyLook.Turn, LiquidGraphic.FireflyFrontSpan), Is.EqualTo(0f).Within(1e-6f), "el frente se llevó la trama");
            Assert.That(LiquidGraphic.FireflyLightAt(0f, 0f), Is.EqualTo(0f).Within(1e-6f), "al empezar, todo pardo");
            Assert.That(LiquidGraphic.FireflyLightAt(LiquidGraphic.FireflyFrontSpan, 1f), Is.EqualTo(1f).Within(1e-6f), "y al salir el frente, todo aclarado");
        }

        [Test]
        public void FireflyDisc_ContrastWithEveryTint() // hallazgo 15; direccion-visual-v2 §2.4, regla 2: iconos ≥ 3:1
        {
            Color ink = ColorMath.CompositeLinear(Palette.UiInk, 0.8f, Palette.UiPaper); // contorno del icono de la fruta sobre el disco
            for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
            {
                Color band = Palette.Tint(tint);
                float disc = Mathf.Max(ColorMath.ContrastRatio(Palette.UiPaper, band), ColorMath.ContrastRatio(ProceduralArt.MurkyFruitDiscRim, band));
                Assert.That(disc, Is.GreaterThanOrEqualTo(3f), "tinte " + tint + ": el disco (marfil o su filo) frente a la banda");
                float icon = Mathf.Max(ColorMath.ContrastRatio(band, Palette.UiPaper), ColorMath.ContrastRatio(ink, Palette.UiPaper));
                Assert.That(icon, Is.GreaterThanOrEqualTo(3f), "tinte " + tint + ": el icono (o su contorno) frente al disco");
            }

            Sprite sprite = _art.MurkyFruitDisc;
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite, Is.SameAs(_art.MurkyFruitDisc), "se genera una vez");
            Assert.That(MurkyMarks.FireflyDiscU, Is.EqualTo(0.8f), "disco de 0,80 u");
            Assert.That(MurkyMarks.FireflyFruitU, Is.LessThan(MurkyMarks.FireflyDiscU), "el icono, dentro del disco");
        }

        [Test]
        public void FireflyParts_OnlyInMurkyCuias_PreparedBeforeTheReveal() // revisión de T15, n.º 4; arquitectura del paso 1 §12
        {
            // Con un arte nuevo (el de la clase ya generó el disco): una cuia sin ocultas no crea discos, iconos ni la textura del
            // disco; una turbia los prepara al mostrar sus ocultas (Prewarm, la carga del nivel), y el revelado con la Luciérnaga
            // ya no crea nada dentro del paso.
            var art = new ProceduralArt("white");
            CuiaView? clear = null;
            CuiaView? murky = null;
            try
            {
                clear = CuiaView.Create((RectTransform)_canvas.transform, 0, 4, art, _ => { }, (_, __) => { });
                clear.ShowContent(Cuia.Create(4, new[] { 8, 4, 4 }), false);
                Assert.That(clear.Murky.FireflyPartsReady, Is.False, "una cuia sin ocultas no prepara la Luciérnaga");
                Assert.That(FireflyImages(clear), Is.EqualTo(0), "ni crea sus discos e iconos");
                Assert.That(art.HasMurkyFruitDisc, Is.False, "ni la textura del disco");

                murky = CuiaView.Create((RectTransform)_canvas.transform, 1, 4, art, _ => { }, (_, __) => { });
                Assert.That(FireflyImages(murky), Is.EqualTo(0), "al construirse, la cuia aún no sabe si será turbia");
                Cuia cuia = Cuia.Create(4, new[] { 8, 4, 4 }, 0b0001);
                murky.ShowContent(cuia, false);
                Assert.That(murky.Murky.FireflyPartsReady, Is.True, "la cuia turbia se prepara al mostrar su oculta");
                Assert.That(FireflyImages(murky), Is.EqualTo(2 * 4), "un disco y un icono por dosis");
                Assert.That(art.HasMurkyFruitDisc, Is.True, "y la textura del disco, antes de cualquier paso");
                Assert.That(murky.Murky.Badge.transform.GetSiblingIndex(), Is.EqualTo(2 * 4), "los discos y los iconos, bajo la insignia");

                Assert.That(murky.PlayReveal(cuia.HiddenMask, RevealStyle.Firefly, 1f), Is.GreaterThan(0f));
                Assert.That(FireflyImages(murky), Is.EqualTo(2 * 4), "el revelado no crea imágenes");
                Assert.That(murky.Murky.FruitDisc(0).sprite, Is.SameAs(art.MurkyFruitDisc));
                Assert.That(murky.Murky.FruitImage(0).sprite, Is.SameAs(art.FruitFlight(8)), "el icono de su fruta");
                murky.SnapMarks();
            }
            finally
            {
                if (clear != null)
                {
                    Object.DestroyImmediate(clear.gameObject);
                }

                if (murky != null)
                {
                    Object.DestroyImmediate(murky.gameObject);
                }

                art.Dispose();
            }
        }

        [Test]
        public void HiddenMud_RevealedMatte() // rama defensiva de MurkyMarks (T20): LevelValidation no deja Barro oculto, pero si llega, mate
        {
            Cuia cuia = Cuia.Create(4, new[] { Cuia.MudColor, 4, 4 }, 0b0001);
            RevealStyle[] styles = { RevealStyle.Rule, RevealStyle.Firefly };
            for (int s = 0; s < styles.Length; s++)
            {
                CuiaView view = NewView();
                view.ShowContent(cuia, false);
                float ms = view.PlayReveal(cuia.HiddenMask, styles[s], 1f);
                Assert.That(ms, Is.GreaterThan(0f), styles[s] + ": revela");
                for (float t = 0f; t < ms && view.Murky.IsAnimating; t += 10f)
                {
                    view.Tick(10f);
                }

                Assert.That(view.Murky.IsAnimating, Is.False, styles[s] + ": terminó");
                Assert.That(view.Liquid.IsMatteAt(0), Is.True, styles[s] + ": el Barro revelado, mate como lo pinta MudMarks.PaintMud");
                Assert.That(view.Liquid.DoseColorAt(0), Is.EqualTo(Palette.Mud), styles[s] + ": de Palette.Mud");
                Assert.That(view.Liquid.DoseTintAt(0), Is.EqualTo(0), styles[s] + ": sin tinte de fruta");
                Assert.That(view.Liquid.IsMatteAt(1), Is.False, styles[s] + ": la fruta de encima, con su brillo");
            }
        }

        [Test]
        public void Tint15_IsMud()
        {
            Assert.That(Palette.Tint(Cuia.MudColor), Is.EqualTo(Palette.Mud));
            Assert.That(Same(Palette.Tint(Cuia.MudColor), Color.magenta), Is.False, "el Barro ya no sale magenta");
        }

        private CuiaView NewView(int capacity = 4)
        {
            return CuiaView.Create((RectTransform)_canvas.transform, 0, capacity, _art, _ => { }, (_, __) => { });
        }

        private static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.005f;
        }

        // El texto de un shader sin sus comentarios (de bloque y de línea).
        private static string WithoutComments(string text)
        {
            string noBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(noBlocks, @"//[^\r\n]*", string.Empty);
        }

        // Imágenes de la Luciérnaga (discos e iconos de fruta, a la vista o no) bajo las marcas de turbias: las que no son la
        // insignia ni sus glifos.
        private static int FireflyImages(CuiaView view)
        {
            int n = 0;
            Image[] images = view.Murky.Root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != view.Murky.Badge && !images[i].transform.IsChildOf(view.Murky.Badge.transform))
                {
                    n++;
                }
            }

            return n;
        }

        // Una dosis oculta en la malla: ni el tinte de su fruta ni el de ninguna otra (su fila del atlas la delataría), y el
        // tinte y el patrón de hoy (HiddenTint y HiddenPattern).
        private static void AssertHiddenInMesh(int tint, int pattern, int realTint, string what)
        {
            Assert.That(tint, Is.Not.EqualTo(NoBand), what + ": tiene banda");
            Assert.That(tint, Is.Not.EqualTo(realTint), what + ": nunca el tinte real");
            Assert.That(tint < 1 || tint > Cuia.MaxTintColor, Is.True, what + ": ninguna fila de fruta del atlas (tinte " + tint + ")");
            Assert.That(tint, Is.EqualTo(HiddenTint), what + ": tinte");
            Assert.That(pattern, Is.EqualTo(HiddenPattern), what + ": patrón");
        }

        // Rellena la malla del líquido como uGUI (OnPopulateMesh) y devuelve, por dosis, el tinte (uv3.w) y el patrón
        // (uv2.w − 4 · dosis: −1 sin marcas, 0 tinta, 1 papel) de su banda; NoBand si la dosis no tiene banda. Todos los
        // vértices de una banda llevan los mismos.
        private static void MeshDoses(CuiaView view, int[] tints, int[] patterns)
        {
            for (int k = 0; k < tints.Length; k++)
            {
                tints[k] = NoBand;
                patterns[k] = NoBand;
            }

            List<UIVertex> vertices = Mesh(view);
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex v = vertices[i];
                int k = BandOf(v);
                int pattern = Mathf.RoundToInt(v.uv2.w - (4f * k));
                int tint = Mathf.RoundToInt(v.uv3.w);
                Assert.That(k >= 0 && k < tints.Length, Is.True, "dosis del vértice " + i + ": " + k);
                if (tints[k] == NoBand)
                {
                    tints[k] = tint;
                    patterns[k] = pattern;
                }
                else
                {
                    Assert.That(tint, Is.EqualTo(tints[k]), "vértice " + i + " de la dosis " + k + ": tinte");
                    Assert.That(pattern, Is.EqualTo(patterns[k]), "vértice " + i + " de la dosis " + k + ": patrón");
                }
            }
        }

        // Dosis de un vértice como la decodifica el shader: floor((uv2.w + 1,5) / 4) (el patrón vive en [−1,25; 2,5)).
        private static int BandOf(UIVertex v)
        {
            return Mathf.FloorToInt((v.uv2.w + 1.5f) / 4f);
        }

        // Imágenes a la vista bajo las marcas de turbias que no son la insignia ni sus glifos («?» sueltos).
        private static int LooseQuestionMarks(CuiaView view)
        {
            int n = 0;
            Image[] images = view.Murky.Root.GetComponentsInChildren<Image>(false);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != view.Murky.Badge && !images[i].transform.IsChildOf(view.Murky.Badge.transform))
                {
                    n++;
                }
            }

            return n;
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

        private static bool SameVertex(UIVertex a, UIVertex b)
        {
            return a.position == b.position && a.color.Equals(b.color) && a.uv0 == b.uv0 && a.uv1 == b.uv1 && a.uv2 == b.uv2 && a.uv3 == b.uv3;
        }

        // Inclina la cuia de view de 0,5° a 92° (con el signo sign) y en cada paso comprueba el «?» de la dosis 0: a la vista
        // solo si cabe, de 0,30 u como mínimo, dentro de su hueco y del vidrio, y lo mismo en la malla; cuenta los pasos con y
        // sin «?» y el mayor salto de su centro entre pasos.
        private static void SweepTilt(CuiaView view, int sign, ref int shown, ref int off, ref float maxJump)
        {
            LiquidGraphic liquid = view.Liquid;
            float prevX = 0f;
            float prevY = CuiaGeometry.CavityBottomU + 0.3f;
            for (float deg = 0.5f; deg <= 92.01f; deg += 0.5f)
            {
                float tilt = sign * deg;
                liquid.SetTilt(tilt);
                string at = tilt.ToString("F1") + "°";
                Assert.That(liquid.MurkyGlyphPlacement(0, out float cx, out float cy, out float size, out float room), Is.True, at);
                bool drawn = LiquidGraphic.GlyphFade(room) > 0.001f;
                Assert.That(view.Murky.MarkVisible(0), Is.EqualTo(drawn), at + ": el «?» se ve solo si cabe");
                if (drawn)
                {
                    shown++;
                    Assert.That(size, Is.GreaterThanOrEqualTo(LiquidGraphic.GlyphMinU - 1e-4f), at + ": de 0,30 u como mínimo");
                    Assert.That(size, Is.LessThanOrEqualTo(room + 1e-3f), at + ": cabe en su hueco");
                    AssertGlyphInsideGlass(cx, cy, size, tilt, view.Capacity, at);
                    maxJump = Mathf.Max(maxJump, Mathf.Sqrt(((cx - prevX) * (cx - prevX)) + ((cy - prevY) * (cy - prevY))));
                }
                else
                {
                    off++;
                }

                prevX = cx;
                prevY = cy;

                // La malla lleva el mismo hueco que dice la colocación (cuantizado).
                List<UIVertex> vertices = Mesh(view);
                bool found = false;
                for (int i = 0; i < vertices.Count; i++)
                {
                    UIVertex v = vertices[i];
                    if (BandOf(v) != 0 || v.uv2.w < 1.5f)
                    {
                        continue;
                    }

                    found = true;
                    int code = (int)Mathf.Floor(v.uv0.w / LiquidGraphic.GlyphPackScale);
                    LiquidGraphic.UnpackGlyph(code, out _, out float meshRoom);
                    Assert.That(meshRoom, Is.EqualTo(room).Within(1e-5f), at + ": el hueco de la malla");
                    break;
                }

                Assert.That(found, Is.True, at + ": la banda turbia tiene malla");
            }
        }

        // El «?» de alto sizeU centrado en (cx, cy) (coordenadas de la cuia) con la cuia inclinada tiltDeg: las esquinas de su
        // caja en pantalla (alto × 0,7 de ancho, con su halo) quedan dentro de la silueta interior, así que el punto del «?» no
        // toca el vidrio.
        private static void AssertGlyphInsideGlass(float cx, float cy, float sizeU, float tiltDeg, int capacity, string where)
        {
            float rad = tiltDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            float e0 = (cx * cs) - (cy * sn);
            float d0 = (cx * sn) + (cy * cs);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    float e = e0 + (sx * 0.35f * sizeU);
                    float d = d0 + (sy * 0.5f * sizeU);
                    float x = (e * cs) + (d * sn);
                    float y = (d * cs) - (e * sn);
                    float floor = CuiaGeometry.CavityFloorU(x);
                    float wall = CuiaGeometry.InnerHalfWidth(Mathf.Max(y, CuiaGeometry.CavityBottomU), capacity);
                    Assert.That(y, Is.GreaterThanOrEqualTo(floor - 0.02f), where + ": la esquina (" + sx + ", " + sy + ") del «?» no baja del fondo de vidrio");
                    Assert.That(Mathf.Abs(x), Is.LessThanOrEqualTo(wall + 0.02f), where + ": ni sale por la pared");
                }
            }
        }
    }
}
