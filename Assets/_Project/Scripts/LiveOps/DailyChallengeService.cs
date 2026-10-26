#nullable enable
using System;
using AF.Levels;
using AF.Platform;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>Estado de la tarjeta del Reto en el inicio (nota §8; 06 §4.1 «Pantalla»; 06 §4.7), en el orden en que se decide.</summary>
    public enum DailyCardState
    {
        /// <summary>1. Oculta: interruptor apagado, nivel insuficiente o sin pool (D-008).</summary>
        Hidden = 0,

        /// <summary>2. En curso: hay partida empezada hoy (R-Q4) [P «Seguir»].</summary>
        InProgress = 1,

        /// <summary>3. Pendiente de validar: el premio de hoy espera la hora del servidor (08 §10.1).</summary>
        PendingValidation = 2,

        /// <summary>4. Sellado: hoy ya se cobró y su sello sigue puesto (un cobro anulado al reconciliar vuelve a «Jugar»).</summary>
        Sealed = 3,

        /// <summary>5. Jugar: el tablero de hoy (con el reloj atrasado se juega sin premio).</summary>
        Play = 4,
    }

    /// <summary>
    /// «Fin de día» del Reto (R-Q15): la partida de un día pasado que se descarta (R-Q4) sin que ese día se ganara. Es el único
    /// <c>af_daily_challenge.result = fail</c> de ese día (08 §8.2; R10). <see cref="Result"/> = <see cref="DailyResult.None"/>
    /// (nada que informar) si ese día ya estaba ganado (rejugada) o si la partida es de una fecha posterior a hoy (reloj atrasado:
    /// ese día no ha terminado) [P]. Sin <c>t_ms</c>: el registro no guarda el tiempo jugado (la sonda lo pone a null).
    /// </summary>
    public readonly struct DailyPlayEnd
    {
        /// <summary>Crea el informe.</summary>
        public DailyPlayEnd(string dateKey, int band, string boardId, DailyResult result, int streakDays)
        {
            DateKey = dateKey;
            Band = band;
            BoardId = boardId;
            Result = result;
            StreakDays = streakDays;
        }

        /// <summary>Fecha de inicio de la partida.</summary>
        public string DateKey { get; }

        /// <summary>Banda congelada.</summary>
        public int Band { get; }

        /// <summary>Tablero.</summary>
        public string BoardId { get; }

        /// <summary>Resultado que se informa: <see cref="DailyResult.Fail"/> o <see cref="DailyResult.None"/> (nada que informar).</summary>
        public DailyResult Result { get; }

        /// <summary>
        /// <c>streak_days</c> de <c>af_daily_challenge</c> [P]: la racha que ese día sin ganar rompe (R-Q7), es decir,
        /// <see cref="DailyChallengeRecord.StreakDays"/> si el último cobro fue el día anterior a <see cref="DateKey"/>; si no, 0.
        /// </summary>
        public int StreakDays { get; }

        /// <summary>Hay que informarlo (<see cref="Result"/> distinto de <see cref="DailyResult.None"/>).</summary>
        public bool IsReported => Result != DailyResult.None;
    }

    /// <summary>
    /// Fachada del Reto del río para <c>AF.UI</c> (nota §1.4 y §3 [P]; 08 §2.2, «Entrega a: UI»): estado de la tarjeta, tablero de
    /// hoy, empezar, reanudar o descartar (R-Q4), salir sin ganar dejándola reanudable (R-Q9, R-Q16) y reclamar (R-Q3, con <see cref="DailyChallengeRules"/>).
    /// Sin UI ni GameFlow: la partida (R8) escribe su estado con <see cref="SaveState"/> y avisa de la victoria con
    /// <see cref="ClaimWin"/>.
    /// <para>
    /// Partida que cruza la medianoche (R-Q3): con la app abierta sigue siendo de su fecha de inicio y se cobra esa fecha. Al
    /// reabrir la app o al volver al inicio (<see cref="DiscardStale"/>) o al empezar otra (<see cref="Start"/>), una partida de
    /// otro día se descarta (R-Q4).
    /// </para>
    /// <para>
    /// <c>af_daily_challenge.result</c> (R-Q15, R-Q16): <c>win</c> al reclamar (<see cref="ClaimWin"/>); <c>fail</c> una sola vez
    /// por día, cuando ese día termina sin victoria. Como salir deja la partida reanudable (R-Q16) y ganar la cierra, un día jugado
    /// y no ganado es exactamente uno cuya partida sigue en <see cref="DailyChallengeRecord.InProgress"/> cuando la fecha local ya
    /// es otra: al descartarla, <see cref="DiscardStale"/> lo informa una vez (<see cref="DayEnded"/>). Un día sin partida no
    /// informa nada. <c>abandon</c> ya no se emite: lo que cubría (salir, descartar por cambio de día) es ahora reanudar o
    /// <c>fail</c>.
    /// </para>
    /// </summary>
    public sealed class DailyChallengeService
    {
        private readonly IDailyChallengeStore _store;
        private readonly IClock _clock;
        private readonly DailyChallengeConfig _config;
        private readonly DailyPoolSource _pool;

        /// <summary>Crea el servicio. <paramref name="pool"/> null = pool ausente o inválido: la tarjeta queda oculta.</summary>
        public DailyChallengeService(
            IDailyChallengeStore store, DailyChallengeRules rules, IClock clock, DailyChallengeConfig config, DailyPool? pool)
            : this(store, rules, clock, config, new DailyPoolSource(pool))
        {
        }

        /// <summary>
        /// Crea el servicio con el pool de <paramref name="pool"/>, que puede leerse al pedirlo (memoria, 2026-10-26): la tarjeta solo
        /// mira <see cref="HasPool"/>; el pool se lee al empezar la partida o al resolver una sustitución del día.
        /// </summary>
        public static DailyChallengeService WithPoolSource(
            IDailyChallengeStore store, DailyChallengeRules rules, IClock clock, DailyChallengeConfig config, DailyPoolSource pool)
        {
            return new DailyChallengeService(store, rules, clock, config, pool);
        }

        // Privado: con el público de DailyPool, un null literal sería ambiguo.
        private DailyChallengeService(
            IDailyChallengeStore store, DailyChallengeRules rules, IClock clock, DailyChallengeConfig config, DailyPoolSource pool)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        /// <summary>Reglas del premio (racha mostrada, último cobro, reconciliación).</summary>
        public DailyChallengeRules Rules { get; }

        /// <summary>Configuración del Reto (08 §9) con la que se creó el servicio.</summary>
        public DailyChallengeConfig Config => _config;

        /// <summary>El pool, o null; con un pool que se lee al pedirlo, lo lee (<see cref="DailyPoolSource.Get"/>).</summary>
        public DailyPool? Pool => _pool.Get();

        /// <summary>Hay pool (o lo habrá al leerlo), sin leerlo (<see cref="DailyPoolSource.InBuild"/>).</summary>
        public bool HasPool => _pool.InBuild;

        /// <summary>El pool está leído ahora mismo (sin leerlo).</summary>
        public bool PoolLoaded => _pool.IsLoaded;

        /// <summary>De dónde sale el pool (sonda y tests).</summary>
        public DailyPoolSource PoolSource => _pool;

        /// <summary>Suelta el pool si se lee al pedirlo (al cerrar el Reto: no se queda en memoria hasta la siguiente partida).</summary>
        public void ReleasePool()
        {
            _pool.Release();
        }

        /// <summary>
        /// Última partida descartada por cambio de día, o null. Puede no informar nada (<see cref="DailyPlayEnd.IsReported"/>).
        /// </summary>
        public DailyPlayEnd? LastDiscard { get; private set; }

        /// <summary>
        /// Un día terminó sin victoria (R-Q15): sale una vez por día, con <see cref="DailyResult.Fail"/>, desde
        /// <see cref="DiscardStale"/> (también la que hace <see cref="Start"/>). Para la sonda <c>daily_challenge</c> (R10).
        /// </summary>
        public event Action<DailyPlayEnd>? DayEnded;

        /// <summary>El Reto está disponible para un jugador de nivel máximo <paramref name="maxLevel"/> (nota §6, «Desbloqueo»).</summary>
        public bool IsAvailable(int maxLevel)
        {
            return _pool.InBuild && DailyCalendar.IsUnlocked(maxLevel, _config);
        }

        /// <summary>Estado de la tarjeta (nota §8), en orden: oculta, en curso, pendiente, sellada, jugar.</summary>
        public DailyCardState CardState(int maxLevel)
        {
            if (!IsAvailable(maxLevel))
            {
                return DailyCardState.Hidden;
            }

            string today = _clock.LocalDateKey();
            DailyChallengeRecord record = _store.Load();
            if (record.InProgress != null && string.Equals(record.InProgress.DateKey, today, StringComparison.Ordinal))
            {
                return DailyCardState.InProgress;
            }

            if (record.IsPending(today))
            {
                return DailyCardState.PendingValidation;
            }

            // Sellado solo con el sello de hoy puesto: si la reconciliación anuló el cobro (reloj adelantado, nota §7.2), el sello se
            // quitó y LastClaimedDate no retrocede, así que la tarjeta vuelve a «Jugar» (sin premio: la fecha ya se reclamó).
            return string.Equals(record.LastClaimedDate, today, StringComparison.Ordinal) && record.IsStamped(today)
                ? DailyCardState.Sealed
                : DailyCardState.Play;
        }

        /// <summary>Slot de hoy (R7a: banda, día del pool y sustitución).</summary>
        /// <exception cref="InvalidOperationException">No hay pool.</exception>
        public DailySlot TodaySlot(int maxLevel)
        {
            if (!_pool.InBuild)
            {
                throw new InvalidOperationException("No hay pool del Reto del río.");
            }

            return DailyCalendar.Today(_clock, maxLevel, _config, _pool); // solo lee el pool si hay sustitución del día
        }

        /// <summary>
        /// Tablero de hoy: el de la partida en curso de hoy si la hay (R-Q4), si no el del slot de hoy. Null si el pool no lo trae.
        /// Lee el pool (<see cref="Pool"/>): entero la primera vez y otra vez tras <see cref="ReleasePool"/>. No se usa desde el inicio
        /// ni desde la tarjeta (memoria, 2026-10-26: allí basta <see cref="TodaySlot"/>, que no lo lee sin sustitución).
        /// </summary>
        public LevelDefinition? TodayBoard(int maxLevel)
        {
            DailyPool pool = RequirePool();
            DailyPlayInProgress? resumable = Resumable;
            return resumable != null ? pool.FindById(resumable.BoardId) : TodaySlot(maxLevel).Board(pool);
        }

        /// <summary>La fecha (<c>yyyy-MM-dd</c>) de la partida en curso, sea del día que sea, o null (D-046: el <c>win_id</c> del Reto en la Liga).</summary>
        public string? InProgressDateKey => _store.Load().InProgress?.DateKey;

        /// <summary>Partida empezada hoy que se puede reanudar (R-Q4), o null.</summary>
        public DailyPlayInProgress? Resumable
        {
            get
            {
                DailyPlayInProgress? play = _store.Load().InProgress;
                return play != null && string.Equals(play.DateKey, _clock.LocalDateKey(), StringComparison.Ordinal) ? play : null;
            }
        }

        /// <summary>
        /// Al abrir la app y cada vez que se vuelve al inicio (R-Q4; la UI, antes de pintar la tarjeta): si hay una partida de otro
        /// día, se descarta y se devuelve; si es de hoy, se conserva para reanudarla. Si es de un día pasado que no se ganó, ese día
        /// terminó sin victoria (R-Q15): se registra <c>result = fail</c> en el perfil y se avisa con <see cref="DayEnded"/>, una
        /// sola vez: la partida ya no está y, aunque el reloj atrasado permita jugar otra vez ese día, solo se informa un día
        /// posterior a <see cref="DailyChallengeRecord.LastDate"/>. Null si no había nada que descartar.
        /// </summary>
        public DailyPlayEnd? DiscardStale()
        {
            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress? play = record.InProgress;
            string today = _clock.LocalDateKey();
            if (play == null || string.Equals(play.DateKey, today, StringComparison.Ordinal))
            {
                return null;
            }

            // Solo un día posterior al último resultado registrado (LastDate solo avanza y guarda win y fail): con el reloj
            // atrasado, un día ya informado con fail o ya ganado (aunque no sea el último cobrado) no se informa otra vez.
            bool ended = string.CompareOrdinal(play.DateKey, today) < 0
                && !WonThatDay(record, play.DateKey)
                && (record.LastDate == null || string.CompareOrdinal(play.DateKey, record.LastDate) > 0);
            DailyResult reported = ended ? DailyResult.Fail : DailyResult.None;
            int streak = ended ? StreakBrokenBy(record, play.DateKey) : 0;
            RecordResult(record, play.DateKey, reported);
            record.InProgress = null;
            _store.Save(record);
            var end = new DailyPlayEnd(play.DateKey, play.Band, play.BoardId, reported, streak);
            LastDiscard = end;
            if (end.IsReported)
            {
                DayEnded?.Invoke(end);
            }

            return end;
        }

        /// <summary>
        /// Empieza (o reanuda, si ya hay una de hoy) la partida del Reto: crea <see cref="DailyPlayInProgress"/> con la fecha local y la
        /// banda congeladas (R-Q3; nota §6) y el tablero de hoy, y la guarda. Una partida de otro día se descarta antes
        /// (<see cref="LastDiscard"/>).
        /// </summary>
        /// <exception cref="InvalidOperationException">El Reto no está disponible o el pool no trae el tablero de hoy.</exception>
        public DailyPlayInProgress Start(int maxLevel)
        {
            if (!IsAvailable(maxLevel))
            {
                throw new InvalidOperationException("El Reto del río no está disponible.");
            }

            DailyPlayInProgress? resumable = Resumable;
            if (resumable != null)
            {
                return resumable;
            }

            DiscardStale();
            DailyPool pool = RequirePool();
            DailySlot slot = TodaySlot(maxLevel);
            LevelDefinition board = slot.Board(pool)
                ?? throw new InvalidOperationException("El pool no trae el tablero de hoy (" + slot + ").");
            var play = new DailyPlayInProgress
            {
                DateKey = _clock.LocalDateKey(),
                Band = slot.Band,
                BoardId = board.Id,
                State = new LevelStateData { Seed = DailyPool.SeedOf(pool.BandOf(board), pool.DayOf(board)) },
            };

            DailyChallengeRecord record = _store.Load();
            record.InProgress = play;
            _store.Save(record);
            return play;
        }

        /// <summary>Guarda el estado de la partida en curso (formato <c>level_state</c>, 04 §10.1), para reanudar el mismo día (R-Q4).</summary>
        /// <exception cref="InvalidOperationException">No hay partida en curso.</exception>
        public void SaveState(LevelStateData state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress play = record.InProgress ?? throw new InvalidOperationException("No hay partida del Reto en curso.");
            play.State = state;
            _store.Save(record);
        }

        /// <summary>
        /// El jugador sale sin ganar. R-Q16: la partida <b>no</b> se cierra: sigue en <see cref="DailyChallengeRecord.InProgress"/>
        /// para reanudarla el mismo día (<see cref="Resumable"/>); al cambiar de día la descarta <see cref="DiscardStale"/> (R-Q4).
        /// R-Q15: salir no es un resultado; el <c>fail</c> sale una vez, si el día termina sin victoria. No cambia el registro (el
        /// estado ya lo guardó la partida con <see cref="SaveState"/>). Devuelve la partida que queda.
        /// </summary>
        /// <exception cref="InvalidOperationException">No hay partida en curso.</exception>
        public DailyPlayInProgress RecordExitWithoutWin()
        {
            return _store.Load().InProgress ?? throw new InvalidOperationException("No hay partida del Reto en curso.");
        }

        /// <summary>
        /// Victoria: reclama la fecha de <b>inicio</b> de la partida (R-Q3) con <see cref="DailyChallengeRules.Claim"/>. Sin premio
        /// (fecha ya cobrada o reloj atrasado), la partida se cierra igual y la racha no cambia. Detalle en
        /// <see cref="DailyChallengeRules.LastClaim"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">No hay partida en curso.</exception>
        public ClaimOutcome ClaimWin()
        {
            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress play = record.InProgress ?? throw new InvalidOperationException("No hay partida del Reto en curso.");
            ClaimOutcome outcome = Rules.Claim(play.DateKey);
            if (outcome == ClaimOutcome.NoReward)
            {
                record = _store.Load();
                RecordResult(record, play.DateKey, DailyResult.Win);
                record.InProgress = null;
                _store.Save(record);
            }

            return outcome;
        }

        private static bool WonThatDay(DailyChallengeRecord record, string dateKey)
        {
            return string.Equals(record.LastClaimedDate, dateKey, StringComparison.Ordinal)
                || (record.Result == DailyResult.Win && string.Equals(record.LastDate, dateKey, StringComparison.Ordinal));
        }

        // La racha viva al empezar dateKey (último cobro el día anterior), que ese día sin ganar rompe (R-Q7); si no, 0.
        private static int StreakBrokenBy(DailyChallengeRecord record, string dateKey)
        {
            if (record.StreakDays <= 0
                || !DailyChallengeRules.TryParseDateKey(record.LastClaimedDate, out DateTime last)
                || !DailyChallengeRules.TryParseDateKey(dateKey, out DateTime date))
            {
                return 0;
            }

            return last == date.AddDays(-1) ? record.StreakDays : 0;
        }

        // last_date/result (08 §4.3) solo avanzan: una fecha anterior (reloj atrasado) no los pisa, ni otro resultado una victoria
        // del mismo día.
        private static void RecordResult(DailyChallengeRecord record, string dateKey, DailyResult result)
        {
            if (result == DailyResult.None)
            {
                return;
            }

            if (record.LastDate != null)
            {
                int cmp = string.CompareOrdinal(dateKey, record.LastDate);
                if (cmp < 0 || (cmp == 0 && record.Result == DailyResult.Win && result != DailyResult.Win))
                {
                    return;
                }
            }

            record.LastDate = dateKey;
            record.Result = result;
        }

        private DailyPool RequirePool()
        {
            return Pool ?? throw new InvalidOperationException("No hay pool del Reto del río.");
        }
    }
}
