#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// La nube falsa (<see cref="FakeCloudSave"/>; 08 §11 y §2.1 principio 6) y el reloj con anti-reloj (<see cref="GuardedClock"/>;
    /// 08 §10.1; 04 §7.1, «Reloj»), del bloque B8.
    /// </summary>
    public sealed class CloudSaveAndClockTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 23, 30, 0, DateTimeKind.Utc);

        [Test]
        public void FakeCloud_RoundTripsACopy_AndDeletes()
        {
            var cloud = new FakeCloudSave();
            CloudLoadResult? loaded = null;
            cloud.Load(r => loaded = r);
            Assert.That(loaded!.Status, Is.EqualTo(CloudLoadStatus.NotFound));

            byte[] blob = { 1, 2, 3 };
            CloudWriteResult? write = null;
            cloud.Save(blob, r => write = r);
            blob[0] = 9; // el snapshot es una copia
            Assert.That(write, Is.EqualTo(CloudWriteResult.Ok));
            cloud.Load(r => loaded = r);
            Assert.That(loaded!.Status, Is.EqualTo(CloudLoadStatus.Loaded));
            Assert.That(loaded.Blob, Is.EqualTo(new byte[] { 1, 2, 3 }));

            cloud.Delete(r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Ok));
            Assert.That(cloud.Blob, Is.Null);
            cloud.Delete(r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Ok), "borrar lo que no existe es un éxito");
            Assert.That(cloud.SaveCount, Is.EqualTo(1));
            Assert.That(cloud.DeleteCount, Is.EqualTo(2));
        }

        [Test]
        public void FakeCloud_WithoutSession_DoesNothing_AndFailNextFailsOnce()
        {
            FakeCloudSave cloud = FakeCloudSave.WithoutSession();
            CloudLoadResult? loaded = null;
            CloudWriteResult? write = null;
            cloud.Load(r => loaded = r);
            cloud.Save(new byte[] { 1 }, r => write = r);
            Assert.That(loaded!.Status, Is.EqualTo(CloudLoadStatus.NoSession));
            Assert.That(write, Is.EqualTo(CloudWriteResult.NoSession));
            Assert.That(cloud.Blob, Is.Null);

            cloud.HasSession = true;
            cloud.FailNext = true;
            cloud.Save(new byte[] { 1 }, r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Failed));
            cloud.Save(new byte[] { 1 }, r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Ok));
        }

        [Test]
        public void FakeCloud_RejectsBlobsOverTheSavedGamesLimit()
        {
            var cloud = new FakeCloudSave();
            CloudWriteResult? write = null;
            cloud.Save(new byte[CloudSaveNames.MaxBlobBytes + 1], r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.TooLarge));
            Assert.That(CloudSaveNames.Snapshot, Is.EqualTo("af_main"), "08 §11");
        }

        [Test]
        public void CloudLoadResult_OnlyLoadedCarriesABlob()
        {
            Assert.Throws<ArgumentException>(() => new CloudLoadResult(CloudLoadStatus.Loaded));
            Assert.Throws<ArgumentException>(() => new CloudLoadResult(CloudLoadStatus.NotFound, new byte[1]));
        }

        [Test]
        public void GuardedClock_Offline_IsTheDeviceClock()
        {
            var device = new ManualClock(T0, TimeSpan.FromHours(-5));
            var clock = new GuardedClock(device, FakeServerDate.Offline());
            Assert.That(clock.DriftExceeded, Is.False, "sin red se acepta la fecha local (08 §10.1)");
            Assert.That(clock.UtcNow, Is.EqualTo(T0));
            Assert.That(clock.LocalOffset, Is.EqualTo(TimeSpan.FromHours(-5)));
            Assert.That(clock.Device, Is.SameAs(device));
        }

        [TestCase(24, false)]
        [TestCase(-24, false)]
        [TestCase(25, true)]
        [TestCase(-72, true)]
        public void GuardedClock_OnlineBeyondTheDrift_UsesTheServerTime(int serverMinusDeviceH, bool exceeded)
        {
            var device = new ManualClock(T0);
            TimeSpan offset = TimeSpan.FromHours(serverMinusDeviceH);
            var clock = new GuardedClock(device, FakeServerDate.Online(offset));
            Assert.That(clock.DriftExceeded, Is.EqualTo(exceeded), "sec_clock_max_drift_h = 24 (08 §9); igual a la cota no la supera");
            Assert.That(clock.UtcNow, Is.EqualTo(exceeded ? T0 + offset : T0));
            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void GuardedClock_DailyCapsUseTheServerDate_WhenTheDeviceIsTooFarAhead()
        {
            // Teléfono adelantado 3 días para reabrir los topes: con red, la fecha de los topes es la del servidor.
            var device = new ManualClock(T0.AddDays(3));
            var server = FakeServerDate.Online(TimeSpan.FromDays(-3));
            var clock = new GuardedClock(device, server);
            Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-20"));

            server.GoOffline();
            Assert.That(clock.LocalDateKey(), Is.EqualTo("2026-10-23"), "sin red, la local (se reconcilia al reconectar)");
            Assert.That(clock is IClockTrust, Is.True);
        }
    }
}
