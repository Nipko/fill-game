#nullable enable
using System;
using AF.Save;

namespace AF.Economy
{
    /// <summary>
    /// Cambio de día local del bloque <c>daily</c> del perfil (04 §10.1: reinicio a las 00:00 locales). Se reinicia el
    /// bloque entero de una vez, para que ningún módulo deje la fecha nueva con contadores del día anterior:
    /// <list type="bullet">
    /// <item>recompensados por ubicación y <c>rv_total</c>, <c>interstitials_today</c>, <c>rescues_today</c>,
    /// <c>af_rescue_shown</c> y <c>shuffles_today</c> vuelven a 0;</item>
    /// <item><c>shield_available</c> vuelve a true (05 §2.4 <c>reset_daily</c>: una Hoja escudo al día);</item>
    /// <item><c>af_rescue_declined_days</c> no se toca: cuenta rechazos de la Balsa de rescate y vuelve a 0 al comprar
    /// (04 §6.2).</item>
    /// </list>
    /// La fecha solo avanza: si el reloj del dispositivo retrocede a un día anterior, los contadores siguen como están
    /// (cambiar la hora no reabre los topes diarios).
    /// </summary>
    public static class DailyCounters
    {
        /// <summary>
        /// Los contadores guardados son de un día anterior a <paramref name="todayKey"/> (o no hay fecha), así que hoy
        /// valen 0.
        /// </summary>
        public static bool IsStale(DailyData daily, string todayKey)
        {
            if (daily == null)
            {
                throw new ArgumentNullException(nameof(daily));
            }

            // yyyy-MM-dd se ordena igual como texto que como fecha.
            return daily.DateLocal == null || string.CompareOrdinal(todayKey, daily.DateLocal) > 0;
        }

        /// <summary>Si los contadores son de un día anterior, los reinicia y pone la fecha de hoy. Devuelve si cambió algo.</summary>
        public static bool RollOver(DailyData daily, string todayKey)
        {
            if (!IsStale(daily, todayKey))
            {
                return false;
            }

            daily.DateLocal = todayKey;
            daily.RvUndoRefill = 0;
            daily.RvHint = 0;
            daily.RvReveal = 0;
            daily.RvExtraCanoe = 0;
            daily.RvDoubleHarvest = 0;
            daily.RvTotal = 0;
            daily.InterstitialsToday = 0;
            daily.RescuesToday = 0;
            daily.AfRescueShown = 0;
            daily.ShufflesToday = 0;
            daily.ShieldAvailable = true;
            return true;
        }
    }
}
