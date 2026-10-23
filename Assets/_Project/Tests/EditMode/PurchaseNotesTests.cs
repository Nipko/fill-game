#nullable enable
using System;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// F3-5: las notas de compra de la UI (<see cref="PurchaseNotes"/>) siguen a la tienda verificada: «Pendiente de confirmar»
    /// mientras espera al servidor, «No se pudo confirmar la compra» si la rechaza (un aviso de una vez) y nada al acreditarse; y
    /// <see cref="MarketText.PurchaseNote"/> da el texto de cada una.
    /// </summary>
    public sealed class PurchaseNotesTests
    {
        private static (VerifiedStoreService store, FakeBackendClient backend, ManualClock clock) NewStore()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var disk = new MemoryProfileStore();
            var wallet = new Wallet(profile, disk, clock);
            var ledger = new StoreLedger(profile, wallet, disk, clock);
            StoreCatalog catalog = ledger.Catalog;
            var backend = new FakeBackendClient(clock) { GrantFor = r => CatalogGrants.For(catalog, r.Sku) };
            return (new VerifiedStoreService(ledger, new FakeBillingClient(catalog, "USD", null, clock), backend, clock), backend, clock);
        }

        [Test]
        public void TheNotes_FollowTheVerification()
        {
            (VerifiedStoreService store, FakeBackendClient backend, ManualClock clock) = NewStore();
            var notes = new PurchaseNotes();
            store.VerificationChanged += notes.Track;

            backend.FailNext(new BackendError(BackendErrorKind.Network));
            store.Purchase(new PurchaseRequest("af_coins_2"), _ => { });
            Assert.That(notes.Of("af_coins_2"), Is.EqualTo(PurchaseNoteKind.Pending));
            Assert.That(notes.Of("af_coins_2", StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.Pending));
            int version = notes.Version;

            clock.Advance(TimeSpan.FromHours(1));
            store.Pump();
            Assert.That(notes.Of("af_coins_2"), Is.EqualTo(PurchaseNoteKind.None), "acreditada: sin nota");
            Assert.That(notes.Version, Is.GreaterThan(version));

            backend.FailNext(new BackendError(BackendErrorKind.Http, 400, ApiErrorCode.BadRequest, "voided"));
            store.Purchase(new PurchaseRequest(StoreCatalog.Rescue, PurchaseContext.Stuck, 40), _ => { });
            Assert.That(notes.Of(StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.Failed));
            Assert.That(notes.Of(StoreCatalog.Handful, StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.Failed), "el fallo pesa más");

            notes.ClearFailures();
            Assert.That(notes.Of(StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.None));
        }

        [Test]
        public void WhileTheServerAnswers_TheLivePurchaseSaysConfirming()
        {
            // 08 §10.3: «estado "verifying"; UI muestra "Confirmando…" (≤3 s)»; sin respuesta, pasa a «Pendiente de confirmar».
            (VerifiedStoreService store, FakeBackendClient backend, _) = NewStore();
            var notes = new PurchaseNotes();
            store.VerificationChanged += notes.Track;
            backend.HoldVerifyResponses = true;
            store.Purchase(new PurchaseRequest("af_coins_2"), _ => { });
            Assert.That(store.IsPurchasing, Is.True);
            Assert.That(notes.Of("af_coins_2"), Is.EqualTo(PurchaseNoteKind.Verifying));
            Assert.That(notes.Of(StoreCatalog.Handful, StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.Verifying), "el modal la ve");
            backend.ReleaseVerifyResponses();
            Assert.That(notes.Of("af_coins_2"), Is.EqualTo(PurchaseNoteKind.None), "acreditada: sin nota");
            Assert.That(store.IsPurchasing, Is.False);

            backend.HoldVerifyResponses = false;
            backend.FailNext(new BackendError(BackendErrorKind.Network));
            store.Purchase(new PurchaseRequest(StoreCatalog.Rescue, PurchaseContext.Stuck, 40), _ => { });
            Assert.That(notes.Of(StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.Pending), "sin respuesta: pendiente");
            backend.HoldVerifyResponses = true;
            store.Purchase(new PurchaseRequest("af_coins_2"), _ => { });
            Assert.That(notes.Of(StoreCatalog.Handful, StoreCatalog.Rescue), Is.EqualTo(PurchaseNoteKind.Verifying),
                "lo que se confirma ahora pesa más que lo pendiente");
        }

        [Test]
        public void EveryNote_HasItsText()
        {
            Assert.That(MarketText.PurchaseNote(PurchaseNoteKind.Verifying), Is.EqualTo(MarketText.PurchaseVerifying));
            Assert.That(MarketText.Keys.PurchaseVerifying, Is.EqualTo("iap_verifying"));
            Assert.That(MarketText.PurchaseNote(PurchaseNoteKind.None), Is.Null);
            Assert.That(MarketText.PurchaseNote(PurchaseNoteKind.Pending), Is.EqualTo(MarketText.PurchasePending));
            Assert.That(MarketText.PurchaseNote(PurchaseNoteKind.Failed), Is.EqualTo(MarketText.PurchaseFailed));
            Assert.That(MarketText.Keys.PurchasePending, Is.EqualTo("iap_pending_confirm"));
            Assert.Throws<ArgumentNullException>(() => new PurchaseNotes().Track(null!));
        }
    }
}
