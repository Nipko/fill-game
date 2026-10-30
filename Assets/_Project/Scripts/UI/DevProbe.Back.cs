#nullable enable
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// Sonda del Atrás de Android (02 §3.5; observación 21 de la jornada 6), solo en desarrollo:
    /// <c>AF_STATE {"event":"back","t":…,"screen":"pause","action":"resume","reto":false}</c> en cada pulsación. <c>screen</c> es lo
    /// que se veía antes (<c>lobby</c>, <c>level</c>, <c>intro</c>, <c>pause</c>, <c>quit_confirm</c>, <c>restart_confirm</c>,
    /// <c>skip_confirm</c>, <c>stuck</c>, <c>giveup_confirm</c>, <c>level_failed</c>, <c>no_canoes</c>, <c>victory</c>,
    /// <c>celebration</c>, <c>settings</c>, <c>map</c>, <c>market</c>, <c>league</c>, <c>avatar</c>, <c>daily_calendar</c>,
    /// <c>consent</c>…); <c>action</c>, lo que hizo (<c>pause</c>, <c>resume</c>, <c>cancel_to_pause</c>, <c>cancel</c>,
    /// <c>close</c>, <c>background</c>, <c>blocked</c> o <c>none</c>); <c>reto</c>, si era el tablero del Reto del río.
    /// </summary>
    public sealed partial class DevProbe
    {
        /// <summary>Última línea <c>AF_STATE back</c> escrita (tests; null sin sonda).</summary>
        public static string? LastBackLine { get; private set; }

        /// <summary>Escribe la línea de una pulsación de Atrás (solo al pulsar: no va por fotograma).</summary>
        public static void Back(string screen, string action, bool reto)
        {
            if (!Enabled)
            {
                return;
            }

            var sb = new StringBuilder(96);
            sb.Append("AF_STATE {\"event\":\"back\"");
            AppendTime(sb);
            sb.Append(",\"screen\":\"").Append(screen).Append('"');
            sb.Append(",\"action\":\"").Append(action).Append('"');
            sb.Append(",\"reto\":").Append(reto ? "true" : "false").Append('}');
            LastBackLine = sb.ToString();
            Write(LastBackLine);
        }
    }
}
