#nullable enable
using AF.Core.Board;

namespace AF.Solver.Shuffle
{
    /// <summary>
    /// Cómo salió el tablero de la Pororoca: el parámetro <c>method</c> de <c>af_shuffle_result</c> (04 §11: <c>inverse</c>,
    /// <c>permute</c> o <c>failed</c>).
    /// </summary>
    public enum ShuffleMethod
    {
        /// <summary>Reconstrucción inversa verificada (04 §2.4, primer camino).</summary>
        Inverse = 0,

        /// <summary>Permutar + verificar (04 §2.4, respaldo tras 20 intentos inversos).</summary>
        Permute = 1,

        /// <summary>Ningún camino dio un tablero válido: «La ola no pudo remover el río» y no se cobra (04 §2.4).</summary>
        Failed = 2,
    }

    /// <summary>Por qué la Pororoca no se puede usar en un tablero, antes de barajar (04 §2.4 b y d).</summary>
    public enum ShuffleBlock
    {
        /// <summary>Se puede barajar.</summary>
        None = 0,

        /// <summary>El tablero lleva Lluvia o Piraña, con estado temporal: la Pororoca se deshabilita (04 §2.4 d; D-007) [P].</summary>
        TemporalMechanic = 1,

        /// <summary>Quedan menos de 2 colores movibles: «Hay poca agua libre para la ola» (04 §2.4 b).</summary>
        LowWater = 2,

        /// <summary>
        /// Hay dosis ocultas y <c>shuffle_reveals_murky</c> está apagado: la ola no puede revelar y la reconstrucción no puede
        /// garantizar el vertido seguro (04 §2.4 a; 02 T6) [P].
        /// </summary>
        HiddenWithoutReveal = 3,
    }

    /// <summary>
    /// Resultado de <see cref="Shuffler.Shuffle"/> (04 §2.4 y §11): el tablero nuevo, el método, el par antes y después
    /// (<c>par_before</c>, <c>par_after</c>) y los intentos (<c>tries</c>).
    /// </summary>
    public readonly struct ShuffleResult
    {
        internal ShuffleResult(ShuffleMethod method, ShuffleBlock block, BoardState? board, int parBefore, int parAfter, int tries, int revealed)
        {
            Method = method;
            Block = block;
            Board = board;
            ParBefore = parBefore;
            ParAfter = parAfter;
            Tries = tries;
            Revealed = revealed;
        }

        /// <summary>Método (<c>af_shuffle_result.method</c>).</summary>
        public ShuffleMethod Method { get; }

        /// <summary>Si no se barajó porque el tablero no lo admite (<see cref="ShuffleBlock.None"/> en otro caso).</summary>
        public ShuffleBlock Block { get; }

        /// <summary>
        /// Tablero barajado, sin dosis ocultas (la ola las revela, 04 §2.4 a), o null si falló. Las cuias fijas son las del
        /// tablero de partida (reveladas) y las movibles conservan su capacidad, su mecánica y su número de dosis vacías.
        /// </summary>
        public BoardState? Board { get; }

        /// <summary>Longitud de la solución del A* (w = 2) desde el tablero de partida; −1 si no la encontró en el presupuesto.</summary>
        public int ParBefore { get; }

        /// <summary>Longitud de la solución del A* desde el tablero barajado; −1 si falló.</summary>
        public int ParAfter { get; }

        /// <summary>Intentos usados: inversos más permutaciones (<c>af_shuffle_result.tries</c>).</summary>
        public int Tries { get; }

        /// <summary>Dosis ocultas que tenía el tablero de partida (las que la ola revela: <c>revealed_murky</c>).</summary>
        public int Revealed { get; }

        /// <summary>Hay tablero nuevo.</summary>
        public bool Succeeded => Board != null;
    }
}
