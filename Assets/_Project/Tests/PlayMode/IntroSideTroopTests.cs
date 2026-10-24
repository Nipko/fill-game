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

        // Desde D-046 (punto 3) estos lienzos dejan la franja de la tropa con 3 filas y la tropa va en su rama
        // (IntroThreeRowBranchTests); la tropa lateral queda de respaldo donde no cabe la franja. Estas pruebas miden ese respaldo
        // con el tablero de antes de D-046 en los mismos lienzos (BoardLayout.TroopBandOffForTests).
        [SetUp]
        public void SetUpWithoutTroopBand()
        {
            BoardLayout.TroopBandOffForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            BoardLayout.TroopBandOffForTests = false;
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

        /// <summary>
        /// Medida para el dueño (revisión de TL1; <c>[Explicit]</c>: es un informe, no una prueba, hasta que el dueño elija entre
        /// las opciones (a), (b) y (c) de D-021): cuánto pisa la tropa lateral la franja de la barra de la Crecida (07 §7.4:
        /// x 348-356, y 120-560, desde el nivel 25), y el hueco que quedaría a la derecha entre la línea de 8 dp de la cuia y la
        /// barra. Con (a), pasará a aserción: ninguna pieza del miquito de la derecha en esa franja desde el nivel 25.
        /// </summary>
        [UnityTest]
        [Explicit("medida para el dueño, D-021")]
        public IEnumerator SideTroop_MeasuresTheCrecidaBarStrip([Values(86, 143)] int level)
        {
            GameScreen.SimulatedScreen = Screen1080;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(800, false);
            IntroChoreography plan = view.Intro.Plan;
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Side));
            Rect bar = Rect.MinMaxRect(CrecidaBarX0, CrecidaBarY0, CrecidaBarX1, CrecidaBarY1);
            var rects = new List<Rect>();
            float worst = 0f;
            int frames = 0;
            for (int t = 0; t <= plan.ExitStartMs + IntroChoreography.ExitMs; t += 10)
            {
                view.Intro.Seek(t);
                rects.Clear();
                view.Troop.CollectRects(rects);
                bool hit = false;
                foreach (Rect r in rects)
                {
                    float w = Mathf.Min(r.xMax, bar.xMax) - Mathf.Max(r.xMin, bar.xMin);
                    float h = Mathf.Min(r.yMax, bar.yMax) - Mathf.Max(r.yMin, bar.yMin);
                    if (w > 0f && h > 0f)
                    {
                        hit = true;
                        worst = Mathf.Max(worst, w);
                    }
                }

                frames += hit ? 1 : 0;
            }

            float gap = CrecidaBarX0 - (view.CanvasWidth - plan.SideFreeDp);
            Debug.Log("AF_SIDE_CRECIDA nivel " + level + ": la tropa pisa la franja de la barra en " + frames + " fotogramas (hasta "
                + worst.ToString("0.0", CultureInfo.InvariantCulture) + " dp de ancho); hueco entre la línea de 8 dp y la barra: "
                + gap.ToString("0.0", CultureInfo.InvariantCulture) + " dp; un miquito de 28 dp necesita "
                + (plan.Model == TroopModel.Hang
                    ? ((IntroChoreography.SideHangReachS * IntroChoreography.MinMonkeyDp) + IntroChoreography.SideLianaMinXDp)
                    : (((IntroChoreography.SideClimbReachS + IntroChoreography.SideClimbBodyS) * IntroChoreography.MinMonkeyDp) - IntroChoreography.SideOffscreenDp))
                    .ToString("0.0", CultureInfo.InvariantCulture) + " dp");
        }

        /// <summary>
        /// En el lanzamiento el miquito se reconoce (revisión de TL1; D-021 y 07 §6.4): en cada fotograma muestreado, como mucho
        /// una fruta (la de la mano) corta la caja de la cabeza de cada miquito lateral; y ninguna fruta en la mano o recién
        /// soltada queda cortada por el borde de la pantalla.
        /// </summary>
        [UnityTest]
        public IEnumerator SideThrow_AtMostOneFruitOverEachHead_AndNoneCutByTheScreenEdge([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            GameScreen.SimulatedScreen = widthPx == 1080 ? Screen1080 : Screen720;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            RectTransform space = view.Troop.SideLayer;
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Side));
                int checkedFrames = 0;
                int maxOver = 0;
                for (int t = 0; t <= plan.TotalMs; t += 4)
                {
                    view.Intro.Seek(t);
                    for (int m = 0; m < plan.MonkeyCount; m++)
                    {
                        MonkeyRig rig = view.Troop.SideRig(m);
                        if (!rig.Visible || rig.Alpha <= 0.01f)
                        {
                            continue;
                        }

                        Rect head = rig.HeadRect(space);
                        int over = 0;
                        for (int k = 0; k < plan.FruitCount; k++)
                        {
                            Image fruit = view.Intro.FruitImage(k);
                            if (!view.Intro.FruitVisible(k) || fruit.canvasRenderer.GetAlpha() <= 0.01f)
                            {
                                continue;
                            }

                            Rect f = VisibleFruit(fruit.rectTransform, space);
                            over += f.Overlaps(head) ? 1 : 0;
                            string where = "nivel " + level + ", " + widthPx + " px, T " + duration + ", t " + t + ", fruta " + k + " " + f;
                            Assert.That(f.xMin, Is.GreaterThanOrEqualTo(0f), "fruta cortada por el borde izquierdo: " + where);
                            Assert.That(f.xMax, Is.LessThanOrEqualTo(view.CanvasWidth), "fruta cortada por el borde derecho: " + where);
                        }

                        maxOver = Mathf.Max(maxOver, over);
                        checkedFrames++;
                        Assert.That(over, Is.LessThanOrEqualTo(1), "como mucho la fruta de la mano sobre la cabeza del miquito " + m
                            + ": nivel " + level + ", " + widthPx + " px, T " + duration + ", t " + t + ", cabeza " + head);
                    }
                }

                Debug.Log("AF_SIDE_HEAD nivel " + level + " a " + widthPx + " px, T " + duration + ": como mucho " + maxOver
                    + " fruta(s) sobre una cabeza en " + checkedFrames + " cabezas muestreadas");
                Assert.That(checkedFrames, Is.GreaterThan(50));
            }
        }

        /// <summary>
        /// La fruta lanzada se oculta solo mientras corta la cabeza de su miquito (<see cref="IntroChoreography.SideClearMs"/>), y
        /// eso es poco (revisión de TL1): como mucho 80 ms o, en los vuelos largos, el 25 % del vuelo, así que nunca desaparece
        /// de la mano para aparecer a media trayectoria. Con la mano por debajo del hombro (guion §4.4), en los dos modelos y
        /// las dos versiones.
        /// </summary>
        [UnityTest]
        public IEnumerator SideThrow_HidesTheThrownFruitOnlyBriefly([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            GameScreen.SimulatedScreen = widthPx == 1080 ? Screen1080 : Screen720;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Side));
                float worst = 0f;
                float worstShare = 0f;
                for (int k = 0; k < plan.FruitCount; k++)
                {
                    float tau = plan.FruitImpactMs[k] - plan.FruitReleaseMs[k];
                    float clear = plan.SideClearMs[k];
                    string at = "nivel " + level + ", " + widthPx + " px, T " + duration + ", fruta " + k + ": oculta " + clear + " ms de un vuelo de " + tau + " ms";
                    Assert.That(clear, Is.LessThanOrEqualTo(Mathf.Max(MaxHiddenMs, MaxHiddenShare * tau)), at);
                    worst = Mathf.Max(worst, clear);
                    worstShare = Mathf.Max(worstShare, clear / Mathf.Max(1f, tau));
                }

                Debug.Log("AF_SIDE_HIDDEN nivel " + level + " a " + widthPx + " px, T " + duration + ": la fruta lanzada se oculta como mucho "
                    + worst.ToString("0", CultureInfo.InvariantCulture) + " ms y como mucho el " + (100f * worstShare).ToString("0", CultureInfo.InvariantCulture)
                    + " % de su vuelo; ocultas/vuelo (ms):" + HiddenList(plan));
            }
        }

        // Tope de lo que una fruta lanzada se oculta tras salir de la mano (revisión de TL1): 80 ms o el 25 % del vuelo.
        private const float MaxHiddenMs = 80f;
        private const float MaxHiddenShare = 0.25f;

        private static string HiddenList(IntroChoreography plan)
        {
            var line = new System.Text.StringBuilder();
            for (int k = 0; k < plan.FruitCount; k++)
            {
                line.Append(' ').Append(plan.SideClearMs[k].ToString("0", CultureInfo.InvariantCulture)).Append('/')
                    .Append((plan.FruitImpactMs[k] - plan.FruitReleaseMs[k]).ToString("0", CultureInfo.InvariantCulture));
            }

            return line.ToString();
        }

        /// <summary>
        /// Colgado (el 86, churuco): la liana va sobre la banda exterior del marco, a media anchura del margen libre (como la del
        /// que trepa) y no pegada al borde; el cuerpo cuelga justo debajo del agarre y ninguna pieza del miquito, cola incluida,
        /// pasa a menos de 1 dp del borde de la pantalla (revisión de TL1). Su perfil de luz y su halo se ven con él.
        /// </summary>
        [UnityTest]
        public IEnumerator SideHang_LianaOverTheFrame_AndTailNeverOffTheEdge([Values(1080, 720)] int widthPx)
        {
            GameScreen.SimulatedScreen = widthPx == 1080 ? Screen1080 : Screen720;
            yield return LoadLevel(86);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            RectTransform space = view.Troop.SideLayer;
            var rects = new List<Rect>();
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Side));
                Assert.That(plan.Model, Is.EqualTo(TroopModel.Hang));
                Assert.That(plan.SideHangShiftDp, Is.GreaterThan(0f), "el cuerpo, desplazado hacia el borde bajo su agarre");
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    float edge = m == 0 ? plan.SideX[m] : view.CanvasWidth - plan.SideX[m];
                    Assert.That(edge, Is.EqualTo(IntroChoreography.SideHangLianaFraction * plan.SideFreeDp).Within(0.01f), "liana " + m + " a media anchura del margen");
                }

                int seen = 0;
                for (int t = 0; t <= plan.ExitStartMs + IntroChoreography.ExitMs; t += 10)
                {
                    view.Intro.Seek(t);
                    for (int m = 0; m < plan.MonkeyCount; m++)
                    {
                        MonkeyRig rig = view.Troop.SideRig(m);
                        if (!rig.Visible || rig.Alpha <= 0f)
                        {
                            continue;
                        }

                        seen++;
                        rects.Clear();
                        rig.CollectRects(space, rects);
                        foreach (Rect r in rects)
                        {
                            float out0 = m == 0 ? r.xMin : view.CanvasWidth - r.xMax;
                            Assert.That(out0, Is.GreaterThanOrEqualTo(IntroChoreography.SideTailMinXDp - 0.01f),
                                "ninguna pieza del miquito " + m + " a menos de 1 dp del borde: " + widthPx + " px, T " + duration + ", t " + t + ", " + r);
                        }

                        Assert.That(view.Troop.SideRim(m).Visible, Is.True, "perfil de luz del miquito " + m);
                        Assert.That(view.Troop.SideRim(m).Alpha, Is.EqualTo(rig.Alpha).Within(0.01f), "el perfil, con el alfa del miquito");
                        Assert.That(view.Troop.SideHalo(m).enabled, Is.True, "halo del miquito " + m);
                    }
                }

                Assert.That(seen, Is.GreaterThan(30));
            }
        }

        /// <summary>
        /// Orden de capas (revisión de TL1; D-021): la capa de la tropa lateral va por delante del agua que corre por los
        /// costados del marco (la liana no se pierde detrás del trazo del marco) y por detrás de las cuias.
        /// </summary>
        [UnityTest]
        public IEnumerator SideLayer_IsInFrontOfTheFrameWater_AndBehindTheCuias()
        {
            GameScreen.SimulatedScreen = Screen1080;
            yield return LoadLevel(143);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(800, false);
            RectTransform side = view.Troop.SideLayer;
            Transform board = side.parent;
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(view.Views[i].Root.parent, Is.EqualTo(board), "la capa lateral y las cuias, en el tablero");
                Assert.That(side.GetSiblingIndex(), Is.LessThan(view.Views[i].Root.GetSiblingIndex()), "detrás de la cuia " + i);
            }

            Assert.That(view.Intro.Layer.parent, Is.EqualTo(board));
            for (int s = 0; s < 2; s++)
            {
                Transform strip = view.Intro.FrameSideStrip(s).transform;
                Assert.That(strip.parent, Is.EqualTo(view.Troop.SideWaterSlot), "el agua del costado " + s + " del marco, en el hueco de la capa lateral");
                Assert.That(strip.IsChildOf(view.Intro.Layer), Is.False, "ya no en la capa de la intro, delante de las cuias");
                for (int m = 0; m < IntroChoreography.SideMonkeys; m++)
                {
                    Assert.That(view.Troop.SideWaterSlot.GetSiblingIndex(), Is.LessThan(view.Troop.SideLiana(m).transform.GetSiblingIndex()), "el agua del marco, detrás de la liana " + m);
                    Assert.That(view.Troop.SideWaterSlot.GetSiblingIndex(), Is.LessThan(view.Troop.SideRig(m).Root.GetSiblingIndex()), "y del miquito " + m);
                    Assert.That(view.Troop.SideWaterSlot.GetSiblingIndex(), Is.LessThan(view.Troop.SideHalo(m).transform.GetSiblingIndex()), "el halo, delante del agua");
                    Assert.That(view.Troop.SideHalo(m).transform.GetSiblingIndex(), Is.LessThan(view.Troop.SideLiana(m).transform.GetSiblingIndex()), "y detrás de la liana " + m);
                    Assert.That(view.Troop.SideRim(m).Root.GetSiblingIndex(), Is.LessThan(view.Troop.SideRig(m).Root.GetSiblingIndex()), "el perfil de luz, detrás del miquito " + m);
                }
            }

            // Y las tiras siguen corriendo por el marco en la intro (en cualquier posadero).
            bool seen = false;
            for (int t = 0; t <= 800 && !seen; t += 20)
            {
                view.Intro.Seek(t);
                seen = view.Intro.FrameSideStrip(0).enabled && view.Intro.FrameSideStrip(1).enabled;
            }

            Assert.That(seen, Is.True, "el agua corre por los costados del marco");
        }

        /// <summary>
        /// La liana lateral se ve (revisión de TL1): 3 dp con borde claro, y nace de un nudo de hojas en lo alto, junto al HUD,
        /// que la contiene, sin pisar el HUD y dentro del margen lateral.
        /// </summary>
        [UnityTest]
        public IEnumerator SideLiana_IsThick_AndHangsFromALeafKnot([Values(86, 143)] int level)
        {
            GameScreen.SimulatedScreen = Screen1080;
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(800, false);
            IntroChoreography plan = view.Intro.Plan;
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Side));
            float hudBottom = view.Intro.Input.HudBottom;
            view.Intro.Seek(Mathf.Max(plan.SideArriveMs[0], plan.SideArriveMs[1]) + 10f);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                BezierStripGraphic liana = view.Troop.SideLiana(m);
                Assert.That(liana.enabled, Is.True, "liana " + m);
                Rect l = liana.LocalBounds();
                Assert.That(l.width, Is.GreaterThanOrEqualTo(TroopLayer.SideLianaWidthDp), "liana " + m + " de ≥ 3 dp con su borde");
                float top = -l.yMax;
                Assert.That(top, Is.EqualTo(plan.SideLianaTopY).Within(TroopLayer.SideLianaEdgeDp + (0.5f * TroopLayer.SideLianaWidthDp) + 0.01f));
                float x0 = float.MaxValue;
                float x1 = float.MinValue;
                float y0 = float.MaxValue;
                float y1 = float.MinValue;
                for (int i = 0; i < TroopLayer.SideKnotLeaves; i++)
                {
                    Image leaf = view.Troop.SideKnotLeaf(m, i);
                    Assert.That(leaf.enabled, Is.True, "hoja " + i + " del nudo de la liana " + m);
                    Assert.That(leaf.raycastTarget, Is.False);
                    Assert.That(leaf.canvasRenderer.GetAlpha(), Is.EqualTo(liana.canvasRenderer.GetAlpha()).Within(0.001f), "con el alfa de la liana");
                    Rect r = CanvasRect(leaf.rectTransform, view.Troop.SideLayer);
                    Assert.That(r.yMin, Is.GreaterThanOrEqualTo(hudBottom), "nunca sobre el HUD");
                    Assert.That(r.xMin, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth));
                    float margin = m == 0 ? r.xMax : view.CanvasWidth - r.xMin;
                    Assert.That(margin, Is.LessThanOrEqualTo(plan.SideFreeDp + 0.01f), "solo en el margen lateral (hasta la línea de 8 dp de la cuia)");
                    x0 = Mathf.Min(x0, r.xMin);
                    x1 = Mathf.Max(x1, r.xMax);
                    y0 = Mathf.Min(y0, r.yMin);
                    y1 = Mathf.Max(y1, r.yMax);
                }

                Assert.That(plan.SideX[m], Is.InRange(x0, x1), "la liana nace dentro del nudo");
                Assert.That(plan.SideLianaTopY, Is.InRange(y0, y1), "el nudo cubre lo alto de la liana");
            }
        }

        // Franja de la barra de la Crecida (07 §7.4), desde el nivel 25 (streak_unlock_level).
        private const float CrecidaBarX0 = 348f;
        private const float CrecidaBarX1 = 356f;
        private const float CrecidaBarY0 = 120f;
        private const float CrecidaBarY1 = 560f;

        // Caja visible de la fruta (el lado del sprite × FruitArtFill), centrada en su sitio y sin el giro.
        private static Rect VisibleFruit(RectTransform rt, RectTransform space)
        {
            Rect r = CanvasRect(rt, space);
            float side = rt.sizeDelta.x * IntroChoreography.FruitArtFill;
            return new Rect(r.center.x - (0.5f * side), r.center.y - (0.5f * side), side, side);
        }

        private static Rect CanvasRect(RectTransform rt, RectTransform space)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect area = space.rect;
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 l = space.InverseTransformPoint(corners[i]);
                float x = l.x - area.xMin;
                float y = area.yMax - l.y;
                x0 = Mathf.Min(x0, x);
                x1 = Mathf.Max(x1, x);
                y0 = Mathf.Min(y0, y);
                y1 = Mathf.Max(y1, y);
            }

            return Rect.MinMaxRect(x0, y0, x1, y1);
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
            if (p.Perch == IntroPerch.Side && p.Model == TroopModel.Hang)
            {
                line.Append(", cuerpo desplazado ").Append(p.SideHangShiftDp.ToString("0.0", CultureInfo.InvariantCulture)).Append(" dp bajo el agarre");
            }
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

                // Cuánto se oculta cada fruta lanzada tras salir de la mano (SideClearMs) y la velocidad horizontal del lanzamiento.
                line.Append(", ocultas tras soltar (ms):");
                float vMin = float.MaxValue;
                float vMax = 0f;
                for (int k = 0; k < p.FruitCount; k++)
                {
                    line.Append(' ').Append(p.SideClearMs[k].ToString("0", CultureInfo.InvariantCulture));
                    float tau = Mathf.Max(1f, p.FruitImpactMs[k] - p.FruitReleaseMs[k]);
                    float v = 1000f * Mathf.Abs(p.FruitImpactX[k] - p.FruitReleaseX[k]) / tau;
                    vMin = Mathf.Min(vMin, v);
                    vMax = Mathf.Max(vMax, v);
                }

                line.Append(", velocidad horizontal de ").Append(vMin.ToString("0", CultureInfo.InvariantCulture)).Append(" a ")
                    .Append(vMax.ToString("0", CultureInfo.InvariantCulture)).Append(" dp/s");
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

        internal static Rect CuiaRect(CuiaView v)
        {
            float w = v.Unit * CuiaGeometry.WidthU;
            float h = v.HeightU * v.Unit;
            Vector2 foot = v.FootPosition;
            return Rect.MinMaxRect(foot.x - (w * 0.5f), -foot.y - h, foot.x + (w * 0.5f), -foot.y);
        }

        internal static float Distance(Rect a, Rect b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
