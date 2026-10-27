#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// D-056: <c>avatar</c> [P] en el perfil (08 §4.3; avatares.md §4.1): falta hasta el primer avatar, hace la ida y vuelta con las
    /// listas en orden y sin repetidos, se lee de forma tolerante, entra en la firma y en el blob de la nube, y la fusión de 08 §11 une
    /// lo desbloqueado y lo visto y se queda con la elección más reciente.
    /// </summary>
    public sealed class AvatarProfileTests
    {
        private static readonly DateTime At1 = new DateTime(2026, 10, 27, 15, 4, 5, DateTimeKind.Utc);

        private static Profile NewProfile() => Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

        [Test]
        public void MissingByDefault_AndRoundTrips()
        {
            Profile p = NewProfile();
            Assert.That(p.Avatar.IsEmpty, Is.True);
            Assert.That(ProfileJson.Serialize(p), Does.Not.Contain("\"avatar\""), "sin migración: un perfil sin avatar se escribe igual");

            p.Avatar.Id = "toucan";
            p.Avatar.Chosen = true;
            p.Avatar.At = At1;
            p.Avatar.AddUnlocked("otter");
            p.Avatar.AddUnlocked("dart_frog");
            p.Avatar.AddUnlocked("otter");
            p.Avatar.AddSeen("dart_frog");
            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"avatar\": {", json);
            Profile back = ProfileJson.Deserialize(json);
            Assert.That(back.Avatar.Id, Is.EqualTo("toucan"));
            Assert.That(back.Avatar.Chosen, Is.True);
            Assert.That(back.Avatar.At, Is.EqualTo(At1));
            Assert.That(back.Avatar.Unlocked, Is.EqualTo(new[] { "dart_frog", "otter" }), "orden ordinal y sin repetidos");
            Assert.That(back.Avatar.Seen, Is.EqualTo(new[] { "dart_frog" }));
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(json));
        }

        [Test]
        public void IsReadTolerantly()
        {
            Profile p = NewProfile();
            string json = ProfileJson.Serialize(p);
            int brace = json.LastIndexOf('}');
            string bad = json.Substring(0, brace).TrimEnd() + ",\n  \"avatar\": {\"id\": \"Tucán!\", \"chosen\": \"sí\", \"at\": \"ayer\", \"unlocked\": [\"otter\", 7, \"BAD ID\"], \"seen\": 3}\n}";
            Profile back = ProfileJson.Deserialize(bad);
            Assert.That(back.Avatar.Id, Is.Null, "un id con otra forma se reasigna al cargar");
            Assert.That(back.Avatar.Chosen, Is.False);
            Assert.That(back.Avatar.At, Is.Null);
            Assert.That(back.Avatar.Unlocked, Is.EqualTo(new[] { "otter" }));
            Assert.That(back.Avatar.Seen, Is.Empty);
        }

        [Test]
        public void IsSigned_AndTravelsInTheCloudBlob()
        {
            Profile a = NewProfile();
            Profile b = a.Clone();
            b.Avatar.Id = "macaw";
            Assert.That(CloudMerge.SharedJson(a), Is.Not.EqualTo(CloudMerge.SharedJson(b)), "sube a la nube");
            Assert.That(ProfileIntegrity.Payload(a), Is.Not.EqualTo(ProfileIntegrity.Payload(b)), "entra en la firma");
        }

        [Test]
        public void Merge_UnionsTheLists_AndTheLatestChoiceWins()
        {
            Profile local = NewProfile();
            Profile cloud = local.Clone();
            local.Avatar.Id = "jaguar";
            local.Avatar.Chosen = true;
            local.Avatar.At = At1.AddHours(2); // la elección más reciente
            local.Avatar.AddUnlocked("jaguar");
            local.Avatar.AddSeen("jaguar");
            cloud.Avatar.Id = "toucan";
            cloud.Avatar.Chosen = true;
            cloud.Avatar.At = At1;
            cloud.Avatar.AddUnlocked("otter");
            cloud.Progress.MaxLevel = local.Progress.MaxLevel + 20; // la nube gana por nivel

            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Cloud));
            AvatarData m = r.Merged.Avatar;
            Assert.That(m.Id, Is.EqualTo("jaguar"), "la elección es una preferencia: gana la más reciente, no el blob con más nivel");
            Assert.That(m.At, Is.EqualTo(At1.AddHours(2)));
            Assert.That(m.Unlocked, Is.EqualTo(new[] { "jaguar", "otter" }));
            Assert.That(m.Seen, Is.EqualTo(new[] { "jaguar" }));
            Assert.That(m.Chosen, Is.True);
        }

        [Test]
        public void Merge_WithoutAt_KeepsTheWinners_OrTakesTheOtherIfItHasNone()
        {
            var winner = new AvatarData { Id = "macaw" };
            var loser = new AvatarData { Id = "tapir" };
            winner.Merge(loser);
            Assert.That(winner.Id, Is.EqualTo("macaw"), "sin at en ninguno, el del ganador");

            var empty = new AvatarData();
            empty.Merge(new AvatarData { Id = "tapir", Chosen = false });
            Assert.That(empty.Id, Is.EqualTo("tapir"));

            var chosen = new AvatarData { Id = "macaw", Chosen = true, At = At1 };
            chosen.Merge(new AvatarData { Id = "tapir" });
            Assert.That(chosen.Id, Is.EqualTo("macaw"), "una elección con fecha gana a un default sin fecha");
        }

        [Test]
        public void Ids_HaveTheirShape()
        {
            Assert.That(AvatarData.IsId("dart_frog"), Is.True);
            Assert.That(AvatarData.IsId(""), Is.False);
            Assert.That(AvatarData.IsId("Dart"), Is.False);
            Assert.That(AvatarData.IsId(new string('a', 33)), Is.False);
            Assert.Throws<ArgumentException>(() => new AvatarData().AddUnlocked("No"));
        }
    }
}
