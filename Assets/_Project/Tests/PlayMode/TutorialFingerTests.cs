#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El dedo guía del tutorial en el juego (tarea DC6; D-028, pregunta 61 (a) «con P5»; 02 §13, filas 1 y 2; 06 §2.2; 02 §4,
    /// D-002):
    /// <list type="bullet">
    /// <item>en el 1 y el 2, primer intento: sale tras la intro y cuando la tropa ya se fue, señala el vertido de la lección (el
    /// primero de la solución en el 1; en el 2, la racha de 3 sobre el hueco de 2, en cuanto es legal), nunca bloquea el toque, y
    /// se va al hacerlo; los dos niveles, de punta a punta siguiendo el dedo;</item>
    /// <item>otro vertido: se va y no vuelve en ese intento [P]; tampoco tras Volver a la orilla [P];</item>
    /// <item>deshacer con Marea con el dedo a la vista no es otro vertido [P]: se va (<c>undo</c>) y vuelve cuando la lección es
    /// legal otra vez; en el 2, la instrucción de TiTino se va al salir el dedo [P];</item>
    /// <item>el arco del viaje lleva la mano entera por encima de las bocas (a mitad del viaje no tapa ninguna cuia del medio,
    /// D-002) y la flecha de Reducir movimiento no pisa el marco del panel;</item>
    /// <item>ningún otro nivel lo muestra: el 3, el 6 y el 12 (D-028, 61 a: la presentación de TiTino hace de guía);</item>
    /// <item>con Reducir movimiento, sin animación: mano fija sobre el origen y flecha al destino;</item>
    /// <item>la sonda <c>AF_STATE tutorial_finger</c> dice si se ve, el origen y el destino.</item>
    /// </list>
    /// </summary>
    public sealed class TutorialFingerTests
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
            GameScreen.TutorialFingerOverride = null;
            GameScreen.TutorialSlothOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level1_AfterTheIntro_PointsAtTheFirstPour_FollowingItWins()
        {
            yield return TutorialSlothTests.Load(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(TutorialFinger.TryChoose(FingerLesson.FirstPour, flow.Session.Current, flow.Session.PourMode,
                flow.Session.FutileRuleEnabled, new AStarSolver(), out Pour expected), Is.True);
            TutorialFinger finger = WaitForFinger(boot, "el 1");
            Assert.That(view.IntroTroopVisible, Is.False, "cuando la tropa ya se fue");
            Assert.That(finger.Source, Is.EqualTo(expected.Source), "el origen del primer vertido de la solución");
            Assert.That(finger.Target, Is.EqualTo(expected.Target), "su destino");
            Assert.That(finger.ReduceMotion, Is.False);
            AssertNoRaycast(finger, "el 1");
            AssertProbe(view, true, expected.Source, expected.Target, "show");
            Assert.That(view.TutorialSlothVisible || view.TutorialShownCount == 1, Is.True, "la instrucción de TiTino sigue siendo la tarjeta del nivel");
            Assert.That(view.FruitNewView.Visible, Is.False, "ninguna otra tarjeta");

            // En bucle: la mano va del origen al destino.
            AssertLoops(boot, finger, "el 1");

            // Seleccionar el origen no lo cambia (D-002: nada se resalta al seleccionar); verter lo retira.
            Assert.That(boot.TapCuia(expected.Source).Kind, Is.EqualTo(TapKind.Selected), "el dedo no bloquea el toque");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.TutorialFingerVisible, Is.True, "con el origen elevado, sigue");
            Assert.That(finger.Source, Is.EqualTo(expected.Source));
            Assert.That(boot.TapCuia(expected.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "se va al hacer el vertido");
            Assert.That(view.TutorialFingerFollowed, Is.True);
            AssertProbe(view, false, expected.Source, expected.Target, "followed");

            yield return FinishWithTheSolver(boot, "el 1");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(1), "una vez en el intento");
        }

        [UnityTest]
        public IEnumerator Level2_WaitsForTheLessonToBeLegal_ThenPointsAtTheRunOf3OnTheHoleOf2_FollowingItWins()
        {
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;

            // El tablero inicial del 2 está lleno: no hay hueco de 2 y el dedo espera (la tropa ya se fue).
            PlayModeHelpers.Step(boot, 6000);
            Assert.That(view.IntroTroopVisible, Is.False, "condición del test: la tropa ya se fue");
            Assert.That(view.TutorialFingerVisible, Is.False, "sin lección legal, sin dedo");
            Assert.That(view.TutorialFingerArmed, Is.True, "espera");

            // El primer vertido de la solución deja la lección posible: las dos dosis de arriba de la cuia 0 a la vacía 3.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            TutorialFinger finger = WaitForFinger(boot, "el 2");
            Pour pour = view.TutorialFingerPour;
            Assert.That(TutorialInstruction.IsPartialRunLesson(flow.Session.Current, pour.Source, pour.Target, flow.Session.PourMode), Is.True,
                "la racha de 3 sobre el hueco de 2 (02 §13)");
            Assert.That(pour, Is.EqualTo(new Pour(2, 0, 2)), "la racha de açaí de la cuia 2 sobre la cuia 0");
            Assert.That(finger.Source, Is.EqualTo(2));
            Assert.That(finger.Target, Is.EqualTo(0));
            AssertNoRaycast(finger, "el 2");
            AssertProbe(view, true, 2, 0, "show");
            AssertLoops(boot, finger, "el 2");

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "se va al hacer el vertido");
            Assert.That(view.TutorialFingerFollowed, Is.True);
            Assert.That(flow.Session.Current[2].Count, Is.EqualTo(2), "el parcial: la tercera dosis se queda");
            yield return FinishWithTheSolver(boot, "el 2");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AnotherPour_TheFingerGoes_AndDoesNotReturnInThatAttempt_NorAfterVolverALaOrilla()
        {
            yield return TutorialSlothTests.Load(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            TutorialFinger finger = WaitForFinger(boot, "el 1");
            Pour pointed = view.TutorialFingerPour;
            int source = -1;
            int target = -1;
            BoardState board = flow.Session.Current;
            for (int s = 0; s < board.Count && source < 0; s++)
            {
                for (int t = 0; t < board.Count; t++)
                {
                    if (s != t && PourRules.IsLegal(board, s, t) && !(s == pointed.Source && t == pointed.Target))
                    {
                        source = s;
                        target = t;
                        break;
                    }
                }
            }

            Assert.That(source, Is.GreaterThanOrEqualTo(0), "condición del test: otro vertido legal");
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "otro vertido: se va");
            Assert.That(view.TutorialFingerCancelled, Is.True);
            AssertProbe(view, false, pointed.Source, pointed.Target, "other_pour");

            // Ni al deshacer con Marea ni al esperar.
            Assert.That(boot.PressUndo(), Is.True);
            for (int t = 0; t < 5000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialFingerVisible, Is.False, "no vuelve en ese intento (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            // Volver a la orilla: un intento nuevo con intro, sin dedo (no en los reintentos [P]).
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            for (int t = 0; t < 9000 && (flow.Phase != FlowPhase.Playing || view.IntroTroopVisible); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "condición del test: el intento nuevo, en juego");
            for (int t = 0; t < 5000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialFingerVisible || view.TutorialFingerArmed, Is.False, "ni en el reintento (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(1));
            Assert.That(finger.Shown, Is.False);
        }

        [UnityTest]
        public IEnumerator Level2_UndoWithTheFingerShowing_ItGoes_AndComesBackWhenTheLessonIsLegalAgain()
        {
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            TutorialFinger finger = WaitForFinger(boot, "el 2");
            Assert.That(view.TutorialFingerPour, Is.EqualTo(new Pour(2, 0, 2)));

            // Deshacer no es otro vertido [P]: se va con su motivo y espera (el tablero inicial, lleno, no tiene la lección).
            Assert.That(boot.PressUndo(), Is.True, "Marea, disponible en el 2");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0), "condición del test: deshecho");
            Assert.That(view.TutorialFingerVisible, Is.False, "deshacer: se va");
            Assert.That(view.TutorialFingerCancelled, Is.False, "no es otro vertido");
            Assert.That(view.TutorialFingerArmed, Is.True, "vuelve a esperar");
            AssertProbe(view, false, 2, 0, "undo");
            for (int t = 0; t < 3000; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.TutorialFingerVisible, Is.False, "sin lección legal, sin dedo (a los " + t + " ms)");
            }

            // El mismo primer vertido vuelve a dejar la lección posible: el dedo vuelve y se sigue hasta ganar.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            finger = WaitForFinger(boot, "el 2 tras deshacer");
            Assert.That(view.TutorialFingerPour, Is.EqualTo(new Pour(2, 0, 2)));
            AssertProbe(view, true, 2, 0, "show");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(2), "vuelve una vez");
            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerFollowed, Is.True);
            Assert.That(finger.Shown, Is.False);
            yield return FinishWithTheSolver(boot, "el 2");
        }

        [UnityTest]
        public IEnumerator Level2_TheTiTinoInstructionLeavesWhenTheFingerShows()
        {
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            for (int t = 0; t < 8000 && !view.TutorialSlothVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialSlothVisible, Is.True, "condición del test: la instrucción del 2 a la vista");
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            for (int t = 0; t < 8000 && !view.TutorialFingerVisible; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialSlothVisible || !view.TutorialFingerVisible, Is.True);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialFingerVisible, Is.True, "el dedo sale");
            SlothNoticeCard card = view.SlothNoticeView!;
            float alpha = card.Visible ? card.Alpha : 0f;
            Assert.That(card.DurationMs, Is.GreaterThan(3000f),
                "condición del test: la instrucción dura lo bastante para coincidir con el dedo si no se fuera");
            PlayModeHelpers.Step(boot, 250);
            Assert.That(view.TutorialSlothVisible, Is.False, "la instrucción del tablero de antes se va con su fundido (alfa al salir el dedo: " + alpha + ")");
            Assert.That(view.TutorialFingerVisible, Is.True, "el dedo sigue");
        }

        [UnityTest]
        public IEnumerator FollowedInTheFirstAttempt_NotAgainAfterVolverALaOrilla()
        {
            yield return TutorialSlothTests.Load(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            TutorialFinger finger = WaitForFinger(boot, "el 1");
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla con el dedo a la vista");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "se va con el intento");
            Assert.That(finger.Shown, Is.False);
            PlayModeHelpers.Step(boot, 9000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.TutorialFingerVisible, Is.False, "no en los reintentos [P]");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OtherLevels_NoFinger_The12NeitherWithItsPresentation([Values(3, 6, 12)] int level)
        {
            yield return TutorialSlothTests.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialFingerVisible || view.TutorialFingerArmed, Is.False, "el " + level + " no lleva dedo (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.IntroTroopVisible, Is.False, "condición del test: la tropa ya se fue");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(0));
            Assert.That(view.TutorialFingerView == null || !view.TutorialFingerView.Shown, Is.True);
            if (level == TutorialInstruction.MorphoLevel)
            {
                Assert.That(view.TutorialSlothVisible || view.TutorialShownCount == 1, Is.True,
                    "en el 12 la guía es la presentación de TiTino con la cola sobre «Pista» (D-028, 61 a)");
            }
        }

        [UnityTest]
        public IEnumerator ReduceMotion_FixedHandOnTheSource_AndAnArrowToTheTarget([Values(1, 2)] int level)
        {
            yield return TutorialSlothTests.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.Flow!.ReduceMotion = true;
            if (level == 2)
            {
                Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 6000);
            }

            TutorialFinger finger = WaitForFinger(boot, "el " + level + " con Reducir movimiento");
            Assert.That(finger.ReduceMotion, Is.True);
            Assert.That(finger.ArrowVisible, Is.True, "flecha al destino");
            Assert.That(finger.ArrowHead.sprite, Is.Not.Null);
            Assert.That(finger.Ripple.gameObject.activeSelf, Is.False, "sin onda");
            AssertProbe(view, true, finger.Source, finger.Target, "show");
            Assert.That(view.LastTutorialFingerProbe, Does.Contain("\"reduce\":true"));
            Vector2 tip = finger.TipPosition;
            Assert.That(Vector2.Distance(tip, finger.SourceTap), Is.LessThan(0.01f), "la mano, sobre el origen");

            // La punta de la flecha, sobre el destino; nada se mueve con el tiempo.
            Vector2 head = finger.ArrowHead.rectTransform.anchoredPosition;
            Assert.That(Mathf.Abs(head.x - finger.TargetTap.x), Is.LessThan(0.01f), "la punta, en el eje del destino");
            Assert.That(head.y, Is.GreaterThan(finger.TargetTap.y), "sobre su boca");
            Assert.That(float.IsInfinity(finger.CeilingY), Is.False, "con el borde del panel");
            Assert.That(head.y + (0.5f * ProceduralArt.FingerArrowHeadHeightDp),
                Is.LessThanOrEqualTo(finger.CeilingY - TutorialFinger.ArrowCeilingAirDp + 0.01f),
                "la punta, a ≥ " + TutorialFinger.ArrowCeilingAirDp + " dp del marco del panel");
            Assert.That(finger.RippleOutline.gameObject.activeSelf, Is.False, "sin contorno de onda");
            for (int t = 0; t < (int)TutorialFinger.CycleMs + 500; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(Vector2.Distance(finger.TipPosition, tip), Is.LessThan(0.01f), "sin animación (a los " + t + " ms)");
                Assert.That(finger.HandAlpha, Is.EqualTo(1f).Within(0.001f), "sin fundidos");
                Assert.That(finger.Hand.rectTransform.localScale.x, Is.EqualTo(1f).Within(0.001f), "sin toques animados");
            }
        }

        [UnityTest]
        public IEnumerator HalfwayThroughTheMove_TheHandDoesNotCoverTheCuiasInBetween([Values(1, 2)] int level)
        {
            yield return TutorialSlothTests.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            if (level == 2)
            {
                Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 6000);
            }

            TutorialFinger finger = WaitForFinger(boot, "el " + level);
            float half = TutorialFinger.AppearMs + TutorialFinger.PressMs + (0.5f * TutorialFinger.MoveMs);
            for (int guard = 0; guard < 400 && finger.CycleTimeMs < half; guard++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(finger.CycleTimeMs, Is.InRange(half, half + PlayModeHelpers.FrameMs + 0.5f), "condición del test: a mitad del viaje");
            Assert.That(finger.HandAlpha, Is.EqualTo(1f).Within(0.001f), "la mano, a la vista");

            // El rectángulo de la mano (inclinada), en las coordenadas del dedo (las del contenedor del tablero, y hacia arriba).
            var corners = new Vector3[4];
            finger.Hand.rectTransform.GetWorldCorners(corners);
            Rect hand = Bounds(finger.Root, corners);
            CuiaView[] views = boot.View!.Views;
            int between = 0;
            for (int i = 0; i < views.Length; i++)
            {
                if (i == finger.Source || i == finger.Target)
                {
                    continue;
                }

                CuiaView v = views[i];
                float u = v.Unit;
                Vector2 foot = v.FootPosition + v.BodyOffset;
                float top = foot.y + (v.HeightU * u);

                // El tapón: el borde de fibra de lo alto de la boca; y la cuia entera, de ancho a ancho.
                var cap = Rect.MinMaxRect(foot.x - (CuiaGeometry.RimHalfWidthU * u), top - (CuiaGeometry.RimU * u),
                    foot.x + (CuiaGeometry.RimHalfWidthU * u), top);
                var body = Rect.MinMaxRect(foot.x - (0.5f * CuiaGeometry.WidthU * u), foot.y, foot.x + (0.5f * CuiaGeometry.WidthU * u), top);
                string at = "el " + level + ", cuia " + i + " (mano " + hand + ", tapón " + cap + ")";
                Assert.That(hand.Overlaps(cap), Is.False, at + ": la mano no corta el tapón de una cuia que no es de la lección (D-002)");
                Assert.That(hand.Overlaps(body), Is.False, at + ": ni la cuia");
                if (body.xMax > hand.xMin && body.xMin < hand.xMax)
                {
                    between++;
                    Assert.That(hand.yMin, Is.GreaterThanOrEqualTo(top + TutorialFinger.ArcHandAirDp - 1f),
                        at + ": la mano pasa a " + TutorialFinger.ArcHandAirDp + " dp por encima de su boca");
                }
            }

            Assert.That(between, Is.GreaterThan(0), "condición del test: a mitad del viaje la mano pasa sobre una cuia del medio");
        }

        // El rectángulo que cubre las esquinas (del mundo) en las coordenadas locales de space.
        private static Rect Bounds(RectTransform space, Vector3[] worldCorners)
        {
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int c = 0; c < worldCorners.Length; c++)
            {
                Vector2 p = space.InverseTransformPoint(worldCorners[c]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // Espera (hasta 8 s) a que salga el dedo: nunca con la tropa en la rama ni fuera de PLAYING.
        internal static TutorialFinger WaitForFinger(GameBootstrap boot, string at)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 8000 && !view.TutorialFingerVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialFingerVisible, Is.True, at + ": el dedo sale");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), at + ": en PLAYING");
            Assert.That(view.IntroTroopVisible, Is.False, at + ": cuando la tropa ya se fue");
            TutorialFinger finger = view.TutorialFingerView!;
            Assert.That(finger.Hand.sprite, Is.Not.Null, at + ": con su mano");
            return finger;
        }

        // Ningún gráfico del dedo captura toques.
        private static void AssertNoRaycast(TutorialFinger finger, string at)
        {
            Graphic[] graphics = finger.Root.GetComponentsInChildren<Graphic>(true);
            Assert.That(graphics.Length, Is.GreaterThan(0));
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, at + ": " + g.name + " no captura toques");
            }
        }

        // En una vuelta del bucle la mano toca el origen y luego el destino, y se funde al final.
        private static void AssertLoops(GameBootstrap boot, TutorialFinger finger, string at)
        {
            bool atSource = false;
            bool atTarget = false;
            bool faded = false;
            float minScale = 1f;
            float maxY = float.NegativeInfinity;
            bool ripple = false;
            bool halo = false;
            float minStroke = float.PositiveInfinity;
            float maxStroke = 0f;
            for (int t = 0; t < (int)TutorialFinger.CycleMs + 200; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Vector2 tip = finger.TipPosition;
                maxY = Mathf.Max(maxY, tip.y);
                ripple |= finger.Ripple.color.a > 0.5f && finger.RippleOutline.color.a > 0.15f;
                halo |= finger.RippleHalo.color.a > 0.1f;
                if (finger.Ripple.color.a > 0f)
                {
                    // El trazo del anillo en dp: el del sprite (pintado a FingerRippleStrokeDp) por lo que se escala.
                    Sprite ring = finger.Ripple.sprite;
                    float scale = finger.Ripple.rectTransform.sizeDelta.x / (ring.rect.width / ProceduralArt.UiPxPerDp);
                    float stroke = ProceduralArt.FingerRippleStrokeDp * scale;
                    minStroke = Mathf.Min(minStroke, stroke);
                    maxStroke = Mathf.Max(maxStroke, stroke);
                    Assert.That(finger.RippleHalo.sprite, Is.Not.Null, at + ": el halo, con su anillo");
                    Assert.That(finger.RippleHalo.rectTransform.sizeDelta, Is.EqualTo(finger.Ripple.rectTransform.sizeDelta), at + ": del tamaño del anillo");
                }
                atSource |= Vector2.Distance(tip, finger.SourceTap) < 0.5f && finger.HandAlpha > 0.9f;
                atTarget |= atSource && Vector2.Distance(tip, finger.TargetTap) < 0.5f && finger.HandAlpha > 0.9f;
                faded |= atTarget && finger.HandAlpha < 0.05f;
                minScale = Mathf.Min(minScale, finger.Hand.rectTransform.localScale.x);
            }

            Assert.That(atSource, Is.True, at + ": toca el origen");
            Assert.That(atTarget, Is.True, at + ": y luego el destino");
            Assert.That(faded, Is.True, at + ": y se funde antes de volver a empezar");
            Assert.That(minScale, Is.LessThan(0.95f), at + ": se hunde al tocar");
            Assert.That(maxY, Is.GreaterThan(finger.RimTopY), at + ": el arco pasa por encima de las bocas (no toca las cuias del medio)");
            Assert.That(maxY, Is.GreaterThanOrEqualTo(finger.RimTopY + TutorialFinger.HandDrop + TutorialFinger.ArcHandAirDp - 1f),
                at + ": lo alto del arco deja la mano entera por encima de las bocas");
            Assert.That(finger.Ripple.sprite, Is.Not.Null, at + ": la onda, un anillo");
            Assert.That(finger.RippleOutline.sprite, Is.Not.Null, at + ": con su contorno de tinta");
            Assert.That(ripple, Is.True, at + ": la onda se ve al tocar (papel 0,9 y tinta 0,35 al nacer)");
            Assert.That(halo, Is.True, at + ": con su halo cálido debajo (0,25 al nacer)");
            Color warm = ProceduralArt.FingerRippleHalo;
            Color tint = finger.RippleHalo.color;
            Assert.That(new Vector3(tint.r, tint.g, tint.b), Is.EqualTo(new Vector3(warm.r, warm.g, warm.b)), at + ": el halo, #FFE9A8");
            Assert.That(minStroke, Is.GreaterThanOrEqualTo(0.9f * ProceduralArt.FingerRippleStrokeDp - 0.01f),
                at + ": el trazo de la onda no adelgaza al empezar (" + minStroke + " dp)");
            Assert.That(maxStroke, Is.LessThanOrEqualTo(1.1f * ProceduralArt.FingerRippleStrokeDp + 0.01f),
                at + ": ni engorda al acabar (" + maxStroke + " dp)");
            Assert.That(finger.Shown, Is.True, at + ": sigue en bucle");
        }

        private static void AssertProbe(GameScreen view, bool visible, int from, int to, string reason)
        {
            string? probe = view.LastTutorialFingerProbe;
            Assert.That(probe, Is.Not.Null, "AF_STATE tutorial_finger");
            Assert.That(probe, Does.Contain("\"visible\":" + (visible ? "true" : "false")));
            Assert.That(probe, Does.Contain("\"from\":" + from + ","));
            Assert.That(probe, Does.Contain("\"to\":" + to + ","));
            Assert.That(probe, Does.Contain("\"reason\":\"" + reason + "\""));
        }

        // Termina el nivel con la solución óptima desde el tablero de ahora y espera a la celebración.
        private static IEnumerator FinishWithTheSolver(GameBootstrap boot, string at)
        {
            LevelFlow flow = boot.Flow!;
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            BoardState state = flow.Session.Current;
            SolveResult solved = new AStarSolver().Solve(state, SolverOptions.ForPipeline(flow.Session.PourMode, state)
                .WithFutileRule(flow.Session.FutileRuleEnabled));
            Assert.That(solved.IsSolved, Is.True, at + ": se resuelve desde aquí");
            for (int i = 0; i < solved.MoveCount; i++)
            {
                Pour m = solved.GetMove(i);
                Assert.That(boot.TapCuia(m.Source).Kind, Is.EqualTo(TapKind.Selected), at + ": vertido " + i);
                Assert.That(boot.TapCuia(m.Target).Kind, Is.EqualTo(TapKind.Poured), at + ": vertido " + i);
                Assert.That(boot.View!.TutorialFingerVisible, Is.False, at + ": el dedo no vuelve");
                if (i < solved.MoveCount - 1)
                {
                    PlayModeHelpers.StepUntilIdle(boot, 8000);
                }
            }

            PlayModeHelpers.Step(boot, 500);
            Assert.That(boot.View!.TutorialFingerVisible, Is.False, at + ": ni en la celebración");

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won), at + ": ganado");
            yield return null;
        }
    }
}
