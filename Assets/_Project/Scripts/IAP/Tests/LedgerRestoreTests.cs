#nullable enable
using System;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// F3-5, <c>restoreLedger</c> en el cliente (<see cref="LedgerRestore"/>; 08 §10.2, §10.3 y §11; D-033, Q-B8-1; D-038, añadido de la
    /// Balsa; plan §9, F3-4) con el backend falso: lo que falta en <c>applied_purchases</c> se acredita una sola vez; lo que la cola de
    /// este dispositivo aún verifica se le deja a ella; lo anulado se retira una vez sin bajar de 0; y el SSV se confirma o se revoca
    /// solo con una respuesta del servidor.
    /// </summary>
    public sealed class LedgerRestoreTests
    {
        private const string NonceA = "0f8b3c2e-1d4a-4b6c-8e9f-0a1b2c3d4e5f";
        private const string NonceB = "1a2b3c4d-5e6f-4a7b-9c8d-0e1f2a3b4c5d";

        private static LedgerRestore Restorer(VerifiedStoreFixture fx, out LocalRevocation revocation, IBackendClient? backend = null)
        {
            revocation = new LocalRevocation(fx.Profile, fx.Wallet, fx.Disk, fx.Clock);
            return new LedgerRestore(fx.Ledger, revocation, backend ?? fx.Backend, fx.Switch, fx.Service);
        }

        private static LedgerRestoreReport Run(LedgerRestore restore)
        {
            LedgerRestoreReport? report = null;
            Assert.That(restore.Restore(r => report = r), Is.True, "se pidió");
            Assert.That(report, Is.Not.Null, "el backend falso responde en el acto");
            return report!;
        }

        // El servidor tiene una compra verificada de otro teléfono (o del que perdió la fusión con la nube).
        private static string ServerHas(FakeBackendClient backend, string token, string sku)
        {
            backend.VerifyPurchase(new VerifyPurchaseRequest(token, sku), r => Assert.That(r.IsOk, Is.True));
            return PurchaseTokenHash.Of(token);
        }

        [Test]
        public void AMissingPurchase_IsCreditedOnce_EvenIfRestoredAgain()
        {
            // Q-B8-1: el perfil ganador de la nube no trae una compra del teléfono que perdió; restoreLedger la vuelve a dar.
            var fx = new VerifiedStoreFixture();
            string hash = ServerHas(fx.Backend, "other.phone.0001", "af_coins_2");
            LedgerRestore restore = Restorer(fx, out _);

            LedgerRestoreReport first = Run(restore);
            Assert.That(first.Ran, Is.True);
            Assert.That(first.Recredited, Is.EqualTo(1));
            Assert.That(first.RecreditedConsumables, Is.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Profile.AppliedPurchases, Is.EqualTo(new[] { hash }));
            Assert.That(fx.Saved().AppliedPurchases, Is.EqualTo(new[] { hash }), "el hash y lo acreditado, guardados");

            LedgerRestoreReport second = Run(restore);
            Assert.That(second.Recredited, Is.EqualTo(0), "nunca dos veces");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
        }

        [Test]
        public void APurchaseAlreadyCreditedHere_IsNotCreditedAgain()
        {
            var fx = new VerifiedStoreFixture();
            Assert.That(fx.Buy("af_coins_2").IsPurchased, Is.True);
            LedgerRestoreReport report = Run(Restorer(fx, out _));
            Assert.That(report.Recredited, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
        }

        [TestCase(true, 30)]
        [TestCase(false, 0)]
        public void ARestoredBalsa_FollowsD038_AndLosesItsRescueCuia(bool lives, int canoeMinutes)
        {
            // D-038, añadido de la Balsa: con las canoas apagadas, «semillas y una cuia», sin las 30 min de Canoas infinitas.
            var fx = new VerifiedStoreFixture();
            fx.Wallet.LivesEnabled = lives;
            ServerHas(fx.Backend, "other.phone.0002", StoreCatalog.Rescue);

            LedgerRestoreReport report = Run(Restorer(fx, out _));

            Assert.That(report.Recredited, Is.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(fx.Ledger.Catalog.Get(StoreCatalog.Rescue).Content.Seeds));
            CanoesStatus canoes = fx.Wallet.Canoes;
            Assert.That(canoes.IsInfinite, Is.EqualTo(lives));
            Assert.That((int)Math.Round(canoes.InfiniteRemaining.TotalMinutes), Is.EqualTo(canoeMinutes));
            Assert.That(fx.Profile.Daily.RescuesToday, Is.EqualTo(0), "la Cuia de rescate se aplica en el nivel: tarde se pierde (F3-3)");
        }

        [Test]
        public void ARestoredNoAdsOrSkin_GivesItsEntitlement()
        {
            var fx = new VerifiedStoreFixture();
            ServerHas(fx.Backend, "other.phone.0003", StoreCatalog.NoAds);
            ServerHas(fx.Backend, "other.phone.0004", "af_skin_m02");

            LedgerRestoreReport report = Run(Restorer(fx, out _));

            Assert.That(report.Recredited, Is.EqualTo(2));
            Assert.That(report.RecreditedNoAds, Is.EqualTo(1));
            Assert.That(report.RecreditedConsumables, Is.EqualTo(0), "Sin anuncios y una skin no son consumibles (af_entitlement_restored)");
            Assert.That(fx.Profile.Entitlements.NoAds, Is.True);
            Assert.That(fx.Profile.Entitlements.Skins, Does.Contain("af_skin_m02"));
        }

        [Test]
        public void APurchaseStillInThisDevicesQueue_IsLeftToTheQueue_AndCreditedOnce()
        {
            // Pagada aquí: el servidor ya la verificó, pero la respuesta aún no llegó (en vuelo). La cola la acredita con su contexto.
            var fx = new VerifiedStoreFixture();
            fx.Backend.HoldVerifyResponses = true;
            Assert.That(fx.TryBuy(StoreCatalog.Piggy, piggySeeds: 1500), Is.Null, "esperando al servidor");

            LedgerRestoreReport report = Run(Restorer(fx, out _));
            Assert.That(report.LeftToQueue, Is.EqualTo(1));
            Assert.That(report.Recredited, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "restoreLedger no la acredita");

            fx.Backend.ReleaseVerifyResponses();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1500), "la cola, con las semillas del Cántaro de esta compra");
            Assert.That(Run(Restorer(fx, out _)).Recredited, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1500), "una sola vez");
        }

        [Test]
        public void AVoidedPurchase_IsRevokedOnce_WithoutGoingBelowZero()
        {
            var fx = new VerifiedStoreFixture();
            Assert.That(fx.Buy("af_coins_2").IsPurchased, Is.True);
            Assert.That(fx.Wallet.Apply(Transaction.Buy(SeedSink.Hint)).IsApplied, Is.True, "gastó parte");
            int left = fx.Wallet.Seeds;
            fx.Backend.RunVoidedPurchasesJob(new[] { VerifiedStoreFixture.Token(1) }, 1);

            LedgerRestore restore = Restorer(fx, out _);
            LedgerRestoreReport report = Run(restore);
            Assert.That(report.Revoked, Is.EqualTo(1));
            Assert.That(report.SeedsRevoked, Is.EqualTo(left), "sin bajar de 0");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Profile.VoidedPurchases, Is.EqualTo(new[] { VerifiedStoreFixture.Hash(1) }));

            fx.Wallet.Apply(Transaction.Earn(SeedSource.Event, 50));
            Assert.That(Run(restore).Revoked, Is.EqualTo(0), "una vez por token");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(50));
        }

        [Test]
        public void TheSsvNonces_AreConfirmedOrRevokedAfterTheGrace_OnlyWithAServerAnswer()
        {
            // F3-4: confirmados por el callback S2S de MAX → dejan de ser provisionales; sin confirmar tras sec_ssv_grace_h → se
            // revocan sin bajar de 0 y fraud_flag++. Sin respuesta del servidor no se revoca nada.
            var fx = new VerifiedStoreFixture();
            LedgerRestore restore = Restorer(fx, out LocalRevocation revocation);
            fx.Wallet.Apply(Transaction.Earn(SeedSource.Event, 60));
            Assert.That(revocation.RegisterProvisional(NonceA, "rv_undo_refill", 30, StockDelta.None), Is.True);
            Assert.That(revocation.RegisterProvisional(NonceB, "rv_undo_refill", 30, StockDelta.None), Is.True);
            fx.Backend.ReceiveSsv(new SsvCallback(VerifiedStoreFixture.Uid, "rv_undo_refill", NonceA));
            fx.Clock.Advance(TimeSpan.FromHours(25));

            fx.Backend.FailNext(new BackendError(BackendErrorKind.Network));
            LedgerRestoreReport offline = Run(restore);
            Assert.That(offline.Ran, Is.False);
            Assert.That(revocation.PendingCount, Is.EqualTo(2), "sin red no se revoca nada");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(60));

            LedgerRestoreReport report = Run(restore);
            Assert.That(report.Ran, Is.True);
            Assert.That(report.Confirmed, Is.EqualTo(1));
            Assert.That(report.Expired, Is.EqualTo(1));
            Assert.That(revocation.PendingCount, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(30), "la no confirmada sale");
            Assert.That(fx.Profile.Integrity.FraudFlag, Is.EqualTo(1));
        }

        [Test]
        public void WithTheBackendOff_NothingIsAsked()
        {
            var fx = new VerifiedStoreFixture();
            fx.Switch.BizBackendEnabled = false;
            ServerHas(fx.Backend, "other.phone.0005", "af_coins_2");
            Assert.That(Restorer(fx, out _).Restore(_ => Assert.Fail("no se llama")), Is.False);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void AnUnknownSku_IsSkipped_AndTheUnavailableBackend_NeverGrants()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.GrantFor = _ => new PurchaseGrant(0); // un SKU de una versión más nueva: el catálogo de esta no lo conoce
            fx.Backend.EntitlementsFor = _ => null;
            ServerHas(fx.Backend, "other.phone.0006", "af_future_pack");
            LedgerRestoreReport report = Run(Restorer(fx, out _));
            Assert.That(report.Skipped, Is.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));

            LedgerRestoreReport none = Run(Restorer(fx, out _, new UnavailableBackendClient()));
            Assert.That(none.Ran, Is.False);
            Assert.That(none.Error!.Kind, Is.EqualTo(BackendErrorKind.NotConfigured));
        }

        [Test]
        public void PendingNonces_FollowTheContract()
        {
            var fx = new VerifiedStoreFixture();
            fx.Profile.PendingRewards.Add(new ProvisionalRewardData { Nonce = NonceA });
            fx.Profile.PendingRewards.Add(new ProvisionalRewardData { Nonce = "no-es-un-uuid" });
            fx.Profile.PendingRewards.Add(new ProvisionalRewardData { Nonce = NonceA });
            Assert.That(LedgerRestore.PendingNonces(fx.Profile), Is.EqualTo(new[] { NonceA }));
        }
    }
}
