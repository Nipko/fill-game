#nullable enable
using System;
using NUnit.Framework;

namespace AF.Analytics.Tests
{
    /// <summary><see cref="SessionTracker"/>: D-033, Q-M4 (a), sesión = arranque en frío o vuelta tras ≥ 30 min en segundo plano.</summary>
    public sealed class SessionTrackerTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ColdStart_IsSessionOne_AndUnder30MinOutsideIsTheSameSession()
        {
            int n = 0;
            var s = new SessionTracker(T0, () => "id" + (++n));
            Assert.That(s.Id, Is.EqualTo("id1"));
            Assert.That(s.Count, Is.EqualTo(1));
            s.Pause(T0.AddMinutes(5));
            Assert.That(s.Resume(T0.AddMinutes(34).AddSeconds(59)), Is.False, "29:59 fuera");
            Assert.That(s.Id, Is.EqualTo("id1"));
        }

        [Test]
        public void ThirtyMinutesOutside_StartsANewSession()
        {
            int n = 0;
            string? started = null;
            var s = new SessionTracker(T0, () => "id" + (++n));
            s.SessionStarted += id => started = id;
            s.Pause(T0.AddMinutes(5));
            Assert.That(s.Resume(T0.AddMinutes(35)), Is.True);
            Assert.That(s.Id, Is.EqualTo("id2"));
            Assert.That(started, Is.EqualTo("id2"));
            Assert.That(s.Count, Is.EqualTo(2));
            Assert.That(s.StartedUtc, Is.EqualTo(T0.AddMinutes(35)));
            Assert.That(s.Resume(T0.AddHours(5)), Is.False, "sin pausa antes no hay sesión nueva");
        }

        [Test]
        public void TheDefaultId_Is32HexDigits_AndChanges()
        {
            var a = new SessionTracker(T0);
            var b = new SessionTracker(T0);
            Assert.That(a.Id.Length, Is.EqualTo(32));
            Assert.That(a.Id, Is.Not.EqualTo(b.Id));
        }
    }
}
