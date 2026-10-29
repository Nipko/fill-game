#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// D-062 (observación 16 de la jornada 6): un aviso abre lo que nombra y se salta el takeover del inicio
    /// (<c>GameBootstrap.PushRoute.cs</c>), con la escena real, el programador falso de avisos y la Bitácora con la casilla de hoy
    /// por sellar (el takeover de prioridad 1). El takeover saltado sale en la siguiente vuelta al inicio.
    /// </summary>
    public sealed class PushRouteTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Tuesday = new DateTime(2026, 10, 20, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private FakeNotificationScheduler _scheduler = null!;
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(Tuesday), Bogota);
            LaunchOptions.ClockOverride = _clock;
            _scheduler = new FakeNotificationScheduler(_clock);
            GameBootstrap.NotificationSchedulerOverride = _scheduler;
            GameBootstrap.LogbookOverride = true;
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord { PlayedDate = "2026-10-20" });
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator DailyReady_OpensTheReto_WithoutTheNotebook_WhichComesAtTheNextLobby()
        {
            _scheduler.SimulateOpenFrom("daily_ready");
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Assert.That(boot.DailyScreen!.IsOpen, Is.True, "D-062: el aviso del Reto abre el Reto");
            Assert.That(boot.DailyScreen.Flow, Is.Not.Null, "el tablero de hoy, como la tarjeta");
            Assert.That(boot.LogbookScreen!.Visible, Is.False, "sin la Bitácora encima");
            Assert.That(boot.LastTakeover, Is.EqualTo(TakeoverKind.None));
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0), "el takeover saltado no gasta el cupo");
            Assert.That(boot.Takeovers.WasSeen(TakeoverKind.Logbook), Is.False, "ni cuenta como visto");
            Assert.That(boot.LastPushDestination, Is.EqualTo("daily"));
            Assert.That(Find("\"event\":\"push_open\""), Does.Contain("\"type\":\"daily_ready\",\"dest\":\"daily\""));

            Assert.That(boot.DailyScreen.Exit(), Is.True);
            Step(boot, Frame);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.LogbookScreen.Visible, Is.True, "la Bitácora sale en la siguiente vuelta al inicio");
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Takeover));
            Assert.That(boot.Takeovers.ShownThisSession, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DailyReady_WithoutTheReto_FallsToTheLobbyWithoutTakeover()
        {
            GameBootstrap.DailyPoolDisabledOverride = true; // sin pool: el Reto no está (D-008); la misma caída que ya jugado o bloqueado
            _scheduler.SimulateOpenFrom("daily_ready");
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Assert.That(boot.DailyScreen == null || !boot.DailyScreen.IsOpen, Is.True);
            Assert.That(boot.LogbookScreen!.Visible, Is.False, "cae al inicio sin takeover");
            Assert.That(boot.LastPushDestination, Is.EqualTo("home"));
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator LogbookPending_OpensTheNotebook_WithoutSpendingTheQuota()
        {
            _scheduler.SimulateOpenFrom("logbook_pending");
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Assert.That(boot.LogbookScreen!.Visible, Is.True);
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Push));
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0));
            Assert.That(boot.LastPushDestination, Is.EqualTo("logbook"));
        }

        [UnityTest]
        public IEnumerator CanoesFull_GoesToTheLobby_AndTheNotebookWaitsForTheNextEntry()
        {
            _scheduler.SimulateOpenFrom("canoes_full");
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, 3 * Frame);

            Assert.That(boot.LogbookScreen!.Visible, Is.False, "esta entrada al inicio, sin takeover");
            Assert.That(boot.LastPushDestination, Is.EqualTo("home"));

            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0));
            Assert.That(boot.LogbookCard!.Visible, Is.True, "la tarjeta de la Bitácora sigue en el inicio");

            Assert.That(boot.PressDailyCard(), Is.True, "se sale del inicio (el Reto) y se vuelve");
            Step(boot, Frame);
            Assert.That(boot.DailyScreen!.Exit(), Is.True);
            Step(boot, Frame);
            Assert.That(boot.LogbookScreen.Visible, Is.True, "la Bitácora sale en la siguiente vuelta al inicio");
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Takeover));
        }

        private IEnumerator Boot()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Tuesday), Bogota));
            profile.Progress.MaxLevel = MaxLevel;
            profile.Progress.NextLevel = MaxLevel + 1;
            profile.Progress.MapSeenLevel = MaxLevel; // sin la secuencia del Mapa del 10 al entrar al inicio
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            if (boot.DailyRunner != null)
            {
                boot.DailyRunner.enabled = false;
            }

            boot.LogbookRunner!.enabled = false;
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += Frame)
            {
                boot.TickDaily(Frame);
                boot.TickLogbook(Frame);
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_STATE ", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        private string Find(string fragment)
        {
            foreach (string line in _lines)
            {
                if (line.Contains(fragment))
                {
                    return line;
                }
            }

            Assert.Fail("no hay ninguna línea con " + fragment);
            return "";
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
