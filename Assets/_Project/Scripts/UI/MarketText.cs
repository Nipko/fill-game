#nullable enable
using AF.IAP;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del Mercado, de las tarjetas de oferta y del Cántaro (bloque B5; 05 §4.1 y §5; 06 §4.7; glosario §10): solo claves y
    /// sus argumentos con nombre, con <see cref="Loc"/>. Las claves nuevas aún <b>no</b> están en <c>strings.csv</c>: las mete el
    /// parche <c>docs/dev/traspaso/spark-enganches/B5-loc.diff</c> (con su registro en <c>LocTablesTests</c>). <c>shop</c>,
    /// <c>currency</c> y <c>no_ads</c> son del glosario §10 (con su EN y su PT-BR); el resto, [P] y <c>pendiente_asesor</c>. Los
    /// nombres de tienda son los de siempre (<c>iap_&lt;sku&gt;_name</c>) y los de las skins, <c>skin_&lt;id&gt;_name</c>.
    /// <para><b>Clave ausente:</b> <see cref="Loc.T(string)"/> devuelve «[clave]» en desarrollo y «» en release, sin excepciones.</para>
    /// </summary>
    public static class MarketText
    {
        /// <summary>Claves.</summary>
        public static class Keys
        {
            /// <summary>«Mercado» (glosario §10, <c>shop</c>): título de la pantalla.</summary>
            public const string Title = "shop";

            /// <summary>«Semillas» (glosario §10, <c>currency</c>): pestaña de los packs y del Cántaro.</summary>
            public const string TabSeeds = "currency";

            /// <summary>«Comodines» [P]: pestaña de la Mochila y de las Canoas infinitas (06 §4.7).</summary>
            public const string TabBoosters = "market_tab_boosters";

            /// <summary>«Cuias» [P]: pestaña de las skins (06 §4.7: «Cuias (skins)»).</summary>
            public const string TabCuias = "market_tab_cuias";

            /// <summary>«Sin anuncios» (glosario §10, <c>no_ads</c>): pestaña.</summary>
            public const string TabNoAds = "no_ads";

            /// <summary>«{n} semillas» (plural): contenido de un pack (05 §4.1).</summary>
            public const string SeedsAmount = "market_seeds_amount";

            /// <summary>Contenido de la Mochila de inicio con <c>{seeds}</c>, <c>{extra}</c> y <c>{hints}</c> (05 §4.1).</summary>
            public const string StarterContent = "market_starter_content";

            /// <summary>«Quedan {h} horas» (plural): el temporizador de la Mochila (05 §4.1).</summary>
            public const string StarterLeft = "market_starter_left";

            /// <summary>Contenido de Sin anuncios con <c>{n}</c> semillas de bono (05 §4.1; 05 §10: los videos siguen, opcionales).</summary>
            public const string NoAdsContent = "market_noads_content";

            /// <summary>Sin anuncios ya comprado (la pestaña lo dice en lugar de venderlo) [P].</summary>
            public const string NoAdsOwned = "market_noads_owned";

            /// <summary>«{seeds} de {cap} semillas»: lo que guarda el Cántaro (05 §4.1: «visible, 1.000-3.000»).</summary>
            public const string PiggyContent = "market_piggy_content";

            /// <summary>«Se puede comprar desde {n} semillas» (05 §4.1: «comprable desde 1.000»).</summary>
            public const string PiggyLocked = "market_piggy_locked";

            /// <summary>«¡Lleno!» (05 §4.1, literal).</summary>
            public const string PiggyFull = "market_piggy_full";

            /// <summary>«Se llena con cada victoria» [P] (05 §4.1: «se llena +15/+30/+40 por victoria»).</summary>
            public const string PiggyFills = "market_piggy_fills";

            /// <summary>«{n} min de Canoas infinitas» (05 §4.1: 60 min).</summary>
            public const string CanoesContent = "market_canoes_content";

            /// <summary>«Poner» [P]: elegir la skin en la pestaña Cuias (D-033, Q-M8 (a)).</summary>
            public const string SkinEquip = "market_skin_equip";

            /// <summary>«Puesta» [P]: la skin que llevan las cuias.</summary>
            public const string SkinEquipped = "market_skin_equipped";

            /// <summary>«Ahora no» [P]: cerrar una tarjeta de oferta sin comprar.</summary>
            public const string NotNow = "offer_not_now";

            /// <summary>«Premio del mundo · faltan {n}» (ya en la tabla, del Mapa del río; 06 §4.5): skin aún no ganada.</summary>
            public const string WorldPrize = "map_world_prize";
        }

        /// <summary>
        /// Las claves nuevas de B5 (las de <c>B5-loc.diff</c>), para que <c>LocTablesTests</c> las cuente como usadas (como
        /// <c>ShuffleText.AllKeys</c>).
        /// </summary>
        public static string[] AllKeys => new[]
        {
            Keys.Title, Keys.TabSeeds, Keys.TabBoosters, Keys.TabCuias, Keys.TabNoAds, Keys.SeedsAmount, Keys.StarterContent,
            Keys.StarterLeft, Keys.NoAdsContent, Keys.NoAdsOwned, Keys.PiggyContent, Keys.PiggyLocked, Keys.PiggyFull, Keys.PiggyFills,
            Keys.CanoesContent, Keys.SkinEquip, Keys.SkinEquipped, Keys.NotNow,
        };

        /// <summary>«Mercado».</summary>
        public static string Title => Loc.T(Keys.Title);

        /// <summary>Nombre de la pestaña <paramref name="tab"/>.</summary>
        public static string Tab(MarketTab tab)
        {
            switch (tab)
            {
                case MarketTab.Boosters: return Loc.T(Keys.TabBoosters);
                case MarketTab.Cuias: return Loc.T(Keys.TabCuias);
                case MarketTab.NoAds: return Loc.T(Keys.TabNoAds);
                default: return Loc.T(Keys.TabSeeds);
            }
        }

        /// <summary>Nombre de tienda del SKU (<c>iap_&lt;sku&gt;_name</c>).</summary>
        public static string ProductName(string sku)
        {
            return Loc.T(StoreProduct.NameKeyOf(sku));
        }

        /// <summary>Nombre de la skin (<c>skin_&lt;id&gt;_name</c>).</summary>
        public static string SkinName(string skinId)
        {
            return Loc.T("skin_" + skinId + "_name");
        }

        /// <summary>«{n} semillas».</summary>
        public static string SeedsAmount(int seeds)
        {
            return Loc.T(Keys.SeedsAmount, LocArgs.Of("n", seeds));
        }

        /// <summary>«500 semillas, 2 Cuia extra y 3 Morpho».</summary>
        public static string StarterContent(int seeds, int extraCuias, int hints)
        {
            return Loc.T(Keys.StarterContent, LocArgs.Of("seeds", seeds).And("extra", extraCuias).And("hints", hints));
        }

        /// <summary>«Quedan {h} horas».</summary>
        public static string StarterLeft(int hours)
        {
            return Loc.T(Keys.StarterLeft, LocArgs.Of("h", hours));
        }

        /// <summary>Contenido de Sin anuncios con su bono.</summary>
        public static string NoAdsContent(int bonusSeeds)
        {
            return Loc.T(Keys.NoAdsContent, LocArgs.Of("n", bonusSeeds));
        }

        /// <summary>Sin anuncios ya comprado.</summary>
        public static string NoAdsOwned => Loc.T(Keys.NoAdsOwned);

        /// <summary>«1.230 de 3.000 semillas».</summary>
        public static string PiggyContent(int seeds, int cap)
        {
            return Loc.T(Keys.PiggyContent, LocArgs.Of("seeds", seeds).And("cap", cap));
        }

        /// <summary>«Se puede comprar desde 1.000 semillas».</summary>
        public static string PiggyLocked(int minBuy)
        {
            return Loc.T(Keys.PiggyLocked, LocArgs.Of("n", minBuy));
        }

        /// <summary>«¡Lleno!».</summary>
        public static string PiggyFull => Loc.T(Keys.PiggyFull);

        /// <summary>«Se llena con cada victoria».</summary>
        public static string PiggyFills => Loc.T(Keys.PiggyFills);

        /// <summary>«60 min de Canoas infinitas».</summary>
        public static string CanoesContent(int minutes)
        {
            return Loc.T(Keys.CanoesContent, LocArgs.Of("n", minutes));
        }

        /// <summary>«Poner».</summary>
        public static string SkinEquip => Loc.T(Keys.SkinEquip);

        /// <summary>«Puesta».</summary>
        public static string SkinEquipped => Loc.T(Keys.SkinEquipped);

        /// <summary>«Ahora no».</summary>
        public static string NotNow => Loc.T(Keys.NotNow);

        /// <summary>«Premio del mundo · faltan {n}».</summary>
        public static string WorldPrize(int levelsLeft)
        {
            return Loc.T(Keys.WorldPrize, LocArgs.Of("n", levelsLeft));
        }
    }
}
