#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Rótulo del lugar en la escena real (tarea M2.3; D-013, punto 8; propuesta del Mapa §10.5, punto 5; reglas de los avisos de
    /// D-011): en el 31 no sale durante la intro, entra en la cola al habilitarse el toque y sale cuando la tropa se va, sobre el
    /// horizonte (2 filas), con «Floresta Nacional do Tapajós · Brasil», durante <see cref="NoticeTiming.ReadMs(string)"/> (3.035
    /// ms), sin capturar toques, apartando las etiquetas del HUD y con su sonda <c>AF_STATE stop_label</c>; un aviso que llega con
    /// el rótulo a la vista espera a que se vaya, y el rótulo espera al aviso que se ve; un modal del embudo lo oculta y en el
    /// reintento no vuelve; al reanudar no sale; en el 131 (3 filas) va como placa bajo el HUD, por encima de las cuias de la
    /// fila 1; en el 32 y con <c>ui_stop_label</c> apagada no sale; y sin asignaciones por fotograma con el rótulo a la vista.
    /// </summary>
    public sealed class StopLabelTests
    {
        private const string Label31 = "Floresta Nacional do Tapajós · Brasil";

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.StopLabelsOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.StopLabelsOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level31_AfterTheIntro_ShowsOverTheHorizon_ForItsReadingTime()
        {
            yield return Load(31);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            StopLabel label = view.StopLabelView;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            int t = 0;
            while (flow.Phase == FlowPhase.Intro && t < 6000)
            {
                Assert.That(label.Visible, Is.False, "nunca durante la intro (t = " + t + " ms)");
                Assert.That(view.NoticesQueued, Is.EqualTo(0), "entra en la cola al habilitarse el toque, no antes");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            StepUntilShown(boot);
            Assert.That(label.Text, Is.EqualTo(Label31));
            Assert.That(label.OnPlate, Is.False, "8 cuias en 2 filas: sobre el horizonte");
            Assert.That(view.Layout!.Rows, Is.EqualTo(2));
            Assert.That(label.DurationMs, Is.EqualTo(3035f), "1.000 + 55 × 37 caracteres (D-011)");
            Assert.That(label.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(Label31)));

            // Sobre el horizonte: el borde de arriba 28 dp por encima (o bajo la fila del HUD), centrado, sin bajar al panel.
            Rect r = label.Rect;
            Assert.That(r.yMin, Is.EqualTo(Mathf.Max(view.HorizonY - StopLabel.AboveHorizonDp, view.HudBottomDp + StopLabel.HudGapDp)).Within(0.01f));
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp), "bajo la fila del HUD");
            Assert.That(r.yMax, Is.LessThanOrEqualTo(view.Layout.PanelY + 0.01f), "no baja al panel");
            Assert.That(r.center.x, Is.EqualTo(view.CanvasWidth * 0.5f).Within(0.01f));
            Assert.That(label.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));

            // No captura toques y aparta las etiquetas del HUD mientras se ve.
            Assert.That(label.BlocksRaycasts, Is.False);
            var graphics = new List<Graphic>();
            label.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }

            PlayModeHelpers.Step(boot, 400);
            Assert.That(label.Alpha, Is.EqualTo(1f), "tras la entrada de 150 ms");
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(0f), "las etiquetas del HUD se apartan");
            int source = FirstNonEmpty(flow.Session.Current);
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), "se juega con el rótulo a la vista");
            boot.TapCuia(source);

            // La sonda.
            string? probe = view.LastStopLabelProbe;
            Assert.That(probe, Is.Not.Null);
            StringAssert.Contains("\"level\":31", probe);
            StringAssert.Contains("\"stop\":3", probe);
            StringAssert.Contains("\"stop_id\":\"s03\"", probe);
            StringAssert.Contains("\"id\":\"flona_tapajos\"", probe);
            StringAssert.Contains("\"text\":\"" + Label31 + "\"", probe);
            StringAssert.Contains("\"ms\":3035", probe);
            StringAssert.Contains("\"mode\":\"horizon\"", probe);

            // Se va a los 3.035 ms y no vuelve.
            for (int k = 0; k < 4000 && label.Visible; k += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(label.Visible, Is.False);
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(1f), "las etiquetas vuelven");
            PlayModeHelpers.Step(boot, 4000);
            Assert.That(label.Visible, Is.False, "una sola vez");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Queue_OneAtATime_WithAnotherNotice()
        {
            yield return Load(31);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopLabel label = view.StopLabelView;
            boot.TapAnywhere();
            StepUntilShown(boot);

            // Un aviso que llega con el rótulo a la vista espera a que se vaya y sale en el fotograma en que se oculta.
            view.ShowNotice(UiText.RescueApplied);
            Assert.That(view.ToastText, Is.Null, "no pisa el rótulo");
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            for (int t = 0; t < 4000 && label.Visible; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.ToastText, Is.Null);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(label.Visible, Is.False);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), "en el mismo fotograma");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));

            // Y al revés: con un aviso a la vista al habilitarse el toque, el rótulo espera su turno.
            yield return Load(31);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            label = view.StopLabelView;
            view.ShowNotice(UiText.RescueApplied); // llega durante la intro: espera al toque, delante del rótulo
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            boot.TapAnywhere();
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), "el que llegó antes sale antes");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el rótulo espera detrás");
            Assert.That(label.Visible, Is.False);
            for (int t = 0; t < 8000 && view.ToastText != null; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(label.Visible, Is.False);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(label.Visible, Is.True, "sale al irse el aviso");
            Assert.That(label.Text, Is.EqualTo(Label31));
        }

        [UnityTest]
        public IEnumerator FunnelModal_HidesIt_AndTheRetryDoesNotBringItBack()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            StopLabel label = view.StopLabelView;
            boot.TapAnywhere();
            StepUntilShown(boot);

            FunnelSetup.GetStuck(boot);
            Assert.That(label.Visible, Is.False, "Aguas estancadas lo oculta en el acto (pregunta 42 (b))");
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.ConfirmGiveUp(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.PressRetry(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            for (int t = 0; t < 6000 && (boot.Flow.Phase != FlowPhase.Playing || t < 5000); t += PlayModeHelpers.FrameMs)
            {
                Assert.That(label.Visible, Is.False, "no sale en el reintento (t = " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.GreaterThan(0));
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Resume_DoesNotShowIt()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            StepUntilShown(boot);
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            boot.TapCuia(solution.GetMove(0).Source);
            boot.TapCuia(solution.GetMove(0).Target);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(ProfileJson.Deserialize(store.Json!).LevelState, Is.Not.Null, "el nivel en curso quedó guardado");

            yield return PlayModeHelpers.LoadApp(store); // como cerrar y abrir la app
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            PlayModeHelpers.TakeClock(again);
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(31));
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "reanudado, sin intro");
            for (int t = 0; t < 5000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(again.View!.StopLabelView.Visible, Is.False, "al reanudar no sale (t = " + t + " ms)");
                PlayModeHelpers.Step(again, PlayModeHelpers.FrameMs);
            }

            Assert.That(again.View!.NoticesQueued, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Level131_ThreeRows_APlateUnderTheHud_AboveTheFirstRow()
        {
            yield return Load(131);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopLabel label = view.StopLabelView;
            boot.TapAnywhere();
            StepUntilShown(boot);
            BoardLayoutResult l = view.Layout!;
            Assert.That(l.Rows, Is.EqualTo(3), "11 cuias");
            Assert.That(label.OnPlate, Is.True);
            Assert.That(label.Text, Is.EqualTo("El río frontera · Colombia y Perú"));
            Rect r = label.Rect;
            Assert.That(r.yMin, Is.EqualTo(view.HudBottomDp + StopLabel.HudGapDp).Within(0.01f), "bajo el HUD");
            Assert.That(r.yMax, Is.LessThanOrEqualTo(l.BlockTop + BoardLayout.Lift - StopLabel.CuiaGapDp + 0.01f), "sin tapar las cuias de la fila 1 en reposo");
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(StopLabel.SideDp - 0.01f));
            Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - StopLabel.SideDp + 0.01f));
            Assert.That(r.center.x, Is.EqualTo(view.CanvasWidth * 0.5f).Within(0.01f));
            Assert.That(label.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), "nunca por debajo de 12 sp");
            Assert.That(label.Label.isTextOverflowing, Is.False);
            StringAssert.Contains("\"mode\":\"plate\"", view.LastStopLabelProbe);
            StringAssert.Contains("\"rows\":3", view.LastStopLabelProbe);
        }

        [UnityTest]
        public IEnumerator NotOnOtherLevels_NorWithTheKeyOff()
        {
            yield return Load(32);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(boot.View!.StopLabelView.Visible, Is.False, "32: no es el primer nivel de la parada");
            Assert.That(boot.View.NoticesQueued, Is.EqualTo(0));

            GameScreen.StopLabelsOverride = false;
            yield return Load(31);
            boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(boot.View!.StopLabelView.Visible, Is.False, "ui_stop_label apagada");
            Assert.That(boot.View.NoticesQueued, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator NoAllocationsPerFrame_WithTheLabelShown()
        {
            yield return Load(31);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            StepUntilShown(boot);
            view.ShowNotice(UiText.RescueApplied);
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            // Ya en régimen: los ~2,2 s que siguen al salto de la intro asignan memoria también sin ningún aviso (NoticeTests).
            // El rótulo del 31 dura 3.035 ms: a los 2.300, quedan 735 para los 30 fotogramas medidos (480 ms).
            PlayModeHelpers.TickExact(boot, 2300);
            Assert.That(view.StopLabelView.Visible, Is.True);
            Assert.That(() =>
            {
                for (int f = 0; f < 30; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                }
            }, Is.Not.AllocatingGCMemory(), "30 fotogramas con el rótulo a la vista y un aviso en la cola");
            Assert.That(view.StopLabelView.Visible, Is.True, "el rótulo siguió a la vista durante la medida");
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
        }

        private static IEnumerator Load(int level)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }

        // Hasta que el rótulo esté a la vista: tras la intro y cuando la tropa se ha ido de la rama.
        private static void StepUntilShown(GameBootstrap boot)
        {
            for (int t = 0; t < 8000 && !boot.View!.StopLabelView.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.View!.StopLabelView.Visible, Is.True, "el rótulo sale tras la intro");
            Assert.That(boot.View.IntroTroopVisible, Is.False, "cuando la tropa se ha ido");
        }

        private static int FirstNonEmpty(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (!board[i].IsEmpty)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
