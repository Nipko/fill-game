#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La tarjeta de la parada del Mapa del río y los miradores (tarea M4.1; D-013, punto 10: Q9-A y Q10-A; 06 §4.5; propuesta del
    /// Mapa §5.2 y §7), en la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>la tarjeta de Manaos con <c>max_level</c> 58, completa: la mini cuia de agua negra, «Manaos · Brasil · río Negro», el
    /// fondo de la parada en pequeño, el dato de llegada y sus dos «Otro dato» en ciclo, la fuente solo si tiene fila y el miquito
    /// del tramo y el achiote, la fruta de la serie de 41-60 (D-022); Belém con sus cinco frutas (el açaí de la serie primero),
    /// Alter do Chão con el agua del Tapajós de la serie y el camu camu que debuta en el 21, y el Encuentro con el achiote;</item>
    /// <item>la fuente del dato («Fuente: …», <c>map_card_source</c>) con su fila en un catálogo de prueba: dentro de la placa,
    /// bajo el dato, ≥ 12 sp y con contraste; con los datos reales de Manaos, pendiente (el contrato de la etiqueta de la fuente no
    /// está fijado y <c>stops.py</c> no la genera: el test queda en Ignore con el motivo);</item>
    /// <item>una parada futura: solo la silueta con su filo de luz bajo el velo, sin reflejo (ni el del PNG ni el de la silueta
    /// provisional: su malla, con el tono, no baja de la línea del agua) y «Nivel 71» en la cabecera;</item>
    /// <item>los tres miradores del estuario desde el 10 (ninguno con 9), solo en la vista Mundo, y su tarjeta, sin el tono de
    /// una parada futura abierta antes;</item>
    /// <item>su ←, el velo y el Atrás de Android la cierran sin cerrar el Mapa;</item>
    /// <item>la luz de la temporada del ciclo del primer nivel de la parada, como el fondo del tablero (07 §2.1; tarea FX8): la
    /// de Manaos (51, ciclo A) y la de Alter do Chão (21, ciclo B) sobre la de la parada; Belém (1), sin temporada; sin la tabla,
    /// como antes;</item>
    /// <item>ningún precio ni semilla, contraste ≥ 4,5:1, controles ≥ 48 dp y textos ≥ 12 sp sin recortes, en las tarjetas de
    /// parada y en la de un mirador.</item>
    /// </list>
    /// </summary>
    public sealed class StopCardTests
    {
        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.SeasonLutEnabled = true;
            GameScreen.SeasonOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator ManaosCard_WithMax58_IsComplete()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverStop manaos = boot.RiverStops!.At(5)!;
            Assert.That(manaos.Id, Is.EqualTo("manaos"));

            // Tocar su mini cuia abre la tarjeta (el gancho de M3.5).
            Assert.That(map.TapStop(5), Is.True);
            yield return null;
            Assert.That(map.LastTappedStop, Is.EqualTo(5));
            Assert.That(map.CardOpen, Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Stop));
            Assert.That(card.Stop, Is.SameAs(manaos));

            // La mini cuia con el agua del sitio y la placa «{lugar} · {país} · río {río}».
            Assert.That(card.SampleShown, Is.True);
            Assert.That(card.Sample.Filled, Is.True);
            Assert.That(card.Sample.WaterColor, Is.EqualTo(ArrivalStrip.SampleWater("black", out _)), "Manaos: agua negra");
            // Sus {sep} son « · » o un salto de línea (la placa no deja un « · » colgando); entre «río» y el río, un espacio duro.
            string title = card.TitleText.Replace("\n", " · ").Replace(' ', ' ');
            Assert.That(title, Is.EqualTo("Manaos · Brasil · río Negro"));
            Assert.That(card.TitleText, Does.Not.Contain(" ·\n"));

            // El fondo de la parada en pequeño: su composición, con su héroe (provisional en desarrollo) y sin silueta.
            Assert.That(card.SceneShown, Is.True);
            Assert.That(card.Backdrop.Stop, Is.SameAs(manaos));
            Assert.That(card.Backdrop.HeroId, Is.Not.Null, "Manaos lleva su héroe (el Teatro Amazonas, provisional)");
            Assert.That(card.Backdrop.BandColor, Is.EqualTo(Palette.WaterBase("black")), "el agua del primer plano es la del mundo 3");
            Assert.That(card.Backdrop.ShapeGraphic.canvasRenderer.GetColor(), Is.EqualTo(Color.white), "sin silueta");
            Assert.That(card.SceneRect.rect.width, Is.GreaterThan(200f));
            Assert.That(card.SceneRect.rect.height, Is.GreaterThanOrEqualTo(StopCard.SceneMinDp - 0.5f));

            // El dato de llegada, con «Otro dato» que recorre los de la parada (en ciclo).
            Assert.That(card.FactKeys, Is.EqualTo(new[] { "map.stop.manaos.dato_02", "map.stop.manaos.dato_03", "map.stop.manaos.dato_04" }));
            Assert.That(card.FactKey, Is.EqualTo("map.stop.manaos.dato_02"));
            Assert.That(card.FactText, Is.EqualTo(Loc.Fact("map.stop.manaos.dato_02")));
            Assert.That(card.FactText, Does.StartWith("En Brasil el río se llama Solimões"));
            Assert.That(card.OtherFactShown, Is.True);
            card.OtherFactButton.onClick.Invoke();
            Assert.That(card.FactKey, Is.EqualTo("map.stop.manaos.dato_03"));
            Assert.That(card.FactText, Does.Contain("1902"));
            card.OtherFactButton.onClick.Invoke();
            Assert.That(card.FactKey, Is.EqualTo("map.stop.manaos.dato_04"));
            Assert.That(card.FactText, Does.Contain("flutuantes"));
            card.OtherFactButton.onClick.Invoke();
            Assert.That(card.FactKey, Is.EqualTo("map.stop.manaos.dato_02"), "vuelve al de llegada");

            // La fuente del dato: ManaosCard_ShowsTheSourceOfItsFact (pendiente) y Source_WithItsRow_IsShownInsideThePlate.
            Assert.That(card.FutureToneOn, Is.False, "una parada alcanzada, a todo color");
            Assert.That(card.HeroTone.enabled || card.ShapeTone.enabled, Is.False, "sin silueta");

            // El miquito del tramo (51-60, mundo 3).
            FaunaSpecies? species = StopCard.SpeciesOf(boot.Fauna, manaos);
            Assert.That(species, Is.Not.Null);
            Assert.That(species!.LivesIn(3), Is.True);
            Assert.That(card.Species, Is.SameAs(species));
            Assert.That(card.FaunaShown, Is.True);
            Assert.That(card.FaunaName, Is.EqualTo(UiText.FaunaName(species.NameKey)));
            Assert.That(card.FaunaName, Is.Not.Empty);

            // La fruta de la serie que corre en 51-60 (D-022; 02 §6; 06 §4.4): el urucum (achiote, 41-60 en el calendario), cuya
            // línea del 60 desplaza el dato de llegada. Ninguna debuta aquí (el achiote debuta en el 41, el agua del Negro en el 61).
            FactCatalog fruits = boot.FruitFacts!.Catalog;
            Assert.That(map.FruitSeries, Is.SameAs(fruits), "el calendario de Frutas del río llega al Mapa");
            FactSeries urucum = fruits.SeriesOf("urucum")!;
            Assert.That(urucum.FromLevel <= manaos.FirstLevel && urucum.LastLevel >= manaos.LastLevel, Is.True, "la serie del urucum cubre 51-60");
            Assert.That(StopCard.DebutTints(manaos, new List<int>()), Is.EqualTo(0));
            Assert.That(card.FruitCount, Is.EqualTo(1));
            Assert.That(card.FruitTint(0), Is.EqualTo(StopCard.TintOf("urucum")));
            Assert.That(card.FruitName(0), Is.EqualTo(Loc.Fact("tint.urucum.name")));
            Assert.That(card.FruitName(0), Is.EqualTo("Achiote"));
            Assert.That(card.FutureLevelText, Is.Empty);
        }

        [UnityTest]
        [Ignore("Pendiente [P] de M4.1: 06 §4.5 y la propuesta del Mapa §7 piden «el dato con su fuente», pero ni 08 §4.7 ni el glosario fijan dónde va la etiqueta corta de la fuente (fila <clave del dato>.source de facts.csv, otra forma o un campo de river_stops_v1.json derivado de fuente/fuentes) y tools/river-map/stops.py no la genera; lo decide el dueño y lo genera el carril del Mapa.")]
        public IEnumerator ManaosCard_ShowsTheSourceOfItsFact()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.OpenStopCard(5), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.FactKey, Is.EqualTo("map.stop.manaos.dato_02"));
            Assert.That(card.SourceText, Is.Not.Empty, "el dato de llegada de Manaos, con su fuente");
            Assert.That(card.SourceText, Is.EqualTo(UiText.MapCardSource(StopCard.SourceOf("map.stop.manaos.dato_02"))));
        }

        [UnityTest]
        public IEnumerator Source_WithItsRow_IsShownInsideThePlate()
        {
            const string factKey = "map.stop.manaos.dato_02";
            const string source = "Fuente de prueba";
            foreach (float textScale in new[] { 1f, UiBuilder.MaxTextScale })
            {
                LaunchOptions.FontScaleOverride = textScale;
                yield return MapMarkersTests.OpenApp(59);
                RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
                string where = "al " + Mathf.RoundToInt(textScale * 100f) + " %";

                // Un catálogo de prueba: las tablas del proyecto y la fila de la fuente del dato de llegada de Manaos.
#if UNITY_EDITOR
                string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
                string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
#else
                Assert.Ignore("Lee las tablas del proyecto: solo en el Editor.");
                string strings = string.Empty;
                string facts = string.Empty;
#endif
                string row = factKey + StopCard.SourceSuffix + "," + source + ",,,Prueba de M4.1: fuente corta del dato.,30,aprobado_es,,,,\n";
                Loc.Configure(LocCatalog.Parse(strings, facts + row), LocLocale.SourceLocale, true);
                try
                {
                    Assert.That(StopCard.SourceOf(factKey), Is.EqualTo(source));
                    Assert.That(map.OpenStopCard(5), Is.True);
                    Canvas.ForceUpdateCanvases();
                    StopCard card = map.Card!;
                    Assert.That(card.FactKey, Is.EqualTo(factKey));
                    Assert.That(card.SourceText, Is.EqualTo(UiText.MapCardSource(source)), where);
                    Assert.That(card.SourceText, Does.Contain(source));
                    Assert.That(card.SourceLabel.gameObject.activeInHierarchy, Is.True, where);
                    Assert.That(new List<TextMeshProUGUI>(card.Texts()), Does.Contain(card.SourceLabel), where);

                    // Dentro de la placa, bajo el dato, ≥ 12 sp, sin recortes y con contraste ≥ 4,5:1 sobre la madera oscura.
                    Rect panel = CanvasRect(card.PanelRect, map.Canvas);
                    Rect text = CanvasRect(card.SourceLabel.rectTransform, map.Canvas);
                    Rect fact = CanvasRect(card.FactLabel.rectTransform, map.Canvas);
                    Assert.That(panel.Contains(text.min + new Vector2(0.5f, 0.5f)) && panel.Contains(text.max - new Vector2(0.5f, 0.5f)), Is.True, where + ": dentro de la placa");
                    Assert.That(text.yMin, Is.GreaterThanOrEqualTo(fact.yMax - 0.5f), where + ": bajo el dato");
                    Assert.That(card.SourceLabel.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * textScale) - 0.01f), where);
                    Assert.That(new LocInventory(LocLocale.SourceLocale, textScale).ProblemsOf(card.SourceLabel), Is.Empty, where);
                    Assert.That(card.SourceLabel.color, Is.EqualTo(StopCard.SourceColor));
                    Assert.That(ColorMath.ContrastRatio(StopCard.SourceColor, Palette.WoodDark), Is.GreaterThanOrEqualTo(4.5f));

                    // Con «Otro dato», el siguiente no tiene fuente: la línea se va.
                    card.OtherFact();
                    Assert.That(card.SourceText, Is.Empty);
                    Assert.That(card.SourceLabel.gameObject.activeSelf, Is.False);
                }
                finally
                {
                    LocSetUpFixture.ConfigureEs419();
                    LaunchOptions.Reset();
                }
            }
        }

        [UnityTest]
        public IEnumerator Fruits_TheSeriesFruitFirst_ThenTheDebuts()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.OpenStopCard(0), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Stop), "Belém, siempre alcanzada");
            Assert.That(card.FruitCount, Is.EqualTo(5), "el açaí de la serie (1-20) y, de los debuts, aguaje en el 1, taperebá en el 2, guanábana en el 4 y bacaba en el 7");
            var names = new List<string>();
            for (int i = 0; i < card.FruitCount; i++)
            {
                names.Add(card.FruitName(i));
                Assert.That(card.FruitName(i), Is.EqualTo(Loc.Fact(StopCard.TintNameKey(card.FruitTint(i))!)));
            }

            Assert.That(names, Is.EqualTo(new[] { "Açaí", "Aguaje", "Taperebá", "Guanábana", "Bacaba" }), "la de la serie primero, sin repetir el açaí");
            Assert.That(card.FactKeys, Is.EqualTo(new[] { "map.stop.belem.dato_01", "map.stop.belem.dato_02" }), "sin dato de llegada: sus dos «Otro dato»");
            Assert.That(card.OtherFactShown, Is.True);

            // Alter do Chão (21-30): el agua del Tapajós de la serie de 21-40 y el camu camu, que debuta en el 21.
            Assert.That(map.OpenStopCard(2), Is.True);
            Assert.That(card.FruitCount, Is.EqualTo(2), card.Stop!.Id);
            Assert.That(card.FruitTint(0), Is.EqualTo(StopCard.TintOf("agua_tapajos")), "la fruta de la serie primero");
            Assert.That(card.FruitTint(1), Is.EqualTo(StopCard.TintOf("camu_camu")));
            Assert.That(card.FruitName(0), Is.EqualTo(Loc.Fact("tint.agua_tapajos.name")));

            Assert.That(map.OpenStopCard(4), Is.True);
            Assert.That(card.Stop!.Id, Is.EqualTo("encontro"));
            Assert.That(card.FruitCount, Is.EqualTo(1), "el urucum es la fruta de la serie y la que debuta en el 41");
            Assert.That(card.FruitName(0), Is.EqualTo("Achiote"));
            Assert.That(card.FactKeys[0], Is.EqualTo("map.stop.manaos.dato_01"));
            Assert.That(card.Sample.Filled, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Card_CarriesTheSeasonOfItsFirstLevel_LikeTheBoard()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SeasonTable seasons = boot.Seasons!;
            Assert.That(seasons, Is.Not.Null, "premisa: la escena lleva la tabla de temporada");
            Assert.That(map.Seasons, Is.SameAs(seasons), "el Mapa recibe la misma tabla que el tablero y el inicio");
            foreach ((int n, Season expected) in new[] { (5, Season.HighWater), (2, Season.LowWater), (0, Season.None), (7, Season.LowWater) })
            {
                RiverStop stop = boot.RiverStops!.At(n)!;
                string where = "parada " + n + " (" + stop.Id + ", nivel " + stop.FirstLevel + ")";
                Assert.That(seasons.SeasonOf(stop.FirstLevel), Is.EqualTo(expected), "premisa: " + where);

                // Sin la LUT de temporada (o sin tabla), como antes de FX8: solo la luz de la parada.
                GameScreen.SeasonLutEnabled = false;
                Assert.That(map.OpenStopCard(n), Is.True, where);
                StopCard card = map.Card!;
                Assert.That(card.Backdrop.Season, Is.EqualTo(Season.None), where);
                Assert.That(card.Backdrop.LutColor, Is.EqualTo(StopBackdrop.LutOf(stop)), where);
                Assert.That(card.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(stop)), where);
                Color off = card.PanoramaTint;
                card.Hide();

                // Con ella, la del ciclo del primer nivel de la parada por encima de la de la parada (la misma que el fondo).
                GameScreen.SeasonLutEnabled = true;
                Assert.That(map.OpenStopCard(n), Is.True, where);
                Assert.That(card.Backdrop.Season, Is.EqualTo(expected), where);
                Assert.That(map.StopCardSeason(stop), Is.EqualTo(expected), where);
                Assert.That(card.Backdrop.LutColor, Is.EqualTo(StopBackdrop.LutOf(stop, expected)), where);
                Assert.That(card.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(stop, expected)), where);
                if (expected == Season.None)
                {
                    Assert.That(card.PanoramaTint, Is.EqualTo(off), "sin temporada en su ciclo, la misma luz: " + where);
                }
                else
                {
                    Assert.That(card.PanoramaTint, Is.Not.EqualTo(off), "la temporada cambia la luz: " + where);
                }

                card.Hide();
            }

            // Sin la tabla, como antes.
            map.Seasons = null;
            Assert.That(map.OpenStopCard(5), Is.True);
            Assert.That(map.Card!.Backdrop.Season, Is.EqualTo(Season.None));
            Assert.That(map.Card.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(boot.RiverStops!.At(5))));
            map.Seasons = seasons;
        }

        [UnityTest]
        public IEnumerator FutureStop_ShowsOnlyTheSilhouetteAndItsLevel()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.TapStop(7), Is.True, "la mini cuia vacía del lago de Tefé se toca");
            yield return null;
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Future));
            Assert.That(card.FutureLevelText, Is.EqualTo(UiText.Level(71)), "«Nivel 71», su primer nivel");
            Assert.That(card.TitleText, Is.Empty, "sin el nombre del sitio");
            Assert.That(card.FactText, Is.Empty);
            Assert.That(card.SourceText, Is.Empty);
            Assert.That(card.OtherFactShown, Is.False);
            Assert.That(card.FaunaShown, Is.False);
            Assert.That(card.FruitCount, Is.EqualTo(0));
            Assert.That(card.SampleShown, Is.False);
            Assert.That(card.SceneShown, Is.True, "la silueta del sitio sobre su fondo");

            // El fondo entero bajo el velo de madera oscura, con el cielo y los montes apagados, y el héroe en silueta con su
            // filo de luz de 1 dp (revisión de M4.1: el misterio se lee a tamaño de teléfono).
            Canvas.ForceUpdateCanvases();
            Assert.That(card.FutureToneOn, Is.True);
            Assert.That(card.FutureVeil.color, Is.EqualTo(StopCard.FutureVeilColor));
            Rect veil = CanvasRect(card.FutureVeil.rectTransform, map.Canvas);
            Rect window = CanvasRect(card.SceneRect, map.Canvas);
            Assert.That(veil.xMin <= window.xMin + 1.5f && veil.yMin <= window.yMin + 1.5f && veil.xMax >= window.xMax - 1.5f && veil.yMax >= window.yMax - 1.5f,
                Is.True, "el velo cubre la ventana entera: " + veil + " frente a " + window);
            Assert.That(card.MuteTones.Count, Is.EqualTo(6));
            foreach (StopCard.ToneEffect tone in card.MuteTones)
            {
                Assert.That(tone.enabled, Is.True, tone.name);
                Assert.That(tone.Kind, Is.EqualTo(StopCard.ToneKind.Mute));
            }

            Assert.That(card.HeroTone.enabled && card.ShapeTone.enabled, Is.True, "el héroe, en PNG o provisional, en silueta");
            Assert.That(card.ShapeTone.Kind, Is.EqualTo(StopCard.ToneKind.Silhouette));
            float scale = card.SceneRect.rect.width / StopCard.SceneWorldWidthDp;
            Assert.That(card.ShapeTone.RimDistance * scale, Is.EqualTo(StopCard.RimLightDp).Within(0.05f), "1 dp en pantalla");
            Assert.That(card.Backdrop.ShapeGraphic.canvasRenderer.GetColor(), Is.EqualTo(Color.white), "el tono va en la malla");
            Assert.That(card.Backdrop.HeroReflection.canvasRenderer.GetAlpha(), Is.EqualTo(0f), "sin el reflejo del PNG");
            Assert.That(card.HeroTone.WaterLineY, Is.NaN, "el PNG, sin recorte: su reflejo es otra imagen");

            // Sin reflejo tampoco en la silueta provisional (la que se ve hoy), que lo pinta en su misma malla: con el tono, nada
            // de la malla baja de la línea del agua, ni el reflejo ni el filo de luz.
            StopShapeGraphic shape = card.Backdrop.ShapeGraphic;
            Assert.That(card.Backdrop.Shape, Is.EqualTo(StopShapes.LagoTefe), "la silueta provisional del lago de Tefé");
            Assert.That(shape.IsEmpty, Is.False);
            float horizon = card.Backdrop.HeroRect.y + StopBackdrop.HeroAboveDp; // k = 1: el fondo va a 360 dp de diseño
            Assert.That(card.ShapeTone.WaterLineY, Is.EqualTo(-horizon).Within(0.01f), "la línea del agua, en y local de la malla");
            Assert.That(shape.MaxY, Is.GreaterThan(horizon + 1f), "sin el tono, la malla lleva su reflejo bajo el horizonte");
            List<UIVertex> toned = TonedMesh(shape, card.ShapeTone);
            Assert.That(toned.Count, Is.GreaterThan(0));
            bool rim = false;
            bool body = false;
            foreach (UIVertex v in toned)
            {
                Assert.That(v.position.y, Is.GreaterThanOrEqualTo(-horizon - 0.01f), "nada bajo la línea del agua: ni el reflejo ni su filo de luz");
                Color c = v.color;
                rim |= Mathf.Abs(c.r - StopCard.RimLightColor.r) < 0.01f && Mathf.Abs(c.g - StopCard.RimLightColor.g) < 0.01f;
                body |= Mathf.Abs(c.r - StopCard.SilhouetteColor.r) < 0.01f && Mathf.Abs(c.g - StopCard.SilhouetteColor.g) < 0.01f;
            }

            Assert.That(rim && body, Is.True, "la silueta con su filo de luz, encima del agua");

            // «Nivel 71» en la fila de la cabecera, a la derecha del ←, centrado con él en vertical; nada debajo del fondo.
            var texts = new List<TextMeshProUGUI>(card.Texts());
            Assert.That(texts.Count, Is.EqualTo(1), "solo «Nivel 71»");
            Rect level = CanvasRect(card.FutureLevelRect, map.Canvas);
            Rect close = CanvasRect((RectTransform)card.CloseButton.transform, map.Canvas);
            Rect panel = CanvasRect(card.PanelRect, map.Canvas);
            Assert.That(level.xMin, Is.GreaterThanOrEqualTo(close.xMax), "a la derecha del ←");
            Assert.That(level.center.y, Is.EqualTo(close.center.y).Within(1f), "centrado con el ← en vertical");
            Assert.That(level.yMax, Is.LessThanOrEqualTo(window.yMin), "encima del fondo");
            Assert.That(panel.yMax - window.yMax, Is.LessThanOrEqualTo(StopCard.PadDp + 4f), "sin fila debajo del fondo");
            Assert.That(texts[0].fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));

            // Al volver a una alcanzada, el héroe ya no va en silueta y el fondo, a todo color.
            Assert.That(map.OpenStopCard(5), Is.True);
            Assert.That(card.FutureToneOn, Is.False);
            Assert.That(card.ShapeTone.enabled || card.HeroTone.enabled, Is.False);
            foreach (StopCard.ToneEffect tone in card.MuteTones)
            {
                Assert.That(tone.enabled, Is.False, tone.name);
            }

            Assert.That(card.Backdrop.HeroReflection.canvasRenderer.GetAlpha(), Is.EqualTo(1f));
        }

        [Test]
        public void ToneEffect_PaintsTheSilhouetteWithItsRimLight_AndMutesTheBackground()
        {
            var go = new GameObject("ToneTest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                StopCard.ToneEffect tone = go.AddComponent<StopCard.ToneEffect>().Silhouette(2f);
                var color = new Color32(200, 120, 40, 128);
                VertexHelper vh = Triangle(color);
                tone.ModifyMesh(vh);
                var verts = new List<UIVertex>();
                vh.GetUIVertexStream(verts);
                Assert.That(verts.Count, Is.EqualTo(15), "cuatro copias del filo y la silueta");
                for (int i = 0; i < 12; i++)
                {
                    Color c = verts[i].color;
                    Assert.That(c.r, Is.EqualTo(StopCard.RimLightColor.r).Within(0.01f));
                    Assert.That(c.a, Is.EqualTo(StopCard.RimLightColor.a * (128f / 255f)).Within(0.01f), "el alfa del vértice cuenta");
                }

                Assert.That(verts[0].position.x - verts[12].position.x, Is.EqualTo(2f).Within(0.001f), "desplazadas RimDistance");
                Assert.That(verts[3].position.x - verts[12].position.x, Is.EqualTo(-2f).Within(0.001f));
                Assert.That(verts[6].position.y - verts[12].position.y, Is.EqualTo(2f).Within(0.001f));
                Assert.That(verts[9].position.y - verts[12].position.y, Is.EqualTo(-2f).Within(0.001f));
                for (int i = 12; i < 15; i++)
                {
                    Color c = verts[i].color;
                    Assert.That(c.g, Is.EqualTo(StopCard.SilhouetteColor.g).Within(0.01f), "la silueta, encima");
                    Assert.That(c.a, Is.EqualTo(StopCard.SilhouetteColor.a * (128f / 255f)).Within(0.01f));
                }

                tone.Mute(StopCard.FutureSaturation, StopCard.FutureMultiply, StopCard.FutureVeilAlpha);
                vh = Triangle(color);
                tone.ModifyMesh(vh);
                verts.Clear();
                vh.GetUIVertexStream(verts);
                Assert.That(verts.Count, Is.EqualTo(3));
                Color expected = StopCard.FutureTone(color, StopCard.FutureSaturation, StopCard.FutureMultiply, StopCard.FutureVeilAlpha);
                Assert.That(((Color)verts[0].color).r, Is.EqualTo(expected.r).Within(0.01f));
                Assert.That(((Color)verts[0].color).a, Is.EqualTo(128f / 255f).Within(0.01f), "conserva el alfa");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // El tono: un gris queda gris, la saturación baja a 0,2 y el velo lleva a la madera oscura.
            Color gray = StopCard.FutureTone(new Color(0.5f, 0.5f, 0.5f, 1f), 0.2f, 1f, 0f);
            Assert.That(gray.r, Is.EqualTo(0.5f).Within(0.001f));
            Color green = new Color(0.2f, 0.7f, 0.3f, 1f);
            Color muted = StopCard.FutureTone(green, 0.2f, 1f, 0f);
            Assert.That(muted.g - muted.r, Is.EqualTo((green.g - green.r) * 0.2f).Within(0.001f));
            Color dark = StopCard.FutureTone(green, 1f, 0.45f, 0f);
            Assert.That(dark.g, Is.EqualTo(green.g * 0.45f).Within(0.001f));
            Color veiled = StopCard.FutureTone(green, 1f, 1f, 1f);
            Assert.That(veiled.r, Is.EqualTo(Palette.WoodDark.r).Within(0.001f));
        }

        [Test]
        public void ToneEffect_WithAWaterLine_KeepsOnlyWhatIsAboveIt()
        {
            var go = new GameObject("ToneClipTest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                StopCard.ToneEffect tone = go.AddComponent<StopCard.ToneEffect>().Silhouette(1f).ClipBelow(0f);
                Assert.That(tone.WaterLineY, Is.EqualTo(0f));
                var color = new Color32(200, 120, 40, 255);
                var vh = new VertexHelper();
                // Un triángulo que cruza la línea (de y −10 a 10) y otro entero bajo ella o en ella (el reflejo).
                vh.AddVert(new Vector3(0f, -10f, 0f), color, Vector4.zero);
                vh.AddVert(new Vector3(10f, -10f, 0f), color, Vector4.zero);
                vh.AddVert(new Vector3(0f, 10f, 0f), color, Vector4.zero);
                vh.AddVert(new Vector3(0f, 0f, 0f), color, Vector4.zero);
                vh.AddVert(new Vector3(10f, 0f, 0f), color, Vector4.zero);
                vh.AddVert(new Vector3(0f, -10f, 0f), color, Vector4.zero);
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(3, 4, 5);
                tone.ModifyMesh(vh);
                var verts = new List<UIVertex>();
                vh.GetUIVertexStream(verts);
                Assert.That(verts.Count, Is.EqualTo(5 * 3), "de cada copia, solo la parte de encima del primero (un triángulo)");
                foreach (UIVertex v in verts)
                {
                    Assert.That(v.position.y, Is.GreaterThanOrEqualTo(-0.0001f), "nada bajo la línea, tampoco la copia del filo que baja");
                }

                // La silueta (la última copia): el vértice de arriba y el corte en la línea, a media altura del lado inclinado.
                Assert.That(verts[12].position.y, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(verts[12].position.x, Is.EqualTo(5f).Within(0.0001f));
                Assert.That(verts[13].position, Is.EqualTo(new Vector3(0f, 10f, 0f)));
                Assert.That(verts[14].position, Is.EqualTo(new Vector3(0f, 0f, 0f)));
                vh.Dispose();

                // Sin línea (NaN), como antes: todo.
                tone.ClipBelow(float.NaN);
                vh = Triangle(color);
                tone.ModifyMesh(vh);
                verts.Clear();
                vh.GetUIVertexStream(verts);
                Assert.That(verts.Count, Is.EqualTo(15));
                vh.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // La malla de shape (dp desde arriba → y local −y) tras el efecto tone.
        private static List<UIVertex> TonedMesh(StopShapeGraphic shape, StopCard.ToneEffect tone)
        {
            var vh = new VertexHelper();
            for (int i = 0; i < shape.VertexCount; i++)
            {
                Vector2 p = shape.PositionAt(i);
                vh.AddVert(new Vector3(p.x, -p.y, 0f), shape.ColorAt(i), Vector4.zero);
            }

            for (int i = 0; i + 2 < shape.TriangleCount * 3; i += 3)
            {
                vh.AddTriangle(shape.TriangleIndexAt(i), shape.TriangleIndexAt(i + 1), shape.TriangleIndexAt(i + 2));
            }

            tone.ModifyMesh(vh);
            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);
            vh.Dispose();
            return verts;
        }

        private static VertexHelper Triangle(Color32 color)
        {
            var vh = new VertexHelper();
            vh.AddVert(new Vector3(0f, 0f, 0f), color, Vector4.zero);
            vh.AddVert(new Vector3(10f, 0f, 0f), color, Vector4.zero);
            vh.AddVert(new Vector3(0f, 10f, 0f), color, Vector4.zero);
            vh.AddTriangle(0, 1, 2);
            return vh;
        }

        [UnityTest]
        public IEnumerator Viewpoints_ThreeFromLevel10_OnlyInTheWorldView_AndTheirCard()
        {
            yield return MapMarkersTests.OpenApp(11); // max 10: el primer Mapa
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.ViewpointCount, Is.EqualTo(3));
            var ids = new List<string>();
            for (int i = 0; i < map.ViewpointCount; i++)
            {
                RiverViewpoint v = map.ViewpointAt(i);
                ids.Add(v.Id);
                Assert.That(v.RevealOn, Is.EqualTo(10));
                Assert.That(map.ViewpointShown(i), Is.True, v.Id + " revelado con el 10");
                RectTransform rt = (RectTransform)map.ViewpointButton(i).transform;
                Assert.That(Mathf.Min(rt.rect.width, rt.rect.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.01f), "≥ 48 dp");
                Vector2 expected = map.Space.ToLocal(new Vector2(v.X, v.Y));
                Assert.That(Vector2.Distance(map.ViewpointLocal(i), expected), Is.LessThan(0.01f), "en su punto");
            }

            Assert.That(ids, Is.EquivalentTo(new[] { "boca_del_amazonas", "macapa_y_la_linea_del_ecuador", "isla_de_marajo" }));

            // Encuadrados (capturas), los tres caben enteros con su área táctil, a ≥ 8 dp del marco; un cambio de vista vuelve
            // al encuadre de siempre.
            float zoom = map.Zoom;
            Assert.That(map.FrameViewpoints(), Is.True);
            Assert.That(map.Zoom, Is.LessThanOrEqualTo(zoom));
            SamplesSnapshotTests.AssertViewpointsInView(map);
            map.SetView(MapView.Basin);
            map.SetView(MapView.World);
            Assert.That(map.Zoom, Is.EqualTo(zoom).Within(0.001f));

            // En la Cuenca, como las mini cuias, no se pintan.
            map.SetView(MapView.Basin);
            Assert.That(map.ViewpointShown(0), Is.False);
            map.SetView(MapView.World);
            Assert.That(map.ViewpointShown(0), Is.True);

            // Antes, una parada futura (Tefé, con max 10), con su tono: cerrar su tarjeta no lo quita.
            Assert.That(map.OpenStopCard(7), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Future));
            Assert.That(card.FutureToneOn, Is.True);
            card.CloseButton.onClick.Invoke();

            // Al tocarlo, su tarjeta: el «?», su nombre y su dato (si tiene texto), sin el tono de la parada futura.
            int marajo = ids.IndexOf("isla_de_marajo");
            Assert.That(map.TapViewpoint(marajo), Is.True);
            Assert.That(map.Card, Is.SameAs(card));
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Viewpoint));
            Assert.That(card.FutureToneOn, Is.False, "el tono de la parada futura no se queda puesto");
            Assert.That(card.FutureVeil.gameObject.activeSelf, Is.False);
            Assert.That(card.HeroTone.enabled || card.ShapeTone.enabled, Is.False);
            foreach (StopCard.ToneEffect tone in card.MuteTones)
            {
                Assert.That(tone.enabled, Is.False, tone.name);
            }

            Assert.That(card.ViewpointBadgeShown, Is.True);
            Assert.That(card.TitleText, Is.EqualTo("Isla de Marajó"));
            Assert.That(card.FactText, Is.EqualTo(Loc.Fact(map.ViewpointAt(marajo).Fact ?? "-")));
            Assert.That(card.SceneShown, Is.False);
            Assert.That(card.SampleShown, Is.False);
            card.CloseButton.onClick.Invoke();

            // Con 9 ganados, ninguno.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(10, 9, 9), 10);
            for (int i = 0; i < map.ViewpointCount; i++)
            {
                Assert.That(map.ViewpointShown(i), Is.False, map.ViewpointAt(i).Id + " sin revelar con 9");
            }

            Assert.That(map.OpenViewpointCard(marajo), Is.False, "un mirador sin revelar no tiene tarjeta");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Card_ClosesWithItsArrowTheScrimAndBack_WithoutClosingTheMap()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            using (var log = new AfLayoutLog())
            {
                Assert.That(map.OpenStopCard(5), Is.True);
                string? layout = log.LastOf("river_map", 59);
                Assert.That(layout, Is.Not.Null, "AF_LAYOUT al abrir la tarjeta");
                Assert.That(AfLayoutLog.ButtonIds(layout!), Does.Contain("card_close").And.Contain("card_other_fact"));
            }

            map.Card!.CloseButton.onClick.Invoke();
            Assert.That(map.CardOpen, Is.False);
            Assert.That(map.Visible, Is.True);

            Assert.That(map.OpenStopCard(5), Is.True);
            Transform scrim = map.Card!.Root.Find("ScrimButton");
            Assert.That(scrim, Is.Not.Null);
            scrim.GetComponent<Button>().onClick.Invoke();
            Assert.That(map.CardOpen, Is.False, "el velo la cierra");

            Assert.That(map.OpenStopCard(5), Is.True);
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.CardOpen, Is.False, "Atrás cierra la tarjeta");
            Assert.That(map.Visible, Is.True, "y no el Mapa");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.Visible, Is.False, "el segundo Atrás cierra el Mapa");

            // Cerrar el Mapa con la tarjeta abierta la cierra también.
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.OpenStopCard(5), Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(map.CardOpen, Is.False);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.CardOpen, Is.False, "al volver a abrir, sin la tarjeta");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Card_HasNoPrices_ReadableTextsAndTouchTargets()
        {
            foreach (float scale in new[] { 1f, UiBuilder.MaxTextScale })
            {
                LaunchOptions.FontScaleOverride = scale;
                yield return MapMarkersTests.OpenApp(59);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                var inventory = new LocInventory(LocLocale.SourceLocale, scale);
                int marajo = -1;
                for (int i = 0; i < map.ViewpointCount; i++)
                {
                    marajo = map.ViewpointAt(i).Id == "isla_de_marajo" ? i : marajo;
                }

                Assert.That(marajo, Is.GreaterThanOrEqualTo(0));

                // Las paradas 0, 4, 5 y 7 (la futura) y, con -1, la tarjeta del mirador de Marajó (revelado desde el 10), que tiene
                // su propia cabecera con el «?».
                foreach (int n in new[] { 0, 4, 5, 7, -1 })
                {
                    Assert.That(n >= 0 ? map.OpenStopCard(n) : map.OpenViewpointCard(marajo), Is.True);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    StopCard card = map.Card!;
                    string where = (n >= 0 ? "parada " + n : "mirador de Marajó") + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                    if (n < 0)
                    {
                        Assert.That(card.Mode, Is.EqualTo(StopCardMode.Viewpoint));
                        Assert.That(card.ViewpointBadgeShown, Is.True);
                        Assert.That(card.TitleText, Is.EqualTo("Isla de Marajó"), where);
                        Assert.That(new List<TextMeshProUGUI>(card.Texts()), Is.Not.Empty, where + ": su nombre (y su dato, si tiene texto)");
                    }

                    AssertNoPrices(card.Root, map.Art, where);
                    foreach (Button b in card.Buttons())
                    {
                        Rect r = ((RectTransform)b.transform).rect;
                        Assert.That(Mathf.Min(r.width, r.height), Is.GreaterThanOrEqualTo(StopCard.MinTouchDp - 0.01f), where + ": " + b.name + " ≥ 48 dp");
                    }

                    Rect panel = CanvasRect(card.PanelRect, map.Canvas);
                    Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(map.SafeTop - 0.5f), where + ": bajo el recorte");
                    Assert.That(panel.yMax, Is.LessThanOrEqualTo(map.CanvasHeight - map.SafeBottom + 0.5f), where + ": sobre la zona de gestos");
                    foreach (TextMeshProUGUI text in card.Texts())
                    {
                        Assert.That(text.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), where + ": " + text.name);
                        Assert.That(inventory.ProblemsOf(text), Is.Empty, where + ": «" + text.text + "»");
                        Rect t = CanvasRect(text.rectTransform, map.Canvas);
                        Assert.That(panel.Contains(t.min + new Vector2(0.5f, 0.5f)) && panel.Contains(t.max - new Vector2(0.5f, 0.5f)), Is.True, where + ": " + text.name + " dentro de la placa");
                    }
                }

                LaunchOptions.Reset();
            }

            foreach ((string name, Color text, Color background) in StopCard.ContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }
        }

        [Test]
        public void TintDebuts_AreTheGlossaryDebuts()
        {
            var levels = new SortedSet<int>();
            var tints = new HashSet<int>();
            for (int i = 0; i < StopCard.TintDebuts.GetLength(0); i++)
            {
                levels.Add(StopCard.TintDebuts[i, 0]);
                Assert.That(tints.Add(StopCard.TintDebuts[i, 1]), Is.True, "cada tinte debuta una vez");
                Assert.That(StopCard.TintNameKey(StopCard.TintDebuts[i, 1]), Does.StartWith("tint.").And.EndWith(".name"));
            }

            Assert.That(tints.Count, Is.EqualTo(14), "los 14 tintes de la paleta (glosario §9)");
            Assert.That(levels, Is.EqualTo(SightingRule.FruitDebutLevels), "los mismos niveles que las tarjetas «Fruta nueva»");

            // Los 14 pares (nivel, tinte) de la columna «Debut» del glosario §9, fijados aquí: un cambio en la tabla se detecta.
            var expected = new[]
            {
                "1 aguaje", "1 acai", "2 tapereba", "4 guanabana", "7 bacaba", "11 agua_tapajos", "21 camu_camu", "41 urucum",
                "61 agua_negro", "81 cupuacu", "111 lulo", "141 jenipapo", "201 cacao", "301 araza",
            };
            var actual = new List<string>();
            for (int i = 0; i < StopCard.TintDebuts.GetLength(0); i++)
            {
                actual.Add(StopCard.TintDebuts[i, 0] + " " + ProceduralArt.TintIds[StopCard.TintDebuts[i, 1]]);
            }

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(StopCard.TintNameKey(0), Is.Null);
            Assert.That(StopCard.TintNameKey(15), Is.Null);
        }

        // Ni precios ni semillas (Q10-A): ningún texto con moneda o «semilla» y ninguna imagen del icono de semillas.
        internal static void AssertNoPrices(RectTransform root, ProceduralArt art, string where)
        {
            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                string value = text.text;
                foreach (string bad in new[] { "$", "€", "S/", "R$", "USD", "COP", "PEN", "BRL", "semilla", "Semilla", UiText.Seeds })
                {
                    Assert.That(value.IndexOf(bad, StringComparison.Ordinal), Is.LessThan(0), where + ": «" + value + "» lleva «" + bad + "»");
                }
            }

            Sprite seed = art.IconSeed;
            foreach (Image image in root.GetComponentsInChildren<Image>(false))
            {
                Assert.That(image.sprite, Is.Not.SameAs(seed), where + ": " + image.name + " sin el icono de semillas");
            }
        }

        // Caja de rt en dp del lienzo, con el origen arriba a la izquierda.
        internal static Rect CanvasRect(RectTransform rt, Canvas canvas)
        {
            var root = (RectTransform)canvas.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
