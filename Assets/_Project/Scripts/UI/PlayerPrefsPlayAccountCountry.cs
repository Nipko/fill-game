#nullable enable
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El país de la cuenta de Google Play guardado en el dispositivo (D-057, P4 (a); <c>docs/dev/consentimiento.md</c>). Play Billing
    /// se conecta después de la pantalla de edad, pero el país legal se decide antes (<see cref="ConsentCountry.Resolve(ISimCountry, IPlayAccountCountry, IDeviceLocale, out CountrySource)"/>):
    /// por eso el país que da <c>getBillingConfigAsync</c> se guarda aquí y vale desde el arranque siguiente. En el teléfono, en
    /// <c>PlayerPrefs</c> (un flag de arranque, 08 §11: lo borra «Eliminar cuenta y datos» y vuelve con la próxima conexión con Play);
    /// en el Editor y en los tests, en memoria. Es un país (dos letras), no un identificador. No va a la nube, pero el país legal que
    /// resulta (SIM → Play → región) sí va como <c>region</c> de la Liga y, agregado, en <c>geo_tier</c> (D-057, decisiones de la
    /// revisión, punto 1).
    /// </summary>
    internal sealed class PlayerPrefsPlayAccountCountry : IPlayAccountCountryStore
    {
        /// <summary>Clave de <c>PlayerPrefs</c>.</summary>
        public const string Key = "af_play_account_country";

        private PlayerPrefsPlayAccountCountry()
        {
        }

        /// <summary>
        /// El almacén de esta plataforma: <c>PlayerPrefs</c> en Android y en el iPhone; en memoria en el resto (el Editor). En el
        /// iPhone guarda al crearse el país de la cuenta del App Store (<see cref="IosStorefrontCountry"/>; fase M2 de
        /// <c>mac-ios.md</c>), que allí ocupa el sitio del de Google Play.
        /// </summary>
        public static IPlayAccountCountryStore ForPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return new PlayerPrefsPlayAccountCountry();
                case RuntimePlatform.IPhonePlayer:
                    var store = new PlayerPrefsPlayAccountCountry();
                    IosStorefrontCountry.SaveInto(store);
                    return store;
                default:
                    return new MemoryPlayAccountCountry();
            }
        }

        /// <inheritdoc />
        public string CountryIso => ConsentCountry.Normalize(PlayerPrefs.GetString(Key, string.Empty)) ?? string.Empty;

        /// <inheritdoc />
        public void Save(string countryIso)
        {
            string? code = ConsentCountry.Normalize(countryIso);
            if (code == null || code == CountryIso)
            {
                return;
            }

            PlayerPrefs.SetString(Key, code);
            PlayerPrefs.Save();
        }
    }
}
