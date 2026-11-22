#nullable enable
using System;
using AF.Backend.Contract;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>
    /// verifyPurchase con una compra del App Store (fase M3 de <c>mac-ios.md</c>): <c>store = app_store</c> y la transacción firmada de
    /// StoreKit 2 en <c>signed_transaction</c>, que va si y solo si la tienda es el App Store. Sin <c>store</c>, Google Play como siempre.
    /// </summary>
    public sealed class VerifyPurchaseAppStoreTests
    {
        private const string Jws = "eyJhbGciOiJFUzI1NiJ9.eyJ0cmFuc2FjdGlvbklkIjoiMjAwMDAwMDk4NzY1NDMyMSJ9.c2lnbmF0dXJl";
        private const string Tx = "2000000987654321";

        [Test]
        public void WithAppStore_WritesStoreAndSignedTransaction_AndReadsBack()
        {
            VerifyPurchaseRequest r = new VerifyPurchaseRequest(Tx, "af_coins_2").WithAppStore(Jws);
            string json = r.ToJson();
            StringAssert.Contains("\"store\":\"app_store\"", json);
            StringAssert.Contains("\"signed_transaction\":\"" + Jws + "\"", json);
            Assert.That(VerifyPurchaseRequest.TryParse(json, out VerifyPurchaseRequest? back, out string? error), Is.True, error);
            Assert.That(back!.Store, Is.EqualTo(ContractRules.StoreAppStore));
            Assert.That(back.SignedTransaction, Is.EqualTo(Jws));
            Assert.That(back.Token, Is.EqualTo(Tx));
        }

        [Test]
        public void WithoutStore_IsGooglePlayAsBefore()
        {
            string json = new VerifyPurchaseRequest("tok.play-123", "af_noads").ToJson();
            StringAssert.DoesNotContain("store", json);
            StringAssert.DoesNotContain("signed_transaction", json);
        }

        [Test]
        public void SignedTransaction_GoesIfAndOnlyIfTheStoreIsTheAppStore()
        {
            Assert.Throws<ArgumentException>(() => new VerifyPurchaseRequest(Tx, "af_coins_2", null, ContractRules.StoreAppStore, null));
            Assert.Throws<ArgumentException>(() => new VerifyPurchaseRequest(Tx, "af_coins_2", null, null, Jws));
            Assert.Throws<ArgumentException>(() => new VerifyPurchaseRequest(Tx, "af_coins_2", null, "amazon", null));
            Assert.Throws<ArgumentException>(() => new VerifyPurchaseRequest(Tx, "af_coins_2").WithAppStore("no es un jws"));
            Assert.That(VerifyPurchaseRequest.TryParse("{\"token\":\"" + Tx + "\",\"sku\":\"af_coins_2\",\"store\":\"app_store\"}", out _, out _), Is.False);
            Assert.That(VerifyPurchaseRequest.TryParse("{\"token\":\"" + Tx + "\",\"sku\":\"af_coins_2\",\"signed_transaction\":\"" + Jws + "\"}", out _, out _), Is.False);
            Assert.That(ContractRules.IsSignedTransaction(new string('a', ContractRules.SignedTransactionMaxLength)), Is.False, "no es un JWS");
            Assert.That(VerifyPurchaseRequest.TryParse("{\"token\":\"tok.abc\",\"sku\":\"af_noads\",\"store\":\"app_store\",\"signed_transaction\":\"" + Jws + "\"}",
                out _, out string? error), Is.False, "con app_store, token solo dígitos: false, sin lanzar");
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
    }
}
