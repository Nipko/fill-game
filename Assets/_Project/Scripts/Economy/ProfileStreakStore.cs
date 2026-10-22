#nullable enable
using System;
using AF.Platform;
using AF.Save;

namespace AF.Economy
{
    /// <summary>
    /// <see cref="IStreakStore"/> del juego (parche S1-profile; 06 §4.3; 08 §4.3 con Q-M9 (a) [P] del plan de la tanda 5): la
    /// Crecida en el mismo <see cref="Profile"/> que la cartera, guardado con el mismo <see cref="IProfileStore"/> (en el juego, el
    /// lote <c>ProfileSaveBatch</c>, por su vía síncrona, como la cartera). <see cref="StreakRecord.Wins"/> y
    /// <see cref="StreakRecord.RiverFull"/> van al bloque <c>streak</c>; <see cref="StreakRecord.ShieldAvailable"/>,
    /// <see cref="StreakRecord.HitsToday"/> y <see cref="StreakRecord.SeedsToday"/>, a <c>daily</c> (<c>shield_available</c>,
    /// <c>streak_hits</c> y <c>streak_seeds</c>), que comparten la cartera, los anuncios y las compras. Así el apunte de un hito y
    /// su pago salen en la misma escritura del perfil y un hito nunca se paga dos veces el mismo día (<see cref="IStreakStore"/>).
    /// Mismo patrón que <c>AF.LiveOps.ProfileDailyChallengeStore</c>: <see cref="Save"/> no lanza por un fallo de escritura; el
    /// cambio queda en el perfil en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado del perfil o con
    /// <see cref="Flush"/>.
    /// <para>
    /// <see cref="Load"/> pasa antes <c>daily</c> al día local del reloj con <see cref="DailyCounters.RollOver"/> (el bloque
    /// entero, solo hacia delante; si cambia, <c>economy_ts</c> pasa a ahora), vuelve a leer el perfil en el estado vivo, lo recorta
    /// (<see cref="StreakRecord.Normalize"/>) y recuerda lo que entregó. <see cref="Save"/> escribe siempre la racha y el río
    /// crecido, y lleva los campos del día a <c>daily</c> con <see cref="StreakRecord.MergeDay"/>: si el registro es del día de
    /// <c>daily</c>, se copian; si es de un día posterior, <c>daily</c> pasa antes a ese día (entero); si es de un día anterior
    /// (cargado antes de medianoche y guardado después de que otro módulo pasara el día), se aplica a hoy solo lo que cambió desde
    /// esa carga: hitos nuevos, aumento de semillas y escudo gastado (regla conservadora: puede pagar de menos en el borde de
    /// medianoche, nunca dos veces). Si cambia algún campo de <c>daily</c>, <c>economy_ts</c> pasa a ahora (08 §4.3).
    /// </para>
    /// </summary>
    public sealed class ProfileStreakStore : IStreakStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private readonly IClock _clock;
        private StreakRecord _live = new StreakRecord();
        private StreakRecord _loaded;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileStreakStore(Profile profile, IProfileStore store, IClock clock)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            ReadInto(profile, _live);
            _loaded = _live.Clone();
        }

        /// <inheritdoc />
        public bool PendingSave { get; private set; }

        /// <summary>Guardados pedidos (diagnóstico).</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public StreakRecord Load()
        {
            if (DailyCounters.RollOver(_profile.Daily, _clock.LocalDateKey()))
            {
                _profile.EconomyTs = _clock.UtcNow;
            }

            ReadInto(_profile, _live);
            _loaded = _live.Clone();
            return _live;
        }

        /// <inheritdoc />
        public void Save(StreakRecord record)
        {
            _live = record ?? throw new ArgumentNullException(nameof(record));
            if (WriteTo(_profile, _loaded, record))
            {
                _profile.EconomyTs = _clock.UtcNow;
            }

            _loaded = record.Clone(); // otro Save del mismo registro no vuelve a sumar lo ya aplicado
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

        /// <summary>
        /// Copia en <paramref name="record"/> la Crecida de <paramref name="profile"/>: <c>streak</c> (0 y false si es null) y los
        /// campos del día de <c>daily</c>, con su <c>date_local</c>, recortados con <see cref="StreakRecord.Normalize"/> (un valor
        /// fuera de rango en el archivo no llega a las reglas). No cambia el perfil.
        /// </summary>
        public static void ReadInto(Profile profile, StreakRecord record)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            StreakData? streak = profile.Streak;
            record.Wins = streak == null ? 0 : streak.Wins;
            record.RiverFull = streak != null && streak.RiverFull;
            DailyData daily = profile.Daily;
            record.DateLocal = daily.DateLocal;
            record.ShieldAvailable = daily.ShieldAvailable;
            record.HitsToday.Clear();
            record.HitsToday.AddRange(daily.StreakHits);
            record.SeedsToday = daily.StreakSeeds;
            record.Normalize();
        }

        /// <summary>
        /// Escribe <paramref name="record"/>, que salió de <paramref name="loaded"/>, en <paramref name="profile"/> (con copias: el
        /// perfil no comparte listas con el registro). <c>streak</c> siempre; los campos del día, con
        /// <see cref="StreakRecord.MergeDay"/>, después de pasar <c>daily</c> entero al día del registro cuando es posterior
        /// (<see cref="DailyCounters.RollOver"/>). Devuelve si cambió algún campo de <c>daily</c> o de <c>streak</c> (B1a-Q2 (a),
        /// D-033: un cambio que solo toca <c>streak</c> también marca <c>economy_ts</c>; bloque B8).
        /// </summary>
        public static bool WriteTo(Profile profile, StreakRecord loaded, StreakRecord record)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (loaded == null)
            {
                throw new ArgumentNullException(nameof(loaded));
            }

            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            StreakData? before = profile.Streak;
            bool streakChanged = before == null ? record.Wins != 0 || record.RiverFull : before.Wins != record.Wins || before.RiverFull != record.RiverFull;
            profile.Streak = new StreakData { Wins = record.Wins, RiverFull = record.RiverFull };

            DailyData daily = profile.Daily;
            bool changed = (record.DateLocal != null && DailyCounters.RollOver(daily, record.DateLocal)) | streakChanged;
            var current = new StreakRecord
            {
                DateLocal = daily.DateLocal,
                ShieldAvailable = daily.ShieldAvailable,
                SeedsToday = daily.StreakSeeds,
            };
            current.HitsToday.AddRange(daily.StreakHits);
            if (StreakRecord.MergeDay(current, loaded, record))
            {
                daily.ShieldAvailable = current.ShieldAvailable;
                daily.StreakSeeds = current.SeedsToday;
                daily.StreakHits.Clear();
                daily.StreakHits.AddRange(current.HitsToday);
                changed = true;
            }

            return changed;
        }

        private void Persist()
        {
            SaveCount++;
            PendingSave = !_store.TrySave(_profile);
        }
    }
}
