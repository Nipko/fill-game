#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
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
    /// La ficha de una fruta de Frutas del río y «Sobre la Amazonía» en Ajustes (D-017, Q4-A y Q6-A; 02 §6, fila «Dato del río»;
    /// propuesta de Frutas §2.3 y §6.4; tarea F4), en la escena, con el calendario real (<c>frutas_del_rio_v1.json</c>) y los
    /// textos de <c>facts.csv</c>:
    /// <list type="bullet">
    /// <item>la línea de la victoria es un botón de ≥ 48 dp desde que entra (un toque de verdad, por la prueba de impacto del
    /// EventSystem, en el centro que publica <c>AF_LAYOUT victory</c>), abre la ficha de su fruta y escribe
    /// <c>af_fact_page_open</c> {tint_id, unlocked}; mientras se ve, la victoria no avanza (ni con Update) y al cerrarla (Atrás o la
    /// X) sigue tal como estaba;</item>
    /// <item>líneas reveladas numeradas y el resto en silueta con «Nivel {n}»; el Salto del Boto revela; serie completa con sello;
    /// serie sin empezar con su frase de «Fruta nueva» y «Su serie empieza en el nivel {n}»; serie de río; nombre científico y
    /// fuentes de lo que se lee;</item>
    /// <item>Ajustes › «Sobre la Amazonía»: una ficha por serie, en silueta las que no empezaron, que abre la ficha encima de
    /// Ajustes; Atrás cierra primero la ficha;</item>
    /// <item>en release, solo lo aprobado; textos ≥ 12 sp al 130 % sin recortes y contraste ≥ 4,5:1.</item>
    /// </list>
    /// </summary>
    public sealed class FruitSheetTests
    {
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
        public IEnumerator VictoryLine_IsATouchTarget_OpensTheSheet_AndClosingReturnsToTheVictoryAsItWas()
        {
            using var log = new AfLayoutLog();
            yield return FactLineTests.Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FactLine line = view.FruitLine!;
            Assert.That(line.Shown, Is.True);
            Assert.That(line.Tapped, Is.Not.Null, "GameBootstrap escucha la línea");
            Assert.That(line.Touchable, Is.False, "invisible hasta «Siguiente»: no recibe toques");
            Assert.That(boot.OpenFruitSheet("urucum", FruitSheetSource.Victory), Is.False, "desde la victoria, solo la fruta de la línea");

            PlayModeHelpers.TickExact(boot, (int)VictoryLineSlot.FruitRevealMs + (int)VictoryLineSlot.FruitFadeMs + 100);
            Assert.That(line.Alpha, Is.EqualTo(1f));
            Assert.That(line.Touchable, Is.True, "a la vista, toda la línea es un botón");
            RectTransform tap = line.TapArea!;
            Assert.That(tap.rect.height, Is.GreaterThanOrEqualTo(VictoryLineSlot.MinContentDp - 0.01f), "≥ 48 dp (02 §6; 07 §7.8)");
            Assert.That(tap.rect.height, Is.GreaterThanOrEqualTo(line.ShownHeightDp - 0.01f), "toda la línea");
            foreach (Graphic g in line.Root!.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.EqualTo(g.transform == tap), g.name + ": solo el área táctil recibe toques");
            }

            // Revisión de F4: se ve que se puede tocar. El chevrón «›» (un icono), a la derecha de la cabecera y dentro del área
            // táctil, sin pisar el contador; y un fondo que solo aparece al pulsar.
            Graphic chevron = line.Chevron!;
            Assert.That(chevron.gameObject.activeSelf, Is.True, "el chevrón, con la línea que abre la ficha");
            Assert.That(chevron.color, Is.EqualTo(FactLine.ChevronInk));
            RectTransform cr = chevron.rectTransform;
            RectTransform progress = line.ProgressLabel!.rectTransform;
            Assert.That(cr.rect.width, Is.EqualTo(FactLine.ChevronDp).Within(0.01f));
            Assert.That(cr.anchoredPosition.x + cr.rect.width, Is.EqualTo(tap.rect.width).Within(0.5f), "pegado a la derecha");
            Assert.That(progress.anchoredPosition.x + progress.rect.width, Is.LessThanOrEqualTo(cr.anchoredPosition.x - FactLine.ChevronGapDp + 0.5f),
                "sin pisar el contador");
            Assert.That(-cr.anchoredPosition.y, Is.GreaterThanOrEqualTo(line.OffsetDp - 0.5f));
            Assert.That(-cr.anchoredPosition.y + cr.rect.height, Is.LessThanOrEqualTo(line.OffsetDp + line.HeaderHeightDp + 0.5f), "en la cabecera");
            Button tapButton = tap.GetComponent<Button>();
            Assert.That(tapButton.transition, Is.EqualTo(Selectable.Transition.ColorTint));
            Assert.That(tapButton.targetGraphic, Is.SameAs(line.PressBack));
            Assert.That(tapButton.colors.normalColor.a, Is.EqualTo(0f), "en reposo, sin fondo");
            Assert.That(tapButton.colors.selectedColor.a, Is.EqualTo(0f), "ni tras el toque");
            Assert.That(tapButton.colors.pressedColor.a, Is.EqualTo(FactLine.PressedAlpha), "al pulsar, el fondo");
            Assert.That(line.PressBack!.canvasRenderer.GetAlpha(), Is.EqualTo(0f).Within(0.001f), "en reposo no se ve");

            string victory = log.RequireLast("victory", 3, "AF_LAYOUT victory");
            Assert.That(AfLayoutLog.ButtonIds(victory), Does.Contain(FactLine.TapId), "la línea entra en AF_LAYOUT victory");

            // Lo que hay que encontrar igual al volver.
            LevelFlow flow = boot.Flow!;
            float since = view.VictorySlot.SinceShownMs;
            float next = view.NextAlpha;
            string body = line.BodyText;

            yield return AfLayoutLog.Tap(victory, FactLine.TapId, tap.GetComponent<Button>());
            FruitSheet sheet = boot.FruitSheetView!;
            Assert.That(sheet.Visible, Is.True, "el toque abre la ficha");
            Assert.That(sheet.Source, Is.EqualTo(FruitSheetSource.Victory));
            Assert.That(sheet.Series!.Subject, Is.EqualTo("acai"));
            Assert.That(sheet.Unlocked, Is.EqualTo(3), "las líneas 1-3: el 3 ya está superado");
            Assert.That(sheet.NameText, Is.EqualTo("Açaí · 3/20"));
            Assert.That(boot.FactPageOpenCount, Is.EqualTo(1));
            Assert.That(boot.OpenRiverMap(MapOpenSource.Victory), Is.False, "nada se abre encima de la ficha");
            Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Victory), Is.False, "ya está abierta");

            // Mientras se ve, la victoria no avanza aunque corra Update (un segundo de fotogramas de verdad).
            boot.enabled = true;
            float until = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            boot.enabled = false;
            Assert.That(sheet.Visible, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la victoria sigue");
            Assert.That(view.VictorySlot.SinceShownMs, Is.EqualTo(since), "congelada: el reloj de la victoria no corre");

            // Atrás cierra la ficha y la victoria está como estaba.
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(sheet.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(3));
            Assert.That(view.VictorySlot.SinceShownMs, Is.EqualTo(since));
            Assert.That(view.NextAlpha, Is.EqualTo(next));
            Assert.That(line.Shown && line.Alpha == 1f && line.BodyText == body, Is.True, "la línea, igual");
            Assert.That(line.Touchable, Is.True);

            // Otra vez, ahora con la X: tampoco cambia nada, y «Siguiente» sigue funcionando.
            line.TapArea!.GetComponent<Button>().onClick.Invoke();
            Assert.That(sheet.Visible, Is.True);
            Assert.That(boot.FactPageOpenCount, Is.EqualTo(2), "una sonda por apertura");
            sheet.CloseButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
            Assert.That(boot.CloseFruitSheet(), Is.False, "ya cerrada");
            Assert.That(view.VictorySlot.SinceShownMs, Is.EqualTo(since));
            view.NextButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.Not.EqualTo(FlowPhase.VictoryScreen), "Siguiente cierra la victoria");
            Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Victory), Is.False, "sin la victoria a la vista, no se abre");
        }

        [UnityTest]
        public IEnumerator Sheet_RevealedNumbered_SilhouettesWithLevelN_AndTheBotoJumpReveals()
        {
            yield return FactLineTests.Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            FruitSheet sheet = boot.FruitSheetView!;
            FactCatalog catalog = boot.FruitFacts!.Catalog;
            FactSeries tapajos = catalog.SeriesOf("agua_tapajos")!;

            // Superado hasta el 24 y el 27 saltado con el Salto del Boto (04 §2.6): 21-24 y 27, reveladas.
            var progress = new MemoryProgressStore(28, 24);
            progress.MarkSkipped(27);
            Assert.That(sheet.Show(tapajos, catalog, progress, FruitSheetSource.Settings), Is.True);
            Assert.That(sheet.Unlocked, Is.EqualTo(5));
            Assert.That(sheet.Started && !sheet.Sealed, Is.True);
            Assert.That(sheet.NameText, Is.EqualTo("Agua del Tapajós · 5/20"), "fact_progress con las reveladas");
            Assert.That(sheet.ScientificText, Is.Empty, "las aguas no tienen nombre científico");
            Assert.That(sheet.RowCount, Is.EqualTo(20));
            for (int i = 0; i < 20; i++)
            {
                int k = i + 1;
                int level = tapajos.LevelAt(k);
                bool revealed = level <= 24 || level == 27;
                Assert.That(sheet.RowNumber(i), Is.EqualTo(k.ToString(System.Globalization.CultureInfo.InvariantCulture)), "numeradas");
                Assert.That(sheet.RowRevealed(i), Is.EqualTo(revealed), "línea " + k);
                Assert.That(sheet.RowText(i), Is.EqualTo(revealed ? Loc.Fact(tapajos.KeyAt(k)) : "Nivel " + level),
                    revealed ? "su texto" : "en silueta, «Nivel {n}» (fact_locked)");
            }

            Assert.That(sheet.RowText(6), Does.Not.StartWith("Nivel"), "el 27, saltado, se lee en la ficha (propuesta §2.9)");
            Assert.That(sheet.ShownKeys, Is.EqualTo(new[]
            {
                "fact.agua_tapajos.01", "fact.agua_tapajos.02", "fact.agua_tapajos.03", "fact.agua_tapajos.04", "fact.agua_tapajos.07",
            }));
            Assert.That(sheet.SourcesShown, Is.True, "«Fuentes» al pie");
            Assert.That(sheet.Sources, Is.Not.Empty);
            Assert.That(sheet.Sources[0].Id, Is.EqualTo("T1"), "las fuentes de lo que se lee, en orden de cita");
            var expected = new List<FactSource>();
            FruitSheet.SourcesOf(sheet.ShownKeys, catalog, Loc.Catalog.Facts, expected);
            Assert.That(sheet.Sources, Is.EqualTo(expected));
            CollectionAssert.AllItemsAreUnique(sheet.Sources);
            foreach (TextMeshProUGUI t in sheet.Texts())
            {
                Assert.That(t.text, Does.Not.Contain("://"), "revisión de F4: el dominio, nunca la dirección entera: " + t.name);
            }

            // Las siluetas (revisión de F4): las barras empiezan todas en la misma x, tenga «Nivel {n}» las cifras que tenga, y son
            // dos, la segunda al 62 % de la primera.
            float barLeft = -1f;
            for (int i = 0; i < 20; i++)
            {
                if (sheet.RowRevealed(i))
                {
                    continue;
                }

                (float left, float first, float second) = sheet.RowBars(i);
                barLeft = barLeft < 0f ? left : barLeft;
                Assert.That(left, Is.EqualTo(barLeft).Within(0.01f), "línea " + (i + 1) + ": la columna «Nivel {n}» es fija");
                Assert.That(first, Is.GreaterThan(FruitSheet.BarDp));
                Assert.That(second, Is.EqualTo(Mathf.Round(first * FruitSheet.SecondBarFraction)).Within(0.01f), "dos barras, la segunda más corta");
            }

            // Sin el Salto del Boto, el 27 vuelve a la silueta.
            sheet.Hide();
            Assert.That(sheet.Show(tapajos, catalog, new MemoryProgressStore(28, 24), FruitSheetSource.Settings), Is.True);
            Assert.That(sheet.Unlocked, Is.EqualTo(4));
            Assert.That(sheet.RowRevealed(6), Is.False);
            Assert.That(sheet.RowText(6), Is.EqualTo("Nivel 27"));
            Assert.That(FruitSheet.IsRevealed(27, progress), Is.True, "FactSchedule.IsLevelRevealed o skipped_levels");
            Assert.That(FruitSheet.IsRevealed(25, progress), Is.False);
            Assert.That(FruitSheet.IsRevealed(0, progress), Is.False);
            sheet.Hide();
        }

        [UnityTest]
        public IEnumerator Sheet_CompleteSeries_Seal_Scientific_UnstartedSeries_AndRiverSeries()
        {
            yield return FactLineTests.Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            FruitSheet sheet = boot.FruitSheetView!;
            FactCatalog catalog = boot.FruitFacts!.Catalog;
            var progress = new MemoryProgressStore(25, 24);

            // El açaí, completo: el nombre con el sello (Q5-A), el nombre científico y sus fuentes.
            FactSeries acai = catalog.SeriesOf("acai")!;
            Assert.That(sheet.Show(acai, catalog, progress, FruitSheetSource.Settings), Is.True);
            Assert.That(sheet.Unlocked, Is.EqualTo(20));
            Assert.That(sheet.Sealed, Is.True);
            Assert.That(sheet.NameText, Is.EqualTo("Açaí"), "con las 20, el contador pasa al sello");
            Assert.That(sheet.Seal.gameObject.activeSelf, Is.True);
            Assert.That(sheet.ScientificText, Is.EqualTo("Euterpe oleracea"));
            Assert.That(sheet.Icon.sprite, Is.Not.Null);
            Assert.That(sheet.Icon.color, Is.EqualTo(Color.white));
            for (int i = 0; i < 20; i++)
            {
                Assert.That(sheet.RowRevealed(i), Is.True);
            }

            Assert.That(sheet.Sources[0].Id, Is.EqualTo("A1"));
            Assert.That(sheet.StartsAtText, Is.Empty);

            // Revisión de F4: en cada fuente, el título (14 sp, gris neutro) y, a 4 dp, el dominio (12 sp, tinta secundaria); entre
            // fuentes, 14 dp. Ninguno en azul ni subrayado.
            Assert.That(sheet.Sources.Count, Is.GreaterThan(1));
            for (int i = 0; i < sheet.Sources.Count; i++)
            {
                (TextMeshProUGUI title, TextMeshProUGUI domain) = sheet.SourceLabels(i);
                Assert.That(title.fontSize, Is.EqualTo(FruitSheet.SourceSp).Within(0.01f), "título");
                Assert.That(domain.fontSize, Is.EqualTo(FruitSheet.SourceDomainSp).Within(0.01f), "dominio");
                Assert.That(title.color, Is.EqualTo(FruitSheet.SourceInk));
                Assert.That(domain.color, Is.EqualTo(FruitSheet.SoftInk));
                Assert.That((title.fontStyle & FontStyles.Underline) == 0 && (domain.fontStyle & FontStyles.Underline) == 0, Is.True, "sin subrayado");
                float titleBottom = -title.rectTransform.anchoredPosition.y + title.rectTransform.rect.height;
                Assert.That(-domain.rectTransform.anchoredPosition.y, Is.EqualTo(titleBottom + FruitSheet.SourceDomainGapDp).Within(0.5f));
                if (i > 0)
                {
                    TextMeshProUGUI previous = sheet.SourceLabels(i - 1).Domain;
                    float previousBottom = -previous.rectTransform.anchoredPosition.y + previous.rectTransform.rect.height;
                    Assert.That(-title.rectTransform.anchoredPosition.y, Is.EqualTo(previousBottom + FruitSheet.SourceGapDp).Within(0.5f), "entre fuentes");
                }
            }

            sheet.Hide();

            // El achiote, sin empezar: su frase de «Fruta nueva» y el nivel en que empieza; sin líneas ni fuentes.
            FactSeries urucum = catalog.SeriesOf("urucum")!;
            Assert.That(sheet.Show(urucum, catalog, progress, FruitSheetSource.Settings), Is.True);
            Assert.That(sheet.Started, Is.False);
            Assert.That(sheet.RowCount, Is.EqualTo(0));
            Assert.That(sheet.NameText, Is.EqualTo("Achiote · 0/20"));
            Assert.That(sheet.TagText, Is.EqualTo(Loc.Fact("fact.urucum.tag")).And.Not.Empty);
            Assert.That(sheet.StartsAtText, Is.EqualTo("Su serie empieza en el nivel 41"));
            Assert.That(sheet.SourcesShown, Is.False);

            // Revisión de F4: la frase de la fruta manda (16 sp, la tinta del texto) y el nivel en que empieza va debajo, en Nunito
            // Regular de 14 sp (como dice el contexto de fact_starts_at) y en tinta secundaria, a 8 dp.
            float scale = UiBuilder.TextScale;
            Assert.That(sheet.TagLabel.fontSize, Is.EqualTo(FruitSheet.TagSp * scale).Within(0.01f));
            Assert.That(sheet.TagLabel.color, Is.EqualTo(FruitSheet.Ink));
            Assert.That(sheet.StartsAtLabel.fontSize, Is.EqualTo(FruitSheet.StartSp * scale).Within(0.01f));
            Assert.That(sheet.StartsAtLabel.font, Is.SameAs(sheet.TagLabel.font), "Nunito Regular, como la frase");
            Assert.That(sheet.StartsAtLabel.color, Is.EqualTo(FruitSheet.SoftInk));
            Assert.That(-sheet.StartsAtLabel.rectTransform.anchoredPosition.y,
                Is.EqualTo(-sheet.TagLabel.rectTransform.anchoredPosition.y + sheet.TagLabel.rectTransform.rect.height + FruitSheet.TagGapDp).Within(0.5f));
            Assert.That(sheet.Icon.color, Is.EqualTo(FruitSheet.SilhouetteColor), "el icono, en silueta");
            Assert.That(sheet.ScientificText, Is.EqualTo("Bixa orellana"));
            sheet.Hide();

            // Una serie de río [P]: sin frase de «Fruta nueva» ni nombre científico, con la gota.
            FactSeries napo = catalog.SeriesOf("rio_napo")!;
            Assert.That(sheet.Show(napo, catalog, progress, FruitSheetSource.Settings), Is.True);
            Assert.That(sheet.NameText, Is.EqualTo("Napo y Cuyabeno · 0/20"));
            Assert.That(sheet.TagText, Is.Empty);
            Assert.That(sheet.StartsAtText, Is.EqualTo("Su serie empieza en el nivel 261"));
            Assert.That(sheet.ScientificText, Is.Empty);
            Assert.That(sheet.Icon.gameObject.activeSelf, Is.True, "la gota del río");
            sheet.Hide();

            // Todos los nombres científicos de las frutas, binomios latinos; ninguno en las aguas y los ríos.
            foreach (FactSeries s in catalog.Series)
            {
                string? scientific = FruitSheet.ScientificName(s, Loc.Catalog.Facts);
                bool water = s.Kind == FactSeriesKind.River || s.Subject.StartsWith("agua_", System.StringComparison.Ordinal);
                if (water)
                {
                    Assert.That(scientific, Is.Null, s.Subject);
                }
                else
                {
                    Assert.That(scientific, Does.Match("^[A-Z][a-z]+ [a-z]+$"), s.Subject);
                }
            }
        }

        [UnityTest]
        public IEnumerator Settings_AboutTheAmazon_ListsTheFruits_OpensTheSheetOnTop_AndBackClosesTheSheetFirst()
        {
            using var log = new AfLayoutLog();
            Profile profile = SettingsScreenTests.ProfileAt(25);
            profile.Progress.SkippedLevels.Add(41); // el Salto del Boto en el 41: la serie del achiote empieza
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            FactCatalog catalog = boot.FruitFacts!.Catalog;
            Assert.That(settings.AboutShown, Is.True, "Sobre la Amazonía, a la vista");
            Assert.That(settings.IdleHintSlot.gameObject.activeSelf, Is.True, "la fila de MO3, a la vista, antes de Sobre la Amazonía");
            Assert.That(settings.AboutTitle, Is.EqualTo("Sobre la Amazonía"));
            Assert.That(settings.FruitsTitle, Is.EqualTo("Frutas del río"));
            Assert.That(settings.FruitCount, Is.EqualTo(catalog.Series.Count), "una ficha por serie del calendario");
            Assert.That(settings.FruitColumns, Is.EqualTo(2), "al 100 %, en dos columnas (ningún nombre pide más de dos líneas)");
            for (int i = 0; i < settings.FruitCount; i++)
            {
                FactSeries s = catalog.Series[i];
                bool started = s.FromLevel <= 24 || s.Subject == "urucum";
                Assert.That(settings.FruitSubject(i), Is.EqualTo(s.Subject), "en el orden del calendario");
                Assert.That(settings.FruitStarted(i), Is.EqualTo(started), s.Subject);
                Assert.That(settings.FruitLabel(i), Is.EqualTo(started ? Loc.Fact(s.NameKey) : "Nivel " + s.FromLevel),
                    started ? "su nombre" : "en silueta, con el nivel de su primera línea");
                Assert.That(settings.FruitIcon(i).color, Is.EqualTo(started ? Color.white : FruitSheet.SilhouetteColor).Or.EqualTo(FruitSheet.RiverDrop), s.Subject);
                Assert.That(((RectTransform)settings.FruitButton(i).transform).rect.height, Is.GreaterThanOrEqualTo(48f), "≥ 48 dp");
                Assert.That(settings.FruitTileHeight(i), Is.EqualTo(settings.FruitTileHeight(0)).Within(0.01f), "revisión de F4: todas las fichas del mismo alto");
            }

            SettingsScreenTests.AssertFits(settings, "Ajustes con Sobre la Amazonía");
            string layout = log.RequireLast("settings", 0, "AF_LAYOUT settings");
            Assert.That(AfLayoutLog.ButtonIds(layout), Does.Contain("fruit_acai").And.Contain("fruit_rio_napo"));

            // Tocar el achiote (empezado por el Salto del Boto): su ficha, encima de Ajustes, que sigue abierta.
            settings.FruitButton("urucum")!.onClick.Invoke();
            FruitSheet sheet = boot.FruitSheetView!;
            Assert.That(sheet.Visible, Is.True);
            Assert.That(sheet.Source, Is.EqualTo(FruitSheetSource.Settings));
            Assert.That(sheet.Series!.Subject, Is.EqualTo("urucum"));
            Assert.That(sheet.Unlocked, Is.EqualTo(1), "solo la del 41, saltado");
            Assert.That(sheet.RowRevealed(0), Is.True);
            Assert.That(settings.Visible, Is.True, "Ajustes, debajo");
            Assert.That(boot.FactPageOpenCount, Is.EqualTo(1));
            Assert.That(sheet.Canvas.sortingOrder, Is.GreaterThan(settings.Canvas.sortingOrder), "encima de Ajustes");
            Assert.That(settings.HeaderBottomDp, Is.GreaterThan(0f));
            Assert.That(sheet.PanelTopDp, Is.GreaterThanOrEqualTo(settings.HeaderBottomDp + FruitSheet.SettingsHeaderGapDp - 0.5f),
                "revisión de F4: la hoja empieza bajo la cabecera de Ajustes («Ajustes» y su X, enteros bajo el velo)");
            Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Settings), Is.False, "una ficha cada vez");

            // Atrás: primero la ficha y después Ajustes.
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(sheet.Visible, Is.False);
            Assert.That(settings.Visible, Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(settings.Visible, Is.False);
            Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Settings), Is.False, "desde Ajustes, solo con Ajustes abierta");
            Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Victory), Is.False, "sin victoria");
        }

        [UnityTest]
        public IEnumerator At100And130Percent_In640And780_TextsAtLeast12sp_NothingCut_AndTheSheetScrollsCutAtARow()
        {
            foreach (bool small in new[] { true, false })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    GameScreen.SimulatedScreen = small ? null : HudLabelTests.Screen780;
                    yield return FactLineTests.Win(3, scale: scale, small: small);
                    GameBootstrap boot = GameBootstrap.Current!;
                    FruitSheet sheet = boot.FruitSheetView!;
                    FactCatalog catalog = boot.FruitFacts!.Catalog;
                    var boto = new MemoryProgressStore(28, 24);
                    boto.MarkSkipped(27);
                    foreach ((string subject, IMapProgress progress) in new (string, IMapProgress)[]
                    {
                        ("acai", new MemoryProgressStore(301, 300)), ("agua_tapajos", boto), ("cupuacu", new MemoryProgressStore(301, 300)),
                        ("rio_amazonas", new MemoryProgressStore(301, 300)),
                    })
                    {
                        string where = subject + " a " + (small ? 640 : 780) + " dp y al " + Mathf.RoundToInt(scale * 100f) + " %";
                        Assert.That(sheet.Show(catalog.SeriesOf(subject)!, catalog, progress, FruitSheetSource.Settings), Is.True, where);
                        yield return null;
                        AssertFits(sheet, where);
                        Assert.That(sheet.Scrolls, Is.True, "20 líneas: se desplaza: " + where);
                        Assert.That(sheet.FadeBottomVisible && !sheet.FadeTopVisible, Is.True, "arriba del todo: solo el fundido de abajo: " + where);
                        AssertCutAtARow(sheet, where);
                        sheet.ScrollToEnd();
                        Assert.That(sheet.FadeTopVisible && !sheet.FadeBottomVisible, Is.True, "al final: solo el fundido de arriba: " + where);
                        sheet.Hide();
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator DismissedMs_CountsTheTimeWithTheSheetOpen_WhileTheVictoryStaysFrozen()
        {
            // Revisión de F4: el dismissed_ms de la línea es el tiempo hasta el toque en «Siguiente» (08 §8.2), también el que el
            // jugador pasa leyendo la ficha, aunque la vista de la victoria no avance mientras tanto.
            yield return FactLineTests.Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, (int)VictoryLineSlot.FruitRevealMs + (int)VictoryLineSlot.FruitFadeMs + 100);
            float since = view.VictorySlot.SinceShownMs;
            Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Victory), Is.True);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(boot.TickFruitSheet(100f), Is.True, "encima de la victoria, Update para aquí");
            }

            Assert.That(view.VictorySlot.SinceShownMs, Is.EqualTo(since), "la vista, congelada");
            Assert.That(view.VictorySlot.SheetMs, Is.EqualTo(5000f).Within(0.01f), "5 s con la ficha abierta");
            Assert.That(boot.CloseFruitSheet(), Is.True);
            Assert.That(boot.TickFruitSheet(100f), Is.False, "cerrada: no cuenta");
            PlayModeHelpers.TickExact(boot, 200);
            view.NextButton.onClick.Invoke();
            Assert.That(view.VictorySlot.DismissedMs, Is.EqualTo(since + 200f + 5000f).Within(0.01f), "con los 5 s de la ficha");
            Assert.That(view.LastRiverFactDismissedMs, Is.EqualTo(GameScreen.FruitDismissedMs(since + 200f + 5000f)).Within(0.01f),
                "el dismissed_ms de af_river_fact_shown, desde que entra la línea");
            view.VictorySlot.AddSheetMs(1000f);
            Assert.That(view.VictorySlot.SheetMs, Is.EqualTo(5000f).Within(0.01f), "cerrada la victoria, ya no suma");
        }

        [UnityTest]
        public IEnumerator InRelease_AnApprovedLineWithADraftSheetTitle_IsNotATouchTarget()
        {
            // Revisión de F4: la línea y la ficha tienen estados independientes. Con la línea aprobada y fact_sheet_title en borrador,
            // la línea se ve, pero no recibe toques ni entra en AF_LAYOUT victory, porque la ficha no se abriría.
            const string head = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args\n";
            string strings = head
                + "fact_progress,{name} · {k}/{n},,,c,28,aprobado_es,,,,\n"
                + "fact_sheet_title,Frutas del río,,,c,24,borrador,,,,\n"
                + "river_fact,Dato del río,,,c,16,aprobado_es,,,,\n";
            string facts = head
                + "fact.acai.03,Tres,,,c,90,aprobado_es,,,,\n"
                + "tint.acai.name,Açaí,,,c,24,aprobado_es,,,,\n";
            LocCatalog catalog = LocCatalog.Parse(strings, facts);
            using var log = new AfLayoutLog();
            try
            {
                yield return FactLineTests.Win(3, v => Loc.Configure(catalog, LocLocale.SpanishLatinAmerica, false));
                GameBootstrap boot = GameBootstrap.Current!;
                FactLine line = boot.View!.FruitLine!;
                PlayModeHelpers.TickExact(boot, (int)VictoryLineSlot.FruitRevealMs + (int)VictoryLineSlot.FruitFadeMs + 100);
                Assert.That(UiText.FruitSheetVisible, Is.False, "fact_sheet_title, en borrador");
                Assert.That(line.Shown && line.Alpha == 1f, Is.True, "la línea aprobada se ve");
                Assert.That(line.BodyText, Is.EqualTo("Tres"));
                Assert.That(line.Tapped, Is.Not.Null);
                Assert.That(line.Opens, Is.False, "la ficha no se abriría");
                Assert.That(line.Touchable, Is.False, "no recibe toques (ni suena)");
                string victory = log.RequireLast("victory", 3, "AF_LAYOUT victory");
                Assert.That(AfLayoutLog.ButtonIds(victory), Does.Not.Contain(FactLine.TapId), "no se anuncia en AF_LAYOUT victory");
                var ids = new List<string>();
                var rects = new List<RectTransform>();
                line.LayoutIds(ids, rects);
                Assert.That(ids, Is.Empty);
                Assert.That(boot.OpenFruitSheet("acai", FruitSheetSource.Victory), Is.False);
            }
            finally
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [Test]
        public void SourceDomain_WithoutSchemeWwwOrPath()
        {
            Assert.That(FruitSheet.SourceDomain("https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1144331/1/x.pdf"), Is.EqualTo("infoteca.cnptia.embrapa.br"));
            Assert.That(FruitSheet.SourceDomain("https://www2.camara.leg.br/legin/fed/lei/2026/x.html"), Is.EqualTo("camara.leg.br"));
            Assert.That(FruitSheet.SourceDomain("https://www.gov.br/x"), Is.EqualTo("gov.br"));
            Assert.That(FruitSheet.SourceDomain("https://es.mongabay.com/2023/03/x/"), Is.EqualTo("es.mongabay.com"));
            Assert.That(FruitSheet.SourceDomain("http://doi.org:443/10.1/x?y=1#z"), Is.EqualTo("doi.org"));
            Assert.That(FruitSheet.SourceDomain("https://www.dicio.com.br/acai"), Is.EqualTo("dicio.com.br"));
            Assert.That(FruitSheet.SourceDomain("https://www.esa.int"), Is.EqualTo("esa.int"));
            Assert.That(FruitSheet.SourceDomain("https://wwwfoo.org/x"), Is.EqualTo("wwwfoo.org"), "solo «www» con cifras");
            Assert.That(FruitSheet.SourceDomain("https://www.com/x"), Is.EqualTo("www.com"), "sin dominio detrás, se queda");
            Assert.That(FruitSheet.SourceDomain("sin dirección"), Is.EqualTo("sin dirección"));
        }

        [Test]
        public void ContrastPairs_AtLeast4_5()
        {
            foreach ((string name, Color text, Color background) in FruitSheet.ContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }
        }

        [Test]
        public void Probe_FactPageOpen_HasTintIdAndUnlocked_WithoutVisibleText()
        {
            string? text = new DevProbe().FormatFactPageOpen("acai", 3, FruitSheetSource.Victory, 3);
            Assert.That(text, Does.StartWith("AF_STATE {\"event\":\"fact_page_open\"").And.Contain("\"tint_id\":\"acai\"")
                .And.Contain("\"unlocked\":3").And.Contain("\"from\":\"victory\"").And.Contain("\"level\":3").And.Not.Contain("Açaí"));
            string? settings = new DevProbe().FormatFactPageOpen("rio_napo", 0, FruitSheetSource.Settings, 0);
            Assert.That(settings, Does.Contain("\"tint_id\":\"rio_napo\"").And.Contain("\"unlocked\":0").And.Contain("\"from\":\"settings\""));
        }

        [UnityTest]
        public IEnumerator InRelease_OnlyApprovedLines_AndWithoutApprovedNamesThereIsNoSheetNorSection()
        {
            yield return FactLineTests.Win(3);
            GameBootstrap boot = GameBootstrap.Current!;
            FruitSheet sheet = boot.FruitSheetView!;
            var series = new FactSeries("acai", FactSeriesKind.Tint, 1, 20, 1);
            var catalog = new FactCatalog(1, 90, new[] { series }, new FactSource[0]);
            var progress = new MemoryProgressStore(4, 3);
            const string head = "key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args\n";
            string strings = head
                + "fact_locked,Nivel {n},,,c,16,aprobado_es,,,,\n"
                + "fact_progress,{name} · {k}/{n},,,c,28,aprobado_es,,,,\n"
                + "fact_sheet_title,Frutas del río,,,c,24,aprobado_es,,,,\n"
                + "fact_sources,Fuentes,,,c,16,aprobado_es,,,,\n"
                + "fact_starts_at,Su serie empieza en el nivel {n},,,c,40,aprobado_es,,,,\n";
            string facts = head
                + "fact.acai.01,Uno,,,c,90,aprobado_es,,,,\n"
                + "fact.acai.02,Dos,,,c,90,pendiente_asesor,,,,\n"
                + "fact.acai.03,Tres,,,c,90,aprobado_es,,,,\n"
                + "tint.acai.name,Açaí,,,c,24,aprobado_es,,,,\n";
            try
            {
                Loc.Configure(LocCatalog.Parse(strings, facts), LocLocale.SpanishLatinAmerica, false);
                Assert.That(sheet.Show(series, catalog, progress, FruitSheetSource.Settings), Is.True);
                Assert.That(sheet.Unlocked, Is.EqualTo(3));
                Assert.That(sheet.RowRevealed(0) && sheet.RowRevealed(2), Is.True, "las aprobadas, con su texto");
                Assert.That(sheet.RowRevealed(1), Is.False, "la pendiente del asesor, en silueta aunque esté revelada");
                Assert.That(sheet.RowText(1), Is.EqualTo("Nivel 2"));
                Assert.That(sheet.ShownKeys, Is.EqualTo(new[] { "fact.acai.01", "fact.acai.03" }));
                sheet.Hide();

                // Con los textos reales, en release no hay nada aprobado todavía: ni ficha ni sección en Ajustes.
                Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SpanishLatinAmerica, false);
                FactCatalog real = boot.FruitFacts!.Catalog;
                Assert.That(sheet.Show(real.SeriesOf("acai")!, real, progress, FruitSheetSource.Settings), Is.False, "sin nombre ni textos aprobados");
                Assert.That(sheet.Visible, Is.False);
                Assert.That(UiText.FruitSheetVisible, Is.False, "fact_sheet_title y fact_progress son borradores");
            }
            finally
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        /// <summary>
        /// Arriba del todo, una fila del contenido queda cortada por el borde de abajo del visor y se ve entre el
        /// <see cref="SettingsScreen.CutRowMin"/> y el <see cref="SettingsScreen.CutRowMax"/> de su alto (revisión de F4, como
        /// Ajustes en la de AU1): ni una fila entera con una franja vacía debajo ni una franja de unos dp.
        /// </summary>
        internal static void AssertCutAtARow(FruitSheet sheet, string where)
        {
            Canvas.ForceUpdateCanvases();
            float viewH = sheet.Viewport.rect.height;
            Assert.That(viewH, Is.EqualTo(sheet.ViewportHeight).Within(0.5f), where);
            int cut = 0;
            float shown = 0f;
            for (int i = 0; i < sheet.BoxCount; i++)
            {
                (float top, float height) = sheet.Box(i);
                if (top < viewH - 0.5f && top + height > viewH + 0.5f)
                {
                    cut++;
                    shown = (viewH - top) / height;
                }
            }

            Assert.That(cut, Is.EqualTo(1), "una fila cortada por el borde de abajo del visor: " + where);
            Assert.That(shown, Is.InRange(SettingsScreen.CutRowMin - 0.01f, SettingsScreen.CutRowMax + 0.01f), "se ve a medias: " + where);
        }

        /// <summary>
        /// La ficha cabe en la pantalla (bajo la zona segura de arriba) y cada texto que se ve mide ≥ 12 sp por la escala, entero y
        /// dentro de su caja y del ancho de la hoja; el cierre, ≥ 48 dp.
        /// </summary>
        internal static void AssertFits(FruitSheet sheet, string where)
        {
            float scale = Mathf.Clamp(UiBuilder.TextScale, 1f, UiBuilder.MaxTextScale);
            RectTransform panel = sheet.Panel;
            float top = -panel.anchoredPosition.y;
            Assert.That(top, Is.GreaterThanOrEqualTo(GameScreen.MinSafeTop), "la hoja, bajo la zona segura: " + where);
            Assert.That(top + panel.rect.height, Is.LessThanOrEqualTo(sheet.CanvasHeight + 8.5f), "pegada abajo: " + where);
            Assert.That(panel.anchoredPosition.x >= 0f && panel.anchoredPosition.x + panel.rect.width <= sheet.CanvasWidth + 0.5f, Is.True, "a lo ancho: " + where);
            Canvas.ForceUpdateCanvases();
            int texts = 0;
            foreach (TextMeshProUGUI text in sheet.Texts())
            {
                texts++;
                text.ForceMeshUpdate(true, true);
                Rect r = text.rectTransform.rect;
                string what = text.name + " «" + text.text + "» (" + where + ")";
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), "≥ 12 sp: " + what);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), "nunca se trunca: " + what);
                Assert.That(text.textInfo.characterCount, Is.EqualTo(text.text.Length), "entero: " + what);
                Bounds b = text.textBounds;
                Assert.That(b.min.x >= r.xMin - 0.5f && b.max.x <= r.xMax + 0.5f, Is.True, "cabe a lo ancho: " + what);
                Assert.That(b.min.y >= r.yMin - 0.5f && b.max.y <= r.yMax + 0.5f, Is.True, "cabe a lo alto: " + what);
                RectTransform rt = text.rectTransform;
                if (rt.parent != panel)
                {
                    Assert.That(rt.anchoredPosition.x + r.width, Is.LessThanOrEqualTo(panel.rect.width - FruitSheet.InsetDp + 0.5f), "dentro de la hoja: " + what);
                }
            }

            Assert.That(texts, Is.GreaterThan(3), where);
            Assert.That(((RectTransform)sheet.CloseButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f));
        }
    }
}
