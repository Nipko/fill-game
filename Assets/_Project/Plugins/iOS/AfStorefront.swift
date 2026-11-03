// País de la cuenta del App Store (fase M2 de docs/dev/traspaso/mac-ios.md, D-067; D-057: en iOS ocupa el sitio del país de la
// cuenta de Google Play). StoreKit 2 (Storefront.current, iOS 15+, el mínimo del juego), de la plataforma: sin permisos ni SDK.
// Storefront.current es asíncrono y StoreKit 1 (SKPaymentQueue.storefront) da nil al arrancar sin un observador de transacciones,
// así que el país se guarda en las preferencias de la app (las mismas de PlayerPrefs de Unity en iOS) y el juego lo lee en el
// arranque siguiente, como el de Play en Android. Lo llama AF.UI.IosStorefrontCountry por [DllImport("__Internal")].

import Foundation
import StoreKit

/// Clave de las preferencias (PlayerPrefs) con el país de la tienda en ISO 3166-1 alfa-3 («COL»).
private let afStorefrontKey = "af_storefront_alpha3"

/// Pide el país de la tienda y, cuando llega, lo guarda en `af_storefront_alpha3`. Sin cuenta del App Store, no guarda nada.
@_cdecl("AfStorefrontRequest")
public func AfStorefrontRequest() {
    Task {
        guard let code = await Storefront.current?.countryCode, !code.isEmpty else {
            return
        }

        UserDefaults.standard.set(code, forKey: afStorefrontKey)
    }
}
