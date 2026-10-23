#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;
using AF.Backend.Verification;
using AF.Platform;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>F3-1: la cola de verificación de 48 h de 08 §10.3 y su procesador.</summary>
    public sealed class VerificationQueueTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        private static VerifyPurchaseRequest Purchase(string token = "tok-1", string sku = "af_coins_2") =>
            new VerifyPurchaseRequest(token, sku, "acct");

        [Test]
        public void EnqueueIgnoresTheSameToken()
        {
            var queue = new VerificationQueue();
            Assert.That(queue.Enqueue(Purchase(), T0), Is.True);
            Assert.That(queue.Enqueue(Purchase(), T0.AddMinutes(1)), Is.False);
            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue.Due(T0), Has.Count.EqualTo(1), "una compra nueva se intenta ya");
        }

        [Test]
        public void FailuresBackOffExponentiallyUpToTheCap()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase(), T0);
            var expected = new[] { 30, 60, 120, 240 };
            DateTime now = T0;
            foreach (int seconds in expected)
            {
                queue.RecordFailure("tok-1", BackendError.Network("x"), now);
                Assert.That(queue.Entries[0].NextAttemptUtc, Is.EqualTo(now.AddSeconds(seconds)));
                Assert.That(queue.Due(now.AddSeconds(seconds - 1)), Is.Empty);
                Assert.That(queue.Due(now.AddSeconds(seconds)), Has.Count.EqualTo(1));
                now = now.AddSeconds(seconds);
            }

            for (int i = 0; i < 20; i++)
            {
                queue.RecordFailure("tok-1", BackendError.Network("x"), now);
            }

            Assert.That(queue.Entries[0].NextAttemptUtc - now, Is.EqualTo(TimeSpan.FromHours(6)));
            Assert.That(queue.Entries[0].Attempts, Is.EqualTo(24));
        }

        [Test]
        public void RetryAfterFromTheServerWins()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase(), T0);
            queue.RecordFailure("tok-1", BackendError.Http(429, ApiErrorCode.RateLimited, null, TimeSpan.FromMinutes(10)), T0);
            Assert.That(queue.Entries[0].NextAttemptUtc, Is.EqualTo(T0.AddMinutes(10)));
            Assert.That(queue.Entries[0].LastError, Does.Contain("rate_limited"));
        }

        [Test]
        public void EntriesExpireAfter48HoursByDefault()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase(), T0);
            Assert.That(queue.DeferredAckAfter, Is.EqualTo(TimeSpan.FromHours(48)), "D-040, Q-BILL-5");
            Assert.That(queue.IsExpired(queue.Entries[0], T0.AddHours(48).AddTicks(-1)), Is.False);
            Assert.That(queue.IsExpired(queue.Entries[0], T0.AddHours(48)), Is.True);
            Assert.That(queue.Due(T0.AddHours(48)), Is.Empty, "una vencida ya no se intenta");
        }

        [Test]
        public void DeadlineComesFromRemoteConfigWithinItsRange()
        {
            var queue = new VerificationQueue(new VerificationQueueConfig { SecPurchaseDeferredAckH = 72 });
            queue.Enqueue(Purchase(), T0);
            Assert.That(queue.IsExpired(queue.Entries[0], T0.AddHours(72).AddTicks(-1)), Is.False);
            Assert.That(queue.IsExpired(queue.Entries[0], T0.AddHours(72)), Is.True);
            Assert.That(() => new VerificationQueue(new VerificationQueueConfig { SecPurchaseDeferredAckH = 47 }), Throws.InvalidOperationException);
            Assert.That(() => new VerificationQueue(new VerificationQueueConfig { SecPurchaseDeferredAckH = 73 }), Throws.InvalidOperationException);
        }

        [Test]
        public void DeadlineMatchesTheTableOf08Section9()
        {
            string chapter = System.IO.File.ReadAllText(System.IO.Path.Combine(
                BackendTestFiles.RepoRoot, "docs", "gdd", "08-arquitectura-tecnica-publicacion.md"));
            string? row = null;
            foreach (string line in chapter.Split('\n'))
            {
                if (line.StartsWith("| ", StringComparison.Ordinal) && line.Contains("`sec_purchase_deferred_ack_h`"))
                {
                    row = line;
                }
            }

            Assert.That(row, Is.Not.Null, "08 §9 debe tener sec_purchase_deferred_ack_h");
            string[] cells = row!.Split('|');
            string[] keys = cells[1].Split('/');
            int index = -1;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].Trim() == "`sec_purchase_deferred_ack_h`")
                {
                    index = i;
                }
            }

            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(cells[2].Split('/')[index].Trim(), Is.EqualTo("int"));
            Assert.That(cells[3].Split('/')[index].Trim(), Is.EqualTo(VerificationQueueConfig.Defaults.SecPurchaseDeferredAckH.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(cells[4].Split('/')[index].Trim(), Is.EqualTo(VerificationQueueConfig.MinDeferredAckH + "-" + VerificationQueueConfig.MaxDeferredAckH));
            var attribute = (RemoteConfigKeyAttribute?)Attribute.GetCustomAttribute(
                typeof(VerificationQueueConfig).GetProperty(nameof(VerificationQueueConfig.SecPurchaseDeferredAckH))!, typeof(RemoteConfigKeyAttribute));
            Assert.That(attribute!.Key, Is.EqualTo("sec_purchase_deferred_ack_h"));
        }

        [Test]
        public void DeadlineCountsFromThePurchaseWhenKnown()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase("tok-1"), T0, purchasedUtc: T0.AddHours(-70));
            queue.Enqueue(Purchase("tok-2"), T0, purchasedUtc: T0.AddHours(5));
            Assert.That(queue.Entries[0].FirstQueuedUtc, Is.EqualTo(T0.AddHours(-70)));
            Assert.That(queue.IsExpired(queue.Entries[0], T0.AddHours(2)), Is.True);
            Assert.That(queue.Entries[1].FirstQueuedUtc, Is.EqualTo(T0), "una hora de compra futura no alarga el plazo");
        }

        [Test]
        public void DefinitiveErrorsAreOnly400And409()
        {
            Assert.That(VerificationQueue.IsDefinitive(BackendError.Http(400, ApiErrorCode.BadRequest)), Is.True);
            Assert.That(VerificationQueue.IsDefinitive(BackendError.Http(409, ApiErrorCode.TokenBelongsToOtherUser)), Is.True);
            Assert.That(VerificationQueue.IsDefinitive(BackendError.Http(403, ApiErrorCode.IntegrityFailed)), Is.False);
            Assert.That(VerificationQueue.IsDefinitive(BackendError.Http(501, ApiErrorCode.NotImplemented)), Is.False);
            Assert.That(VerificationQueue.IsDefinitive(BackendError.Network(null)), Is.False);
            Assert.That(VerificationQueue.IsDefinitive(new BackendError(BackendErrorKind.InvalidResponse, 200)), Is.False);
        }

        [Test]
        public void JsonRoundTrip()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase("tok-1"), T0);
            queue.Enqueue(new VerifyPurchaseRequest("tok-2", "af_noads"), T0.AddMinutes(5));
            queue.RecordFailure("tok-2", BackendError.Http(503, ApiErrorCode.Unknown), T0.AddMinutes(5));

            string json = queue.ToJson();
            Assert.That(VerificationQueue.TryFromJson(json, out VerificationQueue? back, out string? error), Is.True, error);
            Assert.That(back!.Count, Is.EqualTo(2));
            Assert.That(back.Entries[0].Request.Token, Is.EqualTo("tok-1"));
            Assert.That(back.Entries[0].Request.ObfuscatedAccountId, Is.EqualTo("acct"));
            Assert.That(back.Entries[0].FirstQueuedUtc, Is.EqualTo(T0));
            Assert.That(back.Entries[1].Request.ObfuscatedAccountId, Is.Null);
            Assert.That(back.Entries[1].Attempts, Is.EqualTo(1));
            Assert.That(back.Entries[1].NextAttemptUtc, Is.EqualTo(T0.AddMinutes(5).AddSeconds(30)));
            Assert.That(back.Entries[1].LastError, Is.EqualTo(queue.Entries[1].LastError));
            Assert.That(back.ToJson(), Is.EqualTo(json));
        }

        [Test]
        public void TryFromJsonRejectsGarbage()
        {
            Assert.That(VerificationQueue.TryFromJson(null, out _, out _), Is.False);
            Assert.That(VerificationQueue.TryFromJson("no json", out _, out _), Is.False);
            Assert.That(VerificationQueue.TryFromJson("{\"v\":2,\"entries\":[]}", out _, out _), Is.False);
            Assert.That(VerificationQueue.TryFromJson("{\"v\":1,\"entries\":[{\"token\":\"\",\"sku\":\"af_x\"}]}", out _, out _), Is.False);
        }

        [Test]
        public void RunnerRemovesTheAnsweredOnesAndKeepsThePendingOnes()
        {
            var fx = new RunnerFixture();
            fx.Queue.Enqueue(Purchase("tok-1"), T0);
            fx.Queue.Enqueue(Purchase("tok-2"), T0);
            fx.Backend.MarkPending("tok-2");
            fx.Runner.Pump();

            Assert.That(fx.Outcomes, Has.Count.EqualTo(2));
            Assert.That(fx.Outcomes[0].Kind, Is.EqualTo(VerificationOutcomeKind.Answered));
            Assert.That(fx.Outcomes[0].Response!.Status, Is.EqualTo(VerifyStatus.Ok));
            Assert.That(fx.Outcomes[1].Response!.Status, Is.EqualTo(VerifyStatus.Pending));
            Assert.That(fx.Changes, Is.EqualTo(2));
            Assert.That(fx.Queue.Count, Is.EqualTo(1), "pending se queda en la cola (08 §10.3: reintento)");
            Assert.That(fx.Queue.Entries[0].Request.Token, Is.EqualTo("tok-2"));
            Assert.That(fx.Queue.Entries[0].LastError, Is.EqualTo("pending"));

            fx.Runner.Pump();
            Assert.That(fx.Backend.Calls["verifyPurchase"], Is.EqualTo(2), "no se vuelve a preguntar antes de la espera");
            fx.Backend.ClearPending("tok-2");
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Runner.Pump();
            Assert.That(fx.Queue.Count, Is.EqualTo(0));
            Assert.That(fx.Outcomes[2].Response!.Status, Is.EqualTo(VerifyStatus.Ok));
        }

        [Test]
        public void PendingRestartsTheDeadline()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase(), T0);
            queue.RecordPending("tok-1", T0.AddHours(46));
            Assert.That(queue.Entries[0].FirstQueuedUtc, Is.EqualTo(T0.AddHours(46)), "el servidor respondió: el plazo vuelve a contar");
            Assert.That(queue.IsExpired(queue.Entries[0], T0.AddHours(80)), Is.False);
            Assert.That(queue.Entries[0].Attempts, Is.EqualTo(1));
            Assert.That(queue.Entries[0].NextAttemptUtc, Is.EqualTo(T0.AddHours(46).AddSeconds(30)));
        }

        [Test]
        public void RunnerBacksOffOnTransientErrorsAndRejectsDefinitiveOnes()
        {
            var fx = new RunnerFixture();
            fx.Queue.Enqueue(Purchase("tok-1"), T0);
            fx.Queue.Enqueue(Purchase("tok-2"), T0);
            fx.Backend.FailNext(BackendError.Http(503, ApiErrorCode.Unknown));
            fx.Backend.FailNext(BackendError.Http(409, ApiErrorCode.TokenBelongsToOtherUser));
            fx.Runner.Pump();

            Assert.That(fx.Queue.Count, Is.EqualTo(1));
            Assert.That(fx.Queue.Entries[0].Request.Token, Is.EqualTo("tok-1"));
            Assert.That(fx.Queue.Entries[0].Attempts, Is.EqualTo(1));
            Assert.That(fx.Outcomes, Has.Count.EqualTo(2));
            Assert.That(fx.Outcomes[0].Kind, Is.EqualTo(VerificationOutcomeKind.Retrying));
            Assert.That(fx.Outcomes[0].Entry.Request.Token, Is.EqualTo("tok-1"));
            Assert.That(fx.Outcomes[0].Error!.HttpStatus, Is.EqualTo(503));
            Assert.That(fx.Outcomes[1].Kind, Is.EqualTo(VerificationOutcomeKind.Rejected));
            Assert.That(fx.Outcomes[1].Entry.Request.Token, Is.EqualTo("tok-2"));

            fx.Runner.Pump();
            Assert.That(fx.Backend.Calls["verifyPurchase"], Is.EqualTo(2), "no se reintenta antes de la espera");
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Runner.Pump();
            Assert.That(fx.Queue.Count, Is.EqualTo(0));
            Assert.That(fx.Outcomes[2].Kind, Is.EqualTo(VerificationOutcomeKind.Answered));
        }

        [Test]
        public void RunnerDefersTheAckAfter48HoursOnceAndKeepsRetrying()
        {
            var fx = new RunnerFixture();
            fx.Queue.Enqueue(Purchase("tok-1"), T0);
            fx.Backend.FailNext(BackendError.Network("offline"), 1000);
            int deferredAt = -1;
            for (int i = 0; i < 100; i++)
            {
                fx.Runner.Pump();
                if (deferredAt < 0 && fx.Outcomes.Exists(o => o.Kind == VerificationOutcomeKind.DeferredAck))
                {
                    deferredAt = i;
                }

                fx.Clock.Advance(TimeSpan.FromHours(1));
            }

            Assert.That(deferredAt, Is.EqualTo(48), "se anota en el primer Pump a las 48 h");
            Assert.That(fx.Outcomes.FindAll(o => o.Kind == VerificationOutcomeKind.DeferredAck), Has.Count.EqualTo(1), "una sola vez");
            Assert.That(fx.Outcomes.TrueForAll(o => o.Kind != VerificationOutcomeKind.Answered), Is.True, "nunca se acredita sin respuesta");
            Assert.That(fx.Queue.Count, Is.EqualTo(1), "D-035: sigue en la cola hasta que el servidor la verifique");
            Assert.That(fx.Queue.Entries[0].IsDeferredAck, Is.True);
            Assert.That(fx.Queue.Entries[0].DeferredAckUtc, Is.EqualTo(T0.AddHours(48)));

            int calls = fx.Backend.Calls["verifyPurchase"];
            Assert.That(calls, Is.GreaterThan(10), "se reintenta también después del deferred_ack");
            var fresh = new FakeBackendClient(fx.Clock);
            var outcomes = new List<VerificationOutcome>();
            var runner = new VerificationQueueRunner(fx.Queue, fresh, fx.Clock, outcomes.Add);
            fx.Clock.Advance(TimeSpan.FromHours(6));
            runner.Pump();
            Assert.That(outcomes, Has.Count.EqualTo(1));
            Assert.That(outcomes[0].Kind, Is.EqualTo(VerificationOutcomeKind.Answered));
            Assert.That(outcomes[0].Entry.IsDeferredAck, Is.True, "quien acredita sabe que ya se reconoció en local");
            Assert.That(fx.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void WithoutSendTheRunnerOnlyDefersTheAck()
        {
            var fx = new RunnerFixture();
            fx.Queue.Enqueue(Purchase("tok-1"), T0);
            fx.Runner.Pump(send: false);
            Assert.That(fx.Backend.Calls.ContainsKey("verifyPurchase"), Is.False, "biz_backend_enabled = false: sin red");
            fx.Clock.Advance(TimeSpan.FromHours(48));
            fx.Runner.Pump(send: false);
            Assert.That(fx.Outcomes, Has.Count.EqualTo(1));
            Assert.That(fx.Outcomes[0].Kind, Is.EqualTo(VerificationOutcomeKind.DeferredAck));
            Assert.That(fx.Queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void DeferredAckAndClientSeedsSurviveTheJson()
        {
            var queue = new VerificationQueue();
            queue.Enqueue(Purchase("tok-1", "af_piggy"), T0, clientSeeds: 1500);
            queue.Enqueue(Purchase("tok-2"), T0);
            queue.MarkDeferredAck("tok-2", T0.AddHours(48));
            Assert.That(() => queue.Enqueue(Purchase("tok-3"), T0, clientSeeds: -1), Throws.InstanceOf<ArgumentOutOfRangeException>());

            Assert.That(VerificationQueue.TryFromJson(queue.ToJson(), out VerificationQueue? back, out string? error), Is.True, error);
            Assert.That(back!.Get("tok-1")!.ClientSeeds, Is.EqualTo(1500));
            Assert.That(back.Get("tok-1")!.IsDeferredAck, Is.False);
            Assert.That(back.Get("tok-2")!.ClientSeeds, Is.Null);
            Assert.That(back.Get("tok-2")!.DeferredAckUtc, Is.EqualTo(T0.AddHours(48)));
            Assert.That(back.IsExpired(back.Get("tok-2")!, T0.AddHours(500)), Is.False, "ya reconocida: no vuelve a vencer");
            Assert.That(back.IsExpired(back.Get("tok-1")!, T0.AddHours(500)), Is.True);
            Assert.That(back.Due(T0.AddHours(500)), Has.Count.EqualTo(1), "la reconocida se sigue intentando; la vencida espera su deferred_ack");
            Assert.That(back.Due(T0.AddHours(500))[0].Request.Token, Is.EqualTo("tok-2"));
            Assert.That(back.ToJson(), Is.EqualTo(queue.ToJson()));
        }

        [Test]
        public void RunnerNeverSendsTheSamePurchaseTwiceAtOnce()
        {
            var client = new HoldingClient();
            var clock = new ManualClock(T0);
            var queue = new VerificationQueue();
            var outcomes = new List<VerificationOutcome>();
            var runner = new VerificationQueueRunner(queue, client, clock, outcomes.Add);
            queue.Enqueue(Purchase("tok-1"), T0);

            runner.Pump();
            runner.Pump();
            Assert.That(client.Held, Has.Count.EqualTo(1));
            Assert.That(runner.InFlight, Is.EqualTo(1));

            clock.Advance(TimeSpan.FromHours(80));
            runner.Pump();
            Assert.That(outcomes, Is.Empty, "una en vuelo no vence hasta que responda");

            client.Held[0](BackendResult<VerifyPurchaseResponse>.Fail(BackendError.Network("x")));
            Assert.That(outcomes, Has.Count.EqualTo(1));
            Assert.That(outcomes[0].Kind, Is.EqualTo(VerificationOutcomeKind.Retrying));
            runner.Pump();
            Assert.That(outcomes, Has.Count.EqualTo(2));
            Assert.That(outcomes[1].Kind, Is.EqualTo(VerificationOutcomeKind.DeferredAck));
            Assert.That(client.Held, Has.Count.EqualTo(1), "tras el fallo espera su turno");
            clock.Advance(TimeSpan.FromSeconds(30));
            runner.Pump();
            Assert.That(client.Held, Has.Count.EqualTo(2), "la reconocida en local se vuelve a mandar");
        }

        private sealed class RunnerFixture
        {
            public RunnerFixture()
            {
                Clock = new ManualClock(T0);
                Backend = new FakeBackendClient(Clock);
                Queue = new VerificationQueue();
                Runner = new VerificationQueueRunner(Queue, Backend, Clock, Outcomes.Add, () => Changes++);
            }

            public ManualClock Clock { get; }

            public FakeBackendClient Backend { get; }

            public VerificationQueue Queue { get; }

            public VerificationQueueRunner Runner { get; }

            public List<VerificationOutcome> Outcomes { get; } = new List<VerificationOutcome>();

            public int Changes { get; private set; }
        }

        /// <summary>Cliente que retiene las respuestas de <c>verifyPurchase</c>.</summary>
        private sealed class HoldingClient : IBackendClient
        {
            public List<Action<BackendResult<VerifyPurchaseResponse>>> Held { get; } = new List<Action<BackendResult<VerifyPurchaseResponse>>>();

            public void VerifyPurchase(VerifyPurchaseRequest request, Action<BackendResult<VerifyPurchaseResponse>> onDone) => Held.Add(onDone);

            public void RestoreLedger(RestoreLedgerRequest request, Action<BackendResult<RestoreLedgerResponse>> onDone) => throw new NotSupportedException();

            public void GetAccountCode(Action<BackendResult<AccountCodeResponse>> onDone) => throw new NotSupportedException();

            public void DeleteAccount(Action<BackendResult<DeleteAccountResponse>> onDone) => throw new NotSupportedException();
        }
    }
}
