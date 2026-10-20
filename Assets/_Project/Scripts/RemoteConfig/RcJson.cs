#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.RemoteConfig
{
    /// <summary>Tipo de un nodo de <see cref="RcJson"/>.</summary>
    public enum RcJsonKind
    {
        /// <summary><c>null</c>.</summary>
        Null = 0,

        /// <summary><c>true</c> o <c>false</c>.</summary>
        Bool = 1,

        /// <summary>Número (se guarda su texto y su valor).</summary>
        Number = 2,

        /// <summary>Cadena.</summary>
        String = 3,

        /// <summary>Arreglo.</summary>
        Array = 4,

        /// <summary>Objeto (los miembros, en el orden del texto).</summary>
        Object = 5,
    }

    /// <summary>
    /// Nodo JSON inmutable del lector propio de <c>AF.RemoteConfig</c> (sin motor ni paquetes: 08 §2.1; como los lectores de
    /// <c>AF.Levels</c> y <c>AF.Save</c>, sin reflexión, por el Managed Stripping High de B10). Lee RFC 8259 completo salvo
    /// los escapes <c>\u</c> de pares sustitutos sueltos; rechaza claves repetidas y texto sobrante.
    /// </summary>
    public sealed class RcJson
    {
        private static readonly IReadOnlyList<RcJson> NoItems = new RcJson[0];
        private static readonly IReadOnlyList<KeyValuePair<string, RcJson>> NoMembers = new KeyValuePair<string, RcJson>[0];

        private RcJson(RcJsonKind kind, bool boolean = false, string? text = null, double number = 0,
            IReadOnlyList<RcJson>? items = null, IReadOnlyList<KeyValuePair<string, RcJson>>? members = null)
        {
            Kind = kind;
            Bool = boolean;
            Text = text;
            Number = number;
            Items = items ?? NoItems;
            Members = members ?? NoMembers;
        }

        /// <summary>Tipo del nodo.</summary>
        public RcJsonKind Kind { get; }

        /// <summary>Valor de un <see cref="RcJsonKind.Bool"/>.</summary>
        public bool Bool { get; }

        /// <summary>Cadena de un <see cref="RcJsonKind.String"/>, o el texto exacto de un <see cref="RcJsonKind.Number"/>.</summary>
        public string? Text { get; }

        /// <summary>Valor de un <see cref="RcJsonKind.Number"/>.</summary>
        public double Number { get; }

        /// <summary>Elementos de un arreglo.</summary>
        public IReadOnlyList<RcJson> Items { get; }

        /// <summary>Miembros de un objeto, en orden.</summary>
        public IReadOnlyList<KeyValuePair<string, RcJson>> Members { get; }

        /// <summary>El número es entero (sin punto ni exponente) y cabe en un <see cref="long"/>.</summary>
        public bool IsInteger => Kind == RcJsonKind.Number && Text!.IndexOfAny(new[] { '.', 'e', 'E' }) < 0
            && long.TryParse(Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);

        /// <summary>El miembro <paramref name="name"/> de un objeto, o null.</summary>
        public RcJson? this[string name]
        {
            get
            {
                for (int i = 0; i < Members.Count; i++)
                {
                    if (string.Equals(Members[i].Key, name, StringComparison.Ordinal))
                    {
                        return Members[i].Value;
                    }
                }

                return null;
            }
        }

        /// <summary>Lee <paramref name="json"/>. Devuelve false (y null) si no es JSON válido.</summary>
        public static bool TryParse(string? json, out RcJson? node, out string? error)
        {
            node = null;
            error = null;
            if (json == null)
            {
                error = "null";
                return false;
            }

            try
            {
                var reader = new Reader(json);
                reader.SkipSpace();
                node = reader.ReadValue(0);
                reader.SkipSpace();
                if (!reader.AtEnd)
                {
                    throw new FormatException("texto después del valor en " + reader.Position.ToString(CultureInfo.InvariantCulture));
                }

                return true;
            }
            catch (FormatException e)
            {
                node = null;
                error = e.Message;
                return false;
            }
        }

        /// <summary>Lee <paramref name="json"/>.</summary>
        /// <exception cref="FormatException">No es JSON válido.</exception>
        public static RcJson Parse(string json)
        {
            if (!TryParse(json, out RcJson? node, out string? error))
            {
                throw new FormatException("JSON no válido: " + error);
            }

            return node!;
        }

        /// <summary>El nodo en JSON compacto y canónico (sin espacios; los números con su texto original).</summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            Write(sb);
            return sb.ToString();
        }

        /// <summary>Escribe <paramref name="s"/> como cadena JSON.</summary>
        public static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
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
                        if (c < 0x20)
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

        private void Write(StringBuilder sb)
        {
            switch (Kind)
            {
                case RcJsonKind.Null: sb.Append("null"); break;
                case RcJsonKind.Bool: sb.Append(Bool ? "true" : "false"); break;
                case RcJsonKind.Number: sb.Append(Text); break;
                case RcJsonKind.String: AppendString(sb, Text!); break;
                case RcJsonKind.Array:
                    sb.Append('[');
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        Items[i].Write(sb);
                    }

                    sb.Append(']');
                    break;
                default:
                    sb.Append('{');
                    for (int i = 0; i < Members.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        AppendString(sb, Members[i].Key);
                        sb.Append(':');
                        Members[i].Value.Write(sb);
                    }

                    sb.Append('}');
                    break;
            }
        }

        private sealed class Reader
        {
            private const int MaxDepth = 64;
            private readonly string _s;
            private int _i;

            public Reader(string s)
            {
                _s = s;
            }

            public bool AtEnd => _i >= _s.Length;

            public int Position => _i;

            public void SkipSpace()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r'))
                {
                    _i++;
                }
            }

            public RcJson ReadValue(int depth)
            {
                if (depth > MaxDepth)
                {
                    throw new FormatException("demasiado anidado");
                }

                if (AtEnd)
                {
                    throw new FormatException("falta un valor");
                }

                char c = _s[_i];
                switch (c)
                {
                    case '{': return ReadObject(depth);
                    case '[': return ReadArray(depth);
                    case '"': return new RcJson(RcJsonKind.String, text: ReadString());
                    case 't': Expect("true"); return new RcJson(RcJsonKind.Bool, true);
                    case 'f': Expect("false"); return new RcJson(RcJsonKind.Bool, false);
                    case 'n': Expect("null"); return new RcJson(RcJsonKind.Null);
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                        {
                            return ReadNumber();
                        }

                        throw new FormatException("carácter inesperado en " + _i.ToString(CultureInfo.InvariantCulture));
                }
            }

            private RcJson ReadObject(int depth)
            {
                _i++;
                var members = new List<KeyValuePair<string, RcJson>>();
                SkipSpace();
                if (!AtEnd && _s[_i] == '}')
                {
                    _i++;
                    return new RcJson(RcJsonKind.Object, members: members);
                }

                while (true)
                {
                    SkipSpace();
                    if (AtEnd || _s[_i] != '"')
                    {
                        throw new FormatException("se esperaba una clave en " + _i.ToString(CultureInfo.InvariantCulture));
                    }

                    string key = ReadString();
                    for (int k = 0; k < members.Count; k++)
                    {
                        if (string.Equals(members[k].Key, key, StringComparison.Ordinal))
                        {
                            throw new FormatException("clave repetida «" + key + "»");
                        }
                    }

                    SkipSpace();
                    Require(':');
                    SkipSpace();
                    members.Add(new KeyValuePair<string, RcJson>(key, ReadValue(depth + 1)));
                    SkipSpace();
                    if (!AtEnd && _s[_i] == ',')
                    {
                        _i++;
                        continue;
                    }

                    Require('}');
                    return new RcJson(RcJsonKind.Object, members: members);
                }
            }

            private RcJson ReadArray(int depth)
            {
                _i++;
                var items = new List<RcJson>();
                SkipSpace();
                if (!AtEnd && _s[_i] == ']')
                {
                    _i++;
                    return new RcJson(RcJsonKind.Array, items: items);
                }

                while (true)
                {
                    SkipSpace();
                    items.Add(ReadValue(depth + 1));
                    SkipSpace();
                    if (!AtEnd && _s[_i] == ',')
                    {
                        _i++;
                        continue;
                    }

                    Require(']');
                    return new RcJson(RcJsonKind.Array, items: items);
                }
            }

            private string ReadString()
            {
                _i++;
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                    {
                        throw new FormatException("cadena sin cerrar");
                    }

                    char c = _s[_i++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }

                    if (c < 0x20)
                    {
                        throw new FormatException("carácter de control en una cadena");
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (AtEnd)
                    {
                        throw new FormatException("escape sin cerrar");
                    }

                    char e = _s[_i++];
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
                            if (_i + 4 > _s.Length || !int.TryParse(_s.Substring(_i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                            {
                                throw new FormatException("escape \\u no válido");
                            }

                            sb.Append((char)code);
                            _i += 4;
                            break;
                        default:
                            throw new FormatException("escape no válido \\" + e);
                    }
                }
            }

            private RcJson ReadNumber()
            {
                int start = _i;
                if (_s[_i] == '-')
                {
                    _i++;
                }

                int digits = Digits();
                if (digits == 0 || (digits > 1 && _s[_i - digits] == '0'))
                {
                    throw new FormatException("número no válido en " + start.ToString(CultureInfo.InvariantCulture));
                }

                if (!AtEnd && _s[_i] == '.')
                {
                    _i++;
                    if (Digits() == 0)
                    {
                        throw new FormatException("número no válido en " + start.ToString(CultureInfo.InvariantCulture));
                    }
                }

                if (!AtEnd && (_s[_i] == 'e' || _s[_i] == 'E'))
                {
                    _i++;
                    if (!AtEnd && (_s[_i] == '+' || _s[_i] == '-'))
                    {
                        _i++;
                    }

                    if (Digits() == 0)
                    {
                        throw new FormatException("número no válido en " + start.ToString(CultureInfo.InvariantCulture));
                    }
                }

                string text = _s.Substring(start, _i - start);
                double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (double.IsInfinity(value) || double.IsNaN(value))
                {
                    throw new FormatException("número no finito en " + start.ToString(CultureInfo.InvariantCulture));
                }
                return new RcJson(RcJsonKind.Number, text: text, number: value);
            }

            private int Digits()
            {
                int n = 0;
                while (!AtEnd && _s[_i] >= '0' && _s[_i] <= '9')
                {
                    _i++;
                    n++;
                }

                return n;
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0)
                {
                    throw new FormatException("se esperaba «" + word + "» en " + _i.ToString(CultureInfo.InvariantCulture));
                }

                _i += word.Length;
            }

            private void Require(char c)
            {
                if (AtEnd || _s[_i] != c)
                {
                    throw new FormatException("se esperaba «" + c + "» en " + _i.ToString(CultureInfo.InvariantCulture));
                }

                _i++;
            }
        }
    }
}
