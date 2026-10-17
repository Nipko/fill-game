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
    /// del tramo; Belém con sus cinco frutas y el Encuentro con el achiote;</item>
    /// <item>una parada futura: solo la silueta y «Nivel 71»;</item>
    /// <item>los tres miradores del estuario desde el 10 (ninguno con 9), solo en la vista Mundo, y su tarjeta;</item>
    /// <item>su ←, el velo y el Atrás de Android la cierran sin cerrar el Mapa;</item>
    /// <item>ningún precio ni semilla, contraste ≥ 4,5:1, controles ≥ 48 dp y textos ≥ 12 sp sin recortes.</item>
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

            // La fuente: solo con su fila <clave>.source (hoy ninguna: el bloque map.* es de stops.py).
            Assert.That(card.SourceText, Is.EqualTo(Loc.Has("map.stop.manaos.dato_02" + StopCard.SourceSuffix)
                ? UiText.MapCardSource(Loc.Fact("map.stop.manaos.dato_02" + StopCard.SourceSuffix)) : string.Empty));

            // El miquito del tramo (51-60, mundo 3) y ninguna fruta (el achiote debuta en el 41, el agua del Negro en el 61).
            FaunaSpecies? species = StopCard.SpeciesOf(boot.Fauna, manaos);
            Assert.That(species, Is.Not.Null);
            Assert.That(species!.LivesIn(3), Is.True);
            Assert.That(card.Species, Is.SameAs(species));
            Assert.That(card.FaunaShown, Is.True);
            Assert.That(card.FaunaName, Is.EqualTo(UiText.FaunaName(species.NameKey)));
            Assert.That(card.FaunaName, Is.Not.Empty);
            Assert.That(card.FruitCount, Is.EqualTo(0));
            Assert.That(card.FutureLevelText, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Belem_HasItsFiveDebutFruits_AndTheEncuentroItsAchiote()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.OpenStopCard(0), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Stop), "Belém, siempre alcanzada");
            Assert.That(card.FruitCount, Is.EqualTo(5), "aguaje y açaí en el 1, taperebá en el 2, guanábana en el 4 y bacaba en el 7");
            var names = new List<string>();
            for (int i = 0; i < card.FruitCount; i++)
            {
                names.Add(card.FruitName(i));
                Assert.That(card.FruitName(i), Is.EqualTo(Loc.Fact(StopCard.TintNameKey(card.FruitTint(i))!)));
            }

            Assert.That(names, Is.EqualTo(new[] { "Aguaje", "Açaí", "Taperebá", "Guanábana", "Bacaba" }));
            Assert.That(card.FactKeys, Is.EqualTo(new[] { "map.stop.belem.dato_01", "map.stop.belem.dato_02" }), "sin dato de llegada: sus dos «Otro dato»");
            Assert.That(card.OtherFactShown, Is.True);

            Assert.That(map.OpenStopCard(4), Is.True);
            Assert.That(card.Stop!.Id, Is.EqualTo("encontro"));
            Assert.That(card.FruitCount, Is.EqualTo(1));
            Assert.That(card.FruitName(0), Is.EqualTo("Achiote"));
            Assert.That(card.FactKeys[0], Is.EqualTo("map.stop.manaos.dato_01"));
            Assert.That(card.Sample.Filled, Is.True);
            yield return null;
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
            Assert.That(card.Backdrop.ShapeGraphic.canvasRenderer.GetColor(), Is.EqualTo(StopCard.SilhouetteColor));
            Assert.That(card.Backdrop.HeroImage.canvasRenderer.GetColor(), Is.EqualTo(StopCard.SilhouetteColor));
            Assert.That(card.Backdrop.HeroReflection.canvasRenderer.GetAlpha(), Is.EqualTo(0f), "sin reflejo");
            var texts = new List<TextMeshProUGUI>(card.Texts());
            Assert.That(texts.Count, Is.EqualTo(1), "solo «Nivel 71»");

            // Al volver a una alcanzada, el héroe ya no va en silueta.
            Assert.That(map.OpenStopCard(5), Is.True);
            Assert.That(card.Backdrop.ShapeGraphic.canvasRenderer.GetColor(), Is.EqualTo(Color.white));
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

            // En la Cuenca, como las mini cuias, no se pintan.
            map.SetView(MapView.Basin);
            Assert.That(map.ViewpointShown(0), Is.False);
            map.SetView(MapView.World);
            Assert.That(map.ViewpointShown(0), Is.True);

            // Al tocarlo, su tarjeta: el «?», su nombre y su dato (si tiene texto).
            int marajo = ids.IndexOf("isla_de_marajo");
            Assert.That(map.TapViewpoint(marajo), Is.True);
            StopCard card = map.Card!;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Mode, Is.EqualTo(StopCardMode.Viewpoint));
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
                foreach (int n in new[] { 0, 4, 5, 7 })
                {
                    Assert.That(map.OpenStopCard(n), Is.True);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    StopCard card = map.Card!;
                    string where = "parada " + n + " al " + Mathf.RoundToInt(scale * 100f) + " %";
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
