#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Save.Json
{
    /// <summary>Tipo de un nodo JSON del perfil.</summary>
    internal enum JsonKind
    {
        Null = 0,
        Bool = 1,
        Number = 2,
        String = 3,
        Array = 4,
        Object = 5,
    }

    /// <summary>
    /// Documento JSON mutable del perfil. Es el formato sobre el que trabajan las migraciones
    /// (<see cref="ProfileMigrations"/>): una migración <c>n → n+1</c> renombra o mueve claves sin conocer la clase
    /// <see cref="Profile"/>, que solo describe el esquema actual. Los objetos conservan el orden de sus claves (búsqueda
    /// lineal: el perfil tiene pocas). Los números se guardan como texto y se leen con InvariantCulture.
    /// AF.Save no puede usar el lector de <c>AF.Levels</c> (08 §2.2: Levels depende de Save) ni Newtonsoft (sin paquetes
    /// nuevos), por eso tiene el suyo.
    /// </summary>
    internal sealed class JsonNode
    {
        private static readonly JsonNode[] NoItems = new JsonNode[0];
        private static readonly KeyValuePair<string, JsonNode>[] NoMembers = new KeyValuePair<string, JsonNode>[0];

        private readonly string? _text;
        private readonly bool _bool;
        private readonly List<JsonNode>? _items;
        private readonly List<KeyValuePair<string, JsonNode>>? _members;

        private JsonNode(JsonKind kind, string? text, bool boolean, List<JsonNode>? items, List<KeyValuePair<string, JsonNode>>? members)
        {
            Kind = kind;
            _text = text;
            _bool = boolean;
            _items = items;
            _members = members;
        }

        /// <summary><c>null</c> (compartido: no es mutable).</summary>
        public static readonly JsonNode Null = new JsonNode(JsonKind.Null, null, false, null, null);

        public JsonKind Kind { get; }

        public bool IsNull => Kind == JsonKind.Null;

        /// <summary>Elementos de una lista (vacío si no es lista).</summary>
        public IReadOnlyList<JsonNode> Items => (IReadOnlyList<JsonNode>?)_items ?? NoItems;

        /// <summary>Miembros de un objeto en orden (vacío si no es objeto).</summary>
        public IReadOnlyList<KeyValuePair<string, JsonNode>> Members => (IReadOnlyList<KeyValuePair<string, JsonNode>>?)_members ?? NoMembers;

        /// <summary>Texto de un número tal como está en el documento (InvariantCulture).</summary>
        public string NumberText => Kind == JsonKind.Number && _text != null ? _text : throw Mismatch("un número");

        public static JsonNode NewObject()
        {
            return new JsonNode(JsonKind.Object, null, false, null, new List<KeyValuePair<string, JsonNode>>());
        }

        public static JsonNode NewArray()
        {
            return new JsonNode(JsonKind.Array, null, false, new List<JsonNode>(), null);
        }

        public static JsonNode FromBool(bool value)
        {
            return new JsonNode(JsonKind.Bool, null, value, null, null);
        }

        public static JsonNode FromInt(long value)
        {
            return new JsonNode(JsonKind.Number, value.ToString(CultureInfo.InvariantCulture), false, null, null);
        }

        /// <summary>Cadena, o <see cref="Null"/> si <paramref name="value"/> es null.</summary>
        public static JsonNode FromString(string? value)
        {
            return value == null ? Null : new JsonNode(JsonKind.String, value, false, null, null);
        }

        internal static JsonNode FromNumberText(string text)
        {
            return new JsonNode(JsonKind.Number, text, false, null, null);
        }

        /// <summary>Miembro <paramref name="key"/> de un objeto, o null si no existe (o el nodo no es un objeto).</summary>
        public JsonNode? Get(string key)
        {
            if (_members == null)
            {
                return null;
            }

            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                {
                    return _members[i].Value;
                }
            }

            return null;
        }

        /// <summary>Asigna el miembro <paramref name="key"/>: lo sustituye en su sitio o lo añade al final.</summary>
        /// <exception cref="InvalidOperationException">El nodo no es un objeto.</exception>
        public void Set(string key, JsonNode value)
        {
            if (_members == null)
            {
                throw new InvalidOperationException("Set solo vale para objetos JSON.");
            }

            if (key == null || value == null)
            {
                throw new ArgumentNullException(key == null ? nameof(key) : nameof(value));
            }

            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                {
                    _members[i] = new KeyValuePair<string, JsonNode>(key, value);
                    return;
                }
            }

            _members.Add(new KeyValuePair<string, JsonNode>(key, value));
        }

        /// <summary>Quita el miembro <paramref name="key"/>; devuelve si existía.</summary>
        public bool Remove(string key)
        {
            if (_members == null)
            {
                return false;
            }

            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                {
                    _members.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Añade un elemento al final de una lista.</summary>
        /// <exception cref="InvalidOperationException">El nodo no es una lista.</exception>
        public void Add(JsonNode item)
        {
            if (_items == null)
            {
                throw new InvalidOperationException("Add solo vale para listas JSON.");
            }

            _items.Add(item ?? throw new ArgumentNullException(nameof(item)));
        }

        /// <exception cref="FormatException">No es un booleano.</exception>
        public bool AsBool()
        {
            return Kind == JsonKind.Bool ? _bool : throw Mismatch("un booleano");
        }

        /// <exception cref="FormatException">No es una cadena.</exception>
        public string AsString()
        {
            return Kind == JsonKind.String && _text != null ? _text : throw Mismatch("una cadena");
        }

        /// <summary>Entero de 32 bits sin parte decimal ni exponente (InvariantCulture).</summary>
        /// <exception cref="FormatException">No es un número entero de 32 bits.</exception>
        public int AsInt()
        {
            string text = NumberText;
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                throw new FormatException("El número '" + text + "' no es un entero de 32 bits.");
            }

            return value;
        }

        private FormatException Mismatch(string expected)
        {
            return new FormatException("Se esperaba " + expected + " y hay " + Kind + ".");
        }
    }
}
