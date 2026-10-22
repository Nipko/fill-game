#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>profile.consent</c> (08 §4.3; bloque B12, parche <c>B12-profile.diff</c>): ida y vuelta con los nombres de 08 §4.3, la clave
    /// ausente mientras no hay decisión, lectura tolerante (un bloque roto se vuelve a preguntar, nunca corrompe el perfil) y
    /// <see cref="ProfileConsentStore"/>, que guarda en el acto.
    /// </summary>
    public sealed class ProfileConsentTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 21, 13, 10, 20, DateTimeKind.Utc);

        private static Profile NewProfile() => Profile.CreateNew(new ManualClock(T0, TimeSpan.FromHours(-5)), 5);

        [Test]
        public void WithoutADecision_TheKeyIsAbsent()
        {
            Profile p = NewProfile();
            Assert.That(p.Consent, Is.Null);
            Assert.That(ProfileJson.Serialize(p).Contains("\"consent\""), Is.False);
        }

        [Test]
        public void RoundTrip_WithTheNamesOf08_4_3()
        {
            Profile p = NewProfile();
            p.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, false, AgeBand.Adult);

            string json = ProfileJson.Serialize(p);
            StringAssert.Contains(
                "\"consent\":{\"profile\":\"co_1581\",\"text_version\":1,\"at\":\"2026-10-21T13:10:20Z\",\"analytics\":true,"
                + "\"ads_personalized\":false,\"age_band\":\"18+\"}", json.Replace(" ", string.Empty).Replace("\n", string.Empty));
            ConsentRecord back = ProfileJson.Deserialize(json).Consent!;
            Assert.That(back.Profile, Is.EqualTo("co_1581"));
            Assert.That(back.TextVersion, Is.EqualTo(1));
            Assert.That(back.At, Is.EqualTo(T0));
            Assert.That(back.Analytics, Is.True);
            Assert.That(back.AdsPersonalized, Is.False);
            Assert.That(back.AgeBand, Is.EqualTo(AgeBand.Adult));
            Assert.That(back.AgeBandCorrected, Is.False);
        }

        [Test]
        public void TheCorrection_IsWrittenOnlyWhenTrue()
        {
            Profile p = NewProfile();
            p.Consent = new ConsentRecord(LegalProfiles.BrLgpdEca, 2, T0, false, false, AgeBand.Teen, ageBandCorrected: true);

            ConsentRecord back = ProfileJson.Deserialize(ProfileJson.Serialize(p)).Consent!;
            Assert.That(back.AgeBandCorrected, Is.True);
            Assert.That(back.AgeBand, Is.EqualTo(AgeBand.Teen));
            Assert.That(back.TextVersion, Is.EqualTo(2));
        }

        [TestCase("{\"profile_schema\": 2, \"consent\": 7}")]
        [TestCase("{\"profile_schema\": 2, \"consent\": {\"analytics\": true}}")]
        [TestCase("{\"profile_schema\": 2, \"consent\": {\"profile\": 3, \"age_band\": \"18+\"}}")]
        [TestCase("{\"profile_schema\": 2, \"consent\": null}")]
        public void ABrokenBlock_IsAskedAgain_NeverCorruption(string json)
        {
            Profile p = ProfileJson.Deserialize(json);
            Assert.That(p.Consent, Is.Null);
        }

        [Test]
        public void AnUnknownBandOrVersion_ForcesTheQuestion()
        {
            Profile p = ProfileJson.Deserialize(
                "{\"profile_schema\": 2, \"consent\": {\"profile\": \"co_1581\", \"text_version\": \"x\", \"age_band\": \"12\", \"at\": 5}}");

            Assert.That(p.Consent, Is.Not.Null);
            Assert.That(p.Consent!.AgeBand, Is.EqualTo(AgeBand.Unknown));
            Assert.That(p.Consent.TextVersion, Is.EqualTo(0));
            Assert.That(ConsentPolicy.NeedsPrompt(p.Consent, ConsentConfig.Defaults), Is.True);
        }

        [Test]
        public void TheStore_SavesAtOnce()
        {
            Profile p = NewProfile();
            var disk = new MemoryProfileStore();
            var store = new ProfileConsentStore(p, disk);
            Assert.That(store.Load(), Is.Null);

            store.Save(new ConsentRecord(LegalProfiles.Default, 1, T0, false, false, AgeBand.Under13));

            Assert.That(disk.SaveCount, Is.EqualTo(1));
            Assert.That(store.Load()!.AgeBand, Is.EqualTo(AgeBand.Under13));
            Assert.That(disk.Load().Profile!.Consent!.AgeBand, Is.EqualTo(AgeBand.Under13), "en disco");
        }
    }
}
