#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Lo que lee y escribe el Mapa del río del progreso (tarea M3.1b; 08 §2.2 y §4.3; D-013 punto 14; propuesta del Mapa
    /// §4.2): <see cref="IMapProgress"/> sobre el perfil y en memoria, <c>SaveMapSeenLevel</c> monotónico en los dos almacenes
    /// (y por el lote de guardado, sin escribir si no cambia), <c>IsSkipped</c> igual que <c>progress.skipped_levels</c>, y
    /// «ponerse al día» en <see cref="RiverJourney"/> (<c>HasUnseenArrival</c>, <c>CatchUpFrom</c> y <c>CatchUpMs</c>) en los
    /// bordes 9/10, 19/20 y 58/60, todo sin asignaciones.
    /// </summary>
    public sealed class MapSeenProgressTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        private static int s_sink;

        // seen, max, llegada sin ver, CatchUpFrom y CatchUpMs.
        private static readonly object[] Borders =
        {
            new object[] { 9, 10, true, 9, 150 },
            new object[] { 10, 10, false, 10, 0 },
            new object[] { 19, 20, true, 19, 150 },
            new object[] { 20, 20, false, 20, 0 },
            new object[] { 58, 60, true, 58, 300 },
            new object[] { 52, 58, false, 52, 900 },
            new object[] { 50, 58, false, 50, 1200 },
            new object[] { 49, 58, true, 49, 1350 },
            new object[] { 0, 58, true, 48, 1500 },
            new object[] { 0, 9, false, 0, 1350 },
            new object[] { 0, 10, true, 0, 1500 },
            new object[] { 0, 0, false, 0, 0 },
            new object[] { 60, 58, false, 58, 0 },
            new object[] { 990, 1000, true, 990, 1500 },
            new object[] { 1000, 1000, false, 1000, 0 },
        };

        [TestCaseSource(nameof(Borders))]
        public void Borders_UnseenArrivalAndCatchUp(int seen, int max, bool unseen, int from, int ms)
        {
            Assert.That(RiverJourney.HasUnseenArrival(seen, max), Is.EqualTo(unseen), "max ≥ 10 y max / 10 > seen / 10");
            Assert.That(RiverJourney.CatchUpFrom(seen, max), Is.EqualTo(from), "máx(seen, max − 10)");
            Assert.That(RiverJourney.CatchUpMs(seen, max), Is.EqualTo(ms), "150 ms por boya, 1,5 s como mucho");
        }

        [Test]
        public void CatchUp_TenBuoysAtMost_WithinTheMaxTime_AndAlwaysWithTheUnseenArrival()
        {
            Assert.That(RiverJourney.CatchUpMaxBuoys, Is.EqualTo(10), "un tramo");
            Assert.That(RiverJourney.CatchUpMaxBuoys * MapConfig.CatchUpPerBuoyMs, Is.EqualTo(MapConfig.CatchUpMaxMs), "10 · 150 ms = 1,5 s");
            int[] seens = { 0, 1, 9, 10, 11, 19, 20, 47, 50, 52, 58, 59, 60, 139, 140, 199, 200, 699, 700, 999, 1000, 1200 };
            for (int max = 0; max <= WorldTable.LastLevel; max++)
            {
                foreach (int seen in seens)
                {
                    int from = RiverJourney.CatchUpFrom(seen, max);
                    string at = "seen " + seen + ", max " + max;
                    Assert.That(from, Is.InRange(Math.Max(0, max - 10), max), at);
                    Assert.That(from, Is.GreaterThanOrEqualTo(Math.Min(seen, max)), at + ": nunca repite boyas ya vistas");
                    Assert.That(RiverJourney.CatchUpMs(seen, max), Is.InRange(0, MapConfig.CatchUpMaxMs), at);
                    bool unseen = RiverJourney.HasUnseenArrival(seen, max);
                    if (unseen)
                    {
                        int arrival = max / 10 * 10;
                        Assert.That(arrival, Is.GreaterThan(from), at + ": la llegada sin ver entra en las boyas que se encienden");
                    }

                    if (seen >= max)
                    {
                        Assert.That(unseen, Is.False, at + ": todo visto");
                    }
                }
            }
        }

        [Test]
        public void CatchUp_OutOfRange_Throws()
        {
            Assert.That(() => RiverJourney.HasUnseenArrival(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => RiverJourney.HasUnseenArrival(0, 1001), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => RiverJourney.HasUnseenArrival(-1, 10), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => RiverJourney.CatchUpFrom(-1, 10), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => RiverJourney.CatchUpFrom(0, 1001), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => RiverJourney.CatchUpMs(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void MemoryStore_SaveMapSeenLevel_NeverGoesDown_AndCountsOnlyChanges()
        {
            var store = new MemoryProgressStore(59, 58);
            Assert.That(store.MapSeenLevel, Is.EqualTo(0), "por defecto, 0");
            store.SaveMapSeenLevel(52);
            Assert.That(store.LoadMapSeenLevel(), Is.EqualTo(52));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            store.SaveMapSeenLevel(40);
            Assert.That(store.MapSeenLevel, Is.EqualTo(52), "nunca baja");
            store.SaveMapSeenLevel(52);
            Assert.That(store.SaveCount, Is.EqualTo(1), "sin cambios no se guarda");
            store.SaveMapSeenLevel(70);
            Assert.That(store.MapSeenLevel, Is.EqualTo(58), "recortado a max_level");
            Assert.That(store.SaveCount, Is.EqualTo(2));
            store.SaveMapSeenLevel(0);
            Assert.That(store.MapSeenLevel, Is.EqualTo(58));
            Assert.That(() => store.SaveMapSeenLevel(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(new MemoryProgressStore(11, 10, 30).MapSeenLevel, Is.EqualTo(10), "el valor inicial también se recorta");

            var high = new MemoryProgressStore(11, 10, 30);
            high.SaveWin(40, 41);
            Assert.That(high.MapSeenLevel, Is.EqualTo(10), "el valor inicial recortado no reaparece al subir max_level");
            Assert.That(RiverJourney.HasUnseenArrival(high.MapSeenLevel, high.MaxLevel), Is.True, "la llegada sigue sin ver, como en el perfil");
        }

        [Test]
        public void ProfileStore_SaveMapSeenLevel_NeverGoesDown_PersistsAndSkipsUnchangedWrites()
        {
            var disk = new MemoryProfileStore();
            Profile profile = NewProfile();
            var progress = new ProfileProgressStore(profile, disk);
            progress.SaveWin(58, 59);
            int writes = disk.SaveCount;
            Assert.That(progress.MapSeenLevel, Is.EqualTo(0));

            progress.SaveMapSeenLevel(52);
            Assert.That(progress.LoadMapSeenLevel(), Is.EqualTo(52));
            Assert.That(profile.Progress.MapSeenLevel, Is.EqualTo(52));
            Assert.That(ProfileJson.Deserialize(disk.Json!).Progress.MapSeenLevel, Is.EqualTo(52), "persistido");
            Assert.That(disk.SaveCount, Is.EqualTo(writes + 1));

            progress.SaveMapSeenLevel(40);
            progress.SaveMapSeenLevel(52);
            Assert.That(progress.MapSeenLevel, Is.EqualTo(52), "nunca baja");
            Assert.That(disk.SaveCount, Is.EqualTo(writes + 1), "sin cambios no se escribe");

            progress.SaveMapSeenLevel(1000);
            Assert.That(progress.MapSeenLevel, Is.EqualTo(58), "recortado a max_level");
            Assert.That(ProfileJson.Deserialize(disk.Json!).Progress.MapSeenLevel, Is.EqualTo(58));
            Assert.That(disk.SaveCount, Is.EqualTo(writes + 2));
            Assert.That(() => progress.SaveMapSeenLevel(-5), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(progress.PendingSave, Is.False);
        }

        [Test]
        public void ProfileStore_SavesMapSeenLevel_ThroughTheSaveBatch_InOneWrite()
        {
            var disk = new MemoryProfileStore();
            var batch = new ProfileSaveBatch(disk);
            Profile profile = NewProfile();
            profile.Progress.MaxLevel = 60;
            var progress = new ProfileProgressStore(profile, batch);

            using (batch.Begin())
            {
                progress.SaveMapSeenLevel(50);
                progress.SaveMapSeenLevel(60);
                Assert.That(disk.SaveCount, Is.EqualTo(0), "dentro del lote solo se anota");
            }

            Assert.That(disk.SaveCount, Is.EqualTo(1), "una escritura al cerrar el lote");
            Assert.That(ProfileJson.Deserialize(disk.Json!).Progress.MapSeenLevel, Is.EqualTo(60));

            using (batch.Begin())
            {
                progress.SaveMapSeenLevel(55);
            }

            Assert.That(disk.SaveCount, Is.EqualTo(1), "sin cambios, el lote no escribe");
        }

        [Test]
        public void ProfileStore_ReadsTheProfile_AsIMapProgress()
        {
            Profile profile = NewProfile();
            profile.Progress.MaxLevel = 87;
            profile.Progress.NextLevel = 88;
            profile.Progress.MapSeenLevel = 95;
            profile.Progress.SkippedLevels.Add(23);
            profile.Progress.SkippedLevels.Add(47);
            IMapProgress map = new ProfileProgressStore(profile, new MemoryProfileStore());

            Assert.That(map.MaxLevel, Is.EqualTo(87));
            Assert.That(map.NextLevel, Is.EqualTo(88));
            Assert.That(map.MapSeenLevel, Is.EqualTo(87), "recortado a [0, max_level], como se guarda");
            for (int level = -1; level <= WorldTable.LastLevel + 1; level++)
            {
                Assert.That(map.IsSkipped(level), Is.EqualTo(profile.Progress.SkippedLevels.Contains(level)), "nivel " + level);
            }

            profile.Progress.SkippedLevels.Add(61);
            Assert.That(map.IsSkipped(61), Is.True, "lee el perfil vivo, sin copia");
        }

        [Test]
        public void MemoryStore_IsSkipped_AndIMapProgress()
        {
            var store = new MemoryProgressStore(31, 30, 20);
            store.MarkSkipped(17);
            store.MarkSkipped(17);
            IMapProgress map = store;
            Assert.That(map.MaxLevel, Is.EqualTo(30));
            Assert.That(map.NextLevel, Is.EqualTo(31));
            Assert.That(map.MapSeenLevel, Is.EqualTo(20));
            Assert.That(map.IsSkipped(17), Is.True);
            Assert.That(map.IsSkipped(18), Is.False);
            Assert.That(RiverJourney.HasUnseenArrival(map.MapSeenLevel, map.MaxLevel), Is.True, "20 → 30: la llegada del 30");
        }

        [Test]
        public void ReadsAndCatchUp_DoNotAllocate()
        {
            Profile profile = NewProfile();
            profile.Progress.MaxLevel = 58;
            profile.Progress.NextLevel = 59;
            profile.Progress.MapSeenLevel = 52;
            profile.Progress.SkippedLevels.Add(23);
            profile.Progress.SkippedLevels.Add(47);
            var disk = new MemoryProfileStore();
            var progress = new ProfileProgressStore(profile, disk);
            IMapProgress map = progress;
            var memory = new MemoryProgressStore(59, 58, 52);
            memory.MarkSkipped(23);
            IMapProgress memoryMap = memory;

            TestDelegate reads = () =>
            {
                s_sink = map.MaxLevel + map.NextLevel + map.MapSeenLevel + (map.IsSkipped(47) ? 1 : 0) + (map.IsSkipped(48) ? 1 : 0)
                    + memoryMap.MapSeenLevel + (memoryMap.IsSkipped(23) ? 1 : 0) + progress.LoadMapSeenLevel();
            };
            TestDelegate journey = () =>
            {
                s_sink = (RiverJourney.HasUnseenArrival(52, 58) ? 1 : 0) + (RiverJourney.HasUnseenArrival(58, 60) ? 1 : 0)
                    + RiverJourney.CatchUpFrom(0, 58) + RiverJourney.CatchUpMs(52, 58);
            };
            TestDelegate unchanged = () =>
            {
                progress.SaveMapSeenLevel(40);
                memory.SaveMapSeenLevel(52);
            };

            Assert.That(reads, Is.Not.AllocatingGCMemory(), "IMapProgress sin asignaciones");
            Assert.That(journey, Is.Not.AllocatingGCMemory(), "ponerse al día sin asignaciones");
            Assert.That(unchanged, Is.Not.AllocatingGCMemory(), "guardar sin cambios no escribe ni asigna");
            Assert.That(disk.SaveCount, Is.EqualTo(0));
        }

        private static Profile NewProfile()
        {
            return AF.Economy.Wallet.NewProfile(new ManualClock(T0));
        }
    }
}
