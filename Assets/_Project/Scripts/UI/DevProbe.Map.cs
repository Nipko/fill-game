#nullable enable
using System.Globalization;
using System.Text;
using AF.Levels;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sondas del Mapa del río en la victoria (D-013; propuesta del Mapa §11.5; tarea M1.6). Hasta que exista <c>AF.Analytics</c>
    /// (08 §8.2), los eventos van al log de los builds de desarrollo como <c>AF_STATE</c>:
    /// <list type="bullet">
    /// <item><c>arrival</c>: la franja de llegada cuando empieza (<c>phase</c> = <c>start</c>, tras la subida del panel o con el
    /// toque que la adelanta) y al terminar (<c>complete</c>, con <c>by</c> = <c>time</c>, <c>tap</c> o <c>close</c>). Campos de
    /// docs/dev/prototipo-v0.md: <c>level</c>, <c>stop</c> (la k; <c>-1</c> en el dato desplazado), <c>id</c> (el id del sitio;
    /// vacío en el dato desplazado), <c>fact_id</c> (la clave del dato que se ve, o null) y <c>welcome</c>; además <c>phase</c>,
    /// <c>by</c>, <c>kind</c> (<c>arrival</c>, <c>place</c> o <c>deferred</c>), <c>world</c>, <c>reduce_motion</c>, el alto de lo
    /// que se ve (<c>shown_dp</c>) y el de la ranura (<c>slot_dp</c>);</item>
    /// <item><c>stop_arrive</c> (<c>af_stop_arrive</c>, 08 §8.2): <c>stop_id</c> (el id de parada <c>s{nn}</c>, glosario §11),
    /// <c>stop</c> (la k), <c>world</c>, <c>level</c>, <c>view</c> (<c>victory</c> en la franja de la victoria; <c>map</c> al
    /// ponerse al día en el Mapa, M3.5) y <c>skipped</c>;</item>
    /// <item><c>river_fact_shown</c> (<c>af_river_fact_shown</c>, 08 §8.2): <c>level</c>, <c>fact_id</c> (la clave del dato) y
    /// <c>dismissed_ms</c>, desde el punto común de la ranura (lo comparte la línea de fruta, F3);</item>
    /// <item><c>map_open</c> (<c>af_map_open</c>, 08 §8.2; tarea M3.3): <c>source</c> (<c>lobby</c>, <c>pause</c>, <c>victory</c>,
    /// <c>world_change</c>, <c>l10</c> o <c>content_end</c>), <c>level</c> (el de «Jugar»), <c>stop</c> (la k de su parada; −1 sin
    /// ella), <c>stop_id</c>, <c>world</c>, <c>view</c> y <c>zoom</c>;</item>
    /// <item><c>map_close</c> (<c>af_map_close</c>, 08 §8.2; tarea M3.3): <c>source</c>, <c>dwell_ms</c>, <c>stops_tapped</c>,
    /// <c>view_changes</c>, <c>north_up</c> y <c>map_seen_level</c> tras cerrar.</item>
    /// </list>
    /// Solo claves e identificadores, nunca texto visible (regla de localización).
    /// </summary>
    public sealed partial class DevProbe
    {
        internal void Arrival(string phase, ArrivalStrip strip, float slotDp)
        {
            string? line = FormatArrival(phase, strip, slotDp);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE arrival</c>; null si la sonda no escribe.</summary>
        internal string? FormatArrival(string phase, ArrivalStrip strip, float slotDp)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"arrival\"");
            AppendTime(sb);
            sb.Append(",\"level\":").Append(strip.Level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"phase\":\"").Append(phase).Append('"');
            if (strip.CompletedBy.Length > 0)
            {
                sb.Append(",\"by\":\"").Append(strip.CompletedBy).Append('"');
            }

            sb.Append(",\"kind\":\"").Append(KindName(strip.Kind)).Append('"');
            RiverStop? stop = strip.Stop;
            sb.Append(",\"stop\":").Append((stop?.N ?? -1).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"id\":\"").Append(stop?.Id ?? string.Empty).Append('"');
            sb.Append(",\"world\":").Append((stop?.World ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"fact_id\":");
            AppendKey(sb, strip.ShownFactKey);
            sb.Append(",\"welcome\":").Append(strip.Welcome ? "true" : "false");
            sb.Append(",\"reduce_motion\":").Append(strip.ReduceMotion ? "true" : "false");
            sb.Append(",\"shown_dp\":").Append(Mathf.RoundToInt(strip.ShownHeightDp).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"slot_dp\":").Append(Mathf.RoundToInt(slotDp).ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_STATE stop_arrive</c> (<c>af_stop_arrive</c> de la propuesta del Mapa §11.5): en la franja de la victoria
        /// (<c>view</c> = <c>victory</c>) o, con <paramref name="inMap"/>, la llegada sin ver que recorre la canoa al ponerse al
        /// día en el Mapa (<c>view</c> = <c>map</c>; 08 §8.2; M3.5).
        /// </summary>
        public void StopArrive(int level, RiverStop stop, bool skipped, bool inMap = false)
        {
            string? line = FormatStopArrive(level, stop, skipped, inMap);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE stop_arrive</c>; null si la sonda no escribe.</summary>
        public string? FormatStopArrive(int level, RiverStop stop, bool skipped, bool inMap = false)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"stop_arrive\"");
            AppendTime(sb);
            sb.Append(",\"stop_id\":\"").Append(StopId(stop)).Append('"');
            sb.Append(",\"stop\":").Append(stop.N.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"world\":").Append(stop.World.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"view\":\"").Append(inMap ? "map" : "victory").Append('"');
            sb.Append(",\"skipped\":").Append(skipped ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_STATE river_fact_shown</c> (<c>af_river_fact_shown</c>, 08 §8.2): el dato que enseñó la ranura del Dato del
        /// río en la victoria de <paramref name="level"/>, con su clave y los milisegundos desde la victoria hasta cerrarla.
        /// </summary>
        public void RiverFactShown(int level, string factId, float dismissedMs)
        {
            string? line = FormatRiverFactShown(level, factId, dismissedMs);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE river_fact_shown</c>; null si la sonda no escribe.</summary>
        public string? FormatRiverFactShown(int level, string factId, float dismissedMs)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"river_fact_shown\"");
            AppendTime(sb);
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"fact_id\":");
            AppendKey(sb, factId);
            sb.Append(",\"dismissed_ms\":").Append(Mathf.RoundToInt(Mathf.Max(-1f, dismissedMs)).ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Línea <c>AF_STATE map_open</c> (<c>af_map_open</c> de 08 §8.2).</summary>
        public void MapOpen(MapOpenSource source, int level, RiverStop? stop, int world, MapView view, float zoom)
        {
            string? line = FormatMapOpen(source, level, stop, world, view, zoom);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE map_open</c>; null si la sonda no escribe.</summary>
        public string? FormatMapOpen(MapOpenSource source, int level, RiverStop? stop, int world, MapView view, float zoom)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"map_open\"");
            AppendTime(sb);
            sb.Append(",\"source\":\"").Append(MapSourceName(source)).Append('"');
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stop\":").Append((stop?.N ?? -1).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stop_id\":");
            AppendKey(sb, stop != null ? StopId(stop) : null);
            sb.Append(",\"world\":").Append(world.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"view\":\"").Append(view == MapView.Basin ? "basin" : "world").Append('"');
            sb.Append(",\"zoom\":").Append(zoom.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Línea <c>AF_STATE map_close</c> (<c>af_map_close</c> de 08 §8.2).</summary>
        public void MapClose(in MapCloseStats stats, int mapSeenLevel)
        {
            string? line = FormatMapClose(stats, mapSeenLevel);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE map_close</c>; null si la sonda no escribe.</summary>
        public string? FormatMapClose(in MapCloseStats stats, int mapSeenLevel)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"map_close\"");
            AppendTime(sb);
            sb.Append(",\"source\":\"").Append(MapSourceName(stats.Source)).Append('"');
            sb.Append(",\"dwell_ms\":").Append(stats.DwellMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stops_tapped\":").Append(stats.StopsTapped.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"view_changes\":").Append(stats.ViewChanges.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"north_up\":").Append(stats.NorthUp ? "true" : "false");
            sb.Append(",\"map_seen_level\":").Append(mapSeenLevel.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Valor de <c>source</c> de <c>af_map_open</c> (08 §8.2) para <paramref name="source"/>.</summary>
        public static string MapSourceName(MapOpenSource source)
        {
            switch (source)
            {
                case MapOpenSource.Pause: return "pause";
                case MapOpenSource.Victory: return "victory";
                case MapOpenSource.WorldChange: return "world_change";
                case MapOpenSource.L10: return "l10";
                case MapOpenSource.ContentEnd: return "content_end";
                default: return "lobby";
            }
        }

        /// <summary>Id de parada <c>s{nn}</c> de <paramref name="stop"/> (glosario §11; 08 §4.5): <c>s02</c> en el 20, <c>s08</c> en el 80.</summary>
        internal static string StopId(RiverStop stop)
        {
            return "s" + stop.N.ToString("D2", CultureInfo.InvariantCulture);
        }

        // Una clave de las tablas (minúsculas, cifras, puntos y guiones bajos) como cadena JSON, o null.
        private static void AppendKey(StringBuilder sb, string? key)
        {
            if (key == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"').Append(JsonSafe(key)).Append('"');
        }

        private static string KindName(ArrivalKind kind)
        {
            switch (kind)
            {
                case ArrivalKind.Arrival: return "arrival";
                case ArrivalKind.Place: return "place";
                case ArrivalKind.Deferred: return "deferred";
                default: return "none";
            }
        }
    }
}
