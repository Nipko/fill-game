#nullable enable
using System.Collections;
using AF.Boosters;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarea FX16a (decisión del dueño del 2026-10-20: «compactar Aguas estancadas … en 16:9»; 02 §7: «el tablero queda entero a
    /// la vista»; DC3, D-028). En 16:9 (360 × 640 dp), en es-419 y en en-XA, el panel de Aguas estancadas pasa a su disposición
    /// compacta cuando la de siempre taparía alguna cuia con el tablero subido hasta su tope, y siempre cabe en la zona segura
    /// (al 130 %, la rama B medía 597 dp, más que los 576 entre las zonas seguras):
    /// <list type="bullet">
    /// <item><description>en el 31 (una fila), ninguna cuia tapada en la rama A al 100 % y al 130 % ni en la rama B al
    /// 100 %;</description></item>
    /// <item><description>en la rama B al 130 % (con Puñado, Balsa, su línea y «Rendirse»), ni el panel compacto cabe bajo las
    /// cuias: tapa su parte de abajo y se registra (<see cref="Stuck_31_B130_16x9_NoCuiaCovered_KnownIssue"/>, pendiente del
    /// dueño);</description></item>
    /// <item><description>en el 640 (14 cuias en 3 filas, 5/5/4), con el tablero subido hasta su tope, el pie de la tercera
    /// fila queda a 438 dp: ni la rama A cabe debajo; se registra lo que tapa
    /// (<see cref="Stuck_640_16x9_NoCuiaCovered_KnownIssue"/>, pendiente del dueño);</description></item>
    /// <item><description>a 360 × 780 no cambia nada: la disposición de siempre, sin límite de alto.</description></item>
    /// </list>
    /// Los textos (≥ 12 sp, sin desbordes) los miran <c>MinTextSizeTests</c> en es-419 y <c>PseudoLocOverflowTests</c> en en-XA.
    /// </summary>
    public sealed class StuckCompactTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            Release();
        }

        [UnityTest]
        public IEnumerator Stuck_31_A_16x9_NoCuiaCovered_InsideTheSafeArea([Values(100, 130)] int percent, [Values("es-419", "en-XA")] string locale)
        {
            Use(HudLabelTests.Screen640, percent, locale);
            yield return Load31("A");
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 31, rama A, 360 × 640 al " + percent + " % en " + locale;
            AssertBranch(view, "A", where);
            Assert.That(CoveredDp(view, where), Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7)");
            AssertInsideSafeArea(view, where);
        }

        [UnityTest]
        public IEnumerator Stuck_31_B100_16x9_Compact_NoCuiaCovered_InsideTheSafeArea([Values("es-419", "en-XA")] string locale)
        {
            Use(HudLabelTests.Screen640, 100, locale);
            yield return Load31("B");
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 31, rama B, 360 × 640 al 100 % en " + locale;
            AssertBranch(view, "B", where);
            Assert.That(view.Stuck.Compact, Is.True, where + ": la disposición de siempre (413 dp) tapaba las cuias; va compacta");
            Assert.That(view.Stuck.BottomGapDp, Is.EqualTo(StuckModal.CompactBottomGapDp), where);
            Assert.That(CoveredDp(view, where), Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7)");
            AssertInsideSafeArea(view, where);
        }

        [UnityTest]
        public IEnumerator Stuck_31_B130_16x9_Compact_InsideTheSafeArea_RecordsWhatItCovers([Values("es-419", "en-XA")] string locale)
        {
            Use(HudLabelTests.Screen640, 130, locale);
            yield return Load31("B");
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 31, rama B, 360 × 640 al 130 % en " + locale;
            AssertBranch(view, "B", where);
            Assert.That(view.Stuck.Compact, Is.True, where + ": va compacta");
            CoveredDp(view, where);
            AssertInsideSafeArea(view, where);
            AssertRaisedToTheTop(view, where);
        }

        /// <summary>
        /// Pendiente del dueño (FX16a): en el 31, rama B, en 16:9 al 130 %, ninguna cuia tapada. Hoy falla: ver
        /// <see cref="Stuck_31_B130_16x9_Compact_InsideTheSafeArea_RecordsWhatItCovers"/>.
        /// </summary>
        [UnityTest]
        [Explicit("Pendiente del dueño (FX16a): en 16:9 al 130 %, el panel compacto de la rama B aún tapa la parte de abajo de las cuias del 31; hoy falla.")]
        public IEnumerator Stuck_31_B130_16x9_NoCuiaCovered_KnownIssue([Values("es-419", "en-XA")] string locale)
        {
            Use(HudLabelTests.Screen640, 130, locale);
            yield return Load31("B");
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 31, rama B, 360 × 640 al 130 % en " + locale;
            Assert.That(CoveredDp(view, where), Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7)");
        }

        [UnityTest]
        public IEnumerator Stuck_31_360x780_Unchanged([Values("A", "B")] string branch, [Values(100, 130)] int percent)
        {
            Use(HudLabelTests.Screen780, percent, LocLocale.SourceLocale);
            yield return Load31(branch);
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 31, rama " + branch + ", 360 × 780 al " + percent + " %";
            AssertBranch(view, branch, where);
            Assert.That(view.StuckRoomDp, Is.EqualTo(float.PositiveInfinity), where + ": sin límite de alto");
            Assert.That(view.Stuck.Compact, Is.False, where + ": la disposición de siempre");
            Assert.That(view.Stuck.BottomGapDp, Is.EqualTo(StuckModal.RegularBottomGapDp), where);
            CoveredDp(view, where);
        }

        [UnityTest]
        public IEnumerator Stuck_640_16x9_Compact_InsideTheSafeArea_RecordsWhatItCovers(
            [Values("A", "B")] string branch, [Values(100, 130)] int percent)
        {
            Use(HudLabelTests.Screen640, percent, LocLocale.SourceLocale);
            yield return Load640(branch);
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 640 (14 cuias), rama " + branch + ", 360 × 640 al " + percent + " %";
            AssertBranch(view, branch, where);
            Assert.That(view.Stuck.Compact, Is.True, where + ": va compacta");
            CoveredDp(view, where);
            AssertInsideSafeArea(view, where);
            AssertRaisedToTheTop(view, where);
        }

        /// <summary>
        /// Pendiente del dueño (FX16a): en el 640 (3 filas) en 16:9, ninguna cuia tapada. Hoy falla: ver
        /// <see cref="Stuck_640_16x9_Compact_InsideTheSafeArea_RecordsWhatItCovers"/>.
        /// </summary>
        [UnityTest]
        [Explicit("Pendiente del dueño (FX16a): en el 640 (3 filas) en 16:9, el panel compacto aún tapa la tercera fila; hoy falla.")]
        public IEnumerator Stuck_640_16x9_NoCuiaCovered_KnownIssue([Values("A", "B")] string branch, [Values(100, 130)] int percent)
        {
            Use(HudLabelTests.Screen640, percent, LocLocale.SourceLocale);
            yield return Load640(branch);
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 640 (14 cuias), rama " + branch + ", 360 × 640 al " + percent + " %";
            Assert.That(CoveredDp(view, where), Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7)");
        }

        // ---- Ayudas (también para StuckCompactSnapshotTests) ----

        internal static void Use(Vector4 screen, int percent, string locale)
        {
            GameScreen.SimulatedScreen = screen;
            LaunchOptions.FontScaleOverride = percent / 100f;
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }
        }

        internal static void Release()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (Loc.IsConfigured && Loc.Locale != LocLocale.SourceLocale)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        // El 31 (el tablero 6 del tutorial con el desplazamiento de 25) bloqueado: rama A con 1.000 semillas (el rescate a la
        // venta, primero del día) y rama B sin semillas y con la última canoa (el rescate en gris, Puñado y Balsa).
        internal static IEnumerator Load31(string branch)
        {
            bool a = branch == "A";
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(a ? 1000 : 0, a ? 5 : 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 500); // el tablero termina de subir
        }

        // El 640 (14 cuias, 5/5/4) bloqueado por el camino del buscador de DC18 (un vertido): rama A con 5.000 semillas y rama
        // B sin semillas.
        internal static IEnumerator Load640(string branch)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(branch == "A" ? 5000 : 0));
            yield return Canvas16x9LayoutTests.LoadPlaying(640);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.Views.Length, Is.EqualTo(14), "condición del test: 14 cuias");
            Pour[]? path = Dc18SnapshotTests.FindRescuableStuck(boot.Flow!);
            Assert.That(path, Is.Not.Null, "el 640 llega a Aguas estancadas");
            MechanicTestKit.PlayPrefix(boot, path!, false);
            for (int ms = 0; ms < 8000 && boot.Flow!.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 800);
        }

        private static void AssertBranch(GameScreen view, string branch, string where)
        {
            Assert.That(view.StuckVisible, Is.True, where);
            StuckRescueKind rescue = view.Stuck.View!.Rescue;
            if (branch == "A")
            {
                Assert.That(rescue, Is.EqualTo(StuckRescueKind.Available), where + ": condición del test, el rescate a la venta");
            }
            else
            {
                Assert.That(rescue, Is.EqualTo(StuckRescueKind.Disabled), where + ": condición del test, el rescate en gris");
                Assert.That(view.Stuck.HandfulButton.gameObject.activeSelf || view.Stuck.BalsaButton.gameObject.activeSelf, Is.True, where + ": Puñado o Balsa");
            }
        }

        // Lo más que el panel tapa de una cuia (dp, de su boca a su pie, con el tablero en su sitio), y lo registra.
        internal static float CoveredDp(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            BoardLayoutResult l = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            Rect panel = VictorySetup.CanvasRect(view.Stuck.Panel, (RectTransform)view.Canvas.transform);
            float worst = 0f;
            float lowestFoot = float.MinValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                float foot = l.FootY[i] - shift;
                float mouth = foot - (view.Views[i].HeightU * view.Views[i].Unit);
                lowestFoot = Mathf.Max(lowestFoot, foot);
                worst = Mathf.Max(worst, Mathf.Clamp(foot - panel.yMin, 0f, foot - mouth));
            }

            TestContext.WriteLine(where + ": panel " + (view.Stuck.Compact ? "compacto" : "de siempre") + " de " + panel.height.ToString("0.0")
                + " dp desde " + panel.yMin.ToString("0.0") + " dp; tablero subido " + shift.ToString("0.0") + " dp; pie de las cuias en "
                + lowestFoot.ToString("0.0") + " dp: " + (worst <= 0.05f ? "ninguna cuia tapada" : "tapa hasta " + worst.ToString("0.0") + " dp de una cuia") + ".");
            return worst;
        }

        // Si el panel tapa alguna cuia, el tablero subió hasta su tope (07 §5.5: «sube lo justo para que el panel no lo tape»):
        // la cuia más alta, a la altura del borde de abajo del HUD.
        private static void AssertRaisedToTheTop(GameScreen view, string where)
        {
            BoardLayoutResult l = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            float highest = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                highest = Mathf.Min(highest, l.FootY[i] - (view.Views[i].HeightU * view.Views[i].Unit) - CuiaView.LiftDp);
            }

            Assert.That(highest - shift, Is.EqualTo(view.TopRowDp + 48f).Within(0.1f), where + ": el tablero, subido hasta su tope");
        }

        private static void AssertInsideSafeArea(GameScreen view, string where)
        {
            Rect panel = VictorySetup.CanvasRect(view.Stuck.Panel, (RectTransform)view.Canvas.transform);
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + ": sobre la zona segura de abajo");
        }
    }
}
