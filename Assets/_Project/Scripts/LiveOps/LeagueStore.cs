#nullable enable
using System;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>
    /// Dónde vive el bloque <c>league</c> (08 §4.3 con D-046): en el juego, el perfil (<see cref="ProfileLeagueStore"/>); en los
    /// tests y el editor, <see cref="InMemoryLeagueStore"/>. Como <see cref="IDailyChallengeStore"/>: <see cref="Save"/> no lanza
    /// por un fallo de escritura; el cambio sigue en memoria y sale con el siguiente guardado.
    /// </summary>
    public interface ILeagueStore
    {
        /// <summary>El registro vivo (nunca null; la misma instancia mientras dure la sesión).</summary>
        LeagueRecord Load();

        /// <summary>Guarda <paramref name="record"/>.</summary>
        void Save(LeagueRecord record);

        /// <summary>Borra el bloque (borrado de cuenta): la cola y la caché se pierden.</summary>
        void Wipe();
    }

    /// <summary>Almacén en memoria (tests y editor).</summary>
    public sealed class InMemoryLeagueStore : ILeagueStore
    {
        private LeagueRecord _record = new LeagueRecord();

        /// <summary>Guardados pedidos.</summary>
        public int SaveCount { get; private set; }

        /// <summary>El JSON del último guardado (el mismo formato que el perfil), o null.</summary>
        public string? LastJson { get; private set; }

        public LeagueRecord Load() => _record;

        public void Save(LeagueRecord record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            SaveCount++;
            LastJson = LeagueRecordJson.Write(record);
        }

        public void Wipe()
        {
            _record = new LeagueRecord();
            LastJson = null;
        }
    }

    /// <summary>
    /// <see cref="ILeagueStore"/> del juego: el bloque <c>league</c> del perfil (<see cref="Profile.League"/>, que AF.Save guarda
    /// tal cual y nunca sube a la nube: es del dispositivo), guardado con el lote del perfil.
    /// </summary>
    public sealed class ProfileLeagueStore : ILeagueStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private LeagueRecord _live;

        public ProfileLeagueStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _live = LeagueRecordJson.Read(profile.League);
        }

        /// <summary>El último guardado no llegó al disco.</summary>
        public bool PendingSave { get; private set; }

        public LeagueRecord Load() => _live;

        public void Save(LeagueRecord record)
        {
            _live = record ?? throw new ArgumentNullException(nameof(record));
            _profile.League = record.IsEmpty ? null : LeagueRecordJson.Write(record);
            PendingSave = !_store.TrySave(_profile);
        }

        public void Wipe()
        {
            _live = new LeagueRecord();
            _profile.League = null; // sin guardar: el borrado de cuenta ya borró el perfil del disco
        }
    }
}
