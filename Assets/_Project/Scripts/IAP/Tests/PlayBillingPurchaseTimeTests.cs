#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Con el plugin propio, Play da la hora de la compra (<c>purchaseTime</c>): <see cref="PlayBillingClient"/> la pasa a la
    /// <see cref="BillingTransaction"/>, y de ahí la cola de verificación cuenta las 48 h del <c>deferred_ack</c> desde la compra y no desde
    /// que entra en la cola (08 §10.3; D-040, añadido de Q-IAP-1).
    /// </summary>
    public sealed class PlayBillingPurchaseTimeTests
    {
        private static readonly DateTime Bought = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void PurchaseTime_ReachesTheTransaction_FromAQueryAndFromALiveUpdate()
        {
            var port = new FakePlayStorePort().PriceAll(StoreCatalog.Default);
            var client = new PlayBillingClient(port, StoreCatalog.Default, "USD");
            client.Start();
            Assert.That(client.State, Is.EqualTo(PlayBillingState.Ready));

            port.Push(new PlayOrder("af_coins_2", "play.token.0901", PlayOrderState.Purchased, Bought));
            port.Push(new PlayOrder("af_noads", "play.token.0902", PlayOrderState.Confirmed, Bought.AddHours(1)));
            port.Push(new PlayOrder("af_coins_3", "play.token.0903", PlayOrderState.Purchased));

            IReadOnlyList<BillingTransaction> unfinished = client.UnfinishedTransactions();
            Assert.That(unfinished, Has.Count.EqualTo(3));
            Assert.That(unfinished[0].PurchasedUtc, Is.EqualTo(Bought));
            Assert.That(unfinished[1].PurchasedUtc, Is.EqualTo(Bought.AddHours(1)));
            Assert.That(unfinished[2].PurchasedUtc, Is.Null, "sin purchaseTime, la cola cuenta desde que entra");
        }

        [Test]
        public void PlayOrder_RejectsALocalTime()
        {
            Assert.That(() => new PlayOrder("af_coins_2", "play.token.0001", PlayOrderState.Purchased, new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Local)),
                Throws.ArgumentException);
        }
    }
}
