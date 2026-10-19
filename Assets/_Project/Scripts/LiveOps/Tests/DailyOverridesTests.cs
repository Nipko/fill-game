#nullable enable
using System;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>Lectura de <c>daily_overrides</c> con la forma de R-Q6 <c>{fecha: {banda: id}}</c> (nota §6).</summary>
    public sealed class DailyOverridesTests
    {
        [Test]
        public void Empty_IsTheDefault()
        {
            Assert.That(DailyOverrides.Empty.DateCount, Is.EqualTo(0));
            Assert.That(DailyOverrides.Empty.Format(), Is.EqualTo("{}"));
            Assert.That(DailyOverrides.Parse("{}"), Is.SameAs(DailyOverrides.Empty));
            Assert.That(DailyOverrides.Parse(" { } ").TryGet("2026-12-25", 1, out _), Is.False);
        }

        [Test]
        public void Parse_ReadsDatesAndBands()
        {
            DailyOverrides o = DailyOverrides.Parse(
                "{\"2026-12-25\":{\"2\":\"d2_n0033\",\"1\":\"d1_n0100\"},\"2026-01-06\":{\"3\":\"d3_n0001\"}}");

            Assert.That(o.DateCount, Is.EqualTo(2));
            Assert.That(o.TryGet("2026-12-25", 1, out string id1), Is.True);
            Assert.That(id1, Is.EqualTo("d1_n0100"));
            Assert.That(o.TryGet("2026-12-25", 2, out string id2), Is.True);
            Assert.That(id2, Is.EqualTo("d2_n0033"));
            Assert.That(o.TryGet("2026-12-25", 3, out _), Is.False, "banda que falta");
            Assert.That(o.TryGet("2026-01-06", 3, out string id3), Is.True);
            Assert.That(id3, Is.EqualTo("d3_n0001"));
            Assert.That(o.TryGet("2026-01-07", 3, out _), Is.False, "fecha que falta");
            Assert.That(o.TryGet("2026-12-25", 0, out _), Is.False);
            Assert.That(o.TryGet("2026-12-25", 4, out _), Is.False);
            Assert.That(o.Format(),
                Is.EqualTo("{\"2026-01-06\":{\"3\":\"d3_n0001\"},\"2026-12-25\":{\"1\":\"d1_n0100\",\"2\":\"d2_n0033\"}}"),
                "orden ordinal de fechas y de bandas, sea cual sea el del JSON");
        }

        [TestCase("[]")]
        [TestCase("\"x\"")]
        [TestCase("{\"2026-12-25\":\"d1_n0100\"}")]
        [TestCase("{\"2026-13-01\":{\"1\":\"d1_n0100\"}}")]
        [TestCase("{\"2026-2-1\":{\"1\":\"d1_n0100\"}}")]
        [TestCase("{\"25/12/2026\":{\"1\":\"d1_n0100\"}}")]
        [TestCase("{\"2026-12-25\":{\"0\":\"d1_n0100\"}}")]
        [TestCase("{\"2026-12-25\":{\"4\":\"d1_n0100\"}}")]
        [TestCase("{\"2026-12-25\":{\"b1\":\"d1_n0100\"}}")]
        [TestCase("{\"2026-12-25\":{\"1\":100}}")]
        [TestCase("{\"2026-12-25\":{\"1\":\"\"}}")]
        [TestCase("{\"2026-12-25\":{\"1\":null}}")]
        [TestCase("{\"2026-12-25\":{\"1\":\"a\",\"1\":\"b\"}}")]
        [TestCase("{\"2026-12-25\":{\"1\":\"a\"},\"2026-12-25\":{\"2\":\"b\"}}")]
        [TestCase("{")]
        public void Parse_RejectsWhatIsNotRQ6(string json)
        {
            Assert.Throws<FormatException>(() => DailyOverrides.Parse(json));
        }

        [Test]
        public void Parse_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => DailyOverrides.Parse(null!));
        }
    }
}
