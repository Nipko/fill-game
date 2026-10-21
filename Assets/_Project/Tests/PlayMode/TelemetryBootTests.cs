#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Analytics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.RemoteConfig;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El arranque con Remote Config y analítica (bloque B7, parche B7-bootstrap): la caché de Remote Config llega a la cartera,
    /// y un nivel ganado en la escena real sale por la analítica (<c>level_start</c>, <c>earn_virtual_currency</c>,
    /// <c>af_level_win</c>, <c>level_end</c>), con los parámetros comunes y la línea <c>AF_EVENT</c> del sumidero de logcat.
    /// </summary>
    public sealed class TelemetryBootTests
    {
        private readonly List<string> _lines = new List<string>();
        private FakeAnalyticsSink _sink = null!;

        [SetUp]
        public void SetUp()
        {
            _sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = _sink;
            GameBootstrap.RemoteConfigBackendOverride = new FakeRemoteConfigBackend().Set("eco_reward_normal", "25").StoreFetched();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetSessionOverrides();
            LaunchOptions.Reset();
            AF.RemoteConfig.RC.Reset();
        }

        [UnityTest]
        public IEnumerator TheBoot_ReadsRemoteConfig_AndAWonLevelReachesTheAnalytics()
        {
            yield return PlayModeHelpers.LoadGame(3, new MemoryProgressStore(3, 2));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Analytics, Is.Not.Null);
            Assert.That(boot.Rc!.Source, Is.EqualTo(RcSource.Cached), "08 §2.3: la caché se activa al arrancar");
            Assert.That(boot.RcSettings!.Errors, Is.Empty);
            Assert.That(boot.Flow!.Wallet!.Config.EcoRewardNormal, Is.EqualTo(25), "eco_reward_normal de Remote Config en la cartera");
            Assert.That(AF.RemoteConfig.RC.Get<int>("eco_reward_normal"), Is.EqualTo(25));
            Assert.That(GameBootstrap.Session, Is.Not.Null);

            LevelFlow flow = boot.Flow!;
            boot.TapAnywhere();
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            yield return PlayModeHelpers.WaitFor(() => _sink.Last("level_end") != null, "level_end");

            var names = new List<string>(_sink.Names);
            int start = names.IndexOf("level_start");
            Assert.That(start, Is.GreaterThanOrEqualTo(0), string.Join(",", names));
            Assert.That(names.GetRange(start, 4), Is.EqualTo(new[] { "level_start", "earn_virtual_currency", "af_level_win", "level_end" }));
            AnalyticsEvent earn = _sink.Last("earn_virtual_currency")!;
            Assert.That(earn.Find("value")!.Value.Long, Is.EqualTo(25));
            Assert.That(earn.Find("source")!.Value.Text, Is.EqualTo("win"));
            AnalyticsEvent win = _sink.Last("af_level_win")!;
            Assert.That(win.Find("level")!.Value.Long, Is.EqualTo(3), "común: el nivel en curso");
            Assert.That(win.Find("session_id")!.Value.Text, Is.EqualTo(GameBootstrap.Session!.Id));
            Assert.That(win.Find("app_version")!.Value.Text, Is.EqualTo(Application.version));
            Assert.That(boot.Analytics!.Violations, Is.Empty);
            Assert.That(boot.Analytics.CollectionEnabled, Is.False, "sin consentimiento todavía (08 §2.3)");
            Assert.That(_lines.Exists(l => l.StartsWith("AF_EVENT {\"event\":\"af_level_win\"", System.StringComparison.Ordinal)), Is.True,
                "el sumidero de logcat, con su prefijo");
        }

        [UnityTest]
        public IEnumerator TheColdStart_ComebackGift_ReachesTheAnalytics()
        {
            // Más de 7 días sin sesión (live_comeback_gift.days, 06 §8.3): el arranque en frío paga el Regalo de regreso en
            // SetUpSessions, antes de ConnectTelemetry; la cartera ya está en el bus desde que se crea (revisión de B7).
            Profile profile = Wallet.NewProfile(SystemClock.Instance);
            profile.Stats.LastSeenAt = System.DateTime.UtcNow.AddDays(-8);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(3, new MemoryProgressStore(3, 2));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Wallet!.Seeds, Is.EqualTo(ComebackGift.Default.Seeds), "el Regalo de regreso, pagado en el arranque");

            AnalyticsEvent? earn = _sink.Last("earn_virtual_currency");
            Assert.That(earn, Is.Not.Null, "earn:event del Regalo de regreso en el arranque: " + string.Join(",", _sink.Names));
            Assert.That(earn!.Find("source")!.Value.Text, Is.EqualTo("event"), "fuente event [P] (B6-Q6)");
            Assert.That(earn.Find("value")!.Value.Long, Is.EqualTo(ComebackGift.Default.Seeds));
            int earns = 0;
            foreach (string name in _sink.Names)
            {
                if (name == "earn_virtual_currency")
                {
                    earns++;
                }
            }

            Assert.That(earns, Is.EqualTo(1), "una sola vez: reconectar la cartera en ConnectTelemetry no la duplica");
            Assert.That(boot.Analytics!.Violations, Is.Empty);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_EVENT ", System.StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }
    }
}
