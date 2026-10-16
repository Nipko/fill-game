#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «Llenar y teñir» usa el agua base del mundo del nivel (07 §2.1; glosario §11: <c>water_base</c>), no la del primer
    /// paquete del catálogo: un nivel del mundo 3 (Manaos, agua negra) arranca su intro con
    /// <see cref="Palette.WaterBase"/>(<c>"black"</c>) aunque el paquete diga <c>"white"</c>, que es con la que se hornea el
    /// arte compartido (<c>ProceduralArt.Water</c>).
    /// </summary>
    public sealed class IntroWaterTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Intro_World3_UsesBlackWater()
        {
            // Tablero del nivel 1 del tutorial en el nivel 41, primero del mundo 3. El agua del paquete (la del primer paquete
            // del catálogo, con la que se hornea el arte) es blanca y la del nivel, negra: el lector las lee por separado, así
            // que la intro solo sale negra si toma la del nivel.
            string pack = PlayModeHelpers.BoardPack(
                3,
                "w03_manaos",
                "black",
                41,
                new[] { "acai", "aguaje" },
                new[] { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, new int[0] })
                .Replace("\"water_base\": \"black\", \"levels\"", "\"water_base\": \"white\", \"levels\"");
            yield return PlayModeHelpers.LoadGameWithPack(pack, 41, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(flow.Catalog.WaterBase, Is.EqualTo("white"), "el agua del paquete (la del arte) es otra");
            Assert.That(flow.Level.World, Is.EqualTo(3));
            Assert.That(flow.Level.WaterBase, Is.EqualTo("black"));

            Color black = Palette.WaterBase("black");
            Color white = Palette.WaterBase("white");
            Assert.That(black, Is.Not.EqualTo(white), "las dos aguas se distinguen");
            Assert.That(view.IntroWaterColor, Is.EqualTo(black), "intro de la carga");
            Assert.That(view.IntroWaterColor, Is.Not.EqualTo(white), "intro de la carga: no el agua del paquete");

            // La intro repetida (reintento o depuración) vuelve a tomar el agua del nivel.
            view.DebugReplayIntro(1400, false);
            Assert.That(view.IntroWaterColor, Is.EqualTo(black), "intro repetida");
            view.DebugReplayIntro(800, true);
            Assert.That(view.IntroWaterColor, Is.EqualTo(black), "intro del reintento");
        }

        [UnityTest]
        public IEnumerator Intro_World3_RealCatalog_UsesBlackWater()
        {
            // Nota de la ola 1 (T04 → T06): con el catálogo real de la escena (6 paquetes, niveles 1-300) el agua del paquete
            // del catálogo (la del tutorial, blanca) y la del nivel 41 (mundo 3, Manaos, negra) son distintas, así que este caso
            // sí distingue si la intro toma la del nivel; el de arriba, con un solo paquete, solo lo simula.
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Catalog.Packs.Count, Is.EqualTo(6), "el catálogo real de la escena (con el mundo 6 desde C1a)");
            Assert.That(flow.Catalog.WaterBase, Is.EqualTo("white"), "el agua del primer paquete (tutorial)");
            Assert.That(flow.Level.World, Is.EqualTo(3));
            Assert.That(flow.Level.WaterBase, Is.EqualTo("black"));
            Assert.That(boot.View!.IntroWaterColor, Is.EqualTo(Palette.WaterBase("black")), "intro de la carga");
            Assert.That(boot.View.IntroWaterColor, Is.Not.EqualTo(Palette.WaterBase("white")));
        }
    }
}
