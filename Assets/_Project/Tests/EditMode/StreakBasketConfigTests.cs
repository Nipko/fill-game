#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Guarda contra cifras y nombres inventados (B1a, A1; patrón de <see cref="EconomyConfigTests"/>): cada propiedad de
    /// <see cref="StreakConfig"/> y <see cref="BasketConfig"/> lleva su clave de Remote Config, y su clave, su tipo y su default se
    /// leen de la tabla única de GDD 08 §9. Además: ningún alias retirado, lectura de los dos JSON y validación con los rangos
    /// de 08 §9 (nota <c>docs/dev/crecida-canasta.md</c> §4).
    /// </summary>
    public sealed class StreakBasketConfigTests
    {
        private static readonly string[] ExpectedKeys =
        {
            "biz_feature_win_streak", "streak_unlock_level", "streak_milestones", "streak_hits_once_per_day", "streak_daily_cap",
            "streak_shield_per_day", "streak_restart_counts_as_fail", "streak_reminder_hours", "eco_basket_cycle", "eco_basket_seeds",
        };

        // Alias retirados de 08 §9 que tocan la Crecida o la Canasta (06 y 05, Parámetros).
        private static readonly string[] RetiredKeys =
        {
            "eco_streak_rewards", "streak_daily_cap_seeds", "streak_shields_per_day", "streak_hit3_seeds", "streak_hit10_seeds",
            "unlock_level_streak", "chest_cycle", "chest_seeds", "chest_first_seeds", "eco_basket_table",
        };

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, Row> table = ReadTable();
            var seen = new List<string>();
            foreach (object instance in new object[] { StreakConfig.Defaults, BasketConfig.Defaults })
            {
                foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                    if (attribute == null)
                    {
                        continue;
                    }

                    string key = attribute.Key;
                    Assert.That(table.ContainsKey(key), Is.True, key + " no está en 08 §9 (¿alias retirado o nombre inventado?)");
                    Row row = table[key];
                    Assert.That(row.Type, Is.EqualTo(GddType(property.PropertyType)), key + ": tipo");
                    Assert.That(row.Default, Is.Not.Null, key + ": no se pudo leer el default");
                    Assert.That(Format(property.GetValue(instance)), Is.EqualTo(row.Default), key + ": default");
                    seen.Add(key);
                }
            }

            seen.Sort(StringComparer.Ordinal);
            var expected = new List<string>(ExpectedKeys);
            expected.Sort(StringComparer.Ordinal);
            Assert.That(seen, Is.EqualTo(expected), "las 10 claves de la nota §4, ni una más");
        }

        [Test]
        public void TheValues_AreTheOnesTheNoteNames()
        {
            StreakConfig s = StreakConfig.Defaults;
            Assert.That(s.BizFeatureWinStreak, Is.True);
            Assert.That(s.StreakUnlockLevel, Is.EqualTo(25), "06 §4.3; 04 §4");
            Assert.That(s.StreakMilestones.Hit3Seeds, Is.EqualTo(300), "05 §2.4");
            Assert.That(s.StreakHitsOncePerDay, Is.True);
            Assert.That(s.StreakDailyCap, Is.EqualTo(600));
            Assert.That(s.StreakShieldPerDay, Is.EqualTo(1));
            Assert.That(s.StreakRestartCountsAsFail, Is.False, "04 §5.2: el reinicio voluntario nunca rompe la Crecida");
            Assert.That(s.StreakReminderHours, Is.EqualTo(36));

            BasketConfig b = BasketConfig.Defaults;
            Assert.That(b.EcoBasketSeeds, Is.EqualTo(50));
            Assert.That(b.EcoBasketCycle, Is.EqualTo(new[] { "undo3", "hint", "extra_cuia", "undo3", "hint" }), "06 §4.4");
        }

        [Test]
        public void EverySetting_CarriesItsRemoteConfigKey()
        {
            foreach (Type type in new[] { typeof(StreakConfig), typeof(BasketConfig) })
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanWrite)
                    {
                        Assert.That(property.GetCustomAttribute<RemoteConfigKeyAttribute>(), Is.Not.Null,
                            type.Name + "." + property.Name + ": toda cifra configurable sale de 08 §9");
                    }
                }
            }
        }

        [Test]
        public void NoKey_IsARetiredAlias()
        {
            string retired = RetiredAliases();
            foreach (string alias in RetiredKeys)
            {
                StringAssert.Contains("`" + alias + "`", retired, alias + " figura entre los alias retirados de 08 §9");
                Assert.That(Array.IndexOf(ExpectedKeys, alias), Is.EqualTo(-1), alias);
            }
        }

        [Test]
        public void Defaults_IsAFreshInstanceEachTime()
        {
            StreakConfig a = StreakConfig.Defaults;
            a.StreakDailyCap = 1;
            BasketConfig b = BasketConfig.Defaults;
            b.EcoBasketSeeds = 1;
            ((string[])b.EcoBasketCycle)[0] = "hint";

            Assert.That(StreakConfig.Defaults.StreakDailyCap, Is.EqualTo(600));
            Assert.That(BasketConfig.Defaults.EcoBasketSeeds, Is.EqualTo(50));
            Assert.That(BasketConfig.Defaults.EcoBasketCycle[0], Is.EqualTo("undo3"), "el ciclo por defecto no se comparte");
        }

        [Test]
        public void StreakMilestones_ReadsTheDefault_AndRejectsAnythingElse()
        {
            Assert.That(StreakMilestones.TryParse("{\"3\":300,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}", out StreakMilestones m), Is.True);
            Assert.That(m.Hit3Seeds, Is.EqualTo(300));
            Assert.That(StreakMilestones.TryParse(" { \"10\" : \"extra_cuia+rio_crecido\" , \"3\" : 200 , \"6\" : \"chest\" } ", out m), Is.True);
            Assert.That(m.Hit3Seeds, Is.EqualTo(200), "orden y espacios libres; 200 es el mínimo del rango");
            Assert.That(m.Format(), Is.EqualTo("{\"3\":200,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}"));

            string[] bad =
            {
                null!, "", "[]", "{}", "{\"3\":300}",
                "{\"3\":199,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}",
                "{\"3\":301,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}",
                "{\"3\":\"300\",\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}",
                "{\"3\":300.5,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}",
                "{\"3\":300,\"6\":\"canasta\",\"10\":\"extra_cuia+rio_crecido\"}",
                "{\"3\":300,\"6\":\"chest\",\"10\":300}",
                "{\"3\":300,\"6\":\"chest\",\"10\":\"extra_cuia\"}",
                "{\"3\":300,\"6\":\"chest\",\"9\":\"extra_cuia+rio_crecido\"}",
                "{\"3\":300,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\",\"12\":1}",
                "{\"3\":300,\"3\":300,\"6\":\"chest\"}",
                "{\"3\":300,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"} x",
            };
            foreach (string json in bad)
            {
                Assert.That(StreakMilestones.TryParse(json, out m), Is.False, json ?? "null");
                Assert.That(m, Is.EqualTo(StreakMilestones.Default), "un valor rechazado deja el default de 08 §9: " + json);
            }
        }

        [Test]
        public void TheJsonReader_DecodesUnicodeEscapes()
        {
            Assert.That(BasketConfig.TryParseCycle("[\"\\u0068int\",\"undo\\u0033\"]", out IReadOnlyList<string> cycle), Is.True);
            Assert.That(cycle, Is.EqualTo(new[] { "hint", "undo3" }), "\\u0068 = h, \\u0033 = 3");
            Assert.That(StreakMilestones.TryParse("{\"\\u0033\":300,\"6\":\"\\u0063hest\",\"10\":\"extra_cuia+rio_crecido\"}", out StreakMilestones m), Is.True);
            Assert.That(m.Hit3Seeds, Is.EqualTo(300));

            foreach (string bad in new[] { "[\"\\u00\"]", "[\"\\u00zz\"]", "[\"\\x41\"]", "[\"hint\\\"]" })
            {
                Assert.That(BasketConfig.TryParseCycle(bad, out _), Is.False, bad);
            }
        }

        [Test]
        public void BasketCycle_ReadsTokens_AndRejectsUnknownOnes()
        {
            Assert.That(BasketConfig.TryParseCycle("[\"undo3\",\"hint\",\"extra_cuia\",\"undo3\",\"hint\"]", out IReadOnlyList<string> cycle), Is.True);
            Assert.That(BasketConfig.FormatCycle(cycle), Is.EqualTo("[\"undo3\",\"hint\",\"extra_cuia\",\"undo3\",\"hint\"]"));
            Assert.That(BasketConfig.TryParseCycle("[ \"reveal\", \"shuffle2\" ]", out cycle), Is.True);
            Assert.That(cycle, Is.EqualTo(new[] { "reveal", "shuffle2" }));

            foreach (string json in new[] { "", "[]", "{}", "[\"undo0\"]", "[\"skip\"]", "[\"undo100\"]", "[\"Undo3\"]", "[3]", "[\"hint\"" })
            {
                Assert.That(BasketConfig.TryParseCycle(json, out cycle), Is.False, json);
                Assert.That(cycle, Is.EqualTo(new[] { "undo3", "hint", "extra_cuia", "undo3", "hint" }), "default de 08 §9: " + json);
            }
        }

        [Test]
        public void Validate_UsesTheRangesOf08_9()
        {
            StreakConfig.Defaults.Validate();
            BasketConfig.Defaults.Validate();

            var okStreak = new List<Action<StreakConfig>>
            {
                c => c.StreakShieldPerDay = 2, // Q-C6: admitido por el rango, aunque en la 1.0 da una sola hoja
                c => c.StreakUnlockLevel = 20,
                c => c.StreakUnlockLevel = 40,
                c => c.StreakDailyCap = 300,
                c => c.StreakDailyCap = 900,
                c => c.StreakMilestones = new StreakMilestones(200),
                c => c.StreakReminderHours = 24,
                c => c.StreakReminderHours = 48,
            };
            foreach (Action<StreakConfig> change in okStreak)
            {
                StreakConfig config = StreakConfig.Defaults;
                change(config);
                config.Validate();
            }

            var badStreak = new List<Action<StreakConfig>>
            {
                c => c.StreakRestartCountsAsFail = true, // 04 §5.2
                c => c.StreakShieldPerDay = 0,
                c => c.StreakShieldPerDay = 3,
                c => c.StreakUnlockLevel = 19,
                c => c.StreakUnlockLevel = 41,
                c => c.StreakDailyCap = 299,
                c => c.StreakDailyCap = 901,
                c => c.StreakMilestones = new StreakMilestones(301),
                c => c.StreakReminderHours = 23,
                c => c.StreakReminderHours = 49,
            };
            for (int i = 0; i < badStreak.Count; i++)
            {
                StreakConfig config = StreakConfig.Defaults;
                badStreak[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "Crecida, caso " + i);
            }

            var badBasket = new List<Action<BasketConfig>>
            {
                c => c.EcoBasketSeeds = 29,
                c => c.EcoBasketSeeds = 51,
                c => c.EcoBasketCycle = new string[0],
                c => c.EcoBasketCycle = new[] { "undo3", "skip" },
            };
            for (int i = 0; i < badBasket.Count; i++)
            {
                BasketConfig config = BasketConfig.Defaults;
                badBasket[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "Canasta, caso " + i);
            }
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

            if (type == typeof(StreakMilestones) || type == typeof(IReadOnlyList<string>))
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
                case StreakMilestones m: return m.Format();
                case IReadOnlyList<string> cycle: return BasketConfig.FormatCycle(cycle);
                default: throw new NotSupportedException(value?.GetType().Name ?? "null");
            }
        }

        private static string Chapter08()
        {
            string path = Path.Combine(RepoRoot, "docs", "gdd", "08-arquitectura-tecnica-publicacion.md");
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static string RetiredAliases()
        {
            string text = Chapter08();
            int start = text.IndexOf("**Alias retirados**", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "no encuentro los alias retirados de 08 §9");
            return text.Substring(start, text.IndexOf('\n', start) - start);
        }

        /// <summary>
        /// Filas de 08 §9 (<c>| `clave` / `clave2` | tipo / tipo2 | default / default2 | rango | dueño | uso |</c>), como
        /// <see cref="EconomyConfigTests"/>; además, una clave con plantilla (<c>biz_feature_&lt;sistema&gt;</c>) seguida de sufijos
        /// entre comillas invertidas se expande en una clave por sufijo, como hace el lector de <c>AF.LiveOps.Tests</c>.
        /// </summary>
        private static Dictionary<string, Row> ReadTable()
        {
            string text = Chapter08();
            int start = text.IndexOf("\n## 9. ", StringComparison.Ordinal);
            int end = text.IndexOf("\n## 10. ", start + 1, StringComparison.Ordinal);
            Assert.That(start >= 0 && end > start, Is.True, "no encuentro la sección 9 de 08");

            var table = new Dictionary<string, Row>(StringComparer.Ordinal);
            foreach (string line in text.Substring(start, end - start).Split('\n'))
            {
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                var keys = new List<string>();
                foreach (Match m in Regex.Matches(cells[1], "`([^`]+)`"))
                {
                    keys.Add(m.Groups[1].Value);
                }

                Match template = Regex.Match(keys[0], "<[^>]+>");
                if (template.Success)
                {
                    string pattern = keys[0];
                    keys.RemoveAt(0);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        keys[i] = pattern.Replace(template.Value, keys[i]);
                    }
                }

                string[] types = SplitValues(cells[2]);
                string[] defaults = SplitValues(cells[3]);
                for (int i = 0; i < keys.Count; i++)
                {
                    string type = types.Length == keys.Count ? types[i] : types[0];
                    string? value = defaults.Length == keys.Count ? defaults[i] : defaults.Length == 1 ? defaults[0] : null;
                    table[keys[i]] = new Row(type, value);
                }
            }

            Assert.That(table.Count, Is.GreaterThan(100), "la tabla de 08 §9 se leyó entera");
            return table;
        }

        private static string[] SplitValues(string cell)
        {
            string[] parts = cell.Split(new[] { " / " }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Replace("**", "").Trim().Trim('`').Trim('"').Trim();
            }

            return parts;
        }

        private readonly struct Row
        {
            public Row(string type, string? value)
            {
                Type = type;
                Default = value;
            }

            public string Type { get; }

            public string? Default { get; }
        }
    }
}
