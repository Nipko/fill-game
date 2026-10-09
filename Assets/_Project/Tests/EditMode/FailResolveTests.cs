#nullable enable
using System;
using AF.Boosters;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Pasos 3 y 4 del embudo de D-008 y la salida con aviso (tarea B2-3; GDD 04 §5.2, §6.2 <c>failHard</c> y <c>retry</c>,
    /// §7): matriz de consumo de canoas y de <c>fails_level</c>, Nivel fallido, «Reintentar» con la misma semilla o con la
    /// variante aliviada al tercer fallo, Sin canoas y su vuelta a Nivel fallido.
    /// </summary>
    public sealed class FailResolveTests
    {
        [Test]
        public void Matrix_GiveUpFrom31_CostsOneCanoe_AndCountsTheFail()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            fx.Flow.PressGiveUp();
            fx.Flow.ConfirmGiveUp();
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
        }

        [Test]
        public void Matrix_In1To30_TheModalFooterIsAHardFail_WithoutCanoe_AndRestartsDirectly()
        {
            var fx = new FunnelFixture();
            fx.StartPlaying(6);
            fx.GetStuck();
            Assert.That(fx.Flow.PressGiveUp(), Is.False, "en 1-30 no hay «Rendirse»");
            Assert.That(fx.Flow.CanRestart, Is.True);
            Assert.That(fx.Flow.PressRestart(), Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5), "sin canoas antes del 31");
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1), "rama C: es el fallo duro (04 §5.2)");
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "reinicio directo, sin Nivel fallido");
            Assert.That(fx.Listener.Saw("phase:LevelFailed"), Is.False);
            Assert.That(fx.Flow.Session.Current.Equals(fx.Flow.Session.Initial), Is.True, "misma semilla");
        }

        [Test]
        public void Matrix_WithInfiniteCanoes_GivingUpGoesStraightToLevelFailed_WithoutCanoe()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromHours(1)));
            fx.StartPlaying(36);
            fx.GetStuck();
            Assert.That(fx.Flow.PressGiveUp(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "H: sin ¿Seguro? [P]");
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1), "la rendición sigue siendo fallo duro");
        }

        [Test]
        public void Matrix_QuitFrom31_CostsOneCanoe_EvenWithZeroPours()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.StartPlaying(36);
            Assert.That(fx.Flow.QuitCostsCanoe, Is.True);
            Assert.That(fx.Flow.CanQuit, Is.True);
            Assert.That(fx.Flow.ConfirmQuit(), Is.True, "P16 [P]: también con 0 vertidos");
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
        }

        [Test]
        public void Matrix_QuitFrom31_WithInfiniteCanoes_IsAHardFail_ButAnnouncesAndCostsNoCanoe()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromHours(1)));
            fx.StartPlaying(36);
            Assert.That(fx.Flow.QuitIsHardFail, Is.True, "salir con el menú sigue siendo fallo duro (D-008)");
            Assert.That(fx.Flow.CanoeAtStake, Is.False);
            Assert.That(fx.Flow.QuitCostsCanoe, Is.False, "el aviso no anuncia ninguna canoa");
            Assert.That(fx.Flow.ConfirmQuit(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1));
        }

        [Test]
        public void Matrix_WithZeroCanoesMidLevel_QuitAndGiveUpAnnounceNoCanoe_AndSkipAreYouSure()
        {
            // Las Canoas infinitas caducaron a mitad de nivel con 0 canoas: el fallo duro no cobra nada (Wallet.HardFail), así
            // que ni la salida ni «Rendirse» anuncian una pérdida y ¿Seguro? no hace falta (D-008, paso 2 [P]).
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.StartPlaying(36);
            fx.SpendCanoesTo(0);
            Assert.That(fx.Flow.CanoeAtStake, Is.False);
            Assert.That(fx.Flow.QuitCostsCanoe, Is.False);

            fx.GetStuck();
            StuckContext ctx = fx.Flow.BuildStuckContext(false, false, GeoTier.T3);
            Assert.That(StuckModalModel.Build(ctx, fx.Flow.Boosters).CanoeLine, Is.EqualTo(StuckCanoeLine.None));
            Assert.That(StuckModalModel.GiveUpNeedsConfirm(ctx), Is.False);
            Assert.That(fx.Flow.PressGiveUp(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "sin ¿Seguro?");
            Assert.That(fx.Listener.Saw("phase:GiveUpConfirm"), Is.False);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(0));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1), "la rendición sigue siendo fallo duro");
        }

        [Test]
        public void Matrix_QuitIn1To30_IsFree_AndNotAFail()
        {
            var fx = new FunnelFixture();
            fx.StartPlaying(6);
            Assert.That(fx.Flow.QuitCostsCanoe, Is.False);
            Assert.That(fx.Flow.QuitIsHardFail, Is.False);
            Assert.That(fx.Flow.ConfirmQuit(), Is.False, "la salida sin coste la hace AppFlow.ExitToLobby");
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(0));
        }

        [Test]
        public void Matrix_VoluntaryRestartWithFewPours_IsFree_AndNotAFail()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.StartPlaying(31);
            fx.Flow.TapCuia(0);
            fx.Flow.TapCuia(2);
            Assert.That(fx.Flow.RestartNeedsConfirm, Is.False);
            Assert.That(fx.Flow.PressRestart(), Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(0));
        }

        [Test]
        public void Matrix_SoftFail_WithEightPours_NoCanoe_ButCountsForRelief()
        {
            var fx = new FunnelFixture(pack: ReliefPack(), maxLevel: 30);
            fx.StartPlaying(31);
            PourBackAndForth(fx, 8);
            Assert.That(fx.Flow.RestartNeedsConfirm, Is.True, "04 §2.7: con ≥ 8 vertidos pide confirmación");
            Assert.That(fx.Flow.PressRestart(), Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5), "el fallo blando nunca gasta canoa");
            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            Assert.That(fx.Listener.Saw("fail:Soft:1"), Is.True);
        }

        [Test]
        public void LevelFailed_Retry_LoadsTheSameBoard_WithTheShortIntro()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.StartPlaying(36);
            fx.GetStuck();
            fx.Flow.PressGiveUp();
            fx.Flow.ConfirmGiveUp();
            fx.Listener.Events.Clear();
            Assert.That(fx.Flow.PressRetry(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(fx.Listener.Saw("intro:" + GameConfig.AnimTintDurationMsLate + ":short"), Is.True);
            Assert.That(fx.Flow.Session.Current.Equals(fx.Flow.Session.Initial), Is.True);
            Assert.That(fx.Flow.Session.Initial.Equals(fx.Flow.Level.CreateBoard()), Is.True, "misma semilla (02 §2)");
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4), "reintentar no cobra otra canoa");
        }

        [Test]
        public void LevelFailed_RetryWithZeroCanoes_OpensNoCanoes_AndAnyCanoeReturnsToLevelFailed()
        {
            var fx = new FunnelFixture(offset: 30, seeds: 500, maxLevel: 35);
            fx.SpendCanoesTo(1);
            fx.StartPlaying(36);
            fx.GetStuck();
            StuckModalView view = StuckModalModel.Build(fx.Flow.BuildStuckContext(false, false, GeoTier.T3), fx.Flow.Boosters);
            Assert.That(view.CanoeLine, Is.EqualTo(StuckCanoeLine.LostLast), "rama G");
            fx.Flow.PressGiveUp();
            fx.Flow.ConfirmGiveUp();
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(0));

            Assert.That(fx.Flow.PressRetry(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes), "paso 4 (04 §7.3)");
            fx.Flow.Tick(16, true);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));

            Assert.That(fx.Wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill)).IsApplied, Is.True, "Llenar canoas · 200");
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(300));
            fx.Flow.Tick(16, true);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "en cuanto hay una canoa, vuelve a Nivel fallido [P]");
            Assert.That(fx.Flow.PressRetry(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void NoCanoes_TheWaitAlsoBringsACanoeBack()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.SpendCanoesTo(1);
            fx.StartPlaying(36);
            fx.GetStuck();
            fx.Flow.PressGiveUp();
            fx.Flow.ConfirmGiveUp();
            fx.Flow.PressRetry();
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            fx.AdvanceClock(TimeSpan.FromMinutes(25));
            fx.Flow.Tick(16, true);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(1), "1 canoa cada 25 min (04 §7.1)");
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
        }

        [Test]
        public void HudRestartWithZeroCanoesFrom31_OpensNoCanoes_AndClosingReturnsToTheBoard()
        {
            var fx = new FunnelFixture(offset: 30, maxLevel: 35);
            fx.StartPlaying(31);
            fx.SpendCanoesTo(0);
            Assert.That(fx.Flow.PressRestart(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes), "04 §7.1: Volver a la orilla (cabecera) abre Sin canoas");
            Assert.That(fx.Flow.CloseNoCanoes(), Is.True);
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(fx.Flow.AcceptsBoardInput, Is.True, "una partida cargada nunca se interrumpe por canoas");
        }

        [Test]
        public void Relief_AtTheThirdFail_ServesRelief0_WithTheRiverLoweredBanner()
        {
            var fx = new FunnelFixture(pack: ReliefPack(), maxLevel: 30);
            fx.StartPlaying(31);
            int baseCount = fx.Flow.Session.Current.Count;
            for (int fail = 1; fail <= 3; fail++)
            {
                PourBackAndForth(fx, 8);
                Assert.That(fx.Flow.PressRestart(), Is.True, "fallo blando " + fail);
                if (fail < 3)
                {
                    Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(baseCount), "misma semilla hasta el tercer fallo");
                    fx.TickIntro();
                }
            }

            Assert.That(fx.Flow.Attempt.FailsLevel, Is.EqualTo(3));
            Assert.That(fx.Flow.Attempt.ReliefIndex, Is.EqualTo(0));
            Assert.That(fx.Listener.Saw("river:plus_empty:2000"), Is.True, "«El río bajó» de 2.000 ms (anim_river_lowered_banner_ms)");
            Assert.That(fx.Flow.IntroDurationMs, Is.EqualTo(2000 + GameConfig.AnimTintDurationMsLate), "el cartel va antes del teñido");
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(baseCount + 1), "plus_empty: una cuia más");

            // La variante se mantiene hasta ganar, también al salir y volver a entrar.
            fx.TickIntro();
            fx.StartPlaying(31);
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(baseCount + 1));
        }

        [Test]
        public void Relief_WithAnExtraCuiaApplied_ServesMinusColor()
        {
            // 04 §2.2 caso b: con Cuia extra aplicada se sirve la de −1 color, nunca la de +1 vacío, y la cuia se conserva.
            var fx = new FunnelFixture(pack: ReliefPack(), maxLevel: 30);
            fx.Economy.Profile.Stock.ExtraCuia = 1;
            fx.StartPlaying(31);
            Assert.That(fx.Flow.UseExtraCuia()!.IsApplied, Is.True);
            for (int fail = 1; fail <= 3; fail++)
            {
                PourBackAndForth(fx, 8);
                fx.Flow.PressRestart();
                if (fail < 3)
                {
                    fx.TickIntro();
                }
            }

            Assert.That(fx.Flow.Attempt.ReliefIndex, Is.EqualTo(1));
            Assert.That(fx.Listener.Saw("river:minus_color:2000"), Is.True);
            Assert.That(fx.Flow.Session.Current.Count, Is.EqualTo(fx.Flow.Level.Relief[1].CuiaCount + 1), "la Cuia extra se conserva");
        }

        [Test]
        public void Win_ResetsTheFailCounter()
        {
            var attempt = new LevelAttempt();
            attempt.Enter(31);
            FailResolve.Resolve(attempt, FailKind.Soft, 31, null);
            FailResolve.Resolve(attempt, FailKind.Hard, 31, null);
            Assert.That(attempt.FailsLevel, Is.EqualTo(2));
            attempt.Enter(31);
            Assert.That(attempt.FailsLevel, Is.EqualTo(2), "volver al mismo nivel no lo reinicia");
            attempt.Won();
            Assert.That(attempt.FailsLevel, Is.EqualTo(0));
            attempt.Enter(32);
            Assert.That(attempt.FailsLevel, Is.EqualTo(0));
        }

        [Test]
        public void Reader_ReadsReliefBoards_AndRejectsMinusColorWithoutPar()
        {
            LevelDefinition level = ReliefPack().Find(31)!;
            Assert.That(level.Relief.Count, Is.EqualTo(2));
            Assert.That(level.Relief[0].Type, Is.EqualTo(ReliefBoard.TypePlusEmpty));
            Assert.That(level.Relief[0].Par, Is.Null);
            Assert.That(level.Relief[1].Type, Is.EqualTo(ReliefBoard.TypeMinusColor));
            Assert.That(level.Relief[1].Par, Is.EqualTo(1));
            Assert.That(level.Relief[1].CreateBoard().Count, Is.EqualTo(4));

            string bad = ReliefLevel().Replace("\"removed\": 2, \"par\": 1", "\"removed\": 2, \"par\": null");
            Assert.That(() => LevelPackReader.Read(TestPacks.MinimalPack(bad)), Throws.TypeOf<LevelFormatException>());
            Assert.That(TestPacks.Tutorial.Find(1)!.Relief, Is.Empty, "antes del 31 relief = [] (03 §13)");
        }

        // Mueve la racha de la cuia 0 a la 2 y luego la lleva y la trae entre las vacías 2 y 3 (vertidos legales, fútiles los
        // de ida y vuelta: R13 los acepta) hasta sumar n vertidos en el historial.
        private static void PourBackAndForth(FunnelFixture fx, int n)
        {
            Assert.That(fx.Flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(fx.Flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
            int from = 2;
            int to = 3;
            while (fx.Flow.Session.AppliedPours < n)
            {
                Assert.That(fx.Flow.TapCuia(from).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(fx.Flow.TapCuia(to).Kind, Is.EqualTo(TapKind.Poured));
                int t = from;
                from = to;
                to = t;
            }
        }

        // Nivel 31 de prueba con relief[] (08 §4.1): 2 colores, 2 cuias mezcladas y 2 vacías; plus_empty (+1 vacía) y
        // minus_color (sin el color 2).
        private static LevelPack ReliefPack()
        {
            return LevelPackReader.Read(TestPacks.MinimalPack(ReliefLevel()));
        }

        private static string ReliefLevel()
        {
            return "{ \"id\": \"m02_v1_n0031\", \"level\": 31, \"world\": 2, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": 4, \"caps\": [4, 4, 4, 4], "
                + "\"cuias\": [[1, 1, 2, 2], [2, 2, 1, 1], [], []], \"attrs\": [null, null, null, null], \"murky\": false, \"hidden\": [], "
                + "\"difficulty\": { \"par\": 3 }, \"relief\": [ "
                + "{ \"type\": \"plus_empty\", \"caps\": [4, 4, 4, 4, 4], \"cuias\": [[1, 1, 2, 2], [2, 2, 1, 1], [], [], []], \"par\": null, \"bot_fail\": 0.1 }, "
                + "{ \"type\": \"minus_color\", \"caps\": [4, 4, 4, 4], \"cuias\": [[1, 1], [1, 1], [], []], \"removed\": 2, \"par\": 1, \"bot_fail\": 0.0 } ] }";
        }
    }
}
