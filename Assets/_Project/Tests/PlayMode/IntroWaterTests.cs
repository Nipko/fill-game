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
    /// <see cref="Palette.WaterBase"/>(<c>"black"</c>).
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
            // Tablero del nivel 1 del tutorial en el nivel 41, primero del mundo 3.
            string pack = PlayModeHelpers.BoardPack(
                3,
                "w03_manaos",
                "black",
                41,
                new[] { "acai", "aguaje" },
                new[] { new[] { 8, 8, 8, 4 }, new[] { 4, 4, 4, 8 }, new int[0] });
            yield return PlayModeHelpers.LoadGameWithPack(pack, 41, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(flow.Level.World, Is.EqualTo(3));
            Assert.That(flow.Level.WaterBase, Is.EqualTo("black"));

            Color black = Palette.WaterBase("black");
            Assert.That(black, Is.Not.EqualTo(Palette.WaterBase("white")), "las dos aguas se distinguen");
            Assert.That(view.IntroWaterColor, Is.EqualTo(black), "intro de la carga");

            // La intro repetida (reintento o depuración) vuelve a tomar el agua del nivel.
            view.DebugReplayIntro(1400, false);
            Assert.That(view.IntroWaterColor, Is.EqualTo(black), "intro repetida");
            view.DebugReplayIntro(800, true);
            Assert.That(view.IntroWaterColor, Is.EqualTo(black), "intro del reintento");
        }
    }
}
