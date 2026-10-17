#nullable enable
using System;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Progreso guardado en el perfil (GDD 08 §4.3 <c>progress.max_level</c>, <c>progress.next_level</c>, desde M3.1b,
    /// <c>progress.map_seen_level</c> y, desde TI6b, <c>stats.levels_won</c>; tarea B1-5): lee y escribe <see cref="Profile.Progress"/> y guarda el perfil entero con
    /// el <see cref="IProfileStore"/>, el mismo que usa <c>AF.Economy.Wallet</c> sobre el mismo <see cref="Profile"/> (en el
    /// juego, el lote <see cref="ProfileSaveBatch"/>). Si la escritura falla, el cambio sigue en memoria
    /// (<see cref="PendingSave"/>) y sale con el siguiente guardado del perfil. Es también la vista del Mapa
    /// (<see cref="IMapProgress"/>), que lee el perfil sin asignar memoria.
    /// </summary>
    public sealed class ProfileProgressStore : IProgressStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileProgressStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>El último cambio no llegó al disco (escritura fallida o almacén protegido).</summary>
        public bool PendingSave { get; private set; }

        /// <inheritdoc />
        public int MaxLevel => _profile.Progress.MaxLevel;

        /// <inheritdoc />
        public int NextLevel => _profile.Progress.NextLevel;

        /// <inheritdoc />
        public int MapSeenLevel => _profile.Progress.ClampedMapSeenLevel;

        /// <inheritdoc />
        public int LevelsWon => Math.Max(0, _profile.Stats.LevelsWon);

        /// <inheritdoc />
        public bool IsSkipped(int level)
        {
            return ProgressRules.Contains(_profile.Progress.SkippedLevels, level);
        }

        /// <inheritdoc />
        public int LoadNextLevel()
        {
            return _profile.Progress.NextLevel;
        }

        /// <inheritdoc />
        public int LoadMaxLevel()
        {
            return _profile.Progress.MaxLevel;
        }

        /// <inheritdoc />
        public void SaveNextLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "next_level empieza en 1.");
            }

            _profile.Progress.NextLevel = level;
            Persist();
        }

        /// <inheritdoc />
        public void SaveWin(int level, int nextLevel)
        {
            if (level < 1 || nextLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Los niveles empiezan en 1.");
            }

            ProgressData progress = _profile.Progress;
            progress.MaxLevel = Math.Max(progress.MaxLevel, level);
            progress.NextLevel = nextLevel;
            _profile.Stats.LevelsWon = ProgressRules.AddWin(_profile.Stats.LevelsWon); // stats.levels_won, en el mismo guardado
            Persist();
        }

        /// <inheritdoc />
        public int LoadMapSeenLevel()
        {
            return MapSeenLevel;
        }

        /// <inheritdoc />
        public void SaveMapSeenLevel(int level)
        {
            ProgressData progress = _profile.Progress;
            int current = progress.ClampedMapSeenLevel;
            int seen = ProgressRules.NextMapSeen(current, level, progress.MaxLevel);
            if (seen == current)
            {
                return; // sin cambios no se escribe (y un valor fuera de rango ya se recorta al guardar)
            }

            progress.MapSeenLevel = seen;
            Persist();
        }

        private void Persist()
        {
            PendingSave = !_store.TrySave(_profile);
        }
    }

    /// <summary>Origen del progreso del prototipo v0 (PlayerPrefs <c>af_v0_level</c>), que se migra una vez al perfil.</summary>
    public interface ILegacyProgressSource
    {
        /// <summary>Lee el siguiente nivel guardado por v0; false si no hay nada guardado.</summary>
        bool TryRead(out int nextLevel);

        /// <summary>Borra el valor de v0 (ya migrado o descartado).</summary>
        void Delete();
    }

    /// <summary>Resultado de <see cref="ProgressMigration.MigrateV0"/>.</summary>
    public enum LegacyProgressOutcome
    {
        /// <summary>No había progreso de v0.</summary>
        None = 0,

        /// <summary>El progreso de v0 pasó al perfil, se guardó y se borró la clave antigua.</summary>
        Migrated = 1,

        /// <summary>Pasó al perfil, pero el guardado falló: la clave antigua se conserva y se reintenta en el siguiente arranque.</summary>
        MigratedNotSaved = 2,

        /// <summary>Ya había perfil (o el valor no era válido): la clave antigua se borra sin tocar el perfil.</summary>
        Discarded = 3,
    }

    /// <summary>
    /// Migración única del progreso del prototipo v0 (PlayerPrefs <c>af_v0_level</c>, docs/dev/prototipo-v0.md c) al perfil
    /// (08 §4.3), tarea B1-5. Solo se aplica a un perfil recién creado: si ya había perfil, manda el perfil.
    /// </summary>
    public static class ProgressMigration
    {
        /// <summary>
        /// Si <paramref name="legacy"/> tiene progreso de v0 y el perfil es nuevo (<paramref name="profileIsNew"/>):
        /// <c>next_level</c> = el valor de v0 y <c>max_level</c> = ese valor − 1 (v0 guardaba el siguiente nivel al ganar);
        /// guarda el perfil y, si quedó guardado, borra la clave de v0. Con un perfil existente, o un valor &lt; 1, la clave se
        /// borra y el perfil no cambia.
        /// </summary>
        public static LegacyProgressOutcome MigrateV0(Profile profile, bool profileIsNew, IProfileStore store, ILegacyProgressSource legacy)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (legacy == null)
            {
                throw new ArgumentNullException(nameof(legacy));
            }

            if (!legacy.TryRead(out int nextLevel))
            {
                return LegacyProgressOutcome.None;
            }

            if (!profileIsNew || nextLevel < 1)
            {
                legacy.Delete();
                return LegacyProgressOutcome.Discarded;
            }

            ProgressData progress = profile.Progress;
            progress.NextLevel = nextLevel;
            progress.MaxLevel = Math.Max(progress.MaxLevel, nextLevel - 1);
            if (!store.TrySave(profile))
            {
                return LegacyProgressOutcome.MigratedNotSaved;
            }

            legacy.Delete();
            return LegacyProgressOutcome.Migrated;
        }
    }
}
