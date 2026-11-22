#nullable enable
using System;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lo que lee <see cref="RiverMapReader"/> de <c>af-river-map/2</c> además de lo de <c>/1</c> (D-069, M2-1; contrato 29;
    /// esquema <c>schemas/af-river-map-2.schema.json</c>): la zona de banda de la várzea, el peso por vértice de la tierra
    /// (<c>land.shade</c>), las orillas (<c>banks</c>) y la vegetación (<c>veg</c>). Mismas reglas que el esquema; lo demás
    /// (curvatura, solapes, lod) lo comprueba <c>tools/river-map/geometry.py --check</c>.
    /// </summary>
    public static partial class RiverMapReader
    {
        /// <summary>Radio de la Tierra de la herramienta (<c>EARTH_KM</c> de <c>geometry.py</c>).</summary>
        public const double EarthKm = 6371.0;

        /// <summary>Tope de la banda de una zona (dp) [P]: más ancha que media pantalla no tiene sentido.</summary>
        public const int MaxBandDp = 200;

        /// <summary>Tope del ancho de un tramo de orilla (dp) [P].</summary>
        public const int MaxBankDp = 32;

        private static RiverLandZone ReadBandZone(JsonValue zone, string w, string id, string tile)
        {
            if (zone.Get("first") != null || zone.Get("count") != null)
            {
                throw Fail(w, "una zona de banda no lleva first ni count");
            }

            int band = RequireInt(zone, "band", w);
            if (band < 1 || band > MaxBandDp)
            {
                throw Fail(w + ".band", Num(band) + " fuera de [1, " + Num(MaxBandDp) + "]");
            }

            return new RiverLandZone(id, tile, 0, 0, band);
        }

        private static byte[] ReadShade(JsonValue land, int vertices)
        {
            byte[] shade = DecodeBytes(RequireBase64(land, "shade", "land"), "land.shade");
            if (shade.Length != vertices)
            {
                throw Fail("land.shade", Num(shade.Length) + " bytes para " + Num(vertices) + " vértices (uno por vértice)");
            }

            return shade;
        }

        private static RiverBanks ReadBanks(JsonValue root, string[] ids)
        {
            JsonValue? list = root.Get("banks");
            if (list == null || list.Kind != JsonKind.Array)
            {
                throw Fail("banks", "debe ser una lista");
            }

            var banks = new RiverBank[list.Items.Count];
            int lastLine = -1;
            double lastS0 = -1.0;
            for (int i = 0; i < banks.Length; i++)
            {
                string at = "banks[" + Num(i) + "]";
                JsonValue node = list.Items[i];
                if (node.Kind != JsonKind.Object)
                {
                    throw Fail(at, "debe ser un objeto {line, s0, s1, side, kind, w}");
                }

                int line = RequireLineId(RequireSlug(node, "line", at), ids, at + ".line");
                double s0 = RequireUnit(node, "s0", at);
                double s1 = RequireUnit(node, "s1", at);
                if (!(s1 > s0))
                {
                    throw Fail(at, "s1 (" + Dbl(s1) + ") debe ser mayor que s0 (" + Dbl(s0) + ")");
                }

                if (line < lastLine || (line == lastLine && s0 < lastS0))
                {
                    throw Fail(at, "fuera de orden: van por línea (en el orden de lines) y por s0");
                }

                lastLine = line;
                lastS0 = s0;
                string side = RequireString(node, "side", at);
                bool left = string.Equals(side, "left", StringComparison.Ordinal);
                if (!left && !string.Equals(side, "right", StringComparison.Ordinal))
                {
                    throw Fail(at + ".side", "'" + side + "' no es left ni right");
                }

                string kind = RequireString(node, "kind", at);
                RiverBankKind k;
                if (string.Equals(kind, "beach", StringComparison.Ordinal))
                {
                    k = RiverBankKind.Beach;
                }
                else if (string.Equals(kind, "bluff", StringComparison.Ordinal))
                {
                    k = RiverBankKind.Bluff;
                }
                else
                {
                    throw Fail(at + ".kind", "'" + kind + "' no es beach ni bluff");
                }

                int width = RequireInt(node, "w", at);
                if (width < 1 || width > MaxBankDp)
                {
                    throw Fail(at + ".w", Num(width) + " fuera de [1, " + Num(MaxBankDp) + "]");
                }

                banks[i] = new RiverBank(line, (float)s0, (float)s1, left, k, width);
            }

            return new RiverBanks(banks, ids.Length);
        }

        private static RiverVegetation ReadVegetation(JsonValue root, RiverMapFrame frame, double aspect)
        {
            JsonValue? veg = root.Get("veg");
            if (veg == null || veg.Kind != JsonKind.Object)
            {
                throw Fail("veg", "debe ser un objeto {cell, pts, info}");
            }

            double cell = RequireNumber(veg, "cell", "veg");
            if (!(cell > 0.0))
            {
                throw Fail("veg.cell", Dbl(cell) + " km no es positivo");
            }

            ushort[] raw = DecodePoints(RequireBase64(veg, "pts", "veg"), "veg.pts");
            byte[] info = DecodeBytes(RequireBase64(veg, "info", "veg"), "veg.info");
            int count = raw.Length / 2;
            if (info.Length != 2 * count)
            {
                throw Fail("veg.info", Num(info.Length) + " bytes para " + Num(count) + " puntos (dos por punto)");
            }

            var xy = new float[raw.Length];
            for (int i = 0; i < raw.Length; i += 2)
            {
                xy[i] = (float)(raw[i] / (double)Quantum);
                xy[i + 1] = (float)(raw[i + 1] / (double)Quantum * aspect);
            }

            for (int i = 0; i < count; i++)
            {
                if ((info[2 * i] & 3) == 3 || ((info[2 * i] >> 2) & 3) == 3)
                {
                    throw Fail("veg.info", "el punto " + Num(i) + " tiene un tipo o un tamaño de 3 (0-2)");
                }
            }

            double kmPerUnit = (frame.LatMax - frame.LatMin) * Math.PI / 180.0 * EarthKm;
            return new RiverVegetation((float)cell, xy, info, (float)kmPerUnit);
        }

        private static byte[] DecodeBytes(string base64, string where)
        {
            if (!IsStrictBase64(base64))
            {
                throw Fail(where, "no es base64 (RFC 4648: A-Z, a-z, 0-9, + y /, con el relleno = exacto)");
            }

            return Convert.FromBase64String(base64);
        }
    }
}
