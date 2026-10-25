#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using AF.Platform;
using AF.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Reloj de los tests de PlayMode que no fijan el suyo (T6, carril A: tests que no dependen de la hora a la que se ejecutan).
    /// Empieza en <see cref="Anchor"/> (sábado 2026-10-24 a las 15:00 UTC: las 10:00 en Bogotá, con el desfase de −5 h) y avanza
    /// con el tiempo real que pasa desde que se crea, más lo que el test le sume con <see cref="Advance"/>. Así la fecha y la hora
    /// del juego (el saludo y las poses de TiTino, el Dato del día, el cambio de día del Reto, la semana de la Liga, la
    /// regeneración de canoas, economy_ts) son las mismas a cualquier hora, en cualquier día y en cualquier zona horaria, y las
    /// duraciones (la pista, los enfriamientos, las esperas del arranque) siguen corriendo como con el reloj del sistema.
    /// <para><see cref="PlayModeHelpers"/> lo pone en <see cref="LaunchOptions.ClockOverride"/> al cargar la escena si el test no puso
    /// otro (<see cref="PlayModeHelpers.NoRealClockUnlessSet"/>); <see cref="LaunchOptions.Reset"/> lo quita.</para>
    /// <para>La variable de entorno <see cref="ShiftVariable"/> (horas, puede ser negativa o decimal; por defecto 0) mueve el ancla: es la
    /// pasada de comprobación con el reloj del juego adelantado (punto 6 del carril A: 12 h). El reloj de Windows no se toca.</para>
    /// </summary>
    public sealed class TestClock : IClock
    {
        /// <summary>El ancla sin desplazar: sábado 2026-10-24, 15:00 UTC.</summary>
        public static readonly DateTime BaseAnchor = new DateTime(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        /// <summary>Desfase local de los tests (Bogotá, Colombia: −5 h, sin horario de verano).</summary>
        public static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        /// <summary>Variable de entorno con las horas que se adelanta (o atrasa) el ancla.</summary>
        public const string ShiftVariable = "AF_TEST_CLOCK_SHIFT_H";

        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly DateTime _start;
        private TimeSpan _advanced;
        private TimeSpan _localOffset = Bogota;

        /// <summary>Un reloj que empieza en <see cref="Anchor"/>.</summary>
        public TestClock()
            : this(Anchor)
        {
        }

        /// <summary>Un reloj que empieza en <paramref name="startUtc"/> (UTC; uno sin tipo se toma como UTC).</summary>
        /// <exception cref="ArgumentException"><paramref name="startUtc"/> es hora local.</exception>
        public TestClock(DateTime startUtc)
        {
            if (startUtc.Kind == DateTimeKind.Local)
            {
                throw new ArgumentException("TestClock espera una hora UTC, no local.", nameof(startUtc));
            }

            _start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        }

        /// <summary>El ancla de esta pasada: <see cref="BaseAnchor"/> más <see cref="Shift"/>.</summary>
        public static DateTime Anchor => BaseAnchor + Shift;

        /// <summary>Lo que se desplaza el ancla (<see cref="ShiftVariable"/>; cero si falta o no se entiende).</summary>
        public static TimeSpan Shift
        {
            get
            {
                string? raw = Environment.GetEnvironmentVariable(ShiftVariable);
                return raw != null && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours)
                    && Math.Abs(hours) <= 24 * 366
                    ? TimeSpan.FromHours(hours)
                    : TimeSpan.Zero;
            }
        }

        /// <summary>El reloj del juego si es un <see cref="TestClock"/> (el de <see cref="LaunchOptions.ClockOverride"/>); si no, null.</summary>
        public static TestClock? Installed => LaunchOptions.ClockOverride as TestClock;

        /// <inheritdoc />
        public DateTime UtcNow => DateTime.SpecifyKind(_start + _watch.Elapsed + _advanced, DateTimeKind.Utc);

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Al asignar un desfase fuera de ±14 h.</exception>
        public TimeSpan LocalOffset
        {
            get => _localOffset;
            set
            {
                if (value < TimeSpan.FromHours(-14) || value > TimeSpan.FromHours(14))
                {
                    throw new ArgumentException("El desfase local debe estar entre -14 h y +14 h.", nameof(value));
                }

                _localOffset = value;
            }
        }

        /// <summary>
        /// Adelanta el reloj <paramref name="delta"/> sin esperar (p. ej. que pase economy_ts de la nube); negativo, lo atrasa,
        /// como un reloj del teléfono cambiado a mano.
        /// </summary>
        public void Advance(TimeSpan delta)
        {
            _advanced += delta;
        }

        /// <summary>
        /// El reloj del test: el <see cref="TestClock"/> ya puesto en <see cref="LaunchOptions.ClockOverride"/> o uno nuevo que se
        /// pone ahí. Si el test ya puso otro reloj (un <c>ManualClock</c>), falla: entonces la fecha es la de ese reloj.
        /// </summary>
        public static TestClock Install()
        {
            IClock? current = LaunchOptions.ClockOverride;
            if (current == null)
            {
                var clock = new TestClock();
                LaunchOptions.ClockOverride = clock;
                return clock;
            }

            return current as TestClock
                ?? throw new InvalidOperationException("El test ya fijó otro reloj (" + current.GetType().Name + "): usa ese.");
        }
    }
}
