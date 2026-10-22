#nullable enable
using System;
using System.Collections.Generic;

namespace AF.IAP
{
    /// <summary>Cómo registra Play un producto: lo decide <see cref="ProductKind"/> (05 §4.1; <see cref="PlayBillingClient.TypeOf"/>).</summary>
    public enum PlayProductType
    {
        /// <summary>Consumible: <c>ConfirmPurchase</c> lo consume (monedas, Balsa, Cántaro, Canoas y también la Mochila, 1 por cuenta).</summary>
        Consumable = 0,

        /// <summary>No consumible: <c>ConfirmPurchase</c> solo lo reconoce (Sin anuncios y skins).</summary>
        NonConsumable = 1,
    }

    /// <summary>Un producto que se pide a la tienda (<c>ProductDefinition</c> de Unity IAP).</summary>
    public sealed class PlayProductDefinition
    {
        public PlayProductDefinition(string sku, PlayProductType type)
        {
            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            Sku = sku;
            Type = type;
        }

        public string Sku { get; }

        public PlayProductType Type { get; }
    }

    /// <summary>
    /// Lo que la tienda dice de un producto: el importe y la moneda locales (D-025, punto 3 [P]: desde el importe y el código de
    /// moneda, nunca desde la cadena ya formateada de Play).
    /// </summary>
    public sealed class PlayProductInfo
    {
        /// <param name="sku">SKU <c>af_…</c>.</param>
        /// <param name="currency">Código ISO 4217 de la moneda de la tienda (<c>price_currency_code</c>).</param>
        /// <param name="priceMicros">Importe en micros (<c>price_amount_micros</c>; 1 unidad = 1.000.000).</param>
        public PlayProductInfo(string sku, string currency, long priceMicros)
        {
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            PriceMicros = priceMicros;
        }

        public string Sku { get; }

        public string Currency { get; }

        public long PriceMicros { get; }
    }

    /// <summary>Estado de un pedido de Play.</summary>
    public enum PlayOrderState
    {
        /// <summary>Cobrado y sin terminar (<c>PendingOrder</c> de Unity IAP; <c>PURCHASED</c> sin consumir ni reconocer en el cliente).</summary>
        Purchased = 0,

        /// <summary>Pendiente de pago (<c>DeferredOrder</c>; <c>PENDING</c> de Play: efectivo, transferencia…). No se verifica ni se acredita.</summary>
        PaymentPending = 1,

        /// <summary>No consumible ya reconocido (<c>ConfirmedOrder</c>): lo que Play devuelve al consultar las compras (restauración).</summary>
        Confirmed = 2,
    }

    /// <summary>Un pedido de Play. El <see cref="Token"/> es el <c>purchaseToken</c>: va al servidor y al perfil (hash), nunca a los logs.</summary>
    public sealed class PlayOrder
    {
        public PlayOrder(string sku, string token, PlayOrderState state)
        {
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
            Token = token ?? throw new ArgumentNullException(nameof(token));
            State = state;
        }

        public string Sku { get; }

        /// <summary><c>purchaseToken</c>. No va a los logs.</summary>
        public string Token { get; }

        public PlayOrderState State { get; }

        /// <summary>Sin el token.</summary>
        public override string ToString() => "PlayOrder(" + Sku + ", " + State + ")";
    }

    /// <summary>Por qué falló una compra en Play.</summary>
    public enum PlayFailureKind
    {
        /// <summary>El jugador cerró la hoja de Play (<c>UserCancelled</c>).</summary>
        Cancelled = 0,

        /// <summary>
        /// Ya lo tiene sin terminar (<c>ITEM_ALREADY_OWNED</c>, <c>DuplicateTransaction</c> o <c>ExistingPurchasePending</c>): no se cobró
        /// nada nuevo; la compra anterior vuelve con la consulta de compras y la termina la cola (08 §10.3).
        /// </summary>
        AlreadyOwned = 1,

        /// <summary>Tienda desconectada o producto no disponible.</summary>
        Unavailable = 2,

        /// <summary>Cualquier otro fallo del pago.</summary>
        Failed = 3,
    }

    /// <summary>
    /// El puerto de la tienda de Play que usa <see cref="PlayBillingClient"/>: lo mínimo de Unity IAP 5.4.4 (<c>StoreController</c>) para
    /// cobrar, consultar y terminar compras, sin tipos del SDK. La implementación real es <c>AF.IAP.UnityIap.UnityIapStorePort</c>
    /// (solo con el paquete <c>com.unity.purchasing</c>; D-040); los tests usan un puerto falso. No acredita nada. Hilo principal: cada
    /// <c>onDone</c> se llama una vez (puede ser antes de volver) y los eventos llegan en el hilo principal. <see cref="IDisposable.Dispose"/>
    /// suelta los eventos del SDK (al recargar la escena se crea otro puerto: el anterior no debe seguir oyendo).
    /// </summary>
    public interface IPlayStorePort : IDisposable
    {
        /// <summary>Conecta con Play (<c>StoreController.Connect</c>); true si quedó conectada.</summary>
        void Connect(Action<bool> onDone);

        /// <summary>Pide los productos (<c>FetchProducts</c>); null si falló la consulta. Puede faltar alguno (no publicado en Play Console).</summary>
        void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone);

        /// <summary>
        /// Consulta las compras de la cuenta (<c>FetchPurchases</c>): cobradas sin terminar, pendientes de pago y no consumibles
        /// reconocidos. Null si falló la consulta.
        /// </summary>
        void FetchPurchases(Action<IReadOnlyList<PlayOrder>?> onDone);

        /// <summary>
        /// Abre la hoja de Play para <paramref name="sku"/> con <c>setObfuscatedAccountId</c> = <paramref name="obfuscatedAccountId"/>
        /// (si no es null). La respuesta llega por <see cref="OrderUpdated"/> o <see cref="PurchaseFailed"/>.
        /// </summary>
        void Purchase(string sku, string? obfuscatedAccountId);

        /// <summary>
        /// <c>ConfirmPurchase</c> del pedido cobrado de <paramref name="token"/>: Unity IAP consume los consumibles y reconoce los no
        /// consumibles según el tipo con que se registró el producto. False si el puerto no tiene ese pedido sin terminar. True solo dice
        /// que se pidió: si Play falla al terminarlo, el pedido sigue cobrado y vuelve en la próxima consulta.
        /// </summary>
        bool Confirm(string token);

        /// <summary>
        /// Aviso en vivo de Play (no de una consulta): lo cobrado de la compra abierta o de una pendiente de pago que Play cobró con la app
        /// abierta, y lo pendiente de pago de la compra abierta. Puede repetirse con el mismo token. Lo de <see cref="FetchPurchases"/>
        /// llega solo por su <c>onDone</c>.
        /// </summary>
        event Action<PlayOrder>? OrderUpdated;

        /// <summary>Falló la compra abierta (SKU, o null si Play no lo dice, y el motivo).</summary>
        event Action<string?, PlayFailureKind>? PurchaseFailed;

        /// <summary>Se perdió la conexión con Play (<c>OnStoreDisconnected</c>): las consultas en vuelo ya respondieron null.</summary>
        event Action? Disconnected;
    }

    /// <summary>
    /// Puerto sin Play: no conecta nunca. Lo usa <c>PlayBillingFactory</c> si Unity IAP falla al crearse en el teléfono, para que el juego
    /// arranque sin tienda (no con la simulada, que en un build real «vendería» compras que el servidor rechazaría).
    /// </summary>
    public sealed class UnavailablePlayStorePort : IPlayStorePort
    {
#pragma warning disable CS0067 // nunca hay pedidos, fallos ni desconexiones que avisar
        /// <inheritdoc />
        public event Action<PlayOrder>? OrderUpdated;

        /// <inheritdoc />
        public event Action<string?, PlayFailureKind>? PurchaseFailed;

        /// <inheritdoc />
        public event Action? Disconnected;
#pragma warning restore CS0067

        /// <inheritdoc />
        public void Connect(Action<bool> onDone) => (onDone ?? throw new ArgumentNullException(nameof(onDone)))(false);

        /// <inheritdoc />
        public void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone) =>
            (onDone ?? throw new ArgumentNullException(nameof(onDone)))(null);

        /// <inheritdoc />
        public void FetchPurchases(Action<IReadOnlyList<PlayOrder>?> onDone) => (onDone ?? throw new ArgumentNullException(nameof(onDone)))(null);

        /// <inheritdoc />
        public void Purchase(string sku, string? obfuscatedAccountId)
        {
            throw new InvalidOperationException("Sin Play no se abre ninguna compra.");
        }

        /// <inheritdoc />
        public bool Confirm(string token) => false;

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
