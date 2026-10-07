#nullable enable
using System;
using AF.Core.Rules;

namespace AF.Solver.Search
{
    /// <summary>
    /// Resultado de <see cref="AStarSolver.Solve"/>: estado, solución sobre los índices reales del tablero de
    /// partida (no los canónicos) y contadores de la búsqueda.
    /// </summary>
    public readonly struct SolveResult
    {
        private readonly Pour[]? _moves;

        internal SolveResult(SolveStatus status, Pour[]? moves, int expanded, int storedStates)
        {
            Status = status;
            _moves = moves;
            Expanded = expanded;
            StoredStates = storedStates;
        }

        /// <summary>Resuelto, irresoluble o presupuesto agotado.</summary>
        public SolveStatus Status { get; }

        /// <summary><see cref="Status"/> es <see cref="SolveStatus.Solved"/>.</summary>
        public bool IsSolved => Status == SolveStatus.Solved;

        /// <summary>Estados expandidos (en <see cref="SolveStatus.BudgetExceeded"/>, el presupuesto + 1).</summary>
        public int Expanded { get; }

        /// <summary>Estados canónicos distintos que la búsqueda llegó a guardar.</summary>
        public int StoredStates { get; }

        /// <summary>
        /// Vertidos de la solución (0 si no está resuelto o si el tablero ya era victoria). Con peso 1 es el par (03 §5).
        /// </summary>
        public int MoveCount => _moves == null ? 0 : _moves.Length;

        /// <summary>
        /// Vertido <paramref name="index"/> de la solución, aplicable en orden con
        /// <see cref="PourRules.Apply(AF.Core.Board.BoardState, int, int, PourMode)"/> en el modo de vertido de la búsqueda
        /// (<see cref="SolverOptions.PourMode"/>); en <see cref="PourMode.Single"/> su <see cref="Pour.Amount"/> es siempre 1.
        /// </summary>
        public Pour GetMove(int index)
        {
            if (_moves == null || (uint)index >= (uint)_moves.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera de la solución.");
            }

            return _moves[index];
        }

        /// <summary>Copia de la solución (array vacío si no hay).</summary>
        public Pour[] CopyMoves()
        {
            if (_moves == null || _moves.Length == 0)
            {
                return Array.Empty<Pour>();
            }

            return (Pour[])_moves.Clone();
        }
    }
}
