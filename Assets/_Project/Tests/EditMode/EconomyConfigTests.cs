#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AF.Economy;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Guarda contra cifras inventadas (B1-2): cada propiedad de <see cref="EconomyConfig"/> lleva su clave de Remote Config
    /// y su default, su tipo y su clave se leen de la tabla única de GDD 08 §9 (<c>docs/gdd/08-…md</c>). Además: tramos del
    /// rescate, validación y guardas del ensamblado <c>AF.Economy</c> (sin motor, sin reloj del sistema).
    /// </summary>
    public sealed class EconomyConfigTests
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string EconomyDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "Economy");

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, RcRow> table = ReadRemoteConfigTable();
            Assert.That(table.Count, Is.GreaterThan(100), "la tabla de 08 §9 se leyó entera");
            EconomyConfig config = EconomyConfig.Defaults;

            int checkedKeys = 0;
            foreach (PropertyInfo property in typeof(EconomyConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
                checkedKeys++;
            }

            Assert.That(checkedKeys, Is.EqualTo(18));
        }

        [Test]
        public void EverySetting_CarriesItsRemoteConfigKey()
        {
            foreach (PropertyInfo property in typeof(EconomyConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
            EconomyConfig a = EconomyConfig.Defaults;
            a.HintSeedPrice = 1;

            Assert.That(EconomyConfig.Defaults.HintSeedPrice, Is.EqualTo(100));
        }

        [Test]
        public void RescueBands_Give600From31_And300ForTheFirstOfTheDay()
        {
            EconomyConfig config = EconomyConfig.Defaults;

            Assert.That(config.RescueBasePrice(1), Is.Null);
            Assert.That(config.RescueBasePrice(30), Is.Null);
            Assert.That(config.RescueBasePrice(31), Is.EqualTo(600));
            Assert.That(config.RescueBasePrice(5000), Is.EqualTo(600), "un solo tramo: sin 750/900 (08 §9, alias retirados)");
            Assert.That(config.RescueFirstOfDayPrice(600), Is.EqualTo(300));
            Assert.That(EconomyConfig.FormatBands(config.RescuePriceBands), Is.EqualTo("[[31,600]]"));

            config.RescuePriceBands = new[] { new PriceBand(31, 600), new PriceBand(101, 750) };
            Assert.That(config.RescueBasePrice(100), Is.EqualTo(600));
            Assert.That(config.RescueBasePrice(101), Is.EqualTo(750));
            Assert.That(config.RescueFirstOfDayPrice(750), Is.EqualTo(375));
        }

        [Test]
        public void WinRewardAndPrices_ComeFromTheConfig()
        {
            EconomyConfig config = EconomyConfig.Defaults;

            Assert.That(config.WinReward(RewardTier.Normal), Is.EqualTo(20));
            Assert.That(config.WinReward(RewardTier.Hard), Is.EqualTo(60));
            Assert.That(config.WinReward(RewardTier.VeryHard), Is.EqualTo(80));
            Assert.That(config.SeedPrice(SeedSink.CanoesRefill), Is.EqualTo(200));
            Assert.That(config.CanoesRegen, Is.EqualTo(TimeSpan.FromMinutes(25)));
            Assert.Throws<ArgumentException>(() => config.SeedPrice(SeedSink.ContinueCuia));
        }

        [Test]
        public void Validate_RejectsNonsense()
        {
            EconomyConfig.Defaults.Validate();

            var bad = new List<Action<EconomyConfig>>
            {
                c => c.HintSeedPrice = 0,
                c => c.EcoRewardNormal = -1,
                c => c.RescueFirstDayMult = 0,
                c => c.RescueFirstDayMult = 1.5,
                c => c.CanoesMax = 0,
                c => c.CanoesRegenMin = 0,
                c => c.UndoRefillAmount = 0,
                c => c.RescuePriceBands = new PriceBand[0],
                c => c.RescuePriceBands = new[] { new PriceBand(101, 750), new PriceBand(31, 600) },
            };

            for (int i = 0; i < bad.Count; i++)
            {
                EconomyConfig config = EconomyConfig.Defaults;
                bad[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "caso " + i);
            }
        }

        [Test]
        public void EconomyAssembly_HasNoEngine_NoUpwardReferences_AndNoSystemClock()
        {
            string asmdef = File.ReadAllText(Path.Combine(EconomyDir, "AF.Economy.asmdef"));
            StringAssert.Contains("\"noEngineReferences\": true", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);
            StringAssert.Contains("\"AF.Platform\"", asmdef);
            StringAssert.Contains("\"AF.Save\"", asmdef);
            foreach (string forbidden in new[] { "AF.Levels", "AF.GameFlow", "AF.UI", "AF.Presentation", "AF.Core", "Unity" })
            {
                Assert.That(asmdef.Contains(forbidden), Is.False, "AF.Economy no debe referenciar " + forbidden);
            }

            // Se componen por partes para que este archivo no se denuncie a sí mismo.
            string[] banned =
            {
                "DateTime" + ".Now", "DateTime" + ".UtcNow", "DateTime" + ".Today", "DateTimeOffset" + ".Now",
                "DateTimeOffset" + ".UtcNow", "Environment" + ".TickCount", "Stop" + "watch", "System" + ".Random",
                "using " + "Unity",
            };
            string[] files = Directory.GetFiles(EconomyDir, "*.cs", SearchOption.AllDirectories);
            Assert.That(files, Is.Not.Empty);
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word + " (la hora sale de IClock)");
                }
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

            if (type == typeof(double))
            {
                return "float";
            }

            if (type == typeof(IReadOnlyList<PriceBand>))
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
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case IReadOnlyList<PriceBand> bands: return EconomyConfig.FormatBands(bands);
                default: throw new NotSupportedException(value?.GetType().Name ?? "null");
            }
        }

        /// <summary>
        /// Lee las filas de 08 §9: <c>| `clave` / `clave2` | tipo / tipo2 | default / default2 | rango | dueño | uso |</c>.
        /// Con una sola celda de tipo o de default para varias claves, vale para todas.
        /// </summary>
        private static Dictionary<string, RcRow> ReadRemoteConfigTable()
        {
            string path = Path.Combine(RepoRoot, "docs", "gdd", "08-arquitectura-tecnica-publicacion.md");
            Assert.That(File.Exists(path), Is.True, path);
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            int start = text.IndexOf("\n## 9. ", StringComparison.Ordinal);
            int end = text.IndexOf("\n## 10. ", start + 1, StringComparison.Ordinal);
            Assert.That(start >= 0 && end > start, Is.True, "no encuentro la sección 9 de 08");

            var table = new Dictionary<string, RcRow>(StringComparer.Ordinal);
            foreach (string line in text.Substring(start, end - start).Split('\n'))
            {
                if (!line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                MatchCollection keys = Regex.Matches(cells[1], "`([^`]+)`");
                string[] types = SplitValues(cells[2]);
                string[] defaults = SplitValues(cells[3]);
                for (int i = 0; i < keys.Count; i++)
                {
                    string type = types.Length == keys.Count ? types[i] : types[0];
                    string? value = defaults.Length == keys.Count ? defaults[i] : defaults.Length == 1 ? defaults[0] : null;
                    table[keys[i].Groups[1].Value] = new RcRow(type, value);
                }
            }

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

        private readonly struct RcRow
        {
            public RcRow(string type, string? value)
            {
                Type = type;
                Default = value;
            }

            public string Type { get; }

            public string? Default { get; }
        }
    }
}
