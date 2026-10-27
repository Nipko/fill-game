#nullable enable
using System;
using AF.Platform;

namespace AF.Ads
{
    /// <summary>Formato de un bloque de anuncios de Google Mobile Ads.</summary>
    public enum GmaAdKind
    {
        /// <summary>El bloque de recompensados (uno para todas las ubicaciones <c>rv_*</c>).</summary>
        Rewarded = 0,

        /// <summary>El bloque de intersticiales (<c>interstitial_post_level</c>).</summary>
        Interstitial = 1,
    }

    /// <summary>Lo que avisa Google Mobile Ads de un bloque (ya en el hilo principal).</summary>
    public enum GmaAdEvent
    {
        /// <summary>La carga terminó con un anuncio.</summary>
        Loaded = 0,

        /// <summary>La carga falló (<c>LoadAdError</c>).</summary>
        LoadFailed = 1,

        /// <summary><c>OnAdFullScreenContentFailed</c>: no se pudo mostrar.</summary>
        DisplayFailed = 2,

        /// <summary>La recompensa del recompensado (el <c>Action&lt;Reward&gt;</c> de <c>RewardedAd.Show</c>).</summary>
        RewardReceived = 3,

        /// <summary><c>OnAdFullScreenContentClosed</c>: el anuncio se cerró.</summary>
        Closed = 4,
    }

    /// <summary>Quien recibe los avisos de <see cref="IGmaPort"/> (siempre en el hilo principal).</summary>
    public interface IGmaListener
    {
        /// <summary>
        /// UMP terminó (<c>ConsentInformation.Update</c> y, si hacía falta, <c>ConsentForm.LoadAndShowConsentFormIfRequired</c>, con
        /// o sin error): <paramref name="canRequestAds"/> es <c>ConsentInformation.CanRequestAds()</c>.
        /// </summary>
        void OnConsentResolved(bool canRequestAds);

        /// <summary><c>MobileAds.Initialize</c> terminó.</summary>
        void OnInitialized();

        /// <summary>Un aviso de un bloque.</summary>
        void OnAdEvent(GmaAdKind kind, GmaAdEvent ev);

        /// <summary><c>OnAdPaid</c>: la red (<c>AdSourceName</c>), el valor en micros y la moneda.</summary>
        void OnPaid(GmaAdKind kind, string adSource, long valueMicros, string currency);
    }

    /// <summary>
    /// Google Mobile Ads 11.5.0 (con UMP 4.0.0) visto desde el juego (D-049): la parte con motor (<c>AF.Ads.Google.NativeGmaPort</c>,
    /// solo en un teléfono Android) llama al plugin; el editor y los tests usan <see cref="FakeGmaPort"/>. Los bloques de anuncios
    /// los conoce el puerto (<see cref="GoogleAdUnits"/>). Los avisos del SDK llegan en cualquier hilo y el puerto los entrega en
    /// <see cref="Pump"/>, en el hilo principal.
    /// </summary>
    public interface IGmaPort
    {
        /// <summary>Quien recibe los avisos.</summary>
        IGmaListener? Listener { get; set; }

        /// <summary>
        /// <c>MobileAds.SetRequestConfiguration</c>: contenido máximo G siempre (D-029) y <c>TagForUnderAgeOfConsent</c> con 13-17. No
        /// inicia el SDK.
        /// </summary>
        void SetRequestConfiguration(bool underAgeOfConsent);

        /// <summary>
        /// UMP: <c>ConsentInformation.Update</c> (con <c>TagForUnderAgeOfConsent</c> = <paramref name="underAgeOfConsent"/>) y
        /// después <c>ConsentForm.LoadAndShowConsentFormIfRequired</c>; al final, <see cref="IGmaListener.OnConsentResolved"/>.
        /// </summary>
        void RequestConsent(bool underAgeOfConsent);

        /// <summary>
        /// El consentimiento de nuestra pantalla para las redes de mediación que no leen el de Google (D-049, añadido del 2026-10-27:
        /// Unity Ads). En Unity Ads es su API <c>MetaData</c>: <c>gdpr.consent</c> = <paramref name="hasUserConsent"/> y
        /// <c>privacy.consent</c> = no <paramref name="doNotSell"/> (la guía de mediación de Google: CCPA y leyes de los estados de
        /// EE. UU.). Un null no manda esa clave (perfil <c>ump</c> de 18+: deciden las cadenas TCF de UMP). No inicia nada; va antes de
        /// <see cref="Initialize"/>, que es quien inicia las redes de mediación.
        /// </summary>
        void SetMediationConsent(bool? hasUserConsent, bool? doNotSell);

        /// <summary>UMP pide un punto de entrada a las opciones de privacidad (<c>PrivacyOptionsRequirementStatus.Required</c>).</summary>
        bool PrivacyOptionsRequired { get; }

        /// <summary><c>ConsentForm.ShowPrivacyOptionsForm</c>. False si no se pudo pedir.</summary>
        bool ShowPrivacyOptionsForm();

        /// <summary><c>MobileAds.Initialize</c>.</summary>
        void Initialize();

        /// <summary>
        /// Carga un anuncio del bloque <paramref name="kind"/> (con <c>npa=1</c> si <paramref name="nonPersonalized"/> y con
        /// <c>rdp=1</c> si <paramref name="restrictedDataProcessing"/>: 13-17 en EE. UU., T-L5).
        /// </summary>
        void Load(GmaAdKind kind, bool nonPersonalized, bool restrictedDataProcessing);

        /// <summary>Hay un anuncio cargado que se puede mostrar (<c>CanShowAd</c>).</summary>
        bool IsReady(GmaAdKind kind);

        /// <summary>
        /// Muestra el anuncio de <paramref name="kind"/>. En los recompensados con <paramref name="customData"/>, antes fija
        /// <c>ServerSideVerificationOptions</c> con <paramref name="userId"/> y <paramref name="customData"/> (08 §10.2; D-049). Sin
        /// <paramref name="customData"/> (null: sin UID, sin backend) no fija nada.
        /// </summary>
        void Show(GmaAdKind kind, string? userId, string? customData);

        /// <summary>Entrega en el hilo principal los avisos pendientes del SDK (cada fotograma; sin asignaciones si no hay).</summary>
        void Pump();
    }

    /// <summary>
    /// Los bloques de anuncios de Google de este build (D-049; ninguno es secreto): en un build de desarrollo o con el
    /// <c>applicationId</c> provisional <c>com.amazonfill.dev</c> (también el ensayo de release), siempre los <b>bloques de prueba
    /// oficiales de Google</b> (https://developers.google.com/admob/android/test-ads), para no generar tráfico inválido; solo un
    /// release con el ID definitivo usa los de producción. 08 §9 no tiene claves de Remote Config para los bloques, así que van aquí.
    /// </summary>
    public sealed class GoogleAdUnits
    {
        /// <summary>App ID de AdMob para Android («Tintalú»; <c>GoogleMobileAdsSettings</c>, lo fija <c>GoogleAdsProjectSetup</c>).</summary>
        public const string AndroidAppId = "ca-app-pub-2916158449453694~9925643748";

        /// <summary>Bloque recompensado de producción («Tintalu Rewarded», formato Recompensado; el antiguo <c>2877105387</c> es un
        /// intersticial recompensado y no se carga como <c>RewardedAd</c>: no usar; <c>docs/dev/anuncios-admob.md</c> §3 bis).</summary>
        public const string ProductionRewarded = "ca-app-pub-2916158449453694/1182085615";

        /// <summary>Bloque intersticial de producción.</summary>
        public const string ProductionInterstitial = "ca-app-pub-2916158449453694/7187673451";

        /// <summary>Bloque recompensado de prueba de Google (Android).</summary>
        public const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";

        /// <summary>Bloque intersticial de prueba de Google (Android).</summary>
        public const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";

        /// <summary>El <c>applicationId</c> provisional de los builds de prueba (regla dura 6).</summary>
        public const string ProvisionalApplicationId = "com.amazonfill.dev";

        private GoogleAdUnits(string rewarded, string interstitial, bool production)
        {
            Rewarded = rewarded;
            Interstitial = interstitial;
            Production = production;
        }

        /// <summary>El bloque de recompensados.</summary>
        public string Rewarded { get; }

        /// <summary>El bloque de intersticiales.</summary>
        public string Interstitial { get; }

        /// <summary>Son los bloques de producción.</summary>
        public bool Production { get; }

        /// <summary>El bloque de <paramref name="kind"/>.</summary>
        public string For(GmaAdKind kind) => kind == GmaAdKind.Rewarded ? Rewarded : Interstitial;

        /// <summary>
        /// Producción solo con un build que no es de desarrollo y con un <paramref name="applicationId"/> distinto del provisional;
        /// en cualquier otro caso (o sin ID), los de prueba.
        /// </summary>
        public static GoogleAdUnits Choose(bool developmentBuild, string? applicationId)
        {
            bool production = !developmentBuild
                && !string.IsNullOrEmpty(applicationId)
                && !string.Equals(applicationId, ProvisionalApplicationId, StringComparison.Ordinal);
            return production
                ? new GoogleAdUnits(ProductionRewarded, ProductionInterstitial, true)
                : new GoogleAdUnits(TestRewarded, TestInterstitial, false);
        }
    }

    /// <summary>
    /// AdMob directo con Google Mobile Ads detrás de <see cref="IAdService"/> (D-049; 08 §2.2 <c>AF.Ads</c>, §2.3, §7 #2 y §10.2),
    /// sin motor y probado con <see cref="FakeGmaPort"/>:
    /// <list type="bullet">
    /// <item><b>Orden de arranque (08 §2.3):</b> no toca el SDK hasta <see cref="ApplyConsent"/> con una decisión que permita iniciar
    /// (nunca con &lt;13). Entonces: <c>SetRequestConfiguration</c> (G y, con 13-17, <c>TagForUnderAgeOfConsent</c>), UMP
    /// (<c>ConsentInformation.Update</c> y el formulario si hace falta: EEE, Reino Unido, Suiza y estados de EE. UU.) y, solo cuando
    /// UMP dice <c>CanRequestAds</c>, <c>MobileAds.Initialize</c>. Si la edad pasa a &lt;13 después, deja de cargar y de mostrar.</item>
    /// <item><b>No personalizados:</b> con <c>npa</c> de nuestra pantalla (13-17, sin consentimiento de anuncios personalizados o sin
    /// país), cada petición lleva <c>npa=1</c>. Con el perfil <c>ump</c> de 18+ deciden las cadenas TCF de UMP.</item>
    /// <item><b>Redes de mediación (Unity Ads; D-049, añadido del 2026-10-27):</b> antes de UMP y de <c>MobileAds.Initialize</c>, y
    /// otra vez si cambia, <see cref="IGmaPort.SetMediationConsent"/> con los flags de <see cref="AdConsentSettings"/> (los que antes recibía MAX): con <c>npa</c>
    /// (13-17, también 13-17 en EE. UU., o sin consentimiento) <c>gdpr.consent</c> y <c>privacy.consent</c> en false.</item>
    /// <item><b>RDP (T-L5):</b> con 13-17 y el país legal <c>US</c>, cada petición lleva además <c>rdp=1</c> [F28].</item>
    /// <item><b>Precarga según el nivel</b> y reintentos con espera exponencial (<see cref="Backoff"/>: 2, 4 … 64 s).</item>
    /// <item><b>SSV (08 §10.2; D-049):</b> antes de cada recompensado, si hay UID de Firebase Auth, <c>ServerSideVerificationOptions</c>
    /// con <c>user_id</c> = ese UID (el mismo que valida el JWT de <c>restoreLedger</c>) y <c>custom_data = placement|nonce</c>
    /// (<see cref="AdRewardCustomData"/>); AdMob los devuelve firmados a <c>/v1/ssv</c>, que anota el nonce para ese UID. Sin UID
    /// (sin backend) no se fija nada ni hay nonce: la recompensa sigue como hoy, solo provisional. <see cref="AdOutcome.Rewarded"/>
    /// es la concesión provisional; la confirma el servidor (<c>restoreLedger</c>).</item>
    /// <item><b>Ingresos:</b> cada <c>OnAdPaid</c> sale por <see cref="Impression"/> (<c>ad_impression</c> con <c>ad_platform</c> = AdMob).</item>
    /// </list>
    /// Las reglas de cadencia y topes siguen en <see cref="AdPolicy"/>. Hilo principal.
    /// </summary>
    public sealed class GoogleAdService : IAdService, IAdConsentTarget, IAdImpressionSource, IGmaListener, IAdTicker
    {
        /// <summary>Margen para una recompensa que llega después del cierre [P] (el orden de los avisos no está garantizado).</summary>
        public static readonly TimeSpan RewardGrace = TimeSpan.FromSeconds(1.5);

        /// <summary>Margen tras volver de segundo plano con un anuncio aún sin cerrar [P].</summary>
        public static readonly TimeSpan ResumeGrace = TimeSpan.FromSeconds(3);

        /// <summary>Primera espera tras un fallo de carga.</summary>
        public static readonly TimeSpan FirstRetry = TimeSpan.FromSeconds(2);

        /// <summary>Espera máxima entre reintentos de carga.</summary>
        public static readonly TimeSpan MaxRetry = TimeSpan.FromSeconds(64);

        private readonly IGmaPort _port;
        private readonly IClock _clock;
        private readonly Func<string?> _uid;
        private readonly Func<int> _nextLevel;
        private readonly Func<AdsConfig> _config;
        private readonly Func<string> _newNonce;
        private readonly UnitState _rewarded = new UnitState();
        private readonly UnitState _interstitial = new UnitState();

        private bool _allowed;
        private bool _nonPersonalized = true;
        private bool _restricted;
        private bool? _underAgeApplied;
        private bool _mediationApplied;
        private bool? _mediationConsent;
        private bool? _mediationDoNotSell;
        private bool _consentInFlight;
        private bool _canRequestAds;
        private Action<AdOutcome>? _pending;
        private GmaAdKind _pendingKind;
        private bool _rewardReceived;
        private DateTime? _closedAt;
        private DateTime? _resumedAt;

        /// <summary>Crea el servicio sobre <paramref name="port"/>, sin tocarlo.</summary>
        /// <param name="port">El SDK (real o falso).</param>
        /// <param name="clock">Reloj (esperas de reintento y márgenes).</param>
        /// <param name="uid">El UID de Firebase Auth, o null si aún no hay (sin él no va <c>user_id</c> y el SSV no se confirma).</param>
        /// <param name="nextLevel">El siguiente nivel de la campaña (para precargar el intersticial).</param>
        /// <param name="config">Las cifras de los anuncios vigentes (Remote Config o los defaults).</param>
        /// <param name="newNonce">Generador de nonces (por defecto, <see cref="AdRewardCustomData.NewNonce"/>).</param>
        public GoogleAdService(
            IGmaPort port, IClock clock, Func<string?> uid, Func<int> nextLevel, Func<AdsConfig>? config = null, Func<string>? newNonce = null)
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

        /// <summary>Se pidió UMP al menos una vez.</summary>
        public bool ConsentRequested { get; private set; }

        /// <summary>Se pidió <c>MobileAds.Initialize</c> (una sola vez por proceso).</summary>
        public bool InitRequested { get; private set; }

        /// <summary>Google avisó de que está listo.</summary>
        public bool Initialized { get; private set; }

        /// <summary>Los últimos ajustes aplicados (null antes de la primera decisión).</summary>
        public AdConsentSettings? Consent { get; private set; }

        /// <summary>El nonce del último recompensado pedido con SSV (null si aún no hubo o si se mostró sin UID, sin SSV).</summary>
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
                return; // sin decisión, <13 o 13-17 con UMP (Q-MAX-6 (b), D-055 punto 4): ni una llamada más al SDK; si ya estaba iniciado, nada se carga ni se muestra
            }

            if (settings.NonPersonalized && !_nonPersonalized)
            {
                // Pasa a npa (p. ej. retira el consentimiento en Ajustes o corrige la edad a 13-17): lo ya cargado se pidió
                // personalizado y no se muestra; se vuelve a cargar con npa=1.
                Discard(_rewarded);
                Discard(_interstitial);
            }

            _nonPersonalized = settings.NonPersonalized;
            if (!_mediationApplied || _mediationConsent != settings.HasUserConsent || _mediationDoNotSell != settings.DoNotSell)
            {
                // Antes de SetRequestConfiguration, UMP e Initialize: el adaptador de Unity Ads lo lee al iniciarse (D-049, añadido).
                _mediationApplied = true;
                _mediationConsent = settings.HasUserConsent;
                _mediationDoNotSell = settings.DoNotSell;
                _port.SetMediationConsent(settings.HasUserConsent, settings.DoNotSell);
            }

            if (settings.RestrictedDataProcessing && !_restricted)
            {
                // Pasa a rdp=1 (T-L5; p. ej. la señal de edad llega tarde con 13-17): lo ya cargado se pidió sin él y no se muestra.
                Discard(_rewarded);
                Discard(_interstitial);
            }

            _restricted = settings.RestrictedDataProcessing;

            if (_underAgeApplied != settings.Teen)
            {
                _underAgeApplied = settings.Teen;
                _port.Listener = this;
                _port.SetRequestConfiguration(settings.Teen); // antes de UMP y de Initialize (Google: los flags van antes)
            }

            if (!ConsentRequested)
            {
                RequestConsent();
            }
            else if (Initialized)
            {
                EnsureLoads();
            }
            else if (_canRequestAds)
            {
                StartSdk();
            }
        }

        /// <inheritdoc />
        public bool ShowPrivacyOptions()
        {
            return _allowed && ConsentRequested && !_consentInFlight && _port.PrivacyOptionsRequired && _port.ShowPrivacyOptionsForm();
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
            GmaAdKind kind = KindOf(placement);
            return _pending == null && Unit(kind).Loaded && _port.IsReady(kind);
        }

        /// <inheritdoc />
        public void Show(AdPlacement placement, Action<AdOutcome> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            GmaAdKind kind = KindOf(placement);
            if (_pending != null)
            {
                onDone(AdOutcome.Failed);
                return;
            }

            if (!Initialized || !_allowed || !Unit(kind).Loaded || !_port.IsReady(kind))
            {
                if (Initialized && _allowed)
                {
                    EnsureLoads();
                }

                onDone(AdOutcome.NoFill);
                return;
            }

            string? userId = null;
            string? customData = null;
            if (kind == GmaAdKind.Rewarded)
            {
                string? uid = _uid();
                LastRewardNonce = null;
                if (!string.IsNullOrEmpty(uid))
                {
                    // Solo con UID: el servidor ata el nonce a ese UID; sin él, el callback no casaría con restoreLedger.
                    string nonce = _newNonce();
                    userId = uid;
                    customData = AdRewardCustomData.Format(placement, nonce);
                    LastRewardNonce = nonce;
                }

                LastRewardPlacement = placement;
            }

            _pending = onDone;
            _pendingKind = kind;
            _rewardReceived = false;
            _closedAt = null;
            _resumedAt = null;
            Unit(kind).Loaded = false;
            _port.Show(kind, userId, customData);
        }

        /// <inheritdoc />
        void IGmaListener.OnConsentResolved(bool canRequestAds)
        {
            _consentInFlight = false;
            _canRequestAds = canRequestAds;
            if (canRequestAds && _allowed && !InitRequested)
            {
                StartSdk();
            }
        }

        /// <inheritdoc />
        void IGmaListener.OnInitialized()
        {
            Initialized = true;
            if (_allowed)
            {
                EnsureLoads();
            }
        }

        /// <inheritdoc />
        void IGmaListener.OnAdEvent(GmaAdKind kind, GmaAdEvent ev)
        {
            UnitState unit = Unit(kind);
            switch (ev)
            {
                case GmaAdEvent.Loaded:
                    unit.Loading = false;
                    unit.Loaded = !unit.Stale; // pedido antes de pasar a npa: no vale, se vuelve a cargar
                    unit.Stale = false;
                    unit.Failures = 0;
                    break;

                case GmaAdEvent.LoadFailed:
                    unit.Loading = false;
                    unit.Loaded = false;
                    unit.Stale = false;
                    unit.Failures++;
                    unit.RetryAt = _clock.UtcNow + Backoff(unit.Failures);
                    break;

                case GmaAdEvent.RewardReceived:
                    if (_pending != null && _pendingKind == kind)
                    {
                        _rewardReceived = true;
                        if (_closedAt.HasValue)
                        {
                            Finish(kind, AdOutcome.Rewarded); // llegó tarde, dentro del margen
                        }
                    }

                    break;

                case GmaAdEvent.DisplayFailed:
                    Finish(kind, AdOutcome.Failed);
                    break;

                case GmaAdEvent.Closed:
                    if (_pending == null || _pendingKind != kind || _closedAt.HasValue)
                    {
                        break; // un cierre repetido no cuenta dos veces
                    }

                    if (kind == GmaAdKind.Interstitial)
                    {
                        Finish(kind, AdOutcome.Shown);
                    }
                    else if (_rewardReceived)
                    {
                        Finish(kind, AdOutcome.Rewarded);
                    }
                    else
                    {
                        _closedAt = _clock.UtcNow; // espera la recompensa RewardGrace (Tick)
                    }

                    break;
            }
        }

        /// <summary>Espera tras <paramref name="failures"/> fallos de carga seguidos: 2, 4, 8 … hasta 64 s.</summary>
        public static TimeSpan Backoff(int failures)
        {
            int exponent = Math.Max(0, Math.Min(failures - 1, 5));
            double seconds = FirstRetry.TotalSeconds * (1 << exponent);
            return TimeSpan.FromSeconds(Math.Min(seconds, MaxRetry.TotalSeconds));
        }

        /// <inheritdoc />
        void IGmaListener.OnPaid(GmaAdKind kind, string adSource, long valueMicros, string currency)
        {
            AdPlacement placement = kind == GmaAdKind.Interstitial
                ? AdPlacement.InterstitialPostLevel
                : LastRewardPlacement ?? AdPlacement.DoubleHarvest;
            Impression?.Invoke(new AdImpression(
                adSource, placement, valueMicros / 1_000_000.0, string.IsNullOrEmpty(currency) ? "USD" : currency, AdImpression.PlatformAdMob));
        }

        /// <inheritdoc />
        public void OnResume()
        {
            if (_pending != null)
            {
                _resumedAt = _clock.UtcNow;
            }

            // UMP no pudo resolver (p. ej. sin red en la primera sesión): se vuelve a preguntar al volver a la app.
            if (_allowed && ConsentRequested && !_consentInFlight && !_canRequestAds && !InitRequested)
            {
                RequestConsent();
            }
        }

        /// <summary>
        /// Cada fotograma (sin asignaciones mientras no toca): entrega los avisos del SDK, cierra el recompensado que se cerró sin
        /// recompensa pasado <see cref="RewardGrace"/> y suelta el anuncio pendiente pasado <see cref="ResumeGrace"/> tras volver.
        /// </summary>
        public void Tick()
        {
            _port.Pump();
            if (_pending == null)
            {
                return;
            }

            DateTime now = _clock.UtcNow;
            if (_closedAt.HasValue && now - _closedAt.Value >= RewardGrace)
            {
                Finish(_pendingKind, AdOutcome.Skipped);
                return;
            }

            if (_resumedAt.HasValue && now - _resumedAt.Value >= ResumeGrace)
            {
                GmaAdKind kind = _pendingKind;
                Finish(kind, kind == GmaAdKind.Interstitial ? AdOutcome.Shown : _rewardReceived ? AdOutcome.Rewarded : AdOutcome.Skipped);
            }
        }

        private void RequestConsent()
        {
            ConsentRequested = true;
            _consentInFlight = true;
            _port.Listener = this;
            _port.RequestConsent(_underAgeApplied == true);
        }

        private void StartSdk()
        {
            if (InitRequested)
            {
                return;
            }

            InitRequested = true;
            _port.Listener = this;
            _port.Initialize();
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
                TryLoad(GmaAdKind.Rewarded);
            }

            if (config.BizInterstitialsEnabled && _nextLevel() >= config.AdsFirstInterstitialLevel - 1)
            {
                TryLoad(GmaAdKind.Interstitial);
            }
        }

        private void TryLoad(GmaAdKind kind)
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
            _port.Load(kind, _nonPersonalized, _restricted);
        }

        private void Finish(GmaAdKind kind, AdOutcome outcome)
        {
            if (_pending == null || _pendingKind != kind)
            {
                return;
            }

            Action<AdOutcome> done = _pending;
            _pending = null;
            _rewardReceived = false;
            _closedAt = null;
            _resumedAt = null;
            if (_allowed)
            {
                EnsureLoads(); // el siguiente, ya
            }

            done(outcome);
        }

        private static void Discard(UnitState unit)
        {
            unit.Loaded = false;
            unit.Stale = unit.Loading;
        }

        private UnitState Unit(GmaAdKind kind) => kind == GmaAdKind.Rewarded ? _rewarded : _interstitial;

        private static GmaAdKind KindOf(AdPlacement placement) => placement.IsRewarded() ? GmaAdKind.Rewarded : GmaAdKind.Interstitial;

        private sealed class UnitState
        {
            public bool Loading;
            public bool Loaded;
            public bool Stale;
            public int Failures;
            public DateTime RetryAt;
        }
    }
}
