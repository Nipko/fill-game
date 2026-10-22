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

        /// <summary>
        /// <c>af_label_shown</c> (08 §8.2; cierre de B11): la etiqueta Difícil o Muy difícil (<paramref name="label"/>, <c>hard</c> o
        /// <c>very_hard</c>) que se mostró al cargar el nivel, y de dónde vino (<paramref name="relabeled"/>: <c>override</c>, la de
        /// <c>lvl_label_override</c>; si no, <c>pack</c>). Con <c>normal</c> no hay etiqueta a la vista y no se envía nada.
        /// </summary>
        public static void LabelShown(AnalyticsService? analytics, string label, bool relabeled)
        {
            if (!string.Equals(label, LevelDefinition.LabelHard, StringComparison.Ordinal)
                && !string.Equals(label, LevelDefinition.LabelVeryHard, StringComparison.Ordinal))
            {
                return;
            }

            analytics?.Log(AnalyticsEvent.Create("af_label_shown")
                .Add("label", label)
                .Add("source", relabeled ? LabelSourceOverride : LabelSourcePack));
        }

        /// <summary><c>source</c> de <c>af_label_shown</c> con la etiqueta del paquete (08 §8.2).</summary>
        public const string LabelSourcePack = "pack";

        /// <summary><c>source</c> de <c>af_label_shown</c> con la etiqueta de <c>lvl_label_override</c> (08 §8.2).</summary>
        public const string LabelSourceOverride = "override";

        /// <summary>
        /// <c>af_no_canoes_choice</c> (04 §11; 08 §8.2; cierre de B11): la elección del jugador en Sin canoas, <c>choice</c> ∈
        /// {<c>wait</c>, <c>ad</c>, <c>seeds_refill</c>, <c>iap_infinite</c>, <c>daily_challenge</c>, <c>map</c>}.
        /// </summary>
        public static void NoCanoesChoice(AnalyticsService? analytics, string choice)
        {
            analytics?.Log(AnalyticsEvent.Create("af_no_canoes_choice").Add("choice", choice));
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
