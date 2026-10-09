#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M3 Liana (D-007: debuta con fruta en el 83; la de cuenta pasa a variante en el 243; propuesta b.2 M3, reglas 1-5, y
    /// b.3 «Liana de cuenta»; 02 §9.1 M3; 03 §9: se deduce del estado): cuia atada por una liana.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Atada, no es origen ni destino (un toque es un vertido ilegal, R11). Su contenido se ve (Q17 A).</description></item>
    /// <item><description>Con fruta (<c>key</c> = c): se suelta en cuanto existe otra cuia sellada de c.</description></item>
    /// <item><description>De cuenta (<c>n</c> = 1-2): se suelta cuando hay <c>n</c> cuias selladas en el tablero.</description></item>
    /// <item><description>Se suelta en la etapa (d) de R10 (anexo B), así que el vertido que sella c (o la gota que sella,
    /// por el punto fijo (f′)) la suelta en el mismo vertido; la cuia pasa a ser normal (<see cref="MechanicType.None"/>).
    /// Un sellado solo se deshace con Marea, que restaura el estado anterior y la vuelve a atar; Volver a la orilla
    /// también.</description></item>
    /// <item><description>Validación: un nivel con Liana no tiene dosis ocultas (Q14 A); la llave está en el tablero y puede
    /// sellar fuera de la Liana (no tiene dosis dentro y hay otra cuia con cap = U_c); la de cuenta tiene al menos
    /// <c>n</c> colores que pueden sellar fuera.</description></item>
    /// <item><description><b>Bloqueo provisional</b> (C1-9): con capacidades mixtas la propiedad P1 de R14 falla (hay estados
    /// atascados con solución: hace falta un vertido que 02 §1.4 llama fútil para liberar una casa), así que un nivel con
    /// Liana exige capacidades uniformes (todas 4 y <c>U_c</c> = 4) hasta que se decida la pregunta de 02 R16. Ver
    /// <c>DotnetOnly/FutilePropertyTests.P1_Liana_MixedCapacities_OpenR16Counterexample</c>.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class LianaMechanic : MechanicBase
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
            Cuia liana = board[index];
            int count = liana.MechanicN;
            if (count == 0)
            {
                int key = liana.MechanicKey;
                for (int j = 0; j < board.Count; j++)
                {
                    if (j != index && board[j].TopColor == key && board.IsSealed(j))
                    {
                        return true;
                    }
                }

                return false;
            }

            int sealedCuias = 0;
            for (int j = 0; j < board.Count; j++)
            {
                if (j != index && board.IsSealed(j))
                {
                    sealedCuias++;
                }
            }

            return sealedCuias >= count;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            if (state.HasHidden)
            {
                return "Q14 A de D-007: un nivel con Liana no tiene dosis ocultas";
            }

            if (HasMixedCapacities(state))
            {
                return "bloqueo provisional (C1-9): Liana con capacidades mixtas, P1 de R14 falla; pendiente de la pregunta de 02 R16";
            }

            Cuia liana = state[index];
            if (liana.MechanicN == 0)
            {
                int key = liana.MechanicKey;
                if (state.UnitsOf(key) == 0)
                {
                    return "D-007 (Liana, regla 4): la fruta de la Liana no está en el tablero";
                }

                if (!CanSealOutside(state, index, key))
                {
                    return "D-007 (Liana, regla 4): la fruta de la Liana tiene que poder sellar fuera de ella";
                }

                return null;
            }

            int colors = 0;
            for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
            {
                if (state.UnitsOf(c) > 0 && CanSealOutside(state, index, c))
                {
                    colors++;
                }
            }

            return colors >= liana.MechanicN ? null : "D-007 (Liana de cuenta): no hay n colores que puedan sellar fuera de ella";
        }

        /// <summary>
        /// Alguna cuia no es de capacidad 4 o algún color no tiene 4 dosis (02 §8). El Barro (15) no es un color (02 §9.1 M5):
        /// sus dosis no cuentan (corregido en C3-3: un nivel con Barro y Liana, como el 138 del calendario d.4, se rechazaba).
        /// </summary>
        private static bool HasMixedCapacities(BoardState state)
        {
            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].Capacity != Cuia.DefaultCapacity)
                {
                    return true;
                }
            }

            for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
            {
                int units = state.UnitsOf(c);
                if (units != 0 && units != Cuia.DefaultCapacity)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Ninguna dosis de <paramref name="color"/> está atada en la Liana y hay otra cuia con cap = U_c.</summary>
        private static bool CanSealOutside(BoardState state, int index, int color)
        {
            Cuia liana = state[index];
            for (int k = 0; k < liana.Count; k++)
            {
                if (liana.ColorAt(k) == color)
                {
                    return false;
                }
            }

            for (int j = 0; j < state.Count; j++)
            {
                if (j != index && state[j].Capacity == state.UnitsOf(color))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
