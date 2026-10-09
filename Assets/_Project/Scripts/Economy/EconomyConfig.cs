#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Platform;

namespace AF.Economy
{
    /// <summary>Un tramo de <c>rescue_price_bands</c>: desde el nivel <see cref="FromLevel"/> cuesta <see cref="Price"/>.</summary>
    public readonly struct PriceBand
    {
        /// <summary>Crea el tramo <c>[fromLevel, price]</c>.</summary>
        public PriceBand(int fromLevel, int price)
        {
            FromLevel = fromLevel;
            Price = price;
        }

        /// <summary>Primer nivel del tramo.</summary>
        public int FromLevel { get; }

        /// <summary>Precio en semillas.</summary>
        public int Price { get; }
    }

    /// <summary>
    /// Cifras de la economía en cliente (GDD 04 §2-§7; 05 §2) con los <b>defaults compilados</b> de la tabla única de
    /// 08 §9 y sus nombres exactos (ninguna cifra sale de aquí: <c>EconomyConfigTests</c> las contrasta con el GDD). Cuando
    /// exista <c>AF.RemoteConfig</c>, sus valores se copiarán en una instancia al abrir cada pantalla (04 §10.2: un precio
    /// nunca cambia con un modal abierto); hasta entonces se usa <see cref="Defaults"/>.
    /// </summary>
    public sealed class EconomyConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra: modificarla no afecta a nadie).</summary>
        public static EconomyConfig Defaults => new EconomyConfig();

        /// <summary><c>eco_reward_normal</c>: Cosecha de una victoria sin etiqueta (05 §2.1).</summary>
        [RemoteConfigKey("eco_reward_normal")]
        public int EcoRewardNormal { get; set; } = 20;

        /// <summary><c>eco_reward_hard</c>: Cosecha de una victoria Difícil.</summary>
        [RemoteConfigKey("eco_reward_hard")]
        public int EcoRewardHard { get; set; } = 60;

        /// <summary><c>eco_reward_very_hard</c>: Cosecha de una victoria Muy difícil.</summary>
        [RemoteConfigKey("eco_reward_very_hard")]
        public int EcoRewardVeryHard { get; set; } = 80;

        /// <summary><c>ads_rv_double_harvest_from_level</c>: Cosecha doble (x2 por video) desde este nivel (04 §2.8).</summary>
        [RemoteConfigKey("ads_rv_double_harvest_from_level")]
        public int AdsRvDoubleHarvestFromLevel { get; set; } = 11;

        /// <summary><c>undo_refill_amount</c>: Mareas que da una recarga (04 §2.1).</summary>
        [RemoteConfigKey("undo_refill_amount")]
        public int UndoRefillAmount { get; set; } = 3;

        /// <summary><c>undo_refill_seed_price</c>: precio de la recarga de Marea.</summary>
        [RemoteConfigKey("undo_refill_seed_price")]
        public int UndoRefillSeedPrice { get; set; } = 30;

        /// <summary><c>extra_cuia_seed_price</c>: Cuia extra (04 §2.2).</summary>
        [RemoteConfigKey("extra_cuia_seed_price")]
        public int ExtraCuiaSeedPrice { get; set; } = 150;

        /// <summary><c>hint_seed_price</c>: Morpho (04 §2.3).</summary>
        [RemoteConfigKey("hint_seed_price")]
        public int HintSeedPrice { get; set; } = 100;

        /// <summary><c>shuffle_seed_price</c>: Pororoca (04 §2.4).</summary>
        [RemoteConfigKey("shuffle_seed_price")]
        public int ShuffleSeedPrice { get; set; } = 300;

        /// <summary><c>reveal_seed_price</c>: Luciérnaga (04 §2.5). Solo el precio: el debut no es cosa de la economía.</summary>
        [RemoteConfigKey("reveal_seed_price")]
        public int RevealSeedPrice { get; set; } = 120;

        /// <summary><c>skip_seed_price</c>: Salto del Boto (04 §2.6).</summary>
        [RemoteConfigKey("skip_seed_price")]
        public int SkipSeedPrice { get; set; } = 300;

        /// <summary>
        /// <c>rescue_min_level</c>: debut común de Canoas y Cuia de rescate (04 §7.1). Antes, fallar solo reinicia y no
        /// cuesta canoa (04 §5.2).
        /// </summary>
        [RemoteConfigKey("rescue_min_level")]
        public int RescueMinLevel { get; set; } = 31;

        /// <summary><c>rescue_price_bands</c>: precio de la Cuia de rescate por tramo de nivel, ordenado (600 fijo desde el 31).</summary>
        [RemoteConfigKey("rescue_price_bands")]
        public IReadOnlyList<PriceBand> RescuePriceBands { get; set; } = new[] { new PriceBand(31, 600) };

        /// <summary><c>rescue_first_day_mult</c>: multiplicador del primer rescate del día local (600 → 300).</summary>
        [RemoteConfigKey("rescue_first_day_mult")]
        public double RescueFirstDayMult { get; set; } = 0.5;

        /// <summary><c>canoes_max</c>: tope de canoas (04 §7.1).</summary>
        [RemoteConfigKey("canoes_max")]
        public int CanoesMax { get; set; } = 5;

        /// <summary><c>canoes_regen_min</c>: minutos por canoa regenerada.</summary>
        [RemoteConfigKey("canoes_regen_min")]
        public int CanoesRegenMin { get; set; } = 25;

        /// <summary><c>canoes_refill_seeds_enabled</c>: Llenar canoas con semillas disponible.</summary>
        [RemoteConfigKey("canoes_refill_seeds_enabled")]
        public bool CanoesRefillSeedsEnabled { get; set; } = true;

        /// <summary><c>canoes_refill_seed_price</c>: Llenar canoas (hasta <see cref="CanoesMax"/>).</summary>
        [RemoteConfigKey("canoes_refill_seed_price")]
        public int CanoesRefillSeedPrice { get; set; } = 200;

        /// <summary>Tiempo por canoa: <see cref="CanoesRegenMin"/> minutos.</summary>
        public TimeSpan CanoesRegen => TimeSpan.FromMinutes(CanoesRegenMin);

        /// <summary>Cosecha de una victoria según la etiqueta del nivel (05 §2.1).</summary>
        public int WinReward(RewardTier tier)
        {
            switch (tier)
            {
                case RewardTier.Normal: return EcoRewardNormal;
                case RewardTier.Hard: return EcoRewardHard;
                case RewardTier.VeryHard: return EcoRewardVeryHard;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        /// <summary>Precio fijo de un sumidero (05 §2.2). La Cuia de rescate no tiene precio fijo: <see cref="RescueBasePrice"/>.</summary>
        /// <exception cref="ArgumentException"><paramref name="sink"/> es <see cref="SeedSink.ContinueCuia"/>.</exception>
        public int SeedPrice(SeedSink sink)
        {
            switch (sink)
            {
                case SeedSink.UndoRefill: return UndoRefillSeedPrice;
                case SeedSink.ExtraCuia: return ExtraCuiaSeedPrice;
                case SeedSink.Hint: return HintSeedPrice;
                case SeedSink.Shuffle: return ShuffleSeedPrice;
                case SeedSink.Reveal: return RevealSeedPrice;
                case SeedSink.Skip: return SkipSeedPrice;
                case SeedSink.CanoesRefill: return CanoesRefillSeedPrice;
                case SeedSink.ContinueCuia:
                    throw new ArgumentException("La Cuia de rescate se cotiza por tramo y por día (Wallet.QuoteRescue).", nameof(sink));
                default: throw new ArgumentOutOfRangeException(nameof(sink), sink, null);
            }
        }

        /// <summary>
        /// Precio base de la Cuia de rescate en el nivel <paramref name="level"/> (04 §6.2 <c>band(rc.rescue_price_bands,
        /// level)</c>): el del último tramo que empieza en ese nivel o antes; null antes de <see cref="RescueMinLevel"/> o del
        /// primer tramo.
        /// </summary>
        public int? RescueBasePrice(int level)
        {
            if (level < RescueMinLevel)
            {
                return null;
            }

            int? price = null;
            IReadOnlyList<PriceBand> bands = RescuePriceBands;
            for (int i = 0; i < bands.Count; i++)
            {
                if (bands[i].FromLevel <= level)
                {
                    price = bands[i].Price;
                }
            }

            return price;
        }

        /// <summary>Precio del primer rescate del día: <c>round(base × rescue_first_day_mult)</c> (04 §6.2), con redondeo al alza en .5.</summary>
        public int RescueFirstOfDayPrice(int basePrice)
        {
            return (int)Math.Round(basePrice * RescueFirstDayMult, MidpointRounding.AwayFromZero);
        }

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            Require(EcoRewardNormal >= 0 && EcoRewardHard >= 0 && EcoRewardVeryHard >= 0, "eco_reward_* no puede ser negativo");
            Require(AdsRvDoubleHarvestFromLevel >= 1 && RescueMinLevel >= 1, "los niveles empiezan en 1");
            Require(UndoRefillAmount >= 1, "undo_refill_amount debe ser >= 1");
            Require(UndoRefillSeedPrice > 0 && ExtraCuiaSeedPrice > 0 && HintSeedPrice > 0 && ShuffleSeedPrice > 0
                && RevealSeedPrice > 0 && SkipSeedPrice > 0 && CanoesRefillSeedPrice > 0, "los precios deben ser > 0");
            Require(RescueFirstDayMult > 0 && RescueFirstDayMult <= 1, "rescue_first_day_mult debe estar en (0, 1]");
            Require(CanoesMax >= 1 && CanoesRegenMin >= 1, "canoes_max y canoes_regen_min deben ser >= 1");
            IReadOnlyList<PriceBand> bands = RescuePriceBands;
            Require(bands != null && bands.Count > 0, "rescue_price_bands no puede estar vacío");
            for (int i = 0; i < bands!.Count; i++)
            {
                Require(bands[i].FromLevel >= 1 && bands[i].Price > 0, "tramo de rescate inválido");
                Require(i == 0 || bands[i].FromLevel > bands[i - 1].FromLevel, "rescue_price_bands debe ir en orden creciente");
            }
        }

        /// <summary>Escribe los tramos con la sintaxis de 08 §9, p. ej. <c>[[31,600]]</c>.</summary>
        public static string FormatBands(IReadOnlyList<PriceBand> bands)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < bands.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('[').Append(bands[i].FromLevel.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(bands[i].Price.ToString(CultureInfo.InvariantCulture)).Append(']');
            }

            return sb.Append(']').ToString();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("EconomyConfig: " + message + ".");
            }
        }
    }
}
