#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Reloj controlado a mano para tests y builds de desarrollo: la hora solo cambia con <see cref="Set"/> y
    /// <see cref="Advance"/> (p. ej. la línea temporal de canoas de B1-2: gasto en t0, t0 + 25 min, t0 + 50 min).
    /// Puede retroceder, como un reloj del dispositivo cambiado a mano.
    /// </summary>
    public sealed class ManualClock : IClock
    {
        private static readonly TimeSpan MinOffset = TimeSpan.FromHours(-14);
        private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(14);

        private DateTime _utcNow;
        private TimeSpan _localOffset;

        /// <summary>Crea el reloj en <paramref name="utcNow"/> con el desfase local <paramref name="localOffset"/>.</summary>
        /// <param name="utcNow">Instante inicial en UTC; un valor <see cref="DateTimeKind.Unspecified"/> se toma como UTC.</param>
        /// <param name="localOffset">Desfase de la hora local (por defecto 0; Colombia y Perú, −5 h).</param>
        /// <exception cref="ArgumentException"><paramref name="utcNow"/> es hora local o el desfase sale de ±14 h.</exception>
        public ManualClock(DateTime utcNow, TimeSpan localOffset = default)
        {
            _utcNow = ToUtc(utcNow, nameof(utcNow));
            LocalOffset = localOffset;
        }

        /// <inheritdoc />
        public DateTime UtcNow => _utcNow;

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Al asignar un desfase fuera de ±14 h.</exception>
        public TimeSpan LocalOffset
        {
            get => _localOffset;
            set
            {
                if (value < MinOffset || value > MaxOffset)
                {
                    throw new ArgumentException("El desfase local debe estar entre -14 h y +14 h.", nameof(value));
                }

                _localOffset = value;
            }
        }

        /// <summary>Pone el reloj en <paramref name="utcNow"/> (puede ir hacia atrás).</summary>
        /// <exception cref="ArgumentException"><paramref name="utcNow"/> es hora local.</exception>
        public void Set(DateTime utcNow)
        {
            _utcNow = ToUtc(utcNow, nameof(utcNow));
        }

        /// <summary>Mueve el reloj <paramref name="delta"/> (negativo, hacia atrás).</summary>
        public void Advance(TimeSpan delta)
        {
            _utcNow = DateTime.SpecifyKind(_utcNow + delta, DateTimeKind.Utc);
        }

        private static DateTime ToUtc(DateTime value, string paramName)
        {
            if (value.Kind == DateTimeKind.Local)
            {
                // Convertirla dependería de la zona horaria de la máquina que corre el test.
                throw new ArgumentException("ManualClock espera una hora UTC, no local.", paramName);
            }

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
