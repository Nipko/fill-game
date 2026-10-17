#nullable enable
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la instrucción del tutorial con TiTino en los niveles 1, 2, 3 y 6 (tarea TU1a; D-016, T2-A y T5-B; 02 §13; 04
    /// §2.1; 06 §2.2). Parcial de <see cref="UiText"/>: las claves las decide <see cref="TutorialInstruction"/> (AF.GameFlow
    /// devuelve claves, nunca texto) y aquí se leen de <c>strings.csv</c>. Nacen <c>pendiente_asesor</c>, como las demás
    /// <c>tutorial_*</c>; sin traducción, en EN y PT-BR quedan vacías (D-018).
    /// </summary>
    public static partial class UiText
    {
        /// <summary>La instrucción de <paramref name="key"/> (<see cref="TutorialInstruction.KeyFor"/>) en el idioma de la UI.</summary>
        public static string TutorialInstructionText(string key)
        {
            return Loc.T(key);
        }

        /// <summary>Claves de <c>strings.csv</c> (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>tutorial_finish_full</c> [P] (TU1a): la instrucción del nivel 1.</summary>
            public const string TutorialFinishFull = TutorialInstruction.FinishFullKey;

            /// <summary><c>tutorial_keep_one_empty</c> [P] (TU1a): la instrucción del nivel 2.</summary>
            public const string TutorialKeepOneEmpty = TutorialInstruction.KeepOneEmptyKey;

            /// <summary><c>tutorial_undo</c> [P] (TU1a): el tooltip de Marea del nivel 3 (04 §2.1).</summary>
            public const string TutorialUndo = TutorialInstruction.UndoKey;

            /// <summary><c>tutorial_one_empty</c> [P] (TU1a): la frase del nivel 6 (06 §2.2).</summary>
            public const string TutorialOneEmpty = TutorialInstruction.OneEmptyKey;
        }
    }
}
