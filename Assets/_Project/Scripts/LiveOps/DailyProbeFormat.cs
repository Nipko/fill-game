#nullable enable
using System.Globalization;
using System.Text;

namespace AF.LiveOps
{
    /// <summary>
    /// Formato de las líneas <c>AF_STATE</c> del Reto del río (tarea R10; nota <c>docs/dev/reto-del-rio.md</c> §4 y §12; 08 §8.2).
    /// Sin motor, para probarlo en <c>AF.LiveOps.Tests</c>: <c>DevProbe.Daily.cs</c> (AF.UI) solo le pasa la hora de la sonda
    /// (<paramref name="t"/>, los milisegundos que <c>DevProbe</c> escribe en <c>t</c>) y escribe la línea con <c>DevProbe.Write</c>.
    /// Mismo esquema que el resto de <c>DevProbe</c>: <c>AF_STATE {"event":…,"t":…,…}</c>, JSON de una línea, solo claves e
    /// identificadores, nunca texto visible (regla de localización).
    /// <list type="bullet">
    /// <item><c>daily_challenge</c> (<c>af_daily_challenge</c>): <c>date</c>, <c>result</c> (<c>win</c> al reclamar; <c>fail</c>
    /// una vez por día sin victoria, R-Q15; <c>abandon</c> ya no se emite), <c>t_ms</c> (null si no se midió),
    /// <c>murky_bonus</c> = false y <c>streak_days</c>;</item>
    /// <item><c>level_start</c>: <c>level_name</c> = <c>d{b}_n{nnnn}</c>, <c>variant</c> = <c>base</c> (el Reto no tiene alivio)
    /// y <c>band</c> = la banda del Reto (1-3) [P];</item>
    /// <item><c>earn_virtual_currency</c>: <c>virtual_currency_name</c> = <c>seeds</c>, <c>source</c> = <c>daily_challenge</c>,
    /// <c>value</c>;</item>
    /// <item><c>daily_card</c>: el estado de la tarjeta del inicio (nota §8), para jugar con <c>adb</c>.</item>
    /// </list>
    /// </summary>
    public sealed class DailyProbeFormat
    {
        /// <summary>Valor de <c>source</c> de <c>earn_virtual_currency</c> para el premio del Reto (08 §8.2).</summary>
        public const string SeedSourceId = "daily_challenge";

        private readonly StringBuilder _sb = new StringBuilder(256);

        /// <summary>
        /// Línea <c>AF_STATE daily_challenge</c> (<c>af_daily_challenge</c> de 08 §8.2) con los valores dados.
        /// <paramref name="tMs"/> null o negativo sale como <c>null</c>.
        /// </summary>
        public string DailyChallenge(long t, string dateKey, DailyResult result, long? tMs, int streakDays)
        {
            StringBuilder sb = Begin("daily_challenge", t);
            sb.Append(",\"date\":");
            AppendId(sb, dateKey);
            sb.Append(",\"result\":\"").Append(result.ToId()).Append('"');
            sb.Append(",\"t_ms\":");
            if (tMs.HasValue && tMs.Value >= 0)
            {
                sb.Append(tMs.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append("null");
            }

            sb.Append(",\"murky_bonus\":false");
            sb.Append(",\"streak_days\":").Append(streakDays.ToString(CultureInfo.InvariantCulture));
            return End(sb);
        }

        /// <summary>
        /// <c>daily_challenge</c> con <c>result = win</c> tras <see cref="DailyChallengeService.ClaimWin"/>: la fecha reclamada y la
        /// racha tras el cobro (<see cref="DailyChallengeRules.LastClaim"/>). Null con <see cref="ClaimOutcome.NoReward"/> (rejugada
        /// de un día ya cobrado o reloj atrasado: no es la victoria de ese día) [P].
        /// </summary>
        public string? Win(long t, DailyClaimReport claim, long? tMs)
        {
            if (claim.Outcome == ClaimOutcome.NoReward)
            {
                return null;
            }

            return DailyChallenge(t, claim.DateKey, DailyResult.Win, tMs, claim.StreakDays);
        }

        /// <summary>
        /// <c>daily_challenge</c> con <c>result = fail</c> de un fin de día (<see cref="DailyChallengeService.DayEnded"/>; R-Q15),
        /// con <c>t_ms</c> = null (el registro no guarda el tiempo jugado). Null si no hay nada que informar
        /// (<see cref="DailyPlayEnd.IsReported"/>).
        /// </summary>
        public string? DayEnd(long t, DailyPlayEnd end)
        {
            return end.IsReported ? DailyChallenge(t, end.DateKey, end.Result, null, end.StreakDays) : null;
        }

        /// <summary>Línea <c>AF_STATE level_start</c> del Reto: <c>level_name</c> = <paramref name="boardId"/> (<c>d{b}_n{nnnn}</c>).</summary>
        public string LevelStart(long t, string boardId, int band)
        {
            StringBuilder sb = Begin("level_start", t);
            sb.Append(",\"level_name\":");
            AppendId(sb, boardId);
            sb.Append(",\"variant\":\"base\"");
            sb.Append(",\"band\":").Append(band.ToString(CultureInfo.InvariantCulture));
            return End(sb);
        }

        /// <summary>
        /// Línea <c>AF_STATE earn_virtual_currency</c> del premio del Reto (<c>source = daily_challenge</c>): las semillas pagadas
        /// (<see cref="DailyClaimReport.SeedsPaid"/> o <see cref="DailyReconcileResult.SeedsPaid"/>). Null si no se pagó nada.
        /// </summary>
        public string? EarnSeeds(long t, int value)
        {
            if (value <= 0)
            {
                return null;
            }

            StringBuilder sb = Begin("earn_virtual_currency", t);
            sb.Append(",\"virtual_currency_name\":\"seeds\"");
            sb.Append(",\"source\":\"").Append(SeedSourceId).Append('"');
            sb.Append(",\"value\":").Append(value.ToString(CultureInfo.InvariantCulture));
            return End(sb);
        }

        /// <summary>
        /// Línea <c>AF_STATE daily_card</c>: <c>state</c> (<see cref="CardStateId"/>), <c>date</c> (hoy, fecha local), <c>band</c> y
        /// <c>day</c> del slot de hoy (0 sin slot: tarjeta oculta), <c>board</c> (el tablero que se jugaría o se reanuda; null si no
        /// hay), <c>streak</c> (la «{n}/7» mostrada), <c>override</c> (el id que pedía <c>daily_overrides</c>, o null) y
        /// <c>override_ignored</c> (lo pedía pero no se usó).
        /// </summary>
        public string Card(long t, DailyCardState state, string dateKey, DailySlot? slot, string? boardId, int streakShown)
        {
            StringBuilder sb = Begin("daily_card", t);
            sb.Append(",\"state\":\"").Append(CardStateId(state)).Append('"');
            sb.Append(",\"date\":");
            AppendId(sb, dateKey);
            sb.Append(",\"band\":").Append((slot?.Band ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"day\":").Append((slot?.Day ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"board\":");
            AppendId(sb, boardId);
            sb.Append(",\"streak\":").Append(streakShown.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"override\":");
            AppendId(sb, slot?.RequestedOverrideId);
            sb.Append(",\"override_ignored\":").Append(slot.HasValue && slot.Value.IsOverrideIgnored ? "true" : "false");
            return End(sb);
        }

        /// <summary>Id del estado de la tarjeta en la sonda: <c>hidden</c>, <c>in_progress</c>, <c>pending_validation</c>, <c>sealed</c> o <c>play</c>.</summary>
        public static string CardStateId(DailyCardState state)
        {
            switch (state)
            {
                case DailyCardState.InProgress: return "in_progress";
                case DailyCardState.PendingValidation: return "pending_validation";
                case DailyCardState.Sealed: return "sealed";
                case DailyCardState.Play: return "play";
                default: return "hidden";
            }
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

        // Un identificador (fecha, id de tablero) como cadena JSON, o null; sin comillas ni barras, que ningún id válido lleva.
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
