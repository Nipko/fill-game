#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>Lo que respondió un envío de la cola (para <c>af_league_wins_sent</c>, 08 §8.2).</summary>
    public readonly struct LeagueSendReport
    {
        public LeagueSendReport(int count, int accepted, int rejected)
        {
            Count = count;
            Accepted = accepted;
            Rejected = rejected;
        }

        /// <summary>Victorias enviadas.</summary>
        public int Count { get; }

        public int Accepted { get; }

        public int Rejected { get; }
    }

    /// <summary>Estado de un ámbito para la pantalla de la Liga.</summary>
    public enum LeagueBoardState
    {
        /// <summary>Sin nada guardado de esta semana y pidiéndolo: «Cargando…».</summary>
        Loading = 0,

        /// <summary>La clasificación descargada hace menos de <c>live_league_cache_minutes</c>.</summary>
        Fresh = 1,

        /// <summary>Guardada pero vieja (sin conexión, o pidiéndola otra vez): «Actualizado hace {tiempo}».</summary>
        Stale = 2,

        /// <summary>Sin nada guardado y sin poder pedirla: «Conéctate para ver la Liga».</summary>
        Offline = 3,
    }

    /// <summary>
    /// La Liga del río de la 1.0 en el cliente (D-046; liga.md §1 y §4), sin motor: la cola de victorias en el perfil
    /// (<see cref="LeagueQueue"/>), los envíos en lotes de ≤ 50 por <see cref="ILeagueApi"/> (uno en vuelo a la vez), la caché de la
    /// última clasificación por ámbito y su vigencia (<c>live_league_cache_minutes</c>). Todo depende de <see cref="Active"/>: con
    /// la Liga apagada (<c>live_league_enabled</c>), antes del 41 o sin permiso (banda &lt; 13, perfil en solo lectura, cuenta
    /// borrada), ni cola ni red. Lo que se descarga es caché, nunca la fuente de verdad: los puntos los da el servidor.
    /// </summary>
    public sealed class LeagueService
    {
        private readonly ILeagueStore _store;
        private readonly ILeagueApi _api;
        private readonly IClock _clock;
        private readonly Func<int> _maxLevel;
        private readonly Func<string?> _region;
        private readonly Func<bool> _allowed;
        private bool _sending;
        private readonly bool[] _fetching = new bool[2];
        private readonly LeagueFailure?[] _lastFailure = new LeagueFailure?[2];

        public LeagueService(ILeagueStore store, ILeagueApi api, IClock clock, LeagueConfig? config, Func<int> maxLevel, Func<string?> region, Func<bool> allowed)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Config = config ?? LeagueConfig.Defaults;
            _maxLevel = maxLevel ?? throw new ArgumentNullException(nameof(maxLevel));
            _region = region ?? throw new ArgumentNullException(nameof(region));
            _allowed = allowed ?? throw new ArgumentNullException(nameof(allowed));
        }

        /// <summary>Las cifras de Remote Config de la sesión.</summary>
        public LeagueConfig Config { get; }

        /// <summary>El API (la falsa en el editor y con la sonda).</summary>
        public ILeagueApi Api => _api;

        /// <summary>La Liga existe para este jugador ahora: encendida, desbloqueada y con permiso.</summary>
        public bool Active => Config.LiveLeagueEnabled && _allowed() && LeagueRules.IsUnlocked(_maxLevel(), Config);

        /// <summary>Victorias en la cola.</summary>
        public int PendingCount => _store.Load().Pending.Count;

        /// <summary>Hay un envío en vuelo.</summary>
        public bool Sending => _sending;

        /// <summary>El registro vivo (tests y sonda).</summary>
        public LeagueRecord Record => _store.Load();

        /// <summary>Cada envío que respondió el servidor.</summary>
        public event Action<LeagueSendReport>? WinsSent;

        /// <summary>Una llamada que falló, con su motivo (<c>af_league_unavailable</c>).</summary>
        public event Action<LeagueFailure>? Unavailable;

        /// <summary>Cambió lo que se sabe de un ámbito (llegó su clasificación o falló su descarga).</summary>
        public event Action<LeagueScope>? BoardChanged;

        /// <summary>
        /// La victoria del nivel de campaña <paramref name="level"/>, de dificultad <paramref name="tier"/>: si suma (Liga activa,
        /// desde el desbloqueo y la primera vez esta semana), entra en la cola, se intenta mandar y devuelve los puntos para la línea
        /// de la victoria; si no, 0.
        /// </summary>
        public int RecordCampaignWin(int level, RewardTier tier)
        {
            if (level < Config.LiveLeagueUnlockLevel || !Active)
            {
                return 0;
            }

            return EnqueueWin(LeagueWin.Campaign(level, _clock.UtcNow)) ? LeagueRules.CampaignPoints(tier) : 0;
        }

        /// <summary>La victoria del Reto del río del día <paramref name="dayKey"/> (<c>yyyy-MM-dd</c>): como <see cref="RecordCampaignWin"/>.</summary>
        public int RecordDailyWin(string dayKey)
        {
            if (!Active || !LeagueRules.IsDayKey(dayKey))
            {
                return 0;
            }

            return EnqueueWin(LeagueWin.Daily(dayKey, _clock.UtcNow)) ? LeagueRules.PointsDaily : 0;
        }

        /// <summary>
        /// Manda el siguiente lote de la cola si hay algo y nada en vuelo; al responder, sigue con el resto. False si no mandó.
        /// Sin red, la cola se queda como estaba para el próximo intento.
        /// </summary>
        public bool Flush()
        {
            if (_sending || !Active)
            {
                return false;
            }

            LeagueRecord record = _store.Load();
            if (LeagueQueue.Roll(record, _clock.UtcNow))
            {
                _store.Save(record);
            }

            if (record.Pending.Count == 0)
            {
                return false;
            }

            List<LeagueWin> batch = LeagueQueue.NextBatch(record);
            var request = new LeagueWinsRequest(batch, _maxLevel(), _region());
            _sending = true;
            _api.SendWins(request, result => OnSent(batch, result));
            return true;
        }

        /// <summary>
        /// Pide la clasificación de <paramref name="scope"/> si la guardada ya no vale (o con <paramref name="force"/>) y no hay otra
        /// petición de ese ámbito en vuelo. False si no la pidió.
        /// </summary>
        public bool RequestBoard(LeagueScope scope, bool force = false)
        {
            int i = (int)scope;
            if (!Active || _fetching[i] || (!force && IsFresh(scope)))
            {
                return false;
            }

            _fetching[i] = true;
            _api.GetBoard(scope, result => OnBoard(scope, result));
            return true;
        }

        /// <summary>La clasificación guardada de esta semana para <paramref name="scope"/>, o null.</summary>
        public LeagueCacheEntry? Cached(LeagueScope scope)
        {
            LeagueCacheEntry? entry = _store.Load().Cache(scope);
            if (entry == null)
            {
                return null;
            }

            // Solo la de esta semana: la del lunes pasado ya no dice nada (cada lunes los puntos vuelven a 0).
            return string.Equals(entry.Board.Week, LeagueRules.WeekId(_clock.UtcNow), StringComparison.Ordinal) ? entry : null;
        }

        /// <summary>La clasificación guardada de <paramref name="scope"/> tiene menos de <c>live_league_cache_minutes</c>.</summary>
        public bool IsFresh(LeagueScope scope)
        {
            LeagueCacheEntry? entry = Cached(scope);
            if (entry == null || entry.FetchedAt == DateTime.MinValue)
            {
                return false;
            }

            TimeSpan age = _clock.UtcNow - entry.FetchedAt;
            return age >= TimeSpan.Zero && age < TimeSpan.FromMinutes(Config.LiveLeagueCacheMinutes);
        }

        /// <summary>Qué enseña la pantalla para <paramref name="scope"/>.</summary>
        public LeagueBoardState StateOf(LeagueScope scope)
        {
            if (Cached(scope) != null)
            {
                return IsFresh(scope) ? LeagueBoardState.Fresh : LeagueBoardState.Stale;
            }

            return _fetching[(int)scope] ? LeagueBoardState.Loading : LeagueBoardState.Offline;
        }

        /// <summary>El último fallo de <paramref name="scope"/> (null tras una descarga buena).</summary>
        public LeagueFailure? LastFailure(LeagueScope scope) => _lastFailure[(int)scope];

        /// <summary>
        /// El país del jugador en la Liga esta semana (lo último que dijo el servidor), o null si no se sabe. Con él, la pestaña
        /// {país} y la fila de Ajustes.
        /// </summary>
        public string? Country
        {
            get
            {
                LeagueRecord record = _store.Load();
                string week = LeagueRules.WeekId(_clock.UtcNow);
                if (string.Equals(record.MeWeek, week, StringComparison.Ordinal) && record.MeCountry != null)
                {
                    return record.MeCountry;
                }

                LeagueCacheEntry? cached = Cached(LeagueScope.Global) ?? Cached(LeagueScope.Country);
                return cached?.Board.Me.Country ?? cached?.Board.Country;
            }
        }

        /// <summary>Borra la cola y la caché (borrado de cuenta; 08 §11). Lo que esté en vuelo ya no se aplica.</summary>
        public void Wipe()
        {
            _store.Wipe();
            _sending = false;
            _fetching[0] = false;
            _fetching[1] = false;
            Wiped = true;
        }

        /// <summary>Se borró la cuenta: nada más de esta sesión toca el registro.</summary>
        public bool Wiped { get; private set; }

        private bool EnqueueWin(LeagueWin win)
        {
            LeagueRecord record = _store.Load();
            LeagueQueue.Roll(record, _clock.UtcNow);
            bool counted = LeagueQueue.Enqueue(record, win);
            _store.Save(record);
            if (counted)
            {
                Flush();
            }

            return counted;
        }

        private void OnSent(List<LeagueWin> batch, LeagueResult<LeagueWinsResult> result)
        {
            if (Wiped)
            {
                return;
            }

            _sending = false;
            if (!result.IsOk)
            {
                Unavailable?.Invoke(result.Failure);
                return;
            }

            LeagueRecord record = _store.Load();
            int removed = LeagueQueue.Apply(record, batch, result.Value!);
            // Tras sumar, la clasificación guardada ya no cuenta los puntos de ahora: se vuelve a pedir al abrir la pantalla.
            if (record.CacheGlobal != null)
            {
                record.CacheGlobal.FetchedAt = DateTime.MinValue;
            }

            if (record.CacheCountry != null)
            {
                record.CacheCountry.FetchedAt = DateTime.MinValue;
            }

            _store.Save(record);
            WinsSent?.Invoke(new LeagueSendReport(batch.Count, result.Value!.Accepted.Count, result.Value.Rejected.Count));
            if (removed > 0 && record.Pending.Count > 0)
            {
                Flush(); // el resto de la cola, en el lote siguiente
            }
        }

        private void OnBoard(LeagueScope scope, LeagueResult<LeagueBoard> result)
        {
            if (Wiped)
            {
                return;
            }

            int i = (int)scope;
            _fetching[i] = false;
            if (!result.IsOk)
            {
                _lastFailure[i] = result.Failure;
                Unavailable?.Invoke(result.Failure);
                BoardChanged?.Invoke(scope);
                return;
            }

            _lastFailure[i] = null;
            LeagueRecord record = _store.Load();
            record.SetCache(scope, new LeagueCacheEntry(result.Value!, _clock.UtcNow));
            _store.Save(record);
            BoardChanged?.Invoke(scope);
        }
    }
}
