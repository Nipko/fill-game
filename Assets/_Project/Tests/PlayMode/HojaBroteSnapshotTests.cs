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
    /// Capturas de revisión de la Hoja y el Brote (T11; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>50_hoja_closed_l43</c> (la hoja de palma cerrada con el aguaje, tras la intro), <c>51_hoja_falling_l43</c> (a mitad
    /// del desbloqueo: la hoja abierta cae girando al agua), <c>51b_hoja_splash_l43</c> (la hoja toca el agua, salpica y se
    /// desvanece aclarándose hacia la luz, a mitad de ese tramo),
    /// <c>52_brote_closed_l163</c> (el tapón-brote de camu camu con sus
    /// dos hojitas plegadas) y <c>53_brote_open_l163</c> (a mitad de la germinación). Sirven para mirar a 48-56 dp la
    /// legibilidad, los solapes con las vecinas, los colores, que se entienda la mecánica, que la Hoja no se confunda con la
    /// Hoja escudo y la línea D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.HojaBroteSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class HojaBroteSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureHoja()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureClosed(dir, 43, "50_hoja_closed_l43");
            yield return CaptureMidOpen(dir, 43, MechanicType.Hoja, "51_hoja_falling_l43", 0.55f);
            yield return CaptureMidOpen(dir, 43, MechanicType.Hoja, "51b_hoja_splash_l43", 0.84f);
        }

        [UnityTest]
        public IEnumerator CaptureBrote()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureClosed(dir, 163, "52_brote_closed_l163");
            yield return CaptureMidOpen(dir, 163, MechanicType.Brote, "53_brote_open_l163", 0.42f);
        }

        // Carga el nivel, salta la intro y captura con la vista quieta.
        private static IEnumerator CaptureClosed(string dir, int level, string name)
        {
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

        // Juega hasta el vertido que abre la mecánica y captura a la fracción pedida de su paso Open.
        private static IEnumerator CaptureMidOpen(string dir, int level, MechanicType type, string name, float fraction)
        {
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            int owner = MechanicTestKit.IndexOf(boot.Flow!, type);
            int bit = 1 << owner;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ((type == MechanicType.Hoja ? ev.HojaOpenedMask : ev.BroteOpenedMask) & bit) != 0);
            Assert.That(path, Is.Not.Null, name);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, owner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, owner, fraction);
            SnapshotKit.Save(dir, name);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

        private static IEnumerator Load(int level)
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
        }
    }
}
