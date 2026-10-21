#nullable enable
using System;
using AF.Economy;
using AF.Save;
using NUnit.Framework;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Parche B5-profile: <c>piggy</c> (08 §4.3) y <c>offers</c> [P] en el perfil, de ida y vuelta por <see cref="ProfileJson"/>;
    /// ausentes en un perfil que nunca los usó; <c>piggy</c> es dinero (un tipo equivocado es corrupción) y <c>offers</c> no (toma su
    /// default); y el Cántaro sigue tras «reabrir» la app.
    /// </summary>
    public sealed class ProfileOffersStoreTests
    {
        [Test]
        public void ANewProfile_HasNeitherBlock()
        {
            var fx = new StoreFixture();
            string json = fx.Json;
            Assert.That(json, Does.Not.Contain("\"piggy\""));
            Assert.That(json, Does.Not.Contain("\"offers\""));
            OffersRecord r = new ProfileOffersStore(fx.Profile, fx.Store).Load();
            Assert.That(r.PiggySeeds, Is.EqualTo(0));
            Assert.That(r.StarterFirstSeenAt, Is.Null);
        }

        [Test]
        public void BothBlocks_RoundTripThroughTheProfileJson()
        {
            var fx = new StoreFixture();
            var store = new ProfileOffersStore(fx.Profile, fx.Store);
            var record = new OffersRecord
            {
                PiggySeeds = 1230,
                PiggyCap = 3000,
                PiggyLastLevel = 87,
                PiggyFullCardShown = true,
                StarterFirstSeenAt = StoreFixture.T0,
                NoAdsCardDate = "2026-10-08",
                NoAdsDeclines = 2,
                InterstitialsSeen = 3,
            };
            record.StarterReminders.Add(15);
            record.SkinCardWorlds.Add(3);
            Assert.That(store.Save(record), Is.True);

            string json = ProfileJson.Serialize(fx.Saved());
            StringAssert.Contains("\"piggy\": {\n    \"seeds\": 1230,\n    \"cap\": 3000,\n    \"last_level\": 87,\n    \"full_card\": true\n  }", json);
            Profile back = ProfileJson.Deserialize(json);
            OffersRecord r = new ProfileOffersStore(back, new MemoryProfileStore(back)).Load();
            Assert.That(r.PiggySeeds, Is.EqualTo(1230));
            Assert.That(r.PiggyCap, Is.EqualTo(3000));
            Assert.That(r.PiggyLastLevel, Is.EqualTo(87));
            Assert.That(r.PiggyFullCardShown, Is.True);
            Assert.That(r.StarterFirstSeenAt, Is.EqualTo(StoreFixture.T0));
            Assert.That(r.StarterReminders, Is.EqualTo(new[] { 15 }));
            Assert.That(r.NoAdsCardDate, Is.EqualTo("2026-10-08"));
            Assert.That(r.NoAdsDeclines, Is.EqualTo(2));
            Assert.That(r.InterstitialsSeen, Is.EqualTo(3));
            Assert.That(r.SkinCardWorlds, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void Piggy_IsMoney_AWrongTypeIsCorruption_ButOffersFallBack()
        {
            var fx = new StoreFixture();
            new ProfileOffersStore(fx.Profile, fx.Store).Save(new OffersRecord { PiggySeeds = 100, PiggyLastLevel = 87, PiggyFullCardShown = true, NoAdsDeclines = 1 });
            string json = ProfileJson.Serialize(fx.Profile);

            Assert.Throws<FormatException>(() => ProfileJson.Deserialize(json.Replace("\"seeds\": 100", "\"seeds\": \"100\"")));
            Profile tolerant = ProfileJson.Deserialize(json.Replace("\"noads_declines\": 1", "\"noads_declines\": \"x\"")
                .Replace("\"starter_reminders\": []", "\"starter_reminders\": [15, \"y\", 15]"));
            Assert.That(tolerant.Offers!.NoAdsDeclines, Is.EqualTo(0));
            Assert.That(tolerant.Offers.StarterReminders, Is.EqualTo(new[] { 15 }));
            Assert.That(tolerant.Piggy!.Seeds, Is.EqualTo(100));

            Assert.That(json, Does.Contain("\"full_card\": true").And.Contain("\"last_level\": 87"));
            Profile notMoney = ProfileJson.Deserialize(json.Replace("\"full_card\": true", "\"full_card\": 1")
                .Replace("\"last_level\": 87", "\"last_level\": \"20\""));
            Assert.That(notMoney.Piggy!.FullCard, Is.False, "last_level y full_card no son dinero: su default, sin corrupción");
            Assert.That(notMoney.Piggy.LastLevel, Is.EqualTo(0));
            Assert.That(notMoney.Piggy.Seeds, Is.EqualTo(100));
        }

        [Test]
        public void TheJar_SurvivesReopeningTheApp()
        {
            var fx = new StoreFixture();
            var state = new OffersState(new ProfileOffersStore(fx.Profile, fx.Store));
            var piggy = new PiggyJar(state);
            piggy.OnCampaignVictory(20, RewardTier.Hard);
            piggy.OnCampaignVictory(21, RewardTier.Normal);

            Profile reopened = fx.Saved();
            var again = new PiggyJar(new OffersState(new ProfileOffersStore(reopened, new MemoryProfileStore(reopened))));
            Assert.That(again.Seeds, Is.EqualTo(45));
            Assert.That(again.OnCampaignVictory(21, RewardTier.Normal), Is.Null, "la misma victoria no suma otra vez");
        }

        [Test]
        public void AFailedWrite_KeepsTheChangeInTheProfileInMemory()
        {
            var fx = new StoreFixture();
            var store = new ProfileOffersStore(fx.Profile, new FullDisk());
            Assert.That(store.Save(new OffersRecord { PiggySeeds = 30 }), Is.False);
            Assert.That(fx.Profile.Piggy!.Seeds, Is.EqualTo(30));
        }

        private sealed class FullDisk : IProfileStore
        {
            public ProfileLoadResult Load() => throw new NotSupportedException();

            public void Save(Profile profile) => throw new System.IO.IOException("disco lleno");

            public void Delete()
            {
            }
        }
    }
}
