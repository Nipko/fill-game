#nullable enable
using System;
using System.IO;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>Formato exacto de las líneas <c>AF_STATE</c> del Reto del río (R10; nota §12; 08 §8.2).</summary>
    public sealed class DailyProbeFormatTests
    {
        private readonly DailyProbeFormat _format = new DailyProbeFormat();

        [Test]
        public void NoCanoesChoice_DailyChallenge()
        {
            // E7: af_no_canoes_choice con choice = daily_challenge (04 §11: choice ∈ {wait, ad, seeds_refill, iap_infinite,
            // daily_challenge, map}; 08 §8.2 remite al capítulo 4).
            Assert.That(
                _format.NoCanoesChoice(77),
                Is.EqualTo("AF_STATE {\"event\":\"no_canoes_choice\",\"t\":77,\"choice\":\"daily_challenge\"}"));
            string chapter04 = File.ReadAllText(Path.Combine(LiveOpsTestFiles.RepoRoot, "docs", "gdd", "04-comodines-vidas-rescate.md"));
            int row = chapter04.IndexOf("| `af_no_canoes_choice` |", StringComparison.Ordinal);
            Assert.That(row, Is.GreaterThanOrEqualTo(0), "la fila de 04 §11");
            string line = chapter04.Substring(row, chapter04.IndexOf('\n', row) - row);
            Assert.That(line, Does.Contain(DailyProbeFormat.NoCanoesDailyChoice), "el valor de choice sale de 04 §11");
        }

        [Test]
        public void DailyChallenge_Win()
        {
            Assert.That(
                _format.DailyChallenge(1234, "2026-10-19", DailyResult.Win, 83500, 3),
                Is.EqualTo("AF_STATE {\"event\":\"daily_challenge\",\"t\":1234,\"date\":\"2026-10-19\",\"result\":\"win\","
                    + "\"t_ms\":83500,\"murky_bonus\":false,\"streak_days\":3}"));
        }

        [Test]
        public void DailyChallenge_WithoutTime_IsNull()
        {
            const string expected = "AF_STATE {\"event\":\"daily_challenge\",\"t\":5,\"date\":\"2026-10-19\",\"result\":\"fail\","
                + "\"t_ms\":null,\"murky_bonus\":false,\"streak_days\":0}";
            Assert.That(_format.DailyChallenge(5, "2026-10-19", DailyResult.Fail, null, 0), Is.EqualTo(expected));
            Assert.That(_format.DailyChallenge(5, "2026-10-19", DailyResult.Fail, -1, 0), Is.EqualTo(expected));
        }

        [Test]
        public void DailyChallenge_WithoutResult_IsNull()
        {
            Assert.That(_format.DailyChallenge(5, "2026-10-19", DailyResult.None, 1000, 2), Is.Null, "nada que informar");
            Assert.That(
                _format.DailyChallenge(5, "2026-10-19", DailyResult.Abandon, null, 0),
                Does.Contain("\"result\":\"abandon\""),
                "el valor de 08 §8.2 se sigue sabiendo escribir, aunque el servicio ya no lo emite");
        }

        [Test]
        public void Win_FromTheClaimReport_AndNothingWithoutReward()
        {
            var confirmed = new DailyClaimReport(ClaimOutcome.Confirmed, "2026-10-19", 7, 60, 1, null);
            Assert.That(
                _format.Win(10, confirmed, 42000),
                Is.EqualTo("AF_STATE {\"event\":\"daily_challenge\",\"t\":10,\"date\":\"2026-10-19\",\"result\":\"win\","
                    + "\"t_ms\":42000,\"murky_bonus\":false,\"streak_days\":7}"));

            var pending = new DailyClaimReport(ClaimOutcome.Pending, "2026-10-20", 1, 0, 0, null);
            Assert.That(
                _format.Win(11, pending, null),
                Is.EqualTo("AF_STATE {\"event\":\"daily_challenge\",\"t\":11,\"date\":\"2026-10-20\",\"result\":\"win\","
                    + "\"t_ms\":null,\"murky_bonus\":false,\"streak_days\":1}"),
                "pendiente de validar: la victoria cuenta igual");

            var none = new DailyClaimReport(ClaimOutcome.NoReward, "2026-10-19", 7, 0, 0, null);
            Assert.That(_format.Win(12, none, 1000), Is.Null, "una rejugada no es la victoria del día [P]");
        }

        [Test]
        public void DayEnd_Fail_OnlyWhenReported()
        {
            var fail = new DailyPlayEnd("2026-10-21", 2, "d2_n0017", DailyResult.Fail, 2);
            Assert.That(
                _format.DayEnd(99, fail),
                Is.EqualTo("AF_STATE {\"event\":\"daily_challenge\",\"t\":99,\"date\":\"2026-10-21\",\"result\":\"fail\","
                    + "\"t_ms\":null,\"murky_bonus\":false,\"streak_days\":2}"));

            var nothing = new DailyPlayEnd("2026-10-21", 2, "d2_n0017", DailyResult.None, 0);
            Assert.That(_format.DayEnd(99, nothing), Is.Null);
        }

        [Test]
        public void DayEnd_FromTheService()
        {
            var f = new DailyFixture();
            string? line = null;
            f.Service.DayEnded += e => line = _format.DayEnd(7, e);
            f.Service.Start(DailyFixture.MaxLevel);
            f.Service.RecordExitWithoutWin();
            f.NextDay();
            f.Service.DiscardStale();

            Assert.That(
                line,
                Is.EqualTo("AF_STATE {\"event\":\"daily_challenge\",\"t\":7,\"date\":\"2026-10-19\",\"result\":\"fail\","
                    + "\"t_ms\":null,\"murky_bonus\":false,\"streak_days\":0}"));
        }

        [Test]
        public void LevelStart_UsesTheBoardId()
        {
            Assert.That(
                _format.LevelStart(300, "d1_n0291", 1),
                Is.EqualTo("AF_STATE {\"event\":\"level_start\",\"t\":300,\"level_name\":\"d1_n0291\",\"variant\":\"base\",\"band\":1}"));
        }

        [Test]
        public void EarnSeeds_DailyChallengeSource_AndNothingWhenZero()
        {
            Assert.That(
                _format.EarnSeeds(301, 60),
                Is.EqualTo("AF_STATE {\"event\":\"earn_virtual_currency\",\"t\":301,\"virtual_currency_name\":\"seeds\","
                    + "\"source\":\"daily_challenge\",\"value\":60}"));
            Assert.That(_format.EarnSeeds(301, 0), Is.Null, "pendiente o sin premio: no se ganó nada");
        }

        [Test]
        public void Card_Play_WithAnIgnoredOverride()
        {
            var slot = new DailySlot(1, 291, null, "d1_n9999");
            Assert.That(
                _format.Card(400, DailyCardState.Play, "2026-10-19", slot, "d1_n0291", 3),
                Is.EqualTo("AF_STATE {\"event\":\"daily_card\",\"t\":400,\"state\":\"play\",\"date\":\"2026-10-19\",\"band\":1,"
                    + "\"day\":291,\"board\":\"d1_n0291\",\"streak\":3,\"override\":\"d1_n9999\",\"override_ignored\":true}"));
        }

        [Test]
        public void Card_InProgress_WithAnAppliedOverride()
        {
            var slot = new DailySlot(2, 17, "d2_n0100", "d2_n0100");
            Assert.That(
                _format.Card(401, DailyCardState.InProgress, "2026-10-19", slot, "d2_n0100", 0),
                Is.EqualTo("AF_STATE {\"event\":\"daily_card\",\"t\":401,\"state\":\"in_progress\",\"date\":\"2026-10-19\",\"band\":2,"
                    + "\"day\":17,\"board\":\"d2_n0100\",\"streak\":0,\"override\":\"d2_n0100\",\"override_ignored\":false}"));
        }

        [Test]
        public void Card_Hidden_WithoutSlot()
        {
            Assert.That(
                _format.Card(402, DailyCardState.Hidden, "2026-10-19", null, null, 0),
                Is.EqualTo("AF_STATE {\"event\":\"daily_card\",\"t\":402,\"state\":\"hidden\",\"date\":\"2026-10-19\",\"band\":0,"
                    + "\"day\":0,\"board\":null,\"streak\":0,\"override\":null,\"override_ignored\":false}"));
        }

        [Test]
        public void CardStateIds_AreTheExpectedOnes()
        {
            Assert.That(DailyProbeFormat.CardStateId(DailyCardState.Hidden), Is.EqualTo("hidden"));
            Assert.That(DailyProbeFormat.CardStateId(DailyCardState.InProgress), Is.EqualTo("in_progress"));
            Assert.That(DailyProbeFormat.CardStateId(DailyCardState.PendingValidation), Is.EqualTo("pending_validation"));
            Assert.That(DailyProbeFormat.CardStateId(DailyCardState.Sealed), Is.EqualTo("sealed"));
            Assert.That(DailyProbeFormat.CardStateId(DailyCardState.Play), Is.EqualTo("play"));
        }

        [Test]
        public void Ids_NeverBreakTheJson()
        {
            string line = _format.LevelStart(1, "d1_\"n\\0291\n", 1);
            Assert.That(line, Is.EqualTo("AF_STATE {\"event\":\"level_start\",\"t\":1,\"level_name\":\"d1_n0291\",\"variant\":\"base\",\"band\":1}"));
        }

        [Test]
        public void Lines_AreOneLineAndFitTheProbeLimit()
        {
            var slot = new DailySlot(3, 365, "d3_n0365", "d3_n0365");
            string[] lines =
            {
                _format.Card(long.MaxValue, DailyCardState.PendingValidation, "2026-10-19", slot, "d3_n0365", 7),
                _format.DailyChallenge(long.MaxValue, "2026-10-19", DailyResult.Win, long.MaxValue, int.MaxValue)!,
                _format.LevelStart(long.MaxValue, "d3_n0365", 3),
                _format.EarnSeeds(long.MaxValue, int.MaxValue)!,
            };

            foreach (string line in lines)
            {
                Assert.That(line.IndexOf('\n'), Is.EqualTo(-1));
                Assert.That(line.StartsWith("AF_STATE {\"event\":\"", StringComparison.Ordinal), Is.True);
                Assert.That(line.Length, Is.LessThan(960), "DevProbe.MaxLineBytes: sale en una sola parte");
            }
        }
    }
}
