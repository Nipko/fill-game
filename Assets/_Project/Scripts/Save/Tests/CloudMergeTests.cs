#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Los 8 casos de la fusión con la nube (08 §11, <c>ResolveConflict</c>; 08 §14.2: «conflictos local/nube (8 casos)»; 04 §10.2;
    /// bloque B8; nota <c>docs/dev/nube-integridad.md</c> §2) sobre los campos que ya tiene el perfil: sin nube, idénticos, distinto
    /// <c>max_level</c>, mismo nivel con <c>economy_ts</c>, empate, consumibles sin sumar, campos que se fusionan (con B1a-Q1 y
    /// Q-S1) e integridad. La fusión no toca sus entradas y es determinista.
    /// </summary>
    public sealed class CloudMergeTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        private static Profile At(int maxLevel, DateTime? economyTs, int seeds)
        {
            Profile p = Profile.CreateNew(new ManualClock(T0), 5);
            p.Progress.MaxLevel = maxLevel;
            p.Progress.NextLevel = maxLevel + 1;
            p.EconomyTs = economyTs;
            p.Wallet.Seeds = seeds;
            return p;
        }

        [Test]
        public void Case1_NoCloud_KeepsTheLocal_AndAsksToUpload()
        {
            Profile local = At(40, T0, 300);
            CloudMergeResult r = CloudMerge.Resolve(local, null);
            Assert.That(r.Reason, Is.EqualTo(CloudMergeReason.NoCloud));
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Local));
            Assert.That(r.LocalChanged, Is.False);
            Assert.That(r.CloudChanged, Is.True, "sin snapshot, se sube el local");
            Assert.That(r.SyncResult, Is.EqualTo(CloudSyncResult.Ok));
            Assert.That(r.SyncResultId, Is.EqualTo("ok"));
            Assert.That(r.Merged, Is.Not.SameAs(local), "una copia: la entrada no se toca");
            Assert.That(ProfileJson.Serialize(r.Merged), Is.EqualTo(ProfileJson.Serialize(local)));
        }

        [Test]
        public void Case2_Identical_NothingToDo()
        {
            Profile local = At(40, T0, 300);
            Profile cloud = local.Clone();
            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Reason, Is.EqualTo(CloudMergeReason.Identical));
            Assert.That(r.LocalChanged || r.CloudChanged, Is.False);
            Assert.That(r.SyncResultId, Is.EqualTo("ok"));
        }

        [TestCase(50, 40, CloudMergeWinner.Local)]
        [TestCase(40, 50, CloudMergeWinner.Cloud)]
        public void Case3_DifferentMaxLevel_TheHigherWins_EvenWithAnOlderEconomyTs(int localLevel, int cloudLevel, CloudMergeWinner winner)
        {
            // El de menos nivel tiene la economía más reciente: da igual, manda max_level (08 §11).
            Profile local = At(localLevel, localLevel > cloudLevel ? T0 : T0.AddHours(5), 100);
            Profile cloud = At(cloudLevel, cloudLevel > localLevel ? T0 : T0.AddHours(5), 900);
            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Reason, Is.EqualTo(CloudMergeReason.MaxLevel));
            Assert.That(r.Winner, Is.EqualTo(winner));
            Assert.That(r.Merged.Progress.MaxLevel, Is.EqualTo(Math.Max(localLevel, cloudLevel)));
            Assert.That(r.Merged.Wallet.Seeds, Is.EqualTo(winner == CloudMergeWinner.Local ? 100 : 900), "la cartera del ganador");
            Assert.That(r.SyncResultId, Is.EqualTo(winner == CloudMergeWinner.Local ? "conflict_local_won" : "conflict_cloud_won"));
            Assert.That(r.LocalChanged, Is.EqualTo(winner == CloudMergeWinner.Cloud));
            Assert.That(r.CloudChanged, Is.EqualTo(winner == CloudMergeWinner.Local));
        }

        [Test]
        public void Case4_SameMaxLevel_TheLatestEconomyTsWins_AndNullCountsAsOldest()
        {
            Profile local = At(40, T0, 100);
            Profile cloud = At(40, T0.AddMinutes(1), 200);
            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Reason, Is.EqualTo(CloudMergeReason.EconomyTs));
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Cloud));
            Assert.That(r.Merged.Wallet.Seeds, Is.EqualTo(200));

            Profile never = At(40, null, 999);
            r = CloudMerge.Resolve(never, local);
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Cloud), "sin economy_ts pierde frente a uno con fecha");
            Assert.That(r.Merged.Wallet.Seeds, Is.EqualTo(100));
        }

        [Test]
        public void Case5_FullTie_TheLocalWins()
        {
            Profile local = At(40, T0, 100);
            Profile cloud = At(40, T0, 250);
            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Reason, Is.EqualTo(CloudMergeReason.Tie));
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Local), "[P]: a igual nivel y fecha, el del dispositivo");
            Assert.That(r.Merged.Wallet.Seeds, Is.EqualTo(100));
            Assert.That(r.CloudChanged, Is.True);
        }

        [Test]
        public void Case6_Consumables_AreNeverSummed_AndTheLevelStateTravelsWithTheWinner()
        {
            Profile local = At(60, T0, 500);
            local.Stock.Undo = 9;
            local.Stock.Hint = 1;
            local.Wallet.Canoes.Count = 2;
            local.LevelState = new LevelStateData { Level = 61, Seed = "amazonfill:v1:m03:v1:n0061", Pours = 7 };
            Profile cloud = At(55, T0.AddHours(1), 700);
            cloud.Stock.Undo = 4;
            cloud.Stock.Hint = 6;
            cloud.Wallet.Canoes.Count = 5;
            cloud.LevelState = new LevelStateData { Level = 56, Seed = "amazonfill:v1:m03:v1:n0056" };

            Profile merged = CloudMerge.Resolve(local, cloud).Merged;
            Assert.That(merged.Wallet.Seeds, Is.EqualTo(500), "04 §10.2: nunca se suman");
            Assert.That(merged.Stock.Undo, Is.EqualTo(9));
            Assert.That(merged.Stock.Hint, Is.EqualTo(1));
            Assert.That(merged.Wallet.Canoes.Count, Is.EqualTo(2));
            Assert.That(merged.LevelState!.Level, Is.EqualTo(61), "level_state viaja en el blob del ganador (08 §11)");
            Assert.That(merged.LevelState.Pours, Is.EqualTo(7));
        }

        [Test]
        public void Case7_SameDayDaily_MaxPerKey_ShieldAnd_HitsUnion_SeedsMax()
        {
            Profile local = At(60, T0, 500);
            Profile cloud = At(50, T0, 100);
            DailyData a = local.Daily;
            DailyData b = cloud.Daily;
            a.DateLocal = b.DateLocal = "2026-10-20";
            a.RvUndoRefill = 1; b.RvUndoRefill = 3;
            a.RvHint = 2; b.RvHint = 0;
            a.RvTotal = 3; b.RvTotal = 4;
            a.RescuesToday = 0; b.RescuesToday = 1;
            a.InterstitialsToday = 4; b.InterstitialsToday = 2;
            a.ShufflesToday = 1;
            a.ShieldAvailable = true; b.ShieldAvailable = false;
            a.StreakHits.Add(6); b.StreakHits.Add(3); b.StreakHits.Add(6);
            a.StreakSeeds = 100; b.StreakSeeds = 250;
            a.AfRescueDeclinedDays = 1; b.AfRescueDeclinedDays = 2;
            b.AfRescueHiddenUntil = T0.AddHours(48);

            DailyData m = CloudMerge.Resolve(local, cloud).Merged.Daily;
            Assert.That(m.RvUndoRefill, Is.EqualTo(3), "max: el tope no se duplica por dispositivo");
            Assert.That(m.RvHint, Is.EqualTo(2));
            Assert.That(m.RvTotal, Is.EqualTo(4));
            Assert.That(m.RescuesToday, Is.EqualTo(1), "el descuento del primer rescate ya se usó en un teléfono");
            Assert.That(m.InterstitialsToday, Is.EqualTo(4));
            Assert.That(m.ShufflesToday, Is.EqualTo(1));
            Assert.That(m.ShieldAvailable, Is.False, "B1a-Q1: un escudo gastado en un teléfono no vuelve");
            Assert.That(m.StreakHits, Is.EqualTo(new[] { 3, 6 }), "B1a-Q1: unión, en orden");
            Assert.That(m.StreakSeeds, Is.EqualTo(250), "B1a-Q1: máximo");
            Assert.That(m.AfRescueDeclinedDays, Is.EqualTo(2));
            Assert.That(m.AfRescueHiddenUntil, Is.EqualTo(T0.AddHours(48)));
        }

        [Test]
        public void Case7_DifferentDayDaily_TheLatestDayWins_Whole()
        {
            Profile local = At(60, T0, 500);
            Profile cloud = At(50, T0, 100);
            local.Daily.DateLocal = "2026-10-19";
            local.Daily.RvUndoRefill = 4;
            local.Daily.ShieldAvailable = false;
            local.Daily.StreakHits.Add(10);
            local.Daily.AfRescueDeclinedDays = 2;
            cloud.Daily.DateLocal = "2026-10-20";
            cloud.Daily.RvUndoRefill = 1;
            cloud.Daily.ShieldAvailable = true;

            DailyData m = CloudMerge.Resolve(local, cloud).Merged.Daily;
            Assert.That(m.DateLocal, Is.EqualTo("2026-10-20"), "[P]: los contadores de ayer no cuentan hoy (04 §10.1)");
            Assert.That(m.RvUndoRefill, Is.EqualTo(1));
            Assert.That(m.ShieldAvailable, Is.True);
            Assert.That(m.StreakHits, Is.Empty);
            Assert.That(m.AfRescueDeclinedDays, Is.EqualTo(2), "no es del día: máximo");
        }

        [Test]
        public void Case7_ProgressEntitlementsFaunaStats_AreMergedOverTheWinner()
        {
            Profile local = At(60, T0, 500);
            local.Progress.MapSeenLevel = 40;
            local.Progress.SkippedLevels.Add(45);
            local.Entitlements.Skins.Add("af_skin_m03");
            local.Fauna.RecordSighting("saimiri_collinsi", 12);
            local.Stats.LevelsWon = 58;
            local.Stats.Sessions = 10;
            Profile cloud = At(50, T0.AddHours(3), 100);
            cloud.Progress.MapSeenLevel = 50;
            cloud.Progress.UndoTipSeen = true;
            cloud.Progress.SkippedLevels.Add(33);
            cloud.Progress.SkippedLevels.Add(45);
            cloud.Entitlements.NoAds = true;
            cloud.Entitlements.Skins.Add("af_skin_m02");
            cloud.Entitlements.Skins.Add("af_skin_m03");
            cloud.Fauna.RecordSighting("saimiri_collinsi", 9);
            cloud.Fauna.RecordSighting("cebuella_pygmaea", 30);
            cloud.Stats.Sessions = 20;
            cloud.Ads.LastIap = T0.AddHours(2);

            Profile m = CloudMerge.Resolve(local, cloud).Merged;
            Assert.That(m.Progress.MaxLevel, Is.EqualTo(60));
            Assert.That(m.Progress.MapSeenLevel, Is.EqualTo(50), "D-013: máximo");
            Assert.That(m.Progress.UndoTipSeen, Is.True, "DC23: visto si lo vio cualquiera");
            Assert.That(m.Progress.SkippedLevels, Is.EqualTo(new[] { 33, 45 }), "Q-S1: unión, en orden");
            Assert.That(m.Entitlements.NoAds, Is.True, "entitlements = or(...)");
            Assert.That(m.Entitlements.Skins, Is.EqualTo(new[] { "af_skin_m02", "af_skin_m03" }));
            Assert.That(m.Fauna.Sighted.Count, Is.EqualTo(2), "album = union(...)");
            Assert.That(m.Fauna.Find("saimiri_collinsi")!.Level, Is.EqualTo(9), "con el nivel más bajo");
            Assert.That(m.Stats.LevelsWon, Is.EqualTo(58));
            Assert.That(m.Stats.Sessions, Is.EqualTo(20), "[P]: contadores por máximo");
            Assert.That(m.Ads.LastIap, Is.EqualTo(T0.AddHours(2)), "[P]: la última compra con dinero");
        }

        [Test]
        public void Case8_Integrity_TheCoherentBlobWins_WithNothingFromTheOther()
        {
            Profile local = At(90, T0.AddHours(9), 50000); // más nivel, pero manipulado
            local.Entitlements.NoAds = true;
            local.Daily.DateLocal = "2026-10-20";
            local.Daily.RvTotal = 0;
            Profile cloud = At(40, T0, 300);
            cloud.Daily.DateLocal = "2026-10-20";
            cloud.Daily.RvTotal = 2;

            CloudMergeResult r = CloudMerge.Resolve(local, cloud, localCoherent: false, cloudCoherent: true);
            Assert.That(r.Reason, Is.EqualTo(CloudMergeReason.Integrity));
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Cloud), "08 §10.1: gana el blob coherente");
            Assert.That(r.Merged.Progress.MaxLevel, Is.EqualTo(40));
            Assert.That(r.Merged.Wallet.Seeds, Is.EqualTo(300));
            Assert.That(r.Merged.Entitlements.NoAds, Is.False, "[P]: el manipulado no aporta derechos");
            Assert.That(r.Merged.Daily.RvTotal, Is.EqualTo(2));
            Assert.That(r.SyncResultId, Is.EqualTo("conflict_cloud_won"));
            Assert.That(r.CloudChanged, Is.False, "la nube ya es el resultado");

            // Los dos coherentes (o los dos no): las reglas de siempre.
            Assert.That(CloudMerge.Resolve(local, cloud, false, false).Reason, Is.EqualTo(CloudMergeReason.MaxLevel));
            Assert.That(CloudMerge.Resolve(local, cloud, true, false).Winner, Is.EqualTo(CloudMergeWinner.Local));
        }

        [Test]
        public void Resolve_DoesNotTouchItsInputs_AndIsDeterministic()
        {
            Profile local = SampleProfiles.Full();
            Profile cloud = At(87, SampleProfiles.T0.AddDays(1), 10);
            cloud.Daily.DateLocal = local.Daily.DateLocal;
            cloud.Daily.RvTotal = 30;
            string localBefore = ProfileJson.Serialize(local);
            string cloudBefore = ProfileJson.Serialize(cloud);

            string first = ProfileJson.Serialize(CloudMerge.Resolve(local, cloud).Merged);
            string second = ProfileJson.Serialize(CloudMerge.Resolve(local.Clone(), cloud.Clone()).Merged);
            Assert.That(ProfileJson.Serialize(local), Is.EqualTo(localBefore));
            Assert.That(ProfileJson.Serialize(cloud), Is.EqualTo(cloudBefore));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void UnionSorted_SortsAndDropsDuplicates()
        {
            var ints = new System.Collections.Generic.List<int> { 9, 3, 3 };
            CloudMerge.UnionSorted(ints, new[] { 1, 9, 5 });
            Assert.That(ints, Is.EqualTo(new[] { 1, 3, 5, 9 }));

            var texts = new System.Collections.Generic.List<string> { "b", "B" };
            CloudMerge.UnionSorted(texts, new[] { "a", "b" });
            Assert.That(texts, Is.EqualTo(new[] { "B", "a", "b" }), "orden ordinal");
        }

        [TestCase(CloudSyncResult.Ok, "ok")]
        [TestCase(CloudSyncResult.ConflictLocalWon, "conflict_local_won")]
        [TestCase(CloudSyncResult.ConflictCloudWon, "conflict_cloud_won")]
        [TestCase(CloudSyncResult.Merged, "merged")]
        [TestCase(CloudSyncResult.Failed, "failed")]
        public void SyncResultIds_AreThoseOf08_8_2(CloudSyncResult result, string id)
        {
            Assert.That(CloudMerge.ToId(result), Is.EqualTo(id));
        }
    }
}
