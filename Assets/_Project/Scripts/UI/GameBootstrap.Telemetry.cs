#nullable enable
using System;
using AF.Ads;
using AF.Analytics;
using AF.Analytics.Adapters;
using AF.Analytics.Firebase;
using AF.Analytics.Unity;
using AF.Economy;
using AF.Events;
using AF.GameFlow;
using AF.IAP;
using AF.Levels;
using AF.Platform;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using AF.RemoteConfig.Firebase;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Remote Config, bus y analítica del arranque (bloque B7, parche B7-bootstrap; 08 §2.2-§2.3, §8 y §9):
    /// <list type="number">
    /// <item><see cref="SetUpTelemetry"/>, antes de la cartera: el bus (<see cref="EventBus"/>), Remote Config con los defaults
    /// compilados y la caché del proveedor (<see cref="RemoteConfigBackends.CreateDefault"/>: Firebase Remote Config en el teléfono con
    /// la configuración de Firebase, que además descarga para el arranque siguiente; si no, el falso vacío, sin red), las clases de
    /// cifras enlazadas (<see cref="RcConfigBinder"/>; <see cref="ActiveRcConfigs"/>), Crashlytics (08 §2.3, diagnóstico), la sesión y
    /// <see cref="AnalyticsService"/> con el sumidero de logcat (<c>AF_EVENT</c>, solo desarrollo) y, en el teléfono, el de Firebase
    /// Analytics (F3-6).</item>
    /// <item>Las cifras van a la cartera, el flujo, los anuncios, la tienda, el Reto y la Crecida; los anuncios y la tienda se
    /// envuelven con los adaptadores de analítica (<see cref="WrapAds"/>, <see cref="WrapStore"/>).</item>
    /// <item><see cref="ConnectTelemetry"/>, con todo construido: el bus a la cartera y al flujo de campaña, la analítica a las
    /// pantallas y <c>biz_feature_lives</c> a la cartera.</item>
    /// </list>
    /// Consentimiento: la recogida empieza apagada (<see cref="AnalyticsService.CollectionEnabled"/> = false; 08 §2.3): Firebase
    /// Analytics no recibe nada ni recoge por su cuenta hasta que la decisión de la pantalla de consentimiento (B12;
    /// <c>GameBootstrap.Consent.cs</c>) la enciende antes de <c>AppFlow.Start</c>; con la banda &lt; 13 nunca se enciende.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        // La sesión es del proceso (arranque en frío; D-033, Q-M4): la reconstrucción por idioma no empieza otra.
        private static SessionTracker? s_session;

        private EventBus? _events;
        private RemoteConfigService? _rc;
        private RcConfigs? _rcConfigs;
        private AnalyticsService? _analytics;
        private IClock? _telemetryClock;

        /// <summary>Las cifras de Remote Config del último arranque (las leen los métodos estáticos de la Crecida).</summary>
        internal static RcConfigs? ActiveRcConfigs { get; private set; }

        /// <summary>Proveedor de Remote Config para los tests (por defecto, el falso vacío). <see cref="ResetTelemetryOverrides"/>.</summary>
        internal static IRemoteConfigBackend? RemoteConfigBackendOverride { get; set; }

        /// <summary>Sumidero de analítica añadido para los tests. <see cref="ResetTelemetryOverrides"/> lo limpia.</summary>
        internal static IAnalyticsSink? AnalyticsSinkOverride { get; set; }

        /// <summary>El bus de eventos de la sesión.</summary>
        public EventBus? Events => _events;

        /// <summary>La analítica de la sesión.</summary>
        public AnalyticsService? Analytics => _analytics;

        /// <summary>Remote Config de la sesión.</summary>
        public RemoteConfigService? Rc => _rc;

        /// <summary>Las cifras enlazadas de la sesión.</summary>
        public RcConfigs? RcSettings => _rcConfigs;

        /// <summary>La sesión de analítica en curso.</summary>
        public static SessionTracker? Session => s_session;

        /// <summary>Limpia los ganchos de test de Remote Config y de analítica.</summary>
        internal static void ResetTelemetryOverrides()
        {
            RemoteConfigBackendOverride = null;
            AnalyticsSinkOverride = null;
            ActiveRcConfigs = null;
        }

        // Remote Config y las cifras enlazadas (08 §2.3: RC.LoadDefaults(); RC.ActivateCached(); RC.FetchAsync), al principio de Start,
        // antes de cargar el catálogo de niveles (que lee mech_enabled_* y level_overrides, D-033, Q-B11-7 a, y aplica
        // lvl_label_override, cierre de B11). Una vez por escena: SetUpTelemetry no lo repite.
        private void SetUpRemoteConfig(IClock clock)
        {
            if (_rc != null)
            {
                return;
            }

            IRemoteConfigBackend rcBackend = RemoteConfigBackendOverride ?? RemoteConfigBackends.CreateDefault(RemoteConfigDefaults.Compiled);
            _rc = new RemoteConfigService(null, rcBackend);
            _rc.ActivateCached(); // 08 §2.3: RC.LoadDefaults(); RC.ActivateCached(); RC.FetchAsync(min_interval=12h)
            if (_dataServicesAtBoot && rcBackend is IFetchingRemoteConfigBackend firebaseRc)
            {
                // No bloquea; lo descargado se aplica en el arranque siguiente (las cifras no cambian a mitad de sesión). Con la banda
                // <13 guardada no se descarga nada (B12; 08 §7): la caché local o los defaults compilados.
                DateTime fetchAt = clock.UtcNow;
                _rc.FetchIfDue(fetchAt, firebaseRc.LastFetchUtc, ok =>
                {
                    if (ok)
                    {
                        firebaseRc.MarkFetched(fetchAt);
                    }
                });
            }

            AF.RemoteConfig.RC.Use(_rc);
            _rcConfigs = RcConfigBinder.Bind(_rc, Application.version);
            ActiveRcConfigs = _rcConfigs;
            for (int i = 0; i < _rcConfigs.Errors.Count; i++)
            {
                Debug.LogWarning(LogPrefix + "Remote Config: " + _rcConfigs.Errors[i]);
            }
        }

        private void SetUpTelemetry(IClock clock)
        {
            _telemetryClock = clock;
            _events = new EventBus
            {
                HandlerFailed = (type, e) => Debug.LogError(LogPrefix + "Un oyente de " + type.Name + " falló: " + e.Message),
            };

            SetUpRemoteConfig(clock); // ya hecho al principio de Start; aquí solo si alguien llama sin él
            if (_dataServicesAtBoot)
            {
                SetCrashReporting(true); // 08 §2.3: Crashlytics.Enable() (diagnóstico; pregunta abierta 6); solo en el teléfono. Con <13 guardado, nunca (B12)
            }
            s_session ??= new SessionTracker(clock.UtcNow);
            _analytics = new AnalyticsService(_events, new CommonSource(this));
            _events.Subscribe<LevelStarted>(OnLevelStartedLabel); // af_label_shown, tras level_start (cierre de B11)
            _analytics.AddSink(new LogcatAnalyticsSink());
            // null en el editor y sin google-services.json; con <13 guardado, ni se crea (no se toca Firebase Analytics; B12)
            IAnalyticsSink? firebaseSink = _dataServicesAtBoot ? FirebaseAnalyticsSink.TryCreate() : null;
            if (firebaseSink != null)
            {
                _analytics.AddSink(firebaseSink); // apagado hasta el consentimiento (AddSink le pasa CollectionEnabled = false)
            }

            if (AnalyticsSinkOverride != null)
            {
                _analytics.AddSink(AnalyticsSinkOverride);
            }
        }

        private IAdService WrapAds(IAdService inner)
        {
            return _analytics != null ? new AnalyticsAdService(inner, _analytics) : inner;
        }

        private IStoreService WrapStore(IStoreService inner)
        {
            return _analytics != null ? new AnalyticsStoreService(inner, _analytics) : inner;
        }

        private void ConnectTelemetry()
        {
            if (_events == null || _rcConfigs == null)
            {
                return;
            }

            ConnectWallet(_wallet);
            if (_analytics != null && _services != null)
            {
                _analytics.SetUserProperty("geo_tier", GeoTierRule.IdOf(_services.Geo)); // 08 §8.1, propiedad de usuario (Q-E3 (a), B5)
            }

            if (_flow != null)
            {
                _flow.Events = _events;
            }

            if (_screen != null)
            {
                _screen.Analytics = _analytics;
            }

            if (_mapScreen != null)
            {
                _mapScreen.Analytics = _analytics;
            }
        }

        // La cartera de campaña y la del Reto: earn/spend por el bus y el kill switch de las Canoas (biz_feature_lives).
        private void ConnectWallet(Wallet? wallet)
        {
            if (wallet == null || _events == null || _rcConfigs == null)
            {
                return;
            }

            wallet.Events = _events;
            wallet.LivesEnabled = _rcConfigs.Gates.Feature(BizFeature.Lives);
        }

        // OnApplicationPause: la sesión (Q-M4).
        private void TelemetryPause(bool paused)
        {
            SessionTracker? session = s_session;
            IClock? clock = _telemetryClock;
            if (session == null || clock == null)
            {
                return;
            }

            if (paused)
            {
                session.Pause(clock.UtcNow);
            }
            else
            {
                session.Resume(clock.UtcNow);
            }
        }

        // af_label_shown (08 §8.2; cierre de B11): con cada intento de un nivel de campaña Difícil o Muy difícil (el chip a la vista
        // al cargar el nivel, 02 §10), justo tras level_start; source = override si la etiqueta es de lvl_label_override
        // (LevelCatalog.IsRelabeled), si no pack. El Reto no publica LevelStarted.
        private void OnLevelStartedLabel(LevelStarted e)
        {
            LevelCatalog? catalog = _app?.Catalog;
            UiAnalytics.LabelShown(_analytics, e.Difficulty, catalog != null && catalog.IsRelabeled(e.Level));
        }

        private void TrackMapOpen(MapOpenSource source, RiverStop? stop, int world)
        {
            UiAnalytics.MapOpen(_analytics, source, stop, world);
        }

        private void TrackMapClose(in MapCloseStats stats)
        {
            UiAnalytics.MapClose(_analytics, stats);
        }

        // Los comunes de 08 §8.1 en cada envío; lo de un nivel solo con la pantalla de nivel delante.
        private sealed class CommonSource : ICommonParamsSource
        {
            private readonly GameBootstrap _boot;

            public CommonSource(GameBootstrap boot)
            {
                _boot = boot;
            }

            public void Fill(CommonParams p)
            {
                LevelFlow? flow = _boot._flow;
                AppFlow? app = _boot._app;
                if (flow != null && flow.HasLevel && app != null && app.Screen == AppScreen.Level)
                {
                    LevelDefinition level = flow.Level;
                    p.Level = flow.LevelNumber;
                    p.World = level.World;
                    p.Difficulty = LevelFlow.DifficultyId(level);
                    p.Murky = level.IsMurky;
                    p.Attempt = flow.Attempt.FailsLevel + 1;
                }

                Wallet? wallet = _boot._wallet;
                if (wallet != null)
                {
                    p.SeedsBalance = wallet.Seeds;
                    CanoesStatus canoes = wallet.Canoes;
                    p.Canoes = canoes.IsInfinite ? -1 : canoes.Count;
                }

                if (_boot._streakService != null && app != null)
                {
                    p.Streak = _boot._streakService.Peek(app.PlayLevel, true).Wins;
                }

                // geo_tier (Q-E3 (a), B5): el de GameServices, de la región del teléfono; con el IAP real, el país de la tienda.
                GameServices? services = _boot._services;
                p.GeoTier = services != null ? GeoTierRule.IdOf(services.Geo) : null;
                p.NoAds = _boot._profile?.Entitlements.NoAds;
                p.AppVersion = Application.version;
                RemoteConfigService? rc = _boot._rc;
                if (rc != null)
                {
                    p.ContentVersion = rc.Get<string>("content_version");
                    p.RcVariant = rc.Variant;
                }

                p.SessionId = s_session?.Id;
                // net ∈ {online, offline} (08 §8.1), sin datos personales; leer la red trae ACCESS_NETWORK_STATE, aprobado en D-037.
                p.Net = Application.internetReachability == NetworkReachability.NotReachable ? "offline" : "online";
            }
        }
    }
}
