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
    /// Capturas de la tarea FX10 (observación 3 de la prueba de la ola 6 en el teléfono; <c>[Explicit]</c>, no forman parte de la
    /// suite), a 1080 × 2340 px (360 × 780 dp), con la casa de TiTino y su ventanita:
    /// <list type="bullet">
    /// <item><description><c>fx10_map_pausa41_abre</c> y <c>fx10_map_pausa41</c>: el Mapa desde la pausa del 41 (max 40 y
    /// <c>map_seen_level</c> 30), al abrir (la placa del mundo 3, Manaos, que se salía por la derecha, desplazada hacia dentro y
    /// entera) y al acabar «ponerse al día» (la del 3 ya no sale y la del mundo 4, Trapecio, se vuelve a colocar junto a su
    /// punto, fuera de este encuadre; antes heredaba el sitio de la del 3 y se salía por la derecha);</description></item>
    /// <item><description><c>fx10_map_fin_contenido_300_abre</c> y <c>fx10_map_fin_contenido_300</c>: el Mapa del fin de contenido
    /// tras ganar el 300, al abrir (la placa del mundo 7, Madre de Dios, de 215 dp, que se salía por la izquierda bajo la
    /// ventanita, entera arriba a la izquierda, por encima de la canoa con sus banderas) y al acabar el viaje (la del 7 ya no
    /// sale y la del 8, Madidi, se vuelve a colocar junto a su punto, fuera de este encuadre; antes heredaba el sitio de la del 7,
    /// cortada por la izquierda y bajo la ventanita).</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Fx10SnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de FX10; necesitan dispositivo gráfico.")]
    public sealed class Fx10SnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ContentEndMapOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CapturePauseMapAt41()
        {
            string dir = SnapshotKit.Prepare();
            MapArtRule.ForceProvisional = true;
            yield return Fx10Tests.OpenFromPause41(40, 30);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.Attach(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            map.Tick(16f);
            yield return null;
            Fx10Tests.AssertPlatesFit(map, "pausa del 41, al abrir");
            SnapshotKit.Save(dir, "fx10_map_pausa41_abre");
            Finish(map);
            yield return null;
            Fx10Tests.AssertPlatesFit(map, "pausa del 41, al acabar");
            SnapshotKit.Save(dir, "fx10_map_pausa41");
            Log("fx10_map_pausa41", map);
        }

        [UnityTest]
        public IEnumerator CaptureContentEndMap()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            yield return Fx10Tests.OpenContentEnd();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            map.Tick(16f);
            yield return null;
            Fx10Tests.AssertPlatesFit(map, "fin de contenido, al abrir");
            SnapshotKit.Save(dir, "fx10_map_fin_contenido_300_abre");
            Finish(map);
            yield return null;
            Fx10Tests.AssertPlatesFit(map, "fin de contenido, al acabar");
            SnapshotKit.Save(dir, "fx10_map_fin_contenido_300");
            Log("fx10_map_fin_contenido_300", map);
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

        private static void Log(string name, RiverMapScreen map)
        {
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Debug.Log("FX10 " + name + ": la placa «" + map.SilhouetteText(i) + "» en " + map.SilhouetteViewBox(i) + " (zona visible "
                    + map.ViewportSize + "; ventanita " + (map.HomeWindowShown ? map.HomeWindowViewRect.ToString() : "no") + "; oculta "
                    + map.PlateFitHidden(i) + ")");
            }
        }
    }
}
