#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lee y valida el calendario de Frutas del río <c>af-facts/1</c> (D-017; 08 §4.6; contrato 31) con el lector JSON del
    /// cliente (<see cref="JsonParser"/>):
    /// <list type="bullet">
    /// <item><description><c>format</c> = <see cref="FactCatalog.Format"/>, <c>version</c> ≥ 1 y <c>max_chars_es</c> ≥ 1;</description></item>
    /// <item><description><c>series</c>, una lista no vacía de <c>{subject, kind, from_level, lines, step}</c>: <c>subject</c>
    /// <c>[a-z][a-z0-9_]*</c> y sin repetir (las claves de texto salen de él): el id del tinte (glosario §9) o, en las series de
    /// río [P], el del río (<c>rio_napo</c>); <c>kind</c> = <c>tint</c> o <c>river</c> [P] (las series de río de 261-300, propuesta de
    /// Frutas §5.16; tarea C1a; pendiente de registrar en 08 §4.6, 08 §4.7 y glosario §10-§11); <c>from_level</c> ≥ 1;
    /// <c>lines</c> de 1 a <see cref="FactCatalog.MaxLines"/>; <c>step</c> de 1 a <see cref="WorldTable.LastLevel"/>; en orden de <c>from_level</c>, sin solaparse
    /// (cada bloque <c>[from_level, from_level + lines × step)</c> empieza donde acaba el anterior o después) y dentro de la
    /// primera vuelta (<see cref="WorldTable.LastLevel"/>);</description></item>
    /// <item><description><c>sources</c>, un objeto no vacío <c>{id: {title, url}}</c>: id <c>[A-Z][A-Za-z]*[0-9]+</c>, título no
    /// vacío y url <c>http(s)://</c>.</description></item>
    /// </list>
    /// Los campos que no conoce se ignoran. Cada error es una <see cref="LevelFormatException"/> con la ruta del valor (por
    /// ejemplo, <c>series[3].from_level</c>). Que las claves existan en <c>facts.csv</c>, citen fuentes del catálogo y quepan en
    /// <c>max_chars_es</c>, que no haya huecos y que ninguna serie empiece antes del debut de su tinte no lo comprueba el lector,
    /// sino la validación de la propuesta §6.6 (<c>FactCatalogTests</c>), contra las tablas de textos y el glosario.
    /// </summary>
    public static class FactCatalogReader
    {
        private const string TintKind = "tint";
        private const string RiverKind = "river";

        /// <summary>Lee y valida el calendario.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato; el mensaje lleva la ruta.</exception>
        public static FactCatalog Read(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("El calendario de Frutas del río no es JSON válido: " + e.Message, e);
            }

            try
            {
                return ReadRoot(root);
            }
            catch (FormatException e)
            {
                // Un tipo equivocado que se escapa de las comprobaciones (AsInt de un número no entero, por ejemplo).
                throw new LevelFormatException("Calendario de Frutas del río: " + e.Message, e);
            }
        }

        private static FactCatalog ReadRoot(JsonValue root)
        {
            if (root.Kind != JsonKind.Object)
            {
                throw Fail("$", "la raíz debe ser un objeto");
            }

            string format = RequireString(root, "format", string.Empty);
            if (!string.Equals(format, FactCatalog.Format, StringComparison.Ordinal))
            {
                throw Fail("format", "'" + format + "' no admitido (este cliente lee " + FactCatalog.Format + ")");
            }

            int version = RequireInt(root, "version", string.Empty);
            if (version < 1)
            {
                throw Fail("version", Num(version) + " no es ≥ 1");
            }

            int maxChars = RequireInt(root, "max_chars_es", string.Empty);
            if (maxChars < 1)
            {
                throw Fail("max_chars_es", Num(maxChars) + " no es ≥ 1");
            }

            JsonValue? list = root.Get("series");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw Fail("series", "debe ser una lista no vacía");
            }

            var series = new FactSeries[list.Items.Count];
            for (int i = 0; i < series.Length; i++)
            {
                FactSeries s = ReadSeries(list.Items[i], i);
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(series[j].Subject, s.Subject, StringComparison.Ordinal))
                    {
                        throw Fail(Path(i, "subject"), "'" + s.Subject + "' repetido (ya es la serie " + Num(j) + ")");
                    }
                }

                if (i > 0 && s.FromLevel < series[i - 1].EndLevel)
                {
                    throw Fail(Path(i, "from_level"), Num(s.FromLevel) + " y la serie anterior (" + series[i - 1].Subject + ") ocupa hasta el "
                        + Num(series[i - 1].EndLevel - 1) + ": las series van en orden de nivel y sin solaparse");
                }

                series[i] = s;
            }

            return new FactCatalog(version, maxChars, series, ReadSources(root));
        }

        private static FactSeries ReadSeries(JsonValue node, int i)
        {
            string at = "series[" + Num(i) + "]";
            if (node.Kind != JsonKind.Object)
            {
                throw Fail(at, "cada serie debe ser un objeto");
            }

            string subject = RequireString(node, "subject", at);
            if (!IsSubject(subject))
            {
                throw Fail(at + ".subject", "'" + subject + "' no es [a-z][a-z0-9_]* (el id del tinte, glosario §9, o del río, rio_<río> [P])");
            }

            string kindText = RequireString(node, "kind", at);
            FactSeriesKind kind;
            if (string.Equals(kindText, TintKind, StringComparison.Ordinal))
            {
                kind = FactSeriesKind.Tint;
            }
            else if (string.Equals(kindText, RiverKind, StringComparison.Ordinal))
            {
                kind = FactSeriesKind.River;
            }
            else
            {
                throw Fail(at + ".kind", "'" + kindText + "' no admitido (este cliente lee " + TintKind + " y " + RiverKind + ")");
            }

            int from = RequireInt(node, "from_level", at);
            if (from < 1)
            {
                throw Fail(at + ".from_level", Num(from) + " no es ≥ 1");
            }

            int lines = RequireInt(node, "lines", at);
            if (lines < 1 || lines > FactCatalog.MaxLines)
            {
                throw Fail(at + ".lines", Num(lines) + " fuera de 1.." + Num(FactCatalog.MaxLines));
            }

            int step = RequireInt(node, "step", at);
            if (step < 1 || step > WorldTable.LastLevel)
            {
                // Con un paso mayor que la primera vuelta, el bloque [from_level, from_level + lines × step) acabaría fuera de
                // ella (y, con pasos enormes, from_level + lines × step desbordaría un int).
                throw Fail(at + ".step", Num(step) + " fuera de 1.." + Num(WorldTable.LastLevel));
            }

            long last = from + ((long)(lines - 1) * step);
            if (last > WorldTable.LastLevel)
            {
                throw Fail(at, "su última línea sale en el nivel " + last.ToString(CultureInfo.InvariantCulture)
                    + " y la primera vuelta acaba en el " + Num(WorldTable.LastLevel));
            }

            return new FactSeries(subject, kind, from, lines, step);
        }

        private static List<FactSource> ReadSources(JsonValue root)
        {
            JsonValue? map = root.Get("sources");
            if (map == null || map.Kind != JsonKind.Object || map.Members.Count == 0)
            {
                throw Fail("sources", "debe ser un objeto no vacío {id: {title, url}}");
            }

            var sources = new List<FactSource>(map.Members.Count);
            for (int i = 0; i < map.Members.Count; i++)
            {
                string id = map.Members[i].Key;
                JsonValue node = map.Members[i].Value;
                string at = "sources." + id;
                if (!IsSourceId(id))
                {
                    throw Fail(at, "el id no es [A-Z][A-Za-z]*[0-9]+");
                }

                if (node.Kind != JsonKind.Object)
                {
                    throw Fail(at, "debe ser un objeto {title, url}");
                }

                string title = RequireString(node, "title", at);
                if (title.Trim().Length == 0)
                {
                    throw Fail(at + ".title", "no puede estar vacío");
                }

                string url = RequireString(node, "url", at);
                if (!(url.StartsWith("https://", StringComparison.Ordinal) || url.StartsWith("http://", StringComparison.Ordinal))
                    || url.IndexOf(' ') >= 0)
                {
                    throw Fail(at + ".url", "'" + url + "' no es una dirección http(s) sin espacios");
                }

                sources.Add(new FactSource(id, title, url));
            }

            return sources;
        }

        private static string RequireString(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.String)
            {
                throw Fail(where, "falta la cadena");
            }

            return v.AsString();
        }

        private static int RequireInt(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.Number)
            {
                throw Fail(where, "falta el número entero");
            }

            try
            {
                return v.AsInt();
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("Calendario de Frutas del río, " + where + ": " + e.Message, e);
            }
        }

        // Como RiverStopCatalogReader, carácter a carácter y sin expresiones regulares («$» admite un salto de línea final).

        // [a-z][a-z0-9_]*
        private static bool IsSubject(string value)
        {
            if (value.Length == 0 || value[0] < 'a' || value[0] > 'z')
            {
                return false;
            }

            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        // [A-Z][A-Za-z]*[0-9]+
        private static bool IsSourceId(string value)
        {
            if (value.Length < 2 || value[0] < 'A' || value[0] > 'Z')
            {
                return false;
            }

            int i = 1;
            while (i < value.Length && ((value[i] >= 'A' && value[i] <= 'Z') || (value[i] >= 'a' && value[i] <= 'z')))
            {
                i++;
            }

            if (i == value.Length)
            {
                return false;
            }

            for (; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static string Join(string at, string key)
        {
            return at.Length == 0 ? key : at + "." + key;
        }

        private static string Path(int series, string key)
        {
            return "series[" + Num(series) + "]." + key;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static LevelFormatException Fail(string where, string message)
        {
            return new LevelFormatException("Calendario de Frutas del río, " + where + ": " + message + ".");
        }
    }
}
