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
    /// como hacía el botón (la elección del jugador, <c>map</c>, se apunta igual). Deja la elección de <c>af_no_canoes_choice</c> (04 §11; 08 §8.2), <c>choice = map</c>, en
    /// <see cref="LastNoCanoesChoice"/> y en la sonda <c>AF_STATE no_canoes_choice</c>, como la del Reto (E7): ninguna elección de
    /// Sin canoas llega aún a <c>AF.Analytics</c>, y mandar solo esta sesgaría el embudo (tarea del integrador: todas a la vez).
    /// <c>af_map_open</c> lleva <c>source = lobby</c> (08 §8.2 no tiene otro valor para esto).
    /// <para>Con <c>biz_feature_lives</c> apagado (D-038) Sin canoas no se abre nunca, así que este botón no se ve ni hace nada.</para>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Valor de <c>choice</c> de <c>af_no_canoes_choice</c> para «Volver al Mapa del río» (04 §11).</summary>
        public const string NoCanoesMapChoice = "map";

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

            LastNoCanoesChoice = NoCanoesMapChoice;
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
