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

        /// <summary>Hueco entre la tarjeta y el botón «Jugar» (dp) [P].</summary>
        public const float DailyCardAbovePlayDp = 48f;

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
        private float _dailyCardW;
        private float _dailyCardH;

        /// <summary>
        /// Último día del Reto cerrado sin victoria (<see cref="DailyChallengeService.DayEnded"/>: el <c>fail</c> del día, una vez y
        /// solo si no se ganó; R-Q15 a), o null. La sonda <c>AF_STATE daily_challenge</c> lo escribe al recibirlo (R10,
        /// <see cref="DevProbe.DailyDayEnd"/>).
        /// </summary>
        public DailyPlayEnd? LastDailyDayEnd { get; private set; }

        /// <summary>Pool del Reto para los tests (gana a <see cref="dailyPool"/>); <see cref="ResetDailyOverrides"/> lo limpia.</summary>
        internal static DailyPool? DailyPoolOverride { get; set; }

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
            DailyPool? pool = LoadDailyPool();
            if (store == null)
            {
                if (Debug.isDebugBuild)
                {
                    store = new InMemoryDailyChallengeStore();
                    if (pool != null)
                    {
                        Debug.LogWarning(LogPrefix + "Reto del río con el registro en memoria (sin DailyStoreFactory): se pierde al cerrar la app.");
                    }
                }
                else
                {
                    store = new InMemoryDailyChallengeStore();
                    pool = null; // release sin DailyStoreFactory: sin registro en el perfil no se puede pagar una sola vez por fecha; la tarjeta, oculta
                }
            }

            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            IServerDate server = DailyServerOverride ?? FakeServerDate.Offline();
            Wallet dailyWallet = LevelFlow.CreateDailyWallet(profile, batch.Durable, clock);
            var rules = new DailyChallengeRules(store, dailyWallet, clock, server, config);
            var service = new DailyChallengeService(store, rules, clock, config, pool);
            service.DayEnded += OnDailyDayEnded; // antes del primer DiscardStale: el fail del día que se cierra al abrir (R-Q15 a)
            service.DiscardStale();
            DevProbe.DailyEarn(rules.Reconcile().SeedsPaid); // R10: semillas de los cobros pendientes que se confirman al abrir

            GameServices? services = _ads != null && _store != null && _storeLedger != null
                ? new GameServices(dailyWallet, _ads, _store, _storeLedger, GeoTier.T3, purchasesBlocked: ProfileLocked, deviceLocale: _deviceLocale, settings: profile.Settings)
                : null;
            var context = new DailyPlayContext(
                store, clock, progress, dailyWallet, services, batch, profile.Settings,
                LaunchOptions.ResolveTapSwitchSelection(), LaunchOptions.ResolvePourMode());

            _dailyService = service;
            _dailyScreen = new DailyChallengeScreen(transform, _art, service, context, OnDailyClosed);
            _dailyCardCanvas = new OverlayCanvas("DailyCardCanvas", transform, DailyCardSortingOrder);
            _dailyCard = new DailyChallengeCard(_dailyCardCanvas.Root, _art, OnDailyCardTap);
            DailyChallengeRunner runner = gameObject.GetComponent<DailyChallengeRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<DailyChallengeRunner>();
            }

            _dailyRunner = runner;
            _dailyRunner.Bind(this);
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
                    if (screen.Open(_progress.MaxLevel))
                    {
                        return true;
                    }

                    _lobby.Show(_app.Screen);
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
        }

        private void PlaceDailyCard()
        {
            if (_dailyCard == null || _dailyCardCanvas == null)
            {
                return;
            }

            float w = _dailyCardCanvas.Width;
            float h = _dailyCardCanvas.Height;
            if (Mathf.Approximately(w, _dailyCardW) && Mathf.Approximately(h, _dailyCardH) && _dailyCard.Root.sizeDelta.x > 0f)
            {
                return;
            }

            _dailyCardW = w;
            _dailyCardH = h;
            float y = (h * LobbyScreen.PlayCenterFraction) - DailyCardAbovePlayDp - DailyChallengeCard.HeightDp - 32f;
            _dailyCard.Place((w - DailyChallengeCard.WidthDp) * 0.5f, Mathf.Max(96f, y));
        }

        private DailyPool? LoadDailyPool()
        {
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
                return DailyPoolReader.Read(dailyPool.text);
            }
            catch (Exception e) when (e is LevelFormatException || e is FormatException || e is ArgumentException)
            {
                Debug.LogError(LogPrefix + "pool del Reto del río no válido (" + e.Message + "): la tarjeta queda oculta.");
                return null;
            }
        }
    }
}
