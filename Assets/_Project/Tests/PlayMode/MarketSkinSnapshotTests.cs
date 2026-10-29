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
    /// Capturas de las miniaturas del Mercado y las ofertas (observación 4 de la jornada 6 y 7 de la jornada 5), a 360 × 780 dp y en
    /// español: <c>skins_01_market_cuias_todas</c> (pestaña Cuias con las cuatro skins, la del Encuentro puesta),
    /// <c>skins_02_market_cuias_faltan</c> (en el 46: la base y Santarém, y las que faltan con «faltan N»),
    /// <c>skins_03_offer_skin</c> (la tarjeta de la Cuia del Encuentro en el inicio, con la venta de skins encendida),
    /// <c>skins_04_offer_cantaro</c> (la tarjeta del Cántaro lleno) y <c>skins_05_market_semillas</c> (la fila del Cántaro en
    /// Semillas). Explícito: necesita gráficos; los PNG quedan en <c>Logs/snapshots/es-419/</c> (se copian a
    /// <c>docs/art/mercado-skins/</c>).
    /// </summary>
    [Explicit("Capturas de las miniaturas de skins y del Cántaro; necesitan dispositivo gráfico.")]
    public sealed class MarketSkinSnapshotTests
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
        public IEnumerator CaptureCuiasTab()
        {
            string dir = SnapshotKit.Prepare();
            Profile profile = ProfileAt(141, 2450);
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.Skins.Add("af_skin_m03");
            profile.Entitlements.Skins.Add("af_skin_m04");
            profile.Entitlements.SkinEquipped = "af_skin_m03";
            yield return Lobby(profile);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_01_market_cuias_todas");

            JarWith(1230);
            Profile early = ProfileAt(46, 800);
            early.Entitlements.Skins.Add("af_skin_m02");
            early.Entitlements.SkinEquipped = "af_skin_m02";
            yield return Lobby(early);
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_02_market_cuias_faltan");

            boot.Market.SelectTab(MarketTab.Seeds);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_05_market_semillas");
        }

        [UnityTest]
        public IEnumerator CaptureSkinOffer()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap.SkinSalesOverride = true; // Q-B5-1: la venta de skins, encendida para la captura
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(41, 340));
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.PressHome(), Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.OfferCardView!.Visible, Is.True);
            Assert.That(boot.OfferCardView.Sku, Is.EqualTo("af_skin_m03"));
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.OfferCardView.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_03_offer_skin");
        }

        [UnityTest]
        public IEnumerator CaptureJarCard()
        {
            string dir = SnapshotKit.Prepare();
            JarWith(3000);
            yield return Lobby(ProfileAt(60, 800));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressPiggy(), Is.True);
            SnapshotKit.AttachOverlay(boot.OfferCardView!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "skins_04_offer_cantaro");
        }
    }
}
