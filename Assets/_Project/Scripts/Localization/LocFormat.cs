#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Localization
{
    /// <summary>
    /// Plantillas de las tablas de textos (contrato 32; 08 §4.7; propuesta de idiomas §3.2, reglas 2 y 3, y §3.3), con el
    /// subconjunto de Smart Strings de Unity Localization que se usará en la fase 3:
    /// <list type="bullet">
    /// <item><c>{nombre}</c>: el argumento con ese nombre (<see cref="LocArgs"/>). Nada de marcadores posicionales
    /// (<c>{0}</c>). Un marcador sin argumento se queda tal cual, para que se vea.</item>
    /// <item><c>{n:plural:a|b[|c]}</c>: la forma que toca según las reglas CLDR del idioma (<see cref="LocPlural"/>); dentro de
    /// cada forma, <c>{}</c> es el número y <c>{otro}</c>, otro argumento: «{n:plural:Te falta {} semilla|Te faltan {}
    /// semillas}».</item>
    /// <item><c>{sep}</c>: marcador de corte. El código lo da como salto de línea («\n») o como el separador de la tabla
    /// («·» con sus espacios), uno a uno (<c>breaks</c>): así una pantalla que se ajusta sola, como el banner del mundo, elige
    /// dónde partir sin trocear el texto. Sin indicación, es un salto de línea.</item>
    /// </list>
    /// Las cifras llevan los separadores de la región (<see cref="LocNumberFormat"/>). Una plantilla sin llaves se devuelve tal
    /// cual, sin asignar memoria; con marcadores, el formato asigna la cadena resultado: se aplica al construir la pantalla,
    /// nunca por fotograma.
    /// </summary>
    public static class LocFormat
    {
        /// <summary>Nombre del marcador de corte.</summary>
        public const string SeparatorName = "sep";

        /// <summary>El marcador de corte tal como va en la plantilla.</summary>
        public const string SeparatorMarker = "{sep}";

        /// <summary>Lo que vale <c>{sep}</c> cuando se parte la línea.</summary>
        public const string LineBreak = "\n";

        private const string PluralKeyword = "plural:";

        [ThreadStatic]
        private static StringBuilder? s_builder;

        [ThreadStatic]
        private static char[]? s_digits;

        /// <summary>
        /// Aplica <paramref name="args"/> a <paramref name="template"/> con los plurales de <paramref name="locale"/> y las
        /// cifras de <paramref name="numbers"/>; cada <c>{sep}</c> es un salto de línea.
        /// </summary>
        public static string Format(string template, in LocArgs args, string locale, in LocNumberFormat numbers)
        {
            return Format(template, args, LocArgs.None, locale, numbers, LineBreak, 0);
        }

        /// <summary>
        /// Como <see cref="Format(string, in LocArgs, string, in LocNumberFormat)"/>, pero el <c>{sep}</c> número i (desde 0)
        /// es un salto de línea si el bit i de <paramref name="breaks"/> está puesto, y <paramref name="separator"/> si no.
        /// </summary>
        public static string Format(string template, in LocArgs args, string locale, in LocNumberFormat numbers, string separator, int breaks)
        {
            return Format(template, args, LocArgs.None, locale, numbers, separator, breaks);
        }

        /// <summary>
        /// Forma completa: los marcadores buscan primero en <paramref name="args"/> y después en <paramref name="fallback"/>
        /// (los de la columna <c>args</c> de la tabla).
        /// </summary>
        public static string Format(string template, in LocArgs args, in LocArgs fallback, string locale, in LocNumberFormat numbers, string separator, int breaks)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }

            if (template.IndexOf('{') < 0)
            {
                return template;
            }

            StringBuilder sb = s_builder ??= new StringBuilder(256);
            sb.Clear();
            int sepIndex = 0;
            Emit(sb, template, 0, template.Length, args, fallback, locale, numbers, separator, breaks, ref sepIndex, false, default);
            string result = sb.ToString();
            sb.Clear();
            return result;
        }

        /// <summary>Número de <c>{sep}</c> de <paramref name="template"/> (los bits que cuentan en <c>breaks</c>).</summary>
        public static int SeparatorCount(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            int count = 0;
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '{' && TryParseMarker(template, i, template.Length, out Marker m))
                {
                    if (m.IsSeparator(template))
                    {
                        count++;
                    }

                    i = m.End - 1;
                }
            }

            return count;
        }

        /// <summary>
        /// Comprueba la sintaxis de <paramref name="template"/>: llaves cerradas, nombres <c>[a-z][a-z0-9_]*</c> (ningún
        /// marcador posicional), plurales de 2 o 3 formas, <c>{}</c> solo dentro de un plural y <c>{sep}</c> fuera de ellos.
        /// Devuelve null si está bien o el problema, en español.
        /// </summary>
        public static string? Validate(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            return ValidateRange(template, 0, template.Length, false);
        }

        /// <summary>
        /// Añade a <paramref name="names"/> los nombres de los marcadores de <paramref name="template"/> (sin repetir, sin
        /// <c>sep</c> ni <c>{}</c>, también los de dentro de los plurales), para comprobar que una traducción usa los mismos.
        /// </summary>
        public static void CollectNames(string template, List<string> names)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            CollectRange(template, 0, template.Length, names);
        }

        /// <summary>Marcador <c>{…}</c> reconocido en una plantilla.</summary>
        internal readonly struct Marker
        {
            public readonly int Open;
            public readonly int End;
            public readonly int NameStart;
            public readonly int NameLength;
            public readonly int FormsStart;
            public readonly int FormsEnd;

            public Marker(int open, int end, int nameStart, int nameLength, int formsStart, int formsEnd)
            {
                Open = open;
                End = end;
                NameStart = nameStart;
                NameLength = nameLength;
                FormsStart = formsStart;
                FormsEnd = formsEnd;
            }

            public bool IsPlural => FormsStart >= 0;

            public bool IsSeparator(string s)
            {
                return !IsPlural && NameLength == SeparatorName.Length && string.CompareOrdinal(s, NameStart, SeparatorName, 0, NameLength) == 0;
            }
        }

        /// <summary>
        /// Reconoce el marcador que abre en <paramref name="open"/> (que debe ser «{») sin pasar de <paramref name="limit"/>:
        /// <c>{}</c>, <c>{nombre}</c> o <c>{nombre:plural:…}</c> con las llaves de dentro equilibradas.
        /// </summary>
        internal static bool TryParseMarker(string s, int open, int limit, out Marker marker)
        {
            marker = default;
            int i = open + 1;
            int nameStart = i;
            while (i < limit && IsNameChar(s[i]))
            {
                i++;
            }

            int nameLength = i - nameStart;
            if (i >= limit)
            {
                return false;
            }

            if (s[i] == '}')
            {
                marker = new Marker(open, i + 1, nameStart, nameLength, -1, -1);
                return true;
            }

            if (s[i] != ':' || nameLength == 0 || i + 1 + PluralKeyword.Length > limit
                || string.CompareOrdinal(s, i + 1, PluralKeyword, 0, PluralKeyword.Length) != 0)
            {
                return false;
            }

            int formsStart = i + 1 + PluralKeyword.Length;
            int depth = 0;
            for (int j = formsStart; j < limit; j++)
            {
                char c = s[j];
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    if (depth == 0)
                    {
                        marker = new Marker(open, j + 1, nameStart, nameLength, formsStart, j);
                        return true;
                    }

                    depth--;
                }
            }

            return false;
        }

        /// <summary>Fin de la forma que empieza en <paramref name="from"/>: el siguiente «|» de su nivel o <paramref name="formsEnd"/>.</summary>
        internal static int FormEnd(string s, int from, int formsEnd)
        {
            int depth = 0;
            for (int j = from; j < formsEnd; j++)
            {
                char c = s[j];
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                }
                else if (c == '|' && depth == 0)
                {
                    return j;
                }
            }

            return formsEnd;
        }

        /// <summary>Número de formas de un plural.</summary>
        internal static int FormCount(string s, in Marker m)
        {
            int count = 1;
            int at = m.FormsStart;
            while (true)
            {
                int end = FormEnd(s, at, m.FormsEnd);
                if (end >= m.FormsEnd)
                {
                    return count;
                }

                count++;
                at = end + 1;
            }
        }

        private static void Emit(StringBuilder sb, string s, int start, int end, in LocArgs args, in LocArgs fallback, string locale,
            in LocNumberFormat numbers, string separator, int breaks, ref int sepIndex, bool hasValue, in LocArg value)
        {
            int i = start;
            while (i < end)
            {
                char c = s[i];
                if (c != '{' || !TryParseMarker(s, i, end, out Marker m))
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                if (m.NameLength == 0)
                {
                    if (hasValue)
                    {
                        AppendValue(sb, value, numbers);
                    }
                    else
                    {
                        sb.Append(s, m.Open, m.End - m.Open);
                    }
                }
                else if (m.IsSeparator(s))
                {
                    bool lineBreak = sepIndex < 31 && ((breaks >> sepIndex) & 1) != 0;
                    sb.Append(lineBreak ? LineBreak : separator);
                    sepIndex++;
                }
                else if (!args.TryGet(s, m.NameStart, m.NameLength, out LocArg arg)
                    && !fallback.TryGet(s, m.NameStart, m.NameLength, out arg))
                {
                    sb.Append(s, m.Open, m.End - m.Open);
                }
                else if (!m.IsPlural)
                {
                    AppendValue(sb, arg, numbers);
                }
                else if (!arg.IsNumeric)
                {
                    sb.Append(s, m.Open, m.End - m.Open);
                }
                else
                {
                    LocPluralCategory category = arg.Kind == LocArgKind.Decimal
                        ? LocPlural.Category(locale, arg.Real, arg.Decimals)
                        : LocPlural.Category(locale, arg.Integer);
                    int index = LocPlural.FormIndex(category, FormCount(s, m));
                    int formStart = m.FormsStart;
                    for (int k = 0; k < index; k++)
                    {
                        formStart = FormEnd(s, formStart, m.FormsEnd) + 1;
                    }

                    int formEnd = FormEnd(s, formStart, m.FormsEnd);
                    Emit(sb, s, formStart, formEnd, args, fallback, locale, numbers, separator, breaks, ref sepIndex, true, arg);
                }

                i = m.End;
            }
        }

        private static void AppendValue(StringBuilder sb, in LocArg arg, in LocNumberFormat numbers)
        {
            char[] digits = s_digits ??= new char[64];
            switch (arg.Kind)
            {
                case LocArgKind.Text:
                    sb.Append(arg.Text);
                    break;
                case LocArgKind.Number:
                    sb.Append(digits, 0, LocNumber.Write(digits, 0, arg.Integer, true, numbers));
                    break;
                case LocArgKind.Digits:
                    sb.Append(digits, 0, LocNumber.Write(digits, 0, arg.Integer, false, numbers));
                    break;
                case LocArgKind.Decimal:
                    sb.Append(digits, 0, LocNumber.Write(digits, 0, arg.Real, arg.Decimals, true, numbers));
                    break;
            }
        }

        private static string? ValidateRange(string s, int start, int end, bool inPlural)
        {
            for (int i = start; i < end; i++)
            {
                char c = s[i];
                if (c == '}')
                {
                    return "llave de cierre suelta en la posición " + Position(i);
                }

                if (c != '{')
                {
                    continue;
                }

                if (!TryParseMarker(s, i, end, out Marker m))
                {
                    return "marcador sin cerrar o mal escrito en la posición " + Position(i)
                        + " (se espera {nombre}, {nombre:plural:a|b[|c]} o {sep})";
                }

                if (m.NameLength == 0)
                {
                    if (!inPlural)
                    {
                        return "«{}» solo vale dentro de un plural (posición " + Position(i) + ")";
                    }
                }
                else if (!LocArgs.IsValidName(s, m.NameStart, m.NameLength))
                {
                    return "marcador «{" + s.Substring(m.NameStart, m.NameLength) + "}» no válido en la posición " + Position(i)
                        + ": nombres [a-z][a-z0-9_]*, nunca posicionales";
                }
                else if (m.IsSeparator(s))
                {
                    if (inPlural)
                    {
                        return "{sep} no va dentro de un plural (posición " + Position(i) + ")";
                    }
                }
                else if (m.IsPlural)
                {
                    if (m.NameLength == SeparatorName.Length && string.CompareOrdinal(s, m.NameStart, SeparatorName, 0, m.NameLength) == 0)
                    {
                        return "{sep} no admite plural (posición " + Position(i) + ")";
                    }

                    int forms = FormCount(s, m);
                    if (forms < 2 || forms > 3)
                    {
                        return "el plural de la posición " + Position(i) + " tiene " + forms.ToString(CultureInfo.InvariantCulture)
                            + " formas: van 2 (one|other) o 3 (one|many|other)";
                    }

                    int formStart = m.FormsStart;
                    while (true)
                    {
                        int formEnd = FormEnd(s, formStart, m.FormsEnd);
                        string? error = ValidateRange(s, formStart, formEnd, true);
                        if (error != null)
                        {
                            return error;
                        }

                        if (formEnd >= m.FormsEnd)
                        {
                            break;
                        }

                        formStart = formEnd + 1;
                    }
                }

                i = m.End - 1;
            }

            return null;
        }

        private static void CollectRange(string s, int start, int end, List<string> names)
        {
            for (int i = start; i < end; i++)
            {
                if (s[i] != '{' || !TryParseMarker(s, i, end, out Marker m))
                {
                    continue;
                }

                if (m.NameLength > 0 && !m.IsSeparator(s))
                {
                    string name = s.Substring(m.NameStart, m.NameLength);
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }

                if (m.IsPlural)
                {
                    CollectRange(s, m.FormsStart, m.FormsEnd, names);
                }

                i = m.End - 1;
            }
        }

        private static string Position(int index) => index.ToString(CultureInfo.InvariantCulture);

        private static bool IsNameChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
        }
    }
}
