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
    /// Capturas de los arreglos del Mapa de la prueba r2d de la 1.1 (<c>docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2d.md</c>)
    /// a 360 dp, en <c>Logs/snapshots/es-419/r2d_*.png</c>: el Mapa abierto desde el inicio en la vista Mundo con lo visto atrasado
    /// diez niveles (el 79, el 71 y el 521: observaciones 1 y 2 b), al abrir y tras ponerse al día, y «Mundo» pulsado justo al
    /// acabar el viaje del Mapa del 10 (el 79, a los 3 s, aún en el primer plano del tucuxi, y 3 s después de él; el 521, a los 3 s:
    /// observación 2 a). Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas de la prueba r2d de la 1.1; necesitan dispositivo gráfico.")]
    public sealed class MapRound2dSnapshotTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 16, 22, 0, 0, System.DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
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
        public IEnumerator CaptureTheMapOfRound2d_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;

            // Observaciones 1 y 2 (b): abierto de golpe en la vista Mundo con lo visto diez niveles atrás.
            foreach (int max in new[] { 79, 71, 521 })
            {
                RiverMapScreen.ResetCapiSession();
                yield return Load(max + 1, max, max - 10);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                SnapshotKit.AttachOverlay(map.Canvas);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "r2d_mundo_" + max.ToString("000") + "_al_abrir_360dp");
                yield return WaitQuiet(map);
                yield return SnapshotKit.Wait(1.0f);
                map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "r2d_mundo_" + max.ToString("000") + "_al_dia_360dp");
                Debug.Log("AF_MAP r2d captura " + max + ": rótulo «" + map.StopLabelText(map.RemainingStopN()).Replace('\n', ' ') + "» en "
                    + map.LocalToView(map.StopLabelLocalRect(map.RemainingStopN())) + ", premio junto " + map.PrizeInStopLabel + ", zoom ×" + map.Zoom.ToString("0.00"));
                boot.CloseRiverMap();
            }

            // Observación 2 (a): «Mundo» justo tras map_play_ready del viaje del 10.
            foreach (int max in new[] { 79, 521 })
            {
                RiverMapScreen.ResetCapiSession();
                yield return Load(max + 1, max, 0);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                if (!map.Visible)
                {
                    Assert.That(boot.OpenRiverMap(MapOpenSource.L10), Is.True);
                }

                SnapshotKit.AttachOverlay(map.Canvas);
                for (int i = 0; i < 1200 && map.JourneyReadyMs < 0f; i++)
                {
                    yield return null;
                }

                yield return MapRound2cTests.TapTop(map, map.ViewWorldButton);
                yield return SnapshotKit.Wait(3f);
                SnapshotKit.Save(dir, "r2d_l10_" + max.ToString("000") + "_mundo_3s_360dp");
                Debug.Log("AF_MAP r2d captura «Mundo» " + max + " a los 3 s: compañero " + map.CrewIntroKind + ", primer plano ×" + map.CameraCloseUp.ToString("0.00")
                    + ", rótulos ×" + map.CloseUpLabelAlpha);
                if (map.CrewIntroKind != null)
                {
                    yield return WaitQuiet(map);
                    yield return SnapshotKit.Wait(3f);
                    SnapshotKit.Save(dir, "r2d_l10_" + max.ToString("000") + "_mundo_3s_tras_el_compañero_360dp");
                }

                boot.CloseRiverMap();
            }
        }

        // Hasta que nada apaga los rótulos (el viaje, la llegada, el compañero); como mucho 12 s.
        private static IEnumerator WaitQuiet(RiverMapScreen map)
        {
            float t0 = Time.realtimeSinceStartup;
            yield return SnapshotKit.Wait(0.5f);
            while (Time.realtimeSinceStartup - t0 < 12f
                && (map.CatchingUp || map.CrewIntroWaiting || map.CrewArriving || map.CameraCloseUp > 1f || map.CloseUpLabelAlpha < 1f))
            {
                yield return null;
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
