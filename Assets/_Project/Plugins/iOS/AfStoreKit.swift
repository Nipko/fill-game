// Compras de iOS con StoreKit 2 (fase M3 de docs/dev/traspaso/mac-ios.md; I-3 de D-067: plugin propio, como el de Play de D-040).
// Habla con C# (AF.IAP.PlayBilling.IosBillingBridge) con el MISMO protocolo que el plugin Java AfBilling: enteros y filas de texto
// (una por línea, campos separados por tabulador; AF.IAP.PlayBilling.BillingRows), con los códigos de Play (BillingCodes). Así
// NativePlayStorePort, PlayBillingClient y la verificación en el servidor son los mismos en las dos plataformas.
//   - Token de una compra: el id de la transacción (estable: el hash del libro mayor no puede cambiar entre consultas).
//   - Consume = finish(). Acknowledge = finish() en los no consumibles; en los consumibles (el «deferred_ack» de Play) solo la marca,
//     para no perder una compra que el servidor aún no ha acreditado.
//   - Solo transacciones verificadas por StoreKit (las de un .storekit local también lo están).
// Sin permisos ni SDK: StoreKit es de la plataforma. Todo en el hilo principal (@MainActor), que en iOS es el de Unity.

import Foundation
import StoreKit

/// (tipo de aviso, id de petición, código, texto A, texto B). Tipos: 0 setup, 1 desconexión, 2 productos, 3 compras, 4 compras
/// actualizadas, 5 compra fallida (A = sku), 6 consumida (A = token), 7 reconocida (A = token), 8 país de la cuenta.
public typealias AfSkCallback = @convention(c) (Int32, Int32, Int32, UnsafePointer<CChar>?, UnsafePointer<CChar>?) -> Void

private enum Code {
    static let featureNotSupported: Int32 = -2
    static let ok: Int32 = 0
    static let userCanceled: Int32 = 1
    static let serviceUnavailable: Int32 = 2
    static let itemUnavailable: Int32 = 4
    static let error: Int32 = 6
    static let itemNotOwned: Int32 = 8
}

private enum Row {
    static let purchased = 1
    static let pending = 2
}

@MainActor
private final class AfStoreKit {
    static let shared = AfStoreKit()

    var callback: AfSkCallback?
    private var products: [String: Product] = [:]
    private var open: [String: Transaction] = [:]   // token → transacción sin terminar
    private var acknowledged = Set<String>()         // consumibles marcados sin terminar
    private var updates: Task<Void, Never>?

    func emit(_ kind: Int32, _ request: Int32, _ code: Int32, _ a: String?, _ b: String?) {
        guard let callback = callback else { return }
        if let a = a, let b = b {
            a.withCString { pa in b.withCString { pb in callback(kind, request, code, pa, pb) } }
        } else if let a = a {
            a.withCString { pa in callback(kind, request, code, pa, nil) }
        } else if let b = b {
            b.withCString { pb in callback(kind, request, code, nil, pb) }
        } else {
            callback(kind, request, code, nil, nil)
        }
    }

    func connect() {
        if updates == nil {
            // Lo que llega fuera de la hoja: Ask to Buy aprobado, compras de otro dispositivo, renovaciones.
            updates = Task { @MainActor in
                for await result in Transaction.updates {
                    if let row = self.remember(result) {
                        self.emit(4, 0, Code.ok, nil, row)
                    }
                }
            }
        }
        emit(0, 0, Code.ok, nil, nil)
    }

    func queryProducts(_ request: Int32, _ skus: String) {
        let ids = skus.split(separator: "\n").map(String.init).filter { !$0.isEmpty }
        Task { @MainActor in
            do {
                let found = try await Product.products(for: ids)
                var rows: [String] = []
                for p in found {
                    products[p.id] = p
                    let micros = NSDecimalNumber(decimal: p.price * 1_000_000).int64Value
                    rows.append([p.id, p.priceFormatStyle.currencyCode, String(micros)].joined(separator: "\t"))
                }
                emit(2, request, Code.ok, nil, rows.joined(separator: "\n"))
            } catch {
                emit(2, request, Code.serviceUnavailable, nil, nil)
            }
        }
    }

    func queryPurchases(_ request: Int32) {
        Task { @MainActor in
            var rows: [String] = []
            var seen = Set<String>()
            for await result in Transaction.unfinished {
                if let row = remember(result), seen.insert(token(of: result)).inserted { rows.append(row) }
            }
            // Los no consumibles ya terminados (Sin anuncios, skins): como los reconocidos de Play.
            for await result in Transaction.currentEntitlements {
                if case .verified(let t) = result, t.productType == .nonConsumable, t.revocationDate == nil,
                   seen.insert(String(t.id)).inserted {
                    rows.append(row(t, state: Row.purchased, acknowledged: true))
                }
            }
            emit(3, request, Code.ok, nil, rows.joined(separator: "\n"))
        }
    }

    func launch(_ sku: String) {
        Task { @MainActor in
            do {
                var product = products[sku]
                if product == nil { product = try await Product.products(for: [sku]).first }
                guard let product = product else {
                    emit(5, 0, Code.itemUnavailable, sku, nil)
                    return
                }
                switch try await product.purchase() {
                case .success(let result):
                    if let row = remember(result) {
                        emit(4, 0, Code.ok, nil, row)
                    } else {
                        emit(5, 0, Code.error, sku, nil) // StoreKit no la pudo verificar
                    }
                case .pending:
                    // Ask to Buy o un pago por confirmar: la transacción llega después por Transaction.updates.
                    emit(4, 0, Code.ok, nil, [sku, "pending." + sku, String(Row.pending), "0", "0"].joined(separator: "\t"))
                case .userCancelled:
                    emit(5, 0, Code.userCanceled, sku, nil)
                @unknown default:
                    emit(5, 0, Code.error, sku, nil)
                }
            } catch {
                emit(5, 0, Code.error, sku, nil)
            }
        }
    }

    func consume(_ token: String) {
        finish(token, kind: 6)
    }

    func acknowledge(_ token: String) {
        guard let t = open[token] else {
            emit(7, 0, Code.itemNotOwned, token, nil)
            return
        }

        if t.productType == .nonConsumable {
            finish(token, kind: 7)
        } else {
            acknowledged.insert(token) // consumible: se marca y sigue sin terminar hasta que se acredite (Consume)
            emit(7, 0, Code.ok, token, nil)
        }
    }

    // iOS no tiene la configuración de facturación de Play: el país de la tienda lo lee aparte AfStorefront.swift (M2). Responde «no
    // soportado» en otro paso, como el plugin Java, que siempre responde de forma asíncrona.
    func queryBillingConfig(_ request: Int32) {
        Task { @MainActor in emit(8, request, Code.featureNotSupported, nil, nil) }
    }

    func release() {
        updates?.cancel()
        updates = nil
        callback = nil
    }

    private func finish(_ token: String, kind: Int32) {
        guard let t = open[token] else {
            emit(kind, 0, Code.itemNotOwned, token, nil)
            return
        }

        Task { @MainActor in
            await t.finish()
            open[token] = nil
            acknowledged.remove(token)
            emit(kind, 0, Code.ok, token, nil)
        }
    }

    private func token(of result: VerificationResult<Transaction>) -> String {
        switch result {
        case .verified(let t), .unverified(let t, _):
            return String(t.id)
        }
    }

    // Guarda una transacción verificada y sin reembolso, y devuelve su fila; nil si StoreKit no la verificó o se reembolsó.
    private func remember(_ result: VerificationResult<Transaction>) -> String? {
        guard case .verified(let t) = result, t.revocationDate == nil else { return nil }
        let token = String(t.id)
        open[token] = t
        return row(t, state: Row.purchased, acknowledged: acknowledged.contains(token))
    }

    private func row(_ t: Transaction, state: Int, acknowledged: Bool) -> String {
        let ms = Int64(t.purchaseDate.timeIntervalSince1970 * 1000)
        return [t.productID, String(t.id), String(state), String(ms), acknowledged ? "1" : "0"].joined(separator: "\t")
    }
}

@_cdecl("AfSkInit")
public func AfSkInit(_ callback: AfSkCallback) {
    MainActor.assumeIsolated { AfStoreKit.shared.callback = callback }
}

@_cdecl("AfSkConnect")
public func AfSkConnect() {
    MainActor.assumeIsolated { AfStoreKit.shared.connect() }
}

@_cdecl("AfSkQueryProducts")
public func AfSkQueryProducts(_ request: Int32, _ skus: UnsafePointer<CChar>) {
    let list = String(cString: skus)
    MainActor.assumeIsolated { AfStoreKit.shared.queryProducts(request, list) }
}

@_cdecl("AfSkQueryPurchases")
public func AfSkQueryPurchases(_ request: Int32) {
    MainActor.assumeIsolated { AfStoreKit.shared.queryPurchases(request) }
}

@_cdecl("AfSkLaunch")
public func AfSkLaunch(_ sku: UnsafePointer<CChar>) {
    let id = String(cString: sku)
    MainActor.assumeIsolated { AfStoreKit.shared.launch(id) }
}

@_cdecl("AfSkConsume")
public func AfSkConsume(_ token: UnsafePointer<CChar>) {
    let t = String(cString: token)
    MainActor.assumeIsolated { AfStoreKit.shared.consume(t) }
}

@_cdecl("AfSkAcknowledge")
public func AfSkAcknowledge(_ token: UnsafePointer<CChar>) {
    let t = String(cString: token)
    MainActor.assumeIsolated { AfStoreKit.shared.acknowledge(t) }
}

@_cdecl("AfSkQueryBillingConfig")
public func AfSkQueryBillingConfig(_ request: Int32) {
    MainActor.assumeIsolated { AfStoreKit.shared.queryBillingConfig(request) }
}

@_cdecl("AfSkRelease")
public func AfSkRelease() {
    MainActor.assumeIsolated { AfStoreKit.shared.release() }
}
