#nullable enable
using AF.Analytics.Unity;
using AF.Events;
using NUnit.Framework;

namespace AF.Analytics.Tests
{
    /// <summary>
    /// <c>curve</c> y <c>pressure</c> en <c>level_start</c> y <c>level_end</c> (08 §8.2; D-068 [P], de alta con la fase F4), para el
    /// A/B de <c>lvl_curve2_enabled</c> (09 §6): el catálogo los admite; salen en la campaña (la curva 1 o 2 del tablero servido y,
    /// si el paquete la trae, la presión 0-9) y no salen sin curva (el Reto) ni sin presión.
    /// </summary>
    public sealed class CurveAnalyticsTests
    {
        [Test]
        public void TheCatalog_AllowsCurveAndPressure_InLevelStartAndLevelEnd()
        {
            Assert.That(AnalyticsCatalog.Find("level_start")!.Allows("curve"), Is.True);
            Assert.That(AnalyticsCatalog.Find("level_start")!.Allows("pressure"), Is.True);
            Assert.That(AnalyticsCatalog.Find("level_end")!.Allows("curve"), Is.True);
            Assert.That(AnalyticsCatalog.Find("level_end")!.Allows("pressure"), Is.True);
            Assert.That(AnalyticsCatalog.Find("af_level_win")!.Allows("curve"), Is.False, "solo level_start y level_end (08 §8.2)");
        }

        [Test]
        public void Curve2WithPressure_GoesIntoLevelStartAndLevelEnd()
        {
            var bus = new EventBus();
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService(bus);
            analytics.AddSink(sink);
            bus.Publish(new LevelStarted(303, "m07_v1_n0303_c2", "base", "comfort", 1, 7, "normal", false, 2, 4));
            bus.Publish(new LevelWon(303, "m07_v1_n0303_c2", 18, 15, 0, "{}", 41000, 20, false, false, 2, 4));
            bus.Publish(new LevelFailed(304, "m07_v1_n0304", true, true, false, false, false, 22, 90000, 1, -1));

            Assert.That(analytics.Violations, Is.Empty);
            Assert.That(sink.Last("level_start")!.ToJson(), Is.EqualTo(
                "{\"event\":\"level_start\",\"level_name\":\"m07_v1_n0303_c2\",\"variant\":\"base\",\"band\":\"comfort\",\"curve\":2,\"pressure\":4}"));
            Assert.That(sink.Events[2].ToJson(), Is.EqualTo(
                "{\"event\":\"level_end\",\"success\":true,\"level_name\":\"m07_v1_n0303_c2\",\"curve\":2,\"pressure\":4}"));
            Assert.That(sink.Last("level_end")!.ToJson(), Is.EqualTo(
                "{\"event\":\"level_end\",\"success\":false,\"level_name\":\"m07_v1_n0304\",\"curve\":1}"), "sin presión en el paquete: no sale");
            analytics.Dispose();
        }

        [Test]
        public void WithoutCurve_NothingIsAdded()
        {
            var bus = new EventBus();
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService(bus);
            analytics.AddSink(sink);
            bus.Publish(new LevelStarted(12, "m01_v1_n0012", "base", "comfort", 1, 1, "normal", false));
            Assert.That(sink.Events[0].Find("curve"), Is.Null);
            Assert.That(sink.Events[0].Find("pressure"), Is.Null);
            analytics.Dispose();
        }
    }
}
