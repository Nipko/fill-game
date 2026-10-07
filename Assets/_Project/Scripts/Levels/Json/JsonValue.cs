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
    public sealed class JsonValue
    {
        private static readonly List<JsonValue> NoItems = new List<JsonValue>(0);
        private static readonly List<KeyValuePair<string, JsonValue>> NoMembers = new List<KeyValuePair<string, JsonValue>>(0);

        private readonly string? _text;
        private readonly bool _bool;
        private readonly List<JsonValue> _items;
        private readonly List<KeyValuePair<string, JsonValue>> _members;

        private JsonValue(JsonKind kind, string? text, bool boolean, List<JsonValue>? items, List<KeyValuePair<string, JsonValue>>? members)
        {
            Kind = kind;
            _text = text;
            _bool = boolean;
            _items = items ?? NoItems;
            _members = members ?? NoMembers;
        }

        /// <summary>Valor <c>null</c> compartido.</summary>
        public static readonly JsonValue Null = new JsonValue(JsonKind.Null, null, false, null, null);

        /// <summary>Tipo del valor.</summary>
        public JsonKind Kind { get; }

        /// <summary>Elementos de una lista (vacío si no es lista).</summary>
        public IReadOnlyList<JsonValue> Items => _items;

        /// <summary>Miembros de un objeto en el orden del documento (vacío si no es objeto).</summary>
        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members => _members;

        /// <summary>Es <c>null</c>.</summary>
        public bool IsNull => Kind == JsonKind.Null;

        internal static JsonValue FromBool(bool value)
        {
            return new JsonValue(JsonKind.Bool, null, value, null, null);
        }

        internal static JsonValue FromNumber(string text)
        {
            return new JsonValue(JsonKind.Number, text, false, null, null);
        }

        internal static JsonValue FromString(string text)
        {
            return new JsonValue(JsonKind.String, text, false, null, null);
        }

        internal static JsonValue FromArray(List<JsonValue> items)
        {
            return new JsonValue(JsonKind.Array, null, false, items, null);
        }

        internal static JsonValue FromObject(List<KeyValuePair<string, JsonValue>> members)
        {
            return new JsonValue(JsonKind.Object, null, false, null, members);
        }

        /// <summary>Miembro <paramref name="key"/> de un objeto, o null si no existe o el valor no es un objeto.</summary>
        public JsonValue? Get(string key)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                {
                    return _members[i].Value;
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
            if (Kind != JsonKind.String || _text == null)
            {
                throw new FormatException("Se esperaba una cadena y hay " + Kind + ".");
            }

            return _text;
        }

        /// <summary>Número entero (InvariantCulture; sin parte decimal ni exponente).</summary>
        /// <exception cref="FormatException">No es un número entero de 32 bits.</exception>
        public int AsInt()
        {
            if (Kind != JsonKind.Number || _text == null)
            {
                throw new FormatException("Se esperaba un número entero y hay " + Kind + ".");
            }

            if (!int.TryParse(_text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                throw new FormatException("El número '" + _text + "' no es un entero de 32 bits.");
            }

            return value;
        }

        /// <summary>Número en coma flotante (InvariantCulture).</summary>
        /// <exception cref="FormatException">No es un número.</exception>
        public double AsDouble()
        {
            if (Kind != JsonKind.Number || _text == null)
            {
                throw new FormatException("Se esperaba un número y hay " + Kind + ".");
            }

            return double.Parse(_text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
