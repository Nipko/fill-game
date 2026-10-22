#nullable enable
using System;

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
    public sealed class BillingReadyStoreService : IStoreService
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

        /// <inheritdoc />
        public bool IsAvailable => _billing.IsReady && _inner.IsAvailable;

        /// <inheritdoc />
        public bool IsPurchasing => _inner.IsPurchasing;

        /// <inheritdoc />
        public StoreCatalog Catalog => _inner.Catalog;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku) => _inner.PriceOf(sku);

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
