#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Session
{
    /// <summary>
    /// Partida de un nivel en el núcleo: estado actual, vertidos con la evaluación de R10, historial de Marea
    /// (04 §2.1) y Volver a la orilla (04 §2.7). No sabe nada de pantallas, animaciones ni cupos de comodines.
    /// </summary>
    /// <remarks>
    /// <para>Invariantes (docs/dev/prototipo-v0.md, a):</para>
    /// <list type="bullet">
    /// <item><description>I1: <see cref="Status"/> se recalcula tras cada cambio en el orden de R10: victoria (R12) y
    /// después bloqueo: R13 (sin vertidos útiles) y, si aún queda alguno, R13 ampliada (D-014): <see cref="DeadEndProbe"/>
    /// recorre los estados alcanzables con todos los vertidos legales (opción (a) de la pregunta 46 de <c>docs/GDD.md</c>
    /// §6.3, decidida el 2026-10-13; D-020) y, si son como mucho 64 [P] y ninguno es victoria, la partida queda en
    /// <see cref="LevelStatus.Stuck"/> aunque le queden vertidos útiles (el vaivén sin salida del nivel 66).
    /// Con más de 64 estados o una victoria alcanzable sigue en <see cref="LevelStatus.Playing"/> (conservador). No depende de
    /// <see cref="FutileRuleEnabled"/>.</description></item>
    /// <item><description>I2: un vertido ilegal (R11) o con <see cref="Status"/> distinto de
    /// <see cref="LevelStatus.Playing"/> devuelve false y no cambia nada (con Aguas estancadas el input está
    /// bloqueado desde el instante lógico del bloqueo, R15).</description></item>
    /// <item><description>I3: <see cref="Undo"/> restaura el estado anterior exacto, reabre los sellos y resta 1 a
    /// <see cref="AppliedPours"/>; con Aguas turbias, con las dosis ya reveladas visibles (I5).</description></item>
    /// <item><description>I4: <see cref="Restart"/> deja <c>Current.Equals(Initial)</c> y el historial vacío; con Aguas
    /// turbias, <see cref="Initial"/> con las dosis ya reveladas visibles (I5).</description></item>
    /// <item><description>I5 (02 R7, T6; 04 §2.1, §2.5, §2.7): una dosis oculta revelada (al quedar arriba, con
    /// <see cref="Reveal"/>, con <see cref="RevealAll"/> o restaurada con <see cref="RestoreRevealed"/>) no vuelve a ocultarse
    /// en todo el nivel, tampoco tras Marea ni tras Volver a la orilla: «lo único que no se revierte es el
    /// conocimiento».</description></item>
    /// </list>
    /// <para>Modo de vertido (<see cref="AF.Core.Rules.PourMode"/>; por defecto <see cref="AF.Core.Rules.PourMode.Run"/>,
    /// D-005): se fija al crear la partida y no cambia a mitad de nivel, porque el historial de Marea y el par dependen de
    /// él (punto 3 de D-001, sustituida por D-005, que lo conserva). En <see cref="AF.Core.Rules.PourMode.Run"/> cada
    /// vertido mueve la racha que cabe: <see cref="AppliedPours"/> cuenta vertidos y <see cref="Undo"/> deshace el vertido
    /// entero (02 §1.2; 04 §2.1). En <see cref="AF.Core.Rules.PourMode.Single"/> (solo pruebas) cada vertido mueve una
    /// dosis, así que <see cref="AppliedPours"/> cuenta dosis movidas y <see cref="Undo"/> deshace una dosis por uso.
    /// Victoria y bloqueo se evalúan igual en los dos modos.</para>
    /// <para><b>Aguas turbias</b> (02 §11): la partida lleva una máscara de revelado del nivel (<see cref="RevealedLayers"/>),
    /// indexada por la posición inicial <c>[cuia, capa]</c> de cada dosis oculta. Es exacta porque una dosis oculta nunca se
    /// mueve: nunca está en la cima (R7), así que nada la vierte ni se apila directamente sobre ella, y el lavado del Barro
    /// sustituye el barro sin dejarla arriba. Por eso un tablero con dosis ocultas y Remolino (que inserta por el fondo y
    /// desplaza las capas) se rechaza: <see cref="LevelValidation"/> ya prohíbe esa combinación [P]. La Luciérnaga regalada
    /// del nivel 100 la da el cliente (04 §2.5), no el núcleo. Las mecánicas (02 §9) las aplica
    /// <see cref="BoardRules"/> en cada vertido con R10 por etapas (anexo B de D-007); Marea restaura el estado anterior
    /// entero, así que vuelve a cerrar lo que el vertido abrió o soltó.</para>
    /// <para>No es seguro entre hilos.</para>
    /// </remarks>
    public sealed class LevelSession
    {
        private readonly List<BoardState> _previous = new List<BoardState>();
        private readonly List<Pour> _pours = new List<Pour>();

        // Máscara de revelado del nivel (02 T6): bit k de la entrada i = la dosis oculta inicial [i, k] ya se reveló.
        private readonly int[] _revealed;

        // Cierre acotado de R13 ampliada (D-014): búferes propios, reservados una vez por partida.
        private readonly DeadEndProbe _deadEnd = new DeadEndProbe();

        /// <summary>Empieza una partida en <paramref name="initial"/> con el modo de vertido <paramref name="pourMode"/>.</summary>
        /// <param name="initial">Tablero inicial del nivel.</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> (08 §9; kill-switch de R16).</param>
        /// <param name="pourMode">Modo de vertido del nivel (<c>rules_pour_mode</c>, por defecto
        /// <see cref="PourModes.Default"/> = <see cref="AF.Core.Rules.PourMode.Run"/>, D-005;
        /// <see cref="AF.Core.Rules.PourMode.Single"/> solo para pruebas).</param>
        /// <exception cref="NotSupportedException">El tablero tiene dosis ocultas y un Remolino (combinación prohibida [P]).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pourMode"/> no es un valor definido.</exception>
        public LevelSession(BoardState initial, bool futileRuleEnabled, PourMode pourMode)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            PourModes.Check(pourMode, nameof(pourMode));
            if (initial.HasHidden && HasRemolino(initial))
            {
                throw new NotSupportedException("Aguas turbias no se combina con el Remolino [P]: su inserción por el fondo desplazaría las dosis ocultas y la máscara de revelado (02 T6).");
            }

            _revealed = new int[initial.Count];
            Initial = initial;
            Current = initial;
            FutileRuleEnabled = futileRuleEnabled;
            PourMode = pourMode;
            Status = Evaluate(initial);
        }

        /// <summary>
        /// Heredado sin modo: equivale a <see cref="AF.Core.Rules.PourMode.Run"/>, que desde D-005 es también
        /// <see cref="PourModes.Default"/>. Conserva el comportamiento anterior para que Unity y <c>tools/</c> sigan
        /// compilando; T6 de decisiones.md (retirar esta sobrecarga) sigue pendiente.
        /// </summary>
        /// <exception cref="NotSupportedException">El tablero tiene dosis ocultas y un Remolino (combinación prohibida [P]).</exception>
        public LevelSession(BoardState initial, bool futileRuleEnabled)
            : this(initial, futileRuleEnabled, PourMode.Run)
        {
        }

        /// <summary>
        /// Tablero inicial del nivel (misma semilla y misma asignación de capas ocultas en cada reinicio, 04 §2.7). No cambia:
        /// lo revelado está en <see cref="RevealedLayers"/> y se aplica a <see cref="Current"/>.
        /// </summary>
        public BoardState Initial { get; }

        /// <summary>Tablero actual.</summary>
        public BoardState Current { get; private set; }

        /// <summary>Estado de la partida (I1).</summary>
        public LevelStatus Status { get; private set; }

        /// <summary>
        /// Si se excluyen los vertidos fútiles al detectar el bloqueo (R13, R16). R13 ampliada (<see cref="DeadEndProbe"/>, D-014)
        /// no depende de él: recorre siempre todos los vertidos legales.
        /// </summary>
        public bool FutileRuleEnabled { get; }

        /// <summary>
        /// Modo de vertido de esta partida (por defecto <see cref="AF.Core.Rules.PourMode.Run"/>, D-005), fijo durante todo
        /// el nivel (punto 3 de D-001, sustituida por D-005, que lo conserva).
        /// </summary>
        public PourMode PourMode { get; }

        /// <summary>
        /// Vertidos de este intento que siguen en el historial de Marea: en <see cref="AF.Core.Rules.PourMode.Run"/>, cada
        /// vertido cuenta 1 aunque mueva una racha (02 §1.2); en <see cref="AF.Core.Rules.PourMode.Single"/> (pruebas), dosis
        /// movidas.
        /// </summary>
        public int AppliedPours => _pours.Count;

        /// <summary>Hay historial que Marea puede deshacer (04 §2.1).</summary>
        public bool CanUndo => _pours.Count > 0;

        /// <summary>Dosis ocultas que quedan en <see cref="Current"/> (Aguas turbias, 02 §11).</summary>
        public int HiddenCount => Current.HiddenCount;

        /// <summary>
        /// Contador visible del nivel para el HUD (02 R8, §9.1 M6-M7): vertidos útiles hasta la próxima gota de Lluvia o el
        /// próximo mordisco de la Piraña, y las gotas pendientes. Solo baja con vertidos útiles: un vertido fútil no lo avanza
        /// (D-007, Q18 B). Marea y Volver a la orilla lo restauran con el estado. Sin Lluvia ni Piraña (o cuando la nube ya se
        /// fue), <see cref="LevelCounter.IsActive"/> es false.
        /// </summary>
        public LevelCounter Counter => LevelCounter.Of(Current);

        /// <summary>
        /// Máscara de revelado del nivel (02 R8: parte del estado persistente) para la cuia <paramref name="cuia"/>: bit k =
        /// la dosis oculta inicial <c>[cuia, k]</c> ya se reveló y no vuelve a ocultarse (02 R7, T6).
        /// </summary>
        public int RevealedLayers(int cuia)
        {
            CheckIndex(cuia, nameof(cuia));
            return _revealed[cuia];
        }

        /// <summary>
        /// Vertido <paramref name="index"/> del historial, del primero (0) al último (<c>AppliedPours − 1</c>).
        /// Rehacerlos desde <see cref="Initial"/> con el mismo <see cref="PourMode"/> reconstruye <see cref="Current"/>
        /// (02 R8), salvo tras una Pororoca (<see cref="ApplyShuffle"/>), que vacía el historial y cambia el tablero.
        /// </summary>
        public Pour GetAppliedPour(int index)
        {
            if ((uint)index >= (uint)_pours.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera del historial de vertidos.");
            }

            return _pours[index];
        }

        /// <summary>
        /// Intenta el vertido A→B (R9) en el modo <see cref="PourMode"/> de la partida. Si es legal y la partida está en
        /// <see cref="LevelStatus.Playing"/>, lo aplica, lo guarda en el historial, recalcula <see cref="Status"/> y
        /// devuelve true. Si no, devuelve false y no cambia nada (I2, R11). Un vertido fútil es legal y se acepta (R13).
        /// En <see cref="AF.Core.Rules.PourMode.Run"/> (por defecto, D-005) mueve la racha visible que cabe y el resto queda
        /// en A (vertido parcial, R6, R9); en <see cref="AF.Core.Rules.PourMode.Single"/> (pruebas), una sola dosis aunque
        /// haya racha.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Algún índice está fuera del tablero.</exception>
        public bool TryPour(int source, int target, out PourResult result)
        {
            CheckIndex(source, nameof(source));
            CheckIndex(target, nameof(target));
            result = default;
            if (Status != LevelStatus.Playing)
            {
                return false;
            }

            int amount = PourRules.Amount(Current, source, target, PourMode);
            if (amount == 0)
            {
                return false;
            }

            // R10 en el orden del anexo B: (a)-(f′) los aplica BoardRules sobre la copia y (g)-(h) se evalúan después.
            BoardState current = Current;
            bool futile = BoardRules.IsFutile(current.CuiasArray, 0, current.Count, current.UnitsTable, source, target);
            var events = default(PourEvents);
            BoardState next = current.WithPour(source, target, PourMode, futile, ref events);
            RecordReveals(current, next);
            var pour = new Pour(source, target, events.Amount);
            _previous.Add(current);
            _pours.Add(pour);
            Current = next;
            Status = Evaluate(next);
            events.Finish(Status);
            result = new PourResult(pour, next.IsSealed(events.Receiver), Status, events, LevelCounter.Of(next));
            return true;
        }

        /// <summary>
        /// Marea (04 §2.1): deshace el último vertido y restaura el estado anterior exacto, también desde
        /// Aguas estancadas o desde la victoria, salvo el conocimiento: las dosis ya reveladas siguen visibles (I5). Devuelve
        /// false si el historial está vacío. Los cupos son de la UI.
        /// En <see cref="AF.Core.Rules.PourMode.Run"/> (por defecto, D-005) cada uso deshace el vertido entero, con todas las
        /// dosis que movió (04 §2.1); en <see cref="AF.Core.Rules.PourMode.Single"/> (pruebas), una sola dosis.
        /// </summary>
        public bool Undo()
        {
            int last = _pours.Count - 1;
            if (last < 0)
            {
                return false;
            }

            Current = _previous[last].WithRevealed(_revealed);
            _previous.RemoveAt(last);
            _pours.RemoveAt(last);
            Status = Evaluate(Current);
            return true;
        }

        /// <summary>
        /// Volver a la orilla (04 §2.7): vuelve al tablero inicial y vacía el historial (I4); conserva las capas reveladas
        /// (I5).
        /// </summary>
        public void Restart()
        {
            _previous.Clear();
            _pours.Clear();
            Current = Initial.WithRevealed(_revealed);
            Status = Evaluate(Current);
        }

        /// <summary>
        /// Luciérnaga (04 §2.5; 02 T6): revela todas las dosis ocultas de la cuia <paramref name="cuia"/> y devuelve cuántas
        /// reveló (0 si no tenía). No entra en el historial (Marea no deshace comodines, 04 §2.1) y lo revelado sigue visible
        /// tras Marea y tras Volver a la orilla (I5). Revelar no abre una Hoja ni un Brote (04 §2.5 c). Recalcula
        /// <see cref="Status"/>. Cupos, precio y modo de selección son de la UI.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice está fuera del tablero.</exception>
        public int Reveal(int cuia)
        {
            CheckIndex(cuia, nameof(cuia));
            int hidden = Current[cuia].HiddenMask;
            if (hidden == 0)
            {
                return 0;
            }

            _revealed[cuia] |= hidden;
            Current = Current.WithRevealed(_revealed);
            Status = Evaluate(Current);
            return PopCount(hidden);
        }

        /// <summary>
        /// Pororoca en un nivel turbio (04 §2.4 a; 02 T6): revela todas las dosis ocultas del nivel antes de barajar y
        /// devuelve cuántas reveló. Como <see cref="Reveal"/>, no entra en el historial y es permanente (I5). El barajado es
        /// de otra capa (y vacía el historial de Marea, 02 §14).
        /// </summary>
        public int RevealAll()
        {
            int total = 0;
            for (int i = 0; i < _revealed.Length; i++)
            {
                int hidden = Current[i].HiddenMask;
                _revealed[i] |= hidden;
                total += PopCount(hidden);
            }

            if (total > 0)
            {
                Current = Current.WithRevealed(_revealed);
                Status = Evaluate(Current);
            }

            return total;
        }

        /// <summary>
        /// Pororoca (04 §2.4; 02 §14 «Vacía el historial de Marea (nuevo tablero); conserva stock»): revela todas las dosis
        /// ocultas del nivel (<see cref="RevealAll"/>, 04 §2.4 a) y pone <paramref name="shuffled"/> como tablero actual, con el
        /// historial de Marea vacío. <see cref="Initial"/> no cambia: Volver a la orilla vuelve al tablero del nivel (misma
        /// semilla, 04 §2.7) con lo revelado visible (I5) [P]. Recalcula <see cref="Status"/>. El barajado, los topes y el cobro
        /// son de otras capas (AF.Solver y el cliente). False, sin cambiar nada, si la partida no está en
        /// <see cref="LevelStatus.Playing"/> (04 §2.4: no se usa en el bloqueo).
        /// </summary>
        /// <remarks>
        /// Tras una Pororoca, <see cref="Current"/> ya no sale de rehacer el historial desde <see cref="Initial"/>: quien guarde la
        /// partida (02 R8) anota el punto de la ola y la vuelve a dar al rehacer (el barajado es determinista).
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="shuffled"/> no es un barajado de este tablero: otro número de
        /// cuias, otra capacidad en alguna, otro <c>U_c</c> o dosis ocultas.</exception>
        public bool ApplyShuffle(BoardState shuffled)
        {
            if (shuffled == null)
            {
                throw new ArgumentNullException(nameof(shuffled));
            }

            BoardState current = Current;
            if (shuffled.Count != current.Count || shuffled.HasHidden)
            {
                throw new ArgumentException("La Pororoca conserva las cuias y deja todo revelado (04 §2.4).", nameof(shuffled));
            }

            for (int i = 0; i < current.Count; i++)
            {
                if (shuffled.CuiaAt(i).Capacity != current.CuiaAt(i).Capacity)
                {
                    throw new ArgumentException("La Pororoca conserva la capacidad de cada cuia (04 §2.4).", nameof(shuffled));
                }
            }

            for (int color = Cuia.MinColor; color <= Cuia.MaxColor; color++)
            {
                if (shuffled.UnitsTable[color] != current.UnitsTable[color])
                {
                    throw new ArgumentException("La Pororoca conserva los colores (04 §2.4: U_c no cambia).", nameof(shuffled));
                }
            }

            if (Status != LevelStatus.Playing)
            {
                return false;
            }

            RevealAll();
            _previous.Clear();
            _pours.Clear();
            Current = shuffled;
            Status = Evaluate(shuffled);
            return true;
        }

        /// <summary>
        /// Restaura capas ya reveladas de la cuia <paramref name="cuia"/> (02 R8: la máscara de revelado es parte del estado
        /// persistente; 02 T6): hace OR de <paramref name="layers"/> (bit k = la dosis oculta inicial <c>[cuia, k]</c>) en
        /// <see cref="RevealedLayers"/>, aplica la máscara a <see cref="Current"/>, recalcula <see cref="Status"/> como
        /// <see cref="Reveal"/> (R10 g-h) y devuelve cuántas capas nuevas reveló. Es idempotente: si todas ya lo estaban
        /// devuelve 0 y no cambia nada. Como <see cref="Reveal"/>, no entra en el historial (04 §2.1) y es permanente (I5):
        /// sigue visible tras Marea y tras Volver a la orilla.
        /// </summary>
        /// <remarks>
        /// <para>Sirve para recuperar las capas reveladas solo en parte (un vertido puede revelar una de las dos ocultas de una
        /// cuia, R7), que <see cref="Reveal"/> y <see cref="RevealAll"/> no expresan. Dos usos, ambos sobre una sesión nueva
        /// construida desde el tablero inicial con su historial rehecho:</para>
        /// <list type="bullet">
        /// <item><description>Reanudar (02 §14, «App cerrada o muerta a medio nivel»; R8): el cliente rehace los vertidos
        /// guardados y restaura la máscara guardada de cada cuia.</description></item>
        /// <item><description>Rehacer la sesión con la Cuia extra o la Cuia de rescate (04 §2.2 y §6): la cuia vacía se añade
        /// al final del tablero, así que los índices de las demás no cambian y la máscara de la sesión anterior
        /// (<see cref="RevealedLayers"/>) se copia cuia a cuia.</description></item>
        /// </list>
        /// <para>Las capas que el historial rehecho ya reveló (R7) cuentan como reveladas y no suman. En los dos usos esta
        /// llamada va al final del rehacer, como hace el cliente desde la integración del paso 1 (docs/dev/traspaso/README.md §6): el historial <c>h2</c> guarda
        /// por cuia solo la máscara revelada, sin el punto del historial en que se reveló cada capa, y esta clase tampoco lo
        /// expone. Es exacto aunque una capa se revelara a mitad del historial sin que el historial rehecho la revele (con la
        /// Luciérnaga o con un vertido que Marea deshizo después): ver una oculta solo cambia lo que pasa después si cambia la
        /// racha (R6) o el sellado (R5), y con T4 (02 §11; <see cref="LevelValidation"/>: bajo una dosis oculta nunca va el
        /// color de la de encima) eso solo ocurre si un lavado de Barro (02 §9.1 M5) deja encima de la oculta su mismo color.
        /// Ningún nivel de 1-200 lleva Barro en una cuia turbia. Si un pack futuro lo combinara, habría que guardar el punto
        /// del revelado o prohibir la combinación en <see cref="LevelValidation"/>.</para>
        /// <para>La Pororoca queda fuera de los dos usos: su revelado va justo antes de un barajado que vacía el historial y
        /// cambia el tablero (04 §2.1 y §2.4 a; 02 §14), así que tras una Pororoca la sesión ya no se rehace desde
        /// <see cref="Initial"/> más los vertidos.</para>
        /// </remarks>
        /// <param name="cuia">Índice de la cuia en el tablero.</param>
        /// <param name="layers">Máscara de capas (bit k = capa k desde el fondo); solo bits ocultos en <see cref="Initial"/>.</param>
        /// <returns>Número de capas que no estaban reveladas y ahora sí.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cuia"/> está fuera del tablero.</exception>
        /// <exception cref="ArgumentException"><paramref name="layers"/> es negativa o tiene algún bit que no es una dosis
        /// oculta de la cuia en <see cref="Initial"/>.</exception>
        public int RestoreRevealed(int cuia, int layers)
        {
            CheckIndex(cuia, nameof(cuia));
            if (layers < 0 || (layers & ~Initial.CuiaAt(cuia).HiddenMask) != 0)
            {
                throw new ArgumentException("Las capas a restaurar tienen que ser dosis ocultas de la cuia en el tablero inicial (02 T6).", nameof(layers));
            }

            int added = layers & ~_revealed[cuia];
            if (added == 0)
            {
                return 0;
            }

            _revealed[cuia] |= added;
            Current = Current.WithRevealed(_revealed);
            Status = Evaluate(Current);
            return PopCount(added);
        }

        /// <summary>
        /// Anota en la máscara de revelado lo que el vertido reveló (R7): una dosis oculta solo deja de estarlo al quedar
        /// arriba, en la misma posición <c>[cuia, capa]</c> (I5).
        /// </summary>
        private void RecordReveals(BoardState before, BoardState after)
        {
            for (int i = 0; i < _revealed.Length; i++)
            {
                _revealed[i] |= before.CuiaAt(i).HiddenMask & ~after.CuiaAt(i).HiddenMask;
            }
        }

        private static bool HasRemolino(BoardState state)
        {
            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].Mechanic == AF.Core.Mechanics.MechanicType.Remolino)
                {
                    return true;
                }
            }

            return false;
        }

        private static int PopCount(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                count++;
            }

            return count;
        }

        private LevelStatus Evaluate(BoardState state)
        {
            // R10 (g) victoria y (h) bloqueo, en ese orden: R13 (sin vertidos útiles) y, si aún hay alguno, R13 ampliada
            // (D-014): el cierre acotado de estados alcanzables no tiene victoria.
            LevelStatus status = BoardRules.EvaluateStatus(state.CuiasArray, 0, state.Count, state.UnitsTable, FutileRuleEnabled);
            if (status == LevelStatus.Playing && _deadEnd.IsDeadEnd(state.CuiasArray, 0, state.Count, state.UnitsTable, PourMode))
            {
                return LevelStatus.Stuck;
            }

            return status;
        }

        private void CheckIndex(int index, string paramName)
        {
            if ((uint)index >= (uint)Current.Count)
            {
                throw new ArgumentOutOfRangeException(paramName, index, "Índice de cuia fuera del tablero.");
            }
        }
    }
}
