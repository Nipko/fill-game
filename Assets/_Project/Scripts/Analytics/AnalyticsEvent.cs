#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Analytics
{
    /// <summary>Tipo de un parámetro de analítica (los de Firebase Analytics: texto, entero y decimal; el bool va como 0/1 [P]).</summary>
    public enum ParamKind
    {
        /// <summary>Texto (ids y claves, nunca texto visible: 08 §8.1; <c>.claude/rules/localizacion.md</c>).</summary>
        String = 0,

        /// <summary>Entero.</summary>
        Long = 1,

        /// <summary>Decimal.</summary>
        Double = 2,

        /// <summary>Booleano.</summary>
        Bool = 3,
    }

    /// <summary>Un parámetro de un evento.</summary>
    public readonly struct AnalyticsParam
    {
        private AnalyticsParam(string name, ParamKind kind, string? text, long integer, double number, bool boolean)
        {
            Name = name;
            Kind = kind;
            Text = text;
            Long = integer;
            Double = number;
            Bool = boolean;
        }

        /// <summary>Nombre del parámetro (08 §8.2).</summary>
        public string Name { get; }

        /// <summary>Tipo.</summary>
        public ParamKind Kind { get; }

        /// <summary>Valor de un texto.</summary>
        public string? Text { get; }

        /// <summary>Valor de un entero.</summary>
        public long Long { get; }

        /// <summary>Valor de un decimal.</summary>
        public double Double { get; }

        /// <summary>Valor de un booleano.</summary>
        public bool Bool { get; }

        internal static AnalyticsParam Of(string name, string value) => new AnalyticsParam(name, ParamKind.String, value, 0, 0, false);

        internal static AnalyticsParam Of(string name, long value) => new AnalyticsParam(name, ParamKind.Long, null, value, 0, false);

        internal static AnalyticsParam Of(string name, double value) => new AnalyticsParam(name, ParamKind.Double, null, 0, value, false);

        internal static AnalyticsParam Of(string name, bool value) => new AnalyticsParam(name, ParamKind.Bool, null, 0, 0, value);

        /// <summary>El valor en JSON.</summary>
        public void AppendJson(StringBuilder sb)
        {
            switch (Kind)
            {
                case ParamKind.String: AppendString(sb, Text!); break;
                case ParamKind.Long: sb.Append(Long.ToString(CultureInfo.InvariantCulture)); break;
                case ParamKind.Double: sb.Append(Double.ToString("R", CultureInfo.InvariantCulture)); break;
                default: sb.Append(Bool ? "true" : "false"); break;
            }
        }

        /// <summary>El valor como texto (para las pruebas y los sumideros que solo aceptan texto).</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            AppendJson(sb);
            return sb.ToString();
        }

        /// <summary>Escribe <paramref name="s"/> como cadena JSON.</summary>
        public static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c < 0x20)
                {
                    sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
        }
    }

    /// <summary>
    /// Un evento de la taxonomía única de 08 §8.2, con sus parámetros específicos (<see cref="Params"/>) y, cuando lo envía
    /// <see cref="AnalyticsService"/>, los comunes de 08 §8.1 (<see cref="Common"/>). Se construye con <see cref="Create"/> y
    /// <c>Add</c>; el nombre y cada parámetro se comprueban contra <see cref="AnalyticsCatalog"/> al enviarlo.
    /// </summary>
    public sealed class AnalyticsEvent
    {
        private readonly List<AnalyticsParam> _params = new List<AnalyticsParam>();
        private readonly List<AnalyticsParam> _common = new List<AnalyticsParam>();

        private AnalyticsEvent(string name)
        {
            Name = name;
        }

        /// <summary>Nombre del evento (08 §8.2).</summary>
        public string Name { get; }

        /// <summary>Parámetros específicos, en el orden en que se añadieron.</summary>
        public IReadOnlyList<AnalyticsParam> Params => _params;

        /// <summary>Parámetros comunes (08 §8.1), en su orden.</summary>
        public IReadOnlyList<AnalyticsParam> Common => _common;

        /// <summary>Un evento nuevo, sin parámetros.</summary>
        public static AnalyticsEvent Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Falta el nombre del evento.", nameof(name));
            }

            return new AnalyticsEvent(name);
        }

        /// <summary>Añade un texto.</summary>
        public AnalyticsEvent Add(string name, string value)
        {
            _params.Add(AnalyticsParam.Of(name, value ?? throw new ArgumentNullException(nameof(value))));
            return this;
        }

        /// <summary>Añade un entero.</summary>
        public AnalyticsEvent Add(string name, long value)
        {
            _params.Add(AnalyticsParam.Of(name, value));
            return this;
        }

        /// <summary>Añade un decimal.</summary>
        public AnalyticsEvent Add(string name, double value)
        {
            _params.Add(AnalyticsParam.Of(name, value));
            return this;
        }

        /// <summary>Añade un booleano.</summary>
        public AnalyticsEvent Add(string name, bool value)
        {
            _params.Add(AnalyticsParam.Of(name, value));
            return this;
        }

        /// <summary>El parámetro <paramref name="name"/> (específico o común), o null.</summary>
        public AnalyticsParam? Find(string name)
        {
            for (int i = 0; i < _params.Count; i++)
            {
                if (string.Equals(_params[i].Name, name, StringComparison.Ordinal))
                {
                    return _params[i];
                }
            }

            for (int i = 0; i < _common.Count; i++)
            {
                if (string.Equals(_common[i].Name, name, StringComparison.Ordinal))
                {
                    return _common[i];
                }
            }

            return null;
        }

        /// <summary>
        /// La línea JSON del evento: <c>{"event":"…",&lt;específicos&gt;,&lt;comunes&gt;}</c>, en una línea, específicos primero
        /// (formato del sumidero de desarrollo [P]; nota §4).
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder(96);
            sb.Append("{\"event\":");
            AnalyticsParam.AppendString(sb, Name);
            Append(sb, _params);
            Append(sb, _common);
            return sb.Append('}').ToString();
        }

        internal void RemoveParamAt(int index) => _params.RemoveAt(index);

        internal void ClearCommon() => _common.Clear();

        internal void AddCommon(AnalyticsParam p) => _common.Add(p);

        private static void Append(StringBuilder sb, List<AnalyticsParam> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(',');
                AnalyticsParam.AppendString(sb, list[i].Name);
                sb.Append(':');
                list[i].AppendJson(sb);
            }
        }
    }
}
