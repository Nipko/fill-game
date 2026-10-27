#nullable enable
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Scripting;

namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// <see cref="IBillingBridge"/> real: llama por JNI al plugin Java <c>com.amazonfill.billing.AfBilling</c> (<c>AfBilling.androidlib</c>)
    /// y recibe sus avisos con <see cref="BillingListenerProxy"/>. Solo en el teléfono (<c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>).
    /// No escribe nada en el log.
    /// </summary>
    internal sealed class AndroidBillingBridge : IBillingBridge
    {
        private const string PluginClass = "com.amazonfill.billing.AfBilling";

        private readonly AndroidJavaObject _plugin;
        private bool _disposed;

        public AndroidBillingBridge(IBillingBridgeListener listener)
        {
            var proxy = new BillingListenerProxy(listener, AndroidApplication.InvokeOnUnityMainThread);
            _plugin = new AndroidJavaObject(PluginClass, AndroidApplication.currentContext, proxy);
        }

        public void Connect() => _plugin.Call("connect");

        public void QueryProducts(int requestId, string skus) => _plugin.Call("queryProducts", requestId, skus);

        public void QueryPurchases(int requestId) => _plugin.Call("queryPurchases", requestId);

        public void Launch(string sku, string? obfuscatedAccountId)
        {
            // La actividad de ahora (Unity puede recrearla); la de Unity no se libera aquí. Sin UID va "" (JNI no deduce el tipo de un
            // null); Java no llama a setObfuscatedAccountId con una cadena vacía.
            _plugin.Call("launch", AndroidApplication.currentActivity, sku, obfuscatedAccountId ?? string.Empty);
        }

        public void Consume(string token) => _plugin.Call("consume", token);

        public void Acknowledge(string token) => _plugin.Call("acknowledge", token);

        public void QueryBillingConfig(int requestId) => _plugin.Call("queryBillingConfig", requestId);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _plugin.Call("release");
            }
            finally
            {
                _plugin.Dispose();
            }
        }
    }

    /// <summary>
    /// El <c>AfBillingListener</c> de Java en C#: cada aviso pasa por <see cref="BillingCallbacks.TryDispatch"/> al hilo principal de Unity.
    /// Los métodos con el nombre de Java cubren la búsqueda por reflexión de <see cref="AndroidJavaProxy"/> si no pasara por
    /// <see cref="Invoke(string, object[])"/> (con <see cref="PreserveAttribute"/> para el stripping High).
    /// </summary>
    internal sealed class BillingListenerProxy : AndroidJavaProxy
    {
        private readonly IBillingBridgeListener _target;
        private readonly Action<Action> _post;

        public BillingListenerProxy(IBillingBridgeListener target, Action<Action> post)
            : base(BillingCallbacks.JavaInterface)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _post = post ?? throw new ArgumentNullException(nameof(post));
        }

        public override AndroidJavaObject? Invoke(string methodName, object[] args)
        {
            return BillingCallbacks.TryDispatch(_target, _post, methodName, args) ? null : base.Invoke(methodName, args);
        }

        [Preserve]
        public void onSetupFinished(int code) => Dispatch("onSetupFinished", code);

        [Preserve]
        public void onDisconnected() => Dispatch("onDisconnected");

        [Preserve]
        public void onProducts(int requestId, int code, string? rows) => Dispatch("onProducts", requestId, code, rows);

        [Preserve]
        public void onPurchases(int requestId, int code, string? rows) => Dispatch("onPurchases", requestId, code, rows);

        [Preserve]
        public void onPurchasesUpdated(int code, string? rows) => Dispatch("onPurchasesUpdated", code, rows);

        [Preserve]
        public void onLaunchFailed(string? sku, int code) => Dispatch("onLaunchFailed", sku, code);

        [Preserve]
        public void onConsumed(string? token, int code) => Dispatch("onConsumed", token, code);

        [Preserve]
        public void onAcknowledged(string? token, int code) => Dispatch("onAcknowledged", token, code);

        [Preserve]
        public void onBillingConfig(int requestId, int code, string? countryCode) => Dispatch("onBillingConfig", requestId, code, countryCode);

        private void Dispatch(string methodName, params object?[] args)
        {
            BillingCallbacks.TryDispatch(_target, _post, methodName, args);
        }
    }
}
#endif
