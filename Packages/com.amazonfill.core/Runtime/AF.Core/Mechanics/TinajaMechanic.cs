#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M13 Tinaja (D-019; propuesta <c>docs/research/cultura-ribera/propuesta-tinaja.md</c>, b.2 y c.1; debut 323 [P]): vasija
    /// de barro que guarda fruta oculta y solo la suelta, de una en una, por su cuia chica (<see cref="CucharonMechanic"/>).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>Capacidad 5 (<see cref="LevelValidation.TinajaCapacity"/>) con 3-5 dosis al empezar; todas ocultas
    /// salvo la boca (R7), con T4 en toda la columna y sin Barro. Una sola tinaja por nivel y como mucho 4 ocultas, con una
    /// constante propia (<see cref="LevelValidation.TinajaMaxHiddenUnits"/>), no con <c>lvl_murky_max_hidden_units</c>.</description></item>
    /// <item><description>Nunca recibe (<c>bloqueada_destino</c> permanente), tampoco desde su cuia: rechazo sin texto
    /// (R11).</description></item>
    /// <item><description>Solo vierte en su cuia (<c>solo_vierte_en(A)</c>, <see cref="AllowsTarget"/>): es el filtro de origen
    /// que <see cref="BoardRules"/> evalúa antes que la mecánica del destino y que el Barro expuesto.</description></item>
    /// <item><description>Cantidad 1 siempre, en <c>run</c> y en <c>single</c>: por T4 la racha visible de la boca es 1, también
    /// tras una Luciérnaga. Nunca sale una dosis que no se haya visto.</description></item>
    /// <item><description>Exenta como origen en <c>futil</c> (<c>no_puede_recibir(A)</c> del anexo B): sacar la última fruta
    /// visible a su cuia vacía sería «monocolor → vacía con cap ≠ U_c» y R13 declararía Aguas estancadas en un nivel
    /// ganable (R14).</description></item>
    /// <item><description>Vacía, queda inerte en su ranura: no es origen (está vacía) ni destino; cuenta como vacía para R12.
    /// Nunca sella (T4) y nunca se suelta: conserva su tipo, así que la clave canónica la distingue de una Cuia alta
    /// vacía.</description></item>
    /// </list>
    /// No tiene estado: lo revelado vive en la máscara de ocultos de la cuia y en la de la partida (Aguas turbias, R7, T6, I5).
    /// </remarks>
    internal sealed class TinajaMechanic : MechanicBase
    {
        public override bool AllowsTarget(in BoardSlice board, int source, int target)
        {
            // Una tinaja por nivel: su cuia es la única con el tipo Cucharon mientras cuelga del jirau (02 R11 si no).
            return board[target].Mechanic == MechanicType.Cucharon;
        }

        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            return TargetVerdict.Reject; // regla 2: la tinaja nunca recibe, tampoco desde su cuia
        }

        public override bool ExemptAsSource(Cuia self)
        {
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia tinaja = state[index];
            if (tinaja.Capacity != LevelValidation.TinajaCapacity)
            {
                return "M13 (regla 1): la tinaja es de capacidad " + Num(LevelValidation.TinajaCapacity);
            }

            int count = tinaja.Count;
            if (count < LevelValidation.TinajaMinDoses || count > LevelValidation.TinajaMaxDoses)
            {
                return "M13 (regla 1): la tinaja empieza con " + Num(LevelValidation.TinajaMinDoses) + "-" + Num(LevelValidation.TinajaMaxDoses) + " dosis";
            }

            if (tinaja.HiddenMask != (1 << (count - 1)) - 1)
            {
                return "M13 (regla 1): todas las dosis de la tinaja están ocultas salvo la boca";
            }

            if (tinaja.HiddenCount > LevelValidation.TinajaMaxHiddenUnits)
            {
                return "M13 (regla 1): como mucho " + Num(LevelValidation.TinajaMaxHiddenUnits) + " dosis ocultas en la tinaja (≤ 24 realidades)";
            }

            for (int k = 0; k < count; k++)
            {
                if (tinaja.ColorAt(k) == Cuia.MudColor)
                {
                    return "M13 (regla 1): sin Barro en la tinaja";
                }

                if (k + 1 < count && tinaja.ColorAt(k) == tinaja.ColorAt(k + 1))
                {
                    return "M13 (regla 1): T4 en toda la columna, la dosis " + Num(k) + " repite la fruta de la de encima";
                }
            }

            int cucharones = 0;
            for (int j = 0; j < state.Count; j++)
            {
                MechanicType type = state[j].Mechanic;
                if (j != index && type == MechanicType.Tinaja)
                {
                    return "M13 (regla 1): una sola tinaja por nivel (otra en la cuia " + Num(j) + ")";
                }

                if (type == MechanicType.Cucharon)
                {
                    cucharones++;
                }

                if (type == MechanicType.Liana || type == MechanicType.Remolino || type == MechanicType.Lluvia || type == MechanicType.Pirana)
                {
                    return "M13 (combinaciones): la tinaja no se combina con " + MechanicTypes.ToKey(type) + " (cuia " + Num(j) + "; Q14 A)";
                }
            }

            return cucharones == 1 ? null : "M13 (regla 1): la tinaja lleva su cuia chica, una sola (hay " + Num(cucharones) + ")";
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
