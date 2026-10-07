#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Cola visual con una dosis por vertido (D-001; 02 §3.2; <c>docs/dev/decisiones.md</c> §2.1.6, criterio A9): un vertido
    /// de una dosis dura 480 ms con los defaults de 08 §9 (07 §5.2: el origen vuelve tras la cola del chorro), el jugador
    /// puede repetir el mismo vertido sin esperar y los vertidos iguales se encadenan sin saltos de fotograma. El reloj lo lleva el test
    /// (<see cref="PlayModeHelpers.Step"/>) para no depender de los fps del Editor.
    /// </summary>
    public sealed class PourChainTests
    {
        // Nivel 2 (02 §13): la cuia 2 muestra una racha de 3 açaí y la 3 está vacía; 2→3 es legal tres veces seguidas.
        private const int Level = 2;
        private const int Source = 2;
        private const int Target = 3;

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator OnePour_MovesOneDose_AndTheOriginIsBackIn480ms()
        {
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            PourTimings timings = boot.View!.Timings;
            // 07 §5.2: chorro desde el 70 % del traslado (140 ms) + 100 ms de dosis + 80 ms de cola, y 160 ms de retorno.
            Assert.That(BoardAnimator.PourDurationMs(timings, 1), Is.EqualTo(480f).Within(0.01f), "0,48 s por vertido");

            Pour(boot);
            int elapsed = PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(elapsed, Is.InRange(480 - PlayModeHelpers.FrameMs, 480 + (2 * PlayModeHelpers.FrameMs)));
            AssertViewMatchesModel(boot);
            Assert.That(boot.View.Views[Target].LiquidLevel, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ThreeIdenticalPours_TappedEvery300ms_AreAtRestWithin1200ms_WithoutSnaps()
        {
            // A9: tres vertidos A→B tocados cada 300 ms dejan la vista en reposo ≤ 1.200 ms después del último toque y sin
            // saltos de fotograma (queue_max_lag_pours = 2 nunca se supera).
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            for (int i = 0; i < 3; i++)
            {
                if (i > 0)
                {
                    PlayModeHelpers.Step(boot, 300);
                }

                Pour(boot);
                Assert.That(boot.View!.Animator.PendingCount, Is.LessThanOrEqualTo(GameConfig.QueueMaxLagPours), "vertido " + i);
            }

            int elapsed = PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(elapsed, Is.LessThanOrEqualTo(1200));
            Assert.That(boot.View!.Animator.LagSnapCount, Is.EqualTo(0));
            Assert.That(boot.Flow!.Session.AppliedPours, Is.EqualTo(3));
            AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator ThreeIdenticalPours_InTheSameFrame_ChainIntoOneTilt_OfThreeDoses()
        {
            // Encadenado (02 §3.2 [P]): el modelo aplica tres vertidos de una dosis y la vista los encadena en una sola
            // inclinación con el caudal alargado 100 ms por dosis, en lugar de tres idas y vueltas.
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            Pour(boot);
            Pour(boot);
            Pour(boot);
            BoardAnimator animator = boot.View!.Animator;
            Assert.That(animator.PendingCount, Is.EqualTo(1));
            Assert.That(animator.ChainedCount, Is.EqualTo(2));

            int expected = (int)BoardAnimator.PourDurationMs(boot.View.Timings, 3);
            int elapsed = PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(elapsed, Is.InRange(expected - PlayModeHelpers.FrameMs, expected + (2 * PlayModeHelpers.FrameMs)));
            Assert.That(animator.LagSnapCount, Is.EqualTo(0));
            AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator PourWhileTheStreamFlows_ExtendsIt_AndPourDuringTheReturn_IsQueued()
        {
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;

            // Con el chorro saliendo (≈ 200 ms tras el toque) el segundo vertido se encadena.
            Pour(boot);
            PlayModeHelpers.Step(boot, 192);
            Pour(boot);
            Assert.That(animator.ChainedCount, Is.EqualTo(1));
            Assert.That(animator.PendingCount, Is.EqualTo(1));

            // Con el caudal cortado (fin en ≈ 340 ms, cola hasta ≈ 420 ms) y el origen ya volviendo, el tercero se encola
            // aparte, sin saltos.
            PlayModeHelpers.Step(boot, 240);
            Pour(boot);
            Assert.That(animator.ChainedCount, Is.EqualTo(1));
            Assert.That(animator.PendingCount, Is.EqualTo(2));
            PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(animator.LagSnapCount, Is.EqualTo(0));
            AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Marea_InTheMiddleOfAChain_ReturnsOneDose_AndTheViewFollowsTheModel()
        {
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            Pour(boot);
            Pour(boot);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(boot.PressUndo(), Is.True); // 04 §2.1: deshace un vertido, una dosis en single
            Assert.That(boot.Flow!.Session.AppliedPours, Is.EqualTo(1));
            PlayModeHelpers.StepUntilIdle(boot, 3000);
            AssertViewMatchesModel(boot);
            Assert.That(boot.View!.Views[Target].LiquidLevel, Is.EqualTo(1f).Within(0.001f));
        }

        private static IEnumerator StartLevel()
        {
            yield return PlayModeHelpers.LoadGame(Level, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True); // salta Sumergir y teñir
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.Flow.Session.PourMode, Is.EqualTo(PourMode.Single));
            Assert.That(boot.Flow.Session.Current[Source].TopRunLength, Is.EqualTo(3));
            PlayModeHelpers.StepUntilIdle(boot, 1000);
        }

        private static void Pour(GameBootstrap boot)
        {
            Assert.That(boot.TapCuia(Source).Kind, Is.EqualTo(TapKind.Selected));
            TapResult r = boot.TapCuia(Target);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(r.Pour.Pour.Amount, Is.EqualTo(1));
        }

        private static void AssertViewMatchesModel(GameBootstrap boot)
        {
            CuiaView[] views = boot.View!.Views;
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].LiquidLevel, Is.EqualTo(boot.Flow!.Session.Current[i].Count).Within(0.001f), "líquido de la cuia " + i);
                Assert.That(views[i].Locked, Is.False, "cuia " + i + " en su ranura");
            }
        }
    }
}
