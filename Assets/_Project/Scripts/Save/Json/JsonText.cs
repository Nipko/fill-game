#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Save.Json
{
    /// <summary>
    /// Lectura y escritura del JSON del perfil (RFC 8259), sin reflexión e invariante a la cultura del dispositivo.
    /// La escritura es determinista: orden de claves estable, 2 espacios, LF y salida solo ASCII (todo carácter fuera de
    /// 0x20-0x7E va como <c>\uXXXX</c>), de modo que el texto es idéntico en cualquier cultura y la ida y vuelta es exacta.
    /// </summary>
    internal static class JsonText
    {
        private const int MaxDepth = 64;

        /// <summary>Lee un documento completo.</summary>
        /// <exception cref="FormatException">El texto no es JSON válido (el mensaje indica la posición).</exception>
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

        /// <summary>Escribe <paramref name="node"/> con sangría de 2 espacios, LF y salto final.</summary>
        public static string Write(JsonNode node)
        {
            var sb = new StringBuilder(1024);
            WriteValue(sb, node, 0);
            sb.Append('\n');
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, JsonNode node, int depth)
        {
            switch (node.Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                    sb.Append(node.AsBool() ? "true" : "false");
                    break;
                case JsonKind.Number:
                    sb.Append(node.NumberText);
                    break;
                case JsonKind.String:
                    WriteString(sb, node.AsString());
                    break;
                case JsonKind.Array:
                    WriteArray(sb, node, depth);
                    break;
                case JsonKind.Object:
                    WriteObject(sb, node, depth);
                    break;
                default:
                    throw new InvalidOperationException("Tipo JSON desconocido: " + node.Kind + ".");
            }
        }

        private static void WriteObject(StringBuilder sb, JsonNode node, int depth)
        {
            IReadOnlyList<KeyValuePair<string, JsonNode>> members = node.Members;
            if (members.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            sb.Append("{\n");
            for (int i = 0; i < members.Count; i++)
            {
                Indent(sb, depth + 1);
                WriteString(sb, members[i].Key);
                sb.Append(": ");
                WriteValue(sb, members[i].Value, depth + 1);
                sb.Append(i + 1 < members.Count ? ",\n" : "\n");
            }

            Indent(sb, depth);
            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, JsonNode node, int depth)
        {
            IReadOnlyList<JsonNode> items = node.Items;
            if (items.Count == 0)
            {
                sb.Append("[]");
                return;
            }

            bool scalars = true;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == JsonKind.Array || items[i].Kind == JsonKind.Object)
                {
                    scalars = false;
                    break;
                }
            }

            if (scalars)
            {
                // Listas cortas de números (niveles saltados, cuias reveladas) en una línea.
                sb.Append('[');
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    WriteValue(sb, items[i], depth + 1);
                }

                sb.Append(']');
                return;
            }

            sb.Append("[\n");
            for (int i = 0; i < items.Count; i++)
            {
                Indent(sb, depth + 1);
                WriteValue(sb, items[i], depth + 1);
                sb.Append(i + 1 < items.Count ? ",\n" : "\n");
            }

            Indent(sb, depth);
            sb.Append(']');
        }

        private static void Indent(StringBuilder sb, int depth)
        {
            sb.Append(' ', depth * 2);
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

                        throw Error("carácter inesperado (código " + ((int)c).ToString(CultureInfo.InvariantCulture) + ")");
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
                _pos++; // {
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
                        throw Error("clave duplicada '" + key + "'");
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
                _pos++; // [
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
                _pos++; // "
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
