#nullable enable
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>progress.undo_tip_seen</c> [P] (D-030, pregunta 15 (b); tarea DC23): el tooltip de Marea, una vez en la vida. False por
    /// defecto, también en un perfil del esquema 2 sin la clave (sin migración: el esquema aún no se ha publicado); ida y vuelta
    /// por JSON (lo que sobrevive a cerrar la app); un valor de otro tipo vale «no visto» sin hacer corrupto el perfil; y en la
    /// fusión de la nube basta con que uno de los dos lo haya visto.
    /// </summary>
    public sealed class UndoTipSeenTests
    {
        [Test]
        public void Default_IsNotSeen_AlsoWithoutTheField()
        {
            Profile fresh = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Profile old = ProfileJson.Deserialize("{\"profile_schema\": 2, \"progress\": {\"max_level\": 4, \"next_level\": 5}}");
            Profile noProgress = ProfileJson.Deserialize("{\"profile_schema\": 2}");

            Assert.That(fresh.Progress.UndoTipSeen, Is.False);
            Assert.That(old.Progress.UndoTipSeen, Is.False);
            Assert.That(noProgress.Progress.UndoTipSeen, Is.False);
            StringAssert.Contains("\"undo_tip_seen\": false", ProfileJson.Serialize(old));
        }

        [Test]
        public void Seen_RoundTrips_AsIfTheAppWasClosed()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            p.Progress.UndoTipSeen = true;

            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"undo_tip_seen\": true", json);
            Assert.That(ProfileJson.Deserialize(json).Progress.UndoTipSeen, Is.True);
            Assert.That(p.Clone().Progress.UndoTipSeen, Is.True);
        }

        [Test]
        public void Seen_SurvivesTheStoreOnDisk()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "af-undo-tip-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new LocalProfileStore(dir);
                Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
                p.Progress.UndoTipSeen = true;
                store.Save(p);

                ProfileLoadResult loaded = new LocalProfileStore(dir).Load();
                Assert.That(loaded.Profile, Is.Not.Null);
                Assert.That(loaded.Profile!.Progress.UndoTipSeen, Is.True);
            }
            finally
            {
                if (System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.Delete(dir, true);
                }
            }
        }

        [TestCase("\"true\"")]
        [TestCase("1")]
        [TestCase("null")]
        [TestCase("[true]")]
        [TestCase("{}")]
        public void InvalidValue_IsNotSeen_WithoutCorruptingTheProfile(string value)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"progress\": {\"max_level\": 7, \"undo_tip_seen\": " + value + "}}");

            Assert.That(p.Progress.UndoTipSeen, Is.False);
            Assert.That(p.Progress.MaxLevel, Is.EqualTo(7));
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public void Merge_IsSeenIfEitherSawIt(bool winner, bool loser, bool expected)
        {
            var a = new ProgressData { UndoTipSeen = winner };
            var b = new ProgressData { UndoTipSeen = loser };

            a.MergeUndoTipSeen(b);

            Assert.That(a.UndoTipSeen, Is.EqualTo(expected));
            Assert.That(b.UndoTipSeen, Is.EqualTo(loser), "el perdedor no cambia");
        }

        [Test]
        public void Merge_RejectsNull()
        {
            Assert.That(() => new ProgressData().MergeUndoTipSeen(null!), Throws.ArgumentNullException);
        }
    }
}
