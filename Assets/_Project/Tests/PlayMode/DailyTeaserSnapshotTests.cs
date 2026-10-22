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
    /// Capturas de la tarjeta del Reto del río tras ganar el 7 (observación 2 de la hoja de la prueba del 2026-10-22: TiTino tapaba
    /// parte del texto), en <c>Logs/snapshots/es-419/</c>: <c>fx1022_02_reto_l7_{780|640}_{100|130}</c>, con la tarjeta a la vista.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.DailyTeaserSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de la tarjeta del Reto del 7; necesitan dispositivo gráfico.")]
    public sealed class DailyTeaserSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameBootstrap.ResetDailyOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator TeaserAfterLevel7([Values(false, true)] bool small, [Values(1f, 1.3f)] float scale)
        {
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, (int)DailyTeaserCard.DelayMs + 400);
            GameScreen view = boot.View!;
            Assert.That(view.DailyTeaser!.Visible, Is.True);
            SnapshotKit.Attach(boot);
            string name = "fx1022_02_reto_l7_" + (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_TEASER_SNAPSHOT " + name + ": TiTino " + (view.SlothView.Visible ? "a la vista hasta " + view.SlothView.FootprintBottomDp
                + " dp" : view.SlothYieldedToTeaser ? "cede la franja" : "no sale") + "; tarjeta " + view.DailyTeaser.SeenRect);
        }
    }
}
