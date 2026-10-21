#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Verification;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Tienda verificada (F3-3) con la tienda de Play falsa (<see cref="FakeBillingClient"/>) y el backend falso
    /// (<see cref="FakeBackendClient"/>, que responde como el servidor con <see cref="CatalogGrants"/>), sobre un perfil en un
    /// <see cref="MemoryProfileStore"/>. <see cref="Reopen"/> simula cerrar y volver a abrir la app: un perfil leído del JSON guardado y
    /// una tienda nueva, con la misma tienda de Play y el mismo servidor.
    /// </summary>
    internal sealed class VerifiedStoreFixture
    {
        public const string Uid = "uidTest0123456789";

        public VerifiedStoreFixture(
            string? uid = Uid,
            MemoryProfileStore? store = null,
            ManualClock? clock = null,
            FakeBillingClient? billing = null,
            FakeBackendClient? backend = null,
            BackendSwitch? backendSwitch = null)
        {
            Clock = clock ?? new ManualClock(StoreFixture.T0, TimeSpan.FromHours(-5));
            Store = store ?? new MemoryProfileStore();
            ProfileLoadResult load = Store.Load();
            Profile = load.HasProfile ? load.Profile! : Wallet.NewProfile(Clock);
            Disk = new FlakyProfileStore(Store);
            Wallet = new Wallet(Profile, Disk, Clock);
            Ledger = new StoreLedger(Profile, Wallet, Disk, Clock);
            Billing = billing ?? new FakeBillingClient(Ledger.Catalog, "USD", null, Clock);
            StoreCatalog catalog = Ledger.Catalog;
            Backend = backend ?? new FakeBackendClient(Clock)
            {
                GrantFor = r => CatalogGrants.For(catalog, r.Sku),
                EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku),
            };
            Switch = backendSwitch ?? new BackendSwitch();
            UidValue = uid;
            Service = new VerifiedStoreService(Ledger, Billing, Backend, Clock, Switch, new FixedPlayerUid(uid));
            Service.VerificationChanged += e => Events.Add(e.State);
        }

        public ManualClock Clock { get; }

        public MemoryProfileStore Store { get; }

        /// <summary>El almacén que usan la cartera y la tienda: <see cref="FlakyProfileStore.Fail"/> hace fallar las escrituras.</summary>
        public FlakyProfileStore Disk { get; }

        public Profile Profile { get; }

        public Wallet Wallet { get; }

        public StoreLedger Ledger { get; }

        public FakeBillingClient Billing { get; }

        public FakeBackendClient Backend { get; }

        public BackendSwitch Switch { get; }

        public string? UidValue { get; }

        public VerifiedStoreService Service { get; }

        public List<PurchaseVerificationState> Events { get; } = new List<PurchaseVerificationState>();

        /// <summary>Peticiones de <c>verifyPurchase</c> que recibió el servidor.</summary>
        public int VerifyCalls => Backend.VerifyRequests.Count;

        /// <summary>La app se cierra y se vuelve a abrir (sin llamar a <see cref="VerifiedStoreService.Resume"/>).</summary>
        public VerifiedStoreFixture Reopen()
        {
            return new VerifiedStoreFixture(UidValue, Store, Clock, Billing, Backend, Switch);
        }

        /// <summary>Compra <paramref name="sku"/>; el resultado, o null si aún no llegó (respuesta retenida).</summary>
        public PurchaseResult? TryBuy(string sku, int level = 0, int piggySeeds = 0, PurchaseContext context = PurchaseContext.Shop)
        {
            PurchaseResult? result = null;
            Service.Purchase(new PurchaseRequest(sku, context, level, piggySeeds), r =>
            {
                Assert.That(result, Is.Null, "onDone una sola vez");
                result = r;
            });
            return result;
        }

        /// <summary>Compra <paramref name="sku"/> y exige el resultado ya.</summary>
        public PurchaseResult Buy(string sku, int level = 0, int piggySeeds = 0, PurchaseContext context = PurchaseContext.Shop)
        {
            PurchaseResult? result = TryBuy(sku, level, piggySeeds, context);
            Assert.That(result, Is.Not.Null, "onDone se llama al terminar");
            return result!;
        }

        /// <summary>El perfil tal como quedó guardado.</summary>
        public Profile Saved()
        {
            ProfileLoadResult load = Store.Load();
            Assert.That(load.HasProfile, Is.True, "el perfil se guardó");
            return load.Profile!;
        }

        /// <summary>Token de la compra número <paramref name="n"/> de la tienda falsa.</summary>
        public static string Token(int n) => "fake.purchase." + n.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);

        public static string Hash(int n) => PurchaseTokenHash.Of(Token(n));

        /// <summary>La entrada de la cola de <paramref name="token"/>, o null.</summary>
        public PendingVerification? Queued(string token)
        {
            IReadOnlyList<PendingVerification> entries = Service.PendingVerifications;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Request.Token == token)
                {
                    return entries[i];
                }
            }

            return null;
        }

        public int Stock(BoosterKind kind) => Wallet.Stock.Get(kind);
    }

    /// <summary>Almacén cuyas escrituras fallan mientras <see cref="Fail"/> (disco lleno, por ejemplo).</summary>
    internal sealed class FlakyProfileStore : IProfileStore
    {
        private readonly MemoryProfileStore _inner;

        public FlakyProfileStore(MemoryProfileStore inner)
        {
            _inner = inner;
        }

        public bool Fail { get; set; }

        public ProfileLoadResult Load() => _inner.Load();

        public void Save(Profile profile)
        {
            if (Fail)
            {
                throw new System.IO.IOException("disco lleno (test)");
            }

            _inner.Save(profile);
        }

        public void Delete() => _inner.Delete();
    }
}
