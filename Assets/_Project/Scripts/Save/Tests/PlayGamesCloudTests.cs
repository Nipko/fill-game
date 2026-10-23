#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Play Games (D-042; 08 §11; <c>docs/dev/play-games.md</c>) sin el SDK: <see cref="PlayGamesCloudSave"/> sobre un puerto falso
    /// (sesión silenciosa, snapshot vacío, tope del blob, conflicto de Play, sin red y #3379, borrado y cierre),
    /// <see cref="CloudErase"/> (borrado pendiente) y <see cref="CloudSession"/> (arranque, subidas, reintento y borrado de cuenta).
    /// </summary>
    public sealed class PlayGamesCloudTests
    {
        private static readonly IDeviceIdentity Phone = new ManualDeviceIdentity("pgs-phone");

        private static Profile AtLevel(int maxLevel, int seeds)
        {
            Profile p = SampleProfiles.Full();
            p.Progress.MaxLevel = maxLevel;
            p.Progress.NextLevel = maxLevel + 1;
            p.Wallet.Seeds = seeds;
            return p;
        }

        private static byte[] Blob(Profile p)
        {
            ProfileIntegrity.Sign(p, Phone);
            return Encoding.UTF8.GetBytes(CloudMerge.SharedJson(p));
        }

        private static Profile Read(byte[] blob) => ProfileJson.Deserialize(Encoding.UTF8.GetString(blob));

        private static (PlayGamesCloudSave cloud, FakePort port, MemoryCloudSaveFlags flags) Create(bool online = true, bool configured = true)
        {
            var port = new FakePort { Configured = configured };
            var flags = new MemoryCloudSaveFlags();
            bool net = online;
            var cloud = new PlayGamesCloudSave(port, flags, () => net, Phone);
            return (cloud, port, flags);
        }

        private static PlayGamesCloudSave SignedIn(FakePort port, MemoryCloudSaveFlags flags, Func<bool>? online = null)
        {
            var cloud = new PlayGamesCloudSave(port, flags, online ?? (() => true), Phone);
            bool? ok = null;
            cloud.SignIn(r => ok = r);
            Assert.That(ok, Is.True);
            return cloud;
        }

        [Test]
        public void WithoutAppId_TheSdkIsNeverCalled_AndThereIsNoSession()
        {
            var (cloud, port, _) = Create(configured: false);
            bool? ok = null;
            cloud.SignIn(r => ok = r);
            Assert.That(ok, Is.False);
            Assert.That(cloud.HasSession, Is.False);
            Assert.That(port.Calls, Is.Empty, "sin APP_ID (D-034) no se toca Play Games");
            CloudLoadResult? load = null;
            cloud.Load(r => load = r);
            Assert.That(load!.Status, Is.EqualTo(CloudLoadStatus.NoSession));
        }

        [Test]
        public void SilentSignIn_ThenAnEmptySnapshot_IsNotFound_AndASave_IsRead_Back()
        {
            var port = new FakePort();
            PlayGamesCloudSave cloud = SignedIn(port, new MemoryCloudSaveFlags());
            Assert.That(port.Calls, Is.EqualTo(new[] { "signin" }), "solo la silenciosa: nunca la manual (sin botón)");

            CloudLoadResult? load = null;
            cloud.Load(r => load = r);
            Assert.That(load!.Status, Is.EqualTo(CloudLoadStatus.NotFound), "abrir crea el snapshot vacío: vacío = no hay");
            Assert.That(port.LastName, Is.EqualTo("af_main"));

            byte[] blob = Blob(AtLevel(12, 30));
            CloudWriteResult? write = null;
            cloud.Save(blob, r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Ok));
            cloud.Load(r => load = r);
            Assert.That(load!.Status, Is.EqualTo(CloudLoadStatus.Loaded));
            Assert.That(load.Blob, Is.EqualTo(blob));
        }

        [Test]
        public void ABlobOverTheCap_IsNotSent()
        {
            var port = new FakePort();
            PlayGamesCloudSave cloud = SignedIn(port, new MemoryCloudSaveFlags());
            port.Calls.Clear();
            CloudWriteResult? write = null;
            cloud.Save(new byte[CloudSaveNames.MaxBlobBytes + 1], r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.TooLarge));
            Assert.That(port.Calls, Is.Empty, "08 §11: 800 KB; no llega a Play");
        }

        [Test]
        public void AnAuthError_DropsTheSession()
        {
            var port = new FakePort();
            PlayGamesCloudSave cloud = SignedIn(port, new MemoryCloudSaveFlags());
            port.NextOpen = SavedGamesStatus.AuthError;
            CloudLoadResult? load = null;
            cloud.Load(r => load = r);
            Assert.That(load!.Status, Is.EqualTo(CloudLoadStatus.NoSession));
            Assert.That(cloud.HasSession, Is.False);
        }

        [Test]
        public void WithoutNetwork_TheLoadFails_AndTheLocalIsKept()
        {
            var port = new FakePort();
            PlayGamesCloudSave cloud = SignedIn(port, new MemoryCloudSaveFlags());
            port.NextOpen = SavedGamesStatus.Timeout;
            Profile local = AtLevel(5, 10);
            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, Phone, r => report = r);
            Assert.That(report!.Result, Is.EqualTo(CloudSyncResult.Failed));
            Assert.That(report.Profile, Is.SameAs(local));
            Assert.That(report.Upload, Is.Null, "sin fusionar no se sube nada");
        }

        [Test]
        public void Issue3379_ASaveWithoutNetwork_IsUploadedAgain_ByTheNextSync()
        {
            var port = new FakePort();
            var flags = new MemoryCloudSaveFlags();
            bool online = false;
            PlayGamesCloudSave cloud = SignedIn(port, flags, () => online);
            Profile local = AtLevel(8, 20);
            CloudWriteResult? write = null;
            CloudSync.Upload(local, cloud, Phone, (r, _) => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Ok), "Play lo guarda en su caché");
            Assert.That(flags.ResyncPending, Is.True, "#3379: sin red, puede no subir nunca");
            Assert.That(cloud.NeedsResync, Is.True);

            // Con red, la nube es idéntica (la caché de Play), pero se vuelve a subir igual.
            online = true;
            int commits = port.Commits;
            CloudSyncReport? report = null;
            CloudSync.AtStartup(local, cloud, Phone, r => report = r);
            Assert.That(report!.Result, Is.EqualTo(CloudSyncResult.Ok), "la nube es la misma: nada que fusionar");
            Assert.That(report.Upload, Is.EqualTo(CloudWriteResult.Ok), "la subida pendiente sale aunque la fusión no la pida");
            Assert.That(port.Commits, Is.EqualTo(commits + 1));
            Assert.That(flags.ResyncPending, Is.False, "con red, consta");
        }

        [Test]
        public void AFailedCommit_LeavesTheResyncFlag()
        {
            var port = new FakePort();
            var flags = new MemoryCloudSaveFlags();
            PlayGamesCloudSave cloud = SignedIn(port, flags);
            port.NextCommit = SavedGamesStatus.Failed;
            CloudWriteResult? write = null;
            cloud.Save(Blob(AtLevel(3, 1)), r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.Failed));
            Assert.That(flags.ResyncPending, Is.True);
        }

        [Test]
        public void APlayConflict_IsResolvedWithTheMergeRules_NotTheLongestPlaytime()
        {
            var port = new FakePort();
            PlayGamesCloudSave cloud = SignedIn(port, new MemoryCloudSaveFlags());
            port.Conflict = (Blob(AtLevel(10, 500)), Blob(AtLevel(40, 7)));
            CloudLoadResult? load = null;
            cloud.Load(r => load = r);
            Assert.That(load!.Status, Is.EqualTo(CloudLoadStatus.Loaded));
            Profile chosen = Read(load.Blob!);
            Assert.That(chosen.Progress.MaxLevel, Is.EqualTo(40), "08 §11: gana el mayor max_level");
            Assert.That(chosen.Wallet.Seeds, Is.EqualTo(7), "con su cartera: los consumibles nunca se suman");
            Assert.That(ProfileIntegrity.Verify(chosen), Is.EqualTo(IntegrityStatus.Valid), "el resultado va firmado");
        }

        [Test]
        public void AConflictWithAnUnreadableVersion_KeepsTheReadableOne()
        {
            byte[] good = Blob(AtLevel(9, 2));
            Assert.That(CloudSync.ResolveSnapshotConflict(new byte[] { 0x7B, 0xFF }, good, Phone), Is.EqualTo(good));
            Assert.That(CloudSync.ResolveSnapshotConflict(good, new byte[] { 0x7B }, Phone), Is.EqualTo(good));
        }

        [Test]
        public void Delete_AsksPlay_AndAClosedCloud_NeverWritesAgain()
        {
            var port = new FakePort();
            PlayGamesCloudSave cloud = SignedIn(port, new MemoryCloudSaveFlags());
            cloud.Save(Blob(AtLevel(4, 4)), _ => { });
            CloudWriteResult? deleted = null;
            cloud.Delete(r => deleted = r);
            Assert.That(deleted, Is.EqualTo(CloudWriteResult.Ok));
            Assert.That(port.Data, Is.Null, "el snapshot se pidió borrar");

            cloud.Close();
            int calls = port.Calls.Count;
            CloudWriteResult? write = null;
            cloud.Save(Blob(AtLevel(4, 4)), r => write = r);
            Assert.That(write, Is.EqualTo(CloudWriteResult.NoSession), "tras borrar la cuenta (08 §11) no se escribe");
            bool? ok = null;
            cloud.SignIn(r => ok = r);
            Assert.That(ok, Is.False);
            Assert.That(port.Calls.Count, Is.EqualTo(calls), "ni se vuelve a llamar a Play");
        }

        [Test]
        public void CloudErase_PendingWithData_DeletesAgain_AndOnlyAnEmptySnapshot_ClearsTheFlag()
        {
            var cloud = new FakeCloudSave(Blob(AtLevel(30, 3)));
            var flags = new MemoryCloudSaveFlags { DeletePending = true };
            CloudEraseResult? result = null;
            CloudErase.Resume(cloud, flags, r => result = r);
            Assert.That(result, Is.EqualTo(CloudEraseResult.Requested));
            Assert.That(cloud.DeleteCount, Is.EqualTo(1));
            Assert.That(flags.DeletePending, Is.True, "se comprueba en el arranque siguiente");

            CloudErase.Resume(cloud, flags, r => result = r);
            Assert.That(result, Is.EqualTo(CloudEraseResult.Verified));
            Assert.That(flags.DeletePending, Is.False);

            CloudErase.Resume(cloud, flags, r => result = r);
            Assert.That(result, Is.EqualTo(CloudEraseResult.NothingPending));
        }

        [Test]
        public void CloudErase_WithoutSessionOrNetwork_StaysPending()
        {
            var flags = new MemoryCloudSaveFlags { DeletePending = true };
            CloudEraseResult? result = null;
            CloudErase.Resume(FakeCloudSave.WithoutSession(), flags, r => result = r);
            Assert.That(result, Is.EqualTo(CloudEraseResult.Pending));
            var cloud = new FakeCloudSave(Blob(AtLevel(30, 3))) { FailNext = true };
            CloudErase.Resume(cloud, flags, r => result = r);
            Assert.That(result, Is.EqualTo(CloudEraseResult.Pending));
            Assert.That(flags.DeletePending, Is.True);
        }

        [Test]
        public void Session_SignsInSilently_Merges_AndThenUploadsEveryThreeWins()
        {
            var port = new FakePort();
            port.Data = Blob(AtLevel(20, 99));
            var flags = new MemoryCloudSaveFlags();
            var cloud = new PlayGamesCloudSave(port, flags, () => true, Phone);
            var session = new CloudSession(cloud, flags, Phone);
            Profile local = AtLevel(5, 1);
            CloudSessionStart? start = null;
            Assert.That(session.Start(() => local, s => start = s), Is.True);
            Assert.That(start!.State, Is.EqualTo(CloudSessionState.Synced));
            Assert.That(start.Report!.Result, Is.EqualTo(CloudSyncResult.ConflictCloudWon));
            Assert.That(start.Report.Profile.Progress.MaxLevel, Is.EqualTo(20));

            int commits = port.Commits;
            Assert.That(session.OnLevelWon(local), Is.False);
            Assert.That(session.OnLevelWon(local), Is.False);
            Assert.That(session.OnLevelWon(local), Is.True, "08 §11: cada save_cloud_interval_levels (3)");
            Assert.That(port.Commits, Is.EqualTo(commits + 1));
        }

        [Test]
        public void Session_NeverUploadsBeforeMerging_AndRetriesWhenTheNetworkComesBack()
        {
            var cloud = new FakeCloudSave(Blob(AtLevel(50, 5))) { FailNext = true };
            var session = new CloudSession(cloud, new MemoryCloudSaveFlags(), Phone);
            Profile local = AtLevel(2, 1);
            CloudSessionStart? start = null;
            session.Start(() => local, s => start = s);
            Assert.That(start!.State, Is.EqualTo(CloudSessionState.Unsynced), "sin red al arrancar");
            Assert.That(session.Upload(local), Is.False, "subir sin fusionar pisaría una nube con 50 niveles");
            Assert.That(session.OnLevelWon(local) || session.OnLevelWon(local) || session.OnLevelWon(local), Is.False);
            Assert.That(Read(cloud.Blob!).Progress.MaxLevel, Is.EqualTo(50));

            Assert.That(session.Retry(() => local, s => start = s), Is.True, "al volver de segundo plano");
            Assert.That(start.State, Is.EqualTo(CloudSessionState.Synced));
            Assert.That(start.Report!.Result, Is.EqualTo(CloudSyncResult.ConflictCloudWon));
        }

        [Test]
        public void Session_WithADeletePending_DoesNotDownloadTheDeletedAccount()
        {
            var cloud = new FakeCloudSave(Blob(AtLevel(70, 5)));
            var flags = new MemoryCloudSaveFlags { DeletePending = true };
            var session = new CloudSession(cloud, flags, Phone);
            Profile fresh = AtLevel(0, 0);
            CloudSessionStart? start = null;
            session.Start(() => fresh, s => start = s);
            Assert.That(start!.Erase, Is.EqualTo(CloudEraseResult.Requested));
            Assert.That(start.Report, Is.Null, "ni se fusiona ni se sube");
            Assert.That(start.State, Is.EqualTo(CloudSessionState.Unsynced));
            Assert.That(cloud.Blob, Is.Null);
            Assert.That(session.Upload(fresh), Is.False);

            // Arranque siguiente: el borrado consta y la sesión sigue con normalidad.
            var next = new CloudSession(cloud, flags, Phone);
            next.Start(() => fresh, s => start = s);
            Assert.That(start.Erase, Is.EqualTo(CloudEraseResult.Verified));
            Assert.That(start.State, Is.EqualTo(CloudSessionState.Synced));
            Assert.That(flags.DeletePending, Is.False);
        }

        [Test]
        public void Session_EraseForAccountDelete_MarksThePendingDelete_AndClosesTheSession()
        {
            var cloud = new FakeCloudSave(Blob(AtLevel(15, 5)));
            var flags = new MemoryCloudSaveFlags();
            var session = new CloudSession(cloud, flags, Phone);
            Profile local = AtLevel(15, 5);
            session.Start(() => local, _ => { });
            Assert.That(session.State, Is.EqualTo(CloudSessionState.Synced));

            CloudWriteResult? result = null;
            session.EraseForAccountDelete(r => result = r);
            Assert.That(result, Is.EqualTo(CloudWriteResult.Ok));
            Assert.That(cloud.Blob, Is.Null);
            Assert.That(flags.DeletePending, Is.True, "se comprueba en el arranque siguiente");
            Assert.That(session.State, Is.EqualTo(CloudSessionState.Closed));
            int saves = cloud.SaveCount;
            Assert.That(session.Upload(local), Is.False, "tras el borrado (lote cerrado) no se escribe");
            Assert.That(session.Start(() => local, _ => { }), Is.False);
            Assert.That(cloud.SaveCount, Is.EqualTo(saves));
        }

        [Test]
        public void Session_EraseWithoutSession_StillMarksThePendingDelete()
        {
            var flags = new MemoryCloudSaveFlags();
            var session = new CloudSession(FakeCloudSave.WithoutSession(), flags, Phone);
            CloudWriteResult? result = null;
            session.EraseForAccountDelete(r => result = r);
            Assert.That(result, Is.EqualTo(CloudWriteResult.NoSession));
            Assert.That(flags.DeletePending, Is.True);
        }

        [Test]
        public void Session_LateAnswers_ArriveThroughDone_AndUploadsCoalesce()
        {
            var cloud = new FakeCloudSave { Deferred = true };
            var session = new CloudSession(cloud, new MemoryCloudSaveFlags(), Phone);
            Profile local = AtLevel(6, 6);
            CloudSessionStart? start = null;
            session.Start(() => local, s => start = s);
            Assert.That(start, Is.Null, "Play Games responde más tarde");
            Assert.That(session.State, Is.EqualTo(CloudSessionState.Starting));
            Assert.That(session.Upload(local), Is.False, "ni durante la fusión");
            cloud.Flush();
            Assert.That(start!.State, Is.EqualTo(CloudSessionState.Synced));

            int saves = cloud.SaveCount;
            Assert.That(session.Upload(local), Is.True);
            Assert.That(session.Upload(local), Is.True, "en vuelo: se sube otra vez al terminar");
            Assert.That(session.Upload(local), Is.True);
            cloud.Flush();
            Assert.That(cloud.SaveCount, Is.EqualTo(saves + 2), "dos subidas, no tres");
        }

        [Test]
        public void APlayConflict_WithANewerSchema_KeepsThatVersionAsItIs()
        {
            // 08 §4.4: una versión de otro teléfono ya actualizado no se pisa con la vieja (revisión de D-042).
            byte[] old = Blob(AtLevel(80, 9));
            byte[] newerA = Encoding.UTF8.GetBytes("{\"profile_schema\": " + (Profile.CurrentSchema + 1) + ", \"progress\": {\"max_level\": 3}}");
            byte[] newerB = Encoding.UTF8.GetBytes("{\"profile_schema\": " + (Profile.CurrentSchema + 2) + ", \"progress\": {\"max_level\": 4}}");
            Assert.That(CloudSync.ResolveSnapshotConflict(old, newerB, Phone), Is.EqualTo(newerB), "la nueva va de unmerged");
            Assert.That(CloudSync.ResolveSnapshotConflict(newerA, old, Phone), Is.EqualTo(newerA), "la nueva va de original");
            Assert.That(CloudSync.ResolveSnapshotConflict(newerA, newerB, Phone), Is.EqualTo(newerA), "las dos nuevas: la original");
        }

        [Test]
        public void Session_Stop_WhileStarting_NeitherMergesNorWrites_AndLeavesNoDeleteMark()
        {
            // La banda pasa a <13 en Ajustes con la nube respondiendo (D-033): lo que llegue ya no fusiona ni escribe.
            var cloud = new FakeCloudSave(Blob(AtLevel(4, 1))) { Deferred = true };
            var flags = new MemoryCloudSaveFlags();
            var session = new CloudSession(cloud, flags, Phone);
            Profile local = AtLevel(30, 3); // gana el local: la fusión querría subirlo
            CloudSessionStart? start = null;
            session.Start(() => local, s => start = s);
            Assert.That(session.State, Is.EqualTo(CloudSessionState.Starting));
            session.Stop();
            cloud.Flush();
            Assert.That(start!.State, Is.EqualTo(CloudSessionState.Closed));
            Assert.That(cloud.SaveCount, Is.EqualTo(0), "ni la subida de la fusión");
            Assert.That(Read(cloud.Blob!).Progress.MaxLevel, Is.EqualTo(4));
            Assert.That(flags.DeletePending, Is.False, "parar no es borrar");
            Assert.That(session.Upload(local), Is.False);
            Assert.That(session.Start(() => local, _ => { }), Is.False);
        }

        [Test]
        public void Session_EraseForAccountDelete_ClosesWritesAtOnce_EvenWithASaveInFlight()
        {
            var cloud = new FakeCloudSave();
            var session = new CloudSession(cloud, new MemoryCloudSaveFlags(), Phone);
            Profile local = AtLevel(15, 5);
            session.Start(() => local, _ => { });
            cloud.Deferred = true;
            Assert.That(session.Upload(local), Is.True); // en vuelo
            Assert.That(session.Upload(local), Is.True); // se repetiría al terminar
            session.EraseForAccountDelete();
            cloud.Flush();
            Assert.That(cloud.Blob, Is.Null, "lo último que llega es el borrado; la subida repetida no sale");
            Assert.That(session.State, Is.EqualTo(CloudSessionState.Closed));
        }

        [Test]
        public void CloudSync_ReportsTheRemoteProfile_ForALateRemerge()
        {
            var cloud = new FakeCloudSave(Blob(AtLevel(40, 7)));
            CloudSyncReport? report = null;
            CloudSync.AtStartup(AtLevel(10, 1), cloud, Phone, r => report = r);
            Assert.That(report!.Remote, Is.Not.Null);
            Assert.That(report.Remote!.Progress.MaxLevel, Is.EqualTo(40));
            CloudSync.AtStartup(AtLevel(10, 1), new FakeCloudSave(), Phone, r => report = r);
            Assert.That(report!.Remote, Is.Null, "sin nube");
        }

        [Test]
        public void TheBlob_CarriesNeitherTheAgeBandNorTheDeletionCode_AndPreD042SignaturesStillVerify()
        {
            // 08 §7: la banda declarada vive solo en el dispositivo; el código de borrado tampoco sube a Google (D-042).
            Profile local = AtLevel(12, 3);
            local.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, SampleProfiles.T0, true, false, AgeBand.Teen);
            local.AccountCode = "3HZ7M2Q9XK4T8VB1NC6DRF0WPE";
            var cloud = new FakeCloudSave();
            CloudSync.Upload(local, cloud, Phone, (_, __) => { });
            string json = Encoding.UTF8.GetString(cloud.Blob!);
            Assert.That(json, Does.Not.Contain("\"consent\""));
            Assert.That(json, Does.Not.Contain("account_code"));
            Assert.That(ProfileIntegrity.Verify(Read(cloud.Blob!)), Is.EqualTo(IntegrityStatus.Valid), "el blob se comprueba igual");
            Assert.That(local.Consent, Is.Not.Null, "subir no cambia el perfil");
            Assert.That(ProfileIntegrity.Verify(local), Is.EqualTo(IntegrityStatus.Valid));

            // Un perfil firmado antes de D-042 (con consent y account_code en lo firmado) no pasa a «no cuadra».
            local.Integrity.Hmac = ProfileHmac.Sign(LegacyPayload(local), ProfileHmac.DeriveKey(local.Integrity.DeviceFirst!));
            Assert.That(ProfileIntegrity.Verify(local), Is.EqualTo(IntegrityStatus.Valid));
            local.Wallet.Seeds += 1000;
            Assert.That(ProfileIntegrity.Verify(local), Is.EqualTo(IntegrityStatus.Mismatch), "y una edición sigue sin cuadrar");
        }

        private static string LegacyPayload(Profile p)
        {
            string? hmac = p.Integrity.Hmac;
            string? queue = p.PurchaseQueue;
            p.Integrity.Hmac = null;
            p.PurchaseQueue = null;
            try
            {
                return ProfileJson.Serialize(p);
            }
            finally
            {
                p.Integrity.Hmac = hmac;
                p.PurchaseQueue = queue;
            }
        }

        // Puerto falso de Saved Games: un snapshot en memoria, respuestas en el acto y fallos o conflictos a la carta.
        private sealed class FakePort : ISavedGamesPort
        {
            public bool Configured { get; set; } = true;

            public List<string> Calls { get; } = new List<string>();

            public byte[]? Data { get; set; }

            public string? LastName { get; private set; }

            public int Commits { get; private set; }

            public SavedGamesStatus NextOpen { get; set; } = SavedGamesStatus.Ok;

            public SavedGamesStatus NextCommit { get; set; } = SavedGamesStatus.Ok;

            public (byte[] original, byte[] unmerged)? Conflict { get; set; }

            public void SignInSilently(Action<bool> done)
            {
                Calls.Add("signin");
                done(true);
            }

            public void Open(string name, SnapshotConflictResolver resolve, Action<SavedGamesStatus, ISavedGamesSnapshot?> done)
            {
                Calls.Add("open");
                LastName = name;
                SavedGamesStatus status = NextOpen;
                NextOpen = SavedGamesStatus.Ok;
                if (status != SavedGamesStatus.Ok)
                {
                    done(status, null);
                    return;
                }

                if (Conflict.HasValue)
                {
                    Data = resolve(Conflict.Value.original, Conflict.Value.unmerged);
                    Conflict = null;
                }

                done(SavedGamesStatus.Ok, new Handle());
            }

            public void Read(ISavedGamesSnapshot snapshot, Action<SavedGamesStatus, byte[]?> done)
            {
                Calls.Add("read");
                done(SavedGamesStatus.Ok, Data == null ? Array.Empty<byte>() : (byte[])Data.Clone());
            }

            public void Commit(ISavedGamesSnapshot snapshot, byte[] data, Action<SavedGamesStatus> done)
            {
                Calls.Add("commit");
                SavedGamesStatus status = NextCommit;
                NextCommit = SavedGamesStatus.Ok;
                if (status == SavedGamesStatus.Ok)
                {
                    Data = (byte[])data.Clone();
                    Commits++;
                }

                done(status);
            }

            public void Delete(ISavedGamesSnapshot snapshot)
            {
                Calls.Add("delete");
                Data = null;
            }

            private sealed class Handle : ISavedGamesSnapshot
            {
            }
        }
    }
}
