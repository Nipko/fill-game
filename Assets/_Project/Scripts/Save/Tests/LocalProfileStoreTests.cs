#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading;
using AF.Platform;
using AF.Save.Json;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Guardado local atómico del perfil (B1-1; GDD 08 §4.3, §4.4 y §11): <c>profile.json.tmp</c> → flush →
    /// reemplazo con <c>profile.bak</c>; al cargar, principal corrupto o ausente → <c>.bak</c>; un <c>.tmp</c> huérfano se
    /// ignora; migraciones con copia previa y sin tocar nada si fallan; esquema más nuevo → no se pisa; eliminación de
    /// cuenta → no queda ningún archivo. Escritor en segundo plano (T-D, <see cref="ProfileWriter"/>): un corte con la
    /// escritura a medias nunca deja un perfil corrupto, la cola de 1 la gana el último y sus fallos no salen de su hilo.
    /// </summary>
    public sealed class LocalProfileStoreTests
    {
        // Tope de las esperas al hilo del escritor (no se alcanza si todo va bien).
        private const int WaitMs = 10000;

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private string _dir = "";
        private int _mainThread;

        [SetUp]
        public void SetUp()
        {
            _dir = TempDirs.Create();
            _mainThread = Thread.CurrentThread.ManagedThreadId;
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

        [TestCase("TempHalfWritten", false)]
        [TestCase("TempWritten", false)]
        [TestCase("MainMovedToBackup", true)]
        public void ACutMidWrite_LoadsTheMainOrTheBackup_NeverACorruptProfile(string stage, bool moveFallback)
        {
            // T-D: el proceso muere en ese punto de la escritura (el gancho lanza y nada de lo que sigue se ejecuta). Al
            // reabrir, Load da el principal o la copia con el último perfil completo, nunca uno corrupto. (Un corte en
            // TempWritten con ForceMoveFallback sería el mismo caso: el reemplazo de dos Move solo actúa después, en Commit.)
            var store = new LocalProfileStore(_dir) { ForceMoveFallback = moveFallback };
            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 999;
            store.Save(p);
            LocalProfileStore.WriteStage cut = Stage(stage);
            store.WriteHook = s =>
            {
                if (s == cut)
                {
                    throw new ProcessKilled();
                }
            };

            p.Wallet.Seeds = 5;
            Assert.Throws<ProcessKilled>(() => store.Save(p));

            var reopened = new LocalProfileStore(_dir);
            ProfileLoadResult result = reopened.Load();
            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.Loaded).Or.EqualTo(ProfileLoadStatus.LoadedFromBackup), stage);
            Assert.That(result.Profile!.Wallet.Seeds, Is.EqualTo(999), "el último perfil escrito entero");

            // La sesión siguiente guarda sin problemas sobre lo que quedó (.tmp huérfano o principal ausente).
            Profile loaded = result.Profile;
            loaded.Wallet.Seeds = 7;
            reopened.Save(loaded);
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(7));
            Assert.That(File.Exists(reopened.TempPath), Is.False);
            Assert.That(new LocalProfileStore(_dir).Load().Status, Is.EqualTo(ProfileLoadStatus.Loaded));
        }

        [Test]
        public void ACutAfterDiscardingACorruptMain_LoadsTheBackup()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 999;
            store.Save(p);
            Corrupt(store.MainPath, "garbage");

            var reopened = new LocalProfileStore(_dir);
            Profile loaded = reopened.Load().Profile!;
            reopened.WriteHook = s =>
            {
                if (s == LocalProfileStore.WriteStage.CorruptMainDeleted)
                {
                    throw new ProcessKilled();
                }
            };

            loaded.Wallet.Seeds = 5;
            Assert.Throws<ProcessKilled>(() => reopened.Save(loaded));

            ProfileLoadResult result = new LocalProfileStore(_dir).Load();
            Assert.That(result.Status, Is.EqualTo(ProfileLoadStatus.LoadedFromBackup));
            Assert.That(result.Profile!.Wallet.Seeds, Is.EqualTo(1240), "la copia buena sigue ahí");
        }

        [TestCase("TempHalfWritten", false)]
        [TestCase("TempWritten", false)]
        [TestCase("TempWritten", true)]
        [TestCase("MainMovedToBackup", true)]
        public void WithTheWriterHalfway_ACutLeavesAWholeProfileOnDisk(string stage, bool moveFallback)
        {
            // T-D: el escritor de segundo plano se para en ese punto (como si el force-stop llegara ahí) y otro almacén lee la
            // carpeta, como la app al reabrir: el principal o la copia, completos.
            var store = new LocalProfileStore(_dir) { ForceMoveFallback = moveFallback };
            Profile p = SampleProfiles.Full();
            store.Save(p);
            p.Wallet.Seeds = 999;
            store.Save(p);
            LocalProfileStore.WriteStage cut = Stage(stage);
            var reached = new ManualResetEventSlim(false);
            var release = new ManualResetEventSlim(false);
            store.WriteHook = s =>
            {
                if (s == cut)
                {
                    reached.Set();
                    release.Wait(WaitMs);
                }
            };

            var writer = new ProfileWriter(store);
            try
            {
                p.Wallet.Seeds = 5;
                writer.Enqueue(store.Serialize(p));
                Assert.That(reached.Wait(WaitMs), Is.True, "el escritor llega a " + stage);
                Assert.That(writer.IsIdle, Is.False);

                ProfileLoadResult midway = new LocalProfileStore(_dir).Load();
                Assert.That(midway.Status, Is.EqualTo(ProfileLoadStatus.Loaded).Or.EqualTo(ProfileLoadStatus.LoadedFromBackup), stage);
                Assert.That(midway.Profile!.Wallet.Seeds, Is.EqualTo(999), "nunca un perfil a medias");
            }
            finally
            {
                release.Set();
            }

            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Assert.That(writer.TakeFailure(out _), Is.Null);
            Assert.That(writer.AllWritten, Is.True);
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(5));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(ProfileJson.Serialize(WithSeeds(999))), "el anterior pasa a la copia");
            Assert.That(File.Exists(store.TempPath), Is.False);
        }

        [Test]
        public void Writer_QueueOfOne_TheLatestWins()
        {
            var store = new LocalProfileStore(_dir);
            var reached = new ManualResetEventSlim(false);
            var release = new ManualResetEventSlim(false);
            int written = 0;
            store.WriteHook = s =>
            {
                if (s == LocalProfileStore.WriteStage.TempWritten && Interlocked.Increment(ref written) == 1)
                {
                    reached.Set();
                    release.Wait(WaitMs);
                }
            };

            // Marca de tiempo de prueba que avanza 5 ticks (5 ms) en cada lectura: el escritor lee una al empezar y otra al acabar.
            long ticks = 0;
            var writer = new ProfileWriter(store, () => Interlocked.Add(ref ticks, 5), 1000);
            try
            {
                writer.Enqueue(store.Serialize(WithSeeds(1)));
                Assert.That(reached.Wait(WaitMs), Is.True);
                writer.Enqueue(store.Serialize(WithSeeds(2)));
                writer.Enqueue(store.Serialize(WithSeeds(3)));
                writer.Enqueue(store.Serialize(WithSeeds(4)));
            }
            finally
            {
                release.Set();
            }

            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Assert.That(writer.BackgroundWrites, Is.EqualTo(2), "la primera, ya empezada, y la última: la 2 y la 3 se descartan");
            Assert.That(written, Is.EqualTo(2));
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(4), "gana el último");
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(ProfileJson.Serialize(WithSeeds(1))));
            Assert.That(writer.LastBackgroundMs, Is.EqualTo(5.0), "bg_ms: lo que tardó la última escritura del escritor");
            Assert.That(new ProfileWriter(store).LastBackgroundMs, Is.EqualTo(0.0), "sin marca de tiempo no se mide");
        }

        [Test]
        public void Writer_WriteNow_WaitsForTheWriter_DropsTheOlderQueue_AndHasACap()
        {
            // Cada escritura que llega al .tmp completo deja anotado su perfil, en el orden en que se escribe.
            var store = new LocalProfileStore(_dir);
            var reached = new ManualResetEventSlim(false);
            var release = new ManualResetEventSlim(false);
            var written = new System.Collections.Generic.List<int>();
            store.WriteHook = s =>
            {
                if (s != LocalProfileStore.WriteStage.TempWritten)
                {
                    return;
                }

                int seeds = ProfileJson.Deserialize(File.ReadAllText(store.TempPath, Utf8NoBom)).Wallet.Seeds;
                lock (written)
                {
                    written.Add(seeds);
                }

                if (Thread.CurrentThread.ManagedThreadId != _mainThread)
                {
                    reached.Set();
                    release.Wait(WaitMs);
                }
            };

            var writer = new ProfileWriter(store);
            writer.Enqueue(store.Serialize(WithSeeds(1)));
            Assert.That(reached.Wait(WaitMs), Is.True);
            writer.Enqueue(store.Serialize(WithSeeds(2))); // en la cola, más vieja que la síncrona

            // Con el escritor ocupado más que el tope, no escribe a la vez que él: deja los bytes en su cola (la 3 sustituye a la 2).
            Assert.That(writer.WriteNow(store.Serialize(WithSeeds(3)), 50), Is.False);
            Assert.That(File.Exists(store.MainPath), Is.False, "nada escrito todavía");

            // El escritor se suelta desde otro hilo mientras el principal espera en WriteNow(4). Según la carga de la máquina,
            // la 3 sale antes de que WriteNow(4) vacíe la cola o no sale nunca; lo que no cambia es la regla: la 2 nunca se
            // escribe y la 3 nunca pisa a la 4.
            var opener = new Thread(() =>
            {
                Thread.Sleep(100);
                release.Set();
            });
            opener.Start();
            Assert.That(writer.WriteNow(store.Serialize(WithSeeds(4)), WaitMs), Is.True, "espera al escritor y escribe aquí");
            opener.Join();
            Assert.That(writer.IsIdle, Is.True);
            Assert.That(writer.AllWritten, Is.True);
            int[] order;
            lock (written)
            {
                order = written.ToArray();
            }

            Assert.That(order.Length == 2 || order.Length == 3, Is.True, "la 1 y la 4, y como mucho la 3 entre ellas: " + string.Join(",", order));
            Assert.That(order[0], Is.EqualTo(1));
            Assert.That(order[order.Length - 1], Is.EqualTo(4), "la 4 es la última: la 3 nunca la pisa");
            Assert.That(order, Has.No.Member(2), "la 2 nunca se escribe");
            Assert.That(writer.BackgroundWrites, Is.EqualTo(order.Length - 1), "en segundo plano, la 1 y quizá la 3; la 4, en este hilo");
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(4));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(ProfileJson.Serialize(WithSeeds(order[order.Length - 2]))), "la copia es la anterior a la 4 (la 1 o la 3)");
        }

        [Test]
        public void Writer_AFailureStaysOnItsThread_AndTheMainThreadCollectsIt()
        {
            var store = new LocalProfileStore(_dir);
            store.Save(WithSeeds(1));
            store.WriteHook = s =>
            {
                if (s == LocalProfileStore.WriteStage.TempWritten)
                {
                    throw new IOException("disco lleno");
                }
            };

            var writer = new ProfileWriter(store);
            writer.Enqueue(store.Serialize(WithSeeds(2)));
            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Exception? failure = writer.TakeFailure(out bool latest);
            Assert.That(failure, Is.InstanceOf<IOException>());
            Assert.That(latest, Is.True, "ninguna entrega posterior la cubre: hay que reintentar");
            Assert.That(writer.TakeFailure(out _), Is.Null, "se recoge una vez");
            Assert.That(writer.AllWritten, Is.False);
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(1), "el principal anterior, intacto");

            // Una entrega posterior cubre el fallo anterior.
            store.WriteHook = null;
            writer.Enqueue(store.Serialize(WithSeeds(3)));
            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Assert.That(writer.TakeFailure(out _), Is.Null);
            Assert.That(writer.AllWritten, Is.True);
            Assert.That(new LocalProfileStore(_dir).Load().Profile!.Wallet.Seeds, Is.EqualTo(3));
        }

        [Test]
        public void Serialize_IsWhatSaveWrites_AndRespectsTheWriteBlock()
        {
            var store = new LocalProfileStore(_dir);
            Profile p = SampleProfiles.Full();
            byte[] bytes = store.Serialize(p);
            Assert.That(Directory.GetFiles(_dir), Is.Empty, "serializar no toca el disco");
            store.WriteBytes(bytes);
            Assert.That(File.ReadAllBytes(store.MainPath), Is.EqualTo(bytes));
            Assert.That(store.LastSavedBytes, Is.EqualTo(bytes.Length));

            File.WriteAllText(store.MainPath, "{\"profile_schema\": 9}", Utf8NoBom);
            Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.TooNew));
            Assert.Throws<InvalidOperationException>(() => store.Serialize(p));
            Assert.Throws<InvalidOperationException>(() => store.WriteBytes(bytes));
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

        private static LocalProfileStore.WriteStage Stage(string name)
        {
            return (LocalProfileStore.WriteStage)Enum.Parse(typeof(LocalProfileStore.WriteStage), name);
        }

        private static Profile WithSeeds(int seeds)
        {
            Profile p = SampleProfiles.Full();
            p.Wallet.Seeds = seeds;
            return p;
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

        /// <summary>El proceso «muere» en ese punto de la escritura: nada de lo que sigue se ejecuta.</summary>
        private sealed class ProcessKilled : Exception
        {
            public ProcessKilled()
                : base("proceso cortado (test)")
            {
            }
        }
    }
}
