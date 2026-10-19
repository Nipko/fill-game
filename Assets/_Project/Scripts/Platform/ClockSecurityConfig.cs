#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Cifras del anti-reloj (GDD 08 §10.1, fila «Manipulación del reloj») con el <b>default compilado</b> de la tabla única de
    /// 08 §9. <c>sec_clock_max_drift_h</c> es una clave global: la usan el Reto del río (06 §4.1, «Anti-reloj»; <c>AF.LiveOps</c>)
    /// y, cuando llegue la hora del servidor, la regeneración de canoas (04 §7.1, «Reloj»; <c>AF.Economy</c>). Vive en
    /// <c>AF.Platform</c>, junto al reloj y a <c>IServerDate</c>, porque <c>AF.Economy</c> no puede referenciar
    /// <c>AF.LiveOps</c> (08 §2.2): así hay una sola fuente de la clave en el cliente (nota <c>docs/dev/reto-del-rio.md</c> §11).
    /// </summary>
    public sealed class ClockSecurityConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static ClockSecurityConfig Defaults => new ClockSecurityConfig();

        /// <summary>
        /// <c>sec_clock_max_drift_h</c>: horas de desfase máximo entre el reloj del dispositivo y el del servidor; por encima, lo que
        /// depende de la fecha queda «pendiente de validar» (08 §10.1; 06 §4.1). Absorbe el alias retirado
        /// <c>canoes_clock_max_drift_h</c>.
        /// </summary>
        [RemoteConfigKey("sec_clock_max_drift_h")]
        public int SecClockMaxDriftH { get; set; } = 24;

        /// <summary>Desfase máximo como intervalo: <see cref="SecClockMaxDriftH"/> horas.</summary>
        public TimeSpan ClockMaxDrift => TimeSpan.FromHours(SecClockMaxDriftH);

        /// <summary>
        /// El desfase servidor − dispositivo <paramref name="serverMinusDevice"/> supera <see cref="ClockMaxDrift"/> (en valor
        /// absoluto; igual a la cota no la supera).
        /// </summary>
        public bool ExceedsMaxDrift(TimeSpan serverMinusDevice)
        {
            return serverMinusDevice.Duration() > ClockMaxDrift;
        }

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (SecClockMaxDriftH < 1)
            {
                throw new InvalidOperationException("ClockSecurityConfig: sec_clock_max_drift_h debe ser >= 1.");
            }
        }
    }
}
