#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// Tropa lateral en los tableros de 3 filas (D-021) en la escena real, con los paquetes de 1-200: el primer nivel de 3
    /// filas de los mundos 4 (el 86, churuco, <c>cuelga</c>) y 5 (el 143, huapo colorado, <c>sacude</c>) va con un miquito por
    /// lado, de ≥ 28 dp, sin encogerlo por debajo; el calendario de la fruta (impactos y floraciones) y T son los del dosel del
    /// mismo nivel; el toque se habilita a la misma hora y la tropa se va en cuanto se habilita, sin reposo ya en juego; nada
    /// de la tropa recibe toques; ninguna pieza a menos de 8 dp de una cuia, sobre el HUD o sobre la bandeja en ningún
    /// fotograma muestreado (1080 × 2340 y 720 × 1600 px, versiones larga y corta); 0 B por fotograma; los reintentos y los
    /// niveles de 3 filas que no le tocan, en dosel.
    /// </summary>
    public sealed class IntroSideTroopTests
    {
        /// <summary>1080 × 2340 px (360 × 780 dp), la zona segura del teléfono de pruebas.</summary>
        internal static readonly Vector4 Screen1080 = new Vector4(1080f, 2340f, 96f, 48f);

        /// <summary>720 × 1600 px (360 × 800 dp), con 32 dp de cutout y 16 de gestos.</summary>
        internal static readonly Vector4 Screen720 = new Vector4(720f, 1600f, 64f, 32f);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator FirstThreeRowLevelOfWorlds4And5_GoesSide_WithoutShrinkingBelowMin([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            GameScreen.SimulatedScreen = widthPx == 1080 ? Screen1080 : Screen720;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "tablero de 3 filas");
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography p = view.Intro.Plan;
                Debug.Log("AF_SIDE nivel " + level + " a " + widthPx + " px, T " + duration + ": " + Describe(p));
                Assert.That(p.Perch, Is.EqualTo(IntroPerch.Side), "nivel " + level + ": primer nivel de 3 filas de su mundo");
                Assert.That(p.MonkeyCount, Is.EqualTo(IntroChoreography.SideMonkeys));
                Assert.That(p.BodyDp, Is.GreaterThanOrEqualTo(IntroChoreography.MinMonkeyDp), "nunca por debajo de MinMonkeyDp (D-021, punto 4)");
                Assert.That(p.Model, Is.EqualTo(level == 86 ? TroopModel.Hang : TroopModel.Shake), "modelo de suelta de la especie");
                Assert.That(view.Troop.SideActive, Is.True);
                Assert.That(view.IntroSpecies!.Id, Is.EqualTo(level == 86 ? "lagothrix_lagothricha" : "cacajao_calvus_ucayalii"));
                for (int k = 0; k < p.FruitCount; k++)
                {
                    int m = p.FruitMonkey[k];
                    Assert.That(m, Is.InRange(0, 1), "toda fruta la lanza un miquito lateral");
                    Assert.That(p.FruitReleaseMs[k], Is.GreaterThanOrEqualTo(p.SideArriveMs[m] + IntroChoreography.ReleaseAfterArrivalMs - 0.01f), "lanza ya en su sitio");
                    float mid = view.CanvasWidth * 0.5f;
                    int half = Mathf.Abs(p.FruitImpactX[k] - mid) <= 0.5f ? (p.EntryFromRight ? 1 : 0) : (p.FruitImpactX[k] < mid ? 0 : 1);
                    Assert.That(m, Is.EqualTo(half), "cada mitad, su miquito (la columna del centro, el lado de la variación)");
                }
            }
        }

        [UnityTest]
        public IEnumerator SideTroop_KeepsClearOfCuiasHudAndTray_InEverySampledFrame([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            GameScreen.SimulatedScreen = widthPx == 1080 ? Screen1080 : Screen720;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            var rects = new List<Rect>();
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Side));
                float hudBottom = view.Intro.Input.HudBottom;
                float trayTop = view.Intro.Input.TrayTop;
                float end = plan.ExitStartMs + IntroChoreography.ExitMs;
                float nearest = float.MaxValue;
                int sampled = 0;
                for (int t = 0; t <= end; t += 10)
                {
                    view.Intro.Seek(t);
                    rects.Clear();
                    view.Troop.CollectRects(rects);
                    sampled += rects.Count > 0 ? 1 : 0;
                    foreach (Rect r in rects)
                    {
                        if (r.xMax < 0f || r.xMin > view.CanvasWidth)
                        {
                            continue;
                        }

                        string where = "nivel " + level + ", " + widthPx + " px, T " + duration + ", t " + t + ", " + r;
                        Assert.That(r.yMin, Is.GreaterThanOrEqualTo(hudBottom - 0.5f), "nada sobre el HUD: " + where);
                        Assert.That(r.yMax, Is.LessThanOrEqualTo(trayTop + 0.5f), "nada sobre la bandeja: " + where);
                        for (int i = 0; i < view.Views.Length; i++)
                        {
                            float d = Distance(r, CuiaRect(view.Views[i]));
                            nearest = Mathf.Min(nearest, d);
                            Assert.That(d, Is.GreaterThanOrEqualTo(IntroChoreography.CuiaClearanceDp), "≥ 8 dp de la cuia " + i + ": " + where);
                        }
                    }
                }

                Debug.Log("AF_SIDE_CLEAR nivel " + level + " a " + widthPx + " px, T " + duration + ": distancia mínima a una cuia "
                    + nearest.ToString("0.0", CultureInfo.InvariantCulture) + " dp en " + sampled + " fotogramas con tropa");
                Assert.That(sampled, Is.GreaterThan(30), "la tropa se vio en los fotogramas muestreados");
            }
        }

        [UnityTest]
        public IEnumerator SideCalendar_IsTheCanopyCalendar_SameFruitTimesAndT([Values(86, 143)] int level)
        {
            GameScreen.SimulatedScreen = Screen1080;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroInput input = view.Intro.Input;
                Assert.That(input.Perch, Is.EqualTo(IntroPerch.Side));
                var side = new IntroChoreography();
                var canopy = new IntroChoreography();
                side.Plan(input);
                input.Perch = IntroPerch.Canopy;
                canopy.Plan(input);
                input.Perch = IntroPerch.Side;
                string at = "nivel " + level + ", T " + duration;
                Assert.That(side.Perch, Is.EqualTo(IntroPerch.Side), at);
                Assert.That(canopy.Perch, Is.EqualTo(IntroPerch.Canopy), at);
                Assert.That(side.TotalMs, Is.EqualTo(canopy.TotalMs), at + ": T");
                Assert.That(side.DStartMs, Is.EqualTo(canopy.DStartMs), at);
                Assert.That(side.FruitCount, Is.EqualTo(canopy.FruitCount), at);
                Assert.That(side.FruitCount, Is.GreaterThan(0), at);
                Assert.That(side.FruitPerCuia, Is.EqualTo(canopy.FruitPerCuia), at);
                Assert.That(side.FirstImpactMs, Is.EqualTo(canopy.FirstImpactMs), at);
                Assert.That(side.LastBloomEndMs, Is.EqualTo(canopy.LastBloomEndMs), at);
                for (int k = 0; k < side.FruitCount; k++)
                {
                    Assert.That(side.FruitCuia[k], Is.EqualTo(canopy.FruitCuia[k]), at + ", fruta " + k);
                    Assert.That(side.FruitTint[k], Is.EqualTo(canopy.FruitTint[k]), at + ", fruta " + k);
                    Assert.That(side.FruitImpactMs[k], Is.EqualTo(canopy.FruitImpactMs[k]), at + ", impacto de la fruta " + k);
                    Assert.That(side.FruitBloomEndMs[k], Is.EqualTo(canopy.FruitBloomEndMs[k]), at + ", floración de la fruta " + k);
                    Assert.That(side.FruitFirstInRow[k], Is.EqualTo(canopy.FruitFirstInRow[k]), at);
                }

                for (int r = 0; r < side.RunCount; r++)
                {
                    Assert.That(side.RunBloomMs[r], Is.EqualTo(canopy.RunBloomMs[r]), at + ", racha " + r);
                }

                for (int i = 0; i < side.CuiaCount; i++)
                {
                    Assert.That(side.FillEndMs[i], Is.EqualTo(canopy.FillEndMs[i]), at + ", llenado de la cuia " + i);
                }
            }

            // El toque se habilita a la misma hora (T: la duración del flujo, 800 ms desde el 21), y la tropa se va entonces.
            LevelFlow flow = boot.Flow!;
            flow.Load(level);
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Side));
            int total = Mathf.RoundToInt(view.Intro.Plan.TotalMs);
            Assert.That(total, Is.EqualTo(GameConfig.AnimTintDurationMsLate));
            PlayModeHelpers.TickExact(boot, total - 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro), "aún en la intro a T − 1");
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "el toque, en T como siempre");
            Assert.That(view.Intro.Plan.ExitStartMs, Is.EqualTo(view.Intro.Plan.TotalMs), "se va en cuanto se habilita el toque: sin reposo ya en juego");
            Assert.That(view.Intro.TroopGoneMs, Is.EqualTo(view.Intro.Plan.TotalMs + IntroChoreography.ExitMs).Within(0.01f));
            Assert.That(view.Troop.SideActive, Is.True, "sube por su liana");
            PlayModeHelpers.TickExact(boot, (int)IntroChoreography.ExitMs + 1);
            Assert.That(view.Troop.Active, Is.False, "y a los 500 ms ya no está");
            Assert.That(view.Intro.TroopVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator NothingOfTheSideTroop_ReceivesTouches()
        {
            GameScreen.SimulatedScreen = Screen1080;
            yield return LoadLevel(143);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(800, false);
            Assert.That(view.Troop.SideActive, Is.True);
            var graphics = new List<Graphic>();
            for (int t = 0; t <= 1300; t += 100)
            {
                view.Intro.Seek(t);
                graphics.Clear();
                view.Troop.CollectGraphics(graphics);
                foreach (Graphic g in graphics)
                {
                    Assert.That(g.raycastTarget, Is.False, g.name + " a " + t + " ms");
                }
            }

            Assert.That(view.Troop.SideLayer.GetComponentsInChildren<Selectable>(true), Is.Empty);
        }

        [UnityTest]
        public IEnumerator Retry_AndThreeRowLevelsOffTheSchedule_StayInTheCanopy()
        {
            GameScreen.SimulatedScreen = Screen1080;

            // El 86 en un reintento: dosel (D-010, D-021 punto 2).
            yield return LoadLevel(86);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(800, true);
            Assert.That(boot.View.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Canopy), "reintento del 86");
            Assert.That(boot.View.Troop.Active, Is.False);

            // El 89 y el 92: 3 filas, ordinales 1 y 2 en el mundo 4 (dosel); el 95, el 3: tropa lateral.
            foreach ((int level, IntroPerch perch) in new[] { (89, IntroPerch.Canopy), (92, IntroPerch.Canopy), (95, IntroPerch.Side) })
            {
                LaunchOptions.Reset();
                GameScreen.SimulatedScreen = Screen1080;
                yield return LoadLevel(level);
                boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                boot.View!.DebugReplayIntro(800, false);
                Assert.That(boot.View.Layout!.Rows, Is.EqualTo(3), "nivel " + level);
                Assert.That(boot.View.Intro.Plan.Perch, Is.EqualTo(perch), "nivel " + level);
            }
        }

        // Rutas de la tropa lateral para el presupuesto de 0 B por fotograma: los dos modelos y las dos versiones, salto a
        // mitad y Reducir movimiento.
        private static readonly string[] AllocationCases =
        {
            "cuelga_86_corta", "cuelga_86_larga", "sacude_143_corta", "sacude_143_larga", "salto_143", "reducir_143",
        };

        [UnityTest]
        public IEnumerator SideTroop_AllocatesNothingPerFrame([ValueSource(nameof(AllocationCases))] string route)
        {
            int level = route.Contains("86") ? 86 : 143;
            int duration = route.EndsWith("larga", System.StringComparison.Ordinal) ? 1400 : 800;
            bool reduce = route.StartsWith("reducir", System.StringComparison.Ordinal);
            int skipAt = route.StartsWith("salto", System.StringComparison.Ordinal) ? 500 : -1;
            if (reduce)
            {
                duration = GameConfig.ReducedMotionIntroMs;
            }

            GameScreen.SimulatedScreen = Screen1080;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            boot.Flow!.ReduceMotion = reduce;
            IntroAnimation intro = view.Intro;

            // Calentamiento: una pasada completa por la misma ruta.
            view.DebugReplayIntro(duration, false);
            RunRoute(intro, skipAt, null);

            view.DebugReplayIntro(duration, false);
            Assert.That(intro.Plan.Perch, Is.EqualTo(IntroPerch.Side), route);
            int frames = RunRoute(intro, skipAt, (t, step) => Assert.That(step, Is.Not.AllocatingGCMemory(), route + ", t = " + t));
            Assert.That(frames, Is.GreaterThan(10), route + ": fotogramas medidos");
            Assert.That(intro.Active, Is.False, route + ": la intro y la tropa terminan");
            boot.Flow.ReduceMotion = false;
        }

        internal static IEnumerator LoadLevel(int level)
        {
            return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        internal static string Describe(IntroChoreography p)
        {
            var line = new System.Text.StringBuilder();
            line.Append("posadero ").Append(p.Perch).Append(", modelo ").Append(p.Model).Append(", miquitos ").Append(p.MonkeyCount);
            line.Append(" de ").Append(p.BodyDp.ToString("0.0", CultureInfo.InvariantCulture)).Append(" dp");
            line.Append(", ancho libre por lado ").Append(p.SideFreeDp.ToString("0.0", CultureInfo.InvariantCulture)).Append(" dp");
            line.Append(", cabe hasta ").Append(p.SideFitDp.ToString("0.0", CultureInfo.InvariantCulture)).Append(" dp");
            if (p.Perch == IntroPerch.Side)
            {
                for (int m = 0; m < p.MonkeyCount; m++)
                {
                    line.Append(" | m").Append(m).Append(" x ").Append(p.SideX[m].ToString("0.0", CultureInfo.InvariantCulture))
                        .Append(", baja de ").Append(p.SideStartY[m].ToString("0", CultureInfo.InvariantCulture))
                        .Append(" a ").Append(p.SidePerchY[m].ToString("0", CultureInfo.InvariantCulture))
                        .Append(" entre ").Append(p.SideStartMs[m].ToString("0", CultureInfo.InvariantCulture))
                        .Append(" y ").Append(p.SideArriveMs[m].ToString("0", CultureInfo.InvariantCulture)).Append(" ms")
                        .Append(p.SideFadeIn[m] ? " (asoma)" : " (ya bajando en t = 0)");
                }
            }

            line.Append(", frutas ").Append(p.FruitCount).Append(", primera suelta ");
            float first = float.MaxValue;
            for (int k = 0; k < p.FruitCount; k++)
            {
                first = Mathf.Min(first, p.FruitReleaseMs[k]);
            }

            line.Append(first.ToString("0", CultureInfo.InvariantCulture)).Append(" ms");
            if (p.Perch == IntroPerch.Side)
            {
                line.Append(", sueltas del 0:");
                for (int k = 0; k < p.FruitCount; k++)
                {
                    if (p.FruitMonkey[k] == 0)
                    {
                        line.Append(' ').Append(p.FruitReleaseMs[k].ToString("0", CultureInfo.InvariantCulture)).Append('>')
                            .Append(p.FruitImpactMs[k].ToString("0", CultureInfo.InvariantCulture));
                    }
                }
            }

            return line.ToString();
        }

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
