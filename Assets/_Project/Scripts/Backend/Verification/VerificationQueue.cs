#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;
using AF.Backend.Json;

namespace AF.Backend.Verification
{
    /// <summary>Una compra a la espera de que el servidor la verifique.</summary>
    public sealed class PendingVerification
    {
        internal PendingVerification(VerifyPurchaseRequest request, DateTime firstQueuedUtc, int attempts, DateTime nextAttemptUtc, string? lastError)
        {
            Request = request;
            FirstQueuedUtc = firstQueuedUtc;
            Attempts = attempts;
            NextAttemptUtc = nextAttemptUtc;
            LastError = lastError;
        }

        public VerifyPurchaseRequest Request { get; }

        /// <summary>Cuándo entró en la cola: el plazo de 72 h cuenta desde aquí.</summary>
        public DateTime FirstQueuedUtc { get; }

        /// <summary>Intentos fallidos.</summary>
        public int Attempts { get; internal set; }

        /// <summary>No se reintenta antes de este instante.</summary>
        public DateTime NextAttemptUtc { get; internal set; }

        /// <summary>Último error (<see cref="BackendError.ToString"/>: sin tokens).</summary>
        public string? LastError { get; internal set; }

        public override string ToString() => "PendingVerification(" + Request.Sku + ", intentos " + Attempts + ")";
    }

    /// <summary>
    /// Cola local de verificaciones de compra de 08 §10.3: «error de red → cola local con backoff; a las 72 h acknowledge local +
    /// <c>deferred_ack</c> [P]» (08 §10.1, fila «Compra falsa»). Es solo el estado y sus reglas, sin red ni guardado: la procesa
    /// <see cref="VerificationQueueRunner"/> y la guarda quien la tenga (F3-3 la pondrá en el perfil con <see cref="ToJson"/>).
    /// Los <c>purchaseToken</c> viajan dentro del JSON: no va a los logs.
    /// </summary>
    public sealed class VerificationQueue
    {
        /// <summary>Plazo tras el que el cliente hace <c>acknowledge</c> local y marca <c>deferred_ack</c> (08 §10.1 y §10.3).</summary>
        public static readonly TimeSpan DeferredAckAfter = TimeSpan.FromHours(72);

        private const int FormatVersion = 1;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly List<PendingVerification> _entries = new List<PendingVerification>();

        public VerificationQueue(RetryPolicy? policy = null)
        {
            Policy = policy ?? RetryPolicy.Queue;
        }

        public RetryPolicy Policy { get; }

        public IReadOnlyList<PendingVerification> Entries => _entries;

        public int Count => _entries.Count;

        /// <summary>
        /// Encola <paramref name="request"/> para intentarla ya. Si su token ya está, no hace nada y devuelve false (la misma compra
        /// no se encola dos veces).
        /// </summary>
        public bool Enqueue(VerifyPurchaseRequest request, DateTime nowUtc)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (Find(request.Token) != null)
            {
                return false;
            }

            _entries.Add(new PendingVerification(request, Utc(nowUtc), 0, Utc(nowUtc), null));
            return true;
        }

        public bool Contains(string token) => Find(token) != null;

        public bool Remove(string token)
        {
            PendingVerification? entry = Find(token);
            return entry != null && _entries.Remove(entry);
        }

        /// <summary>Las que tocan ahora (su espera venció y no han cumplido las 72 h), por orden de llegada.</summary>
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

        /// <summary>¿Cumplió ya las 72 h en la cola?</summary>
        public static bool IsExpired(PendingVerification entry, DateTime nowUtc)
        {
            return Utc(nowUtc) - entry.FirstQueuedUtc >= DeferredAckAfter;
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
        /// mientras la función es un esqueleto, cuerpo ilegible) se reintentan hasta las 72 h.
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

                entries.Add(node);
            }

            return BackendJson.Write(JsonNode.NewObject()
                .Set("v", JsonNode.FromLong(FormatVersion))
                .Set("entries", entries));
        }

        /// <summary>Lee lo que escribió <see cref="ToJson"/>. False si no se entiende (el llamador decide si empieza vacía).</summary>
        public static bool TryFromJson(string? json, out VerificationQueue? queue, out string? error, RetryPolicy? policy = null)
        {
            bool ok = ContractRules.TryRead(json, node => Read(node, policy), out queue, out error);
            return ok;
        }

        private static VerificationQueue Read(JsonNode node, RetryPolicy? policy)
        {
            var r = new ObjectReader(node, "verificationQueue", strict: false);
            if (r.RequiredInteger("v", 1, int.MaxValue) != FormatVersion)
            {
                throw new ContractException("verificationQueue: versión desconocida.");
            }

            var queue = new VerificationQueue(policy);
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
                if (queue.Find(token) != null)
                {
                    continue;
                }

                queue._entries.Add(new PendingVerification(
                    new VerifyPurchaseRequest(token, sku, oaid), FromMs(first), (int)attempts, FromMs(next), lastError));
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
