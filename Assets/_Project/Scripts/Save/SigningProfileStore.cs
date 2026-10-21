#nullable enable
using System;
using AF.Platform;

namespace AF.Save
{
    /// <summary>
    /// Almacén que firma el perfil antes de cada escritura (bloque B8; 08 §4.3, <c>integrity.hmac</c>; <see cref="ProfileIntegrity"/>)
    /// y comprueba la firma al leer (<see cref="LastIntegrity"/>). Envuelve el almacén de verdad sin cambiarlo: <see cref="Wrap"/>
    /// devuelve un <see cref="IProfileByteStore"/> si el de dentro lo es (así <c>ProfileSaveBatch</c> sigue escribiendo en segundo
    /// plano: la firma va en <see cref="IProfileByteStore.Serialize"/>, en el hilo principal, y el escritor solo ve bytes). Firmar
    /// cambia <c>integrity</c> del perfil en memoria (el mismo objeto que guarda el juego), y nada más.
    /// </summary>
    public class SigningProfileStore : IProfileStore
    {
        private readonly IProfileStore _inner;
        private readonly IDeviceIdentity _device;

        /// <summary>Envuelve <paramref name="inner"/> con la identidad <paramref name="device"/>.</summary>
        protected SigningProfileStore(IProfileStore inner, IDeviceIdentity device)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>El almacén de dentro.</summary>
        public IProfileStore Inner => _inner;

        /// <summary>Firmas hechas (diagnóstico y tests).</summary>
        public int SignCount { get; private set; }

        /// <summary>La firma del último perfil leído con <see cref="Load"/>, o null si no se leyó ninguno.</summary>
        public IntegrityStatus? LastIntegrity { get; private set; }

        /// <summary>
        /// Envuelve <paramref name="inner"/>: un <see cref="IProfileByteStore"/> si <paramref name="inner"/> lo es, y si no, un
        /// <see cref="IProfileStore"/>.
        /// </summary>
        public static SigningProfileStore Wrap(IProfileStore inner, IDeviceIdentity device)
        {
            return inner is IProfileByteStore bytes ? new SigningProfileByteStore(bytes, device) : new SigningProfileStore(inner, device);
        }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            ProfileLoadResult result = _inner.Load();
            LastIntegrity = result.Profile == null ? (IntegrityStatus?)null : ProfileIntegrity.Verify(result.Profile);
            return result;
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            Sign(profile);
            _inner.Save(profile);
        }

        /// <inheritdoc />
        public void Delete()
        {
            _inner.Delete();
            LastIntegrity = null;
        }

        /// <summary>Firma <paramref name="profile"/> con la identidad de este almacén.</summary>
        protected void Sign(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            ProfileIntegrity.Sign(profile, _device);
            SignCount++;
        }

        private sealed class SigningProfileByteStore : SigningProfileStore, IProfileByteStore
        {
            private readonly IProfileByteStore _bytes;

            public SigningProfileByteStore(IProfileByteStore inner, IDeviceIdentity device)
                : base(inner, device)
            {
                _bytes = inner;
            }

            public byte[] Serialize(Profile profile)
            {
                Sign(profile);
                return _bytes.Serialize(profile);
            }

            public void WriteBytes(byte[] bytes)
            {
                _bytes.WriteBytes(bytes);
            }
        }
    }
}
