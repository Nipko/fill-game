#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// <c>reclamar_reto</c> (06 §4.1; nota §7.1), anti-reloj (08 §10.1), racha (R-Q7), sellos del mes (R-Q5), reconciliación
    /// (nota §7.2; R-Q12 en sus dos ramas) y racha mostrada (nota §7.3). Cartera real de <c>AF.Economy</c>.
    /// </summary>
    public sealed class DailyChallengeRulesTests
    {
        // ---- Primer cobro, la misma fecha y el reloj atrasado ----

        [Test]
        public void Claim_FirstTime_PaysTheRewardAndStampsTheDay()
        {
            var f = new DailyFixture();

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Confirmed));

            Assert.That(f.Seeds, Is.EqualTo(60), "live_daily_challenge_reward.seeds (08 §9)");
            Assert.That(f.ShuffleGained, Is.EqualTo(0));
            DailyChallengeRecord r = f.Record;
            Assert.That(r.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(r.LastDate, Is.EqualTo("2026-10-19"));
            Assert.That(r.Result, Is.EqualTo(DailyResult.Win));
            Assert.That(r.StreakDays, Is.EqualTo(1));
            Assert.That(r.StampsMonth, Is.EqualTo("2026-10"));
            Assert.That(r.StampDays, Is.EqualTo(new[] { 19 }));
            Assert.That(f.Rules.LastClaim.SeedsPaid, Is.EqualTo(60));
            Assert.That(f.Store.Persisted.LastClaimedDate, Is.EqualTo("2026-10-19"), "se guardó");
        }

        [Test]
        public void Claim_SameDateTwice_PaysOnce()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-10-19");

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.NoReward));

            Assert.That(f.Seeds, Is.EqualTo(60));
            Assert.That(f.Record.StreakDays, Is.EqualTo(1), "la racha no cambia");
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 19 }));
            Assert.That(f.Rules.LastClaim.Outcome, Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(f.Rules.LastClaim.SeedsPaid, Is.EqualTo(0));
        }

        [Test]
        public void Claim_ClockBack_NoReward()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-10-19");
            f.NextDay(-2);   // el jugador atrasa el reloj dos días

            Assert.That(f.Today, Is.EqualTo("2026-10-17"));
            Assert.That(f.Rules.Claim(f.Today), Is.EqualTo(ClaimOutcome.NoReward), "fecha ≤ ultima_fecha_reclamada (06 §4.1)");
            Assert.That(f.Rules.Claim("2026-10-18"), Is.EqualTo(ClaimOutcome.NoReward));

            Assert.That(f.Seeds, Is.EqualTo(60));
            Assert.That(f.Record.LastClaimedDate, Is.EqualTo("2026-10-19"), "no retrocede");
            Assert.That(f.Record.StreakDays, Is.EqualTo(1));
        }

        // ---- Racha (R-Q7) ----

        [Test]
        public void Claim_NextDay_StreakTwo()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-10-19");
            f.NextDay();

            Assert.That(f.Rules.Claim(f.Today), Is.EqualTo(ClaimOutcome.Confirmed));

            Assert.That(f.Record.StreakDays, Is.EqualTo(2));
            Assert.That(f.Seeds, Is.EqualTo(120));
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 19, 20 }));
        }

        [Test]
        public void Claim_OneDayGap_StreakBackToOne()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-10-19");
            f.Rules.Claim("2026-10-20");

            Assert.That(f.Rules.Claim("2026-10-22"), Is.EqualTo(ClaimOutcome.Confirmed));

            Assert.That(f.Record.StreakDays, Is.EqualTo(1), "un día sin ganar rompe la racha (R-Q7)");
            Assert.That(f.Seeds, Is.EqualTo(180));
        }

        [Test]
        public void Claim_SevenInARow_GivesAPororoca_AndFourteenAnother()
        {
            var f = new DailyFixture();
            var day = new DateTime(2026, 10, 19);
            for (int i = 1; i <= 14; i++)
            {
                Assert.That(f.Rules.Claim(ClockExtensions.FormatDateKey(day.AddDays(i - 1))), Is.EqualTo(ClaimOutcome.Confirmed));
                int expected = i / DailyChallengeRules.StreakLength;
                Assert.That(f.ShuffleGained, Is.EqualTo(expected), "día " + i);
                Assert.That(f.Rules.LastClaim.ShufflePaid, Is.EqualTo(i % 7 == 0 ? 1 : 0), "día " + i);
            }

            Assert.That(f.Record.StreakDays, Is.EqualTo(14));
            Assert.That(f.Seeds, Is.EqualTo(14 * 60));
        }

        [Test]
        public void Claim_StreakRewardThatIsNotShuffle_PaysNothingAndSaysSo()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            config.LiveDailyStreak7Reward = "extra_cuia";
            var f = new DailyFixture(config: config);
            var day = new DateTime(2026, 10, 19);
            for (int i = 0; i < 7; i++)
            {
                f.Rules.Claim(ClockExtensions.FormatDateKey(day.AddDays(i)));
            }

            Assert.That(f.ShuffleGained, Is.EqualTo(0), "no se inventa un premio que 08 §9 no define");
            Assert.That(f.Wallet.Stock.Get(AF.Economy.BoosterKind.ExtraCuia), Is.EqualTo(new DailyFixture().Wallet.Stock.Get(AF.Economy.BoosterKind.ExtraCuia)));
            Assert.That(f.Rules.LastClaim.StreakRewardSkipped, Is.EqualTo("extra_cuia"));
            Assert.That(f.Rules.LastClaim.ShufflePaid, Is.EqualTo(0));
            Assert.That(f.Seeds, Is.EqualTo(7 * 60), "las semillas se pagan igual");
        }

        // ---- Sellos del mes (R-Q5) ----

        [Test]
        public void Claim_MonthChange_StartsANewStampMonth()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-10-30");
            f.Rules.Claim("2026-10-31");
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 30, 31 }));

            f.Rules.Claim("2026-11-01");

            Assert.That(f.Record.StampsMonth, Is.EqualTo("2026-11"));
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 1 }));
            Assert.That(f.Record.StreakDays, Is.EqualTo(3), "la racha cruza el mes");
        }

        [Test]
        public void Claim_YearChange_StreakContinues()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-12-31");
            f.Rules.Claim("2027-01-01");

            Assert.That(f.Record.StreakDays, Is.EqualTo(2));
            Assert.That(f.Record.StampsMonth, Is.EqualTo("2027-01"));
        }

        // ---- Anti-reloj con y sin red (08 §10.1; sec_clock_max_drift_h = 24) ----

        [Test]
        public void Claim_OnlineWithDriftOver24h_IsPendingAndPaysNothing()
        {
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(-25)));

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Pending));

            Assert.That(f.Seeds, Is.EqualTo(0));
            Assert.That(f.ShuffleGained, Is.EqualTo(0));
            DailyChallengeRecord r = f.Record;
            Assert.That(r.IsPending("2026-10-19"), Is.True);
            Assert.That(r.LastClaimedDate, Is.EqualTo("2026-10-19"), "también si queda pendiente (06 §4.1)");
            Assert.That(r.StampDays, Is.EqualTo(new[] { 19 }));
            Assert.That(r.OfflineClaims, Is.Empty);
            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.NoReward), "ni dos veces la misma fecha");
        }

        [TestCase(-25)]
        [TestCase(25)]
        [TestCase(-24 * 3)]
        public void Claim_OnlineWithDriftOver24h_EitherDirection_IsPending(int hours)
        {
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(hours)));

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Pending));
        }

        [TestCase(0)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(-24)]
        public void Claim_OnlineWithDriftUpTo24h_IsConfirmed(int hours)
        {
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(hours)));

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Confirmed));

            Assert.That(f.Seeds, Is.EqualTo(60));
            Assert.That(f.Record.PendingClaims, Is.Empty);
            Assert.That(f.Record.OfflineClaims, Is.Empty, "con red no es un cobro sin red");
        }

        [Test]
        public void Claim_Offline_IsConfirmedAndRememberedForReconciliation()
        {
            var f = new DailyFixture(FakeServerDate.Offline());

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Confirmed));

            Assert.That(f.Seeds, Is.EqualTo(60), "sin red se acepta la fecha local (08 §10.1)");
            Assert.That(f.Record.OfflineClaims, Is.EqualTo(new[] { new DailyClaimEntry("2026-10-19", 1) }));
        }

        // ---- Fallo de escritura entre registrar y pagar ----

        [Test]
        public void Claim_StoreWriteFails_NeverPaysTheSameDateTwice()
        {
            var f = new DailyFixture();
            f.Store.FailWrites = true;

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(f.Store.PendingSave, Is.True);
            Assert.That(f.Store.Persisted.LastClaimedDate, Is.Null, "no llegó al disco");
            Assert.That(f.Seeds, Is.EqualTo(60));

            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.NoReward), "el registro sigue en memoria");
            Assert.That(f.Seeds, Is.EqualTo(60));

            f.Store.FailWrites = false;
            Assert.That(f.Store.Flush(), Is.True);
            Assert.That(f.Store.PendingSave, Is.False);
            Assert.That(f.Store.Persisted.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(f.Seeds, Is.EqualTo(60));
        }

        [Test]
        public void Claim_StoreFailsOnce_TheNextSaveCarriesTheClaim()
        {
            var f = new DailyFixture();
            f.Store.FailuresLeft = 1;
            f.Rules.Claim("2026-10-19");
            Assert.That(f.Store.PendingSave, Is.True);

            f.Rules.Claim("2026-10-20");

            Assert.That(f.Store.PendingSave, Is.False);
            Assert.That(f.Store.Persisted.StampDays, Is.EqualTo(new[] { 19, 20 }));
            Assert.That(f.Seeds, Is.EqualTo(120));
        }

        [Test]
        public void Claim_RejectsWhatIsNotADateKey()
        {
            var f = new DailyFixture();
            foreach (string bad in new[] { "", "2026-10-1", "19/10/2026", "2026-13-01", "2026-02-30" })
            {
                Assert.Throws<ArgumentException>(() => f.Rules.Claim(bad), bad);
            }

            Assert.Throws<ArgumentException>(() => f.Rules.Claim(null!));
            Assert.That(f.Seeds, Is.EqualTo(0));
        }

        // ---- Reconciliación (nota §7.2) ----

        [Test]
        public void Reconcile_OfflineOrStillDrifting_DoesNothing()
        {
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(-48)));
            f.Rules.Claim("2026-10-19");

            f.Server.GoOffline();
            Assert.That(f.Rules.Reconcile().Ran, Is.False);
            f.Server.GoOnline(TimeSpan.FromHours(-48));
            Assert.That(f.Rules.Reconcile().Ran, Is.False, "con el reloj aún desfasado no hay fecha de servidor fiable");

            Assert.That(f.Record.IsPending("2026-10-19"), Is.True);
            Assert.That(f.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void Reconcile_PendingOnOrBeforeServerDate_IsPaid()
        {
            // El reloj del dispositivo va 2 días atrasado: hoy en el dispositivo es 17, en el servidor 19.
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(48)), localStart: new DateTime(2026, 10, 17, 10, 0, 0));
            Assert.That(f.Rules.Claim(f.Today), Is.EqualTo(ClaimOutcome.Pending));

            f.NextDay(2);                       // el jugador corrige el reloj
            f.Server.GoOnline(TimeSpan.Zero);
            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.Ran, Is.True);
            Assert.That(result.Confirmed, Is.EqualTo(1));
            Assert.That(result.Voided, Is.EqualTo(0));
            Assert.That(result.SeedsPaid, Is.EqualTo(60));
            Assert.That(f.Seeds, Is.EqualTo(60));
            Assert.That(f.Record.PendingClaims, Is.Empty);
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 17 }));
            Assert.That(f.Store.Persisted.PendingClaims, Is.Empty, "se guardó");
            Assert.That(f.Rules.Reconcile().Total, Is.EqualTo(0), "no paga dos veces");
            Assert.That(f.Seeds, Is.EqualTo(60));
        }

        [Test]
        public void Reconcile_PendingThatClosedAStreak_PaysThePororocaToo()
        {
            var initial = new DailyChallengeRecord { LastClaimedDate = "2026-10-18", StreakDays = 6 };
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(30)), initial: initial);
            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.Pending));
            Assert.That(f.Record.StreakDays, Is.EqualTo(7));
            Assert.That(f.ShuffleGained, Is.EqualTo(0));

            f.Server.GoOnline(TimeSpan.FromHours(1));
            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.ShufflePaid, Is.EqualTo(1));
            Assert.That(f.ShuffleGained, Is.EqualTo(1));
            Assert.That(f.Seeds, Is.EqualTo(60));
        }

        [Test]
        public void Reconcile_PendingAfterServerDate_IsVoidedWithoutSeeds()
        {
            // El reloj del dispositivo va 2 días adelantado: cobra el 21 cuando en el servidor es el 19.
            var initial = new DailyChallengeRecord { LastClaimedDate = "2026-10-18", StreakDays = 3 };
            initial.StampDays.Add(18);
            initial.StampsMonth = "2026-10";
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(-48)), initial: initial,
                localStart: new DateTime(2026, 10, 21, 10, 0, 0));
            Assert.That(f.Rules.Claim(f.Today), Is.EqualTo(ClaimOutcome.Pending));

            f.NextDay(-2);                      // corrige el reloj
            f.Server.GoOnline(TimeSpan.Zero);
            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.Voided, Is.EqualTo(1));
            Assert.That(result.SeedsPaid, Is.EqualTo(0));
            Assert.That(f.Seeds, Is.EqualTo(0));
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 18 }), "se quita el sello del 21");
            Assert.That(f.Record.StreakDays, Is.EqualTo(0), "la racha se rompe");
            Assert.That(f.Record.LastClaimedDate, Is.EqualTo("2026-10-21"), "no retrocede: el 19-21 no se cobran otra vez");
            Assert.That(f.Rules.Claim("2026-10-19"), Is.EqualTo(ClaimOutcome.NoReward));
        }

        [Test]
        public void Reconcile_RQ12a_KeepsOfflineClaims()
        {
            DailyFixture f = OfflineClaimWithClockAhead(DailyReconcileMode.KeepOfflineClaims);

            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(f.Rules.ReconcileMode, Is.EqualTo(DailyReconcileMode.KeepOfflineClaims), "default [P]");
            Assert.That(result.Ran, Is.True);
            Assert.That(result.OfflineKept, Is.EqualTo(1));
            Assert.That(result.OfflineVoided, Is.EqualTo(0));
            Assert.That(f.Seeds, Is.EqualTo(60), "no se retira nada");
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 22 }));
            Assert.That(f.Record.StreakDays, Is.EqualTo(1));
            Assert.That(f.Record.OfflineClaims, Is.Empty);
        }

        [Test]
        public void Reconcile_RQ12b_VoidsOfflineClaimsAfterServerDate_WithoutTakingSeeds()
        {
            DailyFixture f = OfflineClaimWithClockAhead(DailyReconcileMode.VoidOfflineAfterServerDate);

            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.OfflineVoided, Is.EqualTo(1));
            Assert.That(result.OfflineKept, Is.EqualTo(0));
            Assert.That(f.Seeds, Is.EqualTo(60), "las semillas ya pagadas no se retiran");
            Assert.That(f.Record.StampDays, Is.Empty, "se anula el sello");
            Assert.That(f.Record.StreakDays, Is.EqualTo(0), "y la racha");
            Assert.That(f.Record.LastClaimedDate, Is.EqualTo("2026-10-22"), "no retrocede");
            Assert.That(f.Record.OfflineClaims, Is.Empty);
        }

        [Test]
        public void Reconcile_RQ12b_KeepsOfflineClaimsOnOrBeforeServerDate()
        {
            var f = new DailyFixture(FakeServerDate.Offline(), DailyReconcileMode.VoidOfflineAfterServerDate);
            f.Rules.Claim("2026-10-18");
            f.Rules.Claim("2026-10-19");

            f.Server.GoOnline(TimeSpan.Zero);
            DailyReconcileResult result = f.Rules.Reconcile();

            Assert.That(result.OfflineKept, Is.EqualTo(2));
            Assert.That(result.OfflineVoided, Is.EqualTo(0));
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 18, 19 }));
            Assert.That(f.Record.StreakDays, Is.EqualTo(2));
        }

        // ---- Racha mostrada «{n}/7» (nota §7.3) ----

        [Test]
        public void StreakShown_FollowsTheNote()
        {
            var f = new DailyFixture();
            Assert.That(f.Rules.StreakShown(), Is.EqualTo(0), "sin cobros");

            var day = new DateTime(2026, 10, 13);
            for (int i = 0; i < 7; i++)
            {
                f.Rules.Claim(ClockExtensions.FormatDateKey(day.AddDays(i)));   // del 13 al 19 = hoy
            }

            Assert.That(f.Rules.StreakShown(), Is.EqualTo(7), "el día de la Pororoca se ve 7/7");

            f.NextDay();                                          // 20, sin sellar
            Assert.That(f.Rules.StreakShown(), Is.EqualTo(0), "7 mod 7");
            f.Rules.Claim(f.Today);
            Assert.That(f.Rules.StreakShown(), Is.EqualTo(1), "8 → 1/7");

            f.NextDay();                                          // 21, sin sellar: racha viva
            Assert.That(f.Rules.StreakShown(), Is.EqualTo(1));
            f.NextDay();                                          // 22: ayer no se ganó
            Assert.That(f.Rules.StreakShown(), Is.EqualTo(0));
        }

        [Test]
        public void StreakShown_ClockBehindTheLastClaim_IsZero()
        {
            var f = new DailyFixture();
            f.Rules.Claim("2026-10-19");
            f.NextDay(-3);

            Assert.That(f.Rules.StreakShown(), Is.EqualTo(0));
        }

        // Cobro sin red el 22 con el reloj adelantado 3 días; luego el jugador lo corrige y vuelve la red (servidor = 19).
        private static DailyFixture OfflineClaimWithClockAhead(DailyReconcileMode mode)
        {
            var f = new DailyFixture(FakeServerDate.Offline(), mode, localStart: new DateTime(2026, 10, 22, 10, 0, 0));
            Assert.That(f.Rules.Claim(f.Today), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(f.Seeds, Is.EqualTo(60));

            f.NextDay(-3);
            f.Server.GoOnline(TimeSpan.Zero);
            return f;
        }
    }
}
