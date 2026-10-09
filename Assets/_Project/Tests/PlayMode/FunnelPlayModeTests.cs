#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// el nivel reabierto tras cerrar la app (resume).
    /// </summary>
    public sealed class FunnelPlayModeTests
    {
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
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
            boot.View!.Stuck.GiveUpButton.onClick.Invoke();
            boot.View.GiveUp.GiveUpButton.onClick.Invoke();
            boot.View.Failed.RetryButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            boot.View.NoCanoes.HomeButton.onClick.Invoke();
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
