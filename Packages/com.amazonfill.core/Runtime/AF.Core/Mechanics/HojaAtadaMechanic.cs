#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Hoja atada (variante de M1 de la 1.0, debut 283; D-007, propuesta de D-007 b.3; 02 §1.2 y §1.4; firma de
    /// <c>pack_schema</c> 3, D-023, punto 5: <c>{"type":"hoja_atada","key":c}</c>; reglas de validación, D-024): hoja pequeña
    /// atada al cuello de la cuia, un filtro de color <b>persistente</b> de la fruta <c>key</c>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Como destino solo recibe su fruta: vacía o con ella en la cima (02 §1.2: «la Hoja atada solo recibe su
    /// fruta (vacía o con ella en la cima)»), con hueco; en <c>run</c> entra el tramo de <c>key</c> que cabe. Con otra fruta en
    /// la cima (si empezó con dosis encima de su fondo) no recibe nada hasta que salgan.</description></item>
    /// <item><description><b>Sí es origen</b> (02 §1.2: «la Hoja atada sí es origen») y <b>nunca se suelta</b>: ni al recibir ni
    /// al sellar; sellada sigue atada (b.3: «conserva la cuerda tras sellar»). No tiene estado propio: el tipo y la llave
    /// bastan.</description></item>
    /// <item><description>Como destino nunca es fútil (02 §1.4, <c>cambia_al_recibir</c> del anexo B). Como origen no está
    /// exenta: la regla normal de <c>futil</c> la trata como cualquier cuia.</description></item>
    /// <item><description>No impide la victoria: vacía o sellada vale como cualquier cuia (R12).</description></item>
    /// <item><description>Validación (03 §17 #6; b.3): su llave está en el tablero y, si empieza con dosis, la del fondo es de su
    /// fruta. Sin Barro ni ocultas (las reglas generales de <see cref="LevelValidation"/>: ninguna cuia con mecánica las
    /// lleva).</description></item>
    /// <item><description><b>Es la casa de su fruta</b> (D-024, punto 1, que ratifica el [P] de K3a; 02 R16, P3; precedente del
    /// Matapi de 02 §9.1): su capacidad es la <c>U_c</c> de su llave, como la botella atada de b.3, que «conserva la cuerda tras
    /// sellar». Con otra capacidad (una Cuia chica atada a una fruta de 4, por ejemplo) la exención de destino de <c>futil</c>
    /// rompe P3: la fruta va de su casa a la hoja atada (útil: la hoja atada cambia al recibir) y vuelve a su casa (útil: la
    /// casa correcta), y el tablero vuelve al mismo estado canónico
    /// (<c>FutilePropertyTests.P3_HojaAtada_NotTheHomeOfItsFruit_ARoundTripReturnsToTheSameState</c>).</description></item>
    /// <item><description><b>Como mucho una Hoja atada por fruta</b> (D-024, punto 2; 02 R16, P3, y 02 §8). Dos de la misma
    /// fruta, las dos de capacidad <c>U_c</c>, rompían dos cosas a la vez:
    /// <list type="bullet">
    /// <item><description>P3: con la fruta en una y la otra vacía, el vertido de una a otra es útil en los dos sentidos y, en
    /// <c>run</c>, uno solo ya deja el mismo estado canónico, así que <c>atascado(S)</c> no se cumplía mientras existiera el par
    /// (<c>FutilePropertyTests.P3_HojaAtada_TwoOfTheSameFruit_ARoundTripReturnsToTheSameState_D024RejectsIt</c> y
    /// <c>P3_TwoHojasAtadasOfTheSameFruit_FailsOnGeneratedBoards_AndD024RejectsThem</c>);</description></item>
    /// <item><description>la cuenta de capacidades de 02 §8 en <see cref="LevelValidation"/>, que, contada solo por capacidad,
    /// tomaba la segunda por casa de otra fruta de la misma <c>U_c</c>, aunque solo recibe la suya: dejaba pasar tableros
    /// irresolubles (<c>FutilePropertyTests.TwoHojasAtadasOfTheSameFruit_PassTheCapacityOnlyCount_Unsolvable_02_8AndD024RejectIt</c>).
    /// </description></item>
    /// </list>
    /// La cuenta de 02 §8 ya no depende de esta regla: <see cref="LevelValidation"/> cuenta cada cuia de una sola fruta (la Hoja
    /// atada y el Matapi con dosis) solo como casa de su fruta, y una sola por fruta, y casa las demás frutas de esa <c>U_c</c>
    /// con las cuias que admiten cualquier fruta. Así es exacta también cuando un Matapi con dosis de la fruta comparte la llave
    /// con una Hoja atada, que entonces no es la casa de nadie (revisión de FIXK:
    /// <c>FutilePropertyTests.MatapiWithDosesAndHojaAtadaOfTheSameFruit_PassTheCapacityOnlyCount_Unsolvable_02_8RejectsIt</c> y
    /// <c>SingleFruitCuias_TheCountOf02_8IsExact_EveryValidBoardHasAHomeForEachFruit</c>). Con un ejemplar por fruta, P1-P3 se
    /// cumplen en <c>run</c> y <c>single</c> con capacidades uniformes y mixtas (<c>P1_P2_P3_HoldWithHojaAtada</c> y
    /// <c>P1_P2_P3_HoldWithTwoHojasAtadasOfDifferentFruits</c>). Como la Tinaja, la regla de «una por» la comprueba la propia
    /// mecánica: el informe sale en la primera de las dos y nombra la otra (si la cuenta de 02 §8, que va antes, no rechazó
    /// ya el tablero).</description></item>
    /// </list>
    /// </remarks>
    internal sealed class HojaAtadaMechanic : MechanicBase
    {
        public override TargetVerdict CheckTarget(in BoardSlice board, int source, int target)
        {
            // Solo su fruta; el resto (no llena, y vacía o con la misma cima) lo decide la regla normal.
            return board[source].TopColor == board[target].MechanicKey ? TargetVerdict.Normal : TargetVerdict.Reject;
        }

        public override bool ChangesOnReceive(Cuia self)
        {
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia atada = state[index];
            int key = atada.MechanicKey;
            if (state.UnitsOf(key) == 0)
            {
                return "03 §17 #6: la llave de la Hoja atada no está en el tablero";
            }

            if (!atada.IsEmpty && atada.ColorAt(0) != key)
            {
                return "propuesta de D-007 b.3: la Hoja atada puede empezar con dosis, pero la del fondo es de su fruta";
            }

            if (atada.Capacity != state.UnitsOf(key))
            {
                return "D-024 (02 R16, P3): la Hoja atada es la casa de su fruta, así que su capacidad es la U_c de su llave ("
                    + Num(state.UnitsOf(key)) + ", no " + Num(atada.Capacity) + ")";
            }

            for (int j = 0; j < state.Count; j++)
            {
                Cuia other = state[j];
                if (j != index && other.Mechanic == MechanicType.HojaAtada && other.MechanicKey == key)
                {
                    return "D-024 (02 R16, P3; 02 §8): como mucho una Hoja atada por fruta; la cuia " + Num(j)
                        + " también está atada a la fruta " + Num(key);
                }
            }

            return null;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
