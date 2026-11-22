#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Levels.Json
{
    /// <summary>Tipo de un valor JSON.</summary>
    public enum JsonKind
    {
        /// <summary><c>null</c>.</summary>
        Null = 0,

        /// <summary><c>true</c> o <c>false</c>.</summary>
        Bool = 1,

        /// <summary>Número (se guarda el texto original y se convierte con InvariantCulture al pedirlo).</summary>
        Number = 2,

        /// <summary>Cadena.</summary>
        String = 3,

        /// <summary>Lista.</summary>
        Array = 4,

        /// <summary>Objeto (conserva el orden de las claves).</summary>
        Object = 5,
    }

    /// <summary>
    /// Valor de un documento JSON leído por <see cref="JsonParser"/>. Inmutable tras la lectura. Los objetos
    /// conservan el orden de sus claves (búsqueda lineal: los objetos de un paquete de niveles son pequeños).
    /// </summary>
    /// <remarks>
    /// Memoria (2026-11-17, <c>docs/dev/memoria.md</c> §11): un solo campo para el texto, los elementos o los miembros, guardados
    /// en arrays del tamaño justo; los booleanos y los enteros pequeños sin signo (0-<see cref="SharedIntMax"/>, en su forma
    /// canónica) son valores compartidos. Como el valor es inmutable, compartirlo no cambia nada para quien lo lee.
    /// </remarks>
    public sealed class JsonValue
    {
        /// <summary>Mayor entero que se comparte (<see cref="SharedInt"/>).</summary>
        internal const int SharedIntMax = 1023;

        private static readonly JsonValue[] NoItems = new JsonValue[0];
        private static readonly KeyValuePair<string, JsonValue>[] NoMembers = new KeyValuePair<string, JsonValue>[0];
        private static readonly JsonValue TrueValue = new JsonValue(JsonKind.Bool, true, null);
        private static readonly JsonValue FalseValue = new JsonValue(JsonKind.Bool, false, null);
        private static readonly JsonValue?[] SharedInts = new JsonValue?[SharedIntMax + 1];

        private readonly bool _bool;

        // El texto (número o cadena), los elementos (JsonValue[]) o los miembros (KeyValuePair<string, JsonValue>[]), según Kind.
        private readonly object? _data;

        private JsonValue(JsonKind kind, bool boolean, object? data)
        {
            Kind = kind;
            _bool = boolean;
            _data = data;
        }

        /// <summary>Valor <c>null</c> compartido.</summary>
        public static readonly JsonValue Null = new JsonValue(JsonKind.Null, false, null);

        /// <summary>Tipo del valor.</summary>
        public JsonKind Kind { get; }

        /// <summary>Elementos de una lista (vacío si no es lista).</summary>
        public IReadOnlyList<JsonValue> Items => Kind == JsonKind.Array ? (JsonValue[])_data! : NoItems;

        /// <summary>Miembros de un objeto en el orden del documento (vacío si no es objeto).</summary>
        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members => Kind == JsonKind.Object ? (KeyValuePair<string, JsonValue>[])_data! : NoMembers;

        /// <summary>Es <c>null</c>.</summary>
        public bool IsNull => Kind == JsonKind.Null;

        // Texto de un número o una cadena (null en los demás).
        private string? Text => Kind == JsonKind.Number || Kind == JsonKind.String ? (string?)_data : null;

        internal static JsonValue FromBool(bool value)
        {
            return value ? TrueValue : FalseValue;
        }

        internal static JsonValue FromNumber(string text)
        {
            return new JsonValue(JsonKind.Number, false, text);
        }

        /// <summary>
        /// El entero <paramref name="value"/> (0-<see cref="SharedIntMax"/>) como número compartido, con su texto canónico
        /// (sin signo, sin ceros delante, sin decimales ni exponente: el mismo que tendría en el documento).
        /// </summary>
        internal static JsonValue SharedInt(int value)
        {
            JsonValue? shared = SharedInts[value];
            if (shared == null)
            {
                shared = new JsonValue(JsonKind.Number, false, value.ToString(CultureInfo.InvariantCulture));
                SharedInts[value] = shared;
            }

            return shared;
        }

        internal static JsonValue FromString(string text)
        {
            return new JsonValue(JsonKind.String, false, text);
        }

        internal static JsonValue FromArray(JsonValue[] items)
        {
            return new JsonValue(JsonKind.Array, false, items.Length == 0 ? NoItems : items);
        }

        internal static JsonValue FromObject(KeyValuePair<string, JsonValue>[] members)
        {
            return new JsonValue(JsonKind.Object, false, members.Length == 0 ? NoMembers : members);
        }

        /// <summary>Miembro <paramref name="key"/> de un objeto, o null si no existe o el valor no es un objeto.</summary>
        public JsonValue? Get(string key)
        {
            if (Kind != JsonKind.Object)
            {
                return null;
            }

            var members = (KeyValuePair<string, JsonValue>[])_data!;
            for (int i = 0; i < members.Length; i++)
            {
                if (string.Equals(members[i].Key, key, StringComparison.Ordinal))
                {
                    return members[i].Value;
                }
            }

            return null;
        }

        /// <summary>El objeto tiene el miembro <paramref name="key"/>.</summary>
        public bool Has(string key)
        {
            return Get(key) != null;
        }

        /// <summary>Valor booleano.</summary>
        /// <exception cref="FormatException">No es un booleano.</exception>
        public bool AsBool()
        {
            if (Kind != JsonKind.Bool)
            {
                throw new FormatException("Se esperaba un booleano y hay " + Kind + ".");
            }

            return _bool;
        }

        /// <summary>Texto de una cadena.</summary>
        /// <exception cref="FormatException">No es una cadena.</exception>
        public string AsString()
        {
            string? text = Text;
            if (Kind != JsonKind.String || text == null)
            {
                throw new FormatException("Se esperaba una cadena y hay " + Kind + ".");
            }

            return text;
        }

        /// <summary>Número entero (InvariantCulture; sin parte decimal ni exponente).</summary>
        /// <exception cref="FormatException">No es un número entero de 32 bits.</exception>
        public int AsInt()
        {
            string? text = Text;
            if (Kind != JsonKind.Number || text == null)
            {
                throw new FormatException("Se esperaba un número entero y hay " + Kind + ".");
            }

            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                throw new FormatException("El número '" + text + "' no es un entero de 32 bits.");
            }

            return value;
        }

        /// <summary>Número en coma flotante (InvariantCulture).</summary>
        /// <exception cref="FormatException">No es un número.</exception>
        public double AsDouble()
        {
            string? text = Text;
            if (Kind != JsonKind.Number || text == null)
            {
                throw new FormatException("Se esperaba un número y hay " + Kind + ".");
            }

            return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
