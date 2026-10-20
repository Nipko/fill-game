#nullable enable
using System.Collections;
using System.Globalization;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Vigila que la temporada del ciclo se vea en lo que el tablero deja a la vista (tarea DC21; D-030, pregunta 13 b; SE2;
    /// D-009, punto 6; pregunta 32 b, D-020; 07 §2.1): la LUT de temporada, que es la única señal de la temporada (sin cartel),
    /// llega al panorama, al cielo de la parada y a la banda de agua, y en la zona visible del fondo el ΔE00 medio tiene que
    /// llegar a <see cref="MinVsNone"/> frente al fondo sin temporada con cada una de las dos (A y B, las dos forzadas: así se
    /// vigila también la que no es la del ciclo) y a <see cref="MinAVsB"/> entre las dos temporadas en la misma parada. Por
    /// nivel, tres capturas: sin temporada (<see cref="GameScreen.SeasonLutEnabled"/> apagado), con la A y con la B
    /// (<see cref="GameScreen.SeasonOverride"/>), en 41 y 61 (2 filas), en las paradas con luz propia (91, Leticia al
    /// atardecer; 191, Pacaya-Samiria al alba, que también tiene 3 filas) y en un nivel de 3 filas (151).
    /// <para><b>Zona medida</b>: las filas de la captura por encima del panel (<c>StopPanelRect</c>), y en ellas solo los
    /// píxeles de fondo a la vista: los que cambian (algún canal ≥ <see cref="MaskMin"/> de 255) al repetir la captura sin
    /// temporada sin el panorama ni el fondo de la parada. Así quedan fuera el HUD, el follaje, la garza y el panel, que se
    /// pintan encima, sin cajas fijas que dependan de la maquetación, y entran el cielo, el panorama y la banda de agua aunque
    /// la temporada no los cambie. ΔE00 medio píxel a píxel en una rejilla de <see cref="Step"/> px. Cada nivel escribe su línea
    /// <c>AF_SEASON_VIS</c> con las cifras, las filas del tablero y la zona medida.</para>
    /// <para>Cada nivel tiene que dejar muestras de fondo a la vista (si no, falla: la máscara o la maquetación se rompieron).
    /// Con 3 filas el panel puede empezar casi bajo el HUD (DC21: si no se llega sin tocar el tablero, se mide y se anota): solo
    /// en un tablero de 3 filas con una zona de menos de <see cref="MinRows"/> filas de captura se mide, se escribe y no se exige
    /// la cifra; el 151 tiene que superar <see cref="MinRows"/> (si una maquetación nueva lo deja por debajo, el test falla y hay
    /// que elegir otro nivel de 3 filas o anotarlo). Explícito:
    /// necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SeasonVisibilitySnapshotTests</c>
    /// sin <c>-nographics</c>); las capturas quedan en <c>Logs/snapshots/es-419/</c> (<c>season_vis_l&lt;n&gt;_off|a|b</c>).</para>
    /// </summary>
    [Explicit("Visibilidad de la temporada sobre capturas (DC21); necesita dispositivo gráfico.")]
    public sealed class SeasonVisibilitySnapshotTests
    {
        /// <summary>ΔE00 medio mínimo frente al fondo sin temporada (revisión de SE2; DC21).</summary>
        public const float MinVsNone = 5f;

        /// <summary>ΔE00 medio mínimo entre la A y la B en la misma parada (revisión de SE2; DC21).</summary>
        public const float MinAVsB = 8f;

        /// <summary>
        /// Filas de captura (de 2340) por debajo de las cuales la zona visible de un tablero de 3 filas no basta para exigir las
        /// cifras.
        /// </summary>
        public const int MinRows = 30;

        // El nivel de 3 filas medido (DC21): su zona sobre el panel tiene que llegar a MinRows.
        private const int ThreeRowsLevel = 151;

        private const int MaskMin = 3;
        private const int Step = 3;

        [TearDown]
        public void TearDown()
        {
            GameScreen.SeasonLutEnabled = true;
            GameScreen.SeasonOverride = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator TheSeasonShows_InTheVisibleSkyAndStrip()
        {
            string dir = SnapshotKit.Prepare();
            int[] levels = { 41, 61, 91, 191, ThreeRowsLevel };
            bool twoRows = false;
            bool threeRows = false;
            var failures = new System.Text.StringBuilder();
            foreach (int level in levels)
            {
                Shot? off = null;
                Shot? a = null;
                Shot? b = null;
                yield return Capture(dir, level, null, s => off = s);
                yield return Capture(dir, level, Season.HighWater, s => a = s);
                yield return Capture(dir, level, Season.LowWater, s => b = s);
                Assert.That(off, Is.Not.Null);
                Assert.That(a, Is.Not.Null);
                Assert.That(b, Is.Not.Null);
                Assert.That(a!.Rows, Is.EqualTo(off!.Rows), "el mismo tablero (" + level + ", A)");
                Assert.That(b!.Rows, Is.EqualTo(off.Rows), "el mismo tablero (" + level + ", B)");
                twoRows |= off.Rows == 2;
                threeRows |= off.Rows == 3;
                Measure(level, off, a, b, failures);
            }

            Assert.That(twoRows, Is.True, "algún nivel medido tiene 2 filas");
            Assert.That(threeRows, Is.True, "algún nivel medido tiene 3 filas");
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
        }

        private static IEnumerator Capture(string dir, int level, Season? season, System.Action<Shot> done)
        {
            GameScreen.SeasonLutEnabled = season.HasValue;
            GameScreen.SeasonOverride = season;
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro (" + level + ")");
            yield return SnapshotKit.Wait(0.3f);
            GameScreen view = boot.View!;
            Assert.That(view.StopBackdrop.Season, Is.EqualTo(season ?? Season.None), "temporada de la captura (" + level + ")");
            string name = "season_vis_l" + level + "_" + (season == null ? "off" : (season == Season.HighWater ? "a" : "b"));
            Color32[] pixels = SnapshotKit.SavePixels(dir, name);
            Color32[]? bare = null;
            if (season == null)
            {
                // La misma sin el fondo (panorama y fondo de la parada): lo que cambia es fondo a la vista; lo que no, lo tapa
                // algo (HUD, follaje, garza, panel).
                GameObject panorama = view.PanoramaRect.gameObject;
                GameObject stop = view.StopBackdrop.Root.gameObject;
                bool stopWas = stop.activeSelf;
                panorama.SetActive(false);
                stop.SetActive(false);
                bare = SnapshotKit.CapturePixels();
                panorama.SetActive(true);
                stop.SetActive(stopWas);
            }

            float px = SnapshotKit.Width / view.Canvas.GetComponent<RectTransform>().rect.width; // px de captura por dp
            int panelTopPx = Mathf.Clamp(Mathf.FloorToInt(view.StopPanelRect.y * px), 0, SnapshotKit.Height);
            done(new Shot(pixels, bare, panelTopPx, RowsOf(view.Views), boot.Seasons != null ? boot.Seasons.SeasonOf(level) : Season.None,
                view.StopBackdrop.Stop != null ? view.StopBackdrop.Stop.Id : "-"));
        }

        private static void Measure(int level, Shot off, Shot a, Shot b, System.Text.StringBuilder failures)
        {
            const int w = SnapshotKit.Width;
            const int h = SnapshotKit.Height;
            int rows = Mathf.Min(off.PanelTopPx, Mathf.Min(a.PanelTopPx, b.PanelTopPx));
            double offA = 0d;
            double offB = 0d;
            double ab = 0d;
            int count = 0;
            int grid = 0;
            for (int y = 0; y < rows; y += Step)
            {
                int row = (h - 1 - y) * w; // GetPixels32 empieza por abajo
                for (int x = 0; x < w; x += Step)
                {
                    grid++;
                    int i = row + x;
                    if (!Differs(off.Pixels[i], off.Bare![i]))
                    {
                        continue; // no es fondo a la vista: lo tapa el HUD, el follaje, la garza o el panel
                    }

                    Color co = off.Pixels[i];
                    Color ca = a.Pixels[i];
                    Color cb = b.Pixels[i];
                    offA += ColorMath.DeltaE00(co, ca);
                    offB += ColorMath.DeltaE00(co, cb);
                    ab += ColorMath.DeltaE00(ca, cb);
                    count++;
                }
            }

            CultureInfo ci = CultureInfo.InvariantCulture;
            float mOffA = count > 0 ? (float)(offA / count) : 0f;
            float mOffB = count > 0 ? (float)(offB / count) : 0f;
            float mAb = count > 0 ? (float)(ab / count) : 0f;
            bool enough = rows >= MinRows || off.Rows != 3;
            Debug.Log("AF_SEASON_VIS " + level + " (parada " + off.Stop + ", " + off.Rows + " filas, temporada del ciclo "
                + (SeasonTable.Letter(off.Natural) ?? "-") + "): ΔE00 medio sin temporada / A " + mOffA.ToString("0.00", ci)
                + ", sin temporada / B " + mOffB.ToString("0.00", ci) + ", A / B " + mAb.ToString("0.00", ci) + "; zona: filas 0-"
                + rows + " px de " + h + " (sobre el panel), " + count + " de " + grid + " muestras ("
                + (grid > 0 ? 100d * count / grid : 0d).ToString("0", ci) + " %, rejilla de " + Step + " px)"
                + (enough ? string.Empty : "; zona demasiado pequeña con 3 filas: se anota y no se exige"));
            if (count == 0)
            {
                failures.Append(level).Append(": ninguna muestra de fondo a la vista (¿la máscara o la maquetación?); ");
                return;
            }

            if (level == ThreeRowsLevel && rows < MinRows)
            {
                failures.Append(level).Append(": la zona sobre el panel tiene ").Append(rows).Append(" filas < ").Append(MinRows)
                    .Append(" (el nivel de 3 filas tiene que medirse); ");
            }

            if (!enough)
            {
                return;
            }

            if (mOffA < MinVsNone)
            {
                failures.Append(level).Append(": sin temporada / A ").Append(mOffA.ToString("0.00", ci)).Append(" < ")
                    .Append(MinVsNone.ToString("0", ci)).Append("; ");
            }

            if (mOffB < MinVsNone)
            {
                failures.Append(level).Append(": sin temporada / B ").Append(mOffB.ToString("0.00", ci)).Append(" < ")
                    .Append(MinVsNone.ToString("0", ci)).Append("; ");
            }

            if (mAb < MinAVsB)
            {
                failures.Append(level).Append(": A / B ").Append(mAb.ToString("0.00", ci)).Append(" < ").Append(MinAVsB.ToString("0", ci))
                    .Append("; ");
            }
        }

        private static bool Differs(Color32 p, Color32 q)
        {
            return Mathf.Abs(p.r - q.r) >= MaskMin || Mathf.Abs(p.g - q.g) >= MaskMin || Mathf.Abs(p.b - q.b) >= MaskMin;
        }

        // Filas del tablero: cuántas alturas distintas de pie hay (como SnapshotKit.LogBoard).
        private static int RowsOf(CuiaView[] views)
        {
            int rows = views.Length > 0 ? 1 : 0;
            for (int i = 1; i < views.Length; i++)
            {
                if (Mathf.Abs(views[i].FootPosition.y - views[i - 1].FootPosition.y) >= 0.5f)
                {
                    rows++;
                }
            }

            return rows;
        }

        private sealed class Shot
        {
            public Shot(Color32[] pixels, Color32[]? bare, int panelTopPx, int rows, Season natural, string stop)
            {
                Pixels = pixels;
                Bare = bare;
                PanelTopPx = panelTopPx;
                Rows = rows;
                Natural = natural;
                Stop = stop;
            }

            public Color32[] Pixels { get; }

            // La captura sin temporada repetida sin el fondo (máscara de la zona visible); null en las demás.
            public Color32[]? Bare { get; }

            public int PanelTopPx { get; }

            public int Rows { get; }

            public Season Natural { get; }

            public string Stop { get; }
        }
    }
}
