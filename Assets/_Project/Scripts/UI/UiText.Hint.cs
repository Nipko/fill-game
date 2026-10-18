#nullable enable
using AF.Boosters;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de Morpho · Pista (tarea MO2; 04 §2.3; 02 T5; D-019; glosario §5.1 y §10), de <c>strings.csv</c>. Parcial de
    /// <see cref="UiText"/>. Las respuestas que nombran fauna (la luciérnaga, la morpho) nacen <c>pendiente_asesor</c>; los
    /// botones de la tarjeta de respuesta son los de siempre (<see cref="Undo"/> y <see cref="RestartLong"/>).
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>hint</c>: etiqueta funcional de Morpho en la bandeja (glosario §10: «Pista»).</summary>
        public static string HintLabel => Loc.T(Keys.Hint);

        /// <summary><c>hint_unsupported</c>: el globo de la ranura gris en un nivel sin <c>hint_supported</c> (04 §2.3 b).</summary>
        public static string HintUnsupported => Loc.T(Keys.HintUnsupported);

        /// <summary>
        /// La frase de una respuesta de Morpho sin vertido (04 §2.3; 02 T5, en su orden; D-019): «Desde aquí no hay salida…»,
        /// «No veo un vertido seguro…» (con la tinaja si <paramref name="tinaja"/>) o «No encuentro un camino desde aquí». Con
        /// <see cref="HintAnswer.Pour"/> no hay frase: vacío.
        /// </summary>
        public static string HintAnswerText(HintAnswer answer, bool tinaja)
        {
            return Loc.T(HintAnswerKey(answer, tinaja));
        }

        /// <summary>La clave de <see cref="HintAnswerText"/> (vacía con <see cref="HintAnswer.Pour"/>; tests).</summary>
        public static string HintAnswerKey(HintAnswer answer, bool tinaja)
        {
            switch (answer)
            {
                case HintAnswer.NoExit: return Keys.HintNoExit;
                case HintAnswer.NoSafePour: return tinaja ? Keys.HintNoSafePourTinaja : Keys.HintNoSafePour;
                case HintAnswer.NotFound: return Keys.HintNotFound;
                default: return string.Empty;
            }
        }

        /// <summary>Claves de <c>strings.csv</c> (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>hint</c> (glosario §10).</summary>
            public const string Hint = "hint";

            /// <summary><c>hint_no_exit</c> [P]: respuesta 2 de 02 T5.</summary>
            public const string HintNoExit = "hint_no_exit";

            /// <summary><c>hint_no_safe_pour</c> [P]: respuesta 3 de 02 T5 (cuias turbias).</summary>
            public const string HintNoSafePour = "hint_no_safe_pour";

            /// <summary><c>hint_no_safe_pour_tinaja</c> [P]: respuesta 3 de 02 T5 con tinaja.</summary>
            public const string HintNoSafePourTinaja = "hint_no_safe_pour_tinaja";

            /// <summary><c>hint_not_found</c> [P]: respuesta 4 de 02 T5 (03 §5).</summary>
            public const string HintNotFound = "hint_not_found";

            /// <summary><c>hint_unsupported</c> [P]: 04 §2.3 b.</summary>
            public const string HintUnsupported = "hint_unsupported";
        }
    }
}
