// Lo que concede cada SKU de 1.0 (05 §4.1; espejo de Assets/_Project/Scripts/IAP/StoreCatalog.cs con los
// defaults de 08 §9: eco_seed_pack_amounts y eco_noads_bonus_seeds). Es la respuesta `grant` de verifyPurchase
// (08 §10.3); el cliente acredita con su propio catálogo y usa esto para cuadrar. Ids de comodín de 04 §11
// (booster ∈ {undo, extra_cuia, hint, shuffle, reveal, skip}).
//
// [P] Ids sin fuente en el GDD: `rescue_cuia` (la Cuia de rescate que la Balsa aplica al instante) e
// `infinite_canoes_min` (minutos de Canoas infinitas). [P] `af_piggy` concede las semillas acumuladas en el
// Cántaro, que solo conoce el cliente: el servidor devuelve seeds = 0 y el ítem `piggy` (pregunta al dueño).
// Si Remote Config cambia las cifras de los packs, este espejo no las ve (pregunta al dueño).

export interface GrantItem {
  readonly id: string;
  readonly count: number;
}

export interface Grant {
  readonly seeds: number;
  readonly items: readonly GrantItem[];
}

export interface Entitlements {
  readonly no_ads?: boolean;
  readonly skins?: readonly string[];
}

export interface CatalogEntry {
  readonly sku: string;
  readonly kind: 'consumable' | 'once_per_account' | 'non_consumable';
  readonly grant: Grant;
  readonly entitlements?: Entitlements;
}

const entry = (
  sku: string,
  kind: CatalogEntry['kind'],
  seeds: number,
  items: GrantItem[] = [],
  entitlements?: Entitlements,
): CatalogEntry => ({ sku, kind, grant: { seeds, items }, entitlements });

const SEED_PACKS: Record<string, number> = {
  af_coins_2: 1000,
  af_coins_3: 2800,
  af_coins_4: 6000,
  af_coins_5: 13000,
  af_coins_6: 36000,
};

export const CATALOG: ReadonlyMap<string, CatalogEntry> = new Map(
  [
    entry('af_starter', 'once_per_account', 500, [
      { id: 'extra_cuia', count: 2 },
      { id: 'hint', count: 3 },
    ]),
    ...Object.entries(SEED_PACKS).map(([sku, seeds]) => entry(sku, 'consumable', seeds)),
    entry('af_rescue', 'consumable', 1000, [
      { id: 'rescue_cuia', count: 1 },
      { id: 'extra_cuia', count: 1 },
      { id: 'infinite_canoes_min', count: 30 },
    ]),
    entry('af_noads', 'non_consumable', 500, [], { no_ads: true }),
    entry('af_piggy', 'consumable', 0, [{ id: 'piggy', count: 1 }]),
    entry('af_infinite_canoes_1h', 'consumable', 0, [{ id: 'infinite_canoes_min', count: 60 }]),
    ...[2, 3, 4].map((w) => entry(`af_skin_m0${w}`, 'non_consumable', 300, [], { skins: [`af_skin_m0${w}`] })),
  ].map((e) => [e.sku, e]),
);

/** Lo que concede `quantity` unidades del SKU (Play admite compras de varias unidades si se activan). */
export function grantFor(e: CatalogEntry, quantity: number): Grant {
  const q = Math.max(1, Math.floor(quantity));
  return { seeds: e.grant.seeds * q, items: e.grant.items.map((i) => ({ id: i.id, count: i.count * q })) };
}
