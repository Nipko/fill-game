#nullable enable
using System;
using AF.Boosters;

namespace AF.UI
{
    /// <summary>
    /// <c>geo_tier</c> del cliente (bloque B5; Q-E3 (a) de D-033: «región del dispositivo (<c>IDeviceLocale</c>) y, cuando llegue el
    /// IAP, el país de la tienda»). 04 §6.2 y 05 §6.3 lo definen: <b>T1</b> = EE. UU., Canadá, Europa occidental, Australia, Japón y
    /// Corea; <b>T3</b> = América Latina salvo Chile y Uruguay. Solo cambia el orden Balsa / Puñado del modal sin saldo (05 §10: la
    /// economía no se segmenta). Lo que el GDD no fija va [P] (Q-B5-11): la lista de «Europa occidental» (<see cref="T1Regions"/>) y
    /// que Chile, Uruguay, una región ajena a las dos listas o sin región queden en T3, el valor de siempre (el enum no tiene otro).
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

        /// <summary>La región de una etiqueta BCP 47 (<c>es-CO</c> → <c>CO</c>; <c>pt-BR</c> → <c>BR</c>), en mayúsculas ASCII, o null.</summary>
        public static string? RegionOf(string? languageTag)
        {
            if (string.IsNullOrEmpty(languageTag))
            {
                return null;
            }

            string[] parts = languageTag!.Replace('_', '-').Split('-');
            for (int i = 1; i < parts.Length; i++)
            {
                string p = parts[i];
                if (p.Length == 2 && IsAsciiLetter(p[0]) && IsAsciiLetter(p[1]))
                {
                    return new string(new[] { Upper(p[0]), Upper(p[1]) });
                }
            }

            return null;
        }

        /// <summary><c>geo_tier</c> de la etiqueta del teléfono: T1 si su región está en <see cref="T1Regions"/>; si no, T3.</summary>
        public static GeoTier FromLanguageTag(string? languageTag)
        {
            string? region = RegionOf(languageTag);
            if (region == null)
            {
                return GeoTier.T3;
            }

            for (int i = 0; i < T1Regions.Length; i++)
            {
                if (string.Equals(T1Regions[i], region, StringComparison.Ordinal))
                {
                    return GeoTier.T1;
                }
            }

            return GeoTier.T3;
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static char Upper(char c)
        {
            return c >= 'a' && c <= 'z' ? (char)(c - 32) : c;
        }
    }
}
