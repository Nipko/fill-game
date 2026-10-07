#nullable enable
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Progreso provisional del prototipo v0 en PlayerPrefs, clave <c>af_v0_level</c> (docs/dev/prototipo-v0.md c;
    /// pregunta 4 al dueño). Se persiste en el acto (<see cref="PlayerPrefs.Save"/>) para sobrevivir a un
    /// <c>force-stop</c>. El guardado real es <c>AF.Save</c> (GDD 08 §4.3, §11).
    /// </summary>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        /// <summary>Clave de PlayerPrefs.</summary>
        public const string Key = "af_v0_level";

        /// <inheritdoc />
        public int LoadNextLevel()
        {
            return PlayerPrefs.GetInt(Key, 0);
        }

        /// <inheritdoc />
        public void SaveNextLevel(int level)
        {
            PlayerPrefs.SetInt(Key, level);
            PlayerPrefs.Save();
        }
    }
}
