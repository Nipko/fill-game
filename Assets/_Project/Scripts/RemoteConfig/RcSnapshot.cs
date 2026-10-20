#nullable enable
using System;
using System.Collections.Generic;

namespace AF.RemoteConfig
{
    /// <summary>
    /// Valores de Remote Config congelados en un instante (08 §2.2: «congelación de valores al abrir cada pantalla»; 04 §10.2:
    /// «Precios y topes se leen de la caché de RC al abrir cada modal o botón; nunca cambian dentro de un modal abierto»).
    /// Inmutable: una activación posterior (<see cref="RemoteConfigService.Activate"/>) no cambia este objeto. Lectura
    /// tipada con <see cref="Get{T}"/>; también es lo que lee <see cref="RemoteConfigService"/> cada vez (su valor vigente).
    /// </summary>
    public sealed class RcSnapshot
    {
        private readonly RemoteConfigDefaults _defaults;
        private readonly IReadOnlyDictionary<string, KeyValuePair<RcValue, RcSource>> _overrides;

        internal RcSnapshot(RemoteConfigDefaults defaults, IReadOnlyDictionary<string, KeyValuePair<RcValue, RcSource>> overrides, long version)
        {
            _defaults = defaults;
            _overrides = overrides;
            Version = version;
        }

        /// <summary>Número de la activación de la que salen estos valores (0 = solo defaults).</summary>
        public long Version { get; }

        /// <summary>Los defaults compilados de debajo.</summary>
        public RemoteConfigDefaults Defaults => _defaults;

        /// <summary>
        /// El valor de <paramref name="key"/> como <typeparamref name="T"/>: <c>int</c>/<c>long</c> para <c>int</c>,
        /// <c>double</c>/<c>float</c> para <c>float</c> (y para <c>int</c>), <c>bool</c>, <c>string</c> para <c>string</c> y
        /// para <c>json</c> (el texto compacto) y <see cref="RcJson"/> para <c>json</c>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">La clave no está en 08 §9 (no se inventan claves).</exception>
        /// <exception cref="InvalidOperationException"><typeparamref name="T"/> no sirve para el tipo de la clave, o la clave no
        /// tiene default compilado (<see cref="RcParam.Pending"/>) ni valor descargado.</exception>
        public T Get<T>(string key)
        {
            RcValue v = Value(key);
            return Convert<T>(key, v);
        }

        /// <summary>Como <see cref="Get{T}"/>, sin excepciones de valor: false si no hay valor o el tipo no sirve.</summary>
        /// <exception cref="KeyNotFoundException">La clave no está en 08 §9.</exception>
        public bool TryGet<T>(string key, out T value)
        {
            value = default!;
            RcParam p = Param(key);
            RcValue v;
            if (_overrides.TryGetValue(key, out KeyValuePair<RcValue, RcSource> o))
            {
                v = o.Key;
            }
            else if (p.Default.HasValue)
            {
                v = p.Default.Value;
            }
            else
            {
                return false;
            }

            if (!Accepts<T>(v.Type) || (typeof(T) == typeof(int) && (v.Integer < int.MinValue || v.Integer > int.MaxValue)))
            {
                return false;
            }

            value = Convert<T>(key, v);
            return true;
        }

        /// <summary>El valor crudo de <paramref name="key"/>.</summary>
        /// <exception cref="KeyNotFoundException">La clave no está en 08 §9.</exception>
        /// <exception cref="InvalidOperationException">Sin default compilado ni valor descargado.</exception>
        public RcValue Value(string key)
        {
            RcParam p = Param(key);
            if (_overrides.TryGetValue(key, out KeyValuePair<RcValue, RcSource> o))
            {
                return o.Key;
            }

            if (!p.Default.HasValue)
            {
                throw new InvalidOperationException("Remote Config: «" + key + "» no tiene default compilado (08 §9: " + p.Pending + ") ni valor descargado.");
            }

            return p.Default.Value;
        }

        /// <summary>De dónde sale el valor vigente de <paramref name="key"/>.</summary>
        /// <exception cref="KeyNotFoundException">La clave no está en 08 §9.</exception>
        public RcSource SourceOf(string key)
        {
            Param(key);
            return _overrides.TryGetValue(key, out KeyValuePair<RcValue, RcSource> o) ? o.Value : RcSource.Default;
        }

        /// <summary>
        /// El origen del conjunto (para <c>af_boot.rc_source</c>, 08 §8.2): <c>Remote</c> si algún valor es remoto; si no,
        /// <c>Cached</c> si alguno viene de la caché; si no, <c>Default</c>.
        /// </summary>
        public RcSource Source
        {
            get
            {
                RcSource best = RcSource.Default;
                foreach (KeyValuePair<RcValue, RcSource> o in _overrides.Values)
                {
                    if (o.Value > best)
                    {
                        best = o.Value;
                    }
                }

                return best;
            }
        }

        private RcParam Param(string key)
        {
            RcParam? p = _defaults.Find(key);
            if (p == null)
            {
                throw new KeyNotFoundException("Remote Config: «" + key + "» no está en la tabla única de 08 §9.");
            }

            return p;
        }

        private static bool Accepts<T>(RcType type)
        {
            Type t = typeof(T);
            switch (type)
            {
                case RcType.Int: return t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float);
                case RcType.Float: return t == typeof(double) || t == typeof(float);
                case RcType.Bool: return t == typeof(bool);
                case RcType.String: return t == typeof(string);
                case RcType.Json: return t == typeof(string) || t == typeof(RcJson);
                default: return false;
            }
        }

        private static T Convert<T>(string key, RcValue v)
        {
            if (!Accepts<T>(v.Type))
            {
                throw new InvalidOperationException("Remote Config: «" + key + "» es " + v.Type + " y se pidió como " + typeof(T).Name + ".");
            }

            Type t = typeof(T);
            if (t == typeof(int))
            {
                if (v.Integer < int.MinValue || v.Integer > int.MaxValue)
                {
                    throw new InvalidOperationException("Remote Config: «" + key + "» no cabe en un int.");
                }

                return (T)(object)(int)v.Integer;
            }

            if (t == typeof(long))
            {
                return (T)(object)v.Integer;
            }

            if (t == typeof(double))
            {
                return (T)(object)v.Number;
            }

            if (t == typeof(float))
            {
                return (T)(object)(float)v.Number;
            }

            if (t == typeof(bool))
            {
                return (T)(object)v.Boolean;
            }

            if (t == typeof(RcJson))
            {
                return (T)(object)RcJson.Parse(v.Text!);
            }

            return (T)(object)v.Text!;
        }
    }
}
