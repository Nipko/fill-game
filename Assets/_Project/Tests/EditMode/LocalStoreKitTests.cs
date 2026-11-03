#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;
using AF.EditorTools;
using AF.IAP;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El <c>Tintalu.storekit</c> de desarrollo (fase M3 de <c>mac-ios.md</c>) sale del catálogo del juego (<see cref="StoreCatalog.Default"/>):
    /// un producto por SKU, no consumible donde el catálogo dice <see cref="ProductKind.NonConsumable"/> (Sin anuncios y skins) y
    /// consumible en el resto, con su precio en USD.
    /// </summary>
    public sealed class LocalStoreKitTests
    {
        [Test]
        public void TheStoreKitFile_HasEverySkuOfTheCatalog_WithItsTypeAndPrice()
        {
            StoreCatalog catalog = StoreCatalog.Default;
            string json = BuildIos.StoreKitJson(catalog);
            Assert.That(Regex.Matches(json, "\"productID\"").Count, Is.EqualTo(catalog.Products.Count));
            foreach (StoreProduct p in catalog.Products)
            {
                string type = p.Kind == ProductKind.NonConsumable ? "NonConsumable" : "Consumable";
                string price = p.UsdPrice.ToString("0.00", CultureInfo.InvariantCulture);
                var entry = new Regex("\"displayPrice\" : \"" + Regex.Escape(price) + "\",[\\s\\S]*?\"productID\" : \"" + Regex.Escape(p.Sku)
                    + "\",[\\s\\S]*?\"type\" : \"" + type + "\"");
                Assert.That(entry.IsMatch(json), Is.True, p.Sku + ": " + type + " a " + price + " USD");
            }

            StringAssert.Contains("\"productID\" : \"af_noads\"", json);
            StringAssert.Contains("\"version\" : { \"major\" : 4, \"minor\" : 0 }", json);
        }
    }
}
