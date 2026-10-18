#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.Core.Mechanics;
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
    /// Capturas de revisión del Brote de dos hojas y de la Flor de vitória-régia y de sus tarjetas de estreno (tarea VA1b;
    /// <c>[Explicit]</c>, no forman parte de la suite), con los paquetes de prueba del mundo 7 de <see cref="VariantsVisualTests"/>
    /// y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp), cada una con su recorte a tamaño real alrededor de la
    /// cuia (<c>_zoom</c>, 3 px/dp):
    /// <list type="bullet">
    /// <item><description>tarjetas: <c>va1b_01_debut_brote_n2_l343</c> y <c>va1b_02_debut_brote_n2_l343_130</c> (fuente al 130 %),
    /// <c>va1b_03_debut_flor_l403</c> y <c>va1b_04_debut_flor_l403_130</c>;</description></item>
    /// <item><description>Brote de dos hojas, junto al Brote en la fila de arriba (b.3 no los junta en un nivel; aquí solo para
    /// compararlos): <c>va1b_10_brote_vs_n2</c>, <c>va1b_11_brote_n2_leaf_opening</c> (a mitad de la hoja que se abre),
    /// <c>va1b_12_brote_n2_one_leaf</c> (una abierta y otra cerrada), <c>va1b_13_brote_n2_germinate</c> (a mitad de la
    /// germinación) y <c>va1b_14_brote_n2_l343</c> (el tablero de 12 cuias de la muestra de <c>pack_schema</c> 3);</description></item>
    /// <item><description>Flor: <c>va1b_20_flor_closed</c> (blanca, cerrada, con sus dos frutas), <c>va1b_21_flor_half</c> (una
    /// llave sellada: ✓ y entreabierta), <c>va1b_22_flor_opening</c> y <c>va1b_22b_flor_open</c> (el paso Release: rosa y
    /// abierta), <c>va1b_23_flor_released</c> (la cuia, ya normal) y <c>va1b_24_flor_l403</c> (el tablero de 11 cuias de la
    /// muestra, con la Flor en la última columna de la fila de abajo).</description></item>
    /// </list>
    /// Sirven para mirar a 47-56 dp que se distingan de su base (el Brote y la Liana), que se lea el color del capullo y las dos
    /// frutas, los solapes con las vecinas, con el marco y con la fila de arriba, y la línea D-003. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.VariantsSnapshotTests</c> sin <c>-nographics</c>; los
    /// PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class VariantsSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureCards()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureCard(dir, VariantsVisualTests.BroteDebutPack(), 343, "va1b_01_debut_brote_n2_l343", 1f);
            yield return CaptureCard(dir, VariantsVisualTests.BroteDebutPack(), 343, "va1b_02_debut_brote_n2_l343_130", 1.3f);
            yield return CaptureCard(dir, VariantsVisualTests.FlorDebutPack(), 403, "va1b_03_debut_flor_l403", 1f);
            yield return CaptureCard(dir, VariantsVisualTests.FlorDebutPack(), 403, "va1b_04_debut_flor_l403_130", 1.3f);
            Assert.That(File.Exists(Path.Combine(dir, "va1b_03_debut_flor_l403.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureBroteTwoLeaves()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelPackJsonOverride = VariantsVisualTests.BroteComparePack();
            yield return Load(343);
            GameBootstrap boot = GameBootstrap.Current!;
            const int two = VariantsVisualTests.CompareTwoLeaves;
            Save(dir, "va1b_10_brote_vs_n2", boot, two);

            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(two).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, two, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, two, 0.45f);
            Save(dir, "va1b_11_brote_n2_leaf_opening", boot, two);
            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            Save(dir, "va1b_12_brote_n2_one_leaf", boot, two);

            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(two).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, two, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, two, 0.5f);
            Save(dir, "va1b_13_brote_n2_germinate", boot, two);
            MechanicTestKit.StepToIdle(boot, 6000);

            FunnelSetup.Hooks();
            LaunchOptions.LevelPackJsonOverride = VariantsVisualTests.BroteDebutPack();
            yield return Load(343);
            boot = GameBootstrap.Current!;
            Save(dir, "va1b_14_brote_n2_l343", boot, MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Brote));
        }

        [UnityTest]
        public IEnumerator CaptureFlor()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelPackJsonOverride = VariantsVisualTests.FlorComparePack();
            yield return Load(403);
            GameBootstrap boot = GameBootstrap.Current!;
            const int flor = VariantsVisualTests.FlorIndex;
            Save(dir, "va1b_20_flor_closed", boot, flor);

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, (int)FlorOverlay.HalfOpenMs + 200);
            Save(dir, "va1b_21_flor_half", boot, flor);

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, flor, 6000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, flor, 0.32f);
            Save(dir, "va1b_22_flor_opening", boot, flor);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, flor, 0.6f);
            Save(dir, "va1b_22b_flor_open", boot, flor);
            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "va1b_23_flor_released", boot, flor);

            FunnelSetup.Hooks();
            LaunchOptions.LevelPackJsonOverride = VariantsVisualTests.FlorDebutPack();
            yield return Load(403);
            boot = GameBootstrap.Current!;
            Save(dir, "va1b_24_flor_l403", boot, MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Flor));
        }

        private static IEnumerator CaptureCard(string dir, string pack, int level, string name, float fontScale)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LevelPackJsonOverride = pack;
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
            GameScreen view = boot.View!;
            Assert.That(view.LevelCard.Visible, Is.True, "nivel " + level);
            if (fontScale != 1f)
            {
                view.CardFontScale = fontScale;
                PlayModeHelpers.Step(boot, 50);
            }

            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_VA1B " + name + ": " + view.LastCardProbe);
        }

        // Carga el nivel, salta la intro y deja la vista quieta en Playing.
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

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            PlayModeHelpers.Step(boot, 300);
            boot.View!.Animator.Steps.ClearLog();
        }

        // La captura entera, un recorte a tamaño real alrededor de la cuia y una línea con lo que muestra su overlay.
        private static void Save(string dir, string name, GameBootstrap boot, int cuia)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            CuiaView view = boot.View!.Views[cuia];
            SaveCrop(dir, name + "_zoom", view.Root, 30f);
            string what = string.Empty;
            if (view.OverlayOf(MechanicType.Brote) is BroteOverlay brote)
            {
                what = "brote dos hojas " + brote.TwoLeaves + ", cerradas " + brote.ClosedLeaves + ", tope "
                    + brote.VisibleTopU.ToString("0.00", CultureInfo.InvariantCulture) + " u";
            }

            if (view.OverlayOf(MechanicType.Flor) is FlorOverlay flor)
            {
                what = "flor llaves " + flor.ShownKey + "/" + flor.ShownKey2 + ", cumplidas " + flor.KeysMet + ", apertura "
                    + flor.Openness.ToString("0.00", CultureInfo.InvariantCulture) + ", rosa " + flor.Pinkness.ToString("0.00", CultureInfo.InvariantCulture);
            }

            TestContext.WriteLine("AF_VA1B " + name + ": cuia " + cuia + " de " + view.Unit.ToString("0.0", CultureInfo.InvariantCulture) + " dp/u; " + what);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

        // Recorte a tamaño real (3 px/dp) alrededor de target, con un margen.
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
    }
}
