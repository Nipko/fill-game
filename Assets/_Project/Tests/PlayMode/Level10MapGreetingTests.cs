#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
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
    /// El bocadillo del Mapa del 10 y el saludo del inicio en la escena <c>Game</c> real (tarea DC5; D-028, pregunta 60 b de GDD
    /// §6.3; D-016, punto 5; 06 §2.3): ganar el 10, «Siguiente», la secuencia con <c>map_l10_intro</c> y cerrar el Mapa deja el
    /// inicio sin saludo (ni la mano ni la frase) y sin gastarlo (<c>sloth.last_greeting_date</c> y <c>sloth.last_seen_date</c>
    /// intactos); el contador de frases de la sesión no sube con el bocadillo; en la visita siguiente al inicio (un nivel y
    /// vuelta), el saludo de la mañana sale con su frase.
    /// </summary>
    public sealed class Level10MapGreetingTests
    {
        // 08:00 (hora local con desfase 0): le toca el saludo de la mañana (primera apertura del día entre las 06:00 y las 12:00).
        private static readonly DateTime T0 = new DateTime(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.L10SequenceOverride = true;
            MapArtRule.ForceProvisional = true; // la casa de TiTino, como en el Editor y en desarrollo (P-M5)
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
            HomeSloth.ManualTickForTests = true;
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
        public IEnumerator VictoryOf10_MapBubble_HomeWithoutGreeting_ThenTheNextVisitGreets()
        {
            var clock = new ManualClock(T0);
            string today = clock.LocalDateKey();
            string yesterday = clock.LocalNow().AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Profile profile = Wallet.NewProfile(clock);
            profile.Sloth.LastSeenDate = yesterday;
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(10, new MemoryProgressStore(10, 9));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SlothData sloth = boot.Profile!.Sloth;
            Assert.That(sloth.LastSeenDate, Is.EqualTo(yesterday), "el inicio aún no se ha visto hoy");
            Assert.That(sloth.LastGreetingDate, Is.Null);

            // La victoria del 10 y «Siguiente»: el Mapa del 10 encima del inicio, con el bocadillo de TiTino.
            VictorySetup.WinToVictoryScreen(boot);
            SlothSession session = boot.View!.SlothSession;
            int linesBefore = session.LinesThisSession;
            Assert.That(boot.PressNext(), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(map.Visible && map.L10SequenceActive, Is.True, "la secuencia del Mapa del 10");
            Level10MapTests.Tick(map, 1000);
            Assert.That(map.L10WithBubble, Is.True);
            Assert.That(map.L10BubbleShown, Is.True, "map_l10_intro, a la vista");
            Assert.That(session.LinesThisSession, Is.EqualTo(linesBefore), "el bocadillo no gasta ninguna de las 4 frases");
            Assert.That(session.PreviousScreenSpoke, Is.True, "pero cuenta como pantalla en la que TiTino habló");
            Assert.That(session.PreviousScreenMapIntro, Is.True);

            // La secuencia acaba y «←» deja el inicio: ni saludo ni gasto.
            Level10MapTests.Tick(map, map.L10SequenceDurationMs);
            Assert.That(map.L10SequenceDone, Is.True);
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            LobbyScreen lobby = boot.Lobby!;
            HomeSloth home = lobby.HomeWindow.Sloth!;
            Assert.That(lobby.HomeWindow.Visible, Is.True, "la ventanita de casa, a la vista");
            home.Tick(16f);
            Assert.That(home.GreetingDecisions, Is.EqualTo(1), "la visita decide su saludo");
            SlothLine first = home.LastGreeting;
            Assert.That(first.Deferred, Is.True, "aplazado (D-028, pregunta 60 b)");
            Assert.That(first.Greeting || first.Speaks, Is.False, "el inicio no enseña el saludo");
            Assert.That(first.Gesture, Is.EqualTo(SlothGesture.None), "ni la mano");
            Assert.That(home.BubbleShown, Is.False);
            Assert.That(home.Pose, Is.Not.EqualTo(SlothPoseId.Wave));
            Assert.That(sloth.LastGreetingDate, Is.Null, "sloth.last_greeting_date no se escribe");
            Assert.That(sloth.LastSeenDate, Is.EqualTo(yesterday), "ni sloth.last_seen_date: el saludo queda para la visita siguiente");
            Assert.That(session.LinesThisSession, Is.EqualTo(linesBefore), "contador intacto");
            Assert.That(session.PreviousScreenMapIntro, Is.False, "el aviso dura una pantalla");

            // Otra visita al inicio (el 11 y vuelta): ahora sí, el saludo de la mañana con su frase.
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(11));
            Assert.That(boot.App.ExitToLobby(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.HomeWindow.Visible, Is.True);
            home.Tick(16f);
            Assert.That(home.GreetingDecisions, Is.EqualTo(2), "una visita nueva");
            SlothLine second = home.LastGreeting;
            Assert.That(second.Deferred, Is.False);
            Assert.That(second.Greeting, Is.True, "el saludo del día, en la visita siguiente");
            Assert.That(second.Key, Is.EqualTo("sloth_line_return_04"), "el de la mañana (e.4, n.º 25)");
            Assert.That(home.BubbleShown, Is.True);
            Assert.That(home.Pose, Is.EqualTo(SlothPoseId.Wave));
            Assert.That(sloth.LastGreetingDate, Is.EqualTo(today), "ahora sí se gasta");
            Assert.That(sloth.LastSeenDate, Is.EqualTo(today));
            Assert.That(session.LinesThisSession, Is.EqualTo(linesBefore + 1), "y gasta una de las 4 frases");
        }
    }
}
