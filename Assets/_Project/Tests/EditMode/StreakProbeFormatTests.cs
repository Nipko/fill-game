#nullable enable
using System.Collections.Generic;
using AF.Economy;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Formato exacto y orden de las líneas <c>AF_STATE</c> de la Crecida, la Hoja escudo y la Canasta (<see cref="StreakProbeFormat"/>;
    /// B1a, A7; nota <c>docs/dev/crecida-canasta.md</c> §5). Los resultados salen de un <see cref="StreakService"/> de verdad con
    /// <see cref="InMemoryStreakStore"/> y una <see cref="Wallet"/> real, como en <c>StreakServiceTests</c>.
    /// </summary>
    public sealed class StreakProbeFormatTests
    {
        private const string Today = "2026-10-08"; // EconomyFixture.T0 en Bogotá

        private readonly StreakProbeFormat _format = new StreakProbeFormat();

        private static StreakService Service(StreakRecord? initial = null, StreakConfig? config = null)
        {
            var economy = new EconomyFixture();
            return new StreakService(new InMemoryStreakStore(economy.Clock, initial), economy.Wallet, economy.Clock, 10, config);
        }

        private static StreakRecord Record(int wins, bool river = false, bool shield = true, int seeds = 0, params int[] hits)
        {
            var r = new StreakRecord { DateLocal = Today, Wins = wins, RiverFull = river, ShieldAvailable = shield, SeedsToday = seeds };
            r.HitsToday.AddRange(hits);
            return r;
        }

        private List<string> Lines(StreakService service, StreakServiceResult result, int level)
        {
            var lines = new List<string>();
            int added = _format.AppendResult(lines, 7, result, service.Peek(level, true), level);
            Assert.That(added, Is.EqualTo(lines.Count));
            return lines;
        }

        [Test]
        public void State_HasEveryFieldInAFixedOrder()
        {
            StreakService service = Service(Record(4, river: true, shield: false, seeds: 300, hits: new[] { 6, 3 }));

            StreakSnapshot state = service.Peek(42, true);

            Assert.That(
                _format.State(1234, StreakProbeFormat.CauseStart, 42, in state),
                Is.EqualTo("AF_STATE {\"event\":\"streak_state\",\"t\":1234,\"cause\":\"start\",\"level\":42,\"wins\":4,"
                    + "\"river_full\":true,\"shield\":false,\"hits\":[3,6],\"seeds_today\":300,\"date\":\"2026-10-08\","
                    + "\"enabled\":true,\"active\":true,\"free_double_next\":true}"),
                "los hitos, en orden creciente");
        }

        [Test]
        public void State_BeforeTheDebut_AndWithTheKillSwitch()
        {
            StreakService service = Service();
            StreakSnapshot locked = service.Peek(12, true);
            Assert.That(
                _format.State(1, StreakProbeFormat.CauseDay, 12, in locked),
                Is.EqualTo("AF_STATE {\"event\":\"streak_state\",\"t\":1,\"cause\":\"day\",\"level\":12,\"wins\":0,"
                    + "\"river_full\":false,\"shield\":true,\"hits\":[],\"seeds_today\":0,\"date\":\"2026-10-08\","
                    + "\"enabled\":true,\"active\":false,\"free_double_next\":false}"),
                "antes del 25 no hay barra (C1)");

            StreakService off = Service(Record(2, river: true), new StreakConfig { BizFeatureWinStreak = false });
            StreakSnapshot disabled = off.Peek(42, true);
            Assert.That(
                _format.State(2, StreakProbeFormat.CauseStart, 42, in disabled),
                Does.EndWith("\"enabled\":false,\"active\":false,\"free_double_next\":false}"),
                "Q-C11: apagada, ni barra ni x2");
        }

        [Test]
        public void WinOnMilestone3_MilestoneThenState()
        {
            StreakService service = Service(Record(2));

            StreakServiceResult r = service.ApplyWin(41, true, 40, RewardTier.Normal);

            Assert.That(
                Lines(service, r, 42),
                Is.EqualTo(new[]
                {
                    "AF_STATE {\"event\":\"streak_milestone\",\"t\":7,\"level\":41,\"milestone\":3,\"reward_type\":\"seeds\","
                        + "\"reward_value\":\"300\"}",
                    "AF_STATE {\"event\":\"streak_state\",\"t\":7,\"cause\":\"win\",\"level\":42,\"wins\":3,\"river_full\":false,"
                        + "\"shield\":true,\"hits\":[3],\"seeds_today\":300,\"date\":\"2026-10-08\",\"enabled\":true,"
                        + "\"active\":true,\"free_double_next\":false}",
                }));
        }

        [Test]
        public void WinOnMilestone6_OnA10k_PaysBothBaskets_WithTheContentsEscaped()
        {
            StreakService service = Service(Record(5, hits: new[] { 3 }, seeds: 300));

            StreakServiceResult r = service.ApplyWin(30, true, 29, RewardTier.Normal);

            List<string> lines = Lines(service, r, 31);
            Assert.That(lines.Count, Is.EqualTo(3));
            Assert.That(
                lines[0],
                Is.EqualTo("AF_STATE {\"event\":\"streak_milestone\",\"t\":7,\"level\":30,\"milestone\":6,\"reward_type\":\"chest\","
                    + "\"reward_value\":\"{\\\"kind\\\":\\\"streak\\\",\\\"seeds\\\":50,\\\"extra_cuia\\\":1}\"}"),
                "hito 6: la Canasta de la Crecida con el comodín de la del 40 (Q-C5)");
            Assert.That(
                lines[1],
                Is.EqualTo("AF_STATE {\"event\":\"basket_open\",\"t\":7,\"level\":30,"
                    + "\"contents_json\":\"{\\\"kind\\\":\\\"normal\\\",\\\"seeds\\\":50,\\\"hint\\\":1}\"}"),
                "Q-C13: en el 30 se pagan las dos");
            Assert.That(lines[2], Does.StartWith("AF_STATE {\"event\":\"streak_state\",\"t\":7,\"cause\":\"win\",\"level\":31,\"wins\":6,"));
            Assert.That(lines[2], Does.Contain("\"hits\":[3,6],\"seeds_today\":350,"));
        }

        [Test]
        public void WinOnMilestone10_ThenTheRiverWin_HasTheFreeDouble()
        {
            StreakService service = Service(Record(9, hits: new[] { 3, 6 }));

            StreakServiceResult r10 = service.ApplyWin(43, true, 42, RewardTier.Normal);
            List<string> ten = Lines(service, r10, 44);
            Assert.That(ten.Count, Is.EqualTo(2));
            Assert.That(
                ten[0],
                Is.EqualTo("AF_STATE {\"event\":\"streak_milestone\",\"t\":7,\"level\":43,\"milestone\":10,"
                    + "\"reward_type\":\"extra_cuia+rio_crecido\",\"reward_value\":\"1\"}"));
            Assert.That(ten[1], Does.Contain("\"wins\":0,\"river_full\":true,"));
            Assert.That(ten[1], Does.EndWith("\"free_double_next\":true}"), "el río crecido: la siguiente victoria lleva el x2");

            StreakServiceResult river = service.ApplyWin(44, true, 43, RewardTier.Normal);
            Assert.That(river.DoubleHarvestSeeds, Is.GreaterThan(0));
            List<string> next = Lines(service, river, 45);
            Assert.That(next.Count, Is.EqualTo(2));
            Assert.That(
                next[0],
                Is.EqualTo("AF_STATE {\"event\":\"free_double_harvest\",\"t\":7,\"level\":44,\"seeds\":"
                    + river.DoubleHarvestSeeds + "}"));
            Assert.That(next[1], Does.Contain("\"cause\":\"win\",\"level\":45,\"wins\":1,\"river_full\":true,"));
        }

        [Test]
        public void HardFailWithTheShield_FailShieldThenState()
        {
            StreakService service = Service(Record(4, river: true));

            StreakServiceResult r = service.ApplyHardFail(42, true);

            Assert.That(
                Lines(service, r, 42),
                Is.EqualTo(new[]
                {
                    "AF_STATE {\"event\":\"streak_fail\",\"t\":7,\"level\":42,\"wins_before\":4,\"river_full_before\":true,"
                        + "\"shield_used\":true,\"streak_broken\":false,\"river_ended\":false}",
                    "AF_STATE {\"event\":\"streak_shield_used\",\"t\":7,\"level\":42,\"streak_at_use\":4}",
                    "AF_STATE {\"event\":\"streak_state\",\"t\":7,\"cause\":\"hard_fail\",\"level\":42,\"wins\":4,"
                        + "\"river_full\":true,\"shield\":false,\"hits\":[],\"seeds_today\":0,\"date\":\"2026-10-08\","
                        + "\"enabled\":true,\"active\":true,\"free_double_next\":true}",
                }));
        }

        [Test]
        public void HardFailWithoutTheShield_BreaksTheStreak_AndHasNoShieldLine()
        {
            StreakService service = Service(Record(4, river: true, shield: false));

            StreakServiceResult r = service.ApplyHardFail(42, true);

            List<string> lines = Lines(service, r, 42);
            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(
                lines[0],
                Is.EqualTo("AF_STATE {\"event\":\"streak_fail\",\"t\":7,\"level\":42,\"wins_before\":4,\"river_full_before\":true,"
                    + "\"shield_used\":false,\"streak_broken\":true,\"river_ended\":true}"));
            Assert.That(lines[1], Does.Contain("\"cause\":\"hard_fail\",\"level\":42,\"wins\":0,\"river_full\":false,"));
        }

        [Test]
        public void SkipOnA10k_OnlyTheBasket_NoState()
        {
            StreakService service = Service(Record(5, river: true));

            StreakServiceResult r = service.ApplySkip(20, true);

            Assert.That(
                Lines(service, r, 21),
                Is.EqualTo(new[]
                {
                    "AF_STATE {\"event\":\"basket_open\",\"t\":7,\"level\":20,"
                        + "\"contents_json\":\"{\\\"kind\\\":\\\"normal\\\",\\\"seeds\\\":50,\\\"undo\\\":3}\"}",
                }),
                "C11: el Salto no toca la Crecida");
        }

        [Test]
        public void NothingCounted_NoLines()
        {
            StreakService service = Service(Record(2));

            Assert.That(Lines(service, service.ApplyWin(41, false, 40, RewardTier.Normal), 41), Is.Empty, "el Reto no cuenta (Q-C2)");
            Assert.That(Lines(service, service.ApplyHardFail(41, false), 41), Is.Empty);
            Assert.That(Lines(service, service.ApplyHardFail(12, true), 12), Is.Empty, "antes del 25 (C1)");
        }

        [Test]
        public void AMilestoneAlreadyPaidToday_HasNoMilestoneLine()
        {
            StreakService service = Service(Record(2, hits: new[] { 3 }));

            StreakServiceResult r = service.ApplyWin(41, true, 40, RewardTier.Normal);

            List<string> lines = Lines(service, r, 42);
            Assert.That(lines.Count, Is.EqualTo(1), "C6: el hito que salta la regla de una vez al día no se informa");
            Assert.That(lines[0], Does.StartWith("AF_STATE {\"event\":\"streak_state\","));
        }

        [Test]
        public void WithoutTheStateAfter_OnlyTheEvents()
        {
            StreakService service = Service(Record(2));
            StreakServiceResult r = service.ApplyWin(41, true, 40, RewardTier.Normal);
            var lines = new List<string> { "previa" };

            Assert.That(_format.AppendResult(lines, 3, r, null, 42), Is.EqualTo(1));
            Assert.That(lines[0], Is.EqualTo("previa"), "añade al final");
            Assert.That(lines[1], Does.StartWith("AF_STATE {\"event\":\"streak_milestone\",\"t\":3,"));
        }
    }
}
