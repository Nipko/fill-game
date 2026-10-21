#nullable enable
using System.Globalization;
using System.Text;
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Sonda del Salto del Boto (bloque B3): <c>AF_STATE af_skip_used</c> con los parámetros de 04 §11 (<c>fails_level</c>,
    /// <c>since_last_skip</c>, <c>level_tag</c>; también en 08 §8.2), más <c>level</c> y el saldo tras el cobro (<c>seeds</c>),
    /// que son solo de la sonda. Solo en los builds de desarrollo (<see cref="Enabled"/>) y por el canal de siempre
    /// (<see cref="Write"/>). Solo claves y números, nunca texto visible. Lo envía <c>AF.Analytics</c> cuando exista (B7).
    /// </summary>
    public sealed partial class DevProbe
    {
        private static readonly StringBuilder s_skip = new StringBuilder(160);

        /// <summary>Escribe la línea de <paramref name="result"/>; devuelve la línea, o null fuera de desarrollo.</summary>
        public static string? SkipUsed(in SkipResult result, int seeds)
        {
            if (!Enabled)
            {
                return null;
            }

            string line = FormatSkipUsed(NowMs(), result, seeds);
            Write(line);
            return line;
        }

        /// <summary>Texto de la línea <c>AF_STATE af_skip_used</c> en el instante <paramref name="tMs"/> (tests).</summary>
        internal static string FormatSkipUsed(long tMs, in SkipResult result, int seeds)
        {
            StringBuilder sb = s_skip;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"af_skip_used\"");
            sb.Append(",\"t\":").Append(tMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level\":").Append(result.Level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"fails_level\":").Append(result.FailsLevel.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"since_last_skip\":").Append(result.SinceLastSkip.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level_tag\":\"").Append(result.LevelTag ?? string.Empty).Append('"');
            sb.Append(",\"seeds\":").Append(seeds.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
