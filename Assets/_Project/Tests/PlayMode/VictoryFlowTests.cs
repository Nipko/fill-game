#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Victoria con la cadena corta de D-008 en la escena (punto 3; 02 §6; tarea B2-4): la Cosecha cuenta hacia arriba en
    /// 400 ms hasta +20 y el saldo de arriba la recibe; desde el 11, «Cosecha doble: x2 por video» («+20 → +40») la duplica
    /// exactamente con un anuncio simulado; sin fill el botón no aparece; antes del 11 no existe; Siguiente lleva a N + 1 sin
    /// auto-avance; sin solapes y con controles ≥ 48 dp.
    /// </summary>
    public sealed class VictoryFlowTests
    {
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
        public IEnumerator Level11_TheHarvestCountsUp_AndTheDoubleHarvestDoublesItExactly()
        {
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(120), "la Cosecha ya está cobrada (eco_reward_normal = 20)");
            Assert.That(view.VictoryVisible, Is.True);
            Assert.That(view.DoubleHarvestShown, Is.True, "desde el 11, con video");
            Assert.That(view.DoubleHarvestGainText, Is.EqualTo("+20 → +40"));
            Assert.That(view.WalletBar.Visible, Is.True, "el saldo arriba en la victoria");

            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.HarvestShown, Is.EqualTo(0), "empieza a contar cuando el panel ya subió");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100), "el saldo recibe la Cosecha mientras cuenta");
            PlayModeHelpers.Step(boot, 350);
            Assert.That(view.HarvestShown, Is.InRange(1, 19), "a mitad de los 400 ms");
            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.HarvestText, Is.EqualTo("+20"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(120));

            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1));
            Assert.That(FunnelSetup.AdsPresenter.Last!.Placement, Is.EqualTo(AdPlacement.DoubleHarvest));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(140), "x2 exacto: +20 → +40");
            Assert.That(boot.Flow!.HarvestSeeds, Is.EqualTo(40));
            Assert.That(view.DoubleHarvestShown, Is.False, "una vez por victoria");
            PlayModeHelpers.Step(boot, 500);
            Assert.That(view.HarvestText, Is.EqualTo("+40"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(140));
            Assert.That(boot.PressDoubleHarvest(), Is.False);

            view.NextButton.onClick.Invoke();
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(12), "Siguiente → N + 1 (en el 11 no hay intersticial)");
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level1_ShowsTheHarvest_WithoutDoubleHarvest_AndDoesNotAdvanceAlone()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore(1, 0));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(view.HarvestText, Is.EqualTo("+20"));
            Assert.That(view.DoubleHarvestShown, Is.False, "la Cosecha doble llega en el 11");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(20));

            PlayModeHelpers.Step(boot, 5000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "victory_autonext_s = 0: sin auto-avance");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level11_WithoutFill_TheDoubleHarvestDoesNotAppear()
        {
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.Flow!.DoubleHarvestOffered, Is.True, "el flujo lo ofrecería");
            Assert.That(boot.View!.DoubleHarvestShown, Is.False, "02 §6: sin anuncio precargado el botón no aparece");
            Assert.That(boot.PressDoubleHarvest(), Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator VictoryPanel_DoubleNextAndHome_DoNotOverlap_AndAreTappable()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // SM-S938B: 360 × 780 dp con zona segura
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            try
            {
                RectTransform root = (RectTransform)view.Canvas.transform;
                Rect dbl = VictorySetup.CanvasRect((RectTransform)view.DoubleHarvestButton.transform, root);
                Rect next = VictorySetup.CanvasRect((RectTransform)view.NextButton.transform, root);
                Rect home = VictorySetup.CanvasRect((RectTransform)view.HomeButton.transform, root);
                Assert.That(dbl.yMax, Is.LessThanOrEqualTo(next.yMin + 0.5f), "Cosecha doble encima de Siguiente");
                Assert.That(next.yMax, Is.LessThanOrEqualTo(home.yMin + 0.5f), "Inicio debajo de Siguiente");
                Assert.That(dbl.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f), "07 §7.8: ≥ 48 dp");
                Assert.That(next.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f));
                Assert.That(home.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f));
                Assert.That(dbl.yMin - view.VictoryPanelHeight, Is.GreaterThan(0f), "el panel no sube hasta la fila del saldo");
                Assert.That(home.yMin - next.yMin, Is.EqualTo(68f).Within(0.5f), "Inicio, 68 dp bajo el borde del CTA (desde B1-6)");
            }
            finally
            {
                GameScreen.SimulatedScreen = null;
            }
        }
    }

    /// <summary>Victorias en la escena con el reloj del test.</summary>
    internal static class VictorySetup
    {
        /// <summary>Perfil de una segunda sesión (no la de instalación) con <paramref name="seeds"/> semillas.</summary>
        public static Profile Profile(int seeds, int canoes = 5, int levelsSinceInterstitial = 0)
        {
            Profile profile = FunnelSetup.Profile(seeds, canoes);
            profile.Ads.LevelsSinceInterstitial = levelsSinceInterstitial;
            return profile;
        }

        /// <summary>Salta la intro, juega la solución del solver y avanza hasta VICTORY_SCREEN.</summary>
        public static void WinToVictoryScreen(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            if (flow.Phase == FlowPhase.Intro)
            {
                boot.TapAnywhere();
            }

            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            for (int elapsed = 0; elapsed < 15000 && flow.Phase != FlowPhase.VictoryScreen; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        // Rectángulo de rt en dp del lienzo root, con origen arriba a la izquierda y la y hacia abajo.
        public static Rect CanvasRect(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
