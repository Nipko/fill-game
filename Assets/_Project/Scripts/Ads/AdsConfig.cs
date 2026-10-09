#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;

namespace AF.Ads
{
    /// <summary>Un tramo de <c>ads_interstitial_every_n_levels</c>: desde <see cref="FromLevel"/>, 1 intersticial cada <see cref="EveryLevels"/> niveles.</summary>
    public readonly struct CadenceBand
    {
        /// <summary>Crea el tramo <c>[fromLevel, everyLevels]</c>.</summary>
        public CadenceBand(int fromLevel, int everyLevels)
        {
            FromLevel = fromLevel;
            EveryLevels = everyLevels;
        }

        /// <summary>Primer nivel del tramo.</summary>
        public int FromLevel { get; }

        /// <summary>Niveles completados por intersticial.</summary>
        public int EveryLevels { get; }
    }

    /// <summary>
    /// Cifras de los anuncios (GDD 05 §6; 04 §3) con los <b>defaults compilados</b> de la tabla única de 08 §9 y sus nombres
    /// exactos: <c>AdsConfigTests</c> los contrasta con el GDD. <see cref="Validate"/> exige además los rangos de 08 §9, que
    /// son guardarraíles de 05 §10 (p. ej. ningún intersticial antes del 22). Cuando exista <c>AF.RemoteConfig</c>, sus
    /// valores se copiarán en una instancia; hasta entonces se usa <see cref="Defaults"/>.
    /// <para>
    /// Desviación del plan (B1-3): no hay <c>Assets/_Project/Data/Config/ad_placements.json</c>. Las ubicaciones van en
    /// <see cref="AdPlacement"/> y sus topes y cooldowns aquí, compilados, porque AF.Ads no tiene un lector JSON público al
    /// alcance (el de AF.Levels queda fuera de sus dependencias de 08 §2.2 y el de AF.Save es interno) y no se añaden
    /// paquetes.
    /// </para>
    /// </summary>
    public sealed class AdsConfig
    {
        /// <summary>
        /// Separación mínima entre un recompensado y un intersticial (05 §6.1 «nunca a &lt;60 s de un recompensado» [P];
        /// 05 §6.3). No tiene clave en 08 §9.
        /// </summary>
        public static readonly TimeSpan GapAfterRewarded = TimeSpan.FromSeconds(60);

        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static AdsConfig Defaults => new AdsConfig();

        /// <summary><c>biz_ads_enabled</c>: interruptor de todos los anuncios (los recompensados pasan a cobrar semillas).</summary>
        [RemoteConfigKey("biz_ads_enabled")]
        public bool BizAdsEnabled { get; set; } = true;

        /// <summary><c>biz_interstitials_enabled</c>: único interruptor de los intersticiales.</summary>
        [RemoteConfigKey("biz_interstitials_enabled")]
        public bool BizInterstitialsEnabled { get; set; } = true;

        /// <summary><c>biz_rewarded_enabled</c>: interruptor de los recompensados.</summary>
        [RemoteConfigKey("biz_rewarded_enabled")]
        public bool BizRewardedEnabled { get; set; } = true;

        /// <summary><c>ads_first_interstitial_level</c>: ningún intersticial antes de este nivel (≥ 22).</summary>
        [RemoteConfigKey("ads_first_interstitial_level")]
        public int AdsFirstInterstitialLevel { get; set; } = 22;

        /// <summary><c>ads_interstitial_every_n_levels</c>: cada 3 niveles en 22-60 y cada 2 desde el 61.</summary>
        [RemoteConfigKey("ads_interstitial_every_n_levels")]
        public IReadOnlyList<CadenceBand> AdsInterstitialEveryNLevels { get; set; } = new[] { new CadenceBand(22, 3), new CadenceBand(61, 2) };

        /// <summary><c>ads_interstitial_min_gap_s</c>: segundos desde el intersticial anterior.</summary>
        [RemoteConfigKey("ads_interstitial_min_gap_s")]
        public int AdsInterstitialMinGapS { get; set; } = 150;

        /// <summary><c>ads_interstitial_session_warmup_s</c>: segundos desde el inicio de la sesión.</summary>
        [RemoteConfigKey("ads_interstitial_session_warmup_s")]
        public int AdsInterstitialSessionWarmupS { get; set; } = 120;

        /// <summary><c>ads_interstitial_session_cap</c>: intersticiales por sesión.</summary>
        [RemoteConfigKey("ads_interstitial_session_cap")]
        public int AdsInterstitialSessionCap { get; set; } = 6;

        /// <summary><c>ads_interstitial_daily_cap</c>: intersticiales por día local.</summary>
        [RemoteConfigKey("ads_interstitial_daily_cap")]
        public int AdsInterstitialDailyCap { get; set; } = 12;

        /// <summary><c>ads_interstitial_suppress_after_iap_h</c>: horas sin intersticiales tras cualquier compra.</summary>
        [RemoteConfigKey("ads_interstitial_suppress_after_iap_h")]
        public int AdsInterstitialSuppressAfterIapH { get; set; } = 24;

        /// <summary><c>ads_rv_cap_double_harvest</c>: Cosechas dobles por día.</summary>
        [RemoteConfigKey("ads_rv_cap_double_harvest")]
        public int AdsRvCapDoubleHarvest { get; set; } = 5;

        /// <summary><c>ads_rv_cap_undo_refill</c>: recargas de Marea por video al día.</summary>
        [RemoteConfigKey("ads_rv_cap_undo_refill")]
        public int AdsRvCapUndoRefill { get; set; } = 4;

        /// <summary><c>ads_rv_cap_hint</c>: Morphos por video al día.</summary>
        [RemoteConfigKey("ads_rv_cap_hint")]
        public int AdsRvCapHint { get; set; } = 3;

        /// <summary><c>ads_rv_cap_reveal</c>: Luciérnagas por video al día.</summary>
        [RemoteConfigKey("ads_rv_cap_reveal")]
        public int AdsRvCapReveal { get; set; } = 3;

        /// <summary><c>ads_rv_cap_extra_canoe</c>: Canoas extra por video al día.</summary>
        [RemoteConfigKey("ads_rv_cap_extra_canoe")]
        public int AdsRvCapExtraCanoe { get; set; } = 3;

        /// <summary><c>ads_rv_cap_daily_chest</c>: Bitácora x2 al día (ubicación apagada en 1.0).</summary>
        [RemoteConfigKey("ads_rv_cap_daily_chest")]
        public int AdsRvCapDailyChest { get; set; } = 1;

        /// <summary><c>ads_rv_cooldown_s_double_harvest</c>.</summary>
        [RemoteConfigKey("ads_rv_cooldown_s_double_harvest")]
        public int AdsRvCooldownSDoubleHarvest { get; set; } = 60;

        /// <summary><c>ads_rv_cooldown_s_undo_refill</c>.</summary>
        [RemoteConfigKey("ads_rv_cooldown_s_undo_refill")]
        public int AdsRvCooldownSUndoRefill { get; set; } = 60;

        /// <summary><c>ads_rv_cooldown_s_hint</c>.</summary>
        [RemoteConfigKey("ads_rv_cooldown_s_hint")]
        public int AdsRvCooldownSHint { get; set; } = 60;

        /// <summary><c>ads_rv_cooldown_s_reveal</c>.</summary>
        [RemoteConfigKey("ads_rv_cooldown_s_reveal")]
        public int AdsRvCooldownSReveal { get; set; } = 60;

        /// <summary><c>ads_rv_cooldown_s_extra_canoe</c>: 90 s.</summary>
        [RemoteConfigKey("ads_rv_cooldown_s_extra_canoe")]
        public int AdsRvCooldownSExtraCanoe { get; set; } = 90;

        /// <summary><c>ads_rv_cooldown_s_daily_chest</c>.</summary>
        [RemoteConfigKey("ads_rv_cooldown_s_daily_chest")]
        public int AdsRvCooldownSDailyChest { get; set; } = 60;

        /// <summary><c>ads_rv_global_cap_day</c>: recompensados al día entre todas las ubicaciones.</summary>
        [RemoteConfigKey("ads_rv_global_cap_day")]
        public int AdsRvGlobalCapDay { get; set; } = 15;

        /// <summary><c>eco_calendar_double_ad_enabled</c>: ubicación <c>rv_daily_chest</c> (false en 1.0; 1.1: true).</summary>
        [RemoteConfigKey("eco_calendar_double_ad_enabled")]
        public bool EcoCalendarDoubleAdEnabled { get; set; }

        /// <summary>Separación mínima entre intersticiales.</summary>
        public TimeSpan InterstitialMinGap => TimeSpan.FromSeconds(AdsInterstitialMinGapS);

        /// <summary>Calentamiento de la sesión.</summary>
        public TimeSpan SessionWarmup => TimeSpan.FromSeconds(AdsInterstitialSessionWarmupS);

        /// <summary>Supresión tras una compra.</summary>
        public TimeSpan SuppressAfterPurchase => TimeSpan.FromHours(AdsInterstitialSuppressAfterIapH);

        /// <summary>Tope diario de la ubicación de recompensado <paramref name="placement"/>.</summary>
        public int RewardedCap(AdPlacement placement)
        {
            switch (RequireRewarded(placement))
            {
                case AdPlacement.DoubleHarvest: return AdsRvCapDoubleHarvest;
                case AdPlacement.UndoRefill: return AdsRvCapUndoRefill;
                case AdPlacement.Hint: return AdsRvCapHint;
                case AdPlacement.Reveal: return AdsRvCapReveal;
                case AdPlacement.ExtraCanoe: return AdsRvCapExtraCanoe;
                default: return AdsRvCapDailyChest;
            }
        }

        /// <summary>Cooldown de la ubicación de recompensado <paramref name="placement"/>.</summary>
        public TimeSpan RewardedCooldown(AdPlacement placement)
        {
            int seconds;
            switch (RequireRewarded(placement))
            {
                case AdPlacement.DoubleHarvest: seconds = AdsRvCooldownSDoubleHarvest; break;
                case AdPlacement.UndoRefill: seconds = AdsRvCooldownSUndoRefill; break;
                case AdPlacement.Hint: seconds = AdsRvCooldownSHint; break;
                case AdPlacement.Reveal: seconds = AdsRvCooldownSReveal; break;
                case AdPlacement.ExtraCanoe: seconds = AdsRvCooldownSExtraCanoe; break;
                default: seconds = AdsRvCooldownSDailyChest; break;
            }

            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>La ubicación está encendida en esta configuración (todas salvo <c>rv_daily_chest</c> en 1.0).</summary>
        public bool IsPlacementEnabled(AdPlacement placement)
        {
            return placement != AdPlacement.DailyChest || EcoCalendarDoubleAdEnabled;
        }

        /// <summary>
        /// Cadencia en el nivel <paramref name="level"/>: el <see cref="CadenceBand.EveryLevels"/> del último tramo que
        /// empieza en ese nivel o antes; null antes del primer tramo.
        /// </summary>
        public int? InterstitialEvery(int level)
        {
            int? every = null;
            IReadOnlyList<CadenceBand> bands = AdsInterstitialEveryNLevels;
            for (int i = 0; i < bands.Count; i++)
            {
                if (bands[i].FromLevel <= level)
                {
                    every = bands[i].EveryLevels;
                }
            }

            return every;
        }

        /// <summary>Comprueba los rangos de 08 §9.</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de su rango.</exception>
        public void Validate()
        {
            Require(AdsFirstInterstitialLevel >= 22, "ads_first_interstitial_level debe ser >= 22 (05 §10)");
            IReadOnlyList<CadenceBand> bands = AdsInterstitialEveryNLevels;
            Require(bands != null && bands.Count > 0, "ads_interstitial_every_n_levels no puede estar vacío");
            for (int i = 0; i < bands!.Count; i++)
            {
                Require(bands[i].FromLevel >= 1 && bands[i].EveryLevels >= 2, "ads_interstitial_every_n_levels: n >= 2");
                Require(i == 0 || bands[i].FromLevel > bands[i - 1].FromLevel, "ads_interstitial_every_n_levels debe ir en orden creciente");
            }

            Require(AdsInterstitialMinGapS >= 150, "ads_interstitial_min_gap_s debe ser >= 150");
            Require(AdsInterstitialSessionWarmupS >= 60, "ads_interstitial_session_warmup_s debe ser >= 60");
            Require(AdsInterstitialSessionCap >= 0 && AdsInterstitialSessionCap <= 6, "ads_interstitial_session_cap debe estar en 0-6");
            Require(AdsInterstitialDailyCap >= 0 && AdsInterstitialDailyCap <= 14, "ads_interstitial_daily_cap debe estar en 0-14");
            Require(AdsInterstitialSuppressAfterIapH >= 12 && AdsInterstitialSuppressAfterIapH <= 48, "ads_interstitial_suppress_after_iap_h debe estar en 12-48");
            for (int i = 0; i < AdPlacements.Count; i++)
            {
                var placement = (AdPlacement)i;
                if (!placement.IsRewarded())
                {
                    continue;
                }

                int cap = RewardedCap(placement);
                double cooldown = RewardedCooldown(placement).TotalSeconds;
                Require(cap >= 1 && cap <= 5, "ads_rv_cap_" + placement.RemoteConfigSuffix() + " debe estar en 1-5");
                Require(cooldown >= 60 && cooldown <= 120, "ads_rv_cooldown_s_" + placement.RemoteConfigSuffix() + " debe estar en 60-120");
            }

            Require(AdsRvGlobalCapDay >= 10 && AdsRvGlobalCapDay <= 20, "ads_rv_global_cap_day debe estar en 10-20");

            // daily (04 §10.1) no tiene contador para rv_daily_chest: la ubicación llega en 1.1 con el suyo.
            Require(!EcoCalendarDoubleAdEnabled, "eco_calendar_double_ad_enabled: rv_daily_chest llega en 1.1 (05 §6.2)");
        }

        /// <summary>Escribe los tramos con la sintaxis de 08 §9, p. ej. <c>[[22,3],[61,2]]</c>.</summary>
        public static string FormatBands(IReadOnlyList<CadenceBand> bands)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < bands.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('[').Append(bands[i].FromLevel.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(bands[i].EveryLevels.ToString(CultureInfo.InvariantCulture)).Append(']');
            }

            return sb.Append(']').ToString();
        }

        private static AdPlacement RequireRewarded(AdPlacement placement)
        {
            if (!placement.IsRewarded())
            {
                throw new ArgumentException("No es una ubicación de recompensado: " + placement.ToId(), nameof(placement));
            }

            return placement;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("AdsConfig: " + message + ".");
            }
        }
    }
}
