#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.GameFlow
{
    /// <summary>
    /// La instrucción del tutorial que acompaña TiTino con su retrato y <c>sloth_present</c> (tareas TU1a y TU1b; D-016, T2-A y
    /// T5-B; 02 §13, párrafo «TiTino en el tutorial»; 06 §2.2; guion de la intro §14; propuesta de TiTino d.3, punto 2, y paso 6e
    /// de (i)): una por nivel, de ≤ 2 líneas, en los niveles <see cref="Levels"/> (1, 2, 3, 6 y 12). Regla pura y determinista,
    /// sin motor y sin asignaciones: devuelve claves de <c>strings.csv</c>, nunca texto (regla de localización). El 8 sigue siendo
    /// de Capi, que trae la Cuia extra (04 §2.2), y no está.
    /// <list type="bullet">
    /// <item>1: «Termina la cuia casi llena antes de apartar» (02 §13 y 06 §2.2, la lección del nivel) [P], con el dedo guía en los
    /// dos primeros toques (<see cref="FingerFor"/>: <see cref="FingerLesson.FirstPour"/>);</item>
    /// <item>2: dos vacíos por primera vez, «reserva uno» (02 §13 y 06 §2.2) [P], con el dedo guía en el vertido parcial de la
    /// lección, una racha de 3 sobre un hueco de 2 (<see cref="FingerLesson.PartialRun"/>; D-005);</item>
    /// <item>3: el tooltip de Marea, «Marea · Deshacer: toca para devolver el agua» (04 §2.1; TiTino señala el botón con su
    /// retrato). En 1-10 Marea es ilimitada (<c>undo_unlimited_until_level</c> = 10), así que su primer uso no descuenta;</item>
    /// <item>6: «Guarda una cuia vacía: es tu espacio para pensar» (06 §2.2: la frase de TiTino al inicio del 6, con su retrato);</item>
    /// <item>12: la presentación de Morpho, «¿Atascado? La morpho conoce el río» (04 §2.3 y 06 §2.2: «al cargar el nivel 12 TiTino
    /// presenta el botón con su retrato y <c>sloth_present</c>»; tarea TU1b). Sale tras la intro y en el sitio de la del 1, 2 y 6 (02 §13;
    /// D-016, T5-B); si va sobre el muelle, la cola de su tarjeta señala el botón Pista [P]; los 3 Morphos regalados los acredita el flujo
    /// al cargar el nivel (<c>MorphoBooster.ShouldGift</c>, una sola vez; <c>hint_tutorial_units</c>). Sin dedo guía (D-028,
    /// pregunta 61 (a) de GDD.md §6.3): la presentación de TiTino con la cola sobre «Pista» hace de guía, y la primera Pista se
    /// gasta sin dedo.</item>
    /// </list>
    /// <para><b>Dedo guía</b> (tarea DC6; D-028, pregunta 61 (a) «con P5»: P5 de decisiones.md §5 respondida «Sí»; 02 §13, filas
    /// 1 y 2; 06 §2.2; 02 §4, D-002: ayuda explícita del tutorial, que solo señala el vertido de la lección y nunca resalta
    /// destinos al seleccionar): solo en el 1 y el 2 (<see cref="FingerFor"/>); ningún otro nivel lo lleva, tampoco el 6 (02 §13:
    /// «sin dedo guía»), el 12 (D-028) ni el 8 (el de la Cuia extra llega con Capi). Esta clase dice qué niveles lo llevan y qué
    /// vertido cuenta como el de la lección (<see cref="IsPartialRunLesson"/>); el vertido concreto lo elige la vista con el solver
    /// sobre el tablero del momento (AF.UI, <c>TutorialFinger.TryChoose</c>), porque el pack no guarda la solución. Además, con
    /// el mismo componente, dos estrenos de 02 §9.1 (tarea DC7; D-029: «sí, con el componente de DC6»): el de la Hoja (43, M1,
    /// «dedo guía en el primer uso») y el de la Tinaja (323, M13, «tinaja → su cuia y, después, su cuia → destino»):
    /// <see cref="FingerFor(int, string)"/>, por el <c>intro_mechanic</c> del nivel.</para>
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

        /// <summary>Racha de la lección del dedo guía del 2 (02 §13: «una racha de 3 sobre un hueco de 2»; D-005).</summary>
        public const int FingerLessonRun = 3;

        /// <summary>Hueco del destino en la lección del dedo guía del 2 (02 §13).</summary>
        public const int FingerLessonHole = 2;

        /// <summary>
        /// El vertido que señala el dedo guía de <paramref name="level"/> (D-028 con P5; 02 §13): el primero de la solución en el
        /// 1, el parcial de la lección en el 2 y <see cref="FingerLesson.None"/> en los demás (el 12, sin dedo: D-028, 61 a).
        /// </summary>
        public static FingerLesson FingerFor(int level)
        {
            switch (level)
            {
                case 1: return FingerLesson.FirstPour;
                case 2: return FingerLesson.PartialRun;
                default: return FingerLesson.None;
            }
        }

        /// <summary><paramref name="level"/> lleva dedo guía (solo el 1 y el 2).</summary>
        public static bool HasFinger(int level)
        {
            return FingerFor(level) != FingerLesson.None;
        }

        /// <summary><c>intro_mechanic</c> del estreno de la Hoja (el 43; 02 §9.1 M1; 08 §4.1).</summary>
        public const string HojaIntroMechanic = "hoja";

        /// <summary>
        /// El vertido que señala el dedo guía del nivel <paramref name="level"/> con el estreno <paramref name="introMechanic"/>
        /// (su <c>intro_mechanic</c>, o null): el del tutorial (<see cref="FingerFor(int)"/>, el 1 y el 2) y, además (tarea DC7;
        /// D-028 con P5 y D-029: «sí, con el componente de DC6»), el de dos estrenos de 02 §9.1:
        /// <list type="bullet">
        /// <item><description>M1 Hoja (<c>hoja</c>, el 43): «dedo guía en el primer uso», un vertido de la fruta de la hoja a la
        /// cuia con hoja (<see cref="FingerLesson.HojaKey"/>, <see cref="IsHojaLesson"/>);</description></item>
        /// <item><description>M13 Tinaja (<c>tinaja</c>, el 323 de la variante «a»): «con dedo guía: tinaja → su cuia y, después,
        /// su cuia → destino» (<see cref="FingerLesson.TinajaScoop"/>, en dos pasos). Con la variante «b» el 323 no lleva
        /// tinaja ni <c>intro_mechanic</c> y no hay dedo.</description></item>
        /// </list>
        /// Va por el estreno y no por el número de nivel: los niveles siguientes con Hoja o con Tinaja no lo llevan (ni la Hoja
        /// atada del 283, <c>hoja_atada</c>). Sin asignaciones.
        /// </summary>
        public static FingerLesson FingerFor(int level, string? introMechanic)
        {
            if (string.Equals(introMechanic, HojaIntroMechanic, StringComparison.Ordinal))
            {
                return FingerLesson.HojaKey;
            }

            if (TinajaTutorial.IsDebut(introMechanic))
            {
                return FingerLesson.TinajaScoop;
            }

            return FingerFor(level);
        }

        /// <summary>
        /// El vertido de <paramref name="source"/> a <paramref name="target"/> es el del dedo guía del estreno de la Hoja sobre
        /// <paramref name="state"/> (02 §9.1 M1; DC7): legal y a una cuia que aún tiene la hoja, así que es de su fruta (la hoja
        /// solo deja pasar su <c>key</c>) y la abre. Sin asignaciones.
        /// </summary>
        public static bool IsHojaLesson(BoardState state, int source, int target)
        {
            if (state == null || source == target || (uint)source >= (uint)state.Count || (uint)target >= (uint)state.Count)
            {
                return false;
            }

            return state[target].Mechanic == MechanicType.Hoja && PourRules.IsLegal(state, source, target);
        }

        /// <summary>
        /// El vertido de <paramref name="source"/> a <paramref name="target"/> es el paso 1 del dedo guía del estreno de la Tinaja
        /// (02 §9.1 M13; DC7): sacar, de la tinaja a su cuia colgada, legal. Sin asignaciones.
        /// </summary>
        public static bool IsTinajaScoop(BoardState state, int source, int target)
        {
            if (state == null || source == target || (uint)source >= (uint)state.Count || (uint)target >= (uint)state.Count)
            {
                return false;
            }

            return state[source].Mechanic == MechanicType.Tinaja && PourRules.IsLegal(state, source, target);
        }

        /// <summary>
        /// El vertido de <paramref name="source"/> a <paramref name="target"/> es el de la lección del 2 sobre
        /// <paramref name="state"/> (02 §13: «vertido parcial: una racha de 3 sobre un hueco de 2», en modo <c>run</c>, D-005):
        /// legal, con una racha visible de al menos <see cref="FingerLessonRun"/> dosis en el origen y un destino que no está vacío
        /// y al que le caben exactamente <see cref="FingerLessonHole"/>, así que pasan 2 y el resto se queda (el parcial). En
        /// <c>single</c> (solo pruebas) no hay parcial y nunca lo es. Sin asignaciones.
        /// </summary>
        public static bool IsPartialRunLesson(BoardState state, int source, int target, PourMode mode)
        {
            if (state == null || mode != PourMode.Run || source == target || (uint)source >= (uint)state.Count
                || (uint)target >= (uint)state.Count)
            {
                return false;
            }

            Cuia to = state[target];
            if (to.IsEmpty || state[source].TopRunLength < FingerLessonRun || !PourRules.IsLegal(state, source, target))
            {
                return false;
            }

            int amount = PourRules.Amount(state, source, target, mode);
            return amount == FingerLessonHole && amount < state[source].TopRunLength;
        }
    }

    /// <summary>Qué vertido señala el dedo guía de un nivel del tutorial (<see cref="TutorialInstruction.FingerFor"/>).</summary>
    public enum FingerLesson
    {
        /// <summary>Sin dedo guía.</summary>
        None = 0,

        /// <summary>El primer vertido de la solución desde el tablero del intento (el 1: «los dos primeros toques»).</summary>
        FirstPour = 1,

        /// <summary>El vertido parcial de la lección, una racha de 3 sobre un hueco de 2, en cuanto es legal (el 2).</summary>
        PartialRun = 2,

        /// <summary>
        /// Un vertido de la fruta de la hoja a la cuia con hoja, en cuanto es legal (el estreno de la Hoja, el 43; 02 §9.1 M1;
        /// DC7).
        /// </summary>
        HojaKey = 3,

        /// <summary>
        /// En dos pasos: tinaja → su cuia y, después, su cuia → destino (el estreno de la Tinaja, el 323; 02 §9.1 M13; DC7).
        /// </summary>
        TinajaScoop = 4,
    }
}
