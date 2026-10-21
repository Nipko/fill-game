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
        /// El servidor respondió <c>ok</c>, <c>duplicate</c> o <c>pending</c> (ver <see cref="VerificationOutcome.Response"/>). Con
        /// <c>ok</c> y <c>duplicate</c> la compra sale de la cola; con <c>pending</c> se queda y se vuelve a preguntar más tarde
        /// (<see cref="VerificationQueue.RecordPending"/>; 08 §10.3: «reintento en cada arranque»), sin acreditar nada.
        /// </summary>
        Answered = 0,

        /// <summary>Error definitivo (<see cref="VerificationQueue.IsDefinitive"/>): sale de la cola y no se reintenta.</summary>
        Rejected = 1,

        /// <summary>
        /// Cumplió <c>sec_purchase_deferred_ack_h</c> sin respuesta: toca <c>acknowledge</c> local y <c>deferred_ack</c> (08 §10.3). La
        /// compra se queda en la cola (<see cref="PendingVerification.IsDeferredAck"/>) y se sigue reintentando: no se acredita hasta
        /// que el servidor la verifique (D-035, añadido de F3-1 y F3-2).
        /// </summary>
        DeferredAck = 2,

        /// <summary>
        /// Fallo pasajero (sin red, 401, 403, 429, 5xx, respuesta ilegible…): la compra se queda en la cola con su espera
        /// (<see cref="VerificationQueue.RecordFailure"/>). Ver <see cref="VerificationOutcome.Error"/>.
        /// </summary>
        Retrying = 3,
    }

    /// <summary>Resultado de una compra de la cola.</summary>
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

        /// <summary>El error (con <see cref="VerificationOutcomeKind.Rejected"/> y <see cref="VerificationOutcomeKind.Retrying"/>).</summary>
        public BackendError? Error { get; }
    }

    /// <summary>
    /// Procesa la <see cref="VerificationQueue"/>: en cada <see cref="Pump"/> anota el <c>acknowledge</c> local de las que cumplieron
    /// 72 h (<see cref="VerificationOutcomeKind.DeferredAck"/>, una sola vez por compra) y manda las que tocan, una petición por compra
    /// como mucho a la vez. Qué se acredita con cada resultado lo decide quien lo use (F3-3: <c>AF.IAP.VerifiedStoreService</c>, con
    /// <c>Wallet.Apply</c> y <c>applied_purchases</c>). <c>onOutcome</c> recibe cada resultado con la cola ya actualizada (para que lo
    /// que acredite se guarde junto con la cola) y después <c>onChanged</c> avisa de que la cola cambió, para guardarla.
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

        /// <summary>¿Está en vuelo la petición de <paramref name="token"/>?</summary>
        public bool IsInFlight(string token) => _inFlight.Contains(token);

        /// <summary>
        /// Anota las vencidas y, con <paramref name="send"/>, manda las que tocan. Se puede llamar a menudo (al arrancar, al volver a
        /// primer plano…). Sin <paramref name="send"/> (backend apagado con <c>biz_backend_enabled</c> [P]) no hay red, pero el plazo
        /// del <c>acknowledge</c> local sigue corriendo: Play reembolsa sola una compra sin reconocer a los 3 días.
        /// </summary>
        public void Pump(bool send = true)
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
                _queue.MarkDeferredAck(expired[i].Request.Token, now);
                _onOutcome(new VerificationOutcome(VerificationOutcomeKind.DeferredAck, expired[i], null, null));
                _onChanged?.Invoke();
            }

            if (!send)
            {
                return;
            }

            List<PendingVerification> due = _queue.Due(now);
            for (int i = 0; i < due.Count; i++)
            {
                PendingVerification entry = due[i];
                if (!_queue.Contains(entry.Request.Token) || !_inFlight.Add(entry.Request.Token))
                {
                    continue; // una respuesta síncrona anterior pudo sacarla de la cola
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
                VerifyPurchaseResponse response = result.Value!;
                if (response.Status == VerifyStatus.Pending)
                {
                    _queue.RecordPending(token, _clock.UtcNow);
                }
                else
                {
                    _queue.Remove(token);
                }

                _onOutcome(new VerificationOutcome(VerificationOutcomeKind.Answered, entry, response, null));
                _onChanged?.Invoke();
                return;
            }

            BackendError error = result.Error!;
            if (VerificationQueue.IsDefinitive(error))
            {
                _queue.Remove(token);
                _onOutcome(new VerificationOutcome(VerificationOutcomeKind.Rejected, entry, null, error));
                _onChanged?.Invoke();
                return;
            }

            _queue.RecordFailure(token, error, _clock.UtcNow);
            _onOutcome(new VerificationOutcome(VerificationOutcomeKind.Retrying, entry, null, error));
            _onChanged?.Invoke();
        }
    }
}
