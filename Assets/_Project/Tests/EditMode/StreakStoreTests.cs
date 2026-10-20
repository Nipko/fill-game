#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <see cref="StreakRecord"/> e <see cref="InMemoryStreakStore"/> (B1a, tarea A4; 06 §4.3 y Q-M9 (a) [P]): el cambio de día
    /// local solo hacia delante repone la Hoja escudo y pone a cero hitos y semillas de hoy sin tocar la racha ni el río crecido;
    /// Load nunca da null; el patrón Save + PendingSave + Flush, como el almacén del Reto.
    /// </summary>
    public sealed class StreakStoreTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        [Test]
        public void RollOver_NewDay_RefillsShieldAndClearsToday_ButKeepsTheStreak()
        {
            StreakRecord r = Sample("2026-10-19");

            Assert.That(r.RollOver("2026-10-20"), Is.True);

            Assert.That(r.DateLocal, Is.EqualTo("2026-10-20"));
            Assert.That(r.ShieldAvailable, Is.True);
            Assert.That(r.HitsToday, Is.Empty);
            Assert.That(r.SeedsToday, Is.EqualTo(0));
            Assert.That(r.Wins, Is.EqualTo(7), "sin expiración por reloj (04 §8)");
            Assert.That(r.RiverFull, Is.True);
        }

        [TestCase("2026-10-19")]
        [TestCase("2026-10-18")]
        public void RollOver_SameDayOrClockBack_ChangesNothing(string today)
        {
            StreakRecord r = Sample("2026-10-19");

            Assert.That(r.RollOver(today), Is.False);

            Assert.That(r.DateLocal, Is.EqualTo("2026-10-19"));
            Assert.That(r.ShieldAvailable, Is.False, "atrasar el reloj no repone la Hoja escudo");
            Assert.That(r.HitsToday, Is.EqualTo(new[] { 3, 6 }));
            Assert.That(r.SeedsToday, Is.EqualTo(350));
        }

        [Test]
        public void RollOver_FirstDay_GivesTheShield()
        {
            var r = new StreakRecord();

            Assert.That(r.RollOver("2026-10-19"), Is.True);
            Assert.That(r.ShieldAvailable, Is.True);
            Assert.Throws<ArgumentNullException>(() => r.RollOver(null!));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            StreakRecord r = Sample("2026-10-19");
            StreakRecord c = r.Clone();
            r.HitsToday.Add(10);
            r.Wins = 0;

            Assert.That(c.HitsToday, Is.EqualTo(new[] { 3, 6 }));
            Assert.That(c.Wins, Is.EqualTo(7));
            Assert.That(c.RiverFull, Is.True);
            Assert.That(c.DateLocal, Is.EqualTo("2026-10-19"));
            Assert.That(c.SeedsToday, Is.EqualTo(350));
        }

        [Test]
        public void Load_IsNeverNull_IsTheLiveRecord_AndRollsToTheClockDay()
        {
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 23, 0, 0)), Bogota);
            var store = new InMemoryStreakStore(clock, Sample("2026-10-19"));

            StreakRecord r = store.Load();
            Assert.That(r, Is.Not.Null);
            Assert.That(store.Load(), Is.SameAs(r));
            Assert.That(r.ShieldAvailable, Is.False, "mismo día");

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 5, 0)));
            Assert.That(store.Load().ShieldAvailable, Is.True, "medianoche local");
            Assert.That(store.Load().HitsToday, Is.Empty);
            Assert.That(new InMemoryStreakStore(clock).Load(), Is.Not.Null);
        }

        [Test]
        public void FailedWrite_StaysPending_UntilFlush_AndReopenLosesIt()
        {
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 10, 0, 0)), Bogota);
            var store = new InMemoryStreakStore(clock) { FailuresLeft = 1 };
            StreakRecord r = store.Load();
            r.Wins = 3;
            r.HitsToday.Add(3);

            Assert.DoesNotThrow(() => store.Save(r));
            Assert.That(store.PendingSave, Is.True);
            Assert.That(store.Persisted.Wins, Is.EqualTo(0));
            Assert.That(store.Flush(), Is.True);
            Assert.That(store.PendingSave, Is.False);
            Assert.That(store.Persisted.HitsToday, Is.EqualTo(new[] { 3 }));
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(store.FailedWriteCount, Is.EqualTo(1));

            store.FailWrites = true;
            r.Wins = 4;
            store.Save(r);
            store.Reopen();
            Assert.That(store.Load().Wins, Is.EqualTo(3), "lo que no se escribió se pierde al reabrir");
            Assert.That(store.Load(), Is.Not.SameAs(r));
        }

        private static StreakRecord Sample(string date)
        {
            var r = new StreakRecord { Wins = 7, RiverFull = true, DateLocal = date, ShieldAvailable = false, SeedsToday = 350 };
            r.HitsToday.Add(3);
            r.HitsToday.Add(6);
            return r;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
