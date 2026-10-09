#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Presentation
{
    /// <summary>
    /// Consultas de la vista sobre dos tableros del modelo (arquitectura del paso 1, §3: buscar una pareja o comparar estados
    /// es una consulta para pintar, no una regla; las reglas viven en el núcleo). Estático y sin asignaciones.
    /// </summary>
    public static class BoardDiff
    {
        /// <summary>
        /// Cuias, salvo <paramref name="source"/> y <paramref name="receiver"/>, que cambiaron entre <paramref name="before"/> y
        /// <paramref name="after"/>: bit i si cambió su <see cref="Cuia.Packed"/> (contenido, ocultas, mecánica o contador) o
        /// su sellado. Cubre la gota de la Lluvia (02 §9.1 M6), la cueva de la Piraña tras un mordisco (M7; la víctima es
        /// siempre el receptor), la Liana que se soltó (M3), el sellado por el punto fijo (f′) del anexo B y la pareja del
        /// Remolino cuando el receptor es la cuia tocada. Con −1 en los dos índices, todas las cuias que cambiaron (Marea).
        /// Una cuia que solo existe en <paramref name="after"/> cuenta como cambiada.
        /// </summary>
        /// <exception cref="ArgumentNullException">Falta alguno de los dos tableros.</exception>
        public static int ChangedOthers(BoardState before, BoardState after, int source, int receiver)
        {
            if (before == null)
            {
                throw new ArgumentNullException(nameof(before));
            }

            if (after == null)
            {
                throw new ArgumentNullException(nameof(after));
            }

            int mask = 0;
            for (int i = 0; i < after.Count; i++)
            {
                if (i == source || i == receiver)
                {
                    continue;
                }

                if (i >= before.Count || before[i].Packed != after[i].Packed || before.IsSealed(i) != after.IsSealed(i))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        /// <summary>
        /// Pareja del Remolino <paramref name="index"/> en <paramref name="board"/> (02 §9.1 M4): la otra cuia de Remolino con
        /// el mismo <see cref="Cuia.MechanicPairId"/>, o −1 si <paramref name="index"/> no es un Remolino, está fuera del
        /// tablero o no tiene pareja.
        /// </summary>
        /// <exception cref="ArgumentNullException">Falta el tablero.</exception>
        public static int RemolinoPartner(BoardState board, int index)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if ((uint)index >= (uint)board.Count)
            {
                return -1;
            }

            Cuia self = board[index];
            if (self.Mechanic != MechanicType.Remolino)
            {
                return -1;
            }

            int pair = self.MechanicPairId;
            for (int j = 0; j < board.Count; j++)
            {
                if (j != index)
                {
                    Cuia other = board[j];
                    if (other.Mechanic == MechanicType.Remolino && other.MechanicPairId == pair)
                    {
                        return j;
                    }
                }
            }

            return -1;
        }
    }
}
