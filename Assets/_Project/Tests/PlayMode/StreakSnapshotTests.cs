#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
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
    /// Capturas de la tarea B1b (la interfaz de la Crecida y la Canasta) en es-419, a 360 × 780 (9:19,5) y 360 × 640 (16:9; recortadas
    /// a 1080 × 1920 px), al 100 % y al 130 %, en <c>Logs/snapshots/es-419/b1b_*.png</c>: la barra del HUD (el 30), la victoria del 27
    /// (hito 3) y la del 30 (la Canasta que se abre, a mitad y al final), Aguas estancadas con la línea de la Hoja escudo (el 27), la
    /// tarjeta del 25 y el widget del inicio. Escribe una línea <c>AF_B1B</c> por captura. Explícito: necesita gráficos (sin
    /// <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de B1b; necesitan dispositivo gráfico.")]
    public sealed class StreakSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
        private ManualClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
            _clock = new ManualClock(T0, TimeSpan.FromHours(-5));
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameBootstrap.ResetStreakOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator CaptureAll()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string tail = (small ? "_640_" : "_780_") + Mathf.RoundToInt(scale * 100f);
                    yield return CaptureHud(small, scale, "b1b_hud_l30" + tail);
                    yield return CaptureVictory(27, 2, small, scale, 2000, "b1b_victory_l27" + tail);
                    yield return CaptureVictory(30, 5, small, scale, 1250, "b1b_victory_l30_open" + tail);
                    yield return CaptureVictory(30, 5, small, scale, 2000, "b1b_victory_l30" + tail);
                    yield return CaptureStuck(small, scale, "b1b_stuck_l27" + tail);
                    yield return CaptureCard(small, scale, "b1b_card25" + tail);
                    yield return CaptureLobby(small, scale, "b1b_lobby_l29" + tail);
                }
            }
        }

        private void Begin(bool small, float scale, int wins, bool shield, out string dir)
        {
            FunnelSetup.Hooks();
            dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ClockOverride = _clock;
            GameBootstrap.StreakStoreOverride = new InMemoryStreakStore(_clock, new StreakRecord { DateLocal = "2026-10-08", Wins = wins, ShieldAvailable = shield });
        }

        private static void End(string dir, string name, bool small)
        {
            if (small)
            {
                SnapshotKit.SaveTop(dir, name, (int)HudLabelTests.Screen640.y);
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }

            SnapshotKit.Release();
            GameBootstrap.ResetStreakOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        private IEnumerator CaptureHud(bool small, float scale, string name)
        {
            Begin(small, scale, 4, true, out string dir);
            yield return HudSetup.LoadPlaying(30);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            Debug.Log("AF_B1B " + name + ": barra " + boot.View!.StreakHudBar.Wins + "/10, hoja " + boot.View.StreakHudBar.ShieldShown);
            End(dir, name, small);
        }

        private IEnumerator CaptureVictory(int level, int wins, bool small, float scale, int ms, string name)
        {
            Begin(small, scale, wins, true, out string dir);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, ms);
            GameScreen view = boot.View!;
            Debug.Log("AF_B1B " + name + ": escalón " + view.VictoryCompact + ", barras " + (view.VictoryBarsDropped ? "fuera" : view.VictoryBarsView.Half ? "en una fila" : "a lo ancho")
                + " «" + view.VictoryBarsView.StreakText + "» «" + view.VictoryBarsView.BasketText + "», panel " + view.VictoryPanelHeight + " de " + view.VictoryPanelBudgetDp);
            yield return SnapshotKit.Wait(0.1f);
            End(dir, name, small);
        }

        private IEnumerator CaptureStuck(bool small, float scale, string name)
        {
            Begin(small, scale, 3, true, out string dir);
            LaunchOptions.LevelOffsetOverride = 21;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(27, new MemoryProgressStore(27, 26));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 600);
            Debug.Log("AF_B1B " + name + ": «" + boot.View!.Stuck.ShieldLineText + "», compacto " + boot.View.Stuck.Compact);
            yield return SnapshotKit.Wait(0.1f);
            End(dir, name, small);
        }

        private IEnumerator CaptureCard(bool small, float scale, string name)
        {
            Begin(small, scale, 0, true, out string dir);
            LaunchOptions.LevelCardsOverride = true;
            yield return HudSetup.LoadAtIntro(25);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600);
            yield return SnapshotKit.Wait(0.2f);
            Debug.Log("AF_B1B " + name + ": " + boot.Flow!.Phase + " «" + boot.View!.LevelCard.TutorialText + "»");
            End(dir, name, small);
        }

        private IEnumerator CaptureLobby(bool small, float scale, string name)
        {
            Begin(small, scale, 3, true, out string dir);
            Profile profile = Wallet.NewProfile(_clock);
            profile.Progress.NextLevel = 29;
            profile.Progress.MaxLevel = 28;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            if (boot.DailyCard != null)
            {
                SnapshotKit.AttachOverlay(boot.DailyCard.Root.GetComponentInParent<Canvas>().rootCanvas);
            }

            yield return SnapshotKit.Wait(0.4f);
            Debug.Log("AF_B1B " + name + ": widget " + boot.Lobby!.StreakWidget.Visible + " " + boot.Lobby.StreakWidget.Wins + "/10");
            End(dir, name, small);
        }
    }
}
