#nullable enable
using System;

namespace AF.Analytics
{
    /// <summary>
    /// La sesión de la analítica (<c>session_id</c> de 08 §8.1): D-033, Q-M4 (a), «Sesión = arranque en frío o vuelta tras ≥ 30
    /// min en segundo plano». El id es aleatorio y sin datos del jugador [P] (32 cifras hexadecimales); <see cref="Count"/>
    /// cuenta las sesiones de este proceso. Sin motor: el arranque le pasa la hora en <see cref="Pause"/> y <see cref="Resume"/>.
    /// </summary>
    public sealed class SessionTracker
    {
        /// <summary>Tiempo en segundo plano a partir del cual la vuelta es una sesión nueva (Q-M4).</summary>
        public static readonly TimeSpan NewSessionAfter = TimeSpan.FromMinutes(30);

        private readonly Func<string> _newId;
        private DateTime? _pausedUtc;

        /// <summary>Empieza la primera sesión (arranque en frío). <paramref name="newId"/> sustituye al generador (pruebas).</summary>
        public SessionTracker(DateTime nowUtc, Func<string>? newId = null)
        {
            _newId = newId ?? (() => Guid.NewGuid().ToString("N"));
            StartedUtc = nowUtc;
            Id = _newId();
            Count = 1;
        }

        /// <summary>Hay una sesión nueva (tras <see cref="Resume"/>): el id nuevo.</summary>
        public event Action<string>? SessionStarted;

        /// <summary>Id de la sesión en curso.</summary>
        public string Id { get; private set; }

        /// <summary>Inicio de la sesión en curso (UTC).</summary>
        public DateTime StartedUtc { get; private set; }

        /// <summary>Sesiones desde el arranque en frío (1 al empezar).</summary>
        public int Count { get; private set; }

        /// <summary>La app pasa a segundo plano.</summary>
        public void Pause(DateTime nowUtc)
        {
            _pausedUtc = nowUtc;
        }

        /// <summary>La app vuelve. Devuelve true si empieza una sesión nueva (≥ 30 min fuera).</summary>
        public bool Resume(DateTime nowUtc)
        {
            DateTime? paused = _pausedUtc;
            _pausedUtc = null;
            if (!paused.HasValue || nowUtc - paused.Value < NewSessionAfter)
            {
                return false;
            }

            Id = _newId();
            StartedUtc = nowUtc;
            Count++;
            SessionStarted?.Invoke(Id);
            return true;
        }
    }
}
