#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Platform;

namespace AF.IAP
{
    /// <summary>Semillas de un pack de <c>eco_seed_pack_amounts</c> (08 §9).</summary>
    public readonly struct SeedPack
    {
        /// <summary>Crea el pack.</summary>
        public SeedPack(string sku, int seeds)
        {
            Sku = sku;
            Seeds = seeds;
        }

        /// <summary>SKU (<c>af_coins_2</c> … <c>af_coins_6</c>).</summary>
        public string Sku { get; }

        /// <summary>Semillas.</summary>
        public int Seeds { get; }
    }

    /// <summary>
    /// Cifras de la tienda con los <b>defaults compilados</b> de la tabla única de 08 §9 y sus nombres exactos
    /// (<c>StoreCatalogTests</c> los contrasta con el GDD). Lo que el GDD fija sin clave (contenido de la Mochila, la Balsa,
    /// las Canoas infinitas y las skins; las 48 h de la Balsa) está en <see cref="StoreCatalog"/> y en
    /// <see cref="BalsaHideDuration"/>, citando 05 §4.1.
    /// <para>
    /// Desviación del plan (B1-4): no hay <c>Assets/_Project/Data/Config/iap_catalog.json</c>. El catálogo y sus cifras
    /// van compilados aquí y en <see cref="StoreCatalog"/>, porque AF.IAP no tiene un lector JSON público al alcance (el de
    /// AF.Levels queda fuera de sus dependencias de 08 §2.2 y el de AF.Save es interno) y no se añaden paquetes. Cuando
    /// exista <c>AF.RemoteConfig</c>, sus valores se copiarán en una instancia; hasta entonces se usa
    /// <see cref="Defaults"/>.
    /// </para>
    /// </summary>
    public sealed class StoreConfig
    {
        /// <summary>La Balsa de rescate se oculta 48 h tras <c>af_rescue_hide_after_declines</c> rechazos (05 §4.1 y §5; sin clave en 08 §9).</summary>
        public static readonly TimeSpan BalsaHideDuration = TimeSpan.FromHours(48);

        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static StoreConfig Defaults => new StoreConfig();

        /// <summary><c>biz_iap_enabled</c>: interruptor de la tienda.</summary>
        [RemoteConfigKey("biz_iap_enabled")]
        public bool BizIapEnabled { get; set; } = true;

        /// <summary>
        /// <c>eco_seed_pack_amounts</c>: semillas de cada pack (08 §9; <see cref="StoreCatalog"/> las toma de aquí).
        /// </summary>
        [RemoteConfigKey("eco_seed_pack_amounts")]
        public IReadOnlyList<SeedPack> EcoSeedPackAmounts { get; set; } = new[]
        {
            new SeedPack("af_coins_2", 1000),
            new SeedPack("af_coins_3", 2800),
            new SeedPack("af_coins_4", 6000),
            new SeedPack("af_coins_5", 13000),
            new SeedPack("af_coins_6", 36000),
        };

        /// <summary><c>eco_noads_bonus_seeds</c>: semillas que trae Sin anuncios.</summary>
        [RemoteConfigKey("eco_noads_bonus_seeds")]
        public int EcoNoadsBonusSeeds { get; set; } = 500;

        /// <summary><c>af_rescue_daily_cap</c>: impresiones de la Balsa de rescate al día.</summary>
        [RemoteConfigKey("af_rescue_daily_cap")]
        public int AfRescueDailyCap { get; set; } = 1;

        /// <summary><c>af_rescue_hide_after_declines</c>: rechazos que la ocultan 48 h.</summary>
        [RemoteConfigKey("af_rescue_hide_after_declines")]
        public int AfRescueHideAfterDeclines { get; set; } = 2;

        /// <summary><c>eco_piggy_min_buy</c>: el Cántaro se puede comprar desde estas semillas.</summary>
        [RemoteConfigKey("eco_piggy_min_buy")]
        public int EcoPiggyMinBuy { get; set; } = 1000;

        /// <summary><c>eco_piggy_cap</c>: tope del Cántaro.</summary>
        [RemoteConfigKey("eco_piggy_cap")]
        public int EcoPiggyCap { get; set; } = 3000;

        /// <summary>Semillas del pack <paramref name="sku"/>.</summary>
        /// <exception cref="ArgumentException">No es un pack de semillas.</exception>
        public int SeedPackAmount(string sku)
        {
            IReadOnlyList<SeedPack> packs = EcoSeedPackAmounts;
            for (int i = 0; i < packs.Count; i++)
            {
                if (string.Equals(packs[i].Sku, sku, StringComparison.Ordinal))
                {
                    return packs[i].Seeds;
                }
            }

            throw new ArgumentException("No es un pack de semillas de eco_seed_pack_amounts: " + sku, nameof(sku));
        }

        /// <summary>Comprueba los rangos de 08 §9.</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de su rango.</exception>
        public void Validate()
        {
            IReadOnlyList<SeedPack> packs = EcoSeedPackAmounts;
            Require(packs != null && packs.Count == 5, "eco_seed_pack_amounts lleva los 5 packs af_coins_2 … af_coins_6");
            for (int i = 0; i < packs!.Count; i++)
            {
                Require(packs[i].Sku == "af_coins_" + (i + 2).ToString(CultureInfo.InvariantCulture), "eco_seed_pack_amounts en orden af_coins_2 … af_coins_6");
                Require(packs[i].Seeds > 0, "eco_seed_pack_amounts: semillas > 0");
            }

            Require(EcoNoadsBonusSeeds >= 300 && EcoNoadsBonusSeeds <= 600, "eco_noads_bonus_seeds debe estar en 300-600");
            Require(AfRescueDailyCap == 1, "af_rescue_daily_cap: rango 1");
            Require(AfRescueHideAfterDeclines >= 2 && AfRescueHideAfterDeclines <= 3, "af_rescue_hide_after_declines debe estar en 2-3");
            Require(EcoPiggyCap >= 2000 && EcoPiggyCap <= 4000, "eco_piggy_cap debe estar en 2000-4000");
            Require(EcoPiggyMinBuy > 0 && EcoPiggyMinBuy <= EcoPiggyCap, "eco_piggy_min_buy <= eco_piggy_cap");
        }

        /// <summary>Escribe los packs con la sintaxis de 08 §9: <c>{"af_coins_2":1000,…}</c>.</summary>
        public static string FormatPacks(IReadOnlyList<SeedPack> packs)
        {
            var sb = new StringBuilder("{");
            for (int i = 0; i < packs.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('"').Append(packs[i].Sku).Append("\":").Append(packs[i].Seeds.ToString(CultureInfo.InvariantCulture));
            }

            return sb.Append('}').ToString();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("StoreConfig: " + message + ".");
            }
        }
    }
}
