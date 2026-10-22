#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Analytics;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
using AF.Core.Rules;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La UI de los avisos en la escena <c>Game</c> real (06 §8.1; nota de B6, §3 y §6): la tarjeta del pre-prompt sale al dejar la
    /// victoria que toca (Siguiente o Inicio, B6-Q16 (a)) y no al entrar en ella, ni en la 1.ª sesión, ni dos veces en una sesión,
    /// ni con la banda &lt;13; «Ahora no», «Avísame» (con el diálogo nativo del programador falso) y Atrás; la tarjeta de oferta de
    /// esa victoria no sale (la Mochila pasa a la siguiente, 06 §8.1) y la entrada al inicio que sigue no apila la libreta
    /// (<c>TakeoverQueue</c>); <c>af_push_prompt</c> (08 §8.2). Y la fila «Avisos» de Ajustes en cada estado: apagada, encendida,
    /// bloqueada en el teléfono (con el enlace a sus ajustes y la vuelta con el permiso dado), con su etiqueta y su anuncio de
    /// TalkBack (B9).
    /// </summary>
    public sealed class PushUiTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        private ManualClock _clock = null!;
        private FakeNotificationScheduler _scheduler = null!;
        private InMemoryPushSettingsStore _push = null!;
        private InMemorySessionStore _sessions = null!;
        private FakeAnalyticsSink _sink = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = _clock;
            _scheduler = new FakeNotificationScheduler(_clock);
            _push = new InMemoryPushSettingsStore();
            _sessions = new InMemorySessionStore(new SessionRecord { Sessions = 1, LastSeenAt = T0.AddHours(-20) }); // el arranque abre la 2.ª
            _sink = new FakeAnalyticsSink();
            GameBootstrap.NotificationSchedulerOverride = _scheduler;
            GameBootstrap.PushSettingsStoreOverride = _push;
            GameBootstrap.SessionStoreOverride = _sessions;
            GameBootstrap.AnalyticsSinkOverride = _sink;
            GameBootstrap.PrePromptCardOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.A11yHostOverride = null;
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static int CountOf(FakeAnalyticsSink sink, string name)
        {
            int n = 0;
            for (int i = 0; i < sink.Names.Count; i++)
            {
                if (sink.Names[i] == name)
                {
                    n++;
                }
            }

            return n;
        }

        private static IEnumerator Load(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(level));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        // Gana el nivel cargado hasta VICTORY_SCREEN y deja que las sesiones y el Mercado vean la entrada.
        private static GameBootstrap Win()
        {
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<SessionRunner>().enabled = false;
            if (boot.GetComponent<MarketRunner>() is MarketRunner market)
            {
                market.enabled = false;
            }

            VictorySetup.WinToVictoryScreen(boot);
            boot.TickSessions();
            return boot;
        }

        [UnityTest]
        public IEnumerator SecondSession_TheCardWaitsForNext_ThenNotNowGoesOn()
        {
            yield return Load(12);
            GameBootstrap boot = Win();
            PushPrePromptCard card = boot.PrePromptCard!;
            Assert.That(boot.PrePromptPending, Is.EqualTo(PrePromptKind.First), "2.ª sesión con el 11 superado antes de ganar el 12 (06 §8.1)");
            Assert.That(card.Visible, Is.False, "B6-Q16 (a): no al entrar en la victoria");

            Assert.That(boot.PressNext(), Is.True);
            Assert.That(card.Visible, Is.True, "sale al pulsar Siguiente, encima de la victoria");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria espera debajo");
            Assert.That(card.BodyText, Is.EqualTo(PushText.PrePromptBody));
            Assert.That(boot.PressNext(), Is.False, "otro Siguiente con la tarjeta delante no hace nada");

            card.DeclineButton.onClick.Invoke();
            Assert.That(card.Visible, Is.False);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(13), "«Ahora no» sigue el camino de Siguiente");
            Assert.That(_push.Load().PrePromptShown, Is.True);
            Assert.That(_push.Load().Granted, Is.False);
            Assert.That(_scheduler.PermissionRequests, Is.EqualTo(0), "sin «Avísame» no hay diálogo nativo");
            AnalyticsEvent e = _sink.Last("af_push_prompt")!;
            Assert.That(e.Find("step")!.Value.Text, Is.EqualTo("pre"));
            Assert.That(e.Find("result")!.Value.Text, Is.EqualTo("declined"));
            Assert.That(e.Find("origin")!.Value.Text, Is.EqualTo("first"), "origin, dado de alta en 08 §8.2 (añadido de la UI de los avisos)");
        }

        [UnityTest]
        public IEnumerator Accept_AsksTheNativeDialog_AndGrants()
        {
            yield return Load(12);
            GameBootstrap boot = Win();
            Assert.That(boot.PressNext(), Is.True);
            boot.PrePromptCard!.AcceptButton.onClick.Invoke();
            Assert.That(_scheduler.PermissionRequests, Is.EqualTo(1), "«Avísame» pide POST_NOTIFICATIONS");
            Assert.That(_push.Load().Granted, Is.True);
            Assert.That(boot.Push!.CanNotify, Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(13));
            Assert.That(CountOf(_sink, "af_push_prompt"), Is.EqualTo(2), "pre (accepted) y native (granted)");
            Assert.That(_sink.Last("af_push_prompt")!.Find("result")!.Value.Text, Is.EqualTo("granted"));
        }

        [UnityTest]
        public IEnumerator Accept_WaitsForTheNativeAnswer_BeforeGoingOn()
        {
            _scheduler.NativeAnswer = PushPermission.NotRequested; // el diálogo se queda abierto
            yield return Load(12);
            GameBootstrap boot = Win();
            Assert.That(boot.PressHome(), Is.True);
            boot.PrePromptCard!.AcceptButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "con el diálogo nativo delante, la victoria espera");
            Assert.That(boot.PressHome(), Is.False);
            _scheduler.Answer(PushPermission.Denied);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "con la respuesta, Inicio sigue");
            Assert.That(_push.Load().Granted, Is.False);
            Assert.That(_sink.Last("af_push_prompt")!.Find("result")!.Value.Text, Is.EqualTo("denied"));
        }

        [UnityTest]
        public IEnumerator Accept_ANativeDialogThatNeverAnswers_LetsTheVictoryGoOnOnce()
        {
            _scheduler.NativeAnswer = PushPermission.NotRequested; // el diálogo nunca contesta
            yield return Load(12);
            GameBootstrap boot = Win();
            Assert.That(boot.PressNext(), Is.True);
            boot.PrePromptCard!.AcceptButton.onClick.Invoke();
            LevelSession won = boot.Flow!.Session;

            // Una vuelta de segundo plano (un fotograma enorme) cuenta como mucho 100 ms.
            boot.TickPushUi(60000f);
            for (int t = 100; t < GameBootstrap.PrePromptNativeWaitMs - 200f; t += PlayModeHelpers.FrameMs)
            {
                boot.TickPushUi(PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow.Session, Is.SameAs(won), "antes del tope, la victoria espera al diálogo");
            for (int t = 0; t < 400; t += PlayModeHelpers.FrameMs)
            {
                boot.TickPushUi(PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(13), "con el tope, sigue el camino de Siguiente");
            LevelSession next = boot.Flow.Session;
            _scheduler.Answer(PushPermission.Allowed); // la respuesta llega tarde
            Assert.That(_push.Load().Granted, Is.True, "y se guarda");
            Assert.That(boot.Flow.Session, Is.SameAs(next), "pero el camino no se repite");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(13));
        }

        [UnityTest]
        public IEnumerator TheAutoNextAfterTheDoubleHarvest_BringsTheCard_AndGoesOnOnce()
        {
            // D-011, punto 4: tras el video de la Cosecha doble, la victoria pasa sola por el camino de Siguiente: ahí sale la tarjeta.
            LaunchOptions.LevelOffsetOverride = 11;
            LaunchOptions.AdPresenterOverride = new ImmediateAdPresenter();
            yield return Load(12);
            GameBootstrap boot = Win();
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 900);
            Assert.That(view.DoubleHarvestShown, Is.True);
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(view.AutoNextPending, Is.True);
            PlayModeHelpers.Step(boot, 1500); // vence la espera: TakeAutoNext → PressNext
            Assert.That(boot.PrePromptCard!.Visible, Is.True, "el avance solo trae la tarjeta");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(12));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria espera debajo");
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(12), "sin la respuesta no avanza");

            boot.PrePromptCard.DeclineButton.onClick.Invoke();
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(13));
            LevelSession loaded = boot.Flow.Session;
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(boot.Flow.Session, Is.SameAs(loaded), "N + 1 una sola vez");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(13));
        }

        [UnityTest]
        public IEnumerator BackIsNotNow()
        {
            yield return Load(12);
            GameBootstrap boot = Win();
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.PrePromptCard!.Visible, Is.False);
            Assert.That(_push.Load().PrePromptShown, Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(13));
        }

        [UnityTest]
        public IEnumerator FirstSession_OrBeforeLevel10_NoCard()
        {
            _sessions = new InMemorySessionStore(); // arranque de instalación: sesión 1
            GameBootstrap.SessionStoreOverride = _sessions;
            yield return Load(12);
            GameBootstrap boot = Win();
            Assert.That(boot.PrePromptPending, Is.EqualTo(PrePromptKind.None), "nunca en la 1.ª sesión (push_preprompt_session = 2)");
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.PrePromptCard!.Visible, Is.False);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(13));

            GameBootstrap.ResetSessionOverrides();
            SetUp();
            yield return Load(10);
            boot = Win();
            Assert.That(boot.PrePromptPending, Is.EqualTo(PrePromptKind.None), "la victoria del 10 nunca (06 §2.3): el 10 aún no estaba superado");
        }

        [UnityTest]
        public IEnumerator OnePerSession_TheSecondWaitsForTheNextSession()
        {
            yield return Load(12);
            GameBootstrap boot = Win();
            Assert.That(boot.PressNext(), Is.True);
            boot.PrePromptCard!.DeclineButton.onClick.Invoke();

            // Ocho días después del primero, el segundo tocaría por las reglas de B6 (06 §8.1, regla 2)…
            PushSettingsRecord record = _push.Load();
            record.PrePromptAt = T0.AddDays(-8);
            _push.Save(record);
            Assert.That(PushPrePromptRules.Due(_push.Load(), boot.Push!.Config, _scheduler.Permission, new PrePromptMoment(2, 12, 0, T0)),
                Is.EqualTo(PrePromptKind.Second));
            Assert.That(boot.PrePromptDueNow(12, out _), Is.EqualTo(PrePromptKind.None), "…pero ya salió uno en esta sesión");

            // …y en la sesión siguiente (vuelta tras ≥ 30 min, Q-M4 (a)), sí.
            boot.SessionsPaused(true);
            _clock.Advance(TimeSpan.FromMinutes(31));
            boot.SessionsPaused(false);
            Assert.That(boot.Sessions!.Sessions, Is.EqualTo(3));
            Assert.That(boot.PrePromptDueNow(12, out _), Is.EqualTo(PrePromptKind.Second));
        }

        [UnityTest]
        public IEnumerator ASecondAtStreak3_LeftByAnotherPath_IsRemembered_ForTheNextSession() // D-033, UI de los avisos (b)
        {
            // El primero ya salió (y se rechazó) hace un día; la Crecida en 3 al ganar: toca el segundo por el hito.
            _push.Save(new PushSettingsRecord { PrePromptShown = true, PrePromptAt = T0.AddDays(-1) });
            Profile profile = ProfileAt(12);
            profile.Streak = new StreakData { Wins = 3 };
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            GameBootstrap boot = Win();
            Assert.That(boot.PrePromptPending, Is.EqualTo(PrePromptKind.Second), "el hito 3 tras el primero (06 §8.1, regla 2)");

            // La victoria se deja por otro camino: lo apuntado se olvida, pero el hito se recuerda (no solo los 7 días).
            boot.ForgetPrePromptAtVictory(false);
            Assert.That(boot.PrePromptPending, Is.EqualTo(PrePromptKind.None));
            Assert.That(_push.Load().SecondDueSession, Is.EqualTo(boot.Sessions!.Sessions));
            Assert.That(_push.Load().SecondPromptShown, Is.False);

            // En la sesión siguiente, con la racha ya en 4 y antes de los 7 días, el segundo.
            profile.Streak.Wins = 4;
            boot.SessionsPaused(true);
            _clock.Advance(TimeSpan.FromMinutes(31));
            boot.SessionsPaused(false);
            Assert.That(boot.PrePromptDueNow(12, out _), Is.EqualTo(PrePromptKind.Second));
        }

        [UnityTest]
        public IEnumerator TheCard_PostponesTheVictoryOffer()
        {
            yield return Load(12);
            GameBootstrap boot = Win();
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.OfferCardView!.Visible, Is.False, "06 §8.1: el pre-prompt va primero; la Mochila pasa a la siguiente victoria");
            Assert.That(boot.Offers!.PickAtVictory(new VictoryOfferContext(12, 2, 1)), Is.EqualTo(OfferKind.Starter), "la Mochila sigue pendiente");
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.PrePromptCard!.Visible, Is.True);
        }

        [UnityTest]
        public IEnumerator HomeAfterTheCard_DoesNotStackTheLogbook()
        {
            GameBootstrap.LogbookOverride = true;
            yield return Load(26);
            GameBootstrap boot = Win();
            Assert.That(boot.PressHome(), Is.True);
            Assert.That(boot.PrePromptCard!.Visible, Is.True);
            boot.PrePromptCard.DeclineButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            for (int i = 0; i < 6; i++)
            {
                yield return null;
            }

            Assert.That(boot.LogbookScreen == null || !boot.LogbookScreen.Visible, Is.True, "la libreta no sale encima del pre-prompt");
            Assert.That(boot.Takeovers == null || boot.Takeovers.ShownThisSession == 0, Is.True, "y no gasta el cupo de la sesión");
        }

        [UnityTest]
        public IEnumerator DailyVictory_TheCardComesWithTheCalendarHome()
        {
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(26)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<SessionRunner>().enabled = false;
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere();
            StepDailyUntil(boot, () => flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput, "fin de la intro del Reto");
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                StepDailyUntil(boot, () => flow.AcceptsBoardInput, "tablero listo");
                daily.Actions.TapCuia(move.Source);
                daily.Actions.TapCuia(move.Target);
            }

            StepDailyUntil(boot, () => daily.Won && daily.Calendar.Visible, "calendario de sellos tras la victoria");
            Assert.That(boot.PrePromptPending, Is.EqualTo(PrePromptKind.First), "la victoria del Reto también cuenta (06 §8.1; B6)");
            daily.Calendar.HomeButton.onClick.Invoke();
            Assert.That(boot.PrePromptCard!.Visible, Is.True, "sale al dejar el Reto con «Inicio»");
            Assert.That(daily.IsOpen, Is.True, "el Reto espera debajo");
            daily.Calendar.HomeButton.onClick.Invoke();
            Assert.That(boot.PrePromptCard.ShownCount, Is.EqualTo(1), "otro «Inicio» con la tarjeta delante no hace nada");
            boot.PrePromptCard.DeclineButton.onClick.Invoke();
            Assert.That(daily.IsOpen, Is.False, "«Ahora no» sigue al inicio");
            Assert.That(_push.Load().PrePromptShown, Is.True);
        }

        private static void StepDailyUntil(GameBootstrap boot, Func<bool> done, string what)
        {
            for (int t = 0; t < 20000 && !done(); t += PlayModeHelpers.FrameMs)
            {
                boot.TickDaily(PlayModeHelpers.FrameMs);
                boot.TickSessions();
            }

            Assert.That(done(), Is.True, "tiempo agotado: " + what);
        }

        [UnityTest]
        public IEnumerator Under13_NoCardAndNoRow()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            Profile profile = ProfileAt(12);
            profile.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, T0, false, false, AgeBand.Under13);
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentBlocked, Is.True);
            Assert.That(boot.PrePromptCard == null || !boot.PrePromptCard.Visible, Is.True);
            Assert.That(boot.PrePromptDueNow(12, out _), Is.EqualTo(PrePromptKind.None), "B12: con <13 nunca sale");
            Assert.That(boot.PushRowNow, Is.EqualTo(PushRowState.Hidden));
            GameBootstrap.ResetConsentOverrides();
        }

        [UnityTest]
        public IEnumerator SettingsRow_OffOnAndBlocked()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(12)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.PushShown, Is.True);
            Assert.That(settings.PushState, Is.EqualTo(PushRowState.Off));
            Assert.That(settings.PushMarked, Is.False);
            Assert.That(settings.PushLabel, Is.EqualTo(PushText.Row));
            Assert.That(settings.PushStateText, Is.EqualTo(PushText.State(PushRowState.Off)));
            Assert.That(settings.PushA11yLabel, Is.EqualTo(PushText.A11yLabel(PushRowState.Off)));
            Assert.That(settings.PushOpenPhoneShown, Is.False);

            // Encender: el diálogo nativo (falso: acepta) y la fila marcada, con su anuncio.
            settings.PushButton!.onClick.Invoke();
            Assert.That(_scheduler.PermissionRequests, Is.EqualTo(1));
            Assert.That(_push.Load().Granted, Is.True);
            Assert.That(settings.PushState, Is.EqualTo(PushRowState.On));
            Assert.That(settings.PushMarked, Is.True);
            Assert.That(host.Announcements, Does.Contain(PushText.A11yLabel(PushRowState.On)));
            Assert.That(_sink.Last("af_push_prompt")!.Find("step")!.Value.Text, Is.EqualTo("native"));

            // Apagar: cancela lo programado y guarda settings.push.
            int cancels = _scheduler.CancelCount;
            settings.PushButton.onClick.Invoke();
            Assert.That(_push.Load().Granted, Is.False);
            Assert.That(_scheduler.CancelCount, Is.EqualTo(cancels + 1));
            Assert.That(settings.PushState, Is.EqualTo(PushRowState.Off));
            Assert.That(host.Announcements, Does.Contain(PushText.A11yLabel(PushRowState.Off)));

            // Encender otra vez con el permiso ya dado: sin diálogo.
            settings.PushButton.onClick.Invoke();
            Assert.That(_scheduler.PermissionRequests, Is.EqualTo(1));
            Assert.That(settings.PushState, Is.EqualTo(PushRowState.On));
        }

        [UnityTest]
        public IEnumerator SettingsRow_DeniedStaysOff_BlockedOpensThePhone_AndComesBackOn()
        {
            _scheduler.NativeAnswer = PushPermission.Blocked; // «No volver a preguntar»
            int opened = 0;
            GameBootstrap.OpenPhoneSettingsOverride = () =>
            {
                opened++;
                return true;
            };
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(12)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            settings.PushButton!.onClick.Invoke();
            Assert.That(_push.Load().Granted, Is.False);
            Assert.That(settings.PushState, Is.EqualTo(PushRowState.Blocked));
            Assert.That(settings.PushStateText, Is.EqualTo(PushText.State(PushRowState.Blocked)));
            Assert.That(settings.PushOpenPhoneShown, Is.True);
            Assert.That(settings.PushA11yLabel, Is.EqualTo(PushText.A11yLabel(PushRowState.Blocked)));

            settings.PushOpenPhoneButton!.onClick.Invoke();
            Assert.That(opened, Is.EqualTo(1), "abre los ajustes de la app en el teléfono");
            settings.PushButton.onClick.Invoke();
            Assert.That(opened, Is.EqualTo(2), "la fila bloqueada también los abre");

            // El jugador los activa en el teléfono y vuelve: se encienden como desde Ajustes.
            _scheduler.Permission = PushPermission.Allowed;
            boot.PushUiResumed();
            Assert.That(_push.Load().Granted, Is.True);
            Assert.That(settings.PushState, Is.EqualTo(PushRowState.On));
            Assert.That(settings.PushOpenPhoneShown, Is.False);
        }

        [UnityTest]
        public IEnumerator SettingsRow_BlockedWithoutVisitingThePhone_DoesNotTurnOnAlone()
        {
            _scheduler.Permission = PushPermission.Blocked;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(12)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.PushState, Is.EqualTo(PushRowState.Blocked));
            _scheduler.Permission = PushPermission.Allowed;
            boot.PushUiResumed();
            Assert.That(_push.Load().Granted, Is.False, "B6-Q3: encenderlos en el teléfono no salta el opt-in");
            Assert.That(boot.SettingsView.PushState, Is.EqualTo(PushRowState.Off));
        }
    }
}
