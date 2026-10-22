#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// «Volver al Mapa del río» de Sin canoas (tarea B11; 04 §7.3, wireframe: la última línea, bajo «Mientras esperas: Jugar el
    /// Reto del río»; 04 §7.1, «Qué bloquea»: el Mapa del río sigue disponible con 0 canoas). Sustituye a «Volver al inicio»
    /// (tarea B2-3, que dejaba anotado que el wireframe dice Mapa del río). Cierra Sin canoas por el camino de siempre
    /// (<see cref="PressNoCanoesHome"/>: desde el inicio, al inicio; desde un nivel, sale al inicio) y abre el Mapa encima del
    /// inicio, como su pestaña (<see cref="MapOpenSource.Lobby"/>; el Mapa solo se abre con <c>max_level</c> ≥ 10, y Sin canoas solo
    /// existe desde el 31). Si el Mapa no se puede abrir (sin geometría, o con otra cosa encima del inicio), se queda en el inicio,
    /// como hacía el botón (la elección del jugador, <c>map</c>, se apunta igual). <c>af_map_open</c> lleva <c>source = lobby</c>
    /// (08 §8.2 no tiene otro valor para esto). Lo llama la pantalla por <see cref="IGameActions.PressNoCanoesMap"/>.
    /// <para><b>Todas las elecciones de Sin canoas</b> van a <c>af_no_canoes_choice</c> (04 §11; 08 §8.2; cierre de B11) a la vez
    /// (<see cref="TrackNoCanoesChoice"/>), y quedan en <see cref="LastNoCanoesChoice"/>: <c>wait</c> (la X o Atrás),
    /// <c>ad</c> («Canoa extra · Ver video», al empezar el vídeo), <c>seeds_refill</c> («Llenar canoas», con el cobro hecho),
    /// <c>iap_infinite</c> («Canoas infinitas 1 h», al empezar la compra), <c>daily_challenge</c> (E7) y <c>map</c>. La de
    /// «Volver al Mapa» escribe además la sonda <c>AF_STATE no_canoes_choice</c> (la del Reto, la suya de E7).
    /// <see cref="PressNoCanoesHome"/> no es una elección: es el camino por el que cierran el Reto y el Mapa.</para>
    /// <para>Con <c>biz_feature_lives</c> apagado (D-038) Sin canoas no se abre nunca, así que este botón no se ve ni hace nada.</para>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Valor de <c>choice</c> de <c>af_no_canoes_choice</c> para «Volver al Mapa del río» (04 §11).</summary>
        public const string NoCanoesMapChoice = "map";

        /// <summary><c>choice</c> = <c>wait</c>: la X o Atrás (esperar a la canoa que vuelve sola; 04 §11).</summary>
        public const string NoCanoesChoiceWait = "wait";

        /// <summary><c>choice</c> = <c>ad</c>: «Canoa extra · Ver video» (04 §11).</summary>
        public const string NoCanoesChoiceAd = "ad";

        /// <summary><c>choice</c> = <c>seeds_refill</c>: «Llenar canoas» con semillas (04 §11).</summary>
        public const string NoCanoesChoiceRefill = "seeds_refill";

        /// <summary><c>choice</c> = <c>iap_infinite</c>: «Canoas infinitas 1 h» (04 §11).</summary>
        public const string NoCanoesChoiceInfinite = "iap_infinite";

        /// <summary>Elecciones de Sin canoas enviadas a la analítica en esta escena (tests).</summary>
        internal int NoCanoesChoiceCount { get; private set; }

        // Sin canoas a la vista: sobre el inicio o en el nivel (FlowPhase.NoCanoes).
        private bool NoCanoesInFront
        {
            get
            {
                AppFlow? app = _app;
                if (app == null)
                {
                    return false;
                }

                return app.Screen == AppScreen.NoCanoes || (app.Screen == AppScreen.Level && _flow != null && _flow.Phase == FlowPhase.NoCanoes);
            }
        }

        // af_no_canoes_choice de una elección hecha en Sin canoas (04 §11; 08 §8.2; cierre de B11).
        private void TrackNoCanoesChoice(string choice)
        {
            LastNoCanoesChoice = choice;
            NoCanoesChoiceCount++;
            UiAnalytics.NoCanoesChoice(_analytics, choice);
        }

        private void TrackNoCanoesChoiceIfOpen(string choice)
        {
            if (NoCanoesInFront)
            {
                TrackNoCanoesChoice(choice);
            }
        }

        /// <summary>
        /// «Volver al Mapa del río» de Sin canoas: cierra Sin canoas (al inicio) y abre el Mapa encima. Devuelve si cerró Sin
        /// canoas; sin Sin canoas delante, nada. <see cref="NoCanoesMapOpened"/> dice si además se abrió el Mapa.
        /// </summary>
        public bool PressNoCanoesMap()
        {
            AppFlow? app = _app;
            if (app == null)
            {
                return false;
            }

            bool fromLobby = app.Screen == AppScreen.NoCanoes;
            bool fromLevel = _flow != null && _flow.Phase == FlowPhase.NoCanoes && app.Screen == AppScreen.Level;
            if ((!fromLobby && !fromLevel) || !PressNoCanoesHome())
            {
                NoCanoesMapOpened = false;
                return false;
            }

            TrackNoCanoesChoice(NoCanoesMapChoice);
            if (DevProbe.Enabled)
            {
                _probe?.Event("no_canoes_choice", "\"choice\":\"" + NoCanoesMapChoice + "\""); // AF_STATE, como la del Reto (E7)
            }

            NoCanoesMapOpened = app.Screen == AppScreen.Lobby && OpenRiverMap(MapOpenSource.Lobby);
            return true;
        }

        /// <summary>El último «Volver al Mapa del río» abrió el Mapa (si no, se quedó en el inicio; tests).</summary>
        public bool NoCanoesMapOpened { get; private set; }
    }
}
