#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.LiveOps
{
    /// <summary>
    /// Planificador puro de los avisos locales de la 1.0 (06 §8.2-§8.3 y §9; 08 §2.2 y Desviación 8): a partir del estado del
    /// jugador al pasar a segundo plano (<see cref="NotificationContext"/>), la lista de avisos que hay que programar si no vuelve a
    /// abrir la app. Al abrir se cancelan todos y al salir se vuelve a planificar («se programan al pasar a segundo plano y se
    /// cancelan al abrir», 06 §8.2), así que el plan supone que el jugador no vuelve.
    /// <para><b>Reglas</b> (las [P] están en la nota <c>docs/dev/sesiones-notificaciones.md</c> §4):</para>
    /// <list type="number">
    /// <item><b>Disparadores</b> (06 §8.3): Reto listo a las 09:30 de cada día (hoy solo si aún no son las 09:30 y no jugó; con el día
    /// 6 de 7 ganado ayer, la variante «Racha de Retos» [P]); Bitácora sin sellar a las 20:00 de cada día (hoy solo si no la selló);
    /// Crecida te espera a <c>streak_reminder_hours</c> de la última victoria con racha ≥ 3, una vez (si ya pasó, nada) [P]; Canoas
    /// llenas cuando las canoas llegan a su tope, solo si sale con 0; Nuevos niveles a <c>next_batch_eta_days</c> si terminó el
    /// contenido; Regreso a los 7, 14, 30 y 60 días.</item>
    /// <item><b>Horas silenciosas</b> (<c>push_quiet_hours</c>, 22-09 local): un aviso que cae dentro se mueve al final de la franja
    /// (09:00) [P].</item>
    /// <item><b>Un aviso por día</b> (<c>push_daily_cap_d0_d7</c> = 1): si coinciden, gana la prioridad de 06 §8.2 (Regreso primero
    /// [P]); el resto del día se descarta.</item>
    /// <item><b>Sin abrir 7 días</b> (<c>push_weekly_cap_lapsed</c> = 3): desde el día 7, como mucho 3 por semana, en días alternos
    /// [P] (el Regreso cuenta para el tope pero no espera al día alterno).</item>
    /// <item><b>Parar a los 30 días</b>, salvo el Regreso (06 §8.2).</item>
    /// </list>
    /// Determinista: sin azar ni el reloj del sistema; el mismo contexto da el mismo plan.
    /// </summary>
    public sealed class NotificationPlanner
    {
        /// <summary>Días del plan sin abrir la app tras los que se para (06 §8.2: «parar a los 30 días»).</summary>
        public const int StopAfterDays = 30;

        /// <summary>Desde este día sin abrir rige el tope semanal (06 §8.2: «si no abre en 7 días»).</summary>
        public const int LapsedFromDay = 7;

        /// <summary>Días del Regreso (06 §8.2 y 08 Desviación 8: D7/D14/D30; 06 §9 «Churn»: también a los 60 [P]).</summary>
        public static readonly IReadOnlyList<int> ComebackDays = Array.AsReadOnly(new[] { 7, 14, 30, 60 });

        /// <summary>Hora local de «Reto listo» (06 §8.3: 09:30-11:00; se usa el inicio de la franja [P]).</summary>
        public static readonly TimeSpan DailyReadyAt = new TimeSpan(9, 30, 0);

        /// <summary>Hora local de «Bitácora sin sellar» (06 §8.3: 20:00).</summary>
        public static readonly TimeSpan LogbookAt = new TimeSpan(20, 0, 0);

        /// <summary>Días de la racha de Retos que dan premio (7; <c>live_daily_streak7_reward</c>).</summary>
        public const int DailyStreakLength = DailyChallengeRules.StreakLength;

        private readonly PushConfig _config;

        /// <summary>Crea el planificador.</summary>
        public NotificationPlanner(PushConfig? config = null)
        {
            _config = config ?? PushConfig.Defaults;
            _config.Validate();
        }

        /// <summary>El plan para <paramref name="context"/>, ordenado por hora.</summary>
        public IReadOnlyList<PlannedNotification> Plan(NotificationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            QuietHours quiet = _config.QuietHours;
            DateTime nowLocal = DateTime.SpecifyKind(context.NowUtc + context.LocalOffset, DateTimeKind.Unspecified);
            DateTime today = nowLocal.Date;
            var candidates = new List<Candidate>();

            AddRecurring(context, nowLocal, today, candidates);
            AddOneOffs(context, nowLocal, candidates);

            // Horas silenciosas, y nada en el pasado.
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                Candidate c = candidates[i];
                DateTime local = quiet.NextAllowed(c.Local);
                if (local <= nowLocal)
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                int day = (int)(local.Date - today).TotalDays;
                if (day >= StopAfterDays && !c.Type.IsComeback())
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                candidates[i] = new Candidate(c.Type, local, c.Args, day, c.BodyKey);
            }

            // Prioridad dentro del día, luego hora y tipo (orden total: el plan no depende del orden de entrada).
            candidates.Sort((a, b) =>
            {
                if (a.Day != b.Day)
                {
                    return a.Day.CompareTo(b.Day);
                }

                int p = b.Type.Priority().CompareTo(a.Type.Priority());
                if (p != 0)
                {
                    return p;
                }

                return a.Local != b.Local ? a.Local.CompareTo(b.Local) : ((int)a.Type).CompareTo((int)b.Type);
            });

            var plan = new List<PlannedNotification>();
            int dayCap = _config.PushDailyCapD0D7;
            int weekCap = _config.PushWeeklyCapLapsed;
            int currentDay = -1;
            int usedToday = 0;
            int currentWeek = -1;
            int usedThisWeek = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate c = candidates[i];
                if (c.Day != currentDay)
                {
                    currentDay = c.Day;
                    usedToday = 0;
                }

                if (usedToday >= dayCap)
                {
                    continue;
                }

                if (c.Day >= LapsedFromDay)
                {
                    int week = (c.Day - LapsedFromDay) / 7;
                    if (week != currentWeek)
                    {
                        currentWeek = week;
                        usedThisWeek = 0;
                    }

                    if (usedThisWeek >= weekCap)
                    {
                        continue;
                    }

                    if (!c.Type.IsComeback() && (c.Day - LapsedFromDay) % 2 != 0)
                    {
                        continue; // días alternos sin abrir [P]
                    }

                    usedThisWeek++;
                }

                usedToday++;
                DateTime utc = DateTime.SpecifyKind(c.Local - context.LocalOffset, DateTimeKind.Utc);
                plan.Add(new PlannedNotification(c.Type, c.BodyKey, c.Day, utc, c.Local, c.Args));
            }

            plan.Sort((a, b) => a.FireAtUtc != b.FireAtUtc ? a.FireAtUtc.CompareTo(b.FireAtUtc) : a.Id.CompareTo(b.Id));
            return plan;
        }

        private static void AddRecurring(NotificationContext context, DateTime nowLocal, DateTime today, List<Candidate> candidates)
        {
            DateTime? lastWin = ParseDate(context.DailyLastWinDate);
            int streak = context.DailyStreakDays;
            for (int d = 0; d < StopAfterDays; d++)
            {
                DateTime date = today.AddDays(d);
                if (context.DailyUnlocked && (d > 0 || (!context.DailyPlayedToday && nowLocal < date + DailyReadyAt)))
                {
                    // Racha de Retos (06 §8.3, «Día 6 de 7»): el Reto de este día cerraría la racha de 7.
                    bool closesStreak = streak > 0 && streak % DailyStreakLength == DailyStreakLength - 1
                        && lastWin.HasValue && lastWin.Value == date.AddDays(-1);
                    candidates.Add(new Candidate(
                        closesStreak ? NotificationType.DailyStreak : NotificationType.DailyReady, date + DailyReadyAt, NoArgs, 0));
                }

                if (context.LogbookUnlocked && context.LogbookReward != null
                    && (d > 0 || (!context.LogbookClaimedToday && nowLocal < date + LogbookAt)))
                {
                    candidates.Add(new Candidate(
                        NotificationType.LogbookPending, date + LogbookAt, new[] { NotificationArg.Of("reward", context.LogbookReward) }, 0));
                }
            }
        }

        private static void AddOneOffs(NotificationContext context, DateTime nowLocal, List<Candidate> candidates)
        {
            TimeSpan offset = context.LocalOffset;
            if (context.StreakActive && context.StreakWins >= PushPrePromptRules.SecondAtStreakWins && context.LastWinAt.HasValue
                && context.StreakReminderHours > 0)
            {
                DateTime at = context.LastWinAt.Value + TimeSpan.FromHours(context.StreakReminderHours) + offset;
                int next = NextMilestone(context.StreakWins);
                if (next > 0)
                {
                    candidates.Add(new Candidate(NotificationType.StreakWaiting, Unspecified(at), new[]
                    {
                        // {left} [P]: las victorias que faltan; el texto de 06 §8.3 («una victoria más») solo es verdad con 1.
                        NotificationArg.Number("n", context.StreakWins), NotificationArg.Number("m", next),
                        NotificationArg.Number("left", next - context.StreakWins),
                    }, 0));
                }
            }

            if (context.CanoesActive && context.Canoes.Count == 0)
            {
                DateTime? full = context.CanoesFullAtUtc();
                if (full.HasValue)
                {
                    candidates.Add(new Candidate(
                        NotificationType.CanoesFull, Unspecified(full.Value + offset), new[] { NotificationArg.Number("n", context.Canoes.Max) }, 0));
                }
            }

            if (context.ContentEnd && context.NextWorldName != null && context.NextBatchEtaDays > 0)
            {
                candidates.Add(new Candidate(
                    NotificationType.NewLevels, nowLocal.AddDays(context.NextBatchEtaDays), new[] { NotificationArg.Of("name", context.NextWorldName) }, 0));
            }

            for (int i = 0; i < ComebackDays.Count; i++)
            {
                var type = (NotificationType)((int)NotificationType.ComebackD7 + i);
                NotificationArg seeds = NotificationArg.Number("seeds", context.ComebackSeeds);
                NotificationArg level = NotificationArg.Digits("level", Math.Max(1, context.CurrentLevel));
                bool named = type != NotificationType.ComebackD7 && context.CurrentWorldName != null;
                NotificationArg[] args = named
                    ? new[] { seeds, NotificationArg.Of("world", context.CurrentWorldName!), level }
                    : new[] { seeds, level };

                // Sin el nombre del mundo, el texto del D7 con el tipo de su día [P].
                string key = named || type == NotificationType.ComebackD7 ? type.BodyKey() : NotificationType.ComebackD7.BodyKey();
                candidates.Add(new Candidate(type, nowLocal.AddDays(ComebackDays[i]), args, 0, key));
            }
        }

        // El hito de la Crecida siguiente a wins (6 o 10), o 0 si no hay.
        private static int NextMilestone(int wins)
        {
            for (int i = 0; i < AF.Economy.StreakRecord.Milestones.Count; i++)
            {
                int m = AF.Economy.StreakRecord.Milestones[i];
                if (m > wins)
                {
                    return m;
                }
            }

            return 0;
        }

        private static DateTime Unspecified(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }

        private static DateTime? ParseDate(string? key)
        {
            if (key != null && DateTime.TryParseExact(key, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            {
                return d.Date;
            }

            return null;
        }

        private static readonly NotificationArg[] NoArgs = new NotificationArg[0];

        private readonly struct Candidate
        {
            public Candidate(NotificationType type, DateTime local, IReadOnlyList<NotificationArg> args, int day, string? bodyKey = null)
            {
                Type = type;
                BodyKey = bodyKey ?? type.BodyKey();
                Local = local;
                Args = args;
                Day = day;
            }

            public NotificationType Type { get; }

            public DateTime Local { get; }

            public IReadOnlyList<NotificationArg> Args { get; }

            public int Day { get; }

            public string BodyKey { get; }
        }
    }
}
