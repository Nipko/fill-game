#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;

namespace AF.UI
{
    /// <summary>
    /// La Tinaja en el layout del tablero (M13; D-019; 02 §5; 07 §4.3 y §4.5; tarea TJ2): la tinaja y su cuia van en ranuras
    /// contiguas de la misma fila. Se mira la pareja en el tablero inicial del nivel al construirlo (la suelta quita el tipo a su
    /// cuia, pero las filas no cambian a mitad de nivel, D-006); se pasa a la vista de la tinaja (el jirau va bajo las dos
    /// ranuras de la pareja también al reanudar tras la suelta) y
    /// <see cref="BoardLayout.Compute(int, int, float, float, float, float, int, int)"/> mueve el corte entre filas si las
    /// separaría (siempre) y aprieta su fila si la vasija o el poste de su cuia se salen por una punta (solo si se dibujan:
    /// <see cref="LayoutTinajaIndex"/>).
    /// </summary>
    /// <remarks>
    /// En GameScreen.cs hay tres enganches (ampliación del ámbito de TJ2, a conservar al fusionar con la dueña de GameScreen.cs
    /// en la ola 6, M4.3): <c>BuildBoard</c> llama a <see cref="BindTinajaPair"/>; <c>LayoutBoardPanel</c> pasa
    /// <c>_tinajaPair</c> y <see cref="LayoutTinajaIndex"/> a <c>BoardLayout.Compute</c> y adelgaza el marco con
    /// <see cref="BoardLayoutResult.ContentLeft"/> (lo que se dibuja, con la vasija, en lugar del vidrio solo).
    /// </remarks>
    public sealed partial class GameScreen
    {
        // Primera cuia de la pareja de la Tinaja del nivel (BoardLayout.TinajaPairStart), o −1 sin tinaja.
        private int _tinajaPair = -1;

        // La tinaja de esa pareja (_tinajaPair o _tinajaPair + 1), o −1 sin tinaja.
        private int _tinajaIndex = -1;

        /// <summary>Primera cuia de la pareja de la Tinaja del nivel mostrado (la de índice menor), o −1 sin tinaja.</summary>
        internal int TinajaPair => _tinajaPair;

        /// <summary>La tinaja de la pareja del nivel mostrado, o −1 sin tinaja.</summary>
        internal int TinajaIndex => _tinajaIndex;

        /// <summary>
        /// La tinaja que se pasa al layout para hacer sitio a su vasija y al poste de su cuia: <see cref="TinajaIndex"/> solo si
        /// esas piezas se dibujan (el arte provisional, <see cref="MapArtRule.ShowProvisional"/>: Editor y desarrollo hasta los PNG
        /// del artista); si no, −1. En release, sin el arte, la tinaja se ve como vidrio y no hay vasija ni poste: su fila no se
        /// aprieta y el marco no se adelgaza por piezas que no se ven, así que el tablero sigue el reparto uniforme de 02 §5 y
        /// 07 §4.1 (ranura = ancho útil / cuias de la fila más larga). El corte entre filas para que la pareja vaya junta se mueve
        /// siempre (lo hace <c>_tinajaPair</c>).
        /// </summary>
        internal int LayoutTinajaIndex => MapArtRule.ShowProvisional ? _tinajaIndex : -1;

        // La pareja del tablero inicial de este nivel (BuildBoard, con las vistas ya creadas): guarda cuál es la tinaja, le
        // pasa a su vista el índice de su cuia y devuelve la primera de las dos.
        private int BindTinajaPair(BoardState initial)
        {
            int start = BoardLayout.TinajaPairStart(initial);
            _tinajaIndex = start < 0 ? -1 : (initial[start].Mechanic == MechanicType.Tinaja ? start : start + 1);
            if (_tinajaIndex >= 0 && _tinajaIndex < _views.Length)
            {
                int partner = _tinajaIndex == start ? start + 1 : start;
                _views[_tinajaIndex].SetTinajaPartner(partner);
            }

            return start;
        }
    }
}
