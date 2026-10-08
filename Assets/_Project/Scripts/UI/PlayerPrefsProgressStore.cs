#nullable enable
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Progreso del prototipo v0 en PlayerPrefs, clave <c>af_v0_level</c> (docs/dev/prototipo-v0.md c). Desde la tarea B1-5
    /// el progreso vive en el perfil (<see cref="ProfileProgressStore"/>, 08 §4.3): esta clase solo lee el valor antiguo
    /// para migrarlo una vez (<see cref="ProgressMigration.MigrateV0"/>) y lo borra después.
    /// </summary>
    public sealed class PlayerPrefsProgressStore : ILegacyProgressSource
    {
        /// <summary>Clave de PlayerPrefs.</summary>
        public const string Key = "af_v0_level";

        /// <inheritdoc />
        public bool TryRead(out int nextLevel)
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                nextLevel = 0;
                return false;
            }

            nextLevel = PlayerPrefs.GetInt(Key, 0);
            return true;
        }

        /// <inheritdoc />
        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
