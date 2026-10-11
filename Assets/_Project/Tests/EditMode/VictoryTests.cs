#nullable enable
using System;
using System.Collections.Generic;
using AF.Ads;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Victoria con la cadena corta de D-008 (punto 3; 02 §2 y §6; tarea B2-4): la Cosecha (20/60/80) se cobra en el instante
    /// lógico de la victoria; desde el 11 la Cosecha doble la duplica exactamente una vez; «Siguiente» pasa por AD_GATE, que
    /// espera al intersticial; AD_GATE nunca corre al reintentar, al rendirse, en Nivel fallido ni en Sin canoas (05 §10); y
    /// con los anuncios simulados de verdad, en 22-60 y fuera de la sesión de instalación, 1 intersticial cada 3 victorias,
    /// también con Canoas infinitas (D-008).
    /// </summary>
    public sealed class VictoryTests
    {
        [Test]
        public void TheHarvest_IsPaidAtTheLogicalVictory_BeforeTheCelebration()
        {
            var rig = new LevelStateTests.Rig(seeds: 100);
            rig.Start(1);
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "aún no celebra: la vista termina su cola");
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(120), "eco_reward_normal = 20, ya cobrada (02 §14: sin pérdida)");
            Assert.That(rig.Flow.HarvestSeeds, Is.EqualTo(20));
            Assert.That(rig.Flow.HarvestBase, Is.EqualTo(20));
            Assert.That(rig.Progress.NextLevel, Is.EqualTo(2));

            ToVictoryScreen(rig.Flow);
            Assert.That(rig.Flow.DoubleHarvestOffered, Is.False, "Cosecha doble desde el 11 (ads_rv_double_harvest_from_level)");
            Assert.That(rig.Flow.ApplyDoubleHarvest(), Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(120));
        }

        [Test]
        public void IsResolvingWin_OnlyWhileTheLogicalVictoryWaitsForTheView()
        {
            // D-011, punto 1: la Cosecha se cobra en el instante lógico (02 §14), pero la vista no da la victoria por hecha
            // mientras termina el último vertido. IsResolvingWin marca ese tramo: PLAYING con la victoria pendiente, ni antes
            // (INTRO, PLAYING sin nada pendiente) ni después (CELEBRATION, VICTORY_SCREEN), ni con un bloqueo pendiente.
            var rig = new LevelStateTests.Rig(seeds: 100);
            LevelFlow flow = rig.Flow;
            flow.Load(1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.IsResolvingWin, Is.False, "INTRO");
            Assert.That(flow.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.IsResolvingWin, Is.False, "PLAYING sin nada pendiente");
            Assert.That(flow.IsResolving, Is.False);

            LevelStateTests.Win(flow);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.IsResolvingWin, Is.True, "victoria lógica: la vista termina el último vertido");
            Assert.That(flow.IsResolving, Is.True);
            Assert.That(flow.AcceptsBoardInput, Is.False, "R15: el input ya está bloqueado");
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(120), "y la Cosecha ya está cobrada (02 §14)");
            flow.Tick(16, false);
            Assert.That(flow.IsResolvingWin, Is.True, "sigue mientras la cola de la vista no se vacía");

            flow.Tick(0, true);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            Assert.That(flow.IsResolvingWin, Is.False, "CELEBRATION");
            Assert.That(flow.IsResolving, Is.False);
            Assert.That(flow.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(flow.IsResolvingWin, Is.False, "VICTORY_SCREEN");

            // Con un bloqueo pendiente (tablero 6 del tutorial: 3 → 4 deja Aguas estancadas), IsResolving sí y IsResolvingWin no.
            var stuck = new LevelStateTests.Rig();
            stuck.Start(6);
            Assert.That(stuck.Flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(stuck.Flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(stuck.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(stuck.Flow.IsResolving, Is.True, "bloqueo lógico a la espera del modal (R15)");
            Assert.That(stuck.Flow.IsResolvingWin, Is.False, "un bloqueo no es una victoria");
            for (int i = 0; i < 100 && stuck.Flow.Phase != FlowPhase.StuckModal; i++)
            {
                stuck.Flow.Tick(16, true);
            }

            Assert.That(stuck.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(stuck.Flow.IsResolvingWin, Is.False, "STUCK_MODAL");
        }

        [Test]
        public void From11_TheDoubleHarvest_DoublesExactly_Once()
        {
            var rig = new LevelStateTests.Rig(offset: 10, seeds: 5);
            rig.Start(11);
            LevelStateTests.Win(rig.Flow);
            ToVictoryScreen(rig.Flow);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(25));
            Assert.That(rig.Flow.DoubleHarvestOffered, Is.True);

            Assert.That(rig.Flow.ApplyDoubleHarvest(), Is.True);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(45), "+20 → +40 (04 §2.8)");
            Assert.That(rig.Flow.HarvestSeeds, Is.EqualTo(40));
            Assert.That(rig.Flow.HarvestDoubled, Is.True);
            Assert.That(rig.Listener.Saw("harvest_doubled:40"), Is.True);
            Assert.That(rig.Flow.DoubleHarvestOffered, Is.False, "una vez por victoria");
            Assert.That(rig.Flow.ApplyDoubleHarvest(), Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(45));
        }

        [TestCase("hard", 60)]
        [TestCase("very_hard", 80)]
        public void TheHarvest_FollowsTheLabel_AndTheDoubleDoublesIt(string label, int harvest)
        {
            string level = TestPacks.MinimalLevel().Replace("\"par\": 3", "\"label\": \"" + label + "\", \"par\": 3");
            LevelPack pack = LevelPackReader.Read(TestPacks.MinimalPack(level));
            var economy = new EconomyFixture();
            var flow = new LevelFlow(LevelCatalog.Create(new[] { pack }, 10), new MemoryProgressStore(), new RecordingListener(), GameConfig.TapSwitchSelection, PourModes.Default, economy.Wallet);
            flow.Load(11);
            flow.TapAnywhere();
            LevelStateTests.Win(flow);
            ToVictoryScreen(flow);
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(harvest), "eco_reward_" + label);
            Assert.That(flow.ApplyDoubleHarvest(), Is.True);
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(2 * harvest));
        }

        [Test]
        public void Next_GoesThroughTheAdGate_AndWaitsForTheAd()
        {
            var gate = new HeldGate();
            var rig = new LevelStateTests.Rig();
            rig.Flow.AdGate = gate;
            var app = new AppFlow(rig.Flow, rig.Progress, rig.Economy.Wallet, new Screens());
            app.StartAtLevel(1);
            rig.Flow.TapAnywhere();
            LevelStateTests.Win(rig.Flow);
            Assert.That(gate.Wins, Is.EqualTo(new[] { 1 }), "la victoria cuenta para la cadencia en su instante lógico");
            ToVictoryScreen(rig.Flow);

            Assert.That(app.PressNext(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.AdGate));
            Assert.That(rig.Listener.Saw("ad_gate"), Is.True);
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(1), "espera a que se cierre el intersticial");
            Assert.That(app.PressNext(), Is.False, "un segundo toque no hace nada");
            Assert.That(app.PressHome(), Is.False);
            Assert.That(gate.Runs, Is.EqualTo(new[] { 1 }));

            gate.Release();
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(2));
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            gate.Release();
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(2), "onDone solo una vez");
        }

        [Test]
        public void Home_SkipsTheAdGate_ButTheVictoryStillCounts()
        {
            var gate = new HeldGate();
            var rig = new LevelStateTests.Rig();
            rig.Flow.AdGate = gate;
            var app = new AppFlow(rig.Flow, rig.Progress, rig.Economy.Wallet, new Screens());
            app.StartAtLevel(1);
            rig.Flow.TapAnywhere();
            LevelStateTests.Win(rig.Flow);
            ToVictoryScreen(rig.Flow);
            Assert.That(app.PressHome(), Is.True);
            Assert.That(app.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(gate.Runs, Is.Empty, "02 §6: Inicio sin AD_GATE");
            Assert.That(gate.Wins, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void TheAdGate_NeverRuns_InTheFailFunnel()
        {
            // 05 §10 y D-008: ni al reintentar, ni al rendirse, ni en Nivel fallido, ni en Sin canoas, aunque Magic Sort lo haga.
            var gate = new HeldGate();
            var rig = new LevelStateTests.Rig(offset: 25);
            rig.Flow.AdGate = gate;
            var app = new AppFlow(rig.Flow, rig.Progress, rig.Economy.Wallet, new Screens());
            app.StartAtLevel(31);
            rig.Flow.TapAnywhere();
            for (int attempt = 0; attempt < 5; attempt++)
            {
                LevelStateTests.GetStuck(rig.Flow);
                Assert.That(rig.Flow.PressGiveUp(), Is.True);
                Assert.That(rig.Flow.ConfirmGiveUp(), Is.True);
                Assert.That(rig.Flow.PressRetry(), Is.True);
                if (rig.Flow.Phase == FlowPhase.NoCanoes)
                {
                    break;
                }

                rig.Flow.TapAnywhere();
            }

            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            Assert.That(app.ExitToLobby(), Is.True);
            Assert.That(gate.Runs, Is.Empty);
            Assert.That(gate.Wins, Is.Empty);
        }

        [Test]
        public void SimulatedAds_From22_SecondSession_OneInterstitialEveryThreeWins()
        {
            AdRig rig = AdRig.Create(offset: 21);
            rig.App.StartAtLevel(22);
            int[] expected = { 0, 0, 1, 1, 1, 2 };
            for (int i = 0; i < expected.Length; i++)
            {
                rig.Clock.Advance(TimeSpan.FromSeconds(200)); // ≥ 150 s desde el anterior y ≥ 120 s desde el inicio
                rig.Flow.TapAnywhere();
                LevelStateTests.Win(rig.Flow);
                ToVictoryScreen(rig.Flow);
                Assert.That(rig.App.PressNext(), Is.True);
                Assert.That(rig.Presenter.Presented, Is.EqualTo(expected[i]), "victoria " + (22 + i));
                Assert.That(rig.Flow.LevelNumber, Is.EqualTo(23 + i), "AD_GATE nunca bloquea el avance");
            }

            Assert.That(rig.Presenter.Last!.Placement, Is.EqualTo(AdPlacement.InterstitialPostLevel));
            Assert.That(rig.Presenter.Last.Format, Is.EqualTo(AdFormat.Interstitial));
        }

        [Test]
        public void SimulatedAds_InfiniteCanoes_DoNotRemoveInterstitials()
        {
            AdRig rig = AdRig.Create(offset: 21, prepare: p =>
            {
                p.Wallet.Canoes.InfiniteUntil = EconomyFixture.T0 + TimeSpan.FromHours(1);
                p.Ads.LevelsSinceInterstitial = 2;
            });
            Assert.That(rig.Economy.Wallet.Canoes.IsInfinite, Is.True);
            rig.App.StartAtLevel(22);
            rig.Clock.Advance(TimeSpan.FromSeconds(200));
            rig.Flow.TapAnywhere();
            LevelStateTests.Win(rig.Flow);
            ToVictoryScreen(rig.Flow);
            Assert.That(rig.App.PressNext(), Is.True);
            Assert.That(rig.Presenter.Presented, Is.EqualTo(1), "D-008: las Canoas infinitas no quitan los intersticiales");
        }

        [Test]
        public void SimulatedAds_NeverInTheInstallSession_NorBefore22()
        {
            AdRig install = AdRig.Create(offset: 21, installSession: true, prepare: p => p.Ads.LevelsSinceInterstitial = 2);
            install.App.StartAtLevel(22);
            install.Clock.Advance(TimeSpan.FromSeconds(200));
            install.Flow.TapAnywhere();
            LevelStateTests.Win(install.Flow);
            ToVictoryScreen(install.Flow);
            install.App.PressNext();
            Assert.That(install.Presenter.Presented, Is.EqualTo(0), "05 §6.1: nunca en la sesión de instalación");

            AdRig early = AdRig.Create(offset: 11, prepare: p => p.Ads.LevelsSinceInterstitial = 5);
            early.App.StartAtLevel(21);
            early.Clock.Advance(TimeSpan.FromSeconds(200));
            early.Flow.TapAnywhere();
            LevelStateTests.Win(early.Flow);
            ToVictoryScreen(early.Flow);
            early.App.PressNext();
            Assert.That(early.Presenter.Presented, Is.EqualTo(0), "ads_first_interstitial_level = 22");
        }

        // De la victoria lógica a VICTORY_SCREEN: la vista está libre, celebración y toque.
        private static void ToVictoryScreen(LevelFlow flow)
        {
            flow.Tick(16, true);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            flow.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        private sealed class HeldGate : IAdGate
        {
            private Action? _pending;

            public List<int> Wins { get; } = new List<int>();

            public List<int> Runs { get; } = new List<int>();

            public void RecordWin(int level)
            {
                Wins.Add(level);
            }

            public void Run(int wonLevel, Action onDone)
            {
                Runs.Add(wonLevel);
                _pending = onDone;
            }

            public void Release()
            {
                _pending?.Invoke();
            }
        }

        private sealed class Screens : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }

        // Flujo, navegación y anuncios simulados de verdad (AdController + FakeAdService con un presentador inmediato).
        private sealed class AdRig
        {
            private AdRig(EconomyFixture economy, ImmediateAdPresenter presenter, LevelFlow flow, AppFlow app)
            {
                Economy = economy;
                Presenter = presenter;
                Flow = flow;
                App = app;
            }

            public EconomyFixture Economy { get; }

            public AF.Platform.ManualClock Clock => Economy.Clock;

            public ImmediateAdPresenter Presenter { get; }

            public LevelFlow Flow { get; }

            public AppFlow App { get; }

            public static AdRig Create(int offset, bool installSession = false, Action<AF.Save.Profile>? prepare = null)
            {
                var economy = new EconomyFixture();
                prepare?.Invoke(economy.Profile);
                var presenter = new ImmediateAdPresenter();
                AdController ads = AdController.Create(new FakeAdService(presenter), economy.Profile, economy.Store, economy.Clock, installSession);
                var progress = new MemoryProgressStore();
                var flow = new LevelFlow(LevelCatalog.Create(new[] { TestPacks.Tutorial }, offset), progress, new RecordingListener(), GameConfig.TapSwitchSelection, PourModes.Default, economy.Wallet)
                {
                    AdGate = new InterstitialGate(ads, new DevProbe(), economy.Wallet),
                };
                var app = new AppFlow(flow, progress, economy.Wallet, new Screens());
                return new AdRig(economy, presenter, flow, app);
            }
        }
    }
}
