#nullable enable
using System;
using System.Globalization;

namespace AF.Platform
{
    /// <summary>
    /// Reloj inyectable (GDD 08 §2.2: <c>AF.Platform</c> da el reloj a <c>AF.Save</c>, <c>AF.Ads</c> y <c>AF.LiveOps</c>;
    /// <c>AF.Economy</c> lo usa para regenerar canoas, 04 §7.1). Todo el juego pide la hora a esta interfaz: la única
    /// lectura del reloj del sistema está en <see cref="SystemClock"/>, y los tests usan <see cref="ManualClock"/>.
    /// El contraste con la hora del servidor (<c>sec_clock_max_drift_h</c>, 08 §9; 04 §7.1 "Reloj") llegará con el
    /// backend: hoy no hay red.
    /// </summary>
    public interface IClock
    {
        /// <summary>Instante actual en UTC (<see cref="DateTime.Kind"/> = <see cref="DateTimeKind.Utc"/>).</summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Desfase de la hora local del dispositivo respecto a UTC en el instante actual (zona horaria del sistema, con
        /// su horario de verano). Sirve para la fecha local de los contadores diarios (04 §10.1 <c>daily.date_local</c>;
        /// reinicio a las 00:00 locales).
        /// </summary>
        TimeSpan LocalOffset { get; }
    }

    /// <summary>Operaciones derivadas de un <see cref="IClock"/>, iguales para todas las implementaciones.</summary>
    public static class ClockExtensions
    {
        /// <summary>Hora local del dispositivo (<see cref="DateTimeKind.Unspecified"/>): <c>UtcNow + LocalOffset</c>.</summary>
        public static DateTime LocalNow(this IClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            return DateTime.SpecifyKind(clock.UtcNow + clock.LocalOffset, DateTimeKind.Unspecified);
        }

        /// <summary>Fecha local del dispositivo, a las 00:00 (<see cref="DateTimeKind.Unspecified"/>).</summary>
        public static DateTime LocalDate(this IClock clock)
        {
            return clock.LocalNow().Date;
        }

        /// <summary>
        /// Fecha local con el formato del guardado, <c>yyyy-MM-dd</c> y calendario gregoriano (InvariantCulture), como
        /// <c>daily.date_local</c> de 04 §10.1 (p. ej. <c>"2026-10-05"</c>).
        /// </summary>
        public static string LocalDateKey(this IClock clock)
        {
            return FormatDateKey(clock.LocalDate());
        }

        /// <summary>Formatea una fecha como <c>yyyy-MM-dd</c> (InvariantCulture, calendario gregoriano).</summary>
        public static string FormatDateKey(DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
        }
    }
}
