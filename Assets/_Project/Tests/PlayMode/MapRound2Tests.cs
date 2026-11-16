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
using UnityEngine.UI;
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
                    // La guía (Rosa o el bufeo) y, a bordo, el tucuxi: tampoco quedan tapados (los compacta la fila corta, que va
                    // bajo la canoa y no se toca).
                    Image? guideImage = map.GuideImage;
                    Rect? guide = guideImage != null && guideImage.gameObject.activeInHierarchy ? StopCardTests.CanvasRect(guideImage.rectTransform, map.Canvas) : (Rect?)null;
                    Rect? tucuxi = map.TucuxiAboardShown && view == MapView.World ? StopCardTests.CanvasRect(map.TucuxiImage!.rectTransform, map.Canvas) : (Rect?)null;
                    float clear = RiverMapScreen.BuoyCrewClearDp + 1f;
                    int drawn = 0;
                    for (int i = 0; i < map.BuoyCount; i++)
                    {
                        RiverMapScreen.BuoyState s = map.BuoyStateAt(i);
                        if (s == RiverMapScreen.BuoyState.Skipped)
                        {
                            continue;
                        }

                        RiverMapScreen.BuoyGraphic g = s == RiverMapScreen.BuoyState.Unlit ? ahead : lit;
                        Vector3 w = g.rectTransform.TransformPoint(map.BuoyLocalPoint(i));
                        var root = (RectTransform)map.Canvas.transform;
                        Vector2 local = root.InverseTransformPoint(w);
                        var c = new Vector2(local.x - root.rect.xMin, root.rect.yMax - local.y); // como StopCardTests.CanvasRect
                        float r = g.DrawnRadius * g.rectTransform.lossyScale.x / map.Canvas.transform.lossyScale.x;
                        if (g.IsHidden(i))
                        {
                            // Contra ocultar de más: solo se oculta la que cae junto a la canoa o la guía.
                            bool near = Hits(Grow(canoe, clear), c, r) || (!map.BuoysCompact && ((guide.HasValue && Hits(Grow(guide.Value, clear), c, r))
                                || (tucuxi.HasValue && Hits(Grow(tucuxi.Value, clear), c, r))));
                            Assert.That(near, Is.True, at + ": la boya del " + map.BuoyLevel(i) + " se oculta lejos de la canoa y de la guía: " + c + " r " + r + ", canoa " + canoe + ", guía " + guide + ", tucuxi " + tucuxi);
                            continue;
                        }

                        drawn++;
                        if (!map.BuoysCompact)
                        {
                            if (guide.HasValue)
                            {
                                Assert.That(Hits(guide.Value, c, r), Is.False, at + ": la boya del " + map.BuoyLevel(i) + " sobre la guía");
                            }

                            if (tucuxi.HasValue)
                            {
                                Assert.That(Hits(tucuxi.Value, c, r), Is.False, at + ": la boya del " + map.BuoyLevel(i) + " sobre el tucuxi");
                            }
                        }

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

        [UnityTest]
        public IEnumerator TheMouths_BlendIntoTheirHost_InBothViews_AndNotWithTheMapOf10()
        {
            // Observación 3: el vidrio, la orilla y la várzea de cada afluente o brazo se funden en su boca, medio ancho del anfitrión.
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            Assert.That(map.LiveActive, Is.True);
            int mouths = 0;
            foreach (MapView view in new[] { MapView.World, MapView.Basin })
            {
                map.SetView(view);
                for (int i = 0; i < data.Lines.Count; i++)
                {
                    RiverLine line = data.Lines[i];
                    float expected = 0f;
                    if (line.Mouth.HasValue && line.Mouth.Value.Line != i && line.Mouth.Value.Line >= 0 && line.Mouth.Value.Line < data.Lines.Count)
                    {
                        expected = RiverMapScreen.MouthBlendHostWidths * map.MapRibbonWidthDp(view, data.Lines[line.Mouth.Value.Line].Class);
                        mouths++;
                    }

                    string at = line.Id + " en " + view;
                    Assert.That(map.MouthBlendDp(i), Is.EqualTo(expected).Within(1e-4f), at + ": el vidrio");
                    Assert.That(map.Glass(i).EndFadeDp, Is.EqualTo(expected).Within(1e-4f), at + ": el vidrio");
                    Assert.That(map.VarzeaRibbons[i].EndFadeDp, Is.EqualTo(expected).Within(1e-4f), at + ": la várzea");
                    Assert.That(map.BankRibbons[i].EndFadeDp, Is.EqualTo(expected).Within(1e-4f), at + ": la orilla");
                }
            }

            Assert.That(mouths, Is.GreaterThan(10), "hay afluentes y brazos con boca");

            // Con el Mapa de la 1.0, sin fundido, también tras una apertura con el v2 (el reinicio de LiveOpen).
            map.Close();
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.LiveActive, Is.False);
            for (int i = 0; i < data.Lines.Count; i++)
            {
                Assert.That(map.MouthBlendDp(i), Is.EqualTo(0f), data.Lines[i].Id + ": la 1.0, sin fundido");
            }

            map.Close();
        }

        [UnityTest]
        public IEnumerator AfterTheJourneyOf80_TheLabelsAndPlatesFitTheScreen()
        {
            // Lo que vio el integrador en las capturas: tras el viaje del 80 a ×6, el rótulo junto de Leticia quedaba cortado.
            yield return MapMarkersTests.OpenApp(81);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Open(boot, map, 81, 80, 70);
            for (int ms = 0; ms < 15000 && (map.JourneyLocked || map.ArrivalCloseUpActive || map.CameraCloseUp > 1f || map.CameraSettling);
                ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.CameraSettling, Is.False);
            Assert.That(map.CameraZoom, Is.EqualTo(1f));
            Assert.That(map.JourneyWideFromZoom, Is.GreaterThan(0f), "el viaje con menos zoom");
            Canvas.ForceUpdateCanvases();
            Rect view = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
            int next = map.RemainingStopN();
            Assert.That(next, Is.EqualTo(9), "la parada siguiente, Leticia al atardecer");
            Assert.That(map.StopRemainingShown, Is.False, "sin la placa de respaldo");
            Assert.That(map.StopRemainingInLabel, Is.True, "«Faltan N» en el rótulo junto de Leticia");
            TMPro.TextMeshProUGUI label = map.StopLabelOf(next)!;
            Assert.That(label.gameObject.activeInHierarchy, Is.True);
            Rect r = StopCardTests.CanvasRect(label.rectTransform, map.Canvas);
            Assert.That(Inside(r, view), Is.True, "el rótulo de la parada siguiente, entero a la vista: " + r + " en " + view);
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Image plate = map.SilhouettePlate(i);
                if (!plate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect p = StopCardTests.CanvasRect(plate.rectTransform, map.Canvas);
                Assert.That(!p.Overlaps(view) || Inside(p, view), Is.True, "la placa " + i + ", entera o fuera: " + p + " en " + view);
            }

            Debug.Log("AF_MAP r2 tras el viaje del 80: «" + label.text.Replace("\n", " / ") + "» en " + r + ", vista " + view);
            map.Close();
        }

        [UnityTest]
        public IEnumerator TheRibbons_DoNotFoldInTheTightBends_OfNautaAndElsewhere()
        {
            // Las cuñas oscuras en las curvas cerradas (Nauta): triángulos del revés (la malla se pliega y se pinta dos veces). Se
            // miden el tramo en curso (agua y orla), el lleno y el vidrio de todas las líneas, en las dos vistas y con la 1.0.
            yield return MapMarkersTests.OpenApp(171);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            foreach (bool v2 in new[] { true, false })
            {
                map.LiveRequested = v2; // Reopen abre el Mapa directamente: la clave, aquí
                foreach (int play in new[] { 171, 51, 271, 381 })
                {
                    MapMarkersTests.Reopen(boot, new MemoryProgressStore(play, play - 1, play - 1), play);
                    Assert.That(map.LiveActive, Is.EqualTo(v2));
                    foreach (MapView view in new[] { MapView.World, MapView.Basin })
                    {
                        map.SetView(view);
                        Canvas.ForceUpdateCanvases();
                        int frontBefore = 0;
                        int front = 0;
                        int rest = 0;
                        int total = 0;
                        for (int k = 0; k < map.FrontPieceCount; k++)
                        {
                            foreach (AF.Presentation.RiverRibbonGraphic ribbon in new[] { map.FrontRibbon(k), map.FrontCasingRibbon(k) })
                            {
                                AF.Presentation.RiverRibbonGraphic.FoldGuardOff = true;
                                try
                                {
                                    frontBefore += ribbon.CountFoldedTriangles(out _);
                                }
                                finally
                                {
                                    AF.Presentation.RiverRibbonGraphic.FoldGuardOff = false;
                                }

                                front += Folds(ribbon, ref total);
                            }
                        }

                        for (int i = 0; i < data.Lines.Count; i++)
                        {
                            rest += Folds(map.Glass(i), ref total);
                            if (map.FillRibbon(i).gameObject.activeSelf)
                            {
                                rest += Folds(map.FillRibbon(i), ref total);
                            }
                        }

                        string at = (v2 ? "v2" : "1.0") + ", jugando el " + play + " en " + view;
                        Debug.Log("AF_MAP r2 pliegues " + at + ": tramo " + frontBefore + " sin la guarda y " + front
                            + " con ella; vidrio y lleno, " + rest + "; de " + total);

                        // Antes, 77 en las dos curvas de Nauta en Mundo con el v2 (25 con la 1.0). Con la guarda quedan astillas de unos
                        // 3 dp² (1 con el v2 y 2 con la 1.0, en la segunda curva): se miden para que no vuelvan a crecer.
                        Assert.That(front, Is.LessThanOrEqualTo(2), at + ": el tramo, como mucho dos astillas");
                        Assert.That(rest, Is.LessThanOrEqualTo(RestFoldsAllowed), at + ": el vidrio y el lleno");
                        if (v2 && play == 171 && view == MapView.World)
                        {
                            Assert.That(frontBefore, Is.GreaterThan(10), "sin la guarda, las curvas de Nauta se pliegan (el test lo ve)");
                        }
                    }
                }
            }

            map.LiveRequested = true;
            map.Close();
        }

        // Pliegues que se admiten en el vidrio y el lleno de todas las líneas juntas (una apertura y una vista) [medido: 3 en la
        // Cuenca jugando el 171, 271 y 381, y en el Mundo jugando el 381; astillas de 1 a 3 dp²].
        private const int RestFoldsAllowed = 3;

        private static int Folds(AF.Presentation.RiverRibbonGraphic ribbon, ref int total)
        {
            int f = ribbon.CountFoldedTriangles(out int n, out string where);
            total += n;
            if (f > 0)
            {
                Debug.Log("AF_MAP r2 pliegue en " + ribbon.name + ": " + where);
            }

            return f;
        }

        [UnityTest]
        public IEnumerator TheFinalFraming_IsCancelled_ByTheBasinMidway()
        {
            // Revisión: si el jugador pasa a la Cuenca mientras la vista se corre al encuadre final del viaje del 80, el encuadre se
            // anula y el desplazamiento es el de la Cuenca.
            yield return MapMarkersTests.OpenApp(81);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Open(boot, map, 81, 80, 70);
            for (int ms = 0; ms < 10000 && !map.CameraSettling; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.CameraSettling, Is.True, "tras el viaje, la vista se corre al encuadre final");
            map.SetView(MapView.Basin);
            Assert.That(map.CameraSettling, Is.False, "la Cuenca lo anula");
            Vector2 basin = map.ContentOffset;
            for (int i = 0; i < 30; i++)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.CameraSettling, Is.False);
            Assert.That(Vector2.Distance(map.ContentOffset, basin), Is.LessThan(0.01f), "el desplazamiento, el de la Cuenca");

            // «Volver a la canoa» durante el encuadre también lo anula y manda su propio encuadre.
            Open(boot, map, 81, 80, 70);
            for (int ms = 0; ms < 10000 && !map.CameraSettling; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.CameraSettling, Is.True);
            map.BackToCanoe();
            Assert.That(map.CameraSettling, Is.False, "«Volver a la canoa» lo anula");
            Vector2 back = map.ContentOffset;
            for (int i = 0; i < 30; i++)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(Vector2.Distance(map.ContentOffset, back), Is.LessThan(0.01f), "se queda en el encuadre de «Volver a la canoa»");
            map.Close();
        }

        [UnityTest]
        public IEnumerator TheJointsOfTheLegInProgress_AndTheFillUnderIt_BlendAtTheScaleOfEachView()
        {
            // Jugando el 300: el tramo del Napo, el Amazonas, el Madeira, el Beni y el Madre de Dios, en varias piezas.
            yield return MapMarkersTests.OpenApp(300);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            Assert.That(map.LiveActive, Is.True);
            Assert.That(map.FrontPieceCount, Is.GreaterThanOrEqualTo(3), "un tramo de varias piezas");
            foreach (MapView view in new[] { MapView.World, MapView.Basin })
            {
                map.SetView(view);
                int last = map.FrontPieceCount - 1;
                for (int k = 0; k <= last; k++)
                {
                    float expected = RiverMapScreen.FrontLiveFadeDp;
                    if (k > 0 && k < last)
                    {
                        float w = 0f;
                        for (int j = k - 1; j <= k + 1; j++)
                        {
                            w = Mathf.Max(w, map.MapRibbonWidthDp(view, map.FrontRibbon(j).Line!.Class));
                        }

                        expected = 0.5f * w;
                    }

                    string at = "pieza " + k + " (" + map.FrontRibbon(k).Line!.Id + ") en " + view;
                    Assert.That(map.FrontRibbon(k).EndFadeDp, Is.EqualTo(expected).Within(1e-4f), at);
                    Assert.That(map.FrontCasingRibbon(k).EndFadeDp, Is.EqualTo(expected).Within(1e-4f), at + ": la orla");
                }
            }

            // El fundido cruzado del lleno: jugando el 59, el tramo sale de Manaos río arriba por el Negro, donde acaba su lleno.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(59, 58, 58), 59);
            int negro = map.FrontRibbon(0).Line!.Index;
            AF.Presentation.RiverRibbonGraphic fill = map.FillRibbon(negro);
            float world = 0f;
            foreach (MapView view in new[] { MapView.World, MapView.Basin })
            {
                map.SetView(view);
                float cross = RiverMapScreen.FrontLiveFadeDp / (map.Space.DpPerUnit * data.Lines[negro].Length);
                Assert.That(fill.FillStart, Is.EqualTo(Mathf.Max(0f, map.FillFrom(negro) - cross)).Within(1e-5f), view + ": el lleno, alargado");
                Assert.That(fill.FillStart, Is.LessThan(map.FillFrom(negro)), view.ToString());
                if (view == MapView.World)
                {
                    world = fill.FillStart;
                }
                else
                {
                    Assert.That(fill.FillStart, Is.Not.EqualTo(world).Within(1e-6f), "cambia con la escala de la vista");
                }
            }

            // Con la 1.0: sin fundidos y el lleno donde acaba.
            map.Close();
            map.LiveRequested = false; // Reopen abre el Mapa directamente: la clave, aquí
            foreach (int play in new[] { 300, 59 })
            {
                MapMarkersTests.Reopen(boot, new MemoryProgressStore(play, play - 1, play - 1), play);
                Assert.That(map.LiveActive, Is.False);
                foreach (MapView view in new[] { MapView.World, MapView.Basin })
                {
                    map.SetView(view);
                    for (int k = 0; k < map.FrontPieceCount; k++)
                    {
                        Assert.That(map.FrontRibbon(k).EndFadeDp, Is.EqualTo(0f), "1.0, pieza " + k);
                    }

                    for (int i = 0; i < data.Lines.Count; i++)
                    {
                        if (map.FillRibbon(i).gameObject.activeSelf)
                        {
                            Assert.That(map.FillRibbon(i).FillStart, Is.EqualTo(map.FillFrom(i)).Within(1e-6f), "1.0, " + data.Lines[i].Id);
                        }
                    }
                }
            }

            map.LiveRequested = true;
            map.Close();
        }

        private static bool Inside(Rect r, Rect view)
        {
            return r.xMin >= view.xMin - 0.5f && r.xMax <= view.xMax + 0.5f && r.yMin >= view.yMin - 0.5f && r.yMax <= view.yMax + 0.5f;
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

        private static Rect Grow(Rect r, float by)
        {
            return new Rect(r.xMin - by, r.yMin - by, r.width + (2f * by), r.height + (2f * by));
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
            AF.Presentation.RiverRibbonGraphic.FoldGuardOff = false;
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
                AF.Presentation.RiverRibbonGraphic.FoldGuardOff = before;
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
