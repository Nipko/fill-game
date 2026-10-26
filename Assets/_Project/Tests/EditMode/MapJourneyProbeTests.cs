#nullable enable
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Las sondas del Mapa con viaje (D-053; docs/dev/prototipo-v0.md): <c>AF_STATE map_play_ready</c> al habilitarse «Jugar» y
    /// <c>map_journey</c> con su <c>source</c> (el del fin de contenido sigue saliendo con <c>content_end</c>), y qué entradas del
    /// Mapa abren con viaje.
    /// </summary>
    public sealed class MapJourneyProbeTests
    {
        [Test]
        public void JourneySources_AreL10_Victory_AndContentEnd()
        {
            Assert.That(RiverMapScreen.JourneySource(MapOpenSource.L10), Is.True);
            Assert.That(RiverMapScreen.JourneySource(MapOpenSource.Victory), Is.True);
            Assert.That(RiverMapScreen.JourneySource(MapOpenSource.ContentEnd), Is.True);
            Assert.That(RiverMapScreen.JourneySource(MapOpenSource.Lobby), Is.False, "la pestaña, como siempre");
            Assert.That(RiverMapScreen.JourneySource(MapOpenSource.Pause), Is.False);
            Assert.That(RiverMapScreen.JourneySource(MapOpenSource.WorldChange), Is.False, "el viaje del banner es otra pieza");
            Assert.That(RiverMapScreen.JourneyLeadMs, Is.LessThanOrEqualTo(300), "se mueve en ≤ 300 ms");
            Assert.That(RiverMapScreen.JourneyTapSpeed, Is.GreaterThan(1f), "el toque acelera, no salta");
        }

        [Test]
        public void MapPlayReady_Line()
        {
            var probe = new DevProbe();
            string? line = probe.FormatMapPlayReady(MapOpenSource.Victory, 20, 266, 1766, 1, "time");
            if (!DevProbe.Enabled)
            {
                Assert.That(line, Is.Null);
                return;
            }

            Assert.That(line, Does.StartWith("AF_STATE {\"event\":\"map_play_ready\""));
            StringAssert.Contains("\"source\":\"victory\",\"to\":20,\"first_move_ms\":266,\"ready_ms\":1766,\"taps\":1,\"by\":\"time\"}", line);
        }

        [Test]
        public void MapJourney_Line_CarriesItsSource()
        {
            var probe = new DevProbe();
            string? victory = probe.FormatMapJourney(MapOpenSource.Victory, "start", 10, 20, 1500, 0f, string.Empty);
            string? contentEnd = probe.FormatMapJourney("end", 690, 700, 1500, 1500f, "time");
            if (!DevProbe.Enabled)
            {
                Assert.That(victory, Is.Null);
                Assert.That(contentEnd, Is.Null);
                return;
            }

            StringAssert.Contains("\"source\":\"victory\",\"phase\":\"start\",\"from\":10,\"to\":20", victory);
            StringAssert.Contains("\"source\":\"content_end\",\"phase\":\"end\",\"from\":690,\"to\":700", contentEnd);
        }
    }
}
