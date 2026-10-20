#nullable enable
using System;
using System.Collections.Generic;

namespace AF.RemoteConfig
{
    /// <summary>Tipo de una clave de 08 §9 (columna «Tipo»; <c>enum</c> se guarda como string y <c>map</c> como json).</summary>
    public enum RcType
    {
        /// <summary>Entero.</summary>
        Int = 0,

        /// <summary>Decimal.</summary>
        Float = 1,

        /// <summary>Booleano.</summary>
        Bool = 2,

        /// <summary>Cadena.</summary>
        String = 3,

        /// <summary>JSON (objeto, arreglo o mapa); se lee como texto JSON compacto.</summary>
        Json = 4,
    }

    /// <summary>Una clave de la tabla única de 08 §9 con su default compilado.</summary>
    public sealed class RcParam
    {
        internal RcParam(string key, RcType type, RcValue? defaultValue, string? pending)
        {
            Key = key;
            Type = type;
            Default = defaultValue;
            Pending = pending;
        }

        /// <summary>La clave exacta de 08 §9.</summary>
        public string Key { get; }

        /// <summary>Su tipo.</summary>
        public RcType Type { get; }

        /// <summary>El default compilado; null si 08 §9 remite a otra tabla (<see cref="Pending"/>).</summary>
        public RcValue? Default { get; }

        /// <summary>El texto de la celda de default cuando no es un valor (por ejemplo, «tabla 05 §2.3»); null si lo es.</summary>
        public string? Pending { get; }
    }

    /// <summary>
    /// Los <b>defaults compilados</b> de Remote Config (08 §2.2 y §9): <c>Assets/_Project/Data/Config/RemoteConfigDefaults.json</c>,
    /// que genera <c>tools/remote-config/rc_defaults.py</c> desde la tabla única de 08 §9 y que el mismo generador copia en
    /// <c>RemoteConfigDefaults.Generated.cs</c> como constante. Sin motor: se leen del ensamblado, sin cargar ningún asset.
    /// </summary>
    public sealed class RemoteConfigDefaults
    {
        /// <summary>Formato del archivo [P].</summary>
        public const string Format = "af-rc-defaults/1";

        private static RemoteConfigDefaults? s_compiled;
        private readonly Dictionary<string, RcParam> _byKey;

        private RemoteConfigDefaults(List<RcParam> ordered, Dictionary<string, RcParam> byKey, IReadOnlyList<string> pendingTemplates)
        {
            Params = ordered;
            _byKey = byKey;
            PendingTemplates = pendingTemplates;
        }

        /// <summary>Los defaults del JSON compilado (se leen una vez).</summary>
        public static RemoteConfigDefaults Compiled => s_compiled ??= Parse(RemoteConfigDefaultsData.Json);

        /// <summary>El texto del JSON compilado, igual byte a byte que <c>RemoteConfigDefaults.json</c>.</summary>
        public static string CompiledJson => RemoteConfigDefaultsData.Json;

        /// <summary>Las claves, en el orden de 08 §9.</summary>
        public IReadOnlyList<RcParam> Params { get; }

        /// <summary>Plantillas de 08 §9 sin clave concreta (<c>eco_price_tier_&lt;country&gt;</c>).</summary>
        public IReadOnlyList<string> PendingTemplates { get; }

        /// <summary>Número de claves.</summary>
        public int Count => Params.Count;

        /// <summary>La clave <paramref name="key"/>, o null si no está en 08 §9.</summary>
        public RcParam? Find(string key)
        {
            return key != null && _byKey.TryGetValue(key, out RcParam? p) ? p : null;
        }

        /// <summary>Lee un JSON <c>af-rc-defaults/1</c>.</summary>
        /// <exception cref="FormatException">No es un JSON de ese formato o algún default no es de su tipo.</exception>
        public static RemoteConfigDefaults Parse(string json)
        {
            RcJson root = RcJson.Parse(json);
            if (root.Kind != RcJsonKind.Object || root["format"]?.Text != Format)
            {
                throw new FormatException("RemoteConfigDefaults: falta \"format\": \"" + Format + "\".");
            }

            RcJson? ps = root["params"];
            if (ps == null || ps.Kind != RcJsonKind.Object)
            {
                throw new FormatException("RemoteConfigDefaults: falta \"params\".");
            }

            var ordered = new List<RcParam>(ps.Members.Count);
            var byKey = new Dictionary<string, RcParam>(ps.Members.Count, StringComparer.Ordinal);
            for (int i = 0; i < ps.Members.Count; i++)
            {
                string key = ps.Members[i].Key;
                RcJson entry = ps.Members[i].Value;
                RcType type = ParseType(entry["type"]?.Text, key);
                RcJson? d = entry["default"];
                string? pending = entry["pending"]?.Text;
                RcValue? value = null;
                if (d == null || d.Kind == RcJsonKind.Null)
                {
                    if (string.IsNullOrEmpty(pending))
                    {
                        throw new FormatException("RemoteConfigDefaults: " + key + " sin default ni \"pending\".");
                    }
                }
                else
                {
                    if (!RcValue.TryFromJson(type, d, out RcValue v))
                    {
                        throw new FormatException("RemoteConfigDefaults: el default de " + key + " no es " + type + ".");
                    }

                    value = v;
                }

                var p = new RcParam(key, type, value, pending);
                ordered.Add(p);
                byKey.Add(key, p);
            }

            var templates = new List<string>();
            RcJson? pt = root["pending_templates"];
            if (pt != null)
            {
                for (int i = 0; i < pt.Members.Count; i++)
                {
                    templates.Add(pt.Members[i].Key);
                }
            }

            return new RemoteConfigDefaults(ordered, byKey, templates);
        }

        private static RcType ParseType(string? type, string key)
        {
            switch (type)
            {
                case "int": return RcType.Int;
                case "float": return RcType.Float;
                case "bool": return RcType.Bool;
                case "string": return RcType.String;
                case "json": return RcType.Json;
                default: throw new FormatException("RemoteConfigDefaults: tipo desconocido «" + type + "» en " + key + ".");
            }
        }
    }
}
