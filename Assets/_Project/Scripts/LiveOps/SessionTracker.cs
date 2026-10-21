#nullable enable
using System;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>Cómo empezó una sesión.</summary>
    public enum SessionStartKind
    {
        /// <summary>Arranque en frío de la app.</summary>
        ColdStart = 0,

        /// <summary>Vuelta tras <see cref="SessionTracker.BackgroundGap"/> o más en segundo plano.</summary>
        Resume = 1,
    }

    /// <summary>Una sesión nueva (<see cref="SessionTracker"/>).</summary>
    public readonly struct SessionStart
    {
        /// <summary>Crea el apunte.</summary>
        public SessionStart(SessionStartKind kind, int number, DateTime now, DateTime? previousSeenAt)
        {
            Kind = kind;
            Number = number;
            Now = now;
            PreviousSeenAt = previousSeenAt;
        }

        /// <summary>Arranque en frío o vuelta de segundo plano.</summary>
        public SessionStartKind Kind { get; }

        /// <summary>Número de la sesión, desde 1 (<c>stats.sessions</c> después de contarla).</summary>
        public int Number { get; }

        /// <summary>Hora del inicio, UTC.</summary>
        public DateTime Now { get; }

        /// <summary>La última vez que la app estuvo en primer plano antes de esta sesión, UTC; null en la primera (o sin el dato).</summary>
        public DateTime? PreviousSeenAt { get; }

        /// <summary>Tiempo fuera de la app (0 sin <see cref="PreviousSeenAt"/> o con el reloj atrasado).</summary>
        public TimeSpan Away => PreviousSeenAt.HasValue && Now > PreviousSeenAt.Value ? Now - PreviousSeenAt.Value : TimeSpan.Zero;
    }

    /// <summary>
    /// Cuenta <c>stats.sessions</c> (08 §4.3) con la definición de Q-M4 (a) de D-033: <b>una sesión es un arranque en frío o una
    /// vuelta tras 30 minutos o más en segundo plano</b> (como <c>session_start</c> de Firebase). Lo usan el pre-prompt de push
    /// (<c>push_preprompt_session</c>, 06 §8.1), la Mochila de inicio (<c>eco_starter_first_show</c> = <c>L12_or_session2</c>, 05 §4.1;
    /// B5) y el Regalo de regreso (06 §9: días sin sesión). Apunta además <c>stats.last_seen_at</c> [P] al empezar cada sesión y al
    /// pasar a segundo plano. La hora sale de <see cref="IClock"/>; un reloj que retrocede no abre sesión (la vuelta cuenta como la
    /// misma) [P].
    /// </summary>
    public sealed class SessionTracker
    {
        /// <summary>Tiempo mínimo en segundo plano para que la vuelta sea otra sesión (Q-M4 (a) de D-033: 30 min; no es de 08 §9).</summary>
        public static readonly TimeSpan BackgroundGap = TimeSpan.FromMinutes(30);

        private readonly ISessionStore _store;
        private readonly IClock _clock;
        private DateTime? _backgroundSince;

        /// <summary>Crea el contador.</summary>
        public SessionTracker(ISessionStore store, IClock clock)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Sesiones contadas hasta ahora (<c>stats.sessions</c>).</summary>
        public int Sessions => _store.Load().Sessions;

        /// <summary>La sesión en curso, o null antes de <see cref="OnColdStart"/>.</summary>
        public SessionStart? Current { get; private set; }

        /// <summary>La app está en segundo plano (desde <see cref="OnBackground"/> hasta <see cref="OnForeground"/>).</summary>
        public bool InBackground => _backgroundSince.HasValue;

        /// <summary>Arranque en frío: siempre es una sesión nueva. Una segunda llamada en el mismo proceso no cuenta otra.</summary>
        public SessionStart OnColdStart()
        {
            if (Current.HasValue)
            {
                return Current.Value;
            }

            return Begin(SessionStartKind.ColdStart);
        }

        /// <summary>La app pasa a segundo plano: apunta la hora (<c>stats.last_seen_at</c>) y la guarda.</summary>
        public void OnBackground()
        {
            if (_backgroundSince.HasValue)
            {
                return;
            }

            DateTime now = _clock.UtcNow;
            _backgroundSince = now;
            SessionRecord record = _store.Load();
            record.LastSeenAt = now;
            _store.Save(record);
        }

        /// <summary>
        /// La app vuelve a primer plano. Devuelve la sesión nueva si estuvo fuera <see cref="BackgroundGap"/> o más, o null si sigue
        /// la misma (también sin un <see cref="OnBackground"/> previo o con el reloj atrasado).
        /// </summary>
        public SessionStart? OnForeground()
        {
            DateTime? since = _backgroundSince;
            _backgroundSince = null;
            if (!since.HasValue || !Current.HasValue)
            {
                return null;
            }

            TimeSpan away = _clock.UtcNow - since.Value;
            if (away < BackgroundGap)
            {
                return null;
            }

            return Begin(SessionStartKind.Resume);
        }

        /// <summary>Apunta una victoria (campaña o Reto) en <c>stats.last_win_at</c> [P], para «Crecida te espera».</summary>
        public void OnVictory()
        {
            SessionRecord record = _store.Load();
            record.LastWinAt = _clock.UtcNow;
            _store.Save(record);
        }

        private SessionStart Begin(SessionStartKind kind)
        {
            DateTime now = _clock.UtcNow;
            SessionRecord record = _store.Load();
            DateTime? previous = record.LastSeenAt;
            record.Sessions = record.Sessions < int.MaxValue ? record.Sessions + 1 : int.MaxValue;
            record.LastSeenAt = now;
            _store.Save(record);
            var start = new SessionStart(kind, record.Sessions, now, previous);
            Current = start;
            return start;
        }
    }
}
