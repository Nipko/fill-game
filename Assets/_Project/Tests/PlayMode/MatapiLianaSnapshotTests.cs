#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Mechanics;
using AF.Core.Rules;
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
    /// Capturas de revisión del Matapi y de la Liana con fruta (T12; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>55_matapi_l63</c> (el Matapi tras la intro), <c>55b_matapi_reject_l63</c> (en el pico del latido del embudo
    /// al rechazarlo como origen), <c>55c_matapi_pattern_l63</c> (con 2 dosis dentro: el patrón de la fruta se sigue
    /// leyendo sin varillas delante, con el 15 % de <see cref="AF.Presentation.Palette.PatternOpacity"/>, porque el ajuste
    /// Patrones de fruta al 20 % aún no existe en el cliente), <c>55d_matapi_achiote_l63</c> (lo mismo con Achiote, que no
    /// debe leerse rayado como el Camu camu), <c>56a_matapi_cord_l63</c> (la cuerda floja, inclinada y en catenaria, antes
    /// del tirón), <c>56_matapi_seal_l63</c> (a mitad del tirón: la cuerda tensa y el embudo cerrándose),
    /// <c>56d_matapi_lift_l63</c> (en el pico de la subida de la trampa, con las gotas cayendo a la repisa por fuera de la
    /// silueta), <c>56b_matapi_cap_l63</c> (la tapa y el brillo), <c>56c_matapi_sealed_l63</c>
    /// (sellado y quieto: el embudo cerrado sobre el borde, bajo la tapa, y el cuello despejado), <c>57_liana_l83</c> (la
    /// Liana atada tras la intro), <c>58_liana_check_l83</c> (la ✓ sobre la fruta y el cometa saliendo de la tapa),
    /// <c>59_liana_beam_l83</c> (el cometa llegando a la fruta, bajo la repisa de arriba), <c>59b_liana_fall_l83</c> (la
    /// liana seca cayendo) y <c>59c_liana_beam_row1_l84</c> (el cometa desde una cuia de la fila de arriba, sin salirse del
    /// panel). Sirven para mirar a 48 dp la legibilidad, los solapes, los colores y la línea D-003 (juego premium, nunca
    /// papel ni acuarela). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.MatapiLianaSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MatapiLianaSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureMatapiL63()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "55_matapi_l63", boot);

            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Assert.That(boot.TapCuia(matapi).Kind, Is.EqualTo(TapKind.Rejected));
            PlayModeHelpers.TickExact(boot, (int)(MatapiOverlay.PulseMs * 0.5f));
            Save(dir, "55b_matapi_reject_l63", boot);
            PlayModeHelpers.Step(boot, 300);

            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.SealedMask & (1 << matapi)) != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Seal, matapi, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.07f);
            Save(dir, "56a_matapi_cord_l63", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.22f);
            Save(dir, "56_matapi_seal_l63", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.4f);
            Save(dir, "56d_matapi_lift_l63", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.8f);
            Save(dir, "56b_matapi_cap_l63", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "56c_matapi_sealed_l63", boot);
        }

        [UnityTest]
        public IEnumerator CaptureMatapiPatternL63()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.Receiver == matapi && a[matapi].Count == 2);
            Assert.That(path, Is.Not.Null, "vertidos que dejan 2 dosis en el Matapi");
            MechanicTestKit.PlayPrefix(boot, path!, skipLast: false);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "55c_matapi_pattern_l63", boot);
        }

        [UnityTest]
        public IEnumerator CaptureMatapiAchioteL63()
        {
            // El Matapi con Achiote (urucum, tinte 5: triángulos), el par rojo-rojo que confunde a un daltónico con el Camu camu
            // (rayas verticales): ninguna varilla cruza el líquido, así que los triángulos no se leen como rayas.
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.Receiver == matapi && a[matapi].TopColor == 5 && !a.IsSealed(matapi));
            Assert.That(path, Is.Not.Null, "vertidos que dejan Achiote en el Matapi");
            MechanicTestKit.PlayPrefix(boot, path!, skipLast: false);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "55d_matapi_achiote_l63", boot);
        }

        [UnityTest]
        public IEnumerator CaptureLianaL83()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(83);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "57_liana_l83", boot);

            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.2f);
            Save(dir, "58_liana_check_l83", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.36f);
            Save(dir, "59_liana_beam_l83", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.84f);
            Save(dir, "59b_liana_fall_l83", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        [UnityTest]
        public IEnumerator CaptureLianaTopRowL84()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(84);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            float lianaFootY = animator.Foot(liana).y;
            float u = boot.View.Views[liana].Unit;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0
                && a.IsSealed(ev.Receiver) && a[ev.Receiver].TopColor == key && animator.Foot(ev.Receiver).y > lianaFootY + u);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.28f);
            Save(dir, "59c_liana_beam_row1_l84", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        // Carga el nivel con el lienzo en la cámara de las capturas, quita el reloj, salta la intro y espera la vista quieta.
        private static IEnumerator LoadAttached(int level)
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
