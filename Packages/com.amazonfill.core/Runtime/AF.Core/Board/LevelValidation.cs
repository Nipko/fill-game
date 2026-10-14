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
    /// <c>U_c = v</c>, y para <c>v</c> ∈ {5, 6} exactamente tantas. La Tinaja (M13) no cuenta en ninguna de las dos
    /// comparaciones: nunca recibe, así que no es casa de ninguna fruta. La Hoja atada sí cuenta, y la cuenta es exacta porque
    /// solo es casa de su fruta, con capacidad = <c>U_c</c> de su llave y como mucho una por fruta (D-024; las comprueba
    /// <c>HojaAtadaMechanic</c> más abajo): dos de la misma fruta contarían la segunda como casa de otra fruta.</description></item>
    /// <item><description>Ninguna cuia sellada al inicio (03 §17 #5).</description></item>
    /// <item><description>Barro (02 §9.1 M5): solo en la capa 2 o 3 (índice 1-2) de una cuia de 4; nunca en una cuia con
    /// mecánica (propuesta de D-007 b.2 M5: «nunca dentro de un Matapi»; D-007) ni como dosis oculta [P].</description></item>
    /// <item><description>Aguas turbias (02 §11; D-007, Q14 A): T2 (como mucho <c>lvl_murky_max_cuias</c> cuias turbias y
    /// <c>lvl_murky_max_hidden_units</c> dosis ocultas; por defecto 3 y 4, 08 §9); T3 (una cuia turbia tiene como mucho 3
    /// dosis, nunca está llena y su cima es visible); T4 (bajo una dosis oculta nunca va el color de la dosis de encima);
    /// ocultos solo en cuias sin mecánica; el nivel puede llevar filtros (Hoja, Brote, Matapi, Barro; también las variantes
    /// de la 1.0 Hoja atada y Brote de dos hojas, que son filtros, K3a) pero no Liana, Lluvia ni Piraña (Q14 A) ni Remolino
    /// [P: propuesta b.2, «solo junto a filtros»], ni la Flor de vitória-régia, que es un disparador (02 T1: «nunca con
    /// mecánicas con disparador (Liana, Flor, …)»; K3b). T1 (etiqueta y nivel ≥ 100) y Q9 son del
    /// pipeline. <b>Tinaja</b> (M13; D-019, Q-Ti1 A): si el nivel la lleva, es la única fuente de ocultas (ninguna otra cuia
    /// turbia), sus ocultas son la excepción a «ocultos solo en cuias sin mecánica» y a T3, y su tope es
    /// <see cref="TinajaMaxHiddenUnits"/> en lugar de los de Remote Config; las combinaciones prohibidas de Q14 A siguen
    /// valiendo (también la Flor: la tinaja va «solo con filtros y capacidades», 02 §9.2 regla 7) y solo se comprueban aquí,
    /// con un motivo que nombra la tinaja. También solo se comprueban aquí T4 en toda su
    /// columna (con su máscara, todas sus dosis menos la boca están ocultas), su tope de ocultas y «sin Barro» (lo rechaza la
    /// regla del Barro de arriba: va en una cuia de 4 y nunca en una cuia con mecánica). El resto de sus reglas (capacidad,
    /// dosis, máscara, una sola tinaja y su cuia contigua) son de <c>TinajaMechanic</c> y
    /// <c>CucharonMechanic</c>.</description></item>
    /// <item><description>Como mucho un contador por nivel (Lluvia o Piraña; propuesta de D-007 b.2 M6-M7 y d.2 regla 6): la
    /// Lluvia y la Piraña nunca van juntas.</description></item>
    /// <item><description>Las reglas propias de cada mecánica (03 §17 #6), con las de las variantes de la 1.0 (K3a; propuesta
    /// de D-007 b.3): la Hoja atada, con su llave en el tablero, su fruta en el fondo si empieza con dosis, capacidad =
    /// <c>U_c</c> de su llave y como mucho una por fruta (D-024); el Brote de dos hojas, con espacio ≥ 2; y la Flor de
    /// vitória-régia (K3b), con dos llaves distintas en el tablero que pueden sellar fuera de ella a la vez y, mientras dure el
    /// bloqueo C1-9 de la Liana (pregunta 24 b), sin capacidades mixtas.</description></item>
    /// <item><description><c>atascado(S0)</c> = false con la <c>futil</c> única (02 §9.2 regla 1).</description></item>
    /// </list>
    /// </remarks>
    public static class LevelValidation
    {
        /// <summary>Mínimo de <c>U_c</c> en campaña (02 R3).</summary>
        public const int MinUnits = 4;

        /// <summary>Máximo de <c>U_c</c> en campaña (02 R3, fruta abundante de una Cuia alta de 6).</summary>
        public const int MaxUnits = 6;

        /// <summary><c>lvl_murky_max_cuias</c> por defecto (02 T2; 08 §9).</summary>
        public const int DefaultMurkyMaxCuias = 3;

        /// <summary><c>lvl_murky_max_hidden_units</c> por defecto (02 T2; 08 §9): ≤ 24 realidades.</summary>
        public const int DefaultMurkyMaxHiddenUnits = 4;

        /// <summary>Dosis máximas de una cuia turbia al inicio (02 T3).</summary>
        public const int MurkyMaxDoses = 3;

        /// <summary>Tinte agua del mundo en aguas blancas y claras: Agua del Tapajós (02 §9.1 M6; 03 §9).</summary>
        public const int TapajosWater = 10;

        /// <summary>Tinte agua del mundo en aguas negras: Agua del Río Negro (02 §9.1 M6; 03 §9).</summary>
        public const int RioNegroWater = 14;

        /// <summary><c>U_agua</c> de un nivel con Lluvia: dosis iniciales + gotas pendientes (02 §9.1 M6, R3).</summary>
        public const int LluviaWaterUnits = 4;

        /// <summary>Capacidad de la Tinaja (M13; propuesta de la tinaja, b.2 regla 1: la silueta de la Cuia alta de 5).</summary>
        public const int TinajaCapacity = 5;

        /// <summary>Dosis mínimas de la Tinaja al empezar (M13, b.2 regla 1).</summary>
        public const int TinajaMinDoses = 3;

        /// <summary>Dosis máximas de la Tinaja al empezar (M13, b.2 regla 1): puede empezar llena, porque nunca recibe.</summary>
        public const int TinajaMaxDoses = 5;

        /// <summary>
        /// Tope propio de dosis ocultas de la Tinaja [P] (M13, b.2 regla 1): 4, el límite de 24 realidades de
        /// <c>RealityEnumerator</c>. No es <c>lvl_murky_max_hidden_units</c>: si se baja el tope de Aguas turbias (rango 2-4,
        /// 08 §9), no debe rechazar tinajas ya publicadas. El tope por tipo de nivel (2 en comodidad y práctica, 3-4 en retos e
        /// hitos) lo comprueba el pipeline.
        /// </summary>
        public const int TinajaMaxHiddenUnits = 4;

        /// <summary>Capacidad de la cuia de la Tinaja (M13, b.2 regla 1: una Cuia chica de 2).</summary>
        public const int CucharonCapacity = 2;

        /// <summary>
        /// Comprueba las reglas sobre el tablero inicial. Devuelve null si es válido o el motivo del primer rechazo, en el
        /// orden de la lista de la clase.
        /// </summary>
        /// <param name="state">Tablero inicial (normalmente de <see cref="BoardBuilder"/>).</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> con el que se comprueba <c>atascado(S0)</c>.</param>
        public static string? Validate(BoardState state, bool futileRuleEnabled = true)
        {
            return Validate(state, futileRuleEnabled, DefaultMurkyMaxCuias, DefaultMurkyMaxHiddenUnits);
        }

        /// <summary>
        /// Como <see cref="Validate(BoardState, bool)"/> con los límites de Aguas turbias de Remote Config (02 T2; 08 §9).
        /// </summary>
        /// <param name="state">Tablero inicial.</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c>.</param>
        /// <param name="murkyMaxCuias"><c>lvl_murky_max_cuias</c> (3 por defecto).</param>
        /// <param name="murkyMaxHiddenUnits"><c>lvl_murky_max_hidden_units</c> (4 por defecto).</param>
        public static string? Validate(BoardState state, bool futileRuleEnabled, int murkyMaxCuias, int murkyMaxHiddenUnits)
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
                // M13 (propuesta de la tinaja, b.2 regla 1 y c.1): la Tinaja no es casa de ninguna fruta (nunca recibe), así que
                // no cuenta en ninguna de las dos comparaciones: ni como Cuia alta sin fruta abundante ni como sitio donde
                // sellar una fruta de U_c = 5.
                if (state[i].Mechanic != MechanicType.Tinaja)
                {
                    cuiasWithCap[state[i].Capacity]++;
                }
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

            string? murky = MurkyError(state, murkyMaxCuias, murkyMaxHiddenUnits);
            if (murky != null)
            {
                return murky;
            }

            string? counters = CounterError(state);
            if (counters != null)
            {
                return counters;
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

        /// <summary>
        /// Colocación del Barro (02 §9.1 M5; D-007, propuesta b.2 M5) o null si es válida. Es la única comprobación de «sin
        /// Barro» en la Tinaja (M13, b.2 regla 1): en su cuia de 5 da «el Barro va en una cuia de 4».
        /// </summary>
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

        /// <summary>
        /// Como mucho un contador por nivel (propuesta de D-007 b.2 M6 y M7, d.2 regla 6 y d.3: dos contadores en el mismo
        /// nivel están prohibidos; la Piraña nunca va con la Lluvia) o null.
        /// </summary>
        private static string? CounterError(BoardState state)
        {
            int first = -1;
            for (int i = 0; i < state.Count; i++)
            {
                MechanicType type = state[i].Mechanic;
                if (type != MechanicType.Lluvia && type != MechanicType.Pirana)
                {
                    continue;
                }

                if (first >= 0)
                {
                    return "D-007 (propuesta b.2 M6-M7, d.2 regla 6): como mucho un contador por nivel; hay "
                        + MechanicTypes.ToKey(state[first].Mechanic) + " en la cuia " + Num(first) + " y "
                        + MechanicTypes.ToKey(type) + " en la cuia " + Num(i);
                }

                first = i;
            }

            return null;
        }

        /// <summary>
        /// Aguas turbias (02 §11 T2-T4; D-007, Q14 A) o null si el tablero no tiene dosis ocultas o son válidas. Con una Tinaja
        /// (M13; D-019, Q-Ti1 A; propuesta de la tinaja, b.2 y c.1) la tinaja es la única fuente de ocultas del nivel: sus
        /// ocultas son la excepción a «ocultos solo en cuias sin mecánica» (Q14 A) y a T3 (≤ 3 dosis y nunca llena), su tope
        /// es <see cref="TinajaMaxHiddenUnits"/> (no los de Remote Config de T2) y T4 se comprueba igual. Las combinaciones
        /// prohibidas (Liana, Remolino, Lluvia y Piraña, b.2; y la Flor, K3b; con un motivo propio de la tinaja), el tope y T4 en toda la
        /// columna de la tinaja (b.2 regla 1) se rechazan solo aquí: una tinaja válida siempre llega con ocultas (sin ellas la
        /// rechaza su máscara en <c>TinajaMechanic.ValidateInitial</c>), y con esa máscara todas sus dosis menos la boca pasan
        /// por la comprobación de T4.
        /// </summary>
        private static string? MurkyError(BoardState state, int maxCuias, int maxHiddenUnits)
        {
            int murkyCuias = 0;
            int hiddenUnits = 0;
            int tinaja = -1;
            for (int i = 0; i < state.Count; i++)
            {
                int hidden = state[i].HiddenCount;
                if (hidden > 0)
                {
                    murkyCuias++;
                    hiddenUnits += hidden;
                }

                if (state[i].Mechanic == MechanicType.Tinaja)
                {
                    tinaja = i;
                }
            }

            if (murkyCuias == 0)
            {
                return null;
            }

            if (tinaja >= 0)
            {
                // M13 (b.2, combinaciones): la tinaja es la única fuente de ocultas, sin otras cuias turbias; su tope es propio.
                // Dos tinajas las rechaza TinajaMechanic.ValidateInitial (una por nivel).
                for (int i = 0; i < state.Count; i++)
                {
                    if (state[i].HasHidden && state[i].Mechanic != MechanicType.Tinaja)
                    {
                        return "M13 (combinaciones): la tinaja es la única fuente de dosis ocultas; la cuia " + Num(i) + " es turbia";
                    }
                }

                if (hiddenUnits > TinajaMaxHiddenUnits)
                {
                    return "M13 (regla 1): " + Num(hiddenUnits) + " dosis ocultas en la tinaja; como mucho " + Num(TinajaMaxHiddenUnits);
                }
            }
            else if (murkyCuias > maxCuias)
            {
                return "02 T2: " + Num(murkyCuias) + " cuias turbias; como mucho lvl_murky_max_cuias = " + Num(maxCuias);
            }
            else if (hiddenUnits > maxHiddenUnits)
            {
                return "02 T2: " + Num(hiddenUnits) + " dosis ocultas; como mucho lvl_murky_max_hidden_units = " + Num(maxHiddenUnits);
            }

            for (int i = 0; i < state.Count; i++)
            {
                Cuia cuia = state[i];
                MechanicType type = cuia.Mechanic;
                if (tinaja >= 0 && (type == MechanicType.Liana || type == MechanicType.Remolino || type == MechanicType.Lluvia || type == MechanicType.Pirana
                    || type == MechanicType.Flor))
                {
                    // M13 (b.2, combinaciones prohibidas: «LevelValidation ya lo prohíbe con ocultos»): la única comprobación de
                    // estas combinaciones en un nivel con tinaja, porque sus ocultas siempre llegan aquí (TinajaMechanic no la
                    // repite). El informe nombra la tinaja, no Aguas turbias. La Flor es un disparador y la tinaja va «solo con
                    // filtros y capacidades» (02 §9.2 regla 7; K3b).
                    return "M13 (b.2, combinaciones; Q14 A): la tinaja no se combina con " + MechanicTypes.ToKey(type) + " (cuia " + Num(i) + ")";
                }

                if (type == MechanicType.Liana || type == MechanicType.Lluvia || type == MechanicType.Pirana || type == MechanicType.Flor)
                {
                    // 02 T1: «nunca con mecánicas con disparador (Liana, Flor, …) ni contadores (Lluvia, Piraña…)» (Q14 A; K3b).
                    return "D-007, Q14 A: Aguas turbias no se combina con " + MechanicTypes.ToKey(type) + " (cuia " + Num(i) + ")";
                }

                if (type == MechanicType.Remolino)
                {
                    return "Aguas turbias no se combina con el Remolino (cuia " + Num(i) + ") [P: propuesta de D-007 b.2, solo junto a filtros]";
                }

                if (!cuia.HasHidden)
                {
                    continue;
                }

                string where = "cuia turbia " + Num(i) + ": ";
                bool isTinaja = type == MechanicType.Tinaja;
                if (cuia.HasMechanic && !isTinaja)
                {
                    return where + "02 §11: una cuia con mecánica lleva sus dosis visibles; las dosis ocultas solo van en cuias sin mecánica (D-007, Q14 A)";
                }

                if (!isTinaja && (cuia.Count > MurkyMaxDoses || cuia.IsFull))
                {
                    return where + "02 T3: como mucho 3 dosis al inicio y nunca llena";
                }

                if (cuia.IsHiddenAt(cuia.Count - 1))
                {
                    return where + "02 T3: la cima es visible";
                }

                for (int k = 0; k < cuia.Count - 1; k++)
                {
                    if (!cuia.IsHiddenAt(k))
                    {
                        continue;
                    }

                    if (cuia.ColorAt(k) == cuia.ColorAt(k + 1))
                    {
                        return where + "02 T4: la dosis oculta " + Num(k) + " repite el color de la de encima";
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
