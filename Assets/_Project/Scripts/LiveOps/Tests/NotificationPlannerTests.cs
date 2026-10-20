#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// B6: <see cref="NotificationPlanner"/> (06 §8.2-§8.3 y §9) y <see cref="NotificationService"/> con el programador falso. El
    /// jugador está en Bogotá (UTC−5) y sale a las 21:00 locales del martes 20 de octubre de 2026, salvo que el test diga otra cosa.
    /// </summary>
    public sealed class NotificationPlannerTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Today = new DateTime(2026, 10, 20);

        [Test]
        public void TheDailyReady_GoesAt0930_EveryDay_AndTodayOnlyBeforeItAndUnplayed()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.DailyUnlocked = true;
            IReadOnlyList<PlannedNotification> plan = Plan(c);
            PlannedNotification first = plan[0];
            Assert.That(first.Type, Is.EqualTo(NotificationType.DailyReady));
            Assert.That(first.DayIndex, Is.EqualTo(1));
            Assert.That(first.FireAtLocal, Is.EqualTo(Today.AddDays(1).AddHours(9.5)));
            Assert.That(first.FireAtUtc, Is.EqualTo(new DateTime(2026, 10, 21, 14, 30, 0, DateTimeKind.Utc)));
            Assert.That(first.FireAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));

            NotificationContext morning = Context(Today.AddHours(8));
            morning.DailyUnlocked = true;
            Assert.That(Plan(morning)[0].DayIndex, Is.EqualTo(0), "a las 08:00 sin jugar: el de hoy");
            morning.DailyPlayedToday = true;
            Assert.That(Plan(morning)[0].DayIndex, Is.EqualTo(1), "ya jugó hoy");
            NotificationContext late = Context(Today.AddHours(10));
            late.DailyUnlocked = true;
            Assert.That(Plan(late)[0].DayIndex, Is.EqualTo(1), "pasadas las 09:30, mañana [P]");
        }

        [Test]
        public void OneNoticePerDay_ByThePriorityOf06_8_2()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.DailyUnlocked = true;
            c.LogbookUnlocked = true;
            c.LogbookReward = "50 semillas";
            c.StreakActive = true;
            c.StreakWins = 4;
            c.LastWinAt = Utc(Today.AddHours(20)); // + 36 h = jueves 08:00 → 09:00 por el silencio
            IReadOnlyList<PlannedNotification> plan = Plan(c);
            var perDay = new Dictionary<int, int>();
            foreach (PlannedNotification p in plan)
            {
                perDay[p.DayIndex] = perDay.TryGetValue(p.DayIndex, out int n) ? n + 1 : 1;
                Assert.That(perDay[p.DayIndex], Is.LessThanOrEqualTo(1), "push_daily_cap_d0_d7 = 1 (día " + p.DayIndex + ")");
            }

            Assert.That(Find(plan, 2)!.Type, Is.EqualTo(NotificationType.DailyReady), "Reto listo gana a Bitácora y a Crecida");
            Assert.That(plan.CountOf(NotificationType.StreakWaiting), Is.EqualTo(0));
            Assert.That(plan.CountOf(NotificationType.LogbookPending), Is.EqualTo(0));
        }

        [Test]
        public void Logbook_ComesAt2000_WhenTheRetoIsDoneToday()
        {
            NotificationContext c = Context(Today.AddHours(12));
            c.DailyUnlocked = true;
            c.DailyPlayedToday = true;
            c.LogbookUnlocked = true;
            c.LogbookReward = "50 semillas";
            PlannedNotification first = Plan(c)[0];
            Assert.That(first.Type, Is.EqualTo(NotificationType.LogbookPending));
            Assert.That(first.FireAtLocal, Is.EqualTo(Today.AddHours(20)));
            Assert.That(first.Args, Is.EqualTo(new[] { NotificationArg.Of("reward", "50 semillas") }));

            c.LogbookClaimedToday = true;
            Assert.That(Plan(c)[0].Type, Is.EqualTo(NotificationType.DailyReady), "sellada hoy: nada a las 20:00");
            c.LogbookReward = null;
            c.LogbookClaimedToday = false;
            Assert.That(Plan(c).CountOf(NotificationType.LogbookPending), Is.EqualTo(0), "sin premio que nombrar, sin aviso");
        }

        [Test]
        public void DailyStreak_ReplacesTheDailyReady_OnTheDayThatClosesSeven()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.DailyUnlocked = true;
            c.DailyPlayedToday = true;
            c.DailyStreakDays = 6;
            c.DailyLastWinDate = "2026-10-20";
            IReadOnlyList<PlannedNotification> plan = Plan(c);
            Assert.That(plan[0].Type, Is.EqualTo(NotificationType.DailyStreak));
            Assert.That(plan[0].DayIndex, Is.EqualTo(1));
            Assert.That(Find(plan, 2)!.Type, Is.EqualTo(NotificationType.DailyReady), "solo el día que cerraría la racha");

            c.DailyStreakDays = 13;
            Assert.That(Plan(c)[0].Type, Is.EqualTo(NotificationType.DailyStreak), "13 → el 14 también cierra una de 7");
            c.DailyStreakDays = 5;
            Assert.That(Plan(c)[0].Type, Is.EqualTo(NotificationType.DailyReady));
        }

        [Test]
        public void StreakWaiting_At36HoursFromTheLastWin_OnceAndWithItsArgs()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.StreakActive = true;
            c.StreakWins = 7;
            c.LastWinAt = Utc(Today.AddHours(19));
            IReadOnlyList<PlannedNotification> plan = Plan(c);
            PlannedNotification streak = plan[0];
            Assert.That(streak.Type, Is.EqualTo(NotificationType.StreakWaiting));
            Assert.That(streak.FireAtLocal, Is.EqualTo(Today.AddDays(2).AddHours(9)), "07:00 cae en el silencio: 09:00");
            Assert.That(streak.Args, Is.EqualTo(new[] { NotificationArg.Number("n", 7), NotificationArg.Number("m", 10), NotificationArg.Number("left", 3) }));
            Assert.That(plan.CountOf(NotificationType.StreakWaiting), Is.EqualTo(1));

            c.StreakWins = 2;
            Assert.That(Plan(c).CountOf(NotificationType.StreakWaiting), Is.EqualTo(0), "racha < 3");
            c.StreakWins = 5;
            c.LastWinAt = Utc(Today.AddHours(-40));
            Assert.That(Plan(c).CountOf(NotificationType.StreakWaiting), Is.EqualTo(0), "las 36 h ya pasaron [P]");
            c.LastWinAt = Utc(Today.AddHours(19));
            c.StreakActive = false;
            Assert.That(Plan(c).CountOf(NotificationType.StreakWaiting), Is.EqualTo(0), "Crecida apagada o sin desbloquear");
        }

        [Test]
        public void CanoesFull_OnlyWhenLeavingWithZero_AtTheFullTime()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.CanoesActive = true;
            DateTime now = c.NowUtc;
            c.Canoes = new CanoesStatus(0, 5, now.AddMinutes(10), null, now);
            DateTime full = now.AddMinutes(10 + 4 * 25);
            Assert.That(c.CanoesFullAtUtc(), Is.EqualTo(full));
            PlannedNotification p = Plan(c)[0];
            Assert.That(p.Type, Is.EqualTo(NotificationType.CanoesFull));
            Assert.That(p.FireAtLocal, Is.EqualTo(Today.AddDays(1).AddHours(9)), "llenas a las 22:50, en el silencio: a las 09:00");
            Assert.That(p.Args, Is.EqualTo(new[] { NotificationArg.Number("n", 5) }));
        }

        [Test]
        public void CanoesFull_InTheNight_MovesToNineAndNeverWithCanoesLeft()
        {
            NotificationContext c = Context(Today.AddHours(19));
            c.CanoesActive = true;
            DateTime now = c.NowUtc;
            c.Canoes = new CanoesStatus(0, 5, now.AddMinutes(25), null, now); // llenas a las 21:05
            PlannedNotification p = Plan(c)[0];
            Assert.That(p.FireAtLocal, Is.EqualTo(Today.AddHours(21).AddMinutes(5)));

            c = Context(Today.AddHours(21));
            c.CanoesActive = true;
            now = c.NowUtc;
            c.Canoes = new CanoesStatus(0, 5, now.AddMinutes(25), null, now); // llenas a las 23:05
            Assert.That(Plan(c)[0].FireAtLocal, Is.EqualTo(Today.AddDays(1).AddHours(9)));

            c.Canoes = new CanoesStatus(1, 5, now.AddMinutes(25), null, now);
            Assert.That(Plan(c).CountOf(NotificationType.CanoesFull), Is.EqualTo(0), "solo si salió con 0");
            c.Canoes = new CanoesStatus(0, 5, now.AddMinutes(25), now.AddHours(1), now);
            Assert.That(Plan(c).CountOf(NotificationType.CanoesFull), Is.EqualTo(0), "con Canoas infinitas no");
            c.Canoes = new CanoesStatus(0, 5, now.AddMinutes(25), null, now);
            c.CanoesActive = false;
            Assert.That(Plan(c).CountOf(NotificationType.CanoesFull), Is.EqualTo(0), "antes de rescue_min_level no hay canoas");
        }

        [Test]
        public void NewLevels_AtTheEta_OnlyWithTheContentEndAndAName()
        {
            NotificationContext c = Context(Today.AddHours(15));
            c.ContentEnd = true;
            c.NextBatchEtaDays = 3;
            c.NextWorldName = "Mundo río arriba";
            PlannedNotification p = Plan(c)[0];
            Assert.That(p.Type, Is.EqualTo(NotificationType.NewLevels));
            Assert.That(p.FireAtLocal, Is.EqualTo(Today.AddDays(3).AddHours(15)));
            Assert.That(p.Args, Is.EqualTo(new[] { NotificationArg.Of("name", "Mundo río arriba") }));

            c.NextWorldName = null;
            Assert.That(Plan(c).CountOf(NotificationType.NewLevels), Is.EqualTo(0));
            c.NextWorldName = "x";
            c.ContentEnd = false;
            Assert.That(Plan(c).CountOf(NotificationType.NewLevels), Is.EqualTo(0));
        }

        [Test]
        public void Comeback_AtDays7_14_30_60_WithTheGiftAndTheLevel()
        {
            NotificationContext c = Context(Today.AddHours(15));
            c.CurrentLevel = 1234;
            c.CurrentWorldName = "Manaos";
            IReadOnlyList<PlannedNotification> plan = Plan(c);
            Assert.That(plan.Count, Is.EqualTo(4), "sin otros sistemas, solo el Regreso");
            int[] days = { 7, 14, 30, 60 };
            NotificationType[] types = { NotificationType.ComebackD7, NotificationType.ComebackD14, NotificationType.ComebackD30, NotificationType.ComebackD60 };
            for (int i = 0; i < 4; i++)
            {
                Assert.That(plan[i].Type, Is.EqualTo(types[i]));
                Assert.That(plan[i].DayIndex, Is.EqualTo(days[i]));
                Assert.That(plan[i].FireAtLocal, Is.EqualTo(Today.AddDays(days[i]).AddHours(15)));
                Assert.That(plan[i].BodyKey, Is.EqualTo(types[i].BodyKey()));
            }

            Assert.That(plan[0].Args, Is.EqualTo(new[] { NotificationArg.Number("seeds", 200), NotificationArg.Digits("level", 1234) }));
            Assert.That(plan[1].Args, Is.EqualTo(new[] { NotificationArg.Number("seeds", 200), NotificationArg.Of("world", "Manaos"), NotificationArg.Digits("level", 1234) }));

            c.CurrentWorldName = null;
            plan = Plan(c);
            Assert.That(plan[1].Type, Is.EqualTo(NotificationType.ComebackD14), "el tipo de analítica sigue siendo el del día");
            Assert.That(plan[1].BodyKey, Is.EqualTo(NotificationType.ComebackD7.BodyKey()), "sin nombre de mundo, el texto del D7 [P]");
        }

        [Test]
        public void After30Days_OnlyTheComeback_AndThe7DayLapsedCap()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.DailyUnlocked = true;
            c.CurrentWorldName = "Manaos";
            IReadOnlyList<PlannedNotification> plan = Plan(c);
            int lastNonComeback = -1;
            var perWeek = new int[4];
            foreach (PlannedNotification p in plan)
            {
                if (!p.Type.IsComeback())
                {
                    lastNonComeback = Math.Max(lastNonComeback, p.DayIndex);
                }

                if (p.DayIndex >= NotificationPlanner.LapsedFromDay && p.DayIndex < NotificationPlanner.StopAfterDays)
                {
                    perWeek[(p.DayIndex - NotificationPlanner.LapsedFromDay) / 7]++;
                }
            }

            Assert.That(lastNonComeback, Is.LessThan(NotificationPlanner.StopAfterDays), "parar a los 30 días salvo el Regreso");
            foreach (int n in perWeek)
            {
                Assert.That(n, Is.LessThanOrEqualTo(3), "push_weekly_cap_lapsed = 3");
            }

            // Días 1-6: uno por día (el Reto). Desde el 7, semanas de 3 en días alternos desde el 7 (el Regreso no espera al alterno).
            var days = new List<int>();
            foreach (PlannedNotification p in plan)
            {
                days.Add(p.DayIndex);
            }

            Assert.That(days, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 9, 11, 14, 15, 17, 21, 23, 25, 29, 30, 60 }));
            Assert.That(Find(plan, 7)!.Type, Is.EqualTo(NotificationType.ComebackD7), "el Regreso gana el día 7 [P]");
            Assert.That(Find(plan, 14)!.Type, Is.EqualTo(NotificationType.ComebackD14));
        }

        [Test]
        public void ThePlan_IsDeterministic_WithUniqueIds()
        {
            NotificationContext c = Context(Today.AddHours(21));
            c.DailyUnlocked = true;
            c.LogbookUnlocked = true;
            c.LogbookReward = "x";
            IReadOnlyList<PlannedNotification> a = Plan(c);
            IReadOnlyList<PlannedNotification> b = Plan(c);
            Assert.That(a.Count, Is.EqualTo(b.Count));
            var ids = new HashSet<int>();
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(a[i].Id, Is.EqualTo(b[i].Id));
                Assert.That(a[i].FireAtUtc, Is.EqualTo(b[i].FireAtUtc));
                Assert.That(ids.Add(a[i].Id), Is.True, "id repetido " + a[i].Id);
                if (i > 0)
                {
                    Assert.That(a[i].FireAtUtc, Is.GreaterThanOrEqualTo(a[i - 1].FireAtUtc), "ordenado por hora");
                }

                Assert.That(new QuietHours(22, 9).Contains(a[i].FireAtLocal), Is.False, "nada en las horas silenciosas");
            }
        }

        [Test]
        public void TypeIds_AreUniqueAndRoundTrip()
        {
            var seen = new HashSet<string>();
            foreach (NotificationType t in NotificationTypes.All)
            {
                Assert.That(seen.Add(t.ToId()), Is.True);
                Assert.That(NotificationTypes.TryParse(t.ToId(), out NotificationType back), Is.True);
                Assert.That(back, Is.EqualTo(t));
                StringAssert.StartsWith("push_", t.BodyKey());
            }

            Assert.That(seen.Count, Is.EqualTo(NotificationTypes.Count));
            Assert.That(NotificationTypes.TryParse("league_closes", out _), Is.False, "los de la 1.2 no están");
            Assert.That(NotificationType.ComebackD7.Priority(), Is.GreaterThan(NotificationType.DailyReady.Priority()));
            Assert.That(NotificationType.DailyReady.Priority(), Is.GreaterThan(NotificationType.LogbookPending.Priority()));
            Assert.That(NotificationType.LogbookPending.Priority(), Is.GreaterThan(NotificationType.StreakWaiting.Priority()));
            Assert.That(NotificationType.StreakWaiting.Priority(), Is.GreaterThan(NotificationType.CanoesFull.Priority()));
            Assert.That(NotificationType.CanoesFull.Priority(), Is.GreaterThan(NotificationType.NewLevels.Priority()));
        }

        [Test]
        public void Service_SchedulesOnlyWithPermission_AndCollectsOnOpen()
        {
            var clock = new ManualClock(Utc(Today.AddHours(21)), Bogota);
            var scheduler = new FakeNotificationScheduler(clock);
            var permission = new PushPermissionService(new InMemoryPushSettingsStore(), scheduler);
            var texts = new FakeTexts();
            var service = new NotificationService(scheduler, permission, texts);
            NotificationContext c = Context(Today.AddHours(21));
            c.DailyUnlocked = true;

            Assert.That(service.OnBackground(c), Is.EqualTo(0), "sin «Avísame» no se programa nada");
            Assert.That(scheduler.Scheduled.Count, Is.EqualTo(0));

            permission.SetFromSettings(true);
            int count = service.OnBackground(c);
            Assert.That(count, Is.EqualTo(Plan(c).Count));
            Assert.That(scheduler.Scheduled.Count, Is.EqualTo(count));
            LocalNotification first = scheduler.Scheduled[0];
            Assert.That(first.Type, Is.EqualTo("daily_ready"));
            Assert.That(first.Title, Is.EqualTo("[push_title]"));
            Assert.That(first.Body, Is.EqualTo("[push_daily_ready]"));
            Assert.That(first.FireAtUtc, Is.EqualTo(new DateTime(2026, 10, 21, 14, 30, 0, DateTimeKind.Utc)));

            texts.Missing.Add("push_daily_ready");
            int withoutDaily = service.OnBackground(c);
            Assert.That(withoutDaily, Is.EqualTo(Plan(c).Count - Plan(c).CountOf(NotificationType.DailyReady)), "sin texto en el idioma, sin aviso");
            texts.Missing.Clear();
            texts.Missing.Add("push_comeback_d14");
            service.OnBackground(c);
            LocalNotification? d14 = null;
            foreach (LocalNotification n in scheduler.Scheduled)
            {
                if (n.Type == "comeback_d14")
                {
                    d14 = n;
                }
            }

            Assert.That(d14, Is.Not.Null, "sin la variante en el idioma, el texto del D7");
            Assert.That(d14!.Body, Is.EqualTo("[push_comeback_d7]"));
            texts.Missing.Clear();
            service.OnBackground(c);

            clock.Advance(TimeSpan.FromDays(2)); // pasan los Retos listos de los días 1 y 2
            scheduler.SimulateOpenFrom("daily_ready");
            NotificationOpenReport report = service.OnForeground();
            Assert.That(report.Delivered.Count, Is.EqualTo(2));
            Assert.That(report.Delivered[0].Type, Is.EqualTo("daily_ready"));
            Assert.That(report.OpenedType, Is.EqualTo("daily_ready"));
            Assert.That(scheduler.Scheduled.Count, Is.EqualTo(0), "al abrir se cancela todo");
            Assert.That(service.LastPlan.Count, Is.EqualTo(0));
            Assert.That(service.OnForeground().OpenedType, Is.Null, "el tipo abierto se lee una vez");

            permission.SetFromSettings(false);
            Assert.That(service.OnBackground(c), Is.EqualTo(0));
        }

        [Test]
        public void FakeScheduler_ReplacesById_AndDeliversOnlyWithPermission()
        {
            var clock = new ManualClock(Utc(Today));
            var scheduler = new FakeNotificationScheduler(clock, PushPermission.Denied);
            scheduler.Schedule(new LocalNotification(7, "a", "t", "b", clock.UtcNow.AddMinutes(5)));
            scheduler.Schedule(new LocalNotification(7, "b", "t", "b", clock.UtcNow.AddMinutes(6)));
            Assert.That(scheduler.Scheduled.Count, Is.EqualTo(1));
            Assert.That(scheduler.Scheduled[0].Type, Is.EqualTo("b"));
            clock.Advance(TimeSpan.FromHours(1));
            Assert.That(scheduler.TakeDelivered().Count, Is.EqualTo(0), "sin permiso el sistema no enseña nada");
            Assert.That(scheduler.Scheduled.Count, Is.EqualTo(0));
            Assert.Throws<ArgumentException>(() => new LocalNotification(1, "", "t", "b", clock.UtcNow));
            Assert.Throws<ArgumentException>(() => new LocalNotification(1, "a", "t", "b", DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Local)));

            var android12 = new FakeNotificationScheduler(clock, PushPermission.NotRequested, permissionRequired: false);
            Assert.That(android12.Permission, Is.EqualTo(PushPermission.Allowed));
            PushPermission? answer = null;
            android12.RequestPermission(p => answer = p);
            Assert.That(answer, Is.EqualTo(PushPermission.Allowed));
            Assert.That(android12.PermissionRequests, Is.EqualTo(0));
        }

        private static IReadOnlyList<PlannedNotification> Plan(NotificationContext c)
        {
            return new NotificationPlanner().Plan(c);
        }

        private static PlannedNotification? Find(IReadOnlyList<PlannedNotification> plan, int day)
        {
            foreach (PlannedNotification p in plan)
            {
                if (p.DayIndex == day)
                {
                    return p;
                }
            }

            return null;
        }

        private static DateTime Utc(DateTime local)
        {
            return DateTime.SpecifyKind(local - Bogota, DateTimeKind.Utc);
        }

        private static NotificationContext Context(DateTime local)
        {
            return new NotificationContext { NowUtc = Utc(local), LocalOffset = Bogota, CurrentLevel = 12 };
        }

        private sealed class FakeTexts : INotificationTexts
        {
            public HashSet<string> Missing { get; } = new HashSet<string>();

            public string Resolve(string key, IReadOnlyList<NotificationArg> args)
            {
                return Missing.Contains(key) ? "" : "[" + key + "]";
            }
        }
    }

    internal static class PlanExtensions
    {
        public static int CountOf(this IReadOnlyList<PlannedNotification> plan, NotificationType type)
        {
            int n = 0;
            foreach (PlannedNotification p in plan)
            {
                if (p.Type == type)
                {
                    n++;
                }
            }

            return n;
        }
    }
}
