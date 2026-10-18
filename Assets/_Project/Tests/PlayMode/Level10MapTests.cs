#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La secuencia del Mapa del 10 en la escena <c>Game</c> real (tarea M4.2; 06 §2.3, fila 3; D-013, punto 7; P-M2 y P-M4 de
    /// D-023):
    /// <list type="bullet">
    /// <item>la primera victoria del 10 por «Siguiente» pasa por AD_GATE y abre el Mapa desde <c>l10</c> encima del inicio, en la
    /// vista Cuenca: TiTino en su casa con <c>sloth_present</c> y el bocadillo <c>map_l10_intro</c>, después la placa del Dato del
    /// río 1 con su retrato mientras la canoa sube a Santarém; dura ≤ 10 s y anota lo visto hasta el 10;</item>
    /// <item>con Inicio en esa victoria, o al volver a abrir la app (también si se cerró a mitad de la secuencia), sale al entrar
    /// al inicio, una vez; con lo visto hasta el 10, ya no;</item>
    /// <item>un toque la salta; «Jugar · Nivel 11» abre el 11 y «←» deja el inicio;</item>
    /// <item>sin voz de TiTino, sin bocadillo (solo el gesto); nunca una frase <c>sloth_line_*</c>;</item>
    /// <item>las sondas <c>map_open</c> con <c>source</c> <c>l10</c> y <c>river_fact_shown</c> con el dato;</item>
    /// <item>(revisión de M4.2) el saludo del día de TiTino en el inicio no se gasta debajo del Mapa: se decide al cerrarlo; la
    /// placa del mundo de destino se ve entera toda la secuencia, sin pisar la canoa en Belém; y el pico del bocadillo llega a
    /// ≤ 8 dp de la cabeza de TiTino.</item>
    /// </list>
    /// </summary>
    public sealed class Level10MapTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 17, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.L10SequenceOverride = true;
            MapArtRule.ForceProvisional = true; // la casa y el retrato de TiTino, como en el Editor y en desarrollo (P-M5)
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.L10SequenceOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            HomeSloth.ManualTickForTests = false;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator FirstVictoryOf10_ByNext_OpensTheSequence_InTheBasin_AtMost10s()
        {
            var progress = new MemoryProgressStore(10, 9);
            yield return PlayModeHelpers.LoadGame(10, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.App!.L10MapAvailable, Is.True, "la escena trae el Mapa");
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.RiverMapView!.Visible, Is.False, "la victoria del 10 no abre el Mapa por sí sola");

            Assert.That(boot.PressNext(), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "AD_GATE y después el inicio, con la secuencia encima");
            Assert.That(map.Visible, Is.True);
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.L10));
            Assert.That(map.View, Is.EqualTo(MapView.Basin), "06 §2.3: la cuenca, con el estuario lleno");
            Assert.That(map.L10SequenceActive, Is.True);
            Assert.That(map.PlayLabel, Is.EqualTo(UiText.PlayLevel(11)));
            Assert.That(map.L10SkipButton!.gameObject.activeInHierarchy, Is.True, "un toque la salta");

            // TiTino se presenta: sloth_present y su bocadillo; la canoa espera en Belém.
            Tick(map, 16);
            Assert.That(map.HomeShown, Is.True, "la casa de TiTino con el arte provisional");
            Assert.That(map.HomeSloth!.Pose, Is.EqualTo(SlothPoseId.Present));
            Assert.That(map.L10HomeCurrentScale, Is.LessThan(RiverMapScreen.L10HomeScale), "la casa crece con un rebote");
            Tick(map, (int)RiverMapScreen.L10HomeGrowMs);
            Assert.That(map.L10HomeCurrentScale, Is.EqualTo(RiverMapScreen.L10HomeScale).Within(1e-4f), "y se queda a su tamaño de la secuencia");
            Assert.That(map.L10WithBubble, Is.True);
            Assert.That(map.L10BubbleText, Is.EqualTo("Yo vivo aquí, donde el río entra al mar."), "map_l10_intro (06 §2.3; P-M2)");
            Assert.That(map.L10TripFromMs, Is.EqualTo(NoticeTiming.ReadMs(map.L10BubbleText)), "lo que pide su lectura");
            Tick(map, 1000);
            Assert.That(map.L10BubbleShown, Is.True);
            Assert.That(map.L10FactShown, Is.False, "el dato, después del bocadillo");
            Assert.That(map.CatchingUp, Is.True, "«ponerse al día» espera a que TiTino acabe");
            Assert.That(map.CatchUpElapsedMs, Is.LessThan(MapConfig.CatchUpPerBuoyMs), "la canoa sigue en Belém");
            AssertBubblePointsAtTiTino(map);

            // La placa del Dato del río 1 con el retrato, y la canoa sube a Santarém.
            Tick(map, map.L10TripFromMs - 1000 + 300);
            Assert.That(map.L10BubbleShown, Is.False);
            Assert.That(map.L10FactShown, Is.True);
            Assert.That(map.L10FactKey, Is.EqualTo("map.stop.estuario.dato_03"), "el arrival_fact de la parada 1 (08 §4.5)");
            Assert.That(map.L10FactText, Is.EqualTo(Loc.Fact("map.stop.estuario.dato_03")));
            Assert.That(map.L10FactText, Is.Not.Empty);
            Assert.That(map.L10Portrait!.gameObject.activeSelf, Is.True, "con el retrato de TiTino");
            Assert.That(map.CatchUpElapsedMs, Is.GreaterThan(0f), "la canoa ya sale");
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(0));
            Assert.That(map.CatchUpToLevel, Is.EqualTo(10));

            // Acaba sola, dentro de los 10 s: la canoa llegó, lo visto pasa al 10 y el Mapa se queda con «Jugar · Nivel 11».
            int duration = map.L10SequenceDurationMs;
            Assert.That(duration, Is.LessThanOrEqualTo(MapConfig.L10MapMaxMs));
            Assert.That(duration, Is.GreaterThanOrEqualTo(map.L10TripFromMs + NoticeTiming.ReadMs(map.L10FactText)), "la placa, lo que pide su lectura");
            Tick(map, duration);
            Assert.That(map.L10SequenceActive, Is.False);
            Assert.That(map.L10SequenceDone, Is.True);
            Assert.That(map.L10SequenceSkipped, Is.False);
            Assert.That(map.L10SequenceElapsedMs, Is.LessThanOrEqualTo(MapConfig.L10MapMaxMs + PlayModeHelpers.FrameMs));
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.CatchUpArrivals, Is.EqualTo(1), "la llegada a Santarém");
            Assert.That(map.LastCatchUpArrivalLevel, Is.EqualTo(10));
            Assert.That(map.LastCatchUpArrivalSkipped, Is.False);
            Assert.That(map.HomeSloth.Pose, Is.EqualTo(SlothPoseId.Idle), "TiTino vuelve a su reposo");
            Assert.That(map.L10SkipButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(progress.MapSeenLevel, Is.EqualTo(10), "MarkMapSeen(10): no se repite");
            Assert.That(map.Visible, Is.True, "el Mapa se queda: «Jugar · Nivel 11» y «←»");
            Tick(map, 300);
            Assert.That(map.L10FactShown, Is.False, "la placa se va con su fundido");

            // «Jugar · Nivel 11» abre el 11.
            map.PlayButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(11));
        }

        [UnityTest]
        public IEnumerator HomeInTheVictoryOf10_OpensTheSequenceAtTheLobby_SkipAndBack()
        {
            var progress = new MemoryProgressStore(10, 9);
            yield return PlayModeHelpers.LoadGame(10, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.PressHome(), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(map.Visible, Is.True, "P-M4: tras Inicio, al entrar al inicio");
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.L10));
            Assert.That(map.L10SequenceActive, Is.True);
            Tick(map, 500);

            // Un toque la salta: la canoa llega en el acto (la llegada, saltada) y lo visto pasa al 10.
            map.L10SkipButton!.onClick.Invoke();
            Assert.That(map.L10SequenceActive, Is.False);
            Assert.That(map.L10SequenceSkipped, Is.True);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.CatchUpArrivals, Is.EqualTo(1));
            Assert.That(map.LastCatchUpArrivalSkipped, Is.True);
            Assert.That(map.HomeSloth!.Pose, Is.EqualTo(SlothPoseId.Idle));
            Assert.That(progress.MapSeenLevel, Is.EqualTo(10));
            Assert.That(map.SkipL10Sequence(), Is.False, "ya no está en marcha");
            Tick(map, 300);
            Assert.That(map.L10BubbleShown, Is.False);
            Assert.That(map.L10FactShown, Is.False);

            // «←» deja el inicio, y la secuencia no vuelve a salir al volver a entrar.
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.L10MapDue, Is.False);
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(11));
            Assert.That(boot.App.ExitToLobby(), Is.True);
            Assert.That(map.Visible, Is.False, "no se repite");
        }

        [UnityTest]
        public IEnumerator AfterKillingTheApp_TheSequenceShowsAtTheLobby_UntilItIsSeen_ThenNeverAgain()
        {
            Profile profile = ProfileWonTen();
            var store = new MemoryProfileStore(profile);

            // La app se cerró en la victoria del 10: al abrirla, el inicio con la secuencia encima.
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(map.Visible, Is.True);
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.L10));
            Assert.That(map.L10SequenceActive, Is.True);

            // Se vuelve a cerrar a mitad de la secuencia: sale otra vez (aún no se vio).
            yield return null;
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(0));
            yield return PlayModeHelpers.LoadApp(store);
            boot = GameBootstrap.Current!;
            map = boot.RiverMapView!;
            Assert.That(map.Visible, Is.True, "cerrada a medias: no se vio");
            Assert.That(map.L10SequenceActive, Is.True);

            // Se ve (aquí, saltándola) y se cierra el Mapa: ya no sale.
            Assert.That(map.SkipL10Sequence(), Is.True);
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(10));
            Assert.That(boot.CloseRiverMap(), Is.True);
            yield return PlayModeHelpers.LoadApp(store);
            boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.RiverMapView!.Visible, Is.False, "una sola vez");
            Assert.That(boot.App.L10MapDue, Is.False);
        }

        [UnityTest]
        public IEnumerator AlreadySeen_OrBefore10_NoSequence()
        {
            Profile seen = ProfileWonTen();
            seen.Progress.MapSeenLevel = 10;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(seen));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.RiverMapView!.Visible, Is.False, "map_seen_level 10");

            Profile nine = Wallet.NewProfile(new ManualClock(T0));
            nine.Progress.NextLevel = 10;
            nine.Progress.MaxLevel = 9;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(nine));
            boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.RiverMapView!.Visible, Is.False, "max_level 9: el 10 aún no se ganó");

            // Con la secuencia apagada (sin el Mapa en la escena o en los tests que no tratan de ella), nada sale solo.
            GameBootstrap.L10SequenceOverride = false;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileWonTen()));
            boot = GameBootstrap.Current!;
            Assert.That(boot.App!.L10MapAvailable, Is.False);
            Assert.That(boot.RiverMapView!.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutSlothVoice_NoBubble_OnlyTheGesture_AndNeverASlothLine()
        {
            Profile profile = ProfileWonTen();
            profile.Settings.SlothMessages = false; // Ajustes › Mensajes de TiTino apagado (D-016, punto 4)
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.L10SequenceActive, Is.True);
            Tick(map, 16);
            Assert.That(map.HomeSloth!.Pose, Is.EqualTo(SlothPoseId.Present), "el gesto, sin voz");
            Assert.That(map.L10WithBubble, Is.False);
            Assert.That(map.L10BubbleText, Is.Empty);
            Assert.That(map.L10TripFromMs, Is.EqualTo(RiverMapScreen.PresentOnlyMs));
            Tick(map, 500);
            Assert.That(map.L10BubbleShown, Is.False);
            Tick(map, RiverMapScreen.PresentOnlyMs);
            Assert.That(map.L10FactShown, Is.True, "el dato no es una frase de TiTino: sale igual");

            // El bocadillo es map_l10_intro, fuera del prefijo sloth_line_* (glosario §10).
            Assert.That(Loc.Has("map_l10_intro"), Is.True);
            Assert.That(UiText.MapL10Intro, Is.EqualTo(Loc.T("map_l10_intro")));
            Assert.That("map_l10_intro".StartsWith("sloth_line_", StringComparison.Ordinal), Is.False);
        }

        [UnityTest]
        public IEnumerator AndroidBack_MidSequence_ClosesTheMap_AsSkipped()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileWonTen()));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            RiverMapScreen map = boot.RiverMapView!;
            Tick(map, 400);
            Assert.That(map.L10SequenceActive, Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.Visible, Is.False);
            Assert.That(map.L10SequenceDone, Is.True);
            Assert.That(map.L10SequenceSkipped, Is.True);
            Assert.That(map.L10BubbleShown, Is.False);
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(10));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        [UnityTest]
        public IEnumerator HomeGreeting_IsNotSpentUnderTheSequence_ButWhenTheMapCloses()
        {
            // Revisión de M4.2: el primer arranque tras ganar el 10, con el saludo de regreso pendiente (7 días sin abrir). La
            // ventanita de casa del inicio no se ve bajo el Mapa, así que TiTino no decide su saludo del día (como mucho 1;
            // D-016, punto 5) mientras dura la secuencia, y sí al cerrar el Mapa, en la misma visita.
            HomeSloth.ManualTickForTests = true;
            var clock = new ManualClock(T0);
            Profile profile = ProfileWonTen();
            profile.Sloth.LastSeenDate = clock.LocalNow().AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            LaunchOptions.ClockOverride = clock;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            LobbyScreen lobby = boot.Lobby!;
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(map.Visible, Is.True);
            Assert.That(map.L10SequenceActive, Is.True);

            // Bajo la secuencia: la ventanita no se ve y TiTino no saluda, aunque su reloj avance.
            yield return null;
            yield return null;
            for (int t = 0; t < 1000; t += 16)
            {
                sloth.Tick(16f);
            }

            Assert.That(map.L10SequenceActive, Is.True, "sigue la secuencia");
            Assert.That(lobby.HomeWindow.Visible, Is.False, "la ventanita de casa, oculta bajo el Mapa");
            Assert.That(sloth.Shown, Is.False);
            Assert.That(sloth.GreetingDecisions, Is.EqualTo(0), "el saludo del día no se decide debajo del Mapa");
            Assert.That(sloth.BubbleShown, Is.False);
            Assert.That(boot.Profile!.Sloth.LastGreetingDate, Is.Not.EqualTo(clock.LocalDateKey()), "ni se gasta el de hoy");
            Assert.That(boot.View!.SlothSession.LinesThisSession, Is.EqualTo(0), "ninguna frase de TiTino en el inicio");
            Assert.That(boot.View.SlothSession.PreviousScreenSpoke, Is.False);

            // Se salta y se cierra el Mapa con «←»: la ventanita vuelve y TiTino saluda en esa misma visita.
            Assert.That(map.SkipL10Sequence(), Is.True);
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.HomeWindow.Visible, Is.True, "la ventanita, de vuelta con el inicio a la vista");
            sloth.Tick(16f);
            Assert.That(sloth.GreetingDecisions, Is.EqualTo(1), "el saludo se decide al cerrar el Mapa");
            Assert.That(sloth.LastGreeting.Greeting, Is.True, "el de regreso, a la vista");
            Assert.That(boot.Profile.Sloth.LastGreetingDate, Is.EqualTo(clock.LocalDateKey()));
            yield return null;
            Assert.That(lobby.HomeWindow.Visible, Is.True, "y sigue a la vista");
            Assert.That(sloth.GreetingDecisions, Is.EqualTo(1), "una vez");
        }

        [UnityTest]
        public IEnumerator Probes_MapOpenWithSourceL10_AndRiverFactShown()
        {
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_open\"", StringComparison.Ordinal)
                    || message.StartsWith("AF_STATE {\"event\":\"river_fact_shown\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            Application.logMessageReceived += OnLog;
            try
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileWonTen()));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                RiverMapScreen map = boot.RiverMapView!;
                Tick(map, 16);
                Tick(map, map.L10TripFromMs + 1000);
                Assert.That(map.L10FactShown, Is.True);
                Assert.That(map.SkipL10Sequence(), Is.True);
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
            StringAssert.Contains("\"event\":\"map_open\"", lines[0]);
            StringAssert.Contains("\"source\":\"l10\"", lines[0]);
            StringAssert.Contains("\"level\":11", lines[0]);
            StringAssert.Contains("\"view\":\"basin\"", lines[0]);
            StringAssert.Contains("\"event\":\"river_fact_shown\"", lines[1]);
            StringAssert.Contains("\"level\":10", lines[1]);
            StringAssert.Contains("\"fact_id\":\"map.stop.estuario.dato_03\"", lines[1]);
        }

        [UnityTest]
        public IEnumerator Layout_TheBubbleAndThePlate_StayInsideTheMap_At360x640_AndWithLargeText()
        {
            foreach (Vector4 screen in new[] { new Vector4(1080f, 1920f, 72f, 48f), new Vector4(1080f, 2340f, 96f, 48f) })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    GameScreen.SimulatedScreen = screen;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileWonTen()));
                    GameBootstrap boot = GameBootstrap.Current!;
                    PlayModeHelpers.TakeClock(boot);
                    RiverMapScreen map = boot.RiverMapView!;
                    Tick(map, 400);
                    string what = screen.y + " px, texto ×" + scale.ToString(CultureInfo.InvariantCulture);
                    RectTransform root = (RectTransform)map.Canvas.transform;
                    Rect view = VictorySetup.CanvasRect(map.ViewportRect, root);
                    Assert.That(map.L10BubbleShown, Is.True, what);
                    Rect bubble = VictorySetup.CanvasRect(map.L10Bubble!.Rect, root);
                    AssertInside(bubble, view, "bocadillo, " + what);
                    Assert.That(map.L10Bubble.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp * scale - 0.01f));
                    Assert.That(map.L10Bubble.Label.isTextOverflowing, Is.False, what);
                    AssertBubblePointsAtTiTino(map);

                    Tick(map, map.L10TripFromMs);
                    Assert.That(map.L10FactShown, Is.True, what);
                    Rect plate = VictorySetup.CanvasRect(map.L10Plate!, root);
                    AssertInside(plate, view, "placa, " + what);
                    Rect header = VictorySetup.CanvasRect(map.HeaderRect, root);
                    Assert.That(plate.yMin, Is.GreaterThanOrEqualTo(header.yMax - 0.5f), "la placa no tapa la cabecera, " + what);
                }
            }
        }

        [UnityTest]
        public IEnumerator AtPhoneSize_TiTinoGrows_NorthUpHides_PlatesGo_AndNoLabelIsHalfCovered()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileWonTen()));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            RiverMapScreen map = boot.RiverMapView!;
            RectTransform root = (RectTransform)map.Canvas.transform;
            SlothRig rig = map.HomeSloth!;
            Assert.That(map.NorthUpShown, Is.True, "el botón existe en la Cuenca");

            // Se queda a L10HomeScale (el rebote de entrada, con el reloj del test desde el primer fotograma: FirstVictoryOf10_*):
            // TiTino, de ≥ 40 dp de alto, señala hacia el estuario.
            Tick(map, 400);
            Assert.That(map.L10HomeCurrentScale, Is.EqualTo(RiverMapScreen.L10HomeScale).Within(1e-4f));
            Assert.That(rig.Pose, Is.EqualTo(SlothPoseId.Present));
            var pieces = new List<Rect>();
            rig.CollectRects(root, pieces);
            Assert.That(pieces.Count, Is.GreaterThan(0));
            float top = float.MaxValue;
            float bottom = float.MinValue;
            foreach (Rect r in pieces)
            {
                top = Mathf.Min(top, r.yMin);
                bottom = Mathf.Max(bottom, r.yMax);
            }

            Assert.That(bottom - top, Is.GreaterThanOrEqualTo(RiverMapScreen.L10TiTinoMinDp), "TiTino se ve a tamaño de teléfono");
            Vector2 hand = rig.Current.FrontArmTo - new Vector2(0.22f, 0.02f);
            Assert.That(hand.magnitude * rig.Size * RiverMapScreen.L10HomeScale, Is.GreaterThanOrEqualTo(10f), "sloth_present: la garra se suelta y señala");
            Rect tree = VictorySetup.CanvasRect(map.HomeTree!.rectTransform, root);
            Assert.That(tree.height, Is.GreaterThan(map.HomeTree.rectTransform.rect.height * 1.5f), "la embaúba crece con él");
            AssertBubblePointsAtTiTino(map);

            // «Norte arriba», fuera y sin toques durante toda la secuencia.
            CanvasGroup northUp = map.NorthUpButton.GetComponent<CanvasGroup>();
            Assert.That(northUp, Is.Not.Null);
            Assert.That(northUp.alpha, Is.EqualTo(0f));
            Assert.That(northUp.interactable, Is.False);
            Assert.That(northUp.blocksRaycasts, Is.False);
            AssertNoLabelHalfCovered(map, root, VictorySetup.CanvasRect(map.L10Bubble!.Rect, root), "bocadillo");

            // La placa del mundo de destino (el 2, el de «Jugar · Nivel 11»), entera y sin pisar la canoa en Belém con su mástil y
            // sus banderas (revisión de M4.2).
            RectTransform destination = map.L10DestinationPlate!;
            Assert.That(destination, Is.Not.Null, "la placa del mundo 2 se ve en la secuencia");
            StringAssert.Contains("w02", destination.name);
            AssertWhole(destination, "placa del destino, con TiTino");
            AssertClearOfCanoe(map, root, destination);
            Rect view = VictorySetup.CanvasRect(map.ViewportRect, root);
            Rect dest = VictorySetup.CanvasRect(destination, root);
            Assert.That(dest.yMin, Is.GreaterThanOrEqualTo(view.yMin - 0.5f), "sin salir de la vista");

            // Con la placa del dato y la canoa de viaje: las placas de mundo se van (salvo la del destino) y ningún rótulo queda a
            // medio tapar por ella.
            Tick(map, map.L10TripFromMs - 400 + 600);
            Assert.That(map.L10FactShown, Is.True);
            Assert.That(map.CatchingUp, Is.True, "la canoa aún no llegó a Santarém");
            Assert.That(map.L10DestinationPlate, Is.SameAs(destination));
            foreach (RectTransform plate in map.SilhouettePlates())
            {
                CanvasGroup g = plate.GetComponent<CanvasGroup>();
                if (plate == destination)
                {
                    AssertWhole(plate, "placa del destino, con la placa del dato");
                    continue;
                }

                Assert.That(g != null && g.alpha <= 0.001f, Is.True, plate.name + ": fuera mientras se ve la placa del dato");
            }

            AssertNoLabelHalfCovered(map, root, VictorySetup.CanvasRect(map.L10Plate!, root), "placa del dato");
            Assert.That(northUp.alpha, Is.EqualTo(0f));

            // La canoa llega a Santarém, la gran parada del mundo 2: «ponerse al día» quita su silueta (ya no es un mundo futuro) y
            // su placa pasa a otro mundo, que se trata como las demás.
            Tick(map, map.CatchUpDurationMs);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.L10DestinationPlate, Is.Null, "el mundo 2 ya no es futuro");
            if (map.L10FactShown)
            {
                foreach (RectTransform plate in map.SilhouettePlates())
                {
                    CanvasGroup g = plate.GetComponent<CanvasGroup>();
                    Assert.That(g != null && g.alpha <= 0.001f, Is.True, plate.name + ": fuera mientras se ve la placa del dato");
                }
            }

            // Al acabar, todo vuelve: la casa a su tamaño y mirando como siempre, los rótulos y «Norte arriba».
            Tick(map, map.L10SequenceDurationMs);
            Assert.That(map.L10SequenceDone, Is.True);
            Tick(map, 300);
            Assert.That(map.L10HomeCurrentScale, Is.EqualTo(1f));
            Assert.That(map.L10DimAmount, Is.EqualTo(0f));
            Assert.That(rig.Root.localScale.x, Is.EqualTo(1f), "mira como siempre");
            Assert.That(VictorySetup.CanvasRect(map.HomeTree.rectTransform, root).height, Is.EqualTo(map.HomeTree.rectTransform.rect.height).Within(0.5f));
            Assert.That(northUp.alpha, Is.EqualTo(1f));
            Assert.That(northUp.interactable, Is.True);
            Assert.That(northUp.blocksRaycasts, Is.True);
            Assert.That(map.L10DestinationLifted, Is.False, "la placa del destino, otra vez en su sitio");
            foreach (RectTransform plate in map.SilhouettePlates())
            {
                CanvasGroup g = plate.GetComponent<CanvasGroup>();
                Assert.That(g == null || g.alpha >= 0.999f, Is.True, plate.name + ": vuelve");
            }

            foreach (TMPro.TextMeshProUGUI country in map.CountryLabels())
            {
                CanvasGroup g = country.GetComponent<CanvasGroup>();
                Assert.That(g == null || g.alpha >= 0.999f, Is.True, country.name + ": vuelve");
            }
        }

        // Ningún nombre de país ni placa de mundo a la vista entero (alpha ≥ 0,99) pisa la pieza piece (con el margen de
        // L10DimMarginDp); uno atenuado nunca pasa de L10DimAlpha, y uno que la pieza (opaca) tapa de verdad no se ve.
        private static void AssertNoLabelHalfCovered(RiverMapScreen map, RectTransform root, Rect piece, string what)
        {
            float m = RiverMapScreen.L10DimMarginDp - 0.5f;
            Rect grown = Rect.MinMaxRect(piece.xMin - m, piece.yMin - m, piece.xMax + m, piece.yMax + m);
            var labels = new List<RectTransform>();
            foreach (TMPro.TextMeshProUGUI country in map.CountryLabels())
            {
                labels.Add(country.rectTransform);
            }

            labels.AddRange(map.SilhouettePlates());
            int checkedLabels = 0;
            foreach (RectTransform label in labels)
            {
                CanvasGroup g = label.GetComponent<CanvasGroup>();
                float alpha = g != null ? g.alpha : 1f;
                Rect box = VictorySetup.CanvasRect(label, root);
                checkedLabels++;
                if (alpha >= 0.99f)
                {
                    Assert.That(box.Overlaps(grown), Is.False, label.name + " entero, pisado por el " + what);
                }
                else if (box.Overlaps(piece))
                {
                    Assert.That(alpha, Is.LessThanOrEqualTo(0.001f), label.name + ": tapado de verdad por el " + what + ", fuera");
                }
                else
                {
                    Assert.That(alpha, Is.LessThanOrEqualTo(RiverMapScreen.L10DimAlpha + 0.001f), label.name + ": atenuado o fuera");
                }
            }

            Assert.That(checkedLabels, Is.GreaterThan(0), "hay rótulos que mirar");
        }

        // La placa se ve entera (sin CanvasGroup o con alpha 1).
        private static void AssertWhole(RectTransform plate, string what)
        {
            CanvasGroup g = plate.GetComponent<CanvasGroup>();
            Assert.That(plate.gameObject.activeInHierarchy, Is.True, what);
            Assert.That(g == null || g.alpha >= 0.999f, Is.True, what + ": entera");
        }

        // La placa no corta la canoa con su mástil y sus banderas donde están ahora (con L10DimMarginDp de margen).
        private static void AssertClearOfCanoe(RiverMapScreen map, RectTransform root, RectTransform plate)
        {
            Rect box = VictorySetup.CanvasRect(plate, root);
            var gear = new List<RectTransform> { map.Canoe.rectTransform };
            if (map.Mast != null && map.Mast.gameObject.activeInHierarchy)
            {
                gear.Add(map.Mast.rectTransform);
            }

            for (int i = 0; i < map.CanoeFlagCount; i++)
            {
                gear.Add(map.CanoeFlagImage(i).rectTransform);
            }

            float m = RiverMapScreen.L10DimMarginDp - 0.5f;
            foreach (RectTransform rt in gear)
            {
                Rect o = VictorySetup.CanvasRect(rt, root);
                var grown = Rect.MinMaxRect(o.xMin - m, o.yMin - m, o.xMax + m, o.yMax + m);
                Assert.That(box.Overlaps(grown), Is.False, plate.name + " pisa " + rt.name + " de la canoa");
            }
        }

        // Avanza el Mapa ms milisegundos en pasos de un fotograma (el reloj del juego está quitado).
        internal static void Tick(RiverMapScreen map, int ms)
        {
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }
        }

        // El 10 ganado y la app cerrada en su victoria (sin nivel en curso, «Jugar · Nivel 11») y sin haber visto el Mapa.
        internal static Profile ProfileWonTen()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 11;
            profile.Progress.MaxLevel = 10;
            profile.Progress.MapSeenLevel = 0;
            return profile;
        }

        // El pico del bocadillo apunta a TiTino: su punta, sobre él y a menos de medio cuerpo de su cabeza en horizontal.
        private static void AssertBubblePointsAtTiTino(RiverMapScreen map)
        {
            RectTransform root = (RectTransform)map.Canvas.transform;
            SlothRig rig = map.HomeSloth!;
            Rect tail = VictorySetup.CanvasRect(map.L10Bubble!.Tail, root);
            Rect bubble = VictorySetup.CanvasRect(map.L10Bubble.Rect, root);
            Vector3 local = root.InverseTransformPoint(rig.Root.position);
            Rect r = root.rect;
            var at = new Vector2(local.x - r.xMin, r.yMax - local.y);
            Assert.That(bubble.yMax, Is.LessThanOrEqualTo(at.y), "el bocadillo, encima de TiTino");
            Assert.That(Mathf.Abs(tail.center.x - at.x), Is.LessThanOrEqualTo((rig.Size * RiverMapScreen.L10HomeScale * 0.5f) + 1f), "el pico, sobre TiTino");
            Assert.That(tail.yMax, Is.LessThanOrEqualTo(at.y + 1f), "el pico no lo tapa");

            // Revisión de M4.2: TiTino cuelga bajo la rama, así que la punta del pico (o de su tallo) llega a ≤ 8 dp de su cabeza,
            // por encima de ella, sin taparle la cara.
            var head = rig.Root.Find("Body/Head") as RectTransform;
            Assert.That(head, Is.Not.Null, "la cabeza del rig");
            Rect headBox = VictorySetup.CanvasRect(head!, root);
            Rect pointer = map.L10Bubble.StemShown ? VictorySetup.CanvasRect(map.L10Bubble.Stem, root) : tail;
            var tip = new Vector2(pointer.center.x, pointer.yMax);
            float dx = Mathf.Max(0f, Mathf.Max(headBox.xMin - tip.x, tip.x - headBox.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(headBox.yMin - tip.y, tip.y - headBox.yMax));
            Assert.That(Mathf.Sqrt((dx * dx) + (dy * dy)), Is.LessThanOrEqualTo(8f), "la punta del pico, a ≤ 8 dp de la cabeza de TiTino");
            Assert.That(tip.y, Is.LessThanOrEqualTo(headBox.yMin + 0.5f), "sin taparle la cara");
            if (map.L10Bubble.StemShown)
            {
                Assert.That(pointer.width, Is.EqualTo(RiverMapScreen.L10BubbleStemDp).Within(0.6f), "un tallo fino");
                Assert.That(pointer.yMin, Is.LessThanOrEqualTo(tail.yMax), "que sale del pico");
            }
        }

        private static void AssertInside(Rect inner, Rect outer, string what)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 0.5f), what + ": izquierda");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 0.5f), what + ": derecha");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 0.5f), what + ": arriba");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 0.5f), what + ": abajo");
        }
    }

    /// <summary>
    /// Capturas de la secuencia del Mapa del 10 (tarea M4.2; 06 §2.3), con el arte provisional: <c>17_map_l10</c> (TiTino en su casa
    /// del estuario con <c>sloth_present</c> y el bocadillo <c>map_l10_intro</c>, en la vista Cuenca) y <c>17b_map_l10_dato</c> (la
    /// placa del Dato del río 1 con el retrato y la canoa en Santarém). Cada captura escribe su línea <c>AF_MAP</c>. Explícito:
    /// necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Level10MapSnapshotTests</c> sin
    /// <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la secuencia del Mapa del 10 (M4.2); necesitan dispositivo gráfico.")]
    public sealed class Level10MapSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            GameBootstrap.L10SequenceOverride = true;
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.L10SequenceOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheLevel10Map()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(Level10MapTests.ProfileWonTen()));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Level10MapTests.Tick(map, 1200);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "17_map_l10");
            Log("17_map_l10", map);

            Level10MapTests.Tick(map, map.L10TripFromMs - 1200 + map.CatchUpDurationMs + 600);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "17b_map_l10_dato");
            Log("17b_map_l10_dato", map);
        }

        private static void Log(string name, RiverMapScreen map)
        {
            Debug.Log("AF_MAP " + name + ": vista " + map.View + ", zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture)
                + ", secuencia " + map.L10SequenceElapsedMs.ToString("0", CultureInfo.InvariantCulture) + "/" + map.L10SequenceDurationMs
                + " ms, bocadillo «" + map.L10BubbleText + "» " + map.L10BubbleShown + ", dato «" + map.L10FactText + "» " + map.L10FactShown
                + ", canoa " + map.CanoeInViewport + ", «" + map.PlayLabel + "»");
        }
    }
}
