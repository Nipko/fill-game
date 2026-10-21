#nullable enable
using System;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>
    /// <see cref="ILoginCalendarStore"/> del juego (parche B4-1-profile; 06 §4.2; 08 §4.3 con Q-M3 (a) [P]; D-033, añadido del
    /// 2026-10-20, puntos 6 y 7): la Bitácora en el bloque <c>calendar</c> del mismo <see cref="Profile"/> que la cartera, y los
    /// marcos de país en <c>entitlements.frames</c>, guardado con el mismo <see cref="IProfileStore"/> (en el juego, la vía síncrona
    /// del lote, <c>ProfileSaveBatch.Durable</c>, como el Reto). Así el avance de <c>next_day</c> y el cobro salen en la misma
    /// escritura del perfil. Mismo patrón que <see cref="ProfileDailyChallengeStore"/>: <see cref="Save"/> no lanza por un fallo
    /// de escritura; el cambio queda en el perfil en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado o con
    /// <see cref="Flush"/>. Un marco nunca se quita del perfil (es un derecho; la fusión de 08 §11 los une).
    /// </summary>
    public sealed class ProfileLoginCalendarStore : ILoginCalendarStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private LoginCalendarRecord _live;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileLoginCalendarStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _live = FromProfile(profile);
        }

        /// <inheritdoc />
        public bool PendingSave { get; private set; }

        /// <summary>Guardados pedidos (diagnóstico).</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public LoginCalendarRecord Load()
        {
            return _live;
        }

        /// <inheritdoc />
        public void Save(LoginCalendarRecord record)
        {
            _live = record ?? throw new ArgumentNullException(nameof(record));
            WriteTo(_profile, record);
            Persist();
        }

        /// <inheritdoc />
        public bool Flush()
        {
            if (PendingSave)
            {
                Persist();
            }

            return !PendingSave;
        }

        /// <summary>El registro de <paramref name="profile"/>: <c>calendar</c> (o uno nuevo si es null) y los marcos ganados.</summary>
        public static LoginCalendarRecord FromProfile(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var record = new LoginCalendarRecord();
            CalendarData? data = profile.Calendar;
            if (data != null)
            {
                record.Cycle = data.Cycle;
                record.NextDay = data.NextDay;
                record.LastClaimDate = data.LastClaimDate;
                record.PlayedDate = data.PlayedDate;
            }

            record.Frames.AddRange(profile.Entitlements.Frames);
            record.Sanitize();
            return record;
        }

        /// <summary>Escribe <paramref name="record"/> en <paramref name="profile"/>: <c>calendar</c> entero y los marcos nuevos.</summary>
        public static void WriteTo(Profile profile, LoginCalendarRecord record)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            profile.Calendar = new CalendarData
            {
                Cycle = record.Cycle,
                NextDay = record.NextDay,
                LastClaimDate = record.LastClaimDate,
                PlayedDate = record.PlayedDate,
            };
            for (int i = 0; i < record.Frames.Count; i++)
            {
                if (!profile.Entitlements.Frames.Contains(record.Frames[i]))
                {
                    profile.Entitlements.Frames.Add(record.Frames[i]);
                }
            }
        }

        private void Persist()
        {
            SaveCount++;
            PendingSave = !_store.TrySave(_profile);
        }
    }
}
