#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Localization
{
    /// <summary>
    /// Separadores de una región para las cifras (D-018, Q8-A: el formato de la región del teléfono, con una sola función):
    /// el de miles (<see cref="Group"/>) y el decimal (<see cref="Decimal"/>). <see cref="Tag"/> es la fila de la tabla de
    /// <see cref="LocNumber"/> que se eligió («es-CO», «en»…).
    /// </summary>
    public readonly struct LocNumberFormat : IEquatable<LocNumberFormat>
    {
        /// <summary>Fila de la tabla («es-CO», «es-MX», «fr»…).</summary>
        public readonly string Tag;

        /// <summary>Separador de miles: «.», «,» o U+00A0.</summary>
        public readonly char Group;

        /// <summary>Separador decimal: «,» o «.».</summary>
        public readonly char Decimal;

        /// <summary>Formato con su fila y sus dos separadores.</summary>
        public LocNumberFormat(string tag, char group, char @decimal)
        {
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            Group = group;
            Decimal = @decimal;
        }

        /// <inheritdoc/>
        public bool Equals(LocNumberFormat other)
        {
            return string.Equals(Tag, other.Tag, StringComparison.Ordinal) && Group == other.Group && Decimal == other.Decimal;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LocNumberFormat other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => ((Group * 397) ^ Decimal) ^ (Tag == null ? 0 : Tag.Length);

        /// <inheritdoc/>
        public override string ToString() => Tag + " (" + (Group == LocNumber.NoBreakSpace ? "U+00A0" : Group.ToString()) + " " + Decimal + ")";
    }

    /// <summary>
    /// Cifras con los separadores de la región (D-018, Q8-A; propuesta de idiomas §3.6; arquitectura de la tanda 4, §1), con
    /// una tabla propia en lugar de <c>CultureInfo</c>: los datos de cultura de IL2CPP en Android pueden no coincidir con los
    /// de .NET [NV], y así el formato no depende de la cultura del equipo.
    /// <list type="bullet">
    /// <item>«1.234,5»: es-CO, es-AR, es-ES, pt-BR, de y tr.</item>
    /// <item>«1,234.5»: es-419, es-MX, es-PE, en y ja; y es-US, es-GT, es-HN, es-NI, es-PA, es-SV, es-DO, es-PR, es-CU, es-BZ
    /// y es-BR.</item>
    /// <item>«1 234,5» con U+00A0: fr, ru, pt-PT y es-CR (en francés, U+202F se cambia por U+00A0; Q9-A).</item>
    /// </list>
    /// Las filas de español son todas las regiones es-* de ICU cuyo formato no es el de es-CO, medidas con .NET 10.0.13 e ICU
    /// 72.1 (Windows 11, compilación 26200) el 2026-10-14, como la tabla de la propuesta de idiomas §3.6 [V en .NET; en el
    /// teléfono, NV: el ICU de Android puede traer otro CLDR]. Las demás regiones de español de ICU (es-AR, es-BO, es-CL, es-EC,
    /// es-ES, es-GQ, es-PH, es-PY, es-UY y es-VE) escriben como es-CO.
    /// Un idioma sin región toma su fila por defecto: es → es-CO [P] (lo de hoy, «1.000»), pt → pt-BR y el resto, la de su
    /// idioma. Una región que no está en la tabla toma también la de su idioma (es-CL → es-CO, que coincide con ICU; pt-AO →
    /// pt-BR [P]); un idioma que no está, la de en [P]. Siempre agrupa desde 4 cifras («1.000»), como el glosario §10.
    /// <see cref="Write(char[], int, long, bool, in LocNumberFormat)"/> no asigna memoria: los recuentos por fotograma lo usan
    /// con <c>TMP_Text.SetCharArray</c>.
    /// </summary>
    public static class LocNumber
    {
        /// <summary>Espacio duro (U+00A0), separador de miles de fr, ru y pt-PT.</summary>
        public const char NoBreakSpace = '\u00A0';

        /// <summary>Máximo de decimales de <see cref="Write(char[], int, double, int, bool, in LocNumberFormat)"/>.</summary>
        public const int MaxDecimals = 9;

        /// <summary>Fila de un español sin región [P] (arquitectura de la tanda 4, §1: «lo de hoy»).</summary>
        public const string SpanishDefaultTag = "es-CO";

        /// <summary>Fila de un idioma que no está en la tabla [P].</summary>
        public const string FallbackTag = "en";

        /// <summary>Todos los caracteres que puede escribir: cifras, signo y separadores (para los glifos de la fuente, B1).</summary>
        public const string Characters = "0123456789-.,\u00A0";

        private static readonly LocNumberFormat[] Table =
        {
            new LocNumberFormat("es-CO", '.', ','),
            new LocNumberFormat("es-AR", '.', ','),
            new LocNumberFormat("es-ES", '.', ','),
            new LocNumberFormat("pt-BR", '.', ','),
            new LocNumberFormat("de", '.', ','),
            new LocNumberFormat("tr", '.', ','),
            new LocNumberFormat("es-419", ',', '.'),
            new LocNumberFormat("es-MX", ',', '.'),
            new LocNumberFormat("es-PE", ',', '.'),
            new LocNumberFormat("en", ',', '.'),
            new LocNumberFormat("ja", ',', '.'),
            new LocNumberFormat("fr", NoBreakSpace, ','),
            new LocNumberFormat("ru", NoBreakSpace, ','),
            new LocNumberFormat("pt-PT", NoBreakSpace, ','),

            // Regiones de español que no escriben como es-CO (ICU 72.1 desde .NET 10.0.13, 2026-10-14; en el teléfono, NV).
            new LocNumberFormat("es-US", ',', '.'),
            new LocNumberFormat("es-GT", ',', '.'),
            new LocNumberFormat("es-HN", ',', '.'),
            new LocNumberFormat("es-NI", ',', '.'),
            new LocNumberFormat("es-PA", ',', '.'),
            new LocNumberFormat("es-SV", ',', '.'),
            new LocNumberFormat("es-DO", ',', '.'),
            new LocNumberFormat("es-PR", ',', '.'),
            new LocNumberFormat("es-CU", ',', '.'),
            new LocNumberFormat("es-BZ", ',', '.'),
            new LocNumberFormat("es-BR", ',', '.'),
            new LocNumberFormat("es-CR", NoBreakSpace, ','),
        };

        private static readonly ulong[] Pow10 =
        {
            1UL, 10UL, 100UL, 1000UL, 10000UL, 100000UL, 1000000UL, 10000000UL, 100000000UL, 1000000000UL,
        };

        /// <summary>Las filas de la tabla, en su orden.</summary>
        public static IReadOnlyList<LocNumberFormat> Formats => Table;

        /// <summary>
        /// Formato para una etiqueta de idioma (BCP 47 o con «_»: «es-CO», «es_MX», «pt», «zh-Hant-TW», «en-US-u-…»). Primero la
        /// fila exacta de idioma y región; si no, la del idioma (es → es-CO, pt → pt-BR); si no, la de en. Null o vacío → en.
        /// </summary>
        public static LocNumberFormat For(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return Find(FallbackTag);
            }

            LocLocale.SplitTag(tag!, out string language, out string? region);
            if (region != null && TryFind(language + "-" + region, out LocNumberFormat exact))
            {
                return exact;
            }

            if (language == "es")
            {
                return Find(SpanishDefaultTag);
            }

            if (language == "pt")
            {
                return Find("pt-BR");
            }

            return TryFind(language, out LocNumberFormat byLanguage) ? byLanguage : Find(FallbackTag);
        }

        /// <summary>Caracteres que ocupa <paramref name="value"/> (con signo y, si <paramref name="grouped"/>, separadores).</summary>
        public static int Length(long value, bool grouped)
        {
            bool negative = value < 0;
            int digits = CountDigits(Magnitude(value));
            return digits + (grouped ? (digits - 1) / 3 : 0) + (negative ? 1 : 0);
        }

        /// <summary>
        /// Escribe <paramref name="value"/> en <paramref name="buffer"/> desde <paramref name="pos"/>, con separador de miles si
        /// <paramref name="grouped"/>, y devuelve la posición siguiente. Los negativos llevan «-». Sin asignaciones.
        /// </summary>
        public static int Write(char[] buffer, int pos, long value, bool grouped, in LocNumberFormat format)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            bool negative = value < 0;
            ulong magnitude = Magnitude(value);
            int digits = CountDigits(magnitude);
            int length = digits + (grouped ? (digits - 1) / 3 : 0) + (negative ? 1 : 0);
            if (pos < 0 || pos > buffer.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "No cabe el número en el buffer.");
            }

            WriteDigits(buffer, pos + length - 1, magnitude, grouped, format.Group);
            if (negative)
            {
                buffer[pos] = '-';
            }

            return pos + length;
        }

        /// <summary>
        /// Escribe <paramref name="value"/> con <paramref name="decimals"/> decimales (0-9, redondeo «la mitad hacia fuera»)
        /// y los separadores de <paramref name="format"/>. Devuelve la posición siguiente. Sin asignaciones.
        /// </summary>
        public static int Write(char[] buffer, int pos, double value, int decimals, bool grouped, in LocNumberFormat format)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Split(value, decimals, out bool negative, out ulong integer, out ulong fraction);
            int digits = CountDigits(integer);
            int intLength = digits + (grouped ? (digits - 1) / 3 : 0);
            int length = intLength + (negative ? 1 : 0) + (decimals > 0 ? decimals + 1 : 0);
            if (pos < 0 || pos > buffer.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "No cabe el número en el buffer.");
            }

            int at = pos;
            if (negative)
            {
                buffer[at++] = '-';
            }

            WriteDigits(buffer, at + intLength - 1, integer, grouped, format.Group);
            at += intLength;
            if (decimals > 0)
            {
                buffer[at++] = format.Decimal;
                for (int i = decimals - 1; i >= 0; i--)
                {
                    buffer[at + i] = (char)('0' + (int)(fraction % 10UL));
                    fraction /= 10UL;
                }

                at += decimals;
            }

            return at;
        }

        /// <summary>El entero con separador de miles (si <paramref name="grouped"/>), como cadena.</summary>
        public static string Format(long value, bool grouped, in LocNumberFormat format)
        {
            var buffer = new char[32];
            int n = Write(buffer, 0, value, grouped, format);
            return new string(buffer, 0, n);
        }

        /// <summary>El número con <paramref name="decimals"/> decimales y separador de miles, como cadena.</summary>
        public static string Format(double value, int decimals, in LocNumberFormat format)
        {
            var buffer = new char[64];
            int n = Write(buffer, 0, value, decimals, true, format);
            return new string(buffer, 0, n);
        }

        /// <summary>
        /// Redondea <paramref name="value"/> a <paramref name="decimals"/> decimales (en <see cref="decimal"/>, la mitad hacia
        /// fuera) y lo parte en signo, parte entera y parte decimal (como entero de <paramref name="decimals"/> cifras). El
        /// cero redondeado no lleva signo. Sin asignaciones.
        /// </summary>
        internal static void Split(double value, int decimals, out bool negative, out ulong integer, out ulong fraction)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentException("El valor debe ser un número finito.", nameof(value));
            }

            if (decimals < 0 || decimals > MaxDecimals)
            {
                throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Entre 0 y 9 decimales.");
            }

            if (Math.Abs(value) >= 1e18)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Fuera del rango de las cifras del juego (|v| < 10^18).");
            }

            decimal d = Math.Round((decimal)value, decimals, MidpointRounding.AwayFromZero);
            negative = d < 0m;
            if (negative)
            {
                d = -d;
            }

            decimal whole = decimal.Truncate(d);
            integer = (ulong)whole;
            fraction = (ulong)((d - whole) * Pow10[decimals]);
            if (integer == 0UL && fraction == 0UL)
            {
                negative = false;
            }
        }

        private static void WriteDigits(char[] buffer, int last, ulong magnitude, bool grouped, char group)
        {
            int at = last;
            int written = 0;
            do
            {
                if (grouped && written > 0 && written % 3 == 0)
                {
                    buffer[at--] = group;
                }

                buffer[at--] = (char)('0' + (int)(magnitude % 10UL));
                magnitude /= 10UL;
                written++;
            }
            while (magnitude > 0UL);
        }

        private static ulong Magnitude(long value)
        {
            return value < 0 ? (ulong)(-(value + 1)) + 1UL : (ulong)value;
        }

        private static int CountDigits(ulong value)
        {
            int digits = 1;
            while (value >= 10UL)
            {
                value /= 10UL;
                digits++;
            }

            return digits;
        }

        private static LocNumberFormat Find(string tag)
        {
            TryFind(tag, out LocNumberFormat format);
            return format;
        }

        private static bool TryFind(string tag, out LocNumberFormat format)
        {
            for (int i = 0; i < Table.Length; i++)
            {
                if (string.Equals(Table[i].Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    format = Table[i];
                    return true;
                }
            }

            format = default;
            return false;
        }
    }
}
