#nullable enable
using System;
using System.IO;
using System.Text;
using AF.Platform;
using AF.Save.Json;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>fauna.sighted</c> del perfil y su migración (Q11 del guion de la intro, D-029; tarea DC15): el paso 1 → 2 añade
    /// <c>fauna</c> vacío sin perder nada del perfil y es determinista (un perfil del esquema 1 se lee igual que el mismo perfil
    /// escrito por esta build); <c>RecordSighting</c> es idempotente (la segunda vez de la misma especie, false y la lista no
    /// crece) y guarda las especies en orden ordinal con su primer nivel; ida y vuelta por JSON; lectura tolerante (una entrada
    /// inválida se descarta y una especie repetida se queda con su nivel más bajo); fusión de la nube (unión con el nivel más
    /// bajo); y un principal antiguo con un tipo equivocado no pisa la copia buena al migrar.
    /// </summary>
    public sealed class FaunaSightingTests
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private string? _dir;

        [TearDown]
        public void TearDown()
        {
            TempDirs.Delete(_dir);
            _dir = null;
        }

        [Test]
        public void NewProfile_HasNoSightings_AndWritesAnEmptyList()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

            Assert.That(p.Fauna.Sighted, Is.Empty);
            Assert.That(Profile.CurrentSchema, Is.EqualTo(2));
            StringAssert.EndsWith("  \"fauna\": {\n    \"sighted\": []\n  }\n}\n", ProfileJson.Serialize(p));
        }

        [Test]
        public void MigrateV1ToV2_AddsAnEmptyFauna_AndKeepsEveryOtherKey()
        {
            Profile full = SampleProfiles.Full();
            string current = ProfileJson.Serialize(full);
            string v1 = AsV1(current);
            JsonNode doc = JsonText.Parse(v1);
            int before = doc.Members.Count;

            ProfileMigrations.MigrateV1ToV2(doc);

            Assert.That(doc.Members.Count, Is.EqualTo(before + 1), "solo se añade fauna");
            Assert.That(doc.Members[doc.Members.Count - 1].Key, Is.EqualTo("fauna"));
            Assert.That(doc.Get("fauna")!.Get("sighted")!.Items, Is.Empty);
            doc.Set(ProfileJson.SchemaKey, JsonNode.FromInt(2));
            Assert.That(JsonText.Write(doc), Is.EqualTo(current), "el documento migrado es el que escribe esta build");
        }

        [Test]
        public void MigrateV1ToV2_IsDeterministic_AndLeavesAnExistingFaunaAsItIs()
        {
            string v1 = AsV1(ProfileJson.Serialize(SampleProfiles.Full()));
            JsonNode a = JsonText.Parse(v1);
            JsonNode b = JsonText.Parse(v1);
            ProfileMigrations.MigrateV1ToV2(a);
            ProfileMigrations.MigrateV1ToV2(b);
            Assert.That(JsonText.Write(a), Is.EqualTo(JsonText.Write(b)));

            JsonNode withFauna = JsonText.Parse("{\"profile_schema\": 1, \"fauna\": {\"sighted\": [{\"species_id\": \"mico_argentatus\", \"level\": 21}]}}");
            ProfileMigrations.MigrateV1ToV2(withFauna);
            withFauna.Set(ProfileJson.SchemaKey, JsonNode.FromInt(2));
            Profile p = ProfileJson.FromNode(withFauna);
            Assert.That(p.Fauna.Sighted.Count, Is.EqualTo(1));
            Assert.That(p.Fauna.Sighted[0].SpeciesId, Is.EqualTo("mico_argentatus"));
            Assert.That(p.Fauna.Sighted[0].Level, Is.EqualTo(21));
        }

        [Test]
        public void V1ProfileOnDisk_IsMigratedWithoutLoss_AndBackedUp()
        {
            _dir = TempDirs.Create();
            var store = new LocalProfileStore(_dir);
            Profile full = SampleProfiles.Full();
            string current = ProfileJson.Serialize(full);
            string v1 = AsV1(current);
            File.WriteAllText(store.MainPath, v1, Utf8NoBom);

            ProfileLoadResult load = store.Load();

            Assert.That(load.Status, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(load.Migrated, Is.True);
            Assert.That(ProfileJson.Serialize(load.Profile!), Is.EqualTo(current), "perfil viejo → nuevo sin pérdida");
            Assert.That(load.Profile!.Fauna.Sighted, Is.Empty);
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(v1), "08 §4.4: antes de migrar, profile.bak");
            Assert.That(File.ReadAllText(store.MainPath), Is.EqualTo(v1), "cargar no reescribe el principal");

            load.Profile!.Fauna.RecordSighting("saimiri_collinsi", 5);
            store.Save(load.Profile!);
            Profile again = new LocalProfileStore(_dir).Load().Profile!;
            Assert.That(again.Fauna.HasSighted("saimiri_collinsi"), Is.True);
            Assert.That(again.Wallet.Seeds, Is.EqualTo(full.Wallet.Seeds));
            Assert.That(again.Progress.MapSeenLevel, Is.EqualTo(full.Progress.MapSeenLevel));
        }

        [Test]
        public void V1MainWithAWrongType_DoesNotOverwriteAGoodBackup()
        {
            _dir = TempDirs.Create();
            var store = new LocalProfileStore(_dir);
            string goodV1 = AsV1(ProfileJson.Serialize(SampleProfiles.Full()));
            const string badV1 = "{\"profile_schema\": 1, \"wallet\": {\"seeds\": \"mil\"}}";
            File.WriteAllText(store.BackupPath, goodV1, Utf8NoBom);
            File.WriteAllText(store.MainPath, badV1, Utf8NoBom);

            ProfileLoadResult load = store.Load();

            Assert.That(load.Status, Is.EqualTo(ProfileLoadStatus.LoadedFromBackup));
            Assert.That(load.Migrated, Is.True);
            Assert.That(load.Profile!.Wallet.Seeds, Is.EqualTo(1240));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(goodV1), "la copia buena no se pisa con el principal roto");
        }

        [Test]
        public void RecordSighting_IsIdempotent_KeepsTheFirstLevel_AndSortsOrdinally()
        {
            var fauna = new FaunaData();

            Assert.That(fauna.RecordSighting("saimiri_collinsi", 5), Is.True);
            Assert.That(fauna.RecordSighting("mico_argentatus", 21), Is.True);
            Assert.That(fauna.RecordSighting("saimiri_collinsi", 9), Is.False, "la segunda vez de la misma especie");
            Assert.That(fauna.RecordSighting("ateles_chamek", 141), Is.True);
            Assert.That(fauna.RecordSighting("cacajao_calvus_ucayalii", 81), Is.True);

            Assert.That(fauna.Sighted.Count, Is.EqualTo(4), "la lista no crece con la repetida");
            Assert.That(fauna.Sighted[0].SpeciesId, Is.EqualTo("ateles_chamek"));
            Assert.That(fauna.Sighted[1].SpeciesId, Is.EqualTo("cacajao_calvus_ucayalii"));
            Assert.That(fauna.Sighted[2].SpeciesId, Is.EqualTo("mico_argentatus"));
            Assert.That(fauna.Sighted[3].SpeciesId, Is.EqualTo("saimiri_collinsi"));
            Assert.That(fauna.Find("saimiri_collinsi")!.Level, Is.EqualTo(5), "se conserva su primer nivel");
            Assert.That(fauna.Find("aotus_trivirgatus"), Is.Null);
            Assert.That(fauna.HasSighted("aotus_trivirgatus"), Is.False);
        }

        [Test]
        public void RecordSighting_RejectsBadIdsAndLevels()
        {
            var fauna = new FaunaData();

            Assert.Throws<ArgumentException>(() => fauna.RecordSighting("", 5));
            Assert.Throws<ArgumentException>(() => fauna.RecordSighting("Saimiri collinsi", 5));
            Assert.Throws<ArgumentException>(() => fauna.RecordSighting(new string('a', FaunaData.MaxSpeciesIdLength + 1), 5));
            Assert.Throws<ArgumentException>(() => fauna.RecordSighting(null!, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => fauna.RecordSighting("saimiri_collinsi", 0));
            Assert.That(fauna.Sighted, Is.Empty);
        }

        [Test]
        public void Sightings_RoundTrip_InOrdinalOrder()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            p.Fauna.RecordSighting("saimiri_collinsi", 5);
            p.Fauna.RecordSighting("mico_argentatus", 21);

            string json = ProfileJson.Serialize(p);
            StringAssert.EndsWith("  \"fauna\": {\n    \"sighted\": [\n"
                + "      {\n        \"species_id\": \"mico_argentatus\",\n        \"level\": 21\n      },\n"
                + "      {\n        \"species_id\": \"saimiri_collinsi\",\n        \"level\": 5\n      }\n"
                + "    ]\n  }\n}\n", json);
            Profile back = ProfileJson.Deserialize(json);
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(json));
            Assert.That(back.Clone().Fauna.Sighted.Count, Is.EqualTo(2));
        }

        [Test]
        public void Reading_DropsInvalidEntries_KeepsTheLowestLevelOfARepeat_AndNeverCorrupts()
        {
            const string json = "{\"profile_schema\": 2, \"wallet\": {\"seeds\": 30}, \"fauna\": {\"sighted\": ["
                + "{\"species_id\": \"saimiri_collinsi\", \"level\": 9},"
                + "{\"species_id\": \"saimiri_collinsi\", \"level\": 5},"
                + "{\"species_id\": \"Mico argentatus\", \"level\": 21},"
                + "{\"species_id\": 7, \"level\": 21},"
                + "{\"species_id\": \"ateles_chamek\", \"level\": 0},"
                + "{\"species_id\": \"ateles_chamek\", \"level\": \"141\"},"
                + "{\"species_id\": \"aotus_trivirgatus\"},"
                + "\"pithecia_pithecia\", 3, null,"
                + "{\"species_id\": \"mico_argentatus\", \"level\": 21, \"extra\": true}]}}";

            Profile p = ProfileJson.Deserialize(json);

            Assert.That(p.Wallet.Seeds, Is.EqualTo(30));
            Assert.That(p.Fauna.Sighted.Count, Is.EqualTo(2));
            Assert.That(p.Fauna.Sighted[0].SpeciesId, Is.EqualTo("mico_argentatus"));
            Assert.That(p.Fauna.Sighted[1].SpeciesId, Is.EqualTo("saimiri_collinsi"));
            Assert.That(p.Fauna.Sighted[1].Level, Is.EqualTo(5), "una especie repetida se queda con su nivel más bajo");

            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2, \"fauna\": []}").Fauna.Sighted, Is.Empty);
            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2, \"fauna\": {\"sighted\": {}}}").Fauna.Sighted, Is.Empty);
            Assert.That(ProfileJson.Deserialize("{\"profile_schema\": 2}").Fauna.Sighted, Is.Empty);
        }

        [Test]
        public void MergeSightings_IsTheUnion_WithTheLowestLevel()
        {
            var winner = new FaunaData();
            winner.RecordSighting("saimiri_collinsi", 9);
            winner.RecordSighting("mico_argentatus", 21);
            var loser = new FaunaData();
            loser.RecordSighting("saimiri_collinsi", 5);
            loser.RecordSighting("ateles_chamek", 141);

            winner.MergeSightings(loser);

            Assert.That(winner.Sighted.Count, Is.EqualTo(3));
            Assert.That(winner.Sighted[0].SpeciesId, Is.EqualTo("ateles_chamek"));
            Assert.That(winner.Find("saimiri_collinsi")!.Level, Is.EqualTo(5));
            Assert.That(winner.Find("mico_argentatus")!.Level, Is.EqualTo(21));
            Assert.That(loser.Sighted.Count, Is.EqualTo(2), "el perdedor no cambia");
            Assert.Throws<ArgumentNullException>(() => winner.MergeSightings(null!));
        }

        // El mismo documento en el esquema 1: sin fauna y con profile_schema 1 (como lo escribía la build anterior a DC15).
        private static string AsV1(string current)
        {
            JsonNode doc = JsonText.Parse(current);
            Assert.That(doc.Remove("fauna"), Is.True);
            doc.Set(ProfileJson.SchemaKey, JsonNode.FromInt(1));
            return JsonText.Write(doc);
        }
    }
}
