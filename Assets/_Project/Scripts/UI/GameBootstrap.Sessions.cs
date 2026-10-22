#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Arranque de B6 (nota <c>docs/dev/sesiones-notificaciones.md</c>): sesiones (<c>stats.sessions</c>, Q-M4 (a) de D-033), Regalo
    /// de regreso (06 §9), pre-prompt de push (06 §8.1) y avisos locales (06 §8.2-§8.3). <see cref="SetUpSessions"/> crea los
    /// servicios de <c>AF.LiveOps</c> con el perfil, la cartera y el reloj que ya creó <c>Start</c>, cuenta el arranque en frío y
    /// añade <see cref="SessionRunner"/>, que lleva el segundo plano, el cierre y las victorias. Parcial nuevo: <c>GameBootstrap.cs</c>
    /// no se toca en la rama.
    /// <list type="bullet">
    /// <item><b>B6-profile</b> (parche del integrador): <c>settings.push</c> y <c>stats.last_seen_at</c> / <c>last_win_at</c> en el
    /// perfil, con <c>ProfileSessionStore</c> y <c>ProfilePushSettingsStore</c>.</item>
    /// <item><b>B6-bootstrap</b> (después de B6-profile): la llamada a <see cref="SetUpSessions"/> en <c>Start</c>, tras
    /// <c>SetUpStreak();</c>, y <c>GameBootstrap.SessionsProfile.cs</c>, que implementa <see cref="CreateProfileSessionStores"/>.</item>
    /// <item><b>B6-notifications-real</b> (aparte; con el paquete <c>com.unity.mobile.notifications</c>, Q-M5): implementa
    /// <see cref="CreatePlatformScheduler"/> con el programador de Android. Sin él, <see cref="FakeNotificationScheduler"/>.</item>
    /// </list>
    /// Sin B6-profile: en desarrollo, almacenes en memoria (se pierden al cerrar la app); en release no hay sesiones ni avisos. El
    /// pre-prompt es la tarjeta real (<see cref="PushPrePromptCard"/>; el simulado, <see cref="SimulatedPushPrePrompt"/>, solo con
    /// <see cref="PrePromptOverride"/>): se apunta al entrar en la victoria y sale al pulsar Siguiente o Inicio (B6-Q16 (a);
    /// <c>GameBootstrap.PushUi.cs</c>). Las cifras son las de 08 §9 (<see cref="PushConfig.Defaults"/> y <see cref="ComebackConfig.Defaults"/>).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private SessionTracker? _sessions;
        private ISessionStore? _sessionStore;
        private ComebackService? _comebackService;
        private PushPermissionService? _push;
        private NotificationService? _notifications;
        private INotificationScheduler? _notificationScheduler;
        private IPushPrePromptPresenter? _prePrompt;
        private SessionRunner? _sessionRunner;
        private IClock? _sessionClock;
        private ComebackOutcome? _lastComeback;
        private FlowPhase _campaignPhaseSeen;
        private FlowPhase _dailyPhaseSeen;
        private readonly List<string> _sessionProbeLines = new List<string>(4);

        // La reconstrucción por idioma (RebuildUi) recarga la escena con el mismo perfil en memoria: no es un arranque en frío y no
        // cuenta otra sesión. El contador y su almacén siguen vivos de una escena a otra con ese perfil.
        private static Profile? s_sessionsProfile;
        private static SessionTracker? s_carriedSessions;
        private static ISessionStore? s_carriedSessionStore;

        /// <summary>Almacén de sesiones para los tests (gana al del perfil); <see cref="ResetSessionOverrides"/> lo limpia.</summary>
        internal static ISessionStore? SessionStoreOverride { get; set; }

        /// <summary>Almacén de <c>settings.push</c> para los tests.</summary>
        internal static IPushSettingsStore? PushSettingsStoreOverride { get; set; }

        /// <summary>Programador de avisos para los tests (gana al real y al falso por defecto).</summary>
        internal static INotificationScheduler? NotificationSchedulerOverride { get; set; }

        /// <summary>Pre-prompt para los tests (gana a la tarjeta real, <see cref="PushPrePromptCard"/>).</summary>
        internal static IPushPrePromptPresenter? PrePromptOverride { get; set; }

        /// <summary>Hora del servidor para el anti-reloj del regalo (por defecto, sin red).</summary>
        internal static IServerDate? SessionServerOverride { get; set; }

        /// <summary>Contador de sesiones (null antes de <see cref="SetUpSessions"/>, o en release sin B6-profile).</summary>
        public SessionTracker? Sessions => _sessions;

        /// <summary><c>settings.push</c> y el permiso (para Ajustes; null como <see cref="Sessions"/>).</summary>
        public PushPermissionService? Push => _push;

        /// <summary>Avisos locales (null como <see cref="Sessions"/>).</summary>
        public NotificationService? Notifications => _notifications;

        /// <summary>Lo que pasó con el Regalo de regreso en la última sesión (para el aviso de la UI), o null.</summary>
        public ComebackOutcome? LastComeback => _lastComeback;

        /// <summary>Limpia los ganchos de test de B6.</summary>
        internal static void ResetSessionOverrides()
        {
            SessionStoreOverride = null;
            PushSettingsStoreOverride = null;
            NotificationSchedulerOverride = null;
            PrePromptOverride = null;
            SessionServerOverride = null;
            s_sessionsProfile = null;
            s_carriedSessions = null;
            s_carriedSessionStore = null;
            ResetDevPush();
            PrePromptCardOverride = null;
            OpenPhoneSettingsOverride = null;
            s_prePromptSession = 0;
        }

        /// <summary>
        /// Crea los servicios de B6 y cuenta el arranque en frío (con su Regalo de regreso y la recogida de avisos). Devuelve false,
        /// sin crear nada, antes del arranque o en release sin almacenes en el perfil. Una segunda llamada no hace nada. Tras la
        /// reconstrucción por idioma (el mismo perfil en memoria) sigue la misma sesión: no cuenta otra.
        /// </summary>
        public bool SetUpSessions()
        {
            if (_sessions != null)
            {
                return true;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            Wallet? wallet = _wallet;
            if (profile == null || batch == null || wallet == null || _flow == null || _progress == null)
            {
                Debug.LogError(LogPrefix + "SetUpSessions antes del arranque: las sesiones no se crean.");
                return false;
            }

            if (!CreateSessionStores(profile, batch, Debug.isDebugBuild, out ISessionStore? sessionStore, out IPushSettingsStore? pushStore, out bool inMemory))
            {
                return false; // release sin B6-profile: sin sesiones ni avisos, sin aviso
            }

            if (inMemory)
            {
                Debug.LogWarning(LogPrefix + "Sesiones y settings.push en memoria (sin B6-profile): se pierden al cerrar la app.");
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            INotificationScheduler? scheduler = NotificationSchedulerOverride;
            if (scheduler == null)
            {
                CreatePlatformScheduler(clock, ref scheduler);
            }

            _notificationScheduler = scheduler ?? new FakeNotificationScheduler(clock);
            _sessionClock = clock;
            bool rebuilt = ReferenceEquals(s_sessionsProfile, profile) && s_carriedSessions != null && s_carriedSessionStore != null;
            _sessionStore = rebuilt ? s_carriedSessionStore : sessionStore;
            _sessions = rebuilt ? s_carriedSessions : new SessionTracker(sessionStore!, clock);
            s_sessionsProfile = profile;
            s_carriedSessions = _sessions;
            s_carriedSessionStore = _sessionStore;
            _comebackService = new ComebackService(wallet, null, SessionServerOverride ?? FakeServerDate.Offline());
            _push = new PushPermissionService(pushStore!, _notificationScheduler);
            _push.Reported += OnPushPromptReported;
            _notifications = new NotificationService(_notificationScheduler, _push, new LocNotificationTexts());
            // La tarjeta real del pre-prompt (GameBootstrap.PushUi.cs), o la de los tests (PrePromptOverride).
            _prePrompt = CreatePrePromptPresenter(rebuilt);
            _campaignPhaseSeen = _flow.Phase;
            _dailyPhaseSeen = _dailyScreen?.Flow?.Phase ?? FlowPhase.Loading;

            if (!rebuilt)
            {
                SessionStart start = _sessions!.OnColdStart();
                OnSessionStarted(in start);
            }

            SessionRunner runner = gameObject.GetComponent<SessionRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<SessionRunner>();
            }

            _sessionRunner = runner;
            _sessionRunner.Bind(this);
            return true;
        }

        /// <summary>
        /// La app pasa a segundo plano (<paramref name="paused"/>) o vuelve (lo llama <see cref="SessionRunner"/>). Al salir, apunta
        /// <c>stats.last_seen_at</c> y programa los avisos; al volver tras 30 min o más, abre otra sesión; al volver antes, solo
        /// cancela los avisos (06 §8.2: «se cancelan al abrir»).
        /// </summary>
        internal void SessionsPaused(bool paused)
        {
            SessionTracker? sessions = _sessions;
            NotificationService? notifications = _notifications;

            // B12: con la edad o el consentimiento pendientes, o el bloqueo de <13, ni sesión ni avisos locales (08 §7).
            if (sessions == null || notifications == null || ConsentHoldsSession)
            {
                return;
            }

            if (paused)
            {
                if (sessions.InBackground)
                {
                    return;
                }

                sessions.OnBackground();
                notifications.OnBackground(BuildNotificationContext());
                if (DevProbe.Enabled && _push != null)
                {
                    DevProbe.Write(SessionProbeFormat.Plan(SessionProbeMs(), _push.CanNotify, notifications.LastPlan));
                }

                return;
            }

            SessionStart? resumed = sessions.OnForeground();
            if (resumed.HasValue)
            {
                SessionStart s = resumed.Value;
                OnSessionStarted(in s);
            }
            else
            {
                ProbeOpen(notifications.OnForeground());
            }
        }

        /// <summary>
        /// Cada fotograma (<see cref="SessionRunner"/>): detecta la entrada en la pantalla de victoria de la campaña y del Reto, apunta
        /// <c>stats.last_win_at</c> y, si toca, enseña el pre-prompt de push. Sin asignaciones mientras nada cambia.
        /// </summary>
        internal void TickSessions()
        {
            if (_sessions == null)
            {
                return;
            }

            TickPlatformScheduler();
            TickPushUi(Mathf.Min(Time.unscaledDeltaTime, 1f) * 1000f);
            LevelFlow? flow = _flow;
            if (flow != null)
            {
                FlowPhase phase = flow.Phase;
                if (phase != _campaignPhaseSeen)
                {
                    _campaignPhaseSeen = phase;
                    if (phase == FlowPhase.VictoryScreen)
                    {
                        OnVictoryShown(Math.Max(0, flow.LevelNumber - 1), false);
                    }
                    else
                    {
                        ForgetPrePromptAtVictory(false);
                    }
                }
            }

            LevelFlow? daily = _dailyScreen?.Flow;
            FlowPhase dailyPhase = daily?.Phase ?? FlowPhase.Loading;
            if (dailyPhase != _dailyPhaseSeen)
            {
                _dailyPhaseSeen = dailyPhase;
                if (dailyPhase == FlowPhase.VictoryScreen)
                {
                    OnVictoryShown(_progress?.MaxLevel ?? 0, true); // el Reto no cambia max_level
                }
                else if (_dailyScreen == null || !_dailyScreen.Won)
                {
                    ForgetPrePromptAtVictory(true); // el calendario de sellos sigue a la victoria del Reto: se olvida al salir de él
                }
            }
        }

        /// <summary>
        /// Los almacenes de B6 sobre <paramref name="profile"/>: los de los tests (<see cref="SessionStoreOverride"/> y
        /// <see cref="PushSettingsStoreOverride"/>), si no los del perfil (<see cref="CreateProfileSessionStores"/>, B6-bootstrap con
        /// B6-profile) y, sin ellos, unos en memoria solo si <paramref name="debugBuild"/> (<paramref name="inMemory"/> = true). En
        /// release sin almacenes del perfil devuelve false.
        /// </summary>
        internal static bool CreateSessionStores(
            Profile profile, ProfileSaveBatch batch, bool debugBuild, out ISessionStore? sessions, out IPushSettingsStore? push, out bool inMemory)
        {
            inMemory = false;
            sessions = SessionStoreOverride;
            push = PushSettingsStoreOverride;
            if (sessions == null || push == null)
            {
                ISessionStore? profileSessions = null;
                IPushSettingsStore? profilePush = null;
                CreateProfileSessionStores(profile, batch, ref profileSessions, ref profilePush);
                sessions ??= profileSessions;
                push ??= profilePush;
            }

            if (sessions != null && push != null)
            {
                return true;
            }

            if (!debugBuild)
            {
                sessions = null;
                push = null;
                return false;
            }

            sessions ??= new InMemorySessionStore();
            push ??= new InMemoryPushSettingsStore();
            inMemory = true;
            return true;
        }

        /// <summary>
        /// Gancho de B6-bootstrap: <c>GameBootstrap.SessionsProfile.cs</c> lo implementa con <c>ProfileSessionStore</c> y
        /// <c>ProfilePushSettingsStore</c> (B6-profile) sobre <paramref name="profile"/> y la vía síncrona del lote, la misma que la
        /// cartera (<c>stats.last_seen_at</c> y el Regalo de regreso salen en la misma escritura). Sin el parche, todo queda null.
        /// </summary>
        static partial void CreateProfileSessionStores(Profile profile, ProfileSaveBatch batch, ref ISessionStore? sessions, ref IPushSettingsStore? push);

        /// <summary>
        /// Gancho de B6-notifications-real: crea el programador de Android (<c>com.unity.mobile.notifications</c>). Sin el parche,
        /// <paramref name="scheduler"/> queda null y se usa <see cref="FakeNotificationScheduler"/>.
        /// </summary>
        static partial void CreatePlatformScheduler(IClock clock, ref INotificationScheduler? scheduler);

        /// <summary>
        /// Gancho de B6-notifications-real: cada fotograma, la respuesta pendiente del diálogo nativo (el paquete la da por sondeo).
        /// Sin el parche no hace nada.
        /// </summary>
        partial void TickPlatformScheduler();

        private void OnSessionStarted(in SessionStart start)
        {
            NotificationService? notifications = _notifications;
            if (notifications == null || _push == null || _comebackService == null)
            {
                return;
            }

            if (DevProbe.Enabled)
            {
                DevProbe.Write(SessionProbeFormat.Session(SessionProbeMs(), in start));
            }

            ProbeOpen(notifications.OnForeground());
            _push.SyncWithSystem();
            _lastComeback = _comebackService.OnSessionStart(in start, _profile?.Streak?.Wins ?? 0);
            if (DevProbe.Enabled)
            {
                string? line = SessionProbeFormat.Comeback(SessionProbeMs(), _lastComeback);
                if (line != null)
                {
                    DevProbe.Write(line);
                }
            }
        }

        private void OnVictoryShown(int maxLevelBeforeVictory, bool daily)
        {
            SessionTracker? sessions = _sessions;
            if (sessions == null || _sessionClock == null)
            {
                return;
            }

            sessions.OnVictory();

            // El pre-prompt sale al dejar la victoria (Siguiente o Inicio, B6-Q16 (a); GameBootstrap.PushUi.cs): aquí solo se apunta.
            NotePrePromptAtVictory(maxLevelBeforeVictory, daily);
        }

        private void OnPushPromptReported(PushPromptEvent e)
        {
            LogPushPrompt(e); // af_push_prompt (08 §8.2)
            if (DevProbe.Enabled)
            {
                DevProbe.Write(SessionProbeFormat.Prompt(SessionProbeMs(), in e));
            }
        }

        private void ProbeOpen(NotificationOpenReport report)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            _sessionProbeLines.Clear();
            SessionProbeFormat.Open(SessionProbeMs(), report, _sessionProbeLines);
            for (int i = 0; i < _sessionProbeLines.Count; i++)
            {
                DevProbe.Write(_sessionProbeLines[i]);
            }

            _sessionProbeLines.Clear();
        }

        /// <summary>
        /// El estado del jugador para el planificador (06 §8.2, «Estado real»), leído de cada sistema por su fuente única. La Bitácora
        /// aún no existe (B4): sin avisos suyos.
        /// </summary>
        internal NotificationContext BuildNotificationContext()
        {
            IClock clock = _sessionClock ?? LaunchOptions.ClockOverride ?? SystemClock.Instance;
            int maxLevel = _progress?.MaxLevel ?? 0;
            var context = new NotificationContext
            {
                NowUtc = clock.UtcNow,
                LocalOffset = clock.LocalOffset,
                CurrentLevel = maxLevel + 1,
                CurrentWorldName = WorldName(WorldTable.WorldOf(Math.Max(1, maxLevel + 1))),
                ComebackSeeds = _comebackService?.Config.LiveComebackGift.Seeds ?? ComebackGift.Default.Seeds,
                ComebackGiftDays = _comebackService?.Config.LiveComebackGift.Days ?? ComebackGift.Default.Days,
            };

            DailyChallengeService? daily = _dailyService;
            if (daily != null && daily.IsAvailable(maxLevel))
            {
                context.DailyUnlocked = true;
                context.DailyPlayedToday = daily.CardState(maxLevel) != DailyCardState.Play;
                DailyChallengeRecord record = ProfileDailyChallengeStore.FromData(_profile?.DailyChallenge);
                context.DailyStreakDays = record.StreakDays;
                context.DailyLastWinDate = record.LastClaimedDate;
            }

            StreakService? streak = _streakService;
            if (streak != null)
            {
                StreakSnapshot snapshot = streak.Peek(maxLevel + 1, true);
                context.StreakActive = snapshot.Active;
                context.StreakWins = snapshot.Wins;
                context.StreakReminderHours = streak.Config.StreakReminderHours;
            }

            context.LastWinAt = _sessionStore?.Load().LastWinAt;
            Wallet? wallet = _wallet;
            if (wallet != null)
            {
                context.CanoesActive = wallet.CanoesActive(maxLevel + 1);
                context.Canoes = wallet.Canoes;
                context.CanoesRegen = wallet.Config.CanoesRegen;
            }

            LevelCatalog? catalog = _app?.Catalog;
            if (catalog != null && catalog.Count > 0 && maxLevel >= catalog.LastLevel)
            {
                context.ContentEnd = true;
                context.NextBatchEtaDays = GameConfig.LiveContentEndNextBatchEtaDays;
                context.NextWorldName = WorldName(WorldTable.WorldOf(catalog.LastLevel + 1));
            }

            return context;
        }

        private static string? WorldName(int world)
        {
            if (world < 1)
            {
                return null;
            }

            string key = "world_w" + world.ToString("00", CultureInfo.InvariantCulture) + "_name";
            return Loc.Has(key) ? Loc.T(key) : null;
        }

        private static long SessionProbeMs()
        {
            return (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
        }

        /// <summary>Los textos de los avisos con <c>Loc</c> (D-018): vacío si la clave no está en la tabla (entonces no se avisa).</summary>
        private sealed class LocNotificationTexts : INotificationTexts
        {
            public bool Has(string key)
            {
                return Loc.Has(key);
            }

            public string Resolve(string key, IReadOnlyList<NotificationArg> args)
            {
                if (!Loc.Has(key))
                {
                    return string.Empty;
                }

                LocArgs locArgs = LocArgs.None;
                int count = Math.Min(args.Count, LocArgs.MaxCount);
                for (int i = 0; i < count; i++)
                {
                    NotificationArg a = args[i];
                    switch (a.Kind)
                    {
                        case NotificationArgKind.Number:
                            locArgs = locArgs.And(a.Name, a.Integer);
                            break;
                        case NotificationArgKind.Digits:
                            locArgs = locArgs.AndDigits(a.Name, a.Integer);
                            break;
                        default:
                            locArgs = locArgs.And(a.Name, a.Text ?? string.Empty);
                            break;
                    }
                }

                return count == 0 ? Loc.T(key) : Loc.T(key, in locArgs);
            }
        }
    }
}
