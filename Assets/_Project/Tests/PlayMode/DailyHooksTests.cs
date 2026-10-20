#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
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
    /// Enganches del Reto del río aplicados (R12): E2 (<c>SetUpDaily</c> en el arranque), E3 (el pool de la escena), E4 (el bloque
    /// <c>daily_challenge</c> del perfil), E5 (los textos en <c>strings.csv</c>), E9 (Cosecha doble solo con premio confirmado), E21
    /// (sin «Nivel N · intento k»), E22 (Atrás), E23 (volver de segundo plano) y E24 (las sondas de partida marcadas con el tablero).
    /// El primer test usa la escena tal cual (pool real, almacén del perfil y tablas de la escena); los demás, el pool sintético
    /// (<see cref="DailyPlayTestPool"/>) y un almacén en memoria, como <see cref="DailyChallengeTests"/>.
    /// </summary>
    public sealed class DailyHooksTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private MemoryProfileStore _profiles = null!;
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(Monday), Bogota);
            _profiles = new MemoryProfileStore(ProfileAt(MaxLevel));
            LaunchOptions.ClockOverride = _clock;
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
        public IEnumerator E2_E3_E4_E5_TheSceneStartsTheReto_WithItsPool_AndTheWinIsSavedInTheProfile()
        {
            yield return Boot(callSetUp: false);
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.DailyService, Is.Not.Null, "E2: SetUpDaily en el arranque");
            Assert.That(boot.DailyPoolAsset, Is.Not.Null, "E3: el pool en la escena");
            Assert.That(boot.DailyPoolAsset!.name, Is.EqualTo("daily_pool_v1"));
            Assert.That(boot.DailyService!.Pool, Is.Not.Null);
            DailyChallengeCard card = boot.DailyCard!;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.State, Is.EqualTo(DailyCardState.Play));
            Assert.That(card.TitleText, Is.EqualTo("Reto del río · 19 oct"), "E5: los textos de la tabla de la escena");
            Assert.That(card.StreakText, Is.EqualTo("0/7"));
            Profile profile = boot.Profile!;
            int seeds = profile.Wallet.Seeds;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Assert.That(daily.Flow!.DailyBoardId, Does.StartWith("d1_n"), "banda 1 con max_level 25");
            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));

            // E4: el registro y el pago, en el mismo perfil y en la misma escritura.
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(seeds + 60));
            Assert.That(profile.DailyChallenge, Is.Not.Null);
            Assert.That(profile.DailyChallenge!.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(profile.DailyChallenge.InProgress, Is.Null);
            Profile onDisk = ProfileJson.Deserialize(_profiles.Json!);
            Assert.That(onDisk.Wallet.Seeds, Is.EqualTo(seeds + 60));
            Assert.That(onDisk.DailyChallenge!.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(onDisk.DailyChallenge.StreakDays, Is.EqualTo(1));
            Assert.That(onDisk.DailyChallenge.StampDays, Is.EqualTo(new[] { 19 }));
            Assert.That(onDisk.DailyChallenge.Result, Is.EqualTo("win"));
        }

        [UnityTest]
        public IEnumerator E9_DoubleHarvest_AfterAConfirmedWin_PaysThePrizeAgainOnce()
        {
            var ads = new ImmediateAdPresenter();
            LaunchOptions.AdPresenterOverride = ads;
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            int seeds = boot.Profile!.Wallet.Seeds;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(daily.DoubleHarvestOffered, Is.True);
            Assert.That(daily.Calendar.DoubleHarvestShown, Is.True, "con premio confirmado y video (06 §4.1)");
            Assert.That(daily.Calendar.RewardText, Is.EqualTo("+60"));

            _lines.Clear();
            daily.Calendar.DoubleHarvestButton.onClick.Invoke();
            Assert.That(ads.Last!.Placement, Is.EqualTo(AdPlacement.DoubleHarvest));
            Assert.That(Find("\"event\":\"earn_virtual_currency\""), Does.Contain("\"source\":\"double_harvest\",\"value\":60}"),
                "08 §8.2: earn_virtual_currency con source = double_harvest");
            Assert.That(boot.Profile.Wallet.Seeds, Is.EqualTo(seeds + 120), "60 → 120");
            Assert.That(daily.HarvestDoubled, Is.True);
            Assert.That(daily.Calendar.RewardText, Is.EqualTo("+120"));
            Assert.That(daily.Calendar.DoubleHarvestShown, Is.False, "una vez por victoria");
            Assert.That(daily.PressDoubleHarvest(), Is.False);
            Assert.That(boot.Profile.Daily.RvDoubleHarvest, Is.EqualTo(1), "cuenta para el tope diario de rv_double_harvest");
        }

        [UnityTest]
        public IEnumerator E9_VideoClosedEarly_PaysNothing()
        {
            LaunchOptions.AdPresenterOverride = new ImmediateAdPresenter(SimulatedAdClose.ClosedEarly);
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(daily.Calendar.DoubleHarvestShown, Is.True);
            int seeds = boot.Profile!.Wallet.Seeds;
            int watched = boot.Profile.Daily.RvDoubleHarvest;
            _lines.Clear();

            daily.Calendar.DoubleHarvestButton.onClick.Invoke();

            Assert.That(boot.Profile.Wallet.Seeds, Is.EqualTo(seeds), "sin el video entero no se paga (02 §6)");
            Assert.That(daily.HarvestDoubled, Is.False);
            Assert.That(daily.Calendar.RewardText, Is.EqualTo("+60"));
            Assert.That(boot.Profile.Daily.RvDoubleHarvest, Is.EqualTo(watched), "no cuenta para el tope diario");
            Assert.That(_lines.Exists(l => l.Contains("\"source\":\"double_harvest\"")), Is.False);
        }

        [UnityTest]
        public IEnumerator E9_NoDoubleHarvest_WhenTheDailyCapIsReached()
        {
            LaunchOptions.AdPresenterOverride = new ImmediateAdPresenter();
            Profile profile = ProfileAt(MaxLevel);
            profile.Daily.DateLocal = "2026-10-19";
            profile.Daily.RvDoubleHarvest = 5; // ads_rv_cap_double_harvest (08 §9), compartido con la campaña
            profile.Daily.RvTotal = 5;
            _profiles = new MemoryProfileStore(profile);
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(daily.DoubleHarvestOffered, Is.False, "tope diario agotado");
            Assert.That(daily.Calendar.DoubleHarvestShown, Is.False);
            Assert.That(daily.PressDoubleHarvest(), Is.False);
        }

        [UnityTest]
        public IEnumerator E9_NoDoubleHarvest_OnASecondWinOfTheSameDay_NoReward()
        {
            LaunchOptions.AdPresenterOverride = new ImmediateAdPresenter();
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            daily.Calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            int seeds = boot.Profile!.Wallet.Seeds;

            // Sellado: la tarjeta abre el calendario; el tablero se vuelve a jugar desde el calendario o reabriendo el Reto.
            Assert.That(boot.DailyScreen!.Open(MaxLevel), Is.True, "reintentos ilimitados el mismo día (06 §4.1)");
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(daily.DoubleHarvestOffered, Is.False, "sin premio no hay Cosecha doble");
            Assert.That(daily.Calendar.DoubleHarvestShown, Is.False);
            Assert.That(daily.PressDoubleHarvest(), Is.False);
            Assert.That(boot.Profile.Wallet.Seeds, Is.EqualTo(seeds));
        }

        [UnityTest]
        public IEnumerator E9_NoDoubleHarvest_WhenThePrizeIsPendingValidation()
        {
            LaunchOptions.AdPresenterOverride = new ImmediateAdPresenter();
            GameBootstrap.DailyServerOverride = FakeServerDate.Online(TimeSpan.FromHours(48));
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            int seeds = boot.Profile!.Wallet.Seeds;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Pending));
            Assert.That(daily.DoubleHarvestOffered, Is.False);
            Assert.That(daily.Calendar.DoubleHarvestShown, Is.False);
            Assert.That(daily.PressDoubleHarvest(), Is.False);
            Assert.That(boot.Profile.Wallet.Seeds, Is.EqualTo(seeds));
        }

        [UnityTest]
        public IEnumerator E21_RestartInTheReto_ShowsNoLevelAttemptBanner()
        {
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);

            Assert.That(daily.Actions.PressRestart(), Is.True);
            Step(boot, Frame);
            Assert.That(daily.View!.BannerText, Is.Null.Or.Not.Contains("Nivel"), "06 §4.1: nunca «Nivel N» en el Reto");
            Assert.That(daily.View.LevelTitle.text, Is.EqualTo("Reto del río"));
        }

        [UnityTest]
        public IEnumerator E22_AndroidBack_DoesNothingOnTheBoard_AndClosesTheCalendar()
        {
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);

            Assert.That(boot.PressAndroidBack(), Is.False, "con el tablero en juego, nada (02 §3)");
            Assert.That(daily.IsOpen, Is.True);
            Assert.That(boot.Lobby!.Visible, Is.False, "el inicio sigue oculto");

            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás cierra el calendario como «Inicio»");
            Step(boot, Frame);
            Assert.That(daily.IsOpen, Is.False);
            Assert.That(boot.Lobby.Visible, Is.True);
        }

        [UnityTest]
        public IEnumerator E23_BackFromBackground_SnapsTheRetoView_AndClearsTheSelection()
        {
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            LevelFlow flow = daily.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Pour move = solution.GetMove(0);
            daily.Actions.TapCuia(move.Source);
            Assert.That(flow.Selected, Is.EqualTo(move.Source), "cuia seleccionada");

            boot.SendMessage("OnApplicationPause", false);

            Assert.That(flow.Selected, Is.EqualTo(-1), "02 §14: la selección se limpia");
            Assert.That(daily.View!.IsViewIdle, Is.True, "la vista salta al modelo");
        }

        [UnityTest]
        public IEnumerator E24_TheRetoBoardLines_CarryTheDailyBoardId()
        {
            yield return BootSynthetic();
            GameBootstrap boot = GameBootstrap.Current!;
            _lines.Clear();
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            string board = daily.Flow!.DailyBoardId!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);

            string marker = ",\"daily\":\"" + board + "\"";
            Assert.That(Find("\"event\":\"load\""), Does.Contain(marker));
            Assert.That(Find("\"event\":\"ready\""), Does.Contain(marker));
            Assert.That(Find("\"event\":\"pour\""), Does.Contain(marker));
        }

        [UnityTest]
        public IEnumerator E24_CampaignLines_CarryNoDailyMark()
        {
            LaunchOptions.ProfileStoreOverride = _profiles;
            yield return PlayModeHelpers.LoadGame(26, new MemoryProgressStore(26, MaxLevel));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 3000);

            string load = Find("\"event\":\"load\"");
            Assert.That(load, Does.Contain("\"level\":26"));
            foreach (string line in _lines)
            {
                Assert.That(line, Does.Not.Contain("\"daily\":"), "en la campaña no hay marca del Reto: " + line);
            }
        }

        // ---- Ayudas ----

        private IEnumerator BootSynthetic()
        {
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return Boot(callSetUp: true);
        }

        private IEnumerator Boot(bool callSetUp)
        {
            yield return PlayModeHelpers.LoadApp(_profiles);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            PlayModeHelpers.TakeClock(boot);
            if (callSetUp)
            {
                Assert.That(boot.SetUpDaily(), Is.True, "ya lo hizo el arranque (E2): no hace nada");
            }

            boot.DailyRunner!.enabled = false;
            Step(boot, Frame);
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
    }
}
