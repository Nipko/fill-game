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
    /// atardecer) y <c>02f_board_dawn_l191</c> (Pacaya-Samiria al alba), y la línea <c>AF_SKY_DUSK_DAWN</c> con el ΔE00 medio
    /// del cielo visible entre las dos, medido solo en los píxeles de cielo (<see cref="SkyMeasure"/>). La medida fiable es la del
    /// inicio (<see cref="LobbyStopSnapshotTests"/>), donde el cielo se ve hasta el horizonte.
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

        // DC20: el atardecer de la 9 y el alba de la 19 con el cielo a alfa ≤ 0,5. La 191 es el tablero real de 3 filas: el panel
        // tapa casi todo el cielo y el horizonte, así que el cielo del alba se revisa en lobby_stop_k19 (LobbyStopSnapshotTests) o en
        // 02f_board_stop_19 (StopSheetSnapshotTests, 2 filas); la medida de aquí se queda como referencia de una franja pequeña.
        [UnityTest]
        public IEnumerator CaptureTheDuskAndDawnBoards()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(91, new MemoryProgressStore(91, 90));
            SkyMeasure.Shot? dusk = null;
            yield return CaptureBoard(dir, "02f_board_dusk_l91", 9, s => dusk = s);

            yield return PlayModeHelpers.LoadGame(191, new MemoryProgressStore(191, 190));
            SkyMeasure.Shot? dawn = null;
            yield return CaptureBoard(dir, "02f_board_dawn_l191", 19, s => dawn = s);

            Assert.That(dusk, Is.Not.Null);
            Assert.That(dawn, Is.Not.Null);
            SkyMeasure.Log("tablero 02f_board_dusk_l91 / 02f_board_dawn_l191 (2 y 3 filas)", dusk!, dawn!);
        }

        private static IEnumerator CaptureBoard(string dir, string name, int stop)
        {
            return CaptureBoard(dir, name, stop, null);
        }

        private static IEnumerator CaptureBoard(string dir, string name, int stop, System.Action<SkyMeasure.Shot>? shot)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro (" + name + ")");
            yield return SnapshotKit.Wait(0.3f);
            StopBackdrop b = boot.View!.StopBackdrop;
            Assert.That(b.Stop, Is.Not.Null, name);
            Assert.That(b.Stop!.N, Is.EqualTo(stop), name);
            if (shot != null)
            {
                shot(SkyMeasure.Take(dir, name, b));
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }

            Debug.Log("AF_STOP " + name + ": horizonte " + boot.View.HorizonY.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " dp, héroe " + b.HeroSource + " " + (b.Shape ?? "-") + ", " + boot.View.LastStopProbe);
        }
    }

    /// <summary>
    /// Medida del cielo del atardecer frente al del alba (DC20; D-030, pregunta 18 b), solo sobre píxeles de cielo: cada captura se
    /// repite sin el tinte del cielo (<see cref="StopBackdrop.SkyGraphic"/> apagado) y cuenta como cielo visible el píxel que
    /// cambia en las dos capturas (algún canal ≥ <see cref="MaskMin"/>). Así quedan fuera el HUD, la ventanita, las hojas, la
    /// silueta del pueblo, el panel y lo que haya bajo el horizonte, porque se dibujan encima del cielo o fuera de él. Escribe la
    /// línea <c>AF_SKY_DUSK_DAWN</c>: ΔE00 medio píxel a píxel del cielo visible, el de su tercio de abajo y el de los colores
    /// medios de ese tercio, con la región medida (filas, muestras y fracción de la franja de cielo).
    /// </summary>
    internal static class SkyMeasure
    {
        /// <summary>Diferencia mínima (de 255, en algún canal) para contar un píxel como cielo visible.</summary>
        public const int MaskMin = 3;

        private const int Step = 3;

        /// <summary>Una captura con el tinte del cielo y la misma sin él.</summary>
        public sealed class Shot
        {
            public Shot(Color32[] tinted, Color32[] untinted)
            {
                Tinted = tinted;
                Untinted = untinted;
            }

            public Color32[] Tinted { get; }

            public Color32[] Untinted { get; }
        }

        /// <summary>Guarda <c>&lt;name&gt;.png</c> y la repite sin el tinte del cielo (esta, sin PNG).</summary>
        public static Shot Take(string dir, string name, StopBackdrop backdrop)
        {
            Color32[] tinted = SnapshotKit.SavePixels(dir, name);
            StopShapeGraphic sky = backdrop.SkyGraphic;
            bool was = sky.enabled;
            sky.enabled = false;
            Color32[] untinted = SnapshotKit.CapturePixels();
            sky.enabled = was;
            return new Shot(tinted, untinted);
        }

        private static bool IsSky(Shot s, int i)
        {
            Color32 a = s.Tinted[i];
            Color32 b = s.Untinted[i];
            return Mathf.Abs(a.r - b.r) >= MaskMin || Mathf.Abs(a.g - b.g) >= MaskMin || Mathf.Abs(a.b - b.b) >= MaskMin;
        }

        /// <summary>Mide y escribe <c>AF_SKY_DUSK_DAWN</c> para el par <paramref name="what"/>.</summary>
        public static void Log(string what, Shot dusk, Shot dawn)
        {
            const int w = SnapshotKit.Width;
            const int h = SnapshotKit.Height;

            // Franja de cielo: desde arriba hasta la fila más baja que el tinte cambia en alguna de las dos capturas.
            int horizonPx = 0;
            for (int y = 0; y < h; y += Step)
            {
                int row = (h - 1 - y) * w; // GetPixels32 empieza por abajo
                for (int x = 0; x < w; x += Step)
                {
                    if (IsSky(dusk, row + x) || IsSky(dawn, row + x))
                    {
                        horizonPx = y + 1;
                        break;
                    }
                }
            }

            int lowFrom = horizonPx * 2 / 3;
            double sum = 0d;
            double lowSum = 0d;
            int count = 0;
            int lowCount = 0;
            int grid = 0;
            int minY = -1;
            Vector3 duskLow = Vector3.zero;
            Vector3 dawnLow = Vector3.zero;
            for (int y = 0; y < horizonPx; y += Step)
            {
                int row = (h - 1 - y) * w;
                for (int x = 0; x < w; x += Step)
                {
                    grid++;
                    int i = row + x;
                    if (!IsSky(dusk, i) || !IsSky(dawn, i))
                    {
                        continue;
                    }

                    Color a = dusk.Tinted[i];
                    Color b = dawn.Tinted[i];
                    float d = ColorMath.DeltaE00(a, b);
                    sum += d;
                    count++;
                    if (minY < 0)
                    {
                        minY = y;
                    }

                    if (y >= lowFrom)
                    {
                        lowSum += d;
                        lowCount++;
                        duskLow += ColorMath.ToLab(a);
                        dawnLow += ColorMath.ToLab(b);
                    }
                }
            }

            Assert.That(count, Is.GreaterThan(0), "hay cielo a la vista (" + what + ")");
            float meanOfLow = lowCount > 0 ? ColorMath.DeltaE00Lab(duskLow / lowCount, dawnLow / lowCount) : 0f;
            CultureInfo ci = CultureInfo.InvariantCulture;
            Debug.Log("AF_SKY_DUSK_DAWN " + what + ": alfa del cielo ≤ " + StopBackdrop.SkyMaxAlpha.ToString("0.0#", ci)
                + " (tercio de abajo ≤ " + StopBackdrop.SkyLowMaxAlpha.ToString("0.0#", ci) + "); región: solo píxeles de cielo"
                + " visibles en las dos (máscara con y sin tinte, umbral " + MaskMin + "/255), filas " + minY + "-" + horizonPx
                + " px de " + h + ", " + count + " de " + grid + " muestras de la franja (" + (100d * count / grid).ToString("0", ci)
                + " %, rejilla de " + Step + " px); ΔE00 medio del cielo visible " + (sum / count).ToString("0.00", ci)
                + ", del tercio de abajo (y " + lowFrom + "-" + horizonPx + " px, " + lowCount + " muestras) "
                + (lowCount > 0 ? lowSum / lowCount : 0d).ToString("0.00", ci) + ", entre los colores medios del tercio de abajo "
                + meanOfLow.ToString("0.00", ci));
        }
    }
}
