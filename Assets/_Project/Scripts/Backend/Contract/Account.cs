#nullable enable
using System;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// <c>POST /v1/account/code</c>, respuesta 200 (<c>account-code.response.schema.json</c>; Q-B13-21): código nuevo de borrado
    /// por web, que Ajustes enseña en lugar del UID. Cada llamada invalida el anterior [P]. La petición no lleva cuerpo.
    /// </summary>
    public sealed class AccountCodeResponse
    {
        public AccountCodeResponse(string code)
        {
            if (!ContractRules.IsAccountCode(code))
            {
                throw new ArgumentException("Código fuera del contrato.", nameof(code));
            }

            Code = code;
        }

        /// <summary>26 caracteres de Base32 de Crockford.</summary>
        public string Code { get; }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject().Set("code", JsonNode.FromString(Code)));

        public static bool TryParse(string? json, out AccountCodeResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        public static bool TryParseStrict(string? json, out AccountCodeResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static AccountCodeResponse Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "accountCode.response", strict);
            string code = r.RequiredString("code", ContractRules.IsAccountCode);
            r.End();
            return new AccountCodeResponse(code);
        }
    }

    /// <summary>
    /// <c>POST /v1/account/delete</c>, petición (<c>account-delete.request.schema.json</c>; 08 §11): la app ya pidió la
    /// confirmación en dos pasos y el cuerpo lo deja explícito con <c>confirm: true</c>.
    /// </summary>
    public sealed class DeleteAccountRequest
    {
        /// <summary>La única petición válida.</summary>
        public static readonly DeleteAccountRequest Confirmed = new DeleteAccountRequest();

        private DeleteAccountRequest()
        {
        }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject().Set("confirm", JsonNode.FromBool(true)));

        public static bool TryParse(string? json, out DeleteAccountRequest? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static DeleteAccountRequest Read(JsonNode node)
        {
            var r = new ObjectReader(node, "deleteAccount.request", strict: true);
            if (r.OptionalBool("confirm") != true)
            {
                throw new ContractException("deleteAccount.request: 'confirm' debe ser true.");
            }

            r.End();
            return Confirmed;
        }
    }

    /// <summary>
    /// Respuesta 200 de <c>POST /v1/account/delete</c> y de <c>/v1/account/delete-confirm</c>
    /// (<c>account-delete.response.schema.json</c>): <c>{"status": "deleted"}</c>. Idempotente.
    /// </summary>
    public sealed class DeleteAccountResponse
    {
        /// <summary>La única respuesta válida.</summary>
        public static readonly DeleteAccountResponse Deleted = new DeleteAccountResponse();

        private DeleteAccountResponse()
        {
        }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject().Set("status", JsonNode.FromString("deleted")));

        public static bool TryParse(string? json, out DeleteAccountResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        public static bool TryParseStrict(string? json, out DeleteAccountResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static DeleteAccountResponse Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "deleteAccount.response", strict);
            if (r.RequiredString("status") != "deleted")
            {
                throw new ContractException("deleteAccount.response: 'status' debe ser 'deleted'.");
            }

            r.End();
            return Deleted;
        }
    }

    /// <summary>
    /// <c>POST /v1/account/delete-request</c> (web), petición (<c>account-delete-request.request.schema.json</c>): el código de
    /// Ajustes, nunca el UID. La usa la web de borrado; aquí está para los tests de contrato y el cliente falso.
    /// </summary>
    public sealed class DeleteRequestRequest
    {
        public DeleteRequestRequest(string code)
        {
            if (!ContractRules.IsAccountCode(code))
            {
                throw new ArgumentException("Código fuera del contrato.", nameof(code));
            }

            Code = code;
        }

        public string Code { get; }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject().Set("code", JsonNode.FromString(Code)));

        public static bool TryParse(string? json, out DeleteRequestRequest? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static DeleteRequestRequest Read(JsonNode node)
        {
            var r = new ObjectReader(node, "deleteRequest.request", strict: true);
            string code = r.RequiredString("code", ContractRules.IsAccountCode);
            r.End();
            return new DeleteRequestRequest(code);
        }
    }

    /// <summary>
    /// <c>POST /v1/account/delete-request</c> (web), respuesta 200 (<c>account-delete-request.response.schema.json</c>): token del
    /// segundo paso, válido <see cref="ExpiresInS"/> segundos (como mucho 300).
    /// </summary>
    public sealed class DeleteRequestResponse
    {
        public DeleteRequestResponse(string requestToken, int expiresInS)
        {
            if (!ContractRules.IsRequestToken(requestToken))
            {
                throw new ArgumentException("Token fuera del contrato.", nameof(requestToken));
            }

            if (expiresInS < 1 || expiresInS > 300)
            {
                throw new ArgumentOutOfRangeException(nameof(expiresInS));
            }

            RequestToken = requestToken;
            ExpiresInS = expiresInS;
        }

        public string RequestToken { get; }

        public int ExpiresInS { get; }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject()
            .Set("request_token", JsonNode.FromString(RequestToken))
            .Set("expires_in_s", JsonNode.FromLong(ExpiresInS)));

        public static bool TryParse(string? json, out DeleteRequestResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        public static bool TryParseStrict(string? json, out DeleteRequestResponse? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static DeleteRequestResponse Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "deleteRequest.response", strict);
            string token = r.RequiredString("request_token", ContractRules.IsRequestToken);
            long expires = r.RequiredInteger("expires_in_s", 1, 300);
            r.End();
            return new DeleteRequestResponse(token, (int)expires);
        }
    }

    /// <summary>
    /// <c>POST /v1/account/delete-confirm</c> (web), petición (<c>account-delete-confirm.request.schema.json</c>): el token de
    /// <c>delete-request</c>. Responde con <see cref="DeleteAccountResponse"/>.
    /// </summary>
    public sealed class DeleteConfirmRequest
    {
        public DeleteConfirmRequest(string requestToken)
        {
            if (!ContractRules.IsRequestToken(requestToken))
            {
                throw new ArgumentException("Token fuera del contrato.", nameof(requestToken));
            }

            RequestToken = requestToken;
        }

        public string RequestToken { get; }

        public string ToJson() => BackendJson.Write(JsonNode.NewObject().Set("request_token", JsonNode.FromString(RequestToken)));

        public static bool TryParse(string? json, out DeleteConfirmRequest? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static DeleteConfirmRequest Read(JsonNode node)
        {
            var r = new ObjectReader(node, "deleteConfirm.request", strict: true);
            string token = r.RequiredString("request_token", ContractRules.IsRequestToken);
            r.End();
            return new DeleteConfirmRequest(token);
        }
    }
}
