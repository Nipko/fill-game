#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Analytics;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.RemoteConfig;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Cierre de B11 (nota <c>docs/dev/tutorial-b11.md</c> §3, «Para el integrador»; D-033, añadido de B11): <c>drag_pour_enabled</c>
    /// y <c>lvl_label_override</c> llegan de Remote Config al juego en el arranque (antes del catálogo), <c>af_label_shown</c> sale con
    /// cada intento de un nivel Difícil o Muy difícil con <c>source</c> = <c>override</c> o <c>pack</c> (08 §8.2), y todas las
    /// elecciones de Sin canoas van a <c>af_no_canoes_choice</c> (04 §11).
    /// </summary>
    public sealed class B11CierreTests
    {
        private FakeAnalyticsSink _sink = null!;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            _sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = _sink;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            GameScreen.DragPourOverride = null;
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetSessionOverrides();
            LevelLabelSource.JsonOverride = null;
            LaunchOptions.Reset();
            AF.RemoteConfig.RC.Reset();
        }

        [UnityTest]
        public IEnumerator RemoteConfig_DragAndLabels_ReachTheGame_AndTheRelabeledLevelSendsAfLabelShownOverride()
        {
            GameBootstrap.RemoteConfigBackendOverride = new FakeRemoteConfigBackend()
                .Set("drag_pour_enabled", "false")
                .Set("lvl_label_override", "{\"42\":\"hard\",\"10\":\"hard\"}")
                .StoreFetched();
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.RcSettings!.DragPourEnabled, Is.False);
            Assert.That(GameScreen.DragPourEnabled, Is.False, "drag_pour_enabled de Remote Config en la partida (C10)");
            LevelCatalog catalog = boot.App!.Catalog;
            Assert.That(catalog.IsRelabeled(42), Is.True, "lvl_label_override de Remote Config en el catálogo");
            Assert.That(catalog.Find(42)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));
            Assert.That(catalog.IsRelabeled(10), Is.False, "«hard» en el 10 se descarta (lvl_labels_from_level = 40)");
            Assert.That(string.Join(" | ", boot.RcSettings.Errors), Does.Contain("lvl_labels_from_level"), "y se avisa");

            yield return StartAttempt(boot);
            AnalyticsEvent shown = _sink.Last("af_label_shown")!;
            Assert.That(shown.Find("label")!.Value.Text, Is.EqualTo("hard"));
            Assert.That(shown.Find("source")!.Value.Text, Is.EqualTo("override"));
            Assert.That(shown.Find("level")!.Value.Long, Is.EqualTo(42), "con los comunes de 08 §8.1");
            var names = new List<string>(_sink.Names);
            Assert.That(names.IndexOf("af_label_shown"), Is.EqualTo(names.IndexOf("level_start") + 1), "justo tras level_start");
            Assert.That(boot.Analytics!.Violations, Is.Empty);
        }

        [UnityTest]
        public IEnumerator APackLabel_SendsAfLabelShownPack_AndANormalLevelNothing()
        {
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(GameScreen.DragPourEnabled, Is.True, "08 §9: drag_pour_enabled = true");
            Assert.That(boot.App!.Catalog.Find(40)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard), "el 40 es Difícil en su paquete");
            yield return StartAttempt(boot);
            AnalyticsEvent shown = _sink.Last("af_label_shown")!;
            Assert.That(shown.Find("label")!.Value.Text, Is.EqualTo("hard"));
            Assert.That(shown.Find("source")!.Value.Text, Is.EqualTo("pack"));
            Assert.That(boot.Analytics!.Violations, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ANormalLevel_SendsNoAfLabelShown()
        {
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Catalog.Find(41)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelNormal));
            yield return StartAttempt(boot);
            Assert.That(_sink.Last("af_label_shown"), Is.Null, "sin etiqueta a la vista no hay af_label_shown");
        }

        [UnityTest]
        public IEnumerator NoCanoes_WaitByTheX_AndByBack_AndInfinite_ReachTheAnalytics()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            NoCanoesPanel panel = boot.Lobby!.NoCanoes;

            boot.Lobby.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            panel.CloseButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            AssertChoice(boot, GameBootstrap.NoCanoesChoiceWait, 1);

            boot.Lobby.CanoesButton.onClick.Invoke();
            Assert.That(boot.PressBack(), Is.True, "Atrás cierra Sin canoas como su X");
            AssertChoice(boot, GameBootstrap.NoCanoesChoiceWait, 2);

            Assert.That(boot.PressNoCanoesClose(), Is.False, "sin Sin canoas delante, nada");
            Assert.That(boot.NoCanoesChoiceCount, Is.EqualTo(2), "ni se apunta");

            boot.Lobby.CanoesButton.onClick.Invoke();
            Assert.That(panel.InfiniteButton.gameObject.activeInHierarchy, Is.True, "Canoas infinitas 1 h a la vista");
            panel.InfiniteButton.onClick.Invoke();
            AssertChoice(boot, GameBootstrap.NoCanoesChoiceInfinite, 3);
            Assert.That(boot.Analytics!.Violations, Is.Empty);
        }

        [UnityTest]
        public IEnumerator NoCanoes_SeedsRefill_ReachesTheAnalytics()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Lobby!.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            boot.Lobby.NoCanoes.RefillButton.onClick.Invoke();
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5), "Llenar canoas, cobrado");
            AssertChoice(boot, GameBootstrap.NoCanoesChoiceRefill, 1);
            Assert.That(boot.PressRefillCanoes(), Is.False, "ya llenas: nada que elegir");
            Assert.That(boot.NoCanoesChoiceCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NoCanoes_Video_ReachesTheAnalytics()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            NoCanoesPanel panel = boot.Lobby!.NoCanoes;
            boot.Lobby.CanoesButton.onClick.Invoke();
            Assert.That(panel.VideoShown, Is.True, "con anuncio, «Canoa extra · Ver video»");
            panel.VideoButton.onClick.Invoke();
            AssertChoice(boot, GameBootstrap.NoCanoesChoiceAd, 1);
            yield return PlayModeHelpers.WaitFor(() => boot.Wallet!.Canoes.Count == 1, "la canoa del vídeo");

            Assert.That(boot.NoCanoesChoiceCount, Is.EqualTo(1), "la recompensa del vídeo no es otra elección");
        }

        [UnityTest]
        public IEnumerator NoCanoes_TheMapLink_ReachesTheAnalytics()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Lobby!.CanoesButton.onClick.Invoke();
            boot.Lobby.NoCanoes.HomeButton.onClick.Invoke(); // «Volver al Mapa del río», por IGameActions.PressNoCanoesMap
            Assert.That(boot.NoCanoesMapOpened, Is.True);
            AssertChoice(boot, GameBootstrap.NoCanoesMapChoice, 1);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.NoCanoesChoiceCount, Is.EqualTo(1), "cerrar el Mapa no es otra elección");
        }

        [UnityTest]
        public IEnumerator NoCanoes_FromLevelFailed_EachChoiceIsSentOnce([Values("wait", "ad", "seeds_refill", "iap_infinite")] string choice)
        {
            // Sin canoas dentro del nivel (Reintentar con 0 canoas; 04 §7.3): cada elección, una af_no_canoes_choice, y la salida
            // sola del flujo cuando vuelven las canoas no añade un «wait» de más.
            LaunchOptions.LevelOffsetOverride = 25; // el 31 juega el tablero 6 del tutorial (FunnelSetup.GetStuck)
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            NoCanoesPanel panel = boot.View!.NoCanoes;
            switch (choice)
            {
                case "wait":
                    panel.CloseButton.onClick.Invoke();
                    Assert.That(boot.Flow.Phase, Is.Not.EqualTo(FlowPhase.NoCanoes));
                    break;
                case "ad":
                    Assert.That(panel.VideoShown, Is.True);
                    panel.VideoButton.onClick.Invoke();
                    break;
                case "seeds_refill":
                    panel.RefillButton.onClick.Invoke();
                    Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5));
                    break;
                default:
                    panel.InfiniteButton.onClick.Invoke();
                    break;
            }

            for (int i = 0; i < 30; i++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                yield return null;
            }

            AssertChoice(boot, choice, 1);
            Assert.That(boot.Analytics!.Violations, Is.Empty);
        }

        // Termina la intro (con toques, como el jugador) hasta que empieza el intento (level_start, 08 §8.2).
        private IEnumerator StartAttempt(GameBootstrap boot)
        {
            for (int i = 0; i < 600 && _sink.Last("level_start") == null; i++)
            {
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                yield return null;
            }

            Assert.That(_sink.Last("level_start"), Is.Not.Null, "empezó el intento: " + string.Join(",", _sink.Names));
        }

        private void AssertChoice(GameBootstrap boot, string choice, int count)
        {
            Assert.That(boot.LastNoCanoesChoice, Is.EqualTo(choice));
            Assert.That(boot.NoCanoesChoiceCount, Is.EqualTo(count));
            int sent = 0;
            foreach (AnalyticsEvent e in _sink.Events)
            {
                if (e.Name == "af_no_canoes_choice")
                {
                    sent++;
                }
            }

            Assert.That(sent, Is.EqualTo(count), "una af_no_canoes_choice por elección");
            Assert.That(_sink.Last("af_no_canoes_choice")!.Find("choice")!.Value.Text, Is.EqualTo(choice));
        }

        private static Profile NoCanoesProfile(int maxLevel)
        {
            Profile profile = FunnelSetup.Profile(500, 0);
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }
    }
}
