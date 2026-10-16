#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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
            Assert.That(map.Space.DpPerUnit, Is.EqualTo(MapSpace.BasinWidthDp).Within(0.01f), "1×: 328 dp, el ancho útil de 360 dp");
            Assert.That(map.ViewportSize.x, Is.EqualTo(MapSpace.BasinWidthDp).Within(0.5f));
            Assert.That(map.ContentSize.y, Is.LessThanOrEqualTo(map.ViewportSize.y + 0.5f), "a 360 × 780 la cuenca entera cabe");
            Assert.That(map.CanoeInView, Is.True);
            Assert.That(map.BackToCanoeShown, Is.False, "en la Cuenca no hace falta");
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
