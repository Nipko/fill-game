#nullable enable
using System;
using System.Globalization;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lee y valida la tabla de temporada <c>af-seasons/1</c> (<see cref="SeasonTable"/>; tarea SE1, formato [P] de
    /// <c>tools/level-generator/Generation/SeasonTableWriter.cs</c>) con el lector JSON del cliente (<see cref="JsonParser"/>),
    /// con las mismas reglas que el generador:
    /// <list type="bullet">
    /// <item><description><c>format</c> = <see cref="SeasonTable.Format"/> y <c>source</c>, una cadena no vacía;</description></item>
    /// <item><description><c>cycles</c>, una lista no vacía de objetos <c>{"from_level", "season"}</c>, con <c>season</c>
    /// <c>A</c> o <c>B</c>;</description></item>
    /// <item><description>el primer <c>from_level</c> ≡ 1 mod 20 (21, 41…) y los demás seguidos de 20 en 20, sin huecos ni
    /// solapes.</description></item>
    /// </list>
    /// Cada error es una <see cref="LevelFormatException"/> con la ruta del valor (por ejemplo, <c>cycles[3].season</c>).
    /// </summary>
    public static class SeasonTableReader
    {
        /// <summary>Lee y valida la tabla.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato; el mensaje lleva la ruta.</exception>
        public static SeasonTable Read(string json)
        {
            JsonValue root;
            try
            {
                root = JsonParser.Parse(json ?? string.Empty);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("La tabla de temporada no es JSON válido: " + e.Message, e);
            }

            try
            {
                return ReadRoot(root);
            }
            catch (FormatException e)
            {
                // Un tipo equivocado que se escapa de las comprobaciones (AsInt de un número no entero, por ejemplo).
                throw new LevelFormatException("Tabla de temporada: " + e.Message, e);
            }
        }

        private static SeasonTable ReadRoot(JsonValue root)
        {
            if (root.Kind != JsonKind.Object)
            {
                throw Fail("$", "la raíz debe ser un objeto");
            }

            string format = RequireString(root, "format");
            if (!string.Equals(format, SeasonTable.Format, StringComparison.Ordinal))
            {
                throw Fail("format", "'" + format + "' no admitido (este cliente lee " + SeasonTable.Format + ")");
            }

            string source = RequireString(root, "source");
            if (source.Length == 0)
            {
                throw Fail("source", "no puede estar vacío");
            }

            JsonValue? list = root.Get("cycles");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw Fail("cycles", "debe ser una lista no vacía");
            }

            var cycles = new Season[list.Items.Count];
            int first = 0;
            for (int i = 0; i < cycles.Length; i++)
            {
                string at = "cycles[" + Num(i) + "]";
                JsonValue node = list.Items[i];
                if (node.Kind != JsonKind.Object)
                {
                    throw Fail(at, "cada ciclo debe ser un objeto");
                }

                JsonValue? fromNode = node.Get("from_level");
                if (fromNode == null || fromNode.Kind != JsonKind.Number)
                {
                    throw Fail(at + ".from_level", "falta el número entero");
                }

                int from = fromNode.AsInt();
                if (i == 0)
                {
                    if (from < 1 || (from - 1) % SeasonTable.CycleLength != 0)
                    {
                        throw Fail(at + ".from_level", Num(from) + " no es el primer nivel de un ciclo de 20 (1, 21, 41…)");
                    }

                    first = from;
                }
                else
                {
                    int expected = first + (i * SeasonTable.CycleLength);
                    if (from != expected)
                    {
                        throw Fail(at + ".from_level", Num(from) + " no sigue al ciclo anterior (esperado: " + Num(expected) + ")");
                    }
                }

                JsonValue? seasonNode = node.Get("season");
                string season = seasonNode != null && seasonNode.Kind == JsonKind.String ? seasonNode.AsString() : string.Empty;
                if (string.Equals(season, "A", StringComparison.Ordinal))
                {
                    cycles[i] = Season.HighWater;
                }
                else if (string.Equals(season, "B", StringComparison.Ordinal))
                {
                    cycles[i] = Season.LowWater;
                }
                else
                {
                    throw Fail(at + ".season", "debe ser \"A\" (aguas altas) o \"B\" (aguas bajas)");
                }
            }

            return new SeasonTable(source, first, cycles);
        }

        private static string RequireString(JsonValue node, string key)
        {
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.String)
            {
                throw Fail(key, "falta la cadena");
            }

            return v.AsString();
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static LevelFormatException Fail(string where, string message)
        {
            return new LevelFormatException("Tabla de temporada, " + where + ": " + message + ".");
        }
    }
}
