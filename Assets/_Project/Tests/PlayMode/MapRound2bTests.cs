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
    /// Los arreglos del Mapa de la prueba r2b de la 1.1 (<c>docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2b.md</c>), en la
    /// escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>observación 1: el compañero que se une se ve la primera vez que se abre el Mapa tras ganarlo, sea cual sea la ruta (el
    /// Mapa del 10 que sale solo en el inicio, en la Cuenca; la pestaña del inicio tras la victoria; la pausa) y la vista; si se cierra
    /// antes de verlo en el Mapa del 10, lo visto se anota hasta antes de su punto y la apertura siguiente lo presenta;</item>
    /// <item>observación 3: las boyas no se pintan sobre los rótulos ni las placas;</item>
    /// <item>observación 4: la píldora del Premio del mundo no queda bajo la ventanita de casa en el viaje del 80;</item>
    /// <item>observaciones 5 y 6: con el Mapa v2, solo la placa del mundo siguiente, como mucho 3 banderas en el mástil y sin las
    /// paradas futuras apartadas por el abanico.</item>
    /// </list>
    /// </summary>
    public sealed class MapRound2bTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 16, 12, 0, 0, System.DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator TheTucuxi_AfterTheMapOf10FromTheLobby_JoinsInTheWorldView_WithItsCloseUp_AfterTheSequence()
        {
            // El perfil de la prueba: el 51 ganado y el Mapa sin ver. Al entrar al inicio sale solo el Mapa del 10 (tras la Bitácora,
            // si toca), en la Cuenca.
            yield return LoadApp(52, 51, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Debug.Log("AF_MAP r2b inicio: pantalla " + boot.App!.Screen + ", Mapa " + map.Visible + ", Bitácora " + (boot.LogbookScreen != null && boot.LogbookScreen.Visible)
                + ", L10MapDue " + boot.App.L10MapDue);
            if (!map.Visible && boot.LogbookScreen != null && boot.LogbookScreen.Visible)
            {
                // La Bitácora del 10 sale antes (D-033, punto 11): el Mapa del 10, al cerrarla.
                boot.LogbookScreen.CloseButton.onClick.Invoke();
            }

            if (!map.Visible)
            {
                Assert.That(boot.OpenRiverMap(MapOpenSource.L10), Is.True, "el Mapa del 10, como lo abre el inicio");
            }

            Assert.That(map.Visible, Is.True);
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.L10));
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi));

            bool chipInSequence = false;
            int ms = 0;
            for (; ms < 15000 && !map.L10SequenceDone; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                chipInSequence |= map.CrewChipShown || map.CrewIntroFired;
            }

            Assert.That(map.L10SequenceDone, Is.True);
            Assert.That(chipInSequence, Is.False, "en la secuencia del 10 (la Cuenca del estuario) espera");

            float top = 1f;
            bool chip = false;
            for (int t = 0; t < 4000; t += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                chip |= map.CrewChipShown;
                top = Mathf.Max(top, map.CameraCloseUp);
            }

            Assert.That(map.CrewIntroFired, Is.True);
            Assert.That(map.View, Is.EqualTo(MapView.World), "pasa a la vista Mundo para el primer plano");
            Assert.That(chip, Is.True, "el chip «¡Se une al viaje! · Tucuxi»");
            Assert.That(map.CrewChipText, Is.EqualTo("¡Se une al viaje! · Tucuxi"));
            Assert.That(top, Is.GreaterThan(RiverMapScreen.CamCloseUpZoom * 0.9f), "con su primer plano");
            Assert.That(map.TucuxiAboardShown, Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(51), "ya visto");

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.CrewIntroKind, Is.Null, "no se repite");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator TheTucuxi_FromTheLobbyAfterTheVictory_AndFromThePause_JoinsWithItsCloseUp()
        {
            // Victoria del 51 (con su Bitácora) e Inicio › Mapa: lo visto es el 50, de la franja de llegada de Manaos.
            yield return MapMarkersTests.OpenApp(52, 50);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.View, Is.EqualTo(MapView.World));
            AssertJoinsWithCloseUp(map, "desde el inicio");
            map.Close();

            // Desde la pausa del 52 (y pausa › Salir lleva al inicio: el caso de arriba).
            map.Open(MapOpenSource.Pause, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(52, 51, 50), 52, true);
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi));
            AssertJoinsWithCloseUp(map, "desde la pausa");
            map.Close();
        }

        [UnityTest]
        public IEnumerator InTheBasinChosenByThePlayer_TheCompanionJoinsInThatView_WithItsChip()
        {
            yield return MapMarkersTests.OpenApp(22, 20); // el tucán
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.SetView(MapView.Basin);
            bool chip = false;
            for (int ms = 0; ms < 3000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                chip |= map.CrewChipShown;
            }

            Assert.That(map.CrewIntroFired, Is.True);
            Assert.That(chip, Is.True, "sale en la vista actual");
            Assert.That(map.View, Is.EqualTo(MapView.Basin), "la vista que eligió el jugador no cambia");
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(21));
        }

        [UnityTest]
        public IEnumerator TheMapOf10ClosedInItsSequence_TheNextOpeningPresentsTheCompanion()
        {
            yield return LoadApp(52, 51, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            if (!map.Visible)
            {
                Assert.That(boot.OpenRiverMap(MapOpenSource.L10), Is.True);
            }

            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi));
            Level10MapTests.Tick(map, 500);
            Assert.That(boot.CloseRiverMap(), Is.True, "Atrás a mitad de la secuencia, sin verlo");
            Assert.That(map.CrewSeenCap, Is.EqualTo(RiverCrew.TucuxiJoinLevel - 1));
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(50), "lo visto, hasta antes de su punto (la llegada a Manaos sí)");

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi), "se presenta en la apertura siguiente");
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(50), "con el viaje de su último tramo");
            Level10MapTests.Tick(map, 3000);
            Assert.That(map.CrewIntroFired, Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.Profile.Progress.MapSeenLevel, Is.EqualTo(51));

            // En las demás aperturas, cerrar a medias anota lo visto hasta max_level (M3.5).
            Assert.That(map.CrewSeenCap, Is.EqualTo(int.MaxValue));
        }

        [UnityTest]
        public IEnumerator TheBuoys_AreNeverDrawnOverALabelOrAPlate()
        {
            yield return MapMarkersTests.OpenApp(72);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            int cleared = 0;
            foreach (int max in new[] { 71, 79, 64, 132, 291, 451 })
            {
                Reopen(boot, map, max);
                map.Tick(PlayModeHelpers.FrameMs);
                Canvas.ForceUpdateCanvases();
                map.Tick(PlayModeHelpers.FrameMs);
                foreach (RiverMapScreen.BuoyGraphic g in new[] { map.Buoys!, map.BuoysAhead! })
                {
                    float r = g.DrawnRadius;
                    for (int i = 0; i < map.BuoyCount; i++)
                    {
                        RiverMapScreen.BuoyState s = map.BuoyStateAt(i);
                        if (s == RiverMapScreen.BuoyState.Skipped || (s == RiverMapScreen.BuoyState.Unlit) != g.Ahead || g.IsHidden(i))
                        {
                            continue;
                        }

                        Vector2 p = map.BuoyLocalPoint(i);
                        for (int k = 0; k < map.BuoyLabelClearCount; k++)
                        {
                            Rect box = map.BuoyLabelClear(k);
                            Assert.That(MapRound2Tests.Hits(box, p, r), Is.False, "max " + max + ": la boya del " + map.BuoyLevel(i) + " sobre un rótulo");
                        }
                    }
                }

                cleared += map.BuoyLabelClearCount;
            }

            Assert.That(cleared, Is.GreaterThan(0), "hay rótulos que despejar");
            map.Close();
        }

        [UnityTest]
        public IEnumerator InTheJourneyOf80_ThePrizePill_StaysClearOfTheHomeWindow()
        {
            yield return MapMarkersTests.OpenApp(81);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            MapRound2Tests.Open(boot, map, 81, 80, 70);
            int checkedFrames = 0;
            for (int ms = 0; ms < 8000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                RectTransform? pill = map.PrizePill;
                HomeWindow? home = map.MapHomeWindow;
                if (pill == null || !pill.gameObject.activeInHierarchy || home == null || !home.Visible || map.CameraZoom != 1f)
                {
                    continue;
                }

                Canvas.ForceUpdateCanvases();
                Rect p = StopCardTests.CanvasRect(pill, map.Canvas);
                Assert.That(p.Overlaps(StopCardTests.CanvasRect(home.Root, map.Canvas)), Is.False, "a los " + ms + " ms, bajo el retrato de casa");
                Assert.That(p.Overlaps(StopCardTests.CanvasRect(home.PillRect, map.Canvas)), Is.False, "a los " + ms + " ms, bajo «Casa · a unos …»");
                checkedFrames++;
            }

            Debug.Log("AF_MAP r2b píldora y ventanita: " + checkedFrames + " fotogramas con las dos");
            map.Close();
        }

        [UnityTest]
        public IEnumerator WithTheMapV2_OnlyTheNextWorldPlate_AtMost3Flags_AndNoFannedFutureStops()
        {
            yield return MapMarkersTests.OpenApp(133);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 51, 132, 140, 291, 451, 481 })
            {
                Reopen(boot, map, max);
                foreach (MapView view in new[] { MapView.World, MapView.Basin })
                {
                    map.SetView(view);
                    Assert.That(map.SilhouetteCount, Is.LessThanOrEqualTo(RiverMapScreen.LiveFutureWorldPlates), "max " + max + " en " + view + ": solo la del mundo siguiente");
                }

                map.SetView(MapView.World);
                Assert.That(map.CanoeFlagCount, Is.LessThanOrEqualTo(RiverMapScreen.LiveMaxCanoeFlags), "max " + max);
                for (int i = 0; i < map.StopMarkerCount; i++)
                {
                    if (map.StopMarkerBox(i).width > 0f && map.StopMarkerHasLeader(i))
                    {
                        Assert.That(map.StopMarkerIsBuoyAt(i), Is.False, "max " + max + ": una parada futura apartada por el abanico");
                    }
                }
            }

            map.Close();

            // Con la 1.0 (ui_map_v2 apagada), como en M3.4: una placa por mundo futuro y todas las banderas.
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            yield return MapMarkersTests.OpenApp(292);
            boot = GameBootstrap.Current!;
            map = boot.RiverMapView!;
            Assert.That(map.LiveActive, Is.False);
            Assert.That(map.SilhouetteCount, Is.GreaterThan(1));
            Assert.That(map.CanoeFlagCount, Is.GreaterThan(RiverMapScreen.LiveMaxCanoeFlags));
            map.Close();
        }

        [UnityTest]
        public IEnumerator InBolivia_TheNextStopLabel_GoesNextToItsStop()
        {
            yield return MapMarkersTests.OpenApp(442);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 441, 445, 451, 455, 461, 471, 481, 491, 501, 511 })
            {
                Reopen(boot, map, max);
                map.Tick(PlayModeHelpers.FrameMs);
                int n = map.RemainingStopN();
                int i = map.StopMarkerIndexOf(n);
                Rect label = map.StopLabelLocalRect(n);
                Rect cuia = map.StopMarkerBox(i);
                float gap = label.width > 0f && cuia.width > 0f ? Gap(label, cuia) : -1f;
                Debug.Log("AF_MAP r2b Bolivia " + max + ": parada " + n + ", zoom ×" + map.Zoom.ToString("0.00") + ", alejado " + map.RemainingZoomedOut
                    + ", bajo el tope " + map.RemainingNeedsBelowFloor + ", placa de respaldo " + map.RemainingLabelOffView + ", hueco rótulo-parada " + gap.ToString("0.0") + " dp");
                Assert.That(gap >= 0f || map.RemainingChipViewRect.width > 0f, Is.True, "max " + max + ": «Faltan N» en el rótulo junto a su parada o en la placa");
                if (gap >= 0f)
                {
                    Assert.That(gap, Is.LessThanOrEqualTo(RiverMapScreen.StopLabelNearDp), "max " + max + ": el rótulo de " + n + " lejos de su parada");
                }
            }

            map.Close();
        }

        // Vuelve a abrir el Mapa desde el inicio con max niveles ganados y vistos, con el nivel siguiente publicado (hasta el 700).
        private static void Reopen(GameBootstrap boot, RiverMapScreen map, int max)
        {
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(max + 1, max, max), max + 1, true);
            Canvas.ForceUpdateCanvases();
        }

        // Hueco entre dos cajas (0 si se tocan).
        private static float Gap(Rect a, Rect b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        private static void AssertJoinsWithCloseUp(RiverMapScreen map, string at)
        {
            float top = 1f;
            bool chip = false;
            for (int ms = 0; ms < 4000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                chip |= map.CrewChipShown;
                top = Mathf.Max(top, map.CameraCloseUp);
            }

            Assert.That(chip, Is.True, at + ": el chip");
            Assert.That(top, Is.GreaterThan(RiverMapScreen.CamCloseUpZoom * 0.9f), at + ": el primer plano");
            Assert.That(map.TucuxiAboardShown, Is.True, at);
        }

        private static IEnumerator LoadApp(int play, int max, int seen)
        {
            if (!GameScreen.SimulatedScreen.HasValue)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            }

            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = play;
            profile.Progress.MaxLevel = max;
            profile.Progress.MapSeenLevel = seen;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            yield return null;
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }
    }
}
