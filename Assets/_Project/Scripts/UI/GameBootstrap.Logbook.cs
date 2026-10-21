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
    /// Arranque de la Bitácora del río (B4; 06 §4.2, §2.3 y §4.7; D-033, añadido del 2026-10-20, puntos 1-7 y 11):
    /// <see cref="SetUpLogbook"/> crea el servicio (<see cref="LoginCalendarService"/>), la libreta (<see cref="LogbookScreen"/>),
    /// la tarjeta con punto del inicio (<see cref="LogbookCard"/>, en un lienzo propio sobre la del Reto [P]) y la cola de takeovers
    /// (<see cref="TakeoverQueue"/>), con el perfil, la cartera, el progreso y el reloj que ya creó <c>Start</c>, y añade el
    /// <see cref="LogbookRunner"/> que los avanza. Parcial nuevo: <c>GameBootstrap.cs</c> no se toca.
    /// <list type="bullet">
    /// <item><b>B4-3-bootstrap</b> (lo aplica el integrador): llamar a <see cref="SetUpLogbook"/> en <c>Start</c>, tras
    /// <c>SetUpStreak();</c>, y en la secuencia del Mapa del 10 abrir antes la libreta (<see cref="OpenLogbookBeforeL10Map"/>:
    /// Victoria → libreta → Mapa → Inicio, D-033, punto 11). Sin esa llamada no hay Bitácora (D-008: oculta mientras no exista).</item>
    /// <item><b>B4-1-profile</b>: el bloque <c>calendar</c> del perfil (<c>ProfileLoginCalendarStore</c>) se conecta con
    /// <see cref="LogbookStoreFactory"/>. Sin él, en desarrollo se usa un almacén en memoria (se pierde al cerrar la app) y en
    /// release la Bitácora queda oculta.</item>
    /// <item><b>B4-2-flow</b>: <see cref="LevelFlow.LevelPlayed"/> (fin de nivel de campaña o del Reto, ganado o con fallo duro)
    /// apunta el día jugado (<see cref="LoginCalendarService.MarkPlayed"/>). El flujo del Reto se suscribe al vuelo
    /// (<see cref="DailyChallengeScreen.Flow"/>), sin tocar su pantalla.</item>
    /// </list>
    /// La fila «Bitácora · elige tu premio» del panel de la victoria es de B1b (Q-B11): su gancho es
    /// <see cref="LogbookVictoryRowDue"/> y <see cref="OpenLogbookFromVictory"/>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Orden del lienzo de la tarjeta de la Bitácora: el de la del Reto (encima del inicio, debajo de Ajustes).</summary>
        public const int LogbookCardSortingOrder = DailyCardSortingOrder;

        /// <summary>Hueco entre la tarjeta de la Bitácora y la del Reto (dp) [P].</summary>
        public const float LogbookCardGapDp = 8f;

        /// <summary>
        /// Nivel desde el que la Bitácora puede dar una Luciérnaga: el default de <c>lvl_murky_from_level</c> = 100 (08 §9), el
        /// estreno de Aguas turbias, donde se regala la primera (04 §2.5). Antes, la Luciérnaga se sustituye (D-033, punto 3;
        /// Q-B5). El cliente no declara aún esa clave (la otra copia, <c>DevBench.MurkyFromLevel</c>, solo existe en desarrollo):
        /// cuando llegue <c>AF.RemoteConfig</c>, las dos leen su valor (Q-B5).
        /// </summary>
        public const int LogbookRevealUnlockLevel = 100;

        /// <summary>Origen de la sonda <c>takeover</c> al entrar al inicio.</summary>
        public const string TakeoverSourceLobby = "lobby";

        // La reconstrucción por idioma (RebuildUi) recarga la escena con el mismo perfil en memoria: no es una sesión nueva, así
        // que la cola (su cupo y lo visto) pasa a la escena nueva, como las sesiones de B6.
        private static Profile? s_takeoversProfile;
        private static TakeoverQueue? s_carriedTakeovers;

        private LoginCalendarService? _logbook;
        private LogbookScreen? _logbookScreen;
        private LogbookCard? _logbookCard;
        private OverlayCanvas? _logbookCardCanvas;
        private TakeoverQueue? _takeovers;
        private LogbookRunner? _logbookRunner;
        private IClock? _logbookClock;
        private LevelFlow? _logbookDailyFlow;
        private Action? _logbookAfterClose;
        private string _logbookDay = string.Empty;
        private bool _logbookLobbyShown;
        private bool _logbookFailed;
        private bool _logbookCardShown;
        private float _logbookCardW;
        private float _logbookCardH;

        /// <summary>Almacén de la Bitácora para los tests (gana a <see cref="LogbookStoreFactory"/>).</summary>
        internal static ILoginCalendarStore? LogbookStoreOverride { get; set; }

        /// <summary>Cifras de la Bitácora para los tests (por defecto, <see cref="LoginCalendarConfig.Defaults"/>).</summary>
        internal static LoginCalendarConfig? LogbookConfigOverride { get; set; }

        /// <summary>
        /// Los tests apagan la Bitácora con false (<see cref="SetUpLogbook"/> no crea nada): los que no tratan de ella entran al
        /// inicio sin la libreta encima, como antes de que existiera (<c>PlayModeHelpers.NoLogbookUnlessSet</c>, parche
        /// B4-3-bootstrap). Null o true: como en el juego. Los que la prueban la encienden y la vuelven a null en su TearDown.
        /// </summary>
        internal static bool? LogbookOverride { get; set; }

        /// <summary>
        /// Almacén del bloque <c>calendar</c> sobre el perfil (B4-1-profile): recibe el perfil y su almacén (el lote de guardado),
        /// los mismos que la cartera. Por defecto, <see cref="CreateProfileLogbookStore"/>.
        /// </summary>
        public static Func<Profile, IProfileStore, ILoginCalendarStore>? LogbookStoreFactory { get; set; } = CreateProfileLogbookStore;

        /// <summary>
        /// B4-1-profile: la Bitácora en el bloque <c>calendar</c> del mismo perfil que la cartera, guardada por la vía síncrona del
        /// lote (<see cref="ProfileSaveBatch.Durable"/>), como el Reto: el avance de una casilla está en disco antes del pago, y si
        /// esa escritura falla, los dos salen juntos en la siguiente (<see cref="ILoginCalendarStore"/>).
        /// </summary>
        public static ILoginCalendarStore CreateProfileLogbookStore(Profile profile, IProfileStore store)
        {
            return new ProfileLoginCalendarStore(profile, store is ProfileSaveBatch batch ? batch.Durable : store);
        }

        /// <summary>Servicio de la Bitácora (null antes de <see cref="SetUpLogbook"/>).</summary>
        public LoginCalendarService? Logbook => _logbook;

        /// <summary>La libreta (null antes de <see cref="SetUpLogbook"/>).</summary>
        public LogbookScreen? LogbookScreen => _logbookScreen;

        /// <summary>La tarjeta del inicio (null antes de <see cref="SetUpLogbook"/>).</summary>
        public LogbookCard? LogbookCard => _logbookCard;

        /// <summary>Componente que avanza la Bitácora cada fotograma (null antes de <see cref="SetUpLogbook"/>).</summary>
        public LogbookRunner? LogbookRunner => _logbookRunner;

        /// <summary>La cola de takeovers (null antes de <see cref="SetUpLogbook"/>).</summary>
        public TakeoverQueue? Takeovers => _takeovers;

        /// <summary>Último takeover decidido al entrar al inicio (tests y sonda).</summary>
        public TakeoverKind LastTakeover { get; private set; }

        /// <summary>Limpia los ganchos de test de la Bitácora.</summary>
        internal static void ResetLogbookOverrides()
        {
            LogbookStoreOverride = null;
            LogbookConfigOverride = null;
        }

        /// <summary>
        /// Crea la Bitácora con lo que ya construyó <c>Start</c> (perfil, lote, cartera, progreso, arte y flujo) y el mismo reloj
        /// (<c>LaunchOptions.ClockOverride</c> o el del sistema). Devuelve false, sin crear nada, si aún no hay arranque o si es un
        /// build de release sin el bloque del perfil (B4-1-profile). Una segunda llamada no hace nada y devuelve true.
        /// </summary>
        public bool SetUpLogbook()
        {
            if (_logbook != null)
            {
                return true;
            }

            if (LogbookOverride == false || _logbookFailed)
            {
                return false;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            if (profile == null || batch == null || _progress == null || _wallet == null || _art == null || _app == null || _lobby == null)
            {
                Debug.LogError(LogPrefix + "SetUpLogbook antes del arranque: la Bitácora del río no se crea.");
                return false;
            }

            ILoginCalendarStore? store = LogbookStoreOverride ?? LogbookStoreFactory?.Invoke(profile, batch);
            if (store == null)
            {
                if (!Debug.isDebugBuild)
                {
                    return false; // sin el bloque calendar no se puede cobrar una sola vez por casilla: la Bitácora no existe
                }

                store = new InMemoryLoginCalendarStore();
                Debug.LogWarning(LogPrefix + "Bitácora del río con el registro en memoria (sin LogbookStoreFactory): se pierde al cerrar la app.");
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            LoginCalendarService service;
            try
            {
                // B4: las cifras de la Bitácora de Remote Config (RcConfigBinder, F3-5); los tests las fijan con LogbookConfigOverride.
                service = new LoginCalendarService(store, _wallet, clock, DailyChallengeConfig.Defaults.LiveUnlockLevelDaily, LogbookConfigOverride ?? _rcConfigs?.LoginCalendar);
            }
            catch (Exception e) when (e is InvalidOperationException || e is OverflowException)
            {
                // Una tabla y una tasa que no cuadran (o que desbordan) (por ejemplo, una tasa nueva de Remote Config sin su tabla): la Bitácora no
                // existe en esta sesión, pero el arranque sigue (SetUpMeta y el resto van después).
                Debug.LogError(LogPrefix + "Bitácora del río apagada: " + e.Message);
                _logbookFailed = true; // una vez por escena: otra llamada no lo vuelve a intentar ni a registrar
                return false;
            }

            _logbookClock = clock;
            _logbook = service;
            _logbookScreen = new LogbookScreen(transform, _art, OnLogbookChoose, OnLogbookClose);
            _logbookCardCanvas = new OverlayCanvas("LogbookCardCanvas", transform, LogbookCardSortingOrder);
            _logbookCard = new LogbookCard(_logbookCardCanvas.Root, _art, () => OpenLogbook(LogbookOpenSource.Card));
            bool sameSession = ReferenceEquals(s_takeoversProfile, profile) && s_carriedTakeovers != null;
            _takeovers = sameSession ? s_carriedTakeovers! : new TakeoverQueue(_rcConfigs?.Takeovers); // ui_max_takeovers_per_session (B4)
            s_takeoversProfile = profile;
            s_carriedTakeovers = _takeovers;
            _takeovers.Register(TakeoverKind.Logbook, LogbookPending); // la función de esta escena, no la de la anterior
            if (_flow != null)
            {
                _flow.LevelPlayed += OnLevelPlayedLogbook;
            }

            LogbookRunner runner = gameObject.GetComponent<LogbookRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<LogbookRunner>();
            }

            _logbookRunner = runner;
            _logbookRunner.Bind(this);
            _logbookDay = clock.LocalDateKey();
            _logbookLobbyShown = false;
            _logbookCardShown = false;
            DevProbe.LogbookState(LoginCalendarProbeFormat.CauseStart, LogbookState, _logbook.Record);
            return true;
        }

        /// <summary>Estado de la Bitácora hoy para el jugador (Oculta antes de <see cref="SetUpLogbook"/>). Sin asignaciones.</summary>
        public LoginCalendarState LogbookState => _logbook != null && _progress != null ? _logbook.StateFor(_progress.MaxLevel) : LoginCalendarState.Hidden;

        /// <summary>
        /// La victoria de hoy lleva la fila «Bitácora · elige tu premio» (D-033, punto 5): la casilla de hoy se puede sellar. Gancho
        /// para el panel de B1b (Q-B11). Sin asignaciones.
        /// </summary>
        public bool LogbookVictoryRowDue => LogbookState == LoginCalendarState.Ready;

        /// <summary>
        /// La fila «Bitácora · elige tu premio» de la victoria (D-033, punto 5): abre la libreta encima y, al cerrarla, vuelve a la
        /// victoria. No gasta el cupo de takeovers (no es uno), pero ya no sale sola en esta sesión.
        /// </summary>
        public bool OpenLogbookFromVictory()
        {
            return OpenLogbook(LogbookOpenSource.Victory);
        }

        /// <summary>
        /// Pantalla 2 de la secuencia del nivel 10 (06 §2.3; D-033, punto 11: Victoria → libreta → Mapa → Inicio): con la casilla
        /// de hoy lista, abre la libreta con «Cada día que juegues, sella una página» y «Seguir», y al cerrarla abre el Mapa del 10.
        /// Devuelve false (y quien llama abre el Mapa en el acto) si la Bitácora no existe o no hay casilla que sellar.
        /// </summary>
        public bool OpenLogbookBeforeL10Map()
        {
            if (LogbookState != LoginCalendarState.Ready)
            {
                return false;
            }

            if (!OpenLogbook(LogbookOpenSource.L10))
            {
                return false;
            }

            _logbookAfterClose = () => OpenRiverMap(MapOpenSource.L10);
            return true;
        }

        /// <summary>
        /// Abre la libreta desde <paramref name="source"/>. Con <see cref="LogbookOpenSource.Takeover"/> gasta el cupo de la sesión;
        /// con el resto solo la marca como vista (ya no sale sola en esta sesión; Q-B9). False si la Bitácora no existe para el
        /// jugador o ya está abierta.
        /// </summary>
        public bool OpenLogbook(LogbookOpenSource source)
        {
            LoginCalendarService? service = _logbook;
            LogbookScreen? screen = _logbookScreen;
            if (service == null || screen == null || _progress == null || _takeovers == null || screen.Visible)
            {
                return false;
            }

            int maxLevel = _progress.MaxLevel;
            if (!service.IsAvailable(maxLevel))
            {
                return false;
            }

            _logbookAfterClose = null;
            screen.Show(service.Peek(maxLevel, LogbookUnlocks(maxLevel), LogbookWorld(maxLevel)), source);
            _logbookCard?.SetShown(false);
            if (source == LogbookOpenSource.Takeover)
            {
                _takeovers.MarkShown(TakeoverKind.Logbook);
            }
            else
            {
                _takeovers.MarkSeen(TakeoverKind.Logbook);
            }

            return true;
        }

        /// <summary>
        /// Avanza la Bitácora <paramref name="dtMs"/> ms (lo llama <see cref="LogbookRunner"/>): la libreta, el día local, la
        /// suscripción al flujo del Reto, el takeover al entrar al inicio y la tarjeta. Sin asignaciones mientras nada cambia.
        /// </summary>
        public void TickLogbook(float dtMs)
        {
            LoginCalendarService? service = _logbook;
            LogbookScreen? screen = _logbookScreen;
            if (service == null || screen == null || _logbookClock == null)
            {
                return;
            }

            screen.Tick(dtMs);
            TrackDailyFlow();

            string today = _logbookClock.LocalDateKey();
            bool dayChanged = !string.Equals(today, _logbookDay, StringComparison.Ordinal);
            if (dayChanged)
            {
                _logbookDay = today;
                DevProbe.LogbookState(LoginCalendarProbeFormat.CauseDay, LogbookState, service.Record);
                if ((screen.Choosing || screen.TodayPulsing) && _progress != null)
                {
                    // Medianoche con la libreta abierta y una casilla por sellar: la de hoy (sin las tarjetas de ayer ni su casilla
                    // pulsando). Con lo recibido a la vista no se rehace: el jugador ve qué le tocó.
                    int maxLevel = _progress.MaxLevel;
                    screen.Show(service.Peek(maxLevel, LogbookUnlocks(maxLevel), LogbookWorld(maxLevel)), screen.Source);
                }
            }

            bool lobby = LogbookLobbyClear && !screen.Visible;
            if (lobby && !_logbookLobbyShown)
            {
                TakeoverAtLobby();
            }

            _logbookLobbyShown = lobby;
            bool cardOn = lobby && !screen.Visible;
            if (cardOn && (!_logbookCardShown || dayChanged))
            {
                RefreshLogbookCard();
            }
            else if (_logbookCardCanvas != null && _logbookCardCanvas.Measure())
            {
                PlaceLogbookCard();
            }

            _logbookCardShown = cardOn;
            _logbookCard?.SetShown(cardOn);
        }

        /// <summary>La app pasa a segundo plano (true) o vuelve (false): la sesión de la cola de takeovers (Q-M4 (a)).</summary>
        public void LogbookPause(bool paused)
        {
            if (_takeovers == null || _logbookClock == null)
            {
                return;
            }

            if (paused)
            {
                _takeovers.OnBackground(_logbookClock.UtcNow);
            }
            else if (_takeovers.OnForeground(_logbookClock.UtcNow))
            {
                _logbookLobbyShown = false; // vuelta tras ≥ 30 min: la próxima vista del inicio es una entrada de la sesión nueva
            }
        }

        /// <summary>Recalcula la tarjeta del inicio (estado y punto) y la coloca.</summary>
        public LoginCalendarState RefreshLogbookCard()
        {
            LoginCalendarState state = LogbookState;
            _logbookCard?.Refresh(state);
            _logbookCardCanvas?.Measure();
            _logbookCardW = 0f;
            PlaceLogbookCard();
            return state;
        }

        /// <summary>
        /// Atrás de Android con la libreta abierta (lo llama <c>PressAndroidBack</c> con B4-3-bootstrap): como «Cerrar» o «Seguir»
        /// (en la secuencia del 10, lleva al Mapa). False si la libreta no está a la vista.
        /// </summary>
        public bool PressLogbookBack()
        {
            if (_logbookScreen == null || !_logbookScreen.Visible)
            {
                return false;
            }

            OnLogbookClose();
            return true;
        }

        /// <summary>Toque en la tarjeta (lo que hace su botón; los tests lo llaman igual).</summary>
        public bool PressLogbookCard()
        {
            LogbookCard? card = _logbookCard;
            return card != null && card.Visible && OpenLogbook(LogbookOpenSource.Card);
        }

        // El inicio está a la vista y sin nada encima: ni Ajustes, el Mapa, la tienda, un anuncio, Sin canoas ni el Reto.
        private bool LogbookLobbyClear => _app != null && _app.Screen == AppScreen.Lobby && _lobby != null && _lobby.Visible
            && !_lobby.OverlayVisible && !HomeWindowCovered && (_settingsScreen == null || !_settingsScreen.Visible)
            && (_dailyScreen == null || !_dailyScreen.IsOpen);

        // La Bitácora pendiente para la cola (06 §4.7): la casilla de hoy se puede sellar. Sin asignaciones (método, no lambda).
        private bool LogbookPending()
        {
            return LogbookState == LoginCalendarState.Ready;
        }

        // Al entrar al inicio (D-033, punto 11): la secuencia del 10 tiene prioridad (la abre OnAppScreenChanged con B4-3); si no,
        // el takeover de más prioridad pendiente, como mucho uno por sesión.
        private void TakeoverAtLobby()
        {
            TakeoverQueue? queue = _takeovers;
            if (queue == null || (_app != null && _app.L10MapDue))
            {
                return;
            }

            TakeoverKind kind = queue.Peek();
            bool shown = kind == TakeoverKind.Logbook && OpenLogbook(LogbookOpenSource.Takeover);
            LastTakeover = shown ? kind : TakeoverKind.None;
            DevProbe.LogbookTakeover(kind, shown, queue, TakeoverSourceLobby);
        }

        private void OnLogbookChoose(LoginCalendarChoice choice)
        {
            LoginCalendarService? service = _logbook;
            LogbookScreen? screen = _logbookScreen;
            if (service == null || screen == null || _progress == null)
            {
                return;
            }

            int maxLevel = _progress.MaxLevel;
            LoginCalendarUnlocks unlocks = LogbookUnlocks(maxLevel);
            int world = LogbookWorld(maxLevel);
            LoginCalendarClaim? claim;
            ProfileSaveBatch? batch = _saveBatch;
            if (batch != null)
            {
                // D-033, punto 6: el avance de next_day y el cobro, en la misma escritura del perfil (síncrona, como un fin de nivel).
                using (batch.Begin())
                {
                    batch.RequireDurable();
                    claim = service.Claim(choice, maxLevel, unlocks, world);
                }
            }
            else
            {
                claim = service.Claim(choice, maxLevel, unlocks, world);
            }

            if (claim == null)
            {
                // Ya no se puede sellar (cambió el día local con las tarjetas abiertas): la libreta vuelve a su estado de hoy.
                screen.Show(service.Peek(maxLevel, unlocks, world), screen.Source);
                return;
            }

            if (!claim.AllApplied)
            {
                Debug.LogError(LogPrefix + "Bitácora: la cartera rechazó parte del premio de la casilla " + claim.Day + ".");
            }

            DevProbe.LogbookClaim(claim);
            DevProbe.LogbookState(LoginCalendarProbeFormat.CauseClaim, LogbookState, service.Record);
            screen.ShowClaimed(claim, service.Peek(maxLevel, unlocks, world));
        }

        private void OnLogbookClose()
        {
            _logbookScreen?.Hide();
            Action? after = _logbookAfterClose;
            _logbookAfterClose = null;
            _logbookCardShown = false;
            _logbookLobbyShown = true; // cerrar la libreta no es una entrada nueva al inicio
            after?.Invoke();
        }

        private void OnLevelPlayedLogbook(LevelPlayedInfo info)
        {
            LoginCalendarService? service = _logbook;
            if (service != null && service.MarkPlayed())
            {
                DevProbe.LogbookState(LoginCalendarProbeFormat.CausePlayed, LogbookState, service.Record);
            }
        }

        // El flujo del Reto lo crea su pantalla al abrir una partida: se suscribe al vuelo (B4-2-flow, sin tocar la pantalla).
        private void TrackDailyFlow()
        {
            LevelFlow? current = _dailyScreen?.Flow;
            if (ReferenceEquals(current, _logbookDailyFlow))
            {
                return;
            }

            if (_logbookDailyFlow != null)
            {
                _logbookDailyFlow.LevelPlayed -= OnLevelPlayedLogbook;
            }

            _logbookDailyFlow = current;
            if (current != null)
            {
                current.LevelPlayed += OnLevelPlayedLogbook;
            }
        }

        // Lo que el jugador puede recibir ya (D-033, punto 3): un comodín cuyo nivel de desbloqueo ya superó (max_level ≥ nivel
        // [P], Q-B5). Así la Bitácora nunca adelanta un comodín a su estreno ni a su regalo (Morpho en el 12, la Luciérnaga en el 100,
        // que solo se regala sin ninguna en el stock: 04 §2.5).
        private static LoginCalendarUnlocks LogbookUnlocks(int maxLevel)
        {
            BoosterConfig boosters = BoosterConfig.Defaults;
            return LoginCalendarUnlocks.AtLevel(maxLevel, boosters.HintUnlockLevel, LogbookRevealUnlockLevel, boosters.ExtraCuiaUnlockLevel);
        }

        // El mundo donde está el jugador (06 §4.2, regla 4): el del nivel que va a jugar.
        private static int LogbookWorld(int maxLevel)
        {
            int level = Mathf.Clamp(maxLevel + 1, 1, WorldTable.LastLevel);
            return WorldTable.WorldOf(level);
        }

        private void PlaceLogbookCard()
        {
            if (_logbookCard == null || _logbookCardCanvas == null)
            {
                return;
            }

            float w = _logbookCardCanvas.Width;
            float h = _logbookCardCanvas.Height;
            if (Mathf.Approximately(w, _logbookCardW) && Mathf.Approximately(h, _logbookCardH))
            {
                return;
            }

            _logbookCardW = w;
            _logbookCardH = h;

            // Encima de la tarjeta del Reto (la misma cuenta que PlaceDailyCard) [P]; sin ella, en su sitio.
            float daily = Mathf.Max(96f, (h * LobbyScreen.PlayCenterFraction) - DailyCardAbovePlayDp - DailyChallengeCard.HeightDp - 32f);
            bool dailyShown = _dailyCard != null && _dailyCard.State != DailyCardState.Hidden;
            float y = dailyShown ? daily - LogbookCardGapDp - LogbookCard.HeightDp : daily + DailyChallengeCard.HeightDp - LogbookCard.HeightDp;
            _logbookCard.Place((w - LogbookCard.WidthDp) * 0.5f, Mathf.Max(72f, y));
        }
    }
}
