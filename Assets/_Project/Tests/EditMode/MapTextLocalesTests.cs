#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using AF.Levels;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los textos del Mapa del río en en y pt-BR con las tablas del proyecto (observación 1 de la prueba en el teléfono del
    /// 2026-10-23: en el inicio y en el Mapa salían vacíos «Faltan N niveles para {parada}», la pregunta del mundo de la cabecera
    /// y, con ellos, todo lo que nombra una parada). La causa: las filas <c>map.*</c> de <c>facts.csv</c> (nombres de parada,
    /// ríos, países, preguntas de los mundos, datos y fuentes) no tenían en ni pt-BR, y <c>map_stop_remaining</c> no sale sin
    /// su <c>{place}</c>. Con las traducciones del bloque aplicadas (<c>tools/loc/apply_translations.py</c>, sin retener el
    /// bloque <c>map.*</c>), en un build de desarrollo (enseña lo no aprobado, como el teléfono de pruebas):
    /// <list type="bullet">
    /// <item>toda fila <c>map.*</c> tiene texto en los dos idiomas;</item>
    /// <item>cada parada tiene nombre, país y río, y «Faltan N niveles para…» hacia la siguiente (o hacia la naciente tras la
    /// última) no sale vacío y cabe en su <c>max_chars</c> × 1,3;</item>
    /// <item>la pregunta de cada mundo que la tiene y «Premio del mundo · faltan N» no salen vacíos;</item>
    /// <item>los nombres de parada caben en dos líneas a 296 dp, como en español.</item>
    /// </list>
    /// En release (sin enseñar lo no aprobado), nada de eso llega al jugador mientras el español siga en
    /// <c>pendiente_asesor</c> (08 §4.7: un texto largo solo sale con <c>aprobado_es</c> y <c>revisado_nativo</c>).
    /// </summary>
    public sealed class MapTextLocalesTests
    {
        private static readonly string[] Locales = { LocLocale.English, LocLocale.PortugueseBrazil };

        private static RiverStopCatalog? s_stops;
        private LocStateSnapshot _loc;

        private static RiverStopCatalog Stops => s_stops ??= RiverStopCatalogReader.Read(
            File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Data", "Map", "river_stops_v1.json"), Encoding.UTF8));

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
        public void MapRows_HaveEnglishAndPortuguese([ValueSource(nameof(Locales))] string locale)
        {
            LocTable facts = LocSetUpFixture.Catalog.Facts;
            var empty = new List<string>();
            int rows = 0;
            for (int r = 0; r < facts.Count; r++)
            {
                string key = facts.KeyAt(r);
                if (!key.StartsWith("map.", System.StringComparison.Ordinal))
                {
                    continue;
                }

                rows++;
                if (facts.TextAt(r, locale).Length == 0)
                {
                    empty.Add(key);
                }
            }

            Assert.That(rows, Is.GreaterThan(400), "el bloque map.* de facts.csv");
            Assert.That(empty, Is.Empty, "filas map.* sin " + locale + ":\n" + string.Join("\n", empty));
        }

        [Test]
        public void StopTexts_AreNotEmpty_AndFit_InDevelopment([ValueSource(nameof(Locales))] string locale)
        {
            Loc.Configure(LocSetUpFixture.Catalog, locale, true);
            RiverStopCatalog stops = Stops;
            var problems = new StringBuilder();
            for (int n = 0; n < stops.Count; n++)
            {
                RiverStop stop = stops.At(n)!;
                Check(problems, stop.NameKey, UiText.StopName(stop));
                Check(problems, stop.CountryNameKeyOrDefault, UiText.StopCountry(stop));
                Check(problems, stop.RiverKey, UiText.StopRiver(stop));
                string place = UiText.NextPlaceName(stops, stop);
                Check(problems, "next place after " + stop.N, place);
                string remaining = UiText.MapStopRemaining(RiverStopCatalog.LevelsPerStop, place);
                Check(problems, UiText.Keys.MapStopRemaining + " (" + stop.N + ")", remaining);
                Fits(problems, UiText.Keys.MapStopRemaining, remaining);
                if (stop.WorldQuestion != null)
                {
                    string question = Loc.Fact(stop.WorldQuestion);
                    Check(problems, stop.WorldQuestion, question);
                    Fits(problems, UiText.Keys.MapWorldQuestion, UiText.MapWorldQuestion(stop.World, question));
                }
            }

            Check(problems, UiText.Keys.MapWorldPrize, UiText.MapWorldPrize(7));
            Check(problems, UiText.Keys.MapWorldPrize, UiText.MapWorldPrize(1));
            Assert.That(problems.ToString(), Is.Empty, locale);
        }

        [Test]
        public void StopNames_FitInTwoLinesAt296dp([ValueSource(nameof(Locales))] string locale)
        {
            Loc.Configure(LocSetUpFixture.Catalog, locale, true);
            var report = new StringBuilder();
            for (int n = 0; n < Stops.Count; n++)
            {
                string name = UiText.StopName(Stops.At(n)!);
                int lines = UiText.LineCount(name, TextStyle.ModalTitle, UiText.StopNameSizeSp, UiText.StopNameWidthDp);
                if (lines < 1 || lines > UiText.StopNameMaxLines)
                {
                    report.Append(name).Append(": ").Append(lines).Append(" líneas\n");
                }
            }

            Assert.That(report.ToString(), Is.Empty, locale + " a " + UiText.StopNameWidthDp + " dp");
        }

        [Test]
        public void StopTexts_ReachThePlayer_InRelease_OnlyWithTheSpanishApproved_AndTheTranslationReviewed([ValueSource(nameof(Locales))] string locale)
        {
            Loc.Configure(LocSetUpFixture.Catalog, locale, false);
            RiverStop stop = Stops.At(5)!;
            LocStatus spanish = Loc.StatusOf(stop.NameKey, LocLocale.SourceLocale);
            LocStatus translation = Loc.StatusOf(stop.NameKey, locale);
            Assert.That(spanish, Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs), "lo cultural nace pendiente del asesor");
            Assert.That(translation, Is.EqualTo(LocStatus.Translated).Or.EqualTo(LocStatus.NativeReviewed));

            // 08 §4.7: sin aprobado_es y revisado_nativo, no se muestra; con los dos (revisión de D-054), sí.
            bool shown = spanish >= LocStatus.ApprovedEs && translation == LocStatus.NativeReviewed;
            Assert.That(shown, Is.True, "D-054: la parada 5 tiene el español aprobado y la traducción revisada");
            Assert.That(UiText.StopName(stop), Is.Not.Empty, "08 §4.7: aprobada y revisada, se muestra");
            Assert.That(UiText.NextPlaceName(Stops, stop), Is.Not.Empty);
        }

        private static void Check(StringBuilder problems, string what, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                problems.Append(what).Append(": vacío\n");
            }
        }

        // La regla de las traducciones (LocTablesTests, VisibleLength): ≤ max_chars × 1,3.
        private static void Fits(StringBuilder problems, string key, string text)
        {
            LocTable strings = LocSetUpFixture.Catalog.Strings;
            int max = strings.MaxCharsAt(strings.IndexOf(key));
            if (text.Length > max * 1.3f)
            {
                problems.Append(key).Append(": «").Append(text).Append("» ").Append(text.Length).Append(" > ").Append(max).Append(" × 1,3\n");
            }
        }
    }
}
