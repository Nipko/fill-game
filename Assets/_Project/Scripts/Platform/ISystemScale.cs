#nullable enable

namespace AF.Platform
{
    /// <summary>
    /// Las escalas de accesibilidad del sistema (bloque B9; 08 §2.2: <c>AF.Platform</c> da los servicios del dispositivo):
    /// <list type="bullet">
    /// <item><see cref="FontScale"/>: la escala de fuente del sistema (02 §12, fila «Tamaño de texto»: se respeta hasta el
    /// 130 % y por encima se fija ese tope; lo hace <see cref="A11yRules.TextScaleFor"/>);</item>
    /// <item><see cref="AnimatorDurationScale"/>: la escala de duración de animaciones de Android («Quitar animaciones» = 0), que
    /// [P] enciende «Reducir movimiento» mientras el jugador no lo haya elegido (<see cref="A11yRules.ReduceMotion"/>; pregunta
    /// Q-B9-3).</item>
    /// </list>
    /// Todo el juego lo pide a esta interfaz: en Android lo lee <c>AF.UI.AndroidSystemScale</c> (necesita el motor, que
    /// <c>AF.Platform</c> no tiene, como <c>AndroidDeviceLocale</c>); en el Editor y en los tests, <see cref="FakeSystemScale"/>.
    /// Los valores son los del sistema tal cual, sin recortar: un valor que no se pudo leer es <see cref="float.NaN"/>.
    /// </summary>
    public interface ISystemScale
    {
        /// <summary>Escala de fuente del sistema (1 = 100 %; Android: <c>Configuration.fontScale</c>), o NaN si no se sabe.</summary>
        float FontScale { get; }

        /// <summary>
        /// Escala de duración de animaciones del sistema (1 = normal; 0 = «Quitar animaciones»; Android:
        /// <c>Settings.Global.ANIMATOR_DURATION_SCALE</c>), o NaN si no se sabe.
        /// </summary>
        float AnimatorDurationScale { get; }
    }
}
