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
    /// <c>stop</c> (la k), <c>world</c>, <c>level</c>, <c>view</c> = <c>victory</c> y <c>skipped</c>;</item>
    /// <item><c>river_fact_shown</c> (<c>af_river_fact_shown</c>, 08 §8.2): <c>level</c>, <c>fact_id</c> (la clave del dato) y
    /// <c>dismissed_ms</c>, desde el punto común de la ranura (lo comparte la línea de fruta, F3).</item>
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

        /// <summary>Línea <c>AF_STATE stop_arrive</c> (<c>af_stop_arrive</c> de la propuesta del Mapa §11.5).</summary>
        public void StopArrive(int level, RiverStop stop, bool skipped)
        {
            string? line = FormatStopArrive(level, stop, skipped);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE stop_arrive</c>; null si la sonda no escribe.</summary>
        public string? FormatStopArrive(int level, RiverStop stop, bool skipped)
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
            sb.Append(",\"view\":\"victory\"");
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
