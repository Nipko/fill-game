#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;

namespace AF.IAP
{
    /// <summary>En qué punto está la conexión de <see cref="PlayBillingClient"/> con Play.</summary>
    public enum PlayBillingState
    {
        /// <summary>Nadie llamó a <see cref="PlayBillingClient.Start"/>.</summary>
        NotStarted = 0,

        /// <summary>Conectando, pidiendo los productos o consultando las compras.</summary>
        Starting = 1,

        /// <summary>Lista: precios de la tienda cargados y compras sin terminar consultadas.</summary>
        Ready = 2,

        /// <summary>
        /// Sin Play (sin conexión, sin Play Store, la consulta falló o Play se desconectó): no se vende; <see cref="PlayBillingClient.Refresh"/>
        /// reintenta.
        /// </summary>
        Unavailable = 3,
    }

    /// <summary>¿Puede la tienda de la plataforma cobrar ya? (<see cref="BillingReadyStoreService"/>).</summary>
    public interface IBillingAvailability
    {
        /// <summary>Conectada, con precios y con las compras sin terminar ya consultadas.</summary>
        bool IsReady { get; }

        /// <summary>Lista y con el precio de la tienda de <paramref name="sku"/> (nunca se cobra con un precio de respaldo).</summary>
        bool CanSell(string sku);

        /// <summary>
        /// ¿El precio de <paramref name="sku"/> es el de la tienda (cargado alguna vez en esta sesión), aunque ahora no esté lista? Para
        /// la analítica de lo que se acredita tarde.
        /// </summary>
        bool HasStorePrice(string sku);
    }

    /// <summary>
    /// <see cref="IBillingClient"/> real sobre Play Billing (Play Billing Library 9.0.0 con el plugin propio; D-040 y su añadido de
    /// Q-IAP-1; 08 §7 #3 y §10.3), a través de un
    /// <see cref="IPlayStorePort"/>. No acredita nada: cobra, guarda las compras sin terminar y las termina cuando
    /// <see cref="VerifiedStoreService"/> ya acreditó lo que el servidor verificó (D-035). Reglas:
    /// <list type="bullet">
    /// <item>Catálogo de 05 §4.1 (<see cref="StoreCatalog"/>): consumibles todos salvo Sin anuncios y las skins
    /// (<see cref="TypeOf"/>); la Mochila se consume (1 por cuenta la hace cumplir el servidor, F2c).</item>
    /// <item><c>setObfuscatedAccountId</c> = el UID que pasa <see cref="VerifiedStoreService"/> (08 §10.1; D-035, F3-3).</item>
    /// <item>Pendiente de pago (<c>PENDING</c>): <see cref="BillingResult.Pending"/>; cuando Play la cobra, vuelve como pedido cobrado y
    /// sale por <see cref="UnfinishedChanged"/> para que el juego llame a <see cref="VerifiedStoreService.Resume"/>.</item>
    /// <item>Lo cobrado sin terminar (de otra sesión, de otra instalación o los no consumibles que Play devuelve) se consulta al
    /// arrancar (<see cref="Start"/>) y al volver a primer plano (<see cref="Refresh"/>), y <see cref="UnfinishedTransactions"/> lo
    /// devuelve hasta <see cref="Finish"/>. Cada token, una vez por sesión: los repetidos de Play no abren otra verificación.</item>
    /// <item>La compra abierta solo la responde un aviso en vivo de Play (<see cref="IPlayStorePort.OrderUpdated"/> o
    /// <see cref="IPlayStorePort.PurchaseFailed"/>), nunca una consulta: un pedido del mismo SKU que llega por la consulta con la hoja
    /// abierta (otra sesión, otro dispositivo, una pendiente de pago antigua) espera a que la hoja responda y, si no era el suyo, sale
    /// después por <see cref="UnfinishedChanged"/> sin el contexto de la compra abierta.</item>
    /// <item><see cref="Finish"/> hace <see cref="IPlayStorePort.Confirm"/> (consume o reconoce) solo de un pedido que tiene; nunca antes de acreditar
    /// (lo garantiza quien llama). Si Play no lo terminó y vuelve como cobrado, se vuelve a pedir (ya acreditado: solo se termina).</item>
    /// <item><see cref="AcknowledgeLocal"/> (<c>deferred_ack</c>, 08 §10.3): <see cref="IPlayStorePort.Acknowledge"/>
    /// (<c>acknowledgePurchase</c>), también con un consumible, que sigue cobrado hasta que <see cref="Finish"/> lo consuma tras
    /// acreditar (D-040, añadido de Q-IAP-1: Q-IAP-2 se reabre; <c>docs/dev/iap.md</c>). Se puede repetir: el puerto no repite lo ya
    /// reconocido.</item>
    /// <item>Precios: los de la tienda (importe y moneda; D-025, punto 3 [P]); mientras no lleguen, o si la moneda no se puede escribir o
    /// el precio no es positivo, los de 05 §4.2 en <c>fallbackCurrency</c>, y ese SKU no se vende (<see cref="CanSell"/>,
    /// <see cref="BillingReadyStoreService"/>).</item>
    /// <item>Si Play se desconecta o falla una consulta, queda <see cref="PlayBillingState.Unavailable"/> y <see cref="Refresh"/> vuelve
    /// a conectar; un arranque que no termina (Play no contesta) también se repite en <see cref="Refresh"/>.</item>
    /// <item><see cref="Dispose"/> (al destruir la escena): suelta el puerto y deja de avisar; lo que quede sin terminar en Play vuelve en
    /// la sesión siguiente.</item>
    /// </list>
    /// Nunca escribe tokens ni el UID en los logs. Hilo principal.
    /// </summary>
    public sealed class PlayBillingClient : IBillingClient, IBillingAvailability, IDisposable
    {
        private readonly IPlayStorePort _port;
        private readonly StoreCatalog _catalog;
        private readonly string _fallbackCurrency;
        private readonly Dictionary<string, LocalPrice> _prices = new Dictionary<string, LocalPrice>(StringComparer.Ordinal);
        private readonly List<Tracked> _unfinished = new List<Tracked>();
        private readonly HashSet<string> _finished = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _ackRejected = new HashSet<string>(StringComparer.Ordinal);
        private Open? _open;
        private int _generation;
        private bool _disposed;
        private bool _notifyPending;
        private int _batchDepth;

        /// <param name="port">Puerto de Play (el plugin propio en el teléfono; uno falso en los tests).</param>
        /// <param name="catalog">Catálogo de 1.0 (los productos que se piden y su tipo).</param>
        /// <param name="fallbackCurrency">Moneda de los precios de 05 §4.2 mientras la tienda no dé los suyos.</param>
        public PlayBillingClient(IPlayStorePort port, StoreCatalog catalog, string fallbackCurrency = "USD")
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            StorePrices.Local(StorePrices.UsdTiers[0], fallbackCurrency); // valida la moneda
            _fallbackCurrency = fallbackCurrency;
            _port.OrderUpdated += OnOrderUpdated;
            _port.PurchaseFailed += OnPurchaseFailed;
            _port.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// <see cref="PlayBillingClient"/> sobre el puerto que da <paramref name="port"/>; si crearlo lanza (el SDK sin su plugin, por
        /// ejemplo), sobre <see cref="UnavailablePlayStorePort"/>: el juego arranca y la tienda no vende.
        /// </summary>
        public static PlayBillingClient Create(Func<IPlayStorePort> port, StoreCatalog catalog, string fallbackCurrency)
        {
            if (port == null)
            {
                throw new ArgumentNullException(nameof(port));
            }

            IPlayStorePort created;
            try
            {
                created = port() ?? new UnavailablePlayStorePort();
            }
            catch (Exception)
            {
                created = new UnavailablePlayStorePort(); // sin escribir nada en el log: el arranque sigue sin tienda
            }

            return new PlayBillingClient(created, catalog, fallbackCurrency);
        }

        /// <summary>Estado de la conexión.</summary>
        public PlayBillingState State { get; private set; }

        /// <inheritdoc />
        public bool IsReady => State == PlayBillingState.Ready;

        /// <summary>
        /// Llegó una compra cobrada que no es la respuesta de la compra abierta (una pendiente de pago que Play cobró, la de otra sesión o
        /// un no consumible devuelto por Play): quien la recibe llama a <see cref="VerifiedStoreService.Resume"/>.
        /// </summary>
        public event Action? UnfinishedChanged;

        /// <summary>Cambió el estado o los precios (el Mercado se repinta).</summary>
        public event Action? StateChanged;

        /// <summary>
        /// Pedidos cuyo <c>deferred_ack</c> rechazó el puerto alguna vez en la sesión (no tenía el pedido cobrado o el plugin falló), uno
        /// por pedido aunque después se reconozca. Se queda con su nombre para
        /// no cambiar el contrato (D-040, añadido de Q-IAP-1); con Unity IAP 5.4.4 contaba los consumibles, que no se podían reconocer sin
        /// consumir (Q-IAP-2), y con el plugin propio eso ya no pasa.
        /// </summary>
        public int AcknowledgeUnsupported { get; private set; }

        /// <summary>Productos sin precio de la tienda tras cargarlos (no publicados en Play Console o con moneda que no se escribe).</summary>
        public int MissingPrices { get; private set; }

        /// <summary>Tipo con que se registra en Play un producto del catálogo.</summary>
        public static PlayProductType TypeOf(StoreProduct product)
        {
            return (product ?? throw new ArgumentNullException(nameof(product))).Kind == ProductKind.NonConsumable
                ? PlayProductType.NonConsumable
                : PlayProductType.Consumable;
        }

        /// <summary>Conecta, pide los productos y consulta las compras sin terminar. Sin efecto si ya arrancó y no está en <see cref="PlayBillingState.Unavailable"/>.</summary>
        public void Start()
        {
            if (_disposed || State == PlayBillingState.Starting || State == PlayBillingState.Ready)
            {
                return;
            }

            Restart();
        }

        /// <summary>
        /// Al destruir la escena: suelta el puerto (y con él los eventos del SDK) y deja de avisar, sin llamar a nadie (tampoco a la compra
        /// abierta: la escena se está destruyendo y Play aún puede cobrarla). Lo cobrado sin terminar sigue en Play y vuelve a la tienda de
        /// la escena nueva.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _generation++;
            _port.OrderUpdated -= OnOrderUpdated;
            _port.PurchaseFailed -= OnPurchaseFailed;
            _port.Disconnected -= OnDisconnected;
            UnfinishedChanged = null;
            StateChanged = null;
            _open = null;
            State = PlayBillingState.Unavailable;
            _port.Dispose();
        }

        private void Restart()
        {
            int generation = ++_generation;
            SetState(PlayBillingState.Starting);
            _port.Connect(connected =>
            {
                if (generation != _generation)
                {
                    return; // un arranque anterior que respondió tarde
                }

                if (!connected)
                {
                    SetState(PlayBillingState.Unavailable);
                    return;
                }

                _port.FetchProducts(Definitions(), products => OnProducts(generation, products));
            });
        }

        /// <summary>
        /// Al volver a primer plano: si no hay Play, vuelve a intentarlo; si la hay, consulta otra vez las compras (lo que Play cobró
        /// mientras tanto). Si la consulta falla, queda sin Play hasta el próximo intento.
        /// </summary>
        public void Refresh()
        {
            if (_disposed)
            {
                return;
            }

            if (State != PlayBillingState.Ready)
            {
                Restart(); // sin Play, sin arrancar o con un arranque que no contestó: otra vez (lo que conteste tarde no cuenta)
                return;
            }

            int generation = _generation;
            FetchPurchases(ok =>
            {
                if (!ok && generation == _generation && State == PlayBillingState.Ready)
                {
                    SetState(PlayBillingState.Unavailable);
                }
            });
        }

        /// <inheritdoc />
        public LocalPrice PriceOf(StoreProduct product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            return _prices.TryGetValue(product.Sku, out LocalPrice price) ? price : product.PriceIn(_fallbackCurrency);
        }

        /// <inheritdoc />
        public bool HasStorePrice(string sku) => sku != null && _prices.ContainsKey(sku);

        /// <inheritdoc />
        public bool CanSell(string sku) => IsReady && HasStorePrice(sku);

        /// <inheritdoc />
        public void Purchase(string sku, string? obfuscatedAccountId, Action<BillingResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            _catalog.Get(sku);
            if (!CanSell(sku) || _open != null)
            {
                onDone(BillingResult.Failed()); // sin Play, sin su precio o con otra hoja abierta: no se cobra nada
                return;
            }

            _open = new Open(sku, onDone);
            _port.Purchase(sku, ContractRules.IsObfuscatedAccountId(obfuscatedAccountId) ? obfuscatedAccountId : null);
        }

        /// <inheritdoc />
        public IReadOnlyList<BillingTransaction> UnfinishedTransactions()
        {
            var list = new List<BillingTransaction>(_unfinished.Count);
            for (int i = 0; i < _unfinished.Count; i++)
            {
                if (!_unfinished[i].AwaitingOpen)
                {
                    list.Add(_unfinished[i].Transaction);
                }
            }

            return list;
        }

        /// <inheritdoc />
        public void Finish(string sku, string token, bool consume)
        {
            Tracked? t = Find(token);
            if (t == null)
            {
                return; // ya terminada o desconocida: idempotente
            }

            _unfinished.Remove(t);
            _finished.Add(token);
            if (t.State == PlayOrderState.Purchased)
            {
                // Confirm consume o reconoce según el tipo registrado (TypeOf), que coincide con consume (VerifiedStoreService).
                // Si Play no lo termina, el pedido vuelve como cobrado y Accept lo vuelve a pedir (ya acreditado: solo se termina).
                _port.Confirm(token);
            }
        }

        /// <inheritdoc />
        public void AcknowledgeLocal(string sku, string token)
        {
            Tracked? t = Find(token);
            StoreProduct? product = _catalog.Find(sku);
            if (t == null || product == null || t.State != PlayOrderState.Purchased)
            {
                return;
            }

            // Reconocer sin consumir (acknowledgePurchase), también un consumible: nunca se consume sin acreditar. Sigue cobrado y en la
            // cola; Finish lo termina (consume o da por terminado) cuando el servidor lo verifique. Se puede pedir más de una vez
            // (VerifiedStoreService.Resume lo vuelve a pedir si Play aún no lo reconoció): el puerto no repite lo ya reconocido ni lo
            // que está en vuelo.
            if (!_port.Acknowledge(token) && _ackRejected.Add(token))
            {
                AcknowledgeUnsupported++; // una vez por pedido, aunque Resume lo vuelva a pedir
            }
        }

        private void OnProducts(int generation, IReadOnlyList<PlayProductInfo>? products)
        {
            if (generation != _generation)
            {
                return;
            }

            if (products == null)
            {
                SetState(PlayBillingState.Unavailable);
                return;
            }

            _prices.Clear();
            for (int i = 0; i < products.Count; i++)
            {
                PlayProductInfo info = products[i];
                if (info == null || _catalog.Find(info.Sku) == null || info.PriceMicros <= 0)
                {
                    continue; // un precio 0 es un producto sin datos de la tienda, no un regalo
                }

                if (TryLocalPrice(info.Currency, info.PriceMicros, out LocalPrice price))
                {
                    _prices[info.Sku] = price;
                }
            }

            MissingPrices = _catalog.Products.Count - _prices.Count;
            FetchPurchases(ok =>
            {
                if (generation == _generation)
                {
                    SetState(ok ? PlayBillingState.Ready : PlayBillingState.Unavailable);
                }
            });
        }

        private void FetchPurchases(Action<bool> onDone)
        {
            int generation = _generation;
            _port.FetchPurchases(orders =>
            {
                if (_disposed || generation != _generation)
                {
                    return; // de un arranque anterior o tras soltar el puerto
                }

                if (orders != null)
                {
                    // Un solo UnfinishedChanged por consulta (cada uno es un Resume completo).
                    _batchDepth++;
                    try
                    {
                        for (int i = 0; i < orders.Count; i++)
                        {
                            if (orders[i] != null)
                            {
                                Accept(orders[i], false);
                            }
                        }
                    }
                    finally
                    {
                        _batchDepth--;
                    }

                    FlushUnfinished();
                }

                onDone(orders != null);
            });
        }

        private void NotifyUnfinished()
        {
            if (_batchDepth > 0)
            {
                _notifyPending = true;
                return;
            }

            UnfinishedChanged?.Invoke();
        }

        private void FlushUnfinished()
        {
            if (!_notifyPending)
            {
                return;
            }

            _notifyPending = false;
            UnfinishedChanged?.Invoke();
        }

        private void OnOrderUpdated(PlayOrder order)
        {
            if (order != null && !_disposed)
            {
                Accept(order, true);
            }
        }

        private void OnDisconnected()
        {
            if (!_disposed && (State == PlayBillingState.Ready || State == PlayBillingState.Starting))
            {
                _generation++; // lo que respondan tarde las consultas del arranque ya no cuenta
                SetState(PlayBillingState.Unavailable);
            }
        }

        /// <summary>
        /// Un pedido de Play. <paramref name="live"/>: aviso en vivo (puede responder a la compra abierta); si no, viene de una consulta
        /// de compras (nunca responde a la compra abierta).
        /// </summary>
        private void Accept(PlayOrder order, bool live)
        {
            StoreProduct? product = _catalog.Find(order.Sku);
            bool validToken = ContractRules.IsPurchaseToken(order.Token);
            if (product == null || !validToken)
            {
                // Un producto que esta versión no vende, o un token que no cumple el contrato: no se verifica. Si era el aviso de la compra
                // abierta, falla (si se cobró, Play la devolverá sin terminar y la versión que la venda la terminará).
                if (live && IsOpenFor(order.Sku) && order.State != PlayOrderState.Confirmed)
                {
                    Resolve(BillingResult.Failed(), null);
                }

                return;
            }

            switch (order.State)
            {
                case PlayOrderState.PaymentPending:
                    if (live && IsOpenFor(order.Sku))
                    {
                        Resolve(BillingResult.Pending(), null);
                    }

                    return;

                case PlayOrderState.Confirmed:
                    if (TypeOf(product) != PlayProductType.NonConsumable || _finished.Contains(order.Token) || Find(order.Token) != null)
                    {
                        return; // un consumible siempre vuelve como cobrado; lo ya visto en esta sesión, una vez
                    }

                    _unfinished.Add(new Tracked(order.Sku, order.Token, PlayOrderState.Confirmed, order.PurchasedUtc));
                    NotifyUnfinished();
                    return;

                default:
                    AcceptPurchased(order, live);
                    return;
            }
        }

        private void AcceptPurchased(PlayOrder order, bool live)
        {
            bool liveForOpen = live && IsOpenFor(order.Sku);
            if (_finished.Contains(order.Token))
            {
                // Ya acreditado y terminado en esta sesión, pero Play lo sigue teniendo cobrado: el consumo o el reconocimiento falló. Se vuelve a pedir.
                _port.Confirm(order.Token);
                if (liveForOpen)
                {
                    Resolve(BillingResult.Failed(), null); // la hoja no cobró nada nuevo (Play respondió con el pedido que ya había)
                }

                return;
            }

            Tracked? t = Find(order.Token);
            if (t != null)
            {
                // Repetido. Si es el aviso en vivo de un pedido que la consulta trajo con la hoja abierta, es su respuesta; si ya se
                // conocía (Play puede repetir el pedido sin terminar del mismo SKU en un aviso), la hoja no cobró nada nuevo:
                // falla, y ese pedido sigue su camino en la cola.
                if (liveForOpen)
                {
                    if (t.AwaitingOpen)
                    {
                        t.AwaitingOpen = false;
                        Resolve(BillingResult.Purchased(t.Transaction), t);
                    }
                    else
                    {
                        Resolve(BillingResult.Failed(), null);
                    }
                }

                return;
            }

            t = new Tracked(order.Sku, order.Token, PlayOrderState.Purchased, order.PurchasedUtc);
            _unfinished.Add(t);
            if (IsOpenFor(order.Sku))
            {
                if (live)
                {
                    Resolve(BillingResult.Purchased(t.Transaction), t);
                }
                else
                {
                    t.AwaitingOpen = true; // de una consulta con la hoja abierta: espera a que la hoja responda
                }

                return;
            }

            NotifyUnfinished();
        }

        private void OnPurchaseFailed(string? sku, PlayFailureKind kind)
        {
            if (_disposed || _open == null || (sku != null && !string.Equals(sku, _open.Sku, StringComparison.Ordinal)))
            {
                return;
            }

            Resolve(kind == PlayFailureKind.Cancelled ? BillingResult.Cancelled() : BillingResult.Failed(), null);
            if (kind == PlayFailureKind.AlreadyOwned && State == PlayBillingState.Ready)
            {
                FetchPurchases(_ => { }); // la compra anterior sin terminar vuelve y la termina la cola
            }
        }

        private bool IsOpenFor(string sku) => _open != null && string.Equals(_open.Sku, sku, StringComparison.Ordinal);

        /// <summary>
        /// Responde a la compra abierta y suelta los pedidos que la consulta trajo con la hoja abierta y que no eran su respuesta: salen
        /// por <see cref="UnfinishedChanged"/>, ya con la hoja cerrada, para que la cola los verifique sin el contexto de esta compra.
        /// </summary>
        private void Resolve(BillingResult result, Tracked? answer)
        {
            Open? open = _open;
            if (open == null)
            {
                return;
            }

            _open = null;
            open.OnDone(result);

            bool released = false;
            for (int i = 0; i < _unfinished.Count; i++)
            {
                if (_unfinished[i].AwaitingOpen && !ReferenceEquals(_unfinished[i], answer))
                {
                    _unfinished[i].AwaitingOpen = false;
                    released = true;
                }
            }

            if (released)
            {
                NotifyUnfinished();
            }
        }

        private void SetState(PlayBillingState state)
        {
            if (_disposed)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke();
        }

        private List<PlayProductDefinition> Definitions()
        {
            var list = new List<PlayProductDefinition>(_catalog.Products.Count);
            for (int i = 0; i < _catalog.Products.Count; i++)
            {
                StoreProduct p = _catalog.Products[i];
                list.Add(new PlayProductDefinition(p.Sku, TypeOf(p)));
            }

            return list;
        }

        private Tracked? Find(string token)
        {
            for (int i = 0; i < _unfinished.Count; i++)
            {
                if (string.Equals(_unfinished[i].Token, token, StringComparison.Ordinal))
                {
                    return _unfinished[i];
                }
            }

            return null;
        }

        /// <summary>
        /// El precio de la tienda como <see cref="LocalPrice"/>: unidades menores con los decimales de la función única de precios
        /// (D-025) y, de ellas, el importe. False si la moneda no se puede escribir (no es ISO 4217 o <see cref="LocalPrice"/> no la
        /// admite todavía, Q-IAP-3): entonces vale el precio de respaldo y el SKU no se vende.
        /// </summary>
        public static bool TryLocalPrice(string currency, long micros, out LocalPrice price)
        {
            price = default;
            try
            {
                long minor = AF.Localization.LocPrice.MinorUnitsFromMicros(currency, micros);
                int decimals = AF.Localization.LocPrice.DecimalsOf(currency);
                decimal amount = minor;
                for (int i = 0; i < decimals; i++)
                {
                    amount /= 10m;
                }

                price = new LocalPrice(currency, amount);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private sealed class Tracked
        {
            public Tracked(string sku, string token, PlayOrderState state, DateTime? purchasedUtc)
            {
                Sku = sku;
                Token = token;
                State = state;
                Transaction = new BillingTransaction(sku, token, purchasedUtc); // el plazo del deferred_ack cuenta desde purchaseTime
            }

            public string Sku { get; }

            public string Token { get; }

            public PlayOrderState State { get; set; }

            /// <summary>Llegó por una consulta con la hoja de su SKU abierta: no se ofrece a la cola hasta que la hoja responda.</summary>
            public bool AwaitingOpen { get; set; }

            public BillingTransaction Transaction { get; }
        }

        private sealed class Open
        {
            public Open(string sku, Action<BillingResult> onDone)
            {
                Sku = sku;
                OnDone = onDone;
            }

            public string Sku { get; }

            public Action<BillingResult> OnDone { get; }
        }
    }
}
