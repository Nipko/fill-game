#nullable enable
using System;
using AF.IAP;

namespace AF.UI
{
    /// <summary>
    /// Tienda que avisa cuando una compra acaba en <see cref="PurchaseStatus.StoreUnavailable"/> (la tienda de la plataforma no está
    /// lista o ese SKU no tiene el precio de la tienda; <see cref="BillingReadyStoreService"/>), para que el juego lo diga con un aviso
    /// breve («Las compras no están disponibles ahora») en lugar del silencio (prueba del 2026-10-23, observación 1). Vale para todas
    /// las compras del juego: el Mercado, las tarjetas de oferta y Aguas estancadas. Lo demás pasa tal cual a la tienda envuelta.
    /// </summary>
    public sealed class UnavailableNoticeStoreService : IStoreService, IStoreDecorator
    {
        private readonly IStoreService _inner;
        private readonly Action _unavailable;

        /// <summary>Envuelve <paramref name="inner"/>; <paramref name="unavailable"/> enseña el aviso.</summary>
        public UnavailableNoticeStoreService(IStoreService inner, Action unavailable)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _unavailable = unavailable ?? throw new ArgumentNullException(nameof(unavailable));
        }

        /// <inheritdoc />
        public IStoreService Inner => _inner;

        /// <inheritdoc />
        public bool IsAvailable => _inner.IsAvailable;

        /// <inheritdoc />
        public bool IsPurchasing => _inner.IsPurchasing;

        /// <inheritdoc />
        public StoreCatalog Catalog => _inner.Catalog;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku) => _inner.PriceOf(sku);

        /// <inheritdoc />
        public void Purchase(PurchaseRequest request, Action<PurchaseResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            _inner.Purchase(request, result =>
            {
                onDone(result); // primero quien compró (la sonda, la analítica, el repintado); luego el aviso, encima
                if (result.Status == PurchaseStatus.StoreUnavailable)
                {
                    _unavailable();
                }
            });
        }
    }
}
