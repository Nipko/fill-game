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
    /// él con Reducir movimiento, y no recibe toques (raycastTarget false hasta F4);</item>
    /// <item>la línea del 3 («Dato del río», «Açaí · 3/20» y su texto) y el sello de la última línea de una serie;</item>
    /// <item>ranura fija: «Siguiente» en el mismo sitio con la línea más corta y la más larga de 1-200, en el 3 (sin Cosecha
    /// doble) y en el 39 (con Cosecha doble y el chip), en es-419 y en-XA, al 100 % y al 130 %, a 360 × 640, con el panel y la
    /// cinta bajo la cabecera;</item>
    /// <item>en 1-200, cada victoria lleva su línea salvo donde el árbitro dice otra cosa (los <c>10k</c> con dato de llegada), y
    /// todas caben sin recortes en es-419 al 100 % y en en-XA al 130 %;</item>
    /// <item>el 20 (un <c>10k</c>) lleva la llegada y no la línea de fruta; el 10, la línea sin el Mapa y la ranura vacía con él;</item>
    /// <item>cadencias 0, 1 y 2 (<c>ui_fruit_line_every_n_levels</c>);</item>
    /// <item>sin mezcla de idiomas: en un idioma sin la línea traducida, no sale nada; con la línea traducida pero sin la
    /// cabecera, tampoco; en release, nada sin aprobar;</item>
    /// <item>sonda: <c>river_fact_shown</c> con <c>fact_id</c> = <c>&lt;tinte&gt;.&lt;kk&gt;</c> desde el punto común (Siguiente o
    /// Inicio) y <c>AF_STATE fruit_line</c>.</item>
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

                // No es un botón hasta F4: no bloquea ni recibe toques, y Siguiente funciona.
                Assert.That(view.NextButton.interactable, Is.True);
                foreach (Graphic g in line.Root!.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(g.raycastTarget, Is.False, g.name + ": sin toques hasta F4");
                }

                Assert.That(line.Root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False, "su CanvasGroup no bloquea");

                var ids = new List<string>();
                var rects = new List<RectTransform>();
                line.LayoutIds(ids, rects);
                Assert.That(ids, Is.Empty, "no entra en AF_LAYOUT victory");
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
            Assert.That(line.HeaderHeightDp, Is.EqualTo(VictoryLineSlot.HeaderDp), "cabecera de 32 dp");
            Assert.That(line.Icon!.sprite, Is.Not.Null, "el icono de la fruta");
            Assert.That(line.Icon.rectTransform.rect.width, Is.EqualTo(FactLine.IconDp));
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
            // La línea más corta y la más larga de 1-200 en es-419 (de las que salen en una victoria), el 3 (sin Cosecha doble ni
            // chip) y el 39 (con Cosecha doble y el chip del 40, Difícil).
            yield return HudSetup.LoadAtIntro(1);
            GameBootstrap first = GameBootstrap.Current!;
            FactSchedule schedule = first.FruitFacts!;
            RiverStopCatalog stops = first.RiverStops!;
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

            TestContext.WriteLine("línea más corta: " + shortest + " (" + Loc.Fact(schedule.KeyFor(shortest)!).Length + "), más larga: " + longest
                + " (" + Loc.Fact(schedule.KeyFor(longest)!).Length + ")");
            var levels = new List<int> { 3 };
            foreach (int l in new[] { shortest, longest, 39 })
            {
                if (!levels.Contains(l))
                {
                    levels.Add(l);
                }
            }

            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    float next = -1f;
                    float reserve = -1f;
                    foreach (int level in levels)
                    {
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
                        }

                        TestContext.WriteLine(what + ": Siguiente " + view.NextTopDp + " dp, ranura " + view.VictorySlot.ContentDp + " dp, línea "
                            + view.FruitLine.ShownHeightDp + " dp (H " + view.FruitLine.BlockHeightDp + ", texto " + view.FruitLine.TextSizeDp
                            + " dp, cabecera en " + (view.FruitLine.HeaderWrapped ? "dos filas" : "una fila") + "), cinta "
                            + (view.VictoryPanelTopDp - 38f) + ", cabecera del HUD " + view.SlothBandTopDp);
                        AssertFits(view, what);
                        Assert.That(view.VictoryPanelTopDp - 38f, Is.GreaterThanOrEqualTo(view.SlothBandTopDp), what + ": el panel y la cinta, bajo la cabecera");
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
                    }
                }
            }
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
            Assert.That(view.LastRiverFactDismissedMs, Is.EqualTo(since));
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

            RectTransform header = line.HeaderLabel!.rectTransform;
            RectTransform progress = line.ProgressLabel!.rectTransform;
            if (!line.HeaderWrapped)
            {
                Assert.That(header.anchoredPosition.x + header.rect.width, Is.LessThanOrEqualTo(progress.anchoredPosition.x + 0.5f),
                    what + ": «Dato del río» y el contador no se pisan");
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
