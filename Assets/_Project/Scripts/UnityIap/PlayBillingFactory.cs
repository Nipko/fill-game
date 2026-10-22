#nullable enable

namespace AF.IAP.UnityIap
{
    /// <summary>
    /// Crea la tienda real de Play (<see cref="PlayBillingClient"/> sobre <see cref="UnityIapStorePort"/>) solo donde existe: con el
    /// paquete <c>com.unity.purchasing</c> 5.4.4 (<c>AF_UNITY_IAP</c>, el <c>versionDefines</c> de <c>AF.IAP.UnityIap.asmdef</c>; D-040) y
    /// en un teléfono Android (<c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>). En el editor, en los tests y en un build sin el paquete
    /// devuelve null y el juego sigue con <see cref="FakeBillingClient"/> (08 §2.1, principio 6). Si Unity IAP falla al crearse, la tienda
    /// queda sin Play (<see cref="UnavailablePlayStorePort"/>): el juego arranca y no vende, en lugar de caerse o de usar la simulada.
    /// </summary>
    public static class PlayBillingFactory
    {
        /// <summary>¿Hay tienda real en este build?</summary>
        public static bool Available
        {
            get
            {
#if AF_UNITY_IAP && UNITY_ANDROID && !UNITY_EDITOR
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
#if AF_UNITY_IAP && UNITY_ANDROID && !UNITY_EDITOR
            return PlayBillingClient.Create(() => new UnityIapStorePort(), catalog, fallbackCurrency);
#else
            return null;
#endif
        }
    }
}
