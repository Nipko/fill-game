#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Collections;

namespace AF.Solver.Generation
{
    /// <summary>
    /// Barajado inverso con «mejor prefijo» (<c>WALK</c> de GDD 03 §4.1): desde el tablero resuelto se dan vertidos
    /// inversos al azar entre los 3 mejores candidatos no visitados y se devuelve el estado válido más profundo
    /// (ninguna cuia sellada, no ganado, <c>E</c> vacíos y complejidad ≥ <c>compl_min</c>). La profundidad es un tope, no un
    /// objetivo: el paseo muere cuando no quedan candidatos (03 §1, hecho 3). Tiene solución por construcción.
    /// </summary>
    /// <remarks>
    /// <para><b>Puntuación en enteros</b> [P]: el «rng()·2 − 4·[vacíos(S′) &lt; E]» de 03 §4.1 pasa a
    /// <c>NextInt(2·65.536) − 4·65.536·[vacíos &lt; E]</c>. A igual puntuación manda el orden de los candidatos (a, m, b),
    /// como la ordenación estable del prototipo.</para>
    /// <para><b>Semillas:</b> el intento <c>k</c> (1..200) usa <c>Prng.SubSeed(semilla, k)</c>.</para>
    /// <para>El tablero devuelto conserva el orden de las cuias del paseo (el de <see cref="InverseSpec.Solved"/>). Los búferes
    /// se reutilizan: un paso no asigna. No es seguro entre hilos.</para>
    /// </remarks>
    public sealed class InverseWalk
    {
        /// <summary>Sub-semillas por nivel (03 §4.1: hasta 200 intentos).</summary>
        public const int DefaultAttempts = 200;

        private const int ScoreScale = 65536;

        private readonly Prng _rng = new Prng(0UL);
        private readonly Cuia[] _state = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _child = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _best = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _pair = new Cuia[2];
        private readonly InverseMove[] _moves = new InverseMove[InverseMoves.MaxMoves];
        private readonly int[] _candidates = new int[InverseMoves.MaxMoves];
        private readonly int[] _scores = new int[InverseMoves.MaxMoves];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly StateSet _visited = new StateSet(128);

        /// <summary>Genera un tablero inicial para <paramref name="spec"/> con la semilla <paramref name="seed"/>.</summary>
        public InverseResult Generate(InverseSpec spec, ulong seed, int attempts = DefaultAttempts)
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
                _rng.Reseed(Prng.SubSeed(seed, (ulong)attempt));
                InverseSupport.CopyFrom(solved, _state);
                _visited.Reset(width);
                InverseSupport.AddCanonical(_visited, _state, width, _child);
                int bestDepth = 0;
                int bestComplexity = 0;
                int steps = 0;
                for (int step = 0; step < spec.MaxDepth; step++)
                {
                    int count = InverseMoves.Collect(_state, 0, width, _units, spec.Mode, _moves, _pair);
                    int candidates = 0;
                    for (int k = 0; k < count; k++)
                    {
                        Array.Copy(_state, _child, width);
                        InverseMoves.Apply(_child, 0, _moves[k]);
                        int empties = InverseSupport.Empties(_child, width);
                        BoardCanon.Sort(_child, 0, width);
                        if (_visited.Contains(_child, 0, BoardCanon.Hash(_child, 0, width)))
                        {
                            continue;
                        }

                        _candidates[candidates] = k;
                        _scores[candidates] = _rng.NextInt(2 * ScoreScale) - (empties < spec.Empties ? 4 * ScoreScale : 0);
                        candidates++;
                    }

                    if (candidates == 0)
                    {
                        break; // el paseo muere: solo se crean cortes, nunca se deshacen (03 §1, hecho 3)
                    }

                    int rank = _rng.NextInt(candidates < 3 ? candidates : 3);
                    InverseMoves.Apply(_state, 0, _moves[_candidates[Ranked(candidates, rank)]]);
                    InverseSupport.AddCanonical(_visited, _state, width, _child);
                    steps = step + 1;
                    if (InverseSupport.IsValidStart(_state, width, _units, spec, out int complexity))
                    {
                        Array.Copy(_state, _best, width);
                        bestDepth = steps;
                        bestComplexity = complexity;
                    }
                }

                if (bestDepth > 0)
                {
                    return new InverseResult(InverseSupport.ToBoard(_best, width), attempt, bestDepth, bestComplexity, steps);
                }
            }

            return new InverseResult(null, attempts, 0, 0, 0);
        }

        /// <summary>Índice del candidato en la posición <paramref name="rank"/> (0-2) por puntuación descendente y orden estable.</summary>
        private int Ranked(int count, int rank)
        {
            int first = -1;
            int second = -1;
            int third = -1;
            for (int c = 0; c < count; c++)
            {
                int score = _scores[c];
                if (first < 0 || score > _scores[first])
                {
                    third = second;
                    second = first;
                    first = c;
                }
                else if (second < 0 || score > _scores[second])
                {
                    third = second;
                    second = c;
                }
                else if (third < 0 || score > _scores[third])
                {
                    third = c;
                }
            }

            return rank == 0 ? first : rank == 1 ? second : third;
        }
    }

    /// <summary>Utilidades comunes de los generadores inversos.</summary>
    internal static class InverseSupport
    {
        public static void LoadUnits(BoardState state, int[] units)
        {
            units[0] = 0;
            for (int color = Cuia.MinColor; color <= Cuia.MaxColor; color++)
            {
                units[color] = state.UnitsOf(color);
            }
        }

        public static void CopyFrom(BoardState state, Cuia[] target)
        {
            for (int i = 0; i < state.Count; i++)
            {
                target[i] = state[i];
            }
        }

        public static void AddCanonical(StateSet visited, Cuia[] state, int width, Cuia[] scratch)
        {
            Array.Copy(state, scratch, width);
            BoardCanon.Sort(scratch, 0, width);
            visited.Add(scratch, 0, BoardCanon.Hash(scratch, 0, width));
        }

        public static int Empties(Cuia[] cuias, int width)
        {
            int empties = 0;
            for (int i = 0; i < width; i++)
            {
                empties += cuias[i].IsEmpty ? 1 : 0;
            }

            return empties;
        }

        /// <summary>Filtro de 03 §4.1: ninguna cuia sellada (y por tanto no ganado), E vacíos y complejidad ≥ compl_min.</summary>
        public static bool IsValidStart(Cuia[] cuias, int width, int[] units, InverseSpec spec, out int complexity)
        {
            complexity = 0;
            if (Empties(cuias, width) != spec.Empties)
            {
                return false;
            }

            for (int i = 0; i < width; i++)
            {
                if (CuiaRules.IsSealed(cuias[i], units))
                {
                    return false;
                }
            }

            if (BoardRules.IsWon(cuias, 0, width, units))
            {
                return false;
            }

            complexity = Complexity.Of(cuias, 0, width);
            return complexity >= spec.MinComplexity;
        }

        public static BoardState ToBoard(Cuia[] cuias, int width)
        {
            var copy = new Cuia[width];
            Array.Copy(cuias, copy, width);
            return new BoardState(copy);
        }
    }
}
