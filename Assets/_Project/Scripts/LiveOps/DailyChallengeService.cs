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
    /// Una partida del Reto que terminó sin victoria: descartada por cambio de día (R-Q4, <see cref="DailyResult.Abandon"/>) o
    /// abandonada por el jugador (R-Q9, <see cref="DailyResult.Fail"/>). Para la sonda <c>af_daily_challenge</c> (R10).
    /// <see cref="Result"/> = <see cref="DailyResult.None"/> si ese día ya estaba ganado (no es «salir sin ganar ese día») [P].
    /// </summary>
    public readonly struct DailyPlayEnd
    {
        /// <summary>Crea el informe.</summary>
        public DailyPlayEnd(string dateKey, int band, string boardId, DailyResult result)
        {
            DateKey = dateKey;
            Band = band;
            BoardId = boardId;
            Result = result;
        }

        /// <summary>Fecha de inicio de la partida.</summary>
        public string DateKey { get; }

        /// <summary>Banda congelada.</summary>
        public int Band { get; }

        /// <summary>Tablero.</summary>
        public string BoardId { get; }

        /// <summary>Resultado que se informa (o <see cref="DailyResult.None"/>: nada que informar).</summary>
        public DailyResult Result { get; }
    }

    /// <summary>
    /// Fachada del Reto del río para <c>AF.UI</c> (nota §1.4 y §3 [P]; 08 §2.2, «Entrega a: UI»): estado de la tarjeta, tablero de
    /// hoy, empezar, reanudar o descartar (R-Q4), salir sin ganar (R-Q9) y reclamar (R-Q3, con <see cref="DailyChallengeRules"/>).
    /// Sin UI ni GameFlow: la partida (R8) escribe su estado con <see cref="SaveState"/> y avisa de la victoria con
    /// <see cref="ClaimWin"/>.
    /// <para>
    /// Partida que cruza la medianoche (R-Q3): con la app abierta sigue siendo de su fecha de inicio y se cobra esa fecha. Al
    /// reabrir la app (<see cref="DiscardStale"/>) o al empezar otra (<see cref="Start"/>), una partida de otro día se descarta con
    /// resultado <c>abandon</c>.
    /// </para>
    /// </summary>
    public sealed class DailyChallengeService
    {
        private readonly IDailyChallengeStore _store;
        private readonly IClock _clock;
        private readonly DailyChallengeConfig _config;

        /// <summary>Crea el servicio. <paramref name="pool"/> null = pool ausente o inválido: la tarjeta queda oculta.</summary>
        public DailyChallengeService(
            IDailyChallengeStore store, DailyChallengeRules rules, IClock clock, DailyChallengeConfig config, DailyPool? pool)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Pool = pool;
        }

        /// <summary>Reglas del premio (racha mostrada, último cobro, reconciliación).</summary>
        public DailyChallengeRules Rules { get; }

        /// <summary>El pool, o null.</summary>
        public DailyPool? Pool { get; }

        /// <summary>Última partida descartada por cambio de día (para la sonda), o null.</summary>
        public DailyPlayEnd? LastDiscard { get; private set; }

        /// <summary>El Reto está disponible para un jugador de nivel máximo <paramref name="maxLevel"/> (nota §6, «Desbloqueo»).</summary>
        public bool IsAvailable(int maxLevel)
        {
            return Pool != null && DailyCalendar.IsUnlocked(maxLevel, _config);
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
            return DailyCalendar.Today(_clock, maxLevel, _config, RequirePool());
        }

        /// <summary>
        /// Tablero de hoy: el de la partida en curso de hoy si la hay (R-Q4), si no el del slot de hoy. Null si el pool no lo trae.
        /// </summary>
        public LevelDefinition? TodayBoard(int maxLevel)
        {
            DailyPool pool = RequirePool();
            DailyPlayInProgress? resumable = Resumable;
            return resumable != null ? pool.FindById(resumable.BoardId) : TodaySlot(maxLevel).Board(pool);
        }

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
        /// Al abrir la app (R-Q4): si hay una partida de otro día, se descarta con resultado <c>abandon</c> y se devuelve para la sonda;
        /// si es de hoy, se conserva para reanudarla. Null si no había nada que descartar.
        /// </summary>
        public DailyPlayEnd? DiscardStale()
        {
            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress? play = record.InProgress;
            if (play == null || string.Equals(play.DateKey, _clock.LocalDateKey(), StringComparison.Ordinal))
            {
                return null;
            }

            DailyResult reported = WonThatDay(record, play.DateKey) ? DailyResult.None : DailyResult.Abandon;
            RecordResult(record, play.DateKey, reported);
            record.InProgress = null;
            _store.Save(record);
            var end = new DailyPlayEnd(play.DateKey, play.Band, play.BoardId, reported);
            LastDiscard = end;
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
        /// El jugador sale sin ganar (R-Q9: <c>result = fail</c>): se registra el resultado (salvo si ese día ya estaba ganado [P]) y la
        /// partida se cierra [P: salir no la deja para reanudar; R-Q4 solo habla de reabrir la app].
        /// </summary>
        /// <exception cref="InvalidOperationException">No hay partida en curso.</exception>
        public DailyPlayEnd RecordExitWithoutWin()
        {
            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress play = record.InProgress ?? throw new InvalidOperationException("No hay partida del Reto en curso.");
            DailyResult reported = WonThatDay(record, play.DateKey) ? DailyResult.None : DailyResult.Fail;
            RecordResult(record, play.DateKey, reported);
            record.InProgress = null;
            _store.Save(record);
            return new DailyPlayEnd(play.DateKey, play.Band, play.BoardId, reported);
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
            return record.Result == DailyResult.Win && string.Equals(record.LastDate, dateKey, StringComparison.Ordinal);
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
