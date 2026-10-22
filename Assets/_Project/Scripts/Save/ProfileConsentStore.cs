#nullable enable
using System;
using AF.Platform;

namespace AF.Save
{
    /// <summary>
    /// <see cref="IConsentStore"/> sobre <c>profile.consent</c> (08 §4.3; bloque B12): lee <see cref="Profile.Consent"/> y, al guardar,
    /// lo escribe en el acto por <paramref name="store"/> (en el juego, el almacén durable del lote: la prueba del consentimiento no
    /// espera a otra escritura). Con el perfil en solo lectura (08 §4.4) la escritura falla en silencio y la decisión vale solo para
    /// esta sesión: se volverá a preguntar en el siguiente arranque.
    /// </summary>
    public sealed class ProfileConsentStore : IConsentStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda con <paramref name="store"/>.</summary>
        public ProfileConsentStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <inheritdoc />
        public ConsentRecord? Load() => _profile.Consent;

        /// <inheritdoc />
        public void Save(ConsentRecord record)
        {
            _profile.Consent = record ?? throw new ArgumentNullException(nameof(record));
            _store.TrySave(_profile);
        }
    }
}
