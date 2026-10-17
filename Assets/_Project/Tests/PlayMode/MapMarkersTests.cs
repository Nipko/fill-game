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
    /// <item>las mini cuias de las paradas, tocables a ≥ 48 dp, llenas del agua del sitio las alcanzadas y en abanico en el
    /// Trapecio; la baliza de los cierres de mundo, encendida en el 40, el 80 y el 140; sin «Premio del mundo»;</item>
    /// <item>la casa de TiTino en Guajará y, fuera de la vista, la ventanita con «Casa · a unos 1.480 km» en el mundo 3, que lleva
    /// a la casa; <c>sloth_sleep</c> a los 30 s sin toques; cero asignaciones con el Mapa quieto.</item>
    /// </list>
    /// </summary>
    public sealed class MapMarkersTests
    {
        private const string Nbsp = " ";
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true; // el arte provisional de P-M5, como en el Editor y en desarrollo
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
                RectTransform tap = (RectTransform)map.StopMarkerButton(i).transform;
                Assert.That(tap.rect.width, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp), "≥ 48 dp");
                Assert.That(tap.rect.height, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp), "≥ 48 dp");
            }

            // La baliza de Flona Tapajós (cierre del mundo 2), encendida; la de Tefé (cierre del 3), apagada.
            Assert.That(map.StopMarkerHasBeacon(3) && map.StopMarkerBeaconLit(3), Is.True, "flona_tapajos, con 58 ≥ 40");
            Assert.That(map.StopMarkerHasBeacon(7) && !map.StopMarkerBeaconLit(7), Is.True, "tefe, con 58 < 80");
            Assert.That(map.StopMarkerHasBeacon(5), Is.False, "Manaos no cierra el mundo");

            // Sin «Premio del mundo» (ni ningún otro texto nuevo en el Mapa).
            foreach (TMP_Text text in map.Canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                StringAssert.DoesNotContain("Premio", text.text, text.name);
            }

            // En la Cuenca, a 1×, las mini cuias no caben: no se pintan. La canoa, el guía y la casa, sí.
            map.SetView(MapView.Basin);
            Assert.That(map.StopMarkersShown, Is.False);
            Assert.That(map.GuideShown, Is.True);
            Assert.That(map.HomeShown, Is.True);
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
            Assert.That(map.StopMarkerBeaconLit(map.StopMarkerIndexOf(13)), Is.True, "el río frontera cierra el mundo 4 en el 140");
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
        public IEnumerator Trapecio_TheClusteredMiniCuiasOpenInAFan_TappableAt48dp()
        {
            yield return OpenApp(121); // mundo 4: las 6 paradas del Trapecio, Leticia y su atardecer en el mismo punto
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.View, Is.EqualTo(MapView.World));
            int leticia = map.StopMarkerIndexOf(8);
            int dusk = map.StopMarkerIndexOf(9);
            Assert.That(leticia >= 0 && dusk >= 0, Is.True);
            Assert.That(map.StopMarkerRealLocal(leticia), Is.EqualTo(map.StopMarkerRealLocal(dusk)), "misma vista, otra luz: el mismo punto");
            Assert.That(map.StopMarkerFanned(leticia) && map.StopMarkerFanned(dusk), Is.True, "en abanico");

            float min = float.MaxValue;
            int fanned = 0;
            for (int i = 0; i < map.StopMarkerCount; i++)
            {
                fanned += map.StopMarkerFanned(i) ? 1 : 0;
                for (int j = i + 1; j < map.StopMarkerCount; j++)
                {
                    if (map.StopMarkerFanned(i) && map.StopMarkerFanned(j))
                    {
                        min = Mathf.Min(min, Vector2.Distance(map.StopMarkerLocal(i), map.StopMarkerLocal(j)));
                    }
                }
            }

            Debug.Log("AF_MAP abanico del Trapecio: " + fanned + " mini cuias, la menor distancia entre dos, "
                + min.ToString("0.0", CultureInfo.InvariantCulture) + " dp, zoom " + map.Zoom.ToString("0.00", CultureInfo.InvariantCulture));
            Assert.That(fanned, Is.GreaterThanOrEqualTo(2));
            Assert.That(min, Is.GreaterThanOrEqualTo(MapConfig.ClusterMinDp - 0.5f), "en el abanico, las áreas táctiles no se pisan");

            // Tocar una mini cuia cuenta para map_close (stops_tapped) y la deja para la tarjeta de la parada (M4.1).
            map.StopMarkerButton(dusk).onClick.Invoke();
            Assert.That(map.LastTappedStop, Is.EqualTo(9));
            MapCloseStats stats = map.Close();
            Assert.That(stats.StopsTapped, Is.EqualTo(1));
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
        public IEnumerator Release_WithoutFinalArt_NoHouseNoWindow_NoSimplifiedFlags()
        {
            MapArtRule.ForceProvisional = false;
            yield return OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.HomeShown, Is.False, "P-M5: sin map_home, nada");
            Assert.That(map.HomeWindowShown, Is.False);
            Assert.That(map.CanoeFlagCount, Is.EqualTo(0), "sin la bandera oficial no sale la simplificada");
            Assert.That(map.Mast!.gameObject.activeSelf, Is.False, "ni el mástil");
            Assert.That(map.StopMarkerCount, Is.EqualTo(8), "las mini cuias y las balizas no son arte de P-M5");
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
