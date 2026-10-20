#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Economy
{
    /// <summary>
    /// Estado de la Crecida (<c>win_streak</c>) y de la Hoja escudo (<c>streak_shield</c>) que guarda el perfil (06 §4.3; 05 §2.4;
    /// 04 §8), con el esquema de Q-M9 (a) [P] del plan de la tanda 5: <see cref="Wins"/> y <see cref="RiverFull"/> son el bloque
    /// <c>streak</c> de 08 §4.3 (sin su <c>shield_available</c>); <see cref="ShieldAvailable"/>, <see cref="HitsToday"/> y
    /// <see cref="SeedsToday"/> son contadores del día local y viven en <c>daily</c> (04 §10.1: <c>shield_available</c>,
    /// <c>streak_hits</c> y <c>streak_seeds</c>), que se reinicia entero con <c>date_local</c>. Solo datos: las reglas
    /// (<c>on_level_end</c>, hitos 3/6/10, tope de 600, río crecido) las aplica quien usa <see cref="IStreakStore"/>.
    /// </summary>
    public sealed class StreakRecord
    {
        /// <summary><c>streak.wins</c>: la racha de la Crecida (0-10; vuelve a 0 al pagar el hito 10, 05 §2.4).</summary>
        public int Wins { get; set; }

        /// <summary>
        /// <c>streak.river_full</c>: río crecido (Cosecha doble sin anuncio). Sigue en true, también con <see cref="Wins"/> = 0,
        /// hasta el siguiente fallo duro no escudado (08 §4.3; 05 §2.4); sin plazo de reloj.
        /// </summary>
        public bool RiverFull { get; set; }

        /// <summary>
        /// <c>daily.date_local</c>: el día local (<c>yyyy-MM-dd</c>) de <see cref="ShieldAvailable"/>, <see cref="HitsToday"/> y
        /// <see cref="SeedsToday"/>, o null si aún no hay ninguno. Lo pone <see cref="RollOver"/> (o el almacén): las reglas no lo tocan.
        /// </summary>
        public string? DateLocal { get; set; }

        /// <summary><c>daily.shield_available</c>: la Hoja escudo de hoy sigue disponible (1 por día local, <c>streak_shield_per_day</c>).</summary>
        public bool ShieldAvailable { get; set; }

        /// <summary>
        /// <c>daily.streak_hits</c> [P]: los hitos de la Crecida ya pagados hoy (p. ej. <c>[3, 6]</c>), porque cada hito se paga una
        /// vez por día local (<c>streak_hits_once_per_day</c>, 06 §4.3).
        /// </summary>
        public List<int> HitsToday { get; } = new List<int>();

        /// <summary>
        /// <c>daily.streak_seeds</c> [P]: semillas de los hitos pagadas hoy que cuentan contra el tope diario
        /// (<c>streak_daily_cap</c>, 600: las 300 del hito 3 y las 50 de la Canasta del hito 6; 06 §4.3).
        /// </summary>
        public int SeedsToday { get; set; }

        /// <summary>
        /// Cambio de día local, solo hacia delante, como <c>AF.Save.DailyCounters.RollOver</c> (06 §4.3 <c>on_app_open</c>): si
        /// <see cref="DateLocal"/> es anterior a <paramref name="todayKey"/> (o null), repone la Hoja escudo, vacía los hitos de hoy,
        /// pone a 0 las semillas de hoy y apunta la fecha. La racha y el río crecido no cambian (sin expiración por reloj, 04 §8).
        /// Si el reloj retrocede no hace nada. Devuelve si cambió algo.
        /// </summary>
        public bool RollOver(string todayKey)
        {
            if (todayKey == null)
            {
                throw new ArgumentNullException(nameof(todayKey));
            }

            // yyyy-MM-dd se ordena igual como texto que como fecha.
            if (DateLocal != null && string.CompareOrdinal(todayKey, DateLocal) <= 0)
            {
                return false;
            }

            DateLocal = todayKey;
            ShieldAvailable = true;
            HitsToday.Clear();
            SeedsToday = 0;
            return true;
        }

        /// <summary>Copia independiente.</summary>
        public StreakRecord Clone()
        {
            var copy = new StreakRecord
            {
                Wins = Wins,
                RiverFull = RiverFull,
                DateLocal = DateLocal,
                ShieldAvailable = ShieldAvailable,
                SeedsToday = SeedsToday,
            };
            copy.HitsToday.AddRange(HitsToday);
            return copy;
        }
    }
}
