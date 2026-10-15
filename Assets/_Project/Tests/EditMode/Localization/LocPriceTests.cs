#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using AF.IAP;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// D-025 (tarea FXT): un solo formato de cifras, también en los precios. <see cref="Loc.Price"/> escribe el código ISO 4217,
    /// un espacio y el importe con los separadores de la región del teléfono (<see cref="Loc.Number"/>; sin región, es-CO [P]) y
    /// los decimales de la moneda (0 en COP y MXN; 2 en el resto, 05 §4.2), sin cambiar los importes. La tabla de D-025 (el botón
    /// del Puñado en Aguas estancadas), el cambio de región en caliente, los micros de Play Billing (D-025, punto 3) y una
    /// búsqueda en el código: ningún precio lleva un formato propio.
    /// </summary>
    public sealed class LocPriceTests
    {
        private LocStateSnapshot _before;

        [SetUp]
        public void SetUp()
        {
            _before = LocStateSnapshot.Take();
        }

        [TearDown]
        public void TearDown()
        {
            _before.Restore();
        }

        [TestCase("COP", 0)]
        [TestCase("MXN", 0)]
        [TestCase("USD", 2)]
        [TestCase("BRL", 2)]
        [TestCase("PEN", 2)]
        [TestCase("EUR", 2)]
        public void Decimals_AreZeroInCopAndMxn_AndTwoInTheRest(string currency, int decimals)
        {
            Assert.That(LocPrice.DecimalsOf(currency), Is.EqualTo(decimals));
        }

        // La tabla de D-025: «{semillas} · {precio}» del botón del Puñado (stuck_pack_199), con el teléfono en cada región.
        [TestCase("en-US", "COP", "1,000 · COP 3,900")]
        [TestCase("es-CO", "COP", "1.000 · COP 3.900")]
        [TestCase("es-MX", "MXN", "1,000 · MXN 19")]
        [TestCase("pt-BR", "BRL", "1.000 · BRL 4,99")]
        public void HandfulButton_HasOneNumberFormat_InTheFourRegionsOfD025(string region, string currency, string expected)
        {
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, region);
            string price = StoreCatalog.Default.Get(StoreCatalog.Handful).PriceIn(currency).ToString();

            Assert.That(UiText.StuckPackLine(1000, price), Is.EqualTo("Puñado de semillas\n" + expected));
            Assert.That(price, Is.EqualTo(Loc.Price(currency, StoreCatalog.Default.Get(StoreCatalog.Handful).PriceIn(currency).MinorUnits)));
            Assert.That(expected, Does.StartWith(Loc.Number(1000) + " · "), "las semillas y el precio, con los mismos separadores");
        }

        [Test]
        public void WithoutPhoneRegion_PricesGoWithEsCo()
        {
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, null);

            Assert.That(Loc.Price("COP", 94900), Is.EqualTo("COP 94.900"));
            Assert.That(Loc.Price("USD", 199), Is.EqualTo("USD 1,99"));
        }

        [Test]
        public void ARegionChange_RewritesThePrices_AndTellsTheScreens()
        {
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, "es-CO");
            LocalPrice balsa = StoreCatalog.Default.Get(StoreCatalog.Rescue).PriceIn("COP");
            Assert.That(balsa.ToString(), Is.EqualTo("COP 9.900"));
            Assert.That(UiText.StuckBalsaLine(balsa.ToString()), Is.EqualTo("Balsa de rescate\nCOP 9.900"));

            int changed = 0;
            Action onChanged = () => changed++;
            Loc.Changed += onChanged;
            try
            {
                Loc.SetNumberTag("en-US");
                Assert.That(changed, Is.EqualTo(1), "Loc.Changed: las pantallas rehacen sus textos con precio");
                Assert.That(balsa.ToString(), Is.EqualTo("COP 9,900"));
                Assert.That(UiText.StuckBalsaLine(balsa.ToString()), Is.EqualTo("Balsa de rescate\nCOP 9,900"));
                Assert.That(StoreCatalog.Default.Get("af_coins_6").PriceIn("BRL").ToString(), Is.EqualTo("BRL 129.90"));

                Loc.SetNumberTag("pt-BR");
                Assert.That(changed, Is.EqualTo(2));
                Assert.That(StoreCatalog.Default.Get("af_coins_6").PriceIn("BRL").ToString(), Is.EqualTo("BRL 129,90"));
            }
            finally
            {
                Loc.Changed -= onChanged;
            }
        }

        [TestCase("BRL", 4990000L, "pt-BR", 499L, "BRL 4,99")]
        [TestCase("COP", 3900000000L, "en-US", 3900L, "COP 3,900")]
        [TestCase("MXN", 19000000L, "es-MX", 19L, "MXN 19")]
        [TestCase("MXN", 19500000L, "es-MX", 20L, "MXN 20")]
        [TestCase("USD", 1994999L, "en-US", 199L, "USD 1.99")]
        [TestCase("USD", 1995000L, "en-US", 200L, "USD 2.00")]
        [TestCase("PEN", 0L, "es-PE", 0L, "PEN 0.00")]
        public void FromPlayBillingMicros_ComposesWithTheSameFunction(string currency, long micros, string region, long minor, string expected)
        {
            Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, region);

            Assert.That(LocPrice.MinorUnitsFromMicros(currency, micros), Is.EqualTo(minor), "redondeo «la mitad hacia fuera»");
            Assert.That(Loc.PriceFromMicros(currency, micros), Is.EqualTo(expected));
            Assert.That(Loc.Price(currency, minor), Is.EqualTo(expected));
        }

        [Test]
        public void Amounts_UseTheSeparatorsOfLocNumber_AndTheDecimalsOfTheCurrency()
        {
            LocNumberFormat fr = LocNumber.For("fr");
            Assert.That(LocPrice.Format("COP", 1234567, fr), Is.EqualTo("COP 1\u00A0234\u00A0567"));
            Assert.That(LocPrice.Format("BRL", 123456789, fr), Is.EqualTo("BRL 1\u00A0234\u00A0567,89"));
            Assert.That(LocPrice.Format("USD", 5, LocNumber.For("en")), Is.EqualTo("USD 0.05"));
            Assert.That(LocPrice.FormatAmount("BRL", 12990, LocNumber.For("pt-BR")), Is.EqualTo("129,90"));
            Assert.That(LocPrice.Format("COP", long.MaxValue, LocNumber.For("es-CO")), Is.EqualTo("COP 9.223.372.036.854.775.807"));

            Assert.Throws<ArgumentException>(() => LocPrice.Format("cop", 1, fr));
            Assert.Throws<ArgumentException>(() => LocPrice.Format("US$", 1, fr));
            Assert.Throws<ArgumentNullException>(() => LocPrice.Format(null!, 1, fr));
            Assert.Throws<ArgumentOutOfRangeException>(() => LocPrice.Format("COP", -1, fr));
            Assert.Throws<ArgumentOutOfRangeException>(() => LocPrice.MinorUnitsFromMicros("COP", -1));
        }

        [Test]
        public void EveryTierAndCurrency_GoesThroughLocPrice()
        {
            string[] regions = { "es-CO", "en-US", "es-MX", "pt-BR", "fr" };
            for (int r = 0; r < regions.Length; r++)
            {
                Loc.Configure(LocSetUpFixture.Catalog, LocLocale.SourceLocale, true, regions[r]);
                for (int t = 0; t < StorePrices.UsdTiers.Count; t++)
                {
                    for (int c = 0; c < StorePrices.Currencies.Length; c++)
                    {
                        string currency = StorePrices.Currencies[c];
                        LocalPrice price = StorePrices.Local(StorePrices.UsdTiers[t], currency);
                        Assert.That(price.ToString(), Is.EqualTo(Loc.Price(currency, price.MinorUnits)), regions[r] + " " + currency);
                        Assert.That(price.FormatAmount(), Is.EqualTo(Loc.PriceAmount(currency, price.MinorUnits)));
                        Assert.That(price.Decimals, Is.EqualTo(LocPrice.DecimalsOf(currency)));
                        Assert.That((decimal)price.MinorUnits, Is.EqualTo(price.Amount * (price.Decimals == 0 ? 1m : 100m)), "05 §4.2 no cambia");
                    }
                }
            }
        }

        // Búsqueda en el código (criterio de FXT): en la tienda y en los textos con precio de la UI no queda ningún formato de
        // cifras propio (formatos numéricos de .NET, separadores fijos ni monedas escritas a mano); el precio sale de la tienda
        // (GameServices.PriceText → IStoreService.PriceOf) y se escribe con Loc.Price.
        [Test]
        public void NoPriceHasItsOwnFormat_InTheStoreOrTheUi()
        {
            string scripts = Path.Combine(Application.dataPath, "_Project", "Scripts");
            string iap = Path.Combine(scripts, "IAP");
            string tests = Path.GetFullPath(Path.Combine(iap, "Tests")) + Path.DirectorySeparatorChar;
            var forbidden = new Regex(
                "ToString\\(\"[NnCcFfPp#0]|\\{[0-9a-z]+:[NnCcFf#0]|#,##|Append\\('[.,]'\\)|\"(USD|BRL|MXN|COP|PEN|US\\$|R\\$) ?\\{|\"(USD|BRL|MXN|COP|PEN) \"\\s*\\+",
                RegexOptions.CultureInvariant);

            int files = 0;
            foreach (string file in Directory.GetFiles(iap, "*.cs", SearchOption.AllDirectories))
            {
                // StoreConfig escribe el JSON de 08 §9 (FormatPacks, con sus comas): no es texto visible.
                if (Path.GetFullPath(file).StartsWith(tests, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file) == "StoreConfig.cs")
                {
                    continue;
                }

                files++;
                AssertNoOwnFormat(file, forbidden);
            }

            string[] ui = { "GameServices.cs", "StoreSheet.cs", "StuckModal.cs", "NoCanoesScreen.cs" };
            for (int i = 0; i < ui.Length; i++)
            {
                files++;
                AssertNoOwnFormat(Path.Combine(scripts, "UI", ui[i]), forbidden);
            }

            Assert.That(files, Is.GreaterThanOrEqualTo(8));
            string catalog = File.ReadAllText(Path.Combine(iap, "StoreCatalog.cs"));
            StringAssert.Contains("return Loc.Price(Currency, MinorUnits);", catalog, "LocalPrice.ToString pasa por Loc.Price");
            string services = File.ReadAllText(Path.Combine(scripts, "UI", "GameServices.cs"));
            StringAssert.Contains("return Store.PriceOf(sku).ToString();", services, "GameServices.PriceText, desde la tienda");
        }

        private static void AssertNoOwnFormat(string file, Regex forbidden)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(forbidden.IsMatch(line), Is.False, Path.GetFileName(file) + ":" + (i + 1) + ": formato de cifras propio: " + line);
            }
        }
    }
}
