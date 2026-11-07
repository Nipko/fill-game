#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La curva 2 de la 1.1 jugada en la escena (D-068, fase F4), con paquetes construidos en el test (<see cref="CurveFixtures"/>;
    /// los de la curva llegan con F5): el 303 de la curva 2 con el estreno del Aguapé (02 §9.1 M8), servido por el selector desde
    /// <c>progress.curve2_from_level</c>, con su tarjeta y su dedo guía, las filas fijas, la mata que no es origen, la suelta, Marea
    /// que la devuelve y la victoria; el Aguapé espeso, que se aclara con el primer sello; y la Lancha (02 §9.1 M11) con su muelle,
    /// el acercamiento, el atraque, Marea, Reducir movimiento y TalkBack, hasta ganar.
    /// </summary>
    public sealed class CurvePlayTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.TutorialFingerOverride = null;
            GameScreen.SimulatedScreen = null;
            GameBootstrap.A11yHostOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Aguape303_Curve2_CardFingerRowsLoosenTideAndWin()
        {
            GameScreen.TutorialFingerOverride = true;
            GameScreen.SimulatedScreen ??= HudLabelTests.Screen780;
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            yield return LoadCurve2(303);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;

            // El selector: el 303 de la curva 2 (curve2_from_level = 1 en un perfil nuevo).
            Assert.That(flow.Level.Id, Is.EqualTo("m07_v1_n0303_c2"));
            Assert.That(flow.Level.Curve, Is.EqualTo(LevelDefinition.Curve2));
            Assert.That(boot.App!.Catalog.Curve2Levels, Is.EqualTo(new[] { 302, 303 }));

            // La tarjeta del estreno con su texto.
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            StringAssert.StartsWith("El aguapé sujeta estas cuias", view.LevelCard.TutorialText);
            boot.TapAnywhere();
            boot.TapAnywhere(); // salta la intro
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // Filas fijas 4/3 (rows): las cuias 0-3 arriba y 4-6 abajo; el grupo (0 y 1) en la misma fila.
            BoardLayoutResult layout = view.Layout!;
            Assert.That(layout.Rows, Is.EqualTo(2));
            Assert.That(layout.PerRow[0], Is.EqualTo(4));
            Assert.That(layout.RowOf[3], Is.EqualTo(0));
            Assert.That(layout.RowOf[4], Is.EqualTo(1));

            AguapeOverlay mat0 = (AguapeOverlay)view.Views[0].OverlayOf(MechanicType.Aguape)!;
            AguapeOverlay mat1 = (AguapeOverlay)view.Views[1].OverlayOf(MechanicType.Aguape)!;
            Assert.That(mat0.Visible && mat0.MatVisible, Is.True);
            Assert.That(mat0.BridgeVisible, Is.True, "el grupo se lee como una mata: puente hacia la 1");
            Assert.That(mat1.BridgeVisible, Is.False, "la 2 no es del grupo");
            Assert.That(mat0.ThickVisible, Is.False);

            // Dedo guía hacia una cuia del grupo.
            TutorialFinger finger = TutorialFingerTests.WaitForFinger(boot, "el 303");
            Assert.That(view.TutorialFingerLesson, Is.EqualTo(FingerLesson.AguapeSeal));
            Assert.That(flow.Session.Current[finger.Target].Mechanic, Is.EqualTo(MechanicType.Aguape));

            // La mata no es origen: se sacude.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Rejected));
            PlayModeHelpers.Step(boot, 300);

            // 2 → 1 y 2 → 0: sella la 0 y suelta el grupo (paso Loosen en las dos).
            Assert.That(MechanicTestKit.Tap(boot, new Pour(2, 1, 1)).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(MechanicTestKit.Tap(boot, new Pour(2, 0, 1)).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Loosen, 0, 6000);
            Assert.That(mat0.IsAnimating, Is.True, "la mata se suelta");
            Assert.That(mat1.IsAnimating, Is.True, "todo el grupo a la vez");
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(mat0.Visible || mat1.Visible, Is.False);
            Assert.That(flow.Session.Current[1].Mechanic, Is.EqualTo(MechanicType.None));
            MechanicTestKit.AssertViewMatchesModel(boot);

            // Marea deshace el sello que la soltó: vuelve la mata.
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(flow.Session.Current[0].Mechanic, Is.EqualTo(MechanicType.Aguape));
            Assert.That(mat0.Visible && mat0.MatVisible, Is.True, "Marea vuelve a poner la mata (02 §9.1 M8, regla 7)");

            yield return FinishWithTheSolver(boot, "el 303");
        }

        [UnityTest]
        public IEnumerator Curve2Off_PlaysTheCurve1Board_AndCurve2FromLevelKeepsPlayedLevels()
        {
            LaunchOptions.Curve2EnabledOverride = false;
            yield return LoadCurve2(303);
            Assert.That(GameBootstrap.Current!.Flow!.Level.Id, Is.EqualTo("m07_v1_n0303"), "lvl_curve2_enabled = false: la curva 1");

            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            Profile played = FunnelSetup.Profile(500);
            played.Progress.Curve2FromLevel = 304;
            yield return LoadCurve2(303, played);
            Assert.That(GameBootstrap.Current!.Flow!.Level.Id, Is.EqualTo("m07_v1_n0303"), "jugado antes de la 1.1: no cambia de tablero");
        }

        [UnityTest]
        public IEnumerator ThickAguape_ThinsWithTheFirstSeal_AndLoosensWithTheSecond()
        {
            LaunchOptions.LevelPackJsonOverride = CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 701, CurveFixtures.ThickCuias, CurveFixtures.ThickAttrs));
            yield return MechanicTestKit.LoadPlaying(701);
            GameBootstrap boot = GameBootstrap.Current!;
            var thick = (AguapeOverlay)boot.View!.Views[0].OverlayOf(MechanicType.Aguape)!;
            Assert.That(thick.ThickVisible, Is.True, "el espeso, más tupido");
            Assert.That(thick.ThickAlpha, Is.GreaterThan(0.5f));

            yield return FinishWithTheSolver(boot, "el espeso", afterEach: b =>
            {
                BoardState s = b.Flow!.Session.Current;
                if (s[0].Mechanic == MechanicType.Aguape && CurveMechanics.AguapeSeals(s, s[0].MechanicGroup) == 1)
                {
                    PlayModeHelpers.Step(b, 400);
                    Assert.That(thick.ThickAlpha, Is.LessThan(0.05f), "se aclara con el primer sello (02 §9.1 M8, regla 9)");
                }
            });
        }

        [UnityTest]
        public IEnumerator Lancha_DockApproachDockTideReduceMotionTalkBack_AndWin()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            Profile reduced = FunnelSetup.Profile(500);
            reduced.Settings.ReduceMotion = ReduceMotionSetting.On;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(reduced);
            LaunchOptions.LevelPackJsonOverride = CurveFixtures.Pack("mundo_10_v1", "campaign", 5, 10,
                CurveFixtures.Level(10, 702, CurveFixtures.LanchaCuias, CurveFixtures.LanchaAttrs));
            yield return MechanicTestKit.LoadPlaying(702);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.ReduceMotion, Is.True);
            var lancha = (LanchaOverlay)boot.View!.Views[0].OverlayOf(MechanicType.Lancha)!;
            Assert.That(lancha.DockVisible && lancha.HullVisible, Is.True, "el muelle y la lancha");
            Assert.That(lancha.K, Is.EqualTo(2));
            Assert.That(lancha.Empties, Is.EqualTo(1), "una vacía de las dos");
            Assert.That(lancha.Pips.Filled, Is.EqualTo(1));
            Assert.That(boot.View.Views[0].OverlayHost.BodyNudgeDp, Is.LessThan(0f), "lejos del muelle: más baja");

            // TalkBack nombra la Lancha.
            boot.A11y!.Refresh();
            A11yItem? item = null;
            for (int i = 0; i < host.Items.Count; i++)
            {
                if (host.Items[i].Id == "cuia_0")
                {
                    item = host.Items[i];
                }
            }

            Assert.That(item, Is.Not.Null);
            StringAssert.EndsWith(A11yText.MechanicName("lancha"), item!.Label);
            Assert.That(A11yText.MechanicName("lancha"), Is.Not.Empty);

            // Ni origen ni destino.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Rejected));
            PlayModeHelpers.Step(boot, 200);

            // 1 → 2: sella y deja dos vacías: atraca (con Reducir movimiento, sin subida: la cuia ya en su sitio).
            Assert.That(MechanicTestKit.Tap(boot, new Pour(1, 2, 2)).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Dock, 0, 6000);
            Assert.That(lancha.Docking, Is.True);
            Assert.That(boot.View.Views[0].OverlayHost.BodyNudgeDp, Is.EqualTo(0f), "Reducir movimiento: sin subida");
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(lancha.Visible || lancha.HullVisible || lancha.DockVisible, Is.False, "atracada: una cuia normal");
            Assert.That(flow.Session.Current[0].Mechanic, Is.EqualTo(MechanicType.None));

            // Marea la devuelve al agua.
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            Assert.That(flow.Session.Current[0].Mechanic, Is.EqualTo(MechanicType.Lancha));
            Assert.That(lancha.Visible && lancha.HullVisible, Is.True, "Marea restaura el bit de atraque (02 §9.1 M11, regla 2)");

            yield return FinishWithTheSolver(boot, "la Lancha");
        }

        // El catálogo de prueba del mundo 7 (301-304) con la curva 2 (302 y 303) y el nivel cargado hasta la tarjeta o Playing.
        private static IEnumerator LoadCurve2(int level, Profile? profile = null)
        {
            LaunchOptions.LevelPackJsonOverride = CurveFixtures.Campaign(301, 304);
            LaunchOptions.CurvePackJsonOverride = new[] { CurveFixtures.Curve() };
            LaunchOptions.CurveReservePackJsonOverride = new[] { CurveFixtures.CurveReserve() };
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile ?? FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }

        // Juega la solución del A* desde el tablero del momento hasta ganar.
        private static IEnumerator FinishWithTheSolver(GameBootstrap boot, string at, System.Action<GameBootstrap>? afterEach = null)
        {
            LevelFlow flow = boot.Flow!;
            PlayModeHelpers.StepUntilIdle(boot, 8000);
            BoardState state = flow.Session.Current;
            SolveResult solved = new AStarSolver().Solve(state, SolverOptions.ForPipeline(flow.Session.PourMode, state));
            Assert.That(solved.IsSolved, Is.True, at + ": se resuelve desde aquí");
            for (int i = 0; i < solved.MoveCount; i++)
            {
                Assert.That(MechanicTestKit.Tap(boot, solved.GetMove(i)).Kind, Is.EqualTo(TapKind.Poured), at + ": vertido " + i);
                if (i < solved.MoveCount - 1)
                {
                    PlayModeHelpers.StepUntilIdle(boot, 8000);
                    afterEach?.Invoke(boot);
                }
            }

            PlayModeHelpers.Step(boot, 500);
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won), at + ": ganado");
            yield return null;
        }
    }
}
