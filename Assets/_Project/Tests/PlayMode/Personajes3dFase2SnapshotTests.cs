#nullable enable
using System;
using System.Collections;
using System.IO;
using AF.Audio;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.LiveOps;
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
    /// Capturas de la fase 2 de los personajes «3D cartoon, juguete brillante» (D-058; encargo
    /// <c>docs/dev/traspaso/spark-personajes-3d.md</c> §5), las mismas antes y después del cambio, en
    /// <c>Logs/snapshots/personajes-3d-fase2/</c> (ignorada por git; se copian a mano a
    /// <c>docs/art/personajes-3d/fase2/antes|despues/</c>):
    /// <list type="bullet">
    /// <item><c>f2_01_fauna</c>: los 11 miquitos del catálogo (y la hembra del saquí) sentados a 84 dp, la piraña despierta y
    /// dormida, la luciérnaga en vuelo, Rosa y el bufeo, en grande, sobre la selva clara y el panel azul petróleo;</item>
    /// <item><c>f2_02_avatares</c>: los 16 avatares a 56 dp, normales y bloqueados, y a 32 dp (Liga) y 24 dp (victoria), a su
    /// tamaño real de celda (3 px/dp), sobre el panel y la selva clara;</item>
    /// <item><c>f2_03_intro_tropa</c>: la intro del nivel 2 con la tropa, en el reposo (un gesto en su pico);</item>
    /// <item><c>f2_04_intro_churuco</c>: el reposo colgado del churuco (nivel 82);</item>
    /// <item><c>f2_05_mapa_bufeo</c>: el Mapa con el bufeo boliviano guiando la canoa (mundo 7, max 305);</item>
    /// <item><c>f2_06_victoria_avatar</c>: la victoria del 41 con el avatar en la línea de la Liga (24 dp);</item>
    /// <item><c>f2_07_liga</c>: la Liga con el avatar de cada fila (32 dp);</item>
    /// <item><c>f2_08_delfin</c>: el delfín de río (Rosa y el bufeo) en grande, en el banner con la onda, la espuma y el soplo, y
    /// a su tamaño en el Mapa (la onda, opaca y a escala 1: solo sale así al asomar);</item>
    /// <item><c>f2_08b_delfin_reposo</c>: el banner en reposo, con solo la espuma sobre el corte (sin la onda ni el soplo);</item>
    /// <item>las escenas también con <c>_ac</c>: con Alto contraste encendido (B9).</item>
    /// </list>
    /// Todas a 360 dp (1080 px de ancho, 3 px/dp), con el texto al 100 % y en es-419. Necesitan gráficos: <c>unity test &lt;repo&gt;
    /// --mode PlayMode --filter AF.Tests.PlayMode.Personajes3dFase2SnapshotTests</c> sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas de la fase 2 de personajes 3D (D-058); necesitan dispositivo gráfico.")]
    public sealed class Personajes3dFase2SnapshotTests
    {
        private const float PxPerDp = 3f;
        private const float WidthDp = 360f;

        private static readonly AvatarArtBackdrop[] Backdrops =
        {
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water, AvatarArtBackdrop.Wood,
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Wood, AvatarArtBackdrop.Water,
            AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Wood,
            AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Leaf, AvatarArtBackdrop.Petroleum, AvatarArtBackdrop.Water,
        };

        private Camera? _camera;
        private RenderTexture? _target;
        private GameObject? _canvasGo;
        private ProceduralArt? _art;
        private int _heightPx;
        private readonly System.Collections.Generic.List<Object> _owned = new System.Collections.Generic.List<Object>();

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            GameBootstrap.ResetAvatarOverrides();
            GameBootstrap.ResetLeagueOverrides();
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
            OnDemandRendering.renderFrameInterval = 1;
            _art?.Dispose();
            _art = null;
            foreach (Object o in _owned)
            {
                Object.Destroy(o);
            }

            _owned.Clear();
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

        /// <summary>Carpeta de las capturas de la fase 2.</summary>
        public static string Dir()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots/personajes-3d-fase2");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Toda la fauna que no es TiTino ni Capi, en grande.</summary>
        [UnityTest]
        public IEnumerator CaptureFaunaSheet()
        {
            const float sheetH = 900f;
            RectTransform layer = PrepareSheet((int)sheetH);
            const float rowH = 160f;
            for (int row = 0; row < 4; row++)
            {
                Background(layer, 0f, row * rowH, WidthDp, rowH, row % 2 == 0 ? Palette.JungleLight : Palette.Panel.Top);
            }

            Background(layer, 0f, 4f * rowH, WidthDp, sheetH - (4f * rowH), Palette.Panel.Top);
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            int n = 0;
            foreach (FaunaSpecies sp in catalog.Species)
            {
                PlaceMonkey(layer, sp, false, n++);
                if (sp.PairPalette != null)
                {
                    PlaceMonkey(layer, sp, true, n++);
                }
            }

            // Abajo: la piraña despierta y dormida (a 1,5 × su tamaño de 64 dp por u), la luciérnaga en vuelo (2 ×), Rosa y el
            // bufeo (3 ×).
            float y = (4f * rowH) + 12f;
            Sprite fish = Internal(_art!, "PiranaFishSprite");
            Sprite asleep = Internal(_art, "PiranaFishAsleepSprite");
            float fw = fish.rect.width / PxPerDp * 1.5f;
            float fh = fish.rect.height / PxPerDp * 1.5f;
            Place(layer, fish, 10f, y, fw, fh);
            Place(layer, asleep, 10f, y + fh + 10f, fw, fh);
            Sprite wings = _art.FireflyWingsSprite;
            Sprite body = _art.FireflyBodySprite;
            float ww = wings.rect.width / PxPerDp * 1.4f;
            float wh = wings.rect.height / PxPerDp * 1.4f;
            float bw = body.rect.width / PxPerDp * 1.4f;
            float bh = body.rect.height / PxPerDp * 1.4f;
            float fx = fw + 70f;
            float fy = y + 60f;
            Place(layer, wings, fx - (ww * 0.5f), fy - (wh * 0.5f), ww, wh);
            Place(layer, body, fx - (bw * 0.5f), fy - (bh * 0.5f), bw, bh);
            Place(layer, _art.Rosa, 10f, y + (2f * fh) + 30f, ProceduralArt.RosaWidthDp * 1.6f, ProceduralArt.RosaHeightDp * 1.6f);
            Sprite bufeo = _art.MapBufeo;
            Place(layer, bufeo, 200f, y + 150f, bufeo.rect.width / PxPerDp * 4f, bufeo.rect.height / PxPerDp * 4f);
            yield return null;
            yield return null;
            SaveSheet("f2_01_fauna");
        }

        /// <summary>Los 16 avatares, normales y bloqueados, a 64, 32 y 24 dp.</summary>
        [UnityTest]
        public IEnumerator CaptureAvatarSheet()
        {
            const float sheetH = 944f;
            RectTransform layer = PrepareSheet((int)sheetH);
            Background(layer, 0f, 0f, WidthDp, 672f, Palette.Panel.Top);
            Background(layer, 0f, 672f, WidthDp, sheetH - 672f, Palette.JungleLight);
            AvatarArtBadge[] badges = { AvatarArtBadge.World, AvatarArtBadge.Hard, AvatarArtBadge.VeryHard, AvatarArtBadge.Streak, AvatarArtBadge.Daily, AvatarArtBadge.League };

            // Arriba, en grande (56 dp): los 16 normales y los 16 bloqueados, 6 por fila.
            for (int block = 0; block < 2; block++)
            {
                bool locked = block == 1;
                for (int i = 0; i < ProceduralArt.AvatarArtCount; i++)
                {
                    float x = 8f + ((i % 6) * 58f);
                    float y = 8f + (block * 196f) + ((i / 6) * 62f);
                    PlaceAvatar(layer, i, locked, locked ? badges[i % badges.Length] : AvatarArtBadge.None, 56, x, y);
                }
            }

            // A su tamaño real, sobre el panel (arriba) y sobre la selva clara (abajo): 32 dp (la Liga), 8 por fila, y 24 dp (la
            // victoria), 12 por fila; normales y bloqueados.
            for (int bg = 0; bg < 2; bg++)
            {
                float top = bg == 0 ? 404f : 680f;
                for (int block = 0; block < 2; block++)
                {
                    bool locked = block == 1;
                    for (int i = 0; i < ProceduralArt.AvatarArtCount; i++)
                    {
                        AvatarArtBadge badge = locked ? badges[i % badges.Length] : AvatarArtBadge.None;
                        PlaceAvatar(layer, i, locked, badge, 32, 8f + ((i % 8) * 36f), top + (block * 72f) + ((i / 8) * 36f));
                        PlaceAvatar(layer, i, locked, badge, 24, 8f + ((i % 12) * 28f), top + 148f + (block * 56f) + ((i / 12) * 28f));
                    }
                }
            }

            yield return null;
            SaveSheet("f2_02_avatares");
        }

        /// <summary>
        /// El delfín de río (petición del dueño del 2026-10-27: «mejor forma, sin ese arco»): Rosa y el bufeo en grande; Rosa en el
        /// banner (la línea del agua deja ver el 74 % de su alto, con la onda, la espuma sobre el corte y el soplo en el
        /// respiradero); y los dos a su tamaño real en el Mapa (22 dp en la vista del mundo, 16 dp en la de la cuenca) y a 32 dp,
        /// sobre el verde del Mapa y el agua.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureDolphinSheet()
        {
            const float sheetH = 560f;
            RectTransform layer = PrepareSheet((int)sheetH);
            Background(layer, 0f, 0f, WidthDp, 160f, Palette.JungleLight);
            Background(layer, 0f, 160f, WidthDp, 200f, Palette.Panel.Top);
            Background(layer, 0f, 360f, WidthDp, 100f, MapGreen);
            Background(layer, 0f, 460f, WidthDp, 100f, MapWater);
            Sprite rosa = _art!.Rosa;
            Sprite bufeo = _art.MapBufeo;
            const float rw = ProceduralArt.RosaWidthDp * 1.4f;
            const float rh = ProceduralArt.RosaHeightDp * 1.4f;
            Place(layer, rosa, 6f, 8f, rw, rh);
            Place(layer, bufeo, 6f + rw + 12f, 8f + (rh * 0.15f), rw * 0.82f, rh * 0.82f);

            // El banner: la escena del agua (abajo, oscura), Rosa con el 74 % de su alto fuera, la onda, la espuma y el soplo.
            float waterline = 160f + 130f;
            float bx = 30f;
            float above = 0.74f * ProceduralArt.RosaHeightDp;
            Place(layer, rosa, bx, waterline - above, ProceduralArt.RosaWidthDp, ProceduralArt.RosaHeightDp);
            Background(layer, 0f, waterline, WidthDp, 70f, MapWater);
            layer.GetChild(0).SetAsLastSibling(); // el agua tapa la parte de Rosa bajo la línea
            Place(layer, _art.RosaRipple, bx + (ProceduralArt.RosaWidthDp * 0.5f) - (ProceduralArt.RosaRippleWidthDp * 0.5f), waterline - (ProceduralArt.RosaRippleHeightDp * 0.5f), ProceduralArt.RosaRippleWidthDp, ProceduralArt.RosaRippleHeightDp);
            float foamX = bx + (ProceduralArt.RosaCutCenter * ProceduralArt.RosaWidthDp);
            Place(layer, _art.RosaFoam, foamX - (ProceduralArt.RosaFoamWidthDp * 0.5f), waterline - (ProceduralArt.RosaFoamHeightDp * 0.5f), ProceduralArt.RosaFoamWidthDp, ProceduralArt.RosaFoamHeightDp);
            Vector2 blow = ProceduralArt.RosaBlowhole;
            float sx = bx + (blow.x * ProceduralArt.RosaWidthDp);
            float sy = waterline - above + ((1f - blow.y) * ProceduralArt.RosaHeightDp);
            Place(layer, _art.RosaSpray, sx - (ProceduralArt.RosaSprayDp * 0.5f), sy - ProceduralArt.RosaSprayDp, ProceduralArt.RosaSprayDp, ProceduralArt.RosaSprayDp);

            // A su tamaño en el Mapa: 32, 22 y 16 dp de ancho, Rosa y el bufeo, sobre el verde y sobre el agua.
            for (int row = 0; row < 2; row++)
            {
                float top = row == 0 ? 380f : 480f;
                float x = 12f;
                foreach (float w in new[] { 32f, 22f, 16f })
                {
                    float h = w * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp;
                    Place(layer, rosa, x, top + 30f - h, w, h);
                    Place(layer, bufeo, x + w + 8f, top + 30f - h, w, h);
                    x += (2f * w) + 30f;
                }
            }

            yield return null;
            SaveSheet("f2_08_delfin");
        }

        /// <summary>
        /// El banner en reposo (revisión de la fase 2): Rosa con <see cref="WorldBannerView.RosaAbove"/> de su alto fuera del agua y
        /// solo la espuma sobre el corte, sin la onda ni el soplo (en reposo los dos están a alfa 0), a 2 × y a 1 ×: la espuma
        /// tiene que tapar el corte entero.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureDolphinRest()
        {
            const float sheetH = 300f;
            RectTransform layer = PrepareSheet((int)sheetH);
            Background(layer, 0f, 0f, WidthDp, sheetH, Palette.Panel.Top);
            Sprite rosa = _art!.Rosa;
            Sprite foam = _art.RosaFoam;
            float[] scales = { 2f, 1f };
            float[] xs = { 20f, 250f };
            float waterline = 200f;
            for (int i = 0; i < 2; i++)
            {
                float k = scales[i];
                float w = ProceduralArt.RosaWidthDp * k;
                float h = ProceduralArt.RosaHeightDp * k;
                Place(layer, rosa, xs[i], waterline - (WorldBannerView.RosaAbove * h), w, h);
            }

            Background(layer, 0f, waterline, WidthDp, sheetH - waterline, MapWater);
            layer.GetChild(0).SetAsLastSibling(); // el agua tapa la parte de Rosa bajo la línea
            for (int i = 0; i < 2; i++)
            {
                float k = scales[i];
                float fw = ProceduralArt.RosaFoamWidthDp * k;
                float fh = ProceduralArt.RosaFoamHeightDp * k;
                float fx = xs[i] + (ProceduralArt.RosaCutCenter * ProceduralArt.RosaWidthDp * k);
                Place(layer, foam, fx - (fw * 0.5f), waterline - (fh * 0.5f), fw, fh);
            }

            yield return null;
            SaveSheet("f2_08b_delfin_reposo");
        }

        // El verde de tierra y el agua del Mapa (aproximados, para la lámina).
        private static readonly Color MapGreen = Palette.Hex(0x2F7D4E);
        private static readonly Color MapWater = Palette.Hex(0x5E9FA8);

        [UnityTest]
        public IEnumerator CaptureIntroTroop([Values(false, true)] bool highContrast)
        {
            SnapshotKit.Prepare();
            yield return IntroAt(2, 1400, highContrast, "f2_03_intro_tropa");
            if (!highContrast)
            {
                LaunchOptions.Reset();
                string pack = PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82);
                yield return PlayModeHelpers.LoadGameWithPack(pack, 82, new MemoryProgressStore(82, 81));
                yield return Rest(GameBootstrap.Current!, 1400, false, "f2_04_intro_churuco");
            }
        }

        [UnityTest]
        public IEnumerator CaptureMapBufeo([Values(false, true)] bool highContrast)
        {
            SnapshotKit.Prepare();
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            if (highContrast)
            {
                boot.ChooseA11y(SettingsScreen.HighContrastId, true);
            }

            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(306, 305, 305), 306);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(Dir(), "f2_05_mapa_bufeo" + (highContrast ? "_ac" : string.Empty));
        }

        [UnityTest]
        public IEnumerator CaptureVictoryAvatar([Values(false, true)] bool highContrast)
        {
            var clock = new ManualClock(new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc));
            UseLeague(clock);
            FunnelSetup.Hooks();
            SnapshotKit.Prepare();
            LaunchOptions.LevelOffsetOverride = 40;
            Profile profile = VictorySetup.Profile(100);
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            profile.Settings.HighContrast = highContrast;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            if (highContrast)
            {
                boot.ChooseA11y(SettingsScreen.HighContrastId, true);
            }

            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            for (int t = 0; t < 1600; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.TickExact(boot, Mathf.Min(PlayModeHelpers.FrameMs, 1600 - t));
            }

            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(Dir(), "f2_06_victoria_avatar" + (highContrast ? "_ac" : string.Empty));
        }

        [UnityTest]
        public IEnumerator CaptureLeague([Values(false, true)] bool highContrast)
        {
            var clock = new ManualClock(new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc));
            UseLeague(clock);
            SnapshotKit.Prepare();
            LaunchOptions.ClockOverride = clock;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 145;
            profile.Progress.NextLevel = 146;
            profile.Wallet.Seeds = 2450;
            profile.Settings.HighContrast = highContrast;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.League!.RecordCampaignWin(57, RewardTier.Normal);
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenLeague(), Is.True);
            SnapshotKit.AttachOverlay(boot.LeagueView!.Canvas);
            boot.LeagueAvatarAtlasNow?.PaintAll();
            for (int i = 0; i < 20; i++)
            {
                boot.TickAvatars(16f);
            }

            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(Dir(), "f2_07_liga" + (highContrast ? "_ac" : string.Empty));
        }

        // Los sprites internos de la piraña (la mecánica los pide por dentro de AF.Presentation).
        private static Sprite Internal(ProceduralArt art, string property)
        {
            var info = typeof(ProceduralArt).GetProperty(property, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            return (Sprite)info!.GetValue(art)!;
        }

        private static void UseLeague(ManualClock clock)
        {
            GameBootstrap.LeagueApiOverride = new FakeLeagueApi(clock) { StartPoints = 140, MyAvatar = 1 };
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            GameBootstrap.AvatarPickOverride = n => 1; // el tucán
        }

        private static IEnumerator IntroAt(int level, int duration, bool highContrast, string name)
        {
            LaunchOptions.Reset();
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            yield return Rest(GameBootstrap.Current!, duration, highContrast, name);
        }

        // El reposo de la tropa (la última suelta + medio ciclo de gestos: cada gesto en su pico), como 24_troop_*_3_reposo.
        private static IEnumerator Rest(GameBootstrap boot, int duration, bool highContrast, string name)
        {
            if (highContrast)
            {
                boot.ChooseA11y(SettingsScreen.HighContrastId, true);
            }

            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(duration, false);
            IntroChoreography p = boot.View.Intro.Plan;
            float peak = p.LastReleaseMs + (0.5f * MonkeyRig.GestureCycleMs);
            boot.View.Intro.Seek(peak);
            yield return SnapshotKit.Wait(0.2f);
            boot.View.Intro.Seek(peak);
            SnapshotKit.Save(Dir(), name + (highContrast ? "_ac" : string.Empty));
        }

        private void PlaceMonkey(RectTransform layer, FaunaSpecies sp, bool pair, int n)
        {
            const float size = 84f;
            MonkeyArt art = _art!.MonkeyArtFor(sp, pair);
            var rig = new MonkeyRig(layer, n);
            rig.SetArt(art, size);
            rig.SetVisible(true);
            rig.SetAlpha(1f);
            float x = 60f + ((n % 3) * 120f);
            float y = 150f + ((n / 3) * 160f);
            rig.Place(x, y, 1f);
            rig.Apply(MonkeyRig.Sit(art, 1f, 0f, -1, 0f));
        }

        private void PlaceAvatar(RectTransform layer, int index, bool locked, AvatarArtBadge badge, int dp, float x, float yDown)
        {
            int px = Mathf.RoundToInt(dp * PxPerDp);
            Color32[] pixels = ProceduralArt.AvatarPixels(index, Backdrops[index], badge, locked, px);
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f);
            _owned.Add(tex);
            _owned.Add(sprite);
            Place(layer, sprite, x, yDown, dp, dp);
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
            var camGo = new GameObject("P3dF2SnapshotCamera");
            _camera = camGo.AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.orthographic = true;
            _camera.enabled = false;
            _canvasGo = new GameObject("P3dF2SnapshotCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
