#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.Backend.Verification;
using AF.Economy;
using AF.Platform;

namespace AF.IAP
{
    /// <summary>
    /// En qué punto está la verificación de una compra (08 §10.3; F3-3). Va en <see cref="PurchaseVerificationEvent"/> para la UI
    /// («Confirmando…») y la analítica (<c>af_iap_result</c>, 08 §8.2), sin el token.
    /// </summary>
    public enum PurchaseVerificationState
    {
        /// <summary><c>verifying</c>: Play la cobró y se está preguntando al servidor.</summary>
        Verifying = 0,

        /// <summary><c>ok</c> o <c>duplicate</c> nueva para este perfil: acreditada (una vez) y terminada en la tienda.</summary>
        Credited = 1,

        /// <summary><c>ok</c> o <c>duplicate</c> de una compra ya acreditada (o de algo que ya se tenía): solo se termina en la tienda.</summary>
        AlreadyApplied = 2,

        /// <summary><c>pending</c>: Play aún no la da por pagada; no se acredita y se vuelve a preguntar.</summary>
        Pending = 3,

        /// <summary>Fallo pasajero: queda en la cola de 72 h con su espera; no se acredita.</summary>
        Queued = 4,

        /// <summary><c>verify_failed</c>: 400 o 409; ni se acredita ni se consume.</summary>
        Rejected = 5,

        /// <summary><c>deferred_ack</c>: 72 h sin respuesta; <c>acknowledge</c> local, sin acreditar (sigue en la cola).</summary>
        DeferredAck = 6,

        /// <summary>El servidor la verificó pero no se pudo acreditar (la cartera la rechazó o la respuesta no cuadra): no se termina.</summary>
        CreditFailed = 7,
    }

    /// <summary>Un cambio en la verificación de una compra. No lleva el token ni el UID: se puede registrar.</summary>
    public sealed class PurchaseVerificationEvent
    {
        internal PurchaseVerificationEvent(string sku, PurchaseVerificationState state, BackendError? error, int unknownGrantItems, bool isLive)
        {
            Sku = sku;
            State = state;
            Error = error;
            UnknownGrantItems = unknownGrantItems;
            IsLive = isLive;
        }

        public string Sku { get; }

        /// <summary>
        /// Es de la compra que el jugador acaba de abrir y cuyo <c>onDone</c> aún no se llamó (F3-5): su resultado llega también por
        /// <see cref="IStoreService.Purchase"/>. False: una compra que vuelve de la cola o del arranque (<see cref="VerifiedStoreService.Resume"/>,
        /// <see cref="VerifiedStoreService.Pump"/>), que solo se conoce por este evento (la analítica y la UI la tratan aparte).
        /// </summary>
        public bool IsLive { get; }

        public PurchaseVerificationState State { get; }

        /// <summary>El error (con <see cref="PurchaseVerificationState.Queued"/> y <see cref="PurchaseVerificationState.Rejected"/>).</summary>
        public BackendError? Error { get; }

        /// <summary>Ítems del <c>grant</c> que este cliente no conoce y no acreditó.</summary>
        public int UnknownGrantItems { get; }

        public override string ToString() => "PurchaseVerification(" + Sku + ", " + State + ")";
    }

    /// <summary>
    /// Tienda con compra verificada (F3-3; 08 §10.3; D-035): <see cref="IBillingClient"/> cobra, el backend verifica
    /// (<see cref="IBackendClient.VerifyPurchase"/> por la <see cref="VerificationQueue"/> de 72 h) y solo entonces
    /// <see cref="StoreLedger"/> acredita, <b>una sola vez por</b> <c>token_hash</c> (<c>applied_purchases</c>):
    /// <list type="bullet">
    /// <item><c>ok</c>: acredita el <c>grant</c> del servidor si el hash no está aplicado; luego consume (consumibles) o confirma.</item>
    /// <item><c>duplicate</c>: acredita el contenido del catálogo solo si el hash no está aplicado (otra instalación, o se cerró
    /// la app antes de anotarlo); luego consume o confirma.</item>
    /// <item><c>pending</c>: nada; se queda en la cola y se vuelve a preguntar.</item>
    /// <item>400 o 409: nada, y tampoco se consume ni se confirma (consumir también reconoce: Play ya no reembolsaría el segundo
    /// <c>af_starter</c>; plan §9, F2c).</item>
    /// <item>Fallo pasajero: se queda en la cola con espera; a las 72 h (<c>sec_purchase_deferred_ack_h</c>) se hace
    /// <c>acknowledge</c> local y <c>deferred_ack</c> <b>sin acreditar</b>, y se sigue reintentando hasta que el servidor la verifique
    /// (D-035, añadido de F3-1 y F3-2).</item>
    /// </list>
    /// La cola vive en el perfil (<c>purchase_queue</c> [P]) y se guarda con lo acreditado en la misma escritura; las compras
    /// cobradas sin terminar vuelven de la tienda en cada arranque (<see cref="Resume"/>). <c>setObfuscatedAccountId</c> = UID cuando lo
    /// hay. <c>biz_backend_enabled</c> [P] (<see cref="IBackendSwitch"/>) apagado: no se vende ni se llama al backend. Hilo principal.
    /// </summary>
    public sealed class VerifiedStoreService : IStoreService
    {
        private readonly StoreLedger _ledger;
        private readonly IBillingClient _billing;
        private readonly IClock _clock;
        private readonly IBackendSwitch _switch;
        private readonly IPlayerUidProvider? _uid;
        private readonly VerificationQueue _queue;
        private readonly VerificationQueueRunner _runner;
        private readonly Dictionary<string, Waiting> _waiting = new Dictionary<string, Waiting>(StringComparer.Ordinal);
        private readonly HashSet<string> _rejectedThisSession = new HashSet<string>(StringComparer.Ordinal);

        // SKU de la compra abierta en la tienda que aún no respondió: Resume no encola sus transacciones (las encola la respuesta,
        // con las semillas del Cántaro y la Cuia de rescate al momento).
        private string? _purchaseInStore;

        /// <param name="ledger">Registro de la tienda (perfil y cartera).</param>
        /// <param name="billing">Tienda de la plataforma (Unity IAP o <see cref="FakeBillingClient"/>).</param>
        /// <param name="backend">Cliente del backend (<see cref="HttpBackendClient"/> o <see cref="FakeBackendClient"/>).</param>
        /// <param name="clock">Reloj (el plazo de 72 h se mide con el del dispositivo).</param>
        /// <param name="backendSwitch"><c>biz_backend_enabled</c>; null: el default (<c>true</c>).</param>
        /// <param name="uid">UID de Firebase Auth para <c>setObfuscatedAccountId</c>; null: sin UID.</param>
        /// <param name="queueConfig"><c>sec_purchase_deferred_ack_h</c>; null: el default de 08 §9.</param>
        public VerifiedStoreService(
            StoreLedger ledger,
            IBillingClient billing,
            IBackendClient backend,
            IClock clock,
            IBackendSwitch? backendSwitch = null,
            IPlayerUidProvider? uid = null,
            VerificationQueueConfig? queueConfig = null)
        {
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _billing = billing ?? throw new ArgumentNullException(nameof(billing));
            if (backend == null)
            {
                throw new ArgumentNullException(nameof(backend));
            }

            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _switch = backendSwitch ?? new BackendSwitch();
            _uid = uid;

            // La cola guardada: si no se entiende, vacía (lo cobrado sin terminar vuelve de la tienda en Resume; nada se acredita sin
            // el servidor).
            string? saved = _ledger.Profile.PurchaseQueue;
            if (saved == null || !VerificationQueue.TryFromJson(saved, out VerificationQueue? loaded, out _, queueConfig))
            {
                loaded = new VerificationQueue(queueConfig);
            }

            _queue = loaded!;
            _runner = new VerificationQueueRunner(_queue, backend, _clock, OnOutcome, OnQueueChanged);
        }

        /// <summary>Cada cambio en la verificación de una compra (para la UI y la analítica).</summary>
        public event Action<PurchaseVerificationEvent>? VerificationChanged;

        /// <inheritdoc />
        public bool IsAvailable => _ledger.Config.BizIapEnabled && _switch.BizBackendEnabled;

        /// <inheritdoc />
        public bool IsPurchasing { get; private set; }

        /// <inheritdoc />
        public StoreCatalog Catalog => _ledger.Catalog;

        /// <summary>Compras cobradas que esperan al servidor (cola de 72 h, también las ya reconocidas en local).</summary>
        public IReadOnlyList<PendingVerification> PendingVerifications => _queue.Entries;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku)
        {
            return _billing.PriceOf(Catalog.Get(sku));
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

            if ((product.Kind != ProductKind.Consumable || product.Content.PiggySeeds) && IsQueued(product.Sku))
            {
                // Una Mochila, Sin anuncios o una skin ya pagada que espera al servidor: no se cobra otra (el segundo af_starter
                // daría 400 y Play tardaría 3 días en reembolsarlo). Tampoco otro Cántaro: el pendiente se lleva lo que guarda y el
                // Cántaro no se vacía hasta acreditarlo (Q-B5-12); otro cobraría las mismas semillas dos veces (F3-5).
                onDone(new PurchaseResult(request, product, PurchaseStatus.Pending));
                return;
            }

            string? accountId = AccountId();
            bool closed = false;
            IsPurchasing = true;
            _purchaseInStore = product.Sku;
            _billing.Purchase(product.Sku, accountId, billing =>
            {
                if (closed)
                {
                    return;
                }

                closed = true;
                _purchaseInStore = null;
                OnBillingResult(request, product, accountId, billing, onDone);
            });
        }

        /// <summary>
        /// Al arrancar y al volver a primer plano (08 §10.3: «reintento en cada arranque»): las compras cobradas sin terminar que ya
        /// están en <c>applied_purchases</c> se terminan sin acreditar; las demás entran en la cola; después, <see cref="Pump"/>.
        /// </summary>
        public void Resume()
        {
            IReadOnlyList<BillingTransaction> unfinished = _billing.UnfinishedTransactions();
            bool changed = false;
            DateTime now = _clock.UtcNow;
            for (int i = 0; i < unfinished.Count; i++)
            {
                BillingTransaction t = unfinished[i];
                StoreProduct? product = Catalog.Find(t.Sku);
                if (product == null || !ContractRules.IsPurchaseToken(t.Token) || !ContractRules.IsSku(t.Sku))
                {
                    continue; // un producto que esta versión no vende: se deja como está
                }

                if (_queue.Contains(t.Token) || _waiting.ContainsKey(t.Token) || _rejectedThisSession.Contains(t.Token))
                {
                    continue;
                }

                if (string.Equals(t.Sku, _purchaseInStore, StringComparison.Ordinal))
                {
                    continue; // la de la hoja de Play abierta: la encola su respuesta, con su contexto
                }

                if (_ledger.Profile.HasAppliedPurchase(PurchaseTokenHash.Of(t.Token)))
                {
                    // Acreditada en una sesión anterior que se cerró antes de consumirla: solo se termina, y solo con el hash ya en
                    // disco (si no, Play ya no la devolvería y se perdería).
                    if (_ledger.Flush())
                    {
                        _billing.Finish(t.Sku, t.Token, Consumes(product));
                        Raise(t.Sku, PurchaseVerificationState.AlreadyApplied, false);
                    }

                    continue;
                }

                // Sin las semillas del Cántaro (solo se pierden si la app se cerró entre el cobro y el primer guardado): el mínimo.
                changed |= _queue.Enqueue(new VerifyPurchaseRequest(t.Token, t.Sku, AccountId()), now, t.PurchasedUtc);
            }

            if (changed)
            {
                OnQueueChanged();
            }

            Pump();
        }

        /// <summary>Reintenta lo que toca de la cola y anota el <c>deferred_ack</c> de lo vencido. Se puede llamar a menudo.</summary>
        public void Pump()
        {
            _runner.Pump(_switch.BizBackendEnabled);
        }

        private void OnBillingResult(PurchaseRequest request, StoreProduct product, string? accountId, BillingResult billing, Action<PurchaseResult> onDone)
        {
            switch (billing.Kind)
            {
                case BillingResultKind.Cancelled:
                    IsPurchasing = false;
                    if (product.Content.AppliesRescue)
                    {
                        _ledger.RecordBalsaDeclined();
                    }

                    onDone(new PurchaseResult(request, product, PurchaseStatus.Cancelled));
                    return;
                case BillingResultKind.Failed:
                    IsPurchasing = false;
                    onDone(new PurchaseResult(request, product, PurchaseStatus.Failed));
                    return;
                case BillingResultKind.Pending:
                    // Play aún no la cobró: nada que verificar. Cuando la cobre, vuelve como compra sin terminar (Resume).
                    IsPurchasing = false;
                    Raise(product.Sku, PurchaseVerificationState.Pending, true);
                    onDone(new PurchaseResult(request, product, PurchaseStatus.Pending));
                    return;
            }

            BillingTransaction t = billing.Transaction!;
            if (!string.Equals(t.Sku, product.Sku, StringComparison.Ordinal) || !ContractRules.IsPurchaseToken(t.Token))
            {
                IsPurchasing = false; // la tienda devolvió otra cosa: no se acredita nada; si se cobró, vuelve en Resume
                onDone(new PurchaseResult(request, product, PurchaseStatus.Failed));
                return;
            }

            _waiting[t.Token] = new Waiting(request, product, onDone);
            if (!_queue.Contains(t.Token))
            {
                int? clientSeeds = product.Content.PiggySeeds ? request.PiggySeeds : (int?)null;
                _queue.Enqueue(new VerifyPurchaseRequest(t.Token, t.Sku, accountId), _clock.UtcNow, t.PurchasedUtc, clientSeeds);
                OnQueueChanged(); // el token, guardado antes de preguntar: si la app se cierra, la compra sigue en la cola
            }

            Raise(product.Sku, PurchaseVerificationState.Verifying, true);
            Pump();

            // Sin respuesta ya y sin petición en vuelo (backend apagado, o la entrada esperaba su turno): queda en la cola.
            if (_waiting.ContainsKey(t.Token) && !_runner.IsInFlight(t.Token))
            {
                Complete(t.Token, PurchaseStatus.Pending, null);
            }
        }

        private void OnOutcome(VerificationOutcome outcome)
        {
            string token = outcome.Entry.Request.Token;
            string sku = outcome.Entry.Request.Sku;
            StoreProduct? product = Catalog.Find(sku);

            // Lo que se acredite sale en la misma escritura que la cola ya actualizada.
            _ledger.Profile.PurchaseQueue = _queue.ToJson();
            bool live = _waiting.ContainsKey(token);
            switch (outcome.Kind)
            {
                case VerificationOutcomeKind.Answered:
                    OnAnswered(token, sku, product, outcome.Entry, outcome.Response!);
                    return;
                case VerificationOutcomeKind.Rejected:
                    // 400 o 409: ni acreditar ni consumir ni confirmar (plan §9, F2c). No se reenvía en esta sesión.
                    _rejectedThisSession.Add(token);
                    Raise(sku, PurchaseVerificationState.Rejected, live, outcome.Error);
                    Complete(token, PurchaseStatus.VerifyFailed, null);
                    return;
                case VerificationOutcomeKind.DeferredAck:
                    // 72 h sin respuesta: acknowledge local para que Play no la reembolse; NO se acredita (D-035, F3-1 y F3-2).
                    _billing.AcknowledgeLocal(sku, token);
                    Raise(sku, PurchaseVerificationState.DeferredAck, live);
                    Complete(token, PurchaseStatus.Pending, null);
                    return;
                default:
                    Raise(sku, PurchaseVerificationState.Queued, live, outcome.Error);
                    Complete(token, PurchaseStatus.Pending, null);
                    return;
            }
        }

        private void OnAnswered(string token, string sku, StoreProduct? product, PendingVerification entry, VerifyPurchaseResponse response)
        {
            bool live = _waiting.ContainsKey(token);
            if (response.Status == VerifyStatus.Pending)
            {
                Raise(sku, PurchaseVerificationState.Pending, live);
                Complete(token, PurchaseStatus.Pending, null);
                return;
            }

            string hash = PurchaseTokenHash.Of(token);
            if (product == null || !string.Equals(response.TokenHash, hash, StringComparison.Ordinal))
            {
                // La respuesta no es de esta compra (o el SKU ya no existe): no se acredita ni se termina; vuelve a la cola.
                Requeue(entry, "token_hash");
                Raise(sku, PurchaseVerificationState.CreditFailed, live);
                Complete(token, PurchaseStatus.Failed, null);
                return;
            }

            VerifiedCredit credit = _ledger.CreditVerified(
                product,
                hash,
                response.Status == VerifyStatus.Ok ? response.Grant : null,
                response.Status == VerifyStatus.Ok ? response.Entitlements : null,
                entry.ClientSeeds,
                live);
            if (!credit.IsSettled)
            {
                // Sin acreditar no se termina: vuelve a la cola, con su contexto, y se reintenta con espera.
                Requeue(entry, "credit");
                Raise(sku, PurchaseVerificationState.CreditFailed, live, null, credit.UnknownItems);
                Complete(token, PurchaseStatus.Failed, credit.Transaction);
                return;
            }

            // Ya en applied_purchases: se consume o se confirma en la tienda solo si está en disco. Si el guardado falló, queda sin
            // terminar y Resume la termina cuando se guarde (sin volver a acreditarla).
            if (_ledger.Flush())
            {
                _billing.Finish(sku, token, Consumes(product));
            }

            if (credit.Kind == VerifiedCreditKind.Credited)
            {
                Raise(sku, PurchaseVerificationState.Credited, live, null, credit.UnknownItems);
                Complete(token, PurchaseStatus.Purchased, credit.Transaction);
            }
            else
            {
                Raise(sku, PurchaseVerificationState.AlreadyApplied, live);
                Complete(token, PurchaseStatus.AlreadyOwned, null);
            }
        }

        /// <summary>Devuelve a la cola una compra verificada que no se pudo acreditar, con su contexto y una espera.</summary>
        private void Requeue(PendingVerification entry, string reason)
        {
            var error = new BackendError(BackendErrorKind.InvalidResponse, 200, ApiErrorCode.Unknown, reason);
            if (_queue.Requeue(entry, error, _clock.UtcNow))
            {
                _ledger.Profile.PurchaseQueue = _queue.ToJson();
            }
        }

        /// <summary>
        /// ¿Espera al servidor (en la cola o con su respuesta en vuelo) la compra cuyo <c>token_hash</c> es <paramref name="tokenHash"/>?
        /// Lo usa <see cref="LedgerRestore"/> (F3-5) para no acreditar desde el libro mayor una compra de este dispositivo que la cola
        /// acreditará con su contexto (las semillas del Cántaro). No es un camino caliente.
        /// </summary>
        public bool IsAwaitingServer(string tokenHash)
        {
            if (tokenHash == null)
            {
                throw new ArgumentNullException(nameof(tokenHash));
            }

            for (int i = 0; i < _queue.Entries.Count; i++)
            {
                if (string.Equals(PurchaseTokenHash.Of(_queue.Entries[i].Request.Token), tokenHash, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (string token in _waiting.Keys)
            {
                if (string.Equals(PurchaseTokenHash.Of(token), tokenHash, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>El registro de la tienda (perfil, cartera y catálogo) sobre el que acredita.</summary>
        internal StoreLedger Ledger => _ledger;

        private bool IsQueued(string sku)
        {
            for (int i = 0; i < _queue.Entries.Count; i++)
            {
                if (string.Equals(_queue.Entries[i].Request.Sku, sku, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnQueueChanged()
        {
            _ledger.Profile.PurchaseQueue = _queue.ToJson();
            _ledger.SaveNow();
        }

        private void Complete(string token, PurchaseStatus status, TransactionResult? transaction)
        {
            if (!_waiting.TryGetValue(token, out Waiting? waiting))
            {
                return;
            }

            _waiting.Remove(token);
            IsPurchasing = false;
            waiting.OnDone(new PurchaseResult(waiting.Request, waiting.Product, status, transaction));
        }

        private void Raise(string sku, PurchaseVerificationState state, bool live, BackendError? error = null, int unknownItems = 0)
        {
            VerificationChanged?.Invoke(new PurchaseVerificationEvent(sku, state, error, unknownItems, live));
        }

        private string? AccountId()
        {
            string? uid = _uid?.Uid;
            return ContractRules.IsObfuscatedAccountId(uid) ? uid : null;
        }

        /// <summary>Los consumibles (también la Mochila, 1 por cuenta) se consumen; los no consumibles solo se confirman.</summary>
        private static bool Consumes(StoreProduct product) => product.Kind != ProductKind.NonConsumable;

        private sealed class Waiting
        {
            public Waiting(PurchaseRequest request, StoreProduct product, Action<PurchaseResult> onDone)
            {
                Request = request;
                Product = product;
                OnDone = onDone;
            }

            public PurchaseRequest Request { get; }

            public StoreProduct Product { get; }

            public Action<PurchaseResult> OnDone { get; }
        }
    }
}
