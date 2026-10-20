#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Enganche E4: el bloque <c>daily_challenge</c> del perfil (08 §4.3 con R-Q4, R-Q5 y R-Q16; nota del Reto §10) y
    /// <see cref="ProfileDailyChallengeStore"/>. Ida y vuelta del JSON del perfil con el bloque completo, lectura de un perfil sin
    /// el bloque, tipos equivocados como corrupción, el registro y la cartera en el mismo perfil (un cobro y su pago en la misma
    /// escritura; reabrir no vuelve a cobrar) y el patrón Save + PendingSave + Flush.
    /// </summary>
    public sealed class ProfileDailyChallengeStoreTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        private static readonly string BlockJson = string.Join("\n", new[]
        {
            "  \"daily_challenge\": {",
            "    \"last_date\": \"2026-10-19\",",
            "    \"result\": \"win\",",
            "    \"streak_days\": 6,",
            "    \"last_claimed_date\": \"2026-10-19\",",
            "    \"pending\": [",
            "      {",
            "        \"date\": \"2026-10-17\",",
            "        \"streak\": 4",
            "      }",
            "    ],",
            "    \"offline_claims\": [",
            "      {",
            "        \"date\": \"2026-10-18\",",
            "        \"streak\": 5",
            "      }",
            "    ],",
            "    \"stamps\": {",
            "      \"month\": \"2026-10\",",
            "      \"days\": [1, 2, 5, 19]",
            "    },",
            "    \"in_progress\": {",
            "      \"date\": \"2026-10-19\",",
            "      \"band\": 2,",
            "      \"id\": \"d2_n0291\",",
            "      \"level_state\": {",
            "        \"level\": 41,",
            "        \"seed\": \"amazonfill:daily:v1:b2:n0291\",",
            "        \"variant\": \"base\",",
            "        \"fails_level\": 1,",
            "        \"pours\": 7,",
            "        \"extra_cuias\": 0,",
            "        \"rescues_level\": 0,",
            "        \"undo_free\": 2,",
            "        \"revealed_cuias\": [3],",
            "        \"history\": \"0>1,2>3\",",
            "        \"stuck\": false",
            "      }",
            "    }",
            "  }",
        });

        [Test]
        public void NewProfile_HasNoBlock_AndAnEmptyRecord()
        {
            Profile profile = NewProfile();

            Assert.That(profile.DailyChallenge, Is.Null);
            Assert.That(ProfileJson.Serialize(profile).Contains("daily_challenge"), Is.False, "la clave falta hasta el primer guardado");

            var store = new ProfileDailyChallengeStore(profile, new MemoryProfileStore());
            DailyChallengeRecord record = store.Load();
            Assert.That(record, Is.Not.Null);
            Assert.That(record.LastClaimedDate, Is.Null);
            Assert.That(record.StreakDays, Is.EqualTo(0));
            Assert.That(record.Result, Is.EqualTo(DailyResult.None));
            Assert.That(record.InProgress, Is.Null);
            Assert.That(store.Load(), Is.SameAs(record), "el registro vivo");
        }

        [Test]
        public void FullBlock_IsWrittenWithTheNoteKeys_AndRoundTripsExactly()
        {
            Profile profile = NewProfile();
            var store = new ProfileDailyChallengeStore(profile, new MemoryProfileStore());
            store.Save(FullRecord());

            string json = ProfileJson.Serialize(profile);
            StringAssert.Contains(BlockJson + "\n}\n", json, "al final del perfil, con las claves de la nota §10");

            Profile copy = ProfileJson.Deserialize(json);
            Assert.That(ProfileJson.Serialize(copy), Is.EqualTo(json));
            AssertFull(ProfileDailyChallengeStore.FromData(copy.DailyChallenge));
            Assert.That(profile.Clone().DailyChallenge, Is.Not.Null.And.Not.SameAs(profile.DailyChallenge));
        }

        [Test]
        public void MissingKeys_TakeDefaults()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"daily_challenge\": {\"last_claimed_date\": \"2026-10-19\"}}");

            DailyChallengeRecord r = ProfileDailyChallengeStore.FromData(p.DailyChallenge);
            Assert.That(r.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(r.LastDate, Is.Null);
            Assert.That(r.Result, Is.EqualTo(DailyResult.None));
            Assert.That(r.StreakDays, Is.EqualTo(0));
            Assert.That(r.PendingClaims, Is.Empty);
            Assert.That(r.OfflineClaims, Is.Empty);
            Assert.That(r.StampsMonth, Is.Null);
            Assert.That(r.StampDays, Is.Empty);
            Assert.That(r.InProgress, Is.Null);

            Profile none = ProfileJson.Deserialize("{\"profile_schema\": 2, \"daily_challenge\": null}");
            Assert.That(none.DailyChallenge, Is.Null);
            Assert.That(ProfileDailyChallengeStore.ParseResult("perdido"), Is.EqualTo(DailyResult.None), "un result desconocido no da premio");
        }

        [Test]
        public void SchemaOneProfile_KeepsTheBlock_AfterTheMigrationToSchemaTwo()
        {
            // El bloque entró sin subir el esquema; la migración 1 → 2 de la ola 7 (fauna, DC15) no debe perderlo.
            string dir = Path.Combine(Path.GetTempPath(), "af-daily-migration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, LocalProfileStore.FileName),
                    "{\"profile_schema\": 1, \"daily_challenge\": {\"last_claimed_date\": \"2026-10-19\", \"streak_days\": 3}}");

                ProfileLoadResult loaded = new LocalProfileStore(dir).Load();

                Assert.That(loaded.Migrated, Is.True);
                Assert.That(loaded.Profile, Is.Not.Null);
                Assert.That(loaded.Profile!.DailyChallenge, Is.Not.Null);
                Assert.That(loaded.Profile.DailyChallenge!.LastClaimedDate, Is.EqualTo("2026-10-19"));
                Assert.That(loaded.Profile.DailyChallenge.StreakDays, Is.EqualTo(3));
                Assert.That(loaded.Profile.Fauna.Sighted, Is.Empty);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": []}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"streak_days\": \"6\"}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"last_claimed_date\": 20261019}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"pending\": [\"2026-10-17\"]}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"pending\": {\"date\": \"2026-10-17\"}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"offline_claims\": [{\"streak\": 1}]}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"stamps\": {\"days\": [1, \"2\"]}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"in_progress\": {\"date\": null}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"in_progress\": {\"id\": \"d1_n0001\"}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"in_progress\": {\"date\": \"2026-10-19\"}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"in_progress\": {\"date\": \"2026-10-19\", \"id\": null}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"in_progress\": {\"date\": \"2026-10-19\", \"id\": \"d2_n0001\", \"band\": \"2\"}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily_challenge\": {\"in_progress\": {\"date\": \"2026-10-19\", \"id\": \"d1\", \"level_state\": {\"seed\": null}}}}")]
        public void WrongTypes_AreFormatErrors(string json)
        {
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize(json));
        }

        [Test]
        public void Save_WritesTheProfile_AndAReopenedStoreReadsTheSameRecord()
        {
            Profile profile = NewProfile();
            var disk = new MemoryProfileStore();
            var store = new ProfileDailyChallengeStore(profile, disk);
            DailyChallengeRecord r = FullRecord();
            store.Save(r);

            Assert.That(disk.SaveCount, Is.EqualTo(1));
            Assert.That(store.PendingSave, Is.False);
            Assert.That(store.Load(), Is.SameAs(r));
            r.StampDays.Add(20); // sin Save no llega al perfil
            Assert.That(profile.DailyChallenge!.StampDays, Is.EqualTo(new[] { 1, 2, 5, 19 }), "el perfil guarda una copia");

            Profile reopened = disk.Load().Profile!;
            AssertFull(new ProfileDailyChallengeStore(reopened, disk).Load());
        }

        [Test]
        public void FailedWrite_StaysPendingInTheProfile_UntilFlush()
        {
            Profile profile = NewProfile();
            var disk = new FailingProfileStore { Fail = true };
            var store = new ProfileDailyChallengeStore(profile, disk);
            DailyChallengeRecord r = store.Load();
            r.LastClaimedDate = "2026-10-19";

            Assert.DoesNotThrow(() => store.Save(r));
            Assert.That(store.PendingSave, Is.True);
            Assert.That(profile.DailyChallenge!.LastClaimedDate, Is.EqualTo("2026-10-19"), "el cambio sigue en el perfil en memoria");
            Assert.That(store.Flush(), Is.False);

            disk.Fail = false;
            Assert.That(store.Flush(), Is.True);
            Assert.That(store.PendingSave, Is.False);
            Assert.That(ProfileJson.Deserialize(disk.Json!).DailyChallenge!.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(store.Flush(), Is.True, "nada pendiente");
        }

        [Test]
        public void ClaimAndPayment_LiveInTheSameProfile_AndAReopenNeverPaysTwice()
        {
            var clock = new ManualClock(Utc(new DateTime(2026, 10, 19, 10, 0, 0)), Bogota);
            var disk = new MemoryProfileStore();
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, disk, clock);
            var store = new ProfileDailyChallengeStore(profile, disk);
            var rules = new DailyChallengeRules(store, wallet, clock, FakeServerDate.Offline(), DailyChallengeConfig.Defaults);
            int seeds = wallet.Seeds;

            Assert.That(rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(wallet.Seeds, Is.EqualTo(seeds + 60));

            // Lo que está en disco lleva a la vez el registro y el pago.
            Profile reopened = disk.Load().Profile!;
            Assert.That(reopened.Wallet.Seeds, Is.EqualTo(seeds + 60));
            Assert.That(reopened.DailyChallenge!.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(reopened.DailyChallenge.StreakDays, Is.EqualTo(1));
            Assert.That(reopened.DailyChallenge.StampsMonth, Is.EqualTo("2026-10"));
            Assert.That(reopened.DailyChallenge.StampDays, Is.EqualTo(new[] { 19 }));

            var wallet2 = new Wallet(reopened, disk, clock);
            var store2 = new ProfileDailyChallengeStore(reopened, disk);
            var rules2 = new DailyChallengeRules(store2, wallet2, clock, FakeServerDate.Offline(), DailyChallengeConfig.Defaults);
            Assert.That(rules2.Claim("2026-10-19"), Is.Not.EqualTo(ClaimOutcome.Confirmed), "la misma fecha no se cobra dos veces");
            Assert.That(wallet2.Seeds, Is.EqualTo(seeds + 60));
        }

        [Test]
        public void Mapping_CoversEveryProperty()
        {
            AssertCovers(typeof(DailyChallengeData), "LastDate", "Result", "StreakDays", "LastClaimedDate", "Pending", "OfflineClaims", "StampsMonth", "StampDays", "InProgress");
            AssertCovers(typeof(DailyClaimData), "Date", "Streak");
            AssertCovers(typeof(DailyInProgressData), "Date", "Band", "Id", "LevelState");
            AssertCovers(typeof(DailyChallengeRecord), "LastDate", "Result", "StreakDays", "LastClaimedDate", "PendingClaims", "OfflineClaims", "StampsMonth", "StampDays", "InProgress");

            DailyChallengeRecord back = ProfileDailyChallengeStore.FromData(ProfileDailyChallengeStore.ToData(FullRecord()));
            AssertFull(back);
            foreach (DailyResult result in new[] { DailyResult.None, DailyResult.Win, DailyResult.Fail, DailyResult.Abandon })
            {
                var r = new DailyChallengeRecord { Result = result };
                Assert.That(ProfileDailyChallengeStore.FromData(ProfileDailyChallengeStore.ToData(r)).Result, Is.EqualTo(result));
            }

            Assert.That(ProfileDailyChallengeStore.ToData(new DailyChallengeRecord()).Result, Is.Null, "none se guarda como null");
        }

        private static DailyChallengeRecord FullRecord()
        {
            var r = new DailyChallengeRecord
            {
                LastDate = "2026-10-19", Result = DailyResult.Win, StreakDays = 6, LastClaimedDate = "2026-10-19", StampsMonth = "2026-10",
                InProgress = new DailyPlayInProgress { DateKey = "2026-10-19", Band = 2, BoardId = "d2_n0291" },
            };
            r.PendingClaims.Add(new DailyClaimEntry("2026-10-17", 4));
            r.OfflineClaims.Add(new DailyClaimEntry("2026-10-18", 5));
            r.StampDays.AddRange(new[] { 1, 2, 5, 19 });
            LevelStateData s = r.InProgress.State;
            s.Level = 41;
            s.Seed = "amazonfill:daily:v1:b2:n0291";
            s.FailsLevel = 1;
            s.Pours = 7;
            s.UndoFree = 2;
            s.RevealedCuias.Add(3);
            s.History = "0>1,2>3";
            return r;
        }

        private static void AssertFull(DailyChallengeRecord r)
        {
            Assert.That(r.LastDate, Is.EqualTo("2026-10-19"));
            Assert.That(r.Result, Is.EqualTo(DailyResult.Win));
            Assert.That(r.StreakDays, Is.EqualTo(6));
            Assert.That(r.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(r.PendingClaims, Is.EqualTo(new[] { new DailyClaimEntry("2026-10-17", 4) }));
            Assert.That(r.OfflineClaims, Is.EqualTo(new[] { new DailyClaimEntry("2026-10-18", 5) }));
            Assert.That(r.StampsMonth, Is.EqualTo("2026-10"));
            Assert.That(r.StampDays, Is.EqualTo(new[] { 1, 2, 5, 19 }));
            Assert.That(r.InProgress, Is.Not.Null);
            Assert.That(r.InProgress!.DateKey, Is.EqualTo("2026-10-19"));
            Assert.That(r.InProgress.Band, Is.EqualTo(2));
            Assert.That(r.InProgress.BoardId, Is.EqualTo("d2_n0291"));
            Assert.That(r.InProgress.State.Level, Is.EqualTo(41));
            Assert.That(r.InProgress.State.Seed, Is.EqualTo("amazonfill:daily:v1:b2:n0291"));
            Assert.That(r.InProgress.State.FailsLevel, Is.EqualTo(1));
            Assert.That(r.InProgress.State.Pours, Is.EqualTo(7));
            Assert.That(r.InProgress.State.UndoFree, Is.EqualTo(2));
            Assert.That(r.InProgress.State.RevealedCuias, Is.EqualTo(new[] { 3 }));
            Assert.That(r.InProgress.State.History, Is.EqualTo("0>1,2>3"));
            Assert.That(r.IsStamped("2026-10-19"), Is.True);
        }

        private static void AssertCovers(Type type, params string[] mapped)
        {
            var set = new HashSet<string>(mapped);
            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(set.Contains(p.Name), Is.True, type.Name + "." + p.Name + " no está en la conversión del perfil");
            }
        }

        private static Profile NewProfile()
        {
            return Profile.CreateNew(new ManualClock(Utc(new DateTime(2026, 10, 19, 10, 0, 0)), Bogota), 5);
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }

        private sealed class FailingProfileStore : IProfileStore
        {
            public bool Fail { get; set; }

            public string? Json { get; private set; }

            public ProfileLoadResult Load()
            {
                return Json == null
                    ? new ProfileLoadResult(ProfileLoadStatus.NotFound, null)
                    : new ProfileLoadResult(ProfileLoadStatus.Loaded, ProfileJson.Deserialize(Json));
            }

            public void Save(Profile profile)
            {
                if (Fail)
                {
                    throw new IOException("disco lleno (test)");
                }

                Json = ProfileJson.Serialize(profile);
            }

            public void Delete()
            {
                Json = null;
            }
        }
    }
}
