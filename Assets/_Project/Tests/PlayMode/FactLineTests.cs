#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La línea de fruta de Frutas del río en la ranura del Dato del río de la victoria (D-017; 02 §6, fila «Dato del río»;
    /// propuesta de Frutas §2.2, §2.5, §2.6, §2.8 y §2.10; arquitectura de la tanda 4, §2; tarea F3), en la escena, con el
    /// calendario real (<c>frutas_del_rio_v1.json</c>), las paradas reales y los textos de <c>facts.csv</c>:
    /// <list type="bullet">
    /// <item>tiempo: entra con «Siguiente» (a los 600 ms de la pantalla de victoria, la fila 1,40 s) con un fundido de 200 ms, sin
    /// él con Reducir movimiento, y desde entonces toda la línea es un botón (F4: abre la ficha; FruitSheetTests);</item>
    /// <item>la línea del 3 («Dato del río», el punto medio, «Açaí · 3/20» y su texto, arriba de la ranura) y el sello de la
    /// última línea de una serie;</item>
    /// <item>ranura fija: «Siguiente» en el mismo sitio con la línea más corta y la más larga de 1-200 de cada idioma y la más
    /// alta de cada idioma y escala, en el 3 (sin Cosecha doble ni chip) y en el 39 (con Cosecha doble y el chip), en es-419 y
    /// en-XA, al 100 % y al 130 %, a 360 × 640, con el panel y la cinta bajo la cabecera;</item>
    /// <item>las medidas de la ranura se hacen al cargar el nivel, no en la victoria;</item>
    /// <item>en 1-200, cada victoria lleva su línea salvo donde el árbitro dice otra cosa (los <c>10k</c> con dato de llegada), y
    /// todas caben sin recortes en es-419 al 100 % y en en-XA al 130 %;</item>
    /// <item>el 20 (un <c>10k</c>) lleva la llegada y no la línea de fruta; el 10, la línea sin el Mapa y la ranura vacía con él;</item>
    /// <item>la placa del lugar sin dato y la línea de fruta juntas, como dice el árbitro (arquitectura de la tanda 4, §2): el 20
    /// con SingleKey y el 30 con Canon y otra cadencia del dato de llegada; y el 20 con SingleKey en un idioma sin la línea: la
    /// placa, centrada;</item>
    /// <item>cadencias 0, 1 y 2 (<c>ui_fruit_line_every_n_levels</c>);</item>
    /// <item>sin mezcla de idiomas: en un idioma sin la línea traducida, no sale nada; con la línea traducida pero sin la
    /// cabecera, tampoco; en release, nada sin aprobar;</item>
    /// <item>sonda: <c>river_fact_shown</c> con <c>fact_id</c> = <c>&lt;tinte&gt;.&lt;kk&gt;</c> desde el punto común (Siguiente o
    /// Inicio), con <c>dismissed_ms</c> desde que entra la línea, y <c>AF_STATE fruit_line</c>.</item>
    /// </list>
    /// </summary>
    public sealed class FactLineTests
    {
        private const string AlterFact = "map.stop.alter_do_chao.dato_01";

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica || !Loc.ShowUnapproved
                || !ReferenceEquals(Loc.Catalog, LocSetUpFixture.Catalog))
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator Timing_EntersWithNext_WithA200msFade_OrWithoutItWithReducedMotion_AndTakesNoTouches()
        {
            foreach (bool reduce in new[] { false, true })
            {
                yield return Win(3, v => GameBootstrap.Current!.Flow!.ReduceMotion = reduce);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                FactLine line = view.FruitLine!;
                VictoryLineSlot slot = view.VictorySlot;
                Assert.That(line.Shown, Is.True, "el 3 lleva su línea");
                Assert.That(line.ReduceMotion, Is.EqualTo(reduce));
                Assert.That(slot.SinceShownMs, Is.EqualTo(0f));
                Assert.That(line.Alpha, Is.EqualTo(0f), "nada al abrirse la victoria");

                PlayModeHelpers.TickExact(boot, (int)VictoryLineSlot.FruitRevealMs);
                Assert.That(line.Alpha, Is.EqualTo(0f), "nada antes de Siguiente (fila 1,40 s de 07 §5.4)");
                Assert.That(line.Touchable, Is.False, "invisible: sin toques (F4)");
                Assert.That(view.NextAlpha, Is.EqualTo(0f));
                PlayModeHelpers.TickExact(boot, 100);
                Assert.That(view.NextAlpha, Is.GreaterThan(0f), "Siguiente entra a la vez");
                if (reduce)
                {
                    Assert.That(line.Alpha, Is.EqualTo(1f), "con Reducir movimiento, sin fundido");
                }
                else
                {
                    Assert.That(line.Alpha, Is.EqualTo(0.5f).Within(0.01f), "a mitad del fundido de 200 ms");
                    PlayModeHelpers.TickExact(boot, 100);
                    Assert.That(line.Alpha, Is.EqualTo(1f));
                }

                // Desde F4 toda la línea es un botón (abre la ficha; FruitSheetTests): solo su área táctil recibe toques, desde que
                // se ve, y Siguiente funciona.
                Assert.That(view.NextButton.interactable, Is.True);
                foreach (Graphic g in line.Root!.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(g.raycastTarget, Is.EqualTo(g.transform == line.TapArea), g.name + ": solo el área táctil (F4)");
                }

                Assert.That(line.Root.GetComponent<CanvasGroup>().blocksRaycasts, Is.True, "a la vista, recibe toques (F4)");

                var ids = new List<string>();
                var rects = new List<RectTransform>();
                line.LayoutIds(ids, rects);
                Assert.That(ids, Is.EqualTo(new[] { FactLine.TapId }), "entra en AF_LAYOUT victory (F4)");
                PlayModeHelpers.Step(boot, 2000);
                Assert.That(line.Alpha, Is.EqualTo(1f), "se queda hasta cerrar la victoria");
            }
        }

        [UnityTest]
        public IEnumerator Level3_HeaderIconCounterAndText()
        {
            yield return Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1000);
            FactLine line = view.FruitLine!;
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.EqualTo("fact.acai.03"), "el árbitro da la línea del 3");
            Assert.That(line.Shown, Is.True);
            Assert.That(line.Key, Is.EqualTo("fact.acai.03"));
            Assert.That(line.ShownFactId, Is.EqualTo("acai.03"));
            Assert.That(line.Position.K, Is.EqualTo(3));
            Assert.That(line.Position.Lines, Is.EqualTo(20));
            Assert.That(line.HeaderText, Is.EqualTo("Dato del río"), "river_fact");
            Assert.That(line.ProgressText, Is.EqualTo("Açaí · 3/20"), "fact_progress con tint.acai.name");
            Assert.That(line.BodyText, Is.EqualTo(Loc.Fact("fact.acai.03")));
            Assert.That(line.BodyText, Does.StartWith("Crece en matas"));
            Assert.That(line.Sealed, Is.False);
            Assert.That(line.Seal!.gameObject.activeSelf, Is.False);
            Assert.That(line.HeaderWrapped, Is.False, "al 100 % en es-419, la cabecera va en una fila");
            Assert.That(line.HeaderDot!.gameObject.activeSelf, Is.True, "«Dato del río» · «Açaí · 3/20», seguidos en la misma fila");
            Assert.That(line.OffsetDp, Is.LessThanOrEqualTo(FactLine.TopAirDp), "arriba de la ranura, cerca del filete");
            Assert.That(line.HeaderHeightDp, Is.EqualTo(VictoryLineSlot.HeaderDp), "cabecera de 32 dp");
            Assert.That(line.Icon!.sprite, Is.Not.Null, "el icono de la fruta");
            Assert.That(line.Icon.rectTransform.rect.width, Is.EqualTo(FactLine.IconDp));
            Image back = line.IconBackDisc!;
            Assert.That(back.gameObject.activeSelf, Is.True, "un disco bajo el icono, para las frutas claras");
            Assert.That(back.rectTransform.rect.width, Is.EqualTo(FactLine.IconBackDp));
            Assert.That(back.rectTransform.GetSiblingIndex(), Is.LessThan(line.Icon.rectTransform.GetSiblingIndex()), "el disco, debajo del icono");
            Assert.That(back.rectTransform.rect.center + back.rectTransform.anchoredPosition,
                Is.EqualTo(line.Icon.rectTransform.rect.center + line.Icon.rectTransform.anchoredPosition), "centrado en el icono");
            Assert.That(back.raycastTarget, Is.False);
            Assert.That(line.HeaderLabel!.fontSize, Is.EqualTo(FactLine.HeaderSp * UiBuilder.TextScale), "Nunito Bold de 12 sp");
            Assert.That(line.TextSizeDp, Is.EqualTo(VictoryLineSlot.TextSp * UiBuilder.TextScale), "el texto, a 14 sp");
            Assert.That(view.FruitLineShownCount, Is.EqualTo(1));
            AssertFits(view, "3");
        }

        [UnityTest]
        public IEnumerator TheLastLineOfASeries_TurnsTheCounterIntoASeal()
        {
            // El 20 es un 10k: con las paradas, la llegada ocupa la línea (Canon). Sin el catálogo de paradas, sale la línea 20.
            yield return Win(20, v => v.RiverStops = null);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1000);
            FactLine line = view.FruitLine!;
            Assert.That(line.Shown, Is.True);
            Assert.That(line.ShownFactId, Is.EqualTo("acai.20"));
            Assert.That(line.Sealed, Is.True, "la última de la serie (Q5-A)");
            Assert.That(line.ProgressText, Is.EqualTo("Açaí"), "solo el nombre: el contador pasa a sello");
            Assert.That(line.Seal!.gameObject.activeSelf, Is.True);
            RectTransform seal = line.Seal.rectTransform;
            Assert.That(seal.rect.width, Is.EqualTo(FactLine.SealDp));
            Assert.That(seal.anchoredPosition.x + seal.rect.width, Is.LessThanOrEqualTo(view.VictorySlotWidthDp + 0.01f), "dentro de la ranura");
            RectTransform progress = line.ProgressLabel!.rectTransform;
            Assert.That(progress.anchoredPosition.x + progress.rect.width, Is.LessThanOrEqualTo(seal.anchoredPosition.x + 0.01f), "el sello, junto al nombre");
            AssertFits(view, "20 sin paradas");
        }

        [UnityTest]
        public IEnumerator TheSlotIsFixed_NextStaysPut_WithTheShortestAndLongestLines_At360x640()
        {
            // Por idioma, la línea más corta y la más larga de 1-200 (de las que salen en una victoria), el 3 (sin Cosecha doble ni
            // chip) y el 39 (con Cosecha doble y el chip del 40, Difícil); y, por idioma y escala, la línea que más alto ocupa con
            // su cabecera (la que fija H).
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap first = GameBootstrap.Current!;
            FactSchedule schedule = first.FruitFacts!;
            RiverStopCatalog stops = first.RiverStops!;
            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                if (Loc.Locale != locale)
                {
                    Loc.SetLocale(locale);
                }

                int shortest = 0;
                int longest = 0;
                for (int level = 1; level <= 200; level++)
                {
                    VictoryLine vl = VictoryLineArbiter.For(VictoryLineArbiter.ContextFor(level, false, stops, schedule.KeyFor(level), false));
                    if (vl.FruitKey == null)
                    {
                        continue;
                    }

                    int length = Loc.Fact(vl.FruitKey).Length;
                    if (shortest == 0 || length < Loc.Fact(schedule.KeyFor(shortest)!).Length)
                    {
                        shortest = level;
                    }

                    if (longest == 0 || length > Loc.Fact(schedule.KeyFor(longest)!).Length)
                    {
                        longest = level;
                    }
                }

                TestContext.WriteLine(locale + ": línea más corta: " + shortest + " (" + Loc.Fact(schedule.KeyFor(shortest)!).Length + "), más larga: "
                    + longest + " (" + Loc.Fact(schedule.KeyFor(longest)!).Length + ")");
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    var levels = new List<int> { 3 };
                    foreach (int l in new[] { shortest, longest, 39 })
                    {
                        if (!levels.Contains(l))
                        {
                            levels.Add(l);
                        }
                    }

                    float next = -1f;
                    float reserve = -1f;
                    for (int index = 0; index < levels.Count; index++)
                    {
                        int level = levels[index];
                        string what = level + " en " + locale + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                        yield return Win(level, null, locale, scale, true);
                        GameBootstrap boot = GameBootstrap.Current!;
                        GameScreen view = boot.View!;
                        PlayModeHelpers.Step(boot, 1000);
                        Assert.That(Loc.Locale, Is.EqualTo(locale), what);
                        Assert.That(UiBuilder.TextScale, Is.EqualTo(scale), what);
                        Assert.That(view.FruitLine!.Shown, Is.True, what + ": lleva su línea");
                        if (level == 39)
                        {
                            Assert.That(view.DoubleHarvestShown, Is.True, what + ": Cosecha doble");
                            Assert.That(view.NextChip.Visible, Is.True, what + ": chip del 40");
                        }
                        else if (level == 3)
                        {
                            Assert.That(view.DoubleHarvestShown, Is.False, what + ": sin Cosecha doble antes del 11");
                            Assert.That(view.NextChip.Visible, Is.False, what + ": sin chip");
                        }

                        TestContext.WriteLine(what + ": Siguiente " + view.NextTopDp + " dp, ranura " + view.VictorySlot.ContentDp + " dp, línea "
                            + view.FruitLine.ShownHeightDp + " dp (H " + view.FruitLine.BlockHeightDp + ", texto " + view.FruitLine.TextSizeDp
                            + " dp, cabecera en " + (view.FruitLine.HeaderWrapped ? "dos filas" : "una fila") + "), cinta "
                            + (view.VictoryPanelTopDp - 38f) + ", cabecera del HUD " + view.SlothBandTopDp);
                        AssertFits(view, what);
                        AssertRibbonUnderTheHeader(view, what + ": el panel y la cinta, bajo la cabecera");
                        if (next < 0f)
                        {
                            next = view.NextTopDp;
                            reserve = view.VictorySlot.ReserveDp;
                        }
                        else
                        {
                            Assert.That(view.NextTopDp, Is.EqualTo(next).Within(0.01f), what + ": Siguiente en el mismo sitio");
                            Assert.That(view.VictorySlot.ReserveDp, Is.EqualTo(reserve).Within(0.01f), what + ": la misma reserva");
                        }

                        if (index == 0)
                        {
                            // La línea que más alto ocupa en este idioma y esta escala (con la cabecera en una o dos filas).
                            int tallest = TallestLine(view, stops);
                            TestContext.WriteLine(locale + " al " + Mathf.RoundToInt(scale * 100f) + " %: la línea más alta es la del " + tallest);
                            if (!levels.Contains(tallest))
                            {
                                levels.Add(tallest);
                            }
                        }
                    }
                }
            }
        }

        // El nivel de 1-200 cuya línea de fruta ocupa más alto en el idioma, la escala y el ancho de view (deja la línea oculta).
        private static int TallestLine(GameScreen view, RiverStopCatalog stops)
        {
            FactLine line = view.FruitLine!;
            int tallest = 0;
            float height = -1f;
            for (int level = 1; level <= 200; level++)
            {
                VictoryLine vl = VictoryLineArbiter.For(VictoryLineArbiter.ContextFor(level, false, stops, view.FruitKeyOf!(level), false));
                if (vl.FruitKey != null && line.ShowFor(level, false, in vl) && line.ShownHeightDp > height)
                {
                    height = line.ShownHeightDp;
                    tallest = level;
                }
            }

            line.Hide();
            return tallest;
        }

        [UnityTest]
        public IEnumerator Levels1To200_EachVictoryCarriesItsLine_UnlessTheArbiterSaysOtherwise_AndAllFit()
        {
            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                float scale = locale == LocLocale.Pseudo ? 1.3f : 1f;
                yield return Win(1, null, locale, scale, true);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                FactLine line = view.FruitLine!;
                FactSchedule schedule = boot.FruitFacts!;
                RiverStopCatalog stops = boot.RiverStops!;
                int shown = 0;
                int shrunk = 0;
                int wrapped = 0;
                var without = new List<int>();
                for (int level = 1; level <= 200; level++)
                {
                    string what = level + " en " + locale;
                    VictoryLine vl = VictoryLineArbiter.For(VictoryLineArbiter.ContextFor(level, false, stops, view.FruitKeyOf!(level), false));
                    bool expected = vl.FruitKey != null;
                    Assert.That(line.ShowFor(level, false, in vl), Is.EqualTo(expected), what);
                    if (!expected)
                    {
                        without.Add(level);
                        continue;
                    }

                    shown++;
                    shrunk += line.TextSizeDp < (VictoryLineSlot.TextSp * scale) - 0.01f ? 1 : 0;
                    wrapped += line.HeaderWrapped ? 1 : 0;
                    Assert.That(line.ShownFactId, Is.EqualTo(schedule.FactIdFor(level)), what);
                    Assert.That(line.BodyText, Is.Not.Empty, what);
                    Assert.That(line.ProgressText, Does.Contain(line.Position.Lines == line.Position.K ? string.Empty : line.Position.K + "/20"), what);
                    AssertLineFits(view, what);
                }

                line.Hide();
                TestContext.WriteLine(locale + " al " + Mathf.RoundToInt(scale * 100f) + " %: " + shown + " líneas; " + shrunk
                    + " con el texto por debajo de 14 sp; " + wrapped + " con la cabecera en dos filas; H " + line.BlockHeightDp + " dp (la más alta del calendario "
                    + line.NeedDp(scale, view.VictorySlotWidthDp) + ", medida en " + line.NeedMs.ToString("0.0") + " ms); sin línea: " + string.Join(", ", without));
                Assert.That(shown + without.Count, Is.EqualTo(200));
                Assert.That(shown, Is.GreaterThanOrEqualTo(180), "casi todas: solo faltan los 10k con dato de llegada");
                Assert.That(shrunk, Is.EqualTo(0), "H es el de la línea más alta: ninguna baja de 14 sp");
                foreach (int level in without)
                {
                    Assert.That(level % 10 == 0 && level >= 20 && stops.ArrivalOnWin(level)?.ArrivalFact != null, Is.True,
                        level + ": solo los 10k con parada y dato se quedan sin línea de fruta");
                }

                Assert.That(without.Contains(10), Is.False, "el 10, sin el Mapa, lleva la suya");

                GameScreen.SimulatedScreen = null;
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        [UnityTest]
        public IEnumerator Level20_CarriesTheArrivalWithItsFact_NotTheFruitLine()
        {
            yield return Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            VictoryLine vl = view.CurrentVictoryLine;
            Assert.That(vl.ArrivalFact, Is.True, "Canon: la llegada con su dato");
            Assert.That(vl.FruitKey, Is.Null, "la línea de fruta del 20 queda en la ficha (Q4-A)");
            Assert.That(view.FruitLine!.Shown, Is.False);
            Assert.That(view.FruitLine.Root!.gameObject.activeSelf, Is.False);
            Assert.That(view.Arrival!.Kind, Is.EqualTo(ArrivalKind.Arrival));
            float fruit = view.VictorySlotContentHeight(Loc.Locale, UiBuilder.TextScale);
            float need = view.Arrival.NeedDp(UiBuilder.TextScale, view.VictorySlotWidthDp);
            Assert.That(view.VictorySlot.ContentDp, Is.EqualTo(Mathf.Max(fruit, need)), "la ranura reserva máx(línea de fruta, llegada)");
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1));
            Assert.That(view.LastRiverFactId, Is.EqualTo(AlterFact), "el fact_id es el del dato de llegada");
            Assert.That(view.FruitLineShownCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Level20_WithSingleKey_ThePlacePlateAndTheFruitLineGoTogether_AndNextStaysPut()
        {
            // Arquitectura de la tanda 4, §2: «con SingleKey, la llegada solo con la placa del lugar, más la línea de fruta». La
            // placa va arriba de la ranura y la línea debajo; la ranura reserva ese alto en todas las victorias.
            foreach (bool small in new[] { false, true })
            {
                string locale = small ? LocLocale.Pseudo : LocLocale.SpanishLatinAmerica;
                float scale = small ? 1.3f : 1f;
                string where = small ? " en en-XA al 130 % a 360 × 640" : " en es-419 al 100 %";
                yield return Win(3, v => v.RiverPolicyOverride = RiverFactPolicy.SingleKey, locale, scale, small);
                GameScreen view = GameBootstrap.Current!.View!;
                PlayModeHelpers.Step(GameBootstrap.Current!, 1000);
                float next3 = view.NextTopDp;
                Assert.That(view.Arrival!.Stacked, Is.False, "el 3 no tiene llegada");
                AssertFits(view, "3 con SingleKey" + where);

                yield return Win(20, v => v.RiverPolicyOverride = RiverFactPolicy.SingleKey, locale, scale, small);
                GameBootstrap boot = GameBootstrap.Current!;
                view = boot.View!;
                PlayModeHelpers.Step(boot, 1600);
                string what = "20 con SingleKey" + where;
                VictoryLine vl = view.CurrentVictoryLine;
                Assert.That(vl.Arrival, Is.True, what + ": la llegada");
                Assert.That(vl.ArrivalFact, Is.False, what + ": sin dato");
                Assert.That(vl.FruitKey, Is.EqualTo("fact.acai.20"), what + ": el árbitro da la línea de fruta");
                ArrivalStrip strip = view.Arrival!;
                Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Place), what + ": solo la placa del lugar");
                Assert.That(strip.Stacked, Is.True, what);
                Assert.That(strip.OffsetDp, Is.EqualTo(0f), what + ": la placa, arriba de la ranura");
                Assert.That(strip.ShownFactKey, Is.Null, what);
                FactLine line = view.FruitLine!;
                Assert.That(line.Shown, Is.True, what + ": se ve la línea de fruta");
                Assert.That(line.Alpha, Is.EqualTo(1f), what);
                Assert.That(line.ShownFactId, Is.EqualTo("acai.20"), what);
                Assert.That(line.OffsetDp, Is.GreaterThanOrEqualTo(strip.OffsetDp + strip.ShownHeightDp + FactLine.StackGapDp - 0.01f), what + ": la línea, bajo la placa");
                AssertFits(view, what);
                float width = view.VictorySlotWidthDp;
                float fruit = Mathf.Max(view.VictorySlotContentHeight(Loc.Locale, UiBuilder.TextScale), line.NeedDp(UiBuilder.TextScale, width));
                float stack = view.StackNeedDp(Loc.Locale, UiBuilder.TextScale, width, fruit);
                Assert.That(stack, Is.GreaterThan(fruit), what + ": la placa y la línea juntas");
                Assert.That(view.VictorySlot.ContentDp, Is.EqualTo(Mathf.Max(Mathf.Max(fruit, strip.NeedDp(UiBuilder.TextScale, width)), stack)), what + ": la ranura las reserva");
                Assert.That(view.NextTopDp, Is.EqualTo(next3).Within(0.01f), what + ": Siguiente en el mismo sitio que en el 3");
                TestContext.WriteLine(what + ": placa " + strip.ShownHeightDp + " dp, línea " + line.ShownHeightDp + " dp desde " + line.OffsetDp
                    + ", ranura " + view.VictorySlot.ContentDp + " dp (juntas " + stack + "), Siguiente " + view.NextTopDp + ", cinta "
                    + (view.VictoryPanelTopDp - 38f) + ", cabecera del HUD " + view.SlothBandTopDp);
                AssertRibbonUnderTheHeader(view, what + ": el panel y la cinta, bajo la cabecera");
                view.NextButton.onClick.Invoke();
                Assert.That(view.RiverFactShownCount, Is.EqualTo(1), what + ": river_fact_shown");
                Assert.That(view.LastRiverFactId, Is.EqualTo("acai.20"), what + ": con el fact_id de la línea de fruta");
                Assert.That(view.FruitLineShownCount, Is.EqualTo(1), what);
                GameScreen.SimulatedScreen = null;
                LaunchOptions.Reset();
            }
        }

        [UnityTest]
        public IEnumerator Level20_WithSingleKey_InALocaleWithoutTheLine_ThePlacePlateStaysCentred()
        {
            // en: el árbitro da la placa del lugar sin dato y la clave de la línea de fruta, pero la línea no tiene inglés y no sale
            // (D-018, Q10-A). La placa no se queda arriba con un hueco debajo: va centrada, como sin línea de fruta (revisión de F3).
            yield return Win(20, v =>
            {
                v.RiverPolicyOverride = RiverFactPolicy.SingleKey;
                Loc.SetLocale(LocLocale.English);
            });
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            string what = "20 con SingleKey en en";
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.English));
            VictoryLine vl = view.CurrentVictoryLine;
            Assert.That(vl.Arrival, Is.True, what + ": la llegada");
            Assert.That(vl.ArrivalFact, Is.False, what + ": sin dato");
            Assert.That(vl.FruitKey, Is.EqualTo("fact.acai.20"), what + ": el árbitro da la clave; el texto lo decide el idioma");
            Assert.That(view.FruitLine!.CanShow(in vl), Is.False, what + ": sin texto en inglés");
            Assert.That(view.FruitLine.Shown, Is.False, what + ": no sale la línea de fruta");
            ArrivalStrip strip = view.Arrival!;
            Assert.That(strip.Kind, Is.EqualTo(ArrivalKind.Place), what + ": solo la placa del lugar");
            Assert.That(strip.StackFruit, Is.False, what);
            Assert.That(strip.Stacked, Is.False, what + ": sin la línea debajo, la placa no va arriba");
            Assert.That(strip.ShownHeightDp, Is.LessThan(strip.SlotHeightDp), what);
            Assert.That(strip.OffsetDp, Is.EqualTo(Mathf.Max(0f, Mathf.Floor((strip.SlotHeightDp - strip.ShownHeightDp) * 0.5f))), what + ": la placa, centrada");
            Assert.That(strip.OffsetDp, Is.GreaterThan(0f), what + ": no pegada arriba");
            view.NextButton.onClick.Invoke();
            Assert.That(view.FruitLineShownCount, Is.EqualTo(0), what);
        }

        [UnityTest]
        public IEnumerator Level30_WithCanonAndAnArrivalWithoutItsFact_CarriesThePlacePlateAndTheFruitLine()
        {
            // Canon con otra cadencia del dato de llegada (20): el 30 llega a su parada sin dato, y el árbitro da la línea de fruta.
            yield return Win(30, v => v.RiverFactEveryNOverride = 20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            string what = "30 con Canon y la cadencia 20";
            VictoryLine vl = view.CurrentVictoryLine;
            string? key = view.FruitKeyOf!(30);
            Assert.That(key, Is.Not.Null);
            Assert.That(vl.Arrival, Is.True, what);
            Assert.That(vl.ArrivalFact, Is.False, what + ": la llegada, sin dato");
            Assert.That(vl.FruitKey, Is.EqualTo(key), what + ": y la línea de fruta");
            Assert.That(view.Arrival!.Kind, Is.EqualTo(ArrivalKind.Place), what);
            Assert.That(view.Arrival.Stacked, Is.True, what);
            FactLine line = view.FruitLine!;
            Assert.That(line.Shown, Is.True, what);
            Assert.That(line.OffsetDp, Is.GreaterThanOrEqualTo(view.Arrival.OffsetDp + view.Arrival.ShownHeightDp + FactLine.StackGapDp - 0.01f), what);
            AssertFits(view, what);
            view.HomeButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1), what + ": river_fact_shown");
            Assert.That(view.LastRiverFactId, Is.EqualTo(boot.FruitFacts!.FactIdFor(30)), what + ": con el fact_id de la línea de fruta");
        }

        [UnityTest]
        public IEnumerator Measures_AreMadeWhenTheLevelLoads_NotAtTheVictory()
        {
            int atLoad = -1;
            yield return Win(3, v =>
            {
                atLoad = v.FruitLine!.NeedMeasureCount;
                Assert.That(v.SlotWarmMs, Is.GreaterThanOrEqualTo(0d), "medidas al cargar el nivel");
                Assert.That(v.VictorySlot.Built, Is.True);
            });
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(atLoad, Is.EqualTo(1), "la línea más alta del calendario, una vez");
            Assert.That(view.FruitLine!.NeedMeasureCount, Is.EqualTo(atLoad), "la victoria no vuelve a medir");
            Assert.That(view.FruitLine.Shown, Is.True);
            string? perf = new DevProbe().FormatSlotMeasure(3, Loc.Locale, UiBuilder.TextScale, view.SlotWarmMs, view.FruitLine.NeedMs, view.VictorySlot.ContentDp);
            Assert.That(perf, Does.StartWith("AF_PERF {\"event\":\"slot_measure\"").And.Contain("\"level\":3").And.Contain("\"locale\":\"es-419\""));
            TestContext.WriteLine("medidas de la ranura al cargar el nivel: " + view.SlotWarmMs.ToString("0.0") + " ms (la línea de fruta "
                + view.FruitLine.NeedMs.ToString("0.0") + " ms)");
        }

        [UnityTest]
        public IEnumerator Level10_WithoutTheMap_CarriesAcai10_WithTheMap_TheSlotStaysEmpty_AndNextStaysPut()
        {
            yield return Win(10);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(view.MapAvailable, Is.False, "el Mapa aún no existe en el cliente (M3.3)");
            Assert.That(view.FruitLine!.Shown, Is.True, "sin el Mapa, el 10 lleva la línea 10 del açaí");
            Assert.That(view.FruitLine.ShownFactId, Is.EqualTo("acai.10"));
            Assert.That(view.FruitLine.ProgressText, Is.EqualTo("Açaí · 10/20"));
            float nextWithout = view.NextTopDp;
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1));
            Assert.That(view.LastRiverFactId, Is.EqualTo("acai.10"));

            yield return Win(10, v => v.MapAvailable = true);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.Null, "con el Mapa, la ranura queda vacía en el 10 (06 §2.3)");
            Assert.That(view.FruitLine!.Shown, Is.False);
            Assert.That(view.VictorySlot.Reserved, Is.True, "la ranura existe igual (Q7-A)");
            Assert.That(view.VictorySlot.Shown, Is.False);
            Assert.That(view.NextTopDp, Is.EqualTo(nextWithout).Within(0.01f), "Siguiente en el mismo sitio");
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(0), "sin línea, sin river_fact_shown");
        }

        [UnityTest]
        public IEnumerator Cadences_0_1_And_2()
        {
            float next = -1f;
            foreach (int[] c in new[] { new[] { 0, 3, 0 }, new[] { 1, 3, 1 }, new[] { 2, 3, 0 }, new[] { 2, 4, 1 } })
            {
                int every = c[0];
                int level = c[1];
                bool expected = c[2] == 1;
                string what = "cadencia " + every + " en el " + level;
                yield return Win(level, v => v.FruitEveryNOverride = every);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                PlayModeHelpers.Step(boot, 1000);
                Assert.That(view.CurrentVictoryLine.FruitKey != null, Is.EqualTo(expected), what + ": el árbitro");
                Assert.That(view.FruitLine!.Shown, Is.EqualTo(expected), what);
                Assert.That(view.VictorySlot.Reserved, Is.True, what + ": la ranura existe igual");
                if (expected)
                {
                    Assert.That(view.FruitLine.ShownFactId, Is.EqualTo("acai.0" + level), what);
                }

                if (next < 0f)
                {
                    next = view.NextTopDp;
                }

                Assert.That(view.NextTopDp, Is.EqualTo(next).Within(0.01f), what + ": Siguiente en el mismo sitio");
                view.NextButton.onClick.Invoke();
                Assert.That(view.RiverFactShownCount, Is.EqualTo(expected ? 1 : 0), what + ": river_fact_shown");
            }
        }

        [UnityTest]
        public IEnumerator InALocaleWithoutTheLine_NothingShows()
        {
            // en: la UI de la cabecera tiene inglés (river_fact), pero la línea y el nombre del tinte no: no sale nada, nunca a medias
            // ni en español (D-018, Q10-A).
            yield return Win(3, v => Loc.SetLocale(LocLocale.English));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.English));
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.EqualTo("fact.acai.03"), "el árbitro da la clave; el texto lo decide el idioma");
            Assert.That(view.FruitLine!.Shown, Is.False, "sin texto en inglés, sin línea");
            Assert.That(view.VictorySlot.Shown, Is.False);
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(0));
        }

        [Test]
        public void Texts_NeverMixLanguages_AndInReleaseNothingUnapproved()
        {
            const string head = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args\n";
            string strings = head
                + "fact_progress,{name} · {k}/{n},,,c,28,borrador,,,,\n"
                + "river_fact,Dato del río,River fact,Curiosidade do rio,c,16,aprobado_es,traducido,traducido,,\n";
            string facts = head
                + "fact.acai.03,Uno,One,,c,90,pendiente_asesor,traducido,,,\n"
                + "fact.acai.04,Cuatro,,,c,90,aprobado_es,,,,\n"
                + "fact.acai.20,Veinte,Twenty,,c,90,pendiente_asesor,traducido,,,\n"
                + "tint.acai.name,Açaí,Açaí,,c,24,aprobado_es,traducido,,,\n";
            LocCatalog catalog = LocCatalog.Parse(strings, facts);
            var series = new FactSeries("acai", FactSeriesKind.Tint, 1, 20, 1);
            var p3 = new FactPosition(series, 3);
            var p4 = new FactPosition(series, 4);
            var p20 = new FactPosition(series, 20);
            try
            {
                Loc.Configure(catalog, LocLocale.SpanishLatinAmerica, true);
                Assert.That(UiText.TryFruitLine(in p3, out string header, out string progress, out string text, out bool sealedLine), Is.True);
                Assert.That(header, Is.EqualTo("Dato del río"));
                Assert.That(progress, Is.EqualTo("Açaí · 3/20"));
                Assert.That(text, Is.EqualTo("Uno"));
                Assert.That(sealedLine, Is.False);

                // en: la línea y el nombre existen, pero fact_progress no: nada (ni «[fact_progress]» ni el español).
                Loc.Configure(catalog, LocLocale.English, true);
                Assert.That(UiText.TryFruitLine(in p3, out _, out progress, out text, out _), Is.False, "sin fact_progress en inglés");
                Assert.That(progress, Is.Empty);
                Assert.That(text, Is.Empty);
                Assert.That(UiText.TryFruitLine(in p4, out _, out _, out _, out _), Is.False, "sin la línea en inglés");

                // La última línea no lleva contador: el nombre y el sello, todo en inglés.
                Assert.That(UiText.TryFruitLine(in p20, out header, out progress, out text, out sealedLine), Is.True);
                Assert.That(header, Is.EqualTo("River fact"));
                Assert.That(progress, Is.EqualTo("Açaí"));
                Assert.That(text, Is.EqualTo("Twenty"));
                Assert.That(sealedLine, Is.True);

                // Release: la línea del 3 está pendiente del asesor; la del 4 está aprobada, pero fact_progress es un borrador.
                Loc.Configure(catalog, LocLocale.SpanishLatinAmerica, false);
                Assert.That(UiText.TryFruitLine(in p3, out _, out _, out _, out _), Is.False, "en release, nada pendiente del asesor");
                Assert.That(UiText.TryFruitLine(in p4, out _, out _, out _, out _), Is.False, "ni con la cabecera sin aprobar");
                Assert.That(UiText.TryFruitLine(default, out _, out _, out _, out _), Is.False, "sin serie, sin línea");
            }
            finally
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator Probe_RiverFactShown_FromTheCommonPoint_WithNextOrHome()
        {
            yield return Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, 1500);
            FactLine line = view.FruitLine!;
            string? state = new DevProbe().FormatFruitLine(line, view.VictorySlot.ContentDp);
            Assert.That(state, Does.StartWith("AF_STATE {\"event\":\"fruit_line\"").And.Contain("\"level\":3").And.Contain("\"fact_id\":\"acai.03\"")
                .And.Contain("\"key\":\"fact.acai.03\"").And.Contain("\"k\":3").And.Contain("\"n\":20").And.Contain("\"sealed\":false")
                .And.Contain("\"text_dp\":14.0").And.Not.Contain("Açaí"), "solo claves e identificadores, nunca texto visible");
            Assert.That(view.RiverFactShownCount, Is.EqualTo(0), "solo al cerrar la victoria");
            float since = view.VictorySlot.SinceShownMs;
            view.NextButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1));
            Assert.That(view.LastRiverFactId, Is.EqualTo("acai.03"), "fact_id = <tinte>.<kk> (08 §4.6 y §8.2)");
            Assert.That(since, Is.GreaterThan(VictoryLineSlot.FruitRevealMs));
            Assert.That(view.VictorySlot.DismissedMs, Is.EqualTo(since));
            Assert.That(view.LastRiverFactDismissedMs, Is.EqualTo(since - VictoryLineSlot.FruitRevealMs).Within(0.01f),
                "dismissed_ms de la línea de fruta: desde que entra la línea (Frutas §2.10), no desde la victoria");
            Assert.That(GameScreen.FruitDismissedMs(VictoryLineSlot.FruitRevealMs - 100f), Is.EqualTo(0f), "cerrada antes de que entre: 0");
            string? shown = new DevProbe().FormatRiverFactShown(3, view.LastRiverFactId!, view.LastRiverFactDismissedMs);
            Assert.That(shown, Does.Contain("\"event\":\"river_fact_shown\"").And.Contain("\"level\":3").And.Contain("\"fact_id\":\"acai.03\""));
            PlayModeHelpers.Step(boot, 500);
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1), "una vez por victoria");

            yield return Win(4);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            PlayModeHelpers.TickExact(boot, 1200);
            view.HomeButton.onClick.Invoke();
            Assert.That(view.RiverFactShownCount, Is.EqualTo(1), "Inicio también cierra la victoria");
            Assert.That(view.LastRiverFactId, Is.EqualTo("acai.04"));
        }

        /// <summary>
        /// Gana <paramref name="level"/> del catálogo real con una segunda sesión, en <paramref name="locale"/> (null: es-419) y a
        /// <paramref name="scale"/>, a 360 × 640 si <paramref name="small"/>; <paramref name="before"/> va antes de la victoria.
        /// </summary>
        internal static IEnumerator Win(int level, System.Action<GameScreen>? before = null, string? locale = null, float scale = 1f, bool small = false)
        {
            FunnelSetup.Hooks();
            if (small)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            }

            LaunchOptions.FontScaleOverride = scale;
            bool other = locale != null && locale != LocLocale.SpanishLatinAmerica;
            LaunchOptions.LanguageOverride = other ? locale : null;
            if (other && Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale!);
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            before?.Invoke(boot.View!);
            VictorySetup.WinToVictoryScreen(boot);
        }

        /// <summary>
        /// La cinta de la victoria (38 dp sobre el panel) bajo la cabecera del HUD. En 16:9, desde DC3 (lienzo de 360 × 640 dp;
        /// D-028), la ranura del Dato del río sube el panel por encima de la cabecera al 130 % (ranura de 212-266 dp: la cinta en
        /// 30,6 dp en el 127 en es-419, −3,4 en el 39 y −23,4 en el 20 con SingleKey en en-XA, frente a 90) y, con Cosecha doble y
        /// el chip del 40, ya al 100 % en en-XA (73 dp en el 39). Solo esos casos medidos se registran en vez de fallar (pendiente
        /// del dueño, open issue de DC3; exigido en <c>VictoryArrivalTests.Victory_130_16x9_TheRibbonUnderTheHeader_KnownIssue</c>):
        /// a 640 dp, con la escala de texto por encima del 100 % o en en-XA con la Cosecha doble y el chip del siguiente nivel. En
        /// es-419 al 100 % (y en 19,5:9 y 20:9) siempre se exige, como con <paramref name="strict"/>.
        /// </summary>
        internal static void AssertRibbonUnderTheHeader(GameScreen view, string what, bool strict = false)
        {
            float ribbon = view.VictoryPanelTopDp - 38f;
            if (!strict && ribbon < view.SlothBandTopDp && RibbonOverTheHeaderIsKnown(view))
            {
                TestContext.WriteLine(what + ": en 16:9 la cinta queda en " + ribbon.ToString("0.0") + " dp, "
                    + (view.SlothBandTopDp - ribbon).ToString("0.0") + " dp por encima de la cabecera del HUD (pendiente del dueño, DC3).");
                return;
            }

            Assert.That(ribbon, Is.GreaterThanOrEqualTo(view.SlothBandTopDp), what);
        }

        // Los casos medidos y registrados en DC3 (AssertRibbonUnderTheHeader): a 640 dp, al 130 % o en en-XA con la Cosecha doble y
        // el chip del siguiente nivel.
        private static bool RibbonOverTheHeaderIsKnown(GameScreen view)
        {
            if (view.CardCanvasHeight > GameScreen.MinHeight + 0.5f)
            {
                return false;
            }

            return UiBuilder.TextScale > 1.01f || (Loc.Locale == LocLocale.Pseudo && view.DoubleHarvestShown && view.NextChip.Visible);
        }

        /// <summary>La línea de fruta que se ve cabe en H y en la ranura, cada etiqueta en su caja y nada por debajo de 12 sp.</summary>
        internal static void AssertFits(GameScreen view, string what)
        {
            FactLine line = view.FruitLine!;
            Assert.That(line.Shown, Is.True, what);
            AssertLineFits(view, what);
        }

        private static void AssertLineFits(GameScreen view, string what)
        {
            FactLine line = view.FruitLine!;
            float slot = view.VictorySlot.ContentDp;
            float width = view.VictorySlotWidthDp;
            float scale = UiBuilder.TextScale;
            Assert.That(line.ShownHeightDp, Is.LessThanOrEqualTo(line.BlockHeightDp + 0.5f), what + ": la línea cabe en H");
            Assert.That(line.OffsetDp + line.ShownHeightDp, Is.LessThanOrEqualTo(slot + 0.5f), what + ": y en la ranura");
            Assert.That(line.TextSizeDp, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), what + ": nunca menos de 12 sp");
            foreach (TextMeshProUGUI? label in new[] { line.HeaderLabel, line.ProgressLabel, line.TextLabel })
            {
                RectTransform rt = label!.rectTransform;
                bool wraps = label.textWrappingMode != TextWrappingModes.NoWrap;
                Vector2 preferred = label.GetPreferredValues(label.text, wraps ? rt.rect.width : float.PositiveInfinity, float.PositiveInfinity);
                Assert.That(preferred.y, Is.LessThanOrEqualTo(rt.rect.height + 0.5f), what + ": " + label.name + " cabe en su caja (alto)");
                if (!wraps)
                {
                    Assert.That(preferred.x, Is.LessThanOrEqualTo(rt.rect.width + 0.5f), what + ": " + label.name + " cabe en su caja (ancho)");
                }

                Assert.That(rt.anchoredPosition.x + rt.rect.width, Is.LessThanOrEqualTo(width + 0.5f), what + ": " + label.name + " dentro del ancho");
                Assert.That(-rt.anchoredPosition.y + rt.rect.height, Is.LessThanOrEqualTo(slot + 0.5f), what + ": " + label.name + " dentro de la ranura");
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), what + ": " + label.name + " ≥ 12 sp");
            }

            ArrivalStrip? arrival = view.Arrival;
            if (arrival == null || !arrival.Stacked || arrival.Kind == ArrivalKind.None)
            {
                Assert.That(line.OffsetDp, Is.LessThanOrEqualTo(FactLine.TopAirDp), what + ": arriba de la ranura, cerca del filete");
            }

            RectTransform header = line.HeaderLabel!.rectTransform;
            RectTransform progress = line.ProgressLabel!.rectTransform;
            RectTransform dot = line.HeaderDot!.rectTransform;
            Assert.That(line.HeaderDot.gameObject.activeSelf, Is.EqualTo(!line.HeaderWrapped), what + ": el punto medio, solo en una fila");
            if (!line.HeaderWrapped)
            {
                Assert.That(header.anchoredPosition.x + header.rect.width, Is.LessThanOrEqualTo(dot.anchoredPosition.x + 0.5f),
                    what + ": «Dato del río», el punto");
                Assert.That(dot.anchoredPosition.x + dot.rect.width, Is.LessThanOrEqualTo(progress.anchoredPosition.x + 0.5f),
                    what + ": y el contador, seguidos y sin pisarse");
                Assert.That(progress.anchoredPosition.x - (header.anchoredPosition.x + header.rect.width),
                    Is.LessThanOrEqualTo((2f * FactLine.HeaderGapDp) + FactLine.SeparatorDotDp + 0.5f), what + ": el contador, a continuación");
            }
            else
            {
                Assert.That(-progress.anchoredPosition.y, Is.GreaterThanOrEqualTo(-header.anchoredPosition.y + header.rect.height - 0.5f),
                    what + ": el contador, en la segunda fila");
            }

            RectTransform text = line.TextLabel!.rectTransform;
            Assert.That(-text.anchoredPosition.y, Is.GreaterThanOrEqualTo(line.OffsetDp + line.HeaderHeightDp - 0.5f), what + ": el texto, bajo la cabecera");
        }
    }
}
