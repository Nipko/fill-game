#nullable enable
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del estreno de la Tinaja (M13; D-019; 02 §9.1 M13, «Tutorial»; propuesta de la Tinaja, b.2; tarea TJ4), de
    /// <c>strings.csv</c> con las claves de <see cref="TinajaTutorial"/>. Parcial de <see cref="UiText"/>. Los tres son [P] y
    /// nacen <c>pendiente_asesor</c>: nombran el objeto cultural y el gesto de sacar, punto expreso para el asesor (Q-Ti7).
    /// </summary>
    public static partial class UiText
    {
        /// <summary>
        /// Línea 1 del tutorial de la tarjeta del estreno (02 §9.1 M13; 323 [P]): la tinaja no deja ver y la de debajo nunca es
        /// igual (la regla T4 de su columna). ≤ 2 líneas a 360 dp con la fuente al 100 %.
        /// </summary>
        public static string TutorialTinaja => Loc.T(Keys.TutorialTinaja);

        /// <summary>
        /// Línea 2 del tutorial (02 §9.1 M13): se saca con su cuia chica, de dos dosis. Va en su propio renglón de la tarjeta, donde
        /// la del 100 lleva la línea de la Luciérnaga. ≤ 2 líneas a 360 dp.
        /// </summary>
        public static string TutorialTinajaScoop => Loc.T(Keys.TutorialTinajaScoop);

        /// <summary>
        /// Consejo de una línea del segundo tablero del estreno (02 §9.1 M13; 324 [P]): sacar dos veces deja la primera dosis
        /// debajo. En el aviso de texto, tras la intro.
        /// </summary>
        public static string TutorialTinajaTip => Loc.T(Keys.TutorialTinajaTip);

        /// <summary>Claves de <c>strings.csv</c> (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>tutorial_tinaja</c> [P] (TJ4).</summary>
            public const string TutorialTinaja = TinajaTutorial.CardKey;

            /// <summary><c>tutorial_tinaja_scoop</c> [P] (TJ4).</summary>
            public const string TutorialTinajaScoop = TinajaTutorial.ScoopKey;

            /// <summary><c>tutorial_tinaja_tip</c> [P] (TJ4).</summary>
            public const string TutorialTinajaTip = TinajaTutorial.TipKey;
        }
    }
}
