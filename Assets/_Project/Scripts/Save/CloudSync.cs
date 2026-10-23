#nullable enable
using System;
using System.Text;
using AF.Platform;
using AF.Save.Json;

namespace AF.Save
{
    /// <summary>Lo que dejó <see cref="CloudSync.AtStartup"/>.</summary>
    public sealed class CloudSyncReport
    {
        internal CloudSyncReport(bool ran, CloudSyncResult result, CloudMergeReason? reason, Profile profile, bool replaced,
            IntegrityStatus localIntegrity, IntegrityStatus? cloudIntegrity, CloudWriteResult? upload, int bytes, string? error, Profile? remote = null)
        {
            Remote = remote;
            Ran = ran;
            Result = result;
            Reason = reason;
            Profile = profile;
            Replaced = replaced;
            LocalIntegrity = localIntegrity;
            CloudIntegrity = cloudIntegrity;
            Upload = upload;
            Bytes = bytes;
            Error = error;
        }

        /// <summary>Hubo sesión y se habló con la nube (si no, nada cambió y no se registra <c>af_save_sync</c>).</summary>
        public bool Ran { get; }

        /// <summary><c>af_save_sync.result</c>.</summary>
        public CloudSyncResult Result { get; }

        /// <summary>El caso de <see cref="CloudMerge"/>, o null si no se fusionó (sin sesión o error).</summary>
        public CloudMergeReason? Reason { get; }

        /// <summary>El perfil con el que seguir: el local (el mismo objeto) o, si <see cref="Replaced"/>, el fusionado.</summary>
        public Profile Profile { get; }

        /// <summary>El perfil fusionado sustituye al local: quien llama lo guarda en el dispositivo.</summary>
        public bool Replaced { get; }

        /// <summary>Firma del perfil local.</summary>
        public IntegrityStatus LocalIntegrity { get; }

        /// <summary>Firma del de la nube, o null si no había.</summary>
        public IntegrityStatus? CloudIntegrity { get; }

        /// <summary>Resultado de la subida, o null si no hizo falta subir.</summary>
        public CloudWriteResult? Upload { get; }

        /// <summary>Bytes del blob subido o leído (<c>af_save_sync.bytes</c>).</summary>
        public int Bytes { get; }

        /// <summary>
        /// El perfil leído de la nube (migrado al esquema actual), o null si no había o no se pudo leer. Sirve para volver a fusionar
        /// con el perfil vivo si la respuesta llega tarde y el local cambió mientras (D-042; <c>GameBootstrap.TickCloud</c>).
        /// </summary>
        public Profile? Remote { get; }

        /// <summary>Qué falló (nube caída, blob ilegible o de una versión más nueva), o null.</summary>
        public string? Error { get; }
    }

    /// <summary>
    /// Sincronización con la nube al arrancar (GDD 08 §11, fila «Nube Android 1.0»: «descarga al arrancar con sesión»; 08 §2.3;
    /// bloque B8), antes de crear la cartera y los demás servicios, para que el juego empiece ya con el perfil fusionado:
    /// <list type="number">
    /// <item>sin sesión: nada (<see cref="CloudSyncReport.Ran"/> = false);</item>
    /// <item>lee <c>af_main</c>; si falla, sigue el local (<c>failed</c>);</item>
    /// <item>lee el blob como un perfil (con las migraciones de 08 §4.4); uno más nuevo que la app no se toca ni se pisa
    /// (<c>failed</c>); uno ilegible se sustituye por el local [P];</item>
    /// <item>comprueba las dos firmas (<see cref="ProfileIntegrity.Verify"/>) y fusiona (<see cref="CloudMerge.Resolve"/>);</item>
    /// <item>firma el resultado y, si cambió respecto a la nube, lo sube.</item>
    /// </list>
    /// La vuelta a acreditar con <c>restoreLedger</c> tras fusionar (08 §11) llega con Functions; la parte local está en
    /// <c>AF.Economy.LocalRevocation.ApplyLedger</c>. Las subidas periódicas (cada <c>save_cloud_interval_levels</c> niveles, tras
    /// cada compra y al pasar a segundo plano) se enganchan con el SDK: <see cref="Upload"/> ya firma y sube.
    /// </summary>
    public static class CloudSync
    {
        /// <summary>Sincroniza <paramref name="local"/> con <paramref name="cloud"/> y llama a <paramref name="done"/> con el resultado.</summary>
        /// <param name="localIntegrity">
        /// La firma del local tal como se leyó del disco (antes de los extras de desarrollo y de cualquier cambio en memoria); null =
        /// comprobarla ahora. Un perfil migrado al leerlo se pasa como <see cref="IntegrityStatus.Unsigned"/>.
        /// </param>
        public static void AtStartup(Profile local, ICloudSave cloud, IDeviceIdentity device, Action<CloudSyncReport> done, IntegrityStatus? localIntegrity = null)
        {
            if (local == null)
            {
                throw new ArgumentNullException(nameof(local));
            }

            if (cloud == null)
            {
                throw new ArgumentNullException(nameof(cloud));
            }

            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            IntegrityStatus localStatus = localIntegrity ?? ProfileIntegrity.Verify(local);
            if (!cloud.HasSession)
            {
                done(new CloudSyncReport(false, CloudSyncResult.Ok, null, local, false, localStatus, null, null, 0, null));
                return;
            }

            cloud.Load(loaded =>
            {
                switch (loaded.Status)
                {
                    case CloudLoadStatus.NoSession:
                        done(new CloudSyncReport(false, CloudSyncResult.Ok, null, local, false, localStatus, null, null, 0, null));
                        return;
                    case CloudLoadStatus.Failed:
                        done(new CloudSyncReport(true, CloudSyncResult.Failed, null, local, false, localStatus, null, null, 0, loaded.Error ?? "la nube no respondió"));
                        return;
                }

                Profile? remote = null;
                IntegrityStatus? remoteIntegrity = null;
                int readBytes = loaded.Blob?.Length ?? 0;
                if (loaded.Status == CloudLoadStatus.Loaded)
                {
                    BlobRead read = ReadBlob(loaded.Blob!, out remote, out bool migrated, out string? error);
                    if (read == BlobRead.TooNew)
                    {
                        done(new CloudSyncReport(true, CloudSyncResult.Failed, null, local, false, localStatus, null, null, readBytes, error));
                        return;
                    }

                    // Migrado, lo firmado ya no es lo que se lee: no se puede comprobar (revisión de B8) [P].
                    remoteIntegrity = remote == null ? (IntegrityStatus?)null : migrated ? IntegrityStatus.Unsigned : ProfileIntegrity.Verify(remote);
                }

                CloudMergeResult merge = CloudMerge.Resolve(
                    local, remote, ProfileIntegrity.IsCoherent(localStatus), remoteIntegrity.HasValue && ProfileIntegrity.IsCoherent(remoteIntegrity.Value));
                Profile result = merge.LocalChanged ? merge.Merged : local;
                // #3379 (D-042): la última subida puede no constar en la nube (sin red o fallida): se vuelve a subir aunque la
                // fusión no lo pida.
                bool resync = cloud is ICloudResync pending && pending.NeedsResync;
                if (!merge.CloudChanged && !resync)
                {
                    done(new CloudSyncReport(true, merge.SyncResult, merge.Reason, result, merge.LocalChanged, localStatus, remoteIntegrity, null, readBytes, null, remote));
                    return;
                }

                Upload(result, cloud, device, (write, bytes) =>
                    done(new CloudSyncReport(true, merge.SyncResult, merge.Reason, result, merge.LocalChanged, localStatus, remoteIntegrity, write, bytes, null, remote)));
            });
        }

        /// <summary>Firma <paramref name="profile"/> y lo sube a <c>af_main</c>; <paramref name="done"/> recibe el resultado y los bytes.</summary>
        public static void Upload(Profile profile, ICloudSave cloud, IDeviceIdentity device, Action<CloudWriteResult, int> done)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (cloud == null)
            {
                throw new ArgumentNullException(nameof(cloud));
            }

            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            ProfileIntegrity.Sign(profile, device);
            byte[] blob = Encoding.UTF8.GetBytes(CloudMerge.SharedJson(profile)); // sin purchase_queue: solo local (F3-3)
            cloud.Save(blob, write => done(write, blob.Length));
        }

        /// <summary>
        /// Conflicto de Saved Games (D-042; 08 §11, «Conflicto»: resolución manual): Play guarda dos versiones de <c>af_main</c> y
        /// hay que elegir los bytes que quedan. Se leen las dos como perfiles y se fusionan con <see cref="CloudMerge.Resolve"/> (la
        /// <paramref name="original"/> hace de «local»; la fusión de verdad con el local llega después, en <see cref="AtStartup"/>).
        /// Si una es de un esquema más nuevo que esta app, queda esa tal cual (si lo son las dos, la <paramref name="original"/>);
        /// si una no se puede leer, queda la otra tal cual; si ninguna, la <paramref name="original"/>. El resultado se firma con <paramref name="device"/> si cambió.
        /// </summary>
        public static byte[] ResolveSnapshotConflict(byte[] original, byte[] unmerged, IDeviceIdentity device)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            if (unmerged == null)
            {
                throw new ArgumentNullException(nameof(unmerged));
            }

            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            BlobRead readA = ReadBlob(original, out Profile? a, out bool migratedA, out _);
            BlobRead readB = ReadBlob(unmerged, out Profile? b, out bool migratedB, out _);

            // Una versión de un esquema más nuevo que esta app (otro teléfono ya actualizado) no se pisa ni se fusiona: se queda tal
            // cual (08 §4.4, como en AtStartup); si lo son las dos, la original.
            if (readA == BlobRead.TooNew)
            {
                return (byte[])original.Clone();
            }

            if (readB == BlobRead.TooNew)
            {
                return (byte[])unmerged.Clone();
            }

            bool okA = readA == BlobRead.Ok && a != null;
            bool okB = readB == BlobRead.Ok && b != null;
            if (!okA)
            {
                return okB ? (byte[])unmerged.Clone() : (byte[])original.Clone();
            }

            if (!okB)
            {
                return (byte[])original.Clone();
            }

            bool coherentA = migratedA || ProfileIntegrity.IsCoherent(ProfileIntegrity.Verify(a!));
            bool coherentB = migratedB || ProfileIntegrity.IsCoherent(ProfileIntegrity.Verify(b!));
            CloudMergeResult merge = CloudMerge.Resolve(a!, b, coherentA, coherentB);
            if (!merge.LocalChanged)
            {
                return (byte[])original.Clone();
            }

            Profile merged = merge.Merged;
            ProfileIntegrity.Sign(merged, device);
            return Encoding.UTF8.GetBytes(CloudMerge.SharedJson(merged));
        }

        private enum BlobRead
        {
            Ok,
            Unreadable,
            TooNew,
        }

        // El blob como un perfil del esquema actual (migrado si es de uno anterior). Ilegible: remote = null (se pisa con el local).
        private static BlobRead ReadBlob(byte[] blob, out Profile? profile, out bool migrated, out string? error)
        {
            profile = null;
            migrated = false;
            error = null;
            try
            {
                JsonNode root = JsonText.Parse(new UTF8Encoding(false, true).GetString(blob));
                int schema = ProfileJson.ReadSchema(root);
                if (schema > Profile.CurrentSchema)
                {
                    error = "el perfil de la nube es de un esquema más nuevo (" + schema + ") que esta app";
                    return BlobRead.TooNew;
                }

                if (schema < Profile.CurrentSchema)
                {
                    ProfileMigrations.Current.Migrate(root, schema);
                    migrated = true;
                }

                profile = ProfileJson.FromNode(root);
                return BlobRead.Ok;
            }
            catch (FormatException e)
            {
                error = e.Message;
            }
            catch (ArgumentException e)
            {
                error = e.Message; // UTF-8 mal formado (DecoderFallbackException) y valores fuera de rango
            }
            catch (InvalidOperationException e)
            {
                error = e.Message;
            }

            return BlobRead.Unreadable;
        }
    }
}
