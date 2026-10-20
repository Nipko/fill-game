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
        /// <summary>
        /// <c>streak.wins</c>: la racha de la Crecida, de 0 a 9 entre un evento y otro. El 10 es solo transitorio: dentro de
        /// <c>StreakRules.OnVictory</c> la barra llega a 10, paga el hito y vuelve a 0 en la misma llamada (05 §2.4), así que
        /// nunca se guarda un 10 (<see cref="Normalize"/> lo admite para no tocar un archivo de otra build).
        /// </summary>
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

        /// <summary>Hito 3 de la Crecida: semillas (<c>streak_milestones["3"]</c>, 08 §9; 05 §2.4).</summary>
        public const int HitSeeds = 3;

        /// <summary>Hito 6: la Canasta (<c>streak_milestones["6"]</c> = <c>chest</c>; 06 §4.3).</summary>
        public const int HitBasket = 6;

        /// <summary>Hito 10: Cuia extra + río crecido; la barra vuelve a 0 (<c>streak_milestones["10"]</c>; 05 §2.4).</summary>
        public const int HitRiver = 10;

        /// <summary>
        /// Los hitos de la Crecida en orden creciente (3, 6 y 10: las claves de <c>streak_milestones</c>, 08 §9). Única lista: la
        /// usan <see cref="Normalize"/>, <c>StreakRules</c> y <c>StreakMilestones.TryParse</c>. Solo el premio del 3 es
        /// configurable; si Remote Config cambiara los hitos, cambiaría esta lista [P].
        /// </summary>
        public static readonly IReadOnlyList<int> Milestones = Array.AsReadOnly(new[] { HitSeeds, HitBasket, HitRiver });

        /// <summary>
        /// Tope de <see cref="Wins"/>: la barra tiene 10 marcas (el último hito) y vuelve a 0 al pagar el hito 10 (06 §4.3; 05 §2.4).
        /// </summary>
        public const int MaxWins = HitRiver;

        /// <summary>
        /// <paramref name="wins"/> es uno de los <see cref="Milestones"/>. No asigna memoria.
        /// </summary>
        public static bool IsMilestone(int wins)
        {
            for (int i = 0; i < Milestones.Count; i++)
            {
                if (Milestones[i] == wins)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Recorta lo que no puede salir de las reglas (un archivo editado a mano o de otra build): <see cref="Wins"/> a
        /// [0, <see cref="MaxWins"/>], <see cref="SeedsToday"/> a ≥ 0 y <see cref="HitsToday"/> a los <see cref="Milestones"/> (3, 6 y
        /// 10, las claves de <c>streak_milestones</c>, 08 §9), sin repetir y en orden creciente. Devuelve si cambió algo.
        /// </summary>
        public bool Normalize()
        {
            bool changed = false;
            int wins = Wins < 0 ? 0 : Wins > MaxWins ? MaxWins : Wins;
            if (wins != Wins)
            {
                Wins = wins;
                changed = true;
            }

            if (SeedsToday < 0)
            {
                SeedsToday = 0;
                changed = true;
            }

            // Canónica: solo hitos de Milestones, sin repetir y en el orden de Milestones (creciente).
            bool canonical = true;
            int next = 0;
            for (int i = 0; i < HitsToday.Count && canonical; i++)
            {
                int hit = HitsToday[i];
                while (next < Milestones.Count && Milestones[next] != hit)
                {
                    next++;
                }

                canonical = next < Milestones.Count;
                next++;
            }

            if (!canonical)
            {
                // Sin asignar: se marca qué hitos había (como mucho Milestones.Count, que cabe en un int) y se reescribe.
                int present = 0;
                for (int i = 0; i < HitsToday.Count; i++)
                {
                    for (int m = 0; m < Milestones.Count; m++)
                    {
                        if (Milestones[m] == HitsToday[i])
                        {
                            present |= 1 << m;
                        }
                    }
                }

                HitsToday.Clear();
                for (int m = 0; m < Milestones.Count; m++)
                {
                    if ((present & (1 << m)) != 0)
                    {
                        HitsToday.Add(Milestones[m]);
                    }
                }

                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Lleva los campos del día de <paramref name="record"/> (lo que guarda quien juega) a <paramref name="current"/> (el estado
        /// del día que hay ahora en el almacén), sabiendo que <paramref name="record"/> salió de <paramref name="loaded"/> (lo que
        /// entregó el último <c>Load</c>). No toca <see cref="Wins"/> ni <see cref="RiverFull"/>. Devuelve si cambió
        /// <paramref name="current"/>.
        /// <list type="bullet">
        /// <item>Si <paramref name="record"/> es de un día posterior al de <paramref name="current"/>, este pasa antes a ese día
        /// (<see cref="RollOver"/>) y luego, como si fuera del mismo día, se copian los campos.</item>
        /// <item>Del mismo día: se copian <see cref="ShieldAvailable"/>, <see cref="HitsToday"/> y <see cref="SeedsToday"/>.</item>
        /// <item>De un día anterior (cargado antes de medianoche y guardado cuando otro módulo ya pasó el día): <b>regla
        /// conservadora</b>, solo se aplica lo que cambió desde la carga: los hitos nuevos se suman a los de hoy, el aumento de
        /// semillas se suma a las de hoy y un escudo gastado gasta el de hoy. Lo que ya estaba gastado ayer no niega nada hoy. En
        /// el borde de medianoche puede pagar de menos (un hito pagado ayer cuenta también hoy), nunca dos veces. Si
        /// <paramref name="loaded"/> no es del mismo día que <paramref name="record"/>, se compara con un día vacío (escudo
        /// disponible, sin hitos, 0 semillas): todo lo del registro cuenta como nuevo.</item>
        /// </list>
        /// </summary>
        public static bool MergeDay(StreakRecord current, StreakRecord loaded, StreakRecord record)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (loaded == null)
            {
                throw new ArgumentNullException(nameof(loaded));
            }

            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            bool changed = record.DateLocal != null && current.RollOver(record.DateLocal);
            if (string.Equals(record.DateLocal, current.DateLocal, StringComparison.Ordinal))
            {
                if (current.ShieldAvailable != record.ShieldAvailable || current.SeedsToday != record.SeedsToday
                    || !SameList(current.HitsToday, record.HitsToday))
                {
                    current.ShieldAvailable = record.ShieldAvailable;
                    current.SeedsToday = record.SeedsToday;
                    current.HitsToday.Clear();
                    current.HitsToday.AddRange(record.HitsToday);
                    changed = true;
                }

                return changed;
            }

            // Registro de un día anterior: solo el cambio desde la carga.
            bool sameDay = string.Equals(loaded.DateLocal, record.DateLocal, StringComparison.Ordinal);
            bool loadedShield = !sameDay || loaded.ShieldAvailable;
            int loadedSeeds = sameDay ? loaded.SeedsToday : 0;
            for (int i = 0; i < record.HitsToday.Count; i++)
            {
                int hit = record.HitsToday[i];
                if ((!sameDay || !loaded.HitsToday.Contains(hit)) && !current.HitsToday.Contains(hit))
                {
                    current.HitsToday.Add(hit);
                    changed = true;
                }
            }

            int seeds = record.SeedsToday - loadedSeeds;
            if (seeds > 0)
            {
                current.SeedsToday = current.SeedsToday > int.MaxValue - seeds ? int.MaxValue : current.SeedsToday + seeds;
                changed = true;
            }

            if (loadedShield && !record.ShieldAvailable && current.ShieldAvailable)
            {
                current.ShieldAvailable = false;
                changed = true;
            }

            return changed;
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

        private static bool SameList(List<int> a, List<int> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
