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
    /// Guardas de B1-4 contra SKU, precios y cifras inventados: el catálogo y los precios locales se leen de 05 §4.1 y
    /// §4.2, los defaults de <see cref="StoreConfig"/> de 08 §9, y el ensamblado <c>AF.IAP</c> no usa el motor, el reloj
    /// del sistema ni la red. Desde S12 (D-018), cada producto lleva la clave de su nombre (<c>iap_&lt;sku&gt;_name</c>
    /// [P]) y <c>AF.IAP</c> no tiene ningún texto visible.
    /// </summary>
    public sealed class StoreCatalogTests
    {
        private static string IapDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "IAP");

        [Test]
        public void Catalog_HasThe13SkusOf1_0_WithTheirNameKeysAndTheUsdPricesOf05_4_1()
        {
            List<string[]> rows = Sku05Rows();
            StoreCatalog catalog = StoreCatalog.Default;

            var skus = new List<string>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (StoreProduct product in catalog.Products)
            {
                skus.Add(product.Sku);
                Assert.That(product.NameKey, Is.EqualTo("iap_" + product.Sku + "_name"), product.Sku + ": clave del nombre");
                Assert.That(Regex.IsMatch(product.NameKey, "^[a-z0-9_]+$"), Is.True, product.NameKey + ": clave de UI en snake_case");
                Assert.That(keys.Add(product.NameKey), Is.True, product.NameKey + " repetida");
            }

            var expected = new List<string>();
            foreach (string[] row in rows)
            {
                expected.Add(row[0]);
                StoreProduct? product = catalog.Find(row[0]);
                Assert.That(product, Is.Not.Null, row[0]);
                using (NumberRegionScope.Of("es-CO"))
                {
                    // 05 §4.1 escribe los importes con los separadores de es-CO (D-025: los mismos que da Loc en esa región).
                    Assert.That(product!.PriceIn("USD").FormatAmount(), Is.EqualTo(row[2]), row[0] + ": USD");
                }
            }

            Assert.That(skus, Is.EqualTo(expected));
            Assert.That(skus.Count, Is.EqualTo(13));
            Assert.That(keys.Count, Is.EqualTo(13));
            Assert.That(typeof(StoreProduct).GetProperty("Name"), Is.Null, "Product.Name desaparece (S12)");
            Assert.That(typeof(SimulatedPurchase).GetProperty("Name"), Is.Null);
            Assert.That(StoreProduct.NameKeyOf("af_pass"), Is.EqualTo("iap_af_pass_name"));
            Assert.Throws<ArgumentException>(() => StoreProduct.NameKeyOf(""));
            Assert.That(catalog.Find("af_pass"), Is.Null, "el Pase Expedición es de la v1.2");
            Assert.Throws<ArgumentException>(() => catalog.Get("af_pass"));
        }

        [Test]
        public void IapAssembly_HasNoVisibleText()
        {
            // Los nombres de tienda de 05 §4.1 y el rótulo de la hoja ya no están en AF.IAP: van por clave (D-018).
            var forbidden = new List<string> { "Compra simulada" };
            foreach (string[] row in Sku05Rows())
            {
                forbidden.Add(row[1]);
            }

            Assert.That(forbidden.Count, Is.EqualTo(14));
            string testsDir = Path.GetFullPath(Path.Combine(IapDir, "Tests")) + Path.DirectorySeparatorChar;
            var offenders = new List<string>();
            int files = 0;
            foreach (string file in Directory.GetFiles(IapDir, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFullPath(file).StartsWith(testsDir, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                files++;
                string name = Path.GetFileName(file);
                string[] lines = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//", StringComparison.Ordinal))
                    {
                        continue; // documentación y comentarios
                    }

                    foreach (string text in forbidden)
                    {
                        if (line.Contains(text))
                        {
                            offenders.Add(name + ":" + (i + 1) + ": " + text);
                        }
                    }

                    // Los mensajes de las excepciones y de la validación de la configuración son para el programador.
                    if (line.Contains("Exception(") || line.Contains("Require("))
                    {
                        continue;
                    }

                    // Cualquier otra cadena con una frase (letras separadas por espacios) o con letras no ASCII sería texto
                    // visible. Antes se quitan los caracteres sueltos ('"'), que confundirían la búsqueda de cadenas.
                    string code = Regex.Replace(line, @"'(?:[^'\\]|\\.)'", "' '");
                    foreach (Match literal in Regex.Matches(code, @"""(?:[^""\\]|\\.)*"""))
                    {
                        if (Regex.IsMatch(literal.Value, @"[A-Za-z]\s+[A-Za-z]") || Regex.IsMatch(literal.Value, @"[^\x20-\x7E]"))
                        {
                            offenders.Add(name + ":" + (i + 1) + ": " + literal.Value);
                        }
                    }
                }
            }

            Assert.That(files, Is.GreaterThanOrEqualTo(5));
            Assert.That(offenders, Is.Empty, "texto visible en AF.IAP: va por clave a la tabla de textos");
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
            // 05 §4.2 escribe los importes con los separadores de es-CO: con esa región, Loc.Price (D-025) da lo mismo.
            using NumberRegionScope region = NumberRegionScope.Of("es-CO");
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
            Assert.Throws<ArgumentException>(() => StorePrices.Local(0.99m, "EUR"), "la tienda simulada solo tiene las columnas de 05 §4.2");
            Assert.That(new LocalPrice("EUR", 0.99m).MinorUnits, Is.EqualTo(99), "la real, la moneda que dé Play (Q-IAP-3)");
            Assert.Throws<ArgumentException>(() => new LocalPrice("eur", 1m));
        }

        [Test]
        public void PriceFormat_DoesNotDependOnTheDeviceCulture_OnlyOnTheRegionOfLoc()
        {
            // D-025: la región de las cifras de Loc (la del teléfono) decide los separadores; la cultura del equipo, nunca.
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                using (NumberRegionScope.Of("es-CO"))
                {
                    Assert.That(new LocalPrice("COP", 94900m).ToString(), Is.EqualTo("COP 94.900"));
                    Assert.That(new LocalPrice("PEN", 3.9m).ToString(), Is.EqualTo("PEN 3,90"));
                    Assert.That(new LocalPrice("USD", 0.99m).ToString(), Is.EqualTo("USD 0,99"));
                    Assert.That(new LocalPrice("MXN", 169m).ToString(), Is.EqualTo("MXN 169"));
                }

                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                using (NumberRegionScope.Of("en-US"))
                {
                    Assert.That(new LocalPrice("COP", 94900m).ToString(), Is.EqualTo("COP 94,900"));
                    Assert.That(new LocalPrice("PEN", 3.9m).ToString(), Is.EqualTo("PEN 3.90"));
                    Assert.That(new LocalPrice("BRL", 129.9m).ToString(), Is.EqualTo("BRL 129.90"));
                    Assert.That(new LocalPrice("MXN", 169m).MinorUnits, Is.EqualTo(169L));
                    Assert.That(new LocalPrice("BRL", 129.9m).MinorUnits, Is.EqualTo(12990L));
                }
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

        /// <summary>
        /// Filas de 1.0 de la tabla de 05 §4.1: SKU, nombre de tienda en español (el texto de su clave, que vive en la tabla
        /// de textos) y USD; el pack de skins se expande.
        /// </summary>
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
