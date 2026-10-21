#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del bloque B5 con el arte provisional, a 360 × 780 dp (las que citan los textos <c>market_*</c> y <c>offer_*</c> de
    /// <c>strings.csv</c>; con el parche B5-loc, en español): <c>b5_01_market_seeds</c> (Mercado, pestaña Semillas, con el Cántaro a
    /// 1.230), <c>b5_02_market_boosters</c> (Comodines, con la Mochila y su temporizador), <c>b5_03_market_cuias</c> (Cuias, con la
    /// Cuia de Santarém puesta), <c>b5_04_market_noads</c> (Sin anuncios), <c>b5_05_offer_starter</c> (la tarjeta de la Mochila
    /// sobre la victoria del 12), <c>b5_06_lobby_icons</c> (el inicio con el icono del Mercado y el Cántaro) y
    /// <c>b5_07_offer_piggy</c> (la tarjeta del Cántaro lleno). Explícito: necesita gráficos; los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del Mercado y las ofertas de B5; necesitan dispositivo gráfico.")]
    public sealed class MarketSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        private static Profile ProfileAt(int nextLevel, int seeds)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            return profile;
        }

        private static void JarWith(int seeds)
        {
            var store = new InMemoryOffersStore();
            store.Save(new OffersRecord { PiggySeeds = seeds, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = store;
        }

        private static IEnumerator Lobby(Profile profile)
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
        }

        [UnityTest]
        public IEnumerator CaptureMarket()
        {
            string dir = SnapshotKit.Prepare();
            JarWith(1230);
            Profile profile = ProfileAt(46, 2450);
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.SkinEquipped = "af_skin_m02";
            yield return Lobby(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.MarketIcons!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(boot.MarketIcons.MarketShown && boot.MarketIcons.PiggyShown, Is.True);
            SnapshotKit.Save(dir, "b5_06_lobby_icons_780_100");

            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b5_01_market_seeds_780_100");

            boot.Market.SelectTab(MarketTab.Cuias);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b5_03_market_cuias_780_100");

            boot.Market.SelectTab(MarketTab.NoAds);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b5_04_market_noads_780_100");
        }

        [UnityTest]
        public IEnumerator CaptureStarter()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(12, 340));
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Attach(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.OfferCardView!.Visible, Is.True);
            SnapshotKit.AttachOverlay(boot.OfferCardView.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b5_05_offer_starter_780_100");

            // La Mochila en el Mercado, con su temporizador ya en marcha.
            boot.OfferCardView.Close(false);
            Assert.That(boot.PressHome(), Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Boosters), Is.True);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(boot.Market.RowNoteText(0), Is.Not.Empty, "Quedan 48 horas");
            SnapshotKit.Save(dir, "b5_02_market_boosters_780_100");
        }

        [UnityTest]
        public IEnumerator CapturePiggyCard()
        {
            string dir = SnapshotKit.Prepare();
            JarWith(3000);
            yield return Lobby(ProfileAt(60, 800));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressPiggy(), Is.True);
            SnapshotKit.AttachOverlay(boot.OfferCardView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "b5_07_offer_piggy_780_100");
        }
    }
}
