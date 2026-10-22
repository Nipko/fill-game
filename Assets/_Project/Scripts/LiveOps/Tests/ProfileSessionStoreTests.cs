#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Parche B6-profile: <c>settings.push</c> y <c>stats.last_seen_at</c> / <c>last_win_at</c> [P] en el perfil (08 §4.3), su ida
    /// y vuelta por <see cref="ProfileJson"/> y los almacenes <see cref="ProfileSessionStore"/> y <see cref="ProfilePushSettingsStore"/>.
    /// </summary>
    public sealed class ProfileSessionStoreTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ANewProfile_WritesNoneOfTheNewKeys()
        {
            Profile p = Wallet.NewProfile(new ManualClock(T0));
            string json = ProfileJson.Serialize(p);
            StringAssert.DoesNotContain("\"push\"", json, "settings.push solo cuando deja su default");
            StringAssert.DoesNotContain("last_seen_at", json);
            StringAssert.DoesNotContain("last_win_at", json);
            StringAssert.Contains("\"sessions\":0", json.Replace(" ", ""));
        }

        [Test]
        public void TheNewKeys_RoundTrip()
        {
            Profile p = Wallet.NewProfile(new ManualClock(T0));
            p.Stats.Sessions = 7;
            p.Stats.LastSeenAt = T0.AddDays(-8);
            p.Stats.LastWinAt = T0.AddDays(-9);
            p.Settings.Push.PrePromptShown = true;
            p.Settings.Push.PrePromptAt = T0.AddDays(-20);
            p.Settings.Push.SecondPromptShown = true;
            p.Settings.Push.Granted = true;

            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"push\"", json);
            StringAssert.Contains("\"pre_prompt_shown\"", json);
            StringAssert.Contains("\"granted\"", json);
            Profile back = ProfileJson.Deserialize(json);
            Assert.That(back.Stats.Sessions, Is.EqualTo(7));
            Assert.That(back.Stats.LastSeenAt, Is.EqualTo(T0.AddDays(-8)));
            Assert.That(back.Stats.LastWinAt, Is.EqualTo(T0.AddDays(-9)));
            Assert.That(back.Settings.Push.PrePromptShown, Is.True);
            Assert.That(back.Settings.Push.PrePromptAt, Is.EqualTo(T0.AddDays(-20)));
            Assert.That(back.Settings.Push.SecondPromptShown, Is.True);
            Assert.That(back.Settings.Push.Granted, Is.True);
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(json), "estable");
        }

        [Test]
        public void SecondDueSession_RoundTrips_OnlyWhenSet_AndThroughThePushStore() // D-033, UI de los avisos (b)
        {
            Profile p = Wallet.NewProfile(new ManualClock(T0));
            p.Settings.Push.PrePromptShown = true;
            StringAssert.DoesNotContain("second_due_session", ProfileJson.Serialize(p), "0: no se escribe");
            var disk = new MemoryProfileStore();
            var store = new ProfilePushSettingsStore(p, disk);
            PushSettingsRecord r = store.Load();
            r.SecondDueSession = 4;
            store.Save(r);
            Assert.That(p.Settings.Push.SecondDueSession, Is.EqualTo(4));
            Profile back = ProfileJson.Deserialize(disk.Json!);
            Assert.That(back.Settings.Push.SecondDueSession, Is.EqualTo(4));
            Assert.That(new ProfilePushSettingsStore(back, new MemoryProfileStore()).Load().SecondDueSession, Is.EqualTo(4));

            var fresh = Wallet.NewProfile(new ManualClock(T0));
            fresh.Settings.Push.SecondDueSession = 2;
            Assert.That(fresh.Settings.Push.IsDefault, Is.False, "solo el recuerdo ya se escribe");
            string bad = ProfileJson.Serialize(fresh).Replace("\"second_due_session\": 2", "\"second_due_session\": -3")
                .Replace("\"second_due_session\":2", "\"second_due_session\":-3");
            Assert.That(ProfileJson.Deserialize(bad).Settings.Push.SecondDueSession, Is.EqualTo(0), "un negativo, 0");
        }

        [Test]
        public void BadValues_TakeTheirDefault_WithoutCorruptingTheProfile()
        {
            Profile p = Wallet.NewProfile(new ManualClock(T0));
            p.Stats.LastSeenAt = T0.AddHours(1);
            p.Settings.Push.Granted = true;
            string json = ProfileJson.Serialize(p)
                .Replace(ProfileJsonTimestamp(T0.AddHours(1)), "ayer")
                .Replace("\"granted\": true", "\"granted\": \"sí\"")
                .Replace("\"granted\":true", "\"granted\":\"sí\"");
            Profile back = ProfileJson.Deserialize(json);
            Assert.That(back.Stats.LastSeenAt, Is.Null);
            Assert.That(back.Settings.Push.Granted, Is.False);
        }

        [Test]
        public void SessionStore_WritesStatsAndSaves()
        {
            var clock = new ManualClock(T0);
            Profile p = Wallet.NewProfile(clock);
            p.Stats.Sessions = 3;
            var disk = new MemoryProfileStore();
            var store = new ProfileSessionStore(p, disk);
            var tracker = new SessionTracker(store, clock);
            tracker.OnColdStart();
            Assert.That(p.Stats.Sessions, Is.EqualTo(4));
            Assert.That(p.Stats.LastSeenAt, Is.EqualTo(T0));
            Assert.That(disk.SaveCount, Is.EqualTo(1));
            tracker.OnVictory();
            Assert.That(p.Stats.LastWinAt, Is.EqualTo(T0));
            Assert.That(ProfileJson.Deserialize(disk.Json!).Stats.Sessions, Is.EqualTo(4));
            Assert.That(store.PendingSave, Is.False);
        }

        [Test]
        public void PushStore_WritesSettingsPushAndSaves()
        {
            Profile p = Wallet.NewProfile(new ManualClock(T0));
            var disk = new MemoryProfileStore();
            var store = new ProfilePushSettingsStore(p, disk);
            PushSettingsRecord r = store.Load();
            r.PrePromptShown = true;
            r.PrePromptAt = T0;
            store.Save(r);
            Assert.That(p.Settings.Push.PrePromptShown, Is.True);
            Assert.That(ProfileJson.Deserialize(disk.Json!).Settings.Push.PrePromptAt, Is.EqualTo(T0));

            var copy = new PushSettingsRecord { Granted = true };
            store.Save(copy);
            Assert.That(store.Load().Granted, Is.True, "otra instancia se copia en el registro vivo");
            Assert.That(store.Load().PrePromptShown, Is.False);
        }

        [Test]
        public void TheComeback_IsPaidAfterLastSeenIsOnDisk_InTheSameProfile()
        {
            var clock = new ManualClock(T0);
            Profile p = Wallet.NewProfile(clock);
            p.Stats.LastSeenAt = T0.AddDays(-8);
            var disk = new MemoryProfileStore();
            var wallet = new Wallet(p, disk, clock);
            var tracker = new SessionTracker(new ProfileSessionStore(p, disk), clock);
            SessionStart start = tracker.OnColdStart();
            ComebackOutcome o = new ComebackService(wallet).OnSessionStart(in start, 0);
            Assert.That(o.Granted, Is.True);

            // Al reabrir con lo que quedó en disco, la ausencia ya no cuenta.
            Profile reopened = ProfileJson.Deserialize(disk.Json!);
            Assert.That(reopened.Stats.LastSeenAt, Is.EqualTo(T0));
            Assert.That(reopened.Wallet.Seeds, Is.EqualTo(p.Wallet.Seeds));
            var tracker2 = new SessionTracker(new ProfileSessionStore(reopened, disk), clock);
            SessionStart again = tracker2.OnColdStart();
            Assert.That(new ComebackService(new Wallet(reopened, disk, clock)).OnSessionStart(in again, 0).Granted, Is.False);
        }

        private static string ProfileJsonTimestamp(DateTime t)
        {
            Profile probe = Wallet.NewProfile(new ManualClock(t));
            string json = ProfileJson.Serialize(probe);
            int i = json.IndexOf("\"created_at\"", StringComparison.Ordinal);
            int open = json.IndexOf('"', json.IndexOf(':', i) + 1);
            int close = json.IndexOf('"', open + 1);
            return json.Substring(open + 1, close - open - 1);
        }
    }
}
