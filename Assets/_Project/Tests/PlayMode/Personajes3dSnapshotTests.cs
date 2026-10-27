#nullable enable
using System;
using System.Collections;
using System.IO;
using AF.Audio;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del piloto de los personajes «3D cartoon, juguete brillante» (D-058; encargo
    /// <c>docs/dev/traspaso/spark-personajes-3d.md</c> §4, punto 5), las mismas antes y después del cambio, en
    /// <c>Logs/snapshots/personajes-3d/</c> (ignorada por git; se copian a mano a <c>docs/art/personajes-3d/piloto/antes|despues/</c>):
    /// <list type="bullet">
    /// <item><c>p3d_01_inicio</c> y <c>p3d_02_inicio_noche</c>: el muelle (D-050) con TiTino colgado y la canoa de Capi con Rosa;
    /// de noche, TiTino dormido;</item>
    /// <item><c>p3d_03_intro</c>: la intro «Llenar y teñir» del nivel 7 a 1.100 ms;</item>
    /// <item><c>p3d_04_mapa_viaje</c>: «Ver mapa» de la victoria del 20 a mitad del viaje de Capi (D-053);</item>
    /// <item><c>p3d_05_victoria</c>: el hito del 40 con TiTino a 1,6 s;</item>
    /// <item>cada escena también con <c>_ac</c>: con Alto contraste encendido (B9);</item>
    /// <item><c>p3d_10_lamina_titino</c>: las 10 poses del rig a 120 dp sobre la selva clara y el panel azul petróleo;</item>
    /// <item><c>p3d_11_lamina_personajes</c>: TiTino a 200 dp y su retrato, Capi en el banner (100 dp) y en la canoa del Mapa, la
    /// pata de Capi, Rosa y la mariposa morpho, en grande;</item>
    /// <item><c>p3d_12_siluetas</c>: las siluetas pequeñas (32 y 24 dp) sobre los dos fondos.</item>
    /// </list>
    /// Todas a 360 dp (1080 px de ancho, 3 px/dp), con el texto al 100 % y en es-419. Necesitan gráficos: <c>unity test &lt;repo&gt;
    /// --mode PlayMode --filter AF.Tests.PlayMode.Personajes3dSnapshotTests</c> sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas del piloto de personajes 3D (D-058); necesitan dispositivo gráfico.")]
    public sealed class Personajes3dSnapshotTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        private const float PxPerDp = 3f;
        private const float WidthDp = 360f;

        private Camera? _camera;
        private RenderTexture? _target;
        private GameObject? _canvasGo;
        private ProceduralArt? _art;
        private int _heightPx;

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
            HomeSloth.ManualTickForTests = true;
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            HomeSloth.ManualTickForTests = false;
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            SlothVictoryTests.NoSlot(false);
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
            OnDemandRendering.renderFrameInterval = 1;
            _art?.Dispose();
            _art = null;
            if (_canvasGo != null)
            {
                Object.Destroy(_canvasGo);
            }

            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            _canvasGo = null;
            _camera = null;
            _target = null;
        }

        /// <summary>Carpeta de las capturas del piloto.</summary>
        public static string Dir()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots/personajes-3d");
            Directory.CreateDirectory(dir);
            return dir;
        }

        [UnityTest]
        public IEnumerator CaptureDock([Values(false, true)] bool highContrast)
        {
            SnapshotKit.Prepare();
            string suffix = highContrast ? "_ac" : string.Empty;
            yield return Dock("p3d_01_inicio" + suffix, 31, 8, 400, highContrast);
            if (!highContrast)
            {
                yield return Dock("p3d_02_inicio_noche", 58, 22, 1600, false);
            }
        }

        [UnityTest]
        public IEnumerator CaptureIntro([Values(false, true)] bool highContrast)
        {
            SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            if (highContrast)
            {
                boot.ChooseA11y(SettingsScreen.HighContrastId, true);
            }

            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(1400, false);
            boot.View.Intro.Seek(1100);
            yield return SnapshotKit.Wait(0.2f);
            boot.View.Intro.Seek(1100);
            File.WriteAllBytes(Path.Combine(Dir(), "p3d_03_intro" + (highContrast ? "_ac" : string.Empty) + ".png"), SnapshotKit.Capture());
        }

        [UnityTest]
        public IEnumerator CaptureMapJourney([Values(false, true)] bool highContrast)
        {
            SnapshotKit.Prepare();
            yield return MapJourneyTests.OpenFromVictory20(boot =>
            {
                if (highContrast)
                {
                    boot.ChooseA11y(SettingsScreen.HighContrastId, true);
                }
            });
            GameBootstrap b = GameBootstrap.Current!;
            RiverMapScreen map = b.RiverMapView!;
            SnapshotKit.AttachOverlay(b.View!.Canvas);
            SnapshotKit.AttachOverlay(map.Canvas);
            Level10MapTests.Tick(map, PlayModeHelpers.FrameMs);
            Level10MapTests.Tick(map, RiverMapScreen.JourneyLeadMs + (map.CatchUpDurationMs / 2));
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(Dir(), "p3d_04_mapa_viaje" + (highContrast ? "_ac" : string.Empty));
        }

        [UnityTest]
        public IEnumerator CaptureVictory([Values(false, true)] bool highContrast)
        {
            FunnelSetup.Hooks();
            SlothVictoryTests.NoSlot(true);
            SnapshotKit.Prepare();
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.OneRowPack(40), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            if (highContrast)
            {
                boot.ChooseA11y(SettingsScreen.HighContrastId, true);
            }

            SnapshotKit.Attach(boot);
            for (int t = 0; t < 1600; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.TickExact(boot, Mathf.Min(PlayModeHelpers.FrameMs, 1600 - t));
            }

            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(Dir(), "p3d_05_victoria" + (highContrast ? "_ac" : string.Empty));
        }

        /// <summary>Las 10 poses del rig, a 120 dp, la columna izquierda sobre la selva clara y la derecha sobre el panel.</summary>
        [UnityTest]
        public IEnumerator CaptureTiTinoSheet()
        {
            RectTransform layer = PrepareSheet(780);
            float cellW = WidthDp / 2f;
            float cellH = 780f / 5f;
            Background(layer, 0f, 0f, cellW, 780f, Palette.JungleLight);
            Background(layer, cellW, 0f, cellW, 780f, Palette.Panel.Top);
            float[] poseMs = { 0f, 300f, 400f, 600f, 700f, 900f, 200f, 500f, 0f, 0f };
            float size = Mathf.Floor(cellH / 1.45f);
            SlothArt art = _art!.SlothRigArt()!;
            for (int i = 0; i < 10; i++)
            {
                SlothRig rig = SlothRig.Create(layer, art, size)!;
                rig.Place((cellW * (i % 2)) + (cellW * 0.5f), (cellH * (i / 2)) + (size * 0.5f));
                rig.Play((SlothPoseId)i, false);
                rig.Seek(poseMs[i]);
            }

            yield return null;
            SaveSheet("p3d_10_lamina_titino");
        }

        /// <summary>Cada personaje en grande, sobre la selva clara (arriba) y el panel azul petróleo (abajo).</summary>
        [UnityTest]
        public IEnumerator CaptureCharacterSheet()
        {
            RectTransform layer = PrepareSheet(780);
            Background(layer, 0f, 0f, WidthDp, 390f, Palette.JungleLight);
            Background(layer, 0f, 390f, WidthDp, 390f, Palette.Panel.Top);

            // TiTino a 190 dp y su retrato (56 dp, abierto y cerrado) a 2 ×.
            SlothArt sloth = _art!.SlothRigArt()!;
            SlothRig big = SlothRig.Create(layer, sloth, 190f)!;
            big.Place(110f, 20f);
            big.Play(SlothPoseId.Idle, false);
            Place(layer, _art.SlothPortrait(false)!, 238f, 20f, 112f, 112f);
            Place(layer, _art.SlothPortrait(true)!, 238f, 140f, 112f, 112f);

            // La mariposa morpho (alas y cuerpo) a 4 × su tamaño de vuelo y a 1,6 ×.
            Morpho(layer, 100f, 300f, 4f);
            Morpho(layer, 290f, 320f, 1.6f);

            // Capi en el banner (la escena de 320 × 150 dp, pintada aquí), la canoa del Mapa a 3 ×, la pata y Rosa a 1,5 ×.
            _art.RequestBannerArt(320f, 150f, Palette.Hex(0xB08A5A), null, 1, false);
            _art.TryTakeBannerArt();
            Sprite? scene = _art.BannerSceneSprite;
            if (scene != null)
            {
                Place(layer, scene, 20f, 400f, 320f, 150f);
            }

            Place(layer, _art.MapCanoe, 10f, 560f, ProceduralArt.MapCanoeWidthDp * 3f, ProceduralArt.MapCanoeHeightDp * 3f);
            Place(layer, _art.Rosa, 140f, 560f, ProceduralArt.RosaWidthDp * 2f, ProceduralArt.RosaHeightDp * 2f);
            Place(layer, _art.CapiPaw, 140f, 700f, ProceduralArt.CapiPawLengthDp * 1.5f, ProceduralArt.CapiPawThickDp * 1.5f);
            yield return null;
            SaveSheet("p3d_11_lamina_personajes");
        }

        /// <summary>Las siluetas a 32 y 24 dp (Liga y victoria) y a 48 dp, sobre la selva clara y el panel.</summary>
        [UnityTest]
        public IEnumerator CaptureSilhouettes()
        {
            RectTransform layer = PrepareSheet(260);
            Background(layer, 0f, 0f, WidthDp, 130f, Palette.JungleLight);
            Background(layer, 0f, 130f, WidthDp, 130f, Palette.Panel.Top);
            SlothArt sloth = _art!.SlothRigArt()!;
            for (int row = 0; row < 2; row++)
            {
                float top = (row * 130f) + 10f;
                float x = 10f;
                foreach (float s in new[] { 48f, 32f, 24f })
                {
                    SlothRig rig = SlothRig.Create(layer, sloth, s)!;
                    rig.Place(x + (0.75f * s), top + (0.12f * s));
                    rig.Play(SlothPoseId.Idle, false);
                    x += (1.5f * s) + 4f;
                }

                Place(layer, _art.SlothPortrait(false)!, x, top, 32f, 32f);
                Place(layer, _art.SlothPortrait(false)!, x + 36f, top, 24f, 24f);
                x += 66f;
                Place(layer, _art.MapCanoe, x, top, 40f, 32f);
                Place(layer, _art.MapCanoe, x + 44f, top, 30f, 24f);
                Place(layer, _art.Rosa, 10f, top + 60f, 48f, 48f * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp);
                Place(layer, _art.Rosa, 62f, top + 60f, 32f, 32f * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp);
                Morpho(layer, 130f, top + 80f, 0.42f);
                Morpho(layer, 175f, top + 80f, 0.3f);
            }

            yield return null;
            SaveSheet("p3d_12_siluetas");
        }

        // El inicio del nivel nextLevel a la hora hour (UTC), TiTino avanzado slothMs, con o sin Alto contraste.
        private static IEnumerator Dock(string name, int nextLevel, int hour, int slothMs, bool highContrast)
        {
            var clock = new ManualClock(Day0.AddHours(hour));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            HomeDockTests.UseLogbook();
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = 12345;
            profile.Wallet.Canoes.Count = 3;
            profile.Wallet.Canoes.NextRegenAt = clock.UtcNow + TimeSpan.FromMinutes(10);
            profile.Settings.HighContrast = highContrast;
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.SetUpMarket();
            yield return null;
            boot.TickMarket(16f);
            boot.TickDaily(16f);
            boot.TickLogbook(16f);
            boot.TickLeague(16f);
            LobbyScreen lobby = boot.Lobby!;
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            for (int t = 0; t < slothMs; t += 16)
            {
                sloth.Tick(16f);
            }

            lobby.Tick(16f);
            lobby.River.Seek(2600f);
            SnapshotKit.AttachLobby(boot);
            AttachCard(boot.DailyCard?.Root);
            AttachCard(boot.LogbookCard?.Root);
            if (boot.MarketIcons != null)
            {
                SnapshotKit.AttachOverlay(boot.MarketIcons.Canvas);
            }

            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(Dir(), name);
        }

        private static void AttachCard(RectTransform? card)
        {
            if (card != null)
            {
                SnapshotKit.AttachOverlay(card.GetComponentInParent<Canvas>().rootCanvas);
            }
        }

        // La mariposa de vuelo (alas y cuerpo superpuestos) centrada en (x, yDown) dp, a scale veces su tamaño (3 px/dp).
        private void Morpho(RectTransform layer, float x, float yDown, float scale)
        {
            Sprite wings = _art!.MorphoWingsSprite;
            Sprite body = _art.MorphoBodySprite;
            float ww = wings.rect.width / PxPerDp * scale;
            float wh = wings.rect.height / PxPerDp * scale;
            float bw = body.rect.width / PxPerDp * scale;
            float bh = body.rect.height / PxPerDp * scale;
            Place(layer, wings, x - (ww * 0.5f), yDown - (wh * 0.5f), ww, wh);
            Place(layer, body, x - (bw * 0.5f), yDown - (bh * 0.5f), bw, bh);
        }

        private static void Place(RectTransform layer, Sprite sprite, float x, float yDown, float w, float h)
        {
            var go = new GameObject("Piece", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(layer, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yDown);
            rt.sizeDelta = new Vector2(w, h);
        }

        // Lienzo de 360 × alto dp a 3 px/dp con su cámara; devuelve la capa (origen arriba a la izquierda).
        private RectTransform PrepareSheet(int heightDp)
        {
            ArtSlots.ForceProcedural = true;
            _art = new ProceduralArt("white");
            _heightPx = Mathf.RoundToInt(heightDp * PxPerDp);
            int widthPx = Mathf.RoundToInt(WidthDp * PxPerDp);
            _target = new RenderTexture(widthPx, _heightPx, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("P3dSnapshotCamera");
            _camera = camGo.AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.orthographic = true;
            _camera.enabled = false;
            _canvasGo = new GameObject("P3dSnapshotCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = _canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = PxPerDp;
            var layerGo = new GameObject("Layer", typeof(RectTransform));
            layerGo.transform.SetParent(_canvasGo.transform, false);
            var layer = (RectTransform)layerGo.transform;
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.sizeDelta = Vector2.zero;
            return layer;
        }

        private static void Background(RectTransform layer, float x, float yDown, float w, float h, Color color)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(layer, false);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = color;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yDown);
            rt.sizeDelta = new Vector2(w, h);
            rt.SetAsFirstSibling();
        }

        private void SaveSheet(string name)
        {
            Canvas.ForceUpdateCanvases();
            _camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            var tex = new Texture2D(_target!.width, _heightPx, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _target.width, _heightPx), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(Dir(), name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
