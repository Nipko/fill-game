#nullable enable
using System.Collections;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
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
    /// El dedo guía de los estrenos de la Hoja (43) y de la Tinaja (323) en el juego (tarea DC7; D-028 con P5 y D-029, la
    /// recomendada «sí, con el componente de DC6»; 02 §9.1 M1: «dedo guía en el primer uso»; M13: «con dedo guía: tinaja → su
    /// cuia y, después, su cuia → destino»), con los paquetes reales y la tarjeta del estreno:
    /// <list type="bullet">
    /// <item>43, primer intento: tras la tarjeta y la intro, el dedo señala un vertido de la fruta de la hoja a la cuia con hoja;
    /// siguiéndolo se abre la hoja y se gana; no en los reintentos (Volver a la orilla) ni en el 44;</item>
    /// <item>323 de la variante «a» (la que tiene la Tinaja; se prueba aunque la escena publique la «b»): primero tinaja → su cuia,
    /// con el gesto de sacar de TJ3, y después su cuia → destino; siguiéndolo y con la solución del A* se gana; deshacer con el
    /// paso 2 a la vista vuelve al paso 1; Morpho lo oculta mientras se ve; no en los reintentos ni en el 324;</item>
    /// <item>el 323 publicado con la «b» (sin Tinaja), sin dedo;</item>
    /// <item>ningún gráfico captura toques y la sonda <c>AF_STATE tutorial_finger</c> lleva la lección (<c>hoja</c>,
    /// <c>tinaja_scoop</c> y <c>tinaja_pour</c>).</item>
    /// </list>
    /// </summary>
    public sealed class DebutFingerTests
    {
        private const int HojaLevel = 43;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.TutorialFingerOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.TutorialFingerOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level43_FirstAttempt_PointsAtTheLeafFruitPour_FollowingItOpensTheLeaf_AndWins()
        {
            yield return Load43();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            int hoja = MechanicTestKit.IndexOf(flow, MechanicType.Hoja);
            Assert.That(hoja, Is.GreaterThanOrEqualTo(0), "el 43 lleva la Hoja");
            int key = flow.Session.Current[hoja].MechanicKey;

            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 43");
            Assert.That(view.TutorialFingerLesson, Is.EqualTo(FingerLesson.HojaKey));
            Assert.That(finger.Target, Is.EqualTo(hoja), "a la cuia con hoja");
            Assert.That(flow.Session.Current[finger.Source].TopColor, Is.EqualTo(key), "de la fruta de la hoja");
            Assert.That(PourRules.IsLegal(flow.Session.Current, finger.Source, finger.Target), Is.True);
            AssertNoRaycast(finger, "el 43");
            AssertProbe(view, true, finger.Source, hoja, "show", "hoja");
            AssertLoops(boot, finger, "el 43");

            int source = finger.Source;
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), "el dedo no bloquea el toque");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.TutorialFingerVisible, Is.True, "con el origen elevado, sigue (D-002)");
            Assert.That(boot.TapCuia(hoja).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "se va al hacer el vertido");
            Assert.That(view.TutorialFingerFollowed, Is.True);
            AssertProbe(view, false, source, hoja, "followed", "hoja");
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(flow.Session.Current[hoja].Mechanic, Is.Not.EqualTo(MechanicType.Hoja), "la hoja se abrió");

            yield return FinishWithTheSolver(boot, "el 43");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(1), "una vez en el intento");
        }

        [UnityTest]
        public IEnumerator Level43_NotAfterVolverALaOrilla_NorOnLevel44()
        {
            yield return Load43();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            TutorialFingerTests.WaitForFinger(boot, "el 43");
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla con el dedo a la vista");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "se va con el intento");
            AssertNever(boot, 9000, "el reintento del 43");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(1));

            // El 44, con Hoja o sin ella, ya no es el estreno.
            yield return LoadPlain(HojaLevel + 1);
            AssertNever(GameBootstrap.Current!, 6000, "el 44");
        }

        [UnityTest]
        public IEnumerator Level43_AnotherPourFirst_TheFingerGoes_AndDoesNotReturn()
        {
            yield return Load43();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 43");
            int source = finger.Source;
            int target = finger.Target;
            Pour other = OtherLegalPour(flow.Session.Current, source, target);
            Assert.That(MechanicTestKit.Tap(boot, other).Kind, Is.EqualTo(TapKind.Poured), "otro vertido");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerCancelled, Is.True);
            AssertProbe(view, false, source, target, "other_pour", "hoja");
            AssertNever(boot, 6000, "tras otro vertido");
        }

        [UnityTest]
        public IEnumerator Level323VariantA_TinajaToItsCuia_ThenItsCuiaToADestination_FollowingItWins()
        {
            yield return Load323();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            int tinaja = MechanicTestKit.IndexOf(flow, MechanicType.Tinaja);
            int scoop = MechanicTestKit.IndexOf(flow, MechanicType.Cucharon);

            // Paso 1: de la tinaja a su cuia.
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 1");
            TutorialFinger finger = boot.View!.TutorialFingerView!;
            Assert.That(view.TutorialFingerLesson, Is.EqualTo(FingerLesson.TinajaScoop));
            Assert.That(view.TutorialFingerStep, Is.EqualTo(1));
            Assert.That(finger.Source, Is.EqualTo(tinaja), "paso 1: la tinaja");
            Assert.That(finger.Target, Is.EqualTo(scoop), "a su cuia");
            AssertNoRaycast(finger, "el 323");
            AssertProbe(view, true, tinaja, scoop, "show", "tinaja_scoop");
            AssertLoops(boot, finger, "el 323, paso 1");

            // Sacar como señala, con el gesto de TJ3: la tinaja no se inclina y vuela su cuia.
            CuiaView tv = view.Views[tinaja];
            CuiaView cv = view.Views[scoop];
            Assert.That(boot.TapCuia(tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(scoop).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "el paso 1 se va al sacar");
            Assert.That(view.TutorialFingerArmed, Is.True, "y espera al paso 2");
            Assert.That(view.TutorialFingerStep, Is.EqualTo(2));
            AssertProbe(view, false, tinaja, scoop, "followed", "tinaja_scoop");
            bool layered = false;
            for (int t = 0; !view.IsViewIdle; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(t, Is.LessThan(8000), "la vista se queda quieta tras sacar");
                Assert.That(view.TutorialFingerVisible, Is.False, "el paso 2 no sale durante el gesto de sacar (" + t + " ms)");
                Assert.That(tv.BodyAngle, Is.EqualTo(0f).Within(0.01f), "la tinaja no se inclina (" + t + " ms)");
                layered |= cv.Body.parent != cv.Root;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(layered, Is.True, "el gesto de sacar: vuela su cuia (TJ3)");

            // Paso 2: de su cuia a un destino.
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 2");
            finger = boot.View!.TutorialFingerView!;
            Assert.That(finger.Source, Is.EqualTo(scoop), "paso 2: su cuia");
            Assert.That(finger.Target, Is.Not.EqualTo(tinaja), "la tinaja nunca recibe");
            Assert.That(PourRules.IsLegal(flow.Session.Current, finger.Source, finger.Target), Is.True);
            int target = finger.Target;
            AssertProbe(view, true, scoop, target, "show", "tinaja_pour");

            // La tinaja queda en el camino (en su fila, entre su cuia y el destino, con los extremos): la mano pasa por encima
            // de su boca, no sobre ella (no tapa su insignia).
            float x0 = Mathf.Min(cv.FootPosition.x, view.Views[target].FootPosition.x) - 1f;
            float x1 = Mathf.Max(cv.FootPosition.x, view.Views[target].FootPosition.x) + 1f;
            Assert.That(tv.FootPosition.x, Is.InRange(x0, x1), "condición del test: la tinaja, en el camino del paso 2");
            float jarTop = tv.FootPosition.y + tv.BodyOffset.y + (tv.HeightU * Mathf.Max(1f, tv.Unit));
            Assert.That(finger.RimTopY, Is.GreaterThanOrEqualTo(jarTop - 0.01f), "el arco cuenta la boca de la tinaja");
            AssertLoops(boot, finger, "el 323, paso 2");
            Assert.That(boot.TapCuia(scoop).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False);
            Assert.That(view.TutorialFingerFollowed, Is.True, "los dos pasos, hechos");
            AssertProbe(view, false, scoop, target, "followed", "tinaja_pour");

            yield return FinishWithTheSolver(boot, "el 323");
            Assert.That(view.TutorialFingerShownCount, Is.EqualTo(2), "una vez cada paso");
        }

        [UnityTest]
        public IEnumerator Level323VariantA_UndoWithStep2Showing_BackToStep1()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Undo = 3;
            yield return Load323(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            int tinaja = MechanicTestKit.IndexOf(flow, MechanicType.Tinaja);
            int scoop = MechanicTestKit.IndexOf(flow, MechanicType.Cucharon);
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 1");
            Assert.That(boot.TapCuia(tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(scoop).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 2");
            Assert.That(view.TutorialFingerStep, Is.EqualTo(2));

            Assert.That(boot.PressUndo(), Is.True, "Marea con el paso 2 a la vista");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False, "se va al deshacer");
            Assert.That(view.LastTutorialFingerProbe, Does.Contain("\"reason\":\"undo\""));
            Assert.That(view.TutorialFingerStep, Is.EqualTo(1), "vuelve al paso 1 [P]");
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            yield return DebutFingerTests.WaitReal(boot, "el 323, paso 1 otra vez");
            TutorialFinger finger = boot.View!.TutorialFingerView!;
            Assert.That(finger.Source, Is.EqualTo(tinaja));
            Assert.That(finger.Target, Is.EqualTo(scoop));
            AssertProbe(view, true, tinaja, scoop, "show", "tinaja_scoop");
        }

        [UnityTest]
        public IEnumerator Level323VariantA_MorphoHidesTheFingerWhileItShows()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Stock.Hint = 3;
            yield return Load323(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            yield return DebutFingerTests.WaitReal(boot, "el 323");
            yield return TinajaBoostersTests.PressAndWait(boot);
            Assert.That(boot.Flow!.LastHint!.Value.Answer, Is.EqualTo(HintAnswer.Pour), "Morpho propone un vertido");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            bool hidden = false;
            for (int t = 0; t < 10000 && view.MorphoFx.Active; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialFingerVisible, Is.False, "con Morpho a la vista, el dedo no (" + t + " ms)");
                hidden = true;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(hidden, Is.True, "condición del test: la mariposa salió");
            Assert.That(view.MorphoFx.Active, Is.False, "la mariposa termina");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs * 2);
            Assert.That(view.TutorialFingerVisible, Is.True, "y el dedo vuelve");
            Assert.That(view.LastTutorialFingerProbe, Does.Contain("\"reason\":\"resume\""));
        }

        [UnityTest]
        public IEnumerator Level323VariantA_NotAfterVolverALaOrilla_NorOn324()
        {
            yield return Load323();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            yield return DebutFingerTests.WaitReal(boot, "el 323");
            Assert.That(boot.PressRestart(), Is.True);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.TutorialFingerVisible, Is.False);
            AssertNever(boot, 9000, "el reintento del 323");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));

            // El 324 de la «a» también lleva tinaja, pero no es el estreno: sin dedo.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(Tinaja323EndToEndTests.VariantAPackJson(), TinajaTutorial.TipLevel,
                new MemoryProgressStore(TinajaTutorial.TipLevel, TinajaTutorial.TipLevel - 1));
            GameBootstrap next = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(next);
            Assert.That(next.Flow!.Level.HasTinaja, Is.True, "condición del test: el 324 de la «a» lleva tinaja");
            next.TapAnywhere();
            AssertNever(next, 6000, "el 324");
        }

        [UnityTest]
        public IEnumerator Published323_WithoutTheTinaja_HasNoFinger()
        {
            if (LevelCatalog.TinajaPublished)
            {
                Assert.Ignore("la escena publica la «a»: el 323 publicado lleva la Tinaja (lo prueban los tests de la «a»)");
            }

            yield return LoadPlain(TinajaTutorial.DebutLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Level.HasTinaja, Is.False, "variante «b»: sin Tinaja");
            AssertNever(boot, 6000, "el 323 de la «b»");
        }

        // ------------------------------------------------------------------ Carga

        /// <summary>El 43 real con su tarjeta del estreno: la cierra con un toque y salta la intro; queda en PLAYING.</summary>
        internal static IEnumerator Load43()
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(HojaLevel, new MemoryProgressStore(HojaLevel, HojaLevel - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Flow!.Level.IntroMechanic, Is.EqualTo(TutorialInstruction.HojaIntroMechanic), "el estreno de la Hoja");
            PassCardAndIntro(boot, "el 43");
        }

        /// <summary>El 323 real de la «a» con su tarjeta del estreno (como C1c): la cierra y salta la intro; queda en PLAYING.</summary>
        internal static IEnumerator Load323(Profile? profile = null)
        {
            yield return Tinaja323EndToEndTests.LoadReal323(profile ?? FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Level.IntroMechanic, Is.EqualTo(TinajaTutorial.IntroMechanic), "el estreno de la Tinaja");
            PassCardAndIntro(boot, "el 323");
        }

        // Un nivel de la escena sin tarjetas, con la intro saltada.
        private static IEnumerator LoadPlain(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
        }

        private static void PassCardAndIntro(GameBootstrap boot, string at)
        {
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), at + ": la tarjeta del estreno");
            Assert.That(boot.View!.TutorialFingerArmed || boot.View.TutorialFingerVisible, Is.False, at + ": nada con la tarjeta");
            Assert.That(boot.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            boot.TapAnywhere(); // salta Sumergir y teñir
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), at + ": en juego");
        }

        // ------------------------------------------------------------------ Comprobaciones

        /// <summary>
        /// Como <see cref="TutorialFingerTests.WaitForFinger"/>, pero con tiempo real: en el estreno de la Tinaja el vertido se
        /// busca en un hilo (SafeMoves), así que entre fotograma y fotograma se cede el control hasta que sale (como mucho
        /// <see cref="PlayModeHelpers.TimeoutSeconds"/> s).
        /// </summary>
        internal static IEnumerator WaitReal(GameBootstrap boot, string at)
        {
            GameScreen view = boot.View!;
            float start = Time.realtimeSinceStartup;
            while (!view.TutorialFingerVisible)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), at + ": el dedo sale");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (!view.TutorialFingerVisible)
                {
                    yield return null;
                }
            }

            TutorialFingerTests.WaitForFinger(boot, at); // ya a la vista: sus comprobaciones (PLAYING, sin la tropa, con su mano)
        }

        // Durante ms, el dedo ni se ve ni espera.
        private static void AssertNever(GameBootstrap boot, int ms, string at)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialFingerVisible, Is.False, at + ": sin dedo (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialFingerVisible, Is.False, at + ": sin dedo");
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

        // En una vuelta del bucle la mano toca el origen, luego el destino y se funde; el arco pasa por encima de las bocas.
        private static void AssertLoops(GameBootstrap boot, TutorialFinger finger, string at)
        {
            bool atSource = false;
            bool atTarget = false;
            bool faded = false;
            float maxY = float.NegativeInfinity;
            for (int t = 0; t < (int)TutorialFinger.CycleMs + 200; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Vector2 tip = finger.TipPosition;
                maxY = Mathf.Max(maxY, tip.y);
                atSource |= Vector2.Distance(tip, finger.SourceTap) < 0.5f && finger.HandAlpha > 0.9f;
                atTarget |= atSource && Vector2.Distance(tip, finger.TargetTap) < 0.5f && finger.HandAlpha > 0.9f;
                faded |= atTarget && finger.HandAlpha < 0.05f;
            }

            Assert.That(atSource, Is.True, at + ": toca el origen");
            Assert.That(atTarget, Is.True, at + ": y luego el destino");
            Assert.That(faded, Is.True, at + ": y se funde antes de volver a empezar");
            Assert.That(maxY, Is.GreaterThan(finger.RimTopY), at + ": el arco pasa por encima de las bocas");
            Assert.That(finger.Shown, Is.True, at + ": sigue en bucle");
        }

        private static void AssertProbe(GameScreen view, bool visible, int from, int to, string reason, string lesson)
        {
            string? probe = view.LastTutorialFingerProbe;
            Assert.That(probe, Is.Not.Null, "AF_STATE tutorial_finger");
            Assert.That(probe, Does.Contain("\"visible\":" + (visible ? "true" : "false")));
            Assert.That(probe, Does.Contain("\"from\":" + from + ","));
            Assert.That(probe, Does.Contain("\"to\":" + to + ","));
            Assert.That(probe, Does.Contain("\"lesson\":\"" + lesson + "\""));
            Assert.That(probe, Does.Contain("\"reason\":\"" + reason + "\""));
        }

        // Un vertido legal que no es el del dedo (y no a la hoja: así no es la lección hecha sin el dedo).
        private static Pour OtherLegalPour(BoardState state, int source, int target)
        {
            for (int s = 0; s < state.Count; s++)
            {
                for (int t = 0; t < state.Count; t++)
                {
                    if (s != t && (s != source || t != target) && state[t].Mechanic != MechanicType.Hoja && PourRules.IsLegal(state, s, t))
                    {
                        return new Pour(s, t, PourRules.Amount(state, s, t, PourModes.Default));
                    }
                }
            }

            Assert.Fail("sin otro vertido legal");
            return default;
        }

        // Termina el nivel con la solución óptima del A* desde el tablero de ahora (sin poda con tinaja) y espera a ganar.
        private static IEnumerator FinishWithTheSolver(GameBootstrap boot, string at)
        {
            LevelFlow flow = boot.Flow!;
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            BoardState state = flow.Session.Current;
            SolveResult solved = new AStarSolver().Solve(state, SolverOptions.ForPipeline(flow.Session.PourMode, state));
            Assert.That(solved.IsSolved, Is.True, at + ": se resuelve desde aquí");
            for (int i = 0; i < solved.MoveCount; i++)
            {
                Pour m = solved.GetMove(i);
                Assert.That(MechanicTestKit.Tap(boot, m).Kind, Is.EqualTo(TapKind.Poured), at + ": vertido " + i);
                Assert.That(boot.View!.TutorialFingerVisible, Is.False, at + ": el dedo no vuelve");
                if (i < solved.MoveCount - 1)
                {
                    PlayModeHelpers.StepUntilIdle(boot, 8000);
                    Assert.That(boot.View!.TutorialFingerVisible, Is.False, at + ": el dedo no vuelve");
                }
            }

            PlayModeHelpers.Step(boot, 500);
            Assert.That(boot.View!.TutorialFingerVisible, Is.False, at + ": ni en la celebración");
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won), at + ": ganado");
            yield return null;
        }
    }
}
