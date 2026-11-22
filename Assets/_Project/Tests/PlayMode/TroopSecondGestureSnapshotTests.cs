#nullable enable
using System.Collections;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del segundo gesto de los miquitos (F13; añadido de D-029 del 2026-11-08) a 360 × 780 dp, en
    /// <c>Logs/snapshots/</c>: <c>f13_&lt;nivel&gt;_&lt;gesto&gt;_a</c> y <c>_b</c>, la intro corta del tablero del nivel 7 en la mitad de
    /// cada mundo largo (251 dúo, 361 brincos, 491 y 631 colgado de la cola, 761 desperezo y 871 vuelo) en dos instantes del reposo
    /// (el gesto a un tercio y a dos tercios); <c>f13_&lt;nivel&gt;_primero</c>, el nivel siguiente con el primer gesto en el mismo
    /// instante; y <c>f13_&lt;nivel&gt;_reducir</c>, con Reducir movimiento. Explícito: necesita gráficos
    /// (<c>unity test &lt;repo&gt; --mode PlayMode --architecture x86_64 --filter AF.Tests.PlayMode.TroopSecondGestureSnapshotTests</c>,
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del segundo gesto de los miquitos (F13); necesitan dispositivo gráfico.")]
    public sealed class TroopSecondGestureSnapshotTests
    {
        private static readonly int[] MidIndex = { 0, 1, 2, 3, 4, 5 };

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Capture([ValueSource(nameof(MidIndex))] int index)
        {
            (int world, string worldId, int level, FaunaSecondGesture gesture) = IntroTroopSecondGestureTests.Mids[index];
            string dir = SnapshotKit.Prepare();
            LaunchOptions.FontScaleOverride = 1f;
            yield return IntroTroopSecondGestureTests.Load(world, worldId, level);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            string name = "f13_" + level + "_" + FaunaGestureRule.NameOf(gesture);
            float[] at = Replay(boot, name);
            boot.View!.Intro.Seek(at[0]);
            yield return SnapshotKit.Wait(0.1f);
            SnapshotKit.Save(dir, name + "_a");
            boot.View!.Intro.Seek(at[1]);
            yield return SnapshotKit.Wait(0.1f);
            SnapshotKit.Save(dir, name + "_b");

            boot.Flow!.ReduceMotion = true;
            Replay(boot, name + "_reducir");
            boot.View!.Intro.Seek(400f);
            yield return SnapshotKit.Wait(0.1f);
            SnapshotKit.Save(dir, "f13_" + level + "_reducir");

            yield return IntroTroopSecondGestureTests.Load(world, worldId, level + 1);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(800, false);
            Assert.That(boot.View!.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.None));
            boot.View!.Intro.Seek(at[0]);
            yield return SnapshotKit.Wait(0.1f);
            SnapshotKit.Save(dir, "f13_" + level + "_primero");
        }

        // Vuelve a lanzar la intro corta y da dos instantes del reposo de toda la tropa (a un tercio y a dos tercios).
        private static float[] Replay(GameBootstrap boot, string name)
        {
            GameScreen view = boot.View!;
            view.DebugReplayIntro(800, false);
            IntroChoreography plan = view.Intro.Plan;
            float from = 0f;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                from = Mathf.Max(from, view.Troop.RestFromMs(m) + TroopLayer.RestBlendMs);
            }

            float span = plan.ExitStartMs - TroopLayer.RestBlendMs - from;
            Debug.Log("AF_F13 " + name + ": perch " + plan.Perch + ", " + plan.MonkeyCount + " miquitos, gesto " + view.Troop.SecondShown
                + ", reposo " + from.ToString("0") + "-" + plan.ExitStartMs.ToString("0") + " ms");
            return new[] { from + (span / 3f), from + (2f * span / 3f) };
        }
    }
}
