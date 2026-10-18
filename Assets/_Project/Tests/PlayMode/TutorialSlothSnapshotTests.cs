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
    /// <item><c>tu1a_l6_dock_640_130</c>: la instrucción sobre el muelle, con la franja del panorama forzada por debajo de 64 dp.</item>
    /// </list>
    /// En el 3, el tooltip de Marea sale tras el vertido tentador (que no acerca a la solución), con la cola de la tarjeta de
    /// TiTino señalando el botón de Marea (04 §2.1). En el 2 ya no hay captura de «Fruta nueva»: una tarjeta por nivel como mucho
    /// (D-023, punto 39).
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
            GameScreen.TutorialBandOverrideDp = null;
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
                if (level == TutorialInstruction.MorphoLevel)
                {
                    continue; // la presentación de Morpho del 12 tiene sus capturas (TU1b: TutorialMorphoSnapshotTests)
                }

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
        public IEnumerator CaptureTheDockWithTheBandUnder64dp()
        {
            // La otra rama de TU1a: con la franja del panorama por debajo de 64 dp (forzada: en 1, 2, 3 y 6 mide más), sobre el
            // muelle, a ≥ 8 dp del marco del tablero y a ≥ 12 dp del marco de Marea; el poste que tape se funde con ella.
            string dir = Prepare(true, 1.3f);
            GameScreen.TutorialBandOverrideDp = 40f;
            yield return TutorialSlothTests.Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return Capture(boot, dir, "tu1a_l6_dock_640_130");
            Assert.That(boot.View!.TutorialSlothOnDock, Is.True, "sobre el muelle");
            Release();
        }

        private static IEnumerator Capture(GameBootstrap boot, string dir, string name)
        {
            GameScreen view = boot.View!;
            TutorialSlothTests.PourIfUndoLevel(boot, boot.Flow!.LevelNumber); // el tooltip de Marea del 3 sale tras el vertido
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, name);
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600); // el gesto sloth_present termina de entrar (0,6 s)
            Assert.That(view.TutorialSlothVisible, Is.True, name + ": la instrucción, a la vista");
            TutorialSlothTests.AssertClear(view, name);
            if (TutorialInstruction.PointsAtUndo(boot.Flow!.LevelNumber))
            {
                Assert.That(view.TutorialPointsAtUndo, Is.True, name + ": la cola señala Marea (04 §2.1)");
                TutorialSlothTests.AssertTail(view, name);
            }

            if (view.TutorialSlothOnDock)
            {
                TutorialSlothTests.AssertDockAir(view, name);
                TutorialSlothTests.AssertPostUnder(view, name);
            }

            Debug.Log("AF_TUTORIAL " + name + ": «" + card.Text + "» en " + card.Lines + " líneas a " + SlothFailTests.F(card.Label.fontSize)
                + " dp, " + (view.TutorialSlothOnDock ? "sobre el muelle" : "sobre el panorama") + ", " + SlothFailTests.F(card.Height) + " dp, "
                + SlothFailTests.F(card.DurationMs) + " ms" + (view.TutorialPointsAtUndo ? ", con la cola sobre Marea" : string.Empty)
                + ", " + view.LastTutorialProbe);
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
