#nullable enable
using AF.Boosters;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// <c>geo_tier</c> del cliente (bloque B5; Q-E3 (a) de D-033: «región del dispositivo (<c>IDeviceLocale</c>) y, cuando llegue el
    /// IAP, el país de la tienda»). 04 §6.2 y 05 §6.3 lo definen: <b>T1</b> = EE. UU., Canadá, Europa occidental, Australia, Japón y
    /// Corea; <b>T3</b> = América Latina salvo Chile y Uruguay. Solo cambia el orden Balsa / Puñado del modal sin saldo (05 §10: la
    /// economía no se segmenta). Lo que el GDD no fija va [P] (Q-B5-11): la lista de «Europa occidental» (<see cref="T1Regions"/>) y
    /// que Chile, Uruguay, una región ajena a las dos listas o sin región queden en T3, el valor de siempre (el enum no tiene otro).
    /// La región se lee como la lee el idioma (<see cref="LocLocale.HasRegion"/>: tras el idioma y el script, nunca dentro de una
    /// extensión como <c>en-u-ca-gregory</c>).
    /// </summary>
    public static class GeoTierRule
    {
        /// <summary>
        /// Regiones T1 (ISO 3166-1 alfa-2): US, CA, AU, JP, KR y, como «Europa occidental» [P], los países del Espacio Económico
        /// Europeo del oeste más el Reino Unido, Suiza y los microestados (Andorra, Mónaco, San Marino y Liechtenstein).
        /// </summary>
        public static readonly string[] T1Regions =
        {
            "US", "CA", "AU", "JP", "KR",
            "AD", "AT", "BE", "CH", "DE", "DK", "ES", "FI", "FR", "GB", "IE", "IS", "IT", "LI", "LU", "MC", "NL", "NO", "PT", "SE", "SM",
        };

        /// <summary><c>geo_tier</c> como lo escribe la analítica (08 §8.1 y §8.2): <c>T1</c> o <c>T3</c>.</summary>
        public static string IdOf(GeoTier tier)
        {
            return tier == GeoTier.T1 ? "T1" : "T3";
        }

        /// <summary><c>geo_tier</c> de la etiqueta del teléfono: T1 si su región está en <see cref="T1Regions"/>; si no, T3. Sin asignaciones.</summary>
        public static GeoTier FromLanguageTag(string? languageTag)
        {
            if (languageTag == null || !LocLocale.HasAnyRegion(languageTag))
            {
                return GeoTier.T3;
            }

            for (int i = 0; i < T1Regions.Length; i++)
            {
                if (LocLocale.HasRegion(languageTag, T1Regions[i]))
                {
                    return GeoTier.T1;
                }
            }

            return GeoTier.T3;
        }
    }
}
