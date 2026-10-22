#nullable enable
using System;

namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// Las llamadas al plugin Java <c>com.amazonfill.billing.AfBilling</c> (<c>AfBilling.androidlib</c>), sin JNI: en el teléfono las hace
    /// <c>AndroidBillingBridge</c> (solo <c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>); en los tests, un puente falso. Cada llamada responde
    /// por <see cref="IBillingBridgeListener"/>, en el hilo principal de Unity. Ninguna escribe en el log ni habla con otro host que Play.
    /// </summary>
    public interface IBillingBridge : IDisposable
    {
        /// <summary><c>startConnection</c> (o OK en el acto si ya está conectado) → <see cref="IBillingBridgeListener.OnSetupFinished"/>.</summary>
        void Connect();

        /// <summary><c>queryProductDetailsAsync</c> de los SKU (uno por línea) → <see cref="IBillingBridgeListener.OnProducts"/>.</summary>
        void QueryProducts(int requestId, string skus);

        /// <summary><c>queryPurchasesAsync</c> (<c>INAPP</c>) → <see cref="IBillingBridgeListener.OnPurchases"/>.</summary>
        void QueryPurchases(int requestId);

        /// <summary>
        /// <c>launchBillingFlow</c> con <c>setObfuscatedAccountId</c> (si no es null) → <see cref="IBillingBridgeListener.OnPurchasesUpdated"/>
        /// o <see cref="IBillingBridgeListener.OnLaunchFailed"/>.
        /// </summary>
        void Launch(string sku, string? obfuscatedAccountId);

        /// <summary><c>consumeAsync</c> → <see cref="IBillingBridgeListener.OnConsumed"/>.</summary>
        void Consume(string token);

        /// <summary><c>acknowledgePurchase</c> → <see cref="IBillingBridgeListener.OnAcknowledged"/>.</summary>
        void Acknowledge(string token);
    }

    /// <summary>
    /// Los avisos del plugin Java (<c>com.amazonfill.billing.AfBillingListener</c>), ya en el hilo principal de Unity. Los códigos son los
    /// de <c>BillingClient.BillingResponseCode</c> (<see cref="BillingCodes"/>); las filas, las de <see cref="BillingRows"/>.
    /// </summary>
    public interface IBillingBridgeListener
    {
        void OnSetupFinished(int code);

        void OnDisconnected();

        void OnProducts(int requestId, int code, string? rows);

        void OnPurchases(int requestId, int code, string? rows);

        void OnPurchasesUpdated(int code, string? rows);

        void OnLaunchFailed(string? sku, int code);

        void OnConsumed(string? token, int code);

        void OnAcknowledged(string? token, int code);
    }

    /// <summary>Códigos de <c>BillingClient.BillingResponseCode</c> de Play Billing Library 9.0.0 (leídos con <c>javap</c> del AAR).</summary>
    public static class BillingCodes
    {
        public const int ServiceTimeout = -3;
        public const int FeatureNotSupported = -2;
        public const int ServiceDisconnected = -1;
        public const int Ok = 0;
        public const int UserCanceled = 1;
        public const int ServiceUnavailable = 2;
        public const int BillingUnavailable = 3;
        public const int ItemUnavailable = 4;
        public const int DeveloperError = 5;
        public const int Error = 6;
        public const int ItemAlreadyOwned = 7;
        public const int ItemNotOwned = 8;
        public const int NetworkError = 12;

        /// <summary>Por qué falló la compra abierta, según el código de Play.</summary>
        public static PlayFailureKind FailureOf(int code)
        {
            switch (code)
            {
                case UserCanceled:
                    return PlayFailureKind.Cancelled;
                case ItemAlreadyOwned:
                    return PlayFailureKind.AlreadyOwned;
                case ServiceTimeout:
                case FeatureNotSupported:
                case ServiceDisconnected:
                case ServiceUnavailable:
                case BillingUnavailable:
                case ItemUnavailable:
                case NetworkError:
                    return PlayFailureKind.Unavailable;
                default:
                    return PlayFailureKind.Failed;
            }
        }
    }
}
