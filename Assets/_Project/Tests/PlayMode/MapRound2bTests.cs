#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Levels;
using AF.GameFlow;
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
        private static MemoryProgressStore s_progress = null!; // el de WinThe51AndGoHome

        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
            GameBootstrap.L10SequenceOverride = true; // los helpers de PlayMode la apagan si el test no la pide (PlayModeHelpers)
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameBootstrap.L10SequenceOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator TheTucuxi_AfterTheMapOf10FromTheLobby_JoinsInTheWorldView_WithItsCloseUp_AfterTheSequence()
        {
            // El perfil de la prueba: el Mapa sin ver y el 51 por ganar. Se gana, Inicio (tras la Bitácora, si sale) y el inicio abre
            // solo el Mapa del 10 (L10SequenceRule.OnLobby), en la Cuenca.
            yield return WinThe51AndGoHome();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Visible, Is.True, "el inicio abre solo el Mapa del 10");
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
            Assert.That(s_progress.MapSeenLevel, Is.EqualTo(51), "ya visto");

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
        public IEnumerator BackFromBackground_MidJourney_TheCompanionIsAboard_WithoutAnimation()
        {
            // 02 §14 y D-069, M2-4: al volver de segundo plano el viaje salta al final y quien se unía va a bordo, sin animar.
            yield return MapMarkersTests.OpenApp(52, 40); // el tucuxi pendiente, con el viaje desde el 41
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi));
            Assert.That(map.CatchingUp, Is.True);
            Level10MapTests.Tick(map, 300);
            Assert.That(map.CrewIntroFired, Is.False, "a mitad del viaje, aún no");
            Assert.That(map.ResumeFromBackground(), Is.True);
            bool chip = false;
            bool arriving = false;
            float top = 1f;
            for (int ms = 0; ms < 3000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                chip |= map.CrewChipShown;
                arriving |= map.CrewArriving;
                top = Mathf.Max(top, map.CameraCloseUp);
            }

            Assert.That(map.CrewIntroFired, Is.True);
            Assert.That(chip, Is.False, "sin chip");
            Assert.That(arriving, Is.False, "sin llegar volando ni nadando");
            Assert.That(top, Is.EqualTo(1f), "sin primer plano");
            Assert.That(map.TucuxiAboardShown, Is.True, "a bordo");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator DuringTheCloseUps_NoLabelOrPlateShows()
        {
            // Revisión de la r2b: en el primer plano de la tripulación y en el acercamiento de llegada del viaje del 80, los rótulos y
            // las placas no se ven desde el primer fotograma (antes crecían con la cámara mientras se desvanecían).
            yield return MapMarkersTests.OpenApp(52, 50);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.VisibleMapLabelCount(), Is.GreaterThan(0), "fuera del primer plano hay rótulos");
            // Observación 2 de la r2d: a la vuelta, vuelven al bajar de CloseUpLabelsBackBelow (×1,15), no al llegar a ×1.
            int closeFrames = 0;
            float prev = 1f;
            for (int ms = 0; ms < 4000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                bool rising = map.CameraCloseUp >= prev;
                prev = map.CameraCloseUp;
                if (map.CameraCloseUp > RiverMapScreen.CloseUpLabelsBackBelow || (rising && map.CameraCloseUp > 1.001f))
                {
                    closeFrames++;
                    Assert.That(map.VisibleMapLabelCount(), Is.EqualTo(0), "a los " + ms + " ms del primer plano del tucuxi (×" + map.CameraCloseUp + ")");
                }
            }

            Assert.That(closeFrames, Is.GreaterThan(0), "hubo primer plano");
            Level10MapTests.Tick(map, 1000);
            Assert.That(map.CameraCloseUp, Is.EqualTo(1f));
            Assert.That(map.VisibleMapLabelCount(), Is.GreaterThan(0), "y vuelven");

            MapRound2Tests.Open(boot, map, 81, 80, 70); // el viaje del 80, con su acercamiento de llegada
            int arrivalFrames = 0;
            prev = 1f;
            for (int ms = 0; ms < 9000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                bool rising = map.CameraCloseUp >= prev;
                prev = map.CameraCloseUp;
                if (map.CameraCloseUp > RiverMapScreen.CloseUpLabelsBackBelow || (rising && map.CameraCloseUp > 1.001f))
                {
                    arrivalFrames++;
                    Assert.That(map.VisibleMapLabelCount(), Is.EqualTo(0), "a los " + ms + " ms del acercamiento de llegada (×" + map.CameraCloseUp + ")");
                }
            }

            Assert.That(arrivalFrames, Is.GreaterThan(0), "hubo acercamiento de llegada");
            Assert.That(map.VisibleMapLabelCount(), Is.GreaterThan(0), "y vuelven");
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
            yield return WinThe51AndGoHome();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Visible && map.Source == MapOpenSource.L10, Is.True, "el inicio abre solo el Mapa del 10");
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi));
            Level10MapTests.Tick(map, 500);
            Assert.That(boot.CloseRiverMap(), Is.True, "Atrás a mitad de la secuencia, sin verlo");
            Assert.That(map.CrewSeenCap, Is.EqualTo(RiverCrew.TucuxiJoinLevel - 1));
            Assert.That(s_progress.MapSeenLevel, Is.EqualTo(50), "lo visto, hasta antes de su punto (la llegada a Manaos sí)");

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi), "se presenta en la apertura siguiente");
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(50), "con el viaje de su último tramo");
            Level10MapTests.Tick(map, 3000);
            Assert.That(map.CrewIntroFired, Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(s_progress.MapSeenLevel, Is.EqualTo(51));

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
                        foreach (Rect box in RealLabelBoxes(map))
                        {
                            Assert.That(MapRound2Tests.Hits(box, p, r), Is.False, "max " + max + ": la boya del " + map.BuoyLevel(i) + " sobre un rótulo o una placa");
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
                RectTransform? prize = map.PrizePill;
                if (prize != null && prize.gameObject.activeSelf)
                {
                    Assert.That(prize.parent.gameObject.activeSelf, Is.True, "max " + max + ": la píldora del Premio del mundo, con su mini cuia");
                }

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

        // El 51 por ganar con el Mapa sin ver (el perfil de la prueba, como el pm clear con af_max_level 50): se gana, se cierra la
        // Bitácora si sale y se pulsa Inicio; la Bitácora del 10, si sale, se cierra y el inicio abre el Mapa del 10.
        private static IEnumerator WinThe51AndGoHome()
        {
            if (!GameScreen.SimulatedScreen.HasValue)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            }

            s_progress = new MemoryProgressStore(51, 50, 0);
            yield return PlayModeHelpers.LoadGame(51, s_progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.App!.L10MapAvailable, Is.True, "la escena trae el Mapa y la secuencia del 10");
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.RiverMapView!.Visible, Is.False, "la victoria del 51 no lleva «Ver mapa»");
            CloseLogbook(boot);
            Assert.That(boot.PressHome(), Is.True);
            CloseLogbook(boot); // B4-3: Victoria → libreta → Mapa del 10
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Debug.Log("AF_MAP r2b inicio: Mapa " + boot.RiverMapView.Visible + " (" + boot.RiverMapView.Source + "), L10MapDue " + boot.App.L10MapDue);
        }

        private static void CloseLogbook(GameBootstrap boot)
        {
            if (boot.LogbookScreen != null && boot.LogbookScreen.Visible)
            {
                boot.LogbookScreen.CloseButton.onClick.Invoke();
            }
        }

        /// <summary>Las cajas de verdad (locales del contenido) de los rótulos de parada y de pueblo y de las placas que se ven.</summary>
        internal static List<Rect> RealLabelBoxes(RiverMapScreen map)
        {
            var boxes = new List<Rect>();
            GameBootstrap boot = GameBootstrap.Current!;
            for (int n = 0; n < boot.RiverStops!.Count; n++)
            {
                Rect r = map.StopLabelLocalRect(n);
                if (r.width > 0f)
                {
                    boxes.Add(r);
                }
            }

            for (int i = 0; i < map.TownMarkerCount; i++)
            {
                Rect r = map.TownLabelLocalRect(i);
                if (r.width > 0f)
                {
                    boxes.Add(r);
                }
            }

            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                if (map.SilhouettePlate(i).enabled)
                {
                    boxes.Add(map.SilhouetteLocalBox(i));
                }
            }

            return boxes;
        }
    }
}
