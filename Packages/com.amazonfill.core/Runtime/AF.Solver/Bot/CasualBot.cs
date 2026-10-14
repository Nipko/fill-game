#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AF.Solver.Bot
{
    /// <summary>
    /// Bot casual ×40 (GDD 03 §6; 08 §2.2: vive en AF.Solver; 08 §14.2: reproducible por semilla): juega 40 partidas sin
    /// anticipación, con la jugada que «parece buena» el 85 % de las veces y una al azar el 15 %, y mide
    /// <c>fallo_bot</c> = fallos / partidas (03 §7). Mide la dificultad percibida; la etiqueta la decide él (02 §10).
    /// </summary>
    /// <remarks>
    /// <para><b>Movimientos y transición:</b> <c>M</c> = <see cref="SolverMoves"/> (03 §5) sobre el tablero en orden canónico y
    /// la transición es <see cref="BoardRules.ApplyInPlace(Cuia[], int, int, int[], int, int, PourMode, bool, ref PourEvents)"/>
    /// (R10 por etapas), así que el bot juega cualquier mecánica portada sin código propio.</para>
    /// <para><b>Puntuación</b> (<c>PUNTUACION_CASUAL</c> de 03 §6, congelada aquí antes de calificar el primer pack), en
    /// enteros y multiplicada por 4 [P]: <c>4·n</c>; +16 si la cuia que recibe no está vacía y +40 más si queda llena y
    /// monocolor (sella, 02 R5); −20 si está vacía; −12 si el vertido es parcial (<c>n</c> &lt; racha visible de A); +8 si
    /// vacía la racha y deja expuesto un color que tiene destino disponible (otra cuia vacía, o con ese color arriba y
    /// hueco; si la dosis expuesta está oculta, el jugador no la ve y no puntúa [P]; el Barro expuesto tampoco puntúa,
    /// porque no es un color, 02 §9.1 M5 [P]). Con mecánicas puntúa la cuia que
    /// recibe las dosis (la pareja en el Remolino) [P]. En el Remolino la racha entra por el fondo de la pareja y de
    /// cualquier color (02 §9.1 M4), así que el +16 exige que la pareja sea monocolor del color vertido y el +40, además,
    /// que quede llena con <c>cap</c> = <c>U_c</c> (sella, 02 R5); si la pareja mezcla colores no suma nada [P]. <b>Rama
    /// mínima de la Tinaja</b> (M13; D-019, Q-Ti2 A; propuesta de la tinaja, c.2): la cuia colgada de la tinaja recibe
    /// cualquier fruta, así que el +16 (y el +40 que depende de él) solo suma si su cima es de la fruta sacada; sacar a la
    /// cuia vacía puntúa 4 − 20 = −16. Las demás mecánicas no añaden ramas y el resto de la puntuación sigue congelado.</para>
    /// <para><b>Ruido:</b> el «+ rng()·0,5» de 03 §6 pasa a un entero equivalente: la clave es
    /// <c>puntos·65.536 + NextInt(131.072)</c>, un ruido en [0, 2) unidades de la puntuación ×4, es decir [0, 0,5) de la
    /// original. Como las puntuaciones distintas se separan al menos 4 unidades, el ruido solo desempata, como en el
    /// prototipo. A igualdad total gana el primer movimiento.</para>
    /// <para><b>Partida</b> (<c>BOT</c> de 03 §6): hasta 400 veces: si es victoria, éxito; si <c>M</c> está vacío, fallo
    /// (Aguas estancadas); si <c>NextInt(100)</c> &lt; 85, la de mayor clave; si no, una de <c>M</c> al azar. Agotar el tope
    /// es fallo. Las 40 partidas comparten un generador sembrado con <see cref="SeedFor"/> de la semilla del nivel.</para>
    /// <para>Una instancia reutiliza sus búferes: una vez calentada no asigna (ni por paso ni por partida). No es segura
    /// entre hilos.</para>
    /// </remarks>
    public sealed class CasualBot
    {
        /// <summary>Flujo de sub-semilla del bot (el prototipo usaba <c>semilla ^ 0xB07</c>).</summary>
        public const ulong SeedStream = 0xB07UL;

        private const int ScoreScale = 65536;
        private const int NoiseRange = 2 * ScoreScale;

        private readonly Prng _rng = new Prng(0UL);
        private readonly Cuia[] _start = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _work = new Cuia[BoardState.MaxCuias];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly SolverMove[] _moves = new SolverMove[SolverMoves.MaxMoves];
        private int _width;

        /// <summary>Semilla del bot para un nivel: <c>SubSeed(SeedFrom(seed), 0xB07)</c>.</summary>
        public static ulong SeedFor(string levelSeed)
        {
            return Prng.SubSeed(Prng.SeedFrom(levelSeed), SeedStream);
        }

        /// <summary>
        /// Juega <see cref="BotOptions.Runs"/> partidas desde <paramref name="state"/> con el generador sembrado con
        /// <paramref name="seed"/> y devuelve los fallos. La misma semilla da siempre el mismo resultado.
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

        /// <summary>Como <see cref="FailRate(BoardState, ulong, BotOptions)"/> con 03 §6 en el modo <paramref name="mode"/>.</summary>
        public BotResult FailRate(BoardState state, ulong seed, PourMode mode)
        {
            return FailRate(state, seed, BotOptions.ForPipeline(mode));
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

        /// <summary>Una partida (<c>BOT</c> de 03 §6); true si gana.</summary>
        private bool PlayOnce(BotOptions options)
        {
            int width = _width;
            Array.Copy(_start, _work, width);
            PourMode mode = options.PourMode;
            for (int step = 0; step < options.MaxSteps; step++)
            {
                if (BoardRules.IsWon(_work, 0, width, _units))
                {
                    return true;
                }

                int count = SolverMoves.Collect(_work, 0, width, _units, mode, options.FutileRuleEnabled, _moves);
                if (count == 0)
                {
                    return false; // Aguas estancadas
                }

                SolverMove chosen = _rng.NextInt(100) < options.GreedyPercent
                    ? _moves[Greedy(count)]
                    : _moves[_rng.NextInt(count)];
                var events = default(PourEvents);
                BoardRules.ApplyInPlace(_work, 0, width, _units, chosen.Source, chosen.Target, mode, chosen.IsFutile, ref events);
                BoardCanon.Sort(_work, 0, width);
            }

            return false; // tope
        }

        private int Greedy(int count)
        {
            int best = 0;
            long bestKey = long.MinValue;
            var board = new BoardSlice(_work, 0, _width, _units);
            for (int m = 0; m < count; m++)
            {
                long key = ((long)Score(in board, _moves[m]) * ScoreScale) + _rng.NextInt(NoiseRange);
                if (key > bestKey)
                {
                    bestKey = key;
                    best = m;
                }
            }

            return best;
        }

        /// <summary><c>PUNTUACION_CASUAL</c> de 03 §6 multiplicada por 4.</summary>
        internal static int Score(in BoardSlice board, SolverMove move)
        {
            int source = move.Source;
            int amount = move.Amount;
            Cuia from = board[source];
            int receiver = BoardRules.ReceiverOf(in board, move.Target);
            Cuia to = board[receiver];
            int score = 4 * amount;
            if (to.IsEmpty)
            {
                score -= 20; // gastar un vacío parece caro (sacar a la cuia de la tinaja vacía: 4 − 20 = −16)
            }
            else if (receiver == move.Target)
            {
                // Rama mínima de la Tinaja (M13; D-019, Q-Ti2 A; propuesta de la tinaja, c.2): su cuia colgada recibe cualquier
                // fruta, así que «juntar» (y lo que cuelga de él) solo vale si su cima es de la fruta sacada. En el resto de
                // destinos la regla normal ya garantiza el color.
                if (to.Mechanic != MechanicType.Cucharon || to.TopColor == from.TopColor)
                {
                    score += 16; // juntar con el mismo color
                    if (to.IsMonocolor && to.Count + amount == to.Capacity)
                    {
                        score += 40; // sella una cuia (llena y monocolor, 02 R5)
                    }
                }
            }
            else
            {
                // Remolino (02 §9.1 M4): la racha entra por el fondo de la pareja y de cualquier color, así que solo junta si
                // la pareja es monocolor del color vertido y solo sella si además queda llena con cap = U_c (R5) [P].
                int color = from.TopColor;
                if (to.IsMonocolor && to.TopColor == color)
                {
                    score += 16;
                    if (to.Count + amount == to.Capacity && to.Capacity == board.Units[color])
                    {
                        score += 40;
                    }
                }
            }

            int run = from.TopRunLength;
            if (amount < run)
            {
                score -= 12; // vertido parcial
            }

            int below = from.Count - run - 1;
            if (amount == run && below >= 0 && !from.IsHiddenAt(below) && from.ColorAt(below) != Cuia.MudColor
                && HasDestinationFor(in board, source, from.ColorAt(below)))
            {
                score += 8; // deja expuesto un color con destino disponible (el Barro no es un color, 02 §9.1 M5)
            }

            return score;
        }

        private static bool HasDestinationFor(in BoardSlice board, int source, int color)
        {
            for (int k = 0; k < board.Count; k++)
            {
                if (k == source)
                {
                    continue;
                }

                Cuia cuia = board[k];
                if (cuia.IsEmpty || (cuia.TopColor == color && !cuia.IsFull))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Resultado de <see cref="CasualBot.FailRate(BoardState, ulong, BotOptions)"/>.</summary>
    public readonly struct BotResult
    {
        internal BotResult(int failures, int runs)
        {
            Failures = failures;
            Runs = runs;
        }

        /// <summary>Partidas perdidas (Aguas estancadas o tope de vertidos).</summary>
        public int Failures { get; }

        /// <summary>Partidas jugadas.</summary>
        public int Runs { get; }

        /// <summary><c>fallo_bot</c> (03 §7) = fallos / partidas, para el JSON y los informes (no entra en ninguna regla).</summary>
        public double Rate => Runs == 0 ? 0.0 : (double)Failures / Runs;
    }
}
