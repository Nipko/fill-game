#nullable enable
using System;
using System.Collections.Generic;
using AF.EditorTools;
using AF.Localization;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Cobertura de las fuentes y glifos de los atlas (D-018; propuesta de idiomas §4.1 y §4.3; tarea B1):
    /// <list type="bullet">
    /// <item>todo carácter de las tablas de L2b (y de las cifras y la pseudolocalización) está en la TTF de su familia;</item>
    /// <item>las ramas de error de <see cref="FontCoverage.Analyze"/>: carácter que falta en una TTF, ↑/↓ fuera de los rótulos
    /// del Mapa y ✓ en el texto;</item>
    /// <item><see cref="FontSetup.Characters()"/> lleva ASCII, Latin-1, la puntuación de siempre y lo de las tablas, y Nunito no
    /// pide lo que solo se pinta en Baloo 2;</item>
    /// <item>los atlas generados (<c>Resources/AF/Fonts</c>) tienen todo lo que pide su familia: ningún □ en pantalla, tampoco
    /// en en-XA.</item>
    /// </list>
    /// </summary>
    public sealed class FontCoverageTests
    {
        [Test]
        public void ProjectTables_EveryCharacterIsInTheTtfOfItsFamily()
        {
            IReadOnlyList<LocTextEntry> texts = FontCoverage.ReadProjectTables();
            FontCoverageReport report = FontCoverage.Analyze(texts, new FontCoverage.TtfGlyphs().Has);

            Assert.That(report.Texts, Is.GreaterThan(150), "celdas con texto en es-419, en y pt-BR");
            Assert.That(report.Errors, Is.Empty, string.Join("\n", report.Errors));
            Assert.That(report.BodyCharacters, Does.Contain("ñ").And.Contain("ç").And.Contain("ã"));
        }

        [Test]
        public void ProjectAtlases_HaveEveryCharacterTheirFamilyNeeds_NoMissingGlyphBox()
        {
            FontCoverageReport report = FontCoverage.Analyze(FontCoverage.ReadProjectTables(), new FontCoverage.TtfGlyphs().Has);

            List<string> gaps = FontSetup.AtlasGaps(report);

            Assert.That(gaps, Is.Empty, string.Join("\n", gaps));
        }

        [Test]
        public void TablePaths_AreTheOnesTheGameLoads()
        {
            Assert.That(FontCoverage.StringsPath, Is.EqualTo(LocTables.StringsAssetPath));
            Assert.That(FontCoverage.FactsPath, Is.EqualTo(LocTables.FactsAssetPath));
        }

        [Test]
        public void Ttfs_NunitoLacksTheRightArrow_Baloo2HasTheMapArrows()
        {
            // Por qué double_harvest_delta («+20 → +40», CtaWater) va en TitleOnlyKeys y los rótulos del Mapa en Baloo 2.
            var glyphs = new FontCoverage.TtfGlyphs();
            foreach (string font in FontCoverage.BodyFonts)
            {
                Assert.That(glyphs.Has(font, '→'), Is.False, font + " y «→»");
            }

            foreach (string font in FontCoverage.TitleFonts)
            {
                Assert.That(glyphs.Has(font, '→'), Is.True, font + " y «→»");
                Assert.That(glyphs.Has(font, FontCoverage.ArrowUp), Is.True, font + " y «↑»");
                Assert.That(glyphs.Has(font, FontCoverage.ArrowDown), Is.True, font + " y «↓»");
                Assert.That(glyphs.Has(font, FontCoverage.CheckMark), Is.False, font + " y «✓» (es un icono)");
            }
        }

        [Test]
        public void MissingCharacter_FailsAndListsFontCharacterAndText()
        {
            var texts = new[]
            {
                new LocTextEntry("strings.csv", 7, "zz_test", LocLocale.English, "Agua 水"),
            };

            FontCoverageReport report = FontCoverage.Analyze(texts, new FontCoverage.TtfGlyphs().Has);

            Assert.That(report.Ok, Is.False);
            Assert.That(report.Errors, Has.Count.EqualTo(FontCoverage.BodyFonts.Length + FontCoverage.TitleFonts.Length), string.Join("\n", report.Errors));
            Assert.That(report.Errors, Has.Some.EqualTo("Nunito-Regular.ttf no tiene «水» (U+6C34), que usa strings.csv:7 zz_test (en)."));
            Assert.That(report.Errors, Has.Some.StartsWith("Baloo2-Bold.ttf no tiene «水» (U+6C34)"));
        }

        [Test]
        public void MissingCharacter_InOneFamilyOnly_FailsOnlyForThatFamily()
        {
            // «→» fuera de las claves de Baloo 2: falta en las tres Nunito.
            var texts = new[]
            {
                new LocTextEntry("strings.csv", 3, "zz_arrow", LocLocale.SpanishLatinAmerica, "A → B"),
                new LocTextEntry("strings.csv", 4, "double_harvest_delta", LocLocale.SpanishLatinAmerica, "+{n} → +{doubled}"),
            };

            FontCoverageReport report = FontCoverage.Analyze(texts, new FontCoverage.TtfGlyphs().Has);

            Assert.That(report.Errors, Has.Count.EqualTo(FontCoverage.BodyFonts.Length), string.Join("\n", report.Errors));
            Assert.That(report.Errors, Has.All.Contains("«→» (U+2192), que usa strings.csv:3 zz_arrow (es-419)."));
            Assert.That(report.Errors, Has.None.Contains("double_harvest_delta"));
            Assert.That(report.Errors, Has.None.StartsWith("Baloo2"));
        }

        [Test]
        public void ArrowsInTheMapLabels_GoInBaloo2Only()
        {
            var texts = new[]
            {
                new LocTextEntry("strings.csv", 2, "map_label_andes", LocLocale.SpanishLatinAmerica, "Andes ↑"),
                new LocTextEntry("strings.csv", 3, "map_label_atlantic", LocLocale.SpanishLatinAmerica, "Océano Atlántico ↓"),
            };

            FontCoverageReport report = FontCoverage.Analyze(texts, new FontCoverage.TtfGlyphs().Has);

            Assert.That(report.Errors, Is.Empty, string.Join("\n", report.Errors));
            Assert.That(report.TitleCharacters, Does.Contain("↑").And.Contain("↓"));
            Assert.That(report.BodyCharacters, Does.Not.Contain("↑").And.Not.Contain("↓"));
            Assert.That(FontCoverage.FamilyOf("map_label_andes"), Is.EqualTo(GlyphFamily.Title));
            Assert.That(FontCoverage.FamilyOf("play_level"), Is.EqualTo(GlyphFamily.Both));
        }

        [Test]
        public void ArrowsOutsideTheMapLabels_Fail()
        {
            var texts = new[]
            {
                new LocTextEntry("facts.csv", 9, "map.stop.manaos.dato_01", LocLocale.SpanishLatinAmerica, "Río ↑ arriba"),
                new LocTextEntry("strings.csv", 5, "double_harvest_delta", LocLocale.English, "+{n} ↓"),
            };

            FontCoverageReport report = FontCoverage.Analyze(texts, (font, c) => true);

            Assert.That(report.Errors, Has.Count.EqualTo(2), string.Join("\n", report.Errors));
            Assert.That(report.Errors[0], Is.EqualTo("facts.csv:9 map.stop.manaos.dato_01 (es-419): «↑» (U+2191) solo va en map_label_andes y map_label_atlantic (glosario §10, P-M2)."));
            Assert.That(report.Errors[1], Does.StartWith("strings.csv:5 double_harvest_delta (en): «↓» (U+2193) solo va en"));
        }

        [Test]
        public void CheckMark_NeverInTheText_EvenInAMapLabel()
        {
            var texts = new[]
            {
                new LocTextEntry("strings.csv", 6, "map_world_question", LocLocale.SpanishLatinAmerica, "¿De dónde viene el agua? ✓"),
                new LocTextEntry("strings.csv", 7, "map_label_andes", LocLocale.SpanishLatinAmerica, "Andes ✓"),
            };

            FontCoverageReport report = FontCoverage.Analyze(texts, (font, c) => true);

            Assert.That(report.Errors, Has.Count.EqualTo(2), string.Join("\n", report.Errors));
            Assert.That(report.Errors, Has.All.Contains("«✓» (U+2713) nunca va en el texto: es un icono procedural (M3.3)."));
            Assert.That(report.TitleCharacters, Does.Not.Contain("✓"));
        }

        [Test]
        public void Analyze_SkipsLineBreaks_AndAddsNumberAndPseudoCharacters()
        {
            var texts = new[] { new LocTextEntry("strings.csv", 2, "zz", LocLocale.SpanishLatinAmerica, "a\nb") };

            FontCoverageReport report = FontCoverage.Analyze(texts, (font, c) => true);

            Assert.That(report.BodyCharacters, Does.Not.Contain("\n"));
            foreach (char c in LocNumber.Characters + LocPseudo.Characters)
            {
                Assert.That(report.BodyCharacters.IndexOf(c), Is.GreaterThanOrEqualTo(0), "cuerpo: U+" + ((int)c).ToString("X4"));
                Assert.That(report.TitleCharacters.IndexOf(c), Is.GreaterThanOrEqualTo(0), "títulos: U+" + ((int)c).ToString("X4"));
            }
        }

        [Test]
        public void Glossary_LocaleColumnsCount_OtherColumnsDoNot()
        {
            IReadOnlyList<LocCsvRow> rows = LocCsv.Parse("term,type,es-419,en,pt-BR,notes\nacai,fruta,açaí,açaí,açaí,Ж\n", FontCoverage.GlossaryFile);
            var texts = new List<LocTextEntry>();

            FontCoverage.AddGlossary(texts, rows, FontCoverage.GlossaryFile);

            Assert.That(texts, Has.Count.EqualTo(3));
            Assert.That(texts[0].Where, Is.EqualTo("glossary.csv:2 acai (es-419)"));
            Assert.That(texts, Has.None.Matches<LocTextEntry>(t => t.Text.Contains("Ж")), "las notas no van en los atlas");
        }

        [Test]
        public void Characters_HasAsciiLatin1TypographyAndTheTables_BodyWithoutTitleOnlyCharacters()
        {
            string all = FontSetup.Characters();
            string body = FontSetup.Characters(GlyphFamily.Body);
            string title = FontSetup.Characters(GlyphFamily.Title);

            Assert.That(title, Is.EqualTo(all));
            for (int c = 0x20; c <= 0x7E; c++)
            {
                Assert.That(body.IndexOf((char)c), Is.GreaterThanOrEqualTo(0), "ASCII U+" + c.ToString("X4"));
            }

            for (int c = 0xA0; c <= 0xFF; c++)
            {
                Assert.That(body.IndexOf((char)c), Is.GreaterThanOrEqualTo(0), "Latin-1 U+" + c.ToString("X4"));
            }

            Assert.That(body, Does.Contain(FontSetup.Typography));
            Assert.That(all, Does.Contain("→"), "double_harvest_delta");
            Assert.That(body, Does.Not.Contain("→"), "a Nunito le falta «→» y solo lo pinta Baloo 2");
            Assert.That(new HashSet<char>(all).Count, Is.EqualTo(all.Length), "sin repetir");
            Assert.That(FontSetup.Characters(), Is.EqualTo(all), "determinista");

            foreach (LocTextEntry entry in FontCoverage.ReadProjectTables())
            {
                string atlas = FontCoverage.FamilyOf(entry.Key) == GlyphFamily.Title ? title : body;
                foreach (char c in entry.Text)
                {
                    if (!char.IsControl(c))
                    {
                        Assert.That(atlas.IndexOf(c), Is.GreaterThanOrEqualTo(0), entry.Where + ": U+" + ((int)c).ToString("X4"));
                    }
                }
            }
        }

        [Test]
        public void PseudoLocalized_Tables_UseOnlyCharactersOfTheAtlases()
        {
            // en-XA: el español con acentos de Latin-1, corchetes y relleno; ningún □.
            string body = FontSetup.Characters(GlyphFamily.Body);
            string title = FontSetup.Characters(GlyphFamily.Title);
            int checkedTexts = 0;
            foreach (LocTextEntry entry in FontCoverage.ReadProjectTables())
            {
                if (entry.Locale != LocLocale.SourceLocale)
                {
                    continue;
                }

                string atlas = FontCoverage.FamilyOf(entry.Key) == GlyphFamily.Title ? title : body;
                string pseudo = LocPseudo.Transform(entry.Text);
                foreach (char c in pseudo)
                {
                    if (!char.IsControl(c))
                    {
                        Assert.That(atlas.IndexOf(c), Is.GreaterThanOrEqualTo(0), entry.Where + " en en-XA: U+" + ((int)c).ToString("X4"));
                    }
                }

                checkedTexts++;
            }

            Assert.That(checkedTexts, Is.GreaterThan(100));
        }
    }
}
