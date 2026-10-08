#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;

namespace AF.IAP
{
    /// <summary>Precio en una moneda, con el redondeo y el formato de 05 §4.2 (p. ej. <c>COP 3.900</c>, <c>BRL 4,99</c>).</summary>
    public readonly struct LocalPrice : IEquatable<LocalPrice>
    {
        /// <summary>Crea el precio.</summary>
        /// <exception cref="ArgumentException">Moneda no admitida (<see cref="StorePrices.Currencies"/>).</exception>
        public LocalPrice(string currency, decimal amount)
        {
            Decimals = StorePrices.DecimalsOf(currency);
            Currency = currency;
            Amount = amount;
        }

        /// <summary>Código ISO 4217 (<c>USD</c>, <c>BRL</c>, <c>MXN</c>, <c>COP</c> o <c>PEN</c>).</summary>
        public string Currency { get; }

        /// <summary>Importe.</summary>
        public decimal Amount { get; }

        /// <summary>Cifras decimales con que lo escribe 05 §4.2 (0 en COP y MXN; 2 en el resto).</summary>
        public int Decimals { get; }

        /// <summary>
        /// Importe como en 05 §4.2: punto de miles y coma decimal, sin depender de la cultura del dispositivo (p. ej.
        /// <c>1,99</c>, <c>3.900</c>, <c>19</c>).
        /// </summary>
        public string FormatAmount()
        {
            decimal rounded = Math.Round(Amount, Decimals, MidpointRounding.AwayFromZero);
            decimal whole = decimal.Truncate(rounded);
            string digits = ((long)whole).ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0)
                {
                    sb.Append('.');
                }

                sb.Append(digits[i]);
            }

            if (Decimals > 0)
            {
                decimal scale = 1;
                for (int i = 0; i < Decimals; i++)
                {
                    scale *= 10;
                }

                long fraction = (long)((rounded - whole) * scale);
                sb.Append(',').Append(fraction.ToString("D" + Decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>Moneda e importe, p. ej. <c>COP 3.900</c>.</summary>
        public override string ToString()
        {
            return Currency + " " + FormatAmount();
        }

        /// <inheritdoc />
        public bool Equals(LocalPrice other)
        {
            return string.Equals(Currency, other.Currency, StringComparison.Ordinal) && Amount == other.Amount;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LocalPrice other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Amount.GetHashCode();
        }
    }

    /// <summary>
    /// Escalones de precio de 05 §4.2 (ancla USD tier-1 y su precio local en BRL, MXN, COP y PEN, ≈ 50 % del ancla en
    /// poder adquisitivo). En la tienda real los precios viven en Play Console; esta tabla es la que muestra la compra
    /// simulada.
    /// </summary>
    public static class StorePrices
    {
        /// <summary>Monedas admitidas, en el orden de las columnas de <see cref="Tiers"/>.</summary>
        public static readonly string[] Currencies = { "USD", "BRL", "MXN", "COP", "PEN" };

        private static readonly int[] Decimals = { 2, 2, 0, 0, 2 };

        // 05 §4.2, columnas USD · BRL · MXN · COP · PEN (CLP, EC, ARS y EUR quedan fuera del soft launch).
        private static readonly decimal[][] Tiers =
        {
            new[] { 0.99m, 2.49m, 9m, 1900m, 1.90m },
            new[] { 1.99m, 4.99m, 19m, 3900m, 3.90m },
            new[] { 2.99m, 7.90m, 29m, 5900m, 5.90m },
            new[] { 4.99m, 12.90m, 45m, 9900m, 9.90m },
            new[] { 6.99m, 17.90m, 65m, 13900m, 12.90m },
            new[] { 9.99m, 24.90m, 89m, 18900m, 18.90m },
            new[] { 19.99m, 49.90m, 169m, 36900m, 37.90m },
            new[] { 49.99m, 129.90m, 429m, 94900m, 94.90m },
        };

        /// <summary>Anclas USD de los escalones, de menor a mayor.</summary>
        public static IReadOnlyList<decimal> UsdTiers
        {
            get
            {
                var tiers = new decimal[Tiers.Length];
                for (int i = 0; i < Tiers.Length; i++)
                {
                    tiers[i] = Tiers[i][0];
                }

                return tiers;
            }
        }

        /// <summary>Precio del escalón de <paramref name="usd"/> en <paramref name="currency"/>.</summary>
        /// <exception cref="ArgumentException">No hay escalón con ese ancla o la moneda no está admitida.</exception>
        public static LocalPrice Local(decimal usd, string currency)
        {
            int column = Column(currency);
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (Tiers[i][0] == usd)
                {
                    return new LocalPrice(currency, Tiers[i][column]);
                }
            }

            throw new ArgumentException("No hay escalón de 05 §4.2 para " + usd.ToString(CultureInfo.InvariantCulture) + " USD.", nameof(usd));
        }

        internal static int DecimalsOf(string currency)
        {
            return Decimals[Column(currency)];
        }

        private static int Column(string currency)
        {
            for (int i = 0; i < Currencies.Length; i++)
            {
                if (string.Equals(Currencies[i], currency, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            throw new ArgumentException("Moneda no admitida: " + currency + ".", nameof(currency));
        }
    }

    /// <summary>Tipo de producto.</summary>
    public enum ProductKind
    {
        /// <summary>Consumible: se puede comprar las veces que se quiera.</summary>
        Consumable = 0,

        /// <summary>Contenido consumible, pero 1 por cuenta (<c>af_starter</c>).</summary>
        OncePerAccount = 1,

        /// <summary>No consumible y restaurable (<c>af_noads</c>, skins).</summary>
        NonConsumable = 2,
    }

    /// <summary>Contenido exacto de un producto (05 §4.1). Se acredita con <c>Wallet.Apply(Transaction.Purchase(…))</c>.</summary>
    public sealed class ProductContent
    {
        /// <summary>Semillas fijas.</summary>
        public int Seeds { get; internal set; }

        /// <summary>Las semillas son las del Cántaro (variable: <c>af_piggy</c>, exactamente las acumuladas).</summary>
        public bool PiggySeeds { get; internal set; }

        /// <summary>Comodines al stock.</summary>
        public StockDelta Stock { get; internal set; }

        /// <summary>Canoas infinitas.</summary>
        public TimeSpan InfiniteCanoes { get; internal set; }

        /// <summary>Aplica una Cuia de rescate en el nivel (Balsa de rescate): la añade quien llama.</summary>
        public bool AppliesRescue { get; internal set; }

        /// <summary>Da el derecho Sin anuncios.</summary>
        public bool GrantsNoAds { get; internal set; }

        /// <summary>Da una skin de cuia (se anota el SKU en <c>entitlements.skins</c>).</summary>
        public bool GrantsSkin { get; internal set; }
    }

    /// <summary>Un SKU del catálogo.</summary>
    public sealed class StoreProduct
    {
        internal StoreProduct(string sku, string name, ProductKind kind, decimal usd, ProductContent content)
        {
            Sku = sku;
            Name = name;
            Kind = kind;
            UsdPrice = usd;
            Content = content;
            StorePrices.Local(usd, "USD"); // el ancla tiene escalón en 05 §4.2
        }

        /// <summary>ID de Play Console (glosario §11; 05 §4.1), p. ej. <c>af_coins_2</c>.</summary>
        public string Sku { get; }

        /// <summary>Nombre de tienda en español (05 §4.1).</summary>
        public string Name { get; }

        /// <summary>Tipo.</summary>
        public ProductKind Kind { get; }

        /// <summary>Precio tier-1 en USD.</summary>
        public decimal UsdPrice { get; }

        /// <summary>Contenido.</summary>
        public ProductContent Content { get; }

        /// <summary>Precio en <paramref name="currency"/> según 05 §4.2.</summary>
        public LocalPrice PriceIn(string currency)
        {
            return StorePrices.Local(UsdPrice, currency);
        }
    }

    /// <summary>
    /// Catálogo de los 13 SKU de 1.0 (05 §4.1; glosario §11): <c>af_starter</c>, <c>af_coins_2</c> … <c>af_coins_6</c>,
    /// <c>af_rescue</c>, <c>af_noads</c>, <c>af_piggy</c>, <c>af_infinite_canoes_1h</c> y <c>af_skin_m02</c> …
    /// <c>af_skin_m04</c>. <c>af_pass</c> es de la v1.2 y no está. Las semillas de los packs y el bono de Sin anuncios salen
    /// de <see cref="StoreConfig"/> (08 §9); el resto del contenido, de 05 §4.1.
    /// </summary>
    public sealed class StoreCatalog
    {
        /// <summary><c>af_starter</c>.</summary>
        public const string Starter = "af_starter";

        /// <summary><c>af_coins_2</c>: Puñado de semillas (el upsell del modal sin saldo).</summary>
        public const string Handful = "af_coins_2";

        /// <summary><c>af_rescue</c>: Balsa de rescate.</summary>
        public const string Rescue = "af_rescue";

        /// <summary><c>af_noads</c>.</summary>
        public const string NoAds = "af_noads";

        /// <summary><c>af_piggy</c>.</summary>
        public const string Piggy = "af_piggy";

        /// <summary><c>af_infinite_canoes_1h</c>.</summary>
        public const string InfiniteCanoes1h = "af_infinite_canoes_1h";

        private readonly List<StoreProduct> _products = new List<StoreProduct>();

        /// <summary>Crea el catálogo con las cifras de <paramref name="config"/>.</summary>
        /// <exception cref="InvalidOperationException">La configuración no es válida.</exception>
        public StoreCatalog(StoreConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Validate();
            Add(Starter, "Mochila de inicio", ProductKind.OncePerAccount, 0.99m, new ProductContent
            {
                Seeds = 500,
                Stock = StockDelta.Of(BoosterKind.ExtraCuia, 2).Plus(BoosterKind.Hint, 3),
            });
            string[] packNames = { "Puñado de semillas", "Bolsa de semillas", "Saco de semillas", "Costal de semillas", "Cosecha de semillas" };
            decimal[] packPrices = { 1.99m, 4.99m, 9.99m, 19.99m, 49.99m };
            for (int i = 0; i < packNames.Length; i++)
            {
                string sku = "af_coins_" + (i + 2).ToString(CultureInfo.InvariantCulture);
                Add(sku, packNames[i], ProductKind.Consumable, packPrices[i], new ProductContent { Seeds = config.SeedPackAmount(sku) });
            }

            Add(Rescue, "Balsa de rescate", ProductKind.Consumable, 4.99m, new ProductContent
            {
                AppliesRescue = true,
                Seeds = 1000,
                Stock = StockDelta.Of(BoosterKind.ExtraCuia, 1),
                InfiniteCanoes = TimeSpan.FromMinutes(30),
            });
            Add(NoAds, "Sin anuncios", ProductKind.NonConsumable, 4.99m, new ProductContent
            {
                GrantsNoAds = true,
                Seeds = config.EcoNoadsBonusSeeds,
            });
            Add(Piggy, "Cántaro", ProductKind.Consumable, 2.99m, new ProductContent { PiggySeeds = true });
            Add(InfiniteCanoes1h, "Canoas infinitas 1 h", ProductKind.Consumable, 1.99m, new ProductContent
            {
                InfiniteCanoes = TimeSpan.FromMinutes(60),
            });
            string[] skinNames = { "Pack Cuia de Santarém", "Pack Cuia del Encuentro", "Pack Totuma" };
            for (int i = 0; i < skinNames.Length; i++)
            {
                string sku = "af_skin_m0" + (i + 2).ToString(CultureInfo.InvariantCulture);
                Add(sku, skinNames[i], ProductKind.NonConsumable, 1.99m, new ProductContent { GrantsSkin = true, Seeds = 300 });
            }
        }

        /// <summary>Catálogo con los defaults de 08 §9.</summary>
        public static StoreCatalog Default => new StoreCatalog(StoreConfig.Defaults);

        /// <summary>Productos en el orden de 05 §4.1.</summary>
        public IReadOnlyList<StoreProduct> Products => _products;

        /// <summary>El producto de <paramref name="sku"/>.</summary>
        /// <exception cref="ArgumentException">El SKU no está en el catálogo de 1.0.</exception>
        public StoreProduct Get(string sku)
        {
            StoreProduct? product = Find(sku);
            return product ?? throw new ArgumentException("SKU fuera del catálogo de 1.0: " + sku, nameof(sku));
        }

        /// <summary>El producto de <paramref name="sku"/>, o null.</summary>
        public StoreProduct? Find(string? sku)
        {
            for (int i = 0; i < _products.Count; i++)
            {
                if (string.Equals(_products[i].Sku, sku, StringComparison.Ordinal))
                {
                    return _products[i];
                }
            }

            return null;
        }

        private void Add(string sku, string name, ProductKind kind, decimal usd, ProductContent content)
        {
            _products.Add(new StoreProduct(sku, name, kind, usd, content));
        }
    }
}
