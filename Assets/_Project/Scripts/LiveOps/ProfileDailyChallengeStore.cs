#nullable enable
using System;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>
    /// <see cref="IDailyChallengeStore"/> del juego (enganche E4; 08 §4.3 con R-Q4, R-Q5 y R-Q16; nota del Reto §10): el bloque
    /// <c>daily_challenge</c> del mismo <see cref="Profile"/> que la cartera, guardado con el mismo <see cref="IProfileStore"/>
    /// (en el juego, el lote <c>ProfileSaveBatch</c>, por su vía síncrona, como la cartera). Así el registro de un cobro y su
    /// pago salen en la misma escritura del perfil y nunca se cobra dos veces la misma fecha (<see cref="IDailyChallengeStore"/>).
    /// Mismo patrón que <c>AF.GameFlow.ProfileLevelStateStore</c>: <see cref="Save"/> no lanza por un fallo de escritura; el
    /// cambio queda en el perfil en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado del perfil o con
    /// <see cref="Flush"/>.
    /// <para>
    /// <see cref="Load"/> devuelve el registro vivo, leído del perfil al crear el almacén; <see cref="Save"/> lo copia al perfil
    /// (<see cref="ToData"/>) y lo guarda. Lo que se cambie en el registro sin <see cref="Save"/> no llega al perfil.
    /// </para>
    /// </summary>
    public sealed class ProfileDailyChallengeStore : IDailyChallengeStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private DailyChallengeRecord _live;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileDailyChallengeStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _live = FromData(profile.DailyChallenge);
        }

        /// <inheritdoc />
        public bool PendingSave { get; private set; }

        /// <summary>Guardados pedidos (diagnóstico).</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public DailyChallengeRecord Load()
        {
            return _live;
        }

        /// <inheritdoc />
        public void Save(DailyChallengeRecord record)
        {
            _live = record ?? throw new ArgumentNullException(nameof(record));
            _profile.DailyChallenge = ToData(record);
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

        /// <summary>Registro del Reto a partir del bloque del perfil (un registro vacío si es null).</summary>
        public static DailyChallengeRecord FromData(DailyChallengeData? data)
        {
            var record = new DailyChallengeRecord();
            if (data == null)
            {
                return record;
            }

            record.LastDate = data.LastDate;
            record.Result = ParseResult(data.Result);
            record.StreakDays = data.StreakDays;
            record.LastClaimedDate = data.LastClaimedDate;
            for (int i = 0; i < data.Pending.Count; i++)
            {
                record.PendingClaims.Add(new DailyClaimEntry(data.Pending[i].Date, data.Pending[i].Streak));
            }

            for (int i = 0; i < data.OfflineClaims.Count; i++)
            {
                record.OfflineClaims.Add(new DailyClaimEntry(data.OfflineClaims[i].Date, data.OfflineClaims[i].Streak));
            }

            record.StampsMonth = data.StampsMonth;
            record.StampDays.AddRange(data.StampDays);
            if (data.InProgress != null)
            {
                record.InProgress = new DailyPlayInProgress
                {
                    DateKey = data.InProgress.Date,
                    Band = data.InProgress.Band,
                    BoardId = data.InProgress.Id,
                    State = DailyPlayInProgress.CopyState(data.InProgress.LevelState),
                };
            }

            return record;
        }

        /// <summary>Bloque del perfil con una copia independiente de <paramref name="record"/>.</summary>
        public static DailyChallengeData ToData(DailyChallengeRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var data = new DailyChallengeData
            {
                LastDate = record.LastDate,
                Result = record.Result == DailyResult.None ? null : record.Result.ToId(),
                StreakDays = record.StreakDays,
                LastClaimedDate = record.LastClaimedDate,
                StampsMonth = record.StampsMonth,
            };
            for (int i = 0; i < record.PendingClaims.Count; i++)
            {
                data.Pending.Add(new DailyClaimData { Date = record.PendingClaims[i].DateKey, Streak = record.PendingClaims[i].StreakDays });
            }

            for (int i = 0; i < record.OfflineClaims.Count; i++)
            {
                data.OfflineClaims.Add(new DailyClaimData { Date = record.OfflineClaims[i].DateKey, Streak = record.OfflineClaims[i].StreakDays });
            }

            data.StampDays.AddRange(record.StampDays);
            if (record.InProgress != null)
            {
                data.InProgress = new DailyInProgressData
                {
                    Date = record.InProgress.DateKey,
                    Band = record.InProgress.Band,
                    Id = record.InProgress.BoardId,
                    LevelState = DailyPlayInProgress.CopyState(record.InProgress.State),
                };
            }

            return data;
        }

        /// <summary><c>win</c>, <c>fail</c> o <c>abandon</c>; cualquier otro valor (o null) es <see cref="DailyResult.None"/>.</summary>
        public static DailyResult ParseResult(string? id)
        {
            switch (id)
            {
                case "win": return DailyResult.Win;
                case "fail": return DailyResult.Fail;
                case "abandon": return DailyResult.Abandon;
                default: return DailyResult.None;
            }
        }

        private void Persist()
        {
            SaveCount++;
            PendingSave = !_store.TrySave(_profile);
        }
    }
}
