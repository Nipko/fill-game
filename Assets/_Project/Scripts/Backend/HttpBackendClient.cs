#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Backend.Auth;
using AF.Backend.Contract;
using AF.Backend.Http;
using AF.Platform;

namespace AF.Backend
{
    /// <summary>
    /// <see cref="IBackendClient"/> por HTTPS contra la API v1 (contrato <c>infra/contracts/v1/index.json</c>; plan §7):
    /// <list type="bullet">
    /// <item><c>Authorization: Bearer &lt;ID token&gt;</c> en todas (<c>auth: firebase</c>); ante un 401 pide un token nuevo una
    /// sola vez y repite.</item>
    /// <item><c>X-Firebase-AppCheck</c> con un token nuevo de uso limitado en cada intento de las rutas con
    /// <c>integrity: true</c> (Q-AWS-4).</item>
    /// <item>Reintentos de los fallos pasajeros con <see cref="RetryPolicy.InCall"/>.</item>
    /// <item>La cabecera <c>Date</c> de toda respuesta va a <see cref="ServerDateTracker"/> (08 §10.1); un fallo de red lo deja
    /// sin medición.</item>
    /// </list>
    /// No escribe nada en los logs: los errores vuelven en <see cref="BackendResult{T}"/> sin tokens, UID ni URL.
    /// </summary>
    public sealed class HttpBackendClient : IBackendClient
    {
        /// <summary>Tiempo máximo de cada intento [P].</summary>
        public const int DefaultTimeoutSeconds = 15;

        private const string JsonContentType = "application/json; charset=utf-8";

        private readonly string _baseUrl;
        private readonly IHttpTransport _transport;
        private readonly IAuthTokenProvider _auth;
        private readonly IIntegrityTokenProvider _integrity;
        private readonly IBackendScheduler _scheduler;
        private readonly ServerDateTracker? _serverDate;
        private readonly RetryPolicy _retry;
        private readonly int _timeoutSeconds;

        /// <param name="baseUrl">URL base del entorno (compilada, nunca de Remote Config; plan §7); ver <see cref="BackendEndpoint"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="baseUrl"/> no es una URL HTTPS válida.</exception>
        public HttpBackendClient(
            string baseUrl,
            IHttpTransport transport,
            IAuthTokenProvider auth,
            IIntegrityTokenProvider integrity,
            IBackendScheduler scheduler,
            ServerDateTracker? serverDate = null,
            RetryPolicy? retry = null,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            string? normalized = BackendEndpoint.Normalize(baseUrl);
            if (normalized == null)
            {
                throw new ArgumentException("URL base inválida (se exige https://host[/etapa] sin consulta ni fragmento).", nameof(baseUrl));
            }

            if (timeoutSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            _baseUrl = normalized;
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _integrity = integrity ?? throw new ArgumentNullException(nameof(integrity));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _serverDate = serverDate;
            _retry = retry ?? RetryPolicy.InCall;
            _timeoutSeconds = timeoutSeconds;
        }

        public void VerifyPurchase(VerifyPurchaseRequest request, Action<BackendResult<VerifyPurchaseResponse>> onDone)
        {
            string body = (request ?? throw new ArgumentNullException(nameof(request))).ToJson();
            Start(BackendOperation.VerifyPurchase, body, VerifyPurchaseResponse.TryParse, onDone);
        }

        public void RestoreLedger(RestoreLedgerRequest request, Action<BackendResult<RestoreLedgerResponse>> onDone)
        {
            string body = (request ?? throw new ArgumentNullException(nameof(request))).ToJson();
            Start(BackendOperation.RestoreLedger, body, RestoreLedgerResponse.TryParse, onDone);
        }

        public void GetAccountCode(Action<BackendResult<AccountCodeResponse>> onDone)
        {
            Start(BackendOperation.GetAccountCode, null, AccountCodeResponse.TryParse, onDone);
        }

        public void DeleteAccount(Action<BackendResult<DeleteAccountResponse>> onDone)
        {
            Start(BackendOperation.DeleteAccount, DeleteAccountRequest.Confirmed.ToJson(), DeleteAccountResponse.TryParse, onDone);
        }

        private delegate bool Parser<T>(string? json, out T? value, out string? error)
            where T : class;

        private void Start<T>(BackendOperation op, string? body, Parser<T> parse, Action<BackendResult<T>> onDone)
            where T : class
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            new Call<T>(this, op, body, parse, onDone).Attempt(forceRefresh: false);
        }

        /// <summary>Una llamada con sus intentos.</summary>
        private sealed class Call<T>
            where T : class
        {
            private readonly HttpBackendClient _owner;
            private readonly BackendOperation _op;
            private readonly string? _body;
            private readonly Parser<T> _parse;
            private readonly Action<BackendResult<T>> _onDone;
            private int _failedAttempts;
            private bool _refreshedAuth;
            private bool _done;

            public Call(HttpBackendClient owner, BackendOperation op, string? body, Parser<T> parse, Action<BackendResult<T>> onDone)
            {
                _owner = owner;
                _op = op;
                _body = body;
                _parse = parse;
                _onDone = onDone;
            }

            public void Attempt(bool forceRefresh)
            {
                _owner._auth.GetIdToken(forceRefresh, OnIdToken);
            }

            private void OnIdToken(TokenResult id)
            {
                if (!id.IsOk)
                {
                    Finish(BackendResult<T>.Fail(new BackendError(BackendErrorKind.AuthUnavailable, detail: id.Failure)));
                    return;
                }

                if (!_op.RequiresIntegrity)
                {
                    Send(id.Token!, null);
                    return;
                }

                _owner._integrity.GetLimitedUseToken(integrity =>
                {
                    if (!integrity.IsOk)
                    {
                        Finish(BackendResult<T>.Fail(new BackendError(BackendErrorKind.IntegrityUnavailable, detail: integrity.Failure)));
                        return;
                    }

                    Send(id.Token!, integrity.Token);
                });
            }

            private void Send(string idToken, string? integrityToken)
            {
                var headers = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Authorization", "Bearer " + idToken),
                    new KeyValuePair<string, string>("Accept", "application/json"),
                };
                if (_body != null)
                {
                    headers.Add(new KeyValuePair<string, string>("Content-Type", JsonContentType));
                }

                if (integrityToken != null)
                {
                    headers.Add(new KeyValuePair<string, string>("X-Firebase-AppCheck", integrityToken));
                }

                var request = new HttpRequestData(_op.Method, _owner._baseUrl + _op.Path, headers, _body, _owner._timeoutSeconds);
                _owner._transport.Send(request, OnResponse);
            }

            private void OnResponse(HttpResponseData response)
            {
                if (_done)
                {
                    return;
                }

                if (!response.HasResponse)
                {
                    _owner._serverDate?.MarkUnreachable();
                    RetryOrFail(BackendError.Network(response.TransportError));
                    return;
                }

                _owner._serverDate?.ObserveDateHeader(response.GetHeader("Date"));
                int status = response.StatusCode;
                if (status >= 200 && status < 300)
                {
                    if (_parse(response.Body, out T? value, out string? error))
                    {
                        Finish(BackendResult<T>.Ok(value!));
                    }
                    else
                    {
                        Finish(BackendResult<T>.Fail(new BackendError(BackendErrorKind.InvalidResponse, status, detail: error)));
                    }

                    return;
                }

                if (status == 401 && !_refreshedAuth)
                {
                    // El ID token caducó o lo revocaron: uno nuevo y se repite, sin contarlo como intento.
                    _refreshedAuth = true;
                    Attempt(forceRefresh: true);
                    return;
                }

                RetryOrFail(ErrorFrom(response));
            }

            private void RetryOrFail(BackendError error)
            {
                _failedAttempts++;
                if (_owner._retry.ShouldRetry(_failedAttempts, error, out TimeSpan delay))
                {
                    _owner._scheduler.After(delay, () => Attempt(forceRefresh: false));
                    return;
                }

                Finish(BackendResult<T>.Fail(error));
            }

            private void Finish(BackendResult<T> result)
            {
                if (_done)
                {
                    return;
                }

                _done = true;
                _onDone(result);
            }
        }

        /// <summary>Error de una respuesta no 2xx: el cuerpo del contrato si lo es; si no, solo el código HTTP.</summary>
        internal static BackendError ErrorFrom(HttpResponseData response)
        {
            TimeSpan? retryAfter = ParseRetryAfter(response.GetHeader("Retry-After"));
            if (ApiError.TryParse(response.Body, out ApiError? body, out _) && body != null)
            {
                if (body.RetryAfterS.HasValue)
                {
                    retryAfter = TimeSpan.FromSeconds(body.RetryAfterS.Value);
                }

                return BackendError.Http(response.StatusCode, body.Code, body.Message, retryAfter);
            }

            return BackendError.Http(response.StatusCode, ApiErrorCode.Unknown, null, retryAfter);
        }

        /// <summary><c>Retry-After</c> en segundos (la forma con fecha no la usa la API v1).</summary>
        internal static TimeSpan? ParseRetryAfter(string? value)
        {
            if (value != null
                && int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int seconds)
                && seconds >= 0 && seconds <= 86400)
            {
                return TimeSpan.FromSeconds(seconds);
            }

            return null;
        }
    }
}
