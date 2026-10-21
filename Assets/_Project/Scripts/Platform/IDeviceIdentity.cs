#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Identificador del dispositivo para la clave del HMAC del perfil (GDD 08 §4.3: «<c>hmac</c> con clave derivada del
    /// dispositivo disuade la edición manual del archivo»; 08 §10.1, fila «Edición del perfil local»; bloque B8). No es un
    /// secreto ni seguridad real (08 §10.1, «Ingeniería inversa del binario»: ninguna clave secreta en el cliente): solo hace que
    /// editar <c>profile.json</c> a mano deje un perfil que no cuadra. El perfil nunca guarda este valor en claro: guarda su huella
    /// (<c>integrity.device_first</c>, <c>AF.Save.ProfileHmac.DeviceFingerprint</c>). En el teléfono lo da <c>AF.UI</c> con
    /// <c>InstallIdentity</c> (un id aleatorio por instalación en <c>PlayerPrefs</c>; Q-B8-3); el editor y los tests usan <see cref="ManualDeviceIdentity"/>.
    /// </summary>
    public interface IDeviceIdentity
    {
        /// <summary>
        /// Identificador estable del dispositivo tal como lo da el sistema, o <c>""</c> si no lo da (entonces la huella es la de
        /// <c>""</c>: el perfil se firma igual). Nunca null.
        /// </summary>
        string DeviceId { get; }
    }

    /// <summary><see cref="IDeviceIdentity"/> fijo, para el editor y los tests.</summary>
    public sealed class ManualDeviceIdentity : IDeviceIdentity
    {
        /// <summary>Crea la identidad con <paramref name="deviceId"/> (null se toma como <c>""</c>).</summary>
        public ManualDeviceIdentity(string? deviceId)
        {
            DeviceId = deviceId ?? string.Empty;
        }

        /// <summary>Identidad de desarrollo del editor (sin red ni identificadores reales).</summary>
        public static ManualDeviceIdentity Editor { get; } = new ManualDeviceIdentity("af-editor-device");

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return "ManualDeviceIdentity(" + DeviceId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " car.)";
        }
    }
}
