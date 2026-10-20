#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Hora del servidor para el anti-reloj: <c>Clock.ServerOffset</c> de GDD 08 §10.1 (fila «Manipulación del reloj»), estimado
    /// con la cabecera <c>Date</c> del último fetch de Remote Config o de una respuesta de Functions. 08 §2.2 pone el «<c>Date</c>
    /// del servidor para el reloj» en <c>AF.Platform</c> y lo entrega a <c>AF.LiveOps</c> (nota <c>docs/dev/reto-del-rio.md</c>
    /// §1.2). La implementación real llegará con Firebase (aprobación del dueño); el editor ARM64 no carga sus DLL x64, así que
    /// el editor y los tests usan <see cref="FakeServerDate"/>.
    /// </summary>
    public interface IServerDate
    {
        /// <summary>
        /// Desfase servidor − dispositivo (<c>hora_servidor − IClock.UtcNow</c>) de la última medición. False = sin red o sin
        /// ningún fetch todavía: se acepta la fecha local y se reconcilia al reconectar (08 §10.1).
        /// </summary>
        bool TryGetServerOffset(out TimeSpan serverMinusDevice);
    }
}
