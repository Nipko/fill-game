#nullable enable
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Hoja atada (variante de M1 de la 1.0, debut 283; D-007, propuesta de D-007 b.3; 02 §1.2 y §1.4; firma de
    /// <c>pack_schema</c> 3, pregunta 8: <c>{"type":"hoja_atada","key":c}</c>): hoja pequeña atada al cuello de la cuia, un filtro
    /// de color <b>persistente</b> de la fruta <c>key</c>.
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
    /// <item><description><b>Es la casa de su fruta</b> [P] (K3a; 02 R16, P3): su capacidad es la <c>U_c</c> de su llave, como
    /// la botella atada de b.3, que «conserva la cuerda tras sellar». Con otra capacidad (una Cuia chica atada a una fruta de
    /// 4, por ejemplo) la exención de destino de <c>futil</c> rompe P3: la fruta va de su casa a la hoja atada (útil: la hoja
    /// atada cambia al recibir) y vuelve a su casa (útil: la casa correcta), y el tablero vuelve al mismo estado canónico
    /// (<c>FutilePropertyTests.P3_HojaAtada_NotTheHomeOfItsFruit_ARoundTripReturnsToTheSameState</c>). Con la regla, P1-P3 se
    /// cumplen en <c>run</c> y <c>single</c> con capacidades uniformes y mixtas.</description></item>
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
                return "[P] (02 R16, P3): la Hoja atada es la casa de su fruta, así que su capacidad es la U_c de su llave ("
                    + state.UnitsOf(key).ToString(CultureInfo.InvariantCulture) + ", no "
                    + atada.Capacity.ToString(CultureInfo.InvariantCulture) + ")";
            }

            return null;
        }
    }
}
