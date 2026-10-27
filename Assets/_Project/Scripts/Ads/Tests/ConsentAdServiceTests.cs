#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// Los anuncios detrás del consentimiento y la edad (bloque B12; 08 §2.3: «los anuncios arrancan solo tras consentir»; 08 §7: TFCD con
    /// &lt;13, <c>npa</c> sin consentimiento o con 13-17, contenido G en todas las bandas, D-029).
    /// </summary>
    public sealed class ConsentAdServiceTests
    {
        private static readonly DateTime At = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        private static ConsentFlags FlagsFor(AgeBand band, bool ads)
        {
            return ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.BrLgpdEca, 1, At, true, ads, band), ConsentConfig.Defaults);
        }

        [Test]
        public void BeforeTheDecision_NothingIsLoaded_AndNothingShows()
        {
            var presenter = new ImmediateAdPresenter();
            var ads = new ConsentAdService(new FakeAdService(presenter));

            Assert.That(ads.Initialized, Is.False);
            Assert.That(ads.IsReady(AdPlacement.DoubleHarvest), Is.False);
            AdOutcome? outcome = null;
            ads.Show(AdPlacement.DoubleHarvest, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
            Assert.That(presenter.Presented, Is.EqualTo(0));
            Assert.That(ads.NonPersonalized, Is.True, "sin consentimiento, npa");
            Assert.That(ads.MaxAdContentRating, Is.EqualTo("G"));
        }

        [Test]
        public void AdultWhoAccepted_Initializes_Personalized()
        {
            var presenter = new ImmediateAdPresenter();
            var ads = new ConsentAdService(new FakeAdService(presenter));
            ads.Apply(FlagsFor(AgeBand.Adult, ads: true));

            Assert.That(ads.Initialized, Is.True);
            Assert.That(ads.NonPersonalized, Is.False);
            Assert.That(ads.ChildDirected, Is.False);
            Assert.That(ads.MaxAdContentRating, Is.EqualTo("G"), "D-029: G en todas las bandas");
            Assert.That(ads.IsReady(AdPlacement.DoubleHarvest), Is.True);
            AdOutcome? outcome = null;
            ads.Show(AdPlacement.DoubleHarvest, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.Rewarded));
            Assert.That(presenter.Presented, Is.EqualTo(1));
        }

        [Test]
        public void AdultWhoRejected_IsNonPersonalized()
        {
            var ads = new ConsentAdService(new FakeAdService(new ImmediateAdPresenter()));
            ads.Apply(FlagsFor(AgeBand.Adult, ads: false));

            Assert.That(ads.Initialized, Is.True);
            Assert.That(ads.NonPersonalized, Is.True);
        }

        [Test]
        public void Teen_IsNonPersonalized_EvenIfAccepted()
        {
            var ads = new ConsentAdService(new FakeAdService(new ImmediateAdPresenter()));
            ads.Apply(FlagsFor(AgeBand.Teen, ads: true));

            Assert.That(ads.NonPersonalized, Is.True, "biz_under18_no_personalization");
            Assert.That(ads.ChildDirected, Is.False);
        }

        [Test]
        public void Under13_IsChildDirected_AndNeverInitializes()
        {
            var presenter = new ImmediateAdPresenter();
            var ads = new ConsentAdService(new FakeAdService(presenter));
            ads.Apply(FlagsFor(AgeBand.Under13, ads: true));

            Assert.That(ads.ChildDirected, Is.True, "TFCD");
            Assert.That(ads.NonPersonalized, Is.True);
            Assert.That(ads.Initialized, Is.False);
            AdOutcome? outcome = null;
            ads.Show(AdPlacement.UndoRefill, o => outcome = o);
            Assert.That(outcome, Is.EqualTo(AdOutcome.NoFill));
            Assert.That(presenter.Presented, Is.EqualTo(0));
        }

        [Test]
        public void Ump_LeavesNpaToTheUmpMessage_AndCanReopenIt()
        {
            var ads = new ConsentAdService(new FakeAdService(new ImmediateAdPresenter()));
            Assert.That(ads.ShowPrivacyOptions(), Is.False, "fuera de UMP no hay mensaje que reabrir");
            ads.Apply(ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.Ump, 1, At, true, false, AgeBand.Adult), ConsentConfig.Defaults));

            Assert.That(ads.Initialized, Is.True);
            Assert.That(ads.UmpDecides, Is.True);
            Assert.That(ads.NonPersonalized, Is.False, "las cadenas TCF de UMP deciden (08 §7 #2)");
            Assert.That(ads.ShowPrivacyOptions(), Is.True, "revocación en Ajustes (05 §7)");
            Assert.That(ads.PrivacyOptionsRequests, Is.EqualTo(1));
        }

        [Test]
        public void Ump_Teen_IsStillNpa()
        {
            var ads = new ConsentAdService(new FakeAdService(new ImmediateAdPresenter()));
            ads.Apply(ConsentPolicy.Flags(new ConsentRecord(LegalProfiles.Ump, 1, At, true, false, AgeBand.Teen), ConsentConfig.Defaults));

            Assert.That(ads.UmpDecides, Is.False);
            Assert.That(ads.NonPersonalized, Is.True);
        }

        [Test]
        public void ChangingFromSettings_UpdatesTheFlags()
        {
            var ads = new ConsentAdService(new FakeAdService(new ImmediateAdPresenter()));
            ads.Apply(FlagsFor(AgeBand.Adult, ads: true));
            ads.Apply(FlagsFor(AgeBand.Adult, ads: false));

            Assert.That(ads.NonPersonalized, Is.True);
            Assert.That(ads.Applies, Is.EqualTo(2));
        }
    }
}
