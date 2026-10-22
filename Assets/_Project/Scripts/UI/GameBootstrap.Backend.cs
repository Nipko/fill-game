#nullable enable
using System;
using System.Globalization;
using AF.Analytics;
using AF.Backend;
using AF.Backend.Firebase;
using AF.Backend.Unity;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.IAP.PlayBilling;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El backend en el juego (F3-5; D-035; plan del backend §7 y §9; 08 §10.2, §10.3 y §11): la tienda verificada, el libro mayor y la
    /// cuenta, con el cliente que da <see cref="BackendFactory"/> para este build.
    /// <list type="bullet">
    /// <item><see cref="CreateStore"/>: <see cref="VerifiedStoreService"/> sobre la tienda de Play real (<see cref="PlayBillingClient"/>
    /// con el plugin propio de Play Billing 9.0.0; D-040 y su añadido de Q-IAP-1) en un teléfono y con el backend real
    /// (<see cref="HttpBackendClient"/>), o sobre
    /// <see cref="FakeBillingClient"/> (la hoja «Compra simulada» de siempre, con tokens nuevos en cada arranque) en el resto; con el UID
    /// de Firebase Auth como <c>setObfuscatedAccountId</c> (<see cref="FirebaseAuthUid"/>); y el cliente del backend: <see cref="FakeBackendClient"/> en el editor y en
    /// <c>DEVELOPMENT_BUILD</c> sin la configuración de Firebase (concede lo del catálogo, como el servidor: <see cref="CatalogGrants"/>),
    /// <see cref="HttpBackendClient"/> contra el API de dev con los tokens reales de Firebase Auth y App Check en un
    /// <c>DEVELOPMENT_BUILD</c> con <c>google-services.json</c> (F3-6), y <see cref="UnavailableBackendClient"/> en release sin URL o sin
    /// Firebase, con el que la tienda no vende (<see cref="GameBackendSwitch"/>): el Mercado lo dice y Aguas estancadas no ofrece Puñado
    /// ni Balsa.
    /// <c>biz_backend_enabled</c> (08 §9, B7) llega por <see cref="AF.RemoteConfig.Binding.RcBackendSwitch"/>.</item>
    /// <item><see cref="ResumeBackend"/>, al terminar el arranque y al volver a primer plano: las compras sin terminar vuelven de la tienda
    /// (<see cref="VerifiedStoreService.Resume"/>) y, tras la fusión con la nube de B8, <c>restoreLedger</c> (<see cref="LedgerRestore"/>:
    /// reacredita lo que falte con D-038, retira lo anulado y confirma o revoca el SSV, F3-4; D-033, Q-B8-1). Al volver a primer plano
    /// solo se repite si la del arranque no llegó a responder.</item>
    /// <item><see cref="TickBackend"/>, en cada fotograma: cada <see cref="PumpIntervalMs"/> ms y solo con compras en la cola,
    /// <see cref="VerifiedStoreService.Pump"/> (sin asignaciones mientras no toca).</item>
    /// <item>Las notas de compra de la UI (<see cref="GameServices.PurchaseNotes"/>) y lo que se acredita tarde (el Cántaro se vacía,
    /// la skin se pone) con <see cref="VerifiedStoreService.VerificationChanged"/>.</item>
    /// <item>La cuenta (<c>GameBootstrap.Account.cs</c>): <see cref="ServerAccountDelete"/> llama a <see cref="IBackendClient.DeleteAccount"/>
    /// antes del borrado local, y Ajustes enseña el código de <see cref="IBackendClient.GetAccountCode"/> (<c>account_code</c>).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Cada cuánto se reintenta la cola de verificación con el juego abierto (ms) [P].</summary>
        public const float PumpIntervalMs = 5000f;

        private IBackendClient? _backend;
        private VerifiedStoreService? _verifiedStore;
        private FakeBillingClient? _fakeBilling;
        private PlayBillingClient? _playBilling;
        private GameBackendSwitch? _backendSwitch;
        private LocalRevocation? _revocation;
        private LedgerRestore? _ledgerRestore;
        private float _pumpMs;
        private bool _ledgerRestored;
        private bool _accountCodeInFlight;

        /// <summary>Qué cliente del backend usar en los tests (null: el de <see cref="BackendEndpoints.Mode"/>).</summary>
        internal static BackendMode? BackendModeOverride { get; set; }

        /// <summary>El cliente del backend para los tests (gana a <see cref="BackendModeOverride"/>).</summary>
        internal static IBackendClient? BackendClientOverride { get; set; }

        /// <summary>Limpia los ganchos de test del backend.</summary>
        internal static void ResetBackendOverrides()
        {
            BackendModeOverride = null;
            BackendClientOverride = null;
            AgeUnder13Override = null;
        }

        /// <summary>El cliente del backend de la sesión (null antes del arranque).</summary>
        public IBackendClient? Backend => _backend;

        /// <summary>La tienda verificada (null antes del arranque).</summary>
        public VerifiedStoreService? VerifiedStore => _verifiedStore;

        /// <summary>La tienda de la plataforma simulada (editor, tests y builds sin el backend real).</summary>
        public FakeBillingClient? FakeBilling => _fakeBilling;

        /// <summary>La tienda de Play real (D-040), solo en el teléfono con el backend real; null en el resto.</summary>
        public PlayBillingClient? PlayBilling => _playBilling;

        /// <summary>El cliente falso del backend, si es el de este build (editor y desarrollo sin URL).</summary>
        public FakeBackendClient? FakeBackend => _backend as FakeBackendClient;

        /// <summary>La tienda puede hablar con el backend (hay cliente y <c>biz_backend_enabled</c> está encendido).</summary>
        public bool BackendReady => _backendSwitch != null && _backendSwitch.BizBackendEnabled;

        /// <summary>Lo que hizo el último <c>restoreLedger</c> (null si aún no respondió ninguno).</summary>
        public LedgerRestoreReport? LastLedgerRestore { get; private set; }

        // La tienda del arranque (Start): verificada siempre (F3-5), con el cliente que toque.
        private IStoreService CreateStore(Profile profile, StoreLedger ledger, ISimulatedPurchasePresenter presenter, IClock deviceClock, IClock capsClock, ProfileSaveBatch batch)
        {
            BackendMode mode = BackendModeOverride ?? BackendEndpoints.Mode;
            IBackendClient client = BackendClientOverride ?? CreateBackendClient(mode, deviceClock);
            _backend = client;
            if (client is FakeBackendClient fake)
            {
                // Como el servidor (infra/functions/shared/catalog.ts, espejo de 05 §4.1 y 08 §9): el contenido del catálogo de esta sesión.
                StoreCatalog catalog = ledger.Catalog;
                fake.GrantFor = r => CatalogGrants.For(catalog, r.Sku);
                fake.EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku);
            }

            _backendSwitch = new GameBackendSwitch(!(client is UnavailableBackendClient), _rcConfigs?.BackendSwitch);

            // D-040: Play Billing de verdad (plugin propio, Play Billing Library 9.0.0) solo en el teléfono y con el backend real que
            // verifica cada compra; con el backend falso (editor, desarrollo sin google-services.json), la tienda simulada de siempre.
            IBillingClient billing;
            _playBilling = client is HttpBackendClient ? PlayBillingFactory.TryCreate(ledger.Catalog, LaunchOptions.DefaultStoreCurrency) : null;
            if (_playBilling != null)
            {
                _fakeBilling = null;
                billing = _playBilling;
            }
            else
            {
                // Tokens distintos en cada arranque: applied_purchases guarda su token_hash y la tienda simulada no recuerda los de antes.
                string prefix = FakeBillingClient.DefaultTokenPrefix + Guid.NewGuid().ToString("N") + ".";
                _fakeBilling = new FakeBillingClient(ledger.Catalog, LaunchOptions.DefaultStoreCurrency, presenter, deviceClock, prefix);
                billing = _fakeBilling;
            }

            // El plazo de 72 h se mide con el reloj del dispositivo (D-035, añadido de F3-1 y F3-2); setObfuscatedAccountId = el UID de
            // Firebase Auth en el momento de comprar (08 §10.1; null en el editor y hasta la primera llamada autenticada).
            _verifiedStore = new VerifiedStoreService(
                ledger, billing, client, deviceClock, _backendSwitch, new DelegatePlayerUid(() => FirebaseAuthUid.Current), _rcConfigs?.Verification);
            _verifiedStore.VerificationChanged += OnVerificationChanged;
            if (_playBilling != null)
            {
                _playBilling.UnfinishedChanged += OnPlayUnfinished;
                _playBilling.StateChanged += OnPlayStateChanged;
            }

            _revocation = new LocalRevocation(profile, ledger.Wallet, batch, capsClock, _rcConfigs?.Revocation);
            _ledgerRestore = new LedgerRestore(ledger, _revocation, client, _backendSwitch, _verifiedStore);
            if (_backendSwitch.HasBackend)
            {
                ServerAccountDelete = done =>
                {
                    if (!BackendReady)
                    {
                        done(false); // biz_backend_enabled apagado: no se puede borrar en el servidor; no se borra nada aún [P]
                        return;
                    }

                    client.DeleteAccount(r =>
                    {
                        if (!r.IsOk)
                        {
                            Debug.LogWarning(LogPrefix + "deleteAccount: " + r.Error);
                        }

                        done(r.IsOk);
                    });
                };
            }

            // Con Play real no se vende hasta que Play responda con sus precios y sus compras sin terminar (BillingReadyStoreService).
            return _playBilling != null ? new BillingReadyStoreService(_verifiedStore, _playBilling) : (IStoreService)_verifiedStore;
        }

        // D-040: una compra cobrada fuera de la hoja abierta (pendiente de pago que Play cobró, de otra sesión o un no consumible que Play
        // devuelve) entra en la cola de verificación ya, sin esperar al próximo arranque.
        private void OnPlayUnfinished()
        {
            VerifiedStoreService? store = _verifiedStore;
            if (store == null || ProfileLocked)
            {
                return;
            }

            using (Saves())
            {
                store.Resume();
            }
        }

        // Play conectó (o se cayó) o llegaron sus precios: el Mercado se repinta si está abierto.
        private void OnPlayStateChanged()
        {
            _market?.Refresh();
        }

        /// <summary>
        /// Al destruir la escena (<c>OnDestroy</c>: una recarga con RebuildUi o RestartAsFirstRun crea otra tienda): la tienda de Play de
        /// esta escena suelta su conexión con Play (endConnection del plugin), para que sus avisos no lleguen también a la tienda vieja.
        /// Lo que quede sin terminar en Play vuelve a la tienda nueva. (La tienda verificada vieja y sus peticiones en vuelo al servidor
        /// siguen como antes de D-040: ver «Riesgos» de <c>docs/dev/iap.md</c>.)
        /// </summary>
        private void DisposeBilling()
        {
            PlayBillingClient? play = _playBilling;
            if (play == null)
            {
                return;
            }

            play.UnfinishedChanged -= OnPlayUnfinished;
            play.StateChanged -= OnPlayStateChanged;
            play.Dispose();
            _playBilling = null;
        }

        /// <summary>
        /// D-040: Play se conecta solo con la edad ya decidida (y nunca con la banda &lt; 13; 08 §7: «sin IAP») y con el UID de Firebase ya
        /// creado, para que cada compra lleve <c>setObfuscatedAccountId</c> y la cola la guarde con él (08 §10.1; el usuario anónimo lo crea
        /// la primera llamada autenticada, el <c>restoreLedger</c> del arranque). Por eso se intenta en <see cref="ResumeBackend"/>, al
        /// responder el <c>restoreLedger</c> y al cerrar la pantalla de edad y consentimiento (<c>FinishConsentAtBoot</c>). Al volver a
        /// primer plano, consulta lo que Play cobró mientras tanto o vuelve a conectar si se cayó.
        /// </summary>
        private void StartPlayBilling(bool foreground)
        {
            PlayBillingClient? play = _playBilling;
            if (play == null || ProfileLocked || PlayerUnder13() || (ConsentEnabled && _consentFlow?.Record == null))
            {
                return;
            }

            if (play.State == PlayBillingState.NotStarted)
            {
                if (FirebaseAuthUid.Current != null)
                {
                    play.Start();
                }

                return;
            }

            if (foreground)
            {
                play.Refresh();
            }
        }

        /// <summary>
        /// Con Play real, un SKU sin el precio de la tienda no se vende (<see cref="BillingReadyStoreService"/>): la segunda confirmación
        /// de 13-17 tampoco se abre con un precio de respaldo (05 §4.1; Q-B12-6).
        /// </summary>
        private PurchaseStatus? BillingPrecheck(StoreProduct product)
        {
            PlayBillingClient? play = _playBilling;
            return play != null && !play.CanSell(product.Sku) ? PurchaseStatus.StoreUnavailable : (PurchaseStatus?)null;
        }

        private static IBackendClient CreateBackendClient(BackendMode mode, IClock clock)
        {
            switch (mode)
            {
                case BackendMode.Fake:
                    return new FakeBackendClient(clock);
                case BackendMode.Http:
                    // Con los proveedores reales de Firebase Auth y App Check si el build los tiene (F3-6); si no, el falso en
                    // desarrollo y nada en release.
                    return BackendFactory.Create(clock, null, null, null);
                default:
                    return new UnavailableBackendClient();
            }
        }

        /// <summary>
        /// Las compras sin terminar de la tienda (08 §10.3: «reintento en cada arranque») y <c>restoreLedger</c> (08 §11, tras la fusión
        /// con la nube; 08 §10.2, los nonces del SSV). Al volver a primer plano (<paramref name="foreground"/>), <c>restoreLedger</c> solo
        /// si aún no respondió en esta sesión. Con el perfil en solo lectura no se hace nada (no se podría guardar).
        /// </summary>
        internal void ResumeBackend(bool foreground)
        {
            VerifiedStoreService? store = _verifiedStore;
            if (store == null || ProfileLocked)
            {
                return;
            }

            StartPlayBilling(foreground); // D-040

            using (Saves())
            {
                store.Resume();
            }

            if (!foreground || !_ledgerRestored)
            {
                RestoreLedgerNow();
            }
        }

        /// <summary>Pide <c>restoreLedger</c> y aplica su respuesta en una escritura del perfil. False si no se pidió (en vuelo o apagado).</summary>
        public bool RestoreLedgerNow()
        {
            LedgerRestore? restore = _ledgerRestore;
            if (restore == null || ProfileLocked)
            {
                return false;
            }

            ProfileSaveScope scope = Saves(); // la respuesta del cliente falso llega dentro; la real, en su propio lote
            bool asked;
            try
            {
                asked = restore.Restore(report =>
                {
                    using (Saves())
                    {
                        OnLedgerRestored(report);
                    }
                });
            }
            finally
            {
                scope.Dispose();
            }

            return asked;
        }

        private void OnLedgerRestored(LedgerRestoreReport report)
        {
            LastLedgerRestore = report;
            StartPlayBilling(false); // D-040: tras la primera llamada autenticada ya hay UID para setObfuscatedAccountId
            if (!report.Ran)
            {
                Debug.LogWarning(LogPrefix + "restoreLedger: " + report.Error);
                return;
            }

            _ledgerRestored = true;
            int consumables = report.RecreditedConsumables; // 08 §8.2 type ledger_consumables: solo consumibles
            if (report.RecreditedNoAds > 0)
            {
                _analytics?.Log(AnalyticsEvent.Create("af_entitlement_restored").Add("type", "no_ads").Add("count", report.RecreditedNoAds));
            }

            if (consumables > 0)
            {
                _analytics?.Log(AnalyticsEvent.Create("af_entitlement_restored").Add("type", "ledger_consumables").Add("count", consumables)); // 08 §8.2
            }

            if (DevProbe.Enabled)
            {
                MarketProbe("ledger_restore", "\"recredited\":" + Num(report.Recredited) + ",\"revoked\":" + Num(report.Revoked)
                    + ",\"confirmed\":" + Num(report.Confirmed) + ",\"expired\":" + Num(report.Expired) + ",\"left_to_queue\":" + Num(report.LeftToQueue));
            }

            _iconsPiggySeeds = -1; // el inicio relee el saldo y el Cántaro
        }

        /// <summary>Cada fotograma: la cola de verificación cada <see cref="PumpIntervalMs"/> ms, solo si tiene algo. Sin asignaciones.</summary>
        private void TickBackend(float dtMs)
        {
            VerifiedStoreService? store = _verifiedStore;
            if (store == null)
            {
                return;
            }

            _pumpMs += dtMs;
            if (_pumpMs < PumpIntervalMs)
            {
                return;
            }

            _pumpMs = 0f;
            if (store.PendingVerifications.Count == 0 || ProfileLocked)
            {
                return;
            }

            using (Saves())
            {
                store.Pump();
            }
        }

        // Cada cambio de la verificación: la nota de la UI y lo que se acredita fuera de una compra abierta.
        private void OnVerificationChanged(PurchaseVerificationEvent e)
        {
            _services?.PurchaseNotes.Track(e);
            if (DevProbe.Enabled)
            {
                MarketProbe("iap_verification", "\"sku\":\"" + e.Sku + "\",\"state\":\"" + StateId(e.State) + "\",\"live\":" + (e.IsLive ? "true" : "false"));
            }

            if (e.IsLive || e.State != PurchaseVerificationState.Credited)
            {
                return;
            }

            // Acreditada tarde (la cola, al volver a primer plano o en el arranque): al Cántaro pendiente se le resta lo acreditado,
            // conservando lo que entró después de la compra (Q-B5-12; D-035, añadido de F3-5), y la skin
            // comprada se pone (Q-B5-13), como al comprarla; la Cuia de rescate de una Balsa tardía se pierde (D-035, añadido de F3-3).
            _piggy?.OnCreditedLater(e.Sku, e.CreditedPiggySeeds);
            StoreProduct? product = _storeLedger?.Catalog.Find(e.Sku);
            if (product != null && product.Content.GrantsSkin && _skinService != null && _skinService.Select(e.Sku))
            {
                OnMarketSkinSelected(e.Sku);
            }

            _iconsPiggySeeds = -1;
        }

        private static string StateId(PurchaseVerificationState state)
        {
            switch (state)
            {
                case PurchaseVerificationState.Verifying: return "verifying";
                case PurchaseVerificationState.Credited: return "credited";
                case PurchaseVerificationState.AlreadyApplied: return "already_applied";
                case PurchaseVerificationState.Pending: return "pending";
                case PurchaseVerificationState.Queued: return "queued";
                case PurchaseVerificationState.Rejected: return "verify_failed";
                case PurchaseVerificationState.DeferredAck: return "deferred_ack";
                case PurchaseVerificationState.CreditFailed: return "credit_failed";
                default: return ((int)state).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Ajustes: el código de borrado para la web (Q-B13-21; 08 §11). Con <c>account_code</c> guardado, ese; si no, lo pide una vez
        /// (<see cref="IBackendClient.GetAccountCode"/>, que rota: invalida el anterior) y lo guarda. Sin borrado remoto (banda &lt; 13 o
        /// sin backend), no sale nada.
        /// </summary>
        private void ShowAccountCode()
        {
            SettingsScreen? settings = _settingsScreen;
            Profile? profile = _profile;
            IBackendClient? client = _backend;
            if (settings == null || profile == null)
            {
                return;
            }

            if (!RemoteAccountAllowed || client == null || _backendSwitch == null || !_backendSwitch.HasBackend)
            {
                settings.SetAccountCode(null);
                return;
            }

            if (profile.AccountCode != null)
            {
                settings.SetAccountCode(AccountText.Code(profile.AccountCode));
                return;
            }

            if (!BackendReady || ProfileLocked)
            {
                settings.SetAccountCode(AccountText.CodeUnavailable); // sin guardarlo no se puede pedir: rotaría en cada visita
                return;
            }

            if (_accountCodeInFlight)
            {
                return;
            }

            _accountCodeInFlight = true;
            settings.SetAccountCode(null); // mientras llega, nada (no «sin conexión»)
            client.GetAccountCode(r =>
            {
                _accountCodeInFlight = false;
                if (this == null || _profile != profile)
                {
                    return; // la escena ya se fue
                }

                if (!r.IsOk)
                {
                    Debug.LogWarning(LogPrefix + "account/code: " + r.Error);
                    _settingsScreen?.SetAccountCode(AccountText.CodeUnavailable);
                    return; // se vuelve a pedir al abrir Ajustes
                }

                profile.AccountCode = r.Value!.Code;
                using (Saves())
                {
                    _saveBatch?.TrySave(profile);
                }

                _settingsScreen?.SetAccountCode(AccountText.Code(profile.AccountCode));
            });
        }

        /// <summary>
        /// <see cref="IBackendSwitch"/> del juego: hay backend en este build (no <see cref="UnavailableBackendClient"/>) y
        /// <c>biz_backend_enabled</c> está encendido. Sin backend, la tienda verificada no vende (no se podría acreditar nada).
        /// </summary>
        internal sealed class GameBackendSwitch : IBackendSwitch
        {
            private readonly IBackendSwitch _remote;

            public GameBackendSwitch(bool hasBackend, IBackendSwitch? remote)
            {
                HasBackend = hasBackend;
                _remote = remote ?? new BackendSwitch();
            }

            /// <summary>Este build tiene backend (falso en desarrollo, o el real).</summary>
            public bool HasBackend { get; }

            /// <inheritdoc />
            public bool BizBackendEnabled => HasBackend && _remote.BizBackendEnabled;
        }
    }
}
