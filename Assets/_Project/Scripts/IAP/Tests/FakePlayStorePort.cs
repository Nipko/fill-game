#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.IAP.Tests
{
    /// <summary>Qué hace la hoja de Play de <see cref="FakePlayStorePort"/> con la próxima compra.</summary>
    public enum FakePlayOutcome
    {
        Purchase = 0,
        Cancel = 1,
        Fail = 2,
        PaymentPending = 3,
        AlreadyOwned = 4,
        Unavailable = 5,
        /// <summary>No responde (la hoja sigue abierta): <see cref="FakePlayStorePort.Respond"/> la cierra.</summary>
        Silent = 6,
    }

    /// <summary>Estado de un pedido en el lado de Google Play.</summary>
    internal enum FakePlayState
    {
        Purchased = 0,
        PaymentPending = 1,
        Consumed = 2,
        Acknowledged = 3,
    }

    /// <summary>
    /// La cuenta de Google Play que comparten las sesiones (lo que Play recuerda entre arranques e instalaciones): pedidos, su estado y
    /// el tipo de cada producto. Tokens deterministas (<c>play.token.0001</c>…), del formato del contrato.
    /// </summary>
    internal sealed class FakePlayAccount
    {
        private int _counter;

        public Dictionary<string, FakePlayEntry> Orders { get; } = new Dictionary<string, FakePlayEntry>(StringComparer.Ordinal);

        public List<string> OrderTokens { get; } = new List<string>();

        public Dictionary<string, PlayProductType> Types { get; } = new Dictionary<string, PlayProductType>(StringComparer.Ordinal);

        public static string Token(int n) => "play.token." + n.ToString("D4", CultureInfo.InvariantCulture);

        public FakePlayEntry Add(string sku, FakePlayState state, string? obfuscatedAccountId = null)
        {
            _counter++;
            var entry = new FakePlayEntry(sku, Token(_counter), state, obfuscatedAccountId);
            Orders[entry.Token] = entry;
            OrderTokens.Add(entry.Token);
            return entry;
        }

        public FakePlayState StateOf(string token) => Orders[token].State;

        public bool IsConsumable(string sku) => !Types.TryGetValue(sku, out PlayProductType t) || t == PlayProductType.Consumable;
    }

    internal sealed class FakePlayEntry
    {
        public FakePlayEntry(string sku, string token, FakePlayState state, string? obfuscatedAccountId)
        {
            Sku = sku;
            Token = token;
            State = state;
            ObfuscatedAccountId = obfuscatedAccountId;
        }

        public string Sku { get; }

        public string Token { get; }

        public FakePlayState State { get; set; }

        public string? ObfuscatedAccountId { get; }

        /// <summary>Consumible reconocido sin consumir (<c>deferred_ack</c>): sigue cobrado (<see cref="FakePlayState.Purchased"/>).</summary>
        public bool AcknowledgedOnly { get; set; }
    }

    /// <summary>
    /// <see cref="IPlayStorePort"/> falso, una sesión de la app sobre una <see cref="FakePlayAccount"/>: como Play Billing, devuelve en la
    /// consulta los pedidos cobrados sin consumir (también un consumible ya reconocido), los pendientes de pago y los no consumibles
    /// reconocidos, y <see cref="Confirm"/> solo termina un pedido cobrado que esta sesión vio. Por defecto responde en el acto; con
    /// <see cref="Async"/>, retiene las respuestas hasta <see cref="Flush"/>.
    /// </summary>
    internal sealed class FakePlayStorePort : IPlayStorePort
    {
        private readonly List<Action> _queued = new List<Action>();
        private readonly HashSet<string> _seenPurchased = new HashSet<string>(StringComparer.Ordinal);
        private string? _openSku;
        private string? _openAccountId;

        public FakePlayStorePort(FakePlayAccount? account = null)
        {
            Account = account ?? new FakePlayAccount();
        }

        public FakePlayAccount Account { get; }

        public bool ConnectOk { get; set; } = true;

        public bool FailProducts { get; set; }

        public bool FailPurchases { get; set; }

        /// <summary>Retiene las respuestas (conexión, productos, consultas y hoja de compra) hasta <see cref="Flush"/>.</summary>
        public bool Async { get; set; }

        public FakePlayOutcome NextOutcome { get; set; }

        /// <summary>Precios de la tienda (SKU → moneda y micros); sin entrada, el producto no está publicado.</summary>
        public Dictionary<string, KeyValuePair<string, long>> Prices { get; } = new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);

        /// <summary><see cref="Confirm"/> devuelve false sin terminar nada (la app se cierra antes de consumir).</summary>
        public bool CrashOnConfirm { get; set; }

        /// <summary><see cref="Confirm"/> se pide (true) pero Play no lo termina: el pedido sigue cobrado (el consumo asíncrono falla).</summary>
        public bool FailConfirm { get; set; }

        public List<KeyValuePair<string, string?>> Purchases { get; } = new List<KeyValuePair<string, string?>>();

        public List<string> Confirmed { get; } = new List<string>();

        /// <summary>Tokens que <see cref="Acknowledge"/> reconoció sin consumir.</summary>
        public List<string> Acknowledged { get; } = new List<string>();

        /// <summary>Se llama justo antes de terminar un pedido en Play (para mirar qué había en disco en ese momento).</summary>
        public Action<string>? OnConfirm { get; set; }

        public int FetchPurchasesCalls { get; private set; }

        public int ConnectCalls { get; private set; }

        public event Action<PlayOrder>? OrderUpdated;

        public event Action<string?, PlayFailureKind>? PurchaseFailed;

        public event Action? Disconnected;

        /// <summary>Play se desconecta.</summary>
        public void Disconnect() => Disconnected?.Invoke();

        /// <summary>Veces que se soltó el puerto.</summary>
        public int Disposed { get; private set; }

        public void Dispose() => Disposed++;

        /// <summary>¿Alguien oye todavía los avisos de Play?</summary>
        public bool HasListeners => OrderUpdated != null || PurchaseFailed != null || Disconnected != null;

        /// <summary>Precio en BRL (columna de 05 §4.2) para todo el catálogo salvo <paramref name="except"/>.</summary>
        public FakePlayStorePort PriceAll(StoreCatalog catalog, params string[] except)
        {
            for (int i = 0; i < catalog.Products.Count; i++)
            {
                StoreProduct p = catalog.Products[i];
                if (Array.IndexOf(except, p.Sku) < 0)
                {
                    Prices[p.Sku] = new KeyValuePair<string, long>("BRL", (long)(StorePrices.Local(p.UsdPrice, "BRL").Amount * 1000000m));
                }
            }

            return this;
        }

        public void Connect(Action<bool> onDone)
        {
            ConnectCalls++;
            Run(() => onDone(ConnectOk));
        }

        public void FetchProducts(IReadOnlyList<PlayProductDefinition> products, Action<IReadOnlyList<PlayProductInfo>?> onDone)
        {
            for (int i = 0; i < products.Count; i++)
            {
                Account.Types[products[i].Sku] = products[i].Type;
            }

            Run(() =>
            {
                if (FailProducts)
                {
                    onDone(null);
                    return;
                }

                var list = new List<PlayProductInfo>();
                for (int i = 0; i < products.Count; i++)
                {
                    if (Prices.TryGetValue(products[i].Sku, out KeyValuePair<string, long> price))
                    {
                        list.Add(new PlayProductInfo(products[i].Sku, price.Key, price.Value));
                    }
                }

                onDone(list);
            });
        }

        public void FetchPurchases(Action<IReadOnlyList<PlayOrder>?> onDone)
        {
            FetchPurchasesCalls++;
            Run(() => onDone(FailPurchases ? null : Snapshot()));
        }

        public void Purchase(string sku, string? obfuscatedAccountId)
        {
            Purchases.Add(new KeyValuePair<string, string?>(sku, obfuscatedAccountId));
            _openSku = sku;
            _openAccountId = obfuscatedAccountId;
            if (NextOutcome == FakePlayOutcome.Silent)
            {
                return;
            }

            FakePlayOutcome outcome = NextOutcome;
            Run(() => Respond(outcome));
        }

        /// <summary>Cierra la hoja de Play abierta con <paramref name="outcome"/>.</summary>
        public FakePlayEntry? Respond(FakePlayOutcome outcome)
        {
            string? sku = _openSku;
            _openSku = null;
            if (sku == null)
            {
                return null;
            }

            switch (outcome)
            {
                case FakePlayOutcome.Cancel:
                    PurchaseFailed?.Invoke(sku, PlayFailureKind.Cancelled);
                    return null;
                case FakePlayOutcome.Fail:
                    PurchaseFailed?.Invoke(sku, PlayFailureKind.Failed);
                    return null;
                case FakePlayOutcome.AlreadyOwned:
                    PurchaseFailed?.Invoke(sku, PlayFailureKind.AlreadyOwned);
                    return null;
                case FakePlayOutcome.Unavailable:
                    PurchaseFailed?.Invoke(sku, PlayFailureKind.Unavailable);
                    return null;
                case FakePlayOutcome.PaymentPending:
                    FakePlayEntry pending = Account.Add(sku, FakePlayState.PaymentPending, _openAccountId);
                    OrderUpdated?.Invoke(new PlayOrder(sku, pending.Token, PlayOrderState.PaymentPending));
                    return pending;
                default:
                    FakePlayEntry entry = Account.Add(sku, FakePlayState.Purchased, _openAccountId);
                    _seenPurchased.Add(entry.Token);
                    OrderUpdated?.Invoke(new PlayOrder(sku, entry.Token, PlayOrderState.Purchased));
                    return entry;
            }
        }

        public bool Confirm(string token)
        {
            if (CrashOnConfirm || !_seenPurchased.Contains(token) || !Account.Orders.TryGetValue(token, out FakePlayEntry? entry))
            {
                return false;
            }

            if (entry.State != FakePlayState.Purchased)
            {
                return false;
            }

            if (FailConfirm)
            {
                return true; // pedido, pero Play no lo terminó
            }

            OnConfirm?.Invoke(token);
            _seenPurchased.Remove(token);
            Confirmed.Add(token);
            entry.State = Account.IsConsumable(entry.Sku) ? FakePlayState.Consumed : FakePlayState.Acknowledged;
            return true;
        }

        /// <summary>
        /// Reconoce sin consumir un pedido cobrado que esta sesión vio (<c>acknowledgePurchase</c>): un consumible sigue cobrado; un no
        /// consumible queda reconocido.
        /// </summary>
        public bool Acknowledge(string token)
        {
            if (CrashOnConfirm || !_seenPurchased.Contains(token) || !Account.Orders.TryGetValue(token, out FakePlayEntry? entry)
                || entry.State != FakePlayState.Purchased)
            {
                return false;
            }

            Acknowledged.Add(token);
            if (Account.IsConsumable(entry.Sku))
            {
                entry.AcknowledgedOnly = true;
            }
            else
            {
                _seenPurchased.Remove(token);
                entry.State = FakePlayState.Acknowledged;
            }

            return true;
        }

        /// <summary>Play cobra un pedido pendiente de pago y avisa a la app abierta.</summary>
        public void Settle(string token)
        {
            FakePlayEntry entry = Account.Orders[token];
            entry.State = FakePlayState.Purchased;
            _seenPurchased.Add(token);
            OrderUpdated?.Invoke(new PlayOrder(entry.Sku, token, PlayOrderState.Purchased));
        }

        /// <summary>Play vuelve a avisar de un pedido que ya avisó.</summary>
        public void Redeliver(string token)
        {
            FakePlayEntry entry = Account.Orders[token];
            if (entry.State == FakePlayState.Purchased)
            {
                _seenPurchased.Add(token);
                OrderUpdated?.Invoke(new PlayOrder(entry.Sku, token, PlayOrderState.Purchased));
            }
        }

        /// <summary>Un aviso con un pedido arbitrario (SKU desconocido, token raro…).</summary>
        public void Push(PlayOrder order)
        {
            OrderUpdated?.Invoke(order);
        }

        /// <summary>Entrega las respuestas retenidas, en orden (las que se encolen mientras tanto, también).</summary>
        public void Flush()
        {
            while (_queued.Count > 0)
            {
                Action next = _queued[0];
                _queued.RemoveAt(0);
                next();
            }
        }

        public int QueuedCount => _queued.Count;

        private List<PlayOrder> Snapshot()
        {
            var list = new List<PlayOrder>();
            for (int i = 0; i < Account.OrderTokens.Count; i++)
            {
                FakePlayEntry e = Account.Orders[Account.OrderTokens[i]];
                switch (e.State)
                {
                    case FakePlayState.Purchased:
                        _seenPurchased.Add(e.Token);
                        list.Add(new PlayOrder(e.Sku, e.Token, PlayOrderState.Purchased));
                        break;
                    case FakePlayState.PaymentPending:
                        list.Add(new PlayOrder(e.Sku, e.Token, PlayOrderState.PaymentPending));
                        break;
                    case FakePlayState.Acknowledged:
                        list.Add(new PlayOrder(e.Sku, e.Token, PlayOrderState.Confirmed));
                        break;
                }
            }

            return list;
        }

        private void Run(Action action)
        {
            if (Async)
            {
                _queued.Add(action);
                return;
            }

            action();
        }
    }
}
