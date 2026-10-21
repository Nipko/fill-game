#nullable enable
using System;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Arranque de B6 (parche B6-bootstrap, con B6-profile): <see cref="GameBootstrap.CreateSessionStores"/> con los almacenes de los
    /// tests, con los del perfil en desarrollo y en release, y una sesión con su Regalo de regreso en el mismo perfil que la cartera.
    /// </summary>
    public sealed class GameBootstrapSessionsTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetSessionOverrides();
        }

        [Test]
        public void WithTheTestStores_UsesThem()
        {
            var profile = Wallet.NewProfile(new ManualClock(T0));
            var batch = new ProfileSaveBatch(new MemoryProfileStore());
            var sessions = new InMemorySessionStore();
            var push = new InMemoryPushSettingsStore();
            GameBootstrap.SessionStoreOverride = sessions;
            GameBootstrap.PushSettingsStoreOverride = push;

            Assert.That(GameBootstrap.CreateSessionStores(profile, batch, false, out ISessionStore? s, out IPushSettingsStore? p, out bool inMemory), Is.True);
            Assert.That(s, Is.SameAs(sessions));
            Assert.That(p, Is.SameAs(push));
            Assert.That(inMemory, Is.False);
        }

        [Test]
        public void WithTheProfileStores_DevelopmentAndRelease_UseTheProfile()
        {
            var clock = new ManualClock(T0);
            Profile profile = Wallet.NewProfile(clock);
            var disk = new MemoryProfileStore();
            var batch = new ProfileSaveBatch(disk);

            Assert.That(GameBootstrap.CreateSessionStores(profile, batch, true, out ISessionStore? dev, out _, out bool devInMemory), Is.True);
            Assert.That(GameBootstrap.CreateSessionStores(profile, batch, false, out ISessionStore? release, out IPushSettingsStore? push, out bool releaseInMemory), Is.True);
            Assert.That(devInMemory || releaseInMemory, Is.False, "B6-bootstrap: los almacenes son los del perfil");
            Assert.That(dev, Is.InstanceOf<ProfileSessionStore>());
            Assert.That(push, Is.InstanceOf<ProfilePushSettingsStore>());

            new SessionTracker(release!, clock).OnColdStart();
            Assert.That(profile.Stats.Sessions, Is.EqualTo(1));
            Assert.That(disk.Load().Profile!.Stats.Sessions, Is.EqualTo(1), "guardado por la vía síncrona del lote");
            Assert.That(disk.Load().Profile!.Stats.LastSeenAt, Is.EqualTo(T0));
        }

        [Test]
        public void ASessionAfterAWeek_PaysTheGiftInTheSameProfile()
        {
            var clock = new ManualClock(T0);
            Profile profile = Wallet.NewProfile(clock);
            profile.Stats.LastSeenAt = T0.AddDays(-7);
            var disk = new MemoryProfileStore();
            var batch = new ProfileSaveBatch(disk);
            var wallet = new Wallet(profile, batch.Durable, clock);
            GameBootstrap.CreateSessionStores(profile, batch, false, out ISessionStore? sessions, out _, out _);
            SessionStart start = new SessionTracker(sessions!, clock).OnColdStart();
            ComebackOutcome outcome = new ComebackService(wallet).OnSessionStart(in start, 0);

            Assert.That(outcome.Granted, Is.True);
            Profile saved = disk.Load().Profile!;
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(ComebackGift.Default.Seeds));
            Assert.That(saved.Stock.ExtraCuia, Is.EqualTo(1));
            Assert.That(saved.Stats.LastSeenAt, Is.EqualTo(T0));
        }
    }
}
