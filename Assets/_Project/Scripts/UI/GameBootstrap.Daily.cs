#nullable enable
using System;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Arranque del Reto del río (tarea R9; encargo §3.4, «Arranque»; nota §14, E2): <see cref="SetUpDaily"/> crea el pool, el
    /// servicio (<see cref="DailyChallengeService"/>, R7b), la tarjeta del inicio (<see cref="DailyChallengeCard"/>) y la pantalla
    /// de la partida (<see cref="DailyChallengeScreen"/>) con el perfil, la cartera, el progreso y el reloj que ya creó
    /// <c>Start</c>, y añade el <see cref="DailyChallengeRunner"/> que los avanza. Parcial nuevo: <c>GameBootstrap.cs</c> no se toca.
    /// <list type="bullet">
    /// <item><b>E2</b> (lo aplica el integrador): llamar a <see cref="SetUpDaily"/> en <c>Start</c>, justo después de
    /// <c>_lobby.Bind(_app, _services);</c>. Sin esa llamada no hay Reto (D-008: oculto mientras no exista).</item>
    /// <item><b>E3</b>: <c>GameSceneBuilder</c> asigna <c>Data/Daily/daily_pool_v1.json</c> con <see cref="ConfigureDailyPool"/> al
    /// campo serializado <see cref="dailyPool"/>, que declara este parcial. Sin pool la tarjeta queda oculta.</item>
    /// <item><b>E4</b>: el bloque <c>daily_challenge</c> del perfil (<see cref="ProfileDailyChallengeStore"/>) se conecta con
    /// <see cref="DailyStoreFactory"/> (por defecto, <see cref="CreateProfileDailyStore"/>). Si alguien lo deja en null, en
    /// desarrollo se usa un almacén en memoria (se pierde al cerrar la app y, por eso, la garantía de «nunca dos pagos» de
    /// <see cref="IDailyChallengeStore"/> solo vale dentro de la sesión) y en release la tarjeta queda oculta.</item>
    /// </list>
    /// La tarjeta va en un lienzo propio encima del inicio (orden <see cref="DailyCardSortingOrder"/>), centrada sobre «Jugar»
    /// [P]; con el enganche E1 pasará a <c>LobbyScreen</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Orden del lienzo de la tarjeta del Reto: encima del inicio (0) y por debajo de Ajustes (40).</summary>
        public const int DailyCardSortingOrder = 5;

        [SerializeField]
        private TextAsset? dailyPool;

        private DailyChallengeService? _dailyService;
        private DailyChallengeScreen? _dailyScreen;
        private DailyChallengeCard? _dailyCard;
        private OverlayCanvas? _dailyCardCanvas;
        private DailyChallengeRunner? _dailyRunner;
        private IClock? _dailyClock;
        private DateTime _dailyCardDate;
        private bool _dailyCardShown;

        /// <summary>
        /// Último día del Reto cerrado sin victoria (<see cref="DailyChallengeService.DayEnded"/>: el <c>fail</c> del día, una vez y
        /// solo si no se ganó; R-Q15 a), o null. La sonda <c>AF_STATE daily_challenge</c> lo escribe al recibirlo (R10,
        /// <see cref="DevProbe.DailyDayEnd"/>).
        /// </summary>
        public DailyPlayEnd? LastDailyDayEnd { get; private set; }

        /// <summary>Pool del Reto para los tests (gana a <see cref="dailyPool"/>); <see cref="ResetDailyOverrides"/> lo limpia.</summary>
        internal static DailyPool? DailyPoolOverride { get; set; }

        /// <summary>
        /// Los tests fuerzan «sin pool» (el Reto fuera del build, D-008) aunque la escena traiga <see cref="dailyPool"/> (E3):
        /// gana a <see cref="DailyPoolOverride"/> y al pool de la escena; <see cref="ResetDailyOverrides"/> lo apaga.
        /// </summary>
        internal static bool DailyPoolDisabledOverride { get; set; }

        /// <summary>Almacén del Reto para los tests (gana a <see cref="DailyStoreFactory"/>).</summary>
        internal static IDailyChallengeStore? DailyStoreOverride { get; set; }

        /// <summary>Hora del servidor para los tests (por defecto, sin red: <see cref="FakeServerDate.Offline"/>).</summary>
        internal static IServerDate? DailyServerOverride { get; set; }

        /// <summary>
        /// Almacén del bloque <c>daily_challenge</c> sobre el perfil (enganche E4): por defecto <see cref="ProfileDailyChallengeStore"/>
        /// (<see cref="CreateProfileDailyStore"/>). Recibe el perfil y su almacén (el lote de guardado), los mismos que la cartera.
        /// </summary>
        public static Func<Profile, IProfileStore, IDailyChallengeStore>? DailyStoreFactory { get; set; } = CreateProfileDailyStore;

        /// <summary>
        /// E4: el registro del Reto en el bloque <c>daily_challenge</c> del mismo perfil que la cartera, guardado por la vía síncrona
        /// del lote (<see cref="ProfileSaveBatch.Durable"/>), la misma que la cartera del Reto: el registro de un cobro está en disco
        /// antes del pago, y si esa escritura falla, los dos salen juntos en la siguiente (<see cref="IDailyChallengeStore"/>).
        /// </summary>
        public static IDailyChallengeStore CreateProfileDailyStore(Profile profile, IProfileStore store)
        {
            return new ProfileDailyChallengeStore(profile, store is ProfileSaveBatch batch ? batch.Durable : store);
        }

        /// <summary>Pool del Reto de la escena (null hasta E3).</summary>
        public TextAsset? DailyPoolAsset => dailyPool;

        /// <summary>Asigna el pool del Reto (<c>daily_pool_v1.json</c>; lo llama <c>GameSceneBuilder</c> con el enganche E3).</summary>
        public void ConfigureDailyPool(TextAsset? pool)
        {
            dailyPool = pool;
        }

        /// <summary>Servicio del Reto (null antes de <see cref="SetUpDaily"/>).</summary>
        public DailyChallengeService? DailyService => _dailyService;

        /// <summary>Partida y calendario del Reto (null antes de <see cref="SetUpDaily"/>).</summary>
        public DailyChallengeScreen? DailyScreen => _dailyScreen;

        /// <summary>Tarjeta del inicio (null antes de <see cref="SetUpDaily"/>).</summary>
        public DailyChallengeCard? DailyCard => _dailyCard;

        /// <summary>Componente que avanza el Reto cada fotograma.</summary>
        public DailyChallengeRunner? DailyRunner => _dailyRunner;

        /// <summary>Limpia los ganchos de test del Reto.</summary>
        internal static void ResetDailyOverrides()
        {
            DailyPoolOverride = null;
            DailyPoolDisabledOverride = false;
            DailyStoreOverride = null;
            DailyServerOverride = null;
        }

        /// <summary>
        /// Crea el Reto con lo que ya construyó <c>Start</c> (perfil, lote de guardado, cartera, progreso, arte, anuncios y tienda) y
        /// el mismo reloj (<c>LaunchOptions.ClockOverride</c> o el del sistema). Al abrir la app descarta la partida de otro día
        /// (R-Q4, <see cref="DailyChallengeService.DiscardStale"/>). Devuelve false, sin crear nada, si aún no hay arranque. Una
        /// segunda llamada no hace nada y devuelve true.
        /// </summary>
        public bool SetUpDaily()
        {
            if (_dailyService != null)
            {
                return true;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            IProgressStore? progress = _progress;
            if (profile == null || batch == null || progress == null || _art == null || _app == null || _lobby == null)
            {
                Debug.LogError(LogPrefix + "SetUpDaily antes del arranque: el Reto del río no se crea.");
                return false;
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            _dailyClock = clock;
            IDailyChallengeStore? store = DailyStoreOverride ?? DailyStoreFactory?.Invoke(profile, batch);
            DailyPoolSource pool = CreateDailyPoolSource(); // memoria (2026-10-26): el pool se lee al abrir el Reto, no aquí
            if (store == null)
            {
                if (Debug.isDebugBuild)
                {
                    store = new InMemoryDailyChallengeStore();
                    if (pool.InBuild)
                    {
                        Debug.LogWarning(LogPrefix + "Reto del río con el registro en memoria (sin DailyStoreFactory): se pierde al cerrar la app.");
                    }
                }
                else
                {
                    store = new InMemoryDailyChallengeStore();
                    pool = new DailyPoolSource((DailyPool?)null); // release sin DailyStoreFactory: sin registro en el perfil no se puede pagar una sola vez por fecha; la tarjeta, oculta
                }
            }

            DailyChallengeConfig config = ActiveRcConfigs?.Daily ?? DailyChallengeConfig.Defaults; // Remote Config (B7)
            IServerDate server = DailyServerOverride ?? FakeServerDate.Offline();
            Wallet dailyWallet = LevelFlow.CreateDailyWallet(profile, batch.Durable, _capsClock ?? clock, ActiveRcConfigs?.Economy); // B8: anti-reloj de canoas y topes; las reglas, con el del dispositivo
            ConnectWallet(dailyWallet); // earn/spend del Reto por el bus y biz_feature_lives (B7; GameBootstrap.Telemetry.cs)
            var rules = new DailyChallengeRules(store, dailyWallet, clock, server, config);
            var service = DailyChallengeService.WithPoolSource(store, rules, clock, config, pool);
            service.DayEnded += OnDailyDayEnded; // antes del primer DiscardStale: el fail del día que se cierra al abrir (R-Q15 a)
            service.DiscardStale();
            DevProbe.DailyEarn(rules.Reconcile().SeedsPaid); // R10: semillas de los cobros pendientes que se confirman al abrir

            GameServices? services = _ads != null && _store != null && _storeLedger != null
                ? new GameServices(dailyWallet, _ads, _store, _storeLedger, GeoTierRule.FromLanguageTag(_deviceTag), purchasesBlocked: ProfileLocked, deviceLocale: _deviceLocale, settings: profile.Settings)
                : null;
            var context = new DailyPlayContext(
                store, clock, progress, dailyWallet, services, batch, profile.Settings,
                LaunchOptions.ResolveTapSwitchSelection(), LaunchOptions.ResolvePourMode());

            _dailyService = service;
            _dailyScreen = new DailyChallengeScreen(transform, _art, service, context, OnDailyClosed);
            _dailyScreen.LeavingVictory = leave => PrePromptHoldsLeave || TryPrePromptOnLeave(true, true, leave); // 06 §8.1 (GameBootstrap.PushUi.cs); con la tarjeta delante, la victoria espera
            _dailyScreen.Prepare = PrepareDaily; // B9 (GameBootstrap.A11y.cs): Reducir movimiento, Alto contraste y TalkBack
            _dailyCardCanvas = new OverlayCanvas("DailyCardCanvas", transform, DailyCardSortingOrder);
            _dailyCard = new DailyChallengeCard(_dailyCardCanvas.Root, _art, OnDailyCardTap);
            DailyChallengeRunner runner = gameObject.GetComponent<DailyChallengeRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<DailyChallengeRunner>();
            }

            _dailyRunner = runner;
            _dailyRunner.Bind(this);
            _lobby.DailyUnlocked = () => DailyPlayUnlocked; // «Jugar · Reto del río» tras acabar el contenido (D-032, punto 6)
            _dailyCardShown = false;
            RefreshDailyCard();
            return true;
        }

        /// <summary>
        /// Avanza el Reto <paramref name="dtMs"/> ms (lo llama <see cref="DailyChallengeRunner"/>): la partida y el calendario, y la
        /// tarjeta, que solo se ve sobre el inicio sin nada encima (ni Ajustes, el Mapa, la tienda o un anuncio) y se recalcula
        /// al volver a verse o al cambiar la fecha local. Sin asignaciones mientras nada cambia.
        /// </summary>
        public void TickDaily(float dtMs)
        {
            if (AccountDeleted)
            {
                return; // Nada tras «Eliminar cuenta y datos»: el perfil ya no existe (GameBootstrap.Account.cs).
            }

            DailyChallengeScreen? screen = _dailyScreen;
            DailyChallengeCard? card = _dailyCard;
            if (screen == null || card == null)
            {
                return;
            }

            screen.Tick(dtMs);
            bool lobby = !screen.IsOpen && _app != null && _app.Screen == AppScreen.Lobby && _lobby != null && _lobby.Visible
                && !_lobby.OverlayVisible && !HomeWindowCovered && (_settingsScreen == null || !_settingsScreen.Visible);
            bool dateChanged = _dailyClock != null && _dailyClock.LocalDate() != _dailyCardDate;
            if (lobby && (!_dailyCardShown || dateChanged))
            {
                RefreshDailyCard();
            }
            else if (_dailyCardCanvas != null && _dailyCardCanvas.Measure())
            {
                PlaceDailyCard();
            }

            _dailyCardShown = lobby;
            card.SetShown(lobby);
        }

        /// <summary>Recalcula la tarjeta (estado, fecha, banda y racha) y la coloca.</summary>
        public DailyCardState RefreshDailyCard()
        {
            if (_dailyService == null || _dailyCard == null || _dailyClock == null || _progress == null)
            {
                return DailyCardState.Hidden;
            }

            if (_dailyScreen == null || !_dailyScreen.IsOpen)
            {
                // Al volver al inicio, antes de pintar la tarjeta: la partida de otro día se descarta (R-Q4; contrato de la rama
                // spark/reto-del-rio-dec). Con una partida abierta no: la que cruza la medianoche sigue (R-Q3).
                _dailyService.DiscardStale();
            }

            _dailyCardDate = _dailyClock.LocalDate();
            DailyCardState state = _dailyCard.Refresh(_dailyService, _dailyClock, _progress.MaxLevel);
            _dailyCardCanvas?.Measure();
            PlaceDailyCard();
            return state;
        }

        /// <summary>Toque en la tarjeta (lo que hace el botón; los tests lo llaman igual).</summary>
        public bool PressDailyCard()
        {
            DailyChallengeCard? card = _dailyCard;
            if (card == null || !card.Visible)
            {
                return false;
            }

            return OpenDaily(card.State);
        }

        /// <summary>
        /// El Reto del río está en este build: <see cref="SetUpDaily"/> lo creó con un pool válido (E2 y E3). Sin él, ni la tarjeta
        /// del inicio, ni la presentación del 7 (<see cref="DailyTeaserCard"/>, E6), ni la línea de Sin canoas (E7): D-008.
        /// </summary>
        public bool DailyInBuild => _dailyService != null && _dailyService.HasPool;

        /// <summary>
        /// El Reto del río está desbloqueado: en el build (<see cref="DailyInBuild"/>), con su interruptor y desde
        /// <c>live_unlock_level_daily</c> (<see cref="DailyChallengeService.IsAvailable"/>). Con el contenido acabado, el botón Jugar
        /// del inicio lo abre (D-032, punto 6). Sin asignaciones.
        /// </summary>
        public bool DailyPlayUnlocked => _dailyService != null && _dailyScreen != null && _progress != null && _dailyService.IsAvailable(_progress.MaxLevel);

        // «Jugar · Reto del río» (D-032, punto 6): con el inicio a la vista, recalcula la tarjeta (descarta la partida de otro día,
        // R-Q4) y abre lo que abriría ella: el tablero de hoy (Jugar o Seguir) o, con el día sellado o pendiente, el calendario.
        private bool PressPlayDaily()
        {
            if (_dailyService == null || _progress == null || _app == null || _app.Screen != AppScreen.Lobby)
            {
                return false;
            }

            _dailyCardShown = false;
            RefreshDailyCard();
            return OpenDaily(_dailyService.CardState(_progress.MaxLevel));
        }

        /// <summary>
        /// Sin canoas ofrece «Mientras esperas: Jugar el Reto del río» (E7; 04 §7.3; 04 §7.1: el Reto no requiere canoas): el Reto
        /// está en el build y desbloqueado (nivel ≥ <c>live_unlock_level_daily</c>, con su interruptor) y hoy se puede jugar o
        /// seguir (tarjeta Jugar o Seguir). Con el día ya sellado o pendiente de validar no se ofrece [P]: la tarjeta abriría el
        /// calendario, no un tablero. Ni con el Reto ya abierto.
        /// </summary>
        public bool NoCanoesDailyAvailable
        {
            get
            {
                if (_dailyService == null || _dailyScreen == null || _progress == null || _dailyScreen.IsOpen)
                {
                    return false;
                }

                DailyCardState state = _dailyService.CardState(_progress.MaxLevel);
                return state == DailyCardState.Play || state == DailyCardState.InProgress;
            }
        }

        /// <summary>
        /// «Mientras esperas: Jugar el Reto del río» de Sin canoas (E7): registra <c>af_no_canoes_choice</c> con
        /// <c>choice = daily_challenge</c> (04 §11; 08 §8.2; con la sonda <c>AF_STATE no_canoes_choice</c>),
        /// cierra Sin canoas por el camino de «Volver al inicio» (<see cref="PressNoCanoesHome"/>: desde el inicio, al inicio;
        /// desde un nivel, sale al inicio como ese botón) y abre el tablero de hoy como la tarjeta. Devuelve si se abrió; con el
        /// Reto no disponible (<see cref="NoCanoesDailyAvailable"/>) o sin Sin canoas delante, no hace nada.
        /// </summary>
        public bool PressNoCanoesDaily()
        {
            if (!NoCanoesDailyAvailable || _app == null)
            {
                return false;
            }

            bool fromLobby = _app.Screen == AppScreen.NoCanoes;
            bool fromLevel = _flow != null && _flow.Phase == FlowPhase.NoCanoes;
            if (!fromLobby && !fromLevel)
            {
                return false;
            }

            DailyCardState state = _dailyService!.CardState(_progress!.MaxLevel);
            if (!PressNoCanoesHome())
            {
                return false;
            }

            // La elección se registra cuando Sin canoas ya se cerró por su camino (revisión de T1).
            DevProbe.DailyNoCanoesChoice();
            TrackNoCanoesChoice(DailyProbeFormat.NoCanoesDailyChoice); // af_no_canoes_choice (cierre de B11)

            _dailyCardShown = false;
            RefreshDailyCard();
            return OpenDaily(state);
        }

        /// <summary>Última elección de Sin canoas (<c>af_no_canoes_choice</c>: <c>wait</c>, <c>ad</c>, <c>seeds_refill</c>, <c>iap_infinite</c>, <c>daily_challenge</c> o <c>map</c>), o null (tests).</summary>
        public string? LastNoCanoesChoice { get; private set; }

        private void OnDailyDayEnded(DailyPlayEnd end)
        {
            LastDailyDayEnd = end;
            DevProbe.DailyDayEnd(end); // R10: AF_STATE daily_challenge result=fail, una vez por día (R-Q15)
        }

        private void OnDailyCardTap(DailyCardState state)
        {
            OpenDaily(state);
        }

        private bool OpenDaily(DailyCardState state)
        {
            DailyChallengeScreen? screen = _dailyScreen;
            if (screen == null || _progress == null || _lobby == null || _app == null || _app.Screen != AppScreen.Lobby)
            {
                return false;
            }

            switch (state)
            {
                case DailyCardState.Play:
                case DailyCardState.InProgress:
                    _lobby.Show(AppScreen.Level); // el inicio se oculta mientras se juega el Reto; AppFlow sigue en el inicio
                    _dailyCard?.SetShown(false);
                    int loads = _dailyService?.PoolSource.Loads ?? 0;
                    long openStart = System.Diagnostics.Stopwatch.GetTimestamp();
                    if (screen.Open(_progress.MaxLevel))
                    {
                        // AF_PERF mem daily: si esta apertura leyó el pool y lo que tardó la apertura entera (ms).
                        bool read = _dailyService != null && _dailyService.PoolSource.Loads != loads;
                        long openMs = (System.Diagnostics.Stopwatch.GetTimestamp() - openStart) * 1000L / System.Diagnostics.Stopwatch.Frequency;
                        DevProbe.Mem("daily", 0, DevProbe.Enabled
                            ? "\"pool_read\":" + (read ? "true" : "false") + ",\"open_ms\":" + openMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : null);
                        return true;
                    }

                    _lobby.Show(_app.Screen);

                    // D-008: un pool que resulta no válido al leerlo aquí (perezoso desde 2026-10-26; en release el CI lo valida antes)
                    // deja HasPool en false: la tarjeta se recalcula y queda oculta, como si el Reto no estuviera en el build.
                    _dailyCardShown = false;
                    RefreshDailyCard();
                    return false;
                case DailyCardState.Sealed:
                case DailyCardState.PendingValidation:
                    _dailyCard?.SetShown(false);
                    return screen.OpenCalendar();
                default:
                    return false;
            }
        }

        // Fin del Reto (salir, Inicio en el calendario): el inicio vuelve y la tarjeta se recalcula (sellada, racha…). El saldo del
        // inicio se lee del perfil, que es el mismo que el de la cartera del Reto.
        private void OnDailyClosed()
        {
            if (_lobby != null && _app != null && !_lobby.Visible)
            {
                _lobby.Show(_app.Screen);
            }

            _dailyCardShown = false;
            RefreshDailyCard();
            _dailyService?.ReleasePool(); // memoria (2026-10-26): los 2.187 tableros no se quedan hasta la siguiente partida
        }

        // El pool del Reto: el de los tests tal cual; el de la escena, leído al pedirlo (LoadDailyPool); sin pool, ninguno.
        private DailyPoolSource CreateDailyPoolSource()
        {
            if (DailyPoolDisabledOverride || (DailyPoolOverride == null && dailyPool == null))
            {
                return new DailyPoolSource((DailyPool?)null);
            }

            return DailyPoolOverride != null ? new DailyPoolSource(DailyPoolOverride) : new DailyPoolSource(LoadDailyPool);
        }

        // La del Reto va en la fila de las tarjetas del día del muelle (D-050, punto 6): la coloca PlaceDayCards con las demás.
        private void PlaceDailyCard()
        {
            _dayCardsDirty = true;
            PlaceDayCards();
        }

        private DailyPool? LoadDailyPool()
        {
            if (DailyPoolDisabledOverride)
            {
                return null;
            }

            if (DailyPoolOverride != null)
            {
                return DailyPoolOverride;
            }

            if (dailyPool == null)
            {
                return null; // sin E3: el Reto no existe en este build (D-008: tarjeta oculta)
            }

            try
            {
                return ReadData<DailyPool>(dailyPool, DailyPoolReader.Read);
            }
            catch (Exception e) when (e is LevelFormatException || e is FormatException || e is ArgumentException)
            {
                Debug.LogError(LogPrefix + "pool del Reto del río no válido (" + e.Message + "): la tarjeta queda oculta.");
                return null;
            }
        }
    }
}
