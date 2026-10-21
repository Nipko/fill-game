#nullable enable
using System;

namespace AF.LiveOps
{
    /// <summary>
    /// Lo que las sesiones guardan en el perfil (nota <c>docs/dev/sesiones-notificaciones.md</c> §2): <see cref="Sessions"/> es
    /// <c>stats.sessions</c> de 08 §4.3, y <see cref="LastSeenAt"/>, el campo nuevo <c>stats.last_seen_at</c> [P], la última vez que
    /// la app estuvo en primer plano (al empezar una sesión y al pasar a segundo plano), y <see cref="LastWinAt"/>, el campo nuevo
    /// <c>stats.last_win_at</c> [P]. Con el primero se miden los días sin sesión del
    /// Regalo de regreso (06 §9; <c>af_comeback_gift.days_away</c>, 08 §8.2). Solo datos: las reglas son de
    /// <see cref="SessionTracker"/>.
    /// </summary>
    public sealed class SessionRecord
    {
        /// <summary><c>stats.sessions</c>: sesiones empezadas (Q-M4 de D-033).</summary>
        public int Sessions { get; set; }

        /// <summary><c>stats.last_seen_at</c> [P]: última vez en primer plano, UTC; null si nunca se apuntó (perfil anterior a B6).</summary>
        public DateTime? LastSeenAt { get; set; }

        /// <summary>
        /// <c>stats.last_win_at</c> [P]: la última victoria (campaña o Reto), UTC; null si no se sabe. La necesita «Crecida te espera»
        /// (06 §8.3: <c>streak_reminder_hours</c> sin victoria).
        /// </summary>
        public DateTime? LastWinAt { get; set; }

        /// <summary>Copia.</summary>
        public SessionRecord Clone()
        {
            return new SessionRecord { Sessions = Sessions, LastSeenAt = LastSeenAt, LastWinAt = LastWinAt };
        }
    }

    /// <summary>
    /// Dónde viven las sesiones: en el juego, <c>stats</c> del perfil (parche B6-profile, <c>ProfileSessionStore</c>); en los tests y
    /// en desarrollo sin el parche, <see cref="InMemorySessionStore"/>. Mismo patrón que <see cref="IDailyChallengeStore"/>:
    /// <see cref="Save"/> no lanza por un fallo de escritura.
    /// </summary>
    public interface ISessionStore
    {
        /// <summary>El registro vivo (nunca null).</summary>
        SessionRecord Load();

        /// <summary>Guarda <paramref name="record"/>; si la escritura falla, queda pendiente para el siguiente guardado del perfil.</summary>
        void Save(SessionRecord record);
    }

    /// <summary><see cref="ISessionStore"/> en memoria (tests y desarrollo sin el parche del perfil).</summary>
    public sealed class InMemorySessionStore : ISessionStore
    {
        private SessionRecord _record;

        /// <summary>Crea el almacén con <paramref name="initial"/> (o vacío).</summary>
        public InMemorySessionStore(SessionRecord? initial = null)
        {
            _record = initial ?? new SessionRecord();
        }

        /// <summary>Guardados pedidos.</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public SessionRecord Load()
        {
            return _record;
        }

        /// <inheritdoc />
        public void Save(SessionRecord record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            SaveCount++;
        }
    }
}
