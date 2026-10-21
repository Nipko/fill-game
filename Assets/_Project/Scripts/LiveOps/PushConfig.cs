#nullable enable
using System;
using System.Globalization;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Horas silenciosas de <c>push_quiet_hours</c> (08 §9: <c>"22-09"</c>; 06 §8.2: 22:00-09:00 hora local del dispositivo), en horas
    /// enteras <c>"HH-HH"</c>. La franja puede cruzar la medianoche (inicio mayor que fin) o no; inicio igual a fin es sin silencio.
    /// </summary>
    public readonly struct QuietHours : IEquatable<QuietHours>
    {
        /// <summary>Crea la franja.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Alguna hora sale de 0-23.</exception>
        public QuietHours(int startHour, int endHour)
        {
            if (startHour < 0 || startHour > 23 || endHour < 0 || endHour > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(startHour), "Las horas silenciosas van de 0 a 23.");
            }

            StartHour = startHour;
            EndHour = endHour;
        }

        /// <summary>El default de 08 §9, 22-09.</summary>
        public static QuietHours Default => new QuietHours(22, 9);

        /// <summary>Hora local en que empieza el silencio.</summary>
        public int StartHour { get; }

        /// <summary>Hora local en que acaba (la primera hora en que ya se puede avisar).</summary>
        public int EndHour { get; }

        /// <summary>La hora local <paramref name="local"/> cae en el silencio.</summary>
        public bool Contains(DateTime local)
        {
            if (StartHour == EndHour)
            {
                return false;
            }

            int h = local.Hour;
            return StartHour > EndHour ? h >= StartHour || h < EndHour : h >= StartHour && h < EndHour;
        }

        /// <summary>
        /// La primera hora local en o después de <paramref name="local"/> fuera del silencio: la misma si ya está fuera, o el final de
        /// la franja (<see cref="EndHour"/>:00) si cae dentro.
        /// </summary>
        public DateTime NextAllowed(DateTime local)
        {
            if (!Contains(local))
            {
                return local;
            }

            DateTime end = local.Date.AddHours(EndHour);
            return end > local ? end : end.AddDays(1);
        }

        /// <summary>Escribe la franja con la sintaxis de 08 §9: <c>22-09</c>.</summary>
        public string Format()
        {
            return StartHour.ToString("00", CultureInfo.InvariantCulture) + "-" + EndHour.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>Lee <c>"HH-HH"</c> sin lanzar; si no vale, deja <see cref="Default"/> y devuelve false.</summary>
        public static bool TryParse(string? text, out QuietHours result)
        {
            result = Default;
            if (text == null || text.Length != 5 || text[2] != '-')
            {
                return false;
            }

            if (!int.TryParse(text.Substring(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int start)
                || !int.TryParse(text.Substring(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int end)
                || start > 23 || end > 23)
            {
                return false;
            }

            result = new QuietHours(start, end);
            return true;
        }

        /// <inheritdoc />
        public bool Equals(QuietHours other)
        {
            return StartHour == other.StartHour && EndHour == other.EndHour;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is QuietHours other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StartHour * 24 + EndHour;
        }
    }

    /// <summary>
    /// Cifras del permiso y de la cadencia de las notificaciones (06 §8.1-§8.2) con los defaults compilados de 08 §9 y sus nombres
    /// exactos (nunca el alias retirado <c>push_preprompt_level</c>). Las demás cifras que usan los avisos tienen su fuente única en
    /// otro módulo y se leen de allí: <c>streak_reminder_hours</c> y <c>streak_unlock_level</c> (<c>StreakConfig</c>),
    /// <c>live_unlock_level_daily</c> (<see cref="DailyChallengeConfig"/>), <c>rescue_min_level</c>, <c>canoes_max</c> y
    /// <c>canoes_regen_min</c> (<c>EconomyConfig</c>) y <c>live_content_end_message</c> (<c>GameConfig</c>).
    /// </summary>
    public sealed class PushConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static PushConfig Defaults => new PushConfig();

        /// <summary><c>push_preprompt_session</c>: sesión desde la que sale el pre-prompt (2; rango 2-3).</summary>
        [RemoteConfigKey("push_preprompt_session")]
        public int PushPrepromptSession { get; set; } = 2;

        /// <summary><c>push_preprompt_min_level</c>: nivel que ya tiene que estar superado (10; rango 8-12).</summary>
        [RemoteConfigKey("push_preprompt_min_level")]
        public int PushPrepromptMinLevel { get; set; } = 10;

        /// <summary><c>push_quiet_hours</c>: horas silenciosas, hora local del dispositivo (<c>"22-09"</c>).</summary>
        [RemoteConfigKey("push_quiet_hours")]
        public string PushQuietHours { get; set; } = "22-09";

        /// <summary><c>push_daily_cap_d0_d7</c>: avisos por día como mucho (1; 06 §8.2).</summary>
        [RemoteConfigKey("push_daily_cap_d0_d7")]
        public int PushDailyCapD0D7 { get; set; } = 1;

        /// <summary><c>push_weekly_cap_lapsed</c>: avisos por semana como mucho tras 7 días sin abrir (3; 06 §8.2: «2-3/semana»).</summary>
        [RemoteConfigKey("push_weekly_cap_lapsed")]
        public int PushWeeklyCapLapsed { get; set; } = 3;

        /// <summary>Las horas silenciosas leídas de <see cref="PushQuietHours"/> (las de 08 §9 si el valor no vale).</summary>
        public QuietHours QuietHours
        {
            get
            {
                QuietHours.TryParse(PushQuietHours, out QuietHours result);
                return result;
            }
        }

        /// <summary>Comprueba que los valores tienen sentido.</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            Require(PushPrepromptSession >= 2 && PushPrepromptSession <= 3, "push_preprompt_session debe estar en 2-3 (08 §9)");
            Require(PushPrepromptMinLevel >= 8 && PushPrepromptMinLevel <= 12, "push_preprompt_min_level debe estar en 8-12 (08 §9)");
            Require(QuietHours.TryParse(PushQuietHours, out _), "push_quiet_hours debe ser \"HH-HH\"");
            Require(PushDailyCapD0D7 >= 1, "push_daily_cap_d0_d7 debe ser >= 1");
            Require(PushWeeklyCapLapsed >= 1 && PushWeeklyCapLapsed <= 7, "push_weekly_cap_lapsed debe estar en 1-7");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("PushConfig: " + message + ".");
            }
        }
    }
}
