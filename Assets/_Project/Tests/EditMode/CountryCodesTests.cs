#nullable enable
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El país de la tienda de iOS en alfa-3 pasado a alfa-2 (<see cref="CountryCodes"/>; fase M2 de <c>mac-ios.md</c>; D-057). La
    /// tabla entera se comprobó contra ICU (<c>uloc_getISO3Country</c>) al escribirla; aquí, los países de la Liga y de T1 y los
    /// casos raros.
    /// </summary>
    public sealed class CountryCodesTests
    {
        [TestCase("COL", "CO")]
        [TestCase("BRA", "BR")]
        [TestCase("MEX", "MX")]
        [TestCase("PER", "PE")]
        [TestCase("ARG", "AR")]
        [TestCase("CHL", "CL")]
        [TestCase("ECU", "EC")]
        [TestCase("BOL", "BO")]
        [TestCase("VEN", "VE")]
        [TestCase("USA", "US")]
        [TestCase("CAN", "CA")]
        [TestCase("GBR", "GB")]
        [TestCase("DEU", "DE")]
        [TestCase("ESP", "ES")]
        [TestCase("FRA", "FR")]
        [TestCase("JPN", "JP")]
        [TestCase("KOR", "KR")]
        [TestCase("AUS", "AU")]
        [TestCase("CHE", "CH")]
        [TestCase("ZAF", "ZA")]
        public void Alpha2FromAlpha3_GivesTheIsoCode(string alpha3, string alpha2)
        {
            Assert.That(CountryCodes.Alpha2FromAlpha3(alpha3), Is.EqualTo(alpha2));
            Assert.That(ConsentCountry.Normalize(CountryCodes.Alpha2FromAlpha3(alpha3)), Is.EqualTo(alpha2), "lo acepta el país legal");
        }

        [Test]
        public void Alpha2FromAlpha3_IgnoresCase_AndRejectsWhatIsNotACurrentCode()
        {
            Assert.That(CountryCodes.Alpha2FromAlpha3("col"), Is.EqualTo("CO"));
            Assert.That(CountryCodes.Alpha2FromAlpha3(null), Is.Null);
            Assert.That(CountryCodes.Alpha2FromAlpha3(string.Empty), Is.Null);
            Assert.That(CountryCodes.Alpha2FromAlpha3("CO"), Is.Null, "alfa-2, no alfa-3");
            Assert.That(CountryCodes.Alpha2FromAlpha3("XXX"), Is.Null);
            Assert.That(CountryCodes.Alpha2FromAlpha3("ANT"), Is.Null, "las Antillas Neerlandesas ya no son un código vigente");
        }

        [Test]
        public void TheTable_HasThe249CurrentCodes()
        {
            Assert.That(CountryCodes.Count, Is.EqualTo(249));
        }
    }
}
