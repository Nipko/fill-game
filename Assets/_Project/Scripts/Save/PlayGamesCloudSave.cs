#nullable enable
using System;
using AF.Platform;

namespace AF.Save
{
    /// <summary>
    /// <see cref="ICloudSave"/> sobre Play Games Services v2 Saved Games (D-042; 08 §7 #5 y §11; <c>docs/dev/play-games.md</c>),
    /// a través de <see cref="ISavedGamesPort"/>: la lógica vive aquí, sin tipos del plugin, y se prueba con un puerto falso.
    /// <list type="bullet">
    /// <item><b>Sesión:</b> solo silenciosa (<see cref="SignIn"/>; 08 §2.3: «PGS en segundo plano»), sin botón. Sin <c>APP_ID</c>
    /// (<see cref="ISavedGamesPort.Configured"/>) no se llama al SDK: no hay sesión y el juego sigue sin nube.</item>
    /// <item><b>Un archivo:</b> <c>af_main</c> (<see cref="CloudSaveNames.Snapshot"/>), sin portada; el perfil en UTF-8 de
    /// <see cref="CloudSync.Upload"/>, con el tope de <see cref="CloudSaveNames.MaxBlobBytes"/> (800 KB de 08 §11).</item>
    /// <item><b>Snapshot vacío = no hay:</b> abrir crea el archivo si falta, así que uno sin bytes es <see cref="CloudLoadStatus.NotFound"/>.</item>
    /// <item><b>Conflicto de Play</b> (dos versiones del snapshot): resolución manual con <see cref="CloudSync.ResolveSnapshotConflict"/>,
    /// las mismas reglas de <see cref="CloudMerge"/> (08 §11: nunca <c>UseLongestPlaytime</c>).</item>
    /// <item><b>#3379</b> (tras guardar sin conexión, Saved Games puede no volver a subir): una subida que falla o que se hace sin red
    /// deja <see cref="ICloudSaveFlags.ResyncPending"/>, y la sincronización siguiente con red vuelve a subir el perfil
    /// (<see cref="ICloudResync"/>); se limpia con una subida hecha con red.</item>
    /// <item><b>Borrado:</b> el plugin no avisa del resultado de <c>delete</c>; <see cref="Delete"/> responde <see cref="CloudWriteResult.Ok"/>
    /// cuando lo ha pedido, y <see cref="CloudErase"/> lo comprueba en el arranque siguiente.</item>
    /// <item><b>Cerrada</b> (<see cref="Close"/>, tras borrar la cuenta): ya no escribe ni lee nada.</item>
    /// </list>
    /// </summary>
    public sealed class PlayGamesCloudSave : ICloudSave, ICloudResync, ICloudSignIn
    {
        private readonly ISavedGamesPort _port;
        private readonly ICloudSaveFlags _flags;
        private readonly Func<bool> _online;
        private readonly IDeviceIdentity _device;
        private bool _signingIn;
        private Action<bool>? _signInWaiters;

        /// <summary>Crea la nube; <paramref name="online"/> dice si el teléfono tiene red (para #3379).</summary>
        public PlayGamesCloudSave(ISavedGamesPort port, ICloudSaveFlags flags, Func<bool> online, IDeviceIdentity device)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _flags = flags ?? throw new ArgumentNullException(nameof(flags));
            _online = online ?? throw new ArgumentNullException(nameof(online));
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <inheritdoc />
        public bool HasSession { get; private set; }

        /// <summary>Tras <see cref="Close"/> (cuenta borrada): no lee ni escribe.</summary>
        public bool Closed { get; private set; }

        /// <inheritdoc />
        public bool NeedsResync => _flags.ResyncPending;

        /// <inheritdoc />
        public void SignIn(Action<bool> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (Closed || !_port.Configured)
            {
                HasSession = false;
                done(false);
                return;
            }

            if (HasSession)
            {
                done(true);
                return;
            }

            _signInWaiters += done;
            if (_signingIn)
            {
                return;
            }

            _signingIn = true;
            _port.SignInSilently(ok =>
            {
                _signingIn = false;
                HasSession = ok && !Closed;
                Action<bool>? waiters = _signInWaiters;
                _signInWaiters = null;
                waiters?.Invoke(HasSession);
            });
        }

        /// <summary>Tras «Eliminar cuenta y datos»: nada más se lee ni se escribe en esta sesión.</summary>
        public void Close()
        {
            Closed = true;
            HasSession = false;
        }

        /// <inheritdoc />
        public void Load(Action<CloudLoadResult> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!HasSession)
            {
                done(new CloudLoadResult(CloudLoadStatus.NoSession));
                return;
            }

            _port.Open(CloudSaveNames.Snapshot, ResolveConflict, (open, snapshot) =>
            {
                if (open != SavedGamesStatus.Ok || snapshot == null)
                {
                    done(LoadFailure(open, "abrir"));
                    return;
                }

                _port.Read(snapshot, (read, data) =>
                {
                    if (read != SavedGamesStatus.Ok)
                    {
                        done(LoadFailure(read, "leer"));
                        return;
                    }

                    done(data == null || data.Length == 0
                        ? new CloudLoadResult(CloudLoadStatus.NotFound)
                        : new CloudLoadResult(CloudLoadStatus.Loaded, data));
                });
            });
        }

        /// <inheritdoc />
        public void Save(byte[] blob, Action<CloudWriteResult> done)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (Closed)
            {
                done(CloudWriteResult.NoSession); // la cuenta se borró: nada vuelve a la nube
                return;
            }

            if (blob.Length > CloudSaveNames.MaxBlobBytes)
            {
                done(CloudWriteResult.TooLarge);
                return;
            }

            if (!HasSession)
            {
                done(CloudWriteResult.NoSession);
                return;
            }

            byte[] copy = (byte[])blob.Clone();
            _port.Open(CloudSaveNames.Snapshot, ResolveConflict, (open, snapshot) =>
            {
                if (open != SavedGamesStatus.Ok || snapshot == null)
                {
                    done(WriteFailure(open));
                    return;
                }

                if (Closed)
                {
                    done(CloudWriteResult.NoSession); // se borró la cuenta mientras se abría
                    return;
                }

                _port.Commit(snapshot, copy, commit =>
                {
                    if (commit != SavedGamesStatus.Ok)
                    {
                        done(WriteFailure(commit));
                        return;
                    }

                    // #3379: sin red, Play lo guarda en su caché y puede no subirlo nunca; se vuelve a subir con red.
                    _flags.ResyncPending = !_online();
                    done(CloudWriteResult.Ok);
                });
            });
        }

        /// <inheritdoc />
        public void Delete(Action<CloudWriteResult> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!HasSession)
            {
                done(CloudWriteResult.NoSession);
                return;
            }

            _port.Open(CloudSaveNames.Snapshot, ResolveConflict, (open, snapshot) =>
            {
                if (open != SavedGamesStatus.Ok || snapshot == null)
                {
                    done(WriteFailure(open));
                    return;
                }

                _port.Delete(snapshot);
                _flags.ResyncPending = false; // ya no hay nada que volver a subir
                done(CloudWriteResult.Ok);
            });
        }

        private byte[] ResolveConflict(byte[] original, byte[] unmerged)
        {
            return CloudSync.ResolveSnapshotConflict(original, unmerged, _device);
        }

        private CloudLoadResult LoadFailure(SavedGamesStatus status, string step)
        {
            if (status == SavedGamesStatus.AuthError)
            {
                HasSession = false;
                return new CloudLoadResult(CloudLoadStatus.NoSession);
            }

            return new CloudLoadResult(CloudLoadStatus.Failed, error: "Play Games no pudo " + step + " " + CloudSaveNames.Snapshot + ": " + status);
        }

        private CloudWriteResult WriteFailure(SavedGamesStatus status)
        {
            if (status == SavedGamesStatus.AuthError)
            {
                HasSession = false;
                _flags.ResyncPending = true;
                return CloudWriteResult.NoSession;
            }

            _flags.ResyncPending = true;
            return CloudWriteResult.Failed;
        }
    }
}
