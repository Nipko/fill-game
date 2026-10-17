#nullable enable
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la pantalla de arranque (tarea TI6d; 06 §2.2; D-016, T2-A; 09 §6), solo en desarrollo, para medir en el teléfono
    /// el primer toque en el nivel 1 (objetivo &lt; 20 s) y si cabe el segundo de <c>sloth_wake</c> de la primera apertura:
    /// <c>AF_STATE {"event":"boot","t":…,"first_open":true,"splash_ms":1630,"first_touch_ms":null,"load_ms":640,
    /// "splash_end_ms":2480,"sloth":"sloth_wake","sloth_ms":1000,"hold_ms":360,"wake_fits":true}</c> al cortarse la pantalla, y
    /// la misma línea otra vez en el primer toque del jugador, con <c>first_touch_ms</c> (ms desde el arranque en frío) y dónde
    /// fue: <c>"touch_screen":"level","touch_level":1</c> (o <c>lobby</c> y el nivel del botón Jugar).
    /// <list type="bullet">
    /// <item><c>first_open</c>: no había perfil; <c>splash_ms</c>: tiempo a la vista de la pantalla; <c>load_ms</c>: lo que tardó
    /// la carga con la pantalla delante; <c>splash_end_ms</c>: el corte, desde el arranque en frío.</item>
    /// <item><c>sloth</c>: la pose de TiTino (<c>sloth_wake</c> o <c>sloth_idle</c>; null sin su arte, en release);
    /// <c>sloth_ms</c>: lo que se vio de su pose; <c>hold_ms</c>: la espera añadida (0 salvo en la primera apertura con
    /// TiTino); <c>wake_fits</c>: false si hacía falta esperar y no cabía en los 20 s (se anota para el dueño).</item>
    /// </list>
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Escribe <c>AF_STATE boot</c> (al cortarse la pantalla de arranque y en el primer toque).</summary>
        public void Boot(BootReport report)
        {
            string? line = FormatBoot(report);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>La línea <c>AF_STATE boot</c> de <paramref name="report"/> (null fuera de desarrollo).</summary>
        public string? FormatBoot(BootReport report)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"boot\"");
            AppendTime(sb);
            sb.Append(",\"first_open\":").Append(report.FirstOpen ? "true" : "false");
            sb.Append(",\"splash_ms\":").Append(report.SplashMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"first_touch_ms\":");
            if (report.FirstTouchMs >= 0)
            {
                sb.Append(report.FirstTouchMs.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append("null");
            }

            sb.Append(",\"load_ms\":").Append(report.LoadMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"splash_end_ms\":").Append(report.SplashEndMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"sloth\":");
            if (report.SlothPose != null)
            {
                sb.Append('"').Append(report.SlothPose).Append('"');
            }
            else
            {
                sb.Append("null");
            }

            sb.Append(",\"sloth_ms\":").Append(report.SlothMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"hold_ms\":").Append(report.HoldMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"wake_fits\":").Append(report.WakeFits ? "true" : "false");
            if (report.FirstTouchScreen != null)
            {
                sb.Append(",\"touch_screen\":\"").Append(report.FirstTouchScreen).Append('"');
                sb.Append(",\"touch_level\":").Append(report.FirstTouchLevel.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append('}');
            return sb.ToString();
        }
    }
}
