#nullable enable
using System;
using AF.Economy;

namespace AF.IAP
{
    /// <summary>Dónde se ofreció la compra (<c>context</c> de <c>af_iap_offer_shown</c> y <c>af_iap_result</c>, 08 §8.2).</summary>
    public enum PurchaseContext
    {
        /// <summary><c>shop</c>: Mercado.</summary>
        Shop = 0,

        /// <summary><c>stuck</c>: Aguas estancadas sin saldo (Puñado y Balsa).</summary>
        Stuck = 1,

        /// <summary><c>no_canoes</c>: Sin canoas.</summary>
        NoCanoes = 2,

        /// <summary><c>starter_window</c>: tarjeta de la Mochila de inicio.</summary>
        StarterWindow = 3,

        /// <summary><c>noads_prompt</c>: tarjeta de Sin anuncios.</summary>
        NoAdsPrompt = 4,

        /// <summary><c>piggy</c>: Cántaro.</summary>
        Piggy = 5,
    }

    /// <summary>Cómo terminó una compra.</summary>
    public enum PurchaseStatus
    {
        /// <summary>Comprada y acreditada (<c>ok</c>).</summary>
        Purchased = 0,

        /// <summary>El jugador canceló (<c>cancel</c>): no cambia nada.</summary>
        Cancelled = 1,

        /// <summary>Falló el pago o la acreditación (<c>failed</c>): no cambia nada.</summary>
        Failed = 2,

        /// <summary>Ya lo tiene (Sin anuncios, Mochila de inicio o una skin).</summary>
        AlreadyOwned = 3,

        /// <summary>No se vende en este momento (Balsa antes del 31, Cántaro fuera de 1.000-3.000…).</summary>
        NotAvailable = 4,

        /// <summary>Tienda apagada (<c>biz_iap_enabled</c> = false).</summary>
        StoreUnavailable = 5,

        /// <summary>Ya hay una compra en curso.</summary>
        Busy = 6,
    }

    /// <summary>Petición de compra.</summary>
    public sealed class PurchaseRequest
    {
        /// <summary>Crea la petición.</summary>
        /// <param name="sku">SKU del catálogo de 1.0.</param>
        /// <param name="context">Dónde se ofreció.</param>
        /// <param name="level">Nivel en juego (la Balsa de rescate solo se vende desde <c>rescue_min_level</c>); 0 fuera de un nivel.</param>
        /// <param name="piggySeeds">Semillas del Cántaro (solo <c>af_piggy</c>: se acreditan exactamente esas).</param>
        public PurchaseRequest(string sku, PurchaseContext context = PurchaseContext.Shop, int level = 0, int piggySeeds = 0)
        {
            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            if (level < 0 || piggySeeds < 0)
            {
                throw new ArgumentOutOfRangeException(level < 0 ? nameof(level) : nameof(piggySeeds));
            }

            Sku = sku;
            Context = context;
            Level = level;
            PiggySeeds = piggySeeds;
        }

        /// <summary>SKU.</summary>
        public string Sku { get; }

        /// <summary>Dónde se ofreció.</summary>
        public PurchaseContext Context { get; }

        /// <summary>Nivel en juego (0 fuera de un nivel).</summary>
        public int Level { get; }

        /// <summary>Semillas del Cántaro.</summary>
        public int PiggySeeds { get; }
    }

    /// <summary>Resultado de una compra.</summary>
    public sealed class PurchaseResult
    {
        internal PurchaseResult(PurchaseRequest request, StoreProduct product, PurchaseStatus status, TransactionResult? transaction = null)
        {
            Request = request;
            Product = product;
            Status = status;
            Transaction = transaction;
        }

        /// <summary>La petición.</summary>
        public PurchaseRequest Request { get; }

        /// <summary>El producto.</summary>
        public StoreProduct Product { get; }

        /// <summary>Cómo terminó.</summary>
        public PurchaseStatus Status { get; }

        /// <summary>La acreditación en la cartera (con <see cref="PurchaseStatus.Purchased"/>, o la rechazada con <see cref="PurchaseStatus.Failed"/>).</summary>
        public TransactionResult? Transaction { get; }

        /// <summary>Se compró.</summary>
        public bool IsPurchased => Status == PurchaseStatus.Purchased;

        /// <summary>Se compró la Balsa de rescate: quien llama añade ya la Cuia de rescate al tablero (04 §6.2 <c>BALSA_499</c>).</summary>
        public bool AppliesRescue => IsPurchased && Product.Content.AppliesRescue;
    }

    /// <summary>
    /// Tienda (GDD 08 §2.1 principio 6 y §2.2 <c>AF.IAP</c>). En la fase 2 (D-008) solo existe
    /// <see cref="FakeStoreService"/>, sin Play Billing ni red; Unity IAP 5.4.4 entrará después detrás de esta misma
    /// interfaz, con precios desde la tienda, verificación en Cloud Function antes de acreditar (08 §10.3) y restauración.
    /// Lo comprado se acredita siempre con <see cref="StoreLedger.Fulfill"/> (<c>Wallet.Apply</c>). Hilo principal.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>La tienda está encendida (<c>biz_iap_enabled</c>).</summary>
        bool IsAvailable { get; }

        /// <summary>Hay una compra en curso.</summary>
        bool IsPurchasing { get; }

        /// <summary>Catálogo.</summary>
        StoreCatalog Catalog { get; }

        /// <summary>
        /// Precio que se muestra para <paramref name="sku"/> (moneda local): importe y moneda, que se escriben con la función única
        /// de precios (<see cref="LocalPrice.ToString"/>, <c>Loc.Price</c>; D-025). El adaptador de Unity IAP lo compondrá desde
        /// <c>priceAmountMicros</c> y el código de moneda del producto (D-025, punto 3 [P]; <c>Loc.PriceFromMicros</c>), nunca
        /// desde la cadena formateada de Google Play.
        /// </summary>
        LocalPrice PriceOf(string sku);

        /// <summary>
        /// Compra y llama una sola vez a <paramref name="onDone"/> al terminar (puede ser antes de volver). Una compra
        /// cancelada o fallida nunca descuenta ni acredita nada (05 §4.1).
        /// </summary>
        void Purchase(PurchaseRequest request, Action<PurchaseResult> onDone);
    }
}
