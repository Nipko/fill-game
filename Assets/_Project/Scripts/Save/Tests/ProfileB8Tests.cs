#nullable enable
using System;
using System.IO;
using System.Text;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Los campos del perfil que añade el bloque B8 (08 §4.3; parche <c>B8-profile.diff</c>): <c>first_app_version</c>,
    /// <c>locale</c>, <c>country_first</c>, <c>applied_purchases</c>, <c>voided_purchases</c> [P], <c>pending_rewards</c> [P] e
    /// <c>integrity</c>; la firma (<see cref="ProfileIntegrity"/>, <see cref="SigningProfileStore"/>), la identidad
    /// (<see cref="ProfileIdentity"/>), su parte de la fusión (<c>CloudMerge.Fields.cs</c>) y la sincronización al arrancar
    /// (<see cref="CloudSync"/>).
    /// </summary>
    public sealed class ProfileB8Tests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        private static readonly IDeviceIdentity PhoneA = new ManualDeviceIdentity("phone-a");
        private static readonly IDeviceIdentity PhoneB = new ManualDeviceIdentity("phone-b");

        private static Profile Filled()
        {
            Profile p = SampleProfiles.Full();
            p.FirstAppVersion = "1.0.0";
            p.Locale = "es-419";
            p.CountryFirst = "CO";
            p.AppliedPurchases.Add("GPA.3390-b");
            p.AppliedPurchases.Add("GPA.3312-a");
            p.VoidedPurchases.Add("GPA.3312-a");
            var reward = new ProvisionalRewardData { Nonce = "n-1", Placement = "rv_undo_refill", GrantedAt = T0.AddTicks(77), Seeds = 0 };
            reward.Stock.Undo = 3;
            p.PendingRewards.Add(reward);
            p.Integrity.FraudFlag = 2;
            return p;
        }

        [Test]
        public void NewFields_RoundTrip_WithSortedPurchaseLists()
        {
            Profile p = Filled();
            Profile back = ProfileJson.Deserialize(ProfileJson.Serialize(p));
            Assert.That(back.FirstAppVersion, Is.EqualTo("1.0.0"));
            Assert.That(back.Locale, Is.EqualTo("es-419"));
            Assert.That(back.CountryFirst, Is.EqualTo("CO"));
            Assert.That(back.AppliedPurchases, Is.EqualTo(new[] { "GPA.3312-a", "GPA.3390-b" }), "orden ordinal");
            Assert.That(back.VoidedPurchases, Is.EqualTo(new[] { "GPA.3312-a" }));
            Assert.That(back.PendingRewards.Count, Is.EqualTo(1));
            Assert.That(back.PendingRewards[0].Nonce, Is.EqualTo("n-1"));
            Assert.That(back.PendingRewards[0].Placement, Is.EqualTo("rv_undo_refill"));
            Assert.That(back.PendingRewards[0].GrantedAt, Is.EqualTo(T0.AddTicks(77)));
            Assert.That(back.PendingRewards[0].Stock.Undo, Is.EqualTo(3));
            Assert.That(back.Integrity.FraudFlag, Is.EqualTo(2));
            Assert.That(ProfileJson.Serialize(back), Is.EqualTo(ProfileJson.Serialize(p)));
        }

        [Test]
        public void NewFields_AreOmittedInTheirDefault_SoAnOldProfileWritesTheSame()
        {
            string json = ProfileJson.Serialize(SampleProfiles.Full());
            foreach (string key in new[] { "first_app_version", "locale", "country_first", "applied_purchases", "voided_purchases", "pending_rewards", "integrity" })
            {
                StringAssert.DoesNotContain("\"" + key + "\"", json, key);
            }

            Profile old = ProfileJson.Deserialize("{\"profile_schema\": 2}");
            Assert.That(old.FirstAppVersion, Is.Null, "no se inventa una versión");
            Assert.That(old.AppliedPurchases, Is.Empty);
            Assert.That(old.Integrity.IsEmpty, Is.True);
        }

        [Test]
        public void PurchaseLists_AreEconomy_AWrongTypeIsCorruption_ButIdentityAndIntegrityAreTolerant()
        {
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize("{\"profile_schema\": 2, \"applied_purchases\": \"GPA.1\"}"));
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize("{\"profile_schema\": 2, \"pending_rewards\": [{\"placement\": \"x\"}]}"));

            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"locale\": 5, \"country_first\": null, \"integrity\": {\"hmac\": 7, \"fraud_flag\": -3}}");
            Assert.That(p.Locale, Is.Null);
            Assert.That(p.CountryFirst, Is.Null);
            Assert.That(p.Integrity.Hmac, Is.Null);
            Assert.That(p.Integrity.FraudFlag, Is.EqualTo(0));
            Assert.That(ProfileIntegrity.Verify(p), Is.EqualTo(IntegrityStatus.Unsigned));
        }

        [Test]
        public void Sign_ThenVerify_IsValid_AlsoAfterTheJsonRoundTrip_AndOnAnotherPhone()
        {
            Profile p = Filled();
            Assert.That(ProfileIntegrity.Verify(p), Is.EqualTo(IntegrityStatus.Unsigned));
            ProfileIntegrity.Sign(p, PhoneA);
            Assert.That(p.Integrity.DeviceFirst, Is.EqualTo(ProfileHmac.DeviceFingerprint("phone-a")));
            Assert.That(ProfileHmac.IsSignature(p.Integrity.Hmac), Is.True);
            Assert.That(ProfileIntegrity.Verify(p), Is.EqualTo(IntegrityStatus.Valid));

            Profile back = ProfileJson.Deserialize(ProfileJson.Serialize(p));
            Assert.That(ProfileIntegrity.Verify(back), Is.EqualTo(IntegrityStatus.Valid), "la ida y vuelta por JSON no cambia lo firmado");

            // Restaurado en otro teléfono: la clave sale de device_first, que no cambia (Q-B8-2 [P]).
            ProfileIntegrity.Sign(back, PhoneB);
            Assert.That(back.Integrity.DeviceFirst, Is.EqualTo(ProfileHmac.DeviceFingerprint("phone-a")));
            Assert.That(ProfileIntegrity.Verify(back), Is.EqualTo(IntegrityStatus.Valid));
        }

        [Test]
        public void EditingTheFileByHand_LeavesAMismatch()
        {
            Profile p = Filled();
            ProfileIntegrity.Sign(p, PhoneA);
            string json = ProfileJson.Serialize(p);
            Assert.That(json, Does.Contain("\"seeds\": 1240"));
            Profile edited = ProfileJson.Deserialize(json.Replace("\"seeds\": 1240", "\"seeds\": 999999"));
            Assert.That(ProfileIntegrity.Verify(edited), Is.EqualTo(IntegrityStatus.Mismatch));
            Assert.That(ProfileIntegrity.IsCoherent(IntegrityStatus.Mismatch), Is.False);
            Assert.That(ProfileIntegrity.IsCoherent(IntegrityStatus.Unsigned), Is.True, "[P] Q-B8-5: sin firma (o migrado) sigue las reglas de siempre");
            Assert.That(ProfileIntegrity.ToId(IntegrityStatus.Mismatch), Is.EqualTo("mismatch"));
        }

        [Test]
        public void SigningStore_KeepsTheByteStore_SignsEveryWrite_AndReportsTheIntegrityOnLoad()
        {
            string dir = TempDirs.Create();
            try
            {
                var local = new LocalProfileStore(dir);
                SigningProfileStore store = SigningProfileStore.Wrap(local, PhoneA);
                Assert.That(store, Is.InstanceOf<IProfileByteStore>(), "el lote sigue escribiendo en segundo plano");
                Assert.That(store.Inner, Is.SameAs(local));

                Profile p = Filled();
                store.Save(p);
                Assert.That(store.SignCount, Is.EqualTo(1));
                Assert.That(ProfileIntegrity.Verify(p), Is.EqualTo(IntegrityStatus.Valid), "firma el perfil en memoria");
                store.Load();
                Assert.That(store.LastIntegrity, Is.EqualTo(IntegrityStatus.Valid));

                p.Wallet.Seeds = 5;
                byte[] bytes = ((IProfileByteStore)store).Serialize(p);
                ((IProfileByteStore)store).WriteBytes(bytes);
                Assert.That(store.SignCount, Is.EqualTo(2), "la firma va en Serialize, en el hilo principal");
                ProfileLoadResult loaded = store.Load();
                Assert.That(loaded.Profile!.Wallet.Seeds, Is.EqualTo(5));
                Assert.That(store.LastIntegrity, Is.EqualTo(IntegrityStatus.Valid));

                string path = Path.Combine(dir, "profile.json");
                File.WriteAllText(path, File.ReadAllText(path).Replace("\"seeds\": 5", "\"seeds\": 50000"), new UTF8Encoding(false));
                store.Load();
                Assert.That(store.LastIntegrity, Is.EqualTo(IntegrityStatus.Mismatch), "editado a mano");

                store.Delete();
                Assert.That(store.LastIntegrity, Is.Null);
                Assert.That(store.Load().Status, Is.EqualTo(ProfileLoadStatus.NotFound), "08 §11: borra profile.json y profile.bak");
            }
            finally
            {
                TempDirs.Delete(dir);
            }
        }

        [Test]
        public void SigningStore_OverAMemoryStore_IsAPlainStore()
        {
            var memory = new MemoryProfileStore();
            SigningProfileStore store = SigningProfileStore.Wrap(memory, PhoneA);
            Assert.That(store, Is.Not.InstanceOf<IProfileByteStore>());
            store.Save(Filled());
            StringAssert.Contains("\"hmac\"", memory.Json);
        }

        [TestCase("es-CO", "CO")]
        [TestCase("pt_BR", "BR")]
        [TestCase("zh-Hant-TW", "TW")]
        [TestCase("es-co", "CO")]
        [TestCase("es-419", null)]
        [TestCase("es", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void CountryFromTag_TakesTheTwoLetterRegion(string? tag, string? country)
        {
            Assert.That(ProfileIdentity.CountryFromTag(tag), Is.EqualTo(country));
        }

        [Test]
        public void StampFirstRun_WritesOnce_AndLocaleFollowsEachBoot()
        {
            Profile p = Profile.CreateNew(new ManualClock(T0), 5);
            Assert.That(ProfileIdentity.StampFirstRun(p, "1.0.0", "es-CO", "es-419"), Is.True);
            Assert.That(p.FirstAppVersion, Is.EqualTo("1.0.0"));
            Assert.That(p.CountryFirst, Is.EqualTo("CO"));
            Assert.That(p.Locale, Is.EqualTo("es-419"));

            Assert.That(ProfileIdentity.StampFirstRun(p, "1.2.0", "pt-BR", "es-419"), Is.False, "first_app_version y country_first no cambian");
            Assert.That(p.FirstAppVersion, Is.EqualTo("1.0.0"));
            Assert.That(p.CountryFirst, Is.EqualTo("CO"));

            Assert.That(ProfileIdentity.UpdateLocale(p, "pt-BR"), Is.True);
            Assert.That(p.Locale, Is.EqualTo("pt-BR"));
            Assert.That(ProfileIdentity.UpdateLocale(p, " "), Is.False);
            Assert.That(p.Locale, Is.EqualTo("pt-BR"));
        }

        [Test]
        public void Merge_AppliedAndVoidedPurchases_AreUnions_FraudFlagMax_IdentityFromTheOlderProfile()
        {
            Profile local = Profile.CreateNew(new ManualClock(T0), 5);
            local.Progress.MaxLevel = 70;
            local.AppliedPurchases.Add("GPA.b");
            local.Locale = "pt-BR";
            local.Integrity.FraudFlag = 1;
            var reward = new ProvisionalRewardData { Nonce = "local", GrantedAt = T0 };
            local.PendingRewards.Add(reward);
            Profile cloud = Profile.CreateNew(new ManualClock(T0.AddDays(-30)), 5);
            cloud.Progress.MaxLevel = 40;
            cloud.FirstAppVersion = "1.0.0";
            cloud.CountryFirst = "PE";
            cloud.AppliedPurchases.Add("GPA.a");
            cloud.VoidedPurchases.Add("GPA.a");
            cloud.Integrity.FraudFlag = 3;
            cloud.PendingRewards.Add(new ProvisionalRewardData { Nonce = "cloud", GrantedAt = T0 });

            Profile m = CloudMerge.Resolve(local, cloud).Merged;
            Assert.That(m.Progress.MaxLevel, Is.EqualTo(70));
            Assert.That(m.AppliedPurchases, Is.EqualTo(new[] { "GPA.a", "GPA.b" }), "08 §11: union(...)");
            Assert.That(m.VoidedPurchases, Is.EqualTo(new[] { "GPA.a" }));
            Assert.That(m.Integrity.FraudFlag, Is.EqualTo(3));
            Assert.That(m.PendingRewards.Count, Is.EqualTo(1), "[P]: los provisionales van con su cartera");
            Assert.That(m.PendingRewards[0].Nonce, Is.EqualTo("local"));
            Assert.That(m.FirstAppVersion, Is.EqualTo("1.0.0"), "[P]: del perfil creado antes");
            Assert.That(m.CountryFirst, Is.EqualTo("PE"));
            Assert.That(m.Locale, Is.EqualTo("pt-BR"), "el del ganador");

            // Con el caso de integridad no se une nada del manipulado.
            Profile clean = CloudMerge.Resolve(local, cloud, localCoherent: true, cloudCoherent: false).Merged;
            Assert.That(clean.AppliedPurchases, Is.EqualTo(new[] { "GPA.b" }));
            Assert.That(clean.Integrity.FraudFlag, Is.EqualTo(1));
        }

        [Test]
        public void Merge_PurchaseQueue_IsOnlyLocal_NotCompared_NotSigned()
        {
            // D-035, añadido de F3-3: purchase_queue es del dispositivo; gane quien gane, el fusionado lleva la local.
            Profile local = Profile.CreateNew(new ManualClock(T0), 5);
            local.Progress.MaxLevel = 10;
            local.PurchaseQueue = "{\"v\":1,\"entries\":[]}";
            Profile cloud = Profile.CreateNew(new ManualClock(T0), 5);
            cloud.Progress.MaxLevel = 40;
            cloud.PurchaseQueue = "{\"v\":1,\"entries\":[{\"token\":\"t.cloud\",\"sku\":\"af_coins_2\",\"first_ms\":5,\"attempts\":0,\"next_ms\":5}]}";

            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Cloud));
            Assert.That(r.Merged.PurchaseQueue, Is.EqualTo(local.PurchaseQueue), "la cola del dispositivo");
            Assert.That(CloudMerge.SharedJson(r.Merged), Does.Not.Contain("purchase_queue"), "no sube a la nube");
            Assert.That(local.PurchaseQueue, Is.EqualTo("{\"v\":1,\"entries\":[]}"), "SharedJson no cambia el perfil");

            // Solo la cola distinta: los dos son el mismo perfil.
            Profile same = ProfileJson.Deserialize(ProfileJson.Serialize(local));
            same.PurchaseQueue = null;
            Assert.That(CloudMerge.Resolve(local, same).Reason, Is.EqualTo(CloudMergeReason.Identical));

            // La firma no cubre la cola: el blob que sube (sin cola) se comprueba igual.
            ProfileIntegrity.Sign(local, PhoneA);
            Profile uploaded = ProfileJson.Deserialize(CloudMerge.SharedJson(local));
            Assert.That(ProfileIntegrity.Verify(uploaded), Is.EqualTo(IntegrityStatus.Valid));
        }

        [Test]
        public void Merge_PushAndLastSeen_Q_B8_11()
        {
            // Q-B8-11 (a) [P]: fechas por máximo, pre-prompts por «o» y granted del dispositivo.
            Profile local = Profile.CreateNew(new ManualClock(T0), 5);
            local.Progress.MaxLevel = 10;
            local.Stats.LastSeenAt = T0.AddDays(2);
            local.Settings.Push.Granted = false;
            Profile cloud = Profile.CreateNew(new ManualClock(T0), 5);
            cloud.Progress.MaxLevel = 40;
            cloud.Stats.LastSeenAt = T0;
            cloud.Stats.LastWinAt = T0.AddDays(1);
            cloud.Settings.Push.PrePromptShown = true;
            cloud.Settings.Push.PrePromptAt = T0;
            cloud.Settings.Push.Granted = true;

            Profile m = CloudMerge.Resolve(local, cloud).Merged;
            Assert.That(m.Progress.MaxLevel, Is.EqualTo(40), "gana la nube");
            Assert.That(m.Stats.LastSeenAt, Is.EqualTo(T0.AddDays(2)));
            Assert.That(m.Stats.LastWinAt, Is.EqualTo(T0.AddDays(1)));
            Assert.That(m.Settings.Push.PrePromptShown, Is.True);
            Assert.That(m.Settings.Push.PrePromptAt, Is.EqualTo(T0));
            Assert.That(m.Settings.Push.Granted, Is.False, "el permiso es del teléfono");
        }

        [Test]
        public void CloudSync_WithoutSession_DoesNothing()
        {
            Profile local = Filled();
            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, FakeCloudSave.WithoutSession(), PhoneA, r => report = r);
            Assert.That(report!.Ran, Is.False);
            Assert.That(report.Profile, Is.SameAs(local));
            Assert.That(report.Replaced, Is.False);
        }

        [Test]
        public void CloudSync_FirstTime_UploadsTheSignedLocal()
        {
            Profile local = Filled();
            var cloud = new FakeCloudSave();
            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r);
            Assert.That(report!.Ran, Is.True);
            Assert.That(report.Result, Is.EqualTo(CloudSyncResult.Ok));
            Assert.That(report.Reason, Is.EqualTo(CloudMergeReason.NoCloud));
            Assert.That(report.Upload, Is.EqualTo(CloudWriteResult.Ok));
            Assert.That(report.Bytes, Is.EqualTo(cloud.Blob!.Length));
            Profile uploaded = ProfileJson.Deserialize(Encoding.UTF8.GetString(cloud.Blob));
            Assert.That(ProfileIntegrity.Verify(uploaded), Is.EqualTo(IntegrityStatus.Valid));
        }

        [Test]
        public void CloudSync_ACloudWithMoreLevels_ReplacesTheLocal()
        {
            Profile local = Filled(); // max_level 87
            ProfileIntegrity.Sign(local, PhoneA);
            Profile remote = Filled();
            remote.Progress.MaxLevel = 120;
            remote.Progress.NextLevel = 121;
            remote.Wallet.Seeds = 77;
            ProfileIntegrity.Sign(remote, PhoneB);
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(ProfileJson.Serialize(remote)));

            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r);
            Assert.That(report!.Result, Is.EqualTo(CloudSyncResult.ConflictCloudWon));
            Assert.That(report.Reason, Is.EqualTo(CloudMergeReason.MaxLevel));
            Assert.That(report.Replaced, Is.True);
            Assert.That(report.Profile, Is.Not.SameAs(local));
            Assert.That(report.Profile.Progress.MaxLevel, Is.EqualTo(120));
            Assert.That(report.Profile.Wallet.Seeds, Is.EqualTo(77));
            Assert.That(report.CloudIntegrity, Is.EqualTo(IntegrityStatus.Valid), "firmado en otro teléfono, se comprueba aquí");
        }

        [Test]
        public void CloudSync_ATamperedCloud_LosesAgainstTheCoherentLocal()
        {
            Profile local = Filled();
            ProfileIntegrity.Sign(local, PhoneA);
            Profile remote = Filled();
            ProfileIntegrity.Sign(remote, PhoneB);
            string json = ProfileJson.Serialize(remote).Replace("\"max_level\": 87", "\"max_level\": 999");
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(json));

            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r);
            Assert.That(report!.CloudIntegrity, Is.EqualTo(IntegrityStatus.Mismatch));
            Assert.That(report.Reason, Is.EqualTo(CloudMergeReason.Integrity));
            Assert.That(report.Result, Is.EqualTo(CloudSyncResult.ConflictLocalWon));
            Assert.That(report.Profile, Is.SameAs(local));
            Assert.That(report.Upload, Is.EqualTo(CloudWriteResult.Ok), "la nube se corrige con el local");
            Assert.That(ProfileJson.Deserialize(Encoding.UTF8.GetString(cloud.Blob!)).Progress.MaxLevel, Is.EqualTo(87));
        }

        [Test]
        public void CloudSync_ANewerSchemaInTheCloud_IsNeitherReadNorOverwritten()
        {
            byte[] newer = Encoding.UTF8.GetBytes("{\"profile_schema\": 99, \"progress\": {\"max_level\": 500}}");
            var cloud = new FakeCloudSave(newer);
            CloudSyncReport? report = null;
            CloudSync.AtStartup(Filled(), cloud, PhoneA, r => report = r);
            Assert.That(report!.Result, Is.EqualTo(CloudSyncResult.Failed));
            Assert.That(report.Error, Does.Contain("99"));
            Assert.That(cloud.Blob, Is.EqualTo(newer), "08 §4.4: nunca se pisa uno más nuevo");
            Assert.That(cloud.SaveCount, Is.EqualTo(0));
        }

        [Test]
        public void CloudSync_AnUnreadableCloud_IsReplacedByTheLocal_AndAFailedLoadKeepsTheLocal()
        {
            var cloud = new FakeCloudSave(new byte[] { 0x7B, 0xFF, 0x00 });
            CloudSyncReport? report = null;
            Profile local = Filled();
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r);
            Assert.That(report!.Reason, Is.EqualTo(CloudMergeReason.NoCloud), "[P]: un blob ilegible se trata como sin nube");
            Assert.That(report.Upload, Is.EqualTo(CloudWriteResult.Ok));

            cloud.FailNext = true;
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r);
            Assert.That(report!.Result, Is.EqualTo(CloudSyncResult.Failed));
            Assert.That(report.Profile, Is.SameAs(local));
        }

        [Test]
        public void CloudSync_ASignedButMigratedCloud_DoesNotLoseByIntegrity()
        {
            // Revisión de B8: un blob firmado con un esquema anterior no cuadra tras migrarlo; no debe perder por el caso 8.
            Profile local = Filled(); // max_level 87
            ProfileIntegrity.Sign(local, PhoneA);
            string v1 = "{\"profile_schema\": 1, \"economy_ts\": \"2026-10-20T12:00:00Z\", \"progress\": {\"max_level\": 200, \"next_level\": 201},"
                + " \"integrity\": {\"hmac\": \"" + new string('a', 64) + "\", \"device_first\": \"" + ProfileHmac.DeviceFingerprint("old") + "\", \"fraud_flag\": 0}}";
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(v1));
            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r);
            Assert.That(report!.CloudIntegrity, Is.EqualTo(IntegrityStatus.Unsigned), "migrado: no se puede comprobar");
            Assert.That(report.Reason, Is.EqualTo(CloudMergeReason.MaxLevel));
            Assert.That(report.Profile.Progress.MaxLevel, Is.EqualTo(200));
        }

        [Test]
        public void CloudSync_UsesTheLocalIntegrityItIsGiven()
        {
            // El arranque comprueba la firma antes de los extras de desarrollo y se la pasa a la sincronización.
            Profile local = Filled();
            ProfileIntegrity.Sign(local, PhoneA);
            local.Wallet.Seeds = 5; // un cambio en memoria tras leerlo (af_seeds)
            Profile remote = Filled();
            remote.Progress.MaxLevel = 10;
            ProfileIntegrity.Sign(remote, PhoneB);
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(ProfileJson.Serialize(remote)));
            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, PhoneA, r => report = r, IntegrityStatus.Valid);
            Assert.That(report!.LocalIntegrity, Is.EqualTo(IntegrityStatus.Valid));
            Assert.That(report.Reason, Is.EqualTo(CloudMergeReason.MaxLevel));
        }

        [Test]
        public void CloudSync_MigratesAnOlderCloudSchema()
        {
            // Un blob del esquema 1 (sin fauna) se lee con la migración 1 → 2 (08 §4.4) y gana por nivel.
            string v1 = "{\"profile_schema\": 1, \"economy_ts\": \"2026-10-20T12:00:00Z\", \"progress\": {\"max_level\": 200, \"next_level\": 201}}";
            var cloud = new FakeCloudSave(Encoding.UTF8.GetBytes(v1));
            CloudSyncReport? report = null;
            CloudSync.AtStartup(Filled(), cloud, PhoneA, r => report = r);
            Assert.That(report!.Reason, Is.EqualTo(CloudMergeReason.MaxLevel), "los dos sin firma: las reglas de siempre");
            Assert.That(report.Replaced, Is.True);
            Assert.That(report.Profile.Progress.MaxLevel, Is.EqualTo(200));
        }
    }
}
