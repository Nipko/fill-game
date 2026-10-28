#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AF.Audio;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de teléfono de la ficha de Google Play de la 1.0 (07 §10.2; <c>docs/store-listing/assets-tienda.md</c> §4; D-052:
    /// el arte por código es el arte final y vale para la ficha), con el juego real en es-419, en y pt-BR, en
    /// <c>docs/store-listing/graficos/&lt;idioma&gt;/NN-&lt;escena&gt;.png</c> (1080 × 1920 px, vertical):
    /// <list type="bullet">
    /// <item><c>01</c> a <c>08</c>: las ocho de 07 §10.2, cada una con la pantalla de hoy que mejor la cumple y su texto corto sobre
    /// la placa de madera del kit de UI (Q-B13-5 (a)); el texto se lee de la tabla §6 de <c>docs/store-listing/&lt;idioma&gt;/ficha.md</c>
    /// (no hay claves de <c>strings.csv</c> para él). El juego va a 360 × 640 dp (16:9) reducido bajo la placa, con un marco de
    /// madera sobre el petróleo de la marca (D-045);</item>
    /// <item><c>09-mercado</c> y <c>10-liga</c>: extras sin texto (07 §10.2 no les da hueco), la pantalla de 16:9 a sangre.</item>
    /// </list>
    /// Sin datos de depuración ni anuncios: solo se pintan los lienzos del juego que se enganchan a la cámara de las capturas.
    /// Hora, perfil, idioma y pantalla fijos. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.StoreListingSnapshotTests</c> sin <c>-nographics</c> (ver <c>docs/store-listing/graficos/README.md</c>).
    /// </summary>
    [Explicit("Capturas de la ficha de Play; necesitan dispositivo gráfico y escriben en docs/store-listing/graficos.")]
    [Category("StoreListing")]
    public sealed class StoreListingSnapshotTests
    {
        /// <summary>Alto de la pantalla simulada en 16:9 (px).</summary>
        private const int ScreenPx = 1920;

        /// <summary>Petróleo de la marca (D-045; <c>exportar.py</c>, <c>PETROLEO</c>).</summary>
        private static readonly Color Petroleum = new Color32(0x14, 0x44, 0x4A, 0xFF);

        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        private const int Acai = 8; // ProceduralArt.Fruit: «acai» es el color 8

        private readonly List<Object> _owned = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
            HomeSloth.ManualTickForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Object.Destroy(_owned[i]);
                }
            }

            _owned.Clear();
            HomeSloth.ManualTickForTests = false;
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetDailyOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.StreakStoreOverride = null;
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        // ------------------------------------------------------------------------------------------------- 01 a 08 (07 §10.2)

        /// <summary>1: una cuia vierte Açaí a mitad del chorro en el Encuentro de las Aguas (parada 41-50, Manaos).</summary>
        [UnityTest]
        public IEnumerator Shot01_PourInManaos([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            yield return LoadPlaying(42, null);
            GameBootstrap boot = GameBootstrap.Current!;
            PourMidway(boot, Acai, 40);
            yield return Save(dir, 1, "vertido-encuentro", boot.View!.Canvas);
        }

        /// <summary>2: tablero del Estuario con 5 cuias y los patrones de fruta a la vista (ajuste «Siempre»).</summary>
        [UnityTest]
        public IEnumerator Shot02_EstuaryBoard([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            Profile profile = FunnelSetup.Profile(240);
            profile.Settings.Patterns = PatternsSetting.On;
            yield return LoadPlaying(6, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.Views.Length, Is.EqualTo(5), "5 cuias");
            Assert.That(boot.View.FruitPatternsOn, Is.True, "patrones a la vista");
            yield return Save(dir, 2, "estuario", boot.View.Canvas);
        }

        /// <summary>3: «Llenar y teñir» a mitad, con las frutas cayendo (Alter do Chão).</summary>
        [UnityTest]
        public IEnumerator Shot03_FillAndDye([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(25, new MemoryProgressStore(25, 24));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.View!.DebugReplayIntro(1400, false);
            boot.View.Intro.Seek(1050);
            yield return Save(dir, 3, "llenar-y-tenir", boot.View.Canvas, false);
        }

        /// <summary>4: el Mapa del río en la vista del mundo 3, con la canoa a 7/10 del tramo de Manaos a Anavilhanas (próximo nivel, el 58).</summary>
        [UnityTest]
        public IEnumerator Shot04_RiverMap([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            var clock = new ManualClock(Day0.AddHours(15));
            LaunchOptions.ClockOverride = clock;
            Profile profile = Wallet.NewProfile(clock);
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
            yield return SnapshotKit.Wait(0.3f);
            // La vista del mundo: a 16:9, la de la cuenca amontona las placas de los mundos y la ficha de casa (revisión de las
            // capturas del 2026-10-28); aquí solo asoma cortada, arriba, la línea del premio del mundo.
            Assert.That(map.View, Is.EqualTo(MapView.World));
            yield return Save(dir, 4, "mapa-del-rio", map.Canvas);
        }

        /// <summary>5: un nivel Difícil (el 58, con su chip y su cabecera ámbar) a media partida.</summary>
        [UnityTest]
        public IEnumerator Shot05_HardLevel([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            yield return LoadPlaying(58, null);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayMoves(boot, 4);
            yield return Save(dir, 5, "nivel-dificil", boot.View!.Canvas);
        }

        /// <summary>6: un tablero grande con los patrones de fruta (automáticos desde el 41) a media partida (Tefé).</summary>
        [UnityTest]
        public IEnumerator Shot06_Patterns([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            yield return LoadPlaying(75, null);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.FruitPatternsOn, Is.True, "patrones a la vista");
            PlayMoves(boot, 5);
            yield return Save(dir, 6, "patrones", boot.View.Canvas);
        }

        /// <summary>7: TiTino en su rama del muelle del río (D-050), con su saludo de la mañana.</summary>
        [UnityTest]
        public IEnumerator Shot07_TiTino([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            yield return Dock(31, 8, 400);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return Save(dir, 7, "titino-muelle", boot.Lobby!.Canvas);
        }

        /// <summary>8: el Reto del río ganado: el calendario del mes con el sello del día y su premio.</summary>
        [UnityTest]
        public IEnumerator Shot08_DailyChallenge([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            var monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá
            var clock = new ManualClock(DateTime.SpecifyKind(monday - Bogota, DateTimeKind.Utc), Bogota);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            var record = new DailyChallengeRecord { LastClaimedDate = "2026-10-18", StreakDays = 3, StampsMonth = "2026-10" };
            int[] earlier = { 2, 3, 5, 9, 12, 15, 16, 17, 18 };
            for (int i = 0; i < earlier.Length; i++)
            {
                record.StampDays.Add(earlier[i]);
            }

            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore(record);
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 45;
            profile.Progress.NextLevel = 46;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True, "E2");
            boot.DailyRunner!.enabled = false;
            DailyStep(boot, PlayModeHelpers.FrameMs);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.DailyScreen!.Calendar.Canvas);
            HomeSloth? sloth = boot.Lobby?.HomeWindow.Sloth;
            if (sloth != null)
            {
                for (int t = 0; t < 400 + (int)sloth.BubbleDurationMs + 300; t += 16)
                {
                    sloth.Tick(16f);
                }
            }

            boot.TickDaily(16f);
            Assert.That(boot.PressDailyCard(), Is.True);
            DailyChallengeScreen daily = boot.DailyScreen!;
            SnapshotKit.AttachOverlay(daily.View!.Canvas);
            daily.View.Canvas.planeDistance = 10f;
            WinDaily(boot, daily);
            DailyStepUntil(boot, () => daily.Calendar.Visible, 20000, "calendario de sellos");
            daily.Calendar.Settle();
            Assert.That(daily.Calendar.RewardShown, Is.True);
            yield return Save(dir, 8, "reto-del-rio", daily.Calendar.Canvas);
        }

        // ------------------------------------------------------------------------------------------------- extras sin texto

        /// <summary>9 (extra, sin texto): el Mercado en la pestaña Semillas, con el Cántaro.</summary>
        [UnityTest]
        public IEnumerator Shot09_Market([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            var clock = new ManualClock(Day0.AddHours(15));
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            var offers = new InMemoryOffersStore();
            offers.Save(new OffersRecord { PiggySeeds = 1230, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = offers;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = 46;
            profile.Progress.MaxLevel = 45;
            profile.Wallet.Seeds = 2450;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            yield return SaveRaw(dir, 9, "mercado", boot.Market.Canvas);
        }

        /// <summary>10 (extra, sin texto): la Liga del río (D-046) en Global, con la API falsa.</summary>
        [UnityTest]
        public IEnumerator Shot10_League([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = Begin(locale);
            var clock = new ManualClock(Day0.AddHours(15));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.LeagueApiOverride = new FakeLeagueApi(clock) { StartPoints = 140 };
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 57;
            profile.Progress.NextLevel = 58;
            profile.Wallet.Seeds = 2450;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.TickLeague(16f);
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenLeague(), Is.True);
            SnapshotKit.AttachOverlay(boot.LeagueView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            yield return SaveRaw(dir, 10, "liga", boot.LeagueView.Canvas);
        }

        // ------------------------------------------------------------------------------------------------- arranque y juego

        // El idioma, la pantalla de 16:9 y la carpeta de salida (docs/store-listing/graficos/<idioma>/).
        private static string Begin(string locale)
        {
            FunnelSetup.Hooks(); // LaunchOptions.Reset, reloj fijo y presentadores inmediatos (sin anuncios de prueba a la vista)
            SnapshotKit.Prepare(locale);
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "store-listing", "graficos", locale));
            Directory.CreateDirectory(dir);
            return dir;
        }

        // El nivel en juego, tras la intro, con el lienzo en la cámara de las capturas.
        private static IEnumerator LoadPlaying(int level, Profile? profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile ?? FunnelSetup.Profile(500));
            yield return Canvas16x9LayoutTests.LoadPlaying(level);
            SnapshotKit.Attach(GameBootstrap.Current!);
        }

        private static void PlayMoves(GameBootstrap boot, int count)
        {
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved && solution.MoveCount > count, Is.True, "solución con más de " + count + " vertidos");
            for (int i = 0; i < count; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
                PlayModeHelpers.StepUntilIdle(boot, 4000);
            }

            PlayModeHelpers.Step(boot, 300);
        }

        // Juega la solución hasta el primer vertido de color (como mucho maxPrefix vertidos antes) y lo deja con el chorro ya en la
        // superficie del destino (como VisualSnapshotTests: si no, el chorro sale a medio crecer).
        private static void PourMidway(GameBootstrap boot, int color, int maxPrefix)
        {
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            int i = 0;
            for (; i < solution.MoveCount - 1 && i < maxPrefix; i++)
            {
                Pour candidate = solution.GetMove(i);
                if (flow.Session.Current[candidate.Source].TopColor == color)
                {
                    break;
                }

                boot.TapCuia(candidate.Source);
                boot.TapCuia(candidate.Target);
                PlayModeHelpers.StepUntilIdle(boot, 4000);
            }

            Pour move = solution.GetMove(i);
            Debug.Log("AF_STORE vertido " + i + ": " + move.Source + " → " + move.Target + ", color " + flow.Session.Current[move.Source].TopColor);
            boot.TapCuia(move.Source);
            boot.TapCuia(move.Target);
            for (int t = 0; t < 3000 && !StreamReachedSurface(); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(StreamReachedSurface(), Is.True, "chorro hasta la superficie");
            PlayModeHelpers.Step(boot, 96);
        }

        private static bool StreamReachedSurface()
        {
            StreamGraphic[] streams = Object.FindObjectsByType<StreamGraphic>(FindObjectsSortMode.None);
            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i].enabled && streams[i].Range.y >= 0.999f)
                {
                    return true;
                }
            }

            return false;
        }

        // El muelle del río en nextLevel a la hora hour (como HomeDockSnapshotTests.Shot): con Canoas, la Bitácora y el Reto, y
        // TiTino avanzado slothMs (su saludo).
        private static IEnumerator Dock(int nextLevel, int hour, int slothMs)
        {
            var clock = new ManualClock(Day0.AddHours(hour));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.StreakStoreOverride = null;
            GameBootstrap.ResetLogbookOverrides();
            HomeDockTests.UseLogbook();
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = 1250;
            profile.Wallet.Canoes.Count = 5;
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

            if (boot.LeagueButton != null)
            {
                SnapshotKit.AttachOverlay(boot.LeagueButton.Canvas);
            }

            yield return SnapshotKit.Wait(0.3f);
            Debug.Log("AF_STORE muelle: TiTino " + sloth.Pose + " «" + sloth.BubbleText + "», tarjetas " + lobby.DayCardCount);
        }

        private static void AttachCard(RectTransform? card)
        {
            if (card != null)
            {
                SnapshotKit.AttachOverlay(card.GetComponentInParent<Canvas>().rootCanvas);
            }
        }

        private static void DailyStep(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                boot.TickDaily(PlayModeHelpers.FrameMs);
            }
        }

        private static void DailyStepUntil(GameBootstrap boot, Func<bool> done, int maxMs, string what)
        {
            for (int t = 0; t < maxMs && !done(); t += PlayModeHelpers.FrameMs)
            {
                boot.TickDaily(PlayModeHelpers.FrameMs);
            }

            Assert.That(done(), Is.True, "tiempo agotado: " + what);
        }

        private static void WinDaily(GameBootstrap boot, DailyChallengeScreen daily)
        {
            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere(); // salta la intro
            DailyStepUntil(boot, () => flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput, 10000, "fin de la intro del Reto");
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                DailyStepUntil(boot, () => flow.AcceptsBoardInput, 5000, "tablero listo para el vertido " + i);
                daily.Actions.TapCuia(move.Source);
                daily.Actions.TapCuia(move.Target);
            }

            DailyStepUntil(boot, () => daily.Won, 10000, "victoria del Reto");
        }

        // ------------------------------------------------------------------------------------------------- composición

        // La captura NN con su texto: la pantalla de 16:9 (las 1.920 filas de arriba de la textura de SnapshotKit) reducida en un
        // marco de madera bajo la placa con el texto, sobre el petróleo de la marca, a 1080 × 1920 px.
        private IEnumerator Save(string dir, int number, string scene, Canvas focus, bool waitFrames = true)
        {
            if (waitFrames)
            {
                yield return SnapshotKit.Wait(0.3f);
            }

            Texture2D game = ScreenTexture();
            string caption = Caption(SnapshotKit.Locale, number);
            GameBootstrap boot = GameBootstrap.Current!;
            ProceduralArt art = boot.Art!;

            var target = new RenderTexture(SnapshotKit.Width, ScreenPx, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("StoreListingCamera");
            camGo.transform.position = new Vector3(50000f, 0f, 0f); // lejos del juego: ni él ve la composición ni ella al juego
            Camera cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.targetTexture = target;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Petroleum;
            var canvasGo = new GameObject("StoreListingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = SnapshotKit.Width / GameScreen.ReferenceWidth; // 3 px/dp: el lienzo mide 360 × 640 dp
            _owned.Add(target);
            _owned.Add(camGo);
            _owned.Add(canvasGo);
            _owned.Add(game);

            Transform root = canvasGo.transform;
            Image bg = UiBuilder.Image("Background", root, null, Petroleum);
            UiBuilder.Stretch(bg.rectTransform);

            // La placa con el texto: la madera del kit (la de los contadores del inicio), arriba.
            const float plateTop = 14f;
            const float plateH = 86f;
            Image plate = UiBuilder.Sliced("CaptionPlate", root, art.WoodPlate, Color.white);
            UiBuilder.Place(plate.rectTransform, 14f, plateTop, 332f, plateH);
            TextMeshProUGUI label = UiBuilder.Label("Caption", plate.transform, TextStyle.ModalTitle, caption, 30f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);
            // En una línea si cabe a 22 sp o más; si no, en dos, con el interlineado de Baloo 2 apretado.
            float boxW = 332f - 32f;
            label.fontSize = 22f;
            if (label.GetPreferredValues(caption, float.PositiveInfinity, float.PositiveInfinity).x <= boxW)
            {
                UiBuilder.AutoSize(label, 22f, 30f);
            }
            else
            {
                label.textWrappingMode = TextWrappingModes.Normal;
                label.lineSpacing = -38f;
                UiBuilder.AutoSize(label, 20f, 28f);
            }

            // La pantalla del juego en su marco de madera, centrada en lo que queda.
            const float frame = 6f;
            const float gameTop = plateTop + plateH + 12f + frame;
            float gameH = 640f - gameTop - 10f - frame;
            float gameW = gameH * SnapshotKit.Width / ScreenPx;
            Image border = UiBuilder.Sliced("Frame", root, art.WoodPlate, Color.white);
            UiBuilder.Place(border.rectTransform, ((360f - gameW) * 0.5f) - frame, gameTop - frame, gameW + (2f * frame), gameH + (2f * frame));
            var shotGo = new GameObject("Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            shotGo.transform.SetParent(root, false);
            RawImage shot = shotGo.GetComponent<RawImage>();
            shot.texture = game;
            shot.raycastTarget = false;
            UiBuilder.Place((RectTransform)shotGo.transform, (360f - gameW) * 0.5f, gameTop, gameW, gameH);

            yield return null;
            Canvas.ForceUpdateCanvases();
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, "el texto «" + caption + "» cabe en su placa");
            Debug.Log("AF_STORE " + SnapshotKit.Locale + " " + number + ": «" + caption + "» a " + label.fontSize.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                + " sp, " + label.textInfo.lineCount + " línea(s); juego " + gameW.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " × "
                + gameH.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " dp");
            Write(Path.Combine(dir, FileName(number, scene)), Read(cam, target));
            AssertNoRawKeys(focus);
            Object.Destroy(canvasGo);
        }

        // Extra sin texto: la pantalla de 16:9 a sangre.
        private IEnumerator SaveRaw(string dir, int number, string scene, Canvas focus)
        {
            yield return null;
            Texture2D game = ScreenTexture();
            _owned.Add(game);
            Write(Path.Combine(dir, FileName(number, scene)), game.EncodeToPNG());
            AssertNoRawKeys(focus);
        }

        private static string FileName(int number, string scene)
        {
            return number.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "-" + scene + ".png";
        }

        // Las 1.920 filas de arriba de la textura de SnapshotKit (la pantalla simulada de 16:9; el resto es solo fondo).
        private static Texture2D ScreenTexture()
        {
            Color32[] full = SnapshotKit.CapturePixels(); // las filas empiezan por abajo
            var crop = new Color32[SnapshotKit.Width * ScreenPx];
            Array.Copy(full, (SnapshotKit.Height - ScreenPx) * SnapshotKit.Width, crop, 0, crop.Length);
            var tex = new Texture2D(SnapshotKit.Width, ScreenPx, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(crop);
            tex.Apply();
            return tex;
        }

        private static byte[] Read(Camera cam, RenderTexture target)
        {
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var tex = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            byte[] png = tex.EncodeToPNG();
            Object.Destroy(tex);
            return png;
        }

        private static void Write(string path, byte[] png)
        {
            File.WriteAllBytes(path, png);
            Debug.Log("AF_STORE escrita " + path + " (" + png.Length + " bytes)");
        }

        // Ningún texto visible de la pantalla es una clave cruda de la tabla (p. ej. «market_tab_seeds»).
        private static void AssertNoRawKeys(Canvas focus)
        {
            var raw = new StringBuilder();
            foreach (TMP_Text t in focus.GetComponentsInChildren<TMP_Text>(false))
            {
                string s = t.text;
                if (t.isActiveAndEnabled && s.Length > 3 && s.IndexOf(' ') < 0 && s.IndexOf('_') > 0 && s.ToLowerInvariant() == s)
                {
                    raw.Append(" «").Append(s).Append('»');
                }
            }

            Assert.That(raw.ToString(), Is.Empty, "claves crudas a la vista");
        }

        // El texto de la captura number en locale: la tabla §6 de docs/store-listing/<locale>/ficha.md («| n | texto | estado |»).
        private static string Caption(string locale, int number)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "store-listing", locale, "ficha.md"));
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            bool inSection = false;
            string prefix = "| " + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + " |";
            foreach (string line in lines)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inSection = line.StartsWith("## 6.", StringComparison.Ordinal);
                    continue;
                }

                if (inSection && line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    string[] cells = line.Split('|');
                    string text = cells[2].Trim();
                    Assert.That(text.Length, Is.InRange(1, 36), "texto de la captura " + number + " en " + locale + " (≤ 36 caracteres)");
                    return text;
                }
            }

            Assert.Fail("sin texto para la captura " + number + " en " + path + " §6");
            return string.Empty;
        }
    }
}
