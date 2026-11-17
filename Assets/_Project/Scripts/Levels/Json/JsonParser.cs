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
    /// <remarks>
    /// Memoria (2026-11-17, <c>docs/dev/memoria.md</c> §11): el arranque lee unos 1,8 MB de paquetes de niveles y el Reto otros
    /// 1,4 MB, y con IL2CPP la basura de esa lectura hace crecer el heap gestionado, que no se devuelve. Por eso el lector asigna lo
    /// justo: las cadenas sin escapes salen de un <c>Substring</c> (sin <c>StringBuilder</c>), las cortas que se repiten (las claves,
    /// los nombres de banda o de mecánica) se comparten dentro del documento, los enteros pequeños y los booleanos son valores
    /// compartidos (<see cref="JsonValue"/>), y las listas y los objetos se acumulan en dos pilas del lector y se guardan en arrays
    /// del tamaño justo. El documento que sale es el mismo.
    /// </remarks>
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

        // Cadenas más largas que esto no se comparten (las claves y los nombres cortos sí).
        private const int MaxSharedStringLength = 32;

        // Huecos de la tabla de cadenas compartidas de un documento (potencia de 2).
        private const int SharedStringSlots = 1024;

        private sealed class Reader
        {
            private readonly string _text;
            private int _pos;

            // Pilas de los elementos y de los miembros que se están leyendo (cada lista u objeto usa su tramo del final).
            private readonly List<JsonValue> _itemStack = new List<JsonValue>(64);
            private readonly List<KeyValuePair<string, JsonValue>> _memberStack = new List<KeyValuePair<string, JsonValue>>(64);

            // Cadenas cortas ya leídas en este documento: la misma clave o el mismo nombre, una sola cadena.
            private readonly string?[] _shared = new string?[SharedStringSlots];

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
                            return ReadNumber();
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
                int start = _memberStack.Count;
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == '}')
                {
                    _pos++;
                    return JsonValue.FromObject(PopMembers(start));
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != '"')
                    {
                        throw Error("se esperaba el nombre de un miembro");
                    }

                    string key = ReadString();
                    for (int i = start; i < _memberStack.Count; i++)
                    {
                        if (string.Equals(_memberStack[i].Key, key, StringComparison.Ordinal))
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
                    _memberStack.Add(new KeyValuePair<string, JsonValue>(key, value));
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("objeto sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == '}')
                    {
                        return JsonValue.FromObject(PopMembers(start));
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
                int start = _itemStack.Count;
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == ']')
                {
                    _pos++;
                    return JsonValue.FromArray(PopItems(start));
                }

                while (true)
                {
                    SkipWhitespace();
                    JsonValue item = ReadValue(depth + 1);
                    _itemStack.Add(item);
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("lista sin cerrar");
                    }

                    char c = _text[_pos++];
                    if (c == ']')
                    {
                        return JsonValue.FromArray(PopItems(start));
                    }

                    if (c != ',')
                    {
                        throw Error("se esperaba ',' o ']'");
                    }
                }
            }

            // Los elementos de la lista que empieza en start, en un array del tamaño justo, fuera de la pila.
            private JsonValue[] PopItems(int start)
            {
                int count = _itemStack.Count - start;
                var items = new JsonValue[count];
                _itemStack.CopyTo(start, items, 0, count);
                _itemStack.RemoveRange(start, count);
                return items;
            }

            // Los miembros del objeto que empieza en start, en un array del tamaño justo, fuera de la pila.
            private KeyValuePair<string, JsonValue>[] PopMembers(int start)
            {
                int count = _memberStack.Count - start;
                var members = new KeyValuePair<string, JsonValue>[count];
                _memberStack.CopyTo(start, members, 0, count);
                _memberStack.RemoveRange(start, count);
                return members;
            }

            private string ReadString()
            {
                _pos++; // "
                int start = _pos;
                int end = start;
                while (end < _text.Length)
                {
                    char ch = _text[end];
                    if (ch == '"')
                    {
                        // Sin escapes: el texto tal cual, compartido si es corto.
                        _pos = end + 1;
                        return Slice(start, end - start);
                    }

                    if (ch == '\\' || ch < ' ')
                    {
                        break; // con escapes o con un carácter de control (que es un error): la lectura de siempre, desde ahí
                    }

                    end++;
                }

                var sb = new StringBuilder(end - start + 16);
                sb.Append(_text, start, end - start);
                _pos = end;
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

            private JsonValue ReadNumber()
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

                // Un entero pequeño en su forma canónica (solo dígitos, sin ceros delante): el valor compartido, sin texto nuevo.
                int length = _pos - start;
                if (length <= 4 && _text[start] != '-' && (length == 1 || _text[start] != '0'))
                {
                    int value = 0;
                    bool digits = true;
                    for (int i = start; i < _pos; i++)
                    {
                        char d = _text[i];
                        if (!IsDigit(d))
                        {
                            digits = false;
                            break;
                        }

                        value = (value * 10) + (d - '0');
                    }

                    if (digits && value <= JsonValue.SharedIntMax)
                    {
                        return JsonValue.SharedInt(value);
                    }
                }

                return JsonValue.FromNumber(_text.Substring(start, length));
            }

            // El texto [start, start + length) como cadena: si es corto, la misma cadena cada vez que se repite en el documento.
            private string Slice(int start, int length)
            {
                if (length == 0)
                {
                    return string.Empty;
                }

                if (length > MaxSharedStringLength)
                {
                    return _text.Substring(start, length);
                }

                uint hash = 2166136261u;
                for (int i = 0; i < length; i++)
                {
                    hash = (hash ^ _text[start + i]) * 16777619u;
                }

                int slot = (int)(hash & (SharedStringSlots - 1));
                string? shared = _shared[slot];
                if (shared != null && shared.Length == length && string.CompareOrdinal(_text, start, shared, 0, length) == 0)
                {
                    return shared;
                }

                shared = _text.Substring(start, length);
                _shared[slot] = shared;
                return shared;
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
