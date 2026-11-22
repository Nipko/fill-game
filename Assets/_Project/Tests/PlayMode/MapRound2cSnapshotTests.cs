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
    /// Capturas de los arreglos del Mapa de la prueba r2c de la 1.1 (<c>docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2c.md</c>)
    /// a 360 dp, en <c>Logs/snapshots/es-419/r2c_*.png</c>: la vista Mundo del 521 con la ventanita de casa (observación 1), el Mapa
    /// del 10 del 132 con «Mundo» pulsado justo al llegar la canoa (observación 2) y la Cuenca del Mapa del 10 del 71 y el 79 con la
    /// placa del mundo siguiente (la placa cortada). Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas de la prueba r2c de la 1.1; necesitan dispositivo gráfico.")]
    public sealed class MapRound2cSnapshotTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 16, 18, 0, 0, System.DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.L10SequenceOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.L10SequenceOverride = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheMapOfRound2c_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;

            // Observación 1: el 521 en la vista Mundo, con la ventanita de casa y «Trinidad · faltan N niveles».
            {
                yield return Load(522, 521, 521);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                SnapshotKit.AttachOverlay(map.Canvas);
                map.SetView(MapView.World);
                yield return SnapshotKit.Wait(1.0f);
                map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "r2c_mundo_521_360dp");
                map.Close();
            }

            // Observación 2: el Mapa del 10 del 132 (af_max_level de un salto) y «Mundo» pulsado un fotograma después de
            // map_play_ready; y la placa: el Mapa del 10 del 71 y el 79 al acabar la secuencia y su Cuenca tras el tucuxi.
            foreach (int max in new[] { 132, 71, 79 })
            {
                yield return Load(max + 1, max, 0);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                if (!map.Visible)
                {
                    Assert.That(boot.OpenRiverMap(MapOpenSource.L10), Is.True);
                }

                Assert.That(map.Visible && map.Source == MapOpenSource.L10, Is.True, max + ": el Mapa del 10 (el inicio lo abre solo con el Mapa sin ver)");
                SnapshotKit.AttachOverlay(map.Canvas);
                for (int i = 0; i < 1200 && map.JourneyReadyMs < 0f; i++)
                {
                    yield return null;
                }

                if (max == 132)
                {
                    yield return MapRound2cTests.TapTop(map, map.ViewWorldButton);
                    Debug.Log("AF_MAP r2c captura: «Mundo» tras map_play_ready, vista " + map.View);
                    yield return SnapshotKit.Wait(0.6f);
                    SnapshotKit.Save(dir, "r2c_l10_132_mundo_al_llegar_360dp");
                }
                else
                {
                    // Con el Mapa sin ver, tras la secuencia se une el tucuxi (pasa a la vista Mundo con su primer plano): la Cuenca
                    // se vuelve a pedir al acabar, con la placa del mundo siguiente.
                    for (int i = 0; i < 1200 && !(map.L10SequenceDone && map.L10DimAmount <= 0f); i++)
                    {
                        yield return null;
                    }

                    SnapshotKit.Save(dir, "r2c_l10_" + max.ToString("000") + "_fin_secuencia_360dp");
                    yield return SnapshotKit.Wait(5f);
                    map.SetView(MapView.Basin);
                    yield return SnapshotKit.Wait(0.5f);
                    SnapshotKit.Save(dir, "r2c_l10_" + max.ToString("000") + "_cuenca_360dp");
                    Debug.Log("AF_MAP r2c captura " + max + ": placa " + (map.SilhouetteCount > 0 ? map.SilhouetteViewBox(0).ToString() : "-") + " en " + map.ViewportSize);
                }

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
