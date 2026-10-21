#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>B6: las líneas <c>AF_STATE</c> de sesiones, Regalo de regreso y avisos (<see cref="SessionProbeFormat"/>).</summary>
    public sealed class SessionProbeFormatTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Session_AndPrompt_Lines()
        {
            var start = new SessionStart(SessionStartKind.Resume, 3, T0, T0.AddMinutes(-45));
            Assert.That(SessionProbeFormat.Session(12, in start),
                Is.EqualTo("AF_STATE {\"event\":\"session\",\"t\":12,\"n\":3,\"kind\":\"resume\",\"away_s\":2700}"));
            var e = new PushPromptEvent(PushPromptEvent.StepNative, "granted", "first");
            Assert.That(SessionProbeFormat.Prompt(5, in e),
                Is.EqualTo("AF_STATE {\"event\":\"push_prompt\",\"t\":5,\"step\":\"native\",\"result\":\"granted\",\"origin\":\"first\"}"));
        }

        [Test]
        public void Comeback_OnlyForTheSegmentsWithAGift()
        {
            var none = new ComebackOutcome(ComebackSegment.AtRisk, 4, 0);
            Assert.That(SessionProbeFormat.Comeback(1, none), Is.Null);
            var lost = new ComebackOutcome(ComebackSegment.Lost, 15, 2) { Granted = true, Seeds = 200, ExtraCuia = 1, CanoesAdded = 0 };
            Assert.That(SessionProbeFormat.Comeback(9, lost), Is.EqualTo(
                "AF_STATE {\"event\":\"comeback_gift\",\"t\":9,\"days_away\":15,\"segment\":\"lost\",\"granted\":true,\"seeds\":200,"
                + "\"extra_cuia\":1,\"canoes_added\":0,\"reminder\":true,\"block\":\"none\"}"));
        }

        [Test]
        public void Plan_AndOpen_Lines()
        {
            Assert.That(SessionProbeFormat.Plan(2, false, new List<PlannedNotification>()),
                Is.EqualTo("AF_STATE {\"event\":\"push_plan\",\"t\":2,\"allowed\":false,\"count\":0,\"first\":null,\"first_at\":null}"));
            var c = new NotificationContext { NowUtc = T0, LocalOffset = TimeSpan.FromHours(-5) };
            IReadOnlyList<PlannedNotification> plan = new NotificationPlanner().Plan(c);
            StringAssert.StartsWith("AF_STATE {\"event\":\"push_plan\",\"t\":2,\"allowed\":true,\"count\":4,\"first\":\"comeback_d7\",\"first_at\":\"2026-10-27T10:00\"}",
                SessionProbeFormat.Plan(2, true, plan));

            var lines = new List<string>();
            var report = new NotificationOpenReport(new[] { new DeliveredNotification(1001, "daily_ready", T0) }, "daily_ready");
            SessionProbeFormat.Open(3, report, lines);
            Assert.That(lines, Is.EqualTo(new[]
            {
                "AF_STATE {\"event\":\"push_sent\",\"t\":3,\"type\":\"daily_ready\",\"id\":1001}",
                "AF_STATE {\"event\":\"push_open\",\"t\":3,\"type\":\"daily_ready\"}",
            }));
        }
    }
}
