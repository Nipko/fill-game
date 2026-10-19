#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Board;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del agua negra en ámbar-té translúcido (D-029; guion de la intro §18, Q14-A; tarea DC13;
    /// <c>[Explicit]</c>, no forman parte de la suite): <c>dc13_l61_llenar</c> (el 61, mundo 3, en «Llenar y teñir» con el agua
    /// del río ya en las cuias y sin teñir, en reposo tras los chorros, junto a las del tinte 14 de la capa de abajo; las dos
    /// cuias oscuras sin agua, la 0 y la 5, son cavidades vacías, no agua negra), <c>dc13_l61_tablero</c> (el tablero en reposo
    /// con, en las vistas de las tres primeras cuias de la fila 1, de izquierda a derecha, el agua negra de ahora, la de antes,
    /// #4A2C14, las dos al mismo 70 %, y el Barro bajo el tinte 14), <c>dc13_l61_tablero_zoom</c> (recorte de esas tres cuias
    /// ampliado × 3) y <c>dc13_l40_llegada_encontro</c> (la franja de llegada del Encuentro de las Aguas tras ganar el 40, con su
    /// mini cuia negra y blanca). Una línea <c>AF_WATERBLACK</c> por captura con los colores y sus L*. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.WaterBlackSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class WaterBlackSnapshotTests
    {
        // El agua negra de antes (07 §2.1 hasta DC13), para compararla en la misma captura.
        private static readonly Color Previous = Palette.Hex(0x4A2C14);

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.ArrivalDisabledForTests = false;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureWorld3Board()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(61, new MemoryProgressStore(61, 60));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Level.WaterBase, Is.EqualTo("black"), "el 61 va con agua negra (mundo 3)");

            // «Llenar y teñir» con el agua del río en las cuias, antes de teñirse y con los chorros ya terminados: 120 ms tras el
            // último llenado o, si la madera se seca antes del teñido, justo después (siempre antes del teñido).
            view.DebugReplayIntro(1400, false);
            IntroChoreography plan = view.Intro.Plan;
            float at = Mathf.Max(plan.LastFillEndMs + 120f, plan.DrainEndMs + 20f);
            if (at >= plan.DStartMs)
            {
                at = plan.LastFillEndMs + 120f < plan.DStartMs ? plan.LastFillEndMs + 120f : (plan.LastFillEndMs + plan.DStartMs) * 0.5f;
            }

            at = Mathf.Min(at, plan.TotalMs - 1f);
            view.Intro.Seek(at);
            Log("dc13_l61_llenar", "intro a " + at.ToString("0") + " ms de " + plan.TotalMs.ToString("0") + " (fin del llenado "
                + plan.LastFillEndMs.ToString("0") + ", madera seca a " + plan.DrainEndMs.ToString("0") + ", teñido desde " + plan.DStartMs.ToString("0") + "); agua " + Describe(view.IntroWaterColor));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "dc13_l61_llenar");

            view.Intro.Complete();
            PlayModeHelpers.Step(boot, 600);

            // En reposo, lado a lado en la fila 1 (vistas 0, 1 y 2, de izquierda a derecha): el agua de ahora, la de antes (las
            // dos al 70 %, como las pinta la intro) y el Barro bajo el tinte 14. Solo cambian las vistas, no el tablero.
            BoardState board = boot.Flow.Session.Current;
            CuiaView[] views = view.Views;
            Assert.That(views.Length, Is.GreaterThanOrEqualTo(3), "al menos tres cuias");
            Assert.That(Mathf.Abs(views[1].FootPosition.y - views[0].FootPosition.y), Is.LessThan(0.5f), "cuias 0 y 1 en la misma fila");
            Assert.That(Mathf.Abs(views[2].FootPosition.y - views[0].FootPosition.y), Is.LessThan(0.5f), "cuias 0 y 2 en la misma fila");
            bool hasTint14 = false;
            for (int i = 0; i < board.Count && !hasTint14; i++)
            {
                hasTint14 = Has(board[i], 14);
            }

            Assert.That(hasTint14, Is.True, "el 61 lleva el tinte 14");
            for (int v = 0; v < 2; v++)
            {
                Color water = v == 0 ? Palette.WaterBlack : Previous;
                for (int k = 0; k < views[v].Capacity; k++)
                {
                    views[v].SetDose(k, ColorMath.WithAlpha(water, IntroAnimation.WaterAlpha), 1f, 0);
                }

                Log("dc13_l61_tablero", "cuia " + v + (v == 0 ? " (izquierda): ahora " : " (centro): antes ") + Describe(water));
            }

            Cuia mud = Cuia.Create(views[2].Capacity, new[] { Cuia.MudColor, Cuia.MudColor, 14, 14 });
            views[2].ShowContent(mud, false);
            Log("dc13_l61_tablero", "cuia 2 (derecha): Barro " + Describe(Palette.Mud) + " bajo el tinte 14 " + Describe(Palette.Tint(14)));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "dc13_l61_tablero");
            Assert.That(File.Exists(Path.Combine(dir, "dc13_l61_tablero.png")), Is.True);
            SaveZoom(dir, "dc13_l61_tablero_zoom", views[0].Root, views[2].Root, 3);
            Assert.That(File.Exists(Path.Combine(dir, "dc13_l61_tablero_zoom.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureEncounterArrival()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            MapArtRule.ForceProvisional = true;
            string dir = SnapshotKit.Prepare();
            yield return VictoryArrivalTests.Win(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.Arrival, Is.Not.Null);
            Assert.That(view.Arrival!.Completed, Is.True, "la franja terminada");
            Color first = ArrivalStrip.SampleWater("black_white", out Color? second);
            Log("dc13_l40_llegada_encontro", "mini cuia " + Describe(view.Arrival.SampleColor) + "; negra " + Describe(first) + ", blanca "
                + Describe(second ?? Color.clear));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "dc13_l40_llegada_encontro");
        }

        // Recorte del rectángulo que abarcan a y b (con 8 dp de margen), ampliado × scale sin suavizar.
        private static void SaveZoom(string dir, string name, RectTransform a, RectTransform b, int scale)
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

            const float margin = 8f * 3f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin - margin), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin - margin), 0, rt.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax + margin), x0 + 1, rt.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax + margin), y0 + 1, rt.height);
            int w = x1 - x0;
            int h = y1 - y0;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color32[] src = tex.GetPixels32();
            int bw = w * scale;
            int bh = h * scale;
            var big = new Texture2D(bw, bh, TextureFormat.RGB24, false);
            var dst = new Color32[bw * bh];
            for (int y = 0; y < bh; y++)
            {
                for (int x = 0; x < bw; x++)
                {
                    dst[(y * bw) + x] = src[((y / scale) * w) + (x / scale)];
                }
            }

            big.SetPixels32(dst);
            big.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), big.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(big);
        }

        private static bool Has(Cuia c, int tint)
        {
            for (int k = 0; k < c.Count; k++)
            {
                if (c.ColorAt(k) == tint)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Describe(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c) + " (L* " + ColorMath.LabLightness(c).ToString("0.0") + ")";
        }

        private static void Log(string name, string text)
        {
            Debug.Log("AF_WATERBLACK " + name + ": " + text);
        }
    }
}
