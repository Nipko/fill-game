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
    /// <see cref="AppliedPours"/>.</description></item>
    /// <item><description>I4: <see cref="Restart"/> deja <c>Current.Equals(Initial)</c> y el historial vacío.</description></item>
    /// </list>
    /// <para>Modo de vertido (<see cref="AF.Core.Rules.PourMode"/>; por defecto <see cref="AF.Core.Rules.PourMode.Run"/>,
    /// D-005): se fija al crear la partida y no cambia a mitad de nivel, porque el historial de Marea y el par dependen de
    /// él (punto 3 de D-001, sustituida por D-005, que lo conserva). En <see cref="AF.Core.Rules.PourMode.Run"/> cada
    /// vertido mueve la racha que cabe: <see cref="AppliedPours"/> cuenta vertidos y <see cref="Undo"/> deshace el vertido
    /// entero (02 §1.2; 04 §2.1). En <see cref="AF.Core.Rules.PourMode.Single"/> (solo pruebas) cada vertido mueve una
    /// dosis, así que <see cref="AppliedPours"/> cuenta dosis movidas y <see cref="Undo"/> deshace una dosis por uso.
    /// Victoria y bloqueo se evalúan igual en los dos modos.</para>
    /// <para>Rechaza tableros con dosis ocultas: Marea y Volver a la orilla deberían conservar lo revelado (R7,
    /// 04 §2.1, §2.7) y esa máscara de revelado aún no existe. Tampoco hay mecánicas (02 §9).</para>
    /// <para>No es seguro entre hilos.</para>
    /// </remarks>
    public sealed class LevelSession
    {
        private readonly List<BoardState> _previous = new List<BoardState>();
        private readonly List<Pour> _pours = new List<Pour>();

        /// <summary>Empieza una partida en <paramref name="initial"/> con el modo de vertido <paramref name="pourMode"/>.</summary>
        /// <param name="initial">Tablero inicial del nivel.</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> (08 §9; kill-switch de R16).</param>
        /// <param name="pourMode">Modo de vertido del nivel (<c>rules_pour_mode</c>, por defecto
        /// <see cref="PourModes.Default"/> = <see cref="AF.Core.Rules.PourMode.Run"/>, D-005;
        /// <see cref="AF.Core.Rules.PourMode.Single"/> solo para pruebas).</param>
        /// <exception cref="NotSupportedException">El tablero tiene dosis ocultas (Aguas turbias).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pourMode"/> no es un valor definido.</exception>
        public LevelSession(BoardState initial, bool futileRuleEnabled, PourMode pourMode)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            PourModes.Check(pourMode, nameof(pourMode));
            if (initial.HasHidden)
            {
                throw new NotSupportedException("LevelSession aún no admite Aguas turbias: Marea y Volver a la orilla necesitan la máscara de revelado (02 R7, 04 §2.1).");
            }

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
        /// <exception cref="NotSupportedException">El tablero tiene dosis ocultas (Aguas turbias).</exception>
        public LevelSession(BoardState initial, bool futileRuleEnabled)
            : this(initial, futileRuleEnabled, PourMode.Run)
        {
        }

        /// <summary>Tablero inicial del nivel (misma semilla en cada reinicio, 04 §2.7).</summary>
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

            BoardState next = Current.WithPour(source, target, amount);
            var pour = new Pour(source, target, amount);
            _previous.Add(Current);
            _pours.Add(pour);
            Current = next;
            Status = Evaluate(next);
            result = new PourResult(pour, next.IsSealed(target), Status);
            return true;
        }

        /// <summary>
        /// Marea (04 §2.1): deshace el último vertido y restaura el estado anterior exacto, también desde
        /// Aguas estancadas o desde la victoria. Devuelve false si el historial está vacío. Los cupos son de la UI.
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

            Current = _previous[last];
            _previous.RemoveAt(last);
            _pours.RemoveAt(last);
            Status = Evaluate(Current);
            return true;
        }

        /// <summary>Volver a la orilla (04 §2.7): vuelve al tablero inicial y vacía el historial (I4).</summary>
        public void Restart()
        {
            _previous.Clear();
            _pours.Clear();
            Current = Initial;
            Status = Evaluate(Initial);
        }

        private LevelStatus Evaluate(BoardState state)
        {
            if (state.IsWon())
            {
                return LevelStatus.Won;
            }

            return PourRules.HasUsefulMove(state, FutileRuleEnabled) ? LevelStatus.Playing : LevelStatus.Stuck;
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
