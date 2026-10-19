#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Guarda contra cifras y nombres inventados (R7a; patrón de <c>EconomyConfigTests</c>): cada propiedad de
    /// <see cref="DailyChallengeConfig"/> lleva su clave de Remote Config, y su clave, su tipo y su default se leen de la tabla
    /// única de GDD 08 §9. Además: ninguna clave es un alias retirado, validación y guardas del ensamblado <c>AF.LiveOps</c>.
    /// </summary>
    public sealed class DailyChallengeConfigTests
    {
        // Las 9 claves de la nota docs/dev/reto-del-rio.md §11.
        private static readonly string[] ExpectedKeys =
        {
            "biz_feature_daily_challenge", "live_unlock_level_daily", "lvl_daily_bracket_levels", "live_daily_offset",
            "daily_overrides", "live_daily_challenge_reward", "live_daily_streak7_reward", "ui_murky_bonus_enabled",
            "sec_clock_max_drift_h",
        };

        // Alias retirados de 08 §9 que tocan el Reto o el anti-reloj (06, Parámetros; 04 §7.1).
        private static readonly string[] RetiredKeys =
        {
            "daily_offset", "daily_reward_seeds", "daily_murky_bonus_multiplier", "daily_streak7_reward", "daily_bands",
            "daily_bracket_levels", "daily_challenge_seeds", "canoes_clock_max_drift_h",
        };

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, RcRow> table = LiveOpsTestFiles.RemoteConfigTable();
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;

            var seen = new List<string>();
            foreach (PropertyInfo property in typeof(DailyChallengeConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                string key = attribute.Key;
                Assert.That(table.ContainsKey(key), Is.True, key + " no está en 08 §9 (¿alias retirado o nombre inventado?)");
                RcRow row = table[key];
                Assert.That(row.Type, Is.EqualTo(GddType(property.PropertyType)), key + ": tipo");
                Assert.That(row.Default, Is.Not.Null, key + ": no se pudo leer el default");
                Assert.That(Format(property.GetValue(config)), Is.EqualTo(row.Default), key + ": default");
                seen.Add(key);
            }

            seen.Sort(StringComparer.Ordinal);
            var expected = new List<string>(ExpectedKeys);
            expected.Sort(StringComparer.Ordinal);
            Assert.That(seen, Is.EqualTo(expected), "las 9 claves de la nota §11, ni una más");
        }

        [Test]
        public void NoKey_IsARetiredAlias()
        {
            string retired = LiveOpsTestFiles.RetiredAliases();
            foreach (string alias in RetiredKeys)
            {
                StringAssert.Contains("`" + alias + "`", retired, alias + " figura entre los alias retirados de 08 §9");
            }

            // El párrafo también nombra claves vigentes (los destinos tras «→» y «`live_daily_challenge_reward` = 50», un default
            // antiguo): por eso se contrasta con la lista explícita y no con todo el párrafo.
            var retiredSet = new HashSet<string>(RetiredKeys, StringComparer.Ordinal);
            foreach (PropertyInfo property in typeof(DailyChallengeConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (attribute != null)
                {
                    Assert.That(retiredSet.Contains(attribute.Key), Is.False, attribute.Key + " es un alias retirado");
                }
            }
        }

        [Test]
        public void EverySetting_CarriesItsRemoteConfigKey()
        {
            foreach (PropertyInfo property in typeof(DailyChallengeConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    Assert.That(property.GetCustomAttribute<RemoteConfigKeyAttribute>(), Is.Not.Null,
                        property.Name + ": toda cifra configurable sale de 08 §9");
                }
            }
        }

        [Test]
        public void Defaults_IsAFreshInstanceEachTime()
        {
            DailyChallengeConfig a = DailyChallengeConfig.Defaults;
            a.LiveDailyOffset = 5;
            a.LiveUnlockLevelDaily = 1;

            Assert.That(DailyChallengeConfig.Defaults.LiveDailyOffset, Is.EqualTo(0));
            Assert.That(DailyChallengeConfig.Defaults.LiveUnlockLevelDaily, Is.EqualTo(10));
            Assert.That(DailyChallengeConfig.Defaults.ClockMaxDrift, Is.EqualTo(TimeSpan.FromHours(24)));
        }

        [Test]
        public void Validate_RejectsNonsense()
        {
            DailyChallengeConfig.Defaults.Validate();

            var bad = new List<Action<DailyChallengeConfig>>
            {
                c => c.LiveUnlockLevelDaily = 0,
                c => c.LvlDailyBracketLevels = new[] { 41 },
                c => c.LvlDailyBracketLevels = new[] { 201, 41 },
                c => c.LvlDailyBracketLevels = new[] { 0, 41 },
                c => c.LvlDailyBracketLevels = null!,
                c => c.DailyOverrides = null!,
                c => c.LiveDailyChallengeReward = new DailyChallengeReward(-1, 2),
                c => c.LiveDailyChallengeReward = new DailyChallengeReward(60, 0),
                c => c.LiveDailyStreak7Reward = "",
                c => c.SecClockMaxDriftH = 0,
            };

            for (int i = 0; i < bad.Count; i++)
            {
                DailyChallengeConfig config = DailyChallengeConfig.Defaults;
                bad[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "caso " + i);
            }
        }

        [Test]
        public void SecClockMaxDriftH_HasASingleSourceInTheClient()
        {
            // Nota §11: «clave global; si otro config la declara, una sola fuente». Ningún otro archivo de Scripts la marca.
            string scripts = Path.Combine(LiveOpsTestFiles.RepoRoot, "Assets", "_Project", "Scripts");
            string marker = "[RemoteConfigKey(\"" + "sec_clock_max_drift_h" + "\")]";
            var declaring = new List<string>();
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(file).Contains(marker))
                {
                    declaring.Add(Path.GetFileName(file));
                }
            }

            Assert.That(declaring, Is.EqualTo(new[] { "DailyChallengeConfig.cs" }));
        }

        [Test]
        public void LiveOpsAssembly_HasNoEngine_TheReferencesOfTheNote_AndNoSystemClock()
        {
            string asmdef = File.ReadAllText(Path.Combine(LiveOpsTestFiles.LiveOpsDir, "AF.LiveOps.asmdef"));
            StringAssert.Contains("\"noEngineReferences\": true", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);
            foreach (string reference in new[] { "\"AF.Core\"", "\"AF.Levels\"", "\"AF.Platform\"", "\"AF.Save\"", "\"AF.Economy\"" })
            {
                StringAssert.Contains(reference, asmdef);
            }

            foreach (string forbidden in new[] { "AF.GameFlow", "AF.UI", "AF.Presentation", "AF.Boosters", "Unity" })
            {
                Assert.That(asmdef.Contains(forbidden), Is.False, "AF.LiveOps no debe referenciar " + forbidden);
            }

            // Se componen por partes para que este archivo no se denuncie a sí mismo.
            string[] banned =
            {
                "DateTime" + ".Now", "DateTime" + ".UtcNow", "DateTime" + ".Today", "DateTimeOffset" + ".Now",
                "DateTimeOffset" + ".UtcNow", "Environment" + ".TickCount", "Stop" + "watch", "System" + ".Random",
                "using " + "Unity",
            };
            string tests = Path.Combine(LiveOpsTestFiles.LiveOpsDir, "Tests") + Path.DirectorySeparatorChar;
            int scanned = 0;
            foreach (string file in Directory.GetFiles(LiveOpsTestFiles.LiveOpsDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.StartsWith(tests, StringComparison.Ordinal))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word + " (la hora sale de IClock)");
                }

                scanned++;
            }

            Assert.That(scanned, Is.GreaterThanOrEqualTo(3));
        }

        private static string GddType(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(IReadOnlyList<int>) || type == typeof(DailyOverrides) || type == typeof(DailyChallengeReward))
            {
                return "json";
            }

            throw new NotSupportedException(type.Name);
        }

        private static string Format(object? value)
        {
            switch (value)
            {
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case bool b: return b ? "true" : "false";
                case string s: return s;
                case IReadOnlyList<int> thresholds: return DailyChallengeConfig.FormatThresholds(thresholds);
                case DailyOverrides overrides: return overrides.Format();
                case DailyChallengeReward reward: return reward.Format();
                default: throw new NotSupportedException(value?.GetType().Name ?? "null");
            }
        }
    }
}
