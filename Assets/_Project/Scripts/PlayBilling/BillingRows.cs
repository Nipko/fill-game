#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.IAP.PlayBilling
{
    /// <summary>Una compra tal como la da el plugin (<c>queryPurchasesAsync</c> o <c>onPurchasesUpdated</c>).</summary>
    public sealed class BillingPurchaseRow
    {
        public BillingPurchaseRow(string sku, string token, int purchaseState, DateTime? purchasedUtc, bool acknowledged)
        {
            Sku = sku;
            Token = token;
            PurchaseState = purchaseState;
            PurchasedUtc = purchasedUtc;
            Acknowledged = acknowledged;
        }

        public string Sku { get; }

        /// <summary><c>purchaseToken</c>. No va a los logs.</summary>
        public string Token { get; }

        /// <summary><c>Purchase.PurchaseState</c>: <see cref="BillingRows.Purchased"/> o <see cref="BillingRows.Pending"/>.</summary>
        public int PurchaseState { get; }

        public DateTime? PurchasedUtc { get; }

        /// <summary><c>isAcknowledged</c>.</summary>
        public bool Acknowledged { get; }

        /// <summary>Sin el token.</summary>
        public override string ToString() => "BillingPurchaseRow(" + Sku + ", " + PurchaseState + ")";
    }

    /// <summary>
    /// Las listas que manda el plugin Java como texto (<c>AfBillingListener</c>): una fila por línea y campos separados por tabuladores.
    /// Producto: <c>sku, moneda, micros</c>. Compra: <c>sku, token, estado, purchaseTime en ms, reconocida (1/0)</c>. Una fila que no
    /// cumple se salta (nunca se inventa un precio ni una compra).
    /// </summary>
    public static class BillingRows
    {
        /// <summary><c>Purchase.PurchaseState.PURCHASED</c>.</summary>
        public const int Purchased = 1;

        /// <summary><c>Purchase.PurchaseState.PENDING</c>.</summary>
        public const int Pending = 2;

        /// <summary>Productos con su moneda e importe (D-025, punto 3 [P]: nunca la cadena formateada de Play).</summary>
        public static List<PlayProductInfo> Products(string? rows)
        {
            var list = new List<PlayProductInfo>();
            foreach (string[] f in Split(rows, 3))
            {
                if (f[0].Length == 0 || f[1].Length == 0
                    || !long.TryParse(f[2], NumberStyles.None, CultureInfo.InvariantCulture, out long micros) || micros <= 0)
                {
                    continue;
                }

                list.Add(new PlayProductInfo(f[0], f[1], micros));
            }

            return list;
        }

        /// <summary>Compras en estado <c>PURCHASED</c> o <c>PENDING</c>.</summary>
        public static List<BillingPurchaseRow> Purchases(string? rows)
        {
            var list = new List<BillingPurchaseRow>();
            foreach (string[] f in Split(rows, 5))
            {
                if (f[0].Length == 0 || f[1].Length == 0
                    || !int.TryParse(f[2], NumberStyles.None, CultureInfo.InvariantCulture, out int state)
                    || (state != Purchased && state != Pending)
                    || !long.TryParse(f[3], NumberStyles.None, CultureInfo.InvariantCulture, out long ms)
                    || (f[4] != "0" && f[4] != "1"))
                {
                    continue;
                }

                DateTime? at = null;
                if (ms > 0 && ms <= MaxUnixMs)
                {
                    at = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                }

                list.Add(new BillingPurchaseRow(f[0], f[1], state, at, f[4] == "1"));
            }

            return list;
        }

        // 9999-12-31T23:59:59.999Z: lo que admite DateTimeOffset.
        private const long MaxUnixMs = 253402300799999L;

        private static IEnumerable<string[]> Split(string? rows, int fields)
        {
            if (string.IsNullOrEmpty(rows))
            {
                yield break;
            }

            foreach (string line in rows!.Split('\n'))
            {
                string[] f = line.TrimEnd('\r').Split('\t');
                if (f.Length == fields)
                {
                    yield return f;
                }
            }
        }
    }
}
