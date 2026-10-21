#nullable enable
using System.IO;
using AF.Economy;
using AF.Levels;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Skins por hito (B14; 06 §4.5: Cuia de Santarém al 40, Cuia del Encuentro al 80 y Totuma al 140; D-033, Q-M8 (a): la ganada
    /// se pone sola y se cambia en el Mercado): las reglas, ganar y ponerse en la victoria, el Reto sin skins, lo ya comprado, la
    /// recuperación de los mundos ya cerrados, la elección del Mercado y un fallo de escritura.
    /// </summary>
    public sealed class SkinRewardsTests
    {
        [Test]
        public void Milestones_AreTheLastLevelOfWorlds2To4()
        {
            Assert.That(SkinRewards.Milestones.Count, Is.EqualTo(3));
            for (int i = 0; i < SkinRewards.Milestones.Count; i++)
            {
                SkinMilestone m = SkinRewards.Milestones[i];
                Assert.That(m.Level, Is.EqualTo(WorldTable.LastLevelOf(m.World)), m.SkinId + ": el último nivel de su mundo");
                Assert.That(m.SkinId, Is.EqualTo("af_skin_m0" + m.World), "el SKU de su pack (05 §4.1)");
            }

            Assert.That(SkinRewards.SkinEarnedAt(40), Is.EqualTo("af_skin_m02"));
            Assert.That(SkinRewards.SkinEarnedAt(80), Is.EqualTo("af_skin_m03"));
            Assert.That(SkinRewards.SkinEarnedAt(140), Is.EqualTo("af_skin_m04"));
            Assert.That(SkinRewards.SkinEarnedAt(10), Is.Null, "el Estuario no tiene skin: la base es la Cuia de vidrio");
            Assert.That(SkinRewards.SkinEarnedAt(200), Is.Null, "Huingo es de la 1.1");
            Assert.That(SkinRewards.IsKnown(SkinRewards.BaseSkinId), Is.True);
            Assert.That(SkinRewards.IsKnown("af_skin_m05"), Is.False);
            Assert.That(SkinRewards.TryForWorld(3, out SkinMilestone w3), Is.True);
            Assert.That(w3.Level, Is.EqualTo(80));
            Assert.That(SkinRewards.TryForWorld(1, out _), Is.False);
        }

        [Test]
        public void WinningTheLastLevelOfAWorld_GivesItsSkin_AndPutsItOn()
        {
            var store = new MemoryStore();
            Profile p = NewProfile();
            var skins = new SkinService(p, store);

            Assert.That(skins.Equipped, Is.EqualTo(SkinRewards.BaseSkinId));
            Assert.That(skins.ApplyLevelPassed(39, true), Is.Null);
            SkinUnlock? unlock = skins.ApplyLevelPassed(40, true);

            Assert.That(unlock, Is.Not.Null);
            Assert.That(unlock!.SkinId, Is.EqualTo("af_skin_m02"));
            Assert.That(unlock.Source, Is.EqualTo("world_milestone"));
            Assert.That(unlock.Equipped, Is.True);
            Assert.That(p.Entitlements.Skins, Is.EqualTo(new[] { "af_skin_m02" }));
            Assert.That(skins.Equipped, Is.EqualTo("af_skin_m02"));
            Assert.That(store.Writes, Is.EqualTo(1));
            Assert.That(ProfileJson.Deserialize(store.Json!).Entitlements.SkinEquipped, Is.EqualTo("af_skin_m02"), "el derecho y la skin puesta, guardados");
            Assert.That(skins.ApplyLevelPassed(40, true), Is.Null, "rejugar el 40 no la da otra vez");
            Assert.That(store.Writes, Is.EqualTo(1));
        }

        [Test]
        public void Daily_OrASkinAlreadyBought_GiveNothing()
        {
            Profile p = NewProfile();
            p.Entitlements.Skins.Add("af_skin_m03");
            var skins = new SkinService(p, new MemoryStore());

            Assert.That(skins.ApplyLevelPassed(40, false), Is.Null, "el Reto del río no da skins");
            Assert.That(skins.ApplyLevelPassed(80, true), Is.Null, "comprada antes: nada cambia");
            Assert.That(p.Entitlements.SkinEquipped, Is.Null);
        }

        [Test]
        public void Backfill_GivesTheClosedWorlds_WithoutPuttingThemOn()
        {
            Profile p = NewProfile();
            var store = new MemoryStore();
            var skins = new SkinService(p, store);

            Assert.That(skins.Backfill(79), Is.EqualTo(1));
            Assert.That(p.Entitlements.Skins, Is.EqualTo(new[] { "af_skin_m02" }));
            Assert.That(skins.Equipped, Is.EqualTo(SkinRewards.BaseSkinId));
            Assert.That(skins.Backfill(150), Is.EqualTo(2));
            Assert.That(skins.Backfill(150), Is.EqualTo(0));
            Assert.That(store.Writes, Is.EqualTo(2));
        }

        [Test]
        public void Select_OnlyPutsOnWhatThePlayerOwns()
        {
            Profile p = NewProfile();
            var store = new MemoryStore();
            var skins = new SkinService(p, store);
            skins.ApplyLevelPassed(40, true);

            Assert.That(skins.Select("af_skin_m03"), Is.False, "no la tiene");
            Assert.That(skins.Select("inventada"), Is.False);
            Assert.That(skins.Select(SkinRewards.BaseSkinId), Is.True);
            Assert.That(p.Entitlements.SkinEquipped, Is.Null, "la base no se guarda");
            Assert.That(skins.Equipped, Is.EqualTo(SkinRewards.BaseSkinId));
            Assert.That(skins.Select("af_skin_m02"), Is.True);
            Assert.That(skins.Equipped, Is.EqualTo("af_skin_m02"));
            int writes = store.Writes;
            Assert.That(skins.Select("af_skin_m02"), Is.True);
            Assert.That(store.Writes, Is.EqualTo(writes), "sin cambio no se guarda");
        }

        [Test]
        public void Equipped_FallsBackToTheBase_WhenTheSavedSkinIsNotOwned()
        {
            Profile p = NewProfile();
            p.Entitlements.SkinEquipped = "af_skin_m04";

            Assert.That(new SkinService(p, new MemoryStore()).Equipped, Is.EqualTo(SkinRewards.BaseSkinId));
        }

        [Test]
        public void AFailedWrite_KeepsTheChangeInMemory()
        {
            Profile p = NewProfile();
            var store = new MemoryStore { Fail = true };
            var skins = new SkinService(p, store);

            Assert.That(skins.ApplyLevelPassed(140, true), Is.Not.Null);
            Assert.That(skins.PendingSave, Is.True);
            Assert.That(skins.Equipped, Is.EqualTo("af_skin_m04"));
            store.Fail = false;
            skins.Select("af_skin_m04");
            Assert.That(skins.PendingSave, Is.True, "sin cambio no hay escritura");
            skins.Select(SkinRewards.BaseSkinId);
            Assert.That(skins.PendingSave, Is.False);
        }

        private static Profile NewProfile()
        {
            return Profile.CreateNew(new ManualClock(new System.DateTime(2026, 10, 20, 12, 0, 0, System.DateTimeKind.Utc)), 5);
        }

        private sealed class MemoryStore : IProfileStore
        {
            public bool Fail { get; set; }

            public string? Json { get; private set; }

            public int Writes { get; private set; }

            public ProfileLoadResult Load()
            {
                return new ProfileLoadResult(ProfileLoadStatus.NotFound, null);
            }

            public void Save(Profile profile)
            {
                if (Fail)
                {
                    throw new IOException("disco lleno (simulado)");
                }

                Json = ProfileJson.Serialize(profile);
                Writes++;
            }

            public void Delete()
            {
                Json = null;
            }
        }
    }
}
