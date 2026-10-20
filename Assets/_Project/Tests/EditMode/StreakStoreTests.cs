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

        [Test]
        public void Normalize_ClampsWhatTheRulesCannotProduce()
        {
            var r = new StreakRecord { Wins = 14, SeedsToday = -5 };
            r.HitsToday.AddRange(new[] { 6, 4, 3, 6, 10, -1 });

            Assert.That(r.Normalize(), Is.True);
            Assert.That(r.Wins, Is.EqualTo(StreakRecord.MaxWins));
            Assert.That(r.SeedsToday, Is.EqualTo(0));
            Assert.That(r.HitsToday, Is.EqualTo(new[] { 3, 6, 10 }));
            Assert.That(r.Normalize(), Is.False, "ya está en rango");

            var low = new StreakRecord { Wins = -2 };
            low.HitsToday.Add(6);
            low.HitsToday.Add(3);
            Assert.That(low.Normalize(), Is.True);
            Assert.That(low.Wins, Is.EqualTo(0));
            Assert.That(low.HitsToday, Is.EqualTo(new[] { 3, 6 }), "orden creciente");
        }

        [Test]
        public void Load_ClampsTheStoredState()
        {
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 10, 0, 0)), Bogota);
            var bad = new StreakRecord { Wins = 99, DateLocal = "2026-10-19", SeedsToday = -1 };
            bad.HitsToday.AddRange(new[] { 3, 3, 7 });

            StreakRecord r = new InMemoryStreakStore(clock, bad).Load();

            Assert.That(r.Wins, Is.EqualTo(10));
            Assert.That(r.SeedsToday, Is.EqualTo(0));
            Assert.That(r.HitsToday, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void StaleRecord_HitPaidBeforeMidnight_IsNotPaidAgainToday()
        {
            // (a) Load a las 23:59, otro módulo pasa el día y el hito 3 se guarda con el registro de ayer: cuenta como pagado hoy.
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 23, 59, 0)), Bogota);
            var store = new InMemoryStreakStore(clock);
            StreakRecord r = store.Load();
            r.Wins = 3;

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            store.SimulateDayChangeByAnotherModule();
            r.HitsToday.Add(3);
            r.SeedsToday += 300;
            store.Save(r);

            StreakRecord today = store.Load();
            Assert.That(today.DateLocal, Is.EqualTo("2026-10-20"));
            Assert.That(today.HitsToday, Is.EqualTo(new[] { 3 }), "el hito 3 no se vuelve a pagar hoy");
            Assert.That(today.SeedsToday, Is.EqualTo(300), "y sus semillas cuentan para el tope de hoy");
            Assert.That(today.Wins, Is.EqualTo(3));
            Assert.That(today.ShieldAvailable, Is.True);
        }

        [Test]
        public void StaleRecord_ShieldSpentBeforeMidnight_LeavesNoSecondShieldToday()
        {
            // (b) El escudo se gasta con el registro de ayer: hoy no queda otro.
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 23, 59, 0)), Bogota);
            var store = new InMemoryStreakStore(clock, new StreakRecord { Wins = 4 });
            StreakRecord r = store.Load();
            Assert.That(r.ShieldAvailable, Is.True);

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            store.SimulateDayChangeByAnotherModule();
            r.ShieldAvailable = false;
            store.Save(r);

            Assert.That(store.Load().ShieldAvailable, Is.False, "no hay segundo escudo hoy");
            Assert.That(store.Load().Wins, Is.EqualTo(4));
        }

        [Test]
        public void StaleRecord_ShieldSpentYesterday_DoesNotDenyTodaysShield()
        {
            // (c) El registro de ayer ya llevaba el escudo gastado al cargarse: no niega el de hoy.
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 23, 59, 0)), Bogota);
            var spent = new StreakRecord { Wins = 2, DateLocal = "2026-10-19", ShieldAvailable = false, SeedsToday = 300 };
            spent.HitsToday.Add(3);
            var store = new InMemoryStreakStore(clock, spent);
            StreakRecord r = store.Load();

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            store.SimulateDayChangeByAnotherModule();
            r.Wins = 3;
            store.Save(r);

            StreakRecord today = store.Load();
            Assert.That(today.ShieldAvailable, Is.True, "el escudo de hoy sigue disponible");
            Assert.That(today.HitsToday, Is.Empty, "el hito 3 de ayer no cuenta hoy");
            Assert.That(today.SeedsToday, Is.EqualTo(0));
            Assert.That(today.Wins, Is.EqualTo(3), "la racha se escribe siempre");
        }

        [Test]
        public void MergeDay_RecordFromALaterDay_RollsFirst_AndRepeatedSavesDoNotAddTwice()
        {
            var current = new StreakRecord { DateLocal = "2026-10-19", ShieldAvailable = false, SeedsToday = 350 };
            current.HitsToday.Add(3);
            var later = new StreakRecord { DateLocal = "2026-10-20", ShieldAvailable = true, SeedsToday = 50 };
            later.HitsToday.Add(6);

            Assert.That(StreakRecord.MergeDay(current, new StreakRecord(), later), Is.True);
            Assert.That(current.DateLocal, Is.EqualTo("2026-10-20"));
            Assert.That(current.HitsToday, Is.EqualTo(new[] { 6 }));
            Assert.That(current.SeedsToday, Is.EqualTo(50));

            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 23, 59, 0)), Bogota);
            var store = new InMemoryStreakStore(clock);
            StreakRecord r = store.Load();
            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            store.SimulateDayChangeByAnotherModule();
            r.SeedsToday = 300;
            store.Save(r);
            store.Save(r); // el mismo registro otra vez: el aumento ya se aplicó
            Assert.That(store.Current.SeedsToday, Is.EqualTo(300));
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
