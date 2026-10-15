#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AF.EditorTools;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El glosario por idioma <c>Data/Loc/glossary.csv</c> (contrato 32; 08 §4.7; D-018; propuesta de idiomas §3.9; tarea L2c):
    /// término, tipo, un valor por idioma y notas, derivado del glosario §9-§10, que sigue siendo el canon. No lo lee el juego:
    /// es para los traductores y para los glifos de los atlas (B1).
    /// <list type="bullet">
    /// <item>UTF-8 sin BOM, LF y RFC 4180, con la cabecera <c>term,kind,es-419,en,pt-BR,notes</c>, seis campos por fila y filas en
    /// orden ordinal por término (regla de G1).</item>
    /// <item>Sin duplicados: ni dos filas con el mismo término ni dos con el mismo texto en es-419.</item>
    /// <item>Están los términos de los §9-§10 (con los de §2-§8 a los que remiten): los 14 tintes, las mecánicas, los comodines,
    /// las monedas, los personajes, los mundos y la marca, y en y pt-BR son los que da el glosario.</item>
    /// <item>Un término que es también una clave de <c>strings.csv</c> o de <c>facts.csv</c> dice lo mismo en las dos tablas: en
    /// particular, las filas de nombre de tinte de <c>facts.csv</c> (<c>tint.&lt;tinte&gt;.name</c>, F1F2), cuando existan.</item>
    /// <item>Sus caracteres entran en los atlas al regenerarlos (<see cref="FontSetup.Characters()"/>; B1).</item>
    /// </list>
    /// </summary>
    public sealed class GlossaryTableTests
    {
        private const string Header = "term,kind,es-419,en,pt-BR,notes";

        private static readonly string[] Locales = { LocLocale.SpanishLatinAmerica, LocLocale.English, LocLocale.PortugueseBrazil };

        // Tipos: la marca y lo que no se traduce, personajes, monedas, comodines (nombre temático y etiqueta), mecánicas y
        // modificadores, tintes, mundos y los términos de UI del §10 que nombran algo del juego.
        private static readonly string[] Kinds = { "booster", "brand", "character", "currency", "mechanic", "tint", "ui", "world" };

        // IDs de los comodines del glosario §11 (nombre temático del §5.1 en booster.<id>).
        private static readonly string[] Boosters = { "undo", "extra_cuia", "hint", "shuffle", "reveal", "skip", "restart", "continue_cuia", "rv_double_harvest" };

        // Mecánicas y variantes de la 1.0 con su ID del glosario §11 (attrs, intro_mechanic y variantes de D-023).
        private static readonly string[] Mechanics =
        {
            "hoja", "matapi", "liana", "remolino", "barro", "lluvia", "brote", "pirana", "tinaja", "chica", "alta", "hoja_atada", "flor", "brote_n2", "liana_n",
        };

        // Claves del §10 que son términos del juego: etiquetas de los comodines, monedas y nombres de sistemas y modificadores.
        private static readonly string[] UiTerms =
        {
            "undo", "hint", "shuffle", "reveal", "skip", "restart", "extra_cuia", "currency", "lives", "murky", "streak", "river_lowered",
            "daily_challenge", "river_fact", "fact_sheet_title", "patterns_toggle", "shop", "no_ads", "difficulty_hard", "very_hard",
        };

        private static readonly Regex TermForm = new Regex("^[a-z0-9_]+(\\.[a-z0-9_]+)*$");

        [Test]
        public void File_IsUtf8WithoutBom_Lf_WithItsHeader_AndSixFieldsPerRow()
        {
            byte[] bytes = File.ReadAllBytes(FontCoverage.DiskPath(FontCoverage.GlossaryPath));
            Assert.That(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, Is.False, "sin BOM");
            string text = LocCsv.Decode(bytes, FontCoverage.GlossaryFile);
            Assert.That(text.IndexOf('\r'), Is.EqualTo(-1), "LF");
            Assert.That(text.Substring(0, text.IndexOf('\n')), Is.EqualTo(Header), "cabecera de L2c (08 §4.7: término, tipo, idiomas y notas)");
            Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True, "termina en salto de línea");

            IReadOnlyList<LocCsvRow> rows = LocCsv.Parse(text, FontCoverage.GlossaryFile);
            Assert.That(rows.Count, Is.GreaterThan(1));
            foreach (LocCsvRow row in rows)
            {
                Assert.That(row.Fields.Count, Is.EqualTo(6), "línea " + row.Line);
            }
        }

        [Test]
        public void Rows_AreInOrdinalOrder_WithoutDuplicates_AndWellFormed()
        {
            List<GlossaryRow> rows = ReadGlossary();
            var terms = new HashSet<string>(StringComparer.Ordinal);
            var spanish = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                GlossaryRow row = rows[i];
                Assert.That(TermForm.IsMatch(row.Term), Is.True, "forma del término: " + row.Term);
                Assert.That(terms.Add(row.Term), Is.True, "término repetido: " + row.Term);
                Assert.That(Array.IndexOf(Kinds, row.Kind), Is.GreaterThanOrEqualTo(0), row.Term + ": tipo " + row.Kind);
                Assert.That(row.Es, Is.Not.Empty, row.Term + ": es-419");
                Assert.That(row.Notes, Is.Not.Empty, row.Term + ": notas");
                Assert.That(spanish.ContainsKey(row.Es), Is.False, row.Term + ": el término «" + row.Es + "» ya está en " + (spanish.ContainsKey(row.Es) ? spanish[row.Es] : string.Empty));
                spanish[row.Es] = row.Term;
                foreach (string locale in Locales)
                {
                    string value = row.Text(locale);
                    Assert.That(value, Is.EqualTo(value.Trim()), row.Term + " (" + locale + "): sin espacios en los bordes");
                    Assert.That(value.StartsWith("pendiente", StringComparison.Ordinal), Is.False, row.Term + " (" + locale + "): una traducción que falta queda vacía");
                }

                if (i > 0)
                {
                    Assert.That(string.CompareOrdinal(rows[i - 1].Term, row.Term), Is.LessThan(0), "orden ordinal: " + rows[i - 1].Term + " antes que " + row.Term);
                }
            }
        }

        [Test]
        public void Tints_AreTheFourteenOfTheGlossary9_InTheThreeLanguages()
        {
            Dictionary<string, GlossaryRow> glossary = ByTerm(ReadGlossary());
            string section = Section(Glossary(), "## 9. ", "## 10. ");
            int tints = 0;
            foreach (string line in section.Split('\n'))
            {
                Match m = Regex.Match(line, "^\\| `([a-z_]+)` \\| [0-9]+ \\| ([^|]+) \\| ([^|]+) \\| ([^|]+) \\|");
                if (!m.Success)
                {
                    continue;
                }

                tints++;
                string term = "tint." + m.Groups[1].Value + ".name";
                Assert.That(glossary.ContainsKey(term), Is.True, "falta " + term);
                GlossaryRow row = glossary[term];
                string es = m.Groups[2].Value.Trim();
                int paren = es.IndexOf(" (", StringComparison.Ordinal);
                Assert.That(row.Kind, Is.EqualTo("tint"), term);
                Assert.That(row.Es, Is.EqualTo(paren > 0 ? es.Substring(0, paren) : es), term + ": es-419 sin el color");
                Assert.That(row.En, Is.EqualTo(m.Groups[3].Value.Trim()), term + ": en");
                Assert.That(row.Pt, Is.EqualTo(m.Groups[4].Value.Trim()), term + ": pt-BR");
            }

            Assert.That(tints, Is.EqualTo(14), "K_paleta = 14 (glosario §9)");
            Assert.That(RowsOfKind(glossary, "tint"), Is.EqualTo(14), "ningún tinte de más");
        }

        [Test]
        public void UiTerms_AreTheOnesOfTheGlossary10()
        {
            Dictionary<string, GlossaryRow> glossary = ByTerm(ReadGlossary());
            Dictionary<string, string[]> table = Glossary10();
            foreach (string key in UiTerms)
            {
                Assert.That(table.ContainsKey(key), Is.True, key + " está en el §10");
                Assert.That(glossary.ContainsKey(key), Is.True, "falta " + key);
            }

            foreach (GlossaryRow row in glossary.Values)
            {
                if (!table.TryGetValue(row.Term, out string[]? cells))
                {
                    continue;
                }

                for (int l = 0; l < Locales.Length; l++)
                {
                    string expected = cells[l].StartsWith("pendiente", StringComparison.Ordinal) ? string.Empty : cells[l];
                    Assert.That(row.Text(Locales[l]), Is.EqualTo(expected), row.Term + " (" + Locales[l] + ") como en el glosario §10");
                }
            }
        }

        [Test]
        public void HasTheBoosters_Mechanics_Currencies_Characters_Worlds_AndTheBrand()
        {
            Dictionary<string, GlossaryRow> glossary = ByTerm(ReadGlossary());
            foreach (string id in Boosters)
            {
                AssertKind(glossary, "booster." + id, "booster");
                if (Array.IndexOf(UiTerms, id) >= 0)
                {
                    AssertKind(glossary, id, "booster"); // la etiqueta del HUD, si el §10 la tiene
                }
            }

            foreach (string id in Mechanics)
            {
                AssertKind(glossary, "mechanic." + id, "mechanic");
            }

            AssertKind(glossary, "murky", "mechanic");
            AssertKind(glossary, "currency", "currency");
            AssertKind(glossary, "lives", "currency");
            AssertKind(glossary, "character.capi", "character");
            AssertKind(glossary, "character.rosa", "character");
            AssertKind(glossary, "character.sloth", "character");
            AssertKind(glossary, "brand.name", "brand");
            AssertKind(glossary, "brand.cuia", "brand");
            for (int w = 1; w <= 13; w++)
            {
                AssertKind(glossary, "world_w" + w.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "_name", "world");
            }

            Assert.That(RowsOfKind(glossary, "world"), Is.EqualTo(13), "13 mundos (glosario §8)");

            // Lo que no se traduce (propuesta de idiomas §3.9; glosario §1.2, §3 y §4): igual en los tres idiomas.
            foreach (string term in new[] { "brand.name", "brand.cuia", "character.capi", "character.rosa", "character.sloth", "extra_cuia" })
            {
                GlossaryRow row = glossary[term];
                Assert.That(row.En, Is.EqualTo(row.Es), term + " (en)");
                Assert.That(row.Pt, Is.EqualTo(row.Es), term + " (pt-BR)");
            }

            Assert.That(glossary["brand.name"].Es, Is.EqualTo("Tintalú"), "siempre con tilde en lo visible");
            Assert.That(glossary["character.sloth"].Es, Is.EqualTo("TiTino"));
        }

        [Test]
        public void TermsThatAreKeysOfTheTables_SayTheSameThere_IncludingTintNamesOfFacts()
        {
            Dictionary<string, GlossaryRow> glossary = ByTerm(ReadGlossary());
            Dictionary<string, string[]> section10 = Glossary10();
            LocTable strings = LocTable.Parse(LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile), LocCatalog.StringsFile);
            LocTable facts = LocTable.Parse(LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile), LocCatalog.FactsFile);
            int matched = 0;
            foreach (LocTable table in new[] { strings, facts })
            {
                for (int r = 0; r < table.Count; r++)
                {
                    string key = table.KeyAt(r);
                    if (!glossary.TryGetValue(key, out GlossaryRow? row))
                    {
                        Assert.That(Regex.IsMatch(key, "^tint\\.[a-z_]+\\.name$"), Is.False, table.File + ": el nombre de tinte " + key + " no está en el glosario");
                        continue;
                    }

                    matched++;
                    Assert.That(table.TextAt(r, LocLocale.SpanishLatinAmerica), Is.EqualTo(row.Es), table.File + " " + key + " (es-419)");
                    for (int l = 1; l < Locales.Length; l++)
                    {
                        string there = table.TextAt(r, Locales[l]);
                        if (there.Length > 0)
                        {
                            Assert.That(row.Text(Locales[l]), Is.EqualTo(there), table.File + " " + key + " (" + Locales[l] + "): la traducción usa el término aprobado");
                        }
                    }
                }
            }

            Assert.That(matched, Is.GreaterThanOrEqualTo(13 + 9), "los mundos y las claves de UI de L1 y L2b");

            // Todo término sin puntos es una clave de UI: del §10 o de strings.csv (los nombres de mundo de L1).
            foreach (GlossaryRow row in glossary.Values)
            {
                if (row.Term.IndexOf('.') < 0)
                {
                    Assert.That(section10.ContainsKey(row.Term) || strings.Contains(row.Term), Is.True, row.Term + " es una clave del §10 o de strings.csv");
                }
            }
        }

        [Test]
        public void GlyphsOfTheAtlases_IncludeEveryCharacterOfTheGlossary()
        {
            List<GlossaryRow> rows = ReadGlossary();
            int cells = 0;
            foreach (GlossaryRow row in rows)
            {
                foreach (string locale in Locales)
                {
                    cells += row.Text(locale).Length > 0 ? 1 : 0;
                }
            }

            int fromGlossary = 0;
            foreach (LocTextEntry entry in FontCoverage.ReadProjectTables())
            {
                fromGlossary += entry.File == FontCoverage.GlossaryFile ? 1 : 0;
            }

            Assert.That(fromGlossary, Is.EqualTo(cells), "B1 lee todas las celdas de idioma del glosario");

            string characters = FontSetup.Characters();
            foreach (GlossaryRow row in rows)
            {
                foreach (string locale in Locales)
                {
                    foreach (char c in row.Text(locale))
                    {
                        Assert.That(characters.IndexOf(c), Is.GreaterThanOrEqualTo(0), row.Term + " (" + locale + "): U+" + ((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        private static void AssertKind(Dictionary<string, GlossaryRow> glossary, string term, string kind)
        {
            Assert.That(glossary.ContainsKey(term), Is.True, "falta " + term);
            Assert.That(glossary[term].Kind, Is.EqualTo(kind), term);
        }

        private static int RowsOfKind(Dictionary<string, GlossaryRow> glossary, string kind)
        {
            int n = 0;
            foreach (GlossaryRow row in glossary.Values)
            {
                n += row.Kind == kind ? 1 : 0;
            }

            return n;
        }

        private static Dictionary<string, GlossaryRow> ByTerm(List<GlossaryRow> rows)
        {
            var map = new Dictionary<string, GlossaryRow>(StringComparer.Ordinal);
            foreach (GlossaryRow row in rows)
            {
                map[row.Term] = row;
            }

            return map;
        }

        private static List<GlossaryRow> ReadGlossary()
        {
            string text = LocCsv.Decode(File.ReadAllBytes(FontCoverage.DiskPath(FontCoverage.GlossaryPath)), FontCoverage.GlossaryFile);
            IReadOnlyList<LocCsvRow> records = LocCsv.Parse(text, FontCoverage.GlossaryFile);
            Assert.That(string.Join(",", records[0].Fields), Is.EqualTo(Header));
            var rows = new List<GlossaryRow>();
            for (int i = 1; i < records.Count; i++)
            {
                IReadOnlyList<string> f = records[i].Fields;
                Assert.That(f.Count, Is.EqualTo(6), "línea " + records[i].Line);
                rows.Add(new GlossaryRow(f[0], f[1], f[2], f[3], f[4], f[5]));
            }

            return rows;
        }

        // Filas del §10 del glosario («| `clave` | es | en | pt |»): una clave por celda de idioma; con varias claves en la misma
        // fila («`sound` / `music` / `haptics`»), cada valor separado por « / ».
        private static Dictionary<string, string[]> Glossary10()
        {
            string section = Section(Glossary(), "## 10. ", "## 11. ");
            var table = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (string line in section.Split('\n'))
            {
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                MatchCollection keys = Regex.Matches(cells[1], "`([^`]+)`");
                for (int k = 0; k < keys.Count; k++)
                {
                    var values = new string[3];
                    for (int l = 0; l < 3; l++)
                    {
                        string cell = cells[2 + l].Trim();
                        string[] parts = cell.Split(new[] { " / " }, StringSplitOptions.None);
                        values[l] = keys.Count > 1 && parts.Length == keys.Count ? parts[k].Trim() : cell;
                    }

                    table[keys[k].Groups[1].Value] = values;
                }
            }

            Assert.That(table.Count, Is.GreaterThan(50), "el §10 se leyó entero");
            return table;
        }

        private static string Glossary()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "docs", "gdd", "00-glosario.md");
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static string Section(string text, string startHeading, string endHeading)
        {
            int start = text.IndexOf("\n" + startHeading, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "no encuentro " + startHeading);
            int end = text.IndexOf("\n" + endHeading, start + 1, StringComparison.Ordinal);
            Assert.That(end, Is.GreaterThan(start), "no encuentro " + endHeading);
            return text.Substring(start, end - start);
        }

        private sealed class GlossaryRow
        {
            public GlossaryRow(string term, string kind, string es, string en, string pt, string notes)
            {
                Term = term;
                Kind = kind;
                Es = es;
                En = en;
                Pt = pt;
                Notes = notes;
            }

            public string Term { get; }

            public string Kind { get; }

            public string Es { get; }

            public string En { get; }

            public string Pt { get; }

            public string Notes { get; }

            public string Text(string locale)
            {
                if (locale == LocLocale.SpanishLatinAmerica)
                {
                    return Es;
                }

                return locale == LocLocale.English ? En : Pt;
            }
        }
    }
}
