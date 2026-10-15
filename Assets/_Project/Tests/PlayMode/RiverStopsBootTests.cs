#nullable enable
using System.Collections;
using System.Text.RegularExpressions;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Carga de las paradas del Mapa del río en la escena <c>Game</c> (D-013; 08 §4.5; tarea M1.2), como la de los miquitos: la
    /// escena que genera <c>GameSceneBuilder</c> lleva <c>river_stops_v1.json</c> y, tras el arranque, <see cref="GameBootstrap"/>
    /// y la pantalla de nivel tienen el catálogo; sin el archivo o con uno inválido, un aviso y null, y el juego sigue igual.
    /// </summary>
    public sealed class RiverStopsBootTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Scene_LoadsTheRiverStops_IntoBootstrapAndScreen()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.RiverStopsAsset, Is.Not.Null, "la escena lleva river_stops_v1.json (GameSceneBuilder)");
            RiverStopCatalog? stops = boot.RiverStops;
            Assert.That(stops, Is.Not.Null, "el catálogo se lee al arrancar");
            Assert.That(stops!.Count, Is.EqualTo(20));
            Assert.That(boot.View!.RiverStops, Is.SameAs(stops), "la pantalla de nivel tiene el mismo catálogo");
            Assert.That(stops.StopOf(58)!.Id, Is.EqualTo("manaos"));
            Assert.That(stops.ArrivalOnWin(20)!.Id, Is.EqualTo("alter_do_chao"));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(1), "el nivel arranca como siempre");
        }

        [Test]
        public void LoadRiverStops_WithoutTheFile_WarnsAndGivesNull()
        {
            LogAssert.Expect(LogType.Warning, new Regex("faltan las paradas del Mapa del río"));
            Assert.That(GameBootstrap.LoadRiverStops(null), Is.Null);
        }

        [Test]
        public void LoadRiverStops_WithAnInvalidFile_WarnsWithThePathAndGivesNull()
        {
            var broken = new TextAsset("{\"format\": \"af-river-stops/1\", \"source\": \"x\", \"stops\": [{\"n\": 3}]}");
            LogAssert.Expect(LogType.Warning, new Regex(@"stops\[0\]\.n.*El juego sigue sin el Mapa"));
            Assert.That(GameBootstrap.LoadRiverStops(broken), Is.Null);
            Object.DestroyImmediate(broken);
        }

        [UnityTest]
        public IEnumerator WithoutTheCatalog_TheLevelStillPlays()
        {
            yield return PlayModeHelpers.LoadGame(58, new MemoryProgressStore(58, 57));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.View!.RiverStops = null; // como sin el archivo: nada del nivel depende aún de las paradas
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(58));
        }
    }
}
