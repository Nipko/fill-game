#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Economy
{
    /// <summary>Un miembro de un objeto JSON plano: su clave y su valor (una cadena o un entero, en texto).</summary>
    internal readonly struct FlatMember
    {
        public FlatMember(string key, string value, bool isString)
        {
            Key = key;
            Value = value;
            IsString = isString;
        }

        public string Key { get; }

        public string Value { get; }

        public bool IsString { get; }
    }

    /// <summary>
    /// Lector mínimo de los dos JSON de Remote Config de la Crecida y la Canasta (nota <c>docs/dev/crecida-canasta.md</c> §2):
    /// un objeto plano de cadenas o enteros (<c>streak_milestones</c>) y una lista de cadenas (<c>eco_basket_cycle</c>).
    /// <c>AF.Economy</c> no puede usar el lector de <c>AF.Levels</c> (08 §2.2) ni los internos de <c>AF.Save</c>. No lanza:
    /// ante cualquier forma inesperada devuelve false y el llamador se queda con el default de 08 §9.
    /// </summary>
    internal static class EconomyJsonLite
    {
        /// <summary>Lee <c>{"clave": "texto" | entero, …}</c> sin anidar. Claves repetidas: false.</summary>
        public static bool TryReadFlatObject(string? json, out List<FlatMember> members)
        {
            members = new List<FlatMember>();
            if (json == null)
            {
                return false;
            }

            int pos = 0;
            SkipWhitespace(json, ref pos);
            if (!Expect(json, ref pos, '{'))
            {
                return false;
            }

            SkipWhitespace(json, ref pos);
            if (Peek(json, pos) == '}')
            {
                pos++;
                return AtEnd(json, pos);
            }

            while (true)
            {
                SkipWhitespace(json, ref pos);
                if (!TryReadString(json, ref pos, out string key))
                {
                    return false;
                }

                for (int i = 0; i < members.Count; i++)
                {
                    if (string.Equals(members[i].Key, key, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                SkipWhitespace(json, ref pos);
                if (!Expect(json, ref pos, ':'))
                {
                    return false;
                }

                SkipWhitespace(json, ref pos);
                if (Peek(json, pos) == '"')
                {
                    if (!TryReadString(json, ref pos, out string text))
                    {
                        return false;
                    }

                    members.Add(new FlatMember(key, text, true));
                }
                else
                {
                    if (!TryReadInteger(json, ref pos, out string number))
                    {
                        return false;
                    }

                    members.Add(new FlatMember(key, number, false));
                }

                SkipWhitespace(json, ref pos);
                char c = Peek(json, pos);
                pos++;
                if (c == ',')
                {
                    continue;
                }

                return c == '}' && AtEnd(json, pos);
            }
        }

        /// <summary>Lee <c>["a", "b", …]</c>. Una lista vacía es válida (quien llama decide si la admite).</summary>
        public static bool TryReadStringArray(string? json, out List<string> items)
        {
            items = new List<string>();
            if (json == null)
            {
                return false;
            }

            int pos = 0;
            SkipWhitespace(json, ref pos);
            if (!Expect(json, ref pos, '['))
            {
                return false;
            }

            SkipWhitespace(json, ref pos);
            if (Peek(json, pos) == ']')
            {
                pos++;
                return AtEnd(json, pos);
            }

            while (true)
            {
                SkipWhitespace(json, ref pos);
                if (!TryReadString(json, ref pos, out string item))
                {
                    return false;
                }

                items.Add(item);
                SkipWhitespace(json, ref pos);
                char c = Peek(json, pos);
                pos++;
                if (c == ',')
                {
                    continue;
                }

                return c == ']' && AtEnd(json, pos);
            }
        }

        /// <summary>Escribe <paramref name="text"/> como cadena JSON (con comillas y escapes).</summary>
        public static void AppendString(StringBuilder sb, string text)
        {
            sb.Append('"');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
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

        private static bool AtEnd(string json, int pos)
        {
            SkipWhitespace(json, ref pos);
            return pos >= json.Length;
        }

        private static char Peek(string json, int pos)
        {
            return pos < json.Length ? json[pos] : '\0';
        }

        private static bool Expect(string json, ref int pos, char c)
        {
            if (Peek(json, pos) != c)
            {
                return false;
            }

            pos++;
            return true;
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && (json[pos] == ' ' || json[pos] == '\t' || json[pos] == '\n' || json[pos] == '\r'))
            {
                pos++;
            }
        }

        private static bool TryReadString(string json, ref int pos, out string value)
        {
            value = "";
            if (!Expect(json, ref pos, '"'))
            {
                return false;
            }

            var sb = new StringBuilder();
            while (pos < json.Length)
            {
                char c = json[pos++];
                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                if (c < ' ')
                {
                    return false;
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (pos >= json.Length)
                {
                    return false;
                }

                char e = json[pos++];
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
                        if (pos + 4 > json.Length
                            || !int.TryParse(json.Substring(pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                        {
                            return false;
                        }

                        sb.Append((char)code);
                        pos += 4;
                        break;
                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool TryReadInteger(string json, ref int pos, out string value)
        {
            value = "";
            int start = pos;
            if (Peek(json, pos) == '-')
            {
                pos++;
            }

            int digits = pos;
            while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
            {
                pos++;
            }

            if (pos == digits)
            {
                return false;
            }

            char next = Peek(json, pos);
            if (next == '.' || next == 'e' || next == 'E')
            {
                return false;
            }

            value = json.Substring(start, pos - start);
            return true;
        }
    }
}
