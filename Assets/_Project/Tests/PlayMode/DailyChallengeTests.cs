#nullable enable
using System;
using System.Collections;
using AF.Boosters;
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
    /// Pantallas del Reto del río en la escena Game (tarea R9; 06 §4.1 «Pantalla»; nota §8 y §9): la tarjeta del inicio y sus
    /// estados, una partida de un tablero de prueba (pool sintético construido en código, <see cref="DailyPlayTestPool"/>)
    /// jugada hasta ganar con el sello que cae y el «+60» en el calendario, sin tocar canoas, progreso de campaña,
    /// <c>level_state</c> ni estadísticas de campaña (la Crecida aún no existe en el cliente: se comprueba que no hay barra),
    /// salir sin ganar (R-Q9) y la reanudación el mismo día y el descarte otro día (R-Q4). Simula el enganche E2 llamando a
    /// <see cref="GameBootstrap.SetUpDaily"/> tras el arranque, con el pool, el almacén y el reloj del test, y una tabla de
    /// textos del proyecto (con E5, <c>strings.csv</c> ya trae las filas de <c>docs/dev/traspaso/spark-strings-reto.csv</c>).
    /// </summary>
    public sealed class DailyChallengeTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private InMemoryDailyChallengeStore _store = null!;
        private MemoryProfileStore _profiles = null!;

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
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Card_Play_ThenWinTheBoard_StampFallsWithPlus60_WithoutCampaignEffects()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Profile profile = boot.Profile!;
            int seeds = profile.Wallet.Seeds;
            int canoes = profile.Wallet.Canoes.Count;
            int levelsWon = profile.Stats.LevelsWon;
            int failsHard = profile.Stats.FailsHard;
            string lobbyLabel = boot.Lobby!.PlayLabel;

            DailyChallengeCard card = boot.DailyCard!;
            Assert.That(card.Visible, Is.True, "desbloqueado desde el 10 (live_unlock_level_daily)");
            Assert.That(card.State, Is.EqualTo(DailyCardState.Play));
            Assert.That(card.TitleText, Is.EqualTo("Reto del río · 19 oct"));
            Assert.That(card.StateText, Is.EqualTo("Jugar"));
            Assert.That(card.StreakText, Is.EqualTo("0/7"));
            Assert.That(card.Band, Is.EqualTo(1), "max_level 25 < 41: banda 1");
            Assert.That(card.WavesShown, Is.EqualTo(1));
            Assert.That(card.StampShown, Is.False);

            // Abrir con el botón de la tarjeta (el toque real).
            card.Button.onClick.Invoke();
            DailyChallengeScreen daily = boot.DailyScreen!;
            LevelFlow flow = daily.Flow!;
            GameScreen view = daily.View!;
            Assert.That(flow.IsDaily, Is.True);
            Assert.That(boot.Lobby.Visible, Is.False, "el tablero del Reto sustituye al inicio");
            Assert.That(card.Visible, Is.False);
            Assert.That(view.Visible, Is.True);
            Assert.That(boot.View!.Visible, Is.False, "la pantalla de campaña sigue oculta");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "AppFlow no cambia");
            Assert.That(view.LevelTitle.text, Is.EqualTo("Reto del río"), "cabecera «Reto del río», nunca «Nivel N» (06 §4.1)");
            foreach (Transform t in view.Canvas.GetComponentsInChildren<Transform>(true))
            {
                bool streakBar = t.name.IndexOf("Crecida", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.name.IndexOf("WinStreak", StringComparison.OrdinalIgnoreCase) >= 0;
                Assert.That(streakBar, Is.False, "sin barra de Crecida: " + t.name);
            }

            Win(boot, daily);
            Assert.That(daily.Won, Is.True);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            Assert.That(view.LevelTitle.text, Is.EqualTo("Reto del río"));
            Assert.That(view.VictoryVisible, Is.False, "sin la victoria de campaña (Cosecha, Siguiente)");

            DailyStampCalendar calendar = daily.Calendar;
            Assert.That(calendar.DayCount, Is.EqualTo(31), "octubre");
            Assert.That(calendar.FallingDay, Is.EqualTo(19), "el sello del día cae");
            Assert.That(calendar.StampLanded, Is.False);
            Assert.That(calendar.FallingScale, Is.GreaterThan(1.5f));
            Assert.That(calendar.RewardShown, Is.False, "el +60 sale cuando el sello se posa");
            Step(boot, (int)DailyStampCalendar.StampDropMs + (int)DailyStampCalendar.RewardDelayMs + (4 * Frame));
            Assert.That(calendar.StampLanded, Is.True);
            Assert.That(calendar.FallingScale, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(calendar.StampedDays, Is.EqualTo(new[] { 19 }));
            Assert.That(calendar.RewardShown, Is.True);
            Assert.That(calendar.RewardText, Is.EqualTo("+60"));
            Assert.That(calendar.DoubleHarvestShown, Is.EqualTo(daily.DoubleHarvestOffered), "Cosecha doble del Reto (E9): con premio confirmado y video");
            Assert.That(calendar.TitleText, Is.EqualTo("Reto del río · 19 oct"));

            // Premio: 60 semillas. Nada de campaña: canoas, max_level, next_level, level_state, levels_won, fails_hard.
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(seeds + 60));
            Assert.That(profile.Wallet.Canoes.Count, Is.EqualTo(canoes), "el Reto no gasta canoas (04 §7.1)");
            Assert.That(profile.Progress.MaxLevel, Is.EqualTo(MaxLevel));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(MaxLevel + 1));
            Assert.That(profile.LevelState, Is.Null, "el Reto no usa el level_state de la campaña");
            Assert.That(profile.Stats.LevelsWon, Is.EqualTo(levelsWon));
            Assert.That(profile.Stats.FailsHard, Is.EqualTo(failsHard), "ni fallo duro (la Crecida no se rompe, 06 §4.3)");
            Assert.That(_store.Load().LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(_store.Load().InProgress, Is.Null);

            // Inicio: vuelve al inicio con la tarjeta sellada y la racha 1/7.
            calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(daily.IsOpen, Is.False);
            Assert.That(view.Visible, Is.False);
            Assert.That(boot.Lobby.Visible, Is.True);
            Assert.That(boot.Lobby.PlayLabel, Is.EqualTo(lobbyLabel), "«Jugar · Nivel 26» no cambia");
            Assert.That(card.Visible, Is.True);
            Assert.That(card.State, Is.EqualTo(DailyCardState.Sealed));
            Assert.That(card.StateText, Is.EqualTo("Sellado"));
            Assert.That(card.StampShown, Is.True);
            Assert.That(card.StreakText, Is.EqualTo("1/7"));

            // Sellada: tocarla abre el calendario del mes, sin premio ni sello que cae.
            Assert.That(boot.PressDailyCard(), Is.True);
            Assert.That(calendar.Visible, Is.True);
            Assert.That(calendar.FallingDay, Is.EqualTo(0));
            Assert.That(calendar.RewardShown, Is.False);
            Assert.That(calendar.StampedDays, Is.EqualTo(new[] { 19 }));
            Assert.That(card.Visible, Is.False);
            calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(calendar.Visible, Is.False);
            Assert.That(card.Visible, Is.True);

            // El mismo día se puede volver a jugar, sin premio (reintentos ilimitados; el premio es diario, 06 §4.1).
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(seeds + 60));
        }

        [UnityTest]
        public IEnumerator Card_HiddenBeforeLevel10()
        {
            _profiles = new MemoryProfileStore(ProfileAt(9));
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.DailyService!.CardState(9), Is.EqualTo(DailyCardState.Hidden));
            Assert.That(boot.DailyCard!.Visible, Is.False, "D-008: oculta hasta el 10");
            Assert.That(boot.PressDailyCard(), Is.False);
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Card_WithoutTestPool_UsesTheScenePool_E3()
        {
            GameBootstrap.DailyPoolOverride = null; // con E3 la escena trae daily_pool_v1.json
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.DailyPoolAsset, Is.Not.Null);
            Assert.That(boot.DailyService!.Pool, Is.Not.Null);
            Assert.That(boot.DailyCard!.Visible, Is.True);
            Assert.That(boot.RefreshDailyCard(), Is.EqualTo(DailyCardState.Play));
        }

        /// <summary>
        /// Safe area (auditoría B10; 08 §13.1 #4; T6, carril B, punto 3): con una barra de gestos de 220 px (73,3 dp) abajo y un
        /// recorte de 120 px (40 dp) arriba, el calendario de sellos tras ganar el Reto queda entero entre las dos zonas seguras,
        /// con «Volver al inicio» (y la Cosecha doble, si sale) sobre la barra, a 360 × 780 y en 16:9.
        /// </summary>
        [UnityTest]
        public IEnumerator Calendar_WithAGestureBar_StaysInsideTheSafeArea([Values(2340, 1920, 1600)] int heightPx)
        {
            // 1600 px (a 2,5 px/dp, 432 × 640 dp) con 250 px (100 dp) de barra: la cota de abajo actúa y sube el panel, que
            // centrado («Volver al inicio» y la Cosecha doble) caería sobre la barra.
            bool low = heightPx == 1600;
            var screen = low ? new Vector4(1080f, heightPx, 96f, 250f) : new Vector4(1080f, heightPx, 120f, 220f);
            GameScreen.SimulatedScreen = screen;
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            HomeSloth sloth = boot.Lobby!.HomeWindow.Sloth!; // en 16:9 su saludo cruza la tarjeta y la aparta mientras se ve
            for (int t = 0; t < 400; t += Frame)
            {
                sloth.Tick(Frame);
            }

            for (int t = 0, end = (int)sloth.BubbleDurationMs + 300; t < end; t += Frame)
            {
                sloth.Tick(Frame);
            }

            Step(boot, Frame);
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Step(boot, Frame);
            Canvas.ForceUpdateCanvases();

            DailyStampCalendar calendar = daily.Calendar;
            string where = "el calendario a 1080 × " + heightPx + " px con " + (low ? 250 : 220) + " px de barra";
            SafeAreaOverlayTests.Margins(calendar.Canvas, screen, out float safeTop, out float safeBottom, out float canvasH);
            Rect panel = StopCardTests.CanvasRect((RectTransform)calendar.Canvas.transform.Find("CalendarPanel"), calendar.Canvas);
            Rect home = StopCardTests.CanvasRect((RectTransform)calendar.HomeButton.transform, calendar.Canvas);
            Debug.Log("AF_SAFE_CALENDAR " + where + ": zona segura " + safeTop.ToString("0.0") + " / " + safeBottom.ToString("0.0") + " dp; panel " + panel + ", Volver " + home);
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(safeTop + 8f - 0.05f), where + ": el panel, bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(canvasH - safeBottom - 8f + 0.05f), where + ": el panel, sobre la barra");
            Assert.That(home.yMax, Is.LessThanOrEqualTo(canvasH - safeBottom + 0.05f), where + ": «Volver al inicio», sobre la barra");
            if (low)
            {
                Assert.That(calendar.DoubleHarvestShown, Is.True, where + ": condición del test, con la Cosecha doble");
                Assert.That(panel.yMin, Is.LessThan(((canvasH - panel.height) * 0.5f) - 0.5f), where + ": más arriba que centrado");
                Assert.That(panel.yMax, Is.EqualTo(canvasH - safeBottom - 8f).Within(0.05f), where + ": sobre la barra + 8");
            }

            if (calendar.DoubleHarvestShown)
            {
                Rect twice = StopCardTests.CanvasRect((RectTransform)calendar.DoubleHarvestButton.transform, calendar.Canvas);
                Assert.That(twice.yMax, Is.LessThanOrEqualTo(canvasH - safeBottom + 0.05f), where + ": la Cosecha doble, sobre la barra");
            }
        }

        [UnityTest]
        public IEnumerator Card_PendingValidation_WhenTheClockDriftsOnline()
        {
            GameBootstrap.DailyServerOverride = FakeServerDate.Online(TimeSpan.FromHours(48));
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            int seeds = boot.Profile!.Wallet.Seeds;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Pending));
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.Calendar.RewardShown, Is.False, "pendiente de validar: aún sin semillas (08 §10.1)");
            Assert.That(daily.Calendar.StatusText, Is.EqualTo("Premio pendiente de validar"));
            Assert.That(boot.Profile.Wallet.Seeds, Is.EqualTo(seeds));

            daily.Calendar.HomeButton.onClick.Invoke();
            Step(boot, Frame);
            Assert.That(boot.DailyCard!.State, Is.EqualTo(DailyCardState.PendingValidation));
            Assert.That(boot.DailyCard.StateText, Is.EqualTo("Pendiente"), "D-050: la forma corta en la tarjeta del muelle (home_card_daily_pending)");
            Assert.That(A11yLabels.TryGet(boot.DailyCard.Button.gameObject, out string spoken, out _), Is.True);
            StringAssert.Contains("Pendiente de validar", spoken, "TalkBack lee la larga");
            Assert.That(boot.DailyCard.StampShown, Is.True);
        }

        [UnityTest]
        public IEnumerator ExitWithoutWinning_KeepsTheGameResumable_NoCanoe_RQ16()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Profile profile = boot.Profile!;
            int canoes = profile.Wallet.Canoes.Count;
            int seeds = profile.Wallet.Seeds;

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            LevelFlow flow = daily.Flow!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(1));

            Assert.That(daily.Actions.PressQuit(), Is.True, "pausa › Salir");
            Step(boot, Frame);

            Assert.That(daily.IsOpen, Is.False);
            Assert.That(daily.ExitedWithoutWin, Is.True);
            Assert.That(_store.Load().LastClaimedDate, Is.Null, "sin premio");
            Assert.That(profile.Wallet.Canoes.Count, Is.EqualTo(canoes), "sin canoa (04 §7.1)");
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(seeds));
            Assert.That(profile.Progress.MaxLevel, Is.EqualTo(MaxLevel));
            Assert.That(profile.LevelState, Is.Null);
            Assert.That(boot.Lobby!.Visible, Is.True);
            Assert.That(boot.LastDailyDayEnd, Is.Null, "salir no es un resultado: el fail sale si el día termina sin victoria (R-Q15 a)");

            // R-Q16 (a): la partida sigue reanudable el mismo día y la tarjeta dice «Seguir».
            DailyChallengeCard card = boot.DailyCard!;
            Assert.That(boot.DailyService!.Resumable, Is.Not.Null);
            Assert.That(card.Visible, Is.True);
            Assert.That(card.State, Is.EqualTo(DailyCardState.InProgress));
            Assert.That(card.StateText, Is.EqualTo("Seguir"));

            card.Button.onClick.Invoke();
            daily = boot.DailyScreen!;
            Assert.That(daily.IsOpen, Is.True);
            Assert.That(daily.Resumed, Is.True, "tocar la tarjeta la reanuda");
            Assert.That(daily.Flow!.Session.AppliedPours, Is.EqualTo(1), "con el vertido de antes de salir");
            Assert.That(profile.Wallet.Canoes.Count, Is.EqualTo(canoes));
        }

        [UnityTest]
        public IEnumerator AndroidBack_DoesNothingOnTheBoard_ButClosesTheCalendar()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);

            Assert.That(daily.HandleBack(), Is.False, "con el tablero en juego Atrás no saca del Reto (02 §3; 04 §6.6)");
            Assert.That(daily.IsOpen, Is.True);
            Assert.That(daily.ExitedWithoutWin, Is.False);

            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            Assert.That(daily.HandleBack(), Is.True, "con el calendario, Atrás es «Inicio»");
            Step(boot, Frame);
            Assert.That(daily.IsOpen, Is.False);
            Assert.That(boot.Lobby!.Visible, Is.True);

            Assert.That(boot.PressDailyCard(), Is.True, "tarjeta sellada: el calendario del mes");
            Assert.That(daily.HandleBack(), Is.True);
            Assert.That(daily.Calendar.Visible, Is.False);
            Assert.That(daily.HandleBack(), Is.False, "nada abierto");
        }

        [UnityTest]
        public IEnumerator StreakOfSeven_BeforeLevel30_ThePororocaWaitsInTheStock_RQ14()
        {
            var initial = new DailyChallengeRecord { LastClaimedDate = "2026-10-18", StreakDays = 6, StampsMonth = "2026-10" };
            for (int d = 12; d <= 18; d++)
            {
                initial.StampDays.Add(d);
            }

            _store = new InMemoryDailyChallengeStore(initial);
            GameBootstrap.DailyStoreOverride = _store;
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            int shuffle = boot.Profile!.Stock.Shuffle;
            Assert.That(boot.DailyCard!.StreakText, Is.EqualTo("6/7"));

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Win(boot, daily);
            StepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();

            Assert.That(daily.LastClaim!.Value.ShufflePaid, Is.EqualTo(1));
            Assert.That(boot.Profile.Stock.Shuffle, Is.EqualTo(shuffle + 1), "entra en el stock (R-Q14 a)");
            Assert.That(daily.Calendar.StatusText, Is.EqualTo("+1 Pororoca · se desbloquea en el nivel 30"));
            Assert.That(daily.Calendar.StampedDays.Count, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator GrayUndoRefill_WithoutTheStore_ShowsTheStoreNotice()
        {
            // Observación 6 de la jornada 5: «+3 Deshacer · 30 semillas» en gris también responde en el Reto (DailyGameActions es
            // IStoreNoticeActions): late su etiqueta y, sin tienda, el aviso «Las compras no están disponibles ahora».
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            int seeds = boot.Profile!.Wallet.Seeds;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            Assert.That(daily.Actions, Is.InstanceOf<IStoreNoticeActions>());
            ToPlaying(boot, daily);
            boot.Services!.StoreLedger.Config.BizIapEnabled = false;
            LevelFlow flow = daily.Flow!;
            StuckContext ctx = flow.BuildStuckContext(false, true, GeoTier.T3);
            ctx.UndoUnlimited = false;
            ctx.UndoFree = 0;
            ctx.UndoStock = 0;
            ctx.UndoAdReady = false;
            ctx.Seeds = 0;
            GameScreen view = daily.View!;
            view.ShowStuckVariant(StuckModalModel.Build(ctx, flow.Boosters));
            view.Stuck.Panel.parent.gameObject.SetActive(true); // el panel a la vista, como con un bloqueo de verdad
            view.Stuck.Panel.gameObject.SetActive(true);
            Assert.That(view.Stuck.View!.UndoRefillMissing, Is.GreaterThan(0), "condición: «+3 Deshacer» en gris");

            view.Stuck.RefillSeedsButton.onClick.Invoke();
            Assert.That(view.Stuck.RefillSeedsPulse.Running, Is.True, "late su etiqueta");
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(1), "el aviso de la tienda, también en el Reto");
            Assert.That(boot.Profile.Wallet.Seeds, Is.EqualTo(seeds), "sin cobrar nada");
        }

        [UnityTest]
        public IEnumerator Restart_IsFreeAndUnlimited_SameBoard()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Profile profile = boot.Profile!;
            int canoes = profile.Wallet.Canoes.Count;
            int seeds = profile.Wallet.Seeds;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            LevelFlow flow = daily.Flow!;
            string board = flow.DailyBoardId!;

            for (int attempt = 2; attempt <= 4; attempt++)
            {
                ToPlaying(boot, daily);
                PourFirstMove(boot, daily);
                Assert.That(daily.Actions.PressRestart(), Is.True, "Volver a la orilla");
                Step(boot, Frame);
                Assert.That(flow.DailyAttempt, Is.EqualTo(attempt));
                Assert.That(flow.DailyBoardId, Is.EqualTo(board), "mismo tablero (06 §4.1)");
            }

            Assert.That(profile.Wallet.Canoes.Count, Is.EqualTo(canoes), "reintentos sin canoas");
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(seeds));
            Assert.That(daily.View!.LevelTitle.text, Is.EqualTo("Reto del río"));
            Assert.That(profile.Stats.FailsHard, Is.EqualTo(0));
            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
        }

        [UnityTest]
        public IEnumerator SameDay_TheGameResumesAfterReopening_RQ4()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);
            Assert.That(_store.Persisted.InProgress, Is.Not.Null);
            Assert.That(_store.Persisted.InProgress!.State.Pours, Is.EqualTo(1), "el vertido se guardó en el bloque daily_challenge");
            Assert.That(boot.Profile!.LevelState, Is.Null, "no en el level_state de la campaña");

            // Se cierra la app (sin salir) y se vuelve a abrir el mismo día, unas horas más tarde.
            _store.Reopen();
            _clock.Advance(TimeSpan.FromHours(5));
            yield return Boot();
            boot = GameBootstrap.Current!;
            DailyChallengeCard card = boot.DailyCard!;
            Assert.That(card.State, Is.EqualTo(DailyCardState.InProgress));
            Assert.That(card.StateText, Is.EqualTo("Seguir"));

            Assert.That(boot.PressDailyCard(), Is.True);
            daily = boot.DailyScreen!;
            Assert.That(daily.Resumed, Is.True, "se reanuda (R-Q4)");
            Assert.That(daily.Flow!.Session.AppliedPours, Is.EqualTo(1), "con el vertido de antes");
            Assert.That(daily.View!.LevelTitle.text, Is.EqualTo("Reto del río"));
            Assert.That(boot.LastDailyDayEnd, Is.Null, "el mismo día no termina ningún día (R-Q15 a)");

            Win(boot, daily);
            Assert.That(daily.LastClaimOutcome, Is.EqualTo(ClaimOutcome.Confirmed));
        }

        [UnityTest]
        public IEnumerator OtherDay_TheSavedGameIsDiscarded_RQ4()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            string yesterdayBoard = daily.Flow!.DailyBoardId!;
            ToPlaying(boot, daily);
            PourFirstMove(boot, daily);

            _store.Reopen();
            _clock.Advance(TimeSpan.FromDays(1));
            yield return Boot();
            boot = GameBootstrap.Current!;

            Assert.That(boot.DailyService!.Resumable, Is.Null, "al abrir la app se descarta la de ayer (R-Q4)");
            Assert.That(boot.LastDailyDayEnd.HasValue, Is.True, "DayEnded llega antes del primer DiscardStale (R-Q15 a)");
            Assert.That(boot.LastDailyDayEnd!.Value.DateKey, Is.EqualTo("2026-10-19"));
            Assert.That(boot.LastDailyDayEnd.Value.Result, Is.EqualTo(DailyResult.Fail), "el día terminó sin victoria");
            Assert.That(boot.LastDailyDayEnd.Value.IsReported, Is.True);
            Assert.That(_store.Load().InProgress == null || _store.Load().InProgress!.DateKey == "2026-10-20", Is.True);
            Assert.That(boot.DailyCard!.State, Is.EqualTo(DailyCardState.Play));
            Assert.That(boot.DailyCard.TitleText, Is.EqualTo("Reto del río · 20 oct"));

            Assert.That(boot.PressDailyCard(), Is.True);
            daily = boot.DailyScreen!;
            Assert.That(daily.Resumed, Is.False);
            Assert.That(daily.Flow!.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(daily.Flow.DailyBoardId, Is.Not.EqualTo(yesterdayBoard), "el tablero del día nuevo");
        }

        // ---- Ayudas ----

        // Arranque con el perfil del test (no es la primera vez: el inicio) y el enganche E2 simulado; los relojes del arranque y
        // del Reto los lleva el test.
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
