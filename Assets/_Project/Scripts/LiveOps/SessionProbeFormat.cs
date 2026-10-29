#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.LiveOps
{
    /// <summary>
    /// Líneas de la sonda de desarrollo de B6 (logcat de los builds de desarrollo, como <c>DailyProbeFormat</c>; no son eventos de
    /// 08 §8.2, aunque <c>comeback_gift</c>, <c>push_prompt</c>, <c>push_sent</c> y <c>push_open</c> reflejan <c>af_comeback_gift</c>,
    /// <c>af_push_prompt</c>, <c>af_push_sent</c> y <c>af_push_open</c> sin el prefijo, hasta que exista <c>AF.Analytics</c>; los
    /// demás son solo de la sonda [P]). <c>AF_STATE {"event":…,"t":…,…}</c>, una línea JSON con los campos en orden fijo, solo
    /// claves, números e identificadores. Sin motor, para probarlo en EditMode.
    /// <list type="bullet">
    /// <item><c>session</c>: <c>n</c> (<c>stats.sessions</c>), <c>kind</c> (<c>cold</c> o <c>resume</c>) y <c>away_s</c>;</item>
    /// <item><c>comeback_gift</c>: <c>days_away</c>, <c>segment</c>, <c>granted</c>, <c>seeds</c>, <c>extra_cuia</c>,
    /// <c>canoes_added</c>, <c>reminder</c> y <c>block</c>, solo si el segmento recibe regalo;</item>
    /// <item><c>push_prompt</c>: <c>step</c>, <c>result</c> y <c>origin</c>;</item>
    /// <item><c>push_plan</c>: <c>count</c> y <c>first</c> / <c>first_at</c> (el primero programado, hora local) al pasar a segundo
    /// plano, con <c>allowed</c>;</item>
    /// <item><c>push_sent</c> (uno por entregado: <c>type</c>, <c>id</c>) al abrir y <c>push_open</c> (<c>type</c> y <c>dest</c>, el
    /// destino tomado: <c>daily</c>, <c>logbook</c>, <c>home</c> o <c>none</c>; D-062 [P]) cuando el aviso tocado se encamina al
    /// llegar al inicio, o se descarta.</item>
    /// </list>
    /// </summary>
    public static class SessionProbeFormat
    {
        private const string Prefix = "AF_STATE ";

        /// <summary>Línea <c>session</c>.</summary>
        public static string Session(long t, in SessionStart start)
        {
            StringBuilder sb = Begin("session", t);
            sb.Append(",\"n\":").Append(start.Number.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"kind\":\"").Append(start.Kind == SessionStartKind.ColdStart ? "cold" : "resume").Append('"');
            sb.Append(",\"away_s\":").Append(((long)start.Away.TotalSeconds).ToString(CultureInfo.InvariantCulture));
            return sb.Append('}').ToString();
        }

        /// <summary>Línea <c>comeback_gift</c>, o null si el segmento no recibe regalo.</summary>
        public static string? Comeback(long t, ComebackOutcome outcome)
        {
            if (outcome == null || !ComebackRules.GetsGift(outcome.Segment))
            {
                return null;
            }

            StringBuilder sb = Begin("comeback_gift", t);
            sb.Append(",\"days_away\":").Append(outcome.DaysAway.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"segment\":\"").Append(SegmentId(outcome.Segment)).Append('"');
            sb.Append(",\"granted\":").Append(outcome.Granted ? "true" : "false");
            sb.Append(",\"seeds\":").Append(outcome.Seeds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"extra_cuia\":").Append(outcome.ExtraCuia.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"canoes_added\":").Append(outcome.CanoesAdded.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"reminder\":").Append(outcome.ShowReminder ? "true" : "false");
            sb.Append(",\"block\":\"").Append(outcome.Block == ComebackBlock.ClockDrift ? "clock_drift" : "none").Append('"');
            return sb.Append('}').ToString();
        }

        /// <summary>Línea <c>push_prompt</c>.</summary>
        public static string Prompt(long t, in PushPromptEvent e)
        {
            StringBuilder sb = Begin("push_prompt", t);
            sb.Append(",\"step\":\"").Append(e.Step).Append('"');
            sb.Append(",\"result\":\"").Append(e.Result).Append('"');
            sb.Append(",\"origin\":\"").Append(e.Origin).Append('"');
            return sb.Append('}').ToString();
        }

        /// <summary>Línea <c>push_plan</c>.</summary>
        public static string Plan(long t, bool allowed, IReadOnlyList<PlannedNotification> plan)
        {
            StringBuilder sb = Begin("push_plan", t);
            sb.Append(",\"allowed\":").Append(allowed ? "true" : "false");
            sb.Append(",\"count\":").Append(plan.Count.ToString(CultureInfo.InvariantCulture));
            if (plan.Count > 0)
            {
                sb.Append(",\"first\":\"").Append(plan[0].Type.ToId()).Append('"');
                sb.Append(",\"first_at\":\"").Append(plan[0].FireAtLocal.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture)).Append('"');
            }
            else
            {
                sb.Append(",\"first\":null,\"first_at\":null");
            }

            return sb.Append('}').ToString();
        }

        /// <summary>
        /// Líneas <c>push_sent</c> de <paramref name="report"/>. La de <c>push_open</c> sale aparte (<see cref="PushOpen"/>), cuando
        /// se sabe a qué destino llevó el aviso (D-062).
        /// </summary>
        public static void Open(long t, NotificationOpenReport report, List<string> into)
        {
            if (report == null || into == null)
            {
                return;
            }

            for (int i = 0; i < report.Delivered.Count; i++)
            {
                StringBuilder sb = Begin("push_sent", t);
                sb.Append(",\"type\":\"").Append(report.Delivered[i].Type).Append('"');
                sb.Append(",\"id\":").Append(report.Delivered[i].Id.ToString(CultureInfo.InvariantCulture));
                into.Add(sb.Append('}').ToString());
            }
        }

        /// <summary>
        /// Línea <c>push_open</c>: el <c>type</c> del aviso tocado y el destino tomado (<c>dest</c>: <see cref="NotificationRouting.IdOf"/>
        /// o <see cref="NotificationRouting.DroppedId"/>; D-062 [P]).
        /// </summary>
        public static string PushOpen(long t, string type, string dest)
        {
            return Begin("push_open", t).Append(",\"type\":\"").Append(type).Append("\",\"dest\":\"").Append(dest).Append("\"}").ToString();
        }

        /// <summary>Identificador de <see cref="ComebackSegment"/> (<c>none</c>, <c>at_risk</c>, <c>dormant</c>, <c>lost</c>, <c>churn</c>).</summary>
        public static string SegmentId(ComebackSegment segment)
        {
            switch (segment)
            {
                case ComebackSegment.AtRisk: return "at_risk";
                case ComebackSegment.Dormant: return "dormant";
                case ComebackSegment.Lost: return "lost";
                case ComebackSegment.Churn: return "churn";
                default: return "none";
            }
        }

        private static StringBuilder Begin(string name, long t)
        {
            var sb = new StringBuilder(160);
            sb.Append(Prefix).Append("{\"event\":\"").Append(name).Append("\",\"t\":").Append(t.ToString(CultureInfo.InvariantCulture));
            return sb;
        }
    }
}
