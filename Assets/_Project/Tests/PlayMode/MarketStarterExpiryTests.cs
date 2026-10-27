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
    /// La Mochila de inicio desaparece de verdad al llegar a cero (T-L9 de <c>docs/legal/investigacion-legal-mundial.md</c> §11; 05
    /// §4.1: «temporizador 48 h desde la primera vista»), también con el Mercado o su tarjeta abiertos: la fila se quita, la tarjeta se
    /// va y ya no se puede comprar desde una pantalla que se quedó abierta. Y no vuelve: el Mercado reabierto no la enseña.
    /// </summary>
    public sealed class MarketStarterExpiryTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetDailyOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int nextLevel, DateTime? starterSeen)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            if (starterSeen.HasValue)
            {
                profile.Offers = new OffersData { StarterFirstSeenAt = starterSeen };
            }

            return profile;
        }

        private static GameBootstrap Boot()
        {
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<MarketRunner>().enabled = false; // el test avanza el Mercado a mano
            return boot;
        }

        private static int StarterRow(MarketScreen market)
        {
            for (int i = 0; i < market.Rows.Count; i++)
            {
                if (market.Rows[i].Id == StoreCatalog.Starter)
                {
                    return i;
                }
            }

            return -1;
        }

        [UnityTest]
        public IEnumerator MarketOpen_AtZero_TheRowGoes_AndItCannotBeBought_NorComesBack()
        {
            var clock = new ManualClock(T0);
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(16, T0 - TimeSpan.FromHours(47))));
            Assert.That(GameBootstrap.Current!.SetUpMarket(), Is.True);
            GameBootstrap boot = Boot();
            Assert.That(boot.OpenMarket(MarketTab.Boosters), Is.True);
            MarketScreen market = boot.Market!;
            int row = StarterRow(market);
            Assert.That(row, Is.GreaterThanOrEqualTo(0), "queda 1 h: a la venta");
            Assert.That(market.StarterStale, Is.False);

            clock.Advance(TimeSpan.FromHours(1));
            Assert.That(market.StarterStale, Is.True, "a cero, con su fila aún pintada");
            Assert.That(market.PressRow(row), Is.False, "la fila que se quedó a la vista ya no vende");
            Assert.That(boot.Profile!.Entitlements.StarterBought, Is.False);
            Assert.That(StarterRow(market), Is.EqualTo(-1), "y el Mercado se rehízo sin ella");

            clock.Advance(TimeSpan.FromMinutes(1));
            boot.TickMarket(16f);
            Assert.That(boot.PressMarketBack(), Is.True);
            Assert.That(boot.OpenMarket(MarketTab.Boosters), Is.True);
            Assert.That(StarterRow(boot.Market!), Is.EqualTo(-1), "no reaparece al volver a abrir el Mercado");
        }

        [UnityTest]
        public IEnumerator MarketOpen_TheTickRemovesTheRow_AtZero()
        {
            var clock = new ManualClock(T0);
            LaunchOptions.ClockOverride = clock;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(16, T0 - TimeSpan.FromHours(47))));
            Assert.That(GameBootstrap.Current!.SetUpMarket(), Is.True);
            GameBootstrap boot = Boot();
            Assert.That(boot.OpenMarket(MarketTab.Boosters), Is.True);
            Assert.That(StarterRow(boot.Market!), Is.GreaterThanOrEqualTo(0));
            boot.TickMarket(16f);
            Assert.That(StarterRow(boot.Market!), Is.GreaterThanOrEqualTo(0), "antes de cero sigue");

            clock.Advance(TimeSpan.FromHours(1));
            boot.TickMarket(16f);
            Assert.That(StarterRow(boot.Market!), Is.EqualTo(-1), "a cero, fuera sin tocar nada");
            Assert.That(boot.Market!.Visible, Is.True, "el Mercado sigue abierto");
        }

        [UnityTest]
        public IEnumerator StarterCard_LeftOpenPastZero_GoesAway_AndDoesNotSell()
        {
            var clock = new ManualClock(T0);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 }); // la 2.ª sesión
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(12, null));
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            Assert.That(GameBootstrap.Current!.SetUpMarket(), Is.True);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Sku, Is.EqualTo(StoreCatalog.Starter));
            Assert.That(boot.Offers!.StarterExpiresAt, Is.EqualTo(T0 + TimeSpan.FromHours(48)));

            // Sin pasar por el Tick: el toque en «Comprar» de una tarjeta que se quedó abierta ya no vende.
            clock.Advance(TimeSpan.FromHours(48));
            card.BuyButton.onClick.Invoke();
            Assert.That(boot.Profile!.Entitlements.StarterBought, Is.False, "a cero ya no se vende");
            Assert.That(boot.Offers.StarterAvailable(12), Is.False);
        }

        [UnityTest]
        public IEnumerator StarterCard_TheTickTakesItAway_AtZero()
        {
            var clock = new ManualClock(T0);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(12, null));
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            Assert.That(GameBootstrap.Current!.SetUpMarket(), Is.True);
            GameBootstrap boot = Boot();
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
            OfferCard card = boot.OfferCardView!;
            Assert.That(card.Visible, Is.True);

            clock.Advance(TimeSpan.FromHours(47));
            boot.TickMarket(16f);
            Assert.That(card.Visible, Is.True, "le queda 1 h");
            clock.Advance(TimeSpan.FromHours(1));
            boot.TickMarket(16f);
            Assert.That(card.Visible, Is.False, "a cero, la tarjeta se va");
            Assert.That(boot.Profile!.Entitlements.StarterBought, Is.False);
        }
    }
}
