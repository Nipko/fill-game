#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarea FX16b (D-032, puntos 2 y 6):
    /// <list type="bullet">
    /// <item>la victoria compacta en 16:9 (360 × 640 dp, recortadas a los 1080 × 1920 px de la pantalla simulada): el 39 (Cosecha
    /// doble y el chip del 40), el 20 (con su llegada) y el 20 con SingleKey, en es-419 y en en-XA, al 100 % y al 130 %
    /// (<c>fx16b_victory_l{39|20|20_singlekey}_640_{100|130}.png</c> en <c>Logs/snapshots/&lt;idioma&gt;/</c>), y el 20 a 360 × 780 al
    /// 130 % en es-419, que no cambia (<c>fx16b_victory_l20_780_130.png</c>);</item>
    /// <item>el inicio tras ganar el 700: con el Reto del río desbloqueado, «Jugar · Reto del río», y sin él, «Jugar»
    /// (<c>fx16b_lobby_after700_{reto|sin_reto}.png</c> en <c>Logs/snapshots/es-419/</c>).</item>
    /// </list>
    /// Escribe una línea <c>AF_FX16B</c> por captura. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.Fx16bSnapshotTests</c> sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de FX16b (victoria compacta e inicio tras el 700); necesitan dispositivo gráfico.")]
    public sealed class Fx16bSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.SimulatedScreen = null;
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        [UnityTest]
        public IEnumerator CaptureCompactVictories()
        {
            foreach (string locale in new[] { LocLocale.SpanishLatinAmerica, LocLocale.Pseudo })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    foreach (int code in new[] { 39, 20, -20 })
                    {
                        int level = code < 0 ? -code : code;
                        bool singleKey = code < 0;
                        string name = "fx16b_victory_l" + level + (singleKey ? "_singlekey" : string.Empty) + "_640_" + Mathf.RoundToInt(scale * 100f);
                        yield return Capture(locale, scale, level, singleKey, true, name);
                    }
                }
            }

            yield return Capture(LocLocale.SpanishLatinAmerica, 1.3f, 20, false, false, "fx16b_victory_l20_780_130");
        }

        [UnityTest]
        public IEnumerator CaptureLobbyAfter700()
        {
            foreach (bool reto in new[] { true, false })
            {
                if (reto)
                {
                    GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
                    GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
                }
                else
                {
                    GameBootstrap.DailyPoolDisabledOverride = true;
                }

                GameScreen.SimulatedScreen = HudLabelTests.Screen780;
                string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
                yield return Fx16bTests.LoadAfter700();
                GameBootstrap boot = GameBootstrap.Current!;
                SnapshotKit.AttachLobby(boot);
                if (reto && boot.DailyCard != null)
                {
                    SnapshotKit.AttachOverlay(boot.DailyCard.Root.GetComponentInParent<Canvas>().rootCanvas);
                }

                yield return SnapshotKit.Wait(0.4f);
                LobbyScreen lobby = boot.Lobby!;
                string name = "fx16b_lobby_after700_" + (reto ? "reto" : "sin_reto");
                Assert.That(lobby.PlayMode, Is.EqualTo(reto ? PlayButtonMode.Daily : PlayButtonMode.ContentEnd), name);
                Debug.Log("AF_FX16B " + name + ": «" + lobby.PlayLabel + "»");
                SnapshotKit.Save(dir, name);
                SnapshotKit.Release();
                GameScreen.SimulatedScreen = null;
                GameBootstrap.ResetDailyOverrides();
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
            }
        }

        private static IEnumerator Capture(string locale, float scale, int level, bool singleKey, bool small, string name)
        {
            FunnelSetup.Hooks();
            string dir = SnapshotKit.Prepare(locale);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            if (singleKey)
            {
                view.RiverPolicyOverride = RiverFactPolicy.SingleKey;
            }

            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(Loc.Locale, Is.EqualTo(locale), name);
            Fx16bTests.AssertCompactVictory(view, name);
            Debug.Log("AF_FX16B " + name + ": escalón " + view.VictoryCompact + ", panel " + view.VictoryPanelHeight + " dp (tope "
                + view.VictoryPanelBudgetDp + "), cinta " + (view.VictoryPanelTopDp - 38f) + ", cabecera " + view.SlothHeaderBottomDp
                + ", Siguiente " + view.NextTopDp + ", ranura " + view.VictorySlot.ContentDp + " dp");
            yield return SnapshotKit.Wait(0.2f);
            if (small)
            {
                SnapshotKit.SaveTop(dir, name, (int)HudLabelTests.Screen640.y);
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }

            SnapshotKit.Release();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }
    }
}
