#nullable enable
using System;
using System.Collections.Generic;
using AF.LiveOps;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la Liga del río (D-046; <c>docs/dev/liga.md</c> §1): solo claves de <c>strings.csv</c> con sus marcadores con nombre.
    /// El seudónimo llega como clave de animal (<c>league_name_*</c>) y cifras; una clave que este cliente no conoce se pinta como
    /// «Jugador {n}». El país, por su código ISO con <c>league_country_xx</c>; un país sin fila en la tabla se nombra con su código.
    /// </summary>
    public static class LeagueText
    {
        public const string ButtonKey = "league_button";
        public const string TitleKey = "league_title";
        public const string TabGlobalKey = "league_tab_global";
        public const string WeekKey = "league_week";
        public const string EndsInKey = "league_ends_in";
        public const string UpdatedAgoKey = "league_updated_ago";
        public const string OfflineKey = "league_offline";
        public const string LoadingKey = "league_loading";
        public const string EmptyWeekKey = "league_empty_week";
        public const string YouKey = "league_you";
        public const string PercentileKey = "league_me_percentile";
        public const string NoPointsKey = "league_me_no_points";
        public const string VictoryPointsKey = "league_victory_points";
        public const string SettingsCountryKey = "league_settings_country";
        public const string PlayerNameKey = "league_player_name";
        public const string PlayerFallbackKey = "league_player_fallback";
        public const string RowA11yKey = "league_row_a11y";
        public const string RowPlayerKey = "league_row_player";
        public const string MeRankA11yKey = "league_me_rank_a11y";
        public const string MePercentileA11yKey = "league_me_percentile_a11y";
        public const string PointsKey = "league_points";
        public const string TimeDaysKey = "league_time_days";
        public const string TimeHoursKey = "league_time_hours";
        public const string TimeMinutesKey = "league_time_minutes";
        public const string TimeNowKey = "league_time_now";
        public const string CountryPrefix = "league_country_";

        /// <summary>Los países con bandera dibujada (los amazónicos y los mercados principales; liga.md §4).</summary>
        public static readonly string[] FlagCountries = { "BR", "CO", "PE", "VE", "EC", "BO", "GY", "SR", "MX", "US", "AR", "CL" };

        /// <summary>Los países con nombre en la tabla (los de bandera y algunos más) [P].</summary>
        public static readonly string[] NamedCountries =
        {
            "AR", "BO", "BR", "CA", "CL", "CO", "DE", "EC", "ES", "FR", "GB", "GF", "GY", "MX", "PE", "PT", "PY", "SR", "US", "UY", "VE",
        };

        /// <summary>Todas las claves de <c>strings.csv</c> que pinta o dice la Liga: las de la UI, los 24 animales y los países.</summary>
        public static IReadOnlyList<string> AllKeys
        {
            get
            {
                var keys = new List<string>
                {
                    ButtonKey, TitleKey, TabGlobalKey, WeekKey, EndsInKey, UpdatedAgoKey, OfflineKey, LoadingKey, EmptyWeekKey, YouKey,
                    PercentileKey, NoPointsKey, VictoryPointsKey, SettingsCountryKey, PlayerNameKey, PlayerFallbackKey, RowA11yKey,
                    RowPlayerKey, MeRankA11yKey, MePercentileA11yKey, PointsKey, TimeDaysKey, TimeHoursKey, TimeMinutesKey, TimeNowKey,
                };
                keys.AddRange(LeagueRules.NameKeys);
                for (int i = 0; i < NamedCountries.Length; i++)
                {
                    keys.Add(CountryKey(NamedCountries[i]));
                }

                return keys;
            }
        }

        public static string Button => Loc.T(ButtonKey);

        public static string Title => Loc.T(TitleKey);

        public static string TabGlobal => Loc.T(TabGlobalKey);

        public static string Offline => Loc.T(OfflineKey);

        public static string Loading => Loc.T(LoadingKey);

        public static string EmptyWeek => Loc.T(EmptyWeekKey);

        public static string You => Loc.T(YouKey);

        public static string NoPoints => Loc.T(NoPointsKey);

        /// <summary>«Capibara 4821» o, con un animal que este cliente no conoce, «Jugador 4821».</summary>
        public static string Player(string? nameKey, int nameNum)
        {
            int n = Math.Max(0, nameNum);
            if (LeagueRules.IsKnownName(nameKey))
            {
                return Loc.T(PlayerNameKey, LocArgs.Of("animal", Loc.T(nameKey!)).AndDigits("n", n));
            }

            return Loc.T(PlayerFallbackKey, LocArgs.OfDigits("n", n));
        }

        /// <summary>Nombre del país <paramref name="code"/> (ISO 3166-1 alfa-2) o, sin fila en la tabla, el propio código.</summary>
        public static string Country(string code)
        {
            string key = CountryKey(code);
            return Loc.Has(key) ? Loc.T(key) : code;
        }

        /// <summary><c>league_country_xx</c> de <paramref name="code"/> (en minúsculas por la forma de las claves; no es texto visible).</summary>
        public static string CountryKey(string code)
        {
            if (!LeagueRules.IsCountryCode(code))
            {
                throw new ArgumentException("Código ISO 3166-1 alfa-2 en mayúsculas.", nameof(code));
            }

            return CountryPrefix + new string(new[] { (char)(code[0] + 32), (char)(code[1] + 32) });
        }

        /// <summary>«Semana del {inicio} al {fin}» (fechas cortas, en UTC: el cambio es a las 12:00 UTC).</summary>
        public static string Week(DateTime startsAt, DateTime endsAt)
        {
            return Loc.T(WeekKey, LocArgs.Of("start", DailyText.DateShort(startsAt.Date)).And("end", DailyText.DateShort(endsAt.Date)));
        }

        /// <summary>«Termina en {tiempo}».</summary>
        public static string EndsIn(TimeSpan left)
        {
            return Loc.T(EndsInKey, LocArgs.Of("time", Duration(left)));
        }

        /// <summary>«Actualizado hace {tiempo}».</summary>
        public static string UpdatedAgo(TimeSpan ago)
        {
            return Loc.T(UpdatedAgoKey, LocArgs.Of("time", Duration(ago)));
        }

        /// <summary>
        /// Un plazo: «2 d 3 h», «5 h 12 min», «8 min» o «un momento» (menos de un minuto). Las cifras sin separador (nunca llegan a mil).
        /// </summary>
        public static string Duration(TimeSpan span)
        {
            long minutes = Math.Max(0L, (long)Math.Floor(span.TotalMinutes));
            if (minutes < 1)
            {
                return Loc.T(TimeNowKey);
            }

            long days = minutes / (24 * 60);
            long hours = (minutes / 60) % 24;
            long mins = minutes % 60;
            if (days > 0)
            {
                return Loc.T(TimeDaysKey, LocArgs.OfDigits("d", days).AndDigits("h", hours));
            }

            if (hours > 0)
            {
                return Loc.T(TimeHoursKey, LocArgs.OfDigits("h", hours).AndDigits("m", mins));
            }

            return Loc.T(TimeMinutesKey, LocArgs.OfDigits("m", mins));
        }

        /// <summary>«Estás en el {p} % mejor».</summary>
        public static string Percentile(int p)
        {
            return Loc.T(PercentileKey, LocArgs.OfDigits("p", Math.Max(1, Math.Min(100, p))));
        }

        /// <summary>Los puntos de una fila (cifra con el separador de la región).</summary>
        public static string PointsNumber(int points)
        {
            return Loc.Number(Math.Max(0, points));
        }

        /// <summary>«{n} puntos» (con su plural; para el lector de pantalla).</summary>
        public static string Points(int points)
        {
            return Loc.T(PointsKey, LocArgs.Of("n", Math.Max(0, points)));
        }

        /// <summary>«+{n} puntos de Liga» (la línea de la victoria).</summary>
        public static string VictoryPoints(int points)
        {
            return Loc.T(VictoryPointsKey, LocArgs.Of("n", Math.Max(0, points)));
        }

        /// <summary>«País en la Liga: {país}» (Ajustes).</summary>
        public static string SettingsCountry(string code)
        {
            return Loc.T(SettingsCountryKey, LocArgs.Of("country", Country(code)));
        }

        /// <summary>Lo que lee TalkBack de una fila del top: posición, seudónimo, país (en Global) y puntos.</summary>
        public static string RowA11y(LeagueRow row, bool withCountry)
        {
            return Loc.T(RowA11yKey, LocArgs.OfDigits("rank", row.Rank).And("player", PlayerWithCountry(row.NameKey, row.NameNum, withCountry ? row.Country : null))
                .And("n", Math.Max(0, row.Points)));
        }

        /// <summary>Lo que lee TalkBack de la fila del jugador (con su posición o su percentil).</summary>
        public static string MeA11y(LeagueMe me, bool withCountry)
        {
            string player = PlayerWithCountry(me.NameKey, me.NameNum, withCountry ? me.Country : null);
            if (me.Rank.HasValue)
            {
                return Loc.T(MeRankA11yKey, LocArgs.OfDigits("rank", me.Rank.Value).And("player", player).And("n", Math.Max(0, me.Points)));
            }

            if (me.Percentile.HasValue)
            {
                return Loc.T(MePercentileA11yKey, LocArgs.OfDigits("p", me.Percentile.Value).And("player", player).And("n", Math.Max(0, me.Points)));
            }

            return NoPoints;
        }

        private static string PlayerWithCountry(string? nameKey, int nameNum, string? country)
        {
            string player = Player(nameKey, nameNum);
            return country != null && LeagueRules.IsCountryCode(country)
                ? Loc.T(RowPlayerKey, LocArgs.Of("player", player).And("country", Country(country)))
                : player;
        }
    }
}
