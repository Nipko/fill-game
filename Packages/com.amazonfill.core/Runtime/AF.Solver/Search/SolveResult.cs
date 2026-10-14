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

        internal SolveResult(SolveStatus status, Pour[]? moves, int expanded, int storedStates, bool wasCancelled)
        {
            Status = status;
            _moves = moves;
            Expanded = expanded;
            StoredStates = storedStates;
            WasCancelled = wasCancelled;
        }

        /// <summary>Resuelto, irresoluble o presupuesto agotado (también cuando se cortó: <see cref="WasCancelled"/>).</summary>
        public SolveStatus Status { get; }

        /// <summary><see cref="Status"/> es <see cref="SolveStatus.Solved"/>.</summary>
        public bool IsSolved => Status == SolveStatus.Solved;

        /// <summary>
        /// La búsqueda se cortó porque otro hilo marcó <see cref="SolverOptions.Cancellation"/> (<c>hint_timeout_ms</c>, 08 §9;
        /// K2): entonces <see cref="Status"/> es <see cref="SolveStatus.BudgetExceeded"/> («No encuentro un camino desde aquí»,
        /// sin cobrar; 02 T5, respuesta 4) y no hay solución. False en cualquier otro caso, también si la bandera se marcó
        /// cuando la búsqueda ya había decidido.
        /// </summary>
        public bool WasCancelled { get; }

        /// <summary>
        /// Estados expandidos (en <see cref="SolveStatus.BudgetExceeded"/> por presupuesto, el presupuesto + 1; si se cortó, los
        /// que llegó a expandir, un múltiplo de <see cref="SolverCancellation.PollInterval"/>).
        /// </summary>
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
