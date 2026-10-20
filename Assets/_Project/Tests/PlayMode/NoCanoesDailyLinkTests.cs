#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.LiveOps;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Enganche E7 en la escena (<c>NoCanoesPanel</c>; 04 §7.3): Sin canoas lleva «Mientras esperas: Jugar el Reto del río»
    /// encima de «Volver al inicio», en el inicio y en el nivel, con su id en <c>AF_LAYOUT</c>; tocarlo abre el Reto y registra
    /// <c>af_no_canoes_choice</c> = <c>daily_challenge</c> (04 §11). Sin el Reto en el build no sale. Simula E2 con
    /// <see cref="GameBootstrap.SetUpDaily"/> y el pool sintético.
    /// </summary>
    public sealed class NoCanoesDailyLinkTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            LaunchOptions.LocTablesOverride = DailyTestTables.Texts();
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Lobby_TheLineSitsAboveBackHome_AndOpensTheReto()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile()));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            boot.Lobby!.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));

            NoCanoesPanel panel = boot.Lobby.NoCanoes;
            NoCanoesDailyLink link = panel.DailyLink;
            Assert.That(link.Visible, Is.True);
            Assert.That(link.Text, Is.EqualTo("Mientras esperas: Jugar el Reto del río"), "04 §7.3");
            AssertAboveHome(panel);

            link.Button.onClick.Invoke();
            Assert.That(boot.DailyScreen!.IsOpen, Is.True, "abre el tablero de hoy");
            Assert.That(boot.LastNoCanoesChoice, Is.EqualTo("daily_challenge"));
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(panel.Visible, Is.False);
            Assert.That(boot.Profile!.Wallet.Canoes.Count, Is.EqualTo(0), "sin canoas (04 §7.1)");
        }

        [UnityTest]
        public IEnumerator Level_TheLineIsInLevelFailedsNoCanoesToo()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            boot.TapAnywhere();
            FunnelSetup.GetStuck(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            NoCanoesPanel panel = boot.View!.NoCanoes;
            Assert.That(panel.DailyLink.Visible, Is.True);
            AssertAboveHome(panel);
            panel.DailyLink.Button.onClick.Invoke();
            Assert.That(boot.DailyScreen!.IsOpen, Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        [UnityTest]
        public IEnumerator WithoutTheRetoInTheBuild_NoLine()
        {
            GameBootstrap.DailyPoolDisabledOverride = true; // sin pool aunque la escena lo traiga (E3): D-008
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesProfile()));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.SetUpDaily();
            boot.Lobby!.CanoesButton.onClick.Invoke();
            NoCanoesPanel panel = boot.Lobby.NoCanoes;
            Assert.That(panel.Visible, Is.True);
            Assert.That(panel.DailyLink.Visible, Is.False);
            var ids = new List<string>();
            panel.VisibleButtons(ids, new List<RectTransform>());
            Assert.That(ids, Has.No.Member(NoCanoesDailyLink.LayoutId));
            Assert.That(ids, Has.Member("home"));
        }

        // La línea va justo encima de «Volver al inicio», sin pisarlo ni pisar el botón de arriba, y en AF_LAYOUT antes que «home».
        private static void AssertAboveHome(NoCanoesPanel panel)
        {
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            panel.VisibleButtons(ids, rects);
            int daily = ids.IndexOf(NoCanoesDailyLink.LayoutId);
            int home = ids.IndexOf("home");
            Assert.That(daily, Is.GreaterThanOrEqualTo(0), "AF_LAYOUT con daily_challenge");
            Assert.That(home, Is.EqualTo(daily + 1));
            Rect d = World(rects[daily]);
            Assert.That(d.yMin, Is.GreaterThanOrEqualTo(World(rects[home]).yMax - 0.01f), "encima de «Volver al inicio»");
            for (int i = 0; i < rects.Count; i++)
            {
                if (i != daily)
                {
                    Assert.That(d.Overlaps(World(rects[i])), Is.False, "no pisa " + ids[i]);
                }
            }
        }

        private static Rect World(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        private static Profile NoCanoesProfile()
        {
            Profile profile = FunnelSetup.Profile(500, 0);
            profile.Progress.MaxLevel = 30;
            profile.Progress.NextLevel = 31;
            return profile;
        }
    }
}
