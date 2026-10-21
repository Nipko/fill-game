#nullable enable
using System;
using AF.Backend.Contract;

namespace AF.Backend
{
    /// <summary>
    /// Cliente de la API v1 del backend propio en AWS (D-035; plan §7; contrato único <c>infra/contracts/v1</c>, Q-AWS-12). Lo
    /// usan <c>AF.IAP</c>, <c>AF.Save</c> y <c>AF.RemoteConfig</c> (08 §2.2). Implementaciones: <see cref="HttpBackendClient"/>
    /// (HTTPS con el ID token de Firebase y, donde toca, el token de integridad), <see cref="FakeBackendClient"/> (editor, tests y
    /// builds de desarrollo sin URL) y <see cref="UnavailableBackendClient"/> (release sin URL).
    /// Cada método llama a <c>onDone</c> exactamente una vez, en el hilo principal; las implementaciones falsas pueden llamarlo
    /// antes de volver.
    /// </summary>
    public interface IBackendClient
    {
        /// <summary><c>POST /v1/purchases/verify</c> (08 §10.3), con integridad.</summary>
        void VerifyPurchase(VerifyPurchaseRequest request, Action<BackendResult<VerifyPurchaseResponse>> onDone);

        /// <summary>
        /// <c>POST /v1/ledger/restore</c> (08 §11, fila Conflicto, y §10.2): libro mayor del UID (tokens con su estado) y los
        /// nonces de <paramref name="request"/> que el servidor tiene confirmados.
        /// </summary>
        void RestoreLedger(RestoreLedgerRequest request, Action<BackendResult<RestoreLedgerResponse>> onDone);

        /// <summary>
        /// <c>POST /v1/account/code</c> (Q-B13-21): emite un código nuevo de borrado por web e invalida el anterior [P]. El
        /// cliente lo pide una vez, lo guarda y solo vuelve a pedirlo si lo perdió.
        /// </summary>
        void GetAccountCode(Action<BackendResult<AccountCodeResponse>> onDone);

        /// <summary><c>POST /v1/account/delete</c> (08 §11), con integridad y <c>{"confirm": true}</c>. Idempotente.</summary>
        void DeleteAccount(Action<BackendResult<DeleteAccountResponse>> onDone);
    }

    /// <summary>Una operación de la API v1 tal como la fija <c>infra/contracts/v1/index.json</c>.</summary>
    public sealed class BackendOperation
    {
        private BackendOperation(string name, string method, string path, bool integrity)
        {
            Name = name;
            Method = method;
            Path = path;
            RequiresIntegrity = integrity;
        }

        public static readonly BackendOperation VerifyPurchase = new BackendOperation("verifyPurchase", "POST", "/v1/purchases/verify", true);

        public static readonly BackendOperation RestoreLedger = new BackendOperation("restoreLedger", "POST", "/v1/ledger/restore", false);

        public static readonly BackendOperation GetAccountCode = new BackendOperation("getAccountCode", "POST", "/v1/account/code", false);

        public static readonly BackendOperation DeleteAccount = new BackendOperation("deleteAccount", "POST", "/v1/account/delete", true);

        /// <summary>Las que usa la app (todas con <c>auth: firebase</c>).</summary>
        public static readonly BackendOperation[] AppOperations = { VerifyPurchase, RestoreLedger, GetAccountCode, DeleteAccount };

        /// <summary>Nombre de <c>operation</c> en <c>index.json</c>.</summary>
        public string Name { get; }

        public string Method { get; }

        public string Path { get; }

        /// <summary><c>integrity: true</c>: lleva <c>X-Firebase-AppCheck</c>.</summary>
        public bool RequiresIntegrity { get; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// <see cref="IBackendClient"/> de un build de release sin URL base: toda llamada falla con
    /// <see cref="BackendErrorKind.NotConfigured"/>. Nunca concede nada (a diferencia de <see cref="FakeBackendClient"/>).
    /// </summary>
    public sealed class UnavailableBackendClient : IBackendClient
    {
        public void VerifyPurchase(VerifyPurchaseRequest request, Action<BackendResult<VerifyPurchaseResponse>> onDone) =>
            Fail(onDone);

        public void RestoreLedger(RestoreLedgerRequest request, Action<BackendResult<RestoreLedgerResponse>> onDone) =>
            Fail(onDone);

        public void GetAccountCode(Action<BackendResult<AccountCodeResponse>> onDone) => Fail(onDone);

        public void DeleteAccount(Action<BackendResult<DeleteAccountResponse>> onDone) => Fail(onDone);

        private static void Fail<T>(Action<BackendResult<T>> onDone)
            where T : class
        {
            (onDone ?? throw new ArgumentNullException(nameof(onDone)))(BackendResult<T>.Fail(BackendError.NotConfigured()));
        }
    }
}
