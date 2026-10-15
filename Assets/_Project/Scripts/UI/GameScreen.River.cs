#nullable enable
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// Parcial del Mapa del río en la pantalla de nivel (D-013; 08 §2.2 y §4.5; tarea M1.2): de momento solo guarda el catálogo de
    /// paradas que le pasa <see cref="GameBootstrap"/>. Nada visible cambia; lo usarán los fondos de parada (M1.4), el árbitro de
    /// la victoria (M1.3) y la llegada (M1.6).
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>
        /// Paradas del Mapa del río (<c>af-river-stops/1</c>; null = sin el archivo o inválido: el juego sigue como sin el Mapa).
        /// </summary>
        public RiverStopCatalog? RiverStops { get; set; }
    }
}
