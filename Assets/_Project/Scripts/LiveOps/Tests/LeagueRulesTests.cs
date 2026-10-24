#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Reglas de la Liga del río de la 1.0 (D-046; liga.md §1; 06 §6): la semana de lunes 12:00 UTC a lunes 12:00 UTC con su id ISO,
    /// los puntos de la línea de la victoria, el desbloqueo y los <c>win_id</c>; y las cifras de <see cref="LeagueConfig"/> contra la
    /// tabla única de 08 §9.
    /// </summary>
    public sealed class LeagueRulesTests
    {
        private static DateTime Utc(int y, int mo, int d, int h = 0, int mi = 0) => new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);

        [Test]
        public void TheWeek_GoesFromMondayNoonUtc_ToMondayNoonUtc()
        {
            Assert.That(LeagueRules.WeekStart(Utc(2026, 10, 23, 20)), Is.EqualTo(Utc(2026, 10, 19, 12)), "viernes: el lunes de esa semana");
            Assert.That(LeagueRules.WeekStart(Utc(2026, 10, 26, 11, 59)), Is.EqualTo(Utc(2026, 10, 19, 12)), "lunes antes de las 12: la semana anterior");
            Assert.That(LeagueRules.WeekStart(Utc(2026, 10, 26, 12)), Is.EqualTo(Utc(2026, 10, 26, 12)), "lunes a las 12: la nueva");
            Assert.That(LeagueRules.WeekStart(Utc(2026, 10, 25, 23, 59)), Is.EqualTo(Utc(2026, 10, 19, 12)), "domingo");
            Assert.That(LeagueRules.WeekEnd(Utc(2026, 10, 19, 12)), Is.EqualTo(Utc(2026, 10, 26, 12)));
        }

        [Test]
        public void TheWeekId_IsTheIsoWeekOfItsMonday_AsInTheContractExample()
        {
            Assert.That(LeagueRules.WeekId(Utc(2026, 10, 23, 20)), Is.EqualTo("2026-W43"), "liga.md §3.1: starts_at 2026-10-19 → 2026-W43");
            Assert.That(LeagueRules.WeekId(Utc(2026, 10, 26, 11)), Is.EqualTo("2026-W43"));
            Assert.That(LeagueRules.WeekId(Utc(2026, 10, 26, 12)), Is.EqualTo("2026-W44"));
            Assert.That(LeagueRules.WeekId(Utc(2026, 12, 29, 12)), Is.EqualTo("2026-W53"), "2026 tiene 53 semanas ISO");
            Assert.That(LeagueRules.WeekId(Utc(2027, 1, 4, 12)), Is.EqualTo("2027-W01"));
            Assert.That(LeagueRules.WeekId(Utc(2027, 1, 4, 11)), Is.EqualTo("2026-W53"), "antes del cambio sigue la semana del 28-12");
        }

        [Test]
        public void ThePoints_AreThoseOf06_6()
        {
            Assert.That(LeagueRules.CampaignPoints(RewardTier.Normal), Is.EqualTo(10));
            Assert.That(LeagueRules.CampaignPoints(RewardTier.Hard), Is.EqualTo(20));
            Assert.That(LeagueRules.CampaignPoints(RewardTier.VeryHard), Is.EqualTo(30));
            Assert.That(LeagueRules.PointsDaily, Is.EqualTo(20));

            // La misma tabla que league_points de 08 §9 (la de la 1.2), para que la línea de la victoria no diga otra cosa.
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(LiveOpsTestFiles.RepoRoot, "Assets", "_Project", "Data", "Config", "RemoteConfigDefaults.json"));
            int at = json.IndexOf("\"league_points\"", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0), "league_points en los defaults generados de 08 §9");
            string points = json.Substring(at, Math.Min(400, json.Length - at));
            StringAssert.Contains("\"normal\": 10", points);
            StringAssert.Contains("\"hard\": 20", points);
            StringAssert.Contains("\"very_hard\": 30", points);
            StringAssert.Contains("\"daily\": 20", points);
        }

        [Test]
        public void TheUnlock_IsLiveLeagueUnlockLevel()
        {
            var config = LeagueConfig.Defaults;
            Assert.That(LeagueRules.IsUnlocked(40, config), Is.False);
            Assert.That(LeagueRules.IsUnlocked(41, config), Is.True, "aparece al ganar el 41");
            config.LiveLeagueUnlockLevel = 50;
            Assert.That(LeagueRules.IsUnlocked(41, config), Is.False);
        }

        [Test]
        public void TheWinIds_AreIdempotentKeys()
        {
            Assert.That(LeagueRules.CampaignWinId(57), Is.EqualTo("c:57"));
            Assert.That(LeagueRules.DailyWinId("2026-10-26"), Is.EqualTo("d:2026-10-26"));
            Assert.Throws<ArgumentException>(() => LeagueRules.DailyWinId("26/10/2026"));
            Assert.Throws<ArgumentOutOfRangeException>(() => LeagueRules.CampaignWinId(0));
        }

        [Test]
        public void TheNames_AreThe24OfTheSpec_InOrder()
        {
            Assert.That(LeagueRules.NameKeys.Length, Is.EqualTo(24));
            Assert.That(LeagueRules.NameKeys[0], Is.EqualTo("league_name_capybara"));
            Assert.That(LeagueRules.NameKeys[23], Is.EqualTo("league_name_arapaima"));
            Assert.That(LeagueRules.IsKnownName("league_name_otter"), Is.True);
            Assert.That(LeagueRules.IsKnownName("league_name_quetzal"), Is.False);
        }

        [Test]
        public void TheRegion_IsNormalizedToIso3166Alpha2()
        {
            Assert.That(LeagueRules.NormalizeRegion("co"), Is.EqualTo("CO"));
            Assert.That(LeagueRules.NormalizeRegion("BR"), Is.EqualTo("BR"));
            Assert.That(LeagueRules.NormalizeRegion("COL"), Is.Null);
            Assert.That(LeagueRules.NormalizeRegion("1A"), Is.Null);
            Assert.That(LeagueRules.NormalizeRegion(null), Is.Null);
        }

        [Test]
        public void TheConfig_IsThe08_9Table()
        {
            Dictionary<string, RcRow> table = LiveOpsTestFiles.RemoteConfigTable();
            LeagueConfig config = LeagueConfig.Defaults;
            var seen = new List<string>();
            foreach (PropertyInfo property in typeof(LeagueConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (property.CanWrite)
                {
                    Assert.That(attribute, Is.Not.Null, property.Name);
                }

                if (attribute == null)
                {
                    continue;
                }

                string key = attribute.Key;
                seen.Add(key);
                Assert.That(table.ContainsKey(key), Is.True, key + " no está en 08 §9");
                object? value = property.GetValue(config);
                if (value is bool b)
                {
                    Assert.That(table[key].Type, Is.EqualTo("bool"), key);
                    Assert.That(b ? "true" : "false", Is.EqualTo(table[key].Default), key);
                }
                else
                {
                    Assert.That(table[key].Type, Is.EqualTo("int"), key);
                    Assert.That(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(table[key].Default), key);
                }
            }

            seen.Sort(StringComparer.Ordinal);
            Assert.That(seen, Is.EqualTo(new[] { "live_league_cache_minutes", "live_league_enabled", "live_league_unlock_level" }), "liga.md §5");
            Assert.That(table.ContainsKey("live_unlock_level_league"), Is.False, "alias retirado por live_league_unlock_level (D-046)");
            Assert.That(LiveOpsTestFiles.RetiredAliases(), Does.Contain("`live_unlock_level_league`"));
            Assert.That(config.LiveLeagueEnabled, Is.False, "apagada hasta que haya backend en prod (D-046)");
        }

        [Test]
        public void TheConfig_ValidateRejectsNonsense()
        {
            LeagueConfig.Defaults.Validate();
            Assert.Throws<InvalidOperationException>(() => new LeagueConfig { LiveLeagueUnlockLevel = 0 }.Validate());
            Assert.Throws<InvalidOperationException>(() => new LeagueConfig { LiveLeagueCacheMinutes = -1 }.Validate());
        }
    }
}
