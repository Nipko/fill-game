#nullable enable
using System;
using AF.Levels.Json;

namespace AF.Levels
{
    /// <summary>
    /// Lo que lee <see cref="RiverMapReader"/> de los pueblos de paso (D-069, bloque M2-2; propuesta del Mapa v2 §4 y §9.1; contratos
    /// 29 y 30; esquemas <c>schemas/af-river-map-2.schema.json</c> y <c>af-river-stops-2.schema.json</c>): la viñeta, el país y el
    /// tier de cada <c>passing[]</c>, y la excursión local de los tramos de 0 km (<c>leg.excursion</c>). La fracción de cada
    /// <c>leg.via</c> la lee <c>ReadVia</c>. Mismas reglas que el esquema; la cadencia, el anclaje y los vetos los comprueba
    /// <c>tools/river-map</c>.
    /// </summary>
    public static partial class RiverMapReader
    {
        private static RiverPassingPoint ReadTown(JsonValue p, string w, string id, RiverMapPlace at, string? fact)
        {
            string tier = RequireString(p, "tier", w);
            RiverTownTier t;
            if (string.Equals(tier, "town", StringComparison.Ordinal))
            {
                t = RiverTownTier.Town;
            }
            else if (string.Equals(tier, "city", StringComparison.Ordinal))
            {
                t = RiverTownTier.City;
            }
            else if (string.Equals(tier, "mark", StringComparison.Ordinal))
            {
                t = RiverTownTier.Mark;
            }
            else
            {
                throw Fail(w + ".tier", "'" + tier + "' no es town, city ni mark");
            }

            string country = RequireString(p, "country", w);
            if (country.Length != 2 || country[0] < 'A' || country[0] > 'Z' || country[1] < 'A' || country[1] > 'Z')
            {
                throw Fail(w + ".country", "'" + country + "' no es un código ISO2");
            }

            JsonValue? v = p.Get("vignette");
            RiverTownVignette vignette = RiverTownVignette.None;
            if (v == null)
            {
                throw Fail(w + ".vignette", "falta (null en un mark)");
            }

            if (v.Kind == JsonKind.String)
            {
                vignette = VignetteOf(v.AsString(), w);
            }
            else if (v.Kind != JsonKind.Null)
            {
                throw Fail(w + ".vignette", "debe ser una cadena o null");
            }

            if ((t == RiverTownTier.Mark) != (vignette == RiverTownVignette.None))
            {
                throw Fail(w + ".vignette", "un mark va sin viñeta y un pueblo con ella");
            }

            return new RiverPassingPoint(id, at, fact, vignette, country, t);
        }

        private static RiverTownVignette VignetteOf(string value, string w)
        {
            switch (value)
            {
                case "stilts":
                    return RiverTownVignette.Stilts;
                case "church":
                    return RiverTownVignette.Church;
                case "port":
                    return RiverTownVignette.Port;
                case "market":
                    return RiverTownVignette.Market;
                default:
                    throw Fail(w + ".vignette", "'" + value + "' no es stilts, church, port ni market");
            }
        }

        // La excursión local (P5 (a)): solo en un tramo sin largo, desde la parada, por su misma línea.
        private static RiverExcursion? ReadExcursion(JsonValue leg, string at, RiverMap map, RiverMapPlace place, RiverPathPiece[] path)
        {
            JsonValue? e = leg.Get("excursion");
            if (e == null)
            {
                return null;
            }

            string w = at + ".leg.excursion";
            if (e.Kind != JsonKind.Object)
            {
                throw FailRoute(w, "debe ser un objeto {line, s0, s1}");
            }

            for (int k = 0; k < path.Length; k++)
            {
                if (path[k].S0 != path[k].S1)
                {
                    throw FailRoute(w, "solo en un tramo de 0 km");
                }
            }

            RiverMapPlace start = RoutePlace(e, null, "line", "s0", w, map);
            float s1 = (float)RouteUnit(e, "s1", w);
            if (start.Line != place.Line || Math.Abs(start.S - place.S) > 1e-5f)
            {
                throw FailRoute(w, "debe salir de la parada (" + map.Lines[place.Line].Id + " s=" + Flt(place.S) + ")");
            }

            if (Math.Abs(s1 - start.S) <= 1e-6f)
            {
                throw FailRoute(w + ".s1", "la excursión no tiene largo");
            }

            return new RiverExcursion(start.Line, start.S, s1);
        }
    }
}
