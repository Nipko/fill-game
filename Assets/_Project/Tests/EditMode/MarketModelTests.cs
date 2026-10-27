#nullable enable
using System;
using System.Collections.Generic;
using AF.Boosters;
using AF.Economy;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Bloque B5: qué enseña el Mercado (06 §4.7; 05 §4.1 y §5) en cada pestaña y momento, sin pantalla, y el <c>geo_tier</c> de la
    /// región del teléfono (Q-E3 (a) de D-033; 04 §6.2).
    /// </summary>
    public sealed class MarketModelTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        private sealed class Fx
        {
            public Fx()
            {
                Clock = new ManualClock(T0, TimeSpan.FromHours(-5));
                Profile = Wallet.NewProfile(Clock);
                var store = new MemoryProfileStore();
                Wallet = new Wallet(Profile, store, Clock);
                Ledger = new StoreLedger(Profile, Wallet, store, Clock);
                State = new OffersState(new InMemoryOffersStore());
                Piggy = new PiggyJar(State, null, Ledger.Config);
                Offers = new OfferScheduler(State, Ledger, Piggy, Clock, T0, new SessionTakeoverBudget(() => 2));
                Skins = new SkinService(Profile, store);
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public Wallet Wallet { get; }

            public StoreLedger Ledger { get; }

            public OffersState State { get; }

            public PiggyJar Piggy { get; }

            public OfferScheduler Offers { get; }

            public SkinService Skins { get; }

            public List<MarketRow> Rows(MarketTab tab, int maxLevel)
            {
                var rows = new List<MarketRow>();
                MarketModel.Build(tab, Offers, Skins, maxLevel, AF.Levels.WorldTable.WorldOf(maxLevel + 1), rows);
                return rows;
            }
        }

        private static List<string> Ids(List<MarketRow> rows)
        {
            var ids = new List<string>();
            foreach (MarketRow r in rows)
            {
                ids.Add(r.Id);
            }

            return ids;
        }

        [Test]
        public void Seeds_FivePacks_AndTheJarFrom20()
        {
            var fx = new Fx();
            Assert.That(Ids(fx.Rows(MarketTab.Seeds, 10)), Is.EqualTo(MarketModel.SeedPacks));
            List<MarketRow> rows = fx.Rows(MarketTab.Seeds, 19);
            Assert.That(rows[0].Id, Is.EqualTo("af_piggy"), "Cántaro desde eco_piggy_from_level, primero");
            Assert.That(rows[0].Action, Is.EqualTo(MarketAction.BuyLocked), "por debajo de eco_piggy_min_buy");
            Assert.That(rows[0].Number2, Is.EqualTo(3000));
            Assert.That(rows[1].Number, Is.EqualTo(1000), "Puñado: 1.000 semillas");
            Assert.That(rows[5].Number, Is.EqualTo(36000), "Cosecha: 36.000");

            fx.State.Record.PiggySeeds = 1000;
            Assert.That(fx.Rows(MarketTab.Seeds, 19)[0].Action, Is.EqualTo(MarketAction.Buy));
        }

        [Test]
        public void Boosters_StarterInTheWindow_Canoes1hFrom31_HiddenWhenEmpty()
        {
            var fx = new Fx();
            Assert.That(MarketModel.TabVisible(MarketTab.Boosters, fx.Offers, 10), Is.True);
            Assert.That(Ids(fx.Rows(MarketTab.Boosters, 10)), Is.EqualTo(new[] { "af_starter" }));
            Assert.That(MarketModel.TabVisible(MarketTab.Boosters, fx.Offers, 25), Is.False, "entre el 21 y el 30 no hay nada");
            List<MarketRow> late = fx.Rows(MarketTab.Boosters, 30);
            Assert.That(Ids(late), Is.EqualTo(new[] { "af_infinite_canoes_1h" }));
            Assert.That(late[0].Number, Is.EqualTo(60), "60 min");
        }

        [Test]
        public void Boosters_StarterRow_CarriesTheHoursLeft()
        {
            var fx = new Fx();
            Assert.That(fx.Rows(MarketTab.Boosters, 12)[0].Number, Is.EqualTo(0), "sin temporizador hasta la primera tarjeta");
            var ctx = new VictoryOfferContext(12, 2, 2);
            Assert.That(fx.Offers.TakeAtVictory(in ctx), Is.EqualTo(OfferKind.Starter));
            fx.Clock.Advance(TimeSpan.FromMinutes(90));
            Assert.That(fx.Rows(MarketTab.Boosters, 12)[0].Number, Is.EqualTo(47), "48 h − 1,5 h, hacia arriba");
        }

        [Test]
        public void NoAds_From22_OrAsOwned()
        {
            var fx = new Fx();
            Assert.That(MarketModel.TabVisible(MarketTab.NoAds, fx.Offers, 20), Is.False);
            List<MarketRow> rows = fx.Rows(MarketTab.NoAds, 21);
            Assert.That(rows[0].Action, Is.EqualTo(MarketAction.Buy));
            Assert.That(rows[0].Number, Is.EqualTo(500), "eco_noads_bonus_seeds");
            fx.Profile.Entitlements.NoAds = true;
            Assert.That(MarketModel.TabVisible(MarketTab.NoAds, fx.Offers, 5), Is.True, "comprado: la pestaña lo dice");
            Assert.That(fx.Rows(MarketTab.NoAds, 21)[0].Action, Is.EqualTo(MarketAction.None));
        }

        [Test]
        public void Cuias_BaseAndOwnedToEquip_TheRestAsWorldPrize()
        {
            var fx = new Fx();
            List<MarketRow> rows = fx.Rows(MarketTab.Cuias, 15);
            Assert.That(Ids(rows), Is.EqualTo(new[] { "cuia_vidrio", "af_skin_m02", "af_skin_m03", "af_skin_m04" }));
            Assert.That(rows[0].Action, Is.EqualTo(MarketAction.Equipped));
            Assert.That(rows[1].Action, Is.EqualTo(MarketAction.None), "sin venta de skins (Q-B5-1)");
            Assert.That(rows[1].Number, Is.EqualTo(25), "Premio del mundo · faltan 25 (gana el 40)");

            Assert.That(fx.Skins.ApplyLevelPassed(40, true), Is.Not.Null);
            rows = fx.Rows(MarketTab.Cuias, 40);
            Assert.That(rows[0].Action, Is.EqualTo(MarketAction.Equip));
            Assert.That(rows[1].Action, Is.EqualTo(MarketAction.Equipped), "la ganada se pone sola (Q-M8 (a))");

            Assert.That(fx.Skins.Select("cuia_vidrio"), Is.True);
            rows = fx.Rows(MarketTab.Cuias, 40);
            Assert.That(rows[0].Action, Is.EqualTo(MarketAction.Equipped));
            Assert.That(rows[1].Action, Is.EqualTo(MarketAction.Equip));
        }

        [Test]
        public void Cuias_WithSkinSales_SellTheSkinOfAnEnteredWorld()
        {
            var fx = new Fx();
            fx.Offers.SkinSalesEnabled = true;
            List<MarketRow> rows = fx.Rows(MarketTab.Cuias, 15);
            Assert.That(rows[1].Action, Is.EqualTo(MarketAction.Buy), "Cuia de Santarém: el jugador ya está en el mundo 2");
            Assert.That(rows[2].Action, Is.EqualTo(MarketAction.None), "el mundo 3 aún no");
        }

        [Test]
        public void SeedsAndCuias_AreAlwaysVisible()
        {
            var fx = new Fx();
            Assert.That(MarketModel.TabVisible(MarketTab.Seeds, fx.Offers, 10), Is.True);
            Assert.That(MarketModel.TabVisible(MarketTab.Cuias, fx.Offers, 10), Is.True);
            Assert.That(MarketModel.AllTabs, Is.EqualTo(new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds }));
        }

        [TestCase("es-CO", GeoTier.T3)]
        [TestCase("pt-BR", GeoTier.T3)]
        [TestCase("es-MX", GeoTier.T3)]
        [TestCase("en-US", GeoTier.T1)]
        [TestCase("fr-CA", GeoTier.T1)]
        [TestCase("ja-JP", GeoTier.T1)]
        [TestCase("de-DE", GeoTier.T1)]
        [TestCase("es-ES", GeoTier.T1)]
        [TestCase("en_GB", GeoTier.T1)]
        [TestCase("en-US-u-mu-celsius", GeoTier.T1)]
        [TestCase("es-CL", GeoTier.T3)]
        [TestCase("zh-Hant-TW", GeoTier.T3)]
        [TestCase("en-u-ca-gregory", GeoTier.T3)]
        [TestCase("de-u-co-phonebk", GeoTier.T3)]
        [TestCase("es-419", GeoTier.T3)]
        [TestCase("es", GeoTier.T3)]
        [TestCase("", GeoTier.T3)]
        public void GeoTier_FromThePhoneRegion(string tag, GeoTier tier)
        {
            Assert.That(GeoTierRule.FromLanguageTag(tag), Is.EqualTo(tier));
        }

        [TestCase("DE", "en-US", GeoTier.T1)] // D-057: el país de la cuenta de Play manda sobre la región (Q-E3 (a))
        [TestCase("CO", "en-US", GeoTier.T3)]
        [TestCase("jp", "es-CO", GeoTier.T1)]
        [TestCase("", "en-US", GeoTier.T1)] // sin país de Play, la región del teléfono
        [TestCase(null, "es-MX", GeoTier.T3)]
        [TestCase("419", "de-DE", GeoTier.T1)]
        public void GeoTier_FromThePlayAccountCountry_ThenThePhoneRegion(string? play, string tag, GeoTier tier)
        {
            Assert.That(GeoTierRule.FromSources(play, tag), Is.EqualTo(tier));
        }

        [Test]
        public void TabWidths_ShareTheRow_AndNeverLeaveThePanel()
        {
            var widths = new float[4];

            // Caben a partes iguales: un cuarto cada una; una pestaña oculta se queda en 0.
            MarketScreen.ShareTabWidths(296f, new[] { 50f, 60f, -1f, 70f }, widths);
            Assert.That(widths, Is.EqualTo(new[] { 296f / 3f, 296f / 3f, 0f, 296f / 3f }).Within(0.01f));

            // Una palabra no cabe en su parte: se queda su ancho y las demás se reparten el resto.
            MarketScreen.ShareTabWidths(296f, new[] { 40f, 40f, 40f, 130f }, widths);
            Assert.That(widths[3], Is.EqualTo(130f));
            Assert.That(widths[0], Is.EqualTo(166f / 3f).Within(0.01f));
            Assert.That(Sum(widths), Is.EqualTo(296f).Within(0.01f));

            // Salvaguarda (revisión de fx-130): las palabras suman más que el panel; cada una, en proporción, y sin salirse.
            MarketScreen.ShareTabWidths(296f, new[] { 100f, 120f, 90f, 130f }, widths);
            Assert.That(Sum(widths), Is.LessThanOrEqualTo(296f));
            Assert.That(Sum(widths), Is.GreaterThan(295.9f));
            Assert.That(widths[3] / widths[0], Is.EqualTo(1.3f).Within(0.01f), "en proporción a su palabra");
            foreach (float w in widths)
            {
                Assert.That(w, Is.GreaterThan(0f), "ninguna se queda sin sitio");
            }

            MarketScreen.ShareTabWidths(296f, new[] { -1f, -1f, -1f, -1f }, widths);
            Assert.That(Sum(widths), Is.EqualTo(0f));
        }

        private static float Sum(float[] values)
        {
            float sum = 0f;
            foreach (float v in values)
            {
                sum += v;
            }

            return sum;
        }
    }
}
