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
