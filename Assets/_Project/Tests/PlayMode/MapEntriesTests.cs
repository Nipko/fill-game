#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las entradas del Mapa del río desde la pausa y desde la victoria, y el modo <c>ui_map_mode</c> = <c>worlds</c>, en la escena
    /// <c>Game</c> real (tarea M4.3; D-013, punto 7; 02 §5 y §6; 04 §5.2; 08 §8.2 y §9):
    /// <list type="bullet">
    /// <item>desde el 31 con 3 canoas: pausa › «Mapa del río» abre el Mapa encima del nivel con «Volver al nivel», que vuelve al
    /// nivel tal cual: las canoas, los fallos (<c>fails_level</c> y <c>stats.fails_hard</c>), las victorias, la fase y el nivel en
    /// curso guardado no cambian (la Crecida aún no tiene estado en el cliente: FailResolve); el Atrás de Android también lo
    /// cierra; antes del 10, la pausa no lo ofrece;</item>
    /// <item>«Ver mapa» en la victoria del 20 (con la llegada): entra con «Inicio», en su fila, completa la franja (una sola
    /// <c>stop_arrive</c>: el Mapa no la repite), abre el Mapa encima de la victoria con «Volver» y vuelve a ella; «Siguiente»
    /// se habilita y entra en los mismos milisegundos que sin abrir el Mapa, y AD_GATE lleva al 21; en un AD_GATE que dura
    /// fotogramas (un anuncio retenido), «Ver mapa» queda inactivo y no abre el Mapa, y al cerrarse el anuncio se carga el 21
    /// como sin él; sin llegada (el 21), no está;</item>
    /// <item>las sondas <c>map_open</c> con <c>source</c> <c>pause</c> y <c>victory</c>;</item>
    /// <item>con <c>worlds</c>, solo las grandes paradas (la primera de cada mundo: una por mundo) y sin boyas.</item>
    /// </list>
    /// </summary>
    public sealed class MapEntriesTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = true; // la llegada sola en la ranura (como VictoryArrivalTests)
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Pause_From31_With3Canoes_MapOverTheLevel_BackToTheLevel_WithoutCost()
        {
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_open\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            LaunchOptions.LevelOffsetOverride = 25; // el 31 se juega con un tablero corto del tutorial (como FailFunnelTests)
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500, 3));
            var progress = new MemoryProgressStore(31, 30);
            yield return PlayModeHelpers.LoadGame(31, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.QuitIsHardFail, Is.True, "desde el 31, salir con el menú es fallo duro (D-008)");
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(3));
            Snapshot before = Snapshot.Of(boot, progress);

            Application.logMessageReceived += OnLog;
            try
            {
                view.PauseButton.onClick.Invoke();
                Assert.That(view.Pause.Visible, Is.True);
                Assert.That(view.Pause.MapButton.gameObject.activeSelf, Is.True, "02 §5: la pausa abre el Mapa del río");
                Assert.That(view.Pause.MapButton.GetComponentInChildren<TextMeshProUGUI>(true).text, Is.EqualTo("Mapa del río"));
                Assert.That(UiText.RiverMap, Is.EqualTo("Mapa del río"), "river_map (glosario §7.1)");
                AssertPauseOrder(view);

                view.Pause.MapButton.onClick.Invoke();
                Assert.That(map.Visible, Is.True, "encima del nivel en pausa");
                Assert.That(map.Source, Is.EqualTo(MapOpenSource.Pause));
                Assert.That(map.PlayLabel, Is.EqualTo("Volver al nivel"), "map_back_to_level (P-M2)");
                Assert.That(map.PlayLabel, Is.EqualTo(UiText.MapBackToLevel));
                Assert.That(view.Pause.Visible, Is.False);
                Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "no es salir del nivel (04 §5.2)");
                before.AssertSame(boot, progress, "con el Mapa abierto");

                // El nivel sigue debajo, quieto: un rato con el Mapa abierto no cambia nada.
                map.Tick(500f);
                PlayModeHelpers.Step(boot, 500);
                before.AssertSame(boot, progress, "con el Mapa abierto un rato");
                Assert.That(boot.OpenRiverMap(MapOpenSource.Pause), Is.False, "ya está abierto");

                // «Volver al nivel»: el nivel tal cual, sin la pausa.
                map.PlayButton.onClick.Invoke();
                Assert.That(map.Visible, Is.False);
                Assert.That(view.Pause.Visible, Is.False, "vuelve al nivel, no a la pausa");
                Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
                before.AssertSame(boot, progress, "tras «Volver al nivel»");
                Assert.That(flow.AcceptsBoardInput, Is.True, "y se sigue jugando");

                // El Atrás de Android también lo cierra y deja el nivel.
                view.PauseButton.onClick.Invoke();
                view.Pause.MapButton.onClick.Invoke();
                Assert.That(map.Visible, Is.True);
                Assert.That(boot.PressAndroidBack(), Is.True);
                Assert.That(map.Visible, Is.False);
                Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
                before.AssertSame(boot, progress, "tras el Atrás de Android");
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            if (!DevProbe.Enabled)
            {
                Assert.That(lines, Is.Empty);
                yield break;
            }

            Assert.That(lines.Count, Is.EqualTo(2), string.Join("\n", lines));
            StringAssert.Contains("\"source\":\"pause\"", lines[0]);
            StringAssert.Contains("\"level\":31", lines[0]);
            StringAssert.Contains("\"source\":\"pause\"", lines[1]);
        }

        [UnityTest]
        public IEnumerator Pause_Before10_DoesNotOfferTheMap()
        {
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            Assert.That(view.PauseMapOffered, Is.False, "oculto hasta el 10, como la pestaña del inicio (D-008)");
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True);
            Assert.That(view.Pause.MapButton.gameObject.activeSelf, Is.False);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Pause), Is.False);
            Assert.That(boot.RiverMapView!.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator VerMapa_InTheVictoryOf20_GoesAndComesBack_NextKeepsItsTime_AndAdGateGoesOn()
        {
            // Sin abrir el Mapa: cuándo se habilita, empieza a verse y se ve entero «Siguiente».
            var without = new int[3];
            yield return MeasureNext(20, without, -1, -1);

            // Con «Ver mapa» a los 650 ms (ya a la vista) y «Volver» a los 700 ms.
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_open\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            var with = new int[3];
            Application.logMessageReceived += OnLog;
            try
            {
                yield return MeasureNext(20, with, 650, 700);
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            Assert.That(with, Is.EqualTo(without), "Siguiente con el mismo tiempo (habilitado, empieza a verse, entero)");
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.VictoryMapOpened, Is.EqualTo(1));
            Assert.That(view.StopArriveCount, Is.EqualTo(1), "una sola llegada: la de la franja, completada por «Ver mapa»");
            Assert.That(boot.RiverMapView!.CatchUpArrivals, Is.EqualTo(0), "el Mapa no la repite");

            // AD_GATE y el 21, como siempre.
            Assert.That(boot.PressNext(), Is.True);
            for (int elapsed = 0; elapsed < 10000 && boot.Flow!.LevelNumber == 20; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(21));
            Assert.That(view.VictoryMapShown, Is.False, "el nivel nuevo, sin «Ver mapa»");

            if (DevProbe.Enabled)
            {
                Assert.That(lines.Count, Is.EqualTo(1), string.Join("\n", lines));
                StringAssert.Contains("\"source\":\"victory\"", lines[0]);
                StringAssert.Contains("\"level\":21", lines[0]);
            }
            else
            {
                Assert.That(lines, Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator VerMapa_SharesTheHomeRow_EntersWithHome_AndTheAndroidBackReturnsToTheVictory()
        {
            yield return VictoryArrivalTests.Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(view.Arrival!.Stop, Is.Not.Null, "la llegada a Alter do Chão");
            Assert.That(view.VictoryMapShown, Is.True);
            Assert.That(view.VictoryMapButton.gameObject.activeSelf, Is.True);
            Assert.That(view.VictoryMapButton.GetComponentInChildren<TextMeshProUGUI>(true).text, Is.EqualTo("Ver mapa"), "map_open (P-M2)");
            Assert.That(view.VictoryMapAlpha, Is.EqualTo(0f), "entra con «Inicio», no antes");
            PlayModeHelpers.Step(boot, 900);
            Assert.That(view.VictoryMapAlpha, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(view.VictoryMapAlpha, Is.EqualTo(view.NextAlpha).Within(1e-4f), "a la vez que Siguiente e Inicio");

            // En la fila de «Inicio»: dos botones del mismo ancho, sin solaparse, que juntos ocupan el ancho de «Siguiente».
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect next = VictorySetup.CanvasRect((RectTransform)view.NextButton.transform, root);
            Rect home = VictorySetup.CanvasRect((RectTransform)view.HomeButton.transform, root);
            Rect link = VictorySetup.CanvasRect((RectTransform)view.VictoryMapButton.transform, root);
            Assert.That(link.yMin, Is.EqualTo(home.yMin).Within(0.5f));
            Assert.That(link.height, Is.EqualTo(home.height).Within(0.5f));
            Assert.That(link.width, Is.EqualTo(home.width).Within(0.5f));
            Assert.That(home.xMax, Is.LessThanOrEqualTo(link.xMin + 0.5f), "Inicio a la izquierda");
            Assert.That(home.xMin, Is.EqualTo(next.xMin).Within(0.5f));
            Assert.That(link.xMax, Is.EqualTo(next.xMax).Within(0.5f));
            Assert.That(link.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f), "07 §7.8: ≥ 48 dp");
            Assert.That(home.yMin - next.yMin, Is.EqualTo(68f).Within(0.5f), "Inicio sigue a 68 dp de Siguiente");

            // «Ver mapa» y el Atrás de Android: de vuelta a la victoria, tal cual.
            view.VictoryMapButton.onClick.Invoke();
            Assert.That(map.Visible, Is.True);
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.Victory));
            Assert.That(map.PlayLabel, Is.EqualTo(UiText.Back), "«Volver» [P]");
            Assert.That(view.Arrival.Completed, Is.True, "la franja, completada");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue ahí");
            Assert.That(view.NextButton.interactable, Is.True);
            Assert.That(view.StopArriveCount, Is.EqualTo(1));

            // En AD_GATE ya no responde. La victoria del 20 no trae intersticial (empiezan en el 22) y la puerta del juego cierra
            // en la misma llamada sin anuncio: el test pone una que retiene el anuncio hasta cerrarlo, para que AD_GATE dure
            // fotogramas (como Level11_AnAdGateLeftOpen_CancelsTheWait_AndLoadsTheNextLevelOnce). Ya contó la victoria (RecordWin)
            // con la puerta del juego.
            var gate = new HeldAdGate();
            boot.Flow.AdGate = gate;
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.AdGate));
            Assert.That(gate.Runs, Is.EqualTo(1));
            Assert.That(gate.WonLevel, Is.EqualTo(20));
            PlayModeHelpers.Step(boot, 3 * PlayModeHelpers.FrameMs); // AD_GATE dura fotogramas
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.AdGate), "el anuncio sigue abierto");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(20));
            Assert.That(view.VictoryMapShown, Is.True, "sigue en su sitio, inactivo, como «Inicio»");
            Assert.That(view.VictoryMapButton.interactable, Is.False);
            Assert.That(view.HomeButton.interactable, Is.False);
            Assert.That(view.NextButton.interactable, Is.False);
            view.VictoryMapButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False, "en AD_GATE, «Ver mapa» no abre el Mapa");
            Assert.That(view.VictoryMapOpened, Is.EqualTo(1), "solo la vez de antes");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Victory), Is.False, "ni por la entrada de la victoria");
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.AdGate));
            Assert.That(gate.Runs, Is.EqualTo(1));

            // Al cerrar el anuncio, el 21 como sin el Mapa: una sola carga, sin «Ver mapa» y con el Mapa cerrado.
            gate.Close();
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(21));
            Assert.That(VictorySetup.InNewLevel(boot.Flow.Phase), Is.True, "en su INTRO, LEVEL_CARD o PLAYING: " + boot.Flow.Phase);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            LevelSession loaded = boot.Flow.Session;
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(21), "sin llegar al 22");
            Assert.That(boot.Flow.Session, Is.SameAs(loaded), "el 21 no se vuelve a cargar");
            Assert.That(view.VictoryMapShown, Is.False, "el nivel nuevo, sin «Ver mapa»");
            Assert.That(view.VictoryMapButton.gameObject.activeSelf, Is.False);
            Assert.That(map.Visible, Is.False);
            Assert.That(gate.Runs, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator VerMapa_IsNotInAVictoryWithoutArrival_AndHomeStaysCentered()
        {
            yield return VictoryArrivalTests.Win(21);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.VictoryMapShown, Is.False);
            Assert.That(view.VictoryMapButton.gameObject.activeSelf, Is.False);
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect next = VictorySetup.CanvasRect((RectTransform)view.NextButton.transform, root);
            Rect home = VictorySetup.CanvasRect((RectTransform)view.HomeButton.transform, root);
            Assert.That(home.center.x, Is.EqualTo(next.center.x).Within(0.5f), "Inicio centrado, como siempre");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Pause), Is.False, "la pausa, solo con el nivel en juego");
        }

        [UnityTest]
        public IEnumerator WorldsMode_OnlyTheGreatStops_OnePerWorld_WithoutBuoys()
        {
            yield return PlayModeHelpers.LoadGame(141, new MemoryProgressStore(141, 140));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            RiverMapScreen map = boot.RiverMapView!;
            RiverStopCatalog stops = boot.RiverStops!;
            Assert.That(MapConfig.UiMapMode, Is.EqualTo(MapConfig.MapModeStops), "08 §9: stops por defecto");

            // El filtro sobre el catálogo: con worlds, una parada por mundo (la gran parada); con stops, todas.
            var worlds = new HashSet<int>();
            int gates = 0;
            for (int k = 0; k < stops.Count; k++)
            {
                RiverStop stop = stops.At(k)!;
                worlds.Add(stop.World);
                gates += stop.Kind == StopKind.Gate ? 1 : 0;
            }

            Assert.That(RiverMapScreen.VisibleStopCount(stops, MapConfig.MapModeStops), Is.EqualTo(stops.Count));
            Assert.That(RiverMapScreen.VisibleStopCount(stops, MapConfig.MapModeWorlds), Is.EqualTo(worlds.Count), "una por mundo");
            Assert.That(RiverMapScreen.VisibleStopCount(stops, MapConfig.MapModeWorlds), Is.EqualTo(gates));
            Assert.That(worlds.Count, Is.EqualTo(9), "el catálogo de 1-700: 9 mundos (13 con el de 1-1.000)");

            // En la pantalla: abierto desde la pausa del 141, con stops y con worlds.
            boot.View!.PauseButton.onClick.Invoke();
            boot.View.Pause.MapButton.onClick.Invoke();
            Assert.That(map.Visible, Is.True);
            Assert.That(map.WorldsMode, Is.False);
            int all = map.StopMarkerCount;
            int buoys = map.BuoyCount;
            Assert.That(buoys, Is.GreaterThan(0));
            map.PlayButton.onClick.Invoke();

            map.ProgressMapMode = MapConfig.MapModeWorlds;
            try
            {
                boot.View.PauseButton.onClick.Invoke();
                boot.View.Pause.MapButton.onClick.Invoke();
                Assert.That(map.Visible, Is.True);
                Assert.That(map.WorldsMode, Is.True);
                Assert.That(map.BuoyCount, Is.EqualTo(0), "sin boyas intermedias");
                Assert.That(map.StopMarkerCount, Is.LessThan(all));
                int expected = 0;
                for (int k = 0; k < stops.Count; k++)
                {
                    RiverStop stop = stops.At(k)!;
                    if (stop.Kind == StopKind.Gate && stop.World <= map.World)
                    {
                        expected++;
                    }
                }

                Assert.That(map.StopMarkerCount, Is.EqualTo(expected), "las grandes paradas de los mundos abiertos");
                Assert.That(map.StopMarkerCount, Is.EqualTo(map.World), "una por mundo, hasta el de «Jugar»");
                for (int i = 0; i < map.StopMarkerCount; i++)
                {
                    Assert.That(stops.At(map.StopMarkerStop(i))!.Kind, Is.EqualTo(StopKind.Gate), "parada " + map.StopMarkerStop(i));
                }

                map.PlayButton.onClick.Invoke();
            }
            finally
            {
                map.ProgressMapMode = MapConfig.UiMapMode;
            }
        }

        // Gana el nivel y mide, de milisegundo en milisegundo, cuándo «Siguiente» se habilita, empieza a verse y se ve entero; con
        // openAt ≥ 0, toca «Ver mapa» en ese milisegundo y «Volver» en closeAt.
        private static IEnumerator MeasureNext(int level, int[] times, int openAt, int closeAt)
        {
            yield return VictoryArrivalTests.Win(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            RiverMapScreen map = boot.RiverMapView!;
            for (int i = 0; i < times.Length; i++)
            {
                times[i] = -1;
            }

            for (int ms = 0; ms <= 1700; ms++)
            {
                if (ms == openAt)
                {
                    Assert.That(view.VictoryMapShown, Is.True);
                    view.VictoryMapButton.onClick.Invoke();
                    Assert.That(map.Visible, Is.True, "«Ver mapa» abre el Mapa encima de la victoria");
                    Assert.That(map.Source, Is.EqualTo(MapOpenSource.Victory));
                    Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
                }

                if (ms == closeAt)
                {
                    map.Tick(PlayModeHelpers.FrameMs);
                    map.PlayButton.onClick.Invoke();
                    Assert.That(map.Visible, Is.False);
                    Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "de vuelta a la victoria");
                }

                if (times[0] < 0 && view.NextButton.interactable)
                {
                    times[0] = ms;
                }

                if (times[1] < 0 && view.NextAlpha > 0f)
                {
                    times[1] = ms;
                }

                if (times[2] < 0 && view.NextAlpha >= 1f)
                {
                    times[2] = ms;
                }

                PlayModeHelpers.TickExact(boot, 1);
            }

            Assert.That(times[2], Is.GreaterThanOrEqualTo(0));
        }

        // Orden de la pausa: «Seguir jugando», «Mapa del río», «Volver a la orilla» y «Salir», de arriba abajo, sin solaparse.
        private static void AssertPauseOrder(GameScreen view)
        {
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            view.Pause.VisibleButtons(ids, rects);
            Assert.That(ids, Is.EqualTo(new[] { "resume", "pause_map", "pause_restart", "quit", "pause_music", "pause_sound" }), "con Música y Sonido junto a la X (D-046)");
            for (int i = 1; i < 4; i++) // la columna de botones; Música y Sonido van arriba, junto a la X (D-046)
            {
                float above = -rects[i - 1].anchoredPosition.y + rects[i - 1].sizeDelta.y;
                Assert.That(-rects[i].anchoredPosition.y, Is.GreaterThanOrEqualTo(above - 0.5f), ids[i] + " bajo " + ids[i - 1]);
            }
        }

        // Lo que abrir el Mapa desde la pausa no puede tocar (04 §5.2: no es salir del nivel ni fallo).
        private readonly struct Snapshot
        {
            private readonly int _canoes;
            private readonly int _seeds;
            private readonly FlowPhase _phase;
            private readonly int _level;
            private readonly int _fails;
            private readonly int _failsHard;
            private readonly int _levelsWon;
            private readonly int _maxLevel;
            private readonly int _nextLevel;
            private readonly int? _savedLevel;
            private readonly int? _savedFails;

            private Snapshot(GameBootstrap boot, MemoryProgressStore progress)
            {
                LevelFlow flow = boot.Flow!;
                Profile profile = boot.Profile!;
                _canoes = boot.Wallet!.Canoes.Count;
                _seeds = boot.Wallet.Seeds;
                _phase = flow.Phase;
                _level = flow.LevelNumber;
                _fails = flow.Attempt.FailsLevel;
                _failsHard = profile.Stats.FailsHard;
                _levelsWon = progress.LevelsWon;
                _maxLevel = progress.MaxLevel;
                _nextLevel = progress.NextLevel;
                _savedLevel = profile.LevelState?.Level;
                _savedFails = profile.LevelState?.FailsLevel;
            }

            public static Snapshot Of(GameBootstrap boot, MemoryProgressStore progress)
            {
                return new Snapshot(boot, progress);
            }

            public void AssertSame(GameBootstrap boot, MemoryProgressStore progress, string when)
            {
                Snapshot now = new Snapshot(boot, progress);
                Assert.That(now._canoes, Is.EqualTo(_canoes), when + ": canoas");
                Assert.That(now._seeds, Is.EqualTo(_seeds), when + ": semillas");
                Assert.That(now._phase, Is.EqualTo(_phase), when + ": fase");
                Assert.That(now._level, Is.EqualTo(_level), when + ": nivel");
                Assert.That(now._fails, Is.EqualTo(_fails), when + ": fails_level");
                Assert.That(now._failsHard, Is.EqualTo(_failsHard), when + ": stats.fails_hard");
                Assert.That(now._levelsWon, Is.EqualTo(_levelsWon), when + ": victorias");
                Assert.That(now._maxLevel, Is.EqualTo(_maxLevel), when + ": max_level");
                Assert.That(now._nextLevel, Is.EqualTo(_nextLevel), when + ": next_level");
                Assert.That(now._savedLevel, Is.EqualTo(_savedLevel), when + ": nivel en curso guardado");
                Assert.That(now._savedFails, Is.EqualTo(_savedFails), when + ": fails_level guardado");
            }
        }

        // AD_GATE que retiene el anuncio hasta Close (la puerta del juego cierra en la misma llamada si no hay intersticial).
        private sealed class HeldAdGate : IAdGate
        {
            private Action? _onDone;

            public int Runs { get; private set; }

            public int WonLevel { get; private set; }

            public void RecordWin(int level)
            {
            }

            public void Run(int wonLevel, Action onDone)
            {
                Runs++;
                WonLevel = wonLevel;
                _onDone = onDone;
            }

            public void Close()
            {
                Action? onDone = _onDone;
                _onDone = null;
                onDone?.Invoke();
            }
        }
    }
}
