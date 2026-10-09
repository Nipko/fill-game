#nullable enable
using System;
using System.Collections.Generic;
using AF.Ads;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AF.UI
{
    /// <summary>
    /// Arranque de la escena <c>Game</c> (docs/dev/prototipo-v0.md c; 08 §2.3 en lo que ya existe): lee los paquetes de
    /// niveles (TextAsset) y arma el catálogo (tarea B1-5), carga el perfil local (<c>AF.Save</c>, migrando una vez el
    /// <c>af_v0_level</c> de v0), crea la cartera, los anuncios y la tienda simulados (<c>AF.Ads</c> y <c>AF.IAP</c>, sin SDK:
    /// D-008), genera los gráficos provisionales, construye las pantallas de inicio y de nivel, el <see cref="LevelFlow"/> y el
    /// <see cref="AppFlow"/> y los hace avanzar cada fotograma. La primera vez entra directamente al nivel 1; con un nivel en
    /// curso guardado (<c>level_state</c>, tarea B2-5), lo reabre tal cual; si no, la pantalla de inicio. Conecta AD_GATE con
    /// los anuncios simulados (<see cref="InterstitialGate"/>, tarea B2-4) y la sonda de desarrollo con cada anuncio y
    /// compra. Sus métodos públicos de input (<see cref="IGameActions"/>) son los mismos que llaman los toques y los tests
    /// PlayMode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour, IAppFlowListener, IGameActions
    {
        /// <summary>Tope del paso de tiempo por fotograma (evita saltos tras un tirón).</summary>
        private const float MaxDeltaMs = 100f;

        private const string LogPrefix = "[AF.GameBootstrap] ";

        [SerializeField]
        private TextAsset? levelPack;

        [SerializeField]
        private TextAsset[] extraLevelPacks = Array.Empty<TextAsset>();

        [SerializeField]
        private TextAsset? faunaCatalog;

        private static ProceduralArt? s_art;
        private static string? s_artKey;

        private ProceduralArt? _art;
        private GameScreen? _screen;
        private LobbyScreen? _lobby;
        private LevelFlow? _flow;
        private AppFlow? _app;
        private Wallet? _wallet;
        private AdController? _ads;
        private IStoreService? _store;
        private StoreLedger? _storeLedger;
        private GameServices? _services;
        private AdOverlay? _adOverlay;
        private StoreSheet? _storeSheet;
        private Profile? _profile;
        private IProfileStore? _profileStore;
        private ProfileSaveBatch? _saveBatch;
        private DevProbe? _probe;
        private ProfileLockedNotice? _lockedNotice;
        private ProfileLoadStatus _profileStatus = ProfileLoadStatus.NotFound;
        private float _msCarry;
        private bool _skipNextDelta;

        /// <summary>Instancia activa (una por escena).</summary>
        public static GameBootstrap? Current { get; private set; }

        /// <summary>Flujo del nivel (null hasta <c>Start</c>).</summary>
        public LevelFlow? Flow => _flow;

        /// <summary>Navegación entre pantallas (null hasta <c>Start</c>).</summary>
        public AppFlow? App => _app;

        /// <summary>Pantalla de nivel (null hasta <c>Start</c>).</summary>
        public GameScreen? View => _screen;

        /// <summary>Pantalla de inicio (null hasta <c>Start</c>).</summary>
        public LobbyScreen? Lobby => _lobby;

        /// <summary>Cartera (null hasta <c>Start</c>).</summary>
        public Wallet? Wallet => _wallet;

        /// <summary>Anuncios simulados (null hasta <c>Start</c>).</summary>
        public AdController? Ads => _ads;

        /// <summary>Tienda simulada (null hasta <c>Start</c>).</summary>
        public IStoreService? Store => _store;

        /// <summary>Servicios del embudo (null hasta <c>Start</c>).</summary>
        public GameServices? Services => _services;

        /// <summary>Pantalla «Anuncio simulado» del juego (null con un presentador de test).</summary>
        internal AdOverlay? AdOverlayView => _adOverlay;

        /// <summary>Hoja «Compra simulada» del juego (null con un presentador de test).</summary>
        internal StoreSheet? StoreSheetView => _storeSheet;

        /// <summary>Perfil cargado (null hasta <c>Start</c>).</summary>
        public Profile? Profile => _profile;

        /// <summary>Almacén del perfil (null hasta <c>Start</c>).</summary>
        public IProfileStore? ProfileStore => _profileStore;

        /// <summary>
        /// Lote de guardado que envuelve <see cref="ProfileStore"/>: todo lo que escribe el perfil pasa por él y cada acción del
        /// jugador lo escribe una sola vez (null hasta <c>Start</c>).
        /// </summary>
        public ProfileSaveBatch? SaveBatch => _saveBatch;

        /// <summary>
        /// El perfil del dispositivo no se puede usar ni pisar (<see cref="ProfileLoadStatus.TooNew"/> o
        /// <see cref="ProfileLoadStatus.MigrationFailed"/>; 08 §4.4): se juega con un perfil en blanco que no se guarda, se avisa
        /// con «Actualiza Tintalú» y no se ofrecen compras (medida mínima [P]; la pantalla de bloqueo la decide el dueño).
        /// </summary>
        public bool ProfileLocked => IsLocked(_profileStatus);

        /// <summary>Resultado de la carga del perfil.</summary>
        public ProfileLoadStatus ProfileStatus => _profileStatus;

        /// <summary>Aviso «Actualiza Tintalú» del perfil en solo lectura (null si el perfil se cargó bien).</summary>
        internal ProfileLockedNotice? LockedNotice => _lockedNotice;

        /// <summary>Primer paquete de niveles asignado.</summary>
        public TextAsset? LevelPackAsset => levelPack;

        /// <summary>Paquetes de niveles asignados, en orden (escena generada por <c>GameSceneBuilder</c>).</summary>
        public IReadOnlyList<TextAsset> LevelPackAssets
        {
            get
            {
                var list = new List<TextAsset>();
                if (levelPack != null)
                {
                    list.Add(levelPack);
                }

                for (int i = 0; i < extraLevelPacks.Length; i++)
                {
                    if (extraLevelPacks[i] != null)
                    {
                        list.Add(extraLevelPacks[i]);
                    }
                }

                return list;
            }
        }

        /// <summary>Catálogo de miquitos asignado (<c>miquitos_v0.json</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? FaunaCatalogAsset => faunaCatalog;

        /// <summary>Catálogo de miquitos leído al arrancar (null = la intro siempre en modo dosel).</summary>
        public FaunaCatalog? Fauna { get; private set; }

        /// <summary>Asigna el catálogo de miquitos de la intro (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureFauna(TextAsset? catalog)
        {
            faunaCatalog = catalog;
        }

        /// <summary>Asigna el paquete de niveles (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void Configure(TextAsset pack)
        {
            Configure(new[] { pack });
        }

        /// <summary>
        /// Asigna los paquetes de niveles (08 §2.2: índice nivel → paquete; hoy <c>tutorial_v0</c>, en la integración también
        /// <c>mundo_02</c> … <c>mundo_05</c>). El primero va en <c>levelPack</c> y el resto en <c>extraLevelPacks</c>.
        /// </summary>
        public void Configure(IReadOnlyList<TextAsset> packs)
        {
            if (packs == null || packs.Count == 0)
            {
                throw new ArgumentException("Hace falta al menos un paquete de niveles.", nameof(packs));
            }

            levelPack = packs[0];
            var extra = new TextAsset[packs.Count - 1];
            for (int i = 1; i < packs.Count; i++)
            {
                extra[i - 1] = packs[i];
            }

            extraLevelPacks = extra;
        }

        /// <summary>Toque en una cuia (02 §3.1). Tras un vertido, la sonda de desarrollo escribe lo que costó guardar (AF_PERF).</summary>
        public TapResult TapCuia(int index)
        {
            if (_flow == null || !InLevel)
            {
                return TapResult.Ignored(index, -1);
            }

            int writes = _saveBatch?.WriteCount ?? 0;
            TapResult result = _flow.TapCuia(index);
            if (result.Kind == TapKind.Poured && _saveBatch != null && DevProbe.Enabled)
            {
                _probe?.SaveCost("pour", _flow.LevelNumber, _saveBatch.WriteCount - writes, _saveBatch.LastWriteMs);
            }

            return result;
        }

        /// <summary>Toque en el fondo (C7).</summary>
        public void TapBackground()
        {
            if (InLevel)
            {
                _flow?.TapBackground();
            }
        }

        /// <summary>Toque en cualquier punto durante la intro o la celebración.</summary>
        public bool TapAnywhere()
        {
            return _flow != null && InLevel && _flow.TapAnywhere();
        }

        /// <summary>Marea (bandeja o modal). C8: primero quita la selección (<see cref="LevelFlow.PressUndo"/>).</summary>
        public bool PressUndo()
        {
            return _flow != null && InLevel && _flow.PressUndo();
        }

        /// <summary>Volver a la orilla (cabecera o modal). C8: primero quita la selección (<see cref="LevelFlow.PressRestart"/>).</summary>
        public bool PressRestart()
        {
            return _flow != null && InLevel && _flow.PressRestart();
        }

        /// <summary>Siguiente (pantalla de victoria): N + 1, «El río sigue creciendo» o Sin canoas (<see cref="AppFlow.PressNext"/>).</summary>
        public bool PressNext()
        {
            return _app != null && _app.PressNext();
        }

        /// <summary>«Inicio» (pantalla de victoria) [P]: vuelve a la pantalla de inicio.</summary>
        public bool PressHome()
        {
            return _app != null && _app.PressHome();
        }

        /// <summary>
        /// «Cosecha doble: x2 por video» (<c>rv_double_harvest</c>, desde el 11; 04 §2.8): al terminar el anuncio, otra Cosecha
        /// igual con <c>Wallet.Apply</c>. False si no se ofrece o no hay video (sin fill el botón ni aparece).
        /// </summary>
        public bool PressDoubleHarvest()
        {
            LevelFlow? flow = _flow;
            AdController? ads = _ads;
            if (flow == null || ads == null || !InLevel || !flow.DoubleHarvestOffered || !ads.CheckRewarded(AdPlacement.DoubleHarvest).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.DoubleHarvest, r =>
            {
                if (r.IsRewarded)
                {
                    flow.ApplyDoubleHarvest();
                }
            });
            return true;
        }

        /// <summary>«+1 cuia» de la bandeja (04 §2.2): del stock o por 150 semillas.</summary>
        public bool PressExtraCuia()
        {
            TransactionResult? result = _flow != null && InLevel ? _flow.UseExtraCuia() : null;
            return result != null && result.IsApplied;
        }

        /// <summary>«+3 Deshacer · Ver video» (<c>rv_undo_refill</c>): al terminar el anuncio, +3 Mareas al stock.</summary>
        public bool PressUndoRefillVideo()
        {
            LevelFlow? flow = _flow;
            AdController? ads = _ads;
            if (flow == null || ads == null || !InLevel || !ads.CheckRewarded(AdPlacement.UndoRefill).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.UndoRefill, r =>
            {
                if (r.IsRewarded)
                {
                    flow.GrantUndoRefill();
                }
            });
            return true;
        }

        /// <summary>«+3 Deshacer · 30 semillas».</summary>
        public bool PressUndoRefillSeeds()
        {
            TransactionResult? result = _flow != null && InLevel ? _flow.RefillUndoWithSeeds() : null;
            return result != null && result.IsApplied;
        }

        /// <summary>Recarga gratis de +1 Marea de la rama F [P].</summary>
        public bool PressUndoGift()
        {
            return _flow != null && InLevel && _flow.GrantUndoGift();
        }

        /// <summary>«Continuar: +1 cuia» en Aguas estancadas o en ¿Seguro?</summary>
        public bool PressRescue()
        {
            TransactionResult? result = _flow != null && InLevel ? _flow.BuyRescue() : null;
            return result != null && result.IsApplied;
        }

        /// <summary>Puñado de semillas (<c>af_coins_2</c>) desde el modal sin saldo: compra simulada (04 §6.2 <c>PUÑADO_199</c>).</summary>
        public bool PressHandful()
        {
            return Purchase(StoreCatalog.Handful, PurchaseContext.Stuck, null);
        }

        /// <summary>
        /// Balsa de rescate (<c>af_rescue</c>): compra simulada; si se compra, la Cuia de rescate se aplica sin semillas (04 §6.2
        /// <c>BALSA_499</c>); si se cancela, cuenta como rechazo.
        /// </summary>
        public bool PressBalsa()
        {
            LevelFlow? flow = _flow;
            return Purchase(StoreCatalog.Rescue, PurchaseContext.Stuck, r =>
            {
                if (r.AppliesRescue && flow != null)
                {
                    flow.ApplyPaidRescue();
                }
            });
        }

        /// <summary>«Rendirse» [P] (desde el 31).</summary>
        public bool PressGiveUp()
        {
            return _flow != null && InLevel && _flow.PressGiveUp();
        }

        /// <summary>X o «Rendirse» en ¿Seguro?: fallo duro y Nivel fallido.</summary>
        public bool ConfirmGiveUp()
        {
            return _flow != null && InLevel && _flow.ConfirmGiveUp();
        }

        /// <summary>«Reintentar» en Nivel fallido (con 0 canoas, Sin canoas).</summary>
        public bool PressRetry()
        {
            return _flow != null && InLevel && _flow.PressRetry();
        }

        /// <summary>X de Nivel fallido: al inicio (06 §4.7).</summary>
        public bool PressLevelFailedClose()
        {
            return _flow != null && _app != null && _flow.Phase == FlowPhase.LevelFailed && _app.ExitToLobby();
        }

        /// <summary>«Canoa extra · Ver video» (<c>rv_extra_canoe</c>): +1 canoa al terminar el anuncio.</summary>
        public bool PressExtraCanoeVideo()
        {
            AdController? ads = _ads;
            Wallet? wallet = _wallet;
            if (ads == null || wallet == null || !ads.CheckRewarded(AdPlacement.ExtraCanoe).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.ExtraCanoe, r =>
            {
                if (r.IsRewarded)
                {
                    wallet.Apply(Transaction.AddCanoe());
                }
            });
            return true;
        }

        /// <summary>«Llenar canoas · 200 semillas» (hasta 5/5).</summary>
        public bool PressRefillCanoes()
        {
            return _wallet != null && _wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill)).IsApplied;
        }

        /// <summary>«Canoas infinitas 1 h» (<c>af_infinite_canoes_1h</c>): compra simulada.</summary>
        public bool PressInfiniteCanoes()
        {
            return Purchase(StoreCatalog.InfiniteCanoes1h, PurchaseContext.NoCanoes, null);
        }

        /// <summary>«Volver al inicio» de Sin canoas (desde el nivel o desde el inicio).</summary>
        public bool PressNoCanoesHome()
        {
            if (_app == null)
            {
                return false;
            }

            if (_app.Screen == AppScreen.NoCanoes)
            {
                return _app.CloseOverlay();
            }

            return _flow != null && _flow.Phase == FlowPhase.NoCanoes && _app.ExitToLobby();
        }

        /// <summary>X de Sin canoas: vuelve a donde se abrió (Nivel fallido o el tablero; desde el inicio, al inicio).</summary>
        public bool PressNoCanoesClose()
        {
            if (_app != null && _app.Screen == AppScreen.NoCanoes)
            {
                return _app.CloseOverlay();
            }

            return _flow != null && InLevel && _flow.CloseNoCanoes();
        }

        /// <summary>
        /// Pausa › «Salir» (D-008): desde el 31, ya confirmado, fallo duro y Nivel fallido (−1 canoa si hay una en juego); en
        /// 1-30, al inicio sin coste.
        /// </summary>
        public bool PressQuit()
        {
            LevelFlow? flow = _flow;
            if (flow == null || _app == null || !InLevel || !flow.CanQuit)
            {
                return false;
            }

            return flow.QuitIsHardFail ? flow.ConfirmQuit() : _app.ExitToLobby();
        }

        /// <summary>«Jugar · Nivel {n}» (pantalla de inicio).</summary>
        public bool PressPlay()
        {
            return _app != null && _app.PressPlay();
        }

        /// <summary>Contador de canoas de la pantalla de inicio (con 0/5 abre Sin canoas).</summary>
        public bool PressCanoes()
        {
            return _app != null && _app.PressCanoes();
        }

        /// <summary>Volver desde «El río sigue creciendo» o Sin canoas.</summary>
        public bool PressBack()
        {
            return _app != null && _app.CloseOverlay();
        }

        /// <inheritdoc />
        public void OnAppScreenChanged(AppScreen screen)
        {
            // La pantalla de nivel se muestra antes de cargar el nivel (AppFlow llama a Load después).
            _screen?.SetVisible(screen == AppScreen.Level);
            _lobby?.Show(screen);
            _skipNextDelta = true;
        }

        private bool InLevel => _app == null || _app.Screen == AppScreen.Level;

        // Compra simulada de sku (05 §4.1); lo comprado se acredita con Wallet.Apply dentro de la tienda.
        private bool Purchase(string sku, PurchaseContext context, Action<PurchaseResult>? onDone)
        {
            IStoreService? store = _store;
            if (store == null || store.IsPurchasing || ProfileLocked)
            {
                return false; // con el perfil en solo lectura lo comprado se perdería (08 §4.4): no se vende nada
            }

            int level = _flow != null && _flow.HasLevel ? _flow.LevelNumber : (_app?.OverlayLevel ?? 0);
            store.Purchase(new PurchaseRequest(sku, context, level), r =>
            {
                _probe?.Purchase(r, _wallet);
                onDone?.Invoke(r);
            });
            return true;
        }

        private void Awake()
        {
            Current = this;
            Application.targetFrameRate = 60; // 08 §5.4
            DevProbe.Initialize();
            EnsureEventSystem();
        }

        private void Start()
        {
            LevelCatalog? catalog = LoadCatalog();
            if (catalog == null)
            {
                enabled = false;
                return;
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            IProfileStore store = LaunchOptions.ProfileStoreOverride ?? new LocalProfileStore(Application.persistentDataPath);
            Profile profile = LoadProfile(store, clock, LaunchOptions.ProfileStoreOverride == null, out bool firstRun, out _profileStatus);
            bool locked = IsLocked(_profileStatus);
            if (LaunchOptions.ApplyDevWallet(profile, clock))
            {
                store.TrySave(profile); // extras af_seeds y af_canoes de los builds de desarrollo [P] (B2-6)
            }

            _profile = profile;
            _profileStore = store;

            // Cartera, progreso, nivel en curso, anuncios y tienda guardan el mismo perfil por el lote: dentro de una acción del
            // jugador (vertido, victoria, rescate, rendición, compra, anuncio) el perfil se escribe una sola vez (revisión B2-5).
            var batch = new ProfileSaveBatch(store);
            _saveBatch = batch;
            _wallet = new Wallet(profile, batch, clock);
            IProgressStore progress = LaunchOptions.ProgressStoreOverride ?? new ProfileProgressStore(profile, batch);

            _art = SharedArt(catalog.WaterBase);
            _probe = new DevProbe();

            // Anuncios y compras simulados (D-008 puntos 4 y 5; sin SDK, red ni identificadores): sus pantallas van en lienzos
            // propios por encima de todo; los tests las sustituyen por presentadores inmediatos (LaunchOptions).
            // Cada anuncio escribe su tipo y su ubicación en la sonda (AF_STATE ad y ad_closed, B2-4).
            // Al cerrar un anuncio o una compra, lo que se anota (registro, recompensa, compra y su efecto en el nivel) va en un
            // lote: una escritura del perfil.
            ISimulatedAdPresenter adPresenter = new ProbedAdPresenter(
                LaunchOptions.AdPresenterOverride ?? (_adOverlay = new AdOverlay(transform, _art)),
                _probe,
                () => _flow != null && _flow.HasLevel && InLevel ? _flow.LevelNumber : (_app?.OverlayLevel ?? 0),
                () => _wallet,
                batch);
            ISimulatedPurchasePresenter purchasePresenter = new BatchedPurchasePresenter(
                LaunchOptions.PurchasePresenterOverride ?? (_storeSheet = new StoreSheet(transform, _art)), batch);
            _ads = AdController.Create(new FakeAdService(adPresenter, LaunchOptions.ResolveAdFill()), profile, batch, clock, firstRun);
            _storeLedger = new StoreLedger(profile, _wallet, batch, clock);
            _store = new FakeStoreService(_storeLedger, purchasePresenter, LaunchOptions.DefaultStoreCurrency);
            _services = new GameServices(_wallet, _ads, _store, _storeLedger, GeoTier.T3, purchasesBlocked: locked);

            _screen = new GameScreen(transform, _art, _probe, this);
            _screen.SetVisible(false);
            Fauna = LoadFauna();
            _screen.Fauna = Fauna; // la especie de miquito de cada intro (guion de la intro §4.3)
            _lobby = new LobbyScreen(transform, _art, _probe, () => PressPlay(), () => PressCanoes(), () => { }, () => PressBack(), this);

            // rules_pour_mode (D-005): se resuelve una vez al arrancar y se aplica en cada carga de nivel.
            _flow = new LevelFlow(catalog, progress, _screen, GameConfig.TapSwitchSelection, LaunchOptions.ResolvePourMode(), _wallet)
            {
                AdGate = new InterstitialGate(_ads, _probe, _wallet), // AD_GATE tras «Siguiente» (B2-4)
                StateStore = new ProfileLevelStateStore(profile, batch), // nivel en curso tras cada vertido (B2-5)
                SaveBatch = batch, // una escritura del perfil por acción del jugador
            };
            _screen.Bind(_flow, _services);
            _app = new AppFlow(_flow, progress, _wallet, this);
            _lobby.Bind(_app, _services);

            int requested = LaunchOptions.StartLevelOverride;
            if (requested <= 0)
            {
                requested = LaunchOptions.ReadIntentLevel();
            }

            if (requested > 0)
            {
                _app.StartAtLevel(requested);
            }
            else
            {
                // 02 §14: con un nivel en curso guardado se reabre tal cual (sin intro, con su modal), sin cobrar nada.
                _app.Start(firstRun, profile.LevelState);
            }

            if (locked)
            {
                // 08 §4.4: el perfil anterior se conserva intacto y esta sesión no se guarda; se avisa y no hay compras.
                _lockedNotice = new ProfileLockedNotice(transform, _art);
                _lockedNotice.Show(_profileStatus);
            }

            _skipNextDelta = true;
        }

        private void Update()
        {
            if (_flow == null || _screen == null || _app == null)
            {
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime * 1000f, MaxDeltaMs);
            if (_skipNextDelta)
            {
                _skipNextDelta = false;
                dt = 0f;
            }

            _adOverlay?.Tick(Time.unscaledDeltaTime * 1000f);
            if (_app.Screen != AppScreen.Level)
            {
                _app.RefreshNoCanoes();
                _lobby?.Tick(dt);
                return;
            }

            _screen.Tick(dt);
            _msCarry += dt;
            int whole = (int)_msCarry;
            _msCarry -= whole;
            _flow.Tick(whole, _screen.IsViewIdle);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // Lo pendiente de la cartera, los anuncios y la tienda (una escritura fallida) se reintenta al pasar a segundo plano.
                _saveBatch?.Flush();
                _wallet?.Flush();
                _ads?.Ledger.Flush();
                _storeLedger?.Flush();
                (_flow?.StateStore as ProfileLevelStateStore)?.Flush(); // 02 §14: el nivel en curso ya está en disco
                return;
            }

            // 02 §14: al volver de segundo plano la vista salta al modelo sin animar y la selección se limpia.
            if (_flow != null && _screen != null && InLevel)
            {
                _screen.SnapToModel();
                _flow.ClearSelection();
            }
        }

        // Catálogo de miquitos de la escena (af-fauna/0). Si falta o no valida, la intro va siempre en modo dosel (sin tropa).
        private FaunaCatalog? LoadFauna()
        {
            if (faunaCatalog == null)
            {
                Debug.LogWarning(LogPrefix + "falta el catálogo de miquitos (TextAsset): la intro irá en modo dosel. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return null;
            }

            try
            {
                return FaunaCatalogReader.Read(faunaCatalog.text);
            }
            catch (FaunaFormatException e)
            {
                Debug.LogError(LogPrefix + e.Message);
                return null;
            }
        }

        // Paquetes de la escena (o el del test, LaunchOptions.LevelPackJsonOverride) → catálogo con el desplazamiento de
        // desarrollo (af_level_offset). Un paquete roto no deja la pantalla en blanco con una excepción en Start: se registra.
        private LevelCatalog? LoadCatalog()
        {
            var packs = new List<LevelPack>();
            try
            {
                string? packJson = LaunchOptions.LevelPackJsonOverride;
                if (packJson != null)
                {
                    packs.Add(LevelPackReader.Read(packJson));
                }
                else
                {
                    IReadOnlyList<TextAsset> assets = LevelPackAssets;
                    for (int i = 0; i < assets.Count; i++)
                    {
                        packs.Add(LevelPackReader.Read(assets[i].text));
                    }
                }

                if (packs.Count == 0)
                {
                    Debug.LogError(LogPrefix + "faltan los paquetes de niveles (TextAsset). Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                    return null;
                }

                LevelCatalog catalog = LevelCatalog.Create(packs, LaunchOptions.ResolveLevelOffset());
                if (!catalog.IsContiguous)
                {
                    Debug.LogError(LogPrefix + "faltan niveles en el catálogo: " + catalog.DescribeGaps() + ". Al llegar a ellos se verá «El río sigue creciendo».");
                }

                if (catalog.LevelOffset > 0)
                {
                    Debug.LogWarning(LogPrefix + "desplazamiento de nivel de desarrollo: " + catalog.LevelOffset + " (niveles " + catalog.FirstLevel + "-" + catalog.LastLevel + ").");
                }

                return catalog;
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(LogPrefix + "el paquete de niveles no es válido: " + e.Message);
                return null;
            }
        }

        // 08 §2.3: profile = Save.LoadLocal() ?? Profile.New(). La primera vez (no había perfil ni progreso de v0) se entra
        // directamente al nivel 1. El af_v0_level de PlayerPrefs se migra una vez (B1-5), solo con el almacén real. Con un
        // perfil más nuevo o una migración fallida (08 §4.4) el almacén no deja guardar: el perfil en blanco de esta sesión no
        // se guarda nunca (ProfileLocked).
        private static Profile LoadProfile(IProfileStore store, IClock clock, bool migrateLegacy, out bool firstRun, out ProfileLoadStatus status)
        {
            ProfileLoadResult result = store.Load();
            status = result.Status;
            if (result.Error != null)
            {
                Debug.LogWarning(LogPrefix + "perfil (" + result.Status + "): " + result.Error);
            }

            if (IsLocked(result.Status))
            {
                Debug.LogWarning(LogPrefix + "perfil en solo lectura (" + result.Status + "): se juega sin guardar y sin compras hasta actualizar la app (08 §4.4).");
            }

            Profile profile = result.Profile ?? Wallet.NewProfile(clock);
            bool isNew = result.Profile == null;
            firstRun = result.Status == ProfileLoadStatus.NotFound;
            if (migrateLegacy)
            {
                LegacyProgressOutcome outcome = ProgressMigration.MigrateV0(profile, isNew, store, new PlayerPrefsProgressStore());
                if (outcome == LegacyProgressOutcome.Migrated || outcome == LegacyProgressOutcome.MigratedNotSaved)
                {
                    firstRun = false;
                    Debug.Log(LogPrefix + "progreso de v0 (" + PlayerPrefsProgressStore.Key + ") migrado al perfil: siguiente nivel " + profile.Progress.NextLevel + (outcome == LegacyProgressOutcome.MigratedNotSaved ? " (sin guardar aún)." : "."));
                }
            }

            if (isNew && firstRun)
            {
                // Perfil de primera ejecución: se guarda ya, así la siguiente apertura va a la pantalla de inicio.
                store.TrySave(profile);
            }

            return profile;
        }

        private static bool IsLocked(ProfileLoadStatus status)
        {
            return status == ProfileLoadStatus.TooNew || status == ProfileLoadStatus.MigrationFailed;
        }

        // El arte procedural se genera una vez por sesión y por agua base (guía visual v2, 8: la generación al arrancar
        // cuesta CPU): recargar la escena o pasar de nivel no lo repite. Vive mientras dure la app.
        private static ProceduralArt SharedArt(string waterBase)
        {
            string key = waterBase;
            if (s_art == null || s_artKey != key)
            {
                s_art?.Dispose();
                s_art = new ProceduralArt(waterBase);
                s_artKey = key;
            }

            return s_art;
        }

        private void OnDestroy()
        {
            _saveBatch?.Flush();
            _wallet?.Flush();
            (_flow?.StateStore as ProfileLevelStateStore)?.Flush();
            if (Current == this)
            {
                Current = null;
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            // Sin acciones asignadas, InputSystemUIInputModule toma las acciones por defecto al activarse.
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
