#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Platform;

namespace AF.IAP
{
    /// <summary>Qué hace <see cref="FakeBillingClient"/> al confirmar una compra.</summary>
    public enum FakeBillingMode
    {
        /// <summary>Play la cobra (<c>PURCHASED</c>).</summary>
        Purchase = 0,

        /// <summary>El jugador cancela.</summary>
        Cancel = 1,

        /// <summary>Falla el pago.</summary>
        Fail = 2,

        /// <summary>Queda pendiente de pago en Play (<c>PENDING</c>) hasta <see cref="FakeBillingClient.SettlePending"/>.</summary>
        Pending = 3,
    }

    /// <summary>
    /// <see cref="IBillingClient"/> sin Play Billing ni red (editor, tests y builds sin SDK; D-008): simula lo que importa para no
    /// acreditar dos veces, que una compra cobrada y no terminada vuelve en cada arranque (<see cref="UnfinishedTransactions"/>) hasta
    /// que se consume o se confirma. Tokens deterministas (<c>fake.purchase.0001</c>…). Con una <see cref="ISimulatedPurchasePresenter"/>
    /// enseña la hoja «Compra simulada» antes de cobrar; sin ella, responde según <see cref="Mode"/>.
    /// </summary>
    public sealed class FakeBillingClient : IBillingClient
    {
        private readonly StoreCatalog _catalog;
        private readonly string _currency;
        private readonly ISimulatedPurchasePresenter? _presenter;
        private readonly IClock? _clock;
        private readonly List<BillingTransaction> _unfinished = new List<BillingTransaction>();
        private readonly List<BillingTransaction> _pendingInPlay = new List<BillingTransaction>();
        private readonly List<BillingTransaction> _ownedNonConsumables = new List<BillingTransaction>();
        private readonly List<Action> _held = new List<Action>();
        private readonly string _tokenPrefix;
        private int _counter;

        /// <param name="catalog">Catálogo (precios y tipo de producto).</param>
        /// <param name="currency">Moneda de los precios.</param>
        /// <param name="presenter">Hoja «Compra simulada», o null para responder sin hoja.</param>
        /// <param name="clock">Hora de compra de las transacciones; sin reloj, sin hora.</param>
        /// <param name="tokenPrefix">
        /// Comienzo de cada token (<see cref="DefaultTokenPrefix"/> en los tests, deterministas). El juego le pone uno distinto en cada
        /// arranque (F3-5): <c>applied_purchases</c> guarda el <c>token_hash</c>, y con los tokens de siempre la primera compra de la
        /// sesión siguiente repetiría el de la anterior y no se acreditaría.
        /// </param>
        /// <exception cref="ArgumentException">El prefijo no forma tokens del contrato (<c>[A-Za-z0-9._:-]</c>).</exception>
        public FakeBillingClient(StoreCatalog catalog, string currency = "USD", ISimulatedPurchasePresenter? presenter = null, IClock? clock = null,
            string tokenPrefix = DefaultTokenPrefix)
        {
            if (!AF.Backend.Contract.ContractRules.IsPurchaseToken(tokenPrefix))
            {
                throw new ArgumentException("El prefijo no forma tokens del contrato.", nameof(tokenPrefix));
            }

            _tokenPrefix = tokenPrefix;
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            StorePrices.Local(StorePrices.UsdTiers[0], currency); // valida la moneda
            _currency = currency;
            _presenter = presenter;
            _clock = clock;
        }

        /// <summary>Comienzo de los tokens por defecto (<c>fake.purchase.0001</c>…).</summary>
        public const string DefaultTokenPrefix = "fake.purchase.";

        /// <summary>Qué pasa al comprar.</summary>
        public FakeBillingMode Mode { get; set; }

        /// <summary>Retiene la respuesta de cada compra hasta <see cref="ReleaseHeld"/> (para las carreras).</summary>
        public bool HoldResponses { get; set; }

        /// <summary>Ignora <see cref="Finish"/>: simula que la app se cierra justo después de acreditar y antes de consumir.</summary>
        public bool CrashOnFinish { get; set; }

        /// <summary>Compras pedidas (SKU y <c>obfuscatedAccountId</c>), en orden.</summary>
        public List<KeyValuePair<string, string?>> Requests { get; } = new List<KeyValuePair<string, string?>>();

        /// <summary>Tokens consumidos.</summary>
        public List<string> Consumed { get; } = new List<string>();

        /// <summary>Tokens confirmados sin consumir (no consumibles).</summary>
        public List<string> Confirmed { get; } = new List<string>();

        /// <summary>Tokens con <c>acknowledge</c> local (<c>deferred_ack</c>).</summary>
        public List<string> LocallyAcknowledged { get; } = new List<string>();

        /// <summary>Respuestas retenidas.</summary>
        public int HeldCount => _held.Count;

        /// <inheritdoc />
        public LocalPrice PriceOf(StoreProduct product)
        {
            return (product ?? throw new ArgumentNullException(nameof(product))).PriceIn(_currency);
        }

        /// <inheritdoc />
        public void Purchase(string sku, string? obfuscatedAccountId, Action<BillingResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            StoreProduct product = _catalog.Get(sku);
            Requests.Add(new KeyValuePair<string, string?>(sku, obfuscatedAccountId));
            if (_presenter == null)
            {
                Respond(product, true, onDone);
                return;
            }

            bool closed = false;
            _presenter.Present(new SimulatedPurchase(product, PriceOf(product)), confirmed =>
            {
                if (closed)
                {
                    return;
                }

                closed = true;
                Respond(product, confirmed, onDone);
            });
        }

        /// <inheritdoc />
        public IReadOnlyList<BillingTransaction> UnfinishedTransactions()
        {
            return _unfinished.ToArray();
        }

        /// <inheritdoc />
        public void Finish(string sku, string token, bool consume)
        {
            if (CrashOnFinish)
            {
                return;
            }

            BillingTransaction? t = Take(_unfinished, token);
            if (t == null)
            {
                return; // ya terminada: idempotente, como ConfirmPurchase sobre un pedido confirmado
            }

            if (consume)
            {
                Consumed.Add(token);
                return;
            }

            Confirmed.Add(token);
            _ownedNonConsumables.Add(t);
        }

        /// <inheritdoc />
        public void AcknowledgeLocal(string sku, string token)
        {
            if (!LocallyAcknowledged.Contains(token))
            {
                LocallyAcknowledged.Add(token);
            }
        }

        /// <summary>Entrega las respuestas retenidas, en orden (o en el inverso con <paramref name="reverse"/>).</summary>
        public void ReleaseHeld(bool reverse = false)
        {
            var held = new List<Action>(_held);
            _held.Clear();
            if (reverse)
            {
                held.Reverse();
            }

            for (int i = 0; i < held.Count; i++)
            {
                held[i]();
            }
        }

        /// <summary>Play cobra las compras pendientes de pago: pasan a compras sin terminar.</summary>
        public void SettlePending()
        {
            _unfinished.AddRange(_pendingInPlay);
            _pendingInPlay.Clear();
        }

        /// <summary>
        /// Instalación nueva con la misma cuenta de Google: Play vuelve a entregar los no consumibles ya confirmados (Restaurar
        /// compras), que vuelven a estar sin terminar en este dispositivo.
        /// </summary>
        public void RestoreOwnedOnNewDevice()
        {
            for (int i = 0; i < _ownedNonConsumables.Count; i++)
            {
                if (Find(_unfinished, _ownedNonConsumables[i].Token) == null)
                {
                    _unfinished.Add(_ownedNonConsumables[i]);
                }
            }
        }

        /// <summary>Añade una compra sin terminar con <paramref name="token"/> (p. ej. la de otro dispositivo o una ya anulada).</summary>
        public BillingTransaction AddUnfinished(string sku, string token, DateTime? purchasedUtc = null)
        {
            _catalog.Get(sku);
            var t = new BillingTransaction(sku, token, purchasedUtc);
            _unfinished.Add(t);
            return t;
        }

        /// <summary>¿Sigue sin terminar la compra de <paramref name="token"/>?</summary>
        public bool IsUnfinished(string token) => Find(_unfinished, token) != null;

        private void Respond(StoreProduct product, bool confirmed, Action<BillingResult> onDone)
        {
            BillingResult result;
            if (!confirmed || Mode == FakeBillingMode.Cancel)
            {
                result = BillingResult.Cancelled();
            }
            else if (Mode == FakeBillingMode.Fail)
            {
                result = BillingResult.Failed();
            }
            else
            {
                _counter++;
                var t = new BillingTransaction(
                    product.Sku, _tokenPrefix + _counter.ToString("D4", CultureInfo.InvariantCulture), _clock?.UtcNow);
                if (Mode == FakeBillingMode.Pending)
                {
                    _pendingInPlay.Add(t);
                    result = BillingResult.Pending();
                }
                else
                {
                    _unfinished.Add(t);
                    result = BillingResult.Purchased(t);
                }
            }

            if (HoldResponses)
            {
                _held.Add(() => onDone(result));
                return;
            }

            onDone(result);
        }

        private static BillingTransaction? Find(List<BillingTransaction> list, string token)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Token, token, StringComparison.Ordinal))
                {
                    return list[i];
                }
            }

            return null;
        }

        private static BillingTransaction? Take(List<BillingTransaction> list, string token)
        {
            BillingTransaction? t = Find(list, token);
            if (t != null)
            {
                list.Remove(t);
            }

            return t;
        }
    }
}
