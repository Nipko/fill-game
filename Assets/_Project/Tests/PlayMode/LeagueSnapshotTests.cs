#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la Liga del río (D-046) con la API falsa: el botón del inicio, la pantalla en Global y en País, sin conexión, y la
    /// victoria del 41 con su línea, en es-419 al 100 % y en pt-BR al 130 %.
    /// </summary>
    [Explicit("Capturas de la Liga del río (D-046); necesitan dispositivo gráfico.")]
    public sealed class LeagueSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        private static void Fake(ManualClock clock, bool offline = false)
        {
            GameBootstrap.LeagueApiOverride = new FakeLeagueApi(clock) { StartPoints = 140, Offline = offline };
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
        }

        [UnityTest]
        public IEnumerator CaptureLeague([Values(LocLocale.SourceLocale, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = SnapshotKit.Prepare(locale);
            float scale = locale == LocLocale.SourceLocale ? 1f : 1.3f;
            LaunchOptions.FontScaleOverride = scale;
            string suffix = locale == LocLocale.SourceLocale ? "_100" : "_130";
            var clock = new ManualClock(T0);
            Fake(clock);
            LaunchOptions.ClockOverride = clock;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 57;
            profile.Progress.NextLevel = 58;
            profile.Wallet.Seeds = 2450;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.TickLeague(16f);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.LeagueButton!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "league_01_lobby" + suffix);

            Assert.That(boot.OpenLeague(), Is.True);
            SnapshotKit.AttachOverlay(boot.LeagueView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "league_02_global" + suffix);
            boot.LeagueView.CountryTabButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "league_03_country" + suffix);
            boot.CloseLeague();

            Assert.That(boot.PressSettings(), Is.True);
            SnapshotKit.AttachOverlay(boot.SettingsView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            boot.SettingsView.ScrollToEnd();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "league_05_settings" + suffix);
            boot.CloseSettings();

            Fake(clock, offline: true);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenLeague(), Is.True);
            SnapshotKit.AttachOverlay(boot.LeagueView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "league_04_offline" + suffix);
        }

        [UnityTest]
        public IEnumerator CaptureVictoryLine()
        {
            string dir = SnapshotKit.Prepare();
            FunnelSetup.Hooks();
            var clock = new ManualClock(T0);
            Fake(clock);
            LaunchOptions.LevelOffsetOverride = 40;
            Profile profile = VictorySetup.Profile(100);
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "league_06_victory_l41_100");
        }
    }
}
