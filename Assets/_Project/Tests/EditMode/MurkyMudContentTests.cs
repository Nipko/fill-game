#nullable enable
using System.Collections.Generic;
using System.Reflection;
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
    /// daltónicos o la turbia) y su patrón (<c>uv2.w</c> − 4 · dosis). Una dosis oculta con la fila o el patrón de su fruta la
    /// delataría aunque fuera parda. <see cref="CuiaView"/> en un Canvas de prueba; el arte procedural se crea una vez
    /// (≈ 300 ms en EditMode).
    /// </summary>
    public sealed class MurkyMudContentTests
    {
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

        // Dosis de un vértice como la decodifica el shader: floor((uv2.w + 1,5) / 4) (el patrón vive en [−1, 2,5)).
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
    }
}
