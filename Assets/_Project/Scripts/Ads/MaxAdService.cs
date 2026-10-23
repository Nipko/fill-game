#nullable enable
using System;
using AF.Platform;

namespace AF.Ads
{
    /// <summary>Formato de una unidad de MAX.</summary>
    public enum MaxAdKind
    {
        /// <summary>La unidad de recompensados (una para todas las ubicaciones <c>rv_*</c>).</summary>
        Rewarded = 0,

        /// <summary>La unidad de intersticiales (<c>interstitial_post_level</c>).</summary>
        Interstitial = 1,
    }

    /// <summary>Lo que avisa MAX de una unidad (sus callbacks, ya en el hilo principal).</summary>
    public enum MaxAdEvent
    {
        /// <summary><c>OnAdLoadedEvent</c>.</summary>
        Loaded = 0,

        /// <summary><c>OnAdLoadFailedEvent</c>.</summary>
        LoadFailed = 1,

        /// <summary><c>OnAdDisplayFailedEvent</c>.</summary>
        DisplayFailed = 2,

        /// <summary><c>OnAdReceivedRewardEvent</c> (solo recompensados).</summary>
        RewardReceived = 3,

        /// <summary><c>OnAdHiddenEvent</c>: el anuncio se cerró.</summary>
        Hidden = 4,
    }

    /// <summary>Quien recibe los callbacks de <see cref="IMaxSdkPort"/>.</summary>
    public interface IMaxSdkListener
    {
        /// <summary>El SDK terminó de iniciarse (<c>OnSdkInitializedEvent</c>).</summary>
        void OnInitialized();

        /// <summary>Un callback de una unidad.</summary>
        void OnAdEvent(MaxAdKind kind, MaxAdEvent ev);

        /// <summary>Ingreso de una impresión (<c>OnAdRevenuePaidEvent</c>): la red y el ingreso en USD.</summary>
        void OnRevenuePaid(MaxAdKind kind, string networkName, double revenue);
    }

    /// <summary>
    /// El SDK de AppLovin MAX visto desde el juego (D-042): la parte con motor (<c>AF.Ads.Max.NativeMaxSdkPort</c>, solo en un
    /// teléfono Android) llama a <c>MaxSdk</c>; el editor y los tests usan <see cref="FakeMaxSdkPort"/>. Las unidades de anuncio las
    /// conoce el puerto (salen de la configuración del build, nunca del código). Hilo principal.
    /// </summary>
    public interface IMaxSdkPort
    {
        /// <summary>Quien recibe los callbacks.</summary>
        IMaxSdkListener? Listener { get; set; }

        /// <summary><c>MaxSdk.SetHasUserConsent</c>.</summary>
        void SetHasUserConsent(bool value);

        /// <summary><c>MaxSdk.SetDoNotSell</c>.</summary>
        void SetDoNotSell(bool value);

        /// <summary><c>MaxSdk.SetUserId</c>: el UID de Firebase Auth, para el SSV (08 §10.2).</summary>
        void SetUserId(string userId);

        /// <summary><c>MaxSdk.InitializeSdk</c> (con la automatización de UMP, MAX enseña el mensaje en el EEE, el Reino Unido y Suiza).</summary>
        void Initialize();

        /// <summary>Carga un anuncio de la unidad <paramref name="kind"/>.</summary>
        void Load(MaxAdKind kind);

        /// <summary>Hay un anuncio cargado en la unidad <paramref name="kind"/>.</summary>
        bool IsReady(MaxAdKind kind);

        /// <summary>Muestra el anuncio de <paramref name="kind"/> con la ubicación de MAX <paramref name="placement"/> y, en los recompensados, <paramref name="customData"/>.</summary>
        void Show(MaxAdKind kind, string placement, string? customData);

        /// <summary>Vuelve a abrir el mensaje de UMP (<c>MaxSdk.CmpService.ShowCmpForExistingUser</c>). False si no hay CMP.</summary>
        bool ShowCmpForExistingUser();
    }

    /// <summary>
    /// AppLovin MAX detrás de <see cref="IAdService"/> (D-042; 08 §2.2 <c>AF.Ads</c>, §2.3, §7 #1-#2 y §10.2), sin motor y probado con
    /// <see cref="FakeMaxSdkPort"/>:
    /// <list type="bullet">
    /// <item><b>Consentimiento antes que nada:</b> no toca el SDK hasta <see cref="ApplyConsent"/> con una decisión que permita iniciar;
    /// fija <c>SetHasUserConsent</c> y <c>SetDoNotSell</c> <b>antes</b> de <c>InitializeSdk</c>; con &lt;13 nunca lo inicia, y si la edad
    /// pasa a &lt;13 después, deja de cargar y de mostrar.</item>
    /// <item><b>Precarga según el nivel:</b> tras iniciarse carga el recompensado; el intersticial, solo cuando el siguiente nivel está a uno
    /// de <c>ads_first_interstitial_level</c> (22) o más allá y los interruptores <c>biz_*</c> lo permiten. Tras cada anuncio se carga
    /// el siguiente; un fallo de carga se reintenta con espera exponencial (2, 4 … 64 s), cuando el juego vuelve a preguntar.</item>
    /// <item><b>SSV:</b> antes de cada recompensado, <c>SetUserId(UID)</c> (si hay UID de Firebase Auth) y
    /// <c>custom_data = placement|nonce</c> (<see cref="AdRewardCustomData"/>); el nonce queda en <see cref="LastRewardNonce"/>.
    /// <see cref="AdOutcome.Rewarded"/> sale con <c>OnAdReceivedRewardEvent</c> al cerrarse el anuncio: es la concesión provisional de
    /// 08 §10.2; la confirma o la revoca el servidor después (<c>restoreLedger</c>).</item>
    /// <item><b>ILR:</b> cada <c>OnAdRevenuePaidEvent</c> sale por <see cref="Impression"/> (<c>ad_impression</c>).</item>
    /// </list>
    /// Las reglas de cadencia y topes siguen en <see cref="AdPolicy"/>. Hilo principal.
    /// </summary>
    public sealed class MaxAdService : IAdService, IAdConsentTarget, IAdImpressionSource, IMaxSdkListener
    {
        /// <summary>Primera espera tras un fallo de carga.</summary>
        public static readonly TimeSpan FirstRetry = TimeSpan.FromSeconds(2);

        /// <summary>Espera máxima entre reintentos de carga.</summary>
        public static readonly TimeSpan MaxRetry = TimeSpan.FromSeconds(64);

        private readonly IMaxSdkPort _port;
        private readonly IClock _clock;
        private readonly Func<string?> _uid;
        private readonly Func<int> _nextLevel;
        private readonly Func<AdsConfig> _config;
        private readonly Func<string> _newNonce;
        private readonly UnitState _rewarded = new UnitState();
        private readonly UnitState _interstitial = new UnitState();

        private bool _initRequested;
        private bool _allowed;
        private string? _userId;
        private Action<AdOutcome>? _pending;
        private MaxAdKind _pendingKind;
        private bool _rewardReceived;

        /// <summary>Crea el servicio sobre <paramref name="port"/>, sin tocarlo.</summary>
        /// <param name="port">El SDK (real o falso).</param>
        /// <param name="clock">Reloj (esperas de reintento).</param>
        /// <param name="uid">El UID de Firebase Auth, o null si aún no hay (sin él, MAX no manda <c>user_id</c> y el SSV no se confirma).</param>
        /// <param name="nextLevel">El siguiente nivel de la campaña (para precargar el intersticial).</param>
        /// <param name="config">Las cifras de los anuncios vigentes (Remote Config o los defaults).</param>
        /// <param name="newNonce">Generador de nonces (por defecto, <see cref="AdRewardCustomData.NewNonce"/>).</param>
        public MaxAdService(
            IMaxSdkPort port, IClock clock, Func<string?> uid, Func<int> nextLevel, Func<AdsConfig>? config = null, Func<string>? newNonce = null)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _nextLevel = nextLevel ?? throw new ArgumentNullException(nameof(nextLevel));
            _config = config ?? (() => AdsConfig.Defaults);
            _newNonce = newNonce ?? AdRewardCustomData.NewNonce;
        }

        /// <inheritdoc />
        public event Action<AdImpression>? Impression;

        /// <summary>Se pidió <c>InitializeSdk</c> (una sola vez por proceso).</summary>
        public bool InitRequested => _initRequested;

        /// <summary>MAX avisó de que está listo.</summary>
        public bool Initialized { get; private set; }

        /// <summary>Los últimos flags aplicados (null antes de la primera decisión).</summary>
        public AdConsentSettings? Consent { get; private set; }

        /// <summary>El nonce del último recompensado pedido (null si aún no hubo).</summary>
        public string? LastRewardNonce { get; private set; }

        /// <summary>La ubicación del último recompensado pedido.</summary>
        public AdPlacement? LastRewardPlacement { get; private set; }

        /// <inheritdoc />
        public bool IsShowing => _pending != null;

        /// <inheritdoc />
        public void ApplyConsent(AdConsentSettings settings)
        {
            Consent = settings ?? throw new ArgumentNullException(nameof(settings));
            _allowed = settings.MayInitialize;
            if (!settings.MayInitialize)
            {
                if (_initRequested && settings.ChildDirected)
                {
                    // Edad corregida a <13 con MAX ya iniciado (no se puede apagar): sin personalización y sin anuncios desde ahora.
                    _port.SetHasUserConsent(false);
                    _port.SetDoNotSell(true);
                }

                return;
            }

            // Antes de InitializeSdk (MAX: los flags de privacidad se fijan antes de iniciar). Con UMP, no se tocan.
            if (settings.HasUserConsent.HasValue)
            {
                _port.SetHasUserConsent(settings.HasUserConsent.Value);
            }

            if (settings.DoNotSell.HasValue)
            {
                _port.SetDoNotSell(settings.DoNotSell.Value);
            }

            if (!_initRequested)
            {
                _initRequested = true;
                _port.Listener = this;
                _port.Initialize();
            }
            else if (Initialized)
            {
                EnsureLoads();
            }
        }

        /// <inheritdoc />
        public bool ShowPrivacyOptions()
        {
            return Initialized && _port.ShowCmpForExistingUser();
        }

        /// <inheritdoc />
        public bool IsReady(AdPlacement placement)
        {
            placement.ToId(); // valida la ubicación
            if (!Initialized || !_allowed)
            {
                return false;
            }

            EnsureLoads();
            return _pending == null && _port.IsReady(KindOf(placement));
        }

        /// <inheritdoc />
        public void Show(AdPlacement placement, Action<AdOutcome> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            MaxAdKind kind = KindOf(placement);
            if (_pending != null)
            {
                onDone(AdOutcome.Failed);
                return;
            }

            if (!Initialized || !_allowed || !_port.IsReady(kind))
            {
                if (Initialized && _allowed)
                {
                    EnsureLoads();
                }

                onDone(AdOutcome.NoFill);
                return;
            }

            string? customData = null;
            if (kind == MaxAdKind.Rewarded)
            {
                string? uid = _uid();
                if (!string.IsNullOrEmpty(uid) && !string.Equals(uid, _userId, StringComparison.Ordinal))
                {
                    _userId = uid;
                    _port.SetUserId(uid!);
                }

                string nonce = _newNonce();
                customData = AdRewardCustomData.Format(placement, nonce);
                LastRewardNonce = nonce;
                LastRewardPlacement = placement;
            }

            _pending = onDone;
            _pendingKind = kind;
            _rewardReceived = false;
            Unit(kind).Loaded = false;
            _port.Show(kind, placement.ToId(), customData);
        }

        /// <inheritdoc />
        void IMaxSdkListener.OnInitialized()
        {
            Initialized = true;
            if (_allowed)
            {
                EnsureLoads();
            }
        }

        /// <inheritdoc />
        void IMaxSdkListener.OnAdEvent(MaxAdKind kind, MaxAdEvent ev)
        {
            UnitState unit = Unit(kind);
            switch (ev)
            {
                case MaxAdEvent.Loaded:
                    unit.Loading = false;
                    unit.Loaded = true;
                    unit.Failures = 0;
                    break;

                case MaxAdEvent.LoadFailed:
                    unit.Loading = false;
                    unit.Loaded = false;
                    unit.Failures++;
                    unit.RetryAt = _clock.UtcNow + Backoff(unit.Failures);
                    break;

                case MaxAdEvent.RewardReceived:
                    if (_pending != null && _pendingKind == kind)
                    {
                        _rewardReceived = true;
                    }

                    break;

                case MaxAdEvent.DisplayFailed:
                    Finish(kind, AdOutcome.Failed);
                    break;

                case MaxAdEvent.Hidden:
                    Finish(kind, kind == MaxAdKind.Interstitial ? AdOutcome.Shown : _rewardReceived ? AdOutcome.Rewarded : AdOutcome.Skipped);
                    break;
            }
        }

        /// <inheritdoc />
        void IMaxSdkListener.OnRevenuePaid(MaxAdKind kind, string networkName, double revenue)
        {
            AdPlacement placement = kind == MaxAdKind.Interstitial
                ? AdPlacement.InterstitialPostLevel
                : LastRewardPlacement ?? AdPlacement.DoubleHarvest;
            Impression?.Invoke(new AdImpression(networkName, placement, revenue));
        }

        /// <summary>Espera tras <paramref name="failures"/> fallos seguidos: 2, 4, 8 … hasta 64 s.</summary>
        public static TimeSpan Backoff(int failures)
        {
            int exponent = Math.Max(0, Math.Min(failures - 1, 5));
            double seconds = FirstRetry.TotalSeconds * (1 << exponent);
            return TimeSpan.FromSeconds(Math.Min(seconds, MaxRetry.TotalSeconds));
        }

        /// <summary>Pide las cargas que tocan (lo llaman <see cref="IsReady"/>, el inicio y el cierre de cada anuncio).</summary>
        private void EnsureLoads()
        {
            AdsConfig config = _config();
            if (!config.BizAdsEnabled)
            {
                return;
            }

            if (config.BizRewardedEnabled)
            {
                TryLoad(MaxAdKind.Rewarded);
            }

            if (config.BizInterstitialsEnabled && _nextLevel() >= config.AdsFirstInterstitialLevel - 1)
            {
                TryLoad(MaxAdKind.Interstitial);
            }
        }

        private void TryLoad(MaxAdKind kind)
        {
            UnitState unit = Unit(kind);
            if (unit.Loading || (_pending != null && _pendingKind == kind))
            {
                return;
            }

            if (unit.Loaded && _port.IsReady(kind))
            {
                return;
            }

            if (unit.Failures > 0 && _clock.UtcNow < unit.RetryAt)
            {
                return;
            }

            unit.Loading = true;
            unit.Loaded = false;
            _port.Load(kind);
        }

        private void Finish(MaxAdKind kind, AdOutcome outcome)
        {
            if (_pending == null || _pendingKind != kind)
            {
                return;
            }

            Action<AdOutcome> done = _pending;
            _pending = null;
            _rewardReceived = false;
            if (_allowed)
            {
                EnsureLoads(); // el siguiente, ya
            }

            done(outcome);
        }

        private UnitState Unit(MaxAdKind kind) => kind == MaxAdKind.Rewarded ? _rewarded : _interstitial;

        private static MaxAdKind KindOf(AdPlacement placement) => placement.IsRewarded() ? MaxAdKind.Rewarded : MaxAdKind.Interstitial;

        private sealed class UnitState
        {
            public bool Loading;
            public bool Loaded;
            public int Failures;
            public DateTime RetryAt;
        }
    }
}
