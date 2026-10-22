#nullable enable
using AF.Analytics;

namespace AF.UI
{
    /// <summary>
    /// Analítica del Mapa del río (bloque B7, parche B7-rivermap): las llegadas al ponerse al día y el Dato del río de la
    /// secuencia del 10 (<c>af_stop_arrive</c> con <c>view</c> = <c>map</c> y <c>af_river_fact_shown</c>, 08 §8.2), en los mismos
    /// puntos que sus sondas. <c>af_map_open</c> y <c>af_map_close</c> salen del arranque (parche B7-bootstrap), donde se abre y
    /// se cierra el Mapa. Null (por defecto) = sin analítica.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>El servicio de analítica (lo pone el arranque).</summary>
        public AnalyticsService? Analytics { get; set; }
    }
}
