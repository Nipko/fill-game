#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Backend.Contract;
using AF.Platform;

namespace AF.Backend
{
    /// <summary>
    /// <see cref="IBackendClient"/> para el editor, los tests y los builds de desarrollo sin URL base (08 §2.1, principio 6). Simula
    /// en memoria la semántica del servidor del contrato v1, sin red:
    /// <list type="bullet">
    /// <item><c>verifyPurchase</c>: <c>ok</c> con el <c>grant</c> de <see cref="GrantFor"/> la primera vez; <c>duplicate</c> si el
    /// token ya está en el libro mayor; <c>pending</c> si se marcó con <see cref="MarkPending"/>.</item>
    /// <item><c>restoreLedger</c>: las compras verificadas con su estado y los nonces que confirmó
    /// <see cref="ReceiveSsv"/> (el callback S2S de MAX).</item>
    /// <item><c>account/code</c>: un código nuevo en cada llamada (deterministas, del formato del contrato).</item>
    /// <item><c>account/delete</c>: vacía el estado; idempotente.</item>
    /// </list>
    /// <see cref="FailNext"/> hace fallar las llamadas siguientes con el error que se pida. Responde antes de volver, salvo
    /// <c>verifyPurchase</c> con <see cref="HoldVerifyResponses"/> (el servidor ya decidió, pero la respuesta llega cuando se suelta:
    /// para las carreras de F3-3).
    /// </summary>
    public sealed class FakeBackendClient : IBackendClient
    {
        private const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        private readonly IClock? _clock;
        private readonly Dictionary<string, LedgerEntry> _ledger = new Dictionary<string, LedgerEntry>(StringComparer.Ordinal);
        private readonly List<string> _ledgerOrder = new List<string>();
        private readonly HashSet<string> _pendingTokens = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _confirmedNonces = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<BackendError> _failures = new Queue<BackendError>();
        private readonly List<Action> _heldVerify = new List<Action>();
        private int _codeCounter;

        /// <param name="clock">Para el <c>ts</c> de las compras; sin reloj, 0.</param>
        public FakeBackendClient(IClock? clock = null)
        {
            _clock = clock;
        }

        /// <summary>
        /// Lo que concede una compra verificada. Por defecto, semillas 0 y sin ítems (las cifras reales las fija el servidor con
        /// el catálogo de 05 §4 y 08 §9).
        /// </summary>
        public Func<VerifyPurchaseRequest, PurchaseGrant> GrantFor { get; set; } = _ => new PurchaseGrant(0);

        /// <summary>Derechos que devuelve con <c>ok</c> (null: no los manda).</summary>
        public Func<VerifyPurchaseRequest, Entitlements?> EntitlementsFor { get; set; } = _ => null;

        /// <summary>Peticiones de <c>verifyPurchase</c> recibidas, en orden (llevan el token: solo para tests).</summary>
        public List<VerifyPurchaseRequest> VerifyRequests { get; } = new List<VerifyPurchaseRequest>();

        /// <summary>Retiene las respuestas de <c>verifyPurchase</c> hasta <see cref="ReleaseVerifyResponses"/>.</summary>
        public bool HoldVerifyResponses { get; set; }

        /// <summary>Respuestas de <c>verifyPurchase</c> retenidas.</summary>
        public int HeldVerifyResponses => _heldVerify.Count;

        /// <summary>Cambia las respuestas 200 de <c>verifyPurchase</c> antes de entregarlas (respuestas que no cuadran), o null.</summary>
        public Func<VerifyPurchaseResponse, VerifyPurchaseResponse>? RewriteVerify { get; set; }

        /// <summary>Llamadas recibidas por operación (<see cref="BackendOperation.Name"/>).</summary>
        public Dictionary<string, int> Calls { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Último código emitido, o null.</summary>
        public string? CurrentAccountCode { get; private set; }

        /// <summary>Veces que se borró la cuenta.</summary>
        public int Deletions { get; private set; }

        /// <summary>Compras del libro mayor (por <c>token_hash</c>).</summary>
        public int LedgerCount => _ledger.Count;

        /// <summary>Las siguientes <paramref name="times"/> llamadas fallan con <paramref name="error"/>.</summary>
        public void FailNext(BackendError error, int times = 1)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            for (int i = 0; i < times; i++)
            {
                _failures.Enqueue(error);
            }
        }

        /// <summary>Olvida los fallos pedidos con <see cref="FailNext"/> que aún no se usaron.</summary>
        public void ClearFailures() => _failures.Clear();

        /// <summary>Entrega las respuestas retenidas de <c>verifyPurchase</c>, en orden o en el inverso.</summary>
        public void ReleaseVerifyResponses(bool reverse = false)
        {
            var held = new List<Action>(_heldVerify);
            _heldVerify.Clear();
            if (reverse)
            {
                held.Reverse();
            }

            for (int i = 0; i < held.Count; i++)
            {
                held[i]();
            }
        }

        /// <summary>Play da la compra con <paramref name="token"/> como <c>PENDING</c> hasta <see cref="ClearPending"/>.</summary>
        public void MarkPending(string token) => _pendingTokens.Add(token ?? throw new ArgumentNullException(nameof(token)));

        public void ClearPending(string token) => _pendingTokens.Remove(token);

        /// <summary>
        /// Simula el callback SSV de AdMob (<c>/v1/ssv</c>, D-049): confirma el nonce. Un nonce repetido devuelve <c>duplicate</c>;
        /// sin identidad, <c>ignored</c> (la lista blanca de <c>ad_unit</c> y la firma solo las comprueba la Lambda).
        /// </summary>
        public SsvResponse ReceiveSsv(SsvCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!callback.HasIdentity)
            {
                return SsvResponse.Ignored; // sin user_id o custom_data, como la Lambda: nada que anotar
            }

            return _confirmedNonces.Add(callback.Nonce!) ? SsvResponse.Ok : SsvResponse.Duplicate;
        }

        /// <summary>
        /// Simula el job diario <c>voidedPurchasesJob</c> con una anulación de Play: marca <c>voided</c> la compra del token
        /// (08 §10.1). Devuelve el resumen del contrato.
        /// </summary>
        public VoidedPurchasesJobOutput RunVoidedPurchasesJob(IReadOnlyList<string> voidedTokens, long nowMs)
        {
            int voided = 0;
            for (int i = 0; i < voidedTokens.Count; i++)
            {
                string hash = PurchaseTokenHash.Of(voidedTokens[i]);
                if (_ledger.TryGetValue(hash, out LedgerEntry? entry) && entry.State == LedgerPurchaseState.Ok)
                {
                    entry.State = LedgerPurchaseState.Voided;
                    entry.VoidedAtMs = nowMs;
                    voided++;
                }
            }

            return new VoidedPurchasesJobOutput(false, 1, voided, nowMs);
        }

        public void VerifyPurchase(VerifyPurchaseRequest request, Action<BackendResult<VerifyPurchaseResponse>> onDone)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            VerifyRequests.Add(request);
            BackendResult<VerifyPurchaseResponse>? result = null;
            if (!Begin(BackendOperation.VerifyPurchase, (BackendResult<VerifyPurchaseResponse> r) => result = r))
            {
                result = BackendResult<VerifyPurchaseResponse>.Ok(Verify(request));
            }

            if (result!.IsOk && RewriteVerify != null)
            {
                result = BackendResult<VerifyPurchaseResponse>.Ok(RewriteVerify(result.Value!));
            }

            if (HoldVerifyResponses)
            {
                BackendResult<VerifyPurchaseResponse> held = result;
                _heldVerify.Add(() => onDone(held));
                return;
            }

            onDone(result);
        }

        private VerifyPurchaseResponse Verify(VerifyPurchaseRequest request)
        {
            if (_pendingTokens.Contains(request.Token))
            {
                return VerifyPurchaseResponse.Pending();
            }

            string hash = PurchaseTokenHash.Of(request.Token);
            if (_ledger.ContainsKey(hash))
            {
                return VerifyPurchaseResponse.Duplicate(hash);
            }

            _ledger[hash] = new LedgerEntry(request.Sku, NowMs());
            _ledgerOrder.Add(hash);
            return VerifyPurchaseResponse.Ok(hash, GrantFor(request), EntitlementsFor(request));
        }

        public void RestoreLedger(RestoreLedgerRequest request, Action<BackendResult<RestoreLedgerResponse>> onDone)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (Begin(BackendOperation.RestoreLedger, onDone))
            {
                return;
            }

            var purchases = new List<LedgerPurchase>(_ledgerOrder.Count);
            for (int i = 0; i < _ledgerOrder.Count; i++)
            {
                LedgerEntry e = _ledger[_ledgerOrder[i]];
                purchases.Add(new LedgerPurchase(_ledgerOrder[i], e.Sku, e.State, e.TsMs, e.VoidedAtMs));
            }

            var confirmed = new List<string>();
            for (int i = 0; i < request.PendingNonces.Count; i++)
            {
                if (_confirmedNonces.Contains(request.PendingNonces[i]))
                {
                    confirmed.Add(request.PendingNonces[i]);
                }
            }

            onDone(BackendResult<RestoreLedgerResponse>.Ok(new RestoreLedgerResponse(purchases, confirmed)));
        }

        public void GetAccountCode(Action<BackendResult<AccountCodeResponse>> onDone)
        {
            if (Begin(BackendOperation.GetAccountCode, onDone))
            {
                return;
            }

            _codeCounter++;
            CurrentAccountCode = MakeCode(_codeCounter);
            onDone(BackendResult<AccountCodeResponse>.Ok(new AccountCodeResponse(CurrentAccountCode)));
        }

        public void DeleteAccount(Action<BackendResult<DeleteAccountResponse>> onDone)
        {
            if (Begin(BackendOperation.DeleteAccount, onDone))
            {
                return;
            }

            // 08 §11: el libro mayor se conserva anonimizado en el servidor, pero ya no es de este UID.
            _ledger.Clear();
            _ledgerOrder.Clear();
            _confirmedNonces.Clear();
            _pendingTokens.Clear();
            CurrentAccountCode = null;
            Deletions++;
            onDone(BackendResult<DeleteAccountResponse>.Ok(DeleteAccountResponse.Deleted));
        }

        /// <summary>Cuenta la llamada y, si toca, falla. True si ya respondió.</summary>
        private bool Begin<T>(BackendOperation op, Action<BackendResult<T>> onDone)
            where T : class
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            Calls.TryGetValue(op.Name, out int n);
            Calls[op.Name] = n + 1;
            if (_failures.Count == 0)
            {
                return false;
            }

            onDone(BackendResult<T>.Fail(_failures.Dequeue()));
            return true;
        }

        private long NowMs()
        {
            if (_clock == null)
            {
                return 0;
            }

            long ms = (long)(_clock.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return ms < 0 ? 0 : ms;
        }

        /// <summary>Código del formato del contrato a partir de un contador: 26 símbolos de Crockford, el primero de 0 a 7.</summary>
        private static string MakeCode(int counter)
        {
            string hex = PurchaseTokenHash.Sha256Hex("fake-account-code:" + counter.ToString(CultureInfo.InvariantCulture));
            var chars = new char[26];
            for (int i = 0; i < 26; i++)
            {
                int nibbles = Convert.ToInt32(hex.Substring(i * 2, 2), 16);
                chars[i] = Crockford[nibbles & (i == 0 ? 7 : 31)];
            }

            return new string(chars);
        }

        private sealed class LedgerEntry
        {
            public LedgerEntry(string sku, long tsMs)
            {
                Sku = sku;
                TsMs = tsMs;
                State = LedgerPurchaseState.Ok;
            }

            public string Sku { get; }

            public long TsMs { get; }

            public LedgerPurchaseState State { get; set; }

            public long? VoidedAtMs { get; set; }
        }
    }
}
