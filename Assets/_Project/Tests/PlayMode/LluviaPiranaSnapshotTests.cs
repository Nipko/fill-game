#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la Lluvia y la Piraña (T17; <c>[Explicit]</c>, no forman parte de la suite), tras la intro de
    /// sus estrenos: <c>80_lluvia_l143</c> (la nube con un vertido útil contado y dos gotas pendientes),
    /// <c>81_lluvia_drop_l143</c> (la gota cayendo por la boca) y <c>81b_lluvia_impact_l143</c> (la dosis formándose con la
    /// salpicadura), <c>81c_lluvia_end_l143</c> (la última gota y la nube que se va), <c>80b_lluvia_wait_l143</c> y
    /// <c>80c_lluvia_full_l143</c> (cuia llena: la gota intenta caer y queda colgando, si el nivel llega a ese estado),
    /// <c>82_pirana_l183</c> (la cueva y el cardumen rondando), <c>82b_pirana_asleep_l183</c> (cueva llena: duerme),
    /// <c>83_pirana_bite_l183</c> (muerde a la víctima), <c>83b_pirana_carry_l183</c> (se lleva el bocado) y
    /// <c>83c_pirana_cave_l183</c> (lo deja en la cueva). Sirven para mirar a 48 dp la legibilidad, los solapes, los colores y
    /// la línea D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.LluviaPiranaSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class LluviaPiranaSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureLluvia()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            int rain = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Lluvia);
            int bit = 1 << rain;

            // Un vertido útil contado: un punto lleno y dos gotitas.
            Pour[]? useful = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => !ev.Futile && (ev.LluviaDropMask & bit) == 0 && (ev.LluviaWaitingMask & bit) == 0);
            Assert.That(useful, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, useful!, false);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "80_lluvia_l143", boot);

            // La gota: cayendo y formando la dosis.
            Pour[]? drop = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LluviaDropMask & bit) != 0 && ev.LluviaEndedMask == 0);
            Assert.That(drop, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, drop!);
            MechanicTestKit.Tap(boot, drop![drop.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.RainDrop, rain, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.RainDrop, rain, 0.55f);
            Save(dir, "81_lluvia_drop_l143", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.RainDrop, rain, 0.8f);
            Save(dir, "81b_lluvia_impact_l143", boot);
            MechanicTestKit.StepToIdle(boot, 4000);

            // La última gota: la nube sube y se desvanece.
            Pour[]? end = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LluviaEndedMask & bit) != 0);
            if (end != null)
            {
                MechanicTestKit.PlayPrefix(boot, end);
                MechanicTestKit.Tap(boot, end[end.Length - 1]);
                MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.RainEnd, rain, 4000);
                MechanicTestKit.StepToFraction(boot, MechanicStepKind.RainEnd, rain, 0.6f);
                Save(dir, "81c_lluvia_end_l143", boot);
            }
            else
            {
                TestContext.WriteLine("81c_lluvia_end_l143: no se encontró la última gota dentro de los límites.");
            }

            // Cuia llena: la gota espera colgando (si el nivel llega a ese estado dentro de los límites de búsqueda).
            yield return LoadPlaying(143);
            boot = GameBootstrap.Current!;
            Pour[]? wait = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LluviaWaitingMask & bit) != 0);
            if (wait != null)
            {
                MechanicTestKit.PlayPrefix(boot, wait);
                MechanicTestKit.Tap(boot, wait[wait.Length - 1]);
                MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.RainWait, rain, 4000);
                MechanicTestKit.StepToFraction(boot, MechanicStepKind.RainWait, rain, 0.5f);
                Save(dir, "80b_lluvia_wait_l143", boot);
                MechanicTestKit.StepToIdle(boot, 4000);
                PlayModeHelpers.Step(boot, 300);
                Save(dir, "80c_lluvia_full_l143", boot);
            }
            else
            {
                TestContext.WriteLine("80b_lluvia_wait_l143: el nivel no llega a una Lluvia llena dentro de los límites.");
            }
        }

        [UnityTest]
        public IEnumerator CapturePirana()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "82_pirana_l183", boot);

            // El mordisco: en la víctima, llevándose el bocado y dejándolo en la cueva.
            Pour[]? bite = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.PiranaCaveMask & (1 << cave)) != 0);
            Assert.That(bite, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, bite!);
            MechanicTestKit.Tap(boot, bite![bite.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Bite, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.55f);
            Save(dir, "83_pirana_bite_l183", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.76f);
            Save(dir, "83b_pirana_carry_l183", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Bite, cave, 0.92f);
            Save(dir, "83c_pirana_cave_l183", boot);
            MechanicTestKit.StepToIdle(boot, 4000);

            // Cueva llena: el cardumen duerme atenuado.
            Pour[]? full = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => a[cave].IsFull);
            if (full != null)
            {
                MechanicTestKit.PlayPrefix(boot, full, false);
                PlayModeHelpers.Step(boot, 300);
                Save(dir, "82b_pirana_asleep_l183", boot);
            }
            else
            {
                TestContext.WriteLine("82b_pirana_asleep_l183: la cueva no se llena dentro de los límites.");
            }
        }

        // Carga el nivel con la cámara de capturas, salta la intro y deja la vista quieta en Playing.
        private static IEnumerator LoadPlaying(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            boot.View!.Animator.Steps.ClearLog();
        }

        private static void Save(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True, name);
        }
    }
}
