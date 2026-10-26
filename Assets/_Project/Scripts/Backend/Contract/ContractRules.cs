#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// El cuerpo no cumple el contrato v1 (<c>infra/contracts/v1</c>). El mensaje dice qué campo falla y nunca copia su valor
    /// (podría ser un token o el UID).
    /// </summary>
    public sealed class ContractException : FormatException
    {
        public ContractException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Formatos de <c>infra/contracts/v1/common.schema.json</c> y de los demás esquemas, con los mismos patrones (anclados con
    /// <c>\A</c> y <c>\z</c>: en .NET, <c>$</c> admite un salto de línea final y JSON Schema no).
    /// </summary>
    public static class ContractRules
    {
        private const RegexOptions Options = RegexOptions.CultureInvariant;

        private static readonly Regex SkuRe = new Regex(@"\Aaf_[a-z0-9_]{1,60}\z", Options);
        private static readonly Regex PurchaseTokenRe = new Regex(@"\A[A-Za-z0-9._:-]+\z", Options);
        private static readonly Regex TokenHashRe = new Regex(@"\A[0-9a-f]{64}\z", Options);
        private static readonly Regex NonceRe = new Regex(@"\A[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\z", Options);
        private static readonly Regex FirebaseUidRe = new Regex(@"\A[A-Za-z0-9]{1,128}\z", Options);
        private static readonly Regex PlacementRe = new Regex(@"\A[a-z0-9_]{1,40}\z", Options);
        private static readonly Regex AccountCodeRe = new Regex(@"\A[0-7][0-9A-HJKMNP-TV-Z]{25}\z", Options);
        private static readonly Regex RequestTokenRe = new Regex(@"\A[A-Za-z0-9_-]{43}\z", Options);
        private static readonly Regex ItemIdRe = new Regex(@"\A[a-z0-9_]{1,40}\z", Options);
        private static readonly Regex TransactionIdRe = new Regex(@"\A[A-Za-z0-9._:-]{1,256}\z", Options);
        private static readonly Regex CustomDataRe = new Regex(
            @"\A[a-z0-9_]{1,40}\|[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\z", Options);

        /// <summary>Longitud máxima de <c>purchaseToken</c>.</summary>
        public const int PurchaseTokenMaxLength = 4096;

        /// <summary>Longitud máxima de <c>obfuscated_account_id</c>.</summary>
        public const int ObfuscatedAccountIdMaxLength = 64;

        /// <summary>Máximo de nonces en <c>pending_nonces</c> y <c>confirmed_nonces</c>.</summary>
        public const int MaxNonces = 200;

        /// <summary>SKU del catálogo IAP (05 §4): <c>af_…</c>.</summary>
        public static bool IsSku(string? value) => value != null && SkuRe.IsMatch(value);

        /// <summary><c>purchaseToken</c> de Google Play Billing.</summary>
        public static bool IsPurchaseToken(string? value) =>
            value != null && value.Length >= 1 && value.Length <= PurchaseTokenMaxLength && PurchaseTokenRe.IsMatch(value);

        /// <summary>SHA-256 de un <c>purchaseToken</c> en hexadecimal en minúsculas.</summary>
        public static bool IsTokenHash(string? value) => value != null && TokenHashRe.IsMatch(value);

        /// <summary>Nonce de recompensa: UUID v4 en minúsculas (08 §10.2).</summary>
        public static bool IsNonce(string? value) => value != null && NonceRe.IsMatch(value);

        /// <summary>UID de Firebase Auth.</summary>
        public static bool IsFirebaseUid(string? value) => value != null && FirebaseUidRe.IsMatch(value);

        /// <summary>Id de emplazamiento de anuncio.</summary>
        public static bool IsPlacement(string? value) => value != null && PlacementRe.IsMatch(value);

        /// <summary>Código de borrado por web (Q-B13-21): 26 caracteres de Base32 de Crockford.</summary>
        public static bool IsAccountCode(string? value) => value != null && AccountCodeRe.IsMatch(value);

        /// <summary>Token de confirmación del borrado por web (base64url de 43 caracteres).</summary>
        public static bool IsRequestToken(string? value) => value != null && RequestTokenRe.IsMatch(value);

        /// <summary>Id de un ítem de <c>grant.items</c> o de una skin.</summary>
        public static bool IsItemId(string? value) => value != null && ItemIdRe.IsMatch(value);

        /// <summary><c>obfuscated_account_id</c>: de 1 a 64 caracteres.</summary>
        public static bool IsObfuscatedAccountId(string? value) =>
            value != null && value.Length >= 1 && value.Length <= ObfuscatedAccountIdMaxLength;

        /// <summary><c>transaction_id</c> del SSV de AdMob (D-049).</summary>
        internal static bool IsTransactionId(string? value) => value != null && TransactionIdRe.IsMatch(value);

        internal static bool IsCustomData(string? value) => value != null && CustomDataRe.IsMatch(value);

        /// <summary>Lee el documento raíz; texto que no es JSON → <see cref="ContractException"/>.</summary>
        internal static JsonNode ParseRoot(string? json)
        {
            if (json == null)
            {
                throw new ContractException("Cuerpo vacío.");
            }

            try
            {
                return BackendJson.Parse(json);
            }
            catch (FormatException e)
            {
                throw new ContractException(e.Message);
            }
        }

        /// <summary>Ejecuta <paramref name="read"/> y convierte cualquier fallo del contrato en <c>false</c> con su motivo.</summary>
        internal static bool TryRead<T>(string? json, Func<JsonNode, T> read, out T? value, out string? error)
            where T : class
        {
            try
            {
                value = read(ParseRoot(json));
                error = null;
                return true;
            }
            catch (ContractException e)
            {
                value = null;
                error = e.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Lee los miembros de un objeto JSON comprobando tipos y patrones. En modo estricto (peticiones, como
    /// <c>additionalProperties: false</c>) una clave desconocida falla; en las respuestas del servidor se ignora, para que un
    /// campo opcional nuevo de la v1 no rompa clientes ya publicados.
    /// </summary>
    internal sealed class ObjectReader
    {
        private readonly JsonNode _node;
        private readonly string _where;
        private readonly bool _strict;
        private readonly List<string> _read = new List<string>();

        public ObjectReader(JsonNode node, string where, bool strict)
        {
            if (node.Kind != JsonKind.Object)
            {
                throw new ContractException(where + ": se esperaba un objeto.");
            }

            _node = node;
            _where = where;
            _strict = strict;
        }

        public bool Has(string key) => _node.Get(key) != null;

        public JsonNode? Optional(string key)
        {
            _read.Add(key);
            return _node.Get(key);
        }

        public JsonNode Required(string key)
        {
            JsonNode? value = Optional(key);
            if (value == null)
            {
                throw new ContractException(_where + ": falta '" + key + "'.");
            }

            return value;
        }

        public string RequiredString(string key, Func<string?, bool>? isValid = null)
        {
            return AsString(Required(key), key, isValid);
        }

        public string? OptionalString(string key, Func<string?, bool>? isValid = null)
        {
            JsonNode? value = Optional(key);
            return value == null ? null : AsString(value, key, isValid);
        }

        public long RequiredInteger(string key, long min, long max)
        {
            return AsInteger(Required(key), key, min, max);
        }

        public long? OptionalInteger(string key, long min, long max)
        {
            JsonNode? value = Optional(key);
            return value == null ? (long?)null : AsInteger(value, key, min, max);
        }

        public bool? OptionalBool(string key)
        {
            JsonNode? value = Optional(key);
            if (value == null)
            {
                return null;
            }

            if (value.Kind != JsonKind.Bool)
            {
                throw new ContractException(_where + ": '" + key + "' debe ser booleano.");
            }

            return value.BoolValue;
        }

        public JsonNode RequiredArray(string key, int maxItems)
        {
            return AsArray(Required(key), key, maxItems);
        }

        public JsonNode? OptionalArray(string key, int maxItems)
        {
            JsonNode? value = Optional(key);
            return value == null ? null : AsArray(value, key, maxItems);
        }

        /// <summary>Lista de cadenas válidas y distintas (<c>uniqueItems</c>).</summary>
        public List<string> StringSet(JsonNode array, string key, Func<string?, bool> isValid)
        {
            var result = new List<string>(array.Items.Count);
            for (int i = 0; i < array.Items.Count; i++)
            {
                string value = AsString(array.Items[i], key + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", isValid);
                if (result.Contains(value))
                {
                    throw new ContractException(_where + ": '" + key + "' tiene elementos repetidos.");
                }

                result.Add(value);
            }

            return result;
        }

        /// <summary>En modo estricto, falla si el objeto tiene claves que no se han leído.</summary>
        public void End()
        {
            if (!_strict)
            {
                return;
            }

            for (int i = 0; i < _node.Members.Count; i++)
            {
                if (!_read.Contains(_node.Members[i].Key))
                {
                    throw new ContractException(_where + ": clave no admitida '" + _node.Members[i].Key + "'.");
                }
            }
        }

        public IReadOnlyList<KeyValuePair<string, JsonNode>> UnreadMembers()
        {
            var rest = new List<KeyValuePair<string, JsonNode>>();
            for (int i = 0; i < _node.Members.Count; i++)
            {
                if (!_read.Contains(_node.Members[i].Key))
                {
                    rest.Add(_node.Members[i]);
                }
            }

            return rest;
        }

        public string AsString(JsonNode value, string key, Func<string?, bool>? isValid)
        {
            if (value.Kind != JsonKind.String)
            {
                throw new ContractException(_where + ": '" + key + "' debe ser una cadena.");
            }

            string text = value.Text ?? string.Empty;
            if (isValid != null && !isValid(text))
            {
                throw new ContractException(_where + ": '" + key + "' no tiene el formato del contrato.");
            }

            return text;
        }

        private long AsInteger(JsonNode value, string key, long min, long max)
        {
            if (value.Kind != JsonKind.Number)
            {
                throw new ContractException(_where + ": '" + key + "' debe ser un entero.");
            }

            string text = value.Text ?? string.Empty;
            long parsed;
            if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out parsed))
            {
                // JSON Schema admite 1.0 como entero: se acepta un decimal sin parte fraccionaria.
                if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal d)
                    || d != decimal.Truncate(d) || d < long.MinValue || d > long.MaxValue)
                {
                    throw new ContractException(_where + ": '" + key + "' debe ser un entero.");
                }

                parsed = (long)d;
            }

            if (parsed < min || parsed > max)
            {
                throw new ContractException(_where + ": '" + key + "' está fuera de rango.");
            }

            return parsed;
        }

        private JsonNode AsArray(JsonNode value, string key, int maxItems)
        {
            if (value.Kind != JsonKind.Array)
            {
                throw new ContractException(_where + ": '" + key + "' debe ser una lista.");
            }

            if (value.Items.Count > maxItems)
            {
                throw new ContractException(_where + ": '" + key + "' tiene más de " + maxItems.ToString(CultureInfo.InvariantCulture) + " elementos.");
            }

            return value;
        }
    }
}
