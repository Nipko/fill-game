#nullable enable
using System;
using AF.Ads;

namespace AF.Analytics.Adapters
{
    /// <summary>
    /// Analítica de anuncios (08 §8.2: <c>af_ad_request</c>, <c>af_ad_shown</c> y <c>af_ad_reward</c>) como envoltorio de un
    /// <see cref="IAdService"/>: no cambia nada del anuncio, solo mira el resultado. <c>AF.Ads</c> no se toca (el arranque
    /// envuelve el servicio falso de la fase 2, y más adelante el de MAX). Sin ILR ni SSV en la fase 2: <c>ad_impression</c>
    /// llega con MAX y <c>af_ad_reward</c> sale con <c>granted</c> = <c>provisional</c> [P]. Los parámetros que el servicio
    /// falso no conoce (<c>ms</c>, <c>network</c>, <c>gap_since_last_s</c>…) no se envían, en lugar de inventarlos [P].
    /// </summary>
    public sealed class AnalyticsAdService : IAdService
    {
        private readonly IAdService _inner;
        private readonly AnalyticsService _analytics;

        /// <summary>Envuelve <paramref name="inner"/>.</summary>
        public AnalyticsAdService(IAdService inner, AnalyticsService analytics)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        /// <inheritdoc />
        public bool IsShowing => _inner.IsShowing;

        /// <inheritdoc />
        public bool IsReady(AdPlacement placement) => _inner.IsReady(placement);

        /// <inheritdoc />
        public void Show(AdPlacement placement, Action<AdOutcome> onDone)
        {
            _inner.Show(placement, outcome =>
            {
                Record(placement, outcome);
                onDone?.Invoke(outcome);
            });
        }

        /// <summary>Los eventos de un resultado (público para las pruebas).</summary>
        public void Record(AdPlacement placement, AdOutcome outcome)
        {
            string id = placement.ToId();
            string format = placement.Format() == AdFormat.Rewarded ? "rewarded" : "interstitial";
            if (outcome == AdOutcome.Blocked)
            {
                return; // la política no pidió anuncio: no hubo carga
            }

            string result = outcome == AdOutcome.NoFill ? "no_fill" : outcome == AdOutcome.Failed ? "error" : "filled";
            _analytics.Log(AnalyticsEvent.Create("af_ad_request").Add("placement", id).Add("format", format).Add("result", result));
            if (result != "filled")
            {
                return;
            }

            _analytics.Log(AnalyticsEvent.Create("af_ad_shown").Add("placement", id).Add("format", format));
            if (outcome == AdOutcome.Rewarded)
            {
                _analytics.Log(AnalyticsEvent.Create("af_ad_reward").Add("placement", id).Add("granted", "provisional"));
            }
        }
    }
}
