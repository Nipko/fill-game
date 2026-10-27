#nullable enable
using System;
using System.Collections.Generic;
using AF.LiveOps;
using AF.Localization;

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

        /// <summary>«Tu avatar».</summary>
        public static string Title => Loc.T(TitleKey);

        /// <summary>«Avatar» (la fila de Ajustes).</summary>
        public static string SettingsRow => Loc.T(SettingsRowKey);

        /// <summary>«Nuevo».</summary>
        public static string New => Loc.T(NewKey);

        /// <summary>El nombre del animal (el del seudónimo de la Liga).</summary>
        public static string Name(AvatarInfo avatar) => Loc.T(avatar.NameKey);

        /// <summary>«Tu avatar: Tucán. Toca para cambiarlo» y, con el punto, «Hay un avatar nuevo» (dos frases de la tabla, cada una entera).</summary>
        public static string HomeA11y(AvatarInfo avatar, bool dot)
        {
            string open = Loc.T(HomeA11yKey, LocArgs.Of("animal", Name(avatar)));
            return dot ? open + "\n" + Loc.T(NewDotA11yKey) : open;
        }

        /// <summary>El texto corto del hito bajo una celda bloqueada («Muy difícil», «Mundo 4 · Trapecio»…), o «» si es libre.</summary>
        public static string GoalShort(AvatarInfo avatar)
        {
            switch (avatar.Milestone)
            {
                case AvatarMilestone.HardWin: return Loc.T(GoalPrefix + "hard");
                case AvatarMilestone.VeryHardWin: return Loc.T(GoalPrefix + "very_hard");
                case AvatarMilestone.World3:
                case AvatarMilestone.World4:
                case AvatarMilestone.World5:
                case AvatarMilestone.World6:
                {
                    int world = AvatarCatalog.WorldOf(avatar.Milestone);
                    return Loc.T(GoalPrefix + "world", LocArgs.OfDigits("n", world).And("world", UiText.WorldName(world)));
                }

                case AvatarMilestone.Streak10: return Loc.T(GoalPrefix + "streak_10", LocArgs.OfDigits("n", AvatarRules.StreakWins));
                case AvatarMilestone.DailyStreak7: return Loc.T(GoalPrefix + "daily_streak_7", LocArgs.Of("n", AvatarRules.DailyStreakDays));
                case AvatarMilestone.DailyStamps20: return Loc.T(GoalPrefix + "daily_stamps_20", LocArgs.Of("n", AvatarRules.DailyStamps));
                case AvatarMilestone.League50: return Loc.T(GoalPrefix + "league_50", LocArgs.Of("n", AvatarRules.LeaguePoints));
                default: return string.Empty;
            }
        }

        /// <summary>La frase entera del hito (el globo y TalkBack): «Gana tu primer nivel Muy difícil.», «Completa Trapecio (te faltan 23 niveles).»…</summary>
        public static string GoalLong(AvatarInfo avatar, int maxLevel)
        {
            switch (avatar.Milestone)
            {
                case AvatarMilestone.HardWin: return Loc.T(GoalPrefix + "hard" + GoalLongSuffix);
                case AvatarMilestone.VeryHardWin: return Loc.T(GoalPrefix + "very_hard" + GoalLongSuffix);
                case AvatarMilestone.World3:
                case AvatarMilestone.World4:
                case AvatarMilestone.World5:
                case AvatarMilestone.World6:
                {
                    int world = AvatarCatalog.WorldOf(avatar.Milestone);
                    int left = Math.Max(1, AvatarRules.LevelsLeft(avatar.Milestone, maxLevel));
                    return Loc.T(GoalPrefix + "world" + GoalLongSuffix, LocArgs.Of("n", left).And("world", UiText.WorldName(world)));
                }

                case AvatarMilestone.Streak10: return Loc.T(GoalPrefix + "streak_10" + GoalLongSuffix, LocArgs.Of("n", AvatarRules.StreakWins));
                case AvatarMilestone.DailyStreak7: return Loc.T(GoalPrefix + "daily_streak_7" + GoalLongSuffix, LocArgs.Of("n", AvatarRules.DailyStreakDays));
                case AvatarMilestone.DailyStamps20: return Loc.T(GoalPrefix + "daily_stamps_20" + GoalLongSuffix, LocArgs.Of("n", AvatarRules.DailyStamps));
                case AvatarMilestone.League50: return Loc.T(GoalPrefix + "league_50" + GoalLongSuffix, LocArgs.Of("n", AvatarRules.LeaguePoints));
                default: return string.Empty;
            }
        }

        /// <summary>Lo que lee TalkBack de una celda: elegida, nueva, bloqueada (con el hito entero) o libre.</summary>
        public static string CellA11y(AvatarInfo avatar, bool chosen, bool isNew, bool locked, int maxLevel)
        {
            string name = Name(avatar);
            if (locked)
            {
                return Loc.T(LockedA11yKey, LocArgs.Of("animal", name).And("goal", GoalLong(avatar, maxLevel)));
            }

            if (chosen)
            {
                return Loc.T(ChosenA11yKey, LocArgs.Of("animal", name));
            }

            return Loc.T(isNew ? CellNewA11yKey : CellA11yKey, LocArgs.Of("animal", name));
        }

        /// <summary>«En la Liga: Capibara 4821».</summary>
        public static string LeagueAlias(string nameKey, int nameNum) => Loc.T(LeagueAliasKey, LocArgs.Of("player", LeagueText.Player(nameKey, nameNum)));

        /// <summary>La cabecera para TalkBack: «Tucán. En la Liga: Capibara 4821, Colombia» (sin seudónimo, solo el animal).</summary>
        public static string HeaderA11y(AvatarInfo avatar, string? nameKey, int nameNum, string? country)
        {
            if (nameKey == null)
            {
                return Name(avatar);
            }

            string player = LeagueText.Player(nameKey, nameNum);
            if (country != null)
            {
                player = Loc.T(LeagueText.RowPlayerKey, LocArgs.Of("player", player).And("country", LeagueText.Country(country)));
            }

            return Loc.T(HeaderA11yKey, LocArgs.Of("animal", Name(avatar)).And("player", player));
        }

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
