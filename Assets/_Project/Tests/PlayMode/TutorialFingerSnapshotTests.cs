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
    /// Capturas del dedo guía del tutorial (tarea DC6; D-028 con P5; 02 §13, filas 1 y 2), en <c>Logs/snapshots/es-419/</c>, a
    /// 360 × 780 dp y al 100 %, con la instrucción de TiTino como en el juego:
    /// <list type="bullet">
    /// <item><c>dc6_l1_source</c>, <c>dc6_l1_move</c> y <c>dc6_l1_target</c>: el 1, la mano tocando el origen, a medio viaje y
    /// tocando el destino;</item>
    /// <item><c>dc6_l2_source</c>, <c>dc6_l2_move</c> y <c>dc6_l2_target</c>: el 2 tras el primer vertido, la racha de 3 açaí
    /// sobre el hueco de 2, como en el juego: la instrucción de TiTino ya se fue al salir el dedo;</item>
    /// <item><c>dc6_l{1|2}_reduce</c>: con Reducir movimiento, la mano fija sobre el origen y la flecha al destino.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.TutorialFingerSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del dedo guía del tutorial (DC6); necesitan dispositivo gráfico.")]
    public sealed class TutorialFingerSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
            GameScreen.TutorialFingerOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.TutorialFingerOverride = null;
            GameScreen.TutorialSlothOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureTheLoop([Values(1, 2)] int level)
        {
            string dir = Prepare();
            yield return TutorialSlothTests.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            TutorialFinger finger = Reach(boot, level);
            SnapshotKit.Attach(boot);

            // La mano tocando el origen (en lo hondo del toque), a medio viaje y tocando el destino.
            StepTo(boot, finger, TutorialFinger.AppearMs + (0.5f * TutorialFinger.PressMs));
            yield return Save(boot, dir, "dc6_l" + level + "_source");
            StepTo(boot, finger, TutorialFinger.AppearMs + TutorialFinger.PressMs + (0.5f * TutorialFinger.MoveMs));
            yield return Save(boot, dir, "dc6_l" + level + "_move");

            StepTo(boot, finger, TutorialFinger.AppearMs + TutorialFinger.PressMs + TutorialFinger.MoveMs + (0.5f * TutorialFinger.PressMs));
            yield return Save(boot, dir, "dc6_l" + level + "_target");
        }

        [UnityTest]
        public IEnumerator CaptureReduceMotion([Values(1, 2)] int level)
        {
            string dir = Prepare();
            yield return TutorialSlothTests.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            TutorialFinger finger = Reach(boot, level);
            Assert.That(finger.ArrowVisible, Is.True, "la flecha al destino");
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600);
            yield return Save(boot, dir, "dc6_l" + level + "_reduce");
        }

        // Hasta el dedo a la vista: en el 2, tras el primer vertido de la solución (la cuia 0 a la vacía 3).
        private static TutorialFinger Reach(GameBootstrap boot, int level)
        {
            if (level == 2)
            {
                Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 6000);
            }

            return TutorialFingerTests.WaitForFinger(boot, "el " + level);
        }

        // Avanza fotograma a fotograma hasta que la vuelta del bucle pasa de ms.
        private static void StepTo(GameBootstrap boot, TutorialFinger finger, float ms)
        {
            for (int guard = 0; guard < 400 && finger.CycleTimeMs < ms; guard++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(finger.Shown, Is.True);
        }

        private static IEnumerator Save(GameBootstrap boot, string dir, string name)
        {
            TutorialFinger finger = boot.View!.TutorialFingerView!;
            Debug.Log("AF_TUTORIAL_FINGER " + name + ": " + finger.Source + " → " + finger.Target + ", t = "
                + finger.CycleTimeMs.ToString("0") + " ms, yema en " + finger.TipPosition + ", alfa " + finger.HandAlpha.ToString("0.00")
                + ", " + boot.View!.LastTutorialFingerProbe);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static string Prepare()
        {
            TutorialSlothTests.Hooks();
            GameScreen.TutorialFingerOverride = true;
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = 1f;
            return dir;
        }
    }
}
