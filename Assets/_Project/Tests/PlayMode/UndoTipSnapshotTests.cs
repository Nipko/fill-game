#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del respaldo del tooltip de Marea en el 5 (tarea DC23; D-030, pregunta 15 (b); 02 §13: «o al inicio del nivel 5
    /// si no ocurrió»), en <c>Logs/snapshots/es-419/</c>: <c>dc23_l5_{780|640}_{100|130}</c>, el tooltip a la vista tras la
    /// intro del 5 con un perfil que no lo vio en el 3, con la cola de la tarjeta de TiTino señalando el botón de Marea. Cada
    /// captura comprueba los solapes (<see cref="TutorialSlothTests.AssertClear"/>) y escribe una línea <c>AF_TUTORIAL</c>. A 640
    /// (16:9 desde DC3, D-028) TiTino no cabe y la captura enseña el tooltip como aviso de texto.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.UndoTipSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del tooltip de Marea en el 5 (DC23); necesitan dispositivo gráfico.")]
    public sealed class UndoTipSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.TutorialSlothOverride = null;
            GameScreen.TutorialBandOverrideDp = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureLevel5()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    TutorialSlothTests.Hooks();
                    string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    string name = "dc23_l5_" + (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100");

                    LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500)); // sin el tooltip visto
                    yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
                    GameBootstrap boot = GameBootstrap.Current!;
                    PlayModeHelpers.TakeClock(boot);
                    boot.TapAnywhere();
                    PlayModeHelpers.Step(boot, 50);

                    GameScreen view = boot.View!;
                    if (small && TutorialMorphoTests.AsToastIn16x9(boot, name))
                    {
                        // 16:9 desde DC3 (D-028): sin sitio para TiTino, el tooltip sale como aviso de texto (como el de Morpho en
                        // el 12): la captura enseña ese aviso, con el mismo nombre.
                        SnapshotKit.Attach(boot);
                        PlayModeHelpers.Step(boot, 300);
                        Debug.Log("AF_TUTORIAL " + name + ": sin sitio para TiTino, aviso de texto «" + view.ToastText + "», " + view.LastTutorialProbe);
                        yield return SnapshotKit.Wait(0.2f);
                        SnapshotKit.Save(dir, name);
                        SnapshotKit.Release();
                        LaunchOptions.Reset();
                        continue;
                    }

                    SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, name);
                    SnapshotKit.Attach(boot);
                    PlayModeHelpers.Step(boot, 600); // el gesto sloth_present termina de entrar (0,6 s)
                    Assert.That(view.TutorialSlothVisible, Is.True, name + ": el tooltip, a la vista");
                    Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.UndoKey)), name);
                    Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.True, name + ": apuntado");
                    TutorialSlothTests.AssertClear(view, name);
                    if (view.TutorialPointsAtUndo)
                    {
                        TutorialSlothTests.AssertTail(view, name);
                    }

                    if (view.TutorialSlothOnDock)
                    {
                        TutorialSlothTests.AssertDockAir(view, name);
                        TutorialSlothTests.AssertPostUnder(view, name);
                    }

                    Debug.Log("AF_TUTORIAL " + name + ": «" + card.Text + "» en " + card.Lines + " líneas a "
                        + SlothFailTests.F(card.Label.fontSize) + " dp, " + (view.TutorialSlothOnDock ? "sobre el muelle" : "sobre el panorama")
                        + (view.TutorialPointsAtUndo ? ", con la cola sobre Marea" : ", sin cola") + ", " + view.LastTutorialProbe);
                    yield return SnapshotKit.Wait(0.2f);
                    SnapshotKit.Save(dir, name);
                    SnapshotKit.Release();
                    LaunchOptions.Reset();
                }
            }
        }
    }
}
