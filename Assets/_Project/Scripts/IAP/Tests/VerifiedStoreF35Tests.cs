#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Lo que F3-5 añade a la tienda verificada para conectarla al juego: <see cref="PurchaseVerificationEvent.IsLive"/> (la UI y la
    /// analítica separan la compra abierta de lo que llega tarde), <see cref="VerifiedStoreService.IsAwaitingServer"/> (para que
    /// <c>restoreLedger</c> no pise la cola), un solo Cántaro pendiente a la vez (Q-B5-12) y los tokens de la tienda simulada, distintos
    /// en cada arranque (<c>applied_purchases</c> guarda su hash).
    /// </summary>
    public sealed class VerifiedStoreF35Tests
    {
        [Test]
        public void TheEvents_OfAnOpenPurchase_AreLive_AndALateCredit_IsNot()
        {
            var fx = new VerifiedStoreFixture();
            var live = new List<bool>();
            fx.Service.VerificationChanged += e => live.Add(e.IsLive);

            Assert.That(fx.Buy("af_coins_2").IsPurchased, Is.True);
            Assert.That(live, Is.EqualTo(new[] { true, true }), "verifying y credited, de la compra abierta");

            live.Clear();
            fx.Backend.FailNext(new BackendError(BackendErrorKind.Network));
            Assert.That(fx.Buy("af_coins_3").Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(live, Is.EqualTo(new[] { true, true }), "verifying y queued, aún con la compra abierta");

            live.Clear();
            fx.Clock.Advance(TimeSpan.FromHours(1));
            fx.Service.Pump();
            Assert.That(fx.Events[fx.Events.Count - 1], Is.EqualTo(PurchaseVerificationState.Credited));
            Assert.That(live, Is.EqualTo(new[] { false }), "acreditada desde la cola: no es de una compra abierta");
        }

        [Test]
        public void IsAwaitingServer_SeesTheQueueAndTheResponseInFlight()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.HoldVerifyResponses = true;
            Assert.That(fx.TryBuy("af_coins_2"), Is.Null);
            Assert.That(fx.Service.IsAwaitingServer(VerifiedStoreFixture.Hash(1)), Is.True);
            Assert.That(fx.Service.IsAwaitingServer(VerifiedStoreFixture.Hash(2)), Is.False);
            fx.Backend.ReleaseVerifyResponses();
            Assert.That(fx.Service.IsAwaitingServer(VerifiedStoreFixture.Hash(1)), Is.False, "acreditada: ya no espera");
            Assert.Throws<ArgumentNullException>(() => fx.Service.IsAwaitingServer(null!));
        }

        [Test]
        public void APendingPiggy_BlocksAnotherPiggy_UntilItIsCredited()
        {
            // Q-B5-12: el Cántaro no se vacía hasta acreditar; otro cobraría las mismas semillas otra vez.
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(new BackendError(BackendErrorKind.Network));
            Assert.That(fx.Buy(StoreCatalog.Piggy, piggySeeds: 1500).Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(fx.Buy(StoreCatalog.Piggy, piggySeeds: 1500).Status, Is.EqualTo(PurchaseStatus.Pending), "sin cobrar otro");
            Assert.That(fx.Billing.Requests, Has.Count.EqualTo(1), "Play no abrió una segunda compra");

            fx.Clock.Advance(TimeSpan.FromHours(1));
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1500), "acreditada una vez, con sus semillas");
            Assert.That(fx.Buy(StoreCatalog.Piggy, piggySeeds: 1200).IsPurchased, Is.True, "acreditada la primera, se puede comprar otro");
        }

        [Test]
        public void APiggyCreditedLate_SubtractsOnlyWhatWasCredited_AndKeepsTheSeedsThatCameInAfter()
        {
            // D-035, añadido de F3-5 (Q-B5-12, pregunta 3): al Cántaro acreditado tarde se le resta lo acreditado, no se vacía entero.
            var fx = new VerifiedStoreFixture();
            var records = new InMemoryOffersStore();
            var piggy = new PiggyJar(new OffersState(records), null, fx.Ledger.Config);
            fx.Service.VerificationChanged += e =>
            {
                if (!e.IsLive && e.State == PurchaseVerificationState.Credited)
                {
                    piggy.OnCreditedLater(e.Sku, e.CreditedPiggySeeds); // como GameBootstrap.OnVerificationChanged
                }
            };
            for (int level = 20; level < 120; level++)
            {
                piggy.OnCampaignVictory(level, RewardTier.Normal); // 100 victorias × 15 = 1.500
            }

            Assert.That(piggy.Seeds, Is.EqualTo(1500));
            fx.Backend.FailNext(new BackendError(BackendErrorKind.Network));
            PurchaseResult? result = null;
            fx.Service.Purchase(piggy.Request(PurchaseContext.Piggy, 120), r => result = r);
            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(piggy.OnPurchaseResult(result), Is.False);
            Assert.That(piggy.Seeds, Is.EqualTo(1500), "pendiente: guarda lo suyo");

            // Semillas que entran entre la compra y la acreditación: 15 + 30 + 40.
            piggy.OnCampaignVictory(120, RewardTier.Normal);
            piggy.OnCampaignVictory(121, RewardTier.Hard);
            piggy.OnCampaignVictory(122, RewardTier.VeryHard);
            Assert.That(piggy.Seeds, Is.EqualTo(1585));

            fx.Clock.Advance(TimeSpan.FromHours(1));
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1500), "se acredita lo de la compra, no lo que hay ahora");
            Assert.That(piggy.Seeds, Is.EqualTo(85), "conserva lo que entró después de la compra");
            Assert.That(records.Load().PiggySeeds, Is.EqualTo(85), "y lo guarda");

            Assert.That(piggy.OnCreditedLater("af_coins_2", 500), Is.False, "otro SKU no lo toca");
            Assert.That(piggy.OnCreditedLater(StoreCatalog.Piggy, 1000), Is.True);
            Assert.That(piggy.Seeds, Is.EqualTo(0), "nunca por debajo de 0");
            Assert.Throws<ArgumentOutOfRangeException>(() => piggy.OnCreditedLater(StoreCatalog.Piggy, -1));
        }

        [Test]
        public void FakeBilling_TokensCarryTheirPrefix()
        {
            var fx = new VerifiedStoreFixture();
            var billing = new FakeBillingClient(fx.Ledger.Catalog, "USD", null, fx.Clock, "fake.purchase.run7.");
            BillingResult? result = null;
            billing.Purchase("af_coins_2", null, r => result = r);
            Assert.That(result!.Transaction!.Token, Is.EqualTo("fake.purchase.run7.0001"));
            Assert.Throws<ArgumentException>(() => new FakeBillingClient(fx.Ledger.Catalog, "USD", null, fx.Clock, "con espacios "));
        }
    }
}
