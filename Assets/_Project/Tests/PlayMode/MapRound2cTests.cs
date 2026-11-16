#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los arreglos del Mapa de la prueba r2c de la 1.1 (<c>docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2c.md</c>), en la
    /// escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>observación 1: ningún rótulo de parada o de pueblo, ni placa ni la píldora del Premio, queda debajo de la ventanita de
    /// casa en la vista Mundo («Trinidad … iveles» en el 521);</item>
    /// <item>observación 2: el primer toque en «Mundo» justo al acabar el viaje del Mapa del 10 cambia de vista (antes lo recibía el
    /// área que salta lo que queda de la secuencia) y, mientras el viaje no deja tocarlo, el selector no parece habilitado;</item>
    /// <item>la placa del mundo siguiente, entera en la pantalla en el Mapa del 10 del 71 y el 79 («…ndo 4 · Trapecio · Nivel 81»).</item>
    /// </list>
    /// </summary>
    public sealed class MapRound2cTests
    {
        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
            GameBootstrap.L10SequenceOverride = true; // los helpers de PlayMode la apagan si el test no la pide (PlayModeHelpers)
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameBootstrap.L10SequenceOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator InTheWorldView_NoLabelOrPlate_IsUnderTheHomeWindow()
        {
            yield return MapMarkersTests.OpenApp(522);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            int withWindow = 0;
            int moved = 0;
            foreach (int max in new[] { 521, 71, 79, 132, 291, 300, 445, 451, 481, 511 })
            {
                Reopen(boot, map, max);
                map.SetView(MapView.World);
                map.Tick(PlayModeHelpers.FrameMs);
                Canvas.ForceUpdateCanvases();
                HomeWindow? home = map.MapHomeWindow;
                if (home == null || !home.Visible)
                {
                    Debug.Log("AF_MAP r2c ventanita " + max + ": no sale");
                    continue;
                }

                withWindow++;
                moved += map.StopLabelsMovedForHomeWindow;
                Rect win = map.HomeWindowViewRect;
                int n = map.RemainingStopN();
                Rect next = map.StopLabelLocalRect(n);
                Debug.Log("AF_MAP r2c ventanita " + max + ": " + win + ", parada siguiente " + n + " con rótulo " + (next.width > 0f ? map.LocalToView(next).ToString() : "sin rótulo")
                    + ", placa de respaldo " + map.RemainingLabelOffView + ", rótulos movidos " + map.StopLabelsMovedForHomeWindow);
                AssertNothingUnderHomeWindow(boot, map, "max " + max);
            }

            Debug.Log("AF_MAP r2c ventanita: " + withWindow + " encuadres con ella, " + moved + " rótulos de otras paradas movidos");
            Assert.That(withWindow, Is.GreaterThanOrEqualTo(5), "la ventanita sale en la vista Mundo de casi todos");
            map.Close();
        }

        [UnityTest]
        public IEnumerator In521_TheNextStopLabel_ClearsTheHomeWindow_AndStaysNextToItsStop()
        {
            yield return MapMarkersTests.OpenApp(522);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Reopen(boot, map, 521);
            map.SetView(MapView.World);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.HomeWindowShown, Is.True, "en el 521 la casa no se ve: sale la ventanita");
            int n = map.RemainingStopN();
            Rect label = map.StopLabelLocalRect(n);
            Assert.That(label.width > 0f || map.RemainingLabelOffView, Is.True, "«Faltan N» en el rótulo o en la placa de respaldo");
            if (label.width > 0f)
            {
                Assert.That(map.LocalToView(label).Overlaps(map.HomeWindowViewRect), Is.False, "el rótulo de la parada " + n + " bajo la ventanita");
                Rect view = new Rect(Vector2.zero, map.ViewportSize);
                Rect seen = map.LocalToView(label);
                Assert.That(seen.xMin >= view.xMin && seen.xMax <= view.xMax && seen.yMin >= view.yMin && seen.yMax <= view.yMax, Is.True, "entero a la vista: " + seen);
            }

            map.Close();
        }

        [UnityTest]
        public IEnumerator TappingWorld_RightAfterTheJourneyOfTheMapOf10_ChangesTheView([Values(132, 521)] int max, [Values(0, 1000)] int afterMs)
        {
            yield return MapMarkersTests.OpenApp(max + 1);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            map.Open(MapOpenSource.L10, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(max + 1, max, 0), max + 1, true);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.L10SequenceActive, Is.True, "el Mapa del 10 con af_max_level de un salto");
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            Assert.That(map.JourneyLocked, Is.True);
            Assert.That(map.ViewSwitchAlpha, Is.EqualTo(RiverMapScreen.JourneyViewSwitchAlpha), "durante el viaje el selector no parece habilitado");

            int ms = 0;
            for (; ms < 15000 && map.JourneyReadyMs < 0f; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.JourneyReadyMs, Is.GreaterThanOrEqualTo(0f), "map_play_ready");
            Level10MapTests.Tick(map, afterMs);
            Debug.Log("AF_MAP r2c Mundo tras el viaje del 10 (" + max + ", +" + afterMs + " ms): secuencia " + map.L10SequenceActive + ", selector ×" + map.ViewSwitchAlpha
                + ", subido " + map.ViewSwitchRaised);
            if (afterMs >= RiverMapScreen.JourneyButtonInMs)
            {
                Assert.That(map.ViewSwitchAlpha, Is.EqualTo(1f), "ya se ve habilitado");
            }

            yield return TapTop(map, map.ViewWorldButton);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.View, Is.EqualTo(MapView.World), "el primer toque en «Mundo» cambia de vista");
            Assert.That(map.L10SequenceActive, Is.False, "lo que quedaba de la secuencia acaba");
            AssertNothingUnderHomeWindow(boot, map, max + " al pasar a Mundo");
            AssertPlatesWhole(map, max + " al pasar a Mundo");
            Level10MapTests.Tick(map, 4000);
            Assert.That(map.View, Is.EqualTo(MapView.World), "y nada lo deshace (ni el encuadre de la secuencia ni el compañero)");
            Debug.Log("AF_MAP r2c Mundo tras el Mapa del 10 (" + max + "): ventanita " + map.HomeWindowShown + " en " + map.HomeWindowViewRect + ", rótulo de la parada "
                + map.RemainingStopN() + " en " + map.LocalToView(map.StopLabelLocalRect(map.RemainingStopN())) + ", zoom ×" + map.Zoom.ToString("0.00"));
            AssertNothingUnderHomeWindow(boot, map, max + " en Mundo, 4 s después");
            AssertPlatesWhole(map, max + " en Mundo, 4 s después");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator InTheMapOf10_Of71And79_TheNextWorldPlate_IsWhole()
        {
            yield return MapMarkersTests.OpenApp(72);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 71, 79 })
            {
                map.Close();
                RiverMapScreen.ResetCapiSession();
                map.Open(MapOpenSource.L10, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(max + 1, max, 0), max + 1, true);
                Canvas.ForceUpdateCanvases();
                Assert.That(map.L10SequenceActive, Is.True);
                int checkedFrames = 0;
                for (int ms = 0; ms < 15000 && !(map.L10SequenceDone && map.L10DimAmount <= 0f); ms += PlayModeHelpers.FrameMs)
                {
                    map.Tick(PlayModeHelpers.FrameMs);
                    if (map.L10SequenceActive && map.L10DimAmount > 0f && map.L10FactShown)
                    {
                        continue; // las placas, fuera mientras se ve la placa del dato
                    }

                    checkedFrames += AssertPlatesWhole(map, max + " a los " + ms + " ms de la secuencia") ? 1 : 0;
                }

                Level10MapTests.Tick(map, 500);
                Assert.That(map.SilhouetteCount, Is.GreaterThan(0), max + ": la placa del mundo siguiente");
                Assert.That(map.SilhouetteWorld(0), Is.EqualTo(4), max + ": «Mundo 4 · Trapecio · Nivel 81»");
                Assert.That(AssertPlatesWhole(map, max + " tras la secuencia"), Is.True, max + ": la placa sale tras la secuencia");
                Debug.Log("AF_MAP r2c placa en el Mapa del 10 (" + max + "): " + map.SilhouetteViewBox(0) + " en " + map.ViewportSize + ", " + checkedFrames + " fotogramas con ella");

                // Y en la vista Mundo que elige el jugador en esa apertura (antes, en toda la apertura del Mapa del 10, no se encajaba).
                map.SetView(MapView.World);
                map.Tick(PlayModeHelpers.FrameMs);
                AssertPlatesWhole(map, max + " en la vista Mundo");
                map.SetView(MapView.Basin);
                map.Tick(PlayModeHelpers.FrameMs);
                AssertPlatesWhole(map, max + " otra vez en la Cuenca");
            }

            map.Close();
        }

        // Ningún rótulo de parada o de pueblo, placa ni la píldora del Premio, debajo de la ventanita de casa (si sale). True si sale.
        private static bool AssertNothingUnderHomeWindow(GameBootstrap boot, RiverMapScreen map, string at)
        {
            HomeWindow? home = map.MapHomeWindow;
            if (home == null || !home.Visible)
            {
                return false;
            }

            Rect win = map.HomeWindowViewRect;
            foreach ((string what, Rect box) in LabelBoxes(boot, map))
            {
                Assert.That(map.LocalToView(box).Overlaps(win), Is.False, at + ": " + what + " debajo de la ventanita de casa (" + map.LocalToView(box) + " y " + win + ")");
            }

            RectTransform? prize = map.PrizePill;
            if (prize != null && prize.gameObject.activeInHierarchy)
            {
                Canvas.ForceUpdateCanvases();
                Rect p = StopCardTests.CanvasRect(prize, map.Canvas);
                Assert.That(p.Overlaps(StopCardTests.CanvasRect(home.Root, map.Canvas)), Is.False, at + ": la píldora del Premio bajo el retrato");
                Assert.That(p.Overlaps(StopCardTests.CanvasRect(home.PillRect, map.Canvas)), Is.False, at + ": la píldora del Premio bajo «Casa · a unos …»");
            }

            return true;
        }

        // Cada placa que sale se ve entera en la zona visible o queda entera fuera; true si alguna sale entera a la vista.
        private static bool AssertPlatesWhole(RiverMapScreen map, string at)
        {
            bool any = false;
            var view = new Rect(Vector2.zero, map.ViewportSize);
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                if (!map.SilhouettePlate(i).enabled)
                {
                    continue;
                }

                Rect box = map.SilhouetteViewBox(i);
                if (!box.Overlaps(view))
                {
                    continue;
                }

                Assert.That(box.xMin >= view.xMin - 0.5f && box.xMax <= view.xMax + 0.5f && box.yMin >= view.yMin - 0.5f && box.yMax <= view.yMax + 0.5f, Is.True,
                    at + ": la placa del mundo " + map.SilhouetteWorld(i) + " cortada: " + box + " en " + view);
                any = true;
            }

            return any;
        }

        private static void Reopen(GameBootstrap boot, RiverMapScreen map, int max)
        {
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(max + 1, max, max), max + 1, true);
            Canvas.ForceUpdateCanvases();
        }

        // Los rótulos de parada y de pueblo y las placas que se ven (cajas locales del contenido).
        private static IEnumerable<(string, Rect)> LabelBoxes(GameBootstrap boot, RiverMapScreen map)
        {
            for (int n = 0; n < boot.RiverStops!.Count; n++)
            {
                Rect r = map.StopLabelLocalRect(n);
                if (r.width > 0f)
                {
                    yield return ("el rótulo de la parada " + n, r);
                }
            }

            for (int i = 0; i < map.TownMarkerCount; i++)
            {
                Rect r = map.TownLabelLocalRect(i);
                if (r.width > 0f)
                {
                    yield return ("el rótulo del pueblo " + i, r);
                }
            }

            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                if (map.SilhouettePlate(i).enabled)
                {
                    yield return ("la placa del mundo " + map.SilhouetteWorld(i), map.SilhouetteLocalBox(i));
                }
            }
        }

        /// <summary>
        /// Toca <paramref name="button"/> en su centro como un dedo: la prueba de impacto del EventSystem tiene que dar como primer
        /// manejador de clic el propio botón (nada lo tapa: ni el área que salta el Mapa del 10 ni la del viaje) y el clic va a él.
        /// Antes deja pasar un fotograma (los Graphic recién subidos tienen depth −1 hasta que el lienzo los procesa). En batchmode
        /// la pantalla es más pequeña que el teléfono simulado: el lienzo se encoge lo justo mientras dura la prueba de impacto.
        /// </summary>
        internal static IEnumerator TapTop(RiverMapScreen map, Button button)
        {
            yield return null;
            EventSystem? events = EventSystem.current;
            Assert.That(events, Is.Not.Null, "EventSystem de la escena");
            Canvas canvas = map.Canvas.rootCanvas;
            float scale = canvas.scaleFactor;
            var hits = new List<RaycastResult>();
            var tap = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
            GameObject? handler;
            try
            {
                Rect root = ((RectTransform)canvas.transform).rect;
                Vector2 need = map.ViewportSize + new Vector2(0f, 400f); // la zona del mapa con la cabecera y el pie
                if (root.width < need.x || root.height < need.y)
                {
                    canvas.scaleFactor = Mathf.Min(scale, Mathf.Min(Screen.width / Mathf.Max(360f, need.x), Screen.height / Mathf.Max(780f, need.y)));
                }

                Canvas.ForceUpdateCanvases();
                var rt = (RectTransform)button.transform;
                Vector3 center = rt.TransformPoint(rt.rect.center);
                Camera? cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                tap.position = RectTransformUtility.WorldToScreenPoint(cam, center);
                events!.RaycastAll(tap, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), "el toque en " + tap.position + " da en algo");
                handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                Assert.That(handler, Is.SameAs(button.gameObject), "el toque da primero en «" + hits[0].gameObject.name + "» (clic: «" + (handler == null ? "nadie" : handler.name) + "»), no en «" + button.name + "»");
            }
            finally
            {
                if (canvas.scaleFactor != scale)
                {
                    canvas.scaleFactor = scale;
                    Canvas.ForceUpdateCanvases();
                }
            }

            tap.pointerCurrentRaycast = hits[0];
            tap.pointerPressRaycast = hits[0];
            tap.rawPointerPress = hits[0].gameObject;
            tap.pointerPress = handler;
            tap.eligibleForClick = true;
            ExecuteEvents.Execute(handler!, tap, ExecuteEvents.pointerClickHandler);
        }
    }
}
