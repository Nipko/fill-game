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
    /// <c>83_pirana_bite_l183</c> (muerde: el bocado sale de la dosis de la víctima y sube por su eje),
    /// <c>83b_pirana_carry_l183</c> (se lleva el bocado) y <c>83c_pirana_cave_l183</c> (el bocado entra por la boca de la
    /// cueva); el mordisco fallido visible (tanda B2): <c>83d_pirana_lost_reach_l183</c> (llega sobre la tapa de la víctima
    /// sellada), <c>83e_pirana_lost_bump_l183</c> (el golpe: la que va delante contra la tapa, aplastada, con el
    /// destello en su punto máximo), <c>83f_pirana_lost_rebound_l183</c> (fin del rebote), <c>83g_pirana_lost_back_l183</c> (a medio camino de
    /// vuelta a la cueva) y <c>83h_pirana_lost_end_l183</c> (al acabar el paso: sobre el arco, con los puntos de después), y
    /// con la cueva llena <c>82c_pirana_peek_l183</c> (se asoma), <c>82d_pirana_asleep_again_l183</c> (vuelve a dormirse) y
    /// <c>82e_pirana_peek_up_l183</c> (con la víctima encima, en la columna de la cueva: se asoma hacia arriba; si el solver pasa
    /// por ese vertido). Sirven para mirar a
    /// 48 dp la legibilidad, los solapes, los colores y
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

        [UnityTest]
        public IEnumerator CapturePiranaBiteLost()
        {
            // Mordisco fallido visible (decisión C + D del dueño para el 183; tanda B2): el vertido sella la víctima, el cardumen
            // llega a su tapa, choca y vuelve (83d al llegar, 83e al chocar, 83f al acabar el rebote, 83g de vuelta y 83h al
            // acabar el paso).
            string dir = SnapshotKit.Prepare();
            yield return LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            int cave = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Pirana);
            int bit = 1 << cave;
            Pour[]? lost = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.PiranaBiteLostMask & bit) != 0 && ev.Receiver != cave && (ev.SealedMask & (1 << ev.Receiver)) != 0);
            Assert.That(lost, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, lost!);
            MechanicTestKit.Tap(boot, lost![lost.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.BiteLost, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.36f);
            Save(dir, "83d_pirana_lost_reach_l183", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, PiranaOverlay.LostHitAt + 0.03f);
            Save(dir, "83e_pirana_lost_bump_l183", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, PiranaOverlay.LostReboundEnd);
            Save(dir, "83f_pirana_lost_rebound_l183", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.8f);
            Save(dir, "83g_pirana_lost_back_l183", boot);
            for (int ms = 0; ms < 400 && boot.View!.Animator.Steps.TryGetRunning(MechanicStepKind.BiteLost, cave, out _, out _); ms++)
            {
                PlayModeHelpers.TickExact(boot, 1);
            }

            Save(dir, "83h_pirana_lost_end_l183", boot);
            MechanicTestKit.StepToIdle(boot, 4000);

            // Cueva llena (el 183 con la cueva ya llena, [acaí, agua del Río Negro]): con el cuarto vertido útil el cardumen se
            // despierta, se asoma hacia la víctima (82c) y vuelve a dormirse (82d).
            LaunchOptions.LevelPackJsonOverride = CaveFull183;
            yield return LoadPlaying(183);
            boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Session.Current[cave].IsFull, Is.True, "la cueva empieza llena");
            Pour[]? peek = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.PiranaBiteLostMask & bit) != 0 && ev.Receiver != cave);
            Assert.That(peek, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, peek!);
            MechanicTestKit.Tap(boot, peek![peek.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.BiteLost, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.5f);
            Save(dir, "82c_pirana_peek_l183", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "82d_pirana_asleep_again_l183", boot);

            // Cueva llena con la víctima encima, en la columna de la cueva (revisión de la tanda B2, ronda 2): la que va delante
            // se asoma hacia arriba, sin pasar de la repisa de la fila de encima (82e), si el solver encuentra ese vertido.
            LaunchOptions.LevelPackJsonOverride = CaveFull183;
            yield return LoadPlaying(183);
            boot = GameBootstrap.Current!;
            AF.Presentation.BoardAnimator animator = boot.View!.Animator;
            UnityEngine.Vector2 caveFoot = animator.Foot(cave);
            Pour[]? up = MechanicTestKit.FindTrigger(
                boot.Flow!,
                (ev, b, a) => (ev.PiranaBiteLostMask & bit) != 0 && ev.Receiver != cave
                    && UnityEngine.Mathf.Abs(animator.Foot(ev.Receiver).x - caveFoot.x) < 0.5f && animator.Foot(ev.Receiver).y > caveFoot.y);
            if (up == null)
            {
                TestContext.WriteLine("82e_pirana_peek_up_l183: el solver no pasa por un mordisco perdido contra una víctima de la columna de la cueva.");
                yield break;
            }

            MechanicTestKit.PlayPrefix(boot, up);
            MechanicTestKit.Tap(boot, up[up.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.BiteLost, cave, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.BiteLost, cave, 0.5f);
            Save(dir, "82e_pirana_peek_up_l183", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        // El 183 con la cueva (Cuia chica, la 8) ya llena: sus dos dosis salen de la cima de la cuia 0 (8 y 14, acaí y agua del
        // Río Negro), así que cada color sigue teniendo sus 4 dosis.
        private const string CaveFull183 =
            "{ \"pack_schema\": 2, \"pack_id\": \"test_pirana_llena\", \"kind\": \"campaign\", \"world\": 5, \"world_id\": \"w05_loreto\", \"lap\": 1, "
            + "\"water_base\": \"white\", \"levels\": [ { \"id\": \"m05_v1_n0183\", \"level\": 183, \"world\": 5, \"lap\": 1, \"water_base\": \"white\", "
            + "\"palette\": [\"cupuacu\", \"tapereba\", \"aguaje\", \"camu_camu\", \"bacaba\", \"acai\", \"agua_tapajos\", \"lulo\", \"agua_negro\"], "
            + "\"units_per_color\": 4, \"caps\": [4, 4, 4, 4, 4, 4, 4, 4, 2, 4, 4, 4], "
            + "\"cuias\": [[11, 11], [3, 6, 6, 10], [4, 2, 10, 7], [], [2, 2, 2, 6], [4, 4, 7, 14], [6, 3, 3, 4], [11, 11, 3, 7], [8, 14], [8, 8, 8, 7], [], [14, 14, 10, 10]], "
            + "\"attrs\": [null, null, null, null, null, null, null, null, {\"type\": \"pirana\", \"k\": 4}, null, null, null], "
            + "\"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";

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
