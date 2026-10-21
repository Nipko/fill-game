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

            // 02 §2 (INTRO): «En reintento muestra Nivel N · intento k», con k = fails_level + 1 (aquí, tras un fallo duro).
            Assert.That(view.BannerText, Is.EqualTo(UiText.LevelAttempt(31, 2)));
            Assert.That(view.BannerText, Is.EqualTo("Nivel 31 · intento 2"));
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "la intro corta (800 ms) terminó");
            Assert.That(view.BannerText, Is.EqualTo("Nivel 31 · intento 2"), "el rótulo sigue un momento sobre el tablero");
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.BannerText, Is.Null, "y se apaga solo");
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
            Assert.That(view.Confirm.CanoeShown, Is.True);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5), "preguntar no cuesta");
            AssertQuitComposition(view);

            // La X sigue jugando y devuelve la cabecera de partida, con sus píldoras de Semillas y Canoas (07 §7.4).
            view.Confirm.CloseButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.False);
            Assert.That(view.PauseButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(view.UndoButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(view.WalletBar.Visible, Is.True);
            Assert.That(view.WalletBar.Compact, Is.True, "las píldoras de la cabecera, no el saldo grande del embudo");
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5));

            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(4));
            view.Failed.CloseButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "X → inicio");
        }

        [UnityTest]
        public IEnumerator Level31_WithInfiniteCanoes_QuitAsks_WithoutAnnouncingACanoe()
        {
            // Revisión de B2-3: con Canoas infinitas no hay canoa en juego (HardFail no cobra): el aviso no dice «Perderás 1
            // canoa», pero salir sigue siendo fallo duro y lleva a Nivel fallido (D-008).
            Profile profile = FunnelSetup.Profile(0);
            profile.Wallet.Canoes.InfiniteUntil = FunnelSetup.T0 + System.TimeSpan.FromHours(1);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.CanoeAtStake, Is.False);
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True, "salir desde el 31 sigue preguntando: es fallo duro");
            Assert.That(view.Confirm.BodyText, Is.Null, "sin «Perderás 1 canoa»");
            Assert.That(view.Confirm.CanoeShown, Is.False);
            AssertQuitComposition(view);

            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5), "no cuesta canoa");
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level31_WithInfiniteCanoes_GiveUpSkipsAreYouSure()
        {
            Profile profile = FunnelSetup.Profile(0);
            profile.Wallet.Canoes.InfiniteUntil = FunnelSetup.T0 + System.TimeSpan.FromHours(1);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.CanoeLineText, Is.Null, "rama H: sin línea de canoa");
            view.Stuck.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed), "sin ¿Seguro? (D-008, paso 2 [P])");
            Assert.That(view.GiveUp.Visible, Is.False);
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5));
        }

        // «¿Salir del nivel?» con la composición de «Are You Sure?» de Magic Sort (ficha §4.3): X y un solo botón «Salir», saldo
        // arriba y el HUD de partida (insignia, Reiniciar, pausa y bandeja) oculto, como en el embudo.
        private static void AssertQuitComposition(GameScreen view)
        {
            Assert.That(view.Confirm.KeepPlayingButton.gameObject.activeSelf, Is.False, "sin «Seguir jugando»: lo hace la X");
            Assert.That(view.Confirm.CloseButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(view.Confirm.ConfirmButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(view.WalletBar.Visible, Is.True, "saldo arriba");
            Assert.That(view.PauseButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(view.RestartButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(view.UndoButton.gameObject.activeInHierarchy, Is.False, "bandeja oculta");
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
        public IEnumerator Level31_WithLivesOff_EverythingWorksAsIn1To30()
        {
            // D-038: con biz_feature_lives apagado, desde el 31 el embudo es el de 1-30: sin canoas en la barra ni en el modal,
            // el pie de Aguas estancadas es «Volver a la orilla (gratis)» (reinicio directo, sin ¿Seguro? ni Nivel fallido) y
            // «Salir» desde la pausa sale gratis al inicio, sin aviso y sin contar como fallo. El rescate no cambia.
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Wallet!.LivesEnabled = false; // lo que deja ConnectWallet con biz_feature_lives = false (08 §9)
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.CanoeLineText, Is.Null, "sin línea de canoa");
            Assert.That(view.Stuck.RestartButton.gameObject.activeInHierarchy, Is.True, "el pie de 1-30 (rama C)");
            Assert.That(view.Stuck.GiveUpButton.gameObject.activeSelf, Is.False, "sin «Rendirse»");
            Assert.That(view.Stuck.RescueButton.gameObject.activeInHierarchy, Is.True, "el rescate no cambia");
            Assert.That(view.WalletBar.CanoesShown, Is.EqualTo(-1), "la barra no enseña canoas");

            view.Stuck.RestartButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro), "reinicio directo");
            Assert.That(view.GiveUp.Visible, Is.False);
            Assert.That(view.Failed.Visible, Is.False);
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(1), "rama C: fallo duro sin canoa, como en 1-30");

            PlayModeHelpers.Step(boot, 1000);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.False, "sin «¿Salir del nivel?»: sale sin coste ni pregunta");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(1), "salir no suma a fails_level");
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5));
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
            Assert.That(view.BannerText, Is.EqualTo("Nivel 2 · intento 2"), "02 §2: el reintento lleva su rótulo");
        }

        [UnityTest]
        public IEnumerator VoluntaryRestart_WithoutAFail_ShowsTheSameAttempt()
        {
            // Volver a la orilla con < 8 vertidos no es fallo (04 §2.7): el rótulo dice el mismo intento (fails_level + 1).
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            Assert.That(view.BannerText, Is.Null, "la primera intro no lleva rótulo de intento");
            view.RestartButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(0));
            Assert.That(view.BannerText, Is.EqualTo("Nivel 7 · intento 1"));
        }
    }
}
