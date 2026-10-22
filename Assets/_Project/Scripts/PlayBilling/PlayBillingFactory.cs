#nullable enable

namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// Crea la tienda real de Play (<see cref="PlayBillingClient"/> sobre <see cref="NativePlayStorePort"/> y el plugin propio
    /// <c>AfBilling.androidlib</c>, Play Billing Library 9.0.0; D-040, añadido de Q-IAP-1) solo en un teléfono Android
    /// (<c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>). En el editor y en los tests devuelve null y el juego sigue con
    /// <see cref="FakeBillingClient"/> (08 §2.1, principio 6). Si el plugin falla al crearse (sin la clase Java, sin Play Store…), la
    /// tienda queda sin Play (<see cref="UnavailablePlayStorePort"/>): el juego arranca y no vende, en lugar de caerse o de usar la simulada.
    /// </summary>
    public static class PlayBillingFactory
    {
        /// <summary>¿Hay tienda real en este build?</summary>
        public static bool Available
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>La tienda real sin arrancar (<see cref="PlayBillingClient.Start"/>), o null si este build no la tiene.</summary>
        /// <param name="catalog">Catálogo de 1.0.</param>
        /// <param name="fallbackCurrency">Moneda de los precios de 05 §4.2 mientras Play no dé los suyos.</param>
        public static PlayBillingClient? TryCreate(StoreCatalog catalog, string fallbackCurrency)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return PlayBillingClient.Create(() => new NativePlayStorePort(listener => new AndroidBillingBridge(listener)), catalog, fallbackCurrency);
#else
            return null;
#endif
        }
    }
}
