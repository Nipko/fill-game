#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// <c>POST /v1/purchases/verify</c>, petición (<c>verify-purchase.request.schema.json</c>; 08 §10.3). El UID sale del ID
    /// token y la integridad va en la cabecera <c>X-Firebase-AppCheck</c>: el cuerpo no lleva ninguno de los dos.
    /// </summary>
    public sealed class VerifyPurchaseRequest
    {
        /// <exception cref="ArgumentException">Algún campo no cumple el contrato.</exception>
        public VerifyPurchaseRequest(string token, string sku, string? obfuscatedAccountId = null)
        {
            if (!ContractRules.IsPurchaseToken(token))
            {
                throw new ArgumentException("purchaseToken fuera del contrato.", nameof(token));
            }

            if (!ContractRules.IsSku(sku))
            {
                throw new ArgumentException("SKU fuera del contrato.", nameof(sku));
            }

            if (obfuscatedAccountId != null && !ContractRules.IsObfuscatedAccountId(obfuscatedAccountId))
            {
                throw new ArgumentException("obfuscated_account_id fuera del contrato.", nameof(obfuscatedAccountId));
            }

            Token = token;
            Sku = sku;
            ObfuscatedAccountId = obfuscatedAccountId;
        }

        /// <summary><c>purchaseToken</c> de Play Billing, tal cual. No va a los logs.</summary>
        public string Token { get; }

        /// <summary>SKU <c>af_…</c>.</summary>
        public string Sku { get; }

        /// <summary>Lo que el cliente pasó a <c>setObfuscatedAccountId</c> (08 §10.1); opcional.</summary>
        public string? ObfuscatedAccountId { get; }

        public string ToJson() => BackendJson.Write(ToNode());

        internal JsonNode ToNode()
        {
            JsonNode node = JsonNode.NewObject()
                .Set("token", JsonNode.FromString(Token))
                .Set("sku", JsonNode.FromString(Sku));
            if (ObfuscatedAccountId != null)
            {
                node.Set("obfuscated_account_id", JsonNode.FromString(ObfuscatedAccountId));
            }

            return node;
        }

        public static bool TryParse(string? json, out VerifyPurchaseRequest? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static VerifyPurchaseRequest Read(JsonNode node)
        {
            var r = new ObjectReader(node, "verifyPurchase.request", strict: true);
            string token = r.RequiredString("token", ContractRules.IsPurchaseToken);
            string sku = r.RequiredString("sku", ContractRules.IsSku);
            string? obfuscated = r.OptionalString("obfuscated_account_id", ContractRules.IsObfuscatedAccountId);
            r.End();
            return new VerifyPurchaseRequest(token, sku, obfuscated);
        }
    }

    /// <summary>Estado de <c>verifyPurchase</c> (08 §10.3).</summary>
    public enum VerifyStatus
    {
        /// <summary><c>ok</c>: verificada; el cliente acredita <see cref="VerifyPurchaseResponse.Grant"/>.</summary>
        Ok = 0,

        /// <summary><c>pending</c>: no se concede; la transacción sigue en Unity IAP.</summary>
        Pending = 1,

        /// <summary><c>duplicate</c>: el token ya está en el libro mayor de este UID.</summary>
        Duplicate = 2,
    }

    /// <summary>Un ítem de <c>grant.items</c>.</summary>
    public sealed class GrantItem
    {
        public GrantItem(string id, long count)
        {
            if (!ContractRules.IsItemId(id))
            {
                throw new ArgumentException("Id de ítem fuera del contrato.", nameof(id));
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Id = id;
            Count = count;
        }

        public string Id { get; }

        public long Count { get; }
    }

    /// <summary><c>grant</c>: lo que acredita el cliente (08 §10.3: <c>{seeds, items}</c>).</summary>
    public sealed class PurchaseGrant
    {
        /// <summary>Máximo de ítems de un <c>grant</c>.</summary>
        public const int MaxItems = 32;

        public PurchaseGrant(long seeds, IReadOnlyList<GrantItem>? items = null)
        {
            if (seeds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seeds));
            }

            var copy = new List<GrantItem>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    copy.Add(items[i] ?? throw new ArgumentNullException(nameof(items)));
                }
            }

            if (copy.Count > MaxItems)
            {
                throw new ArgumentException("Demasiados ítems.", nameof(items));
            }

            Seeds = seeds;
            Items = copy;
        }

        public long Seeds { get; }

        public IReadOnlyList<GrantItem> Items { get; }

        internal JsonNode ToNode()
        {
            JsonNode items = JsonNode.NewArray();
            for (int i = 0; i < Items.Count; i++)
            {
                items.Add(JsonNode.NewObject()
                    .Set("id", JsonNode.FromString(Items[i].Id))
                    .Set("count", JsonNode.FromLong(Items[i].Count)));
            }

            return JsonNode.NewObject()
                .Set("seeds", JsonNode.FromLong(Seeds))
                .Set("items", items);
        }

        internal static PurchaseGrant Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "grant", strict);
            long seeds = r.RequiredInteger("seeds", 0, long.MaxValue);
            JsonNode array = r.RequiredArray("items", MaxItems);
            var items = new List<GrantItem>(array.Items.Count);
            for (int i = 0; i < array.Items.Count; i++)
            {
                var ir = new ObjectReader(array.Items[i], "grant.items", strict);
                string id = ir.RequiredString("id", ContractRules.IsItemId);
                long count = ir.RequiredInteger("count", 1, long.MaxValue);
                ir.End();
                items.Add(new GrantItem(id, count));
            }

            r.End();
            return new PurchaseGrant(seeds, items);
        }
    }

    /// <summary><c>entitlements</c>: derechos del UID tras la compra (08 §10.3: <c>no_ads</c>, <c>pass_until</c>, <c>skins</c>) [P].</summary>
    public sealed class Entitlements
    {
        /// <summary>Máximo de skins.</summary>
        public const int MaxSkins = 64;

        public Entitlements(bool? noAds = null, long? passUntilMs = null, IReadOnlyList<string>? skins = null)
        {
            if (passUntilMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(passUntilMs));
            }

            List<string>? copy = null;
            if (skins != null)
            {
                copy = new List<string>(skins.Count);
                for (int i = 0; i < skins.Count; i++)
                {
                    if (!ContractRules.IsItemId(skins[i]) || copy.Contains(skins[i]))
                    {
                        throw new ArgumentException("Skin fuera del contrato o repetida.", nameof(skins));
                    }

                    copy.Add(skins[i]);
                }

                if (copy.Count > MaxSkins)
                {
                    throw new ArgumentException("Demasiadas skins.", nameof(skins));
                }
            }

            NoAds = noAds;
            PassUntilMs = passUntilMs;
            Skins = copy;
        }

        /// <summary>Sin anuncios; null si el servidor no lo dice.</summary>
        public bool? NoAds { get; }

        /// <summary>Fin del pase en ms desde 1970 (UTC); null si no lo dice.</summary>
        public long? PassUntilMs { get; }

        /// <summary>Skins del UID; null si no lo dice.</summary>
        public IReadOnlyList<string>? Skins { get; }

        internal JsonNode ToNode()
        {
            JsonNode node = JsonNode.NewObject();
            if (NoAds.HasValue)
            {
                node.Set("no_ads", JsonNode.FromBool(NoAds.Value));
            }

            if (PassUntilMs.HasValue)
            {
                node.Set("pass_until", JsonNode.FromLong(PassUntilMs.Value));
            }

            if (Skins != null)
            {
                JsonNode skins = JsonNode.NewArray();
                for (int i = 0; i < Skins.Count; i++)
                {
                    skins.Add(JsonNode.FromString(Skins[i]));
                }

                node.Set("skins", skins);
            }

            return node;
        }

        internal static Entitlements Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "entitlements", strict);
            bool? noAds = r.OptionalBool("no_ads");
            long? passUntil = r.OptionalInteger("pass_until", 0, long.MaxValue);
            JsonNode? skinsNode = r.OptionalArray("skins", MaxSkins);
            List<string>? skins = skinsNode == null ? null : r.StringSet(skinsNode, "skins", ContractRules.IsItemId);
            r.End();
            return new Entitlements(noAds, passUntil, skins);
        }
    }

    /// <summary>
    /// <c>POST /v1/purchases/verify</c>, respuesta 200 (<c>verify-purchase.response.schema.json</c>): <c>ok</c> con
    /// <c>token_hash</c> y <c>grant</c>; <c>pending</c> sin nada más; <c>duplicate</c> con <c>token_hash</c>.
    /// </summary>
    public sealed class VerifyPurchaseResponse
    {
        private VerifyPurchaseResponse(VerifyStatus status, string? tokenHash, PurchaseGrant? grant, Entitlements? entitlements)
        {
            Status = status;
            TokenHash = tokenHash;
            Grant = grant;
            Entitlements = entitlements;
        }

        public VerifyStatus Status { get; }

        /// <summary>SHA-256 del token (con <c>ok</c> y <c>duplicate</c>); el cliente compara <c>applied_purchases</c> por él.</summary>
        public string? TokenHash { get; }

        /// <summary>Lo que se acredita (solo con <c>ok</c>).</summary>
        public PurchaseGrant? Grant { get; }

        /// <summary>Derechos del UID (opcional, solo con <c>ok</c>).</summary>
        public Entitlements? Entitlements { get; }

        public static VerifyPurchaseResponse Ok(string tokenHash, PurchaseGrant grant, Entitlements? entitlements = null)
        {
            if (!ContractRules.IsTokenHash(tokenHash))
            {
                throw new ArgumentException("token_hash fuera del contrato.", nameof(tokenHash));
            }

            return new VerifyPurchaseResponse(VerifyStatus.Ok, tokenHash, grant ?? throw new ArgumentNullException(nameof(grant)), entitlements);
        }

        public static VerifyPurchaseResponse Pending() => new VerifyPurchaseResponse(VerifyStatus.Pending, null, null, null);

        public static VerifyPurchaseResponse Duplicate(string tokenHash)
        {
            if (!ContractRules.IsTokenHash(tokenHash))
            {
                throw new ArgumentException("token_hash fuera del contrato.", nameof(tokenHash));
            }

            return new VerifyPurchaseResponse(VerifyStatus.Duplicate, tokenHash, null, null);
        }

        public string ToJson() => BackendJson.Write(ToNode());

        internal JsonNode ToNode()
        {
            switch (Status)
            {
                case VerifyStatus.Ok:
                    JsonNode ok = JsonNode.NewObject()
                        .Set("status", JsonNode.FromString("ok"))
                        .Set("token_hash", JsonNode.FromString(TokenHash!))
                        .Set("grant", Grant!.ToNode());
                    if (Entitlements != null)
                    {
                        ok.Set("entitlements", Entitlements.ToNode());
                    }

                    return ok;
                case VerifyStatus.Pending:
                    return JsonNode.NewObject().Set("status", JsonNode.FromString("pending"));
                default:
                    return JsonNode.NewObject()
                        .Set("status", JsonNode.FromString("duplicate"))
                        .Set("token_hash", JsonNode.FromString(TokenHash!));
            }
        }

        /// <summary>Lee una respuesta del servidor (las claves desconocidas se ignoran).</summary>
        public static bool TryParse(string? json, out VerifyPurchaseResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        /// <summary>Lee con <c>additionalProperties: false</c>, como el esquema (tests de contrato).</summary>
        public static bool TryParseStrict(string? json, out VerifyPurchaseResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static VerifyPurchaseResponse Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "verifyPurchase.response", strict);
            string status = r.RequiredString("status");
            switch (status)
            {
                case "ok":
                {
                    string hash = r.RequiredString("token_hash", ContractRules.IsTokenHash);
                    PurchaseGrant grant = PurchaseGrant.Read(r.Required("grant"), strict);
                    JsonNode? ent = r.Optional("entitlements");
                    Entitlements? entitlements = ent == null ? null : Entitlements.Read(ent, strict);
                    r.End();
                    return Ok(hash, grant, entitlements);
                }

                case "pending":
                    r.End();
                    return Pending();
                case "duplicate":
                {
                    string hash = r.RequiredString("token_hash", ContractRules.IsTokenHash);
                    r.End();
                    return Duplicate(hash);
                }

                default:
                    throw new ContractException("verifyPurchase.response: 'status' desconocido.");
            }
        }
    }
}
