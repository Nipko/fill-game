#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Parche S1-profile (B1a, tarea A4; 06 §4.3; 08 §4.3 con Q-M9 (a) [P]): el bloque <c>streak</c> = <c>{wins, river_full}</c>
    /// (omitido mientras es null), <c>daily.streak_hits</c> y <c>daily.streak_seeds</c> (omitidos en su default), un solo
    /// <c>shield_available</c> (el de <c>daily</c>), el cambio de día de <see cref="DailyCounters"/> y
    /// <see cref="ProfileStreakStore"/> sobre el mismo perfil y el mismo almacén que la cartera.
    /// </summary>
    public sealed class StreakProfileTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        private static readonly string StreakJson = string.Join("\n", new[]
        {
            "    \"undo_tip_seen\": false",
            "  },",
            "  \"streak\": {",
            "    \"wins\": 7,",
            "    \"river_full\": true",
            "  },",
            "  \"level_state\": null,",
        });

        private static readonly string DailyTailJson = string.Join("\n", new[]
        {
            "    \"shield_available\": false,",
            "    \"streak_hits\": [3, 6],",
            "    \"streak_seeds\": 350",
            "  },",
        });

        [Test]
        public void NewProfile_WritesNoStreakKeys()
        {
            Profile p = NewProfile();
            string json = ProfileJson.Serialize(p);

            Assert.That(p.Streak, Is.Null);
            Assert.That(json.Contains("streak"), Is.False, "streak, streak_hits y streak_seeds faltan hasta tener valor");
            Assert.That(p.Daily.StreakHits, Is.Empty);
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(0));
            Assert.That(Profile.CurrentSchema, Is.EqualTo(2), "sin migración: el esquema no se ha publicado (08 §4.3)");

            DailyCounters.RollOver(p.Daily, "2026-10-19");
            Assert.That(ProfileJson.Serialize(p).Contains("streak"), Is.False, "tras el cambio de día siguen en su default");
            StringAssert.Contains("\"shield_available\": true\n  },", ProfileJson.Serialize(p));
        }

        [Test]
        public void FullStreak_IsWrittenWithItsKeys_AndRoundTripsExactly()
        {
            Profile p = FullProfile();
            string json = ProfileJson.Serialize(p);

            StringAssert.Contains(StreakJson, json, "streak tras progress, como en 08 §4.3, sin shield_available");
            StringAssert.Contains(DailyTailJson, json, "streak_hits y streak_seeds al final de daily");
            Assert.That(json.Split(new[] { "shield_available" }, StringSplitOptions.None).Length, Is.EqualTo(2), "un solo shield_available");

            Profile copy = ProfileJson.Deserialize(json);
            Assert.That(ProfileJson.Serialize(copy), Is.EqualTo(json));
            Assert.That(copy.Streak, Is.Not.Null);
            Assert.That(copy.Streak!.Wins, Is.EqualTo(7));
            Assert.That(copy.Streak.RiverFull, Is.True);
            Assert.That(copy.Daily.StreakHits, Is.EqualTo(new[] { 3, 6 }));
            Assert.That(copy.Daily.StreakSeeds, Is.EqualTo(350));
            Assert.That(copy.Daily.ShieldAvailable, Is.False);

            Profile clone = p.Clone();
            Assert.That(clone.Streak, Is.Not.SameAs(p.Streak));
            clone.Daily.StreakHits.Add(10);
            Assert.That(p.Daily.StreakHits, Is.EqualTo(new[] { 3, 6 }));
        }

        [Test]
        public void ZeroStreak_IsStillWritten_OnceSaved()
        {
            Profile p = NewProfile();
            p.Streak = new StreakData();

            StringAssert.Contains("\"streak\": {\n    \"wins\": 0,\n    \"river_full\": false\n  },", ProfileJson.Serialize(p));
            Assert.That(ProfileJson.Deserialize(ProfileJson.Serialize(p)).Streak, Is.Not.Null);
        }

        [Test]
        public void MissingKeys_TakeDefaults()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"streak\": {\"wins\": 4}, \"daily\": {\"date_local\": \"2026-10-19\"}}");
            Assert.That(p.Streak!.Wins, Is.EqualTo(4));
            Assert.That(p.Streak.RiverFull, Is.False);
            Assert.That(p.Daily.StreakHits, Is.Empty);
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(0));

            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2, \"streak\": null}").Streak, Is.Null);
            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2}").Streak, Is.Null);
            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2, \"daily\": {\"streak_hits\": null}}").Daily.StreakHits, Is.Empty);
        }

        [TestCase("{\"profile_schema\": 2, \"streak\": []}")]
        [TestCase("{\"profile_schema\": 2, \"streak\": 4}")]
        [TestCase("{\"profile_schema\": 2, \"streak\": {\"wins\": \"4\"}}")]
        [TestCase("{\"profile_schema\": 2, \"streak\": {\"wins\": 4.5}}")]
        [TestCase("{\"profile_schema\": 2, \"streak\": {\"river_full\": 1}}")]
        [TestCase("{\"profile_schema\": 2, \"streak\": {\"river_full\": \"true\"}}")]
        [TestCase("{\"profile_schema\": 2, \"daily\": {\"streak_hits\": 3}}")]
        [TestCase("{\"profile_schema\": 2, \"daily\": {\"streak_hits\": {\"3\": true}}}")]
        [TestCase("{\"profile_schema\": 2, \"daily\": {\"streak_hits\": [3, \"6\"]}}")]
        [TestCase("{\"profile_schema\": 2, \"daily\": {\"streak_seeds\": \"350\"}}")]
        [TestCase("{\"profile_schema\": 2, \"daily\": {\"streak_seeds\": true}}")]
        [TestCase("{\"profile_schema\": 2, \"daily\": {\"streak_seeds\": null}}")]
        public void WrongTypes_AreFormatErrors(string json)
        {
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize(json));
        }

        [Test]
        public void RollOver_NewDay_RefillsTheShield_AndClearsHitsAndSeeds_ButNotTheStreak()
        {
            Profile p = FullProfile();
            p.Daily.RvTotal = 4;

            Assert.That(DailyCounters.RollOver(p.Daily, "2026-10-20"), Is.True);

            Assert.That(p.Daily.DateLocal, Is.EqualTo("2026-10-20"));
            Assert.That(p.Daily.ShieldAvailable, Is.True);
            Assert.That(p.Daily.StreakHits, Is.Empty);
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(0));
            Assert.That(p.Daily.RvTotal, Is.EqualTo(0), "el bloque entero, de una vez");
            Assert.That(p.Streak!.Wins, Is.EqualTo(7), "sin expiración por reloj (04 §8)");
            Assert.That(p.Streak.RiverFull, Is.True);
        }

        [TestCase("2026-10-19")]
        [TestCase("2026-10-18")]
        [TestCase("2025-12-31")]
        public void RollOver_SameDayOrClockBack_RefillsNothing(string today)
        {
            Profile p = FullProfile();

            Assert.That(DailyCounters.RollOver(p.Daily, today), Is.False);

            Assert.That(p.Daily.DateLocal, Is.EqualTo("2026-10-19"));
            Assert.That(p.Daily.ShieldAvailable, Is.False, "atrasar el reloj no repone la Hoja escudo");
            Assert.That(p.Daily.StreakHits, Is.EqualTo(new[] { 3, 6 }));
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(350));
        }

        [Test]
        public void Store_Load_IsNeverNull_AndRollsTheWholeDailyBlock()
        {
            var clock = Clock(new DateTime(2026, 10, 19, 10, 0, 0));
            Profile p = NewProfile();
            var store = new ProfileStreakStore(p, new MemoryProfileStore(), clock);

            StreakRecord r = store.Load();
            Assert.That(r, Is.Not.Null);
            Assert.That(store.Load(), Is.SameAs(r), "el estado vivo");
            Assert.That(r.Wins, Is.EqualTo(0));
            Assert.That(r.RiverFull, Is.False);
            Assert.That(r.ShieldAvailable, Is.True, "primer día: una Hoja escudo");
            Assert.That(r.DateLocal, Is.EqualTo("2026-10-19"));
            Assert.That(p.Daily.DateLocal, Is.EqualTo("2026-10-19"));
            Assert.That(p.Streak, Is.Null, "leer no crea el bloque");
            Assert.That(p.EconomyTs, Is.EqualTo(clock.UtcNow), "el cambio de día muta daily: economy_ts (08 §4.3)");

            Profile full = FullProfile();
            full.Daily.RvHint = 2;
            var store2 = new ProfileStreakStore(full, new MemoryProfileStore(), clock);
            Assert.That(store2.Load().HitsToday, Is.EqualTo(new[] { 3, 6 }), "mismo día: nada se repone");
            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 1, 0)));
            StreakRecord next = store2.Load();
            Assert.That(next.ShieldAvailable, Is.True);
            Assert.That(next.HitsToday, Is.Empty);
            Assert.That(next.SeedsToday, Is.EqualTo(0));
            Assert.That(next.Wins, Is.EqualTo(7));
            Assert.That(full.Daily.RvHint, Is.EqualTo(0), "el cambio de día es el del bloque entero");
        }

        [Test]
        public void Store_SeesADayChangeMadeByAnotherModule()
        {
            var clock = Clock(new DateTime(2026, 10, 19, 23, 59, 0));
            Profile p = FullProfile();
            var store = new ProfileStreakStore(p, new MemoryProfileStore(), clock);
            StreakRecord stale = store.Load().Clone();

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 1, 0)));
            DailyCounters.RollOver(p.Daily, clock.LocalDateKey()); // la cartera o los anuncios pasan el día
            stale.Wins = 8;
            store.Save(stale);

            Assert.That(p.Streak!.Wins, Is.EqualTo(8), "la racha se escribe siempre");
            Assert.That(p.Daily.ShieldAvailable, Is.True, "un estado de ayer no pisa la Hoja escudo de hoy");
            Assert.That(p.Daily.StreakHits, Is.Empty);
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(0));
            Assert.That(store.Load().ShieldAvailable, Is.True);
        }

        [Test]
        public void Store_Save_RecordFromALaterDay_RollsTheWholeDailyBlockFirst()
        {
            var clock = Clock(new DateTime(2026, 10, 20, 9, 0, 0));
            Profile p = FullProfile();
            p.Daily.RvTotal = 3;
            var record = new StreakRecord { Wins = 1, DateLocal = "2026-10-20", ShieldAvailable = false };

            Assert.That(ProfileStreakStore.WriteTo(p, new StreakRecord(), record), Is.True);

            Assert.That(p.Daily.DateLocal, Is.EqualTo("2026-10-20"));
            Assert.That(p.Daily.RvTotal, Is.EqualTo(0));
            Assert.That(p.Daily.ShieldAvailable, Is.False, "el escudo del estado");
            Assert.That(p.Daily.StreakHits, Is.Empty);
            Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-20"));
        }

        [Test]
        public void Store_Save_WritesTheWholeProfile_AndAReopenedStoreReadsTheSameState()
        {
            var clock = Clock(new DateTime(2026, 10, 19, 10, 0, 0));
            var disk = new MemoryProfileStore();
            Profile p = NewProfile();
            var store = new ProfileStreakStore(p, disk, clock);
            StreakRecord r = store.Load();
            r.Wins = 3;
            r.HitsToday.Add(3);
            r.SeedsToday = 300;
            store.Save(r);

            Assert.That(disk.SaveCount, Is.EqualTo(1));
            Assert.That(store.PendingSave, Is.False);
            Assert.That(p.EconomyTs, Is.EqualTo(clock.UtcNow), "daily cambió: economy_ts (08 §4.3)");
            r.HitsToday.Add(6); // sin Save no llega al perfil
            Assert.That(p.Daily.StreakHits, Is.EqualTo(new[] { 3 }), "el perfil guarda una copia");

            StreakRecord reopened = new ProfileStreakStore(disk.Load().Profile!, disk, clock).Load();
            Assert.That(reopened.Wins, Is.EqualTo(3));
            Assert.That(reopened.HitsToday, Is.EqualTo(new[] { 3 }));
            Assert.That(reopened.SeedsToday, Is.EqualTo(300));
            Assert.That(reopened.ShieldAvailable, Is.True);
        }

        [Test]
        public void Store_OnlyStreakChange_LeavesEconomyTs()
        {
            var clock = Clock(new DateTime(2026, 10, 19, 10, 0, 0));
            Profile p = NewProfile();
            var store = new ProfileStreakStore(p, new MemoryProfileStore(), clock);
            StreakRecord r = store.Load();
            DateTime? afterRollOver = p.EconomyTs;
            Assert.That(afterRollOver, Is.EqualTo(clock.UtcNow), "el cambio de día de Load sí cuenta");
            clock.Set(clock.UtcNow.AddMinutes(5));
            Assert.That(store.Load(), Is.SameAs(r));
            Assert.That(p.EconomyTs, Is.EqualTo(afterRollOver), "mismo día: Load no muta nada");
            store.Save(r); // sin cambios en daily
            Assert.That(p.EconomyTs, Is.EqualTo(afterRollOver));

            r.Wins = 2;
            store.Save(r);
            Assert.That(p.EconomyTs, Is.EqualTo(afterRollOver), "streak no es wallet, stock ni daily");
            Assert.That(p.Streak!.Wins, Is.EqualTo(2));
        }

        [Test]
        public void StaleRecord_HitPaidAfterTheWalletPassedTheDay_IsNotPaidAgainToday()
        {
            // (a) Load a las 23:59; la cartera pasa daily al día siguiente al pagar; el hito 3 se guarda con el registro de ayer.
            var clock = Clock(new DateTime(2026, 10, 19, 23, 59, 0));
            var disk = new MemoryProfileStore();
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, disk, clock);
            var store = new ProfileStreakStore(profile, disk, clock);
            StreakRecord r = store.Load();
            r.Wins = 3;

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            wallet.Apply(Transaction.Earn(SeedSource.Streak, 300));
            Assert.That(profile.Daily.DateLocal, Is.EqualTo("2026-10-20"), "la cartera pasó el día");
            r.HitsToday.Add(3);
            r.SeedsToday += 300;
            store.Save(r);

            Assert.That(profile.Daily.StreakHits, Is.EqualTo(new[] { 3 }), "el hito 3 cuenta como pagado hoy");
            Assert.That(profile.Daily.StreakSeeds, Is.EqualTo(300));
            StreakRecord again = new ProfileStreakStore(disk.Load().Profile!, disk, clock).Load();
            Assert.That(again.HitsToday, Is.EqualTo(new[] { 3 }), "no se vuelve a pagar hoy, tampoco al reabrir");
            Assert.That(again.Wins, Is.EqualTo(3));
        }

        [Test]
        public void StaleRecord_ShieldSpentAfterAnotherModulePassedTheDay_LeavesNoSecondShield()
        {
            // (b) El escudo se gasta con el registro de ayer: no queda un segundo escudo hoy.
            var clock = Clock(new DateTime(2026, 10, 19, 23, 59, 0));
            Profile p = NewProfile();
            p.Streak = new StreakData { Wins = 4 };
            var store = new ProfileStreakStore(p, new MemoryProfileStore(), clock);
            StreakRecord r = store.Load();
            Assert.That(r.ShieldAvailable, Is.True);

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            DailyCounters.RollOver(p.Daily, clock.LocalDateKey()); // los anuncios o las compras pasan el día
            r.ShieldAvailable = false;
            store.Save(r);

            Assert.That(p.Daily.ShieldAvailable, Is.False, "no hay segundo escudo hoy");
            Assert.That(store.Load().ShieldAvailable, Is.False);
            Assert.That(p.Streak!.Wins, Is.EqualTo(4));
        }

        [Test]
        public void StaleRecord_ShieldSpentYesterday_DoesNotDenyTodaysShield()
        {
            // (c) El registro ya llevaba el escudo de ayer gastado al cargarse: el de hoy sigue disponible.
            var clock = Clock(new DateTime(2026, 10, 19, 23, 59, 0));
            Profile p = FullProfile(); // 2026-10-19, escudo gastado, hitos [3, 6], 350 semillas
            var store = new ProfileStreakStore(p, new MemoryProfileStore(), clock);
            StreakRecord r = store.Load();

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            DailyCounters.RollOver(p.Daily, clock.LocalDateKey());
            r.Wins = 8;
            store.Save(r);

            Assert.That(p.Daily.ShieldAvailable, Is.True, "el escudo de hoy no se niega");
            Assert.That(p.Daily.StreakHits, Is.Empty, "los hitos de ayer no cuentan hoy");
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(0));
            Assert.That(p.Streak!.Wins, Is.EqualTo(8));
        }

        [Test]
        public void StaleRecord_TwoSavesInARow_OnlyTheSecondChangeIsApplied_AndSpendsTodaysShield()
        {
            // (d) Dos Save seguidos del mismo registro de ayer: el primero lleva el hito 3; entre los dos se gasta el escudo. El
            // segundo no vuelve a sumar el hito ni las semillas y gasta el escudo de hoy (la carga de referencia es el primer Save).
            var clock = Clock(new DateTime(2026, 10, 19, 23, 59, 0));
            Profile p = NewProfile();
            p.Streak = new StreakData { Wins = 2 };
            var store = new ProfileStreakStore(p, new MemoryProfileStore(), clock);
            StreakRecord r = store.Load();
            Assert.That(r.ShieldAvailable, Is.True);

            clock.Set(Utc(new DateTime(2026, 10, 20, 0, 0, 30)));
            DailyCounters.RollOver(p.Daily, clock.LocalDateKey()); // la cartera o los anuncios pasan el día
            r.Wins = 3;
            r.HitsToday.Add(3);
            r.SeedsToday += 300;
            store.Save(r);

            Assert.That(p.Daily.StreakHits, Is.EqualTo(new[] { 3 }));
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(300));
            Assert.That(p.Daily.ShieldAvailable, Is.True, "el primer Save no cambió el escudo");

            r.ShieldAvailable = false; // fallo duro cubierto, con el mismo registro de ayer
            store.Save(r);

            Assert.That(p.Daily.ShieldAvailable, Is.False, "el escudo gastado entre los dos Save gasta el de hoy");
            Assert.That(p.Daily.StreakHits, Is.EqualTo(new[] { 3 }), "el hito no se suma dos veces");
            Assert.That(p.Daily.StreakSeeds, Is.EqualTo(300), "las semillas no se suman dos veces");
            Assert.That(p.Daily.DateLocal, Is.EqualTo("2026-10-20"));
            Assert.That(p.Streak!.Wins, Is.EqualTo(3));
        }

        [Test]
        public void Load_ClampsOutOfRangeValuesFromTheFile()
        {
            var clock = Clock(new DateTime(2026, 10, 19, 10, 0, 0));
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"streak\": {\"wins\": 15, \"river_full\": true},"
                + " \"daily\": {\"date_local\": \"2026-10-19\", \"streak_hits\": [6, 3, 3, 7], \"streak_seeds\": -3}}");
            Assert.That(p.Daily.StreakHits, Is.EqualTo(new[] { 6, 3, 3, 7 }), "AF.Save guarda lo que lee; recorta AF.Economy");

            StreakRecord r = new ProfileStreakStore(p, new MemoryProfileStore(), clock).Load();

            Assert.That(r.Wins, Is.EqualTo(10));
            Assert.That(r.RiverFull, Is.True);
            Assert.That(r.HitsToday, Is.EqualTo(new[] { 3, 6 }));
            Assert.That(r.SeedsToday, Is.EqualTo(0));
            var direct = new StreakRecord();
            p.Streak!.Wins = -4;
            ProfileStreakStore.ReadInto(p, direct);
            Assert.That(direct.Wins, Is.EqualTo(0));
        }

        [Test]
        public void HitAndPayment_LiveInTheSameProfile_AndGoOutInOneWrite()
        {
            // Requisito de «nunca dos pagos»: el apunte del hito y su pago, con el disco caído, quedan juntos en memoria y salen
            // juntos en la siguiente escritura; reabrir no deja el pago sin el apunte.
            var clock = Clock(new DateTime(2026, 10, 19, 10, 0, 0));
            var disk = new FailingProfileStore { Fail = true };
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, disk, clock);
            var store = new ProfileStreakStore(profile, disk, clock);
            int seeds = wallet.Seeds;

            StreakRecord r = store.Load();
            r.Wins = 3;
            r.HitsToday.Add(3);
            r.SeedsToday = 300;
            store.Save(r);
            wallet.Apply(Transaction.Earn(SeedSource.Streak, 300));
            Assert.That(store.PendingSave, Is.True);
            Assert.That(wallet.PendingSave, Is.True);
            Assert.That(disk.Json, Is.Null);

            disk.Fail = false;
            Assert.That(store.Flush(), Is.True);
            Assert.That(disk.WriteCount, Is.EqualTo(1));

            Profile reopened = ProfileJson.Deserialize(disk.Json!);
            Assert.That(reopened.Wallet.Seeds, Is.EqualTo(seeds + 300), "el pago");
            Assert.That(reopened.Daily.StreakHits, Is.EqualTo(new[] { 3 }), "y su apunte, en la misma escritura");
            Assert.That(reopened.Daily.StreakSeeds, Is.EqualTo(300));
            Assert.That(reopened.Streak!.Wins, Is.EqualTo(3));

            StreakRecord again = new ProfileStreakStore(reopened, disk, clock).Load();
            Assert.That(again.HitsToday.Contains(3), Is.True, "el hito 3 ya está pagado hoy");
        }

        [Test]
        public void FailedWrite_StaysPendingInTheProfile_UntilFlush()
        {
            var clock = Clock(new DateTime(2026, 10, 19, 10, 0, 0));
            var disk = new FailingProfileStore { Fail = true };
            Profile p = NewProfile();
            var store = new ProfileStreakStore(p, disk, clock);
            StreakRecord r = store.Load();
            r.ShieldAvailable = false;

            Assert.DoesNotThrow(() => store.Save(r));
            Assert.That(store.PendingSave, Is.True);
            Assert.That(p.Daily.ShieldAvailable, Is.False, "el cambio sigue en el perfil en memoria");
            Assert.That(store.Flush(), Is.False);

            disk.Fail = false;
            Assert.That(store.Flush(), Is.True);
            Assert.That(ProfileJson.Deserialize(disk.Json!).Daily.ShieldAvailable, Is.False);
            Assert.That(store.Flush(), Is.True, "nada pendiente");
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void SchemaOneProfile_KeepsTheStreak_AfterTheMigrationToSchemaTwo()
        {
            string dir = Path.Combine(Path.GetTempPath(), "af-streak-migration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, LocalProfileStore.FileName),
                    "{\"profile_schema\": 1, \"streak\": {\"wins\": 5, \"river_full\": true},"
                    + " \"daily\": {\"date_local\": \"2026-10-19\", \"streak_hits\": [3], \"streak_seeds\": 300}}");

                ProfileLoadResult loaded = new LocalProfileStore(dir).Load();

                Assert.That(loaded.Migrated, Is.True);
                Assert.That(loaded.Profile!.Streak!.Wins, Is.EqualTo(5));
                Assert.That(loaded.Profile.Streak.RiverFull, Is.True);
                Assert.That(loaded.Profile.Daily.StreakHits, Is.EqualTo(new[] { 3 }));
                Assert.That(loaded.Profile.Daily.StreakSeeds, Is.EqualTo(300));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void Mapping_CoversEveryProperty()
        {
            AssertCovers(typeof(StreakData), "Wins", "RiverFull");
            AssertCovers(typeof(StreakRecord), "Wins", "RiverFull", "DateLocal", "ShieldAvailable", "HitsToday", "SeedsToday");

            Profile p = FullProfile();
            var r = new StreakRecord();
            ProfileStreakStore.ReadInto(p, r);
            Assert.That(r.Wins, Is.EqualTo(7));
            Assert.That(r.RiverFull, Is.True);
            Assert.That(r.DateLocal, Is.EqualTo("2026-10-19"));
            Assert.That(r.ShieldAvailable, Is.False);
            Assert.That(r.HitsToday, Is.EqualTo(new[] { 3, 6 }));
            Assert.That(r.SeedsToday, Is.EqualTo(350));

            Profile back = NewProfile();
            back.Daily.DateLocal = "2026-10-19";
            ProfileStreakStore.WriteTo(back, new StreakRecord(), r);
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(ProfileJson.Serialize(WithCreatedAt(FullProfile(), back.CreatedAt))));
            Assert.That(back.Daily.StreakHits, Is.Not.SameAs(r.HitsToday));
        }

        private static Profile FullProfile()
        {
            Profile p = NewProfile();
            p.Streak = new StreakData { Wins = 7, RiverFull = true };
            p.Daily.DateLocal = "2026-10-19";
            p.Daily.ShieldAvailable = false;
            p.Daily.StreakHits.Add(3);
            p.Daily.StreakHits.Add(6);
            p.Daily.StreakSeeds = 350;
            return p;
        }

        private static Profile WithCreatedAt(Profile p, DateTime createdAt)
        {
            p.CreatedAt = createdAt;
            return p;
        }

        private static Profile NewProfile()
        {
            return Profile.CreateNew(new ManualClock(Utc(new DateTime(2026, 10, 19, 10, 0, 0)), Bogota), 5);
        }

        private static ManualClock Clock(DateTime bogota)
        {
            return new ManualClock(Utc(bogota), Bogota);
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }

        private static void AssertCovers(Type type, params string[] mapped)
        {
            var set = new HashSet<string>(mapped);
            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(set.Contains(p.Name), Is.True, type.Name + "." + p.Name + " no está en la conversión del perfil");
            }
        }

        private sealed class FailingProfileStore : IProfileStore
        {
            public bool Fail { get; set; }

            public string? Json { get; private set; }

            public int WriteCount { get; private set; }

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
                    throw new IOException("disco lleno (simulado)");
                }

                Json = ProfileJson.Serialize(profile);
                WriteCount++;
            }

            public void Delete()
            {
                Json = null;
            }
        }
    }
}
