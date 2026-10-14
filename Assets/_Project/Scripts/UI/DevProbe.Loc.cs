#nullable enable
using System.Globalization;
using System.Text;
using AF.Localization;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda del idioma de la UI (D-018; tarea L2b): al arrancar, una línea
    /// <c>AF_STATE {"event":"loc","t":…,"locale":"es-419","source":"fallback","culture":"es-CO"}</c> con el idioma resuelto
    /// (<see cref="LocResolution.Locale"/>), de dónde salió (<c>dev</c>, <c>saved</c>, <c>device</c>, <c>default</c> o
    /// <c>fallback</c>; <see cref="LocSource"/>) y la cultura que da el runtime (<see cref="CultureInfo.CurrentCulture"/>; vacía
    /// si es la invariante, que es lo que hay que medir en IL2CPP). Con <c>af_lang</c> (<c>--es af_lang en-XA</c>), source =
    /// <c>dev</c>. Solo en desarrollo.
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Escribe la línea <c>AF_STATE loc</c> de <paramref name="resolution"/>.</summary>
        public void LocaleState(LocResolution resolution)
        {
            string? line = FormatLocaleState(resolution, CultureInfo.CurrentCulture.Name);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE loc</c>; null si la sonda no escribe.</summary>
        public string? FormatLocaleState(LocResolution resolution, string culture)
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
            sb.Append(",\"culture\":\"").Append(culture ?? string.Empty).Append("\"}");
            return sb.ToString();
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
