#nullable enable

namespace AF.Solver.Search
{
    /// <summary>Resultado de una búsqueda del solver (GDD 03 §5, pseudocódigo <c>ASTAR</c>).</summary>
    public enum SolveStatus
    {
        /// <summary>Encontró una solución; con peso 1 su longitud es el par (óptimo).</summary>
        Solved = 0,

        /// <summary>Agotó el espacio de estados con vertidos útiles: prueba de irresolubilidad.</summary>
        Unsolvable = 1,

        /// <summary>Superó el presupuesto de estados expandidos sin decidir.</summary>
        BudgetExceeded = 2,
    }
}
