#nullable enable
using System;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>Código de error de la API v1 (<c>error.schema.json</c>).</summary>
    public enum ApiErrorCode
    {
        /// <summary>Sin código: el cuerpo no era un error del contrato (p. ej. una página de un proxy).</summary>
        Unknown = 0,
        BadRequest,
        Unauthorized,
        IntegrityFailed,
        TokenBelongsToOtherUser,
        BadSignature,
        CodeNotFound,
        RequestExpired,
        RateLimited,
        NotImplemented,
        UpstreamError,
        InternalError,

        /// <summary><c>country_unknown</c>: <c>GET /v1/league?scope=country</c> sin país (404; D-046).</summary>
        CountryUnknown,
    }

    /// <summary>
    /// Cuerpo de cualquier respuesta 4xx o 5xx (<c>error.schema.json</c>): <c>{"error": {code, message, retry_after_s?}}</c>.
    /// Nunca lleva el UID ni el token.
    /// </summary>
    public sealed class ApiError
    {
        /// <summary>Longitud máxima de <see cref="Message"/>.</summary>
        public const int MaxMessageLength = 300;

        public ApiError(ApiErrorCode code, string message, int? retryAfterS = null)
        {
            if (code == ApiErrorCode.Unknown)
            {
                throw new ArgumentException("Un error del contrato tiene código.", nameof(code));
            }

            if (message == null || message.Length > MaxMessageLength)
            {
                throw new ArgumentException("Mensaje nulo o de más de 300 caracteres.", nameof(message));
            }

            if (retryAfterS.HasValue && (retryAfterS.Value < 1 || retryAfterS.Value > 86400))
            {
                throw new ArgumentOutOfRangeException(nameof(retryAfterS));
            }

            Code = code;
            Message = message;
            RetryAfterS = retryAfterS;
        }

        public ApiErrorCode Code { get; }

        /// <summary>Mensaje del servidor, para diagnóstico (no se enseña al jugador: no está traducido).</summary>
        public string Message { get; }

        /// <summary>Segundos que pide esperar el servidor (con <c>rate_limited</c>).</summary>
        public int? RetryAfterS { get; }

        /// <summary>Texto del contrato de <paramref name="code"/> (<c>bad_request</c>…).</summary>
        public static string CodeToWire(ApiErrorCode code)
        {
            switch (code)
            {
                case ApiErrorCode.BadRequest: return "bad_request";
                case ApiErrorCode.Unauthorized: return "unauthorized";
                case ApiErrorCode.IntegrityFailed: return "integrity_failed";
                case ApiErrorCode.TokenBelongsToOtherUser: return "token_belongs_to_other_user";
                case ApiErrorCode.BadSignature: return "bad_signature";
                case ApiErrorCode.CodeNotFound: return "code_not_found";
                case ApiErrorCode.RequestExpired: return "request_expired";
                case ApiErrorCode.RateLimited: return "rate_limited";
                case ApiErrorCode.NotImplemented: return "not_implemented";
                case ApiErrorCode.UpstreamError: return "upstream_error";
                case ApiErrorCode.InternalError: return "internal_error";
                case ApiErrorCode.CountryUnknown: return "country_unknown";
                default: throw new ArgumentOutOfRangeException(nameof(code));
            }
        }

        /// <summary>Código de <paramref name="wire"/>, o <see cref="ApiErrorCode.Unknown"/> si no es del contrato.</summary>
        public static ApiErrorCode CodeFromWire(string? wire)
        {
            switch (wire)
            {
                case "bad_request": return ApiErrorCode.BadRequest;
                case "unauthorized": return ApiErrorCode.Unauthorized;
                case "integrity_failed": return ApiErrorCode.IntegrityFailed;
                case "token_belongs_to_other_user": return ApiErrorCode.TokenBelongsToOtherUser;
                case "bad_signature": return ApiErrorCode.BadSignature;
                case "code_not_found": return ApiErrorCode.CodeNotFound;
                case "request_expired": return ApiErrorCode.RequestExpired;
                case "rate_limited": return ApiErrorCode.RateLimited;
                case "not_implemented": return ApiErrorCode.NotImplemented;
                case "upstream_error": return ApiErrorCode.UpstreamError;
                case "internal_error": return ApiErrorCode.InternalError;
                case "country_unknown": return ApiErrorCode.CountryUnknown;
                default: return ApiErrorCode.Unknown;
            }
        }

        public string ToJson()
        {
            JsonNode inner = JsonNode.NewObject()
                .Set("code", JsonNode.FromString(CodeToWire(Code)))
                .Set("message", JsonNode.FromString(Message));
            if (RetryAfterS.HasValue)
            {
                inner.Set("retry_after_s", JsonNode.FromLong(RetryAfterS.Value));
            }

            return BackendJson.Write(JsonNode.NewObject().Set("error", inner));
        }

        public static bool TryParse(string? json, out ApiError? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        public static bool TryParseStrict(string? json, out ApiError? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static ApiError Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "error", strict);
            var inner = new ObjectReader(r.Required("error"), "error.error", strict);
            ApiErrorCode code = CodeFromWire(inner.RequiredString("code"));
            if (code == ApiErrorCode.Unknown)
            {
                throw new ContractException("error: 'code' desconocido.");
            }

            string message = inner.RequiredString("message", m => m != null && m.Length <= MaxMessageLength);
            long? retryAfter = inner.OptionalInteger("retry_after_s", 1, 86400);
            inner.End();
            r.End();
            return new ApiError(code, message, retryAfter.HasValue ? (int)retryAfter.Value : (int?)null);
        }
    }
}
