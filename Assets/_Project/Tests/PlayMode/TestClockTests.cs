#nullable enable
using System;
using System.Collections;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El reloj de los tests de PlayMode (T6, carril A; <see cref="TestClock"/>): empieza en el ancla, corre con el tiempo real,
    /// se adelanta a mano, se desplaza con <see cref="TestClock.ShiftVariable"/> y es el del juego en toda escena que el test
    /// arranca sin su propio reloj (<see cref="PlayModeHelpers.NoRealClockUnlessSet"/>).
    /// </summary>
    public sealed class TestClockTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [Test]
        public void StartsAtTheAnchor_InBogota_RunsWithRealTime_AndAdvancesByHand()
        {
            var clock = new TestClock();
            DateTime first = clock.UtcNow;
            Assert.That(first.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(first, Is.GreaterThanOrEqualTo(TestClock.Anchor));
            Assert.That(first, Is.LessThan(TestClock.Anchor.AddSeconds(5)), "recién creado, en el ancla");
            Assert.That(clock.LocalOffset, Is.EqualTo(TimeSpan.FromHours(-5)), "Bogotá");
            Assert.That(clock.UtcNow, Is.GreaterThanOrEqualTo(first), "nunca va hacia atrás solo");

            clock.Advance(TimeSpan.FromDays(1));
            Assert.That(clock.UtcNow, Is.GreaterThanOrEqualTo(first.AddDays(1)));
            Assert.That(clock.UtcNow, Is.LessThan(first.AddDays(1).AddSeconds(5)));
            clock.Advance(TimeSpan.FromDays(-2));
            Assert.That(clock.UtcNow, Is.LessThan(first), "puede retroceder, como un reloj cambiado a mano");

            Assert.Throws<ArgumentException>(() => new TestClock(new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Local)));
            Assert.Throws<ArgumentException>(() => clock.LocalOffset = TimeSpan.FromHours(15));
        }

        [Test]
        public void TheAnchor_IsTheBaseOne_PlusTheShiftOfTheEnvironment()
        {
            Assert.That(TestClock.BaseAnchor, Is.EqualTo(new DateTime(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc)));
            Assert.That(TestClock.BaseAnchor.DayOfWeek, Is.EqualTo(DayOfWeek.Saturday));
            string? before = Environment.GetEnvironmentVariable(TestClock.ShiftVariable);
            try
            {
                Environment.SetEnvironmentVariable(TestClock.ShiftVariable, "12");
                Assert.That(TestClock.Anchor, Is.EqualTo(TestClock.BaseAnchor.AddHours(12)));
                Assert.That(new TestClock().UtcNow, Is.GreaterThanOrEqualTo(TestClock.BaseAnchor.AddHours(12)));
                Environment.SetEnvironmentVariable(TestClock.ShiftVariable, "-3.5");
                Assert.That(TestClock.Anchor, Is.EqualTo(TestClock.BaseAnchor.AddHours(-3.5)));
                Environment.SetEnvironmentVariable(TestClock.ShiftVariable, "doce");
                Assert.That(TestClock.Anchor, Is.EqualTo(TestClock.BaseAnchor), "lo que no se entiende no mueve el ancla");
                Environment.SetEnvironmentVariable(TestClock.ShiftVariable, null);
                Assert.That(TestClock.Anchor, Is.EqualTo(TestClock.BaseAnchor));
            }
            finally
            {
                Environment.SetEnvironmentVariable(TestClock.ShiftVariable, before);
            }
        }

        [Test]
        public void Install_TakesTheTestClockInPlace_OrPutsOne_ButNeverReplacesAnother()
        {
            TestClock clock = TestClock.Install();
            Assert.That(LaunchOptions.ClockOverride, Is.SameAs(clock));
            Assert.That(TestClock.Install(), Is.SameAs(clock));
            Assert.That(TestClock.Installed, Is.SameAs(clock));

            LaunchOptions.ClockOverride = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc));
            Assert.Throws<InvalidOperationException>(() => TestClock.Install(), "el test ya fijó su reloj");
            Assert.That(TestClock.Installed, Is.Null);
        }

        [UnityTest]
        public IEnumerator TheScene_WithoutAClockOfItsOwn_RunsOnTheTestClock()
        {
            Assert.That(LaunchOptions.ClockOverride, Is.Null, "condición del test: sin reloj propio");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(LaunchOptions.ClockOverride, Is.InstanceOf<TestClock>(), "PlayModeHelpers pone el TestClock");
            DateTime now = boot.CapsClock!.UtcNow; // el reloj de los topes, sobre el del dispositivo (sin servidor)
            Assert.That(now, Is.GreaterThanOrEqualTo(TestClock.Anchor), "la fecha del juego es la del ancla, no la del sistema");
            Assert.That(now, Is.LessThan(TestClock.Anchor.AddMinutes(5)));
            Assert.That(boot.CapsClock.LocalOffset, Is.EqualTo(TestClock.Bogota));
        }

        [UnityTest]
        public IEnumerator TheScene_KeepsTheClockTheTestPut()
        {
            var manual = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = manual;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            Assert.That(LaunchOptions.ClockOverride, Is.SameAs(manual));
            Assert.That(GameBootstrap.Current!.CapsClock!.UtcNow, Is.EqualTo(manual.UtcNow));
        }
    }
}
