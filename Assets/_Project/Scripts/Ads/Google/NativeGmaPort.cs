#if UNITY_ANDROID && !UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

namespace AF.Ads.Google
{
    /// <summary>
    /// <see cref="IGmaPort"/> sobre el plugin Google Mobile Ads 11.5.0 (<c>play-services-ads</c> 25.4.0 y UMP 4.0.0; D-049). Solo en un
    /// teléfono Android: el editor y los tests usan <see cref="FakeGmaPort"/>. Los avisos del SDK (que pueden llegar en otro hilo) se
    /// encolan y se entregan en <see cref="Pump"/>, en el hilo principal. Nunca escribe el UID ni los bloques en los logs.
    /// </summary>
    public sealed class NativeGmaPort : IGmaPort
    {
        private readonly GoogleAdUnits _units;
        private readonly Queue<Action> _queue = new Queue<Action>();
        private readonly object _lock = new object();
        private RewardedAd? _rewarded;
        private InterstitialAd? _interstitial;
        private int _queued; // lectura sin candado en Pump: evita entrar en el candado cada fotograma

        /// <summary>Crea el puerto con <paramref name="units"/>; no toca el SDK hasta la primera llamada.</summary>
        public NativeGmaPort(GoogleAdUnits units)
        {
            _units = units ?? throw new ArgumentNullException(nameof(units));
        }

        /// <inheritdoc />
        public IGmaListener? Listener { get; set; }

        /// <inheritdoc />
        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        /// <inheritdoc />
        public void SetRequestConfiguration(bool underAgeOfConsent)
        {
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                MaxAdContentRating = MaxAdContentRating.G, // D-029, todas las bandas
                TagForUnderAgeOfConsent = underAgeOfConsent ? TagForUnderAgeOfConsent.True : TagForUnderAgeOfConsent.Unspecified,
            });
        }

        /// <inheritdoc />
        public void RequestConsent(bool underAgeOfConsent)
        {
            var parameters = new ConsentRequestParameters { TagForUnderAgeOfConsent = underAgeOfConsent };
            ConsentInformation.Update(parameters, updateError => Enqueue(() =>
            {
                if (updateError != null)
                {
                    ResolveConsent(); // sin red: CanRequestAds dice si vale el consentimiento de una sesión anterior
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError => Enqueue(ResolveConsent));
            }));
        }

        /// <inheritdoc />
        public bool ShowPrivacyOptionsForm()
        {
            ConsentForm.ShowPrivacyOptionsForm(_ => { });
            return true;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            MobileAds.Initialize(_ => Enqueue(() => Listener?.OnInitialized()));
        }

        /// <inheritdoc />
        public void Load(GmaAdKind kind, bool nonPersonalized)
        {
            var request = new AdRequest();
            if (nonPersonalized)
            {
                request.Extras.Add("npa", "1"); // 13-17, sin consentimiento de personalizados o sin país (08 §7)
            }

            string unit = _units.For(kind);
            if (kind == GmaAdKind.Rewarded)
            {
                RewardedAd.Load(unit, request, (ad, error) => Enqueue(() => OnRewardedLoaded(ad, error)));
            }
            else
            {
                InterstitialAd.Load(unit, request, (ad, error) => Enqueue(() => OnInterstitialLoaded(ad, error)));
            }
        }

        /// <inheritdoc />
        public bool IsReady(GmaAdKind kind)
        {
            return kind == GmaAdKind.Rewarded
                ? _rewarded != null && _rewarded.CanShowAd()
                : _interstitial != null && _interstitial.CanShowAd();
        }

        /// <inheritdoc />
        public void Show(GmaAdKind kind, string? userId, string? customData)
        {
            if (kind == GmaAdKind.Rewarded)
            {
                RewardedAd? ad = _rewarded;
                if (ad == null)
                {
                    Listener?.OnAdEvent(kind, GmaAdEvent.DisplayFailed);
                    return;
                }

                // SSV de AdMob (08 §10.2; D-049): solo con UID y nonce; sin ellos no se fija nada (el callback llegaría vacío).
                if (customData != null && userId != null)
                {
                    ad.SetServerSideVerificationOptions(new ServerSideVerificationOptions { UserId = userId, CustomData = customData });
                }
                ad.Show(_ => Enqueue(() => Listener?.OnAdEvent(GmaAdKind.Rewarded, GmaAdEvent.RewardReceived)));
            }
            else
            {
                InterstitialAd? ad = _interstitial;
                if (ad == null)
                {
                    Listener?.OnAdEvent(kind, GmaAdEvent.DisplayFailed);
                    return;
                }

                ad.Show();
            }
        }

        /// <inheritdoc />
        public void Pump()
        {
            if (System.Threading.Volatile.Read(ref _queued) == 0)
            {
                return;
            }

            while (true)
            {
                Action next;
                lock (_lock)
                {
                    if (_queue.Count == 0)
                    {
                        _queued = 0;
                        return;
                    }

                    next = _queue.Dequeue();
                }

                next();
            }
        }

        private void Enqueue(Action action)
        {
            lock (_lock)
            {
                _queue.Enqueue(action);
                _queued = 1;
            }
        }

        private void ResolveConsent()
        {
            Listener?.OnConsentResolved(ConsentInformation.CanRequestAds());
        }

        private void OnRewardedLoaded(RewardedAd? ad, LoadAdError? error)
        {
            if (error != null || ad == null)
            {
                Listener?.OnAdEvent(GmaAdKind.Rewarded, GmaAdEvent.LoadFailed);
                return;
            }

            _rewarded?.Destroy();
            _rewarded = ad;
            ad.OnAdFullScreenContentClosed += () => Enqueue(() => OnClosed(GmaAdKind.Rewarded, ad));
            ad.OnAdFullScreenContentFailed += _ => Enqueue(() => OnFailed(GmaAdKind.Rewarded, ad));
            ad.OnAdPaid += value => Enqueue(() => OnPaid(GmaAdKind.Rewarded, ad.GetResponseInfo, value));
            Listener?.OnAdEvent(GmaAdKind.Rewarded, GmaAdEvent.Loaded);
        }

        private void OnInterstitialLoaded(InterstitialAd? ad, LoadAdError? error)
        {
            if (error != null || ad == null)
            {
                Listener?.OnAdEvent(GmaAdKind.Interstitial, GmaAdEvent.LoadFailed);
                return;
            }

            _interstitial?.Destroy();
            _interstitial = ad;
            ad.OnAdFullScreenContentClosed += () => Enqueue(() => OnClosed(GmaAdKind.Interstitial, ad));
            ad.OnAdFullScreenContentFailed += _ => Enqueue(() => OnFailed(GmaAdKind.Interstitial, ad));
            ad.OnAdPaid += value => Enqueue(() => OnPaid(GmaAdKind.Interstitial, ad.GetResponseInfo, value));
            Listener?.OnAdEvent(GmaAdKind.Interstitial, GmaAdEvent.Loaded);
        }

        private void OnClosed(GmaAdKind kind, object ad)
        {
            Release(kind, ad);
            Listener?.OnAdEvent(kind, GmaAdEvent.Closed);
        }

        private void OnFailed(GmaAdKind kind, object ad)
        {
            Release(kind, ad);
            Listener?.OnAdEvent(kind, GmaAdEvent.DisplayFailed);
        }

        /// <summary>Un anuncio de Google se muestra una sola vez: tras cerrarse o fallar se destruye.</summary>
        private void Release(GmaAdKind kind, object ad)
        {
            if (kind == GmaAdKind.Rewarded && ReferenceEquals(_rewarded, ad))
            {
                _rewarded!.Destroy();
                _rewarded = null;
            }
            else if (kind == GmaAdKind.Interstitial && ReferenceEquals(_interstitial, ad))
            {
                _interstitial!.Destroy();
                _interstitial = null;
            }
        }

        private void OnPaid(GmaAdKind kind, Func<ResponseInfo> responseInfo, AdValue? value)
        {
            if (value == null)
            {
                return;
            }

            string source = string.Empty;
            try
            {
                source = responseInfo()?.GetLoadedAdapterResponseInfo()?.AdSourceName ?? string.Empty;
            }
            catch (Exception)
            {
                // el anuncio ya se destruyó: la impresión sale sin la red
            }

            Listener?.OnPaid(kind, source, value.Value, value.CurrencyCode ?? string.Empty);
        }
    }
}
#endif
