#nullable enable
using AF.Localization;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los miles según el idioma del juego y, en español, el país (observación 22 de la jornada 6, decisión del dueño del
    /// 2026-10-30): en «1,000», pt-BR «1.000» y es-419 con el punto en Colombia, Argentina, Chile, Uruguay, Paraguay, Venezuela,
    /// Ecuador, Bolivia y Brasil y la coma en México, Perú, Guatemala, la Centroamérica de la coma y EE. UU.; sin país, el punto.
    /// Todo con la tabla propia, sin <c>CultureInfo</c>; y la región del teléfono ya no manda sobre el idioma (en-US con la UI
    /// en pt-BR da «1.000», que era el fallo).
    /// </summary>
    public sealed class LocNumberCountryTests
    {
        private LocStateSnapshot _before;

        [SetUp]
        public void SetUp()
        {
            _before = LocStateSnapshot.Take();
        }

        [TearDown]
        public void TearDown()
        {
            _before.Restore();
        }

        [TestCase("en", null, "en")]
        [TestCase("en", "CO", "en")]
        [TestCase("en", "BR", "en")]
        [TestCase("pt-BR", null, "pt-BR")]
        [TestCase("pt-BR", "US", "pt-BR")]
        [TestCase("pt-BR", "MX", "pt-BR")]
        [TestCase("es-419", null, "es-CO")]
        [TestCase("es-419", "co", "es-CO")]
        [TestCase("es-419", "MX", "es-MX")]
        [TestCase("es-419", "419", "es-CO")]
        [TestCase("es-419", "", "es-CO")]
        [TestCase("en-XA", "MX", "es-MX")]
        [TestCase("en-XA", null, "es-CO")]
        public void NumberTagFor_FollowsTheLanguageAndInSpanishTheCountry(string locale, string? country, string tag)
        {
            Assert.That(LocLocale.NumberTagFor(locale, country), Is.EqualTo(tag));
        }

        /// <summary>La tabla de es-419 que fijó el dueño, país por país.</summary>
        [TestCase("CO", "1.000")]
        [TestCase("AR", "1.000")]
        [TestCase("CL", "1.000")]
        [TestCase("UY", "1.000")]
        [TestCase("PY", "1.000")]
        [TestCase("VE", "1.000")]
        [TestCase("EC", "1.000")]
        [TestCase("BO", "1.000")]
        [TestCase("BR", "1.000")]
        [TestCase("ES", "1.000")]
        [TestCase("MX", "1,000")]
        [TestCase("PE", "1,000")]
        [TestCase("GT", "1,000")]
        [TestCase("HN", "1,000")]
        [TestCase("NI", "1,000")]
        [TestCase("PA", "1,000")]
        [TestCase("SV", "1,000")]
        [TestCase("DO", "1,000")]
        [TestCase("PR", "1,000")]
        [TestCase("CU", "1,000")]
        [TestCase("US", "1,000")]
        [TestCase("CR", "1 000")]
        [TestCase("DE", "1.000")]
        [TestCase(null, "1.000")]
        public void Spanish_ThousandsByCountry(string? country, string expected)
        {
            LocNumberFormat format = LocNumber.For(LocLocale.NumberTagFor("es-419", country));
            Assert.That(LocNumber.Format(1000L, true, format), Is.EqualTo(expected), country ?? "sin país");
        }

        [TestCase("en", null, 0L, "0")]
        [TestCase("en", null, 999L, "999")]
        [TestCase("en", null, 1000L, "1,000")]
        [TestCase("en", null, 13040L, "13,040")]
        [TestCase("en", null, 1000000L, "1,000,000")]
        [TestCase("en", null, -1000L, "-1,000")]
        [TestCase("pt-BR", "US", 0L, "0")]
        [TestCase("pt-BR", "US", 999L, "999")]
        [TestCase("pt-BR", "US", 1000L, "1.000")]
        [TestCase("pt-BR", "US", 13040L, "13.040")]
        [TestCase("pt-BR", "US", 1000000L, "1.000.000")]
        [TestCase("pt-BR", "US", -13040L, "-13.040")]
        [TestCase("es-419", "CO", 0L, "0")]
        [TestCase("es-419", "CO", 999L, "999")]
        [TestCase("es-419", "CO", 1000L, "1.000")]
        [TestCase("es-419", "AR", 13040L, "13.040")]
        [TestCase("es-419", "CL", 1000000L, "1.000.000")]
        [TestCase("es-419", "MX", 1000L, "1,000")]
        [TestCase("es-419", "PE", 13040L, "13,040")]
        [TestCase("es-419", "US", 1000000L, "1,000,000")]
        [TestCase("es-419", "MX", -1000000L, "-1,000,000")]
        [TestCase("es-419", null, 13040L, "13.040")]
        public void Loc_Number_ByLanguageAndCountry(string locale, string? country, long value, string expected)
        {
            Loc.Configure(LocSamples.Catalog(), locale, true, null, country);
            Assert.That(Loc.Number(value), Is.EqualTo(expected), locale + " " + (country ?? "-"));
        }

        [Test]
        public void Loc_Decimals_FollowTheSameRow()
        {
            Loc.Configure(LocSamples.Catalog(), "pt-BR", true, null, "US");
            Assert.That(Loc.Number(1234.5, 1), Is.EqualTo("1.234,5"));
            Loc.Configure(LocSamples.Catalog(), "en", true, null, "BR");
            Assert.That(Loc.Number(1234.5, 1), Is.EqualTo("1,234.5"));
            Loc.Configure(LocSamples.Catalog(), "es-419", true, null, "MX");
            Assert.That(Loc.Number(1234.5, 1), Is.EqualTo("1,234.5"));
            Loc.Configure(LocSamples.Catalog(), "es-419", true, null, "CO");
            Assert.That(Loc.Number(1234.5, 1), Is.EqualTo("1.234,5"));
        }

        [Test]
        public void Loc_ChangingTheLanguage_KeepsTheCountry()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", true, null, "MX");
            Assert.That(Loc.Number(13040), Is.EqualTo("13,040"));
            Loc.SetLocale("pt-BR");
            Assert.That(Loc.Number(13040), Is.EqualTo("13.040"), "pt-BR siempre con punto");
            Loc.SetLocale("en");
            Assert.That(Loc.Number(13040), Is.EqualTo("13,040"));
            Loc.SetLocale("es-419");
            Assert.That(Loc.Number(13040), Is.EqualTo("13,040"), "vuelve al país");
            Assert.That(Loc.NumberCountry, Is.EqualTo("MX"));
        }

        [Test]
        public void Loc_SetNumberCountry_OnlyMattersInSpanish_AndAForcedRowWins()
        {
            Loc.Configure(LocSamples.Catalog(), "es-419", true, null, null);
            Assert.That(Loc.Number(1000), Is.EqualTo("1.000"), "sin país, el punto");
            Loc.SetNumberCountry("PE");
            Assert.That(Loc.Number(1000), Is.EqualTo("1,000"));
            Loc.SetNumberCountry("not a country");
            Assert.That(Loc.NumberCountry, Is.Null);
            Assert.That(Loc.Number(1000), Is.EqualTo("1.000"));

            Loc.Configure(LocSamples.Catalog(), "en", true, null, "AR");
            Assert.That(Loc.Number(1000), Is.EqualTo("1,000"), "en no mira el país");

            Loc.Configure(LocSamples.Catalog(), "es-419", true, "es-MX", "CO");
            Assert.That(Loc.Number(1000), Is.EqualTo("1,000"), "la fila forzada (tests y desarrollo) manda");
        }

        [Test]
        public void Prices_UseTheSameRow()
        {
            Loc.Configure(LocSamples.Catalog(), "pt-BR", true, null, "US");
            Assert.That(Loc.Price("COP", 5900), Is.EqualTo("COP 5.900"));
            Assert.That(Loc.Price("BRL", 499), Is.EqualTo("BRL 4,99"));
            Loc.Configure(LocSamples.Catalog(), "en", true, null, "CO");
            Assert.That(Loc.Price("COP", 5900), Is.EqualTo("COP 5,900"));
            Loc.Configure(LocSamples.Catalog(), "es-419", true, null, "CO");
            Assert.That(Loc.Price("COP", 5900), Is.EqualTo("COP 5.900"));
            Loc.Configure(LocSamples.Catalog(), "es-419", true, null, "MX");
            Assert.That(Loc.Price("MXN", 9900), Is.EqualTo("MXN 9,900"));
        }

        [TestCase("co", "en-US", "CO")]
        [TestCase(null, "es-MX", "MX")]
        [TestCase("", "es_ar", "AR")]
        [TestCase("XYZ", "pt-BR", "BR")]
        [TestCase(null, "es-419", null)]
        [TestCase(null, "pt", null)]
        [TestCase(null, null, null)]
        [TestCase(null, "en-US-u-mu-celsius", "US")]
        public void NumberCountry_PlayFirstThenThePhoneRegion(string? play, string? device, string? expected)
        {
            Assert.That(LocLocale.NumberCountry(play, device), Is.EqualTo(expected));
        }
    }
}
