#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Prueba del 2026-10-23, observación 1: con Play conectada pero sin ningún SKU con el precio de la tienda (la app aún no está en
    /// Play Console), la tienda no está disponible (el Mercado, sus accesos y las ofertas, como cuando Play no conecta; 05 §4.1: «precio
    /// local con impuestos visible antes de comprar»); con uno, sí. Y una compra que acaba en <see cref="PurchaseStatus.StoreUnavailable"/>
    /// pide el aviso (<see cref="UnavailableNoticeStoreService"/>).
    /// </summary>
    public sealed class StoreWithoutPricesTests
    {
        private sealed class Availability : IBillingAvailability
        {
            public bool Ready { get; set; } = true;

            public HashSet<string> Priced { get; } = new HashSet<string>(StringComparer.Ordinal);

            public bool IsReady => Ready;

            public bool CanSell(string sku) => Ready && Priced.Contains(sku);

            public bool HasStorePrice(string sku) => Priced.Contains(sku);
        }

        private static (BillingReadyStoreService store, Availability billing) NewStore()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var disk = new MemoryProfileStore();
            var wallet = new Wallet(profile, disk, clock);
            var ledger = new StoreLedger(profile, wallet, disk, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new AF.Backend.FakeBackendClient(clock) { GrantFor = r => CatalogGrants.For(catalog, r.Sku) };
            var verified = new VerifiedStoreService(ledger, new FakeBillingClient(catalog, "USD", null, clock), backend, clock);
            var billing = new Availability();
            return (new BillingReadyStoreService(verified, billing), billing);
        }

        [Test]
        public void PlayReady_WithoutAnySkuPriced_TheStoreIsUnavailable()
        {
            (BillingReadyStoreService store, Availability billing) = NewStore();

            Assert.That(billing.IsReady, Is.True, "Play conectada");
            Assert.That(store.IsAvailable, Is.False, "ningún SKU con el precio de la tienda: no hay Mercado");
        }

        [Test]
        public void PlayReady_WithOneSkuPriced_TheStoreIsAvailable_AndOnlyThatOneSells()
        {
            (BillingReadyStoreService store, Availability billing) = NewStore();
            billing.Priced.Add(StoreCatalog.Handful);

            Assert.That(store.IsAvailable, Is.True, "con un precio de la tienda, el Mercado aparece en la misma sesión");

            PurchaseResult? other = null;
            store.Purchase(new PurchaseRequest("af_coins_6"), r => other = r);
            Assert.That(other!.Status, Is.EqualTo(PurchaseStatus.StoreUnavailable), "un SKU sin su precio sigue sin venderse");

            PurchaseResult? bought = null;
            store.Purchase(new PurchaseRequest(StoreCatalog.Handful), r => bought = r);
            Assert.That(bought!.IsPurchased, Is.True);
        }

        [Test]
        public void PricesWithoutPlayReady_TheStoreIsUnavailable()
        {
            (BillingReadyStoreService store, Availability billing) = NewStore();
            billing.Priced.Add(StoreCatalog.Handful);
            billing.Ready = false;

            Assert.That(store.IsAvailable, Is.False);
        }

        [Test]
        public void AStoreUnavailablePurchase_AsksForTheNotice_AfterTheCaller()
        {
            (BillingReadyStoreService ready, Availability billing) = NewStore();
            var order = new List<string>();
            var store = new UnavailableNoticeStoreService(ready, () => order.Add("notice"));

            store.Purchase(new PurchaseRequest(StoreCatalog.Handful), r => order.Add(r.Status.ToString()));
            Assert.That(order, Is.EqualTo(new[] { nameof(PurchaseStatus.StoreUnavailable), "notice" }));

            order.Clear();
            billing.Priced.Add(StoreCatalog.Handful);
            store.Purchase(new PurchaseRequest(StoreCatalog.Handful), r => order.Add(r.Status.ToString()));
            Assert.That(order, Is.EqualTo(new[] { nameof(PurchaseStatus.Purchased) }), "una compra hecha no avisa");
            Assert.That(store.IsAvailable, Is.True);
            Assert.That(store.Inner, Is.SameAs(ready));
        }
    }
}
