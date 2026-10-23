// Q-IAP-4 (D-040, recomendada (a)): un no consumible que Play devuelve a otra instalación de la misma cuenta de Google
// con otro UID pasa al UID nuevo (`duplicate`), con tope por token, espera entre dos y tope por UID y día. Todo lo
// demás (consumibles, anuladas, sin cuenta ofuscada, otro SKU) sigue en 409. DynamoDB en memoria, sin red.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sha256hex } from '../../functions/shared/crypto.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import { uidHash } from '../../functions/shared/log.ts';
import type { PlayApi, ProductPurchase } from '../../functions/shared/playApi.ts';
import { deletedKey } from '../../functions/shared/rateLimit.ts';
import {
  REASSIGN_COOLDOWN_S,
  REASSIGN_DAILY_PER_UID,
  REASSIGN_MAX_PER_TOKEN,
  createHandler,
} from '../../functions/verify-purchase/handler.ts';
import { validator } from '../contract/ajv.ts';
import { NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const ROUTE = 'POST /v1/purchases/verify';
const TOKEN = 'tok.noads-reinstall-1';
const TH = sha256hex(TOKEN);
const OLD_UID = 'OldUid00000000000000000000aa';
const okResponse = validator('verify-purchase.response.schema.json');
const errResponse = validator('error.schema.json');
const okIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: true }) };
const DAY = 86_400_000;

class Play implements PlayApi {
  /** Lo que Play devuelve para cualquier token: comprado con OLD_UID y ya reconocido. */
  purchase: ProductPurchase | null = {
    state: 'purchased',
    acknowledged: true,
    purchaseTimeMs: NOW - 10 * DAY,
    orderId: 'GPA.1',
    regionCode: 'CO',
    quantity: 1,
    obfuscatedAccountId: OLD_UID,
  };
  gets = 0;
  acks: string[] = [];
  getProduct = (): Promise<ProductPurchase | null> => {
    this.gets++;
    return Promise.resolve(this.purchase);
  };
  acknowledge = (_sku: string, token: string): Promise<void> => {
    this.acks.push(token);
    return Promise.resolve();
  };
  listVoided = vi.fn();
}

describe('verifyPurchase · reasignación de no consumibles (Q-IAP-4)', () => {
  let db: MemDb;
  let play: Play;
  let now: number;
  let n = 0;

  beforeEach(() => {
    db = new MemDb();
    play = new Play();
    now = NOW;
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => {
    db.restore();
    vi.restoreAllMocks();
  });

  const handler = () =>
    createHandler(() => ({ ddb: db.client, play, integrity: okIntegrity, env: { ...TABLES, AF_ENV: 'dev' }, now: () => now }));
  const call = async (uid: string, body: unknown = { token: TOKEN, sku: 'af_noads' }) =>
    res(await handler()(httpEvent({ routeKey: ROUTE, body, uid, headers: { 'x-firebase-appcheck': `i${++n}` } })));
  /** La fila que dejó el `ok` del UID que compró. */
  const seed = (over: Record<string, unknown> = {}, token = TOKEN): void =>
    db.put(TABLES.AF_TABLE_PURCHASES, {
      token_hash: sha256hex(token),
      uid: OLD_UID,
      sku: 'af_noads',
      order_id: 'GPA.1',
      ts: NOW - 10 * DAY,
      verified_at: NOW - 10 * DAY,
      country: 'CO',
      state: 'ok',
      ...over,
    });
  const row = (token = TOKEN) => db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex(token) });

  it('no consumible de otro UID que Play da como comprado por esa cuenta → duplicate y la fila pasa al UID nuevo', async () => {
    seed();
    const r = await call(UID);
    expect(r.status).toBe(200);
    expect(r.body).toEqual({ status: 'duplicate', token_hash: TH });
    expect(okResponse(r.body), JSON.stringify(okResponse.errors)).toBe(true);
    // Se conserva la compra original (pedido, fecha, verificación) y se apunta el historial sin identificadores.
    expect(row()).toMatchObject({
      uid: UID,
      sku: 'af_noads',
      order_id: 'GPA.1',
      ts: NOW - 10 * DAY,
      verified_at: NOW - 10 * DAY,
      state: 'ok',
      reassign_seq: 1,
      reassign_count: 1,
      reassigned_at: NOW,
      reassign_log: [{ at: NOW, kind: 'other' }],
    });
    expect(JSON.stringify(row()!.reassign_log)).not.toContain(OLD_UID);
    expect(play.acks).toEqual([]); // ya estaba reconocida
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(1);
  });

  it('las skins también; la cuenta ofuscada puede venir como SHA-256 del UID', async () => {
    seed({ sku: 'af_skin_m03' });
    play.purchase = { ...play.purchase!, obfuscatedAccountId: sha256hex(OLD_UID) };
    expect((await call(UID, { token: TOKEN, sku: 'af_skin_m03' })).body.status).toBe('duplicate');
    expect(row()).toMatchObject({ uid: UID, reassign_count: 1 });
  });

  it('consumible de otro UID → sigue en 409, sin llamar a Play ni tocar la fila', async () => {
    seed({ sku: 'af_coins_2' });
    const r = await call(UID, { token: TOKEN, sku: 'af_coins_2' });
    expect(r.status).toBe(409);
    expect(r.body).toMatchObject({ error: { code: 'token_belongs_to_other_user' } });
    expect(errResponse(r.body)).toBe(true);
    expect(play.gets).toBe(0);
    expect(row()).toMatchObject({ uid: OLD_UID });
    expect(row()!.reassign_seq).toBeUndefined();
    // Tampoco la Mochila (once_per_account) ni un consumible sin fila comprado con otra cuenta.
    seed({ sku: 'af_starter' }, 'starter');
    expect((await call(UID, { token: 'starter', sku: 'af_starter' })).status).toBe(409);
    expect((await call(UID, { token: 'coins-new', sku: 'af_coins_3' })).status).toBe(409);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(2);
  });

  it('anulada: en el libro mayor (voided), lápida o cancelada en Play → no se reasigna', async () => {
    seed({ state: 'voided', voided_at: NOW - DAY });
    expect((await call(UID)).status).toBe(409);
    expect(play.gets).toBe(0);
    expect(row()).toMatchObject({ uid: OLD_UID, state: 'voided' });

    // Lápida del job (anulada antes de verificarse): 400 como siempre.
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tomb'), sku: 'af_noads', state: 'voided', tombstone: true });
    expect((await call(UID, { token: 'tomb', sku: 'af_noads' })).status).toBe(400);

    // Reembolsada en Play y el job aún no ha pasado: Play la da como cancelada → 409 y la fila no cambia.
    seed({}, 'refunded');
    play.purchase = { ...play.purchase!, state: 'canceled' };
    expect((await call(UID, { token: 'refunded', sku: 'af_noads' })).status).toBe(409);
    expect(row('refunded')).toMatchObject({ uid: OLD_UID });
    // Play ya no la conoce.
    play.purchase = null;
    expect((await call(UID, { token: 'refunded', sku: 'af_noads' })).status).toBe(409);
    expect(row('refunded')!.reassign_seq).toBeUndefined();
  });

  it('sin cuenta ofuscada en Play, o con otro SKU que el de la fila → 409', async () => {
    seed();
    play.purchase = { ...play.purchase!, obfuscatedAccountId: undefined };
    expect((await call(UID)).status).toBe(409);
    play.purchase = { ...play.purchase, obfuscatedAccountId: OLD_UID };
    expect((await call(UID, { token: TOKEN, sku: 'af_skin_m02' })).status).toBe(409);
    expect(row()).toMatchObject({ uid: OLD_UID });
  });

  it('idempotente: repetir la petición responde duplicate sin volver a Play, sin gastar cupo ni repetir historial', async () => {
    seed();
    expect((await call(UID)).body.status).toBe('duplicate');
    const gets = play.gets;
    now += 3 * DAY;
    expect((await call(UID)).body).toEqual({ status: 'duplicate', token_hash: TH });
    expect((await call(UID)).body.status).toBe('duplicate');
    expect(play.gets).toBe(gets);
    expect(row()).toMatchObject({ uid: UID, reassign_seq: 1, reassign_count: 1, reassigned_at: NOW });
    expect(row()!.reassign_log).toHaveLength(1);
  });

  it('carrera: dos peticiones a la vez del UID nuevo → las dos duplicate y una sola reasignación', async () => {
    seed();
    const h = handler();
    const ev = (i: number) =>
      httpEvent({ routeKey: ROUTE, body: { token: TOKEN, sku: 'af_noads' }, uid: UID, headers: { 'x-firebase-appcheck': `race${i}` } });
    const out = (await Promise.all([h(ev(1)), h(ev(2))])).map(res);
    expect(out.map((o) => o.body.status)).toEqual(['duplicate', 'duplicate']);
    expect(row()).toMatchObject({ uid: UID, reassign_seq: 1, reassign_count: 1 });
  });

  it('carrera entre dos UID nuevos → uno se la lleva y el otro recibe 409', async () => {
    seed();
    const h = handler();
    const ev = (uid: string, i: number) =>
      httpEvent({ routeKey: ROUTE, body: { token: TOKEN, sku: 'af_noads' }, uid, headers: { 'x-firebase-appcheck': `r${i}` } });
    const out = (await Promise.all([h(ev(UID, 1)), h(ev(OTHER_UID, 2))])).map(res);
    expect(out.filter((o) => o.body.status === 'duplicate')).toHaveLength(1);
    expect(out.filter((o) => o.status === 409)).toHaveLength(1);
    expect(row()).toMatchObject({ reassign_seq: 1, reassign_count: 1 });
  });

  it(`límite: espera de ${REASSIGN_COOLDOWN_S / 3600} h entre dos y como mucho ${REASSIGN_MAX_PER_TOKEN} por token`, async () => {
    seed();
    const uids = Array.from({ length: REASSIGN_MAX_PER_TOKEN + 1 }, (_, i) => `NewUid${String(i).padStart(22, '0')}`);
    expect((await call(uids[0])).body.status).toBe('duplicate');
    // Otro UID antes de que pase la espera → 409; la fila no se mueve.
    now += REASSIGN_COOLDOWN_S * 1000 - 1;
    expect((await call(uids[1])).status).toBe(409);
    expect(row()).toMatchObject({ uid: uids[0], reassign_count: 1 });
    // Pasada la espera, sí; hasta el tope.
    for (let i = 1; i < REASSIGN_MAX_PER_TOKEN; i++) {
      now += REASSIGN_COOLDOWN_S * 1000;
      expect((await call(uids[i])).body.status, `reasignación ${i + 1}`).toBe('duplicate');
    }
    expect(row()).toMatchObject({ uid: uids[REASSIGN_MAX_PER_TOKEN - 1], reassign_count: REASSIGN_MAX_PER_TOKEN });
    now += 30 * DAY;
    const over = await call(uids[REASSIGN_MAX_PER_TOKEN]);
    expect(over.status).toBe(409);
    expect(over.body).toMatchObject({ error: { code: 'token_belongs_to_other_user' } });
    expect(row()).toMatchObject({ uid: uids[REASSIGN_MAX_PER_TOKEN - 1], reassign_count: REASSIGN_MAX_PER_TOKEN });
    expect(row()!.reassign_log).toHaveLength(REASSIGN_MAX_PER_TOKEN);
  });

  it('el UID que compró (cuenta ofuscada de Play) la recupera siempre, sin gastar cupo ni esperar', async () => {
    seed({ reassign_seq: REASSIGN_MAX_PER_TOKEN, reassign_count: REASSIGN_MAX_PER_TOKEN, reassigned_at: NOW, uid: UID });
    const r = await call(OLD_UID);
    expect(r.body).toEqual({ status: 'duplicate', token_hash: TH });
    expect(row()).toMatchObject({
      uid: OLD_UID,
      reassign_seq: REASSIGN_MAX_PER_TOKEN + 1,
      reassign_count: REASSIGN_MAX_PER_TOKEN,
      reassigned_at: NOW,
    });
    expect(row()!.reassign_log).toEqual([{ at: NOW, kind: 'owner' }]);
  });

  it(`límite por UID: como mucho ${REASSIGN_DAILY_PER_UID} reasignaciones al día → 429`, async () => {
    const tokens = Array.from({ length: REASSIGN_DAILY_PER_UID + 1 }, (_, i) => `stolen-${i}`);
    for (const t of tokens) seed({}, t);
    for (const t of tokens.slice(0, REASSIGN_DAILY_PER_UID)) expect((await call(UID, { token: t, sku: 'af_noads' })).body.status).toBe('duplicate');
    const r = await call(UID, { token: tokens[REASSIGN_DAILY_PER_UID], sku: 'af_noads' });
    expect(r.status).toBe(429);
    expect(r.body).toMatchObject({ error: { code: 'rate_limited' } });
    expect(errResponse(r.body)).toBe(true);
    expect(row(tokens[REASSIGN_DAILY_PER_UID])).toMatchObject({ uid: OLD_UID });
    now += DAY;
    expect((await call(UID, { token: tokens[REASSIGN_DAILY_PER_UID], sku: 'af_noads' })).body.status).toBe('duplicate');
  });

  it('fila anonimizada (el dueño borró su cuenta) → se reasigna como anonymized', async () => {
    seed({ uid: undefined, anonymized_at: NOW - DAY });
    expect((await call(UID)).body.status).toBe('duplicate');
    expect(row()).toMatchObject({ uid: UID, reassign_count: 1, reassign_log: [{ at: NOW, kind: 'anonymized' }] });
  });

  it('cuenta borrada mientras tanto → 401 y la fila no cambia', async () => {
    seed();
    db.hookNext = {
      command: 'TransactWrite',
      table: TABLES.AF_TABLE_PURCHASES,
      fn: () => db.put(TABLES.AF_TABLE_RATE_LIMITS, { key: deletedKey(uidHash(UID)), ttl: Math.floor(NOW / 1000) + 3600 }),
    };
    expect((await call(UID)).status).toBe(401);
    expect(row()).toMatchObject({ uid: OLD_UID });
  });

  it('sin fila (comprado con otro UID y nunca verificado) → ok con grant a este UID, contando como reasignación', async () => {
    play.purchase = { ...play.purchase!, acknowledged: false };
    const r = await call(UID, { token: 'unverified', sku: 'af_noads' });
    expect(r.body).toMatchObject({ status: 'ok', grant: { seeds: 500 }, entitlements: { no_ads: true } });
    expect(okResponse(r.body)).toBe(true);
    expect(play.acks).toEqual(['unverified']);
    expect(row('unverified')).toMatchObject({ uid: UID, state: 'ok', reassign_seq: 1, reassign_count: 1, reassign_log: [{ at: NOW, kind: 'claim' }] });
    // Otro UID al momento: espera → 409; el que compró la recupera como dueño.
    expect((await call(OTHER_UID, { token: 'unverified', sku: 'af_noads' })).status).toBe(409);
    expect((await call(OLD_UID, { token: 'unverified', sku: 'af_noads' })).body.status).toBe('duplicate');
    expect(row('unverified')).toMatchObject({ uid: OLD_UID, reassign_count: 1, reassign_seq: 2 });
  });

  it('sin fila y pendiente de pago con otra cuenta → 409, como antes', async () => {
    play.purchase = { ...play.purchase!, state: 'pending' };
    expect((await call(UID, { token: 'pend', sku: 'af_noads' })).status).toBe(409);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
  });

  it('restoreLedger: tras la reasignación, la compra sale en el UID nuevo y no en el viejo (índice by_uid)', async () => {
    seed();
    await call(UID);
    const byUid = (u: string) => db.items(TABLES.AF_TABLE_PURCHASES).filter((it) => it.uid === u);
    expect(byUid(UID)).toHaveLength(1);
    expect(byUid(OLD_UID)).toHaveLength(0);
  });

  it('los logs no llevan UID ni token', async () => {
    seed();
    const warn = vi.spyOn(console, 'warn');
    const logSpy = vi.spyOn(console, 'log');
    await call(UID);
    for (const c of [...warn.mock.calls, ...logSpy.mock.calls]) {
      for (const secret of [UID, OLD_UID, TOKEN]) expect(String(c[0])).not.toContain(secret);
    }
    expect(warn.mock.calls.some((c) => String(c[0]).includes('purchase_reassigned'))).toBe(true);
  });
});
