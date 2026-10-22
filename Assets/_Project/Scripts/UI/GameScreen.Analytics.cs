#nullable enable
using AF.Analytics;

namespace AF.UI
{
    /// <summary>
    /// Analítica de la pantalla de nivel (bloque B7, parche B7-gamescreen): la llegada a una parada, el Dato del río y la
    /// tarjeta «Fruta nueva» (<c>af_stop_arrive</c>, <c>af_river_fact_shown</c> y <c>af_fruit_discovered</c>, 08 §8.2), en los
    /// mismos puntos que sus sondas <c>AF_STATE</c>. Null (por defecto) = sin analítica.
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>El servicio de analítica (lo pone el arranque).</summary>
        public AnalyticsService? Analytics { get; set; }
    }
}
