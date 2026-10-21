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
            Assert.That(AnalyticsStoreService.ContextId(PurchaseContext.NoCanoes), Is.EqualTo("no_canoes"));
            Assert.That(AnalyticsStoreService.ContextId(PurchaseContext.StarterWindow), Is.EqualTo("starter_window"));
            Assert.That(AnalyticsStoreService.ContextId(PurchaseContext.Piggy), Is.EqualTo("piggy"));
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
