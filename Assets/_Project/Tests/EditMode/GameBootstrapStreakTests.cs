#nullable enable
using System;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Arranque de la Crecida (B1a, A7; E32): <see cref="GameBootstrap.CreateStreakService"/> con el almacén de los tests, con el
    /// de memoria en desarrollo y sin nada en release mientras no esté el parche S1/S3 (<c>ProfileStreakStore</c>). El parche
    /// <c>S3-bootstrap.diff</c> cambia los dos últimos tests: con S1, el almacén es el del perfil en los dos casos.
    /// </summary>
    public sealed class GameBootstrapStreakTests
    {
        private sealed class Fx
        {
            public Fx()
            {
                Clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
                Profile = Wallet.NewProfile(Clock);
                Disk = new MemoryProfileStore();
                Batch = new ProfileSaveBatch(Disk);
                Wallet = new Wallet(Profile, Batch.Durable, Clock);
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public MemoryProfileStore Disk { get; }

            public ProfileSaveBatch Batch { get; }

            public Wallet Wallet { get; }

            public StreakService? Create(bool debugBuild, out bool inMemory, int basketUnlock = 10)
            {
                return GameBootstrap.CreateStreakService(Profile, Batch, Wallet, Clock, basketUnlock, debugBuild, out inMemory);
            }
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetStreakOverrides();
        }

        [Test]
        public void WithTheTestStore_UsesIt_WithTheDefaultsAndTheDailyUnlock()
        {
            var fx = new Fx();
            var store = new InMemoryStreakStore(fx.Clock);
            GameBootstrap.StreakStoreOverride = store;

            StreakService? service = fx.Create(false, out bool inMemory, 11);

            Assert.That(service, Is.Not.Null);
            Assert.That(inMemory, Is.False, "el de los tests no es el de respaldo");
            Assert.That(service!.BasketUnlockLevel, Is.EqualTo(11), "live_unlock_level_daily, de DailyChallengeConfig");
            Assert.That(service.Config.StreakUnlockLevel, Is.EqualTo(StreakConfig.Defaults.StreakUnlockLevel));
            Assert.That(service.Basket.EcoBasketSeeds, Is.EqualTo(BasketConfig.Defaults.EcoBasketSeeds));

            service.ApplyWin(25, true, 24, RewardTier.Normal);
            Assert.That(store.Persisted.Wins, Is.EqualTo(1), "guarda en el almacén dado");
        }

        [Test]
        public void TheConfigOverrides_Apply()
        {
            var fx = new Fx();
            GameBootstrap.StreakStoreOverride = new InMemoryStreakStore(fx.Clock);
            GameBootstrap.StreakConfigOverride = new StreakConfig { StreakUnlockLevel = 30 };
            GameBootstrap.BasketConfigOverride = new BasketConfig { EcoBasketSeeds = 40 };

            StreakService service = fx.Create(true, out _)!;

            Assert.That(service.Config.StreakUnlockLevel, Is.EqualTo(30));
            Assert.That(service.Basket.EcoBasketSeeds, Is.EqualTo(40));
        }

        [Test]
        public void WithoutTheProfileStore_DevelopmentFallsBackToMemory()
        {
            var fx = new Fx();

            StreakService? service = fx.Create(true, out bool inMemory);

            Assert.That(service, Is.Not.Null);
            Assert.That(inMemory, Is.True, "sin S1: el estado, en memoria (se pierde al cerrar la app)");
        }

        [Test]
        public void WithoutTheProfileStore_ReleaseHasNoStreak()
        {
            var fx = new Fx();

            Assert.That(fx.Create(false, out bool inMemory), Is.Null, "sin S1, en release no hay Crecida");
            Assert.That(inMemory, Is.False);
        }
    }
}
