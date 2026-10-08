#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace AF.Ads.Tests
{
    /// <summary>Una fila de la tabla única de Remote Config (08 §9): tipo y default tal como los escribe el GDD.</summary>
    public readonly struct RcRow
    {
        /// <summary>Crea la fila.</summary>
        public RcRow(string type, string? value)
        {
            Type = type;
            Default = value;
        }

        /// <summary>Tipo (<c>int</c>, <c>bool</c>, <c>json</c>…).</summary>
        public string Type { get; }

        /// <summary>Default sin comillas, negritas ni notas entre paréntesis, o null si no se pudo leer.</summary>
        public string? Default { get; }
    }

    /// <summary>
    /// Lectura de tablas del GDD para las guardas contra cifras y nombres inventados (B1-3 y B1-4; la usan también los
    /// tests de <c>AF.IAP</c>). Lee los Markdown del repositorio, nunca copias.
    /// </summary>
    public static class GddTables
    {
        /// <summary>Sufijos de las claves por ubicación de 08 §9 (<c>ads_rv_cap_&lt;placement&gt;</c>).</summary>
        public static readonly string[] PlacementSuffixes = { "double_harvest", "undo_refill", "hint", "reveal", "extra_canoe", "daily_chest" };

        /// <summary>Raíz del repositorio (la carpeta del proyecto Unity).</summary>
        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>Texto de <c>docs/gdd/<paramref name="fileName"/></c> con saltos LF.</summary>
        public static string Chapter(string fileName)
        {
            string path = Path.Combine(RepoRoot, "docs", "gdd", fileName);
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        /// <summary>
        /// Texto entre el encabezado que empieza por <paramref name="startHeading"/> y el siguiente que empieza por
        /// <paramref name="endHeading"/> (p. ej. <c>"### 4.1 "</c> y <c>"### 4.2 "</c>).
        /// </summary>
        public static string Section(string text, string startHeading, string endHeading)
        {
            int start = text.IndexOf("\n" + startHeading, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "no encuentro " + startHeading);
            int end = text.IndexOf("\n" + endHeading, start + 1, StringComparison.Ordinal);
            Assert.That(end, Is.GreaterThan(start), "no encuentro " + endHeading);
            return text.Substring(start, end - start);
        }

        /// <summary>
        /// Filas de 08 §9 (<c>| `clave` / `clave2` | tipo / tipo2 | default / default2 | rango | dueño | uso |</c>). Con una
        /// sola celda de tipo o de default para varias claves, vale para todas. Las claves <c>&lt;placement&gt;</c> se
        /// expanden por ubicación: «double_harvest 5 · undo_refill 4 · …» o «60 (extra_canoe 90)».
        /// </summary>
        public static Dictionary<string, RcRow> RemoteConfigTable()
        {
            string section = Section(Chapter("08-arquitectura-tecnica-publicacion.md"), "## 9. ", "## 10. ");
            var table = new Dictionary<string, RcRow>(StringComparer.Ordinal);
            foreach (string line in section.Split('\n'))
            {
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                MatchCollection keys = Regex.Matches(cells[1], "`([^`]+)`");
                string[] types = SplitValues(cells[2]);
                string[] defaults = SplitValues(cells[3]);
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i].Groups[1].Value;
                    string type = types.Length == keys.Count ? types[i] : types[0];
                    string? value = defaults.Length == keys.Count ? defaults[i] : defaults.Length == 1 ? defaults[0] : null;
                    if (key.Contains("<placement>") && value != null)
                    {
                        Dictionary<string, string> perPlacement = PerPlacement(value);
                        foreach (KeyValuePair<string, string> entry in perPlacement)
                        {
                            table[key.Replace("<placement>", entry.Key)] = new RcRow(type, entry.Value);
                        }

                        continue;
                    }

                    table[key] = new RcRow(type, value == null ? null : DropNote(value));
                }
            }

            Assert.That(table.Count, Is.GreaterThan(100), "la tabla de 08 §9 se leyó entera");
            return table;
        }

        private static Dictionary<string, string> PerPlacement(string cell)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (cell.Contains("·"))
            {
                foreach (string part in cell.Split('·'))
                {
                    string[] words = part.Trim().Split(' ');
                    values[words[0]] = words[1];
                }
            }
            else
            {
                string[] words = cell.Trim().Split(' ');
                foreach (string suffix in PlacementSuffixes)
                {
                    values[suffix] = words[0];
                }

                Match note = Regex.Match(cell, "\\(([a-z_]+) ([0-9]+)\\)");
                if (note.Success)
                {
                    values[note.Groups[1].Value] = note.Groups[2].Value;
                }
            }

            Assert.That(values.Count, Is.EqualTo(PlacementSuffixes.Length), "una cifra por ubicación en «" + cell + "»");
            return values;
        }

        private static string DropNote(string value)
        {
            int note = value.IndexOf(" (", StringComparison.Ordinal);
            return note < 0 ? value : value.Substring(0, note).Trim();
        }

        private static string[] SplitValues(string cell)
        {
            string[] parts = cell.Split(new[] { " / " }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Replace("**", "").Trim().Trim('`').Trim('"').Trim();
            }

            return parts;
        }
    }
}
