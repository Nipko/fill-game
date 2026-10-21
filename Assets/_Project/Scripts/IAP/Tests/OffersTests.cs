#nullable enable
using System;
using AF.Economy;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Bloque B5 (05 §4.1 y §5; 08 §9): las cifras de las ofertas, el Cántaro (llenado, tope, compra y vaciado) y el
    /// <c>offer_scheduler</c> (Mochila de inicio con su ventana, su temporizador y sus recordatorios; Sin anuncios con su disparador,
    /// su tope diario y sus rechazos; Cántaro lleno; skin del mundo; un takeover por sesión y ninguna oferta en la primera sesión).
    /// </summary>
    public sealed class OffersTests
    {
        private sealed class Fx
        {
            public Fx(int session = 2, OffersConfig? config = null)
            {
                Store = new StoreFixture();
                Session = session;
                Records = new InMemoryOffersStore();
                State = new OffersState(Records);
                Piggy = new PiggyJar(State, config, Store.Ledger.Config);
                Budget = new SessionTakeoverBudget(() => Session);
                Offers = new OfferScheduler(State, Store.Ledger, Piggy, Store.Clock, StoreFixture.T0, Budget, config);
            }

            public StoreFixture Store { get; }

            public int Session { get; set; }

            public InMemoryOffersStore Records { get; }

            public OffersState State { get; }

            public PiggyJar Piggy { get; }

            public SessionTakeoverBudget Budget { get; }

            public OfferScheduler Offers { get; }

            public OfferKind Win(int level, int world = 0)
            {
                var ctx = new VictoryOfferContext(level, Session, world == 0 ? 2 : world);
                return Offers.TakeAtVictory(in ctx);
            }

            public void NewSession()
            {
                Session++;
            }

            public void FillPiggyTo(int seeds)
            {
                State.Record.PiggySeeds = seeds;
                State.Record.PiggyCap = 3000;
                State.Persist();
            }
        }

        [Test]
        public void Defaults_AreThoseOf08_9()
        {
            OffersConfig c = OffersConfig.Defaults;
            Assert.That(c.EcoStarterWindowLevels, Is.EqualTo(new[] { 10, 20 }));
            Assert.That(c.EcoStarterTimerH, Is.EqualTo(48));
            Assert.That(c.EcoStarterFirstShow, Is.EqualTo("L12_or_session2"));
            Assert.That(c.EcoNoadsCardTrigger, Is.EqualTo("3_interstitials_or_d2"));
            Assert.That(c.EcoPiggyFromLevel, Is.EqualTo(20));
            Assert.That(c.EcoPiggyFill, Is.EqualTo(new[] { 15, 30, 40 }));
            Assert.That(c.StarterFirstShow.MinLevel, Is.EqualTo(12));
            Assert.That(c.StarterFirstShow.MinSession, Is.EqualTo(2));
            Assert.That(c.NoAdsTrigger.Interstitials, Is.EqualTo(3));
            Assert.That(c.NoAdsTrigger.Day, Is.EqualTo(2));
            Assert.DoesNotThrow(() => c.Validate());
        }

        [TestCase(14)]
        [TestCase(31)]
        public void Validate_RejectsPiggyFromLevelOutsideItsRange(int level)
        {
            var c = new OffersConfig { EcoPiggyFromLevel = level };
            Assert.Throws<InvalidOperationException>(() => c.Validate());
        }

        [TestCase("L12")]
        [TestCase("12_or_session2")]
        [TestCase("Lx_or_session2")]
        [TestCase("L12_or_session")]
        [TestCase("")]
        public void Validate_RejectsAFirstShowWithAnotherShape(string text)
        {
            var c = new OffersConfig { EcoStarterFirstShow = text };
            Assert.Throws<InvalidOperationException>(() => c.Validate());
        }

        [TestCase("3_interstitials")]
        [TestCase("_interstitials_or_d2")]
        [TestCase("3_interstitials_or_dx")]
        public void Validate_RejectsANoAdsTriggerWithAnotherShape(string text)
        {
            var c = new OffersConfig { EcoNoadsCardTrigger = text };
            Assert.Throws<InvalidOperationException>(() => c.Validate());
        }

        [Test]
        public void Validate_RejectsAWindowOrFillOfAnotherShape()
        {
            Assert.Throws<InvalidOperationException>(() => new OffersConfig { EcoStarterWindowLevels = new[] { 20, 10 } }.Validate());
            Assert.Throws<InvalidOperationException>(() => new OffersConfig { EcoStarterWindowLevels = new[] { 10 } }.Validate());
            Assert.Throws<InvalidOperationException>(() => new OffersConfig { EcoPiggyFill = new[] { 15, 30 } }.Validate());
            Assert.Throws<InvalidOperationException>(() => new OffersConfig { EcoPiggyFill = new[] { 15, 0, 40 } }.Validate());
            Assert.Throws<InvalidOperationException>(() => new OffersConfig { EcoStarterTimerH = 0 }.Validate());
        }

        [Test]
        public void Piggy_FillsFromLevel20_With15_30_40_AndSavesOncePerLevel()
        {
            var fx = new Fx();

            Assert.That(fx.Piggy.OnCampaignVictory(19, RewardTier.Normal), Is.Null, "antes de eco_piggy_from_level no se llena");
            Assert.That(fx.Piggy.VisibleAt(19), Is.False);
            Assert.That(fx.Piggy.VisibleAt(20), Is.True);

            PiggyFill? a = fx.Piggy.OnCampaignVictory(20, RewardTier.Normal);
            PiggyFill? b = fx.Piggy.OnCampaignVictory(21, RewardTier.Hard);
            PiggyFill? c = fx.Piggy.OnCampaignVictory(22, RewardTier.VeryHard);
            Assert.That(a!.Value.Added, Is.EqualTo(15));
            Assert.That(b!.Value.Added, Is.EqualTo(30));
            Assert.That(c!.Value.Added, Is.EqualTo(40));
            Assert.That(fx.Piggy.Seeds, Is.EqualTo(85));
            Assert.That(fx.Piggy.OnCampaignVictory(22, RewardTier.VeryHard), Is.Null, "la misma victoria no suma dos veces");
            Assert.That(fx.Records.Load().PiggySeeds, Is.EqualTo(85), "guardado");
            Assert.That(fx.Records.Load().PiggyLastLevel, Is.EqualTo(22));
        }

        [Test]
        public void Piggy_StopsAtTheCap_AndSaysWhenItBecameFull()
        {
            var fx = new Fx();
            fx.FillPiggyTo(2990);

            PiggyFill? fill = fx.Piggy.OnCampaignVictory(100, RewardTier.Normal);

            Assert.That(fill!.Value.Added, Is.EqualTo(10));
            Assert.That(fill.Value.Seeds, Is.EqualTo(3000));
            Assert.That(fill.Value.BecameFull, Is.True);
            Assert.That(fx.Piggy.IsFull, Is.True);
            PiggyFill? again = fx.Piggy.OnCampaignVictory(101, RewardTier.Normal);
            Assert.That(again!.Value.Added, Is.EqualTo(0));
            Assert.That(again.Value.BecameFull, Is.False, "ya estaba lleno");
        }

        [Test]
        public void Piggy_IsBoughtFrom1000_CreditsExactlyItsSeeds_AndEmpties()
        {
            var fx = new Fx();
            fx.FillPiggyTo(999);
            Assert.That(fx.Piggy.CanBuy, Is.False);
            Assert.That(fx.Store.Buy("af_piggy", piggySeeds: 999).Status, Is.EqualTo(PurchaseStatus.NotAvailable));

            fx.FillPiggyTo(1230);
            Assert.That(fx.Piggy.CanBuy, Is.True);
            PurchaseResult? result = null;
            fx.Store.Service.Purchase(fx.Piggy.Request(PurchaseContext.Piggy, 40), r => result = r);

            Assert.That(result!.IsPurchased, Is.True);
            Assert.That(fx.Store.Wallet.Seeds, Is.EqualTo(1230), "exactamente lo acumulado (05 §4.1)");
            Assert.That(fx.Piggy.OnPurchaseResult(result), Is.True);
            Assert.That(fx.Piggy.Seeds, Is.EqualTo(0), "se vacía y vuelve a llenarse");
            Assert.That(fx.Records.Load().PiggySeeds, Is.EqualTo(0));
        }

        [Test]
        public void Piggy_ACancelledPurchase_KeepsTheSeeds()
        {
            var fx = new Fx();
            fx.FillPiggyTo(1500);
            fx.Store.Presenter.Confirm = false;
            PurchaseResult? result = null;
            fx.Store.Service.Purchase(fx.Piggy.Request(PurchaseContext.Shop, 40), r => result = r);

            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(fx.Piggy.OnPurchaseResult(result), Is.False);
            Assert.That(fx.Piggy.Seeds, Is.EqualTo(1500));
            Assert.That(fx.Store.Wallet.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void Piggy_APendingPurchase_KeepsTheJar()
        {
            var fx = new VerifiedStoreFixture();
            fx.Billing.Mode = FakeBillingMode.Pending; // pago en efectivo de Play (OXXO, Efecty) aún sin pagar
            var piggy = new PiggyJar(new OffersState(new InMemoryOffersStore()));
            PurchaseResult? result = null;
            fx.Service.Purchase(new PurchaseRequest("af_piggy", PurchaseContext.Piggy, 40, 1500), r => result = r);

            Assert.That(result!.Status, Is.EqualTo(PurchaseStatus.Pending));
            Assert.That(piggy.OnPurchaseResult(result), Is.False, "Q-B5-12: no se vacía hasta acreditarse");
        }

        [Test]
        public void Piggy_ALowerCapFromRemoteConfig_KeepsTheJarFullAndBuyable()
        {
            var fx = new Fx();
            fx.FillPiggyTo(3000);
            fx.Store.Ledger.Config.EcoPiggyCap = 2000;

            Assert.That(fx.Piggy.Cap, Is.EqualTo(2000));
            Assert.That(fx.Piggy.Seeds, Is.EqualTo(2000), "lo guardado, con el tope nuevo");
            Assert.That(fx.Piggy.IsFull, Is.True);
            PurchaseResult? result = null;
            fx.Store.Service.Purchase(fx.Piggy.Request(PurchaseContext.Piggy, 40), r => result = r);
            Assert.That(result!.IsPurchased, Is.True, "la tienda no vende más que el tope: se vende el tope");
            Assert.That(fx.Store.Wallet.Seeds, Is.EqualTo(2000));
        }

        [Test]
        public void NoOfferCard_InTheFirstSession()
        {
            var fx = new Fx(session: 1);
            Assert.That(fx.Win(12), Is.EqualTo(OfferKind.None));
            Assert.That(fx.Win(13), Is.EqualTo(OfferKind.None));
        }

        [Test]
        public void Starter_FirstCard_AtTheFirstWinFrom12_InSession2_StartsIts48h()
        {
            var fx = new Fx();
            Assert.That(fx.Win(11), Is.EqualTo(OfferKind.None), "antes del 12 no");
            Assert.That(fx.Win(12), Is.EqualTo(OfferKind.Starter));
            Assert.That(fx.Offers.StarterExpiresAt, Is.EqualTo(StoreFixture.T0 + TimeSpan.FromHours(48)));
            Assert.That(fx.Offers.StarterHoursLeft, Is.EqualTo(48));
            Assert.That(fx.Records.Load().StarterFirstSeenAt, Is.EqualTo(StoreFixture.T0), "guardado");
        }

        [Test]
        public void Takeovers_AtMostOnePerSession_AndANewSessionRefillsTheQuota()
        {
            var fx = new Fx();
            Assert.That(fx.Win(12), Is.EqualTo(OfferKind.Starter));
            Assert.That(fx.Win(15), Is.EqualTo(OfferKind.None), "ui_max_takeovers_per_session = 1");

            fx.NewSession();
            Assert.That(fx.Budget.QuotaLeft, Is.True);
            Assert.That(fx.Win(15), Is.EqualTo(OfferKind.Starter), "recordatorio del 15 en la sesión siguiente");
        }

        [Test]
        public void Starter_RemindsAt15And19_Once()
        {
            var fx = new Fx();
            fx.Win(12);
            fx.NewSession();
            Assert.That(fx.Win(14), Is.EqualTo(OfferKind.None));
            Assert.That(fx.Win(15), Is.EqualTo(OfferKind.Starter));
            fx.NewSession();
            Assert.That(fx.Win(15), Is.EqualTo(OfferKind.None), "cada recordatorio, una vez");
            Assert.That(fx.Win(19), Is.EqualTo(OfferKind.Starter));
            Assert.That(fx.Records.Load().StarterReminders, Is.EqualTo(new[] { 15, 19 }));
        }

        [Test]
        public void Starter_AReminderWithoutQuota_ComesAtTheNextWin_AndTheFirstCardCoversThePastOnes()
        {
            var fx = new Fx();
            Assert.That(fx.Win(12), Is.EqualTo(OfferKind.Starter));
            Assert.That(fx.Win(15), Is.EqualTo(OfferKind.None), "sin cupo en la sesión");
            fx.NewSession();
            Assert.That(fx.Win(16), Is.EqualTo(OfferKind.Starter), "el recordatorio del 15 no se pierde");
            fx.NewSession();
            Assert.That(fx.Win(17), Is.EqualTo(OfferKind.None));

            var late = new Fx();
            Assert.That(late.Win(16), Is.EqualTo(OfferKind.Starter), "primera tarjeta en el 16");
            late.NewSession();
            Assert.That(late.Win(17), Is.EqualTo(OfferKind.None), "la primera tarjeta ya cubre el recordatorio del 15");
            Assert.That(late.Win(19), Is.EqualTo(OfferKind.Starter));

            var both = new Fx();
            both.Win(12);
            both.NewSession();
            Assert.That(both.Win(19), Is.EqualTo(OfferKind.Starter), "el 15 y el 19 pasados sin verlos: uno solo");
            Assert.That(both.Records.Load().StarterReminders, Is.EqualTo(new[] { 15, 19 }));
            both.NewSession();
            Assert.That(both.Win(19), Is.EqualTo(OfferKind.None));
        }

        [Test]
        public void Starter_LeavesAfter48h_OrAfterWinning20_OrOnceBought()
        {
            var fx = new Fx();
            fx.Win(12);
            Assert.That(fx.Offers.StarterAvailable(12), Is.True);
            fx.Store.Clock.Advance(TimeSpan.FromHours(48));
            Assert.That(fx.Offers.StarterAvailable(12), Is.False, "48 h desde la primera vista");
            fx.NewSession();
            Assert.That(fx.Win(15), Is.EqualTo(OfferKind.None));

            var window = new Fx();
            Assert.That(window.Offers.StarterAvailable(9), Is.False, "el Mercado se abre al ganar el 10");
            Assert.That(window.Offers.StarterAvailable(10), Is.True, "ventana 10-20 (eco_starter_window_levels)");
            Assert.That(window.Offers.StarterAvailable(19), Is.True);
            Assert.That(window.Offers.StarterAvailable(20), Is.False, "desaparece al pasar el 20");

            var bought = new Fx();
            Assert.That(bought.Store.Buy("af_starter").IsPurchased, Is.True);
            Assert.That(bought.Offers.StarterAvailable(12), Is.False);
            Assert.That(bought.Win(12), Is.EqualTo(OfferKind.None));
        }

        [Test]
        public void NoAds_NotBefore31_ThenOnDay2_OncePerDay()
        {
            var fx = new Fx();
            fx.Store.Clock.Advance(TimeSpan.FromDays(1));
            Assert.That(fx.Offers.DayIndex, Is.EqualTo(2));
            Assert.That(fx.Win(30), Is.EqualTo(OfferKind.None), "05 §10: ninguna oferta de pago antes del 31 salvo la Mochila");
            Assert.That(fx.Win(31), Is.EqualTo(OfferKind.NoAds));
            fx.NewSession();
            Assert.That(fx.Win(32), Is.EqualTo(OfferKind.None), "1 tarjeta al día");
            fx.Store.Clock.Advance(TimeSpan.FromDays(1));
            Assert.That(fx.Win(33), Is.EqualTo(OfferKind.NoAds));
        }

        [Test]
        public void NoAds_OnDay1_AfterTheThirdInterstitial()
        {
            var fx = new Fx();
            Assert.That(fx.Offers.DayIndex, Is.EqualTo(1));
            fx.Offers.RecordInterstitialSeen();
            fx.Offers.RecordInterstitialSeen();
            Assert.That(fx.Win(31), Is.EqualTo(OfferKind.None));
            fx.Offers.RecordInterstitialSeen();
            fx.Offers.RecordInterstitialSeen();
            Assert.That(fx.Records.Load().InterstitialsSeen, Is.EqualTo(3), "con tope en el del disparador");
            Assert.That(fx.Win(32), Is.EqualTo(OfferKind.NoAds));
        }

        [Test]
        public void NoAds_AfterThreeDeclines_OnlyInTheMarket_AndNeverOnceBought()
        {
            var fx = new Fx();
            for (int day = 1; day <= 3; day++)
            {
                fx.Store.Clock.Advance(TimeSpan.FromDays(1));
                fx.NewSession();
                Assert.That(fx.Win(30 + day), Is.EqualTo(OfferKind.NoAds));
                fx.Offers.RecordDeclined(OfferKind.NoAds);
            }

            fx.Store.Clock.Advance(TimeSpan.FromDays(1));
            fx.NewSession();
            Assert.That(fx.Win(40), Is.EqualTo(OfferKind.None), "tras 3 rechazos, solo Mercado");
            Assert.That(fx.Offers.NoAdsInMarket(40), Is.True);

            var owned = new Fx();
            owned.Store.Clock.Advance(TimeSpan.FromDays(1));
            Assert.That(owned.Store.Buy("af_noads").IsPurchased, Is.True);
            Assert.That(owned.Win(31), Is.EqualTo(OfferKind.None));
            Assert.That(owned.Offers.NoAdsInMarket(40), Is.False);
            Assert.That(owned.Offers.NoAdsOwned, Is.True);
        }

        [Test]
        public void NoAds_InTheMarketFrom22()
        {
            var fx = new Fx();
            Assert.That(fx.Offers.NoAdsInMarket(20), Is.False);
            Assert.That(fx.Offers.NoAdsInMarket(21), Is.True, "con el 22 en juego (05 §4.1)");
        }

        [Test]
        public void PiggyFull_OneCardPerFill_AfterNoAds()
        {
            var fx = new Fx();
            fx.FillPiggyTo(3000);
            Assert.That(fx.Win(30), Is.EqualTo(OfferKind.None), "antes del 31 no");
            Assert.That(fx.Win(40), Is.EqualTo(OfferKind.PiggyFull));
            fx.NewSession();
            Assert.That(fx.Win(41), Is.EqualTo(OfferKind.None), "1 tarjeta al llenarse");

            var both = new Fx();
            both.FillPiggyTo(3000);
            both.Store.Clock.Advance(TimeSpan.FromDays(1));
            Assert.That(both.Win(40), Is.EqualTo(OfferKind.NoAds), "prioridad de 05 §5: Sin anuncios antes que el Cántaro");
            both.NewSession();
            Assert.That(both.Win(41), Is.EqualTo(OfferKind.PiggyFull));
        }

        [Test]
        public void PiggyFull_ComesBackAfterBuyingAndFillingAgain()
        {
            var fx = new Fx();
            fx.FillPiggyTo(3000);
            Assert.That(fx.Win(40), Is.EqualTo(OfferKind.PiggyFull));
            PurchaseResult? result = null;
            fx.Store.Service.Purchase(fx.Piggy.Request(PurchaseContext.Piggy, 41), r => result = r);
            fx.Piggy.OnPurchaseResult(result!);
            Assert.That(fx.Records.Load().PiggyFullCardShown, Is.False);
            fx.FillPiggyTo(3000);
            fx.NewSession();
            Assert.That(fx.Win(42), Is.EqualTo(OfferKind.PiggyFull));
        }

        [Test]
        public void SkinCard_OffUntilTheArt_ThenFrom41_OncePerWorld()
        {
            var off = new Fx();
            Assert.That(off.Offers.SkinSalesEnabled, Is.False, "Q-B5-1 (a): sin venta de skins hasta el arte final");
            Assert.That(off.Win(41, world: 3), Is.EqualTo(OfferKind.None));

            var fx = new Fx();
            fx.Offers.SkinSalesEnabled = true;
            Assert.That(fx.Win(11, world: 2), Is.EqualTo(OfferKind.None), "en el mundo 2, solo en el Mercado");
            fx.NewSession();
            var ctx = new VictoryOfferContext(41, fx.Session, 3);
            Assert.That(fx.Offers.TakeAtVictory(in ctx), Is.EqualTo(OfferKind.SkinWorld));
            Assert.That(OfferScheduler.SkuOf(OfferKind.SkinWorld, in ctx), Is.EqualTo("af_skin_m03"));
            fx.NewSession();
            Assert.That(fx.Win(42, world: 3), Is.EqualTo(OfferKind.None), "1 por mundo");

            var owned = new Fx();
            owned.Offers.SkinSalesEnabled = true;
            owned.Store.Profile.Entitlements.Skins.Add("af_skin_m03");
            Assert.That(owned.Win(41, world: 3), Is.EqualTo(OfferKind.None), "desaparece al ganarla");
        }

        [Test]
        public void SkinCard_WaitsForAVictoryWithQuota_WhenAnotherOfferTookIt()
        {
            var fx = new Fx();
            fx.Offers.SkinSalesEnabled = true;
            fx.Store.Clock.Advance(TimeSpan.FromDays(1));
            Assert.That(fx.Win(41, world: 3), Is.EqualTo(OfferKind.NoAds), "prioridad de 05 §5");
            Assert.That(fx.Win(42, world: 3), Is.EqualTo(OfferKind.None), "sin cupo en la sesión");
            fx.NewSession();
            Assert.That(fx.Win(43, world: 3), Is.EqualTo(OfferKind.SkinWorld), "no se pierde: la primera victoria del mundo con cupo");
        }

        [Test]
        public void SkinInTheMarket_FromItsWorld_OnlyWithSales()
        {
            var fx = new Fx();
            Assert.That(SkinRewards.TryForWorld(2, out SkinMilestone m2), Is.True);
            Assert.That(fx.Offers.SkinPurchasable(m2, 10, 2), Is.False, "venta apagada");
            fx.Offers.SkinSalesEnabled = true;
            Assert.That(fx.Offers.SkinPurchasable(m2, 9, 1), Is.False, "antes de entrar al mundo 2");
            Assert.That(fx.Offers.SkinPurchasable(m2, 10, 2), Is.True);
            fx.Store.Profile.Entitlements.Skins.Add("af_skin_m02");
            Assert.That(fx.Offers.SkinPurchasable(m2, 10, 2), Is.False);
        }

        [Test]
        public void Canoes1h_InTheMarketFrom31()
        {
            var fx = new Fx();
            Assert.That(fx.Offers.CanoesInMarket(29), Is.False);
            Assert.That(fx.Offers.CanoesInMarket(30), Is.True, "con el 31 en juego (rescue_min_level)");
        }

        [Test]
        public void NoCards_WithTheStoreOff_OrTheProfileLocked()
        {
            var off = new Fx();
            off.Store.Ledger.Config.BizIapEnabled = false;
            Assert.That(off.Win(12), Is.EqualTo(OfferKind.None));

            var locked = new Fx();
            locked.Offers.PurchasesBlocked = true;
            Assert.That(locked.Win(12), Is.EqualTo(OfferKind.None));
        }

        [Test]
        public void PickAtVictory_ChangesNothing()
        {
            var fx = new Fx();
            var ctx = new VictoryOfferContext(12, 2, 2);
            Assert.That(fx.Offers.PickAtVictory(in ctx), Is.EqualTo(OfferKind.Starter));
            Assert.That(fx.Offers.PickAtVictory(in ctx), Is.EqualTo(OfferKind.Starter));
            Assert.That(fx.Budget.Spent, Is.EqualTo(0));
            Assert.That(fx.Records.Saves, Is.EqualTo(0));
        }

        [Test]
        public void AFailedSave_KeepsTheChangeInMemory_AndRetriesWithTheNext()
        {
            var fx = new Fx();
            fx.Records.FailSaves = true;
            fx.Piggy.OnCampaignVictory(20, RewardTier.Normal);
            Assert.That(fx.State.PendingSave, Is.True);
            Assert.That(fx.Piggy.Seeds, Is.EqualTo(15));
            fx.Records.FailSaves = false;
            fx.Piggy.OnCampaignVictory(21, RewardTier.Normal);
            Assert.That(fx.State.PendingSave, Is.False);
            Assert.That(fx.Records.Load().PiggySeeds, Is.EqualTo(30));
        }

        [Test]
        public void TakeIf_OnlyTakesTheExpectedOffer_AndPlacementSplitsVictoryAndLobby()
        {
            var fx = new Fx();
            var ctx = new VictoryOfferContext(12, 2, 2);
            Assert.That(fx.Offers.TakeIf(in ctx, OfferKind.NoAds), Is.False, "toca la Mochila, no Sin anuncios");
            Assert.That(fx.Budget.Spent, Is.EqualTo(0));
            Assert.That(fx.Offers.TakeIf(in ctx, OfferKind.Starter), Is.True);
            Assert.That(fx.Budget.Spent, Is.EqualTo(1));
            Assert.That(OfferScheduler.ShowsOnVictory(OfferKind.Starter), Is.True, "en la pantalla de victoria (contrato 11)");
            Assert.That(OfferScheduler.ShowsOnVictory(OfferKind.PiggyFull), Is.True, "HUD de victoria");
            Assert.That(OfferScheduler.ShowsOnVictory(OfferKind.NoAds), Is.False, "tras la pantalla de victoria: en el inicio [P]");
            Assert.That(OfferScheduler.ShowsOnVictory(OfferKind.SkinWorld), Is.False, "fuera del nivel");
        }

        [Test]
        public void DayIndex_WithoutCreatedAt_IsDay1_AndDoesNotThrow()
        {
            var store = new StoreFixture();
            var state = new OffersState(new InMemoryOffersStore());
            var offers = new OfferScheduler(state, store.Ledger, new PiggyJar(state), store.Clock, default, new SessionTakeoverBudget(() => 2));
            Assert.That(offers.DayIndex, Is.EqualTo(1), "UTC−5: DateTime.MinValue − 5 h no se calcula");
            var ctx = new VictoryOfferContext(31, 2, 2);
            Assert.That(offers.PickAtVictory(in ctx), Is.EqualTo(OfferKind.None));
        }

        [Test]
        public void Contexts_AndIds_Of08_8_2()
        {
            Assert.That(OfferScheduler.ContextOf(OfferKind.Starter), Is.EqualTo(PurchaseContext.StarterWindow));
            Assert.That(OfferScheduler.ContextOf(OfferKind.NoAds), Is.EqualTo(PurchaseContext.NoAdsPrompt));
            Assert.That(OfferScheduler.ContextOf(OfferKind.PiggyFull), Is.EqualTo(PurchaseContext.Piggy));
            Assert.That(OfferScheduler.IdOf(OfferKind.SkinWorld), Is.EqualTo("skin_world"));
            Assert.That(OfferScheduler.IdOf(OfferKind.None), Is.EqualTo("none"));
        }
    }
}
