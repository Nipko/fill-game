#nullable enable
namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// Los avisos del plugin de StoreKit 2 (<c>Plugins/iOS/AfStoreKit.swift</c>; fase M3 de <c>docs/dev/traspaso/mac-ios.md</c>) en
    /// <see cref="IBillingBridgeListener"/>: el plugin manda (tipo, id de petición, código, texto A, texto B) y aquí se pasa al aviso del
    /// plugin de Play que corresponde. Sin código nativo, para probarlo en el editor; lo usa <c>IosBillingBridge</c> en el iPhone.
    /// </summary>
    public static class StoreKitBridgeEvents
    {
        public const int Setup = 0;
        public const int Disconnected = 1;
        public const int Products = 2;
        public const int Purchases = 3;
        public const int PurchasesUpdated = 4;
        public const int LaunchFailed = 5;
        public const int Consumed = 6;
        public const int Acknowledged = 7;
        public const int BillingConfig = 8;

        /// <summary>Pasa el aviso a <paramref name="listener"/>; false si el tipo no existe (no hace nada).</summary>
        public static bool Dispatch(IBillingBridgeListener listener, int kind, int requestId, int code, string? a, string? b)
        {
            switch (kind)
            {
                case Setup:
                    listener.OnSetupFinished(code);
                    return true;
                case Disconnected:
                    listener.OnDisconnected();
                    return true;
                case Products:
                    listener.OnProducts(requestId, code, b);
                    return true;
                case Purchases:
                    listener.OnPurchases(requestId, code, b);
                    return true;
                case PurchasesUpdated:
                    listener.OnPurchasesUpdated(code, b);
                    return true;
                case LaunchFailed:
                    listener.OnLaunchFailed(a, code);
                    return true;
                case Consumed:
                    listener.OnConsumed(a, code);
                    return true;
                case Acknowledged:
                    listener.OnAcknowledged(a, code);
                    return true;
                case BillingConfig:
                    listener.OnBillingConfig(requestId, code, a);
                    return true;
                default:
                    return false;
            }
        }
    }
}
