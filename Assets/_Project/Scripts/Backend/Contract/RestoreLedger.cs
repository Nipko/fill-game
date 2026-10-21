#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// <c>POST /v1/ledger/restore</c>, petición (<c>restore-ledger.request.schema.json</c>; 08 §11, fila Conflicto, y §10.2):
    /// pide el libro mayor del UID y confirma los nonces de recompensa pendientes (&lt; 24 h).
    /// </summary>
    public sealed class RestoreLedgerRequest
    {
        /// <exception cref="ArgumentException">Algún nonce no es un UUID v4 en minúsculas, hay repetidos o pasan de 200.</exception>
        public RestoreLedgerRequest(IReadOnlyList<string>? pendingNonces = null)
        {
            PendingNonces = CopyNonces(pendingNonces, nameof(pendingNonces));
        }

        /// <summary>Nonces de recompensas concedidas en provisional y aún sin confirmar.</summary>
        public IReadOnlyList<string> PendingNonces { get; }

        public string ToJson() => BackendJson.Write(ToNode());

        internal JsonNode ToNode()
        {
            return JsonNode.NewObject().Set("pending_nonces", NonceArray(PendingNonces));
        }

        public static bool TryParse(string? json, out RestoreLedgerRequest? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static RestoreLedgerRequest Read(JsonNode node)
        {
            var r = new ObjectReader(node, "restoreLedger.request", strict: true);
            List<string> nonces = r.StringSet(r.RequiredArray("pending_nonces", ContractRules.MaxNonces), "pending_nonces", ContractRules.IsNonce);
            r.End();
            return new RestoreLedgerRequest(nonces);
        }

        internal static List<string> CopyNonces(IReadOnlyList<string>? nonces, string paramName)
        {
            var copy = new List<string>();
            if (nonces == null)
            {
                return copy;
            }

            for (int i = 0; i < nonces.Count; i++)
            {
                if (!ContractRules.IsNonce(nonces[i]) || copy.Contains(nonces[i]))
                {
                    throw new ArgumentException("Nonce fuera del contrato o repetido.", paramName);
                }

                copy.Add(nonces[i]);
            }

            if (copy.Count > ContractRules.MaxNonces)
            {
                throw new ArgumentException("Más de 200 nonces.", paramName);
            }

            return copy;
        }

        internal static JsonNode NonceArray(IReadOnlyList<string> nonces)
        {
            JsonNode array = JsonNode.NewArray();
            for (int i = 0; i < nonces.Count; i++)
            {
                array.Add(JsonNode.FromString(nonces[i]));
            }

            return array;
        }
    }

    /// <summary>Estado de una compra en el libro mayor.</summary>
    public enum LedgerPurchaseState
    {
        /// <summary><c>ok</c>: vigente.</summary>
        Ok = 0,

        /// <summary><c>voided</c>: anulada por Play (reembolso o contracargo; 08 §10.1): el cliente la retira sin bajar de 0.</summary>
        Voided = 1,
    }

    /// <summary>Una compra del libro mayor del UID.</summary>
    public sealed class LedgerPurchase
    {
        public LedgerPurchase(string tokenHash, string sku, LedgerPurchaseState state, long tsMs, long? voidedAtMs = null)
        {
            if (!ContractRules.IsTokenHash(tokenHash))
            {
                throw new ArgumentException("token_hash fuera del contrato.", nameof(tokenHash));
            }

            if (!ContractRules.IsSku(sku))
            {
                throw new ArgumentException("SKU fuera del contrato.", nameof(sku));
            }

            if (tsMs < 0 || voidedAtMs < 0)
            {
                throw new ArgumentOutOfRangeException(tsMs < 0 ? nameof(tsMs) : nameof(voidedAtMs));
            }

            TokenHash = tokenHash;
            Sku = sku;
            State = state;
            TsMs = tsMs;
            VoidedAtMs = voidedAtMs;
        }

        public string TokenHash { get; }

        public string Sku { get; }

        public LedgerPurchaseState State { get; }

        /// <summary>Instante de la compra (ms desde 1970, UTC).</summary>
        public long TsMs { get; }

        /// <summary>Instante de la anulación, si la hay.</summary>
        public long? VoidedAtMs { get; }

        internal JsonNode ToNode()
        {
            JsonNode node = JsonNode.NewObject()
                .Set("token_hash", JsonNode.FromString(TokenHash))
                .Set("sku", JsonNode.FromString(Sku))
                .Set("state", JsonNode.FromString(State == LedgerPurchaseState.Voided ? "voided" : "ok"))
                .Set("ts", JsonNode.FromLong(TsMs));
            if (VoidedAtMs.HasValue)
            {
                node.Set("voided_at", JsonNode.FromLong(VoidedAtMs.Value));
            }

            return node;
        }

        internal static LedgerPurchase Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "restoreLedger.purchases", strict);
            string hash = r.RequiredString("token_hash", ContractRules.IsTokenHash);
            string sku = r.RequiredString("sku", ContractRules.IsSku);
            string state = r.RequiredString("state");
            LedgerPurchaseState parsed;
            if (state == "ok")
            {
                parsed = LedgerPurchaseState.Ok;
            }
            else if (state == "voided")
            {
                parsed = LedgerPurchaseState.Voided;
            }
            else
            {
                throw new ContractException("restoreLedger.purchases: 'state' desconocido.");
            }

            long ts = r.RequiredInteger("ts", 0, long.MaxValue);
            long? voidedAt = r.OptionalInteger("voided_at", 0, long.MaxValue);
            r.End();
            return new LedgerPurchase(hash, sku, parsed, ts, voidedAt);
        }
    }

    /// <summary>
    /// <c>POST /v1/ledger/restore</c>, respuesta 200 (<c>restore-ledger.response.schema.json</c>): las compras del UID con su
    /// estado y los nonces de <c>pending_nonces</c> que el servidor tiene confirmados. Los no devueltos siguen sin confirmar.
    /// </summary>
    public sealed class RestoreLedgerResponse
    {
        /// <summary>Máximo de compras de una respuesta.</summary>
        public const int MaxPurchases = 1000;

        public RestoreLedgerResponse(IReadOnlyList<LedgerPurchase>? purchases, IReadOnlyList<string>? confirmedNonces)
        {
            var copy = new List<LedgerPurchase>();
            if (purchases != null)
            {
                for (int i = 0; i < purchases.Count; i++)
                {
                    copy.Add(purchases[i] ?? throw new ArgumentNullException(nameof(purchases)));
                }
            }

            if (copy.Count > MaxPurchases)
            {
                throw new ArgumentException("Más de 1.000 compras.", nameof(purchases));
            }

            Purchases = copy;
            ConfirmedNonces = RestoreLedgerRequest.CopyNonces(confirmedNonces, nameof(confirmedNonces));
        }

        public IReadOnlyList<LedgerPurchase> Purchases { get; }

        public IReadOnlyList<string> ConfirmedNonces { get; }

        public string ToJson() => BackendJson.Write(ToNode());

        internal JsonNode ToNode()
        {
            JsonNode purchases = JsonNode.NewArray();
            for (int i = 0; i < Purchases.Count; i++)
            {
                purchases.Add(Purchases[i].ToNode());
            }

            return JsonNode.NewObject()
                .Set("purchases", purchases)
                .Set("confirmed_nonces", RestoreLedgerRequest.NonceArray(ConfirmedNonces));
        }

        /// <summary>Lee una respuesta del servidor (las claves desconocidas se ignoran).</summary>
        public static bool TryParse(string? json, out RestoreLedgerResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        /// <summary>Lee con <c>additionalProperties: false</c>, como el esquema.</summary>
        public static bool TryParseStrict(string? json, out RestoreLedgerResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static RestoreLedgerResponse Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "restoreLedger.response", strict);
            JsonNode array = r.RequiredArray("purchases", MaxPurchases);
            var purchases = new List<LedgerPurchase>(array.Items.Count);
            for (int i = 0; i < array.Items.Count; i++)
            {
                purchases.Add(LedgerPurchase.Read(array.Items[i], strict));
            }

            List<string> confirmed = r.StringSet(r.RequiredArray("confirmed_nonces", ContractRules.MaxNonces), "confirmed_nonces", ContractRules.IsNonce);
            r.End();
            return new RestoreLedgerResponse(purchases, confirmed);
        }
    }
}
