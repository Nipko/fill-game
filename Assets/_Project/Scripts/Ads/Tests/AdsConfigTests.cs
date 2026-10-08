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

namespace AF.Ads.Tests
{
    /// <summary>
    /// Guardas de B1-3 contra cifras y nombres inventados: cada default de <see cref="AdsConfig"/> sale de la tabla única
    /// de 08 §9, los IDs de ubicación son los de 08 §2.2, los rangos de 08 §9 se exigen y el ensamblado <c>AF.Ads</c> no
    /// usa el motor, el reloj del sistema ni la red.
    /// </summary>
    public sealed class AdsConfigTests
    {
        private static string AdsDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "Ads");

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, RcRow> table = GddTables.RemoteConfigTable();
            AdsConfig config = AdsConfig.Defaults;

            int checkedKeys = 0;
            foreach (PropertyInfo property in typeof(AdsConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
                Assert.That(Format(property.GetValue(config)), Is.EqualTo(row.Default), key + ": default");
                checkedKeys++;
            }

            Assert.That(checkedKeys, Is.EqualTo(24));
        }

        [Test]
        public void EverySetting_CarriesItsRemoteConfigKey()
        {
            foreach (PropertyInfo property in typeof(AdsConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    Assert.That(property.GetCustomAttribute<RemoteConfigKeyAttribute>(), Is.Not.Null,
                        property.Name + ": toda cifra configurable sale de 08 §9");
                }
            }
        }

        [Test]
        public void PlacementIds_AreTheOnesOf08_2_2()
        {
            string text = GddTables.Chapter("08-arquitectura-tecnica-publicacion.md");
            Match row = Regex.Match(text, "\n\\| `AF\\.Ads` \\|[^\n]*");
            Assert.That(row.Success, Is.True, "fila de AF.Ads en 08 §2.2");
            var ids = new List<string>();
            foreach (Match id in Regex.Matches(row.Value, "`((?:rv|interstitial)_[a-z_]+)`"))
            {
                ids.Add(id.Groups[1].Value);
            }

            var ours = new List<string>();
            for (int i = 0; i < AdPlacements.Count; i++)
            {
                ours.Add(((AdPlacement)i).ToId());
            }

            Assert.That(ours, Is.EqualTo(ids));
            Assert.That(AdPlacement.InterstitialPostLevel.Format(), Is.EqualTo(AdFormat.Interstitial));
            Assert.That(AdPlacement.ExtraCanoe.RemoteConfigSuffix(), Is.EqualTo("extra_canoe"));
            Assert.That(AdPlacements.TryParse("rv_hint", out AdPlacement hint) && hint == AdPlacement.Hint, Is.True);
            Assert.That(AdPlacements.TryParse("rv_lives", out _), Is.False);
        }

        [Test]
        public void Defaults_GiveTheTablesOf05_6()
        {
            AdsConfig config = AdsConfig.Defaults;

            Assert.That(config.InterstitialEvery(21), Is.Null);
            Assert.That(config.InterstitialEvery(22), Is.EqualTo(3));
            Assert.That(config.InterstitialEvery(60), Is.EqualTo(3));
            Assert.That(config.InterstitialEvery(61), Is.EqualTo(2));
            Assert.That(config.InterstitialEvery(500), Is.EqualTo(2));
            Assert.That(config.RewardedCap(AdPlacement.DoubleHarvest), Is.EqualTo(5));
            Assert.That(config.RewardedCap(AdPlacement.UndoRefill), Is.EqualTo(4));
            Assert.That(config.RewardedCap(AdPlacement.ExtraCanoe), Is.EqualTo(3));
            Assert.That(config.RewardedCooldown(AdPlacement.Hint), Is.EqualTo(TimeSpan.FromSeconds(60)));
            Assert.That(config.RewardedCooldown(AdPlacement.ExtraCanoe), Is.EqualTo(TimeSpan.FromSeconds(90)));
            Assert.That(config.IsPlacementEnabled(AdPlacement.DailyChest), Is.False, "rv_daily_chest apagada en 1.0");
            Assert.That(AdsConfig.GapAfterRewarded, Is.EqualTo(TimeSpan.FromSeconds(60)));
            Assert.Throws<ArgumentException>(() => config.RewardedCap(AdPlacement.InterstitialPostLevel));
        }

        [Test]
        public void Defaults_IsAFreshInstanceEachTime()
        {
            AdsConfig a = AdsConfig.Defaults;
            a.AdsInterstitialSessionCap = 1;

            Assert.That(AdsConfig.Defaults.AdsInterstitialSessionCap, Is.EqualTo(6));
        }

        [Test]
        public void Validate_EnforcesTheRangesOf08_9()
        {
            AdsConfig.Defaults.Validate();

            var bad = new List<Action<AdsConfig>>
            {
                c => c.AdsFirstInterstitialLevel = 21,
                c => c.AdsInterstitialEveryNLevels = new[] { new CadenceBand(22, 1) },
                c => c.AdsInterstitialEveryNLevels = new CadenceBand[0],
                c => c.AdsInterstitialEveryNLevels = new[] { new CadenceBand(61, 2), new CadenceBand(22, 3) },
                c => c.AdsInterstitialMinGapS = 149,
                c => c.AdsInterstitialSessionWarmupS = 59,
                c => c.AdsInterstitialSessionCap = 7,
                c => c.AdsInterstitialDailyCap = 15,
                c => c.AdsInterstitialSuppressAfterIapH = 11,
                c => c.AdsInterstitialSuppressAfterIapH = 49,
                c => c.AdsRvCapUndoRefill = 0,
                c => c.AdsRvCapDoubleHarvest = 6,
                c => c.AdsRvCooldownSExtraCanoe = 121,
                c => c.AdsRvCooldownSHint = 59,
                c => c.AdsRvGlobalCapDay = 9,
                c => c.EcoCalendarDoubleAdEnabled = true,
            };

            for (int i = 0; i < bad.Count; i++)
            {
                AdsConfig config = AdsConfig.Defaults;
                bad[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "caso " + i);
            }
        }

        [Test]
        public void AdsAssembly_HasNoEngine_NoSystemClock_AndNoNetwork()
        {
            string asmdef = File.ReadAllText(Path.Combine(AdsDir, "AF.Ads.asmdef"));
            StringAssert.Contains("\"noEngineReferences\": true", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);
            foreach (string forbidden in new[] { "AF.Levels", "AF.GameFlow", "AF.UI", "AF.Presentation", "AF.Core", "AF.IAP", "Unity" })
            {
                Assert.That(asmdef.Contains(forbidden), Is.False, "AF.Ads no debe referenciar " + forbidden);
            }

            // Se componen por partes para que este archivo no se denuncie a sí mismo.
            string[] banned =
            {
                "DateTime" + ".Now", "DateTime" + ".UtcNow", "DateTime" + ".Today", "DateTimeOffset" + ".Now",
                "DateTimeOffset" + ".UtcNow", "Environment" + ".TickCount", "Stop" + "watch", "System" + ".Random",
                "using " + "Unity", "System" + ".Net", "Http" + "Client", "Web" + "Request", "Sock" + "et",
            };
            string testsDir = Path.GetFullPath(Path.Combine(AdsDir, "Tests")) + Path.DirectorySeparatorChar;
            int files = 0;
            foreach (string file in Directory.GetFiles(AdsDir, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFullPath(file).StartsWith(testsDir, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                files++;
                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word);
                }
            }

            Assert.That(files, Is.GreaterThanOrEqualTo(7));
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

            if (type == typeof(IReadOnlyList<CadenceBand>))
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
                case IReadOnlyList<CadenceBand> bands: return AdsConfig.FormatBands(bands);
                default: throw new NotSupportedException(value?.GetType().Name ?? "null");
            }
        }
    }
}
