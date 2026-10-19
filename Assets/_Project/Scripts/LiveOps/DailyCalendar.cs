#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Tablero del Reto del río que toca un día: la banda del jugador, el día del pool y, si <c>daily_overrides</c> lo sustituye,
    /// el id del tablero sustituto (nota <c>docs/dev/reto-del-rio.md</c> §6).
    /// </summary>
    public readonly struct DailySlot : IEquatable<DailySlot>
    {
        /// <summary>Crea el slot.</summary>
        public DailySlot(int band, int day, string? overrideId)
        {
            Band = band;
            Day = day;
            OverrideId = overrideId;
        }

        /// <summary>Banda del jugador (1-3) según su nivel máximo. Con una sustitución de otra banda [P] sigue siendo la del jugador.</summary>
        public int Band { get; }

        /// <summary>Día del pool (0-729) por el cálculo normal, con <c>live_daily_offset</c>.</summary>
        public int Day { get; }

        /// <summary>Id del tablero sustituto (<c>d{b}_n{nnnn}</c>, ya comprobado en el pool), o null si se sirve el cálculo normal.</summary>
        public string? OverrideId { get; }

        /// <summary>Hay sustitución.</summary>
        public bool IsOverride => OverrideId != null;

        /// <summary>
        /// El tablero de este slot en <paramref name="pool"/>: el sustituto si lo hay, si no <c>pool.Find(Band, Day)</c> (null si el
        /// pool no trae esa banda). No asigna memoria.
        /// </summary>
        public LevelDefinition? Board(DailyPool pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            return OverrideId != null ? pool.FindById(OverrideId) : pool.Find(Band, Day);
        }

        /// <inheritdoc />
        public bool Equals(DailySlot other)
        {
            return Band == other.Band && Day == other.Day && string.Equals(OverrideId, other.OverrideId, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is DailySlot other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Sin string.GetHashCode (varía entre procesos): el id solo entra por su longitud.
            return (Band * 1000 + Day) * 31 + (OverrideId == null ? 0 : OverrideId.Length + 1);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "b" + Band + ":d" + Day + (OverrideId == null ? "" : "→" + OverrideId);
        }
    }

    /// <summary>
    /// Calendario del Reto del río (03 §16; 06 §4.1; nota <c>docs/dev/reto-del-rio.md</c> §5-§6): fecha local → día del pool,
    /// nivel máximo → banda, desbloqueo y sustituciones <c>daily_overrides</c> (R-Q6). Sin motor y sin reloj del sistema: la
    /// fecha llega como parámetro o de <see cref="IClock"/> (<see cref="ClockExtensions.LocalDate"/>).
    /// <code>reto(fecha, tramo) = pool[tramo][(días_desde(2026-01-01) + live_daily_offset) mod 730]</code>
    /// </summary>
    public static class DailyCalendar
    {
        /// <summary>Día 0 del pool: 2026-01-01 (03 §16).</summary>
        public static readonly DateTime Epoch = new DateTime(2026, 1, 1);

        /// <summary>Días del pool por banda (03 §16).</summary>
        public const int PoolDays = DailyPool.Days;

        /// <summary>Número de bandas (B1-B3; 03 §16).</summary>
        public const int BandCount = DailyPool.MaxBand;

        /// <summary>Días enteros desde <see cref="Epoch"/> hasta la fecha de <paramref name="localDate"/> (se ignora la hora); negativo antes de 2026.</summary>
        public static int DaysSinceEpoch(DateTime localDate)
        {
            return (localDate.Date - Epoch).Days;
        }

        /// <summary>
        /// Día del pool (0-729): <c>(DaysSinceEpoch + offset) mod 730</c> con módulo positivo, también con fechas anteriores a
        /// 2026-01-01 y con <paramref name="offset"/> negativo (<c>live_daily_offset</c>).
        /// </summary>
        public static int PoolDay(DateTime localDate, int offset)
        {
            long raw = (long)DaysSinceEpoch(localDate) + offset;
            long day = raw % PoolDays;
            return (int)(day < 0 ? day + PoolDays : day);
        }

        /// <summary>
        /// Banda (1-3) del jugador con nivel máximo <paramref name="maxLevel"/> y los umbrales <c>lvl_daily_bracket_levels</c>
        /// (<c>[41, 201]</c>): 1 por debajo del primero, 2 por debajo del segundo, 3 desde él (03 §16).
        /// </summary>
        /// <exception cref="ArgumentException">Los umbrales no son dos valores en orden creciente.</exception>
        public static int BandFor(int maxLevel, IReadOnlyList<int> thresholds)
        {
            if (thresholds == null)
            {
                throw new ArgumentNullException(nameof(thresholds));
            }

            if (thresholds.Count != BandCount - 1 || thresholds[1] <= thresholds[0])
            {
                throw new ArgumentException("lvl_daily_bracket_levels debe tener 2 umbrales en orden creciente.", nameof(thresholds));
            }

            if (maxLevel < thresholds[0])
            {
                return 1;
            }

            return maxLevel < thresholds[1] ? 2 : 3;
        }

        /// <summary>
        /// El Reto está disponible: <c>biz_feature_daily_challenge</c> encendido y nivel máximo superado ≥
        /// <c>live_unlock_level_daily</c> (06 §2.3: se desbloquea tras ganar el 10). Que el pool esté cargado y sea válido lo
        /// comprueba el llamador (nota §6, «Desbloqueo»).
        /// </summary>
        public static bool IsUnlocked(int maxLevel, DailyChallengeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.BizFeatureDailyChallenge && maxLevel >= config.LiveUnlockLevelDaily;
        }

        /// <summary>
        /// Slot de hoy con la fecha local de <paramref name="clock"/> (<see cref="ClockExtensions.LocalDate"/> y
        /// <see cref="ClockExtensions.LocalDateKey"/>, que no asigna memoria mientras no cambie el día).
        /// </summary>
        public static DailySlot Today(IClock clock, int maxLevel, DailyChallengeConfig config, DailyPool pool)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            return Resolve(clock.LocalDate(), clock.LocalDateKey(), maxLevel, config, pool);
        }

        /// <summary>Slot de la fecha local <paramref name="localDate"/> (formatea su clave <c>yyyy-MM-dd</c>).</summary>
        public static DailySlot Resolve(DateTime localDate, int maxLevel, DailyChallengeConfig config, DailyPool pool)
        {
            return Resolve(localDate.Date, ClockExtensions.FormatDateKey(localDate.Date), maxLevel, config, pool);
        }

        /// <summary>
        /// Slot de la fecha <paramref name="localDate"/> (con su clave <paramref name="dateKey"/>) para un jugador de nivel máximo
        /// <paramref name="maxLevel"/>. Sustituciones (R-Q6; nota §6): si <c>daily_overrides[fecha][banda]</c> existe y ese id está
        /// en <paramref name="pool"/> (<see cref="DailyPool.FindById"/>), se sirve ese tablero; si no (sin fecha, sin esa banda o con
        /// un id que el pool no tiene), el cálculo normal. Se admite un id de otra banda [P].
        /// </summary>
        private static DailySlot Resolve(DateTime localDate, string dateKey, int maxLevel, DailyChallengeConfig config, DailyPool pool)
        {
            if (dateKey == null)
            {
                throw new ArgumentNullException(nameof(dateKey));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            int band = BandFor(maxLevel, config.LvlDailyBracketLevels);
            int day = PoolDay(localDate, config.LiveDailyOffset);
            string? overrideId = null;
            if (config.DailyOverrides != null && config.DailyOverrides.TryGet(dateKey, band, out string id) && pool.FindById(id) != null)
            {
                overrideId = id;
            }

            return new DailySlot(band, day, overrideId);
        }
    }
}
