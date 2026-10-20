#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// B6: sesiones (Q-M4 (a) de D-033: arranque en frío o vuelta tras ≥ 30 min en segundo plano) y Regalo de regreso (06 §9;
    /// <c>live_comeback_gift</c>, 08 §9).
    /// </summary>
    public sealed class SessionComebackTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ColdStart_CountsOneSession_AndOnlyOncePerProcess()
        {
            var clock = new ManualClock(T0, Bogota);
            var store = new InMemorySessionStore();
            var tracker = new SessionTracker(store, clock);

            SessionStart first = tracker.OnColdStart();
            Assert.That(first.Number, Is.EqualTo(1));
            Assert.That(first.Kind, Is.EqualTo(SessionStartKind.ColdStart));
            Assert.That(first.PreviousSeenAt, Is.Null);
            Assert.That(first.Away, Is.EqualTo(TimeSpan.Zero));
            Assert.That(store.Load().LastSeenAt, Is.EqualTo(T0));

            Assert.That(tracker.OnColdStart().Number, Is.EqualTo(1), "una segunda llamada no cuenta otra");
            Assert.That(tracker.Sessions, Is.EqualTo(1));
        }

        [Test]
        public void Resume_IsANewSession_OnlyAfterThirtyMinutesInBackground()
        {
            var clock = new ManualClock(T0, Bogota);
            var store = new InMemorySessionStore(new SessionRecord { Sessions = 4 });
            var tracker = new SessionTracker(store, clock);
            tracker.OnColdStart();
            Assert.That(tracker.Sessions, Is.EqualTo(5));

            tracker.OnBackground();
            Assert.That(tracker.InBackground, Is.True);
            clock.Advance(TimeSpan.FromMinutes(29) + TimeSpan.FromSeconds(59));
            Assert.That(tracker.OnForeground(), Is.Null, "29:59 fuera es la misma sesión");
            Assert.That(tracker.Sessions, Is.EqualTo(5));

            tracker.OnBackground();
            DateTime pausedAt = clock.UtcNow;
            clock.Advance(SessionTracker.BackgroundGap);
            SessionStart? resumed = tracker.OnForeground();
            Assert.That(resumed.HasValue, Is.True, "30 min justos abren otra");
            Assert.That(resumed!.Value.Kind, Is.EqualTo(SessionStartKind.Resume));
            Assert.That(resumed.Value.Number, Is.EqualTo(6));
            Assert.That(resumed.Value.PreviousSeenAt, Is.EqualTo(pausedAt));
            Assert.That(resumed.Value.Away, Is.EqualTo(SessionTracker.BackgroundGap));
            Assert.That(store.Load().LastSeenAt, Is.EqualTo(clock.UtcNow));
        }

        [Test]
        public void Foreground_WithoutBackground_OrWithTheClockBack_KeepsTheSession()
        {
            var clock = new ManualClock(T0, Bogota);
            var tracker = new SessionTracker(new InMemorySessionStore(), clock);
            Assert.That(tracker.OnForeground(), Is.Null, "sin arranque ni segundo plano");
            tracker.OnColdStart();
            Assert.That(tracker.OnForeground(), Is.Null, "sin segundo plano previo");

            tracker.OnBackground();
            clock.Advance(TimeSpan.FromHours(-3));
            Assert.That(tracker.OnForeground(), Is.Null, "el reloj atrasado no abre sesión");
            Assert.That(tracker.Sessions, Is.EqualTo(1));
        }

        [Test]
        public void Background_TwiceInARow_KeepsTheFirstPauseTime()
        {
            var clock = new ManualClock(T0, Bogota);
            var store = new InMemorySessionStore();
            var tracker = new SessionTracker(store, clock);
            tracker.OnColdStart();
            tracker.OnBackground();
            clock.Advance(TimeSpan.FromMinutes(20));
            tracker.OnBackground(); // pausa + cierre de la app
            clock.Advance(TimeSpan.FromMinutes(10));
            Assert.That(tracker.OnForeground().HasValue, Is.True, "se cuenta desde la primera pausa");
        }

        [Test]
        public void Victory_WritesLastWinAt()
        {
            var clock = new ManualClock(T0, Bogota);
            var store = new InMemorySessionStore();
            var tracker = new SessionTracker(store, clock);
            tracker.OnColdStart();
            clock.Advance(TimeSpan.FromMinutes(3));
            tracker.OnVictory();
            Assert.That(store.Load().LastWinAt, Is.EqualTo(T0.AddMinutes(3)));
            Assert.That(store.Load().Clone().LastWinAt, Is.EqualTo(T0.AddMinutes(3)));
        }

        [TestCase(0, ComebackSegment.None)]
        [TestCase(2, ComebackSegment.None)]
        [TestCase(3, ComebackSegment.AtRisk)]
        [TestCase(6, ComebackSegment.AtRisk)]
        [TestCase(7, ComebackSegment.Dormant)]
        [TestCase(13, ComebackSegment.Dormant)]
        [TestCase(14, ComebackSegment.Lost)]
        [TestCase(29, ComebackSegment.Lost)]
        [TestCase(30, ComebackSegment.Churn)]
        [TestCase(400, ComebackSegment.Churn)]
        public void Segments_AreThoseOf06_9(int days, ComebackSegment expected)
        {
            ComebackGift gift = ComebackGift.Default;
            Assert.That(ComebackRules.Segment(days, in gift), Is.EqualTo(expected));
        }

        [Test]
        public void DaysAway_CountsWholePeriodsOf24Hours()
        {
            Assert.That(ComebackRules.DaysAway(TimeSpan.FromDays(7) - TimeSpan.FromSeconds(1)), Is.EqualTo(6));
            Assert.That(ComebackRules.DaysAway(TimeSpan.FromDays(7)), Is.EqualTo(7));
            Assert.That(ComebackRules.DaysAway(TimeSpan.FromHours(-5)), Is.EqualTo(0));
            Assert.That(ComebackRules.DaysAway(TimeSpan.MaxValue), Is.EqualTo((int)Math.Floor(TimeSpan.MaxValue.TotalDays)));
        }

        [Test]
        public void ComebackGift_ParsesThe08_9Default_AndRejectsNonsense()
        {
            Assert.That(ComebackGift.TryParse("{\"days\":7,\"seeds\":200,\"extra_cuia\":1,\"refill_canoes\":true}", out ComebackGift g), Is.True);
            Assert.That(g, Is.EqualTo(ComebackGift.Default));
            Assert.That(g.Format(), Is.EqualTo("{\"days\":7,\"seeds\":200,\"extra_cuia\":1,\"refill_canoes\":true}"));

            Assert.That(ComebackGift.TryParse("{\"days\":10,\"seeds\":0,\"extra_cuia\":2,\"refill_canoes\":false}", out g), Is.True);
            Assert.That(g, Is.EqualTo(new ComebackGift(10, 0, 2, false)));

            string[] bad =
            {
                null!, "", "[]", "{", "{\"days\":7,\"seeds\":200,\"extra_cuia\":1}",
                "{\"days\":0,\"seeds\":200,\"extra_cuia\":1,\"refill_canoes\":true}",
                "{\"days\":7,\"seeds\":-1,\"extra_cuia\":1,\"refill_canoes\":true}",
                "{\"days\":7,\"seeds\":\"200\",\"extra_cuia\":1,\"refill_canoes\":true}",
                "{\"days\":7,\"seeds\":200,\"extra_cuia\":1,\"refill_canoes\":1}",
                "{\"days\":7,\"seeds\":200,\"extra_cuia\":1,\"refill_canoes\":true,\"x\":1}",
                "{\"days\":7.5,\"seeds\":200,\"extra_cuia\":1,\"refill_canoes\":true}",
            };
            foreach (string json in bad)
            {
                Assert.That(ComebackGift.TryParse(json, out g), Is.False, json ?? "null");
                Assert.That(g, Is.EqualTo(ComebackGift.Default), "el default si no vale");
            }
        }

        [Test]
        public void ComebackConfig_ValidatesItsRanges()
        {
            ComebackConfig.Defaults.Validate();
            foreach (ComebackGift gift in new[] { new ComebackGift(0, 200, 1, true), new ComebackGift(14, 200, 1, true), new ComebackGift(7, -1, 1, true), new ComebackGift(7, 200, -1, true) })
            {
                var config = new ComebackConfig { LiveComebackGift = gift };
                Assert.Throws<InvalidOperationException>(() => config.Validate(), gift.Format());
            }
        }

        [Test]
        public void SixDaysAway_GivesNothing()
        {
            var f = new Fixture();
            f.LeaveFor(TimeSpan.FromDays(7) - TimeSpan.FromMinutes(1));
            ComebackOutcome o = f.Return();
            Assert.That(o.Segment, Is.EqualTo(ComebackSegment.AtRisk));
            Assert.That(o.Granted, Is.False);
            Assert.That(f.Profile.Wallet.Seeds, Is.EqualTo(f.SeedsBefore));
        }

        [Test]
        public void SevenDaysAway_GivesTheGift_ThroughTheWallet()
        {
            var f = new Fixture();
            f.Profile.Wallet.Canoes.Count = 0;
            f.Profile.Wallet.Canoes.NextRegenAt = f.Clock.UtcNow.AddDays(30); // canoas vacías al volver (p. ej. reloj tocado)
            f.LeaveFor(TimeSpan.FromDays(7));
            ComebackOutcome o = f.Return(streakWins: 4);

            Assert.That(o.Segment, Is.EqualTo(ComebackSegment.Dormant));
            Assert.That(o.DaysAway, Is.EqualTo(7));
            Assert.That(o.Granted, Is.True);
            Assert.That(o.AnyRejected, Is.False);
            Assert.That(o.Seeds, Is.EqualTo(200));
            Assert.That(o.ExtraCuia, Is.EqualTo(1));
            Assert.That(o.CanoesAdded, Is.EqualTo(5));
            Assert.That(o.StreakWins, Is.EqualTo(4), "«Tu Crecida de {n} sigue en pie»");
            Assert.That(o.ShowReminder, Is.False);
            Assert.That(f.Profile.Wallet.Seeds, Is.EqualTo(f.SeedsBefore + 200));
            Assert.That(f.Profile.Stock.ExtraCuia, Is.EqualTo(1));
            Assert.That(f.Wallet.Canoes.IsFull, Is.True);
            Assert.That(f.ProfileStore.SaveCount, Is.GreaterThan(0), "cada transacción guarda el perfil");
        }

        [Test]
        public void FullCanoes_AreNotTouched()
        {
            var f = new Fixture();
            f.LeaveFor(TimeSpan.FromDays(9));
            ComebackOutcome o = f.Return();
            Assert.That(o.Granted, Is.True);
            Assert.That(o.CanoesAdded, Is.EqualTo(0), "tras 7 días las canoas ya se regeneraron solas");
            Assert.That(o.AnyRejected, Is.False);
        }

        [TestCase(14, ComebackSegment.Lost)]
        [TestCase(45, ComebackSegment.Churn)]
        public void LostAndChurn_GetTheGiftAndTheReminder(int days, ComebackSegment segment)
        {
            var f = new Fixture();
            f.LeaveFor(TimeSpan.FromDays(days));
            ComebackOutcome o = f.Return();
            Assert.That(o.Segment, Is.EqualTo(segment));
            Assert.That(o.Granted, Is.True);
            Assert.That(o.ShowReminder, Is.True, "recordatorio de una frase y etiqueta honesta (06 §9)");
        }

        [Test]
        public void TheFirstSessionEver_AndAProfileWithoutLastSeen_GetNothing()
        {
            var clock = new ManualClock(T0, Bogota);
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, new MemoryProfileStore(), clock);
            var service = new ComebackService(wallet);
            SessionStart start = new SessionTracker(new InMemorySessionStore(new SessionRecord { Sessions = 30 }), clock).OnColdStart();
            Assert.That(start.PreviousSeenAt, Is.Null);
            Assert.That(service.OnSessionStart(in start, 0).Granted, Is.False);
        }

        [Test]
        public void TheGift_IsNeverGivenTwiceForTheSameAbsence()
        {
            var f = new Fixture();
            f.LeaveFor(TimeSpan.FromDays(8));
            Assert.That(f.Return().Granted, Is.True);
            int seeds = f.Profile.Wallet.Seeds;

            // Vuelve a segundo plano 31 min: otra sesión, pero la ausencia ya se cobró (last_seen_at = la vuelta).
            f.LeaveFor(TimeSpan.FromMinutes(31));
            ComebackOutcome again = f.Return();
            Assert.That(again.Granted, Is.False);
            Assert.That(f.Profile.Wallet.Seeds, Is.EqualTo(seeds));
        }

        [Test]
        public void ClockDrift_BeyondTheLimit_BlocksTheGift()
        {
            var server = FakeServerDate.Online(TimeSpan.FromDays(-8)); // el teléfono va 8 días por delante del servidor
            var f = new Fixture(server);
            f.LeaveFor(TimeSpan.FromDays(8));
            ComebackOutcome o = f.Return();
            Assert.That(o.Segment, Is.EqualTo(ComebackSegment.Dormant));
            Assert.That(o.Granted, Is.False);
            Assert.That(o.Block, Is.EqualTo(ComebackBlock.ClockDrift));

            server.GoOnline(TimeSpan.FromHours(2));
            f.LeaveFor(TimeSpan.FromDays(8));
            Assert.That(f.Return().Granted, Is.True, "con un desfase dentro de sec_clock_max_drift_h, sí");
        }

        [Test]
        public void TheShield_IsBackAfterAnyAbsenceOfAWeek_ByTheDailyRollOver()
        {
            // 06 §9 pide «Hoja escudo activa»: no hace falta pagarla, la repone el cambio de día de la Crecida (06 §4.3).
            var record = new StreakRecord { DateLocal = "2026-10-12", ShieldAvailable = false, Wins = 3 };
            Assert.That(record.RollOver("2026-10-20"), Is.True);
            Assert.That(record.ShieldAvailable, Is.True);
            Assert.That(record.Wins, Is.EqualTo(3), "la Crecida no expira por reloj");
        }

        private sealed class Fixture
        {
            public Fixture(IServerDate? server = null)
            {
                Clock = new ManualClock(T0, Bogota);
                Profile = Wallet.NewProfile(Clock);
                ProfileStore = new MemoryProfileStore();
                Wallet = new Wallet(Profile, ProfileStore, Clock);
                Sessions = new InMemorySessionStore();
                Tracker = new SessionTracker(Sessions, Clock);
                Service = new ComebackService(Wallet, null, server);
                Tracker.OnColdStart();
                SeedsBefore = Profile.Wallet.Seeds;
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public MemoryProfileStore ProfileStore { get; }

            public Wallet Wallet { get; }

            public InMemorySessionStore Sessions { get; }

            public SessionTracker Tracker { get; }

            public ComebackService Service { get; }

            public int SeedsBefore { get; }

            public void LeaveFor(TimeSpan away)
            {
                Tracker.OnBackground();
                Clock.Advance(away);
            }

            public ComebackOutcome Return(int streakWins = 0)
            {
                SessionStart? start = Tracker.OnForeground();
                Assert.That(start.HasValue, Is.True, "la vuelta abre sesión");
                SessionStart s = start!.Value;
                return Service.OnSessionStart(in s, streakWins);
            }
        }
    }
}
