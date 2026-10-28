#nullable enable
using System.Collections;
using AF.Boosters;
using AF.GameFlow;
using AF.IAP;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Observación 6 de la jornada 5 en el teléfono (2026-10-28): sin saldo, «Continuar: +1 cuia» en gris no hacía nada al tocarlo.
    /// Ahora, en Aguas estancadas y en ¿Seguro?, el toque no cobra nada y responde como un comodín sin saldo: late «Te faltan {n}
    /// semillas» (con Reducir movimiento, solo se atenúa); y, si la tienda no vende, sale el aviso «Las compras no están disponibles
    /// ahora», el de «Canoas infinitas» en Sin canoas (guion 8.2: «no un toque mudo»).
    /// </summary>
    public sealed class RescueMissingTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.A11yHostOverride = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Stuck_TheGrayRescue_PulsesTheMissingLine_AndChargesNothing()
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled), "condición: sin saldo");
            Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf, Is.True, "con la tienda, Puñado y Balsa debajo");
            Assert.That(view.Stuck.RescueButton.IsInteractable(), Is.True, "gris, pero el toque llega");
            Assert.That(view.Stuck.RescueButton.GetComponent<ReliefButton>().Interactable, Is.False, "con el aspecto deshabilitado");
            Assert.That(view.Stuck.RescueLabel.color, Is.EqualTo(AF.Presentation.Palette.DisabledText));
            int cuias = view.Views.Length;

            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(view.Stuck.RescuePulse.Running, Is.True, "late «Te faltan {n} semillas»");
            Assert.That(view.Stuck.RescueNoteText, Is.EqualTo(UiText.MissingSeeds(300)));
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal), "no se cierra el modal");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(view.Views.Length, Is.EqualTo(cuias), "ninguna cuia añadida");
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0), "ninguna compra");
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(0), "con la tienda, sin aviso: ahí están Puñado y Balsa");

            float max = 1f;
            for (int ms = 0; ms < NotePulse.DurationMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                max = UnityEngine.Mathf.Max(max, view.Stuck.RescuePulse.ScaleNow);
            }

            Assert.That(max, Is.GreaterThan(1.04f).And.LessThanOrEqualTo(NotePulse.MaxScale + 1e-4f), "el latido de la burbuja de un comodín");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Stuck.RescuePulse.Running, Is.False);
            Assert.That(view.Stuck.RescuePulse.ScaleNow, Is.EqualTo(1f), "vuelve a reposo");
            Assert.That(view.Stuck.RescuePulse.AlphaNow, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Stuck_TheGrayRescue_WithTheStoreOff_ShowsTheStoreNotice()
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            boot.Services!.StoreLedger.Config.BizIapEnabled = false; // biz_iap_enabled apagado: la tienda no vende
            Assert.That(boot.Services.Store.IsAvailable, Is.False);
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf, Is.False, "sin tienda, ni Puñado…");
            Assert.That(view.Stuck.BalsaButton.gameObject.activeSelf, Is.False, "…ni Balsa");

            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(1), "el aviso, no un toque mudo (guion 8.2)");
            Assert.That(boot.SkinToast, Is.Not.Null);
            Assert.That(boot.SkinToast!.Visible, Is.True);
            Assert.That(boot.SkinToast.Text, Is.EqualTo(MarketText.StoreUnavailable), "el mismo texto que «Canoas infinitas» en Sin canoas");
            Assert.That(view.Stuck.RescuePulse.Running, Is.True, "y late «Te faltan {n} semillas»");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Stuck_WithSeeds_TheRescueStillRescues()
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(1000), b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available));
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "con saldo, el rescate de siempre");
            Assert.That(view.Stuck.RescuePulse.Starts, Is.EqualTo(0));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator GiveUpConfirm_TheGrayRescue_Responds_WithTheNoticeOnlyWithoutTheStore([Values(true, false)] bool storeOn)
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.GiveUp.Visible, Is.True);
            Assert.That(view.GiveUp.RescueButton.gameObject.activeSelf, Is.True);
            Assert.That(view.GiveUp.RescueButton.GetComponent<ReliefButton>().Interactable, Is.False, "en gris");

            view.GiveUp.RescueButton.onClick.Invoke();
            Assert.That(view.GiveUp.RescuePulse.Running, Is.True, "late «Te faltan {n} semillas»");
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm), "sigue en ¿Seguro?");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1), "el aviso solo si la tienda no vende");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_TheLineFadesInsteadOfBeating()
        {
            Profile profile = FunnelSetup.Profile(0);
            profile.Settings.ReduceMotion = ReduceMotionSetting.On;
            GameBootstrap boot = null!;
            yield return Load31(profile, b => boot = b);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.ReduceMotion, Is.True);
            FunnelSetup.GetStuck(boot);
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(view.Stuck.RescuePulse.Running, Is.True);
            float minAlpha = 1f;
            for (int ms = 0; ms < NotePulse.DurationMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.Stuck.RescuePulse.ScaleNow, Is.EqualTo(1f), "con Reducir movimiento, sin latido (02 §12)");
                minAlpha = UnityEngine.Mathf.Min(minAlpha, view.Stuck.RescuePulse.AlphaNow);
            }

            Assert.That(minAlpha, Is.LessThan(0.6f).And.GreaterThanOrEqualTo(NotePulse.ReducedAlpha - 1e-3f), "se atenúa y vuelve");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Stuck.RescuePulse.AlphaNow, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Stuck_TheGrayRefillSeeds_PulsesItsOwnLabel_AndChargesNothing([Values(true, false)] bool storeOn)
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            FunnelSetup.GetStuck(boot);

            // Sin cupo ni stock de Marea, sin video y sin semillas: «+3 Deshacer · 30 semillas» en gris (04 §6.2, regla 0).
            StuckContext ctx = boot.Flow!.BuildStuckContext(false, true, GeoTier.T3);
            ctx.UndoUnlimited = false;
            ctx.UndoFree = 0;
            ctx.UndoStock = 0;
            ctx.UndoAdReady = false;
            ctx.PurchasesBlocked = !storeOn;
            view.ShowStuckVariant(StuckModalModel.Build(ctx, boot.Flow.Boosters));
            Assert.That(view.Stuck.View!.Undo, Is.EqualTo(StuckUndoKind.Refill));
            Assert.That(view.Stuck.View.UndoRefillMissing, Is.GreaterThan(0), "condición: sin saldo");
            Assert.That(view.Stuck.RefillSeedsButton.GetComponent<ReliefButton>().Interactable, Is.False, "en gris");
            Assert.That(view.Stuck.RefillSeedsButton.IsInteractable(), Is.True, "pero el toque llega");

            view.Stuck.RefillSeedsButton.onClick.Invoke();
            Assert.That(view.Stuck.RefillSeedsPulse.Running, Is.True, "sin línea «Te faltan», late su etiqueta con el precio");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1), "el aviso solo si la tienda no vende");
            float max = 1f;
            for (int ms = 0; ms < NotePulse.DurationMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                max = UnityEngine.Mathf.Max(max, view.Stuck.RefillSeedsPulse.ScaleNow);
            }

            Assert.That(max, Is.GreaterThan(1.04f));
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Stuck.RefillSeedsPulse.ScaleNow, Is.EqualTo(1f), "vuelve a reposo");
        }

        [UnityTest]
        public IEnumerator Stuck_TheGraySkip_PulsesTheMissingLine_AndOpensNothing([Values(true, false)] bool storeOn)
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            FunnelSetup.GetStuck(boot);

            // Rama D sin saldo (04 §6.2 y §1: el precio en gris y «Te faltan {n} semillas»).
            StuckContext ctx = boot.Flow!.BuildStuckContext(false, true, GeoTier.T3);
            ctx.Skip = new SkipOffer(SkipState.Disabled, 300, 300, 31);
            ctx.PurchasesBlocked = !storeOn;
            view.ShowStuckVariant(StuckModalModel.Build(ctx, boot.Flow.Boosters));
            Assert.That(view.Stuck.SkipButton.gameObject.activeSelf, Is.True, "rama D");
            Assert.That(view.Stuck.SkipNoteText, Is.EqualTo(UiText.MissingSeeds(300)));
            Assert.That(view.Stuck.SkipButton.GetComponent<ReliefButton>().Interactable, Is.False, "en gris");

            view.Stuck.SkipButton.onClick.Invoke();
            Assert.That(view.Stuck.SkipPulse.Running, Is.True, "late «Te faltan {n} semillas»");
            Assert.That(boot.SkipConfirmView == null || !boot.SkipConfirmView.Visible, Is.True, "no abre la confirmación");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(31));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1));
        }

        [UnityTest]
        public IEnumerator NoCanoes_TheGrayRefill_PulsesTheMissingLine([Values(true, false)] bool storeOn)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            PlayModeHelpers.Step(boot, 20);
            NoCanoesPanel panel = boot.View!.NoCanoes;
            Assert.That(panel.RefillButton.gameObject.activeSelf, Is.True);
            Assert.That(panel.RefillButton.GetComponent<ReliefButton>().Interactable, Is.False, "sin saldo, en gris");

            panel.RefillButton.onClick.Invoke();
            Assert.That(panel.RefillPulse.Running, Is.True, "late «Te faltan {n} semillas»");
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0), "no llena nada");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1));
            PlayModeHelpers.Step(boot, (int)NotePulse.DurationMs + 100);
            Assert.That(panel.RefillPulse.Running, Is.False, "la pantalla de nivel la hace avanzar");
        }

        [UnityTest]
        public IEnumerator NoCanoes_FromTheLobby_TheGrayRefill_Responds_Too([Values(true, false)] bool storeOn)
        {
            Profile profile = FunnelSetup.Profile(0, 0);
            profile.Progress.MaxLevel = 30;
            profile.Progress.NextLevel = 31;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            boot.Lobby!.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            NoCanoesPanel panel = boot.Lobby.NoCanoes;
            Assert.That(panel.RefillButton.GetComponent<ReliefButton>().Interactable, Is.False, "sin saldo, en gris");

            panel.RefillButton.onClick.Invoke();
            Assert.That(panel.RefillPulse.Running, Is.True);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0));
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1));
            for (int i = 0; i < 40; i++)
            {
                boot.Lobby.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(panel.RefillPulse.Running, Is.False, "el inicio la hace avanzar");
        }

        [UnityTest]
        public IEnumerator TalkBack_EachGrayButton_AnnouncesTheMissingSeeds()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(host.Announcements, Does.Contain(UiText.MissingSeeds(300)), "el rescate: «Te faltan 300 semillas»");

            // «+3 Deshacer», sin línea propia: la misma clave (stuck_rescue_missing) con lo que le falta.
            StuckContext ctx = boot.Flow!.BuildStuckContext(false, true, GeoTier.T3);
            ctx.UndoUnlimited = false;
            ctx.UndoFree = 0;
            ctx.UndoStock = 0;
            ctx.UndoAdReady = false;
            ctx.Skip = new SkipOffer(SkipState.Disabled, 300, 300, 31);
            view.ShowStuckVariant(StuckModalModel.Build(ctx, boot.Flow.Boosters));
            int refillMissing = view.Stuck.View!.UndoRefillMissing;
            Assert.That(refillMissing, Is.GreaterThan(0));
            view.Stuck.RefillSeedsButton.onClick.Invoke();
            Assert.That(host.Announcements[host.Announcements.Count - 1], Is.EqualTo(UiText.MissingSeeds(refillMissing)), "«+3 Deshacer»");
            view.Stuck.SkipButton.onClick.Invoke();
            Assert.That(host.Announcements[host.Announcements.Count - 1], Is.EqualTo(UiText.MissingSeeds(300)), "el Salto del Boto");
            Assert.That(view.Stuck.RescueButton.IsInteractable(), Is.True, "sigue activable, como la bandeja");
        }

        [UnityTest]
        public IEnumerator DoubleTap_PulsesTwice_AndShowsTwoNotices_WithoutCharging()
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            boot.Services!.StoreLedger.Config.BizIapEnabled = false;
            FunnelSetup.GetStuck(boot);
            view.Stuck.RescueButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs * 3); // a medio latido
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(view.Stuck.RescuePulse.Starts, Is.EqualTo(2), "el latido vuelve a empezar");
            Assert.That(view.Stuck.RescuePulse.Running, Is.True);
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(2), "dos toques, dos avisos");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0));
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, (int)NotePulse.DurationMs + 100);
            Assert.That(view.Stuck.RescuePulse.ScaleNow, Is.EqualTo(1f), "y vuelve a reposo una sola vez");
        }

        [UnityTest]
        public IEnumerator ReadOnlyProfile_TheGrayRescue_ShowsTheStoreNotice()
        {
            var store = new ReadOnlyProfileStore(ProfileLoadStatus.TooNew);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Services!.PurchasesBlocked, Is.True, "perfil en solo lectura (08 §4.4)");
            Assert.That(boot.Services.Store.IsAvailable, Is.True, "la tienda vende, pero no a este perfil");
            boot.LockedNotice!.PlayButton.onClick.Invoke();
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf, Is.False);
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(view.Stuck.RescuePulse.Running, Is.True);
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(1), "sin compras para este perfil: el aviso");
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ClosingTheModal_MidPulse_LeavesTheLineAtRest()
        {
            Profile profile = FunnelSetup.Profile(0);
            GameBootstrap boot = null!;
            yield return Load31(profile, b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);

            // Aguas estancadas: «Rendirse» con el latido a medias.
            view.Stuck.RescueButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs * 4);
            Assert.That(view.Stuck.RescuePulse.ScaleNow, Is.GreaterThan(1f), "a medio latido");
            Assert.That(boot.PressGiveUp(), Is.True);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.GiveUp.Visible, Is.True);
            Assert.That(view.Stuck.RescuePulse.Running, Is.False, "el modal se cerró: el latido se acaba");
            Assert.That(view.Stuck.RescuePulse.ScaleNow, Is.EqualTo(1f));
            Assert.That(view.Stuck.RescuePulse.AlphaNow, Is.EqualTo(1f));

            // ¿Seguro?: la X con el latido a medias.
            view.GiveUp.RescueButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs * 4);
            Assert.That(view.GiveUp.RescuePulse.ScaleNow, Is.GreaterThan(1f));
            view.GiveUp.CloseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.GiveUp.Visible, Is.False);
            Assert.That(view.GiveUp.RescuePulse.Running, Is.False);
            Assert.That(view.GiveUp.RescuePulse.ScaleNow, Is.EqualTo(1f), "Hide deja la línea en reposo");
        }

        [UnityTest]
        public IEnumerator GiveUpConfirm_RecomputesTheRescue_WhenTheSeedsChange()
        {
            GameBootstrap boot = null!;
            yield return Load31(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.PressGiveUp(), Is.True);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.GiveUp.RescueKind, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.GiveUp.RescueMissingSeeds, Is.EqualTo(300));

            // Las semillas llegan con ¿Seguro? abierto (una recompensa o una compra): como Aguas estancadas, se rearma.
            Assert.That(boot.Wallet!.Apply(AF.Economy.Transaction.Earn(AF.Economy.SeedSource.Calendar, 1000)).IsApplied, Is.True);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.GiveUp.RescueKind, Is.EqualTo(StuckRescueKind.Available), "con saldo, el rescate");
            Assert.That(view.GiveUp.RescueButton.GetComponent<ReliefButton>().Interactable, Is.True);
            int cuias = view.Views.Length;
            view.GiveUp.RescueButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "y rescata");
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
        }

        // El nivel 31 con el tablero 6 del tutorial (como StuckFunnelTests).
        private static IEnumerator Load31(Profile profile, System.Action<GameBootstrap> done)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            done(boot);
        }
    }
}
