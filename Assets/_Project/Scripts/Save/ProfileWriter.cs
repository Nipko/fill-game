#nullable enable
using System;
using System.Threading;

namespace AF.Save
{
    /// <summary>
    /// Escritor del perfil en segundo plano (T-D, opción B, aprobada por el dueño el 2026-10-11; 08 §5.4 y §11). El hilo
    /// principal serializa (<see cref="IProfileByteStore.Serialize"/>, &lt; 1 ms) y entrega los bytes con
    /// <see cref="Enqueue"/>; un único escritor los escribe con <see cref="IProfileByteStore.WriteBytes"/> (<c>.tmp</c>,
    /// <c>Flush(true)</c> y <c>File.Replace</c> con <c>.bak</c>) fuera del hilo principal.
    /// <list type="bullet">
    /// <item><b>Cola de 1, gana el último:</b> si llegan bytes nuevos antes de que el escritor empiece los anteriores, estos
    /// se descartan (cada entrega lleva el perfil entero). Lo que ya está a medias termina.</item>
    /// <item><b>Síncrono cuando hace falta:</b> <see cref="WriteNow"/> descarta la cola, espera a que el escritor acabe lo
    /// que tiene a medias (con un tope) y escribe en el hilo que llama (fin de nivel, cartera, segundo plano y cierre).</item>
    /// <item><b>Fallos:</b> una excepción del escritor nunca sale de su hilo: se guarda y el hilo principal la recoge con
    /// <see cref="TakeFailure"/> (quien entregó los bytes sigue teniendo el perfil en memoria y lo reintenta).</item>
    /// </list>
    /// Hilos: <see cref="Enqueue"/>, <see cref="WriteNow"/>, <see cref="WaitIdle"/>, <see cref="Cancel"/> y
    /// <see cref="TakeFailure"/> se llaman desde un solo hilo (el principal). El escritor corre en el <see cref="ThreadPool"/>,
    /// uno a la vez, y termina en cuanto la cola se vacía: no deja hilos vivos al salir de la escena ni del Play Mode. No lee
    /// el reloj (B1-1: en AF.Save solo <c>SystemClock</c>): lo que tarda cada escritura lo mide la marca de tiempo que
    /// inyecta quien lo crea. Sin paquetes ni dependencias nuevas.
    /// </summary>
    public sealed class ProfileWriter
    {
        private static readonly WaitCallback RunCallback = Run;

        private readonly IProfileByteStore _store;
        private readonly Func<long>? _timestamp;
        private readonly double _msPerTick;
        private readonly object _gate = new object();

        // Abierto mientras el escritor no tiene nada en cola ni a medias (_running = false). Se cambia bajo _gate.
        private readonly ManualResetEventSlim _idle = new ManualResetEventSlim(true);

        // Todo lo que sigue, bajo _gate.
        private byte[]? _queued;
        private long _queuedSeq;
        private bool _running;
        private long _lastSeq;
        private long _writtenSeq;
        private long _failedSeq;
        private Exception? _failure;
        private double _lastBackgroundMs;
        private int _backgroundWrites;

        /// <summary>Crea el escritor sobre <paramref name="store"/>, sin medir lo que tarda cada escritura.</summary>
        public ProfileWriter(IProfileByteStore store)
            : this(store, null, 0)
        {
        }

        /// <summary>Crea el escritor sobre <paramref name="store"/>.</summary>
        /// <param name="store">Almacén donde escribe.</param>
        /// <param name="timestamp">
        /// Marca de tiempo monótona con la que se mide cada escritura (<see cref="LastBackgroundMs"/>), por ejemplo
        /// la del cronómetro de alta resolución de .NET que pasa <c>AF.GameFlow.ProfileSaveBatch</c>; null = sin medir.
        /// </param>
        /// <param name="ticksPerSecond">Unidades de <paramref name="timestamp"/> por segundo.</param>
        public ProfileWriter(IProfileByteStore store, Func<long>? timestamp, long ticksPerSecond)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            if (timestamp != null && ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Hacen falta ticks por segundo para medir.");
            }

            _timestamp = timestamp;
            _msPerTick = timestamp == null ? 0.0 : 1000.0 / ticksPerSecond;
        }

        /// <summary>El almacén donde escribe.</summary>
        public IProfileByteStore Store => _store;

        /// <summary>No tiene nada en cola ni a medias.</summary>
        public bool IsIdle
        {
            get
            {
                lock (_gate)
                {
                    return !_running;
                }
            }
        }

        /// <summary>Está parado y la última entrega (de <see cref="Enqueue"/> o de <see cref="WriteNow"/>) está en disco.</summary>
        public bool AllWritten
        {
            get
            {
                lock (_gate)
                {
                    return !_running && _writtenSeq == _lastSeq;
                }
            }
        }

        /// <summary>Escrituras terminadas en segundo plano (diagnóstico).</summary>
        public int BackgroundWrites
        {
            get
            {
                lock (_gate)
                {
                    return _backgroundWrites;
                }
            }
        }

        /// <summary>
        /// Lo que tardó la última escritura terminada en segundo plano, en ms (<c>.tmp</c>, <c>Flush(true)</c> y
        /// <c>File.Replace</c>); 0 si aún no hubo ninguna o si no se mide.
        /// </summary>
        public double LastBackgroundMs
        {
            get
            {
                lock (_gate)
                {
                    return _lastBackgroundMs;
                }
            }
        }

        /// <summary>
        /// Entrega <paramref name="bytes"/> (de <see cref="IProfileByteStore.Serialize"/>) al escritor y vuelve enseguida. Si ya
        /// había unos esperando, se descartan: gana el último.
        /// </summary>
        public void Enqueue(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            bool start;
            lock (_gate)
            {
                _queued = bytes;
                _queuedSeq = ++_lastSeq;
                start = MarkRunning();
            }

            if (start)
            {
                Start();
            }
        }

        /// <summary>
        /// Escribe <paramref name="bytes"/> ya, en el hilo que llama: descarta lo que había en la cola (es más viejo), espera
        /// a que el escritor termine lo que tenga a medias (como mucho <paramref name="timeoutMs"/>; −1 sin tope) y escribe.
        /// Las excepciones de la escritura llegan a quien llama. Devuelve false, sin escribir, si el escritor no terminó a
        /// tiempo: entonces los bytes quedan en su cola y salen en cuanto acabe.
        /// </summary>
        public bool WriteNow(byte[] bytes, int timeoutMs)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            long seq;
            lock (_gate)
            {
                seq = ++_lastSeq;
                _queued = null;
            }

            if (!WaitIdle(timeoutMs))
            {
                bool start;
                lock (_gate)
                {
                    if (_queued == null || _queuedSeq < seq)
                    {
                        _queued = bytes;
                        _queuedSeq = seq;
                    }

                    start = MarkRunning();
                }

                if (start)
                {
                    Start();
                }

                return false;
            }

            _store.WriteBytes(bytes);
            lock (_gate)
            {
                if (seq > _writtenSeq)
                {
                    _writtenSeq = seq;
                }
            }

            return true;
        }

        /// <summary>
        /// Espera a que el escritor no tenga nada en cola ni a medias, como mucho <paramref name="timeoutMs"/> (−1 sin tope).
        /// Devuelve si lo consiguió.
        /// </summary>
        public bool WaitIdle(int timeoutMs)
        {
            return _idle.Wait(timeoutMs < 0 ? Timeout.Infinite : timeoutMs);
        }

        /// <summary>Descarta lo que espera en la cola (eliminación de cuenta). Lo que ya está a medias termina.</summary>
        public void Cancel()
        {
            lock (_gate)
            {
                _queued = null;
            }
        }

        /// <summary>
        /// Recoge el último fallo del escritor (o null si no hubo) en el hilo principal. <paramref name="latest"/> = ninguna
        /// entrega posterior lo cubre: el perfil de esa entrega no llegó al disco y hay que reintentarlo.
        /// </summary>
        public Exception? TakeFailure(out bool latest)
        {
            lock (_gate)
            {
                Exception? failure = _failure;
                latest = failure != null && _failedSeq == _lastSeq;
                _failure = null;
                return failure;
            }
        }

        private static void Run(object? state)
        {
            ((ProfileWriter)state!).Drain();
        }

        // Bajo _gate: el escritor pasa a estar en marcha; true si hay que lanzarlo.
        private bool MarkRunning()
        {
            if (_running)
            {
                return false;
            }

            _running = true;
            _idle.Reset();
            return true;
        }

        private void Start()
        {
            bool queued;
            try
            {
                queued = ThreadPool.QueueUserWorkItem(RunCallback, this);
            }
            catch (NotSupportedException)
            {
                queued = false;
            }

            if (!queued)
            {
                Drain(); // sin ThreadPool: se escribe en este mismo hilo
            }
        }

        // El bucle del escritor: escribe lo que haya en la cola hasta vaciarla.
        private void Drain()
        {
            while (true)
            {
                byte[] bytes;
                long seq;
                lock (_gate)
                {
                    if (_queued == null)
                    {
                        _running = false;
                        _idle.Set();
                        return;
                    }

                    bytes = _queued;
                    seq = _queuedSeq;
                    _queued = null;
                    if (seq <= _writtenSeq)
                    {
                        continue; // ya hay en disco algo más nuevo
                    }
                }

                long start = _timestamp == null ? 0L : _timestamp();
                Exception? error = null;
                try
                {
                    _store.WriteBytes(bytes);
                }
                catch (Exception e)
                {
                    error = e; // nunca sale del hilo del escritor: lo recoge el principal (TakeFailure)
                }

                double ms = _timestamp == null ? 0.0 : (_timestamp() - start) * _msPerTick;
                lock (_gate)
                {
                    if (error == null)
                    {
                        if (seq > _writtenSeq)
                        {
                            _writtenSeq = seq;
                        }

                        _lastBackgroundMs = ms;
                        _backgroundWrites++;
                    }
                    else if (seq >= _failedSeq)
                    {
                        _failedSeq = seq;
                        _failure = error;
                    }
                }
            }
        }
    }
}
