#nullable enable
using System;
using AF.Core.Rules;

namespace AF.Solver.Shuffle
{
    /// <summary>
    /// Parámetros de la Pororoca · Barajar (GDD 04 §2.4; 03 §16 y la tabla de usos del solver de 03 §5; 08 §9): la holgura del
    /// par (<c>shuffle_par_slack</c> = 2), el presupuesto del A* de verificación (<c>shuffle_budget_nodes</c> = 40.000) con
    /// <c>w</c> = 2, los intentos de reconstrucción inversa (20) y los de «Permutar + verificar» [P], el kill-switch de los
    /// fútiles y el modo de vertido. Los valores del cliente vienen de Remote Config (08 §9): aquí solo están sus defaults, para
    /// los tests y las herramientas.
    /// </summary>
    /// <remarks><c>default(ShuffleOptions)</c> no es válido: usa el constructor o <see cref="Default"/>.</remarks>
    public readonly struct ShuffleOptions
    {
        /// <summary><c>shuffle_par_slack</c> (08 §9: 2, rango 1-3): par nuevo ≤ par actual + holgura.</summary>
        public const int DefaultParSlack = 2;

        /// <summary><c>shuffle_budget_nodes</c> (08 §9: 40.000; 03 §5, «Pororoca (verificación del par tras barajar)»).</summary>
        public const int DefaultBudgetNodes = 40000;

        /// <summary>Peso del A* de verificación (03 §5: w = 2, como Morpho).</summary>
        public const int DefaultWeight = 2;

        /// <summary>Intentos de reconstrucción inversa antes de Permutar (04 §2.4: «si en 20 intentos no se cumple»).</summary>
        public const int DefaultInverseAttempts = 20;

        /// <summary>
        /// Intentos de «Permutar + verificar» [P]: 04 §2.4 no los cuenta; se usan los mismos 20 que la reconstrucción
        /// (<c>docs/dev/pororoca.md</c>, P-2).
        /// </summary>
        public const int DefaultPermuteAttempts = 20;

        /// <summary>Movimientos inversos mínimos (03 §16 y 04 §2.4: k = max(12, 3 × colores restantes)).</summary>
        public const int MinInverseMoves = 12;

        /// <summary>Movimientos inversos por color restante (k = max(12, 3 × colores restantes)).</summary>
        public const int InverseMovesPerColor = 3;

        /// <summary>Crea unas opciones.</summary>
        /// <param name="parSlack">Holgura del par (≥ 0; 08 §9: 1-3).</param>
        /// <param name="budgetNodes">Estados expandidos de cada A* de verificación (≥ 1).</param>
        /// <param name="weight">Peso del A* (1-16).</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> (02 R16): el A* excluye los fútiles.</param>
        /// <param name="pourMode">Modo de vertido de la partida (<c>rules_pour_mode</c>; D-005: <c>run</c>).</param>
        /// <param name="inverseAttempts">Intentos de reconstrucción inversa (≥ 0).</param>
        /// <param name="permuteAttempts">Intentos de Permutar + verificar (≥ 0).</param>
        public ShuffleOptions(
            int parSlack,
            int budgetNodes,
            int weight = DefaultWeight,
            bool futileRuleEnabled = true,
            PourMode pourMode = PourMode.Run,
            int inverseAttempts = DefaultInverseAttempts,
            int permuteAttempts = DefaultPermuteAttempts)
        {
            if (parSlack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parSlack), parSlack, "La holgura del par no puede ser negativa.");
            }

            if (budgetNodes < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(budgetNodes), budgetNodes, "El presupuesto debe ser al menos 1.");
            }

            if (weight < 1 || weight > Search.SolverOptions.MaxWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "El peso va de 1 a 16.");
            }

            if (!PourModes.IsDefined(pourMode))
            {
                throw new ArgumentOutOfRangeException(nameof(pourMode), pourMode, "Modo de vertido desconocido: Single o Run.");
            }

            if (inverseAttempts < 0 || permuteAttempts < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inverseAttempts), inverseAttempts, "Los intentos no pueden ser negativos.");
            }

            ParSlack = parSlack;
            BudgetNodes = budgetNodes;
            Weight = weight;
            FutileRuleEnabled = futileRuleEnabled;
            PourMode = pourMode;
            InverseAttempts = inverseAttempts;
            PermuteAttempts = permuteAttempts;
        }

        /// <summary>Los defaults de 08 §9 y 04 §2.4, en <c>run</c> y con la regla de fútiles.</summary>
        public static ShuffleOptions Default => new ShuffleOptions(DefaultParSlack, DefaultBudgetNodes);

        /// <summary>Holgura del par.</summary>
        public int ParSlack { get; }

        /// <summary>Presupuesto de cada A*.</summary>
        public int BudgetNodes { get; }

        /// <summary>Peso del A*.</summary>
        public int Weight { get; }

        /// <summary>Si el A* excluye los vertidos fútiles.</summary>
        public bool FutileRuleEnabled { get; }

        /// <summary>Modo de vertido.</summary>
        public PourMode PourMode { get; }

        /// <summary>Intentos de reconstrucción inversa.</summary>
        public int InverseAttempts { get; }

        /// <summary>Intentos de Permutar + verificar.</summary>
        public int PermuteAttempts { get; }

        internal bool IsValid => BudgetNodes >= 1 && Weight >= 1 && PourModes.IsDefined(PourMode);
    }
}
