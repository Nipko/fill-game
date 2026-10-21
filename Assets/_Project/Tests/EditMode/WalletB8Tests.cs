#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La cartera con lo del bloque B8 (parche <c>B8-economy.diff</c>): el anti-reloj de las canoas y de los topes diarios (04 §7.1,
    /// «Reloj»; 08 §10.1) con <see cref="GuardedClock"/>; <c>applied_purchases</c> (08 §10.3: un token se acredita una vez); la
    /// revocación sin bajar de 0 (08 §10.1 y §10.2) con <see cref="LocalRevocation"/>; y B1a-Q2 (D-033): un cambio que solo toca
    /// <c>streak</c> marca <c>economy_ts</c>.
    /// </summary>
    public sealed class WalletB8Tests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        private sealed class Fixture
        {
            public Fixture(IServerDate server, int seeds = 0)
            {
                Device = new ManualClock(T0, TimeSpan.FromHours(-5));
                Server = server;
                Clock = new GuardedClock(Device, server);
                Profile = Wallet.NewProfile(Device);
                Profile.Wallet.Seeds = seeds;
                Store = new MemoryProfileStore();
                Wallet = new Wallet(Profile, Store, Clock);
                Revocation = new LocalRevocation(Profile, Wallet, Store, Clock);
            }

            public ManualClock Device { get; }

            public IServerDate Server { get; }

            public GuardedClock Clock { get; }

            public Profile Profile { get; }

            public MemoryProfileStore Store { get; }

            public Wallet Wallet { get; }

            public LocalRevocation Revocation { get; }
        }

        [Test]
        public void Canoes_DoNotRegenerate_WhileTheDeviceClockIsBeyondTheDrift_AndResumeWithASaneClock()
        {
            FakeServerDate server = FakeServerDate.Online(TimeSpan.Zero);
            var fx = new Fixture(server);
            fx.Wallet.Apply(Transaction.HardFail(31));
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
            DateTime next = fx.Wallet.Canoes.NextRegenAt!.Value;

            // El jugador adelanta el teléfono 2 días para recuperar canoas: con red, el desfase pasa de 24 h y no regeneran.
            fx.Device.Advance(TimeSpan.FromDays(2));
            server.GoOnline(TimeSpan.FromDays(-2));
            Assert.That(fx.Wallet.CanoesFrozen, Is.True);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4), "04 §7.1: las canoas no regeneran hasta reconectar");
            fx.Wallet.Apply(Transaction.HardFail(32));
            Assert.That(fx.Profile.Wallet.Canoes.Count, Is.EqualTo(3), "Apply tampoco regenera");
            Assert.That(fx.Profile.Wallet.Canoes.NextRegenAt, Is.EqualTo(next), "el cronómetro no se pierde");

            // Reloj sano otra vez: la regeneración sigue con la hora de verdad.
            fx.Device.Advance(TimeSpan.FromDays(-2) + TimeSpan.FromMinutes(26));
            server.GoOnline(TimeSpan.Zero);
            Assert.That(fx.Wallet.CanoesFrozen, Is.False);
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(4));
        }

        [Test]
        public void Canoes_Offline_AcceptTheLocalTime()
        {
            var fx = new Fixture(FakeServerDate.Offline());
            fx.Wallet.Apply(Transaction.HardFail(31));
            fx.Device.Advance(TimeSpan.FromDays(2));
            Assert.That(fx.Wallet.CanoesFrozen, Is.False, "sin red se acepta la fecha local (08 §10.1)");
            Assert.That(fx.Wallet.Canoes.Count, Is.EqualTo(5));
        }

        [Test]
        public void DailyCaps_UseTheServerDate_WhenTheDeviceRunsAhead()
        {
            FakeServerDate server = FakeServerDate.Online(TimeSpan.Zero);
            var fx = new Fixture(server, seeds: 1000);
            Assert.That(fx.Wallet.Apply(Transaction.Rescue(31, 300)).IsApplied, Is.True, "el primero del día, a mitad de precio");
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(1));

            // Mañana en el teléfono, hoy en el servidor: el descuento del primer rescate no vuelve.
            fx.Device.Advance(TimeSpan.FromDays(1));
            server.GoOnline(TimeSpan.FromDays(-1) - TimeSpan.FromMinutes(1));
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(1));
            Assert.That(fx.Wallet.QuoteRescue(31)!.Value.IsDiscounted, Is.False);
        }

        [Test]
        public void Purchase_WithAToken_IsAppliedOnce_AndTheTokenGoesInTheSameWrite()
        {
            var fx = new Fixture(FakeServerDate.Offline());
            TransactionResult first = fx.Wallet.Apply(Transaction.Purchase("af_coins_2", seeds: 1200, purchaseToken: "GPA.1"));
            Assert.That(first.IsApplied && first.Saved, Is.True);
            Assert.That(fx.Profile.AppliedPurchases, Is.EqualTo(new[] { "GPA.1" }));
            Assert.That(ProfileJson.Deserialize(fx.Store.Json!).AppliedPurchases, Is.EqualTo(new[] { "GPA.1" }), "en la misma escritura");

            TransactionResult again = fx.Wallet.Apply(Transaction.Purchase("af_coins_2", seeds: 1200, purchaseToken: "GPA.1"));
            Assert.That(again.Reason, Is.EqualTo(RejectReason.AlreadyApplied));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1200));

            fx.Wallet.Apply(Transaction.Purchase("af_coins_2", seeds: 1200)); // sin token (compra simulada): como siempre
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2400));
            Assert.That(fx.Profile.AppliedPurchases.Count, Is.EqualTo(1));
        }

        [Test]
        public void Revoke_NeverGoesBelowZero()
        {
            var fx = new Fixture(FakeServerDate.Offline(), seeds: 50);
            fx.Profile.Stock.Undo = 1;
            TransactionResult r = fx.Wallet.Apply(Transaction.Revoke(200, StockDelta.Of(BoosterKind.Undo, 3).Plus(BoosterKind.Hint, 1)));
            Assert.That(r.IsApplied, Is.True);
            Assert.That(r.SeedsDelta, Is.EqualTo(-50));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Profile.Stock.Undo, Is.EqualTo(0));
            Assert.That(fx.Profile.Stock.Hint, Is.EqualTo(0));
            Assert.That(fx.Profile.EconomyTs, Is.EqualTo(T0));
        }

        [Test]
        public void ExpireStale_RevokesUnconfirmedRewards_After24h_AndCountsTheFraudFlag()
        {
            var fx = new Fixture(FakeServerDate.Offline(), seeds: 10);
            fx.Wallet.Apply(Transaction.GrantStock(BoosterKind.Undo, 3));
            Assert.That(fx.Revocation.RegisterProvisional("n-1", "rv_undo_refill", 0, StockDelta.Of(BoosterKind.Undo, 3)), Is.True);
            Assert.That(fx.Revocation.RegisterProvisional("n-1", "rv_undo_refill", 0, StockDelta.Of(BoosterKind.Undo, 3)), Is.False, "nonce único");
            fx.Wallet.Apply(Transaction.GrantStock(BoosterKind.Hint, 1));
            fx.Revocation.RegisterProvisional("n-2", "rv_hint", 0, StockDelta.Of(BoosterKind.Hint, 1));
            Assert.That(ProfileJson.Deserialize(fx.Store.Json!).PendingRewards.Count, Is.EqualTo(2));

            Assert.That(fx.Revocation.Confirm("n-2"), Is.True, "granted=confirmed");
            fx.Device.Advance(TimeSpan.FromHours(23));
            Assert.That(fx.Revocation.ExpireStale(), Is.EqualTo(0));

            fx.Profile.Stock.Undo = 1; // gastó dos
            fx.Device.Advance(TimeSpan.FromHours(1));
            Assert.That(fx.Revocation.ExpireStale(), Is.EqualTo(1), "sec_ssv_grace_h = 24 (08 §9)");
            Assert.That(fx.Profile.Stock.Undo, Is.EqualTo(0), "no baja de 0");
            Assert.That(fx.Profile.Stock.Hint, Is.EqualTo(1), "la confirmada se queda");
            Assert.That(fx.Profile.Integrity.FraudFlag, Is.EqualTo(1));
            Assert.That(fx.Revocation.PendingCount, Is.EqualTo(0));
            Profile saved = ProfileJson.Deserialize(fx.Store.Json!);
            Assert.That(saved.PendingRewards, Is.Empty);
            Assert.That(saved.Integrity.FraudFlag, Is.EqualTo(1));
        }

        [Test]
        public void ExpireStale_WithNothingLeftToTake_StillSavesTheFlag()
        {
            var fx = new Fixture(FakeServerDate.Offline());
            fx.Revocation.RegisterProvisional("n-1", "rv_reveal", 0, StockDelta.Of(BoosterKind.Reveal, 1));
            fx.Device.Advance(TimeSpan.FromHours(25));
            Assert.That(fx.Revocation.ExpireStale(), Is.EqualTo(1));
            Assert.That(ProfileJson.Deserialize(fx.Store.Json!).Integrity.FraudFlag, Is.EqualTo(1));
        }

        [Test]
        public void ApplyLedger_RecreditsMissingTokens_AndRevokesVoidedOnesOnce()
        {
            var fx = new Fixture(FakeServerDate.Offline());
            fx.Wallet.Apply(Transaction.Purchase("af_coins_2", seeds: 1200, purchaseToken: "GPA.a"));
            fx.Wallet.Apply(Transaction.Purchase("af_noads", grantsNoAds: true, purchaseToken: "GPA.c"));
            fx.Profile.Wallet.Seeds = 700; // gastó parte

            var ledger = new[]
            {
                new LedgerEntry("GPA.b", "af_coins_1", seeds: 500), // falta aquí: se vuelve a acreditar
                new LedgerEntry("GPA.a", "af_coins_2", seeds: 1200, voided: true), // reembolsada: se retira sin bajar de 0
                new LedgerEntry("GPA.c", "af_noads", grantsNoAds: true, voided: true),
                new LedgerEntry("GPA.z", "af_coins_1", seeds: 500, voided: true), // nunca se acreditó aquí
            };
            LedgerReport report = fx.Revocation.ApplyLedger(ledger);
            Assert.That(report.Recredited, Is.EqualTo(1));
            Assert.That(report.Revoked, Is.EqualTo(2));
            // En orden ordinal de token [P]: GPA.a se retira primero (700, sin bajar de 0) y luego vuelve GPA.b (+500).
            Assert.That(report.SeedsRevoked, Is.EqualTo(700));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fx.Profile.Entitlements.NoAds, Is.False, "[P]: se retira el derecho");
            Assert.That(fx.Profile.AppliedPurchases, Is.EqualTo(new[] { "GPA.a", "GPA.b", "GPA.c" }));
            Assert.That(fx.Profile.VoidedPurchases, Is.EqualTo(new[] { "GPA.a", "GPA.c" }));

            LedgerReport again = fx.Revocation.ApplyLedger(ledger);
            Assert.That(again.Recredited + again.Revoked, Is.EqualTo(0), "idempotente");
        }

        [Test]
        public void AfterTheMerge_TheLedgerRecreditsTheLosersPurchases_AndRevokesWhatWasVoidedOnlyThere()
        {
            // D-033, Q-B8-1: applied_purchases y voided_purchases [P] son los del ganador; restoreLedger hace el resto. GPA.a está
            // acreditada en los dos y se reembolsó en el perdedor (allí ya se retiró); GPA.b solo se compró en el perdedor.
            var device = new ManualClock(T0, TimeSpan.FromHours(-5));
            Profile local = Wallet.NewProfile(device);
            local.Progress.MaxLevel = 70;
            local.Wallet.Seeds = 1200;
            local.AppliedPurchases.Add("GPA.a");
            Profile cloud = Wallet.NewProfile(device);
            cloud.Progress.MaxLevel = 40;
            cloud.AppliedPurchases.Add("GPA.a");
            cloud.AppliedPurchases.Add("GPA.b");
            cloud.VoidedPurchases.Add("GPA.a");

            Profile merged = CloudMerge.Resolve(local, cloud).Merged;
            Assert.That(merged.AppliedPurchases, Is.EqualTo(new[] { "GPA.a" }));
            Assert.That(merged.VoidedPurchases, Is.Empty);

            var store = new MemoryProfileStore();
            var clock = new GuardedClock(device, FakeServerDate.Offline());
            var wallet = new Wallet(merged, store, clock);
            var revocation = new LocalRevocation(merged, wallet, store, clock);
            LedgerReport report = revocation.ApplyLedger(new[]
            {
                new LedgerEntry("GPA.a", "af_coins_2", seeds: 1200, voided: true),
                new LedgerEntry("GPA.b", "af_coins_1", seeds: 500),
            });

            Assert.That(report.Revoked, Is.EqualTo(1), "con la unión de voided_purchases GPA.a no se retiraría");
            Assert.That(report.Recredited, Is.EqualTo(1), "la compra del perdedor vuelve con el libro mayor");
            Assert.That(wallet.Seeds, Is.EqualTo(500));
            Assert.That(merged.AppliedPurchases, Is.EqualTo(new[] { "GPA.a", "GPA.b" }));
            Assert.That(merged.VoidedPurchases, Is.EqualTo(new[] { "GPA.a" }));
        }

        [Test]
        public void Revoke_WithAnAlreadyVoidedToken_IsRejected()
        {
            var fx = new Fixture(FakeServerDate.Offline(), seeds: 500);
            Assert.That(fx.Wallet.Apply(Transaction.Revoke(100, purchaseToken: "GPA.v")).IsApplied, Is.True);
            TransactionResult again = fx.Wallet.Apply(Transaction.Revoke(100, purchaseToken: "GPA.v"));
            Assert.That(again.Reason, Is.EqualTo(RejectReason.AlreadyApplied), "un token no se retira dos veces");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(400));
        }

        [Test]
        public void TheDailyChallengeWallet_WithTheGuardedClock_AlsoFreezesCanoesAndUsesTheServerDate()
        {
            // Revisión de B8: la cartera del Reto (LevelFlow.CreateDailyWallet) va con el mismo reloj que la de campaña.
            FakeServerDate server = FakeServerDate.Online(TimeSpan.Zero);
            var fx = new Fixture(server, seeds: 100);
            fx.Wallet.Apply(Transaction.HardFail(31));
            fx.Profile.Daily.DateLocal = "2026-10-20";
            fx.Profile.Daily.RvTotal = 3;
            Wallet daily = AF.GameFlow.LevelFlow.CreateDailyWallet(fx.Profile, fx.Store, fx.Clock);
            fx.Device.Advance(TimeSpan.FromDays(2));
            server.GoOnline(TimeSpan.FromDays(-2));
            daily.Apply(Transaction.Earn(SeedSource.DailyChallenge, 60));
            Assert.That(fx.Profile.Wallet.Canoes.Count, Is.EqualTo(4), "no regeneran");
            Assert.That(fx.Profile.Daily.DateLocal, Is.EqualTo("2026-10-20"), "los topes, con la fecha del servidor");
            Assert.That(fx.Profile.Daily.RvTotal, Is.EqualTo(3));
        }

        [Test]
        public void StreakOnlyChange_MarksEconomyTs()
        {
            var clock = new ManualClock(T0);
            Profile profile = Wallet.NewProfile(clock);
            var store = new ProfileStreakStore(profile, new MemoryProfileStore(), clock);
            StreakRecord record = store.Load();
            DateTime? before = profile.EconomyTs;
            clock.Advance(TimeSpan.FromMinutes(5));
            record.Wins = 3;
            store.Save(record);
            Assert.That(profile.EconomyTs, Is.EqualTo(T0.AddMinutes(5)), "B1a-Q2 (a): streak cuenta para economy_ts");
            Assert.That(before, Is.Not.EqualTo(profile.EconomyTs));
        }
    }
}
