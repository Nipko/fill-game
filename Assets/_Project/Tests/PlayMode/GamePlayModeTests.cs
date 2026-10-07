#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pruebas de la escena <c>Game</c> real (docs/dev/prototipo-v0.md c): se carga el nivel con el progreso en memoria,
    /// se juega con los mismos puntos de entrada que los toques y se espera a que la vista persiga al modelo.
    /// </summary>
    public sealed class GamePlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        private static Vector2 ScreenCenter(CuiaView view)
        {
            RectTransform body = view.Body;
            Vector3 world = body.TransformPoint(body.rect.center);
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        [UnityTest]
        public IEnumerator Level1_SolverSolution_ReachesVictory_AndNextOpensLevel2()
        {
            var progress = new MemoryProgressStore();
            yield return PlayModeHelpers.LoadGame(1, progress);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.LevelNumber, Is.EqualTo(1));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.View!.Views.Length, Is.EqualTo(3));

            // Un toque durante Sumergir y teñir la salta sin seleccionar (02 §4.2, §14).
            Assert.That(boot.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));

            // Solución del solucionador del núcleo, tocada sin esperar a las animaciones (cola de 02 §3.2).
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.Pipeline);
            Assert.That(solution.IsSolved, Is.True);
            Assert.That(solution.MoveCount, Is.EqualTo(flow.Level.Par));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(progress.NextLevel, Is.EqualTo(2));

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "pantalla de victoria");
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(flow.Level.Par));
            Assert.That(boot.View!.IsViewIdle, Is.True);
            CuiaView[] views = boot.View.Views;
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].Sealed, Is.EqualTo(flow.Session.Current.IsSealed(i)), "sello de la cuia " + i);
                Assert.That(views[i].LiquidLevel, Is.EqualTo(flow.Session.Current[i].Count).Within(0.001f), "líquido de la cuia " + i);
            }

            Assert.That(boot.PressNext(), Is.True);
            Assert.That(flow.LevelNumber, Is.EqualTo(2));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fin de la intro del nivel 2");
        }

        [UnityTest]
        public IEnumerator TouchDownLifts_TouchUpDecides_AndReleasingOutsideCancels()
        {
            // 02 §3.1: la elevación empieza en el touch-down; la selección (y cualquier vertido) se decide en el touch-up.
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            boot.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            CuiaView cuia = boot.View!.Views[0];
            Vector2 inside = ScreenCenter(cuia);

            var down = new PointerEventData(EventSystem.current) { position = inside };
            ExecuteEvents.Execute(cuia.gameObject, down, ExecuteEvents.pointerDownHandler);
            Assert.That(cuia.IsLifted, Is.True, "sube en el touch-down");
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection), "aún sin decidir");

            ExecuteEvents.Execute(cuia.gameObject, new PointerEventData(EventSystem.current) { position = inside }, ExecuteEvents.pointerUpHandler);
            Assert.That(flow.Selected, Is.EqualTo(0), "el touch-up dentro selecciona (C1)");
            Assert.That(cuia.IsLifted, Is.True);

            // Toque en el destino: el vertido también se decide al soltar.
            CuiaView target = boot.View.Views[2];
            Vector2 targetPoint = ScreenCenter(target);
            ExecuteEvents.Execute(target.gameObject, new PointerEventData(EventSystem.current) { position = targetPoint }, ExecuteEvents.pointerDownHandler);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0));
            ExecuteEvents.Execute(target.gameObject, new PointerEventData(EventSystem.current) { position = targetPoint }, ExecuteEvents.pointerUpHandler);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(1));

            // Dedo apoyado por error y soltado fuera: no selecciona y la cuia vuelve a bajar.
            yield return PlayModeHelpers.WaitFor(() => boot.View.IsViewIdle, "fin del vertido");
            CuiaView other = boot.View.Views[1];
            ExecuteEvents.Execute(other.gameObject, new PointerEventData(EventSystem.current) { position = ScreenCenter(other) }, ExecuteEvents.pointerDownHandler);
            Assert.That(other.IsLifted, Is.True);
            ExecuteEvents.Execute(other.gameObject, new PointerEventData(EventSystem.current) { position = new Vector2(-50f, -50f) }, ExecuteEvents.pointerUpHandler);
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(other.IsLifted, Is.False);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level6_StuckPath_OpensTheModal_AndMareaRecovers()
        {
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fin de la intro");

            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            float stuckAt = Time.realtimeSinceStartup;

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal Aguas estancadas");
            Assert.That(Time.realtimeSinceStartup - stuckAt, Is.GreaterThanOrEqualTo(GameConfig.StuckModalDelayMs / 1000f * 0.9f)); // R15

            Assert.That(boot.PressUndo(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0));
            yield return PlayModeHelpers.WaitFor(() => boot.View!.IsViewIdle, "cola visual tras Marea");
            Assert.That(boot.View!.Views[4].LiquidLevel, Is.EqualTo(0f).Within(0.001f));

            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.Session.Current.Equals(flow.Session.Initial), Is.True);
        }
    }
}
