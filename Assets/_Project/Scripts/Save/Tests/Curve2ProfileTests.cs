#nullable enable
using AF.Platform;
using AF.Save.Json;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>progress.curve2_from_level</c> y la migración del perfil a <c>profile_schema</c> 3 (D-068, P1 B; 08 §4.3 y §4.4; fase
    /// F4): un perfil nuevo empieza en 1; la migración 2 → 3 fija el siguiente nivel sin jugar (<c>max_level</c> + 1, o el nivel en
    /// curso + 1 si va más allá) y no toca nada más; ida y vuelta por JSON; un documento del 3 sin el campo lee el siguiente nivel
    /// sin jugar, nunca el 1; y en la fusión con la nube gana el mínimo (pregunta 15 de 03), con los dos perfiles ya migrados.
    /// </summary>
    public sealed class Curve2ProfileTests
    {
        [Test]
        public void NewProfile_StartsAt1_AndWritesIt()
        {
            Profile fresh = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Assert.That(fresh.Progress.Curve2FromLevel, Is.EqualTo(1), "una instalación nueva juega 301-700 con la curva 2 (08 §4.3)");
            StringAssert.Contains("\"curve2_from_level\": 1", ProfileJson.Serialize(fresh));
            Assert.That(Profile.CurrentSchema, Is.EqualTo(3));
        }

        [TestCase(350, -1, 351)]
        [TestCase(0, -1, 1)]
        [TestCase(350, 351, 352)]
        [TestCase(350, 120, 351)]
        public void MigrateV2ToV3_SetsTheNextUnplayedLevel(int maxLevel, int levelInProgress, int expected)
        {
            string state = levelInProgress < 0 ? "null" : "{\"level\": " + levelInProgress + ", \"seed\": \"s\", \"variant\": \"base\", \"history\": \"\"}";
            JsonNode doc = JsonText.Parse("{\"profile_schema\": 2, \"progress\": {\"max_level\": " + maxLevel + ", \"next_level\": " + (maxLevel + 1)
                + "}, \"level_state\": " + state + "}");

            ProfileMigrations.Current.Migrate(doc, 2);

            Assert.That(doc.Get(ProfileJson.SchemaKey)!.AsInt(), Is.EqualTo(3));
            Profile p = ProfileJson.FromNode(doc);
            Assert.That(p.Progress.Curve2FromLevel, Is.EqualTo(expected));
            Assert.That(p.Progress.MaxLevel, Is.EqualTo(maxLevel), "no toca nada más");
        }

        [Test]
        public void MigrateV2ToV3_LeavesAnExistingValue_AndAProfileWithoutProgress()
        {
            JsonNode withValue = JsonText.Parse("{\"profile_schema\": 2, \"progress\": {\"max_level\": 400, \"curve2_from_level\": 320}}");
            ProfileMigrations.MigrateV2ToV3(withValue);
            Assert.That(withValue.Get("progress")!.Get("curve2_from_level")!.AsInt(), Is.EqualTo(320), "escrito por otra build: se deja");

            JsonNode noProgress = JsonText.Parse("{\"profile_schema\": 2}");
            ProfileMigrations.MigrateV2ToV3(noProgress);
            Assert.That(noProgress.Get("progress"), Is.Null);
            noProgress.Set(ProfileJson.SchemaKey, JsonNode.FromInt(3));
            Assert.That(ProfileJson.FromNode(noProgress).Progress.Curve2FromLevel, Is.EqualTo(1));

            // Determinista: el mismo documento da lo mismo.
            JsonNode a = JsonText.Parse("{\"profile_schema\": 2, \"progress\": {\"max_level\": 77}}");
            JsonNode b = JsonText.Parse("{\"profile_schema\": 2, \"progress\": {\"max_level\": 77}}");
            ProfileMigrations.MigrateV2ToV3(a);
            ProfileMigrations.MigrateV2ToV3(b);
            Assert.That(JsonText.Write(a), Is.EqualTo(JsonText.Write(b)));
        }

        [Test]
        public void Schema3WithoutTheField_ReadsTheNextUnplayedLevel_NeverLevel1()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 3, \"progress\": {\"max_level\": 410}}");
            Assert.That(p.Progress.Curve2FromLevel, Is.EqualTo(411));
            Profile bad = ProfileJson.Deserialize("{\"profile_schema\": 3, \"progress\": {\"max_level\": 410, \"curve2_from_level\": \"x\"}}");
            Assert.That(bad.Progress.Curve2FromLevel, Is.EqualTo(411), "otro tipo: el mismo respaldo, sin hacer corrupto el perfil");
            Profile zero = ProfileJson.Deserialize("{\"profile_schema\": 3, \"progress\": {\"max_level\": 10, \"curve2_from_level\": 0}}");
            Assert.That(zero.Progress.Curve2FromLevel, Is.EqualTo(1), "nunca menor que 1");
        }

        [Test]
        public void RoundTrips()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            p.Progress.MaxLevel = 330;
            p.Progress.Curve2FromLevel = 312;
            Profile back = ProfileJson.Deserialize(ProfileJson.Serialize(p));
            Assert.That(back.Progress.Curve2FromLevel, Is.EqualTo(312));
            Assert.That(p.Clone().Progress.Curve2FromLevel, Is.EqualTo(312));
        }

        // (from, jugado) de cada perfil → el resultado.
        [TestCase(301, 320, 301, 310, 301, TestName = "Merge_TwoCurve2Players_TheMinimum")]
        [TestCase(1, 0, 451, 450, 451, TestName = "Merge_Reinstall_EmptyProfileDoesNotLowerTheOther")]
        [TestCase(451, 450, 1, 0, 451, TestName = "Merge_Reinstall_EitherSide")]
        [TestCase(1, 12, 451, 450, 451, TestName = "Merge_NewPhoneWithAFewEarlyLevels_KeepsTheOldBoards")]
        [TestCase(321, 320, 451, 450, 451, TestName = "Merge_TwoCurve1Players_ProtectsBothCurve1Boards")]
        [TestCase(201, 200, 1, 0, 1, TestName = "Merge_NothingPlayedIn301To700_TheMinimum")]
        [TestCase(301, 350, 451, 450, 301, TestName = "Merge_EachDeviceWithACurve_TheAcceptedEdgeCase_TheMinimum")]
        public void Merge_TakesTheMinimum_WithoutChangingPlayedBoards(int fromA, int playedA, int fromB, int playedB, int expected)
        {
            Assert.That(ProgressData.MergedCurve2FromLevel(fromA, playedA, fromB, playedB), Is.EqualTo(expected));
            Assert.That(ProgressData.MergedCurve2FromLevel(fromB, playedB, fromA, playedA), Is.EqualTo(expected), "simétrico");
        }

        [Test]
        public void CloudResolve_TakesTheMinimum_WhicheverWins()
        {
            Profile local = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            local.Progress.MaxLevel = 360;
            local.Progress.Curve2FromLevel = 351;
            Profile cloud = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            cloud.Progress.MaxLevel = 320;
            cloud.Progress.Curve2FromLevel = 301;

            CloudMergeResult result = CloudMerge.Resolve(local, cloud);

            Assert.That(result.Merged.Progress.Curve2FromLevel, Is.EqualTo(301), "08 §4.3: en la fusión gana el mínimo (el caso límite aceptado)");
        }

        [Test]
        public void CloudResolve_ReinstallWithAMigratedCloud_KeepsTheCloudValue()
        {
            // Revisión de F4: un perfil nuevo (1, nada jugado) no baja el de la nube migrada a 451, gane quien gane.
            Profile local = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Profile cloud = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            cloud.Progress.MaxLevel = 450;
            cloud.Progress.NextLevel = 451;
            cloud.Progress.Curve2FromLevel = 451;

            Assert.That(CloudMerge.Resolve(local, cloud).Merged.Progress.Curve2FromLevel, Is.EqualTo(451));
            Assert.That(CloudMerge.Resolve(cloud, local).Merged.Progress.Curve2FromLevel, Is.EqualTo(451));
        }
    }
}
