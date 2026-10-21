#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.IAP;

namespace AF.UI
{
    /// <summary>Pestañas del Mercado (06 §4.7: «Semillas · Comodines · Cuias (skins) · Sin anuncios»), en ese orden.</summary>
    public enum MarketTab
    {
        /// <summary>Semillas: el Cántaro y los packs <c>af_coins_2</c> … <c>af_coins_6</c>.</summary>
        Seeds = 0,

        /// <summary>Comodines: la Mochila de inicio (2 Cuia extra + 3 Morpho + 500) y las Canoas infinitas 1 h.</summary>
        Boosters = 1,

        /// <summary>Cuias: las skins, para elegir la puesta (Q-M8 (a)) y, con el arte, comprarlas (Q-B5-1).</summary>
        Cuias = 2,

        /// <summary>Sin anuncios.</summary>
        NoAds = 3,
    }

    /// <summary>Qué hace el botón de una fila del Mercado.</summary>
    public enum MarketAction
    {
        /// <summary>Sin botón (fila informativa).</summary>
        None = 0,

        /// <summary>Comprar el SKU de la fila, con su precio local.</summary>
        Buy = 1,

        /// <summary>Comprar, pero aún no se puede (Cántaro por debajo de <c>eco_piggy_min_buy</c>): el botón, desactivado.</summary>
        BuyLocked = 2,

        /// <summary>Poner la skin de la fila.</summary>
        Equip = 3,

        /// <summary>La skin de la fila es la puesta (botón desactivado).</summary>
        Equipped = 4,
    }

    /// <summary>Una fila del Mercado: el producto o la skin, lo que enseña y su botón.</summary>
    public readonly struct MarketRow
    {
        /// <summary>Crea la fila.</summary>
        public MarketRow(string id, MarketAction action, int number = 0, int number2 = 0)
        {
            Id = id;
            Action = action;
            Number = number;
            Number2 = number2;
        }

        /// <summary>SKU (<c>af_*</c>) o, en la pestaña Cuias, el id de la skin (<c>cuia_vidrio</c>, <c>af_skin_m02</c>…).</summary>
        public string Id { get; }

        /// <summary>Botón.</summary>
        public MarketAction Action { get; }

        /// <summary>
        /// Cifra de la línea: semillas del pack; semillas del Cántaro; horas que le quedan a la Mochila (0 sin temporizador aún);
        /// niveles que faltan para ganar una skin (0 si se tiene).
        /// </summary>
        public int Number { get; }

        /// <summary>Segunda cifra: el tope del Cántaro.</summary>
        public int Number2 { get; }
    }

    /// <summary>
    /// Qué enseña el Mercado (bloque B5; 05 §4.1 y §5; 06 §4.7), sin motor: las pestañas visibles y las filas de cada una, con lo
    /// que decide <see cref="OfferScheduler"/> (ventana de la Mochila, Sin anuncios desde el 22, Canoas desde el 31, Cántaro desde
    /// el 20, venta de skins) y lo que el jugador ya tiene (<see cref="SkinService"/>). La Balsa de rescate no se vende aquí: solo
    /// en Aguas estancadas (05 §4.1). «Restaurar compras» queda oculto hasta el adaptador real de IAP (Q-E5 (a) de D-033).
    /// </summary>
    public static class MarketModel
    {
        /// <summary>
        /// El Mercado se abre desde aquí: <c>max_level</c> ≥ 10 (06 §2.2, fila 10, y §4.7: «desde el nivel 10»; 01 §7.1; 05 §2.1).
        /// </summary>
        public const int FromMaxLevel = 10;

        /// <summary>Packs de semillas en el orden de 05 §4.1.</summary>
        public static readonly string[] SeedPacks = { "af_coins_2", "af_coins_3", "af_coins_4", "af_coins_5", "af_coins_6" };

        /// <summary>Pestañas en el orden de 06 §4.7.</summary>
        public static readonly MarketTab[] AllTabs = { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds };

        /// <summary>La pestaña <paramref name="tab"/> tiene algo que enseñar (Semillas y Cuias, siempre).</summary>
        public static bool TabVisible(MarketTab tab, OfferScheduler offers, int maxLevel)
        {
            if (offers == null)
            {
                throw new ArgumentNullException(nameof(offers));
            }

            switch (tab)
            {
                case MarketTab.Boosters:
                    return offers.StarterInMarket(maxLevel) || offers.CanoesInMarket(maxLevel);
                case MarketTab.NoAds:
                    return offers.NoAdsOwned || offers.NoAdsInMarket(maxLevel);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Las filas de <paramref name="tab"/> en <paramref name="rows"/> (que se vacía antes). <paramref name="currentWorld"/> es el
        /// mundo del nivel en juego (<c>max_level</c> + 1).
        /// </summary>
        public static void Build(MarketTab tab, OfferScheduler offers, SkinService? skins, int maxLevel, int currentWorld, List<MarketRow> rows)
        {
            if (offers == null)
            {
                throw new ArgumentNullException(nameof(offers));
            }

            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            rows.Clear();
            StoreCatalog catalog = offers.Ledger.Catalog;
            switch (tab)
            {
                case MarketTab.Seeds:
                    if (offers.PiggyInMarket(maxLevel))
                    {
                        PiggyJar piggy = offers.Piggy;
                        rows.Add(new MarketRow(StoreCatalog.Piggy, piggy.CanBuy ? MarketAction.Buy : MarketAction.BuyLocked, piggy.Seeds, piggy.Cap));
                    }

                    for (int i = 0; i < SeedPacks.Length; i++)
                    {
                        rows.Add(new MarketRow(SeedPacks[i], MarketAction.Buy, catalog.Get(SeedPacks[i]).Content.Seeds));
                    }

                    break;
                case MarketTab.Boosters:
                    if (offers.StarterInMarket(maxLevel))
                    {
                        rows.Add(new MarketRow(StoreCatalog.Starter, MarketAction.Buy, offers.StarterHoursLeft));
                    }

                    if (offers.CanoesInMarket(maxLevel))
                    {
                        rows.Add(new MarketRow(StoreCatalog.InfiniteCanoes1h, MarketAction.Buy,
                            (int)catalog.Get(StoreCatalog.InfiniteCanoes1h).Content.InfiniteCanoes.TotalMinutes));
                    }

                    break;
                case MarketTab.Cuias:
                    string equipped = skins?.Equipped ?? SkinRewards.BaseSkinId;
                    rows.Add(SkinRow(SkinRewards.BaseSkinId, equipped, true));
                    IReadOnlyList<SkinMilestone> table = SkinRewards.Milestones;
                    for (int i = 0; i < table.Count; i++)
                    {
                        SkinMilestone m = table[i];
                        bool owned = skins != null && skins.Owns(m.SkinId);
                        if (owned)
                        {
                            rows.Add(SkinRow(m.SkinId, equipped, true));
                        }
                        else if (offers.SkinPurchasable(m, maxLevel, currentWorld))
                        {
                            rows.Add(new MarketRow(m.SkinId, MarketAction.Buy, Math.Max(0, m.Level - maxLevel)));
                        }
                        else
                        {
                            rows.Add(new MarketRow(m.SkinId, MarketAction.None, Math.Max(0, m.Level - maxLevel)));
                        }
                    }

                    break;
                case MarketTab.NoAds:
                    if (offers.NoAdsOwned)
                    {
                        rows.Add(new MarketRow(StoreCatalog.NoAds, MarketAction.None));
                    }
                    else if (offers.NoAdsInMarket(maxLevel))
                    {
                        rows.Add(new MarketRow(StoreCatalog.NoAds, MarketAction.Buy, catalog.Get(StoreCatalog.NoAds).Content.Seeds));
                    }

                    break;
            }
        }

        private static MarketRow SkinRow(string id, string equipped, bool owned)
        {
            if (!owned)
            {
                return new MarketRow(id, MarketAction.None);
            }

            return new MarketRow(id, string.Equals(id, equipped, StringComparison.Ordinal) ? MarketAction.Equipped : MarketAction.Equip);
        }
    }
}
