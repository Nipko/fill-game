#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using AF.EditorTools;
using AF.Levels;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Textos del sistema del Mapa del río (D-013; glosario §10; P-M2 de D-023; tarea M1.5) en <c>strings.csv</c> y su acceso
    /// tipado (<c>UiText.Map.cs</c>):
    /// <list type="bullet">
    /// <item>las 22 claves (las 11 del glosario, las 10 de P-M2 y la naciente de FX13) y ninguna <c>map_*</c> más, todas <c>pendiente_asesor</c>,
    /// solo en ES, con contexto y <c>max_chars</c>;</item>
    /// <item>el plural de <c>map_stop_remaining</c> con 0, 1, 2 y 21, y las cifras con la región de <see cref="Loc"/>;</item>
    /// <item>las longitudes: los textos con las 70 paradas reales (1-700, M4.5b) caben en su <c>max_chars</c>, la presentación
    /// del 10 en 50 caracteres y los nombres de las 70 paradas en 2 líneas a 296 dp;</item>
    /// <item>↑ y ↓ solo en los dos rótulos de orientación, que se pintan en Baloo 2; y el acceso tipado sin <c>PluralEs</c>,
    /// <c>Grouped</c>, <c>string.Format</c> ni <c>Replace</c>.</item>
    /// </list>
    /// </summary>
    public sealed class MapTextTests
    {
        /// <summary>Las 11 claves del glosario §10 para el Mapa (D-013; propuesta del Mapa §6.4).</summary>
        private static readonly string[] GlossaryKeys =
        {
            "map_stop_remaining", "map_arrived", "map_home_distance", "map_country_welcome", "map_world_question", "map_next_world",
            "map_samples", "map_north_up", "map_back_to_canoe", "map_hito_preview", "stop_label",
        };

        /// <summary>Las 10 claves de P-M2 (D-023, punto 13).</summary>
        private static readonly string[] PM2Keys =
        {
            "map_view_world", "map_view_basin", "map_label_andes", "map_label_atlantic", "map_back_to_level", "map_open",
            "map_card_other_fact", "map_card_source", "map_facts_count", "map_l10_intro",
        };

        /// <summary>La de FX13: la naciente del último tramo (D-013, viñeta del 2026-10-19).</summary>
        private static readonly string[] Fx13Keys = { "map_source_name" };

        private static RiverStopCatalog? s_stops;
        private LocStateSnapshot _loc;

        private static string RiverStopsPath => Path.Combine(Application.dataPath, "_Project", "Data", "Map", "river_stops_v1.json");

        private static string MapTextSource => Path.Combine(Application.dataPath, "_Project", "Scripts", "UI", "UiText.Map.cs");

        private static RiverStopCatalog Stops => s_stops ??= RiverStopCatalogReader.Read(File.ReadAllText(RiverStopsPath, Encoding.UTF8));

        [SetUp]
        public void SetUp()
        {
            _loc = LocStateSnapshot.Take();
            LocSetUpFixture.ConfigureEs419();
        }

        [TearDown]
        public void TearDown()
        {
            _loc.Restore();
        }

        [Test]
        public void Keys_AreTheElevenOfTheGlossary_TheTenOfPM2_AndTheSourceOfFx13_AndNoOtherMapKey()
        {
            var expected = new List<string>(GlossaryKeys);
            expected.AddRange(PM2Keys);
            expected.AddRange(Fx13Keys);
            expected.Sort(StringComparer.Ordinal);
            Assert.That(expected, Has.Count.EqualTo(22));
            Assert.That(new HashSet<string>(expected), Has.Count.EqualTo(22), "sin repetidas");

            LocTable strings = LocSetUpFixture.Catalog.Strings;
            var found = new List<string>();
            for (int r = 0; r < strings.Count; r++)
            {
                string key = strings.KeyAt(r);
                if (key.StartsWith("map_", StringComparison.Ordinal) || key == "stop_label")
                {
                    found.Add(key);
                }
            }

            Assert.That(found, Is.EqualTo(expected), "las 22 claves del Mapa, en orden ordinal, y ninguna map_* más");

            var typed = new List<string>();
            foreach (FieldInfo f in typeof(UiText.Keys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsLiteral && f.GetValue(null) is string key && (key.StartsWith("map_", StringComparison.Ordinal) || key == "stop_label"))
                {
                    typed.Add(key);
                }
            }

            typed.Sort(StringComparer.Ordinal);
            Assert.That(typed, Is.EqualTo(expected), "UiText.Keys tiene una constante por clave del Mapa");
        }

        [Test]
        public void EveryMapText_IsPendingTheAdvisor_InSpanishOnly_WithContextAndMaxChars()
        {
            LocTable strings = LocSetUpFixture.Catalog.Strings;
            foreach (string key in AllKeys())
            {
                int r = strings.IndexOf(key);
                Assert.That(r, Is.GreaterThanOrEqualTo(0), key);
                Assert.That(strings.StatusAt(r, LocLocale.SourceLocale), Is.EqualTo(LocStatus.PendingAdvisor), key + ": nace pendiente_asesor");
                Assert.That(strings.ContextAt(r), Is.Not.Empty, key);
                Assert.That(strings.MaxCharsAt(r), Is.GreaterThan(0), key);
                foreach (string locale in strings.Locales)
                {
                    if (locale != LocLocale.SourceLocale)
                    {
                        // Sin traducir (vacío y sin estado) o con la traducción con IA de T2 (traducido; D-022, pregunta 51 (b)),
                        // que espera la revisión nativa y la del asesor.
                        bool translated = strings.TextAt(r, locale).Length > 0;
                        Assert.That(strings.StatusAt(r, locale), translated ? Is.EqualTo(LocStatus.Translated) : Is.EqualTo(LocStatus.Missing), key + " (" + locale + ")");
                    }
                }
            }
        }

        [TestCase(0, "Faltan 0 niveles para Santarém")]
        [TestCase(1, "Falta 1 nivel para Santarém")]
        [TestCase(2, "Faltan 2 niveles para Santarém")]
        [TestCase(21, "Faltan 21 niveles para Santarém")]
        public void StopRemaining_HasTheSpanishPlural_NeverOneNiveles(int levels, string expected)
        {
            RiverStop santarem = Stops.At(1)!;
            Assert.That(UiText.MapStopRemaining(levels, santarem), Is.EqualTo(expected));
            Assert.That(UiText.MapStopRemaining(levels, "Santarém"), Is.EqualTo(expected));
        }

        [Test]
        public void StopRemaining_LevelsGoWithoutThousandsSeparator()
        {
            Assert.That(UiText.MapStopRemaining(1000, "Iquitos"), Is.EqualTo("Faltan 1000 niveles para Iquitos"));
            Assert.That(UiText.MapStopRemaining(-3, "Iquitos"), Is.EqualTo("Faltan 0 niveles para Iquitos"), "un negativo se escribe 0");
        }

        // FX13 (D-013, viñeta del 2026-10-19): tras la última parada (Tambo, 691-700), «Faltan N» va hacia la naciente del Mismi.
        [Test]
        public void NextPlace_IsTheNextStop_AndAfterTheLastStop_TheSourceOfTheMismi()
        {
            RiverStopCatalog stops = Stops;
            Assert.That(stops.At(69)!.AtLine, Is.EqualTo(RiverMap.CauceLineId), "Tambo, en el cauce (at.line)");
            Assert.That(stops.HeadsToSource, Is.True, "tras Tambo, la naciente");
            Assert.That(UiText.NextPlaceName(stops, stops.StopOf(685)), Is.EqualTo("El Tambo"), "681-690: hacia Tambo (69)");
            Assert.That(UiText.NextPlaceName(stops, stops.StopOf(690)), Is.EqualTo("El Tambo"));
            Assert.That(UiText.MapSourceName, Is.EqualTo("El nevado Mismi"));
            Assert.That(UiText.NextPlaceName(stops, stops.StopOf(691)), Is.EqualTo(UiText.MapSourceName), "691-700: hacia la naciente");
            Assert.That(UiText.NextPlaceName(stops, stops.StopOf(700)), Is.EqualTo(UiText.MapSourceName));
            Assert.That(UiText.NextPlaceName(stops, null), Is.Empty, "sin parada en juego");

            // «Faltan N» = 10 · (k + 1) − p + 1 (RiverJourney.LevelsLeft): 6 en el 695 y 1 en el 700.
            Assert.That(UiText.MapStopRemaining(6, UiText.NextPlaceName(stops, stops.StopOf(695))), Is.EqualTo("Faltan 6 niveles para El nevado Mismi"));
            Assert.That(UiText.MapStopRemaining(1, UiText.NextPlaceName(stops, stops.StopOf(700))), Is.EqualTo("Falta 1 nivel para El nevado Mismi"));

            // Un catálogo que acaba fuera del cauce (las paradas 0-19, la última en el Marañón): sin meta y sin «Faltan…» tras ella,
            // como el Mapa, que no tiene ruta hasta la naciente (RiverMapReader.SourceGoal).
            var first20 = new RiverStop[20];
            for (int k = 0; k < first20.Length; k++)
            {
                first20[k] = stops.At(k)!;
            }

            var truncated = new RiverStopCatalog("test", first20);
            Assert.That(truncated.At(19)!.AtLine, Is.EqualTo("maranon"));
            Assert.That(truncated.HeadsToSource, Is.False);
            Assert.That(UiText.NextPlaceName(truncated, truncated.StopOf(195)), Is.Empty);
            Assert.That(UiText.NextPlaceName(truncated, truncated.StopOf(185)), Is.EqualTo(UiText.StopName(truncated.At(19)!)));
        }

        // Revisión de FX13: el fin de contenido del 700 abre en el 701 (mundo 10, sin paradas en el Mapa); la cabecera nombra el
        // mundo de la última parada (9, Ucayali), solo, nunca el mundo 10.
        [Test]
        public void SourceTitle_BeyondTheLastStop_NamesTheLastStopWorld()
        {
            RiverStopCatalog stops = Stops;
            Assert.That(stops.At(stops.Count - 1)!.World, Is.EqualTo(9), "premisa: Tambo, en el mundo 9");
            Assert.That(RiverMapScreen.SourceTitleWorld(stops, 10), Is.EqualTo(9), "el 701, más allá de Tambo");
            Assert.That(RiverMapScreen.SourceTitleWorld(stops, 9), Is.EqualTo(0), "el mundo 9 tiene paradas: su pregunta, como siempre");
            Assert.That(RiverMapScreen.SourceTitleWorld(stops, 1), Is.EqualTo(0));
            Assert.That(UiText.WorldName(9), Is.EqualTo("Ucayali"));

            // Sin meta (la última parada fuera del cauce), nada cambia.
            var first20 = new RiverStop[20];
            for (int k = 0; k < first20.Length; k++)
            {
                first20[k] = stops.At(k)!;
            }

            var truncated = new RiverStopCatalog("test", first20);
            Assert.That(RiverMapScreen.SourceTitleWorld(truncated, 3), Is.EqualTo(0));
        }

        [Test]
        public void Texts_FillTheirNamedMarkers_WithTheNamesOfFacts()
        {
            RiverStop alter = Stops.At(2)!;
            Assert.That(alter.Id, Is.EqualTo("alter_do_chao"));
            Assert.That(UiText.StopName(alter), Is.EqualTo("Alter do Chão"));
            Assert.That(UiText.StopLabel(alter), Is.EqualTo("Alter do Chão · Brasil"));
            Assert.That(UiText.MapArrived(alter), Is.EqualTo("Alter do Chão · Brasil · río\u00A0Tapajós"), "el criterio de M1.6 al ganar el 20 (espacio duro tras «río»)");
            Assert.That(UiText.MapArrivedSeparators(), Is.EqualTo(2), "map_arrived se parte por sus dos {sep} (revisión de M1.6)");
            Assert.That(UiText.MapArrived("Alter do Chão", "Brasil", "Tapajós", 0b10), Is.EqualTo("Alter do Chão · Brasil\nrío\u00A0Tapajós"),
                "partida por el último corte: sin « · » colgando");
            Assert.That(UiText.MapArrived("Alter do Chão", "Brasil", "Tapajós", 0b11), Is.EqualTo("Alter do Chão\nBrasil\nrío\u00A0Tapajós"));
            Assert.That(UiText.MapHitoPreview(alter), Is.EqualTo("Si lo superas, llegas a Alter do Chão"));

            RiverStop leticia = Stops.At(8)!;
            Assert.That(UiText.MapCountryWelcome(leticia), Is.EqualTo("Colombia te da la bienvenida"), "P-M7, sin género");
            RiverStop frontier = Stops.At(13)!;
            Assert.That(UiText.StopLabel(frontier), Is.EqualTo("El río frontera · Colombia y Perú"), "el país de la parada 13 (country_name)");

            Assert.That(UiText.MapWorldQuestion(2, "¿Por qué el agua del Tapajós es clara?"), Is.EqualTo(UiText.WorldName(2) + " · ¿Por qué el agua del Tapajós es clara?"));
            Assert.That(UiText.MapNextWorld(6, 201), Is.EqualTo("Mundo 6 · " + UiText.WorldName(6) + " · Nivel 201"));
            Assert.That(UiText.MapNextWorld(13, 1000), Does.EndWith(" · Nivel 1000"), "el nivel, sin separador de miles");
            Assert.That(UiText.MapCardSource("Ramsar"), Is.EqualTo("Fuente: Ramsar"));
            Assert.That(UiText.MapNorthUp, Is.EqualTo("Norte arriba"));
            Assert.That(UiText.MapBackToCanoe, Is.EqualTo("Volver a la canoa"));
            Assert.That(UiText.MapViewWorld, Is.EqualTo("Mundo"));
            Assert.That(UiText.MapViewBasin, Is.EqualTo("Cuenca"));
            Assert.That(UiText.MapBackToLevel, Is.EqualTo("Volver al nivel"));
            Assert.That(UiText.MapOpen, Is.EqualTo("Ver mapa"));
            Assert.That(UiText.MapCardOtherFact, Is.EqualTo("Otro dato"));
            Assert.That(UiText.MapL10Intro, Is.EqualTo("Yo vivo aquí, donde el río entra al mar."));
            Assert.That(UiText.MapLabelAndes, Is.EqualTo("Andes ↑"));
            Assert.That(UiText.MapLabelAtlantic, Is.EqualTo("Océano Atlántico ↓"));
        }

        [Test]
        public void Numbers_FollowTheRegionOfLoc_WithAHardSpaceBeforeKm()
        {
            Assert.That(UiText.MapHomeDistance(6575), Is.EqualTo("Casa · a unos 6.575 km"), "sin región del teléfono, es-CO");
            Assert.That(UiText.MapSamples(3, 20), Is.EqualTo("Muestrario · 3/20"));
            Assert.That(UiText.MapFactsCount(1000, 1200), Is.EqualTo("Datos del río · 1.000/1.200"));
            Loc.SetNumberTag("es-MX");
            Assert.That(UiText.MapHomeDistance(6575), Is.EqualTo("Casa · a unos 6,575 km"));
            Assert.That(UiText.MapFactsCount(1000, 1200), Is.EqualTo("Datos del río · 1,000/1,200"));
            Assert.That(UiText.MapHomeDistance(-1), Is.EqualTo("Casa · a unos 0 km"));
        }

        [Test]
        public void Lengths_TheTextsOfTheStopsFitTheirMaxChars()
        {
            Assert.That(Stops.Count, Is.EqualTo(70), "paradas 0-69 de los niveles 1-700 (M4.5b)");
            var over = new StringBuilder();
            for (int n = 0; n < Stops.Count; n++)
            {
                RiverStop stop = Stops.At(n)!;
                Assert.That(UiText.StopName(stop), Is.Not.Empty, stop.NameKey);
                Assert.That(UiText.StopCountry(stop), Is.Not.Empty, stop.CountryNameKeyOrDefault);
                Assert.That(UiText.StopRiver(stop), Is.Not.Empty, stop.RiverKey);
                Fits(over, UiText.Keys.StopLabel, UiText.StopLabel(stop));
                Fits(over, UiText.Keys.MapArrived, UiText.MapArrived(stop));
                Fits(over, UiText.Keys.MapCountryWelcome, UiText.MapCountryWelcome(stop));
                Fits(over, UiText.Keys.MapHitoPreview, UiText.MapHitoPreview(stop));
                Fits(over, UiText.Keys.MapStopRemaining, UiText.MapStopRemaining(RiverStopCatalog.LevelsPerStop, stop));
                Fits(over, UiText.Keys.MapHomeDistance, UiText.MapHomeDistance(stop.KmHome));
            }

            for (int w = 1; w <= WorldTable.WorldCount; w++)
            {
                Fits(over, UiText.Keys.MapNextWorld, UiText.MapNextWorld(w, 1000));
                Fits(over, UiText.Keys.MapWorldQuestion, UiText.MapWorldQuestion(w, new string('x', RiverStopCatalog.QuestionMaxChars)));
            }

            Assert.That(over.ToString(), Is.Empty);
            Assert.That(UiText.MapL10Intro.Length, Is.LessThanOrEqualTo(50), "la presentación del 10, ≤ 50 caracteres (06 §2.3)");
        }

        [Test]
        public void StopNames_FitInTwoLinesAt296dp()
        {
            var report = new StringBuilder();
            int widest = 0;
            for (int n = 0; n < Stops.Count; n++)
            {
                string name = UiText.StopName(Stops.At(n)!);
                int lines = UiText.LineCount(name, TextStyle.ModalTitle, UiText.StopNameSizeSp, UiText.StopNameWidthDp);
                Assert.That(lines, Is.GreaterThan(0), "con la fuente del atlas (Resources/AF/Fonts)");
                widest = Math.Max(widest, lines);
                if (lines > UiText.StopNameMaxLines)
                {
                    report.Append(name).Append(": ").Append(lines).Append(" líneas\n");
                }
            }

            Assert.That(report.ToString(), Is.Empty, "a " + UiText.StopNameWidthDp + " dp");
            Assert.That(widest, Is.LessThanOrEqualTo(UiText.StopNameMaxLines));
        }

        [Test]
        public void LineCount_BreaksByWords_AndCountsTheLineBreaks()
        {
            Assert.That(UiText.LineCount("Manaos", TextStyle.ModalTitle, 20f, 296f), Is.EqualTo(1));
            Assert.That(UiText.LineCount("Manaos\nIquitos", TextStyle.ModalTitle, 20f, 296f), Is.EqualTo(2));
            const string name = "Floresta Nacional do Tapajós";
            int wide = UiText.LineCount(name, TextStyle.ModalTitle, 20f, 296f);
            Assert.That(UiText.LineCount(name, TextStyle.ModalTitle, 20f, 120f), Is.GreaterThan(wide), "más estrecho, más líneas");
            Assert.That(UiText.LineCount(name, TextStyle.ModalTitle, 40f, 296f), Is.GreaterThan(wide), "más grande, más líneas");
            Assert.That(UiText.LineCount("Amazonas", TextStyle.Body, 14f, 10f), Is.GreaterThan(1), "una palabra que no cabe se parte");
            var many = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                many.Append("Amazonas ");
            }

            Assert.That(UiText.LineCount(many.ToString(), TextStyle.Body, 14f, 296f), Is.GreaterThan(2));
        }

        [Test]
        public void Arrows_OnlyInTheTwoOrientationLabels_PaintedInBaloo2()
        {
            LocTable strings = LocSetUpFixture.Catalog.Strings;
            foreach (string key in AllKeys())
            {
                string text = strings.TextAt(strings.IndexOf(key), LocLocale.SourceLocale);
                bool arrows = text.IndexOf(FontCoverage.ArrowUp) >= 0 || text.IndexOf(FontCoverage.ArrowDown) >= 0;
                bool label = key == UiText.Keys.MapLabelAndes || key == UiText.Keys.MapLabelAtlantic;
                Assert.That(arrows, Is.EqualTo(label), key);
                Assert.That(text.IndexOf(FontCoverage.CheckMark), Is.EqualTo(-1), key + ": el ✓ es un icono, nunca texto");
                Assert.That(FontCoverage.FamilyOf(key), Is.EqualTo(label ? GlyphFamily.Title : GlyphFamily.Both), key);
            }
        }

        [Test]
        public void TypedAccess_HasNoPluralEs_NoGrouped_NoFormat_NoReplace()
        {
            string source = File.ReadAllText(MapTextSource, Encoding.UTF8);
            foreach (string trap in new[] { "PluralEs", "Grouped(", "string.Format", ".Replace(", "ToUpper", "ToLower", "== 1 ?" })
            {
                Assert.That(source.IndexOf(trap, StringComparison.Ordinal), Is.EqualTo(-1), trap);
            }
        }

        private static IEnumerable<string> AllKeys()
        {
            foreach (string key in GlossaryKeys)
            {
                yield return key;
            }

            foreach (string key in PM2Keys)
            {
                yield return key;
            }

            foreach (string key in Fx13Keys)
            {
                yield return key;
            }
        }

        private static void Fits(StringBuilder over, string key, string text)
        {
            LocTable strings = LocSetUpFixture.Catalog.Strings;
            int max = strings.MaxCharsAt(strings.IndexOf(key));
            if (text.Length > max)
            {
                over.Append(key).Append(": «").Append(text).Append("» ").Append(text.Length).Append(" > ").Append(max).Append('\n');
            }
        }
    }
}
