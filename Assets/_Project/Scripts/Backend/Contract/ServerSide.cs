#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// Parámetros del callback S2S de MAX a <c>/v1/ssv</c> (<c>rewarded-ssv.request.schema.json</c>; 08 §10.1-§10.2). El cliente
    /// nunca lo envía: lo usan los tests de contrato y <see cref="FakeBackendClient"/> para simular que MAX confirmó un nonce.
    /// Admite parámetros extra de texto (los fija la URL del panel de MAX).
    /// </summary>
    public sealed class SsvCallback
    {
        /// <summary>Longitud máxima de un parámetro extra.</summary>
        public const int MaxExtraLength = 2048;

        public SsvCallback(
            string userId,
            string placement,
            string nonce,
            string? network = null,
            string? eventId = null,
            string? eventToken = null,
            IReadOnlyList<KeyValuePair<string, string>>? extras = null)
        {
            if (!ContractRules.IsFirebaseUid(userId))
            {
                throw new ArgumentException("user_id fuera del contrato.", nameof(userId));
            }

            if (!ContractRules.IsCustomData((placement ?? string.Empty) + "|" + (nonce ?? string.Empty)))
            {
                throw new ArgumentException("custom_data fuera del contrato.", nameof(nonce));
            }

            if (network != null && network.Length > 64)
            {
                throw new ArgumentException("network de más de 64 caracteres.", nameof(network));
            }

            if (eventId != null && !ContractRules.IsEventId(eventId))
            {
                throw new ArgumentException("event_id fuera del contrato.", nameof(eventId));
            }

            if (eventToken != null && !ContractRules.IsEventToken(eventToken))
            {
                throw new ArgumentException("event_token fuera del contrato.", nameof(eventToken));
            }

            var copy = new List<KeyValuePair<string, string>>();
            if (extras != null)
            {
                for (int i = 0; i < extras.Count; i++)
                {
                    string key = extras[i].Key;
                    if (key == null || extras[i].Value == null || extras[i].Value.Length > MaxExtraLength || IsKnownKey(key))
                    {
                        throw new ArgumentException("Parámetro extra inválido.", nameof(extras));
                    }

                    copy.Add(extras[i]);
                }
            }

            UserId = userId;
            Placement = placement!;
            Nonce = nonce!;
            Network = network;
            EventId = eventId;
            EventToken = eventToken;
            Extras = copy;
        }

        /// <summary>UID de Firebase (<c>user_id</c>).</summary>
        public string UserId { get; }

        public string Placement { get; }

        public string Nonce { get; }

        /// <summary><c>custom_data</c> = <c>placement|nonce</c>.</summary>
        public string CustomData => Placement + "|" + Nonce;

        public string? Network { get; }

        public string? EventId { get; }

        public string? EventToken { get; }

        public IReadOnlyList<KeyValuePair<string, string>> Extras { get; }

        public string ToJson()
        {
            JsonNode node = JsonNode.NewObject()
                .Set("user_id", JsonNode.FromString(UserId))
                .Set("custom_data", JsonNode.FromString(CustomData));
            if (Network != null)
            {
                node.Set("network", JsonNode.FromString(Network));
            }

            if (EventId != null)
            {
                node.Set("event_id", JsonNode.FromString(EventId));
            }

            if (EventToken != null)
            {
                node.Set("event_token", JsonNode.FromString(EventToken));
            }

            for (int i = 0; i < Extras.Count; i++)
            {
                node.Set(Extras[i].Key, JsonNode.FromString(Extras[i].Value));
            }

            return BackendJson.Write(node);
        }

        public static bool TryParse(string? json, out SsvCallback? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static SsvCallback Read(JsonNode node)
        {
            var r = new ObjectReader(node, "rewardedSsv.request", strict: false);
            string userId = r.RequiredString("user_id", ContractRules.IsFirebaseUid);
            string customData = r.RequiredString("custom_data", ContractRules.IsCustomData);
            string? network = r.OptionalString("network", n => n != null && n.Length <= 64);
            string? eventId = r.OptionalString("event_id", ContractRules.IsEventId);
            string? eventToken = r.OptionalString("event_token", ContractRules.IsEventToken);
            var extras = new List<KeyValuePair<string, string>>();
            IReadOnlyList<KeyValuePair<string, JsonNode>> rest = r.UnreadMembers();
            for (int i = 0; i < rest.Count; i++)
            {
                extras.Add(new KeyValuePair<string, string>(
                    rest[i].Key, r.AsString(rest[i].Value, rest[i].Key, v => v != null && v.Length <= MaxExtraLength)));
            }

            int bar = customData.IndexOf('|');
            return new SsvCallback(userId, customData.Substring(0, bar), customData.Substring(bar + 1), network, eventId, eventToken, extras);
        }

        private static bool IsKnownKey(string key)
        {
            return key == "user_id" || key == "custom_data" || key == "network" || key == "event_id" || key == "event_token";
        }
    }

    /// <summary>Estado de la respuesta del SSV: <c>ok</c> o <c>duplicate</c> (nonce repetido, idempotente).</summary>
    public sealed class SsvResponse
    {
        public static readonly SsvResponse Ok = new SsvResponse(false);

        public static readonly SsvResponse Duplicate = new SsvResponse(true);

        private SsvResponse(bool duplicate)
        {
            IsDuplicate = duplicate;
        }

        public bool IsDuplicate { get; }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject().Set("status", JsonNode.FromString(IsDuplicate ? "duplicate" : "ok")));

        public static bool TryParseStrict(string? json, out SsvResponse? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static SsvResponse Read(JsonNode node)
        {
            var r = new ObjectReader(node, "rewardedSsv.response", strict: true);
            string status = r.RequiredString("status");
            r.End();
            if (status == "ok")
            {
                return Ok;
            }

            if (status == "duplicate")
            {
                return Duplicate;
            }

            throw new ContractException("rewardedSsv.response: 'status' desconocido.");
        }
    }

    /// <summary>
    /// Entrada del job diario <c>voidedPurchasesJob</c> (<c>voided-purchases-job.input.schema.json</c>): un objeto vacío. Para
    /// los tests de contrato.
    /// </summary>
    public sealed class VoidedPurchasesJobInput
    {
        public static readonly VoidedPurchasesJobInput Empty = new VoidedPurchasesJobInput();

        private VoidedPurchasesJobInput()
        {
        }

        public string ToJson() => "{}";

        public static bool TryParseStrict(string? json, out VoidedPurchasesJobInput? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static VoidedPurchasesJobInput Read(JsonNode node)
        {
            new ObjectReader(node, "voidedPurchasesJob.input", strict: true).End();
            return Empty;
        }
    }

    /// <summary>
    /// Resultado del job diario (<c>voided-purchases-job.output.schema.json</c>). Lo devuelve
    /// <see cref="FakeBackendClient.RunVoidedPurchasesJob"/> y lo leen los tests de contrato.
    /// </summary>
    public sealed class VoidedPurchasesJobOutput
    {
        public VoidedPurchasesJobOutput(bool notImplemented, long? pages = null, long? voided = null, long? cursorMs = null)
        {
            if (pages < 0 || voided < 0 || cursorMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pages));
            }

            NotImplemented = notImplemented;
            Pages = pages;
            Voided = voided;
            CursorMs = cursorMs;
        }

        /// <summary><c>not_implemented</c> (solo mientras la función es un esqueleto); si no, <c>ok</c>.</summary>
        public bool NotImplemented { get; }

        public long? Pages { get; }

        public long? Voided { get; }

        public long? CursorMs { get; }

        public string ToJson()
        {
            JsonNode node = JsonNode.NewObject().Set("status", JsonNode.FromString(NotImplemented ? "not_implemented" : "ok"));
            if (Pages.HasValue)
            {
                node.Set("pages", JsonNode.FromLong(Pages.Value));
            }

            if (Voided.HasValue)
            {
                node.Set("voided", JsonNode.FromLong(Voided.Value));
            }

            if (CursorMs.HasValue)
            {
                node.Set("cursor_ms", JsonNode.FromLong(CursorMs.Value));
            }

            return BackendJson.Write(node);
        }

        public static bool TryParseStrict(string? json, out VoidedPurchasesJobOutput? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static VoidedPurchasesJobOutput Read(JsonNode node)
        {
            var r = new ObjectReader(node, "voidedPurchasesJob.output", strict: true);
            string status = r.RequiredString("status");
            if (status != "ok" && status != "not_implemented")
            {
                throw new ContractException("voidedPurchasesJob.output: 'status' desconocido.");
            }

            long? pages = r.OptionalInteger("pages", 0, long.MaxValue);
            long? voided = r.OptionalInteger("voided", 0, long.MaxValue);
            long? cursor = r.OptionalInteger("cursor_ms", 0, long.MaxValue);
            r.End();
            return new VoidedPurchasesJobOutput(status == "not_implemented", pages, voided, cursor);
        }
    }
}
