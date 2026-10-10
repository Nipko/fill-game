#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de las Aguas turbias en el shader (T15; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>70_murky_l100</c> (las 2 ocultas del estreno, pardas con trama y «?»), <c>71_murky_tilt_l106</c> (la cuia turbia
    /// inclinada al verter: la trama y el «?» en su banda), <c>72_reveal_l100</c> (el revelado por la regla al 20 %: el «?» se
    /// deforma entre destellos), <c>72b_reveal_mid_l100</c> (al 45 %: burbuja y pardo que se aclara),
    /// <c>72c_firefly_l100</c> (la Luciérnaga a mitad: de arriba abajo y el icono de la fruta) y <c>73_murky_badge_l100</c>
    /// (tras el revelado, «?×1»). De cada una, además, un recorte a tamaño real de la cuia turbia (<c>*_zoom</c>) para mirar la
    /// legibilidad a 48-56 dp, los solapes, los colores y la línea D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode
    /// PlayMode --filter AF.Tests.PlayMode.MurkySnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MurkySnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureMurkyL100()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "70_murky_l100", boot, murky);

            // El revelado por la regla, a mitad.
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.RevealedMask != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            MechanicTestKit.Tap(boot, pour);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Reveal, pour.Source, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Reveal, pour.Source, 0.2f);
            Save(dir, "72_reveal_l100", boot, pour.Source);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Reveal, pour.Source, 0.45f);
            Save(dir, "72b_reveal_mid_l100", boot, pour.Source);
            MechanicTestKit.StepToIdle(boot, 4000);
            PlayModeHelpers.Step(boot, 600); // los últimos destellos y la burbuja se apagan
            Save(dir, "73_murky_badge_l100", boot, pour.Source);
            Assert.That(boot.View!.Views[pour.Source].Murky.BadgeCount, Is.EqualTo(1), "«?×1»");

            // La Luciérnaga (la usa T14) a mitad, sobre la oculta que queda y la revelada de nuevo turbia en la vista.
            CuiaView view = boot.View.Views[pour.Source];
            int mask = boot.Flow!.Session.Current[pour.Source].HiddenMask;
            Assert.That(mask, Is.Not.EqualTo(0));
            view.PlayReveal(mask, RevealStyle.Firefly, 1f);
            for (int t = 0; t < 330; t += 10)
            {
                view.Tick(10f);
            }

            Save(dir, "72c_firefly_l100", boot, pour.Source);
            view.SnapMarks();
            Assert.That(File.Exists(Path.Combine(dir, "70_murky_l100.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureMurkyTiltL106()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(106);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => a[murky].Count < b[murky].Count);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            CuiaView source = boot.View!.Views[murky];
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            float best = 0f;
            for (int t = 0; t < 3000; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
                float tilt = Mathf.Abs(source.Liquid.TiltDeg);
                if (source.BodyInFront && tilt >= best)
                {
                    best = tilt;
                }
                else if (best > 30f)
                {
                    break; // la inclinación máxima, mientras vierte
                }
            }

            Assert.That(best, Is.GreaterThan(30f), "inclinada");
            Save(dir, "71_murky_tilt_l106", boot, murky, source.Body);
            Assert.That(File.Exists(Path.Combine(dir, "71_murky_tilt_l106.png")), Is.True);
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

            boot.View!.Animator.Steps.ClearLog();
        }

        // La captura entera y un recorte a tamaño real (3 px/dp) alrededor de la cuia (o de target), con la repisa.
        private static void Save(string dir, string name, GameBootstrap boot, int cuia, RectTransform? target = null)
        {
            SnapshotKit.Save(dir, name);
            CuiaView view = boot.View!.Views[cuia];
            SaveCrop(dir, name + "_zoom", target ?? view.Root, 28f);
        }

        private static void SaveCrop(string dir, string name, RectTransform target, float marginDp)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture rt = cam.targetTexture;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(corners[i]);
                xMin = Mathf.Min(xMin, p.x);
                yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x);
                yMax = Mathf.Max(yMax, p.y);
            }

            float margin = marginDp * 3f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin - margin), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin - margin), 0, rt.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax + margin), x0 + 1, rt.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax + margin), y0 + 1, rt.height);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, x1 - x0, y1 - y0), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        private static int MurkyCuia(GameBootstrap boot)
        {
            BoardState b = boot.Flow!.Session.Current;
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].HasHidden)
                {
                    return i;
                }
            }

            Assert.Fail("El nivel " + boot.Flow.LevelNumber + " no tiene dosis ocultas");
            return -1;
        }
    }
}
