#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la curva 2 de la 1.1 (D-068, fase F4; <c>[Explicit]</c>, no forman parte de la suite), con los
    /// paquetes de prueba de <see cref="CurveFixtures"/> y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp):
    /// <c>f4_aguape_card_l303</c> (la tarjeta del estreno, Difícil, con el icono del Aguapé), <c>f4_aguape_board_l303</c> (la mata
    /// del grupo, con el puente), <c>f4_aguape_loosen_l303</c> (a mitad de la suelta), <c>f4_aguape_n2_board</c> (el espeso),
    /// <c>f4_aguape_n2_card</c> (la tarjeta del estreno del espeso), <c>f4_lancha_card_l363</c> (la del estreno de la Lancha, con
    /// su k), <c>f4_lancha_board</c> (el muelle y la lancha lejos), <c>f4_lancha_dock</c> (a mitad del atraque) y
    /// <c>f4_lancha_docked</c> (ya atracada). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.CurveSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class CurveSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureAguape()
        {
            string dir = SnapshotKit.Prepare();
            yield return Card(dir, Campaign303(), 303, "f4_aguape_card_l303");

            yield return Board(Campaign303(), 303);
            GameBootstrap boot = GameBootstrap.Current!;
            Save(dir, "f4_aguape_board_l303");
            Assert.That(MechanicTestKit.Tap(boot, new AF.Core.Rules.Pour(2, 1, 1)).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(MechanicTestKit.Tap(boot, new AF.Core.Rules.Pour(2, 0, 1)).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Loosen, 0, 6000);
            PlayModeHelpers.Step(boot, 250);
            Save(dir, "f4_aguape_loosen_l303");
        }

        [UnityTest]
        public IEnumerator CaptureThickAguape()
        {
            string dir = SnapshotKit.Prepare();
            string pack = CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 703, CurveFixtures.ThickCuias, CurveFixtures.ThickAttrs, ", \"intro_mechanic\": \"aguape_n2\""));
            yield return Card(dir, pack, 703, "f4_aguape_n2_card"); // el 703: el 701 abre el mundo con su banner, antes de la tarjeta
            yield return Board(pack, 703);
            Save(dir, "f4_aguape_n2_board");
        }

        [UnityTest]
        public IEnumerator CaptureLancha()
        {
            string dir = SnapshotKit.Prepare();
            string card = CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs, ", \"intro_mechanic\": \"lancha\""));
            yield return Card(dir, card, 702, "f4_lancha_card_l363");

            string pack = CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs));
            yield return Board(pack, 702);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 600); // la lancha, en su sitio
            Save(dir, "f4_lancha_board");
            Assert.That(MechanicTestKit.Tap(boot, new AF.Core.Rules.Pour(1, 2, 2)).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Dock, 0, 6000);
            PlayModeHelpers.Step(boot, 600);
            Save(dir, "f4_lancha_dock");
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Save(dir, "f4_lancha_docked");
            Assert.That(boot.Flow!.Session.Current[0].Mechanic, Is.EqualTo(MechanicType.None));
        }

        /// <summary>
        /// Fase F5 (D-068): los tableros de los packs reales de la curva 2 (la escena, sin paquetes del test) para la curación del
        /// dueño (<c>docs/dev/curva-1.1-curacion.md</c>): los estrenos del Aguapé (303), del espeso (347) y de la Lancha (363), y tres
        /// hitos (380, el de bot atento más bajo; 560 y 700, cúspides). PNG <c>f5_l{nivel}</c> en <c>Logs/snapshots/es-419/</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureTheF5Packs()
        {
            string dir = SnapshotKit.Prepare();
            foreach (int level in new[] { 303, 347, 363, 380, 560, 700 })
            {
                LaunchOptions.Curve2EnabledOverride = true;
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
                yield return MechanicTestKit.LoadPlaying(level);
                GameBootstrap boot = GameBootstrap.Current!;
                Assert.That(boot.Flow!.Level.Curve, Is.EqualTo(AF.Levels.LevelDefinition.Curve2), "el " + level + " de la curva 2");
                SnapshotKit.Attach(boot);
                PlayModeHelpers.Step(boot, 600);
                yield return SnapshotKit.Wait(0.3f);
                Save(dir, "f5_l" + level);
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
                SnapshotKit.Prepare();
            }
        }

        private static string Campaign303()
        {
            return CurveFixtures.Pack("mundo_07_v1", "campaign", 5, 7, CurveFixtures.AguapeCurveLevel(303)
                .Replace("\"id\": \"m07_v1_n0303_c2\"", "\"id\": \"m07_v1_n0303\"")
                .Replace(", \"replaces\": \"m07_v1_n0303\"", string.Empty));
        }

        private static IEnumerator Card(string dir, string pack, int level, string name)
        {
            LaunchOptions.LevelPackJsonOverride = pack;
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(boot.View!.LevelCard.Visible, Is.True, name);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            SnapshotKit.Prepare();
        }

        private static IEnumerator Board(string pack, int level)
        {
            LaunchOptions.LevelPackJsonOverride = pack;
            yield return MechanicTestKit.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 300);
        }

        private static void Save(string dir, string name)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, GameBootstrap.Current!);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }
    }
}
