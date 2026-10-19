#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Pendientes menores de la revisión de R7b: <c>offline_claims</c> acotada a los últimos
    /// <see cref="DailyChallengeRules.OfflineClaimsMaxDays"/> días [P] y <see cref="DailyChallengeRules.Reconcile"/> sin escritura
    /// cuando no hay nada que reconciliar.
    /// </summary>
    public sealed class DailyChallengeHousekeepingTests
    {
        [Test]
        public void OfflineClaims_KeepOnlyTheLast31Days()
        {
            var f = new DailyFixture(FakeServerDate.Offline(), localStart: new DateTime(2026, 9, 1, 10, 0, 0));
            for (int i = 0; i < 40; i++)
            {
                Assert.That(f.Rules.Claim(f.Today), Is.EqualTo(ClaimOutcome.Confirmed), f.Today);
                f.NextDay();
            }

            Assert.That(DailyChallengeRules.OfflineClaimsMaxDays, Is.EqualTo(31));
            Assert.That(f.Record.OfflineClaims.Count, Is.EqualTo(31));
            Assert.That(f.Record.OfflineClaims[0].DateKey, Is.EqualTo("2026-09-10"), "40 días desde el 1 de septiembre: quedan del 10-09 al 10-10");
            Assert.That(f.Record.OfflineClaims[30].DateKey, Is.EqualTo("2026-10-10"));
            Assert.That(f.Seeds, Is.EqualTo(40 * 60), "acotar la lista no cambia los pagos");
        }

        [Test]
        public void OfflineClaims_WithGaps_DropByDateNotByCount()
        {
            var f = new DailyFixture(FakeServerDate.Offline());
            f.Rules.Claim("2026-08-01");
            f.Rules.Claim("2026-09-20");
            f.Rules.Claim("2026-10-19");

            Assert.That(f.Record.OfflineClaims.Count, Is.EqualTo(2), "el 1 de agosto queda a más de 31 días del 19 de octubre");
            Assert.That(f.Record.OfflineClaims[0].DateKey, Is.EqualTo("2026-09-20"));
        }

        [Test]
        public void OfflineClaims_OldEntriesCanNoLongerBeVoided_RQ12b()
        {
            var f = new DailyFixture(FakeServerDate.Offline(), DailyReconcileMode.VoidOfflineAfterServerDate);
            f.Rules.Claim("2026-10-19");
            f.Rules.Claim("2026-12-01");   // reloj adelantado sin red: el 19-10 queda fuera de la ventana

            Assert.That(f.Record.OfflineClaims.Count, Is.EqualTo(1));
            Assert.That(f.Record.OfflineClaims[0].DateKey, Is.EqualTo("2026-12-01"));
        }

        [Test]
        public void Reconcile_NothingToDo_DoesNotWrite()
        {
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.Zero));
            int writes = f.Store.WriteCount;
            int failed = f.Store.FailedWriteCount;

            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.Ran, Is.True, "hubo hora de servidor fiable");
            Assert.That(result.Total, Is.EqualTo(0));
            Assert.That(f.Store.WriteCount, Is.EqualTo(writes), "sin cambios no se guarda");
            Assert.That(f.Store.FailedWriteCount, Is.EqualTo(failed));
        }

        [Test]
        public void Reconcile_WithSomethingToDo_StillWrites()
        {
            var f = new DailyFixture(FakeServerDate.Offline());
            f.Rules.Claim("2026-10-19");
            int writes = f.Store.WriteCount;

            f.Server.GoOnline(TimeSpan.Zero);
            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.OfflineKept, Is.EqualTo(1));
            Assert.That(f.Store.WriteCount, Is.EqualTo(writes + 1));
            Assert.That(f.Store.Persisted.OfflineClaims, Is.Empty);
        }
    }
}
