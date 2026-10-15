#nullable enable
using AF.Localization;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Resolución del idioma (L2a; D-018, Q7-A; propuesta de idiomas §3.5): desarrollo → guardado → teléfono → en, con es-* →
    /// es-419, pt-* → pt-BR y el resto → en; solo idiomas con la UI completa (<c>IsComplete</c>) y, si ninguno vale, es-419
    /// [P]. en-XA solo con <c>showUnapproved</c>.
    /// </summary>
    public sealed class LocLocaleTests
    {
        private const string Header = "key,es-419,en,pt-BR\n";

        /// <summary>Hoy: solo es-419 completo (notas de la tanda 4, punto 10).</summary>
        private static LocCatalog SpanishOnly() => LocCatalog.Parse(Header + "next,Siguiente,,\nplay,Jugar,Play,\n", null);

        /// <summary>es-419 y en completos; pt-BR a medias.</summary>
        private static LocCatalog SpanishAndEnglish() => LocCatalog.Parse(Header + "next,Siguiente,Next,\nplay,Jugar,Play,Jogar\n", null);

        /// <summary>Los tres completos.</summary>
        private static LocCatalog AllThree() => LocCatalog.Parse(Header + "next,Siguiente,Next,Próximo\nplay,Jugar,Play,Jogar\n", null);

        [TestCase("es-CO", "es-419")]
        [TestCase("es", "es-419")]
        [TestCase("es-419", "es-419")]
        [TestCase("es_MX", "es-419")]
        [TestCase("ES-ar", "es-419")]
        [TestCase("pt-BR", "pt-BR")]
        [TestCase("pt-PT", "pt-BR")]
        [TestCase("pt", "pt-BR")]
        [TestCase("en-US", "en")]
        [TestCase("en", "en")]
        [TestCase("fr-FR", "en")]
        [TestCase("ja", "en")]
        [TestCase("und", "en")]
        [TestCase("en-XA", "en-XA")]
        [TestCase("en_xa", "en-XA")]
        [TestCase("auto", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void Map_FollowsQ7A(string? tag, string? expected)
        {
            Assert.That(LocLocale.Map(tag), Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_Order_DevThenSavedThenDeviceThenEnglish()
        {
            LocCatalog catalog = SpanishAndEnglish();
            Check(LocLocale.Resolve(catalog, "es-419", "en", "en-US", false), "es-419", LocSource.Dev);
            Check(LocLocale.Resolve(catalog, null, "en", "es-CO", false), "en", LocSource.Saved);
            Check(LocLocale.Resolve(catalog, null, "es-419", "en-US", false), "es-419", LocSource.Saved);
            Check(LocLocale.Resolve(catalog, null, "auto", "es-MX", false), "es-419", LocSource.Device);
            Check(LocLocale.Resolve(catalog, null, null, "fr-FR", false), "en", LocSource.Device);
            Check(LocLocale.Resolve(catalog, null, null, null, false), "en", LocSource.Default);
        }

        [Test]
        public void Resolve_SkipsIncompleteLanguages_AndFallsBackToEs419()
        {
            LocCatalog english = SpanishAndEnglish();
            Check(LocLocale.Resolve(english, "pt-BR", null, null, false), "en", LocSource.Default, "pt-BR incompleto: pasa al siguiente");
            Check(LocLocale.Resolve(english, null, "pt-BR", "es-AR", false), "es-419", LocSource.Device);
            Check(LocLocale.Resolve(english, null, null, "pt-BR", false), "en", LocSource.Default);

            LocCatalog spanish = SpanishOnly();
            Check(LocLocale.Resolve(spanish, null, null, null, false), "es-419", LocSource.Fallback);
            Check(LocLocale.Resolve(spanish, null, null, "en-US", false), "es-419", LocSource.Fallback,
                "mientras EN no esté completo, el automático se queda en es-419");
            Check(LocLocale.Resolve(spanish, null, null, "es-CO", false), "es-419", LocSource.Device);

            Check(LocLocale.Resolve(AllThree(), null, null, "pt-PT", false), "pt-BR", LocSource.Device, "un solo portugués (Q6-A)");
        }

        [Test]
        public void Resolve_PseudoOnlyWithShowUnapproved()
        {
            LocCatalog catalog = SpanishOnly();
            Check(LocLocale.Resolve(catalog, "en-XA", null, null, true), "en-XA", LocSource.Dev);
            Check(LocLocale.Resolve(catalog, "en-XA", null, "es-CO", false), "es-419", LocSource.Device, "en release, en-XA no vale");
            Check(LocLocale.Resolve(catalog, null, "en-XA", null, true), "en-XA", LocSource.Saved, "elegible en Ajustes en desarrollo");
        }

        [Test]
        public void Offered_IsTheCompleteLanguages_PlusPseudoInDevelopment()
        {
            Assert.That(LocLocale.Offered(SpanishOnly(), false), Is.EqualTo(new[] { "es-419" }));
            Assert.That(LocLocale.Offered(SpanishOnly(), true), Is.EqualTo(new[] { "es-419", "en-XA" }));
            Assert.That(LocLocale.Offered(SpanishAndEnglish(), false), Is.EqualTo(new[] { "es-419", "en" }));
            Assert.That(LocLocale.Offered(AllThree(), false), Is.EqualTo(new[] { "es-419", "en", "pt-BR" }));
        }

        [Test]
        public void IsComplete_EveryUiKeyNeedsText_AndPseudoFollowsTheSource()
        {
            LocCatalog catalog = SpanishAndEnglish();
            Assert.That(catalog.IsComplete("es-419"), Is.True);
            Assert.That(catalog.IsComplete("en"), Is.True);
            Assert.That(catalog.IsComplete("pt-BR"), Is.False, "una clave sin texto basta");
            Assert.That(catalog.IsComplete("fr"), Is.False, "sin columna");
            Assert.That(catalog.IsComplete("en-XA"), Is.True, "en-XA sale del español");
            Assert.That(LocCatalog.Parse("key,es-419,en\nnext,Siguiente,Next\nplay,,Play\n", null).IsComplete("en-XA"), Is.False);
            Assert.That(LocLocale.IsOffered(catalog, "en-XA", false), Is.False);
            Assert.That(LocLocale.IsOffered(catalog, "en-XA", true), Is.True);
        }

        [Test]
        public void DefaultNumberTag_IsEsCoForSpanishWithoutRegion()
        {
            Assert.That(LocLocale.DefaultNumberTag("es-419"), Is.EqualTo("es-CO"), "[P] lo de hoy: «1.000»");
            Assert.That(LocLocale.DefaultNumberTag("en-XA"), Is.EqualTo("es-CO"), "en-XA es el español falseado");
            Assert.That(LocLocale.DefaultNumberTag("pt-BR"), Is.EqualTo("pt-BR"));
            Assert.That(LocLocale.DefaultNumberTag("en"), Is.EqualTo("en"));
        }

        [Test]
        public void Tags_LanguageAndRegion_SkipScriptsAndExtensions()
        {
            Assert.That(LocLocale.HasLanguage("es-CO", "es"), Is.True);
            Assert.That(LocLocale.HasLanguage("est-EE", "es"), Is.False);
            Assert.That(LocLocale.HasLanguage("es", "es"), Is.True);
            Assert.That(LocLocale.HasRegion("zh-Hant-TW", "TW"), Is.True, "salta el script");
            Assert.That(LocLocale.HasRegion("es-419", "419"), Is.True);
            Assert.That(LocLocale.HasRegion("pt_pt", "PT"), Is.True);
            Assert.That(LocLocale.HasRegion("en-u-ca-gregory", "CA"), Is.False, "una extensión no es la región");
            Assert.That(LocLocale.HasAnyRegion("es-MX") && LocLocale.HasAnyRegion("es-419") && LocLocale.HasAnyRegion("zh-Hant-TW"), Is.True);
            Assert.That(LocLocale.HasAnyRegion("es") || LocLocale.HasAnyRegion("pt") || LocLocale.HasAnyRegion("en-u-ca-gregory"), Is.False, "sin región");
            Assert.That(LocLocale.HasAnyRegion(null) || LocLocale.HasAnyRegion(string.Empty), Is.False);
            Assert.That(LocLocale.IsPseudo("en-XA"), Is.True);
            Assert.That(LocLocale.IsPseudo("en-US"), Is.False);
        }

        private static void Check(LocResolution resolution, string locale, LocSource source, string? message = null)
        {
            Assert.That(resolution.Locale, Is.EqualTo(locale), message);
            Assert.That(resolution.Source, Is.EqualTo(source), message);
        }
    }
}
