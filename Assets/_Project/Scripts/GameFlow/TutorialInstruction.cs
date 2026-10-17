#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>
    /// La instrucción del tutorial que acompaña TiTino con su retrato y <c>sloth_present</c> (tarea TU1a; D-016, T2-A y T5-B;
    /// 02 §13, párrafo «TiTino en el tutorial»; 06 §2.2; guion de la intro §14; propuesta de TiTino d.3, punto 2, y paso 6e de
    /// (i)): una por nivel, de ≤ 2 líneas, en los niveles <see cref="Levels"/> (1, 2, 3 y 6). Regla pura y determinista, sin motor
    /// y sin asignaciones: devuelve claves de <c>strings.csv</c>, nunca texto (regla de localización). La presentación de Morpho
    /// del 12 es de TU1b, que añade su nivel aquí; el 8 sigue siendo de Capi, que trae la Cuia extra (04 §2.2), y no está.
    /// <list type="bullet">
    /// <item>1: «Termina la cuia casi llena antes de apartar» (02 §13 y 06 §2.2, la lección del nivel; el dedo guía de 1-2 aún no
    /// existe en el cliente) [P];</item>
    /// <item>2: dos vacíos por primera vez, «reserva uno» (02 §13 y 06 §2.2) [P];</item>
    /// <item>3: el tooltip de Marea, «Marea · Deshacer: toca para devolver el agua» (04 §2.1; TiTino señala el botón con su
    /// retrato). En 1-10 Marea es ilimitada (<c>undo_unlimited_until_level</c> = 10), así que su primer uso no descuenta;</item>
    /// <item>6: «Guarda una cuia vacía: es tu espacio para pensar» (06 §2.2: la frase de TiTino al inicio del 6, con su retrato).</item>
    /// </list>
    /// Prioridad tras la intro (02 §4.2; 07 §6.4; guion de la intro §14): tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo. La
    /// instrucción entra la primera en la cola de avisos; la tarjeta del miquito y el rótulo no salen en estos niveles
    /// (<see cref="SightingRule.IsBusy"/>; <c>StopLabelRule</c>). En el 2, «Fruta nueva: Taperebá» sale después de la instrucción,
    /// como lista 06 §2.2 para ese nivel [P] (02 §4.2 dice «como mucho una tarjeta por nivel»: pregunta abierta).
    /// </summary>
    public static class TutorialInstruction
    {
        /// <summary>Clave de la instrucción del nivel 1 (<c>tutorial_finish_full</c>) [P].</summary>
        public const string FinishFullKey = "tutorial_finish_full";

        /// <summary>Clave de la instrucción del nivel 2 (<c>tutorial_keep_one_empty</c>) [P].</summary>
        public const string KeepOneEmptyKey = "tutorial_keep_one_empty";

        /// <summary>Clave del tooltip de Marea del nivel 3 (<c>tutorial_undo</c>; 04 §2.1) [P].</summary>
        public const string UndoKey = "tutorial_undo";

        /// <summary>Clave de la frase del nivel 6 (<c>tutorial_one_empty</c>; 06 §2.2) [P].</summary>
        public const string OneEmptyKey = "tutorial_one_empty";

        /// <summary>Nivel del tutorial de Marea (04 §2.1: tutorial en el nivel 3 [P]; 02 §13).</summary>
        public const int UndoLevel = 3;

        /// <summary>Niveles con instrucción del tutorial con TiTino, en orden (TU1a: 1, 2, 3 y 6; el 12 llega con TU1b).</summary>
        public static readonly int[] Levels = { 1, 2, UndoLevel, 6 };

        // Clave de cada nivel de Levels, en el mismo orden.
        private static readonly string[] s_keys = { FinishFullKey, KeepOneEmptyKey, UndoKey, OneEmptyKey };

        /// <summary>
        /// Las claves de las instrucciones, en el orden de <see cref="Levels"/> (tests: todas en <c>strings.csv</c>). Una copia
        /// nueva en cada llamada: no se usa en el juego.
        /// </summary>
        public static string[] Keys => (string[])s_keys.Clone();

        /// <summary><paramref name="level"/> tiene instrucción del tutorial con TiTino.</summary>
        public static bool Has(int level)
        {
            return Array.IndexOf(Levels, level) >= 0;
        }

        /// <summary>
        /// Clave de la instrucción de <paramref name="level"/> o null si no tiene. Sin asignaciones. Sale en cada intento con su
        /// intro (el primero y tras Volver a la orilla: en estos niveles no hay aviso de TiTino tras el reinicio, porque la
        /// instrucción ya lo lleva; guion §14), nunca al reanudar, que va sin intro (02 §14).
        /// </summary>
        public static string? KeyFor(int level)
        {
            int i = Array.IndexOf(Levels, level);
            return i < 0 ? null : s_keys[i];
        }

        /// <summary>La instrucción de <paramref name="level"/> señala el botón de Marea (el tutorial del 3; 04 §2.1).</summary>
        public static bool PointsAtUndo(int level)
        {
            return level == UndoLevel;
        }
    }
}
