#nullable enable
using System.Collections.Generic;

namespace AF.UI
{
    /// <summary>
    /// Los textos de los avatares del jugador (D-056; <c>docs/dev/avatares.md</c> §3, con Q-AV-1 a Q-AV-12 en la recomendada, D-057):
    /// la pantalla «Tu avatar», la fila de Ajustes, el retrato del inicio, las celdas de la rejilla, los hitos y su lectura de TalkBack.
    /// El nombre de cada animal es el de su seudónimo de la Liga (<c>league_name_*</c>, avatares.md §1.1), que ya está en
    /// <see cref="LeagueText.AllKeys"/>.
    /// </summary>
    public static partial class AvatarText
    {
        public const string TitleKey = "avatar_title";
        public const string SettingsRowKey = "avatar_settings_row";
        public const string HomeA11yKey = "avatar_home_a11y";
        public const string NewDotA11yKey = "avatar_new_dot_a11y";
        public const string NewKey = "avatar_new";
        public const string CellA11yKey = "avatar_cell_a11y";
        public const string ChosenA11yKey = "avatar_chosen_a11y";
        public const string CellNewA11yKey = "avatar_cell_new_a11y";
        public const string LockedA11yKey = "avatar_locked_a11y";
        public const string LeagueAliasKey = "avatar_league_alias";
        public const string HeaderA11yKey = "avatar_header_a11y";

        /// <summary>Prefijo de los textos de los hitos: <c>avatar_goal_&lt;hito&gt;</c> (corto) y <c>…_long</c> (la frase entera).</summary>
        public const string GoalPrefix = "avatar_goal_";

        /// <summary>Sufijo de la frase entera de un hito.</summary>
        public const string GoalLongSuffix = "_long";

        /// <summary>Los hitos con texto propio (los cuatro de mundo comparten <c>avatar_goal_world</c>).</summary>
        public static readonly string[] GoalIds =
        {
            "hard", "very_hard", "world", "streak_10", "daily_streak_7", "daily_stamps_20", "league_50",
        };

        /// <summary>Todas las claves de <c>strings.csv</c> que pinta o dice la pantalla de los avatares.</summary>
        public static IReadOnlyList<string> AllKeys
        {
            get
            {
                var keys = new List<string>
                {
                    TitleKey, SettingsRowKey, HomeA11yKey, NewDotA11yKey, NewKey, CellA11yKey, ChosenA11yKey, CellNewA11yKey,
                    LockedA11yKey, LeagueAliasKey, HeaderA11yKey,
                };
                for (int i = 0; i < GoalIds.Length; i++)
                {
                    keys.Add(GoalPrefix + GoalIds[i]);
                    keys.Add(GoalPrefix + GoalIds[i] + GoalLongSuffix);
                }

                return keys;
            }
        }
    }
}
