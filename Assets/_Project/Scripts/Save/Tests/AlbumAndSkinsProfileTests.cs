#nullable enable
using AF.Platform;
using AF.Save.Json;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Los campos de B14 en el perfil (esquema 2, aditivos y sin migración propia: aún no se ha publicado): <c>album.fruits</c>
    /// (08 §4.3), <c>entitlements.skin_equipped</c> [P] (Q-M8 (a)) y <c>entitlements.frames</c> [P] (Q-M6 (a)). Un perfil del
    /// esquema 1 migra sin ellos y un perfil del 2 sin las claves los lee en su default; en su default no se escriben (el documento
    /// de esta build no cambia); ida y vuelta; lectura tolerante (un valor que no vale no hace corrupto el perfil); fusión de la nube.
    /// </summary>
    public sealed class AlbumAndSkinsProfileTests
    {
        [Test]
        public void Defaults_AreEmpty_AndAreNotWritten()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

            Assert.That(p.Album.Fruits, Is.Empty);
            Assert.That(p.Entitlements.SkinEquipped, Is.Null);
            Assert.That(p.Entitlements.Frames, Is.Empty);
            string json = ProfileJson.Serialize(p);
            StringAssert.DoesNotContain("\"album\"", json);
            StringAssert.DoesNotContain("skin_equipped", json);
            StringAssert.DoesNotContain("\"frames\"", json);
        }

        [Test]
        public void V2ProfileWithoutTheKeys_ReadsTheDefaults()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"entitlements\": {\"no_ads\": false, \"starter_bought\": false, \"skins\": [\"af_skin_m02\"]}}");

            Assert.That(p.Album.Fruits, Is.Empty);
            Assert.That(p.Entitlements.SkinEquipped, Is.Null);
            Assert.That(p.Entitlements.Frames, Is.Empty);
            Assert.That(p.Entitlements.Skins, Is.EqualTo(new[] { "af_skin_m02" }));
        }

        [Test]
        public void V1Profile_MigratesToTheDefaults_WithoutChangingTheDocument()
        {
            string current = ProfileJson.Serialize(SampleProfiles.Full());
            JsonNode doc = JsonText.Parse(current);
            Assert.That(doc.Remove("fauna"), Is.True);
            doc.Set(ProfileJson.SchemaKey, JsonNode.FromInt(1));

            ProfileMigrations.Current.Migrate(doc, 1);
            Profile migrated = ProfileJson.FromNode(doc);

            Assert.That(migrated.Album.Fruits, Is.Empty);
            Assert.That(migrated.Entitlements.SkinEquipped, Is.Null);
            Assert.That(migrated.Entitlements.Frames, Is.Empty);
            Assert.That(ProfileJson.Serialize(migrated), Is.EqualTo(current), "los campos nuevos en su default no cambian el archivo");
        }

        [Test]
        public void Values_RoundTrip_AsIfTheAppWasClosed()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Assert.That(p.Album.AddFruit(8), Is.True);
            Assert.That(p.Album.AddFruit(4), Is.True);
            Assert.That(p.Album.AddFruit(8), Is.False, "un tinte ya descubierto no se repite");
            p.Entitlements.Skins.Add("af_skin_m02");
            p.Entitlements.SkinEquipped = "af_skin_m02";
            p.Entitlements.Frames.Add("frame_br");

            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"album\": {\n    \"fruits\": [4, 8]\n  }", json);
            Profile back = ProfileJson.Deserialize(json);

            Assert.That(back.Album.Fruits, Is.EqualTo(new[] { 4, 8 }));
            Assert.That(back.Entitlements.SkinEquipped, Is.EqualTo("af_skin_m02"));
            Assert.That(back.Entitlements.Frames, Is.EqualTo(new[] { "frame_br" }));
            Assert.That(p.Clone().Album.Fruits, Is.EqualTo(new[] { 4, 8 }));
        }

        [Test]
        public void BadValues_AreDropped_WithoutCorruptingTheProfile()
        {
            Profile p = ProfileJson.Deserialize(
                "{\"profile_schema\": 2, \"entitlements\": {\"skins\": [], \"skin_equipped\": 7, \"frames\": [\"a\", 3, \"a\"]},"
                + " \"album\": {\"fruits\": [8, 0, 15, \"x\", 8, 1]}}");

            Assert.That(p.Entitlements.SkinEquipped, Is.Null);
            Assert.That(p.Entitlements.Frames, Is.EqualTo(new[] { "a" }));
            Assert.That(p.Album.Fruits, Is.EqualTo(new[] { 1, 8 }));
            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2, \"album\": 3}").Album.Fruits, Is.Empty);
            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2, \"album\": {\"fruits\": {}}}").Album.Fruits, Is.Empty);
        }

        [Test]
        public void AddFruit_RejectsWhatIsNotATint()
        {
            var album = new AlbumData();

            Assert.That(() => album.AddFruit(0), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => album.AddFruit(15), Throws.InstanceOf<System.ArgumentOutOfRangeException>(), "15 es el Barro");
            Assert.That(album.HasFruit(3), Is.False);
            album.AddFruit(14);
            Assert.That(album.HasFruit(14), Is.True);
        }

        [Test]
        public void Merge_IsTheUnion_AndKeepsTheWinnersEquippedSkin()
        {
            Profile winner = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            Profile loser = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            winner.Album.AddFruit(8);
            loser.Album.AddFruit(4);
            loser.Album.AddFruit(8);
            winner.Entitlements.Skins.Add("af_skin_m02");
            winner.Entitlements.SkinEquipped = "af_skin_m02";
            loser.Entitlements.Skins.Add("af_skin_m03");
            loser.Entitlements.Skins.Add("af_skin_m02");
            loser.Entitlements.SkinEquipped = "af_skin_m03";
            loser.Entitlements.Frames.Add("frame_co");

            winner.Album.MergeFruits(loser.Album);
            winner.Entitlements.MergeOwned(loser.Entitlements);

            Assert.That(winner.Album.Fruits, Is.EqualTo(new[] { 4, 8 }));
            Assert.That(winner.Entitlements.Skins, Is.EqualTo(new[] { "af_skin_m02", "af_skin_m03" }));
            Assert.That(winner.Entitlements.Frames, Is.EqualTo(new[] { "frame_co" }));
            Assert.That(winner.Entitlements.SkinEquipped, Is.EqualTo("af_skin_m02"));
            Assert.That(() => winner.Album.MergeFruits(null!), Throws.ArgumentNullException);
            Assert.That(() => winner.Entitlements.MergeOwned(null!), Throws.ArgumentNullException);
        }
    }
}
