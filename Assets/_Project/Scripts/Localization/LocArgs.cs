#nullable enable
using System;
using System.Globalization;

namespace AF.Localization
{
    /// <summary>Tipo del valor de un <see cref="LocArg"/>.</summary>
    public enum LocArgKind : byte
    {
        /// <summary>Sin valor (la ranura vacía de un <see cref="LocArgs"/>).</summary>
        None = 0,

        /// <summary>Texto que se inserta tal cual (otra cadena ya localizada, un nombre propio).</summary>
        Text = 1,

        /// <summary>Entero con separador de miles de la región (<see cref="LocNumber"/>): cantidades, «1.000».</summary>
        Number = 2,

        /// <summary>Entero sin separador de miles: números de nivel, intentos («Nivel 1000»).</summary>
        Digits = 3,

        /// <summary>Número con decimales fijos y separadores de la región («5,2» en es-CO, «5.2» en es-MX).</summary>
        Decimal = 4,
    }

    /// <summary>Un par con nombre de <see cref="LocArgs"/>: el marcador <c>{nombre}</c> de la plantilla y su valor.</summary>
    public readonly struct LocArg
    {
        /// <summary>Nombre del marcador, sin llaves (<c>n</c>, <c>level</c>, <c>price</c>).</summary>
        public readonly string Name;

        /// <summary>Tipo del valor.</summary>
        public readonly LocArgKind Kind;

        /// <summary>Valor de <see cref="LocArgKind.Number"/> y <see cref="LocArgKind.Digits"/>.</summary>
        public readonly long Integer;

        /// <summary>Valor de <see cref="LocArgKind.Decimal"/>.</summary>
        public readonly double Real;

        /// <summary>Decimales visibles de <see cref="LocArgKind.Decimal"/> (0-9).</summary>
        public readonly int Decimals;

        /// <summary>Valor de <see cref="LocArgKind.Text"/>.</summary>
        public readonly string? Text;

        internal LocArg(string name, LocArgKind kind, long integer, double real, int decimals, string? text)
        {
            Name = name;
            Kind = kind;
            Integer = integer;
            Real = real;
            Decimals = decimals;
            Text = text;
        }

        /// <summary>Si el valor es un número (vale para un plural).</summary>
        public bool IsNumeric => Kind == LocArgKind.Number || Kind == LocArgKind.Digits || Kind == LocArgKind.Decimal;

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (Kind)
            {
                case LocArgKind.Text: return Name + "=\"" + Text + "\"";
                case LocArgKind.Number:
                case LocArgKind.Digits: return Name + "=" + Integer.ToString(CultureInfo.InvariantCulture);
                case LocArgKind.Decimal: return Name + "=" + Real.ToString("F" + Decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                default: return "(vacío)";
            }
        }
    }

    /// <summary>
    /// Argumentos con nombre de una plantilla (contrato 32; 08 §4.7: marcadores con nombre, nunca posicionales): hasta
    /// <see cref="MaxCount"/> pares (texto, entero o decimal), en un struct y sin <c>params</c>, para no asignar memoria al
    /// construirlos. Se encadenan: <c>LocArgs.Of("n", 3).And("level", 12)</c>. El nombre <c>sep</c> está reservado para el
    /// marcador de corte (<see cref="LocFormat.SeparatorName"/>).
    /// </summary>
    public readonly struct LocArgs
    {
        /// <summary>Máximo de pares por plantilla (arquitectura de la tanda 4, §1).</summary>
        public const int MaxCount = 3;

        private readonly LocArg _a0;
        private readonly LocArg _a1;
        private readonly LocArg _a2;

        private LocArgs(in LocArg a0, in LocArg a1, in LocArg a2, int count)
        {
            _a0 = a0;
            _a1 = a1;
            _a2 = a2;
            Count = count;
        }

        /// <summary>Sin argumentos.</summary>
        public static LocArgs None => default;

        /// <summary>Número de pares.</summary>
        public int Count { get; }

        /// <summary>El par <paramref name="index"/> (0 ≤ índice &lt; <see cref="Count"/>).</summary>
        public LocArg this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return index == 0 ? _a0 : (index == 1 ? _a1 : _a2);
            }
        }

        /// <summary>Un entero con separador de miles (<see cref="LocArgKind.Number"/>).</summary>
        public static LocArgs Of(string name, long value) => default(LocArgs).And(name, value);

        /// <summary>Un texto que se inserta tal cual (<see cref="LocArgKind.Text"/>).</summary>
        public static LocArgs Of(string name, string value) => default(LocArgs).And(name, value);

        /// <summary>Un número con <paramref name="decimals"/> decimales (<see cref="LocArgKind.Decimal"/>).</summary>
        public static LocArgs Of(string name, double value, int decimals) => default(LocArgs).And(name, value, decimals);

        /// <summary>Un entero sin separador de miles (<see cref="LocArgKind.Digits"/>), como el número de nivel.</summary>
        public static LocArgs OfDigits(string name, long value) => default(LocArgs).AndDigits(name, value);

        /// <summary>Añade un entero con separador de miles.</summary>
        public LocArgs And(string name, long value)
        {
            return Add(new LocArg(CheckName(name), LocArgKind.Number, value, 0d, 0, null));
        }

        /// <summary>Añade un texto.</summary>
        public LocArgs And(string name, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return Add(new LocArg(CheckName(name), LocArgKind.Text, 0L, 0d, 0, value));
        }

        /// <summary>Añade un número con <paramref name="decimals"/> decimales (0-9).</summary>
        public LocArgs And(string name, double value, int decimals)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentException("El valor debe ser un número finito.", nameof(value));
            }

            if (decimals < 0 || decimals > LocNumber.MaxDecimals)
            {
                throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Entre 0 y 9 decimales.");
            }

            return Add(new LocArg(CheckName(name), LocArgKind.Decimal, 0L, value, decimals, null));
        }

        /// <summary>Añade un entero sin separador de miles.</summary>
        public LocArgs AndDigits(string name, long value)
        {
            return Add(new LocArg(CheckName(name), LocArgKind.Digits, value, 0d, 0, null));
        }

        /// <summary>Busca el par <paramref name="name"/> (comparación ordinal).</summary>
        public bool TryGet(string name, out LocArg arg)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return TryGet(name, 0, name.Length, out arg);
        }

        /// <summary>Busca el par cuyo nombre es <c>source[start..start+length)</c>, sin crear la subcadena.</summary>
        internal bool TryGet(string source, int start, int length, out LocArg arg)
        {
            if (Count > 0 && Matches(_a0.Name, source, start, length))
            {
                arg = _a0;
                return true;
            }

            if (Count > 1 && Matches(_a1.Name, source, start, length))
            {
                arg = _a1;
                return true;
            }

            if (Count > 2 && Matches(_a2.Name, source, start, length))
            {
                arg = _a2;
                return true;
            }

            arg = default;
            return false;
        }

        /// <summary>
        /// Si <paramref name="name"/> vale como nombre de marcador: <c>[a-z][a-z0-9_]*</c>. Así no hay marcadores posicionales
        /// (<c>{0}</c>) ni nombres que dependan de mayúsculas.
        /// </summary>
        public static bool IsValidName(string? name)
        {
            return name != null && IsValidName(name, 0, name.Length);
        }

        internal static bool IsValidName(string source, int start, int length)
        {
            if (length <= 0 || source[start] < 'a' || source[start] > 'z')
            {
                return false;
            }

            for (int i = start + 1; i < start + length; i++)
            {
                char c = source[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Lee la columna <c>args</c> [P] de una tabla (Q8-A de D-018: el texto lleva <c>{n}</c> y el valor va aquí): pares
        /// <c>nombre=valor</c> separados por <c>;</c>, con valores enteros (<c>5200</c>, con separador de miles al mostrarse) o
        /// decimales con punto (<c>3.25</c>: tantos decimales como cifras tras el punto). Vacío = sin argumentos.
        /// </summary>
        public static bool TryParse(string? text, out LocArgs args, out string? error)
        {
            args = default;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            string[] pairs = text!.Split(';');
            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i].Trim();
                int eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    error = "«" + pair + "» no es un par nombre=valor";
                    return false;
                }

                string name = pair.Substring(0, eq).Trim();
                string value = pair.Substring(eq + 1).Trim();
                if (!IsValidName(name) || name == LocFormat.SeparatorName)
                {
                    error = "«" + name + "» no vale como nombre de marcador";
                    return false;
                }

                if (args.Count >= MaxCount)
                {
                    error = "más de " + MaxCount.ToString(CultureInfo.InvariantCulture) + " argumentos";
                    return false;
                }

                if (args.TryGet(name, out _))
                {
                    error = "el argumento «" + name + "» está repetido";
                    return false;
                }

                if (!TryParseValue(value, out LocArg parsed, name))
                {
                    error = "el valor de «" + name + "» («" + value + "») no es un entero ni un decimal con punto";
                    return false;
                }

                args = args.Add(parsed);
            }

            return true;
        }

        private static bool TryParseValue(string value, out LocArg arg, string name)
        {
            arg = default;
            int dot = value.IndexOf('.');
            int start = value.Length > 0 && value[0] == '-' ? 1 : 0;
            if (dot < 0)
            {
                if (!AllDigits(value, start, value.Length - start)
                    || !long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long integer))
                {
                    return false;
                }

                arg = new LocArg(name, LocArgKind.Number, integer, 0d, 0, null);
                return true;
            }

            int decimals = value.Length - dot - 1;
            if (!AllDigits(value, start, dot - start) || !AllDigits(value, dot + 1, decimals) || decimals > LocNumber.MaxDecimals
                || !double.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double real))
            {
                return false;
            }

            arg = new LocArg(name, LocArgKind.Decimal, 0L, real, decimals, null);
            return true;
        }

        private static bool AllDigits(string s, int start, int length)
        {
            if (length <= 0)
            {
                return false;
            }

            for (int i = start; i < start + length; i++)
            {
                if (s[i] < '0' || s[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(string name, string source, int start, int length)
        {
            return name.Length == length && string.CompareOrdinal(name, 0, source, start, length) == 0;
        }

        private static string CheckName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!IsValidName(name))
            {
                throw new ArgumentException("Nombre de marcador no válido: «" + name + "» (se espera [a-z][a-z0-9_]*).", nameof(name));
            }

            if (name == LocFormat.SeparatorName)
            {
                throw new ArgumentException("«sep» es el marcador de corte: no lleva valor.", nameof(name));
            }

            return name;
        }

        private LocArgs Add(in LocArg arg)
        {
            if (TryGet(arg.Name, out _))
            {
                throw new ArgumentException("El argumento «" + arg.Name + "» ya está.", nameof(arg));
            }

            switch (Count)
            {
                case 0: return new LocArgs(arg, default, default, 1);
                case 1: return new LocArgs(_a0, arg, default, 2);
                case 2: return new LocArgs(_a0, _a1, arg, 3);
                default: throw new InvalidOperationException("Una plantilla lleva como mucho " + MaxCount.ToString(CultureInfo.InvariantCulture) + " argumentos.");
            }
        }
    }
}
