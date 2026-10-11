#nullable enable
using System;

namespace AF.Ads
{
    /// <summary>Formato de un anuncio (<c>format</c> de <c>af_ad_*</c>, 08 §8.2).</summary>
    public enum AdFormat
    {
        /// <summary><c>rewarded</c>: recompensado, siempre voluntario.</summary>
        Rewarded = 0,

        /// <summary><c>interstitial</c>: intersticial tras la victoria.</summary>
        Interstitial = 1,
    }

    /// <summary>
    /// Ubicaciones de anuncio con los IDs exactos de GDD 08 §2.2 (<c>AF.Ads</c>) y 05 §6.2 (<c>placement</c> de
    /// <c>af_ad_*</c>, 08 §8.2). En 1.0 hay cinco recompensados activos; <c>rv_daily_chest</c> existe pero está apagada
    /// hasta 1.1 (<c>eco_calendar_double_ad_enabled</c> = false).
    /// </summary>
    public enum AdPlacement
    {
        /// <summary><c>rv_double_harvest</c>: Cosecha doble (x2 semillas del nivel).</summary>
        DoubleHarvest = 0,

        /// <summary><c>rv_undo_refill</c>: Marea +3.</summary>
        UndoRefill = 1,

        /// <summary><c>rv_hint</c>: Morpho.</summary>
        Hint = 2,

        /// <summary><c>rv_reveal</c>: Luciérnaga.</summary>
        Reveal = 3,

        /// <summary><c>rv_extra_canoe</c>: Canoa extra (+1 canoa).</summary>
        ExtraCanoe = 4,

        /// <summary><c>rv_daily_chest</c>: Bitácora x2 (1.1; apagada en 1.0).</summary>
        DailyChest = 5,

        /// <summary><c>interstitial_post_level</c>: intersticial tras tocar «Siguiente» en la pantalla de victoria.</summary>
        InterstitialPostLevel = 6,
    }

    /// <summary>
    /// Momento del juego en el que se pide un intersticial (05 §6.3 <c>context</c>). Solo
    /// <see cref="VictoryScreen"/> lo admite: nunca al empezar, al reintentar, al reiniciar ni en el embudo de fallo de
    /// D-008, aunque Magic Sort lo haga (05 §6.1 y §10).
    /// </summary>
    public enum AdContext
    {
        /// <summary>
        /// <c>VICTORY_SCREEN</c>: tras tocar «Siguiente» (AD_GATE; <c>victory_autonext_s</c> = 0) o tras el avance que sigue a la
        /// Cosecha doble (D-011, punto 4), donde la política lo bloquea (<c>AfterRewarded</c>).
        /// </summary>
        VictoryScreen = 0,

        /// <summary><c>LEVEL_START</c>.</summary>
        LevelStart = 1,

        /// <summary><c>RETRY</c>: «Reintentar» en Nivel fallido.</summary>
        Retry = 2,

        /// <summary><c>RESTART</c>: Volver a la orilla.</summary>
        Restart = 3,

        /// <summary><c>STUCK_MODAL</c>: Aguas estancadas.</summary>
        StuckModal = 4,

        /// <summary><c>GIVE_UP_CONFIRM</c>: ¿Seguro?</summary>
        GiveUpConfirm = 5,

        /// <summary><c>LEVEL_FAILED</c>: Nivel fallido.</summary>
        LevelFailed = 6,

        /// <summary><c>NO_CANOES</c>: Sin canoas.</summary>
        NoCanoes = 7,
    }

    /// <summary>IDs y formato de cada <see cref="AdPlacement"/>.</summary>
    public static class AdPlacements
    {
        /// <summary>Número de ubicaciones.</summary>
        public const int Count = 7;

        private static readonly string[] Ids =
        {
            "rv_double_harvest", "rv_undo_refill", "rv_hint", "rv_reveal", "rv_extra_canoe", "rv_daily_chest",
            "interstitial_post_level",
        };

        /// <summary>ID de la ubicación (p. ej. <c>rv_extra_canoe</c>).</summary>
        public static string ToId(this AdPlacement placement)
        {
            return Ids[Index(placement)];
        }

        /// <summary>Formato de la ubicación.</summary>
        public static AdFormat Format(this AdPlacement placement)
        {
            return Index(placement) == (int)AdPlacement.InterstitialPostLevel ? AdFormat.Interstitial : AdFormat.Rewarded;
        }

        /// <summary>Es una ubicación de recompensado.</summary>
        public static bool IsRewarded(this AdPlacement placement)
        {
            return placement.Format() == AdFormat.Rewarded;
        }

        /// <summary>
        /// Sufijo de las claves por ubicación de 08 §9 (<c>ads_rv_cap_&lt;placement&gt;</c>,
        /// <c>ads_rv_cooldown_s_&lt;placement&gt;</c>): el ID sin <c>rv_</c>, p. ej. <c>extra_canoe</c>.
        /// </summary>
        /// <exception cref="ArgumentException">La ubicación no es de recompensado.</exception>
        public static string RemoteConfigSuffix(this AdPlacement placement)
        {
            if (!placement.IsRewarded())
            {
                throw new ArgumentException("Solo los recompensados tienen topes y cooldown por ubicación.", nameof(placement));
            }

            return placement.ToId().Substring("rv_".Length);
        }

        /// <summary>Convierte un ID de 05 §6.2 en su ubicación.</summary>
        public static bool TryParse(string? id, out AdPlacement placement)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (string.Equals(Ids[i], id, StringComparison.Ordinal))
                {
                    placement = (AdPlacement)i;
                    return true;
                }
            }

            placement = AdPlacement.DoubleHarvest;
            return false;
        }

        private static int Index(AdPlacement placement)
        {
            int i = (int)placement;
            if (i < 0 || i >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(placement), placement, null);
            }

            return i;
        }
    }
}
