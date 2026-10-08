#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lee un paquete de niveles <c>af-level/1</c> (GDD 08 §4.1) o el subconjunto documentado de v0
    /// (<c>"subset_of": "af-level/1"</c>, docs/dev/prototipo-v0.md b) y produce <see cref="LevelPack"/>.
    /// </summary>
    /// <remarks>
    /// <para>Acepta <c>pack_schema</c> = 1 o <c>subset_of</c> = <c>af-level/1</c>; rechaza cualquier otro
    /// <c>pack_schema</c> (08 §4.1: el cliente rechaza esquemas desconocidos).</para>
    /// <para>El cliente v0 no tiene mecánicas (02 §9) ni Aguas turbias (02 §11): un nivel con <c>attrs</c> no nulos,
    /// Barro (15), <c>murky</c> = true u ocultos se rechaza con <see cref="LevelFormatException"/>.</para>
    /// <para>No valida lo que valida el pipeline (resolubilidad, par, bot): solo lo que el cliente necesita para
    /// construir el tablero sin sorpresas (longitudes, capacidades, colores y <c>U_c</c>).</para>
    /// </remarks>
    public static class LevelPackReader
    {
        /// <summary>Valor de <c>subset_of</c> que admite el cliente v0.</summary>
        public const string SubsetOf = "af-level/1";

        /// <summary>Única versión de <c>pack_schema</c> que admite el cliente.</summary>
        public const int SupportedPackSchema = 1;

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

            CheckSchema(root);
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
                LevelDefinition level = ReadLevel(levelsNode.Items[i], packId, i);
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

        private static void CheckSchema(JsonValue root)
        {
            JsonValue? schema = root.Get("pack_schema");
            JsonValue? subset = root.Get("subset_of");
            if (schema != null)
            {
                int version = AsInt(schema, "pack_schema", "paquete");
                if (version != SupportedPackSchema)
                {
                    throw new LevelFormatException("pack_schema " + version.ToString(CultureInfo.InvariantCulture)
                        + " no admitido (este cliente lee la versión " + SupportedPackSchema.ToString(CultureInfo.InvariantCulture) + ").");
                }

                return;
            }

            if (subset != null && subset.Kind == JsonKind.String && string.Equals(subset.AsString(), SubsetOf, StringComparison.Ordinal))
            {
                return;
            }

            throw new LevelFormatException("El paquete no declara pack_schema = 1 ni subset_of = \"" + SubsetOf + "\".");
        }

        private static LevelDefinition ReadLevel(JsonValue node, string packId, int position)
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
            int unitsPerColor = RequireInt(node, "units_per_color", where);
            if (unitsPerColor < 1 || unitsPerColor > Cuia.MaxCapacity)
            {
                throw new LevelFormatException(where + ": 'units_per_color' fuera de 1..6.");
            }

            int[] caps = ReadIntList(RequireArray(node, "caps", where), "caps", where);
            JsonValue cuiasNode = RequireArray(node, "cuias", where);
            if (caps.Length < 1 || caps.Length > BoardState.MaxCuias)
            {
                throw new LevelFormatException(where + ": 'caps' debe tener entre 1 y 16 cuias.");
            }

            if (cuiasNode.Items.Count != caps.Length)
            {
                throw new LevelFormatException(where + ": 'cuias' y 'caps' deben tener la misma longitud.");
            }

            var counts = new int[Cuia.MaxColor + 1];
            var cuias = new int[caps.Length][];
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
                    if (color == Cuia.MudColor)
                    {
                        throw new LevelFormatException(where + ": Barro (15) necesita las mecánicas de 02 §9, que el cliente v0 no tiene.");
                    }

                    if (color < Cuia.MinColor || color > Cuia.MaxTintColor)
                    {
                        throw new LevelFormatException(where + ": color " + color.ToString(CultureInfo.InvariantCulture) + " fuera de los tintes 1..14.");
                    }

                    counts[color]++;
                }

                cuias[i] = doses;
            }

            for (int color = Cuia.MinColor; color <= Cuia.MaxTintColor; color++)
            {
                if (counts[color] != 0 && counts[color] != unitsPerColor)
                {
                    throw new LevelFormatException(where + ": el color " + color.ToString(CultureInfo.InvariantCulture)
                        + " tiene " + counts[color].ToString(CultureInfo.InvariantCulture) + " dosis y units_per_color es "
                        + unitsPerColor.ToString(CultureInfo.InvariantCulture) + " (02 R3).");
                }
            }

            CheckNoMechanics(node, caps.Length, where);

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
            return new LevelDefinition(id, level, world, waterBase, palette, unitsPerColor, caps, cuias, par, label);
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

        private static void CheckNoMechanics(JsonValue node, int cuiaCount, string where)
        {
            JsonValue? attrs = node.Get("attrs");
            if (attrs != null)
            {
                if (attrs.Kind != JsonKind.Array || attrs.Items.Count != cuiaCount)
                {
                    throw new LevelFormatException(where + ": 'attrs' debe tener una entrada por cuia.");
                }

                for (int i = 0; i < attrs.Items.Count; i++)
                {
                    if (!attrs.Items[i].IsNull)
                    {
                        throw new LevelFormatException(where + ": las mecánicas (attrs) de 02 §9 aún no existen en el cliente v0.");
                    }
                }
            }

            JsonValue? murky = node.Get("murky");
            if (murky != null && murky.Kind == JsonKind.Bool && murky.AsBool())
            {
                throw new LevelFormatException(where + ": Aguas turbias (02 §11) aún no existe en el cliente v0.");
            }

            JsonValue? hidden = node.Get("hidden");
            if (hidden != null && (hidden.Kind != JsonKind.Array || hidden.Items.Count != 0))
            {
                throw new LevelFormatException(where + ": las dosis ocultas (02 §11) aún no existen en el cliente v0.");
            }
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
