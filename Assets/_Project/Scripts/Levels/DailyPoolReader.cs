#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>Qué bandas debe traer el pool para que <see cref="DailyPoolReader"/> lo acepte.</summary>
    public enum DailyPoolCoverage
    {
        /// <summary>
        /// Por defecto y el único modo del juego: las 3 bandas, cada una con sus 730 días (2.190 tableros; 03 §16). Un pool real
        /// al que le falte una banda dejaría sin Reto a los jugadores de esa banda, así que se rechaza.
        /// </summary>
        AllBands = 0,

        /// <summary>
        /// Solo para tests y herramientas: al menos una banda, y cada banda presente con sus 730 días. El formato de cada
        /// tablero se valida igual que en <see cref="AllBands"/>; lo único que cambia es que una banda puede faltar entera.
        /// </summary>
        PresentBands = 1,
    }

    /// <summary>
    /// Lee el pool del Reto del río (<c>daily_pool_v1.json</c>; 03 §16; 08 §4.1 con <c>kind</c> = <c>daily</c>; esquema
    /// <c>schemas/af-level-1.schema.json</c>, rama <c>daily</c> de <c>allOf</c>) con <see cref="JsonParser"/> y produce
    /// <see cref="DailyPool"/>. Nota de diseño <c>docs/dev/reto-del-rio.md</c> §3 (fila <c>AF.Levels</c>) y §13.
    /// </summary>
    /// <remarks>
    /// <para>Falla cerrado con <see cref="LevelFormatException"/>, con el id del tablero o su posición en el mensaje:</para>
    /// <list type="bullet">
    /// <item>cabecera: <c>kind</c> distinto de <c>daily</c>; <c>pack_schema</c> ausente o fuera de
    /// <see cref="LevelPackReader.MinPackSchema"/>-<see cref="LevelPackReader.SupportedPackSchema"/> (08 §4.4);</item>
    /// <item>cada tablero: sin <c>slot</c> o con <c>level</c>; banda fuera de 1-3 o día fuera de 0-729; <c>id</c> distinto de
    /// <c>d{b}_n{nnnn}</c> o <c>seed</c> distinta de <c>amazonfill:v1:daily:b{b}:d{ddd}</c> para su slot, con el padding exacto
    /// (la semilla con otro padding da otro hash y no reproduce el tablero; 03 §17 #10); <c>solution</c>, <c>safe_path</c> o
    /// <c>qa</c> (el esquema los excluye en todo nivel); <c>relief</c> ausente o no vacío (03 §16: el Reto no tiene alivio);</item>
    /// <item>el pool: un slot o un id repetido; una banda incompleta (menos de 730 días) y, en el modo por defecto
    /// (<see cref="DailyPoolCoverage.AllBands"/>), una banda que falta.</item>
    /// </list>
    /// <para><c>difficulty</c> es opcional en <c>daily</c> según el esquema (el pool se publica sin métricas, nota §13): si viene,
    /// se leen <c>par</c> (≥ 1) y <c>label</c>; si no, par 0 y etiqueta <c>normal</c>. <c>world</c> también es opcional: 0 si
    /// falta.</para>
    /// <para>[P] Sin mecánicas ni dosis ocultas: el pool no las lleva (nota §13; QA-DAILY #6 y #7) y el lector de mecánicas de
    /// <see cref="LevelPackReader"/> es privado. Un tablero con un <c>attrs</c> no nulo, <c>murky</c> = true, <c>hidden</c> no
    /// vacío o <c>intro_mechanic</c> no nulo se rechaza en lugar de leerse a medias. Si el Reto llega a usar mecánicas, el enganche
    /// es exponer como <c>internal</c> <c>LevelPackReader.ReadAttrs</c> y <c>ReadMurky</c> y llamarlos desde aquí.</para>
    /// <para>Cada tablero se construye con <see cref="BoardBuilder"/> y se comprueba con <see cref="LevelValidation"/>, como en
    /// <see cref="LevelPackReader"/>.</para>
    /// </remarks>
    public static class DailyPoolReader
    {
        // Campos que el esquema excluye en todo nivel ("solution": false, "safe_path": false, "qa": false; 08 §4.1: las
        // soluciones nunca viajan en el paquete).
        private static readonly string[] ForbiddenLevelFields = { "solution", "safe_path", "qa" };

        private static readonly string[] Labels = { LevelDefinition.LabelNormal, LevelDefinition.LabelHard, LevelDefinition.LabelVeryHard };

        /// <summary>Lee el pool en el modo por defecto, <see cref="DailyPoolCoverage.AllBands"/>.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato del pool.</exception>
        public static DailyPool Read(string json)
        {
            return Read(json, DailyPoolCoverage.AllBands);
        }

        /// <summary>Lee el pool exigiendo las bandas de <paramref name="coverage"/>.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato del pool.</exception>
        public static DailyPool Read(string json, DailyPoolCoverage coverage)
        {
            if (coverage != DailyPoolCoverage.AllBands && coverage != DailyPoolCoverage.PresentBands)
            {
                throw new ArgumentOutOfRangeException(nameof(coverage));
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("El pool del Reto del río no es JSON válido: " + e.Message, e);
            }

            if (root.Kind != JsonKind.Object)
            {
                throw new LevelFormatException("La raíz del pool del Reto del río debe ser un objeto.");
            }

            string packId = RequireString(root, "pack_id", "pool");
            int version = CheckSchema(root, packId);
            string kind = RequireString(root, "kind", packId);
            if (!string.Equals(kind, DailyPool.KindDaily, StringComparison.Ordinal))
            {
                throw new LevelFormatException(packId + ": 'kind' es '" + kind + "' y el pool del Reto del río debe ser 'daily' (08 §4.1).");
            }

            JsonValue? levelsNode = root.Get("levels");
            if (levelsNode == null || levelsNode.Kind != JsonKind.Array || levelsNode.Items.Count == 0)
            {
                throw new LevelFormatException(packId + ": 'levels' debe ser una lista no vacía.");
            }

            var boards = new LevelDefinition?[]?[DailyPool.MaxBand];
            var counts = new int[DailyPool.MaxBand];
            var byId = new Dictionary<string, LevelDefinition>(levelsNode.Items.Count, StringComparer.Ordinal);
            var slotOf = new Dictionary<LevelDefinition, int>(levelsNode.Items.Count);
            for (int i = 0; i < levelsNode.Items.Count; i++)
            {
                LevelDefinition board = ReadBoard(levelsNode.Items[i], packId, i, out int band, out int day);
                LevelDefinition?[]? row = boards[band - 1];
                if (row == null)
                {
                    row = new LevelDefinition?[DailyPool.Days];
                    boards[band - 1] = row;
                }

                LevelDefinition? previous = row[day];
                if (previous != null)
                {
                    throw new LevelFormatException(packId + ".levels[" + Position(i) + "] (" + board.Id + "): la banda "
                        + Position(band) + " y el día " + Position(day) + " están repetidos (ya los tiene " + previous.Id + ").");
                }

                if (byId.ContainsKey(board.Id))
                {
                    // El id sale del slot, así que no se llega aquí sin un slot repetido; se comprueba igual por si cambia el formato.
                    throw new LevelFormatException(packId + ".levels[" + Position(i) + "]: el id " + board.Id + " está repetido.");
                }

                row[day] = board;
                counts[band - 1]++;
                byId.Add(board.Id, board);
                slotOf.Add(board, (band - DailyPool.MinBand) * DailyPool.Days + day);
            }

            for (int b = 0; b < DailyPool.MaxBand; b++)
            {
                int band = b + DailyPool.MinBand;
                LevelDefinition?[]? present = boards[b];
                if (present == null)
                {
                    if (coverage == DailyPoolCoverage.AllBands)
                    {
                        throw new LevelFormatException(packId + ": falta la banda " + Position(band) + " (el pool del Reto del río trae las 3 bandas con "
                            + Position(DailyPool.Days) + " días cada una; 03 §16).");
                    }

                    continue;
                }

                if (counts[b] != DailyPool.Days)
                {
                    int missing = 0;
                    while (present[missing] != null)
                    {
                        missing++;
                    }

                    throw new LevelFormatException(packId + ": la banda " + Position(band) + " está incompleta: tiene "
                        + Position(counts[b]) + " de " + Position(DailyPool.Days) + " días (falta el día " + Position(missing)
                        + ", " + DailyPool.IdOf(band, missing) + "; 03 §16).");
                }
            }

            return new DailyPool(packId, version, boards, byId, slotOf);
        }

        // pack_schema obligatorio (un pool no admite el subset_of de v0) y entre MinPackSchema y SupportedPackSchema (08 §4.4).
        private static int CheckSchema(JsonValue root, string packId)
        {
            JsonValue? schema = root.Get("pack_schema");
            if (schema == null)
            {
                throw new LevelFormatException(packId + ": el pool del Reto del río debe declarar pack_schema.");
            }

            int version = AsInt(schema, "pack_schema", packId);
            if (version < LevelPackReader.MinPackSchema || version > LevelPackReader.SupportedPackSchema)
            {
                string tail = version > LevelPackReader.SupportedPackSchema ? "; 08 §4.4: hace falta una versión más nueva del juego)." : ").";
                throw new LevelFormatException(packId + ": pack_schema " + Position(version) + " no admitido (este cliente lee las versiones "
                    + Position(LevelPackReader.MinPackSchema) + " a " + Position(LevelPackReader.SupportedPackSchema) + tail);
            }

            return version;
        }

        private static LevelDefinition ReadBoard(JsonValue node, string packId, int position, out int band, out int day)
        {
            string where = packId + ".levels[" + Position(position) + "]";
            if (node.Kind != JsonKind.Object)
            {
                throw new LevelFormatException(where + ": cada tablero debe ser un objeto.");
            }

            JsonValue? idNode = node.Get("id");
            if (idNode != null && idNode.Kind == JsonKind.String)
            {
                where = where + " (" + idNode.AsString() + ")";
            }

            if (node.Has("level"))
            {
                throw new LevelFormatException(where + ": un tablero del Reto lleva 'slot' y no 'level' (esquema, rama daily).");
            }

            JsonValue? slot = node.Get("slot");
            if (slot == null || slot.Kind != JsonKind.Object)
            {
                throw new LevelFormatException(where + ": falta el objeto 'slot' {band, day} (esquema, rama daily).");
            }

            band = RequireInt(slot, "band", where);
            day = RequireInt(slot, "day", where);
            if (band < DailyPool.MinBand || band > DailyPool.MaxBand)
            {
                throw new LevelFormatException(where + ": 'slot.band' " + Position(band) + " fuera de 1-3 (03 §16).");
            }

            if (day < 0 || day >= DailyPool.Days)
            {
                throw new LevelFormatException(where + ": 'slot.day' " + Position(day) + " fuera de 0-729 (03 §16).");
            }

            string id = RequireString(node, "id", where);
            string expectedId = DailyPool.IdOf(band, day);
            if (!string.Equals(id, expectedId, StringComparison.Ordinal))
            {
                throw new LevelFormatException(where + ": el id '" + id + "' no corresponde al slot (banda " + Position(band) + ", día "
                    + Position(day) + "), que pide '" + expectedId + "' (d{b}_n{nnnn}; 03 §16).");
            }

            where = id;
            string seed = RequireString(node, "seed", where);
            string expectedSeed = DailyPool.SeedOf(band, day);
            if (!string.Equals(seed, expectedSeed, StringComparison.Ordinal))
            {
                throw new LevelFormatException(where + ": la semilla '" + seed + "' no corresponde al slot, que pide '" + expectedSeed
                    + "' (amazonfill:v1:daily:b{b}:d{ddd}; 03 §16 y §17 #10).");
            }

            for (int i = 0; i < ForbiddenLevelFields.Length; i++)
            {
                if (node.Has(ForbiddenLevelFields[i]))
                {
                    throw new LevelFormatException(where + ": '" + ForbiddenLevelFields[i] + "' no puede viajar en el paquete (08 §4.1; esquema).");
                }
            }

            int world = OptionalInt(node, "world", 0, where);
            if (world != 0 && (world < 1 || world > 13))
            {
                throw new LevelFormatException(where + ": 'world' fuera de 1-13 (glosario §8).");
            }

            string waterBase = RequireWaterBase(node, where);
            string[] palette = ReadStringList(node, "palette", where);
            UnitsPerColor units = ReadUnits(node, where);
            ReadCuias(node, where, out int[] caps, out int[][] cuias);
            MechanicSpec[] attrs = ReadNoMechanics(node, caps.Length, where);
            ReadNoHidden(node, where);
            ReadRelief(node, where);

            int par = 0;
            string label = LevelDefinition.LabelNormal;
            JsonValue? difficulty = node.Get("difficulty");
            if (difficulty != null)
            {
                if (difficulty.Kind != JsonKind.Object)
                {
                    throw new LevelFormatException(where + ": 'difficulty' debe ser un objeto.");
                }

                par = RequireInt(difficulty, "par", where);
                if (par < 1)
                {
                    throw new LevelFormatException(where + ": 'difficulty.par' debe ser ≥ 1.");
                }

                label = ReadLabel(difficulty, where);
            }

            bool skippable = ReadOptionalBool(node, "skippable", true, where);
            bool hintSupported = ReadOptionalBool(node, "hint_supported", true, where);

            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, null, units);
            if (built.State == null)
            {
                throw new LevelFormatException(where + ": " + built.Error);
            }

            string? reason = LevelValidation.Validate(built.State);
            if (reason != null)
            {
                throw new LevelFormatException(where + ": " + reason);
            }

            return new LevelDefinition(
                id, 0, world, waterBase, palette, units, caps, cuias, attrs, false, null, par, label, null, null, skippable, hintSupported);
        }

        // attrs: ausente, null o una lista de null (una por cuia). [P] Cualquier mecánica se rechaza (ver remarks).
        private static MechanicSpec[] ReadNoMechanics(JsonValue node, int cuiaCount, string where)
        {
            var specs = new MechanicSpec[cuiaCount];
            JsonValue? attrs = node.Get("attrs");
            if (attrs != null && !attrs.IsNull)
            {
                if (attrs.Kind != JsonKind.Array || attrs.Items.Count != cuiaCount)
                {
                    throw new LevelFormatException(where + ": 'attrs' debe tener una entrada por cuia.");
                }

                for (int i = 0; i < cuiaCount; i++)
                {
                    if (!attrs.Items[i].IsNull)
                    {
                        throw new LevelFormatException(where + ": attrs[" + Position(i) + "] lleva una mecánica y el Reto del río no las admite (nota §13; QA-DAILY).");
                    }
                }
            }

            JsonValue? intro = node.Get("intro_mechanic");
            if (intro != null && !intro.IsNull)
            {
                throw new LevelFormatException(where + ": 'intro_mechanic' debe ser null en el Reto del río (sin mecánicas; nota §13).");
            }

            return specs;
        }

        // murky false (o ausente) y hidden vacío (o ausente): el Reto no tiene dosis ocultas (nota §13; QA-DAILY).
        private static void ReadNoHidden(JsonValue node, string where)
        {
            if (ReadOptionalBool(node, "murky", false, where))
            {
                throw new LevelFormatException(where + ": 'murky' debe ser false en el Reto del río (sin dosis ocultas; nota §13).");
            }

            JsonValue? hidden = node.Get("hidden");
            if (hidden != null && !hidden.IsNull && (hidden.Kind != JsonKind.Array || hidden.Items.Count != 0))
            {
                throw new LevelFormatException(where + ": 'hidden' debe estar vacío en el Reto del río (sin dosis ocultas; nota §13).");
            }
        }

        // relief obligatorio (esquema: required) y vacío: el Reto no tiene alivio (03 §16; esquema, "[] ... en el Reto del río").
        private static void ReadRelief(JsonValue node, string where)
        {
            JsonValue? relief = node.Get("relief");
            if (relief == null || relief.Kind != JsonKind.Array)
            {
                throw new LevelFormatException(where + ": 'relief' debe ser una lista vacía en el Reto del río.");
            }

            if (relief.Items.Count != 0)
            {
                throw new LevelFormatException(where + ": 'relief' debe estar vacío en el Reto del río (sin alivio; 03 §16) y trae "
                    + Position(relief.Items.Count) + " variantes.");
            }
        }

        private static string ReadLabel(JsonValue difficulty, string where)
        {
            JsonValue? value = difficulty.Get("label");
            if (value == null)
            {
                return LevelDefinition.LabelNormal;
            }

            if (value.Kind == JsonKind.String && Array.IndexOf(Labels, value.AsString()) >= 0)
            {
                return value.AsString();
            }

            throw new LevelFormatException(where + ": 'difficulty.label' debe ser normal, hard o very_hard.");
        }

        // Copia de LevelPackReader.ReadUnits (privado): un entero 1-6 o un objeto {"1".."16": 1-6}.
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

        // Copia de LevelPackReader.ReadBoard (privado): caps y cuias de la misma longitud (1-16), capacidades 1-6, no más
        // dosis que la capacidad y colores 1-15.
        private static void ReadCuias(JsonValue node, string where, out int[] caps, out int[][] cuias)
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
                    throw new LevelFormatException(where + ": capacidad fuera de 1..6 en la cuia " + Position(i) + ".");
                }

                JsonValue cuiaNode = cuiasNode.Items[i];
                if (cuiaNode.Kind != JsonKind.Array)
                {
                    throw new LevelFormatException(where + ": cada cuia debe ser una lista de dosis.");
                }

                int[] doses = ReadIntList(cuiaNode, "cuias", where);
                if (doses.Length > caps[i])
                {
                    throw new LevelFormatException(where + ": la cuia " + Position(i) + " tiene más dosis que su capacidad.");
                }

                for (int k = 0; k < doses.Length; k++)
                {
                    int color = doses[k];
                    if (color < Cuia.MinColor || color > Cuia.MudColor)
                    {
                        throw new LevelFormatException(where + ": color " + Position(color)
                            + " fuera de 1..15 (tintes 1-14 y Barro 15; el 16 está reservado, 02 R2).");
                    }
                }

                cuias[i] = doses;
            }
        }

        private static string Position(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
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
