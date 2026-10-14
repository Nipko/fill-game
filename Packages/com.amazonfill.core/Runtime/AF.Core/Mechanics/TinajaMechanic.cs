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
    /// constante propia (<see cref="LevelValidation.TinajaMaxHiddenUnits"/>), no con <c>lvl_murky_max_hidden_units</c>. T4,
    /// el Barro y el tope los comprueba solo <see cref="LevelValidation"/>; <see cref="ValidateInitial"/>, el resto.</description></item>
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

            // El tope de ocultas, T4 en toda la columna y «sin Barro» (b.2 regla 1) no se comprueban aquí: las comprueba solo
            // LevelValidation, antes que ValidateInitial (TinajaValidationTests fija sus motivos). El tope no se puede superar
            // tras las tres comprobaciones de arriba (capacidad 5, como mucho 5 dosis y todas ocultas menos la boca: ≤ 4), y
            // MurkyError lo mira antes. T4 lo comprueba MurkyError en cada dosis oculta, que con la máscara de arriba son todas
            // menos la boca. El Barro lo rechaza siempre MudError, que va antes que MurkyError (solo en una cuia de 4, en la capa
            // 2 o 3 y nunca en una cuia con mecánica): en la cuia de 5 de la tinaja, «el Barro va en una cuia de 4».

            // Las combinaciones prohibidas de b.2 (Liana, Remolino, Lluvia y Piraña; «LevelValidation ya lo prohíbe con
            // ocultos») no se comprueban aquí: si se llega a este punto, la máscara de arriba garantiza ≥ 2 ocultas, así que
            // LevelValidation.MurkyError, que va antes que ValidateInitial, ya las vio y rechazó la combinación. Es su única
            // comprobación (TinajaValidationTests fija su motivo).
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
            }

            return cucharones == 1 ? null : "M13 (regla 1): la tinaja lleva su cuia chica, una sola (hay " + Num(cucharones) + ")";
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
