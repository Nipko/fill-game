#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de los Patrones de fruta (T-E de la prueba del 2026-10-09; <c>[Explicit]</c>, no forman parte de la
    /// suite): <c>90_patterns_l63</c> (desde el 41: patrón al 20 % e icono en cada dosis visible), <c>90b_patterns_pour_l63</c>
    /// y <c>90c_patterns_fill_l63</c> (a mitad de un vertido: los iconos siguen a sus bandas en el origen inclinado y en la
    /// dosis que se llena),
    /// <c>91_patterns_l40</c> (hasta el 40: patrón al 15 % y sin icono), <c>92_patterns_lift_l7</c> y
    /// <c>92b_patterns_rest_l7</c> (el icono solo con la cuia levantada; 07 §3, regla 2), <c>93_patterns_murky_l100</c> (las
    /// turbias sin icono ni patrón), <c>93b_patterns_barro_l123</c> (el Barro sin icono) y <c>94_fruit_icons_atlas</c> (los 14
    /// iconos de 07 §3 sobre su tinte, × 3). De cada captura del tablero, un recorte a tamaño real (<c>*_zoom</c>) para mirar a
    /// 48 dp la legibilidad, los solapes y la línea D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.FruitPatternSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class FruitPatternSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CapturePatternsL63()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.20f).Within(1e-6f));
            SnapshotKit.Save(dir, "90_patterns_l63");
            SaveBoardCrop(dir, "90_patterns_l63_zoom", boot);

            // A mitad de un vertido de 2 dosis o más: el origen inclinado y el destino llenándose.
            BoardState state = boot.Flow!.Session.Current;
            int source = -1;
            int target = -1;
            for (int pass = 0; pass < 2 && source < 0; pass++)
            {
                // Mejor a una cuia con líquido (el icono de la dosis de abajo sigue a la vista); si no hay, a una vacía.
                for (int a = 0; a < state.Count && source < 0; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        if (a != b && state[a].Count >= 2 && (pass == 1 || state[b].Count > 0) && PourRules.IsLegal(state, a, b))
                        {
                            source = a;
                            target = b;
                            break;
                        }
                    }
                }
            }

            Assert.That(source, Is.GreaterThanOrEqualTo(0));
            boot.TapCuia(source);
            boot.TapCuia(target);
            CuiaView src = boot.View!.Views[source];
            CuiaView dst = boot.View.Views[target];
            int receiving = dst.ShownCuia.Count;
            for (int t = 0; t < 2000 && Mathf.Abs(src.BodyAngle) < 40f; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
            }

            Assert.That(Mathf.Abs(src.BodyAngle), Is.GreaterThanOrEqualTo(40f), "el origen se inclina");
            SnapshotKit.Save(dir, "90b_patterns_pour_l63");
            SaveCrop(dir, "90b_patterns_pour_l63_zoom", src.Body, dst.Root, 20f, 20f);

            // La dosis que se llena, hacia la mitad: su icono aparece y crece con ella.
            for (int t = 0; t < 2000 && dst.DoseFill(receiving) < 0.55f; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
            }

            SnapshotKit.Save(dir, "90c_patterns_fill_l63");
            SaveCrop(dir, "90c_patterns_fill_l63_zoom", src.Body, dst.Root, 20f, 20f);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
        }

        [UnityTest]
        public IEnumerator CapturePatternsL40()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(40);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.15f).Within(1e-6f));
            SnapshotKit.Save(dir, "91_patterns_l40");
            SaveBoardCrop(dir, "91_patterns_l40_zoom", boot);
        }

        [UnityTest]
        public IEnumerator CapturePatternsLiftL7()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(7);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardState state = boot.Flow!.Session.Current;
            int source = -1;
            int best = 0;
            for (int i = 0; i < state.Count; i++)
            {
                if (PourRules.CanBeSource(state, i) && state[i].Count > best)
                {
                    source = i;
                    best = state[i].Count;
                }
            }

            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 300);
            SnapshotKit.Save(dir, "92_patterns_lift_l7");
            SaveBoardCrop(dir, "92_patterns_lift_l7_zoom", boot);
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Deselected));
            PlayModeHelpers.Step(boot, 300);
            SnapshotKit.Save(dir, "92b_patterns_rest_l7");
            SaveBoardCrop(dir, "92b_patterns_rest_l7_zoom", boot);
        }

        [UnityTest]
        public IEnumerator CapturePatternsMurkyL100()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(100);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Save(dir, "93_patterns_murky_l100");
            SaveBoardCrop(dir, "93_patterns_murky_l100_zoom", boot);
        }

        [UnityTest]
        public IEnumerator CapturePatternsBarroL123()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(123);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Save(dir, "93b_patterns_barro_l123");
            BoardState state = boot.Flow!.Session.Current;
            int mud = 0;
            for (int i = 0; i < state.Count; i++)
            {
                for (int k = 0; k < state[i].Count; k++)
                {
                    mud = state[i].ColorAt(k) == Cuia.MudColor ? i : mud;
                }
            }

            int left = Mathf.Max(0, mud - 1);
            SaveCrop(dir, "93b_patterns_barro_l123_zoom", boot.View!.Views[left].Root, boot.View.Views[mud].Root, 12f, 12f);
        }

        [Test]
        public void CaptureIconAtlas()
        {
            // Los 14 iconos de 07 §3 sobre una dosis de su tinte, del color de sus marcas (Palette.PatternBase) al
            // LiquidGraphic.FruitIconOpacity, con su contorno oscurecido LiquidGraphic.FruitIconRimDarken donde lo llevan
            // (ProceduralArt.FruitIconHasRim: Achiote y Río Negro), × 3 (cada celda de 64 px a 192), en 2 filas de 7.
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots");
            Directory.CreateDirectory(dir);
            const int scale = 3;
            const int cell = ProceduralArt.FruitIconPx * scale;
            const int pad = 12;
            int w = (7 * (cell + pad)) + pad;
            int h = (2 * (cell + pad)) + pad;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Palette.Panel.Bottom;
            }

            for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
            {
                float[] mask = ProceduralArt.FruitIconMask(tint);
                float[] rim = ProceduralArt.FruitIconRimMask(tint);
                int col = (tint - 1) % 7;
                int row = 1 - ((tint - 1) / 7);
                int ox = pad + (col * (cell + pad));
                int oy = pad + (row * (cell + pad));
                Color dose = Palette.Tint(tint);
                Color mark = Palette.PatternBase(tint);
                for (int y = 0; y < cell; y++)
                {
                    for (int x = 0; x < cell; x++)
                    {
                        float m = Sample(mask, (x + 0.5f) / scale, (y + 0.5f) / scale);
                        float g = Sample(rim, (x + 0.5f) / scale, (y + 0.5f) / scale);
                        Color under = dose * (1f - (LiquidGraphic.FruitIconRimDarken * g)); // en sRGB ≈ la raíz cuadrada del shader
                        under.a = 1f;
                        pixels[((oy + y) * w) + ox + x] = Color.Lerp(under, mark, LiquidGraphic.FruitIconOpacity * m);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, "94_fruit_icons_atlas.png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        // Muestra bilineal de la máscara de 64 × 64 px en (px, py) píxeles.
        private static float Sample(float[] mask, float px, float py)
        {
            const int n = ProceduralArt.FruitIconPx;
            float fx = Mathf.Clamp(px - 0.5f, 0f, n - 1f);
            float fy = Mathf.Clamp(py - 0.5f, 0f, n - 1f);
            int x0 = (int)fx;
            int y0 = (int)fy;
            int x1 = Mathf.Min(x0 + 1, n - 1);
            int y1 = Mathf.Min(y0 + 1, n - 1);
            float tx = fx - x0;
            float ty = fy - y0;
            float a = Mathf.Lerp(mask[(y0 * n) + x0], mask[(y0 * n) + x1], tx);
            float b = Mathf.Lerp(mask[(y1 * n) + x0], mask[(y1 * n) + x1], tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static IEnumerator LoadAttached(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 300);
        }

        // Recorte a tamaño real de la mitad izquierda del tablero (las 3 primeras cuias o menos), con su margen.
        private static void SaveBoardCrop(string dir, string name, GameBootstrap boot)
        {
            CuiaView[] views = boot.View!.Views;
            int last = Mathf.Min(views.Length, 3) - 1;
            SaveCrop(dir, name, views[0].Root, views[last].Root, 12f, 12f);
        }

        private static void SaveCrop(string dir, string name, RectTransform target, float marginX, float marginY)
        {
            SaveCrop(dir, name, target, target, marginX, marginY);
        }

        // Recorte a tamaño real (3 px/dp) del rectángulo que abarcan a y b, con marginX y marginY dp de margen.
        private static void SaveCrop(string dir, string name, RectTransform a, RectTransform b, float marginX, float marginY)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture rt = cam.targetTexture;
            var corners = new Vector3[4];
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            for (int r = 0; r < 2; r++)
            {
                (r == 0 ? a : b).GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = cam.WorldToScreenPoint(corners[i]);
                    xMin = Mathf.Min(xMin, p.x);
                    yMin = Mathf.Min(yMin, p.y);
                    xMax = Mathf.Max(xMax, p.x);
                    yMax = Mathf.Max(yMax, p.y);
                }
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin - (marginX * 3f)), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin - (marginY * 3f)), 0, rt.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax + (marginX * 3f)), x0 + 1, rt.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax + (marginY * 3f)), y0 + 1, rt.height);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, x1 - x0, y1 - y0), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
