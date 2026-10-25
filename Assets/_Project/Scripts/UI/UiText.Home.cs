#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del inicio «el muelle del río» (D-050; 06 §4.7, fila Lobby): el letrero de la próxima parada, las frases nuevas del
    /// bocadillo de TiTino, las tarjetas del día del muelle y las etiquetas de TalkBack de la travesía, la Crecida y las tarjetas.
    /// Todos en <c>strings.csv</c> como borrador (08 §4.7; <c>.claude/rules/localizacion.md</c>).
    /// </summary>
    public static partial class UiText
    {
        /// <summary>«Faltan {n} niveles» de la píldora del letrero de la parada (<c>home_voyage_levels</c>).</summary>
        public static string HomeVoyageLevels(int levels)
        {
            return Format(Keys.HomeVoyageLevels, LocArgs.OfDigits("n", NonNegative(levels)));
        }

        /// <summary>
        /// Lo que lee TalkBack en la canoa y en la parada del inicio (<c>home_voyage_a11y</c>): la línea de la parada
        /// (<paramref name="line"/>, <c>map_stop_remaining</c>) y que abre el Mapa del río.
        /// </summary>
        public static string HomeVoyageA11y(string line)
        {
            return line.Length == 0 ? RiverMap : Format(Keys.HomeVoyageA11y, LocArgs.Of("line", line));
        }

        /// <summary>Bocadillo de TiTino con el Reto del río de hoy sin jugar (<c>home_sloth_daily</c>).</summary>
        public static string HomeSlothDaily => Loc.T(Keys.HomeSlothDaily);

        /// <summary>Bocadillo de TiTino con lo que falta para la próxima Canasta (<c>home_sloth_basket</c>).</summary>
        public static string HomeSlothBasket(int levels)
        {
            return Format(Keys.HomeSlothBasket, LocArgs.OfDigits("n", NonNegative(levels)));
        }

        /// <summary>Bocadillo de TiTino que anima a jugar (<c>home_sloth_play</c>).</summary>
        public static string HomeSlothPlay => Loc.T(Keys.HomeSlothPlay);

        /// <summary>Título de la tarjeta de la Bitácora en el muelle (<c>home_card_logbook</c>).</summary>
        public static string HomeCardLogbook => Loc.T(Keys.HomeCardLogbook);

        /// <summary>Estado de la tarjeta de la Bitácora con la página de hoy por sellar (<c>home_card_logbook_ready</c>).</summary>
        public static string HomeCardLogbookReady => Loc.T(Keys.HomeCardLogbookReady);

        /// <summary>Estado de la tarjeta de la Bitácora sin nivel jugado hoy (<c>home_card_logbook_play</c>).</summary>
        public static string HomeCardLogbookPlay => Loc.T(Keys.HomeCardLogbookPlay);

        /// <summary>Estado corto de la tarjeta del Reto con el premio pendiente de validar (<c>home_card_daily_pending</c>).</summary>
        public static string HomeCardDailyPending => Loc.T(Keys.HomeCardDailyPending);

        /// <summary>Puesto del jugador en la tarjeta de la Liga (<c>home_card_league_rank</c>).</summary>
        public static string HomeCardLeagueRank(int rank)
        {
            return Format(Keys.HomeCardLeagueRank, LocArgs.OfDigits("rank", NonNegative(rank)));
        }

        /// <summary>TalkBack de una tarjeta del día (<c>home_card_a11y</c>): su título y su estado.</summary>
        public static string HomeCardA11y(string title, string state)
        {
            return state.Length == 0 ? title : Format(Keys.HomeCardA11y, LocArgs.Of("title", title).And("state", state));
        }

        /// <summary>TalkBack de la tarjeta del Reto (<c>home_card_daily_a11y</c>): título con la fecha, estado y racha de 7.</summary>
        public static string HomeCardDailyA11y(string title, string state, int streak)
        {
            return Format(Keys.HomeCardDailyA11y, LocArgs.Of("title", title).And("state", state).AndDigits("n", NonNegative(streak)));
        }

        /// <summary>TalkBack del poste de la Crecida (<c>home_crecida_a11y</c> o, con la Hoja escudo, <c>home_crecida_shield_a11y</c>).</summary>
        public static string HomeCrecidaA11y(int wins, int total, bool shield)
        {
            return Format(shield ? Keys.HomeCrecidaShieldA11y : Keys.HomeCrecidaA11y, LocArgs.OfDigits("n", NonNegative(wins)).AndDigits("total", NonNegative(total)));
        }

        public static partial class Keys
        {
            /// <summary><c>home_voyage_levels</c> [P] (D-050).</summary>
            public const string HomeVoyageLevels = "home_voyage_levels";

            /// <summary><c>home_voyage_a11y</c> [P] (D-050).</summary>
            public const string HomeVoyageA11y = "home_voyage_a11y";

            /// <summary><c>home_sloth_daily</c> [P] (D-050).</summary>
            public const string HomeSlothDaily = "home_sloth_daily";

            /// <summary><c>home_sloth_basket</c> [P] (D-050).</summary>
            public const string HomeSlothBasket = "home_sloth_basket";

            /// <summary><c>home_sloth_play</c> [P] (D-050).</summary>
            public const string HomeSlothPlay = "home_sloth_play";

            /// <summary><c>home_card_logbook</c> [P] (D-050).</summary>
            public const string HomeCardLogbook = "home_card_logbook";

            /// <summary><c>home_card_logbook_ready</c> [P] (D-050).</summary>
            public const string HomeCardLogbookReady = "home_card_logbook_ready";

            /// <summary><c>home_card_logbook_play</c> [P] (D-050).</summary>
            public const string HomeCardLogbookPlay = "home_card_logbook_play";

            /// <summary><c>home_card_daily_pending</c> [P] (D-050).</summary>
            public const string HomeCardDailyPending = "home_card_daily_pending";

            /// <summary><c>home_card_league_rank</c> [P] (D-050).</summary>
            public const string HomeCardLeagueRank = "home_card_league_rank";

            /// <summary><c>home_card_a11y</c> [P] (D-050).</summary>
            public const string HomeCardA11y = "home_card_a11y";

            /// <summary><c>home_card_daily_a11y</c> [P] (D-050).</summary>
            public const string HomeCardDailyA11y = "home_card_daily_a11y";

            /// <summary><c>home_crecida_a11y</c> [P] (D-050).</summary>
            public const string HomeCrecidaA11y = "home_crecida_a11y";

            /// <summary><c>home_crecida_shield_a11y</c> [P] (D-050).</summary>
            public const string HomeCrecidaShieldA11y = "home_crecida_shield_a11y";
        }
    }
}
