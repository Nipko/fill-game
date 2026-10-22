#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Reglas de accesibilidad sin motor (bloque B9; 02 §3.1 C9 y §12; 07 §3, regla 5, y §7.8; 08 §9). Solo cifras del GDD o
    /// marcadas [P] con su pregunta (nota <c>docs/dev/accesibilidad.md</c>); ni texto ni estado: quien llama pasa lo que sabe.
    /// </summary>
    public static class A11yRules
    {
        /// <summary>Tope de la escala de texto (02 §12, fila «Tamaño de texto»: hasta el 130 %; por encima se fija ese tope).</summary>
        public const float MaxTextScale = 1.3f;

        /// <summary>
        /// Suelo de la escala de texto [P] (Q-B9-2): 1. Con una fuente del sistema más pequeña los textos bajarían de 12 sp, el
        /// mínimo de 02 §12 (fila «Texto»), así que se quedan al 100 %, como ya hacía la escala de desarrollo.
        /// </summary>
        public const float MinTextScale = 1f;

        /// <summary>Opacidad del patrón en «Alto contraste» (07 §3, reglas 1 y 5; 02 §12, fila «Modo daltónico»; D-033, Q-N4 (a)): 35 %.</summary>
        public const float HighContrastPatternOpacity = 0.35f;

        /// <summary>
        /// Contorno de la dosis en «Alto contraste» (07 §3, regla 5): 1,5 px de tinta centrados en el arco de abajo de cada dosis
        /// (Q-B9-6; lo pinta el shader del líquido, <c>LiquidGraphic.ApplyDoseOutline</c>). Los px de 07 son de diseño, a 360 de
        /// ancho: el cliente los toma como dp [P] (1,5 px de pantalla no se ven en un teléfono de 1.080 px).
        /// </summary>
        public const float HighContrastDoseOutlinePx = 1.5f;

        /// <summary>C9 (02 §3.1 y §12): mantener al menos 400 ms sobre una cuia muestra los nombres de sus dosis.</summary>
        public const int HoldMs = 400;

        /// <summary>
        /// Lo que puede moverse el dedo y seguir siendo «sin mover el dedo» en C9 (dp) [P] (Q-B9-4): 24 dp, el umbral del arrastre
        /// (C10, 02 §3.1). Desde 24 dp el gesto es un arrastre y nunca un mantener pulsado, así que C9 y C10 no se pisan.
        /// </summary>
        public const float HoldSlopDp = 24f;

        /// <summary>
        /// Escala de texto del juego con la fuente del sistema <paramref name="systemFontScale"/> (02 §12): entre
        /// <see cref="MinTextScale"/> y <see cref="MaxTextScale"/>; un valor que no se sabe (NaN o infinito), 1.
        /// </summary>
        public static float TextScaleFor(float systemFontScale)
        {
            if (float.IsNaN(systemFontScale) || float.IsInfinity(systemFontScale))
            {
                return 1f;
            }

            return Math.Min(MaxTextScale, Math.Max(MinTextScale, systemFontScale));
        }

        /// <summary>El sistema tiene las animaciones quitadas (Android «Quitar animaciones»: escala de duración 0).</summary>
        public static bool SystemAnimationsOff(float animatorDurationScale)
        {
            return !float.IsNaN(animatorDurationScale) && animatorDurationScale <= 0f;
        }

        /// <summary>
        /// «Reducir movimiento» en vigor (07 §7.8; 08 §9 <c>ui_reduce_motion_default</c>): la elección del jugador
        /// <paramref name="choice"/> (<c>settings.reduce_motion</c> = <c>on</c>/<c>off</c>) si la hay; si no (<c>auto</c>), el
        /// default de Remote Config <paramref name="remoteDefault"/> o [P] (Q-B9-3) el sistema con las animaciones quitadas
        /// (<paramref name="animatorDurationScale"/> = 0).
        /// </summary>
        public static bool ReduceMotion(bool? choice, bool remoteDefault, float animatorDurationScale)
        {
            if (choice.HasValue)
            {
                return choice.Value;
            }

            return remoteDefault || SystemAnimationsOff(animatorDurationScale);
        }

        /// <summary>
        /// Factor de las duraciones del vertido (02 §4: todas se multiplican por <c>anim_reduce_motion_factor</c> con «Reducir
        /// movimiento»): <paramref name="factor"/> con <paramref name="reduceMotion"/>, 1 sin él. Un factor fuera de 0,3-0,7
        /// (08 §9) no llega aquí: lo descarta <see cref="A11yConfig.Validate"/>.
        /// </summary>
        public static float MotionFactor(bool reduceMotion, double factor)
        {
            return reduceMotion ? (float)factor : 1f;
        }

        /// <summary>
        /// Opacidad del patrón en el líquido (07 §3, reglas 1 y 5): <see cref="HighContrastPatternOpacity"/> en «Alto contraste»;
        /// si no, la del nivel <paramref name="levelOpacity"/> (15 % o, con Patrones de fruta, <c>ui_pattern_opacity</c> = 20 %).
        /// «Alto contraste» no cambia ningún HEX ni enciende los iconos: eso es Patrones de fruta (Q-B9-5).
        /// </summary>
        public static float PatternOpacity(bool highContrast, float levelOpacity)
        {
            return highContrast ? HighContrastPatternOpacity : levelOpacity;
        }
    }
}
