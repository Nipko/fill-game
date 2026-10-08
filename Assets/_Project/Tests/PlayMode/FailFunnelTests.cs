#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pasos 3 y 4 del embudo de D-008 y la salida con aviso en la escena (tarea B2-3; 04 §5.2, §6.6, §7.3): en el 31 con 1
    /// canoa, Aguas estancadas → ¿Seguro? («tu última canoa») → Nivel fallido (premio que no se cobra) → «Reintentar» → Sin
    /// canoas → «Llenar canoas» → otra vez Nivel fallido → «Reintentar» juega; desde la pausa, «Salir» pregunta y cuesta una
    /// canoa desde el 31, y es gratis en 1-30.
    /// </summary>
    public sealed class FailFunnelTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level31_WithOneCanoe_TheWholeFunnel_AndBackWithRefill()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.CanoeLineText, Is.EqualTo(UiText.StuckCanoeLostLast), "rama G");

            view.Stuck.GiveUpButton.onClick.Invoke();
            Assert.That(view.GiveUp.LineText, Is.EqualTo(UiText.GiveUpConfirmLastCanoe));
            view.GiveUp.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(view.Failed.TitleText, Is.EqualTo("Nivel 31"));
            Assert.That(view.Failed.RewardText, Is.EqualTo("Premio: 20 semillas"), "eco_reward_normal; no se cobra");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(500));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(0));

            view.Failed.RetryButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            Assert.That(view.NoCanoes.Visible, Is.True);
            PlayModeHelpers.Step(boot, 20);
            StringAssert.StartsWith(UiText.NoCanoesNext, view.NoCanoes.TimerText);
            Assert.That(view.NoCanoes.VideoShown, Is.True, "rv_extra_canoe con fill");

            view.NoCanoes.RefillButton.onClick.Invoke();
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5), "Llenar canoas: 5/5");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(300));
            PlayModeHelpers.Step(boot, 20);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "con una canoa vuelve a Nivel fallido [P]");
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(view.NoCanoes.Visible, Is.False);

            view.Failed.RetryButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.Failed.Visible, Is.False);
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5), "reintentar no cobra canoa");
        }

        [UnityTest]
        public IEnumerator Level31_TheExtraCanoeVideo_GivesOneCanoe()
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
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            boot.View!.NoCanoes.VideoButton.onClick.Invoke();
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "anuncio simulado de rv_extra_canoe");
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(1));
            PlayModeHelpers.Step(boot, 20);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
        }

        [UnityTest]
        public IEnumerator Level31_QuitFromThePause_AsksFirst_AndCostsACanoe()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            Assert.That(view.PauseButton.gameObject.activeInHierarchy, Is.True);
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True);
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True);
            Assert.That(view.Confirm.Kind, Is.EqualTo(ConfirmKind.Quit));
            Assert.That(view.Confirm.BodyText, Is.EqualTo(UiText.QuitConfirmCanoe));
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5), "preguntar no cuesta");

            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(4));
            view.Failed.CloseButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "X → inicio");
        }

        [UnityTest]
        public IEnumerator Level6_QuitFromThePause_IsFreeAndGoesHome()
        {
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.False, "en 1-30 se sale sin coste ni pregunta");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.Flow!.Attempt.FailsLevel, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RestartWithEightPours_AsksForConfirmation_WithAnUndoShortcut()
        {
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore(2, 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            // Tablero 2: la racha 4,4 de la cuia 0 va a la vacía 3 y se lleva y se trae entre las vacías 3 y 4 (vertidos
            // fútiles pero legales, R13) hasta sumar 8 vertidos.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            int from = 3;
            int to = 4;
            while (boot.Flow!.Session.AppliedPours < 8)
            {
                Assert.That(boot.TapCuia(from).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(to).Kind, Is.EqualTo(TapKind.Poured));
                int t = from;
                from = to;
                to = t;
            }

            PlayModeHelpers.StepUntilIdle(boot, 12000);
            Assert.That(boot.Flow.RestartNeedsConfirm, Is.True);
            view.RestartButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True);
            Assert.That(view.Confirm.Kind, Is.EqualTo(ConfirmKind.Restart));
            Assert.That(view.Confirm.UndoButton.gameObject.activeSelf, Is.True, "atajo a Deshacer (04 §2.7)");
            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(1), "fallo blando");
        }
    }
}
