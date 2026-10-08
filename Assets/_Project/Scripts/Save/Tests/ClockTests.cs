#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Reloj inyectable de <c>AF.Platform</c> (B1-1; GDD 08 §2.2): <see cref="SystemClock"/> da UTC,
    /// <see cref="ManualClock"/> solo cambia a mano (también hacia atrás) y la fecha local de los contadores diarios
    /// (04 §10.1 <c>daily.date_local</c>) sale de <c>UtcNow + LocalOffset</c> con el formato <c>yyyy-MM-dd</c>.
    /// </summary>
    public sealed class ClockTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void SystemClock_ReturnsUtc_AndASaneLocalOffset()
        {
            DateTime now = SystemClock.Instance.UtcNow;

            Assert.That(now.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(now.Year, Is.GreaterThanOrEqualTo(2026));
            Assert.That(Math.Abs(SystemClock.Instance.LocalOffset.TotalHours), Is.LessThanOrEqualTo(14));
        }

        [Test]
        public void ManualClock_OnlyMovesWhenTold()
        {
            var clock = new ManualClock(T0);
            Assert.That(clock.UtcNow, Is.EqualTo(T0));
            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(clock.UtcNow, Is.EqualTo(T0), "leerlo dos veces no avanza");

            clock.Advance(TimeSpan.FromMinutes(25));
            Assert.That(clock.UtcNow, Is.EqualTo(T0.AddMinutes(25)));

            clock.Advance(TimeSpan.FromMinutes(-50));
            Assert.That(clock.UtcNow, Is.EqualTo(T0.AddMinutes(-25)), "puede retroceder, como un reloj cambiado a mano");

            clock.Set(T0.AddDays(1));
            Assert.That(clock.UtcNow, Is.EqualTo(T0.AddDays(1)));
            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void ManualClock_TakesUnspecifiedAsUtc_AndRejectsLocalTimes()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Unspecified));
            Assert.That(clock.UtcNow, Is.EqualTo(T0));
            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));

            DateTime local = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Local);
            Assert.Throws<ArgumentException>(() => new ManualClock(local));
            Assert.Throws<ArgumentException>(() => clock.Set(local));
            Assert.Throws<ArgumentException>(() => clock.LocalOffset = TimeSpan.FromHours(15));
        }

        [Test]
        public void LocalDate_UsesTheLocalOffset_ToCrossMidnight()
        {
            // 2026-10-09 02:30 UTC es todavía el 8 de octubre en Bogotá (UTC−5) y ya el 9 en UTC.
            var clock = new ManualClock(new DateTime(2026, 10, 9, 2, 30, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));

            Assert.That(clock.LocalNow(), Is.EqualTo(new DateTime(2026, 10, 8, 21, 30, 0)));
            Assert.That(clock.LocalNow().Kind, Is.EqualTo(DateTimeKind.Unspecified));
            Assert.That(clock.LocalDate(), Is.EqualTo(new DateTime(2026, 10, 8)));
            Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-08"));

            clock.Advance(TimeSpan.FromHours(3));
            Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-09"), "las 00:00 locales cambian el día");

            clock.LocalOffset = TimeSpan.Zero;
            Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-09"));
        }

        [Test]
        public void DateKey_IsGregorianAndInvariant_InAnyCulture()
        {
            var clock = new ManualClock(T0);
            using (CultureScope.Use(CultureScope.Hostile()))
            {
                Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-08"));
            }

            using (CultureScope.Use(CultureScope.ThaiOrHostile()))
            {
                Assert.That(ClockExtensions.FormatDateKey(new DateTime(2026, 10, 8)), Is.EqualTo("2026-10-08"));
            }
        }
    }
}
