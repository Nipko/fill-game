#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Localization
{
    /// <summary>
    /// Una tabla de textos (<c>strings.csv</c> o <c>facts.csv</c>; contrato 32; 08 §4.7) leída por el nombre de sus columnas,
    /// en cualquier orden:
    /// <list type="bullet">
    /// <item><c>key</c> y <c>es-419</c> (el texto fuente), obligatorias.</item>
    /// <item>Una columna por idioma (<c>en</c>, <c>pt-BR</c> y, en la tanda 2, <c>fr</c>, <c>de</c>…; vacía = pendiente).</item>
    /// <item><c>context</c>, <c>max_chars</c> (entero o vacío) y <c>notes</c>.</item>
    /// <item>El estado, de dos formas (pregunta 49 de GDD.md §6.3): <c>status</c> solo, que recorre todo el flujo para la fila
    /// entera, o <c>status</c> (el del texto fuente) más <c>status_&lt;idioma&gt;</c> por cada traducción, como la cabecera de
    /// Frutas que eligió el dueño con la 49 (a): <c>key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes</c>.</item>
    /// <item><c>args</c> [P], al final: los valores de las cifras de los textos largos (Q8-A; <see cref="LocArgs.TryParse"/>).</item>
    /// </list>
    /// Las claves van en <c>[a-z0-9_.]</c> (UI en <c>snake_case</c> y contenido con puntos; 49 a) y no se repiten. Cualquier
    /// otra columna es un error, para que una errata no se lea como un idioma. El orden alfabético de las filas lo comprueban
    /// los tests de las tablas (<see cref="UnsortedAt"/>).
    /// </summary>
    public sealed class LocTable
    {
        /// <summary>Columna de la clave.</summary>
        public const string KeyColumn = "key";

        /// <summary>Columna del contexto para el traductor.</summary>
        public const string ContextColumn = "context";

        /// <summary>Columna del límite de caracteres.</summary>
        public const string MaxCharsColumn = "max_chars";

        /// <summary>Columna del estado (del texto fuente o, sin columnas por idioma, de la fila).</summary>
        public const string StatusColumn = "status";

        /// <summary>Prefijo de las columnas de estado por idioma (<c>status_en</c>, <c>status_pt-BR</c>).</summary>
        public const string StatusPrefix = "status_";

        /// <summary>Columna de notas.</summary>
        public const string NotesColumn = "notes";

        /// <summary>Columna de los argumentos de las cifras [P].</summary>
        public const string ArgsColumn = "args";

        private readonly string[] _columns;
        private readonly string[] _locales;
        private readonly int[] _localeColumn;
        private readonly int[] _localeStatusColumn;
        private readonly string[][] _rows;
        private readonly int[] _lines;
        private readonly LocStatus[][] _status;
        private readonly int[] _maxChars;
        private readonly LocArgs[] _args;
        private readonly Dictionary<string, int> _index;
        private readonly int _keyColumn;
        private readonly int _contextColumn;
        private readonly int _notesColumn;
        private readonly int _argsColumn;

        private LocTable(string file, string[] columns, string[] locales, int[] localeColumn, int[] localeStatusColumn, bool perLocaleStatus,
            int keyColumn, int contextColumn, int notesColumn, int argsColumn, string[][] rows, int[] lines, LocStatus[][] status, int[] maxChars, LocArgs[] args,
            Dictionary<string, int> index, int unsortedAt)
        {
            File = file;
            _columns = columns;
            _locales = locales;
            _localeColumn = localeColumn;
            _localeStatusColumn = localeStatusColumn;
            PerLocaleStatus = perLocaleStatus;
            _keyColumn = keyColumn;
            _contextColumn = contextColumn;
            _notesColumn = notesColumn;
            _argsColumn = argsColumn;
            _rows = rows;
            _lines = lines;
            _status = status;
            _maxChars = maxChars;
            _args = args;
            _index = index;
            UnsortedAt = unsortedAt;
        }

        /// <summary>Tabla vacía con solo <c>key</c> y <c>es-419</c>.</summary>
        public static LocTable Empty { get; } = Parse("key,es-419\n", "(vacía)");

        /// <summary>Archivo de origen (para los mensajes).</summary>
        public string File { get; }

        /// <summary>Columnas de la cabecera, en su orden.</summary>
        public IReadOnlyList<string> Columns => _columns;

        /// <summary>Idiomas (columnas de texto), en el orden de la cabecera; es-419 siempre está.</summary>
        public IReadOnlyList<string> Locales => _locales;

        /// <summary>Si el estado va por idioma (<c>status</c> + <c>status_&lt;idioma&gt;</c>) o en una sola columna.</summary>
        public bool PerLocaleStatus { get; }

        /// <summary>Si la tabla tiene la columna <c>args</c>.</summary>
        public bool HasArgs => _argsColumn >= 0;

        /// <summary>Número de filas (sin la cabecera).</summary>
        public int Count => _rows.Length;

        /// <summary>
        /// Primera fila cuya clave no va después de la anterior en orden ordinal (filas en orden alfabético por clave, 08
        /// §4.7), o -1 si están en orden.
        /// </summary>
        public int UnsortedAt { get; }

        /// <summary>Lee una tabla desde su texto.</summary>
        public static LocTable Parse(string csv, string file)
        {
            return FromRows(LocCsv.Parse(csv, file), file);
        }

        /// <summary>Construye la tabla con los registros ya leídos por <see cref="LocCsv.Parse"/> (el primero, la cabecera).</summary>
        public static LocTable FromRows(IReadOnlyList<LocCsvRow> records, string file)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            if (records.Count == 0)
            {
                throw new LocCsvException(file, 1, "falta la cabecera");
            }

            LocCsvRow header = records[0];
            int width = header.Fields.Count;
            var columns = new string[width];
            var locales = new List<string>();
            var localeColumns = new List<int>();
            var statusByLocale = new Dictionary<string, int>(StringComparer.Ordinal);
            int keyColumn = -1;
            int statusColumn = -1;
            int contextColumn = -1;
            int maxCharsColumn = -1;
            int notesColumn = -1;
            int argsColumn = -1;
            for (int c = 0; c < width; c++)
            {
                string name = header.Fields[c];
                columns[c] = name;
                for (int p = 0; p < c; p++)
                {
                    if (columns[p] == name)
                    {
                        throw new LocCsvException(file, header.Line, "la columna «" + name + "» está repetida");
                    }
                }

                switch (name)
                {
                    case KeyColumn: keyColumn = c; break;
                    case StatusColumn: statusColumn = c; break;
                    case ContextColumn: contextColumn = c; break;
                    case MaxCharsColumn: maxCharsColumn = c; break;
                    case NotesColumn: notesColumn = c; break;
                    case ArgsColumn: argsColumn = c; break;
                    default:
                        if (name.StartsWith(StatusPrefix, StringComparison.Ordinal) && IsLocaleName(name.Substring(StatusPrefix.Length)))
                        {
                            statusByLocale[name.Substring(StatusPrefix.Length)] = c;
                        }
                        else if (IsLocaleName(name))
                        {
                            locales.Add(name);
                            localeColumns.Add(c);
                        }
                        else
                        {
                            throw new LocCsvException(file, header.Line, "columna desconocida «" + name
                                + "» (se esperan key, los idiomas, context, max_chars, status, status_<idioma>, notes y args)");
                        }

                        break;
                }
            }

            if (keyColumn < 0)
            {
                throw new LocCsvException(file, header.Line, "falta la columna «key»");
            }

            int sourceAt = locales.IndexOf(LocLocale.SourceLocale);
            if (sourceAt < 0)
            {
                throw new LocCsvException(file, header.Line, "falta la columna del texto fuente «es-419»");
            }

            bool perLocale = statusByLocale.Count > 0;
            if (perLocale && statusColumn < 0)
            {
                throw new LocCsvException(file, header.Line, "hay columnas status_<idioma> pero falta «status» (el del texto fuente)");
            }

            if (statusByLocale.ContainsKey(LocLocale.SourceLocale))
            {
                throw new LocCsvException(file, header.Line, "el estado del texto fuente va en «status», no en «status_es-419»");
            }

            var localeStatusColumns = new int[locales.Count];
            foreach (KeyValuePair<string, int> pair in statusByLocale)
            {
                if (!locales.Contains(pair.Key))
                {
                    throw new LocCsvException(file, header.Line, "«status_" + pair.Key + "» no tiene su columna de idioma «" + pair.Key + "»");
                }
            }

            for (int l = 0; l < locales.Count; l++)
            {
                if (locales[l] == LocLocale.SourceLocale)
                {
                    localeStatusColumns[l] = statusColumn;
                }
                else if (!perLocale)
                {
                    localeStatusColumns[l] = statusColumn;
                }
                else if (statusByLocale.TryGetValue(locales[l], out int sc))
                {
                    localeStatusColumns[l] = sc;
                }
                else
                {
                    throw new LocCsvException(file, header.Line, "con estado por idioma, falta «status_" + locales[l] + "»");
                }
            }

            int count = records.Count - 1;
            var rows = new string[count][];
            var lines = new int[count];
            var status = new LocStatus[count][];
            var maxChars = new int[count];
            var args = new LocArgs[count];
            var index = new Dictionary<string, int>(count, StringComparer.Ordinal);
            int unsortedAt = -1;
            for (int r = 0; r < count; r++)
            {
                LocCsvRow record = records[r + 1];
                if (record.Fields.Count != width)
                {
                    throw new LocCsvException(file, record.Line, "la fila tiene " + record.Fields.Count.ToString(CultureInfo.InvariantCulture)
                        + " campos y la cabecera " + width.ToString(CultureInfo.InvariantCulture));
                }

                var fields = new string[width];
                for (int c = 0; c < width; c++)
                {
                    fields[c] = record.Fields[c];
                }

                string key = fields[keyColumn];
                if (!IsValidKey(key))
                {
                    throw new LocCsvException(file, record.Line, "clave «" + key + "» no válida (solo a-z, 0-9, «_» y «.»)");
                }

                if (index.TryGetValue(key, out int previous))
                {
                    throw new LocCsvException(file, record.Line, "la clave «" + key + "» está repetida (ya en la línea "
                        + lines[previous].ToString(CultureInfo.InvariantCulture) + ")");
                }

                index.Add(key, r);
                if (unsortedAt < 0 && r > 0 && string.CompareOrdinal(rows[r - 1][keyColumn], key) >= 0)
                {
                    unsortedAt = r;
                }

                rows[r] = fields;
                lines[r] = record.Line;
                maxChars[r] = ParseMaxChars(maxCharsColumn < 0 ? string.Empty : fields[maxCharsColumn], file, record.Line);
                if (argsColumn >= 0 && !LocArgs.TryParse(fields[argsColumn], out args[r], out string? argsError))
                {
                    throw new LocCsvException(file, record.Line, "columna args: " + argsError);
                }

                var rowStatus = new LocStatus[locales.Count];
                for (int l = 0; l < locales.Count; l++)
                {
                    rowStatus[l] = ParseStatus(localeStatusColumns[l] < 0 ? string.Empty : fields[localeStatusColumns[l]], file, record.Line);
                }

                status[r] = rowStatus;
            }

            return new LocTable(file, columns, locales.ToArray(), localeColumns.ToArray(), localeStatusColumns, perLocale, keyColumn, contextColumn,
                notesColumn, argsColumn, rows, lines, status, maxChars, args, index, unsortedAt);
        }

        /// <summary>Fila de <paramref name="key"/>, o -1. Sin asignaciones.</summary>
        public int IndexOf(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _index.TryGetValue(key, out int row) ? row : -1;
        }

        /// <summary>Si la tabla tiene <paramref name="key"/>.</summary>
        public bool Contains(string key) => IndexOf(key) >= 0;

        /// <summary>Si la tabla tiene la columna de <paramref name="locale"/>.</summary>
        public bool HasLocale(string locale) => LocaleAt(locale) >= 0;

        /// <summary>Clave de la fila <paramref name="row"/>.</summary>
        public string KeyAt(int row) => _rows[row][_keyColumn];

        /// <summary>Línea del CSV donde empieza la fila <paramref name="row"/>.</summary>
        public int LineAt(int row) => _lines[row];

        /// <summary>Texto de la fila en <paramref name="locale"/>; vacío si no hay columna o no hay traducción.</summary>
        public string TextAt(int row, string locale)
        {
            int l = LocaleAt(locale);
            return l < 0 ? string.Empty : _rows[row][_localeColumn[l]];
        }

        /// <summary>Texto de <paramref name="key"/> en <paramref name="locale"/>; vacío si no está.</summary>
        public string Text(string key, string locale)
        {
            int row = IndexOf(key);
            return row < 0 ? string.Empty : TextAt(row, locale);
        }

        /// <summary>Contexto para el traductor (vacío si no hay columna).</summary>
        public string ContextAt(int row) => _contextColumn < 0 ? string.Empty : _rows[row][_contextColumn];

        /// <summary>Notas (vacío si no hay columna).</summary>
        public string NotesAt(int row) => _notesColumn < 0 ? string.Empty : _rows[row][_notesColumn];

        /// <summary>Texto de la columna <c>args</c> (vacío si no hay columna).</summary>
        public string ArgsTextAt(int row) => _argsColumn < 0 ? string.Empty : _rows[row][_argsColumn];

        /// <summary>Argumentos de la columna <c>args</c> ya leídos.</summary>
        public LocArgs ArgsAt(int row) => _args[row];

        /// <summary>Límite de caracteres del texto fuente (0 = sin límite).</summary>
        public int MaxCharsAt(int row) => _maxChars[row];

        /// <summary>
        /// Estado de la fila en <paramref name="locale"/>: <see cref="LocStatus.Missing"/> sin texto en ese idioma. En es-419,
        /// el de <c>status</c> (con una sola columna, lo que pasa de <c>aprobado_es</c> cuenta como <c>aprobado_es</c>). En una
        /// traducción, el de <c>status_&lt;idioma&gt;</c> o, con una sola columna, <c>traducido</c> o <c>revisado_nativo</c> si
        /// la fila llegó ahí y <see cref="LocStatus.Draft"/> si no.
        /// </summary>
        public LocStatus StatusAt(int row, string locale)
        {
            int l = LocaleAt(locale);
            if (l < 0 || _rows[row][_localeColumn[l]].Length == 0)
            {
                return LocStatus.Missing;
            }

            LocStatus status = _status[row][l];
            if (PerLocaleStatus)
            {
                return status;
            }

            if (locale == LocLocale.SourceLocale)
            {
                return status > LocStatus.ApprovedEs ? LocStatus.ApprovedEs : status;
            }

            return status >= LocStatus.Translated ? status : LocStatus.Draft;
        }

        /// <summary>
        /// Si la fila se puede enseñar en un build de release en <paramref name="locale"/> (08 §4.7; D-018, Q5-A y Q10-A): el
        /// texto fuente en <c>aprobado_es</c> y, en una traducción, además <c>revisado_nativo</c> (nunca una traducción sin
        /// revisión en algo que vea el jugador) [P].
        /// </summary>
        public bool IsApprovedAt(int row, string locale)
        {
            if (StatusAt(row, LocLocale.SourceLocale) < LocStatus.ApprovedEs)
            {
                return false;
            }

            return locale == LocLocale.SourceLocale || StatusAt(row, locale) == LocStatus.NativeReviewed;
        }

        /// <summary>Si todas las filas tienen texto en <paramref name="locale"/> (false si no hay columna).</summary>
        public bool IsComplete(string locale)
        {
            int l = LocaleAt(locale);
            if (l < 0)
            {
                return false;
            }

            int column = _localeColumn[l];
            for (int r = 0; r < _rows.Length; r++)
            {
                if (_rows[r][column].Length == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Si <paramref name="name"/> tiene forma de idioma: <c>es-419</c>, <c>pt-BR</c>, <c>en</c>, <c>zh-Hant</c>…</summary>
        public static bool IsLocaleName(string name)
        {
            if (name == null || name.Length < 2)
            {
                return false;
            }

            int i = 0;
            while (i < name.Length && name[i] >= 'a' && name[i] <= 'z')
            {
                i++;
            }

            if (i < 2 || i > 3)
            {
                return false;
            }

            while (i < name.Length)
            {
                if (name[i] != '-')
                {
                    return false;
                }

                int start = ++i;
                while (i < name.Length && ((name[i] >= 'a' && name[i] <= 'z') || (name[i] >= 'A' && name[i] <= 'Z') || (name[i] >= '0' && name[i] <= '9')))
                {
                    i++;
                }

                if (i - start < 2 || i - start > 8)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Si <paramref name="key"/> vale como clave: no vacía y solo con a-z, 0-9, «_» y «.».</summary>
        public static bool IsValidKey(string? key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < key!.Length; i++)
            {
                char c = key[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.'))
                {
                    return false;
                }
            }

            return true;
        }

        private int LocaleAt(string locale)
        {
            for (int l = 0; l < _locales.Length; l++)
            {
                if (string.Equals(_locales[l], locale, StringComparison.Ordinal))
                {
                    return l;
                }
            }

            return -1;
        }

        private static int ParseMaxChars(string text, string file, int line)
        {
            if (text.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    throw new LocCsvException(file, line, "max_chars «" + text + "» no es un entero");
                }
            }

            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value <= 0)
            {
                throw new LocCsvException(file, line, "max_chars «" + text + "» fuera de rango (entero ≥ 1 o vacío)");
            }

            return value;
        }

        private static LocStatus ParseStatus(string text, string file, int line)
        {
            if (text.Length == 0)
            {
                return LocStatus.Draft;
            }

            if (!LocStatusNames.TryParse(text, out LocStatus status))
            {
                throw new LocCsvException(file, line, "estado «" + text
                    + "» desconocido (borrador, pendiente_asesor, aprobado_es, traducido o revisado_nativo)");
            }

            return status;
        }
    }
}
