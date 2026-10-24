#nullable enable
using System;
using System.Globalization;
using AF.Economy;

namespace AF.LiveOps
{
    /// <summary>
    /// Reglas de la Liga del río de la 1.0 (D-046; <c>docs/dev/liga.md</c> §1; 06 §6), puras y sin motor:
    /// <list type="bullet">
    /// <item>la semana va de lunes 12:00 UTC a lunes 12:00 UTC; su id es la semana ISO 8601 del lunes que la abre
    /// (<c>2026-W43</c> para la que empieza el 2026-10-19, como el ejemplo de liga.md §3.1);</item>
    /// <item>los puntos por victoria (Normal 10, Difícil 20, Muy difícil 30; Reto del río 20), <b>solo</b> para la línea de la
    /// victoria: el servidor los calcula con su tabla y manda (liga.md §3.2), y el cliente nunca los envía;</item>
    /// <item>el desbloqueo: la Liga aparece al ganar <c>live_league_unlock_level</c> (41);</item>
    /// <item>los <c>win_id</c> idempotentes: <c>c:&lt;nivel&gt;</c> (campaña, una vez por semana) y <c>d:&lt;día&gt;</c> (Reto, una
    /// vez por día).</item>
    /// </list>
    /// </summary>
    public static class LeagueRules
    {
        /// <summary>Victoria Normal (06 §6).</summary>
        public const int PointsNormal = 10;

        /// <summary>Victoria Difícil (06 §6).</summary>
        public const int PointsHard = 20;

        /// <summary>Victoria Muy difícil (06 §6).</summary>
        public const int PointsVeryHard = 30;

        /// <summary>Reto del río ganado (06 §6).</summary>
        public const int PointsDaily = 20;

        /// <summary>
        /// El nivel de desbloqueo que fija el servidor (<c>infra/functions/league/rules.ts</c>): por debajo, ninguna victoria suma y el
        /// cliente nunca enseña «+{n} puntos de Liga». <c>live_league_unlock_level</c> queda fijo en este valor (08 §9, rango 41-41).
        /// </summary>
        public const int ServerUnlockLevel = 41;

        /// <summary>Victorias por petición como mucho (liga.md §3.1).</summary>
        public const int MaxBatch = 50;

        /// <summary>Filas del top por ámbito como mucho (liga.md §3.1).</summary>
        public const int MaxTop = 100;

        /// <summary>Hora UTC del cambio de semana (06 §6: lunes 12:00 UTC).</summary>
        public const int ResetHourUtc = 12;

        /// <summary>Prefijo de las victorias de campaña.</summary>
        public const string CampaignPrefix = "c:";

        /// <summary>Prefijo de las victorias del Reto del río.</summary>
        public const string DailyPrefix = "d:";

        /// <summary>
        /// Los 24 animales del seudónimo, en el orden de liga.md §1 (índice 0-23). El servidor elige uno al crear el seudónimo y lo
        /// manda como clave; sus textos están en <c>strings.csv</c> (nacen <c>pendiente_asesor</c>).
        /// </summary>
        public static readonly string[] NameKeys =
        {
            "league_name_capybara", "league_name_jaguar", "league_name_toucan", "league_name_macaw", "league_name_boto",
            "league_name_sloth", "league_name_tapir", "league_name_otter", "league_name_manatee", "league_name_turtle",
            "league_name_howler", "league_name_squirrel_monkey", "league_name_caiman", "league_name_anaconda",
            "league_name_dart_frog", "league_name_morpho", "league_name_hummingbird", "league_name_heron", "league_name_kingfisher",
            "league_name_anteater", "league_name_armadillo", "league_name_ocelot", "league_name_coati", "league_name_arapaima",
        };

        /// <summary>Puntos de una victoria de campaña de la dificultad <paramref name="tier"/> (para la línea de la victoria).</summary>
        public static int CampaignPoints(RewardTier tier)
        {
            switch (tier)
            {
                case RewardTier.Hard: return PointsHard;
                case RewardTier.VeryHard: return PointsVeryHard;
                default: return PointsNormal;
            }
        }

        /// <summary>La Liga está desbloqueada con <paramref name="maxLevel"/> ganado.</summary>
        public static bool IsUnlocked(int maxLevel, LeagueConfig config)
        {
            return maxLevel >= (config ?? throw new ArgumentNullException(nameof(config))).LiveLeagueUnlockLevel;
        }

        /// <summary>El inicio de la semana de <paramref name="utc"/>: el último lunes a las 12:00 UTC que no es posterior.</summary>
        public static DateTime WeekStart(DateTime utc)
        {
            DateTime t = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            int sinceMonday = ((int)t.DayOfWeek + 6) % 7; // lunes = 0
            DateTime monday = new DateTime(t.Year, t.Month, t.Day, ResetHourUtc, 0, 0, DateTimeKind.Utc).AddDays(-sinceMonday);
            return monday > t ? monday.AddDays(-7) : monday;
        }

        /// <summary>El fin de la semana que empieza en <paramref name="weekStart"/> (el lunes siguiente a las 12:00 UTC).</summary>
        public static DateTime WeekEnd(DateTime weekStart)
        {
            return weekStart.AddDays(7);
        }

        /// <summary>Id de la semana de <paramref name="utc"/>: <c>yyyy-Www</c>, la semana ISO 8601 del lunes que la abre.</summary>
        public static string WeekId(DateTime utc)
        {
            DateTime monday = WeekStart(utc).Date;
            // ISO 8601: la semana de un lunes es la del jueves de esa semana; su año es el del jueves.
            DateTime thursday = monday.AddDays(3);
            int week = ((thursday.DayOfYear - 1) / 7) + 1;
            return thursday.Year.ToString("D4", CultureInfo.InvariantCulture) + "-W" + week.ToString("D2", CultureInfo.InvariantCulture);
        }

        /// <summary><c>win_id</c> de una victoria de campaña: <c>c:57</c>.</summary>
        public static string CampaignWinId(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return CampaignPrefix + level.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary><c>win_id</c> del Reto del río de <paramref name="dayKey"/> (<c>yyyy-MM-dd</c>): <c>d:2026-10-26</c>.</summary>
        public static string DailyWinId(string dayKey)
        {
            if (!IsDayKey(dayKey))
            {
                throw new ArgumentException("Día del Reto fuera de formato (yyyy-MM-dd).", nameof(dayKey));
            }

            return DailyPrefix + dayKey;
        }

        /// <summary><paramref name="value"/> es una fecha <c>yyyy-MM-dd</c> válida.</summary>
        public static bool IsDayKey(string? value)
        {
            return value != null && value.Length == 10
                && DateTime.TryParseExact(value, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary><paramref name="key"/> es uno de los 24 animales que conoce este cliente.</summary>
        public static bool IsKnownName(string? key)
        {
            return key != null && Array.IndexOf(NameKeys, key) >= 0;
        }

        /// <summary><paramref name="code"/> tiene forma de ISO 3166-1 alfa-2 (dos letras mayúsculas).</summary>
        public static bool IsCountryCode(string? code)
        {
            return code != null && code.Length == 2 && code[0] >= 'A' && code[0] <= 'Z' && code[1] >= 'A' && code[1] <= 'Z';
        }

        /// <summary>La región del teléfono como ISO 3166-1 alfa-2 en mayúsculas, o null si no tiene esa forma.</summary>
        public static string? NormalizeRegion(string? region)
        {
            if (region == null || region.Length != 2)
            {
                return null;
            }

            char a = Upper(region[0]);
            char b = Upper(region[1]);
            return a >= 'A' && a <= 'Z' && b >= 'A' && b <= 'Z' ? new string(new[] { a, b }) : null;
        }

        /// <summary>Hora UTC en el formato del contrato: <c>2026-10-26T15:04:05Z</c>.</summary>
        public static string FormatUtc(DateTime utc)
        {
            DateTime t = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc;
            return t.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>Lee una hora UTC ISO 8601 (<c>…Z</c>, con fracción o sin ella); false si no lo es.</summary>
        public static bool TryParseUtc(string? text, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(text) || text![text.Length - 1] != 'Z')
            {
                return false;
            }

            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
            {
                return false;
            }

            utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        private static char Upper(char c)
        {
            return c >= 'a' && c <= 'z' ? (char)(c - 32) : c;
        }
    }
}
