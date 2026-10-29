#nullable enable
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>D-062: lo que abre cada aviso al tocarlo (<see cref="NotificationRouting"/>), con sus caídas al inicio.</summary>
    public sealed class NotificationRoutingTests
    {
        [Test]
        public void TheTable_CoversEveryType()
        {
            Assert.That(NotificationType.DailyReady.Destination(), Is.EqualTo(NotificationDestination.DailyChallenge));
            Assert.That(NotificationType.DailyStreak.Destination(), Is.EqualTo(NotificationDestination.DailyChallenge));
            Assert.That(NotificationType.LogbookPending.Destination(), Is.EqualTo(NotificationDestination.Logbook));
            Assert.That(NotificationType.StreakWaiting.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationType.CanoesFull.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationType.NewLevels.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationType.ComebackD7.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationType.ComebackD14.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationType.ComebackD30.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationType.ComebackD60.Destination(), Is.EqualTo(NotificationDestination.Home));
            Assert.That(NotificationTypes.All.Count, Is.EqualTo(NotificationTypes.Count), "un tipo nuevo necesita su fila en D-062");
        }

        [TestCase("daily_ready", DailyCardState.Play, NotificationDestination.DailyChallenge)]
        [TestCase("daily_ready", DailyCardState.InProgress, NotificationDestination.DailyChallenge)]
        [TestCase("daily_streak", DailyCardState.Play, NotificationDestination.DailyChallenge)]
        [TestCase("daily_ready", DailyCardState.Hidden, NotificationDestination.Home)] // bloqueado por nivel o sin pool
        [TestCase("daily_ready", DailyCardState.Sealed, NotificationDestination.Home)] // ya jugado hoy
        [TestCase("daily_ready", DailyCardState.PendingValidation, NotificationDestination.Home)] // ya ganado, esperando la hora del servidor
        [TestCase("daily_streak", DailyCardState.Sealed, NotificationDestination.Home)]
        public void TheReto_OpensOnlyWhenTodayCanBePlayed(string type, DailyCardState daily, NotificationDestination expected)
        {
            Assert.That(NotificationRouting.Route(type, daily, true), Is.EqualTo(expected));
        }

        [Test]
        public void TheLogbook_OpensOnlyWithTodaysPagePending()
        {
            Assert.That(NotificationRouting.Route("logbook_pending", DailyCardState.Play, true), Is.EqualTo(NotificationDestination.Logbook));
            Assert.That(NotificationRouting.Route("logbook_pending", DailyCardState.Play, false), Is.EqualTo(NotificationDestination.Home));
        }

        [TestCase("streak_waiting")]
        [TestCase("canoes_full")]
        [TestCase("new_levels")]
        [TestCase("comeback_d7")]
        [TestCase("comeback_d60")]
        [TestCase("not_a_type")]
        [TestCase("")]
        [TestCase(null)]
        public void TheRest_GoHome(string? type)
        {
            Assert.That(NotificationRouting.Route(type, DailyCardState.Play, true), Is.EqualTo(NotificationDestination.Home));
        }

        [Test]
        public void ProbeIds()
        {
            Assert.That(NotificationRouting.IdOf(NotificationDestination.DailyChallenge), Is.EqualTo("daily"));
            Assert.That(NotificationRouting.IdOf(NotificationDestination.Logbook), Is.EqualTo("logbook"));
            Assert.That(NotificationRouting.IdOf(NotificationDestination.Home), Is.EqualTo("home"));
            Assert.That(NotificationRouting.DroppedId, Is.EqualTo("none"));
        }
    }
}
