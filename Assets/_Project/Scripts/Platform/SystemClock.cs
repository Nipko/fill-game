#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Reloj del sistema: la <b>única</b> lectura de la hora del dispositivo en el código del juego (criterio de B1-1;
    /// GDD 08 §2.1 principio 5 prohíbe <c>DateTime.Now</c> en el núcleo). Un test de guarda comprueba que
    /// <c>AF.Platform</c> y <c>AF.Save</c> no leen el reloj en ningún otro archivo.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        /// <summary>Instancia compartida (no tiene estado).</summary>
        public static readonly SystemClock Instance = new SystemClock();

        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;

        /// <inheritdoc />
        public TimeSpan LocalOffset => TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
    }
}
