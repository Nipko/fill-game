#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.EditorTools;
using AF.Platform;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El aviso de rastreo de Apple (ATT; fase M3 de <c>mac-ios.md</c>; regla aprobada por el dueño el 2026-11-04): solo con la edad
    /// decidida, 18+, anuncios personalizados e iOS sin preguntar aún; los textos salen de <c>strings.csv</c> en los tres idiomas y
    /// <see cref="BuildIos"/> los escribe en el <c>Info.plist</c> sin duplicar la clave.
    /// </summary>
    public sealed class IosTrackingTests
    {
        private const string Plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<plist version=\"1.0\">\n<dict>\n"
            + "  <key>CFBundleName</key>\n  <string>Tintalú (dev)</string>\n</dict>\n</plist>\n";

        [Test]
        public void Asks_OnlyAdultsWithPersonalizedAds_WhileIosHasNotAsked()
        {
            Assert.That(IosTracking.ShouldAsk(TrackingStatus.NotDetermined, true, false, AgeBand.Adult, true), Is.True);
            Assert.That(IosTracking.ShouldAsk(TrackingStatus.NotDetermined, true, false, AgeBand.Adult, false), Is.False, "npa");
            Assert.That(IosTracking.ShouldAsk(TrackingStatus.NotDetermined, true, false, AgeBand.Teen, true), Is.False, "13-17");
            Assert.That(IosTracking.ShouldAsk(TrackingStatus.NotDetermined, true, true, AgeBand.Under13, true), Is.False, "<13");
            Assert.That(IosTracking.ShouldAsk(TrackingStatus.NotDetermined, false, false, AgeBand.Adult, true), Is.False, "sin decidir");
            Assert.That(IosTracking.ShouldAsk(TrackingStatus.NotDetermined, true, true, AgeBand.Adult, true), Is.False, "bloqueo");
            foreach (TrackingStatus answered in new[] { TrackingStatus.Restricted, TrackingStatus.Denied, TrackingStatus.Authorized })
            {
                Assert.That(IosTracking.ShouldAsk(answered, true, false, AgeBand.Adult, true), Is.False, answered.ToString());
            }

            Assert.That(IosTracking.ShouldAsk(ConsentPolicy.Undecided, TrackingStatus.NotDetermined), Is.False);
        }

        [Test]
        public void NoTrackingPort_OutsideTheIphone()
        {
            Assert.That(IosTracking.ForPlatform(), Is.Null);
        }

        [Test]
        public void FakePort_AnswersOnce_ThenKeepsTheAnswer()
        {
            var port = new FakeTrackingPort(TrackingStatus.NotDetermined, TrackingStatus.Denied);
            TrackingStatus? got = null;
            port.Request(s => got = s);
            Assert.That(got, Is.EqualTo(TrackingStatus.Denied));
            port.Answer = TrackingStatus.Authorized;
            port.Request(s => got = s);
            Assert.That(got, Is.EqualTo(TrackingStatus.Denied));
            Assert.That(port.Requests, Is.EqualTo(2));
        }

        [Test]
        public void UsageDescription_ComesFromStringsCsv_InEveryLanguage()
        {
            Dictionary<string, string> texts = BuildIos.TrackingUsageTexts(File.ReadAllBytes(BuildIos.StringsCsvPath));
            Assert.That(texts.Keys, Is.EquivalentTo(new[] { "es-419", "en", "pt-BR" }));
            foreach (string text in texts.Values)
            {
                Assert.That(text.Length, Is.InRange(10, 120));
            }
        }

        [Test]
        public void UsageDescription_MissingRow_Throws()
        {
            byte[] csv = System.Text.Encoding.UTF8.GetBytes("key,es-419,en,pt-BR\nother,a,b,c\n");
            Assert.That(() => BuildIos.TrackingUsageTexts(csv), Throws.InvalidOperationException);
        }

        [Test]
        public void UsageDescription_MissingTranslation_Throws()
        {
            byte[] csv = System.Text.Encoding.UTF8.GetBytes("key,es-419,en,pt-BR\natt_usage_description,a,,c\n");
            Assert.That(() => BuildIos.TrackingUsageTexts(csv), Throws.InvalidOperationException);
        }

        [Test]
        public void PrePromptKeys_ExistInStringsCsv_InEveryLanguage()
        {
            IReadOnlyList<AF.Localization.LocCsvRow> rows = AF.Localization.LocCsv.Parse(
                AF.Localization.LocCsv.Decode(File.ReadAllBytes(BuildIos.StringsCsvPath), BuildIos.StringsCsvPath), BuildIos.StringsCsvPath);
            var found = new HashSet<string>();
            foreach (AF.Localization.LocCsvRow row in rows)
            {
                string key = row.Fields[0];
                if (key == AttPrePromptCard.TitleKey || key == AttPrePromptCard.BodyKey || key == AttPrePromptCard.ContinueKey)
                {
                    Assert.That(row.Fields[1].Length > 0 && row.Fields[2].Length > 0 && row.Fields[3].Length > 0, Is.True, key);
                    found.Add(key);
                }
            }

            Assert.That(found.Count, Is.EqualTo(3));
        }

        [Test]
        public void SetPlistString_AddsOnce_AndReplaces_Escaped()
        {
            string once = BuildIos.SetPlistString(Plist, "NSUserTrackingUsageDescription", "Ads & <more>");
            string twice = BuildIos.SetPlistString(once, "NSUserTrackingUsageDescription", "Second");
            Assert.That(once, Does.Contain("<string>Ads &amp; &lt;more&gt;</string>"));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(twice, "NSUserTrackingUsageDescription").Count, Is.EqualTo(1));
            Assert.That(twice, Does.Contain("<string>Second</string>"));
            Assert.That(twice, Does.Contain("<key>CFBundleName</key>"));
        }
    }
}
