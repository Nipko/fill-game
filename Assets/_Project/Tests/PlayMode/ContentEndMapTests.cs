#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
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
    /// El Mapa del río al acabarse el contenido, en la escena <c>Game</c> real (tarea M4.3; 06 §9; D-013, punto 7; 08 §6.2, §8.2 y
    /// §9): al ganar el último nivel publicado (hoy el 700, desde C1b), «Siguiente» pasa por AD_GATE y, antes de «El río sigue
    /// creciendo», abre el Mapa desde <c>content_end</c>:
    /// <list type="bullet">
    /// <item>el viaje del último tramo publicado (del 690 al 700) de boya en boya, dentro del tope de <c>anim_map_world_ms</c>, sin
    /// repetir la llegada ya vista (ningún <c>stop_arrive</c> más) y con «Volver» en lugar de «Jugar»;</item>
    /// <item>un toque lo completa; con Reducir movimiento, el estado final con el fundido de «ponerse al día»;</item>
    /// <item>sin frase de TiTino: ni el bocadillo del Mapa ni el saludo o el Dato de la ventanita de casa, debajo;</item>
    /// <item>al cerrarlo (← o «Volver»), la pantalla de siempre, con su título y su ETA, y sin frase de TiTino;</item>
    /// <item>la sonda <c>map_open</c> con <c>source</c> <c>content_end</c>;</item>
    /// <item>«Jugar» en el inicio con el contenido ya acabado abre la pantalla sola, sin el Mapa.</item>
    /// </list>
    /// </summary>
    public sealed class ContentEndMapTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 18, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.ContentEndMapOverride = true;
            GameScreen.ArrivalDisabledForTests = false; // la franja del 700 anota lo visto, como en el juego
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ContentEndMapOverride = null;
            GameBootstrap.ResetDailyOverrides();
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            RiverMapScreen? map = GameBootstrap.Current != null ? GameBootstrap.Current.RiverMapView : null;
            if (map != null)
            {
                map.ProgressReduceMotion = false;
            }

            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Winning700_Next_TheMapWithTheJourneyOfTheLastLeg_ThenTheRiverKeepsGrowing()
        {
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_open\"", StringComparison.Ordinal)
                    || message.StartsWith("AF_STATE {\"event\":\"stop_arrive\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            yield return FactLineTests.Win(700);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.Flow!.Catalog.LastLevel, Is.EqualTo(700), "el último nivel publicado (C1b)");
            Assert.That(boot.App!.ContentEndMapAvailable, Is.True, "la escena trae el Mapa");
            Assert.That(map.Visible, Is.False, "la victoria del 700 no lo abre por sí sola");

            // TiTino: lo que ya dijo en la sesión (la victoria) y los saludos del inicio decididos hasta ahora.
            Assert.That(boot.Lobby!.HomeWindow.Sloth, Is.Not.Null, "TiTino en la ventanita de casa, debajo de la pantalla");
            HomeSloth sloth = boot.Lobby.HomeWindow.Sloth!;
            SlothSession slothSession = boot.View!.SlothSession;
            int slothLines = slothSession.LinesThisSession;
            int greetings = sloth.GreetingDecisions;

            Application.logMessageReceived += OnLog;
            try
            {
                NextToContentEnd(boot);
                AppFlow app = boot.App;
                Assert.That(app.Screen, Is.EqualTo(AppScreen.ContentEnd), "06 §9: tras el 700, «El río sigue creciendo»");
                Assert.That(app.OverlayLevel, Is.EqualTo(701));
                Assert.That(app.ContentEndMapDue, Is.True);
                Assert.That(map.Visible, Is.True, "antes, el Mapa encima");
                Assert.That(map.Source, Is.EqualTo(MapOpenSource.ContentEnd));
                Assert.That(map.PlayLabel, Is.EqualTo(UiText.Back), "nada que jugar: «Volver» [P]");
                Assert.That(boot.Lobby!.OverlayVisible, Is.True, "la pantalla de siempre espera debajo");
                AssertNoSlothLine(map, sloth, slothSession, slothLines, greetings, "con el Mapa recién abierto");

                // El viaje del último tramo publicado, de boya en boya, aunque la franja de la victoria ya lo anotara como visto.
                Assert.That(map.ContentEndJourney, Is.True);
                Assert.That(map.ContentEndJourneyFromLevel, Is.EqualTo(690));
                Assert.That(RiverMapScreen.LastLegStart(700), Is.EqualTo(690));
                Assert.That(map.CatchingUp, Is.True, "la canoa recorre el tramo");
                Assert.That(map.CatchUpFromLevel, Is.EqualTo(690));
                Assert.That(map.CatchUpToLevel, Is.EqualTo(700));
                int journey = map.ContentEndJourneyMs;
                Assert.That(journey, Is.EqualTo(RiverJourney.CatchUpMs(690, 700)));
                Assert.That(journey, Is.GreaterThan(0));
                Assert.That(journey, Is.LessThanOrEqualTo(MapConfig.AnimMapWorldMs), "tope de anim_map_world_ms (08 §9)");

                Assert.That(map.PlayShown, Is.False, "D-053: «Volver» espera a que la canoa llegue");
                Tick(map, journey / 2);
                Assert.That(map.CatchingUp, Is.True, "a medio tramo");
                Assert.That(map.JourneyFirstMoveMs, Is.GreaterThan(0f).And.LessThanOrEqualTo(300f), "D-053: se mueve en ≤ 300 ms");
                Tick(map, journey);
                Assert.That(map.PlayShown, Is.True, "llegó: «Volver» y «←»");
                Assert.That(map.CatchingUp, Is.False, "llegó, dentro del tope");
                Assert.That(map.CatchUpCompletedByTouch, Is.False);
                Assert.That(map.CatchUpElapsedMs, Is.LessThanOrEqualTo(MapConfig.AnimMapWorldMs));
                Assert.That(map.CatchUpArrivals, Is.EqualTo(0), "la llegada del último tramo ya se vio en la victoria (o el 700 no tiene parada de llegada): no se repite");
                Assert.That(map.Visible, Is.True, "el Mapa se queda hasta que se cierra");
                AssertNoSlothLine(map, sloth, slothSession, slothLines, greetings, "con el viaje terminado");

                // Se cierra: «El río sigue creciendo», con su texto y su ETA; sin frase de TiTino.
                map.BackButton.onClick.Invoke();
                Assert.That(map.Visible, Is.False);
                Assert.That(app.Screen, Is.EqualTo(AppScreen.ContentEnd));
                Assert.That(boot.Lobby.OverlayVisible, Is.True);
                Assert.That(boot.Lobby.OverlayTitle, Is.EqualTo("El río sigue creciendo"));
                Assert.That(boot.Lobby.OverlayTitle, Is.EqualTo(UiText.ContentEndTitle));
                Assert.That(boot.Flow.LevelNumber, Is.EqualTo(700), "no se carga ningún 701");

                // Un rato con la pantalla a la vista: TiTino, debajo, no saluda ni dice nada (el saludo espera al inicio).
                for (int t = 0; t < 2000; t += PlayModeHelpers.FrameMs)
                {
                    sloth.Tick(PlayModeHelpers.FrameMs);
                }

                AssertNoSlothLine(map, sloth, slothSession, slothLines, greetings, "con «El río sigue creciendo»");

                // Volver lleva al inicio; el Mapa no vuelve a salir solo.
                boot.Lobby.BackButton.onClick.Invoke();
                Assert.That(app.Screen, Is.EqualTo(AppScreen.Lobby));
                Assert.That(app.ContentEndMapDue, Is.False);
                Assert.That(map.Visible, Is.False);
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

            // La llegada de la victoria una vez (la de la franja, al cerrarse la victoria) si el 700 tiene parada de llegada en los
            // datos del Mapa (con las paradas 0-69 de M4.5b no la tiene: la última, la 69, se alcanza al ganar el 690), y después
            // el Mapa del fin de contenido.
            bool arrival = boot.RiverStops!.ArrivalOnWin(700) != null;
            Assert.That(lines.Count, Is.EqualTo(arrival ? 2 : 1), string.Join("\n", lines));
            if (arrival)
            {
                StringAssert.Contains("\"event\":\"stop_arrive\"", lines[0]);
                StringAssert.Contains("\"view\":\"victory\"", lines[0]);
            }

            string open = lines[lines.Count - 1];
            StringAssert.Contains("\"event\":\"map_open\"", open);
            StringAssert.Contains("\"source\":\"content_end\"", open);
            StringAssert.Contains("\"level\":701", open);
        }

        [UnityTest]
        public IEnumerator TheJourney_ATouchDoesNotSkipIt_VolverComesWhenTheCanoeArrives_AndLeavesTheScreen()
        {
            yield return FactLineTests.Win(700);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            NextToContentEnd(boot);
            Assert.That(map.Visible, Is.True);
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.JourneyLocked, Is.True, "D-053: «Volver» espera a que la canoa llegue");
            Assert.That(map.PlayShown, Is.False);
            Tick(map, 300);

            // Un toque no lo salta (D-053): como mucho, lo acelera; «Volver» y «←» siguen sin responder.
            Assert.That(map.TapJourney(), Is.True);
            Assert.That(map.CatchingUp, Is.True, "el toque no se salta el viaje");
            map.PlayButton.onClick.Invoke();
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.True, "sin «Volver» ni «←» durante el viaje");
            Tick(map, map.ContentEndJourneyMs);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.CatchUpCompletedByTouch, Is.False, "llegó sola, acelerada");
            Assert.That(map.CatchUpArrivals, Is.EqualTo(0), "tampoco al acelerarlo");
            Assert.That(map.PlayShown, Is.True);

            map.PlayButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False, "«Volver» cierra el Mapa");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoJourney_TheFinalStateFadesIn()
        {
            yield return FactLineTests.Win(700);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.ProgressReduceMotion = true;
            NextToContentEnd(boot);
            Assert.That(map.Visible, Is.True);
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.ContentEnd));
            Assert.That(map.ContentEndJourney, Is.True);
            Assert.That(map.CatchingUp, Is.False, "07 §7.8: sin viaje");
            Assert.That(map.CatchUpFromLevel, Is.EqualTo(690));
            Assert.That(map.CatchUpFading, Is.True, "el estado final entra con su fundido");
            Assert.That(map.CatchUpArrivals, Is.EqualTo(0));
            Tick(map, MapConfig.ReducedMotionFadeMs + 20);
            Assert.That(map.CatchUpFading, Is.False);
            Assert.That(boot.PressAndroidBack(), Is.True, "el Atrás de Android también lo cierra");
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator PlayFromTheLobby_WithTheContentOver_OpensTheScreenAlone()
        {
            // Sin el Reto del río en el build: con él desbloqueado, el botón es «Jugar · Reto del río» y abre el Reto (D-032,
            // punto 6; Fx16bTests).
            GameBootstrap.DailyPoolDisabledOverride = true;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 701;
            profile.Progress.MaxLevel = 700;
            profile.Progress.MapSeenLevel = 700;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.Lobby!.PlayLabel, Is.EqualTo(UiText.Play), "«Jugar» a secas, no «Jugar · Nivel 701» (D-032, punto 6)");
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(boot.App.ContentEndMapDue, Is.False, "solo al ganar el último nivel publicado");
            Assert.That(boot.RiverMapView!.Visible, Is.False);
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutTheOverride_OffInTests_TheScreenComesAlone()
        {
            GameBootstrap.ContentEndMapOverride = false;
            yield return FactLineTests.Win(700);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.ContentEndMapAvailable, Is.False);
            NextToContentEnd(boot);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(boot.RiverMapView!.Visible, Is.False, "como antes de M4.3");
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
        }

        // «Siguiente» en la victoria del 700 y los fotogramas de AD_GATE hasta que la app deja el nivel.
        private static void NextToContentEnd(GameBootstrap boot)
        {
            Assert.That(boot.PressNext(), Is.True);
            for (int elapsed = 0; elapsed < 10000 && boot.App!.Screen == AppScreen.Level; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }
        }

        // Sin frase de TiTino en el Mapa del fin de contenido ni en la pantalla de debajo (M4.3, punto 3): ni el bocadillo del Mapa,
        // ni el saludo o el Dato del perezoso de la ventanita de casa, ni ninguna línea más en la sesión de TiTino.
        private static void AssertNoSlothLine(RiverMapScreen map, HomeSloth sloth, SlothSession session, int lines, int greetings, string when)
        {
            Assert.That(map.L10WithBubble, Is.False, when + ": el Mapa sin bocadillo de TiTino");
            Assert.That(map.L10BubbleShown, Is.False, when);
            Assert.That(sloth.BubbleShown, Is.False, when + ": sin saludo en la ventanita");
            Assert.That(sloth.Note, Is.EqualTo(HomeSloth.HomeNote.None), when + ": sin mensajes de TiTino");
            Assert.That(sloth.GreetingDecisions, Is.EqualTo(greetings), when + ": el saludo del día no se decide");
            Assert.That(session.LinesThisSession, Is.EqualTo(lines), when + ": ninguna frase más de TiTino");
        }

        private static void Tick(RiverMapScreen map, int ms)
        {
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }
        }
    }
}
