#nullable enable
using System.Collections;
using AF.Core.Mechanics;
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
    /// Capturas del dedo guía de los estrenos de la Hoja (43) y de la Tinaja (323, variante «a») (tarea DC7; D-028 con P5 y
    /// D-029; 02 §9.1 M1 y M13), en <c>Logs/snapshots/es-419/</c>, a 360 × 780 dp y al 100 %, tras la tarjeta del estreno y la
    /// intro:
    /// <list type="bullet">
    /// <item><c>dc7_l43_source</c>, <c>dc7_l43_move</c> y <c>dc7_l43_target</c>: la mano tocando el origen (la fruta de la hoja), a
    /// medio viaje y tocando la cuia con hoja; <c>dc7_l43_reduce</c>, con Reducir movimiento;</item>
    /// <item><c>dc7_l323_scoop_source</c>, <c>dc7_l323_scoop_move</c> y <c>dc7_l323_scoop_target</c>: paso 1, de la tinaja a su
    /// cuia colgada;</item>
    /// <item><c>dc7_l323_pour_move</c> y <c>dc7_l323_pour_target</c>: paso 2, tras sacar, de su cuia a un destino;
    /// <c>dc7_l323_reduce</c>, el paso 1 con Reducir movimiento.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.DebutFingerSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del dedo guía de los estrenos de la Hoja y de la Tinaja (DC7); necesitan dispositivo gráfico.")]
    public sealed class DebutFingerSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.TutorialFingerOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.TutorialFingerOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Capture43()
        {
            string dir = Prepare();
            yield return DebutFingerTests.Load43();
            GameBootstrap boot = GameBootstrap.Current!;
            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 43");
            SnapshotKit.Attach(boot);
            yield return CaptureLoop(boot, finger, dir, "dc7_l43", true);
        }

        [UnityTest]
        public IEnumerator Capture43ReduceMotion()
        {
            string dir = Prepare();
            yield return DebutFingerTests.Load43();
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 43");
            Assert.That(finger.ArrowVisible, Is.True, "la flecha a la cuia con hoja");
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600);
            yield return Save(boot, dir, "dc7_l43_reduce");
        }

        [UnityTest]
        public IEnumerator Capture323BothSteps()
        {
            string dir = Prepare();
            yield return DebutFingerTests.Load323();
            GameBootstrap boot = GameBootstrap.Current!;
            int tinaja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Tinaja);
            int scoop = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Cucharon);
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 1");
            TutorialFinger finger = boot.View!.TutorialFingerView!;
            SnapshotKit.Attach(boot);
            yield return CaptureLoop(boot, finger, dir, "dc7_l323_scoop", true);

            Assert.That(boot.TapCuia(tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(scoop).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 2");
            finger = boot.View!.TutorialFingerView!;
            yield return CaptureLoop(boot, finger, dir, "dc7_l323_pour", false);
        }

        [UnityTest]
        public IEnumerator Capture323ReduceMotion()
        {
            string dir = Prepare();
            yield return DebutFingerTests.Load323();
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            yield return DebutFingerTests.WaitReal(boot, "el 323");
            TutorialFinger finger = boot.View!.TutorialFingerView!;
            Assert.That(finger.ArrowVisible, Is.True, "la flecha a su cuia");
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600);
            yield return Save(boot, dir, "dc7_l323_reduce");
        }

        // La mano tocando el origen (si withSource), a medio viaje y tocando el destino.
        private static IEnumerator CaptureLoop(GameBootstrap boot, TutorialFinger finger, string dir, string prefix, bool withSource)
        {
            if (withSource)
            {
                StepTo(boot, finger, TutorialFinger.AppearMs + (0.5f * TutorialFinger.PressMs));
                yield return Save(boot, dir, prefix + "_source");
            }

            StepTo(boot, finger, TutorialFinger.AppearMs + TutorialFinger.PressMs + (0.5f * TutorialFinger.MoveMs));
            yield return Save(boot, dir, prefix + "_move");
            StepTo(boot, finger, TutorialFinger.AppearMs + TutorialFinger.PressMs + TutorialFinger.MoveMs + (0.5f * TutorialFinger.PressMs));
            yield return Save(boot, dir, prefix + "_target");
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
            Debug.Log("AF_DEBUT_FINGER " + name + ": " + finger.Source + " → " + finger.Target + ", t = "
                + finger.CycleTimeMs.ToString("0") + " ms, yema en " + finger.TipPosition + ", " + boot.View!.LastTutorialFingerProbe);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static string Prepare()
        {
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = 1f;
            return dir;
        }
    }
}
