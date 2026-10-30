#nullable enable
using System;
using System.Collections.Generic;

namespace AF.LiveOps
{
    /// <summary>Ámbito de la clasificación (liga.md §3.1, <c>scope=global|country</c>).</summary>
    public enum LeagueScope
    {
        /// <summary>Todos los jugadores.</summary>
        Global = 0,

        /// <summary>Los del país del jugador en esta semana.</summary>
        Country = 1,
    }

    /// <summary>Tipo de victoria del contrato (<c>kind</c>).</summary>
    public enum LeagueWinKind
    {
        /// <summary><c>campaign</c>: un nivel de campaña, con <see cref="LeagueWin.Level"/>.</summary>
        Campaign = 0,

        /// <summary><c>daily</c>: el Reto del río de un día, con <see cref="LeagueWin.Day"/>.</summary>
        Daily = 1,
    }

    /// <summary>Por qué falló una llamada de la Liga (y el <c>reason</c> de <c>af_league_unavailable</c>, 08 §8.2).</summary>
    public enum LeagueFailure
    {
        /// <summary><c>offline</c>: sin red o sin respuesta.</summary>
        Offline = 0,

        /// <summary><c>server</c>: el servidor respondió con un error (5xx, 429, 401 tras renovar…).</summary>
        Server = 1,

        /// <summary><c>invalid</c>: la respuesta no cumple el contrato.</summary>
        Invalid = 2,

        /// <summary><c>country_unknown</c>: <c>scope=country</c> sin país (404, liga.md §3.1).</summary>
        CountryUnknown = 3,

        /// <summary><c>not_configured</c>: este build no tiene backend o está apagado (<c>biz_backend_enabled</c>).</summary>
        NotConfigured = 4,

        /// <summary>
        /// 400 <c>bad_request</c> de <c>POST /v1/league/avatar</c> (D-056; avatares.md §4.4): un índice que el servidor no admite. No sale
        /// en <c>af_league_unavailable</c> (solo lo usa el envío del avatar, que lo anota en <c>af_error{module=league}</c>).
        /// </summary>
        Rejected = 5,

        /// <summary>
        /// <c>no_identity</c>: hay red, pero aún no hay identidad de la Liga (sin ID token: el usuario anónimo nuevo no existe todavía,
        /// como justo tras eliminar la cuenta, o el servidor rechaza la de antes con un 401). No es «sin conexión»: la pantalla enseña
        /// el primer contacto, con tu fila «Gana un nivel para entrar en la Liga» (nota del punto 18 de la jornada 6, 2026-10-30). No
        /// sale en <c>af_league_unavailable</c> (08 §8.2): solo en la sonda.
        /// </summary>
        NoIdentity = 6,
    }

    /// <summary>Ids de <see cref="LeagueScope"/>, <see cref="LeagueWinKind"/> y <see cref="LeagueFailure"/> en el contrato y la analítica.</summary>
    public static class LeagueIds
    {
        public static string Of(LeagueScope scope) => scope == LeagueScope.Country ? "country" : "global";

        public static string Of(LeagueWinKind kind) => kind == LeagueWinKind.Daily ? "daily" : "campaign";

        public static string Of(LeagueFailure failure)
        {
            switch (failure)
            {
                case LeagueFailure.Server: return "server";
                case LeagueFailure.Invalid: return "invalid";
                case LeagueFailure.CountryUnknown: return "country_unknown";
                case LeagueFailure.NotConfigured: return "not_configured";
                case LeagueFailure.NoIdentity: return "no_identity";
                default: return "offline";
            }
        }

        public static bool TryScope(string? id, out LeagueScope scope)
        {
            scope = id == "country" ? LeagueScope.Country : LeagueScope.Global;
            return id == "country" || id == "global";
        }

        public static bool TryKind(string? id, out LeagueWinKind kind)
        {
            kind = id == "daily" ? LeagueWinKind.Daily : LeagueWinKind.Campaign;
            return id == "daily" || id == "campaign";
        }
    }

    /// <summary>Una victoria en la cola (liga.md §3.1, elemento de <c>wins</c>).</summary>
    public sealed class LeagueWin
    {
        private LeagueWin(string winId, LeagueWinKind kind, int level, string? day, DateTime wonAt)
        {
            WinId = winId;
            Kind = kind;
            Level = level;
            Day = day;
            WonAt = DateTime.SpecifyKind(wonAt, DateTimeKind.Utc);
        }

        /// <summary>Id idempotente: <c>c:57</c> o <c>d:2026-10-26</c>.</summary>
        public string WinId { get; }

        public LeagueWinKind Kind { get; }

        /// <summary>Nivel de campaña (0 en el Reto).</summary>
        public int Level { get; }

        /// <summary>Día del Reto (<c>yyyy-MM-dd</c>; null en campaña).</summary>
        public string? Day { get; }

        /// <summary>Cuándo se ganó (UTC).</summary>
        public DateTime WonAt { get; }

        /// <summary>Victoria del nivel de campaña <paramref name="level"/>.</summary>
        public static LeagueWin Campaign(int level, DateTime wonAt) =>
            new LeagueWin(LeagueRules.CampaignWinId(level), LeagueWinKind.Campaign, level, null, wonAt);

        /// <summary>Victoria del Reto del río del día <paramref name="dayKey"/>.</summary>
        public static LeagueWin Daily(string dayKey, DateTime wonAt) =>
            new LeagueWin(LeagueRules.DailyWinId(dayKey), LeagueWinKind.Daily, 0, dayKey, wonAt);
    }

    /// <summary><c>POST /v1/league/wins</c>, petición (liga.md §3.1).</summary>
    public sealed class LeagueWinsRequest
    {
        public LeagueWinsRequest(IReadOnlyList<LeagueWin> wins, int maxLevel, string? region)
        {
            if (wins == null || wins.Count == 0 || wins.Count > LeagueRules.MaxBatch)
            {
                throw new ArgumentException("Entre 1 y " + LeagueRules.MaxBatch + " victorias por petición.", nameof(wins));
            }

            Wins = wins;
            MaxLevel = Math.Max(0, maxLevel);
            Region = LeagueRules.NormalizeRegion(region);
        }

        public IReadOnlyList<LeagueWin> Wins { get; }

        /// <summary><c>max_level</c> del perfil.</summary>
        public int MaxLevel { get; }

        /// <summary><c>region</c>: la del teléfono (ISO 3166-1 alfa-2), o null si no se sabe (el campo no se manda).</summary>
        public string? Region { get; }
    }

    /// <summary>Una victoria rechazada (<c>rejected[]</c>).</summary>
    public readonly struct LeagueRejection
    {
        public LeagueRejection(string winId, string reason)
        {
            WinId = winId;
            Reason = reason;
        }

        public string WinId { get; }

        /// <summary><c>duplicate</c>, <c>rate_limited</c>, <c>unreachable_level</c>, <c>outside_week</c> o <c>invalid</c>.</summary>
        public string Reason { get; }
    }

    /// <summary>Respuesta 200 de <c>POST /v1/league/wins</c> (liga.md §3.1).</summary>
    public sealed class LeagueWinsResult
    {
        public LeagueWinsResult(string week, int points, IReadOnlyList<string> accepted, IReadOnlyList<LeagueRejection> rejected,
            string nameKey, int nameNum, string? country, int? avatar = null)
        {
            Avatar = avatar;
            Week = week;
            Points = points;
            Accepted = accepted;
            Rejected = rejected;
            NameKey = nameKey;
            NameNum = nameNum;
            Country = country;
        }

        public string Week { get; }

        /// <summary>Puntos de la semana tras el envío.</summary>
        public int Points { get; }

        public IReadOnlyList<string> Accepted { get; }

        public IReadOnlyList<LeagueRejection> Rejected { get; }

        public string NameKey { get; }

        public int NameNum { get; }

        /// <summary>País de la semana, o null si es desconocido.</summary>
        public string? Country { get; }

        /// <summary><c>avatar</c> (D-056) del jugador según el servidor, o null (nunca eligió o el campo falta).</summary>
        public int? Avatar { get; }
    }

    /// <summary>Una fila del top (liga.md §3.1, <c>top[]</c>).</summary>
    public sealed class LeagueRow
    {
        public LeagueRow(int rank, string nameKey, int nameNum, string? country, int points, int? avatar = null)
        {
            Avatar = avatar;
            Rank = rank;
            NameKey = nameKey;
            NameNum = nameNum;
            Country = country;
            Points = points;
        }

        public int Rank { get; }

        public string NameKey { get; }

        public int NameNum { get; }

        public string? Country { get; }

        public int Points { get; }

        /// <summary><c>avatar</c> (D-056) de la fila: el índice del catálogo, o null (nunca eligió: el disco neutro).</summary>
        public int? Avatar { get; }
    }

    /// <summary>La fila del jugador (liga.md §3.1, <c>me</c>).</summary>
    public sealed class LeagueMe
    {
        public LeagueMe(int points, int? rank, int? percentile, string? nameKey, int nameNum, string? country, int? avatar = null)
        {
            Avatar = avatar;
            Points = points;
            Rank = rank;
            Percentile = percentile;
            NameKey = nameKey;
            NameNum = nameNum;
            Country = country;
        }

        public int Points { get; }

        /// <summary>Posición exacta si está en el top 100; si no, null.</summary>
        public int? Rank { get; }

        /// <summary>«Estás en el p % mejor» (1-100) fuera del top; null sin puntos o dentro del top.</summary>
        public int? Percentile { get; }

        /// <summary>Animal del seudónimo, o null si aún no tiene (sin ningún envío aceptado).</summary>
        public string? NameKey { get; }

        public int NameNum { get; }

        public string? Country { get; }

        /// <summary><c>me.avatar</c> (D-056) según el servidor, o null. La fila del jugador pinta el del perfil local (avatares.md §3.3).</summary>
        public int? Avatar { get; }
    }

    /// <summary>Respuesta 200 de <c>GET /v1/league?scope=</c> (liga.md §3.1).</summary>
    public sealed class LeagueBoard
    {
        public LeagueBoard(string week, DateTime startsAt, DateTime endsAt, LeagueScope scope, string? country, DateTime updatedAt,
            IReadOnlyList<LeagueRow> top, LeagueMe me)
        {
            Week = week;
            StartsAt = startsAt;
            EndsAt = endsAt;
            Scope = scope;
            Country = country;
            UpdatedAt = updatedAt;
            Top = top;
            Me = me;
        }

        public string Week { get; }

        public DateTime StartsAt { get; }

        public DateTime EndsAt { get; }

        public LeagueScope Scope { get; }

        /// <summary>País del ámbito (o del jugador en Global), o null.</summary>
        public string? Country { get; }

        /// <summary>Cuándo calculó el servidor esta clasificación.</summary>
        public DateTime UpdatedAt { get; }

        public IReadOnlyList<LeagueRow> Top { get; }

        public LeagueMe Me { get; }
    }

    /// <summary>Resultado de una llamada de <see cref="ILeagueApi"/>.</summary>
    public sealed class LeagueResult<T>
        where T : class
    {
        private LeagueResult(T? value, LeagueFailure failure)
        {
            Value = value;
            Failure = failure;
        }

        public T? Value { get; }

        public LeagueFailure Failure { get; }

        public bool IsOk => Value != null;

        public static LeagueResult<T> Ok(T value) => new LeagueResult<T>(value ?? throw new ArgumentNullException(nameof(value)), LeagueFailure.Offline);

        public static LeagueResult<T> Fail(LeagueFailure failure) => new LeagueResult<T>(null, failure);
    }

    /// <summary>
    /// El API de la Liga (liga.md §3.1), sin motor: en el juego, el cliente HTTP de <c>AF.Backend</c> por un adaptador de la UI;
    /// en el editor, los tests y la sonda, <see cref="FakeLeagueApi"/>. Cada método llama a <c>onDone</c> exactamente una vez, en
    /// el hilo principal; la falsa puede llamarlo antes de volver.
    /// </summary>
    public interface ILeagueApi
    {
        /// <summary><c>POST /v1/league/wins</c>.</summary>
        void SendWins(LeagueWinsRequest request, Action<LeagueResult<LeagueWinsResult>> onDone);

        /// <summary><c>GET /v1/league?scope=</c>.</summary>
        void GetBoard(LeagueScope scope, Action<LeagueResult<LeagueBoard>> onDone);

        /// <summary>
        /// <c>POST /v1/league/avatar</c> (D-056; avatares.md §4.4) con el índice <paramref name="avatar"/> (0-63). Un 400 falla con
        /// <see cref="LeagueFailure.Rejected"/>; un 404 de ruta (backend aún sin desplegar), un 429 y lo demás, con su motivo de siempre.
        /// </summary>
        void SetAvatar(int avatar, Action<LeagueResult<LeagueAvatarResult>> onDone);
    }

    /// <summary>Respuesta 200 de <c>POST /v1/league/avatar</c>: el índice que guardó el servidor.</summary>
    public sealed class LeagueAvatarResult
    {
        public LeagueAvatarResult(int avatar)
        {
            Avatar = avatar;
        }

        public int Avatar { get; }
    }
}
