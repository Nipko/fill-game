#nullable enable
using System;
using AF.Ads;
using AF.Economy;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Compras simuladas (B1-4; 05 §4.1; 04 §6.2; D-008): cada SKU acredita su contenido exacto con <c>Wallet.Apply</c>,
    /// una compra cancelada o fallida nunca cambia nada, el derecho Sin anuncios persiste, toda compra corta los
    /// intersticiales 24 h y la Balsa de rescate sale 1 vez al día y se oculta 48 h tras 2 rechazos.
    /// </summary>
    public sealed class FakeStoreTests
    {
        [TestCase("af_coins_2", 1000)]
        [TestCase("af_coins_3", 2800)]
        [TestCase("af_coins_4", 6000)]
        [TestCase("af_coins_5", 13000)]
        [TestCase("af_coins_6", 36000)]
        public void SeedPacks_CreditTheirSeeds(string sku, int seeds)
        {
            var fx = new StoreFixture();

            PurchaseResult result = fx.Buy(sku);

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Purchased));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(seeds));
            Assert.That(result.Transaction!.SeedsDelta, Is.EqualTo(seeds));
            Assert.That(result.Transaction!.Transaction.Source.ToId(), Is.EqualTo("iap"));
            Assert.That(fx.Saved().Wallet.Seeds, Is.EqualTo(seeds), "guardado");
        }

        [Test]
        public void Starter_Gives500Seeds_2ExtraCuias_3Hints_OncePerAccount()
        {
            var fx = new StoreFixture();

            Assert.That(fx.Buy("af_starter").IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(2));
            Assert.That(fx.Stock(BoosterKind.Hint), Is.EqualTo(3));
            Assert.That(fx.Saved().Entitlements.StarterBought, Is.True);

            Assert.That(fx.Buy("af_starter").Status, Is.EqualTo(PurchaseStatus.AlreadyOwned));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(1), "no se enseña la hoja de algo ya comprado");
        }

        [Test]
        public void Balsa_AppliesTheRescue_1000Seeds_1ExtraCuia_And30MinOfInfiniteCanoes()
        {
            var fx = new StoreFixture();

            PurchaseResult result = fx.Buy("af_rescue", level: 31, context: PurchaseContext.Stuck);

            Assert.That(result.IsPurchased, Is.True);
            Assert.That(result.AppliesRescue, Is.True, "quien llama añade la Cuia de rescate al tablero");
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(fx.Stock(BoosterKind.ExtraCuia), Is.EqualTo(1));
            Assert.That(fx.Wallet.Canoes.InfiniteRemaining, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(fx.Wallet.RescuesToday, Is.EqualTo(1), "cuenta como el rescate del día");
            Assert.That(fx.Profile.Stats.Rescues, Is.EqualTo(1));
            Assert.That(fx.Wallet.QuoteRescue(31)!.Value.Price, Is.EqualTo(600), "el siguiente ya no lleva el descuento del primero");
        }

        [Test]
        public void Balsa_IsNotSoldBeforeLevel31()
        {
            var fx = new StoreFixture();
            string before = fx.Json;

            Assert.That(fx.Buy("af_rescue", level: 30).Status, Is.EqualTo(PurchaseStatus.NotAvailable));
            Assert.That(fx.Buy("af_rescue").Status, Is.EqualTo(PurchaseStatus.NotAvailable), "fuera de un nivel");
            Assert.That(fx.Json, Is.EqualTo(before));
            Assert.That(fx.Ledger.CanOfferBalsa(30), Is.False);
        }

        [Test]
        public void NoAds_GivesTheRight_And500Seeds_AndPersists()
        {
            var fx = new StoreFixture();

            Assert.That(fx.Buy("af_noads").IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(500));
            Assert.That(fx.Buy("af_noads").Status, Is.EqualTo(PurchaseStatus.AlreadyOwned));

            Profile reloaded = fx.Store.Load().Profile!;
            Assert.That(reloaded.Entitlements.NoAds, Is.True, "el derecho sobrevive a cerrar la app");
            AdController ads = AdController.Create(new FakeAdService(new ImmediateAdPresenter()), reloaded, fx.Store, fx.Clock, false);
            fx.Clock.Advance(TimeSpan.FromDays(2));
            Assert.That(ads.CheckInterstitial(40, AdContext.VictoryScreen).Reason, Is.EqualTo(AdBlockReason.NoAds));
            Assert.That(ads.CheckRewarded(AdPlacement.ExtraCanoe).IsAllowed, Is.True, "conserva los recompensados");
        }

        [Test]
        public void Piggy_CreditsExactlyTheJarSeeds()
        {
            var fx = new StoreFixture();

            Assert.That(fx.Buy("af_piggy", piggySeeds: 999).Status, Is.EqualTo(PurchaseStatus.NotAvailable), "comprable desde 1.000");
            Assert.That(fx.Buy("af_piggy", piggySeeds: 3001).Status, Is.EqualTo(PurchaseStatus.NotAvailable), "tope 3.000");
            Assert.That(fx.Buy("af_piggy", piggySeeds: 1234).IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1234));
        }

        [Test]
        public void InfiniteCanoes1h_Gives60Minutes_AddedToWhatIsLeft()
        {
            var fx = new StoreFixture();

            fx.Buy("af_infinite_canoes_1h");
            Assert.That(fx.Wallet.Canoes.InfiniteRemaining, Is.EqualTo(TimeSpan.FromMinutes(60)));
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(0));
            fx.Clock.Advance(TimeSpan.FromMinutes(10));
            fx.Buy("af_infinite_canoes_1h");
            Assert.That(fx.Wallet.Canoes.InfiniteRemaining, Is.EqualTo(TimeSpan.FromMinutes(110)));
        }

        [TestCase("af_skin_m02")]
        [TestCase("af_skin_m03")]
        [TestCase("af_skin_m04")]
        public void SkinPacks_GiveTheSkin_And300Seeds_OncePerSkin(string sku)
        {
            var fx = new StoreFixture();

            Assert.That(fx.Buy(sku).IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(300));
            Assert.That(fx.Saved().Entitlements.Skins, Is.EqualTo(new[] { sku }));
            Assert.That(fx.Buy(sku).Status, Is.EqualTo(PurchaseStatus.AlreadyOwned));
            Assert.That(fx.Buy(sku == "af_skin_m02" ? "af_skin_m03" : "af_skin_m02").IsPurchased, Is.True, "otra skin sí");
        }

        [TestCase("af_coins_2")]
        [TestCase("af_starter")]
        [TestCase("af_noads")]
        [TestCase("af_infinite_canoes_1h")]
        [TestCase("af_skin_m03")]
        public void ACancelledOrFailedPurchase_NeverChangesAnything(string sku)
        {
            var fx = new StoreFixture(seeds: 250);
            string before = fx.Json;

            fx.Presenter.Confirm = false;
            Assert.That(fx.Buy(sku).Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(fx.Json, Is.EqualTo(before));

            fx.Presenter.Confirm = true;
            fx.Service.Mode = FakeStoreMode.Fail;
            Assert.That(fx.Buy(sku).Status, Is.EqualTo(PurchaseStatus.Failed));
            Assert.That(fx.Json, Is.EqualTo(before));
            Assert.That(fx.Store.SaveCount, Is.EqualTo(0));
            Assert.That(fx.Presenter.Presented, Is.EqualTo(2));
        }

        [Test]
        public void ACreditTheWalletRejects_UndoesTheRest()
        {
            var fx = new StoreFixture(seeds: int.MaxValue - 100);
            string before = fx.Json;

            PurchaseResult result = fx.Buy("af_skin_m02");

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Failed));
            Assert.That(result.Transaction!.Reason, Is.EqualTo(RejectReason.Overflow));
            Assert.That(fx.Json, Is.EqualTo(before), "ni skin ni last_iap sin acreditación");
        }

        [Test]
        public void TheSheet_ShowsNameKeyAndLocalPrice()
        {
            var fx = new StoreFixture(currency: "COP");

            fx.Buy("af_coins_2");

            SimulatedPurchase sheet = fx.Presenter.Last!;
            Assert.That(SimulatedPurchase.LabelKey, Is.EqualTo("iap_simulated_purchase"), "el rótulo, por clave (D-018)");
            Assert.That(sheet.NameKey, Is.EqualTo("iap_af_coins_2_name"), "el nombre, por clave: el texto lo pone la UI");
            Assert.That(sheet.Sku, Is.EqualTo("af_coins_2"));
            Assert.That(sheet.PriceText, Is.EqualTo("COP 3.900"));
            Assert.That(fx.Service.PriceOf("af_infinite_canoes_1h").ToString(), Is.EqualTo("COP 3.900"));
            Assert.Throws<ArgumentException>(() => new FakeStoreService(fx.Ledger, fx.Presenter, "EUR"));
        }

        [Test]
        public void StoreOff_AndOnePurchaseAtATime()
        {
            var off = new StoreFixture(config: new StoreConfig { BizIapEnabled = false });
            Assert.That(off.Service.IsAvailable, Is.False);
            Assert.That(off.Buy("af_coins_2").Status, Is.EqualTo(PurchaseStatus.StoreUnavailable));
            Assert.That(off.Presenter.Presented, Is.EqualTo(0));

            var fx = new StoreFixture();
            var held = new HeldPurchasePresenter();
            var service = new FakeStoreService(fx.Ledger, held);
            PurchaseResult? first = null;
            PurchaseResult? second = null;
            service.Purchase(new PurchaseRequest("af_coins_2"), r => first = r);
            service.Purchase(new PurchaseRequest("af_coins_3"), r => second = r);

            Assert.That(first, Is.Null);
            Assert.That(service.IsPurchasing, Is.True);
            Assert.That(second!.Status, Is.EqualTo(PurchaseStatus.Busy));
            held.Close(true);
            held.Close(true);
            Assert.That(first!.IsPurchased, Is.True);
            Assert.That(fx.Wallet.Seeds, Is.EqualTo(1000), "un segundo cierre no acredita dos veces");
            Assert.That(service.IsPurchasing, Is.False);
            Assert.Throws<ArgumentException>(() => service.Purchase(new PurchaseRequest("af_pass"), r => { }));
        }

        // ------------------------------------------------------------------ intersticiales tras una compra

        [Test]
        public void AnyPurchase_SuppressesInterstitialsFor24h_AlsoInfiniteCanoes1h()
        {
            var fx = new StoreFixture();
            AdController ads = AdController.Create(new FakeAdService(new ImmediateAdPresenter()), fx.Profile, fx.Store, fx.Clock, false);
            fx.Clock.Advance(TimeSpan.FromMinutes(5));
            for (int i = 0; i < 40; i++)
            {
                ads.RecordLevelCompleted();
            }

            Assert.That(ads.CheckInterstitial(40, AdContext.VictoryScreen).IsAllowed, Is.True);
            fx.Buy("af_infinite_canoes_1h");
            Assert.That(fx.Saved().Ads.LastIap, Is.EqualTo(fx.Clock.UtcNow));
            Assert.That(ads.CheckInterstitial(40, AdContext.VictoryScreen).Reason, Is.EqualTo(AdBlockReason.AfterPurchase));

            fx.Clock.Advance(TimeSpan.FromMinutes(30));
            Assert.That(fx.Wallet.Canoes.IsInfinite, Is.True);
            Assert.That(ads.CheckInterstitial(40, AdContext.VictoryScreen).Reason, Is.EqualTo(AdBlockReason.AfterPurchase));

            fx.Clock.Advance(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(30));
            Assert.That(ads.CheckInterstitial(40, AdContext.VictoryScreen).IsAllowed, Is.True, "a las 24 h vuelven");
        }

        // ------------------------------------------------------------------ Balsa de rescate

        [Test]
        public void Balsa_IsOfferedOnceADay()
        {
            var fx = new StoreFixture();

            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.True);
            fx.Ledger.RecordBalsaShown();
            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.False, "af_rescue_daily_cap = 1");
            Assert.That(fx.Saved().Daily.AfRescueShown, Is.EqualTo(1));

            fx.Clock.Set(new DateTime(2026, 10, 9, 5, 0, 0, DateTimeKind.Utc));
            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.True, "al día local siguiente");
        }

        [Test]
        public void Balsa_HidesFor48hAfterTwoDeclines()
        {
            var fx = new StoreFixture();
            fx.Presenter.Confirm = false;

            Assert.That(fx.Buy("af_rescue", level: 31).Status, Is.EqualTo(PurchaseStatus.Cancelled));
            Assert.That(fx.Profile.Daily.AfRescueDeclinedDays, Is.EqualTo(1));
            Assert.That(fx.Profile.Wallet.Seeds, Is.EqualTo(0), "cancelar nunca descuenta");

            fx.Clock.Set(new DateTime(2026, 10, 9, 15, 0, 0, DateTimeKind.Utc));
            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.True);
            fx.Buy("af_rescue", level: 31);
            DateTime hiddenAt = fx.Clock.UtcNow;
            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.False);
            Assert.That(fx.Ledger.BalsaHiddenUntil, Is.EqualTo(hiddenAt + TimeSpan.FromHours(48)));
            Assert.That(fx.Saved().Daily.AfRescueHiddenUntil, Is.EqualTo(hiddenAt + TimeSpan.FromHours(48)));

            fx.Clock.Advance(TimeSpan.FromHours(47));
            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.False);
            fx.Clock.Advance(TimeSpan.FromHours(1));
            Assert.That(fx.Ledger.CanOfferBalsa(31), Is.True, "vuelve a las 48 h");
            Assert.That(fx.Profile.Daily.AfRescueDeclinedDays, Is.EqualTo(0), "y hacen falta otros 2 rechazos");
        }

        [Test]
        public void BuyingTheBalsa_ResetsItsDeclines()
        {
            var fx = new StoreFixture();
            fx.Presenter.Confirm = false;
            fx.Buy("af_rescue", level: 40);
            Assert.That(fx.Profile.Daily.AfRescueDeclinedDays, Is.EqualTo(1));

            fx.Presenter.Confirm = true;
            Assert.That(fx.Buy("af_rescue", level: 40).IsPurchased, Is.True);
            Assert.That(fx.Saved().Daily.AfRescueDeclinedDays, Is.EqualTo(0));

            fx.Presenter.Confirm = false;
            fx.Buy("af_coins_2");
            Assert.That(fx.Profile.Daily.AfRescueDeclinedDays, Is.EqualTo(0), "cancelar otro SKU no es rechazar la Balsa");
        }

        /// <summary>Hoja que no responde hasta que el test lo dice.</summary>
        private sealed class HeldPurchasePresenter : ISimulatedPurchasePresenter
        {
            private Action<bool>? _onClosed;

            public void Present(SimulatedPurchase purchase, Action<bool> onClosed)
            {
                _onClosed = onClosed;
            }

            public void Close(bool confirmed)
            {
                _onClosed?.Invoke(confirmed);
            }
        }
    }
}
