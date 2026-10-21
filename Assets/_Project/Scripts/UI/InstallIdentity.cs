#nullable enable
using System;
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// <see cref="IDeviceIdentity"/> del teléfono (bloque B8; 08 §4.3, «clave derivada del dispositivo»; Q-B8-3 (b) [P]): un id
    /// aleatorio por instalación, guardado en <c>PlayerPrefs</c> (<see cref="Key"/>), nunca un identificador del dispositivo
    /// (<c>SystemInfo.deviceUniqueIdentifier</c> sale del ANDROID_ID y exigiría su fila de Data Safety, 08 §13.3, sin dar más
    /// protección: la clave del HMAC sale de la huella que va en el propio perfil). «Eliminar cuenta y datos» lo borra con
    /// <c>PlayerPrefs</c>. Sale del teléfono solo su huella con sal (<c>integrity.device_first</c>).
    /// </summary>
    internal sealed class InstallIdentity : IDeviceIdentity
    {
        /// <summary>Clave de <c>PlayerPrefs</c> (un flag de arranque más, 08 §11: «PlayerPrefs solo para flags de arranque»).</summary>
        public const string Key = "af_install_id";

        /// <summary>Instancia compartida.</summary>
        public static readonly InstallIdentity Instance = new InstallIdentity();

        /// <inheritdoc />
        public string DeviceId
        {
            get
            {
                string id = PlayerPrefs.GetString(Key, string.Empty);
                if (id.Length == 0)
                {
                    id = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(Key, id);
                    PlayerPrefs.Save();
                }

                return id;
            }
        }
    }
}
