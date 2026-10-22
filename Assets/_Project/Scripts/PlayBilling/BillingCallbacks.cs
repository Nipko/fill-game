#nullable enable
using System;

namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// Reparte un aviso de Java (<c>com.amazonfill.billing.AfBillingListener</c>, por <c>AndroidJavaProxy.Invoke</c>) a un
    /// <see cref="IBillingBridgeListener"/>, pasándolo por <c>post</c> al hilo principal de Unity. Sin JNI, para poder probarlo en el editor
    /// con los argumentos tal como los desempaqueta Unity (<c>Integer</c> → <see cref="int"/>, <c>String</c> → <see cref="string"/> o null).
    /// Un nombre o unos argumentos que no encajan devuelven false (el proxy los pasa a la implementación de Unity: <c>equals</c>,
    /// <c>hashCode</c>, <c>toString</c>).
    /// </summary>
    public static class BillingCallbacks
    {
        public const string JavaInterface = "com.amazonfill.billing.AfBillingListener";

        /// <summary>¿Era un aviso del plugin? Si lo era, ya está encolado en <paramref name="post"/>.</summary>
        public static bool TryDispatch(IBillingBridgeListener target, Action<Action> post, string methodName, object?[]? args)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (post == null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            object?[] a = args ?? Array.Empty<object?>();
            switch (methodName)
            {
                case "onSetupFinished":
                    if (a.Length == 1 && TryInt(a[0], out int setup))
                    {
                        post(() => target.OnSetupFinished(setup));
                        return true;
                    }

                    return false;
                case "onDisconnected":
                    if (a.Length == 0)
                    {
                        post(target.OnDisconnected);
                        return true;
                    }

                    return false;
                case "onProducts":
                    if (a.Length == 3 && TryInt(a[0], out int productsId) && TryInt(a[1], out int productsCode) && TryString(a[2], out string? productRows))
                    {
                        post(() => target.OnProducts(productsId, productsCode, productRows));
                        return true;
                    }

                    return false;
                case "onPurchases":
                    if (a.Length == 3 && TryInt(a[0], out int purchasesId) && TryInt(a[1], out int purchasesCode) && TryString(a[2], out string? purchaseRows))
                    {
                        post(() => target.OnPurchases(purchasesId, purchasesCode, purchaseRows));
                        return true;
                    }

                    return false;
                case "onPurchasesUpdated":
                    if (a.Length == 2 && TryInt(a[0], out int updatedCode) && TryString(a[1], out string? updatedRows))
                    {
                        post(() => target.OnPurchasesUpdated(updatedCode, updatedRows));
                        return true;
                    }

                    return false;
                case "onLaunchFailed":
                    if (a.Length == 2 && TryString(a[0], out string? sku) && TryInt(a[1], out int launchCode))
                    {
                        post(() => target.OnLaunchFailed(sku, launchCode));
                        return true;
                    }

                    return false;
                case "onConsumed":
                    if (a.Length == 2 && TryString(a[0], out string? consumed) && TryInt(a[1], out int consumeCode))
                    {
                        post(() => target.OnConsumed(consumed, consumeCode));
                        return true;
                    }

                    return false;
                case "onAcknowledged":
                    if (a.Length == 2 && TryString(a[0], out string? acknowledged) && TryInt(a[1], out int ackCode))
                    {
                        post(() => target.OnAcknowledged(acknowledged, ackCode));
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool TryInt(object? value, out int result)
        {
            if (value is int i)
            {
                result = i;
                return true;
            }

            result = 0;
            return false;
        }

        private static bool TryString(object? value, out string? result)
        {
            if (value == null || value is string)
            {
                result = (string?)value;
                return true;
            }

            result = null;
            return false;
        }
    }
}
