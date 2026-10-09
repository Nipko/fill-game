#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.GameFlow;
using AF.Presentation;
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
    /// «Llenar y teñir» en la escena real (guion de la intro §8 y §16: A1, A3, A7, A8, A9): salto en cualquier instante con el
    /// estado final en ese fotograma y el primer toque de juego en el siguiente, restos fundidos en ≤ 100 ms y un solo
    /// <c>af_anim_skipped</c> con <c>anim = tint</c>; primero se mueve el río; un chorrito sobre cada cuia no vacía y ninguno
    /// sobre las vacías; nada de la intro recibe toques; la tropa queda a ≥ 8 dp de las cuias y fuera del HUD y del panel con
    /// 3, 7 y 16 cuias en 20:9 (360 × 780 dp) y 16:9 (el lienzo nunca baja de 780 dp: 439 × 780), en los tres modelos;
    /// seleccionar o verter no cambia nada de la tropa; 0 B por fotograma; la ruta de Reducir movimiento; y la tarjeta
    /// «¡Miquito a la vista!» del nivel 5 (guion §4.5, A15 sin la persistencia).
    /// </summary>
    public sealed class IntroTests
    {
        private static readonly int[] SkipTimes = { 0, 16, 300, 700, 1000, 1399 };

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Skip_AtAnyInstant_LeavesTheFinalStateInThatFrame_AndTheNextTapSelects()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            for (int s = 0; s < SkipTimes.Length; s++)
            {
                int at = SkipTimes[s];
                flow.Load(7); // intro larga desde t = 0
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
                Assert.That(flow.IntroDurationMs, Is.EqualTo(1400));
                PlayModeHelpers.TickExact(boot, at);
                Assert.That(view.Intro.Running, Is.True, "t = " + at);
                int skipsBefore = view.AnimSkippedCount;

                Assert.That(boot.TapAnywhere(), Is.True);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "input habilitado en el mismo fotograma");
                Assert.That(view.Intro.Running, Is.False);
                Assert.That(view.AnimSkippedCount - skipsBefore, Is.EqualTo(1), "un solo af_anim_skipped");
                Assert.That(view.LastAnimSkipAtMs, Is.EqualTo(at).Within(1));
                AssertFinalBoard(view, flow.Session.Current, "t = " + at);

                // Los restos se funden en su sitio en ≤ 100 ms y nada más se mueve.
                Vector2[] before = TroopPositions(view);
                PlayModeHelpers.TickExact(boot, 50);
                Assert.That(TroopPositions(view), Is.EqualTo(before), "los restos no se mueven al fundirse");
                PlayModeHelpers.TickExact(boot, 60);
                Assert.That(view.Intro.Active, Is.False, "restos con alfa 0 en ≤ 100 ms (t = " + at + ")");
                AssertNoIntroGraphicVisible(view);
                Assert.That(view.Ambient.RiverBoost, Is.EqualTo(0f));

                int target = FirstNonEmpty(flow.Session.Current);
                Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Selected), "el toque siguiente selecciona");
                boot.TapBackground();
                PlayModeHelpers.StepUntilIdle(boot, 1000);
            }
        }

        [UnityTest]
        public IEnumerator SkipOnTheFirstFrame_AlsoWorks_AfterTheNextFrame()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(1);
            Assert.That(boot.TapAnywhere(), Is.True);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
        }

        [UnityTest]
        public IEnumerator LongIntro_RiverMovesFirst_StreamsFillEveryNonEmptyCuia_AndTheLastBloomEndsAtT()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            flow.Load(7);
            IntroAnimation intro = view.Intro;
            Assert.That(view.IntroSpecies, Is.Not.Null, "nivel 7, primer intento: la tropa del mono ardilla");
            Assert.That(view.IntroSpecies!.Id, Is.EqualTo("saimiri_collinsi"));
            Assert.That(intro.Plan.Perch, Is.EqualTo(IntroPerch.Branch));

            // A3: en [0, 100] ms solo se mueven el río y sus destellos.
            Vector2[] troop0 = TroopPositions(view);
            for (int t = 0; t <= 100; t += 4)
            {
                PlayModeHelpers.TickExact(boot, t == 0 ? 0 : 4);
                for (int i = 0; i < view.Views.Length; i++)
                {
                    Assert.That(intro.StreamVisible(i), Is.False, "sin chorritos a " + t + " ms");
                }

                for (int k = 0; k < IntroChoreography.MaxFruits; k++)
                {
                    Assert.That(intro.FruitVisible(k), Is.False);
                }

                Assert.That(intro.Plan.SheetStartMs, Is.GreaterThan(100f), "la lámina desborda después");
                Assert.That(TroopPositions(view), Is.EqualTo(troop0), "la tropa no se mueve");
                for (int i = 0; i < view.Views.Length; i++)
                {
                    Assert.That(view.Views[i].LiquidLevel, Is.EqualTo(0f), "las cuias siguen vacías y secas");
                    Assert.That(view.Views[i].BodyOffset, Is.EqualTo(Vector2.zero), "sin caída: cada cuia en su ranura");
                }
            }

            Assert.That(view.Ambient.RiverBoost, Is.GreaterThan(0.5f), "el río ha crecido");

            // Chorrito sobre cada cuia no vacía (y ninguno sobre las vacías); todo lleno antes de 700 ms.
            BoardState board = flow.Session.Current;
            var streamed = new bool[board.Count];
            int now = 100;
            while (now < 700)
            {
                PlayModeHelpers.TickExact(boot, 4);
                now += 4;
                for (int i = 0; i < board.Count; i++)
                {
                    streamed[i] |= intro.StreamVisible(i);
                }
            }

            for (int i = 0; i < board.Count; i++)
            {
                Assert.That(streamed[i], Is.EqualTo(!board[i].IsEmpty), "chorrito de la cuia " + i);
                Assert.That(view.Views[i].LiquidLevel, Is.EqualTo((float)board[i].Count).Within(0.001f), "llena hasta sus dosis a 700 ms");
            }

            // La troupe entra cuando la fila 1 está llena y la última floración termina en T (input en T).
            Assert.That(intro.Plan.Row1FullMs, Is.LessThan(700f));
            Assert.That(intro.Plan.LastBloomEndMs, Is.EqualTo(1400f).Within(1000f / 60f));
            PlayModeHelpers.TickExact(boot, 1400 - now - 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "Phase == Playing en T (A1)");
            AssertFinalBoard(view, board, "fin natural");
            PlayModeHelpers.TickExact(boot, 320);
            Assert.That(view.Intro.Active, Is.False, "a T + 300 ms no queda nada de la intro");
            Assert.That(view.Troop.Active, Is.False);
        }

        [UnityTest]
        public IEnumerator NothingOfTheIntro_IsARaycastTarget_AtAnyTime()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(7);
            var graphics = new List<Graphic>();
            for (int t = 0; t <= 1700; t += 50)
            {
                graphics.Clear();
                boot.View!.Intro.CollectGraphics(graphics);
                Assert.That(graphics.Count, Is.GreaterThan(40));
                foreach (Graphic g in graphics)
                {
                    Assert.That(g.raycastTarget, Is.False, g.name + " a " + t + " ms");
                }

                PlayModeHelpers.TickExact(boot, 50);
            }
        }

        [UnityTest]
        public IEnumerator Troop_KeepsClearOfCuiasHudAndPanel_InTheThreeModels([Values(2340, 1920)] int screenHeightPx)
        {
            // 20:9 (360 × 780 dp) y 16:9 (el lienzo nunca baja de 780 dp de alto: 439 × 780 dp; 02 §5).
            GameScreen.SimulatedScreen = new Vector4(1080f, screenHeightPx, 96f, 48f);
            var cases = new (string Pack, int Level)[]
            {
                (string.Empty, 1), (string.Empty, 7), (PlayModeHelpers.SixteenCuiaPack(), 1),
                (PlayModeHelpers.Level7BoardPack(2, "w02_tapajos", "clear", 13), 13), // cuelga
                (PlayModeHelpers.Level7BoardPack(5, "w05_loreto", "white", 142), 142), // sacude
                (PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82), 82), // cuelga de la cola
            };
            var rects = new List<Rect>();
            int branches = 0;
            foreach ((string pack, int level) in cases)
            {
                LaunchOptions.Reset();
                if (pack.Length > 0)
                {
                    yield return PlayModeHelpers.LoadGameWithPack(pack, level, new MemoryProgressStore());
                }
                else
                {
                    yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore());
                }

                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                GameScreen view = boot.View!;
                foreach (int duration in new[] { 1400, 800 })
                {
                    view.DebugReplayIntro(duration, false);
                    IntroChoreography plan = view.Intro.Plan;
                    branches += plan.Perch == IntroPerch.Branch ? 1 : 0;
                    float hudBottom = view.Intro.Input.HudBottom;
                    float panelTop = view.Intro.Input.PanelTop;
                    for (int t = 0; t <= duration + 300; t += 25)
                    {
                        view.Intro.Seek(t);
                        rects.Clear();
                        view.Troop.CollectRects(rects);
                        foreach (Rect r in rects)
                        {
                            if (r.xMax < 0f || r.xMin > view.CanvasWidth)
                            {
                                continue; // aún en el follaje, fuera de la pantalla
                            }

                            string where = "nivel " + level + ", T " + duration + ", t " + t + ", " + r;
                            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(hudBottom - 0.5f), "nada sobre los botones del HUD: " + where);
                            Assert.That(r.yMax, Is.LessThanOrEqualTo(panelTop + 0.5f), "nada dentro del panel: " + where);
                            for (int i = 0; i < view.Views.Length; i++)
                            {
                                Assert.That(Distance(r, CuiaRect(view.Views[i])), Is.GreaterThanOrEqualTo(8f), "≥ 8 dp de la cuia " + i + ": " + where);
                            }
                        }
                    }
                }
            }

            Assert.That(branches, Is.GreaterThan(0), "algún caso con tropa");
        }

        [UnityTest]
        public IEnumerator SelectingOrPouring_ChangesNothingOfTheTroop()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            flow.Load(1);
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Branch));
            PlayModeHelpers.TickExact(boot, 1400 + 60); // fin natural; la tropa sale de espaldas
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.Troop.Active, Is.True);
            Vector2[] before = TroopPositions(view);
            float[] alpha = TroopAlphas(view);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(TroopPositions(view), Is.EqualTo(before), "seleccionar no cambia nada de la tropa (D-002)");
            Assert.That(TroopAlphas(view), Is.EqualTo(alpha));
            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(TroopPositions(view), Is.EqualTo(before), "verter no cambia nada de la tropa");
            PlayModeHelpers.TickExact(boot, 300);
            Assert.That(view.Troop.Active, Is.False, "la tropa ya salió");
        }

        [UnityTest]
        public IEnumerator Retry_UsesTheCanopy_WithoutTroop()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 1000);
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(boot.Flow!.IntroDurationMs, Is.EqualTo(GameConfig.AnimTintDurationMsLate));
            Assert.That(boot.View!.IntroSpecies, Is.Null, "reintento: dosel");
            Assert.That(boot.View.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Canopy));
            Assert.That(boot.View.Troop.Active, Is.False);
            Assert.That(boot.View.Intro.Plan.FruitCount, Is.EqualTo(15));
        }

        [UnityTest]
        public IEnumerator IntroTick_AllocatesNothing_AfterStart()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(1400, false);
            IntroAnimation intro = view.Intro;

            // Pasadas completas para calentar (primer uso de cada gráfico), y luego cada fotograma sin asignar.
            for (int pass = 0; pass < 1; pass++)
            {
                view.DebugReplayIntro(1400, false);
                for (int t = 0; t < 1700; t += 16)
                {
                    intro.Tick(16f);
                }
            }

            view.DebugReplayIntro(1400, false);
            for (int t = 0; t < 1700; t += 16)
            {
                Assert.That(() => intro.Tick(16f), Is.Not.AllocatingGCMemory(), "t = " + t);
            }
        }

        [UnityTest]
        public IEnumerator ReduceMotion_FadesIn250ms_WithAStillMonkey()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            flow.ReduceMotion = true;
            flow.Load(7);
            Assert.That(flow.IntroDurationMs, Is.EqualTo(GameConfig.ReducedMotionIntroMs), "la intro dura 250 ms en total");
            Vector2[] start = TroopPositions(view);
            int visible = 0;
            for (int i = 0; i < start.Length; i++)
            {
                visible += view.Troop.Rig(i).Visible ? 1 : 0;
            }

            Assert.That(visible, Is.LessThanOrEqualTo(1), "un solo miquito, quieto (Q13 A)");
            for (int t = 0; t < 1100; t += 25)
            {
                for (int i = 0; i < view.Views.Length; i++)
                {
                    Assert.That(view.Views[i].BodyOffset, Is.EqualTo(Vector2.zero));
                    Assert.That(view.Intro.StreamVisible(i), Is.False, "sin chorritos");
                }

                for (int k = 0; k < IntroChoreography.MaxFruits; k++)
                {
                    Assert.That(view.Intro.FruitVisible(k), Is.False, "sin frutas");
                }

                Vector2[] now = TroopPositions(view);
                for (int i = 0; i < now.Length; i++)
                {
                    if (view.Troop.Rig(i).Visible)
                    {
                        Assert.That(now[i], Is.EqualTo(start[i]), "ninguna pieza cambia de posición");
                    }
                }

                if (t == 250)
                {
                    Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
                    AssertFinalBoard(view, flow.Session.Current, "fundido de 250 ms");
                }

                PlayModeHelpers.TickExact(boot, 25);
            }

            Assert.That(view.Intro.Active, Is.False, "el miquito quieto se desvanece 600 ms después");
            flow.ReduceMotion = false;
        }

        [UnityTest]
        public IEnumerator SightingCard_AppearsAfterTheIntroOfLevel5_WithoutBlockingTouchesOrCoveringCuias()
        {
            yield return PlayModeHelpers.LoadGame(4, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            Assert.That(boot.View!.Sighting.Visible, Is.False, "nivel 4: Fruta nueva (guanábana), sin tarjeta");

            boot.Flow!.Load(5);
            boot.TapAnywhere(); // también tras un salto: la tarjeta llega después de la intro
            GameScreen view = boot.View;
            SightingCard card = view.Sighting;
            Assert.That(card.Visible, Is.True, "primer nivel libre desde el nivel_tarjeta del mono ardilla");
            Assert.That(card.Name, Is.EqualTo("mono ardilla"));
            Assert.That(card.Fact.Length, Is.InRange(1, 50), "una línea de ≤ 50 caracteres");
            var graphics = new List<Graphic>();
            card.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }

            // No tapa cuias ni botones: en la franja del panorama (entre el HUD y el panel) o bajo el panel.
            RectTransform root = card.Root;
            var r = new Rect(root.anchoredPosition.x, -root.anchoredPosition.y, root.sizeDelta.x, root.sizeDelta.y);
            float hudBottom = view.Intro.Input.HudBottom;
            float panelTop = view.Intro.Input.PanelTop;
            bool inBand = r.yMin >= hudBottom && r.yMax <= panelTop;
            bool onDock = r.yMin >= view.Intro.Input.PanelBottom;
            Assert.That(inBand || onDock, Is.True, "posición " + r);

            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected), "la tarjeta no captura toques");
            boot.TapBackground();
            PlayModeHelpers.TickExact(boot, SightingRule.CardMs + 50);
            Assert.That(card.Visible, Is.False, "2,5 s y se desvanece");

            // En el reintento no vuelve a salir.
            PlayModeHelpers.StepUntilIdle(boot, 1000);
            Assert.That(boot.PressRestart(), Is.True);
            boot.TapAnywhere();
            Assert.That(card.Visible, Is.False);
        }

        private static void AssertFinalBoard(GameScreen view, BoardState board, string when)
        {
            for (int i = 0; i < view.Views.Length; i++)
            {
                CuiaView v = view.Views[i];
                Assert.That(v.Locked, Is.False, when);
                Assert.That(v.LiquidLevel, Is.EqualTo((float)board[i].Count).Within(0.001f), when + ": cuia " + i);
                for (int k = 0; k < board[i].Count; k++)
                {
                    Color expected = Palette.Tint(board[i].ColorAt(k));
                    Color shown = v.DoseColor(k);
                    Assert.That(Mathf.Abs(shown.r - expected.r) + Mathf.Abs(shown.g - expected.g) + Mathf.Abs(shown.b - expected.b), Is.LessThan(0.01f), when + ": cuia " + i + ", dosis " + k);
                }
            }
        }

        private static void AssertNoIntroGraphicVisible(GameScreen view)
        {
            var graphics = new List<Graphic>();
            view.Intro.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                bool shown = g.enabled && g.gameObject.activeInHierarchy && g.canvasRenderer.GetAlpha() > 0.001f;
                Assert.That(shown, Is.False, g.name + " sigue a la vista");
            }
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

        private static Vector2[] TroopPositions(GameScreen view)
        {
            var p = new Vector2[IntroChoreography.MaxMonkeys];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = view.Troop.Rig(i).Root.anchoredPosition;
            }

            return p;
        }

        private static float[] TroopAlphas(GameScreen view)
        {
            var a = new float[IntroChoreography.MaxMonkeys];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = view.Troop.Rig(i).Alpha;
            }

            return a;
        }

        // Caja de la cuia en reposo (dp del lienzo, y desde arriba).
        private static Rect CuiaRect(CuiaView v)
        {
            float w = v.Unit * CuiaGeometry.WidthU;
            float h = v.HeightU * v.Unit;
            Vector2 foot = v.FootPosition;
            return Rect.MinMaxRect(foot.x - (w * 0.5f), -foot.y - h, foot.x + (w * 0.5f), -foot.y);
        }

        private static float Distance(Rect a, Rect b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
