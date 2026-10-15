#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Localization;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>Resultado de <see cref="LocReleaseGate.Evaluate"/>: los problemas de los textos para un build de release.</summary>
    public sealed class LocReleaseReport
    {
        internal LocReleaseReport(string applicationId, bool development, List<string> problems)
        {
            ApplicationId = applicationId;
            IsDevelopmentId = development;
            Problems = problems;
        }

        /// <summary>El <c>applicationId</c> del build.</summary>
        public string ApplicationId { get; }

        /// <summary>Si es el de los builds de prueba (<see cref="ProjectSetup.ApplicationId"/>): entonces la compuerta solo avisa.</summary>
        public bool IsDevelopmentId { get; }

        /// <summary>Un problema por línea: idioma ofrecido con la UI incompleta o texto cultural sin <c>aprobado_es</c>.</summary>
        public IReadOnlyList<string> Problems { get; }

        /// <summary>Si el build se para: hay problemas y el <c>applicationId</c> no es el de prueba.</summary>
        public bool Blocks => !IsDevelopmentId && Problems.Count > 0;
    }

    /// <summary>
    /// Compuerta de release de los textos (08 §4.7; D-018, Q10-A; propuesta de idiomas §3.10; tarea B1), que llaman
    /// <see cref="BuildDev"/> y <see cref="BuildSmoke"/> antes de construir. Un build con un <c>applicationId</c> distinto del de
    /// prueba (<c>com.amazonfill.dev</c>) <b>falla</b> si:
    /// <list type="bullet">
    /// <item><description>un idioma ofrecido tiene la UI incompleta: alguna clave de <c>strings.csv</c> sin texto en ese idioma.
    /// Los idiomas ofrecidos son las columnas de idioma de <c>strings.csv</c> (es-419, en y pt-BR en la tanda 1): la tabla declara
    /// los idiomas del juego [P]. en-XA no es una columna y nunca se ofrece en release;</description></item>
    /// <item><description>hay texto cultural (fruta, fauna, flora, lugar o pueblo) sin <c>aprobado_es</c>: toda fila de
    /// <c>facts.csv</c> (los textos largos son de fruta, fauna y del Mapa) y toda fila de <c>strings.csv</c> en
    /// <c>pendiente_asesor</c> (el estado con el que nace lo cultural; regla 9 de la propuesta de idiomas).</description></item>
    /// </list>
    /// Con <c>com.amazonfill.dev</c> solo avisa, para no frenar el desarrollo. Un dato sin traducir no bloquea: no se muestra
    /// (<see cref="Loc.Fact(string)"/>, Q10-A).
    /// </summary>
    public static class LocReleaseGate
    {
        private const string LogPrefix = "[AF.LocReleaseGate] ";

        // Máximo de claves citadas por idioma incompleto (el resto, «y N más»).
        private const int MaxKeysPerLocale = 8;

        // Máximo de problemas en el aviso de desarrollo (el resto se resume).
        private const int MaxWarningLines = 12;

        /// <summary>Los problemas de <paramref name="catalog"/> para un build con <paramref name="applicationId"/>.</summary>
        public static LocReleaseReport Evaluate(LocCatalog catalog, string applicationId)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (applicationId == null)
            {
                throw new ArgumentNullException(nameof(applicationId));
            }

            var problems = new List<string>();
            LocTable strings = catalog.Strings;
            IReadOnlyList<string> offered = strings.Locales;
            for (int l = 0; l < offered.Count; l++)
            {
                string locale = offered[l];
                var missing = new List<string>();
                for (int r = 0; r < strings.Count; r++)
                {
                    if (strings.TextAt(r, locale).Length == 0)
                    {
                        missing.Add(strings.KeyAt(r));
                    }
                }

                if (missing.Count > 0)
                {
                    problems.Add("UI incompleta en " + locale + ": claves de " + strings.File + " sin texto: "
                        + missing.Count.ToString(CultureInfo.InvariantCulture) + " (" + List(missing, MaxKeysPerLocale) + ").");
                }
            }

            for (int r = 0; r < strings.Count; r++)
            {
                LocStatus status = strings.StatusAt(r, LocLocale.SourceLocale);
                if (status == LocStatus.PendingAdvisor)
                {
                    problems.Add(Cultural(strings, r, status));
                }
            }

            LocTable facts = catalog.Facts;
            for (int r = 0; r < facts.Count; r++)
            {
                LocStatus status = facts.StatusAt(r, LocLocale.SourceLocale);
                if (status < LocStatus.ApprovedEs)
                {
                    problems.Add(Cultural(facts, r, status));
                }
            }

            bool development = string.Equals(applicationId, ProjectSetup.ApplicationId, StringComparison.Ordinal);
            return new LocReleaseReport(applicationId, development, problems);
        }

        /// <summary>
        /// Evalúa las tablas del proyecto para <paramref name="applicationId"/> y lo registra: con el de prueba, un aviso (o nada si
        /// no hay problemas); con otro, un error por problema. Devuelve false si el build se tiene que parar (también si las tablas
        /// no se pueden leer con un <c>applicationId</c> de release; en desarrollo eso solo avisa).
        /// </summary>
        public static bool Check(string applicationId, string logPrefix)
        {
            LocCatalog catalog;
            try
            {
                catalog = ReadProjectCatalog();
            }
            catch (Exception e) when (e is LocCsvException || e is IOException || e is FormatException)
            {
                bool dev = string.Equals(applicationId, ProjectSetup.ApplicationId, StringComparison.Ordinal);
                string line = logPrefix + LogPrefix + "no se pueden leer las tablas de textos: " + e.Message;
                if (dev)
                {
                    Debug.LogWarning(line);
                    return true;
                }

                Debug.LogError(line);
                return false;
            }

            return Log(Evaluate(catalog, applicationId), logPrefix);
        }

        /// <summary>Registra <paramref name="report"/> y devuelve false si bloquea el build.</summary>
        public static bool Log(LocReleaseReport report, string logPrefix)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            string prefix = logPrefix + LogPrefix;
            if (report.Problems.Count == 0)
            {
                Debug.Log(prefix + "OK: textos listos para release (" + report.ApplicationId + ").");
                return true;
            }

            if (report.IsDevelopmentId)
            {
                var sb = new StringBuilder();
                sb.Append(prefix).Append("aviso (").Append(report.ApplicationId)
                    .Append(", build de prueba: no bloquea). Problemas de textos que pararían un build de release: ")
                    .Append(report.Problems.Count.ToString(CultureInfo.InvariantCulture)).Append('.');
                int shown = Math.Min(MaxWarningLines, report.Problems.Count);
                for (int i = 0; i < shown; i++)
                {
                    sb.Append('\n').Append("  - ").Append(report.Problems[i]);
                }

                if (report.Problems.Count > shown)
                {
                    sb.Append('\n').Append("  - y ").Append((report.Problems.Count - shown).ToString(CultureInfo.InvariantCulture)).Append(" más.");
                }

                Debug.LogWarning(sb.ToString());
                return true;
            }

            for (int i = 0; i < report.Problems.Count; i++)
            {
                Debug.LogError(prefix + "ERROR " + report.Problems[i]);
            }

            Debug.LogError(prefix + "build de release (" + report.ApplicationId + ") parado por los textos (08 §4.7); problemas: "
                + report.Problems.Count.ToString(CultureInfo.InvariantCulture) + ".");
            return false;
        }

        /// <summary>Las dos tablas del proyecto (<see cref="FontCoverage.StringsPath"/> y <see cref="FontCoverage.FactsPath"/>), en UTF-8 estricto.</summary>
        public static LocCatalog ReadProjectCatalog()
        {
            string strings = LocCsv.Decode(File.ReadAllBytes(FontCoverage.DiskPath(FontCoverage.StringsPath)), LocCatalog.StringsFile);
            string facts = LocCsv.Decode(File.ReadAllBytes(FontCoverage.DiskPath(FontCoverage.FactsPath)), LocCatalog.FactsFile);
            return LocCatalog.Parse(strings, facts);
        }

        private static string Cultural(LocTable table, int row, LocStatus status)
        {
            string state = status == LocStatus.Missing ? "sin texto en es-419" : LocStatusNames.ToCsv(status);
            return "texto cultural sin " + LocStatusNames.ApprovedEs + ": " + table.File + ":"
                + table.LineAt(row).ToString(CultureInfo.InvariantCulture) + " " + table.KeyAt(row) + " (" + state + ").";
        }

        private static string List(List<string> keys, int max)
        {
            int shown = Math.Min(max, keys.Count);
            var sb = new StringBuilder();
            for (int i = 0; i < shown; i++)
            {
                sb.Append(i == 0 ? string.Empty : ", ").Append(keys[i]);
            }

            if (keys.Count > shown)
            {
                sb.Append(" y ").Append((keys.Count - shown).ToString(CultureInfo.InvariantCulture)).Append(" más");
            }

            return sb.ToString();
        }
    }
}
