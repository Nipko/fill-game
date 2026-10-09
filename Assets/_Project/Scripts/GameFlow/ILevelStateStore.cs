#nullable enable
using System;
using System.Diagnostics;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Dónde se guarda el nivel en curso (<c>level_state</c> de 04 §10.1 y 08 §4.3; 02 R8 y §14; tarea B2-5): en el juego,
    /// <see cref="ProfileLevelStateStore"/> sobre el perfil de <c>AF.Save</c>; en los tests, <see cref="MemoryLevelStateStore"/>.
    /// </summary>
    public interface ILevelStateStore
    {
        /// <summary>Estado guardado, o null si no hay nivel en curso.</summary>
        LevelStateData? Load();

        /// <summary>Guarda <paramref name="state"/> y lo deja persistido (sobrevive a un cierre o un <c>force-stop</c>).</summary>
        void Save(LevelStateData state);

        /// <summary>Ya no hay nivel en curso (victoria): <c>level_state</c> = null, persistido.</summary>
        void Clear();
    }

    /// <summary>
    /// <c>level_state</c> en el perfil (08 §4.3): escribe <see cref="Profile.LevelState"/> y guarda el perfil entero con el
    /// mismo <see cref="IProfileStore"/> que la cartera y el progreso. Si la escritura falla, el cambio sigue en memoria
    /// (<see cref="PendingSave"/>) y sale con el siguiente guardado del perfil (02 §14: lo peor es reabrir un vertido antes).
    /// </summary>
    public sealed class ProfileLevelStateStore : ILevelStateStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileLevelStateStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>El último cambio no llegó al disco.</summary>
        public bool PendingSave { get; private set; }

        /// <summary>Guardados hechos (diagnóstico del coste por vertido).</summary>
        public int SaveCount { get; private set; }

        /// <summary>Lo que tardó el último guardado, en ms (plan B2-5: &lt; 5 ms por vertido en el Editor [P]).</summary>
        public double LastSaveMs { get; private set; }

        /// <inheritdoc />
        public LevelStateData? Load()
        {
            return _profile.LevelState;
        }

        /// <inheritdoc />
        public void Save(LevelStateData state)
        {
            _profile.LevelState = state ?? throw new ArgumentNullException(nameof(state));
            Persist();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _profile.LevelState = null;
            Persist();
        }

        /// <summary>Reintenta un guardado que falló (al pasar a segundo plano). Devuelve si no queda nada por guardar.</summary>
        public bool Flush()
        {
            if (PendingSave)
            {
                Persist();
            }

            return !PendingSave;
        }

        private void Persist()
        {
            long start = Stopwatch.GetTimestamp();
            SaveCount++;
            PendingSave = !_store.TrySave(_profile);
            LastSaveMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
    }

    /// <summary>Nivel en curso en memoria (tests): guarda una copia, como el disco.</summary>
    public sealed class MemoryLevelStateStore : ILevelStateStore
    {
        private LevelStateData? _saved;

        /// <summary>Veces que se ha guardado o borrado.</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public LevelStateData? Load()
        {
            return _saved == null ? null : LevelStateSnapshot.Copy(_saved);
        }

        /// <inheritdoc />
        public void Save(LevelStateData state)
        {
            _saved = LevelStateSnapshot.Copy(state ?? throw new ArgumentNullException(nameof(state)));
            SaveCount++;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _saved = null;
            SaveCount++;
        }
    }
}
