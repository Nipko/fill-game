#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Flor de vitória-régia (variante de la Liana con fruta de la 1.0, debut 403; D-007, propuesta de D-007 b.3; 02 §1.2,
    /// R10 (d) y §9.1; firma de <c>pack_schema</c> 3 (D-023, punto 5): <c>{"type":"flor","key":c1,"key2":c2}</c> con c1 ≠ c2): una
    /// flor cerrada sobre la cuia, con dos llaves en lógica Y.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Cerrada, no es origen ni destino (02 §1.2: «la Flor cerrada, como la Liana»): un toque es un vertido
    /// ilegal (R11). Su contenido se ve (Q17 A).</description></item>
    /// <item><description>Se abre en la etapa (d) de R10 (liberaciones por sellado) cuando existen otras cuias selladas de las dos
    /// frutas, c1 y c2 (b.3: «se abre cuando existen cuias selladas de las dos frutas»). Con una sola sigue cerrada (blanca y
    /// entreabierta en la presentación). Se deduce del estado, sin bandera: el vertido que sella la segunda fruta (o la gota que
    /// la sella, por el punto fijo (f′)) la abre en el mismo vertido, y la cuia pasa a ser normal
    /// (<see cref="MechanicType.None"/>; 02 R16 para D-007; evento <see cref="PourEvents.ReleasedMask"/>). Un sellado solo se
    /// deshace con Marea, que restaura el estado anterior entero y la vuelve a cerrar; Volver a la orilla también.</description></item>
    /// <item><description>No impide la victoria: vacía vale como cualquier cuia (R12); con dosis, se abre antes de poder sellar.</description></item>
    /// <item><description>Validación (03 §17 #6; b.3, como la Liana con fruta, regla 4): las dos llaves son frutas distintas
    /// (c1 ≠ c2, la firma de D-023, punto 5), están en el tablero y pueden sellar fuera de la flor a la vez (ninguna dosis de
    /// ellas dentro y una cuia de capacidad <c>U_c</c> para cada una, dos si comparten <c>U_c</c>). Sin dosis ocultas en el
    /// nivel ni Tinaja: es un disparador (02 T1 y §9.2 regla 7; Q14 A), y lo rechaza <see cref="LevelValidation"/>.</description></item>
    /// <item><description><b>Bloqueo provisional</b> (C1-9, pregunta 24 b de GDD §6.3): con capacidades mixtas la propiedad P1
    /// de R14 falla, como con la Liana (un fútil libera la casa de una de sus llaves:
    /// <c>DotnetOnly/FutilePropertyTests.P1_Flor_MixedCapacities_OpenR16Counterexample</c>), así que un nivel con Flor exige
    /// capacidades uniformes (todas 4 y <c>U_c</c> = 4) mientras dure el bloqueo de la Liana.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class FlorMechanic : MechanicBase
    {
        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            return TargetVerdict.Reject;
        }

        public override bool ReleasedBySeal(in BoardSlice board, int index)
        {
            Cuia flor = board[index];
            int key = flor.MechanicKey;
            int key2 = flor.MechanicKey2;
            bool first = false;
            bool second = false;
            for (int j = 0; j < board.Count; j++)
            {
                int top = board[j].TopColor;
                if ((top != key && top != key2) || j == index || !board.IsSealed(j))
                {
                    continue;
                }

                if (top == key)
                {
                    first = true;
                }
                else
                {
                    second = true;
                }

                if (first && second)
                {
                    return true;
                }
            }

            return false;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia flor = state[index];
            int key = flor.MechanicKey;
            int key2 = flor.MechanicKey2;
            if (key == key2)
            {
                return "D-023, punto 5 (firma de pack_schema 3): las dos llaves de la Flor son frutas distintas (key ≠ key2)";
            }

            if (state.UnitsOf(key) == 0 || state.UnitsOf(key2) == 0)
            {
                return "03 §17 #6: las dos llaves de la Flor están en el tablero";
            }

            for (int k = 0; k < flor.Count; k++)
            {
                int color = flor.ColorAt(k);
                if (color == key || color == key2)
                {
                    return "propuesta de D-007 b.3 (como la Liana, regla 4): las frutas de la Flor no tienen dosis dentro de ella";
                }
            }

            int units = state.UnitsOf(key);
            int units2 = state.UnitsOf(key2);
            int homes = 0;
            int homes2 = 0;
            for (int j = 0; j < state.Count; j++)
            {
                if (j == index)
                {
                    continue;
                }

                int capacity = state[j].Capacity;
                if (capacity == units)
                {
                    homes++;
                }

                if (capacity == units2)
                {
                    homes2++;
                }
            }

            bool canSeal = units == units2 ? homes >= 2 : homes >= 1 && homes2 >= 1;
            if (!canSeal)
            {
                return "propuesta de D-007 b.3 (como la Liana, regla 4): las dos frutas de la Flor tienen que poder sellar fuera de ella a la vez";
            }

            // Después de las reglas de diseño, que son definitivas: el bloqueo es provisional (pregunta 24 b).
            return LianaMechanic.HasMixedCapacities(state)
                ? "bloqueo provisional (C1-9, pregunta 24 b): Flor con capacidades mixtas, P1 de R14 falla, como con la Liana; pendiente de la pregunta de 02 R16"
                : null;
        }
    }
}
