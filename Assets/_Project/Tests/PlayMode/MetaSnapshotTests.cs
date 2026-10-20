#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la meta de B14 (tanda 5), con el arte provisional: <c>b14_01_skin_toast</c> (el 40 recargado tras ganarlo, con «Cuia de
    /// Santarém · puesta» arriba), <c>b14_01b_board_&lt;skin&gt;</c> (el tablero del 40 con cada skin de la 1.0: la base, Santarém,
    /// el Encuentro y la Totuma), <c>b14_02_map_markers</c> (el Mapa con max 25: ✓ en las Canastas cobradas, la cestita en la de
    /// Flona Tapajós y «Premio del mundo · faltan 15» bajo ella), <c>b14_03_future_card</c> (su tarjeta de parada futura con la
    /// Canasta del 30 y el premio), <c>b14_03b_passing_card</c> (Teotônio, con max 300), <c>b14_04_settings_river_facts</c>
    /// (Ajustes › «Sobre la Amazonía» con max 25, desplazado hasta la sección) y <c>b14_06_light_&lt;nivel&gt;</c> (los fondos de
    /// 41 normal, 40 Difícil con la luz de tarde y 60 Muy difícil con la noche, y el 60 con el velo antiguo para comparar).
    /// Explícito: necesita gráficos; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de la meta de B14; necesitan dispositivo gráfico.")]
    public sealed class MetaSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.DifficultyLightEnabled = true;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureSkins()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.LevelOffsetOverride = 30;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(Wallet.NewProfile(new ManualClock(T0)));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            boot.TickMeta(16f);
            boot.Flow!.Load(40); // el aviso sale en la pantalla siguiente, con las cuias ya con la skin
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 600);
            boot.TickMeta(16f);
            Assert.That(boot.SkinToast!.Visible, Is.True);
            boot.TickMeta(300f); // el aviso, entero (su fundido de entrada)
            SnapshotKit.AttachOverlay(boot.MetaToastCanvas!);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "b14_01_skin_toast_780_100");

            foreach (CuiaSkin skin in new[] { CuiaSkin.Base, CuiaSkin.Santarem, CuiaSkin.Encuentro, CuiaSkin.Totuma })
            {
                boot.Art!.CuiaSkin = skin;
                boot.Flow!.Load(40);
                boot.TapAnywhere(); // sin la intro: el tablero quieto
                PlayModeHelpers.Step(boot, 600);
                yield return SnapshotKit.Wait(0.2f);
                Assert.That(boot.View!.Views[0].Skin, Is.SameAs(skin));
                SnapshotKit.Save(dir, "b14_01b_board_" + skin.Id);
            }
        }

        [UnityTest]
        public IEnumerator CaptureToastAfterTheWorldBanner()
        {
            // El camino real (revisión de B14): 40 → «Siguiente» → banner del mundo 3 en el 41 → el aviso, al cerrarse el banner.
            string dir = SnapshotKit.Prepare();
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(Wallet.NewProfile(new ManualClock(T0)));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Attach(boot);
            MetaTests.CloseCards(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMeta(16f);
            boot.PressNext();
            for (int i = 0; i < 200 && boot.Flow!.LevelNumber != 41; i++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 600);
            boot.TickMeta(16f);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "b14_01c_banner_41_without_toast_780_100");
            MetaTests.CloseCards(boot);
            boot.TickMeta(16f);
            boot.TickMeta(300f);
            PlayModeHelpers.Step(boot, 400);
            SnapshotKit.AttachOverlay(boot.MetaToastCanvas!);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "b14_01d_level41_toast_780_100");
        }

        [UnityTest]
        public IEnumerator CaptureMapAndCards()
        {
            string dir = SnapshotKit.Prepare();
            yield return MapShot(dir, 26, "b14_02_map_markers_780_100", null);
            yield return MapShot(dir, 26, "b14_03_future_card_780_100", m => Assert.That(m.OpenStopCard(3), Is.True));
            yield return MapShot(dir, 301, "b14_03b_passing_card_780_100", m =>
            {
                for (int i = 0; i < m.PassingCount; i++)
                {
                    if (m.PassingAt(i).Id == "teotonio")
                    {
                        Assert.That(m.TapPassing(i), Is.True);
                    }
                }
            });
        }

        [UnityTest]
        public IEnumerator CaptureSettingsRiverFacts()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 26;
            profile.Progress.MaxLevel = 25;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            SnapshotKit.AttachOverlay(settings.Canvas);
            yield return null;
            settings.ScrollToAbout();
            yield return null;
            settings.ScrollToAbout();
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "b14_04_settings_river_facts_780_100");
            settings.ScrollToBottomForTests();
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "b14_04b_settings_river_facts_bottom_780_100");
        }

        [UnityTest]
        public IEnumerator CaptureDifficultyLight()
        {
            string dir = SnapshotKit.Prepare();
            foreach ((int level, bool light, string name) in new[] { (41, true, "41_normal"), (40, true, "40_hard_afternoon"), (60, true, "60_veryhard_night"), (60, false, "60_veryhard_old_veil") })
            {
                GameScreen.DifficultyLightEnabled = light;
                GameScreen.SimulatedScreen = HudLabelTests.Screen780;
                yield return HudSetup.LoadPlaying(level);
                GameBootstrap boot = GameBootstrap.Current!;
                SnapshotKit.Attach(boot);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "b14_06_light_" + name);
            }
        }

        private static IEnumerator MapShot(string dir, int playLevel, string name, Action<RiverMapScreen>? before)
        {
            yield return MapMarkersTests.OpenApp(playLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            before?.Invoke(map);
            map.Tick(16f);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }
    }
}
