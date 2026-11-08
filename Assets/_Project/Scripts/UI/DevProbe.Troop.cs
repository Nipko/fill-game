#nullable enable
using System.Globalization;
using System.Text;
using AF.Levels;
using AF.Presentation;

namespace AF.UI
{
    /// <summary>
    /// Sonda del segundo gesto de los miquitos (F13; añadido de D-029 del 2026-11-08), en los builds de desarrollo: al arrancar
    /// cada intro con especie, una línea
    /// <c>AF_STATE {"event":"troop_gesture","t":…,"level":N,"species_id":"…","second_from":F,"chosen":true|false,"forced":0|1|2,
    /// "gesture":"duo|brincos|colgado_de_la_cola|desperezo|vuelo|first","perch":"branch|side|canopy","monkeys":M,"still":true|false}</c>.
    /// <c>chosen</c> es la regla (o el extra <c>af_troop_gesture</c>, en <c>forced</c>); <c>gesture</c>, lo que hace de verdad la
    /// tropa en el reposo: el segundo gesto o <c>first</c> (sus gestos de siempre: el nivel no le toca, la tropa no va en la rama
    /// o el modelo de suelta no lo admite). Con <c>still</c> (Reducir movimiento), un solo miquito quieto en la pose del gesto;
    /// con brincos y vuelo, que se quedan sentados, <c>first</c>.
    /// Solo identificadores (regla de localización).
    /// </summary>
    public sealed partial class DevProbe
    {
        internal void TroopGesture(int level, FaunaSpecies species, TroopLayer troop, IntroChoreography plan, bool still, int forced)
        {
            string? text = FormatTroopGesture(level, species, troop, plan, still, forced);
            if (text != null)
            {
                Write(text);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE troop_gesture</c>; null si la sonda no escribe.</summary>
        internal string? FormatTroopGesture(int level, FaunaSpecies species, TroopLayer troop, IntroChoreography plan, bool still, int forced)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"troop_gesture\"");
            AppendTime(sb);
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"species_id\":");
            AppendKey(sb, species.Id);
            sb.Append(",\"second_from\":").Append(species.SecondGestureFrom.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"chosen\":").Append(troop.SecondChosen ? "true" : "false");
            sb.Append(",\"forced\":").Append(forced.ToString(CultureInfo.InvariantCulture));
            FaunaSecondGesture shown = still ? troop.SecondStill : troop.SecondShown; // quieto: brincos y vuelo, sentado
            sb.Append(",\"gesture\":");
            AppendKey(sb, shown == FaunaSecondGesture.None ? "first" : FaunaGestureRule.NameOf(shown));
            sb.Append(",\"perch\":\"").Append(plan.Perch == IntroPerch.Branch ? "branch" : (plan.Perch == IntroPerch.Side ? "side" : "canopy")).Append('"');
            sb.Append(",\"monkeys\":").Append(troop.MonkeyCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"still\":").Append(still ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
