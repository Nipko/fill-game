#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Bloque B5 en la escena <c>Game</c> real (05 §4.1 y §5; 06 §4.7), con la tienda simulada (D-008) y sin los parches: el Mercado
    /// se abre desde el icono del inicio (desde el 10), vende con la hoja de compra (aquí, la inmediata), elige la skin puesta; el
    /// Cántaro se llena con las victorias desde el 20 y su icono sale en el inicio; la tarjeta de la Mochila sale en la victoria del 12
    /// de la 2.ª sesión, pasado <see cref="GameBootstrap.OfferCardDelayMs"/>, y nunca en la primera; Atrás cierra el Mercado y la
    /// tarjeta. El almacén de <c>piggy</c> y <c>offers</c> es el de memoria (sin B5-profile).
    /// </summary>
    public sealed class MarketTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetDailyOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        // El arranque cuenta otra sesión al abrir: con una ya contada, esta es la 2.ª (Q-M4 (a)).
        private static void SecondSession()
        {
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
        }

        private static GameBootstrap Boot()
        {
            GameBootstrap boot = GameBootstrap.Current!;
            MarketRunner runner = boot.GetComponent<MarketRunner>();
            Assert.That(runner, Is.Not.Null, "SetUpMarket añade su runner (enganche B5-bootstrap o el test)");
            runner.enabled = false; // el test avanza el Mercado a mano
            return boot;
        }

        // Sin el parche B5-bootstrap, Start no llama a SetUpMarket: el test lo llama (con el parche, la segunda llamada no hace nada).
        private static IEnumerator LoadLobby(Profile profile)
        {
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            Assert.That(GameBootstrap.Current!.SetUpMarket(), Is.True);
        }

        private static IEnumerator LoadLevel(int level)
        {
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(level));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            Assert.That(GameBootstrap.Current!.SetUpMarket(), Is.True);
        }

        [UnityTest]
        public IEnumerator Lobby_ShowsTheMarketFrom10_AndThePiggyFrom20()
        {
            yield return LoadLobby(ProfileAt(10));
            GameBootstrap boot = Boot();
            boot.TickMarket(16f);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.MarketIcons!.MarketShown, Is.False, "antes de max_level 10 no");
            Assert.That(boot.MarketIcons.PiggyShown, Is.False);

            yield return LoadLobby(ProfileAt(26));
            boot = Boot();
            boot.TickMarket(16f);
            Assert.That(boot.MarketIcons!.MarketShown, Is.True);
            Assert.That(boot.MarketIcons.PiggyShown, Is.True, "Cántaro desde eco_piggy_from_level");
            Assert.That(boot.MarketIcons.PiggyText, Is.EqualTo("0"));
            Assert.That(boot.MarketIcons.MarketRect.rect.height, Is.GreaterThanOrEqualTo(48f), "área táctil (07 §7.8)");
        }

        [UnityTest]
        public IEnumerator Market_BuysAPack_WithTheSimulatedStore_AndBackClosesIt()
        {
            yield return LoadLobby(ProfileAt(26));
            GameBootstrap boot = Boot();
            boot.TickMarket(16f);
            boot.MarketIcons!.MarketButton.onClick.Invoke();
            MarketScreen market = boot.Market!;
            Assert.That(market.Visible, Is.True);
            Assert.That(market.Tab, Is.EqualTo(MarketTab.Seeds));
            Assert.That(market.TabShown(MarketTab.NoAds), Is.True, "Sin anuncios desde el 22");
            Assert.That(market.TabShown(MarketTab.Boosters), Is.False, "entre el 21 y el 30 no hay Mochila ni Canoas");
            Assert.That(market.Rows[0].Id, Is.EqualTo("af_piggy"));
            Assert.That(market.RowButton(0).GetComponent<ReliefButton>().Interactable, Is.False, "Cántaro vacío: comprable desde 1.000");
            Assert.That(market.Rows[1].Id, Is.EqualTo("af_coins_2"));
            Assert.That(market.RowButtonText(1), Is.EqualTo(boot.Store!.PriceOf("af_coins_2").ToString()), "el precio de la tienda, tal cual");

            Assert.That(market.PressRow(1), Is.True);
            Assert.That(boot.Profile!.Wallet.Seeds, Is.EqualTo(1000), "Puñado: 1.000 semillas con Wallet.Apply");
            Assert.That(market.Visible, Is.True, "el Mercado sigue abierto tras comprar");
            Assert.That(boot.PressMarketBack(), Is.True, "Atrás (con B5-bootstrap, desde PressAndroidBack)");
            Assert.That(market.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Market_CuiasTab_PutsOnAnOwnedSkin()
        {
            Profile profile = ProfileAt(46);
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.SkinEquipped = "af_skin_m02";
            yield return LoadLobby(profile);
            GameBootstrap boot = Boot();
            Assert.That(boot.OpenMarket(MarketTab.Cuias), Is.True);
            MarketScreen market = boot.Market!;
            Assert.That(market.Tab, Is.EqualTo(MarketTab.Cuias));
            Assert.That(market.Rows[1].Action, Is.EqualTo(MarketAction.Equipped));
            Assert.That(market.Rows[0].Action, Is.EqualTo(MarketAction.Equip));

            Assert.That(market.PressRow(0), Is.True);
            Assert.That(boot.Skins!.Equipped, Is.EqualTo(SkinRewards.BaseSkinId));
            Assert.That(boot.Profile!.Entitlements.SkinEquipped, Is.Null, "la base deja skin_equipped en null");
            Assert.That(boot.Art!.CuiaSkin, Is.SameAs(CuiaSkin.Base), "las cuias del próximo nivel la llevan");
            Assert.That(market.Rows[0].Action, Is.EqualTo(MarketAction.Equipped));
        }

        [UnityTest]
        public IEnumerator Victory20_FillsTheJar()
        {
            yield return LoadLevel(20);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            Assert.That(boot.Piggy!.Seeds, Is.EqualTo(boot.Piggy.FillFor(boot.Flow!.HarvestTier)), "+15 / +30 / +40 (05 §4.1)");
            boot.TickMarket(16f);
            Assert.That(boot.Piggy.Seeds, Is.EqualTo(boot.Piggy.FillFor(boot.Flow.HarvestTier)), "una vez por victoria");
        }

        [UnityTest]
        public IEnumerator Victory12_OfTheSecondSession_ShowsTheStarterCard_AfterTheDelay_AndBuysIt()
        {
            SecondSession();
            yield return LoadLevel(12);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Sessions!.Current!.Value.Number, Is.EqualTo(2));
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs - 100f);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.False, "espera a «Siguiente»");
            boot.TickMarket(200f);
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Sku, Is.EqualTo("af_starter"));
            Assert.That(boot.LastOfferShown, Is.EqualTo(OfferKind.Starter));
            Assert.That(card.BuyText, Is.EqualTo(boot.Store!.PriceOf("af_starter").ToString()));
            Assert.That(boot.Offers!.StarterExpiresAt, Is.Not.Null, "arranca las 48 h");

            card.BuyButton.onClick.Invoke();
            Assert.That(card.Visible, Is.False);
            Assert.That(boot.Profile!.Entitlements.StarterBought, Is.True);
            Assert.That(boot.Profile.Wallet.Seeds, Is.GreaterThanOrEqualTo(500));
        }

        [UnityTest]
        public IEnumerator Victory12_OfTheFirstSession_ShowsNoCard_AndBackClosesACard()
        {
            yield return LoadLevel(12);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Sessions!.Current!.Value.Number, Is.EqualTo(1));
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 100f);
            Assert.That(boot.OfferCardView!.Visible, Is.False, "06 §2.1: ninguna oferta de pago en la primera sesión");
            Assert.That(boot.LastOfferShown, Is.EqualTo(OfferKind.None));
        }

        [UnityTest]
        public IEnumerator PiggyIcon_OpensItsCard_LockedBelow1000_AndBackClosesIt()
        {
            yield return LoadLobby(ProfileAt(26));
            GameBootstrap boot = Boot();
            boot.TickMarket(16f);
            boot.MarketIcons!.PiggyButton.onClick.Invoke();
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Sku, Is.EqualTo("af_piggy"));
            Assert.That(card.BuyButton.GetComponent<ReliefButton>().Interactable, Is.False, "comprable desde eco_piggy_min_buy");
            Assert.That(boot.PressMarketBack(), Is.True);
            Assert.That(card.Visible, Is.False);
            Assert.That(boot.Offers!.Ledger.Wallet.Seeds, Is.EqualTo(0));
        }
    }
}
