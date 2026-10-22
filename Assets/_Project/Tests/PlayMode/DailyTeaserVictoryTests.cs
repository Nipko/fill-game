#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.LiveOps;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Enganche E6 en la escena (<c>GameScreen.OnVictoryScreen</c>; 06 §2.2, nivel 7; 02 §13): la victoria real del 7 lleva la
    /// tarjeta de 2 s del Reto del río, que no toca el flujo de la victoria (Cosecha, Siguiente, Inicio y el paso al 8), no tapa
    /// la cinta, el panel ni sus botones, y se va sola o con la victoria. Sin el Reto en el build (sin pool:
    /// <c>DailyPoolDisabledOverride</c>), ni la victoria del 8, no sale. Simula E2 con <see cref="GameBootstrap.SetUpDaily"/> y el pool sintético.
    /// </summary>
    public sealed class DailyTeaserVictoryTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            LaunchOptions.LocTablesOverride = DailyTestTables.Texts();
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Victory7_ShowsTheTeaserTwoSeconds_OverTheCuias_AndTheVictoryGoesOn()
        {
            GameBootstrap boot = null!;
            yield return Load(7, true, b => boot = b);
            VictorySetup.WinToVictoryScreen(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            DailyTeaserCard teaser = view.DailyTeaser!;
            Assert.That(teaser, Is.Not.Null, "la victoria del 7 crea la tarjeta");
            Assert.That(teaser.Pending, Is.True);
            Assert.That(teaser.Visible, Is.False, "espera a la Cosecha");

            PlayModeHelpers.Step(boot, (int)DailyTeaserCard.DelayMs + 48);
            Assert.That(teaser.Visible, Is.True);
            Assert.That(teaser.Text, Is.EqualTo("Desde el nivel 10, un tablero nuevo cada día"));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(view.VictoryVisible, Is.True);
            Assert.That(view.NextButton.interactable, Is.True, "Siguiente sigue activo");
            Assert.That(view.HomeButton.interactable, Is.True, "Inicio sigue activo");
            Assert.That(teaser.BlocksRaycasts, Is.False);

            // No tapa la cinta, el panel ni sus botones.
            Rect card = WorldRect(teaser.Root, includeChildren: true);
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(WorldRect(view.VictoryRibbonRect, false).yMax - 0.01f), "encima de la cinta");
            Assert.That(card.Overlaps(WorldRect(view.VictoryPanelRect, false)), Is.False, "fuera del panel");
            Assert.That(card.Overlaps(WorldRect((RectTransform)view.NextButton.transform, false)), Is.False);
            Assert.That(card.Overlaps(WorldRect((RectTransform)view.HomeButton.transform, false)), Is.False);

            PlayModeHelpers.Step(boot, (int)DailyTeaserCard.VisibleMs);
            Assert.That(teaser.Visible, Is.False, "2 s y se va sola");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue esperando a Siguiente");

            Assert.That(boot.PressNext(), Is.True);
            for (int t = 0; t < 15000 && (boot.Flow == null || boot.Flow.LevelNumber != 8); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(8), "Siguiente lleva al 8 como siempre");
            Assert.That(teaser.Pending, Is.False);
        }

        /// <summary>
        /// Observación 2 de la hoja del 2026-10-22: TiTino de la victoria tapaba parte del texto («De[…]ivel 10»). La tarjeta va
        /// bajo su gesto si cabe; si no, TiTino cede la franja (D-016, punto 5: el gesto «si cabe»). Nunca se cruzan, en 360 × 780 y
        /// 360 × 640, al 100 % y al 130 %.
        /// </summary>
        [UnityTest]
        public IEnumerator Victory7_TheTeaser_IsNeverUnderTiTino([Values(false, true)] bool small, [Values(1f, 1.3f)] float scale)
        {
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            try
            {
                GameBootstrap boot = null!;
                yield return Load(7, true, b => boot = b);
                VictorySetup.WinToVictoryScreen(boot);
                GameScreen view = boot.View!;
                PlayModeHelpers.Step(boot, (int)DailyTeaserCard.DelayMs + 48);
                DailyTeaserCard teaser = view.DailyTeaser!;
                Assert.That(teaser.Visible, Is.True);
                SlothVictory sloth = view.SlothView;
                string at = (small ? "640" : "780") + " al " + (scale * 100f) + " %";
                if (!sloth.Visible)
                {
                    Assert.That(view.SlothYieldedToTeaser, Is.True, at + ": sin TiTino solo si cedió la franja a la tarjeta");
                    Debug.Log("AF_TEASER " + at + ": TiTino cede la franja");
                    yield break;
                }

                Assert.That(view.SlothYieldedToTeaser, Is.False, at);
                var mine = new List<Rect>();
                sloth.CollectRects(sloth.Layer, mine, true);
                Assert.That(mine.Count, Is.GreaterThan(0), at + ": las piezas de TiTino");
                int checkedParts = 0;
                foreach (Graphic g in teaser.Root.GetComponentsInChildren<Graphic>(false))
                {
                    Rect card = VictorySetup.CanvasRect(g.rectTransform, sloth.Layer);
                    if (card.width <= 0.01f || card.height <= 0.01f)
                    {
                        continue;
                    }

                    checkedParts++;
                    foreach (Rect part in mine)
                    {
                        if (part.width > 0.01f && part.height > 0.01f)
                        {
                            Assert.That(card.Overlaps(part), Is.False, at + ": la tarjeta " + card + " (" + g.name + ") bajo TiTino " + part);
                        }
                    }
                }

                Assert.That(checkedParts, Is.GreaterThan(0), at + ": la tarjeta, medida");
                Assert.That(teaser.SeenRect.yMin, Is.GreaterThanOrEqualTo(sloth.FootprintBottomDp - 0.5f), at + ": la tarjeta, bajo el pie de TiTino");
                Debug.Log("AF_TEASER " + at + ": TiTino hasta " + sloth.FootprintBottomDp + " dp; tarjeta " + teaser.SeenRect);
            }
            finally
            {
                GameScreen.SimulatedScreen = null;
            }
        }

        [UnityTest]
        public IEnumerator Victory7_NextDuringTheTeaser_TakesItAway()
        {
            GameBootstrap boot = null!;
            yield return Load(7, true, b => boot = b);
            VictorySetup.WinToVictoryScreen(boot);
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, (int)DailyTeaserCard.DelayMs + 48);
            DailyTeaserCard teaser = view.DailyTeaser!;
            Assert.That(teaser.Visible, Is.True);
            Assert.That(boot.PressNext(), Is.True);
            PlayModeHelpers.Step(boot, 64);
            Assert.That(teaser.Visible, Is.False, "se va con la victoria");
        }

        [UnityTest]
        public IEnumerator Victory7_WithoutTheRetoInTheBuild_NoTeaser()
        {
            GameBootstrap.DailyPoolDisabledOverride = true; // sin pool aunque la escena lo traiga (E3): D-008
            GameBootstrap boot = null!;
            yield return Load(7, false, b => boot = b);
            Assert.That(boot.DailyInBuild, Is.False);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, (int)DailyTeaserCard.DelayMs + 48);
            DailyTeaserCard? teaser = boot.View!.DailyTeaser;
            Assert.That(teaser == null || !teaser.Pending, Is.True, "D-008: sin Reto no se presenta");
        }

        [UnityTest]
        public IEnumerator Victory8_NoTeaser()
        {
            GameBootstrap boot = null!;
            yield return Load(8, true, b => boot = b);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, (int)DailyTeaserCard.DelayMs + 48);
            Assert.That(boot.View!.DailyTeaser, Is.Null, "solo la victoria del 7");
        }

        private static IEnumerator Load(int level, bool daily, System.Action<GameBootstrap> got)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            if (daily)
            {
                Assert.That(boot.SetUpDaily(), Is.True, "E2");
                boot.DailyRunner!.enabled = false;
                Assert.That(boot.DailyInBuild, Is.True);
            }

            got(boot);
        }

        // Rectángulo en coordenadas del mundo (y hacia arriba); con los hijos, el de todos sus gráficos activos.
        private static Rect WorldRect(RectTransform rt, bool includeChildren)
        {
            var corners = new Vector3[4];
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            void Add(RectTransform r)
            {
                r.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    xMin = Mathf.Min(xMin, corners[i].x);
                    xMax = Mathf.Max(xMax, corners[i].x);
                    yMin = Mathf.Min(yMin, corners[i].y);
                    yMax = Mathf.Max(yMax, corners[i].y);
                }
            }

            Add(rt);
            if (includeChildren)
            {
                foreach (Graphic g in rt.GetComponentsInChildren<Graphic>(false))
                {
                    Add(g.rectTransform);
                }
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
