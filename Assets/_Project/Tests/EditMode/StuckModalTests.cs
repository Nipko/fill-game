#nullable enable
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Pasos 1 y 2 del embudo de D-008 (tarea B2-2): árbol de Aguas estancadas de GDD 04 §6.2 con la tabla de ramas de §6.3
    /// (C, A, A', B, B', E, F, G, H) y ¿Seguro? [P]; y el flujo real en el nivel 36 (tablero 6 del tutorial con
    /// <c>af_level_offset</c> = 30): verificación «la compra cumple», precio congelado, compra del rescate y su tope.
    /// </summary>
    public sealed class StuckModalTests
    {
        private static readonly BoosterConfig Config = BoosterConfig.Defaults;

        [Test]
        public void BranchC_Levels1To30_MareaAndFreeRestart_WithoutRescueOrCanoes()
        {
            StuckContext ctx = Ctx(level: 6, seeds: 5000);
            ctx.UndoUnlimited = true;
            StuckModalView view = StuckModalModel.Build(ctx, Config);
            Assert.That(view.Undo, Is.EqualTo(StuckUndoKind.Undo));
            Assert.That(view.UndoUnlimited, Is.True);
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.None), "sin rescate antes del 31 (dirección 10)");
            Assert.That(view.Offers, Is.Empty, "05 §10: ninguna oferta de pago antes del 31");
            Assert.That(view.CanoeLine, Is.EqualTo(StuckCanoeLine.None));
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.RestartFree));
            Assert.That(StuckModalModel.GiveUpNeedsConfirm(ctx), Is.False);
        }

        [Test]
        public void BranchA_WithSeeds_FirstOfTheDayCosts300_With600Struck_AndGiveUpFooter()
        {
            StuckModalView view = StuckModalModel.Build(Ctx(level: 31, seeds: 1240, price: 300), Config);
            Assert.That(view.Undo, Is.EqualTo(StuckUndoKind.Undo), "regla 0: Marea primero");
            Assert.That(view.UndoBadge, Is.EqualTo(3));
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.Available));
            Assert.That(view.RescuePrice, Is.EqualTo(300));
            Assert.That(view.RescueBasePrice, Is.EqualTo(600));
            Assert.That(view.RescueDiscounted, Is.True);
            Assert.That(view.CanoeLine, Is.EqualTo(StuckCanoeLine.Lost), "«Travesía perdida: −1 canoa»");
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.GiveUp), "D-008: «Rendirse» desde el 31");
            Assert.That(view.Offers, Is.Empty);

            StuckModalView second = StuckModalModel.Build(Ctx(level: 31, seeds: 1240, price: 600), Config);
            Assert.That(second.RescuePrice, Is.EqualTo(600));
            Assert.That(second.RescueDiscounted, Is.False);
        }

        [Test]
        public void BranchAPrime_SecondBlockOfTheLevel_HasNoRescue()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 600);
            ctx.RescuesLevel = 1;
            StuckModalView view = StuckModalModel.Build(ctx, Config);
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.None), "rescue_max_per_level = 1");
            Assert.That(view.Offers, Is.Empty);
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.GiveUp));
            Assert.That(StuckModalModel.BuildGiveUpConfirm(ctx).Rescue, Is.EqualTo(StuckRescueKind.None), "¿Seguro? tampoco lo ofrece");
        }

        [Test]
        public void BranchB_WithoutSeeds_GreyRescue_AndHandfulThenBalsaInT3()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 140, price: 600);
            StuckModalView view = StuckModalModel.Build(ctx, Config);
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(view.RescueMissing, Is.EqualTo(460), "«Te faltan 460 semillas» (wireframe B)");
            Assert.That(view.Offers, Is.EqualTo(new[] { StuckOffer.Handful, StuckOffer.Balsa }), "T3: Puñado y Balsa");

            ctx.BalsaOk = false;
            Assert.That(StuckModalModel.Build(ctx, Config).Offers, Is.EqualTo(new[] { StuckOffer.Handful }), "Balsa: 1 al día, oculta tras 2 rechazos");
        }

        [Test]
        public void BranchBPrime_T1_BalsaFirst()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 140, price: 600);
            ctx.Geo = GeoTier.T1;
            Assert.That(StuckModalModel.Build(ctx, Config).Offers, Is.EqualTo(new[] { StuckOffer.Balsa, StuckOffer.Handful }));
        }

        [Test]
        public void BranchE_NoQuotaAndNoFill_OnlyTheSeedsRefill_AndVideoFirstWhenThereIsFill()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 600);
            ctx.UndoFree = 0;
            ctx.UndoStock = 0;
            StuckModalView noFill = StuckModalModel.Build(ctx, Config);
            Assert.That(noFill.Undo, Is.EqualTo(StuckUndoKind.Refill));
            Assert.That(noFill.UndoVideo, Is.False, "sin fill se oculta el video (rama E)");
            Assert.That(noFill.UndoRefillPrice, Is.EqualTo(30));

            ctx.UndoAdReady = true;
            StuckModalView t3 = StuckModalModel.Build(ctx, Config);
            Assert.That(t3.UndoVideo, Is.True);
            Assert.That(t3.UndoVideoFirst, Is.True, "T3: «undo_ad» primero (stuck_order_t3)");

            ctx.Geo = GeoTier.T1;
            Assert.That(StuckModalModel.Build(ctx, Config).UndoVideoFirst, Is.False, "T1 con saldo para 30: el video baja (04 §6.6)");
            ctx.Seeds = 10;
            Assert.That(StuckModalModel.Build(ctx, Config).UndoVideoFirst, Is.True);
        }

        [Test]
        public void BranchF_Unsolvable_NoRescueForSale_AndOneFreeMarea()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 600);
            ctx.RescueCheck = RescueCheck.Unsolvable;
            ctx.UndoFree = 0;
            StuckModalView view = StuckModalModel.Build(ctx, Config);
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.None), "principio 4: no se vende lo que no cumple");
            Assert.That(view.Unsolvable, Is.True);
            Assert.That(view.Undo, Is.EqualTo(StuckUndoKind.Gift), "04 §6.3 rama F: 1 recarga gratis de +1 [P]");

            ctx.UndoGiftUsed = true;
            Assert.That(StuckModalModel.Build(ctx, Config).Undo, Is.EqualTo(StuckUndoKind.Refill));

            ctx.RescueCheck = RescueCheck.Timeout;
            Assert.That(StuckModalModel.Build(ctx, Config).Rescue, Is.EqualTo(StuckRescueKind.Available), "timeout → se ofrece (04 §6.4)");
        }

        [Test]
        public void BranchG_LastCanoe_AndStep2SaysSo()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 600);
            ctx.Canoes = 1;
            Assert.That(StuckModalModel.Build(ctx, Config).CanoeLine, Is.EqualTo(StuckCanoeLine.LostLast));
            GiveUpConfirmView step2 = StuckModalModel.BuildGiveUpConfirm(ctx);
            Assert.That(step2.LastCanoe, Is.True, "«Perderás tu última canoa»");

            ctx.Canoes = 4;
            Assert.That(StuckModalModel.BuildGiveUpConfirm(ctx).LastCanoe, Is.False, "«Perderás 1 canoa»");
        }

        [Test]
        public void BranchH_InfiniteCanoes_NoCanoeLine_AndGiveUpSkipsStep2()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 600);
            ctx.InfiniteCanoes = true;
            Assert.That(StuckModalModel.Build(ctx, Config).CanoeLine, Is.EqualTo(StuckCanoeLine.None));
            Assert.That(StuckModalModel.GiveUpNeedsConfirm(ctx), Is.False, "D-008: directo a Nivel fallido [P]");
            GiveUpConfirmView step2 = StuckModalModel.BuildGiveUpConfirm(ctx);
            Assert.That(step2.ShowsCanoe, Is.False, "si ¿Seguro? se reabre, no anuncia ninguna canoa");
            Assert.That(step2.CanoeLine, Is.EqualTo(StuckCanoeLine.None));
        }

        [Test]
        public void NoCanoeAtStake_WithZeroCanoes_NoCanoeLine_AndGiveUpSkipsStep2()
        {
            // Revisión de B2-2: con 0 canoas (p. ej. Canoas infinitas caducadas a mitad de nivel) el fallo duro no cobra nada;
            // «Perderás tu última canoa» (Canoes <= 1) anunciaba una pérdida que no existe.
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 600);
            ctx.Canoes = 0;
            Assert.That(StuckModalModel.Build(ctx, Config).CanoeLine, Is.EqualTo(StuckCanoeLine.None));
            Assert.That(StuckModalModel.GiveUpNeedsConfirm(ctx), Is.False);
            GiveUpConfirmView step2 = StuckModalModel.BuildGiveUpConfirm(ctx);
            Assert.That(step2.ShowsCanoe, Is.False);
            Assert.That(step2.LastCanoe, Is.False);

            ctx.Canoes = 1;
            Assert.That(StuckModalModel.GiveUpNeedsConfirm(ctx), Is.True);
            Assert.That(StuckModalModel.BuildGiveUpConfirm(ctx).CanoeLine, Is.EqualTo(StuckCanoeLine.LostLast));
        }

        [Test]
        public void Step2_OffersTheSameRescue_AtTheSamePrice()
        {
            StuckContext ctx = Ctx(level: 31, seeds: 1240, price: 300);
            GiveUpConfirmView step2 = StuckModalModel.BuildGiveUpConfirm(ctx);
            Assert.That(step2.Rescue, Is.EqualTo(StuckRescueKind.Available));
            Assert.That(step2.RescuePrice, Is.EqualTo(300));
            Assert.That(step2.RescueDiscounted, Is.True);

            ctx.Seeds = 100;
            GiveUpConfirmView poor = StuckModalModel.BuildGiveUpConfirm(ctx);
            Assert.That(poor.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(poor.RescueMissing, Is.EqualTo(200));
        }

        [Test]
        public void Flow_At36_ChecksTheRescue_FreezesThePrice_AndBuyingAddsACuiaAndPlays()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.StartPlaying(36);
            Assert.That(fx.Flow.UndoStatus.FreeLeft, Is.EqualTo(3), "desde el 11, 3 Mareas por nivel");
            fx.GetStuck();
            Assert.That(fx.Flow.RescueCheck, Is.EqualTo(RescueCheck.Solvable), "04 §6.4: el tablero + 1 cuia se resuelve");
            Assert.That(fx.Flow.FrozenRescueQuote.HasValue, Is.True);
            Assert.That(fx.Flow.FrozenRescueQuote!.Value.Price, Is.EqualTo(300), "primer rescate del día");

            StuckModalView view = StuckModalModel.Build(fx.Flow.BuildStuckContext(false, true, GeoTier.T3), fx.Flow.Boosters);
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.Available));
            Assert.That(view.CanoeLine, Is.EqualTo(StuckCanoeLine.Lost));
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.GiveUp));

            int cuias = fx.Flow.Session.Current.Count;
            TransactionResult? paid = fx.Flow.BuyRescue();
            Assert.That(paid!.IsApplied, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(700));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5), "la Cuia de rescate nunca consume canoa (04 §7.2)");
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(fx.Flow.AcceptsBoardInput, Is.True, "la partida sigue");
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
            Assert.That(fx.Flow.Session.AppliedPours, Is.EqualTo(1), "el historial previo se conserva (04 §2.1 caso d)");
            Assert.That(fx.Listener.Saw("cuia:" + cuias + ":Rescue"), Is.True);
            Assert.That(fx.Flow.Attempt.Rescues, Is.EqualTo(1));

            // Rama A': en el mismo nivel ya no hay otro rescate (tampoco tras reiniciar: la cuia persiste).
            StuckContext again = fx.Flow.BuildStuckContext(false, true, GeoTier.T3);
            Assert.That(StuckModalModel.Build(again, fx.Flow.Boosters).Rescue, Is.EqualTo(StuckRescueKind.None));
            Assert.That(fx.Flow.Restart(), Is.True);
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
            Assert.That(fx.Flow.Attempt.Rescues, Is.EqualTo(1));
        }

        [Test]
        public void Flow_TheSecondRescueOfTheDayCosts600()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 2000, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.BuyRescue()!.IsApplied, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1700));

            fx.StartPlaying(37); // tablero 7, otro nivel: el tope de 1 por nivel vuelve a cero
            Assert.That(fx.Wallet.QuoteRescue(37)!.Value.Price, Is.EqualTo(600), "ya hubo uno hoy (rescues_today)");
        }

        [Test]
        public void Flow_WithoutSeeds_TheRescueIsRejected_AndNothingChanges()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 100, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            TransactionResult? rejected = fx.Flow.BuyRescue();
            Assert.That(rejected!.IsApplied, Is.False);
            Assert.That(rejected.Missing, Is.EqualTo(200));
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(100));

            // Balsa de rescate comprada: el rescate se aplica sin semillas (la tienda ya acreditó su contenido).
            Assert.That(fx.Flow.ApplyPaidRescue(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(100));
        }

        [Test]
        public void Flow_UndoFromTheModal_ClosesIt_AndSpendsTheQuota()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 0, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.Undo(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(fx.Flow.UndoStatus.FreeLeft, Is.EqualTo(2));
            Assert.That(fx.Listener.Saw("undo:3->4:modal"), Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5), "Marea desde el modal no es fallo (04 §5.2)");
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(0));
        }

        [Test]
        public void Flow_GiveUp_OpensStep2_AndConfirmingIsAHardFail()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.CanRestart, Is.False, "desde el 31 el pie es «Rendirse»");
            Assert.That(fx.Flow.PressGiveUp(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(fx.Listener.Saw("phase:GiveUpConfirm"), Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5), "¿Seguro? aún no cobra");
            Assert.That(fx.Flow.RescueOffered, Is.True, "otra vez «Continuar: +1 cuia»");

            Assert.That(fx.Flow.ConfirmGiveUp(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            Assert.That(fx.Listener.Saw("fail:Hard:1:canoe"), Is.True);
        }

        [Test]
        public void Flow_RescueBoughtInStep2_ClosesTheFunnel()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            fx.Flow.PressGiveUp();
            Assert.That(fx.Flow.BuyRescue()!.IsApplied, Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(0));
        }

        [Test]
        public void Flow_AndroidBack_HasNoWayOut_ButTheModalStaysOpenUntilAChoice()
        {
            // 04 §6.6: el modal no se cierra sin elegir; el flujo no tiene un «cerrar» y ni los toques al tablero lo cierran.
            var fx = new FunnelFixture(offset: 30, seeds: 1000, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(fx.Flow.TapBackground().Kind, Is.EqualTo(TapKind.Ignored));
            fx.Flow.Tick(10000, true);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
        }

        [Test]
        public void RescueCheck_SolvesTheBlockedBoardPlusOne_AndHasNoRoomWith16()
        {
            // 04 §6.4: A* acotado (hint_budget_nodes = 40.000) sobre el estado + 1 cuia vacía de capacidad 4.
            BoardState stuck = PourRules.Apply(TestPacks.Tutorial.Levels[5].CreateBoard(), 3, 4, PourMode.Run);
            Assert.That(PourRules.IsStuck(stuck, true), Is.True);
            var rescue = new RescueBooster(BoosterConfig.Defaults);
            Assert.That(rescue.Check(stuck, PourMode.Run, true), Is.EqualTo(RescueCheck.Solvable));
            Assert.That(rescue.LastExpanded, Is.LessThanOrEqualTo(BoosterConfig.Defaults.HintBudgetNodes));
            Assert.That(RescueBooster.WithEmptyCuia(stuck).Count, Is.EqualTo(stuck.Count + 1));

            var full = new Cuia[BoardState.MaxCuias];
            for (int i = 0; i < full.Length; i++)
            {
                full[i] = Cuia.Empty(4);
            }

            Assert.That(rescue.Check(new BoardState(full), PourMode.Run, true), Is.EqualTo(RescueCheck.NoRoom), "02 R1: 16 cuias como máximo");
            Assert.That(RescueBooster.IsOffered(RescueCheck.Timeout), Is.True, "el presupuesto no demostró nada: se ofrece");
            Assert.That(RescueBooster.IsOffered(RescueCheck.Unsolvable), Is.False);
            Assert.That(RescueBooster.IsOffered(RescueCheck.NoRoom), Is.False);
        }

        private static StuckContext Ctx(int level, int seeds, int price = 0)
        {
            return new StuckContext
            {
                Level = level,
                RescueMinLevel = 31,
                UndoUnlimited = level <= 10,
                UndoFree = level <= 10 ? 0 : 3,
                UndoStock = 0,
                HasHistory = true,
                UndoAdReady = false,
                Seeds = seeds,
                UndoRefillPrice = 30,
                UndoRefillAmount = 3,
                Canoes = 5,
                InfiniteCanoes = false,
                RescuesLevel = 0,
                RescueMaxPerLevel = 1,
                RescueCheck = level >= 31 ? RescueCheck.Solvable : RescueCheck.NotChecked,
                RescuePrice = level >= 31 ? price : 0,
                RescueBasePrice = level >= 31 ? 600 : 0,
                BalsaOk = true,
                Geo = GeoTier.T3,
            };
        }
    }
}
