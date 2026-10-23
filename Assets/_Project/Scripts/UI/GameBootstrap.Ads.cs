#nullable enable
using System;
using AF.Ads;
using AF.Ads.Max;
using AF.Backend.Firebase;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El proveedor de anuncios de este build (D-042; nota <c>docs/dev/anuncios-max.md</c>): AppLovin MAX en un teléfono con la
    /// configuración de MAX en el build (<see cref="MaxAdProvider"/>); si no, los anuncios simulados de siempre
    /// (<see cref="FakeAdService"/>) en el editor, los tests y los builds de desarrollo, y ninguno en un release. Va siempre envuelto por
    /// <c>WrapConsentAds</c>: MAX no se inicia hasta que hay una decisión de consentimiento y edad, y con &lt;13 nunca (el arranque se
    /// para antes, en <c>BlockedAtBoot</c>). Los tests que fijan un presentador (<see cref="LaunchOptions.AdPresenterOverride"/>) siempre
    /// usan el simulado.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Tests: el proveedor de anuncios (p. ej. <see cref="MaxAdService"/> sobre <see cref="FakeMaxSdkPort"/>); gana a todo.</summary>
        internal static Func<Profile, IClock, IAdService>? AdProviderOverride { get; set; }

        // MAX: los márgenes de cierre (recompensa tardía y vuelta de segundo plano); null con el simulado.
        private IAdTicker? _adTicker;

        /// <summary>Cada fotograma: los márgenes de cierre de MAX (sin asignaciones mientras no toca).</summary>
        private void TickAds()
        {
            _adTicker?.Tick();
        }

        /// <summary>La app vuelve de segundo plano: MAX suelta un anuncio que se quedó sin su cierre.</summary>
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

            if (LaunchOptions.AdPresenterOverride == null)
            {
                IAdService? real = MaxAdProvider.Create(
                    clock,
                    () => FirebaseAuthUid.Current, // SSV (08 §10.2): user_id = UID; sin Firebase, null y MAX no lo manda
                    () => profile.Progress.NextLevel,
                    () => _rcConfigs?.Ads ?? AdsConfig.Defaults,
                    releaseBuild: !Debug.isDebugBuild);
#if !UNITY_EDITOR
                Debug.Log("[AF.Ads] proveedor: " + MaxAdProvider.LastReason); // max, not_configured o config_unreadable; sin datos
#endif
                if (real != null)
                {
                    _adTicker = real as IAdTicker;
                    return real;
                }
            }

            return new FakeAdService(presenter, LaunchOptions.ResolveAdFill());
        }
    }
}
