#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using AF.Ads.Tests;
using AF.Platform;
using NUnit.Framework;
using UnityEngine;

namespace AF.IAP.Tests
{
    /// <summary>
    /// Guardas de B1-4 contra SKU, nombres, precios y cifras inventados: el catálogo y los precios locales se leen de
    /// 05 §4.1 y §4.2, los defaults de <see cref="StoreConfig"/> de 08 §9, y el ensamblado <c>AF.IAP</c> no usa el motor,
    /// el reloj del sistema ni la red.
    /// </summary>
    public sealed class StoreCatalogTests
    {
        private static string IapDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "IAP");

        [Test]
        public void Catalog_HasThe13SkusOf1_0_WithTheNamesAndUsdPricesOf05_4_1()
        {
            List<string[]> rows = Sku05Rows();
            StoreCatalog catalog = StoreCatalog.Default;

            var skus = new List<string>();
            foreach (StoreProduct product in catalog.Products)
            {
                skus.Add(product.Sku);
            }

            var expected = new List<string>();
            foreach (string[] row in rows)
            {
                expected.Add(row[0]);
                StoreProduct? product = catalog.Find(row[0]);
                Assert.That(product, Is.Not.Null, row[0]);
                Assert.That(product!.Name, Is.EqualTo(row[1]), row[0] + ": nombre de tienda");
                Assert.That(product.PriceIn("USD").FormatAmount(), Is.EqualTo(row[2]), row[0] + ": USD");
            }

            Assert.That(skus, Is.EqualTo(expected));
            Assert.That(skus.Count, Is.EqualTo(13));
            Assert.That(catalog.Find("af_pass"), Is.Null, "el Pase Expedición es de la v1.2");
            Assert.Throws<ArgumentException>(() => catalog.Get("af_pass"));
        }

        [Test]
        public void Skus_AreTheOnesOfTheGlossary_11()
        {
            string glossary = GddTables.Chapter("00-glosario.md");
            Match row = Regex.Match(glossary, "\n\\| SKUs IAP[^\n]*");
            Assert.That(row.Success, Is.True);
            foreach (StoreProduct product in StoreCatalog.Default.Products)
            {
                string sku = product.Sku;
                bool listed = row.Value.Contains("`" + sku + "`")
                    || (sku == "af_coins_3" || sku == "af_coins_4" || sku == "af_coins_5" || sku == "af_skin_m03");
                Assert.That(listed, Is.True, sku + " (los intermedios van como «…» en el glosario)");
            }
        }

        [Test]
        public void LocalPrices_AreTheOnesOf05_4_2()
        {
            string section = GddTables.Section(GddTables.Chapter("05-economia-monetizacion.md"), "### 4.2 ", "**Métodos de pago");
            int rows = 0;
            foreach (string line in section.Split('\n'))
            {
                if (!Regex.IsMatch(line, "^\\| [0-9]"))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                decimal usd = decimal.Parse(cells[1].Trim().Replace(',', '.'), CultureInfo.InvariantCulture);
                for (int c = 0; c < StorePrices.Currencies.Length; c++)
                {
                    string currency = StorePrices.Currencies[c];
                    Assert.That(StorePrices.Local(usd, currency).FormatAmount(), Is.EqualTo(cells[1 + c].Trim()), usd + " USD → " + currency);
                }

                rows++;
            }

            Assert.That(rows, Is.EqualTo(8));
            Assert.That(StorePrices.UsdTiers.Count, Is.EqualTo(8));
            Assert.That(StoreCatalog.Default.Get("af_coins_2").PriceIn("COP").ToString(), Is.EqualTo("COP 3.900"), "D-008: Puñado, COP 3.900");
            Assert.That(StoreCatalog.Default.Get("af_rescue").PriceIn("COP").ToString(), Is.EqualTo("COP 9.900"));
            Assert.That(StoreCatalog.Default.Get("af_coins_6").PriceIn("BRL").ToString(), Is.EqualTo("BRL 129,90"));
            Assert.Throws<ArgumentException>(() => StorePrices.Local(1.98m, "USD"));
            Assert.Throws<ArgumentException>(() => new LocalPrice("EUR", 1m));
        }

        [Test]
        public void PriceFormat_DoesNotDependOnTheDeviceCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Assert.That(new LocalPrice("COP", 94900m).ToString(), Is.EqualTo("COP 94.900"));
                Assert.That(new LocalPrice("PEN", 3.9m).ToString(), Is.EqualTo("PEN 3,90"));
                Assert.That(new LocalPrice("USD", 0.99m).ToString(), Is.EqualTo("USD 0,99"));
                Assert.That(new LocalPrice("MXN", 169m).ToString(), Is.EqualTo("MXN 169"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, RcRow> table = GddTables.RemoteConfigTable();
            StoreConfig config = StoreConfig.Defaults;

            int checkedKeys = 0;
            foreach (PropertyInfo property in typeof(StoreConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (attribute == null)
                {
                    Assert.That(property.CanWrite, Is.False, property.Name + ": toda cifra configurable sale de 08 §9");
                    continue;
                }

                string key = attribute.Key;
                Assert.That(table.ContainsKey(key), Is.True, key + " no está en 08 §9");
                RcRow row = table[key];
                object? value = property.GetValue(config);
                string type = value is bool ? "bool" : value is int ? "int" : "json";
                string text = value is bool b ? (b ? "true" : "false")
                    : value is int i ? i.ToString(CultureInfo.InvariantCulture)
                    : StoreConfig.FormatPacks((IReadOnlyList<SeedPack>)value!);
                Assert.That(row.Type, Is.EqualTo(type), key + ": tipo");
                Assert.That(text, Is.EqualTo(row.Default), key + ": default");
                checkedKeys++;
            }

            Assert.That(checkedKeys, Is.EqualTo(7));
            Assert.That(StoreConfig.BalsaHideDuration, Is.EqualTo(TimeSpan.FromHours(48)), "05 §4.1: oculta 48 h");
        }

        [Test]
        public void Validate_EnforcesTheRangesOf08_9()
        {
            StoreConfig.Defaults.Validate();

            var bad = new List<Action<StoreConfig>>
            {
                c => c.EcoNoadsBonusSeeds = 299,
                c => c.EcoNoadsBonusSeeds = 601,
                c => c.AfRescueDailyCap = 2,
                c => c.AfRescueHideAfterDeclines = 1,
                c => c.AfRescueHideAfterDeclines = 4,
                c => c.EcoPiggyCap = 1999,
                c => c.EcoPiggyMinBuy = 3001,
                c => c.EcoSeedPackAmounts = new[] { new SeedPack("af_coins_2", 1000) },
                c => c.EcoSeedPackAmounts = new[]
                {
                    new SeedPack("af_coins_2", 1000), new SeedPack("af_coins_3", 0), new SeedPack("af_coins_4", 6000),
                    new SeedPack("af_coins_5", 13000), new SeedPack("af_coins_6", 36000),
                },
            };

            for (int i = 0; i < bad.Count; i++)
            {
                StoreConfig config = StoreConfig.Defaults;
                bad[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "caso " + i);
            }
        }

        [Test]
        public void IapAssembly_HasNoEngine_NoSystemClock_AndNoNetwork()
        {
            string asmdef = File.ReadAllText(Path.Combine(IapDir, "AF.IAP.asmdef"));
            StringAssert.Contains("\"noEngineReferences\": true", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);
            foreach (string forbidden in new[] { "AF.Levels", "AF.GameFlow", "AF.UI", "AF.Presentation", "AF.Core", "AF.Ads", "Unity" })
            {
                Assert.That(asmdef.Contains(forbidden), Is.False, "AF.IAP no debe referenciar " + forbidden);
            }

            string[] banned =
            {
                "DateTime" + ".Now", "DateTime" + ".UtcNow", "DateTime" + ".Today", "DateTimeOffset" + ".Now",
                "DateTimeOffset" + ".UtcNow", "Environment" + ".TickCount", "Stop" + "watch", "System" + ".Random",
                "using " + "Unity", "System" + ".Net", "Http" + "Client", "Web" + "Request", "Sock" + "et",
            };
            string testsDir = Path.GetFullPath(Path.Combine(IapDir, "Tests")) + Path.DirectorySeparatorChar;
            int files = 0;
            foreach (string file in Directory.GetFiles(IapDir, "*.cs", SearchOption.AllDirectories))
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

            Assert.That(files, Is.GreaterThanOrEqualTo(5));
        }

        /// <summary>Filas de 1.0 de la tabla de 05 §4.1: SKU, nombre de tienda en español y USD; el pack de skins se expande.</summary>
        private static List<string[]> Sku05Rows()
        {
            string section = GddTables.Section(GddTables.Chapter("05-economia-monetizacion.md"), "### 4.1 ", "### 4.2 ");
            var rows = new List<string[]>();
            foreach (string line in section.Split('\n'))
            {
                if (!line.StartsWith("| `af_", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|');
                string name = Regex.Match(cells[2], "\\*\\*([^*]+)\\*\\*").Groups[1].Value.Trim();
                if (cells[2].Contains("v1.2"))
                {
                    continue; // Pase Expedición
                }

                string usd = cells[3].Trim().Split(' ')[0];
                MatchCollection skus = Regex.Matches(cells[1], "`(af_[a-z0-9_]+)`");
                if (skus.Count == 1)
                {
                    rows.Add(new[] { skus[0].Groups[1].Value, name, usd });
                    continue;
                }

                // `af_skin_m02` … `af_skin_m04` con «Pack Cuia de Santarém / Cuia del Encuentro / Totuma».
                string[] names = name.Split(new[] { " / " }, StringSplitOptions.None);
                Assert.That(names.Length, Is.EqualTo(3), cells[2]);
                for (int i = 0; i < names.Length; i++)
                {
                    string sku = "af_skin_m0" + (i + 2).ToString(CultureInfo.InvariantCulture);
                    rows.Add(new[] { sku, i == 0 ? names[i] : "Pack " + names[i], usd });
                }
            }

            return rows;
        }
    }
}
