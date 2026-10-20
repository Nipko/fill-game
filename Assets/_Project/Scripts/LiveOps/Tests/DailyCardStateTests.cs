#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Estado «Sellado» de la tarjeta (nota §8) tras anular un cobro pendiente (caso de la revisión de R7b): el sello se quita y
    /// <c>LastClaimedDate</c> no retrocede, así que el día del cobro anulado la tarjeta dice «Jugar» (sin premio), no «Sellado».
    /// También <see cref="DailyChallengeRecord.IsStamped"/>.
    /// </summary>
    public sealed class DailyCardStateTests
    {
        [Test]
        public void VoidedPendingClaim_TheCardIsNotSealedOnThatDay()
        {
            // Reloj 2 días adelantado y con red: cobra el 21 cuando en el servidor es el 19 → pendiente.
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(-48)), localStart: new DateTime(2026, 10, 21, 10, 0, 0));
            Assert.That(f.WinToday(), Is.EqualTo(ClaimOutcome.Pending));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.PendingValidation));

            f.NextDay(-2);                      // corrige el reloj: la reconciliación anula el 21
            f.Server.GoOnline(TimeSpan.Zero);
            Assert.That(f.Rules.Reconcile().Voided, Is.EqualTo(1));

            f.NextDay(2);                       // llega de verdad el 21
            Assert.That(f.Today, Is.EqualTo("2026-10-21"));
            Assert.That(f.Record.LastClaimedDate, Is.EqualTo("2026-10-21"));
            Assert.That(f.Record.IsStamped("2026-10-21"), Is.False);
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play), "sin sello no está sellado");

            Assert.That(f.WinToday(), Is.EqualTo(ClaimOutcome.NoReward), "se juega sin premio: la fecha ya se reclamó");
            Assert.That(f.Seeds, Is.EqualTo(0));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play));
        }

        [Test]
        public void ConfirmedClaim_IsSealed()
        {
            var f = new DailyFixture();
            f.WinToday();

            Assert.That(f.Record.IsStamped(f.Today), Is.True);
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Sealed));
        }

        [Test]
        public void IsStamped_ChecksMonthAndDay()
        {
            var r = new DailyChallengeRecord { StampsMonth = "2026-10" };
            r.StampDays.Add(1);
            r.StampDays.Add(19);
            r.StampDays.Add(31);

            Assert.That(r.IsStamped("2026-10-01"), Is.True);
            Assert.That(r.IsStamped("2026-10-19"), Is.True);
            Assert.That(r.IsStamped("2026-10-31"), Is.True);
            Assert.That(r.IsStamped("2026-10-02"), Is.False);
            Assert.That(r.IsStamped("2026-11-19"), Is.False, "otro mes");
            Assert.That(r.IsStamped("2025-10-19"), Is.False, "otro año");
            Assert.That(r.IsStamped(null), Is.False);
            Assert.That(r.IsStamped(""), Is.False);
            Assert.That(r.IsStamped("2026-10-1"), Is.False);
            Assert.That(r.IsStamped("2026-10-x9"), Is.False);
            Assert.That(new DailyChallengeRecord().IsStamped("2026-10-19"), Is.False, "sin sellos");
        }
    }
}
