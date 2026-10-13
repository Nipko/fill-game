#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Su cuia chica (M13, la cuia de la Tinaja; D-019; propuesta <c>docs/research/cultura-ribera/propuesta-tinaja.md</c>, b.2
    /// reglas 5-8 y c.1): Cuia chica de 2 que cuelga del jirau junto a la tinaja (<see cref="TinajaMechanic"/>) y es el único
    /// camino para sacar su fruta.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Regla 5: mientras cuelga solo recibe de su tinaja, de cualquier fruta (Q-Ti2 A), mientras tenga hueco:
    /// dos dosis en capas, que no se mezclan; lo último que se saca es lo primero que se juega. Desde cualquier otra cuia es
    /// ilegal (R11).</description></item>
    /// <item><description>Regla 6: como origen sigue la regla normal (vierte su cima en cualquier destino legal, nunca en la
    /// tinaja). Nunca sella: cap 2 &lt; <c>U_c</c> (R5).</description></item>
    /// <item><description><c>futil</c>: exenta como origen mientras cuelga (como el Remolino en Q-B): con [x, x] llena, vaciarla
    /// en una vacía de 2 o de 5 es justo lo que deja volver a sacar. Sin ciclos de vertidos exentos: Φ = 2 · (dosis en la
    /// tinaja) + (dosis en su cuia) baja con cada uno.</description></item>
    /// <item><description>Regla 8, R10 (f): en cuanto la tinaja queda vacía, baja del jirau y pasa a ser una Cuia chica normal
    /// (<see cref="Cuia.WithoutMechanic"/>, principio de 02 R16; eventos <see cref="PourEvents.CucharonReleasedMask"/> y
    /// <see cref="PourEvents.ReleasedMask"/>). Depende solo del recuento visible («×n» llega a 0), nunca del color de una
    /// oculta: ocurre en el mismo vertido en todas las realidades (excepción a Q14 A de Q-Ti1). Marea restaura el estado
    /// anterior entero y la vuelve a colgar.</description></item>
    /// <item><description>Validación: capacidad 2 (<see cref="LevelValidation.CucharonCapacity"/>), vacía al empezar, una sola
    /// y contigua a la tinaja (índice ± 1; que estén en la misma fila lo comprueban el layout y level-qa).</description></item>
    /// </list>
    /// Encuentra su tinaja por el tipo (una por nivel; sin id de pareja), en O(T) sin asignaciones y solo en (f).
    /// </remarks>
    internal sealed class CucharonMechanic : MechanicBase
    {
        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            return board[source].Mechanic == MechanicType.Tinaja && !board[target].IsFull ? TargetVerdict.Accept : TargetVerdict.Reject;
        }

        public override bool ExemptAsSource(Cuia self)
        {
            return true;
        }

        public override bool CheckCondition(in BoardSlice board, int index, ref PourEvents events)
        {
            for (int j = 0; j < board.Count; j++)
            {
                Cuia cuia = board[j];
                if (cuia.Mechanic == MechanicType.Tinaja && !cuia.IsEmpty)
                {
                    return false; // la tinaja aún tiene dosis: sigue colgada
                }
            }

            Cuia self = board[index];
            board.Set(index, self.WithoutMechanic());
            events.RecordReleased(index, MechanicType.Cucharon);
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia cuia = state[index];
            if (cuia.Capacity != LevelValidation.CucharonCapacity)
            {
                return "M13 (regla 1): la cuia de la tinaja es una Cuia chica de " + Num(LevelValidation.CucharonCapacity);
            }

            if (!cuia.IsEmpty)
            {
                return "M13 (regla 1): la cuia de la tinaja empieza vacía";
            }

            int tinajas = 0;
            bool contiguous = false;
            for (int j = 0; j < state.Count; j++)
            {
                MechanicType type = state[j].Mechanic;
                if (j != index && type == MechanicType.Cucharon)
                {
                    return "M13 (regla 1): una sola cuia de tinaja por nivel (otra en la cuia " + Num(j) + ")";
                }

                if (type == MechanicType.Tinaja)
                {
                    tinajas++;
                    contiguous |= j == index - 1 || j == index + 1;
                }
            }

            if (tinajas != 1)
            {
                return "M13 (regla 1): la cuia chica cuelga de una tinaja, una sola por nivel (hay " + Num(tinajas) + ")";
            }

            return contiguous ? null : "M13 (regla 1): la cuia chica va en la ranura contigua a la tinaja";
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
