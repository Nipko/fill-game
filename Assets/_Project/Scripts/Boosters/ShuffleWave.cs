#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Shuffle;

namespace AF.Boosters
{
    /// <summary>
    /// La ola de la Pororoca en el cliente (bloque B2; 04 §2.4; 03 §16): envuelve el <see cref="Shuffler"/> de <c>AF.Solver</c>
    /// con las cifras de <see cref="ShuffleConfig"/> (08 §9) y devuelve un <see cref="ShuffleOutcome"/>, como
    /// <see cref="MorphoBooster"/> envuelve el solver. Determinista: la semilla sale de la del nivel, del tablero revelado y del
    /// ordinal de la Pororoca en el nivel (<see cref="Shuffler.SeedFor"/>), así que rehacer la partida guardada da la misma ola.
    /// Corre en el hilo principal [P]: la ola entera mide pocos milisegundos (<c>ShuffleAllocationTests</c>) frente a los
    /// 1,2 s de su animación. No es seguro entre hilos.
    /// </summary>
    public sealed class ShuffleWave
    {
        private readonly Shuffler _shuffler = new Shuffler();

        /// <summary>El tablero lleva Lluvia o Piraña (04 §2.4 d): la Pororoca se deshabilita en todo el nivel.</summary>
        public static bool HasTemporalMechanic(BoardState board)
        {
            return Shuffler.HasTemporalMechanic(board);
        }

        /// <summary>
        /// Lo que dice el tablero <paramref name="current"/> para el botón (04 §2.4 a, b y d): no admitida si el nivel lleva
        /// Lluvia o Piraña (<paramref name="levelHasTemporal"/>, del tablero inicial) o si hay ocultas sin
        /// <c>shuffle_reveals_murky</c>; poca agua con menos de 2 colores movibles. Sin asignaciones.
        /// </summary>
        public static ShuffleBoardBlock BlockOf(BoardState current, bool levelHasTemporal, ShuffleConfig config)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (levelHasTemporal)
            {
                return ShuffleBoardBlock.Unsupported;
            }

            switch (Shuffler.Check(current, config.ShuffleRevealsMurky))
            {
                case ShuffleBlock.None:
                    return ShuffleBoardBlock.None;
                case ShuffleBlock.LowWater:
                    return ShuffleBoardBlock.LowWater;
                default:
                    return ShuffleBoardBlock.Unsupported;
            }
        }

        /// <summary>
        /// Baraja <paramref name="current"/> (04 §2.4): semilla de <paramref name="levelSeed"/> y <paramref name="ordinal"/>, holgura
        /// y presupuesto de <paramref name="config"/>, w = 2 y el modo de vertido y la regla de fútiles de la partida.
        /// </summary>
        public ShuffleOutcome Run(BoardState current, string levelSeed, int ordinal, ShuffleConfig config, PourMode mode, bool futileRuleEnabled)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var options = new ShuffleOptions(config.ShuffleParSlack, config.ShuffleBudgetNodes, ShuffleOptions.DefaultWeight, futileRuleEnabled, mode);
            ShuffleResult result = _shuffler.Shuffle(current, Shuffler.SeedFor(levelSeed, current, ordinal), options);
            ShuffleWaveMethod method = result.Method == ShuffleMethod.Inverse ? ShuffleWaveMethod.Inverse
                : result.Method == ShuffleMethod.Permute ? ShuffleWaveMethod.Permute : ShuffleWaveMethod.Failed;
            return new ShuffleOutcome(method, result.Board, result.ParBefore, result.ParAfter, result.Tries, result.Revealed);
        }
    }
}
