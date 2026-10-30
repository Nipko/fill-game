#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
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
            GameBootstrap.L10SequenceOverride = null;
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
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(1), "push_open sale una sola vez por aviso");
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

        [UnityTest]
        public IEnumerator Resume_WithTheLobbyClear_Routes()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord()); // sin casilla: el inicio queda limpio
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);

            Resume(boot, "daily_ready");
            Assert.That(boot.PushRoutePending, Is.True, "en el inicio sin nada encima: espera a la siguiente vuelta del bucle");
            Step(boot, Frame);
            Assert.That(boot.DailyScreen.IsOpen, Is.True, "a) vuelta de segundo plano en el inicio: se encamina");
            Assert.That(boot.LastPushDestination, Is.EqualTo("daily"));
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Resume_InALevel_Drops_AndStaysInTheLevel()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord());
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));

            Resume(boot, "daily_ready");
            Step(boot, Frame);
            Assert.That(boot.PushRoutePending, Is.False);
            Assert.That(boot.LastPushDestination, Is.EqualTo("none"), "b) en un nivel: no se saca al jugador");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);
            Assert.That(Find("\"event\":\"push_open\""), Does.Contain("\"type\":\"daily_ready\",\"dest\":\"none\""));
        }

        [UnityTest]
        public IEnumerator Resume_InTheReto_Drops_AndStaysInIt()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord());
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            Assert.That(boot.PressDailyCard(), Is.True);
            LevelFlow? before = boot.DailyScreen!.Flow;

            Resume(boot, "logbook_pending");
            Step(boot, Frame);
            Assert.That(boot.LastPushDestination, Is.EqualTo("none"), "b) en el Reto");
            Assert.That(boot.DailyScreen.IsOpen, Is.True);
            Assert.That(boot.DailyScreen.Flow, Is.SameAs(before), "la misma partida");
            Assert.That(boot.LogbookScreen!.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Resume_WithTheNotebookTakeoverOpen_ClosesIt_AndRoutes()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            Assert.That(boot.LogbookScreen!.Visible, Is.True, "el takeover del arranque, sin aviso");
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Takeover));

            Resume(boot, "daily_ready"); // observación 19: se dejó la app con el takeover encima del inicio
            Step(boot, Frame);
            Assert.That(boot.LogbookScreen.Visible, Is.False, "el aviso se salta el takeover (D-062)");
            Assert.That(boot.DailyScreen!.IsOpen, Is.True);
            Assert.That(boot.LastPushDestination, Is.EqualTo("daily"));
        }

        [UnityTest]
        public IEnumerator Resume_WithTheNotebookOpenedByHand_Drops_AndKeepsIt()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord());
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            Assert.That(boot.PressLogbookCard(), Is.True, "la libreta abierta desde su tarjeta");

            Resume(boot, "daily_ready");
            Step(boot, Frame);
            Assert.That(boot.LastPushDestination, Is.EqualTo("none"), "b) con la libreta abierta a mano no se toca");
            Assert.That(boot.LogbookScreen!.Visible, Is.True);
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator ANewNotification_ReplacesThePendingOne()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord());
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Resume(boot, "canoes_full");
            Assert.That(boot.PushRoutePending, Is.True);
            _scheduler.SimulateOpenFrom("daily_ready"); // otro aviso tocado antes de que el bucle encamine el primero
            boot.SessionsPaused(false);
            Assert.That(Find("\"event\":\"push_open\""), Does.Contain("\"type\":\"canoes_full\",\"dest\":\"none\""), "el primero se descarta");
            Step(boot, Frame);
            Assert.That(boot.DailyScreen!.IsOpen, Is.True, "y el nuevo se encamina");
            Assert.That(boot.LastPushDestination, Is.EqualTo("daily"));
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator WithoutTheLogbook_GoingToBackground_DropsThePendingOne()
        {
            GameBootstrap.LogbookOverride = false; // sin Bitácora no hay LogbookRunner ni LogbookPause
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NewProfile(MaxLevel, MaxLevel)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.DailyRunner!.enabled = false;
            Assert.That(boot.LogbookRunner, Is.Null);

            boot.SessionsPaused(true);
            _clock.Advance(TimeSpan.FromMinutes(5));
            _scheduler.SimulateOpenFrom("daily_ready");
            boot.SessionsPaused(false);
            Assert.That(boot.PushRoutePending, Is.True);
            boot.SessionsPaused(true);
            Assert.That(boot.PushRoutePending, Is.False, "SessionsPaused(true) lo descarta");
            Assert.That(boot.LastPushDestination, Is.EqualTo("none"));
        }

        [UnityTest]
        public IEnumerator BackgroundBeforeReachingTheLobby_Drops()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord());
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Resume(boot, "daily_ready");
            Assert.That(boot.PushRoutePending, Is.True);
            boot.LogbookPause(true); // c) se va otra vez antes de que el bucle lo encamine
            Assert.That(boot.PushRoutePending, Is.False);
            Assert.That(boot.LastPushDestination, Is.EqualTo("none"));
            boot.LogbookPause(false);
            Step(boot, Frame);
            Assert.That(boot.DailyScreen!.IsOpen, Is.False, "descartado: no abre nada al volver");
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(1), "una sola línea: la del descarte");
        }

        [UnityTest]
        public IEnumerator ColdStart_WithTheLevel10MapDue_ShowsTheSequenceFirst_ThenTheReto()
        {
            GameBootstrap.L10SequenceOverride = true;
            _scheduler.SimulateOpenFrom("daily_ready");
            yield return Boot(10, 9);
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            LogbookScreen screen = boot.LogbookScreen!;
            Assert.That(screen.Visible, Is.True, "e) la secuencia del 10 va antes (06 §2.3): primero la libreta");
            Assert.That(screen.Source, Is.EqualTo(LogbookOpenSource.L10));
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);
            Assert.That(boot.PushRoutePending, Is.True, "el aviso espera");

            screen.CloseButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(boot.RiverMapView!.Visible, Is.True, "luego el Mapa");
            Assert.That(boot.DailyScreen.IsOpen, Is.False);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Step(boot, Frame);
            Assert.That(boot.DailyScreen.IsOpen, Is.True, "y al acabar, el destino del aviso");
            Assert.That(boot.LastPushDestination, Is.EqualTo("daily"));
        }

        [UnityTest]
        public IEnumerator ColdStart_ThatReopensALevelInProgress_Drops()
        {
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord());
            var store = new MemoryProfileStore(NewProfile(MaxLevel, MaxLevel));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap first = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(first);
            Assert.That(first.PressPlay(), Is.True);
            first.TapAnywhere(); // salta la intro
            PlayModeHelpers.StepUntilIdle(first, 4000);
            LevelFlow flow = first.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Pour move = solution.GetMove(0);
            first.TapCuia(move.Source);
            first.TapCuia(move.Target);
            PlayModeHelpers.StepUntilIdle(first, 4000);
            Assert.That(ProfileJson.Deserialize(store.Json!).LevelState, Is.Not.Null, "un nivel en curso guardado");

            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord { PlayedDate = "2026-10-20" });
            _scheduler.SimulateOpenFrom("daily_ready");
            _lines.Clear();
            yield return PlayModeHelpers.LoadApp(store); // como cerrar la app y abrirla desde el aviso
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.LogbookRunner!.enabled = false;
            boot.DailyRunner!.enabled = false;
            Step(boot, Frame);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "02 §14: reabre el nivel en curso");
            Assert.That(boot.PushRoutePending, Is.False, "f) el arranque fue a un nivel: el aviso se descarta");
            Assert.That(boot.LastPushDestination, Is.EqualTo("none"));
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(1));
        }

        private IEnumerator Boot(int maxLevel = MaxLevel, int mapSeenLevel = MaxLevel)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NewProfile(maxLevel, mapSeenLevel)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            if (boot.DailyRunner != null)
            {
                boot.DailyRunner.enabled = false;
            }

            boot.LogbookRunner!.enabled = false;
        }

        private static Profile NewProfile(int maxLevel, int mapSeenLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Tuesday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            profile.Progress.MapSeenLevel = mapSeenLevel; // = maxLevel: sin la secuencia del Mapa del 10 al entrar al inicio
            return profile;
        }

        // Vuelta de segundo plano (< 30 min: la misma sesión) tocando un aviso de tipo type, como SessionRunner y LogbookRunner.
        private void Resume(GameBootstrap boot, string type)
        {
            boot.SessionsPaused(true);
            boot.LogbookPause(true);
            _clock.Advance(TimeSpan.FromMinutes(5));
            _scheduler.SimulateOpenFrom(type);
            boot.LogbookPause(false);
            boot.SessionsPaused(false);
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += Frame)
            {
                boot.TickSessions();
                boot.TickDaily(Frame);
                boot.TickLogbook(Frame);
            }
        }

        private int Count(string fragment)
        {
            int n = 0;
            foreach (string line in _lines)
            {
                if (line.Contains(fragment))
                {
                    n++;
                }
            }

            return n;
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
