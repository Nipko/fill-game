#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.Localization;

namespace AF.IAP
{
    /// <summary>
    /// Precio en una moneda, con el importe de 05 §4.2 y los decimales de la moneda. Se escribe con la función única de precios
    /// (<see cref="Loc.Price"/>; D-025): código ISO 4217 e importe con los separadores del idioma del juego y, en
    /// español, del país (observación 22 de la jornada 6), p. ej. <c>COP 3.900</c> en pt-BR o en es-419 en Colombia y <c>COP 3,900</c>
    /// en en.
    /// </summary>
    public readonly struct LocalPrice : IEquatable<LocalPrice>
    {
        /// <summary>
        /// Crea el precio. Cualquier código ISO 4217: los de la tienda real llegan en la moneda de la cuenta de Play del jugador (D-040;
        /// Q-IAP-3), también fuera de las cinco columnas de 05 §4.2 (<see cref="StorePrices.Currencies"/>, que siguen siendo las únicas
        /// de la tienda simulada).
        /// </summary>
        /// <exception cref="ArgumentException">No es un código ISO 4217 de tres letras mayúsculas.</exception>
        public LocalPrice(string currency, decimal amount)
        {
            Decimals = LocPrice.DecimalsOf(currency);
            Currency = currency;
            Amount = amount;
            decimal scale = 1m;
            for (int i = 0; i < Decimals; i++)
            {
                scale *= 10m;
            }

            MinorUnits = (long)Math.Round(amount * scale, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>Código ISO 4217 (en la tienda simulada, <c>USD</c>, <c>BRL</c>, <c>MXN</c>, <c>COP</c> o <c>PEN</c>).</summary>
        public string Currency { get; }

        /// <summary>Importe.</summary>
        public decimal Amount { get; }

        /// <summary>
        /// Cifras decimales con que se escribe (<see cref="LocPrice.DecimalsOf"/>: 0 en COP y MXN, 2 en el resto, 05 §4.2; y, por el
        /// añadido de D-025 de Q-IAP-3 y D-057, la tabla de ISO 4217: 0 en KRW, VND, CLP, JPY…, 3 en KWD, BHD…).
        /// </summary>
        public int Decimals { get; }

        /// <summary>Importe en unidades menores de <see cref="Decimals"/> (BRL 4,99 → 499; COP 3.900 → 3900).</summary>
        public long MinorUnits { get; }

        /// <summary>
        /// Solo el importe, con los separadores de la región (<see cref="Loc.PriceAmount"/>; D-025), p. ej. <c>1,99</c> en es-CO y
        /// <c>1.99</c> en en-US.
        /// </summary>
        public string FormatAmount()
        {
            return Loc.PriceAmount(Currency, MinorUnits);
        }

        /// <summary>Moneda e importe, con la función única de precios (<see cref="Loc.Price"/>; D-025), p. ej. <c>COP 3.900</c>.</summary>
        public override string ToString()
        {
            return Loc.Price(Currency, MinorUnits);
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

        // Los decimales de cada moneda los fija la función única de precios (D-025): LocPrice.DecimalsOf.
        internal static int DecimalsOf(string currency)
        {
            Column(currency);
            return LocPrice.DecimalsOf(currency);
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
        internal StoreProduct(string sku, ProductKind kind, decimal usd, ProductContent content)
        {
            Sku = sku;
            NameKey = NameKeyOf(sku);
            Kind = kind;
            UsdPrice = usd;
            Content = content;
            StorePrices.Local(usd, "USD"); // el ancla tiene escalón en 05 §4.2
        }

        /// <summary>ID de Play Console (glosario §11; 05 §4.1), p. ej. <c>af_coins_2</c>.</summary>
        public string Sku { get; }

        /// <summary>
        /// Clave del nombre de tienda en la tabla de textos (<c>iap_&lt;sku&gt;_name</c> [P]; 05 §4.1 da el texto en español,
        /// D-018: toda cadena visible por clave). <c>AF.IAP</c> no lleva ningún texto visible.
        /// </summary>
        public string NameKey { get; }

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

        /// <summary>Clave del nombre de tienda de <paramref name="sku"/>: <c>iap_&lt;sku&gt;_name</c> [P] (p. ej. <c>iap_af_coins_2_name</c>).</summary>
        public static string NameKeyOf(string sku)
        {
            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            return "iap_" + sku + "_name";
        }
    }

    /// <summary>
    /// Catálogo de los 13 SKU de 1.0 (05 §4.1; glosario §11): <c>af_starter</c>, <c>af_coins_2</c> … <c>af_coins_6</c>,
    /// <c>af_rescue</c>, <c>af_noads</c>, <c>af_piggy</c>, <c>af_infinite_canoes_1h</c> y <c>af_skin_m02</c> …
    /// <c>af_skin_m04</c>. <c>af_pass</c> es de la v1.2 y no está. Las semillas de los packs y el bono de Sin anuncios salen
    /// de <see cref="StoreConfig"/> (08 §9); el resto del contenido, de 05 §4.1. Los nombres de tienda no están aquí: cada
    /// producto lleva su clave (<see cref="StoreProduct.NameKey"/>) y el texto vive en la tabla de textos (D-018).
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
            Add(Starter, ProductKind.OncePerAccount, 0.99m, new ProductContent
            {
                Seeds = 500,
                Stock = StockDelta.Of(BoosterKind.ExtraCuia, 2).Plus(BoosterKind.Hint, 3),
            });
            decimal[] packPrices = { 1.99m, 4.99m, 9.99m, 19.99m, 49.99m };
            for (int i = 0; i < packPrices.Length; i++)
            {
                string sku = "af_coins_" + (i + 2).ToString(CultureInfo.InvariantCulture);
                Add(sku, ProductKind.Consumable, packPrices[i], new ProductContent { Seeds = config.SeedPackAmount(sku) });
            }

            Add(Rescue, ProductKind.Consumable, 4.99m, new ProductContent
            {
                AppliesRescue = true,
                Seeds = 1000,
                Stock = StockDelta.Of(BoosterKind.ExtraCuia, 1),
                InfiniteCanoes = TimeSpan.FromMinutes(30),
            });
            Add(NoAds, ProductKind.NonConsumable, 4.99m, new ProductContent
            {
                GrantsNoAds = true,
                Seeds = config.EcoNoadsBonusSeeds,
            });
            Add(Piggy, ProductKind.Consumable, 2.99m, new ProductContent { PiggySeeds = true });
            Add(InfiniteCanoes1h, ProductKind.Consumable, 1.99m, new ProductContent
            {
                InfiniteCanoes = TimeSpan.FromMinutes(60),
            });
            for (int world = 2; world <= 4; world++)
            {
                // af_skin_m02 … af_skin_m04: los packs de skins de los mundos 2-4 (05 §4.1).
                string sku = "af_skin_m0" + world.ToString(CultureInfo.InvariantCulture);
                Add(sku, ProductKind.NonConsumable, 1.99m, new ProductContent { GrantsSkin = true, Seeds = 300 });
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

        private void Add(string sku, ProductKind kind, decimal usd, ProductContent content)
        {
            _products.Add(new StoreProduct(sku, kind, usd, content));
        }
    }
}
