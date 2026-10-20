#nullable enable
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// <see cref="IDeviceIdentity"/> del teléfono (bloque B8; 08 §4.3, «clave derivada del dispositivo»): el
    /// <c>SystemInfo.deviceUniqueIdentifier</c> de Unity (en Android, sin permisos). Solo sale del dispositivo su huella con sal
    /// (<c>integrity.device_first</c>), nunca el id en claro (pregunta Q-B8-3 de la nota, sobre Data Safety). Sin id (plataforma
    /// que no lo da), <c>""</c>.
    /// </summary>
    internal sealed class UnityDeviceIdentity : IDeviceIdentity
    {
        /// <summary>Instancia compartida (lee el id en cada consulta; Unity lo cachea).</summary>
        public static readonly UnityDeviceIdentity Instance = new UnityDeviceIdentity();

        /// <inheritdoc />
        public string DeviceId
        {
            get
            {
                string? id = SystemInfo.deviceUniqueIdentifier;
                return id == null || id == SystemInfo.unsupportedIdentifier ? string.Empty : id;
            }
        }
    }
}
