#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «Ponerse al día» al abrir el Mapa del río (tarea M3.5; D-013, punto 14; propuesta del Mapa §4.2 y §11.5; 08 §8.2), en la
    /// escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>con <c>map_seen_level</c> 52 y <c>max_level</c> 58, las 6 boyas se encienden de una en una cada 150 ms (900 ms ≤
    /// 1,5 s), la canoa va con ellas y, al cerrar, lo visto queda en 58 y la pestaña del inicio sin punto;</item>
    /// <item>una llegada sin ver (el 50 con seen 45) la recorre la canoa: llega a Manaos, se llena su mini cuia y escribe
    /// <c>stop_arrive</c> con <c>view</c> = <c>map</c>; el punto de la pestaña desaparece al cerrar;</item>
    /// <item>un toque lo completa (la llegada que faltaba, saltada); cerrarlo a medias, también; con Reducir movimiento, el estado
    /// final sin viaje ni llenado animado, con un fundido de 150 ms en lo nuevo; un toque en Mundo, Cuenca o «Volver a la canoa»
    /// también lo completa;</item>
    /// <item>la franja de llegada de la victoria del 20 anota lo visto hasta el 20 (y la del 21, que no es llegada, nada).</item>
    /// </list>
    /// </summary>
    public sealed class MapCatchUpTests
    {
        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Seen52Max58_SixBuoysOneBy150ms_InAtMost1500ms_ThenSeenAndNoDot()
        {
            var lines = new List<string>();
            using var probe = new ProbeLog(lines);
            yield return MapMarkersTests.OpenApp(59, 52);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.Lobby!.MapTabDotShown, Is.False, "52 y 58: el mismo tramo, ninguna llegada sin ver");
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(52));
            Assert.That(map.CatchUpBuoys, Is.EqualTo(6));
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(900));
            Assert.That(map.CatchUpDurationMs, Is.LessThanOrEqualTo(MapConfig.CatchUpMaxMs));
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(RiverJourney.CatchUpMs(52, 58)));
            Assert.That(map.ProgressLevel, Is.EqualTo(52), "el río arranca en lo ya visto");
            Assert.That(LitBuoys(map), Is.EqualTo(2), "las del 51 y el 52");
            Assert.That(Vector2.Distance(map.CanoeShownMapPoint, RiverMapScreen.CanoeAt(boot.RiverMapData!, boot.RiverRoutes!, 52)), Is.LessThan(1e-6f));

            Vector2 previous = map.CanoeShownMapPoint;
            for (int step = 1; step <= 6; step++)
            {
                map.Tick(149f);
                Assert.That(map.ProgressLevel, Is.EqualTo(52 + step - 1), "a los " + ((step * 150) - 1) + " ms");
                map.Tick(1f);
                Assert.That(map.ProgressLevel, Is.EqualTo(52 + step), "a los " + (step * 150) + " ms");
                Assert.That(LitBuoys(map), Is.EqualTo(2 + step));
                Assert.That(map.CanoeShownMapPoint, Is.Not.EqualTo(previous), "la canoa va con las boyas");
                previous = map.CanoeShownMapPoint;
                Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "a 60 fps mientras dura");
            }

            Assert.That(map.CatchingUp, Is.False, "900 ms: hecho");
            Assert.That(map.CatchUpElapsedMs, Is.LessThanOrEqualTo(MapConfig.CatchUpMaxMs));
            Assert.That(map.CatchUpCompletedByTouch, Is.False);
            Assert.That(map.CanoeShownMapPoint, Is.EqualTo(map.CanoeMapPoint), "la canoa, en su sitio de max_level");
            Assert.That(map.CatchUpArrivals, Is.EqualTo(0), "sin llegadas por el camino");

            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.Services!.MapProgress!.MapSeenLevel, Is.EqualTo(58), "al cerrar, lo visto hasta max_level");
            boot.Lobby.Tick(16f);
            Assert.That(boot.Lobby.MapTabDotShown, Is.False);
            Assert.That(lines.Exists(l => l.Contains("\"event\":\"map_close\"") && l.Contains("\"map_seen_level\":58")), Is.True, string.Join("\n", lines));
            Assert.That(lines.Exists(l => l.Contains("\"event\":\"stop_arrive\"")), Is.False);
        }

        [UnityTest]
        public IEnumerator UnseenArrival_TheCanoeRunsIt_TheMiniCuiaFills_StopArriveInTheMap_AndTheDotGoes()
        {
            var lines = new List<string>();
            using var probe = new ProbeLog(lines);
            yield return MapMarkersTests.OpenApp(53, 45);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            boot.Lobby!.Tick(16f);
            Assert.That(boot.Lobby.MapTabDotShown, Is.True, "el 50 se ganó después de lo visto");
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(45));
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(1050));
            int manaos = map.StopMarkerIndexOf(5);
            Assert.That(map.StopMarkerReached(manaos), Is.False, "Manaos, aún de vidrio");

            map.Tick(749f);
            Assert.That(map.ProgressLevel, Is.EqualTo(49));
            Assert.That(map.CatchUpArrivals, Is.EqualTo(0));
            map.Tick(1f);
            Assert.That(map.ProgressLevel, Is.EqualTo(50));
            Assert.That(map.CatchUpArrivals, Is.EqualTo(1), "la canoa llega a Manaos");
            Assert.That(map.LastCatchUpArrivalLevel, Is.EqualTo(50));
            Assert.That(map.LastCatchUpArrivalSkipped, Is.False, "vista entera");
            Vector2 stop = RiverMapScreen.PlacePoint(boot.RiverMapData!, boot.RiverRoutes!.At(5)!.At);
            Assert.That(Vector2.Distance(map.CanoeShownMapPoint, stop), Is.LessThan(1e-4f), "en la parada");
            Assert.That(map.StopMarkerReached(manaos), Is.True);
            Assert.That(map.StopMarkerFill(manaos), Is.EqualTo(0f), "la mini cuia empieza a llenarse");
            map.Tick(MapConfig.ArrivalSampleFillMs * 0.5f);
            Assert.That(map.StopMarkerFill(manaos), Is.InRange(0.3f, 0.7f));
            map.Tick(MapConfig.ArrivalSampleFillMs);
            Assert.That(map.StopMarkerFill(manaos), Is.EqualTo(1f));
            Assert.That(map.CatchingUp, Is.False);

            string? arrive = lines.Find(l => l.Contains("\"event\":\"stop_arrive\""));
            Assert.That(arrive, Is.Not.Null, string.Join("\n", lines));
            StringAssert.Contains("\"stop_id\":\"s05\"", arrive!);
            StringAssert.Contains("\"level\":50", arrive);
            StringAssert.Contains("\"view\":\"map\"", arrive);
            StringAssert.Contains("\"skipped\":false", arrive);

            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.Services!.MapProgress!.MapSeenLevel, Is.EqualTo(52));
            boot.Lobby.Tick(16f);
            Assert.That(boot.Lobby.MapTabDotShown, Is.False, "el punto desaparece");
        }

        [UnityTest]
        public IEnumerator ATouch_CompletesIt_AndTheArrivalStillToCome_IsSkipped()
        {
            var lines = new List<string>();
            using var probe = new ProbeLog(lines);
            yield return MapMarkersTests.OpenApp(53, 40);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(42), "como mucho las 10 boyas de un tramo");
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(MapConfig.CatchUpMaxMs));
            map.Tick(150f);
            Assert.That(map.ProgressLevel, Is.EqualTo(43));

            // Un toque en el mapa (el área que arrastra) lo completa en el acto.
            map.ViewportRect.GetComponent<PointerDownRelay>().OnPointerDown(null!);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.CatchUpCompletedByTouch, Is.True);
            Assert.That(map.ProgressLevel, Is.EqualTo(52));
            Assert.That(map.CanoeShownMapPoint, Is.EqualTo(map.CanoeMapPoint));
            Assert.That(map.CatchUpArrivals, Is.EqualTo(1));
            Assert.That(map.LastCatchUpArrivalLevel, Is.EqualTo(50));
            Assert.That(map.LastCatchUpArrivalSkipped, Is.True, "la llegada del 50, saltada");
            Assert.That(map.StopMarkerFill(map.StopMarkerIndexOf(5)), Is.EqualTo(1f));
            Assert.That(map.CanoeFlagCount, Is.EqualTo(1));
            string? arrive = lines.Find(l => l.Contains("\"event\":\"stop_arrive\""));
            StringAssert.Contains("\"skipped\":true", arrive!);
            StringAssert.Contains("\"view\":\"map\"", arrive);

            // El primer toque de una mini cuia también solo completa; el siguiente ya la toca.
            yield return MapMarkersTests.OpenApp(53, 45);
            map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.CatchingUp, Is.True);
            map.StopMarkerButton(map.StopMarkerIndexOf(2)).onClick.Invoke();
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.LastTappedStop, Is.EqualTo(-1), "ese toque solo completa");
            map.StopMarkerButton(map.StopMarkerIndexOf(2)).onClick.Invoke();
            Assert.That(map.LastTappedStop, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ClosingHalfWay_CompletesIt_AndMarksTheMapSeen()
        {
            var lines = new List<string>();
            using var probe = new ProbeLog(lines);
            yield return MapMarkersTests.OpenApp(53, 45);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.Tick(300f);
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.LastCatchUpArrivalSkipped, Is.True);
            Assert.That(boot.Services!.MapProgress!.MapSeenLevel, Is.EqualTo(52));
            int arrive = lines.FindIndex(l => l.Contains("\"event\":\"stop_arrive\""));
            int close = lines.FindIndex(l => l.Contains("\"event\":\"map_close\""));
            Assert.That(arrive >= 0 && close > arrive, Is.True, "stop_arrive (saltada) antes de map_close\n" + string.Join("\n", lines));
            StringAssert.Contains("\"map_seen_level\":52", lines[close]);

            // Abrirlo otra vez: nada que ponerse al día.
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.CatchUpBuoys, Is.EqualTo(0));
            Assert.That(map.ProgressLevel, Is.EqualTo(52));
        }

        [UnityTest]
        public IEnumerator ATouchOnTheViewSwitchOrBackToCanoe_CompletesIt()
        {
            yield return MapMarkersTests.OpenApp(53, 45);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.Tick(150f);
            Assert.That(map.CatchingUp, Is.True);
            map.ViewBasinButton.onClick.Invoke(); // SetView(Basin) a los 150 ms
            Assert.That(map.CatchingUp, Is.False, "el toque en Cuenca lo completa");
            Assert.That(map.CatchUpCompletedByTouch, Is.True);
            Assert.That(map.ProgressLevel, Is.EqualTo(52));
            Assert.That(map.View, Is.EqualTo(MapView.Basin), "y cambia de vista");

            yield return MapMarkersTests.OpenApp(53, 45);
            map = GameBootstrap.Current!.RiverMapView!;
            map.Tick(150f);
            Assert.That(map.CatchingUp, Is.True);
            map.BackToCanoe();
            Assert.That(map.CatchingUp, Is.False, "«Volver a la canoa» también");
            Assert.That(map.CanoeShownMapPoint, Is.EqualTo(map.CanoeMapPoint));
        }

        [UnityTest]
        public IEnumerator ReduceMotion_TheFinalStateAtOnce_WithA150msFade()
        {
            yield return MapMarkersTests.OpenApp(53, 53 - 1);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.ProgressReduceMotion = true;
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(53, 52, 45), 53);
            Assert.That(map.CatchingUp, Is.False, "sin viaje ni llenado animado");
            Assert.That(map.ProgressLevel, Is.EqualTo(52));
            Assert.That(map.CanoeShownMapPoint, Is.EqualTo(map.CanoeMapPoint));
            Assert.That(map.CatchUpArrivals, Is.EqualTo(1));
            Assert.That(map.LastCatchUpArrivalSkipped, Is.False);

            // El estado final aparece con un fundido de 150 ms (propuesta del Mapa §4.2): el frente con sus boyas y el agua de Manaos.
            int manaos = map.StopMarkerIndexOf(5);
            Assert.That(map.StopMarkerFill(manaos), Is.EqualTo(1f), "llena, sin llenado animado");
            Assert.That(map.CatchUpFading, Is.True);
            Assert.That(map.CatchUpFades(map.StopMarkerWater(manaos)), Is.True, "el agua de la llegada");
            Assert.That(map.CatchUpFades(map.Buoys!), Is.True, "las boyas del frente");
            Assert.That(map.CatchUpFades(map.StopMarkerWater(map.StopMarkerIndexOf(4))), Is.False, "lo ya visto, no");
            Assert.That(map.CatchUpFades(map.CanoeFlagImage(0)), Is.False, "la bandera de Brasil ya iba en el mástil");
            for (int i = 0; i < map.CatchUpFadeCount; i++)
            {
                Assert.That(map.CatchUpFadeAlpha(i), Is.EqualTo(0f));
            }

            map.Tick(MapConfig.ReducedMotionFadeMs * 0.5f);
            Assert.That(map.CatchUpFadeAlpha(0), Is.EqualTo(0.5f).Within(0.01f), "a mitad");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "a 60 fps mientras dura");
            Image water = map.StopMarkerWater(manaos);
            map.Tick(MapConfig.ReducedMotionFadeMs * 0.5f);
            Assert.That(map.CatchUpFading, Is.False, "150 ms: hecho");
            Assert.That(water.canvasRenderer.GetAlpha(), Is.EqualTo(1f));
            Assert.That(map.Buoys!.canvasRenderer.GetAlpha(), Is.EqualTo(1f));

            // Al pasar del 79 al 80 se iza la de Colombia y se enciende la baliza de Tefé: entran con el fundido.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(81, 80, 79), 81);
            Assert.That(map.CatchUpFading, Is.True);
            Assert.That(map.CanoeFlag(1), Is.EqualTo("CO"));
            Assert.That(map.CatchUpFades(map.CanoeFlagImage(1)), Is.True, "la de Colombia, nueva");
            Assert.That(map.CatchUpFades(map.CanoeFlagImage(0)), Is.False);
            // Cerrar a medias deja todo a la vista.
            map.Close();
            Assert.That(map.CatchUpFading, Is.False);
            Assert.That(map.CanoeFlagImage(1).canvasRenderer.GetAlpha(), Is.EqualTo(1f));
            map.ProgressReduceMotion = false;
        }

        [UnityTest]
        public IEnumerator TheVictoryStripOfThe20_MarksTheMapSeen_The21DoesNot()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = true;
            yield return VictoryArrivalTests.Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            IMapProgress progress = boot.Services!.MapProgress!;
            Assert.That(progress.MaxLevel, Is.EqualTo(20));
            Assert.That(progress.MapSeenLevel, Is.LessThan(20), "antes de la franja");
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.Arrival!.Completed, Is.True);
            Assert.That(view.ArrivalMapSeenCount, Is.EqualTo(1));
            Assert.That(progress.MapSeenLevel, Is.EqualTo(20), "al acabar la franja, lo visto hasta el 20");
            Assert.That(RiverJourney.HasUnseenArrival(progress.MapSeenLevel, progress.MaxLevel), Is.False, "sin punto en la pestaña");

            yield return VictoryArrivalTests.Win(21);
            boot = GameBootstrap.Current!;
            progress = boot.Services!.MapProgress!;
            int before = progress.MapSeenLevel;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(boot.View!.ArrivalMapSeenCount, Is.EqualTo(0), "el 21 no es una llegada");
            Assert.That(progress.MapSeenLevel, Is.EqualTo(before));
        }

        private static int LitBuoys(RiverMapScreen map)
        {
            int lit = 0;
            for (int i = 0; i < map.BuoyCount; i++)
            {
                lit += map.BuoyStateAt(i) == RiverMapScreen.BuoyState.Lit ? 1 : 0;
            }

            return lit;
        }

        // Recoge las líneas AF_STATE del Mapa mientras vive.
        private sealed class ProbeLog : IDisposable
        {
            private readonly List<string> _lines;

            public ProbeLog(List<string> lines)
            {
                _lines = lines;
                Application.logMessageReceived += OnLog;
            }

            public void Dispose()
            {
                Application.logMessageReceived -= OnLog;
            }

            private void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_", StringComparison.Ordinal)
                    || message.StartsWith("AF_STATE {\"event\":\"stop_arrive\"", StringComparison.Ordinal))
                {
                    _lines.Add(message);
                }
            }
        }
    }

    /// <summary>
    /// Capturas de los marcadores y de «ponerse al día» en el Mapa del río (tarea M3.5), con el arte provisional: <c>12b_map_catchup</c>
    /// (seen 45 y max 52, a mitad: la canoa acaba de llegar a Manaos y su mini cuia se llena), <c>12c_map_marcadores_l58</c> (la
    /// canoa con la bandera de Brasil, Rosa delante, las mini cuias de los mundos 1-3, las balizas y la ventanita de casa),
    /// <c>12d_map_trapecio_abanico</c> (max 120: las mini cuias del Trapecio en abanico), <c>12e_map_casa</c> (tras tocar la
    /// ventanita: la casa de TiTino en Guajará), <c>12f_map_l140_banderas</c> (BR, CO y PE en el mástil y la baliza del río
    /// frontera), <c>12g_map_bufeo_l305</c> (el bufeo boliviano en el mundo 7) y <c>12h_map_l140_baliza</c> (max 140, la vista en la
    /// mini cuia del río frontera: su baliza encendida, con el halo ámbar). Cada captura escribe su línea <c>AF_MAP</c>.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MapCatchUpSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de los marcadores y de ponerse al día en el Mapa del río (M3.5); necesitan dispositivo gráfico.")]
    public sealed class MapCatchUpSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureCatchUpAndMarkers()
        {
            string dir = SnapshotKit.Prepare();

            yield return MapMarkersTests.OpenApp(53, 45);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            map.Tick(750f);
            map.Tick(225f); // a mitad: la canoa acaba de dejar Manaos y su mini cuia se llena
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "12b_map_catchup");
            Log("12b_map_catchup", map);

            yield return Shot(dir, 59, "12c_map_marcadores_l58", null);
            yield return Shot(dir, 121, "12d_map_trapecio_abanico", null);
            yield return Shot(dir, 59, "12e_map_casa", m => m.GoHome());
            yield return Shot(dir, 141, "12f_map_l140_banderas", null);
            yield return Shot(dir, 141, "12h_map_l140_baliza", m => m.CenterOnStopMarker(13));

            yield return MapMarkersTests.OpenApp(59);
            boot = GameBootstrap.Current!;
            map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "12g_map_bufeo_l305");
            Log("12g_map_bufeo_l305", map);
        }

        private static IEnumerator Shot(string dir, int playLevel, string name, Action<RiverMapScreen>? before)
        {
            yield return MapMarkersTests.OpenApp(playLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            before?.Invoke(map);
            map.Tick(16f);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Log(name, map);
        }

        private static void Log(string name, RiverMapScreen map)
        {
            int fanned = 0;
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                fanned += map.StopMarkerFanned(i) ? 1 : 0;
            }

            Debug.Log("AF_MAP " + name + ": nivel " + map.ProgressLevel + ", vista " + map.View + ", zoom "
                + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture) + ", banderas " + map.CanoeFlagCount + ", guía " + map.GuideId
                + ", mini cuias " + map.StopMarkerCount + " (" + fanned + " en abanico), casa a la vista " + map.HomeInView
                + ", ventanita «" + map.HomeWindowText + "» " + map.HomeWindowShown);
        }
    }
}
