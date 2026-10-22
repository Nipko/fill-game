#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
    /// QA con simulador de daltonismo sobre el shader real (07 §3, regla 3, y §7.8; 08 §14.3; bloque B9; el método es [P], Q-B9-10):
    /// un tablero de 16 cuias con los 14 tintes (cada uno en la dosis de abajo de una cuia), pintado por el shader del líquido
    /// (<c>AF_UI_Liquid</c>) con el patrón sutil (15 %), con Patrones de fruta (20 %) y en Alto contraste (35 %). De cada captura
    /// sale la propia, su versión simulada en los tres tipos (PNG, para la prueba manual de 08 §14.3) y un informe con el ΔE2000
    /// de los 91 pares de tintes medidos en los píxeles de la dosis, en <c>Logs/snapshots/es-419/cvd/</c>. Comprueba, como el test
    /// de EditMode sobre los HEX, si en cada tipo el par más parecido es un par de riesgo de 07 §3: si no, lo anota como hallazgo en el
    /// informe (el GDD no fija umbral, Q-B9-10); solo falla si no hay píxeles de verdad. <c>[Explicit]</c>: necesita
    /// gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.ColorVisionShaderTests</c> sin
    /// <c>-nographics</c>), como las demás capturas.
    /// </summary>
    [Explicit("Capturas del simulador de daltonismo; necesitan dispositivo gráfico.")]
    public sealed class ColorVisionShaderTests
    {
        private static readonly string[] TintIds =
        {
            "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo",
            "araza", "cacao", "agua_negro",
        };

        // Fuera del tutorial (1-12) y antes del 41: sin icono de fruta en el centro de las dosis (07 §3, regla 1), que taparía el color.
        private const int Level = 13;

        private static readonly ColorVisionDeficiency[] Types =
        {
            ColorVisionDeficiency.None, ColorVisionDeficiency.Protan, ColorVisionDeficiency.Deutan, ColorVisionDeficiency.Tritan,
        };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator FourteenTints_OnTheRealShader_InTheThreeDeficiencies()
        {
            string dir = Path.Combine(SnapshotKit.Prepare(), "cvd");
            Directory.CreateDirectory(dir);
            yield return PlayModeHelpers.LoadGameWithPack(FourteenTintPack(), Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 600); // la onda de la superficie, quieta
            var report = new StringBuilder();
            Capture(boot, dir, "cvd_15", Palette.PatternOpacitySubtle, report);
            Capture(boot, dir, "cvd_20", Palette.PatternOpacityOn, report);
            Capture(boot, dir, "cvd_35_alto_contraste", AF.Platform.A11yRules.HighContrastPatternOpacity, report);
            File.WriteAllText(Path.Combine(dir, "cvd_report.md"), report.ToString());
            TestContext.WriteLine(report.ToString());
        }

        private static void Capture(GameBootstrap boot, string dir, string name, float opacity, StringBuilder report)
        {
            LiquidGraphic.ApplyPatternOpacity(opacity);
            Color32[] pixels = SnapshotKit.SavePixels(dir, name);
            Color[] tint = new Color[15];
            BoardState state = boot.Flow!.Session.Current;
            CuiaView[] views = boot.View!.Views;
            for (int i = 0; i < views.Length && i < state.Count; i++)
            {
                if (state[i].IsEmpty)
                {
                    continue;
                }

                tint[state[i].ColorAt(0)] = Sample(pixels, views[i], 0);
                Assert.That(ColorVision.Lab(tint[state[i].ColorAt(0)]).x, Is.GreaterThan(5f), name + ": la dosis se pintó (¿sin gráficos?)");
            }

            report.Append("## ").Append(name).Append(" (patrón al ").Append((opacity * 100f).ToString("0", CultureInfo.InvariantCulture)).Append(" %)\n\n");
            foreach (ColorVisionDeficiency type in Types)
            {
                SaveSimulated(pixels, dir, name + "_" + type.ToString().ToLowerInvariant(), type);
                var pairs = new List<(double De, int A, int B)>();
                for (int a = 1; a <= 14; a++)
                {
                    for (int b = a + 1; b <= 14; b++)
                    {
                        pairs.Add((ColorVision.DeltaE2000(tint[a], tint[b], type), a, b));
                    }
                }

                pairs.Sort((x, y) => x.De.CompareTo(y.De));
                report.Append("- ").Append(type).Append(':');
                for (int k = 0; k < 10; k++)
                {
                    report.Append(' ').Append(pairs[k].A).Append('-').Append(pairs[k].B).Append(" = ")
                        .Append(pairs[k].De.ToString("0.0", CultureInfo.InvariantCulture)).Append(RiskMark(pairs[k].A, pairs[k].B)).Append(';');
                }

                report.Append('\n');
                if (type != ColorVisionDeficiency.None && RiskMark(pairs[0].A, pairs[0].B).Length == 0)
                {
                    // Hallazgo para el QA manual de 08 §14.3 y el dueño (Q-B9-10), no un fallo: el GDD no fija umbral.
                    report.Append("  - **Hallazgo:** con ").Append(type).Append(", el par más parecido en pantalla (").Append(pairs[0].A)
                        .Append('-').Append(pairs[0].B).Append(") no es un par de riesgo de 07 §3.\n");
                }
            }

            report.Append('\n');
        }

        // Media de un cuadrado de 9 × 9 px en el centro de la dosis k (contada desde abajo) de la cuia, en sRGB.
        private static Color Sample(Color32[] pixels, CuiaView view, int k)
        {
            var corners = new Vector3[4];
            view.Body.GetWorldCorners(corners);
            float t = (CuiaGeometry.CavityBottomU + k + 0.5f) / view.HeightU;
            Vector3 world = Vector3.Lerp((corners[0] + corners[3]) * 0.5f, (corners[1] + corners[2]) * 0.5f, t);
            Vector3 p = SnapshotKit.CaptureCamera!.WorldToScreenPoint(world);
            int cx = Mathf.RoundToInt(p.x);
            int cy = Mathf.RoundToInt(p.y);
            float r = 0f, g = 0f, b = 0f;
            int n = 0;
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int x = Mathf.Clamp(cx + dx, 0, SnapshotKit.Width - 1);
                    int y = Mathf.Clamp(cy + dy, 0, SnapshotKit.Height - 1);
                    Color32 c = pixels[(y * SnapshotKit.Width) + x];
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    n++;
                }
            }

            return new Color(r / (255f * n), g / (255f * n), b / (255f * n), 1f);
        }

        private static void SaveSimulated(Color32[] pixels, string dir, string name, ColorVisionDeficiency type)
        {
            var tex = new Texture2D(SnapshotKit.Width, SnapshotKit.Height, TextureFormat.RGB24, false);
            var simulated = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                simulated[i] = ColorVision.Simulate(pixels[i], type);
            }

            tex.SetPixels32(simulated);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        private static string RiskMark(int a, int b)
        {
            int[,] risk = { { 1, 2 }, { 3, 12 }, { 4, 5 }, { 4, 14 }, { 5, 6 }, { 6, 13 }, { 8, 9 }, { 10, 11 }, { 11, 12 }, { 13, 14 } };
            for (int i = 0; i < risk.GetLength(0); i++)
            {
                if ((risk[i, 0] == a && risk[i, 1] == b) || (risk[i, 0] == b && risk[i, 1] == a))
                {
                    return " (R)";
                }
            }

            return string.Empty;
        }

        // 16 cuias de 4: la i (0-13) lleva abajo el tinte i + 1 (dos dosis) y encima el siguiente (dos), y dos vacías; cada tinte, 4
        // dosis. El nivel 13 (mundo 2) de un paquete de prueba.
        private static string FourteenTintPack()
        {
            var board = new int[16][];
            for (int i = 0; i < 14; i++)
            {
                int a = i + 1;
                int b = ((i + 1) % 14) + 1;
                board[i] = new[] { a, a, b, b };
            }

            board[14] = new int[0];
            board[15] = new int[0];
            return PlayModeHelpers.BoardPack(2, "w02_tapajos", "clear", Level, TintIds, board);
        }
    }
}
