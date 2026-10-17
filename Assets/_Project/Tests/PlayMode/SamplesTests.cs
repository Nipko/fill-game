#nullable enable
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
    /// El Muestrario del río (tarea M4.1; D-013, punto 10: Q9-A; 06 §4.5; propuesta del Mapa §7), en la escena <c>Game</c> real:
    /// con <c>max_level</c> 58, la pestaña «Muestrario · 9/n» (las 6 paradas alcanzadas, de Belém a Manaos, y los 3 miradores del
    /// estuario), una fila por mundo del catálogo con sus mini cuias en repisas de 6 como mucho (llenas del agua del sitio las
    /// alcanzadas, vacías las demás y, detrás, las muestras especiales de los miradores), el contador «Datos del río · k/n»; una
    /// muestra abre su tarjeta; Atrás cierra la tarjeta, luego el Muestrario y luego el Mapa; la pestaña tiene su fila sin pisar
    /// nada; controles ≥ 48 dp, textos ≥ 12 sp y ningún precio.
    /// </summary>
    public sealed class SamplesTests
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
        public IEnumerator Max58_TheSamplesAreRight()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverStopCatalog stops = boot.RiverStops!;
            RiverMap data = boot.RiverMapData!;
            Assert.That(data.Viewpoints.Count, Is.EqualTo(3));

            // La pestaña: 6 paradas (0-5) y 3 miradores ganados, de todas las paradas del catálogo y los miradores.
            int total = stops.Count + data.Viewpoints.Count;
            Assert.That(map.SamplesTabShown, Is.True);
            Assert.That(map.SamplesEarned, Is.EqualTo(9));
            Assert.That(map.SamplesTotal, Is.EqualTo(total));
            Assert.That(map.SamplesTabText, Is.EqualTo(UiText.MapSamples(9, total)));
            Assert.That(map.SamplesTabText, Does.StartWith("Muestrario · 9/"));

            map.SamplesTabButton!.onClick.Invoke();
            yield return null;
            Assert.That(map.SamplesSheetOpen, Is.True);
            Assert.That(map.SamplesTitle, Is.EqualTo(UiText.MapSamples(9, total)));

            // Los datos del río: los de las paradas 0-5 y de los miradores revelados, con texto, de todos los que hay.
            var keys = new List<string>();
            var seen = new HashSet<string>();
            var all = new HashSet<string>();
            for (int n = 0; n < stops.Count; n++)
            {
                StopCard.FactKeysOf(stops.At(n)!, keys);
                foreach (string k in keys)
                {
                    all.Add(k);
                    if (n <= 5)
                    {
                        seen.Add(k);
                    }
                }
            }

            foreach (RiverViewpoint v in data.Viewpoints)
            {
                if (v.Fact != null && Loc.Fact(v.Fact).Length > 0)
                {
                    all.Add(v.Fact);
                    seen.Add(v.Fact);
                }
            }

            Assert.That(map.FactsSeen, Is.EqualTo(seen.Count));
            Assert.That(map.FactsTotal, Is.EqualTo(all.Count));
            Assert.That(map.FactsSeen, Is.EqualTo(12), "Belém 2, Santarém 2, Alter do Chão 1, la Flona 2, el Encuentro 2 y Manaos 3 (los miradores aún sin texto)");
            Assert.That(map.SamplesFactsText, Is.EqualTo(UiText.MapFactsCount(seen.Count, all.Count)));

            // Una fila por mundo del catálogo, en orden; en cada una sus paradas en orden y detrás sus miradores.
            int worlds = 0;
            for (int n = 0; n < stops.Count; n++)
            {
                worlds = Mathf.Max(worlds, stops.At(n)!.World);
            }

            Assert.That(map.SampleWorldRows, Is.EqualTo(worlds));
            for (int row = 0; row < worlds; row++)
            {
                Assert.That(map.SampleWorldLabel(row), Is.EqualTo(UiText.WorldName(row + 1)));
            }

            Assert.That(map.SampleCount, Is.EqualTo(total));
            var perWorld = new int[worlds + 1];
            int previousStop = -1;
            int earned = 0;
            for (int i = 0; i < map.SampleCount; i++)
            {
                int world = map.SampleWorld(i);
                perWorld[world]++;
                int stop = map.SampleStop(i);
                if (stop >= 0)
                {
                    Assert.That(stop, Is.EqualTo(previousStop + 1), "las paradas en su orden");
                    previousStop = stop;
                    RiverStop s = stops.At(stop)!;
                    Assert.That(world, Is.EqualTo(s.World));
                    bool reached = stop <= 5;
                    Assert.That(map.SampleEarned(i), Is.EqualTo(reached), s.Id);
                    Assert.That(map.SampleCuiaAt(i).Filled, Is.EqualTo(reached), s.Id + ": llena solo si se alcanzó");
                    Assert.That(map.SampleCuiaAt(i).WaterColor, Is.EqualTo(ArrivalStrip.SampleWater(s.Sample, out _)), s.Id + ": el agua del sitio");
                    Assert.That(map.SampleButton(i).interactable, Is.True, s.Id + ": también la futura (su silueta)");
                }
                else
                {
                    Assert.That(world, Is.EqualTo(1), "los miradores del estuario, en la fila del mundo 1");
                    Assert.That(map.SampleEarned(i), Is.True, map.SampleViewpoint(i)!.Id + " revelado desde el 10");
                }

                earned += map.SampleEarned(i) ? 1 : 0;
            }

            Assert.That(earned, Is.EqualTo(9));
            Assert.That(perWorld[1], Is.EqualTo(4), "Belém y los 3 miradores");
            Assert.That(perWorld[2], Is.EqualTo(3));
            Assert.That(perWorld[3], Is.EqualTo(4));
            Assert.That(perWorld[4], Is.EqualTo(6));
            Assert.That(perWorld[5], Is.EqualTo(6), "1 + 3 + 4 + 6 + 6 = 20 en 1-200 (propuesta §7)");

            // Repisas de 6 como mucho: una por cada 6 muestras de un mundo.
            int shelves = 0;
            for (int w = 1; w <= worlds; w++)
            {
                shelves += (perWorld[w] + RiverMapScreen.SamplesPerShelf - 1) / RiverMapScreen.SamplesPerShelf;
            }

            Assert.That(map.SampleShelfCount, Is.EqualTo(shelves));
            for (int i = 0; i + 1 < map.SampleCount; i++)
            {
                if (map.SampleShelf(i) == map.SampleShelf(i + 1))
                {
                    Assert.That(map.SampleWorld(i), Is.EqualTo(map.SampleWorld(i + 1)), "una repisa no mezcla mundos");
                }
            }
        }

        [Test]
        public void CountRules_AreDeducedFromMaxLevel()
        {
            var stops = new RiverStopCatalog("test", new RiverStop[0]);
            RiverMapScreen.CountSamples(stops, null, 58, out int earned, out int total);
            Assert.That(earned, Is.EqualTo(0));
            Assert.That(total, Is.EqualTo(0));
            var viewpoint = new RiverViewpoint("isla_de_marajo", 0.5f, 0.5f, 10, null);
            Assert.That(RiverMapScreen.ViewpointRevealed(viewpoint, 9), Is.False);
            Assert.That(RiverMapScreen.ViewpointRevealed(viewpoint, 10), Is.True);
        }

        [UnityTest]
        public IEnumerator ASample_OpensItsCard_AndBackClosesCardSheetAndMap()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.OpenSamples();
            int manaos = -1;
            int tefe = -1;
            for (int i = 0; i < map.SampleCount; i++)
            {
                manaos = map.SampleStop(i) == 5 ? i : manaos;
                tefe = map.SampleStop(i) == 7 ? i : tefe;
            }

            map.SampleButton(manaos).onClick.Invoke();
            Assert.That(map.CardOpen, Is.True);
            Assert.That(map.Card!.Stop!.Id, Is.EqualTo("manaos"));
            Assert.That(map.Card.Mode, Is.EqualTo(StopCardMode.Stop));
            Assert.That(map.Card.Root.GetSiblingIndex(), Is.GreaterThan(map.SamplesSheet!.GetSiblingIndex()), "encima del Muestrario");

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.CardOpen, Is.False);
            Assert.That(map.SamplesSheetOpen, Is.True, "Atrás cierra antes la tarjeta");
            Assert.That(map.TapSample(tefe), Is.True);
            Assert.That(map.Card.Mode, Is.EqualTo(StopCardMode.Future), "Tefé, aún sin alcanzar: su silueta y «Nivel 71»");
            map.Card.CloseButton.onClick.Invoke();

            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.SamplesSheetOpen, Is.False);
            Assert.That(map.Visible, Is.True, "luego el Muestrario");
            map.OpenSamples();
            map.SamplesBackButton!.onClick.Invoke();
            Assert.That(map.SamplesSheetOpen, Is.False, "su ← lo cierra");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.Visible, Is.False, "y luego el Mapa");
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnrevealedViewpoint_IsAnEmptyGlassThatCannotBeTapped()
        {
            yield return MapMarkersTests.OpenApp(11);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(10, 9, 9), 10);
            Assert.That(map.SamplesEarned, Is.EqualTo(1), "solo Belém con 9 ganados");
            map.OpenSamples();
            for (int i = 0; i < map.SampleCount; i++)
            {
                if (map.SampleViewpoint(i) != null)
                {
                    Assert.That(map.SampleEarned(i), Is.False);
                    Assert.That(map.SampleCuiaAt(i).Filled, Is.False);
                    Assert.That(map.SampleButton(i).interactable, Is.False, "el secreto no se toca hasta que se revela");
                    Assert.That(map.TapSample(i), Is.False);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SamplesTab_HasItsOwnRow_AndEverythingIsReadableAndTouchable()
        {
            var sizes = new[] { new Vector4(1080f, 2340f, 96f, 48f), new Vector4(720f, 1280f, 48f, 0f) };
            foreach (Vector4 size in sizes)
            {
                foreach (float scale in new[] { 1f, UiBuilder.MaxTextScale })
                {
                    GameScreen.SimulatedScreen = size;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return MapMarkersTests.OpenApp(59);
                    GameBootstrap boot = GameBootstrap.Current!;
                    RiverMapScreen map = boot.RiverMapView!;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    string where = size.x + " × " + size.y + " al " + Mathf.RoundToInt(scale * 100f) + " %";

                    // La pestaña: ≥ 48 dp, entre la zona del mapa y la fila del pie, sin pisar nada.
                    Rect tab = StopCardTests.CanvasRect((RectTransform)map.SamplesTabButton!.transform, map.Canvas);
                    Rect viewport = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
                    Rect remaining = StopCardTests.CanvasRect(map.StopRemainingRect, map.Canvas);
                    Rect capsule = StopCardTests.CanvasRect((RectTransform)map.ViewWorldButton.transform, map.Canvas);
                    Rect play = StopCardTests.CanvasRect((RectTransform)map.PlayButton.transform, map.Canvas);
                    Assert.That(Mathf.Min(tab.width, tab.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f), where);
                    Assert.That(tab.yMin, Is.GreaterThanOrEqualTo(viewport.yMax), where + ": bajo la zona del mapa");
                    Assert.That(tab.yMax, Is.LessThanOrEqualTo(Mathf.Min(remaining.yMin, capsule.yMin) + 0.5f), where + ": sobre la fila del pie");
                    Assert.That(tab.Overlaps(play), Is.False, where);
                    Assert.That(tab.xMin, Is.GreaterThanOrEqualTo(-0.5f), where);
                    Assert.That(tab.xMax, Is.LessThanOrEqualTo(map.CanvasWidth + 0.5f), where);

                    var inventory = new LocInventory(LocLocale.SourceLocale, scale);
                    foreach (TextMeshProUGUI text in map.Texts())
                    {
                        Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp * scale - 0.01f), where + ": " + text.name);
                        Assert.That(inventory.ProblemsOf(text), Is.Empty, where + ": «" + text.text + "»");
                    }

                    // El Muestrario: cada muestra ≥ 48 dp, sin pisarse; sus textos legibles; ningún precio.
                    map.OpenSamples();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var rects = new List<Rect>();
                    for (int i = 0; i < map.SampleCount; i++)
                    {
                        Rect r = StopCardTests.CanvasRect((RectTransform)map.SampleButton(i).transform, map.Canvas);
                        Assert.That(Mathf.Min(r.width, r.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f), where + ": muestra " + i);
                        Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), where);
                        Assert.That(r.xMax, Is.LessThanOrEqualTo(map.CanvasWidth + 0.5f), where);
                        foreach (Rect other in rects)
                        {
                            Assert.That(Shrunk(r).Overlaps(Shrunk(other)), Is.False, where + ": muestra " + i);
                        }

                        rects.Add(r);
                    }

                    Rect back = StopCardTests.CanvasRect((RectTransform)map.SamplesBackButton!.transform, map.Canvas);
                    Assert.That(Mathf.Min(back.width, back.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f), where);
                    foreach (TextMeshProUGUI text in map.SampleTexts())
                    {
                        Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp * scale - 0.01f), where + ": " + text.name);
                        Assert.That(inventory.ProblemsOf(text), Is.Empty, where + ": «" + text.text + "»");
                    }

                    StopCardTests.AssertNoPrices(map.SamplesSheet!, map.Art, where);
                    StopCardTests.AssertNoPrices((RectTransform)map.SamplesTabButton.transform, map.Art, where);
                    LaunchOptions.Reset();
                }
            }

            // Contraste: la pestaña es la madera de «Volver a la canoa» (crema sobre madera) y el Muestrario, crema sobre el fondo.
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWood.Base), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, RiverMapScreen.BackgroundColor), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.WoodDark), Is.GreaterThanOrEqualTo(4.5f));
        }

        [UnityTest]
        public IEnumerator AfLayout_PublishesTheTab_TheViewpointsInView_AndTheOpenSheet()
        {
            using var log = new AfLayoutLog();
            yield return MapMarkersTests.OpenApp(11);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            string? line = log.LastOf("river_map", 11);
            Assert.That(line, Is.Not.Null);
            Assert.That(AfLayoutLog.ButtonIds(line!), Does.Contain("samples"));
            map.OpenSamples();
            line = log.LastOf("river_map", 11);
            Assert.That(AfLayoutLog.ButtonIds(line!), Does.Contain("samples_back").And.Contain("sample_s00"));
            map.CloseSamples();
            line = log.LastOf("river_map", 11);
            Assert.That(AfLayoutLog.ButtonIds(line!), Does.Not.Contain("samples_back"));
            yield return null;
        }

        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }
    }
}
