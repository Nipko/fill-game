#nullable enable
using System;
using System.Collections;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
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
    /// «Norte arriba» y Rosa con sus parientes en Tarapoto (tarea M4.4; D-013, puntos 5 y 6; propuesta del Mapa §2.1, §3.1 y
    /// §6.1), en la escena <c>Game</c> real con <c>river_map_v1.json</c> y <c>river_stops_v1.json</c>:
    /// <list type="bullet">
    /// <item>el botón <c>map_north_up</c> solo en la vista Cuenca, ≥ 48 dp, con su texto de la tabla a ≥ 12 sp, dentro de la zona del
    /// mapa y sin pisar «Andes ↑»;</item>
    /// <item>un toque gira el contenido con el norte arriba (el norte del mapa, a la derecha en la vista de siempre, queda arriba y
    /// el oeste a la izquierda) en <see cref="MapConfig.NorthUpRotateMs"/> ms, con la escala <see cref="MapConfig.NorthUpScale"/>,
    /// y la cuenca entera queda dentro de la zona, centrada; otro toque lo devuelve; salir de la Cuenca lo quita en el acto;</item>
    /// <item>los rótulos no giran: los países y las placas de los mundos futuros se leen derechos durante todo el giro; «Andes ↑» y
    /// «Océano Atlántico ↓» se funden y vuelven;</item>
    /// <item>con el norte arriba no se arrastra y no salen «Volver a la canoa» ni la ventanita de casa; <c>map_close.north_up</c>;
    /// Reducir movimiento: corte; cero asignaciones durante el giro;</item>
    /// <item>Rosa salta con 2 lomos de parientes en Tarapoto (la parada 12) al llegar la canoa al ponerse al día y al tocar su mini
    /// cuia (al cerrarse su tarjeta), en ≤ 1 s, una vez por apertura, sin dos Rosas a la vez y sin asignaciones.</item>
    /// </list>
    /// </summary>
    public sealed class MapNorthUpTests
    {
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

        // ============================================================================================ «Norte arriba»

        [UnityTest]
        public IEnumerator Button_OnlyInTheBasin_ReadableAndClearOfTheAndesLabel()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.View, Is.EqualTo(MapView.World));
            Assert.That(map.NorthUpShown, Is.False, "en la vista Mundo no sale");

            map.SetView(MapView.Basin);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.NorthUpShown, Is.True, "en la Cuenca, sí");
            Assert.That(map.NorthUpLabel, Is.EqualTo(UiText.MapNorthUp));
            Assert.That(map.NorthUpLabel, Is.EqualTo("Norte arriba"));
            Assert.That(Loc.Has("map_north_up"), Is.True, "la clave del glosario §10");

            RectTransform frame = (RectTransform)map.ViewportRect.parent;
            Rect button = FrameRect((RectTransform)map.NorthUpButton.transform, frame);
            Rect viewport = FrameRect(map.ViewportRect, frame);
            Assert.That(button.width, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.01f));
            Assert.That(button.height, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.01f));
            Assert.That(Inside(button, viewport, 0.5f), Is.True, "dentro de la zona del mapa: " + button + " en " + viewport);
            Assert.That(button.center.x, Is.GreaterThan(viewport.center.x), "arriba a la derecha");
            Assert.That(button.center.y, Is.LessThan(viewport.center.y), "arriba a la derecha");
            TextMeshProUGUI andes = map.AndesLabel!;
            Assert.That(andes.gameObject.activeSelf, Is.True);
            Assert.That(button.Overlaps(FrameRect(andes.rectTransform, frame)), Is.False, "no pisa «Andes ↑»");

            TextMeshProUGUI label = map.NorthUpLabelText!;
            label.ForceMeshUpdate();
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * UiBuilder.TextScale) - 0.01f), "≥ 12 sp");
            Assert.That(label.isTextOverflowing, Is.False, "sin recortes");

            map.SetView(MapView.World);
            Assert.That(map.NorthUpShown, Is.False);
        }

        [UnityTest]
        public IEnumerator Turn_NorthUpIn300ms_At076_CenteredAndWhole_AndBack()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.SetView(MapView.Basin);
            map.Tick(16f);
            RectTransform pivot = map.NorthUpPivot!;
            Assert.That(pivot.localRotation, Is.EqualTo(Quaternion.identity), "sin giro");
            Assert.That(map.ContentRect.parent, Is.SameAs(pivot), "el contenido gira dentro del contenedor");

            map.NorthUpButton.onClick.Invoke();
            Assert.That(map.NorthUpOn, Is.True);
            Assert.That(map.NorthUpTurning, Is.True);
            map.Tick(MapConfig.NorthUpRotateMs * 0.5f);
            Assert.That(map.NorthUpProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(map.NorthUpAngle, Is.EqualTo(45f).Within(0.01f), "a medio giro");
            Assert.That(map.NorthUpTurning, Is.True);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "60 fps mientras gira");
            map.Tick((MapConfig.NorthUpRotateMs * 0.5f) - 4f);
            Assert.That(map.NorthUpTurning, Is.True, "aún no: 300 ms");
            map.Tick(4f);
            Assert.That(map.NorthUpTurning, Is.False, "acaba a los 300 ms");
            Assert.That(map.NorthUpProgress, Is.EqualTo(1f));
            Assert.That(Mathf.DeltaAngle(pivot.localEulerAngles.z, RiverMapScreen.NorthUpAngleDeg), Is.EqualTo(0f).Within(0.01f));
            Assert.That(pivot.localScale.x, Is.EqualTo(MapConfig.NorthUpScale).Within(1e-4f));
            Assert.That(pivot.localScale.y, Is.EqualTo(MapConfig.NorthUpScale).Within(1e-4f));
            Assert.That(map.NorthUpCurrentScale, Is.EqualTo(0.76f).Within(1e-4f));

            // El norte arriba: el eje x del mapa (de sur a norte, a la derecha en la vista de siempre) apunta arriba en pantalla y
            // el eje y (de oeste a este, hacia abajo) apunta a la derecha: X = y; Y = 1 − x (propuesta del Mapa §3.1).
            Vector3 north = map.ContentRect.TransformDirection(Vector3.right).normalized;
            Vector3 east = map.ContentRect.TransformDirection(Vector3.down).normalized;
            Assert.That(north.y, Is.GreaterThan(0.999f), "el norte, arriba");
            Assert.That(east.x, Is.GreaterThan(0.999f), "el este (el Atlántico), a la derecha");

            // Las figuras, derechas en pantalla (la canoa, el guía y la casa de TiTino), sin tumbarse con el contenido.
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(map.Canoe.rectTransform.eulerAngles.z, 0f)), Is.LessThan(0.01f), "la canoa");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(map.GuideImage!.rectTransform.eulerAngles.z, 0f)), Is.LessThan(0.01f), "Rosa");
            Assert.That(map.HomeShown, Is.True);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(map.HomeSloth!.Root.eulerAngles.z, 0f)), Is.LessThan(0.01f), "TiTino");

            // La cuenca entera, dentro de la zona y centrada.
            Canvas.ForceUpdateCanvases();
            Rect content = InViewport(map.ContentRect, map.ViewportRect);
            Rect zone = map.ViewportRect.rect;
            Assert.That(Inside(content, zone, 1f), Is.True, "la cuenca cabe entera: " + content + " en " + zone);
            Assert.That(Vector2.Distance(content.center, zone.center), Is.LessThan(0.5f), "centrada");
            Assert.That(content.width, Is.EqualTo(map.ContentSize.y * MapConfig.NorthUpScale).Within(0.5f));
            Assert.That(content.height, Is.EqualTo(map.ContentSize.x * MapConfig.NorthUpScale).Within(0.5f));

            // Otro toque lo devuelve, también en 300 ms.
            map.NorthUpButton.onClick.Invoke();
            Assert.That(map.NorthUpOn, Is.False);
            map.Tick(MapConfig.NorthUpRotateMs * 0.5f);
            Assert.That(map.NorthUpAngle, Is.EqualTo(45f).Within(0.01f));
            map.Tick(MapConfig.NorthUpRotateMs * 0.5f);
            Assert.That(map.NorthUpTurning, Is.False);
            Assert.That(pivot.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(pivot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(pivot.anchoredPosition.magnitude, Is.LessThan(1e-3f), "el contenido, donde estaba");
            Assert.That(map.DragEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Labels_StayUpright_AndTheFixedOnesFade()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.SetView(MapView.Basin);
            map.Tick(16f);
            Assert.That(map.CountryLabelCount, Is.GreaterThan(0), "los países en su región");
            Assert.That(map.SilhouetteCount, Is.GreaterThan(0), "las placas de los mundos futuros");

            map.NorthUpButton.onClick.Invoke();
            for (int step = 1; step <= 5; step++)
            {
                map.Tick(MapConfig.NorthUpRotateMs / 5f);
                string at = "a " + (step * MapConfig.NorthUpRotateMs / 5) + " ms (" + map.NorthUpAngle.ToString("0.#", CultureInfo.InvariantCulture) + "°)";
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(map.ContentRect.eulerAngles.z, map.NorthUpAngle)), Is.LessThan(0.01f), at + ": el contenido gira");
                foreach (TextMeshProUGUI country in map.CountryLabels())
                {
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(country.rectTransform.eulerAngles.z, 0f)), Is.LessThan(0.01f), at + ": " + country.name + " derecho");
                }

                foreach (RectTransform plate in map.SilhouettePlates())
                {
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(plate.eulerAngles.z, 0f)), Is.LessThan(0.01f), at + ": " + plate.name + " derecha");
                }

                // «Andes ↑» y «Océano Atlántico ↓», fijos en el marco: nunca giran; se funden con el giro.
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(map.AndesLabel!.rectTransform.eulerAngles.z, 0f)), Is.LessThan(0.01f), at);
                if (map.NorthUpTurning)
                {
                    Assert.That(map.OrientationLabelsAlpha, Is.LessThan(1f), at + ": se funden");
                }
            }

            Assert.That(map.NorthUpTurning, Is.False);

            // Derechos, ocupan otra caja: con el norte arriba ninguno pisa a otro.
            Canvas.ForceUpdateCanvases();
            var boxes = new System.Collections.Generic.List<(string Name, Rect Box)>();
            foreach (TextMeshProUGUI country in map.CountryLabels())
            {
                boxes.Add((country.name, InViewport(country.rectTransform, map.ViewportRect)));
            }

            foreach (RectTransform plate in map.SilhouettePlates())
            {
                boxes.Add((plate.name, InViewport(plate, map.ViewportRect)));
            }

            Rect zone = map.ViewportRect.rect;
            for (int a = 0; a < boxes.Count; a++)
            {
                Assert.That(boxes[a].Box.xMin >= zone.xMin - 0.5f && boxes[a].Box.xMax <= zone.xMax + 0.5f, Is.True,
                    boxes[a].Name + " " + boxes[a].Box + ": dentro de la zona a lo ancho");
                for (int b = a + 1; b < boxes.Count; b++)
                {
                    Rect x = boxes[a].Box;
                    Rect y = boxes[b].Box;
                    bool overlap = x.xMin < y.xMax - 0.5f && x.xMax > y.xMin + 0.5f && x.yMin < y.yMax - 0.5f && x.yMax > y.yMin + 0.5f;
                    Assert.That(overlap, Is.False, boxes[a].Name + " " + x + " pisa " + boxes[b].Name + " " + y);
                }
            }

            // Cada placa sigue señalando su sitio (revisión de M4.4): a ≤ NorthUpPlateMaxDistDp de su punto en pantalla, sin
            // taparlo, y con el punto pintado.
            AssertPlatesNearTheirPoint(map, "max 58");
            Assert.That(map.NorthUpPinCount, Is.EqualTo(map.SilhouetteCount), "un punto por placa");
            Assert.That(map.NorthUpPinAlpha, Is.EqualTo(1f), "los puntos, con el norte arriba");

            Assert.That(map.OrientationLabelsAlpha, Is.EqualTo(0f), "con el norte arriba, «Andes ↑» mentiría");
            Assert.That(map.AtlanticLabel!.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f));

            map.NorthUpButton.onClick.Invoke();
            map.Tick(MapConfig.NorthUpRotateMs);
            Assert.That(map.OrientationLabelsAlpha, Is.EqualTo(1f), "vuelven con la vista de siempre");
            Assert.That(map.AtlanticLabel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(map.NorthUpPinAlpha, Is.EqualTo(0f), "sin los puntos de las placas");
            Assert.That(map.AndesLabel!.gameObject.activeSelf && map.AtlanticLabel.gameObject.activeSelf, Is.True);
            foreach (TextMeshProUGUI country in map.CountryLabels())
            {
                Assert.That(country.rectTransform.localRotation, Is.EqualTo(Quaternion.identity), country.name);
            }
        }

        [UnityTest]
        public IEnumerator Plates_StayNearTheirPoint_WithMoreFutureWorlds()
        {
            // Con max 10 (todos los mundos futuros con placa) y con max 120 y 200: las placas, derechas, sin pisarse ni pisar los
            // países y a ≤ NorthUpPlateMaxDistDp de su punto; los países se apartan antes.
            yield return MapMarkersTests.OpenApp(11);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 10, 120, 200 })
            {
                if (max != 10)
                {
                    MapMarkersTests.Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                }

                map.SetView(MapView.Basin);
                map.Tick(16f);
                Assert.That(map.SilhouetteCount, Is.GreaterThan(0), "max " + max);
                map.NorthUpButton.onClick.Invoke();
                map.Tick(MapConfig.NorthUpRotateMs);
                Assert.That(map.NorthUpTurning, Is.False);
                Canvas.ForceUpdateCanvases();
                AssertPlatesNearTheirPoint(map, "max " + max);
                AssertNoLabelOverlaps(map, "max " + max);
                Assert.That(map.NorthUpPinCount, Is.EqualTo(map.SilhouetteCount));
                map.SetView(MapView.World);
            }
        }

        [UnityTest]
        public IEnumerator Button_ShowsWhetherItIsOn()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.SetView(MapView.Basin);
            map.Tick(16f);
            ReliefButton relief = map.NorthUpButton.GetComponent<ReliefButton>();
            TextMeshProUGUI label = map.NorthUpLabelText!;
            Assert.That(map.NorthUpLatched, Is.False, "apagado: la madera de siempre");
            Assert.That(label.color, Is.EqualTo(Palette.Cream));
            Material wood = label.fontSharedMaterial;

            map.NorthUpButton.onClick.Invoke();
            relief.Step(200f);
            Assert.That(map.NorthUpLatched, Is.True, "activo: cara crema y hundido");
            Assert.That(label.color, Is.EqualTo(RiverMapScreen.SegmentSelectedText), "el texto en la madera oscura del «Cuenca» elegido");
            Assert.That(relief.FaceOffsetDp, Is.GreaterThanOrEqualTo(3f - 0.01f), "el relieve, hundido");
            Assert.That(label.text, Is.EqualTo(UiText.MapNorthUp), "sin texto nuevo");

            // Un dedo que se va del botón lo suelta: sigue hundido mientras el norte está arriba.
            relief.Press(false);
            map.Tick(16f);
            Assert.That(map.NorthUpLatched, Is.True);

            map.NorthUpButton.onClick.Invoke();
            relief.Step(400f);
            Assert.That(map.NorthUpLatched, Is.False, "otro toque lo devuelve");
            Assert.That(label.color, Is.EqualTo(Palette.Cream));
            Assert.That(label.fontSharedMaterial, Is.SameAs(wood));
            Assert.That(relief.FaceOffsetDp, Is.LessThan(0.01f));

            // Salir de la Cuenca lo apaga.
            map.NorthUpButton.onClick.Invoke();
            map.SetView(MapView.World);
            Assert.That(map.NorthUpLatched, Is.False);
            Assert.That(label.color, Is.EqualTo(Palette.Cream));
        }

        [UnityTest]
        public IEnumerator NorthUp_NoDrag_NoBackToCanoe_NoHomeWindow_LeavingTheBasinResets()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.SetView(MapView.Basin);
            map.Tick(16f);
            map.NorthUpButton.onClick.Invoke();
            map.Tick(MapConfig.NorthUpRotateMs);
            Assert.That(map.DragEnabled, Is.False, "con el norte arriba no se arrastra");
            Assert.That(map.BackToCanoeShown, Is.False, "la canoa se ve");
            Assert.That(map.HomeInView, Is.True);
            Assert.That(map.HomeWindowShown, Is.False, "la casa se ve: sin ventanita");

            // Aunque algo mueva el encuadre por debajo, la cuenca sigue centrada.
            map.Drag(new Vector2(0f, 120f));
            map.Tick(16f);
            Canvas.ForceUpdateCanvases();
            Rect content = InViewport(map.ContentRect, map.ViewportRect);
            Assert.That(Vector2.Distance(content.center, map.ViewportRect.rect.center), Is.LessThan(0.5f), "centrada");
            Assert.That(map.BackToCanoeShown, Is.False);

            // Salir de la Cuenca lo quita en el acto, sin giro.
            map.SetView(MapView.World);
            Assert.That(map.NorthUpOn, Is.False);
            Assert.That(map.NorthUpProgress, Is.EqualTo(0f));
            Assert.That(map.NorthUpPivot!.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(map.NorthUpPivot.localScale, Is.EqualTo(Vector3.one));
            Assert.That(map.DragEnabled, Is.True);
            Assert.That(map.NorthUpShown, Is.False);
            Assert.That(map.OrientationLabelsAlpha, Is.EqualTo(1f));
            Assert.That(map.StopMarkersShown, Is.True, "la vista Mundo, como siempre");
        }

        [UnityTest]
        public IEnumerator CloseStats_NorthUp_OnlyIfUsed()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Close().NorthUp, Is.False, "sin tocarlo");

            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(59, 58, 58), 59, true);
            map.SetView(MapView.Basin);
            map.NorthUpButton.onClick.Invoke();
            map.Tick(MapConfig.NorthUpRotateMs);
            map.NorthUpButton.onClick.Invoke(); // y vuelto a su sitio: se usó igual
            MapCloseStats stats = map.Close();
            Assert.That(stats.NorthUp, Is.True, "map_close.north_up");
            Assert.That(map.NorthUpPivot!.localRotation, Is.EqualTo(Quaternion.identity), "al cerrar, sin giro");

            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(59, 58, 58), 59, true);
            Assert.That(map.NorthUpOn, Is.False, "cada apertura empieza sin giro");
            Assert.That(map.View, Is.EqualTo(MapView.World));
            Assert.That(map.Close().NorthUp, Is.False);
        }

        [UnityTest]
        public IEnumerator ReduceMotion_Cuts()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.ProgressReduceMotion = true;
            map.SetView(MapView.Basin);
            map.NorthUpButton.onClick.Invoke();
            Assert.That(map.NorthUpTurning, Is.False, "corte");
            Assert.That(map.NorthUpAngle, Is.EqualTo(RiverMapScreen.NorthUpAngleDeg));
            Assert.That(Mathf.DeltaAngle(map.NorthUpPivot!.localEulerAngles.z, RiverMapScreen.NorthUpAngleDeg), Is.EqualTo(0f).Within(0.01f));
            Assert.That(map.NorthUpPivot.localScale.x, Is.EqualTo(MapConfig.NorthUpScale).Within(1e-4f));
            Assert.That(map.OrientationLabelsAlpha, Is.EqualTo(0f));

            map.NorthUpButton.onClick.Invoke();
            Assert.That(map.NorthUpTurning, Is.False);
            Assert.That(map.NorthUpPivot.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(map.OrientationLabelsAlpha, Is.EqualTo(1f));
            map.ProgressReduceMotion = false;
        }

        [UnityTest]
        public IEnumerator Turn_DoesNotAllocate()
        {
            yield return MapMarkersTests.OpenApp(59);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.SetView(MapView.Basin);
            map.Tick(16f);
            map.Tick(16f);
            map.NorthUpButton.onClick.Invoke();
            Assert.That(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    map.Tick(16f);
                }
            }, Is.Not.AllocatingGCMemory(), "el giro de ida (320 ms)");
            Assert.That(map.NorthUpTurning, Is.False);
            Assert.That(() => map.Tick(16f), Is.Not.AllocatingGCMemory(), "con el norte arriba y quieto");

            map.NorthUpButton.onClick.Invoke();
            Assert.That(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    map.Tick(16f);
                }
            }, Is.Not.AllocatingGCMemory(), "el giro de vuelta");
            Assert.That(map.NorthUpTurning, Is.False);
        }

        // ============================================================================================ Rosa en Tarapoto

        [UnityTest]
        public IEnumerator RosaJumps_WhenTheCanoeArrivesAtTarapoto_Once_UnderOneSecond()
        {
            yield return MapMarkersTests.OpenApp(121, 115); // max 120: la canoa llega a Tarapoto al ponerse al día
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.RiverStops!.At(RiverMapScreen.RosaJumpStop)!.Id, Is.EqualTo("tarapoto"), "la parada 12, los lagos de Tarapoto");
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.RosaJumpCount, Is.EqualTo(0));

            float playingMs = 0f;
            int maxRelatives = 0;
            bool sawBoth = false;
            for (int t = 0; t < 2500; t += 16)
            {
                map.Tick(16f);
                if (map.RosaJumpPlaying)
                {
                    playingMs += 16f;
                    maxRelatives = Mathf.Max(maxRelatives, map.RosaRelativesShown);
                    Assert.That(map.GuideId, Is.EqualTo(RiverJourney.GuideRosa));
                    Assert.That(map.GuideImage!.gameObject.activeSelf, Is.False, "una sola Rosa: la que guía es la que salta");
                    Assert.That(map.RosaJumper!.gameObject.activeInHierarchy, Is.True);
                    RectTransform layer = map.RosaJumpLayer!;
                    Assert.That(layer.GetSiblingIndex(), Is.EqualTo(layer.parent.childCount - 1), "el salto, encima de las líneas de las mini cuias");
                    sawBoth |= map.RosaRelativesShown == RiverMapScreen.RosaJumpRelatives;
                }
            }

            Assert.That(map.RosaJumpCount, Is.EqualTo(1), "una vez");
            Assert.That(playingMs, Is.GreaterThan(0f));
            Assert.That(playingMs, Is.LessThanOrEqualTo(1000f), "≤ 1 s");
            Assert.That(maxRelatives, Is.EqualTo(2), "2 lomos más");
            Assert.That(sawBoth, Is.True, "los dos parientes llegan a verse a la vez");
            Assert.That(map.RosaJumpPlaying, Is.False);
            Assert.That(map.GuideImage!.gameObject.activeSelf, Is.True, "y Rosa vuelve a guiar");
            RiverStopRoute route = boot.RiverRoutes!.At(RiverMapScreen.RosaJumpStop)!;
            Vector2 tarapoto = RiverMapScreen.PlacePoint(boot.RiverMapData!, route.At);
            Assert.That(Vector2.Distance(map.RosaJumpStopMapPoint, tarapoto), Is.LessThan(1e-5f), "Tarapoto");
            float dpu = map.Space.DpPerUnit;
            Assert.That(Vector2.Distance(map.RosaJumpMapPoint, tarapoto) * dpu, Is.LessThanOrEqualTo(RiverMapScreen.RosaJumpNearDp + 0.01f),
                "salta en Tarapoto: donde nadaba la Rosa que guía, junto a la parada");
            AssertRelativesReadable(map);
        }

        [UnityTest]
        public IEnumerator RosaDoesNotJump_WithTheBufeoGuiding_OrBeforeTarapoto()
        {
            yield return MapMarkersTests.OpenApp(121);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;

            // Mundo 7 (max 305): guía el bufeo y Rosa está en los rápidos de Teotônio (D-013, punto 6).
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            map.Tick(16f);
            Assert.That(map.GuideId, Is.EqualTo(RiverJourney.GuideBufeo));
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True, "la mini cuia de Tarapoto se puede tocar en el mundo 7");
            Assert.That(map.CardOpen, Is.True, "el toque abre su tarjeta");
            Assert.That(map.RosaJumpPending, Is.False, "sin salto: no guía Rosa");
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying, Is.False);
            Assert.That(map.RosaJumpCount, Is.EqualTo(0));
            Assert.That(map.GuideImage!.gameObject.activeSelf, Is.True, "el bufeo sigue guiando");

            // Mundo 4 antes de ganar el 120: Tarapoto aún no se alcanzó.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(115, 114, 114), 115);
            map.Tick(16f);
            Assert.That(map.GuideId, Is.EqualTo(RiverJourney.GuideRosa));
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True, "la parada futura se puede tocar");
            Assert.That(map.RosaJumpPending, Is.False, "sin salto antes del 120");
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying, Is.False);
            Assert.That(map.RosaJumpCount, Is.EqualTo(0));
            Assert.That(map.GuideImage!.gameObject.activeSelf, Is.True, "la Rosa que guía sigue delante de la canoa");
        }

        [UnityTest]
        public IEnumerator RosaJump_StaysUpright_WhenTheNorthGoesUp()
        {
            yield return MapMarkersTests.OpenApp(121);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.Tick(16f);
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True);
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying, Is.True);

            map.SetView(MapView.Basin);
            map.NorthUpButton.onClick.Invoke();
            map.Tick(MapConfig.NorthUpRotateMs);
            Assert.That(map.NorthUpTurning, Is.False);
            Assert.That(map.RosaJumpPlaying, Is.True, "el salto sigue: 900 ms");
            Canvas.ForceUpdateCanvases();

            float p = map.RosaJumpElapsedMs / RiverMapScreen.RosaJumpMs;
            float k = RiverMapScreen.GuideBasinDp / RiverMapScreen.GuideWorldDp;
            RectTransform rosa = map.RosaJumper!.rectTransform;
            float dir = -Mathf.Sign(rosa.localScale.x);
            float pitch = dir * Mathf.Lerp(RiverMapScreen.RosaJumpPitchDeg, -RiverMapScreen.RosaJumpPitchDeg, p);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(rosa.eulerAngles.z, pitch)), Is.LessThan(0.5f), "el cabeceo, en pantalla: Rosa no salta de lado");

            RectTransform layer = map.RosaJumpLayer!;
            Vector2 local = map.Space.ToLocal(map.RosaJumpMapPoint);
            Rect r = layer.rect;
            Vector3 point = layer.TransformPoint(new Vector3(r.xMin + local.x, r.yMax + local.y, 0f));
            Vector2 at = map.ViewportRect.InverseTransformPoint(rosa.position);
            Vector2 from = map.ViewportRect.InverseTransformPoint(point);
            float s = MapConfig.NorthUpScale;
            Assert.That(at.y - from.y, Is.EqualTo(4f * RiverMapScreen.RosaJumpHeightDp * k * p * (1f - p) * s).Within(0.5f), "el arco, hacia arriba en pantalla");
            Assert.That(at.x - from.x, Is.EqualTo(dir * RiverMapScreen.RosaJumpSpanDp * k * (p - 0.5f) * s).Within(0.5f), "y hacia donde mira la canoa");
            for (int i = 0; i < RiverMapScreen.RosaJumpRelatives; i++)
            {
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(map.RosaRelative(i)!.eulerAngles.z, 0f)), Is.LessThan(0.5f), "el lomo " + i + ", derecho");
            }
        }

        [UnityTest]
        public IEnumerator RosaJumps_WhenTarapotoIsTapped_AfterItsCard_OncePerOpening()
        {
            yield return MapMarkersTests.OpenApp(121);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.Tick(16f);
            Assert.That(map.CatchingUp, Is.False);
            Assert.That(map.RosaJumpCount, Is.EqualTo(0), "abrir no basta: al llegar o al tocarla");

            // Otra parada: nada.
            Assert.That(map.TapStop(11), Is.True);
            Assert.That(map.RosaJumpPending, Is.False);
            map.Card!.CloseButton.onClick.Invoke();

            // Tarapoto: su tarjeta encima; el salto espera a que se cierre.
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True);
            Assert.That(map.CardOpen, Is.True);
            Assert.That(map.RosaJumpPending, Is.True);
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying, Is.False, "la tarjeta lo taparía");
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying, Is.True);
            Assert.That(map.RosaJumpCount, Is.EqualTo(1));

            // En su sitio y en su arco, sin asignaciones.
            Vector2 tarapoto = map.Space.ToLocal(map.RosaJumpMapPoint);
            map.Tick(RiverMapScreen.RosaJumpMs * 0.5f - 16f);
            RectTransform rosa = map.RosaJumper!.rectTransform;
            Assert.That(Mathf.Abs(rosa.anchoredPosition.x - tarapoto.x), Is.LessThan(1f), "a medio salto, sobre Tarapoto");
            Assert.That(rosa.anchoredPosition.y - tarapoto.y, Is.EqualTo(RiverMapScreen.RosaJumpHeightDp).Within(0.5f), "en lo alto del arco");
            Assert.That(() => map.Tick(16f), Is.Not.AllocatingGCMemory(), "el salto no asigna");
            map.Tick(RiverMapScreen.RosaJumpMs);
            Assert.That(map.RosaJumpPlaying, Is.False);

            // Una vez por apertura.
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True);
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying || map.RosaJumpPending, Is.False, "ya saltó en esta apertura");
            Assert.That(map.RosaJumpCount, Is.EqualTo(1));

            // Otra apertura, otro salto.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(121, 120, 120), 121);
            Assert.That(map.RosaJumpCount, Is.EqualTo(0));
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True);
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying, Is.True);

            // Cerrar el Mapa lo acaba.
            map.Close();
            Assert.That(map.RosaJumpPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator RosaJump_WithReduceMotion_DoesNotPlay()
        {
            yield return MapMarkersTests.OpenApp(121);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.ProgressReduceMotion = true;
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True);
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            Assert.That(map.RosaJumpPlaying || map.RosaJumpPending, Is.False, "Reducir movimiento: sin salto");
            Assert.That(map.RosaJumpCount, Is.EqualTo(0));
            map.ProgressReduceMotion = false;
        }

        // ============================================================================================ ayudas

        // Con el norte arriba, cada placa a ≤ NorthUpPlateMaxDistDp de su punto en pantalla (del punto a su borde más cercano) y sin
        // taparlo.
        private static void AssertPlatesNearTheirPoint(RiverMapScreen map, string at)
        {
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                RectTransform plate = map.SilhouettePlate(i).rectTransform; // estirada a su silueta
                Rect box = InViewport(plate, map.ViewportRect);
                Vector2 point = map.ViewportRect.InverseTransformPoint(map.SilhouettePivotWorld(i));
                float dx = Mathf.Max(0f, Mathf.Max(box.xMin - point.x, point.x - box.xMax));
                float dy = Mathf.Max(0f, Mathf.Max(box.yMin - point.y, point.y - box.yMax));
                float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                Assert.That(dist, Is.LessThanOrEqualTo(RiverMapScreen.NorthUpPlateMaxDistDp + 0.5f),
                    at + ": " + plate.parent.name + " " + box + " lejos de su punto " + point);
                Assert.That(dist, Is.GreaterThan(RiverMapScreen.NorthUpPinDotDp * 0.5f), at + ": " + plate.parent.name + " no tapa su punto");
            }
        }

        // Con el norte arriba, ningún rótulo (países y placas) pisa a otro y todos quedan dentro de la zona a lo ancho.
        private static void AssertNoLabelOverlaps(RiverMapScreen map, string at)
        {
            var boxes = new System.Collections.Generic.List<(string Name, Rect Box)>();
            foreach (TextMeshProUGUI country in map.CountryLabels())
            {
                boxes.Add((country.name, InViewport(country.rectTransform, map.ViewportRect)));
            }

            foreach (RectTransform plate in map.SilhouettePlates())
            {
                boxes.Add((plate.name, InViewport(plate, map.ViewportRect)));
            }

            Rect zone = map.ViewportRect.rect;
            for (int a = 0; a < boxes.Count; a++)
            {
                Assert.That(boxes[a].Box.xMin >= zone.xMin - 0.5f && boxes[a].Box.xMax <= zone.xMax + 0.5f, Is.True,
                    at + ": " + boxes[a].Name + " " + boxes[a].Box + ": dentro de la zona a lo ancho");
                for (int b = a + 1; b < boxes.Count; b++)
                {
                    Rect x = boxes[a].Box;
                    Rect y = boxes[b].Box;
                    bool overlap = x.xMin < y.xMax - 0.5f && x.xMax > y.xMin + 0.5f && x.yMin < y.yMax - 0.5f && x.yMax > y.yMin + 0.5f;
                    Assert.That(overlap, Is.False, at + ": " + boxes[a].Name + " " + x + " pisa " + boxes[b].Name + " " + y);
                }
            }
        }

        // Los lomos de los parientes se leen como delfines (≥ 24 × 10 dp, con su chapoteo de 12 × 4) y asoman por el río cerca de
        // Rosa, a ≥ RosaRelativeClearDp de la canoa con sus banderas y sin pisarse.
        private static void AssertRelativesReadable(RiverMapScreen map)
        {
            Rect gear = map.CanoeGearBoxLocal;
            float g = RiverMapScreen.RosaRelativeClearDp;
            var clear = Rect.MinMaxRect(gear.xMin - g, gear.yMin - g, gear.xMax + g, gear.yMax + g);
            Vector2 rosa = map.Space.ToLocal(map.RosaJumpMapPoint);
            var boxes = new Rect[RiverMapScreen.RosaJumpRelatives];
            for (int i = 0; i < RiverMapScreen.RosaJumpRelatives; i++)
            {
                Vector2 size = map.RosaRelativeBack(i)!.rectTransform.sizeDelta;
                Assert.That(size.x, Is.GreaterThanOrEqualTo(24f), "el lomo " + i + ", ≥ 24 dp de ancho");
                Assert.That(size.y, Is.GreaterThanOrEqualTo(10f), "y ≥ 10 dp de alto");
                Vector2 at = map.Space.ToLocal(map.RosaRelativeMapPoint(i));
                float w = Mathf.Max(RiverMapScreen.RosaBackWidthDp, RiverMapScreen.RosaSplashWidthDp);
                boxes[i] = new Rect(at.x - (w * 0.5f), at.y - RiverMapScreen.RosaSplashHeightDp, w, RiverMapScreen.RosaBackHeightDp + RiverMapScreen.RosaSplashHeightDp);
                Assert.That(gear.width, Is.GreaterThan(0f));
                Assert.That(boxes[i].Overlaps(clear), Is.False, "el lomo " + i + " " + boxes[i] + " lejos de la canoa y sus banderas " + gear);
                Assert.That(Vector2.Distance(at, rosa), Is.InRange(1f, 80f), "el lomo " + i + ", cerca de Rosa");
            }

            Assert.That(boxes[0].Overlaps(boxes[1]), Is.False, "los dos lomos no se pisan");
        }

        // Caja de rt en coordenadas del marco (dp, y hacia abajo).
        internal static Rect FrameRect(RectTransform rt, RectTransform frame)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = frame.InverseTransformPoint(corners[0]);
            Vector3 b = frame.InverseTransformPoint(corners[2]);
            Rect f = frame.rect;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) - f.xMin, f.yMax - Mathf.Max(a.y, b.y), Mathf.Max(a.x, b.x) - f.xMin, f.yMax - Mathf.Min(a.y, b.y));
        }

        // Caja (alineada a los ejes) de rt, girado o no, en las coordenadas locales de la zona visible (y hacia arriba).
        private static Rect InViewport(RectTransform rt, RectTransform viewport)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float x0 = float.MaxValue;
            float y0 = float.MaxValue;
            float x1 = float.MinValue;
            float y1 = float.MinValue;
            foreach (Vector3 c in corners)
            {
                Vector3 p = viewport.InverseTransformPoint(c);
                x0 = Mathf.Min(x0, p.x);
                y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x);
                y1 = Mathf.Max(y1, p.y);
            }

            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        private static bool Inside(Rect inner, Rect outer, float tolerance)
        {
            return inner.xMin >= outer.xMin - tolerance && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance && inner.yMax <= outer.yMax + tolerance;
        }
    }

    /// <summary>
    /// Capturas de «Norte arriba» y del salto de Rosa en Tarapoto (tarea M4.4), con el arte provisional: <c>13b_map_norte</c>
    /// (max 57, la vista Cuenca con el norte arriba a 0,76: la cuenca entera, centrada, con los países y las placas derechos y sin
    /// «Andes ↑» ni «Océano Atlántico ↓») y <c>13c_map_tarapoto_salto</c> (max 120, tras tocar Tarapoto y cerrar su tarjeta: Rosa
    /// en lo alto del salto con los lomos de sus parientes). Cada captura escribe su línea <c>AF_MAP</c>. Explícito: necesita
    /// gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MapNorthUpSnapshotTests</c> sin
    /// <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de «Norte arriba» y del salto de Rosa en Tarapoto (M4.4); necesitan dispositivo gráfico.")]
    public sealed class MapNorthUpSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

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
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureNorthUpAndTarapoto()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 58;
            profile.Progress.MaxLevel = 57;
            profile.Progress.MapSeenLevel = 57;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachOverlay(map.Canvas);
            map.ViewBasinButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.2f);
            map.NorthUpButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.6f);
            Assert.That(map.NorthUpOn, Is.True);
            Assert.That(map.NorthUpTurning, Is.False);
            SnapshotKit.Save(dir, "13b_map_norte");
            Log("13b_map_norte", map);

            yield return MapMarkersTests.OpenApp(121);
            boot = GameBootstrap.Current!;
            map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            map.CenterOnStopMarker(RiverMapScreen.RosaJumpStop);
            Assert.That(map.TapStop(RiverMapScreen.RosaJumpStop), Is.True);
            map.Card!.CloseButton.onClick.Invoke();
            map.Tick(16f);
            map.Tick((RiverMapScreen.RosaJumpMs * 0.5f) - 16f); // en lo alto del arco, con los dos parientes asomando
            yield return SnapshotKit.Wait(0.2f);
            Assert.That(map.RosaJumpPlaying, Is.True);
            SnapshotKit.Save(dir, "13c_map_tarapoto_salto");
            Log("13c_map_tarapoto_salto", map);
        }

        private static void Log(string name, RiverMapScreen map)
        {
            Debug.Log("AF_MAP " + name + ": vista " + map.View + ", norte arriba " + map.NorthUpOn + " ("
                + map.NorthUpAngle.ToString("0.#", CultureInfo.InvariantCulture) + "°, escala "
                + map.NorthUpCurrentScale.ToString("0.00", CultureInfo.InvariantCulture) + "), contenido " + map.ContentSize + " en "
                + map.ViewportSize + ", países " + map.CountryLabelCount + ", siluetas " + map.SilhouetteCount + ", salto de Rosa "
                + map.RosaJumpPlaying + " (" + map.RosaJumpElapsedMs.ToString("0", CultureInfo.InvariantCulture) + " ms, "
                + map.RosaRelativesShown + " parientes)");
        }
    }
}
