#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>progress.map_seen_level</c> (S12; D-013 punto 14; 08 §4.3 y §11): 0 por defecto, también en un perfil sin el
    /// campo y sin migrar; recortado a [0, <c>max_level</c>] al guardar y al leer; un valor inválido vale 0; y en la
    /// fusión de la nube gana el máximo, como <c>max_level</c>.
    /// </summary>
    public sealed class MapSeenLevelTests
    {
        [Test]
        public void Default_IsZero_AlsoWithoutTheField()
        {
            Profile fresh = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Profile old = ProfileJson.Deserialize("{\"profile_schema\": 1, \"progress\": {\"max_level\": 87, \"next_level\": 88}}");
            Profile noProgress = ProfileJson.Deserialize("{\"profile_schema\": 1}");

            Assert.That(fresh.Progress.MapSeenLevel, Is.EqualTo(0));
            Assert.That(old.Progress.MapSeenLevel, Is.EqualTo(0));
            Assert.That(noProgress.Progress.MapSeenLevel, Is.EqualTo(0));
            StringAssert.Contains("\"map_seen_level\": 0", ProfileJson.Serialize(old));
            Assert.That(Profile.CurrentSchema, Is.EqualTo(1), "sin migración: el esquema aún no se ha publicado");
        }

        [TestCase(0, 0)]
        [TestCase(10, 10)]
        [TestCase(80, 80)]
        [TestCase(87, 87)]
        [TestCase(88, 87)]
        [TestCase(1000, 87)]
        [TestCase(-1, 0)]
        [TestCase(int.MinValue, 0)]
        public void IsClampedToZeroAndMaxLevel_WhenRead(int stored, int expected)
        {
            string json = "{\"profile_schema\": 1, \"progress\": {\"max_level\": 87, \"map_seen_level\": "
                + stored.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}}";

            Assert.That(ProfileJson.Deserialize(json).Progress.MapSeenLevel, Is.EqualTo(expected));
        }

        [Test]
        public void IsClampedWhenSaved_AndRoundTripsInsideTheRange()
        {
            Profile p = SampleProfiles.Full();
            Assert.That(p.Progress.MaxLevel, Is.EqualTo(87));

            p.Progress.MapSeenLevel = 60;
            Assert.That(ProfileJson.Deserialize(ProfileJson.Serialize(p)).Progress.MapSeenLevel, Is.EqualTo(60));

            p.Progress.MapSeenLevel = 95;
            Assert.That(p.Progress.ClampedMapSeenLevel, Is.EqualTo(87));
            StringAssert.Contains("\"map_seen_level\": 87", ProfileJson.Serialize(p));

            p.Progress.MapSeenLevel = -3;
            StringAssert.Contains("\"map_seen_level\": 0", ProfileJson.Serialize(p));

            p.Progress.MaxLevel = 0;
            p.Progress.MapSeenLevel = 5;
            Assert.That(p.Progress.ClampedMapSeenLevel, Is.EqualTo(0), "sin niveles superados no hay nada que ver");
        }

        [TestCase("\"80\"")]
        [TestCase("80.5")]
        [TestCase("3000000000")]
        [TestCase("null")]
        [TestCase("true")]
        [TestCase("[80]")]
        public void InvalidValue_IsZero_WithoutCorruptingTheProfile(string value)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 1, \"progress\": {\"max_level\": 87, \"map_seen_level\": " + value + "}}");

            Assert.That(p.Progress.MapSeenLevel, Is.EqualTo(0));
            Assert.That(p.Progress.MaxLevel, Is.EqualTo(87));
        }

        [TestCase(60, 80, 80)]
        [TestCase(80, 60, 80)]
        [TestCase(0, 0, 0)]
        [TestCase(87, 0, 87)]
        public void Merge_TakesTheMaximum(int winnerSeen, int loserSeen, int expected)
        {
            Profile winner = SampleProfiles.Full();
            Profile loser = SampleProfiles.Full();
            winner.Progress.MapSeenLevel = winnerSeen;
            loser.Progress.MapSeenLevel = loserSeen;

            Profile merged = winner.Clone();
            merged.Progress.MergeMapSeenLevel(loser.Progress);

            Assert.That(merged.Progress.MapSeenLevel, Is.EqualTo(expected));
            Assert.That(winner.Progress.MapSeenLevel, Is.EqualTo(winnerSeen), "la fusión trabaja sobre la copia");
        }

        [Test]
        public void Merge_StaysWithinTheWinnersMaxLevel()
        {
            // 08 §11: gana el perfil con más max_level, así que el máximo de los dos ya cabe; si el otro trae un valor
            // fuera de rango (un archivo editado), se recorta igual que al leer.
            var winner = new ProgressData { MaxLevel = 40, MapSeenLevel = 30 };
            var other = new ProgressData { MaxLevel = 40, MapSeenLevel = 55 };

            winner.MergeMapSeenLevel(other);

            Assert.That(winner.MapSeenLevel, Is.EqualTo(40));
            Assert.Throws<ArgumentNullException>(() => winner.MergeMapSeenLevel(null!));
        }
    }
}
