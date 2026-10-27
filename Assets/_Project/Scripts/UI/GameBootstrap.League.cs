#nullable enable
using System;
using System.Globalization;
using AF.Analytics;
using AF.Backend;
using AF.Events;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La Liga del río de la 1.0 en el juego (D-046; <c>docs/dev/liga.md</c> §1 y §4). <see cref="SetUpLeague"/> (en <c>Start</c>, tras
    /// el Mercado) crea:
    /// <list type="bullet">
    /// <item>el servicio (<see cref="LeagueService"/>) con la cola en el perfil (<see cref="ProfileLeagueStore"/>, por el lote) y el API:
    /// el real (<see cref="LeagueBackendApi"/> sobre <see cref="HttpBackendClient"/>) con el backend real; la falsa
    /// (<see cref="FakeLeagueApi"/>) en el editor, en desarrollo sin Firebase y con el extra <c>af_league_fake</c>; y ninguno en release
    /// sin backend (todo falla con <c>not_configured</c>);</item>
    /// <item>el botón «Liga» del inicio (<see cref="LeagueLobbyButton"/>), desde <c>live_league_unlock_level</c> y con
    /// <c>live_league_enabled</c> (o el extra <c>af_league</c> en desarrollo), y la pantalla (<see cref="LeagueScreen"/>);</item>
    /// <item>los enganches: la victoria de campaña (<see cref="LevelWon"/> del bus: la cola y la línea «+{n} puntos de Liga»), la del
    /// Reto del río (<see cref="DailyChallengeScreen.DayWon"/>), Ajustes («País en la Liga»), el envío al volver a primer plano y cada
    /// <see cref="RetryIntervalMs"/> con cola, el Atrás de Android y el borrado de cuenta (la cola y la caché se borran).</item>
    /// </list>
    /// Con la banda &lt; 13 (D-033: sin red ni SDK), el perfil en solo lectura o la cuenta borrada, nada: ni botón ni red.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Cada cuánto se reintenta la cola con el juego abierto y algo pendiente (ms) [P].</summary>
        public const float RetryIntervalMs = 120000f;

        private LeagueService? _league;
        private LeagueScreen? _leagueScreen;
        private LeagueLobbyButton? _leagueButton;
        private IClock? _leagueClock;
        private float _leagueRetryMs;
        private bool _leagueButtonShown;
        private LeagueFailure? _leagueLastFailure;

        /// <summary>API de la Liga para los tests (gana a todas).</summary>
        internal static ILeagueApi? LeagueApiOverride { get; set; }

        /// <summary>Cifras de la Liga para los tests (ganan a Remote Config y a los extras).</summary>
        internal static LeagueConfig? LeagueConfigOverride { get; set; }

        /// <summary>Reloj de la Liga para los tests (por defecto, el del dispositivo).</summary>
        internal static IClock? LeagueClockOverride { get; set; }

        /// <summary>Limpia los ganchos de test de la Liga.</summary>
        internal static void ResetLeagueOverrides()
        {
            LeagueApiOverride = null;
            LeagueConfigOverride = null;
            LeagueClockOverride = null;
        }

        /// <summary>El servicio de la Liga (null antes del arranque).</summary>
        public LeagueService? League => _league;

        internal LeagueScreen? LeagueView => _leagueScreen;

        internal LeagueLobbyButton? LeagueButton => _leagueButton;

        /// <summary>La pantalla de la Liga está abierta.</summary>
        public bool LeagueOpen => _leagueScreen != null && _leagueScreen.Visible;

        /// <summary>La Liga existe ahora para este jugador (botón, cola y red).</summary>
        public bool LeagueActive => _league != null && _league.Active;

        /// <summary>
        /// La Liga puede actuar ahora: con la edad ya contestada (como Play Billing en <c>GameBootstrap.Backend.cs</c>: con el
        /// consentimiento activo y sin registro, nada), nunca con la banda &lt; 13 (D-033: sin red ni SDK) y con el perfil escribible.
        /// </summary>
        private bool LeagueAllowed()
        {
            if (AgeUnder13Override ?? PlayerUnder13())
            {
                return false;
            }

            return !(ConsentEnabled && _consentFlow?.Record == null) && !ProfileLocked && !AccountDeleted;
        }

        private void SetUpLeague()
        {
            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            if (profile == null || batch == null || _art == null)
            {
                return;
            }

            IClock clock = LeagueClockOverride ?? _telemetryClock ?? new SystemClock();
            _leagueClock = clock;
            LeagueConfig config = LeagueConfigOverride ?? CopyLeagueConfig(_rcConfigs?.League);
            bool development = Debug.isDebugBuild;
            if (LeagueConfigOverride == null && LaunchOptions.ResolveLeagueEnabled(development))
            {
                config.LiveLeagueEnabled = true; // extra af_league (solo desarrollo)
                Debug.LogWarning("AF_LEAGUE " + LaunchOptions.IntentExtraLeague + " (solo desarrollo): la Liga del río encendida sin Remote Config.");
            }

            ILeagueApi api = LeagueApiOverride ?? CreateLeagueApi(clock, development);
            var store = new ProfileLeagueStore(profile, batch);
            _league = new LeagueService(store, api, clock, config,
                () => _progress?.MaxLevel ?? 0,
                () => LeagueRules.NormalizeRegion(ConsentCountryCode),
                () => LeagueAllowed());
            _league.WinsSent += OnLeagueWinsSent;
            _league.Unavailable += OnLeagueUnavailable;
            _league.BoardChanged += OnLeagueBoardChanged;

            _leagueScreen = new LeagueScreen(transform, _art, _probe, () => CloseLeague());
            _leagueScreen.TabSelected = scope => OnLeagueTab(scope);
            _leagueButton = new LeagueLobbyButton(transform, _art, () => OpenLeague());
            _events?.Subscribe<LevelWon>(OnLeagueLevelWon);
            if (_dailyScreen != null)
            {
                _dailyScreen.DayWon = day => OnLeagueDailyWon(day);
            }

            if (_screen != null)
            {
                _screen.LeagueLineReserved = _league.Active;
            }

            LeagueRunner runner = gameObject.GetComponent<LeagueRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<LeagueRunner>();
            }

            runner.Bind(this);
        }

        // Las cifras de la sesión en una copia (el extra de desarrollo no toca las de Remote Config).
        private static LeagueConfig CopyLeagueConfig(LeagueConfig? rc)
        {
            LeagueConfig source = rc ?? LeagueConfig.Defaults;
            return new LeagueConfig
            {
                LiveLeagueEnabled = source.LiveLeagueEnabled,
                LiveLeagueUnlockLevel = source.LiveLeagueUnlockLevel,
                LiveLeagueCacheMinutes = source.LiveLeagueCacheMinutes,
            };
        }

        private ILeagueApi CreateLeagueApi(IClock clock, bool development)
        {
            if (LaunchOptions.ResolveLeagueFake(development))
            {
                Debug.LogWarning("AF_LEAGUE " + LaunchOptions.IntentExtraLeagueFake + " (solo desarrollo): la Liga con datos falsos, sin red.");
                return SampleLeagueApi(clock);
            }

            if (_backend is HttpBackendClient http)
            {
                return new LeagueBackendApi(http, _backendSwitch);
            }

            if (_backend is FakeBackendClient)
            {
                return SampleLeagueApi(clock); // editor y desarrollo sin google-services.json
            }

            return new UnavailableLeagueApi();
        }

        /// <summary>La Liga falsa con datos de muestra (capturas y pruebas en el teléfono).</summary>
        internal static FakeLeagueApi SampleLeagueApi(IClock clock)
        {
            return new FakeLeagueApi(clock) { Country = "CO", StartPoints = 0, TopPoints = 1980 };
        }

        /// <summary>
        /// Abre la pantalla de la Liga desde su botón del inicio: solo con el inicio a la vista y la Liga activa. Pide la clasificación
        /// de los dos ámbitos si la guardada ya no vale y manda la cola. Escribe <c>af_league_open</c> y <c>AF_STATE league_open</c>.
        /// </summary>
        public bool OpenLeague()
        {
            LeagueService? league = _league;
            LeagueScreen? screen = _leagueScreen;
            if (league == null || screen == null || screen.Visible || !league.Active || !LobbyClear() || _leagueClock == null)
            {
                return false;
            }

            league.Flush();
            league.RequestBoard(LeagueScope.Global);
            if (league.Country != null)
            {
                league.RequestBoard(LeagueScope.Country);
            }

            PrepareLeagueAvatars(screen); // D-056: el avatar en cada fila (GameBootstrap.Avatars.cs)
            screen.Open(league, _leagueClock, LeagueScope.Global);
            _leagueButton?.Hide();
            _leagueButtonShown = false;
            LogLeagueOpen(LeagueScope.Global);
            _skipNextDelta = true;
            return true;
        }

        /// <summary>Cierra la pantalla de la Liga (su X y el Atrás de Android).</summary>
        public bool CloseLeague()
        {
            LeagueScreen? screen = _leagueScreen;
            if (screen == null || !screen.Visible)
            {
                return false;
            }

            screen.Close();
            if (DevProbe.Enabled)
            {
                MarketProbe("league_closed", null);
            }

            return true;
        }

        private void OnLeagueTab(LeagueScope scope)
        {
            _league?.RequestBoard(scope);
            LogLeagueOpen(scope);
        }

        private void LogLeagueOpen(LeagueScope scope)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_league_open").Add("scope", LeagueIds.Of(scope))); // 08 §8.2 (D-046)
            if (DevProbe.Enabled)
            {
                LeagueService? league = _league;
                MarketProbe("league_open", "\"scope\":\"" + LeagueIds.Of(scope) + "\",\"state\":\"" + StateId(league?.StateOf(scope))
                    + "\",\"pending\":" + (league?.PendingCount ?? 0).ToString(CultureInfo.InvariantCulture));
            }
        }

        private static string StateId(LeagueBoardState? state)
        {
            switch (state)
            {
                case LeagueBoardState.Fresh: return "fresh";
                case LeagueBoardState.Stale: return "stale";
                case LeagueBoardState.Loading: return "loading";
                default: return "offline";
            }
        }

        // La victoria de campaña (el bus, antes de que se vea la victoria): la cola y la línea de la victoria.
        private void OnLeagueLevelWon(LevelWon won)
        {
            LeagueService? league = _league;
            if (league == null || _flow == null || AccountDeleted || ProfileLocked)
            {
                return;
            }

            int points;
            using (Saves())
            {
                points = league.RecordCampaignWin(won.Level, _flow.HarvestTier);
            }

            if (_screen != null)
            {
                _screen.LeagueLineReserved = league.Active;
                _screen.SetLeagueVictory(won.Level, points);
                if (points > 0)
                {
                    _screen.LeagueAvatar = AvatarsEnabled ? AvatarPortraitNow() : null; // D-056 (Q-AV-4): el avatar, solo en esta línea
                }

            }

            if (DevProbe.Enabled && points > 0)
            {
                MarketProbe("league_win", "\"win_id\":\"" + LeagueRules.CampaignWinId(won.Level) + "\",\"points\":" + points.ToString(CultureInfo.InvariantCulture)
                    + ",\"pending\":" + league.PendingCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        // El Reto del río ganado (d:<día>): a la cola, sin línea (la victoria del Reto es otra pantalla).
        private void OnLeagueDailyWon(string day)
        {
            LeagueService? league = _league;
            if (league == null || AccountDeleted || ProfileLocked)
            {
                return;
            }

            int points;
            using (Saves())
            {
                points = league.RecordDailyWin(day);
            }

            if (DevProbe.Enabled && points > 0)
            {
                MarketProbe("league_win", "\"win_id\":\"" + LeagueRules.DailyWinId(day) + "\",\"points\":" + points.ToString(CultureInfo.InvariantCulture)
                    + ",\"pending\":" + league.PendingCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void OnLeagueWinsSent(LeagueSendReport report)
        {
            _leagueLastFailure = null;
            _analytics?.Log(AnalyticsEvent.Create("af_league_wins_sent").Add("count", report.Count).Add("accepted", report.Accepted)
                .Add("rejected", report.Rejected)); // 08 §8.2 (D-046)
            if (DevProbe.Enabled)
            {
                MarketProbe("league_wins_sent", "\"count\":" + report.Count.ToString(CultureInfo.InvariantCulture) + ",\"accepted\":"
                    + report.Accepted.ToString(CultureInfo.InvariantCulture) + ",\"rejected\":" + report.Rejected.ToString(CultureInfo.InvariantCulture));
            }

            if (_settingsScreen != null && _settingsScreen.Visible)
            {
                ShowLeagueCountry(); // el país de la semana llega con el primer envío aceptado
            }
        }

        private void OnLeagueUnavailable(LeagueFailure failure)
        {
            // Solo cuando cambia el motivo: un reintento sin red no repite af_league_unavailable (vuelve a valer tras una respuesta buena).
            if (_leagueLastFailure == failure)
            {
                return;
            }

            _leagueLastFailure = failure;
            _analytics?.Log(AnalyticsEvent.Create("af_league_unavailable").Add("reason", LeagueIds.Of(failure))); // 08 §8.2 (D-046)
            if (DevProbe.Enabled)
            {
                MarketProbe("league_unavailable", "\"reason\":\"" + LeagueIds.Of(failure) + "\"");
            }
        }

        private void OnLeagueBoardChanged(LeagueScope scope)
        {
            if (_league != null && _league.LastFailure(scope) == null)
            {
                _leagueLastFailure = null; // llegó una clasificación: el próximo fallo se vuelve a contar
            }

            if (scope == LeagueScope.Global)
            {
                RefreshLeagueRank(); // «Puesto {n}» de la tarjeta del muelle (D-050)
            }

            LeagueScreen? screen = _leagueScreen;
            if (screen != null && screen.Visible)
            {
                screen.Refresh();
                LeagueService? league = _league;
                if (league != null && league.Country != null && league.Cached(LeagueScope.Country) == null && scope == LeagueScope.Global)
                {
                    league.RequestBoard(LeagueScope.Country); // el país llegó con la clasificación global
                }
            }

            if (_settingsScreen != null && _settingsScreen.Visible)
            {
                ShowLeagueCountry();
            }
        }

        /// <summary>Ajustes: «País en la Liga: {país}» con la Liga activa y el país sabido; si no, nada.</summary>
        private void ShowLeagueCountry()
        {
            SettingsScreen? settings = _settingsScreen;
            if (settings == null)
            {
                return;
            }

            string? country = _league != null && _league.Active ? _league.Country : null;
            settings.SetLeagueCountry(country != null ? LeagueText.SettingsCountry(country) : null);
        }

        /// <summary>Al volver a primer plano: la cola que no salió.</summary>
        private void ResumeLeague()
        {
            if (AccountDeleted || ProfileLocked)
            {
                return;
            }

            using (Saves())
            {
                _league?.Flush();
            }

            SyncLeagueAvatar(); // D-056: el avatar que no salió (GameBootstrap.Avatars.cs)
        }

        /// <summary>Borrado de cuenta (08 §11): la cola y la caché de la Liga se van con el perfil y nada más se envía.</summary>
        private void WipeLeague()
        {
            _league?.Wipe();
            _leagueScreen?.Close();
            _leagueButton?.Hide();
            _leagueButtonShown = false;
        }

        /// <summary>Cada fotograma (lo llama <see cref="LeagueRunner"/>): el botón del inicio, la pantalla y el reintento de la cola.</summary>
        internal void TickLeague(float dtMs)
        {
            LeagueService? league = _league;
            if (league == null || AccountDeleted)
            {
                return;
            }

            _leagueScreen?.Tick(dtMs);
            SyncLeagueButton(league);
            if (league.PendingCount == 0 || league.Sending)
            {
                _leagueRetryMs = 0f;
                return;
            }

            _leagueRetryMs += dtMs;
            if (_leagueRetryMs >= RetryIntervalMs)
            {
                _leagueRetryMs = 0f;
                ResumeLeague();
            }
        }

        private void SyncLeagueButton(LeagueService league)
        {
            LeagueLobbyButton? button = _leagueButton;
            if (button == null || _lobby == null)
            {
                return;
            }

            bool show = LobbyClear() && !LeagueOpen && league.Active;
            bool moved = show && button.Remeasure();
            if (show == _leagueButtonShown && !moved)
            {
                return;
            }

            _leagueButtonShown = show;
            _dayCardsDirty = true;
            if (!show)
            {
                button.Hide();
                return;
            }

            RefreshLeagueRank();
            button.Show();
            PlaceDayCards(); // en la fila de las tarjetas del día del muelle, tras el Reto y la Bitácora (D-050, punto 6)
            if (DevProbe.Enabled && _probe != null)
            {
                _probe.Layout("lobby_league", 0, null, new[] { "league_tab" }, new[] { button.Rect });
            }
        }

        /// <summary><see cref="ILeagueApi"/> de un build sin backend: todo falla con <see cref="LeagueFailure.NotConfigured"/>.</summary>
        private sealed class UnavailableLeagueApi : ILeagueApi
        {
            public void SendWins(LeagueWinsRequest request, Action<LeagueResult<LeagueWinsResult>> onDone) =>
                onDone(LeagueResult<LeagueWinsResult>.Fail(LeagueFailure.NotConfigured));

            public void GetBoard(LeagueScope scope, Action<LeagueResult<LeagueBoard>> onDone) =>
                onDone(LeagueResult<LeagueBoard>.Fail(LeagueFailure.NotConfigured));

            public void SetAvatar(int avatar, Action<LeagueResult<LeagueAvatarResult>> onDone) =>
                onDone(LeagueResult<LeagueAvatarResult>.Fail(LeagueFailure.NotConfigured));
        }
    }
}
