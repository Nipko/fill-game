#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Backend.Json
{
    /// <summary>Tipo de un nodo JSON del contrato.</summary>
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
    /// Nodo JSON de los cuerpos de la API v1 (<c>infra/contracts/v1</c>). <c>AF.Backend</c> no puede usar los lectores de
    /// <c>AF.Save</c> ni de <c>AF.Levels</c> (08 §2.2: Save depende de Backend) ni Newtonsoft (sin paquetes nuevos), así que
    /// tiene el suyo, como <c>AF.Economy</c> y <c>AF.Save</c>. Los objetos conservan el orden de sus claves y los números se
    /// guardan como texto (InvariantCulture).
    /// </summary>
    internal sealed class JsonNode
    {
        private static readonly List<JsonNode> NoItems = new List<JsonNode>(0);
        private static readonly List<KeyValuePair<string, JsonNode>> NoMembers = new List<KeyValuePair<string, JsonNode>>(0);

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

        public static readonly JsonNode Null = new JsonNode(JsonKind.Null, null, false, null, null);

        public JsonKind Kind { get; }

        public IReadOnlyList<JsonNode> Items => _items ?? NoItems;

        public IReadOnlyList<KeyValuePair<string, JsonNode>> Members => _members ?? NoMembers;

        public bool BoolValue => Kind == JsonKind.Bool && _bool;

        /// <summary>Texto de una cadena o de un número; null en los demás tipos.</summary>
        public string? Text => _text;

        public static JsonNode FromBool(bool value)
        {
            return new JsonNode(JsonKind.Bool, null, value, null, null);
        }

        public static JsonNode FromString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new JsonNode(JsonKind.String, value, false, null, null);
        }

        public static JsonNode FromLong(long value)
        {
            return new JsonNode(JsonKind.Number, value.ToString(CultureInfo.InvariantCulture), false, null, null);
        }

        public static JsonNode FromNumberText(string text)
        {
            return new JsonNode(JsonKind.Number, text, false, null, null);
        }

        public static JsonNode NewArray()
        {
            return new JsonNode(JsonKind.Array, null, false, new List<JsonNode>(), null);
        }

        public static JsonNode NewObject()
        {
            return new JsonNode(JsonKind.Object, null, false, null, new List<KeyValuePair<string, JsonNode>>());
        }

        /// <summary>Añade un elemento a una lista.</summary>
        public JsonNode Add(JsonNode item)
        {
            if (_items == null)
            {
                throw new InvalidOperationException("El nodo no es una lista.");
            }

            _items.Add(item ?? throw new ArgumentNullException(nameof(item)));
            return this;
        }

        /// <summary>Añade un miembro a un objeto (la clave no puede estar ya).</summary>
        public JsonNode Set(string key, JsonNode value)
        {
            if (_members == null)
            {
                throw new InvalidOperationException("El nodo no es un objeto.");
            }

            if (Get(key) != null)
            {
                throw new InvalidOperationException("Clave repetida: " + key + ".");
            }

            _members.Add(new KeyValuePair<string, JsonNode>(key, value ?? throw new ArgumentNullException(nameof(value))));
            return this;
        }

        /// <summary>Miembro <paramref name="key"/> de un objeto, o null si no está (o el nodo no es un objeto).</summary>
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

        /// <summary>
        /// Igualdad estructural: mismo tipo; objetos con las mismas claves (en cualquier orden) y valores iguales; listas en el
        /// mismo orden; números iguales como decimales (<c>1</c> y <c>1.0</c> son iguales).
        /// </summary>
        public static bool StructurallyEqual(JsonNode a, JsonNode b)
        {
            if (a.Kind != b.Kind)
            {
                return false;
            }

            switch (a.Kind)
            {
                case JsonKind.Null:
                    return true;
                case JsonKind.Bool:
                    return a._bool == b._bool;
                case JsonKind.String:
                    return string.Equals(a._text, b._text, StringComparison.Ordinal);
                case JsonKind.Number:
                    if (decimal.TryParse(a._text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal da)
                        && decimal.TryParse(b._text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal db))
                    {
                        return da == db;
                    }

                    return string.Equals(a._text, b._text, StringComparison.Ordinal);
                case JsonKind.Array:
                    if (a.Items.Count != b.Items.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < a.Items.Count; i++)
                    {
                        if (!StructurallyEqual(a.Items[i], b.Items[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                case JsonKind.Object:
                    if (a.Members.Count != b.Members.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < a.Members.Count; i++)
                    {
                        JsonNode? other = b.Get(a.Members[i].Key);
                        if (other == null || !StructurallyEqual(a.Members[i].Value, other))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Lectura (RFC 8259) y escritura compacta de JSON para la API v1, sin reflexión e invariante a la cultura. La escritura es
    /// determinista (orden de inserción, sin espacios, solo ASCII: lo demás va como <c>\uXXXX</c>).
    /// </summary>
    internal static class BackendJson
    {
        private const int MaxDepth = 32;

        /// <summary>Lee un documento completo.</summary>
        /// <exception cref="FormatException">El texto no es JSON válido.</exception>
        public static JsonNode Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var reader = new Reader(text);
            reader.SkipBom();
            reader.SkipWhitespace();
            JsonNode value = reader.ReadValue(0);
            reader.SkipWhitespace();
            if (!reader.AtEnd)
            {
                throw reader.Error("hay texto después del valor raíz");
            }

            return value;
        }

        /// <summary>Escribe <paramref name="node"/> sin espacios.</summary>
        public static string Write(JsonNode node)
        {
            var sb = new StringBuilder(256);
            WriteValue(sb, node);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, JsonNode node)
        {
            switch (node.Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                    sb.Append(node.BoolValue ? "true" : "false");
                    break;
                case JsonKind.Number:
                    sb.Append(node.Text);
                    break;
                case JsonKind.String:
                    WriteString(sb, node.Text ?? string.Empty);
                    break;
                case JsonKind.Array:
                    sb.Append('[');
                    for (int i = 0; i < node.Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        WriteValue(sb, node.Items[i]);
                    }

                    sb.Append(']');
                    break;
                case JsonKind.Object:
                    sb.Append('{');
                    for (int i = 0; i < node.Members.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        WriteString(sb, node.Members[i].Key);
                        sb.Append(':');
                        WriteValue(sb, node.Members[i].Value);
                    }

                    sb.Append('}');
                    break;
                default:
                    throw new InvalidOperationException("Tipo JSON desconocido: " + node.Kind + ".");
            }
        }

        private static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < ' ' || c > '~')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
        }

        private sealed class Reader
        {
            private readonly string _text;
            private int _pos;

            public Reader(string text)
            {
                _text = text;
            }

            public bool AtEnd => _pos >= _text.Length;

            public void SkipBom()
            {
                if (_pos < _text.Length && _text[_pos] == '﻿')
                {
                    _pos++;
                }
            }

            public void SkipWhitespace()
            {
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
                    {
                        break;
                    }

                    _pos++;
                }
            }

            public FormatException Error(string message)
            {
                return new FormatException("JSON inválido en la posición " + _pos.ToString(CultureInfo.InvariantCulture) + ": " + message + ".");
            }

            public JsonNode ReadValue(int depth)
            {
                if (depth > MaxDepth)
                {
                    throw Error("anidamiento demasiado profundo");
                }

                if (AtEnd)
                {
                    throw Error("fin inesperado");
                }

                char c = _text[_pos];
                switch (c)
                {
                    case '{':
                        return ReadObject(depth);
                    case '[':
                        return ReadArray(depth);
                    case '"':
                        return JsonNode.FromString(ReadString());
                    case 't':
                        Expect("true");
                        return JsonNode.FromBool(true);
                    case 'f':
                        Expect("false");
                        return JsonNode.FromBool(false);
                    case 'n':
                        Expect("null");
                        return JsonNode.Null;
                    default:
                        if (c == '-' || IsDigit(c))
                        {
                            return JsonNode.FromNumberText(ReadNumber());
                        }

                        throw Error("carácter inesperado");
                }
            }

            private void Expect(string literal)
            {
                if (_pos + literal.Length > _text.Length || string.CompareOrdinal(_text, _pos, literal, 0, literal.Length) != 0)
                {
                    throw Error("se esperaba '" + literal + "'");
                }

                _pos += literal.Length;
            }

            private JsonNode ReadObject(int depth)
            {
                _pos++;
                JsonNode obj = JsonNode.NewObject();
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == '}')
                {
                    _pos++;
                    return obj;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != '"')
                    {
                        throw Error("se esperaba el nombre de un miembro");
                    }

                    string key = ReadString();
                    if (obj.Get(key) != null)
                    {
                        throw Error("clave repetida");
                    }

                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != ':')
                    {
                        throw Error("se esperaba ':'");
                    }

                    _pos++;
                    SkipWhitespace();
                    obj.Set(key, ReadValue(depth + 1));
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("objeto sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == '}')
                    {
                        return obj;
                    }

                    if (c != ',')
                    {
                        throw Error("se esperaba ',' o '}'");
                    }
                }
            }

            private JsonNode ReadArray(int depth)
            {
                _pos++;
                JsonNode array = JsonNode.NewArray();
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == ']')
                {
                    _pos++;
                    return array;
                }

                while (true)
                {
                    SkipWhitespace();
                    array.Add(ReadValue(depth + 1));
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("lista sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == ']')
                    {
                        return array;
                    }

                    if (c != ',')
                    {
                        throw Error("se esperaba ',' o ']'");
                    }
                }
            }

            private string ReadString()
            {
                _pos++;
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                    {
                        throw Error("cadena sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }

                    if (c < ' ')
                    {
                        throw Error("carácter de control dentro de una cadena");
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (AtEnd)
                    {
                        throw Error("escape incompleto");
                    }

                    char e = _text[_pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _text.Length
                                || !int.TryParse(_text.Substring(_pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                            {
                                throw Error("escape \\u incompleto o no hexadecimal");
                            }

                            sb.Append((char)code);
                            _pos += 4;
                            break;
                        default:
                            throw Error("escape desconocido");
                    }
                }
            }

            private string ReadNumber()
            {
                int start = _pos;
                if (_text[_pos] == '-')
                {
                    _pos++;
                }

                if (AtEnd)
                {
                    throw Error("número incompleto");
                }

                if (_text[_pos] == '0')
                {
                    _pos++;
                }
                else if (_text[_pos] >= '1' && _text[_pos] <= '9')
                {
                    SkipDigits();
                }
                else
                {
                    throw Error("número mal formado");
                }

                if (!AtEnd && _text[_pos] == '.')
                {
                    _pos++;
                    if (AtEnd || !IsDigit(_text[_pos]))
                    {
                        throw Error("faltan decimales");
                    }

                    SkipDigits();
                }

                if (!AtEnd && (_text[_pos] == 'e' || _text[_pos] == 'E'))
                {
                    _pos++;
                    if (!AtEnd && (_text[_pos] == '+' || _text[_pos] == '-'))
                    {
                        _pos++;
                    }

                    if (AtEnd || !IsDigit(_text[_pos]))
                    {
                        throw Error("exponente incompleto");
                    }

                    SkipDigits();
                }

                return _text.Substring(start, _pos - start);
            }

            private void SkipDigits()
            {
                while (!AtEnd && IsDigit(_text[_pos]))
                {
                    _pos++;
                }
            }
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
