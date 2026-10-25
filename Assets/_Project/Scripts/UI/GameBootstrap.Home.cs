#nullable enable
using AF.Economy;
using AF.LiveOps;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El inicio «el muelle del río» (D-050; 06 §4.7, fila Lobby): lo que el arranque le da a la escena del inicio.
    /// <list type="bullet">
    /// <item><b>Las tarjetas del día</b> (punto 6): Reto del río, Bitácora y Liga (si está activa), cada una en su lienzo, en la fila
    /// que deja <see cref="LobbyScreen"/> bajo «Jugar» (<see cref="LobbyScreen.DayCardSlot"/>): con 2, centradas; todas con el mismo
    /// alto, el que pida la de texto más largo (02 §12). Se recolocan solo cuando cambia algo (<see cref="PlaceDayCards"/>).</item>
    /// <item><b>Lo que cuenta TiTino</b> (punto 1): si el Reto de hoy está sin jugar (<see cref="HomeDailyPending"/>) y lo que falta
    /// para la próxima Canasta (<see cref="HomeLevelsToBasket"/>).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private bool _dayCardsDirty = true;
        private int _dayCardsLayout = -1;
        private int _dayCardsCount = -1;

        /// <summary>
        /// El Reto del río de hoy está sin jugar (la tarjeta dice «Jugar»): TiTino lo cuenta en su bocadillo (D-050, punto 1).
        /// </summary>
        internal bool HomeDailyPending => _dailyService != null && _dailyScreen != null && _progress != null
            && _dailyService.CardState(_progress.MaxLevel) == DailyCardState.Play;

        /// <summary>
        /// El inicio está delante y despejado (sin Ajustes, el Mercado, el Reto, la Liga, una oferta ni el Mapa encima): solo entonces
        /// corre la pausa entre los mensajes de TiTino, para que no se gasten sin verse.
        /// </summary>
        internal bool HomeNotesClear => LobbyClear();

        /// <summary>
        /// Niveles que faltan para la próxima Canasta (la de la parada siguiente, 06 §4.4 y §4.5), contando el de «Jugar»; −1 sin
        /// Canasta (antes de su desbloqueo o tras el fin de contenido).
        /// </summary>
        internal int HomeLevelsToBasket
        {
            get
            {
                if (_app == null || !_app.PlayLevelAvailable)
                {
                    return -1;
                }

                int play = _app.PlayLevel;
                int per = AF.Levels.RiverStopCatalog.LevelsPerStop;
                int next = ((play + per - 1) / per) * per;
                if (next < per || MapBasketFor(next).Kind == BasketKind.None)
                {
                    return -1;
                }

                return next - play + 1;
            }
        }

        /// <summary>
        /// Coloca las tarjetas del día en la fila del muelle (D-050, punto 6), en orden Reto, Bitácora y Liga: cuenta las que existen
        /// para el jugador (no oculta por estado o por su sistema), le pide al inicio el ancho de cada una y su sitio, y a todas el
        /// mismo alto. Solo hace algo si cambió el estado, las tarjetas o el sitio que deja el inicio (sin asignaciones si no).
        /// </summary>
        internal void PlaceDayCards()
        {
            LobbyScreen? lobby = _lobby;
            if (lobby == null)
            {
                return;
            }

            bool daily = _dailyCard != null && _dailyCard.State != DailyCardState.Hidden;
            bool logbook = _logbookCard != null && _logbookCard.State != LoginCalendarState.Hidden;
            bool league = _leagueButton != null && _league != null && _league.Active && !AccountDeleted;
            int n = (daily ? 1 : 0) + (logbook ? 1 : 0) + (league ? 1 : 0);
            if (!_dayCardsDirty && n == _dayCardsCount && lobby.LayoutVersion == _dayCardsLayout)
            {
                return;
            }

            _dayCardsDirty = false;
            _dayCardsCount = n;
            lobby.DayCardCount = n; // el muelle sin tarjetas lleva la caja de cuias de Capi (antes del 10)
            _dayCardsLayout = lobby.LayoutVersion;
            if (n == 0)
            {
                return;
            }

            // Todas del mismo alto, el de la que más pide; si la fila no cabe entre «Jugar» y las pestañas (16:9 con el texto grande),
            // primero el Reto sin la fecha en el título (06 §4.1: la sigue leyendo TalkBack) y, si ni así, sin la línea de estado en
            // ninguna (el sello y el punto del icono la dicen) [P].
            float w = lobby.DayCardWidth(n);
            float max = lobby.DayCardMaxHeight;
            _dailyCard?.SetDated(true);
            SetDayCardsCompact(false);
            float h = DayCardsHeight(daily, logbook, league, w);
            if (h > max && daily)
            {
                _dailyCard!.SetDated(false);
                h = DayCardsHeight(daily, logbook, league, w);
            }

            if (h > max)
            {
                SetDayCardsCompact(true);
                h = DayCardsHeight(daily, logbook, league, w);
            }

            h = Mathf.Max(h, LobbyScreen.MinTouchDp);
            int i = 0;
            if (daily)
            {
                Rect r = lobby.DayCardSlot(i++, n, h);
                _dailyCard!.Place(r.x, r.y, r.width, r.height);
            }

            if (logbook)
            {
                Rect r = lobby.DayCardSlot(i++, n, h);
                _logbookCard!.Place(r.x, r.y, r.width, r.height);
            }

            if (league)
            {
                Rect r = lobby.DayCardSlot(i, n, h);
                _leagueButton!.Place(r.x, r.y, r.width, r.height);
            }

            if (DevProbe.Enabled && _probe != null)
            {
                // AF_LAYOUT lobby_cards: las tarjetas del día del muelle (daily_card, logbook_card, league_card), solo al recolocarlas.
                var ids = new System.Collections.Generic.List<string>(3);
                var rects = new System.Collections.Generic.List<RectTransform>(3);
                if (daily)
                {
                    ids.Add("daily_card");
                    rects.Add(_dailyCard!.Root);
                }

                if (logbook)
                {
                    ids.Add("logbook_card");
                    rects.Add(_logbookCard!.Root);
                }

                if (league)
                {
                    ids.Add("league_card");
                    rects.Add(_leagueButton!.Rect);
                }

                _probe.Layout("lobby_cards", 0, null, ids.ToArray(), rects.ToArray());
            }
        }

        // El alto que pide la fila: el de la tarjeta que más pide a ese ancho.
        private float DayCardsHeight(bool daily, bool logbook, bool league, float w)
        {
            float h = 0f;
            if (daily)
            {
                h = Mathf.Max(h, _dailyCard!.HeightFor(w));
            }

            if (logbook)
            {
                h = Mathf.Max(h, _logbookCard!.HeightFor(w));
            }

            if (league)
            {
                h = Mathf.Max(h, _leagueButton!.HeightFor(w));
            }

            return h;
        }

        private void SetDayCardsCompact(bool on)
        {
            _dailyCard?.SetCompact(on);
            _logbookCard?.SetCompact(on);
            _leagueButton?.SetCompact(on);
        }

        /// <summary>Solo tests: vuelve a colocar las tarjetas del día aunque nada haya cambiado (tras poner un texto a mano).</summary>
        internal void PlaceDayCardsForTests()
        {
            _dayCardsDirty = true;
            PlaceDayCards();
        }

        // «Puesto {n}» de la tarjeta de la Liga con la clasificación global de esta semana (al llegar una o al enseñar la tarjeta,
        // nunca por fotograma: Cached arma el id de la semana).
        private void RefreshLeagueRank()
        {
            LeagueService? league = _league;
            LeagueLobbyButton? button = _leagueButton;
            if (league == null || button == null)
            {
                return;
            }

            LeagueCacheEntry? cached = league.Cached(LeagueScope.Global);
            if (button.SetRank(cached?.Board.Me.Rank))
            {
                _dayCardsDirty = true;
                PlaceDayCards();
            }
        }
    }
}
