#nullable enable
using System.Globalization;
using AF.Core.Mechanics;

namespace AF.Core.Board
{
    /// <summary>
    /// Construye el <see cref="BoardState"/> de un nivel desde los campos de <c>af-level/1</c> (08 §4.1): <c>caps</c>,
    /// <c>cuias</c>, <c>attrs</c> (<see cref="MechanicSpec"/>), <c>hidden</c> y <c>units_per_color</c>. Comprueba lo que hace
    /// falta para construirlo bien (formato y campos obligatorios de cada mecánica, 02 §9.1 y D-007) y, si rechaza, devuelve
    /// el motivo. Las reglas de diseño del nivel (03 §17 #5-#6; 02 §9.2 regla 1) las comprueba <see cref="LevelValidation"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>partner</c> del Remolino → id de pareja (0..7) por orden de aparición; la pareja tiene que ser
    /// simétrica.</description></item>
    /// <item><description>Banderas iniciales: Hoja puesta y Brote cerrado llevan <see cref="Cuia.FlagClosed"/> (03 §9).</description></item>
    /// <item><description>Variantes de la 1.0 con la firma de <c>pack_schema</c> 3 (pregunta 8; propuesta de D-007 b.3): la Hoja
    /// atada, <c>{"type":"hoja_atada","key":c}</c>, con su llave y sin bandera (nunca se cierra ni se suelta); el Brote, con
    /// <c>n</c> ∈ {1, 2} opcional: sin <c>n</c> o con 1 es el Brote normal (<c>n</c> = 0 en la cuia: el mismo
    /// <see cref="Cuia.Packed"/> y el mismo hash que antes) y con 2, el Brote de dos hojas (<c>n</c> = 2 en la cuia: las hojas
    /// cerradas que le faltan, que bajan al recibir); y la Flor de vitória-régia, <c>{"type":"flor","key":c1,"key2":c2}</c>,
    /// con dos tintes distintos y sin bandera (se abre por sellados, que se deducen del estado; K3b). <c>key2</c> solo lo lleva
    /// la Flor.</description></item>
    /// <item><description>Tinaja y su cuia (M13): <c>{"type":"tinaja"}</c> y <c>{"type":"cucharon"}</c>, sin campos ni id de
    /// pareja (una tinaja por nivel); las ocultas de la tinaja llegan por <c>hidden</c>, como las de Aguas turbias.</description></item>
    /// <item><description><c>k</c> ausente en Lluvia o Piraña toma el valor por defecto que pasa el llamante
    /// (<c>lluvia_k_pours</c> / <c>pirana_k_pours</c> de Remote Config, 08 §9; 3 y 4 por defecto).</description></item>
    /// <item><description>Falla cerrado: rechaza las mecánicas cuyas reglas aún no están portadas al núcleo (se jugarían como
    /// cuias normales) y el color 16 (reservado, 02 R2). El 15 es Barro (02 §9.1 M5): no es un color de
    /// <c>units_per_color</c>, así que no se contrasta con él; sus reglas de colocación las comprueba
    /// <see cref="LevelValidation"/>.</description></item>
    /// </list>
    /// No es código caliente: se llama una vez por nivel.
    /// </remarks>
    public static class BoardBuilder
    {
        /// <summary><c>lluvia_k_pours</c> por defecto (08 §9).</summary>
        public const int DefaultLluviaKPours = 3;

        /// <summary><c>pirana_k_pours</c> por defecto (08 §9).</summary>
        public const int DefaultPiranaKPours = 4;

        /// <summary>Máximo de parejas de Remolino (3 bits de id).</summary>
        public const int MaxRemolinoPairs = Cuia.MaxMechanicField + 1;

        /// <summary>Construye el tablero o devuelve el motivo del rechazo.</summary>
        /// <param name="caps">Capacidad de cada cuia (1-6).</param>
        /// <param name="cuias">Dosis de cada cuia, de abajo arriba (índices de motor).</param>
        /// <param name="attrs">Mecánica de cada cuia (misma longitud) o null si el nivel no tiene ninguna.</param>
        /// <param name="hidden">Posiciones ocultas <c>[cuia, capa]</c> (Aguas turbias) o null.</param>
        /// <param name="units">U_c declarado (<c>units_per_color</c>) o null para no contrastarlo.</param>
        /// <param name="lluviaKPours">Ritmo de la Lluvia si su <c>attrs</c> no trae <c>k</c>.</param>
        /// <param name="piranaKPours">Ritmo de la Piraña si su <c>attrs</c> no trae <c>k</c>.</param>
        public static BoardBuildResult Build(
            int[] caps,
            int[][] cuias,
            MechanicSpec[]? attrs = null,
            int[][]? hidden = null,
            UnitsPerColor? units = null,
            int lluviaKPours = DefaultLluviaKPours,
            int piranaKPours = DefaultPiranaKPours)
        {
            return Build(caps, cuias, attrs, hidden, units, lluviaKPours, piranaKPours, true);
        }

        /// <summary>
        /// Como <see cref="Build(int[], int[][], MechanicSpec[], int[][], UnitsPerColor, int, int)"/>; con
        /// <paramref name="requirePorted"/> = false acepta mecánicas aún sin portar (solo para probar el formato).
        /// </summary>
        internal static BoardBuildResult Build(
            int[] caps,
            int[][] cuias,
            MechanicSpec[]? attrs,
            int[][]? hidden,
            UnitsPerColor? units,
            int lluviaKPours,
            int piranaKPours,
            bool requirePorted)
        {
            if (caps == null || cuias == null)
            {
                return Fail("faltan caps o cuias");
            }

            int count = cuias.Length;
            if (caps.Length != count)
            {
                return Fail("caps y cuias tienen longitudes distintas (" + Num(caps.Length) + " y " + Num(count) + ")");
            }

            if (count < 1 || count > BoardState.MaxCuias)
            {
                return Fail("R1: un tablero tiene entre 1 y 16 cuias, no " + Num(count));
            }

            if (attrs != null && attrs.Length != count)
            {
                return Fail("attrs y cuias tienen longitudes distintas (" + Num(attrs.Length) + " y " + Num(count) + ")");
            }

            var masks = new int[count];
            string? error = HiddenMasks(cuias, hidden, masks);
            if (error != null)
            {
                return Fail(error);
            }

            var built = new Cuia[count];
            for (int i = 0; i < count; i++)
            {
                error = BuildCuia(i, caps[i], cuias[i], masks[i], out built[i]);
                if (error != null)
                {
                    return Fail(error);
                }
            }

            if (attrs != null)
            {
                error = ApplyMechanics(attrs, built, lluviaKPours, piranaKPours, requirePorted);
                if (error != null)
                {
                    return Fail(error);
                }
            }

            // U_c (02 R3): las dosis del tablero más las gotas que la Lluvia aún no ha dejado caer.
            var table = new int[Rules.CuiaRules.UnitsTableLength];
            for (int i = 0; i < count; i++)
            {
                for (int k = 0; k < built[i].Count; k++)
                {
                    table[built[i].ColorAt(k)]++;
                }

                if (built[i].Mechanic == MechanicType.Lluvia)
                {
                    table[built[i].MechanicKey] += built[i].MechanicN;
                }
            }

            if (units != null)
            {
                for (int c = Cuia.MinColor; c <= Cuia.MaxTintColor; c++)
                {
                    // El Barro (15) no es un color (02 §9.1 M5): units_per_color no lo declara.
                    if (table[c] != 0 && table[c] != units.Of(c))
                    {
                        return Fail("units_per_color: el color " + Num(c) + " tiene " + Num(table[c]) + " dosis y se declararon " + Num(units.Of(c)) + " (02 R3)");
                    }
                }
            }

            return new BoardBuildResult(BoardState.FromValidated(built, table), null);
        }

        private static string? HiddenMasks(int[][] cuias, int[][]? hidden, int[] masks)
        {
            if (hidden == null)
            {
                return null;
            }

            for (int h = 0; h < hidden.Length; h++)
            {
                int[] position = hidden[h];
                if (position == null || position.Length != 2)
                {
                    return "hidden[" + Num(h) + "] no es un par [cuia, capa]";
                }

                int cuia = position[0];
                int layer = position[1];
                if ((uint)cuia >= (uint)cuias.Length || cuias[cuia] == null || (uint)layer >= (uint)cuias[cuia].Length)
                {
                    return "hidden[" + Num(h) + "] = [" + Num(cuia) + ", " + Num(layer) + "] no es una dosis del tablero";
                }

                if (layer == cuias[cuia].Length - 1)
                {
                    return "hidden[" + Num(h) + "]: R7, la cima de la cuia " + Num(cuia) + " siempre es visible";
                }

                if ((masks[cuia] & (1 << layer)) != 0)
                {
                    return "hidden[" + Num(h) + "] repite la dosis [" + Num(cuia) + ", " + Num(layer) + "]";
                }

                masks[cuia] |= 1 << layer;
            }

            return null;
        }

        private static string? BuildCuia(int index, int capacity, int[] doses, int hiddenMask, out Cuia cuia)
        {
            cuia = default;
            if (capacity < Cuia.MinCapacity || capacity > Cuia.MaxCapacity)
            {
                return "cuia " + Num(index) + ": R1, la capacidad va de 1 a 6, no " + Num(capacity);
            }

            if (doses == null)
            {
                return "cuia " + Num(index) + ": falta su lista de dosis";
            }

            if (doses.Length > capacity)
            {
                return "cuia " + Num(index) + ": R1, " + Num(doses.Length) + " dosis no caben en capacidad " + Num(capacity);
            }

            for (int k = 0; k < doses.Length; k++)
            {
                int color = doses[k];
                if (color < Cuia.MinColor || color > Cuia.MaxColor)
                {
                    return "cuia " + Num(index) + ": R2, el color " + Num(color) + " no es un índice de motor (1-16)";
                }

                if (color == Cuia.ReservedColor)
                {
                    return "cuia " + Num(index) + ": R2, el color 16 está reservado";
                }
            }

            cuia = Cuia.Create(capacity, doses, hiddenMask);
            return null;
        }

        private static string? ApplyMechanics(MechanicSpec[] attrs, Cuia[] built, int lluviaKPours, int piranaKPours, bool requirePorted)
        {
            int count = built.Length;
            var pairIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                pairIds[i] = -1;
            }

            int nextPair = 0;
            for (int i = 0; i < count; i++)
            {
                MechanicSpec spec = attrs[i];
                if (spec.IsNone)
                {
                    if (spec.Key != 0 || spec.Partner >= 0 || spec.N != 0 || spec.K != 0 || spec.Key2 != 0)
                    {
                        return "attrs[" + Num(i) + "]: campos sin type";
                    }

                    continue;
                }

                string where = "attrs[" + Num(i) + "] (" + MechanicTypes.ToKey(spec.Type) + "): ";
                if (spec.Key2 != 0 && spec.Type != MechanicType.Flor)
                {
                    return where + "key2 solo lo lleva la Flor de vitória-régia (pregunta 8)";
                }

                int flags = 0;
                int key = 0;
                int key2 = 0;
                int n = 0;
                int k = 0;
                int pair = 0;
                switch (spec.Type)
                {
                    case MechanicType.Hoja:
                        if (!IsTint(spec.Key) || spec.Partner >= 0 || spec.N != 0 || spec.K != 0)
                        {
                            return where + "lleva solo key, un tinte de 1 a 14 (02 §9.1 M1)";
                        }

                        key = spec.Key;
                        flags = Cuia.FlagClosed;
                        break;
                    case MechanicType.HojaAtada:
                        if (!IsTint(spec.Key) || spec.Partner >= 0 || spec.N != 0 || spec.K != 0)
                        {
                            return where + "lleva solo key, un tinte de 1 a 14 (propuesta de D-007 b.3; pregunta 8)";
                        }

                        key = spec.Key;
                        break;
                    case MechanicType.Brote:
                        if (spec.Key != 0 || spec.Partner >= 0 || spec.K != 0 || spec.N < 0 || spec.N > BroteMechanic.TwoLeaves)
                        {
                            return where + "lleva como mucho n, 1 o 2 (Brote de dos hojas; propuesta de D-007 b.3; pregunta 8)";
                        }

                        n = spec.N == BroteMechanic.TwoLeaves ? BroteMechanic.TwoLeaves : 0;
                        flags = Cuia.FlagClosed;
                        break;
                    case MechanicType.Matapi:
                    case MechanicType.Tinaja:
                    case MechanicType.Cucharon:
                        // M13: la tinaja y su cuia no llevan campos ni id de pareja; con una tinaja por nivel, cada pieza
                        // encuentra a la otra por su tipo (propuesta de la tinaja, c.1).
                        if (spec.Key != 0 || spec.Partner >= 0 || spec.N != 0 || spec.K != 0)
                        {
                            return where + "no lleva campos (02 §9.1; D-007; M13)";
                        }

                        break;
                    case MechanicType.Liana:
                        bool withFruit = spec.Key != 0;
                        bool withCount = spec.N != 0;
                        if (withFruit == withCount || spec.Partner >= 0 || spec.K != 0)
                        {
                            return where + "lleva exactamente uno de key (Liana con fruta) o n (Liana de cuenta) (D-007)";
                        }

                        if (withFruit && !IsTint(spec.Key))
                        {
                            return where + "key es un tinte de 1 a 14";
                        }

                        if (withCount && (spec.N < 1 || spec.N > 2))
                        {
                            return where + "n va de 1 a 2 (02 §9.1 M3)";
                        }

                        key = spec.Key;
                        n = spec.N;
                        break;
                    case MechanicType.Flor:
                        // Firma de pack_schema 3 (pregunta 8): flor {key, key2} con key ≠ key2; sin bandera (K3b).
                        if (!IsTint(spec.Key) || !IsTint(spec.Key2) || spec.Key == spec.Key2 || spec.Partner >= 0 || spec.N != 0 || spec.K != 0)
                        {
                            return where + "lleva solo key y key2, dos tintes distintos de 1 a 14 (propuesta de D-007 b.3; pregunta 8)";
                        }

                        key = spec.Key;
                        key2 = spec.Key2;
                        break;
                    case MechanicType.Remolino:
                        if (spec.Key != 0 || spec.N != 0 || spec.K != 0)
                        {
                            return where + "lleva solo partner (02 §9.1 M4)";
                        }

                        int partner = spec.Partner;
                        if ((uint)partner >= (uint)count || partner == i)
                        {
                            return where + "partner no es otra cuia del tablero";
                        }

                        if (attrs[partner].Type != MechanicType.Remolino || attrs[partner].Partner != i)
                        {
                            return where + "la pareja tiene que ser un Remolino que apunte a esta cuia";
                        }

                        if (pairIds[i] < 0)
                        {
                            if (nextPair == MaxRemolinoPairs)
                            {
                                return where + "hay más de " + Num(MaxRemolinoPairs) + " parejas";
                            }

                            pairIds[i] = nextPair;
                            pairIds[partner] = nextPair;
                            nextPair++;
                        }

                        pair = pairIds[i];
                        break;
                    case MechanicType.Lluvia:
                        if (!IsTint(spec.Key) || spec.N < 1 || spec.N > Cuia.MaxMechanicField || spec.Partner >= 0)
                        {
                            return where + "lleva key (tinte agua) y n (gotas, 1-7) (02 §9.1 M6)";
                        }

                        key = spec.Key;
                        n = spec.N;
                        k = spec.K != 0 ? spec.K : lluviaKPours;
                        break;
                    case MechanicType.Pirana:
                        if (spec.Key != 0 || spec.Partner >= 0 || spec.N != 0)
                        {
                            return where + "lleva solo k (02 §9.1 M7)";
                        }

                        k = spec.K != 0 ? spec.K : piranaKPours;
                        break;
                }

                if ((spec.Type == MechanicType.Lluvia || spec.Type == MechanicType.Pirana) && (k < 2 || k > 5))
                {
                    return where + "k va de 2 a 5 (08 §9)";
                }

                if (requirePorted && !MechanicTable.IsPorted(spec.Type))
                {
                    return where + "la mecánica aún no está portada al núcleo";
                }

                built[i] = built[i].WithMechanic(spec.Type, key, pair, n, k, 0, flags, key2);
            }

            return null;
        }

        private static bool IsTint(int color)
        {
            return color >= Cuia.MinColor && color <= Cuia.MaxTintColor;
        }

        private static BoardBuildResult Fail(string reason)
        {
            return new BoardBuildResult(null, reason);
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Resultado de <see cref="BoardBuilder.Build"/>: el tablero o el motivo del rechazo.</summary>
    public readonly struct BoardBuildResult
    {
        internal BoardBuildResult(BoardState? state, string? error)
        {
            State = state;
            Error = error;
        }

        /// <summary>Tablero construido, o null si se rechazó.</summary>
        public BoardState? State { get; }

        /// <summary>Motivo del rechazo, o null si se construyó.</summary>
        public string? Error { get; }

        /// <summary>Se construyó el tablero.</summary>
        public bool IsValid => State != null;
    }
}
