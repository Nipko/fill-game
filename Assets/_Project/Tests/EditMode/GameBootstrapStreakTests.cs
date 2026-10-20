#nullable enable
using System;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Arranque de la Crecida (B1a, A7; E32): <see cref="GameBootstrap.CreateStreakService"/> con el almacén de los tests, con el
    /// del perfil (<c>ProfileStreakStore</c>, S1) en desarrollo y en release, y la victoria con la Crecida en una sola escritura del
    /// perfil. Versión del parche <c>S3-bootstrap.diff</c> (sin él, los dos últimos tests comprueban el respaldo en memoria).
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
        public void WithTheProfileStore_DevelopmentAndRelease_UseTheProfile()
        {
            var fx = new Fx();

            StreakService? dev = fx.Create(true, out bool devInMemory);
            StreakService? release = fx.Create(false, out bool releaseInMemory);

            Assert.That(dev, Is.Not.Null);
            Assert.That(release, Is.Not.Null, "con S1 (ProfileStreakStore), también en release");
            Assert.That(devInMemory || releaseInMemory, Is.False, "S3: el almacén es el del perfil, no el de memoria");

            release!.ApplyWin(25, true, 24, RewardTier.Normal);
            Assert.That(fx.Profile.Streak, Is.Not.Null, "la Crecida, en el bloque streak del mismo perfil");
            Assert.That(fx.Profile.Streak!.Wins, Is.EqualTo(1));
            Assert.That(fx.Disk.Load().Profile!.Streak!.Wins, Is.EqualTo(1), "guardada por el lote (vía síncrona, fuera de un lote abierto)");
        }

        [Test]
        public void WithTheProfileStore_AWinAndItsStreakGoOutInOneWrite()
        {
            var fx = new Fx();
            fx.Profile.Progress.MaxLevel = 24;
            fx.Profile.Progress.NextLevel = 25;
            var progress = new ProfileProgressStore(fx.Profile, fx.Batch);
            var flow = new LevelFlow(
                LevelCatalog.Create(new[] { TestPacks.Tutorial }, 20), progress, new RecordingListener(), GameConfig.TapSwitchSelection,
                PourModes.Default, fx.Wallet)
            {
                SaveBatch = fx.Batch,
            };
            flow.UseStreak(fx.Create(false, out _));

            flow.Load(25);
            Assert.That(flow.TapAnywhere(), Is.True);
            int writes = fx.Batch.WriteCount;
            LevelStateTests.Win(flow);

            Assert.That(flow.LastStreakResult!.Streak.Counted, Is.True);
            Assert.That(fx.Batch.WriteCount, Is.EqualTo(writes + 1), "E32: la victoria, la Cosecha y la Crecida, en una sola escritura");
            Profile saved = fx.Disk.Load().Profile!;
            Assert.That(saved.Progress.MaxLevel, Is.EqualTo(25));
            Assert.That(saved.Streak!.Wins, Is.EqualTo(1));
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(flow.HarvestBase));
        }
    }
}
