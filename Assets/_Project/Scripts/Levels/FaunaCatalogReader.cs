#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>El catálogo de fauna no cumple el formato <c>af-fauna/0</c> (guion de la intro §4.3).</summary>
    public sealed class FaunaFormatException : Exception
    {
        /// <summary>Crea la excepción.</summary>
        public FaunaFormatException(string message)
            : base(message)
        {
        }

        /// <summary>Crea la excepción con la causa.</summary>
        public FaunaFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Lee y valida el catálogo de miquitos <c>af-fauna/0</c> (guion de la intro §4.3) con el lector JSON del cliente
    /// (<see cref="JsonParser"/>): ids únicos; valores de <c>esqueleto</c>, <c>modelo</c>, <c>locomocion</c> e
    /// <c>iluminacion</c> conocidos; colores <c>#RRGGBB</c>; <c>nivel_tarjeta</c> dentro de su tramo; ranuras de arte
    /// <c>fauna_&lt;id&gt;_partes</c> y <c>fauna_&lt;id&gt;_retrato</c>; los <c>mundos</c> de cada especie y de cada tramo
    /// <c>sin_fauna</c> son justo los que cruza su tramo según la tabla de mundos (<see cref="WorldTable"/>, glosario §8); y
    /// los tramos de las especies y de <c>sin_fauna</c> cubren 1-1.000 sin solapes ni huecos.
    /// </summary>
    public static class FaunaCatalogReader
    {
        private static readonly string[] PaletteKeys = { "cuerpo", "extremidades", "cara", "hocico", "gorra", "punta_cola" };

        /// <summary>Lee el catálogo.</summary>
        /// <exception cref="FaunaFormatException">El JSON no es válido o no cumple el formato.</exception>
        public static FaunaCatalog Read(string json)
        {
            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (FormatException e)
            {
                throw new FaunaFormatException("El catálogo de fauna no es JSON válido: " + e.Message, e);
            }

            try
            {
                return ReadRoot(root);
            }
            catch (FormatException e)
            {
                throw new FaunaFormatException("Catálogo de fauna: " + e.Message, e);
            }
        }

        private static FaunaCatalog ReadRoot(JsonValue root)
        {
            if (root.Kind != JsonKind.Object)
            {
                throw new FaunaFormatException("La raíz del catálogo de fauna debe ser un objeto.");
            }

            string format = RequireString(root, "formato", "catálogo");
            if (!string.Equals(format, FaunaCatalog.Format, StringComparison.Ordinal))
            {
                throw new FaunaFormatException("formato '" + format + "' no admitido (este cliente lee " + FaunaCatalog.Format + ").");
            }

            JsonValue? list = root.Get("especies");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw new FaunaFormatException("'especies' debe ser una lista no vacía.");
            }

            var species = new FaunaSpecies[list.Items.Count];
            for (int i = 0; i < species.Length; i++)
            {
                FaunaSpecies sp = ReadSpecies(list.Items[i], i);
                for (int k = 0; k < i; k++)
                {
                    if (string.Equals(species[k].Id, sp.Id, StringComparison.Ordinal))
                    {
                        throw new FaunaFormatException("el id '" + sp.Id + "' está repetido.");
                    }
                }

                species[i] = sp;
            }

            var gaps = new List<FaunaGap>();
            JsonValue? gapList = root.Get("sin_fauna");
            if (gapList != null)
            {
                if (gapList.Kind != JsonKind.Array)
                {
                    throw new FaunaFormatException("'sin_fauna' debe ser una lista.");
                }

                for (int i = 0; i < gapList.Items.Count; i++)
                {
                    JsonValue g = gapList.Items[i];
                    string where = "sin_fauna[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    if (g.Kind != JsonKind.Object)
                    {
                        throw new FaunaFormatException(where + ": debe ser un objeto.");
                    }

                    int[] worlds = ReadWorlds(g, where);
                    ReadRange(g, where, out int from, out int to);
                    CheckWorlds(worlds, from, to, where);
                    gaps.Add(new FaunaGap(worlds, from, to));
                }
            }

            CheckCoverage(species, gaps);
            return new FaunaCatalog(species, gaps.ToArray());
        }

        private static FaunaSpecies ReadSpecies(JsonValue node, int position)
        {
            string where = "especies[" + position.ToString(CultureInfo.InvariantCulture) + "]";
            if (node.Kind != JsonKind.Object)
            {
                throw new FaunaFormatException(where + ": cada especie debe ser un objeto.");
            }

            string id = RequireString(node, "id", where);
            if (!IsId(id))
            {
                throw new FaunaFormatException(where + ": el id '" + id + "' solo admite minúsculas, cifras y '_'.");
            }

            where = id;
            string scientific = RequireString(node, "cientifico", where);
            bool showScientific = RequireBool(node, "mostrar_cientifico", where);

            JsonValue? namesNode = node.Get("nombres");
            if (namesNode == null || namesNode.Kind != JsonKind.Object)
            {
                throw new FaunaFormatException(where + ": 'nombres' debe ser un objeto.");
            }

            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < namesNode.Members.Count; i++)
            {
                names[namesNode.Members[i].Key] = namesNode.Members[i].Value.AsString();
            }

            if (!names.ContainsKey("es"))
            {
                throw new FaunaFormatException(where + ": 'nombres' necesita el nombre en español ('es').");
            }

            FaunaSkeleton skeleton = ParseSkeleton(RequireString(node, "esqueleto", where), where);
            FaunaModel model = ParseModel(RequireString(node, "modelo", where), where);
            FaunaLocomotion locomotion = ParseLocomotion(RequireString(node, "locomocion", where), where);
            int size = RequireInt(node, "tam_dp", where);
            if (size < 16 || size > 96)
            {
                throw new FaunaFormatException(where + ": 'tam_dp' fuera de 16..96.");
            }

            int troop = RequireInt(node, "tropa_intro", where);
            if (troop < 1 || troop > 8)
            {
                throw new FaunaFormatException(where + ": 'tropa_intro' fuera de 1..8.");
            }

            string[] traits = ReadStrings(node, "rasgos_reposo", where);
            FaunaPalette palette = ReadPalette(node, where);
            FaunaLighting lighting = ParseLighting(RequireString(node, "iluminacion", where), where);
            int[] worlds = ReadWorlds(node, where);
            ReadRange(node, where, out int from, out int to);
            CheckWorlds(worlds, from, to, where);
            int card = RequireInt(node, "nivel_tarjeta", where);
            if (card < from || card > to)
            {
                throw new FaunaFormatException(where + ": 'nivel_tarjeta' " + card.ToString(CultureInfo.InvariantCulture) + " fuera de su tramo.");
            }

            JsonValue? iucnNode = node.Get("uicn");
            string? iucn = iucnNode == null || iucnNode.IsNull ? null : iucnNode.AsString();
            bool threatened = RequireBool(node, "amenazada", where);
            string fact = RequireString(node, "dato_tarjeta", where);

            JsonValue? slots = node.Get("ranuras_arte");
            if (slots == null || slots.Kind != JsonKind.Object)
            {
                throw new FaunaFormatException(where + ": 'ranuras_arte' debe ser un objeto.");
            }

            string parts = RequireString(slots, "partes", where);
            string portrait = RequireString(slots, "retrato", where);
            if (!string.Equals(parts, "fauna_" + id + "_partes", StringComparison.Ordinal)
                || !string.Equals(portrait, "fauna_" + id + "_retrato", StringComparison.Ordinal))
            {
                throw new FaunaFormatException(where + ": las ranuras de arte deben ser fauna_" + id + "_partes y fauna_" + id + "_retrato.");
            }

            string review = RequireString(node, "revision_asesor", where);
            string[] avoid = ReadStrings(node, "evitar", where);
            var species = new FaunaSpecies(id, scientific, showScientific, names, skeleton, model, locomotion, size, troop, traits, palette,
                lighting, worlds, from, to, card, iucn, threatened, fact, parts, portrait, review, avoid);
            species.PairPalette = node.Get("paleta_pareja") == null ? null : ReadPalette(node, where, "paleta_pareja");
            return species;
        }

        private static FaunaPalette ReadPalette(JsonValue node, string where, string key = "paleta")
        {
            JsonValue? p = node.Get(key);
            if (p == null || p.Kind != JsonKind.Object)
            {
                throw new FaunaFormatException(where + ": '" + key + "' debe ser un objeto.");
            }

            var c = new int[PaletteKeys.Length];
            for (int i = 0; i < PaletteKeys.Length; i++)
            {
                c[i] = ParseColor(RequireString(p, PaletteKeys[i], where + "." + key), where + "." + key + "." + PaletteKeys[i]);
            }

            JsonValue? tailNode = p.Get("cola");
            int tail = tailNode == null ? c[0] : ParseColor(tailNode.AsString(), where + "." + key + ".cola");
            return new FaunaPalette(c[0], c[1], c[2], c[3], c[4], tail, c[5]);
        }

        /// <summary>Color <c>#RRGGBB</c> como 0xRRGGBB.</summary>
        /// <exception cref="FaunaFormatException">No es <c>#RRGGBB</c>.</exception>
        public static int ParseColor(string text, string where)
        {
            if (text == null || text.Length != 7 || text[0] != '#'
                || !int.TryParse(text.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb))
            {
                throw new FaunaFormatException(where + ": '" + text + "' no es un color #RRGGBB.");
            }

            return rgb;
        }

        // Los tramos de las especies y de sin_fauna cubren 1..1.000 sin solapes ni huecos.
        private static void CheckCoverage(FaunaSpecies[] species, List<FaunaGap> gaps)
        {
            int count = species.Length + gaps.Count;
            var from = new int[count];
            var to = new int[count];
            for (int i = 0; i < species.Length; i++)
            {
                from[i] = species[i].FromLevel;
                to[i] = species[i].ToLevel;
            }

            for (int i = 0; i < gaps.Count; i++)
            {
                from[species.Length + i] = gaps[i].FromLevel;
                to[species.Length + i] = gaps[i].ToLevel;
            }

            Array.Sort(from, to);
            int expected = 1;
            for (int i = 0; i < count; i++)
            {
                if (from[i] < expected)
                {
                    throw new FaunaFormatException("el tramo " + Range(from[i], to[i]) + " se solapa con el anterior.");
                }

                if (from[i] > expected)
                {
                    throw new FaunaFormatException("hay un hueco sin especie ni sin_fauna en " + Range(expected, from[i] - 1) + ".");
                }

                expected = to[i] + 1;
            }

            if (expected != FaunaCatalog.LastLevel + 1)
            {
                throw new FaunaFormatException("los tramos terminan en " + (expected - 1).ToString(CultureInfo.InvariantCulture) + " y deben llegar a 1.000.");
            }
        }

        private static string Range(int a, int b)
        {
            return a.ToString(CultureInfo.InvariantCulture) + "-" + b.ToString(CultureInfo.InvariantCulture);
        }

        private static void ReadRange(JsonValue node, string where, out int from, out int to)
        {
            from = RequireInt(node, "desde_nivel", where);
            to = RequireInt(node, "hasta_nivel", where);
            if (from < 1 || to > FaunaCatalog.LastLevel || from > to)
            {
                throw new FaunaFormatException(where + ": el tramo " + Range(from, to) + " no es válido (1..1.000, desde ≤ hasta).");
            }
        }

        // Los mundos de una especie (o de un tramo sin fauna) son exactamente los que cruza su tramo de niveles según la tabla
        // de mundos (glosario §8): un tramo que se sale de sus mundos dejaría esos niveles en dosel sin avisar (LivesIn falla en
        // IntroCast.Resolve), y un mundo de más la haría «vivir» donde nunca sale.
        private static void CheckWorlds(int[] worlds, int from, int to, string where)
        {
            int first = WorldTable.WorldOf(from);
            int last = WorldTable.WorldOf(to);
            for (int w = first; w <= last; w++)
            {
                if (Array.IndexOf(worlds, w) < 0)
                {
                    throw new FaunaFormatException(where + ": el tramo " + Range(from, to) + " cruza el mundo " + w.ToString(CultureInfo.InvariantCulture)
                        + " (glosario §8), que no está en 'mundos'.");
                }
            }

            for (int i = 0; i < worlds.Length; i++)
            {
                if (worlds[i] < first || worlds[i] > last)
                {
                    throw new FaunaFormatException(where + ": el mundo " + worlds[i].ToString(CultureInfo.InvariantCulture) + " no tiene niveles en el tramo "
                        + Range(from, to) + " (glosario §8).");
                }
            }
        }

        private static int[] ReadWorlds(JsonValue node, string where)
        {
            JsonValue? list = node.Get("mundos");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw new FaunaFormatException(where + ": 'mundos' debe ser una lista no vacía.");
            }

            var worlds = new int[list.Items.Count];
            for (int i = 0; i < worlds.Length; i++)
            {
                worlds[i] = list.Items[i].AsInt();
                if (worlds[i] < 1 || worlds[i] > 13)
                {
                    throw new FaunaFormatException(where + ": el mundo " + worlds[i].ToString(CultureInfo.InvariantCulture) + " no existe (1..13).");
                }
            }

            return worlds;
        }

        private static string[] ReadStrings(JsonValue node, string key, string where)
        {
            JsonValue? list = node.Get(key);
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw new FaunaFormatException(where + ": '" + key + "' debe ser una lista.");
            }

            var result = new string[list.Items.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = list.Items[i].AsString();
            }

            return result;
        }

        private static FaunaSkeleton ParseSkeleton(string value, string where)
        {
            switch (value)
            {
                case "A":
                    return FaunaSkeleton.A;
                case "B":
                    return FaunaSkeleton.B;
                case "C":
                    return FaunaSkeleton.C;
                case "D":
                    return FaunaSkeleton.D;
                default:
                    throw new FaunaFormatException(where + ": esqueleto '" + value + "' desconocido (A, B, C o D).");
            }
        }

        private static FaunaModel ParseModel(string value, string where)
        {
            switch (value)
            {
                case "fila":
                    return FaunaModel.Row;
                case "cuelga":
                    return FaunaModel.Hang;
                case "sacude":
                    return FaunaModel.Shake;
                default:
                    throw new FaunaFormatException(where + ": modelo '" + value + "' desconocido (fila, cuelga o sacude).");
            }
        }

        private static FaunaLocomotion ParseLocomotion(string value, string where)
        {
            switch (value)
            {
                case "corre":
                    return FaunaLocomotion.Run;
                case "salta":
                    return FaunaLocomotion.Jump;
                case "camina":
                    return FaunaLocomotion.Walk;
                case "trepa":
                    return FaunaLocomotion.Climb;
                default:
                    throw new FaunaFormatException(where + ": locomoción '" + value + "' desconocida (corre, salta, camina o trepa).");
            }
        }

        private static FaunaLighting ParseLighting(string value, string where)
        {
            switch (value)
            {
                case "dia":
                    return FaunaLighting.Day;
                case "noche":
                    return FaunaLighting.Night;
                default:
                    throw new FaunaFormatException(where + ": iluminación '" + value + "' desconocida (dia o noche).");
            }
        }

        private static bool IsId(string id)
        {
            if (id.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private static string RequireString(JsonValue node, string key, string where)
        {
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.String)
            {
                throw new FaunaFormatException(where + ": falta la cadena '" + key + "'.");
            }

            return v.AsString();
        }

        private static int RequireInt(JsonValue node, string key, string where)
        {
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.Number)
            {
                throw new FaunaFormatException(where + ": falta el número '" + key + "'.");
            }

            return v.AsInt();
        }

        private static bool RequireBool(JsonValue node, string key, string where)
        {
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.Bool)
            {
                throw new FaunaFormatException(where + ": falta el booleano '" + key + "'.");
            }

            return v.AsBool();
        }
    }
}
