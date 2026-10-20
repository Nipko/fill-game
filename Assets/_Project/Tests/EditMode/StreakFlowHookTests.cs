#nullable enable
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La Crecida, la Hoja escudo y la Canasta con las llamadas del parche <c>S2-flow.diff</c> (B1a, A6; E29, E30 y E31 de
    /// <c>docs/dev/crecida-canasta.md</c>; 06 §4.3-§4.4; 05 §2.1 y §2.4; 04 §2.8 y §5.2): la victoria con el hito 3, la Canasta
    /// de un <c>10k</c> pagada una sola vez (también tras un fallo y al rejugarlo por la vía de desarrollo), el fallo duro con y
    /// sin Hoja escudo, el Reto del río que no cuenta y la Cosecha doble del río crecido sin oferta de vídeo (Q-C9 (a)).
    /// </summary>
    public sealed class StreakFlowHookTests
    {
        [Test]
        public void ThreeWinsInARow_FromThe25_PayTheMilestone3()
        {
            var rig = new StreakFlowRig(offset: 22);
            for (int level = 25; level <= 26; level++)
            {
                rig.Start(level);
                LevelStateTests.Win(rig.Flow);
                StreakServiceResult r = rig.Flow.LastStreakResult!;
                Assert.That(r.Streak.Counted, Is.True);
                Assert.That(r.Streak.Wins, Is.EqualTo(level - 24));
                Assert.That(r.Has(StreakServiceEvents.MilestonePaid), Is.False);
            }

            rig.Start(27);
            int seeds = rig.Economy.Wallet.Seeds;
            LevelStateTests.Win(rig.Flow);
            StreakServiceResult third = rig.Flow.LastStreakResult!;
            Assert.That(third.Streak.Wins, Is.EqualTo(3));
            Assert.That(third.Streak.Milestone, Is.EqualTo(3));
            Assert.That(third.Has(StreakServiceEvents.MilestonePaid), Is.True);
            Assert.That(third.StreakSeeds, Is.EqualTo(300), "streak_milestones[\"3\"] = 300, source = streak");
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(seeds + rig.Flow.HarvestBase + 300));
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(3));
            Assert.That(rig.Store.Current.HitsToday, Does.Contain(3));
            Assert.That(rig.Flow.HarvestDoubled, Is.False, "sin río crecido no hay x2 gratis");
        }

        [Test]
        public void WinningA10k_PaysItsBasket_WithTheHarvest()
        {
            // 06 §4.4, K3: el 20 da 50 semillas + 3 Mareas (eco_basket_cycle[0] = undo3), source = basket; antes del 25 la
            // Crecida no cuenta.
            var rig = new StreakFlowRig(offset: 10);
            rig.Start(20);
            int undo = rig.Economy.Wallet.Stock.Get(BoosterKind.Undo);
            LevelStateTests.Win(rig.Flow);
            StreakServiceResult r = rig.Flow.LastStreakResult!;
            Assert.That(r.Streak.Counted, Is.False, "antes del 25 (C1)");
            Assert.That(r.Has(StreakServiceEvents.BasketOpened), Is.True);
            Assert.That(r.LevelBasket.Level, Is.EqualTo(20));
            Assert.That(r.BasketSeeds, Is.EqualTo(50));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(rig.Flow.HarvestBase + 50));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(undo + 3));
            Assert.That(rig.Flow.HarvestSeeds, Is.EqualTo(rig.Flow.HarvestBase), "la Canasta no es Cosecha");
        }

        [Test]
        public void Retrying10kAfterAHardFail_AndWinning_PaysTheBasketOnce()
        {
            // El 10 es el tablero 6 del tutorial (desplazado 4): 3 → 4 lo bloquea; Volver a la orilla desde el modal es el fallo
            // duro de 1-30 (sin canoa). La Canasta del 10 (50 semillas, K2) se paga al ganarlo, una sola vez.
            var rig = new StreakFlowRig(offset: 4);
            rig.Start(10);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.Restart(), Is.True);
            Assert.That(rig.Listener.Saw("fail:Hard:1"), Is.True);
            StreakServiceResult fail = rig.Flow.LastStreakResult!;
            Assert.That(fail.Streak.Counted, Is.False, "antes del 25 la Crecida no cuenta");
            Assert.That(fail.Transactions.Count, Is.EqualTo(0));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(0));

            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            LevelStateTests.Win(rig.Flow);
            StreakServiceResult win = rig.Flow.LastStreakResult!;
            Assert.That(win.BasketSeeds, Is.EqualTo(50));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(rig.Flow.HarvestBase + 50));
            Assert.That(rig.Progress.MaxLevel, Is.EqualTo(10));
        }

        [Test]
        public void Replaying10kAlreadyPassed_ThroughTheDevPath_DoesNotPayTwice()
        {
            // En el juego no se rejuega un nivel de campaña ya superado (AppFlow.PlayLevel es next_level); solo por desarrollo
            // (LaunchOptions.StartLevelOverride) o en los tests. Aun así, la Canasta del 20 no se paga dos veces (K8).
            var rig = new StreakFlowRig(offset: 10);
            rig.Start(20);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LastStreakResult!.BasketSeeds, Is.EqualTo(50));
            int seeds = rig.Economy.Wallet.Seeds;

            rig.Start(20);
            LevelStateTests.Win(rig.Flow);
            StreakServiceResult again = rig.Flow.LastStreakResult!;
            Assert.That(again.LevelBasket.IsNone, Is.True);
            Assert.That(again.Has(StreakServiceEvents.BasketOpened), Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(seeds + rig.Flow.HarvestBase), "solo la Cosecha");

            var dev = new StreakFlowRig(offset: 10, maxLevel: 40);
            dev.Start(20);
            LevelStateTests.Win(dev.Flow);
            Assert.That(dev.Flow.LastStreakResult!.LevelBasket.IsNone, Is.True, "max_level 40: el 20 ya se superó");
        }

        [Test]
        public void HardFail_WithTheShield_SpendsOnlyTheShield()
        {
            // C9: con la barra ≥ 1 y la hoja del día, el fallo duro gasta la hoja; la barra y el río no cambian.
            var rig = new StreakFlowRig(offset: 20);
            rig.Start(25);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(1));

            rig.Start(26); // el tablero 6 del tutorial
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.Restart(), Is.True);
            StreakServiceResult r = rig.Flow.LastStreakResult!;
            Assert.That(r.Streak.Counted, Is.True);
            Assert.That(r.Streak.ShieldUsed, Is.True, "shield_used de af_level_fail");
            Assert.That(r.Streak.StreakBroken, Is.False);
            Assert.That(r.Has(StreakServiceEvents.ShieldUsed), Is.True);
            Assert.That(r.Has(StreakServiceEvents.StreakFail), Is.True);
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(1));
            Assert.That(rig.Store.Current.RiverFull, Is.False, "el río no cambia (no estaba crecido)");
            Assert.That(rig.Store.Current.ShieldAvailable, Is.False);

            // Con el río crecido, la hoja también lo protege.
            var river = new StreakFlowRig(offset: 20, initial: new StreakRecord { Wins = 1, RiverFull = true });
            river.Start(26);
            LevelStateTests.GetStuck(river.Flow);
            Assert.That(river.Flow.Restart(), Is.True);
            Assert.That(river.Flow.LastStreakResult!.Streak.ShieldUsed, Is.True);
            Assert.That(river.Store.Current.Wins, Is.EqualTo(1));
            Assert.That(river.Store.Current.RiverFull, Is.True, "C9: el río sigue crecido");
        }

        [Test]
        public void HardFail_WithoutTheShield_BreaksTheStreak_AndEndsTheRiver()
        {
            // C10: sin hoja, el fallo duro pone la barra a 0 y apaga el río crecido.
            var start = new StreakFlowRig(offset: 20);
            var initial = new StreakRecord { Wins = 2, RiverFull = true, DateLocal = start.Today, ShieldAvailable = false };
            var rig = new StreakFlowRig(offset: 20, initial: initial);
            rig.Start(26);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.Restart(), Is.True);
            StreakServiceResult r = rig.Flow.LastStreakResult!;
            Assert.That(r.Streak.Counted, Is.True);
            Assert.That(r.Streak.ShieldUsed, Is.False);
            Assert.That(r.Streak.StreakBroken, Is.True, "streak_broken de af_level_fail (Q-C7 (a))");
            Assert.That(r.Streak.RiverEnded, Is.True);
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(0));
            Assert.That(rig.Store.Current.RiverFull, Is.False);

        }

        [Test]
        public void TheDaily_DoesNotCount_NeitherWinNorHardFail()
        {
            // 06 §4.3, C1, Q-C2 (a): con el nivel efectivo en el 30 (un 10k desde el 25), ganar el Reto no suma, ni paga Canasta
            // ni x2 del río, y Volver a la orilla desde el modal no gasta la hoja ni rompe la barra.
            var initial = new StreakRecord { Wins = 2, RiverFull = true };
            var win = new StreakDailyRig(maxLevel: 29, initial: initial);
            int seeds = win.CampaignWallet.Seeds;
            win.Flow.StartDaily();
            Assert.That(win.Flow.TapAnywhere(), Is.True);
            Assert.That(win.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            LevelStateTests.Win(win.Flow);
            StreakServiceResult r = win.Flow.LastStreakResult!;
            Assert.That(r.Streak.Counted, Is.False);
            Assert.That(r.Transactions.Count, Is.EqualTo(0));
            Assert.That(r.LevelBasket.IsNone, Is.True);
            Assert.That(win.Flow.HarvestDoubled, Is.False, "sin x2 del río en el Reto");
            Assert.That(win.CampaignWallet.Seeds, Is.EqualTo(seeds));
            Assert.That(win.Store.Current.Wins, Is.EqualTo(2));
            Assert.That(win.Store.WriteCount, Is.EqualTo(0));

            var fail = new StreakDailyRig(maxLevel: 29, board: TestPacks.Tutorial.Find(6), initial: initial);
            fail.Flow.StartDaily();
            Assert.That(fail.Flow.TapAnywhere(), Is.True);
            Assert.That(fail.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            LevelStateTests.GetStuck(fail.Flow);
            Assert.That(fail.Flow.PressRestart(), Is.True);
            StreakServiceResult f = fail.Flow.LastStreakResult!;
            Assert.That(f.Streak.Counted, Is.False);
            Assert.That(f.Streak.ShieldUsed, Is.False);
            Assert.That(f.Streak.StreakBroken, Is.False);
            Assert.That(fail.Store.Current.Wins, Is.EqualTo(2));
            Assert.That(fail.Store.Current.RiverFull, Is.True);
            Assert.That(fail.Store.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void RiverFull_DoublesTheHarvest_WithoutTheVideoOffer()
        {
            // 04 §2.8, Q-C1 (a), Q-C9 (a): con río crecido, la victoria de campaña lleva la Cosecha doble sin anuncio; la
            // pantalla no ofrece el x2 por vídeo (aunque el 25 está por encima de ads_rv_double_harvest_from_level = 11).
            var initial = new StreakRecord { RiverFull = true };
            var rig = new StreakFlowRig(offset: 22, initial: initial);
            rig.Start(25);
            rig.WinAndShowVictory();
            StreakServiceResult r = rig.Flow.LastStreakResult!;
            Assert.That(r.FreeDoubleHarvest, Is.True);
            Assert.That(r.Has(StreakServiceEvents.FreeDoubleHarvest), Is.True);
            Assert.That(r.DoubleHarvestSeeds, Is.EqualTo(rig.Flow.HarvestBase));
            Assert.That(rig.Flow.HarvestDoubled, Is.True);
            Assert.That(rig.Flow.HarvestSeeds, Is.EqualTo(2 * rig.Flow.HarvestBase));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(2 * rig.Flow.HarvestBase));
            Assert.That(rig.Flow.DoubleHarvestOffered, Is.False, "sin botón x2 por vídeo");
            Assert.That(rig.Flow.ApplyDoubleHarvest(), Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(2 * rig.Flow.HarvestBase));

            var dry = new StreakFlowRig(offset: 22);
            dry.Start(25);
            dry.WinAndShowVictory();
            Assert.That(dry.Flow.HarvestDoubled, Is.False);
            Assert.That(dry.Flow.DoubleHarvestOffered, Is.True, "sin río crecido, el x2 por vídeo sigue (04 §2.8)");
        }

        [Test]
        public void SoftFail_LeavesTheStreakAlone()
        {
            // C11 y 04 §5.2: Volver a la orilla desde la cabecera con ≥ restart_confirm_min_pours vertidos es fallo blando: no
            // llama a la Crecida (ni gasta la hoja, ni rompe la barra, ni apaga el río) ni escribe su almacén.
            var rig = new StreakFlowRig(offset: 0, maxLevel: 30, initial: new StreakRecord { Wins = 2, RiverFull = true }, pack: SoftPack());
            rig.Start(31);
            Assert.That(rig.Flow.PeekStreak()!.Value.ShieldAvailable, Is.True);
            int writes = rig.Store.WriteCount;
            PourBackAndForth(rig.Flow, rig.Flow.Boosters.RestartConfirmMinPours);
            Assert.That(rig.Flow.RestartNeedsConfirm, Is.True);
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Listener.Saw("fail:Soft:1"), Is.True);
            Assert.That(rig.Flow.LastStreakResult, Is.Null, "el fallo blando no llama a la Crecida");
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(2));
            Assert.That(rig.Store.Current.ShieldAvailable, Is.True);
            Assert.That(rig.Store.Current.RiverFull, Is.True);
            Assert.That(rig.Store.WriteCount, Is.EqualTo(writes));
        }

        [Test]
        public void HardFailFrom31_GiveUpThenQuit_FirstSpendsTheShield_ThenBreaksTheStreak()
        {
            // D-008 desde el 31 (camino FailToLevelFailed): «Rendirse» → ¿Seguro? → confirmar, y luego Pausa › «Salir». El 36 es
            // el tablero 6 del tutorial (desplazado 30), con canoas.
            var rig = new StreakFlowRig(offset: 30, maxLevel: 35, initial: new StreakRecord { Wins = 2 });
            rig.Start(36);
            Assert.That(rig.Flow.CanoeAtStake, Is.True);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.PressGiveUp(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(rig.Flow.LastStreakResult, Is.Null, "¿Seguro? aún no es fallo");
            Assert.That(rig.Flow.ConfirmGiveUp(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            StreakServiceResult first = rig.Flow.LastStreakResult!;
            Assert.That(first.Streak.ShieldUsed, Is.True);
            Assert.That(first.Streak.StreakBroken, Is.False);
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(2));
            Assert.That(rig.Store.Current.ShieldAvailable, Is.False);

            Assert.That(rig.Flow.PressRetry(), Is.True);
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(rig.Flow.ConfirmQuit(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            StreakServiceResult second = rig.Flow.LastStreakResult!;
            Assert.That(second.Streak.ShieldUsed, Is.False);
            Assert.That(second.Streak.StreakBroken, Is.True, "sin hoja: la barra vuelve a 0");
            Assert.That(rig.Store.Current.Wins, Is.EqualTo(0));
        }

        [Test]
        public void HardFailFrom31_GiveUpWithoutACanoeAtStake_StillCounts()
        {
            // Con Canoas infinitas «Rendirse» va directo a Nivel fallido (D-008, paso 2 [P]): sigue siendo fallo duro.
            var rig = new StreakFlowRig(offset: 30, maxLevel: 35, initial: new StreakRecord { Wins = 1 });
            Assert.That(rig.Economy.Wallet.Apply(Transaction.InfiniteCanoesFor(System.TimeSpan.FromHours(1))).IsApplied, Is.True);
            rig.Start(36);
            Assert.That(rig.Flow.CanoeAtStake, Is.False);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.PressGiveUp(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "sin ¿Seguro?");
            Assert.That(rig.Flow.LastStreakResult!.Streak.ShieldUsed, Is.True);
            Assert.That(rig.Store.Current.ShieldAvailable, Is.False);
        }

        [Test]
        public void LoadingALevel_ClearsTheLastResult()
        {
            var rig = new StreakFlowRig(offset: 22);
            rig.Start(25);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LastStreakResult, Is.Not.Null);
            rig.Start(26);
            Assert.That(rig.Flow.LastStreakResult, Is.Null);
        }

        // Nivel 31 de prueba (como el de FailResolveTests, sin relief): 2 colores, 2 cuias mezcladas y 2 vacías.
        private static LevelPack SoftPack()
        {
            return LevelPackReader.Read(TestPacks.MinimalPack(
                "{ \"id\": \"m02_v1_n0031\", \"level\": 31, \"world\": 2, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": 4, \"caps\": [4, 4, 4, 4], "
                + "\"cuias\": [[1, 1, 2, 2], [2, 2, 1, 1], [], []], \"attrs\": [null, null, null, null], \"murky\": false, \"hidden\": [], "
                + "\"difficulty\": { \"par\": 3 } }"));
        }

        // 0 → 2 y luego 2 ↔ 3 hasta n vertidos (como FailResolveTests).
        private static void PourBackAndForth(LevelFlow flow, int n)
        {
            Assert.That(flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
            int from = 2;
            int to = 3;
            while (flow.Session.AppliedPours < n)
            {
                Assert.That(flow.TapCuia(from).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(flow.TapCuia(to).Kind, Is.EqualTo(TapKind.Poured));
                int t = from;
                from = to;
                to = t;
            }
        }
    }
}
