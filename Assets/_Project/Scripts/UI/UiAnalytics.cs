#nullable enable
using System;
using AF.Analytics;
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// Eventos de pantalla de 08 §8.2 que emite la UI (bloque B7, parches B7-gamescreen, B7-rivermap y B7-bootstrap), con los
    /// mismos datos que las sondas <c>AF_STATE</c> de <see cref="DevProbe"/>, que no cambian. Cada método admite un servicio
    /// null (sin analítica: tests y pantallas sin arranque) y no hace nada.
    /// </summary>
    internal static class UiAnalytics
    {
        /// <summary><c>af_stop_arrive</c>: llegada a una parada, en la victoria o al ponerse al día en el Mapa.</summary>
        public static void StopArrive(AnalyticsService? analytics, int level, RiverStop stop, bool skipped, bool inMap)
        {
            analytics?.Log(AnalyticsEvent.Create("af_stop_arrive")
                .Add("stop_id", DevProbe.StopId(stop))
                .Add("world", stop.World)
                .Add("level", level)
                .Add("view", inMap ? "map" : "victory")
                .Add("skipped", skipped));
        }

        /// <summary><c>af_river_fact_shown</c>: el dato que se enseñó y los ms hasta cerrarlo (−1 si no se sabe).</summary>
        public static void RiverFactShown(AnalyticsService? analytics, string factId, float dismissedMs)
        {
            analytics?.Log(AnalyticsEvent.Create("af_river_fact_shown")
                .Add("fact_id", factId)
                .Add("dismissed_ms", (long)Math.Round(Math.Max(-1f, dismissedMs))));
        }

        /// <summary><c>af_fruit_discovered</c>: la tarjeta «Fruta nueva» del tinte <paramref name="tintId"/> salió.</summary>
        public static void FruitDiscovered(AnalyticsService? analytics, string tintId)
        {
            analytics?.Log(AnalyticsEvent.Create("af_fruit_discovered").Add("tint_id", tintId));
        }

        /// <summary><c>af_map_open</c>: <c>source</c>, la parada (<c>stop</c>, su número, −1 sin parada) y el mundo.</summary>
        public static void MapOpen(AnalyticsService? analytics, MapOpenSource source, RiverStop? stop, int world)
        {
            analytics?.Log(AnalyticsEvent.Create("af_map_open")
                .Add("source", DevProbe.MapSourceName(source))
                .Add("stop", stop?.N ?? -1)
                .Add("world", world));
        }

        /// <summary><c>af_map_close</c>: lo que se hizo en el Mapa.</summary>
        public static void MapClose(AnalyticsService? analytics, in MapCloseStats stats)
        {
            analytics?.Log(AnalyticsEvent.Create("af_map_close")
                .Add("dwell_ms", stats.DwellMs)
                .Add("stops_tapped", stats.StopsTapped)
                .Add("view_changes", stats.ViewChanges)
                .Add("north_up", stats.NorthUp));
        }
    }
}
