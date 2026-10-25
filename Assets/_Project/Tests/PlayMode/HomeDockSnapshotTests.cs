#nullable enable
using System;
using System.Collections;
using AF.Audio;
using AF.Economy;
using AF.IAP;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas para el dueño del inicio «el muelle del río» (D-050), con la pantalla simulada de <see cref="SnapshotKit"/>
    /// (1080 × 2340 px, 360 × 780 dp) y el juego real, en <c>Logs/snapshots/&lt;locale&gt;/dock_*.png</c>:
    /// <list type="bullet">
    /// <item><c>dock_01_l4</c>: el 4, antes del Reto (sin tarjetas: la caja de cuias), TiTino animando a jugar;</item>
    /// <item><c>dock_02_l31</c>: el 31 por la mañana, con Canoas, el saludo de TiTino y las tarjetas del Reto y la Bitácora;</item>
    /// <item><c>dock_03_l58_liga</c>: el 58 (Difícil) con la Liga activa (tres tarjetas), la Crecida, el Cántaro y la travesía hacia
    /// Anavilhanas;</item>
    /// <item><c>dock_04_l140</c>: el 140, otro mundo y otra agua;</item>
    /// <item><c>dock_05_night</c>: el 58 a las 22:00, TiTino dormido;</item>
    /// <item><c>dock_06_l58_640</c>, <c>dock_07_l58_320</c> y <c>dock_08_l58_130</c>: el 58 con la Liga a 360 × 640 (16:9), en un
    /// lienzo de 320 dp y con el texto al 130 % (este, en en-XA y en pt-BR).</item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.HomeDockSnapshotTests</c> sin
    /// <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class HomeDockSnapshotTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
            HomeSloth.ManualTickForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            HomeSloth.ManualTickForTests = false;
            GameBootstrap.ResetLeagueOverrides();
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator CaptureDock_Es419()
        {
            string dir = SnapshotKit.Prepare();
            yield return Shot(dir, "dock_01_l4", 4, 15, false, 0, 1600);
            yield return Shot(dir, "dock_02_l31", 31, 8, false, 0, 400);
            yield return Shot(dir, "dock_03_l58_liga", 58, 15, true, 4, 1600);
            yield return Shot(dir, "dock_04_l140", 140, 17, false, 6, 1600);
            yield return Shot(dir, "dock_05_night", 58, 22, true, 4, 1600);

            GameScreen.SimulatedScreen = new Vector4(1080f, 1920f, 96f, 48f);
            yield return Shot(dir, "dock_06_l58_640", 58, 15, true, 4, 1600);
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            GameScreen.SimulatedPxPerDp = 3.375f; // 320 dp de ancho con los 1080 px de la captura
            yield return Shot(dir, "dock_07_l58_320", 58, 15, true, 4, 1600);
        }

        [UnityTest]
        public IEnumerator CaptureDock130([Values("en-XA", "pt-BR")] string locale)
        {
            string dir = SnapshotKit.Prepare(locale);
            LaunchOptions.FontScaleOverride = 1.3f;
            yield return Shot(dir, "dock_08_l58_130", 58, 8, true, 4, 400);
            GameScreen.SimulatedScreen = new Vector4(1080f, 1920f, 96f, 48f);
            yield return Shot(dir, "dock_09_l58_640_130", 58, 15, true, 4, 1600);
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            GameScreen.SimulatedPxPerDp = 3.375f;
            yield return Shot(dir, "dock_10_l58_320_130", 58, 15, true, 4, 1600);
        }

        // El inicio de nextLevel a la hora hour (UTC, sin desfase), con o sin la Liga, la Crecida con streakWins victorias y TiTino
        // avanzado slothMs (el saludo, o el primer mensaje de la rotación). La pantalla simulada, la que haya puesto quien llama.
        private static IEnumerator Shot(string dir, string name, int nextLevel, int hour, bool league, int streakWins, int slothMs)
        {
            var clock = new ManualClock(Day0.AddHours(hour));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.ResetLeagueOverrides();
            if (league)
            {
                GameBootstrap.LeagueApiOverride = new AF.LiveOps.FakeLeagueApi(clock);
                GameBootstrap.LeagueClockOverride = clock;
                GameBootstrap.LeagueConfigOverride = new AF.LiveOps.LeagueConfig { LiveLeagueEnabled = true };
            }

            GameBootstrap.StreakStoreOverride = streakWins > 0 ? StreakStore(streakWins, clock) : null;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = nextLevel >= 10 ? 12345 : 240;
            if (nextLevel >= 31)
            {
                profile.Wallet.Canoes.Count = 3;
                profile.Wallet.Canoes.NextRegenAt = clock.UtcNow + TimeSpan.FromMinutes(10);
            }

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
            Attach(boot.DailyCard?.Root);
            Attach(boot.LogbookCard?.Root);
            if (boot.MarketIcons != null)
            {
                SnapshotKit.AttachOverlay(boot.MarketIcons.Canvas);
            }

            if (boot.LeagueButton != null)
            {
                SnapshotKit.AttachOverlay(boot.LeagueButton.Canvas);
            }

            yield return SnapshotKit.Wait(0.3f);
            Vector4? screen = GameScreen.SimulatedScreen;
            if (screen.HasValue && screen.Value.y < SnapshotKit.Height)
            {
                SnapshotKit.SaveTop(dir, name, (int)screen.Value.y); // 16:9: solo la pantalla, sin el resto de la textura
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }
            Debug.Log("AF_DOCK " + name + ": nivel " + nextLevel + " a las " + hour + ":00, " + lobby.CanvasWidth + " × " + lobby.CanvasHeight
                + " dp, TiTino " + sloth.Pose + " «" + sloth.BubbleText + "» (" + sloth.Note + "), letrero «" + lobby.StopRemainingText
                + "», tarjetas " + lobby.DayCardCount + ", crecida " + lobby.StreakWidget.Visible);
            GameBootstrap.StreakStoreOverride = null;
        }

        private static void Attach(RectTransform? card)
        {
            if (card != null)
            {
                SnapshotKit.AttachOverlay(card.GetComponentInParent<Canvas>().rootCanvas);
            }
        }

        // Una Crecida en marcha con wins victorias y la Hoja escudo de hoy.
        private static InMemoryStreakStore StreakStore(int wins, ManualClock clock)
        {
            var record = new StreakRecord { DateLocal = clock.LocalDateKey(), Wins = wins, ShieldAvailable = true };
            return new InMemoryStreakStore(clock, record);
        }
    }
}
