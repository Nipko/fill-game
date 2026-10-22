#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del contorno de dosis de «Alto contraste» (07 §3, regla 5: 1,5 px de diseño, en dp [P]; Q-B9-6) sobre el shader
    /// real del líquido: el mismo tablero sin el ajuste y con él, con los pares de riesgo 4-5 (Aguaje y Achiote) y 8-9 (Açaí y Huito)
    /// en dosis vecinas, en <c>Logs/snapshots/es-419/a11y/</c>. Comprueba que, con el ajuste, cada una de las 3 fronteras de la
    /// cuia del Aguaje y el Achiote lleva una línea oscura de lado a lado de la franja del eje, y ninguna más. <c>[Explicit]</c>: necesita
    /// gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.DoseOutlineCaptureTests</c> sin
    /// <c>-nographics</c>), como las demás capturas.
    /// </summary>
    [Explicit("Capturas del contorno de Alto contraste; necesitan dispositivo gráfico.")]
    public sealed class DoseOutlineCaptureTests
    {
        private static readonly string[] TintIds =
        {
            "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo",
            "araza", "cacao", "agua_negro",
        };

        // Fuera del tutorial y antes del 41: patrón sutil y sin icono de fruta (07 §3, regla 1).
        private const int Level = 13;

        [TearDown]
        public void TearDown()
        {
            LiquidGraphic.ApplyDoseOutline(0f);
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator HighContrast_DrawsTheDoseOutline_OnTheRealShader()
        {
            string dir = Path.Combine(SnapshotKit.Prepare(), "a11y");
            Directory.CreateDirectory(dir);
            yield return PlayModeHelpers.LoadGameWithPack(Pack(), Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 600); // la onda de la superficie, quieta
            CuiaView aguaje = boot.View!.Views[0];

            boot.View.HighContrast = false;
            Color32[] off = SnapshotKit.SavePixels(dir, "alto_contraste_off");
            boot.View.HighContrast = true;
            Assert.That(LiquidGraphic.ShaderDoseOutline, Is.EqualTo(1.5f).Within(1e-6f));
            Color32[] on = SnapshotKit.SavePixels(dir, "alto_contraste_on");

            // En la cuia del Aguaje y el Achiote (4 dosis, 3 fronteras), una franja de 41 px alrededor del eje: las filas en que casi
            // toda la franja se oscurece con el ajuste son las líneas de contorno (el patrón al 35 % oscurece píxeles sueltos, no
            // filas enteras). Cada frontera da un solo grupo de filas seguidas (las de su línea), así que se esperan exactamente 3.
            int lines = OutlineRows(off, on, aguaje, out float maxDrop);
            string report = string.Format(CultureInfo.InvariantCulture,
                "Cuia 1 (Aguaje y Achiote): {0} grupos de filas oscurecidas en la franja del eje; mayor caída de luma {1:0.000}.", lines, maxDrop);
            File.WriteAllText(Path.Combine(dir, "alto_contraste_report.txt"), report);
            TestContext.WriteLine(report);
            Assert.That(lines, Is.EqualTo(3), "una línea por frontera (3), ninguna sobre el fondo ni la superficie");
            Assert.That(maxDrop, Is.GreaterThan(0.3f), "la línea es tinta, no un matiz");
        }

        // Cuatro cuias de 4 y dos vacías: Aguaje/Achiote y Açaí/Huito alternados (pares de riesgo de 07 §3), cada tinte 4 dosis.
        private static string Pack()
        {
            int[][] board =
            {
                new[] { 4, 5, 4, 5 },
                new[] { 8, 9, 8, 9 },
                new[] { 5, 4, 9, 8 },
                new[] { 9, 8, 5, 4 },
                new int[0],
                new int[0],
            };
            return PlayModeHelpers.BoardPack(2, "w02_tapajos", "clear", Level, TintIds, board);
        }

        // Grupos de filas seguidas en que ≥ 80 % de la franja de ±20 px alrededor del eje de la cuia baja su luma más de 0,12 con
        // el ajuste, dentro del rectángulo del cuerpo de la cuia.
        private static int OutlineRows(Color32[] off, Color32[] on, CuiaView view, out float maxDrop)
        {
            var corners = new Vector3[4];
            view.Body.GetWorldCorners(corners);
            Vector3 lo = SnapshotKit.CaptureCamera!.WorldToScreenPoint(corners[0]);
            Vector3 hi = SnapshotKit.CaptureCamera!.WorldToScreenPoint(corners[2]);
            int cx = Mathf.RoundToInt((lo.x + hi.x) * 0.5f);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(lo.y, hi.y)), 0, SnapshotKit.Height - 1);
            int y1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(lo.y, hi.y)), 0, SnapshotKit.Height - 1);
            maxDrop = 0f;
            int groups = 0;
            bool inGroup = false;
            for (int y = y0; y <= y1; y++)
            {
                int dark = 0;
                int n = 0;
                for (int x = cx - 20; x <= cx + 20; x++)
                {
                    int i = (y * SnapshotKit.Width) + Mathf.Clamp(x, 0, SnapshotKit.Width - 1);
                    float drop = Luma(off[i]) - Luma(on[i]);
                    maxDrop = Mathf.Max(maxDrop, drop);
                    dark += drop > 0.12f ? 1 : 0;
                    n++;
                }

                bool line = dark >= n * 0.8f;
                if (line && !inGroup)
                {
                    groups++;
                }

                inGroup = line;
            }

            return groups;
        }

        private static float Luma(Color32 c)
        {
            return Luma(new Color(c.r / 255f, c.g / 255f, c.b / 255f));
        }

        private static float Luma(Color c)
        {
            return (0.2126f * c.r) + (0.7152f * c.g) + (0.0722f * c.b);
        }
    }
}
