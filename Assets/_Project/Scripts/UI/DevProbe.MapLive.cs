#nullable enable
using System;
using System.Globalization;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// La sonda del Mapa v2 (D-069, M2-1: «60 fps en el teléfono de pruebas y 30 fps o más en gama baja, medidos con la sonda
    /// <c>AF_PERF</c> antes de cerrar M2-1»; propuesta del Mapa v2 §7.2), solo en desarrollo:
    /// <list type="bullet">
    /// <item><description><c>AF_PERF {"event":"map_frames","world":…,"view":…,"frames":…,"rest_frames":…,"avg_ms":…,"p95_ms":…,
    /// "max_ms":…,"fps":…,"veg_quads":…,"live_tex_kib":…}</c> al cerrar el Mapa v2: los fotogramas en que el Mapa pinta a 60 fps
    /// (arrastre, viaje, «ponerse al día»; los últimos <see cref="RiverMapScreen.MapFrameSamples"/>) y los de reposo, que solo se
    /// cuentan (pinta a <c>perf_target_fps_menus</c>);</description></item>
    /// <item><description><c>AF_PERF mem</c> con <c>at</c> = <c>map</c> lleva además <c>map_v2</c>, <c>live_tex_kib</c> y
    /// <c>veg_points</c>, y sale otra con <c>at</c> = <c>map_close</c> al cerrarlo: la diferencia de <c>tex_mib</c> dice si el arte
    /// del Mapa se soltó.</description></item>
    /// </list>
    /// Para leerlas: <c>adb logcat -d -s Unity | grep -E "AF_PERF \{\"event\":\"(map_frames|mem)\""</c>.
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Línea <c>AF_PERF map_frames</c> con los tiempos <paramref name="sortedMs"/> ya ordenados de menor a mayor.</summary>
        public static string FormatMapFrames(int world, string view, float[] sortedMs, int restFrames, int vegQuads, long liveTexKib)
        {
            int n = sortedMs.Length;
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                sum += sortedMs[i];
            }

            double avg = n > 0 ? sum / n : 0;
            float p95 = n > 0 ? sortedMs[Math.Min(n - 1, Math.Max(0, (int)Math.Ceiling(0.95 * n) - 1))] : 0f;
            float max = n > 0 ? sortedMs[n - 1] : 0f;
            var sb = new StringBuilder(220);
            sb.Append("AF_PERF {\"event\":\"map_frames\",\"world\":").Append(world.ToString(CultureInfo.InvariantCulture))
                .Append(",\"view\":\"").Append(view).Append('"')
                .Append(",\"frames\":").Append(n.ToString(CultureInfo.InvariantCulture))
                .Append(",\"rest_frames\":").Append(restFrames.ToString(CultureInfo.InvariantCulture))
                .Append(",\"avg_ms\":").Append(avg.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(",\"p95_ms\":").Append(p95.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(",\"max_ms\":").Append(max.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(",\"fps\":").Append((avg > 0 ? 1000.0 / avg : 0).ToString("0.00", CultureInfo.InvariantCulture))
                .Append(",\"veg_quads\":").Append(vegQuads.ToString(CultureInfo.InvariantCulture))
                .Append(",\"live_tex_kib\":").Append(liveTexKib.ToString(CultureInfo.InvariantCulture))
                .Append('}');
            return sb.ToString();
        }
    }
}
