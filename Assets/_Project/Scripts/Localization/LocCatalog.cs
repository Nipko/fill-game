#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Localization
{
    /// <summary>
    /// Las tablas de textos que lee el juego (08 §4.7; contrato 32): <c>strings.csv</c> (UI, tutorial, tienda, Ajustes,
    /// modales, avisos y etiquetas del lector de pantalla) y <c>facts.csv</c> (textos largos de fruta, fauna y el Mapa del
    /// río). <c>glossary.csv</c> no lo lee el juego (L2c). Inmutable. Una clave no puede estar en las dos tablas, para que
    /// <see cref="Loc.Has"/> y <see cref="StatusOf"/> no sean ambiguos.
    /// </summary>
    public sealed class LocCatalog
    {
        /// <summary>Nombre de la tabla de UI en los mensajes.</summary>
        public const string StringsFile = "strings.csv";

        /// <summary>Nombre de la tabla de textos largos en los mensajes.</summary>
        public const string FactsFile = "facts.csv";

        private readonly Dictionary<string, bool> _complete = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>Catálogo con la tabla de UI y, si la hay, la de textos largos.</summary>
        public LocCatalog(LocTable strings, LocTable? facts = null)
        {
            Strings = strings ?? throw new ArgumentNullException(nameof(strings));
            Facts = facts ?? LocTable.Empty;
            for (int r = 0; r < Facts.Count; r++)
            {
                int other = Strings.IndexOf(Facts.KeyAt(r));
                if (other >= 0)
                {
                    throw new LocCsvException(Facts.File, Facts.LineAt(r), "la clave «" + Facts.KeyAt(r) + "» ya está en " + Strings.File
                        + " (línea " + Strings.LineAt(other).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
                }
            }

            for (int l = 0; l < Strings.Locales.Count; l++)
            {
                _complete[Strings.Locales[l]] = Strings.IsComplete(Strings.Locales[l]);
            }
        }

        /// <summary>Catálogo sin textos (solo es-419, sin filas).</summary>
        public static LocCatalog Empty { get; } = new LocCatalog(LocTable.Empty);

        /// <summary>Tabla de UI (<c>strings.csv</c>), la de <see cref="Loc.T(string)"/>.</summary>
        public LocTable Strings { get; }

        /// <summary>Tabla de textos largos (<c>facts.csv</c>), la de <see cref="Loc.Fact(string)"/>; vacía si no la hay.</summary>
        public LocTable Facts { get; }

        /// <summary>Idiomas de la UI (columnas de <c>strings.csv</c>).</summary>
        public IReadOnlyList<string> Locales => Strings.Locales;

        /// <summary>Lee las dos tablas desde su texto (<paramref name="factsCsv"/> puede ser null).</summary>
        public static LocCatalog Parse(string stringsCsv, string? factsCsv)
        {
            if (stringsCsv == null)
            {
                throw new ArgumentNullException(nameof(stringsCsv));
            }

            LocTable strings = LocTable.Parse(stringsCsv, StringsFile);
            LocTable? facts = factsCsv == null ? null : LocTable.Parse(factsCsv, FactsFile);
            return new LocCatalog(strings, facts);
        }

        /// <summary>Si la UI tiene columna para <paramref name="locale"/> (en-XA no es una columna).</summary>
        public bool HasLocale(string locale)
        {
            return locale != null && Strings.HasLocale(locale);
        }

        /// <summary>
        /// Si todas las claves de UI tienen texto en <paramref name="locale"/> (D-018, Q7-A: el selector solo ofrece esos;
        /// Q10-A: la UI incompleta para un build de release). en-XA está completo si lo está es-419, que es su fuente.
        /// </summary>
        public bool IsComplete(string locale)
        {
            if (locale == null)
            {
                return false;
            }

            if (LocLocale.IsPseudo(locale))
            {
                locale = LocLocale.SourceLocale;
            }

            return _complete.TryGetValue(locale, out bool complete) && complete;
        }

        /// <summary>Si alguna de las dos tablas tiene <paramref name="key"/>.</summary>
        public bool Contains(string key)
        {
            return Strings.Contains(key) || Facts.Contains(key);
        }

        /// <summary>
        /// Estado de <paramref name="key"/> en <paramref name="locale"/> (<see cref="LocTable.StatusAt"/>), en la tabla donde
        /// esté; <see cref="LocStatus.Missing"/> si no está. en-XA da el de es-419.
        /// </summary>
        public LocStatus StatusOf(string key, string locale)
        {
            string text = TextLocale(locale);
            int row = Strings.IndexOf(key);
            if (row >= 0)
            {
                return Strings.StatusAt(row, text);
            }

            row = Facts.IndexOf(key);
            return row >= 0 ? Facts.StatusAt(row, text) : LocStatus.Missing;
        }

        /// <summary>
        /// Si <paramref name="key"/> se puede enseñar en un build de release en <paramref name="locale"/>
        /// (<see cref="LocTable.IsApprovedAt"/>); false si no está. en-XA da lo de es-419.
        /// </summary>
        public bool IsApproved(string key, string locale)
        {
            string text = TextLocale(locale);
            int row = Strings.IndexOf(key);
            if (row >= 0)
            {
                return Strings.IsApprovedAt(row, text);
            }

            row = Facts.IndexOf(key);
            return row >= 0 && Facts.IsApprovedAt(row, text);
        }

        /// <summary>Columna de texto de <paramref name="locale"/>: la de es-419 para en-XA.</summary>
        internal static string TextLocale(string locale)
        {
            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            return LocLocale.IsPseudo(locale) ? LocLocale.SourceLocale : locale;
        }
    }
}
