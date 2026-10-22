#nullable enable
using System;
using System.Collections.Generic;

namespace AF.IAP.PlayBilling.Tests
{
    /// <summary>
    /// <see cref="IBillingBridge"/> falso: apunta cada llamada al plugin Java y deja que el test responda como lo haría Play por
    /// <see cref="Listener"/> (en el hilo principal, como el puente real tras <c>InvokeOnUnityMainThread</c>).
    /// </summary>
    internal sealed class FakeBillingBridge : IBillingBridge
    {
        public FakeBillingBridge(IBillingBridgeListener listener)
        {
            Listener = listener;
        }

        public IBillingBridgeListener Listener { get; }

        public int ConnectCalls { get; private set; }

        public List<KeyValuePair<int, string>> ProductQueries { get; } = new List<KeyValuePair<int, string>>();

        public List<int> PurchaseQueries { get; } = new List<int>();

        public List<KeyValuePair<string, string?>> Launches { get; } = new List<KeyValuePair<string, string?>>();

        public List<string> Consumes { get; } = new List<string>();

        public List<string> Acknowledges { get; } = new List<string>();

        public int Disposed { get; private set; }

        /// <summary>La próxima llamada lanza (JNI roto, plugin ausente).</summary>
        public bool Throw { get; set; }

        public int LastProductsId => ProductQueries[ProductQueries.Count - 1].Key;

        public int LastPurchasesId => PurchaseQueries[PurchaseQueries.Count - 1];

        public void Connect()
        {
            ConnectCalls++;
            Maybe();
        }

        public void QueryProducts(int requestId, string skus)
        {
            Maybe();
            ProductQueries.Add(new KeyValuePair<int, string>(requestId, skus));
        }

        public void QueryPurchases(int requestId)
        {
            Maybe();
            PurchaseQueries.Add(requestId);
        }

        public void Launch(string sku, string? obfuscatedAccountId)
        {
            Maybe();
            Launches.Add(new KeyValuePair<string, string?>(sku, obfuscatedAccountId));
        }

        public void Consume(string token)
        {
            Maybe();
            Consumes.Add(token);
        }

        public void Acknowledge(string token)
        {
            Maybe();
            Acknowledges.Add(token);
        }

        public void Dispose() => Disposed++;

        /// <summary>Fila de compra del plugin.</summary>
        public static string Row(string sku, string token, int state = BillingRows.Purchased, long timeMs = 1792000000000L, bool acknowledged = false) =>
            sku + "\t" + token + "\t" + state + "\t" + timeMs + "\t" + (acknowledged ? "1" : "0");

        public static string Rows(params string[] rows) => string.Join("\n", rows);

        private void Maybe()
        {
            if (Throw)
            {
                throw new InvalidOperationException("JNI");
            }
        }
    }
}
