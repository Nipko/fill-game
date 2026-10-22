#nullable enable
using System;
using System.Collections;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
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
    /// Enganches de la Bitácora aplicados (B4-1-profile, B4-2-flow y B4-3-bootstrap; 06 §2.3 y §4.2; D-033, añadido del
    /// 2026-10-20, puntos 5, 6 y 11) con la escena real: el arranque la crea sobre el bloque <c>calendar</c> del perfil, ganar un
    /// nivel de campaña o del Reto apunta el día jugado y la libreta sale sola al volver al inicio, en la victoria del 10 la
    /// libreta va antes del Mapa (Victoria → libreta → Mapa → Inicio) y Atrás de Android la cierra.
    /// </summary>
    public sealed class LogbookHooksTests
    {
        private const int Frame = PlayModeHelpers.FrameMs;

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.LogbookOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.L10SequenceOverride = null;
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator WinningALevel_MarksTheDay_InTheProfile_AndTheNotebookWaitsAtTheLobby()
        {
            var profiles = new MemoryProfileStore(VictorySetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = profiles;
            var progress = new MemoryProgressStore(26, 25);
            yield return PlayModeHelpers.LoadGame(26, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Loc.Configure(LogbookTestTables.Catalog(), LocLocale.SourceLocale, true);
            Assert.That(boot.Logbook, Is.Not.Null, "B4-3: SetUpLogbook en el arranque");
            Assert.That(boot.Logbook!.Store, Is.InstanceOf<ProfileLoginCalendarStore>(), "B4-1: el bloque calendar del perfil");
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Waiting));

            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Ready), "B4-2: la victoria termina el nivel del día");
            Assert.That(boot.LogbookVictoryRowDue, Is.True, "la victoria ya sabe que la libreta está lista (Q-B11)");
            Profile profile = boot.Profile!;
            string today = boot.Logbook.Record.PlayedDate!;
            Assert.That(profile.Calendar!.PlayedDate, Is.EqualTo(today));
            Assert.That(ProfileJson.Deserialize(profiles.Json!).Calendar!.PlayedDate, Is.EqualTo(today), "en la escritura del fin de nivel");

            Assert.That(boot.PressHome(), Is.True);
            boot.LogbookRunner!.enabled = false;
            boot.TickLogbook(Frame);
            Assert.That(boot.LogbookScreen!.Visible, Is.True, "el único takeover de la sesión al volver al inicio (D-033, punto 5)");
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Takeover));

            Assert.That(boot.PressAndroidBack(), Is.True, "B4-3: Atrás de Android cierra la libreta");
            Assert.That(boot.LogbookScreen.Visible, Is.False);
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Ready), "cerrar sin elegir la deja pendiente");
        }

        [UnityTest]
        public IEnumerator WinningTheReto_AlsoMarksTheDay()
        {
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            Profile start = VictorySetup.Profile(0);
            start.Progress.MaxLevel = 25;
            start.Progress.NextLevel = 26;
            start.Progress.MapSeenLevel = 25;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(start));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.DailyRunner!.enabled = false;
            boot.LogbookRunner!.enabled = false;
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Waiting));

            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            boot.TickLogbook(Frame); // el flujo del Reto se crea al abrir la partida: la Bitácora se suscribe al vuelo
            WinDaily(boot, daily);
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Ready), "D-033, punto 5: campaña o Reto");
            Assert.That(boot.Profile!.Calendar!.PlayedDate, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TheVictoryOf10_ByNext_OpensTheNotebookBeforeTheMap()
        {
            GameBootstrap.L10SequenceOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            var progress = new MemoryProgressStore(10, 9);
            yield return PlayModeHelpers.LoadGame(10, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Loc.Configure(LogbookTestTables.Catalog(), LocLocale.SourceLocale, true);
            Assert.That(boot.App!.L10MapAvailable, Is.True, "la escena trae el Mapa");
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Ready), "con el 10 ganado, la Bitácora ya existe");

            Assert.That(boot.PressNext(), Is.True);
            LogbookScreen screen = boot.LogbookScreen!;
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(screen.Visible, Is.True, "06 §2.3, pantalla 2: la libreta, antes del Mapa");
            Assert.That(screen.Source, Is.EqualTo(LogbookOpenSource.L10));
            Assert.That(screen.IntroShown, Is.True);
            Assert.That(screen.ButtonText, Is.EqualTo("Seguir"));
            Assert.That(boot.RiverMapView!.Visible, Is.False);
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0), "dentro del hito no gasta el cupo");

            Assert.That(screen.PressToday(), Is.True);
            screen.CardButton(LoginCalendarChoice.Seeds).onClick.Invoke();
            Assert.That(boot.Profile!.Calendar!.NextDay, Is.EqualTo(2), "la página 1 sellada");
            screen.CloseButton.onClick.Invoke();
            Assert.That(screen.Visible, Is.False);
            Assert.That(boot.RiverMapView.Visible, Is.True, "«Seguir» lleva al Mapa del 10");
            Assert.That(boot.RiverMapView.Source, Is.EqualTo(MapOpenSource.L10));
        }

        // Gana la partida del Reto abierta, con el solver (como DailyHooksTests).
        private static void WinDaily(GameBootstrap boot, DailyChallengeScreen daily)
        {
            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere(); // salta la intro
            StepUntil(boot, () => flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput, 10000, "fin de la intro del Reto");
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
    }
}
