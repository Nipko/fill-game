#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lee un paquete de niveles <c>af-level/1</c> (GDD 08 §4.1; contrato 21, <c>schemas/af-level-1.schema.json</c>) o el
    /// subconjunto documentado de v0 (<c>"subset_of": "af-level/1"</c>, docs/dev/prototipo-v0.md b) y produce
    /// <see cref="LevelPack"/>.
    /// </summary>
    /// <remarks>
    /// <para>Acepta <c>pack_schema</c> 1, 2 o 3 (<see cref="MinPackSchema"/>-<see cref="SupportedPackSchema"/>) o
    /// <c>subset_of</c> = <c>af-level/1</c>, que se lee como <c>pack_schema</c> 1; rechaza cualquier otra versión, también un
    /// paquete 4 (08 §4.4: el cliente rechaza esquemas que no conoce). Con <c>pack_schema</c> 1 rechaza lo que el esquema solo
    /// admite desde el 2 (allOf): el tipo <c>matapi</c>, <c>intro_mechanic</c> <c>chica</c>, <c>alta</c>, <c>matapi</c> o
    /// <c>murky</c> y <c>difficulty.par_approx</c>. Con el 1 y el 2 rechaza lo que el esquema solo admite en el 3 (D-019,
    /// punto 9, y D-023, punto 5; contrato 21): los tipos <c>tinaja</c>, <c>cucharon</c>, <c>hoja_atada</c> y <c>flor</c>,
    /// <c>key2</c>, el Brote de dos hojas (<c>brote</c> con <c>n</c> = 2) [P], <c>intro_mechanic</c> <c>tinaja</c>,
    /// <c>liana_n</c>, <c>hoja_atada</c>, <c>brote_n2</c> o <c>flor</c> y <c>hidden</c> en una variante de
    /// <c>relief</c>.</para>
    /// <para>[P] El Brote de dos hojas con el 1 o el 2 es la única regla de versión que el lector no comparte con el esquema ni
    /// con <c>tools/level-qa</c>: el esquema dice que el <c>n</c> del Brote «no se restringe con el 1 ni con el 2» y level-qa no
    /// lo mira por versión, así que un paquete así pasa las dos y falla cerrado aquí. Hoy no lo lleva ningún paquete (1-200 y
    /// mundo 6); falta que el dueño o el tech-lead decidan si la regla pasa también a level-qa o sale del lector.</para>
    /// <para>Con <c>pack_schema</c> 3 lee <c>key2</c> (la segunda llave de la Flor de vitória-régia), el <c>n</c> del Brote
    /// (1 o 2) y la Tinaja con su cuia (M13): las ocultas de la tinaja llegan por <c>murky</c> y <c>hidden</c>, como las de
    /// Aguas turbias, con su tope propio (<see cref="LevelValidation.TinajaMaxHiddenUnits"/>, no el de 02 T2), y una variante
    /// de <c>relief</c> que lleva la tinaja trae su máscara en <c>hidden</c> (obligatoria y solo entonces; 08 §4.1).</para>
    /// <para>Cada tablero (el base y los de <c>relief[]</c>) se construye con <see cref="BoardBuilder"/> (mecánicas de
    /// <c>attrs</c>, dosis ocultas de <c>hidden</c> y <c>units_per_color</c>; ritmos de Lluvia y Piraña por defecto, 08 §9) y se
    /// comprueba con <see cref="LevelValidation"/>, como <c>tools/level-qa</c>. Falla cerrado: un tablero que no construye o no
    /// valida rechaza el paquete entero con <see cref="LevelFormatException"/>, y el mensaje empieza por el id del nivel.</para>
    /// <para>No valida lo que valida el pipeline (resolubilidad, par, bot, debuts): solo lo que el cliente necesita para
    /// construir el tablero sin sorpresas.</para>
    /// </remarks>
    public static class LevelPackReader
    {
        /// <summary>Valor de <c>subset_of</c> que admite el cliente v0.</summary>
        public const string SubsetOf = "af-level/1";

        /// <summary>Última versión de <c>pack_schema</c> que admite el cliente (D-007, D-019 y D-023; contrato 21).</summary>
        public const int SupportedPackSchema = 3;

        /// <summary>Primera versión de <c>pack_schema</c> que admite el cliente.</summary>
        public const int MinPackSchema = 1;

        /// <summary>Dosis ocultas como mucho por nivel en Aguas turbias (02 T2; esquema <c>$defs/hidden</c> <c>maxItems</c>).</summary>
        public const int MaxHidden = 4;

        // Primera versión de pack_schema con la Tinaja y las variantes de la 1.0 (D-019, punto 9; D-023, punto 5).
        private const int TinajaPackSchema = 3;

        // n del Brote de dos hojas (02 §9.1, b.3; BroteMechanic.TwoLeaves del núcleo, que es interno).
        private const int BroteTwoLeaves = 2;

        // intro_mechanic de pack_schema 1 (allOf/1), las que añade el 2 [P] (chica, alta, matapi y murky) y las del 3 (tinaja,
        // liana_n [P], hoja_atada, brote_n2 [P] y flor; D-019 y D-023, punto 5).
        private static readonly string[] IntroMechanics1 = { "hoja", "brote", "liana", "remolino", "barro", "lluvia", "pirana" };
        private static readonly string[] IntroMechanics2 = { "matapi", "chica", "alta", "murky" };
        private static readonly string[] IntroMechanics3 = { "tinaja", "liana_n", "hoja_atada", "brote_n2", "flor" };

        private static readonly string[] Bands =
        {
            LevelDefinition.BandComfort, LevelDefinition.BandChallenge, LevelDefinition.BandRecovery, LevelDefinition.BandMilestone,
        };

        /// <summary>Lee el paquete.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato admitido.</exception>
        public static LevelPack Read(string json)
        {
            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("El paquete de niveles no es JSON válido: " + e.Message, e);
            }

            if (root.Kind != JsonKind.Object)
            {
                throw new LevelFormatException("La raíz del paquete debe ser un objeto.");
            }

            int version = CheckSchema(root);
            string packId = RequireString(root, "pack_id", "paquete");
            string kind = RequireString(root, "kind", packId);
            int world = RequireInt(root, "world", packId);
            string worldId = RequireString(root, "world_id", packId);
            string waterBase = RequireWaterBase(root, packId);

            JsonValue? levelsNode = root.Get("levels");
            if (levelsNode == null || levelsNode.Kind != JsonKind.Array || levelsNode.Items.Count == 0)
            {
                throw new LevelFormatException(packId + ": 'levels' debe ser una lista no vacía.");
            }

            var levels = new List<LevelDefinition>(levelsNode.Items.Count);
            for (int i = 0; i < levelsNode.Items.Count; i++)
            {
                LevelDefinition level = ReadLevel(levelsNode.Items[i], packId, i, version);
                for (int k = 0; k < levels.Count; k++)
                {
                    if (levels[k].Level == level.Level)
                    {
                        throw new LevelFormatException(packId + ": el nivel " + level.Level.ToString(CultureInfo.InvariantCulture) + " está repetido.");
                    }
                }

                levels.Add(level);
            }

            LevelDefinition[] sorted = levels.ToArray();
            Array.Sort(sorted, CompareByLevel);
            return new LevelPack(packId, kind, world, worldId, waterBase, sorted);
        }

        private static int CompareByLevel(LevelDefinition a, LevelDefinition b)
        {
            return a.Level.CompareTo(b.Level);
        }

        // pack_schema 1-3 o subset_of = af-level/1 (que se lee como el 1). Devuelve la versión.
        private static int CheckSchema(JsonValue root)
        {
            JsonValue? idNode = root.Get("pack_id");
            string where = idNode != null && idNode.Kind == JsonKind.String ? idNode.AsString() : "paquete";
            JsonValue? schema = root.Get("pack_schema");
            if (schema != null)
            {
                int version = AsInt(schema, "pack_schema", where);
                if (version < MinPackSchema || version > SupportedPackSchema)
                {
                    throw new LevelFormatException(where + ": pack_schema " + version.ToString(CultureInfo.InvariantCulture)
                        + " no admitido (este cliente lee las versiones " + MinPackSchema.ToString(CultureInfo.InvariantCulture)
                        + " a " + SupportedPackSchema.ToString(CultureInfo.InvariantCulture) + "; 08 §4.4: hace falta una versión más nueva del juego).");
                }

                return version;
            }

            JsonValue? subset = root.Get("subset_of");
            if (subset != null && subset.Kind == JsonKind.String && string.Equals(subset.AsString(), SubsetOf, StringComparison.Ordinal))
            {
                return MinPackSchema;
            }

            throw new LevelFormatException(where + ": el paquete no declara pack_schema (1, 2 o 3) ni subset_of = \"" + SubsetOf + "\".");
        }

        private static LevelDefinition ReadLevel(JsonValue node, string packId, int position, int version)
        {
            string where = packId + ".levels[" + position.ToString(CultureInfo.InvariantCulture) + "]";
            if (node.Kind != JsonKind.Object)
            {
                throw new LevelFormatException(where + ": cada nivel debe ser un objeto.");
            }

            string id = RequireString(node, "id", where);
            where = id;
            int level = RequireInt(node, "level", where);
            if (level < 1)
            {
                throw new LevelFormatException(where + ": 'level' debe ser ≥ 1.");
            }

            int world = RequireInt(node, "world", where);
            string waterBase = RequireWaterBase(node, where);
            string[] palette = ReadStringList(node, "palette", where);
            UnitsPerColor units = ReadUnits(node, where);
            ReadBoard(node, where, out int[] caps, out int[][] cuias);
            MechanicSpec[] attrs = ReadAttrs(node, caps.Length, version, where);
            bool murky = ReadMurky(node, caps.Length, HasTinaja(attrs), where, out int[][] hidden);

            JsonValue? difficulty = node.Get("difficulty");
            if (difficulty == null || difficulty.Kind != JsonKind.Object)
            {
                throw new LevelFormatException(where + ": falta 'difficulty'.");
            }

            int par = RequireInt(difficulty, "par", where);
            if (par < 1)
            {
                throw new LevelFormatException(where + ": 'difficulty.par' debe ser ≥ 1.");
            }

            string label = ReadDifficultyLabel(difficulty, where);
            string? band = ReadBand(difficulty, where);
            bool parApprox = ReadParApprox(difficulty, version, where);
            string? intro = ReadIntroMechanic(node, version, where);
            bool skippable = ReadOptionalBool(node, "skippable", true, where);
            bool hintSupported = ReadOptionalBool(node, "hint_supported", true, where);

            BuildAndValidate(caps, cuias, attrs, hidden.Length == 0 ? null : hidden, units, where);
            ReliefBoard[] relief = ReadRelief(node, units, version, where);
            return new LevelDefinition(
                id, level, world, waterBase, palette, units, caps, cuias, attrs, murky, hidden, par, label, relief, intro, skippable, hintSupported, band, parApprox);
        }

        // BoardBuilder (formato y campos de cada mecánica, U_c con las gotas de la Lluvia) y LevelValidation (03 §17 #5-#6;
        // 02 §8, §9.1, §9.2 regla 1 y §11), con el patrón de tools/level-qa/Checks/QaCore.cs. Falla cerrado.
        private static void BuildAndValidate(int[] caps, int[][] cuias, MechanicSpec[] attrs, int[][]? hidden, UnitsPerColor units, string where)
        {
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden, units);
            if (built.State == null)
            {
                throw new LevelFormatException(where + ": " + built.Error);
            }

            string? reason = LevelValidation.Validate(built.State);
            if (reason != null)
            {
                throw new LevelFormatException(where + ": " + reason);
            }
        }

        // units_per_color (08 §4.1): un entero 1-6 (todos los colores) o un objeto {"1".."16": 1-6} con las excepciones
        // (p. ej. {"10": 5} en el nivel 27, 02 §8); los colores que no nombra valen 4 (UnitsPerColor.DefaultUnits).
        private static UnitsPerColor ReadUnits(JsonValue node, string where)
        {
            JsonValue? value = node.Get("units_per_color");
            if (value == null)
            {
                throw new LevelFormatException(where + ": falta 'units_per_color'.");
            }

            if (value.Kind == JsonKind.Number)
            {
                int units = AsInt(value, "units_per_color", where);
                if (units < 1 || units > Cuia.MaxCapacity)
                {
                    throw new LevelFormatException(where + ": 'units_per_color' fuera de 1..6.");
                }

                return UnitsPerColor.Uniform(units);
            }

            if (value.Kind != JsonKind.Object || value.Members.Count == 0)
            {
                throw new LevelFormatException(where + ": 'units_per_color' debe ser un entero de 1 a 6 o un objeto {color: dosis} no vacío.");
            }

            var overrides = new Dictionary<int, int>(value.Members.Count);
            for (int i = 0; i < value.Members.Count; i++)
            {
                string key = value.Members[i].Key;
                bool numeric = int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out int color);
                if (!numeric || color < Cuia.MinColor || color > Cuia.MaxColor || key != color.ToString(CultureInfo.InvariantCulture))
                {
                    throw new LevelFormatException(where + ": 'units_per_color' tiene la clave '" + key + "', que no es un color de 1 a 16.");
                }

                int units = AsInt(value.Members[i].Value, "units_per_color", where);
                if (units < 1 || units > Cuia.MaxCapacity)
                {
                    throw new LevelFormatException(where + ": 'units_per_color' del color " + key + " fuera de 1..6.");
                }

                overrides[color] = units;
            }

            try
            {
                return UnitsPerColor.FromObject(overrides);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new LevelFormatException(where + ": 'units_per_color' no es válido: " + e.Message, e);
            }
        }

        // caps + cuias de un tablero (nivel o variante de relief[]): solo el formato. Longitudes iguales de 1 a 16, capacidades
        // 1-6, no más dosis que la capacidad y colores 1-15 (15 = Barro; 16 reservado, 02 R2). U_c lo comprueba BoardBuilder,
        // que cuenta las gotas que la Lluvia aún no ha dejado caer.
        private static void ReadBoard(JsonValue node, string where, out int[] caps, out int[][] cuias)
        {
            caps = ReadIntList(RequireArray(node, "caps", where), "caps", where);
            JsonValue cuiasNode = RequireArray(node, "cuias", where);
            if (caps.Length < 1 || caps.Length > BoardState.MaxCuias)
            {
                throw new LevelFormatException(where + ": 'caps' debe tener entre 1 y 16 cuias.");
            }

            if (cuiasNode.Items.Count != caps.Length)
            {
                throw new LevelFormatException(where + ": 'cuias' y 'caps' deben tener la misma longitud.");
            }

            cuias = new int[caps.Length][];
            for (int i = 0; i < caps.Length; i++)
            {
                if (caps[i] < Cuia.MinCapacity || caps[i] > Cuia.MaxCapacity)
                {
                    throw new LevelFormatException(where + ": capacidad fuera de 1..6 en la cuia " + i.ToString(CultureInfo.InvariantCulture) + ".");
                }

                JsonValue cuiaNode = cuiasNode.Items[i];
                if (cuiaNode.Kind != JsonKind.Array)
                {
                    throw new LevelFormatException(where + ": cada cuia debe ser una lista de dosis.");
                }

                int[] doses = ReadIntList(cuiaNode, "cuias", where);
                if (doses.Length > caps[i])
                {
                    throw new LevelFormatException(where + ": la cuia " + i.ToString(CultureInfo.InvariantCulture) + " tiene más dosis que su capacidad.");
                }

                for (int k = 0; k < doses.Length; k++)
                {
                    int color = doses[k];
                    if (color < Cuia.MinColor || color > Cuia.MudColor)
                    {
                        throw new LevelFormatException(where + ": color " + color.ToString(CultureInfo.InvariantCulture)
                            + " fuera de 1..15 (tintes 1-14 y Barro 15; el 16 está reservado, 02 R2).");
                    }
                }

                cuias[i] = doses;
            }
        }

        // attrs (08 §4.1; 02 §9.1; D-007): ausente o null, ninguna mecánica; si es lista, una entrada por cuia, null o
        // {type, key, key2, partner, n, k}. Los campos ausentes valen key 0, key2 0, partner −1, n 0 y k 0 (como Attr() de
        // tools/level-qa/PackReader.cs); los obligatorios de cada tipo los comprueba BoardBuilder. Lo que solo existe desde un
        // pack_schema (matapi desde el 2; tinaja, cucharon, hoja_atada, flor, key2 y el Brote de dos hojas con el 3) se rechaza
        // con una versión anterior.
        private static MechanicSpec[] ReadAttrs(JsonValue node, int cuiaCount, int version, string where)
        {
            var specs = new MechanicSpec[cuiaCount];
            JsonValue? attrs = node.Get("attrs");
            if (attrs == null || attrs.IsNull)
            {
                return specs;
            }

            if (attrs.Kind != JsonKind.Array || attrs.Items.Count != cuiaCount)
            {
                throw new LevelFormatException(where + ": 'attrs' debe tener una entrada por cuia.");
            }

            for (int i = 0; i < cuiaCount; i++)
            {
                JsonValue entry = attrs.Items[i];
                if (entry.IsNull)
                {
                    continue;
                }

                string at = where + ": attrs[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (entry.Kind != JsonKind.Object)
                {
                    throw new LevelFormatException(at + " debe ser null o un objeto {type, key, key2, partner, n, k}.");
                }

                JsonValue? typeNode = entry.Get("type");
                string typeKey = typeNode != null && typeNode.Kind == JsonKind.String ? typeNode.AsString() : string.Empty;
                if (!MechanicTypes.TryParse(typeKey, out MechanicType type))
                {
                    throw new LevelFormatException(at + ": tipo de mecánica desconocido '" + typeKey + "' (02 §9.1; D-007).");
                }

                if (type == MechanicType.Matapi && version < 2)
                {
                    throw new LevelFormatException(at + ": el tipo matapi necesita pack_schema 2 (D-007; contrato 21).");
                }

                if (version < TinajaPackSchema && IsPackSchema3Type(type))
                {
                    throw new LevelFormatException(at + ": el tipo " + typeKey + " necesita pack_schema 3 (D-019 y D-023, punto 5; contrato 21).");
                }

                JsonValue? key2Node = entry.Get("key2");
                if (key2Node != null && version < TinajaPackSchema)
                {
                    throw new LevelFormatException(at + ": 'key2' necesita pack_schema 3 (Flor de vitória-régia; D-023, punto 5).");
                }

                int key = OptionalInt(entry, "key", 0, at);
                int key2 = key2Node == null ? 0 : AsInt(key2Node, "key2", at);
                int partner = OptionalInt(entry, "partner", -1, at);
                JsonValue? nNode = entry.Get("n");
                int n = nNode == null ? 0 : AsInt(nNode, "n", at);
                int k = OptionalInt(entry, "k", 0, at);
                if (type == MechanicType.Brote && nNode != null)
                {
                    // P2b (D-023, punto 5): con el 3, el n del Brote es 1 (el Brote normal) o 2 (el Brote de dos hojas). Con el 1 y
                    // el 2 el esquema no lo restringe, pero el Brote de dos hojas solo existe con el 3 ([P]: level-qa no lo comprueba;
                    // ver los remarks de la clase).
                    if (version >= TinajaPackSchema && n != 1 && n != BroteTwoLeaves)
                    {
                        throw new LevelFormatException(at + ": el n del Brote es 1 o 2 (Brote de dos hojas; D-023, punto 5).");
                    }

                    if (version < TinajaPackSchema && n == BroteTwoLeaves)
                    {
                        throw new LevelFormatException(at + ": el Brote de dos hojas (n = 2) necesita pack_schema 3 (D-023, punto 5).");
                    }
                }

                try
                {
                    specs[i] = new MechanicSpec(type, key, partner, n, k, key2);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    throw new LevelFormatException(at + ": " + e.Message, e);
                }
            }

            return specs;
        }

        // Tipos de attrs que solo existen desde pack_schema 3: la Tinaja y su cuia (D-019, punto 9) y las variantes de la 1.0
        // con tipo propio (D-023, punto 5). El Brote de dos hojas y la Liana de cuenta no son tipos.
        private static bool IsPackSchema3Type(MechanicType type)
        {
            return type == MechanicType.Tinaja || type == MechanicType.Cucharon || type == MechanicType.HojaAtada || type == MechanicType.Flor;
        }

        private static bool HasTinaja(MechanicSpec[] attrs)
        {
            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i].Type == MechanicType.Tinaja)
                {
                    return true;
                }
            }

            return false;
        }

        // murky (bool, por defecto false) y hidden. murky = false exige hidden vacío y murky = true al menos uno (esquema
        // $defs/level allOf/0). Con la Tinaja (solo pack_schema 3; M13) murky = true marca sus ocultas, no Aguas turbias, y el
        // tope es el suyo (propuesta de la tinaja, b.2 regla 1 y c.6). Que la capa sea una dosis y no la cima (R7) lo comprueba
        // BoardBuilder; la máscara exacta de la tinaja y que sea la única fuente de ocultas, TinajaMechanic y LevelValidation.
        private static bool ReadMurky(JsonValue node, int cuiaCount, bool tinaja, string where, out int[][] hidden)
        {
            bool murky = ReadOptionalBool(node, "murky", false, where);
            hidden = ReadHidden(node, cuiaCount, tinaja, where);
            if (murky != (hidden.Length > 0))
            {
                throw new LevelFormatException(where + (murky
                    ? ": murky = true necesita al menos una dosis oculta en 'hidden' (02 §11; M13)."
                    : ": 'hidden' solo puede tener dosis ocultas con murky = true (02 §11; M13)."));
            }

            return murky;
        }

        // hidden (esquema $defs/hidden): lista de pares [cuia, capa], sin repetidos y en rango; ausente o null, vacía. Como mucho
        // MaxHidden en Aguas turbias (02 T2) o, si el tablero lleva la tinaja, LevelValidation.TinajaMaxHiddenUnits: su tope
        // propio [P], que no lee lvl_murky_max_hidden_units (M13 regla 1; propuesta de la tinaja, c.6). Hoy los dos valen 4.
        private static int[][] ReadHidden(JsonValue node, int cuiaCount, bool tinaja, string where)
        {
            JsonValue? list = node.Get("hidden");
            if (list == null || list.IsNull)
            {
                return new int[0][];
            }

            if (list.Kind != JsonKind.Array)
            {
                throw new LevelFormatException(where + ": 'hidden' debe ser una lista de pares [cuia, capa].");
            }

            int count = list.Items.Count;
            if (tinaja && count > LevelValidation.TinajaMaxHiddenUnits)
            {
                throw new LevelFormatException(where + ": 'hidden' tiene " + count.ToString(CultureInfo.InvariantCulture)
                    + " dosis ocultas; la tinaja oculta como mucho " + LevelValidation.TinajaMaxHiddenUnits.ToString(CultureInfo.InvariantCulture)
                    + " (su tope propio, M13 regla 1).");
            }

            if (!tinaja && count > MaxHidden)
            {
                throw new LevelFormatException(where + ": 'hidden' tiene " + count.ToString(CultureInfo.InvariantCulture) + " dosis ocultas; como mucho 4 (02 T2).");
            }

            var hidden = new int[count][];
            for (int h = 0; h < count; h++)
            {
                JsonValue pair = list.Items[h];
                string at = where + ": hidden[" + h.ToString(CultureInfo.InvariantCulture) + "]";
                if (pair.Kind != JsonKind.Array || pair.Items.Count != 2)
                {
                    throw new LevelFormatException(at + " debe ser un par [cuia, capa].");
                }

                int cuia = AsInt(pair.Items[0], "hidden", where);
                int layer = AsInt(pair.Items[1], "hidden", where);
                if (cuia < 0 || cuia >= cuiaCount || layer < 0 || layer >= Cuia.MaxCapacity)
                {
                    throw new LevelFormatException(at + " = [" + cuia.ToString(CultureInfo.InvariantCulture) + ", "
                        + layer.ToString(CultureInfo.InvariantCulture) + "] está fuera del tablero.");
                }

                for (int j = 0; j < h; j++)
                {
                    if (hidden[j][0] == cuia && hidden[j][1] == layer)
                    {
                        throw new LevelFormatException(at + " repite la dosis [" + cuia.ToString(CultureInfo.InvariantCulture) + ", "
                            + layer.ToString(CultureInfo.InvariantCulture) + "].");
                    }
                }

                hidden[h] = new[] { cuia, layer };
            }

            return hidden;
        }

        // relief (08 §4.1; esquema relief_board): lista de 0 a 2 tableros {type, caps, cuias, attrs?, hidden?, removed?, par,
        // bot_fail}, en orden de prioridad. Cada uno se construye con sus attrs, las units_per_color del nivel y, si lleva la
        // tinaja (solo pack_schema 3), su máscara en hidden; sin la tinaja, sin ocultos (Aguas turbias incluidas). Y se valida.
        private static ReliefBoard[] ReadRelief(JsonValue node, UnitsPerColor units, int version, string where)
        {
            JsonValue? list = node.Get("relief");
            if (list == null || list.IsNull)
            {
                return new ReliefBoard[0];
            }

            if (list.Kind != JsonKind.Array || list.Items.Count > 2)
            {
                throw new LevelFormatException(where + ": 'relief' debe ser una lista de como mucho 2 tableros.");
            }

            var result = new ReliefBoard[list.Items.Count];
            for (int i = 0; i < result.Length; i++)
            {
                JsonValue entry = list.Items[i];
                string at = where + ".relief[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (entry.Kind != JsonKind.Object)
                {
                    throw new LevelFormatException(at + ": cada variante debe ser un objeto.");
                }

                string type = RequireString(entry, "type", at);
                if (type != ReliefBoard.TypePlusEmpty && type != ReliefBoard.TypeMinusColor)
                {
                    throw new LevelFormatException(at + ": 'type' debe ser plus_empty o minus_color.");
                }

                ReadBoard(entry, at, out int[] caps, out int[][] cuias);
                MechanicSpec[] attrs = ReadAttrs(entry, caps.Length, version, at);
                int[][] hidden = ReadReliefHidden(entry, caps.Length, HasTinaja(attrs), version, at);
                JsonValue? parNode = entry.Get("par");
                int? par = parNode == null || parNode.IsNull ? (int?)null : AsInt(parNode, "par", at);
                if (par == null && type == ReliefBoard.TypeMinusColor)
                {
                    throw new LevelFormatException(at + ": minus_color lleva siempre par (03 §13).");
                }

                BuildAndValidate(caps, cuias, attrs, hidden.Length == 0 ? null : hidden, units, at);
                result[i] = new ReliefBoard(type, caps, cuias, attrs, units, par, at, hidden);
            }

            return result;
        }

        // hidden de una variante de relief (esquema relief_board y rama del 3 en allOf; decisión del dueño del 2026-10-14, opción
        // a de la revisión de P2a): solo con pack_schema 3 y si y solo si su attrs lleva la tinaja, y entonces no vacía. Que sea
        // exactamente su máscara (todas sus capas menos la boca) lo comprueba TinajaMechanic al construir.
        private static int[][] ReadReliefHidden(JsonValue entry, int cuiaCount, bool tinaja, int version, string where)
        {
            JsonValue? list = entry.Get("hidden");
            bool present = list != null && !list.IsNull;
            if (present && version < TinajaPackSchema)
            {
                throw new LevelFormatException(where + ": 'hidden' en una variante de relief necesita pack_schema 3 y la tinaja (08 §4.1).");
            }

            if (present && !tinaja)
            {
                throw new LevelFormatException(where + ": una variante de relief solo lleva 'hidden' con la tinaja (su máscara); sin ella va sin dosis ocultas (08 §4.1).");
            }

            int[][] hidden = ReadHidden(entry, cuiaCount, tinaja, where);
            if (tinaja && hidden.Length == 0)
            {
                throw new LevelFormatException(where + ": una variante de relief con la tinaja lleva su máscara en 'hidden' (08 §4.1; M13).");
            }

            return hidden;
        }

        // difficulty.label (08 §4.1): normal, hard o very_hard; sin etiqueta (subconjunto v0) es normal.
        private static string ReadDifficultyLabel(JsonValue difficulty, string where)
        {
            JsonValue? value = difficulty.Get("label");
            if (value == null || value.IsNull)
            {
                return LevelDefinition.LabelNormal;
            }

            if (value.Kind == JsonKind.String)
            {
                string label = value.AsString();
                if (label == LevelDefinition.LabelNormal || label == LevelDefinition.LabelHard || label == LevelDefinition.LabelVeryHard)
                {
                    return label;
                }
            }

            throw new LevelFormatException(where + ": 'difficulty.label' debe ser normal, hard o very_hard (08 §4.1).");
        }

        // difficulty.band (03 §12): comfort, challenge, recovery o milestone; sin banda (subconjunto v0), null.
        private static string? ReadBand(JsonValue difficulty, string where)
        {
            JsonValue? value = difficulty.Get("band");
            if (value == null || value.IsNull)
            {
                return null;
            }

            if (value.Kind == JsonKind.String && Array.IndexOf(Bands, value.AsString()) >= 0)
            {
                return value.AsString();
            }

            throw new LevelFormatException(where + ": 'difficulty.band' debe ser comfort, challenge, recovery o milestone (03 §12).");
        }

        // difficulty.par_approx [P]: solo con pack_schema 2 (con el 1 el esquema la prohíbe); si falta, el par es exacto.
        private static bool ReadParApprox(JsonValue difficulty, int version, string where)
        {
            JsonValue? value = difficulty.Get("par_approx");
            if (value == null)
            {
                return false;
            }

            if (version < 2)
            {
                throw new LevelFormatException(where + ": 'difficulty.par_approx' necesita pack_schema 2 (contrato 21).");
            }

            if (value.Kind != JsonKind.Bool)
            {
                throw new LevelFormatException(where + ": 'difficulty.par_approx' debe ser un booleano.");
            }

            return value.AsBool();
        }

        // intro_mechanic (08 §4.1; contrato 21): null o una mecánica que se estrena; chica, alta, matapi y murky solo desde
        // pack_schema 2, y tinaja, liana_n, hoja_atada, brote_n2 y flor solo con el 3.
        private static string? ReadIntroMechanic(JsonValue node, int version, string where)
        {
            JsonValue? value = node.Get("intro_mechanic");
            if (value == null || value.IsNull)
            {
                return null;
            }

            if (value.Kind == JsonKind.String)
            {
                string intro = value.AsString();
                if (Array.IndexOf(IntroMechanics1, intro) >= 0)
                {
                    return intro;
                }

                if (Array.IndexOf(IntroMechanics2, intro) >= 0)
                {
                    if (version < 2)
                    {
                        throw new LevelFormatException(where + ": 'intro_mechanic' " + intro + " necesita pack_schema 2 (contrato 21).");
                    }

                    return intro;
                }

                if (Array.IndexOf(IntroMechanics3, intro) >= 0)
                {
                    if (version < TinajaPackSchema)
                    {
                        throw new LevelFormatException(where + ": 'intro_mechanic' " + intro + " necesita pack_schema 3 (D-019 y D-023, punto 5; contrato 21).");
                    }

                    return intro;
                }
            }

            throw new LevelFormatException(where + ": 'intro_mechanic' debe ser null o hoja, brote, liana, matapi, remolino, barro, lluvia, pirana, chica, alta, murky, tinaja, liana_n, hoja_atada, brote_n2 o flor.");
        }

        private static bool ReadOptionalBool(JsonValue node, string key, bool fallback, string where)
        {
            JsonValue? value = node.Get(key);
            if (value == null)
            {
                return fallback;
            }

            if (value.Kind != JsonKind.Bool)
            {
                throw new LevelFormatException(where + ": '" + key + "' debe ser un booleano.");
            }

            return value.AsBool();
        }

        private static int OptionalInt(JsonValue node, string key, int fallback, string where)
        {
            JsonValue? value = node.Get(key);
            return value == null ? fallback : AsInt(value, key, where);
        }

        private static string RequireWaterBase(JsonValue node, string where)
        {
            string value = RequireString(node, "water_base", where);
            if (value != "black" && value != "white" && value != "clear")
            {
                throw new LevelFormatException(where + ": 'water_base' debe ser black, white o clear (glosario §11).");
            }

            return value;
        }

        private static JsonValue RequireArray(JsonValue node, string key, string where)
        {
            JsonValue? value = node.Get(key);
            if (value == null || value.Kind != JsonKind.Array)
            {
                throw new LevelFormatException(where + ": '" + key + "' debe ser una lista.");
            }

            return value;
        }

        private static string RequireString(JsonValue node, string key, string where)
        {
            JsonValue? value = node.Get(key);
            if (value == null || value.Kind != JsonKind.String)
            {
                throw new LevelFormatException(where + ": falta la cadena '" + key + "'.");
            }

            return value.AsString();
        }

        private static int RequireInt(JsonValue node, string key, string where)
        {
            JsonValue? value = node.Get(key);
            if (value == null)
            {
                throw new LevelFormatException(where + ": falta el entero '" + key + "'.");
            }

            return AsInt(value, key, where);
        }

        private static int AsInt(JsonValue value, string key, string where)
        {
            try
            {
                return value.AsInt();
            }
            catch (FormatException e)
            {
                throw new LevelFormatException(where + ": '" + key + "' debe ser un entero.", e);
            }
        }

        private static int[] ReadIntList(JsonValue array, string key, string where)
        {
            var result = new int[array.Items.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = AsInt(array.Items[i], key, where);
            }

            return result;
        }

        private static string[] ReadStringList(JsonValue node, string key, string where)
        {
            JsonValue array = RequireArray(node, key, where);
            var result = new string[array.Items.Count];
            for (int i = 0; i < result.Length; i++)
            {
                if (array.Items[i].Kind != JsonKind.String)
                {
                    throw new LevelFormatException(where + ": '" + key + "' debe ser una lista de cadenas.");
                }

                result[i] = array.Items[i].AsString();
            }

            return result;
        }
    }
}
