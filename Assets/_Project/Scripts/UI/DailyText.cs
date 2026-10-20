#nullable enable
using System;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del Reto del río (tarea R9; nota <c>docs/dev/reto-del-rio.md</c> §8; 06 §4.1 «Pantalla»): solo claves y sus
    /// argumentos con nombre, con <see cref="Loc"/>. Las claves nuevas aún <b>no</b> están en <c>strings.csv</c>: van en
    /// <c>docs/dev/traspaso/spark-strings-reto.csv</c> y el integrador las copia (enganche E5). No es un parcial de
    /// <c>UiText</c> porque los <c>UiText*.cs</c> están reservados a la ola 7 (encargo §3.2).
    /// <para>
    /// <b>Clave ausente (fallback documentado):</b> es el comportamiento de <see cref="Loc.T(string)"/>: «[clave]» en desarrollo y
    /// «» en release, sin excepciones. Las pantallas del Reto no dependen del texto para funcionar (los botones y sus áreas
    /// táctiles tienen tamaño fijo, y la tarjeta y el calendario se componen igual con «[clave]» o vacío); en release la
    /// tarjeta sigue oculta hasta que el pool llegue a la escena (enganche E3), y E5 entra con él. Las claves que ya existen
    /// (<c>play</c>, <c>home</c>, <c>double_harvest</c>) se reutilizan con su mismo sentido.
    /// </para>
    /// </summary>
    public static class DailyText
    {
        /// <summary>Claves del Reto (las nuevas, en <c>spark-strings-reto.csv</c>).</summary>
        public static class Keys
        {
            /// <summary>Nombre del sistema (glosario §10, ID <c>daily_challenge</c>; aún no está en <c>strings.csv</c>): cabecera del tablero del Reto.</summary>
            public const string Title = "daily_challenge";

            /// <summary>«Reto del río · {date}»: título de la tarjeta del inicio y del calendario.</summary>
            public const string CardTitle = "daily_card_title";

            /// <summary>«{day} {month}»: fecha corta; {month} es <see cref="MonthShort"/>.</summary>
            public const string DateShort = "daily_date_short";

            /// <summary>«{n}/{total}»: racha mostrada (nota §7.3).</summary>
            public const string Streak = "daily_streak";

            /// <summary>Estado En curso: «Seguir» [P].</summary>
            public const string Continue = "daily_card_continue";

            /// <summary>Estado Sellado: «Sellado».</summary>
            public const string Sealed = "daily_card_sealed";

            /// <summary>Estado Pendiente de validar.</summary>
            public const string Pending = "daily_card_pending";

            /// <summary>Estado Jugar (clave existente <c>play</c>).</summary>
            public const string Play = "play";

            /// <summary>«+{n}»: semillas del premio en el calendario.</summary>
            public const string RewardSeeds = "daily_reward_seeds";

            /// <summary>Premio de la racha de 7: «+1 Pororoca».</summary>
            public const string StreakReward = "daily_streak_reward";

            /// <summary>Premio de la racha antes del nivel que desbloquea la Pororoca (R-Q14 a).</summary>
            public const string StreakRewardLocked = "daily_streak_reward_locked";

            /// <summary>Victoria sin premio (fecha ya cobrada o reloj atrasado; 06 §4.1).</summary>
            public const string NoReward = "daily_win_no_reward";

            /// <summary>Victoria con el premio pendiente de validar (08 §10.1).</summary>
            public const string WinPending = "daily_win_pending";

            /// <summary>Volver al inicio desde el calendario (clave existente <c>home</c>).</summary>
            public const string Home = "home";

            /// <summary>Cosecha doble (clave existente <c>double_harvest</c>), oculta hasta el enganche E9.</summary>
            public const string DoubleHarvest = "double_harvest";

            /// <summary>Presentación del Reto tras ganar el 7: «Desde el nivel {level}, un tablero nuevo cada día» (E6; <see cref="DailyTeaserCard"/>).</summary>
            public const string Teaser = DailyTeaserCard.TextKey;

            /// <summary>Línea de Sin canoas: «Mientras esperas: Jugar el Reto del río» (E7; <see cref="NoCanoesDailyLink"/>).</summary>
            public const string NoCanoesDaily = NoCanoesDailyLink.TextKey;

            /// <summary>Prefijo de los meses abreviados: <c>month_short_01</c> … <c>month_short_12</c>.</summary>
            public const string MonthShortPrefix = "month_short_";
        }

        private static readonly string[] MonthKeys =
        {
            "month_short_01", "month_short_02", "month_short_03", "month_short_04", "month_short_05", "month_short_06",
            "month_short_07", "month_short_08", "month_short_09", "month_short_10", "month_short_11", "month_short_12",
        };

        /// <summary>Las claves nuevas del Reto ya integradas en <c>strings.csv</c> (enganche E5; filas en <c>spark-strings-reto.csv</c>).</summary>
        public static readonly string[] NewKeys =
        {
            Keys.Title, Keys.CardTitle, Keys.DateShort, Keys.Streak, Keys.Continue, Keys.Sealed, Keys.Pending, Keys.RewardSeeds,
            Keys.StreakReward, Keys.StreakRewardLocked, Keys.NoReward, Keys.WinPending,
            "month_short_01", "month_short_02", "month_short_03", "month_short_04", "month_short_05", "month_short_06",
            "month_short_07", "month_short_08", "month_short_09", "month_short_10", "month_short_11", "month_short_12",
        };

        /// <summary>
        /// Claves de E6 y E7 (la tarjeta del 7 y la línea de Sin canoas), con sus filas en <c>spark-strings-reto.csv</c>, que entran
        /// en <c>strings.csv</c> con el enganche E5b (<c>docs/dev/traspaso/spark-enganches/E5b.diff</c>, que también las suma a
        /// <c>LocTablesTests.UiTextKeys</c>). Cuando E5b esté integrado, pasan a <see cref="NewKeys"/>.
        /// </summary>
        public static readonly string[] E5bKeys = { Keys.Teaser, Keys.NoCanoesDaily };

        /// <summary>Claves que ya existen en <c>strings.csv</c> y se reutilizan con su mismo sentido.</summary>
        public static readonly string[] ReusedKeys = { Keys.Play, Keys.Home, Keys.DoubleHarvest };

        /// <summary>Clave del mes abreviado (1-12).</summary>
        public static string MonthShortKey(int month)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, "Mes 1-12.");
            }

            return MonthKeys[month - 1];
        }

        /// <summary>«Reto del río» (cabecera del tablero).</summary>
        public static string Title => Loc.T(Keys.Title);

        /// <summary>
        /// Fecha corta de <paramref name="date"/> con el orden y el mes del idioma (08 §2.2: el formato de la región vía
        /// <see cref="Loc"/>): <c>daily_date_short</c> con <c>{day}</c> (cifras de la región) y <c>{month}</c>.
        /// </summary>
        public static string DateShort(DateTime date)
        {
            return Loc.T(Keys.DateShort, LocArgs.OfDigits("day", date.Day).And("month", Loc.T(MonthShortKey(date.Month))));
        }

        /// <summary>«Reto del río · {fecha corta}».</summary>
        public static string CardTitle(DateTime date)
        {
            return Loc.T(Keys.CardTitle, LocArgs.Of("date", DateShort(date)));
        }

        /// <summary>«{n}/7».</summary>
        public static string Streak(int shown, int total)
        {
            return Loc.T(Keys.Streak, LocArgs.OfDigits("n", shown).AndDigits("total", total));
        }

        /// <summary>«+{n}» semillas.</summary>
        public static string RewardSeeds(int seeds)
        {
            return Loc.T(Keys.RewardSeeds, LocArgs.Of("n", seeds));
        }

        /// <summary>«Inicio» (clave existente <c>home</c>).</summary>
        public static string HomeLabel => Loc.T(Keys.Home);

        /// <summary>«Cosecha doble: x2 por video» (clave existente <c>double_harvest</c>).</summary>
        public static string DoubleHarvestLabel => Loc.T(Keys.DoubleHarvest);

        /// <summary>Victoria con el premio pendiente de validar.</summary>
        public static string WinPendingLabel => Loc.T(Keys.WinPending);

        /// <summary>Victoria sin premio.</summary>
        public static string NoRewardLabel => Loc.T(Keys.NoReward);

        /// <summary>«+1 Pororoca» de la racha de 7.</summary>
        public static string StreakRewardLabel => Loc.T(Keys.StreakReward);

        /// <summary>«+1 Pororoca · se desbloquea en el nivel {level}» (R-Q14 a).</summary>
        public static string StreakRewardLocked(int level)
        {
            return Loc.T(Keys.StreakRewardLocked, LocArgs.OfDigits("level", level));
        }

        /// <summary>Texto del estado de la tarjeta.</summary>
        public static string State(AF.LiveOps.DailyCardState state)
        {
            switch (state)
            {
                case AF.LiveOps.DailyCardState.InProgress: return Loc.T(Keys.Continue);
                case AF.LiveOps.DailyCardState.PendingValidation: return Loc.T(Keys.Pending);
                case AF.LiveOps.DailyCardState.Sealed: return Loc.T(Keys.Sealed);
                case AF.LiveOps.DailyCardState.Play: return Loc.T(Keys.Play);
                default: return string.Empty;
            }
        }
    }
}
