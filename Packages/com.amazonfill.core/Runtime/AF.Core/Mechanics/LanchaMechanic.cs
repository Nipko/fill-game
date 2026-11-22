#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M11 Lancha (1.1; D-068, P8 A [P]; 02 §9.1 M11; 03 §9; ficha b.2 M11 de la propuesta de D-007): la cuia de carga trae fruta a
    /// mitad del nivel. Es la cortina de Magic Sort.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Antes de atracar</b> (regla 2) no es origen ni destino (R11): no cuenta como vertido posible para R13
    /// ni para <c>DeadEndProbe</c>, ni para el solver o el bot. Su contenido se ve.</description></item>
    /// <item><description><b>Atraque</b> (R10, etapa f): en cuanto el tablero tiene <c>k</c> o más cuias vacías, atraca y queda
    /// como una cuia normal hasta el final del nivel (<see cref="Cuia.WithoutMechanic"/>; eventos
    /// <see cref="PourEvents.LanchaDockedMask"/> y <see cref="PourEvents.ReleasedMask"/>). «Atracada» es 1 bit de estado (el código
    /// de la mecánica en <see cref="Cuia.Packed"/>, que entra en la clave canónica): no se deduce del contenido, porque tras atracar
    /// sigue atracada aunque se llenen las vacías. Marea restaura el estado anterior entero y la vuelve a alejar.</description></item>
    /// <item><description><b>Qué cuias cuentan como vacías para <c>k</c></b> [P] (02 §9.1 M11, regla 1, lo deja al núcleo): toda
    /// cuia sin dosis, de cualquier capacidad y con o sin mecánica (la Cuia chica, una Hoja puesta, la Cuia extra y la de rescate
    /// incluidas). Es lo que ve el jugador: «cuando tengas 3 cuias vacías».</description></item>
    /// <item><description><b>Validación</b> (03 §17 #6; regla 1 y 03 §17, QA-LANCHA): una lancha por nivel, con 1-3 dosis (nunca llena;
    /// sin ocultas ni Barro, que rechazan las reglas de Aguas turbias y del Barro de <see cref="LevelValidation"/>), <c>k</c> ≥
    /// vacíos iniciales + 1 (si no, atracaría en el primer vertido) y <c>k</c> ≤ T − 1. Que atraque en la solución lo comprueba la
    /// verificación con A* del generador y de level-qa. Nunca con Aguas turbias ni con la Tinaja (es un disparador, 02 T1; lo
    /// rechaza <see cref="LevelValidation"/>); con la Liana solo desde el 401, que depende del número de nivel y lo comprueba
    /// level-qa.</description></item>
    /// </list>
    /// </remarks>
    internal sealed class LanchaMechanic : MechanicBase
    {
        /// <summary>Dosis mínimas de la carga (02 §9.1 M11, regla 1).</summary>
        public const int MinCargo = 1;

        /// <summary>Dosis máximas de la carga (02 §9.1 M11, regla 1).</summary>
        public const int MaxCargo = 3;

        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            return TargetVerdict.Reject;
        }

        public override bool CheckCondition(in BoardSlice board, int index, ref PourEvents events)
        {
            Cuia self = board[index];
            if (EmptyCuias(in board, index) < self.MechanicK)
            {
                return false;
            }

            board.Set(index, self.WithoutMechanic());
            events.RecordReleased(index, MechanicType.Lancha);
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia self = state[index];
            for (int j = 0; j < state.Count; j++)
            {
                if (j != index && state[j].Mechanic == MechanicType.Lancha)
                {
                    return "02 §9.1 M11 (regla 1): una sola lancha por nivel (otra en la cuia " + Num(j) + ")";
                }
            }

            if (self.Count < MinCargo || self.Count > MaxCargo || self.IsFull)
            {
                return "02 §9.1 M11 (regla 1): la carga lleva de 1 a 3 dosis y no está llena";
            }

            int empties = 0;
            for (int j = 0; j < state.Count; j++)
            {
                if (j != index && state[j].IsEmpty)
                {
                    empties++;
                }
            }

            int k = self.MechanicK;
            if (k < empties + 1)
            {
                return "02 §9.1 M11 (regla 1): k = " + Num(k) + " tiene que ser al menos los vacíos iniciales + 1 (" + Num(empties + 1) + ")";
            }

            return k > state.Count - 1 ? "02 §9.1 M11: k = " + Num(k) + " es mayor que las otras cuias del tablero" : null;
        }

        /// <summary>Cuias sin dosis del tablero, sin contar la propia lancha (que nunca está vacía).</summary>
        internal static int EmptyCuias(in BoardSlice board, int lancha)
        {
            int empties = 0;
            for (int j = 0; j < board.Count; j++)
            {
                if (j != lancha && board[j].IsEmpty)
                {
                    empties++;
                }
            }

            return empties;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
