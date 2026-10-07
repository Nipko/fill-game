#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Levels.Json
{
    /// <summary>
    /// Lector JSON mínimo (RFC 8259) para los paquetes de niveles (docs/dev/prototipo-v0.md, contrato c): sin
    /// dependencias (Newtonsoft solo llega como dependencia transitiva y no se usa), sin reflexión e invariante a la
    /// cultura del dispositivo. Los números se guardan como texto y se convierten con InvariantCulture al pedirlos.
    /// </summary>
    public static class JsonParser
    {
        private const int MaxDepth = 64;

        /// <summary>Lee un documento JSON completo.</summary>
        /// <exception cref="FormatException">El texto no es JSON válido (el mensaje indica la posición).</exception>
        public static JsonValue Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var reader = new Reader(text);
            reader.SkipBom();
            reader.SkipWhitespace();
            JsonValue value = reader.ReadValue(0);
            reader.SkipWhitespace();
            if (!reader.AtEnd)
            {
                throw reader.Error("hay texto después del valor raíz");
            }

            return value;
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
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    {
                        _pos++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public FormatException Error(string message)
            {
                return new FormatException("JSON inválido en la posición " + _pos.ToString(CultureInfo.InvariantCulture) + ": " + message + ".");
            }

            public JsonValue ReadValue(int depth)
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
                        return JsonValue.FromString(ReadString());
                    case 't':
                        Expect("true");
                        return JsonValue.FromBool(true);
                    case 'f':
                        Expect("false");
                        return JsonValue.FromBool(false);
                    case 'n':
                        Expect("null");
                        return JsonValue.Null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                        {
                            return JsonValue.FromNumber(ReadNumber());
                        }

                        throw Error("carácter inesperado '" + c + "'");
                }
            }

            private void Expect(string literal)
            {
                if (string.CompareOrdinal(_text, _pos, literal, 0, literal.Length) != 0)
                {
                    throw Error("se esperaba '" + literal + "'");
                }

                _pos += literal.Length;
            }

            private JsonValue ReadObject(int depth)
            {
                _pos++; // {
                var members = new List<KeyValuePair<string, JsonValue>>();
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == '}')
                {
                    _pos++;
                    return JsonValue.FromObject(members);
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != '"')
                    {
                        throw Error("se esperaba el nombre de un miembro");
                    }

                    string key = ReadString();
                    for (int i = 0; i < members.Count; i++)
                    {
                        if (string.Equals(members[i].Key, key, StringComparison.Ordinal))
                        {
                            throw Error("clave duplicada '" + key + "'");
                        }
                    }

                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != ':')
                    {
                        throw Error("se esperaba ':'");
                    }

                    _pos++;
                    SkipWhitespace();
                    JsonValue value = ReadValue(depth + 1);
                    members.Add(new KeyValuePair<string, JsonValue>(key, value));
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("objeto sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == '}')
                    {
                        return JsonValue.FromObject(members);
                    }

                    if (c != ',')
                    {
                        throw Error("se esperaba ',' o '}'");
                    }
                }
            }

            private JsonValue ReadArray(int depth)
            {
                _pos++; // [
                var items = new List<JsonValue>();
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == ']')
                {
                    _pos++;
                    return JsonValue.FromArray(items);
                }

                while (true)
                {
                    SkipWhitespace();
                    items.Add(ReadValue(depth + 1));
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("lista sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == ']')
                    {
                        return JsonValue.FromArray(items);
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
                            if (_pos + 4 > _text.Length)
                            {
                                throw Error("escape \\u incompleto");
                            }

                            if (!int.TryParse(_text.Substring(_pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                            {
                                throw Error("escape \\u no hexadecimal");
                            }

                            sb.Append((char)code);
                            _pos += 4;
                            break;
                        default:
                            throw Error("escape desconocido '\\" + e + "'");
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

            private static bool IsDigit(char c)
            {
                return c >= '0' && c <= '9';
            }
        }
    }
}
