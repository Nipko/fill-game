#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Arte procedural del banner de mundo (08 §5.4: carga de nivel &lt; 300 ms e hilo principal ≤ 8 ms por fotograma; tarea A4,
    /// revisiones 2 y 3): las piezas que pide <c>WorldBannerView.Show</c> al abrirse (la viñeta con Capi, la pata, Rosa, el
    /// soplo, la onda y la espuma) se pintan y se pasan a Color32 en un hilo del grupo
    /// (<see cref="ProceduralArt.RequestBannerArt"/>), así que en el fotograma de la carga del 11, el 41, el 81 o el 141 el hilo
    /// principal solo pide el trabajo y, cuando acaba, crea las texturas. Se acota lo que toca al hilo principal (un fotograma);
    /// lo que tarda en pintarse (lo que el banner espera antes de entrar) depende de la máquina y va en un test explícito de
    /// rendimiento. Un trabajo que falla en su hilo o que el grupo no acepta nunca lanza en el hilo principal: deja el estado de
    /// fallo con un solo aviso, y la vista entra sin arte. Un trabajo abandonado (el tiempo límite de la vista) o soltado para y
    /// no se recoge. En el teléfono se miden con <c>AF_STATE world_banner_art</c> (<c>main_ms</c>, <c>mascots_ms</c>,
    /// <c>wait_ms</c> y <c>fallback</c>).
    /// </summary>
    public sealed class WorldBannerArtTests
    {
        /// <summary>
        /// Presupuesto [P] del hilo principal (ms, en el Editor; el mejor de 3 rondas): pedir el trabajo y crear las 6 texturas
        /// con los Color32 del otro hilo cuando acaba, dentro de los 8 ms de hilo principal por fotograma de 08 §5.4 (medido en
        /// el escritorio x64: ≈ 1,5-2 ms antes de pasar a SetPixels32). Pintarlas en el hilo principal costaba unos 230 ms (la
        /// medida de la revisión 2 de A4: viñeta ≈ 196-230, pata ≈ 15, Rosa ≈ 15, soplo ≈ 3, onda ≈ 2 y espuma ≈ 0,5).
        /// </summary>
        public const double MainBudgetMs = 8d;

        /// <summary>
        /// Presupuesto [P] de pintar el arte en el otro hilo (ms, en el Editor; el mejor de 3 rondas; medido en el escritorio x64:
        /// ≈ 226 ms): acota lo que el banner espera antes de entrar. Depende de la máquina, así que solo lo mira el test explícito
        /// de rendimiento (<see cref="BannerArt_Raster_FitsItsBudget"/>); en el juego, el tiempo límite de la vista
        /// (<c>WorldBannerView.ArtTimeoutMs</c>) garantiza que el banner entra aunque tarde más.
        /// </summary>
        public const double RasterBudgetMs = 500d;

        private const string WarningTag = "[AF.ProceduralArt] El arte del banner de mundo";

        // La viñeta del tamaño real: el ancho interior de la tarjeta en la pantalla de 360 dp y el alto que le da el mínimo
        // compartido de 281 dp (WorldBannerView.Layout), con las dos aguas del mundo 3 (el caso más caro).
        private static float Inner => Mathf.Min(LevelCardView.MaxWidthDp, 360f - (2f * LevelCardView.SideMarginDp)) - 32f;

        private const float SceneH = 152f;

        private readonly List<string> _warnings = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _warnings.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
        }

        [Test]
        public void BannerArt_OffTheMainThread_MainThreadFitsTheFrame()
        {
            double bestMain = double.MaxValue;
            string bestLine = string.Empty;
            for (int round = 0; round < 3; round++)
            {
                var art = new ProceduralArt("white");
                try
                {
                    Assert.That(art.MascotsBuilt, Is.False, "construir el arte común no genera el del banner");
                    var watch = Stopwatch.StartNew();
                    art.RequestBannerArt(Inner, SceneH, Palette.WaterBlack, WorldBannerView.MeetingWater(3), 3, true);
                    watch.Stop();
                    double request = watch.Elapsed.TotalMilliseconds;
                    Assert.That(art.BannerArtPending, Is.True, "en otro hilo");
                    Assert.That(art.MascotsMainMs, Is.EqualTo(0d));
                    Assert.That(art.WaitForBannerArt(30000), Is.True, "el hilo termina");
                    Assert.That(art.MascotsBuilt, Is.False, "nada hasta recogerlo en el hilo principal");
                    Assert.That(art.TryTakeBannerArt(), Is.True);
                    Assert.That(art.BannerArtPending, Is.False);
                    Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.None));
                    double main = request + art.MascotsMainMs;
                    double raster = art.MascotsMs;
                    Sprite scene = art.BannerSceneSprite!;
                    Assert.That(scene, Is.Not.Null);
                    Assert.That(scene.texture.width, Is.EqualTo(Mathf.RoundToInt(Inner * ProceduralArt.BannerScenePxPerDp)));
                    Assert.That(scene.texture.height, Is.EqualTo(Mathf.RoundToInt(SceneH * ProceduralArt.BannerScenePxPerDp)));
                    Assert.That(scene.texture.format, Is.EqualTo(TextureFormat.RGBA32));
                    Assert.That(scene.texture.isReadable, Is.False, "subida a la GPU sin copia en la CPU");
                    Assert.That(art.BannerSceneCount, Is.EqualTo(1));

                    // Las mascotas ya están: pedirlas no pinta nada más en el hilo principal.
                    double before = art.MascotsMainMs;
                    Assert.That(art.CapiPaw != null && art.Rosa != null && art.RosaSpray != null && art.RosaRipple != null && art.RosaFoam != null, Is.True);
                    Assert.That(art.MascotsMainMs, Is.EqualTo(before), "las mascotas venían con la viñeta");
                    Assert.That(art.TryTakeBannerArt(), Is.True, "listo: otra vez true sin hacer nada");

                    string line = "main=" + Ms(main) + " (pedir " + Ms(request) + ") raster=" + Ms(raster);
                    Debug.Log("AF_ART banner round " + round.ToString(CultureInfo.InvariantCulture) + ": " + line);
                    if (main < bestMain)
                    {
                        bestMain = main;
                        bestLine = line;
                    }

                    art.ReleaseBannerArt();
                    Assert.That(art.MascotsBuilt, Is.False, "se suelta al cerrar el banner");
                    Assert.That(art.BannerSceneSprite, Is.Null);
                    Assert.That(art.MascotsMs, Is.EqualTo(0d));
                    Assert.That(art.MascotsMainMs, Is.EqualTo(0d));
                }
                finally
                {
                    art.Dispose();
                }
            }

            Assert.That(bestMain, Is.LessThanOrEqualTo(PerfBudget.Ms(MainBudgetMs)), "arte del banner en el hilo principal (ms, " + PerfBudget.Describe(MainBudgetMs) + "): " + bestLine);
            Assert.That(_warnings, Is.Empty, "sin avisos");
        }

        [Test]
        public void BannerArt_PaintsTheCardSurfaceOffTheMainThread_TheMainThreadStillFitsTheFrame()
        {
            // T52 (observación 4 de la prueba del 2026-10-13): la superficie de madera de la tarjeta del banner (328 × 281 dp, la
            // compartida) se pintaba en el hilo principal al abrirlo (≈ 220 ms en el Editor, 66-111 ms en el teléfono). Ahora la
            // pinta el mismo trabajo del arte y el hilo principal solo la sube con el resto, dentro de los 8 ms; queda en la caché
            // de superficies (no se suelta con el arte del banner: la comparten las tarjetas previas).
            const float surfaceW = LevelCardView.MaxWidthDp - 16f;
            const float surfaceH = 281f;
            double bestMain = double.MaxValue;
            string bestLine = string.Empty;
            for (int round = 0; round < 3; round++)
            {
                var art = new ProceduralArt("white");
                try
                {
                    Assert.That(art.HasModalSurface(surfaceW, surfaceH), Is.False);
                    var watch = Stopwatch.StartNew();
                    art.RequestBannerArt(Inner, SceneH, Palette.WaterBlack, WorldBannerView.MeetingWater(3), 3, true, surfaceW, surfaceH);
                    watch.Stop();
                    double request = watch.Elapsed.TotalMilliseconds;
                    Assert.That(art.WaitForBannerArt(30000), Is.True, "el hilo termina");
                    Assert.That(art.HasModalSurface(surfaceW, surfaceH), Is.False, "nada hasta recogerlo en el hilo principal");
                    Assert.That(art.TryTakeBannerArt(), Is.True);
                    Assert.That(art.HasModalSurface(surfaceW, surfaceH), Is.True, "la superficie llega con el arte");
                    Sprite surface = art.ModalSurfaceFor(surfaceW, surfaceH);
                    Assert.That(surface.texture.width, Is.EqualTo(Mathf.RoundToInt(surfaceW * 1.5f)));
                    Assert.That(surface.texture.height, Is.EqualTo(Mathf.RoundToInt(surfaceH * 1.5f)));
                    Assert.That(surface.texture.isReadable, Is.False, "subida a la GPU sin copia en la CPU");
                    double main = request + art.MascotsMainMs;
                    string line = "main=" + Ms(main) + " (pedir " + Ms(request) + ") raster=" + Ms(art.MascotsMs);
                    Debug.Log("AF_ART banner+surface round " + round.ToString(CultureInfo.InvariantCulture) + ": " + line);
                    if (main < bestMain)
                    {
                        bestMain = main;
                        bestLine = line;
                    }

                    art.ReleaseBannerArt();
                    Assert.That(art.HasModalSurface(surfaceW, surfaceH), Is.True, "la superficie se queda en la caché de la sesión");
                    Assert.That(art.ModalSurfaceFor(surfaceW, surfaceH), Is.SameAs(surface));

                    // Con la superficie ya en la caché, otra petición no la vuelve a pintar.
                    art.RequestBannerArt(Inner, SceneH, Palette.WaterBlack, WorldBannerView.MeetingWater(3), 3, true, surfaceW, surfaceH);
                    Assert.That(art.WaitForBannerArt(30000) && art.TryTakeBannerArt(), Is.True);
                    Assert.That(art.ModalSurfaceFor(surfaceW, surfaceH), Is.SameAs(surface), "la misma de antes");
                    art.ReleaseBannerArt();
                }
                finally
                {
                    art.Dispose();
                }
            }

            Assert.That(bestMain, Is.LessThanOrEqualTo(PerfBudget.Ms(MainBudgetMs)), "arte del banner con la superficie, en el hilo principal (ms, " + PerfBudget.Describe(MainBudgetMs) + "): " + bestLine);
            Assert.That(_warnings, Is.Empty, "sin avisos");
        }

        [TestCase(328, 281)]
        [TestCase(328, 560)]
        public void ModalSurfaceColorAt_MatchesTheSurfaceWithoutTheGrain(int widthDp, int heightDp)
        {
            // Revisión de AU1: los fundidos del visor de Ajustes toman el color de la superficie en su borde. La veta solo
            // oscurece y ondula ±1,5 dp, así que en cada fila el píxel más claro es el de sin veta: debe ser el de la función.
            Color32[] px = ProceduralArt.ModalSurfacePixels(widthDp, heightDp);
            const float k = 1.5f; // px por dp de la superficie
            int w = Mathf.RoundToInt(widthDp * k);
            int h = Mathf.RoundToInt(heightDp * k);
            Assert.That(px.Length, Is.EqualTo(w * h));
            for (int y = 0; y < h; y += 7)
            {
                Color32 lightest = px[y * w];
                for (int x = 1; x < w; x++)
                {
                    Color32 c = px[(y * w) + x];
                    if (c.r + c.g + c.b > lightest.r + lightest.g + lightest.b)
                    {
                        lightest = c;
                    }
                }

                float yd = heightDp - ((y + 0.5f) / k);
                Color32 expected = ProceduralArt.ModalSurfaceColorAt(yd, heightDp);
                Assert.That(Mathf.Abs(lightest.r - expected.r), Is.LessThanOrEqualTo(1), "fila " + y);
                Assert.That(Mathf.Abs(lightest.g - expected.g), Is.LessThanOrEqualTo(1), "fila " + y);
                Assert.That(Mathf.Abs(lightest.b - expected.b), Is.LessThanOrEqualTo(1), "fila " + y);
            }
        }

        [TestCase(328, 281)]
        [TestCase(296, 144)]
        [TestCase(120, 90)]
        public void ModalSurface_PaintedFaster_SamePixelsAsBefore(int widthDp, int heightDp)
        {
            // T52: la veta de la superficie de madera solo recorre, en cada fila, las líneas que pueden llegar a ella (a menos de
            // 1,2 dp más su ondulación de ±1,5): exactamente los mismos píxeles que recorriendo todas las líneas en cada píxel. Y el
            // periodo y el tono de cada línea, calculados una vez por línea, solo cambian el redondeo de la coma flotante (el JIT
            // del Editor guarda los intermedios con más precisión): como mucho 1/255 en algún píxel suelto frente al código de
            // antes.
            Color32[] fast = ProceduralArt.ModalSurfacePixels(widthDp, heightDp);
            Color32[] allLines = ReferenceModalSurface(widthDp, heightDp, true);
            Color32[] before = ReferenceModalSurface(widthDp, heightDp, false);
            Assert.That(fast.Length, Is.EqualTo(allLines.Length));
            Assert.That(fast.Length, Is.EqualTo(before.Length));
            int differ = 0;
            int rounding = 0;
            int worst = 0;
            for (int i = 0; i < fast.Length; i++)
            {
                if (!fast[i].Equals(allLines[i]))
                {
                    differ++;
                }

                int d = Mathf.Max(Mathf.Max(Mathf.Abs(fast[i].r - before[i].r), Mathf.Abs(fast[i].g - before[i].g)), Mathf.Abs(fast[i].b - before[i].b));
                if (d > 0 || fast[i].a != before[i].a)
                {
                    rounding++;
                    worst = Mathf.Max(worst, d);
                }
            }

            Assert.That(differ, Is.EqualTo(0), "saltar las líneas lejanas no cambia ningún píxel (de " + fast.Length + ")");
            Assert.That(worst, Is.LessThanOrEqualTo(1), "frente al código de antes, como mucho 1/255 por canal");
            Assert.That(rounding, Is.LessThanOrEqualTo(fast.Length / 10000), "y en algún píxel suelto (" + rounding + " de " + fast.Length + ")");
        }

        [Test]
        [Explicit("Rendimiento: lo que tarda en pintarse el arte depende de la máquina (RasterBudgetMs [P], medido en el Editor).")]
        [Category("Performance")]
        public void BannerArt_Raster_FitsItsBudget()
        {
            double bestRaster = double.MaxValue;
            for (int round = 0; round < 3; round++)
            {
                var art = new ProceduralArt("white");
                try
                {
                    art.RequestBannerArt(Inner, SceneH, Palette.WaterBlack, WorldBannerView.MeetingWater(3), 3, true);
                    Assert.That(art.WaitForBannerArt(30000), Is.True, "el hilo termina");
                    Assert.That(art.TryTakeBannerArt(), Is.True);
                    bestRaster = System.Math.Min(bestRaster, art.MascotsMs);
                    Debug.Log("AF_ART banner raster round " + round.ToString(CultureInfo.InvariantCulture) + ": " + Ms(art.MascotsMs));
                    art.ReleaseBannerArt();
                }
                finally
                {
                    art.Dispose();
                }
            }

            Assert.That(bestRaster, Is.LessThanOrEqualTo(PerfBudget.Ms(RasterBudgetMs)), "arte del banner en el otro hilo (ms, " + PerfBudget.Describe(RasterBudgetMs) + ")");
        }

        [Test]
        public void BannerArt_InPlace_IsTheSameRequest()
        {
            // Sin hilo (los tests que miden instantes exactos de la entrada): listo al volver, todo pintado en el hilo principal.
            var art = new ProceduralArt("white");
            try
            {
                art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, false);
                Assert.That(art.BannerArtPending, Is.False);
                Assert.That(art.TryTakeBannerArt(), Is.True);
                Assert.That(art.MascotsBuilt, Is.True);
                Assert.That(art.MascotsMainMs, Is.GreaterThanOrEqualTo(art.MascotsMs), "pintado en el hilo principal");
                Sprite scene = art.BannerSceneSprite!;

                // La misma petición no repite nada; la viñeta síncrona del mismo tamaño es la misma.
                double main = art.MascotsMainMs;
                art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, true);
                Assert.That(art.BannerArtPending, Is.False, "ya estaba");
                Assert.That(art.MascotsMainMs, Is.EqualTo(main));
                Assert.That(art.BannerScene(Inner, SceneH, Palette.WaterClear, null, 2), Is.SameAs(scene));
            }
            finally
            {
                art.Dispose();
            }
        }

        [Test]
        public void BannerArt_ReleasedWhilePainting_IsDiscarded()
        {
            // El banner se cierra (o se carga otro nivel) antes de que llegue su arte: lo que el otro hilo pintaba no se recoge.
            var art = new ProceduralArt("white");
            try
            {
                art.RequestBannerArt(Inner, SceneH, Palette.WaterWhite, null, 4, true);
                Assert.That(art.BannerArtPending, Is.True);
                art.ReleaseBannerArt();
                Assert.That(art.BannerArtPending, Is.False);
                Assert.That(art.TryTakeBannerArt(), Is.False, "no hay nada pedido");
                System.Threading.Thread.Sleep(50);
                Assert.That(art.MascotsBuilt, Is.False);
                Assert.That(art.BannerSceneSprite, Is.Null);

                // Otra petición con otras medidas sustituye a la pendiente.
                art.RequestBannerArt(Inner, SceneH, Palette.WaterWhite, null, 4, true);
                art.RequestBannerArt(Inner, SceneH + 10f, Palette.WaterWhite, null, 4, true);
                Assert.That(art.WaitForBannerArt(30000), Is.True);
                Assert.That(art.TryTakeBannerArt(), Is.True);
                Assert.That(art.BannerSceneSprite!.texture.height, Is.EqualTo(Mathf.RoundToInt((SceneH + 10f) * ProceduralArt.BannerScenePxPerDp)));
                Assert.That(art.BannerSceneCount, Is.EqualTo(1));
            }
            finally
            {
                art.Dispose();
            }
        }

        [Test]
        public void BannerArt_JobThatFailsInItsThread_DoesNotThrowOnTheMainThread_AndLeavesTheFailureState()
        {
            // Revisión 3 de A4, punto 1: un fallo del otro hilo no sale en el hilo principal (la vista entraría sin arte).
            var art = new ProceduralArt("white");
            try
            {
                art.BannerArtFault = BannerArtFault.Throw;
                art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, true);
                Assert.That(art.BannerArtPending, Is.True, "en otro hilo");
                Assert.That(art.WaitForBannerArt(30000), Is.True, "el hilo termina (con el fallo)");
                Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.None), "aún sin recoger");

                bool taken = true;
                Assert.That(() => { taken = art.TryTakeBannerArt(); }, Throws.Nothing, "el fallo del otro hilo no lanza aquí");
                Assert.That(taken, Is.False);
                Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.Error), "el estado de fallo");
                Assert.That(art.BannerArtPending, Is.False);
                Assert.That(art.MascotsBuilt, Is.False);
                Assert.That(art.BannerSceneSprite, Is.Null);
                Assert.That(art.TryTakeBannerArt(), Is.False, "otra vez: nada");
                Assert.That(_warnings.Count, Is.EqualTo(1), "un solo aviso: " + string.Join(" | ", _warnings));
                Assert.That(_warnings[0], Does.Contain("falló al pintarse").And.Contain("InvalidOperationException"));

                // Otra petición sin fallo lo deja atrás.
                art.BannerArtFault = BannerArtFault.None;
                art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, true);
                Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.None));
                Assert.That(art.WaitForBannerArt(30000), Is.True);
                Assert.That(art.TryTakeBannerArt(), Is.True);
                Assert.That(_warnings.Count, Is.EqualTo(1));

                // Pedido en el acto (sin hilo), el mismo fallo tampoco lanza.
                art.ReleaseBannerArt();
                art.BannerArtFault = BannerArtFault.Throw;
                Assert.That(() => art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, false), Throws.Nothing);
                Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.Error));
                Assert.That(art.MascotsBuilt, Is.False);
                Assert.That(_warnings.Count, Is.EqualTo(2));
            }
            finally
            {
                art.Dispose();
            }
        }

        [Test]
        public void BannerArt_QueueRejectedOrNotSupported_LeavesTheFailureState()
        {
            // Revisión 3 de A4, punto 1c: si QueueUserWorkItem devuelve false o lanza NotSupportedException (como en
            // ProfileWriter.Start), no se pinta en el hilo principal: el estado de fallo, un aviso y la vista entra sin arte.
            foreach (BannerArtFault fault in new[] { BannerArtFault.QueueRejected, BannerArtFault.QueueNotSupported })
            {
                _warnings.Clear();
                var art = new ProceduralArt("white");
                try
                {
                    art.BannerArtFault = fault;
                    Assert.That(() => art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, true), Throws.Nothing, fault.ToString());
                    Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.Queue), fault.ToString());
                    Assert.That(art.BannerArtPending, Is.False, fault + ": nada pendiente");
                    Assert.That(art.MascotsMainMs, Is.EqualTo(0d), fault + ": nada pintado en el hilo principal");
                    Assert.That(art.TryTakeBannerArt(), Is.False);
                    Assert.That(art.MascotsBuilt, Is.False);
                    Assert.That(_warnings.Count, Is.EqualTo(1), fault + ": un solo aviso");
                    Assert.That(_warnings[0], Does.Contain("no se pudo encolar"));
                }
                finally
                {
                    art.Dispose();
                }
            }
        }

        [Test]
        public void BannerArt_Abandoned_TheJobStops_AndItsArtIsDiscarded()
        {
            // El tiempo límite de la vista abandona el arte: el trabajo pendiente se cancela (lo mira por fila y entre piezas, así
            // que deja de gastar CPU) y lo que pinte ya no se recoge (revisión 3 de A4, puntos 1b y 7).
            var art = new ProceduralArt("white");
            try
            {
                art.BannerArtFault = BannerArtFault.Stall; // no acaba hasta que lo cancelan (o 30 s)
                art.RequestBannerArt(Inner, SceneH, Palette.WaterClear, null, 2, true);
                Assert.That(WaitUntil(() => ProceduralArt.BannerArtJobsRunning > 0, 10000), Is.True, "el trabajo arranca en otro hilo");
                art.AbandonBannerArt();
                Assert.That(art.BannerArtPending, Is.False);
                Assert.That(WaitUntil(() => ProceduralArt.BannerArtJobsRunning == 0, 5000), Is.True, "cancelado, para sin esperar sus 30 s");
                Assert.That(art.TryTakeBannerArt(), Is.False, "nada que recoger");
                Assert.That(art.BannerArtFailure, Is.EqualTo(BannerArtFailure.None), "abandonarlo no es un fallo");
                Assert.That(art.MascotsBuilt, Is.False);

                // Un trabajo de verdad abandonado a medias tampoco deja nada: lo que pinte, nadie lo recoge.
                art.BannerArtFault = BannerArtFault.None;
                art.RequestBannerArt(Inner, SceneH, Palette.WaterWhite, null, 4, true);
                art.AbandonBannerArt();
                Assert.That(WaitUntil(() => ProceduralArt.BannerArtJobsRunning == 0, 30000), Is.True);
                Assert.That(art.TryTakeBannerArt(), Is.False);
                Assert.That(art.MascotsBuilt, Is.False);
                Assert.That(art.BannerSceneSprite, Is.Null);
                Assert.That(_warnings, Is.Empty, "abandonar no avisa");
            }
            finally
            {
                art.Dispose();
            }
        }

        private static bool WaitUntil(System.Func<bool> condition, int timeoutMs)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.ElapsedMilliseconds >= timeoutMs)
                {
                    return false;
                }

                System.Threading.Thread.Sleep(2);
            }

            return true;
        }

        private static string Ms(double ms)
        {
            return ms.ToString("0.0", CultureInfo.InvariantCulture);
        }

        // La superficie de madera recorriendo todas las líneas de veta en cada píxel, con el mismo hash: con perLine, con el
        // periodo y el tono calculados una vez por línea (como ahora); sin él, en cada píxel (el código de antes de T52).
        private static Color32[] ReferenceModalSurface(int widthDp, int heightDp, bool perLine)
        {
            const float k = 1.5f;
            int w = Mathf.RoundToInt(widthDp * k);
            int h = Mathf.RoundToInt(heightDp * k);
            var px = new Color32[w * h];
            const int maxLines = 64;
            float[] lineY = new float[maxLines];
            int lines = 0;
            float yLine = 4f;
            while (yLine < heightDp && lines < maxLines)
            {
                lineY[lines++] = yLine;
                yLine += 7f + (Hash(lines, 3, 557) * 4f);
            }

            float[] period = new float[maxLines];
            float[] tone = new float[maxLines];
            for (int i = 0; i < lines; i++)
            {
                period[i] = 40f + (Hash(i, 5, 557) * 40f);
                tone[i] = Mathf.Lerp(0.94f, 0.96f, Hash(i, 7, 557));
            }

            for (int y = 0; y < h; y++)
            {
                float yd = heightDp - ((y + 0.5f) / k);
                float t = Mathf.Clamp01(yd / heightDp);
                Color baseC = Color.Lerp(Palette.PanelLight, Palette.PanelLightLow, t);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float mult = 1f;
                    for (int i = 0; i < lines; i++)
                    {
                        float wave = perLine
                            ? 1.5f * Mathf.Sin((xd / period[i]) * Mathf.PI * 2f + i)
                            : 1.5f * Mathf.Sin((xd / (40f + (Hash(i, 5, 557) * 40f))) * Mathf.PI * 2f + i);
                        float d = Mathf.Abs(yd - (lineY[i] + wave));
                        if (d < 1.2f)
                        {
                            float m = perLine ? tone[i] : Mathf.Lerp(0.94f, 0.96f, Hash(i, 7, 557));
                            mult = Mathf.Min(mult, Mathf.Lerp(m, 1f, d / 1.2f));
                        }
                    }

                    var c = new Color(baseC.r * mult, baseC.g * mult, baseC.b * mult, 1f);
                    float lustre = Mathf.Clamp01(1f - (yd / Mathf.Max(12f, heightDp * 0.3f)));
                    c = Color.Lerp(c, Color.white, 0.08f * lustre);
                    px[(y * w) + x] = c;
                }
            }

            return px;
        }

        // ArtNoise.Hash (interno de AF.Presentation), copiado tal cual.
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint hh = (uint)((x * 374761393) + (y * 668265263) + (seed * 1442695041));
                hh = (hh ^ (hh >> 13)) * 1274126177u;
                hh ^= hh >> 16;
                return (hh & 0x00FFFFFF) / 16777216f;
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning && condition.StartsWith(WarningTag, System.StringComparison.Ordinal))
            {
                _warnings.Add(condition);
            }
        }
    }
}
