#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace AF.Analytics.Tests
{
    /// <summary>
    /// <see cref="AnalyticsCatalog"/> es la taxonomía única de 08 §8.2 (y, donde 08 remite, de 04 §11): los mismos eventos, con
    /// los mismos parámetros específicos, ninguno con más de 10, y los comunes y las propiedades de usuario de 08 §8.1.
    /// La tabla se lee del GDD en cada ejecución: si 08 §8.2 cambia, esta prueba lo dice.
    /// </summary>
    public sealed class AnalyticsCatalogTests
    {
        // 04 §11, fila af_stuck_choice: «y, con D-008, `give_up`, `confirm_play_on` y `quit` [P]» son valores de choice, no parámetros.
        private static readonly string[] ValuesNotParams = { "give_up", "confirm_play_on", "quit" };

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string Chapter(string file)
        {
            string path = Path.Combine(RepoRoot, "docs", "gdd", file);
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static string Section(string text, string startHeading, string endHeading)
        {
            int start = text.IndexOf(startHeading, StringComparison.Ordinal);
            int end = text.IndexOf(endHeading, start + 1, StringComparison.Ordinal);
            Assert.That(start >= 0 && end > start, Is.True, startHeading);
            return text.Substring(start, end - start);
        }

        // Filas «| `evento` / `evento2` | parámetros | …» de una tabla de eventos.
        private static Dictionary<string, string> Rows(string section)
        {
            var rows = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string line in section.Split('\n'))
            {
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                MatchCollection names = Regex.Matches(cells[1], "`([a-z_]+)`");
                string previous = names[0].Groups[1].Value;
                foreach (Match m in names)
                {
                    string name = m.Groups[1].Value;
                    if (name.StartsWith("_", StringComparison.Ordinal))
                    {
                        name = previous.Substring(0, previous.LastIndexOf('_')) + name; // `af_infinite_canoes_start` / `_end`
                    }

                    rows[name] = cells[2];
                    previous = name;
                }
            }

            return rows;
        }

        // Parámetros de una celda: lo que va entre comillas invertidas fuera de paréntesis, sin lo que sigue a «= » (valores),
        // con el identificador del principio (`completed (bool)` → completed; `source ∈ {…}` → source).
        private static List<string> Params(string cell)
        {
            string top = Regex.Replace(cell, @"\([^()]*\)", "");
            top = Regex.Replace(top, @"\([^()]*\)", "");
            var names = new List<string>();
            foreach (Match m in Regex.Matches(top, @"(=\s*)?`([a-z_][a-z0-9_]*)[^`]*`"))
            {
                if (m.Groups[1].Success)
                {
                    continue;
                }

                string name = m.Groups[2].Value;
                if (Array.IndexOf(ValuesNotParams, name) < 0 && !names.Contains(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private static Dictionary<string, List<string>> ExpectedFromGdd()
        {
            Dictionary<string, string> rows08 = Rows(Section(Chapter("08-arquitectura-tecnica-publicacion.md"), "### 8.2 Tabla de eventos", "\n## 9. "));
            Dictionary<string, string> rows04 = Rows(Section(Chapter("04-comodines-vidas-rescate.md"), "## 11. Métricas a instrumentar", "\n## Parámetros configurables"));
            var expected = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> row in rows08)
            {
                string cell = row.Value;
                if (cell.Contains("Según capítulo 4 §11"))
                {
                    Assert.That(rows04.ContainsKey(row.Key), Is.True, row.Key + ": 08 remite a 04 §11 y 04 §11 no lo tiene");
                    cell = rows04[row.Key];
                }

                expected[row.Key] = Params(cell);
            }

            return expected;
        }

        [Test]
        public void TheCatalog_IsExactlyThe08_2Table()
        {
            Dictionary<string, List<string>> expected = ExpectedFromGdd();
            Assert.That(expected.Count, Is.GreaterThan(60), "la tabla de 08 §8.2 se leyó entera");
            var catalogNames = new List<string>();
            foreach (CatalogEntry e in AnalyticsCatalog.All)
            {
                catalogNames.Add(e.Name);
                Assert.That(expected.ContainsKey(e.Name), Is.True, e.Name + " no está dado de alta en 08 §8.2");
                Assert.That(e.Params, Is.EqualTo(expected[e.Name]), e.Name + ": parámetros de 08 §8.2 / 04 §11");
            }

            var gddNames = new List<string>(expected.Keys);
            gddNames.Sort(StringComparer.Ordinal);
            catalogNames.Sort(StringComparer.Ordinal);
            Assert.That(catalogNames, Is.EqualTo(gddNames), "ni un evento de más ni de menos");
        }

        [Test]
        public void NoEvent_HasMoreThan10SpecificParams()
        {
            foreach (CatalogEntry e in AnalyticsCatalog.All)
            {
                Assert.That(e.Params.Count, Is.LessThanOrEqualTo(AnalyticsCatalog.MaxSpecificParams), e.Name + " (08 §8.1: ≤ 10 propios)");
                Assert.That(Regex.IsMatch(e.Name, "^(af_[a-z_]+|level_start|level_end|earn_virtual_currency|spend_virtual_currency|ad_impression|purchase)$"),
                    Is.True, e.Name + ": prefijo af_ o evento recomendado de Firebase (08 §8.1)");
            }
        }

        [Test]
        public void TheCommonParams_AndTheUserProperties_AreThoseOf08_1()
        {
            string s81 = Section(Chapter("08-arquitectura-tecnica-publicacion.md"), "### 8.1 Convenciones", "### 8.2 ");
            string commonLine = s81.Substring(s81.IndexOf("Parámetros comunes", StringComparison.Ordinal));
            commonLine = commonLine.Substring(0, commonLine.IndexOf('\n'));
            var common = new List<string>();
            foreach (Match m in Regex.Matches(commonLine, @"`([a-z_]+)( ∈[^`]*)?`"))
            {
                common.Add(m.Groups[1].Value);
            }

            Assert.That(AnalyticsCatalog.CommonParamNames, Is.EqualTo(common));
            Assert.That(common.Count, Is.EqualTo(15), "08 §8.1: «los parámetros comunes ocupan 15»");

            string userLine = s81.Substring(s81.IndexOf("Propiedades de usuario", StringComparison.Ordinal));
            userLine = userLine.Substring(0, userLine.IndexOf('\n'));
            foreach (string p in AnalyticsCatalog.UserProperties)
            {
                StringAssert.Contains("`" + p, userLine, p);
            }
        }
    }
}
