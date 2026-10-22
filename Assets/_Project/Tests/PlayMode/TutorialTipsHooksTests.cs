#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las ayudas del tutorial de la tarea B11 en el juego (parche B11-tutorial; 02 §13; 06 §2.2):
    /// <list type="bullet">
    /// <item>el dedo del 2 señala al empezar el vertido que deja la lección posible y, hecho, la lección (Q-B11-1);</item>
    /// <item>el dedo de la Cuia extra del 8, tras el aviso de Capi y hasta usarla (04 §2.2);</item>
    /// <item>el tooltip «Reiniciar es gratis…» encima de Aguas estancadas en el 6 (y el 8), una vez por nivel, con la frase de las
    /// canoas solo con <c>biz_feature_lives</c> encendido (D-038);</item>
    /// <item>el tooltip de la Cosecha doble en la victoria del 11, y no en la del 12.</item>
    /// </list>
    /// </summary>
    public sealed class TutorialTipsHooksTests
    {
        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
            GameScreen.TutorialTipsOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.TutorialTipsOverride = null;
            GameScreen.TutorialFingerOverride = null;
            GameScreen.TutorialSlothOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level2_TheFingerFirstPointsAtThePourThatOpensTheLesson_ThenAtTheLesson()
        {
            GameScreen.TutorialFingerOverride = true;
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(TutorialFingerSetup.TryChoose(flow.Session.Current, flow.Session.PourMode, flow.Session.FutileRuleEnabled,
                new AStarSolver(), out Pour setup), Is.True);
            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 2, al empezar");
            Assert.That(view.TutorialFingerPour.Source, Is.EqualTo(setup.Source), "el paso previo (Q-B11-1)");
            Assert.That(view.TutorialFingerPour.Target, Is.EqualTo(setup.Target));
            Assert.That(view.LastTutorialFingerProbe, Does.Contain("\"lesson\":\"partial_setup\""));
            Assert.That(view.TutorialSlothVisible || view.TutorialShownCount == 1, Is.True,
                "la instrucción de TiTino no se va con el paso previo: habla de este tablero");

            Assert.That(boot.TapCuia(setup.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(setup.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "el paso previo hecho: se va");
            Assert.That(view.TutorialFingerArmed, Is.True, "y espera la lección");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            finger = TutorialFingerTests.WaitForFinger(boot, "el 2, la lección");
            Pour lesson = view.TutorialFingerPour;
            Assert.That(TutorialInstruction.IsPartialRunLesson(flow.Session.Current, lesson.Source, lesson.Target, flow.Session.PourMode), Is.True,
                "la racha de 3 sobre el hueco de 2 (02 §13)");
            Assert.That(view.LastTutorialFingerProbe, Does.Contain("\"lesson\":\"partial\""));
            Assert.That(boot.TapCuia(lesson.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(lesson.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerFollowed, Is.True);
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(2), "el paso previo y la lección");
            Assert.That(finger.Shown, Is.False);
        }

        [UnityTest]
        public IEnumerator Level2_AnotherPourDuringTheSetupStep_TheFingerWaitsAgain()
        {
            GameScreen.TutorialFingerOverride = true;
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            TutorialFingerTests.WaitForFinger(boot, "el 2, al empezar");
            Pour setup = view.TutorialFingerPour;
            BoardState board = flow.Session.Current;
            int source = -1;
            int target = -1;
            for (int s = 0; s < board.Count && source < 0; s++)
            {
                for (int t = 0; t < board.Count; t++)
                {
                    if (s != t && (s != setup.Source || t != setup.Target) && PourRules.IsLegal(board, s, t))
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
            Assert.That(view.TutorialFingerCancelled, Is.False, "el paso previo no se cancela con otro vertido [P]");
            Assert.That(view.TutorialFingerArmed, Is.True, "vuelve a esperar sobre el tablero nuevo");
        }

        [UnityTest]
        public IEnumerator Level8_ExtraCuiaFinger_AfterCapi_UntilItIsUsed()
        {
            yield return TutorialSlothTests.Load(8);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.ExtraCuia.Stock, Is.EqualTo(1), "condición del test: Capi regaló una (04 §2.2)");
            bool sawToastWithFinger = false;
            for (int t = 0; t < 15000 && !view.ExtraCuiaFingerVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                sawToastWithFinger |= view.ExtraCuiaFingerVisible && view.ToastText != null;
            }

            Assert.That(view.ExtraCuiaFingerVisible, Is.True, "el dedo de la Cuia extra sale (02 §13 y 06 §2.2: en su primer uso)");
            Assert.That(sawToastWithFinger, Is.False, "nunca a la vez que el aviso de Capi");
            Assert.That(view.ToastText, Is.Null, "el aviso de Capi ya se fue");
            TrayFinger finger = view.ExtraCuiaFinger!;
            Assert.That(finger.Target, Is.SameAs((RectTransform)view.Tray.ExtraCuiaButton.transform), "sobre «+1 cuia»");
            foreach (Graphic g in finger.Hand.transform.parent.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.False, g.name + " no captura toques");
            }

            Assert.That(view.LastTutorialTipProbe, Does.Contain("\"tip\":\"extra_cuia_finger\"").And.Contains("\"visible\":true"));
            Vector3 tip = finger.Hand.rectTransform.position;
            var button = (RectTransform)view.Tray.ExtraCuiaButton.transform;
            Vector3 local = button.InverseTransformPoint(tip);
            Assert.That(button.rect.Contains(new Vector2(local.x, local.y)), Is.True, "la yema, sobre el botón");

            // Sigue mientras no se use (en bucle) y se va al usarla; no vuelve en el nivel.
            PlayModeHelpers.Step(boot, (int)TrayFinger.CycleMs + 100);
            Assert.That(view.ExtraCuiaFingerVisible, Is.True);
            Assert.That(boot.PressExtraCuia(), Is.True, "la Cuia extra del stock");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.ExtraCuiaFingerVisible, Is.False, "usada: se va");
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(view.ExtraCuiaFingerVisible, Is.False, "y no vuelve");
        }

        [UnityTest]
        public IEnumerator Level8_ExtraCuiaFinger_ReduceMotion_FixedHand()
        {
            yield return TutorialSlothTests.Load(8);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.Flow!.ReduceMotion = true;
            for (int t = 0; t < 15000 && !view.ExtraCuiaFingerVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            TrayFinger finger = view.ExtraCuiaFinger!;
            Assert.That(finger.ReduceMotion, Is.True);
            Vector3 at = finger.Hand.rectTransform.position;
            for (int t = 0; t < (int)TrayFinger.CycleMs + 200; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(Vector3.Distance(finger.Hand.rectTransform.position, at), Is.LessThan(0.01f), "sin animación");
                Assert.That(finger.HandAlpha, Is.EqualTo(1f).Within(0.001f), "sin fundidos");
            }
        }

        [UnityTest]
        public IEnumerator OtherLevels_NoExtraCuiaFinger([Values(9, 12)] int level)
        {
            yield return TutorialSlothTests.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            for (int t = 0; t < 8000; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(boot.View!.ExtraCuiaFingerVisible, Is.False, "solo en el 8 (a los " + t + " ms)");
            }
        }

        [UnityTest]
        public IEnumerator Level6_Stuck_TheRestartTip_AboveTheModal_OncePerLevel()
        {
            yield return TutorialSlothTests.Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.RestartTipVisible, Is.True, "02 §13, fila 6: el tooltip con el primer modal");
            TipBubble tip = view.RestartTip!;
            Assert.That(tip.Text, Is.EqualTo(Loc.T(TutorialTips.RestartFreeCanoeKey)), "el texto canon de 04 §2.7");
            Assert.That(tip.TailUp, Is.False, "encima del panel, con el pico hacia él");

            // El pico, en el eje de «Volver a la orilla (gratis)» y en el canto de arriba del panel.
            var restart = (RectTransform)view.Stuck.RestartButton.transform;
            var space = (RectTransform)tip.Rect.parent;
            Vector3 center = space.InverseTransformPoint(restart.TransformPoint(restart.rect.center));
            Assert.That(tip.TailTip.x, Is.EqualTo(center.x - space.rect.xMin).Within(1f), "el pico, en el eje de «Volver a la orilla»");
            var corners = new Vector3[4];
            tip.Rect.GetWorldCorners(corners);
            float tipBottom = corners[0].y;
            view.Stuck.Panel.GetWorldCorners(corners);
            float panelTop = corners[1].y;
            Assert.That(tipBottom, Is.GreaterThanOrEqualTo(panelTop - 0.5f), "no tapa el panel (Marea, la primera opción)");
            Assert.That(tip.Alpha, Is.GreaterThan(0.5f));
            Assert.That(view.LastTutorialTipProbe, Does.Contain("\"tip\":\"restart\"").And.Contains(TutorialTips.RestartFreeCanoeKey));

            // Marea cierra el modal: el tooltip se va; un segundo bloqueo en el mismo nivel ya no lo trae.
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.RestartTipVisible, Is.False, "se va con el modal");
            PlayModeHelpers.StepUntilIdle(boot, 3000);
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.RestartTipVisible, Is.False, "una vez por nivel [P]");

            // Tampoco al salir al inicio y volver a entrar en el 6 (la app sigue abierta) [P].
            Assert.That(boot.PressQuit(), Is.True, "en 1-30, salir es gratis");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(6));
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.RestartTipVisible, Is.False, "ni al volver a entrar [P]");
        }

        [UnityTest]
        public IEnumerator Level6_WithoutLives_TheTipDropsTheCanoeSentence_D038()
        {
            yield return TutorialSlothTests.Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Wallet!.LivesEnabled = false; // lo que deja ConnectWallet con biz_feature_lives = false (08 §9; D-038)
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(boot.View!.RestartTipVisible, Is.True);
            Assert.That(boot.View.RestartTip!.Text, Is.EqualTo(Loc.T(TutorialTips.RestartFreeKey)), "sin canoas en los avisos (D-038)");
            Assert.That(boot.View.RestartTip.Text, Does.Not.Contain("canoa"));
        }

        [UnityTest]
        public IEnumerator Level11_Victory_TheDoubleHarvestTip_AboveTheButton_UntilPressed()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.DoubleHarvestShown, Is.True, "condición del test: la Cosecha doble, con su video");
            Assert.That(view.DoubleHarvestTipVisible, Is.True, "06 §2.2, fila 11: presentada con un tooltip sin personaje");
            TipBubble tip = view.DoubleHarvestTip!;
            Assert.That(tip.Text, Is.EqualTo(Loc.T(TutorialTips.DoubleHarvestKey)));
            var corners = new Vector3[4];
            tip.Rect.GetWorldCorners(corners);
            float tipBottom = corners[0].y;
            ((RectTransform)view.DoubleHarvestButton.transform).GetWorldCorners(corners);
            Assert.That(tipBottom, Is.GreaterThanOrEqualTo(corners[1].y - 0.5f), "encima del botón, sin taparlo");
            Assert.That(boot.PressDoubleHarvest(), Is.True);
            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.DoubleHarvestTipVisible, Is.False, "se va al pulsarlo");
        }

        [UnityTest]
        public IEnumerator Level12_Victory_NoDoubleHarvestTip()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.View!.DoubleHarvestShown, Is.True, "condición del test: la Cosecha doble, con su video");
            Assert.That(boot.View.DoubleHarvestTipVisible, Is.False, "una sola vez: en el 11");
        }
    }
}
