#nullable enable
using System.Globalization;
using System.Text;
using AF.Platform;

namespace AF.UI
{
    /// <summary>
    /// Sonda de la edad y el consentimiento (bloque B12), solo en desarrollo (<see cref="Enabled"/>):
    /// <c>AF_STATE {"event":"consent","t":…,"step":"age","profile":"co_1581","country":"CO","country_source":"sim",…}</c> cada vez
    /// que cambia el paso o se aplican los flags, con lo que reciben los anuncios, la analítica y la tienda (<c>analytics</c>,
    /// <c>npa</c>, <c>tfcd</c>, <c>rating</c>, <c>purchases</c>, <c>double_confirm</c>). Solo ids y booleanos, nunca texto visible;
    /// los botones de la pantalla salen en <c>AF_LAYOUT</c> con <c>"screen":"consent"</c> para tocarlos con <c>adb shell input tap</c>.
    /// </summary>
    public sealed partial class DevProbe
    {
        private static readonly StringBuilder s_consent = new StringBuilder(320);

        /// <summary>Escribe la línea; devuelve la línea, o null fuera de desarrollo.</summary>
        public static string? ConsentState(string step, ConsentFlags flags, string profile, string? country, CountrySource source)
        {
            if (!Enabled)
            {
                return null;
            }

            string line = FormatConsentState(NowMs(), step, flags, profile, country, source);
            Write(line);
            return line;
        }

        /// <summary>Texto de la línea <c>AF_STATE consent</c> en el instante <paramref name="tMs"/> (tests).</summary>
        internal static string FormatConsentState(long tMs, string step, ConsentFlags flags, string profile, string? country, CountrySource source)
        {
            StringBuilder sb = s_consent;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"consent\"");
            sb.Append(",\"t\":").Append(tMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"step\":\"").Append(step).Append('"');
            sb.Append(",\"profile\":\"").Append(profile).Append('"');
            sb.Append(",\"country\":");
            if (country == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('"').Append(country).Append('"');
            }

            sb.Append(",\"country_source\":\"").Append(ConsentCountry.SourceKey(source)).Append('"');
            string? band = AgeBands.Id(flags.Band);
            sb.Append(",\"age_band\":");
            if (band == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('"').Append(band).Append('"');
            }

            sb.Append(",\"decided\":").Append(flags.Decided ? "true" : "false");
            sb.Append(",\"blocked\":").Append(flags.Blocked ? "true" : "false");
            sb.Append(",\"analytics\":").Append(flags.AnalyticsEnabled ? "true" : "false");
            sb.Append(",\"npa\":").Append(!flags.AdsPersonalized && !flags.AdsByUmp ? "true" : "false"); // como ConsentAdService
            sb.Append(",\"ump\":").Append(flags.AdsByUmp ? "true" : "false");
            sb.Append(",\"tfcd\":").Append(flags.ChildDirected ? "true" : "false");
            sb.Append(",\"rating\":\"").Append(flags.MaxAdContentRating).Append('"');
            sb.Append(",\"purchases\":").Append(flags.PurchasesAllowed ? "true" : "false");
            sb.Append(",\"double_confirm\":").Append(flags.PurchaseDoubleConfirm ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
