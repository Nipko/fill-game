#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// F3-3 en Play Mode: la compra verificada con la tienda y el backend falsos, con las respuestas del servidor llegando unos
    /// fotogramas después (como la red real) y la cola bombeada en cada fotograma, como hará el juego. Una prueba por rama (ok,
    /// duplicate, pending, 400, 409, fallo pasajero y 48 h), la carrera de dos compras y la cola al reabrir. Lo que se vigila: nada se
    /// acredita sin verificar ni dos veces, y nada se consume sin acreditar.
    /// </summary>
    public sealed class VerifiedPurchasePlayModeTests
    {
        private const int NetworkFrames = 3;

        [UnityTest]
        public IEnumerator Ok_ShowsVerifyingForSomeFrames_ThenCreditsOnceAndConsumes()
        {
            var fx = new Fx();
            PurchaseResult? result = fx.Start("af_coins_2");
            Assert.That(result, Is.Null, "«Confirmando…» mientras responde el servidor");
            Assert.That(fx.Events, Is.EqualTo(new[] { PurchaseVerificationState.Verifying }));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "nada antes de verificar");

            yield return fx.Network();

            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1));
            Assert.That(fx.Saved().AppliedPurchases, Has.Count.EqualTo(1));
            yield return fx.Frames(5);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000), "bombear la cola no vuelve a acreditar");
        }

        [UnityTest]
        public IEnumerator Duplicate_CreditsOnlyWhenNotApplied()
        {
            var fx = new Fx();
            fx.Backend.VerifyPurchase(new VerifyPurchaseRequest("restored.token", "af_noads"), _ => { });
            fx.Backend.ReleaseVerifyResponses();
            fx.Billing.AddUnfinished("af_noads", "restored.token");
            fx.Service.Resume();
            yield return fx.Network();
            Assert.That(fx.Profile.Entitlements.NoAds, Is.True, "otra instalación: duplicate no aplicado se acredita");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));

            fx.Billing.RestoreOwnedOnNewDevice(); // vuelve a entregarla (Restaurar compras)
            fx.Service.Resume();
            yield return fx.Network();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500), "ya aplicado: no se acredita otra vez");
            Assert.That(fx.Backend.VerifyRequests, Has.Count.EqualTo(2), "aplicado: se termina sin preguntar");
        }

        [UnityTest]
        public IEnumerator Pending_CreditsNothingUntilPaid()
        {
            var fx = new Fx();
            fx.Backend.MarkPending(Fx.Token(1));
            fx.Start("af_coins_3");
            yield return fx.Network();
            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Billing.IsUnfinished(Fx.Token(1)), Is.True);

            fx.Backend.ClearPending(Fx.Token(1));
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            yield return fx.Network();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2800));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Rejected400And409_NeitherCreditNorConsume()
        {
            var fx = new Fx();
            fx.Backend.FailNext(BackendError.Http(400, ApiErrorCode.BadRequest));
            fx.Start("af_starter");
            yield return fx.Network();
            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.VerifyFailed));

            fx.Backend.FailNext(BackendError.Http(409, ApiErrorCode.TokenBelongsToOtherUser));
            fx.Start("af_coins_2");
            yield return fx.Network();
            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.VerifyFailed));

            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Profile.Entitlements.StarterBought, Is.False);
            Assert.That(fx.Billing.Consumed, Is.Empty);
            Assert.That(fx.Billing.Confirmed, Is.Empty);
            Assert.That(fx.Billing.LocallyAcknowledged, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Transient_ThenDeferredAckAfter48h_ThenCreditedOnceWhenVerified()
        {
            var fx = new Fx();
            fx.Backend.FailNext(BackendError.Network("offline"), 10000);
            fx.Start("af_coins_2");
            yield return fx.Network();
            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.Pending));

            for (int hour = 0; hour < 51; hour++)
            {
                fx.Clock.Advance(TimeSpan.FromHours(1));
                yield return fx.Network();
            }

            Assert.That(fx.Billing.LocallyAcknowledged, Has.Count.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "el deferred_ack no acredita");

            fx.Backend.ClearFailures();
            fx.Clock.Advance(TimeSpan.FromHours(6));
            yield return fx.Network();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Race_TwoPurchasesInFlight_AnsweredInReverse_EachCreditedOnce()
        {
            var fx = new Fx();
            fx.Backend.FailNext(BackendError.Network("offline"));
            fx.Start("af_coins_2");
            yield return fx.Network();
            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.Pending));

            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Start("af_coins_3");
            Assert.That(fx.Backend.HeldVerifyResponses, Is.EqualTo(2), "las dos en vuelo a la vez");
            Assert.That(fx.Start("af_coins_4")!.Status, Is.EqualTo(PurchaseStatus.Busy));
            yield return fx.Frames(NetworkFrames);
            fx.Backend.ReleaseVerifyResponses(reverse: true);
            yield return fx.Frames(2);

            Assert.That(fx.Wallet.Seeds, Is.EqualTo(3800));
            Assert.That(fx.Profile.AppliedPurchases, Has.Count.EqualTo(2));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TheQueueSurvivesReopening()
        {
            var fx = new Fx();
            fx.Backend.FailNext(BackendError.Network("offline"));
            fx.Start("af_piggy", piggySeeds: 1900);
            yield return fx.Network();
            Assert.That(fx.Last!.Status, Is.EqualTo(PurchaseStatus.Pending));

            Fx again = fx.Reopen();
            again.Clock.Advance(TimeSpan.FromMinutes(1));
            again.Service.Resume();
            yield return again.Network();
            Assert.That(again.Wallet.Seeds, Is.EqualTo(1900));

            Fx third = again.Reopen();
            third.Service.Resume();
            yield return third.Network();
            Assert.That(third.Wallet.Seeds, Is.EqualTo(1900), "nada dos veces al reabrir");
            Assert.That(third.Billing.Consumed, Has.Count.EqualTo(1));
        }

        /// <summary>Tienda de Play y servidor falsos; la red tarda <see cref="NetworkFrames"/> fotogramas.</summary>
        private sealed class Fx
        {
            private static readonly DateTime T0 = new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);

            public Fx(MemoryProfileStore? store = null, ManualClock? clock = null, FakeBillingClient? billing = null, FakeBackendClient? backend = null)
            {
                Clock = clock ?? new ManualClock(T0, TimeSpan.FromHours(-5));
                Store = store ?? new MemoryProfileStore();
                ProfileLoadResult load = Store.Load();
                Profile = load.HasProfile ? load.Profile! : Wallet.NewProfile(Clock);
                Wallet = new Wallet(Profile, Store, Clock);
                var ledger = new StoreLedger(Profile, Wallet, Store, Clock);
                StoreCatalog catalog = ledger.Catalog;
                Billing = billing ?? new FakeBillingClient(catalog, "USD", null, Clock);
                Backend = backend ?? new FakeBackendClient(Clock)
                {
                    GrantFor = r => CatalogGrants.For(catalog, r.Sku),
                    EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku),
                };
                Backend.HoldVerifyResponses = true;
                Service = new VerifiedStoreService(ledger, Billing, Backend, Clock, new BackendSwitch(), new FixedPlayerUid("uidPlayMode01"));
                Service.VerificationChanged += e => Events.Add(e.State);
            }

            public ManualClock Clock { get; }

            public MemoryProfileStore Store { get; }

            public Profile Profile { get; }

            public Wallet Wallet { get; }

            public FakeBillingClient Billing { get; }

            public FakeBackendClient Backend { get; }

            public VerifiedStoreService Service { get; }

            public List<PurchaseVerificationState> Events { get; } = new List<PurchaseVerificationState>();

            public PurchaseResult? Last { get; private set; }

            public static string Token(int n) => "fake.purchase." + n.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);

            public Fx Reopen() => new Fx(Store, Clock, Billing, Backend);

            /// <summary>Empieza la compra; el resultado si ya llegó.</summary>
            public PurchaseResult? Start(string sku, int piggySeeds = 0)
            {
                PurchaseResult? now = null;
                Service.Purchase(new PurchaseRequest(sku, PurchaseContext.Shop, 0, piggySeeds), r =>
                {
                    now = r;
                    Last = r;
                });
                return now;
            }

            /// <summary>La red responde tras unos fotogramas, bombeando la cola en cada uno (como el juego en Update).</summary>
            public IEnumerator Network()
            {
                for (int round = 0; round < 3; round++)
                {
                    Service.Pump();
                    yield return Frames(NetworkFrames);
                    Backend.ReleaseVerifyResponses();
                }
            }

            public IEnumerator Frames(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    yield return null;
                    Service.Pump();
                }
            }

            public Profile Saved()
            {
                ProfileLoadResult load = Store.Load();
                Assert.That(load.HasProfile, Is.True);
                return load.Profile!;
            }
        }
    }
}
