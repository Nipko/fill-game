#nullable enable
using System;
using AF.Levels;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Fachada del Reto (nota §3 y §8 [P]): estados de la tarjeta en su orden, empezar con fecha y banda congeladas, la partida que
    /// cruza la medianoche (R-Q3), reanudar el mismo día y descartar otro día (R-Q4), salir sin ganar dejándola reanudable (R-Q16),
    /// el <c>fail</c> una vez por día sin victoria (R-Q9, R-Q15) y reclamar.
    /// </summary>
    public sealed class DailyChallengeServiceTests
    {
        // ---- Estados de la tarjeta (nota §8) ----

        [Test]
        public void CardState_Hidden_WhenSwitchedOff_Locked_OrWithoutPool()
        {
            var f = new DailyFixture();
            Assert.That(f.Service.CardState(9), Is.EqualTo(DailyCardState.Hidden), "live_unlock_level_daily = 10");
            Assert.That(f.Service.CardState(10), Is.EqualTo(DailyCardState.Play));

            f.Config.BizFeatureDailyChallenge = false;
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Hidden), "biz_feature_daily_challenge");

            var noPool = new DailyChallengeService(f.Store, f.Rules, f.Clock, DailyChallengeConfig.Defaults, null);
            Assert.That(noPool.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Hidden), "pool ausente");
            Assert.Throws<InvalidOperationException>(() => noPool.Start(DailyFixture.MaxLevel));
        }

        [Test]
        public void CardState_GoesPlay_InProgress_Sealed_InThatOrder()
        {
            var f = new DailyFixture();
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play));

            f.Service.Start(DailyFixture.MaxLevel);
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress));

            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Sealed));

            // Reintentos ilimitados (06 §4.1): rejugar hoy pone la tarjeta en curso, que va antes que Sellado.
            f.Service.Start(DailyFixture.MaxLevel);
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress));
            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Sealed));
            Assert.That(f.Seeds, Is.EqualTo(60));

            f.NextDay();
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play), "el día siguiente vuelve a Jugar");
        }

        [Test]
        public void CardState_PendingValidation_GoesBeforeSealed_AndAfterInProgress()
        {
            var f = new DailyFixture(FakeServerDate.Online(TimeSpan.FromHours(-30)));
            Assert.That(f.WinToday(), Is.EqualTo(ClaimOutcome.Pending));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.PendingValidation));

            f.Service.Start(DailyFixture.MaxLevel);
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress));
            f.Service.RecordExitWithoutWin();
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress), "salir la deja reanudable (R-Q16)");
            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.NoReward), "la fecha ya está reclamada (pendiente)");
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.PendingValidation));

            f.Server.GoOnline(TimeSpan.Zero);
            f.Rules.Reconcile();
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Sealed));
            Assert.That(f.Seeds, Is.EqualTo(60));
        }

        [Test]
        public void CardState_ClockBack_PlaysWithoutReward()
        {
            var f = new DailyFixture();
            f.WinToday();
            f.NextDay(-2);

            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play), "el tablero siempre se juega");
            Assert.That(f.WinToday(), Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(f.Seeds, Is.EqualTo(60));
        }

        // ---- Empezar ----

        [Test]
        public void Start_FreezesDateBandAndBoard()
        {
            var f = new DailyFixture();

            DailyPlayInProgress play = f.Service.Start(DailyFixture.MaxLevel);

            DailySlot slot = f.Service.TodaySlot(DailyFixture.MaxLevel);
            Assert.That(slot, Is.EqualTo(new DailySlot(1, 291, null)), "2026-10-19, B1 (R7a)");
            Assert.That(play.DateKey, Is.EqualTo("2026-10-19"));
            Assert.That(play.Band, Is.EqualTo(1));
            Assert.That(play.BoardId, Is.EqualTo("d1_n0291"));
            Assert.That(play.State.Seed, Is.EqualTo("amazonfill:v1:daily:b1:d291"));
            Assert.That(f.Store.Persisted.InProgress!.BoardId, Is.EqualTo("d1_n0291"), "se guardó");
            Assert.That(f.Service.TodayBoard(DailyFixture.MaxLevel)!.Id, Is.EqualTo("d1_n0291"));
        }

        [Test]
        public void Start_BandStaysFrozen_EvenIfTheMaxLevelGrows()
        {
            var f = new DailyFixture();
            f.Service.Start(40);

            DailyPlayInProgress again = f.Service.Start(41);

            Assert.That(again.Band, Is.EqualTo(1), "la banda se congela al empezar (nota §6)");
            Assert.That(f.Service.TodayBoard(41)!.Id, Is.EqualTo("d1_n0291"));
        }

        [Test]
        public void Start_WithAnOverride_UsesTheSubstituteBoard()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            config.DailyOverrides = DailyOverrides.Parse("{\"2026-10-19\":{\"1\":\"d1_n0100\"}}");
            var f = new DailyFixture(config: config);

            Assert.That(f.Service.Start(DailyFixture.MaxLevel).BoardId, Is.EqualTo("d1_n0100"));
        }

        [Test]
        public void Start_WhenNotAvailable_Throws()
        {
            var f = new DailyFixture();
            Assert.Throws<InvalidOperationException>(() => f.Service.Start(9));
            Assert.That(f.Record.InProgress, Is.Null);
        }

        // ---- R-Q3: manda la fecha de inicio ----

        [Test]
        public void StartedAt2355_WonAt0005_ClaimsTheStartDate()
        {
            var f = new DailyFixture(localStart: new DateTime(2026, 10, 19, 23, 55, 0));
            DailyPlayInProgress play = f.Service.Start(DailyFixture.MaxLevel);
            Assert.That(play.DateKey, Is.EqualTo("2026-10-19"));

            f.Clock.Advance(TimeSpan.FromMinutes(10));
            Assert.That(f.Today, Is.EqualTo("2026-10-20"));

            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(f.Record.LastClaimedDate, Is.EqualTo("2026-10-19"), "R-Q3");
            Assert.That(f.Record.StampDays, Is.EqualTo(new[] { 19 }));
            Assert.That(f.Seeds, Is.EqualTo(60));

            // El 20 sigue disponible con su propio tablero y su premio.
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play));
            Assert.That(f.Service.Start(DailyFixture.MaxLevel).BoardId, Is.EqualTo("d1_n0292"));
            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.Confirmed));
            Assert.That(f.Record.StreakDays, Is.EqualTo(2));
            Assert.That(f.Seeds, Is.EqualTo(120));
        }

        // ---- R-Q4: reanudar el mismo día, descartar otro día ----

        [Test]
        public void Reopen_SameDay_Resumes()
        {
            var f = new DailyFixture();
            f.Service.Start(DailyFixture.MaxLevel);
            var state = new LevelStateData { Seed = "amazonfill:v1:daily:b1:d291", Pours = 7, History = "abc" };
            f.Service.SaveState(state);

            f.Store.Reopen();
            f.Clock.Advance(TimeSpan.FromHours(3));

            Assert.That(f.Service.DiscardStale(), Is.Null, "es de hoy: no se descarta");
            DailyPlayInProgress? resumed = f.Service.Resumable;
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed!.State.Pours, Is.EqualTo(7));
            Assert.That(resumed.State.History, Is.EqualTo("abc"));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress));
            Assert.That(f.Service.Start(DailyFixture.MaxLevel).State.Pours, Is.EqualTo(7), "Start reanuda");
        }

        [Test]
        public void Reopen_NextDay_DiscardsWithFail_OnceForThatDay()
        {
            var f = new DailyFixture();
            var ended = new System.Collections.Generic.List<DailyPlayEnd>();
            f.Service.DayEnded += e => ended.Add(e);
            f.Service.Start(DailyFixture.MaxLevel);

            f.Store.Reopen();
            f.NextDay();

            DailyPlayEnd? discarded = f.Service.DiscardStale();
            Assert.That(discarded, Is.Not.Null);
            Assert.That(discarded!.Value.Result, Is.EqualTo(DailyResult.Fail), "el 19 terminó sin victoria (R-Q15)");
            Assert.That(discarded.Value.Result.ToId(), Is.EqualTo("fail"));
            Assert.That(discarded.Value.IsReported, Is.True);
            Assert.That(discarded.Value.DateKey, Is.EqualTo("2026-10-19"));
            Assert.That(discarded.Value.BoardId, Is.EqualTo("d1_n0291"));
            Assert.That(discarded.Value.StreakDays, Is.EqualTo(0), "no había racha");
            Assert.That(f.Service.LastDiscard, Is.EqualTo(discarded));
            Assert.That(ended, Is.EqualTo(new[] { discarded.Value }));
            Assert.That(f.Record.InProgress, Is.Null);
            Assert.That(f.Record.LastDate, Is.EqualTo("2026-10-19"));
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.Fail));
            Assert.That(f.Store.Persisted.InProgress, Is.Null, "se guardó");
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play));
            Assert.That(f.Seeds, Is.EqualTo(0));

            Assert.That(f.Service.DiscardStale(), Is.Null, "una sola vez");
            f.Store.Reopen();
            Assert.That(f.Service.DiscardStale(), Is.Null);
            Assert.That(ended.Count, Is.EqualTo(1));
        }

        [Test]
        public void Start_NextDayWithoutReopen_DiscardsYesterdaysPlayWithFail()
        {
            var f = new DailyFixture();
            int ended = 0;
            f.Service.DayEnded += _ => ended++;
            f.Service.Start(DailyFixture.MaxLevel);
            f.NextDay();

            DailyPlayInProgress play = f.Service.Start(DailyFixture.MaxLevel);

            Assert.That(play.DateKey, Is.EqualTo("2026-10-20"));
            Assert.That(f.Service.LastDiscard!.Value.Result, Is.EqualTo(DailyResult.Fail));
            Assert.That(ended, Is.EqualTo(1));
        }

        [Test]
        public void SeveralExitsTheSameDay_GiveOneFail_WhenTheDayEnds()
        {
            var f = new DailyFixture();
            var ended = new System.Collections.Generic.List<DailyPlayEnd>();
            f.Service.DayEnded += e => ended.Add(e);
            for (int i = 0; i < 3; i++)
            {
                f.Service.Start(DailyFixture.MaxLevel);
                f.Service.RecordExitWithoutWin();
                Assert.That(f.Service.DiscardStale(), Is.Null, "el mismo día no termina nada");
            }

            Assert.That(ended, Is.Empty, "salir no es un resultado (R-Q15)");
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.None));
            Assert.That(f.Record.LastDate, Is.Null);

            f.NextDay(3);
            f.Service.DiscardStale();
            Assert.That(ended.Count, Is.EqualTo(1), "una vez por día, aunque se reabra días después");
            Assert.That(ended[0].DateKey, Is.EqualTo("2026-10-19"));
        }

        [Test]
        public void ExitsThenWinTheSameDay_NoFail()
        {
            var f = new DailyFixture();
            int ended = 0;
            f.Service.DayEnded += _ => ended++;
            f.Service.Start(DailyFixture.MaxLevel);
            f.Service.RecordExitWithoutWin();
            f.Service.Start(DailyFixture.MaxLevel);
            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.Confirmed));

            f.NextDay();
            Assert.That(f.Service.DiscardStale(), Is.Null, "ganar cerró la partida: nada que descartar");
            Assert.That(ended, Is.EqualTo(0));
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.Win));
        }

        [Test]
        public void ADayWithoutPlay_ReportsNothing()
        {
            var f = new DailyFixture();
            int ended = 0;
            f.Service.DayEnded += _ => ended++;
            f.WinToday();
            f.NextDay(2);

            Assert.That(f.Service.DiscardStale(), Is.Null, "el 20 no se jugó: no hay fin de día que informar");
            Assert.That(ended, Is.EqualTo(0));
        }

        [Test]
        public void Fail_ReportsTheStreakItBreaks()
        {
            var f = new DailyFixture();
            f.WinToday();
            f.NextDay();
            f.WinToday();
            f.NextDay();
            f.Service.Start(DailyFixture.MaxLevel);
            f.Service.RecordExitWithoutWin();
            f.NextDay();

            DailyPlayEnd end = f.Service.DiscardStale()!.Value;

            Assert.That(end.Result, Is.EqualTo(DailyResult.Fail));
            Assert.That(end.DateKey, Is.EqualTo("2026-10-21"));
            Assert.That(end.StreakDays, Is.EqualTo(2), "la racha del 19-20 que el 21 rompe [P]");
            Assert.That(f.Rules.StreakShown(), Is.EqualTo(0));
        }

        [Test]
        public void DiscardOfAReplayOfAWonDay_ReportsNothing_AndKeepsTheWin()
        {
            var f = new DailyFixture();
            int ended = 0;
            f.Service.DayEnded += _ => ended++;
            f.WinToday();
            f.Service.Start(DailyFixture.MaxLevel);
            f.Service.RecordExitWithoutWin();
            f.Store.Reopen();
            f.NextDay();

            DailyPlayEnd end = f.Service.DiscardStale()!.Value;
            Assert.That(end.Result, Is.EqualTo(DailyResult.None));
            Assert.That(end.IsReported, Is.False);
            Assert.That(ended, Is.EqualTo(0));
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.Win));
        }

        [Test]
        public void ClockBackToADayAlreadyFailed_DoesNotReportItAgain()
        {
            var f = new DailyFixture();
            var ended = new System.Collections.Generic.List<DailyPlayEnd>();
            f.Service.DayEnded += e => ended.Add(e);
            f.Service.Start(DailyFixture.MaxLevel);
            f.NextDay();
            Assert.That(f.Service.DiscardStale()!.Value.Result, Is.EqualTo(DailyResult.Fail), "fail del 19, el 20");

            f.NextDay(-1);                      // el reloj vuelve al 19
            Assert.That(f.Service.Start(DailyFixture.MaxLevel).DateKey, Is.EqualTo("2026-10-19"));
            f.Service.RecordExitWithoutWin();
            f.NextDay();

            DailyPlayEnd end = f.Service.DiscardStale()!.Value;
            Assert.That(end.Result, Is.EqualTo(DailyResult.None), "el 19 ya se informó");
            Assert.That(ended.Count, Is.EqualTo(1), "una sola vez por día");
            Assert.That(f.Record.InProgress, Is.Null);
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.Fail));
            Assert.That(f.Record.LastDate, Is.EqualTo("2026-10-19"));
        }

        [Test]
        public void ClockBackReplayOfAWonDayThatIsNotTheLastClaimed_NoFail()
        {
            var f = new DailyFixture();
            int ended = 0;
            f.Service.DayEnded += _ => ended++;
            f.WinToday();
            f.NextDay();
            f.WinToday();

            f.NextDay(-1);                      // el reloj vuelve al 19, ganado, pero el último cobro es el 20
            Assert.That(f.Service.Start(DailyFixture.MaxLevel).DateKey, Is.EqualTo("2026-10-19"));
            f.Service.RecordExitWithoutWin();
            f.NextDay(2);

            Assert.That(f.Service.DiscardStale()!.Value.Result, Is.EqualTo(DailyResult.None));
            Assert.That(ended, Is.EqualTo(0));
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.Win));
            Assert.That(f.Record.LastDate, Is.EqualTo("2026-10-20"));
        }

        [Test]
        public void DiscardOfAFuturePlay_ClockBack_ReportsNothing()
        {
            var f = new DailyFixture();
            int ended = 0;
            f.Service.DayEnded += _ => ended++;
            f.Service.Start(DailyFixture.MaxLevel);
            f.NextDay(-1);

            DailyPlayEnd end = f.Service.DiscardStale()!.Value;

            Assert.That(end.DateKey, Is.EqualTo("2026-10-19"));
            Assert.That(end.Result, Is.EqualTo(DailyResult.None), "el 19 aún no ha terminado [P]");
            Assert.That(ended, Is.EqualTo(0));
            Assert.That(f.Record.InProgress, Is.Null, "pero se descarta (R-Q4)");
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.None));
        }

        // ---- R-Q9 con R-Q15: salir sin ganar no es aún un resultado ----

        [Test]
        public void ExitWithoutWin_RecordsNoResult()
        {
            var f = new DailyFixture();
            DailyPlayInProgress started = f.Service.Start(DailyFixture.MaxLevel);

            DailyPlayInProgress kept = f.Service.RecordExitWithoutWin();

            Assert.That(kept.DateKey, Is.EqualTo(started.DateKey));
            Assert.That(kept.BoardId, Is.EqualTo("d1_n0291"));
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.None));
            Assert.That(f.Record.LastDate, Is.Null);
            Assert.That(f.Seeds, Is.EqualTo(0));
        }

        // ---- R-Q16: salir sin ganar la deja reanudable el mismo día ----

        [Test]
        public void ExitWithoutWin_KeepsThePlayResumableTheSameDay()
        {
            var f = new DailyFixture();
            f.Service.Start(DailyFixture.MaxLevel);
            f.Service.SaveState(new LevelStateData { Seed = "amazonfill:v1:daily:b1:d291", Pours = 4, History = "h" });

            f.Service.RecordExitWithoutWin();

            Assert.That(f.Store.Persisted.InProgress, Is.Not.Null, "R-Q16: no se cierra");
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress), "«Seguir»");
            DailyPlayInProgress? resumable = f.Service.Resumable;
            Assert.That(resumable, Is.Not.Null);
            Assert.That(resumable!.State.Pours, Is.EqualTo(4));
            Assert.That(f.Service.Start(DailyFixture.MaxLevel).State.Pours, Is.EqualTo(4), "Start la reanuda");

            f.Store.Reopen();
            Assert.That(f.Service.DiscardStale(), Is.Null, "el mismo día, al reabrir, tampoco se descarta");
            Assert.That(f.Service.Resumable, Is.Not.Null);

            f.NextDay();
            Assert.That(f.Service.Resumable, Is.Null, "otro día ya no se reanuda (R-Q4)");
            Assert.That(f.Service.DiscardStale(), Is.Not.Null);
            Assert.That(f.Record.InProgress, Is.Null);
        }

        [Test]
        public void ExitFromAReplayOfAWonDay_IsNotAFail()
        {
            var f = new DailyFixture();
            f.WinToday();
            f.Service.Start(DailyFixture.MaxLevel);

            f.Service.RecordExitWithoutWin();
            Assert.That(f.Record.Result, Is.EqualTo(DailyResult.Win));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.InProgress), "la rejugada también se reanuda (R-Q16) [P]");
            Assert.That(f.Service.ClaimWin(), Is.EqualTo(ClaimOutcome.NoReward));
            Assert.That(f.Service.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Sealed));
        }

        [Test]
        public void WinAndExit_WithoutAPlay_Throw()
        {
            var f = new DailyFixture();
            Assert.Throws<InvalidOperationException>(() => f.Service.ClaimWin());
            Assert.Throws<InvalidOperationException>(() => f.Service.RecordExitWithoutWin());
            Assert.Throws<InvalidOperationException>(() => f.Service.SaveState(new LevelStateData()));
        }

        [Test]
        public void ClaimWin_ClockBack_DoesNotRewindLastDate()
        {
            var f = new DailyFixture();
            f.WinToday();
            f.NextDay(-2);

            f.WinToday();

            Assert.That(f.Record.LastDate, Is.EqualTo("2026-10-19"));
            Assert.That(f.Record.InProgress, Is.Null, "la partida se cierra igual");
        }

        [Test]
        public void TodayBoard_IsThePoolBoardOfTheSlot()
        {
            var f = new DailyFixture();
            LevelDefinition? board = f.Service.TodayBoard(201);

            Assert.That(board, Is.Not.Null);
            Assert.That(board!.Id, Is.EqualTo(DailyPool.IdOf(3, 291)));
        }
    }
}
