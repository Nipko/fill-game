#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AF.Solver.Murky
{
    /// <summary>Resultado de <see cref="SafePath.Find"/>.</summary>
    public enum SafePathStatus
    {
        /// <summary>Hay ruta segura hasta la victoria (03 §8: OK).</summary>
        Ok = 0,

        /// <summary>El tablero inicial no cumple T4 (02 §11; 03 §8: RECHAZAR).</summary>
        RejectedT4 = 1,

        /// <summary>Algún estado de la ruta tiene más realidades que el máximo (24; 02 T2): el nivel se rechaza.</summary>
        TooManyRealities = 2,

        /// <summary>Lo visible no cuadra con <c>units_per_color</c>.</summary>
        Inconsistent = 3,

        /// <summary>Algún estado de la ruta no tiene ningún vertido seguro (03 §8: FALLO, nivel rechazado).</summary>
        NoSafeMove = 4,

        /// <summary>400 pasos sin llegar a la victoria (03 §8).</summary>
        StepLimit = 5,
    }

    /// <summary>
    /// <c>RUTA_SEGURA</c> de 03 §8 (02 §11 <c>validar_turbio</c>, T5): desde el tablero real del nivel con sus dosis ocultas,
    /// en cada paso (hasta 400) enumera las realidades aún posibles (lo revelado reduce el conjunto), calcula los vertidos
    /// seguros (<see cref="SafeMoves"/>) y aplica el de menor par en la realidad verdadera (a igualdad, el primero en el orden
    /// de <see cref="SolverMoves"/>). El generador garantiza así <c>seguros(S) ≠ ∅</c> en S0 y en cada estado de la ruta;
    /// <c>seguros(S0)</c> es la primera pista y la ruta se guarda solo para QA (<c>safe_path</c>, 08 §4.1).
    /// </summary>
    /// <remarks>
    /// Las realidades se enumeran con T4 respecto al tablero inicial (<see cref="RealityEnumerator"/>). Con
    /// <c>enforceT4</c> = false no se exige T4 ni al tablero ni a las realidades: es el criterio del prototipo de 03 §18.3,
    /// anterior a T4 (paridad). No es para el bucle caliente del cliente (asigna un tablero por paso); el cálculo por
    /// realidad sí lo es (<see cref="SafeMoves"/>). No es segura entre hilos.
    /// </remarks>
    public sealed class SafePath
    {
        /// <summary>Pasos máximos de la ruta (03 §8: «repetir hasta 400 veces»).</summary>
        public const int MaxSteps = 400;

        private readonly SafeMoves _safe = new SafeMoves();
        private readonly Pour[] _path = new Pour[MaxSteps];

        /// <summary>Los vertidos seguros que usa la ruta (memo compartido entre pasos y niveles con las mismas opciones).</summary>
        public SafeMoves Moves => _safe;

        /// <summary>
        /// Busca la ruta segura desde <paramref name="start"/> (el tablero inicial del nivel, con los colores reales en las
        /// posiciones ocultas).
        /// </summary>
        /// <param name="start">Tablero inicial con sus dosis ocultas.</param>
        /// <param name="options">Opciones por realidad (<see cref="SafeMoves.ForPipeline(PourMode)"/>: 20.000 nodos, w = 1; en
        /// un nivel con Tinaja, sin poda de fútiles: <see cref="SafeMoves.ForPipeline(PourMode, BoardState)"/>, b.2 R14).</param>
        /// <param name="enforceT4">Exigir T4 (true en campaña; false solo para la paridad con el prototipo).</param>
        /// <param name="maxRealities">Máximo de realidades por estado (24 por defecto, 02 T2).</param>
        public SafePathResult Find(BoardState start, SolverOptions options, bool enforceT4 = true, int maxRealities = RealityEnumerator.DefaultMaxRealities)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (enforceT4 && !RealityEnumerator.SatisfiesT4(start))
            {
                return new SafePathResult(SafePathStatus.RejectedT4, Array.Empty<Pour>(), 0, 0);
            }

            BoardState state = start;
            int steps = 0;
            int maxSeen = 0;
            int firstSafe = 0;
            while (true)
            {
                if (state.IsWon())
                {
                    return Result(SafePathStatus.Ok, steps, maxSeen, firstSafe);
                }

                if (steps == MaxSteps)
                {
                    return Result(SafePathStatus.StepLimit, steps, maxSeen, firstSafe);
                }

                RealityStatus realities = _safe.Find(state, options, start, enforceT4, maxRealities);
                if (realities == RealityStatus.TooMany)
                {
                    return Result(SafePathStatus.TooManyRealities, steps, maxRealities + 1, firstSafe);
                }

                if (realities == RealityStatus.Inconsistent)
                {
                    return Result(SafePathStatus.Inconsistent, steps, maxSeen, firstSafe);
                }

                maxSeen = Math.Max(maxSeen, _safe.RealityCount);
                if (steps == 0)
                {
                    firstSafe = _safe.Count;
                }

                int best = -1;
                int bestPar = int.MaxValue;
                for (int k = 0; k < _safe.Count; k++)
                {
                    int par = _safe.TrueParAfter(k);
                    if (par >= 0 && par < bestPar)
                    {
                        best = k;
                        bestPar = par;
                    }
                }

                if (best < 0)
                {
                    return Result(SafePathStatus.NoSafeMove, steps, maxSeen, firstSafe, _safe.Count > 0 ? SafeMovesOutcome.Found : _safe.Outcome);
                }

                Pour pour = _safe.GetMove(best);
                _path[steps++] = pour;
                state = PourRules.Apply(state, pour.Source, pour.Target, options.PourMode);
            }
        }

        private SafePathResult Result(SafePathStatus status, int steps, int maxRealities, int firstSafe, SafeMovesOutcome outcome = SafeMovesOutcome.Found)
        {
            var moves = new Pour[steps];
            Array.Copy(_path, moves, steps);
            return new SafePathResult(status, moves, maxRealities, firstSafe, outcome);
        }
    }

    /// <summary>Resultado de <see cref="SafePath.Find"/>: estado, ruta y medidas para el informe (03 §8).</summary>
    public readonly struct SafePathResult
    {
        private readonly Pour[] _moves;

        internal SafePathResult(SafePathStatus status, Pour[] moves, int maxRealities, int firstSafeMoves, SafeMovesOutcome noSafeMoveOutcome = SafeMovesOutcome.Found)
        {
            Status = status;
            _moves = moves;
            MaxRealities = maxRealities;
            FirstSafeMoves = firstSafeMoves;
            NoSafeMoveOutcome = noSafeMoveOutcome;
        }

        /// <summary>
        /// Con <see cref="SafePathStatus.NoSafeMove"/>, por qué no hubo vertido seguro en el último estado de la ruta (propuesta
        /// de la tinaja, c.2 y c.3: el informe separa «sin vertido seguro» de «presupuesto agotado»):
        /// <see cref="SafeMovesOutcome.NoExit"/>, <see cref="SafeMovesOutcome.SomeRealitySolvable"/> o
        /// <see cref="SafeMovesOutcome.BudgetExceeded"/>. En los demás estados, <see cref="SafeMovesOutcome.Found"/>.
        /// </summary>
        public SafeMovesOutcome NoSafeMoveOutcome { get; }

        /// <summary>Ruta segura encontrada o motivo del rechazo.</summary>
        public SafePathStatus Status { get; }

        /// <summary><see cref="Status"/> es <see cref="SafePathStatus.Ok"/>.</summary>
        public bool IsOk => Status == SafePathStatus.Ok;

        /// <summary>Vertidos de la ruta (los aplicados hasta el final o hasta el fallo).</summary>
        public int MoveCount => _moves == null ? 0 : _moves.Length;

        /// <summary>Máximo de realidades a lo largo de la ruta (03 §18.3: 12 en el lote de prueba).</summary>
        public int MaxRealities { get; }

        /// <summary>Vertidos seguros en S0: la primera pista (02 §11, <c>validar_turbio</c>).</summary>
        public int FirstSafeMoves { get; }

        /// <summary>Vertido <paramref name="index"/> de la ruta.</summary>
        public Pour GetMove(int index)
        {
            if (_moves == null || (uint)index >= (uint)_moves.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera de la ruta.");
            }

            return _moves[index];
        }
    }
}
