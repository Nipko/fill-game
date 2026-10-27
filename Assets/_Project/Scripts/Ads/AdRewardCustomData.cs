#nullable enable
using System;
using System.Text.RegularExpressions;

namespace AF.Ads
{
    /// <summary>
    /// El <c>custom_data</c> del SSV de los recompensados (08 §10.1 y §10.2; contrato <c>infra/contracts/v1/rewarded-ssv</c>):
    /// <c>placement|nonce</c>, con el ID de la ubicación (<c>[a-z0-9_]{1,40}</c>) y un nonce UUID v4 en minúsculas, que es lo que
    /// acepta la Lambda <c>rewardedSsv</c> (<c>parseSsvQuery</c>) y lo que <c>restoreLedger</c> confirma después.
    /// </summary>
    public static class AdRewardCustomData
    {
        private static readonly Regex NonceRe = new Regex(
            @"\A[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\z", RegexOptions.CultureInvariant);

        private static readonly Regex PlacementRe = new Regex(@"\A[a-z0-9_]{1,40}\z", RegexOptions.CultureInvariant);

        /// <summary>Un nonce nuevo: UUID v4 en minúsculas (<see cref="Guid.NewGuid"/>, formato <c>D</c>).</summary>
        public static string NewNonce()
        {
            return Guid.NewGuid().ToString("D");
        }

        /// <summary>El nonce tiene la forma que exige el contrato.</summary>
        public static bool IsNonce(string? value)
        {
            return value != null && NonceRe.IsMatch(value);
        }

        /// <summary><c>placement|nonce</c> de un recompensado.</summary>
        /// <exception cref="ArgumentException">La ubicación no es de recompensado o el nonce no tiene la forma del contrato.</exception>
        public static string Format(AdPlacement placement, string nonce)
        {
            if (!placement.IsRewarded())
            {
                throw new ArgumentException("El SSV solo va en los recompensados: " + placement.ToId(), nameof(placement));
            }

            if (!IsNonce(nonce))
            {
                throw new ArgumentException("Nonce no válido (UUID v4 en minúsculas).", nameof(nonce));
            }

            string id = placement.ToId();
            if (!PlacementRe.IsMatch(id))
            {
                throw new ArgumentException("Ubicación no válida para el SSV: " + id, nameof(placement));
            }

            return id + "|" + nonce;
        }
    }

    /// <summary>
    /// Una impresión pagada (<c>OnAdPaid</c> de Google Mobile Ads, D-049) para el evento recomendado de Firebase <c>ad_impression</c> (08 §8.2; <c>ads_ilr_to_firebase</c>):
    /// <c>ad_platform</c>, <c>ad_source</c>, <c>ad_format</c>, <c>ad_unit_name</c>, <c>value</c> y <c>currency</c>. Sin identificadores.
    /// </summary>
    public sealed class AdImpression
    {
        /// <summary><c>ad_platform</c> de Google Mobile Ads (AdMob directo, D-049).</summary>
        public const string PlatformAdMob = "AdMob";

        /// <summary>
        /// Crea la impresión (<paramref name="platform"/>: <see cref="PlatformAdMob"/> por defecto). Las redes de la mediación de AdMob
        /// (también AppLovin si entra así, añadido de D-049 del 2026-10-27) van en <paramref name="adSource"/>, no en la plataforma.
        /// </summary>
        public AdImpression(string adSource, AdPlacement placement, double value, string currency = "USD", string platform = PlatformAdMob)
        {
            AdPlatform = string.IsNullOrEmpty(platform) ? PlatformAdMob : platform;
            AdSource = adSource ?? string.Empty;
            Placement = placement;
            Value = value;
            Currency = currency ?? "USD";
        }

        /// <summary><c>ad_platform</c>: <c>AdMob</c> (Google Mobile Ads, D-049).</summary>
        public string AdPlatform { get; }

        /// <summary><c>ad_source</c>: la red que sirvió el anuncio (el <c>AdSourceName</c> de Google).</summary>
        public string AdSource { get; }

        /// <summary>La ubicación del juego (<c>ad_unit_name</c> = su ID).</summary>
        public AdPlacement Placement { get; }

        /// <summary><c>ad_format</c>: <c>rewarded</c> o <c>interstitial</c>.</summary>
        public string AdFormat => Placement.IsRewarded() ? "rewarded" : "interstitial";

        /// <summary><c>value</c>: ingreso estimado de la impresión.</summary>
        public double Value { get; }

        /// <summary><c>currency</c>: la moneda del <c>AdValue</c> de Google (USD si no viene).</summary>
        public string Currency { get; }
    }

    /// <summary>Un proveedor de anuncios que informa de las impresiones pagadas (ILR). El falso no lo hace.</summary>
    public interface IAdImpressionSource
    {
        /// <summary>Cada impresión pagada, en el hilo principal.</summary>
        event Action<AdImpression>? Impression;
    }
}
