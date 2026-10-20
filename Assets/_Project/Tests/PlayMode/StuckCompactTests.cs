#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    /// la vista»; 07 §5.5: «las cuias siguen visibles sobre el modal»; DC3, D-028). En 16:9 (360 × 640 dp), en es-419 y en en-XA,
    /// al 100 % y al 130 %, el panel de Aguas estancadas pasa a su disposición compacta cuando la de siempre taparía alguna cuia
    /// con el tablero subido hasta su tope, cabe en la zona segura y no tapa ninguna cuia: si ni así caben sobre el panel (la
    /// rama B del 31 al 130 % y el 640, de 14 cuias en 3 filas), el tablero se reduce mientras el modal esté abierto
    /// (<c>GameScreen.FitBoardToStuck</c>), con su parte de arriba bajo el HUD. Lo comprueban, con la geometría de verdad del
    /// tablero (su desplazamiento y su escala):
    /// <list type="bullet">
    /// <item><description>el 31 en las ramas A y B y el 640 en las ramas A y B, al 100 % y al 130 %: ninguna cuia tapada, el panel
    /// en la zona segura y, si el tablero se redujo, no más que <c>GameScreen.MinStuckBoardScale</c> y con lo más alto de las
    /// cuias (con su elevación) en el borde de abajo del HUD;</description></item>
    /// <item><description>a 360 × 780 no cambia nada: la disposición de siempre, sin límite de alto, con el alto del panel y de sus
    /// botones de antes de FX16a y el tablero a su tamaño.</description></item>
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
        public IEnumerator Stuck_31_16x9_NoCuiaCovered_InsideTheSafeArea(
            [Values("A", "B")] string branch, [Values(100, 130)] int percent, [Values("es-419", "en-XA")] string locale)
        {
            Use(HudLabelTests.Screen640, percent, locale);
            yield return Load31(branch);
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 31, rama " + branch + ", 360 × 640 al " + percent + " % en " + locale;
            AssertBranch(view, branch, where);
            if (branch == "B")
            {
                Assert.That(view.Stuck.Compact, Is.True, where + ": la disposición de siempre (413 dp al 100 %) tapaba las cuias; va compacta");
                Assert.That(view.Stuck.BottomGapDp, Is.EqualTo(StuckModal.CompactBottomGapDp), where);
            }

            Assert.That(CoveredDp(view, where), Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7)");
            AssertInsideSafeArea(view, where);
            AssertBoardUnderTheHud(view, where);
        }

        [UnityTest]
        public IEnumerator Stuck_640_16x9_NoCuiaCovered_InsideTheSafeArea([Values("A", "B")] string branch, [Values(100, 130)] int percent)
        {
            Use(HudLabelTests.Screen640, percent, LocLocale.SourceLocale);
            yield return Load640(branch);
            GameScreen view = GameBootstrap.Current!.View!;
            string where = "el 640 (14 cuias), rama " + branch + ", 360 × 640 al " + percent + " %";
            AssertBranch(view, branch, where);
            Assert.That(view.Stuck.Compact, Is.True, where + ": va compacta");
            Assert.That(view.BoardScale, Is.LessThan(1f), where + ": con 3 filas, ni compacto cabe bajo el tablero a su tamaño: se reduce");
            Assert.That(CoveredDp(view, where), Is.LessThanOrEqualTo(0.05f), where + ": ninguna cuia tapada (02 §7)");
            AssertInsideSafeArea(view, where);
            AssertBoardUnderTheHud(view, where);
        }

        /// <summary>
        /// A 360 × 780 nada cambia con FX16a: la disposición de siempre, el tablero a su tamaño, el alto del panel (316 y 413 dp al
        /// 100 %, 371,8 y 597 dp al 130 %, ramas A y B, los de antes de la tarea) y el sitio y el alto de cada botón.
        /// </summary>
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
            Assert.That(view.BoardScale, Is.EqualTo(1f), where + ": el tablero a su tamaño");
            CoveredDp(view, where);

            float expected = branch == "A" ? (percent > 100 ? 371.8f : 316f) : (percent > 100 ? 597f : 413f);
            Assert.That(view.Stuck.Height, Is.EqualTo(expected).Within(0.15f), where + ": el alto del panel de antes de FX16a");
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            view.Stuck.VisibleButtons(ids, rects);
            var got = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                RectTransform rt = rects[i];
                got.Append(ids[i]).Append(' ').Append((-rt.anchoredPosition.y).ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(' ').Append(rt.rect.height.ToString("0.0", CultureInfo.InvariantCulture)).Append(';');
            }

            TestContext.WriteLine(where + ": " + got);
            Assert.That(got.ToString(), Is.EqualTo(Regular780Buttons(branch, percent)),
                where + ": el sitio (y desde arriba del panel) y el alto de cada botón, los de antes de FX16a");
        }

        // Sitio (y desde arriba del panel, dp) y alto de cada botón de la disposición de siempre a 360 × 780, con sus ids de
        // AF_LAYOUT: los que da la ruta de siempre de StuckModal.Lay, registrados con la revisión de FX16a (con los mismos altos de
        // panel que en 815b2e5, antes de la tarea: 316, 413, 371,8 y 597 dp). Si cambian, cambió la disposición de siempre.
        private static string Regular780Buttons(string branch, int percent)
        {
            if (branch == "A")
            {
                return percent > 100 ? "undo_modal 120.0 77.2;rescue 205.2 77.2;give_up 315.8 48.0;" : "undo_modal 108.0 58.0;rescue 174.0 58.0;give_up 260.0 48.0;";
            }

            return percent > 100 ? "undo_modal 120.0 77.2;rescue 205.2 77.2;handful 315.8 83.1;balsa 406.9 83.1;balsa_note 490.0 43.0;give_up 541.0 48.0;" : "undo_modal 108.0 58.0;rescue 174.0 58.0;handful 260.0 56.0;balsa 260.0 56.0;balsa_note 316.0 33.0;give_up 357.0 48.0;";
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

        // Lo más que el panel tapa de una cuia (dp, de su boca a su pie, con el tablero en su sitio: su desplazamiento y su
        // escala, por sus transformadas de verdad), y lo registra.
        internal static float CoveredDp(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            BoardLayoutResult l = view.Layout!;
            Rect panel = VictorySetup.CanvasRect(view.Stuck.Panel, (RectTransform)view.Canvas.transform);
            float worst = 0f;
            float lowestFoot = float.MinValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                float foot = BoardToCanvasY(view, l.FootY[i]);
                float mouth = BoardToCanvasY(view, l.FootY[i] - (view.Views[i].HeightU * view.Views[i].Unit));
                lowestFoot = Mathf.Max(lowestFoot, foot);
                worst = Mathf.Max(worst, Mathf.Clamp(foot - panel.yMin, 0f, foot - mouth));
            }

            TestContext.WriteLine(where + ": panel " + (view.Stuck.Compact ? "compacto" : "de siempre") + " de " + panel.height.ToString("0.0")
                + " dp desde " + panel.yMin.ToString("0.0") + " dp; tablero subido " + view.BoardContainer.anchoredPosition.y.ToString("0.0")
                + " dp a escala " + view.BoardScale.ToString("0.000") + "; pie de las cuias en " + lowestFoot.ToString("0.0") + " dp: "
                + (worst <= 0.05f ? "ninguna cuia tapada" : "tapa hasta " + worst.ToString("0.0") + " dp de una cuia") + ".");
            var parts = new StringBuilder();
            foreach (Transform child in view.Stuck.Panel)
            {
                if (child.gameObject.activeSelf && child is RectTransform rt)
                {
                    parts.Append(child.name).Append(' ').Append((-rt.anchoredPosition.y).ToString("0.0", CultureInfo.InvariantCulture))
                        .Append('+').Append(rt.rect.height.ToString("0.0", CultureInfo.InvariantCulture)).Append("; ");
                }
            }

            TestContext.WriteLine(where + ": piezas del panel (y desde arriba + alto, dp): " + parts);
            if (view.Stuck.HandfulButton.gameObject.activeSelf)
            {
                TestContext.WriteLine(where + ": Puñado «" + view.Stuck.HandfulLabel.text.Replace("\n", " / ") + "» en " + view.Stuck.HandfulLabel.textInfo.lineCount
                    + " líneas a " + view.Stuck.HandfulLabel.fontSize.ToString("0.0") + "; Balsa «" + view.Stuck.BalsaLabel.text.Replace("\n", " / ") + "» en "
                    + view.Stuck.BalsaLabel.textInfo.lineCount + " líneas a " + view.Stuck.BalsaLabel.fontSize.ToString("0.0") + ".");
            }
            return worst;
        }

        /// <summary>
        /// Del alto <paramref name="boardY"/> (dp desde arriba en las coordenadas del tablero: las de <c>BoardLayoutResult</c>) al
        /// del lienzo (dp desde arriba), por las transformadas de verdad del contenedor del tablero (desplazamiento y escala).
        /// </summary>
        internal static float BoardToCanvasY(GameScreen view, float boardY)
        {
            RectTransform board = view.BoardContainer;
            var root = (RectTransform)view.Canvas.transform;
            Rect r = board.rect;
            Vector3 world = board.TransformPoint(new Vector3(r.center.x, r.yMax - boardY, 0f));
            return root.rect.yMax - root.InverseTransformPoint(world).y;
        }

        // Lo más alto de las cuias (con su elevación) queda bajo el borde de abajo del HUD; si el tablero se redujo, justo en él
        // (lo justo), y nunca por debajo de GameScreen.MinStuckBoardScale.
        private static void AssertBoardUnderTheHud(GameScreen view, string where)
        {
            BoardLayoutResult l = view.Layout!;
            float highest = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                highest = Mathf.Min(highest, l.FootY[i] - (view.Views[i].HeightU * view.Views[i].Unit) - CuiaView.LiftDp);
            }

            float top = BoardToCanvasY(view, highest);
            float hud = view.TopRowDp + 48f;
            TestContext.WriteLine(where + ": tablero a escala " + view.BoardScale.ToString("0.000") + ", lo más alto de las cuias en "
                + top.ToString("0.0") + " dp (HUD hasta " + hud.ToString("0.0") + " dp).");
            Assert.That(top, Is.GreaterThanOrEqualTo(hud - 0.1f), where + ": las cuias, bajo el HUD");
            if (view.BoardScale < 1f)
            {
                Assert.That(view.BoardScale, Is.GreaterThanOrEqualTo(GameScreen.MinStuckBoardScale), where + ": escala mínima");
                Assert.That(top, Is.EqualTo(hud).Within(0.1f), where + ": reducido lo justo, con lo más alto en el borde del HUD");
            }
        }

        private static void AssertInsideSafeArea(GameScreen view, string where)
        {
            Rect panel = VictorySetup.CanvasRect(view.Stuck.Panel, (RectTransform)view.Canvas.transform);
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + ": bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + ": sobre la zona segura de abajo");
        }
    }
}
