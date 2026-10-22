#nullable enable
using System;
using System.Globalization;

namespace AF.Platform
{
    /// <summary>
    /// <see cref="IServerDate"/> real: estima <c>Clock.ServerOffset</c> (08 §10.1, fila «Manipulación del reloj») con la cabecera
    /// <c>Date</c> de cada respuesta del backend (contrato v1, <c>dateHeader</c>: toda respuesta la lleva; plan §7). La alimenta
    /// <c>AF.Backend</c>; mientras no haya ninguna respuesta, o tras un fallo de red, <see cref="TryGetServerOffset"/> devuelve
    /// false y el juego acepta la fecha local (08 §10.1). Solo se usa desde el hilo principal.
    /// </summary>
    public sealed class ServerDateTracker : IServerDate
    {
        // Una fecha anterior a esta no viene de nuestro servidor (proxy roto, cabecera de prueba): se ignora.
        private static readonly DateTime Earliest = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly IClock _clock;
        private bool _known;
        private TimeSpan _offset;

        public ServerDateTracker(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Hay una medición vigente.</summary>
        public bool HasOffset => _known;

        /// <summary>Anota la hora del servidor <paramref name="serverUtc"/> medida ahora.</summary>
        public void Observe(DateTime serverUtc)
        {
            if (serverUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Se esperaba una hora UTC.", nameof(serverUtc));
            }

            _offset = serverUtc - _clock.UtcNow;
            _known = true;
        }

        /// <summary>
        /// Anota el valor de una cabecera <c>Date</c> (RFC 9110 §5.6.7, formato IMF-fixdate). False si falta, no se entiende o es
        /// anterior a 2026 (no cambia nada).
        /// </summary>
        public bool ObserveDateHeader(string? value)
        {
            if (!TryParseHttpDate(value, out DateTime utc) || utc < Earliest)
            {
                return false;
            }

            Observe(utc);
            return true;
        }

        /// <summary>No hubo respuesta (sin red): hasta la siguiente, el juego usa la fecha local (08 §10.1).</summary>
        public void MarkUnreachable()
        {
            _known = false;
            _offset = TimeSpan.Zero;
        }

        /// <inheritdoc />
        public bool TryGetServerOffset(out TimeSpan serverMinusDevice)
        {
            serverMinusDevice = _known ? _offset : TimeSpan.Zero;
            return _known;
        }

        /// <summary>
        /// Lee una fecha HTTP: el formato preferente IMF-fixdate (<c>Sun, 06 Nov 1994 08:49:37 GMT</c>) y, como pide RFC 9110, los
        /// obsoletos RFC 850 y asctime.
        /// </summary>
        public static bool TryParseHttpDate(string? value, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] formats =
            {
                "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'",
                "dddd, dd'-'MMM'-'yy HH':'mm':'ss 'GMT'",
                "ddd MMM d HH':'mm':'ss yyyy",
            };
            const DateTimeStyles Styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowInnerWhite;
            if (!DateTime.TryParseExact(value!.Trim(), formats, CultureInfo.InvariantCulture, Styles, out DateTime parsed))
            {
                return false;
            }

            utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }
    }
}
