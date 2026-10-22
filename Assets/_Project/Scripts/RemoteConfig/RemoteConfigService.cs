#nullable enable
using System;
using System.Collections.Generic;

namespace AF.RemoteConfig
{
    /// <summary>
    /// El proveedor de valores remotos detrás de <see cref="RemoteConfigService"/> (08 §2.1, principio 6: el SDK, Firebase
    /// Remote Config, se toca solo a través de esta interfaz; en el editor y en la fase 2, <see cref="FakeRemoteConfigBackend"/>).
    /// Los valores van como cadenas, como los da Firebase.
    /// </summary>
    public interface IRemoteConfigBackend
    {
        /// <summary>Los últimos valores descargados que guardó el dispositivo, o null si no hay ninguno.</summary>
        IReadOnlyDictionary<string, string>? Cached { get; }

        /// <summary>
        /// Pide valores nuevos sin bloquear (08 §2.3: <c>RC.FetchAsync(min_interval=12h)</c>); <paramref name="onDone"/> recibe
        /// lo descargado, o null sin red o con un error. Lo descargado pasa a la caché y se activa en el arranque siguiente.
        /// </summary>
        void Fetch(Action<IReadOnlyDictionary<string, string>?> onDone);
    }

    /// <summary>
    /// Remote Config del cliente (08 §2.2 <c>AF.RemoteConfig</c>; §9): <b>defaults compilados</b>
    /// (<see cref="RemoteConfigDefaults.Compiled"/>), caché, acceso tipado (<see cref="Get{T}"/>), congelación de valores al
    /// abrir cada pantalla (<see cref="Freeze"/>; 04 §10.2), <c>rc_variant</c> (<see cref="Variant"/>) y origen
    /// <c>Default/Cached/Remote</c>. Sin motor y sin red: el envío real (Firebase) entra después detrás de
    /// <see cref="IRemoteConfigBackend"/>.
    /// <para>
    /// Reglas: una clave que no está en 08 §9 no existe (<see cref="KeyNotFoundException"/>); un valor remoto que no es de su
    /// tipo se descarta y queda el anterior (y se anota en <see cref="Rejected"/>); un conjunto con un <c>rc_schema</c> mayor
    /// que el compilado se ignora entero (08 §9: «el cliente ignora esquemas mayores»). Las claves remotas desconocidas se
    /// ignoran. Cada activación crea un <see cref="RcSnapshot"/> nuevo: los congelados no cambian.
    /// </para>
    /// Solo desde el hilo principal.
    /// </summary>
    public sealed class RemoteConfigService
    {
        /// <summary>Intervalo mínimo entre descargas (08 §2.2; research/02 Complemento 4.2 [V]).</summary>
        public static readonly TimeSpan MinFetchInterval = TimeSpan.FromHours(12);

        /// <summary>Clave de la versión del esquema de claves.</summary>
        public const string SchemaKey = "rc_schema";

        /// <summary>Clave de la cohorte de experimento, que viaja en analítica como <c>rc_variant</c> (08 §9).</summary>
        public const string ExperimentKey = "biz_experiment_tag";

        private readonly IRemoteConfigBackend? _backend;
        private readonly List<string> _rejected = new List<string>();
        private RcSnapshot _current;
        private long _version;

        /// <summary>Crea el servicio con los defaults compilados (o <paramref name="defaults"/>) y, si hay, su proveedor.</summary>
        public RemoteConfigService(RemoteConfigDefaults? defaults = null, IRemoteConfigBackend? backend = null)
        {
            Defaults = defaults ?? RemoteConfigDefaults.Compiled;
            _backend = backend;
            _current = new RcSnapshot(Defaults, new Dictionary<string, KeyValuePair<RcValue, RcSource>>(StringComparer.Ordinal), 0);
        }

        /// <summary>Hay una activación nueva (para que la pantalla que se abra después lea los valores nuevos).</summary>
        public event Action<RcSnapshot>? Activated;

        /// <summary>Los defaults compilados.</summary>
        public RemoteConfigDefaults Defaults { get; }

        /// <summary>Los valores vigentes. Guardarlo es congelarlo (como <see cref="Freeze"/>).</summary>
        public RcSnapshot Current => _current;

        /// <summary>El origen del conjunto vigente (<c>af_boot.rc_source</c>).</summary>
        public RcSource Source => _current.Source;

        /// <summary><c>rc_variant</c> de la analítica (08 §8.1): el valor vigente de <c>biz_experiment_tag</c>.</summary>
        public string Variant => _current.Get<string>(ExperimentKey);

        /// <summary>El <c>rc_schema</c> de los defaults compilados.</summary>
        public long CompiledSchema => Defaults.Find(SchemaKey)?.Default?.Integer ?? 1;

        /// <summary>Claves remotas descartadas en la última activación (tipo incorrecto o esquema mayor), en orden.</summary>
        public IReadOnlyList<string> Rejected => _rejected;

        /// <summary>El último conjunto se ignoró entero por un <c>rc_schema</c> mayor que el compilado.</summary>
        public bool IgnoredNewerSchema { get; private set; }

        /// <summary>Valor vigente de <paramref name="key"/> (ver <see cref="RcSnapshot.Get{T}"/>).</summary>
        public T Get<T>(string key) => _current.Get<T>(key);

        /// <summary>Valor vigente de <paramref name="key"/>, sin excepción si falta o el tipo no sirve.</summary>
        public bool TryGet<T>(string key, out T value) => _current.TryGet(key, out value);

        /// <summary>Congela los valores vigentes al abrir una pantalla o un modal (04 §10.2).</summary>
        public RcSnapshot Freeze() => _current;

        /// <summary>Activa la caché del proveedor (08 §2.3: <c>RC.ActivateCached()</c>), si la hay. Devuelve si cambió algo.</summary>
        public bool ActivateCached()
        {
            IReadOnlyDictionary<string, string>? cached = _backend?.Cached;
            return cached != null && Activate(cached, RcSource.Cached);
        }

        /// <summary>
        /// Pide valores nuevos al proveedor si han pasado <see cref="MinFetchInterval"/> desde <paramref name="lastFetchUtc"/>
        /// (08 §2.3). No activa nada: lo descargado queda en la caché para el próximo arranque. Devuelve si lo pidió.
        /// </summary>
        public bool FetchIfDue(DateTime nowUtc, DateTime? lastFetchUtc, Action<bool>? onDone = null)
        {
            if (_backend == null || (lastFetchUtc.HasValue && nowUtc - lastFetchUtc.Value < MinFetchInterval))
            {
                return false;
            }

            _backend.Fetch(values => onDone?.Invoke(values != null));
            return true;
        }

        /// <summary>
        /// Activa <paramref name="values"/> (clave → valor en cadena, como Firebase) con origen <paramref name="source"/>,
        /// sobre los defaults: una clave que no viene vuelve a su default. Devuelve si cambió algo.
        /// </summary>
        public bool Activate(IReadOnlyDictionary<string, string> values, RcSource source = RcSource.Remote)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (source == RcSource.Default)
            {
                throw new ArgumentException("Los defaults no se activan: son la base.", nameof(source));
            }

            _rejected.Clear();
            IgnoredNewerSchema = false;
            if (values.TryGetValue(SchemaKey, out string? schemaText)
                && RcValue.TryFromRemote(RcType.Int, schemaText, out RcValue schema) && schema.Integer > CompiledSchema)
            {
                IgnoredNewerSchema = true;
                _rejected.Add(SchemaKey);
                return false;
            }

            var next = new Dictionary<string, KeyValuePair<RcValue, RcSource>>(StringComparer.Ordinal);
            var keys = new List<string>(values.Keys);
            keys.Sort(StringComparer.Ordinal); // orden estable de Rejected
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                RcParam? p = Defaults.Find(key);
                if (p == null)
                {
                    continue; // clave remota que este cliente no conoce: se ignora
                }

                if (!RcValue.TryFromRemote(p.Type, values[key], out RcValue v))
                {
                    _rejected.Add(key);
                    if (_current.SourceOf(key) != RcSource.Default)
                    {
                        next[key] = new KeyValuePair<RcValue, RcSource>(_current.Value(key), _current.SourceOf(key)); // se queda el anterior
                    }

                    continue;
                }

                if (p.Default.HasValue && p.Default.Value.Equals(v))
                {
                    continue; // igual que el default: no cuenta como remoto
                }

                next[key] = new KeyValuePair<RcValue, RcSource>(v, source);
            }

            _current = new RcSnapshot(Defaults, next, ++_version);
            Activated?.Invoke(_current);
            return true;
        }

        /// <summary>Vuelve a los defaults compilados (pruebas).</summary>
        public void Reset()
        {
            _rejected.Clear();
            IgnoredNewerSchema = false;
            _current = new RcSnapshot(Defaults, new Dictionary<string, KeyValuePair<RcValue, RcSource>>(StringComparer.Ordinal), ++_version);
            Activated?.Invoke(_current);
        }
    }

    /// <summary>
    /// Proveedor falso (08 §2.1, principio 6): defaults y sobrescrituras para los tests y el editor, sin red. Lo que se pone
    /// con <see cref="Set"/> es lo que devuelve <see cref="Fetch"/> y, tras <see cref="StoreFetched"/>, la caché.
    /// </summary>
    public sealed class FakeRemoteConfigBackend : IRemoteConfigBackend
    {
        private readonly Dictionary<string, string> _remote = new Dictionary<string, string>(StringComparer.Ordinal);
        private Dictionary<string, string>? _cached;

        /// <summary>Simula estar sin red: <see cref="Fetch"/> devuelve null.</summary>
        public bool Offline { get; set; }

        /// <summary>Descargas pedidas.</summary>
        public int Fetches { get; private set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string>? Cached => _cached;

        /// <summary>Pone el valor remoto de <paramref name="key"/> (en cadena, como Firebase).</summary>
        public FakeRemoteConfigBackend Set(string key, string value)
        {
            _remote[key ?? throw new ArgumentNullException(nameof(key))] = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        /// <summary>Copia los valores remotos en la caché, como tras una descarga en una sesión anterior.</summary>
        public FakeRemoteConfigBackend StoreFetched()
        {
            _cached = new Dictionary<string, string>(_remote, StringComparer.Ordinal);
            return this;
        }

        /// <summary>Los valores remotos puestos (para <see cref="RemoteConfigService.Activate"/>).</summary>
        public IReadOnlyDictionary<string, string> Remote => _remote;

        /// <inheritdoc />
        public void Fetch(Action<IReadOnlyDictionary<string, string>?> onDone)
        {
            Fetches++;
            if (Offline)
            {
                onDone?.Invoke(null);
                return;
            }

            StoreFetched();
            onDone?.Invoke(_cached);
        }
    }
}
