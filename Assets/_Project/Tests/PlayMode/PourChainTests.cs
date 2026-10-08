#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Cola visual en el modo single de pruebas, forzado en <see cref="SetUp"/>: el encadenado de 02 §3.2 solo existe en
    /// single, porque en run (el modo por defecto, D-005) dos vertidos A→B seguidos nunca son legales (o el origen cambia de
    /// cima o el destino queda lleno). <c>docs/dev/decisiones.md</c> §2.1.6, criterio A9: un vertido de una dosis dura
    /// 870 ms con los defaults de 08 §9 tras D-006 (07 §5.2: el origen vuelve tras la cola del chorro), el jugador puede
    /// repetir el mismo vertido sin esperar y los vertidos iguales se encadenan sin saltos de fotograma, con la inclinación
    /// de Magic Sort por cada dosis añadida (92° − 9° × dosis que quedan). El reloj lo lleva el test
    /// (<see cref="PlayModeHelpers.Step"/> y <see cref="PlayModeHelpers.TickExact"/>) para no depender de los fps del Editor.
    /// </summary>
    public sealed class PourChainTests
    {
        // Nivel 2 (02 §13): la cuia 2 muestra una racha de 3 açaí y la 3 está vacía; 2→3 es legal tres veces seguidas.
        private const int Level = 2;
        private const int Source = 2;
        private const int Target = 3;

        [SetUp]
        public void SetUp()
        {
            // D-005: single queda solo para pruebas; aquí hace falta para repetir el mismo vertido A→B.
            LaunchOptions.PourModeOverride = PourModes.SingleKey;
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator OnePour_MovesOneDose_AndTheOriginIsBackIn870ms()
        {
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            PourTimings timings = boot.View!.Timings;
            // 07 §5.2 con los tiempos de D-006: chorro desde el 70 % del traslado (210 ms) + 280 ms de dosis + 80 ms de cola,
            // y 300 ms de retorno (Magic Sort: 0,9-1,0 s de la salida al reposo).
            Assert.That(BoardAnimator.PourDurationMs(timings, 1), Is.EqualTo(870f).Within(0.01f), "0,87 s por vertido");

            Pour(boot);
            int elapsed = PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(elapsed, Is.InRange(870 - PlayModeHelpers.FrameMs, 870 + (2 * PlayModeHelpers.FrameMs)));
            AssertViewMatchesModel(boot);
            Assert.That(boot.View.Views[Target].LiquidLevel, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ThreeIdenticalPours_TappedEvery300ms_AreAtRestWithin1200ms_WithoutSnaps()
        {
            // A9: tres vertidos A→B tocados cada 300 ms dejan la vista en reposo ≤ 1.200 ms después del último toque y sin
            // saltos de fotograma (queue_max_lag_pours = 2 nunca se supera). Con los tiempos de D-006 se encadenan: el caudal
            // termina en 210 + 3 × 280 = 1.050 ms, la cola en 1.130 y el origen vuelve en 1.430 ms desde el primer toque,
            // 830 ms tras el último. El origen (4 dosis) se inclina a 65°, 74° y 83° al terminar cada dosis (D-006).
            yield return StartLevel();
            GameBootstrap boot = GameBootstrap.Current!;
            PourTimings timings = boot.View!.Timings;
            int streamStart = Mathf.RoundToInt(timings.TravelMs * BoardAnimator.StreamStartFraction);
            int dose = Mathf.RoundToInt(timings.DoseMs);
            CuiaView source = boot.View.Views[Source];
            int elapsed0 = 0;
            int tapped = 0;
            int nextDose = 1;
            float[] tilts = { 65f, 74f, 83f };
            while (nextDose <= 3)
            {
                if (tapped < 3 && elapsed0 >= tapped * 300)
                {
                    Pour(boot);
                    Assert.That(boot.View!.Animator.PendingCount, Is.LessThanOrEqualTo(GameConfig.QueueMaxLagPours), "vertido " + tapped);
                    tapped++;
                }

                int doseEnd = streamStart + (nextDose * dose);
                int nextTap = tapped < 3 ? tapped * 300 : int.MaxValue;
                int next = Mathf.Min(elapsed0 + PlayModeHelpers.FrameMs, Mathf.Min(doseEnd, nextTap));
                PlayModeHelpers.TickExact(boot, next - elapsed0);
                elapsed0 = next;
                if (elapsed0 == doseEnd)
                {
                    Assert.That(Mathf.Abs(source.BodyAngle), Is.EqualTo(tilts[nextDose - 1]).Within(1f), "inclinación al terminar la dosis " + nextDose);
                    nextDose++;
                }
            }

            Assert.That(tapped, Is.EqualTo(3));
            Assert.That(boot.View!.Animator.ChainedCount, Is.EqualTo(2), "los tres vertidos van en una sola inclinación");
            int elapsed = (elapsed0 - 600) + PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(elapsed, Is.LessThanOrEqualTo(1200));
            Assert.That(boot.View!.Animator.LagSnapCount, Is.EqualTo(0));
            Assert.That(boot.Flow!.Session.AppliedPours, Is.EqualTo(3));
            AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator ThreeIdenticalPours_InTheSameFrame_ChainIntoOneTilt_OfThreeDoses()
        {
            // Encadenado (02 §3.2 [P]): el modelo aplica tres vertidos de una dosis y la vista los encadena en una sola
            // inclinación con el caudal alargado anim_pour_dose_ms por dosis, en lugar de tres idas y vueltas.
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

            // Con el trabajo en marcha y el caudal sin cortar (≈ 200 ms tras el toque) el segundo vertido se encadena.
            PourTimings timings = boot.View.Timings;
            Pour(boot);
            PlayModeHelpers.TickExact(boot, 192);
            Pour(boot);
            Assert.That(animator.ChainedCount, Is.EqualTo(1));
            Assert.That(animator.PendingCount, Is.EqualTo(1));

            // Con el caudal cortado (fin en 0,7 · viaje + 2 dosis, cola 80 ms después) y el origen ya volviendo, el tercero se
            // encola aparte, sin saltos.
            int returning = Mathf.RoundToInt((timings.TravelMs * BoardAnimator.StreamStartFraction) + (2f * timings.DoseMs) + timings.TailMs) + 50;
            Assert.That(returning, Is.LessThan(BoardAnimator.PourDurationMs(timings, 2)), "aún volviendo");
            PlayModeHelpers.TickExact(boot, returning - 192);
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
