#nullable enable
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la línea de fruta de Frutas del río en la victoria (D-017; propuesta de Frutas §2.10; tarea F3). Hasta que exista
    /// <c>AF.Analytics</c> (08 §8.2), va al log de los builds de desarrollo como <c>AF_STATE</c>:
    /// <list type="bullet">
    /// <item><c>fruit_line</c>, al enseñarse la línea en una victoria: <c>level</c>, <c>fact_id</c> (<c>&lt;tinte&gt;.&lt;kk&gt;</c>, el
    /// de <c>af_river_fact_shown</c>), <c>key</c> (la clave del texto, <c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>), <c>k</c> y <c>n</c> (la
    /// línea de la serie y sus líneas), <c>sealed</c> (la última de la serie, con el sello), <c>wrapped</c> (el contador bajó a una
    /// segunda fila de la cabecera), <c>reduce_motion</c>, <c>text_dp</c> (el tamaño del texto con la escala, 14 sp o menos si no
    /// cabía), <c>shown_dp</c> (el alto de lo que se ve) y <c>slot_dp</c> (el de la ranura).</item>
    /// </list>
    /// Y <c>AF_PERF slot_measure</c> (revisión de F3), al cargar un nivel si las medidas de la ranura del Dato del río no salieron
    /// de la caché (la primera vez por idioma, escala y ancho): <c>level</c>, <c>locale</c>, <c>scale</c>, <c>ms</c> (todo lo que
    /// costó medir la ranura en el hilo principal), <c>fruit_ms</c> (la línea de fruta más alta del calendario) y <c>slot_dp</c>.
    /// Sirve para medirlo en el teléfono con <c>adb logcat -s Unity</c>.
    /// <c>river_fact_shown</c> sale del punto común de la ranura (<see cref="RiverFactShown"/>, DevProbe.Map.cs) con el
    /// <c>fact_id</c> de la línea de fruta. Solo claves e identificadores, nunca texto visible (regla de localización).
    /// </summary>
    public sealed partial class DevProbe
    {
        internal void FruitLine(FactLine line, float slotDp)
        {
            string? text = FormatFruitLine(line, slotDp);
            if (text != null)
            {
                Debug.Log(text);
            }
        }

        internal void SlotMeasure(int level, string locale, float scale, double ms, double fruitMs, float slotDp)
        {
            string? text = FormatSlotMeasure(level, locale, scale, ms, fruitMs, slotDp);
            if (text != null)
            {
                Debug.Log(text);
            }
        }

        /// <summary>Texto de la línea <c>AF_PERF slot_measure</c>; null si la sonda no escribe.</summary>
        internal string? FormatSlotMeasure(int level, string locale, float scale, double ms, double fruitMs, float slotDp)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_PERF {\"event\":\"slot_measure\"");
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"locale\":");
            AppendKey(sb, locale);
            sb.Append(",\"scale\":").Append(scale.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(",\"ms\":").Append(ms.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(",\"fruit_ms\":").Append(fruitMs.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(",\"slot_dp\":").Append(Mathf.RoundToInt(slotDp).ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Texto de la línea <c>AF_STATE fruit_line</c>; null si la sonda no escribe o la línea no se enseña.</summary>
        internal string? FormatFruitLine(FactLine line, float slotDp)
        {
            if (!Enabled || !line.Shown)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"fruit_line\"");
            AppendTime(sb);
            sb.Append(",\"level\":").Append(line.Level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"fact_id\":");
            AppendKey(sb, line.ShownFactId);
            sb.Append(",\"key\":");
            AppendKey(sb, line.Key);
            sb.Append(",\"k\":").Append(line.Position.K.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"n\":").Append(line.Position.Lines.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"sealed\":").Append(line.Sealed ? "true" : "false");
            sb.Append(",\"wrapped\":").Append(line.HeaderWrapped ? "true" : "false");
            sb.Append(",\"reduce_motion\":").Append(line.ReduceMotion ? "true" : "false");
            sb.Append(",\"text_dp\":").Append(line.TextSizeDp.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(",\"shown_dp\":").Append(Mathf.RoundToInt(line.ShownHeightDp).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"slot_dp\":").Append(Mathf.RoundToInt(slotDp).ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
