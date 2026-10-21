#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

namespace AF.Save
{
    /// <summary>
    /// Las cuentas del HMAC del perfil (GDD 08 §4.3, <c>integrity</c>: «<c>hmac</c> con clave derivada del dispositivo disuade la
    /// edición manual del archivo (no es seguridad real)»; 08 §10.1, fila «Edición del perfil local»; bloque B8), con
    /// <c>System.Security.Cryptography</c> de netstandard2.1 (sin paquetes). Sin secretos: las sales son constantes públicas de
    /// este archivo y la clave se deriva de la huella del dispositivo que creó el perfil (<c>integrity.device_first</c>), así que
    /// cualquier dispositivo puede comprobar un blob que viene de la nube [P] (nota <c>docs/dev/nube-integridad.md</c>, Q-B8-2).
    /// <list type="bullet">
    /// <item><see cref="DeviceFingerprint"/>: SHA-256 de la sal de huella + el id del dispositivo, en 32 hexadecimales: el perfil
    /// nunca guarda el id en claro (08 §8.1: sin identificadores en los datos).</item>
    /// <item><see cref="DeriveKey"/>: SHA-256 de la sal de clave + la huella (32 bytes).</item>
    /// <item><see cref="Sign"/>: HMAC-SHA-256 del texto en UTF-8, en 64 hexadecimales en minúscula.</item>
    /// <item><see cref="Matches"/>: comparación en tiempo constante.</item>
    /// </list>
    /// Qué texto se firma (el perfil sin su propio <c>hmac</c>) lo decide <c>ProfileIntegrity</c>.
    /// </summary>
    public static class ProfileHmac
    {
        /// <summary>Versión del esquema de firma; si cambia el algoritmo, cambia la sal y todos los perfiles se vuelven a firmar al guardar.</summary>
        public const string Version = "v1";

        /// <summary>Largo de <see cref="DeviceFingerprint"/> (hexadecimales).</summary>
        public const int FingerprintLength = 32;

        /// <summary>Largo de <see cref="Sign"/> (hexadecimales).</summary>
        public const int SignatureLength = 64;

        private const string FingerprintSalt = "amazonfill/device-first/" + Version + "|";
        private const string KeySalt = "amazonfill/profile-hmac/" + Version + "|";

        /// <summary>Huella del dispositivo para <c>integrity.device_first</c>: 32 hexadecimales en minúscula.</summary>
        public static string DeviceFingerprint(string deviceId)
        {
            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(FingerprintSalt + deviceId));
                return Hex(hash, FingerprintLength / 2);
            }
        }

        /// <summary>true si <paramref name="text"/> tiene la forma de una huella: 32 hexadecimales en minúscula.</summary>
        public static bool IsFingerprint(string? text)
        {
            return IsHex(text, FingerprintLength);
        }

        /// <summary>true si <paramref name="text"/> tiene la forma de una firma: 64 hexadecimales en minúscula.</summary>
        public static bool IsSignature(string? text)
        {
            return IsHex(text, SignatureLength);
        }

        /// <summary>Clave del HMAC (32 bytes) derivada de la huella <paramref name="deviceFirst"/>.</summary>
        public static byte[] DeriveKey(string deviceFirst)
        {
            if (deviceFirst == null)
            {
                throw new ArgumentNullException(nameof(deviceFirst));
            }

            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(KeySalt + deviceFirst));
            }
        }

        /// <summary>HMAC-SHA-256 de <paramref name="text"/> (UTF-8) con <paramref name="key"/>, en 64 hexadecimales en minúscula.</summary>
        public static string Sign(string text, byte[] key)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var hmac = new HMACSHA256(key))
            {
                return Hex(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)), SignatureLength / 2);
            }
        }

        /// <summary>
        /// <paramref name="expected"/> y <paramref name="actual"/> son la misma firma, comparadas en tiempo constante (null o de
        /// largo distinto: false).
        /// </summary>
        public static bool Matches(string? expected, string? actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                diff |= expected[i] ^ actual[i];
            }

            return diff == 0;
        }

        private static string Hex(byte[] bytes, int count)
        {
            const string digits = "0123456789abcdef";
            var chars = new char[count * 2];
            for (int i = 0; i < count; i++)
            {
                chars[2 * i] = digits[bytes[i] >> 4];
                chars[(2 * i) + 1] = digits[bytes[i] & 0xF];
            }

            return new string(chars);
        }

        private static bool IsHex(string? text, int length)
        {
            if (text == null || text.Length != length)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
