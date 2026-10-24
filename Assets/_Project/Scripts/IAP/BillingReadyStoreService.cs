#nullable enable
using System;
using System.Collections.Generic;

namespace AF.IAP
{
    /// <summary>
    /// La tienda solo vende cuando la tienda de la plataforma está lista (<see cref="IBillingAvailability"/>: Play conectada, con sus
    /// precios y con las compras sin terminar ya consultadas; D-040). Mientras tanto <see cref="IsAvailable"/> es false (el Mercado dice
    /// «tienda no disponible» y Aguas estancadas no ofrece Puñado ni Balsa) y toda compra da
    /// <see cref="PurchaseStatus.StoreUnavailable"/> sin abrir nada; y un SKU sin el precio de la tienda (no publicado en Play Console o con
    /// una moneda que no se escribe) tampoco se vende (<see cref="IBillingAvailability.CanSell"/>), para no cobrar con un precio de
    /// respaldo (05 §4.1: «precio local con impuestos visible antes de comprar»). No cambia el contrato de la tienda envuelta (<see cref="VerifiedStoreService"/>): la cola de
    /// verificación sigue con su <see cref="VerifiedStoreService.Resume"/> y su <see cref="VerifiedStoreService.Pump"/>. Hilo principal.
    /// </summary>
    public sealed class BillingReadyStoreService : IStoreService, IStoreDecorator
    {
        private readonly IStoreService _inner;
        private readonly IBillingAvailability _billing;

        public BillingReadyStoreService(IStoreService inner, IBillingAvailability billing)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        }

        /// <summary>La tienda envuelta.</summary>
        public IStoreService Inner => _inner;

        /// <summary>La tienda de la plataforma (lista o no, y con qué precios de la tienda).</summary>
        public IBillingAvailability Billing => _billing;

        /// <summary>
        /// Play lista, la tienda envuelta encendida y al menos un SKU del catálogo con el precio de la tienda. Con Play conectada pero sin
        /// ningún producto con precio (la app aún no está en Play Console, o ninguna moneda se puede escribir), la tienda tampoco está
        /// disponible: el Mercado, sus accesos y las ofertas se comportan como cuando Play no conecta, en lugar de enseñar los precios de
        /// respaldo de 05 §4.2 que nadie puede comprar (05 §4.1: «precio local con impuestos visible antes de comprar»; prueba del
        /// 2026-10-23, observación 1). Si los precios llegan después, vuelve a estar disponible en la misma sesión. Sin asignaciones.
        /// </summary>
        public bool IsAvailable => _billing.IsReady && _inner.IsAvailable && AnySellable();

        /// <inheritdoc />
        public bool IsPurchasing => _inner.IsPurchasing;

        /// <inheritdoc />
        public StoreCatalog Catalog => _inner.Catalog;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku) => _inner.PriceOf(sku);

        // ¿Algún SKU del catálogo con el precio de la tienda? Recorre la lista por índice: sin asignaciones por fotograma.
        private bool AnySellable()
        {
            IReadOnlyList<StoreProduct> products = _inner.Catalog.Products;
            for (int i = 0; i < products.Count; i++)
            {
                if (_billing.CanSell(products[i].Sku))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void Purchase(PurchaseRequest request, Action<PurchaseResult> onDone)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            if (!_billing.CanSell(request.Sku))
            {
                StoreProduct product = Catalog.Get(request.Sku);
                onDone(new PurchaseResult(request, product, _inner.IsPurchasing ? PurchaseStatus.Busy : PurchaseStatus.StoreUnavailable));
                return;
            }

            _inner.Purchase(request, onDone);
        }
    }
}
