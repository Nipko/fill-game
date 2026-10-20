#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Cierre de la pista B de extremo a extremo (tarea B2-6), leyendo las líneas <c>AF_STATE</c> que escribe la sonda de
    /// desarrollo como las leería <c>adb logcat</c> en el teléfono: en el 31 con 1 canoa, stuck → giveup_confirm →
    /// level_failed → no_canoes → lobby, siempre con semillas y canoas; una victoria desde el 22 con su anuncio simulado
    /// (victory → ad_gate → ad_check → ad → ad_closed → nivel siguiente); una compra simulada (purchase_sim) con el saldo; y
    /// el nivel reabierto tras cerrar la app (resume). Y las líneas <c>AF_LAYOUT</c> de los modales del embudo (T-B de la prueba
    /// del 2026-10-09): giveup_confirm, level_failed y no_canoes con sus ids al abrirse, no_canoes otra vez cuando rehace su
    /// layout, y tocar sus centros publicados (como <c>adb shell input tap</c>) da el AF_STATE esperado.
    /// </summary>
    public sealed class FunnelPlayModeTests
    {
        private readonly List<string> _lines = new List<string>();
        private AfLayoutLog _layout = null!;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
            _layout = new AfLayoutLog();
        }

        [TearDown]
        public void TearDown()
        {
            _layout?.Dispose();
            _layout = null!;
            Application.logMessageReceived -= OnLog;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level31_WithOneCanoe_StuckGiveUpLevelFailedNoCanoesLobby()
        {
            Assert.That(DevProbe.Enabled, Is.True);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(500, canoes: 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            _lines.Clear();

            FunnelSetup.GetStuck(boot);
            GameScreen view = boot.View!;
            view.Stuck.GiveUpButton.onClick.Invoke();
            string giveUp = _layout.RequireLast("giveup_confirm", 31, "¿Seguro?");
            Assert.That(AfLayoutLog.ButtonIds(giveUp), Is.EqualTo(new[] { "close", "rescue", "give_up" }), "T-B: ¿Seguro? publica su AF_LAYOUT");
            AfLayoutLog.AssertCenterInside(giveUp, "close", (RectTransform)view.GiveUp.CloseButton.transform);
            AfLayoutLog.AssertCenterInside(giveUp, "rescue", (RectTransform)view.GiveUp.RescueButton.transform);
            AfLayoutLog.AssertCenterInside(giveUp, "give_up", (RectTransform)view.GiveUp.GiveUpButton.transform);
            view.GiveUp.GiveUpButton.onClick.Invoke();
            string failed = _layout.RequireLast("level_failed", 31, "Nivel fallido");
            Assert.That(AfLayoutLog.ButtonIds(failed), Is.EqualTo(new[] { "retry", "close" }), "T-B: Nivel fallido publica su AF_LAYOUT");
            AfLayoutLog.AssertCenterInside(failed, "retry", (RectTransform)view.Failed.RetryButton.transform);
            AfLayoutLog.AssertCenterInside(failed, "close", (RectTransform)view.Failed.CloseButton.transform);
            view.Failed.RetryButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            AssertNoCanoesLayout(view, "T-B: Sin canoas publica su AF_LAYOUT");
            view.NoCanoes.HomeButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));

            string[] sequence = Events("stuck_modal", "giveup_confirm", "level_failed", "no_canoes", "lobby");
            Assert.That(sequence, Is.EqualTo(new[] { "stuck_modal", "giveup_confirm", "level_failed", "no_canoes", "lobby" }));
            AssertBalance("stuck_modal", 500, 1);
            AssertBalance("giveup_confirm", 500, 1);
            AssertBalance("level_failed", 500, 0);
            AssertBalance("no_canoes", 500, 0);
            AssertBalance("lobby", 500, 0);
            Assert.That(Line("level_failed"), Does.Contain("\"level\":31"));
            Assert.That(Has("fail_hard_canoe"), Is.True, "la rendición confirmada cobra la canoa");
            Assert.That(Has("ad"), Is.False, "ningún anuncio en el embudo de fallo (05 §10)");
        }

        [UnityTest]
        public IEnumerator From22_AVictory_WithItsSimulatedInterstitial()
        {
            LaunchOptions.LevelOffsetOverride = 21;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(40, levelsSinceInterstitial: 2));
            yield return PlayModeHelpers.LoadGame(22, new MemoryProgressStore(22, 21));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(200));
            _lines.Clear();
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            boot.View!.NextButton.onClick.Invoke();
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(23));

            string[] sequence = Events("win", "victory", "ad_gate", "ad_check", "ad", "ad_closed", "load");
            Assert.That(sequence, Is.EqualTo(new[] { "win", "victory", "ad_gate", "ad_check", "ad", "ad_closed", "load" }));
            AssertBalance("victory", 60, 5);
            Assert.That(Line("victory"), Does.Contain("\"harvest\":20"));
            Assert.That(Line("ad_check"), Does.Contain("\"decision\":\"allowed\""));
            Assert.That(Line("ad"), Does.Contain("\"format\":\"interstitial\""));
            Assert.That(Line("ad"), Does.Contain("\"placement\":\"interstitial_post_level\""));
            Assert.That(Line("ad"), Does.Contain("\"level\":22"));
            AssertBalance("ad", 60, 5);
            Assert.That(Line("load"), Does.Contain("\"level\":23"));
        }

        [UnityTest]
        public IEnumerator APurchase_AndTheResumedLevel_WriteTheirLinesWithTheBalance()
        {
            var store = new MemoryProfileStore(VictorySetup.Profile(0));
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            _lines.Clear();
            boot.View!.Stuck.HandfulButton.onClick.Invoke();
            Assert.That(Line("purchase_sim"), Does.Contain("\"sku\":\"af_coins_2\""));
            Assert.That(Line("purchase_sim"), Does.Contain("\"status\":\"Purchased\""));
            AssertBalance("purchase_sim", 1000, 5);

            _lines.Clear();
            yield return PlayModeHelpers.LoadApp(store);
            Assert.That(Line("resume"), Does.Contain("\"phase\":\"STUCK_MODAL\""));
            AssertBalance("resume", 1000, 5);
            Assert.That(Has("load"), Is.True, "LOADING del mismo nivel");
            Assert.That(Has("ready"), Is.False, "sin intro ni tablero nuevo: el modal reabierto");
        }

        [UnityTest]
        public IEnumerator Level31_TappingThePublishedCenters_GivesTheExpectedStates()
        {
            // T-B (la prueba del teléfono, en el Editor): en el 31 con 1 canoa y 100 semillas se toca cada botón en el centro que
            // publicó la última AF_LAYOUT, como adb shell input tap (AfLayoutLog.Tap: la prueba de impacto del EventSystem en ese
            // punto tiene que dar el botón, sin nada encima): Rendirse en Aguas estancadas y en ¿Seguro? (give_up →
            // fail_hard_canoe y level_failed) y Reintentar con 0 canoas (retry → no_canoes). Sin canoas ofrece «Llenar canoas»
            // por 200 con «Te faltan 100»; al llegar las semillas que faltan, rehace su layout (el aviso se va y lo de debajo
            // sube) y vuelve a publicar su AF_LAYOUT, una sola vez.
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100, canoes: 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            GameScreen view = boot.View!;
            _lines.Clear();

            yield return AfLayoutLog.Tap(_layout.RequireLast("stuck", 31, "Aguas estancadas"), "give_up", view.Stuck.GiveUpButton);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(Has("giveup_confirm"), Is.True);

            yield return AfLayoutLog.Tap(_layout.RequireLast("giveup_confirm", 31, "¿Seguro?"), "give_up", view.GiveUp.GiveUpButton);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(Events("fail_hard_canoe", "level_failed"), Is.EqualTo(new[] { "fail_hard_canoe", "level_failed" }), "give_up → fail_hard_canoe y level_failed");
            AssertBalance("level_failed", 100, 0);

            yield return AfLayoutLog.Tap(_layout.RequireLast("level_failed", 31, "Nivel fallido"), "retry", view.Failed.RetryButton);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes), "retry con 0 canoas → no_canoes");
            Assert.That(Has("no_canoes"), Is.True);
            AssertNoCanoesLayout(view, "Sin canoas");
            string before = _layout.RequireLast("no_canoes", 31, "Sin canoas");
            Assert.That(AfLayoutLog.ButtonIds(before), Does.Contain("refill_canoes"), "Llenar canoas, sin saldo");
            int count = _layout.AllOf("no_canoes", 31).Count;
            PlayModeHelpers.Step(boot, 100);
            Assert.That(_layout.AllOf("no_canoes", 31).Count, Is.EqualTo(count), "sin cambios, sin AF_LAYOUT nueva");

            // T-B: Sin canoas rehace su layout y vuelve a publicarlo.
            Assert.That(boot.Wallet!.Apply(Transaction.Earn(SeedSource.Refund, 100)).IsApplied, Is.True);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(_layout.AllOf("no_canoes", 31).Count, Is.EqualTo(count + 1), "una AF_LAYOUT no_canoes nueva al rehacer el layout");
            string after = _layout.RequireLast("no_canoes", 31, "Sin canoas rehecha");
            Assert.That(AfLayoutLog.Button(after, "home")!.Value.y, Is.LessThan(AfLayoutLog.Button(before, "home")!.Value.y), "sin «Te faltan», «Volver al inicio» sube");
            AssertNoCanoesLayout(view, "Sin canoas rehecha");
            yield return AfLayoutLog.Tap(after, "home", view.NoCanoes.HomeButton);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        // La última AF_LAYOUT es la de Sin canoas del 31, con un id por cada botón que se ve (close, extra_canoe_video,
        // refill_canoes, infinite_canoes y daily_challenge (E7) si se ofrecen, y home), en ese orden, y sus centros caen en lo que se ve.
        private void AssertNoCanoesLayout(GameScreen view, string what)
        {
            string line = _layout.RequireLast("no_canoes", 31, what);
            NoCanoesPanel panel = view.NoCanoes;
            var expected = new List<string> { "close" };
            var buttons = new List<UnityEngine.UI.Button> { panel.CloseButton };
            if (panel.VideoButton.gameObject.activeInHierarchy)
            {
                expected.Add("extra_canoe_video");
                buttons.Add(panel.VideoButton);
            }

            if (panel.RefillButton.gameObject.activeInHierarchy)
            {
                expected.Add("refill_canoes");
                buttons.Add(panel.RefillButton);
            }

            if (panel.InfiniteButton.gameObject.activeInHierarchy)
            {
                expected.Add("infinite_canoes");
                buttons.Add(panel.InfiniteButton);
            }

            if (panel.DailyLink.Visible)
            {
                expected.Add(NoCanoesDailyLink.LayoutId); // E7: «Mientras esperas: Jugar el Reto del río» (04 §7.3)
                buttons.Add(panel.DailyLink.Button);
            }

            expected.Add("home");
            buttons.Add(panel.HomeButton);
            Assert.That(AfLayoutLog.ButtonIds(line), Is.EqualTo(expected.ToArray()), what);
            for (int i = 0; i < expected.Count; i++)
            {
                AfLayoutLog.AssertCenterInside(line, expected[i], (RectTransform)buttons[i].transform);
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_STATE ", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        // Eventos de las líneas capturadas que están en wanted, en orden y sin repetir el mismo evento seguido.
        private string[] Events(params string[] wanted)
        {
            var set = new HashSet<string>(wanted);
            var result = new List<string>();
            for (int i = 0; i < _lines.Count; i++)
            {
                string evt = EventOf(_lines[i]);
                if (set.Contains(evt) && (result.Count == 0 || result[result.Count - 1] != evt))
                {
                    result.Add(evt);
                }
            }

            return result.ToArray();
        }

        private bool Has(string evt)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (EventOf(_lines[i]) == evt)
                {
                    return true;
                }
            }

            return false;
        }

        private string Line(string evt)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (EventOf(_lines[i]) == evt)
                {
                    return _lines[i];
                }
            }

            Assert.Fail("Falta la línea AF_STATE " + evt + " en: " + string.Join("\n", _lines));
            return string.Empty;
        }

        private void AssertBalance(string evt, int seeds, int canoes)
        {
            string line = Line(evt);
            Assert.That(line, Does.Contain("\"seeds\":" + seeds), evt + ": " + line);
            Assert.That(line, Does.Contain("\"canoes\":" + canoes), evt + ": " + line);
        }

        private static string EventOf(string line)
        {
            const string key = "{\"event\":\"";
            int start = line.IndexOf(key, StringComparison.Ordinal);
            if (start < 0)
            {
                return string.Empty;
            }

            start += key.Length;
            int end = line.IndexOf('"', start);
            return end < 0 ? string.Empty : line.Substring(start, end - start);
        }
    }
}
