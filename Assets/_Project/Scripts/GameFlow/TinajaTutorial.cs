#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Textos del estreno de la Tinaja (M13; D-019; 02 §9.1 M13, «Tutorial»; propuesta de la Tinaja, b.2 «Tutorial»; tarea TJ4),
    /// como claves de <c>strings.csv</c>, nunca texto (regla de localización). Regla pura y determinista, sin asignaciones:
    /// <list type="bullet">
    /// <item><description><b>Tarjeta del estreno</b> (la del nivel con <c>intro_mechanic</c> = <c>tinaja</c>, el 323 [P]): las dos
    /// líneas de 02 §9.1, cada una en su renglón de la tarjeta y ninguna de más de 2 líneas a 360 dp: el tutorial
    /// (<see cref="CardKey"/>, con la regla T4 de la tinaja) y la línea de su cuia (<see cref="ScoopKey"/>), donde la tarjeta del
    /// 100 lleva la de la Luciérnaga.</description></item>
    /// <item><description><b>Consejo del segundo tablero del estreno</b> (<see cref="TipLevel"/>, el 324 [P]; 02 §9.1: «en el
    /// segundo tablero del estreno»): una línea (<see cref="TipKey"/>) en el aviso de texto, al habilitarse el toque tras la
    /// intro, solo si ese nivel lleva tinaja (con la salida B del 323 el ciclo sale sin ella, D-019 Q-Ti3) y nunca al reanudar
    /// (sin intro).</description></item>
    /// </list>
    /// Los tres textos son [P] y nacen <c>pendiente_asesor</c> (gesto de sacar y estreno: punto expreso para el asesor, Q-Ti7).
    /// Nunca «barro», «porción», «servir», «atar» ni «desatar» (02 §9.1 M13).
    /// </summary>
    public static class TinajaTutorial
    {
        /// <summary><c>intro_mechanic</c> del estreno de la Tinaja (<c>pack_schema</c> 3; 08 §4.1).</summary>
        public const string IntroMechanic = "tinaja";

        /// <summary>Nivel del estreno (02 §9.1, lista única: 323 Tinaja; D-019) [P].</summary>
        public const int DebutLevel = 323;

        /// <summary>Segundo tablero del estreno, el del consejo (02 §9.1 M13) [P].</summary>
        public const int TipLevel = DebutLevel + 1;

        /// <summary><c>tutorial_tinaja</c> [P]: línea 1 del tutorial de la tarjeta del estreno (02 §9.1 M13).</summary>
        public const string CardKey = "tutorial_tinaja";

        /// <summary><c>tutorial_tinaja_scoop</c> [P]: línea 2 del tutorial, la de su cuia chica (02 §9.1 M13).</summary>
        public const string ScoopKey = "tutorial_tinaja_scoop";

        /// <summary><c>tutorial_tinaja_tip</c> [P]: el consejo del segundo tablero del estreno (02 §9.1 M13).</summary>
        public const string TipKey = "tutorial_tinaja_tip";

        /// <summary>
        /// <c>ui_tinaja_show_contents</c> (08 §9; D-019, Q-Ti5-A): la Tinaja enseña qué frutas guarda, sin el orden. Apagada: lo
        /// oculto no se ve por ningún lado, ni su color, ni su orden, ni qué frutas guarda (02 §9.1 M13, «Qué se ve»). Constante sin
        /// <c>[RemoteConfigKey]</c> hasta que exista <c>AF.RemoteConfig</c> en el cliente (<see cref="GameConfig.UiTinajaShowContents"/>).
        /// </summary>
        public static bool ShowsContents => GameConfig.UiTinajaShowContents;

        /// <summary>El estreno <paramref name="introMechanic"/> es el de la Tinaja.</summary>
        public static bool IsDebut(string? introMechanic)
        {
            return string.Equals(introMechanic, IntroMechanic, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// Clave del consejo del nivel <paramref name="level"/> (<see cref="TipKey"/> en <see cref="TipLevel"/> si el nivel lleva
        /// tinaja, <paramref name="hasTinaja"/>), o null. Sin asignaciones.
        /// </summary>
        public static string? TipKeyFor(int level, bool hasTinaja)
        {
            return level == TipLevel && hasTinaja ? TipKey : null;
        }
    }
}
