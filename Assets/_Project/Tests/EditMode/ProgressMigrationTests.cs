#nullable enable
using System;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El progreso pasa de PlayerPrefs (<c>af_v0_level</c>, prototipo v0) a <c>progress.max_level</c> y
    /// <c>progress.next_level</c> del perfil (08 §4.3), una sola vez (tarea B1-5), y <see cref="ProfileProgressStore"/>
    /// guarda cada victoria en el perfil.
    /// </summary>
    public sealed class ProgressMigrationTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        [Test]
        public void NewProfile_TakesTheV0Level_SavesIt_AndDeletesTheOldKey()
        {
            var store = new MemoryProfileStore();
            Profile profile = NewProfile();
            var legacy = new FakeLegacy(4);

            LegacyProgressOutcome outcome = ProgressMigration.MigrateV0(profile, true, store, legacy);

            Assert.That(outcome, Is.EqualTo(LegacyProgressOutcome.Migrated));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(4));
            Assert.That(profile.Progress.MaxLevel, Is.EqualTo(3), "v0 guardaba el siguiente nivel al ganar el anterior");
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.NextLevel, Is.EqualTo(4));
            Assert.That(legacy.Deleted, Is.True);
        }

        [Test]
        public void ExistingProfile_Wins_AndTheOldKeyIsDiscarded()
        {
            var store = new MemoryProfileStore();
            Profile profile = NewProfile();
            profile.Progress.NextLevel = 7;
            profile.Progress.MaxLevel = 6;
            var legacy = new FakeLegacy(3);

            LegacyProgressOutcome outcome = ProgressMigration.MigrateV0(profile, false, store, legacy);

            Assert.That(outcome, Is.EqualTo(LegacyProgressOutcome.Discarded));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(7));
            Assert.That(profile.Progress.MaxLevel, Is.EqualTo(6));
            Assert.That(store.SaveCount, Is.EqualTo(0));
            Assert.That(legacy.Deleted, Is.True);
        }

        [Test]
        public void WithoutV0Progress_NothingChanges()
        {
            var store = new MemoryProfileStore();
            Profile profile = NewProfile();
            var legacy = new FakeLegacy(null);

            Assert.That(ProgressMigration.MigrateV0(profile, true, store, legacy), Is.EqualTo(LegacyProgressOutcome.None));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(0));
            Assert.That(legacy.Deleted, Is.False);
        }

        [Test]
        public void AnInvalidV0Value_IsDiscarded()
        {
            var store = new MemoryProfileStore();
            Profile profile = NewProfile();
            var legacy = new FakeLegacy(0);

            Assert.That(ProgressMigration.MigrateV0(profile, true, store, legacy), Is.EqualTo(LegacyProgressOutcome.Discarded));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(1));
            Assert.That(legacy.Deleted, Is.True);
        }

        [Test]
        public void IfTheSaveFails_TheOldKeyIsKept_ForTheNextLaunch()
        {
            Profile profile = NewProfile();
            var legacy = new FakeLegacy(5);

            LegacyProgressOutcome outcome = ProgressMigration.MigrateV0(profile, true, new FailingStore(), legacy);

            Assert.That(outcome, Is.EqualTo(LegacyProgressOutcome.MigratedNotSaved));
            Assert.That(profile.Progress.NextLevel, Is.EqualTo(5));
            Assert.That(legacy.Deleted, Is.False);
        }

        [Test]
        public void ItRunsOnce_TheSecondLaunchFindsNothingToMigrate()
        {
            var store = new MemoryProfileStore();
            var legacy = new FakeLegacy(6);
            Assert.That(ProgressMigration.MigrateV0(NewProfile(), true, store, legacy), Is.EqualTo(LegacyProgressOutcome.Migrated));

            Profile reloaded = store.Load().Profile!;
            Assert.That(ProgressMigration.MigrateV0(reloaded, false, store, legacy), Is.EqualTo(LegacyProgressOutcome.None));
            Assert.That(reloaded.Progress.NextLevel, Is.EqualTo(6));
        }

        [Test]
        public void PlayerPrefsSource_ReadsAndDeletesAfV0Level()
        {
            // El origen real: la clave af_v0_level de PlayerPrefs (se restaura al terminar).
            bool had = PlayerPrefs.HasKey(PlayerPrefsProgressStore.Key);
            int previous = PlayerPrefs.GetInt(PlayerPrefsProgressStore.Key, 0);
            try
            {
                var source = new PlayerPrefsProgressStore();
                PlayerPrefs.DeleteKey(PlayerPrefsProgressStore.Key);
                Assert.That(source.TryRead(out _), Is.False);
                PlayerPrefs.SetInt(PlayerPrefsProgressStore.Key, 8);
                Assert.That(source.TryRead(out int next), Is.True);
                Assert.That(next, Is.EqualTo(8));
                source.Delete();
                Assert.That(PlayerPrefs.HasKey(PlayerPrefsProgressStore.Key), Is.False);
            }
            finally
            {
                if (had)
                {
                    PlayerPrefs.SetInt(PlayerPrefsProgressStore.Key, previous);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PlayerPrefsProgressStore.Key);
                }
            }
        }

        [Test]
        public void ProfileProgressStore_SavesEachWinInTheProfile()
        {
            var store = new MemoryProfileStore();
            Profile profile = NewProfile();
            var progress = new ProfileProgressStore(profile, store);
            Assert.That(progress.LoadNextLevel(), Is.EqualTo(1), "08 §2.3: la primera vez, el nivel 1");
            Assert.That(progress.LoadMaxLevel(), Is.EqualTo(0));

            progress.SaveWin(1, 2);
            Assert.That(progress.LoadNextLevel(), Is.EqualTo(2));
            Assert.That(progress.LoadMaxLevel(), Is.EqualTo(1));
            Assert.That(progress.PendingSave, Is.False);
            Profile saved = ProfileJson.Deserialize(store.Json!);
            Assert.That(saved.Progress.NextLevel, Is.EqualTo(2));
            Assert.That(saved.Progress.MaxLevel, Is.EqualTo(1));

            progress.SaveNextLevel(5);
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.NextLevel, Is.EqualTo(5));
            progress.SaveWin(3, 4);
            Assert.That(progress.LoadMaxLevel(), Is.EqualTo(3));
            progress.SaveWin(2, 3);
            Assert.That(progress.LoadMaxLevel(), Is.EqualTo(3), "max_level nunca baja");
            Assert.That(() => progress.SaveNextLevel(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ProfileProgressStore_KeepsTheChangeInMemory_WhenTheSaveFails()
        {
            Profile profile = NewProfile();
            var progress = new ProfileProgressStore(profile, new FailingStore());
            progress.SaveWin(1, 2);
            Assert.That(progress.PendingSave, Is.True);
            Assert.That(progress.LoadNextLevel(), Is.EqualTo(2));
        }

        private static Profile NewProfile()
        {
            return AF.Economy.Wallet.NewProfile(new ManualClock(T0));
        }

        private sealed class FakeLegacy : ILegacyProgressSource
        {
            private readonly int? _value;

            public FakeLegacy(int? value)
            {
                _value = value;
            }

            public bool Deleted { get; private set; }

            public bool TryRead(out int nextLevel)
            {
                nextLevel = _value ?? 0;
                return _value.HasValue && !Deleted;
            }

            public void Delete()
            {
                Deleted = true;
            }
        }

        private sealed class FailingStore : IProfileStore
        {
            public ProfileLoadResult Load()
            {
                return new ProfileLoadResult(ProfileLoadStatus.NotFound, null);
            }

            public void Save(Profile profile)
            {
                throw new System.IO.IOException("disco lleno");
            }

            public void Delete()
            {
            }
        }
    }
}
