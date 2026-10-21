#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AF.Backend
{
    /// <summary>
    /// Reintentos con espera exponencial (08 §10.3: «error de red → cola local con backoff»). Dos usos:
    /// <see cref="InCall"/>, dentro de una llamada (pocos intentos y esperas cortas, para no alargar «Confirmando…»), y
    /// <see cref="Queue"/>, entre intentos de la cola de verificación de 72 h (<c>VerificationQueue</c>).
    /// Las cifras son [P]: el GDD no las fija y no son claves de Remote Config.
    /// </summary>
    public sealed class RetryPolicy
    {
        public RetryPolicy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay)
        {
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }

            if (baseDelay <= TimeSpan.Zero || maxDelay < baseDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDelay));
            }

            MaxAttempts = maxAttempts;
            BaseDelay = baseDelay;
            MaxDelay = maxDelay;
        }

        /// <summary>Dentro de una llamada [P]: 3 intentos, esperas de 1 y 2 s, tope de 8 s.</summary>
        public static RetryPolicy InCall => new RetryPolicy(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8));

        /// <summary>Cola de verificación [P]: sin tope de intentos (manda el plazo de 72 h), de 30 s a 6 h.</summary>
        public static RetryPolicy Queue => new RetryPolicy(int.MaxValue, TimeSpan.FromSeconds(30), TimeSpan.FromHours(6));

        /// <summary>Intentos totales (el primero incluido).</summary>
        public int MaxAttempts { get; }

        public TimeSpan BaseDelay { get; }

        public TimeSpan MaxDelay { get; }

        /// <summary>
        /// Espera tras <paramref name="failedAttempts"/> fallos seguidos (1 = tras el primero): <c>BaseDelay × 2^(n−1)</c> con tope
        /// <see cref="MaxDelay"/>, o lo que pida el servidor si es más (<paramref name="retryAfter"/>, sin tope).
        /// </summary>
        public TimeSpan DelayFor(int failedAttempts, TimeSpan? retryAfter = null)
        {
            if (failedAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(failedAttempts));
            }

            double factor = Math.Pow(2, Math.Min(failedAttempts - 1, 30));
            double ms = Math.Min(BaseDelay.TotalMilliseconds * factor, MaxDelay.TotalMilliseconds);
            TimeSpan delay = TimeSpan.FromMilliseconds(ms);
            if (retryAfter.HasValue && retryAfter.Value > delay)
            {
                delay = retryAfter.Value;
            }

            return delay;
        }

        /// <summary>
        /// ¿Se reintenta dentro de la llamada tras <paramref name="failedAttempts"/> intentos fallidos con <paramref name="error"/>?
        /// Solo los pasajeros, mientras queden intentos y la espera no pase de <see cref="MaxDelay"/> (un <c>Retry-After</c>
        /// largo se deja para la cola).
        /// </summary>
        public bool ShouldRetry(int failedAttempts, BackendError error, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            if (error == null || !error.IsTransient || failedAttempts >= MaxAttempts)
            {
                return false;
            }

            delay = DelayFor(failedAttempts, error.RetryAfter);
            return delay <= MaxDelay;
        }
    }

    /// <summary>Programa una acción para más tarde, en el hilo principal (las esperas de <see cref="RetryPolicy.InCall"/>).</summary>
    public interface IBackendScheduler
    {
        void After(TimeSpan delay, Action action);
    }

    /// <summary>
    /// <see cref="IBackendScheduler"/> sobre el <see cref="SynchronizationContext"/> del hilo que lo crea (en Unity, el hilo
    /// principal): espera con <see cref="Task.Delay(TimeSpan)"/> y vuelve a ese hilo para ejecutar la acción.
    /// </summary>
    public sealed class SynchronizationContextScheduler : IBackendScheduler
    {
        private readonly SynchronizationContext _context;

        /// <exception cref="InvalidOperationException">El hilo actual no tiene <see cref="SynchronizationContext"/>.</exception>
        public SynchronizationContextScheduler()
        {
            _context = SynchronizationContext.Current
                ?? throw new InvalidOperationException("SynchronizationContextScheduler: créalo en el hilo principal.");
        }

        public void After(TimeSpan delay, Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (delay <= TimeSpan.Zero)
            {
                _context.Post(_ => action(), null);
                return;
            }

            Task.Delay(delay).ContinueWith(_ => _context.Post(__ => action(), null), TaskScheduler.Default);
        }
    }

    /// <summary><see cref="IBackendScheduler"/> manual para los tests: las acciones esperan a <see cref="Advance"/>.</summary>
    public sealed class ManualBackendScheduler : IBackendScheduler
    {
        private readonly System.Collections.Generic.List<Pending> _pending = new System.Collections.Generic.List<Pending>();
        private TimeSpan _now = TimeSpan.Zero;
        private long _sequence;

        /// <summary>Esperas pedidas, en orden.</summary>
        public System.Collections.Generic.List<TimeSpan> Requested { get; } = new System.Collections.Generic.List<TimeSpan>();

        public int PendingCount => _pending.Count;

        public void After(TimeSpan delay, Action action)
        {
            Requested.Add(delay);
            _pending.Add(new Pending(_now + delay, _sequence++, action ?? throw new ArgumentNullException(nameof(action))));
        }

        /// <summary>Avanza el tiempo y ejecuta, por orden, lo que venza (también lo que se programe mientras tanto).</summary>
        public void Advance(TimeSpan by)
        {
            TimeSpan target = _now + by;
            while (true)
            {
                int next = -1;
                for (int i = 0; i < _pending.Count; i++)
                {
                    if (_pending[i].Due <= target
                        && (next < 0 || _pending[i].Due < _pending[next].Due
                            || (_pending[i].Due == _pending[next].Due && _pending[i].Sequence < _pending[next].Sequence)))
                    {
                        next = i;
                    }
                }

                if (next < 0)
                {
                    break;
                }

                Pending p = _pending[next];
                _pending.RemoveAt(next);
                _now = p.Due;
                p.Action();
            }

            _now = target;
        }

        private readonly struct Pending
        {
            public Pending(TimeSpan due, long sequence, Action action)
            {
                Due = due;
                Sequence = sequence;
                Action = action;
            }

            public TimeSpan Due { get; }

            public long Sequence { get; }

            public Action Action { get; }
        }
    }
}
