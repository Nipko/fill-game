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
    /// producto en <see cref="FetchProducts"/>; un producto que no se pidió no se termina. Si Play falla, el pedido sigue guardado y
    /// vuelve en la próxima consulta. <see cref="Acknowledge"/> no repite lo ya reconocido ni lo que está en vuelo.</item>
    /// <item>Una consulta de productos nueva deja sin respuesta (null) a la anterior; cada consulta de compras responde a quien la pidió.</item>
    /// <item>Al desconectarse Play, lo que esperaba respuesta responde null o false y se avisa <see cref="Disconnected"/> (también con una
    /// hoja abierta, que no se da por fallida: su respuesta llega igual); sin nada en vuelo no se avisa (la biblioteca se reconecta sola en
    /// la próxima llamada). <see cref="Dispose"/> suelta la conexión y no avisa de nada más.</item>
    /// <item>Un aviso en vivo de error, o OK sin ninguna compra utilizable, hace fallar la hoja que abrió este puerto (no se queda colgada);
    /// sin hoja abierta no responde a nada.</item>
    /// <item>El precio sale del importe en micros y de la moneda (nunca de la cadena de Play); la hora de compra, de <c>purchaseTime</c>.</item>
    /// </list>
    /// Hilo principal de Unity (el puente real pasa ahí los avisos de Java). Nunca escribe el token ni el UID en los logs.
    /// </summary>
    public sealed class NativePlayStorePort : IPlayStorePort, IPlayAccountCountryPort, IBillingBridgeListener
    {
        private readonly IBillingBridge _bridge;
        private readonly Dictionary<string, PlayProductType> _types = new Dictionary<string, PlayProductType>(StringComparer.Ordinal);
        private readonly Dictionary<string, Known> _known = new Dictionary<string, Known>(StringComparer.Ordinal);
        private readonly List<Action<bool>> _connectWaiting = new List<Action<bool>>();
        private readonly Dictionary<int, Action<IReadOnlyList<PlayOrder>?>> _purchasesWaiting = new Dictionary<int, Action<IReadOnlyList<PlayOrder>?>>();
        private readonly Dictionary<int, Action<string?>> _countryWaiting = new Dictionary<int, Action<string?>>();
        private Action<IReadOnlyList<PlayProductInfo>?>? _productsDone;
        private int _productsRequest;
        private int _nextRequest;
        private string? _launching; // SKU de la hoja que este puerto abrió y aún no respondió
        private bool _launchSurvivedDisconnect; // el servicio se cayó con esa hoja abierta
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

        /// <summary>Pedidos cobrados que conoce este puerto: sin terminar, o no consumibles ya reconocidos (para los tests).</summary>
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

        /// <summary>
        /// El país de la cuenta de Play (<c>getBillingConfigAsync</c>; D-057): el código tal como lo da Play, o null si falla, si se
        /// desconecta o tras <see cref="Dispose"/>. No guarda nada.
        /// </summary>
        public void FetchAccountCountry(Action<string?> onDone)
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
            _countryWaiting[id] = onDone;
            try
            {
                _bridge.QueryBillingConfig(id);
            }
            catch (Exception)
            {
                if (_countryWaiting.Remove(id))
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

            _launching = sku;
            _launchSurvivedDisconnect = false;
            try
            {
                _bridge.Launch(sku, obfuscatedAccountId);
            }
            catch (Exception)
            {
                ClearLaunch();
                PurchaseFailed?.Invoke(sku, PlayFailureKind.Failed);
            }
        }

        /// <inheritdoc />
        public bool Confirm(string token)
        {
            if (_disposed || token == null || !_known.TryGetValue(token, out Known? k)
                || !_types.TryGetValue(k.Sku, out PlayProductType type))
            {
                return false; // sin el pedido, o de un producto que no se pidió: no se sabe si consumirlo, así que no se termina
            }

            if (type == PlayProductType.Consumable)
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

            return k.Acknowledged || RequestAcknowledge(token, k); // un no consumible reconocido está terminado
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
            DeliverAllCountries();
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
            if (_disposed)
            {
                return;
            }

            // La hoja abierta cuando se cayó el servicio y que sigue sin respuesta tras reconectar (Refresh al volver a primer plano,
            // así que la hoja ya se cerró): su resultado se perdió. Falla para que la tienda no se quede «comprando»; si Play cobró, la
            // compra vuelve con la consulta que sigue y la cola la acredita una vez (sin el contexto de la hoja; iap.md §7).
            // Solo la respuesta a un Connect pedido (el Restart del Refresh): una reconexión que llega sola, con la hoja aún en pantalla,
            // no la da por perdida.
            string? orphan = code == BillingCodes.Ok && _launchSurvivedDisconnect && _connectWaiting.Count > 0 ? _launching : null;
            if (orphan != null)
            {
                ClearLaunch();
            }

            DeliverConnect(code == BillingCodes.Ok);
            if (orphan != null && !_disposed)
            {
                PurchaseFailed?.Invoke(orphan, PlayFailureKind.Unavailable);
            }
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnDisconnected()
        {
            if (_disposed)
            {
                return;
            }

            // La biblioteca se reconecta sola en la próxima llamada (enableAutoServiceReconnection).
            // - Lo que esperaba respuesta (conexión, consultas) responde ya, sin Play, y se avisa: PlayBillingClient queda sin Play hasta
            //   Refresh. Sin nada de eso en vuelo, la tienda sigue lista y la próxima llamada reconecta.
            // - Una hoja abierta no se da por fallida: la hoja de Play es otra actividad y su respuesta llega por el resultado de la
            //   actividad, no por la conexión del servicio (y el jugador aún puede pagar). Se deja de vender (Disconnected) y la hoja
            //   espera su respuesta en vivo, que sí la cierra.
            // - Consumos y reconocimientos en vuelo se vuelven a pedir en el acto (la biblioteca reconecta; Play no repite lo hecho).
            bool waiting = _connectWaiting.Count > 0 || _productsDone != null || _purchasesWaiting.Count > 0 || _launching != null;
            _launchSurvivedDisconnect |= _launching != null;
            var reconsume = new List<string>();
            var reacknowledge = new List<KeyValuePair<string, Known>>();
            foreach (KeyValuePair<string, Known> pair in _known)
            {
                // Solo se repite lo mismo que estaba en vuelo: un consumo (ya acreditado) o un reconocimiento (sin consumir).
                if (pair.Value.Consuming)
                {
                    reconsume.Add(pair.Key);
                }
                else if (pair.Value.Acknowledging)
                {
                    reacknowledge.Add(pair);
                }

                pair.Value.Consuming = false;
                pair.Value.Acknowledging = false;
            }

            DeliverConnect(false);
            DeliverProducts(null);
            DeliverAllPurchases();
            DeliverAllCountries(); // sin avisar Disconnected por esto solo: el país no es parte de la venta
            if (waiting && !_disposed)
            {
                Disconnected?.Invoke();
            }

            for (int i = 0; i < reconsume.Count && !_disposed; i++)
            {
                Confirm(reconsume[i]); // solo se pidió consumir tras acreditar (PlayBillingClient.Finish)
            }

            for (int i = 0; i < reacknowledge.Count && !_disposed; i++)
            {
                RequestAcknowledge(reacknowledge[i].Key, reacknowledge[i].Value);
            }
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
        void IBillingBridgeListener.OnBillingConfig(int requestId, int code, string? countryCode)
        {
            if (_disposed || !_countryWaiting.TryGetValue(requestId, out Action<string?>? done))
            {
                return;
            }

            _countryWaiting.Remove(requestId);
            done(code == BillingCodes.Ok && !string.IsNullOrEmpty(countryCode) ? countryCode : null);
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

            string? launching = _launching;
            if (code != BillingCodes.Ok)
            {
                // Un error en vivo solo puede ser la respuesta de la hoja (Play no dice el SKU): sin hoja de este puerto, no responde a nada.
                ClearLaunch();
                if (launching != null)
                {
                    PurchaseFailed?.Invoke(launching, BillingCodes.FailureOf(code));
                    RaiseIfServiceLost(code);
                }

                return;
            }

            List<BillingPurchaseRow> purchases = BillingRows.Purchases(rows);
            if (purchases.Count == 0)
            {
                // OK sin ninguna compra que se pueda usar (lista vacía, varios productos, estado sin especificar). Con una hoja de este
                // puerto abierta, falla, para que no quede colgada; si Play cobró algo, vuelve con la próxima consulta y la cola lo
                // acredita una vez. Sin hoja abierta, no hay nada que responder.
                ClearLaunch();
                if (launching != null)
                {
                    PurchaseFailed?.Invoke(launching, PlayFailureKind.Failed);
                }

                return;
            }

            var orders = new List<PlayOrder>(purchases.Count);
            bool answered = false;
            bool alreadyOwned = true;
            for (int i = 0; i < purchases.Count; i++)
            {
                PlayOrder order = Map(purchases[i]);
                orders.Add(order);
                if (launching != null && string.Equals(order.Sku, launching, StringComparison.Ordinal))
                {
                    answered = true; // la respuesta de la hoja; un aviso de otro SKU (un pago pendiente que se cobró) no la cierra
                    alreadyOwned &= order.State == PlayOrderState.Confirmed;
                }
            }

            if (answered)
            {
                ClearLaunch();
                if (alreadyOwned)
                {
                    // Solo un no consumible que ya estaba reconocido: no es un cobro nuevo. La hoja responde «ya lo tiene» ANTES del aviso,
                    // para que, ya cerrada, la restauración entre en la cola (con la hoja abierta, VerifiedStoreService.Resume se la salta).
                    PurchaseFailed?.Invoke(launching, PlayFailureKind.AlreadyOwned);
                }
            }

            for (int i = 0; i < orders.Count && !_disposed; i++)
            {
                OrderUpdated?.Invoke(orders[i]);
            }
        }

        /// <inheritdoc />
        void IBillingBridgeListener.OnLaunchFailed(string? sku, int code)
        {
            if (!_disposed)
            {
                ClearLaunch();
                PurchaseFailed?.Invoke(string.IsNullOrEmpty(sku) ? null : sku, BillingCodes.FailureOf(code));
                RaiseIfServiceLost(code);
            }
        }

        /// <summary>
        /// La hoja no se abrió o falló porque el servicio de Play se cayó y no se pudo reconectar: se avisa como desconexión, para que
        /// PlayBillingClient deje de vender hasta reconectar (Refresh), en lugar de fallar cada compra.
        /// </summary>
        private void RaiseIfServiceLost(int code)
        {
            if (code == BillingCodes.ServiceDisconnected && !_disposed)
            {
                Disconnected?.Invoke();
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
                // Sigue sin reconocer: quien lo pidió lo puede volver a pedir (VerifiedStoreService.Resume repite el deferred_ack al
                // arrancar y al volver a primer plano; PlayBillingClient vuelve a terminar lo que Play devuelve sin terminar).
                return;
            }

            k.Acknowledged = true; // un no consumible reconocido queda como «hecho» para Acknowledge y Confirm
        }

        // ------------------------------------------------------------------ privado

        private void ClearLaunch()
        {
            _launching = null;
            _launchSurvivedDisconnect = false;
        }

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

            if (!_known.TryGetValue(row.Token, out Known? k))
            {
                k = new Known(row.Sku);
                _known[row.Token] = k;
            }

            k.Acknowledged |= row.Acknowledged;
            if (row.Acknowledged && _types.TryGetValue(row.Sku, out PlayProductType type) && type == PlayProductType.NonConsumable)
            {
                // Restauración: ya reconocido. Se queda como conocido para que Acknowledge y Confirm respondan «hecho».
                return new PlayOrder(row.Sku, row.Token, PlayOrderState.Confirmed, row.PurchasedUtc);
            }

            return new PlayOrder(row.Sku, row.Token, PlayOrderState.Purchased, row.PurchasedUtc);
        }

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

        private void DeliverAllCountries()
        {
            if (_countryWaiting.Count == 0)
            {
                return;
            }

            var waiting = new List<Action<string?>>(_countryWaiting.Values);
            _countryWaiting.Clear();
            for (int i = 0; i < waiting.Count; i++)
            {
                waiting[i](null);
            }
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
