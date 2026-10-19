#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lee, valida y decodifica una sola vez la geometría del Mapa del río <c>af-river-map/1</c> (08 §2.2 y §4.5; contrato 29;
    /// esquema <c>schemas/af-river-map-1.schema.json</c>; codificación de P-M1, D-023) con el lector JSON del cliente
    /// (<see cref="JsonParser"/>), sin motor:
    /// <list type="bullet">
    /// <item><description><c>format</c> = <see cref="RiverMap.Format"/>; <c>frame</c> con <c>rotation</c> = <c>cw90</c>,
    /// <c>lat</c> y <c>lon</c> como [mínimo, máximo] crecientes y <c>lat0</c> en (−90, 90);</description></item>
    /// <item><description>líneas con id <c>[a-z0-9_]+</c> único, <c>kind</c>, <c>class</c> 1-3, <c>src</c> conocido y
    /// <c>pts</c> en base64 (RFC 4648) de pares uint16 little-endian (el primero absoluto y los demás deltas módulo 65.536),
    /// con al menos un punto;</description></item>
    /// <item><description>tramos en [0, 1]: <c>water</c> seguidos de 0 a 1 (o vacío, [NV]), <c>splits</c> con s0 &lt; s1, en
    /// orden y sin solaparse (solo en <c>main</c>), <c>mouth.s</c>, <c>home.s</c> y <c>passing[].s</c>, y
    /// <c>fill_on_pass</c> en (0, 1];</description></item>
    /// <item><description><c>mouth.on</c>, <c>home.on</c>, <c>passing[].line</c> y las líneas de <c>insets</c>, líneas del Mapa
    /// (la boca, de otra línea); <c>main</c> sin <c>mouth</c> ni <c>fill_on_pass</c> y las demás con los dos;</description></item>
    /// <item><description><c>land</c>: al menos 3 vértices, <c>tris</c> de tres en tres dentro de <c>verts</c> y zonas que
    /// cubren cada triángulo una sola vez;</description></item>
    /// <item><description>rótulos ISO2, miradores con <c>reveal_on</c> 1-1.000, claves de dato <c>map.*</c> (glosario §10), ids
    /// de puntos de paso y miradores únicos, y ningún id ni clave con «belen» (D-013, punto 11; los datos base64 no son
    /// nombres y no se miran).</description></item>
    /// </list>
    /// Cada error es una <see cref="LevelFormatException"/> con la ruta del valor (por ejemplo, <c>lines[3].mouth.on</c>). Lo que
    /// solo puede comprobar la herramienta (puntos de control, bocas sobre su río, polígono simple) es de
    /// <c>tools/river-map/geometry.py --check</c>. Al decodificar, cada punto (X, Y) pasa a unidades del marco con W = 1
    /// (X / 65535, Y / 65535 · aspecto) y se acumula la longitud de cada línea en doble precisión, como la herramienta, para que
    /// la s del cliente sea la del archivo. <see cref="ReadRoutes"/> lee los campos <c>at</c> y <c>leg</c> de
    /// <c>af-river-stops/1</c> contra este Mapa. Las asignaciones son solo de la carga (una vez al abrir el Mapa).
    /// </summary>
    public static class RiverMapReader
    {
        /// <summary>Valor máximo de una coordenada uint16 (X = round(65535 x)).</summary>
        public const int Quantum = 65535;

        /// <summary>Último nivel que puede revelar un mirador.</summary>
        public const int MaxRevealLevel = 1000;

        /// <summary>
        /// Distancia, en unidades del marco (≈ 0,03 dp en la Cuenca), por debajo de la cual dos puntos de una ruta son el mismo
        /// punto del agua aunque el archivo no declare la unión (el arranque del ramal del Pará sobre el cauce) [P].
        /// </summary>
        public const float JoinTolerance = 1e-4f;

        private const string Belen = "belen";
        private const string StopFactPrefix = "map.stop.";
        private const string CountryFactPrefix = "map.country.";
        private const string MythFactPrefix = "map.myth.";
        private const string GeneralFactPrefix = "map.general.";
        private const string DatoSuffix = ".dato_";

        private static readonly string[] Sources = { "rio_amazonas", "ne10m", "hydrorivers" };

        // ============================================================================================ geometría

        /// <summary>Lee, valida y decodifica la geometría.</summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o no cumple el formato; el mensaje lleva la ruta.</exception>
        public static RiverMap Read(string json)
        {
            JsonValue root = Parse(json, "El Mapa del río");
            try
            {
                return ReadRoot(root);
            }
            catch (FormatException e)
            {
                // Un tipo equivocado que se escapa de las comprobaciones (AsInt de un número no entero, por ejemplo).
                throw new LevelFormatException("Mapa del río: " + e.Message, e);
            }
        }

        private static RiverMap ReadRoot(JsonValue root)
        {
            if (root.Kind != JsonKind.Object)
            {
                throw Fail("$", "la raíz debe ser un objeto");
            }

            string format = RequireString(root, "format", string.Empty);
            if (!string.Equals(format, RiverMap.Format, StringComparison.Ordinal))
            {
                throw Fail("format", "'" + format + "' no admitido (este cliente lee " + RiverMap.Format + ")");
            }

            RiverMapFrame frame = ReadFrame(root);
            double aspect = frame.Aspect; // en doble hasta guardar xy y la longitud, como la herramienta

            JsonValue? list = root.Get("lines");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw Fail("lines", "debe ser una lista no vacía");
            }

            // Primera pasada: ids, para resolver las bocas que apuntan a líneas posteriores.
            int count = list.Items.Count;
            var ids = new string[count];
            for (int i = 0; i < count; i++)
            {
                JsonValue node = list.Items[i];
                string at = "lines[" + Num(i) + "]";
                if (node.Kind != JsonKind.Object)
                {
                    throw Fail(at, "cada línea debe ser un objeto");
                }

                string id = RequireSlug(node, "id", at);
                for (int k = 0; k < i; k++)
                {
                    if (string.Equals(ids[k], id, StringComparison.Ordinal))
                    {
                        throw Fail(at + ".id", "'" + id + "' repetido (ya es lines[" + Num(k) + "])");
                    }
                }

                ids[i] = id;
            }

            var lines = new RiverLine[count];
            for (int i = 0; i < count; i++)
            {
                lines[i] = ReadLine(list.Items[i], i, ids, aspect);
            }

            RiverLand land = ReadLand(root, aspect);
            RiverInset[] insets = ReadInsets(root, ids);
            RiverMapPlace home = ReadPlace(root, "home", string.Empty, ids, "on");
            RiverMapLabel[] labels = ReadLabels(root, aspect);
            RiverViewpoint[] viewpoints = ReadViewpoints(root, aspect);
            RiverPassingPoint[] passing = ReadPassing(root, ids, viewpoints);
            return new RiverMap(frame, lines, land, insets, home, labels, viewpoints, passing);
        }

        private static RiverMapFrame ReadFrame(JsonValue root)
        {
            JsonValue? frame = root.Get("frame");
            if (frame == null || frame.Kind != JsonKind.Object)
            {
                throw Fail("frame", "debe ser un objeto {rotation, lat, lon, lat0}");
            }

            string rotation = RequireString(frame, "rotation", "frame");
            if (!string.Equals(rotation, "cw90", StringComparison.Ordinal))
            {
                throw Fail("frame.rotation", "'" + rotation + "' no admitido (cw90: 90° en el sentido de las agujas del reloj, 06 §4.5)");
            }

            ReadRange(frame, "lat", -90.0, 90.0, out double latMin, out double latMax);
            ReadRange(frame, "lon", -180.0, 180.0, out double lonMin, out double lonMax);
            double lat0 = RequireNumber(frame, "lat0", "frame");
            if (!(lat0 > -90.0 && lat0 < 90.0))
            {
                throw Fail("frame.lat0", Dbl(lat0) + " fuera de (−90, 90)");
            }

            return new RiverMapFrame(latMin, latMax, lonMin, lonMax, lat0);
        }

        private static void ReadRange(JsonValue frame, string key, double min, double max, out double a, out double b)
        {
            string where = "frame." + key;
            JsonValue? range = frame.Get(key);
            if (range == null || range.Kind != JsonKind.Array || range.Items.Count != 2
                || range.Items[0].Kind != JsonKind.Number || range.Items[1].Kind != JsonKind.Number)
            {
                throw Fail(where, "debe ser [mínimo, máximo]");
            }

            a = range.Items[0].AsDouble();
            b = range.Items[1].AsDouble();
            if (!(a >= min && b <= max && a < b))
            {
                throw Fail(where, "[" + Dbl(a) + ", " + Dbl(b) + "] no es un intervalo creciente dentro de [" + Dbl(min) + ", " + Dbl(max) + "]");
            }
        }

        private static RiverLine ReadLine(JsonValue node, int index, string[] ids, double aspect)
        {
            string at = "lines[" + Num(index) + "]";
            string id = ids[index];
            RiverLineKind kind = ParseKind(RequireString(node, "kind", at), at + ".kind");

            int lineClass = RequireInt(node, "class", at);
            if (lineClass < 1 || lineClass > 3)
            {
                throw Fail(at + ".class", Num(lineClass) + " fuera de 1..3");
            }

            string source = RequireString(node, "src", at);
            if (Array.IndexOf(Sources, source) < 0)
            {
                throw Fail(at + ".src", "'" + source + "' desconocido (rio_amazonas, ne10m o hydrorivers)");
            }

            ushort[] raw = DecodePoints(RequireBase64(node, "pts", at), at + ".pts");
            int points = raw.Length / 2;
            var xy = new float[raw.Length];
            var cumulative = new float[points];
            double total = 0.0;
            double px = 0.0;
            double py = 0.0;
            for (int i = 0; i < points; i++)
            {
                // Las unidades de la herramienta: (X / 65535, Y / 65535 · aspecto), con la longitud acumulada en doble precisión.
                double x = raw[2 * i] / (double)Quantum;
                double y = raw[(2 * i) + 1] / (double)Quantum * aspect;
                if (i > 0)
                {
                    double dx = x - px;
                    double dy = y - py;
                    total += Math.Sqrt((dx * dx) + (dy * dy));
                }

                xy[2 * i] = (float)x;
                xy[(2 * i) + 1] = (float)y;
                cumulative[i] = (float)total;
                px = x;
                py = y;
            }

            RiverWaterSpan[] water = ReadWater(node, at);
            RiverSplit[] splits = ReadSplits(node, at, kind);

            RiverMapPlace? mouth = null;
            float fill = 0f;
            if (kind == RiverLineKind.Main)
            {
                if (node.Has("mouth"))
                {
                    throw Fail(at + ".mouth", "una línea main no lleva boca");
                }

                if (node.Has("fill_on_pass"))
                {
                    throw Fail(at + ".fill_on_pass", "una línea main no lleva fill_on_pass");
                }
            }
            else
            {
                RiverMapPlace place = ReadPlace(node, "mouth", at, ids, "on");
                if (place.Line == index)
                {
                    throw Fail(at + ".mouth.on", "'" + id + "' no puede desembocar en sí misma");
                }

                mouth = place;
                if (!node.Has("fill_on_pass"))
                {
                    throw Fail(at + ".fill_on_pass", "falta (fracción en (0, 1] que se llena al pasar la boca)");
                }

                double f = RequireNumber(node, "fill_on_pass", at);
                if (!(f > 0.0 && f <= 1.0))
                {
                    throw Fail(at + ".fill_on_pass", Dbl(f) + " fuera de (0, 1]");
                }

                fill = (float)f;
            }

            return new RiverLine(index, id, kind, lineClass, source, xy, cumulative, water, splits, mouth, fill);
        }

        private static RiverWaterSpan[] ReadWater(JsonValue node, string at)
        {
            string where = at + ".water";
            JsonValue? list = node.Get("water");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail(where, "debe ser una lista (vacía si el agua está sin comprobar)");
            }

            var spans = new RiverWaterSpan[list.Items.Count];
            double previous = 0.0;
            for (int k = 0; k < spans.Length; k++)
            {
                string w = where + "[" + Num(k) + "]";
                JsonValue span = list.Items[k];
                if (span.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {s0, s1, type}");
                }

                double s0 = RequireUnit(span, "s0", w);
                double s1 = RequireUnit(span, "s1", w);
                if (s0 != previous)
                {
                    throw Fail(w + ".s0", Dbl(s0) + " y el tramo anterior acaba en " + Dbl(previous)
                        + " (los tramos van seguidos de 0 a 1)");
                }

                if (!(s0 < s1))
                {
                    throw Fail(w + ".s1", Dbl(s1) + " no es mayor que s0 = " + Dbl(s0));
                }

                RiverWater type = ParseWater(RequireString(span, "type", w), w + ".type", true);
                spans[k] = new RiverWaterSpan((float)s0, (float)s1, type);
                previous = s1;
            }

            if (spans.Length > 0 && previous != 1.0)
            {
                throw Fail(where + "[" + Num(spans.Length - 1) + "].s1", Dbl(previous) + " y el último tramo acaba en 1");
            }

            return spans;
        }

        private static RiverSplit[] ReadSplits(JsonValue node, string at, RiverLineKind kind)
        {
            string where = at + ".splits";
            JsonValue? list = node.Get("splits");
            if (list == null)
            {
                return Array.Empty<RiverSplit>();
            }

            if (kind != RiverLineKind.Main)
            {
                throw Fail(where, "solo las líneas main llevan dos aguas");
            }

            if (list.Kind != JsonKind.Array)
            {
                throw Fail(where, "debe ser una lista");
            }

            var splits = new RiverSplit[list.Items.Count];
            double previous = 0.0;
            for (int k = 0; k < splits.Length; k++)
            {
                string w = where + "[" + Num(k) + "]";
                JsonValue split = list.Items[k];
                if (split.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {id, s0, s1, left, right}");
                }

                string id = RequireSlug(split, "id", w);
                double s0 = RequireUnit(split, "s0", w);
                double s1 = RequireUnit(split, "s1", w);
                if (!(s0 < s1))
                {
                    throw Fail(w, "s0 = " + Dbl(s0) + " no es menor que s1 = " + Dbl(s1));
                }

                if (s0 < previous)
                {
                    throw Fail(w + ".s0", Dbl(s0) + " antes del final de la franja anterior (" + Dbl(previous) + "): en orden y sin solaparse");
                }

                RiverWater left = ParseWater(RequireString(split, "left", w), w + ".left", false);
                RiverWater right = ParseWater(RequireString(split, "right", w), w + ".right", false);
                splits[k] = new RiverSplit(id, (float)s0, (float)s1, left, right);
                previous = s1;
            }

            return splits;
        }

        private static RiverLand ReadLand(JsonValue root, double aspect)
        {
            JsonValue? land = root.Get("land");
            if (land == null || land.Kind != JsonKind.Object)
            {
                throw Fail("land", "debe ser un objeto {verts, tris, zones}");
            }

            ushort[] raw = DecodePoints(RequireBase64(land, "verts", "land"), "land.verts");
            int vertices = raw.Length / 2;
            if (vertices < 3)
            {
                throw Fail("land.verts", "tiene " + Num(vertices) + " vértices y un polígono necesita al menos 3");
            }

            var xy = new float[raw.Length];
            for (int i = 0; i < raw.Length; i += 2)
            {
                xy[i] = (float)(raw[i] / (double)Quantum);
                xy[i + 1] = (float)(raw[i + 1] / (double)Quantum * aspect);
            }

            ushort[] rawTris = DecodeUInt16(RequireBase64(land, "tris", "land"), "land.tris");
            if (rawTris.Length % 3 != 0)
            {
                throw Fail("land.tris", Num(rawTris.Length) + " índices no son triángulos de tres en tres");
            }

            var tris = new int[rawTris.Length];
            for (int i = 0; i < rawTris.Length; i++)
            {
                if (rawTris[i] >= vertices)
                {
                    throw Fail("land.tris", "el índice " + Num(i) + " (" + Num(rawTris[i]) + ") cae fuera de verts (" + Num(vertices)
                        + " vértices)");
                }

                tris[i] = rawTris[i];
            }

            int triangles = tris.Length / 3;
            JsonValue? list = land.Get("zones");
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw Fail("land.zones", "debe ser una lista no vacía");
            }

            var zones = new RiverLandZone[list.Items.Count];
            var covered = new bool[triangles];
            int total = 0;
            for (int k = 0; k < zones.Length; k++)
            {
                string w = "land.zones[" + Num(k) + "]";
                JsonValue zone = list.Items[k];
                if (zone.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {id, tile, first, count}");
                }

                string id = RequireSlug(zone, "id", w);
                string tile = RequireSlug(zone, "tile", w);
                int first = RequireInt(zone, "first", w);
                int n = RequireInt(zone, "count", w);
                if (first < 0 || n < 0 || first + n > triangles)
                {
                    throw Fail(w, "[" + Num(first) + ", " + Num(first + n) + ") cae fuera de los " + Num(triangles) + " triángulos");
                }

                for (int t = first; t < first + n; t++)
                {
                    if (covered[t])
                    {
                        throw Fail(w, "el triángulo " + Num(t) + " ya es de otra zona");
                    }

                    covered[t] = true;
                }

                total += n;
                zones[k] = new RiverLandZone(id, tile, first, n);
            }

            if (total != triangles)
            {
                throw Fail("land.zones", "cubren " + Num(total) + " de los " + Num(triangles) + " triángulos");
            }

            return new RiverLand(xy, tris, zones);
        }

        private static RiverInset[] ReadInsets(JsonValue root, string[] ids)
        {
            JsonValue? list = root.Get("insets");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail("insets", "debe ser una lista (vacía en la 1.0)");
            }

            var insets = new RiverInset[list.Items.Count];
            for (int k = 0; k < insets.Length; k++)
            {
                string w = "insets[" + Num(k) + "]";
                JsonValue inset = list.Items[k];
                if (inset.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {id, rect, lines, link}");
                }

                string id = RequireSlug(inset, "id", w);
                JsonValue? rect = inset.Get("rect");
                if (rect == null || rect.Kind != JsonKind.Array || rect.Items.Count != 4)
                {
                    throw Fail(w + ".rect", "debe ser [x0, y0, x1, y1]");
                }

                var r = new float[4];
                for (int c = 0; c < 4; c++)
                {
                    JsonValue v = rect.Items[c];
                    double d = v.Kind == JsonKind.Number ? v.AsDouble() : double.NaN;
                    if (!(d >= 0.0 && d <= 1.0))
                    {
                        throw Fail(w + ".rect[" + Num(c) + "]", "debe ser una fracción en [0, 1]");
                    }

                    r[c] = (float)d;
                }

                if (!(r[0] < r[2] && r[1] < r[3]))
                {
                    throw Fail(w + ".rect", "no es un rectángulo con x0 < x1 e y0 < y1");
                }

                JsonValue? names = inset.Get("lines");
                if (names == null || names.Kind != JsonKind.Array || names.Items.Count == 0)
                {
                    throw Fail(w + ".lines", "debe ser una lista no vacía de ids de línea");
                }

                var lineIndexes = new int[names.Items.Count];
                for (int j = 0; j < lineIndexes.Length; j++)
                {
                    string wj = w + ".lines[" + Num(j) + "]";
                    JsonValue name = names.Items[j];
                    if (name.Kind != JsonKind.String)
                    {
                        throw Fail(wj, "debe ser un id de línea");
                    }

                    lineIndexes[j] = RequireLineId(CheckBelen(name.AsString(), wj), ids, wj);
                }

                int link = RequireLineId(RequireSlug(inset, "link", w), ids, w + ".link");
                insets[k] = new RiverInset(id, r[0], r[1], r[2], r[3], lineIndexes, link);
            }

            return insets;
        }

        private static RiverMapLabel[] ReadLabels(JsonValue root, double aspect)
        {
            JsonValue? list = root.Get("labels");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail("labels", "debe ser una lista");
            }

            var labels = new RiverMapLabel[list.Items.Count];
            for (int k = 0; k < labels.Length; k++)
            {
                string w = "labels[" + Num(k) + "]";
                JsonValue label = list.Items[k];
                if (label.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {country, xy}");
                }

                string country = RequireString(label, "country", w);
                if (!IsIso2(country))
                {
                    throw Fail(w + ".country", "'" + country + "' no es ISO2 en mayúsculas");
                }

                ReadXy(label, w, aspect, out float x, out float y);
                labels[k] = new RiverMapLabel(country, x, y);
            }

            return labels;
        }

        private static RiverViewpoint[] ReadViewpoints(JsonValue root, double aspect)
        {
            JsonValue? list = root.Get("viewpoints");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail("viewpoints", "debe ser una lista");
            }

            var viewpoints = new RiverViewpoint[list.Items.Count];
            for (int k = 0; k < viewpoints.Length; k++)
            {
                string w = "viewpoints[" + Num(k) + "]";
                JsonValue v = list.Items[k];
                if (v.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {id, xy, reveal_on, fact}");
                }

                string id = RequireSlug(v, "id", w);
                for (int j = 0; j < k; j++)
                {
                    if (string.Equals(viewpoints[j].Id, id, StringComparison.Ordinal))
                    {
                        throw Fail(w + ".id", "'" + id + "' repetido");
                    }
                }

                ReadXy(v, w, aspect, out float x, out float y);
                int reveal = RequireInt(v, "reveal_on", w);
                if (reveal < 1 || reveal > MaxRevealLevel)
                {
                    throw Fail(w + ".reveal_on", Num(reveal) + " fuera de 1.." + Num(MaxRevealLevel));
                }

                viewpoints[k] = new RiverViewpoint(id, x, y, reveal, OptionalFact(v, w));
            }

            return viewpoints;
        }

        private static RiverPassingPoint[] ReadPassing(JsonValue root, string[] ids, RiverViewpoint[] viewpoints)
        {
            JsonValue? list = root.Get("passing");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail("passing", "debe ser una lista");
            }

            var passing = new RiverPassingPoint[list.Items.Count];
            for (int k = 0; k < passing.Length; k++)
            {
                string w = "passing[" + Num(k) + "]";
                JsonValue p = list.Items[k];
                if (p.Kind != JsonKind.Object)
                {
                    throw Fail(w, "debe ser un objeto {id, line, s, fact}");
                }

                string id = RequireSlug(p, "id", w);
                for (int j = 0; j < k; j++)
                {
                    if (string.Equals(passing[j].Id, id, StringComparison.Ordinal))
                    {
                        throw Fail(w + ".id", "'" + id + "' repetido");
                    }
                }

                for (int j = 0; j < viewpoints.Length; j++)
                {
                    if (string.Equals(viewpoints[j].Id, id, StringComparison.Ordinal))
                    {
                        throw Fail(w + ".id", "'" + id + "' es también el id de un mirador (comparten map.stop.<id>.name)");
                    }
                }

                int line = RequireLineId(RequireSlug(p, "line", w), ids, w + ".line");
                double s = RequireUnit(p, "s", w);
                passing[k] = new RiverPassingPoint(id, new RiverMapPlace(line, (float)s), OptionalFact(p, w));
            }

            return passing;
        }

        // ============================================================================================ rutas de las paradas

        /// <summary>
        /// Lee las rutas de las paradas (los campos <c>at</c> y <c>leg</c> de <c>af-river-stops/1</c>; 08 §4.5) contra el Mapa
        /// <paramref name="map"/>: líneas del Mapa, s en [0, 1], cada ruta continua desde la parada anterior (su primer tramo
        /// empieza en el <c>at</c> de la anterior, cada tramo sale de donde acaba el anterior y el último acaba en el suyo; en
        /// n = 0, sobre la propia parada), contando como el mismo punto la boca de un afluente y su s sobre el anfitrión, como
        /// <c>same_place</c> de la herramienta; <c>via</c> con puntos de paso del Mapa y <c>mouths</c> con líneas que no son
        /// <c>main</c>. El resto del archivo lo valida
        /// <see cref="RiverStopCatalogReader"/>; que los tramos no crucen tierra, <c>tools/river-map/geometry.py --check</c>.
        /// </summary>
        /// <exception cref="LevelFormatException">El JSON no es válido o las rutas no casan con el Mapa; el mensaje lleva la ruta.</exception>
        public static RiverRoutes ReadRoutes(string stopsJson, RiverMap map)
        {
            JsonValue root = Parse(stopsJson, "Las paradas del Mapa");
            try
            {
                return ReadRoutesRoot(root, map);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("Rutas del Mapa: " + e.Message, e);
            }
        }

        private static RiverRoutes ReadRoutesRoot(JsonValue root, RiverMap map)
        {
            JsonValue? list = root.Kind == JsonKind.Object ? root.Get("stops") : null;
            if (list == null || list.Kind != JsonKind.Array || list.Items.Count == 0)
            {
                throw FailRoute("stops", "debe ser una lista no vacía");
            }

            var routes = new RiverStopRoute[list.Items.Count];
            for (int i = 0; i < routes.Length; i++)
            {
                string at = "stops[" + Num(i) + "]";
                JsonValue node = list.Items[i];
                if (node.Kind != JsonKind.Object)
                {
                    throw FailRoute(at, "cada parada debe ser un objeto");
                }

                JsonValue? nValue = node.Get("n");
                if (nValue == null || nValue.Kind != JsonKind.Number || nValue.AsInt() != i)
                {
                    throw FailRoute(at + ".n", "las paradas van de 0 en adelante, sin huecos");
                }

                JsonValue? idValue = node.Get("id");
                if (idValue == null || idValue.Kind != JsonKind.String)
                {
                    throw FailRoute(at + ".id", "falta la cadena");
                }

                RiverMapPlace place = RoutePlace(node, "at", "line", "s", at, map);

                JsonValue? leg = node.Get("leg");
                if (leg == null || leg.Kind != JsonKind.Object)
                {
                    throw FailRoute(at + ".leg", "debe ser un objeto {path, via, mouths}");
                }

                JsonValue? pathList = leg.Get("path");
                if (pathList == null || pathList.Kind != JsonKind.Array || pathList.Items.Count == 0)
                {
                    throw FailRoute(at + ".leg.path", "debe ser una lista no vacía de tramos");
                }

                var path = new RiverPathPiece[pathList.Items.Count];
                for (int k = 0; k < path.Length; k++)
                {
                    string w = at + ".leg.path[" + Num(k) + "]";
                    JsonValue piece = pathList.Items[k];
                    if (piece.Kind != JsonKind.Object)
                    {
                        throw FailRoute(w, "debe ser un objeto {line, s0, s1}");
                    }

                    RiverMapPlace start = RoutePlace(piece, null, "line", "s0", w, map);
                    float s1 = (float)RouteUnit(piece, "s1", w);
                    path[k] = new RiverPathPiece(start.Line, start.S, s1);
                }

                // Continuidad: el tramo empieza donde está la parada anterior (en n = 0, en la propia parada), cada tramo sale de
                // donde acaba el anterior y el último acaba en la parada (el mismo punto del agua: SamePlace).
                RiverMapPlace from = i == 0 ? place : routes[i - 1].At;
                if (!SamePlace(map, path[0].Line, path[0].S0, from.Line, from.S))
                {
                    throw FailRoute(at + ".leg.path[0]", "empieza en " + map.Lines[path[0].Line].Id + " s=" + Flt(path[0].S0)
                        + " y la canoa sale de " + map.Lines[from.Line].Id + " s=" + Flt(from.S));
                }

                for (int k = 1; k < path.Length; k++)
                {
                    if (!SamePlace(map, path[k].Line, path[k].S0, path[k - 1].Line, path[k - 1].S1))
                    {
                        throw FailRoute(at + ".leg.path[" + Num(k) + "]", "empieza en " + map.Lines[path[k].Line].Id + " s="
                            + Flt(path[k].S0) + " y el tramo anterior acaba en " + map.Lines[path[k - 1].Line].Id + " s="
                            + Flt(path[k - 1].S1) + " (dos tramos solo se unen en el mismo punto o en una confluencia)");
                    }
                }

                RiverPathPiece last = path[path.Length - 1];
                if (!SamePlace(map, last.Line, last.S1, place.Line, place.S))
                {
                    throw FailRoute(at + ".leg.path[" + Num(path.Length - 1) + "]", "acaba en " + map.Lines[last.Line].Id + " s="
                        + Flt(last.S1) + " y la parada está en " + map.Lines[place.Line].Id + " s=" + Flt(place.S));
                }

                string[] via = ReadVia(leg, at, map);
                int[] mouths = ReadMouths(leg, at, map);
                routes[i] = new RiverStopRoute(i, idValue.AsString(), place, path, via, mouths);
            }

            return new RiverRoutes(routes, SourceGoal(map, routes));
        }

        /// <summary>
        /// La meta tras la última parada (tarea FX13; decisión del dueño del 2026-10-19, en D-013: «que el último tramo (691-700)
        /// apunte a la naciente (el Mismi) como meta, sin parada nueva»): si la última parada está en el cauce
        /// (<see cref="RiverMap.CauceLineId"/>, <c>main</c>, que va de la naciente del Mismi, s = 0, al Atlántico), una ruta de ella
        /// a s = 0 por el cauce, río arriba, con los puntos de paso y las bocas que haya por el camino (ninguno con las paradas
        /// 0-69). Null si la última parada no está en el cauce o ya está en la naciente. La misma condición, sin el Mapa, es
        /// <see cref="RiverStopCatalog.HeadsToSource"/>. Asigna solo aquí, al cargar.
        /// </summary>
        public static RiverStopRoute? SourceGoal(RiverMap map, RiverStopRoute[] routes)
        {
            if (map == null || routes == null)
            {
                throw new ArgumentNullException(map == null ? nameof(map) : nameof(routes));
            }

            int cauce = map.LineIndexOf(RiverMap.CauceLineId);
            if (routes.Length == 0 || cauce < 0 || map.Lines[cauce].Kind != RiverLineKind.Main)
            {
                return null;
            }

            RiverMapPlace from = routes[routes.Length - 1].At;
            if (from.Line != cauce || from.S <= 0f)
            {
                return null;
            }

            // Río arriba, de la última parada hacia la naciente: s decreciente (inserción estable, sin depender del orden de Sort).
            var via = new List<RiverPassingPoint>();
            for (int i = 0; i < map.Passing.Count; i++)
            {
                RiverPassingPoint p = map.Passing[i];
                if (p.At.Line == cauce && p.At.S > 0f && p.At.S < from.S)
                {
                    int k = via.Count;
                    while (k > 0 && via[k - 1].At.S < p.At.S)
                    {
                        k--;
                    }

                    via.Insert(k, p);
                }
            }

            var viaIds = new string[via.Count];
            for (int i = 0; i < viaIds.Length; i++)
            {
                viaIds[i] = via[i].Id;
            }

            var mouths = new List<int>();
            var mouthS = new List<float>();
            for (int i = 0; i < map.Lines.Count; i++)
            {
                RiverMapPlace? mouth = map.Lines[i].Mouth;
                if (map.Lines[i].Kind == RiverLineKind.Main || mouth == null || mouth.Value.Line != cauce || mouth.Value.S >= from.S)
                {
                    continue;
                }

                int k = mouths.Count;
                while (k > 0 && mouthS[k - 1] < mouth.Value.S)
                {
                    k--;
                }

                mouths.Insert(k, i);
                mouthS.Insert(k, mouth.Value.S);
            }

            var path = new[] { new RiverPathPiece(cauce, from.S, 0f) };
            return new RiverStopRoute(routes.Length, RiverRoutes.SourceGoalId, new RiverMapPlace(cauce, 0f), path, viaIds, mouths.ToArray());
        }

        // Dos posiciones son el mismo punto del agua (como same_place de geometry.py): iguales; la boca de una línea (su s = 1) y
        // su s sobre el anfitrión (mouth); o, para el arranque del ramal del Pará, que el archivo no declara, dos puntos a menos
        // de JoinTolerance.
        private static bool SamePlace(RiverMap map, int lineA, float sA, int lineB, float sB)
        {
            if (lineA == lineB && sA == sB)
            {
                return true;
            }

            if (IsMouth(map, lineA, sA, lineB, sB) || IsMouth(map, lineB, sB, lineA, sA))
            {
                return true;
            }

            map.Lines[lineA].PointAt(sA, out float ax, out float ay);
            map.Lines[lineB].PointAt(sB, out float bx, out float by);
            float dx = ax - bx;
            float dy = ay - by;
            return (dx * dx) + (dy * dy) <= JoinTolerance * JoinTolerance;
        }

        private static bool IsMouth(RiverMap map, int line, float s, int host, float hostS)
        {
            RiverMapPlace? mouth = map.Lines[line].Mouth;
            return s == 1f && mouth.HasValue && mouth.Value.Line == host && mouth.Value.S == hostS;
        }

        private static RiverMapPlace RoutePlace(JsonValue node, string? key, string lineKey, string sKey, string at, RiverMap map)
        {
            string where = key == null ? at : at + "." + key;
            JsonValue? obj = key == null ? node : node.Get(key);
            if (obj == null || obj.Kind != JsonKind.Object)
            {
                throw FailRoute(where, "debe ser un objeto {" + lineKey + ", " + sKey + "}");
            }

            JsonValue? line = obj.Get(lineKey);
            int index = line != null && line.Kind == JsonKind.String ? map.LineIndexOf(line.AsString()) : -1;
            if (index < 0)
            {
                throw FailRoute(where + "." + lineKey, "no es una línea del Mapa");
            }

            return new RiverMapPlace(index, (float)RouteUnit(obj, sKey, where));
        }

        private static double RouteUnit(JsonValue node, string key, string at)
        {
            JsonValue? v = node.Get(key);
            double d = v != null && v.Kind == JsonKind.Number ? v.AsDouble() : double.NaN;
            if (!(d >= 0.0 && d <= 1.0))
            {
                throw FailRoute(at + "." + key, "debe ser una fracción en [0, 1]");
            }

            return d;
        }

        private static string[] ReadVia(JsonValue leg, string at, RiverMap map)
        {
            JsonValue? list = leg.Get("via");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw FailRoute(at + ".leg.via", "debe ser una lista de puntos de paso");
            }

            var via = new string[list.Items.Count];
            for (int k = 0; k < via.Length; k++)
            {
                JsonValue v = list.Items[k];
                if (v.Kind != JsonKind.String || map.PassingPoint(v.AsString()) == null)
                {
                    throw FailRoute(at + ".leg.via[" + Num(k) + "]", "no es un punto de paso del Mapa (passing)");
                }

                via[k] = v.AsString();
            }

            return via;
        }

        private static int[] ReadMouths(JsonValue leg, string at, RiverMap map)
        {
            JsonValue? list = leg.Get("mouths");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw FailRoute(at + ".leg.mouths", "debe ser una lista de líneas");
            }

            var mouths = new int[list.Items.Count];
            for (int k = 0; k < mouths.Length; k++)
            {
                JsonValue v = list.Items[k];
                int index = v.Kind == JsonKind.String ? map.LineIndexOf(v.AsString()) : -1;
                if (index < 0 || map.Lines[index].Kind == RiverLineKind.Main)
                {
                    throw FailRoute(at + ".leg.mouths[" + Num(k) + "]", "no es una línea del Mapa con boca");
                }

                mouths[k] = index;
            }

            return mouths;
        }

        // ============================================================================================ base64 (P-M1)

        /// <summary>
        /// Decodifica <c>pts</c> o <c>verts</c>: base64 (RFC 4648, estricto: sin espacios y con el relleno exacto) de pares uint16
        /// little-endian, el primero absoluto y los demás deltas módulo 65.536 (08 §4.5; P-M1). Devuelve x0, y0, x1, y1… absolutos.
        /// </summary>
        /// <exception cref="LevelFormatException">No es base64, no son pares de uint16 o está vacío; el mensaje lleva <paramref name="where"/>.</exception>
        public static ushort[] DecodePoints(string base64, string where)
        {
            ushort[] values = DecodeUInt16(base64, where);
            if (values.Length == 0 || values.Length % 2 != 0)
            {
                throw Fail(where, Num(values.Length * 2) + " bytes no son pares (x, y) de uint16 (múltiplo de 4, al menos uno)");
            }

            for (int i = 2; i < values.Length; i++)
            {
                values[i] = unchecked((ushort)(values[i] + values[i - 2]));
            }

            return values;
        }

        /// <summary>Decodifica <c>tris</c>: base64 (RFC 4648, estricto) de enteros uint16 little-endian absolutos.</summary>
        /// <exception cref="LevelFormatException">No es base64 o los bytes no son uint16; el mensaje lleva <paramref name="where"/>.</exception>
        public static ushort[] DecodeUInt16(string base64, string where)
        {
            if (!IsStrictBase64(base64))
            {
                throw Fail(where, "no es base64 (RFC 4648: A-Z, a-z, 0-9, + y /, con el relleno = exacto)");
            }

            byte[] bytes = Convert.FromBase64String(base64);
            if (bytes.Length % 2 != 0)
            {
                throw Fail(where, Num(bytes.Length) + " bytes no son enteros uint16 (múltiplo de 2)");
            }

            var values = new ushort[bytes.Length / 2];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (ushort)(bytes[2 * i] | (bytes[(2 * i) + 1] << 8));
            }

            return values;
        }

        // El patrón del esquema: ^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$ (Convert admite además espacios).
        private static bool IsStrictBase64(string text)
        {
            if (text.Length == 0 || text.Length % 4 != 0)
            {
                return false;
            }

            int padding = text[text.Length - 1] == '=' ? (text[text.Length - 2] == '=' ? 2 : 1) : 0;
            for (int i = 0; i < text.Length - padding; i++)
            {
                char c = text[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/'))
                {
                    return false;
                }
            }

            return true;
        }

        // ============================================================================================ campos

        private static RiverMapPlace ReadPlace(JsonValue node, string key, string at, string[] ids, string lineKey)
        {
            string where = Join(at, key);
            JsonValue? place = node.Get(key);
            if (place == null || place.Kind != JsonKind.Object)
            {
                throw Fail(where, "debe ser un objeto {" + lineKey + ", s}");
            }

            int line = RequireLineId(RequireSlug(place, lineKey, where), ids, where + "." + lineKey);
            double s = RequireUnit(place, "s", where);
            return new RiverMapPlace(line, (float)s);
        }

        private static int RequireLineId(string id, string[] ids, string where)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            throw Fail(where, "'" + id + "' no es una línea del Mapa");
        }

        private static void ReadXy(JsonValue node, string at, double aspect, out float x, out float y)
        {
            string where = at + ".xy";
            JsonValue? xy = node.Get("xy");
            if (xy == null || xy.Kind != JsonKind.Array || xy.Items.Count != 2)
            {
                throw Fail(where, "debe ser [X, Y] (uint16)");
            }

            int rx = ToUInt16(xy.Items[0], where + "[0]");
            int ry = ToUInt16(xy.Items[1], where + "[1]");
            x = (float)(rx / (double)Quantum);
            y = (float)(ry / (double)Quantum * aspect);
        }

        private static int ToUInt16(JsonValue v, string where)
        {
            if (v.Kind != JsonKind.Number)
            {
                throw Fail(where, "debe ser un entero uint16");
            }

            int value = ToInt(v, where);
            if (value < 0 || value > Quantum)
            {
                throw Fail(where, Num(value) + " fuera de 0.." + Num(Quantum));
            }

            return value;
        }

        private static RiverLineKind ParseKind(string value, string where)
        {
            switch (value)
            {
                case "main":
                    return RiverLineKind.Main;
                case "tributary":
                    return RiverLineKind.Tributary;
                case "passby":
                    return RiverLineKind.Passby;
                case "local":
                    return RiverLineKind.Local;
                default:
                    throw Fail(where, "'" + value + "' no es main, tributary, passby ni local");
            }
        }

        private static RiverWater ParseWater(string value, string where, bool allowAndean)
        {
            switch (value)
            {
                case "white":
                    return RiverWater.White;
                case "black":
                    return RiverWater.Black;
                case "clear":
                    return RiverWater.Clear;
                case "andean" when allowAndean:
                    return RiverWater.Andean;
                default:
                    throw Fail(where, "'" + value + "' no es " + (allowAndean ? "white, black, clear ni andean" : "white, black ni clear"));
            }
        }

        private static string? OptionalFact(JsonValue node, string at)
        {
            string where = at + ".fact";
            JsonValue? v = node.Get("fact");
            if (v == null)
            {
                throw Fail(where, "falta el campo (clave map.* o null)");
            }

            if (v.IsNull)
            {
                return null;
            }

            if (v.Kind != JsonKind.String || !IsFactKey(v.AsString()))
            {
                throw Fail(where, "no es una clave de contenido map.* (glosario §10)");
            }

            return CheckBelen(v.AsString(), where);
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

        // pts, verts y tris: datos binarios en base64, no nombres; el veto de «belen» no se les aplica (un base64 largo
        // podría contener esas cinco letras por azar).
        private static string RequireBase64(JsonValue node, string key, string at)
        {
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.String)
            {
                throw Fail(Join(at, key), "falta la cadena base64");
            }

            return v.AsString();
        }

        private static string RequireSlug(JsonValue node, string key, string at)
        {
            string value = RequireString(node, key, at);
            if (!IsId(value, 0, value.Length))
            {
                throw Fail(Join(at, key), "'" + value + "' no es [a-z0-9_]+");
            }

            return value;
        }

        private static double RequireNumber(JsonValue node, string key, string at)
        {
            string where = Join(at, key);
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.Number)
            {
                throw Fail(where, "falta el número");
            }

            double d = v.AsDouble();
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                throw Fail(where, "no es un número finito");
            }

            return d;
        }

        private static double RequireUnit(JsonValue node, string key, string at)
        {
            double d = RequireNumber(node, key, at);
            if (!(d >= 0.0 && d <= 1.0))
            {
                throw Fail(Join(at, key), Dbl(d) + " fuera de [0, 1]");
            }

            return d;
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

        private static int ToInt(JsonValue v, string where)
        {
            try
            {
                return v.AsInt();
            }
            catch (FormatException e)
            {
                throw new LevelFormatException("Mapa del río, " + where + ": " + e.Message, e);
            }
        }

        // Los formatos se comprueban carácter a carácter, como RiverStopCatalogReader (sin expresiones regulares: en .NET, «$»
        // admite un salto de línea final).

        private static bool IsId(string value, int start, int end)
        {
            if (end <= start)
            {
                return false;
            }

            for (int i = start; i < end; i++)
            {
                char c = value[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIso2(string value)
        {
            return value.Length == 2 && value[0] >= 'A' && value[0] <= 'Z' && value[1] >= 'A' && value[1] <= 'Z';
        }

        // Claves de dato del Mapa (esquema, $defs/fact_key; glosario §10): map.stop.<id>.dato_NN, map.country.<aa>.dato_NN,
        // map.myth.<id> y map.general.<id>.
        private static bool IsFactKey(string key)
        {
            if ((key.StartsWith(MythFactPrefix, StringComparison.Ordinal) && IsId(key, MythFactPrefix.Length, key.Length))
                || (key.StartsWith(GeneralFactPrefix, StringComparison.Ordinal) && IsId(key, GeneralFactPrefix.Length, key.Length)))
            {
                return true;
            }

            int dato = key.Length - DatoSuffix.Length - 2;
            if (dato < 0 || string.CompareOrdinal(key, dato, DatoSuffix, 0, DatoSuffix.Length) != 0
                || !IsDigit(key[key.Length - 2]) || !IsDigit(key[key.Length - 1]))
            {
                return false;
            }

            if (key.StartsWith(StopFactPrefix, StringComparison.Ordinal))
            {
                return IsId(key, StopFactPrefix.Length, dato);
            }

            int country = CountryFactPrefix.Length;
            return key.StartsWith(CountryFactPrefix, StringComparison.Ordinal) && dato == country + 2
                && key[country] >= 'a' && key[country] <= 'z' && key[country + 1] >= 'a' && key[country + 1] <= 'z';
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static string CheckBelen(string value, string where)
        {
            if (value.IndexOf(Belen, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw Fail(where, "'" + value + "' lleva «belen» (D-013, punto 11)");
            }

            return value;
        }

        private static JsonValue Parse(string json, string what)
        {
            try
            {
                return JsonParser.Parse(json);
            }
            catch (FormatException e)
            {
                throw new LevelFormatException(what + " no es JSON válido: " + e.Message, e);
            }
        }

        private static string Join(string at, string key)
        {
            return at.Length == 0 ? key : at + "." + key;
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Dbl(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Flt(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static LevelFormatException Fail(string where, string message)
        {
            return new LevelFormatException("Mapa del río, " + where + ": " + message + ".");
        }

        private static LevelFormatException FailRoute(string where, string message)
        {
            return new LevelFormatException("Rutas del Mapa, " + where + ": " + message + ".");
        }
    }
}
