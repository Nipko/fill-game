#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>Una fila de la tabla única de Remote Config (08 §9): tipo y default tal como los escribe el GDD.</summary>
    internal readonly struct RcRow
    {
        public RcRow(string type, string? value)
        {
            Type = type;
            Default = value;
        }

        /// <summary>Primera palabra del tipo (<c>int</c>, <c>bool</c>, <c>json</c>, <c>string</c>…).</summary>
        public string Type { get; }

        /// <summary>Default sin comillas, negritas ni notas entre paréntesis, o null si no se pudo leer.</summary>
        public string? Default { get; }
    }

    /// <summary>
    /// Archivos del repositorio para los tests de <c>AF.LiveOps</c>, sin <c>UnityEngine</c>: la raíz se busca subiendo desde la
    /// carpeta de trabajo (en Unity, la del proyecto) o desde la ruta de este archivo, hasta dar con
    /// <c>docs/gdd/08-arquitectura-tecnica-publicacion.md</c>. Lee los Markdown del GDD, nunca copias.
    /// </summary>
    internal static class LiveOpsTestFiles
    {
        private const string Chapter08 = "08-arquitectura-tecnica-publicacion.md";

        /// <summary>Raíz del repositorio (la carpeta del proyecto Unity).</summary>
        public static string RepoRoot => FindRoot();

        /// <summary>Carpeta <c>Assets/_Project/Scripts/LiveOps</c>.</summary>
        public static string LiveOpsDir => Path.Combine(RepoRoot, "Assets", "_Project", "Scripts", "LiveOps");

        /// <summary>
        /// Filas de 08 §9 (<c>| `clave` / `clave2` | tipo / tipo2 | default / default2 | rango | dueño | uso |</c>). Con una sola
        /// celda de tipo o de default para varias claves, vale para todas. Una clave con plantilla (<c>biz_feature_&lt;sistema&gt;</c>)
        /// seguida de los sufijos entre comillas invertidas se expande en una clave por sufijo.
        /// </summary>
        public static Dictionary<string, RcRow> RemoteConfigTable()
        {
            string section = Chapter08Text();
            int start = section.IndexOf("\n## 9. ", StringComparison.Ordinal);
            int end = section.IndexOf("\n## 10. ", start + 1, StringComparison.Ordinal);
            Assert.That(start >= 0 && end > start, Is.True, "no encuentro la sección 9 de 08");

            var table = new Dictionary<string, RcRow>(StringComparer.Ordinal);
            foreach (string line in section.Substring(start, end - start).Split('\n'))
            {
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                var keys = new List<string>();
                foreach (Match m in Regex.Matches(cells[1], "`([^`]+)`"))
                {
                    keys.Add(m.Groups[1].Value);
                }

                Match template = Regex.Match(keys[0], "<[^>]+>");
                if (template.Success)
                {
                    string pattern = keys[0];
                    keys.RemoveAt(0);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        keys[i] = pattern.Replace(template.Value, keys[i]);
                    }
                }

                string[] types = SplitValues(cells[2]);
                string[] defaults = SplitValues(cells[3]);
                for (int i = 0; i < keys.Count; i++)
                {
                    string type = types.Length == keys.Count ? types[i] : types[0];
                    string? value = defaults.Length == keys.Count ? defaults[i] : defaults.Length == 1 ? defaults[0] : null;
                    table[keys[i]] = new RcRow(FirstWord(type), value == null ? null : DropNote(value));
                }
            }

            Assert.That(table.Count, Is.GreaterThan(100), "la tabla de 08 §9 se leyó entera");
            return table;
        }

        /// <summary>Párrafo de los alias retirados de 08 §9.</summary>
        public static string RetiredAliases()
        {
            string text = Chapter08Text();
            int start = text.IndexOf("**Alias retirados**", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "no encuentro los alias retirados de 08 §9");
            int end = text.IndexOf('\n', start);
            return text.Substring(start, end - start);
        }

        private static string Chapter08Text()
        {
            string path = Path.Combine(RepoRoot, "docs", "gdd", Chapter08);
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static string[] SplitValues(string cell)
        {
            string[] parts = cell.Split(new[] { " / " }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Replace("**", "").Trim();
            }

            return parts;
        }

        private static string FirstWord(string type)
        {
            int space = type.IndexOf(' ');
            return (space < 0 ? type : type.Substring(0, space)).Trim('`');
        }

        private static string DropNote(string value)
        {
            int note = value.IndexOf(" (", StringComparison.Ordinal);
            string v = note < 0 ? value : value.Substring(0, note);
            return v.Trim().Trim('`').Trim().Trim('"');
        }

        private static string FindRoot([CallerFilePath] string thisFile = "")
        {
            string? fromCwd = WalkUp(Environment.CurrentDirectory);
            if (fromCwd != null)
            {
                return fromCwd;
            }

            string? fromSource = thisFile.Length == 0 ? null : WalkUp(Path.GetDirectoryName(Path.GetFullPath(thisFile)));
            Assert.That(fromSource, Is.Not.Null, "no encuentro la raíz del repositorio (docs/gdd/" + Chapter08 + ")");
            return fromSource!;
        }

        private static string? WalkUp(string? dir)
        {
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "docs", "gdd", Chapter08)))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }
    }
}
