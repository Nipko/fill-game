#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    /// Cableado de las sondas del Reto del río (tarea R10; nota §12; 08 §8.2): las líneas <c>AF_STATE</c> que salen al log (como las
    /// lee <c>adb logcat</c>) en la victoria (<c>level_start</c> antes, <c>daily_challenge</c> con <c>result = win</c> y
    /// <c>earn_virtual_currency</c> con <c>source = daily_challenge</c>), en el fin de día sin victoria (<c>result = fail</c> una
    /// sola vez, R-Q15) y en cada recálculo de la tarjeta (<c>daily_card</c>). El formato de cada línea lo prueba
    /// <c>DailyProbeFormatTests</c> (AF.LiveOps); aquí, que se escriben, cuándo y en qué orden. Mismo arranque que
    /// <see cref="DailyChallengeTests"/> (enganche E2 simulado, pool sintético, reloj y almacén del test).
    /// </summary>
    public sealed class DailyProbeTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá

        private static readonly Regex LevelStartRx = new Regex(
            "^AF_STATE \\{\"event\":\"level_start\",\"t\":\\d+,\"level_name\":\"(d[1-3]_n\\d{4})\",\"variant\":\"base\",\"band\":(\\d)\\}$");

        private static readonly Regex WinRx = new Regex(
            "^AF_STATE \\{\"event\":\"daily_challenge\",\"t\":\\d+,\"date\":\"2026-10-19\",\"result\":\"win\",\"t_ms\":(\\d+|null),"
            + "\"murky_bonus\":false,\"streak_days\":1\\}$");

        private static readonly Regex EarnRx = new Regex(
            "^AF_STATE \\{\"event\":\"earn_virtual_currency\",\"t\":\\d+,\"virtual_currency_name\":\"seeds\","
            + "\"source\":\"daily_challenge\",\"value\":60\\}$");

        private static readonly Regex FailRx = new Regex(
            "^AF_STATE \\{\"event\":\"daily_challenge\",\"t\":\\d+,\"date\":\"2026-10-19\",\"result\":\"fail\",\"t_ms\":null,"
            + "\"murky_bonus\":false,\"streak_days\":0\\}$");

        private ManualClock _clock = null!;
        private InMemoryDailyChallengeStore _store = null!;
        private MemoryProfileStore _profiles = null!;
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(Monday), Bogota);
            _store = new InMemoryDailyChallengeStore();
            _profiles = new MemoryProfileStore(ProfileAt(MaxLevel));
            LaunchOptions.ClockOverride = _clock;
            LaunchOptions.LocTablesOverride = TestTables();
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = _store;
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ResetDailyOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Win_LevelStartFirst_ThenDailyChallengeWin_ThenEarn60()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(DevProbe.Enabled, Is.True, "en el Editor la sonda escribe");
            string today = TodayBoard(boot);
            _lines.Clear();

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Assert.That(Count("level_start"), Is.EqualTo(0), "aún en Llenar y teñir");
            ToPlaying(boot, daily);
            Assert.That(Count("level_start"), Is.EqualTo(1), "al terminar Llenar y teñir (08 §8.2)");
            Match start = LevelStartRx.Match(Only("level_start"));
            Assert.That(start.Success, Is.True, Only("level_start"));
            Assert.That(start.Groups[1].Value, Is.EqualTo(today), "level_name = id del tablero de hoy (d{b}_n{nnnn})");
            Assert.That(start.Groups[2].Value, Is.EqualTo("1"), "banda 1 con max_level 25");
            Assert.That(Count("daily_challenge"), Is.EqualTo(0));

            // «Volver a la orilla» repite la intro, pero no es una partida nueva: no repite level_start.
            PourFirstMove(boot, daily);
            Assert.That(daily.Actions.PressRestart(), Is.True);
            Step(boot, Frame);
            ToPlaying(boot, daily);
            Assert.That(Count("level_start"), Is.EqualTo(1), "una vez por partida");

            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(Count("level_start"), Is.EqualTo(1));
            Assert.That(Count("daily_challenge"), Is.EqualTo(1));
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(1));
            Match win = WinRx.Match(Only("daily_challenge"));
            Assert.That(win.Success, Is.True, Only("daily_challenge"));
            Assert.That(win.Groups[1].Value, Is.Not.EqualTo("null"), "t_ms medido: la partida no se reanudó");
            Assert.That(long.Parse(win.Groups[1].Value), Is.GreaterThan(0L), "t_ms: lo jugado desde que se abrió");
            Assert.That(EarnRx.IsMatch(Only("earn_virtual_currency")), Is.True, Only("earn_virtual_currency"));
            int iStart = IndexOf("level_start");
            int iWin = IndexOf("daily_challenge");
            int iEarn = IndexOf("earn_virtual_currency");
            Assert.That(iStart < iWin && iWin < iEarn, Is.True, "orden: level_start, daily_challenge, earn_virtual_currency");

            // Volver al inicio no repite nada: la tarjeta sale sellada con la racha 1.
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            daily.Calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(Count("daily_challenge"), Is.EqualTo(1));
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(1));
            Assert.That(boot.DailyCard!.LastProbe, Does.Contain("\"state\":\"sealed\"").And.Contain("\"streak\":1"));
        }

        [UnityTest]
        public IEnumerator Resumed_TheWinHasNullTMs_AndOneLevelStart()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);
            Assert.That(daily.Actions.PressQuit(), Is.True, "pausa › Salir");
            Step(boot, Frame);
            Assert.That(boot.DailyCard!.State, Is.EqualTo(DailyCardState.InProgress));

            _lines.Clear();
            Assert.That(boot.PressDailyCard(), Is.True, "Seguir");
            daily = boot.DailyScreen!;
            Assert.That(daily.Resumed, Is.True);
            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(Count("level_start"), Is.EqualTo(1), "la reanudada también empieza una vez");
            Match win = WinRx.Match(Only("daily_challenge"));
            Assert.That(win.Success, Is.True, Only("daily_challenge"));
            Assert.That(win.Groups[1].Value, Is.EqualTo("null"), "t_ms: lo jugado antes de salir no se guarda");
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PendingValidation_WritesWin_ButNoEarn()
        {
            GameBootstrap.DailyServerOverride = FakeServerDate.Online(TimeSpan.FromHours(48));
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(0), "fuera de la deriva no se reconcilia");
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Pending));
            Assert.That(WinRx.IsMatch(Only("daily_challenge")), Is.True, Only("daily_challenge"));
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(0), "pendiente de validar: aún sin semillas (08 §10.1)");
        }

        [UnityTest]
        public IEnumerator Reconcile_AtStartUp_WritesOneEarn_AndNoDailyChallenge()
        {
            _store = new InMemoryDailyChallengeStore(new DailyChallengeRecord
            {
                LastClaimedDate = "2026-10-18", LastDate = "2026-10-18", Result = DailyResult.Win, StreakDays = 1,
                StampsMonth = "2026-10", StampDays = { 18 }, PendingClaims = { new DailyClaimEntry("2026-10-18", 1) },
            });
            GameBootstrap.DailyStoreOverride = _store;
            GameBootstrap.DailyServerOverride = FakeServerDate.Online(TimeSpan.Zero);
            yield return Boot();

            Assert.That(_store.Load().PendingClaims.Count, Is.EqualTo(0), "el cobro pendiente se confirmó");
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(1));
            Assert.That(EarnRx.IsMatch(Only("earn_virtual_currency")), Is.True, Only("earn_virtual_currency"));
            Assert.That(Count("daily_challenge"), Is.EqualTo(0), "reconciliar no es un resultado del día");
        }

        [UnityTest]
        public IEnumerator Reconcile_AtStartUp_NothingPending_WritesNoEarn()
        {
            GameBootstrap.DailyServerOverride = FakeServerDate.Online(TimeSpan.Zero);
            yield return Boot();
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(0));
            Assert.That(Count("daily_challenge"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator AppOpen_OnANewDay_WritesTheFailOfYesterday()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);
            Assert.That(daily.Actions.PressQuit(), Is.True);
            Step(boot, Frame);

            // Se cierra la app con la partida reanudable y se abre al día siguiente: SetUpDaily la descarta (R-Q4, R-Q15).
            _store.Reopen();
            _clock.Advance(TimeSpan.FromDays(1));
            _lines.Clear();
            yield return Boot();
            Assert.That(Count("daily_challenge"), Is.EqualTo(1));
            Assert.That(FailRx.IsMatch(Only("daily_challenge")), Is.True, Only("daily_challenge"));
            Assert.That(IndexOf("daily_challenge"), Is.LessThan(IndexOf("daily_card")), "antes de pintar la tarjeta");
        }

        [UnityTest]
        public IEnumerator Replay_OfAClaimedDay_WritesNoWinAndNoEarn()
        {
            _store = new InMemoryDailyChallengeStore(new DailyChallengeRecord
            {
                LastClaimedDate = "2026-10-19", LastDate = "2026-10-19", Result = DailyResult.Win, StreakDays = 1, StampsMonth = "2026-10", StampDays = { 19 },
            });
            GameBootstrap.DailyStoreOverride = _store;
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.DailyCard!.State, Is.EqualTo(DailyCardState.Sealed));

            // Sellado abre el calendario: se juega desde la pantalla directamente (rejugada sin premio, 06 §4.1).
            DailyChallengeScreen daily = boot.DailyScreen!;
            _lines.Clear();
            Assert.That(daily.Open(MaxLevel), Is.True);
            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(Count("level_start"), Is.EqualTo(1));
            Assert.That(Count("daily_challenge"), Is.EqualTo(0), "sin premio no es la victoria del día");
            Assert.That(Count("earn_virtual_currency"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DayChange_AfterExitingWithoutWinning_WritesOneFail()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);
            Assert.That(daily.Actions.PressQuit(), Is.True, "pausa › Salir");
            Step(boot, Frame);
            Assert.That(Count("daily_challenge"), Is.EqualTo(0), "salir no es un resultado (R-Q16)");

            // Pasa la medianoche con la app abierta en el inicio: la tarjeta se recalcula (TickDaily lo hace al cambiar la fecha local)
            // y la partida de ayer se descarta.
            _lines.Clear();
            _clock.Advance(TimeSpan.FromDays(1));
            Assert.That(boot.RefreshDailyCard(), Is.EqualTo(DailyCardState.Play)); // lo que hace TickDaily al cambiar la fecha
            Assert.That(Count("daily_challenge"), Is.EqualTo(1), "el fail del día que termina sin victoria (R-Q15)");
            Assert.That(FailRx.IsMatch(Only("daily_challenge")), Is.True, Only("daily_challenge"));
            Assert.That(boot.LastDailyDayEnd!.Value.Result, Is.EqualTo(DailyResult.Fail));
            Assert.That(IndexOf("daily_challenge"), Is.LessThan(IndexOf("daily_card")), "el fail sale antes de pintar la tarjeta");
            Assert.That(boot.DailyCard!.LastProbe, Does.Contain("\"state\":\"play\"").And.Contain("\"date\":\"2026-10-20\""));

            // Una vez por día: otro recálculo, ni al reabrir, lo repite.
            boot.RefreshDailyCard();
            _store.Reopen();
            yield return Boot();
            Assert.That(Count("daily_challenge"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Card_EveryRecalculation_WritesItsState()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            DailyChallengeCard card = boot.DailyCard!;
            string today = TodayBoard(boot);
            AF.LiveOps.DailySlot slot = boot.DailyService!.TodaySlot(MaxLevel);
            string expected = "^AF_STATE \\{\"event\":\"daily_card\",\"t\":\\d+,\"state\":\"play\",\"date\":\"2026-10-19\",\"band\":1,"
                + "\"day\":" + slot.Day + ",\"board\":\"" + today + "\",\"streak\":0,\"override\":null,\"override_ignored\":false\\}$";
            Assert.That(card.LastProbe, Does.Match(expected));
            Assert.That(_lines, Does.Contain(card.LastProbe), "la misma línea sale al log");

            _lines.Clear();
            Assert.That(boot.RefreshDailyCard(), Is.EqualTo(DailyCardState.Play));
            Assert.That(Count("daily_card"), Is.EqualTo(1), "una línea por recálculo");

            // En curso: la partida de hoy, con su tablero.
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);
            Assert.That(daily.Actions.PressQuit(), Is.True);
            Step(boot, Frame);
            Assert.That(card.State, Is.EqualTo(DailyCardState.InProgress));
            Assert.That(card.LastProbe, Does.Contain("\"state\":\"in_progress\"").And.Contain("\"board\":\"" + today + "\""));
        }

        [UnityTest]
        public IEnumerator Card_Hidden_WritesHiddenWithoutSlot()
        {
            _profiles = new MemoryProfileStore(ProfileAt(9));
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.DailyCard!.LastProbe, Does.Match(
                "^AF_STATE \\{\"event\":\"daily_card\",\"t\":\\d+,\"state\":\"hidden\",\"date\":\"2026-10-19\",\"band\":0,\"day\":0,"
                + "\"board\":null,\"streak\":0,\"override\":null,\"override_ignored\":false\\}$"));
        }

        // ---- Ayudas ----

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && condition.StartsWith("AF_STATE {\"event\":\"", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        private static string Head(string evt)
        {
            return "AF_STATE {\"event\":\"" + evt + "\",";
        }

        private int Count(string evt)
        {
            int n = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(Head(evt), StringComparison.Ordinal))
                {
                    n++;
                }
            }

            return n;
        }

        private int IndexOf(string evt)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(Head(evt), StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private string Only(string evt)
        {
            int i = IndexOf(evt);
            Assert.That(i, Is.GreaterThanOrEqualTo(0), "sin línea " + evt);
            return _lines[i];
        }

        private static string TodayBoard(GameBootstrap boot)
        {
            return boot.DailyService!.TodayBoard(MaxLevel)!.Id;
        }

        // Arranque con el perfil del test (el inicio) y el enganche E2 simulado; los relojes los lleva el test.
        private IEnumerator Boot()
        {
            yield return PlayModeHelpers.LoadApp(_profiles);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True, "E2");
            boot.DailyRunner!.enabled = false;
            Step(boot, Frame);
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += Frame)
            {
                boot.TickDaily(Frame);
            }
        }

        private static void StepUntil(GameBootstrap boot, Func<bool> done, int maxMs, string what)
        {
            for (int t = 0; t < maxMs; t += Frame)
            {
                if (done())
                {
                    return;
                }

                boot.TickDaily(Frame);
            }

            Assert.That(done(), Is.True, "tiempo agotado: " + what);
        }

        private static void ToPlaying(GameBootstrap boot, DailyChallengeScreen daily)
        {
            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere(); // salta la intro
            StepUntil(boot, () => flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput, 10000, "fin de la intro del Reto");
        }

        private static void PourFirstMove(GameBootstrap boot, DailyChallengeScreen daily)
        {
            LevelFlow flow = daily.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved && solution.MoveCount >= 2, Is.True);
            Pour move = solution.GetMove(0);
            daily.Actions.TapCuia(move.Source);
            daily.Actions.TapCuia(move.Target);
            Step(boot, 1000);
        }

        private static void Win(GameBootstrap boot, DailyChallengeScreen daily)
        {
            ToPlaying(boot, daily);
            LevelFlow flow = daily.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                StepUntil(boot, () => flow.AcceptsBoardInput, 5000, "tablero listo para el vertido " + i);
                daily.Actions.TapCuia(move.Source);
                daily.Actions.TapCuia(move.Target);
            }

            StepUntil(boot, () => daily.Won, 10000, "victoria del Reto");
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Monday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }

        // Tablas del proyecto: con E5, strings.csv ya trae las filas del Reto (docs/dev/traspaso/spark-strings-reto.csv).
        private static LocTableTexts TestTables()
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, AF.Localization.LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, AF.Localization.LocCatalog.FactsFile);
            return new LocTableTexts(strings, facts);
        }
    }
}
