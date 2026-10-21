// Lo que concede cada SKU de 1.0 (05 §4.1; espejo de Assets/_Project/Scripts/IAP/StoreCatalog.cs con los
// defaults de 08 §9: eco_seed_pack_amounts y eco_noads_bonus_seeds). Es la respuesta `grant` de verifyPurchase
// (08 §10.3); el cliente acredita con su propio catálogo y usa esto para cuadrar. Ids de comodín de 04 §11
// (booster ∈ {undo, extra_cuia, hint, shuffle, reveal, skip}).
//
// [P] Ids sin fuente en el GDD: `rescue_cuia` (la Cuia de rescate que la Balsa aplica al instante) e
// `infinite_canoes_min` (minutos de Canoas infinitas). [P] `af_piggy` concede las semillas acumuladas en el
// Cántaro, que solo conoce el cliente: el servidor devuelve seeds = 0 y el ítem `piggy` (pregunta al dueño).
// Si Remote Config cambia las cifras de los packs, este espejo no las ve (pregunta al dueño).
//
// `once_per_account` (af_starter, 05 §4.1) lo hace cumplir el servidor (F2c): verifyPurchase escribe, en la misma
// transacción que la fila del libro mayor, la marca onceKey(sku, uid) en af-<env>-rate_limits (sin TTL; la borra
// deleteAccount). Una segunda compra con otro token no concede (400) ni se reconoce: Play la reembolsa sola.
import { sha256hex } from './crypto.ts';

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
  // Varias unidades solo tienen sentido en los consumibles; Sin anuncios, skins y la Mochila son una.
  const q = e.kind === 'consumable' ? Math.max(1, Math.floor(quantity)) : 1;
  return { seeds: e.grant.seeds * q, items: e.grant.items.map((i) => ({ id: i.id, count: i.count * q })) };
}

/** SKU que solo se pueden comprar una vez por cuenta (UID). */
export const ONCE_PER_ACCOUNT_SKUS: readonly string[] = [...CATALOG.values()].filter((e) => e.kind === 'once_per_account').map((e) => e.sku);

/**
 * Clave de la marca «ya comprado» de un SKU `once_per_account` en af-<env>-rate_limits. Lleva el SHA-256 completo
 * (no el uidHash corto de los logs: una colisión negaría una compra) y nunca el UID en claro.
 */
export function onceKey(sku: string, uid: string): string {
  return `once#${sku}#${sha256hex(`af-once:${uid}`)}`;
}
