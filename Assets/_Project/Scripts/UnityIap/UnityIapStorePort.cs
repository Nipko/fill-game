#nullable enable
#if AF_UNITY_IAP
using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace AF.IAP.UnityIap
{
    /// <summary>
    /// <see cref="IPlayStorePort"/> sobre Unity IAP 5.4.4 (<c>StoreController</c> de Google Play, que trae Play Billing Library 9.0.0;
    /// D-040; 08 §7 #3). Solo traduce: no decide nada ni acredita nada (eso es <see cref="PlayBillingClient"/> y
    /// <see cref="VerifiedStoreService"/>). Compila en el editor con el paquete, pero solo se crea en el teléfono
    /// (<see cref="PlayBillingFactory"/>).
    /// <list type="bullet">
    /// <item>La tienda se pide por nombre (<c>GooglePlay</c>), sin depender de <c>Resources/BillingMode.json</c>: sin ese archivo Unity IAP
    /// usaría la tienda falsa en Android.</item>
    /// <item>No llama a <c>UnityServices.InitializeAsync</c>: el juego no usa Unity Gaming Services en la 1.0 (08 §7 #9).</item>
    /// <item>Las compras de <c>FetchPurchases</c> llegan solo por <c>OnPurchasesFetched</c> (<c>ProcessPendingOrdersOnPurchasesFetched(false)</c>);
    /// <see cref="OrderUpdated"/> es solo lo que Play avisa en vivo.</item>
    /// <item><c>PendingOrder</c> → <see cref="PlayOrderState.Purchased"/> (se guarda para <see cref="Confirm"/> hasta que Unity IAP confirma
    /// que lo terminó); <c>DeferredOrder</c> → <see cref="PlayOrderState.PaymentPending"/>; <c>ConfirmedOrder</c> (no consumible ya
    /// reconocido) → <see cref="PlayOrderState.Confirmed"/>.</item>
    /// <item>Un producto sin cargar (<c>availableToPurchase</c> false) o con precio 0 no da precio.</item>
    /// <item>Una consulta de productos que Unity IAP sigue reintentando (su política reintenta siempre) no se pide otra vez: la respuesta
    /// tardía va al que espere entonces. Cada consulta de compras sí se pide a Unity IAP (si una se pierde, la siguiente no queda
    /// tapada).</item>
    /// <item><see cref="Dispose"/> suelta los eventos de Unity IAP, cuyos servicios son únicos en el proceso: al recargar la escena el
    /// puerto anterior deja de oír.</item>
    /// <item>El token es el <c>purchaseToken</c> de Play (<c>IGoogleOrderInfo.PurchaseToken</c>; Unity IAP lo usa también como
    /// <c>TransactionID</c>). Nunca va a los logs.</item>
    /// </list>
    /// Hilo principal (Unity IAP llama a sus eventos en el hilo principal).
    /// </summary>
    public sealed class UnityIapStorePort : IPlayStorePort
    {
        private readonly StoreController _store;
        private readonly Dictionary<string, PendingOrder> _pending = new Dictionary<string, PendingOrder>(StringComparer.Ordinal);
        private readonly List<Action<IReadOnlyList<PlayOrder>?>> _purchasesWaiting = new List<Action<IReadOnlyList<PlayOrder>?>>();
        // Del proceso, como los servicios de Unity IAP: tras recargar la escena, el puerto nuevo sabe que Unity sigue reintentando la
        // consulta de productos del anterior (pedir otra fallaría en el acto) y recibe su respuesta.
        private static bool s_productsInFlight;

        private Action<IReadOnlyList<PlayProductInfo>?>? _productsDone;
        private bool _disposed;

        public UnityIapStorePort()
        {
            _store = UnityIAPServices.StoreController(GooglePlay.Name);

            // Las compras que devuelve FetchPurchases llegan solo por OnPurchasesFetched (sin repetirlas por OnPurchasePending, y sin
            // otro evento de telemetría de Unity por cada una; Q-IAP-1); las que Play cobra con la app abierta siguen llegando por
            // OnPurchasePending.
            _store.ProcessPendingOrdersOnPurchasesFetched(false);
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            _store.OnPurchaseFailed += OnPurchaseFailedRaw;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _store.OnStoreDisconnected += OnStoreDisconnected;
        }

        /// <inheritdoc />
        public event Action<PlayOrder>? OrderUpdated;

        /// <inheritdoc />
        public event Action<string?, PlayFailureKind>? PurchaseFailed;

        /// <inheritdoc />
        public event Action? Disconnected;

        /// <inheritdoc />
        public async void Connect(Action<bool> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            bool connected;
            try
            {
                await _store.Connect();
                connected = _store.GetConnectionState() == ConnectionState.Connected;
            }
            catch (Exception)
            {
                connected = false; // sin Play Store, sin cuenta o sin red: no se vende
            }

            onDone(connected);
        }

        /// <inheritdoc />
        public void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone)
        {
            if (products == null)
            {
                throw new ArgumentNullException(nameof(products));
            }

            DeliverProducts(null); // una consulta anterior sin responder ya no cuenta
            _productsDone = onDone ?? throw new ArgumentNullException(nameof(onDone));
            if (s_productsInFlight)
            {
                return; // Unity IAP sigue reintentando la anterior: su respuesta será la de esta
            }

            var definitions = new List<ProductDefinition>(products.Count);
            for (int i = 0; i < products.Count; i++)
            {
                PlayProductDefinition p = products[i];
                definitions.Add(new ProductDefinition(p.Sku, p.Type == PlayProductType.NonConsumable ? ProductType.NonConsumable : ProductType.Consumable));
            }

            s_productsInFlight = true;
            try
            {
                _store.FetchProducts(definitions);
            }
            catch (Exception)
            {
                s_productsInFlight = false;
                DeliverProducts(null);
            }
        }

        /// <inheritdoc />
        public void FetchPurchases(Action<IReadOnlyList<PlayOrder>?> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            _purchasesWaiting.Add(onDone); // la primera respuesta que llegue va a todos los que esperan
            try
            {
                _store.FetchPurchases();
            }
            catch (Exception)
            {
                DeliverPurchases(null);
            }
        }

        /// <inheritdoc />
        public void Purchase(string sku, string? obfuscatedAccountId)
        {
            Product? product = _store.GetProductById(sku);
            if (product == null || !product.availableToPurchase)
            {
                PurchaseFailed?.Invoke(sku, PlayFailureKind.Unavailable); // no publicado en Play Console o sin cargar
                return;
            }

            try
            {
                if (obfuscatedAccountId != null)
                {
                    _store.GooglePlayStoreExtendedService?.SetObfuscatedAccountId(obfuscatedAccountId);
                }

                _store.PurchaseProduct(product);
            }
            catch (Exception)
            {
                PurchaseFailed?.Invoke(sku, PlayFailureKind.Failed);
            }
        }

        /// <inheritdoc />
        public bool Confirm(string token)
        {
            if (token == null || !_pending.TryGetValue(token, out PendingOrder? order))
            {
                return false;
            }

            try
            {
                _store.ConfirmPurchase(order); // el PendingOrder se suelta en OnPurchaseConfirmed, solo si Play lo terminó
                return true;
            }
            catch (Exception)
            {
                return false; // sigue sin terminar en Play: vuelve en la próxima consulta (ya acreditada, solo se termina)
            }
        }

        /// <inheritdoc />
        /// <remarks>Unity IAP 5.4.4 no reconoce sin consumir (Q-IAP-2): este puerto se retira con <c>BILL-manifest.diff</c>.</remarks>
        public bool Acknowledge(string token) => false;

        private void OnPurchasePending(PendingOrder order)
        {
            PlayOrder? mapped = Map(order, PlayOrderState.Purchased);
            if (mapped == null)
            {
                return;
            }

            _pending[mapped.Token] = order;
            OrderUpdated?.Invoke(mapped);
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            PlayOrder? mapped = Map(order, PlayOrderState.PaymentPending);
            if (mapped != null)
            {
                OrderUpdated?.Invoke(mapped);
            }
        }

        private void OnPurchaseConfirmed(Order order)
        {
            // ConfirmPurchase solo se llama tras acreditar. Si Play lo terminó, se suelta el PendingOrder; un FailedOrder lo deja: el
            // pedido sigue cobrado en Play, vuelve en la próxima consulta y PlayBillingClient lo vuelve a terminar (sin acreditar otra vez).
            if (order is FailedOrder)
            {
                return;
            }

            string? token = TokenOf(order);
            if (token != null)
            {
                _pending.Remove(token);
            }
        }

        private void OnPurchaseFailedRaw(FailedOrder order)
        {
            string? sku = SkuOf(order);
            PurchaseFailed?.Invoke(sku, Kind(order.FailureReason));
        }

        private void OnProductsFetched(List<Product> products)
        {
            s_productsInFlight = false;
            DeliverProducts(products);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            // Algún producto no está en Play Console (o falló la consulta): vale lo que ya haya en la caché; sin nada, no hay tienda.
            s_productsInFlight = false;
            var cached = new List<Product>(_store.GetProducts());
            DeliverProducts(cached.Count > 0 ? cached : null);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            DeliverProducts(null); // si Unity IAP sigue reintentando los productos, su respuesta irá al próximo que pregunte
            DeliverPurchases(null);
            Disconnected?.Invoke();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseDeferred -= OnPurchaseDeferred;
            _store.OnPurchaseFailed -= OnPurchaseFailedRaw;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasesFetched -= OnPurchasesFetched;
            _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _store.OnStoreDisconnected -= OnStoreDisconnected;
            DeliverProducts(null);
            DeliverPurchases(null);
            _pending.Clear();
        }

        private void DeliverProducts(IList<Product>? products)
        {
            Action<IReadOnlyList<PlayProductInfo>?>? done = _productsDone;
            if (done == null)
            {
                return;
            }

            _productsDone = null;
            if (products == null)
            {
                done(null);
                return;
            }

            var infos = new List<PlayProductInfo>(products.Count);
            for (int i = 0; i < products.Count; i++)
            {
                Product? p = products[i];
                ProductMetadata? meta = p?.metadata;
                string? sku = p?.definition?.id;
                if (p == null || !p.availableToPurchase || meta == null || string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(meta.isoCurrencyCode)
                    || meta.localizedPrice <= 0m)
                {
                    continue;
                }

                // El importe de Play (price_amount_micros / 1.000.000), nunca la cadena formateada (D-025, punto 3 [P]).
                long micros = (long)Math.Round(meta.localizedPrice * 1000000m, 0, MidpointRounding.AwayFromZero);
                infos.Add(new PlayProductInfo(sku!, meta.isoCurrencyCode, micros));
            }

            done(infos);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            var list = new List<PlayOrder>();
            for (int i = 0; i < orders.PendingOrders.Count; i++)
            {
                PendingOrder order = orders.PendingOrders[i];
                PlayOrder? mapped = Map(order, PlayOrderState.Purchased);
                if (mapped != null)
                {
                    _pending[mapped.Token] = order;
                    list.Add(mapped);
                }
            }

            for (int i = 0; i < orders.DeferredOrders.Count; i++)
            {
                PlayOrder? mapped = Map(orders.DeferredOrders[i], PlayOrderState.PaymentPending);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            for (int i = 0; i < orders.ConfirmedOrders.Count; i++)
            {
                PlayOrder? mapped = Map(orders.ConfirmedOrders[i], PlayOrderState.Confirmed);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            DeliverPurchases(list);
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            DeliverPurchases(null);
        }

        private void DeliverPurchases(IReadOnlyList<PlayOrder>? orders)
        {
            if (_purchasesWaiting.Count == 0)
            {
                return;
            }

            var waiting = _purchasesWaiting.ToArray();
            _purchasesWaiting.Clear();
            for (int i = 0; i < waiting.Length; i++)
            {
                waiting[i](orders);
            }
        }

        private static PlayOrder? Map(Order order, PlayOrderState state)
        {
            string? sku = SkuOf(order);
            string? token = TokenOf(order);
            return string.IsNullOrEmpty(sku) || token == null ? null : new PlayOrder(sku!, token, state);
        }

        private static string? TokenOf(Order order)
        {
            string? token = order?.Info?.Google?.PurchaseToken;
            if (string.IsNullOrEmpty(token))
            {
                token = order?.Info?.TransactionID;
            }

            return string.IsNullOrEmpty(token) ? null : token;
        }

        private static string? SkuOf(Order order)
        {
            IReadOnlyList<CartItem>? items = order?.CartOrdered?.Items();
            if (items == null || items.Count == 0)
            {
                return null;
            }

            return items[0]?.Product?.definition?.id;
        }

        private static PlayFailureKind Kind(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.UserCancelled:
                    return PlayFailureKind.Cancelled;
                case PurchaseFailureReason.DuplicateTransaction:
                case PurchaseFailureReason.ExistingPurchasePending:
                    return PlayFailureKind.AlreadyOwned;
                case PurchaseFailureReason.PurchasingUnavailable:
                case PurchaseFailureReason.ProductUnavailable:
                case PurchaseFailureReason.StoreNotConnected:
                    return PlayFailureKind.Unavailable;
                default:
                    return PlayFailureKind.Failed;
            }
        }
    }
}
#endif
