#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La fila «Bitácora · elige tu premio» del panel único de la victoria (D-033, añadido del 2026-10-20, puntos 5 y 10; B4, Q-B11;
    /// cierre de B11; <c>GameScreen.LogbookRow.cs</c>) en la escena real: sale en la victoria del primer nivel del día, abre la libreta
    /// encima y vuelve a la victoria; se va al sellar la casilla; no sale en el 10 (su secuencia la abre después) ni cuando con ella
    /// la victoria compacta necesitaría el escalón 3, donde iría tras el chevron (hoy, en 360 × 640 dp, siempre: medido), y cuando
    /// sale es un objetivo de 48 dp dentro del panel que no pisa ningún botón. Las demás victorias no cambian de escalón (la fila
    /// solo cuenta en la que la lleva).
    /// </summary>
    public sealed class LogbookVictoryRowTests
    {
        [SetUp]
        public void SetUp()
        {
            GameBootstrap.LogbookOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.ResetLogbookOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator TheFirstVictoryOfTheDay_HasTheRow_ItOpensTheNotebook_AndGoesAwayOnceSealed()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return FactLineTests.Win(26);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(boot.LogbookVictoryRowDue, Is.True, "D-033, punto 5: el primer nivel del día deja la casilla lista");
            Assert.That(view.VictoryLogbookShown, Is.True, "la fila «Bitácora · elige tu premio»");
            Assert.That(view.VictoryBarsView.LogbookRowText, Is.EqualTo("Bitácora · elige tu premio"), "texto de D-033, punto 5");
            Assert.That(view.VictoryCompact, Is.EqualTo(0), "a 360 × 780 no compacta");
            AssertRowFits(view, "26 a 360 × 780");
            float panelWith = view.VictoryPanelHeight;

            // Cerrar sin elegir: la casilla queda pendiente y la fila sigue (D-033, punto 4).
            view.VictoryBarsView.LogbookButton.onClick.Invoke();
            LogbookScreen screen = boot.LogbookScreen!;
            Assert.That(screen.Visible, Is.True, "la libreta, encima de la victoria");
            Assert.That(screen.Source, Is.EqualTo(LogbookOpenSource.Victory));
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue debajo");
            Assert.That(view.VictoryLogbookOpened, Is.EqualTo(1));
            screen.CloseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.VictoryLogbookShown, Is.True, "sin elegir, la fila sigue");

            // Elegir: la casilla se sella y, al volver, la fila se va y la victoria se recoloca.
            view.VictoryBarsView.LogbookButton.onClick.Invoke();
            Assert.That(screen.PressToday(), Is.True);
            screen.CardButton(LoginCalendarChoice.Seeds).onClick.Invoke();
            Assert.That(boot.LogbookState, Is.EqualTo(LoginCalendarState.Sealed));
            screen.CloseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.VictoryLogbookShown, Is.False, "sellada hoy: la fila se va");
            Assert.That(view.VictoryPanelHeight, Is.LessThan(panelWith - 40f), "el panel pierde la fila");
            Assert.That(view.VictoryLogbookOpened, Is.EqualTo(2));
            Assert.That(boot.PressNext(), Is.True, "y la victoria sigue como siempre");
        }

        [UnityTest]
        public IEnumerator WithoutAPendingPage_OrInTheVictoryOf10_ThereIsNoRow()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            GameBootstrap.LogbookOverride = false;
            yield return FactLineTests.Win(26);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LogbookVictoryRowDue, Is.False, "sin Bitácora");
            Assert.That(boot.View!.VictoryLogbookShown, Is.False);

            GameBootstrap.LogbookOverride = true;
            yield return FactLineTests.Win(10);
            boot = GameBootstrap.Current!;
            Assert.That(boot.LogbookVictoryRowDue, Is.True, "con el 10 ganado, la casilla está lista");
            Assert.That(boot.View!.VictoryLogbookShown, Is.False, "la secuencia del 10 abre la libreta después (D-033, punto 11)");
        }

        [UnityTest]
        public IEnumerator In16x9_TheRowFits_OrStaysOutRatherThanBehindTheChevron(
            [Values(LocLocale.SpanishLatinAmerica, LocLocale.Pseudo)] string locale, [Values(1f, 1.3f)] float scale)
        {
            string what = "26 en " + locale + " al " + Mathf.RoundToInt(scale * 100f) + " % a 360 × 640";
            yield return FactLineTests.Win(26, null, locale, scale, true);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(boot.LogbookVictoryRowDue, Is.True, what);
            TestContext.WriteLine(what + ": escalón " + view.VictoryCompact + ", fila " + (view.VictoryLogbookShown ? "sí" : "no")
                + ", panel " + view.VictoryPanelHeight + " dp (tope " + view.VictoryPanelBudgetDp + ")");
            Fx16bTests.AssertCompactVictory(view, what);
            Assert.That(view.VictoryBarsView.ChevronShown, Is.False, what + ": la fila nunca va tras el chevron");
            if (view.VictoryLogbookShown)
            {
                Assert.That(view.VictoryCompact, Is.LessThan(GameScreen.VictoryCompactRow), what);
                AssertRowFits(view, what);
            }
            else
            {
                // Con la fila haría falta el escalón 3, donde iría tras el chevron: no sale y el escalón es el de sin ella [P].
                Assert.That(view.VictoryBarsView.LogbookSuppressed, Is.True, what + ": solo falta si con ella haría falta el escalón 3");
            }
        }

        // La fila: 48 dp de alto, dentro del panel, el texto en una línea de 12 sp o más, sin pisar ningún botón de la victoria.
        internal static void AssertRowFits(GameScreen view, string what)
        {
            RectTransform panel = view.VictoryPanelRect;
            Rect row = VictorySetup.CanvasRect((RectTransform)view.VictoryBarsView.LogbookButton.transform, panel);
            Assert.That(row.height, Is.GreaterThanOrEqualTo(48f - 0.01f), what + ": la fila, objetivo de 48 dp");
            Assert.That(row.xMin, Is.GreaterThanOrEqualTo(-0.01f), what + ": dentro del panel");
            Assert.That(row.xMax, Is.LessThanOrEqualTo(view.VictoryPanelWidthDp + 0.01f), what + ": dentro del panel");
            Assert.That(row.yMin, Is.GreaterThanOrEqualTo(-0.01f), what + ": dentro del panel");
            Assert.That(row.yMax, Is.LessThanOrEqualTo(view.VictoryPanelHeight + 0.01f), what + ": dentro del panel");
            var buttons = new List<Button> { view.NextButton, view.HomeButton };
            if (view.DoubleHarvestShown)
            {
                buttons.Add(view.DoubleHarvestButton);
            }

            foreach (Button b in buttons)
            {
                Rect r = VictorySetup.CanvasRect((RectTransform)b.transform, panel);
                bool overlap = row.xMin < r.xMax - 0.01f && r.xMin < row.xMax - 0.01f && row.yMin < r.yMax - 0.01f && r.yMin < row.yMax - 0.01f;
                Assert.That(overlap, Is.False, what + ": la fila no pisa " + b.name);
            }

            foreach (RectTransform bar in new[] { view.VictoryBarsView.StreakRect, view.VictoryBarsView.BasketRect })
            {
                if (!bar.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect r = VictorySetup.CanvasRect(bar, panel);
                Assert.That(r.yMax, Is.LessThanOrEqualTo(row.yMin + 0.01f), what + ": " + bar.name + " encima de la fila");
            }

            TMPro.TextMeshProUGUI label = view.VictoryBarsView.LogbookRect.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * UiBuilder.TextScale) - 0.01f), what + ": nunca menos de 12 sp");
            Assert.That(label.textWrappingMode, Is.EqualTo(TMPro.TextWrappingModes.NoWrap), what + ": en una línea");
        }
    }
}
