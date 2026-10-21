#nullable enable
using System;
using System.IO;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// El bloque <c>calendar</c> del perfil (parche B4-1-profile; 08 §4.3 con Q-M3 (a) [P]; D-033, añadido del 2026-10-20, puntos
    /// 6 y 7): ida y vuelta por el JSON del perfil, la clave que falta hasta el primer guardado, el bloque antiguo de 08 §4.3, un
    /// tipo equivocado como corrupción, los marcos en <c>entitlements.frames</c> y el sello con su pago en la misma escritura.
    /// </summary>
    public sealed class ProfileLoginCalendarStoreTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        [Test]
        public void TheBlock_IsOmittedUntilTheFirstSave_AndRoundTrips()
        {
            var clock = Clock();
            Profile profile = Wallet.NewProfile(clock);
            StringAssert.DoesNotContain("\"calendar\"", ProfileJson.Serialize(profile));

            var disk = new MemoryProfileStore();
            var store = new ProfileLoginCalendarStore(profile, disk);
            LoginCalendarRecord record = store.Load();
            Assert.That(record.Cycle, Is.EqualTo(1));
            Assert.That(record.NextDay, Is.EqualTo(1));
            record.NextDay = 8;
            record.Cycle = 2;
            record.LastClaimDate = "2026-10-19";
            record.PlayedDate = "2026-10-20";
            record.Frames.Add("frame_w01");
            store.Save(record);
            Assert.That(store.PendingSave, Is.False);
            StringAssert.Contains("\"calendar\"", disk.Json);
            StringAssert.Contains("\"played_date\"", disk.Json);

            Profile back = ProfileJson.Deserialize(disk.Json!);
            Assert.That(back.Calendar!.NextDay, Is.EqualTo(8));
            Assert.That(back.Entitlements.Frames, Is.EqualTo(new[] { "frame_w01" }), "el marco, como derecho en entitlements.frames (D-033, punto 7)");
            LoginCalendarRecord again = ProfileLoginCalendarStore.FromProfile(back);
            Assert.That(again.Cycle, Is.EqualTo(2));
            Assert.That(again.LastClaimDate, Is.EqualTo("2026-10-19"));
            Assert.That(again.PlayedDate, Is.EqualTo("2026-10-20"));
            Assert.That(again.Frames, Is.EqualTo(new[] { "frame_w01" }));
        }

        [Test]
        public void TheOldExampleOf08_4_3_ReadsAsANewLogbook_AndAWrongTypeIsCorruption()
        {
            Profile profile = Wallet.NewProfile(Clock());
            string json = ProfileJson.Serialize(profile);
            string head = json.Substring(0, json.LastIndexOf('}'));
            string old = head + ",\"calendar\":{\"month\":\"2026-10\",\"claimed_days\":[1,2,3],\"last_claim_date\":\"2026-10-05\"}}";
            Profile read = ProfileJson.Deserialize(old);
            Assert.That(read.Calendar, Is.Not.Null);
            Assert.That(read.Calendar!.Cycle, Is.EqualTo(1));
            Assert.That(read.Calendar.NextDay, Is.EqualTo(1));
            Assert.That(read.Calendar.LastClaimDate, Is.EqualTo("2026-10-05"));

            string bad = head + ",\"calendar\":{\"cycle\":\"uno\"}}";
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize(bad));
            string notObject = head + ",\"calendar\":[]}";
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize(notObject));

            var outOfRange = new Profile { Calendar = new CalendarData { NextDay = 99, Cycle = 0 } };
            LoginCalendarRecord record = ProfileLoginCalendarStore.FromProfile(outOfRange);
            Assert.That(record.NextDay, Is.EqualTo(1), "fuera de rango: lo recorta AF.LiveOps");
            Assert.That(record.Cycle, Is.EqualTo(1));
        }

        [Test]
        public void TheSealAndItsPayment_GoOutInTheSameWriteOfTheProfile()
        {
            ManualClock clock = Clock();
            Profile profile = Wallet.NewProfile(clock);
            var disk = new MemoryProfileStore();
            var wallet = new Wallet(profile, disk, clock);
            var store = new ProfileLoginCalendarStore(profile, disk);
            var service = new LoginCalendarService(store, wallet, clock, 10);
            service.MarkPlayed();
            LoginCalendarClaim? claim = service.Claim(LoginCalendarChoice.Seeds, 25, LoginCalendarUnlocks.All, 1);
            Assert.That(claim, Is.Not.Null);

            Profile onDisk = ProfileJson.Deserialize(disk.Json!);
            Assert.That(onDisk.Calendar!.NextDay, Is.EqualTo(2));
            Assert.That(onDisk.Calendar.LastClaimDate, Is.EqualTo("2026-10-20"));
            Assert.That(onDisk.Wallet.Seeds, Is.EqualTo(60), "el mismo perfil: el avance y el cobro, juntos");

            // Al reabrir, la casilla no se vuelve a cobrar.
            var reopened = new LoginCalendarService(new ProfileLoginCalendarStore(onDisk, disk), new Wallet(onDisk, disk, clock), clock, 10);
            Assert.That(reopened.StateFor(25), Is.EqualTo(LoginCalendarState.Sealed));
            Assert.That(reopened.Claim(LoginCalendarChoice.Seeds, 25, LoginCalendarUnlocks.All, 1), Is.Null);
        }

        [Test]
        public void AFailedWrite_StaysPending_AndFlushRetries()
        {
            ManualClock clock = Clock();
            Profile profile = Wallet.NewProfile(clock);
            var disk = new FailingProfileStore { Fail = true };
            var store = new ProfileLoginCalendarStore(profile, disk);
            LoginCalendarRecord record = store.Load();
            record.PlayedDate = "2026-10-20";
            store.Save(record);
            Assert.That(store.PendingSave, Is.True);
            Assert.That(profile.Calendar!.PlayedDate, Is.EqualTo("2026-10-20"), "el cambio sigue en el perfil en memoria");
            disk.Fail = false;
            Assert.That(store.Flush(), Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(2));
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

        private static ManualClock Clock()
        {
            return new ManualClock(DateTime.SpecifyKind(new DateTime(2026, 10, 20, 10, 0, 0) - Bogota, DateTimeKind.Utc), Bogota);
        }
    }
}
