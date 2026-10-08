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
    /// Guardado local atómico del perfil (B1-1; GDD 08 §4.3, §4.4 y §11): <c>profile.json.tmp</c> → flush →
    /// reemplazo con <c>profile.bak</c>; al cargar, principal corrupto o ausente → <c>.bak</c>; un <c>.tmp</c> huérfano se
    /// ignora; migraciones con copia previa y sin tocar nada si fallan; esquema más nuevo → no se pisa; eliminación de
    /// cuenta → no queda ningún archivo.
    /// </summary>
    public sealed class LocalProfileStoreTests
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private string _dir = "";

        [SetUp]
        public void SetUp()
        {
            _dir = TempDirs.Create();
        }

        [TearDown]
        public void TearDown()
        {
            TempDirs.Delete(_dir);
        }

        [Test]
        public void EmptyFolder_LoadsNotFound_AndWritesNothing()
        {
            var store = new LocalProfileStore(_dir);

            ProfileLoadResult result = store.Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.NotFound));
            Assert.That(result.HasProfile, Is.False);
            Assert.That(Directory.GetFiles(_dir), Is.Empty);
        }

        [Test]
        public void SaveThenLoad_RoundTripsExactly_WithoutLeftovers()
        {
            var store = new LocalProfileStore(_dir);
            Profile original = SampleProfiles.Full();

            store.Save(original);
            ProfileLoadResult result = new LocalProfileStore(_dir).Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(result.Migrated, Is.False);
            Assert.That(result.Error, Is.Null);
            Assert.That(SampleProfiles.Describe(result.Profile!), Is.EqualTo(SampleProfiles.Describe(original)));
            Assert.That(File.Exists(store.MainPath), Is.True);
            Assert.That(File.Exists(store.TempPath), Is.False, "el temporal se consume al reemplazar");
            Assert.That(File.Exists(store.BackupPath), Is.False, "en el primer guardado no hay estado anterior");
        }

        [Test]
        public void SavedFile_IsUtf8WithoutBom_AndMatchesTheSerializer()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();

            store.Save(p);
            byte[] bytes = File.ReadAllBytes(store.MainPath);

            Assert.That(bytes[0], Is.EqualTo((byte)'{'), "sin BOM");
            Assert.That(Utf8NoBom.GetString(bytes), Is.EqualTo(ProfileJson.Serialize(p)));
            Assert.That(store.LastSavedBytes, Is.EqualTo(bytes.Length));
        }

        [Test]
        public void EachSave_KeepsThePreviousStateInTheBackup()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            string first = File.ReadAllText(store.MainPath);

            p.Wallet.Seeds = 999;
            store.Save(p);

            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(first));
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(999));
        }

        [TestCase("garbage")]
        [TestCase("empty")]
        [TestCase("truncated")]
        [TestCase("invalid-utf8")]
        [TestCase("wrong-type")]
        public void CorruptMain_LoadsTheBackup(string corruption)
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 999;
            store.Save(p);
            Corrupt(store.MainPath, corruption);

            ProfileLoadResult result = new LocalProfileStore(_dir).Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.LoadedFromBackup));
            Assert.That(result.Profile!.Wallet.Seeds, Is.EqualTo(1240), "la copia es el estado anterior");
            StringAssert.Contains(LocalProfileStore.FileName, result.Error);
        }

        [Test]
        public void AfterLoadingTheBackup_TheNextSaveKeepsTheGoodBackup()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 999;
            store.Save(p);
            string goodBackup = File.ReadAllText(store.BackupPath);
            Corrupt(store.MainPath, "garbage");

            var reopened = new LocalProfileStore(_dir);
            Profile loaded = reopened.Load().Profile!;
            loaded.Wallet.Seeds = 5;
            reopened.Save(loaded);

            Assert.That(File.ReadAllText(reopened.BackupPath), Is.EqualTo(goodBackup), "el principal corrupto no pasa a ser la copia");
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(5));
        }

        [Test]
        public void MissingMain_LoadsTheBackup()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 999;
            store.Save(p);
            File.Delete(store.MainPath); // p. ej., un cierre entre los dos pasos del reemplazo en Unix

            ProfileLoadResult result = new LocalProfileStore(_dir).Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.LoadedFromBackup));
            Assert.That(result.Profile!.Wallet.Seeds, Is.EqualTo(1240));
        }

        [Test]
        public void OrphanTemp_IsIgnored_AndOverwrittenByTheNextSave()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            Profile newer = SampleProfiles.Full();
            newer.Wallet.Seeds = 777;
            File.WriteAllText(store.TempPath, ProfileJson.Serialize(newer), Utf8NoBom); // escrito, pero sin reemplazar

            ProfileLoadResult result = store.Load();
            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(result.Profile!.Wallet.Seeds, Is.EqualTo(1240), "un .tmp huérfano nunca se lee");

            File.WriteAllText(store.TempPath, "{ medio escrito", Utf8NoBom);
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.Loaded));

            store.Save(result.Profile!);
            Assert.That(File.Exists(store.TempPath), Is.False);
            Assert.That(store.Load().Profile!.Wallet.Seeds, Is.EqualTo(1240));
        }

        [Test]
        public void OnlyAnOrphanTemp_IsNotFound()
        {
            var store = new LocalProfileStore(_dir);
            File.WriteAllText(store.TempPath, ProfileJson.Serialize(SampleProfiles.Full()), Utf8NoBom);

            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.NotFound));
        }

        [Test]
        public void MainAndBackupCorrupt_AreUnreadable_ButSavingStillWorks()
        {
            var store = new LocalProfileStore(_dir);
            File.WriteAllText(store.MainPath, "{", Utf8NoBom);
            File.WriteAllText(store.BackupPath, "nada", Utf8NoBom);

            ProfileLoadResult result = store.Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.Unreadable));
            Assert.That(result.HasProfile, Is.False);
            StringAssert.Contains(LocalProfileStore.FileName, result.Error);
            StringAssert.Contains(LocalProfileStore.BackupFileName, result.Error);
            Assert.That(store.WriteBlocked, Is.False);

            store.Save(Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5));
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.Loaded));
        }

        [Test]
        public void NewerSchema_IsNotOverwritten()
        {
            var store = new LocalProfileStore(_dir);
            const string future = "{\"profile_schema\": 2, \"wallet\": {\"seeds\": 5000}}";
            File.WriteAllText(store.MainPath, future, Utf8NoBom);

            ProfileLoadResult result = store.Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.TooNew));
            Assert.That(result.HasProfile, Is.False);
            Assert.That(store.WriteBlocked, Is.True);
            Assert.Throws<InvalidOperationException>(() => store.Save(Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5)));
            Assert.That(File.ReadAllText(store.MainPath), Is.EqualTo(future));
            Assert.That(File.Exists(store.BackupPath), Is.False, "no se cae a una copia más vieja");
        }

        [Test]
        public void OlderSchema_IsMigratedInMemory_AfterBackingItUp()
        {
            // Esquema 0 sintético con una clave renombrada: la build real no tiene pasos todavía (esquema 1).
            const string v0 = "{\"profile_schema\": 0, \"coins\": 7}";
            var migrations = new ProfileMigrations(1, ProfileMigrations.Step(0, RenameCoinsToSeeds));
            var store = new LocalProfileStore(_dir, migrations);
            File.WriteAllText(store.MainPath, v0, Utf8NoBom);

            ProfileLoadResult result = store.Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(result.Migrated, Is.True);
            Assert.That(result.Profile!.Wallet.Seeds, Is.EqualTo(7));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(v0), "08 §4.4: antes de migrar se escribe profile.bak");
            Assert.That(File.ReadAllText(store.MainPath), Is.EqualTo(v0), "cargar no reescribe el principal");

            store.Save(result.Profile!);
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(7));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(v0));
        }

        [Test]
        public void FailedMigration_KeepsThePreviousProfile_AndBlocksWrites()
        {
            const string v0 = "{\"profile_schema\": 0, \"coins\": 7}";
            var migrations = new ProfileMigrations(1, ProfileMigrations.Step(0, _ => throw new InvalidOperationException("paso roto")));
            var store = new LocalProfileStore(_dir, migrations);
            File.WriteAllText(store.MainPath, v0, Utf8NoBom);

            ProfileLoadResult result = store.Load();

            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.MigrationFailed));
            Assert.That(result.HasProfile, Is.False);
            StringAssert.Contains("paso roto", result.Error);
            Assert.That(File.ReadAllText(store.MainPath), Is.EqualTo(v0));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(v0));
            Assert.That(store.WriteBlocked, Is.True);
            Assert.Throws<InvalidOperationException>(() => store.Save(Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5)));
        }

        [Test]
        public void OlderSchemaWithoutAStep_IsAFailedMigration()
        {
            var store = new LocalProfileStore(_dir);
            File.WriteAllText(store.MainPath, "{\"profile_schema\": 0}", Utf8NoBom);

            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.MigrationFailed));
        }

        [Test]
        public void Delete_RemovesEveryFile_AndLiftsTheWriteBlock()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            store.Save(p);
            File.WriteAllText(store.TempPath, "x", Utf8NoBom);
            File.WriteAllText(store.MainPath, "{\"profile_schema\": 9}", Utf8NoBom);
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.TooNew));

            store.Delete();

            Assert.That(Directory.GetFiles(_dir), Is.Empty, "08 §11: se borran profile.json y profile.bak");
            Assert.That(store.WriteBlocked, Is.False);
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.NotFound));
            store.Save(p);
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.Loaded));
        }

        [Test]
        public void Save_CreatesTheFolderIfNeeded()
        {
            string nested = Path.Combine(_dir, "a", "b");
            var store = new LocalProfileStore(nested);

            store.Save(SampleProfiles.Full());

            Assert.That(File.Exists(Path.Combine(nested, LocalProfileStore.FileName)), Is.True);
        }

        [Test]
        public void Constructor_RequiresAnAbsoluteFolder_AndCurrentMigrations()
        {
            Assert.Throws<ArgumentException>(() => new LocalProfileStore("perfil"));
            Assert.Throws<ArgumentException>(() => new LocalProfileStore(""));
            Assert.Throws<ArgumentException>(() => new LocalProfileStore(_dir, new ProfileMigrations(2)));
            Assert.That(ProfileMigrations.Current.TargetSchema, Is.EqualTo(Profile.CurrentSchema));
        }

        [Test]
        public void MemoryStore_ReturnsIndependentCopies()
        {
            var store = new MemoryProfileStore();
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.NotFound));

            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 1;
            Profile loaded = store.Load().Profile!;
            loaded.Wallet.Seeds = 2;

            Assert.That(store.Load().Profile!.Wallet.Seeds, Is.EqualTo(1240));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            store.Delete();
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.NotFound));
        }

        private static void RenameCoinsToSeeds(JsonNode root)
        {
            JsonNode wallet = JsonNode.NewObject();
            wallet.Set("seeds", JsonNode.FromInt(root.Get("coins")!.AsInt()));
            root.Remove("coins");
            root.Set("wallet", wallet);
        }

        private static void Corrupt(string path, string corruption)
        {
            byte[] bytes = File.ReadAllBytes(path);
            switch (corruption)
            {
                case "garbage":
                    File.WriteAllText(path, "\u0000\u0001 no es JSON", Utf8NoBom);
                    break;
                case "empty":
                    File.WriteAllBytes(path, new byte[0]);
                    break;
                case "truncated":
                    Array.Resize(ref bytes, bytes.Length / 2);
                    File.WriteAllBytes(path, bytes);
                    break;
                case "invalid-utf8":
                    bytes[bytes.Length / 2] = 0xFF;
                    File.WriteAllBytes(path, bytes);
                    break;
                case "wrong-type":
                    File.WriteAllText(path, "{\"profile_schema\": 1, \"wallet\": {\"seeds\": \"mil\"}}", Utf8NoBom);
                    break;
                default:
                    throw new ArgumentException(corruption);
            }
        }
    }
}
