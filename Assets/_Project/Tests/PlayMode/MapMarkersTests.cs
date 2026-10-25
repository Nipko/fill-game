#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los marcadores del Mapa del río (tarea M3.5; D-013, puntos 3 y 6: Q3-A, Q17-A y Q18-A; propuesta del Mapa §3.2, §3.3,
    /// §4.1 y §6.1), en la escena <c>Game</c> real con <c>river_map_v1.json</c> y <c>river_stops_v1.json</c>:
    /// <list type="bullet">
    /// <item>la canoa de Capi con las banderas izadas según <c>max_level</c>: BR, luego CO (80) y PE (140), la más reciente
    /// arriba del mástil;</item>
    /// <item>Rosa nada delante de la canoa por el agua; en los mundos 7 y 8, el bufeo boliviano, con el relevo en Teotônio;</item>
    /// <item>las mini cuias de las paradas, tocables a ≥ 48 dp, llenas del agua del sitio las alcanzadas (con canto crema las que
    /// faltan) y en abanico en el Trapecio; todas las que se ven, a ≥ 48 dp entre centros y sin pisar la canoa con su mástil ni el
    /// guía (en el 121, el 141 y el 306); la baliza de los cierres de mundo, encendida y con su halo en el 40, el 80 y el 140, sin
    /// que la tape la canoa; con B14, el «Premio del mundo · faltan N» solo bajo el cierre del mundo con skin sin ganar;</item>
    /// <item>la casa de TiTino en Guajará y, fuera de la vista, la ventanita con «Casa · a unos 1.480 km» en el mundo 3 (la píldora
    /// bajo el retrato, a ≥ 8 dp de toda mini cuia), que lleva a la casa; <c>sloth_sleep</c> a los 30 s sin toques; cero
    /// asignaciones con el Mapa quieto.</item>
    /// </list>
    /// </summary>
    public sealed class MapMarkersTests
    {
        private const string Nbsp = " ";
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true; // el arte por código (D-052: en todos los builds)
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [Test]
        public void CanoeFlags_FollowMax_BR_ThenCO_ThenPE_TheNewestLast()
        {
            RiverStopCatalog stops = Catalog();
            var into = new string?[RiverMapScreen.MaxCanoeFlags];
            AssertFlags(stops, 0, into, "BR");
            AssertFlags(stops, 58, into, "BR");
            AssertFlags(stops, 79, into, "BR");
            AssertFlags(stops, 80, into, "BR", "CO");
            AssertFlags(stops, 139, into, "BR", "CO");
            AssertFlags(stops, 140, into, "BR", "CO", "PE");
            AssertFlags(stops, 220, into, "BR", "CO", "PE", "EC");
            AssertFlags(stops, 300, into, "BR", "CO", "PE", "EC", "BO");
            AssertFlags(stops, 310, into, "BR", "CO", "EC", "BO", "PE"); // vuelve al Perú: su bandera pasa a ser la más reciente
            Assert.That(() => { RiverMapScreen.CanoeFlags(stops, 140, into); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Beacon_IsLitAt40_80And140_TheEndOfEachWorld()
        {
            RiverStopCatalog stops = Catalog();
            foreach (int n in new[] { 3, 7, 13 })
            {
                RiverStop stop = stops.At(n)!;
                Assert.That(stop.Kind, Is.EqualTo(StopKind.Close), stop.Id);
                Assert.That(RiverMapScreen.BeaconLit(stop, stop.LastLevel - 1), Is.False, stop.Id + " con " + (stop.LastLevel - 1));
                Assert.That(RiverMapScreen.BeaconLit(stop, stop.LastLevel), Is.True, stop.Id + " con " + stop.LastLevel);
            }

            Assert.That(stops.At(3)!.LastLevel, Is.EqualTo(40));
            Assert.That(stops.At(7)!.LastLevel, Is.EqualTo(80));
            Assert.That(stops.At(13)!.LastLevel, Is.EqualTo(140));
            Assert.That(RiverMapScreen.BeaconLit(stops.At(4)!, 1000), Is.False, "una gran parada no lleva baliza");
            Assert.That(RiverMapScreen.BeaconLit(stops.At(5)!, 1000), Is.False, "una parada tampoco");
        }

        [UnityTest]
        public IEnumerator Max58_TheCanoeFliesBR_RosaSwimsAhead_AndTheMiniCuiasOfWorlds1To3()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            Assert.That(map.View, Is.EqualTo(MapView.World));

            // Las banderas: solo la de Brasil, en el mástil de popa.
            Assert.That(map.CanoeFlagCount, Is.EqualTo(1));
            Assert.That(map.CanoeFlag(0), Is.EqualTo("BR"));
            Assert.That(map.Mast!.gameObject.activeSelf, Is.True);
            Assert.That(map.CanoeFlagImage(0).sprite, Is.Not.Null);
            Assert.That(map.CanoeShownMapPoint, Is.EqualTo(map.CanoeMapPoint), "sin nada que ponerse al día, la canoa en su sitio");

            // Rosa nada delante, por el agua: más allá de los 8/10 del tramo de Manaos a Anavilhanas, o esperando en Anavilhanas;
            // si a la canoa le queda muy poco (este tramo mide unos 66 dp a esta escala: quedan 13), encima de ella. Nunca por el
            // tramo siguiente, que vuelve río abajo hacia Tefé.
            Assert.That(map.GuideShown, Is.True);
            Assert.That(map.GuideId, Is.EqualTo(RiverJourney.GuideRosa));
            Assert.That((map.GuideRoute == 6 && map.GuideRouteFraction > 0.8f) || map.GuideRoute == -1, Is.True,
                "Rosa, delante: ruta " + map.GuideRoute + " a " + map.GuideRouteFraction.ToString("0.00", CultureInfo.InvariantCulture));
            if (map.GuideRoute == 6)
            {
                Vector2 expected = RiverMapScreen.RoutePoint(data, routes.At(6)!, map.GuideRouteFraction);
                Assert.That(Vector2.Distance(map.GuideMapPoint, expected), Is.LessThan(1e-5f), "por el agua");
            }
            else
            {
                Assert.That(map.GuideMapPoint.y, Is.LessThan(map.CanoeMapPoint.y), "encima de la canoa (y del marco hacia abajo)");
            }

            // En el 60, con la canoa en Anavilhanas, ya guía el tramo siguiente.
            Assert.That(RiverMapScreen.GuidePoint(data, routes, 60, 0.02f, 0.01f, out _, out int next, out float at), Is.True);
            Assert.That(next, Is.EqualTo(7));
            Assert.That(at, Is.GreaterThan(0f));
            float aheadDp = Vector2.Distance(map.GuideMapPoint, map.CanoeMapPoint) * map.Space.DpPerUnit;
            Assert.That(aheadDp, Is.InRange(1f, RiverMapScreen.GuideLeadDp + 0.5f), "a lo sumo 35 dp por el agua");
            float legDp = RiverMapScreen.RouteLength(data, routes.At(6)!) * map.Space.DpPerUnit;
            Debug.Log("AF_MAP guía con max 58: ruta " + map.GuideRoute + " a " + map.GuideRouteFraction.ToString("0.00", CultureInfo.InvariantCulture)
                + ", " + aheadDp.ToString("0.0", CultureInfo.InvariantCulture) + " dp de la canoa; tramo de " + legDp.ToString("0", CultureInfo.InvariantCulture) + " dp");
            Assert.That(map.GuideImage!.sprite, Is.SameAs(map.Art.Rosa));
            Assert.That(map.GuideImage.transform.parent.GetSiblingIndex(), Is.GreaterThan(map.Canoe.transform.GetSiblingIndex()), "delante de la canoa");

            // Las mini cuias de los mundos 1-3 (k = 0-7): llenas las alcanzadas (0-5), de vidrio Anavilhanas y Tefé.
            Assert.That(map.StopMarkersShown, Is.True);
            Assert.That(map.StopMarkerCount, Is.EqualTo(8));
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                int n = map.StopMarkerStop(i);
                Assert.That(n, Is.EqualTo(i));
                Assert.That(map.StopMarkerReached(i), Is.EqualTo(n <= 5), "parada " + n);
                Assert.That(map.StopMarkerFill(i), Is.EqualTo(n <= 5 ? 1f : 0f), "parada " + n);
                Assert.That(map.StopMarkerRimShown(i), Is.EqualTo(n > 5), "el canto crema, solo en las que faltan: parada " + n);
                RectTransform tap = (RectTransform)map.StopMarkerButton(i).transform;
                Assert.That(tap.rect.width, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp), "≥ 48 dp");
                Assert.That(tap.rect.height, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp), "≥ 48 dp");
            }

            // La baliza de Flona Tapajós (cierre del mundo 2), encendida; la de Tefé (cierre del 3), apagada.
            Assert.That(map.StopMarkerHasBeacon(3) && map.StopMarkerBeaconLit(3), Is.True, "flona_tapajos, con 58 ≥ 40");
            Assert.That(map.StopMarkerBeaconHaloShown(3), Is.True, "la encendida, con su halo ámbar");
            Assert.That(map.StopMarkerHasBeacon(7) && !map.StopMarkerBeaconLit(7), Is.True, "tefe, con 58 < 80");
            Assert.That(map.StopMarkerBeaconHaloShown(7), Is.False, "la apagada, sin halo");
            Assert.That(map.StopMarkerHasBeacon(5), Is.False, "Manaos no cierra el mundo");

            // B14 (06 §4.5): el «Premio del mundo · faltan N» va bajo el cierre del mundo con skin aún sin ganar (Tefé, el 80:
            // faltan 22), y en ningún otro sitio del Mapa.
            Assert.That(map.PrizeMarker, Is.EqualTo(7), "tefe");
            Assert.That(map.PrizeText, Is.EqualTo("Premio del mundo · faltan 22"));
            foreach (TMP_Text text in map.Canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text.Contains("Premio"))
                {
                    Assert.That(text.transform.IsChildOf(map.StopMarkerButton(7).transform), Is.True, text.name);
                }
            }

            // En la Cuenca, a 1×, las mini cuias no caben: no se pintan. La canoa, el guía y la casa, sí.
            map.SetView(MapView.Basin);
            Assert.That(map.StopMarkersShown, Is.False);
            Assert.That(map.GuideShown, Is.True);
            Assert.That(map.HomeShown, Is.True);
        }

        [UnityTest]
        public IEnumerator Max52_RosaSwims35dpAheadByTheWater_OnTheRouteToAnavilhanas()
        {
            yield return OpenApp(53); // max 52: la canoa va a 2/10 del tramo de Manaos a Anavilhanas
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            Assert.That(map.CanoeShownLevel, Is.EqualTo(52f));
            float legDp = RiverMapScreen.RouteLength(data, routes.At(6)!) * map.Space.DpPerUnit;
            Assert.That(legDp * 0.8f, Is.GreaterThan(RiverMapScreen.GuideLeadDp), "a la canoa le quedan más de 35 dp hasta Anavilhanas");

            Assert.That(map.GuideShown, Is.True);
            Assert.That(map.GuideRoute, Is.EqualTo(6), "Rosa nada por la ruta de Anavilhanas, delante de la canoa");
            Vector2 expected = RiverMapScreen.RoutePoint(data, routes.At(6)!, map.GuideRouteFraction);
            Assert.That(Vector2.Distance(map.GuideMapPoint, expected), Is.LessThan(1e-5f), "sobre la ruta");
            float swumDp = (map.GuideRouteFraction - 0.2f) * legDp;
            Debug.Log("AF_MAP guía con max 52: ruta " + map.GuideRoute + " a " + map.GuideRouteFraction.ToString("0.00", CultureInfo.InvariantCulture)
                + ", " + swumDp.ToString("0.0", CultureInfo.InvariantCulture) + " dp por el agua; tramo de " + legDp.ToString("0", CultureInfo.InvariantCulture) + " dp");
            Assert.That(swumDp, Is.EqualTo(RiverMapScreen.GuideLeadDp).Within(0.5f), "a unos 35 dp por el agua");
            Assert.That(map.CanoeFacing * (map.GuideMapPoint.x - map.CanoeMapPoint.x), Is.GreaterThanOrEqualTo(0f), "la proa mira hacia Rosa");
        }

        [UnityTest]
        public IEnumerator Flags_At80And140_AddCOAndPE_TheNewestOnTop()
        {
            yield return OpenApp(81);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.CanoeFlagCount, Is.EqualTo(2));
            Assert.That(map.CanoeFlag(0) + map.CanoeFlag(1), Is.EqualTo("BRCO"));
            Assert.That(map.StopMarkerBeaconLit(map.StopMarkerIndexOf(7)), Is.True, "Tefé cierra el mundo 3 en el 80");

            yield return OpenApp(141);
            map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.CanoeFlagCount, Is.EqualTo(3));
            Assert.That(map.CanoeFlag(0) + map.CanoeFlag(1) + map.CanoeFlag(2), Is.EqualTo("BRCOPE"));
            int frontier = map.StopMarkerIndexOf(13);
            Assert.That(map.StopMarkerBeaconLit(frontier), Is.True, "el río frontera cierra el mundo 4 en el 140");
            Assert.That(map.StopMarkerBeaconHaloShown(frontier), Is.True);
            Rect beacon = map.StopMarkerBeaconLocal(frontier);
            Assert.That(beacon.Overlaps(map.CanoeGearBoxLocal), Is.False, "la canoa, el mástil y las banderas no tapan la baliza");
            Assert.That(beacon.Overlaps(map.GuideBoxLocal), Is.False, "ni el guía");
            float br = map.CanoeFlagImage(0).rectTransform.anchoredPosition.y;
            float co = map.CanoeFlagImage(1).rectTransform.anchoredPosition.y;
            float pe = map.CanoeFlagImage(2).rectTransform.anchoredPosition.y;
            Assert.That(pe, Is.GreaterThan(co), "la del Perú, la más reciente, arriba");
            Assert.That(co, Is.GreaterThan(br));
            RectTransform mast = map.Mast!.rectTransform;
            Assert.That(pe + (map.CanoeFlagImage(2).rectTransform.rect.height * 0.5f), Is.LessThanOrEqualTo(mast.anchoredPosition.y + mast.rect.height), "dentro del mástil");
            Assert.That(br - (map.CanoeFlagImage(0).rectTransform.rect.height * 0.5f), Is.GreaterThanOrEqualTo(mast.anchoredPosition.y));
        }

        [UnityTest]
        public IEnumerator Guide_IsTheBufeo_InWorlds7And8_WithTheRelayAtTeotonio()
        {
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            RiverStopCatalog stops = boot.RiverStops!;

            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 58), Is.EqualTo(RiverJourney.GuideRosa));
            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 290), Is.EqualTo(RiverJourney.GuideRosa), "en el mundo 6");
            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 290.1f), Is.EqualTo(RiverJourney.GuideRosa), "sale de Ecuador con Rosa");
            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 300), Is.EqualTo(RiverJourney.GuideBufeo), "llega a Bolivia con el bufeo");
            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 305), Is.EqualTo(RiverJourney.GuideBufeo), "mundo 7");
            float relay = RiverMapScreen.PlaceFraction(data, routes.At(30)!, data.PassingPoint("teotonio")!.At);
            Assert.That(relay, Is.InRange(0.01f, 0.99f), "el viaje del 300 pasa por los rápidos de Teotônio");
            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 290f + (10f * relay) - 0.05f), Is.EqualTo(RiverJourney.GuideRosa), "antes de los rápidos");
            Assert.That(RiverMapScreen.GuideAt(data, routes, stops, 290f + (10f * relay) + 0.05f), Is.EqualTo(RiverJourney.GuideBufeo), "después");

            // En el Mapa, con max_level 305 (mundo 7): el bufeo y las banderas BR, CO, PE, EC y BO.
            Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            Assert.That(map.GuideId, Is.EqualTo(RiverJourney.GuideBufeo));
            Assert.That(map.GuideImage!.sprite, Is.SameAs(map.Art.MapBufeo));
            Assert.That(map.CanoeFlagCount, Is.EqualTo(5));
            Assert.That(map.CanoeFlag(0) + map.CanoeFlag(1) + map.CanoeFlag(2) + map.CanoeFlag(3) + map.CanoeFlag(4), Is.EqualTo("BRCOPEECBO"),
                "Bolivia, la más reciente (el Perú vuelve arriba en el 310)");
        }

        [UnityTest]
        public IEnumerator MiniCuiasThatShow_AreAtLeast48dpApart_OffTheCanoe_In121_141And306()
        {
            yield return OpenApp(121); // mundo 4: las 6 paradas del Trapecio, Leticia y su atardecer en el mismo punto
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.View, Is.EqualTo(MapView.World));
            int leticia = map.StopMarkerIndexOf(8);
            int dusk = map.StopMarkerIndexOf(9);
            Assert.That(leticia >= 0 && dusk >= 0, Is.True);
            Assert.That(map.StopMarkerRealLocal(leticia), Is.EqualTo(map.StopMarkerRealLocal(dusk)), "misma vista, otra luz: el mismo punto");
            Assert.That(map.StopMarkerFanned(leticia) && map.StopMarkerFanned(dusk), Is.True, "en abanico");
            Assert.That(map.StopMarkerHasLeader(leticia) && map.StopMarkerHasLeader(dusk), Is.True, "con su línea al punto real");
            AssertMiniCuiasClear(map, "121");

            // Tocar una mini cuia cuenta para map_close (stops_tapped) y la deja para la tarjeta de la parada (M4.1).
            map.StopMarkerButton(dusk).onClick.Invoke();
            Assert.That(map.LastTappedStop, Is.EqualTo(9));
            MapCloseStats stats = map.Close();
            Assert.That(stats.StopsTapped, Is.EqualTo(1));

            yield return OpenApp(141);
            AssertMiniCuiasClear(CurrentMap(), "141");

            yield return OpenApp(59);
            boot = GameBootstrap.Current!;
            Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            AssertMiniCuiasClear(boot.RiverMapView!, "306");
        }

        /// <summary>
        /// Revisión de FX14: las líneas finas de las mini cuias en abanico van en el lienzo estático, debajo del vidrio de los ríos
        /// (el río las tapa donde lo cruzan o lo recorren), y sus puntos reales, encima, con las mini cuias: dos dibujos con las
        /// mismas líneas, uno sin puntos y otro sin líneas (nada se dibuja dos veces). Fuera del Mundo (en la Cuenca), las líneas se
        /// apagan con las mini cuias.
        /// </summary>
        [UnityTest]
        public IEnumerator LeaderLines_GoUnderTheRivers_TheirDotsOnTop_AndOffInTheBasin()
        {
            yield return OpenApp(121); // mundo 4: Leticia y su atardecer, en abanico con su línea
            RiverMapScreen map = CurrentMap();
            Assert.That(map.View, Is.EqualTo(MapView.World));
            int fanned = 0;
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                fanned += map.StopMarkerHasLeader(i) ? 1 : 0;
            }

            Assert.That(fanned, Is.GreaterThan(0), "premisa: hay mini cuias con su línea");
            RiverMapScreen.LeaderLinesGraphic lines = map.LeaderLines;
            RiverMapScreen.LeaderLinesGraphic dots = map.LeaderDots;
            Assert.That(lines, Is.Not.SameAs(dots));
            Assert.That(lines.transform.parent, Is.SameAs(map.StaticLayer), "las líneas, en el lienzo estático");
            Assert.That(map.GlassLayer.parent, Is.SameAs(map.StaticLayer));
            Assert.That(lines.transform.GetSiblingIndex(), Is.LessThan(map.GlassLayer.GetSiblingIndex()), "las líneas, debajo del vidrio");
            Assert.That(dots.transform.IsChildOf(map.StaticLayer), Is.False, "los puntos, fuera del lienzo estático (encima)");
            Assert.That(lines.DrawLines && !lines.DrawDots, Is.True, "debajo, solo las líneas");
            Assert.That(dots.DrawDots && !dots.DrawLines, Is.True, "encima, solo los puntos");
            Assert.That(lines.Count, Is.GreaterThan(0), "con líneas");
            Assert.That(lines.Count, Is.EqualTo(dots.Count), "las mismas líneas y puntos");
            Assert.That(lines.gameObject.activeInHierarchy && dots.gameObject.activeInHierarchy, Is.True, "en el Mundo, las dos");

            map.SetView(MapView.Basin);
            map.Tick(16f);
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            Assert.That(lines.gameObject.activeInHierarchy, Is.False, "en la Cuenca, sin las líneas");
            Assert.That(dots.gameObject.activeInHierarchy, Is.False, "en la Cuenca, sin los puntos");

            map.SetView(MapView.World);
            map.Tick(16f);
            Assert.That(lines.gameObject.activeInHierarchy && dots.gameObject.activeInHierarchy, Is.True, "de vuelta al Mundo, las dos");
            Assert.That(lines.Count, Is.EqualTo(dots.Count), "de vuelta al Mundo, las mismas");
        }

        [UnityTest]
        public IEnumerator HomeWindow_SaysAbout1480Km_InWorld3_AndTappingItGoesToGuajara()
        {
            using var layout = new AfLayoutLog();
            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.World, Is.EqualTo(3));
            Assert.That(map.HomeShown, Is.True, "la casa de TiTino, en Guajará");
            Assert.That(Vector2.Distance(map.HomeMapPoint, RiverMapScreen.PlacePoint(boot.RiverMapData!, boot.RiverMapData!.Home)), Is.LessThan(1e-6f));
            Assert.That(map.HomeInView, Is.False, "en Manaos, la casa queda lejos");
            Assert.That(map.HomeWindowShown, Is.True, "la ventanita, cuando la casa queda fuera de la vista");
            Assert.That(map.HomeWindowText, Is.EqualTo("Casa · a unos 1.480" + Nbsp + "km"));
            RectTransform tap = (RectTransform)map.HomeWindowButton!.transform;
            Assert.That(tap.rect.height, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp));
            string line = layout.RequireLast("river_map", 59, "AF_LAYOUT del Mapa");
            Assert.That(AfLayoutLog.ButtonIds(line), Does.Contain("home_window"));
            Assert.That(AfLayoutLog.ButtonIds(line), Does.Contain("stop_s05"), "Manaos, a la vista");

            // Dentro de la zona del mapa, sin pisar «Volver a la canoa» ni el rótulo del Atlántico.
            RectTransform frame = (RectTransform)map.ViewportRect.parent;
            Rect window = FrameRect(map.MapHomeWindow!.Root, frame);
            Rect pill = FrameRect(map.MapHomeWindow.PillRect, frame);
            Rect viewport = FrameRect(map.ViewportRect, frame);
            Assert.That(viewport.Contains(window.min) && viewport.Contains(window.max), Is.True, "la ventanita, dentro de la zona");
            Assert.That(pill.xMax, Is.LessThanOrEqualTo(viewport.xMax + 0.01f));
            Rect canoeRow = FrameRect((RectTransform)map.BackToCanoeButton.transform, frame);
            Assert.That(window.yMax, Is.LessThanOrEqualTo(canoeRow.yMin + 0.01f), "por encima de la fila de «Volver a la canoa»");
            Assert.That(pill.yMin, Is.GreaterThanOrEqualTo(window.yMax - 0.01f), "la píldora, debajo del retrato");
            Assert.That(pill.xMin, Is.EqualTo(window.xMin).Within(0.01f), "alineada a su izquierda");
            Assert.That(pill.width, Is.LessThanOrEqualTo(HomeWindow.PillMaxWidthDp + 0.01f));
            Assert.That(pill.yMax, Is.LessThanOrEqualTo(canoeRow.yMin + 0.01f), "y también por encima de «Volver a la canoa»");
            AssertWindowClearOfMiniCuias(map, "59");

            map.HomeWindowButton.onClick.Invoke();
            Assert.That(map.HomeInView, Is.True, "tocarla lleva la vista a Guajará");
            Assert.That(map.HomeWindowShown, Is.False);
            Assert.That(map.View, Is.EqualTo(MapView.World), "sin cambiar de vista");
            line = layout.RequireLast("river_map", 59, "AF_LAYOUT tras ir a casa");
            Assert.That(AfLayoutLog.ButtonIds(line), Does.Not.Contain("home_window"));

            map.BackToCanoe();
            map.Tick(16f);
            Assert.That(map.HomeWindowShown, Is.True, "de vuelta a la canoa, otra vez la ventanita");
        }

        [UnityTest]
        public IEnumerator HomeWindow_StaysAtLeast8dpFromEveryMiniCuia_In121_141And306()
        {
            foreach (int level in new[] { 121, 141 })
            {
                yield return OpenApp(level);
                RiverMapScreen map = CurrentMap();
                Assert.That(map.HomeWindowShown, Is.True, "nivel " + level);
                AssertWindowClearOfMiniCuias(map, level.ToString(CultureInfo.InvariantCulture));
                AssertPlatesClearOfWindow(map, level.ToString(CultureInfo.InvariantCulture));
            }

            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            Assert.That(boot.RiverMapView!.HomeWindowShown, Is.True, "306");
            AssertWindowClearOfMiniCuias(boot.RiverMapView!, "306");
            AssertPlatesClearOfWindow(boot.RiverMapView!, "306");
        }

        [UnityTest]
        public IEnumerator FixedLabelsAndBackToCanoe_LeaveEveryMiniCuia8dpClear_AndTheFramingIsCheap()
        {
            // El encuadre de apertura (12c: la mini cuia de Tefé contra «Andes ↑»).
            foreach (int level in new[] { 59, 121, 141 })
            {
                yield return OpenApp(level);
                AssertFixedOverlaysClear(CurrentMap(), level + " al abrir");
            }

            // 12h: max 140 con la vista en la mini cuia del río frontera, con «Volver a la canoa» a la vista.
            RiverMapScreen map = CurrentMap();
            Assert.That(map.CenterOnStopMarker(13), Is.True);
            AssertFixedOverlaysClear(map, "141 en el río frontera");
            Vector2 view = map.ViewportSize;
            int fine = ((2 * Mathf.FloorToInt(view.x * 0.3f / RiverMapScreen.FrameStepDp)) + 1)
                * ((2 * Mathf.FloorToInt(view.y * 0.3f / RiverMapScreen.FrameStepDp)) + 1);
            Assert.That(map.FrameEvaluations, Is.LessThanOrEqualTo(fine / 5), "sin recorrer la rejilla fina entera (" + fine + ")");

            // Lo que cuesta encuadrar (cada «Volver a la canoa» o cambio de vista), en el Editor.
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int worst = 0;
            for (int i = 0; i < 20; i++)
            {
                map.CenterOnStopMarker(13);
                worst = Mathf.Max(worst, map.FrameEvaluations);
                map.Drag(new Vector2(-100000f, -100000f));
                map.BackToCanoe();
                worst = Mathf.Max(worst, map.FrameEvaluations);
            }

            watch.Stop();
            Debug.Log("AF_PERF encuadre del Mapa en el 141: " + (watch.Elapsed.TotalMilliseconds / 40.0).ToString("0.000", CultureInfo.InvariantCulture)
                + " ms por encuadre (con su AF_LAYOUT), como mucho " + worst + " encuadres probados de " + fine);
            Assert.That(worst, Is.LessThanOrEqualTo(fine / 5));
            AssertFixedOverlaysClear(map, "141 tras «Volver a la canoa»");

            yield return OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            AssertFixedOverlaysClear(boot.RiverMapView!, "306 al abrir");
        }

        [UnityTest]
        public IEnumerator Sloth_DozesOffAfter30sWithoutTouches_AndWakesWithTheNext()
        {
            yield return OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            SlothRig rig = map.HomeSloth!;
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.Pose, Is.EqualTo(SlothPoseId.Idle));
            for (int i = 0; i < 29; i++)
            {
                map.Tick(1000f);
            }

            Assert.That(map.HomeSlothAsleep, Is.False, "a los 29 s, despierto");
            map.Tick(1000f);
            Assert.That(map.HomeSlothAsleep, Is.True, "a los 30 s sin toques, dormita");
            Assert.That(rig.Pose, Is.EqualTo(SlothPoseId.Sleep));
            Assert.That(SlothRig.IdOf(rig.Pose), Is.EqualTo("sloth_sleep"));
            Assert.That(map.MapHomeWindow!.Asleep, Is.True, "también en la ventanita, con los ojos cerrados");

            map.Drag(new Vector2(4f, 0f));
            map.Tick(16f);
            Assert.That(map.HomeSlothAsleep, Is.False, "un toque lo despierta");
            Assert.That(rig.Pose, Is.EqualTo(SlothPoseId.Wake));
            Assert.That(map.MapHomeWindow.Asleep, Is.False);
            map.Tick(RiverMapScreen.ActiveLingerMs + 1f);
            map.Tick(SlothRig.WakeMs + 1f);
            Assert.That(rig.Pose, Is.EqualTo(SlothPoseId.Idle), "y vuelve a colgar tranquilo");
            Assert.That(map.IdleMs, Is.LessThan(MapConfig.MapIdleSleepMs));
        }

        [UnityTest]
        public IEnumerator InRelease_WithoutPng_TheHouseTheWindowAndTheSimplifiedFlags()
        {
            MapArtRule.ForceProvisional = null; // la regla real: la misma en release (D-052)
            Assert.That(MapArtRule.ShowProvisionalIn(false, false), Is.True, "release");
            Assert.That(ArtSlots.Find(ProceduralArt.MapHomeSlot), Is.Null, "sin el PNG de map_home");
            yield return OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.HomeShown, Is.True, "D-052: la casa por código también en release");
            Assert.That(map.HomeWindowShown, Is.True);
            Assert.That(map.StopMarkerCount, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator WithoutCodeArt_NoHouseNoWindow_NoSimplifiedFlags()
        {
            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            yield return OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.HomeShown, Is.False, "sin arte por código ni map_home, nada");
            Assert.That(map.HomeWindowShown, Is.False);
            Assert.That(map.CanoeFlagCount, Is.EqualTo(0), "sin la bandera oficial no sale la simplificada");
            Assert.That(map.Mast!.gameObject.activeSelf, Is.False, "ni el mástil");
            Assert.That(map.StopMarkerCount, Is.EqualTo(8), "las mini cuias y las balizas no dependen de MapArtRule");
            Assert.That(map.GuideShown, Is.True);
        }

        [UnityTest]
        public IEnumerator AtRest_TheMarkersDoNotAllocate()
        {
            yield return OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.HomeWindowShown, Is.True);
            map.Tick(16f);
            map.Tick(16f);
            Assert.That(() => map.Tick(16f), Is.Not.AllocatingGCMemory(), "el Mapa quieto, con la casa, la ventanita y el guía");
        }

        // ============================================================================================ ayudas

        private static RiverMapScreen CurrentMap()
        {
            return GameBootstrap.Current!.RiverMapView!;
        }

        // Todas las mini cuias que se ven (o que tocan a una que se ve) quedan a ≥ ClusterMinDp entre centros (sus áreas táctiles
        // de 48 dp no se pisan) y ninguna pisa la canoa con su mástil y sus banderas ni el guía.
        private static void AssertMiniCuiasClear(RiverMapScreen map, string label)
        {
            Assert.That(map.StopMarkersShown, Is.True, label);
            Rect gear = map.CanoeGearBoxLocal;
            Rect guide = map.GuideBoxLocal;
            Assert.That(gear.width, Is.GreaterThan(0f), label + ": la caja de la canoa");
            Assert.That(gear.Contains(map.CanoeBoxLocal.center), Is.True, label + ": la caja con el equipo contiene la canoa que se pinta");
            float min = float.MaxValue;
            int shown = 0;
            int fanned = 0;
            int leaders = 0;
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                if (!map.StopMarkerInView(i))
                {
                    continue;
                }

                shown++;
                fanned += map.StopMarkerFanned(i) ? 1 : 0;
                leaders += map.StopMarkerHasLeader(i) ? 1 : 0;
                Rect box = CuiaBox(map.StopMarkerLocal(i));
                int n = map.StopMarkerStop(i);
                Assert.That(box.Overlaps(gear), Is.False, label + ": la mini cuia de la parada " + n + " pisa la canoa");
                Assert.That(box.Overlaps(guide), Is.False, label + ": la mini cuia de la parada " + n + " pisa el guía");
                for (int j = 0; j < map.StopMarkerCount; j++)
                {
                    if (j != i)
                    {
                        min = Mathf.Min(min, Vector2.Distance(map.StopMarkerLocal(i), map.StopMarkerLocal(j)));
                    }
                }
            }

            Debug.Log("AF_MAP mini cuias en el " + label + ": " + shown + " a la vista (" + fanned + " en abanico, " + leaders
                + " con línea), la menor distancia entre centros " + min.ToString("0.0", CultureInfo.InvariantCulture) + " dp, zoom "
                + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture));
            Assert.That(shown, Is.GreaterThanOrEqualTo(2), label);
            Assert.That(min, Is.GreaterThanOrEqualTo((float)MapConfig.ClusterMinDp), label + ": las áreas táctiles de 48 dp no se pisan");
        }

        // La ventanita de casa (marco y píldora) queda a ≥ HomeWindowCuiaGapDp de toda mini cuia que se ve.
        private static void AssertWindowClearOfMiniCuias(RiverMapScreen map, string label)
        {
            Rect win = map.HomeWindowViewRect;
            float gap = RiverMapScreen.HomeWindowCuiaGapDp - 0.01f;
            win = Rect.MinMaxRect(win.xMin - gap, win.yMin - gap, win.xMax + gap, win.yMax + gap);
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                if (!map.StopMarkerInView(i))
                {
                    continue;
                }

                Vector2 v = map.StopMarkerViewPoint(i);
                var box = new Rect(v.x - (ProceduralArt.MiniCuiaWidthDp * 0.5f), v.y - (ProceduralArt.MiniCuiaHeightDp * 0.5f),
                    ProceduralArt.MiniCuiaWidthDp, ProceduralArt.MiniCuiaHeightDp);
                Assert.That(box.Overlaps(win), Is.False, label + ": la mini cuia de la parada " + map.StopMarkerStop(i) + " a menos de 8 dp de la ventanita");
            }
        }

        // Ninguna mini cuia que se ve (aunque sea en parte) queda a menos de FixedOverlayGapDp de «Andes ↑», de «Océano Atlántico ↓»
        // ni, si sale, de «Volver a la canoa».
        private static void AssertFixedOverlaysClear(RiverMapScreen map, string label)
        {
            Assert.That(map.StopMarkersShown, Is.True, label);
            float gap = RiverMapScreen.FixedOverlayGapDp - 0.01f;
            Assert.That(map.AndesLabelViewRect.width, Is.GreaterThan(0f), label);
            Assert.That(map.AtlanticLabelViewRect.width, Is.GreaterThan(0f), label);
            Rect andes = Grow(map.AndesLabelViewRect, gap);
            Rect atlantic = Grow(map.AtlanticLabelViewRect, gap);
            Rect button = Grow(map.BackToCanoeViewRect, gap);
            Vector2 size = map.ViewportSize;
            var view = new Rect(0f, 0f, size.x, size.y);
            int seen = 0;
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                Vector2 v = map.StopMarkerViewPoint(i);
                Rect box = CuiaBox(new Vector2(v.x, v.y));
                if (!box.Overlaps(view))
                {
                    continue;
                }

                seen++;
                int n = map.StopMarkerStop(i);
                Assert.That(box.Overlaps(andes), Is.False, label + ": la mini cuia de la parada " + n + " pegada a «Andes ↑»");
                Assert.That(box.Overlaps(atlantic), Is.False, label + ": la mini cuia de la parada " + n + " pegada a «Océano Atlántico ↓»");
                if (map.BackToCanoeShown)
                {
                    Assert.That(box.Overlaps(button), Is.False, label + ": la mini cuia de la parada " + n + " pegada a «Volver a la canoa»");
                }
            }

            Debug.Log("AF_MAP rótulos y botón en el " + label + ": " + seen + " mini cuias a la vista, «Volver a la canoa» " + map.BackToCanoeShown
                + ", encuadre corrido (" + map.FrameShiftDp.x.ToString("0", CultureInfo.InvariantCulture) + ", "
                + map.FrameShiftDp.y.ToString("0", CultureInfo.InvariantCulture) + ") dp, coste " + map.FrameCost);
        }

        // Ninguna placa de mundo futuro que se ve queda a menos de HomeWindowCuiaGapDp de la ventanita de casa (marco y píldora).
        private static void AssertPlatesClearOfWindow(RiverMapScreen map, string label)
        {
            if (!map.HomeWindowShown)
            {
                return;
            }

            RectTransform frame = (RectTransform)map.ViewportRect.parent;
            Rect win = FrameRect(map.MapHomeWindow!.Root, frame);
            if (map.MapHomeWindow.PillShown)
            {
                Rect pill = FrameRect(map.MapHomeWindow.PillRect, frame);
                win = Rect.MinMaxRect(Mathf.Min(win.xMin, pill.xMin), Mathf.Min(win.yMin, pill.yMin), Mathf.Max(win.xMax, pill.xMax), Mathf.Max(win.yMax, pill.yMax));
            }

            win = Grow(win, RiverMapScreen.HomeWindowCuiaGapDp - 0.01f);
            Rect viewport = FrameRect(map.ViewportRect, frame);
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Rect plate = FrameRect(map.SilhouettePlate(i).rectTransform, frame);
                if (plate.Overlaps(viewport))
                {
                    Vector2 point = map.Space.ToLocal(map.SilhouetteMapPoint(i));
                    Vector2 inFrame = new Vector2(viewport.xMin + map.ContentOffset.x + point.x, viewport.yMin + map.ContentOffset.y - point.y);
                    float far = Mathf.Sqrt(Mathf.Pow(Mathf.Max(0f, Mathf.Max(plate.xMin - inFrame.x, inFrame.x - plate.xMax)), 2f)
                        + Mathf.Pow(Mathf.Max(0f, Mathf.Max(plate.yMin - inFrame.y, inFrame.y - plate.yMax)), 2f));
                    Debug.Log("AF_MAP placa «" + map.SilhouetteText(i) + "» en el " + label + ": " + plate + ", a "
                        + far.ToString("0", CultureInfo.InvariantCulture) + " dp de su punto " + inFrame);
                    Assert.That(plate.Overlaps(win), Is.False, label + ": la placa «" + map.SilhouetteText(i) + "» " + plate + " pegada a la ventanita de casa "
                        + win + " (encuadre corrido " + map.FrameShiftDp + ", coste " + map.FrameCost + ")");
                }
            }
        }

        private static Rect Grow(Rect r, float by)
        {
            return Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);
        }

        private static Rect CuiaBox(Vector2 c)
        {
            return new Rect(c.x - (ProceduralArt.MiniCuiaWidthDp * 0.5f), c.y - (ProceduralArt.MiniCuiaHeightDp * 0.5f),
                ProceduralArt.MiniCuiaWidthDp, ProceduralArt.MiniCuiaHeightDp);
        }

        private static void AssertFlags(RiverStopCatalog stops, int max, string?[] into, params string[] expected)
        {
            int count = RiverMapScreen.CanoeFlags(stops, max, into);
            Assert.That(count, Is.EqualTo(expected.Length), "max " + max);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(into[i], Is.EqualTo(expected[i]), "max " + max + ", bandera " + i);
            }
        }

        private static RiverStopCatalog Catalog()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json");
            return RiverStopCatalogReader.Read(System.IO.File.ReadAllText(path));
        }

        // Abre la app en el inicio con «Jugar · Nivel playLevel» (max_level y map_seen_level = playLevel − 1: sin ponerse al día),
        // toma el reloj (los Tick, a mano) y abre el Mapa desde su pestaña.
        internal static IEnumerator OpenApp(int playLevel, int seenLevel = -1)
        {
            if (!GameScreen.SimulatedScreen.HasValue)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
            }

            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = playLevel;
            profile.Progress.MaxLevel = playLevel - 1;
            profile.Progress.MapSeenLevel = seenLevel < 0 ? playLevel - 1 : seenLevel;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa se abre desde el inicio");
            Canvas.ForceUpdateCanvases();
        }

        // Vuelve a abrir el Mapa de la escena con otro progreso.
        internal static void Reopen(GameBootstrap boot, IMapProgress progress, int playLevel)
        {
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, progress, playLevel, playLevel <= 300);
            Canvas.ForceUpdateCanvases();
        }

        // Caja de rt en coordenadas del marco (dp, y hacia abajo).
        private static Rect FrameRect(RectTransform rt, RectTransform frame)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = frame.InverseTransformPoint(corners[0]);
            Vector3 b = frame.InverseTransformPoint(corners[2]);
            Rect f = frame.rect;
            return Rect.MinMaxRect(a.x - f.xMin, f.yMax - b.y, b.x - f.xMin, f.yMax - a.y);
        }
    }
}
