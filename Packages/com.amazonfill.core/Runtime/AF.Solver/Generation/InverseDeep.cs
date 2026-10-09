#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Collections;

namespace AF.Solver.Generation
{
    /// <summary>
    /// Barajado inverso en profundidad (<c>DEEP</c> de GDD 03 §4.2): el paseo inverso se explora como DFS con retroceso
    /// hasta <c>max_nodes</c> estados (20.000) y se devuelve el estado válido más profundo. Es la fuente de reto e hito con 1
    /// vacío para K ≥ 9 y conserva la solución por construcción.
    /// </summary>
    /// <remarks>
    /// <para>Cada nivel de la pila baraja (Fisher-Yates con el PRNG del núcleo) sus movimientos inversos no visitados y los
    /// toma de uno en uno; un estado visitado entre tanto por otra rama se salta sin contar como nodo [P: el prototipo lo
    /// volvía a contar].</para>
    /// <para><b>Semillas:</b> el intento <c>k</c> (1..5) usa <c>Prng.SubSeed(semilla, 0xDEE90000 + k)</c>.</para>
    /// <para>Pila, candidatos y visitados reutilizan sus arrays (solo crecen); el bucle no asigna. No es seguro entre hilos.</para>
    /// </remarks>
    public sealed class InverseDeep
    {
        /// <summary>Estados por intento (03 §4.2).</summary>
        public const int DefaultMaxNodes = 20000;

        /// <summary>Intentos (sub-semillas) por nivel, como el prototipo.</summary>
        public const int DefaultAttempts = 5;

        private const ulong SeedStream = 0xDEE90000UL;

        private readonly Prng _rng = new Prng(0UL);
        private readonly Cuia[] _child = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _canon = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _best = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _pair = new Cuia[2];
        private readonly InverseMove[] _moves = new InverseMove[InverseMoves.MaxMoves];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly StateSet _visited = new StateSet(DefaultMaxNodes);
        private Cuia[] _levelStates = new Cuia[64 * BoardState.MaxCuias];
        private int[] _levelStart = new int[64];
        private int[] _levelEnd = new int[64];
        private bool[] _levelReady = new bool[64];
        private InverseMove[] _stack = new InverseMove[4096];

        /// <summary>Genera un tablero inicial para <paramref name="spec"/> con la semilla <paramref name="seed"/>.</summary>
        public InverseResult Generate(InverseSpec spec, ulong seed, int maxNodes = DefaultMaxNodes, int attempts = DefaultAttempts)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            BoardState solved = spec.Solved;
            int width = solved.Count;
            InverseSupport.LoadUnits(solved, _units);
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                _rng.Reseed(Prng.SubSeed(seed, SeedStream + (ulong)attempt));
                _visited.Reset(width);
                for (int i = 0; i < width; i++)
                {
                    _levelStates[i] = solved[i];
                }

                InverseSupport.AddCanonical(_visited, _levelStates, width, _canon);
                _levelReady[0] = false;
                _levelStart[0] = 0;
                _levelEnd[0] = 0;
                int top = 0;
                int nodes = 0;
                int bestDepth = 0;
                int bestComplexity = 0;
                while (top >= 0 && nodes < maxNodes)
                {
                    if (!_levelReady[top])
                    {
                        Candidates(top, width, spec.Mode);
                    }

                    if (_levelEnd[top] == _levelStart[top])
                    {
                        top--;
                        continue;
                    }

                    InverseMove move = _stack[--_levelEnd[top]];
                    Array.Copy(_levelStates, top * width, _child, 0, width);
                    InverseMoves.Apply(_child, 0, move);
                    Array.Copy(_child, _canon, width);
                    BoardCanon.Sort(_canon, 0, width);
                    if (!_visited.Add(_canon, 0, BoardCanon.Hash(_canon, 0, width)))
                    {
                        continue; // visitado por otra rama después de barajar
                    }

                    nodes++;
                    int depth = top + 1;
                    if (depth > bestDepth && InverseSupport.IsValidStart(_child, width, _units, spec, out int complexity))
                    {
                        Array.Copy(_child, _best, width);
                        bestDepth = depth;
                        bestComplexity = complexity;
                    }

                    Push(depth);
                    Array.Copy(_child, 0, _levelStates, depth * width, width);
                    _levelReady[depth] = false;
                    _levelStart[depth] = _levelEnd[top];
                    _levelEnd[depth] = _levelStart[depth];
                    top = depth;
                }

                if (bestDepth > 0)
                {
                    return new InverseResult(InverseSupport.ToBoard(_best, width), attempt, bestDepth, bestComplexity, nodes);
                }
            }

            return new InverseResult(null, attempts, 0, 0, 0);
        }

        /// <summary>Movimientos inversos no visitados del nivel <paramref name="level"/>, barajados (Fisher-Yates).</summary>
        private void Candidates(int level, int width, PourMode mode)
        {
            int count = InverseMoves.Collect(_levelStates, level * width, width, _units, mode, _moves, _pair);
            int start = _levelStart[level];
            EnsureStack(start + count);
            int end = start;
            for (int k = 0; k < count; k++)
            {
                Array.Copy(_levelStates, level * width, _canon, 0, width);
                InverseMoves.Apply(_canon, 0, _moves[k]);
                BoardCanon.Sort(_canon, 0, width);
                if (!_visited.Contains(_canon, 0, BoardCanon.Hash(_canon, 0, width)))
                {
                    _stack[end++] = _moves[k];
                }
            }

            for (int i = end - 1; i > start; i--)
            {
                int j = start + _rng.NextInt(i - start + 1);
                InverseMove tmp = _stack[i];
                _stack[i] = _stack[j];
                _stack[j] = tmp;
            }

            _levelEnd[level] = end;
            _levelReady[level] = true;
        }

        private void Push(int depth)
        {
            if (depth < _levelStart.Length)
            {
                return;
            }

            int size = _levelStart.Length * 2;
            Array.Resize(ref _levelStart, size);
            Array.Resize(ref _levelEnd, size);
            Array.Resize(ref _levelReady, size);
            var states = new Cuia[size * BoardState.MaxCuias];
            Array.Copy(_levelStates, states, _levelStates.Length);
            _levelStates = states;
        }

        private void EnsureStack(int needed)
        {
            if (needed > _stack.Length)
            {
                var bigger = new InverseMove[Math.Max(needed, _stack.Length * 2)];
                Array.Copy(_stack, bigger, _stack.Length);
                _stack = bigger;
            }
        }
    }
}
