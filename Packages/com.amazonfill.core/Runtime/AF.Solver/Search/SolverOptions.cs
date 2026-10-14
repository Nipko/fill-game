#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.Solver.Search
{
    /// <summary>
    /// Parámetros de una búsqueda (GDD 03 §5): presupuesto de estados expandidos, peso <c>w</c> de la heurística
    /// (<c>f = g + w·H</c>, solo enteros), el kill-switch <c>futile_rule_enabled</c> (02 R16, 04 §5.1) y el modo de
    /// vertido (<c>rules_pour_mode</c>; D-005: <see cref="AF.Core.Rules.PourMode.Run"/> por defecto,
    /// <see cref="AF.Core.Rules.PourMode.Single"/> para pruebas).
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
        /// <param name="pourMode">Modo de vertido de la partida que se resuelve (por defecto
        /// <see cref="AF.Core.Rules.PourMode.Run"/>, D-005; <see cref="AF.Core.Rules.PourMode.Single"/> para pruebas).</param>
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
        /// Heredado sin modo: equivale a <see cref="AF.Core.Rules.PourMode.Run"/>, que desde D-005 es también
        /// <see cref="PourModes.Default"/>. Conserva el comportamiento anterior; T6 de decisiones.md (retirarlo) sigue
        /// pendiente. Usa <see cref="SolverOptions(int, int, bool, AF.Core.Rules.PourMode)"/>.
        /// </summary>
        public SolverOptions(int nodeBudget, int weight, bool futileRuleEnabled)
            : this(nodeBudget, weight, futileRuleEnabled, PourMode.Run)
        {
        }

        /// <summary>
        /// Heredado sin modo: <c>Pipeline</c> = <c>ForPipeline(PourMode.Run)</c> = <c>ForPipeline(PourModes.Default)</c>
        /// desde D-005: par y solución óptimos del pipeline con la racha que cabe. Lo usan hoy <c>tools/level-generator</c>,
        /// la paridad con <c>levelgen_proto.py</c> y los tests de Unity; T6 de decisiones.md (retirarlo) sigue pendiente.
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

        /// <summary>
        /// Como <see cref="ForPipeline(PourMode)"/> para el nivel <paramref name="level"/>: sin poda de fútiles si el nivel
        /// lleva Tinaja (<see cref="RequiresUnprunedSearch"/>); en cualquier otro nivel, exactamente las mismas opciones.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="level"/> es null.</exception>
        public static SolverOptions ForPipeline(PourMode mode, BoardState level)
        {
            return ForPipeline(mode).ForLevel(level);
        }

        /// <summary>
        /// Las búsquedas que deciden «hay solución» en este tablero tienen que ir sin poda de fútiles (M13; propuesta de la
        /// tinaja, b.2 R14 y c.2; hallazgo C1-8): lleva una Tinaja (también vacía e inerte). Todo nivel con tinaja acaba con
        /// capacidades mixtas (su cuia suelta es una Cuia chica de 2 y la tinaja, de 5), y la poda de <c>MOVIMIENTOS</c> (03
        /// §5) puede dejar sin camino útil un tablero que se gana. Afecta al par, <c>SafePath</c>, <c>SafeMoves</c> (pipeline y
        /// Morpho), los paseos, la prueba de irresolubilidad y la verificación del rescate; el bot no cambia.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> es null.</exception>
        public static bool RequiresUnprunedSearch(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].Mechanic == MechanicType.Tinaja)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Las mismas opciones con otro valor de <see cref="FutileRuleEnabled"/>.</summary>
        public SolverOptions WithFutileRule(bool futileRuleEnabled)
        {
            return new SolverOptions(NodeBudget, Weight, futileRuleEnabled, PourMode);
        }

        /// <summary>
        /// Las mismas opciones para el nivel <paramref name="level"/>: sin poda de fútiles si <see cref="RequiresUnprunedSearch"/>
        /// (Tinaja, M13); si no, sin cambios. Vale igual para las opciones de Morpho que pasa el cliente.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="level"/> es null.</exception>
        public SolverOptions ForLevel(BoardState level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            return RequiresUnprunedSearch(level) ? WithFutileRule(false) : this;
        }
    }
}
