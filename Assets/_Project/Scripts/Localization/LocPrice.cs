#nullable enable
using System;

namespace AF.Localization
{
    /// <summary>
    /// Precios con el mismo formato que el resto de las cifras (D-025, «un solo formato de cifras, también en los precios»):
    /// código ISO 4217 de la moneda, un espacio y el importe con los separadores de <see cref="LocNumber"/> (los de la región del
    /// teléfono, Q8-A de D-018; sin región, es-CO [P]) y los decimales de la moneda (<see cref="DecimalsOf"/>: 0 en COP y MXN, 2
    /// en el resto, 05 §4.2; y, por el añadido de D-025 de Q-IAP-3, 0 en CLP y JPY y 3 en KWD y BHD). Los importes de 05 §4.2 no cambian, solo su escritura: con el teléfono en en-US, «COP 3,900»; en
    /// es-CO, «COP 3.900»; en es-MX, «MXN 19»; en pt-BR, «BRL 4,99».
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
        /// Decimales con que se escribe un importe en <paramref name="iso4217"/> (D-025, punto 2; 05 §4.2): 0 en COP y MXN, 2 en
        /// el resto. Añadido de D-025 por Q-IAP-3 (D-040; <c>LocalPrice</c> admite cualquier código ISO 4217): las monedas sin
        /// céntimos, CLP y JPY, con 0, y las de tres decimales, KWD y BHD, con 3.
        /// </summary>
        /// <exception cref="ArgumentException">No es un código ISO 4217 (tres letras mayúsculas).</exception>
        public static int DecimalsOf(string iso4217)
        {
            CheckCode(iso4217);
            switch (iso4217)
            {
                case "COP":
                case "MXN":
                case "CLP":
                case "JPY":
                    return 0;
                case "KWD":
                case "BHD":
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
