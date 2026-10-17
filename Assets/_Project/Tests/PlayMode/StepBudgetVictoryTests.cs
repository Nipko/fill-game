#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Arreglo FX6 (observación 2 de la prueba de la ola 4 en el teléfono, 2026-10-16): el aviso <c>step_budget</c> en la
    /// victoria del 280 (Piraña y Brote, 13 cuias). Al ritmo del guion del teléfono (un vertido cada 0,5 s con la solución
    /// A*), la solución acaba con cuatro vertidos encadenados por sus cuias (1 → 12, 1 → 10, 0 → 1 y 0 → 6), y el penúltimo,
    /// 0 → 6, sella la 6 con el contador de la Piraña (Seal y BiteLost, 850 ms de pasos): encolado detrás de los otros, llegaba
    /// a su caudal 2.630 ms después de su toque, y el presupuesto, que contaba desde ese toque, saltaba su etapa (e) aunque el
    /// jugador ya había tocado el vertido que gana 500 ms después, y la celebración no corta antes de 3.000 ms desde ese toque.
    /// El presupuesto cuenta ahora desde el último vertido tocado (<see cref="BoardAnimator.SinceLastPourMs"/>): el 280 se gana
    /// sin <c>step_budget</c> y cada paso que no saltó por la cola (<c>queue_max_lag_pours</c>) acaba dentro de los 2.800 ms
    /// desde el último toque.
    /// </summary>
    public sealed class StepBudgetVictoryTests
    {
        /// <summary>Ritmo del guion de la prueba en el teléfono: un vertido (origen y destino) cada 0,5 s.</summary>
        private const int ScriptPaceMs = 500;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level280_WonAtTheScriptPace_WithoutStepBudget()
        {
            yield return Play280(ScriptPaceMs);
        }

        [UnityTest]
        public IEnumerator Level280_WonWaitingForTheView_WithoutStepBudget()
        {
            yield return Play280(0);
        }

        // Gana el 280 con la solución A*: con paceMs > 0, un vertido cada paceMs ms (el último espera a la vista); con 0, cada
        // vertido espera a que la vista quede quieta. Falla si sale step_budget.
        private static IEnumerator Play280(int paceMs)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(280, new MemoryProgressStore(280, 279));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            boot.TapAnywhere(); // salta la intro
            for (int t = 0; t < 10000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            BoardAnimator animator = view.Animator;
            MechanicStepQueue steps = animator.Steps;
            int budgetEvents = 0;
            string? budgetFields = null;
            System.Action<string, string>? probe = animator.DevEvent;
            animator.DevEvent = (evt, fields) =>
            {
                if (evt == "step_budget")
                {
                    budgetEvents++;
                    budgetFields = fields;
                }

                probe?.Invoke(evt, fields);
            };

            try
            {
                SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
                Assert.That(solution.IsSolved, Is.True);
                int overruns = steps.BudgetOverruns;
                steps.ClearLog();
                float lastTap = 0f;
                int drainMs = 0;
                for (int i = 0; i < solution.MoveCount; i++)
                {
                    Pour move = solution.GetMove(i);
                    Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected), "vertido " + i + ": origen");
                    Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured), "vertido " + i + ": destino");
                    lastTap = animator.Clock;
                    if (paceMs == 0 || i == solution.MoveCount - 1)
                    {
                        drainMs = PlayModeHelpers.StepUntilIdle(boot, 8000);
                    }
                    else
                    {
                        PlayModeHelpers.Step(boot, paceMs);
                    }
                }

                TestContext.WriteLine("280 a " + (paceMs == 0 ? "vista quieta" : paceMs + " ms por vertido") + ": " + solution.MoveCount
                    + " vertidos, vista quieta " + drainMs + " ms después del último toque (fase " + flow.Phase + "), " + steps.BudgetScaled + " planes acelerados, " + animator.LagSnapCount + " saltos por la cola. "
                    + MechanicTestKit.Describe(animator));
                Assert.That(budgetEvents, Is.EqualTo(0), "sin step_budget en la victoria del 280: " + budgetFields);
                Assert.That(steps.BudgetOverruns, Is.EqualTo(overruns), "ninguna etapa salta por el presupuesto");
                for (int k = 0; k < steps.LogCount; k++)
                {
                    MechanicStepRecord r = steps.LogAt(k);
                    if (!r.Snapped)
                    {
                        Assert.That(r.EndMs, Is.LessThanOrEqualTo(lastTap + MechanicStepQueue.BudgetFromTapMs + 0.5f), "dentro del presupuesto desde el último toque: " + r);
                    }
                }

                PlayModeHelpers.Step(boot, 3000);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen).Or.EqualTo(FlowPhase.Celebration), "ganado");
            }
            finally
            {
                animator.DevEvent = probe;
            }
        }
    }
}
