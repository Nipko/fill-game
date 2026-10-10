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
    /// Capturas de revisión de los estrenos de mecánicas tras la intro (T07; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>30_chica_l14</c>, <c>31_alta_l27</c>, <c>32_hoja_l43</c>, <c>33_matapi_l63</c>, <c>34_liana_l83</c>,
    /// <c>35_murky_l100</c>, <c>36_remolino_l103</c>, <c>37_barro_l123</c>, <c>38_lluvia_l143</c>, <c>39_brote_l163</c>,
    /// <c>39b_pirana_l183</c> y, a mitad de la cadena del 83, <c>39c_chain_l83</c> (la tapa del taperebá cayendo antes de que
    /// se suelte la Liana) y <c>39d_chain_done_l83</c> al terminar. Los pasos por defecto de la cola, a mitad:
    /// <c>39e_reveal_l100</c> (fundido del pardo al tinte), <c>39f_drop_l143</c> (la gota sube como una dosis) y
    /// <c>39g_bite_l183</c> (la víctima pierde la dosis de arriba) y <c>39h_drop_source_l143</c> (la gota de una Lluvia que
    /// vierte, con la cuia ya de vuelta en su ranura). Sirven para mirar a 48 dp la legibilidad, los solapes (la hoja de 2,3 u sobre la cuia de 2 u, la
    /// piraña que asoma, la fruta de la Liana sobre la dosis de arriba), los colores y la línea D-003. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MechanicSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MechanicSnapshotTests
    {
        private static readonly int[] Levels = { 14, 27, 43, 63, 83, 100, 103, 123, 143, 163, 183 };

        private static readonly string[] Names =
        {
            "30_chica_l14", "31_alta_l27", "32_hoja_l43", "33_matapi_l63", "34_liana_l83", "35_murky_l100", "36_remolino_l103",
            "37_barro_l123", "38_lluvia_l143", "39_brote_l163", "39b_pirana_l183",
        };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureMechanicDebuts()
        {
            string dir = SnapshotKit.Prepare();
            for (int i = 0; i < Levels.Length; i++)
            {
                yield return PlayModeHelpers.LoadGame(Levels[i], new MemoryProgressStore(Levels[i], Levels[i] - 1));
                GameBootstrap boot = GameBootstrap.Current!;
                SnapshotKit.Attach(boot);
                PlayModeHelpers.TakeClock(boot);
                boot.TapAnywhere();
                for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                PlayModeHelpers.Step(boot, 300);
                SnapshotKit.Save(dir, Names[i]);
                SnapshotKit.LogBoard(Names[i], boot);
                Assert.That(File.Exists(Path.Combine(dir, Names[i] + ".png")), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator CaptureChainL83()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(83, new MemoryProgressStore(83, 82));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            TapResult poured = MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Seal, poured.Pour.Receiver, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, poured.Pour.Receiver, 0.45f);
            SnapshotKit.Save(dir, "39c_chain_l83");
            Assert.That(boot.View!.Views[liana].Overlay!.Visible, Is.True, "a mitad del sellado la Liana sigue atada");
            MechanicTestKit.StepToIdle(boot, 4000);
            SnapshotKit.Save(dir, "39d_chain_done_l83");
            Assert.That(File.Exists(Path.Combine(dir, "39c_chain_l83.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureDefaultSteps()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureMidStep(dir, 100, "39e_reveal_l100", MechanicStepKind.Reveal, (ev, b, a) => ev.RevealedMask != 0, 0.4f);
            yield return CaptureMidStep(dir, 143, "39f_drop_l143", MechanicStepKind.RainDrop, (ev, b, a) => ev.LluviaDropMask != 0 && ev.LluviaEndedMask == 0, 0.5f);
            yield return CaptureMidStep(dir, 183, "39g_bite_l183", MechanicStepKind.Bite, (ev, b, a) => ev.PiranaCaveMask != 0, 0.25f);
            yield return CaptureMidStep(dir, 143, "39h_drop_source_l143", MechanicStepKind.RainDrop, (ev, b, a) => ev.LluviaEndedMask == 0 && DropOnSource(ev.LluviaDropMask, b, a), 0.5f);
        }

        // Cae una gota en una Lluvia que vierte (Q-B): la cuia de la gota tiene, después, como mucho lo que tenía antes.
        private static bool DropOnSource(int dropMask, AF.Core.Board.BoardState before, AF.Core.Board.BoardState after)
        {
            for (int i = 0; i < before.Count; i++)
            {
                if (((dropMask >> i) & 1) != 0 && after[i].Count <= before[i].Count)
                {
                    return true;
                }
            }

            return false;
        }

        // Carga el nivel, juega hasta el vertido que dispara el paso y captura a la fracción pedida de ese paso.
        private static IEnumerator CaptureMidStep(string dir, int level, string name, MechanicStepKind kind, MechanicTestKit.PourWanted wanted, float fraction)
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

            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, name);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, kind, -1, 4000);
            MechanicTestKit.StepToFraction(boot, kind, -1, fraction);
            SnapshotKit.Save(dir, name);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }
    }
}
