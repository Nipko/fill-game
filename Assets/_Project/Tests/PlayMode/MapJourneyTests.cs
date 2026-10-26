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

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Mapa que se abre con viaje (D-053, petición del dueño del 2026-10-26; 06 §2.3 y §4.5; RiverMapScreen.Journey.cs), en la
    /// escena <c>Game</c> real, con «Ver mapa» en la victoria del 20 (Alter do Chão): el viaje del tramo 10 → 20 se repite aunque la
    /// franja ya anotara la llegada; la canoa se mueve en ≤ 300 ms; «Volver» y «←» no están ni responden hasta que llega; el toque
    /// no salta el viaje (como mucho lo acelera); el Atrás de Android lo cierra; con Reducir movimiento no hay viaje y los botones
    /// están desde el principio; al volver de segundo plano salta al final; TalkBack anuncia la llegada; las sondas
    /// <c>AF_LAYOUT</c> (solo <c>map_journey</c> durante el viaje) y <c>map_play_ready</c>. El Mapa de la pestaña sigue como antes.
    /// El del 10 y el del fin de contenido, en <c>Level10MapTests</c> y <c>ContentEndMapTests</c>.
    /// </summary>
    public sealed class MapJourneyTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.A11yHostOverride = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        // Gana el 20 y toca «Ver mapa» (el Mapa encima de la victoria, con el viaje del tramo).
        internal static IEnumerator OpenFromVictory20(Action<GameBootstrap>? before = null)
        {
            yield return VictoryArrivalTests.Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            before?.Invoke(boot);
            PlayModeHelpers.Step(boot, 900);
            Assert.That(boot.View!.VictoryMapShown, Is.True);
            boot.View.VictoryMapButton.onClick.Invoke();
            Assert.That(boot.RiverMapView!.Visible, Is.True);
            Assert.That(boot.RiverMapView.Source, Is.EqualTo(MapOpenSource.Victory));
        }

        [UnityTest]
        public IEnumerator VerMapa_ReplaysTheLeg_MovesWithin300ms_AndTheButtonsWaitForTheCanoe()
        {
            using var layout = new AfLayoutLog();
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_play_ready\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            Application.logMessageReceived += OnLog;
            try
            {
                yield return OpenFromVictory20();
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                Assert.That(map.ReplayJourney, Is.True);
                Assert.That(map.ReplayJourneyFromLevel, Is.EqualTo(10));
                Assert.That(map.ContentEndJourney, Is.False);
                Assert.That(map.CatchingUp, Is.True, "D-053: la canoa repite el tramo 10 → 20");
                Assert.That(map.CatchUpFromLevel, Is.EqualTo(10));
                Assert.That(map.CatchUpToLevel, Is.EqualTo(20));
                Assert.That(map.CanoeInView, Is.True, "la cámara ya tiene la canoa");
                Assert.That(map.JourneyOpened, Is.True);
                Assert.That(map.JourneyLocked, Is.True);
                Assert.That(map.PlayShown, Is.False, "«Volver» espera a que Capi llegue");
                Assert.That(map.BackShown, Is.False);
                Assert.That(map.PlayButton.gameObject.activeInHierarchy, Is.False);
                Assert.That(map.BackButton.gameObject.activeInHierarchy, Is.False);
                if (DevProbe.Enabled)
                {
                    string line = layout.RequireLast("river_map", 21, "AF_LAYOUT del Mapa con viaje");
                    Assert.That(AfLayoutLog.ButtonIds(line), Is.EqualTo(new[] { "map_journey" }), "durante el viaje, solo el área del viaje");
                }

                // Se mueve en ≤ 300 ms (antes, en la victoria, la canoa no se movía: ya estaba en Alter do Chão).
                int moved = -1;
                for (int ms = 0; ms <= 400 && moved < 0; ms += PlayModeHelpers.FrameMs)
                {
                    map.Tick(PlayModeHelpers.FrameMs);
                    if (map.CatchUpElapsedMs > 0f)
                    {
                        moved = ms + PlayModeHelpers.FrameMs;
                    }
                }

                Assert.That(moved, Is.GreaterThan(0).And.LessThanOrEqualTo(300), "la canoa sale en ≤ 300 ms");
                Assert.That(map.JourneyFirstMoveMs, Is.LessThanOrEqualTo(300f));

                // Los botones no responden aunque alguien los invoque, y el toque no salta el viaje.
                map.PlayButton.onClick.Invoke();
                map.BackButton.onClick.Invoke();
                Assert.That(map.Visible, Is.True, "ni «Volver» ni «←» durante el viaje");
                Assert.That(map.JourneyTouchArea!.gameObject.activeInHierarchy, Is.True);
                Assert.That(map.CatchingUp, Is.True);
                Assert.That(lines, Is.Empty, "map_play_ready, solo al llegar");
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }

        [UnityTest]
        public IEnumerator ATouchOnlySpeedsItUp_TheButtonsEnterWhenTheCanoeArrives()
        {
            using var layout = new AfLayoutLog();
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_play_ready\"", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            Application.logMessageReceived += OnLog;
            try
            {
                yield return OpenFromVictory20();
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                int duration = map.CatchUpDurationMs;
                Assert.That(duration, Is.EqualTo(RiverJourney.CatchUpMs(10, 20)));
                Level10MapTests.Tick(map, 100);

                // En la anticipación, un toque hace salir la canoa ya; remando, la acelera. Nunca la salta.
                map.JourneyTouchArea!.onClick.Invoke();
                Assert.That(map.JourneyTaps, Is.EqualTo(1));
                Level10MapTests.Tick(map, 400);
                Assert.That(map.CatchingUp, Is.True, "el toque no se salta el viaje");
                Assert.That(map.CatchUpElapsedMs, Is.GreaterThan(600f), "acelerado");
                Assert.That(map.CatchUpCompletedByTouch, Is.False);
                Assert.That(map.PlayShown, Is.False);
                layout.Clear();

                // Llega: «Volver» y «←» entran con su rebote, AF_LAYOUT los publica y map_play_ready lo dice.
                Level10MapTests.Tick(map, duration);
                Assert.That(map.CatchingUp, Is.False);
                Assert.That(map.JourneyLocked, Is.False);
                Assert.That(map.JourneyEndedBy, Is.EqualTo("time"));
                Assert.That(map.CatchUpArrivals, Is.EqualTo(0), "la llegada ya se vio en la victoria: sin stop_arrive");
                Assert.That(map.PlayShown, Is.True);
                Assert.That(map.BackShown, Is.True);
                Assert.That(map.JourneyReadyMs, Is.GreaterThan(map.JourneyFirstMoveMs));
                if (DevProbe.Enabled)
                {
                    string line = layout.RequireLast("river_map", 21, "AF_LAYOUT al llegar la canoa");
                    string[] ids = AfLayoutLog.ButtonIds(line);
                    Assert.That(ids, Does.Contain("play"));
                    Assert.That(ids, Does.Contain("back"));
                    Assert.That(ids, Does.Not.Contain("map_journey"));
                    Assert.That(lines.Count, Is.EqualTo(1), string.Join("\n", lines));
                    StringAssert.Contains("\"source\":\"victory\"", lines[0]);
                    StringAssert.Contains("\"to\":20", lines[0]);
                    StringAssert.Contains("\"taps\":1", lines[0]);
                    StringAssert.Contains("\"by\":\"time\"", lines[0]);
                }

                // «Volver» devuelve a la victoria, tal cual.
                Level10MapTests.Tick(map, (int)RiverMapScreen.JourneyButtonInMs + 50);
                Assert.That(map.PlayAlpha, Is.EqualTo(1f).Within(1e-4f));
                map.PlayButton.onClick.Invoke();
                Assert.That(map.Visible, Is.False);
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }

        [UnityTest]
        public IEnumerator TheButtonsEnterWithAFadeAndABounce()
        {
            yield return OpenFromVictory20();
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Level10MapTests.Tick(map, RiverMapScreen.JourneyLeadMs + map.CatchUpDurationMs + PlayModeHelpers.FrameMs);
            Assert.That(map.PlayShown, Is.True);
            Assert.That(map.PlayAlpha, Is.LessThan(1f), "entra con su fundido");
            Level10MapTests.Tick(map, (int)RiverMapScreen.JourneyButtonInMs);
            Assert.That(map.PlayAlpha, Is.EqualTo(1f).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator AndroidBack_MidJourney_ClosesTheMap_BackToTheVictory()
        {
            yield return OpenFromVictory20();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Level10MapTests.Tick(map, 500);
            Assert.That(map.JourneyLocked, Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás nunca se bloquea");
            Assert.That(map.Visible, Is.False);
            Assert.That(map.JourneyEndedBy, Is.EqualTo("close"));
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue ahí");
            Assert.That(boot.View!.NextButton.interactable, Is.True, "y se puede seguir");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoJourney_TheButtonsAreThereAtOnce()
        {
            yield return OpenFromVictory20(b => b.RiverMapView!.ProgressReduceMotion = true);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.CatchingUp, Is.False, "07 §7.8: sin viaje (el fundido)");
            Assert.That(map.JourneyLocked, Is.False);
            Assert.That(map.PlayShown, Is.True);
            Assert.That(map.BackShown, Is.True);
            Assert.That(map.PlayAlpha, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator BackFromBackground_MidJourney_SnapsToTheArrival()
        {
            yield return OpenFromVictory20();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Level10MapTests.Tick(map, 600);
            Assert.That(map.CatchingUp, Is.True);
            boot.gameObject.SendMessage("OnApplicationPause", true);
            boot.gameObject.SendMessage("OnApplicationPause", false);
            Assert.That(map.CatchingUp, Is.False, "02 §14: salta al final");
            Assert.That(map.JourneyLocked, Is.False);
            Assert.That(map.JourneyEndedBy, Is.EqualTo("resume"));
            Assert.That(map.PlayShown, Is.True);
            Assert.That(map.Visible, Is.True, "el Mapa sigue abierto");
        }

        [UnityTest]
        public IEnumerator TalkBack_AnnouncesTheArrival_AndTheButton()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return OpenFromVictory20();
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Level10MapTests.Tick(map, RiverMapScreen.JourneyLeadMs + map.CatchUpDurationMs + PlayModeHelpers.FrameMs);
            Assert.That(map.PlayShown, Is.True);
            Assert.That(host.Announcements.Count, Is.GreaterThan(0));
            string said = host.Announcements[host.Announcements.Count - 1];
            StringAssert.Contains(map.PlayLabel, said, "el botón que ya se puede tocar");
            RiverStop stop = GameBootstrap.Current.RiverStops!.At(2)!;
            string place = UiText.StopName(stop);
            if (place.Length > 0 && UiText.StopCountry(stop).Length > 0 && UiText.StopRiver(stop).Length > 0)
            {
                StringAssert.Contains(place, said, "y adónde llegó");
            }
        }

        [UnityTest]
        public IEnumerator FromTheLobbyTab_NoJourneyLock_ATouchStillCompletesTheCatchUp()
        {
            Profile profile = Level10MapTests.ProfileWonTen();
            profile.Progress.MaxLevel = 58;
            profile.Progress.NextLevel = 59;
            profile.Progress.MapSeenLevel = 52;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CatchingUp, Is.True, "ponerse al día de 52 a 58");
            Assert.That(map.JourneyOpened, Is.False, "la pestaña no bloquea «Jugar» (D-053 solo vale con viaje)");
            Assert.That(map.PlayShown, Is.True);
            Assert.That(map.BackShown, Is.True);
            Assert.That(map.CatchUpTouch(), Is.True, "un toque lo completa, como antes");
            Assert.That(map.CatchingUp, Is.False);
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Level10_TheCanoeMovesWithin300ms_EvenWithoutTiTinosVoice()
        {
            foreach (bool voice in new[] { true, false })
            {
                GameBootstrap.L10SequenceOverride = true;
                try
                {
                    Profile profile = Level10MapTests.ProfileWonTen();
                    profile.Settings.SlothMessages = voice;
                    yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                    GameBootstrap boot = GameBootstrap.Current!;
                    PlayModeHelpers.TakeClock(boot);
                    RiverMapScreen map = boot.RiverMapView!;
                    Assert.That(map.L10SequenceActive, Is.True);
                    Assert.That(map.PlayShown, Is.False, "voz " + voice);
                    Level10MapTests.Tick(map, 300);
                    Assert.That(map.JourneyFirstMoveMs, Is.GreaterThan(0f).And.LessThanOrEqualTo(300f), "voz " + voice);
                    Level10MapTests.Tick(map, map.CatchUpDurationMs);
                    Assert.That(map.PlayShown, Is.True, "voz " + voice + ": Capi llegó a Santarém");
                    Assert.That(map.PlayLabel, Is.EqualTo(UiText.PlayLevel(11)));
                    Debug.Log("AF_MAP journey l10 voz " + voice + ": primer movimiento "
                        + map.JourneyFirstMoveMs.ToString("0", CultureInfo.InvariantCulture) + " ms, botón a los "
                        + map.JourneyReadyMs.ToString("0", CultureInfo.InvariantCulture) + " ms");
                }
                finally
                {
                    GameBootstrap.L10SequenceOverride = null;
                }
            }
        }
    }

    /// <summary>
    /// Capturas del Mapa con viaje (D-053): el primer fotograma, la mitad del viaje y la llegada con el botón, en el Mapa del 10
    /// (<c>53a</c>-<c>53c</c>) y en «Ver mapa» de la victoria del 20 (<c>53d</c>-<c>53f</c>). Explícito: necesita gráficos
    /// (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MapJourneySnapshotTests</c> sin
    /// <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del Mapa con viaje (D-053); necesitan dispositivo gráfico.")]
    public sealed class MapJourneySnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
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
        public IEnumerator CaptureTheLevel10Journey()
        {
            GameBootstrap.L10SequenceOverride = true;
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(Level10MapTests.ProfileWonTen()));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Level10MapTests.Tick(map, PlayModeHelpers.FrameMs);
            yield return Save(dir, "53a_journey_l10_inicio", map);
            Level10MapTests.Tick(map, RiverMapScreen.JourneyLeadMs + (map.CatchUpDurationMs / 2));
            yield return Save(dir, "53b_journey_l10_mitad", map);
            Level10MapTests.Tick(map, map.CatchUpDurationMs);
            Level10MapTests.Tick(map, (int)RiverMapScreen.JourneyButtonInMs + 50);
            yield return Save(dir, "53c_journey_l10_llegada", map);
        }

        [UnityTest]
        public IEnumerator CaptureTheVictoryJourney()
        {
            string dir = SnapshotKit.Prepare();
            yield return MapJourneyTests.OpenFromVictory20();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachOverlay(boot.View!.Canvas);
            SnapshotKit.AttachOverlay(map.Canvas);
            Level10MapTests.Tick(map, PlayModeHelpers.FrameMs);
            yield return Save(dir, "53d_journey_victoria_inicio", map);
            Level10MapTests.Tick(map, RiverMapScreen.JourneyLeadMs + (map.CatchUpDurationMs / 2));
            yield return Save(dir, "53e_journey_victoria_mitad", map);
            Level10MapTests.Tick(map, map.CatchUpDurationMs);
            Level10MapTests.Tick(map, (int)RiverMapScreen.JourneyButtonInMs + 50);
            yield return Save(dir, "53f_journey_victoria_llegada", map);
        }

        private static IEnumerator Save(string dir, string name, RiverMapScreen map)
        {
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_MAP " + name + ": viaje " + map.CatchUpElapsedMs.ToString("0", CultureInfo.InvariantCulture) + "/"
                + map.CatchUpDurationMs + " ms, bloqueado " + map.JourneyLocked + ", «" + map.PlayLabel + "» " + map.PlayShown
                + ", canoa " + map.CanoeInViewport + ", vista " + map.View);
        }
    }
}
