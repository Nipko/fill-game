#nullable enable
using System;
using AF.Ads;
using AF.Analytics.Adapters;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Analytics.Tests
{
    /// <summary>
    /// Los envoltorios de anuncios y compras (08 §8.2: <c>af_ad_request</c>, <c>af_ad_shown</c>, <c>af_ad_reward</c>,
    /// <c>af_iap_offer_shown</c>, <c>af_iap_result</c> y <c>purchase</c>) sobre los servicios simulados de la fase 2 (D-008).
    /// </summary>
    public sealed class AnalyticsAdaptersTests
    {
        private static (AnalyticsService analytics, FakeAnalyticsSink sink) NewAnalytics()
        {
            var analytics = new AnalyticsService();
            var sink = new FakeAnalyticsSink();
            analytics.AddSink(sink);
            return (analytics, sink);
        }

        [Test]
        public void ARewardedThatPays_IsRequestShownAndReward()
        {
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var ads = new AnalyticsAdService(new FakeAdService(new ImmediateAdPresenter()), analytics);
            AdOutcome? outcome = null;
            ads.Show(AdPlacement.Hint, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded), "el envoltorio no cambia el resultado");
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_ad_request", "af_ad_shown", "af_ad_reward" }));
            Assert.That(sink.Events[0].ToJson(), Is.EqualTo("{\"event\":\"af_ad_request\",\"placement\":\"rv_hint\",\"format\":\"rewarded\",\"result\":\"filled\"}"));
            Assert.That(sink.Events[2].Find("granted")!.Value.Text, Is.EqualTo("provisional"));
            Assert.That(analytics.Violations, Is.Empty);
        }

        [Test]
        public void MaxImpressions_AreAdImpression_UnlessIlrIsOff()
        {
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var port = new FakeMaxSdkPort();
            var max = new MaxAdService(port, new ManualClock(new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc)), () => null, () => 1);
            bool ilr = true;
            var ads = new AnalyticsAdService(new ConsentAdService(max), analytics, () => ilr);
            port.Listener = max;
            port.RaiseRevenue(MaxAdKind.Interstitial, "AppLovin", 0.5);

            Assert.That(sink.Names, Is.EqualTo(new[] { "ad_impression" }));
            Assert.That(sink.Events[0].Find("ad_platform")!.Value.Text, Is.EqualTo("appLovin"));
            Assert.That(sink.Events[0].Find("ad_unit_name")!.Value.Text, Is.EqualTo("interstitial_post_level"));
            Assert.That(analytics.Violations, Is.Empty);

            sink.Clear();
            ilr = false; // ads_ilr_to_firebase
            port.RaiseRevenue(MaxAdKind.Interstitial, "AppLovin", 0.5);
            Assert.That(sink.Names, Is.Empty);
            GC.KeepAlive(ads);
        }

        [Test]
        public void NoFill_AndAnInterstitial_AndABlockedAd()
        {
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var ads = new AnalyticsAdService(new FakeAdService(new ImmediateAdPresenter(), FakeAdFill.NoFill), analytics);
            ads.Show(AdPlacement.DoubleHarvest, o => { });
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_ad_request" }));
            Assert.That(sink.Events[0].Find("result")!.Value.Text, Is.EqualTo("no_fill"));

            sink.Clear();
            ads.Record(AdPlacement.InterstitialPostLevel, AdOutcome.Shown);
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_ad_request", "af_ad_shown" }));
            Assert.That(sink.Events[1].Find("format")!.Value.Text, Is.EqualTo("interstitial"));
            Assert.That(sink.Events[1].Find("placement")!.Value.Text, Is.EqualTo("interstitial_post_level"));

            sink.Clear();
            ads.Record(AdPlacement.Hint, AdOutcome.Blocked);
            Assert.That(sink.Events, Is.Empty, "la política no pidió anuncio");
            ads.Record(AdPlacement.Hint, AdOutcome.Failed);
            Assert.That(sink.Events[0].Find("result")!.Value.Text, Is.EqualTo("error"));
        }

        [Test]
        public void APurchase_IsResultAndPurchase_ACancel_OnlyTheResult()
        {
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var clock = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var store = new MemoryProfileStore();
            var wallet = new Wallet(profile, store, clock);
            var presenter = new ImmediatePurchasePresenter();
            var inner = new FakeStoreService(new StoreLedger(profile, wallet, store, clock), presenter, "USD");
            var service = new AnalyticsStoreService(inner, analytics);

            PurchaseResult? result = null;
            service.Purchase(new PurchaseRequest("af_coins_2", PurchaseContext.Stuck, 31), r => result = r);
            Assert.That(result!.IsPurchased, Is.True);
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result", "purchase" }));
            Assert.That(sink.Events[0].ToJson(), Is.EqualTo("{\"event\":\"af_iap_result\",\"sku\":\"af_coins_2\",\"result\":\"ok\",\"context\":\"stuck\"}"));
            Assert.That(sink.Events[1].Find("items")!.Value.Text, Is.EqualTo("af_coins_2"));
            Assert.That(sink.Events[1].Find("currency")!.Value.Text, Is.EqualTo("USD"));
            Assert.That(sink.Events[1].Find("transaction_id"), Is.Null, "sin servidor no hay id: no se inventa");

            sink.Clear();
            service.Record(PurchaseResultFor(service, "af_coins_3", false));
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result" }));
            Assert.That(sink.Events[0].Find("result")!.Value.Text, Is.EqualTo("cancel"));

            sink.Clear();
            service.OfferShown("af_noads", PurchaseContext.NoAdsPrompt);
            Assert.That(sink.Events[0].Find("context")!.Value.Text, Is.EqualTo("noads_prompt"));
            Assert.That(analytics.Violations, Is.Empty);

            Assert.That(AnalyticsStoreService.ResultId(PurchaseStatus.AlreadyOwned), Is.EqualTo("failed"));
            Assert.That(AnalyticsStoreService.ResultId(PurchaseStatus.Failed), Is.EqualTo("failed"));
            Assert.That(AnalyticsStoreService.ResultId(PurchaseStatus.Pending), Is.EqualTo("pending"), "F3-3: pagada, sin verificar (08 §8.2)");
            Assert.That(AnalyticsStoreService.ResultId(PurchaseStatus.VerifyFailed), Is.EqualTo("verify_failed"), "F3-3: rechazada por el servidor");
            Assert.That(AnalyticsStoreService.ContextId(PurchaseContext.NoCanoes), Is.EqualTo("no_canoes"));
            Assert.That(AnalyticsStoreService.ContextId(PurchaseContext.StarterWindow), Is.EqualTo("starter_window"));
            Assert.That(AnalyticsStoreService.ContextId(PurchaseContext.Piggy), Is.EqualTo("piggy"));
        }

        [Test]
        public void TheVerifiedStore_LogsALivePurchaseOnce_AndALateCreditWithoutContext()
        {
            // F3-5: con la tienda verificada, af_iap_result sigue saliendo una vez por compra abierta (por su resultado) y, para lo que
            // se acredita después desde la cola (IsLive false), sale ok sin context (la cola no lo guarda: no se inventa) y purchase.
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var clock = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var store = new MemoryProfileStore();
            var wallet = new Wallet(profile, store, clock);
            var ledger = new StoreLedger(profile, wallet, store, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new AF.Backend.FakeBackendClient(clock) { GrantFor = r => CatalogGrants.For(catalog, r.Sku) };
            var verified = new VerifiedStoreService(ledger, new FakeBillingClient(catalog, "USD", null, clock), backend, clock);
            var service = new AnalyticsStoreService(verified, analytics);

            PurchaseResult? live = null;
            service.Purchase(new PurchaseRequest("af_coins_2", PurchaseContext.Shop), r => live = r);
            Assert.That(live!.IsPurchased, Is.True);
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result", "purchase" }), "una vez: los eventos de la verificación en vivo no se repiten");

            sink.Clear();
            backend.FailNext(new AF.Backend.BackendError(AF.Backend.BackendErrorKind.Network));
            PurchaseResult? pending = null;
            service.Purchase(new PurchaseRequest("af_coins_3", PurchaseContext.Stuck, 31), r => pending = r);
            Assert.That(pending!.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result" }));
            Assert.That(sink.Events[0].Find("result")!.Value.Text, Is.EqualTo("pending"));

            sink.Clear();
            clock.Advance(TimeSpan.FromHours(1));
            verified.Pump();
            Assert.That(wallet.Seeds, Is.EqualTo(1000 + catalog.Get("af_coins_3").Content.Seeds), "acreditada una vez, tarde");
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result", "purchase" }));
            Assert.That(sink.Events[0].ToJson(), Is.EqualTo("{\"event\":\"af_iap_result\",\"sku\":\"af_coins_3\",\"result\":\"ok\"}"));
            Assert.That(analytics.Violations, Is.Empty);
        }

        [Test]
        public void ALateCredit_IsLoggedAlsoBehindConsentAndTheReadyStore()
        {
            // D-040: en el juego la tienda verificada va envuelta por el consentimiento (B12) y por la tienda lista de Play; lo que se
            // acredita tarde (un pago pendiente que Play cobra después, la cola) también sale como af_iap_result ok y purchase (08 §8.2).
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var clock = new ManualClock(new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var store = new MemoryProfileStore();
            var wallet = new Wallet(profile, store, clock);
            var ledger = new StoreLedger(profile, wallet, store, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new AF.Backend.FakeBackendClient(clock) { GrantFor = r => CatalogGrants.For(catalog, r.Sku) };
            var verified = new VerifiedStoreService(ledger, new FakeBillingClient(catalog, "USD", null, clock), backend, clock);
            var consent = new ConsentStoreService(new BillingReadyStoreService(verified, new StubAvailability()));
            consent.Apply(ConsentPolicy.Flags(
                new ConsentRecord(LegalProfiles.Co1581, 1, new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc), true, true, AgeBand.Adult),
                ConsentConfig.Defaults));
            var service = new AnalyticsStoreService(consent, analytics);

            backend.FailNext(new AF.Backend.BackendError(AF.Backend.BackendErrorKind.Network));
            PurchaseResult? pending = null;
            service.Purchase(new PurchaseRequest("af_coins_3", PurchaseContext.Shop), r => pending = r);
            Assert.That(pending!.Status, Is.EqualTo(PurchaseStatus.Pending));

            sink.Clear();
            clock.Advance(TimeSpan.FromHours(1));
            verified.Pump();
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result", "purchase" }), "se oye a la tienda verificada bajo los envoltorios");
            Assert.That(sink.Events[0].ToJson(), Is.EqualTo("{\"event\":\"af_iap_result\",\"sku\":\"af_coins_3\",\"result\":\"ok\"}"));
        }

        [Test]
        public void ALateCreditBeforePlayHasItsPrices_IsLoggedWithoutInventingTheRevenue()
        {
            // D-040: si la cola acredita antes de que Play dé sus precios, el de 05 §4.2 sería otra moneda: purchase sale sin value.
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var clock = new ManualClock(new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var store = new MemoryProfileStore();
            var wallet = new Wallet(profile, store, clock);
            var ledger = new StoreLedger(profile, wallet, store, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new AF.Backend.FakeBackendClient(clock) { GrantFor = r => CatalogGrants.For(catalog, r.Sku) };
            var verified = new VerifiedStoreService(ledger, new FakeBillingClient(catalog, "USD", null, clock), backend, clock);
            var availability = new StubAvailability();
            var service = new AnalyticsStoreService(new BillingReadyStoreService(verified, availability), analytics);

            backend.FailNext(new AF.Backend.BackendError(AF.Backend.BackendErrorKind.Network));
            service.Purchase(new PurchaseRequest("af_coins_3", PurchaseContext.Shop), _ => { });
            sink.Clear();
            availability.Sell = false; // reinicio: Play aún no respondió con sus precios
            clock.Advance(TimeSpan.FromHours(1));
            verified.Pump();

            Assert.That(sink.Names, Is.EqualTo(new[] { "af_iap_result", "purchase" }));
            Assert.That(sink.Events[1].ToJson(), Is.EqualTo("{\"event\":\"purchase\",\"items\":\"af_coins_3\"}"));
            Assert.That(analytics.Violations, Is.Empty);
        }

        [Test]
        public void ANoAdsRestoredFromPlay_IsARestorationNotARevenue()
        {
            // D-040: reinstalar con la misma cuenta de Google y el mismo UID: Play devuelve Sin anuncios, el servidor dice duplicate y se
            // acredita; no es un ingreso nuevo (af_entitlement_restored, 08 §8.2), ni af_iap_result ni purchase.
            (AnalyticsService analytics, FakeAnalyticsSink sink) = NewAnalytics();
            var clock = new ManualClock(new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile first = Wallet.NewProfile(clock);
            var firstStore = new MemoryProfileStore();
            var firstWallet = new Wallet(first, firstStore, clock);
            var firstLedger = new StoreLedger(first, firstWallet, firstStore, clock);
            StoreCatalog catalog = firstLedger.Catalog;
            var backend = new AF.Backend.FakeBackendClient(clock)
            {
                GrantFor = r => CatalogGrants.For(catalog, r.Sku),
                EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku),
            };
            var billing = new FakeBillingClient(catalog, "USD", null, clock);
            var firstStoreService = new VerifiedStoreService(firstLedger, billing, backend, clock);
            firstStoreService.Purchase(new PurchaseRequest("af_noads", PurchaseContext.Shop), _ => { });
            Assert.That(first.Entitlements.NoAds, Is.True);

            Profile fresh = Wallet.NewProfile(clock);
            var freshStore = new MemoryProfileStore();
            var freshLedger = new StoreLedger(fresh, new Wallet(fresh, freshStore, clock), freshStore, clock);
            var verified = new VerifiedStoreService(freshLedger, billing, backend, clock);
            var service = new AnalyticsStoreService(new ConsentStoreService(verified), analytics);
            billing.RestoreOwnedOnNewDevice();
            verified.Resume();

            Assert.That(fresh.Entitlements.NoAds, Is.True, "restaurado");
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_entitlement_restored" }));
            Assert.That(sink.Events[0].ToJson(), Is.EqualTo("{\"event\":\"af_entitlement_restored\",\"type\":\"no_ads\",\"count\":1}"));
            Assert.That(service.Inner, Is.Not.Null);
        }

        private sealed class StubAvailability : IBillingAvailability
        {
            public bool Sell { get; set; } = true;

            public bool IsReady => true;

            public bool CanSell(string sku) => Sell;

            public bool HasStorePrice(string sku) => Sell;
        }

        // Una compra cancelada de verdad, con el presentador que dice que no.
        private static PurchaseResult PurchaseResultFor(AnalyticsStoreService service, string sku, bool confirm)
        {
            var clock = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var store = new MemoryProfileStore();
            var wallet = new Wallet(profile, store, clock);
            var inner = new FakeStoreService(new StoreLedger(profile, wallet, store, clock), new ImmediatePurchasePresenter(confirm), "USD");
            PurchaseResult? result = null;
            inner.Purchase(new PurchaseRequest(sku), r => result = r);
            return result!;
        }
    }
}
