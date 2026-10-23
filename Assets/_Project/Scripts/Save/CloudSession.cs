#nullable enable
using System;
using AF.Platform;

namespace AF.Save
{
    /// <summary>En qué punto está la nube de esta sesión (<see cref="CloudSession"/>).</summary>
    public enum CloudSessionState
    {
        /// <summary>Aún no se arrancó (o no se puede: edad sin decidir, &lt;13, perfil en solo lectura).</summary>
        Idle = 0,

        /// <summary>Iniciando sesión, retomando un borrado o fusionando.</summary>
        Starting = 1,

        /// <summary>Fusionada: las subidas periódicas están permitidas.</summary>
        Synced = 2,

        /// <summary>Con sesión, pero sin fusionar (la nube no respondió o hay un borrado pendiente): nada se sube hasta reintentar.</summary>
        Unsynced = 3,

        /// <summary>Sin sesión de Play Games (sin configurar, sin cuenta o rechazada): nada se sube.</summary>
        NoSession = 4,

        /// <summary>La cuenta se borró: la nube no vuelve a leer ni escribir en esta sesión.</summary>
        Closed = 5,
    }

    /// <summary>Lo que dejó <see cref="CloudSession.Start"/>.</summary>
    public sealed class CloudSessionStart
    {
        internal CloudSessionStart(CloudSessionState state, CloudEraseResult erase, CloudSyncReport? report)
        {
            State = state;
            Erase = erase;
            Report = report;
        }

        /// <summary>El estado al terminar.</summary>
        public CloudSessionState State { get; }

        /// <summary>El borrado pendiente (si lo había).</summary>
        public CloudEraseResult Erase { get; }

        /// <summary>La fusión (null si no se llegó a fusionar).</summary>
        public CloudSyncReport? Report { get; }
    }

    /// <summary>
    /// La nube de una sesión de juego (D-042; 08 §2.3 y §11; <c>docs/dev/play-games.md</c> §4): ordena el inicio de sesión
    /// silencioso, el borrado pendiente (<see cref="CloudErase"/>), la fusión del arranque (<see cref="CloudSync.AtStartup"/>) y las
    /// subidas (<see cref="Upload"/>: cada <see cref="IntervalLevels"/> victorias, tras cada compra y al pasar a segundo plano).
    /// <list type="bullet">
    /// <item>Solo sube tras haber fusionado en esta sesión (<see cref="CloudSessionState.Synced"/>): subir sin fusionar pisaría una
    /// nube con más progreso. Si la fusión no llegó (sin red), <see cref="Retry"/> la vuelve a intentar al volver de segundo plano.</item>
    /// <item>Quien la crea decide cuándo arrancarla: no con la banda &lt;13 ni antes de declarar la edad (08 §2.3 y §7; D-033).</item>
    /// <item><see cref="EraseForAccountDelete"/> marca el borrado pendiente, lo pide y la cierra: ya no escribe (el lote está cerrado).</item>
    /// </list>
    /// Las respuestas pueden llegar tarde (Play Games es asíncrono): quien llama recibe el resultado por su <c>done</c>.
    /// </summary>
    public sealed class CloudSession
    {
        private readonly ICloudSave _cloud;
        private readonly WriteGate _gate;
        private readonly ICloudSaveFlags _flags;
        private readonly IDeviceIdentity _device;
        private int _levelsSinceUpload;
        private bool _uploading;
        private Profile? _uploadAgain;

        /// <summary>Crea la sesión de nube.</summary>
        public CloudSession(ICloudSave cloud, ICloudSaveFlags flags, IDeviceIdentity device)
        {
            _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
            _gate = new WriteGate(this, cloud);
            _flags = flags ?? throw new ArgumentNullException(nameof(flags));
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>Victorias entre subidas (<c>save_cloud_interval_levels</c>, 08 §9 y §11: 3; rango 1-5).</summary>
        public int IntervalLevels { get; set; } = 3;

        /// <summary>El estado.</summary>
        public CloudSessionState State { get; private set; }

        /// <summary>La nube.</summary>
        public ICloudSave Cloud => _cloud;

        /// <summary>Las marcas persistentes.</summary>
        public ICloudSaveFlags Flags => _flags;

        /// <summary>Subidas pedidas a la nube en esta sesión (tests y sonda).</summary>
        public int Uploads { get; private set; }

        /// <summary>
        /// Arranca (o reintenta) la nube: sesión silenciosa, borrado pendiente y fusión de <paramref name="current"/> (que se lee
        /// cuando llega la nube: el perfil vivo). False, sin hacer nada, si ya está arrancando, fusionada o cerrada.
        /// </summary>
        public bool Start(Func<Profile> current, Action<CloudSessionStart> done, IntegrityStatus? localIntegrity = null)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (State == CloudSessionState.Starting || State == CloudSessionState.Synced || State == CloudSessionState.Closed)
            {
                return false;
            }

            State = CloudSessionState.Starting;
            if (_cloud is ICloudSignIn signIn && !_cloud.HasSession)
            {
                signIn.SignIn(_ => AfterSignIn(current, done, localIntegrity));
            }
            else
            {
                AfterSignIn(current, done, localIntegrity);
            }

            return true;
        }

        /// <summary>Al volver de segundo plano: si la fusión no llegó, otra vez; si ya está fusionada y hay una subida pendiente (#3379), la sube.</summary>
        public bool Retry(Func<Profile> current, Action<CloudSessionStart> done, IntegrityStatus? localIntegrity = null)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (State == CloudSessionState.Unsynced || State == CloudSessionState.NoSession)
            {
                return Start(current, done, localIntegrity);
            }

            if (State == CloudSessionState.Synced && _cloud is ICloudResync pending && pending.NeedsResync)
            {
                Upload(current());
            }

            return false;
        }

        /// <summary>Una victoria: cada <see cref="IntervalLevels"/>, sube <paramref name="profile"/>.</summary>
        public bool OnLevelWon(Profile profile)
        {
            if (State != CloudSessionState.Synced)
            {
                return false;
            }

            _levelsSinceUpload++;
            if (_levelsSinceUpload < Math.Max(1, IntervalLevels))
            {
                return false;
            }

            return Upload(profile);
        }

        /// <summary>
        /// Firma y sube <paramref name="profile"/> (tras una compra, al pasar a segundo plano o cada <see cref="IntervalLevels"/>
        /// victorias). Solo fusionada; si ya hay una subida en vuelo, se sube otra vez al terminar con lo último. False si no sube.
        /// </summary>
        public bool Upload(Profile profile, Action<CloudWriteResult>? done = null)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (State != CloudSessionState.Synced)
            {
                return false;
            }

            _levelsSinceUpload = 0;
            if (_uploading)
            {
                _uploadAgain = profile;
                return true;
            }

            _uploading = true;
            Uploads++;
            CloudSync.Upload(profile, _gate, _device, (result, _) =>
            {
                _uploading = false;
                done?.Invoke(result);
                Profile? again = _uploadAgain;
                _uploadAgain = null;
                if (again != null && State == CloudSessionState.Synced)
                {
                    Upload(again);
                }
            });
            return true;
        }

        /// <summary>
        /// «Eliminar cuenta y datos» (08 §11), tras borrar lo local: marca el borrado pendiente (se comprueba en el arranque
        /// siguiente), pide el borrado de <c>af_main</c> si hay sesión y cierra la sesión de nube: nada más se sube.
        /// </summary>
        public void EraseForAccountDelete(Action<CloudWriteResult>? done = null)
        {
            _flags.DeletePending = true;
            _uploadAgain = null;
            State = CloudSessionState.Closed; // desde ya, ninguna escritura (WriteGate), haya sesión o no
            bool session = _cloud.HasSession;
            if (session)
            {
                _cloud.Delete(result => done?.Invoke(result)); // se pide antes de cerrar: el borrado sí sale
            }

            (_cloud as PlayGamesCloudSave)?.Close(); // y la real tampoco escribe lo que ya estuviera en vuelo
            if (!session)
            {
                done?.Invoke(CloudWriteResult.NoSession);
            }
        }

        /// <summary>
        /// Para la nube de esta sesión sin borrar nada (la banda pasa a &lt;13 desde Ajustes; D-033): <see cref="CloudSessionState.Closed"/>,
        /// sin marca de borrado; lo que esté en vuelo ya no fusiona ni escribe.
        /// </summary>
        public void Stop()
        {
            _uploadAgain = null;
            State = CloudSessionState.Closed;
            (_cloud as PlayGamesCloudSave)?.Close();
        }

        private void AfterSignIn(Func<Profile> current, Action<CloudSessionStart> done, IntegrityStatus? localIntegrity)
        {
            if (State == CloudSessionState.Closed)
            {
                done(new CloudSessionStart(State, CloudEraseResult.NothingPending, null));
                return;
            }

            if (!_cloud.HasSession)
            {
                State = CloudSessionState.NoSession;
                done(new CloudSessionStart(State, CloudEraseResult.NothingPending, null));
                return;
            }

            CloudErase.Resume(_cloud, _flags, erase =>
            {
                if (State == CloudSessionState.Closed)
                {
                    done(new CloudSessionStart(State, erase, null));
                    return;
                }

                if (erase == CloudEraseResult.Requested || erase == CloudEraseResult.Pending)
                {
                    State = CloudSessionState.Unsynced; // la cuenta borrada no se descarga ni se pisa hasta comprobar el borrado
                    done(new CloudSessionStart(State, erase, null));
                    return;
                }

                CloudSync.AtStartup(current(), _gate, _device, report =>
                {
                    if (State == CloudSessionState.Closed)
                    {
                        done(new CloudSessionStart(State, erase, report));
                        return;
                    }

                    State = !report.Ran ? CloudSessionState.NoSession
                        : report.Result == CloudSyncResult.Failed ? CloudSessionState.Unsynced : CloudSessionState.Synced;
                    if (report.Upload.HasValue)
                    {
                        Uploads++;
                    }

                    done(new CloudSessionStart(State, erase, report));
                }, localIntegrity);
            });
        }

        // La nube vista por la sesión: lee y borra como la de debajo, pero no escribe con la sesión cerrada (cuenta borrada o <13),
        // aunque la escritura venga de una fusión o una subida que ya estaba en vuelo.
        private sealed class WriteGate : ICloudSave, ICloudResync
        {
            private readonly CloudSession _session;
            private readonly ICloudSave _inner;

            public WriteGate(CloudSession session, ICloudSave inner)
            {
                _session = session;
                _inner = inner;
            }

            public bool HasSession => _inner.HasSession;

            public bool NeedsResync => _inner is ICloudResync r && r.NeedsResync;

            public void Load(Action<CloudLoadResult> done) => _inner.Load(done);

            public void Save(byte[] blob, Action<CloudWriteResult> done)
            {
                if (_session.State == CloudSessionState.Closed)
                {
                    done(CloudWriteResult.NoSession);
                    return;
                }

                _inner.Save(blob, done);
            }

            public void Delete(Action<CloudWriteResult> done) => _inner.Delete(done);
        }
    }
}
