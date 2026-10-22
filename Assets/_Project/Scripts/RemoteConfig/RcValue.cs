#nullable enable
using System;
using System.Globalization;

namespace AF.RemoteConfig
{
    /// <summary>Origen de un valor de Remote Config (08 §2.2: «origen Default/Cached/Remote»).</summary>
    public enum RcSource
    {
        /// <summary>El default compilado de <c>RemoteConfigDefaults.json</c>.</summary>
        Default = 0,

        /// <summary>El último valor descargado, guardado en el dispositivo (sin red, 04 §10.2).</summary>
        Cached = 1,

        /// <summary>Un valor descargado y activado en esta sesión.</summary>
        Remote = 2,
    }

    /// <summary>Un valor tipado de Remote Config. Inmutable.</summary>
    public readonly struct RcValue : IEquatable<RcValue>
    {
        private RcValue(RcType type, long integer, double number, bool boolean, string? text)
        {
            Type = type;
            Integer = integer;
            Number = number;
            Boolean = boolean;
            Text = text;
        }

        /// <summary>Tipo del valor.</summary>
        public RcType Type { get; }

        /// <summary>Valor de un <see cref="RcType.Int"/>.</summary>
        public long Integer { get; }

        /// <summary>Valor de un <see cref="RcType.Float"/> (o de un <see cref="RcType.Int"/>, convertido).</summary>
        public double Number { get; }

        /// <summary>Valor de un <see cref="RcType.Bool"/>.</summary>
        public bool Boolean { get; }

        /// <summary>Cadena de un <see cref="RcType.String"/>, o el JSON compacto de un <see cref="RcType.Json"/>.</summary>
        public string? Text { get; }

        /// <summary>Un entero.</summary>
        public static RcValue OfInt(long value) => new RcValue(RcType.Int, value, value, false, null);

        /// <summary>Un decimal.</summary>
        public static RcValue OfFloat(double value) => new RcValue(RcType.Float, 0, value, false, null);

        /// <summary>Un booleano.</summary>
        public static RcValue OfBool(bool value) => new RcValue(RcType.Bool, 0, 0, value, null);

        /// <summary>Una cadena.</summary>
        public static RcValue OfString(string value) => new RcValue(RcType.String, 0, 0, false, value ?? throw new ArgumentNullException(nameof(value)));

        /// <summary>Un JSON (se guarda compacto).</summary>
        public static RcValue OfJson(RcJson node) => new RcValue(RcType.Json, 0, 0, false, (node ?? throw new ArgumentNullException(nameof(node))).ToJson());

        /// <summary>Lee un valor de tipo <paramref name="type"/> de un nodo JSON (el default del archivo).</summary>
        public static bool TryFromJson(RcType type, RcJson node, out RcValue value)
        {
            value = default;
            switch (type)
            {
                case RcType.Int:
                    if (!node.IsInteger)
                    {
                        return false;
                    }

                    value = OfInt(long.Parse(node.Text!, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
                    return true;
                case RcType.Float:
                    if (node.Kind != RcJsonKind.Number)
                    {
                        return false;
                    }

                    value = OfFloat(node.Number);
                    return true;
                case RcType.Bool:
                    if (node.Kind != RcJsonKind.Bool)
                    {
                        return false;
                    }

                    value = OfBool(node.Bool);
                    return true;
                case RcType.String:
                    if (node.Kind != RcJsonKind.String)
                    {
                        return false;
                    }

                    value = OfString(node.Text!);
                    return true;
                case RcType.Json:
                    value = OfJson(node);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Lee un valor remoto (Firebase Remote Config los da como cadena): un int en decimal, un float con punto, un bool
        /// <c>true</c>/<c>false</c>, una cadena tal cual y un JSON válido. Si no se entiende, devuelve false.
        /// </summary>
        public static bool TryFromRemote(RcType type, string? raw, out RcValue value)
        {
            value = default;
            if (raw == null)
            {
                return false;
            }

            switch (type)
            {
                case RcType.Int:
                    if (!long.TryParse(raw.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long i))
                    {
                        return false;
                    }

                    value = OfInt(i);
                    return true;
                case RcType.Float:
                    if (!double.TryParse(raw.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                            CultureInfo.InvariantCulture, out double d) || double.IsNaN(d) || double.IsInfinity(d))
                    {
                        return false;
                    }

                    value = OfFloat(d);
                    return true;
                case RcType.Bool:
                    string b = raw.Trim();
                    if (b == "true")
                    {
                        value = OfBool(true);
                        return true;
                    }

                    if (b == "false")
                    {
                        value = OfBool(false);
                        return true;
                    }

                    return false;
                case RcType.String:
                    value = OfString(raw);
                    return true;
                case RcType.Json:
                    if (!RcJson.TryParse(raw, out RcJson? node, out _))
                    {
                        return false;
                    }

                    value = OfJson(node!);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>El valor como lo escribiría Firebase (cadena): el inverso de <see cref="TryFromRemote"/>.</summary>
        public string ToRemoteString()
        {
            switch (Type)
            {
                case RcType.Int: return Integer.ToString(CultureInfo.InvariantCulture);
                case RcType.Float: return Number.ToString("R", CultureInfo.InvariantCulture);
                case RcType.Bool: return Boolean ? "true" : "false";
                default: return Text!;
            }
        }

        /// <inheritdoc />
        public bool Equals(RcValue other)
        {
            return Type == other.Type && Integer == other.Integer && Number.Equals(other.Number) && Boolean == other.Boolean
                && string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RcValue other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Type * 397) ^ Integer.GetHashCode() ^ Number.GetHashCode() ^ Boolean.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Type + ":" + ToRemoteString();
    }
}
