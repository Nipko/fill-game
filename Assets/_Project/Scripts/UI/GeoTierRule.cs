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
    /// <para>Con D-057 (T-M6), la segunda mitad de Q-E3 (a): si se sabe el país de la cuenta de Google Play (el de la tienda,
    /// <c>getBillingConfigAsync</c>, guardado desde la última conexión con Play), manda ese país; si no, la región del teléfono
    /// (<see cref="FromSources"/>). <b>No usa la SIM</b>, a diferencia del país legal (<c>ConsentCountry</c>): Q-E3 (a) fija como fuentes
    /// la región del dispositivo y el país de la tienda, porque el tier mide dónde paga el jugador (el orden Balsa/Puñado y la cohorte
    /// de ingresos) y ese país lo fija la cuenta de Play, que es la que pone la moneda y el precio; la SIM dice dónde está la línea,
    /// no dónde se compra. Con D-057 (decisiones de la revisión, punto 4), T1 suma NZ, SG, HK, TW, IL y el Golfo [P].</para>
    /// </summary>
    public static class GeoTierRule
    {
        /// <summary>
        /// Regiones T1 (ISO 3166-1 alfa-2): US, CA, AU, JP, KR y, como «Europa occidental» [P], los países del Espacio Económico
        /// Europeo del oeste más el Reino Unido, Suiza y los microestados (Andorra, Mónaco, San Marino y Liechtenstein). Con D-057
        /// (decisiones de la revisión del código mundial, punto 4; 04 §6.2 y 05 §6.3) [P]: Nueva Zelanda, Singapur, Hong Kong, Taiwán,
        /// Israel y los países del Golfo (AE, SA, QA, KW, BH y OM).
        /// </summary>
        public static readonly string[] T1Regions =
        {
            "US", "CA", "AU", "JP", "KR",
            "AD", "AT", "BE", "CH", "DE", "DK", "ES", "FI", "FR", "GB", "IE", "IS", "IT", "LI", "LU", "MC", "NL", "NO", "PT", "SE", "SM",
            "NZ", "SG", "HK", "TW", "IL", "AE", "SA", "QA", "KW", "BH", "OM",
        };

        /// <summary><c>geo_tier</c> como lo escribe la analítica (08 §8.1 y §8.2): <c>T1</c> o <c>T3</c>.</summary>
        public static string IdOf(GeoTier tier)
        {
            return tier == GeoTier.T1 ? "T1" : "T3";
        }

        /// <summary>
        /// <c>geo_tier</c> con las dos fuentes de Q-E3 (a) (D-057): el país de la cuenta de Play (<paramref name="playCountry"/>, ISO
        /// alfa-2; vacío o null si no se sabe) y, sin él, la región de <paramref name="languageTag"/>.
        /// </summary>
        public static GeoTier FromSources(string? playCountry, string? languageTag)
        {
            if (playCountry != null && playCountry.Length == 2)
            {
                return FromCountry(playCountry);
            }

            return FromLanguageTag(languageTag);
        }

        /// <summary><c>geo_tier</c> de un país (ISO alfa-2, sin distinguir mayúsculas): T1 si está en <see cref="T1Regions"/>; si no, T3.</summary>
        public static GeoTier FromCountry(string? country)
        {
            if (country == null || country.Length != 2)
            {
                return GeoTier.T3;
            }

            for (int i = 0; i < T1Regions.Length; i++)
            {
                if (string.Equals(T1Regions[i], country, System.StringComparison.OrdinalIgnoreCase))
                {
                    return GeoTier.T1;
                }
            }

            return GeoTier.T3;
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
