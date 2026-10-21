#nullable enable
using System.Collections;
using AF.Boosters;
using AF.GameFlow;
using AF.IAP;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
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

            // 07 §7.4 (dueño del HUD) y 02 §5: en PLAYING, la cabecera lleva las píldoras de Semillas y, desde el 31, Canoas
            // «n/5» (el jugador ve su saldo cuando la bandeja le pone precios), entre la insignia y Reiniciar, sin pisarlos.
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.WalletBar.Visible, Is.True, "saldo en la cabecera de partida");
            Assert.That(view.WalletBar.Compact, Is.True, "píldoras de 07 §7.4, no el saldo grande del embudo");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(1000));
            Assert.That(view.WalletBar.CanoesShown, Is.EqualTo(5), "Canoas desde el 31");
            Assert.That(view.WalletBar.TimerShown, Is.False, "02 §5: Canoas «4/5», sin cronómetro en partida");
            Assert.That(view.WalletBar.Root.parent, Is.SameAs(view.Hud), "en la capa del HUD: el origen que vierte la tapa (D-006)");
            AssertApart(view.WalletBar.SeedsRect, (RectTransform)view.RestartButton.transform, "Semillas y Reiniciar");
            AssertApart(view.WalletBar.CanoesRect, (RectTransform)view.RestartButton.transform, "Canoas y Reiniciar");
            AssertApart(view.WalletBar.SeedsRect, (RectTransform)view.PauseButton.transform, "Semillas y pausa");
            AssertApart(view.WalletBar.SeedsRect, view.WalletBar.CanoesRect, "Semillas y Canoas");

            FunnelSetup.GetStuck(boot);
            Assert.That(view.StuckVisible, Is.True);
            Assert.That(view.WalletBar.Compact, Is.False, "D-008 paso 1: en el embudo, el saldo grande arriba");
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

            // 07 §1.2 y §7.8 (contraste ≥ 4,5:1): el CTA deshabilitado lleva el texto deshabilitado_texto (#3A3530, 4,85:1 sobre
            // el gris) sin el contorno verde del CTA, que rellenaba los huecos de las letras.
            UnityEngine.Material disabled = view.Stuck.RescueLabel.fontSharedMaterial;
            Assert.That(disabled.IsKeywordEnabled("OUTLINE_ON"), Is.False, "sin contorno en el estado deshabilitado");
            Assert.That(disabled.GetFloat("_OutlineWidth"), Is.EqualTo(0f));
            Assert.That(disabled.IsKeywordEnabled("UNDERLAY_ON"), Is.False, "ni sombra");
            Assert.That(view.Stuck.RescueLabel.color, Is.EqualTo(Palette.DisabledText));

            view.Stuck.HandfulButton.onClick.Invoke();
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(1));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000), "Puñado: 1.000 semillas (05 §4.1)");
            PlayModeHelpers.Step(boot, 20);
            Assert.That(view.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Available), "el modal se rearma con el saldo nuevo");
            Assert.That(view.Stuck.RescuePriceShown, Is.EqualTo(300), "mismo precio");
            Assert.That(view.Stuck.RescueLabel.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"), Is.True, "habilitado, vuelve el contorno del CTA");
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
        public IEnumerator Level31_WithLivesOff_TheBalsaIsSeedsAndACuia_AtTheSamePrice()
        {
            // D-038, añadido del 2026-10-21: con biz_feature_lives apagado, la Balsa queda en «semillas y una cuia» (texto y
            // contenido), con el mismo precio; con el interruptor en su default, nada cambia.
            // (El default, con «canoas infinitas», lo comprueba Fx5Tests.Stuck40_NoSeeds_HandfulAndBalsaWithItsLine.)
            GameBootstrap boot = null!;
            yield return Load31(0, b => boot = b, livesOn: false);
            GameScreen view = boot.View!;
            Assert.That(boot.Services!.BalsaGivesCanoes, Is.False);
            string priced = UiText.StuckBalsaLine(boot.Services.PriceText(StoreCatalog.Rescue));
            FunnelSetup.GetStuck(boot);
            Assert.That(view.Stuck.BalsaButton.gameObject.activeInHierarchy, Is.True, "la Balsa se sigue ofreciendo");
            Assert.That(view.Stuck.BalsaNote.text, Is.EqualTo(UiText.StuckBalsaContentNoCanoes));
            Assert.That(view.Stuck.BalsaNote.text, Is.Not.EqualTo(UiText.StuckBalsaContent));
            Assert.That(view.Stuck.BalsaLabel.text, Is.EqualTo(priced), "mismo precio");
            int cuias = view.Views.Length;
            view.Stuck.BalsaButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "rescate aplicado");
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000));
            Assert.That(boot.Wallet.Canoes.IsInfinite, Is.False, "sin Canoas infinitas");
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
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.WalletBar.Visible, Is.True, "07 §7.4: la píldora de Semillas también en la intro");
            Assert.That(view.WalletBar.Compact, Is.True);
            Assert.That(view.WalletBar.CanoesShown, Is.EqualTo(-1), "07 §7.4: Canoas, visible desde el 31; antes, oculto");
            boot.TapAnywhere();
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

        // Los dos rectángulos (en el mundo) no se pisan.
        private static void AssertApart(RectTransform a, RectTransform b, string what)
        {
            Assert.That(WorldRect(a).Overlaps(WorldRect(b)), Is.False, what + " se pisan");
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static IEnumerator Load31(int seeds, System.Action<GameBootstrap> done, bool livesOn = true)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            if (!livesOn)
            {
                boot.Wallet!.LivesEnabled = false; // lo que deja ConnectWallet con biz_feature_lives = false (08 §9; D-038)
            }

            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            done(boot);
        }
    }
}
