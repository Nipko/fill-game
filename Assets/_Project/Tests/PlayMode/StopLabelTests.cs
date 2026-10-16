#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Core.Board;
using AF.Core.Session;
using AF.Economy;
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
    /// reintento no vuelve, ni tras Volver a la orilla (visto o aún en la cola) ni al volver al 31 desde el inicio sin fallos; al
    /// reanudar no sale; en el 131 (3 filas) va como placa bajo el HUD, con aire sobre las cuias de la
    /// fila 1; en el 32 y con <c>ui_stop_label</c> apagada no sale; y sin asignaciones por fotograma con el rótulo a la vista.
    /// De FX4 y su revisión: la placa y la fila 1 (clavada al marco, de lado o sobre el muelle), la elevación, la burbuja, el
    /// «no sale» y su sonda <c>stop_label_off</c>.
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
            GameScreen.StopLabelSpotsOverride = GameScreen.StopLabelSpot.All;
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
        public IEnumerator Restart_SeenOrStillQueued_DoesNotBringItBack()
        {
            // Volver a la orilla con el rótulo a la vista: lo oculta y el intento nuevo no lo repite.
            yield return Load(31);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopLabel label = view.StopLabelView;
            boot.TapAnywhere();
            StepUntilShown(boot);
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(label.Visible, Is.False, "otro intento: los avisos del anterior ya no valen");
            StepWithoutIt(boot, "tras Reiniciar con el rótulo a la vista");
            Assert.That(boot.Flow!.Attempt.FailsLevel, Is.EqualTo(0), "sin vertidos, Reiniciar no cuenta como fallo");

            // Y otra vez con el rótulo ya ido: tampoco vuelve.
            Assert.That(boot.PressRestart(), Is.True);
            StepWithoutIt(boot, "tras Reiniciar con el rótulo ya visto");

            // Volver a la orilla antes de que salga (en la cola, detrás de un aviso): ya entró en esta visita y no sale.
            yield return Load(31);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            label = view.StopLabelView;
            view.ShowNotice(UiText.RescueApplied); // durante la intro: sale antes que el rótulo al habilitarse el toque
            boot.TapAnywhere();
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied));
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el rótulo espera detrás del aviso");
            Assert.That(label.Visible, Is.False);
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(view.NoticesQueued, Is.EqualTo(0), "Reiniciar vacía la cola");
            StepWithoutIt(boot, "tras Reiniciar con el rótulo en la cola");
            Assert.That(boot.Flow!.Attempt.FailsLevel, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator BackToTheLobby_AndAgainLevel31_DoesNotRepeatIt()
        {
            yield return Load(31);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            StepUntilShown(boot);
            Assert.That(boot.App!.ExitToLobby(), Is.True, "salir al inicio sin fallo");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.PlayLevel, Is.EqualTo(31));

            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(31));
            Assert.That(boot.Flow.Attempt.FailsLevel, Is.EqualTo(0), "sin fallos: lo evita la visita, no el fallo");
            Assert.That(boot.View, Is.SameAs(view), "la misma pantalla de juego");
            Assert.That(view.StopLabelView.Visible, Is.False, "al cargar el nivel no queda ninguno del anterior");
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "intro completa, no la corta del reintento");
            boot.TapAnywhere();
            StepWithoutIt(boot, "al volver al 31 desde el inicio");
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
            float frameInner = l.PanelY + view.PanelFrameDp; // canto interior de la barra de arriba del marco (sin desplazar)
            Assert.That(r.yMin, Is.EqualTo(Mathf.Max(view.HudBottomDp, frameInner - StopLabel.PlateOverFrameDp)).Within(0.01f), "clavada al marco");
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.01f), "bajo la fila del HUD");
            Assert.That(r.yMin, Is.LessThan(frameInner), "monta sobre el canto interior del marco (ni tangente ni con hueco)");
            Assert.That(r.yMax, Is.LessThanOrEqualTo(l.BlockTop + BoardLayout.Lift - StopLabel.PlateCuiaGapDp + 0.01f), "al menos 6 dp sobre las cuias de la fila 1 en reposo");
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(StopLabel.SideDp - 0.01f));
            Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - StopLabel.SideDp + 0.01f));
            Assert.That(r.center.x, Is.EqualTo(view.CanvasWidth * 0.5f).Within(0.01f));
            Assert.That(label.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), "nunca por debajo de 12 sp");
            Assert.That(label.Label.fontSize, Is.LessThanOrEqualTo(StopLabel.PlateTextSp + 0.01f));
            Assert.That(label.Label.isTextOverflowing, Is.False);
            Assert.That(r.height, Is.EqualTo(Mathf.Ceil((label.Label.fontSize * StopLabel.PlateHeightEm) - 0.01f)).Within(0.01f), "una línea");
            StringAssert.Contains("\"mode\":\"plate\"", view.LastStopLabelProbe);
            StringAssert.Contains("\"rows\":3", view.LastStopLabelProbe);
        }

        // FX4 (observación 3 de la prueba de la ola 2 en el teléfono): con la placa a la vista nada de la fila 1 queda bajo ella,
        // ni elevado. En el 131 y el 191 va clavada al marco sobre la fila 1 en reposo y, al elevar la cuia 0, se oculta (no cabe
        // sobre la cuia elevada ni a 12 sp); en el 171, la Hoja de la cuia 1 asoma sobre la boca y no deja sitio sobre toda la
        // fila: va a la misma altura, de lado, sobre las cuias 2 y 3 (revisión de FX4), sigue a la vista con la cuia 0 elevada
        // (no está bajo ella) y se oculta al elevar una cuia que sí lo está. Los rectángulos se miden con los gráficos que se ven.
        [UnityTest]
        public IEnumerator Level131_ThePlateClearsTheFirstRow_AndHidesWhenCuia0Lifts()
        {
            yield return PlateAndFirstRow(131, "El río frontera · Colombia y Perú", false);
        }

        [UnityTest]
        public IEnumerator Level171_TheLeafEmblemLeavesNoRoomAcross_SoThePlateGoesBeside()
        {
            yield return PlateAndFirstRow(171, "Nauta · Perú", true);
        }

        [UnityTest]
        public IEnumerator Level191_ThePlateClearsTheFirstRow_AndHidesWhenCuia0Lifts()
        {
            yield return PlateAndFirstRow(191, "Pacaya-Samiria al alba · Perú", false);
        }

        private static IEnumerator PlateAndFirstRow(int level, string text, bool side)
        {
            var probes = new List<string>();
            Application.LogCallback capture = CaptureProbes(probes, "stop_label_off");
            Application.logMessageReceived += capture;
            try
            {
                yield return Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                StopLabel label = view.StopLabelView;
                boot.TapAnywhere();
                StepUntilShown(boot);
                PlayModeHelpers.Step(boot, 400);
                BoardLayoutResult l = view.Layout!;
                string mode = side ? "plate_side" : "plate";
                Assert.That(l.Rows, Is.EqualTo(3), level + ": 11 cuias");
                Assert.That(label.OnPlate, Is.True);
                Assert.That(label.Text, Is.EqualTo(text));
                Assert.That(view.StopLabelMode, Is.EqualTo(mode), level + ": sitio de la placa");
                Assert.That(view.StopLabelOnDock, Is.False, level + ": no va sobre el muelle");
                StringAssert.Contains("\"mode\":\"" + mode + "\"", view.LastStopLabelProbe);
                Assert.That(label.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), "nunca por debajo de 12 sp");
                Assert.That(label.Label.isTextOverflowing, Is.False);
                Assert.That(label.Rect.height, Is.EqualTo(Mathf.Ceil((label.Label.fontSize * StopLabel.PlateHeightEm) - 0.01f)).Within(0.01f), "una línea");

                RectTransform canvas = CanvasOf(label);
                Rect plate = label.Rect;
                float frameInner = l.PanelY + view.PanelFrameDp;
                Assert.That(plate.yMin, Is.EqualTo(Mathf.Max(view.HudBottomDp, frameInner - StopLabel.PlateOverFrameDp)).Within(0.01f), level + ": clavada al marco");
                Assert.That(plate.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.01f), "bajo la fila del HUD");
                Assert.That(plate.xMin, Is.GreaterThanOrEqualTo(StopLabel.SideDp - 0.01f));
                Assert.That(plate.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - StopLabel.SideDp + 0.01f));
                int beside = 0;
                int under = -1;
                for (int i = 0; i < view.Views.Length; i++)
                {
                    if (l.RowOf[i] != 0)
                    {
                        continue;
                    }

                    Rect box = VisibleBox(view.Views[i], canvas);
                    Assert.That(view.StopLabelRestTopOf(i), Is.EqualTo(box.yMin).Within(0.05f), level + ": lo más alto de la cuia " + i + " que mide la pantalla");
                    Vector2 span = view.StopLabelRestSpanOf(i);
                    Assert.That(span.x, Is.EqualTo(box.xMin).Within(0.05f), level + ": lo que ocupa a lo ancho la cuia " + i);
                    Assert.That(span.y, Is.EqualTo(box.xMax).Within(0.05f), level + ": lo que ocupa a lo ancho la cuia " + i);
                    bool below = box.xMax + StopLabel.PlateSideAirDp > plate.xMin && box.xMin - StopLabel.PlateSideAirDp < plate.xMax;
                    TestContext.WriteLine(level + ": cuia " + i + " " + box + (below ? " bajo la placa" : " al lado"));
                    if (below)
                    {
                        Assert.That(plate.yMax + StopLabel.PlateCuiaGapDp, Is.LessThanOrEqualTo(box.yMin + 0.01f), level + ": aire sobre la cuia " + i + " en reposo, corcho y mecánica incluidos");
                        if (under < 0 && !boot.Flow!.Session.Current[i].IsEmpty)
                        {
                            under = i;
                        }
                    }
                    else if (box.yMin < plate.yMax + StopLabel.PlateCuiaGapDp)
                    {
                        beside++; // no le deja sitio encima: queda al lado, con su aire a los lados
                    }
                }

                TestContext.WriteLine(level + ": placa " + plate + " (" + mode + ")");
                if (side)
                {
                    Assert.That(beside, Is.GreaterThan(0), level + ": alguna cuia de la fila 1 no le deja sitio encima (la Hoja)");
                    Rect leaf = VisibleBox(view.Views[1], canvas);
                    Assert.That(plate.xMin - leaf.xMax, Is.GreaterThanOrEqualTo(StopLabel.PlateSideAirDp - 0.01f), "a la derecha de la Hoja, con su aire");
                }
                else
                {
                    Assert.That(beside, Is.EqualTo(0), level + ": cabe sobre toda la fila 1");
                    Assert.That(plate.center.x, Is.EqualTo(view.CanvasWidth * 0.5f).Within(0.01f), "centrada");
                }

                // La cuia 0 elevada.
                Assert.That(label.Visible, Is.True);
                Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected), level + ": la cuia 0 se selecciona con la placa a la vista");
                PlayModeHelpers.Step(boot, 300);
                Assert.That(view.Views[0].IsLifted, Is.True);
                Rect lifted = VisibleBox(view.Views[0], canvas);
                Assert.That(lifted.yMin, Is.LessThan(view.StopLabelRestTopOf(0) - BoardLayout.Lift + 0.5f), "elevada");
                if (side)
                {
                    Assert.That(lifted.xMax + StopLabel.PlateSideAirDp, Is.LessThanOrEqualTo(plate.xMin), "la cuia 0 no está bajo la placa");
                    Assert.That(label.Visible, Is.True, "de lado, la cuia 0 elevada no la toca");
                    Assert.That(probes.Count, Is.EqualTo(0), string.Join("\n", probes));
                    boot.TapCuia(0);
                    PlayModeHelpers.Step(boot, 300);
                    Assert.That(under, Is.GreaterThan(0), "alguna cuia con fruta bajo la placa");
                    Assert.That(boot.TapCuia(under).Kind, Is.EqualTo(TapKind.Selected));
                    PlayModeHelpers.Step(boot, 300);
                    Assert.That(VisibleBox(view.Views[under], canvas).yMin, Is.LessThan(plate.yMax + StopLabel.PlateLiftAirDp), "la cuia " + under + " elevada no cabe bajo la placa");
                }
                else
                {
                    Assert.That(lifted.yMin, Is.LessThan(plate.yMax + StopLabel.PlateLiftAirDp), "la cuia elevada no cabe bajo la placa");
                }

                Assert.That(label.Visible, Is.False, "se oculta al elevar una cuia de la fila 1 bajo ella");
                Assert.That(probes.Count, Is.EqualTo(1), string.Join("\n", probes));
                StringAssert.Contains("\"level\":" + level + ",\"reason\":\"lift\"", probes[0]);
                boot.TapCuia(side ? under : 0); // se baja: no vuelve
                PlayModeHelpers.Step(boot, 3000);
                Assert.That(label.Visible, Is.False, "descartada: no vuelve");
                Assert.That(view.NoticesQueued, Is.EqualTo(0));
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }

        // Revisión de FX4, (b): una cuia de la fila 1 bajo la placa ya elevada cuando le llega el turno al rótulo (espera detrás
        // de un aviso que llegó durante la intro, y mientras tanto se eleva la cuia 0). Sale (con su sonda stop_label) y se
        // oculta en el mismo fotograma (stop_label_off, lift), sin llegar a verse.
        [UnityTest]
        public IEnumerator Level131_ACuiaAlreadyLiftedWhenItsTurnComes_HidesItAtOnce()
        {
            var probes = new List<string>();
            Application.LogCallback capture = CaptureProbes(probes, "stop_label");
            Application.logMessageReceived += capture;
            try
            {
                yield return Load(131);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                StopLabel label = view.StopLabelView;
                view.ShowNotice(UiText.RescueApplied); // durante la intro: sale antes que el rótulo al habilitarse el toque
                boot.TapAnywhere();
                for (int t = 0; t < 6000 && boot.Flow!.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
                Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), "condición del test: el aviso, delante");
                Assert.That(view.NoticesQueued, Is.EqualTo(1), "condición del test: el rótulo espera detrás");
                Assert.That(label.Visible, Is.False, "condición del test: aún no ha salido");
                Assert.That(probes.Count, Is.EqualTo(0), "condición del test: aún no ha salido");
                Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected), "la cuia 0 (bajo la placa en el 131) se eleva antes");
                for (int t = 0; t < 8000 && probes.Count == 0; t += PlayModeHelpers.FrameMs)
                {
                    Assert.That(label.Visible, Is.False);
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(view.Views[0].IsLifted, Is.True, "la cuia 0 sigue elevada");
                Assert.That(probes.Count, Is.EqualTo(2), "sale y se oculta en el mismo fotograma:\n" + string.Join("\n", probes));
                StringAssert.Contains("\"event\":\"stop_label\"", probes[0]);
                StringAssert.Contains("\"mode\":\"plate\"", probes[0]);
                StringAssert.Contains("\"event\":\"stop_label_off\"", probes[1]);
                StringAssert.Contains("\"level\":131,\"reason\":\"lift\"", probes[1]);
                Assert.That(label.Visible, Is.False, "no llega a verse");
                boot.TapCuia(0);
                PlayModeHelpers.Step(boot, 3000);
                Assert.That(label.Visible, Is.False, "descartada: no vuelve");
                Assert.That(view.NoticesQueued, Is.EqualTo(0));
                Assert.That(probes.Count, Is.EqualTo(2));
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }

        // Revisión de FX4, (a): sobre el muelle (el 171 solo con el muelle: de lado cabría), centrada en su hueco y a la vista con
        // la cuia 0 elevada; si después sale debajo una burbuja «Te faltan {n}» (sin semillas para «+1 cuia» y sin video), se
        // oculta en el acto (stop_label_off, bubble) y no vuelve.
        [UnityTest]
        public IEnumerator Level171_OnTheDock_AMissingBubbleUnderItHidesIt()
        {
            var probes = new List<string>();
            Application.LogCallback capture = CaptureProbes(probes, "stop_label_off");
            Application.logMessageReceived += capture;
            try
            {
                GameScreen.StopLabelSpotsOverride = GameScreen.StopLabelSpot.Dock;
                yield return Load(171, 150, true);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                StopLabel label = view.StopLabelView;
                boot.TapAnywhere();
                StepUntilShown(boot);
                PlayModeHelpers.Step(boot, 400);
                Assert.That(view.Tray.TryGetMissingTop(out _), Is.False, "condición del test: con 150 semillas, ninguna burbuja");
                BoardLayoutResult l = view.Layout!;
                Assert.That(view.StopLabelOnDock, Is.True);
                Assert.That(view.StopLabelMode, Is.EqualTo("dock"));
                StringAssert.Contains("\"mode\":\"dock\"", view.LastStopLabelProbe);
                Rect plate = label.Rect;
                float bandTop = l.PanelBottom + GameScreen.NoticeBubbleGapDp;
                float bandBottom = view.TrayTopDp - 8f;
                TestContext.WriteLine("171 sobre el muelle: placa " + plate + ", hueco " + bandTop + "-" + bandBottom + " dp");
                Assert.That(plate.yMin, Is.GreaterThanOrEqualTo(bandTop - 0.01f), "sobre el muelle, bajo el panel");
                Assert.That(plate.yMax, Is.LessThanOrEqualTo(bandBottom + 0.01f), "por encima de la bandeja");
                Assert.That(Mathf.Abs((plate.yMin - bandTop) - (bandBottom - plate.yMax)), Is.LessThanOrEqualTo(1.01f), "centrada en el hueco");
                Assert.That(label.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));

                Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
                PlayModeHelpers.Step(boot, 300);
                Assert.That(label.Visible, Is.True, "sobre el muelle, la elevación no la toca");
                boot.TapCuia(0);
                PlayModeHelpers.Step(boot, 300);
                Assert.That(probes.Count, Is.EqualTo(0), string.Join("\n", probes));

                Assert.That(boot.Wallet!.Apply(Transaction.Buy(SeedSink.ExtraCuia)).IsApplied, Is.True, "gasta las 150 semillas");
                PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
                Assert.That(view.Tray.TryGetMissingTop(out float bubbleTop), Is.True, "sale la burbuja «Te faltan {n}»");
                Assert.That(plate.yMax, Is.GreaterThan(bubbleTop - GameScreen.NoticeBubbleGapDp), "condición del test: la burbuja queda bajo la placa");
                Assert.That(label.Visible, Is.False, "se oculta en el acto");
                Assert.That(probes.Count, Is.EqualTo(1), string.Join("\n", probes));
                StringAssert.Contains("\"level\":171,\"reason\":\"bubble\"", probes[0]);
                PlayModeHelpers.Step(boot, 3000);
                Assert.That(label.Visible, Is.False, "descartada: no vuelve");
                Assert.That(view.NoticesQueued, Is.EqualTo(0));
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }

        // Revisión de FX4, (c): sin sitio en ninguna parte (el 171 solo con el muelle y, sin semillas ni video, la burbuja
        // «Te faltan 150» ya a la vista cuando le llega el turno, detrás de un aviso: entre el panel y la burbuja no cabe la placa
        // ni a 12 sp), el rótulo no sale: su sonda stop_label_off con no_room, sin stop_label, y la cola sigue.
        [UnityTest]
        public IEnumerator Level171_NoRoomAnywhere_ItDoesNotShow()
        {
            var probes = new List<string>();
            Application.LogCallback capture = CaptureProbes(probes, "stop_label");
            Application.logMessageReceived += capture;
            try
            {
                GameScreen.StopLabelSpotsOverride = GameScreen.StopLabelSpot.Dock;
                yield return Load(171, 0, true);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                StopLabel label = view.StopLabelView;
                view.ShowNotice(UiText.RescueApplied); // durante la intro: sale antes que el rótulo al habilitarse el toque
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 4 * PlayModeHelpers.FrameMs);
                Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), "condición del test: el aviso, delante");
                Assert.That(view.NoticesQueued, Is.EqualTo(1), "condición del test: el rótulo espera detrás");
                Assert.That(view.Tray.TryGetMissingTop(out float bubbleTop), Is.True, "condición del test: la burbuja «Te faltan 150»");
                BoardLayoutResult l = view.Layout!;
                float room = bubbleTop - GameScreen.NoticeBubbleGapDp - (l.PanelBottom + GameScreen.NoticeBubbleGapDp);
                TestContext.WriteLine("171 con la burbuja: " + room + " dp entre el panel y la burbuja");
                Assert.That(room, Is.LessThan(Mathf.Ceil((UiBuilder.MinTextSp * StopLabel.PlateHeightEm) - 0.01f)), "condición del test: no cabe ni a 12 sp");
                for (int t = 0; t < 8000 && probes.Count == 0; t += PlayModeHelpers.FrameMs)
                {
                    Assert.That(label.Visible, Is.False);
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(view.ToastText, Is.Null, "le llegó el turno al irse el aviso");
                Assert.That(probes.Count, Is.EqualTo(1), string.Join("\n", probes));
                StringAssert.Contains("\"event\":\"stop_label_off\"", probes[0]);
                StringAssert.Contains("\"level\":171,\"reason\":\"no_room\"", probes[0]);
                Assert.That(view.LastStopLabelProbe, Is.Null, "sin sonda stop_label: no salió");
                Assert.That(label.Visible, Is.False);
                Assert.That(view.NoticesQueued, Is.EqualTo(0), "la cola sigue");
                PlayModeHelpers.Step(boot, 3000);
                Assert.That(label.Visible, Is.False);
                Assert.That(probes.Count, Is.EqualTo(1));
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }

        [UnityTest]
        public IEnumerator Level131_NoAllocationsPerFrame_WithThePlateShown()
        {
            yield return NoAllocationsWithThePlate(131, "plate");
        }

        // Revisión de FX4, (d): de lado (el 171) y sobre el muelle (el 171 solo con el muelle: StopLabelDockLimit y
        // TryGetMissingTop en cada fotograma).
        [UnityTest]
        public IEnumerator Level171_NoAllocationsPerFrame_WithThePlateBeside()
        {
            yield return NoAllocationsWithThePlate(171, "plate_side");
        }

        [UnityTest]
        public IEnumerator Level171_NoAllocationsPerFrame_WithThePlateOnTheDock()
        {
            GameScreen.StopLabelSpotsOverride = GameScreen.StopLabelSpot.Dock;
            yield return NoAllocationsWithThePlate(171, "dock");
        }

        private static IEnumerator NoAllocationsWithThePlate(int level, string mode)
        {
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            StepUntilShown(boot);
            Assert.That(view.StopLabelMode, Is.EqualTo(mode));
            // Como NoAllocationsPerFrame_WithTheLabelShown: ya en régimen, tras los ~2,2 s del salto de la intro. Los rótulos del
            // 131 y del 171 duran 3.000 ms (el mínimo de NoticeTiming): a los 2.300, quedan 700 para los 30 fotogramas medidos
            // (480 ms), con la comprobación de cada fotograma de la fila 1 elevada o, sobre el muelle, de las burbujas (FX4).
            PlayModeHelpers.TickExact(boot, 2300);
            Assert.That(view.StopLabelView.Visible, Is.True);
            Assert.That(() =>
            {
                for (int f = 0; f < 30; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                }
            }, Is.Not.AllocatingGCMemory(), "30 fotogramas con la placa a la vista (" + mode + ")");
            Assert.That(view.StopLabelView.Visible, Is.True, "la placa siguió a la vista durante la medida");
        }

        // Las sondas AF_STATE cuyo evento empieza por prefix (stop_label recoge también stop_label_off).
        private static Application.LogCallback CaptureProbes(List<string> probes, string prefix)
        {
            string key = "\"event\":\"" + prefix;
            return (message, stack, type) =>
            {
                if (message.Contains(key))
                {
                    probes.Add(message);
                }
            };
        }

        private static RectTransform CanvasOf(StopLabel label)
        {
            return (RectTransform)label.Root.GetComponentInParent<Canvas>().rootCanvas.transform;
        }

        // Lo que se ve de una cuia (dp desde arriba a la izquierda del lienzo): sus gráficos activos con alfa, menos el área
        // táctil.
        internal static Rect VisibleBox(AF.Presentation.CuiaView cuia, RectTransform canvas)
        {
            Canvas.ForceUpdateCanvases();
            float left = float.MaxValue;
            float top = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MinValue;
            var corners = new Vector3[4];
            foreach (Graphic g in cuia.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.enabled || g is AF.Presentation.HitArea || g.color.a <= 0.01f || g.canvasRenderer.GetAlpha() <= 0.01f)
                {
                    continue;
                }

                g.rectTransform.GetWorldCorners(corners);
                foreach (Vector3 c in corners)
                {
                    Vector3 p = canvas.InverseTransformPoint(c);
                    left = Mathf.Min(left, p.x - canvas.rect.xMin);
                    right = Mathf.Max(right, p.x - canvas.rect.xMin);
                    top = Mathf.Min(top, canvas.rect.yMax - p.y);
                    bottom = Mathf.Max(bottom, canvas.rect.yMax - p.y);
                }
            }

            return Rect.MinMaxRect(left, top, right, bottom);
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

        private static IEnumerator Load(int level, int seeds = 1240, bool noAdFill = false)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            if (noAdFill)
            {
                LaunchOptions.AdFillOverride = FakeAdFill.NoFill; // sin video: la burbuja «Te faltan {n}» sale sin semillas
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
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

        // Pasa la intro y 5 s de juego sin que el rótulo salga ni quede en la cola.
        private static void StepWithoutIt(GameBootstrap boot, string when)
        {
            StopLabel label = boot.View!.StopLabelView;
            for (int t = 0; t < 8000 && (boot.Flow!.Phase != FlowPhase.Playing || t < 5000); t += PlayModeHelpers.FrameMs)
            {
                Assert.That(label.Visible, Is.False, "no sale " + when + " (t = " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(label.Visible, Is.False, when);
            Assert.That(boot.View.NoticesQueued, Is.EqualTo(0), when);
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
