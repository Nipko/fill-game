#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;
using AF.Platform;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>F3-1: <see cref="FakeBackendClient"/> imita la semántica del contrato v1.</summary>
    public sealed class FakeBackendClientTests
    {
        private const string Nonce1 = "6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f";
        private const string Nonce2 = "0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d";

        private static T Sync<T>(Action<Action<BackendResult<T>>> call)
            where T : class
        {
            BackendResult<T>? result = null;
            call(r => result = r);
            Assert.That(result, Is.Not.Null, "el falso responde antes de volver");
            Assert.That(result!.IsOk, Is.True, result.ToString());
            return result.Value!;
        }

        [Test]
        public void TokenHashIsSha256Hex()
        {
            Assert.That(PurchaseTokenHash.Of("abc"), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
            Assert.That(ContractRules.IsTokenHash(PurchaseTokenHash.Of("opaque-token")), Is.True);
        }

        [Test]
        public void VerifyIsOkOnceThenDuplicate()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc));
            var fake = new FakeBackendClient(clock) { GrantFor = r => new PurchaseGrant(r.Sku == "af_coins_2" ? 1200 : 0) };
            var request = new VerifyPurchaseRequest("tok-1", "af_coins_2");

            VerifyPurchaseResponse first = Sync<VerifyPurchaseResponse>(cb => fake.VerifyPurchase(request, cb));
            VerifyPurchaseResponse second = Sync<VerifyPurchaseResponse>(cb => fake.VerifyPurchase(request, cb));

            Assert.That(first.Status, Is.EqualTo(VerifyStatus.Ok));
            Assert.That(first.Grant!.Seeds, Is.EqualTo(1200));
            Assert.That(first.TokenHash, Is.EqualTo(PurchaseTokenHash.Of("tok-1")));
            Assert.That(second.Status, Is.EqualTo(VerifyStatus.Duplicate));
            Assert.That(second.TokenHash, Is.EqualTo(first.TokenHash));

            RestoreLedgerResponse ledger = Sync<RestoreLedgerResponse>(cb => fake.RestoreLedger(new RestoreLedgerRequest(), cb));
            Assert.That(ledger.Purchases, Has.Count.EqualTo(1));
            Assert.That(ledger.Purchases[0].Sku, Is.EqualTo("af_coins_2"));
            Assert.That(ledger.Purchases[0].TsMs, Is.EqualTo(1792540800000L));
        }

        [Test]
        public void PendingPurchasesAreNotGranted()
        {
            var fake = new FakeBackendClient();
            fake.MarkPending("tok-p");
            Assert.That(Sync<VerifyPurchaseResponse>(cb => fake.VerifyPurchase(new VerifyPurchaseRequest("tok-p", "af_noads"), cb)).Status,
                Is.EqualTo(VerifyStatus.Pending));
            Assert.That(fake.LedgerCount, Is.EqualTo(0));
            fake.ClearPending("tok-p");
            Assert.That(Sync<VerifyPurchaseResponse>(cb => fake.VerifyPurchase(new VerifyPurchaseRequest("tok-p", "af_noads"), cb)).Status,
                Is.EqualTo(VerifyStatus.Ok));
        }

        [Test]
        public void RestoreConfirmsOnlyNoncesTheSsvReceived()
        {
            var fake = new FakeBackendClient();
            Assert.That(fake.ReceiveSsv(new SsvCallback("kT3x9QeVb2Rz8LmN4pWs7YcA1dF5", "rescue", Nonce1)).IsDuplicate, Is.False);
            Assert.That(fake.ReceiveSsv(new SsvCallback("kT3x9QeVb2Rz8LmN4pWs7YcA1dF5", "rescue", Nonce1)).IsDuplicate, Is.True);
            Assert.That(fake.ReceiveSsv(SsvCallback.WithoutIdentity("tx-1")).IsIgnored, Is.True, "sin user_id ni custom_data: ignored (D-049)");

            RestoreLedgerResponse r = Sync<RestoreLedgerResponse>(cb => fake.RestoreLedger(new RestoreLedgerRequest(new[] { Nonce1, Nonce2 }), cb));
            Assert.That(r.ConfirmedNonces, Is.EqualTo(new[] { Nonce1 }));
        }

        [Test]
        public void VoidedJobMarksThePurchase()
        {
            var fake = new FakeBackendClient();
            Sync<VerifyPurchaseResponse>(cb => fake.VerifyPurchase(new VerifyPurchaseRequest("tok-v", "af_noads"), cb));
            VoidedPurchasesJobOutput job = fake.RunVoidedPurchasesJob(new[] { "tok-v", "tok-unknown" }, 1792540800000L);

            Assert.That(job.Voided, Is.EqualTo(1));
            LedgerPurchase p = Sync<RestoreLedgerResponse>(cb => fake.RestoreLedger(new RestoreLedgerRequest(), cb)).Purchases[0];
            Assert.That(p.State, Is.EqualTo(LedgerPurchaseState.Voided));
            Assert.That(p.VoidedAtMs, Is.EqualTo(1792540800000L));
        }

        [Test]
        public void AccountCodesFollowTheContractAndRotate()
        {
            var fake = new FakeBackendClient();
            var codes = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                string code = Sync<AccountCodeResponse>(cb => fake.GetAccountCode(cb)).Code;
                Assert.That(ContractRules.IsAccountCode(code), Is.True, code);
                Assert.That(codes.Add(code), Is.True, "cada llamada emite uno nuevo");
                Assert.That(fake.CurrentAccountCode, Is.EqualTo(code));
            }
        }

        [Test]
        public void DeleteClearsTheAccountAndIsIdempotent()
        {
            var fake = new FakeBackendClient();
            Sync<VerifyPurchaseResponse>(cb => fake.VerifyPurchase(new VerifyPurchaseRequest("tok-d", "af_noads"), cb));
            Sync<AccountCodeResponse>(cb => fake.GetAccountCode(cb));

            Assert.That(Sync<DeleteAccountResponse>(cb => fake.DeleteAccount(cb)), Is.SameAs(DeleteAccountResponse.Deleted));
            Assert.That(Sync<DeleteAccountResponse>(cb => fake.DeleteAccount(cb)), Is.SameAs(DeleteAccountResponse.Deleted));
            Assert.That(fake.LedgerCount, Is.EqualTo(0));
            Assert.That(fake.CurrentAccountCode, Is.Null);
            Assert.That(fake.Deletions, Is.EqualTo(2));
        }

        [Test]
        public void FailNextFailsTheNextCalls()
        {
            var fake = new FakeBackendClient();
            fake.FailNext(BackendError.Network("offline"), 2);
            BackendResult<AccountCodeResponse>? r = null;
            fake.GetAccountCode(x => r = x);
            Assert.That(r!.Error!.Kind, Is.EqualTo(BackendErrorKind.Network));
            fake.DeleteAccount(x => Assert.That(x.IsOk, Is.False));
            Assert.That(Sync<AccountCodeResponse>(cb => fake.GetAccountCode(cb)), Is.Not.Null);
            Assert.That(fake.Calls["getAccountCode"], Is.EqualTo(2));
        }

        [Test]
        public void UnavailableClientNeverGrants()
        {
            var client = new UnavailableBackendClient();
            BackendResult<VerifyPurchaseResponse>? r = null;
            client.VerifyPurchase(new VerifyPurchaseRequest("tok", "af_noads"), x => r = x);
            Assert.That(r!.IsOk, Is.False);
            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.NotConfigured));
        }

        [Test]
        public void RequestsRejectValuesOutsideTheContract()
        {
            Assert.That(() => new VerifyPurchaseRequest("", "af_noads"), Throws.ArgumentException);
            Assert.That(() => new VerifyPurchaseRequest("tok", "coins"), Throws.ArgumentException);
            Assert.That(() => new VerifyPurchaseRequest("tok with spaces", "af_noads"), Throws.ArgumentException);
            Assert.That(() => new VerifyPurchaseRequest("tok", "af_noads", new string('a', 65)), Throws.ArgumentException);
            Assert.That(() => new RestoreLedgerRequest(new[] { "not-a-uuid" }), Throws.ArgumentException);
            Assert.That(() => new RestoreLedgerRequest(new[] { Nonce1, Nonce1 }), Throws.ArgumentException);
            Assert.That(() => new RestoreLedgerRequest(new[] { Nonce1.ToUpperInvariant() }), Throws.ArgumentException);
            Assert.That(ContractRules.IsSku("af_noads\n"), Is.False, "el ancla \\z no admite un salto final");
        }
    }
}
