#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;
using AF.Platform;

namespace AF.Backend.Verification
{
    /// <summary>Qué pasó con una compra de la cola.</summary>
    public enum VerificationOutcomeKind
    {
        /// <summary>
        /// El servidor respondió <c>ok</c>, <c>duplicate</c> o <c>pending</c> (ver <see cref="VerificationOutcome.Response"/>). Una
        /// <c>pending</c> también sale de la cola: Unity IAP mantiene la transacción y quien use la cola (F3-3) la vuelve a encolar
        /// cuando Play la entregue de nuevo (08 §10.3: «reintento en cada arranque»).
        /// </summary>
        Answered = 0,

        /// <summary>Error definitivo (<see cref="VerificationQueue.IsDefinitive"/>): no se reintenta.</summary>
        Rejected = 1,

        /// <summary>
        /// Cumplió <c>sec_purchase_deferred_ack_h</c> sin respuesta: toca <c>acknowledge</c> local y <c>deferred_ack</c> (08 §10.3).
        /// Si además se acredita lo decide F3-3 (pregunta abierta al dueño).
        /// </summary>
        DeferredAck = 2,
    }

    /// <summary>Resultado de una compra que sale de la cola.</summary>
    public sealed class VerificationOutcome
    {
        internal VerificationOutcome(VerificationOutcomeKind kind, PendingVerification entry, VerifyPurchaseResponse? response, BackendError? error)
        {
            Kind = kind;
            Entry = entry;
            Response = response;
            Error = error;
        }

        public VerificationOutcomeKind Kind { get; }

        public PendingVerification Entry { get; }

        /// <summary>Respuesta del servidor (con <see cref="VerificationOutcomeKind.Answered"/>).</summary>
        public VerifyPurchaseResponse? Response { get; }

        /// <summary>Último error (con <see cref="VerificationOutcomeKind.Rejected"/>).</summary>
        public BackendError? Error { get; }
    }

    /// <summary>
    /// Procesa la <see cref="VerificationQueue"/>: en cada <see cref="Pump"/> saca las que cumplieron 72 h
    /// (<see cref="VerificationOutcomeKind.DeferredAck"/>) y manda las que tocan, una petición por compra como mucho a la vez. Qué se
    /// acredita con cada resultado lo decide quien lo use (F3-3: <c>Wallet.Apply</c> y <c>applied_purchases</c>).
    /// <c>onChanged</c> avisa cada vez que la cola cambia, para guardarla.
    /// </summary>
    public sealed class VerificationQueueRunner
    {
        private readonly VerificationQueue _queue;
        private readonly IBackendClient _client;
        private readonly IClock _clock;
        private readonly Action<VerificationOutcome> _onOutcome;
        private readonly Action? _onChanged;
        private readonly HashSet<string> _inFlight = new HashSet<string>(StringComparer.Ordinal);

        public VerificationQueueRunner(VerificationQueue queue, IBackendClient client, IClock clock, Action<VerificationOutcome> onOutcome, Action? onChanged = null)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _onOutcome = onOutcome ?? throw new ArgumentNullException(nameof(onOutcome));
            _onChanged = onChanged;
        }

        /// <summary>Peticiones en vuelo.</summary>
        public int InFlight => _inFlight.Count;

        /// <summary>Saca las vencidas y manda las que tocan. Se puede llamar a menudo (al arrancar, al volver a primer plano…).</summary>
        public void Pump()
        {
            DateTime now = _clock.UtcNow;
            var expired = new List<PendingVerification>();
            for (int i = 0; i < _queue.Entries.Count; i++)
            {
                PendingVerification e = _queue.Entries[i];
                if (!_inFlight.Contains(e.Request.Token) && _queue.IsExpired(e, now))
                {
                    expired.Add(e);
                }
            }

            for (int i = 0; i < expired.Count; i++)
            {
                _queue.Remove(expired[i].Request.Token);
                _onChanged?.Invoke();
                _onOutcome(new VerificationOutcome(VerificationOutcomeKind.DeferredAck, expired[i], null, null));
            }

            List<PendingVerification> due = _queue.Due(now);
            for (int i = 0; i < due.Count; i++)
            {
                PendingVerification entry = due[i];
                if (!_inFlight.Add(entry.Request.Token))
                {
                    continue;
                }

                _client.VerifyPurchase(entry.Request, result => OnResult(entry, result));
            }
        }

        private void OnResult(PendingVerification entry, BackendResult<VerifyPurchaseResponse> result)
        {
            string token = entry.Request.Token;
            _inFlight.Remove(token);
            if (!_queue.Contains(token))
            {
                return;
            }

            if (result.IsOk)
            {
                _queue.Remove(token);
                _onChanged?.Invoke();
                _onOutcome(new VerificationOutcome(VerificationOutcomeKind.Answered, entry, result.Value, null));
                return;
            }

            BackendError error = result.Error!;
            if (VerificationQueue.IsDefinitive(error))
            {
                _queue.Remove(token);
                _onChanged?.Invoke();
                _onOutcome(new VerificationOutcome(VerificationOutcomeKind.Rejected, entry, null, error));
                return;
            }

            _queue.RecordFailure(token, error, _clock.UtcNow);
            _onChanged?.Invoke();
        }
    }
}
