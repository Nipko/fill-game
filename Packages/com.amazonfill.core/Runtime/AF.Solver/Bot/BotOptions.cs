#nullable enable
using System;
using AF.Core.Rules;

namespace AF.Solver.Bot
{
    /// <summary>
    /// Parámetros del bot casual ×40 (GDD 03 §6; research/02 §4: requiemformemories): 40 partidas, 85 % de jugadas codiciosas
    /// y 15 % al azar, tope de 400 vertidos por partida. El modo de vertido es el del juego (<see cref="PourMode.Run"/>,
    /// D-005): la puntuación y los umbrales de etiqueta se calibran en <c>run</c> (03 §6).
    /// </summary>
    public readonly struct BotOptions
    {
        /// <summary>Partidas por nivel (03 §6).</summary>
        public const int DefaultRuns = 40;

        /// <summary>Tope de vertidos por partida (03 §6).</summary>
        public const int DefaultMaxSteps = 400;

        /// <summary>Probabilidad de jugada codiciosa, en tanto por cien (p_greedy = 0,85, 03 §6).</summary>
        public const int DefaultGreedyPercent = 85;

        /// <summary>Crea unos parámetros.</summary>
        public BotOptions(int runs, int maxSteps, int greedyPercent, PourMode pourMode, bool futileRuleEnabled)
        {
            if (runs < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(runs), runs, "Al menos una partida.");
            }

            if (maxSteps < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSteps), maxSteps, "Al menos un vertido.");
            }

            if (greedyPercent < 0 || greedyPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(greedyPercent), greedyPercent, "Un tanto por cien.");
            }

            if (!PourModes.IsDefined(pourMode))
            {
                throw new ArgumentOutOfRangeException(nameof(pourMode), pourMode, "Modo de vertido desconocido.");
            }

            Runs = runs;
            MaxSteps = maxSteps;
            GreedyPercent = greedyPercent;
            PourMode = pourMode;
            FutileRuleEnabled = futileRuleEnabled;
        }

        /// <summary>Los de 03 §6 en el modo <paramref name="mode"/>: 40 partidas, 85/15, tope 400, con la regla de fútiles.</summary>
        public static BotOptions ForPipeline(PourMode mode)
        {
            return new BotOptions(DefaultRuns, DefaultMaxSteps, DefaultGreedyPercent, mode, true);
        }

        /// <summary>Partidas.</summary>
        public int Runs { get; }

        /// <summary>Tope de vertidos por partida.</summary>
        public int MaxSteps { get; }

        /// <summary>Jugadas codiciosas, en tanto por cien.</summary>
        public int GreedyPercent { get; }

        /// <summary>Modo de vertido.</summary>
        public PourMode PourMode { get; }

        /// <summary><c>futile_rule_enabled</c>: el bot elige entre <c>MOVIMIENTOS</c> (03 §5) y falla en Aguas estancadas.</summary>
        public bool FutileRuleEnabled { get; }

        internal bool IsValid => Runs >= 1 && MaxSteps >= 1 && PourModes.IsDefined(PourMode);
    }
}
