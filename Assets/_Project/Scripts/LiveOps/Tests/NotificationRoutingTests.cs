#nullable enable
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>D-062: lo que abre cada aviso al tocarlo (<see cref="NotificationRouting"/>), con sus caídas al inicio.</summary>
    public sealed class NotificationRoutingTests
    {
        // La tabla de D-062, escrita a mano en el orden de NotificationTypes.All: un tipo nuevo hace fallar el test hasta que se
        // decida qué abre.
        private static readonly string[] ExpectedIds =
        {
            "daily_ready", "daily_streak", "logbook_pending", "streak_waiting", "canoes_full", "new_levels", "comeback_d7",
            "comeback_d14", "comeback_d30", "comeback_d60",
        };

        private static readonly NotificationDestination[] ExpectedDestinations =
        {
            NotificationDestination.DailyChallenge, NotificationDestination.DailyChallenge, NotificationDestination.Logbook,
            NotificationDestination.Home, NotificationDestination.Home, NotificationDestination.Home, NotificationDestination.Home,
            NotificationDestination.Home, NotificationDestination.Home, NotificationDestination.Home,
        };

        [Test]
        public void TheTable_CoversEveryType()
        {
            Assert.That(NotificationTypes.All.Count, Is.EqualTo(ExpectedIds.Length), "un tipo nuevo necesita su fila en D-062");
            for (int i = 0; i < NotificationTypes.All.Count; i++)
            {
                NotificationType type = NotificationTypes.All[i];
                Assert.That(type.ToId(), Is.EqualTo(ExpectedIds[i]));
                Assert.That(type.Destination(), Is.EqualTo(ExpectedDestinations[i]), ExpectedIds[i]);
            }
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
