#nullable enable
using System.Collections;
using AF.Core.Rules;
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
    /// Guardas de la dirección visual v2 (D-003; docs/art/direccion-visual-v2.md 3.4, 3.7, 5.1, 6.12 y 9): las capas
    /// decorativas no reciben toques y no tocan las zonas táctiles (V7, VA-08); los controles miden ≥ 48 dp; el ambiente
    /// nunca entra en el panel; Deshacer deshabilitado es plano y al pulsar la cara baja 4 dp (VA-12); el arte de la cuia
    /// se hornea a la densidad de u = 32 dp sin ampliarse (D-004; guía 4.1). El reloj lo lleva el test.
    /// </summary>
    public sealed class VisualGuardTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator OnlyButtonsAndTouchAreas_AreRaycastTargets()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 200);

            Graphic[] graphics = boot.View!.Canvas.GetComponentsInChildren<Graphic>(true);
            int targets = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic g = graphics[i];
                if (!g.raycastTarget)
                {
                    continue;
                }

                targets++;
                bool touchArea = g is HitArea;
                bool dim = g.name == "Dim"; // velo de los modales: bloquea el tablero detrás (07 §7.5)
                Assert.That(touchArea || dim, Is.True, "decoración con raycastTarget: " + Path(g.transform));
            }

            Assert.That(targets, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DockDecor_StaysOutOfTouchAreas_AndControlsAreAtLeast48dp()
        {
            // Nivel 1: una fila, banda libre de 172,5 dp con las cuias de 64 dp (D-004): el poste se ve; Capi solo con su PNG
            // (capi_idle.png, sin respaldo procedural: revisión visual del 2026-10-07).
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 200);
            GameScreen view = boot.View!;
            Assert.That(view.CapiVisible, Is.EqualTo(ArtSlots.Find(ArtSlots.Capi) != null), "Capi solo con capi_idle.png");
            Assert.That(view.DecorRects[1].gameObject.activeSelf, Is.True, "poste de amarre en el muelle (banda ≥ 64 dp)");

            Rect[] touch = new Rect[view.Views.Length + 2];
            for (int i = 0; i < view.Views.Length; i++)
            {
                touch[i] = WorldRect(view.Views[i].Root);
            }

            touch[view.Views.Length] = WorldRect((RectTransform)view.UndoButton.transform);
            touch[view.Views.Length + 1] = WorldRect((RectTransform)view.RestartButton.transform);
            RectTransform[] decor = view.DecorRects;
            for (int d = 0; d < decor.Length; d++)
            {
                if (!decor[d].gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect r = WorldRect(decor[d]);
                for (int t = 0; t < touch.Length; t++)
                {
                    Assert.That(r.Overlaps(touch[t]), Is.False, decor[d].name + " pisa una zona táctil (" + t + ")");
                }
            }

            // Controles ≥ 48 dp (07 §7.8): rectángulos táctiles en dp del lienzo.
            Assert.That(((RectTransform)view.UndoButton.transform).rect.width, Is.GreaterThanOrEqualTo(48f));
            Assert.That(((RectTransform)view.UndoButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f));
            Assert.That(((RectTransform)view.RestartButton.transform).rect.width, Is.GreaterThanOrEqualTo(48f));
            Assert.That(((RectTransform)view.RestartButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f));
        }

        [UnityTest]
        public IEnumerator Ambient_NeverEntersThePanel_AndStaysWithinBudget()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            BoardLayoutResult layout = view.Layout!;
            Transform canvas = view.Canvas.transform;
            for (int step = 0; step < 40; step++)
            {
                PlayModeHelpers.Step(boot, 250);
                Assert.That(view.Ambient.ActiveCount, Is.LessThanOrEqualTo(AmbientLayer.PollenCount + AmbientLayer.GlintCount));
                Image[] images = view.Ambient.Near.GetComponentsInChildren<Image>(false);
                for (int i = 0; i < images.Length; i++)
                {
                    // Posición en dp desde arriba a la izquierda del lienzo.
                    Vector3 local = canvas.InverseTransformPoint(images[i].rectTransform.position);
                    Rect canvasRect = ((RectTransform)canvas).rect;
                    float x = local.x - canvasRect.xMin;
                    float y = canvasRect.yMax - local.y;
                    bool inside = x > layout.PanelX && x < layout.PanelX + layout.PanelWidth && y > layout.PanelY && y < layout.PanelBottom;
                    Assert.That(inside, Is.False, images[i].name + " dentro del panel en (" + x + ", " + y + ")");
                }
            }
        }

        [UnityTest]
        public IEnumerator Undo_IsFlatWhenDisabled_AndItsFaceSinks4dpWhenPressed()
        {
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            GameScreen view = boot.View!;
            ReliefButton relief = view.UndoButton.GetComponent<ReliefButton>();
            Assert.That(relief.Interactable, Is.False, "sin vertidos, Marea está deshabilitada");
            Image? baseImage = FindImage(view.UndoButton.transform, "Base");
            Assert.That(baseImage != null && !baseImage.enabled, Is.True, "deshabilitado: sin canto ni sombra (guía 5.1)");

            boot.TapCuia(2);
            boot.TapCuia(3);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(relief.Interactable, Is.True);
            Assert.That(baseImage!.enabled, Is.True);

            relief.Press(true);
            relief.Step(100f);
            Assert.That(relief.FaceOffsetDp, Is.EqualTo(ProceduralArt.ButtonPressDp).Within(0.01f));
            relief.Press(false);
            relief.Step(200f);
            Assert.That(relief.FaceOffsetDp, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator VictoryBurst_RisesAboveThePanel_ThenLeavesDriftDown_WithinBudget()
        {
            // Revisión visual del 2026-10-07 (06_celebration_l1): el estallido nace fuera de los bordes de la pantalla y su
            // pico sube por encima de panel.top − 40 dp (antes nacía en las esquinas del panel, sobre las cuias); nunca hay
            // más de 60 partículas (VA-13); tras él caen despacio como mucho 12 hojas y pétalos. Las cuias selladas brillan
            // detrás durante la victoria, y no antes (D-002).
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            float panelTop = view.Layout!.PanelY;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            float highest = float.PositiveInfinity;
            int maxCount = 0;
            int maxDrift = 0;
            bool auraBeforeVictory = false;
            bool auraInVictory = false;
            for (int ms = 0; ms < 7000; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                maxCount = Mathf.Max(maxCount, view.Burst.ActiveCount);
                highest = Mathf.Min(highest, view.Burst.HighestY);
                if (view.Burst.Drifting)
                {
                    maxDrift = Mathf.Max(maxDrift, view.Burst.ActiveCount);
                }

                bool aura = false;
                for (int v = 0; v < view.Views.Length; v++)
                {
                    aura |= view.Views[v].VictoryAuraVisible;
                }

                if (flow.Phase == FlowPhase.VictoryScreen)
                {
                    auraInVictory |= aura;
                }
                else
                {
                    auraBeforeVictory |= aura;
                }
            }

            Assert.That(maxCount, Is.GreaterThan(0).And.LessThanOrEqualTo(GameConfig.AnimConfettiMaxParticles), "VA-13: ≤ 60 partículas");
            Assert.That(highest, Is.LessThan(panelTop - 40f), "el pico del estallido queda por encima de panel.top − 40 dp");
            Assert.That(maxDrift, Is.InRange(1, ConfettiLayer.DriftCount), "caída lenta de ≤ 12 hojas y pétalos");
            Assert.That(auraInVictory, Is.True, "brillo detrás de las cuias selladas en la victoria");
            Assert.That(auraBeforeVictory, Is.False, "sin brillo de victoria antes de la pantalla de victoria");
        }

        [UnityTest]
        public IEnumerator Level1_CuiaArt_IsBakedForU32()
        {
            // D-004 (C18): en el nivel 1, u = 32 dp. A 3 px/dp (SM-S938B, 1080 × 2340 px) el vidrio se hornea a 96 px/u
            // (guía 4.1: ceil(u_dp × escala)) y el aro del sellado (2,4 u ≈ 230 px) y la tapa (1,10 u ≈ 106 px) no se
            // amplían; a 4 px/dp el vidrio llega a 128 px/u (MaxCuiaPixelsPerU).
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 200);
            GameScreen view = boot.View!;
            CuiaView cuia = view.Views[0];
            Assert.That(cuia.Unit, Is.EqualTo(32f).Within(0.01f), "u del nivel 1 (D-004)");

            Image? glass = FindImage(cuia.Root, "GlassBack");
            Image? ring = FindImage(cuia.Root, "SealRing");
            Image? cap = FindImage(cuia.Root, "SealCap");
            Assert.That(glass != null && ring != null && cap != null, Is.True, "GlassBack, SealRing y SealCap");
            Assert.That(glass!.sprite.texture.width, Is.EqualTo(192), "vidrio a 96 px/u (2 u de ancho)");
            Assert.That(ring!.sprite.texture.width, Is.GreaterThanOrEqualTo(231), "aro del sellado sin ampliar (2,4 u × 96)");
            Assert.That(cap!.sprite.texture.width, Is.GreaterThanOrEqualTo(106), "tapa sin ampliar (1,10 u × 96)");

            // 4 px/dp (1440 × 3120 px): el lienzo sigue en 360 × 780 dp y el vidrio se rehornea a 128 px/u.
            GameScreen.SimulatedScreen = new Vector4(1440f, 3120f, 128f, 64f);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(cuia.Unit, Is.EqualTo(32f).Within(0.01f), "u a 4 px/dp");
            Assert.That(glass.sprite.texture.width, Is.EqualTo(256), "vidrio a 128 px/u a 4 px/dp: CuiaPixelsPerUFor(32) = 128");
            Assert.That(ring.sprite.texture.width, Is.GreaterThanOrEqualTo(Mathf.CeilToInt(2.4f * 128f)), "aro a 4 px/dp");
            Assert.That(cap.sprite.texture.width, Is.GreaterThanOrEqualTo(Mathf.CeilToInt(1.10f * 128f)), "tapa a 4 px/dp");
        }

        private static Image? FindImage(Transform root, string name)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == name)
                {
                    return images[i];
                }
            }

            return null;
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                Mathf.Min(corners[0].x, corners[2].x),
                Mathf.Min(corners[0].y, corners[2].y),
                Mathf.Max(corners[0].x, corners[2].x),
                Mathf.Max(corners[0].y, corners[2].y));
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
