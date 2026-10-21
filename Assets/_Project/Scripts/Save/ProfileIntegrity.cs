#nullable enable
using System;
using AF.Platform;

namespace AF.Save
{
    /// <summary>Lo que dice la firma de un perfil (<see cref="ProfileIntegrity.Verify"/>).</summary>
    public enum IntegrityStatus
    {
        /// <summary>Cuadra: <c>integrity.hmac</c> es el HMAC del perfil con la clave de su <c>device_first</c>.</summary>
        Valid = 0,

        /// <summary>Sin firma (perfil anterior a B8, o <c>hmac</c> o <c>device_first</c> ausentes o mal formados).</summary>
        Unsigned = 1,

        /// <summary>No cuadra: el archivo se cambió fuera del juego (o lo firmó un algoritmo distinto).</summary>
        Mismatch = 2,
    }

    /// <summary>
    /// Firma del perfil (GDD 08 §4.3, <c>integrity</c>; 08 §10.1, fila «Edición del perfil local»: «HMAC con clave derivada del
    /// dispositivo + <c>economy_ts</c>; en conflicto con la nube gana el blob coherente»; bloque B8). Lo firmado es el JSON del
    /// perfil (<see cref="ProfileJson.Serialize"/>, determinista) con <c>integrity.hmac</c> = null; la clave sale de
    /// <c>integrity.device_first</c> (<see cref="ProfileHmac"/>). Disuade la edición a mano; no es seguridad real (08 §10.1) y no
    /// castiga: un perfil que no cuadra se sigue jugando, solo pierde los conflictos con la nube frente a uno que cuadra
    /// (<see cref="CloudMerge"/>) y se mide (sonda <c>profile_integrity</c>; <c>payer_class = suspect</c> llega con la analítica).
    /// </summary>
    public static class ProfileIntegrity
    {
        /// <summary>
        /// El texto que se firma: el perfil sin su <c>hmac</c> y sin <c>purchase_queue</c> (F3-3: es solo del dispositivo y no sube
        /// a la nube, <see cref="CloudMerge.SharedJson"/>; así el blob de la nube se puede comprobar). No cambia
        /// <paramref name="profile"/>.
        /// </summary>
        public static string Payload(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            string? hmac = profile.Integrity.Hmac;
            profile.Integrity.Hmac = null;
            try
            {
                return CloudMerge.SharedJson(profile);
            }
            finally
            {
                profile.Integrity.Hmac = hmac;
            }
        }

        /// <summary>
        /// Firma <paramref name="profile"/>: si aún no tiene <c>device_first</c> (o no es una huella), pone la de
        /// <paramref name="device"/>; luego escribe <c>integrity.hmac</c>. Un perfil que viene de otro teléfono conserva su
        /// <c>device_first</c> (la huella del que lo creó) [P].
        /// </summary>
        public static void Sign(Profile profile, IDeviceIdentity device)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (!ProfileHmac.IsFingerprint(profile.Integrity.DeviceFirst))
            {
                profile.Integrity.DeviceFirst = ProfileHmac.DeviceFingerprint(device.DeviceId);
            }

            profile.Integrity.Hmac = ProfileHmac.Sign(Payload(profile), ProfileHmac.DeriveKey(profile.Integrity.DeviceFirst!));
        }

        /// <summary>Comprueba la firma de <paramref name="profile"/> sin cambiarlo.</summary>
        public static IntegrityStatus Verify(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            string? hmac = profile.Integrity.Hmac;
            string? deviceFirst = profile.Integrity.DeviceFirst;
            if (!ProfileHmac.IsSignature(hmac) || !ProfileHmac.IsFingerprint(deviceFirst))
            {
                return IntegrityStatus.Unsigned;
            }

            string expected = ProfileHmac.Sign(Payload(profile), ProfileHmac.DeriveKey(deviceFirst!));
            return ProfileHmac.Matches(expected, hmac) ? IntegrityStatus.Valid : IntegrityStatus.Mismatch;
        }

        /// <summary>
        /// Coherente para la fusión (08 §10.1): todo salvo una firma que no cuadra (<see cref="IntegrityStatus.Mismatch"/>). Un perfil
        /// sin firma o migrado (que se comprueba como <see cref="IntegrityStatus.Unsigned"/>: la migración cambia lo firmado) no entra
        /// en el caso de integridad y sigue las reglas de siempre [P] (Q-B8-5).
        /// </summary>
        public static bool IsCoherent(IntegrityStatus status)
        {
            return status != IntegrityStatus.Mismatch;
        }

        /// <summary>Texto de la sonda: <c>valid</c>, <c>unsigned</c> o <c>mismatch</c>.</summary>
        public static string ToId(IntegrityStatus status)
        {
            switch (status)
            {
                case IntegrityStatus.Valid: return "valid";
                case IntegrityStatus.Unsigned: return "unsigned";
                case IntegrityStatus.Mismatch: return "mismatch";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
