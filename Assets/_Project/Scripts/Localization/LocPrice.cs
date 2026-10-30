#nullable enable
using System;

namespace AF.Localization
{
    /// <summary>
    /// Precios con el mismo formato que el resto de las cifras (D-025, «un solo formato de cifras, también en los precios»):
    /// código ISO 4217 de la moneda, un espacio y el importe con los separadores de <see cref="LocNumber"/> (los del idioma del
    /// juego y, en español, del país: añadido de D-025 del 2026-10-30, observación 22 de la jornada 6; sin país, es-CO) y los decimales de la moneda (<see cref="DecimalsOf"/>: 0 en COP y MXN, 2
    /// en el resto, 05 §4.2; por el añadido de D-025 de Q-IAP-3, las monedas sin céntimos y las de tres decimales de ISO 4217; y,
    /// con D-057, la tabla ISO 4217 completa más IDR, HUF y TWD sin decimales, como las escribe Google Play). Los importes de 05 §4.2 no cambian, solo su escritura: con la UI en en, «COP 3,900»; en
    /// es-419 en Colombia, «COP 3.900»; en es-419 en México, «MXN 19»; en pt-BR, «BRL 4,99».
    /// <para>Es la única función de precios: la usan la tienda simulada (<c>AF.IAP</c>: <c>LocalPrice</c>,
    /// <c>FakeStoreService</c>) y, por ella, todos los textos con precio de la UI (<c>GameServices.PriceText</c>). Con la tienda
    /// real (el plugin propio de Play Billing Library 9.0.0, D-040; D-025, punto 3 [P]), <c>AF.IAP.PlayBillingClient</c> compone el
    /// precio desde el importe y la moneda del producto (<c>getPriceAmountMicros</c> y <c>getPriceCurrencyCode</c>) con
    /// <see cref="MinorUnitsFromMicros"/> y <see cref="Format"/>, en lugar de la cadena ya formateada de Google Play, para que la tienda simulada y la real se vean
    /// iguales.</para>
    /// </summary>
    public static class LocPrice
    {
        /// <summary>Micros por unidad de moneda (<c>priceAmountMicros</c> de Play Billing: 1 unidad = 1.000.000).</summary>
        public const long MicrosPerUnit = 1000000L;

        private static readonly long[] Pow10 = { 1L, 10L, 100L, 1000L };

        /// <summary>
        /// Decimales con que se escribe un importe en <paramref name="iso4217"/> (D-025, punto 2; 05 §4.2): 0 en COP y MXN (decisión
        /// de D-025, aunque ISO 4217 les da 2), 2 en el resto. Añadido de D-025 por Q-IAP-3 (D-040; <c>LocalPrice</c> admite cualquier
        /// código ISO 4217) y por D-057 (lanzamiento mundial; <c>docs/dev/lanzamiento-mundial.md</c> §2.3), con la tabla de unidades
        /// menores de ISO 4217:
        /// <list type="bullet">
        /// <item>0: las monedas sin céntimos de ISO 4217 (BIF, CLP, DJF, GNF, ISK, JPY, KMF, KRW, PYG, RWF, UGX, UYI, VND, VUV, XAF,
        /// XOF y XPF) y las que Google Play escribe sin decimales aunque ISO 4217 les dé 2: IDR, HUF y TWD [P; NV, confirmar en una
        /// compra de prueba];</item>
        /// <item>3: BHD, IQD, JOD, KWD, LYD, OMR y TND;</item>
        /// <item>2: el resto (también un código que ISO 4217 no tenga: el importe sigue siendo el de Play).</item>
        /// </list>
        /// Las de 4 decimales de ISO (CLF, UYW) no son monedas de venta y quedan en 2.
        /// </summary>
        /// <exception cref="ArgumentException">No es un código ISO 4217 (tres letras mayúsculas).</exception>
        public static int DecimalsOf(string iso4217)
        {
            CheckCode(iso4217);
            switch (iso4217)
            {
                case "COP": // D-025
                case "MXN": // D-025
                case "BIF":
                case "CLP":
                case "DJF":
                case "GNF":
                case "ISK":
                case "JPY":
                case "KMF":
                case "KRW":
                case "PYG":
                case "RWF":
                case "UGX":
                case "UYI":
                case "VND":
                case "VUV":
                case "XAF":
                case "XOF":
                case "XPF":
                case "IDR": // Google Play, sin decimales [P]
                case "HUF": // Google Play, sin decimales [P]
                case "TWD": // Google Play, sin decimales [P]
                    return 0;
                case "BHD":
                case "IQD":
                case "JOD":
                case "KWD":
                case "LYD":
                case "OMR":
                case "TND":
                    return 3;
                default:
                    return 2;
            }
        }

        /// <summary>
        /// Importe en unidades menores de <paramref name="iso4217"/> (las de <see cref="DecimalsOf"/>: céntimos con 2 decimales,
        /// milésimas con 3, la unidad con 0) desde los micros de Play Billing, redondeado «la mitad hacia fuera» (p. ej. BRL
        /// 4.990.000 → 499; COP 3.900.000.000 → 3.900; KWD 1.250.000 → 1.250).
        /// </summary>
        public static long MinorUnitsFromMicros(string iso4217, long micros)
        {
            int decimals = DecimalsOf(iso4217);
            if (micros < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(micros), "Un precio no es negativo.");
            }

            long per = MicrosPerUnit / Pow10[decimals];
            return (micros / per) + ((micros % per) * 2 >= per ? 1 : 0);
        }

        /// <summary>
        /// Precio como se ve: «código importe» (p. ej. «COP 3.900» o «BRL 4,99») con los separadores de
        /// <paramref name="format"/>. <paramref name="minorUnits"/> va en las unidades de <see cref="DecimalsOf"/>.
        /// </summary>
        public static string Format(string iso4217, long minorUnits, in LocNumberFormat format)
        {
            CheckCode(iso4217);
            var buffer = new char[40];
            iso4217.CopyTo(0, buffer, 0, 3);
            buffer[3] = ' ';
            int n = WriteAmount(buffer, 4, iso4217, minorUnits, format);
            return new string(buffer, 0, n);
        }

        /// <summary>Solo el importe (sin el código), p. ej. «3.900» o «4,99», con los separadores de <paramref name="format"/>.</summary>
        public static string FormatAmount(string iso4217, long minorUnits, in LocNumberFormat format)
        {
            var buffer = new char[36];
            int n = WriteAmount(buffer, 0, iso4217, minorUnits, format);
            return new string(buffer, 0, n);
        }

        private static int WriteAmount(char[] buffer, int pos, string iso4217, long minorUnits, in LocNumberFormat format)
        {
            int decimals = DecimalsOf(iso4217);
            if (minorUnits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minorUnits), "Un precio no es negativo.");
            }

            long scale = Pow10[decimals];
            int at = LocNumber.Write(buffer, pos, minorUnits / scale, true, format);
            if (decimals == 0)
            {
                return at;
            }

            buffer[at++] = format.Decimal;
            long fraction = minorUnits % scale;
            for (int i = decimals - 1; i >= 0; i--)
            {
                buffer[at + i] = (char)('0' + (int)(fraction % 10L));
                fraction /= 10L;
            }

            return at + decimals;
        }

        private static void CheckCode(string iso4217)
        {
            if (iso4217 == null)
            {
                throw new ArgumentNullException(nameof(iso4217));
            }

            if (iso4217.Length != 3 || !IsUpper(iso4217[0]) || !IsUpper(iso4217[1]) || !IsUpper(iso4217[2]))
            {
                throw new ArgumentException("No es un código ISO 4217 de tres letras mayúsculas: «" + iso4217 + "».", nameof(iso4217));
            }
        }

        private static bool IsUpper(char c)
        {
            return c >= 'A' && c <= 'Z';
        }
    }
}
