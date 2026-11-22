#nullable enable
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>progress.league_debut_seen</c> y <c>progress.league_opened</c> [P] (añadido de D-046 del 2026-11-06, el estreno de la Liga
    /// de la 1.1): false por defecto, también en un perfil de la 1.0 sin las claves (entran sin migración); solo se escriben si son
    /// ciertas (el JSON de un perfil sin ellas no cambia); ida y vuelta por JSON; un valor de otro tipo vale false sin hacer
    /// corrupto el perfil; y en la fusión de la nube van por «o», así el estreno no vuelve tras reinstalar.
    /// </summary>
    public sealed class LeagueDebutProfileTests
    {
        [Test]
        public void Default_IsFalse_AndNotWritten()
        {
            Profile fresh = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Profile old = ProfileJson.Deserialize("{\"profile_schema\": 3, \"progress\": {\"max_level\": 45, \"next_level\": 46}}");

            Assert.That(fresh.Progress.LeagueDebutSeen, Is.False);
            Assert.That(fresh.Progress.LeagueOpened, Is.False);
            Assert.That(old.Progress.LeagueDebutSeen, Is.False);
            Assert.That(old.Progress.LeagueOpened, Is.False);
            string json = ProfileJson.Serialize(old);
            StringAssert.DoesNotContain("league_debut_seen", json);
            StringAssert.DoesNotContain("league_opened", json);
        }

        [Test]
        public void True_RoundTrips()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            p.Progress.LeagueDebutSeen = true;
            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"league_debut_seen\": true", json);
            StringAssert.DoesNotContain("league_opened", json);
            Profile back = ProfileJson.Deserialize(json);
            Assert.That(back.Progress.LeagueDebutSeen, Is.True);
            Assert.That(back.Progress.LeagueOpened, Is.False);

            p.Progress.LeagueOpened = true;
            Profile clone = p.Clone();
            Assert.That(clone.Progress.LeagueDebutSeen, Is.True);
            Assert.That(clone.Progress.LeagueOpened, Is.True);
        }

        [TestCase("\"true\"")]
        [TestCase("1")]
        [TestCase("null")]
        [TestCase("{}")]
        public void InvalidValue_IsFalse_WithoutCorruptingTheProfile(string value)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 3, \"progress\": {\"max_level\": 50, \"league_debut_seen\": " + value
                + ", \"league_opened\": " + value + "}}");

            Assert.That(p.Progress.LeagueDebutSeen, Is.False);
            Assert.That(p.Progress.LeagueOpened, Is.False);
            Assert.That(p.Progress.MaxLevel, Is.EqualTo(50));
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public void Merge_IsTrueIfEitherHasIt(bool winner, bool loser, bool expected)
        {
            var a = new ProgressData { LeagueDebutSeen = winner, LeagueOpened = loser };
            var b = new ProgressData { LeagueDebutSeen = loser, LeagueOpened = winner };

            a.MergeLeagueDebut(b);

            Assert.That(a.LeagueDebutSeen, Is.EqualTo(expected));
            Assert.That(a.LeagueOpened, Is.EqualTo(expected));
            Assert.That(b.LeagueDebutSeen, Is.EqualTo(loser), "el perdedor no cambia");
        }

        [Test]
        public void Merge_RejectsNull()
        {
            Assert.That(() => new ProgressData().MergeLeagueDebut(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void CloudRestore_KeepsTheDebutSeen_EvenIfTheWinnerLacksIt()
        {
            // Reinstalar: el perfil local nuevo (sin nada) pierde con la nube; o el local con más nivel gana a una nube que ya lo vio.
            Profile local = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            local.Progress.MaxLevel = 60;
            Profile cloud = local.Clone();
            cloud.Progress.MaxLevel = 45;
            cloud.Progress.LeagueDebutSeen = true;
            cloud.Progress.LeagueOpened = true;

            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Local));
            Assert.That(r.Merged.Progress.LeagueDebutSeen, Is.True, "el estreno no vuelve por perder un conflicto");
            Assert.That(r.Merged.Progress.LeagueOpened, Is.True);
        }
    }
}
