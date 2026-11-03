#nullable enable
#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;

namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// <see cref="IBillingBridge"/> de iOS (fase M3 de <c>docs/dev/traspaso/mac-ios.md</c>; I-3 de D-067): llama al plugin propio de
    /// StoreKit 2 <c>Plugins/iOS/AfStoreKit.swift</c>, que habla el mismo protocolo que el de Play (códigos de <see cref="BillingCodes"/> y
    /// filas de <see cref="BillingRows"/>), así que <see cref="NativePlayStorePort"/> y <see cref="PlayBillingClient"/> son los mismos que en
    /// Android. El token de una compra es el id de la transacción de StoreKit. iOS no tiene la configuración de facturación de Play: el
    /// país de la cuenta de la tienda lo lee aparte <c>AF.UI.IosStorefrontCountry</c> (M2), y aquí se responde «no soportado».
    /// Solo en el iPhone (<c>UNITY_IOS &amp;&amp; !UNITY_EDITOR</c>); los avisos llegan en el hilo principal (el de UIKit, que en iOS es el
    /// de Unity). No escribe nada en el log.
    /// </summary>
    internal sealed class IosBillingBridge : IBillingBridge
    {
        private delegate void Callback(int kind, int requestId, int code, IntPtr a, IntPtr b);

        [DllImport("__Internal")] private static extern void AfSkInit(Callback callback);
        [DllImport("__Internal")] private static extern void AfSkConnect();
        [DllImport("__Internal")] private static extern void AfSkQueryProducts(int requestId, string skus);
        [DllImport("__Internal")] private static extern void AfSkQueryPurchases(int requestId);
        [DllImport("__Internal")] private static extern void AfSkLaunch(string sku);
        [DllImport("__Internal")] private static extern void AfSkConsume(string token);
        [DllImport("__Internal")] private static extern void AfSkAcknowledge(string token);
        [DllImport("__Internal")] private static extern void AfSkQueryBillingConfig(int requestId);
        [DllImport("__Internal")] private static extern void AfSkRelease();

        // El plugin es uno por proceso: sus avisos van al puente vivo (una recarga de la escena suelta el anterior con Dispose).
        private static IosBillingBridge? s_current;
        private static readonly Callback s_callback = OnNative;

        private readonly IBillingBridgeListener _listener;
        private bool _disposed;

        public IosBillingBridge(IBillingBridgeListener listener)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            s_current = this;
            AfSkInit(s_callback);
        }

        public void Connect() => AfSkConnect();

        public void QueryProducts(int requestId, string skus) => AfSkQueryProducts(requestId, skus);

        public void QueryPurchases(int requestId) => AfSkQueryPurchases(requestId);

        // StoreKit pide un UUID para appAccountToken; el UID de Firebase no lo es. El servidor ata la compra al UID del token de
        // Firebase de la llamada a verifyPurchase (nunca al cuerpo), así que aquí no hace falta [P].
        public void Launch(string sku, string? obfuscatedAccountId) => AfSkLaunch(sku);

        public void Consume(string token) => AfSkConsume(token);

        public void Acknowledge(string token) => AfSkAcknowledge(token);

        public void QueryBillingConfig(int requestId) => AfSkQueryBillingConfig(requestId); // responde «no soportado», en otro paso

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (s_current == this)
            {
                s_current = null;
                AfSkRelease();
            }
        }

        [MonoPInvokeCallback(typeof(Callback))]
        private static void OnNative(int kind, int requestId, int code, IntPtr a, IntPtr b)
        {
            IosBillingBridge? bridge = s_current;
            if (bridge == null || bridge._disposed)
            {
                return;
            }

            string? textA = a == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(a);
            string? textB = b == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(b);
            StoreKitBridgeEvents.Dispatch(bridge._listener, kind, requestId, code, textA, textB);
        }
    }
}
#endif
