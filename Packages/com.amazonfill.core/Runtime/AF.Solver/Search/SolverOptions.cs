#nullable enable
using System;
using AF.Core.Rules;

namespace AF.Solver.Search
{
    /// <summary>
    /// Parámetros de una búsqueda (GDD 03 §5): presupuesto de estados expandidos, peso <c>w</c> de la heurística
    /// (<c>f = g + w·H</c>, solo enteros), el kill-switch <c>futile_rule_enabled</c> (02 R16, 04 §5.1) y el modo de
    /// vertido (D-001; <c>rules_pour_mode</c>).
    /// </summary>
    /// <remarks>
    /// <c>default(SolverOptions)</c> no es válido: usa el constructor o <see cref="ForPipeline"/>. Los valores del
    /// cliente (Morpho: <c>hint_budget_nodes</c>, <c>hint_weight</c>) vienen de Remote Config (08 §9) y no se
    /// duplican aquí. El par depende del modo: un pack resuelto en un modo y jugado en el otro da pares falsos
    /// (riesgo R-2 de decisiones.md).
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
        /// <param name="pourMode">Modo de vertido de la partida que se resuelve (D-001).</param>
        public SolverOptions(int nodeBudget, int weight, bool futileRuleEnabled, PourMode pourMode)
        {
            if (nodeBudget < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeBudget), nodeBudget, "El presupuesto debe ser al menos 1.");
            }

            if (weight < 1 || weight > MaxWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "El peso va de 1 a 16.");
            }

            if (!PourModes.IsDefined(pourMode))
            {
                throw new ArgumentOutOfRangeException(nameof(pourMode), pourMode, "Modo de vertido desconocido: Single o Run.");
            }

            NodeBudget = nodeBudget;
            Weight = weight;
            FutileRuleEnabled = futileRuleEnabled;
            PourMode = pourMode;
        }

        /// <summary>
        /// Heredado (equivale a <see cref="AF.Core.Rules.PourMode.Run"/>): conserva el comportamiento anterior hasta que
        /// T6 lo retire. Usa <see cref="SolverOptions(int, int, bool, AF.Core.Rules.PourMode)"/>.
        /// </summary>
        public SolverOptions(int nodeBudget, int weight, bool futileRuleEnabled)
            : this(nodeBudget, weight, futileRuleEnabled, PourMode.Run)
        {
        }

        /// <summary>
        /// Heredado (equivale a <c>ForPipeline(PourMode.Run)</c>): par y solución óptimos del pipeline con la racha que
        /// cabe. Lo usan hoy <c>tools/level-generator</c>, la paridad con <c>levelgen_proto.py</c> y los tests de Unity;
        /// lo retira T6.
        /// </summary>
        public static SolverOptions Pipeline => ForPipeline(PourMode.Run);

        /// <summary>Máximo de estados expandidos.</summary>
        public int NodeBudget { get; }

        /// <summary>Peso <c>w</c> de la heurística.</summary>
        public int Weight { get; }

        /// <summary>Si se excluyen los vertidos fútiles.</summary>
        public bool FutileRuleEnabled { get; }

        /// <summary>Modo de vertido de las transiciones (cantidad 1 en <see cref="AF.Core.Rules.PourMode.Single"/>).</summary>
        public PourMode PourMode { get; }

        internal bool IsValid => NodeBudget >= 1 && Weight >= 1 && Weight <= MaxWeight && PourModes.IsDefined(PourMode);

        /// <summary>
        /// Par y solución óptimos del pipeline en el modo <paramref name="mode"/>: 300.000 expandidos, w = 1, con la regla
        /// de fútiles (03 §5).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> no es un valor definido.</exception>
        public static SolverOptions ForPipeline(PourMode mode)
        {
            return new SolverOptions(PipelineNodeBudget, 1, true, mode);
        }
    }
}
