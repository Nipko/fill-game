#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// F3-3: <c>applied_purchases</c> (08 §4.3 y §11; los <c>token_hash</c> ya acreditados) y <c>purchase_queue</c> [P] (la cola de
    /// verificación de 08 §10.3) en el perfil: faltan mientras están vacías, hacen la ida y vuelta, <c>applied_purchases</c> es dinero
    /// (un tipo equivocado es corrupción) y la cola se descarta si no es un objeto. En la fusión de 08 §11 queda la lista del ganador (D-033, Q-B8-1).
    /// </summary>
    public sealed class AppliedPurchasesProfileTests
    {
        private const string HashA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string HashB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        private static Profile NewProfile() => Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

        [Test]
        public void EmptyByDefault_AndTheKeysAreMissing()
        {
            Profile p = NewProfile();
            string json = ProfileJson.Serialize(p);

            Assert.That(p.AppliedPurchases, Is.Empty);
            Assert.That(p.PurchaseQueue, Is.Null);
            Assert.That(json, Does.Not.Contain("applied_purchases"));
            Assert.That(json, Does.Not.Contain("purchase_queue"));
        }

        [Test]
        public void RoundTrip()
        {
            Profile p = NewProfile();
            p.AppliedPurchases.Add(HashA);
            p.AppliedPurchases.Add(HashB);
            p.PurchaseQueue = "{\"v\":1,\"entries\":[{\"token\":\"t.1\",\"sku\":\"af_coins_2\",\"first_ms\":5,\"attempts\":0,\"next_ms\":5}]}";

            string json = ProfileJson.Serialize(p);
            Profile back = ProfileJson.Deserialize(json);

            Assert.That(back.AppliedPurchases, Is.EqualTo(new[] { HashA, HashB }));
            Assert.That(back.HasAppliedPurchase(HashB), Is.True);
            Assert.That(back.HasAppliedPurchase(HashA.Replace('a', 'c')), Is.False);
            Assert.That(back.PurchaseQueue, Does.Contain("\"token\": \"t.1\""));
            Assert.That(back.PurchaseQueue, Does.Contain("\"next_ms\": 5"));
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(json));
            StringAssert.Contains("\"applied_purchases\": [", json);
            Assert.That(json.IndexOf("\"level_state\"", StringComparison.Ordinal),
                Is.LessThan(json.IndexOf("\"applied_purchases\"", StringComparison.Ordinal)), "tras level_state, como en 08 §4.3");
        }

        [Test]
        public void RepeatedHashes_AreReadOnce()
        {
            Profile p = NewProfile();
            p.AppliedPurchases.Add(HashA);
            p.AppliedPurchases.Add(HashB);
            p.AppliedPurchases.Add(HashA);

            Profile back = ProfileJson.Deserialize(ProfileJson.Serialize(p));

            Assert.That(back.AppliedPurchases, Is.EqualTo(new[] { HashA, HashB }));
        }

        [Test]
        public void AppliedPurchasesWithTheWrongType_IsCorruption()
        {
            string json = ProfileJson.Serialize(NewProfile());
            string bad = json.Replace("\"fauna\":", "\"applied_purchases\": [1, 2],\n  \"fauna\":");
            Assert.That(() => ProfileJson.Deserialize(bad), Throws.InstanceOf<FormatException>());
            string notAList = json.Replace("\"fauna\":", "\"applied_purchases\": \"x\",\n  \"fauna\":");
            Assert.That(() => ProfileJson.Deserialize(notAList), Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void AQueueThatIsNotAnObject_IsDropped()
        {
            string json = ProfileJson.Serialize(NewProfile());
            Profile p = ProfileJson.Deserialize(json.Replace("\"fauna\":", "\"purchase_queue\": [1],\n  \"fauna\":"));
            Assert.That(p.PurchaseQueue, Is.Null, "las compras sin terminar vuelven de la tienda; nada se acredita sin el servidor");

            Profile q = NewProfile();
            q.PurchaseQueue = "no es json";
            Assert.That(ProfileJson.Deserialize(ProfileJson.Serialize(q)).PurchaseQueue, Is.Null);
        }

        [Test]
        public void HasAppliedPurchase_RejectsNull()
        {
            Profile p = NewProfile();
            p.AppliedPurchases.Add(HashA);

            Assert.That(p.HasAppliedPurchase(HashA), Is.True);
            Assert.That(p.HasAppliedPurchase(HashB), Is.False);
            Assert.That(() => p.HasAppliedPurchase(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Clone_CopiesBoth()
        {
            Profile p = NewProfile();
            p.AppliedPurchases.Add(HashA);
            p.PurchaseQueue = "{\"v\":1,\"entries\":[]}";

            Profile copy = p.Clone();
            p.AppliedPurchases.Clear();

            Assert.That(copy.AppliedPurchases, Is.EqualTo(new[] { HashA }));
            Assert.That(copy.PurchaseQueue, Does.Contain("\"entries\": []"));
        }
    }
}
