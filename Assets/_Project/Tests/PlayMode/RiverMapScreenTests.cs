#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Levels;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La pantalla del Mapa del río en la escena <c>Game</c> real (tarea M3.3; D-013, puntos 4, 5 y 7; 06 §4.5 y §4.7; 08 §5.4;
    /// propuesta del Mapa §3.2, §8.1, §8.4 y §11.4):
    /// <list type="bullet">
    /// <item>la pestaña del inicio sale desde <c>max_level</c> 10 (no en el 9), con el punto si hay una llegada sin ver, que se va
    /// al cerrar el Mapa (<c>map_seen_level</c>);</item>
    /// <item>abre en la vista Mundo (entre 4× y 15×) con la pregunta del mundo, su ✓ cuando ya se respondió, «Faltan N» y «Jugar»;
    /// arrastre dentro del mapa, «Volver a la canoa» y la vista Cuenca a 1×;</item>
    /// <item>«Jugar» abre el nivel; el ← y el Atrás de Android cierran;</item>
    /// <item>tres lienzos (dos anidados sin <c>overrideSorting</c>) por debajo de la hoja de compra, las mallas al abrir, cero
    /// asignaciones con el Mapa quieto y 30 fps en reposo (60 arrastrando y al cerrar);</item>
    /// <item>las sondas <c>map_open</c> y <c>map_close</c>, el extra <c>af_max_level</c> y la maquetación sin solapes a 360 × 640 y
    /// a 360 × 780, también con el texto al 130 %.</item>
    /// </list>
    /// </summary>
    public sealed class RiverMapScreenTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator MapTab_ShowsFromMaxLevel10_NotAt9()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(10)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(boot.RiverMapAvailable, Is.True, "la escena trae la geometría y las paradas");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.MapTabShown, Is.False, "con max_level 9 no hay pestaña (D-008: oculta hasta el 10)");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "ni se abre desde el inicio");

            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(11)));
            boot = GameBootstrap.Current!;
            lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.MapTabShown, Is.True, "desde max_level 10 (06 §4.7)");
            Assert.That(lobby.MapTabDotShown, Is.True, "la llegada del 10 aún no se vio");
            Assert.That(lobby.MapTabButton.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator MapTab_Dot_GoesAwayAfterSeeingTheMap()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.MapTabDotShown, Is.True, "map_seen_level 0 con max_level 57");
            lobby.MapTabButton.onClick.Invoke();
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Visible, Is.True, "la pestaña abre el Mapa");
            yield return null;
            Assert.That(boot.CloseRiverMap(), Is.True);
            yield return null;
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(57), "al cerrar se anota lo visto hasta max_level");
            Assert.That(lobby.MapTabDotShown, Is.False, "ya no hay llegadas sin ver");
            Assert.That(lobby.MapTabShown, Is.True);
            Assert.That(boot.MarkMapSeen(30), Is.False, "map_seen_level nunca baja");
            Assert.That(boot.Profile.Progress.MapSeenLevel, Is.EqualTo(57));
        }

        [UnityTest]
        public IEnumerator Open_ShowsTheWorldView_WithTheWorldQuestion_StopRemainingAndPlay()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Visible, Is.True);
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.Lobby));
            Assert.That(map.View, Is.EqualTo(MapView.World), "abre en la vista Mundo (D-013, punto 5)");
            Assert.That(map.World, Is.EqualTo(3), "el mundo de «Jugar · Nivel 58»");
            Assert.That(map.Zoom, Is.InRange(RiverMapScreen.WorldZoomMin, RiverMapScreen.WorldZoomMax));
            Assert.That(map.Title, Is.EqualTo("Manaos · ¿Por qué el río Negro es negro?"), "map_world_question (glosario §10)");
            Assert.That(map.QuestionAnswered, Is.False, "se responde al llegar a Anavilhanas (el 60)");
            Assert.That(map.CheckShown, Is.False);
            Assert.That(map.StopRemainingShown, Is.True);
            Assert.That(map.StopRemainingText, Is.EqualTo("Faltan 3 niveles para Anavilhanas"));
            Assert.That(map.PlayLabel, Is.EqualTo(UiText.PlayLevel(58)));
            Assert.That(map.ViewWorldLabel, Is.EqualTo("Mundo"));
            Assert.That(map.ViewBasinLabel, Is.EqualTo("Cuenca"));
            Assert.That(map.HasCanoe, Is.True);
            Assert.That(map.CanoeInView, Is.True, "la canoa se ve al abrir");
            Assert.That(map.BackToCanoeShown, Is.False);

            // La canoa, a 7/10 del tramo de Manaos a Anavilhanas (max_level 57).
            Vector2 expected = RiverMapScreen.RoutePoint(boot.RiverMapData!, boot.RiverRoutes!.At(6)!, 0.7f);
            Assert.That(Vector2.Distance(map.CanoeMapPoint, expected), Is.LessThan(1e-5f));
        }

        [UnityTest]
        public IEnumerator Viewport_Ground_IsLandOutsideTheBasin_NotSea()
        {
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Image ground = map.Ground;
            Assert.That(ground.transform.parent, Is.SameAs(map.ViewportRect), "el fondo del visor");
            Assert.That(ground.transform.GetSiblingIndex(), Is.EqualTo(0), "detrás del contenido");
            Assert.That(ground.raycastTarget, Is.True, "recibe el arrastre fuera de la cuenca");
            Assert.That(ground.color, Is.EqualTo(RiverMapScreen.GroundColor), "tierra, no mar: la cuenca no es una isla (D-003)");
            Assert.That(RiverMapScreen.GroundColor, Is.Not.EqualTo(RiverMapScreen.SeaColor));
            foreach (Image image in map.ViewportRect.GetComponentsInChildren<Image>(true))
            {
                Assert.That(image.color, Is.Not.EqualTo(RiverMapScreen.SeaColor), image.name + ": sin polígono del mar en los datos, no se pinta mar");
            }

            map.ViewBasinButton.onClick.Invoke();
            yield return null;
            Assert.That(ground.color, Is.EqualTo(RiverMapScreen.GroundColor), "también en Cuenca");
        }

        [UnityTest]
        public IEnumerator Glass_InTheWorldView_IsWiderPerClass_ThanInTheBasin()
        {
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 1), Is.EqualTo(8f));
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 2), Is.EqualTo(5f));
            Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, 3), Is.EqualTo(3f));
            for (int c = 1; c <= 3; c++)
            {
                Assert.That(RiverMapScreen.RibbonWidthDp(MapView.Basin, c), Is.EqualTo(RiverRibbonGraphic.WidthForClass(c)), "en Cuenca, el de la cinta");
                Assert.That(RiverMapScreen.RibbonWidthDp(MapView.World, c), Is.GreaterThan(RiverRibbonGraphic.WidthForClass(c)), "en Mundo, más ancho");
            }

            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.GlassCount, Is.GreaterThan(0));
            var classes = new HashSet<int>();
            for (int i = 0; i < map.GlassCount; i++)
            {
                RiverRibbonGraphic ribbon = map.Glass(i);
                classes.Add(ribbon.Line!.Class);
                Assert.That(ribbon.WidthDp, Is.EqualTo(RiverMapScreen.RibbonWidthDp(MapView.World, ribbon.Line.Class)), ribbon.name + " en Mundo");
            }

            Assert.That(classes, Is.SupersetOf(new[] { 1, 2 }), "el cauce y los afluentes");

            map.ViewBasinButton.onClick.Invoke();
            yield return null;
            for (int i = 0; i < map.GlassCount; i++)
            {
                RiverRibbonGraphic ribbon = map.Glass(i);
                Assert.That(ribbon.WidthDp, Is.EqualTo(RiverRibbonGraphic.WidthForClass(ribbon.Line!.Class)), ribbon.name + " en Cuenca");
            }

            map.ViewWorldButton.onClick.Invoke();
            yield return null;
            Assert.That(map.Glass(0).WidthDp, Is.EqualTo(RiverMapScreen.RibbonWidthDp(MapView.World, map.Glass(0).Line!.Class)), "de vuelta a Mundo");
        }

        [UnityTest]
        public IEnumerator StopRemaining_KeepsItsAirFromTheFillet_InTwoLines()
        {
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI label = map.StopRemainingLabel;
            label.ForceMeshUpdate();
            Rect plate = map.StopRemainingRect.rect;
            Rect box = label.rectTransform.rect;
            Assert.That(label.textInfo.lineCount, Is.EqualTo(2), "«Faltan 3 niveles para Anavilhanas» va en dos líneas");
            Assert.That(plate.height, Is.EqualTo(56f).Within(0.5f), "fila de 56 dp con el texto en dos líneas");
            Assert.That(box.width, Is.EqualTo(plate.width - 28f).Within(0.5f), "14 dp de aire a cada lado, desde el filete");
            Assert.That(box.height, Is.EqualTo(plate.height - 20f).Within(0.5f), "10 dp arriba y abajo");
            Assert.That(label.rectTransform.anchoredPosition.x, Is.EqualTo(14f).Within(0.5f));
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(13f * UiBuilder.TextScale - 0.01f), "no se encoge más de un punto");
            Rect ink = new Rect(label.textBounds.min, label.textBounds.size);
            Assert.That(ink.xMin, Is.GreaterThanOrEqualTo(box.xMin - 0.5f), "el texto no se sale de su caja");
            Assert.That(ink.xMax, Is.LessThanOrEqualTo(box.xMax + 0.5f));
        }

        [UnityTest]
        public IEnumerator Open_InAnAnsweredWorld_ShowsTheProceduralCheck()
        {
            yield return OpenAt(61);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.World, Is.EqualTo(3));
            Assert.That(map.QuestionAnswered, Is.True, "con el 60 ganado se llegó a Anavilhanas, que responde la pregunta del mundo 3");
            Assert.That(map.CheckShown, Is.True, "el ✓ es un icono (P-M2), no un carácter del texto");
            Assert.That(map.Title, Does.Not.Contain("✓"));
            Assert.That(RiverMapScreen.WorldQuestionAnswered(GameBootstrap.Current.RiverStops!, 3, 59), Is.False);
            Assert.That(RiverMapScreen.WorldQuestionAnswered(GameBootstrap.Current.RiverStops!, 3, 60), Is.True);
            Rect check = CanvasRect(map.CheckRect, map.Canvas);
            Rect header = CanvasRect(map.HeaderRect, map.Canvas);
            Assert.That(header.Contains(check.center), Is.True, "el ✓ va en la cabecera");
            Assert.That(CanvasRect(map.TitleLabel.rectTransform, map.Canvas).Overlaps(Shrunk(check)), Is.False, "sin tapar la pregunta");
        }

        [UnityTest]
        public IEnumerator Drag_MovesInsideTheMap_AndBackToCanoeRecenters()
        {
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Vector2 start = map.ContentOffset;
            map.Drag(new Vector2(-60f, 40f));
            Assert.That(map.ContentOffset.x, Is.EqualTo(start.x - 60f).Within(0.01f), "arrastrar a la izquierda mueve el mapa a la izquierda");
            Assert.That(map.ContentOffset.y, Is.EqualTo(start.y - 40f).Within(0.01f), "arrastrar hacia arriba sube el mapa");

            // Nunca se sale del mapa: la esquina queda entre vista − contenido y 0.
            map.Drag(new Vector2(-100000f, -100000f));
            Vector2 view = map.ViewportSize;
            Vector2 content = map.ContentSize;
            Assert.That(map.ContentOffset.x, Is.EqualTo(view.x - content.x).Within(0.01f));
            Assert.That(map.ContentOffset.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(map.CanoeInView, Is.False);
            Assert.That(map.BackToCanoeShown, Is.True, "con la canoa fuera de la vista sale «Volver a la canoa»");
            Assert.That(map.BackToCanoeLabel, Is.EqualTo("Volver a la canoa"));

            map.BackToCanoeButton.onClick.Invoke();
            Assert.That(map.CanoeInView, Is.True);
            Vector2 canoe = map.CanoeInViewport;
            Assert.That(canoe.x, Is.EqualTo(view.x * 0.5f).Within(1f), "la canoa, en el centro");
            Assert.That(canoe.y, Is.EqualTo(view.y * 0.5f).Within(1f));
            Assert.That(map.BackToCanoeShown, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ViewSwitch_Basin_ShowsTheWholeBasinAt1x()
        {
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            float worldZoom = map.Zoom;
            map.ViewBasinButton.onClick.Invoke();
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            Assert.That(map.Zoom, Is.EqualTo(1f));
            Assert.That(map.Space.DpPerUnit, Is.EqualTo(MapSpace.BasinWidthDp).Within(0.01f), "1×: 328 dp, el ancho útil (360 − 2 × 16)");
            Assert.That(map.ViewportSize.x, Is.EqualTo(MapSpace.BasinWidthDp).Within(0.5f));
            Assert.That(map.ContentSize.y, Is.LessThanOrEqualTo(map.ViewportSize.y + 0.5f), "a 360 × 780 la cuenca entera cabe");
            Assert.That(map.CanoeInView, Is.True);
            Assert.That(map.BackToCanoeShown, Is.False, "con la cuenca entera a la vista no hace falta (a 360 × 640, sí: Basin_At360x640_…)");
            Vector2 offset = map.ContentOffset;
            map.Drag(new Vector2(50f, 50f));
            Assert.That(map.ContentOffset, Is.EqualTo(offset), "si el mapa cabe, se queda centrado");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.Glass(0).LastVertexCount, Is.GreaterThan(0), "el vidrio se reconstruye a la escala nueva");

            map.ViewWorldButton.onClick.Invoke();
            Assert.That(map.View, Is.EqualTo(MapView.World));
            Assert.That(map.Zoom, Is.EqualTo(worldZoom).Within(1e-4f), "la misma vista Mundo");
        }

        [UnityTest]
        public IEnumerator Basin_At360x640_DoesNotFit_AndOffersBackToCanoe()
        {
            // A 360 × 640 la cuenca a 1× (≈ 431 dp de alto) no cabe en la zona del mapa (≈ 354 dp): se arrastra en vertical y la
            // canoa puede quedar fuera, así que «Volver a la canoa» sale también en la Cuenca (M3.3, punto 2; propuesta §3.2).
            GameScreen.SimulatedScreen = new Vector4(1080f, 1920f, 96f, 48f); // un teléfono de 360 × 640 dp
            yield return OpenAt(11); // max_level 10: la canoa en la parada 1, cerca de la desembocadura (un extremo de la cuenca)
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.ViewBasinButton.onClick.Invoke();
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            Assert.That(map.Zoom, Is.EqualTo(1f));
            Assert.That(map.ContentSize.y, Is.GreaterThan(map.ViewportSize.y + 1f), "a 360 × 640 la cuenca no cabe entera");
            Assert.That(map.HasCanoe, Is.True);

            // Hasta un extremo; si la canoa sigue a la vista, hasta el otro.
            map.Drag(new Vector2(0f, -100000f));
            if (map.CanoeInView)
            {
                map.Drag(new Vector2(0f, 200000f));
            }

            Assert.That(map.CanoeInView, Is.False, "la canoa queda fuera en uno de los dos extremos");
            Assert.That(map.BackToCanoeShown, Is.True, "«Volver a la canoa», también en la Cuenca");
            Assert.That(map.BackToCanoeLabel, Is.EqualTo("Volver a la canoa"));

            map.BackToCanoeButton.onClick.Invoke();
            Assert.That(map.View, Is.EqualTo(MapView.Basin), "sin cambiar de vista");
            Assert.That(map.CanoeInView, Is.True);
            Assert.That(map.BackToCanoeShown, Is.False);
            yield return null;

            // De vuelta en Mundo, el botón sigue las mismas reglas.
            map.ViewWorldButton.onClick.Invoke();
            Assert.That(map.CanoeInView, Is.True);
            Assert.That(map.BackToCanoeShown, Is.False);
        }

        [UnityTest]
        public IEnumerator ViewportCorners_FollowTheFrameFillet()
        {
            // RectMask2D recorta en ángulo recto: cuatro rincones de madera encima del contenido redondean la zona del mapa,
            // concéntricos con las esquinas de fuera del marco (D-003).
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            RiverMapScreen.FrameCornersGraphic corners = map.ViewportCorners;
            Transform frame = map.ViewportRect.parent;
            Assert.That(corners.transform.parent, Is.SameAs(frame), "en el marco, fuera de la máscara");
            Assert.That(corners.transform.GetSiblingIndex(), Is.EqualTo(map.ViewportRect.GetSiblingIndex() + 1), "justo encima del contenido");
            Assert.That(corners.transform.GetSiblingIndex(), Is.LessThan(map.BackToCanoeButton.transform.GetSiblingIndex()), "debajo de «Volver a la canoa»");
            Assert.That(corners.raycastTarget, Is.False, "el arrastre pasa al mapa");
            Assert.That(corners.color, Is.EqualTo(frame.GetComponent<Image>().color), "la madera del marco");
            Assert.That(RiverMapScreen.ViewportRadiusDp, Is.EqualTo(RiverMapScreen.FrameRadiusDp - RiverMapScreen.FramePadDp));
            Assert.That(corners.Radius, Is.EqualTo(RiverMapScreen.ViewportRadiusDp));
            Assert.That(corners.Radius, Is.GreaterThan(0f));

            Canvas.ForceUpdateCanvases();
            Rect a = CanvasRect(corners.rectTransform, map.Canvas);
            Rect b = CanvasRect(map.ViewportRect, map.Canvas);
            Assert.That(Vector2.Distance(a.min, b.min), Is.LessThan(0.01f), "la caja de la zona del mapa");
            Assert.That(Vector2.Distance(a.max, b.max), Is.LessThan(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReloadingTheScene_WithTheMapAtRest_GivesBack60fps()
        {
            // La escena se va con el Mapa abierto (una recarga, como RebuildUi): su Close no llega, pero el intervalo de pintado,
            // que es global, vuelve a 1 al destruirse GameBootstrap, y el nivel siguiente no pinta a 30 fps.
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            map.Tick(RiverMapScreen.ActiveLingerMs + 1f);
            map.Tick(16f);
            Assert.That(RiverMapScreen.RestRenderInterval, Is.GreaterThan(1), "con el objetivo de 60 fps del juego");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(RiverMapScreen.RestRenderInterval), "el Mapa, en reposo");
            Assert.That(map.Visible, Is.True);

            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            Assert.That(GameBootstrap.Current!.RiverMapView!.Visible, Is.False);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "a 60 fps tras la recarga");
        }

        [UnityTest]
        public IEnumerator WithoutMapData_FromTheStart_TheLobbyStaysAsInWave2()
        {
            // La escena sin river_map_v1.json (GameSceneBuilder avisa: «el juego seguirá sin la pantalla del Mapa»): con
            // max_level ≥ 10 no hay pestaña, el Mapa no se abre por ninguna entrada, AF_LAYOUT no publica map_tab y no hay errores.
            using var layout = new AfLayoutLog();
            bool dropped = false;
            void DropTheMap(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
            {
                // sceneLoaded llega tras Awake y antes de Start: como si la escena no trajera la geometría.
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameBootstrap? b = root.GetComponentInChildren<GameBootstrap>(true);
                    if (b != null && b.RiverMapAsset != null)
                    {
                        b.ConfigureRiverMap(null);
                        dropped = true;
                    }
                }
            }

            var problems = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || type == LogType.Warning)
                {
                    problems.Add(type + ": " + message);
                }
            }

            Application.logMessageReceived += OnLog;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += DropTheMap;
            try
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
                GameBootstrap boot = GameBootstrap.Current!;
                LobbyScreen lobby = boot.Lobby!;
                yield return null;
                yield return null;
                Assert.That(dropped, Is.True, "la escena traía la geometría y se quitó antes de Start");
                Assert.That(boot.RiverMapAsset, Is.Null);
                Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(57), "max_level ≥ 10");
                Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
                Assert.That(boot.RiverMapAvailable, Is.False);
                Assert.That(lobby.MapTabShown, Is.False, "sin datos, sin pestaña");
                Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "ni se abre desde el inicio");
                Assert.That(boot.OpenRiverMap(MapOpenSource.Pause), Is.False, "ni por otra entrada");
                Assert.That(boot.RiverMapView!.Visible, Is.False);
                Assert.That(boot.RiverMapData, Is.Null, "no se intenta leer nada");
                string? lobbyLine = layout.LastOf("lobby", 58);
                Assert.That(lobbyLine, Is.Not.Null, "AF_LAYOUT del inicio");
                Assert.That(AfLayoutLog.ButtonIds(lobbyLine!), Does.Not.Contain("map_tab"));
                Assert.That(AfLayoutLog.ButtonIds(lobbyLine!), Does.Contain("play"), "el inicio de siempre, con «Jugar»");
                Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= DropTheMap;
                Application.logMessageReceived -= OnLog;
            }

            Assert.That(problems, Is.Empty, "ni errores ni avisos: el juego sigue como en la ola 2");
        }

        [UnityTest]
        public IEnumerator Play_ClosesTheMapAndOpensTheLevel()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.PlayButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.LevelNumber == 58, "el nivel 58");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "el nivel pinta a 60 fps");
        }

        [UnityTest]
        public IEnumerator BackArrowAndAndroidBack_CloseTheMap()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False, "el ← cierra");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.Lobby!.Visible, Is.True);
            yield return null;

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "ya está abierto");
            Assert.That(boot.PressAndroidBack(), Is.True);
            Assert.That(map.Visible, Is.False, "el Atrás de Android cierra");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.CloseRiverMap(), Is.False, "cerrado no hace nada");
        }

        [UnityTest]
        public IEnumerator BrokenGeometry_LogsTheError_HidesTheTab_AndTheMapNeverOpens()
        {
            // Una geometría que no valida (08 §4.5): se avisa con un error, la pestaña desaparece y el juego sigue sin el Mapa.
            using var layout = new AfLayoutLog();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(boot.RiverMapAvailable, Is.True);
            Assert.That(boot.RiverMapData, Is.Null, "la geometría se lee al abrir el Mapa por primera vez");
            Assert.That(lobby.MapTabShown, Is.True);

            boot.ConfigureRiverMap(new TextAsset("{}"));
            layout.Clear();
            LogAssert.Expect(LogType.Error, new Regex("El juego sigue sin el Mapa del río"));
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "no abre un Mapa que no valida");
            Assert.That(boot.RiverMapAvailable, Is.False);
            Assert.That(boot.RiverMapData, Is.Null);
            Assert.That(boot.RiverMapView!.Visible, Is.False);
            Assert.That(lobby.MapTabShown, Is.False, "la pestaña desaparece al momento");
            string? lobbyLine = layout.LastOf("lobby", 58);
            Assert.That(lobbyLine, Is.Not.Null, "AF_LAYOUT del inicio se reescribe al quitar la pestaña");
            Assert.That(AfLayoutLog.ButtonIds(lobbyLine!), Does.Not.Contain("map_tab"), "sin un botón que ya no está");

            // Ni vuelve, ni se reintenta la lectura (un segundo error haría fallar el test).
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False);
            lobby.MapTabButton.onClick.Invoke();
            Assert.That(boot.RiverMapView.Visible, Is.False);
            yield return null;
            yield return null;
            Assert.That(lobby.MapTabShown, Is.False, "el refresco del inicio no la saca de nuevo");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        [UnityTest]
        public IEnumerator OpenRiverMap_Refuses_WithSettings_OrTheStoreSheet_InFront()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;

            // Con Ajustes abierta (por encima del inicio), no se abre.
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            Assert.That(boot.SettingsView!.Visible, Is.True);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "con Ajustes delante");
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.CloseSettings(), Is.True);
            yield return null;

            // Con la hoja «Compra simulada» delante, tampoco.
            StoreSheet sheet = boot.StoreSheetView!;
            boot.Store!.Purchase(new PurchaseRequest(StoreCatalog.NoAds), r => { });
            Assert.That(sheet.Visible, Is.True);
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "con la hoja de compra delante");
            Assert.That(map.Visible, Is.False);
            sheet.CancelButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
            yield return null;

            // Sin nada delante, sí.
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.Visible, Is.True);
            Assert.That(boot.CloseRiverMap(), Is.True);
        }

        [UnityTest]
        public IEnumerator OpenRiverMap_Refuses_WithTheLockedProfileNoticeInFront()
        {
            // Perfil en solo lectura (08 §4.4) con max_level 58 (extra de desarrollo): el aviso «Actualiza Tintalú» va delante.
            LaunchOptions.DevMaxLevelOverride = 58;
            yield return PlayModeHelpers.LoadApp(new ReadOnlyProfileStore(ProfileLoadStatus.TooNew));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(boot.ProfileLocked, Is.True);
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(58));
            ProfileLockedNotice notice = boot.LockedNotice!;
            Assert.That(notice.Visible, Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.False, "con el aviso del perfil bloqueado delante");
            Assert.That(boot.RiverMapView!.Visible, Is.False);

            // Cerrado el aviso con su botón, el Mapa se abre (y se cierra sin guardar: el perfil es de solo lectura).
            notice.PlayButton.onClick.Invoke();
            Assert.That(notice.Visible, Is.False);
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(boot.RiverMapView.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator LeavingTheLobbyAnotherWay_ClosesTheMapFirst()
        {
            // El Mapa abierto desde la pestaña vive sobre el inicio: si la app sale del inicio por otro camino (aquí, el «Jugar»
            // del inicio sin pasar por el Mapa), se cierra antes, con su map_close y su map_seen_level.
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.Lobby));
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(map.Visible, Is.False, "el Mapa no se queda encima del nivel");
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "el nivel pinta a 60 fps");
            Assert.That(boot.Profile!.Progress.MapSeenLevel, Is.EqualTo(57), "se anotó lo visto al cerrarse");
            Assert.That(boot.CloseRiverMap(), Is.False, "ya estaba cerrado");
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.LevelNumber == 58, "el nivel 58");
            Assert.That(map.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator PointerDrag_OnTheViewport_MovesTheMapInDp_At60fps()
        {
            using var layout = new AfLayoutLog();
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            GameObject viewport = map.ViewportRect.gameObject;
            float pixelsPerDp = (GameScreen.SimulatedScreen.HasValue ? GameScreen.SimulatedScreen.Value.x : Screen.width) / map.CanvasWidth;
            Assert.That(pixelsPerDp, Is.EqualTo(3f).Within(0.01f), "1080 px en 360 dp");

            // En reposo, a 30 fps.
            map.Tick(RiverMapScreen.ActiveLingerMs + 1f);
            map.Tick(16f);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(RiverMapScreen.RestRenderInterval));
            Vector2 start = map.ContentOffset;

            // El arrastre del puntero (EventSystem → DragArea), con delta en píxeles de pantalla y y hacia arriba.
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = new Vector2(540f, 1200f) };
            Assert.That(ExecuteEvents.Execute(viewport, data, ExecuteEvents.beginDragHandler), Is.True, "la zona del mapa recibe el arrastre");
            Assert.That(map.Dragging, Is.True);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "a 60 fps desde que empieza");
            data.delta = new Vector2(-150f, 90f);
            Assert.That(ExecuteEvents.Execute(viewport, data, ExecuteEvents.dragHandler), Is.True);
            Assert.That(map.ContentOffset.x, Is.EqualTo(start.x + (data.delta.x / pixelsPerDp)).Within(0.01f), "−150 px = −50 dp");
            Assert.That(map.ContentOffset.y, Is.EqualTo(start.y - (data.delta.y / pixelsPerDp)).Within(0.01f), "+90 px hacia arriba = el mapa sube 30 dp");
            map.Tick(RiverMapScreen.ActiveLingerMs + 1f);
            map.Tick(16f);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "mientras se arrastra, aunque pase el tiempo");

            // Lejos, hasta sacar la canoa de la vista; al soltar, AF_LAYOUT publica «Volver a la canoa».
            data.delta = new Vector2(-300000f, -300000f);
            ExecuteEvents.Execute(viewport, data, ExecuteEvents.dragHandler);
            Assert.That(map.BackToCanoeShown, Is.True);
            layout.Clear();
            Assert.That(ExecuteEvents.Execute(viewport, data, ExecuteEvents.endDragHandler), Is.True);
            Assert.That(map.Dragging, Is.False);
            map.Tick(16f);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1), "un momento más a 60 tras soltar");
            map.Tick(RiverMapScreen.ActiveLingerMs + 1f);
            map.Tick(16f);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(RiverMapScreen.RestRenderInterval), "y vuelta a 30");
            string? line = layout.LastOf("river_map", 58);
            Assert.That(line, Is.Not.Null, "AF_LAYOUT al soltar con «Volver a la canoa» nuevo");
            Assert.That(AfLayoutLog.ButtonIds(line!), Does.Contain("back_to_canoe"));

            // «Volver a la canoa» la quita y AF_LAYOUT deja de publicarla.
            layout.Clear();
            map.BackToCanoeButton.onClick.Invoke();
            Assert.That(map.BackToCanoeShown, Is.False);
            line = layout.LastOf("river_map", 58);
            Assert.That(line, Is.Not.Null, "AF_LAYOUT tras «Volver a la canoa»");
            Assert.That(AfLayoutLog.ButtonIds(line!), Does.Not.Contain("back_to_canoe"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ViewSwitch_IsOneWoodCapsule_WithTheChosenTabInCream_AndNoGreen()
        {
            yield return OpenAt(58);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.ViewSwitch.color, Is.EqualTo(RiverMapScreen.SegmentSwitchColor));
            AssertTabs(map, MapView.World);
            map.ViewBasinButton.onClick.Invoke();
            AssertTabs(map, MapView.Basin);
            yield return null;

            // Las dos pestañas, dentro de la cápsula; la fila del pie es una sola banda con «Faltan…».
            Canvas.ForceUpdateCanvases();
            Rect capsule = CanvasRect(map.ViewSwitch.rectTransform, map.Canvas);
            Rect remaining = CanvasRect(map.StopRemainingRect, map.Canvas);
            foreach (Button b in new[] { map.ViewWorldButton, map.ViewBasinButton })
            {
                Rect r = CanvasRect((RectTransform)b.transform, map.Canvas);
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(capsule.xMin + 3.5f), b.name);
                Assert.That(r.xMax, Is.LessThanOrEqualTo(capsule.xMax - 3.5f), b.name);
                Assert.That(r.yMin, Is.EqualTo(remaining.yMin).Within(0.5f), b.name + ": mismo borde de arriba que «Faltan…»");
                Assert.That(r.yMax, Is.EqualTo(remaining.yMax).Within(0.5f), b.name + ": mismo borde de abajo");
            }

            Assert.That(capsule.yMin, Is.EqualTo(remaining.yMin).Within(0.5f));
            Assert.That(capsule.yMax, Is.EqualTo(remaining.yMax).Within(0.5f));
        }

        [Test]
        public void Texts_HaveAtLeast4_5To1()
        {
            // 07 §1.2 y §7.8: texto con contraste ≥ 4,5:1 sobre su fondo.
            foreach ((string name, Color text, Color background) in RiverMapScreen.ContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }
        }

        [UnityTest]
        public IEnumerator Canvases_AreThree_BelowTheStoreSheet_WithTheMeshesBuiltOnOpen()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Canvas.isRootCanvas, Is.True);
            Assert.That(map.Canvas.sortingOrder, Is.GreaterThan(40).And.LessThan(50), "sobre Ajustes y bajo la hoja de compra (50), el anuncio (60) y el aviso (70)");
            Assert.That(map.Canvas.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(3), "3 lienzos (propuesta del Mapa §11.4)");
            foreach (Canvas nested in new[] { map.StaticCanvas, map.DynamicCanvas })
            {
                Assert.That(nested.isRootCanvas, Is.False);
                Assert.That(nested.overrideSorting, Is.False, "anidado sin overrideSorting: lo recorta la máscara");
                Assert.That(nested.transform.IsChildOf(map.ViewportRect), Is.True);
            }

            Assert.That(map.ViewportRect.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(map.GlassCount, Is.EqualTo(boot.RiverMapData!.Lines.Count), "una cinta de vidrio por línea: toda la cuenca");
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.That(map.Land.LastVertexCount, Is.GreaterThan(0));
            for (int i = 0; i < map.GlassCount; i++)
            {
                Assert.That(map.Glass(i).Look, Is.EqualTo(RibbonLook.Glass));
                Assert.That(map.Glass(i).Line, Is.SameAs(boot.RiverMapData.Lines[i]));
                RiverLine line = boot.RiverMapData.Lines[i];
                if (line.PointCount >= 2)
                {
                    Assert.That(map.Glass(i).LastVertexCount, Is.GreaterThan(0), line.Id);
                }
            }
        }

        [UnityTest]
        public IEnumerator AtRest_ItDoesNotAllocate_AndRendersAt30fps()
        {
            yield return OpenAt(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return SnapshotKit.Wait(0.5f);
            Assert.That(map.Dragging, Is.False);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(RiverMapScreen.RestRenderInterval));
            if (Application.targetFrameRate == 60)
            {
                Assert.That(RiverMapScreen.RestRenderInterval, Is.EqualTo(2));
                Assert.That(OnDemandRendering.effectiveRenderFrameRate, Is.EqualTo(RiverMapScreen.PerfTargetFpsMenus), "perf_target_fps_menus = 30 (08 §9)");
            }

            map.Tick(16f);
            Assert.That(() => map.Tick(16f), Is.Not.AllocatingGCMemory(), "el Mapa quieto");
            LobbyScreen lobby = boot.Lobby!;
            lobby.Tick(16f);
            Assert.That(() => lobby.Tick(16f), Is.Not.AllocatingGCMemory(), "el inicio detrás del Mapa");
            IMapProgress progress = boot.Services!.MapProgress!;
            Assert.That(() => { _ = progress.MapSeenLevel + progress.MaxLevel; }, Is.Not.AllocatingGCMemory());

            // Arrastrando, a 60; al soltar, un momento más y vuelta a 30; al cerrar, a 60.
            map.Drag(new Vector2(-20f, 0f));
            map.Tick(16f);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1));
            map.Tick(RiverMapScreen.ActiveLingerMs + 1f);
            map.Tick(16f);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(RiverMapScreen.RestRenderInterval));
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(OnDemandRendering.renderFrameInterval, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Probes_WriteMapOpenAndMapClose()
        {
            var lines = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_", StringComparison.Ordinal))
                {
                    lines.Add(message);
                }
            }

            Application.logMessageReceived += OnLog;
            try
            {
                yield return OpenAt(58);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                map.ViewBasinButton.onClick.Invoke();
                yield return null;
                Assert.That(boot.CloseRiverMap(), Is.True);
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            Assert.That(lines.Count, Is.EqualTo(2), string.Join("\n", lines));
            StringAssert.Contains("\"event\":\"map_open\"", lines[0]);
            StringAssert.Contains("\"source\":\"lobby\"", lines[0]);
            StringAssert.Contains("\"level\":58", lines[0]);
            StringAssert.Contains("\"stop\":5", lines[0]);
            StringAssert.Contains("\"stop_id\":\"s05\"", lines[0]);
            StringAssert.Contains("\"world\":3", lines[0]);
            StringAssert.Contains("\"view\":\"world\"", lines[0]);
            StringAssert.Contains("\"event\":\"map_close\"", lines[1]);
            StringAssert.Contains("\"source\":\"lobby\"", lines[1]);
            StringAssert.Contains("\"dwell_ms\":", lines[1]);
            StringAssert.Contains("\"stops_tapped\":0", lines[1]);
            StringAssert.Contains("\"view_changes\":1", lines[1]);
            StringAssert.Contains("\"north_up\":false", lines[1]);
            StringAssert.Contains("\"map_seen_level\":57", lines[1]);
        }

        [UnityTest]
        public IEnumerator AfLayout_OfTheMapAndTheLobby_PublishTheirButtons()
        {
            using var layout = new AfLayoutLog();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            string? lobbyLine = layout.LastOf("lobby", 58);
            Assert.That(lobbyLine, Is.Not.Null);
            Assert.That(AfLayoutLog.ButtonIds(lobbyLine!), Does.Contain("map_tab"));
            Assert.That(AfLayoutLog.Button(lobbyLine!, "map_tab")!.Value, Is.EqualTo(AfLayoutLog.Published(boot.Lobby!.MapTabRect)));

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return null;
            string? line = layout.LastOf("river_map", 58);
            Assert.That(line, Is.Not.Null, "AF_LAYOUT del Mapa");
            string[] ids = AfLayoutLog.ButtonIds(line!);
            Assert.That(ids, Is.SupersetOf(new[] { "back", "view_world", "view_basin", "play", "stop_remaining" }));
            Assert.That(AfLayoutLog.Button(line!, "play")!.Value, Is.EqualTo(AfLayoutLog.Published((RectTransform)boot.RiverMapView!.PlayButton.transform)));
        }

        [UnityTest]
        public IEnumerator DevMaxLevel_SetsTheProgress_AndTheTabShows()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.LevelState = new LevelStateData { Level = 5 };
            Assert.That(LaunchOptions.ApplyDevProgress(profile, -1), Is.False, "−1 no toca nada");
            Assert.That(profile.LevelState, Is.Not.Null);
            Assert.That(LaunchOptions.ApplyDevProgress(profile, 58), Is.True);
            Assert.That(profile.Progress.MaxLevel, Is.EqualTo(58));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(59));
            Assert.That(profile.LevelState, Is.Null, "un nivel en curso de otro nivel se quita");
            Assert.That(LaunchOptions.ApplyDevProgress(profile, 5000), Is.True);
            Assert.That(profile.Progress.MaxLevel, Is.EqualTo(WorldTable.LastLevel));
            Assert.That(LaunchOptions.IntentExtraMaxLevel, Is.EqualTo("af_max_level"));

            LaunchOptions.DevMaxLevelOverride = 58;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(Wallet.NewProfile(new ManualClock(T0))));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.PlayLevel, Is.EqualTo(59));
            Assert.That(boot.Lobby!.MapTabShown, Is.True);
            Assert.That(boot.Lobby.MapTabDotShown, Is.True);
        }

        [UnityTest]
        public IEnumerator Layout_HasNoOverlaps_At360x640And360x780_AlsoAt130Percent()
        {
            foreach (float scale in new[] { 1f, 1.3f })
            {
                foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340) })
                {
                    LaunchOptions.FontScaleOverride = scale;
                    GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                    yield return OpenAt(61);
                    GameBootstrap boot = GameBootstrap.Current!;
                    RiverMapScreen map = boot.RiverMapView!;
                    map.Drag(new Vector2(-100000f, 0f)); // con «Volver a la canoa» a la vista
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    string where = size.x + " × " + size.y + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                    Assert.That(map.BackToCanoeShown, Is.True, where);
                    Assert.That(map.CheckShown, Is.True, where);

                    var named = new List<KeyValuePair<string, Rect>>
                    {
                        Named("←", (RectTransform)map.BackButton.transform, map.Canvas),
                        Named("título", map.TitleLabel.rectTransform, map.Canvas),
                        Named("✓", map.CheckRect, map.Canvas),
                        Named("mapa", map.ViewportRect, map.Canvas),
                        Named("mundo", (RectTransform)map.ViewWorldButton.transform, map.Canvas),
                        Named("cuenca", (RectTransform)map.ViewBasinButton.transform, map.Canvas),
                        Named("faltan", map.StopRemainingRect, map.Canvas),
                        Named("jugar", (RectTransform)map.PlayButton.transform, map.Canvas),
                    };
                    float w = map.CanvasWidth;
                    float h = map.CanvasHeight;
                    for (int i = 0; i < named.Count; i++)
                    {
                        Rect r = named[i].Value;
                        Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), where + ": " + named[i].Key);
                        Assert.That(r.xMax, Is.LessThanOrEqualTo(w + 0.5f), where + ": " + named[i].Key);
                        Assert.That(r.yMin, Is.GreaterThanOrEqualTo(map.SafeTop - 0.5f), where + ": " + named[i].Key + " bajo el recorte");
                        Assert.That(r.yMax, Is.LessThanOrEqualTo(h - map.SafeBottom + 0.5f), where + ": " + named[i].Key + " sobre la zona de gestos");
                        for (int j = i + 1; j < named.Count; j++)
                        {
                            Assert.That(Shrunk(named[i].Value).Overlaps(Shrunk(named[j].Value)), Is.False, where + ": " + named[i].Key + " y " + named[j].Key);
                        }
                    }

                    // El ←, el título y el ✓, dentro de la placa de la cabecera; el mapa empieza por debajo de ella.
                    Rect header = CanvasRect(map.HeaderRect, map.Canvas);
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.That(named[i].Value.yMax, Is.LessThanOrEqualTo(header.yMax + 0.5f), where + ": " + named[i].Key + " dentro de la cabecera");
                    }

                    Assert.That(named[3].Value.yMin, Is.GreaterThanOrEqualTo(header.yMax), where + ": el mapa bajo la cabecera");

                    Rect canoeButton = CanvasRect((RectTransform)map.BackToCanoeButton.transform, map.Canvas);
                    Assert.That(named[3].Value.Contains(canoeButton.min) && named[3].Value.Contains(canoeButton.max - new Vector2(0.5f, 0.5f)), Is.True, where + ": «Volver a la canoa» dentro del mapa");
                    Assert.That(canoeButton.height, Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f), where);
                    foreach (Button b in new[] { map.BackButton, map.ViewWorldButton, map.ViewBasinButton, map.PlayButton })
                    {
                        Rect r = CanvasRect((RectTransform)b.transform, map.Canvas);
                        Assert.That(Mathf.Min(r.width, r.height), Is.GreaterThanOrEqualTo(RiverMapScreen.MinTouchDp - 0.5f), where + ": " + b.name + " ≥ 48 dp");
                    }

                    // Ningún texto baja de 12 sp ni se sale de su caja, ni siquiera al 130 % (el criterio de L4: LocInventory).
                    var inventory = new LocInventory(LocLocale.SourceLocale, scale);
                    foreach (TextMeshProUGUI text in map.Texts())
                    {
                        Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), where + ": " + text.name);
                        Assert.That(inventory.ProblemsOf(text), Is.Empty, where + ": «" + text.text + "»");
                    }

                    // La pestaña del inicio no pisa nada del inicio.
                    Assert.That(boot.CloseRiverMap(), Is.True);
                    yield return null;
                    LobbyScreen lobby = boot.Lobby!;
                    Canvas.ForceUpdateCanvases();
                    Assert.That(lobby.MapTabShown, Is.True, where);
                    Rect tab = CanvasRect(lobby.MapTabRect, lobby.Canvas);
                    Assert.That(tab.xMax, Is.LessThanOrEqualTo(lobby.CanvasWidth + 0.5f), where);
                    Assert.That(tab.yMax, Is.LessThanOrEqualTo(lobby.CanvasHeight - lobby.SafeBottom + 0.5f), where);
                    Assert.That(Mathf.Min(tab.width, tab.height), Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f), where);
                    foreach (RectTransform other in new[] { lobby.HomeTabRect, (RectTransform)lobby.PlayButton.transform, lobby.StopRemainingRect, lobby.SeedsRect })
                    {
                        Assert.That(Shrunk(tab).Overlaps(Shrunk(CanvasRect(other, lobby.Canvas))), Is.False, where + ": la pestaña del Mapa y " + other.name);
                    }
                }
            }
        }

        // Abre la app en el inicio con «Jugar · Nivel playLevel» (max_level = playLevel − 1) y el Mapa desde su pestaña.
        private static IEnumerator OpenAt(int playLevel)
        {
            if (!GameScreen.SimulatedScreen.HasValue)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
            }

            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(playLevel)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(boot.Lobby!.MapTabShown, Is.True, "pestaña del Mapa con Jugar · Nivel " + playLevel);
            boot.Lobby.MapTabButton.onClick.Invoke();
            Assert.That(boot.RiverMapView!.Visible, Is.True, "el Mapa se abre desde la pestaña");
            yield return null;
        }

        // La pestaña elegida, con su placa crema y el texto en madera oscura; la otra, sin placa y en crema sobre la cápsula. Ninguna
        // en verde: el verde queda para «Jugar».
        private static void AssertTabs(RiverMapScreen map, MapView chosen)
        {
            foreach (MapView view in new[] { MapView.World, MapView.Basin })
            {
                bool selected = view == chosen;
                Image plate = map.ViewPlate(view);
                Assert.That(plate.enabled, Is.EqualTo(selected), view + ": placa");
                Assert.That(plate.color, Is.EqualTo(RiverMapScreen.SegmentSelectedPlate), view + ": color de la placa");
                Assert.That(map.ViewLabel(view).color, Is.EqualTo(selected ? RiverMapScreen.SegmentSelectedText : RiverMapScreen.SegmentText), view + ": texto");
                Assert.That(plate.color, Is.Not.EqualTo(Palette.FamilyLeaf.Deep), view + ": sin verde");
                Assert.That(plate.color, Is.Not.EqualTo(Palette.FamilyLeaf.Base), view + ": sin verde");
            }
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static KeyValuePair<string, Rect> Named(string name, RectTransform rt, Canvas canvas)
        {
            return new KeyValuePair<string, Rect>(name, CanvasRect(rt, canvas));
        }

        // Medio dp menos por lado: dos cajas que solo se tocan no se solapan.
        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }

        // Caja de rt en dp del lienzo, con el origen arriba a la izquierda.
        private static Rect CanvasRect(RectTransform rt, Canvas canvas)
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
