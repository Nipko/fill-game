#nullable enable
using System.Globalization;
using System.Text;

namespace AF.LiveOps
{
    /// <summary>
    /// Formato de las líneas <c>AF_STATE</c> de la Bitácora del río (08 §8.2; D-033, añadido del 2026-10-20), hasta que exista
    /// <c>AF.Analytics</c> (B7). Sin motor, para probarlo en <c>AF.LiveOps.Tests</c>; <c>DevProbe.Logbook.cs</c> (AF.UI) solo le
    /// pasa la hora de la sonda y escribe la línea. Mismo esquema que el resto de <c>DevProbe</c>: una línea JSON, solo claves e
    /// identificadores, nunca texto visible.
    /// <list type="bullet">
    /// <item><c>calendar_claim</c> (<c>af_calendar_claim</c>): <c>day</c>, <c>reward_type</c>, <c>reward_value</c> y
    /// <c>days_missed_before</c> de 08 §8.2, más <c>choice</c> y <c>cycle</c> de D-033 [P]; y, solo en la sonda,
    /// <c>reward_json</c> y <c>extras_json</c> (la Canasta, la Canasta grande o el marco fijos de la casilla) [P].</item>
    /// <item><c>calendar_state</c> [P] (solo sonda): el estado de la libreta (<c>cause</c> ∈ {<c>start</c>, <c>played</c>,
    /// <c>day</c>, <c>claim</c>}), para jugar con <c>adb</c>.</item>
    /// <item><c>takeover</c> [P] (solo sonda): qué takeover salió al entrar al inicio, o por qué no (<c>kind</c>, <c>shown</c>,
    /// <c>session</c>).</item>
    /// </list>
    /// No es seguro entre hilos (un solo <see cref="StringBuilder"/>).
    /// </summary>
    public sealed class LoginCalendarProbeFormat
    {
        /// <summary><c>cause</c> de <c>calendar_state</c> al crear el servicio.</summary>
        public const string CauseStart = "start";

        /// <summary><c>cause</c> tras apuntar el primer nivel terminado del día.</summary>
        public const string CausePlayed = "played";

        /// <summary><c>cause</c> al cambiar el día local.</summary>
        public const string CauseDay = "day";

        /// <summary><c>cause</c> tras sellar una casilla.</summary>
        public const string CauseClaim = "claim";

        private readonly StringBuilder _sb = new StringBuilder(256);

        /// <summary>Línea <c>AF_STATE calendar_claim</c> (<c>af_calendar_claim</c>).</summary>
        public string Claim(long t, LoginCalendarClaim claim)
        {
            StringBuilder sb = Begin("calendar_claim", t);
            sb.Append(",\"day\":").Append(claim.Day.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"reward_type\":\"").Append(claim.RewardType).Append('"');
            sb.Append(",\"reward_value\":").Append(claim.RewardValue.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"days_missed_before\":").Append(claim.DaysMissedBefore.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"choice\":\"").Append(LoginCalendarTable.ChoiceId(claim.Choice)).Append('"');
            sb.Append(",\"cycle\":").Append(claim.Cycle.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"reward_json\":");
            AppendJsonString(sb, claim.Reward.ToJson());
            sb.Append(",\"extras_json\":");
            AppendJsonString(sb, ExtrasJson(claim));
            return End(sb);
        }

        /// <summary>Línea <c>AF_STATE calendar_state</c> [P].</summary>
        public string State(long t, string cause, LoginCalendarState state, int cycle, int nextDay, string? lastClaimDate, string? playedDate)
        {
            StringBuilder sb = Begin("calendar_state", t);
            sb.Append(",\"cause\":\"").Append(cause).Append('"');
            sb.Append(",\"state\":\"").Append(StateId(state)).Append('"');
            sb.Append(",\"cycle\":").Append(cycle.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"next_day\":").Append(nextDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"last_claim_date\":");
            AppendId(sb, lastClaimDate);
            sb.Append(",\"played_date\":");
            AppendId(sb, playedDate);
            return End(sb);
        }

        /// <summary>Línea <c>AF_STATE takeover</c> [P]: <paramref name="kind"/> es el id del takeover (o <c>none</c>).</summary>
        public string Takeover(long t, string kind, bool shown, int session, int shownThisSession, string source)
        {
            StringBuilder sb = Begin("takeover", t);
            sb.Append(",\"kind\":\"").Append(kind).Append('"');
            sb.Append(",\"shown\":").Append(shown ? "true" : "false");
            sb.Append(",\"session\":").Append(session.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"shown_this_session\":").Append(shownThisSession.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"source\":\"").Append(source).Append('"');
            return End(sb);
        }

        /// <summary>Id del estado: <c>hidden</c>, <c>waiting</c>, <c>ready</c> o <c>sealed</c>.</summary>
        public static string StateId(LoginCalendarState state)
        {
            switch (state)
            {
                case LoginCalendarState.Waiting: return "waiting";
                case LoginCalendarState.Ready: return "ready";
                case LoginCalendarState.Sealed: return "sealed";
                default: return "hidden";
            }
        }

        /// <summary>
        /// Lo fijo de la casilla como JSON [P]: <c>{}</c>, <c>{"basket":{…contents_json…}}</c> o
        /// <c>{"basket":{…},"frame":"frame_w01"}</c>, con el <c>contents_json</c> de <c>af_basket_open</c>.
        /// </summary>
        public static string ExtrasJson(LoginCalendarClaim claim)
        {
            var sb = new StringBuilder(96);
            sb.Append('{');
            bool first = true;
            if (!claim.Basket.IsNone)
            {
                sb.Append("\"basket\":").Append(claim.Basket.ContentsJson());
                first = false;
            }

            if (claim.FrameId != null)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                sb.Append("\"frame\":");
                AppendId(sb, claim.FrameId);
            }

            return sb.Append('}').ToString();
        }

        private StringBuilder Begin(string eventName, long t)
        {
            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(eventName).Append('"');
            sb.Append(",\"t\":").Append(t.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        private static string End(StringBuilder sb)
        {
            sb.Append('}');
            return sb.ToString();
        }

        // Una cadena JSON con las comillas y barras escapadas (los parámetros JSON de analítica van como cadena, como contents_json).
        private static void AppendJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            sb.Append('"');
        }

        private static void AppendId(StringBuilder sb, string? id)
        {
            if (id == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c != '"' && c != '\\' && c >= ' ')
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
        }
    }
}
