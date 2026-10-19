#nullable enable
using System;
using AF.Levels;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Calendario del Reto del río (R7a; 03 §16; 06 §4.1; nota <c>docs/dev/reto-del-rio.md</c> §6): día del pool con módulo
    /// positivo, bandas por <c>lvl_daily_bracket_levels</c>, desbloqueo y sustituciones <c>daily_overrides</c> (R-Q6).
    /// </summary>
    public sealed class DailyCalendarTests
    {
        // ---- Día del pool (nota §6, tabla) ----

        [TestCase(2026, 1, 1, 0, 0)]
        [TestCase(2026, 10, 19, 0, 291)]
        [TestCase(2027, 12, 31, 0, 729)]
        [TestCase(2028, 1, 1, 0, 0)]
        [TestCase(2025, 12, 31, 0, 729)]
        [TestCase(2026, 1, 1, -1, 729)]
        [TestCase(2026, 1, 2, 1, 2)]
        [TestCase(2026, 1, 1, 730, 0)]
        [TestCase(2026, 1, 1, -731, 729)]
        [TestCase(2024, 1, 1, 0, 729)]
        public void PoolDay_IsAPositiveModuloOf730(int year, int month, int day, int offset, int expected)
        {
            Assert.That(DailyCalendar.PoolDay(new DateTime(year, month, day), offset), Is.EqualTo(expected));
        }

        [Test]
        public void DaysSinceEpoch_IgnoresTheTime_AndIsNegativeBefore2026()
        {
            Assert.That(DailyCalendar.Epoch, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(DailyCalendar.PoolDays, Is.EqualTo(730));
            Assert.That(DailyCalendar.DaysSinceEpoch(new DateTime(2026, 10, 19, 23, 59, 59)), Is.EqualTo(291));
            Assert.That(DailyCalendar.DaysSinceEpoch(new DateTime(2025, 12, 31)), Is.EqualTo(-1));
        }

        [Test]
        public void PoolDay_DoesNotOverflow_WithExtremeOffsets()
        {
            DateTime date = new DateTime(9999, 12, 31);
            int a = DailyCalendar.PoolDay(date, int.MaxValue);
            int b = DailyCalendar.PoolDay(DateTime.MinValue, int.MinValue);
            Assert.That(a, Is.InRange(0, 729));
            Assert.That(b, Is.InRange(0, 729));
        }

        // ---- Bandas (03 §16; [41, 201]) ----

        [TestCase(0, 1)]
        [TestCase(10, 1)]
        [TestCase(40, 1)]
        [TestCase(41, 2)]
        [TestCase(200, 2)]
        [TestCase(201, 3)]
        [TestCase(5000, 3)]
        public void BandFor_UsesTheDefaultThresholds(int maxLevel, int expected)
        {
            Assert.That(DailyCalendar.BandFor(maxLevel, DailyChallengeConfig.Defaults.LvlDailyBracketLevels), Is.EqualTo(expected));
        }

        [Test]
        public void BandFor_FollowsOtherThresholds_AndRejectsBadOnes()
        {
            Assert.That(DailyCalendar.BandFor(29, new[] { 30, 100 }), Is.EqualTo(1));
            Assert.That(DailyCalendar.BandFor(30, new[] { 30, 100 }), Is.EqualTo(2));
            Assert.That(DailyCalendar.BandFor(100, new[] { 30, 100 }), Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => DailyCalendar.BandFor(10, new[] { 41 }));
            Assert.Throws<ArgumentException>(() => DailyCalendar.BandFor(10, new[] { 41, 201, 301 }));
            Assert.Throws<ArgumentException>(() => DailyCalendar.BandFor(10, new[] { 201, 41 }));
            Assert.Throws<ArgumentNullException>(() => DailyCalendar.BandFor(10, null!));
        }

        // ---- Desbloqueo (06 §2.3; live_unlock_level_daily = 10; biz_feature_daily_challenge) ----

        [Test]
        public void IsUnlocked_AfterBeatingLevel10_OnlyWithTheSwitchOn()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            Assert.That(DailyCalendar.IsUnlocked(0, config), Is.False);
            Assert.That(DailyCalendar.IsUnlocked(9, config), Is.False);
            Assert.That(DailyCalendar.IsUnlocked(10, config), Is.True);
            Assert.That(DailyCalendar.IsUnlocked(500, config), Is.True);

            config.BizFeatureDailyChallenge = false;
            Assert.That(DailyCalendar.IsUnlocked(10, config), Is.False, "kill switch apagado");
            Assert.That(DailyCalendar.IsUnlocked(500, config), Is.False, "kill switch apagado");

            config.BizFeatureDailyChallenge = true;
            config.LiveUnlockLevelDaily = 12;
            Assert.That(DailyCalendar.IsUnlocked(11, config), Is.False);
            Assert.That(DailyCalendar.IsUnlocked(12, config), Is.True);
        }

        // ---- Resolución sin sustituciones ----

        [Test]
        public void Resolve_WithoutOverrides_GivesBandAndDay()
        {
            DailySlot slot = DailyCalendar.Resolve(new DateTime(2026, 10, 19), 50, DailyChallengeConfig.Defaults, DailyTestPool.Full);

            Assert.That(slot, Is.EqualTo(new DailySlot(2, 291, null)));
            Assert.That(slot.IsOverride, Is.False);
            Assert.That(slot.Board(DailyTestPool.Full), Is.SameAs(DailyTestPool.Full.Find(2, 291)));
            Assert.That(slot.Board(DailyTestPool.Full)!.Id, Is.EqualTo("d2_n0291"));
        }

        [Test]
        public void Resolve_AppliesTheOffset()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            config.LiveDailyOffset = -292;

            DailySlot slot = DailyCalendar.Resolve(new DateTime(2026, 10, 19), 300, config, DailyTestPool.Full);

            Assert.That(slot, Is.EqualTo(new DailySlot(3, 729, null)));
        }

        [Test]
        public void Today_UsesTheLocalDateOfTheClock()
        {
            // 2026-10-20 03:00 UTC es todavía el 19 en Colombia (−5 h): manda la fecha local.
            var clock = new ManualClock(new DateTime(2026, 10, 20, 3, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;

            Assert.That(DailyCalendar.Today(clock, 10, config, DailyTestPool.Full), Is.EqualTo(new DailySlot(1, 291, null)));

            clock.LocalOffset = TimeSpan.Zero;
            Assert.That(DailyCalendar.Today(clock, 10, config, DailyTestPool.Full), Is.EqualTo(new DailySlot(1, 292, null)));
        }

        // ---- Sustituciones (R-Q6: {fecha: {banda: id}}) ----

        [Test]
        public void Override_Present_IsServed()
        {
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"1\":\"d1_n0100\",\"2\":\"d2_n0033\"}}");
            var christmas = new DateTime(2026, 12, 25);

            DailySlot b1 = DailyCalendar.Resolve(christmas, 10, config, DailyTestPool.Full);
            DailySlot b2 = DailyCalendar.Resolve(christmas, 41, config, DailyTestPool.Full);

            Assert.That(b1, Is.EqualTo(new DailySlot(1, 358, "d1_n0100")));
            Assert.That(b1.Board(DailyTestPool.Full)!.Id, Is.EqualTo("d1_n0100"));
            Assert.That(b2, Is.EqualTo(new DailySlot(2, 358, "d2_n0033")));
            Assert.That(b2.Board(DailyTestPool.Full)!.Id, Is.EqualTo("d2_n0033"));
        }

        [Test]
        public void Override_ViaTheClock_UsesTheLocalDateKey()
        {
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"1\":\"d1_n0100\"}}");
            var clock = new ManualClock(new DateTime(2026, 12, 26, 2, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));

            Assert.That(DailyCalendar.Today(clock, 10, config, DailyTestPool.Full).OverrideId, Is.EqualTo("d1_n0100"));
        }

        [Test]
        public void Override_OtherDate_UsesTheNormalCalculation()
        {
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"1\":\"d1_n0100\"}}");

            DailySlot slot = DailyCalendar.Resolve(new DateTime(2026, 12, 24), 10, config, DailyTestPool.Full);

            Assert.That(slot, Is.EqualTo(new DailySlot(1, 357, null)));
        }

        [Test]
        public void Override_UnknownId_UsesTheNormalCalculation()
        {
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"1\":\"d1_n9999\",\"2\":\"no_existe\"}}");
            var christmas = new DateTime(2026, 12, 25);

            Assert.That(DailyCalendar.Resolve(christmas, 10, config, DailyTestPool.Full), Is.EqualTo(new DailySlot(1, 358, null)));
            Assert.That(DailyCalendar.Resolve(christmas, 41, config, DailyTestPool.Full), Is.EqualTo(new DailySlot(2, 358, null)));
        }

        [Test]
        public void Override_MissingBand_UsesTheNormalCalculation()
        {
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"1\":\"d1_n0100\"}}");

            DailySlot b3 = DailyCalendar.Resolve(new DateTime(2026, 12, 25), 201, config, DailyTestPool.Full);

            Assert.That(b3, Is.EqualTo(new DailySlot(3, 358, null)));
            Assert.That(b3.Board(DailyTestPool.Full)!.Id, Is.EqualTo("d3_n0358"));
        }

        [Test]
        public void Override_IdOfAnotherBand_IsAccepted()
        {
            // [P] de la nota §6: el id de otra banda se admite; la banda del slot sigue siendo la del jugador.
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"3\":\"d1_n0005\"}}");

            DailySlot slot = DailyCalendar.Resolve(new DateTime(2026, 12, 25), 250, config, DailyTestPool.Full);

            Assert.That(slot, Is.EqualTo(new DailySlot(3, 358, "d1_n0005")));
            LevelDefinition board = slot.Board(DailyTestPool.Full)!;
            Assert.That(board.Id, Is.EqualTo("d1_n0005"));
            Assert.That(DailyTestPool.Full.BandOf(board), Is.EqualTo(1));
        }

        [Test]
        public void Override_DoesNotUseTheOffset()
        {
            DailyChallengeConfig config = WithOverrides("{\"2026-12-25\":{\"1\":\"d1_n0100\"}}");
            config.LiveDailyOffset = 7;

            DailySlot slot = DailyCalendar.Resolve(new DateTime(2026, 12, 25), 10, config, DailyTestPool.Full);

            Assert.That(slot.OverrideId, Is.EqualTo("d1_n0100"), "la fecha manda, no el índice desplazado");
            Assert.That(slot.Day, Is.EqualTo(365));
        }

        [Test]
        public void Resolve_RejectsNulls()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            var date = new DateTime(2026, 1, 1);
            Assert.Throws<ArgumentNullException>(() => DailyCalendar.Resolve(date, 10, null!, DailyTestPool.Full));
            Assert.Throws<ArgumentNullException>(() => DailyCalendar.Resolve(date, 10, config, null!));
            Assert.Throws<ArgumentNullException>(() => DailyCalendar.Today(null!, 10, config, DailyTestPool.Full));
            Assert.Throws<ArgumentNullException>(() => DailyCalendar.IsUnlocked(10, null!));
        }

        private static DailyChallengeConfig WithOverrides(string json)
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            config.DailyOverrides = DailyOverrides.Parse(json);
            return config;
        }
    }
}
