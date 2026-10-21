#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// La tienda detrás del consentimiento y la edad (bloque B12; 08 §7: &lt;13 «sin IAP»; Q-T4 (a): con 13-17, doble confirmación
    /// en el juego antes de la hoja de Google Play; 05 §4.1: una compra cancelada nunca descuenta).
    /// </summary>
    public sealed class ConsentStoreTests
    {
        private static readonly DateTime At = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        private static ConsentFlags FlagsFor(AgeBand band)
        {
            var record = new ConsentRecord(LegalProfiles.Co1581, 1, At, true, true, band);
            return ConsentPolicy.Flags(record, ConsentConfig.Defaults);
        }

        private static PurchaseResult Buy(IStoreService store, string sku = "af_coins_2")
        {
            PurchaseResult? result = null;
            store.Purchase(new PurchaseRequest(sku), r => result = r);
            Assert.That(result, Is.Not.Null, "onDone siempre se llama");
            return result!;
        }

        [Test]
        public void WithoutFlags_NothingCanBeBought_FailsClosed()
        {
            var fx = new StoreFixture();
            var store = new ConsentStoreService(fx.Service);

            Assert.That(store.IsAvailable, Is.False);
            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.StoreUnavailable), "sin decidir, sin compras");
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0));
        }

        [Test]
        public void Teen_WhatCannotBeBought_IsNeverConfirmed()
        {
            var fx = new StoreFixture();
            var confirm = new ImmediatePurchaseConfirmation();
            var store = new ConsentStoreService(fx.Service) { Confirmation = confirm, Precheck = (p, r) => fx.Ledger.CheckPurchasable(p, r) };
            store.Apply(FlagsFor(AgeBand.Teen));
            Assert.That(Buy(store, "af_starter").IsPurchased, Is.True);
            Assert.That(confirm.Asked, Is.EqualTo(1));

            Assert.That(Buy(store, "af_starter").Status, Is.EqualTo(PurchaseStatus.AlreadyOwned));
            Assert.That(confirm.Asked, Is.EqualTo(1), "la Mochila ya comprada no pide confirmación con precio");
            Assert.That(fx.Presenter.Presented, Is.EqualTo(1));
        }

        [Test]
        public void Under13_HasNoPurchases_AndNothingOpens()
        {
            var fx = new StoreFixture();
            var store = new ConsentStoreService(fx.Service) { Confirmation = new ImmediatePurchaseConfirmation() };
            store.Apply(FlagsFor(AgeBand.Under13));

            Assert.That(store.IsAvailable, Is.False);
            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0), "ni la hoja de Google Play");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void Undecided_HasNoPurchases()
        {
            var fx = new StoreFixture();
            var store = new ConsentStoreService(fx.Service);
            store.Apply(ConsentPolicy.Undecided);

            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0));
        }

        [Test]
        public void Teen_AsksASecondTime_ThenOpensGooglePlay()
        {
            var fx = new StoreFixture();
            var confirm = new ImmediatePurchaseConfirmation(answer: true);
            var store = new ConsentStoreService(fx.Service) { Confirmation = confirm };
            store.Apply(FlagsFor(AgeBand.Teen));

            PurchaseResult result = Buy(store, "af_coins_3");

            Assert.That(confirm.Asked, Is.EqualTo(1), "Q-T4 (a): la segunda confirmación en el juego");
            Assert.That(confirm.Last!.Sku, Is.EqualTo("af_coins_3"));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(1), "después, la hoja de Google Play (simulada)");
            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2800));
        }

        [Test]
        public void Teen_CancellingTheSecondConfirmation_ChargesNothing_AndNeverOpensGooglePlay()
        {
            var fx = new StoreFixture();
            var store = new ConsentStoreService(fx.Service) { Confirmation = new ImmediatePurchaseConfirmation(answer: false) };
            store.Apply(FlagsFor(AgeBand.Teen));

            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(store.IsPurchasing, Is.False);
        }

        [Test]
        public void Teen_WithoutAConfirmationView_Cancels()
        {
            var fx = new StoreFixture();
            var store = new ConsentStoreService(fx.Service);
            store.Apply(FlagsFor(AgeBand.Teen));

            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(0));
        }

        [Test]
        public void Teen_WhileTheConfirmationIsOpen_ASecondPurchaseIsBusy()
        {
            var fx = new StoreFixture();
            var held = new HeldConfirmation();
            var store = new ConsentStoreService(fx.Service) { Confirmation = held };
            store.Apply(FlagsFor(AgeBand.Teen));

            PurchaseResult? first = null;
            store.Purchase(new PurchaseRequest("af_coins_2"), r => first = r);
            Assert.That(store.IsPurchasing, Is.True);
            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.Busy));

            held.Answer!(true);
            held.Answer(true); // una sola respuesta cuenta
            Assert.That(first!.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(store.IsPurchasing, Is.False);
        }

        [Test]
        public void Teen_WithPersonalizationAllowedByRemoteConfig_HasNoSecondConfirmation()
        {
            var fx = new StoreFixture();
            var confirm = new ImmediatePurchaseConfirmation();
            var store = new ConsentStoreService(fx.Service) { Confirmation = confirm };
            var config = ConsentConfig.Defaults;
            config.BizUnder18NoPersonalization = false;
            store.Apply(ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.Default, 1, At, true, true, AgeBand.Teen), config));

            Assert.That(Buy(store).IsPurchased, Is.True);
            Assert.That(confirm.Asked, Is.EqualTo(0));
        }

        [Test]
        public void Adult_BuysWithoutTheSecondConfirmation()
        {
            var fx = new StoreFixture();
            var confirm = new ImmediatePurchaseConfirmation();
            var store = new ConsentStoreService(fx.Service) { Confirmation = confirm };
            store.Apply(FlagsFor(AgeBand.Adult));

            Assert.That(Buy(store).IsPurchased, Is.True);
            Assert.That(confirm.Asked, Is.EqualTo(0));
            Assert.That(store.IsAvailable, Is.True);
        }

        [Test]
        public void TheStoreTurnedOff_AnswersAsBefore_WithoutAskingTwice()
        {
            var config = StoreConfig.Defaults;
            config.BizIapEnabled = false;
            var fx = new StoreFixture(config);
            var confirm = new ImmediatePurchaseConfirmation();
            var store = new ConsentStoreService(fx.Service) { Confirmation = confirm };
            store.Apply(FlagsFor(AgeBand.Teen));

            Assert.That(Buy(store).Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(confirm.Asked, Is.EqualTo(0));
        }

        private sealed class HeldConfirmation : IPurchaseConfirmation
        {
            public Action<bool>? Answer { get; private set; }

            public void Confirm(StoreProduct product, LocalPrice price, Action<bool> onAnswer)
            {
                Answer = onAnswer;
            }
        }
    }
}
