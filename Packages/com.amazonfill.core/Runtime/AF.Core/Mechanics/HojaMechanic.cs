#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M1 Hoja (GDD 02 §9.1 y §9.2; 03 §9; propuesta de D-007 b.2 M1, debut 43): cuia vacía cubierta por una hoja con el
    /// icono de la fruta <c>key</c>, un «vacío reservado».
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Como destino solo acepta la fruta <c>key</c> (<c>legal</c> de 02 §1.2:
    /// <c>color_top(A) == color_hoja(B) and espacio(B) &gt; 0</c>); en <c>run</c> entra el tramo de <c>key</c> que cabe.</description></item>
    /// <item><description>No es origen mientras tenga la hoja (está vacía).</description></item>
    /// <item><description>Con el primer vertido de <c>key</c> la hoja cae en la etapa (b) de R10 y la cuia pasa a ser normal
    /// (<see cref="MechanicType.None"/>; 02 R16 para D-007). Como solo entra <c>key</c>, todo vertido la abre.</description></item>
    /// <item><description>Como destino nunca es fútil (<c>cambia_al_recibir</c> del anexo B): verter el resto de una fruta en
    /// su Hoja convierte un vacío reservado en uno general.</description></item>
    /// <item><description>Marea devuelve la hoja (restaura el estado anterior); Volver a la orilla también.</description></item>
    /// <item><description>Validación (03 §17 #6): empieza vacía y su llave está en el tablero.</description></item>
    /// </list>
    /// El estado es el propio tipo de la cuia (1 bit en 03 §9: hoja presente) con la bandera <see cref="Cuia.FlagClosed"/>.
    /// </remarks>
    internal sealed class HojaMechanic : MechanicBase
    {
        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            Cuia hoja = board[target];
            return board[source].TopColor == hoja.MechanicKey && hoja.Space > 0 ? TargetVerdict.Accept : TargetVerdict.Reject;
        }

        public override bool ChangesOnReceive(Cuia self)
        {
            return true;
        }

        public override Cuia OnReceived(Cuia self, int color)
        {
            return color == self.MechanicKey ? self.WithoutMechanic() : self;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia hoja = state[index];
            if (!hoja.IsEmpty)
            {
                return "02 §9.1 M1: la Hoja empieza vacía";
            }

            if (state.UnitsOf(hoja.MechanicKey) == 0)
            {
                return "03 §17 #6: la llave de la Hoja no está en el tablero";
            }

            return null;
        }
    }
}
