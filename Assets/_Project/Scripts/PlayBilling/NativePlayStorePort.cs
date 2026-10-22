#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace AF.IAP.PlayBilling
{
    /// <summary>
    /// <see cref="IPlayStorePort"/> sobre el plugin propio de Play Billing Library 9.0.0 (<c>AfBilling.androidlib</c>; D-040, añadido de
    /// Q-IAP-1; 08 §7 #3 y §10.3), a través de un <see cref="IBillingBridge"/>. Solo traduce: no decide ni acredita nada (eso es
    /// <see cref="PlayBillingClient"/> y <see cref="VerifiedStoreService"/>). Sin JNI: el puente real (<c>AndroidBillingBridge</c>) solo
    /// existe en el teléfono, y los tests usan uno falso. Reglas:
    /// <list type="bullet">
    /// <item>Lo de una consulta de compras llega solo por su <c>onDone</c>; <see cref="OrderUpdated"/> es solo lo que Play avisa en vivo
    /// (<c>PurchasesUpdatedListener</c>).</item>
    /// <item><c>PURCHASED</c> → <see cref="PlayOrderState.Purchased"/> (se guarda para <see cref="Confirm"/> y <see cref="Acknowledge"/> hasta
    /// que Play lo termina), salvo un no consumible ya reconocido → <see cref="PlayOrderState.Confirmed"/>; <c>PENDING</c> →
    /// <see cref="PlayOrderState.PaymentPending"/>. Un consumible reconocido sin consumir (<c>deferred_ack</c>) sigue siendo
    /// <see cref="PlayOrderState.Purchased"/>: se consume tras acreditar.</item>
    /// <item><see cref="Confirm"/> consume (<c>consumeAsync</c>) o reconoce (<c>acknowledgePurchase</c>) según el tipo con que se pidió el
    /// producto en <see cref="FetchProducts"/>; si Play falla, el pedido sigue guardado y vuelve en la próxima consulta.</item>
    /// <item>Una consulta de productos nueva deja sin respuesta (null) a la anterior; cada consulta de compras responde a quien la pidió.</item>
    /// <item>Al desconectarse Play, las consultas en vuelo responden null; <see cref="Dispose"/> suelta la conexión y no avisa de nada más.</item>
    /// <item>El precio sale del importe en micros y de la moneda (nunca de la cadena de Play); la hora de compra, de <c>purchaseTime</c>.</item>
    /// </list>
    /// Hilo principal de Unity (el puente real pasa ahí los avisos de Java). Nunca escribe el token ni el UID en los logs.
    /// </summary>
    public sealed class NativePlayStorePort : IPlayStorePort, IBillingBridgeListener
    {
        private readonly IBillingBridge _bridge;
        private readonly Dictionary<string, PlayProductType> _types = new Dictionary<string, PlayProductType>(StringComparer.Ordinal);
        private readonly Dictionary<string, Known> _known = new Dictionary<string, Known>(StringComparer.Ordinal);
        private readonly List<Action<bool>> _connectWaiting = new List<Action<bool>>();
        private readonly Dictionary<int, Action<IReadOnlyList<PlayOrder>?>> _purchasesWaiting = new Dictionary<int, Action<IReadOnlyList<PlayOrder>?>>();
        private Action<IReadOnlyList<PlayProductInfo>?>? _productsDone;
        private int _productsRequest;
        private int _nextRequest;
        private bool _disposed;

        /// <param name="bridge">Crea el puente con este puerto como oyente (el real, con JNI, solo en el teléfono).</param>
        public NativePlayStorePort(Func<IBillingBridgeListener, IBillingBridge> bridge)
        {
            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            _bridge = bridge(this) ?? throw new InvalidOperationException("Sin puente con el plugin de Play Billing.");
        }

        /// <inheritdoc />
        public event Action<PlayOrder>? OrderUpdated;

        /// <inheritdoc />
        public event Action<string?, PlayFailureKind>? PurchaseFailed;

        /// <inheritdoc />
        public event Action? Disconnected;

        /// <summary>Pedidos cobrados que este puerto puede terminar o reconocer (para los tests).</summary>
        public int KnownPurchases => _known.Count;

        /// <inheritdoc />
        public void Connect(Action<bool> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            if (_disposed)
            {
                onDone(false);
                return;
            }

            _connectWaiting.Add(onDone); // la respuesta de la conexión en curso vale para todos los que esperan
            try
            {
                _bridge.Connect();
            }
            catch (Exception)
            {
                DeliverConnect(false); // sin Play Store o el plugin falló: no se vende
            }
        }

        /// <inheritdoc />
        public void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone)
        {
            if (products == null)
            {
                throw new ArgumentNullException(nameof(products));
            }

            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            DeliverProducts(null); // una consulta anterior sin responder ya no cuenta
            if (_disposed)
            {
                onDone(null);
                return;
            }

            var skus = new StringBuilder();
            for (int i = 0; i < products.Count; i++)
            {
                PlayProductDefinition p = products[i];
                _types[p.Sku] = p.Type;
                if (skus.Length > 0)
                {
                    skus.Append('\n');
                }

                skus.Append(p.Sku);
            }

            int id = ++_nextRequest;
            _productsRequest = id;
            _productsDone = onDone;
            try
            {
                _bridge.QueryProducts(id, skus.ToString());
            }
            catch (Exception)
            {
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

            if (_disposed)
            {
                onDone(null);
                return;
            }

            int id = ++_nextRequest;
            _purchasesWaiting[id] = onDone;
            try
            {
                _bridge.QueryPurchases(id);
            }
            catch (Exception)
            {
                if (_purchasesWaiting.Remove(id))
                {
                    onDone(null);
                }
            }
        }

        /// <inheritdoc />
        public void Purchase(string sku, string? obfuscatedAccountId)
        {
            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            if (_disposed)
            {
                PurchaseFailed?.Invoke(sku, PlayFailureKind.Unavailable);
                return;
            }

            try
            {
                _bridge.Launch(sku, obfuscatedAccountId);
            }
            catch (Exception)
            {
                PurchaseFailed?.Invoke(sku, PlayFailureKind.Failed);
            }
        }

        /// <inheritdoc />
        public bool Confirm(string token)
        {
            if (_disposed || token == null || !_known.TryGetValue(token, out Known? k))
            {
                return false;
            }

            if (TypeOf(k.Sku) == PlayProductType.Consumable)
            {
                if (k.Consuming)
                {
                    return true; // ya pedido: su respuesta lo suelta o lo deja
                }

                k.Consuming = true;
                try
                {
                    _bridge.Consume(token);
                    return true;
                }
                catch (Exception)
                {
                    k.Consuming = false;
                    return false; // sigue cobrado en Play: vuelve en la próxima consulta (ya acreditado, solo se termina)
                }
            }

            if (k.Acknowledged)
            {
                _known.Remove(token); // un no consumible ya reconocido está terminado
                return true;
            }

            return RequestAcknowledge(token, k);
        }

        /// <inheritdoc />
        public bool Acknowledge(string token)
        {
            if (_disposed || token == null || !_known.TryGetValue(token, out Known? k))
            {
                return false;
            }

            return k.Acknowledged || RequestAcknowledge(token, k);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            OrderUpdated = null;
            PurchaseFailed = null;
            Disconnected = null;
            DeliverConnect(false);
            DeliverProducts(null);
            DeliverAllPurchases();
            _known.Clear();
            try
            {
                _bridge.Dispose(); // endConnection en Java
            }
            catch (Exception)
            {
                // el plugin ya no está: nada que soltar
            }
        }

        // ------------------------------------------------------------------ avisos del plugin (hilo principal de Unity)

        /// <inheritdoc />
        void IBillingBridgeListener.OnSetupFinished(int code)
        {
            if (!_disposed)
            {
                DeliverConnect(code == BillingCodes.Ok);
            }
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnDisconnected()
        {
            if (_disposed)
            {
                return;
            }

            DeliverConnect(false);
            DeliverProducts(null);
            DeliverAllPurchases();
            Disconnected?.Invoke();
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnProducts(int requestId, int code, string? rows)
        {
            if (_disposed || requestId != _productsRequest || _productsDone == null)
            {
                return; // de una consulta que ya no cuenta
            }

            DeliverProducts(code == BillingCodes.Ok ? BillingRows.Products(rows) : null);
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnPurchases(int requestId, int code, string? rows)
        {
            if (_disposed || !_purchasesWaiting.TryGetValue(requestId, out Action<IReadOnlyList<PlayOrder>?>? done))
            {
                return;
            }

            _purchasesWaiting.Remove(requestId);
            if (code != BillingCodes.Ok)
            {
                done(null);
                return;
            }

            List<BillingPurchaseRow> purchases = BillingRows.Purchases(rows);
            var orders = new List<PlayOrder>(purchases.Count);
            for (int i = 0; i < purchases.Count; i++)
            {
                orders.Add(Map(purchases[i]));
            }

            done(orders);
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnPurchasesUpdated(int code, string? rows)
        {
            if (_disposed)
            {
                return;
            }

            if (code != BillingCodes.Ok)
            {
                PurchaseFailed?.Invoke(null, BillingCodes.FailureOf(code)); // Play no dice el SKU: es el de la hoja abierta
                return;
            }

            List<BillingPurchaseRow> purchases = BillingRows.Purchases(rows);
            for (int i = 0; i < purchases.Count && !_disposed; i++)
            {
                OrderUpdated?.Invoke(Map(purchases[i]));
            }
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnLaunchFailed(string? sku, int code)
        {
            if (!_disposed)
            {
                PurchaseFailed?.Invoke(string.IsNullOrEmpty(sku) ? null : sku, BillingCodes.FailureOf(code));
            }
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnConsumed(string? token, int code)
        {
            if (_disposed || token == null || !_known.TryGetValue(token, out Known? k))
            {
                return;
            }

            k.Consuming = false;
            if (code == BillingCodes.Ok || code == BillingCodes.ItemNotOwned)
            {
                _known.Remove(token); // consumido (ITEM_NOT_OWNED: ya lo estaba)
            }
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnAcknowledged(string? token, int code)
        {
            if (_disposed || token == null || !_known.TryGetValue(token, out Known? k))
            {
                return;
            }

            k.Acknowledging = false;
            if (code != BillingCodes.Ok)
            {
                return; // sigue sin reconocer: se volverá a pedir
            }

            k.Acknowledged = true;
            if (TypeOf(k.Sku) == PlayProductType.NonConsumable)
            {
                _known.Remove(token); // un no consumible reconocido está terminado
            }
        }

        // ------------------------------------------------------------------ privado

        private bool RequestAcknowledge(string token, Known k)
        {
            if (k.Acknowledging)
            {
                return true;
            }

            k.Acknowledging = true;
            try
            {
                _bridge.Acknowledge(token);
                return true;
            }
            catch (Exception)
            {
                k.Acknowledging = false;
                return false;
            }
        }

        private PlayOrder Map(BillingPurchaseRow row)
        {
            if (row.PurchaseState == BillingRows.Pending)
            {
                return new PlayOrder(row.Sku, row.Token, PlayOrderState.PaymentPending, row.PurchasedUtc);
            }

            if (row.Acknowledged && TypeOf(row.Sku) == PlayProductType.NonConsumable)
            {
                _known.Remove(row.Token);
                return new PlayOrder(row.Sku, row.Token, PlayOrderState.Confirmed, row.PurchasedUtc);
            }

            if (!_known.TryGetValue(row.Token, out Known? k))
            {
                k = new Known(row.Sku);
                _known[row.Token] = k;
            }

            k.Acknowledged |= row.Acknowledged;
            return new PlayOrder(row.Sku, row.Token, PlayOrderState.Purchased, row.PurchasedUtc);
        }

        /// <summary>Tipo con que se pidió el producto; uno que no se pidió se trata como consumible (lo que no sea del catálogo no se termina).</summary>
        private PlayProductType TypeOf(string sku) =>
            _types.TryGetValue(sku, out PlayProductType type) ? type : PlayProductType.Consumable;

        private void DeliverConnect(bool connected)
        {
            if (_connectWaiting.Count == 0)
            {
                return;
            }

            Action<bool>[] waiting = _connectWaiting.ToArray();
            _connectWaiting.Clear();
            for (int i = 0; i < waiting.Length; i++)
            {
                waiting[i](connected);
            }
        }

        private void DeliverProducts(IReadOnlyList<PlayProductInfo>? products)
        {
            Action<IReadOnlyList<PlayProductInfo>?>? done = _productsDone;
            if (done == null)
            {
                return;
            }

            _productsDone = null;
            _productsRequest = 0;
            done(products);
        }

        private void DeliverAllPurchases()
        {
            if (_purchasesWaiting.Count == 0)
            {
                return;
            }

            var ids = new List<int>(_purchasesWaiting.Keys);
            ids.Sort(); // en el orden en que se pidieron
            var waiting = new List<Action<IReadOnlyList<PlayOrder>?>>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                waiting.Add(_purchasesWaiting[ids[i]]);
            }

            _purchasesWaiting.Clear();
            for (int i = 0; i < waiting.Count; i++)
            {
                waiting[i](null);
            }
        }

        private sealed class Known
        {
            public Known(string sku)
            {
                Sku = sku;
            }

            public string Sku { get; }

            /// <summary><c>isAcknowledged</c> (por Play o por el <c>deferred_ack</c>).</summary>
            public bool Acknowledged { get; set; }

            public bool Acknowledging { get; set; }

            public bool Consuming { get; set; }
        }
    }
}
