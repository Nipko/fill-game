#nullable enable
using System;
using System.Collections;
using AF.Analytics;
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
            GameBootstrap.ResetTelemetryOverrides();
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

        private static IEnumerator LoadLevel(int level, ImmediatePurchasePresenter? presenter = null)
        {
            LaunchOptions.PurchasePresenterOverride = presenter ?? new ImmediatePurchasePresenter();
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
        public IEnumerator Market_SendsShopView_AndOfferShownWithContextShop()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            yield return LoadLobby(ProfileAt(26));
            GameBootstrap boot = Boot();
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            AnalyticsEvent view = sink.Last("af_shop_view")!;
            Assert.That(view, Is.Not.Null, "08 §8.2: af_shop_view");
            Assert.That(view.Find("tab")!.Value.Text, Is.EqualTo("seeds"));
            Assert.That(view.Find("from")!.Value.Text, Is.EqualTo("lobby"));
            AnalyticsEvent shown = sink.Last("af_iap_offer_shown")!;
            Assert.That(shown, Is.Not.Null);
            Assert.That(shown.Find("context")!.Value.Text, Is.EqualTo("shop"));
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
        public IEnumerator SeenInterstitials_CountForTheNoAdsTrigger_ThroughTheAdsLedger()
        {
            yield return LoadLobby(ProfileAt(26));
            GameBootstrap boot = Boot();
            boot.TickMarket(16f);
            Assert.That(boot.Offers!.InterstitialsSeen, Is.EqualTo(0));
            boot.Ads!.Ledger.RecordInterstitialShown();
            boot.Ads.Ledger.RecordInterstitialShown();
            boot.TickMarket(16f);
            Assert.That(boot.Offers.InterstitialsSeen, Is.EqualTo(2), "el contador del día de AF.Ads, sin tocar sus archivos");
            boot.Ads.Ledger.RecordInterstitialShown();
            boot.Ads.Ledger.RecordInterstitialShown();
            boot.TickMarket(16f);
            Assert.That(boot.Offers.InterstitialsSeen, Is.EqualTo(3), "con tope en el del disparador (3_interstitials_or_d2)");
        }

        [UnityTest]
        public IEnumerator LeavingTheVictory_TakesTheCardAway()
        {
            // Tras la Cosecha doble el juego pasa solo al nivel siguiente (D-011): la tarjeta no se queda encima del nivel nuevo.
            SecondSession();
            yield return LoadLevel(12);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.OfferCardView!.Visible, Is.True);
            Assert.That(boot.PressNext(), Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(boot.OfferCardView.Visible, Is.False);
            Assert.That(boot.Offers!.StarterExpiresAt, Is.Not.Null, "la tarjeta se vio: su temporizador sigue");
        }

        [UnityTest]
        public IEnumerator NoCard_WhileTheVictoryIsLeaving()
        {
            // Con la salida en marcha (el avance solo tras la Cosecha doble, D-011, o los 250 ms de «Siguiente», D-033, punto 9) no
            // sale ninguna tarjeta, que gastaría el cupo para quitarse al momento; cuando la victoria sigue quieta, sí.
            SecondSession();
            yield return LoadLevel(12);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.VictoryLeaving, Is.False, "la victoria, quieta");
            GameBootstrap.VictoryLeavingOverride = true;
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.OfferCardView!.Visible, Is.False);
            Assert.That(boot.Offers!.StarterExpiresAt, Is.Null, "nada apuntado ni cupo gastado");
            GameBootstrap.VictoryLeavingOverride = false;
            boot.TickMarket(16f);
            Assert.That(boot.OfferCardView.Visible, Is.True, "en cuanto la victoria se queda quieta");
        }

        [UnityTest]
        public IEnumerator NoAdsCard_OnDay2_ACancelledPurchaseCountsAsADecline()
        {
            SecondSession();
            LaunchOptions.ClockOverride = new ManualClock(T0 + TimeSpan.FromDays(1)); // el perfil se creó en T0: hoy es el día 2
            var sheet = new ImmediatePurchasePresenter(confirm: false); // la hoja de compra: «Cancelar»
            yield return LoadLevel(31, sheet);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.False, "05 §5: «tarjeta tras la pantalla de victoria»: sale en el inicio [P] (Q-B5-6)");

            Assert.That(boot.PressHome(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            boot.TickMarket(16f);
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Sku, Is.EqualTo("af_noads"));
            Assert.That(boot.LastOfferShown, Is.EqualTo(OfferKind.NoAds));

            // Cancelar la hoja de compra también es no comprar (05 §5: un rechazo de Sin anuncios).
            card.BuyButton.onClick.Invoke();
            Assert.That(card.Visible, Is.False);
            Assert.That(sheet.Presented, Is.EqualTo(1));
            Assert.That(boot.Offers!.NoAdsDeclines, Is.EqualTo(1));
            Assert.That(boot.Profile!.Entitlements.NoAds, Is.False);
        }

        private static void JarWith(int seeds)
        {
            var store = new InMemoryOffersStore();
            store.Save(new OffersRecord { PiggySeeds = seeds, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = store;
        }

        [UnityTest]
        public IEnumerator Piggy_UnlocksAfterWinning19_AndSaysSoToTheAnalytics()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            yield return LoadLevel(19);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            AnalyticsEvent piggy = sink.Last("af_piggy")!;
            Assert.That(piggy, Is.Not.Null, "08 §8.2: af_piggy");
            Assert.That(piggy.Find("state")!.Value.Text, Is.EqualTo("unlocked"));
            Assert.That(boot.Piggy!.Seeds, Is.EqualTo(0), "el 19 aún no lo llena");
        }

        [UnityTest]
        public IEnumerator Market_BuysTheJar_ItEmpties_AndTheAnalyticsSaysBought()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            JarWith(1500);
            yield return LoadLobby(ProfileAt(26));
            GameBootstrap boot = Boot();
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            Assert.That(market.Rows[0].Id, Is.EqualTo("af_piggy"));
            Assert.That(market.Rows[0].Action, Is.EqualTo(MarketAction.Buy));
            Assert.That(market.PressRow(0), Is.True);
            Assert.That(boot.Profile!.Wallet.Seeds, Is.EqualTo(1500), "exactamente lo acumulado (05 §4.1)");
            Assert.That(boot.Piggy!.Seeds, Is.EqualTo(0), "se vacía y vuelve a llenarse");
            AnalyticsEvent bought = sink.Last("af_piggy")!;
            Assert.That(bought.Find("state")!.Value.Text, Is.EqualTo("bought"));
            Assert.That(bought.Find("seeds_inside")!.Value.Long, Is.EqualTo(1500));
        }

        [UnityTest]
        public IEnumerator FullJar_ShowsItsCardOnTheVictory_Once()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            SecondSession();
            LaunchOptions.ClockOverride = new ManualClock(T0); // día 1: Sin anuncios aún no toca
            JarWith(3000);
            yield return LoadLevel(35);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.True, "05 §5: «1 tarjeta al llenarse», en el HUD de victoria");
            Assert.That(card.Sku, Is.EqualTo("af_piggy"));
            Assert.That(card.Icon.sprite, Is.SameAs(boot.Art!.DockJar), "observación 7 de la jornada 5: la tinaja, no un círculo marrón");
            Assert.That(boot.LastOfferShown, Is.EqualTo(OfferKind.PiggyFull));
            Assert.That(sink.Last("af_piggy")!.Find("state")!.Value.Text, Is.EqualTo("shown_full"));
            Assert.That(card.BuyButton.GetComponent<ReliefButton>().Interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator SkinCard_WithSkinSales_ComesOnTheLobby_AndABoughtSkinIsPutOn()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            GameBootstrap.SkinSalesOverride = true; // Q-B5-1: la venta de skins, encendida para la prueba
            SecondSession();
            LaunchOptions.ClockOverride = new ManualClock(T0); // día 1: Sin anuncios aún no toca
            yield return LoadLevel(41);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.False, "la skin, «fuera del nivel» (05 §5): en el inicio [P]");
            Assert.That(boot.PressHome(), Is.True);
            boot.TickMarket(16f);
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Sku, Is.EqualTo("af_skin_m03"));
            Assert.That(ProceduralArt.IsSkinThumb(card.Icon.sprite, "af_skin_m03"), Is.True, "observación 4 de la jornada 6: la cuia con la skin, no un círculo");
            Assert.That(card.Icon.rectTransform.rect.height, Is.EqualTo(OfferCard.TallIconDp), "la ficha de la skin, alta: no a 56 dp");

            card.BuyButton.onClick.Invoke();
            Assert.That(boot.Profile!.Entitlements.Skins, Does.Contain("af_skin_m03"));
            Assert.That(boot.Skins!.Equipped, Is.EqualTo("af_skin_m03"), "Q-B5-13: la comprada se pone, como la ganada");
            Assert.That(boot.Art!.CuiaSkin, Is.SameAs(CuiaSkin.Encuentro));
            AnalyticsEvent unlock = sink.Last("af_skin_unlock")!;
            Assert.That(unlock.Find("source")!.Value.Text, Is.EqualTo("iap"));
            Assert.That(unlock.Find("skin_id")!.Value.Text, Is.EqualTo("af_skin_m03"));
        }

        [UnityTest]
        public IEnumerator TappingAFullJar_CountsAsItsCard_Once()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            SecondSession();
            LaunchOptions.ClockOverride = new ManualClock(T0);
            JarWith(3000);
            yield return LoadLobby(ProfileAt(40));
            GameBootstrap boot = Boot();
            boot.TickMarket(16f);
            Assert.That(boot.PressPiggy(), Is.True);
            Assert.That(boot.Offers!.PiggyFullCardShown, Is.True, "05 §5: «1 tarjeta al llenarse»");
            Assert.That(boot.PressMarketBack(), Is.True);
            Assert.That(boot.PressPiggy(), Is.True);
            Assert.That(boot.PressMarketBack(), Is.True);
            int shownFull = 0;
            foreach (AnalyticsEvent e in sink.Events)
            {
                if (e.Name == "af_piggy" && e.Find("state")!.Value.Text == "shown_full")
                {
                    shownFull++;
                }
            }

            Assert.That(shownFull, Is.EqualTo(1), "shown_full, una vez por llenado");
            var ctx = new VictoryOfferContext(40, 2, 2);
            Assert.That(boot.Offers.PickAtVictory(in ctx), Is.EqualTo(OfferKind.None), "ya no sale sola en una victoria");
        }

        [UnityTest]
        public IEnumerator WithTheStoreOff_NeitherTheMarketIconNorTheJarShow()
        {
            JarWith(1500);
            yield return LoadLobby(ProfileAt(26));
            GameBootstrap boot = Boot();
            boot.TickMarket(16f);
            Assert.That(boot.MarketIcons!.MarketShown && boot.MarketIcons.PiggyShown, Is.True);
            boot.Offers!.Ledger.Config.BizIapEnabled = false; // biz_iap_enabled apagado (08 §9) o mantenimiento
            boot.TickMarket(16f);
            Assert.That(boot.MarketIcons.MarketShown, Is.False);
            Assert.That(boot.MarketIcons.PiggyShown, Is.False, "D-008: sin tienda no hay Cántaro que comprar");
            Assert.That(boot.PressPiggy(), Is.False);
        }

        [UnityTest]
        public IEnumerator TheSpentQuota_SurvivesTheLanguageRebuild()
        {
            SecondSession();
            LaunchOptions.ClockOverride = new ManualClock(T0);
            yield return LoadLevel(12);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            Assert.That(boot.LastOfferShown, Is.EqualTo(OfferKind.Starter), "el cupo de la sesión, gastado");
            Assert.That(boot.PressMarketBack(), Is.True);
            Assert.That(boot.PressHome(), Is.True);
            Assert.That(boot.PressSettings(), Is.True);

            GameBootstrap first = boot;
            Assert.That(boot.ChooseLanguage(AF.Localization.LocLocale.Pseudo), Is.True);
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción de la UI en otro idioma");
            boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            Assert.That(boot.Sessions!.Current!.Value.Number, Is.EqualTo(2), "la misma sesión (B6)");
            var ctx = new VictoryOfferContext(15, 2, 2);
            Assert.That(boot.Offers!.PickAtVictory(in ctx), Is.EqualTo(OfferKind.None), "el recordatorio del 15 espera a otra sesión");
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
