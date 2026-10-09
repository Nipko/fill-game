#nullable enable
using System;
using AF.Platform;
using AF.Save;

namespace AF.Ads
{
    /// <summary>
    /// Lo que la cadencia de anuncios guarda en el perfil (GDD 05 §6.3; 04 §10.1):
    /// <list type="bullet">
    /// <item><c>daily</c>: recompensados de hoy por ubicación, <c>rv_total</c> e <c>interstitials_today</c>. El cambio de
    /// día reinicia el bloque entero con <see cref="DailyCounters"/>, igual que la cartera, para que ningún módulo deje la
    /// fecha nueva con contadores de ayer;</item>
    /// <item><c>ads.levels_since_interstitial</c> y <c>ads.last_iap</c> (este lo escribe <c>AF.IAP</c> al comprar);</item>
    /// <item><c>entitlements.no_ads</c> (lo escribe la cartera con <c>af_noads</c>) y <c>stats.rv_watched</c>.</item>
    /// </list>
    /// Las lecturas no cambian el perfil. Cada anotación que toca <c>daily</c> marca <c>economy_ts</c> (08 §4.3) y todas
    /// guardan con el <see cref="IProfileStore"/>; si la escritura falla, el cambio sigue en memoria
    /// (<see cref="PendingSave"/>). Usa el mismo <see cref="Profile"/> que la cartera. Un solo hilo.
    /// </summary>
    public sealed class AdLedger
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private readonly IClock _clock;

        /// <summary>Crea el registro sobre <paramref name="profile"/>, que guarda en <paramref name="store"/>.</summary>
        public AdLedger(Profile profile, IProfileStore store, IClock clock)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Compró Sin anuncios: 0 intersticiales; los recompensados siguen (05 §4.1).</summary>
        public bool NoAds => _profile.Entitlements.NoAds;

        /// <summary>Última compra con dinero (UTC), o null.</summary>
        public DateTime? LastIap => _profile.Ads.LastIap;

        /// <summary>Niveles completados desde el último intersticial.</summary>
        public int LevelsSinceInterstitial => _profile.Ads.LevelsSinceInterstitial;

        /// <summary>Intersticiales de hoy (día local).</summary>
        public int InterstitialsToday => IsStale ? 0 : _profile.Daily.InterstitialsToday;

        /// <summary>Recompensados de hoy entre todas las ubicaciones.</summary>
        public int RewardedTotalToday => IsStale ? 0 : _profile.Daily.RvTotal;

        /// <summary>Un cambio quedó sin guardar porque falló la escritura; se reintenta en la próxima anotación o con <see cref="Flush"/>.</summary>
        public bool PendingSave { get; private set; }

        private bool IsStale => DailyCounters.IsStale(_profile.Daily, _clock.LocalDateKey());

        /// <summary>Recompensados de hoy en <paramref name="placement"/> (0 en <c>rv_daily_chest</c>, que no tiene contador en 1.0).</summary>
        public int RewardedToday(AdPlacement placement)
        {
            if (!placement.IsRewarded())
            {
                throw new ArgumentException("No es una ubicación de recompensado: " + placement.ToId(), nameof(placement));
            }

            return IsStale || placement == AdPlacement.DailyChest ? 0 : ReadCounter(_profile.Daily, placement);
        }

        /// <summary>Se completó un nivel de la campaña (antes de la AD_GATE de su victoria): suma 1 a la cadencia.</summary>
        public void RecordLevelCompleted()
        {
            AdsData ads = _profile.Ads;
            if (ads.LevelsSinceInterstitial < int.MaxValue)
            {
                ads.LevelsSinceInterstitial++;
            }

            Save();
        }

        /// <summary>Se vio un intersticial: +1 hoy y la cadencia vuelve a 0.</summary>
        public void RecordInterstitialShown()
        {
            DateTime now = _clock.UtcNow;
            DailyCounters.RollOver(_profile.Daily, _clock.LocalDateKey());
            _profile.Daily.InterstitialsToday++;
            _profile.Ads.LevelsSinceInterstitial = 0;
            _profile.EconomyTs = now;
            Save();
        }

        /// <summary>Se concedió la recompensa de <paramref name="placement"/>: +1 en su contador, en <c>rv_total</c> y en <c>stats.rv_watched</c>.</summary>
        /// <exception cref="NotSupportedException"><c>rv_daily_chest</c>: llega en 1.1 con su contador.</exception>
        public void RecordRewarded(AdPlacement placement)
        {
            if (!placement.IsRewarded())
            {
                throw new ArgumentException("No es una ubicación de recompensado: " + placement.ToId(), nameof(placement));
            }

            if (placement == AdPlacement.DailyChest)
            {
                throw new NotSupportedException("rv_daily_chest llega en 1.1 (05 §6.2): daily aún no tiene su contador.");
            }

            DateTime now = _clock.UtcNow;
            DailyData daily = _profile.Daily;
            DailyCounters.RollOver(daily, _clock.LocalDateKey());
            WriteCounter(daily, placement, ReadCounter(daily, placement) + 1);
            daily.RvTotal++;
            _profile.Stats.RvWatched++;
            _profile.EconomyTs = now;
            Save();
        }

        /// <summary>Reintenta un guardado pendiente. Devuelve si no queda nada por guardar.</summary>
        public bool Flush()
        {
            if (PendingSave)
            {
                Save();
            }

            return !PendingSave;
        }

        private void Save()
        {
            PendingSave = !_store.TrySave(_profile);
        }

        private static int ReadCounter(DailyData daily, AdPlacement placement)
        {
            switch (placement)
            {
                case AdPlacement.DoubleHarvest: return daily.RvDoubleHarvest;
                case AdPlacement.UndoRefill: return daily.RvUndoRefill;
                case AdPlacement.Hint: return daily.RvHint;
                case AdPlacement.Reveal: return daily.RvReveal;
                case AdPlacement.ExtraCanoe: return daily.RvExtraCanoe;
                default: throw new ArgumentOutOfRangeException(nameof(placement), placement, null);
            }
        }

        private static void WriteCounter(DailyData daily, AdPlacement placement, int value)
        {
            switch (placement)
            {
                case AdPlacement.DoubleHarvest: daily.RvDoubleHarvest = value; break;
                case AdPlacement.UndoRefill: daily.RvUndoRefill = value; break;
                case AdPlacement.Hint: daily.RvHint = value; break;
                case AdPlacement.Reveal: daily.RvReveal = value; break;
                case AdPlacement.ExtraCanoe: daily.RvExtraCanoe = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(placement), placement, null);
            }
        }
    }
}
