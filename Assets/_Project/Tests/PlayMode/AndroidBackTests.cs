#nullable enable
using System;
using System.Collections;
using System.Threading;
using AF.Boosters;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Atrás de Android en el nivel, la pausa y el inicio (02 §3.5; observación 21 de la jornada 6, decisión del dueño del
    /// 2026-10-30): jugando abre la pausa como su botón; en la pausa es «Continuar»; en «¿Salir del nivel?» cancela y vuelve a la
    /// pausa sin gastar la canoa; con un vertido que resuelve el nivel no hace nada; en el inicio sin nada encima manda la app a
    /// segundo plano (puerto falso) una sola vez aunque se pulse dos veces seguidas, y otra vez tras volver de segundo plano; un
    /// doble Atrás rápido no lanza excepciones; ¿Reiniciar? abierto desde la pausa vuelve a ella; el Salto del Boto, la tarjeta
    /// de Morpho y la hoja y la tarjeta de Barajar se cierran sin coste; Sin canoas en el nivel es su X (D-063). Cada pulsación
    /// escribe <c>AF_STATE back</c>. Aguas estancadas, ¿Seguro? y Nivel fallido: <see cref="BackButtonTests"/>.
    /// </summary>
    public sealed class AndroidBackTests : UiInputTestFixture
    {
        private FakeAppTaskPort _task = null!;

        /// <inheritdoc />
        public override void Setup()
        {
            base.Setup();
            FunnelSetup.Hooks();
            _task = new FakeAppTaskPort();
            GameBootstrap.AppTaskPortOverride = _task;
            GameBootstrap.ResetSkipOverrides();
        }

        /// <inheritdoc />
        public override void TearDown()
        {
            GameBootstrap.AppTaskPortOverride = null;
            GameBootstrap.ResetSkipOverrides();
            GameScreen.ShuffleOverride = null;
            GameScreen.SimulatedScreen = null;
            if (GameBootstrap.Current != null)
            {
                GameBootstrap.Current.enabled = false;
            }

            LaunchOptions.Reset();
            base.TearDown();
        }

        private static IEnumerator Level31()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
        }

        [UnityTest]
        public IEnumerator Level_BackOpensThePause_AndBackAgainResumes()
        {
            yield return Level31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.Pause.Visible, Is.False);

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(view.Pause.Visible, Is.True, "Atrás abre la pausa, como su botón");
            Assert.That(boot.LastBackScreen, Is.EqualTo("level"));
            Assert.That(boot.LastBackAction, Is.EqualTo("pause"));
            Assert.That(DevProbe.LastBackLine, Does.StartWith("AF_STATE {\"event\":\"back\",\"t\":")
                .And.EndWith(",\"screen\":\"level\",\"action\":\"pause\",\"reto\":false}"));

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(view.Pause.Visible, Is.False, "en la pausa, Atrás es «Continuar»");
            Assert.That(boot.LastBackScreen, Is.EqualTo("pause"));
            Assert.That(boot.LastBackAction, Is.EqualTo("resume"));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "sigue en el nivel");
            Assert.That(_task.Calls, Is.EqualTo(0), "el nivel nunca manda la app al fondo");
        }

        [UnityTest]
        public IEnumerator QuitConfirm_BackCancels_BackToThePause_WithoutSpendingTheCanoe()
        {
            yield return Level31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.QuitVisible, Is.True, "desde el 31, «¿Salir del nivel?»");

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("quit_confirm"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel_to_pause"));
            Assert.That(view.Confirm.Visible, Is.False);
            Assert.That(view.Pause.Visible, Is.True, "vuelve a la pausa");
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5), "sin gastar la canoa");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(view.Pause.Visible, Is.False, "y de la pausa, al juego");
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator RestartConfirm_BackCancels_WithoutRestarting()
        {
            yield return Level31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            int pours = flow.Session.AppliedPours;
            // ¿Reiniciar? (04 §2.7) sale con ≥ restart_confirm_min_pours vertidos: aquí se abre directamente, como lo haría la cabecera.
            view.Confirm.Show(ConfirmKind.Restart, false, false, view.CanvasWidth, view.CardCanvasHeight);
            Assert.That(view.Confirm.Visible, Is.True);
            Assert.That(view.Confirm.Kind, Is.EqualTo(ConfirmKind.Restart));

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("restart_confirm"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel"));
            Assert.That(view.Confirm.Visible, Is.False);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(pours), "no reinicia");
        }

        [UnityTest]
        public IEnumerator Level_BackWhileTheLastPourResolvesTheWin_DoesNothing()
        {
            yield return Level31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            AF.Core.Rules.Pour last = VictorySetup.PlayAllButTheLastPour(boot);
            boot.TapCuia(last.Source);
            boot.TapCuia(last.Target);
            Assert.That(flow.IsResolvingWin, Is.True);

            Assert.That(boot.PressAndroidBack(), Is.False, "sin pausa que la victoria cerraría al momento");
            Assert.That(boot.LastBackAction, Is.EqualTo("none"));
            Assert.That(view.Pause.Visible, Is.False);
            PlayModeHelpers.StepUntilIdle(boot, 15000);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration).Or.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue su curso");
            Assert.That(boot.PressAndroidBack(), Is.False, "ni durante la victoria");
            Assert.That(view.Pause.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Level_DoubleBackInOneFrame_AndOnConsecutiveFrames_NoExceptions()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Level31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.enabled = true;

            // Dos pulsaciones encoladas antes del mismo fotograma: cuentan como una (la pausa se abre una vez).
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;
            Assert.That(view.Pause.Visible, Is.True, "abre la pausa");
            PlayModeHelpers.TakeClock(boot);

            // Abrir y cerrar en dos fotogramas seguidos, y lo mismo llamando a mano dos veces en el mismo fotograma.
            boot.enabled = true;
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;
            Assert.That(view.Pause.Visible, Is.False, "la cierra");
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
        }

        [UnityTest]
        public IEnumerator Lobby_BackSendsTheAppToTheBackground_OnceForADoubleBack()
        {
            GameBootstrap.DailyPoolDisabledOverride = true;
            try
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(20)));
                GameBootstrap boot = GameBootstrap.Current!;
                Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
                Assert.That(boot.Lobby!.OverlayVisible, Is.False);

                Assert.That(boot.PressAndroidBack(), Is.True);
                Assert.That(boot.LastBackScreen, Is.EqualTo("lobby"));
                Assert.That(boot.LastBackAction, Is.EqualTo("background"));
                Assert.That(_task.Calls, Is.EqualTo(1), "moveTaskToBack, sin diálogo");
                Assert.That(boot.PressAndroidBack(), Is.True, "el segundo Atrás rápido se consume");
                Assert.That(_task.Calls, Is.EqualTo(1), "sin pedirlo dos veces");
                Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));

                // Se va al fondo y vuelve: el siguiente Atrás vuelve a mandarla al fondo (la guarda se reinicia en OnApplicationPause).
                boot.SendMessage("OnApplicationPause", true);
                boot.SendMessage("OnApplicationPause", false);
                Assert.That(boot.PressAndroidBack(), Is.True);
                Assert.That(boot.LastBackAction, Is.EqualTo("background"));
                Assert.That(_task.Calls, Is.EqualTo(2), "tras volver de segundo plano, otra vez");

                // Con Ajustes encima, Atrás la cierra primero y no manda la app al fondo.
                Assert.That(boot.PressSettings(), Is.True);
                Assert.That(boot.PressAndroidBack(), Is.True);
                Assert.That(boot.SettingsView!.Visible, Is.False);
                Assert.That(boot.LastBackScreen, Is.EqualTo("settings"));
                Assert.That(boot.LastBackAction, Is.EqualTo("close"));
                Assert.That(_task.Calls, Is.EqualTo(2), "cerrar Ajustes no manda la app al fondo");
            }
            finally
            {
                GameBootstrap.ResetDailyOverrides();
            }
        }

        [UnityTest]
        public IEnumerator RestartConfirm_OpenedFromThePause_BackReturnsToThePause()
        {
            yield return Level31();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            flow.Boosters.RestartConfirmMinPours = 1; // ¿Reiniciar? ya con un vertido (04 §2.7)
            Pour first = MorphoUiTests.OtherLegalPour(flow.Session.Current, default);
            boot.TapCuia(first.Source);
            boot.TapCuia(first.Target);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(flow.RestartNeedsConfirm, Is.True);
            int pours = flow.Session.AppliedPours;

            view.PauseButton.onClick.Invoke();
            view.Pause.RestartButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True);
            Assert.That(view.Confirm.Kind, Is.EqualTo(ConfirmKind.Restart));
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("restart_confirm"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel_to_pause"));
            Assert.That(view.Confirm.Visible, Is.False);
            Assert.That(view.Pause.Visible, Is.True, "vuelve a la pausa, como con «¿Salir?»");
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(pours), "no reinicia");

            // Desde la cabecera, en cambio, Atrás solo cancela.
            Assert.That(boot.PressAndroidBack(), Is.True);
            view.RestartButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel"));
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(pours));
        }

        [UnityTest]
        public IEnumerator SkipConfirm_BackCancels_WithoutChargingTheSeeds()
        {
            Profile profile = FunnelSetup.Profile(1000);
            profile.Wallet.Canoes.InfiniteUntil = FunnelSetup.T0 + TimeSpan.FromHours(2);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            for (int fail = 0; fail < 5; fail++)
            {
                FunnelSetup.GetStuck(boot);
                Assert.That(boot.Flow!.PressGiveUp(), Is.True);
                Assert.That(boot.Flow.PressRetry(), Is.True);
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 50);
            }

            Assert.That(boot.Flow!.SkipOffer.CanUse, Is.True, "el Salto, elegible y con saldo");
            GameScreen view = boot.View!;
            int seeds = boot.Wallet!.Seeds;
            int level = boot.Flow.LevelNumber;
            view.PauseButton.onClick.Invoke();
            view.Pause.SkipButton.onClick.Invoke();
            Assert.That(boot.SkipConfirmView!.Visible, Is.True);

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("skip_confirm"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel"));
            Assert.That(boot.SkipConfirmView.Visible, Is.False);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds), "sin cobrar las semillas");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(level), "sin saltar el nivel");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
        }

        [UnityTest]
        public IEnumerator MorphoCard_BackClosesIt_WithoutCharging()
        {
            const int level = 15;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            LevelFlow flow = boot.Flow!;
            for (int t = 0; t < 4000 && (flow.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 100);
            GameScreen view = boot.View!;

            // «No encuentro un camino desde aquí» (como MorphoUiTests.NotFound): el hilo de Morpho, retenido, pasa el corte.
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(BoosterConfig.HintCooldownMs + 1));
            using (var gate = new ManualResetEventSlim(false))
            {
                flow.Morpho = new MorphoBooster(flow.Boosters, flow.Clock, () => gate.Wait(20000));
                Assert.That(boot.PressHint(), Is.True);
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(flow.Boosters.HintTimeoutMs));
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                gate.Set();
                float start = UnityEngine.Time.realtimeSinceStartup;
                while (flow.HintThinking)
                {
                    Assert.That(UnityEngine.Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                    PlayModeHelpers.TickExact(boot, 4);
                    yield return null;
                }
            }

            Assert.That(view.HintAnswerCard.Visible, Is.True);
            int seeds = boot.Wallet!.Seeds;
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("hint_answer"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel"));
            Assert.That(view.HintAnswerCard.Visible, Is.False, "como su X");
            Assert.That(view.Pause.Visible, Is.False, "sin abrir la pausa a la vez");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        [UnityTest]
        public IEnumerator ShuffleCardAndSheet_BackClosesThem_WithoutSpendingTheBooster()
        {
            GameScreen.ShuffleOverride = true;
            Profile profile = FunnelSetup.Profile(1000);
            profile.Stock.Shuffle = 0;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(30, new MemoryProgressStore(30, 29));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);

            Assert.That(view.ShuffleCard.Visible, Is.True, "la tarjeta de la Pororoca en el primer 30");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("shuffle_card"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel"));
            Assert.That(view.ShuffleCard.Visible, Is.False);
            Assert.That(view.Pause.Visible, Is.False);

            object board = boot.Flow!.Session.Current;
            view.Tray.ShuffleButton.onClick.Invoke();
            Assert.That(view.ShuffleConfirm.Visible, Is.True, "la hoja de una línea con semillas");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("shuffle_confirm"));
            Assert.That(boot.LastBackAction, Is.EqualTo("cancel"));
            Assert.That(view.ShuffleConfirm.Visible, Is.False);
            Assert.That(view.ShuffleWave.Playing, Is.False, "sin la ola");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000), "sin cobrar");
            Assert.That(profile.Stock.Shuffle, Is.EqualTo(0));
            Assert.That(boot.Flow.Session.Current, Is.SameAs(board), "el tablero no cambió");
        }

        [UnityTest]
        public IEnumerator NoCanoesInTheLevel_BackIsItsX_BackToLevelFailed()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            Assert.That(boot.BackBlocked(), Is.False, "Sin canoas ya no vibra (D-063)");

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("no_canoes"));
            Assert.That(boot.LastBackAction, Is.EqualTo("close"));
            PlayModeHelpers.Step(boot, 20);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "como su X: vuelve a Nivel fallido");
            Assert.That(view.NoCanoes.Visible, Is.False);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0));
        }
    }
}
