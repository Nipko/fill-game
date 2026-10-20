#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarea FX13 (el último tramo, 691-700, apunta a la naciente del Mismi; D-013, viñeta del 2026-10-19;
    /// <c>[Explicit]</c>, no forman parte de la suite), a 1080 × 2340 px (360 × 780 dp):
    /// <list type="bullet">
    /// <item><description><c>fx13_map_695</c> y <c>fx13_map_695_cuenca</c>: el Mapa desde el inicio jugando el 695 (max 694), en
    /// las vistas Mundo y Cuenca: el frente y sus 10 boyas de Tambo a la naciente (4 encendidas), la canoa a 0,4 del tramo y
    /// «Faltan 6 niveles para El nevado Mismi», con el hito y el rótulo de la naciente (revisión de FX13);</description></item>
    /// <item><description><c>fx13_map_fin_contenido_700_abre</c>, <c>fx13_map_fin_contenido_700_medio</c> y
    /// <c>fx13_map_fin_contenido_700</c>: el Mapa del fin de contenido tras ganar el 700, al abrir (la canoa en Tambo), a medio
    /// viaje y al acabar (la canoa en la naciente, con el tramo lleno), con la cabecera «Ucayali» y el hito
    /// y el rótulo de la naciente.</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Fx13SnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de FX13; necesitan dispositivo gráfico.")]
    public sealed class Fx13SnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ContentEndMapOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureMapAt695()
        {
            string dir = SnapshotKit.Prepare();
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return Fx13Tests.LoadAt(695);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Finish(map);
            yield return null;
            Assert.That(map.FrontStop, Is.EqualTo(70));
            Assert.That(map.StopRemainingText, Is.EqualTo("Faltan 6 niveles para El nevado Mismi"));
            SnapshotKit.Save(dir, "fx13_map_695");
            map.SetView(MapView.Basin);
            map.Tick(16f);
            yield return null;
            SnapshotKit.Save(dir, "fx13_map_695_cuenca");
            Debug.Log("FX13 fx13_map_695: canoa " + map.CanoeMapPoint + " a la vista " + map.CanoeInView);
        }

        [UnityTest]
        public IEnumerator CaptureContentEndMapAt700()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return Fx10Tests.OpenContentEnd();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            map.Tick(16f);
            yield return null;
            SnapshotKit.Save(dir, "fx13_map_fin_contenido_700_abre");
            int half = map.ContentEndJourneyMs / 2;
            for (int t = 0; t < half; t += 16)
            {
                map.Tick(16f);
            }

            yield return null;
            SnapshotKit.Save(dir, "fx13_map_fin_contenido_700_medio");
            Finish(map);
            yield return null;
            Fx10Tests.AssertPlatesFit(map, "fin de contenido del 700, al acabar");
            SnapshotKit.Save(dir, "fx13_map_fin_contenido_700");
            Debug.Log("FX13 fx13_map_fin_contenido_700: canoa " + map.CanoeMapPoint + " a la vista " + map.CanoeInView + ", frente "
                + map.FrontStop + " lleno " + map.FrontFraction);
        }

        // «Ponerse al día» (o el viaje del fin de contenido) hasta el final, fotograma a fotograma, y el fundido.
        private static void Finish(RiverMapScreen map)
        {
            for (int t = 0; t < 30000 && (map.CatchingUp || map.CatchUpFading); t += 16)
            {
                map.Tick(16f);
            }

            map.Tick(16f);
        }
    }
}
