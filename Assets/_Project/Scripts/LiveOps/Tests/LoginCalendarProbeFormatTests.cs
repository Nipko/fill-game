#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Líneas <c>AF_STATE</c> de la Bitácora (B4; 08 §8.2 <c>af_calendar_claim</c> con <c>choice</c> y <c>cycle</c> de D-033 [P]):
    /// formato exacto, solo ids y cifras, nunca texto visible.
    /// </summary>
    public sealed class LoginCalendarProbeFormatTests
    {
        [Test]
        public void Claim_WritesTheFieldsOfAfCalendarClaim()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            var wallet = new Wallet(Wallet.NewProfile(clock), new MemoryProfileStore(), clock);
            var store = new InMemoryLoginCalendarStore(new LoginCalendarRecord { NextDay = 7, Cycle = 2, LastClaimDate = "2026-10-17" });
            var service = new LoginCalendarService(store, wallet, clock, 10);
            service.MarkPlayed();
            LoginCalendarClaim claim = service.Claim(LoginCalendarChoice.Booster, 25, LoginCalendarUnlocks.All, 1)!;

            string line = new LoginCalendarProbeFormat().Claim(1234, claim);
            Assert.That(line, Is.EqualTo(
                "AF_STATE {\"event\":\"calendar_claim\",\"t\":1234,\"day\":7,\"reward_type\":\"mix\",\"reward_value\":250,"
                + "\"days_missed_before\":2,\"choice\":\"booster\",\"cycle\":2,\"reward_json\":\"{\\\"seeds\\\":50,\\\"hint\\\":2}\","
                + "\"extras_json\":\"{\\\"basket\\\":" + claim.Basket.ContentsJson().Replace("\"", "\\\"") + "}\"}"));
            StringAssert.Contains("\\\"kind\\\":\\\"normal\\\"", line);
        }

        [Test]
        public void State_AndTakeover_WriteIdsOnly()
        {
            var format = new LoginCalendarProbeFormat();
            Assert.That(format.State(5, LoginCalendarProbeFormat.CausePlayed, LoginCalendarState.Ready, 1, 3, null, "2026-10-20"), Is.EqualTo(
                "AF_STATE {\"event\":\"calendar_state\",\"t\":5,\"cause\":\"played\",\"state\":\"ready\",\"cycle\":1,\"next_day\":3,"
                + "\"last_claim_date\":null,\"played_date\":\"2026-10-20\"}"));
            Assert.That(format.Takeover(6, "logbook", true, 2, 1, "lobby"), Is.EqualTo(
                "AF_STATE {\"event\":\"takeover\",\"t\":6,\"kind\":\"logbook\",\"shown\":true,\"session\":2,\"shown_this_session\":1,\"source\":\"lobby\"}"));
            Assert.That(LoginCalendarProbeFormat.StateId(LoginCalendarState.Hidden), Is.EqualTo("hidden"));
            Assert.That(LoginCalendarProbeFormat.StateId(LoginCalendarState.Waiting), Is.EqualTo("waiting"));
            Assert.That(LoginCalendarProbeFormat.StateId(LoginCalendarState.Sealed), Is.EqualTo("sealed"));
        }
    }
}
