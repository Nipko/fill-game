#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Piezas de R7b sin reglas: <see cref="InMemoryDailyChallengeStore"/> (patrón Save + PendingSave + Flush de
    /// <c>ILevelStateStore</c>), copia de <see cref="DailyChallengeRecord"/> y <see cref="FakeServerDate"/> (08 §10.1).
    /// </summary>
    public sealed class DailyChallengeStoreTests
    {
        [Test]
        public void Load_IsNeverNull_AndIsTheLiveInstance()
        {
            var store = new InMemoryDailyChallengeStore();

            DailyChallengeRecord a = store.Load();
            a.StreakDays = 3;

            Assert.That(a, Is.Not.Null);
            Assert.That(store.Load(), Is.SameAs(a));
            Assert.That(store.Persisted.StreakDays, Is.EqualTo(0), "sin Save no llega al disco");
            Assert.That(store.PendingSave, Is.False);
        }

        [Test]
        public void Save_WritesAnIndependentCopy()
        {
            var store = new InMemoryDailyChallengeStore();
            DailyChallengeRecord r = store.Load();
            r.LastClaimedDate = "2026-10-19";
            r.StampDays.Add(19);

            store.Save(r);
            r.StampDays.Add(20);

            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(store.Persisted.LastClaimedDate, Is.EqualTo("2026-10-19"));
            Assert.That(store.Persisted.StampDays, Is.EqualTo(new[] { 19 }));
        }

        [Test]
        public void FailedWrite_StaysPending_UntilFlush()
        {
            var store = new InMemoryDailyChallengeStore { FailWrites = true };
            DailyChallengeRecord r = store.Load();
            r.StreakDays = 5;

            store.Save(r);
            Assert.That(store.PendingSave, Is.True);
            Assert.That(store.FailedWriteCount, Is.EqualTo(1));
            Assert.That(store.Flush(), Is.False, "sigue sin poder escribir");
            Assert.That(store.Load().StreakDays, Is.EqualTo(5), "el cambio sigue en memoria");

            store.FailWrites = false;
            Assert.That(store.Flush(), Is.True);
            Assert.That(store.PendingSave, Is.False);
            Assert.That(store.Persisted.StreakDays, Is.EqualTo(5));
            Assert.That(store.Flush(), Is.True, "nada pendiente");
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void Reopen_LosesWhatWasNotWritten()
        {
            var store = new InMemoryDailyChallengeStore();
            store.Load().StreakDays = 2;
            store.Save(store.Load());
            store.FailuresLeft = 1;
            store.Load().StreakDays = 9;
            store.Save(store.Load());

            store.Reopen();

            Assert.That(store.Load().StreakDays, Is.EqualTo(2));
            Assert.That(store.PendingSave, Is.False);
        }

        [Test]
        public void Initial_IsCopied()
        {
            var initial = new DailyChallengeRecord { StreakDays = 4 };
            var store = new InMemoryDailyChallengeStore(initial);
            initial.StreakDays = 1;

            Assert.That(store.Load().StreakDays, Is.EqualTo(4));
            Assert.That(store.Load(), Is.Not.SameAs(initial));
        }

        [Test]
        public void Record_Clone_IsDeep()
        {
            var r = new DailyChallengeRecord
            {
                LastDate = "2026-10-19", Result = DailyResult.Fail, StreakDays = 3, LastClaimedDate = "2026-10-18",
                StampsMonth = "2026-10",
                InProgress = new DailyPlayInProgress { DateKey = "2026-10-19", Band = 2, BoardId = "d2_n0291" },
            };
            r.PendingClaims.Add(new DailyClaimEntry("2026-10-17", 2));
            r.OfflineClaims.Add(new DailyClaimEntry("2026-10-18", 3));
            r.StampDays.Add(18);
            r.InProgress.State.Pours = 4;
            r.InProgress.State.RevealedCuias.Add(1);

            DailyChallengeRecord c = r.Clone();
            r.PendingClaims.Clear();
            r.OfflineClaims.Clear();
            r.StampDays.Clear();
            r.InProgress.State.Pours = 0;
            r.InProgress.State.RevealedCuias.Clear();

            Assert.That(c.LastDate, Is.EqualTo("2026-10-19"));
            Assert.That(c.Result, Is.EqualTo(DailyResult.Fail));
            Assert.That(c.StreakDays, Is.EqualTo(3));
            Assert.That(c.LastClaimedDate, Is.EqualTo("2026-10-18"));
            Assert.That(c.StampsMonth, Is.EqualTo("2026-10"));
            Assert.That(c.PendingClaims, Is.EqualTo(new[] { new DailyClaimEntry("2026-10-17", 2) }));
            Assert.That(c.OfflineClaims, Is.EqualTo(new[] { new DailyClaimEntry("2026-10-18", 3) }));
            Assert.That(c.StampDays, Is.EqualTo(new[] { 18 }));
            Assert.That(c.InProgress!.BoardId, Is.EqualTo("d2_n0291"));
            Assert.That(c.InProgress.Band, Is.EqualTo(2));
            Assert.That(c.InProgress.State.Pours, Is.EqualTo(4));
            Assert.That(c.InProgress.State.RevealedCuias, Is.EqualTo(new[] { 1 }));
            Assert.That(c.IsPending("2026-10-17"), Is.True);
            Assert.That(c.IsPending("2026-10-18"), Is.False);
        }

        [Test]
        public void Record_CloneCopiesEveryProperty()
        {
            // Guarda contra campos nuevos que la copia olvide (la del registro y la de level_state, que replica
            // AF.GameFlow.LevelStateSnapshot.Copy porque GameFlow no es visible desde AF.LiveOps).
            var copiedRecord = new HashSet<string>
            {
                "LastDate", "Result", "StreakDays", "LastClaimedDate", "PendingClaims", "OfflineClaims", "StampsMonth", "StampDays",
                "InProgress",
            };
            var copiedPlay = new HashSet<string> { "DateKey", "Band", "BoardId", "State" };
            var copiedState = new HashSet<string>
            {
                "Level", "Seed", "Variant", "FailsLevel", "Pours", "ExtraCuias", "RescuesLevel", "UndoFree", "RevealedCuias", "History",
                "Stuck",
            };

            AssertCovers(typeof(DailyChallengeRecord), copiedRecord);
            AssertCovers(typeof(DailyPlayInProgress), copiedPlay);
            AssertCovers(typeof(LevelStateData), copiedState);

            var state = new LevelStateData
            {
                Level = 3, Seed = "s", Variant = "plus_empty", FailsLevel = 1, Pours = 2, ExtraCuias = 1, RescuesLevel = 1, UndoFree = 2,
                History = "h", Stuck = true,
            };
            state.RevealedCuias.Add(5);
            LevelStateData copy = new DailyPlayInProgress { State = state }.Clone().State;
            foreach (PropertyInfo p in typeof(LevelStateData).GetProperties())
            {
                object? a = p.GetValue(state);
                object? b = p.GetValue(copy);
                if (a is List<int> list)
                {
                    Assert.That(b, Is.EqualTo(list).And.Not.SameAs(list), p.Name);
                }
                else
                {
                    Assert.That(b, Is.EqualTo(a), p.Name);
                }
            }
        }

        [Test]
        public void ClaimEntry_ClosesStreak_EverySeven()
        {
            Assert.That(new DailyClaimEntry("2026-10-19", 6).ClosesStreak, Is.False);
            Assert.That(new DailyClaimEntry("2026-10-19", 7).ClosesStreak, Is.True);
            Assert.That(new DailyClaimEntry("2026-10-19", 8).ClosesStreak, Is.False);
            Assert.That(new DailyClaimEntry("2026-10-19", 14).ClosesStreak, Is.True);
            Assert.That(new DailyClaimEntry("2026-10-19", 0).ClosesStreak, Is.False);
        }

        [Test]
        public void DailyResult_Ids_AreThoseOf08_8_2()
        {
            Assert.That(DailyResult.Win.ToId(), Is.EqualTo("win"));
            Assert.That(DailyResult.Fail.ToId(), Is.EqualTo("fail"));
            Assert.That(DailyResult.Abandon.ToId(), Is.EqualTo("abandon"));
            Assert.That(DailyResult.None.ToId(), Is.EqualTo("none"));
        }

        // ---- FakeServerDate ----

        [Test]
        public void FakeServerDate_OfflineAndOnline()
        {
            FakeServerDate server = FakeServerDate.Offline();
            Assert.That(server.IsOnline, Is.False);
            Assert.That(server.TryGetServerOffset(out TimeSpan off), Is.False);
            Assert.That(off, Is.EqualTo(TimeSpan.Zero));

            server.GoOnline(TimeSpan.FromHours(-30));
            Assert.That(server.TryGetServerOffset(out off), Is.True);
            Assert.That(off, Is.EqualTo(TimeSpan.FromHours(-30)));

            server.GoOffline();
            Assert.That(server.TryGetServerOffset(out _), Is.False);

            IServerDate online = FakeServerDate.Online(TimeSpan.FromMinutes(5));
            Assert.That(online.TryGetServerOffset(out off), Is.True);
            Assert.That(off, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        private static void AssertCovers(Type type, HashSet<string> copied)
        {
            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(copied.Contains(p.Name), Is.True, type.Name + "." + p.Name + " no está en la copia");
            }
        }
    }
}
