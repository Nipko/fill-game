#nullable enable
using System;
using AF.Platform;

namespace AF.Ads
{
    /// <summary>Por qué no se muestra un anuncio.</summary>
    public enum AdBlockReason
    {
        /// <summary>Se puede mostrar.</summary>
        None = 0,

        /// <summary><c>biz_ads_enabled</c> = false.</summary>
        AdsDisabled = 1,

        /// <summary><c>biz_interstitials_enabled</c> = false.</summary>
        InterstitialsDisabled = 2,

        /// <summary><c>biz_rewarded_enabled</c> = false.</summary>
        RewardedDisabled = 3,

        /// <summary>El intersticial solo va en <see cref="AdContext.VictoryScreen"/> (05 §10).</summary>
        WrongContext = 4,

        /// <summary>Compró Sin anuncios.</summary>
        NoAds = 5,

        /// <summary>Antes de <c>ads_first_interstitial_level</c> (22).</summary>
        BelowFirstLevel = 6,

        /// <summary>Menos de <c>ads_interstitial_suppress_after_iap_h</c> (24 h) desde una compra.</summary>
        AfterPurchase = 7,

        /// <summary>Tope de la sesión (<c>ads_interstitial_session_cap</c>).</summary>
        SessionCap = 8,

        /// <summary>Tope del día (<c>ads_interstitial_daily_cap</c>).</summary>
        DailyCap = 9,

        /// <summary>Menos de <c>ads_interstitial_min_gap_s</c> desde el anterior.</summary>
        MinGap = 10,

        /// <summary>Menos de <c>ads_interstitial_session_warmup_s</c> desde el inicio de la sesión.</summary>
        Warmup = 11,

        /// <summary>Menos de 60 s desde un recompensado.</summary>
        AfterRewarded = 12,

        /// <summary>Sesión de la instalación.</summary>
        InstallSession = 13,

        /// <summary>Aún no toca por cadencia (<c>ads_interstitial_every_n_levels</c>).</summary>
        Cadence = 14,

        /// <summary>La ubicación está apagada (<c>rv_daily_chest</c> en 1.0).</summary>
        PlacementOff = 15,

        /// <summary>Tope diario de la ubicación (<c>ads_rv_cap_&lt;placement&gt;</c>).</summary>
        PlacementCap = 16,

        /// <summary>Tope diario global (<c>ads_rv_global_cap_day</c>).</summary>
        GlobalCap = 17,

        /// <summary>Cooldown de la ubicación (<c>ads_rv_cooldown_s_&lt;placement&gt;</c>).</summary>
        Cooldown = 18,

        /// <summary>No hay anuncio cargado: el botón de video se oculta (05 §6.2).</summary>
        NoFill = 19,

        /// <summary>Ya hay un anuncio en pantalla.</summary>
        Busy = 20,
    }

    /// <summary>Resultado de una comprobación de <see cref="AdPolicy"/> o <see cref="AdController"/>.</summary>
    public readonly struct AdDecision
    {
        /// <summary>Crea la decisión.</summary>
        public AdDecision(AdBlockReason reason)
        {
            Reason = reason;
        }

        /// <summary>Se puede mostrar.</summary>
        public static AdDecision Allowed => default;

        /// <summary>Motivo del bloqueo (<see cref="AdBlockReason.None"/> si se puede).</summary>
        public AdBlockReason Reason { get; }

        /// <summary>Se puede mostrar.</summary>
        public bool IsAllowed => Reason == AdBlockReason.None;

        /// <inheritdoc />
        public override string ToString()
        {
            return IsAllowed ? "allowed" : "blocked:" + Reason;
        }
    }

    /// <summary>
    /// Reglas de cuándo se puede mostrar un anuncio (GDD 05 §6.1-§6.3 con D-008; 04 §3), sin efectos: lee el perfil
    /// (<see cref="AdLedger"/>), la sesión y el reloj.
    /// <list type="bullet">
    /// <item><b>Intersticial</b> (<c>can_show_interstitial</c> de 05 §6.3): solo en la pantalla de victoria tras tocar
    /// «Siguiente», desde el 22, nunca en la sesión de instalación, cada 3 niveles en 22-60 y cada 2 desde el 61, ≥ 150 s
    /// desde el anterior y ≥ 120 s desde el inicio de la sesión, como mucho 6 por sesión y 12 al día, nunca en las 24 h
    /// siguientes a cualquier compra ni a &lt; 60 s de un recompensado, y 0 con Sin anuncios. Las Canoas infinitas no
    /// cuentan: no suprimen intersticiales (D-008).</item>
    /// <item><b>Recompensado</b>: tope diario por ubicación (5/4/3/3/3), cooldown (60 s; 90 s la Canoa extra) y 15 al día
    /// en total. Sin anuncios no los quita. Si la ubicación existe en un nivel (debut del comodín) lo decide
    /// <c>AF.Boosters</c>.</item>
    /// </list>
    /// </summary>
    public sealed class AdPolicy
    {
        private readonly AdLedger _ledger;
        private readonly AdSession _session;
        private readonly IClock _clock;

        /// <summary>Crea las reglas.</summary>
        /// <exception cref="InvalidOperationException">La configuración no es válida (<see cref="AdsConfig.Validate"/>).</exception>
        public AdPolicy(AdsConfig config, AdLedger ledger, AdSession session, IClock clock)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Config.Validate();
        }

        /// <summary>Cifras con las que decide.</summary>
        public AdsConfig Config { get; }

        /// <summary>
        /// ¿Se puede mostrar el intersticial tras la victoria del nivel <paramref name="level"/> (el que se acaba de ganar),
        /// pedido en <paramref name="context"/>? Sigue el orden de 05 §6.3, con el contexto justo tras los interruptores.
        /// </summary>
        public AdDecision CheckInterstitial(int level, AdContext context)
        {
            if (!Config.BizAdsEnabled)
            {
                return Block(AdBlockReason.AdsDisabled);
            }

            if (!Config.BizInterstitialsEnabled)
            {
                return Block(AdBlockReason.InterstitialsDisabled);
            }

            if (context != AdContext.VictoryScreen)
            {
                return Block(AdBlockReason.WrongContext);
            }

            if (_ledger.NoAds)
            {
                return Block(AdBlockReason.NoAds);
            }

            if (level < Config.AdsFirstInterstitialLevel)
            {
                return Block(AdBlockReason.BelowFirstLevel);
            }

            DateTime now = _clock.UtcNow;
            DateTime? lastIap = _ledger.LastIap;

            // Una compra con fecha futura (reloj atrasado) también suprime: es lo prudente.
            if (lastIap.HasValue && now - lastIap.Value < Config.SuppressAfterPurchase)
            {
                return Block(AdBlockReason.AfterPurchase);
            }

            if (_session.Interstitials >= Config.AdsInterstitialSessionCap)
            {
                return Block(AdBlockReason.SessionCap);
            }

            if (_ledger.InterstitialsToday >= Config.AdsInterstitialDailyCap)
            {
                return Block(AdBlockReason.DailyCap);
            }

            if (_session.LastInterstitialAt.HasValue && now - _session.LastInterstitialAt.Value < Config.InterstitialMinGap)
            {
                return Block(AdBlockReason.MinGap);
            }

            if (now - _session.StartedAt < Config.SessionWarmup)
            {
                return Block(AdBlockReason.Warmup);
            }

            if (_session.LastRewardedAt.HasValue && now - _session.LastRewardedAt.Value < AdsConfig.GapAfterRewarded)
            {
                return Block(AdBlockReason.AfterRewarded);
            }

            if (_session.IsInstallSession)
            {
                return Block(AdBlockReason.InstallSession);
            }

            int? every = Config.InterstitialEvery(level);
            if (!every.HasValue)
            {
                return Block(AdBlockReason.BelowFirstLevel);
            }

            return _ledger.LevelsSinceInterstitial < every.Value ? Block(AdBlockReason.Cadence) : AdDecision.Allowed;
        }

        /// <summary>¿Se puede ofrecer el recompensado de <paramref name="placement"/>? (sin contar si hay anuncio cargado).</summary>
        /// <exception cref="ArgumentException"><paramref name="placement"/> no es de recompensado.</exception>
        public AdDecision CheckRewarded(AdPlacement placement)
        {
            if (!placement.IsRewarded())
            {
                throw new ArgumentException("No es una ubicación de recompensado: " + placement.ToId(), nameof(placement));
            }

            if (!Config.BizAdsEnabled)
            {
                return Block(AdBlockReason.AdsDisabled);
            }

            if (!Config.BizRewardedEnabled)
            {
                return Block(AdBlockReason.RewardedDisabled);
            }

            if (!Config.IsPlacementEnabled(placement))
            {
                return Block(AdBlockReason.PlacementOff);
            }

            if (_ledger.RewardedToday(placement) >= Config.RewardedCap(placement))
            {
                return Block(AdBlockReason.PlacementCap);
            }

            if (_ledger.RewardedTotalToday >= Config.AdsRvGlobalCapDay)
            {
                return Block(AdBlockReason.GlobalCap);
            }

            DateTime? last = _session.LastRewardedAtFor(placement);
            if (last.HasValue && _clock.UtcNow - last.Value < Config.RewardedCooldown(placement))
            {
                return Block(AdBlockReason.Cooldown);
            }

            return AdDecision.Allowed;
        }

        private static AdDecision Block(AdBlockReason reason)
        {
            return new AdDecision(reason);
        }
    }
}
