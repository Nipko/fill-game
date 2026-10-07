#nullable enable
using System;

namespace AF.Solver.Search
{
    /// <summary>
    /// Parámetros de una búsqueda (GDD 03 §5): presupuesto de estados expandidos, peso <c>w</c> de la heurística
    /// (<c>f = g + w·H</c>, solo enteros) y el kill-switch <c>futile_rule_enabled</c> (02 R16, 04 §5.1).
    /// </summary>
    /// <remarks>
    /// <c>default(SolverOptions)</c> no es válido: usa el constructor o <see cref="Pipeline"/>. Los valores del
    /// cliente (Morpho: <c>hint_budget_nodes</c>, <c>hint_weight</c>) vienen de Remote Config (08 §9) y no se
    /// duplican aquí.
    /// </remarks>
    public readonly struct SolverOptions
    {
        /// <summary>Presupuesto del pipeline offline para el par y la solución de campaña (03 §5).</summary>
        public const int PipelineNodeBudget = 300000;

        /// <summary>Peso máximo admitido.</summary>
        public const int MaxWeight = 16;

        /// <summary>Crea unas opciones.</summary>
        /// <param name="nodeBudget">Máximo de estados expandidos (≥ 1).</param>
        /// <param name="weight">Peso de la heurística (1 = óptimo; 2 = pista en cliente, 03 §5), de 1 a <see cref="MaxWeight"/>.</param>
        /// <param name="futileRuleEnabled">Excluir los vertidos fútiles (02 §1.4); false = solo vertidos legales.</param>
        public SolverOptions(int nodeBudget, int weight, bool futileRuleEnabled)
        {
            if (nodeBudget < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeBudget), nodeBudget, "El presupuesto debe ser al menos 1.");
            }

            if (weight < 1 || weight > MaxWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "El peso va de 1 a 16.");
            }

            NodeBudget = nodeBudget;
            Weight = weight;
            FutileRuleEnabled = futileRuleEnabled;
        }

        /// <summary>Par y solución óptimos del pipeline: 300.000 expandidos, w = 1, con la regla de fútiles (03 §5).</summary>
        public static SolverOptions Pipeline => new SolverOptions(PipelineNodeBudget, 1, true);

        /// <summary>Máximo de estados expandidos.</summary>
        public int NodeBudget { get; }

        /// <summary>Peso <c>w</c> de la heurística.</summary>
        public int Weight { get; }

        /// <summary>Si se excluyen los vertidos fútiles.</summary>
        public bool FutileRuleEnabled { get; }

        internal bool IsValid => NodeBudget >= 1 && Weight >= 1 && Weight <= MaxWeight;
    }
}
