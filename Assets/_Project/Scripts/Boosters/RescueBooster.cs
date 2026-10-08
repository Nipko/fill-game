#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.Solver.Search;

namespace AF.Boosters
{
    /// <summary>Resultado de la verificación «la compra cumple» (04 §6.4).</summary>
    public enum RescueCheck
    {
        /// <summary>No se comprobó (antes del 31, ya hubo rescate en el nivel o no hay cartera).</summary>
        NotChecked = 0,

        /// <summary>El A* encontró solución con una cuia más: se ofrece.</summary>
        Solvable = 1,

        /// <summary>Demostrado sin salida ni con una cuia más: rama F, no se vende (principio 4).</summary>
        Unsolvable = 2,

        /// <summary>Se agotó el presupuesto sin decidir: se ofrece (el presupuesto no demostró nada).</summary>
        Timeout = 3,

        /// <summary>Ya hay 16 cuias en pantalla (02 R1): no cabe otra.</summary>
        NoRoom = 4,
    }

    /// <summary>
    /// Cuia de rescate · «Continuar: +1 cuia» (GDD 04 §6.2-§6.4; 08 §9 <c>rescue_*</c> y <c>hint_*</c>): verificación «la
    /// compra cumple» con un A* acotado (<c>hint_budget_nodes</c> = 40.000, peso <c>hint_weight</c>) sobre el estado + 1 cuia
    /// vacía de capacidad 4, y el tope de 1 por nivel. El precio (600, o 300 el primero del día) lo cotiza
    /// <see cref="Wallet.QuoteRescue"/> y se cobra con <see cref="Transaction.Rescue"/>. Reutiliza su solver entre llamadas
    /// (sin asignaciones en el bucle de expansión). Un solo hilo.
    /// </summary>
    public sealed class RescueBooster
    {
        private readonly AStarSolver _solver = new AStarSolver();
        private readonly BoosterConfig _config;

        /// <summary>Crea el comodín.</summary>
        public RescueBooster(BoosterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Expandidos de la última verificación (diagnóstico).</summary>
        public int LastExpanded { get; private set; }

        /// <summary>El rescate se ofrece con <paramref name="check"/> (resoluble o sin decidir; 04 §6.4).</summary>
        public static bool IsOffered(RescueCheck check)
        {
            return check == RescueCheck.Solvable || check == RescueCheck.Timeout;
        }

        /// <summary><paramref name="state"/> con una cuia vacía de capacidad 4 añadida al final (04 §2.2: la que trae Capi).</summary>
        /// <exception cref="InvalidOperationException">Ya hay 16 cuias.</exception>
        public static BoardState WithEmptyCuia(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Count >= BoardState.MaxCuias)
            {
                throw new InvalidOperationException("R1: ya hay 16 cuias; no cabe otra.");
            }

            var cuias = new Cuia[state.Count + 1];
            for (int i = 0; i < state.Count; i++)
            {
                cuias[i] = state[i];
            }

            cuias[state.Count] = Cuia.Empty(BoosterConfig.AddedCuiaCapacity);
            return new BoardState(cuias);
        }

        /// <summary>
        /// Verifica «la compra cumple» sobre <paramref name="state"/> + 1 cuia (04 §6.4) con el modo de vertido y la regla de
        /// fútiles de la partida. Con <c>rescue_check_solvable</c> = false no comprueba y lo da por resoluble.
        /// </summary>
        public RescueCheck Check(BoardState state, PourMode pourMode, bool futileRuleEnabled)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Count >= BoardState.MaxCuias)
            {
                return RescueCheck.NoRoom;
            }

            if (!_config.RescueCheckSolvable)
            {
                return RescueCheck.Solvable;
            }

            var options = new SolverOptions(_config.HintBudgetNodes, _config.SolverWeight, futileRuleEnabled, pourMode);
            SolveResult result = _solver.Solve(WithEmptyCuia(state), options);
            LastExpanded = result.Expanded;
            switch (result.Status)
            {
                case SolveStatus.Solved: return RescueCheck.Solvable;
                case SolveStatus.Unsolvable: return RescueCheck.Unsolvable;
                default: return RescueCheck.Timeout;
            }
        }
    }
}
