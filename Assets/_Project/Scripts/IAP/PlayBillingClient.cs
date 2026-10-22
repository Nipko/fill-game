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

        /// <summary>Sin Play (sin conexión, sin Play Store o la consulta falló): no se vende; <see cref="PlayBillingClient.Refresh"/> reintenta.</summary>
        Unavailable = 3,
    }

    /// <summary>¿Puede la tienda de la plataforma cobrar ya? (<see cref="BillingReadyStoreService"/>).</summary>
    public interface IBillingAvailability
    {
        /// <summary>Conectada, con precios y con las compras sin terminar ya consultadas.</summary>
        bool IsReady { get; }
    }

    /// <summary>
    /// <see cref="IBillingClient"/> real sobre Play Billing (Unity IAP 5.4.4; D-040; 08 §7 #3 y §10.3), a través de un
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
    /// <item><see cref="Finish"/> hace <c>ConfirmPurchase</c> (consume o reconoce) solo de un pedido que tiene; nunca antes de acreditar
    /// (lo garantiza quien llama) y nunca dos veces.</item>
    /// <item><see cref="AcknowledgeLocal"/> (<c>deferred_ack</c>): con un no consumible, <c>ConfirmPurchase</c> (que solo reconoce); con
    /// un consumible Unity IAP 5.4.4 no permite reconocer sin consumir, así que no hace nada y lo cuenta
    /// (<see cref="AcknowledgeUnsupported"/>): pregunta Q-IAP-2 de <c>docs/dev/iap.md</c>.</item>
    /// <item>Precios: los de la tienda (importe y moneda; D-025, punto 3 [P]); mientras no lleguen, o si la moneda no se puede escribir,
    /// los de 05 §4.2 en <c>fallbackCurrency</c>. Mientras no esté lista no se vende (<see cref="BillingReadyStoreService"/>).</item>
    /// </list>
    /// Nunca escribe tokens ni el UID en los logs. Hilo principal.
    /// </summary>
    public sealed class PlayBillingClient : IBillingClient, IBillingAvailability
    {
        private readonly IPlayStorePort _port;
        private readonly StoreCatalog _catalog;
        private readonly string _fallbackCurrency;
        private readonly Dictionary<string, LocalPrice> _prices = new Dictionary<string, LocalPrice>(StringComparer.Ordinal);
        private readonly List<Tracked> _unfinished = new List<Tracked>();
        private readonly HashSet<string> _finished = new HashSet<string>(StringComparer.Ordinal);
        private Open? _open;
        private bool _fetchingPurchases;

        /// <param name="port">Puerto de Play (Unity IAP en el teléfono; uno falso en los tests).</param>
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

        /// <summary>Veces que se pidió un <c>deferred_ack</c> de un consumible, que Unity IAP 5.4.4 no permite (Q-IAP-2).</summary>
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
            if (State == PlayBillingState.Starting || State == PlayBillingState.Ready)
            {
                return;
            }

            SetState(PlayBillingState.Starting);
            _port.Connect(connected =>
            {
                if (!connected)
                {
                    SetState(PlayBillingState.Unavailable);
                    return;
                }

                _port.FetchProducts(Definitions(), OnProducts);
            });
        }

        /// <summary>
        /// Al volver a primer plano: si no hay Play, vuelve a intentarlo; si la hay, consulta otra vez las compras (lo que Play cobró
        /// mientras tanto).
        /// </summary>
        public void Refresh()
        {
            if (State == PlayBillingState.Unavailable || State == PlayBillingState.NotStarted)
            {
                Start();
                return;
            }

            if (State == PlayBillingState.Ready)
            {
                FetchPurchases(null);
            }
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

        /// <summary>¿El precio de <paramref name="sku"/> es el de la tienda (y no el de respaldo de 05 §4.2)?</summary>
        public bool HasStorePrice(string sku) => sku != null && _prices.ContainsKey(sku);

        /// <inheritdoc />
        public void Purchase(string sku, string? obfuscatedAccountId, Action<BillingResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            _catalog.Get(sku);
            if (State != PlayBillingState.Ready || _open != null)
            {
                onDone(BillingResult.Failed()); // sin Play o con otra hoja abierta: no se cobra nada
                return;
            }

            _open = new Open(sku, onDone);
            _port.Purchase(sku, ContractRules.IsObfuscatedAccountId(obfuscatedAccountId) ? obfuscatedAccountId : null);
        }

        /// <inheritdoc />
        public IReadOnlyList<BillingTransaction> UnfinishedTransactions()
        {
            var list = new BillingTransaction[_unfinished.Count];
            for (int i = 0; i < _unfinished.Count; i++)
            {
                list[i] = _unfinished[i].Transaction;
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
                // ConfirmPurchase consume o reconoce según el tipo registrado (TypeOf), que coincide con consume (VerifiedStoreService).
                // Si el puerto ya no tiene el pedido, sigue sin terminar en Play y vuelve en la próxima sesión (ya acreditado: solo se termina).
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

            if (TypeOf(product) == PlayProductType.NonConsumable)
            {
                // Reconocer un no consumible es ConfirmPurchase: Play ya no lo reembolsa y sigue en la cola hasta que el servidor responda.
                if (_port.Confirm(token))
                {
                    t.State = PlayOrderState.Confirmed;
                }

                return;
            }

            AcknowledgeUnsupported++; // Q-IAP-2: Unity IAP 5.4.4 no reconoce un consumible sin consumirlo; no se consume sin acreditar
        }

        private void OnProducts(IReadOnlyList<PlayProductInfo>? products)
        {
            if (products == null)
            {
                SetState(PlayBillingState.Unavailable);
                return;
            }

            _prices.Clear();
            for (int i = 0; i < products.Count; i++)
            {
                PlayProductInfo info = products[i];
                if (info == null || _catalog.Find(info.Sku) == null || info.PriceMicros < 0)
                {
                    continue;
                }

                if (TryLocalPrice(info.Currency, info.PriceMicros, out LocalPrice price))
                {
                    _prices[info.Sku] = price;
                }
            }

            MissingPrices = _catalog.Products.Count - _prices.Count;
            FetchPurchases(ok => SetState(ok ? PlayBillingState.Ready : PlayBillingState.Unavailable));
        }

        private void FetchPurchases(Action<bool>? onDone)
        {
            if (_fetchingPurchases)
            {
                onDone?.Invoke(true);
                return;
            }

            _fetchingPurchases = true;
            _port.FetchPurchases(orders =>
            {
                _fetchingPurchases = false;
                if (orders != null)
                {
                    for (int i = 0; i < orders.Count; i++)
                    {
                        if (orders[i] != null)
                        {
                            Accept(orders[i]);
                        }
                    }
                }

                onDone?.Invoke(orders != null);
            });
        }

        private void OnOrderUpdated(PlayOrder order)
        {
            if (order != null)
            {
                Accept(order);
            }
        }

        private void Accept(PlayOrder order)
        {
            StoreProduct? product = _catalog.Find(order.Sku);
            bool validToken = ContractRules.IsPurchaseToken(order.Token);
            if (product == null || !validToken)
            {
                // Un producto que esta versión no vende, o un token que no cumple el contrato: no se verifica. Si era la compra abierta,
                // falla (si se cobró, Play la devolverá sin terminar y la versión que la venda la terminará).
                if (_open != null && string.Equals(_open.Sku, order.Sku, StringComparison.Ordinal) && order.State != PlayOrderState.Confirmed)
                {
                    Resolve(BillingResult.Failed());
                }

                return;
            }

            switch (order.State)
            {
                case PlayOrderState.PaymentPending:
                    if (IsOpenFor(order.Sku))
                    {
                        Resolve(BillingResult.Pending());
                    }

                    return;

                case PlayOrderState.Confirmed:
                    if (TypeOf(product) != PlayProductType.NonConsumable || _finished.Contains(order.Token) || Find(order.Token) != null)
                    {
                        return; // un consumible siempre vuelve como cobrado; lo ya visto en esta sesión, una vez
                    }

                    _unfinished.Add(new Tracked(order.Sku, order.Token, PlayOrderState.Confirmed));
                    UnfinishedChanged?.Invoke();
                    return;

                default:
                    if (_finished.Contains(order.Token) || Find(order.Token) != null)
                    {
                        return; // Play lo repite (consulta y aviso a la vez): un token, una vez
                    }

                    var t = new Tracked(order.Sku, order.Token, PlayOrderState.Purchased);
                    _unfinished.Add(t);
                    if (IsOpenFor(order.Sku))
                    {
                        Resolve(BillingResult.Purchased(t.Transaction));
                    }
                    else
                    {
                        UnfinishedChanged?.Invoke();
                    }

                    return;
            }
        }

        private void OnPurchaseFailed(string? sku, PlayFailureKind kind)
        {
            if (_open == null || (sku != null && !string.Equals(sku, _open.Sku, StringComparison.Ordinal)))
            {
                return;
            }

            Resolve(kind == PlayFailureKind.Cancelled ? BillingResult.Cancelled() : BillingResult.Failed());
            if (kind == PlayFailureKind.AlreadyOwned && State == PlayBillingState.Ready)
            {
                FetchPurchases(null); // la compra anterior sin terminar vuelve y la termina la cola
            }
        }

        private bool IsOpenFor(string sku) => _open != null && string.Equals(_open.Sku, sku, StringComparison.Ordinal);

        private void Resolve(BillingResult result)
        {
            Open? open = _open;
            if (open == null)
            {
                return;
            }

            _open = null;
            open.OnDone(result);
        }

        private void SetState(PlayBillingState state)
        {
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
        /// admite todavía): entonces vale el precio de respaldo.
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
            public Tracked(string sku, string token, PlayOrderState state)
            {
                Sku = sku;
                Token = token;
                State = state;
                Transaction = new BillingTransaction(sku, token);
            }

            public string Sku { get; }

            public string Token { get; }

            public PlayOrderState State { get; set; }

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
