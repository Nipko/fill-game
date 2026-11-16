#nullable enable
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los arreglos de la segunda ronda de la 1.1 en el teléfono (<c>docs/dev/traspaso/pruebas-telefono-2026-11-09-1.1-r2.md</c>),
    /// en la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>observación 1: con un zoom del mundo alto (el viaje del 80, ×15), el viaje se hace con menos zoom
    /// (<see cref="RiverMapScreen.JourneyWideZoom"/>) y, al llegar, la cámara se acerca un par de segundos y vuelve; el viaje sigue
    /// durando 4,5 s, «Jugar» entra al llegar (no espera al acercamiento), un toque lo acelera ×6 y el del 70 (×4) no cambia;</item>
    /// <item>observación 2: las boyas de la ruta, más discretas y sin dibujarse sobre la canoa con el tucán ni sobre el guía, en
    /// las vistas Cuenca y Mundo, y la ruta se sigue leyendo.</item>
    /// </list>
    /// </summary>
    public sealed class MapRound2Tests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator TheJourneyOf80_GoesWithLessZoom_ThenACloseUpOnArrival_SameTime_AndThe70IsAsBefore()
        {
            yield return MapMarkersTests.OpenApp(81);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;

            // El del 70 (×4, «sí está bien»): sin cambios visibles.
            float speed70 = RunJourney(boot, map, 71, 70, 60, out float ready70, out float top70);
            float zoom70 = map.Zoom;
            Assert.That(map.JourneyWideFromZoom, Is.EqualTo(0f), "el 70 va con el zoom del mundo");
            Assert.That(map.Zoom, Is.LessThanOrEqualTo(RiverMapScreen.JourneyWideAboveZoom));
            Assert.That(top70, Is.LessThan(1.2f), "sin acercamiento de llegada en el 70 (solo el de §5.5, ×1,15)");
            Assert.That(ready70, Is.EqualTo(4500f + RiverMapScreen.JourneyLeadMs).Within(2f * PlayModeHelpers.FrameMs));
            map.Close();

            // El del 80 (cruza al mundo 4, ×15): con menos zoom, 4,5 s y «Jugar» al llegar.
            float speed80 = RunJourney(boot, map, 81, 80, 70, out float ready80, out float top80);
            float world = map.JourneyWideFromZoom;
            Debug.Log("AF_MAP r2 viaje: 70 a ×" + Fmt(zoom70) + " " + Fmt(speed70) + " dp/s; 80 del ×" + Fmt(world) + " al ×"
                + Fmt(map.Zoom) + ": " + Fmt(speed80) + " dp/s (con el zoom del mundo serían " + Fmt(speed80 * world / map.Zoom)
                + "); «Jugar» a los " + Fmt(ready70) + " y " + Fmt(ready80) + " ms; acercamiento de llegada ×" + Fmt(top80));
            Assert.That(world, Is.GreaterThan(RiverMapScreen.JourneyWideAboveZoom), "el mundo 4 va a ×15");
            Assert.That(map.Zoom, Is.LessThanOrEqualTo(RiverMapScreen.JourneyWideZoom + 1e-3f), "el viaje, con menos zoom");
            Assert.That(map.CatchUpDurationMs, Is.EqualTo(4500), "el mismo tiempo");
            Assert.That(map.CatchUpPerBuoyMs, Is.EqualTo(MapConfig.JourneyPerBuoyMs).Within(1e-3f));
            Assert.That(ready80, Is.EqualTo(4500f + RiverMapScreen.JourneyLeadMs).Within(2f * PlayModeHelpers.FrameMs),
                "map_play_ready: «Jugar» entra al llegar, no espera al acercamiento");
            Assert.That(speed80 * world / map.Zoom, Is.GreaterThan(speed80 * 2f), "la canoa cruza la pantalla más despacio que con ×15");
            Assert.That(speed80, Is.LessThan(speed70 * 2f), "y a menos del doble que en el viaje del 70, que estaba bien (antes, a 4,3 veces)");
            float expected = Mathf.Min(world / map.Zoom, RiverMapScreen.CamCloseUpZoom);
            Assert.That(top80, Is.GreaterThan(1f + ((expected - 1f) * 0.9f)), "al llegar, el acercamiento hasta el detalle del zoom del mundo");

            // Vuelve a la vista del viaje a los ~2 s más la vuelta suave.
            for (int ms = 0; ms < 6000 && (map.ArrivalCloseUpActive || map.CameraCloseUp > 1f); ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.ArrivalCloseUpActive, Is.False);
            Assert.That(map.CameraZoom, Is.EqualTo(1f), "vuelve a la vista principal");
            Assert.That(map.CloseUpLabelAlpha, Is.EqualTo(1f), "y los rótulos vuelven");

            // Cuenca y vuelta a Mundo: la vista Mundo de siempre (×15).
            map.SetView(MapView.Basin);
            map.SetView(MapView.World);
            Assert.That(map.JourneyWideFromZoom, Is.EqualTo(0f));
            Assert.That(map.Zoom, Is.GreaterThan(RiverMapScreen.JourneyWideAboveZoom), "al cambiar de vista, el zoom del mundo");
            map.Close();

            // Con un toque al abrir: «Jugar» a los 0,75 s, como siempre; el acercamiento llega igual, sin retrasarlo.
            Open(boot, map, 81, 80, 70);
            Assert.That(map.JourneyLocked, Is.True);
            map.JourneyTouchArea!.onClick.Invoke();
            for (int ms = 0; ms < 3000 && map.JourneyLocked; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.JourneyReadyMs, Is.LessThanOrEqualTo(750f + (2f * PlayModeHelpers.FrameMs)), "el toque ×6 sigue igual");
            Assert.That(map.ArrivalCloseUpActive, Is.True, "el acercamiento, tras llegar");
            Assert.That(map.PlayShown, Is.True);
            map.Close();

            // Reducir movimiento: sin viaje ni cámara; la vista Mundo de siempre.
            map.ProgressReduceMotion = true;
            Open(boot, map, 81, 80, 70);
            Assert.That(map.JourneyWideFromZoom, Is.EqualTo(0f));
            Assert.That(map.Zoom, Is.GreaterThan(RiverMapScreen.JourneyWideAboveZoom));
            for (int ms = 0; ms < 3000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                Assert.That(map.CameraZoom, Is.EqualTo(1f), "Reducir movimiento: sin animación de cámara");
            }

            map.ProgressReduceMotion = false;
            map.Close();
        }

        [UnityTest]
        public IEnumerator TheBuoys_AreSofter_AndNeverDrawnOverTheCanoeWithTheToucan_InBothViews()
        {
            yield return MapMarkersTests.OpenApp(22); // max 21, el tucán ya a bordo y visto
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach ((int play, int max) in new[] { (22, 21), (24, 23), (27, 26), (59, 58), (75, 74) })
            {
                MapMarkersTests.Reopen(boot, new MemoryProgressStore(play, max, max), play);
                foreach (MapView view in new[] { MapView.Basin, MapView.World })
                {
                    map.SetView(view);
                    for (int ms = 0; ms < 3000 && map.CameraCloseUp > 1f; ms += PlayModeHelpers.FrameMs)
                    {
                        map.Tick(PlayModeHelpers.FrameMs);
                    }

                    Canvas.ForceUpdateCanvases();
                    string at = "jugando el " + play + " en " + view;
                    Assert.That(map.LiveActive, Is.True);
                    RiverMapScreen.BuoyGraphic ahead = map.BuoysAhead!;
                    RiverMapScreen.BuoyGraphic lit = map.Buoys!;
                    Assert.That(ahead.DrawnRadius, Is.EqualTo(ahead.Radius * RiverMapScreen.BuoyLiveDrawScale).Within(1e-4f), at + ": más pequeñas");
                    Rect canoe = StopCardTests.CanvasRect(map.Canoe.rectTransform, map.Canvas);
                    Rect? toucan = map.ToucanAboardShown ? StopCardTests.CanvasRect(map.ToucanRect!, map.Canvas) : (Rect?)null;
                    int drawn = 0;
                    for (int i = 0; i < map.BuoyCount; i++)
                    {
                        RiverMapScreen.BuoyState s = map.BuoyStateAt(i);
                        if (s == RiverMapScreen.BuoyState.Skipped)
                        {
                            continue;
                        }

                        RiverMapScreen.BuoyGraphic g = s == RiverMapScreen.BuoyState.Unlit ? ahead : lit;
                        if (g.IsHidden(i))
                        {
                            continue;
                        }

                        drawn++;
                        Vector3 w = g.rectTransform.TransformPoint(map.BuoyLocalPoint(i));
                        Vector2 c = ((RectTransform)map.Canvas.transform).InverseTransformPoint(w);
                        float r = g.DrawnRadius * g.rectTransform.lossyScale.x / map.Canvas.transform.lossyScale.x;
                        Assert.That(Hits(canoe, c, r), Is.False, at + ": la boya del " + map.BuoyLevel(i) + " sobre la canoa");
                        if (toucan.HasValue)
                        {
                            Assert.That(Hits(toucan.Value, c, r), Is.False, at + ": la boya del " + map.BuoyLevel(i) + " sobre el tucán");
                        }
                    }

                    Debug.Log("AF_MAP r2 boyas " + at + ": " + drawn + " dibujadas, " + (ahead.HiddenCount + lit.HiddenCount) + " en el claro");
                    // La ruta se sigue leyendo: la fila corta, entera; en el Mundo, al menos 3 boyas fuera del claro de la canoa (la de 60 dp
                    // tapa ya las de su sitio). En la Cuenca, un tramo corto (el 71-80, de unos dp) cae casi entero bajo la canoa y Rosa: allí
                    // la cuenta la da «Faltan N», y lo que se pide es que ninguna boya se pinte sobre ellas (arriba).
                    if (map.BuoysCompact)
                    {
                        Assert.That(drawn, Is.EqualTo(10), at + ": la fila corta, entera");
                    }
                    else if (view == MapView.World)
                    {
                        Assert.That(drawn, Is.GreaterThanOrEqualTo(3), at + ": la ruta se sigue leyendo");
                    }
                }
            }

            // Con el Mapa de la 1.0 (ui_map_v2 apagada), las boyas de siempre.
            map.Close();
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.LiveActive, Is.False);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.BuoysAhead!.DrawnRadius, Is.EqualTo(map.BuoysAhead.Radius));
            Assert.That(map.BuoysAhead.HiddenCount + map.Buoys!.HiddenCount, Is.EqualTo(0));
            map.Close();
        }

        // ================================================================================================ ayudas

        internal static void Open(GameBootstrap boot, RiverMapScreen map, int play, int max, int seen)
        {
            map.Close();
            RiverMapScreen.ResetCapiSession();
            map.Open(MapOpenSource.Victory, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(play, max, seen), play, true);
            Canvas.ForceUpdateCanvases();
        }

        // Abre «Ver mapa» con el viaje de seen a max y lo deja llegar sin tocar; devuelve la velocidad de la canoa sobre el mapa
        // (dp de la vista por segundo de viaje), cuándo entró «Jugar» y el mayor primer plano tras llegar.
        private static float RunJourney(GameBootstrap boot, RiverMapScreen map, int play, int max, int seen, out float readyMs, out float topCloseUp)
        {
            Open(boot, map, play, max, seen);
            Assert.That(map.JourneyLocked, Is.True, "con viaje");
            Vector2 last = map.Canoe.rectTransform.anchoredPosition;
            float path = 0f;
            for (int ms = 0; ms < 8000 && map.JourneyLocked; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                Vector2 p = map.Canoe.rectTransform.anchoredPosition;
                path += Vector2.Distance(p, last);
                last = p;
            }

            Assert.That(map.JourneyLocked, Is.False);
            Assert.That(map.JourneyEndedBy, Is.EqualTo("time"));
            readyMs = map.JourneyReadyMs;
            topCloseUp = map.CameraCloseUp;
            for (int ms = 0; ms < 2500; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                topCloseUp = Mathf.Max(topCloseUp, map.CameraCloseUp);
            }

            return path / (map.CatchUpDurationMs / 1000f);
        }

        private static bool Hits(Rect box, Vector2 p, float r)
        {
            float dx = Mathf.Max(0f, Mathf.Max(box.xMin - p.x, p.x - box.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(box.yMin - p.y, p.y - box.yMax));
            return (dx * dx) + (dy * dy) < r * r * 0.98f;
        }

        private static string Fmt(float v)
        {
            return v.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Capturas de los arreglos de la segunda ronda de la 1.1 a 360 dp, en <c>Logs/snapshots/es-419/r2_*.png</c>: el viaje del 80
    /// con menos zoom y su acercamiento de llegada, el tucán con las boyas en la Cuenca y en el Mundo, y las confluencias del río
    /// de vidrio de cerca (×2,5 alrededor de la canoa). Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas de la segunda ronda de la 1.1; necesitan dispositivo gráfico.")]
    public sealed class MapRound2SnapshotTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 9, 12, 0, 0, System.DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
            RiverMapScreen.MouthBlendOffForCapture = false;
        }

        [UnityTest]
        public IEnumerator CaptureTheJourneyOf80_TheToucanBuoys_AndTheConfluences_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;

            // El tucán con las boyas (jugando el 22), en la Cuenca y en el Mundo.
            yield return Load(22, 21);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            SnapshotKit.AttachOverlay(map.Canvas);
            foreach (MapView view in new[] { MapView.Basin, MapView.World })
            {
                map.SetView(view);
                yield return SnapshotKit.Wait(1.0f);
                map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "r2_tucan_boyas_" + (view == MapView.Basin ? "cuenca" : "mundo") + "_360dp");
                map.ScaleForCapture(RiverMapScreen.CamCloseUpZoom);
                SnapshotKit.Save(dir, "r2_tucan_boyas_" + (view == MapView.Basin ? "cuenca" : "mundo") + "_cerca_360dp");
            }

            boot.CloseRiverMap();

            // El viaje del 80: a mitad, al llegar con el acercamiento y de vuelta a la vista del viaje.
            yield return Load(81, 80);
            boot = GameBootstrap.Current!;
            map = boot.RiverMapView!;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True); // carga los datos del Mapa
            SnapshotKit.AttachOverlay(map.Canvas);
            MapRound2Tests.Open(boot, map, 81, 80, 70);
            for (int i = 0; i < 600 && map.CatchUpElapsedMs < 2250f; i++)
            {
                yield return null;
            }

            SnapshotKit.Save(dir, "r2_viaje_080_mitad_360dp");
            for (int i = 0; i < 900 && !(map.ArrivalCloseUpActive && map.CameraCloseUp > RiverMapScreen.CamCloseUpZoom * 0.95f); i++)
            {
                yield return null;
            }

            SnapshotKit.Save(dir, "r2_viaje_080_llegada_cerca_360dp");
            for (int i = 0; i < 900 && (map.ArrivalCloseUpActive || map.CameraCloseUp > 1f); i++)
            {
                yield return null;
            }

            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "r2_viaje_080_vuelta_360dp");
            Debug.Log("AF_MAP r2 viaje 80: zoom ×" + map.Zoom + " (del mundo ×" + map.JourneyWideFromZoom + ")");
            map.Close();

            // Las confluencias, con la canoa en ellas: Santarém (Tapajós), el Encuentro y Manaos (Negro), Nauta (Ucayali y Marañón),
            // la boca del Coca (Napo) y la boca del Manu (Madre de Dios).
            foreach ((int play, string name, bool before) in new[]
            {
                (11, "santarem", true), (11, "santarem", false), (41, "encuentro", true), (41, "encuentro", false), (51, "manaos", true),
                (51, "manaos", false), (171, "nauta", true), (171, "nauta", false), (271, "boca_coca", true), (271, "boca_coca", false),
                (381, "boca_manu", true), (381, "boca_manu", false),
            })
            {
                RiverMapScreen.MouthBlendOffForCapture = before;
                string file = (before ? "antes_" : string.Empty) + name;
                yield return Load(play, play - 1);
                boot = GameBootstrap.Current!;
                map = boot.RiverMapView!;
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
                SnapshotKit.AttachOverlay(map.Canvas);
                yield return SnapshotKit.Wait(1.0f);
                map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
                yield return SnapshotKit.Wait(0.2f);
                SnapshotKit.Save(dir, "r2_confluencia_" + file + "_360dp");
                map.ScaleForCapture(RiverMapScreen.CamCloseUpZoom);
                SnapshotKit.Save(dir, "r2_confluencia_" + file + "_cerca_360dp");
                boot.CloseRiverMap();
            }
        }

        private static IEnumerator Load(int play, int max)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = play;
            profile.Progress.MaxLevel = max;
            profile.Progress.MapSeenLevel = max;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            SnapshotKit.AttachLobby(GameBootstrap.Current!);
            yield return null;
        }
    }
}
