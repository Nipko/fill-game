#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Lo que un reloj sabe del contraste con la hora del servidor (GDD 08 §10.1, fila «Manipulación del reloj»; 04 §7.1,
    /// «Reloj»; bloque B8). Lo implementa <see cref="GuardedClock"/>; quien recibe un <see cref="IClock"/> lo pregunta con
    /// <c>clock is IClockTrust</c> (un reloj que no lo implementa se toma como de fiar: hoy, sin backend, todos).
    /// </summary>
    public interface IClockTrust
    {
        /// <summary>
        /// Hay hora del servidor y el reloj del dispositivo se aparta más de <c>sec_clock_max_drift_h</c> (24 h, 08 §9). Con
        /// esto, las Canoas no regeneran (04 §7.1) y los topes diarios usan la fecha del servidor (08 §10.1). Sin red, false: se
        /// acepta la fecha local y se reconcilia al reconectar.
        /// </summary>
        bool DriftExceeded { get; }
    }

    /// <summary>
    /// Reloj con el anti-reloj de 08 §10.1 (bloque B8): envuelve el reloj del dispositivo y la hora del servidor
    /// (<see cref="IServerDate"/>, <c>Clock.ServerOffset</c>).
    /// <list type="bullet">
    /// <item>Sin red, o con un desfase dentro de <c>sec_clock_max_drift_h</c>: es el reloj del dispositivo, sin cambios.</item>
    /// <item>Con un desfase mayor (<see cref="DriftExceeded"/>): <see cref="UtcNow"/> es la hora del servidor (dispositivo +
    /// desfase), así que las fechas locales de los topes diarios (<c>daily.date_local</c>, 04 §10.1) salen de la fecha del
    /// servidor (08 §10.1: «los topes diarios usan la fecha del servidor»). <see cref="LocalOffset"/> sigue siendo la zona del
    /// teléfono. La congelación de las Canoas la aplica <c>AF.Economy</c> preguntando <see cref="IClockTrust"/>.</item>
    /// </list>
    /// No se le da al Reto del río: <c>AF.LiveOps.DailyChallengeRules</c> ya contrasta él mismo el reloj del dispositivo con
    /// <see cref="IServerDate"/> y aplicaría el desfase dos veces. En la 1.0 sin backend el servidor es
    /// <see cref="FakeServerDate.Offline"/> y este reloj es idéntico al del dispositivo.
    /// </summary>
    public sealed class GuardedClock : IClock, IClockTrust
    {
        private readonly IClock _device;
        private readonly IServerDate _server;
        private readonly ClockSecurityConfig _config;

        /// <summary>Crea el reloj sobre <paramref name="device"/> y <paramref name="server"/>.</summary>
        /// <exception cref="InvalidOperationException"><paramref name="config"/> no es válida.</exception>
        public GuardedClock(IClock device, IServerDate server, ClockSecurityConfig? config = null)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _config = config ?? ClockSecurityConfig.Defaults;
            _config.Validate();
        }

        /// <summary>El reloj del dispositivo que envuelve.</summary>
        public IClock Device => _device;

        /// <inheritdoc />
        public bool DriftExceeded => _server.TryGetServerOffset(out TimeSpan offset) && _config.ExceedsMaxDrift(offset);

        /// <inheritdoc />
        public DateTime UtcNow
        {
            get
            {
                DateTime device = _device.UtcNow;
                if (_server.TryGetServerOffset(out TimeSpan offset) && _config.ExceedsMaxDrift(offset))
                {
                    return DateTime.SpecifyKind(device + offset, DateTimeKind.Utc);
                }

                return device;
            }
        }

        /// <inheritdoc />
        public TimeSpan LocalOffset => _device.LocalOffset;
    }
}
