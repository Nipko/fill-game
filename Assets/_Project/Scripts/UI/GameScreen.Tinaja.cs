#nullable enable
using AF.Core.Board;

namespace AF.UI
{
    /// <summary>
    /// La Tinaja en el layout del tablero (M13; D-019; 02 §5; 07 §4.5; tarea TJ2): la tinaja y su cuia van en ranuras contiguas
    /// de la misma fila. Se mira la pareja en el tablero inicial del nivel al construirlo (la suelta quita el tipo a su cuia, pero
    /// las filas no cambian a mitad de nivel, D-006) y <see cref="BoardLayout.Compute(int, int, float, float, float, float, int)"/>
    /// mueve el corte entre filas si las separaría. En GameScreen.cs solo hay dos líneas: guardar la pareja en
    /// <c>BuildBoard</c> y pasarla al layout.
    /// </summary>
    public sealed partial class GameScreen
    {
        // Primera cuia de la pareja de la Tinaja del nivel (BoardLayout.TinajaPairStart), o −1 sin tinaja.
        private int _tinajaPair = -1;

        /// <summary>Primera cuia de la pareja de la Tinaja del nivel mostrado (la de índice menor), o −1 sin tinaja.</summary>
        internal int TinajaPair => _tinajaPair;

        // La pareja del tablero inicial de este nivel (BuildBoard).
        private static int TinajaPairOf(BoardState initial)
        {
            return BoardLayout.TinajaPairStart(initial);
        }
    }
}
