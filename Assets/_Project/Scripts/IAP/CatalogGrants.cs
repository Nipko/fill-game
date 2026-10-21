#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend.Contract;
using AF.Economy;

namespace AF.IAP
{
    /// <summary>
    /// El <c>grant</c> y los <c>entitlements</c> que el servidor devuelve con <c>ok</c> para cada producto, calculados desde el catálogo
    /// del cliente (espejo de <c>infra/functions/shared/catalog.ts</c>, que a su vez espeja 05 §4.1 con los defaults de 08 §9): semillas
    /// fijas, comodines por su id de 04 §11, <c>rescue_cuia</c>, <c>infinite_canoes_min</c> y, en el Cántaro, semillas 0 más el ítem
    /// <c>piggy</c> [P]. Sirve para que <see cref="AF.Backend.FakeBackendClient"/> responda como el servidor real (editor, tests y builds
    /// sin backend) y para los tests de paridad.
    /// </summary>
    public static class CatalogGrants
    {
        /// <summary>El <c>grant</c> de <paramref name="sku"/>.</summary>
        /// <exception cref="ArgumentException">El SKU no está en <paramref name="catalog"/>.</exception>
        public static PurchaseGrant For(StoreCatalog catalog, string sku)
        {
            ProductContent c = (catalog ?? throw new ArgumentNullException(nameof(catalog))).Get(sku).Content;
            var items = new List<GrantItem>();
            for (int i = 0; i < BoosterKinds.Count; i++)
            {
                var kind = (BoosterKind)i;
                int units = c.Stock.Get(kind);
                if (units > 0)
                {
                    items.Add(new GrantItem(kind.ToId(), units));
                }
            }

            if (c.AppliesRescue)
            {
                items.Add(new GrantItem(GrantItemIds.RescueCuia, 1));
            }

            if (c.InfiniteCanoes > TimeSpan.Zero)
            {
                items.Add(new GrantItem(GrantItemIds.InfiniteCanoesMinutes, (long)c.InfiniteCanoes.TotalMinutes));
            }

            if (c.PiggySeeds)
            {
                items.Add(new GrantItem(GrantItemIds.Piggy, 1));
            }

            return new PurchaseGrant(c.PiggySeeds ? 0 : c.Seeds, items);
        }

        /// <summary>Los <c>entitlements</c> de <paramref name="sku"/> (Sin anuncios y skins), o null si no da ninguno.</summary>
        public static Entitlements? EntitlementsFor(StoreCatalog catalog, string sku)
        {
            StoreProduct product = (catalog ?? throw new ArgumentNullException(nameof(catalog))).Get(sku);
            if (product.Content.GrantsNoAds)
            {
                return new Entitlements(noAds: true);
            }

            return product.Content.GrantsSkin ? new Entitlements(skins: new[] { product.Sku }) : null;
        }
    }
}
