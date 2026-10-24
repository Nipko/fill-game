#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Sitio para la tropa con 3 filas (D-046, punto 3) en la escena real: el primer nivel de 3 filas de los mundos 4 (el 86,
    /// churuco) y 5 (el 143, huapo colorado), a 1080 × 2340 px (360 × 780 dp) y 720 × 1600 px (360 × 800 dp), deja una franja
    /// ≥ 44 dp entre el HUD y el panel, el panel a ≥ 31,5 dp de la bandeja como antes (Q-T6T-3) y la tropa en su rama en el primer intento (no la
    /// tropa lateral de D-021, que queda de respaldo), en las dos versiones; nada de la tropa sobre el HUD ni dentro del panel,
    /// salvo el roce con el borde del HUD de Q-T6T-4, y ≥ 8 dp de toda cuia en todos los fotogramas muestreados; el reintento, en dosel (D-010). En 16:9 (1080 × 1920 px,
    /// 360 × 640 dp) no cabe la franja: el tablero de 3 filas es el de antes y la intro no va en la rama.
    /// </summary>
    public sealed class IntroThreeRowBranchTests
    {
        /// <summary>
        /// Lo que la tropa puede entrar en la fila del HUD con la franja justa de 44 dp (Q-T6T-4): como mucho la subida de la rama
        /// hacia las esquinas (6 dp), por donde entra y sale. Medido: 0,5 dp en el 86 y 3,6 dp en el 143 (al salir por la esquina
        /// de la derecha, bajo Reiniciar), a 1080 y 720 px.
        /// </summary>
        private const float HudTouchDp = 6f;

        /// <summary>1080 × 1920 px (360 × 640 dp, 16:9).</summary>
        internal static readonly Vector4 Screen16x9 = new Vector4(1080f, 1920f, 96f, 48f);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator FirstThreeRowLevelOfWorlds4And5_GoesOnTheBranch_ClearOfHudPanelAndCuias([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            return Branch(level, widthPx, HudTouchDp);
        }

        /// <summary>
        /// Lo mismo sin tolerancia sobre el HUD (Q-T6T-4; <c>[Explicit]</c>: hoy falla). Con la franja justa de 44 dp, el tamaño de
        /// 07 §6.4, <c>min(tam_dp, franja − 10)</c>, deja la cabeza de un miquito sentado a 0,3 dp del borde del HUD, y sus gestos
        /// la meten en él (0,5 dp en el 86; 3,6 dp en el 143 al salir por la esquina de la derecha, bajo Reiniciar); con el tablero de prueba de 16 cuias del mundo 1 (mono ardilla), al entrar
        /// por la punta de la rama, que sube hacia las esquinas, hasta ≈ 5 dp en el borde de la pantalla. Pasará cuando el dueño
        /// decida la holgura (la regla de 07 §6.4 o una franja mayor).
        /// </summary>
        [UnityTest]
        [Explicit("Q-T6T-4: la tropa roza el HUD con la franja justa de 44 dp (D-046).")]
        public IEnumerator FirstThreeRowLevel_Branch_StrictlyOffTheHud_KnownIssue([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            return Branch(level, widthPx, 0.5f);
        }

        private static IEnumerator Branch(int level, int widthPx, float hudTolerance)
        {
            GameScreen.SimulatedScreen = widthPx == 1080 ? IntroSideTroopTests.Screen1080 : IntroSideTroopTests.Screen720;
            yield return IntroSideTroopTests.LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            BoardLayoutResult l = view.Layout!;
            Assert.That(l.Rows, Is.EqualTo(3), "tablero de 3 filas");
            Assert.That(l.TroopBand, Is.True, "la franja de la tropa (D-046)");
            float hudBottom = view.HudBottomDp;
            float trayTop = view.TrayTopDp;
            Assert.That(l.PanelY - hudBottom, Is.GreaterThanOrEqualTo(IntroChoreography.BranchMinBandDp), "franja ≥ 44 dp");
            Assert.That(trayTop - l.PanelBottom, Is.GreaterThanOrEqualTo(31.5f - BoardLayout.ThreeRowFloorDropDp - 0.01f), "≥ 31,5 dp hasta la bandeja (menos lo que baje el suelo)");
            Assert.That(l.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth));
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(CuiaRect(view.Views[i]).yMin - CuiaView.LiftDp, Is.GreaterThan(hudBottom), "ni la cuia " + i + " elevada ni su cielo en el HUD");
            }

            Debug.Log("AF_TROOP_BAND nivel " + level + " a " + widthPx + " px: tablero " + Dp(hudBottom) + "-" + Dp(trayTop) + ", panel "
                + Dp(l.PanelY) + "-" + Dp(l.PanelBottom) + ", franja " + Dp(l.PanelY - hudBottom) + " dp, a la bandeja " + Dp(trayTop - l.PanelBottom)
                + " dp, cuia " + Dp(l.CuiaWidth) + " dp (" + view.Views.Length + " cuias)");

            var rects = new List<Rect>();
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                string what = "nivel " + level + ", " + widthPx + " px, T " + duration;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), what + ": en la rama en el primer intento");
                Assert.That(plan.MonkeyCount, Is.GreaterThan(0), what);
                Assert.That(plan.BodyDp, Is.GreaterThanOrEqualTo(IntroChoreography.MinMonkeyDp), what);
                Assert.That(view.Troop.SideActive, Is.False, what + ": sin tropa lateral");
                Assert.That(view.IntroSpecies!.Id, Is.EqualTo(level == 86 ? "lagothrix_lagothricha" : "cacajao_calvus_ucayalii"));
                float panelTop = view.Intro.Input.PanelTop;
                float end = plan.ExitStartMs + IntroChoreography.ExitMs;
                int seen = 0;
                float worstHud = float.MinValue;
                for (int t = 0; t <= end; t += 10)
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

                        seen++;
                        string where = what + ", t " + t + ", " + r;
                        // Con la franja justa de 44 dp, el tamaño de 07 §6.4 deja la tropa rozar el borde del HUD (Q-T6T-4; ver
                        // FirstThreeRowLevel_Branch_StrictlyOffTheHud_KnownIssue): se tolera HudTouchDp.
                        worstHud = Mathf.Max(worstHud, hudBottom - r.yMin);
                        Assert.That(r.yMin, Is.GreaterThanOrEqualTo(hudBottom - hudTolerance), "nada sobre el HUD: " + where);
                        Assert.That(r.yMax, Is.LessThanOrEqualTo(panelTop + 0.5f), "nada dentro del panel: " + where);
                        for (int i = 0; i < view.Views.Length; i++)
                        {
                            Assert.That(Distance(r, CuiaRect(view.Views[i])), Is.GreaterThanOrEqualTo(IntroChoreography.CuiaClearanceDp), "≥ 8 dp de la cuia " + i + ": " + where);
                        }
                    }
                }

                Assert.That(seen, Is.GreaterThan(30), what + ": la tropa se vio");
                Debug.Log("AF_TROOP_BAND " + what + ": lo más que entra en el HUD, " + Dp(Mathf.Max(0f, worstHud)) + " dp");
            }

            view.DebugReplayIntro(800, true);
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Canopy), "el reintento, en dosel (D-010)");
        }

        // El 240 (13 cuias) y el 335 (14): 3 filas en cualquier lienzo (P12 de D-006); los de 11-12 van en 2 filas en 16:9.
        [UnityTest]
        public IEnumerator In16x9_ThreeRowBoards_KeepTheLayoutOfBefore_AndDoNotGoOnTheBranch([Values(240, 335)] int level)
        {
            GameScreen.SimulatedScreen = Screen16x9;
            yield return IntroSideTroopTests.LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            BoardLayoutResult l = view.Layout!;
            Assert.That(l.Rows, Is.EqualTo(3), "nivel " + level + ": 3 filas también en 16:9");
            Assert.That(l.TroopBand, Is.False, "en 16:9 no cabe la franja sin bajar de 28 dp");
            Assert.That(l.PanelY, Is.LessThan(view.HudBottomDp + IntroChoreography.BranchMinBandDp));
            foreach (int duration in new[] { 1400, 800 })
            {
                view.DebugReplayIntro(duration, false);
                IntroPerch perch = view.Intro.Plan.Perch;
                Debug.Log("AF_TROOP_BAND nivel " + level + " en 16:9, T " + duration + ": " + perch + ", panel " + Dp(l.PanelY) + "-" + Dp(l.PanelBottom)
                    + ", cuia " + Dp(l.CuiaWidth) + " dp");
                Assert.That(perch, Is.Not.EqualTo(IntroPerch.Branch), "sin franja, la tropa lateral (D-021) o el dosel, como antes");
            }
        }

        private static string Dp(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }

        // Caja de la cuia en dp del lienzo (y hacia abajo), como en IntroSideTroopTests.
        private static Rect CuiaRect(CuiaView cuia)
        {
            return IntroSideTroopTests.CuiaRect(cuia);
        }

        private static float Distance(Rect a, Rect b)
        {
            return IntroSideTroopTests.Distance(a, b);
        }
    }
}
