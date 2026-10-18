#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Search;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Navegación entre pantallas (tarea B1-6; 08 §2.3; 06 §4.7; D-008 punto 6): Boot → Nivel 1 la primera vez o el inicio;
    /// Jugar → nivel guardado; Victoria → Siguiente (N + 1) o Inicio; «El río sigue creciendo» tras el último nivel publicado
    /// (08 §6.2) y Sin canoas desde el 31 (04 §7.1 y §7.3).
    /// </summary>
    public sealed class AppFlowTests
    {
        [Test]
        public void FirstRun_GoesStraightToLevel1_WithoutTheLobby()
        {
            Fixture fx = Fixture.Tutorial();
            fx.App.Start(true);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(1));
            Assert.That(fx.Screens, Is.EqualTo(new[] { AppScreen.Level }));
        }

        [Test]
        public void LaterRuns_OpenTheLobby_AndPlayLoadsTheSavedLevel()
        {
            Fixture fx = Fixture.Tutorial(nextLevel: 4);
            fx.App.Start(false);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(fx.Flow.HasLevel, Is.False, "el inicio no carga ningún nivel");
            Assert.That(fx.App.PlayLevel, Is.EqualTo(4));
            Assert.That(fx.App.PlayLevelAvailable, Is.True);
            Assert.That(fx.App.CanoesVisible, Is.False, "las canoas se ven desde el 31 (rescue_min_level)");

            Assert.That(fx.App.PressNext(), Is.False, "Siguiente solo en la victoria");
            Assert.That(fx.App.PressHome(), Is.False);
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(4));
            Assert.That(fx.App.PressPlay(), Is.False, "Jugar solo desde el inicio");
        }

        [Test]
        public void Victory_NextLoadsNPlus1_AndHomeReturnsToTheLobbyWithIt()
        {
            Fixture fx = Fixture.Tutorial();
            fx.App.Start(true);
            Win(fx.Flow);
            Assert.That(fx.Progress.NextLevel, Is.EqualTo(2));
            Assert.That(fx.App.PressHome(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(fx.App.PlayLevel, Is.EqualTo(2), "el botón del inicio dice «Jugar · Nivel 2»");

            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(2));
            Win(fx.Flow);
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(3));
            Assert.That(fx.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void AfterTheLastPublishedLevel_TheRiverKeepsGrowing()
        {
            // 08 §6.2 y 06 §9: tras el 10 (último del tutorial) Siguiente abre «El río sigue creciendo»; Volver va al inicio y
            // Jugar vuelve a abrirla mientras el 11 no esté publicado.
            Fixture fx = Fixture.Tutorial(nextLevel: 10);
            fx.App.Start(false);
            fx.App.PressPlay();
            Win(fx.Flow);
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(fx.App.OverlayLevel, Is.EqualTo(11));
            Assert.That(fx.App.ContentEndEtaDays, Is.EqualTo(14));

            Assert.That(fx.App.CloseOverlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(fx.App.PlayLevel, Is.EqualTo(11));
            Assert.That(fx.App.PlayLevelAvailable, Is.False);
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(fx.App.CloseOverlay(), Is.True);
            Assert.That(fx.App.CloseOverlay(), Is.False);
        }

        [Test]
        public void ContentEndMap_IsDueOnlyAfterWinningTheLastLevel_WithTheMap()
        {
            // 06 §9; D-013, punto 7; tarea M4.3: tras ganar el último nivel publicado, «Siguiente» abre «El río sigue creciendo»
            // y la vista abre antes el Mapa encima; con el contenido ya acabado, «Jugar» abre la pantalla sola.
            Fixture fx = Fixture.Tutorial(nextLevel: 10);
            fx.App.ContentEndMapAvailable = true;
            fx.App.Start(false);
            Assert.That(fx.App.ContentEndMapDue, Is.False, "en el inicio");
            fx.App.PressPlay();
            Win(fx.Flow);
            Assert.That(fx.App.ContentEndMapDue, Is.False, "en la victoria");
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(fx.App.ContentEndMapDue, Is.True, "tras ganar el último nivel publicado");
            Assert.That(fx.App.CloseOverlay(), Is.True);
            Assert.That(fx.App.ContentEndMapDue, Is.False, "al salir de la pantalla deja de cumplirse");
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(fx.App.ContentEndMapDue, Is.False, "«Jugar» con el contenido acabado: la pantalla sola");

            // Sin el Mapa en la escena, nunca.
            Fixture without = Fixture.Tutorial(nextLevel: 10);
            without.App.Start(false);
            without.App.PressPlay();
            Win(without.Flow);
            Assert.That(without.App.PressNext(), Is.True);
            Assert.That(without.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(without.App.ContentEndMapAvailable, Is.False, "false por defecto");
            Assert.That(without.App.ContentEndMapDue, Is.False);
        }

        [Test]
        public void From31_PlayingWithoutCanoes_OpensNoCanoes()
        {
            // 04 §7.1 «Qué bloquea» y 06 §4.7 (D-008): con el desplazamiento de desarrollo, el nivel 31 es el tablero del 1.
            Fixture fx = Fixture.Tutorial(nextLevel: 31, offset: 30);
            SpendAllCanoes(fx);
            fx.App.Start(false);
            Assert.That(fx.App.CanoesVisible, Is.True);
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(fx.App.OverlayLevel, Is.EqualTo(31));
            Assert.That(fx.Flow.HasLevel, Is.False, "sin canoas no se entra al nivel");
            Assert.That(fx.App.CloseOverlay(), Is.True);

            // El contador en 0/5 también la abre; con una canoa (o con Canoas infinitas) no.
            Assert.That(fx.App.PressCanoes(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.NoCanoes));
            fx.App.CloseOverlay();
            fx.Clock.Advance(TimeSpan.FromMinutes(25));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(1));
            Assert.That(fx.App.PressCanoes(), Is.False);
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(31));
        }

        [Test]
        public void InfiniteCanoes_LetYouPlayWithZeroCanoes()
        {
            Fixture fx = Fixture.Tutorial(nextLevel: 31, offset: 30);
            SpendAllCanoes(fx);
            Assert.That(fx.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromHours(1))).IsApplied, Is.True);
            fx.App.Start(false);
            Assert.That(fx.App.PressCanoes(), Is.False);
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
        }

        [Test]
        public void Before31_CanoesNeverBlock()
        {
            Fixture fx = Fixture.Tutorial(nextLevel: 30 - 20, offset: 20);
            SpendAllCanoes(fx);
            fx.App.Start(false);
            Assert.That(fx.App.PlayLevel, Is.EqualTo(21), "por debajo del catálogo, el primero (21)");
            Assert.That(fx.App.CanoesVisible, Is.False);
            Assert.That(fx.App.PressCanoes(), Is.False);
            Assert.That(fx.App.PressPlay(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
        }

        [Test]
        public void Victory_NextWithoutCanoesFrom31_OpensNoCanoes()
        {
            Fixture fx = Fixture.Tutorial(nextLevel: 31, offset: 30);
            fx.App.Start(false);
            fx.App.PressPlay();
            Win(fx.Flow);
            SpendAllCanoes(fx);
            Assert.That(fx.App.PressNext(), Is.True);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(fx.App.OverlayLevel, Is.EqualTo(32));
        }

        [Test]
        public void StartAtLevel_IsTheDevAndTestShortcut_AndStartOnlyRunsOnce()
        {
            Fixture fx = Fixture.Tutorial();
            fx.App.StartAtLevel(6);
            Assert.That(fx.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(fx.Flow.LevelNumber, Is.EqualTo(6));
            Assert.That(() => fx.App.Start(false), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TheLabelOfThePlayLevel_ComesFromThePack()
        {
            string level = TestPacks.MinimalLevel().Replace("\"difficulty\": { \"par\": 3 }", "\"difficulty\": { \"label\": \"very_hard\", \"par\": 3 }");
            LevelPack pack = LevelPackReader.Read(TestPacks.MinimalPack(level));
            Fixture fx = new Fixture(LevelCatalog.FromPack(pack), new MemoryProgressStore());
            fx.App.Start(false);
            Assert.That(fx.App.PlayLevelLabel, Is.EqualTo(LevelDefinition.LabelVeryHard));
        }

        [Test]
        public void ContentEndEta_IsTheDefaultOf08_9()
        {
            // live_content_end_message = {"next_batch_eta_days":14} en la tabla única de 08 §9 (guarda contra cifras inventadas).
            string path = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs", "gdd", "08-arquitectura-tecnica-publicacion.md");
            string row = string.Empty;
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith("| `live_content_end_message`", StringComparison.Ordinal))
                {
                    row = line;
                }
            }

            Match m = Regex.Match(row, "next_batch_eta_days\\\"?:\\s*(\\d+)");
            Assert.That(m.Success, Is.True, "fila de live_content_end_message en 08 §9");
            Assert.That(GameConfig.LiveContentEndNextBatchEtaDays, Is.EqualTo(int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static void SpendAllCanoes(Fixture fx)
        {
            while (fx.Wallet.Canoes.Count > 0)
            {
                Assert.That(fx.Wallet.Apply(Transaction.HardFail(31)).IsApplied, Is.True);
            }
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
            public Fixture(LevelCatalog catalog, MemoryProgressStore progress)
            {
                var economy = new EconomyFixture();
                Clock = economy.Clock;
                Wallet = economy.Wallet;
                Progress = progress;
                Flow = new LevelFlow(catalog, progress, new RecordingListener());
                App = new AppFlow(Flow, progress, Wallet, this);
            }

            public AF.Platform.ManualClock Clock { get; }

            public Wallet Wallet { get; }

            public MemoryProgressStore Progress { get; }

            public LevelFlow Flow { get; }

            public AppFlow App { get; }

            public List<AppScreen> Screens { get; } = new List<AppScreen>();

            public static Fixture Tutorial(int nextLevel = 0, int offset = 0)
            {
                return new Fixture(LevelCatalog.Create(new[] { TestPacks.Tutorial }, offset), new MemoryProgressStore(nextLevel));
            }

            public void OnAppScreenChanged(AppScreen screen)
            {
                Screens.Add(screen);
            }
        }
    }
}
