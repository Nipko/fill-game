#nullable enable
using System.Collections.Generic;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Cuándo sale la secuencia del Mapa del 10 (tarea M4.2; 06 §2.3; D-013, punto 7; P-M4 de D-023): la regla
    /// (<see cref="L10SequenceRule"/>) con <c>max_level</c>, <c>map_seen_level</c> y la entrada («Siguiente» en la victoria o la
    /// entrada al inicio), y la navegación de <see cref="AppFlow"/>: «Siguiente» en el 10 pasa por AD_GATE y va al inicio, donde
    /// <see cref="AppFlow.L10MapDue"/> pide la secuencia; Inicio en esa victoria y un arranque posterior, también; con lo visto
    /// hasta el 10, ya no.
    /// </summary>
    public sealed class L10SequenceRuleTests
    {
        [Test]
        public void TheSequenceLevel_IsTheFirstArrival_10()
        {
            Assert.That(L10SequenceRule.Level, Is.EqualTo(10));
            Assert.That(L10SequenceRule.Level, Is.EqualTo(RiverStopCatalog.LevelsPerStop));
        }

        [TestCase(10, 0, true, true)]
        [TestCase(10, 9, true, true)]
        [TestCase(10, 10, true, false)]
        [TestCase(10, 0, false, false)]
        [TestCase(9, 0, true, false)]
        [TestCase(11, 0, true, false)]
        [TestCase(20, 0, true, false)]
        [TestCase(0, 0, true, false)]
        [TestCase(1001, 0, true, false)]
        public void AfterNext_OnlyInTheVictoryOf10_WithTheMap_AndNotSeenYet(int won, int seen, bool available, bool expected)
        {
            Assert.That(L10SequenceRule.AfterNext(won, seen, available), Is.EqualTo(expected));
        }

        [TestCase(10, 0, true, true)]
        [TestCase(10, 9, true, true)]
        [TestCase(58, 0, true, true)]
        [TestCase(9, 0, true, false)]
        [TestCase(0, 0, true, false)]
        [TestCase(10, 10, true, false)]
        [TestCase(58, 57, true, false)]
        [TestCase(10, 0, false, false)]
        public void OnLobby_WithMax10OrMore_AndSeenUnder10(int max, int seen, bool available, bool expected)
        {
            Assert.That(L10SequenceRule.OnLobby(max, seen, available), Is.EqualTo(expected));
        }

        [Test]
        public void AfterNext_AgreesWithMapMoments_L10Map()
        {
            Assert.That(MapMoments.ForVictory(10, null, true, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.L10Map));
            Assert.That(L10SequenceRule.AfterNext(10, 0, true), Is.True);
            Assert.That(MapMoments.ForVictory(10, null, false, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None));
            Assert.That(L10SequenceRule.AfterNext(10, 0, false), Is.False);
        }

        [Test]
        public void Next_InTheVictoryOf10_GoesToTheLobby_WithTheSequenceDue()
        {
            Fixture fx = Fixture.At(nextLevel: 10, maxLevel: 9, available: true);
            fx.App.Start(false);
            Assert.That(fx.App.L10MapDue, Is.False, "con max_level 9 aún no");
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(10));
            Assert.That(fx.App.L10MapDue, Is.False, "solo con el inicio a la vista");
            Win(fx.Flow);
            Assert.That(fx.Progress.MaxLevel, Is.EqualTo(10));

            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Lobby), "AD_GATE y después el inicio, con la secuencia encima");
            Assert.That(fx.Screens, Is.EqualTo(new[] { AppScreen.Lobby, AppScreen.Level, AppScreen.Lobby }));
            Assert.That(fx.DueAtLobby, Is.EqualTo(new[] { false, true }), "la vista la pide al entrar al inicio");
            Assert.That(fx.App.L10MapDue, Is.True);
            Assert.That(fx.App.PlayLevel, Is.EqualTo(11), "«Jugar · Nivel 11»");

            // Lo visto del Mapa hasta el 10 (al acabar, saltar o cerrar la secuencia): no se repite.
            fx.Progress.SaveMapSeenLevel(L10SequenceRule.Level);
            Assert.That(fx.App.L10MapDue, Is.False);
        }

        [Test]
        public void Next_InTheVictoryOf10_WithoutTheMap_GoesOnAsBefore()
        {
            Fixture fx = Fixture.At(nextLevel: 10, maxLevel: 9, available: false);
            fx.App.Start(false);
            fx.App.PressPlay();
            Win(fx.Flow);
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.ContentEnd), "el paquete de prueba acaba en el 10: N + 1 no está publicado");
            Assert.That(fx.App.L10MapDue, Is.False);
        }

        [Test]
        public void Next_InTheVictoryOf10_AlreadySeen_GoesOnAsBefore()
        {
            Fixture fx = Fixture.At(nextLevel: 10, maxLevel: 10, available: true, mapSeen: 10);
            fx.App.Start(false);
            Assert.That(fx.App.L10MapDue, Is.False, "ya vio el Mapa hasta el 10");
            fx.App.PressPlay();
            Win(fx.Flow);
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
        }

        [Test]
        public void Next_InTheVictoryOf9_LoadsThe10()
        {
            Fixture fx = Fixture.At(nextLevel: 9, maxLevel: 8, available: true);
            fx.App.Start(false);
            fx.App.PressPlay();
            Win(fx.Flow);
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(10));
        }

        [Test]
        public void Home_InTheVictoryOf10_TheSequenceIsDueAtTheLobby()
        {
            Fixture fx = Fixture.At(nextLevel: 10, maxLevel: 9, available: true);
            fx.App.Start(false);
            fx.App.PressPlay();
            Win(fx.Flow);
            Assert.That(fx.App.PressHome(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(fx.App.L10MapDue, Is.True, "P-M4: tras Inicio, al entrar al inicio");
            Assert.That(fx.DueAtLobby, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void AfterClosingTheApp_TheNextStart_HasTheSequenceDue_Once()
        {
            // El 10 ganado y la app cerrada en su victoria: el arranque siguiente va al inicio y la pide.
            var progress = new MemoryProgressStore(11, 10, 0);
            Fixture fx = Fixture.With(progress, available: true);
            fx.App.Start(false);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(fx.App.L10MapDue, Is.True);
            Assert.That(fx.DueAtLobby, Is.EqualTo(new[] { true }));

            // Se ve (map_seen_level 10): el arranque siguiente ya no.
            progress.SaveMapSeenLevel(10);
            Fixture again = Fixture.With(progress, available: true);
            again.App.Start(false);
            Assert.That(again.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(again.App.L10MapDue, Is.False);
            Assert.That(again.DueAtLobby, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void Lobby_WithoutTheMap_NeverHasTheSequenceDue()
        {
            Fixture fx = Fixture.With(new MemoryProgressStore(11, 10, 0), available: false);
            fx.App.Start(false);
            Assert.That(fx.App.L10MapDue, Is.False);
        }

        // Juega la solución del solver y avanza hasta VICTORY_SCREEN.
        private static void Win(LevelFlow flow)
        {
            flow.TapAnywhere();
            SolveResult result = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour move = result.GetMove(i);
                flow.TapCuia(move.Source);
                flow.TapCuia(move.Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            flow.Tick(16, true);
            flow.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        private sealed class Fixture : IAppFlowListener
        {
            private Fixture(MemoryProgressStore progress, bool available)
            {
                var economy = new EconomyFixture();
                Progress = progress;
                Flow = new LevelFlow(LevelCatalog.Create(new[] { TestPacks.Tutorial }), progress, new RecordingListener());
                App = new AppFlow(Flow, progress, economy.Wallet, this) { L10MapAvailable = available };
            }

            public MemoryProgressStore Progress { get; }

            public LevelFlow Flow { get; }

            public AppFlow App { get; }

            public List<AppScreen> Screens { get; } = new List<AppScreen>();

            /// <summary>Lo que daba <see cref="AppFlow.L10MapDue"/> en cada entrada al inicio, como lo mira la vista.</summary>
            public List<bool> DueAtLobby { get; } = new List<bool>();

            public static Fixture At(int nextLevel, int maxLevel, bool available, int mapSeen = 0)
            {
                return new Fixture(new MemoryProgressStore(nextLevel, maxLevel, mapSeen), available);
            }

            public static Fixture With(MemoryProgressStore progress, bool available)
            {
                return new Fixture(progress, available);
            }

            public void OnAppScreenChanged(AppScreen screen)
            {
                Screens.Add(screen);
                if (screen == AppScreen.Lobby)
                {
                    DueAtLobby.Add(App.L10MapDue);
                }
            }
        }
    }
}
