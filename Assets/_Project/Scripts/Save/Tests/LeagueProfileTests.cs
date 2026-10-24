#nullable enable
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// D-046: <c>league</c> [P] en el perfil (08 §4.3): falta hasta el primer guardado, hace la ida y vuelta como objeto anidado, se
    /// descarta si no es un objeto (es cola y caché, nunca dinero) y es solo del dispositivo: no se firma, no sube a la nube y la
    /// fusión de 08 §11 se queda con la local.
    /// </summary>
    public sealed class LeagueProfileTests
    {
        private const string League = "{\"week\":\"2026-W43\",\"pending\":[{\"win_id\":\"c:57\",\"kind\":\"campaign\",\"level\":57,\"won_at\":\"2026-10-23T15:00:00Z\"}],\"counted\":[\"c:57\"],\"me\":null,\"cache\":{\"global\":null,\"country\":null}}";

        private static Profile NewProfile() => Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

        [Test]
        public void MissingByDefault_AndRoundTrips()
        {
            Profile p = NewProfile();
            Assert.That(p.League, Is.Null);
            Assert.That(ProfileJson.Serialize(p), Does.Not.Contain("\"league\""));

            p.League = League;
            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"league\": {", json);
            Profile back = ProfileJson.Deserialize(json);
            Assert.That(back.League, Does.Contain("\"win_id\": \"c:57\""));
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(json));
            Assert.That(p.Clone().League, Is.Not.Null);
        }

        [Test]
        public void NotAnObject_IsDiscarded()
        {
            Profile p = NewProfile();
            p.League = "no es json";
            Assert.That(ProfileJson.Deserialize(ProfileJson.Serialize(p)).League, Is.Null);
        }

        [Test]
        public void IsDeviceOnly_NotSignedNotUploaded_AndKeptInTheMerge()
        {
            Profile local = NewProfile();
            Profile cloud = local.Clone();
            local.League = League;
            Assert.That(CloudMerge.SharedJson(local), Is.EqualTo(CloudMerge.SharedJson(cloud)), "no viaja en el blob de la nube");
            Assert.That(ProfileIntegrity.Payload(local), Is.EqualTo(ProfileIntegrity.Payload(cloud)), "no cambia la firma");
            Assert.That(local.League, Is.EqualTo(League), "SharedJson no toca el perfil");

            cloud.Progress.MaxLevel = local.Progress.MaxLevel + 5; // gana la nube
            CloudMergeResult merged = CloudMerge.Resolve(local, cloud);
            Assert.That(merged.Winner, Is.EqualTo(CloudMergeWinner.Cloud));
            Assert.That(merged.Merged.League, Is.EqualTo(League), "la cola de la Liga es del dispositivo");
        }
    }
}
