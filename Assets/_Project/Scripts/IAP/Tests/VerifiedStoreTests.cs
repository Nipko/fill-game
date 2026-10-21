#nullable enable
using System;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// F3-3, compra verificada (08 §10.3; D-035 y sus añadidos; plan §9): cada rama del servidor con la tienda y el backend falsos.
    /// Lo que se vigila en todas: nada se acredita sin una respuesta <c>ok</c> o <c>duplicate</c> del servidor, nada se acredita dos
    /// veces (<c>applied_purchases</c>) y una compra solo se consume o confirma en la tienda después de acreditarla.
    /// </summary>
    public sealed class VerifiedStoreTests
    {
        private static readonly PurchaseVerificationState[] VerifyThenCredit =
        {
            PurchaseVerificationState.Verifying, PurchaseVerificationState.Credited,
        };

        // ------------------------------------------------------------------ ok

        [Test]
        public void Ok_CreditsTheServerGrantOnce_ThenConsumes()
        {
            var fx = new VerifiedStoreFixture();

            PurchaseResult result = fx.Buy("af_coins_2");

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(result.Transaction!.SeedsDelta, Is.EqualTo(1000));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Events, Is.EqualTo(VerifyThenCredit));
            Assert.That(fx.Profile.AppliedPurchases, Is.EqualTo(new[] { VerifiedStoreFixture.Hash(1) }));
            Assert.That(fx.Billing.Consumed, Is.EqualTo(new[] { VerifiedStoreFixture.Token(1) }), "D-035: consume el cliente tras ok");
            Assert.That(fx.Billing.IsUnfinished(VerifiedStoreFixture.Token(1)), Is.False);
            Assert.That(fx.Service.PendingVerifications, Is.Empty);
            Assert.That(fx.Service.IsPurchasing, Is.False);

            Profile saved = fx.Saved();
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(saved.AppliedPurchases, Is.EqualTo(new[] { VerifiedStoreFixture.Hash(1) }), "lo acreditado y el hash, en la misma escritura");
            Assert.That(saved.Ads.LastIap, Is.Not.Null);
        }

        [Test]
        public void Ok_CreditsWhatTheServerGrants_NotTheLocalCatalog()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.GrantFor = r => new PurchaseGrant(1234, new[] { new GrantItem("hint", 2), new GrantItem("skip", 1) });

            PurchaseResult result = fx.Buy("af_coins_2");

            Assert.That(result.IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1234), "manda el grant del servidor");
            Assert.That(fx.Stock(BoosterKind.Hint), Is.EqualTo(2));
            Assert.That(fx.Profile.AppliedPurchases, Has.Count.EqualTo(1), "un ítem desconocido (skip) no impide acreditar el resto");
        }

        [TestCase("af_coins_6", 36000)]
        [TestCase("af_skin_m03", 300)]
        public void Ok_EveryProductMatchesTheCatalog(string sku, int seeds)
        {
            var fx = new VerifiedStoreFixture();

            Assert.That(fx.Buy(sku).IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(seeds));
        }

        [Test]
        public void Ok_Starter_CreditsItsContent_AndIsConsumed()
        {
            var fx = new VerifiedStoreFixture();

            Assert.That(fx.Buy("af_starter").IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(2));
            Assert.That(fx.Stock(BoosterKind.Hint), Is.EqualTo(3));
            Assert.That(fx.Profile.Entitlements.StarterBought, Is.True);
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1), "la Mochila es consumible en Play (1 por cuenta)");
            Assert.That(fx.Buy("af_starter").Status, Is.EqualTo(PurchaseStatus.AlreadyOwned));
            Assert.That(fx.Billing.Requests, Has.Count.EqualTo(1), "no se abre la compra de algo ya comprado");
        }

        [Test]
        public void Ok_NoAds_IsConfirmedNotConsumed()
        {
            var fx = new VerifiedStoreFixture();

            Assert.That(fx.Buy("af_noads").IsPurchased, Is.True);
            Assert.That(fx.Profile.Entitlements.NoAds, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fx.Billing.Confirmed, Is.EqualTo(new[] { VerifiedStoreFixture.Token(1) }));
            Assert.That(fx.Billing.Consumed, Is.Empty);
        }

        [Test]
        public void Ok_Piggy_CreditsTheSeedsOnlyTheClientKnows()
        {
            var fx = new VerifiedStoreFixture();

            Assert.That(fx.Buy("af_piggy", piggySeeds: 1730, context: PurchaseContext.Piggy).IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1730), "grant: semillas 0 + ítem piggy");
        }

        [Test]
        public void Ok_Balsa_AppliesTheRescueNow_ButNotWhenCreditedLater()
        {
            var fx = new VerifiedStoreFixture();
            PurchaseResult live = fx.Buy("af_rescue", level: 31, context: PurchaseContext.Stuck);
            Assert.That(live.AppliesRescue, Is.True);
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Wallet.Canoes.IsInfinite, Is.True);

            var later = new VerifiedStoreFixture();
            later.Backend.FailNext(BackendError.Network("offline"));
            Assert.That(later.Buy("af_rescue", level: 31).Status, Is.EqualTo(PurchaseStatus.Pending));
            later.Clock.Advance(TimeSpan.FromSeconds(30));
            later.Service.Pump();
            Assert.That(later.Wallet.Seeds, Is.EqualTo(1000), "lo demás se acredita igual");
            Assert.That(later.Wallet.RescuesToday, Is.EqualTo(0), "fuera del nivel no hay Cuia de rescate que aplicar");
        }

        // ------------------------------------------------------------------ setObfuscatedAccountId

        [Test]
        public void TheUidGoesToPlayAndToTheServer()
        {
            var fx = new VerifiedStoreFixture();
            fx.Buy("af_coins_2");
            Assert.That(fx.Billing.Requests[0].Value, Is.EqualTo(VerifiedStoreFixture.Uid), "setObfuscatedAccountId = UID");
            Assert.That(fx.Backend.VerifyRequests[0].ObfuscatedAccountId, Is.EqualTo(VerifiedStoreFixture.Uid));

            var anonymous = new VerifiedStoreFixture(uid: null);
            anonymous.Buy("af_coins_2");
            Assert.That(anonymous.Billing.Requests[0].Value, Is.Null, "sin UID todavía, sin cuenta ofuscada");
            Assert.That(anonymous.Backend.VerifyRequests[0].ObfuscatedAccountId, Is.Null);
            Assert.That(anonymous.Wallet.Seeds, Is.EqualTo(1000));
        }

        // ------------------------------------------------------------------ duplicate

        [Test]
        public void Duplicate_NotYetApplied_CreditsTheCatalogContentOnce()
        {
            // Otra instalación: el servidor ya la verificó (libro mayor), pero este perfil no la tiene.
            var fx = new VerifiedStoreFixture();
            fx.Backend.VerifyPurchase(new VerifyPurchaseRequest("other.device.token", "af_coins_3"), _ => { });
            fx.Billing.AddUnfinished("af_coins_3", "other.device.token");

            fx.Service.Resume();

            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2800));
            Assert.That(fx.Events, Is.EqualTo(new[] { PurchaseVerificationState.Credited }));
            Assert.That(fx.Profile.HasAppliedPurchase(PurchaseTokenHash.Of("other.device.token")), Is.True);
            Assert.That(fx.Billing.Consumed, Is.EqualTo(new[] { "other.device.token" }));

            fx.Service.Resume();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2800), "terminada: no vuelve");
        }

        [Test]
        public void Duplicate_AlreadyApplied_NeverCreditsTwice_OnlyFinishes()
        {
            var fx = new VerifiedStoreFixture();
            fx.Billing.CrashOnFinish = true; // la app se cierra tras acreditar y antes de consumir
            Assert.That(fx.Buy("af_coins_2").IsPurchased, Is.True);
            Assert.That(fx.Billing.IsUnfinished(VerifiedStoreFixture.Token(1)), Is.True);

            // Peor caso: la cola guardada aún tiene el token (se fuerza) y el servidor responde duplicate.
            Profile saved = fx.Saved();
            var queue = new AF.Backend.Verification.VerificationQueue();
            queue.Enqueue(new VerifyPurchaseRequest(VerifiedStoreFixture.Token(1), "af_coins_2"), fx.Clock.UtcNow);
            saved.PurchaseQueue = queue.ToJson();
            fx.Store.Save(saved);
            fx.Billing.CrashOnFinish = false;

            VerifiedStoreFixture again = fx.Reopen();
            again.Service.Resume();

            Assert.That(fx.VerifyCalls, Is.EqualTo(2), "se volvió a preguntar");
            Assert.That(again.Wallet.Seeds, Is.EqualTo(1000), "duplicate de un hash ya aplicado: nada más");
            Assert.That(again.Events, Is.EqualTo(new[] { PurchaseVerificationState.AlreadyApplied }));
            Assert.That(again.Billing.Consumed, Is.EqualTo(new[] { VerifiedStoreFixture.Token(1) }));
            Assert.That(again.Service.PendingVerifications, Is.Empty);
        }

        [Test]
        public void AppliedButNotConsumed_IsFinishedOnResumeWithoutAskingTheServer()
        {
            var fx = new VerifiedStoreFixture();
            fx.Billing.CrashOnFinish = true;
            fx.Buy("af_coins_2");
            fx.Billing.CrashOnFinish = false;

            VerifiedStoreFixture again = fx.Reopen();
            again.Service.Resume();

            Assert.That(fx.VerifyCalls, Is.EqualTo(1));
            Assert.That(again.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(again.Billing.Consumed, Is.EqualTo(new[] { VerifiedStoreFixture.Token(1) }));
        }

        // ------------------------------------------------------------------ pending

        [Test]
        public void ServerPending_CreditsNothing_StaysQueued_AndCreditsWhenPaid()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.MarkPending(VerifiedStoreFixture.Token(1));

            PurchaseResult result = fx.Buy("af_coins_2");

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Profile.AppliedPurchases, Is.Empty);
            Assert.That(fx.Billing.IsUnfinished(VerifiedStoreFixture.Token(1)), Is.True, "no se consume");
            Assert.That(fx.Queued(VerifiedStoreFixture.Token(1)), Is.Not.Null, "reencolada");
            Assert.That(fx.Events, Is.EqualTo(new[] { PurchaseVerificationState.Verifying, PurchaseVerificationState.Pending }));

            fx.Service.Pump();
            Assert.That(fx.VerifyCalls, Is.EqualTo(1), "espera su turno");
            fx.Backend.ClearPending(VerifiedStoreFixture.Token(1));
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1));
        }

        [Test]
        public void PlayPending_IsNotVerifiedUntilPlayChargesIt()
        {
            var fx = new VerifiedStoreFixture();
            fx.Billing.Mode = FakeBillingMode.Pending;

            Assert.That(fx.Buy("af_coins_2").Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(fx.VerifyCalls, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));

            fx.Billing.SettlePending();
            fx.Service.Resume();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1));
        }

        // ------------------------------------------------------------------ errores definitivos

        [Test]
        public void SecondStarter400_IsNeitherCreditedNorConsumed()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Http(400, ApiErrorCode.BadRequest, "af_starter ya comprado"));

            PurchaseResult result = fx.Buy("af_starter");

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.VerifyFailed));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Profile.Entitlements.StarterBought, Is.False);
            Assert.That(fx.Profile.AppliedPurchases, Is.Empty);
            Assert.That(fx.Billing.Consumed, Is.Empty, "consumir también reconoce: Play ya no la reembolsaría");
            Assert.That(fx.Billing.Confirmed, Is.Empty);
            Assert.That(fx.Billing.LocallyAcknowledged, Is.Empty);
            Assert.That(fx.Billing.IsUnfinished(VerifiedStoreFixture.Token(1)), Is.True);
            Assert.That(fx.Service.PendingVerifications, Is.Empty);
            Assert.That(fx.Events, Is.EqualTo(new[] { PurchaseVerificationState.Verifying, PurchaseVerificationState.Rejected }));

            fx.Service.Resume();
            Assert.That(fx.VerifyCalls, Is.EqualTo(1), "no se reenvía en la misma sesión");

            VerifiedStoreFixture again = fx.Reopen();
            again.Backend.FailNext(BackendError.Http(400, ApiErrorCode.BadRequest));
            again.Service.Resume();
            Assert.That(fx.VerifyCalls, Is.EqualTo(2), "en el siguiente arranque se pregunta otra vez (sigue sin terminar)");
            Assert.That(again.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(again.Billing.Consumed, Is.Empty);
        }

        [Test]
        public void TokenOfAnotherUser409_IsNeitherCreditedNorConsumed()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Http(409, ApiErrorCode.TokenBelongsToOtherUser));

            Assert.That(fx.Buy("af_coins_4").Status, Is.EqualTo(PurchaseStatus.VerifyFailed));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Billing.Consumed, Is.Empty);
            Assert.That(fx.Billing.IsUnfinished(VerifiedStoreFixture.Token(1)), Is.True);
        }

        [Test]
        public void AnAnswerForAnotherToken_IsNotCredited()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.RewriteVerify = r => r.Status == VerifyStatus.Ok
                ? VerifyPurchaseResponse.Ok(PurchaseTokenHash.Of("otro"), r.Grant!)
                : r;

            Assert.That(fx.Buy("af_coins_2").Status, Is.EqualTo(PurchaseStatus.Failed));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Billing.Consumed, Is.Empty);
            Assert.That(fx.Events[1], Is.EqualTo(PurchaseVerificationState.CreditFailed));

            Assert.That(fx.Queued(VerifiedStoreFixture.Token(1)), Is.Not.Null, "vuelve a la cola con espera");
            fx.Backend.RewriteVerify = null;
            fx.Service.Resume();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "espera su turno");
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000), "duplicate y se acredita una vez");
            fx.Service.Resume();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
        }

        // ------------------------------------------------------------------ fallos pasajeros y 72 h

        [Test]
        public void Transient_QueuesWithBackoff_AndCreditsOnceWhenTheServerAnswers()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Http(503, ApiErrorCode.Unknown));

            PurchaseResult result = fx.Buy("af_coins_2");

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Pending), "«Confirmando…»: se acreditará al verificarse");
            Assert.That(fx.Service.IsPurchasing, Is.False);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Events, Is.EqualTo(new[] { PurchaseVerificationState.Verifying, PurchaseVerificationState.Queued }));
            Assert.That(fx.Saved().PurchaseQueue, Does.Contain(VerifiedStoreFixture.Token(1)), "la cola se guarda");

            fx.Service.Pump();
            Assert.That(fx.VerifyCalls, Is.EqualTo(1));
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Service.Pump();
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.VerifyCalls, Is.EqualTo(2));
            Assert.That(fx.Saved().PurchaseQueue, Does.Not.Contain(VerifiedStoreFixture.Token(1)));
        }

        [Test]
        public void After72HoursWithoutAnswer_AcknowledgesLocallyWithoutCrediting()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Network("offline"), 10000);
            Assert.That(fx.Buy("af_coins_5").Status, Is.EqualTo(PurchaseStatus.Pending));

            for (int hour = 0; hour < 80; hour++)
            {
                fx.Clock.Advance(TimeSpan.FromHours(1));
                fx.Service.Pump();
            }

            string token = VerifiedStoreFixture.Token(1);
            Assert.That(fx.Billing.LocallyAcknowledged, Is.EqualTo(new[] { token }), "una sola vez");
            Assert.That(fx.Events.FindAll(e => e == PurchaseVerificationState.DeferredAck), Has.Count.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0), "D-035: el deferred_ack no acredita");
            Assert.That(fx.Profile.AppliedPurchases, Is.Empty);
            Assert.That(fx.Billing.Consumed, Is.Empty);
            Assert.That(fx.Queued(token)!.IsDeferredAck, Is.True, "sigue en la cola");

            // Reabrir no lo pierde ni vuelve a reconocer.
            VerifiedStoreFixture again = fx.Reopen();
            again.Service.Resume();
            Assert.That(again.Queued(token)!.IsDeferredAck, Is.True);

            fx.Backend.ClearFailures();
            again.Clock.Advance(TimeSpan.FromHours(6));
            again.Service.Pump();
            Assert.That(again.Wallet.Seeds, Is.EqualTo(13000), "se acredita cuando el servidor la verifica");
            Assert.That(again.Billing.Consumed, Is.EqualTo(new[] { token }));
            Assert.That(again.Billing.LocallyAcknowledged, Has.Count.EqualTo(1));
            Assert.That(again.Service.PendingVerifications, Is.Empty);
        }

        // ------------------------------------------------------------------ persistencia de la cola

        [Test]
        public void TheQueueSurvivesReopening_AndIsCreditedOnce()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Network("offline"), 2);
            Assert.That(fx.Buy("af_piggy", piggySeeds: 2400).Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(fx.Buy("af_coins_3").Status, Is.EqualTo(PurchaseStatus.Pending));

            VerifiedStoreFixture again = fx.Reopen();
            Assert.That(again.Service.PendingVerifications, Has.Count.EqualTo(2), "leída del perfil guardado");
            Assert.That(again.Queued(VerifiedStoreFixture.Token(1))!.ClientSeeds, Is.EqualTo(2400));

            again.Clock.Advance(TimeSpan.FromMinutes(1));
            again.Service.Resume();
            Assert.That(fx.VerifyCalls, Is.EqualTo(4), "una petición por compra, sin duplicar las de la tienda");
            Assert.That(again.Wallet.Seeds, Is.EqualTo(2400 + 2800));
            Assert.That(again.Profile.AppliedPurchases, Has.Count.EqualTo(2));
            Assert.That(again.Billing.Consumed, Has.Count.EqualTo(2));

            VerifiedStoreFixture third = again.Reopen();
            third.Service.Resume();
            Assert.That(third.Wallet.Seeds, Is.EqualTo(5200), "nada se acredita dos veces al reabrir");
            Assert.That(fx.VerifyCalls, Is.EqualTo(4));
        }

        [Test]
        public void AnUnreadableSavedQueue_StartsEmpty_AndTheStoreRedeliversThePurchase()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Network("offline"));
            fx.Buy("af_coins_2");
            Profile saved = fx.Saved();
            saved.PurchaseQueue = "{\"v\":99,\"entries\":[]}";
            fx.Store.Save(saved);

            VerifiedStoreFixture again = fx.Reopen();
            Assert.That(again.Service.PendingVerifications, Is.Empty);
            again.Service.Resume();
            Assert.That(again.Wallet.Seeds, Is.EqualTo(1000), "vuelve de la tienda y se acredita una vez");
        }

        // ------------------------------------------------------------------ carreras

        [Test]
        public void Race_ASecondPurchaseWhileTheFirstIsVerifying_IsBusy()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.HoldVerifyResponses = true;

            Assert.That(fx.TryBuy("af_coins_2"), Is.Null, "esperando al servidor");
            Assert.That(fx.Service.IsPurchasing, Is.True);
            Assert.That(fx.Buy("af_coins_3").Status, Is.EqualTo(PurchaseStatus.Busy));
            Assert.That(fx.Billing.Requests, Has.Count.EqualTo(1), "no se cobra la segunda");

            fx.Backend.ReleaseVerifyResponses();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Service.IsPurchasing, Is.False);
        }

        [Test]
        public void Race_TwoPurchasesAnsweredInReverseOrder_EachCreditedOnce()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Network("offline"));
            Assert.That(fx.Buy("af_coins_2").Status, Is.EqualTo(PurchaseStatus.Pending)); // la 1, en la cola

            fx.Backend.HoldVerifyResponses = true;
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            PurchaseResult? second = null;
            fx.Service.Purchase(new PurchaseRequest("af_coins_3"), r => second = r); // la 2 y, en el mismo Pump, la 1
            Assert.That(fx.Backend.HeldVerifyResponses, Is.EqualTo(2));

            fx.Backend.ReleaseVerifyResponses(reverse: true);
            Assert.That(second!.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(second.Transaction!.SeedsDelta, Is.EqualTo(2800), "cada respuesta acredita su compra");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(3800));
            Assert.That(fx.Profile.AppliedPurchases, Has.Count.EqualTo(2));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(2));

            fx.Backend.HoldVerifyResponses = false;
            fx.Service.Resume();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(3800));
        }

        [Test]
        public void Race_TheSamePurchaseFromTheStoreAndFromResume_IsSentAndCreditedOnce()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.HoldVerifyResponses = true;
            Assert.That(fx.TryBuy("af_coins_2"), Is.Null);

            fx.Service.Resume(); // la app vuelve a primer plano: la tienda entrega la misma compra sin terminar
            fx.Service.Pump();
            Assert.That(fx.VerifyCalls, Is.EqualTo(1), "una sola petición en vuelo por compra");

            fx.Backend.ReleaseVerifyResponses();
            fx.Backend.HoldVerifyResponses = false;
            fx.Service.Resume();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.VerifyCalls, Is.EqualTo(1));
        }

        [Test]
        public void Race_OkAndDuplicateForTheSameToken_CreditOnce()
        {
            // Dos peticiones del mismo token llegan al servidor (la compra y un reinicio): una ok y otra duplicate.
            var fx = new VerifiedStoreFixture();
            fx.Backend.HoldVerifyResponses = true;
            fx.Billing.CrashOnFinish = true;
            Assert.That(fx.TryBuy("af_coins_2"), Is.Null);

            VerifiedStoreFixture other = fx.Reopen(); // otra sesión sobre el mismo almacén: la cola guardada ya tiene el token
            other.Service.Resume();
            Assert.That(fx.Backend.HeldVerifyResponses, Is.EqualTo(2));

            fx.Backend.ReleaseVerifyResponses(); // ok para la primera sesión, duplicate para la segunda
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));

            VerifiedStoreFixture last = fx.Reopen();
            Assert.That(last.Wallet.Seeds, Is.EqualTo(1000), "el perfil guardado tiene una sola acreditación");
            Assert.That(last.Profile.AppliedPurchases, Has.Count.EqualTo(1));
        }

        // ------------------------------------------------------------------ revisión: contexto, disco y compras únicas

        [Test]
        public void Race_ResumeBeforeTheStoreAnswers_KeepsThePiggySeedsAndTheRescue()
        {
            var fx = new VerifiedStoreFixture();
            fx.Billing.HoldResponses = true;
            Assert.That(fx.TryBuy("af_piggy", piggySeeds: 2200), Is.Null);
            fx.Service.Resume(); // la hoja de Play se cierra y la app vuelve a primer plano antes de la respuesta
            Assert.That(fx.VerifyCalls, Is.EqualTo(0), "la compra abierta no se encola sin su contexto");
            fx.Billing.ReleaseHeld();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(2200), "las semillas exactas del Cántaro");
            Assert.That(fx.VerifyCalls, Is.EqualTo(1));

            var balsa = new VerifiedStoreFixture();
            balsa.Billing.HoldResponses = true;
            PurchaseResult? result = null;
            balsa.Service.Purchase(new PurchaseRequest("af_rescue", PurchaseContext.Stuck, 31), r => result = r);
            balsa.Service.Resume();
            balsa.Billing.ReleaseHeld();
            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(result.AppliesRescue, Is.True);
            Assert.That(balsa.Wallet.RescuesToday, Is.EqualTo(1), "la Cuia de rescate se aplica al momento");
        }

        [Test]
        public void IfTheCreditIsNotOnDisk_ThePurchaseIsNotConsumed()
        {
            var fx = new VerifiedStoreFixture();
            fx.Disk.Fail = true;

            Assert.That(fx.Buy("af_coins_2").IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000), "en memoria");
            Assert.That(fx.Billing.IsUnfinished(VerifiedStoreFixture.Token(1)), Is.True, "sin guardar no se consume");
            fx.Service.Resume();
            Assert.That(fx.Billing.Consumed, Is.Empty);

            fx.Disk.Fail = false;
            fx.Service.Resume();
            Assert.That(fx.Billing.Consumed, Is.EqualTo(new[] { VerifiedStoreFixture.Token(1) }));
            Assert.That(fx.Saved().Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000), "no se acredita otra vez");
        }

        [Test]
        public void IfNothingReachedTheDisk_ReopeningCreditsOnce()
        {
            var fx = new VerifiedStoreFixture();
            fx.Disk.Fail = true;
            fx.Buy("af_coins_3");

            VerifiedStoreFixture again = fx.Reopen(); // la app muere: en disco no hay nada de la compra
            Assert.That(again.Wallet.Seeds, Is.EqualTo(0));
            again.Service.Resume();
            Assert.That(again.Wallet.Seeds, Is.EqualTo(2800), "Play la devuelve; duplicate no aplicado: una vez");
            Assert.That(again.Billing.Consumed, Has.Count.EqualTo(1));
        }

        [Test]
        public void ACreditFailure_KeepsThePurchaseQueuedWithItsContext()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.RewriteVerify = r => r.Status == VerifyStatus.Ok ? VerifyPurchaseResponse.Ok(PurchaseTokenHash.Of("otro"), r.Grant!) : r;

            Assert.That(fx.Buy("af_piggy", piggySeeds: 1800).Status, Is.EqualTo(PurchaseStatus.Failed));
            Assert.That(fx.Queued(VerifiedStoreFixture.Token(1))!.ClientSeeds, Is.EqualTo(1800));
            Assert.That(fx.Saved().PurchaseQueue, Does.Contain(VerifiedStoreFixture.Token(1)));

            fx.Backend.RewriteVerify = null;
            fx.Clock.Advance(TimeSpan.FromSeconds(30));
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1800));
            Assert.That(fx.Billing.Consumed, Has.Count.EqualTo(1));
        }

        [Test]
        public void AnotherStarterWhenItIsAlreadyOwned_IsNeitherCreditedNorConsumed()
        {
            var fx = new VerifiedStoreFixture();
            fx.Profile.Entitlements.StarterBought = true;
            fx.Billing.AddUnfinished("af_starter", "second.starter");

            fx.Service.Resume();

            Assert.That(fx.VerifyCalls, Is.EqualTo(1));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Profile.AppliedPurchases, Is.Empty);
            Assert.That(fx.Billing.IsUnfinished("second.starter"), Is.True, "no se consume: Play aún podría reembolsarla");
            fx.Service.Resume();
            Assert.That(fx.VerifyCalls, Is.EqualTo(1), "no se repite en la misma sesión");
        }

        [Test]
        public void AUniqueProductWaitingForTheServer_IsNotChargedAgain()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Network("offline"));
            Assert.That(fx.Buy("af_noads").Status, Is.EqualTo(PurchaseStatus.Pending));

            Assert.That(fx.Buy("af_noads").Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(fx.Billing.Requests, Has.Count.EqualTo(1), "no se abre otra compra");
            Assert.That(fx.Buy("af_coins_2").IsPurchased, Is.True, "los consumibles sí");
        }

        // ------------------------------------------------------------------ interruptor y tienda

        [Test]
        public void BackendSwitchOff_SellsNothing_AndDoesNotCallTheBackend_ButStillDefersTheAck()
        {
            var fx = new VerifiedStoreFixture();
            fx.Backend.FailNext(BackendError.Network("offline"));
            fx.Buy("af_coins_2");
            fx.Switch.BizBackendEnabled = false;

            Assert.That(fx.Service.IsAvailable, Is.False);
            Assert.That(fx.Buy("af_coins_3").Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(fx.Billing.Requests, Has.Count.EqualTo(1));

            fx.Clock.Advance(TimeSpan.FromHours(73));
            fx.Service.Resume();
            Assert.That(fx.VerifyCalls, Is.EqualTo(1), "biz_backend_enabled = false: sin red");
            Assert.That(fx.Billing.LocallyAcknowledged, Has.Count.EqualTo(1), "Play no la reembolsa");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));

            fx.Switch.BizBackendEnabled = true;
            fx.Service.Pump();
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(BackendSwitch.Key, Is.EqualTo("biz_backend_enabled"));
            Assert.That(new BackendSwitch().BizBackendEnabled, Is.True, "default [P]");
        }

        [Test]
        public void CancelledOrFailedInTheStore_ChangesNothing()
        {
            var fx = new VerifiedStoreFixture();
            fx.Billing.Mode = FakeBillingMode.Cancel;
            Assert.That(fx.Buy("af_rescue", level: 31).Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(fx.Profile.Daily.AfRescueDeclinedDays, Is.EqualTo(1), "cancelar la Balsa cuenta como rechazo (04 §6.2)");
            fx.Billing.Mode = FakeBillingMode.Fail;
            Assert.That(fx.Buy("af_coins_2").Status, Is.EqualTo(PurchaseStatus.Failed));
            Assert.That(fx.VerifyCalls, Is.EqualTo(0));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(fx.Service.IsPurchasing, Is.False);
        }

        [Test]
        public void PricesComeFromTheStore()
        {
            var fx = new VerifiedStoreFixture();
            Assert.That(fx.Service.PriceOf("af_coins_2"), Is.EqualTo(fx.Billing.PriceOf(fx.Ledger.Catalog.Get("af_coins_2"))));
        }

        [Test]
        public void CatalogGrants_MirrorTheServerCatalog()
        {
            StoreCatalog catalog = StoreCatalog.Default;
            PurchaseGrant rescue = CatalogGrants.For(catalog, "af_rescue");
            Assert.That(rescue.Seeds, Is.EqualTo(1000));
            Assert.That(rescue.Items.Count, Is.EqualTo(3));
            PurchaseGrant piggy = CatalogGrants.For(catalog, "af_piggy");
            Assert.That(piggy.Seeds, Is.EqualTo(0));
            Assert.That(piggy.Items[0].Id, Is.EqualTo("piggy"));
            Assert.That(CatalogGrants.For(catalog, "af_infinite_canoes_1h").Items[0].Count, Is.EqualTo(60));
            Assert.That(CatalogGrants.EntitlementsFor(catalog, "af_noads")!.NoAds, Is.True);
            Assert.That(CatalogGrants.EntitlementsFor(catalog, "af_coins_2"), Is.Null);
        }
    }
}
