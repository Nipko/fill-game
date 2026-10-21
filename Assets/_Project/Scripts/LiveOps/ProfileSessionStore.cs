#nullable enable
using System;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>
    /// <see cref="ISessionStore"/> del juego (parche B6-profile): <c>stats.sessions</c>, <c>stats.last_seen_at</c> y
    /// <c>stats.last_win_at</c> [P] del mismo <see cref="Profile"/> que la cartera, guardado con el mismo <see cref="IProfileStore"/>
    /// (en el juego, la vía síncrona del lote, <c>ProfileSaveBatch.Durable</c>). Así <c>last_seen_at</c> = la vuelta llega al disco
    /// antes que el Regalo de regreso, en el mismo archivo, y una ausencia nunca se cobra dos veces (<see cref="ComebackService"/>).
    /// Como <see cref="ProfileDailyChallengeStore"/>, <see cref="Save"/> no lanza por un fallo de escritura: el cambio sigue en el
    /// perfil en memoria y sale con el siguiente guardado.
    /// </summary>
    public sealed class ProfileSessionStore : ISessionStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private readonly SessionRecord _live;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileSessionStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _live = new SessionRecord
            {
                Sessions = Math.Max(0, profile.Stats.Sessions),
                LastSeenAt = profile.Stats.LastSeenAt,
                LastWinAt = profile.Stats.LastWinAt,
            };
        }

        /// <summary>El último guardado no llegó al disco.</summary>
        public bool PendingSave { get; private set; }

        /// <inheritdoc />
        public SessionRecord Load()
        {
            return _live;
        }

        /// <inheritdoc />
        public void Save(SessionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (!ReferenceEquals(record, _live))
            {
                _live.Sessions = record.Sessions;
                _live.LastSeenAt = record.LastSeenAt;
                _live.LastWinAt = record.LastWinAt;
            }

            _profile.Stats.Sessions = _live.Sessions;
            _profile.Stats.LastSeenAt = _live.LastSeenAt;
            _profile.Stats.LastWinAt = _live.LastWinAt;
            PendingSave = !_store.TrySave(_profile);
        }
    }

    /// <summary>
    /// <see cref="IPushSettingsStore"/> del juego (parche B6-profile): <c>settings.push</c> del perfil (08 §4.3), con el mismo
    /// <see cref="IProfileStore"/>. <see cref="Save"/> no lanza por un fallo de escritura.
    /// </summary>
    public sealed class ProfilePushSettingsStore : IPushSettingsStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private readonly PushSettingsRecord _live;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfilePushSettingsStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            PushSettingsData push = profile.Settings.Push;
            _live = new PushSettingsRecord
            {
                PrePromptShown = push.PrePromptShown,
                Granted = push.Granted,
                PrePromptAt = push.PrePromptAt,
                SecondPromptShown = push.SecondPromptShown,
            };
        }

        /// <summary>El último guardado no llegó al disco.</summary>
        public bool PendingSave { get; private set; }

        /// <inheritdoc />
        public PushSettingsRecord Load()
        {
            return _live;
        }

        /// <inheritdoc />
        public void Save(PushSettingsRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (!ReferenceEquals(record, _live))
            {
                _live.PrePromptShown = record.PrePromptShown;
                _live.Granted = record.Granted;
                _live.PrePromptAt = record.PrePromptAt;
                _live.SecondPromptShown = record.SecondPromptShown;
            }

            PushSettingsData push = _profile.Settings.Push;
            push.PrePromptShown = _live.PrePromptShown;
            push.Granted = _live.Granted;
            push.PrePromptAt = _live.PrePromptAt;
            push.SecondPromptShown = _live.SecondPromptShown;
            PendingSave = !_store.TrySave(_profile);
        }
    }
}
