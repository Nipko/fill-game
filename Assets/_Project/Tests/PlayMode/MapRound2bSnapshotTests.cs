#nullable enable
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de los arreglos del Mapa de la prueba r2b de la 1.1 (<c>docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2b.md</c>,
    /// observaciones 1 y 3-6) a 360 dp, en <c>Logs/snapshots/es-419/r2b_*.png</c>: el primer plano del tucuxi tras el Mapa del 10,
    /// las boyas y los rótulos del 71 y el 79, el viaje del 80 con la placa de Casa, las placas de los mundos siguientes en el 132,
    /// el 140 y el 291 (y la Cuenca del 132) y Bolivia en el 451. Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas de la prueba r2b de la 1.1; necesitan dispositivo gráfico.")]
    public sealed class MapRound2bSnapshotTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 16, 12, 0, 0, System.DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheMapOfRound2b_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;

            // El Mapa abierto desde la pausa con max_level N (como las capturas s23/30-mapa-N de Spark), en la vista Mundo.
            foreach (int max in new[] { 71, 79, 132, 140, 291, 300, 445, 451 })
            {
                yield return Load(max + 1, max, max);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True); // carga los datos del Mapa
                map.Close();
                map.Open(MapOpenSource.Pause, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(max + 1, max, max), max + 1, true);
                SnapshotKit.AttachOverlay(map.Canvas);
                yield return SnapshotKit.Wait(1.0f);
                map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "r2b_mapa_" + max.ToString("000") + "_360dp");
                if (max == 132)
                {
                    map.SetView(MapView.Basin);
                    yield return SnapshotKit.Wait(0.5f);
                    SnapshotKit.Save(dir, "r2b_mapa_" + max.ToString("000") + "_cuenca_360dp");
                }

                map.Close();
            }

            // El viaje del 80 a mitad y al acabar (la placa de Casa y la píldora del premio del mundo).
            {
                yield return Load(81, 80, 70);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                SnapshotKit.AttachOverlay(map.Canvas);
                MapRound2Tests.Open(boot, map, 81, 80, 70);
                for (int i = 0; i < 600 && map.CatchUpElapsedMs < 1500f; i++)
                {
                    yield return null;
                }

                SnapshotKit.Save(dir, "r2b_viaje_080_salida_360dp");
                for (int i = 0; i < 600 && map.CatchUpElapsedMs < 3000f; i++)
                {
                    yield return null;
                }

                SnapshotKit.Save(dir, "r2b_viaje_080_mitad_360dp");
                for (int i = 0; i < 900 && (map.JourneyLocked || map.ArrivalCloseUpActive || map.CameraCloseUp > 1f); i++)
                {
                    yield return null;
                }

                yield return SnapshotKit.Wait(0.5f);
                SnapshotKit.Save(dir, "r2b_viaje_080_final_360dp");
                map.Close();
            }

            // El tucuxi: el 51 ganado con el Mapa sin ver (el perfil de la prueba), el Mapa del 10 desde el inicio y su primer plano.
            {
                yield return Load(52, 51, 0);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                Assert.That(boot.OpenRiverMap(MapOpenSource.L10), Is.True);
                SnapshotKit.AttachOverlay(map.Canvas);
                bool mid = false;
                int after = 0;
                for (int i = 0; i < 2400; i++)
                {
                    if (!mid && map.L10SequenceActive && map.L10SequenceElapsedMs > 3000f)
                    {
                        mid = true;
                        SnapshotKit.Save(dir, "r2b_tucuxi_secuencia_360dp");
                    }

                    if (map.CrewChipShown && map.CameraCloseUp > RiverMapScreen.CamCloseUpZoom * 0.95f)
                    {
                        break;
                    }

                    if (map.L10SequenceDone && ++after > 240)
                    {
                        break; // sin primer plano: 4 s después de la secuencia
                    }

                    yield return null;
                }

                SnapshotKit.Save(dir, "r2b_tucuxi_primer_plano_360dp");
                Debug.Log("AF_MAP r2b tucuxi: vista " + map.View + ", chip " + map.CrewChipShown + ", zoom ×" + map.CameraCloseUp);
                boot.CloseRiverMap();
            }
        }

        private static IEnumerator Load(int play, int max, int seen)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = play;
            profile.Progress.MaxLevel = max;
            profile.Progress.MapSeenLevel = seen;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            SnapshotKit.AttachLobby(GameBootstrap.Current!);
            yield return null;
        }
    }
}
