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
            for (int t = 0; t < (int)TutorialFinger.CycleMs + 500; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(Vector2.Distance(finger.TipPosition, tip), Is.LessThan(0.01f), "sin animación (a los " + t + " ms)");
                Assert.That(finger.HandAlpha, Is.EqualTo(1f).Within(0.001f), "sin fundidos");
                Assert.That(finger.Hand.rectTransform.localScale.x, Is.EqualTo(1f).Within(0.001f), "sin toques animados");
            }
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
            for (int t = 0; t < (int)TutorialFinger.CycleMs + 200; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Vector2 tip = finger.TipPosition;
                atSource |= Vector2.Distance(tip, finger.SourceTap) < 0.5f && finger.HandAlpha > 0.9f;
                atTarget |= atSource && Vector2.Distance(tip, finger.TargetTap) < 0.5f && finger.HandAlpha > 0.9f;
                faded |= atTarget && finger.HandAlpha < 0.05f;
                minScale = Mathf.Min(minScale, finger.Hand.rectTransform.localScale.x);
            }

            Assert.That(atSource, Is.True, at + ": toca el origen");
            Assert.That(atTarget, Is.True, at + ": y luego el destino");
            Assert.That(faded, Is.True, at + ": y se funde antes de volver a empezar");
            Assert.That(minScale, Is.LessThan(0.95f), at + ": se hunde al tocar");
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
