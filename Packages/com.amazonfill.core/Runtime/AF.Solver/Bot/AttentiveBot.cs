#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AF.Solver.Bot
{
    /// <summary>
    /// Bot atento ×40 [P] (D-068; propuesta de la curva 1.1, §6.4; 03 §12 y §17, comprobación 20): el bot casual (03 §6) con
    /// lookahead de 2 vertidos y sin los vertidos que bloquean de inmediato. Solo va en el informe de level-qa, nunca en la
    /// etiqueta (la sigue decidiendo el bot casual, D-009): sirve para comprobar que los picos crecen dentro de un mundo
    /// (<c>bot_atento(hito n+1) ≥ bot_atento(hito n)</c>), porque el casual satura en los hitos.
    /// </summary>
    /// <remarks>
    /// <para><b>Movimientos y transición:</b> los del casual: <c>M</c> = <see cref="SolverMoves"/> (03 §5) sobre el tablero en
    /// orden canónico y R10 por etapas con <see cref="BoardRules"/>, así que juega cualquier mecánica portada sin código propio.</para>
    /// <para><b>Sin bloqueos inmediatos</b> [P]: de <c>M</c> descarta los vertidos tras los que el tablero queda en Aguas estancadas
    /// (R13: sin vertidos útiles y sin victoria). Si todos bloquean, juega entre todos (y pierde en el paso siguiente).</para>
    /// <para><b>Lookahead de 2</b> [P]: el valor de un vertido m es <c>PUNTUACION_CASUAL(m)</c> (la de 03 §6 ×4, la misma función
    /// que el casual) más la mejor <c>PUNTUACION_CASUAL</c> de los vertidos de <c>M</c> en el tablero que deja m; si m gana, más
    /// <see cref="WinBonus"/>. Con el 85 % elige el de mayor valor, con el mismo ruido entero que el casual (solo desempata), y con
    /// el 15 %, uno al azar de los que no bloquean.</para>
    /// <para><b>Partida:</b> la de 03 §6 con <see cref="BotOptions"/> (40 partidas, 85/15, tope de 400 vertidos; fallo en Aguas
    /// estancadas o al agotar el tope), con su propia sub-semilla (<see cref="SeedFor"/>, flujo <see cref="SeedStream"/>). La
    /// misma semilla da siempre el mismo resultado en CoreCLR, Mono e IL2CPP: solo enteros y el PRNG del núcleo.</para>
    /// <para>Una instancia reutiliza sus búferes: una vez calentada no asigna (lo mide <c>HotLoopAllocationTests</c>). No es segura
    /// entre hilos.</para>
    /// </remarks>
    public sealed class AttentiveBot
    {
        /// <summary>Flujo de sub-semilla del bot atento (distinto del casual, <see cref="CasualBot.SeedStream"/>).</summary>
        public const ulong SeedStream = 0xA7E07UL;

        /// <summary>Premio de un vertido que gana el nivel, en las unidades de la puntuación ×4 [P].</summary>
        public const int WinBonus = 1000;

        private const int ScoreScale = 65536;
        private const int NoiseRange = 2 * ScoreScale;

        private readonly Prng _rng = new Prng(0UL);
        private readonly Cuia[] _start = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _work = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _next = new Cuia[BoardState.MaxCuias];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly SolverMove[] _moves = new SolverMove[SolverMoves.MaxMoves];
        private readonly SolverMove[] _replies = new SolverMove[SolverMoves.MaxMoves];
        private readonly int[] _values = new int[SolverMoves.MaxMoves];
        private readonly int[] _pool = new int[SolverMoves.MaxMoves];
        private int _width;

        /// <summary>Semilla del bot atento para un nivel: <c>SubSeed(SeedFrom(seed), 0xA7E07)</c>.</summary>
        public static ulong SeedFor(string levelSeed)
        {
            return Prng.SubSeed(Prng.SeedFrom(levelSeed), SeedStream);
        }

        /// <summary>
        /// Juega <see cref="BotOptions.Runs"/> partidas desde <paramref name="state"/> con el generador sembrado con
        /// <paramref name="seed"/> y devuelve los fallos.
        /// </summary>
        public BotResult FailRate(BoardState state, ulong seed, BotOptions options)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!options.IsValid)
            {
                throw new ArgumentException("Opciones no válidas: usa BotOptions.ForPipeline o su constructor.", nameof(options));
            }

            Load(state);
            _rng.Reseed(seed);
            int failures = 0;
            for (int run = 0; run < options.Runs; run++)
            {
                if (!PlayOnce(options))
                {
                    failures++;
                }
            }

            return new BotResult(failures, options.Runs);
        }

        /// <summary>Como <see cref="FailRate(BoardState, ulong, BotOptions)"/> con los parámetros de 03 §6 en el modo <paramref name="mode"/>.</summary>
        public BotResult FailRate(BoardState state, ulong seed, PourMode mode)
        {
            return FailRate(state, seed, BotOptions.ForPipeline(mode));
        }

        /// <summary>
        /// Para los tests: carga <paramref name="state"/> (en orden canónico) y devuelve en <paramref name="moves"/> sus vertidos
        /// de <c>M</c> y en <paramref name="pool"/> los que el bot consideraría (los que no bloquean, o todos); devuelve cuántos
        /// hay en el grupo y en <paramref name="count"/>, cuántos en <c>M</c>.
        /// </summary>
        internal int Candidates(BoardState state, PourMode mode, SolverMove[] moves, int[] pool, out int count)
        {
            Load(state);
            Array.Copy(_start, _work, _width);
            count = SolverMoves.Collect(_work, 0, _width, _units, mode, true, _moves);
            int size = count == 0 ? 0 : Evaluate(count, mode, true);
            Array.Copy(_moves, moves, count);
            Array.Copy(_pool, pool, size);
            return size;
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

        private bool PlayOnce(BotOptions options)
        {
            int width = _width;
            Array.Copy(_start, _work, width);
            PourMode mode = options.PourMode;
            bool futileRule = options.FutileRuleEnabled;
            for (int step = 0; step < options.MaxSteps; step++)
            {
                if (BoardRules.IsWon(_work, 0, width, _units))
                {
                    return true;
                }

                int count = SolverMoves.Collect(_work, 0, width, _units, mode, futileRule, _moves);
                if (count == 0)
                {
                    return false; // Aguas estancadas
                }

                int pool = Evaluate(count, mode, futileRule);
                int chosen;
                if (_rng.NextInt(100) < options.GreedyPercent)
                {
                    chosen = _pool[0];
                    long bestKey = long.MinValue;
                    for (int p = 0; p < pool; p++)
                    {
                        int m = _pool[p];
                        long key = ((long)_values[m] * ScoreScale) + _rng.NextInt(NoiseRange);
                        if (key > bestKey)
                        {
                            bestKey = key;
                            chosen = m;
                        }
                    }
                }
                else
                {
                    chosen = _pool[_rng.NextInt(pool)];
                }

                SolverMove move = _moves[chosen];
                var events = default(PourEvents);
                BoardRules.ApplyInPlace(_work, 0, width, _units, move.Source, move.Target, mode, move.IsFutile, ref events);
                BoardCanon.Sort(_work, 0, width);
            }

            return false; // tope
        }

        /// <summary>
        /// Valora los <paramref name="count"/> vertidos de <see cref="_moves"/> (lookahead de 2) y deja en <see cref="_pool"/> los
        /// que no bloquean (o todos, si bloquean todos); devuelve cuántos hay en el grupo.
        /// </summary>
        private int Evaluate(int count, PourMode mode, bool futileRule)
        {
            int width = _width;
            var board = new BoardSlice(_work, 0, width, _units);
            var next = new BoardSlice(_next, 0, width, _units);
            int safe = 0;
            for (int m = 0; m < count; m++)
            {
                SolverMove move = _moves[m];
                int value = CasualBot.Score(in board, move);
                Array.Copy(_work, _next, width);
                var events = default(PourEvents);
                BoardRules.ApplyInPlace(_next, 0, width, _units, move.Source, move.Target, mode, move.IsFutile, ref events);
                bool blocks;
                if (BoardRules.IsWon(in next))
                {
                    value += WinBonus;
                    blocks = false;
                }
                else
                {
                    int replies = SolverMoves.Collect(_next, 0, width, _units, mode, futileRule, _replies);
                    blocks = replies == 0;
                    int best = 0;
                    for (int r = 0; r < replies; r++)
                    {
                        int score = CasualBot.Score(in next, _replies[r]);
                        if (r == 0 || score > best)
                        {
                            best = score;
                        }
                    }

                    value += best;
                }

                _values[m] = value;
                if (!blocks)
                {
                    _pool[safe++] = m;
                }
            }

            if (safe > 0)
            {
                return safe;
            }

            for (int m = 0; m < count; m++)
            {
                _pool[m] = m;
            }

            return count;
        }
    }
}
