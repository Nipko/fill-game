#nullable enable
using System.Globalization;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.Core.Board
{
    /// <summary>
    /// Reglas estructurales comunes del tablero inicial de un nivel de campaña (03 §17 #5 y #6; 02 §8; 02 §9.2 regla 1),
    /// con la misma implementación de las reglas que el cliente. La usan <c>tools/level-generator</c>,
    /// <c>tools/level-qa</c> y los tests; cada mecánica añade sus reglas propias (<c>IMechanic.ValidateInitial</c>).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>Como mucho 16 cuias (02 R1: 14 generadas, hasta 16 con Cuia extra y Cuia de rescate; el tope de 14
    /// generadas lo comprueba <c>tools/level-qa</c>, porque el alivio <c>plus_empty</c> añade una).</description></item>
    /// <item><description>Una mecánica por cuia: la representación solo admite un tipo, y las combinaciones prohibidas dentro
    /// de una cuia (Barro u ocultos en un Matapi…) las rechaza la mecánica.</description></item>
    /// <item><description><c>U_c</c> ∈ {4, 5, 6} para todo color (02 R3).</description></item>
    /// <item><description>Cada color puede sellar en una cuia propia con <c>cap = U_c</c>, y cada Cuia alta (5-6) llega con su
    /// fruta abundante (02 §8; 03 §3): para cada <c>v</c>, hay al menos tantas cuias de capacidad <c>v</c> como colores con
    /// <c>U_c = v</c>, y para <c>v</c> ∈ {5, 6} exactamente tantas.</description></item>
    /// <item><description>Ninguna cuia sellada al inicio (03 §17 #5).</description></item>
    /// <item><description>Barro (02 §9.1 M5): solo en la capa 2 o 3 (índice 1-2) de una cuia de 4; nunca en una cuia con
    /// mecánica (propuesta de D-007 b.2 M5: «nunca dentro de un Matapi»; D-007) ni como dosis oculta [P].</description></item>
    /// <item><description>Las reglas propias de cada mecánica (03 §17 #6).</description></item>
    /// <item><description><c>atascado(S0)</c> = false con la <c>futil</c> única (02 §9.2 regla 1).</description></item>
    /// </list>
    /// </remarks>
    public static class LevelValidation
    {
        /// <summary>Mínimo de <c>U_c</c> en campaña (02 R3).</summary>
        public const int MinUnits = 4;

        /// <summary>Máximo de <c>U_c</c> en campaña (02 R3, fruta abundante de una Cuia alta de 6).</summary>
        public const int MaxUnits = 6;

        /// <summary>
        /// Comprueba las reglas sobre el tablero inicial. Devuelve null si es válido o el motivo del primer rechazo, en el
        /// orden de la lista de la clase.
        /// </summary>
        /// <param name="state">Tablero inicial (normalmente de <see cref="BoardBuilder"/>).</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> con el que se comprueba <c>atascado(S0)</c>.</param>
        public static string? Validate(BoardState state, bool futileRuleEnabled = true)
        {
            if (state == null)
            {
                return "falta el tablero";
            }

            if (state.Count > BoardState.MaxCuias)
            {
                return "R1: más de 16 cuias";
            }

            int[] units = state.UnitsTable;
            var colorsWithUnits = new int[MaxUnits + 1];
            for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
            {
                // El Barro (15) no es un color (02 §9.1 M5): no tiene U_c ni casa donde sellar.
                int u = units[c];
                if (u == 0)
                {
                    continue;
                }

                if (u < MinUnits || u > MaxUnits)
                {
                    return "02 R3: U_c del color " + Num(c) + " es " + Num(u) + "; en campaña va de 4 a 6";
                }

                colorsWithUnits[u]++;
            }

            var cuiasWithCap = new int[Cuia.MaxCapacity + 1];
            for (int i = 0; i < state.Count; i++)
            {
                cuiasWithCap[state[i].Capacity]++;
            }

            for (int v = MinUnits; v <= MaxUnits; v++)
            {
                if (cuiasWithCap[v] < colorsWithUnits[v])
                {
                    return "02 §8: hay " + Num(colorsWithUnits[v]) + " colores con U_c = " + Num(v) + " y solo " + Num(cuiasWithCap[v]) + " cuias de capacidad " + Num(v) + " donde sellar";
                }

                if (v > Cuia.DefaultCapacity && cuiasWithCap[v] > colorsWithUnits[v])
                {
                    return "02 §8: cada Cuia alta de " + Num(v) + " llega con su fruta abundante (U_c = " + Num(v) + "); hay " + Num(cuiasWithCap[v]) + " cuias y " + Num(colorsWithUnits[v]) + " frutas";
                }
            }

            for (int i = 0; i < state.Count; i++)
            {
                if (state.IsSealed(i))
                {
                    return "03 §17 #5: la cuia " + Num(i) + " empieza sellada";
                }
            }

            if (state.IsWon())
            {
                return "03 §17 #5: el nivel empieza ganado (no hay dosis)";
            }

            string? mud = MudError(state);
            if (mud != null)
            {
                return mud;
            }

            for (int i = 0; i < state.Count; i++)
            {
                Cuia cuia = state[i];
                if (!cuia.HasMechanic)
                {
                    continue;
                }

                string? error = MechanicTable.Of(cuia).ValidateInitial(state, i);
                if (error != null)
                {
                    return "cuia " + Num(i) + " (" + MechanicTypes.ToKey(cuia.Mechanic) + "): " + error;
                }
            }

            if (BoardRules.IsStuck(state.CuiasArray, 0, state.Count, units, futileRuleEnabled))
            {
                return "02 §9.2 regla 1: el nivel empieza en Aguas estancadas";
            }

            return null;
        }

        /// <summary>Colocación del Barro (02 §9.1 M5; D-007, propuesta b.2 M5) o null si es válida.</summary>
        private static string? MudError(BoardState state)
        {
            for (int i = 0; i < state.Count; i++)
            {
                Cuia cuia = state[i];
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (cuia.ColorAt(k) != Cuia.MudColor)
                    {
                        continue;
                    }

                    string where = "cuia " + Num(i) + ", capa " + Num(k) + ": ";
                    if (cuia.Capacity != Cuia.DefaultCapacity)
                    {
                        return where + "02 §9.1 M5: el Barro va en una cuia de 4";
                    }

                    if (k < 1 || k > 2)
                    {
                        return where + "02 §9.1 M5: el Barro va en la capa 2 o 3 (índice 1-2)";
                    }

                    if (cuia.HasMechanic)
                    {
                        return where + "D-007 (propuesta b.2 M5): el Barro nunca va en una cuia con mecánica";
                    }

                    if (cuia.IsHiddenAt(k))
                    {
                        return where + "el Barro nunca va oculto [P]";
                    }
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
