#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Edad y consentimiento sin motor (bloque B12; 08 §2.3, §4.3, §7, §8.2 y §9; 05 §7; Q-T3 (a) y Q-T4 (a) de D-033): las bandas,
    /// el perfil legal por país, las cifras de Remote Config, los flags y el recorrido del arranque y de Ajustes.
    /// </summary>
    public sealed class ConsentTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);

        private static ManualClock Clock() => new ManualClock(T0, TimeSpan.FromHours(-5));

        private static ConsentRecord Record(AgeBand band, bool analytics = true, bool ads = true, int version = 1, string profile = LegalProfiles.Co1581)
        {
            return new ConsentRecord(profile, version, T0, analytics, ads, band);
        }

        // ---- Bandas y perfiles ----

        [Test]
        public void AgeBands_RoundTrip_WithTheIdsOf08()
        {
            Assert.That(AgeBands.Id(AgeBand.Under13), Is.EqualTo("<13"));
            Assert.That(AgeBands.Id(AgeBand.Teen), Is.EqualTo("13-17"));
            Assert.That(AgeBands.Id(AgeBand.Adult), Is.EqualTo("18+"));
            Assert.That(AgeBands.Id(AgeBand.Unknown), Is.Null);
            foreach (AgeBand band in new[] { AgeBand.Under13, AgeBand.Teen, AgeBand.Adult })
            {
                Assert.That(AgeBands.Parse(AgeBands.Id(band)), Is.EqualTo(band));
            }

            Assert.That(AgeBands.Parse("12"), Is.EqualTo(AgeBand.Unknown));
            Assert.That(AgeBands.Parse(null), Is.EqualTo(AgeBand.Unknown));
        }

        [TestCase("BR", LegalProfiles.BrLgpdEca)]
        [TestCase("br", LegalProfiles.BrLgpdEca)]
        [TestCase("CO", LegalProfiles.Co1581)]
        [TestCase("PE", LegalProfiles.Pe29733)]
        [TestCase("MX", LegalProfiles.MxLfpdppp)]
        [TestCase("DE", LegalProfiles.Ump)]
        [TestCase("ES", LegalProfiles.Ump)]
        [TestCase("NO", LegalProfiles.Ump)]
        [TestCase("GB", LegalProfiles.Ump)]
        [TestCase("CH", LegalProfiles.Ump)]
        [TestCase("US", LegalProfiles.Default)]
        [TestCase("AR", LegalProfiles.Default)]
        [TestCase("", LegalProfiles.Default)]
        [TestCase(null, LegalProfiles.Default)]
        public void ProfileFor_FollowsTheMapOf08_9(string? country, string expected)
        {
            Assert.That(ConsentConfig.Defaults.ProfileFor(country), Is.EqualTo(expected));
        }

        [Test]
        public void Country_SimFirst_ThenRegion_ThenNone()
        {
            Assert.That(ConsentCountry.Resolve(new ManualSimCountry("br"), new ManualDeviceLocale("es-CO"), out CountrySource s1), Is.EqualTo("BR"));
            Assert.That(s1, Is.EqualTo(CountrySource.Sim));
            Assert.That(ConsentCountry.Resolve(ManualSimCountry.None, new ManualDeviceLocale("es-CO"), out CountrySource s2), Is.EqualTo("CO"));
            Assert.That(s2, Is.EqualTo(CountrySource.Region));
            Assert.That(ConsentCountry.Resolve(new ManualSimCountry("  "), new ManualDeviceLocale("es-419"), out CountrySource s3), Is.Null, "419 no es un país");
            Assert.That(s3, Is.EqualTo(CountrySource.None));
            Assert.That(ConsentCountry.Resolve(null, null, out CountrySource s4), Is.Null);
            Assert.That(s4, Is.EqualTo(CountrySource.None));
        }

        [Test]
        public void Country_SimFirst_ThenPlayAccount_ThenRegion_ThenNone()
        {
            // D-057 (P4 (a)): la cuenta de Google Play va detrás de la SIM y delante de la región del teléfono.
            var de = new ManualPlayAccountCountry("de");
            Assert.That(ConsentCountry.Resolve(new ManualSimCountry("br"), de, new ManualDeviceLocale("en-US"), out CountrySource s1), Is.EqualTo("BR"));
            Assert.That(s1, Is.EqualTo(CountrySource.Sim));
            Assert.That(ConsentCountry.Resolve(ManualSimCountry.None, de, new ManualDeviceLocale("en-US"), out CountrySource s2), Is.EqualTo("DE"),
                "una tableta sin SIM en Alemania con el teléfono en en-US ya no queda como US");
            Assert.That(s2, Is.EqualTo(CountrySource.PlayAccount));
            Assert.That(ConsentCountry.SourceKey(s2), Is.EqualTo("play"));
            Assert.That(ConsentCountry.Resolve(ManualSimCountry.None, ManualPlayAccountCountry.None, new ManualDeviceLocale("es-CO"), out CountrySource s3), Is.EqualTo("CO"));
            Assert.That(s3, Is.EqualTo(CountrySource.Region));
            Assert.That(ConsentCountry.Resolve(ManualSimCountry.None, new ManualPlayAccountCountry("419"), new ManualDeviceLocale("es-419"), out CountrySource s4), Is.Null);
            Assert.That(s4, Is.EqualTo(CountrySource.None));
            Assert.That(ConsentConfig.Defaults.ProfileFor(ConsentCountry.Resolve(ManualSimCountry.None, de, new ManualDeviceLocale("en-US"), out _)),
                Is.EqualTo(LegalProfiles.Ump));
        }

        [Test]
        public void ThePlayAccountStore_KeepsTwoLettersInUppercase()
        {
            var store = new MemoryPlayAccountCountry();
            Assert.That(store.CountryIso, Is.Empty);
            store.Save("jp");
            Assert.That(store.CountryIso, Is.EqualTo("JP"));
            store.Save("JP");
            store.Save("419");
            store.Save("");
            Assert.That(store.CountryIso, Is.EqualTo("JP"));
            Assert.That(store.Saves, Is.EqualTo(1));
        }

        [TestCase("pt-BR", "BR")]
        [TestCase("pt_BR", "BR")]
        [TestCase("zh-Hant-TW", "TW")]
        [TestCase("es-419", null)]
        [TestCase("es", null)]
        [TestCase("en-x-ab", null)]
        [TestCase("", null)]
        public void RegionOf_ReadsTheTwoLetterRegion(string tag, string? expected)
        {
            Assert.That(ConsentCountry.RegionOf(tag), Is.EqualTo(expected));
        }

        // ---- Remote Config ----

        [Test]
        public void TheCompiledDefaults_AreTheDefaultsOfConsentConfig()
        {
            var errors = new List<string>();
            ConsentConfig c = ConsentConfigBinder.Bind(new RemoteConfigService().Freeze(), errors);

            Assert.That(errors, Is.Empty);
            ConsentConfig d = ConsentConfig.Defaults;
            Assert.That(c.ConsentTextVersion, Is.EqualTo(d.ConsentTextVersion).And.EqualTo(1));
            Assert.That(c.ConsentUnder13Policy, Is.EqualTo(d.ConsentUnder13Policy).And.EqualTo("block"));
            Assert.That(c.ConsentAgeSignalSource, Is.EqualTo(d.ConsentAgeSignalSource).And.EqualTo("self_declared+play_age_signals"));
            Assert.That(c.BizUnder18NoPersonalization, Is.EqualTo(d.BizUnder18NoPersonalization).And.True);
            Assert.That(Sorted(c.BizLegalProfileByCountry), Is.EqualTo(Sorted(d.BizLegalProfileByCountry)));
            Assert.That(SortedAges(c.ConsentDigitalAgeByCountry), Is.EqualTo(SortedAges(d.ConsentDigitalAgeByCountry)), "D-057");
        }

        [Test]
        public void EveryKey_IsInTheCompiledTableOf08_9()
        {
            var rc = new RemoteConfigService();
            foreach (string key in ConsentConfigBinder.Keys)
            {
                Assert.That(rc.Defaults.Find(key), Is.Not.Null, key + " está en RemoteConfigDefaults (08 §9)");
            }
        }

        [Test]
        public void RemoteValues_ReachTheConfig()
        {
            var errors = new List<string>();
            ConsentConfig c = ConsentConfigBinder.Bind(Snapshot(
                "consent_text_version", "3",
                "biz_under18_no_personalization", "false",
                "biz_legal_profile_by_country", "{\"AR\":\"co_1581\",\"*\":\"default\"}"), errors);

            Assert.That(errors, Is.Empty);
            Assert.That(c.ConsentTextVersion, Is.EqualTo(3));
            Assert.That(c.BizUnder18NoPersonalization, Is.False);
            Assert.That(c.ProfileFor("AR"), Is.EqualTo(LegalProfiles.Co1581));
            Assert.That(c.ProfileFor("BR"), Is.EqualTo(LegalProfiles.Default), "sin entrada propia, el resto");
        }

        [Test]
        public void CountryKeys_AreReadInUppercase()
        {
            var errors = new List<string>();
            ConsentConfig c = ConsentConfigBinder.Bind(Snapshot("biz_legal_profile_by_country", "{\"br\":\"br_lgpd_eca\",\"*\":\"default\"}"), errors);

            Assert.That(errors, Is.Empty);
            Assert.That(c.ProfileFor("BR"), Is.EqualTo(LegalProfiles.BrLgpdEca), "«br» de Remote Config vale para BR");
        }

        [Test]
        public void AnUnknownProfile_FallsBackToTheDefaults_AndIsReported()
        {
            var errors = new List<string>();
            ConsentConfig c = ConsentConfigBinder.Bind(Snapshot("biz_legal_profile_by_country", "{\"BR\":\"lgpd\",\"*\":\"default\"}"), errors);

            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(c.ProfileFor("BR"), Is.EqualTo(LegalProfiles.BrLgpdEca));
        }

        // ---- Flags ----

        [Test]
        public void NoRecord_IsUndecided_AndEverythingOff()
        {
            ConsentFlags f = ConsentPolicy.Flags(null, ConsentConfig.Defaults);
            Assert.That(f.Decided, Is.False);
            Assert.That(f.AnalyticsEnabled, Is.False);
            Assert.That(f.AdsPersonalized, Is.False);
            Assert.That(f.PurchasesAllowed, Is.False);
            Assert.That(ConsentPolicy.NeedsPrompt(null, ConsentConfig.Defaults), Is.True);
        }

        [Test]
        public void Under13_IsBlocked_ChildDirected_WithoutAnalyticsPurchasesPlayGamesOrPersonalRc()
        {
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Under13), ConsentConfig.Defaults);
            Assert.That(f.Blocked, Is.True);
            Assert.That(f.Decided, Is.False);
            Assert.That(f.ChildDirected, Is.True);
            Assert.That(f.AnalyticsEnabled, Is.False);
            Assert.That(f.AdsPersonalized, Is.False);
            Assert.That(f.PurchasesAllowed, Is.False);
            Assert.That(f.PlayGamesAllowed, Is.False);
            Assert.That(f.RcPersonalization, Is.False);
            Assert.That(f.MaxAdContentRating, Is.EqualTo("G"));
        }

        [Test]
        public void Under13_IsNeverAskedAgain_EvenWithNewLegalTexts()
        {
            var config = ConsentConfig.Defaults;
            config.ConsentTextVersion = 2;
            Assert.That(ConsentPolicy.NeedsPrompt(Record(AgeBand.Under13), config), Is.False);
            Assert.That(ConsentPolicy.IsBlocked(Record(AgeBand.Under13)), Is.True);
        }

        [Test]
        public void DataServicesAtBoot_OnlyWithoutTheUnder13BandSaved()
        {
            // B12 (08 §7 y §13.1 #11): con < 13 guardado, el arranque no enciende Crashlytics ni descarga Remote Config (ni toca
            // Firebase Analytics ni la nube) en ningún momento; sin banda (primer arranque), 13-17 y 18+, como siempre (08 §2.3).
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(null), Is.True, "primer arranque, sin edad todavía");
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(Record(AgeBand.Unknown)), Is.True);
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(Record(AgeBand.Teen)), Is.True);
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(Record(AgeBand.Adult)), Is.True);
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(Record(AgeBand.Adult, version: 0)), Is.True, "textos nuevos: se vuelve a preguntar");
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(Record(AgeBand.Under13)), Is.False);
            Assert.That(ConsentPolicy.StartsDataServicesAtBoot(Record(AgeBand.Under13, analytics: false, ads: false, version: 0)), Is.False,
                "el bloqueo es definitivo (Q-B12-3)");
        }

        [Test]
        public void Teen_IsNpa_WithoutPersonalRc_AndWithTheDoubleConfirmation()
        {
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Teen, analytics: true, ads: true), ConsentConfig.Defaults);
            Assert.That(f.Decided, Is.True);
            Assert.That(f.AnalyticsEnabled, Is.True);
            Assert.That(f.AdsPersonalized, Is.False, "biz_under18_no_personalization");
            Assert.That(f.RcPersonalization, Is.False);
            Assert.That(f.PurchasesAllowed, Is.True);
            Assert.That(f.PurchaseDoubleConfirm, Is.True, "Q-T4 (a)");
            Assert.That(f.ChildDirected, Is.False);
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void Adult_GetsWhatWasDecided(bool analytics, bool ads)
        {
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Adult, analytics, ads), ConsentConfig.Defaults);
            Assert.That(f.Decided, Is.True);
            Assert.That(f.AnalyticsEnabled, Is.EqualTo(analytics));
            Assert.That(f.AdsPersonalized, Is.EqualTo(ads));
            Assert.That(f.PurchaseDoubleConfirm, Is.False);
            Assert.That(f.MaxAdContentRating, Is.EqualTo("G"), "D-029: en todas las bandas");
        }

        [Test]
        public void NewLegalTexts_AskAgain_AndUntilThenNothingIsOn()
        {
            var config = ConsentConfig.Defaults;
            config.ConsentTextVersion = 2;
            ConsentRecord old = Record(AgeBand.Adult);

            Assert.That(ConsentPolicy.NeedsPrompt(old, config), Is.True);
            ConsentFlags f = ConsentPolicy.Flags(old, config);
            Assert.That(f.Decided, Is.False);
            Assert.That(f.AnalyticsEnabled, Is.False);
            Assert.That(f.PurchasesAllowed, Is.False);
        }

        [Test]
        public void TheAdsChoice_IsOfferedOnlyToAdults_OutsideUmp()
        {
            ConsentConfig c = ConsentConfig.Defaults;
            Assert.That(ConsentPolicy.OffersAdsChoice(LegalProfiles.BrLgpdEca, AgeBand.Adult, c), Is.True);
            Assert.That(ConsentPolicy.OffersAdsChoice(LegalProfiles.Default, AgeBand.Adult, c), Is.True);
            Assert.That(ConsentPolicy.OffersAdsChoice(LegalProfiles.Ump, AgeBand.Adult, c), Is.False);
            Assert.That(ConsentPolicy.OffersAdsChoice(LegalProfiles.BrLgpdEca, AgeBand.Teen, c), Is.False);
            Assert.That(ConsentPolicy.OffersAdsChoice(LegalProfiles.Default, AgeBand.Adult, c, countryKnown: false), Is.False, "Q-T3 (a): sin país, lo más estricto");
        }

        [Test]
        public void Ump_Adult_LeavesTheAdsToUmp()
        {
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Adult, ads: true, profile: LegalProfiles.Ump), ConsentConfig.Defaults);
            Assert.That(f.AdsByUmp, Is.True);
            Assert.That(f.AdsPersonalized, Is.False, "la pantalla propia no decide los anuncios en UMP");
            Assert.That(ConsentPolicy.Flags(Record(AgeBand.Teen, profile: LegalProfiles.Ump), ConsentConfig.Defaults).AdsByUmp, Is.False);
            Assert.That(ConsentPolicy.Flags(Record(AgeBand.Adult), ConsentConfig.Defaults).AdsByUmp, Is.False);
        }

        [Test]
        public void UnknownCountry_IsTheStrictestTreatment_WithoutTheAdsChoice()
        {
            var store = new MemoryConsentStore();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), ConsentConfig.Defaults.ProfileFor(null), countryKnown: false);
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Adult);
            Assert.That(flow.OffersAdsChoice, Is.False);
            Assert.That(flow.CountryKnown, Is.False);

            flow.Choose(ConsentAnswer.AcceptAll);

            Assert.That(store.Record!.Profile, Is.EqualTo(LegalProfiles.Default));
            Assert.That(store.Record!.AdsPersonalized, Is.False, "npa sin elección");
            Assert.That(store.Record!.Analytics, Is.True);
        }

        // ---- Edad de consentimiento digital (D-057, P2 (a); consent_digital_age_by_country) ----

        [TestCase("DE", 16)]
        [TestCase("de", 16)]
        [TestCase("NL", 16)]
        [TestCase("FR", 15)]
        [TestCase("ES", 14)]
        [TestCase("IT", 14)]
        [TestCase("BE", 13)]
        [TestCase("GB", 13)]
        [TestCase("CH", 16)]
        [TestCase("KR", 14)]
        [TestCase("IN", 18)]
        [TestCase("NO", 15)] // la investigación legal: ante la duda, el valor más alto
        [TestCase("SI", 16)]
        [TestCase("US", 13)]
        [TestCase("AR", 13)]
        [TestCase("CO", 18)]
        [TestCase("BR", 18)]
        [TestCase("MX", 18)]
        [TestCase("PE", 14)]
        [TestCase("CA", 14)]
        [TestCase("JP", 16)]
        [TestCase(null, 13)]
        public void DigitalAgeFor_FollowsTheMapOf08_9(string? country, int expected)
        {
            Assert.That(ConsentConfig.Defaults.DigitalAgeFor(country), Is.EqualTo(expected));
        }

        [Test]
        public void TheDefaultMap_HasEveryEeaState_AndTheRest()
        {
            ConsentConfig c = ConsentConfig.Defaults;
            foreach (string code in new[]
            {
                "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "ES", "FI", "FR", "GR", "HR", "HU", "IE", "IS", "IT", "LI", "LT", "LU",
                "LV", "MT", "NL", "NO", "PL", "PT", "RO", "SE", "SI", "SK",
            })
            {
                Assert.That(ConsentConfig.IsEea(code), Is.True, code);
                Assert.That(c.ConsentDigitalAgeByCountry.ContainsKey(code), Is.True, code + ": cada Estado del EEE con su edad del RGPD art. 8");
            }

            Assert.That(c.ConsentDigitalAgeByCountry[ConsentConfig.RestKey], Is.EqualTo(13));
            Assert.DoesNotThrow(c.Validate);
        }

        [Test]
        public void TheGroupKeys_EeaAndUk_AreUsedWhenTheCountryHasNoEntry()
        {
            ConsentConfig c = ConsentConfig.Defaults;
            c.ConsentDigitalAgeByCountry = new Dictionary<string, int>(StringComparer.Ordinal) { { "*", 13 }, { "EEE", 16 }, { "UK", 14 } };
            Assert.That(c.DigitalAgeFor("PT"), Is.EqualTo(16));
            Assert.That(c.DigitalAgeFor("GB"), Is.EqualTo(14));
            Assert.That(c.DigitalAgeFor("JP"), Is.EqualTo(13));
        }

        [Test]
        public void ADigitalAgeOutOf13To18_OrWithoutTheRest_FailsValidation()
        {
            ConsentConfig c = ConsentConfig.Defaults;
            c.ConsentDigitalAgeByCountry = new Dictionary<string, int>(StringComparer.Ordinal) { { "*", 13 }, { "DE", 12 } };
            Assert.Throws<InvalidOperationException>(c.Validate);
            c.ConsentDigitalAgeByCountry = new Dictionary<string, int>(StringComparer.Ordinal) { { "DE", 16 } };
            Assert.Throws<InvalidOperationException>(c.Validate);
        }

        [Test]
        public void RemoteDigitalAges_ReachTheConfig_AndABadMapFallsBackToTheDefaults()
        {
            var errors = new List<string>();
            ConsentConfig c = ConsentConfigBinder.Bind(Snapshot("consent_digital_age_by_country", "{\"*\":13,\"jp\":15}"), errors);
            Assert.That(errors, Is.Empty);
            Assert.That(c.DigitalAgeFor("JP"), Is.EqualTo(15), "«jp» de Remote Config vale para JP");
            Assert.That(c.DigitalAgeFor("DE"), Is.EqualTo(13), "sin entrada propia, el resto");

            errors.Clear();
            c = ConsentConfigBinder.Bind(Snapshot("consent_digital_age_by_country", "{\"*\":13,\"DE\":\"16\"}"), errors);
            Assert.That(errors, Has.Count.EqualTo(1), "una edad que no es un entero");
            Assert.That(c.DigitalAgeFor("DE"), Is.EqualTo(16), "se queda el default de la propiedad");

            errors.Clear();
            c = ConsentConfigBinder.Bind(Snapshot("consent_digital_age_by_country", "{\"*\":13,\"DE\":21}"), errors);
            Assert.That(errors, Has.Count.EqualTo(1), "fuera de 13-18: Validate falla y vuelven los defaults");
            Assert.That(c.DigitalAgeFor("DE"), Is.EqualTo(16));
        }

        [TestCase("DE", false)]
        [TestCase("FR", false)]
        [TestCase("KR", false)]
        [TestCase("IN", false)]
        [TestCase("CH", false)]
        [TestCase("BE", true)]
        [TestCase("GB", true)]
        [TestCase("CO", false)]
        [TestCase("BR", false)]
        [TestCase("US", true)]
        [TestCase("AR", true)]
        public void Teen_GetsTheAnalyticsChoice_OnlyWhereTheDigitalAgeIs13(string country, bool offered)
        {
            ConsentConfig c = ConsentConfig.Defaults;
            Assert.That(ConsentPolicy.OffersAnalyticsChoice(AgeBand.Teen, c, country), Is.EqualTo(offered));
            Assert.That(ConsentPolicy.OffersAnalyticsChoice(AgeBand.Adult, c, country), Is.True, "con 18+, siempre");

            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Teen, analytics: true, profile: c.ProfileFor(country)), c, c.ProfileFor(country), true, country);
            Assert.That(f.Decided, Is.True);
            Assert.That(f.AnalyticsEnabled, Is.EqualTo(offered), "aceptada antes, se apaga donde la edad pasa de 13");
            Assert.That(f.PurchasesAllowed, Is.True, "las compras siguen, con la doble confirmación");
        }

        [Test]
        public void Teen_WithoutCountry_GetsNoAnalytics()
        {
            ConsentConfig c = ConsentConfig.Defaults;
            Assert.That(ConsentPolicy.OffersAnalyticsChoice(AgeBand.Teen, c, null, countryKnown: false), Is.False, "Q-T3 (a): lo más estricto");
            Assert.That(ConsentPolicy.OffersAnalyticsChoice(AgeBand.Adult, c, null, countryKnown: false), Is.True);
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Teen, profile: LegalProfiles.Default), c, LegalProfiles.Default, countryKnown: false);
            Assert.That(f.AnalyticsEnabled, Is.False);
        }

        [Test]
        public void TheFlow_ForATeenInGermany_HidesAnalytics_AndAcceptAllLeavesItOff()
        {
            var store = new MemoryConsentStore();
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Ump, true, "DE") { Reported = reports.Add };
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Teen);
            Assert.That(flow.OffersAnalyticsChoice, Is.False);

            flow.Choose(ConsentAnswer.AcceptAll);
            Assert.That(store.Record!.Analytics, Is.False);
            Assert.That(flow.Flags.AnalyticsEnabled, Is.False);
            Assert.That(reports[reports.Count - 1].Analytics, Is.False, "af_consent dice lo mismo que el registro");

            var save = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Ump, true, "DE");
            save.StartBoot();
            save.ChooseAge(AgeBand.Teen);
            save.Choose(ConsentAnswer.Save, analytics: true);
            Assert.That(save.Record!.Analytics, Is.False, "no se puede encender lo que no se ofrece");
        }

        [Test]
        public void TheFlow_WithNothingToChoose_ReportsTheButton_NotARejection()
        {
            // 13-17 en Alemania (UMP, sin anuncios ni analítica que elegir): «Aceptar todo» no es un rechazo (revisión de D-057).
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Ump, true, "DE") { Reported = reports.Add };
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Teen);
            Assert.That(flow.OffersAdsChoice || flow.OffersAnalyticsChoice, Is.False);
            flow.Choose(ConsentAnswer.AcceptAll);
            Assert.That(reports[reports.Count - 1].Step, Is.EqualTo(ConsentReport.Accepted));
            Assert.That(reports[reports.Count - 1].Analytics, Is.False);

            var rejected = new List<ConsentReport>();
            var flow2 = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Ump, true, "DE") { Reported = rejected.Add };
            flow2.StartBoot();
            flow2.ChooseAge(AgeBand.Teen);
            flow2.Choose(ConsentAnswer.RejectAll);
            Assert.That(rejected[rejected.Count - 1].Step, Is.EqualTo(ConsentReport.Rejected));

            // Con algo que elegir, como siempre: guardar sin marcar nada es un rechazo.
            var adult = new List<ConsentReport>();
            var flow3 = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Co1581, true, "CO") { Reported = adult.Add };
            flow3.StartBoot();
            flow3.ChooseAge(AgeBand.Adult);
            flow3.Choose(ConsentAnswer.Save);
            Assert.That(adult[adult.Count - 1].Step, Is.EqualTo(ConsentReport.Rejected));
        }

        [Test]
        public void BeginChange_ForATeenInGermany_StartsWithAnalyticsOff_EvenIfTheRecordSaysOn()
        {
            var store = new MemoryConsentStore(new ConsentRecord(LegalProfiles.Ump, 1, T0, true, false, AgeBand.Teen));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Ump, true, "DE");
            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Done));
            Assert.That(flow.BeginChange(), Is.EqualTo(ConsentStep.Choices));
            Assert.That(flow.InitialAnalytics, Is.False, "lo que no se ofrece no sale marcado");
            Assert.That(flow.OffersAnalyticsChoice, Is.False);
            Assert.That(flow.Flags.AnalyticsEnabled, Is.False);
        }

        [Test]
        public void TheFlow_ForATeenInTheUnitedStates_StillOffersAnalytics()
        {
            var store = new MemoryConsentStore();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Default, true, "US");
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Teen);
            Assert.That(flow.OffersAnalyticsChoice, Is.True, "Q-B12-7 (a)");
            flow.Choose(ConsentAnswer.AcceptAll);
            Assert.That(store.Record!.Analytics, Is.True);
            Assert.That(flow.Flags.AnalyticsEnabled, Is.True);
        }

        [Test]
        public void CorrectingTheAgeTo13To17_InGermany_TurnsTheAnalyticsOff()
        {
            var store = new MemoryConsentStore();
            store.Save(new ConsentRecord(LegalProfiles.Ump, 1, T0, true, false, AgeBand.Adult));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Ump, true, "DE");
            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Done));
            Assert.That(flow.Flags.AnalyticsEnabled, Is.True);

            flow.BeginAgeCorrection();
            flow.ChooseAge(AgeBand.Teen);
            Assert.That(store.Record!.Analytics, Is.False);
            Assert.That(flow.Flags.AnalyticsEnabled, Is.False);
            flow.BeginChange();
            Assert.That(flow.InitialAnalytics, Is.False);
        }

        // ---- Recorrido ----

        [Test]
        public void FirstBoot_AgeThenChoices_AcceptAll_SavesTheProof()
        {
            var store = new MemoryConsentStore();
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581) { Reported = reports.Add };

            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Age));
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(reports[0].Step, Is.EqualTo("shown"));
            Assert.That(flow.ChooseAge(AgeBand.Adult), Is.EqualTo(ConsentStep.Choices));
            Assert.That(flow.InitialAnalytics, Is.False, "consentimiento expreso: nada marcado");
            Assert.That(flow.InitialAdsPersonalized, Is.False);
            Assert.That(flow.OffersAdsChoice, Is.True);
            Assert.That(flow.Choose(ConsentAnswer.AcceptAll), Is.EqualTo(ConsentStep.Done));

            ConsentRecord saved = store.Record!;
            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(saved.Profile, Is.EqualTo("co_1581"));
            Assert.That(saved.TextVersion, Is.EqualTo(1));
            Assert.That(saved.At, Is.EqualTo(T0));
            Assert.That(saved.Analytics, Is.True);
            Assert.That(saved.AdsPersonalized, Is.True);
            Assert.That(saved.AgeBand, Is.EqualTo(AgeBand.Adult));
            Assert.That(reports[1].Step, Is.EqualTo("accepted"));
            Assert.That(reports[1].Band, Is.EqualTo(AgeBand.Adult));
            Assert.That(flow.Flags.Decided, Is.True);
        }

        [Test]
        public void RejectAll_SavesEverythingOff_AndReportsRejected()
        {
            var store = new MemoryConsentStore();
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.BrLgpdEca) { Reported = reports.Add };
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Adult);

            flow.Choose(ConsentAnswer.RejectAll, analytics: true, adsPersonalized: true);

            Assert.That(store.Record!.Analytics, Is.False);
            Assert.That(store.Record!.AdsPersonalized, Is.False);
            Assert.That(reports[1].Step, Is.EqualTo("rejected"));
            Assert.That(flow.Flags.Decided, Is.True, "rechazar también deja jugar");
        }

        [Test]
        public void SaveMyChoice_KeepsOnlyWhatWasMarked()
        {
            var store = new MemoryConsentStore();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.MxLfpdppp);
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Adult);

            flow.Choose(ConsentAnswer.Save, analytics: true, adsPersonalized: false);

            Assert.That(store.Record!.Analytics, Is.True);
            Assert.That(store.Record!.AdsPersonalized, Is.False);
        }

        [Test]
        public void Teen_CannotTurnOnPersonalizedAds()
        {
            var store = new MemoryConsentStore();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.BrLgpdEca);
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Teen);
            Assert.That(flow.OffersAdsChoice, Is.False);

            flow.Choose(ConsentAnswer.AcceptAll);

            Assert.That(store.Record!.Analytics, Is.True);
            Assert.That(store.Record!.AdsPersonalized, Is.False);
            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Teen));
        }

        [Test]
        public void Ump_LeavesTheAdsToItsOwnMessage()
        {
            var store = new MemoryConsentStore();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Ump);
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Adult);

            flow.Choose(ConsentAnswer.AcceptAll);

            Assert.That(store.Record!.AdsPersonalized, Is.False, "lo decide UMP al inicializar los anuncios (08 §7 #2)");
            Assert.That(store.Record!.Analytics, Is.True);
            Assert.That(store.Record!.Profile, Is.EqualTo("ump"));
        }

        [Test]
        public void Under13_BlocksAndIsSaved_WithoutAnyConsentReport()
        {
            var store = new MemoryConsentStore();
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581) { Reported = reports.Add };
            flow.StartBoot();

            Assert.That(flow.ChooseAge(AgeBand.Under13), Is.EqualTo(ConsentStep.Blocked));
            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Under13));
            Assert.That(store.Record!.Analytics, Is.False);
            Assert.That(reports, Has.Count.EqualTo(1), "solo el shown del arranque");
            Assert.That(flow.Flags.Blocked, Is.True);

            var again = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581);
            Assert.That(again.StartBoot(), Is.EqualTo(ConsentStep.Blocked), "en cualquier arranque posterior (08 §7)");
        }

        [Test]
        public void AValidDecision_ShowsNothingAtBoot()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult));
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581) { Reported = reports.Add };

            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Done));
            Assert.That(reports, Is.Empty);
            Assert.That(store.Saves, Is.EqualTo(0));
        }

        [Test]
        public void NewLegalTexts_AskForTheChoicesAgain_KeepingTheAgeBand()
        {
            var config = ConsentConfig.Defaults;
            config.ConsentTextVersion = 2;
            var store = new MemoryConsentStore(Record(AgeBand.Teen, version: 1));
            var flow = new ConsentFlow(config, store, Clock(), LegalProfiles.Co1581);

            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Choices));
            flow.Choose(ConsentAnswer.RejectAll);

            Assert.That(store.Record!.TextVersion, Is.EqualTo(2));
            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Teen));
        }

        [Test]
        public void AnotherLegalProfile_AsksForTheChoicesAgain_WithTheProfileOfNow()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult, analytics: true, ads: false, profile: LegalProfiles.Ump));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.BrLgpdEca);

            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Choices), "decidió en el EEE y ahora está en Brasil");
            Assert.That(flow.OffersAdsChoice, Is.True);
            flow.Choose(ConsentAnswer.RejectAll);

            Assert.That(store.Record!.Profile, Is.EqualTo(LegalProfiles.BrLgpdEca));
            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Adult), "la banda se conserva");
            Assert.That(flow.Flags.AdsByUmp, Is.False, "fuera del EEE, sin UMP: npa");
        }

        [Test]
        public void TheFlags_FollowTheProfileAndCountryOfNow()
        {
            ConsentRecord us = Record(AgeBand.Adult, analytics: true, ads: true, profile: LegalProfiles.Default);
            ConsentConfig c = ConsentConfig.Defaults;

            ConsentFlags known = ConsentPolicy.Flags(us, c, LegalProfiles.Default, countryKnown: true);
            Assert.That(known.AdsPersonalized, Is.True);
            ConsentFlags unknown = ConsentPolicy.Flags(us, c, LegalProfiles.Default, countryKnown: false);
            Assert.That(unknown.Decided, Is.True);
            Assert.That(unknown.AdsPersonalized, Is.False, "sin país, npa aunque se aceptara con país (Q-T3 (a))");
            ConsentFlags other = ConsentPolicy.Flags(us, c, LegalProfiles.BrLgpdEca);
            Assert.That(other.Decided, Is.False, "otro perfil: sin decidir hasta volver a preguntar (Q-B12-15)");
            Assert.That(other.AdsPersonalized, Is.False);
            Assert.That(other.PlayGamesAllowed, Is.False, "sin decidir, sin Play Games");
        }

        [Test]
        public void TheFlow_SaysWhenUmpDecides_AlsoForTeensWithoutTheRestriction()
        {
            var config = ConsentConfig.Defaults;
            config.BizUnder18NoPersonalization = false;
            var flow = new ConsentFlow(config, new MemoryConsentStore(), Clock(), LegalProfiles.Ump);
            flow.StartBoot();
            flow.ChooseAge(AgeBand.Teen);
            Assert.That(flow.AdsByUmp, Is.True);
            Assert.That(flow.OffersAdsChoice, Is.False);

            var strict = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Ump);
            strict.StartBoot();
            strict.ChooseAge(AgeBand.Teen);
            Assert.That(strict.AdsByUmp, Is.False, "13-17 con la restricción: npa también en el EEE");

            var noCountry = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Ump, countryKnown: false);
            noCountry.StartBoot();
            noCountry.ChooseAge(AgeBand.Adult);
            Assert.That(noCountry.AdsByUmp, Is.False);
        }

        [Test]
        public void Teen_WithPersonalizationAllowed_IsLikeAnAdult_AlsoWithUmp()
        {
            var config = ConsentConfig.Defaults;
            config.BizUnder18NoPersonalization = false;
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Teen, profile: LegalProfiles.Ump), config);
            Assert.That(f.AdsByUmp, Is.True);
            Assert.That(f.PurchaseDoubleConfirm, Is.False);
        }

        [Test]
        public void AtBoot_TheScreenCannotBeClosedWithoutDeciding()
        {
            var flow = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), LegalProfiles.Co1581);
            flow.StartBoot();
            Assert.Throws<InvalidOperationException>(() => flow.Cancel());
        }

        [Test]
        public void FromSettings_ChangeStartsWithWhatWasSaved_AndReportsChanged()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult, analytics: true, ads: false));
            var reports = new List<ConsentReport>();
            ManualClock clock = Clock();
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, clock, LegalProfiles.Co1581) { Reported = reports.Add };
            flow.StartBoot();

            clock.Advance(TimeSpan.FromDays(3));
            Assert.That(flow.BeginChange(), Is.EqualTo(ConsentStep.Choices));
            Assert.That(flow.InitialAnalytics, Is.True);
            Assert.That(flow.InitialAdsPersonalized, Is.False);
            flow.Choose(ConsentAnswer.Save, analytics: false, adsPersonalized: true);

            Assert.That(store.Record!.Analytics, Is.False, "revocación en Ajustes (05 §7)");
            Assert.That(store.Record!.AdsPersonalized, Is.True);
            Assert.That(store.Record!.At, Is.EqualTo(T0.AddDays(3)));
            Assert.That(reports[reports.Count - 1].Step, Is.EqualTo("changed"));
        }

        [Test]
        public void FromSettings_ClosingChangesNothing()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581);
            flow.StartBoot();
            flow.BeginChange();

            flow.Cancel();

            Assert.That(flow.Step, Is.EqualTo(ConsentStep.None));
            Assert.That(store.Saves, Is.EqualTo(0));
        }

        [Test]
        public void TheAgeBand_IsCorrectedOnce_AndTeenTurnsOffPersonalizedAds()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult, analytics: true, ads: true));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581);
            flow.StartBoot();
            Assert.That(flow.CanCorrectAge, Is.True);

            flow.BeginAgeCorrection();
            Assert.That(flow.ChooseAge(AgeBand.Teen), Is.EqualTo(ConsentStep.Done));

            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Teen));
            Assert.That(store.Record!.AgeBandCorrected, Is.True);
            Assert.That(store.Record!.AdsPersonalized, Is.False);
            Assert.That(store.Record!.Analytics, Is.True);
            Assert.That(flow.CanCorrectAge, Is.False, "una sola vez (08 §7)");
            Assert.Throws<InvalidOperationException>(() => flow.BeginAgeCorrection());
        }

        [Test]
        public void TheCorrection_KeepsTheProfileAndVersionOfTheDecision_InTheRecordAndInTheReport()
        {
            var config = ConsentConfig.Defaults;
            var store = new MemoryConsentStore(Record(AgeBand.Adult, profile: LegalProfiles.Co1581));
            var reports = new List<ConsentReport>();
            var flow = new ConsentFlow(config, store, Clock(), LegalProfiles.BrLgpdEca) { Reported = reports.Add };
            flow.StartBoot();
            flow.BeginAgeCorrection();

            flow.ChooseAge(AgeBand.Teen);

            Assert.That(store.Record!.Profile, Is.EqualTo(LegalProfiles.Co1581), "la corrección cambia solo la banda");
            Assert.That(reports[reports.Count - 1].Profile, Is.EqualTo(LegalProfiles.Co1581), "af_consent dice lo mismo que el registro");
            Assert.That(reports[reports.Count - 1].TextVersion, Is.EqualTo(store.Record!.TextVersion));
        }

        [Test]
        public void ChoosingTheSameBand_DoesNotSpendTheCorrection()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581);
            flow.StartBoot();
            flow.BeginAgeCorrection();

            flow.ChooseAge(AgeBand.Adult);

            Assert.That(store.Saves, Is.EqualTo(0));
            Assert.That(flow.CanCorrectAge, Is.True);
        }

        [Test]
        public void CorrectingTo13_Blocks()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Teen));
            var flow = new ConsentFlow(ConsentConfig.Defaults, store, Clock(), LegalProfiles.Co1581);
            flow.StartBoot();
            flow.BeginAgeCorrection();

            Assert.That(flow.ChooseAge(AgeBand.Under13), Is.EqualTo(ConsentStep.Blocked));
            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Under13));
            Assert.That(flow.Flags.Blocked, Is.True);
            Assert.That(flow.Flags.ChildDirected, Is.True);
        }

        [Test]
        public void AnUnknownLegalProfile_IsTreatedAsDefault()
        {
            var flow = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(), Clock(), "xx");
            Assert.That(flow.Profile, Is.EqualTo(LegalProfiles.Default));
        }

        private static RcSnapshot Snapshot(params string[] kv)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < kv.Length; i += 2)
            {
                d[kv[i]] = kv[i + 1];
            }

            var rc = new RemoteConfigService();
            Assert.That(rc.Activate(d), Is.True);
            return rc.Freeze();
        }

        private static List<string> SortedAges(IReadOnlyDictionary<string, int> map)
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, int> entry in map)
            {
                list.Add(entry.Key + "=" + entry.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static List<string> Sorted(IReadOnlyDictionary<string, string> map)
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, string> entry in map)
            {
                list.Add(entry.Key + "=" + entry.Value);
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}
