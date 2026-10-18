#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>
    /// La instrucción del tutorial que acompaña TiTino con su retrato y <c>sloth_present</c> (tareas TU1a y TU1b; D-016, T2-A y
    /// T5-B; 02 §13, párrafo «TiTino en el tutorial»; 06 §2.2; guion de la intro §14; propuesta de TiTino d.3, punto 2, y paso 6e
    /// de (i)): una por nivel, de ≤ 2 líneas, en los niveles <see cref="Levels"/> (1, 2, 3, 6 y 12). Regla pura y determinista,
    /// sin motor y sin asignaciones: devuelve claves de <c>strings.csv</c>, nunca texto (regla de localización). El 8 sigue siendo
    /// de Capi, que trae la Cuia extra (04 §2.2), y no está.
    /// <list type="bullet">
    /// <item>1: «Termina la cuia casi llena antes de apartar» (02 §13 y 06 §2.2, la lección del nivel; el dedo guía de 1-2 aún no
    /// existe en el cliente) [P];</item>
    /// <item>2: dos vacíos por primera vez, «reserva uno» (02 §13 y 06 §2.2) [P];</item>
    /// <item>3: el tooltip de Marea, «Marea · Deshacer: toca para devolver el agua» (04 §2.1; TiTino señala el botón con su
    /// retrato). En 1-10 Marea es ilimitada (<c>undo_unlimited_until_level</c> = 10), así que su primer uso no descuenta;</item>
    /// <item>6: «Guarda una cuia vacía: es tu espacio para pensar» (06 §2.2: la frase de TiTino al inicio del 6, con su retrato);</item>
    /// <item>12: la presentación de Morpho, «¿Atascado? La morpho conoce el río» (04 §2.3 y 06 §2.2: «al cargar el nivel 12 TiTino
    /// presenta el botón con su retrato y <c>sloth_present</c>»; tarea TU1b). Sale tras la intro y en el sitio de la del 1, 2 y 6 (02 §13;
    /// D-016, T5-B); si va sobre el muelle, la cola de su tarjeta señala el botón Pista [P]; los 3 Morphos regalados los acredita el flujo
    /// al cargar el nivel (<c>MorphoBooster.ShouldGift</c>, una sola vez; <c>hint_tutorial_units</c>). 06 §2.2 pide además que
    /// «la primera se gasta con dedo guía»: como en el 1-2, el dedo guía aún no existe en el cliente y la primera Pista se gasta
    /// sin él (pendiente; pregunta para el dueño) [P].</item>
    /// </list>
    /// Prioridad tras la intro (02 §4.2; 07 §6.4; guion de la intro §14): tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo. La
    /// instrucción entra la primera en la cola de avisos; la tarjeta del miquito y el rótulo no salen en estos niveles
    /// (<see cref="SightingRule.IsBusy"/>; <c>StopLabelRule</c>). Como mucho una tarjeta por nivel (D-023, punto 39; 02 §4.2;
    /// guion de la intro §14): en el 2 no sale «Fruta nueva: Taperebá» (<see cref="SightingRule.FruitNewAt"/> devuelve −1, como en
    /// el 1), aunque la fila del 2 de 06 §2.2 la liste (contradicción registrada para el dueño en la entrega de TU1a).
    /// <para>El tooltip de Marea del 3 no sale tras la intro sino tras el primer vertido que no acerca a la solución, medido con
    /// el solver (02 §13 y 06 §2.2: «la primera vez que un vertido aleja de la solución (distancia del solver +2)»; 04 §2.1: «tras
    /// ejecutarlo» el vertido tentador). Lectura débil [P]: en el tablero del 3 ningún vertido útil sube la distancia
    /// (tools/out/tutorial_v0_report.md), así que vale «no la baja», la de la comprobación del nivel 3. Sale una vez por sesión
    /// de juego, nunca en los reintentos [P]; el respaldo «al inicio del nivel 5 si no ocurrió» y «la primera vez» entre
    /// sesiones necesitan guardarlo en el perfil (pregunta para el dueño). TiTino señala el botón: la tarjeta va sobre el muelle,
    /// junto a la bandeja, con una cola que señala el botón de Marea (GameScreen.Cards.cs).</para>
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

        /// <summary>Clave de la presentación de Morpho del nivel 12 (<c>tutorial_morpho</c>; 04 §2.3; TU1b) [P].</summary>
        public const string MorphoKey = "tutorial_morpho";

        /// <summary>Nivel del tutorial de Marea (04 §2.1: tutorial en el nivel 3 [P]; 02 §13).</summary>
        public const int UndoLevel = 3;

        /// <summary>
        /// Nivel de la presentación de Morpho (04 §2.3 y §4: «Nivel 12» [P]; 02 §13): el default de <c>hint_unlock_level</c>. Si
        /// Remote Config moviera la ranura, la vista no la enseña en un nivel sin ranura Pista (GameScreen.Cards.cs).
        /// </summary>
        public const int MorphoLevel = 12;

        /// <summary>Niveles con instrucción del tutorial con TiTino, en orden (TU1a: 1, 2, 3 y 6; TU1b: 12).</summary>
        public static readonly int[] Levels = { 1, 2, UndoLevel, 6, MorphoLevel };

        // Clave de cada nivel de Levels, en el mismo orden.
        private static readonly string[] s_keys = { FinishFullKey, KeepOneEmptyKey, UndoKey, OneEmptyKey, MorphoKey };

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
        /// instrucción ya lo lleva; guion §14), nunca al reanudar, que va sin intro (02 §14); salvo el tooltip de Marea del 3, que
        /// sale una sola vez (<see cref="PointsAtUndo"/>).
        /// </summary>
        public static string? KeyFor(int level)
        {
            int i = Array.IndexOf(Levels, level);
            return i < 0 ? null : s_keys[i];
        }

        /// <summary>
        /// La instrucción de <paramref name="level"/> es el tooltip de Marea (el tutorial del 3; 04 §2.1): sale tras el primer
        /// vertido que no acerca a la solución [P] y TiTino señala el botón.
        /// </summary>
        public static bool PointsAtUndo(int level)
        {
            return level == UndoLevel;
        }

        /// <summary>
        /// La instrucción de <paramref name="level"/> es la presentación de Morpho (el 12; 04 §2.3: «TiTino presenta el botón»):
        /// sale tras la intro y TiTino señala el botón Pista [P].
        /// </summary>
        public static bool PointsAtHint(int level)
        {
            return level == MorphoLevel;
        }
    }
}
