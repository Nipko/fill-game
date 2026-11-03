#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Platform
{
    /// <summary>
    /// Códigos de país ISO 3166-1 (fase M2 de <c>docs/dev/traspaso/mac-ios.md</c>): StoreKit da el país de la tienda en alfa-3
    /// (<c>COL</c>) y el juego usa alfa-2 (<c>CO</c>; <see cref="ConsentCountry.Normalize"/>). Los 249 códigos oficiales vigentes.
    /// </summary>
    public static class CountryCodes
    {
        // Pares alfa-3 y alfa-2, en el orden alfabético del alfa-3.
        private static readonly string[] Pairs =
        {
            "ABW", "AW", "AFG", "AF", "AGO", "AO", "AIA", "AI", "ALA", "AX", "ALB", "AL", "AND", "AD", "ARE", "AE",
            "ARG", "AR", "ARM", "AM", "ASM", "AS", "ATA", "AQ", "ATF", "TF", "ATG", "AG", "AUS", "AU", "AUT", "AT",
            "AZE", "AZ", "BDI", "BI", "BEL", "BE", "BEN", "BJ", "BES", "BQ", "BFA", "BF", "BGD", "BD", "BGR", "BG",
            "BHR", "BH", "BHS", "BS", "BIH", "BA", "BLM", "BL", "BLR", "BY", "BLZ", "BZ", "BMU", "BM", "BOL", "BO",
            "BRA", "BR", "BRB", "BB", "BRN", "BN", "BTN", "BT", "BVT", "BV", "BWA", "BW", "CAF", "CF", "CAN", "CA",
            "CCK", "CC", "CHE", "CH", "CHL", "CL", "CHN", "CN", "CIV", "CI", "CMR", "CM", "COD", "CD", "COG", "CG",
            "COK", "CK", "COL", "CO", "COM", "KM", "CPV", "CV", "CRI", "CR", "CUB", "CU", "CUW", "CW", "CXR", "CX",
            "CYM", "KY", "CYP", "CY", "CZE", "CZ", "DEU", "DE", "DJI", "DJ", "DMA", "DM", "DNK", "DK", "DOM", "DO",
            "DZA", "DZ", "ECU", "EC", "EGY", "EG", "ERI", "ER", "ESH", "EH", "ESP", "ES", "EST", "EE", "ETH", "ET",
            "FIN", "FI", "FJI", "FJ", "FLK", "FK", "FRA", "FR", "FRO", "FO", "FSM", "FM", "GAB", "GA", "GBR", "GB",
            "GEO", "GE", "GGY", "GG", "GHA", "GH", "GIB", "GI", "GIN", "GN", "GLP", "GP", "GMB", "GM", "GNB", "GW",
            "GNQ", "GQ", "GRC", "GR", "GRD", "GD", "GRL", "GL", "GTM", "GT", "GUF", "GF", "GUM", "GU", "GUY", "GY",
            "HKG", "HK", "HMD", "HM", "HND", "HN", "HRV", "HR", "HTI", "HT", "HUN", "HU", "IDN", "ID", "IMN", "IM",
            "IND", "IN", "IOT", "IO", "IRL", "IE", "IRN", "IR", "IRQ", "IQ", "ISL", "IS", "ISR", "IL", "ITA", "IT",
            "JAM", "JM", "JEY", "JE", "JOR", "JO", "JPN", "JP", "KAZ", "KZ", "KEN", "KE", "KGZ", "KG", "KHM", "KH",
            "KIR", "KI", "KNA", "KN", "KOR", "KR", "KWT", "KW", "LAO", "LA", "LBN", "LB", "LBR", "LR", "LBY", "LY",
            "LCA", "LC", "LIE", "LI", "LKA", "LK", "LSO", "LS", "LTU", "LT", "LUX", "LU", "LVA", "LV", "MAC", "MO",
            "MAF", "MF", "MAR", "MA", "MCO", "MC", "MDA", "MD", "MDG", "MG", "MDV", "MV", "MEX", "MX", "MHL", "MH",
            "MKD", "MK", "MLI", "ML", "MLT", "MT", "MMR", "MM", "MNE", "ME", "MNG", "MN", "MNP", "MP", "MOZ", "MZ",
            "MRT", "MR", "MSR", "MS", "MTQ", "MQ", "MUS", "MU", "MWI", "MW", "MYS", "MY", "MYT", "YT", "NAM", "NA",
            "NCL", "NC", "NER", "NE", "NFK", "NF", "NGA", "NG", "NIC", "NI", "NIU", "NU", "NLD", "NL", "NOR", "NO",
            "NPL", "NP", "NRU", "NR", "NZL", "NZ", "OMN", "OM", "PAK", "PK", "PAN", "PA", "PCN", "PN", "PER", "PE",
            "PHL", "PH", "PLW", "PW", "PNG", "PG", "POL", "PL", "PRI", "PR", "PRK", "KP", "PRT", "PT", "PRY", "PY",
            "PSE", "PS", "PYF", "PF", "QAT", "QA", "REU", "RE", "ROU", "RO", "RUS", "RU", "RWA", "RW", "SAU", "SA",
            "SDN", "SD", "SEN", "SN", "SGP", "SG", "SGS", "GS", "SHN", "SH", "SJM", "SJ", "SLB", "SB", "SLE", "SL",
            "SLV", "SV", "SMR", "SM", "SOM", "SO", "SPM", "PM", "SRB", "RS", "SSD", "SS", "STP", "ST", "SUR", "SR",
            "SVK", "SK", "SVN", "SI", "SWE", "SE", "SWZ", "SZ", "SXM", "SX", "SYC", "SC", "SYR", "SY", "TCA", "TC",
            "TCD", "TD", "TGO", "TG", "THA", "TH", "TJK", "TJ", "TKL", "TK", "TKM", "TM", "TLS", "TL", "TON", "TO",
            "TTO", "TT", "TUN", "TN", "TUR", "TR", "TUV", "TV", "TWN", "TW", "TZA", "TZ", "UGA", "UG", "UKR", "UA",
            "UMI", "UM", "URY", "UY", "USA", "US", "UZB", "UZ", "VAT", "VA", "VCT", "VC", "VEN", "VE", "VGB", "VG",
            "VIR", "VI", "VNM", "VN", "VUT", "VU", "WLF", "WF", "WSM", "WS", "YEM", "YE", "ZAF", "ZA", "ZMB", "ZM",
            "ZWE", "ZW",
        };

        private static Dictionary<string, string>? s_alpha2;

        /// <summary>Cuántos códigos hay en la tabla (249).</summary>
        public static int Count => Pairs.Length / 2;

        /// <summary>
        /// El alfa-2 (<c>CO</c>) del alfa-3 <paramref name="alpha3"/> (<c>COL</c>, sin distinguir mayúsculas), o null si no es un código
        /// ISO 3166-1 vigente.
        /// </summary>
        public static string? Alpha2FromAlpha3(string? alpha3)
        {
            if (alpha3 == null || alpha3.Length != 3)
            {
                return null;
            }

            if (s_alpha2 == null)
            {
                var map = new Dictionary<string, string>(Pairs.Length / 2, StringComparer.Ordinal);
                for (int i = 0; i < Pairs.Length; i += 2)
                {
                    map[Pairs[i]] = Pairs[i + 1];
                }

                s_alpha2 = map;
            }

            return s_alpha2.TryGetValue(alpha3.ToUpperInvariant(), out string? alpha2) ? alpha2 : null;
        }
    }
}
