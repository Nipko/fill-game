#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Backend.Firebase;
#if UNITY_ANDROID && !UNITY_EDITOR
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

namespace AF.RemoteConfig.Firebase
{
    /// <summary>
    /// Proveedor real de Remote Config: Firebase Remote Config (F3-6; D-039; 08 §2.2-§2.3 y §9), detrás de
    /// <see cref="IRemoteConfigBackend"/>. Solo en el teléfono con la configuración de Firebase (<see cref="RemoteConfigBackends"/>).
    /// <list type="bullet">
    /// <item><see cref="Cached"/>: lo último descargado, de un archivo propio (<see cref="RcCacheFile"/>), que se lee sin red ni SDK al
    /// arrancar (08 §2.3: <c>RC.ActivateCached()</c> es síncrono; Firebase tarda en estar listo).</item>
    /// <item><see cref="Fetch"/>: fija el intervalo mínimo de 12 h (<see cref="RemoteConfigService.MinFetchInterval"/>) y los defaults
    /// compilados (<see cref="RemoteConfigDefaults"/>), descarga, activa en Firebase y guarda en el archivo los valores que vienen
    /// del servidor. <see cref="RemoteConfigService"/> no los aplica hasta el arranque siguiente (las cifras no cambian a mitad de
    /// sesión).</item>
    /// </list>
    /// Solo desde el hilo principal.
    /// </summary>
    public sealed class FirebaseRemoteConfigBackend : IRemoteConfigBackend
    {
        private readonly RemoteConfigDefaults _defaults;
        private readonly string _cachePath;
        private IReadOnlyDictionary<string, string>? _cached;
        private bool _fetching;

        /// <summary>Crea el proveedor con la caché de <paramref name="cachePath"/> (si no existe o no se puede leer, sin caché).</summary>
        public FirebaseRemoteConfigBackend(RemoteConfigDefaults defaults, string cachePath)
        {
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
            _cachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
            if (RcCacheFile.TryRead(_cachePath, out Dictionary<string, string>? values, out DateTime? fetchedAt))
            {
                _cached = values;
                LastFetchUtc = fetchedAt;
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string>? Cached => _cached;

        /// <summary>Hora de la última descarga correcta (la de la caché), o null.</summary>
        public DateTime? LastFetchUtc { get; private set; }

        /// <summary>Anota la hora de una descarga correcta en la caché (la da el reloj del juego, no este proveedor).</summary>
        public void MarkFetched(DateTime nowUtc)
        {
            LastFetchUtc = nowUtc;
            if (_cached != null)
            {
                RcCacheFile.TryWrite(_cachePath, _cached, nowUtc);
            }
        }

        /// <inheritdoc />
        public void Fetch(Action<IReadOnlyDictionary<string, string>?> onDone)
        {
            if (_fetching)
            {
                onDone?.Invoke(null);
                return;
            }

            _fetching = true;
            FirebaseBoot.WhenReady(ok =>
            {
                if (!ok)
                {
                    Done(null, onDone);
                    return;
                }

                try
                {
                    FetchFromFirebase(onDone);
                }
                catch (Exception)
                {
                    Done(null, onDone); // una excepción síncrona del SDK no deja la descarga colgada
                }
            });
        }

        private void Done(IReadOnlyDictionary<string, string>? values, Action<IReadOnlyDictionary<string, string>?>? onDone)
        {
            if (!_fetching)
            {
                return; // ya se respondió (onDone, una sola vez)
            }

            _fetching = false;
            if (values != null)
            {
                _cached = values;
                RcCacheFile.TryWrite(_cachePath, values, LastFetchUtc);
            }

            onDone?.Invoke(values);
        }

        /// <summary>Los defaults compilados como los quiere Firebase (clave → texto); las claves sin valor (tablas) no van.</summary>
        public static Dictionary<string, object> DefaultsFor(RemoteConfigDefaults defaults)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            IReadOnlyList<RcParam> all = defaults.Params;
            for (int i = 0; i < all.Count; i++)
            {
                RcParam p = all[i];
                if (p.Default.HasValue)
                {
                    result[p.Key] = p.Default.Value.ToRemoteString();
                }
            }

            return result;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool _defaultsSet;

        private void FetchFromFirebase(Action<IReadOnlyDictionary<string, string>?> onDone)
        {
            FirebaseRemoteConfig rc = FirebaseRemoteConfig.DefaultInstance;
            var settings = new ConfigSettings
            {
                MinimumFetchIntervalInMilliseconds = (ulong)RemoteConfigService.MinFetchInterval.TotalMilliseconds,
                FetchTimeoutInMilliseconds = FirebaseRemoteConfig.DefaultTimeoutInMilliseconds,
            };
            rc.SetConfigSettingsAsync(settings).ContinueWithOnMainThread(configured => Step(onDone, () =>
            {
                if (configured.IsFaulted || configured.IsCanceled)
                {
                    Done(null, onDone);
                    return;
                }

                if (_defaultsSet)
                {
                    FetchAndActivate(rc, onDone);
                    return;
                }

                rc.SetDefaultsAsync(DefaultsFor(_defaults)).ContinueWithOnMainThread(withDefaults => Step(onDone, () =>
                {
                    if (withDefaults.IsFaulted || withDefaults.IsCanceled)
                    {
                        Done(null, onDone);
                        return;
                    }

                    _defaultsSet = true;
                    FetchAndActivate(rc, onDone);
                }));
            }));
        }

        private void FetchAndActivate(FirebaseRemoteConfig rc, Action<IReadOnlyDictionary<string, string>?> onDone)
        {
            rc.FetchAsync().ContinueWithOnMainThread(fetched => Step(onDone, () =>
            {
                if (fetched.IsFaulted || fetched.IsCanceled)
                {
                    Done(null, onDone); // sin red, o Firebase lo limitó: se queda la caché
                    return;
                }

                rc.ActivateAsync().ContinueWithOnMainThread(activated => Step(onDone, () =>
                {
                    if (activated.IsFaulted || activated.IsCanceled)
                    {
                        Done(null, onDone);
                        return;
                    }

                    var values = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (KeyValuePair<string, ConfigValue> kv in rc.AllValues)
                    {
                        if (kv.Value.Source == ValueSource.RemoteValue)
                        {
                            values[kv.Key] = kv.Value.StringValue;
                        }
                    }

                    Done(values, onDone);
                }));
            }));
        }

        // Cada paso de la cadena: una excepción del SDK responde null (una sola vez) y no deja la descarga colgada.
        private void Step(Action<IReadOnlyDictionary<string, string>?> onDone, Action body)
        {
            try
            {
                body();
            }
            catch (Exception)
            {
                Done(null, onDone);
            }
        }
#else
        private void FetchFromFirebase(Action<IReadOnlyDictionary<string, string>?> onDone)
        {
            Done(null, onDone); // nunca llega aquí: FirebaseBoot no está listo fuera del teléfono
        }
#endif
    }

    /// <summary>Elige el proveedor de Remote Config del build.</summary>
    public static class RemoteConfigBackends
    {
        /// <summary>Nombre del archivo de la caché en <c>Application.persistentDataPath</c> [P].</summary>
        public const string CacheFileName = "af_rc_cache.json";

        /// <summary>
        /// Firebase Remote Config en el teléfono con la configuración de Firebase; si no (editor, tests o un build sin
        /// <c>google-services.json</c>), el falso vacío de siempre (<see cref="FakeRemoteConfigBackend"/>): defaults compilados.
        /// </summary>
        public static IRemoteConfigBackend CreateDefault(RemoteConfigDefaults defaults)
        {
            if (!FirebaseBoot.IsConfigured)
            {
                return new FakeRemoteConfigBackend();
            }

            return new FirebaseRemoteConfigBackend(defaults, Path.Combine(UnityEngine.Application.persistentDataPath, CacheFileName));
        }
    }

    /// <summary>
    /// La caché de Remote Config en disco (formato <c>af-rc-cache/1</c> [P]): <c>{"format":…,"fetched_at":…,"values":{clave:texto}}</c>.
    /// Solo valores de Remote Config (cifras e interruptores; ningún dato personal). Escritura a un temporal y copia, para no dejar
    /// medio archivo.
    /// </summary>
    public static class RcCacheFile
    {
        /// <summary>Formato del archivo [P].</summary>
        public const string Format = "af-rc-cache/1";

        /// <summary>Lee la caché; false si no existe, no se puede leer o no es de este formato.</summary>
        public static bool TryRead(string path, out Dictionary<string, string>? values, out DateTime? fetchedAt)
        {
            values = null;
            fetchedAt = null;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                if (!RcJson.TryParse(File.ReadAllText(path, Encoding.UTF8), out RcJson? root, out _) || root == null || root.Kind != RcJsonKind.Object)
                {
                    return false;
                }

                RcJson? format = root["format"];
                RcJson? map = root["values"];
                if (format == null || format.Kind != RcJsonKind.String || format.Text != Format || map == null || map.Kind != RcJsonKind.Object)
                {
                    return false;
                }

                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < map.Members.Count; i++)
                {
                    KeyValuePair<string, RcJson> m = map.Members[i];
                    if (m.Value.Kind == RcJsonKind.String && m.Value.Text != null)
                    {
                        result[m.Key] = m.Value.Text;
                    }
                }

                RcJson? at = root["fetched_at"];
                if (at != null && at.Kind == RcJsonKind.String
                    && DateTime.TryParseExact(at.Text, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
                {
                    fetchedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }

                values = result;
                return true;
            }
            catch (Exception)
            {
                values = null;
                fetchedAt = null;
                return false;
            }
        }

        /// <summary>Escribe la caché (claves en orden ordinal). False si no se pudo.</summary>
        public static bool TryWrite(string path, IReadOnlyDictionary<string, string> values, DateTime? fetchedAt)
        {
            try
            {
                File.WriteAllText(path + ".tmp", ToJson(values, fetchedAt), new UTF8Encoding(false));
                File.Copy(path + ".tmp", path, true);
                File.Delete(path + ".tmp");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>El texto del archivo.</summary>
        public static string ToJson(IReadOnlyDictionary<string, string> values, DateTime? fetchedAt)
        {
            var keys = new List<string>(values.Keys);
            keys.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.Append("{\"format\":");
            RcJson.AppendString(sb, Format);
            if (fetchedAt.HasValue)
            {
                sb.Append(",\"fetched_at\":");
                RcJson.AppendString(sb, fetchedAt.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            }

            sb.Append(",\"values\":{");
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                RcJson.AppendString(sb, keys[i]);
                sb.Append(':');
                RcJson.AppendString(sb, values[keys[i]]);
            }

            return sb.Append("}}").ToString();
        }
    }
}
