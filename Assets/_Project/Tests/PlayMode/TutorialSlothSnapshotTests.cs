#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de TiTino en el tutorial (tarea TU1a; 02 §13; propuesta de TiTino d.3 y paso 6e de (i)), en
    /// <c>Logs/snapshots/es-419/</c>:
    /// <list type="bullet">
    /// <item><c>tu1a_l{1|2|3|6}_780_100</c>: la instrucción de cada nivel a la vista, con el retrato y <c>sloth_present</c>;</item>
    /// <item><c>tu1a_l{3|6}_640_{100|130}</c> y <c>tu1a_l{3|6}_780_130</c>: la pantalla pequeña y la escala de texto;</item>
    /// <item><c>tu1a_l2_fruta_780_100</c>: «Fruta nueva: Taperebá» después de la instrucción del 2 (06 §2.2) [P].</item>
    /// </list>
    /// Cada captura comprueba los solapes (<see cref="TutorialSlothTests.AssertClear"/>) y escribe una línea <c>AF_TUTORIAL</c>.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.TutorialSlothSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de TiTino en el tutorial de 1, 2, 3 y 6 (TU1a); necesitan dispositivo gráfico.")]
    public sealed class TutorialSlothSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.TutorialSlothOverride = null;
            GameScreen.FruitNewOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureEachLevel()
        {
            foreach (int level in TutorialInstruction.Levels)
            {
                string dir = Prepare(false, 1f);
                yield return TutorialSlothTests.Load(level);
                yield return Capture(GameBootstrap.Current!, dir, "tu1a_l" + level + "_780_100");
                Release();
            }
        }

        [UnityTest]
        public IEnumerator CaptureSmallScreenAndLargeText()
        {
            foreach (int level in new[] { 3, 6 })
            {
                foreach (bool small in new[] { false, true })
                {
                    foreach (float scale in new[] { 1f, 1.3f })
                    {
                        if (!small && scale < 1.01f)
                        {
                            continue; // ya está en CaptureEachLevel
                        }

                        string dir = Prepare(small, scale);
                        yield return TutorialSlothTests.Load(level);
                        yield return Capture(GameBootstrap.Current!, dir,
                            "tu1a_l" + level + "_" + (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100"));
                        Release();
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator CaptureFruitNewAfterTheInstructionOfLevel2()
        {
            string dir = Prepare(false, 1f);
            GameScreen.FruitNewOverride = true;
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, "el 2");
            for (int t = 0; t < 9000 && !view.FruitNewView.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.FruitNewView.Visible, Is.True, "«Fruta nueva» después de la instrucción");
            Assert.That(card.Visible, Is.False);
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "tu1a_l2_fruta_780_100");
            Release();
        }

        private static IEnumerator Capture(GameBootstrap boot, string dir, string name)
        {
            GameScreen view = boot.View!;
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, name);
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600); // el gesto sloth_present termina de entrar (0,6 s)
            Assert.That(view.TutorialSlothVisible, Is.True, name + ": la instrucción, a la vista");
            TutorialSlothTests.AssertClear(view, name);
            Debug.Log("AF_TUTORIAL " + name + ": «" + card.Text + "» en " + card.Lines + " líneas a " + SlothFailTests.F(card.Label.fontSize)
                + " dp, " + (view.TutorialSlothOnDock ? "sobre el muelle" : "sobre el panorama") + ", " + SlothFailTests.F(card.Height) + " dp, "
                + SlothFailTests.F(card.DurationMs) + " ms");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static string Prepare(bool small, float scale)
        {
            TutorialSlothTests.Hooks();
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            return dir;
        }

        private static void Release()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
