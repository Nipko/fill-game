#nullable enable
using System;
using AF.Backend.Auth;
using AF.Backend.Contract;
using AF.Backend.Http;
using AF.Platform;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>F3-1/F3-2: <see cref="HttpBackendClient"/> contra un transporte falso (cabeceras, rutas, reintentos, 401, Date).</summary>
    public sealed class HttpBackendClientTests
    {
        private const string BaseUrl = "https://api.example.test/dev";
        private const string Token = "opaque-token-example.AO-J1OzExample_Only-NotARealPurchaseToken0123456789";
        private static readonly string Hash = PurchaseTokenHash.Of(Token);
        private const string OtherHash = "3f0c6b0e2a5d4c1b9e8f7a6d5c4b3a291807f6e5d4c3b2a1908f7e6d5c4b3a29";
        private const string ServerDate = "Wed, 21 Oct 2026 12:00:00 GMT";

        private FakeHttpTransport _http = null!;
        private FakeAuthTokenProvider _auth = null!;
        private FakeIntegrityTokenProvider _integrity = null!;
        private ManualBackendScheduler _scheduler = null!;
        private ManualClock _clock = null!;
        private ServerDateTracker _serverDate = null!;
        private HttpBackendClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _http = new FakeHttpTransport();
            _auth = new FakeAuthTokenProvider();
            _integrity = new FakeIntegrityTokenProvider();
            _scheduler = new ManualBackendScheduler();
            _clock = new ManualClock(new DateTime(2026, 10, 21, 11, 0, 0, DateTimeKind.Utc));
            _serverDate = new ServerDateTracker(_clock);
            _client = new HttpBackendClient(BaseUrl + "/", _http, _auth, _integrity, _scheduler, _serverDate);
        }

        private static VerifyPurchaseRequest Purchase() => new VerifyPurchaseRequest(Token, "af_coins_2", "a1b2c3d4");

        private static string OkBody() => VerifyPurchaseResponse.Ok(Hash, new PurchaseGrant(1200)).ToJson();

        private BackendResult<T> Capture<T>(Action<Action<BackendResult<T>>> call)
            where T : class
        {
            BackendResult<T>? result = null;
            int calls = 0;
            call(r =>
            {
                calls++;
                result = r;
            });
            Assert.That(calls, Is.EqualTo(1), "onDone debe llamarse una vez");
            return result!;
        }

        [Test]
        public void VerifyPurchaseSendsTheContractRequest()
        {
            _http.Enqueue(200, OkBody(), ServerDate);
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.IsOk, Is.True, r.ToString());
            Assert.That(r.Value!.Status, Is.EqualTo(VerifyStatus.Ok));
            Assert.That(r.Value.Grant!.Seeds, Is.EqualTo(1200));
            HttpRequestData sent = _http.Requests[0];
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(sent.Url, Is.EqualTo(BaseUrl + "/v1/purchases/verify"));
            Assert.That(sent.GetHeader("Authorization"), Is.EqualTo("Bearer " + _auth.CurrentToken));
            Assert.That(sent.GetHeader("X-Firebase-AppCheck"), Is.EqualTo("fake-appcheck-1"));
            Assert.That(sent.GetHeader("Content-Type"), Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(VerifyPurchaseRequest.TryParse(sent.Body, out VerifyPurchaseRequest? body, out _), Is.True);
            Assert.That(body!.Token, Is.EqualTo(Token));
            Assert.That(body.ObfuscatedAccountId, Is.EqualTo("a1b2c3d4"));
        }

        [Test]
        public void RouteHeadersFollowTheIndex()
        {
            _http.Handler = req => new HttpResponseData(200, req.Url.EndsWith("/v1/ledger/restore", StringComparison.Ordinal)
                ? new RestoreLedgerResponse(null, null).ToJson()
                : req.Url.EndsWith("/v1/account/code", StringComparison.Ordinal)
                    ? new AccountCodeResponse("3HZ7M2Q9XK4T8VB1NC6DRF0WPE").ToJson()
                    : DeleteAccountResponse.Deleted.ToJson());

            Assert.That(Capture<RestoreLedgerResponse>(cb => _client.RestoreLedger(new RestoreLedgerRequest(), cb)).IsOk, Is.True);
            Assert.That(Capture<AccountCodeResponse>(cb => _client.GetAccountCode(cb)).Value!.Code, Is.EqualTo("3HZ7M2Q9XK4T8VB1NC6DRF0WPE"));
            Assert.That(Capture<DeleteAccountResponse>(cb => _client.DeleteAccount(cb)).IsOk, Is.True);

            HttpRequestData restore = _http.Requests[0];
            Assert.That(restore.Url, Is.EqualTo(BaseUrl + "/v1/ledger/restore"));
            Assert.That(restore.GetHeader("X-Firebase-AppCheck"), Is.Null, "restoreLedger no lleva integridad");
            Assert.That(restore.Body, Is.EqualTo("{\"pending_nonces\":[]}"));

            HttpRequestData code = _http.Requests[1];
            Assert.That(code.Method, Is.EqualTo("POST"));
            Assert.That(code.Url, Is.EqualTo(BaseUrl + "/v1/account/code"));
            Assert.That(code.Body, Is.Null, "account/code no lleva cuerpo");
            Assert.That(code.GetHeader("Content-Type"), Is.Null);
            Assert.That(code.GetHeader("X-Firebase-AppCheck"), Is.Null);

            HttpRequestData delete = _http.Requests[2];
            Assert.That(delete.Url, Is.EqualTo(BaseUrl + "/v1/account/delete"));
            Assert.That(delete.Body, Is.EqualTo("{\"confirm\":true}"));
            Assert.That(delete.GetHeader("X-Firebase-AppCheck"), Is.Not.Null, "deleteAccount lleva integridad");
            foreach (HttpRequestData req in _http.Requests)
            {
                Assert.That(req.GetHeader("Authorization"), Does.StartWith("Bearer "));
                Assert.That(req.Url, Does.Not.Contain("?"), "nada de identificadores en la URL");
            }
        }

        [Test]
        public void DateHeaderFeedsTheServerDate()
        {
            Assert.That(_serverDate.TryGetServerOffset(out _), Is.False);
            _http.Enqueue(200, OkBody(), ServerDate);
            Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(_serverDate.TryGetServerOffset(out TimeSpan offset), Is.True);
            Assert.That(offset, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void ErrorResponsesAlsoFeedTheServerDate()
        {
            _http.Enqueue(400, new ApiError(ApiErrorCode.BadRequest, "Cuerpo inválido.").ToJson(), ServerDate);
            Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));
            Assert.That(_serverDate.TryGetServerOffset(out TimeSpan offset), Is.True);
            Assert.That(offset, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void NetworkFailureLeavesTheServerDateUnknown()
        {
            _serverDate.Observe(new DateTime(2026, 10, 21, 11, 30, 0, DateTimeKind.Utc));
            _http.Enqueue(HttpResponseData.NetworkFailure("connection_error"));
            var client = new HttpBackendClient(BaseUrl, _http, _auth, _integrity, _scheduler, _serverDate, new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.Network));
            Assert.That(_serverDate.TryGetServerOffset(out _), Is.False);
        }

        [Test]
        public void Unauthorized_RefreshesTheIdTokenOnce()
        {
            _http.Enqueue(401, new ApiError(ApiErrorCode.Unauthorized, "Token caducado.").ToJson());
            _http.Enqueue(200, OkBody());
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.IsOk, Is.True);
            Assert.That(_auth.Refreshes, Is.EqualTo(1));
            Assert.That(_http.Requests[0].GetHeader("Authorization"), Is.EqualTo("Bearer fake-id-token-1"));
            Assert.That(_http.Requests[1].GetHeader("Authorization"), Is.EqualTo("Bearer fake-id-token-2"));
            Assert.That(_http.Requests[1].GetHeader("X-Firebase-AppCheck"), Is.EqualTo("fake-appcheck-2"), "token de integridad nuevo en cada intento");
            Assert.That(_scheduler.Requested, Is.Empty, "el refresco no espera");
        }

        [Test]
        public void Unauthorized_Twice_Fails()
        {
            _http.Enqueue(401, new ApiError(ApiErrorCode.Unauthorized, "No.").ToJson());
            _http.Enqueue(401, new ApiError(ApiErrorCode.Unauthorized, "No.").ToJson());
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.HttpStatus, Is.EqualTo(401));
            Assert.That(r.Error.Code, Is.EqualTo(ApiErrorCode.Unauthorized));
            Assert.That(_auth.Refreshes, Is.EqualTo(1));
            Assert.That(_http.Requests.Count, Is.EqualTo(2));
        }

        [Test]
        public void TransientErrors_RetryWithBackoff()
        {
            _http.Enqueue(HttpResponseData.NetworkFailure("connection_error"));
            _http.Enqueue(503, null);
            _http.Enqueue(200, OkBody());
            BackendResult<VerifyPurchaseResponse>? result = null;
            _client.VerifyPurchase(Purchase(), r => result = r);

            Assert.That(result, Is.Null);
            Assert.That(_scheduler.Requested, Is.EqualTo(new[] { TimeSpan.FromSeconds(1) }));
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(result, Is.Null);
            Assert.That(_scheduler.Requested, Is.EqualTo(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }));
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(result!.IsOk, Is.True);
            Assert.That(_http.Requests.Count, Is.EqualTo(3));
        }

        [Test]
        public void TransientErrors_GiveUpAfterMaxAttempts()
        {
            BackendResult<VerifyPurchaseResponse>? result = null;
            _client.VerifyPurchase(Purchase(), r => result = r);
            _scheduler.Advance(TimeSpan.FromMinutes(1));

            Assert.That(_http.Requests.Count, Is.EqualTo(RetryPolicy.InCall.MaxAttempts));
            Assert.That(result!.Error!.Kind, Is.EqualTo(BackendErrorKind.Network));
            Assert.That(result.Error.IsTransient, Is.True);
        }

        [Test]
        public void RateLimited_WithShortRetryAfter_Waits()
        {
            _http.Enqueue(429, new ApiError(ApiErrorCode.RateLimited, "Demasiadas.", 3).ToJson());
            _http.Enqueue(200, OkBody());
            BackendResult<VerifyPurchaseResponse>? result = null;
            _client.VerifyPurchase(Purchase(), r => result = r);

            Assert.That(_scheduler.Requested, Is.EqualTo(new[] { TimeSpan.FromSeconds(3) }));
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(result!.IsOk, Is.True);
        }

        [Test]
        public void RateLimited_WithLongRetryAfter_ReturnsToTheCaller()
        {
            _http.Enqueue(429, new ApiError(ApiErrorCode.RateLimited, "Demasiadas.", 60).ToJson());
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Code, Is.EqualTo(ApiErrorCode.RateLimited));
            Assert.That(r.Error.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(60)));
            Assert.That(_scheduler.Requested, Is.Empty);
        }

        [Test]
        public void RetryAfterHeaderIsUsedWithoutAContractBody()
        {
            var headers = new[] { new System.Collections.Generic.KeyValuePair<string, string>("Retry-After", "120") };
            _http.Enqueue(new HttpResponseData(429, "<html>throttled</html>", headers));
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Code, Is.EqualTo(ApiErrorCode.Unknown));
            Assert.That(r.Error.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(120)));
        }

        [Test]
        public void DefinitiveErrors_AreNotRetried()
        {
            _http.Enqueue(409, new ApiError(ApiErrorCode.TokenBelongsToOtherUser, "Otro usuario.").ToJson());
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.Http));
            Assert.That(r.Error.HttpStatus, Is.EqualTo(409));
            Assert.That(r.Error.Code, Is.EqualTo(ApiErrorCode.TokenBelongsToOtherUser));
            Assert.That(r.Error.Detail, Is.EqualTo("Otro usuario."));
            Assert.That(_http.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void NotImplemented_IsNotRetriedInTheCall()
        {
            _http.Enqueue(501, BackendTestFiles.ReadExample("error.not-implemented.json"));
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Code, Is.EqualTo(ApiErrorCode.NotImplemented));
            Assert.That(_http.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidSuccessBody_IsAnInvalidResponse()
        {
            _http.Enqueue(200, "{\"status\":\"ok\"}");
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.InvalidResponse));
            Assert.That(r.Error.IsTransient, Is.False);
        }

        [Test]
        public void UnknownResponseFields_AreIgnored()
        {
            _http.Enqueue(200, "{\"status\":\"duplicate\",\"token_hash\":\"" + Hash + "\",\"extra_v1_field\":1}");
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Value!.Status, Is.EqualTo(VerifyStatus.Duplicate));
            Assert.That(r.Value.TokenHash, Is.EqualTo(Hash));
        }

        [Test]
        public void WithoutIdToken_NothingIsSent()
        {
            _auth.FailWith = "auth_not_ready";
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.AuthUnavailable));
            Assert.That(_http.Requests, Is.Empty);
        }

        [Test]
        public void WithoutIntegrityToken_NothingIsSent()
        {
            _integrity.FailWith = "appcheck_failed";
            BackendResult<DeleteAccountResponse> r = Capture<DeleteAccountResponse>(cb => _client.DeleteAccount(cb));

            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.IntegrityUnavailable));
            Assert.That(_http.Requests, Is.Empty);
        }

        [Test]
        public void ErrorsAndRequestsNeverShowTokens()
        {
            _http.Enqueue(400, new ApiError(ApiErrorCode.BadRequest, "Cuerpo inválido.").ToJson());
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));
            string text = r.ToString() + " " + r.Error + " " + _http.Requests[0];

            Assert.That(text, Does.Not.Contain(Token));
            Assert.That(text, Does.Not.Contain(_auth.CurrentToken));
            Assert.That(text, Does.Not.Contain("fake-appcheck"));
            Assert.That(text, Does.Not.Contain("api.example.test"));
            Assert.That(_http.Requests[0].ToString(), Is.EqualTo("POST /dev/v1/purchases/verify"));
        }

        [Test]
        public void BaseUrlMustBeHttps()
        {
            Assert.That(() => new HttpBackendClient("http://api.example.test", _http, _auth, _integrity, _scheduler),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new HttpBackendClient("", _http, _auth, _integrity, _scheduler),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void TokenHashOfAnotherPurchase_IsAnInvalidResponse()
        {
            _http.Enqueue(200, VerifyPurchaseResponse.Duplicate(OtherHash).ToJson());
            BackendResult<VerifyPurchaseResponse> r = Capture<VerifyPurchaseResponse>(cb => _client.VerifyPurchase(Purchase(), cb));

            Assert.That(r.Error!.Kind, Is.EqualTo(BackendErrorKind.InvalidResponse));
            Assert.That(r.Error.Detail, Does.Contain("token_hash"));
        }

        [Test]
        public void ThrowingProviders_StillAnswerOnce()
        {
            var client = new HttpBackendClient(BaseUrl, _http, new ThrowingAuth(), _integrity, _scheduler);
            BackendResult<AccountCodeResponse> a = Capture<AccountCodeResponse>(cb => client.GetAccountCode(cb));
            Assert.That(a.Error!.Kind, Is.EqualTo(BackendErrorKind.AuthUnavailable));

            client = new HttpBackendClient(BaseUrl, _http, _auth, new ThrowingIntegrity(), _scheduler);
            BackendResult<DeleteAccountResponse> d = Capture<DeleteAccountResponse>(cb => client.DeleteAccount(cb));
            Assert.That(d.Error!.Kind, Is.EqualTo(BackendErrorKind.IntegrityUnavailable));

            client = new HttpBackendClient(BaseUrl, new ThrowingTransport(), _auth, _integrity, _scheduler,
                retry: new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            BackendResult<AccountCodeResponse> t = Capture<AccountCodeResponse>(cb => client.GetAccountCode(cb));
            Assert.That(t.Error!.Kind, Is.EqualTo(BackendErrorKind.Network));
            Assert.That(_http.Requests, Is.Empty);
        }

        [Test]
        public void ExceptionsFromTheCallerAreNotSwallowed()
        {
            _http.Enqueue(200, new AccountCodeResponse("3HZ7M2Q9XK4T8VB1NC6DRF0WPE").ToJson());
            int calls = 0;
            Assert.That(() => _client.GetAccountCode(_ =>
            {
                calls++;
                throw new InvalidOperationException("del llamador");
            }), Throws.InvalidOperationException);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void SynchronizationContextSchedulerPostsBackToItsContext()
        {
            SynchronizationContextForTests context = new SynchronizationContextForTests();
            System.Threading.SynchronizationContext? previous = System.Threading.SynchronizationContext.Current;
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var scheduler = new SynchronizationContextScheduler();
                int ran = 0;
                scheduler.After(TimeSpan.Zero, () => ran++);
                scheduler.After(TimeSpan.FromMilliseconds(20), () => ran++);
                Assert.That(ran, Is.EqualTo(0), "nunca en la misma pila");
                Assert.That(context.WaitForPosts(2, TimeSpan.FromSeconds(5)), Is.True, "las dos acciones vuelven al contexto");
                context.RunAll();
                Assert.That(ran, Is.EqualTo(2));
            }
            finally
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(previous);
            }

            System.Threading.SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                Assert.That(() => new SynchronizationContextScheduler(), Throws.InvalidOperationException);
            }
            finally
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        private sealed class SynchronizationContextForTests : System.Threading.SynchronizationContext
        {
            private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Threading.SendOrPostCallback, object?>> _posts =
                new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Threading.SendOrPostCallback, object?>>();

            public override void Post(System.Threading.SendOrPostCallback d, object? state)
            {
                lock (_posts)
                {
                    _posts.Add(new System.Collections.Generic.KeyValuePair<System.Threading.SendOrPostCallback, object?>(d, state));
                    System.Threading.Monitor.PulseAll(_posts);
                }
            }

            public bool WaitForPosts(int count, TimeSpan timeout)
            {
                DateTime until = DateTime.UtcNow + timeout;
                lock (_posts)
                {
                    while (_posts.Count < count)
                    {
                        TimeSpan left = until - DateTime.UtcNow;
                        if (left <= TimeSpan.Zero)
                        {
                            return false;
                        }

                        System.Threading.Monitor.Wait(_posts, left);
                    }

                    return true;
                }
            }

            public void RunAll()
            {
                System.Collections.Generic.KeyValuePair<System.Threading.SendOrPostCallback, object?>[] posts;
                lock (_posts)
                {
                    posts = _posts.ToArray();
                    _posts.Clear();
                }

                foreach (var p in posts)
                {
                    p.Key(p.Value);
                }
            }
        }

        private sealed class ThrowingAuth : IAuthTokenProvider
        {
            public void GetIdToken(bool forceRefresh, Action<TokenResult> onDone) => throw new InvalidOperationException("SDK sin iniciar");

            public void SignOut()
            {
            }
        }

        private sealed class ThrowingIntegrity : IIntegrityTokenProvider
        {
            public void GetLimitedUseToken(Action<TokenResult> onDone) => throw new InvalidOperationException("SDK sin iniciar");
        }

        private sealed class ThrowingTransport : IHttpTransport
        {
            public void Send(HttpRequestData request, Action<HttpResponseData> onDone) => throw new InvalidOperationException("roto");
        }
    }
}
