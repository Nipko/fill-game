#nullable enable
using System;
using AF.Platform;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace AF.UI
{
    /// <summary>
    /// El país de la cuenta del App Store (fase M2 de <c>docs/dev/traspaso/mac-ios.md</c>; D-057): en iOS ocupa el sitio del país de la
    /// cuenta de Google Play en la cadena del país legal (SIM → tienda → región, <see cref="ConsentCountry"/>), que en el iPhone queda
    /// tienda → región porque iOS no da el país de la SIM. Lo pide StoreKit 2 (<c>Storefront.current</c>, asíncrono) por el plugin
    /// propio <c>Plugins/iOS/AfStorefront.swift</c>, que lo deja en alfa-3 en <c>PlayerPrefs</c> (<see cref="Alpha3Key"/>); se pasa a
    /// alfa-2 con <see cref="CountryCodes"/> y vale desde el arranque siguiente, como el de Play en Android. Fuera del iPhone no hace
    /// nada.
    /// </summary>
    public static class IosStorefrontCountry
    {
        /// <summary>Clave de <c>PlayerPrefs</c> donde el plugin deja el país de la tienda en alfa-3 (<c>COL</c>).</summary>
        public const string Alpha3Key = "af_storefront_alpha3";

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AfStorefrontRequest();
#endif

        /// <summary>
        /// Guarda en <paramref name="store"/> el país de la tienda que llegó en un arranque anterior (si llegó) y pide el de ahora para
        /// el siguiente.
        /// </summary>
        public static void SaveInto(IPlayAccountCountryStore store)
        {
            string alpha3 = PlayerPrefs.GetString(Alpha3Key, string.Empty);
            string? alpha2 = CountryCodes.Alpha2FromAlpha3(alpha3);
            if (Debug.isDebugBuild)
            {
                Debug.Log("[AF.IosStorefrontCountry] tienda=" + (alpha3.Length > 0 ? alpha3 : "(aún no)") + " -> " + (alpha2 ?? "null"));
            }

            if (alpha2 != null)
            {
                store.Save(alpha2);
            }

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                AfStorefrontRequest();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.IosStorefrontCountry] no se pudo pedir el país de la tienda: " + e.Message);
            }
#endif
        }
    }
}
