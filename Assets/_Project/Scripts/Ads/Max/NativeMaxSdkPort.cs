#nullable enable
using System;
using UnityEngine;

namespace AF.Ads.Max
{
    /// <summary>
    /// La configuración de MAX de este build (D-042): las unidades de anuncio de Android, que el build escribe como recursos
    /// (<c>af_max_rewarded_unit</c>, <c>af_max_interstitial_unit</c> y <c>af_max_configured</c>) desde
    /// <c>Assets/_LocalSecrets/applovin-max.json</c> o las variables de entorno del CI (<c>MaxBuildConfig</c>); la clave del SDK va en el
    /// <c>applovin_settings.json</c> del propio plugin. Nada de esto está en el repositorio. En el editor y en los tests, nunca hay
    /// configuración: se usan los anuncios simulados.
    /// </summary>
    public sealed class MaxConfig
    {
        private MaxConfig(string rewardedUnit, string interstitialUnit)
        {
            RewardedUnit = rewardedUnit;
            InterstitialUnit = interstitialUnit;
        }

        /// <summary>La unidad de recompensados de Android.</summary>
        public string RewardedUnit { get; }

        /// <summary>La unidad de intersticiales de Android.</summary>
        public string InterstitialUnit { get; }

        /// <summary>
        /// Lee la configuración del build; null si no la hay (sin el archivo local o fuera de un teléfono Android) o no se puede leer.
        /// <paramref name="why"/> dice por qué (<c>editor</c>, <c>not_configured</c>, <c>config_unreadable</c>), sin datos.
        /// </summary>
        public static MaxConfig? Read(out string why)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaObject context = UnityEngine.Android.AndroidApplication.currentContext; // de Unity: no se libera aquí
                using (AndroidJavaObject resources = context.Call<AndroidJavaObject>("getResources"))
                {
                    string package = context.Call<string>("getPackageName");
                    string? configured = ReadString(resources, package, "af_max_configured");
                    string? rewarded = ReadString(resources, package, "af_max_rewarded_unit");
                    string? interstitial = ReadString(resources, package, "af_max_interstitial_unit");
                    if (configured != "true" || string.IsNullOrEmpty(rewarded) || string.IsNullOrEmpty(interstitial))
                    {
                        why = "not_configured";
                        return null;
                    }

                    why = "ok";
                    return new MaxConfig(rewarded!, interstitial!);
                }
            }
            catch (Exception)
            {
                why = "config_unreadable";
                return null;
            }
#else
            why = "editor"; // el editor y los tests usan los anuncios simulados (08 §2.1, principio 6)
            return null;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string? ReadString(AndroidJavaObject resources, string package, string name)
        {
            int id = resources.Call<int>("getIdentifier", name, "string", package);
            return id == 0 ? null : resources.Call<string>("getString", id);
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// <see cref="IMaxSdkPort"/> sobre el plugin de AppLovin MAX 8.6.6 (SDK Android 13.6.4; D-042). Solo en un teléfono Android: el
    /// editor ARM64 no carga MAX y los tests usan <see cref="FakeMaxSdkPort"/>. Los callbacks de MAX llegan en el hilo principal
    /// (<c>MaxSdkBase.InvokeEventsOnUnityMainThread</c> = true) y se filtran por unidad. Nunca escribe el UID ni las unidades en los logs.
    /// </summary>
    public sealed class NativeMaxSdkPort : IMaxSdkPort
    {
        private readonly string _rewarded;
        private readonly string _interstitial;
        private bool _hooked;

        /// <summary>Crea el puerto con las unidades de <paramref name="config"/>; no toca MAX hasta la primera llamada.</summary>
        public NativeMaxSdkPort(MaxConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _rewarded = config.RewardedUnit;
            _interstitial = config.InterstitialUnit;
        }

        /// <inheritdoc />
        public IMaxSdkListener? Listener { get; set; }

        /// <inheritdoc />
        public void SetHasUserConsent(bool value) => MaxSdk.SetHasUserConsent(value);

        /// <inheritdoc />
        public void SetDoNotSell(bool value) => MaxSdk.SetDoNotSell(value);

        /// <inheritdoc />
        public void SetUserId(string userId) => MaxSdk.SetUserId(userId);

        /// <inheritdoc />
        public void Initialize()
        {
            Hook();
            MaxSdk.InitializeSdk();
        }

        /// <inheritdoc />
        public void Load(MaxAdKind kind)
        {
            Hook();
            if (kind == MaxAdKind.Rewarded)
            {
                MaxSdk.LoadRewardedAd(_rewarded);
            }
            else
            {
                MaxSdk.LoadInterstitial(_interstitial);
            }
        }

        /// <inheritdoc />
        public bool IsReady(MaxAdKind kind)
        {
            return kind == MaxAdKind.Rewarded ? MaxSdk.IsRewardedAdReady(_rewarded) : MaxSdk.IsInterstitialReady(_interstitial);
        }

        /// <inheritdoc />
        public void Show(MaxAdKind kind, string placement, string? customData)
        {
            if (kind == MaxAdKind.Rewarded)
            {
                MaxSdk.ShowRewardedAd(_rewarded, placement, customData);
            }
            else
            {
                MaxSdk.ShowInterstitial(_interstitial, placement);
            }
        }

        /// <inheritdoc />
        public bool ShowCmpForExistingUser()
        {
            if (!MaxSdk.CmpService.HasSupportedCmp)
            {
                return false;
            }

            MaxSdk.CmpService.ShowCmpForExistingUser(_ => { });
            return true;
        }

        private void Hook()
        {
            if (_hooked)
            {
                return;
            }

            _hooked = true;
            MaxSdkBase.InvokeEventsOnUnityMainThread = true;
            MaxSdkCallbacks.OnSdkInitializedEvent += _ => Listener?.OnInitialized();

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (unit, _) => Raise(unit, MaxAdEvent.Loaded);
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (unit, _) => Raise(unit, MaxAdEvent.LoadFailed);
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (unit, _, __) => Raise(unit, MaxAdEvent.DisplayFailed);
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (unit, _, __) => Raise(unit, MaxAdEvent.RewardReceived);
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (unit, _) => Raise(unit, MaxAdEvent.Hidden);
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (unit, info) => Revenue(unit, info);

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (unit, _) => Raise(unit, MaxAdEvent.Loaded);
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (unit, _) => Raise(unit, MaxAdEvent.LoadFailed);
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (unit, _, __) => Raise(unit, MaxAdEvent.DisplayFailed);
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (unit, _) => Raise(unit, MaxAdEvent.Hidden);
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (unit, info) => Revenue(unit, info);
        }

        private bool TryKind(string unit, out MaxAdKind kind)
        {
            if (string.Equals(unit, _rewarded, StringComparison.Ordinal))
            {
                kind = MaxAdKind.Rewarded;
                return true;
            }

            kind = MaxAdKind.Interstitial;
            return string.Equals(unit, _interstitial, StringComparison.Ordinal);
        }

        private void Raise(string unit, MaxAdEvent ev)
        {
            if (TryKind(unit, out MaxAdKind kind))
            {
                Listener?.OnAdEvent(kind, ev);
            }
        }

        private void Revenue(string unit, MaxSdkBase.AdInfo info)
        {
            if (info != null && TryKind(unit, out MaxAdKind kind))
            {
                Listener?.OnRevenuePaid(kind, info.NetworkName ?? string.Empty, info.Revenue);
            }
        }
    }
#endif
}
