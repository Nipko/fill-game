#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Guarda contra cifras y nombres inventados (B4; patrón de <see cref="DailyChallengeConfigTests"/>): cada propiedad de
    /// <see cref="LoginCalendarConfig"/> lleva su clave de Remote Config, y su clave, su tipo y su default se leen de la tabla única
    /// de 08 §9, salvo <c>eco_calendar_table_v2</c>, la única clave nueva, que decidió D-033 (añadido del 2026-10-20) y aún no está
    /// en 08 §9 (la da de alta el integrador). Ninguna es un alias retirado, y <c>eco_calendar_double_ad_enabled</c> tiene una sola
    /// fuente en el cliente.
    /// </summary>
    public sealed class LoginCalendarConfigTests
    {
        private static readonly string[] KeysIn08_9 = { "biz_feature_login_calendar", "eco_calendar_locked_booster_seeds" };

        /// <summary>D-033: «clave nueva <c>eco_calendar_table_v2</c> [P]»; se anota en 08 §9 al integrar B4.</summary>
        private const string NewKeyOfD033 = "eco_calendar_table_v2";

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table_ButTheNewKeyOfD033()
        {
            Dictionary<string, RcRow> table = LiveOpsTestFiles.RemoteConfigTable();
            LoginCalendarConfig config = LoginCalendarConfig.Defaults;
            var seen = new List<string>();
            foreach (PropertyInfo property in typeof(LoginCalendarConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                string key = attribute.Key;
                seen.Add(key);
                if (key == NewKeyOfD033)
                {
                    if (table.ContainsKey(key))
                    {
                        Assert.That(table[key].Type, Is.EqualTo("json"), key + ": tipo, ya en 08 §9");
                    }

                    continue;
                }

                Assert.That(table.ContainsKey(key), Is.True, key + " no está en 08 §9 (¿alias retirado o nombre inventado?)");
                RcRow row = table[key];
                object? value = property.GetValue(config);
                if (value is bool b)
                {
                    Assert.That(row.Type, Is.EqualTo("bool"), key);
                    Assert.That(b ? "true" : "false", Is.EqualTo(row.Default), key);
                }
                else if (value is LoginCalendarValueRate rate)
                {
                    Assert.That(row.Type, Is.EqualTo("json"), key);
                    Assert.That(rate.Format(), Is.EqualTo(row.Default), key + ": default");
                    Assert.That(LoginCalendarValueRate.TryParse(row.Default, out LoginCalendarValueRate parsed), Is.True);
                    Assert.That(parsed, Is.EqualTo(rate));
                }
                else
                {
                    Assert.Fail(key + ": tipo sin comprobar");
                }
            }

            seen.Sort(StringComparer.Ordinal);
            var expected = new List<string>(KeysIn08_9) { NewKeyOfD033 };
            expected.Sort(StringComparer.Ordinal);
            Assert.That(seen, Is.EqualTo(expected), "las claves de la nota §9, ni una más");
            Assert.That(table.ContainsKey("eco_calendar_table_v1"), Is.True, "la v1 sigue en 08 §9 de respaldo (D-033)");
        }

        [Test]
        public void EverySetting_CarriesItsRemoteConfigKey()
        {
            foreach (PropertyInfo property in typeof(LoginCalendarConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    Assert.That(property.GetCustomAttribute<RemoteConfigKeyAttribute>(), Is.Not.Null, property.Name);
                }
            }
        }

        [Test]
        public void NoKey_IsARetiredAlias()
        {
            // Alias retirados de 08 §9 que tocan la Bitácora o la cola de takeovers (05 y 06, Parámetros).
            string[] retiredKeys = { "calendar_table_v1", "logbook_*", "max_takeovers_per_session", "offer_takeovers_per_session" };
            string retired = LiveOpsTestFiles.RetiredAliases();
            foreach (string alias in retiredKeys)
            {
                StringAssert.Contains("`" + alias + "`", retired, alias + " figura entre los alias retirados de 08 §9");
            }

            foreach (PropertyInfo property in typeof(LoginCalendarConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (attribute != null)
                {
                    Assert.That(attribute.Key.StartsWith("logbook_", StringComparison.Ordinal), Is.False, attribute.Key);
                    Assert.That(Array.IndexOf(retiredKeys, attribute.Key), Is.EqualTo(-1), attribute.Key + " es un alias retirado");
                }
            }
        }

        [Test]
        public void TheLogbookKeysWithAnotherOwner_HaveASingleSourceInTheClient()
        {
            // eco_calendar_double_ad_enabled es de AF.Ads (rv_daily_chest); live_unlock_level_daily, de DailyChallengeConfig.
            Assert.That(Declaring("eco_calendar_double_ad_enabled"), Is.EqualTo(new[] { "AdsConfig.cs" }));
            Assert.That(Declaring("live_unlock_level_daily"), Is.EqualTo(new[] { "DailyChallengeConfig.cs" }));
            Assert.That(Declaring("biz_feature_login_calendar"), Is.EqualTo(new[] { "LoginCalendarConfig.cs" }));
            Assert.That(Declaring("eco_calendar_locked_booster_seeds"), Is.EqualTo(new[] { "LoginCalendarConfig.cs" }));
        }

        [Test]
        public void Defaults_IsAFreshInstance_AndValidateRejectsNonsense()
        {
            LoginCalendarConfig a = LoginCalendarConfig.Defaults;
            a.BizFeatureLoginCalendar = false;
            Assert.That(LoginCalendarConfig.Defaults.BizFeatureLoginCalendar, Is.True);
            Assert.That(LoginCalendarConfig.Defaults.EcoCalendarTableV2, Is.Not.SameAs(LoginCalendarConfig.Defaults.EcoCalendarTableV2));
            LoginCalendarConfig.Defaults.Validate();

            var bad = new List<Action<LoginCalendarConfig>>
            {
                c => c.EcoCalendarTableV2 = null!,
                c => c.EcoCalendarLockedBoosterSeeds = new LoginCalendarValueRate(0, 100),
                c => c.EcoCalendarLockedBoosterSeeds = new LoginCalendarValueRate(30, -1),
                c => c.EcoCalendarLockedBoosterSeeds = new LoginCalendarValueRate(10, 100), // la tabla deja de tener opciones de igual valor
            };

            for (int i = 0; i < bad.Count; i++)
            {
                LoginCalendarConfig config = LoginCalendarConfig.Defaults;
                bad[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "caso " + i);
            }
        }

        private static string[] Declaring(string key)
        {
            string marker = "[RemoteConfigKey(\"" + key + "\")]";
            var files = new List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(LiveOpsTestFiles.RepoRoot, "Assets", "_Project", "Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(file).Contains(marker))
                {
                    files.Add(Path.GetFileName(file));
                }
            }

            files.Sort(StringComparer.Ordinal);
            return files.ToArray();
        }
    }
}
