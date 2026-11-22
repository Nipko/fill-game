#nullable enable
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de los pueblos de paso del Mapa v2 (D-069, M2-2; propuesta del Mapa v2 §7.3): la etiqueta de TalkBack de cada pueblo,
    /// «Coari · pueblo de paso · Brasil». Solo claves de <c>strings.csv</c> con <see cref="Loc"/>; el nombre y el país salen de
    /// <c>facts.csv</c> (<c>map.stop.&lt;id&gt;.name</c> y <c>map.country.&lt;iso2&gt;.name</c>). Asigna su cadena solo cuando el
    /// lector la pide.
    /// </summary>
    public static class TownText
    {
        /// <summary>Todas las claves (registro de los tests).</summary>
        public static readonly string[] AllKeys = { Keys.Town, Keys.TownPlace, Keys.City, Keys.CityPlace };

        /// <summary>
        /// Etiqueta de TalkBack del pueblo <paramref name="point"/>: <c>a11y_map_town</c> o, en una ciudad, <c>a11y_map_city</c>; sin
        /// nombre de país aprobado, sus variantes sin país.
        /// </summary>
        public static string A11yTown(RiverPassingPoint? point)
        {
            if (point == null)
            {
                return string.Empty;
            }

            string place = Loc.Fact(point.NameKey);
            string country = point.CountryNameKey.Length > 0 ? Loc.Fact(point.CountryNameKey) : string.Empty;
            bool city = point.Tier == RiverTownTier.City;
            if (country.Length == 0)
            {
                return Loc.T(city ? Keys.CityPlace : Keys.TownPlace, LocArgs.Of("place", place));
            }

            return Loc.T(city ? Keys.City : Keys.Town, LocArgs.Of("place", place).And("country", country));
        }

        /// <summary>Claves de <c>strings.csv</c>.</summary>
        public static class Keys
        {
            /// <summary>«{place} · pueblo de paso · {country}» [P].</summary>
            public const string Town = "a11y_map_town";

            /// <summary>«{place} · pueblo de paso» [P].</summary>
            public const string TownPlace = "a11y_map_town_place";

            /// <summary>«{place} · ciudad de paso · {country}» [P].</summary>
            public const string City = "a11y_map_city";

            /// <summary>«{place} · ciudad de paso» [P].</summary>
            public const string CityPlace = "a11y_map_city_place";
        }
    }
}
