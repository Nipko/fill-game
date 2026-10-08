#nullable enable
using System.Collections;
using AF.Boosters;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pasos 1 y 2 del embudo de D-008 en la escena (tarea B2-2; 04 §6.2, §6.6): en el nivel 31 (tablero 6 del tutorial con
    /// <c>af_level_offset</c> = 25) el bloqueo abre Aguas estancadas con el saldo arriba y sin HUD ni bandeja; el rescate
    /// añade una cuia y la partida sigue; después ya no se ofrece otro (rama A'); sin saldo, el Puñado rearma el modal;
    /// «Rendirse» abre ¿Seguro?. En 1-30, el modal de siempre con «Volver a la orilla (gratis)».
    /// </summary>
    public sealed class StuckFunnelTests
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
        public IEnumerator Level31_TheRescueAddsACuia_TheGameGoesOn_AndThereIsNoSecondOne()
        {
            GameBootstrap boot = null!;
            yield return Load31(1000, b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.StuckVisible, Is.True);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available));
            Assert.That(view.Stuck.RescuePriceShown, Is.EqualTo(300), "primer rescate del día (rescue_first_day_mult)");
            Assert.That(view.Stuck.RescueNoteText, Is.EqualTo(UiText.StuckRescueDiscount));
            Assert.That(view.Stuck.CanoeLineText, Is.EqualTo(UiText.StuckCanoeLost));
            Assert.That(view.Stuck.GiveUpButton.gameObject.activeSelf, Is.True, "D-008: «Rendirse» al pie desde el 31");
            Assert.That(view.Stuck.RestartButton.gameObject.activeSelf, Is.False);
            Assert.That(view.UndoButton.gameObject.activeInHierarchy, Is.False, "la bandeja se oculta (ficha §4.4)");
            Assert.That(view.WalletBar.Visible, Is.True, "saldo arriba");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(1000));
            Assert.That(view.WalletBar.CanoesShown, Is.EqualTo(5));

            int cuias = view.Views.Length;
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(700));
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(view.StuckVisible, Is.False);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied));
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.UndoButton.gameObject.activeInHierarchy, Is.True, "vuelven el HUD y la bandeja");
            Assert.That(boot.Flow.AcceptsBoardInput, Is.True);

            StuckModalView again = StuckModalModel.Build(boot.Flow.BuildStuckContext(false, true, GeoTier.T3), boot.Flow.Boosters);
            Assert.That(again.Rescue, Is.EqualTo(StuckRescueKind.None), "rama A': 1 rescate por nivel");
        }

        [UnityTest]
        public IEnumerator Level31_WithoutSeeds_TheHandfulRefreshesTheModal_WithTheSamePrice()
        {
            GameBootstrap boot = null!;
            yield return Load31(0, b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.Stuck.RescueNoteText, Is.EqualTo(UiText.MissingSeeds(300)));
            Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf, Is.True);
            Assert.That(view.Stuck.BalsaButton.gameObject.activeSelf, Is.True, "Balsa: 1 al día");

            view.Stuck.HandfulButton.onClick.Invoke();
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(1));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000), "Puñado: 1.000 semillas (05 §4.1)");
            PlayModeHelpers.Step(boot, 20);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available), "el modal se rearma con el saldo nuevo");
            Assert.That(view.Stuck.RescuePriceShown, Is.EqualTo(300), "mismo precio");
            Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Level31_TheBalsaAppliesTheRescueWithoutSeeds()
        {
            GameBootstrap boot = null!;
            yield return Load31(0, b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            int cuias = view.Views.Length;
            view.Stuck.BalsaButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "04 §6.2 BALSA_499: rescate aplicado");
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000), "+1.000 semillas sin cobrar el rescate");
            Assert.That(boot.Wallet.Canoes.IsInfinite, Is.True, "+ Canoas infinitas 30 min");
        }

        [UnityTest]
        public IEnumerator Level31_GiveUp_OpensAreYouSure_WithTheSameRescue()
        {
            GameBootstrap boot = null!;
            yield return Load31(1000, b => boot = b);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            view.Stuck.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(view.GiveUp.Visible, Is.True);
            Assert.That(view.GiveUp.LineText, Is.EqualTo(UiText.GiveUpConfirmCanoe));
            Assert.That(view.GiveUp.RescueButton.gameObject.activeSelf, Is.True, "otra vez «Continuar: +1 cuia»");
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5));

            view.GiveUp.CloseButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "la X confirma la rendición");
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator Level6_TheModalKeepsTheFreeRestart_AndHasNoRescue()
        {
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.RestartButton.gameObject.activeSelf, Is.True, "rama C");
            Assert.That(view.Stuck.GiveUpButton.gameObject.activeSelf, Is.False);
            Assert.That(view.Stuck.RescueButton.gameObject.activeSelf, Is.False);
            Assert.That(view.Stuck.CanoeLineText, Is.Null);
            Assert.That(view.WalletBar.CanoesShown, Is.EqualTo(-1), "sin canoas antes del 31");

            view.Stuck.RestartButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(1));
        }

        private static IEnumerator Load31(int seeds, System.Action<GameBootstrap> done)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            done(boot);
        }
    }
}
