#nullable enable
using System;
using AF.Ads.Max;
using AF.Platform;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// Qué proveedor de anuncios usa cada build (D-042; <see cref="MaxAdProvider.Choose"/>): un release sin la configuración de MAX no
    /// enseña nunca el anuncio simulado, sino ninguno (<see cref="UnavailableAdService"/>); el desarrollo, el editor y los tests sin
    /// configuración siguen con el simulado (null).
    /// </summary>
    public sealed class AdProviderChoiceTests
    {
        private static readonly DateTime At = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ReleaseWithoutConfig_IsUnavailable()
        {
            Assert.That(MaxAdProvider.Choose(false, "not_configured", releaseBuild: true), Is.InstanceOf<UnavailableAdService>());
            Assert.That(MaxAdProvider.Choose(false, "config_unreadable", releaseBuild: true), Is.InstanceOf<UnavailableAdService>());
        }

        [Test]
        public void DevelopmentWithoutConfig_IsTheSimulatedOne()
        {
            Assert.That(MaxAdProvider.Choose(false, "not_configured", releaseBuild: false), Is.Null);
            Assert.That(MaxAdProvider.Choose(false, MaxAdProvider.ReasonEditor, releaseBuild: true), Is.Null, "el editor, siempre el simulado");
        }

        [Test]
        public void WithConfig_IsMax()
        {
            var max = new MaxAdService(new FakeMaxSdkPort(), new ManualClock(At), () => null, () => 1);
            Assert.That(MaxAdProvider.Choose(true, "max", releaseBuild: true, () => max), Is.SameAs(max));
            Assert.That(MaxAdProvider.Choose(true, "max", releaseBuild: false, () => max), Is.SameAs(max));
        }

        [Test]
        public void Unavailable_IsNeverReady_AndShowsNothing()
        {
            var ads = new UnavailableAdService();
            for (int i = 0; i < AdPlacements.Count; i++)
            {
                var placement = (AdPlacement)i;
                Assert.That(ads.IsReady(placement), Is.False, placement.ToId());
                AdOutcome? outcome = null;
                ads.Show(placement, o => outcome = o);
                Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill), placement.ToId());
            }

            Assert.That(ads.IsShowing, Is.False);
            Assert.Throws<ArgumentNullException>(() => ads.Show(AdPlacement.Hint, null!));
        }

        [Test]
        public void ConsentOverUnavailable_WithADecision_StillHasNoAd()
        {
            var ads = new ConsentAdService(new UnavailableAdService());
            ads.Apply(ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.Co1581, 1, At, true, true, AgeBand.Adult), ConsentConfig.Defaults));

            Assert.That(ads.Initialized, Is.True);
            Assert.That(ads.IsReady(AdPlacement.ExtraCanoe), Is.False);
            AdOutcome? outcome = null;
            ads.Show(AdPlacement.InterstitialPostLevel, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
        }
    }
}
