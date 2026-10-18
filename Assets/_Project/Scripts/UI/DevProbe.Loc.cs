#nullable enable
using System.Globalization;
using System.Text;
using AF.Localization;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda del idioma de la UI (D-018; tareas L2b y L3): al arrancar, y al elegir idioma en Ajustes, una línea
    /// <c>AF_STATE {"event":"loc","t":…,"locale":"es-419","source":"fallback","culture":"es-CO","device":"es-CO","device_src":"jni","numbers":"es-CO"}</c>
    /// con el idioma resuelto (<see cref="LocResolution.Locale"/>), de dónde salió (<c>dev</c>, <c>saved</c>, <c>device</c>,
    /// <c>default</c> o <c>fallback</c>; <see cref="LocSource"/>), la cultura que da el runtime
    /// (<see cref="CultureInfo.CurrentCulture"/>; vacía si es la invariante, que es lo que hay que medir en IL2CPP), la etiqueta
    /// que leyó el teléfono (<c>device</c>; vacía si no dio ninguna) y de dónde (<c>device_src</c>: <c>jni</c>, <c>culture</c> o
    /// <c>manual</c>; <see cref="AndroidDeviceLocale.SourceKey"/>) y la fila de cifras en uso (<c>numbers</c>,
    /// <see cref="Loc.NumberFormat"/>: la región del teléfono o, sin ella, es-CO [P]; Q8-A). Con <c>af_lang</c>
    /// (<c>--es af_lang en-XA</c>), source = <c>dev</c>. Solo en desarrollo.
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Escribe la línea <c>AF_STATE loc</c> de <paramref name="resolution"/> (sin datos del teléfono).</summary>
        public void LocaleState(LocResolution resolution)
        {
            string? line = FormatLocaleState(resolution, CultureInfo.CurrentCulture.Name);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>
        /// Escribe la línea <c>AF_STATE loc</c> de <paramref name="resolution"/> con la etiqueta <paramref name="deviceTag"/> que
        /// leyó <paramref name="device"/> y las cifras en uso (tarea L3).
        /// </summary>
        public void LocaleState(LocResolution resolution, AF.Platform.IDeviceLocale device, string deviceTag)
        {
            string? line = FormatLocaleState(resolution, CultureInfo.CurrentCulture.Name, deviceTag, AndroidDeviceLocale.SourceKey(device), Loc.NumberFormat.Tag);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE loc</c> sin datos del teléfono; null si la sonda no escribe.</summary>
        public string? FormatLocaleState(LocResolution resolution, string culture)
        {
            return FormatLocaleState(resolution, culture, null, null, null);
        }

        /// <summary>
        /// Texto de la línea <c>AF_STATE loc</c>; null si la sonda no escribe. Con <paramref name="deviceSource"/> (no null), lleva
        /// además <c>device</c>, <c>device_src</c> y <c>numbers</c>.
        /// </summary>
        public string? FormatLocaleState(LocResolution resolution, string culture, string? deviceTag, string? deviceSource, string? numbers)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"loc\"");
            AppendTime(sb);
            sb.Append(",\"locale\":\"").Append(resolution.Locale).Append('"');
            sb.Append(",\"source\":\"").Append(SourceKey(resolution.Source)).Append('"');
            sb.Append(",\"culture\":\"").Append(JsonSafe(culture)).Append('"');
            if (deviceSource != null)
            {
                sb.Append(",\"device\":\"").Append(JsonSafe(deviceTag)).Append('"');
                sb.Append(",\"device_src\":\"").Append(deviceSource).Append('"');
                sb.Append(",\"numbers\":\"").Append(JsonSafe(numbers)).Append('"');
            }

            sb.Append('}');
            return sb.ToString();
        }

        // Una etiqueta del sistema en una cadena JSON: sin comillas ni barras, que no lleva ninguna etiqueta BCP 47 válida.
        private static string JsonSafe(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value!.IndexOf('"') < 0 && value.IndexOf('\\') < 0 ? value : value.Replace("\"", string.Empty).Replace("\\", string.Empty);
        }

        /// <summary>Nombre de <paramref name="source"/> en la sonda.</summary>
        public static string SourceKey(LocSource source)
        {
            switch (source)
            {
                case LocSource.Dev: return "dev";
                case LocSource.Saved: return "saved";
                case LocSource.Device: return "device";
                case LocSource.Default: return "default";
                default: return "fallback";
            }
        }
    }
}
