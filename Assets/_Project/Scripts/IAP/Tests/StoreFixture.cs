#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>Perfil nuevo (0 semillas, 5/5 canoas), almacén en memoria, reloj manual (Bogotá, UTC−5) y tienda simulada.</summary>
    internal sealed class StoreFixture
    {
        /// <summary>2026-10-08 15:00 UTC = 10:00 en Bogotá.</summary>
        public static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        public StoreFixture(StoreConfig? config = null, string currency = "USD", int seeds = 0)
        {
            Clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            Profile = Wallet.NewProfile(Clock);
            Profile.Wallet.Seeds = seeds;
            Store = new MemoryProfileStore();
            Wallet = new Wallet(Profile, Store, Clock);
            Ledger = new StoreLedger(Profile, Wallet, Store, Clock, config);
            Presenter = new ImmediatePurchasePresenter();
            Service = new FakeStoreService(Ledger, Presenter, currency);
        }

        public ManualClock Clock { get; }

        public Profile Profile { get; }

        public MemoryProfileStore Store { get; }

        public Wallet Wallet { get; }

        public StoreLedger Ledger { get; }

        public ImmediatePurchasePresenter Presenter { get; }

        public FakeStoreService Service { get; }

        /// <summary>El perfil en memoria serializado.</summary>
        public string Json => ProfileJson.Serialize(Profile);

        /// <summary>Compra <paramref name="sku"/> y devuelve el resultado (la hoja responde al instante).</summary>
        public PurchaseResult Buy(string sku, int level = 0, int piggySeeds = 0, PurchaseContext context = PurchaseContext.Shop)
        {
            PurchaseResult? result = null;
            Service.Purchase(new PurchaseRequest(sku, context, level, piggySeeds), r => result = r);
            Assert.That(result, Is.Not.Null, "onDone siempre se llama");
            return result!;
        }

        /// <summary>El perfil tal como quedó guardado.</summary>
        public Profile Saved()
        {
            ProfileLoadResult load = Store.Load();
            Assert.That(load.HasProfile, Is.True, "el perfil se guardó");
            return load.Profile!;
        }

        /// <summary>Stock de <paramref name="kind"/>.</summary>
        public int Stock(BoosterKind kind)
        {
            return Wallet.Stock.Get(kind);
        }
    }
}
