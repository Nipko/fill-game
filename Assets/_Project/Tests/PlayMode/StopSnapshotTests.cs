#nullable enable
using System.Collections;
using System.Globalization;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del fondo por parada en tableros de 1 y de 3 filas (tarea M1.4; D-013, punto 9; propuesta del Mapa §10.6;
    /// VA-20 a VA-23 de direccion-visual-v2 §9), con las siluetas provisionales (solo en el Editor y en desarrollo; nunca en la
    /// ficha, las capturas de la tienda ni la UA): <c>02b_board_l1</c> (1 fila: el héroe entero sobre el horizonte) y
    /// <c>02f_board_stop_15_l151</c> (el 151 real, 3 filas: el panel empieza bajo el HUD y no queda franja). Las 20
    /// composiciones de 1-200 en el tablero de 2 filas (<c>02f_board_stop_&lt;nn&gt;</c>) las genera un solo fixture,
    /// <see cref="StopSheetSnapshotTests"/>, junto con la hoja de VA-20 que las reutiliza (revisión de M2.4: antes las escribían
    /// los dos y se pisaban). Cada captura escribe en el log su línea <c>AF_STOP</c> con la sonda. Con DC20 (D-030, pregunta 18 b:
    /// el cielo con alfa ≤ 0,5 también en el tercio de abajo del atardecer y del alba), <c>02f_board_dusk_l91</c> (Leticia al
    /// atardecer) y <c>02f_board_dawn_l191</c> (Pacaya-Samiria al alba), y la línea <c>AF_SKY_DUSK_DAWN</c> con el ΔE00 medio del
    /// cielo visible entre las dos (píxel a píxel, del borde de arriba al horizonte más alto de los dos) y el de su tercio de abajo.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.StopSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del fondo por parada (M1.4); necesitan dispositivo gráfico.")]
    public sealed class StopSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTheOneAndThreeRowBoards()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            yield return CaptureBoard(dir, "02b_board_l1", 0);

            yield return PlayModeHelpers.LoadGame(151, new MemoryProgressStore(151, 150));
            yield return CaptureBoard(dir, "02f_board_stop_15_l151", 15);
        }

        // DC20: el atardecer de la 9 y el alba de la 19 con el cielo a alfa ≤ 0,5, y cuánto se distinguen (se anota para el dueño).
        [UnityTest]
        public IEnumerator CaptureTheDuskAndDawnBoards()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(91, new MemoryProgressStore(91, 90));
            float duskHorizon = 0f;
            Color32[]? dusk = null;
            yield return CaptureBoard(dir, "02f_board_dusk_l91", 9, (p, h) => { dusk = p; duskHorizon = h; });

            yield return PlayModeHelpers.LoadGame(191, new MemoryProgressStore(191, 190));
            float dawnHorizon = 0f;
            Color32[]? dawn = null;
            yield return CaptureBoard(dir, "02f_board_dawn_l191", 19, (p, h) => { dawn = p; dawnHorizon = h; });

            Assert.That(dusk, Is.Not.Null);
            Assert.That(dawn, Is.Not.Null);
            int horizonPx = Mathf.FloorToInt(Mathf.Min(duskHorizon, dawnHorizon) * SnapshotKit.Width / 360f);
            int lowFrom = horizonPx * 2 / 3;
            double sum = 0d;
            double lowSum = 0d;
            int count = 0;
            int lowCount = 0;
            Vector3 duskLow = Vector3.zero;
            Vector3 dawnLow = Vector3.zero;
            for (int y = 0; y < horizonPx; y += 3)
            {
                int row = (SnapshotKit.Height - 1 - y) * SnapshotKit.Width; // GetPixels32 empieza por abajo
                for (int x = 0; x < SnapshotKit.Width; x += 3)
                {
                    Color a = dusk![row + x];
                    Color b = dawn![row + x];
                    float d = ColorMath.DeltaE00(a, b);
                    sum += d;
                    count++;
                    if (y >= lowFrom)
                    {
                        lowSum += d;
                        lowCount++;
                        duskLow += ColorMath.ToLab(a);
                        dawnLow += ColorMath.ToLab(b);
                    }
                }
            }

            Assert.That(count, Is.GreaterThan(0), "hay cielo a la vista");
            float meanOfLow = lowCount > 0 ? ColorMath.DeltaE00Lab(duskLow / lowCount, dawnLow / lowCount) : 0f;
            CultureInfo ci = CultureInfo.InvariantCulture;
            Debug.Log("AF_SKY_DUSK_DAWN alfa del cielo ≤ " + StopBackdrop.SkyMaxAlpha.ToString("0.0#", ci) + " (tercio de abajo ≤ "
                + StopBackdrop.SkyLowMaxAlpha.ToString("0.0#", ci) + "): ΔE00 medio del cielo visible (y 0-" + horizonPx + " px) "
                + (sum / count).ToString("0.00", ci) + ", del tercio de abajo (y " + lowFrom + "-" + horizonPx + " px) "
                + (lowCount > 0 ? lowSum / lowCount : 0d).ToString("0.00", ci) + ", entre los colores medios del tercio de abajo "
                + meanOfLow.ToString("0.00", ci));
        }

        private static IEnumerator CaptureBoard(string dir, string name, int stop)
        {
            return CaptureBoard(dir, name, stop, null);
        }

        private static IEnumerator CaptureBoard(string dir, string name, int stop, System.Action<Color32[], float>? pixels)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro (" + name + ")");
            yield return SnapshotKit.Wait(0.3f);
            StopBackdrop b = boot.View!.StopBackdrop;
            Assert.That(b.Stop, Is.Not.Null, name);
            Assert.That(b.Stop!.N, Is.EqualTo(stop), name);
            if (pixels != null)
            {
                pixels(SnapshotKit.SavePixels(dir, name), boot.View.HorizonY);
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }

            Debug.Log("AF_STOP " + name + ": horizonte " + boot.View.HorizonY.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " dp, héroe " + b.HeroSource + " " + (b.Shape ?? "-") + ", " + boot.View.LastStopProbe);
        }
    }
}
