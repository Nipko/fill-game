#nullable enable
using System;
using AF.Ads;
using AF.Ads.Google;
using AF.Backend.Firebase;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El proveedor de anuncios de este build (D-049; nota <c>docs/dev/anuncios-admob.md</c>): AdMob directo con Google Mobile Ads en
    /// un teléfono Android (<see cref="GoogleAdProvider"/>, con los bloques de prueba de Google en desarrollo y con
    /// <c>com.amazonfill.dev</c>); en el editor y los tests, los anuncios simulados de siempre (<see cref="FakeAdService"/>). AppLovin
    /// MAX (D-042) queda instalado sin servir. Va siempre envuelto por <c>WrapConsentAds</c>: Google no se toca hasta que hay una
    /// decisión de consentimiento y edad, y con &lt;13 nunca (el arranque se para antes, en <c>BlockedAtBoot</c>). Los tests que fijan
    /// un presentador (<see cref="LaunchOptions.AdPresenterOverride"/>) siempre usan el simulado, y un build de desarrollo lanzado con
    /// el extra <c>af_ads_fill</c> también (las pruebas automáticas en el teléfono).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Tests: el proveedor de anuncios (p. ej. <see cref="GoogleAdService"/> sobre <see cref="FakeGmaPort"/>); gana a todo.</summary>
        internal static Func<Profile, IClock, IAdService>? AdProviderOverride { get; set; }

        // Google (o MAX): los avisos del SDK y los márgenes de cierre (recompensa tardía y vuelta de segundo plano); null con el simulado.
        private IAdTicker? _adTicker;

        /// <summary>Cada fotograma: los avisos del SDK y los márgenes de cierre (sin asignaciones mientras no toca).</summary>
        private void TickAds()
        {
            _adTicker?.Tick();
        }

        /// <summary>La app vuelve de segundo plano: se suelta un anuncio que se quedó sin su cierre (y se reintenta UMP si no resolvió).</summary>
        private void ResumeAds()
        {
            _adTicker?.OnResume();
        }

        private IAdService CreateAdProvider(ISimulatedAdPresenter presenter, Profile profile, IClock clock)
        {
            if (AdProviderOverride != null)
            {
                IAdService overridden = AdProviderOverride(profile, clock);
                _adTicker = overridden as IAdTicker;
                return overridden;
            }

            bool simulatedOnDevice = Debug.isDebugBuild && LaunchOptions.ReadIntentString(LaunchOptions.IntentExtraAdsFill) != null;
            if (LaunchOptions.AdPresenterOverride == null && !simulatedOnDevice)
            {
                IAdService? real = GoogleAdProvider.Create(
                    clock,
                    () => FirebaseAuthUid.Current, // SSV (08 §10.2): user_id = UID; sin Firebase, null y no va user_id
                    () => profile.Progress.NextLevel,
                    () => _rcConfigs?.Ads ?? AdsConfig.Defaults);
#if !UNITY_EDITOR
                // google (bloques de prueba o de produccion) o editor; MAX no se usa (D-049). Sin ID ni bloques.
                Debug.Log("[AF.Ads] proveedor: " + GoogleAdProvider.LastReason
                    + (GoogleAdProvider.LastReason == GoogleAdProvider.ReasonGoogle ? (GoogleAdProvider.LastProductionUnits ? " (bloques de produccion)" : " (bloques de prueba)") : string.Empty)
                    + "; max: not_configured");
#endif
                if (real != null)
                {
                    _adTicker = real as IAdTicker;
                    return real;
                }
            }
#if !UNITY_EDITOR
            else if (simulatedOnDevice)
            {
                Debug.Log("[AF.Ads] proveedor: simulado (extra " + LaunchOptions.IntentExtraAdsFill + " en un build de desarrollo)");
            }
#endif

            return new FakeAdService(presenter, LaunchOptions.ResolveAdFill());
        }
    }
}
