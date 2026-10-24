#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// <see cref="ILeagueApi"/> falsa (editor, tests, capturas y el extra <c>af_league_fake</c> de la sonda): imita el servidor de
    /// liga.md §3 sin red. Idempotente por <c>win_id</c> dentro de la semana, puntos con la tabla de 06 §6 (con
    /// <see cref="TierOf"/>), rechaza un nivel por encima de <c>max_level</c> y lo de otra semana, y da un top determinista de
    /// <see cref="TopCount"/> filas con el jugador dentro o, si no llega, con su percentil. Sin asignaciones que importen: es de
    /// pruebas.
    /// </summary>
    public sealed class FakeLeagueApi : ILeagueApi
    {
        private static readonly string[] GlobalCountries = { "BR", "CO", "MX", "PE", "US", "AR", "VE", "EC", "CL", "BO", "FR", "GY", "ES", "SR", "DE" };

        private readonly IClock _clock;
        private readonly List<string> _accepted = new List<string>();
        private readonly List<Action> _held = new List<Action>();
        private string? _week;
        private int _points;

        public FakeLeagueApi(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Sin red: todo falla con <see cref="LeagueFailure.Offline"/>.</summary>
        public bool Offline { get; set; }

        /// <summary>Falla cada llamada con este motivo (null: no).</summary>
        public LeagueFailure? FailWith { get; set; }

        /// <summary>País del jugador (null: desconocido, solo Global y 404 <c>country_unknown</c> en País).</summary>
        public string? Country { get; set; } = "CO";

        /// <summary>Animal del seudónimo que «asigna el servidor».</summary>
        public string NameKey { get; set; } = "league_name_capybara";

        /// <summary>Cifras del seudónimo.</summary>
        public int NameNum { get; set; } = 4821;

        /// <summary>Filas del top (≤ 100); 0 = la semana aún vacía.</summary>
        public int TopCount { get; set; } = LeagueRules.MaxTop;

        /// <summary>Puntos de la fila 1 (los demás bajan de 15 en 15).</summary>
        public int TopPoints { get; set; } = 1980;

        /// <summary>Puntos iniciales del jugador en la semana (para capturas).</summary>
        public int StartPoints { get; set; }

        /// <summary>Dificultad de cada nivel (por defecto, Normal).</summary>
        public Func<int, RewardTier>? TierOf { get; set; }

        /// <summary>Guarda las respuestas hasta <see cref="Release"/> (para probar lo que está en vuelo).</summary>
        public bool Hold { get; set; }

        /// <summary>Llamadas recibidas (tests).</summary>
        public int WinsCalls { get; private set; }

        public int BoardCalls { get; private set; }

        /// <summary>La última petición de victorias (tests).</summary>
        public LeagueWinsRequest? LastRequest { get; private set; }

        /// <summary>Puntos del jugador esta semana.</summary>
        public int Points
        {
            get
            {
                Roll();
                return _points;
            }
        }

        /// <summary>Responde todo lo guardado con <see cref="Hold"/>.</summary>
        public void Release()
        {
            var held = new List<Action>(_held);
            _held.Clear();
            for (int i = 0; i < held.Count; i++)
            {
                held[i]();
            }
        }

        /// <inheritdoc />
        public void SendWins(LeagueWinsRequest request, Action<LeagueResult<LeagueWinsResult>> onDone)
        {
            if (request == null || onDone == null)
            {
                throw new ArgumentNullException(request == null ? nameof(request) : nameof(onDone));
            }

            WinsCalls++;
            LastRequest = request;
            Respond(() =>
            {
                if (Offline || FailWith.HasValue)
                {
                    onDone(LeagueResult<LeagueWinsResult>.Fail(Offline ? LeagueFailure.Offline : FailWith!.Value));
                    return;
                }

                Roll();
                DateTime start = LeagueRules.WeekStart(_clock.UtcNow);
                var accepted = new List<string>();
                var rejected = new List<LeagueRejection>();
                for (int i = 0; i < request.Wins.Count; i++)
                {
                    LeagueWin win = request.Wins[i];
                    if (_accepted.Contains(win.WinId))
                    {
                        rejected.Add(new LeagueRejection(win.WinId, "duplicate"));
                    }
                    else if (win.WonAt < start || win.WonAt > _clock.UtcNow.AddMinutes(5))
                    {
                        rejected.Add(new LeagueRejection(win.WinId, "outside_week"));
                    }
                    else if (win.Kind == LeagueWinKind.Campaign && win.Level > request.MaxLevel)
                    {
                        rejected.Add(new LeagueRejection(win.WinId, "unreachable_level"));
                    }
                    else
                    {
                        _accepted.Add(win.WinId);
                        accepted.Add(win.WinId);
                        _points += win.Kind == LeagueWinKind.Daily
                            ? LeagueRules.PointsDaily
                            : LeagueRules.CampaignPoints(TierOf?.Invoke(win.Level) ?? RewardTier.Normal);
                    }
                }

                onDone(LeagueResult<LeagueWinsResult>.Ok(new LeagueWinsResult(_week!, _points, accepted, rejected, NameKey, NameNum, Country)));
            });
        }

        /// <inheritdoc />
        public void GetBoard(LeagueScope scope, Action<LeagueResult<LeagueBoard>> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            BoardCalls++;
            Respond(() =>
            {
                if (Offline || FailWith.HasValue)
                {
                    onDone(LeagueResult<LeagueBoard>.Fail(Offline ? LeagueFailure.Offline : FailWith!.Value));
                    return;
                }

                if (scope == LeagueScope.Country && Country == null)
                {
                    onDone(LeagueResult<LeagueBoard>.Fail(LeagueFailure.CountryUnknown));
                    return;
                }

                onDone(LeagueResult<LeagueBoard>.Ok(Board(scope)));
            });
        }

        /// <summary>La clasificación que daría ahora el servidor falso.</summary>
        public LeagueBoard Board(LeagueScope scope)
        {
            Roll();
            DateTime now = _clock.UtcNow;
            DateTime start = LeagueRules.WeekStart(now);
            int count = Math.Max(0, Math.Min(LeagueRules.MaxTop, TopCount));
            var top = new List<LeagueRow>(count + 1);
            int myRank = 0;
            bool hasPoints = _points > 0;
            int rank = 1;
            for (int i = 0; i < count && top.Count < count; i++)
            {
                int points = Math.Max(1, TopPoints - (15 * i));
                if (hasPoints && myRank == 0 && _points > points)
                {
                    myRank = rank;
                    top.Add(new LeagueRow(rank++, NameKey, NameNum, Country, _points));
                    if (top.Count >= count)
                    {
                        break;
                    }
                }

                string key = LeagueRules.NameKeys[(i * 7) % LeagueRules.NameKeys.Length];
                if (i == 37)
                {
                    key = "league_name_unknown_future"; // una clave que este cliente no conoce: «Jugador {n}»
                }

                string? country = scope == LeagueScope.Country ? Country : GlobalCountries[i % GlobalCountries.Length];
                top.Add(new LeagueRow(rank++, key, 1000 + ((i * 397) % 9000), country, points));
            }

            int? percentile = null;
            if (hasPoints && myRank == 0)
            {
                percentile = Math.Max(1, Math.Min(100, 100 - (_points / 10)));
            }

            var me = new LeagueMe(_points, myRank > 0 ? myRank : (int?)null, percentile, hasPoints || _accepted.Count > 0 ? NameKey : null, NameNum, Country);
            return new LeagueBoard(_week!, start, LeagueRules.WeekEnd(start), scope, Country, now, top, me);
        }

        private void Respond(Action respond)
        {
            if (Hold)
            {
                _held.Add(respond);
                return;
            }

            respond();
        }

        private void Roll()
        {
            string week = LeagueRules.WeekId(_clock.UtcNow);
            if (!string.Equals(week, _week, StringComparison.Ordinal))
            {
                _week = week;
                _points = StartPoints;
                _accepted.Clear();
            }
        }
    }
}
