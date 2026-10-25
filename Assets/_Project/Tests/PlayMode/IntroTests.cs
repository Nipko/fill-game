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
    /// «Llenar y teñir» en la escena real (guion de la intro §8 y §16: A1, A3, A7, A8, A9, A10, A14): salto en cualquier
    /// instante con el estado final en ese fotograma y el primer toque de juego en el siguiente, restos fundidos en ≤ 100 ms y
    /// un solo <c>af_anim_skipped</c> con <c>anim = tint</c>; primero se mueve el río; un chorrito sobre cada cuia no vacía y
    /// ninguno sobre las vacías; nada de la intro recibe toques; la tropa queda a ≥ 8 dp de las cuias y fuera del HUD y del
    /// panel con 3, 7 y 16 cuias en 20:9 (360 × 780 dp) y 16:9 (360 × 640 dp desde DC3), en los tres
    /// modelos, y el agua nunca tapa la rama bajo los miquitos (labio &lt; rama &lt; tropa); seleccionar o verter no cambia
    /// nada de la tropa; 0 B por fotograma en todas las rutas; la onda del menisco, función solo del tiempo; la ruta de
    /// Reducir movimiento; la compuerta del arte de release; y la tarjeta «¡Miquito a la vista!» del nivel 5 (guion §4.5, A15
    /// sin la persistencia).
    /// </summary>
    public sealed class IntroTests
    {
        private static readonly int[] SkipTimes = { 0, 16, 300, 700, 1000, 1399 };

        [TearDown]
        public void TearDown()
        {
            BoardLayout.TroopBandOffForTests = false;
            GameScreen.SimulatedScreen = null;
            AmbientLayer.Frozen = false;
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
                Assert.That(view.Intro.RemnantAlpha, Is.EqualTo(0.5f).Within(0.01f), "a medio fundido a los 50 ms");
                PlayModeHelpers.TickExact(boot, 50);
                Assert.That(view.Intro.Active, Is.False, "restos con alfa 0 a los 100 ms (t = " + at + ")");
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
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // DC3: sin pantalla simulada, el Editor da un lienzo de 853 × 640 dp
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

            // A3: en [0, 100] ms solo se mueven el río y sus destellos (el polen, en pausa).
            Vector2[] troop0 = TroopPositions(view);
            Assert.That(view.Ambient.PollenPaused, Is.True, "el polen se pausa en t = 0 (guion §7.2)");
            var pollen0 = new Vector2[AmbientLayer.PollenCount];
            for (int i = 0; i < pollen0.Length; i++)
            {
                pollen0[i] = view.Ambient.PollenPosition(i);
            }

            for (int t = 0; t <= 100; t += 4)
            {
                PlayModeHelpers.TickExact(boot, t == 0 ? 0 : 4);
                for (int i = 0; i < pollen0.Length; i++)
                {
                    Assert.That(view.Ambient.PollenPosition(i), Is.EqualTo(pollen0[i]), "el polen no se mueve a " + t + " ms");
                }

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
            Assert.That(view.Ambient.RiverSwell.color.a, Is.GreaterThan(0.5f * AmbientLayer.RiverSwellAlpha), "la capa de crecida aclara la franja");

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

            // La tropa llega a su primera columna cuando la fila 1 está llena (D-011: entra antes, mientras el río llena) y la
            // última floración termina en T (input en T).
            Assert.That(intro.Plan.Row1FullMs, Is.LessThan(700f));
            Assert.That(intro.Plan.LastBloomEndMs, Is.EqualTo(1400f).Within(1000f / 60f));
            PlayModeHelpers.TickExact(boot, 1400 - now - 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "Phase == Playing en T (A1)");
            Assert.That(view.Ambient.PollenPaused, Is.False, "el polen vuelve con el input");
            AssertFinalBoard(view, board, "fin natural");

            // D-011, punto 3: antes, a T + 300 ms no quedaba nada; ahora la tropa reposa en la rama ya en juego y se va sola.
            PlayModeHelpers.TickExact(boot, 1200);
            Assert.That(view.Troop.Active, Is.True, "la tropa sigue a la vista 1.200 ms después de T");
            Assert.That(view.Intro.TroopVisible, Is.True);
            int left = Mathf.CeilToInt(view.Intro.TroopGoneMs - view.Intro.TimeMs) + 1;
            Assert.That(view.Intro.TroopGoneMs - 1400f, Is.LessThanOrEqualTo(2200f), "se va hacia T + 2,2 s");
            PlayModeHelpers.TickExact(boot, left);
            Assert.That(view.Intro.Active, Is.False, "tras la salida no queda nada de la intro");
            Assert.That(view.Troop.Active, Is.False);
            Assert.That(view.Intro.TroopVisible, Is.False);
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
            // 20:9 (360 × 780 dp) y 16:9 (360 × 640 dp desde DC3; 02 §5). El de 16 cuias, con el tablero de antes de D-046 (sin
            // la franja de la tropa: dosel); la rama con 3 filas, en IntroThreeRowBranchTests. En 16:9 los tableros que no dejan entre el HUD y el panel
            // la franja de la rama (BranchMinBandDp) van con el dosel, y la tropa que cuelga, con menos de 64 dp, en fila: la regla
            // de D-010 («pantallas de 640 dp sin franja», 02 §5, fila de la intro), no una excepción del test.
            GameScreen.SimulatedScreen = new Vector4(1080f, screenHeightPx, 96f, 48f);
            var cases = new (string Pack, int Level, IntroPerch Perch, TroopModel Model)[]
            {
                (string.Empty, 1, IntroPerch.Branch, TroopModel.Row), (string.Empty, 7, IntroPerch.Branch, TroopModel.Row),
                (PlayModeHelpers.SixteenCuiaPack(), 1, IntroPerch.Canopy, TroopModel.Row), // 3 filas sin la franja de D-046: dosel
                (PlayModeHelpers.Level7BoardPack(2, "w02_tapajos", "clear", 13), 13, IntroPerch.Branch, TroopModel.Hang), // cuelga
                (PlayModeHelpers.Level7BoardPack(5, "w05_loreto", "white", 142), 142, IntroPerch.Branch, TroopModel.Shake), // sacude
                (PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82), 82, IntroPerch.Branch, TroopModel.Hang), // cuelga de la cola
                (PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202), 202, IntroPerch.Branch, TroopModel.Shake), // la pareja, colas trenzadas
                (PlayModeHelpers.Level7BoardPack(7, "w07_madre_de_dios", "white", 302), 302, IntroPerch.Branch, TroopModel.Row), // bigote
                (PlayModeHelpers.Level7BoardPack(8, "w08_madidi", "white", 422), 422, IntroPerch.Branch, TroopModel.Hang), // brazos largos
            };
            var rects = new List<Rect>();
            int branches = 0;
            foreach ((string pack, int level, IntroPerch perch, TroopModel model) in cases)
            {
                LaunchOptions.Reset();
                BoardLayout.TroopBandOffForTests = perch == IntroPerch.Canopy;
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
                    float band = view.Intro.Input.PanelTop - view.Intro.Input.HudBottom;
                    bool short16x9 = screenHeightPx == 1920 && perch == IntroPerch.Branch;
                    IntroPerch wantPerch = short16x9 && band < IntroChoreography.BranchMinBandDp ? IntroPerch.Canopy : perch;
                    TroopModel wantModel = short16x9 && model == TroopModel.Hang && band < IntroChoreography.HangMinBandDp ? TroopModel.Row : model;
                    Assert.That(plan.Perch, Is.EqualTo(wantPerch), "posadero del nivel " + level + " (franja de " + band + " dp)");
                    if (wantPerch == IntroPerch.Branch)
                    {
                        Assert.That(plan.Model, Is.EqualTo(wantModel), "modelo del nivel " + level);
                    }

                    float hudBottom = view.Intro.Input.HudBottom;
                    float panelTop = view.Intro.Input.PanelTop;
                    float end = plan.ExitStartMs + IntroChoreography.ExitMs; // entrada, sueltas, reposo y salida (D-011)
                    for (int t = 0; t <= end; t += 25)
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

                            // Las mismas comprobaciones de siempre; el mensaje y el Assert, solo si alguna falla (T6, carril A: el
                            // texto y las restricciones de NUnit en cada pieza, instante y cuia hacían de este test uno de los más lentos).
                            bool clear = r.yMin >= hudBottom - 0.5f && r.yMax <= panelTop + 0.5f;
                            for (int i = 0; clear && i < view.Views.Length; i++)
                            {
                                clear = Distance(r, CuiaRect(view.Views[i])) >= 8f;
                            }

                            if (clear)
                            {
                                continue;
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

            if (screenHeightPx == 2340)
            {
                Assert.That(branches, Is.EqualTo(16), "8 tableros con tropa × 2 versiones");
            }
            else
            {
                Assert.That(branches, Is.GreaterThanOrEqualTo(2), "en 16:9, al menos el nivel 1 (1 fila) en la rama");
            }
        }

        /// <summary>
        /// D-002 en el reposo de la tropa, ya en juego (D-011, punto 3): seleccionar y verter no cambian nada de la tropa en el
        /// fotograma del toque, y fotogramas después la tropa está exactamente igual (posición, alfa y pose de cada pieza) que
        /// en una partida sin toques en el mismo instante de la intro; después se va sola, a su hora. Antes la prueba acababa
        /// con «la tropa ya salió» a T + 360 ms; ahora a esa hora reposa.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectingOrPouring_ChangesNothingOfTheTroop()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;

            // Sin toques: la tropa en el reposo, a T + 900 ms.
            flow.Load(1);
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Branch));
            PlayModeHelpers.TickExact(boot, 1400 + 900);
            float quietT = view.Intro.TimeMs;
            Assert.That(quietT, Is.LessThan(view.Intro.Plan.ExitStartMs), "en el reposo, antes de la salida");
            List<Vector3> quiet = TroopPieces(view);

            // Con toques en el reposo.
            flow.Load(1);
            PlayModeHelpers.TickExact(boot, 1400 + 60); // fin natural; la tropa reposa en la rama
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.Troop.Active, Is.True);
            Vector2[] before = TroopPositions(view);
            float[] alpha = TroopAlphas(view);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(TroopPositions(view), Is.EqualTo(before), "seleccionar no cambia nada de la tropa (D-002)");
            Assert.That(TroopAlphas(view), Is.EqualTo(alpha));
            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(TroopPositions(view), Is.EqualTo(before), "verter no cambia nada de la tropa");
            PlayModeHelpers.TickExact(boot, 840);
            Assert.That(view.Intro.TimeMs, Is.EqualTo(quietT).Within(0.01f));
            AssertSamePieces(TroopPieces(view), quiet, "en el reposo, la tropa está igual que sin toques (D-002)");
            Assert.That(view.Troop.Active, Is.True, "sigue reposando");
            PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(view.Intro.TroopGoneMs - view.Intro.TimeMs) + 1);
            Assert.That(view.Troop.Active, Is.False, "y se va sola, a su hora");
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

        // Rutas de la intro para el presupuesto de 0 B por fotograma (guion §13 y A10): especie y modelo, versión, salto a mitad
        // y Reducir movimiento.
        private static readonly string[] AllocationCases =
        {
            "rama_fila_l7_larga", "rama_fila_l7_corta", "dosel_16_larga", "reintento_dosel_l7", "cuelga_lianas_13",
            "cuelga_abraza_82", "sacude_142", "pareja_202_larga", "pareja_202_corta", "salta_bigote_302", "noche_702",
            "salto_a_mitad_l7", "reducir_movimiento_l7",
        };

        /// <summary>
        /// A10: 0 B asignados por fotograma tras <see cref="IntroAnimation.Start"/> en todas las rutas de la intro (y en el
        /// salto, también el fotograma del toque), con una pasada completa de calentamiento antes de medir.
        /// </summary>
        [UnityTest]
        public IEnumerator IntroTick_AllocatesNothing_AfterStart([ValueSource(nameof(AllocationCases))] string route)
        {
            string pack = string.Empty;
            int level = 7;
            int duration = 1400;
            bool retry = false;
            bool reduce = false;
            int skipAt = -1;
            switch (route)
            {
                case "rama_fila_l7_corta":
                    duration = 800;
                    break;
                case "dosel_16_larga":
                    pack = PlayModeHelpers.SixteenCuiaPack();
                    level = 1;
                    BoardLayout.TroopBandOffForTests = true; // 16 cuias sin la franja de la tropa (D-046): el dosel
                    break;
                case "reintento_dosel_l7":
                    duration = 800;
                    retry = true;
                    break;
                case "cuelga_lianas_13":
                    pack = PlayModeHelpers.Level7BoardPack(2, "w02_tapajos", "clear", 13);
                    level = 13;
                    break;
                case "cuelga_abraza_82":
                    pack = PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82);
                    level = 82;
                    break;
                case "sacude_142":
                    pack = PlayModeHelpers.Level7BoardPack(5, "w05_loreto", "white", 142);
                    level = 142;
                    break;
                case "pareja_202_larga":
                    pack = PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202);
                    level = 202;
                    break;
                case "pareja_202_corta":
                    pack = PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202);
                    level = 202;
                    duration = 800;
                    break;
                case "salta_bigote_302":
                    pack = PlayModeHelpers.Level7BoardPack(7, "w07_madre_de_dios", "white", 302);
                    level = 302;
                    duration = 800;
                    break;
                case "noche_702":
                    pack = PlayModeHelpers.Level7BoardPack(10, "w10_alto_negro", "black", 702);
                    level = 702;
                    duration = 800;
                    break;
                case "salto_a_mitad_l7":
                    skipAt = 700;
                    break;
                case "reducir_movimiento_l7":
                    reduce = true;
                    duration = GameConfig.ReducedMotionIntroMs;
                    break;
            }

            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp: cada ruta, la que dice su nombre
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
            boot.Flow!.ReduceMotion = reduce;
            IntroAnimation intro = view.Intro;

            // Calentamiento: una pasada completa por la misma ruta (primer uso de cada gráfico, de cada pieza y del salto).
            view.DebugReplayIntro(duration, retry);
            RunRoute(intro, skipAt, null);

            view.DebugReplayIntro(duration, retry);
            AssertRouteDescribed(view, route, duration > GameConfig.AnimTintDurationMsLate && !retry);
            float tailEnd = intro.TroopGoneMs;
            int frames = RunRoute(intro, skipAt, (t, step) => Assert.That(step, Is.Not.AllocatingGCMemory(), route + ", t = " + t));
            Assert.That(frames, Is.GreaterThan(10), route + ": fotogramas medidos");
            if (skipAt < 0 && tailEnd > 0f)
            {
                Assert.That(frames * 16f, Is.GreaterThanOrEqualTo(tailEnd - 16f), route + ": también el reposo y la salida de la tropa (D-011)");
            }
            Assert.That(intro.Active, Is.False, route + ": la intro y sus restos terminan");
            boot.Flow.ReduceMotion = false;
        }

        /// <summary>
        /// A10 también en la primera intro tras montar un tablero (sin pasada de calentamiento): al arrancar, todos los gráficos
        /// ocultos de la intro y de la tropa quedan registrados en su Canvas, así que mostrarlos en mitad de la intro no hace
        /// crecer el registro de UGUI (reordenar el tablero o activar la capa los desregistra).
        /// </summary>
        [UnityTest]
        public IEnumerator IntroStart_RegistersEveryHiddenPoolGraphic_InItsCanvas()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            foreach (string pack in new[] { string.Empty, PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82) })
            {
                LaunchOptions.Reset();
                int level = pack.Length > 0 ? 82 : 7;
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
                Assert.That(view.Intro.Running, Is.True, "la intro de la carga, recién arrancada");
                var graphics = new List<Graphic>();
                view.Intro.CollectGraphics(graphics);
                int hidden = 0;
                foreach (Graphic g in graphics)
                {
                    if (!g.enabled && g.gameObject.activeInHierarchy)
                    {
                        hidden++;
                        Assert.That(GraphicPool.IsRegistered(g), Is.True, g.name + " (nivel " + level + "): oculto pero registrado en su Canvas");
                    }
                }

                Assert.That(hidden, Is.GreaterThan(80), "los pools de la intro, ocultos al arrancar");
            }
        }

        // Avanza la intro a 16 ms por fotograma hasta que no quede nada (con el salto en skipAt, si lo hay) y, con measure,
        // mide cada fotograma (y el del toque). Devuelve los fotogramas.
        private static int RunRoute(IntroAnimation intro, int skipAt, System.Action<int, TestDelegate>? measure)
        {
            int frames = 0;
            for (int t = 0; intro.Active && t < 4000; t += 16)
            {
                TestDelegate step;
                if (skipAt >= 0 && t == skipAt - (skipAt % 16))
                {
                    step = () =>
                    {
                        intro.Complete(true);
                        intro.Tick(16f);
                    };
                }
                else
                {
                    step = () => intro.Tick(16f);
                }

                if (measure != null)
                {
                    measure(t, step);
                }
                else
                {
                    step();
                }

                frames++;
            }

            return frames;
        }

        // Cada ruta es la que dice su nombre (si no, el test no prueba lo que promete).
        private static void AssertRouteDescribed(GameScreen view, string route, bool expectLong)
        {
            IntroChoreography p = view.Intro.Plan;
            bool canopy = route.StartsWith("dosel", System.StringComparison.Ordinal) || route.StartsWith("reintento", System.StringComparison.Ordinal);
            Assert.That(p.Perch, Is.EqualTo(canopy ? IntroPerch.Canopy : IntroPerch.Branch), route);
            Assert.That(view.Intro.ReduceMotion, Is.EqualTo(route.StartsWith("reducir", System.StringComparison.Ordinal)), route);
            if (route.StartsWith("dosel", System.StringComparison.Ordinal))
            {
                Assert.That(p.FruitPerCuia, Is.True, route + ": una fruta por cuia");
            }
            else if (route.StartsWith("cuelga", System.StringComparison.Ordinal))
            {
                Assert.That(p.Model, Is.EqualTo(TroopModel.Hang), route);
            }
            else if (route.StartsWith("pareja", System.StringComparison.Ordinal))
            {
                Assert.That(p.Pair, Is.True, route);
            }
            else if (route.StartsWith("sacude", System.StringComparison.Ordinal))
            {
                Assert.That(p.Model, Is.EqualTo(TroopModel.Shake), route);
            }

            if (route.StartsWith("cuelga_abraza", System.StringComparison.Ordinal))
            {
                Assert.That(view.Troop.Look!.HugsFruit, Is.True, route + ": la fruta abrazada");
            }

            if (route.StartsWith("noche", System.StringComparison.Ordinal))
            {
                Assert.That(view.Troop.NightVisible, Is.True, route + ": el velo nocturno");
            }

            if (route.StartsWith("salta_bigote", System.StringComparison.Ordinal))
            {
                Assert.That(view.Troop.Look!.Whiskers, Is.Not.Null, route + ": el bigote");
            }

            Assert.That(p.IsLong, Is.EqualTo(expectLong), route + ": versión");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_FadesIn250ms_WithAStillMonkey()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // DC3: sin pantalla simulada, el Editor da un lienzo de 853 × 640 dp
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

            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Branch), "nivel 7, primer intento: modo rama");
            Assert.That(visible, Is.EqualTo(1), "un solo miquito, quieto (Q13 A)");
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
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // DC3: sin pantalla simulada, el Editor da un lienzo de 853 × 640 dp
            yield return PlayModeHelpers.LoadGame(4, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            AssertNoSightingCard(boot, "nivel 4: Fruta nueva (guanábana), sin tarjeta");

            boot.Flow!.Load(5);
            boot.TapAnywhere(); // también tras un salto: la tarjeta llega después de la intro
            GameScreen view = boot.View!;
            SightingCard card = view.Sighting;

            // D-011, punto 3: la tarjeta espera a que la tropa se vaya (tras un salto, su fundido de 100 ms) y no la tapa.
            Assert.That(card.Visible, Is.False, "la tropa aún se funde en la rama");
            Assert.That(view.SightingPending, Is.True);
            Assert.That(view.Intro.TroopVisible, Is.True);
            PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(view.Intro.TroopGoneMs - view.Intro.TimeMs) + 1);
            Assert.That(view.Intro.TroopVisible, Is.False);
            PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            Assert.That(card.Visible, Is.True, "primer nivel libre desde el nivel_tarjeta del mono ardilla, ya sin la tropa");
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

            // D-011, punto 2: dura lo que se tarda en leer el título, el nombre y el dato (unos 5 s), no 2,5 s.
            int readMs = NoticeTiming.ReadMs(UiText.FaunaSightedCard, card.Name, card.Fact);
            Assert.That(card.DurationMs, Is.EqualTo(readMs));
            Assert.That(readMs, Is.EqualTo(4960), "20 + 12 + 40 caracteres");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected), "la tarjeta no captura toques");
            boot.TapBackground();
            PlayModeHelpers.TickExact(boot, 4500);
            Assert.That(card.Visible, Is.True, "a los 4,5 s aún se lee");
            PlayModeHelpers.TickExact(boot, readMs - 4500);
            Assert.That(card.Visible, Is.False, "y se desvanece a su hora");

            // En el reintento no vuelve a salir.
            PlayModeHelpers.StepUntilIdle(boot, 1000);
            Assert.That(boot.PressRestart(), Is.True);
            boot.TapAnywhere();
            AssertNoSightingCard(boot, "en el reintento no vuelve a salir");
        }

        /// <summary>
        /// El labio del río que desborda (revisión: no se veía de dónde salía el agua): aparece con la lámina, del mismo ancho
        /// en cada instante, desde 14 dp sobre el panel, y se va con la madera. Orden de capas (guion §4.6.5; revisión del
        /// 2026-10-09): labio &lt; rama &lt; tropa, con el labio al 70 % en la franja de la rama y pleno en la cresta.
        /// </summary>
        [UnityTest]
        public IEnumerator RiverLip_GrowsWithTheSheet_FromTheBank()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(1400, false);
            IntroAnimation intro = view.Intro;
            IntroChoreography p = intro.Plan;
            intro.Seek(p.SheetStartMs - 1f);
            Assert.That(intro.LipVisible, Is.False, "nada antes del desborde");
            foreach (float dt in new[] { 20f, 60f, 150f })
            {
                intro.Seek(p.SheetStartMs + dt);
                float half = Mathf.Min(p.SheetHalfWidth, dt * p.SheetSpeed);
                Assert.That(intro.LipVisible, Is.True);
                Assert.That(intro.LipHalfWidth, Is.EqualTo(half).Within(0.5f), "el labio, tan ancho como la lámina a " + dt + " ms");
            }

            // Al 70 % en la franja de la rama (de panel.top − 11 a panel.top − 5: del borde plano, en panel.top + 2, son 7-13
            // dp) y pleno en la cresta (panel.top − 14).
            WaterStripGraphic lip = intro.LipGraphic!;
            float below = IntroAnimation.LipBelowPanelDp;
            Assert.That(lip.BodyAlphaAt(below + 5f), Is.EqualTo(IntroAnimation.LipBranchBandAlpha).Within(0.001f), "panel.top − 5");
            Assert.That(lip.BodyAlphaAt(below + 8f), Is.EqualTo(IntroAnimation.LipBranchBandAlpha).Within(0.001f), "panel.top − 8");
            Assert.That(lip.BodyAlphaAt(below + IntroAnimation.LipBranchBandTopDp), Is.EqualTo(IntroAnimation.LipBranchBandAlpha).Within(0.001f), "panel.top − 11");
            Assert.That(lip.BodyAlphaAt(below + IntroAnimation.LipAbovePanelDp), Is.EqualTo(1f).Within(0.001f), "cresta, pleno");

            intro.Seek(p.DrainEndMs + 1f);
            Assert.That(intro.LipVisible, Is.False, "la madera ya está seca");
            Assert.That(view.Troop.WaterSlot.IsChildOf(view.Troop.Layer), Is.True, "en la capa de la tropa");
            int water = view.Troop.WaterSlot.GetSiblingIndex();
            Assert.That(water, Is.LessThan(view.Troop.Branch.transform.GetSiblingIndex()), "labio < rama: la rama sobre la crecida");
            Assert.That(view.Troop.Branch.transform.GetSiblingIndex(), Is.LessThan(view.Troop.Rig(0).Root.GetSiblingIndex()), "rama < tropa");
        }

        /// <summary>
        /// A7 ampliada (revisión del 2026-10-09: «monos de pie sobre el río»): mientras el río desborda (300-850 ms en la
        /// larga y 0-550 ms en la corta), al menos el 80 % de la rama bajo cada miquito se sigue viendo: ninguna agua de la
        /// intro dibujada por delante de la rama (el labio, la lámina, las tiras, los chorritos) la tapa. Se muestrea la rama
        /// de 6 dp cada 1 dp bajo el cuerpo de cada miquito (± 0,4 S) y se contrasta con las cajas de esa agua, en el orden de
        /// dibujo real (recorrido de la jerarquía del lienzo). Con 3 y 7 cuias y las especies de cada modelo, en 20:9 y 16:9.
        /// </summary>
        [UnityTest]
        public IEnumerator Branch_StaysVisibleUnderEachMonkey_WhileTheRiverOverflows([Values(2340, 1920)] int screenHeightPx)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, screenHeightPx, 96f, 48f);
            var cases = new (string Pack, int Level)[]
            {
                (string.Empty, 1), (string.Empty, 7),
                (PlayModeHelpers.Level7BoardPack(2, "w02_tapajos", "clear", 13), 13),
                (PlayModeHelpers.Level7BoardPack(5, "w05_loreto", "white", 142), 142),
                (PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202), 202),
                (PlayModeHelpers.Level7BoardPack(7, "w07_madre_de_dios", "white", 302), 302),
                (PlayModeHelpers.Level7BoardPack(12, "w12_orinoco", "clear", 922), 922),
            };
            int measured = 0;
            int lipFrames = 0;
            var quads = new List<Vector2[]>();
            var all = new List<Graphic>();
            var troop = new List<Graphic>();
            var troopSet = new HashSet<Graphic>(); // Contains en O(1): con la lista, cada instante miraba cada gráfico contra toda la tropa
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
                RectTransform canvas = (RectTransform)view.Canvas.transform;
                foreach (int duration in new[] { 1400, 800 })
                {
                    view.DebugReplayIntro(duration, false);
                    IntroChoreography plan = view.Intro.Plan;
                    if (screenHeightPx == 1920 && view.Intro.Input.PanelTop - view.Intro.Input.HudBottom < IntroChoreography.BranchMinBandDp)
                    {
                        // 16:9 (360 × 640 dp desde DC3): sin la franja de la rama, el dosel (D-010; 02 §5): no hay rama que medir.
                        Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Canopy), "nivel " + level + " en 16:9 sin franja");
                        continue;
                    }

                    Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), "nivel " + level);
                    Dictionary<Transform, int> order = RenderOrder(canvas);
                    int branchOrder = order[view.Troop.Branch.transform];
                    all.Clear();
                    troop.Clear();
                    view.Intro.CollectGraphics(all);
                    view.Troop.CollectGraphics(troop);
                    troopSet.Clear();
                    troopSet.UnionWith(troop);
                    int from = duration == 1400 ? 300 : 0;
                    int to = duration == 1400 ? 850 : 550;
                    for (int t = from; t <= to; t += 25)
                    {
                        view.Intro.Seek(t);
                        lipFrames += view.Intro.LipVisible ? 1 : 0;
                        quads.Clear();
                        foreach (Graphic g in all)
                        {
                            bool water = !troopSet.Contains(g) && (g is WaterStripGraphic || g is StreamGraphic || g.transform.IsChildOf(view.Troop.WaterSlot));
                            if (water && Shown(g) && order[g.transform] > branchOrder)
                            {
                                quads.Add(CanvasQuad(g, canvas));
                            }
                        }

                        for (int m = 0; m < plan.MonkeyCount; m++)
                        {
                            MonkeyRig rig = view.Troop.Rig(m);
                            if (!rig.Visible || rig.Alpha <= 0.01f)
                            {
                                continue;
                            }

                            float x0 = Mathf.Max(0f, rig.Root.anchoredPosition.x - (0.4f * rig.Size));
                            float x1 = Mathf.Min(view.CanvasWidth, rig.Root.anchoredPosition.x + (0.4f * rig.Size));
                            int total = 0;
                            int hidden = 0;
                            for (float x = x0; x <= x1; x += 1f)
                            {
                                float yc = plan.BranchCurveY(x);
                                for (float dy = -2.5f; dy <= 2.51f; dy += 1f)
                                {
                                    var sample = new Vector2(x, yc + dy);
                                    total++;
                                    foreach (Vector2[] q in quads)
                                    {
                                        if (Inside(q, sample))
                                        {
                                            hidden++;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (total == 0)
                            {
                                continue; // aún en el follaje, fuera de la pantalla
                            }

                            measured++;
                            float visible = (total - hidden) / (float)total;
                            Assert.That(visible, Is.GreaterThanOrEqualTo(0.8f), "rama bajo el miquito " + m + ": nivel " + level + ", T " + duration + ", t " + t);
                        }
                    }
                }
            }

            Assert.That(measured, Is.GreaterThan(screenHeightPx == 2340 ? 100 : 40), "miquitos medidos sobre la rama (en 16:9, solo los tableros con franja)");
            Assert.That(lipFrames, Is.GreaterThan(20), "con el labio del río a la vista");
        }

        /// <summary>
        /// A14 sin dispositivo gráfico: la onda del menisco depende solo del tiempo de la intro. Se toma el mismo instante en dos
        /// pasadas independientes, con otras intros, fotogramas reales y un reintento por medio, en instantes con la onda activa
        /// (300 ms: llenándose; 1.100 ms: impactos de fruta), y el estado de la onda de cada cuia sale idéntico. Un fotograma
        /// del juego entre medias no la mueve.
        /// </summary>
        [UnityTest]
        public IEnumerator MeniscusRipple_DependsOnlyOnTheIntroTime()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere(); // la intro del flujo, terminada: el resto lo lleva el enganche de depuración
            PlayModeHelpers.StepUntilIdle(boot, 1000);
            GameScreen view = boot.View!;
            int[] instants = { 300, 1100 };
            var first = new float[instants.Length][];
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1)
                {
                    // Otra historia: intros de otra duración, un reintento y fotogramas reales por medio.
                    view.DebugReplayIntro(800, false);
                    for (int i = 0; i < 30; i++)
                    {
                        view.Tick(16f);
                    }

                    view.DebugReplayIntro(800, true);
                    view.Intro.Seek(500);
                    view.DebugReplayIntro(1400, false);
                    for (int i = 0; i < 70; i++)
                    {
                        view.Tick(16f);
                    }
                }

                view.DebugReplayIntro(1400, false);
                for (int s = 0; s < instants.Length; s++)
                {
                    view.Intro.Seek(instants[s]);
                    float[] state = RippleStates(view);
                    for (int i = 0; i < view.Views.Length; i++)
                    {
                        view.Views[i].Tick(16f); // un fotograma de la cuia no avanza la onda que lleva la intro
                    }

                    Assert.That(RippleStates(view), Is.EqualTo(state), "la cuia no acumula la onda a " + instants[s] + " ms");
                    int rippling = 0;
                    for (int i = 0; i < view.Views.Length; i++)
                    {
                        rippling += view.Views[i].Rippling ? 1 : 0;
                    }

                    Assert.That(rippling, Is.GreaterThan(0), "onda activa a " + instants[s] + " ms");
                    if (pass == 0)
                    {
                        first[s] = state;
                    }
                    else
                    {
                        Assert.That(state, Is.EqualTo(first[s]), "mismo instante, dos pasadas: " + instants[s] + " ms");
                    }
                }
            }
        }

        /// <summary>
        /// El enganche de depuración no deja un fotograma imposible: tras el fin de la intro (las vistas ya desbloqueadas y con
        /// su contenido final), buscar un instante anterior a T se rechaza; hacia la cola, sí.
        /// </summary>
        [UnityTest]
        public IEnumerator Seek_BeforeTAfterTheIntroEnded_IsRejected()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(1400, false);
            IntroAnimation intro = view.Intro;
            intro.Seek(1100);
            intro.Seek(300); // mientras corre, hacia atrás también
            Assert.That(intro.Running, Is.True);
            intro.Seek(1400);
            Assert.That(intro.Running, Is.False, "T: input habilitado");
            Assert.Throws<System.InvalidOperationException>(() => intro.Seek(300), "antes de T, ya terminada");
            Assert.That(view.Views[0].Locked, Is.False, "el intento no vuelve a bloquear las vistas");
            intro.Seek(1500); // la cola, sí
            Assert.That(intro.Active, Is.True);
        }

        /// <summary>
        /// Guion §4.3 y §11: en un build de release sin el arte final de la especie, ni la tropa sale en la intro (dosel) ni la
        /// tarjeta «¡Miquito a la vista!» con el miquito provisional por código. Con la misma regla (<c>FaunaArtAvailable</c>), en
        /// desarrollo sí salen las dos.
        /// </summary>
        [UnityTest]
        public IEnumerator WithoutFinalArt_InARelease_NeitherTheTroopNorTheCardAppear()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            LaunchOptions.DevelopmentBuildOverride = false;
            yield return PlayModeHelpers.LoadGame(4, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 1000);

            boot.Flow!.Load(5); // el nivel de la tarjeta del mono ardilla
            AF.Levels.FaunaSpecies squirrel = view.Fauna!.ForLevel(5)!;
            Assert.That(GameScreen.FaunaArtAvailable(squirrel, false), Is.False, "release sin fauna_saimiri_collinsi_partes");
            Assert.That(GameScreen.FaunaArtAvailable(squirrel, true), Is.False);
            Assert.That(view.IntroSpecies, Is.Null, "sin arte: dosel");
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Canopy));
            Assert.That(view.Troop.Active, Is.False, "sin tropa");
            for (int t = 0; t < 1400; t += 100)
            {
                PlayModeHelpers.TickExact(boot, 100);
                Assert.That(view.Troop.Active, Is.False, "sin tropa a " + t + " ms");
            }

            PlayModeHelpers.TickExact(boot, 400);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            AssertNoSightingCard(boot, "sin tarjeta con el miquito provisional");

            // La misma regla en desarrollo: la tropa provisional y la tarjeta.
            LaunchOptions.DevelopmentBuildOverride = true;
            boot.Flow.Load(5);
            Assert.That(view.IntroSpecies, Is.Not.Null);
            Assert.That(view.Troop.Active, Is.True);
            boot.TapAnywhere();
            Assert.That(view.SightingPending, Is.True, "la tarjeta espera a que la tropa se funda (D-011, punto 3)");
            PlayModeHelpers.TickExact(boot, 100 + PlayModeHelpers.FrameMs);
            Assert.That(view.Sighting.Visible, Is.True);
        }

        /// <summary>
        /// Presupuesto del guion §13 con contador, fotograma a fotograma: ≤ 8 brillos aditivos de la intro (espuma y anillos) y
        /// ≤ 20 partículas (gotas, penachos y espuma del labio); con el ambiente, ≤ 60 (07: gama baja). Larga y corta, con 15 y
        /// 24 rachas.
        /// </summary>
        [UnityTest]
        public IEnumerator Intro_StaysWithinTheGlowAndParticleBudget()
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai" };
            var board = new int[10][];
            for (int i = 0; i < 10; i++)
            {
                board[i] = i < 8 ? new[] { i + 1, i + 1, ((i + 1) % 8) + 1, ((i + 2) % 8) + 1 } : new int[0]; // 8 × 3 rachas = 24
            }

            var cases = new (string Pack, int Level, int Runs)[]
            {
                (string.Empty, 7, 15), (PlayModeHelpers.BoardPack(1, "w01_estuario", "white", 9, ids, board), 9, 24),
            };
            foreach ((string pack, int level, int runs) in cases)
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
                    Assert.That(view.Intro.Plan.RunCount, Is.EqualTo(runs));
                    int maxAdditive = 0;
                    int maxParticles = 0;
                    for (int t = 0; t <= duration + 300; t += 4)
                    {
                        view.Intro.Seek(t);
                        view.Intro.CountVisible(out int additive, out int particles);
                        maxAdditive = Mathf.Max(maxAdditive, additive);
                        maxParticles = Mathf.Max(maxParticles, particles);
                        string where = runs + " rachas, T " + duration + ", t " + t;
                        Assert.That(additive, Is.LessThanOrEqualTo(IntroAnimation.MaxAdditive), "≤ 8 brillos aditivos: " + where);
                        Assert.That(particles, Is.LessThanOrEqualTo(IntroAnimation.MaxParticles), "≤ 20 partículas: " + where);
                        Assert.That(particles + view.Ambient.ActiveCount, Is.LessThanOrEqualTo(60), "≤ 60 con el ambiente: " + where);
                    }

                    Assert.That(maxAdditive, Is.GreaterThan(0), "hay brillos que contar");
                    Assert.That(maxParticles, Is.GreaterThan(0), "hay partículas que contar");
                }
            }
        }

        /// <summary>
        /// Capturas deterministas (A14): con el ambiente congelado, el reloj de los shaders del líquido y del chorro es el
        /// tiempo de la intro (no el del motor) y no avanza entre fotogramas; al descongelar vuelve el del motor.
        /// </summary>
        [UnityTest]
        public IEnumerator FrozenAmbient_PinsTheShaderClockToTheIntroTime()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            AmbientLayer.Frozen = true;
            boot.View!.DebugReplayIntro(1400, false);
            boot.View.Intro.Seek(300);
            ShaderClock.ReadGlobals(out float seconds, out float frozen);
            Assert.That(frozen, Is.EqualTo(1f));
            Assert.That(seconds, Is.EqualTo(0.3f).Within(0.0001f));
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            ShaderClock.ReadGlobals(out seconds, out frozen);
            Assert.That(seconds, Is.EqualTo(0.3f).Within(0.0001f), "no avanza con los fotogramas");
            boot.View.Intro.Seek(1100);
            ShaderClock.ReadGlobals(out seconds, out _);
            Assert.That(seconds, Is.EqualTo(1.1f).Within(0.0001f));
            AmbientLayer.Frozen = false;
            ShaderClock.ReadGlobals(out _, out frozen);
            Assert.That(frozen, Is.EqualTo(0f), "fuera de las capturas, el reloj del motor");
        }

        /// <summary>
        /// <c>af_anim_skipped</c> en la variante aliviada: si el salto llega durante el cartel «El río bajó», la intro aún no ha
        /// empezado: at_ms = 0 (no el tiempo de la intro anterior) y duration_cfg_ms es la duración de la animación (800), sin
        /// el cartel.
        /// </summary>
        [UnityTest]
        public IEnumerator SkipDuringTheRiverLoweredBanner_ReportsAt0_AndTheAnimationDuration()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, 500); // una intro anterior a medias: TimeMs = 500
            Assert.That(view.Intro.TimeMs, Is.GreaterThan(0f));
            view.OnRiverLowered("plus_empty", 2000);
            view.OnIntroStarted(2000 + GameConfig.AnimTintDurationMsLate, true);
            view.OnIntroFinished(true);
            Assert.That(view.LastAnimSkipAtMs, Is.EqualTo(0), "at_ms = 0: la intro no había empezado");
            Assert.That(view.LastAnimSkipCfgMs, Is.EqualTo(GameConfig.AnimTintDurationMsLate), "duration_cfg_ms sin el cartel");

            view.OnIntroStarted(GameConfig.AnimTintDurationMsLate, true); // reintento normal: la intro empieza ya
            view.Intro.Seek(300);
            view.OnIntroFinished(true);
            Assert.That(view.LastAnimSkipAtMs, Is.EqualTo(300));
            Assert.That(view.LastAnimSkipCfgMs, Is.EqualTo(GameConfig.AnimTintDurationMsLate));
        }

        /// <summary>
        /// Salida de la tropa (revisión: parecían monos fantasma a media salida): trepan hacia el follaje de la esquina más
        /// cercana, con alfa 1 hasta los últimos 80 ms, y la capa de la tropa está detrás del follaje de las esquinas. En un
        /// teléfono (360 dp) llegan a su esquina y trepan junto a ella; en un lienzo ancho (el de 853 dp del Editor sin
        /// pantalla simulada, como una tableta), a 450 dp/s no llegan en los 500 ms de la salida y trepan igual mientras se
        /// funden (revisión del 2026-10-11: antes salían andando en plano). En la rama baja, un manojo de hojas en cada extremo,
        /// por delante de la tropa, de la rama hacia arriba y sin pasar sobre el HUD.
        /// </summary>
        [UnityTest]
        public IEnumerator TroopExit_ClimbsToTheCornerFoliage_OpaqueUntilTheLast80ms([Values(false, true)] bool wide)
        {
            GameScreen.SimulatedScreen = wide ? (Vector4?)null : new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(1400, false);
            IntroAnimation intro = view.Intro;
            IntroChoreography plan = intro.Plan;
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch));
            float w = view.CanvasWidth;
            Assert.That(w, wide ? (NUnit.Framework.Constraints.IResolveConstraint)Is.GreaterThan(600f) : Is.EqualTo(360f).Within(0.5f), "lienzo");
            int short_ = 0;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float end = plan.MonkeyX(m, plan.ExitStartMs + IntroChoreography.ExitMs);
                short_ += end > 0f && end < w ? 1 : 0;
            }

            Assert.That(short_ > 0, Is.EqualTo(wide), wide ? "en el lienzo ancho alguno no llega a su esquina" : "en el teléfono todos llegan a su esquina");
            float exit = plan.ExitStartMs;
            intro.Seek(exit);
            var y0 = new float[plan.MonkeyCount];
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                y0[m] = view.Troop.Rig(m).Root.anchoredPosition.y;
            }

            intro.Seek(exit + IntroChoreography.ExitMs - IntroChoreography.ExitFadeMs - 10f);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                MonkeyRig rig = view.Troop.Rig(m);
                Assert.That(rig.Alpha, Is.EqualTo(1f).Within(0.001f), "miquito " + m + ": opaco hasta los últimos 80 ms");
                Assert.That(rig.Root.anchoredPosition.y, Is.GreaterThan(y0[m] + 5f), "miquito " + m + ": trepa hacia el follaje");
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, rig.Root.localEulerAngles.z)), Is.GreaterThan(5f), "miquito " + m + ": inclinado, trepando");
            }

            intro.Seek(exit + IntroChoreography.ExitMs - 40f);
            Assert.That(view.Troop.Rig(0).Alpha, Is.InRange(0.01f, 0.99f), "se funde en los últimos 80 ms");
            Transform scene = view.Troop.Layer.parent;
            Transform foliage = scene.Find("FoliageTL");
            Assert.That(foliage, Is.Not.Null);
            Assert.That(view.Troop.Layer.GetSiblingIndex(), Is.LessThan(foliage.GetSiblingIndex()), "la tropa, por detrás del follaje");

            // Manojos de hojas de los extremos de la rama baja: a la vista, por delante de los miquitos, en x = 0 y x = ancho.
            float hudBottom = intro.Input.HudBottom;
            foreach (string name in new[] { "BranchEndLeft", "BranchEndRight" })
            {
                Transform end = view.Troop.Layer.Find(name);
                Assert.That(end, Is.Not.Null, name);
                Image img = end.GetComponent<Image>();
                Assert.That(img.enabled, Is.True, name + ": en la rama baja");
                Assert.That(img.raycastTarget, Is.False);
                Assert.That(end.GetSiblingIndex(), Is.GreaterThan(view.Troop.Rig(0).Root.GetSiblingIndex()), name + ": por delante de la tropa");
                var rt = (RectTransform)end;
                Assert.That(rt.anchoredPosition.x, Is.EqualTo(name == "BranchEndLeft" ? 0f : w).Within(0.01f), name + ": en el borde");
                float bottom = -rt.anchoredPosition.y;
                Assert.That(bottom, Is.EqualTo(plan.BranchCurveY(rt.anchoredPosition.x) + TroopLayer.BranchEndBelowDp).Within(0.01f), name + ": la rama sale de él");
                Assert.That(bottom - rt.sizeDelta.y, Is.GreaterThanOrEqualTo(hudBottom), name + ": sin pasar sobre el HUD");
                Assert.That(rt.sizeDelta.y, Is.GreaterThan(1.1f * plan.BodyDp), name + ": tapa al miquito que trepa");
            }
        }

        /// <summary>
        /// Rasgos de especie que hacen algo en la intro (revisión, 22_species_*): la pareja del cotoncillo lado a lado y mirando
        /// al mismo sitio, con las colas colgando bajo la rama y trenzadas dos vueltas (4 cruces), sin entrar en el panel; el
        /// velo nocturno del mono nocturno; y nada de eso en el mono ardilla.
        /// </summary>
        [UnityTest]
        public IEnumerator SpeciesTraits_PairAndNight()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202), 202, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(800, false);
            IntroChoreography p = boot.View.Intro.Plan;
            Assert.That(p.Pair, Is.True, "cotoncillo: la pareja");
            Assert.That(p.MonkeySeatX(1) - p.MonkeySeatX(0), Is.EqualTo(IntroChoreography.PairGapS * p.BodyDp).Within(0.01f));
            Assert.That(boot.View.Troop.NightVisible, Is.False);

            // Sentados juntos, ya en el reposo (desde D-011 la corta también entra por la rama: en t = 300 aún llegan).
            boot.View.Intro.Seek(800 + 300);
            TroopLayer troop = boot.View.Troop;
            Assert.That(troop.Rig(0).Root.localScale.x, Is.EqualTo(troop.Rig(1).Root.localScale.x), "mirando al mismo sitio");
            int crossings = 0;
            float prev = 0f;
            for (int i = 0; i <= 400; i++)
            {
                float t = i / 400f;
                Assert.That(troop.Rig(0).Tail.Waving && troop.Rig(1).Tail.Waving, Is.True, "las dos colas, trenzadas");
                float d = troop.Rig(0).Tail.WaveOffset(t) - troop.Rig(1).Tail.WaveOffset(t);
                crossings += i > 0 && Mathf.Sign(d) != Mathf.Sign(prev) && Mathf.Abs(d) > 1e-4f ? 1 : 0;
                prev = Mathf.Abs(d) > 1e-4f ? d : prev;
            }

            Assert.That(crossings, Is.EqualTo(4), "dos vueltas: 4 cruces");
            RectTransform canvas = (RectTransform)boot.View.Canvas.transform;
            float panelTop = boot.View.Intro.Input.PanelTop;
            for (int m = 0; m < 2; m++)
            {
                BezierStripGraphic tail = troop.Rig(m).Tail;
                Vector3 l = canvas.InverseTransformPoint(tail.rectTransform.TransformPoint(tail.PointAt(1f)));
                Vector2 tip = new Vector2(l.x - canvas.rect.xMin, canvas.rect.yMax - l.y);
                Assert.That(tip.y, Is.GreaterThan(p.BranchCurveY(tip.x) + (0.5f * p.PairTailHangDp)), "la cola " + m + " cuelga bajo la rama");
                Assert.That(tip.y, Is.LessThanOrEqualTo(panelTop - IntroChoreography.PairTailClearDp + 1f), "sin entrar en el panel");
            }

            LaunchOptions.Reset();
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.Level7BoardPack(10, "w10_alto_negro", "black", 702), 702, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(800, false);
            Assert.That(boot.View.IntroSpecies!.Id, Is.EqualTo("aotus_trivirgatus"));
            Assert.That(boot.View.Troop.NightVisible, Is.True, "mono nocturno: velo de noche sobre la franja");
            Assert.That(boot.View.Troop.NightVisible, Is.True, "con la tropa en la rama");
            boot.View.Intro.Seek(800 + 600);
            Assert.That(boot.View.Troop.NightVisible, Is.True, "el velo sigue mientras la tropa reposa en juego (D-011)");
            boot.View.Intro.Seek(boot.View.Intro.Plan.ExitStartMs + IntroChoreography.ExitMs + 10);
            Assert.That(boot.View.Troop.NightVisible, Is.False, "el velo se va con la tropa");
        }

        // Orden de dibujo de cada Transform del lienzo: recorrido en profundidad de la jerarquía (los Canvas anidados sin orden
        // propio se dibujan en ese orden dentro de su padre).
        private static Dictionary<Transform, int> RenderOrder(Transform root)
        {
            var order = new Dictionary<Transform, int>();
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform t = stack.Pop();
                order[t] = order.Count;
                for (int i = t.childCount - 1; i >= 0; i--)
                {
                    stack.Push(t.GetChild(i));
                }
            }

            return order;
        }

        private static bool Shown(Graphic g)
        {
            return g.enabled && g.gameObject.activeInHierarchy && g.canvasRenderer.GetAlpha() > 0.01f && g.color.a > 0.01f;
        }

        // Caja de un gráfico en dp del lienzo (y desde arriba), como cuadrilátero: las tiras de agua por su malla, los chorritos
        // por su curva (con 2 dp de margen) y las imágenes por su RectTransform (giro incluido).
        private static Vector2[] CanvasQuad(Graphic g, RectTransform canvas)
        {
            var world = new Vector3[4];
            RectTransform rt = g.rectTransform;
            if (g is WaterStripGraphic strip)
            {
                Rect b = strip.LocalBounds();
                world[0] = rt.TransformPoint(new Vector3(b.xMin, b.yMin, 0f));
                world[1] = rt.TransformPoint(new Vector3(b.xMin, b.yMax, 0f));
                world[2] = rt.TransformPoint(new Vector3(b.xMax, b.yMax, 0f));
                world[3] = rt.TransformPoint(new Vector3(b.xMax, b.yMin, 0f));
            }
            else if (g is StreamGraphic stream)
            {
                Vector2 a = stream.CurveStart;
                Vector2 e = stream.CurveEnd;
                var b = Rect.MinMaxRect(Mathf.Min(a.x, e.x) - 2f, Mathf.Min(a.y, e.y) - 2f, Mathf.Max(a.x, e.x) + 2f, Mathf.Max(a.y, e.y) + 2f);
                world[0] = rt.TransformPoint(new Vector3(b.xMin, b.yMin, 0f));
                world[1] = rt.TransformPoint(new Vector3(b.xMin, b.yMax, 0f));
                world[2] = rt.TransformPoint(new Vector3(b.xMax, b.yMax, 0f));
                world[3] = rt.TransformPoint(new Vector3(b.xMax, b.yMin, 0f));
            }
            else
            {
                rt.GetWorldCorners(world);
            }

            Rect area = canvas.rect;
            var quad = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 l = canvas.InverseTransformPoint(world[i]);
                quad[i] = new Vector2(l.x - area.xMin, area.yMax - l.y);
            }

            return quad;
        }

        // Punto dentro de un cuadrilátero convexo (en cualquier sentido de giro); uno sin área no tapa nada.
        private static bool Inside(Vector2[] q, Vector2 p)
        {
            float area = 0f;
            for (int i = 0; i < 4; i++)
            {
                area += (q[i].x * q[(i + 1) % 4].y) - (q[(i + 1) % 4].x * q[i].y);
            }

            if (Mathf.Abs(area) < 1e-3f)
            {
                return false;
            }

            bool pos = false;
            bool neg = false;
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = q[i];
                Vector2 b = q[(i + 1) % 4];
                float cross = ((b.x - a.x) * (p.y - a.y)) - ((b.y - a.y) * (p.x - a.x));
                pos |= cross > 1e-4f;
                neg |= cross < -1e-4f;
                if (pos && neg)
                {
                    return false;
                }
            }

            return true;
        }

        // Edad y fase de la onda de cada cuia.
        private static float[] RippleStates(GameScreen view)
        {
            var state = new float[view.Views.Length * 2];
            for (int i = 0; i < view.Views.Length; i++)
            {
                view.Views[i].RippleState(out float age, out float phase);
                state[2 * i] = age;
                state[(2 * i) + 1] = phase;
            }

            return state;
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

        // Sin tarjeta «¡Miquito a la vista!» en este intento. Desde D-011, punto 3, la tarjeta no sale al habilitarse el toque:
        // queda pendiente hasta que la tropa se va y luego espera su turno en la cola de avisos, así que mirar Sighting.Visible
        // en ese fotograma no prueba nada. Por eso se comprueba que no queda pendiente y que tampoco sale mientras le queda
        // tropa a la vista (más 3 fotogramas) ni hasta que la cola se vacía: una tarjeta encolada habría salido antes.
        private static void AssertNoSightingCard(GameBootstrap boot, string because)
        {
            GameScreen view = boot.View!;
            Assert.That(view.Sighting.Visible, Is.False, because);
            Assert.That(view.SightingPending, Is.False, because + ": ni pendiente");
            float troopLeft = view.Intro.TroopVisible ? view.Intro.TroopGoneMs - view.Intro.TimeMs : 0f;
            int waitMs = Mathf.CeilToInt(Mathf.Max(0f, troopLeft)) + (3 * PlayModeHelpers.FrameMs);
            const int maxMs = 10000;
            int t = 0;
            while (t < maxMs && (t < waitMs || view.NoticesQueued > 0))
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                Assert.That(view.Sighting.Visible, Is.False, because + " (a " + t + " ms)");
            }

            Assert.That(view.SightingPending, Is.False, because + ": tampoco después");
            Assert.That(view.NoticesQueued, Is.EqualTo(0), because + ": nada esperando en la cola");
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

        // Misma pose: posiciones y tamaños a 0,01 dp, giros a 0,2° y alfas a 0,003 (MonkeyRig no reescribe un giro que cambia
        // menos de lo que distingue Quaternion ==, unos 0,16°, ni un alfa que cambia menos de 0,002: depende de los fotogramas).
        private static void AssertSamePieces(List<Vector3> actual, List<Vector3> expected, string what)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count), what);
            for (int i = 0; i < actual.Count; i++)
            {
                bool pose = i % 2 == 0 && i < actual.Count - IntroChoreography.MaxMonkeys;
                bool root = i >= actual.Count - IntroChoreography.MaxMonkeys;
                Assert.That(actual[i].x, Is.EqualTo(expected[i].x).Within(0.01f), what + " [" + i + "].x");
                Assert.That(actual[i].y, Is.EqualTo(expected[i].y).Within(0.01f), what + " [" + i + "].y");
                float tol = pose || root ? 0.2f : 0.003f;
                float dz = pose || root ? Mathf.Abs(Mathf.DeltaAngle(actual[i].z, expected[i].z)) : Mathf.Abs(actual[i].z - expected[i].z);
                Assert.That(dz, Is.LessThanOrEqualTo(tol), what + " [" + i + "].z");
            }
        }

        // Posición, giro y tamaño (x, y, z) de cada pieza de la tropa y su alfa, en orden fijo: la pose entera.
        private static List<Vector3> TroopPieces(GameScreen view)
        {
            var graphics = new List<Graphic>();
            view.Troop.CollectGraphics(graphics);
            var pieces = new List<Vector3>();
            foreach (Graphic g in graphics)
            {
                RectTransform rt = g.rectTransform;
                bool shown = g.enabled && g.gameObject.activeInHierarchy;
                pieces.Add(shown ? (Vector3)rt.anchoredPosition + new Vector3(0f, 0f, rt.localEulerAngles.z) : Vector3.zero);
                pieces.Add(shown ? new Vector3(rt.sizeDelta.x, rt.sizeDelta.y, g.canvasRenderer.GetAlpha()) : Vector3.zero);
            }

            for (int m = 0; m < IntroChoreography.MaxMonkeys; m++)
            {
                RectTransform root = view.Troop.Rig(m).Root;
                pieces.Add(new Vector3(root.anchoredPosition.x, root.anchoredPosition.y, root.localEulerAngles.z));
            }

            return pieces;
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
