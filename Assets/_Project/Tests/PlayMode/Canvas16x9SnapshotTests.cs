#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de DC3 (pregunta 19 (a) de <c>docs/GDD.md</c> §6.3; D-028; <c>[Explicit]</c>, no forman parte de la suite): el
    /// lienzo del juego a 360 × 640 dp en 16:9 (1080 × 1920 px, <see cref="HudLabelTests.Screen640"/>), recortadas a los 1.920 px
    /// de arriba de la captura de <see cref="SnapshotKit"/>:
    /// <c>dc3_l1_16x9</c> (nivel 1), <c>dc3_l374_3filas_16x9</c> (13 cuias en 3 filas), <c>dc3_l&lt;n&gt;_14mas2_16x9</c> (14 cuias
    /// con la Cuia de rescate y la Cuia extra), <c>dc3_l&lt;n&gt;_14_stuck_16x9</c> (su Aguas estancadas), <c>dc3_l323_tinaja_16x9</c>,
    /// <c>dc3_l40_victoria_16x9</c> (victoria del hito 40 con la ranura del Dato del río: TiTino no cabe) y <c>dc3_l40_victoria_titino_sinranura_16x9</c> (sin la ranura, con TiTino, DC2), <c>dc3_l31_stuck_16x9</c> (Aguas estancadas, rama
    /// B) y <c>dc3_l31_pausa_16x9</c>. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.Canvas16x9SnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de DC3; necesitan dispositivo gráfico.")]
    public sealed class Canvas16x9SnapshotTests
    {
        private static int Crop => (int)HudLabelTests.Screen640.y;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            SlothVictoryTests.NoSlot(false);
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureBoards_Level1_ThreeRows_Tinaja323()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Canvas16x9LayoutTests.LoadPlaying(1);
            yield return Shoot(dir, "dc3_l1_16x9");

            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Canvas16x9LayoutTests.LoadPlaying(374);
            Assert.That(GameBootstrap.Current!.View!.Layout!.Rows, Is.EqualTo(3));
            yield return Shoot(dir, "dc3_l374_3filas_16x9");

            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return DebutFingerTests.Load323();
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            yield return Shoot(dir, "dc3_l323_tinaja_16x9");
        }

        [UnityTest]
        public IEnumerator CaptureFourteenPlusTwo()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap? boot = null;
            Pour[]? path = null;
            int level = 0;
            foreach (int candidate in Dc18SnapshotTests.FourteenCandidates)
            {
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(5000));
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                yield return Canvas16x9LayoutTests.LoadPlaying(candidate);
                boot = GameBootstrap.Current!;
                path = Dc18SnapshotTests.FindRescuableStuck(boot.Flow!);
                if (path != null)
                {
                    level = candidate;
                    break;
                }
            }

            Assert.That(path, Is.Not.Null);
            LevelFlow flow = boot!.Flow!;
            GameScreen view = boot.View!;
            MechanicTestKit.PlayPrefix(boot, path!, false);
            for (int ms = 0; ms < 8000 && flow.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 600);
            yield return Shoot(dir, "dc3_l" + level + "_14_stuck_16x9");
            view.Stuck.RescueButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 700);
            Assert.That(boot.PressExtraCuia(), Is.True);
            PlayModeHelpers.Step(boot, 700);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(view.Views.Length, Is.EqualTo(16), "14 + 2");
            yield return Shoot(dir, "dc3_l" + level + "_14mas2_16x9");
        }

        [UnityTest]
        public IEnumerator CaptureVictoryWithTheSloth_Hito40([Values(true, false)] bool slot)
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            SlothVictoryTests.NoSlot(!slot);
            yield return VictoryArrivalTests.Win(40, view => SnapshotKit.Attach(GameBootstrap.Current!));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 2500);
            Assert.That(boot.View!.SlothView.Visible, Is.EqualTo(!slot), "TiTino en su hito sin ranura; con ella, en 16:9 no cabe");
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, slot ? "dc3_l40_victoria_16x9" : "dc3_l40_victoria_titino_sinranura_16x9", Crop);
        }

        [UnityTest]
        public IEnumerator CaptureStuck_AndPause_Level31()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return Canvas16x9LayoutTests.LoadPlaying(31);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            yield return Shoot(dir, "dc3_l31_pausa_16x9");
            view.Pause.ResumeButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(view.StuckVisible, Is.True);
            yield return Shoot(dir, "dc3_l31_stuck_16x9");
        }

        private static IEnumerator Shoot(string dir, string name)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, name, Crop);
            SnapshotKit.LogBoard(name, boot);
        }
    }
}
