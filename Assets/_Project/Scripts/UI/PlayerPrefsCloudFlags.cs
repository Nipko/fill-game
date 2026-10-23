#nullable enable
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// <see cref="ICloudSaveFlags"/> en <c>PlayerPrefs</c>, en el teléfono (D-042; <c>docs/dev/play-games.md</c> §5): dos enteros,
    /// <c>af_cloud_delete_pending</c> y <c>af_cloud_resync_pending</c>, sin identificadores. «Eliminar cuenta y datos» borra
    /// <c>PlayerPrefs</c> y luego vuelve a poner la marca de borrado pendiente (<c>CloudSession.EraseForAccountDelete</c>).
    /// </summary>
    public sealed class PlayerPrefsCloudFlags : ICloudSaveFlags
    {
        /// <summary>Clave de la marca de borrado pendiente.</summary>
        public const string DeletePendingKey = "af_cloud_delete_pending";

        /// <summary>Clave de la marca de subida pendiente (#3379).</summary>
        public const string ResyncPendingKey = "af_cloud_resync_pending";

        private PlayerPrefsCloudFlags()
        {
        }

        /// <summary>La única instancia.</summary>
        public static PlayerPrefsCloudFlags Instance { get; } = new PlayerPrefsCloudFlags();

        /// <inheritdoc />
        public bool DeletePending
        {
            get => PlayerPrefs.GetInt(DeletePendingKey, 0) != 0;
            set => Write(DeletePendingKey, value);
        }

        /// <inheritdoc />
        public bool ResyncPending
        {
            get => PlayerPrefs.GetInt(ResyncPendingKey, 0) != 0;
            set => Write(ResyncPendingKey, value);
        }

        private static void Write(string key, bool value)
        {
            if (value)
            {
                PlayerPrefs.SetInt(key, 1);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }
    }
}
