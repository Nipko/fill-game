#nullable enable
using System.Collections;
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
    /// segundo plano (puerto falso) una sola vez aunque se pulse dos veces seguidas; y un doble Atrás rápido no lanza excepciones.
    /// Cada pulsación escribe <c>AF_STATE back</c>. Aguas estancadas, ¿Seguro? y Nivel fallido: <see cref="BackButtonTests"/>.
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
        }

        /// <inheritdoc />
        public override void TearDown()
        {
            GameBootstrap.AppTaskPortOverride = null;
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

                // Con Ajustes encima, Atrás la cierra primero y no manda la app al fondo.
                Assert.That(boot.PressSettings(), Is.True);
                Assert.That(boot.PressAndroidBack(), Is.True);
                Assert.That(boot.SettingsView!.Visible, Is.False);
                Assert.That(boot.LastBackScreen, Is.EqualTo("settings"));
                Assert.That(boot.LastBackAction, Is.EqualTo("close"));
                Assert.That(_task.Calls, Is.EqualTo(1));
            }
            finally
            {
                GameBootstrap.ResetDailyOverrides();
            }
        }
    }
}
