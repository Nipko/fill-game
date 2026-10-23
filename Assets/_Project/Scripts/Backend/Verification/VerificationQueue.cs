#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;
using AF.Backend.Json;
using AF.Platform;

namespace AF.Backend.Verification
{
    /// <summary>
    /// Cifras de la cola con el <b>default compilado</b> de la tabla única de 08 §9. <c>sec_purchase_deferred_ack_h</c>: horas sin
    /// respuesta del servidor tras las que el cliente hace <c>acknowledge</c> local y marca <c>deferred_ack</c> (08 §10.1 y §10.3);
    /// default 48, rango 48-72 (Play reembolsa sola una compra sin reconocer a los 3 días; 48 y no 72 para dejar margen: D-040, Q-BILL-5).
    /// </summary>
    public sealed class VerificationQueueConfig
    {
        /// <summary>Mínimo del rango de 08 §9.</summary>
        public const int MinDeferredAckH = 48;

        /// <summary>Máximo del rango de 08 §9.</summary>
        public const int MaxDeferredAckH = 72;

        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static VerificationQueueConfig Defaults => new VerificationQueueConfig();

        /// <summary><c>sec_purchase_deferred_ack_h</c> [P].</summary>
        [RemoteConfigKey("sec_purchase_deferred_ack_h")]
        public int SecPurchaseDeferredAckH { get; set; } = 48;

        /// <summary>El plazo como intervalo.</summary>
        public TimeSpan DeferredAckAfter => TimeSpan.FromHours(SecPurchaseDeferredAckH);

        /// <exception cref="InvalidOperationException">Fuera de 48-72.</exception>
        public void Validate()
        {
            if (SecPurchaseDeferredAckH < MinDeferredAckH || SecPurchaseDeferredAckH > MaxDeferredAckH)
            {
                throw new InvalidOperationException("VerificationQueueConfig: sec_purchase_deferred_ack_h debe estar entre 48 y 72.");
            }
        }
    }

    /// <summary>Una compra a la espera de que el servidor la verifique.</summary>
    public sealed class PendingVerification
    {
        internal PendingVerification(
            VerifyPurchaseRequest request, DateTime firstQueuedUtc, int attempts, DateTime nextAttemptUtc, string? lastError,
            int? clientSeeds = null, DateTime? deferredAckUtc = null)
        {
            Request = request;
            FirstQueuedUtc = firstQueuedUtc;
            Attempts = attempts;
            NextAttemptUtc = nextAttemptUtc;
            LastError = lastError;
            ClientSeeds = clientSeeds;
            DeferredAckUtc = deferredAckUtc;
        }

        public VerifyPurchaseRequest Request { get; }

        /// <summary>
        /// Desde cuándo cuenta el plazo de <c>sec_purchase_deferred_ack_h</c>: la hora de la compra si se conoce (Play la reembolsa
        /// a los 3 días de la compra, no de entrar en la cola) o, si no, la de entrar en la cola.
        /// </summary>
        public DateTime FirstQueuedUtc { get; internal set; }

        /// <summary>Intentos fallidos.</summary>
        public int Attempts { get; internal set; }

        /// <summary>No se reintenta antes de este instante.</summary>
        public DateTime NextAttemptUtc { get; internal set; }

        /// <summary>Último error (<see cref="BackendError.ToString"/>: sin tokens), o <c>pending</c> si el servidor respondió así.</summary>
        public string? LastError { get; internal set; }

        /// <summary>
        /// Semillas que solo conoce el cliente y que la compra acredita: las del Cántaro (<c>af_piggy</c>; el servidor responde
        /// semillas 0 y el ítem <c>piggy</c>, D-035, añadido de F2-3 a F2-8 [P]). Null en el resto.
        /// </summary>
        public int? ClientSeeds { get; }

        /// <summary>
        /// Cuándo se hizo el <c>acknowledge</c> local por vencer <c>sec_purchase_deferred_ack_h</c> (<c>deferred_ack</c>, 08 §10.3), o
        /// null. Una compra así sigue en la cola y se sigue reintentando sin plazo: no se acredita hasta que el servidor la verifique
        /// (D-035, añadido de F3-1 y F3-2).
        /// </summary>
        public DateTime? DeferredAckUtc { get; internal set; }

        /// <summary>Ya se hizo el <c>acknowledge</c> local (<see cref="DeferredAckUtc"/>).</summary>
        public bool IsDeferredAck => DeferredAckUtc.HasValue;

        public override string ToString() => "PendingVerification(" + Request.Sku + ", intentos " + Attempts + ")";
    }

    /// <summary>
    /// Cola local de verificaciones de compra de 08 §10.3: «error de red → cola local con backoff; a las 48 h acknowledge local +
    /// <c>deferred_ack</c> [P]» (08 §10.1, fila «Compra falsa»), con el plazo de <c>sec_purchase_deferred_ack_h</c> (08 §9). Es solo el estado y sus reglas, sin red ni guardado: la procesa
    /// <see cref="VerificationQueueRunner"/> y la guarda quien la tenga (F3-3: <c>AF.IAP.VerifiedStoreService</c>, en el perfil con <see cref="ToJson"/>).
    /// Los <c>purchaseToken</c> viajan dentro del JSON: no va a los logs.
    /// </summary>
    public sealed class VerificationQueue
    {
        private const int FormatVersion = 1;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly List<PendingVerification> _entries = new List<PendingVerification>();

        /// <exception cref="InvalidOperationException"><paramref name="config"/> fuera del rango de 08 §9.</exception>
        public VerificationQueue(VerificationQueueConfig? config = null, RetryPolicy? policy = null)
        {
            VerificationQueueConfig c = config ?? VerificationQueueConfig.Defaults;
            c.Validate();
            DeferredAckAfter = c.DeferredAckAfter;
            Policy = policy ?? RetryPolicy.Queue;
        }

        public RetryPolicy Policy { get; }

        /// <summary>Plazo tras el que el cliente hace <c>acknowledge</c> local y marca <c>deferred_ack</c> (<c>sec_purchase_deferred_ack_h</c>).</summary>
        public TimeSpan DeferredAckAfter { get; }

        public IReadOnlyList<PendingVerification> Entries => _entries;

        public int Count => _entries.Count;

        /// <summary>
        /// Encola <paramref name="request"/> para intentarla ya. Si su token ya está, no hace nada y devuelve false (la misma compra
        /// no se encola dos veces). <paramref name="purchasedUtc"/> es la hora de la compra si se conoce: el plazo cuenta desde ella
        /// (si es posterior a <paramref name="nowUtc"/>, se usa <paramref name="nowUtc"/>).
        /// </summary>
        /// <param name="clientSeeds">Semillas que solo conoce el cliente (<see cref="PendingVerification.ClientSeeds"/>), o null.</param>
        public bool Enqueue(VerifyPurchaseRequest request, DateTime nowUtc, DateTime? purchasedUtc = null, int? clientSeeds = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (Find(request.Token) != null)
            {
                return false;
            }

            if (clientSeeds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(clientSeeds));
            }

            DateTime now = Utc(nowUtc);
            DateTime start = now;
            if (purchasedUtc.HasValue && Utc(purchasedUtc.Value) < now)
            {
                start = purchasedUtc.Value;
            }

            _entries.Add(new PendingVerification(request, start, 0, now, null, clientSeeds));
            return true;
        }

        public bool Contains(string token) => Find(token) != null;

        /// <summary>
        /// Devuelve a la cola una entrada que ya había salido (el servidor respondió, pero quien la usa no pudo acreditarla), tal cual
        /// (intentos, semillas del cliente y <c>deferred_ack</c>), y anota el fallo con su espera. False si su token ya está.
        /// </summary>
        public bool Requeue(PendingVerification entry, BackendError error, DateTime nowUtc)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (Find(entry.Request.Token) != null)
            {
                return false;
            }

            _entries.Add(entry);
            RecordFailure(entry.Request.Token, error, nowUtc);
            return true;
        }

        /// <summary>La entrada de <paramref name="token"/>, o null.</summary>
        public PendingVerification? Get(string token) => Find(token);

        public bool Remove(string token)
        {
            PendingVerification? entry = Find(token);
            return entry != null && _entries.Remove(entry);
        }

        /// <summary>Las que tocan ahora (su espera venció y no han cumplido el plazo), por orden de llegada.</summary>
        public List<PendingVerification> Due(DateTime nowUtc)
        {
            DateTime now = Utc(nowUtc);
            var due = new List<PendingVerification>();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].NextAttemptUtc <= now && !IsExpired(_entries[i], now))
                {
                    due.Add(_entries[i]);
                }
            }

            return due;
        }

        /// <summary>
        /// ¿Cumplió ya el plazo de <see cref="DeferredAckAfter"/> sin <c>acknowledge</c> local? Una entrada con
        /// <see cref="PendingVerification.IsDeferredAck"/> ya no vence: se sigue reintentando.
        /// </summary>
        public bool IsExpired(PendingVerification entry, DateTime nowUtc)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return !entry.IsDeferredAck && Utc(nowUtc) - entry.FirstQueuedUtc >= DeferredAckAfter;
        }

        /// <summary>
        /// Anota el <c>acknowledge</c> local de <paramref name="token"/> (<c>deferred_ack</c>): la entrada sigue en la cola, sin plazo,
        /// para que un reintento la verifique (D-035, añadido de F3-1 y F3-2: no se acredita hasta entonces). Se puede volver a
        /// intentar ya.
        /// </summary>
        public void MarkDeferredAck(string token, DateTime nowUtc)
        {
            PendingVerification entry = Find(token) ?? throw new ArgumentException("El token no está en la cola.", nameof(token));
            if (!entry.DeferredAckUtc.HasValue)
            {
                entry.DeferredAckUtc = Utc(nowUtc);
            }
        }

        /// <summary>
        /// El servidor respondió <c>pending</c> (Play aún no la da por pagada; 08 §10.3): no se acredita y se vuelve a preguntar con
        /// la espera de <see cref="Policy"/>. El plazo de <see cref="DeferredAckAfter"/> vuelve a contar desde ahora, porque el
        /// servidor sí respondió (y una compra pendiente no se puede reconocer).
        /// </summary>
        public void RecordPending(string token, DateTime nowUtc)
        {
            PendingVerification entry = Find(token) ?? throw new ArgumentException("El token no está en la cola.", nameof(token));
            DateTime now = Utc(nowUtc);
            entry.Attempts++;
            entry.LastError = "pending";
            entry.NextAttemptUtc = now + Policy.DelayFor(entry.Attempts);
            if (!entry.IsDeferredAck)
            {
                entry.FirstQueuedUtc = now;
            }
        }

        /// <summary>
        /// Anota un intento fallido de <paramref name="token"/>: la siguiente espera es la de <see cref="Policy"/> (o el
        /// <c>Retry-After</c> del servidor si es mayor).
        /// </summary>
        public void RecordFailure(string token, BackendError error, DateTime nowUtc)
        {
            PendingVerification entry = Find(token) ?? throw new ArgumentException("El token no está en la cola.", nameof(token));
            entry.Attempts++;
            entry.LastError = error?.ToString();
            entry.NextAttemptUtc = Utc(nowUtc) + Policy.DelayFor(entry.Attempts, error?.RetryAfter);
        }

        /// <summary>
        /// Un error tras el que no tiene sentido reintentar la misma petición: 400 <c>bad_request</c> (el cuerpo no vale) y 409
        /// <c>token_belongs_to_other_user</c> (el token es de otro UID). Los demás (red, 401, 403 de integridad, 429, 5xx, 501
        /// mientras la función es un esqueleto, cuerpo ilegible) se reintentan hasta que vence el plazo.
        /// </summary>
        public static bool IsDefinitive(BackendError error)
        {
            return error != null && error.Kind == BackendErrorKind.Http && (error.HttpStatus == 400 || error.HttpStatus == 409);
        }

        /// <summary>Estado para guardarlo (lleva los tokens: va al perfil, nunca a los logs).</summary>
        public string ToJson()
        {
            JsonNode entries = JsonNode.NewArray();
            for (int i = 0; i < _entries.Count; i++)
            {
                PendingVerification e = _entries[i];
                JsonNode node = JsonNode.NewObject()
                    .Set("token", JsonNode.FromString(e.Request.Token))
                    .Set("sku", JsonNode.FromString(e.Request.Sku));
                if (e.Request.ObfuscatedAccountId != null)
                {
                    node.Set("obfuscated_account_id", JsonNode.FromString(e.Request.ObfuscatedAccountId));
                }

                node.Set("first_ms", JsonNode.FromLong(ToMs(e.FirstQueuedUtc)))
                    .Set("attempts", JsonNode.FromLong(e.Attempts))
                    .Set("next_ms", JsonNode.FromLong(ToMs(e.NextAttemptUtc)));
                if (e.LastError != null)
                {
                    node.Set("last_error", JsonNode.FromString(e.LastError));
                }

                if (e.ClientSeeds.HasValue)
                {
                    node.Set("client_seeds", JsonNode.FromLong(e.ClientSeeds.Value));
                }

                if (e.DeferredAckUtc.HasValue)
                {
                    node.Set("deferred_ack_ms", JsonNode.FromLong(ToMs(e.DeferredAckUtc.Value)));
                }

                entries.Add(node);
            }

            return BackendJson.Write(JsonNode.NewObject()
                .Set("v", JsonNode.FromLong(FormatVersion))
                .Set("entries", entries));
        }

        /// <summary>Lee lo que escribió <see cref="ToJson"/>. False si no se entiende (el llamador decide si empieza vacía).</summary>
        public static bool TryFromJson(
            string? json, out VerificationQueue? queue, out string? error, VerificationQueueConfig? config = null, RetryPolicy? policy = null)
        {
            return ContractRules.TryRead(json, node => Read(node, config, policy), out queue, out error);
        }

        private static VerificationQueue Read(JsonNode node, VerificationQueueConfig? config, RetryPolicy? policy)
        {
            var r = new ObjectReader(node, "verificationQueue", strict: false);
            if (r.RequiredInteger("v", 1, int.MaxValue) != FormatVersion)
            {
                throw new ContractException("verificationQueue: versión desconocida.");
            }

            var queue = new VerificationQueue(config, policy);
            JsonNode entries = r.RequiredArray("entries", 10000);
            for (int i = 0; i < entries.Items.Count; i++)
            {
                var er = new ObjectReader(entries.Items[i], "verificationQueue.entries", strict: false);
                string token = er.RequiredString("token", ContractRules.IsPurchaseToken);
                string sku = er.RequiredString("sku", ContractRules.IsSku);
                string? oaid = er.OptionalString("obfuscated_account_id", ContractRules.IsObfuscatedAccountId);
                long first = er.RequiredInteger("first_ms", 0, long.MaxValue / 2);
                long attempts = er.RequiredInteger("attempts", 0, int.MaxValue);
                long next = er.RequiredInteger("next_ms", 0, long.MaxValue / 2);
                string? lastError = er.OptionalString("last_error");
                long? clientSeeds = er.OptionalInteger("client_seeds", 0, int.MaxValue);
                long? deferredAck = er.OptionalInteger("deferred_ack_ms", 0, long.MaxValue / 2);
                if (queue.Find(token) != null)
                {
                    continue;
                }

                queue._entries.Add(new PendingVerification(
                    new VerifyPurchaseRequest(token, sku, oaid), FromMs(first), (int)attempts, FromMs(next), lastError,
                    clientSeeds.HasValue ? (int)clientSeeds.Value : (int?)null,
                    deferredAck.HasValue ? FromMs(deferredAck.Value) : (DateTime?)null));
            }

            return queue;
        }

        private PendingVerification? Find(string token)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Request.Token, token, StringComparison.Ordinal))
                {
                    return _entries[i];
                }
            }

            return null;
        }

        private static DateTime Utc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Se esperaba una hora UTC (IClock.UtcNow).");
            }

            return value;
        }

        private static long ToMs(DateTime utc) => (long)(utc - Epoch).TotalMilliseconds;

        private static DateTime FromMs(long ms)
        {
            // Sin pasar del máximo de DateTime aunque el archivo traiga un valor absurdo.
            const long MaxMs = 253402300799000L;
            return Epoch.AddMilliseconds(Math.Min(ms, MaxMs - 1));
        }
    }
}
