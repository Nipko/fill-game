#nullable enable
using System;

namespace AF.IAP
{
    /// <summary>Qué hace <see cref="FakeStoreService"/> al confirmar una compra (para pruebas y builds de desarrollo).</summary>
    public enum FakeStoreMode
    {
        /// <summary>La compra sale bien y se acredita.</summary>
        Normal = 0,

        /// <summary>El pago falla tras confirmar: no se acredita nada.</summary>
        Fail = 1,
    }

    /// <summary>Lo que la UI enseña en la hoja «Compra simulada».</summary>
    public sealed class SimulatedPurchase
    {
        /// <summary>Rótulo de la hoja.</summary>
        public const string Label = "Compra simulada";

        internal SimulatedPurchase(StoreProduct product, LocalPrice price)
        {
            Product = product;
            Price = price;
        }

        /// <summary>Producto.</summary>
        public StoreProduct Product { get; }

        /// <summary>SKU.</summary>
        public string Sku => Product.Sku;

        /// <summary>Nombre de tienda (05 §4.1).</summary>
        public string Name => Product.Name;

        /// <summary>Precio local (05 §4.2).</summary>
        public LocalPrice Price { get; }

        /// <summary>Precio como se muestra, p. ej. <c>COP 3.900</c>.</summary>
        public string PriceText => Price.ToString();
    }

    /// <summary>
    /// La hoja «Compra simulada» que pinta la UI (B2-4, <c>StoreSheet</c>): rótulo, producto y precio, con Comprar y
    /// Cancelar. Llama a <c>onClosed</c> una sola vez: true si el jugador confirma.
    /// </summary>
    public interface ISimulatedPurchasePresenter
    {
        /// <summary>Muestra <paramref name="purchase"/> y espera la respuesta.</summary>
        void Present(SimulatedPurchase purchase, Action<bool> onClosed);
    }

    /// <summary>Hoja sin pantalla para tests: responde al instante con <see cref="Confirm"/>.</summary>
    public sealed class ImmediatePurchasePresenter : ISimulatedPurchasePresenter
    {
        /// <summary>Crea la hoja.</summary>
        public ImmediatePurchasePresenter(bool confirm = true)
        {
            Confirm = confirm;
        }

        /// <summary>El jugador confirma (true) o cancela.</summary>
        public bool Confirm { get; set; }

        /// <summary>Hojas mostradas.</summary>
        public int Presented { get; private set; }

        /// <summary>La última, o null.</summary>
        public SimulatedPurchase? Last { get; private set; }

        /// <inheritdoc />
        public void Present(SimulatedPurchase purchase, Action<bool> onClosed)
        {
            Presented++;
            Last = purchase;
            onClosed(Confirm);
        }
    }

    /// <summary>
    /// Compras simuladas de la fase 2 (D-008, aprobado por el dueño el 2026-10-08): sin Play Billing, sin red y sin
    /// identificadores. <see cref="Purchase"/> pide a la UI la hoja «Compra simulada» con el nombre y el precio local;
    /// al confirmar se acredita con <see cref="StoreLedger.Fulfill"/> (<c>Wallet.Apply</c>); al cancelar no cambia nada,
    /// salvo anotar el rechazo si era la Balsa de rescate (04 §6.2). Una compra a la vez.
    /// </summary>
    public sealed class FakeStoreService : IStoreService
    {
        private readonly StoreLedger _ledger;
        private readonly ISimulatedPurchasePresenter _presenter;

        /// <summary>Crea la tienda simulada.</summary>
        /// <param name="ledger">Registro de la tienda sobre el perfil.</param>
        /// <param name="presenter">Hoja «Compra simulada».</param>
        /// <param name="currency">Moneda de los precios (<c>USD</c>, <c>BRL</c>, <c>MXN</c>, <c>COP</c> o <c>PEN</c>).</param>
        public FakeStoreService(StoreLedger ledger, ISimulatedPurchasePresenter presenter, string currency = "USD")
        {
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            StorePrices.Local(StorePrices.UsdTiers[0], currency); // valida la moneda
            Currency = currency;
        }

        /// <summary>Moneda de los precios.</summary>
        public string Currency { get; }

        /// <summary>Qué pasa al confirmar.</summary>
        public FakeStoreMode Mode { get; set; }

        /// <inheritdoc />
        public bool IsAvailable => _ledger.Config.BizIapEnabled;

        /// <inheritdoc />
        public bool IsPurchasing { get; private set; }

        /// <inheritdoc />
        public StoreCatalog Catalog => _ledger.Catalog;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku)
        {
            return Catalog.Get(sku).PriceIn(Currency);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">El SKU no está en el catálogo de 1.0.</exception>
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

            StoreProduct product = Catalog.Get(request.Sku);
            if (IsPurchasing)
            {
                onDone(new PurchaseResult(request, product, PurchaseStatus.Busy));
                return;
            }

            if (!IsAvailable)
            {
                onDone(new PurchaseResult(request, product, PurchaseStatus.StoreUnavailable));
                return;
            }

            PurchaseStatus? blocked = _ledger.CheckPurchasable(product, request);
            if (blocked.HasValue)
            {
                onDone(new PurchaseResult(request, product, blocked.Value));
                return;
            }

            bool closed = false;
            IsPurchasing = true;
            _presenter.Present(new SimulatedPurchase(product, product.PriceIn(Currency)), confirmed =>
            {
                if (closed)
                {
                    return;
                }

                closed = true;
                IsPurchasing = false;
                if (!confirmed)
                {
                    if (product.Content.AppliesRescue)
                    {
                        _ledger.RecordBalsaDeclined();
                    }

                    onDone(new PurchaseResult(request, product, PurchaseStatus.Cancelled));
                    return;
                }

                if (Mode == FakeStoreMode.Fail)
                {
                    onDone(new PurchaseResult(request, product, PurchaseStatus.Failed));
                    return;
                }

                onDone(_ledger.Fulfill(request));
            });
        }
    }
}
