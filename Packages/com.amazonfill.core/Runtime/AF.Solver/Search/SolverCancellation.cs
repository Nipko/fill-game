#nullable enable

namespace AF.Solver.Search
{
    /// <summary>
    /// Bandera del corte cooperativo por tiempo del solver en el cliente (Morpho y la pista por inactividad): 08 §9
    /// <c>hint_timeout_ms</c> = 150 (100-300), «al agotarse no se cobra»; 03 §5 («el cliente ejecuta el solver en un hilo y
    /// cancela por tiempo»); 04 §2.3 y 02 T5, respuesta 4 de D-019: «No encuentro un camino desde aquí», sin cobrar.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>El núcleo no lee relojes.</b> La marca otro hilo (el principal del cliente, al pasar
    /// <c>hint_timeout_ms</c> con su <c>IClock</c>) con <see cref="Request"/>; el núcleo solo la consulta.
    /// <see cref="AStarSolver"/> la consulta antes de expandir el primer estado y luego cada <see cref="PollInterval"/>
    /// expansiones, sin asignar memoria; al verla devuelve <see cref="SolveStatus.BudgetExceeded"/> con
    /// <see cref="SolveResult.WasCancelled"/>. <see cref="AF.Solver.Murky.SafeMoves"/> la consulta también al empezar y la
    /// propaga (<see cref="AF.Solver.Murky.SafeMovesOutcome.BudgetExceeded"/> con
    /// <see cref="AF.Solver.Murky.SafeMoves.WasCancelled"/>).</description></item>
    /// <item><description><b>Hilos:</b> es el único objeto que comparten el hilo que busca y el que corta. Un <c>int</c>
    /// volátil: la escritura de <see cref="Request"/> se ve en la siguiente consulta del otro hilo. Se pasa en
    /// <see cref="SolverOptions.Cancellation"/> (<see cref="SolverOptions.WithCancellation"/>); null = sin corte (el pipeline
    /// y las herramientas). <see cref="Reset"/> solo con la búsqueda parada (antes de lanzar la siguiente petición), o una
    /// instancia nueva por petición.</description></item>
    /// </list>
    /// </remarks>
    public sealed class SolverCancellation
    {
        /// <summary>
        /// Expansiones entre dos consultas de <see cref="AStarSolver"/> [P] (K2): tras <see cref="Request"/>, la búsqueda en curso
        /// expande como mucho estas antes de parar. Potencia de 2 (la consulta es una máscara).
        /// </summary>
        public const int PollInterval = 256;

        private volatile int _requested;

        /// <summary>Solo tests: consultas de <see cref="Poll"/> que faltan para marcar la bandera (0 = ninguna).</summary>
        private int _pollsUntilRequest;

        /// <summary>Si se pidió el corte (lectura volátil).</summary>
        public bool IsRequested => _requested != 0;

        /// <summary>Pide el corte. La llama otro hilo (el principal, al pasar <c>hint_timeout_ms</c>); idempotente.</summary>
        public void Request()
        {
            _requested = 1;
        }

        /// <summary>Quita la marca para reutilizar la instancia. Solo con ninguna búsqueda en curso que la use.</summary>
        public void Reset()
        {
            _pollsUntilRequest = 0;
            _requested = 0;
        }

        /// <summary>
        /// Solo tests (los compartidos no pueden sincronizarse con un hilo a mitad de búsqueda): simula que otro hilo marca la
        /// bandera justo antes de la consulta número <paramref name="poll"/> del solver (1 = la primera, antes de expandir
        /// nada; la consulta k llega con (k − 1) · <see cref="PollInterval"/> expansiones hechas en esa búsqueda). Cuenta las
        /// consultas de todas las búsquedas que la usen (<see cref="AF.Solver.Murky.SafeMoves"/> hace varias). No es segura entre
        /// hilos; <see cref="Reset"/> la anula.
        /// </summary>
        internal void RequestAtPoll(int poll)
        {
            _pollsUntilRequest = poll < 1 ? 1 : poll;
        }

        /// <summary>Consulta de <see cref="AStarSolver"/>: <see cref="IsRequested"/> más el gancho de los tests.</summary>
        internal bool Poll()
        {
            if (_pollsUntilRequest > 0 && --_pollsUntilRequest == 0)
            {
                _requested = 1;
            }

            return _requested != 0;
        }
    }
}
