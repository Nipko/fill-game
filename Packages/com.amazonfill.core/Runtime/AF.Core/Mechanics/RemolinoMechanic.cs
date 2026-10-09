#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M4 Remolino (GDD 02 §1.2 rama del remolino en <c>legal</c> y §9.1 M4; 03 §9: transición distinta y canon que conserva
    /// el emparejamiento; propuesta de D-007 b.2 M4; debut 103): par de cuias unidas por debajo.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Tocar una cuia del par como destino siempre significa remolino: la racha entra por el <b>fondo</b>
    /// de la pareja, sea del color que sea (no aplica la regla de mismo color).</description></item>
    /// <item><description>Es legal si la cuia tocada no está sellada (02 §1.2: <c>sellada(B)</c> va antes) y la pareja tiene
    /// hueco y no está sellada (si una sella, la otra ya no puede recibir). La cuia tocada puede estar llena.</description></item>
    /// <item><description>Cantidad: <c>min(racha, espacio(pareja))</c> en <c>run</c>; 1 en <c>single</c> (02 §1.2).</description></item>
    /// <item><description>Respuesta Q-A del dueño: una cuia del par no puede verter en su propia pareja (vertido ilegal,
    /// rechazo sin texto, R11): tocar la pareja como destino metería la racha por el fondo de la propia cuia de
    /// origen.</description></item>
    /// <item><description>Como origen, la regla normal (vierte su cima).</description></item>
    /// <item><description>Como destino nunca es fútil (<c>cambia_al_recibir</c> del anexo B; regresión 4 de 04 §5.1) y, por
    /// la respuesta Q-B del dueño, tampoco como origen (<c>ExemptAsSource</c>), para que R14 se cumpla.</description></item>
    /// <item><description>Sin estado propio: el emparejamiento es el id de pareja de <see cref="Cuia.Packed"/> (03 §3: las dos
    /// cuias se canonicalizan como unidad); la pareja de una cuia es la otra cuia de Remolino con el mismo id. Marea deshace
    /// el vertido entero (04 §2.1 b); nunca se abre ni se suelta.</description></item>
    /// <item><description>Validación: exactamente una pareja con el mismo id (pares simétricos y distintos, los dos de tipo
    /// remolino; un Matapi nunca forma parte del par porque una cuia lleva una sola mecánica, propuesta b.2 M4); sin dosis
    /// ocultas en el nivel [P]; sin Piraña en el nivel [P: la «última cuia destino» no está definida con el Remolino y el
    /// calendario 1-200 no los combina]. El Barro en una cuia con mecánica ya lo rechaza la validación general.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class RemolinoMechanic : MechanicBase
    {
        /// <summary>Índice de la pareja de la cuia <paramref name="index"/> (la otra cuia de Remolino con su id), o −1.</summary>
        internal static int PartnerOf(in BoardSlice board, int index)
        {
            int pair = board[index].MechanicPairId;
            for (int j = 0; j < board.Count; j++)
            {
                if (j != index && board[j].Mechanic == MechanicType.Remolino && board[j].MechanicPairId == pair)
                {
                    return j;
                }
            }

            return -1;
        }

        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            int partner = PartnerOf(in board, target);
            if (partner < 0 || partner == source)
            {
                return TargetVerdict.Reject; // sin pareja, o Q-A: verter en la propia pareja es ilegal (R11)
            }

            return board[partner].IsFull || board.IsSealed(partner) ? TargetVerdict.Reject : TargetVerdict.Accept;
        }

        public override int ReceiveSpace(in BoardSlice board, int target)
        {
            return board[PartnerOf(in board, target)].Space;
        }

        public override int ReceiverOf(in BoardSlice board, int target)
        {
            return PartnerOf(in board, target);
        }

        public override int Receive(in BoardSlice board, int target, int color, int amount)
        {
            int partner = PartnerOf(in board, target);
            board.Set(partner, board[partner].InsertAtBottom(color, amount));
            return partner;
        }

        public override bool ChangesOnReceive(Cuia self)
        {
            return true;
        }

        public override bool ExemptAsSource(Cuia self)
        {
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            int pair = state[index].MechanicPairId;
            int partners = 0;
            for (int j = 0; j < state.Count; j++)
            {
                if (j != index && state[j].Mechanic == MechanicType.Remolino && state[j].MechanicPairId == pair)
                {
                    partners++;
                }
            }

            if (partners != 1)
            {
                return "02 §9.1 M4: el Remolino es un par de dos cuias (hay " + (partners + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " con su id)";
            }

            if (state.HasHidden)
            {
                return "un nivel con Remolino no tiene dosis ocultas [P]";
            }

            for (int j = 0; j < state.Count; j++)
            {
                if (state[j].Mechanic == MechanicType.Pirana)
                {
                    return "un nivel con Remolino no tiene Piraña [P: la última cuia destino no está definida con el Remolino]";
                }
            }

            return null;
        }
    }
}
