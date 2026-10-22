#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Cifras de accesibilidad de Remote Config con el <b>default compilado</b> de la tabla única de 08 §9 (bloque B9):
    /// <c>ui_reduce_motion_default</c> (el ajuste inicial de «Reducir movimiento»; dueño 07) y <c>anim_reduce_motion_factor</c>
    /// (el multiplicador de las duraciones con «Reducir movimiento»; 02 §4 y §12, rango 0,3-0,7; dueño 02). Vive en
    /// <c>AF.Platform</c>, como <see cref="ClockSecurityConfig"/>, porque la leen la pantalla del nivel, el arranque y Ajustes y
    /// porque <c>AF.RemoteConfig.Binding</c> tiene que poder verla sin referenciar <c>AF.UI</c> (08 §2.2). La lee de Remote Config
    /// <c>AF.RemoteConfig.Binding.A11yConfigBinder</c>, desde <c>RcConfigBinder</c> (<c>RcConfigs.A11y</c>).
    /// </summary>
    public sealed class A11yConfig
    {
        /// <summary>Límite inferior de <see cref="AnimReduceMotionFactor"/> (08 §9: rango 0,3-0,7).</summary>
        public const double MinReduceMotionFactor = 0.3;

        /// <summary>Límite superior de <see cref="AnimReduceMotionFactor"/> (08 §9: rango 0,3-0,7).</summary>
        public const double MaxReduceMotionFactor = 0.7;

        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static A11yConfig Defaults => new A11yConfig();

        /// <summary>
        /// <c>ui_reduce_motion_default</c> (08 §9: bool, false; 07 §7.8): el valor de «Reducir movimiento» mientras el jugador no
        /// lo haya elegido en Ajustes (<c>settings.reduce_motion</c> = <c>auto</c> [P]).
        /// </summary>
        [RemoteConfigKey("ui_reduce_motion_default")]
        public bool UiReduceMotionDefault { get; set; }

        /// <summary>
        /// <c>anim_reduce_motion_factor</c> (08 §9: float, 0,5, rango 0,3-0,7; 02 §4 y §12): con «Reducir movimiento», todas las
        /// duraciones del vertido se multiplican por este factor.
        /// </summary>
        [RemoteConfigKey("anim_reduce_motion_factor")]
        public double AnimReduceMotionFactor { get; set; } = 0.5;

        /// <summary>
        /// Comprueba que los valores tienen sentido: el factor dentro del rango de 08 §9 (0,3-0,7). No comprueba que coincidan con
        /// 08 §9: eso lo hacen los tests.
        /// </summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (double.IsNaN(AnimReduceMotionFactor) || AnimReduceMotionFactor < MinReduceMotionFactor || AnimReduceMotionFactor > MaxReduceMotionFactor)
            {
                throw new InvalidOperationException("A11yConfig: anim_reduce_motion_factor debe estar en 0,3-0,7 (08 §9).");
            }
        }
    }
}
