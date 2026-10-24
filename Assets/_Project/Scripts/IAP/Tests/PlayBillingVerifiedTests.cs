#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// La tienda verificada (<see cref="VerifiedStoreService"/>, F3-3) sobre la tienda de Play real (<see cref="PlayBillingClient"/>) con un
    /// puerto de Play falso y el backend falso, detrás de <see cref="BillingReadyStoreService"/> y <see cref="ConsentStoreService"/>
    /// como en el juego (D-040; 08 §10.3; D-035; B12). Lo que se vigila en todas: nada se acredita sin <c>ok</c> o <c>duplicate</c> del
    /// servidor, nada se acredita dos veces y Play solo consume o reconoce después de acreditar.
    /// </summary>
    public sealed class PlayBillingVerifiedTests
    {
        private const string Uid = "uidTest0123456789";

        /// <summary>Una sesión de la app: perfil del disco, tienda de Play sobre la cuenta compartida y el mismo servidor.</summary>
        private sealed class Session
        {
            public Session(FakePlayAccount account, MemoryProfileStore disk, ManualClock clock, FakeBackendClient? backend = null, bool start = true)
            {
                Account = account;
                Disk = disk;
                Clock = clock;
                ProfileLoadResult load = disk.Load();
                Profile = load.HasProfile ? load.Profile! : Wallet.NewProfile(clock);
                Wallet = new Wallet(Profile, disk, clock);
                Ledger = new StoreLedger(Profile, Wallet, disk, clock);
                StoreCatalog catalog = Ledger.Catalog;
                Backend = backend ?? new FakeBackendClient(clock)
                {
                    GrantFor = r => CatalogGrants.For(catalog, r.Sku),
                    EntitlementsFor = r => CatalogGrants.EntitlementsFor(catalog, r.Sku),
                };
                Port = new FakePlayStorePort(account);
                Port.PriceAll(catalog);
                Billing = new PlayBillingClient(Port, catalog, "USD");
                Verified = new VerifiedStoreService(Ledger, Billing, Backend, clock, new BackendSwitch(), new FixedPlayerUid(Uid));
                Verified.VerificationChanged += e => Events.Add(e.State);
                Ready = new BillingReadyStoreService(Verified, Billing);
                Billing.UnfinishedChanged += () => Verified.Resume(); // lo que hace el juego (parche IAP-bootstrap)
                if (start)
                {
                    Billing.Start();
                    Verified.Resume();
                }
            }

            public FakePlayAccount Account { get; }

            public MemoryProfileStore Disk { get; }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public Wallet Wallet { get; }

            public StoreLedger Ledger { get; }

            public FakeBackendClient Backend { get; }

            public FakePlayStorePort Port { get; }

            public PlayBillingClient Billing { get; }

            public VerifiedStoreService Verified { get; }

            public BillingReadyStoreService Ready { get; }

            public List<PurchaseVerificationState> Events { get; } = new List<PurchaseVerificationState>();

            public Session Reopen(bool start = true) => new Session(Account, Disk, Clock, Backend, start);

            public PurchaseResult? TryBuy(IStoreService store, string sku = "af_coins_2", int level = 0)
            {
                PurchaseResult? result = null;
                store.Purchase(new PurchaseRequest(sku, PurchaseContext.Shop, level), r =>
                {
                    Assert.That(result, Is.Null, "onDone una sola vez");
                    result = r;
                });
                return result;
            }

            public PurchaseResult Buy(string sku = "af_coins_2") => TryBuy(Ready, sku)!;

            public Profile Saved() => Disk.Load().Profile!;
        }

        private static Session NewSession(FakeBackendClient? backend = null)
        {
            return new Session(new FakePlayAccount(), new MemoryProfileStore(), new ManualClock(StoreFixture.T0, TimeSpan.FromHours(-5)), backend);
        }

        private static string Hash(string token) => PurchaseTokenHash.Of(token);

        // ------------------------------------------------------------------ compra normal

        [Test]
        public void Ok_CreditsOnce_ThenPlayConsumes_InThatOrder()
        {
            Session s = NewSession();
            int seedsOnDiskWhenConsumed = -1;
            s.Port.OnConfirm = _ => seedsOnDiskWhenConsumed = s.Saved().Wallet.Seeds;

            PurchaseResult result = s.Buy();

            string token = FakePlayAccount.Token(1);
            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(seedsOnDiskWhenConsumed, Is.EqualTo(1000), "lo acreditado ya estaba en disco al consumir (se acredita antes de consumir)");
            Assert.That(s.Port.Confirmed, Is.EqualTo(new[] { token }));
            Assert.That(s.Account.StateOf(token), Is.EqualTo(FakePlayState.Consumed));
            Assert.That(s.Backend.VerifyRequests, Has.Count.EqualTo(1));
            Assert.That(s.Backend.VerifyRequests[0].Token, Is.EqualTo(token), "el purchaseToken de Play va al servidor");
            Assert.That(s.Backend.VerifyRequests[0].ObfuscatedAccountId, Is.EqualTo(Uid));
            Assert.That(s.Account.Orders[token].ObfuscatedAccountId, Is.EqualTo(Uid), "y a Play como setObfuscatedAccountId");
            Assert.That(s.Profile.AppliedPurchases, Is.EqualTo(new[] { Hash(token) }));
            Assert.That(s.Verified.PriceOf("af_coins_2"), Is.EqualTo(new LocalPrice("BRL", 4.99m)), "el Mercado enseña el precio de la tienda");
        }

        [Test]
        public void Cancelled_NothingChanges()
        {
            Session s = NewSession();
            s.Port.NextOutcome = FakePlayOutcome.Cancel;

            Assert.That(s.Buy().Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(s.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(s.Backend.VerifyRequests, Is.Empty);
            Assert.That(s.Verified.IsPurchasing, Is.False);
            Assert.That(s.Buy().Status, Is.EqualTo(PurchaseStatus.Cancelled), "se puede volver a intentar");
        }

        // ------------------------------------------------------------------ sin verificación no hay crédito

        [Test]
        public void ServerDown_NoCredit_NoConsume_ThenCreditedOnceWhenItAnswers()
        {
            Session s = NewSession();
            s.Backend.FailNext(BackendError.Network("offline"), 3);

            Assert.That(s.Buy().Status, Is.EqualTo(PurchaseStatus.Pending));
            string token = FakePlayAccount.Token(1);
            Assert.That(s.Wallet.Seeds, Is.EqualTo(0), "sin respuesta del servidor no se acredita");
            Assert.That(s.Port.Confirmed, Is.Empty, "ni se consume");

            // Play repite el pedido y el juego vuelve a primer plano: una sola entrada en la cola.
            s.Port.Redeliver(token);
            s.Billing.Refresh();
            s.Verified.Resume();
            Assert.That(s.Verified.PendingVerifications, Has.Count.EqualTo(1));

            for (int i = 0; i < 20 && s.Verified.PendingVerifications.Count > 0; i++)
            {
                s.Clock.Advance(TimeSpan.FromMinutes(5));
                s.Verified.Pump();
            }

            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(s.Port.Confirmed, Is.EqualTo(new[] { token }));
            Assert.That(s.Profile.AppliedPurchases, Has.Count.EqualTo(1));
        }

        [TestCase(400, ApiErrorCode.BadRequest)]
        [TestCase(409, ApiErrorCode.TokenBelongsToOtherUser)]
        public void Rejected_NeitherCreditedNorConsumed(int status, ApiErrorCode code)
        {
            Session s = NewSession();
            s.Backend.FailNext(BackendError.Http(status, code));

            Assert.That(s.Buy().Status, Is.EqualTo(PurchaseStatus.VerifyFailed));
            string token = FakePlayAccount.Token(1);
            Assert.That(s.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(s.Port.Confirmed, Is.Empty, "consumir también reconoce: Play ya no la reembolsaría");
            Assert.That(s.Account.StateOf(token), Is.EqualTo(FakePlayState.Purchased));

            // La misma sesión no la reenvía por un aviso repetido.
            s.Port.Redeliver(token);
            s.Verified.Resume();
            Assert.That(s.Backend.VerifyRequests, Has.Count.EqualTo(1));
        }

        [Test]
        public void ServerSaysPending_NoCredit_UntilItSaysOk()
        {
            Session s = NewSession();
            s.Backend.MarkPending(FakePlayAccount.Token(1));

            Assert.That(s.Buy().Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(s.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(s.Port.Confirmed, Is.Empty);

            s.Backend.ClearPending(FakePlayAccount.Token(1));
            for (int i = 0; i < 20 && s.Wallet.Seeds == 0; i++)
            {
                s.Clock.Advance(TimeSpan.FromMinutes(5));
                s.Verified.Pump();
            }

            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(s.Port.Confirmed, Has.Count.EqualTo(1));
        }

        // ------------------------------------------------------------------ PENDING de Play

        [Test]
        public void PaymentPending_IsNotVerified_UntilPlayCollectsIt_ThenCreditedOnce()
        {
            Session s = NewSession();
            s.Port.NextOutcome = FakePlayOutcome.PaymentPending;

            Assert.That(s.Buy().Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(s.Backend.VerifyRequests, Is.Empty, "PENDING: nada que verificar");
            Assert.That(s.Verified.IsPurchasing, Is.False);

            s.Port.Settle(FakePlayAccount.Token(1)); // el jugador paga en OXXO; Play avisa con la app abierta

            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000), "UnfinishedChanged → Resume → verify → ok");
            Assert.That(s.Backend.VerifyRequests, Has.Count.EqualTo(1));
            Assert.That(s.Port.Confirmed, Has.Count.EqualTo(1));
            Assert.That(s.Events, Does.Contain(PurchaseVerificationState.Credited));
        }

        [Test]
        public void PaymentPending_CollectedWhileClosed_IsCreditedOnTheNextLaunch()
        {
            Session s = NewSession();
            s.Port.NextOutcome = FakePlayOutcome.PaymentPending;
            s.Buy();
            s.Account.Orders[FakePlayAccount.Token(1)].State = FakePlayState.Purchased; // Play la cobró con la app cerrada

            Session next = s.Reopen();

            Assert.That(next.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(next.Port.Confirmed, Has.Count.EqualTo(1));
        }

        // ------------------------------------------------------------------ Resume y repeticiones

        [Test]
        public void CrashBetweenCreditAndConsume_NextLaunchOnlyConsumes()
        {
            Session s = NewSession();
            s.Port.CrashOnConfirm = true;
            Assert.That(s.Buy().IsPurchased, Is.True);
            string token = FakePlayAccount.Token(1);
            Assert.That(s.Account.StateOf(token), Is.EqualTo(FakePlayState.Purchased), "la app se cerró antes de consumir");

            Session next = s.Reopen();

            Assert.That(next.Wallet.Seeds, Is.EqualTo(1000), "no se acredita otra vez");
            Assert.That(next.Account.StateOf(token), Is.EqualTo(FakePlayState.Consumed));
            Assert.That(next.Backend.VerifyRequests, Has.Count.EqualTo(1), "applied_purchases ya lo tenía: ni se pregunta");
            Assert.That(next.Events, Is.EqualTo(new[] { PurchaseVerificationState.AlreadyApplied }));
        }

        [Test]
        public void CrashAfterPaying_BeforeTheAppKnew_IsCreditedOnTheNextLaunch()
        {
            Session s = NewSession();
            s.Port.NextOutcome = FakePlayOutcome.Silent;
            Assert.That(s.TryBuy(s.Ready), Is.Null);
            s.Account.Add("af_coins_2", FakePlayState.Purchased, Uid); // Play cobró y la app murió antes del aviso

            Session next = s.Reopen();

            Assert.That(next.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(next.Port.Confirmed, Has.Count.EqualTo(1));
            Assert.That(next.Profile.AppliedPurchases, Has.Count.EqualTo(1));
        }

        [Test]
        public void RepeatedDeliveriesWhileVerifying_OneVerification_OneCredit()
        {
            Session s = NewSession();
            s.Backend.HoldVerifyResponses = true;

            Assert.That(s.TryBuy(s.Ready), Is.Null, "esperando al servidor");
            string token = FakePlayAccount.Token(1);
            s.Port.Redeliver(token);
            s.Billing.Refresh();
            s.Verified.Resume();
            s.Verified.Pump();
            Assert.That(s.Backend.VerifyRequests, Has.Count.EqualTo(1));

            s.Backend.HoldVerifyResponses = false;
            s.Backend.ReleaseVerifyResponses();

            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(s.Port.Confirmed, Is.EqualTo(new[] { token }));
            s.Verified.Resume();
            s.Billing.Refresh();
            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(s.Backend.VerifyRequests, Has.Count.EqualTo(1));
        }

        [Test]
        public void TwoPurchasesInARow_EachCreditedOnce()
        {
            Session s = NewSession();
            Assert.That(s.Buy().IsPurchased, Is.True);
            Assert.That(s.Buy().IsPurchased, Is.True);
            Assert.That(s.Buy("af_coins_3").IsPurchased, Is.True);

            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000 + 1000 + 2800));
            Assert.That(s.Profile.AppliedPurchases, Has.Count.EqualTo(3));
            Assert.That(s.Port.Confirmed, Has.Count.EqualTo(3));
        }

        // ------------------------------------------------------------------ deferred_ack

        [Test]
        public void DeferredAck_Consumable_NotConsumedNorCredited_UntilTheServerAnswers()
        {
            Session s = NewSession();
            s.Backend.FailNext(BackendError.Network("offline"), 100000);
            Assert.That(s.Buy("af_coins_3").Status, Is.EqualTo(PurchaseStatus.Pending));
            for (int hour = 0; hour < 80; hour++)
            {
                s.Clock.Advance(TimeSpan.FromHours(1));
                s.Verified.Pump();
            }

            string token = FakePlayAccount.Token(1);
            Assert.That(s.Events, Does.Contain(PurchaseVerificationState.DeferredAck));
            // D-040, añadido de Q-IAP-1: el consumible se reconoce sin consumir (antes, AcknowledgeUnsupported == 1 con Unity IAP).
            Assert.That(s.Port.Acknowledged, Is.EqualTo(new[] { token }), "08 §10.3: acknowledge local a las 48 h");
            Assert.That(s.Billing.AcknowledgeUnsupported, Is.EqualTo(0));
            Assert.That(s.Port.Confirmed, Is.Empty, "sin consumir");
            Assert.That(s.Wallet.Seeds, Is.EqualTo(0));

            s.Backend.ClearFailures();
            s.Clock.Advance(TimeSpan.FromHours(6));
            s.Verified.Pump();
            Assert.That(s.Wallet.Seeds, Is.EqualTo(2800));
            Assert.That(s.Port.Confirmed, Is.EqualTo(new[] { token }));
        }

        [Test]
        public void DeferredAck_NoAds_AcknowledgedButNotCredited_UntilTheServerAnswers()
        {
            Session s = NewSession();
            s.Backend.FailNext(BackendError.Network("offline"), 100000);
            Assert.That(s.Buy("af_noads").Status, Is.EqualTo(PurchaseStatus.Pending));
            for (int hour = 0; hour < 80; hour++)
            {
                s.Clock.Advance(TimeSpan.FromHours(1));
                s.Verified.Pump();
            }

            string token = FakePlayAccount.Token(1);
            Assert.That(s.Account.StateOf(token), Is.EqualTo(FakePlayState.Acknowledged), "Play ya no la reembolsa");
            Assert.That(s.Profile.Entitlements.NoAds, Is.False, "D-035: el deferred_ack no acredita");

            // Se reabre la app: Play la devuelve como reconocida y sigue en la cola, sin otra entrada.
            Session next = s.Reopen();
            Assert.That(next.Verified.PendingVerifications, Has.Count.EqualTo(1));
            Assert.That(next.Profile.Entitlements.NoAds, Is.False);

            s.Backend.ClearFailures();
            next.Clock.Advance(TimeSpan.FromHours(6));
            next.Verified.Pump();
            Assert.That(next.Profile.Entitlements.NoAds, Is.True);
            Assert.That(next.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(next.Port.Confirmed, Is.Empty, "ya reconocida: no se vuelve a confirmar");
            Assert.That(next.Billing.UnfinishedTransactions(), Is.Empty);
        }

        [Test]
        public void DeferredAck_ThatPlayDidNotDo_IsAskedAgainOnResume_InThisSessionAndTheNext_WithoutCrediting()
        {
            // 08 §10.3: el acknowledge local evita el reembolso automático. Si Play no lo hizo (sin red), se vuelve a pedir al volver a
            // primer plano y en el arranque siguiente; nunca se consume ni se acredita sin el servidor (D-035).
            Session s = NewSession();
            s.Backend.FailNext(BackendError.Network("offline"), 100000);
            s.Port.FailAcknowledge = true;
            Assert.That(s.Buy("af_coins_3").Status, Is.EqualTo(PurchaseStatus.Pending));
            for (int hour = 0; hour < 80; hour++)
            {
                s.Clock.Advance(TimeSpan.FromHours(1));
                s.Verified.Pump();
            }

            string token = FakePlayAccount.Token(1);
            Assert.That(s.Port.Acknowledged, Is.EqualTo(new[] { token }), "se pidió una vez");
            Assert.That(s.Account.Orders[token].AcknowledgedOnly, Is.False, "Play no lo hizo");

            s.Billing.Refresh(); // vuelta a primer plano: la consulta y el Resume del juego
            s.Verified.Resume();
            Assert.That(s.Port.Acknowledged, Is.EqualTo(new[] { token, token }), "se vuelve a pedir");

            Session next = s.Reopen(); // arranque siguiente, ya con red para Play
            next.Port.FailAcknowledge = false;
            next.Verified.Resume();
            Assert.That(next.Port.Acknowledged, Is.EqualTo(new[] { token }));
            Assert.That(next.Account.Orders[token].AcknowledgedOnly, Is.True, "reconocido sin consumir");
            Assert.That(next.Account.StateOf(token), Is.EqualTo(FakePlayState.Purchased));
            next.Verified.Resume();
            Assert.That(next.Port.Acknowledged, Has.Count.EqualTo(1), "ya reconocido: no se repite");
            Assert.That(next.Wallet.Seeds, Is.EqualTo(0), "el deferred_ack no acredita");
            Assert.That(next.Port.Confirmed, Is.Empty, "ni consume");

            s.Backend.ClearFailures();
            next.Clock.Advance(TimeSpan.FromHours(6));
            next.Verified.Pump();
            Assert.That(next.Wallet.Seeds, Is.EqualTo(2800), "acreditado una vez al verificar");
            Assert.That(next.Port.Confirmed, Is.EqualTo(new[] { token }), "y después consumido");
        }

        // ------------------------------------------------------------------ restauración

        [Test]
        public void NewInstall_SameUid_NoAdsComesBackFromPlay_CreditedOnce()
        {
            Session s = NewSession();
            Assert.That(s.Buy("af_noads").IsPurchased, Is.True);

            // Instalación nueva (perfil vacío), misma cuenta de Google y mismo UID: Play devuelve Sin anuncios y el servidor dice duplicate.
            var fresh = new Session(s.Account, new MemoryProfileStore(), s.Clock, s.Backend);

            Assert.That(fresh.Profile.Entitlements.NoAds, Is.True);
            Assert.That(fresh.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fresh.Events, Does.Contain(PurchaseVerificationState.Credited));

            Session again = fresh.Reopen();
            Assert.That(again.Wallet.Seeds, Is.EqualTo(500), "la siguiente apertura no la acredita otra vez");
            Assert.That(again.Backend.VerifyRequests, Has.Count.EqualTo(2));
        }

        // ------------------------------------------------------------------ la tienda lista y el consentimiento

        [Test]
        public void BeforePlayIsReady_TheStoreIsUnavailable_AndNothingOpens()
        {
            Session s = NewSession();
            Session cold = s.Reopen(start: false);
            cold.Port.Async = true;
            cold.Billing.Start();

            Assert.That(cold.Ready.IsAvailable, Is.False);
            Assert.That(cold.TryBuy(cold.Ready)!.Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(cold.Port.Purchases, Is.Empty);

            cold.Port.Flush();
            Assert.That(cold.Ready.IsAvailable, Is.True);
        }

        [Test]
        public void ASkuWithoutTheStoresPrice_IsNotSold_TheRestIs()
        {
            Session s = NewSession();
            Session cold = s.Reopen(start: false);
            cold.Port.Prices.Remove("af_coins_6"); // no publicado en Play Console (o moneda que no se escribe)
            cold.Billing.Start();

            Assert.That(cold.Ready.IsAvailable, Is.True);
            Assert.That(cold.TryBuy(cold.Ready, "af_coins_6")!.Status, Is.EqualTo(PurchaseStatus.StoreUnavailable),
                "05 §4.1: no se cobra con el precio de respaldo");
            Assert.That(cold.Port.Purchases, Is.Empty);
            Assert.That(cold.Buy("af_coins_2").IsPurchased, Is.True);
        }

        [Test]
        public void PlayConnected_WithoutAnyStorePrice_TheStoreIsUnavailable_UntilARefreshBringsThePrices()
        {
            // Prueba del 2026-10-23, observación 1: la app aún no está en Play Console (Play conecta, pero sin productos).
            Session s = NewSession();
            Session cold = s.Reopen(start: false);
            cold.Port.Prices.Clear();
            cold.Billing.Start();

            Assert.That(cold.Billing.IsReady, Is.True, "Play conectada");
            Assert.That(cold.Ready.IsAvailable, Is.False, "sin ningún precio de la tienda no hay Mercado");
            Assert.That(cold.TryBuy(cold.Ready)!.Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(cold.Port.Purchases, Is.Empty);

            cold.Port.PriceAll(cold.Ledger.Catalog); // los productos se publican en Play Console
            cold.Billing.Refresh(); // al volver a primer plano: se vuelven a pedir los productos
            Assert.That(cold.Ready.IsAvailable, Is.True, "el Mercado aparece en la misma sesión");
            Assert.That(cold.Buy("af_coins_2").IsPurchased, Is.True);
        }

        [Test]
        public void Under13_NoPurchases_AndPlayNeverOpens()
        {
            Session s = NewSession();
            var store = new ConsentStoreService(s.Ready) { Confirmation = new ImmediatePurchaseConfirmation() };
            store.Apply(Flags(AgeBand.Under13));

            Assert.That(store.IsAvailable, Is.False);
            Assert.That(s.TryBuy(store)!.Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(s.Port.Purchases, Is.Empty);
        }

        [Test]
        public void Teen_SecondConfirmationWithTheStorePrice_BeforePlay()
        {
            Session s = NewSession();
            var confirm = new ImmediatePurchaseConfirmation(false);
            LocalPrice? asked = null;
            var store = new ConsentStoreService(s.Ready) { Confirmation = new RecordingConfirmation(confirm, p => asked = p) };
            store.Apply(Flags(AgeBand.Teen));

            Assert.That(s.TryBuy(store)!.Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(asked, Is.EqualTo(new LocalPrice("BRL", 4.99m)), "Q-B12-6: la confirmación lleva el precio de la tienda");
            Assert.That(s.Port.Purchases, Is.Empty, "«Cancelar» no abre la hoja de Play");

            confirm.Answer = true;
            Assert.That(s.TryBuy(store)!.IsPurchased, Is.True);
            Assert.That(s.Port.Purchases, Has.Count.EqualTo(1));
            Assert.That(s.Wallet.Seeds, Is.EqualTo(1000));
        }

        private static ConsentFlags Flags(AgeBand band)
        {
            var record = new ConsentRecord(LegalProfiles.Co1581, 1, new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc), true, true, band);
            return ConsentPolicy.Flags(record, ConsentConfig.Defaults);
        }

        private sealed class RecordingConfirmation : IPurchaseConfirmation
        {
            private readonly IPurchaseConfirmation _inner;
            private readonly Action<LocalPrice> _onPrice;

            public RecordingConfirmation(IPurchaseConfirmation inner, Action<LocalPrice> onPrice)
            {
                _inner = inner;
                _onPrice = onPrice;
            }

            public void Confirm(StoreProduct product, LocalPrice price, Action<bool> onAnswer)
            {
                _onPrice(price);
                _inner.Confirm(product, price, onAnswer);
            }
        }
    }
}
