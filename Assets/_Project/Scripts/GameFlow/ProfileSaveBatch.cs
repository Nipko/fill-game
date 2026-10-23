#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Una sola escritura del perfil por acción del jugador (revisión de B2-5; D-008; 02 R8 y §14; 08 §5.4). La cartera, el
    /// progreso, el nivel en curso, la tienda y los anuncios guardan el mismo <see cref="Profile"/> con su
    /// <see cref="IProfileStore"/>; este almacén envuelve el de verdad y, dentro de un lote (<see cref="Begin"/>), solo anota
    /// que hay cambios: el perfil se escribe una vez al cerrar el lote más externo. Así:
    /// <list type="bullet">
    /// <item><description>la victoria (borrar <c>level_state</c>, progreso, Cosecha y cadencia de intersticiales) es una
    /// escritura, no cuatro;</description></item>
    /// <item><description>comprar el rescate o rendirse cobra y anota la pantalla nueva en la misma escritura: un cierre entre
    /// el cobro y <c>level_state</c> ya no puede reabrir el embudo y cobrar otra vez.</description></item>
    /// </list>
    /// <para>
    /// <b>Escritura en segundo plano</b> (T-D, opción B, aprobada por el dueño el 2026-10-11). Si el almacén de verdad separa la
    /// serialización del disco (<see cref="IProfileByteStore"/>, como <c>LocalProfileStore</c>), el cierre de un lote solo
    /// serializa en el hilo principal (&lt; 1 ms) y entrega los bytes a un único <see cref="ProfileWriter"/>, con cola de 1 en
    /// la que gana el último; el <c>.tmp</c>, el <c>Flush(true)</c> y el <c>File.Replace</c> con <c>.bak</c> (08 §11) van en
    /// su hilo. Esperan al escritor y escriben en síncrono, con un tope de espera (<see cref="SyncTimeoutMs"/>):
    /// <list type="bullet">
    /// <item><description>el fin de nivel (<see cref="RequireDurable"/>, en la victoria y al salir del nivel);</description></item>
    /// <item><description>la cartera (guarda por <see cref="Durable"/>: cada mutación de semillas, canoas o stock);</description></item>
    /// <item><description><see cref="FlushSync"/> (<c>OnApplicationPause(true)</c>, <c>OnApplicationQuit</c> y <c>OnDestroy</c>);</description></item>
    /// <item><description>cualquier guardado fuera de un lote (pasa tal cual y lanza sus excepciones, como espera
    /// <c>AF.Economy.Wallet</c>; si falla, el perfil también queda pendiente en el lote).</description></item>
    /// </list>
    /// Con otro almacén (en memoria, los de los tests) todo es síncrono, como antes de T-D.
    /// </para>
    /// <para>
    /// Si una escritura falla (también la del escritor, que se publica al hilo principal y nunca se lanza desde su hilo), el
    /// perfil queda pendiente (<see cref="PendingSave"/>) y sale con el siguiente guardado o con <see cref="FlushSync"/>; como
    /// todo comparte el mismo perfil, cualquier escritura posterior lo lleva entero. Se usa desde un solo hilo (el principal).
    /// </para>
    /// <para>
    /// Dentro de un lote, <see cref="Save"/> solo anota y nunca falla: quien guarda a través del lote (la cartera, el
    /// progreso, <c>level_state</c>, los registros de anuncios y de la tienda) ve un guardado correcto aunque la escritura del
    /// cierre falle después. Así, <c>TransactionResult.Saved</c> = true y <c>Wallet.PendingSave</c> = false significan
    /// «anotado en el lote»; el único que sabe si llegó al disco es este lote (<see cref="PendingSave"/> y
    /// <see cref="FlushSync"/>). No se pierde nada: lo pendiente sale con la siguiente escritura o con <see cref="FlushSync"/>.
    /// </para>
    /// </summary>
    public sealed class ProfileSaveBatch : IProfileStore
    {
        /// <summary>
        /// Tope de espera al escritor de los guardados síncronos, en ms [P]: si el escritor sigue a medias pasado este
        /// tiempo, lo nuevo queda en su cola y el guardado síncrono cuenta como no hecho (sigue pendiente).
        /// </summary>
        public const int DefaultSyncTimeoutMs = 1000;

        private readonly IProfileStore _inner;
        private readonly IProfileByteStore? _bytes;
        private readonly ProfileWriter? _writer;
        private int _depth;
        private bool _durable;
        private bool _closed;
        private Profile? _pending;

        // El último perfil entregado al escritor: si su escritura falla, vuelve a estar pendiente.
        private Profile? _handedOff;

        /// <summary>Envuelve <paramref name="inner"/>, con escritor en segundo plano si es un <see cref="IProfileByteStore"/>.</summary>
        public ProfileSaveBatch(IProfileStore inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (inner is IProfileByteStore bytes)
            {
                _bytes = bytes;
                // AF.Save no lee el reloj (B1-1): la marca de tiempo con la que el escritor mide bg_ms la pone este ensamblado.
                _writer = new ProfileWriter(bytes, Stopwatch.GetTimestamp, Stopwatch.Frequency);
            }

            Durable = new DurableStore(this);
        }

        /// <summary>El almacén de verdad.</summary>
        public IProfileStore Inner => _inner;

        /// <summary>
        /// El mismo lote, pero cada guardado pide escritura síncrona (<see cref="RequireDurable"/>): lo usa la cartera, para que
        /// una mutación de semillas, canoas o stock esté en disco al cerrar su acción.
        /// </summary>
        public IProfileStore Durable { get; }

        /// <summary>El escritor en segundo plano (null si el almacén de verdad no es un <see cref="IProfileByteStore"/>).</summary>
        public ProfileWriter? Writer => _writer;

        /// <summary>Hay un lote abierto.</summary>
        public bool InBatch => _depth > 0;

        /// <summary>
        /// El perfil se borró (<see cref="Delete"/>): el lote queda cerrado para siempre y descarta todo guardado posterior, venga
        /// de donde venga (la sesión al pasar a segundo plano, la cartera, un lote que se cierra después), para que nada vuelva a
        /// crear <c>profile.json</c> con el perfil borrado («Eliminar cuenta y datos», 08 §11).
        /// </summary>
        public bool Closed => _closed;

        /// <summary>
        /// Hay cambios que no se han entregado a ninguna escritura: lote abierto con cambios, o escritura fallida (también la del
        /// escritor, ya recogida). Lo que está en la cola del escritor no cuenta: para saber si está en disco,
        /// <see cref="FlushSync"/>.
        /// </summary>
        public bool PendingSave
        {
            get
            {
                CollectWriterFailure();
                return _pending != null;
            }
        }

        /// <summary>Tope de espera al escritor de los guardados síncronos, en ms (<see cref="DefaultSyncTimeoutMs"/>).</summary>
        public int SyncTimeoutMs { get; set; } = DefaultSyncTimeoutMs;

        /// <summary>Escrituras pedidas (diagnóstico: una por acción del jugador, en segundo plano o en síncrono).</summary>
        public int WriteCount { get; private set; }

        /// <summary>
        /// Lo que tardó en el hilo principal la última escritura pedida, en ms: serializar y entregar al escritor si fue en
        /// segundo plano; esperar al escritor y escribir si fue síncrona (o sin escritor).
        /// </summary>
        public double LastWriteMs { get; private set; }

        /// <summary>La última escritura pedida se entregó al escritor (false: fue síncrona).</summary>
        public bool LastWriteInBackground { get; private set; }

        /// <summary>Lo que tardó la última escritura terminada del escritor, en ms (<c>.tmp</c>, fsync y Replace); 0 sin escritor.</summary>
        public double LastBackgroundMs => _writer == null ? 0.0 : _writer.LastBackgroundMs;

        /// <summary>Fallos del escritor recogidos en el hilo principal (diagnóstico).</summary>
        public int BackgroundFailures { get; private set; }

        /// <summary>El último fallo del escritor recogido (tipo y mensaje), para el registro; null si no hubo.</summary>
        public string? LastBackgroundError { get; private set; }

        /// <summary>
        /// Abre un lote: hasta que se cierre (<see cref="ProfileSaveScope.Dispose"/>), los guardados solo se anotan. Los lotes se
        /// anidan; escribe el cierre del más externo. Para usar con <c>using</c>: sin asignaciones.
        /// </summary>
        public ProfileSaveScope Begin()
        {
            _depth++;
            return new ProfileSaveScope(this);
        }

        /// <summary>
        /// El lote abierto se escribirá en síncrono al cerrarse, tras esperar al escritor (fin de nivel y cartera). Fuera de un
        /// lote no hace nada: esos guardados ya son síncronos.
        /// </summary>
        public void RequireDurable()
        {
            if (_depth > 0)
            {
                _durable = true;
            }
        }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            return _inner.Load();
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (_closed)
            {
                return; // perfil borrado: no se vuelve a escribir
            }

            CollectWriterFailure();
            if (_depth > 0)
            {
                _pending = profile;
                return;
            }

            try
            {
                Write(profile);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidOperationException)
            {
                // Fuera de un lote el error llega a quien guarda (AF.Economy.Wallet lo espera), pero el lote no lo olvida: el
                // perfil sigue pendiente y sale con el siguiente guardado o con FlushSync, que solo da true con él en disco.
                _pending = profile;
                throw;
            }

            if (ReferenceEquals(_pending, profile))
            {
                _pending = null;
            }
        }

        /// <inheritdoc />
        /// <remarks>Cierra el lote (<see cref="Closed"/>): desde aquí, todo guardado se descarta.</remarks>
        public void Delete()
        {
            _closed = true;
            _pending = null;
            _handedOff = null;
            if (_writer != null)
            {
                _writer.Cancel();
                _writer.WaitIdle(SyncTimeoutMs);
            }

            _inner.Delete();
            _writer?.TakeFailure(out _);
        }

        /// <summary>
        /// Fuera de un lote, deja el perfil en disco: <see cref="FlushSync"/> con <see cref="SyncTimeoutMs"/>. Devuelve si no
        /// queda nada por escribir.
        /// </summary>
        public bool Flush()
        {
            return FlushSync(SyncTimeoutMs);
        }

        /// <summary>
        /// Espera al escritor y escribe en síncrono lo que falte (<c>OnApplicationPause(true)</c>, <c>OnApplicationQuit</c> y el
        /// fin de nivel), con un tope de <paramref name="timeoutMs"/> ms de espera. Devuelve true solo cuando el último perfil
        /// está en disco; false si sigue pendiente (escritura fallida o lote abierto) o si el escritor no terminó a tiempo (lo
        /// nuevo queda en su cola).
        /// </summary>
        public bool FlushSync(int timeoutMs)
        {
            if (_closed)
            {
                return true; // perfil borrado: nada que escribir
            }

            CollectWriterFailure();
            if (_depth > 0)
            {
                return _pending == null && LatestOnDisk();
            }

            if (_pending == null && _writer != null)
            {
                if (!_writer.WaitIdle(timeoutMs))
                {
                    return false; // el escritor sigue con el último perfil: saldrá en cuanto acabe
                }

                CollectWriterFailure(); // si falló, el perfil vuelve a estar pendiente y se escribe aquí
                if (_pending == null && !LatestOnDisk())
                {
                    // Parado, sin fallo que recoger y con la última entrega fuera del disco (una escritura síncrona que falló
                    // sin pasar por Save): se vuelve a escribir, para que true signifique siempre «en disco».
                    _pending = _handedOff;
                }
            }

            if (_pending != null)
            {
                TryWriteSync(timeoutMs);
            }

            return _pending == null && LatestOnDisk();
        }

        internal void End()
        {
            if (_depth == 0)
            {
                return;
            }

            _depth--;
            if (_depth > 0)
            {
                return;
            }

            bool durable = _durable;
            _durable = false;
            if (_closed)
            {
                _pending = null; // un lote abierto antes del borrado no escribe al cerrarse
                return;
            }

            CollectWriterFailure();
            if (durable)
            {
                FlushSync(SyncTimeoutMs);
            }
            else if (_pending != null)
            {
                if (_writer != null)
                {
                    HandOff();
                }
                else
                {
                    TryWriteSync(SyncTimeoutMs);
                }
            }
        }

        // Cierre de un lote con escritor: serializar y entregar (lo único que paga el hilo principal).
        private void HandOff()
        {
            Profile profile = _pending!;
            long start = Stopwatch.GetTimestamp();
            WriteCount++;
            LastWriteInBackground = true;
            try
            {
                byte[] bytes = _bytes!.Serialize(profile);
                _writer!.Enqueue(bytes);
                _handedOff = profile;
                _pending = null;
            }
            catch (InvalidOperationException)
            {
                // El almacén protege un perfil más nuevo o una migración fallida (IProfileStore.Save): se sigue en memoria.
            }
            finally
            {
                LastWriteMs = ElapsedMs(start);
            }
        }

        private void TryWriteSync(int timeoutMs)
        {
            Profile profile = _pending!;
            try
            {
                Write(profile, timeoutMs);
                _pending = null;
            }
            catch (IOException)
            {
                // Sigue pendiente: sale con el siguiente guardado o con FlushSync (02 §14: lo peor es reabrir un paso antes).
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (InvalidOperationException)
            {
                // El almacén protege un perfil más nuevo o una migración fallida (IProfileStore.Save): se sigue en memoria.
            }
        }

        private void Write(Profile profile)
        {
            Write(profile, SyncTimeoutMs);
        }

        // Escritura síncrona en el hilo principal: con escritor, espera a que acabe lo que tenga a medias y descarta su cola.
        private void Write(Profile profile, int timeoutMs)
        {
            long start = Stopwatch.GetTimestamp();
            WriteCount++;
            LastWriteInBackground = false;
            try
            {
                if (_writer == null)
                {
                    _inner.Save(profile);
                    return;
                }

                byte[] bytes = _bytes!.Serialize(profile);
                _handedOff = profile;
                if (!_writer.WriteNow(bytes, timeoutMs))
                {
                    throw new IOException("El escritor del perfil sigue ocupado: este guardado queda en su cola.");
                }
            }
            finally
            {
                LastWriteMs = ElapsedMs(start);
            }
        }

        // Publica en el hilo principal el último fallo del escritor: si ninguna entrega posterior lo cubre, el perfil vuelve a
        // estar pendiente (sale con el siguiente guardado o con FlushSync). Nunca lanza la excepción del escritor.
        private void CollectWriterFailure()
        {
            if (_writer == null)
            {
                return;
            }

            Exception? error = _writer.TakeFailure(out bool latest);
            if (error == null)
            {
                return;
            }

            BackgroundFailures++;
            LastBackgroundError = error.GetType().Name + ": " + error.Message;
            if (latest && _pending == null)
            {
                _pending = _handedOff;
            }
        }

        // La última entrega al escritor está en disco, o no hay ninguna que importe (sin escritor, o tras Delete, que descarta
        // la cola a propósito).
        private bool LatestOnDisk()
        {
            return _writer == null || _handedOff == null || _writer.AllWritten;
        }

        private static double ElapsedMs(long start)
        {
            return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }

        // Vista de la cartera: cada guardado pide escritura síncrona.
        private sealed class DurableStore : IProfileStore
        {
            private readonly ProfileSaveBatch _batch;

            public DurableStore(ProfileSaveBatch batch)
            {
                _batch = batch;
            }

            public ProfileLoadResult Load()
            {
                return _batch.Load();
            }

            public void Save(Profile profile)
            {
                _batch.RequireDurable();
                _batch.Save(profile);
            }

            public void Delete()
            {
                _batch.Delete();
            }
        }
    }

    /// <summary>Lote abierto con <see cref="ProfileSaveBatch.Begin"/>; se cierra con <see cref="Dispose"/> (una vez).</summary>
    public readonly struct ProfileSaveScope : IDisposable
    {
        private readonly ProfileSaveBatch? _batch;

        internal ProfileSaveScope(ProfileSaveBatch batch)
        {
            _batch = batch;
        }

        /// <summary>Cierra el lote; el más externo escribe el perfil si hubo cambios. El valor por defecto no hace nada.</summary>
        public void Dispose()
        {
            _batch?.End();
        }
    }
}
