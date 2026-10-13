#nullable enable
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
    /// Presupuesto del arte procedural del banner de mundo (08 §5.4: carga de nivel &lt; 300 ms e hilo principal ≤ 8 ms por
    /// fotograma; tarea A4, revisión 2): las piezas que pide <c>WorldBannerView.Show</c> al abrirse (la viñeta con Capi, la pata,
    /// Rosa, el soplo, la onda y la espuma) se pintan en un hilo del grupo (<see cref="ProceduralArt.RequestBannerArt"/>), así
    /// que en el fotograma de la carga del 11, el 41, el 81 o el 141 el hilo principal solo pide el trabajo y, cuando acaba,
    /// crea las texturas. Se acotan las dos cosas: lo que toca al hilo principal (un fotograma) y lo que tarda en pintarse (lo
    /// que el banner espera antes de entrar). En el teléfono se miden con <c>AF_STATE world_banner_art</c> (<c>main_ms</c>,
    /// <c>mascots_ms</c> y <c>wait_ms</c>).
    /// </summary>
    public sealed class WorldBannerArtTests
    {
        /// <summary>
        /// Presupuesto [P] del hilo principal (ms, en el Editor; el mejor de 3 rondas): pedir el trabajo y crear las 6 texturas
        /// cuando acaba, dentro de los 8 ms de hilo principal por fotograma de 08 §5.4 (medido en el escritorio x64: ≈ 1,5-2 ms).
        /// Pintarlas en el hilo principal costaba unos 230 ms (la medida de la revisión 2 de A4: viñeta ≈ 196-230, pata ≈ 15,
        /// Rosa ≈ 15, soplo ≈ 3, onda ≈ 2 y espuma ≈ 0,5).
        /// </summary>
        public const double MainBudgetMs = 8d;

        /// <summary>
        /// Presupuesto [P] de pintar el arte en el otro hilo (ms, en el Editor; el mejor de 3 rondas; medido en el escritorio x64:
        /// ≈ 226 ms): acota lo que el banner espera antes de entrar.
        /// </summary>
        public const double RasterBudgetMs = 500d;

        // La viñeta del tamaño real: el ancho interior de la tarjeta en la pantalla de 360 dp y el alto que le da el mínimo
        // compartido de 281 dp (WorldBannerView.Layout), con las dos aguas del mundo 3 (el caso más caro).
        private static float Inner => Mathf.Min(LevelCardView.MaxWidthDp, 360f - (2f * LevelCardView.SideMarginDp)) - 32f;

        private const float SceneH = 152f;

        [Test]
        public void BannerArt_OffTheMainThread_FitsTheFrameBudget()
        {
            double bestMain = double.MaxValue;
            double bestRaster = double.MaxValue;
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
                    double main = request + art.MascotsMainMs;
                    double raster = art.MascotsMs;
                    Sprite scene = art.BannerSceneSprite!;
                    Assert.That(scene, Is.Not.Null);
                    Assert.That(scene.texture.width, Is.EqualTo(Mathf.RoundToInt(Inner * ProceduralArt.BannerScenePxPerDp)));
                    Assert.That(scene.texture.height, Is.EqualTo(Mathf.RoundToInt(SceneH * ProceduralArt.BannerScenePxPerDp)));
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

                    bestRaster = System.Math.Min(bestRaster, raster);
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

            Assert.That(bestMain, Is.LessThanOrEqualTo(MainBudgetMs), "arte del banner en el hilo principal (ms): " + bestLine);
            Assert.That(bestRaster, Is.LessThanOrEqualTo(RasterBudgetMs), "arte del banner en el otro hilo (ms): " + bestLine);
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

        private static string Ms(double ms)
        {
            return ms.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
