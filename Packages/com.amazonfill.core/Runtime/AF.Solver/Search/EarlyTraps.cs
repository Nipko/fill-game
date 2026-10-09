#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;

namespace AF.Solver.Search
{
    /// <summary>
    /// Trampas tempranas (respuesta Q16 B de D-007; propuesta de Magic Sort, pregunta 5.5 B; propuesta de D-007 c.7): las
    /// jugadas iniciales que llevan a Aguas estancadas en 2 vertidos o menos. <c>tools/level-qa</c> las compara con el tope de
    /// la etiqueta (0 en Normal, 1 en Difícil, sin tope en Muy difícil).
    /// </summary>
    /// <remarks>
    /// <para><b>Definición operativa [P]:</b> se cuentan los primeros vertidos útiles distintos desde S0 (los
    /// <c>MOVIMIENTOS</c> de 03 §5 sobre el tablero en orden canónico: dos vertidos a vacíos equivalentes son la misma
    /// jugada) tras los que el tablero queda atascado en ese mismo vertido, o en el siguiente con <b>alguna</b> respuesta útil
    /// (<see cref="Count"/>). Es la lectura de «se bloquean en ≤ 2 vertidos con alguna jugada inicial» y la del camino más
    /// corto a Aguas estancadas que ya mide <c>tools/level-generator</c> en el tutorial (1 vertido en el nivel 6, 2 en el
    /// 8). <see cref="CountUnavoidable"/> da la lectura estricta (todas las respuestas atascan), como información.</para>
    /// <para>Se mide en el modo de vertido vigente (el número de vertidos cambia con el modo). Reutiliza sus búferes; no es
    /// segura entre hilos.</para>
    /// </remarks>
    public sealed class EarlyTraps
    {
        private readonly Cuia[] _start = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _first = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _second = new Cuia[BoardState.MaxCuias];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly SolverMove[] _firstMoves = new SolverMove[SolverMoves.MaxMoves];
        private readonly SolverMove[] _secondMoves = new SolverMove[SolverMoves.MaxMoves];
        private int _width;

        /// <summary>
        /// Jugadas iniciales tras las que el tablero queda atascado en ese vertido o puede quedarlo en el siguiente.
        /// </summary>
        public int Count(BoardState state, PourMode mode, bool futileRuleEnabled = true)
        {
            return CountTraps(state, mode, futileRuleEnabled, false);
        }

        /// <summary>
        /// Lectura estricta: jugadas iniciales tras las que el tablero queda atascado en ese vertido o en el siguiente con
        /// cualquier respuesta.
        /// </summary>
        public int CountUnavoidable(BoardState state, PourMode mode, bool futileRuleEnabled = true)
        {
            return CountTraps(state, mode, futileRuleEnabled, true);
        }

        private int CountTraps(BoardState state, PourMode mode, bool futileRuleEnabled, bool unavoidable)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Load(state);
            int width = _width;
            int first = SolverMoves.Collect(_start, 0, width, _units, mode, futileRuleEnabled, _firstMoves);
            int traps = 0;
            for (int m = 0; m < first; m++)
            {
                Apply(_start, _first, _firstMoves[m], mode);
                LevelStatus afterFirst = BoardRules.EvaluateStatus(_first, 0, width, _units, futileRuleEnabled);
                if (afterFirst == LevelStatus.Stuck)
                {
                    traps++;
                    continue;
                }

                if (afterFirst == LevelStatus.Won)
                {
                    continue;
                }

                BoardCanon.Sort(_first, 0, width);
                int second = SolverMoves.Collect(_first, 0, width, _units, mode, futileRuleEnabled, _secondMoves);
                int stuckReplies = 0;
                for (int r = 0; r < second; r++)
                {
                    Apply(_first, _second, _secondMoves[r], mode);
                    if (BoardRules.IsStuck(_second, 0, width, _units, futileRuleEnabled))
                    {
                        stuckReplies++;
                        if (!unavoidable)
                        {
                            break;
                        }
                    }
                }

                bool trap = unavoidable ? stuckReplies == second : stuckReplies > 0;
                if (trap)
                {
                    traps++;
                }
            }

            return traps;
        }

        private void Load(BoardState state)
        {
            _width = state.Count;
            for (int i = 0; i < _width; i++)
            {
                _start[i] = state[i];
            }

            BoardCanon.Sort(_start, 0, _width);
            _units[0] = 0;
            for (int color = Cuia.MinColor; color <= Cuia.MaxColor; color++)
            {
                _units[color] = state.UnitsOf(color);
            }
        }

        private void Apply(Cuia[] from, Cuia[] to, SolverMove move, PourMode mode)
        {
            Array.Copy(from, to, _width);
            var events = default(PourEvents);
            BoardRules.ApplyInPlace(to, 0, _width, _units, move.Source, move.Target, mode, move.IsFutile, ref events);
        }
    }
}
