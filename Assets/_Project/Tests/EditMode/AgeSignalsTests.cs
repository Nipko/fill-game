#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Play Age Signals sin motor (T-L3 y T-L4 de <c>docs/legal/investigacion-legal-mundial.md</c> §11; D-057 y su añadido del
    /// 2026-10-27; <c>consent_age_signal_source</c> de 08 §9): el formato del puente, las bandas (0-12 bloquea; 13-15 y 16-17 son
    /// 13-17; sin señal, la autodeclarada), la más estricta de las dos, los flags y el flujo, y que la banda de la señal nunca va a la
    /// analítica.
    /// </summary>
    public sealed class AgeSignalsTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc);

        private static ManualClock Clock() => new ManualClock(T0, TimeSpan.FromHours(-5));

        private static ConsentConfig WithSignals() => new ConsentConfig { ConsentAgeSignalSource = ConsentConfig.SourceWithPlayAgeSignals };

        private static ConsentRecord Record(AgeBand band, bool analytics = true, bool ads = true)
        {
            return new ConsentRecord(LegalProfiles.Default, 1, T0, analytics, ads, band);
        }

        // ---- La señal ----

        [TestCase("range:0:12", AgeBand.Under13)]
        [TestCase("range:13:15", AgeBand.Teen)]
        [TestCase("range:16:17", AgeBand.Teen)]
        [TestCase("range:18", AgeBand.Adult)]
        [TestCase("adult", AgeBand.Adult)]
        [TestCase("none", AgeBand.Unknown)]
        [TestCase("error", AgeBand.Unknown)]
        [TestCase("error:-1", AgeBand.Unknown)]
        [TestCase("", AgeBand.Unknown)]
        [TestCase(null, AgeBand.Unknown)]
        [TestCase("range", AgeBand.Unknown)]
        [TestCase("range:x:12", AgeBand.Unknown)]
        [TestCase("range:15:13", AgeBand.Unknown)]
        [TestCase("range:-1:12", AgeBand.Unknown)]
        [TestCase("supervised", AgeBand.Unknown)]
        public void Parse_GivesTheBandsOfTL3(string? wire, AgeBand expected)
        {
            Assert.That(AgeSignal.Parse(wire).Band, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_UnknownOrBroken_IsAnError_AndNone_IsNoSignal()
        {
            Assert.That(AgeSignal.Parse("none"), Is.EqualTo(AgeSignal.None));
            Assert.That(AgeSignal.Parse("garbage").Status, Is.EqualTo(AgeSignalStatus.Error));
            Assert.That(AgeSignal.Parse(null).Status, Is.EqualTo(AgeSignalStatus.Error));
        }

        [Test]
        public void ToString_RoundTripsThroughParse()
        {
            foreach (AgeSignal s in new[] { AgeSignal.None, AgeSignal.Error, AgeSignal.VerifiedAdult, AgeSignal.ForRange(0, 12), AgeSignal.ForRange(13, 15), AgeSignal.ForRange(18) })
            {
                Assert.That(AgeSignal.Parse(s.ToString()), Is.EqualTo(s), s.ToString());
            }
        }

        [Test]
        public void ForRange_RejectsBrokenRanges()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AgeSignal.ForRange(-1, 12));
            Assert.Throws<ArgumentOutOfRangeException>(() => AgeSignal.ForRange(16, 13));
        }

        [Test]
        public void FakeAgeSignals_AnswersOncePerRequest()
        {
            var fake = new FakeAgeSignals();
            Assert.That(fake.TryTake(out _), Is.False);
            fake.Request();
            Assert.That(fake.TryTake(out _), Is.False, "sin Result, espera a Complete");
            fake.Complete(AgeSignal.ForRange(13, 15));
            Assert.That(fake.TryTake(out AgeSignal s), Is.True);
            Assert.That(s.Band, Is.EqualTo(AgeBand.Teen));
            Assert.That(fake.TryTake(out _), Is.False, "una sola vez");

            var instant = new FakeAgeSignals(AgeSignal.VerifiedAdult);
            instant.Request();
            Assert.That(instant.TryTake(out AgeSignal a) && a.Band == AgeBand.Adult, Is.True);
            Assert.That(instant.Requests, Is.EqualTo(1));
        }

        // ---- Política ----

        [Test]
        public void Enabled_OnlyWithTheSecondSourceOf08_9()
        {
            Assert.That(AgeSignalPolicy.Enabled(ConsentConfig.Defaults), Is.False, "default de 08 §9: self_declared");
            Assert.That(AgeSignalPolicy.Enabled(WithSignals()), Is.True);
        }

        [TestCase(AgeBand.Adult, AgeBand.Unknown, AgeBand.Adult)]
        [TestCase(AgeBand.Teen, AgeBand.Unknown, AgeBand.Teen)]
        [TestCase(AgeBand.Adult, AgeBand.Teen, AgeBand.Teen)]
        [TestCase(AgeBand.Adult, AgeBand.Under13, AgeBand.Under13)]
        [TestCase(AgeBand.Teen, AgeBand.Under13, AgeBand.Under13)]
        [TestCase(AgeBand.Teen, AgeBand.Adult, AgeBand.Teen)]
        [TestCase(AgeBand.Unknown, AgeBand.Under13, AgeBand.Under13)]
        [TestCase(AgeBand.Unknown, AgeBand.Unknown, AgeBand.Unknown)]
        public void Effective_IsTheStricterOfTheTwo(AgeBand declared, AgeBand signal, AgeBand expected)
        {
            Assert.That(AgeSignalPolicy.Effective(declared, signal), Is.EqualTo(expected));
        }

        // ---- Flags ----

        [Test]
        public void Flags_Signal0to12_Blocks_EvenWithoutADeclaredBand()
        {
            foreach (ConsentRecord? record in new[] { null, Record(AgeBand.Adult), Record(AgeBand.Teen) })
            {
                ConsentFlags f = ConsentPolicy.Flags(record, WithSignals(), signalBand: AgeBand.Under13);
                Assert.That(f.Blocked, Is.True);
                Assert.That(f.Decided, Is.False);
                Assert.That(f.ChildDirected, Is.True);
                Assert.That(f.AnalyticsEnabled, Is.False);
                Assert.That(f.PurchasesAllowed, Is.False);
                Assert.That(f.BandFromSignal, Is.True);
            }
        }

        [Test]
        public void Flags_DeclaredAdult_SignalTeen_AreThoseOfTeen()
        {
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Adult, analytics: true, ads: true), WithSignals(), signalBand: AgeBand.Teen);

            Assert.That(f.Band, Is.EqualTo(AgeBand.Teen));
            Assert.That(f.BandFromSignal, Is.True);
            Assert.That(f.AdsPersonalized, Is.False, "13-17: npa aunque aceptara con 18+");
            Assert.That(f.PurchaseDoubleConfirm, Is.True);
            Assert.That(f.RcPersonalization, Is.False);
            Assert.That(f.AnalyticsEnabled, Is.True, "la analítica sigue la decisión, como con 13-17 declarado");
        }

        [Test]
        public void Flags_DeclaredTeen_SignalAdult_StayTeen()
        {
            ConsentFlags f = ConsentPolicy.Flags(Record(AgeBand.Teen), WithSignals(), signalBand: AgeBand.Adult);

            Assert.That(f.Band, Is.EqualTo(AgeBand.Teen), "un 18+ verificado no relaja la banda declarada [P]");
            Assert.That(f.BandFromSignal, Is.False);
        }

        [Test]
        public void Flags_WithoutSignal_AreTheSelfDeclared()
        {
            ConsentFlags with = ConsentPolicy.Flags(Record(AgeBand.Adult), WithSignals(), signalBand: AgeBand.Unknown);
            ConsentFlags without = ConsentPolicy.Flags(Record(AgeBand.Adult), WithSignals());

            Assert.That(with.Band, Is.EqualTo(AgeBand.Adult));
            Assert.That(with.AdsPersonalized, Is.EqualTo(without.AdsPersonalized).And.True);
            Assert.That(with.BandFromSignal, Is.False);
        }

        // ---- Flujo ----

        [Test]
        public void Flow_WithSelfDeclaredSource_IgnoresTheSignal()
        {
            var flow = new ConsentFlow(ConsentConfig.Defaults, new MemoryConsentStore(Record(AgeBand.Adult)), Clock(), LegalProfiles.Default);

            Assert.That(flow.ApplyAgeSignal(AgeSignal.ForRange(0, 12)), Is.False);
            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Done));
            Assert.That(flow.Flags.Band, Is.EqualTo(AgeBand.Adult));
        }

        [Test]
        public void Flow_Signal0to12_BlocksTheBoot_WithoutSavingAnything()
        {
            var store = new MemoryConsentStore(Record(AgeBand.Adult));
            var flow = new ConsentFlow(WithSignals(), store, Clock(), LegalProfiles.Default);

            Assert.That(flow.ApplyAgeSignal(AgeSignal.ForRange(0, 12)), Is.True);
            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Blocked));
            Assert.That(flow.Flags.Blocked, Is.True);
            Assert.That(store.Saves, Is.EqualTo(0), "la señal se pide en cada arranque: no se guarda");
            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Adult));
        }

        [Test]
        public void Flow_Signal0to12_OnTheFirstRun_BlocksBeforeTheAgeScreen()
        {
            var flow = new ConsentFlow(WithSignals(), new MemoryConsentStore(), Clock(), LegalProfiles.Default);
            flow.ApplyAgeSignal(AgeSignal.ForRange(0, 12));

            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Blocked));
        }

        [Test]
        public void Flow_Signal0to12_ArrivingLate_Blocks()
        {
            var flow = new ConsentFlow(WithSignals(), new MemoryConsentStore(Record(AgeBand.Adult)), Clock(), LegalProfiles.Default);
            Assert.That(flow.StartBoot(), Is.EqualTo(ConsentStep.Done));

            Assert.That(flow.ApplyAgeSignal(AgeSignal.ForRange(0, 12)), Is.True);
            Assert.That(flow.Step, Is.EqualTo(ConsentStep.Blocked));
            Assert.That(flow.Flags.Blocked, Is.True);
            Assert.That(flow.ApplyAgeSignal(AgeSignal.ForRange(0, 12)), Is.False, "la misma señal no cambia nada");
        }

        [Test]
        public void Flow_SignalTeen_HidesThePersonalizedAdsChoice_AndKeepsTheDeclaredBandInTheRecord()
        {
            var store = new MemoryConsentStore();
            var flow = new ConsentFlow(WithSignals(), store, Clock(), LegalProfiles.Default);
            flow.ApplyAgeSignal(AgeSignal.ForRange(16, 17));
            var reports = new List<ConsentReport>();
            flow.Reported = reports.Add;

            flow.StartBoot();
            flow.ChooseAge(AgeBand.Adult);
            Assert.That(flow.EffectiveBand, Is.EqualTo(AgeBand.Teen));
            Assert.That(flow.OffersAdsChoice, Is.False, "13-17 por la señal: sin anuncios personalizados");
            flow.Choose(ConsentAnswer.AcceptAll);

            Assert.That(store.Record!.AgeBand, Is.EqualTo(AgeBand.Adult), "el registro guarda la autodeclarada");
            Assert.That(store.Record.AdsPersonalized, Is.False);
            Assert.That(flow.Flags.Band, Is.EqualTo(AgeBand.Teen));
        }

        // ---- T-L4: la banda de la señal nunca va a la analítica ----

        [Test]
        public void Report_SignalCorrectedTheBand_SendsNoAgeBand()
        {
            var flow = new ConsentFlow(WithSignals(), new MemoryConsentStore(), Clock(), LegalProfiles.Default);
            flow.ApplyAgeSignal(AgeSignal.ForRange(13, 15));
            var reports = new List<ConsentReport>();
            flow.Reported = reports.Add;

            flow.StartBoot();
            flow.ChooseAge(AgeBand.Adult);
            flow.Choose(ConsentAnswer.AcceptAll);

            ConsentReport accepted = reports[reports.Count - 1];
            Assert.That(accepted.Step, Is.EqualTo(ConsentReport.Accepted));
            Assert.That(accepted.Band, Is.EqualTo(AgeBand.Adult), "la autodeclarada, nunca la de la señal");
            Assert.That(accepted.BandFromSignal, Is.True);
            Assert.That(accepted.AnalyticsAgeBand, Is.Null, "T-L4: corregida por la señal, af_consent sin age_band");
        }

        [Test]
        public void Report_SignalAgrees_SendsTheSelfDeclaredBand()
        {
            var flow = new ConsentFlow(WithSignals(), new MemoryConsentStore(), Clock(), LegalProfiles.Default);
            flow.ApplyAgeSignal(AgeSignal.ForRange(13, 15));
            var reports = new List<ConsentReport>();
            flow.Reported = reports.Add;

            flow.StartBoot();
            flow.ChooseAge(AgeBand.Teen);
            flow.Choose(ConsentAnswer.RejectAll);

            ConsentReport rejected = reports[reports.Count - 1];
            Assert.That(rejected.BandFromSignal, Is.False);
            Assert.That(rejected.AnalyticsAgeBand, Is.EqualTo("13-17"), "la autodeclarada");
        }

        [Test]
        public void Report_WithoutSignal_IsAsBefore()
        {
            var report = new ConsentReport(ConsentReport.Accepted, LegalProfiles.Default, 1, true, true, AgeBand.Adult);
            Assert.That(report.BandFromSignal, Is.False);
            Assert.That(report.AnalyticsAgeBand, Is.EqualTo("18+"));
            Assert.That(new ConsentReport(ConsentReport.Shown, LegalProfiles.Default, 1, false, false, AgeBand.Unknown).AnalyticsAgeBand, Is.Null);
        }
    }
}
