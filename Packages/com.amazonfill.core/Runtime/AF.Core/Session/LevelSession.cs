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
    /// después bloqueo (R13).</description></item>
    /// <item><description>I2: un vertido ilegal (R11) o con <see cref="Status"/> distinto de
    /// <see cref="LevelStatus.Playing"/> devuelve false y no cambia nada (con Aguas estancadas el input está
    /// bloqueado desde el instante lógico del bloqueo, R15).</description></item>
    /// <item><description>I3: <see cref="Undo"/> restaura el estado anterior exacto, reabre los sellos y resta 1 a
    /// <see cref="AppliedPours"/>; con Aguas turbias, con las dosis ya reveladas visibles (I5).</description></item>
    /// <item><description>I4: <see cref="Restart"/> deja <c>Current.Equals(Initial)</c> y el historial vacío; con Aguas
    /// turbias, <see cref="Initial"/> con las dosis ya reveladas visibles (I5).</description></item>
    /// <item><description>I5 (02 R7, T6; 04 §2.1, §2.5, §2.7): una dosis oculta revelada (al quedar arriba, con
    /// <see cref="Reveal"/> o con <see cref="RevealAll"/>) no vuelve a ocultarse en todo el nivel, tampoco tras Marea ni tras
    /// Volver a la orilla: «lo único que no se revierte es el conocimiento».</description></item>
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

        /// <summary>Si se excluyen los vertidos fútiles al detectar el bloqueo (R13, R16).</summary>
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
        /// (02 R8).
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
            // R10 (g) victoria y (h) bloqueo, en ese orden.
            return BoardRules.EvaluateStatus(state.CuiasArray, 0, state.Count, state.UnitsTable, FutileRuleEnabled);
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
