#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lee y valida las paradas del Mapa del río <c>af-river-stops/1</c> (08 §4.5; contrato 30; esquema
    /// <c>schemas/af-river-stops-1.schema.json</c>) con el lector JSON del cliente (<see cref="JsonParser"/>), con las mismas
    /// reglas que <c>tools/river-map/stops.py --check</c> y contra la tabla de mundos (<see cref="WorldTable"/>, glosario §8):
    /// <list type="bullet">
    /// <item><description><c>format</c> = <see cref="RiverStopCatalog.Format"/>, <c>source</c> y de 1 a 100 paradas;</description></item>
    /// <item><description><c>n</c> contiguos desde 0 y <c>levels</c> = [10n+1, 10n+10];</description></item>
    /// <item><description><c>world</c> igual al de <see cref="WorldTable.WorldOf"/> en sus diez niveles; <c>gate</c> si y solo si es
    /// la primera parada de su mundo (<see cref="WorldTable.FirstLevel"/>) y <c>close</c> si y solo si es la última de un mundo de
    /// más de un tramo (<see cref="WorldTable.LastLevelOf"/>); y la última parada termina en el último nivel de su mundo (1-200:
    /// el 200), para que cada mundo presente esté completo;</description></item>
    /// <item><description>ids <c>[a-z0-9_]+</c> únicos; <c>country</c> ISO2 y <c>country_name</c> null o
    /// <c>map.stop.&lt;id&gt;.country</c>; km enteros ≥ 0 (<c>km_from_sea</c> admite null); <c>sample</c> conocido;
    /// <c>flag_raise</c> al empezar y al cambiar de país;</description></item>
    /// <item><description><c>arrival_fact</c> null solo en n = 0 y, con <c>facts</c>, claves de contenido <c>map.*</c> (glosario
    /// §10); <c>world_question</c> = <c>map.world.w{nn}.question</c> de su mundo, una por mundo, respondida por una sola parada
    /// (<c>answers_world_question</c>) posterior y del mismo mundo;</description></item>
    /// <item><description><c>bg</c>: <c>kit</c> = <c>bg_kit_w{nn}</c> de su mundo, uno y solo uno de <c>hero</c> y
    /// <c>code</c>, y <c>lut</c> null, <c>dusk</c> o <c>dawn</c>;</description></item>
    /// <item><description>ningún texto del archivo lleva «belen» (D-013, punto 11).</description></item>
    /// </list>
    /// Cada error es una <see cref="LevelFormatException"/> con la ruta del valor (por ejemplo, <c>stops[3].bg.kit</c>). Que las
    /// claves de texto existan en <c>facts.csv</c> y midan lo que deben no lo comprueba el lector, sino un test contra la tabla de
    /// textos. Los campos opcionales <c>at</c> y <c>leg</c> (desde M2.6) no se leen todavía.
    /// </summary>
    public static class RiverStopCatalogReader
    {
        /// <summary>Tope de paradas del archivo (100 en total; 08 §4.5).</summary>
        public const int MaxStops = 100;

        private const string Belen = "belen";

        private static readonly Regex IdPattern = new Regex("^[a-z0-9_]+$", RegexOptions.CultureInvariant);
        private static readonly Regex Iso2Pattern = new Regex("^[A-Z]{2}$", RegexOptions.CultureInvariant);
        private static readonly Regex FactKeyPattern = new Regex(
            @"^map\.(stop\.[a-z0-9_]+\.dato_[0-9]{2}|country\.[a-z]{2}\.dato_[0-9]{2}|myth\.[a-z0-9_]+|general\.[a-z0-9_]+)$",
            RegexOptions.CultureInvariant);

        private static readonly Regex HeroPattern = new Regex("^bg_hero_[a-z0-9_]+$", RegexOptions.CultureInvariant);
        private static readonly Regex CodePattern = new Regex("^(codigo|kit):[a-z0-9_]+$", RegexOptions.CultureInvariant);
        private static readonly string[] Samples = { "white", "black", "clear", "clear_white", "black_white" };

        /// <summary>Lee y valida el catálogo.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato; el mensaje lleva la ruta.</exception>
        public static RiverStopCatalog Read(string json)
        {
            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("Las paradas del Mapa no son JSON válido: " + e.Message, e);
            }

            try
            {
                return ReadRoot(root);
            }
            catch (FormatException e)
            {
                // Un tipo equivocado que se escapa de las comprobaciones (AsInt de un número no entero, por ejemplo).
                throw new LevelFormatException("Paradas del Mapa: " + e.Message, e);
            }
        }

        private static RiverStopCatalog ReadRoot(JsonValue root)
        {
            if (root.Kind != JsonKind.Object)
            {
                throw Fail("$", "la raíz debe ser un objeto");
            }

            string format = RequireString(root, "format", string.Empty);
            if (!string.Equals(format, RiverStopCatalog.Format, StringComparison.Ordinal))
            {
                throw Fail("format", "'" + format + "' no admitido (este cliente lee " + RiverStopCatalog.Format + ")");
            }

            string source = RequireString(root, "source", string.Empty);
            if (source.Length == 0)
            {
                throw Fail("source", "no puede estar vacío");
            }

            JsonValue? list = root.Get("stops");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw Fail("stops", "debe ser una lista no vacía");
            }

            if (list.Items.Count > MaxStops)
            {
                throw Fail("stops", "tiene " + Num(list.Items.Count) + " paradas y el tope es " + Num(MaxStops));
            }

            var stops = new RiverStop[list.Items.Count];
            var questionAt = new int[WorldTable.WorldCount + 1];
            var answerAt = new int[WorldTable.WorldCount + 1];
            var answerCount = new int[WorldTable.WorldCount + 1];
            for (int w = 0; w <= WorldTable.WorldCount; w++)
            {
                questionAt[w] = -1;
                answerAt[w] = -1;
            }

            for (int i = 0; i < stops.Length; i++)
            {
                RiverStop stop = ReadStop(list.Items[i], i, i == 0 ? null : stops[i - 1]);
                for (int k = 0; k < i; k++)
                {
                    if (string.Equals(stops[k].Id, stop.Id, StringComparison.Ordinal))
                    {
                        throw Fail(Path(i, "id"), "'" + stop.Id + "' repetido (ya es la parada n=" + Num(k) + ")");
                    }
                }

                if (stop.WorldQuestion != null)
                {
                    if (questionAt[stop.World] >= 0)
                    {
                        throw Fail(Path(i, "world_question"), "segunda pregunta del mundo " + Num(stop.World) + " (la primera, en n="
                            + Num(questionAt[stop.World]) + ")");
                    }

                    questionAt[stop.World] = i;
                }

                if (stop.AnswersWorldQuestion)
                {
                    answerCount[stop.World]++;
                    answerAt[stop.World] = i;
                }

                stops[i] = stop;
            }

            // El documento termina en el último nivel de un mundo (1-200: el 200 del mundo 5): con n contiguos y la cadencia de
            // diez niveles, así cada mundo presente está completo, con su gate y, si tiene más de un tramo, su close.
            RiverStop last = stops[stops.Length - 1];
            if (last.LastLevel != WorldTable.LastLevelOf(last.World))
            {
                throw Fail(Path(stops.Length - 1, "levels"), "las paradas terminan en el nivel " + Num(last.LastLevel) + " y el mundo "
                    + Num(last.World) + " acaba en el " + Num(WorldTable.LastLevelOf(last.World))
                    + ": tienen que llegar al final de un mundo (faltan las últimas del mundo, con su close)");
            }

            for (int w = 1; w <= WorldTable.WorldCount; w++)
            {
                if (questionAt[w] < 0)
                {
                    if (answerCount[w] > 0)
                    {
                        throw Fail(Path(answerAt[w], "answers_world_question"), "el mundo " + Num(w) + " no tiene pregunta");
                    }

                    continue;
                }

                if (answerCount[w] != 1)
                {
                    throw Fail(Path(questionAt[w], "world_question"), "la pregunta del mundo " + Num(w) + " la responden "
                        + Num(answerCount[w]) + " paradas y tiene que ser 1");
                }

                if (answerAt[w] <= questionAt[w])
                {
                    throw Fail(Path(answerAt[w], "answers_world_question"), "la pregunta del mundo " + Num(w)
                        + " se responde antes de plantearse (n=" + Num(questionAt[w]) + ")");
                }
            }

            return new RiverStopCatalog(source, stops);
        }

        private static RiverStop ReadStop(JsonValue node, int i, RiverStop? previous)
        {
            string at = "stops[" + Num(i) + "]";
            if (node.Kind != JsonKind.Object)
            {
                throw Fail(at, "cada parada debe ser un objeto");
            }

            int n = RequireInt(node, "n", at);
            if (n != i)
            {
                throw Fail(at + ".n", Num(n) + " no es contiguo (posición " + Num(i)
                    + "; las paradas van de 0 en adelante, sin huecos ni repetidas)");
            }

            JsonValue? levels = node.Get("levels");
            if (levels == null || levels.Kind != JsonKind.Array || levels.Items.Count != 2
                || levels.Items[0].Kind != JsonKind.Number || levels.Items[1].Kind != JsonKind.Number)
            {
                throw Fail(at + ".levels", "debe ser [primero, último]");
            }

            int first = ToInt(levels.Items[0], at + ".levels[0]");
            int lastLevel = ToInt(levels.Items[1], at + ".levels[1]");
            int expectedFirst = (RiverStopCatalog.LevelsPerStop * n) + 1;
            int expectedLast = (RiverStopCatalog.LevelsPerStop * n) + RiverStopCatalog.LevelsPerStop;
            if (first != expectedFirst || lastLevel != expectedLast)
            {
                throw Fail(at + ".levels", "[" + Num(first) + ", " + Num(lastLevel) + "] y la cadencia da [" + Num(expectedFirst) + ", "
                    + Num(expectedLast) + "]");
            }

            if (lastLevel > WorldTable.LastLevel)
            {
                throw Fail(at + ".levels", "pasa del último nivel de la primera vuelta (" + Num(WorldTable.LastLevel) + ")");
            }

            int world = RequireInt(node, "world", at);
            if (world < 1 || world > WorldTable.WorldCount)
            {
                throw Fail(at + ".world", Num(world) + " fuera de 1.." + Num(WorldTable.WorldCount));
            }

            int w0 = WorldTable.WorldOf(first);
            int w1 = WorldTable.WorldOf(lastLevel);
            if (w0 != world || w1 != world)
            {
                throw Fail(at + ".world", Num(world) + " y WorldTable da " + Num(w0) + " (nivel " + Num(first) + ") y " + Num(w1) + " (nivel "
                    + Num(lastLevel) + ")");
            }

            string id = RequireString(node, "id", at);
            if (!IdPattern.IsMatch(id))
            {
                throw Fail(at + ".id", "'" + id + "' no es [a-z0-9_]+");
            }

            StopKind kind = ParseKind(RequireString(node, "kind", at), at + ".kind");
            bool isFirst = first == WorldTable.FirstLevel(world);
            bool isLast = lastLevel == WorldTable.LastLevelOf(world);
            if (isFirst != (kind == StopKind.Gate))
            {
                throw Fail(at + ".kind", isFirst
                    ? "la primera parada del mundo " + Num(world) + " tiene que ser gate"
                    : "gate fuera del primer nivel del mundo " + Num(world) + " (" + Num(WorldTable.FirstLevel(world)) + ")");
            }

            if (!isFirst && isLast != (kind == StopKind.Close))
            {
                throw Fail(at + ".kind", isLast
                    ? "la última parada del mundo " + Num(world) + " tiene que ser close"
                    : "close fuera del último tramo del mundo " + Num(world) + " (nivel " + Num(WorldTable.LastLevelOf(world)) + ")");
            }

            string country = RequireString(node, "country", at);
            if (!Iso2Pattern.IsMatch(country))
            {
                throw Fail(at + ".country", "'" + country + "' no es ISO2 en mayúsculas");
            }

            string? countryName = OptionalString(node, "country_name", at);
            if (countryName != null && !string.Equals(countryName, "map.stop." + id + ".country", StringComparison.Ordinal))
            {
                throw Fail(at + ".country_name", "'" + countryName + "' y la convención es map.stop." + id + ".country");
            }

            int? kmFromSea = OptionalNonNegativeInt(node, "km_from_sea", at);
            int? kmHome = OptionalNonNegativeInt(node, "km_home", at);
            if (kmHome == null)
            {
                throw Fail(at + ".km_home", "debe ser un entero ≥ 0");
            }

            string sample = RequireString(node, "sample", at);
            if (Array.IndexOf(Samples, sample) < 0)
            {
                throw Fail(at + ".sample", "'" + sample + "' desconocido (white, black, clear, clear_white o black_white)");
            }

            string? flag = OptionalString(node, "flag_raise", at);
            string? expectedFlag = previous == null || !string.Equals(previous.Country, country, StringComparison.Ordinal) ? country : null;
            if (!string.Equals(flag, expectedFlag, StringComparison.Ordinal))
            {
                throw Fail(at + ".flag_raise", "'" + (flag ?? "null") + "' y se esperaba '" + (expectedFlag ?? "null")
                    + "' (se iza al empezar y al cambiar de país)");
            }

            string? arrival = OptionalString(node, "arrival_fact", at);
            if (n == 0)
            {
                if (arrival != null)
                {
                    throw Fail(at + ".arrival_fact", "la parada 0 no tiene llegada (tiene que ser null)");
                }
            }
            else if (arrival == null || !FactKeyPattern.IsMatch(arrival))
            {
                throw Fail(at + ".arrival_fact", "'" + (arrival ?? "null") + "' no es una clave de contenido map.* (glosario §10)");
            }

            string[] facts = ReadFacts(node, at);

            string? question = OptionalString(node, "world_question", at);
            if (question != null)
            {
                string expected = "map.world.w" + world.ToString("00", CultureInfo.InvariantCulture) + ".question";
                if (!string.Equals(question, expected, StringComparison.Ordinal))
                {
                    throw Fail(at + ".world_question", "'" + question + "' y se esperaba " + expected);
                }
            }

            JsonValue? answers = node.Get("answers_world_question");
            if (answers == null || answers.Kind != JsonKind.Bool)
            {
                throw Fail(at + ".answers_world_question", "debe ser un booleano");
            }

            RiverStopBackdrop backdrop = ReadBackdrop(node, at, world);
            return new RiverStop(n, world, id, kind, country, countryName, kmFromSea, kmHome.Value, sample, flag, arrival, facts, question,
                answers.AsBool(), backdrop);
        }

        private static string[] ReadFacts(JsonValue node, string at)
        {
            JsonValue? list = node.Get("facts");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail(at + ".facts", "debe ser una lista");
            }

            var facts = new string[list.Items.Count];
            for (int k = 0; k < facts.Length; k++)
            {
                string where = at + ".facts[" + Num(k) + "]";
                JsonValue item = list.Items[k];
                if (item.Kind != JsonKind.String || !FactKeyPattern.IsMatch(item.AsString()))
                {
                    throw Fail(where, "no es una clave de contenido map.* (glosario §10)");
                }

                facts[k] = CheckBelen(item.AsString(), where);
                for (int j = 0; j < k; j++)
                {
                    if (string.Equals(facts[j], facts[k], StringComparison.Ordinal))
                    {
                        throw Fail(where, "'" + facts[k] + "' repetida");
                    }
                }
            }

            return facts;
        }

        private static RiverStopBackdrop ReadBackdrop(JsonValue node, string at, int world)
        {
            string where = at + ".bg";
            JsonValue? bg = node.Get("bg");
            if (bg == null || bg.Kind != JsonKind.Object)
            {
                throw Fail(where, "debe ser un objeto {kit, hero, code, lut}");
            }

            string kit = RequireString(bg, "kit", where);
            string expectedKit = "bg_kit_w" + world.ToString("00", CultureInfo.InvariantCulture);
            if (!string.Equals(kit, expectedKit, StringComparison.Ordinal))
            {
                throw Fail(where + ".kit", "'" + kit + "' y el mundo " + Num(world) + " pide " + expectedKit);
            }

            string? hero = OptionalString(bg, "hero", where);
            string? code = OptionalString(bg, "code", where);
            if ((hero == null) == (code == null))
            {
                throw Fail(where, "lleva hero o code, uno y solo uno");
            }

            if (hero != null && !HeroPattern.IsMatch(hero))
            {
                throw Fail(where + ".hero", "'" + hero + "' no es bg_hero_<sitio>");
            }

            if (code != null && !CodePattern.IsMatch(code))
            {
                throw Fail(where + ".code", "'" + code + "' no es codigo:<pieza> ni kit:<pieza>");
            }

            string? lut = OptionalString(bg, "lut", where);
            if (lut != null && !string.Equals(lut, "dusk", StringComparison.Ordinal) && !string.Equals(lut, "dawn", StringComparison.Ordinal))
            {
                throw Fail(where + ".lut", "'" + lut + "' no es dusk, dawn ni null");
            }

            return new RiverStopBackdrop(kit, hero, code, lut);
        }

        private static StopKind ParseKind(string value, string where)
        {
            switch (value)
            {
                case "gate":
                    return StopKind.Gate;
                case "stop":
                    return StopKind.Stop;
                case "close":
                    return StopKind.Close;
                default:
                    throw Fail(where, "'" + value + "' no es gate, stop ni close");
            }
        }

        private static string RequireString(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.String)
            {
                throw Fail(where, "falta la cadena");
            }

            return CheckBelen(v.AsString(), where);
        }

        // Cadena o null; un campo ausente también es un error (todos los campos de una parada son obligatorios, 08 §4.5).
        private static string? OptionalString(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null)
            {
                throw Fail(where, "falta el campo (cadena o null)");
            }

            if (v.IsNull)
            {
                return null;
            }

            if (v.Kind != JsonKind.String)
            {
                throw Fail(where, "debe ser una cadena o null");
            }

            return CheckBelen(v.AsString(), where);
        }

        private static int RequireInt(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.Number)
            {
                throw Fail(where, "falta el número entero");
            }

            return ToInt(v, where);
        }

        private static int? OptionalNonNegativeInt(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null)
            {
                throw Fail(where, "falta el campo (entero o null)");
            }

            if (v.IsNull)
            {
                return null;
            }

            if (v.Kind != JsonKind.Number)
            {
                throw Fail(where, "debe ser un entero ≥ 0 o null");
            }

            int value = ToInt(v, where);
            if (value < 0)
            {
                throw Fail(where, Num(value) + " es negativo");
            }

            return value;
        }

        private static int ToInt(JsonValue v, string where)
        {
            try
            {
                return v.AsInt();
            }
            catch (FormatException e)
            {
                throw new LevelFormatException(where + ": " + e.Message, e);
            }
        }

        // Ningún id, clave, ranura ni pieza lleva «Belén» (D-013, punto 11): la parada 0 es «belem».
        private static string CheckBelen(string value, string where)
        {
            if (value.IndexOf(Belen, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw Fail(where, "'" + value + "' lleva «belen» (D-013, punto 11)");
            }

            return value;
        }

        private static string Join(string at, string key)
        {
            return at.Length == 0 ? key : at + "." + key;
        }

        private static string Path(int stop, string key)
        {
            return "stops[" + Num(stop) + "]." + key;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static LevelFormatException Fail(string where, string message)
        {
            return new LevelFormatException("Paradas del Mapa, " + where + ": " + message + ".");
        }
    }
}
