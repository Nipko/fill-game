#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.IAP;
using AF.LiveOps;

namespace AF.UI
{
    /// <summary>
    /// <see cref="ILeagueApi"/> real de la Liga (D-046): el cliente HTTP de <c>AF.Backend</c> (<see cref="ILeagueBackend"/>, con el
    /// autorizador de siempre: ID token de Firebase y App Check) traducido a los tipos de <c>AF.LiveOps</c>, que no conoce el
    /// backend (08 §2.2). Con <c>biz_backend_enabled</c> apagado no llama: falla con <see cref="LeagueFailure.NotConfigured"/>.
    /// </summary>
    internal sealed class LeagueBackendApi : ILeagueApi
    {
        private readonly ILeagueBackend _backend;
        private readonly IBackendSwitch? _switch;

        public LeagueBackendApi(ILeagueBackend backend, IBackendSwitch? backendSwitch)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _switch = backendSwitch;
        }

        public void SendWins(LeagueWinsRequest request, Action<LeagueResult<LeagueWinsResult>> onDone)
        {
            if (_switch != null && !_switch.BizBackendEnabled)
            {
                onDone(LeagueResult<LeagueWinsResult>.Fail(LeagueFailure.NotConfigured));
                return;
            }

            _backend.SendLeagueWins(ToDto(request), r =>
            {
                if (!r.IsOk)
                {
                    onDone(LeagueResult<LeagueWinsResult>.Fail(FailureOf(r.Error!, false), DetailOf(r.Error!)));
                    return;
                }

                onDone(LeagueResult<LeagueWinsResult>.Ok(FromDto(r.Value!)));
            });
        }

        public void GetBoard(LeagueScope scope, Action<LeagueResult<LeagueBoard>> onDone)
        {
            if (_switch != null && !_switch.BizBackendEnabled)
            {
                onDone(LeagueResult<LeagueBoard>.Fail(LeagueFailure.NotConfigured));
                return;
            }

            _backend.GetLeagueBoard(LeagueIds.Of(scope), r =>
            {
                if (!r.IsOk)
                {
                    onDone(LeagueResult<LeagueBoard>.Fail(FailureOf(r.Error!, scope == LeagueScope.Country), DetailOf(r.Error!)));
                    return;
                }

                onDone(LeagueResult<LeagueBoard>.Ok(FromDto(r.Value!, scope)));
            });
        }

        public void SetAvatar(int avatar, Action<LeagueResult<LeagueAvatarResult>> onDone)
        {
            if (_switch != null && !_switch.BizBackendEnabled)
            {
                onDone(LeagueResult<LeagueAvatarResult>.Fail(LeagueFailure.NotConfigured));
                return;
            }

            if (avatar < 0 || avatar > LeagueContract.MaxAvatar)
            {
                onDone(LeagueResult<LeagueAvatarResult>.Fail(LeagueFailure.Rejected));
                return;
            }

            _backend.SetLeagueAvatar(new LeagueAvatarRequestDto(avatar), r =>
            {
                if (!r.IsOk)
                {
                    onDone(LeagueResult<LeagueAvatarResult>.Fail(AvatarFailureOf(r.Error!), DetailOf(r.Error!)));
                    return;
                }

                onDone(LeagueResult<LeagueAvatarResult>.Ok(new LeagueAvatarResult(r.Value!.Avatar)));
            });
        }

        // D-056 (avatares.md §4.4): un 400 es un índice que el servidor no admite (se descarta); el resto (sin red, 404 de una ruta aún
        // sin desplegar, 429…) deja el pendiente para la siguiente apertura.
        internal static LeagueFailure AvatarFailureOf(BackendError error)
        {
            if (error.HttpStatus == 400 || error.Code == ApiErrorCode.BadRequest)
            {
                return LeagueFailure.Rejected;
            }

            return FailureOf(error, false);
        }

        internal static LeagueWinsRequestDto ToDto(LeagueWinsRequest request)
        {
            var wins = new List<LeagueWinDto>(request.Wins.Count);
            for (int i = 0; i < request.Wins.Count; i++)
            {
                LeagueWin w = request.Wins[i];
                wins.Add(w.Kind == LeagueWinKind.Daily
                    ? new LeagueWinDto(w.WinId, "daily", null, w.Day, w.WonAt)
                    : new LeagueWinDto(w.WinId, "campaign", w.Level, null, w.WonAt));
            }

            return new LeagueWinsRequestDto(wins, request.MaxLevel, request.Region);
        }

        internal static LeagueWinsResult FromDto(LeagueWinsResponseDto dto)
        {
            var rejected = new List<LeagueRejection>(dto.Rejected.Count);
            for (int i = 0; i < dto.Rejected.Count; i++)
            {
                rejected.Add(new LeagueRejection(dto.Rejected[i].Key, dto.Rejected[i].Value));
            }

            return new LeagueWinsResult(dto.Week, dto.Points, dto.Accepted, rejected, dto.NameKey, dto.NameNum, dto.Country, dto.Avatar);
        }

        internal static LeagueBoard FromDto(LeagueBoardResponseDto dto, LeagueScope asked)
        {
            var top = new List<LeagueRow>(dto.Top.Count);
            for (int i = 0; i < dto.Top.Count; i++)
            {
                LeagueRowDto r = dto.Top[i];
                top.Add(new LeagueRow(r.Rank, r.NameKey, r.NameNum, r.Country, r.Points, r.Avatar));
            }

            var me = new LeagueMe(dto.MePoints, dto.MeRank, dto.MePercentile, dto.MeNameKey, dto.MeNameNum, dto.MeCountry, dto.MeAvatar);
            LeagueIds.TryScope(dto.Scope, out LeagueScope scope);
            return new LeagueBoard(dto.Week, dto.StartsAt, dto.EndsAt, dto.Scope == null ? asked : scope, dto.Country, dto.UpdatedAt, top, me);
        }

        /// <summary>
        /// El detalle del error para la sonda (punto 18 de la jornada 6, 2026-10-30): con respuesta HTTP, <c>http_{código}</c> (el
        /// mensaje del servidor es texto libre); si no, el <see cref="BackendError.Detail"/> del proveedor (<c>auth_token_failed</c>,
        /// <c>appcheck_token_failed</c>, <c>firebase_unavailable:…</c>, el error del transporte…), que nunca lleva tokens, UID ni URL.
        /// Solo letras, cifras y <c>_:.-</c>, hasta 64 caracteres; null si no queda nada.
        /// </summary>
        internal static string? DetailOf(BackendError error)
        {
            string? raw = error.Kind == BackendErrorKind.Http
                ? "http_" + error.HttpStatus.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : error.Detail;
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            var sb = new System.Text.StringBuilder(Math.Min(raw!.Length, 64));
            for (int i = 0; i < raw.Length && sb.Length < 64; i++)
            {
                char c = raw[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == ':' || c == '.' || c == '-';
                sb.Append(ok ? c : '_');
            }

            return sb.ToString();
        }

        internal static LeagueFailure FailureOf(BackendError error, bool country)
        {
            switch (error.Kind)
            {
                case BackendErrorKind.Network:
                case BackendErrorKind.AuthUnavailable:
                case BackendErrorKind.IntegrityUnavailable:
                    return LeagueFailure.Offline;
                case BackendErrorKind.InvalidResponse:
                    return LeagueFailure.Invalid;
                case BackendErrorKind.NotConfigured:
                    return LeagueFailure.NotConfigured;
                default:
                    return country && (error.Code == ApiErrorCode.CountryUnknown || error.HttpStatus == 404) ? LeagueFailure.CountryUnknown : LeagueFailure.Server;
            }
        }
    }
}
