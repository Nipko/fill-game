// Arreglos de la revisión de seguridad: event_id de MAX de un solo uso, marca de cuenta borrada, límite por UID
// en verifyPurchase, límite de delete-confirm y cantidad solo en consumibles.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CATALOG, grantFor } from '../../functions/shared/catalog.ts';
import type { FirebaseAdmin } from '../../functions/shared/firebaseAdmin.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import type { PlayApi, ProductPurchase } from '../../functions/shared/playApi.ts';
import { DELETED_MARK_TTL_S } from '../../functions/shared/rateLimit.ts';
import { LIMITS, createHandler as deleteHandler } from '../../functions/delete-account/handler.ts';
import { createHandler as ssvHandler } from '../../functions/rewarded-ssv/handler.ts';
import { expectedEventToken } from '../../functions/rewarded-ssv/maxSignature.ts';
import { VERIFY_HOURLY_LIMIT, createHandler as verifyHandler } from '../../functions/verify-purchase/handler.ts';
import { MapSecrets, NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const N1 = '6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f';
const N2 = '0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d';
const okIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: true }) };

let db: MemDb;
let now: number;
let n = 0;
beforeEach(() => {
  db = new MemDb();
  now = NOW;
  vi.spyOn(console, 'log').mockImplementation(() => undefined);
  vi.spyOn(console, 'warn').mockImplementation(() => undefined);
  vi.spyOn(console, 'error').mockImplementation(() => undefined);
});
afterEach(() => {
  db.restore();
  vi.restoreAllMocks();
});

describe('rewardedSsv: event_id de un solo uso', () => {
  const KEY = 'clave';
  const secrets = new MapSecrets();
  secrets.values.set('arn:max', KEY);
  const call = async (uid: string, nonce: string, eventId: string) =>
    res(
      await ssvHandler(() => ({ ddb: db.client, secrets, env: { ...TABLES, AF_SECRET_MAX_SSV: 'arn:max' }, now: () => now }))(
        httpEvent({
          routeKey: 'GET /v1/ssv',
          query: { user_id: uid, custom_data: `rescue|${nonce}`, event_id: eventId, event_token: expectedEventToken(eventId, KEY) },
        }),
      ),
    );

  it('una firma válida no sirve para otro nonce ni para otro UID', async () => {
    expect((await call(UID, N1, 'ev-1')).body).toEqual({ status: 'ok' });
    const otherNonce = await call(UID, N2, 'ev-1');
    expect(otherNonce.status).toBe(403);
    expect(otherNonce.body).toMatchObject({ error: { code: 'bad_signature' } });
    expect((await call(OTHER_UID, N2, 'ev-1')).status).toBe(403);
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N2 })).toBeUndefined();
  });

  it('el reintento de MAX con los mismos valores responde duplicate y no gasta cupo', async () => {
    await call(UID, N1, 'ev-1');
    const counter = () => db.items(TABLES.AF_TABLE_RATE_LIMITS).find((i) => String(i.key).startsWith('ssv#'))?.count;
    expect(counter()).toBe(1);
    expect((await call(UID, N1, 'ev-1')).body).toEqual({ status: 'duplicate' });
    expect(counter()).toBe(1);
  });

  it('si la primera vez falló al escribir la recompensa, el reintento la escribe', async () => {
    db.failNext = { command: 'Put', table: TABLES.AF_TABLE_REWARDS, error: new Error('boom') };
    expect((await call(UID, N1, 'ev-1')).status).toBe(500);
    expect((await call(UID, N1, 'ev-1')).body).toEqual({ status: 'ok' });
  });
});

class Play implements PlayApi {
  purchase: ProductPurchase = { state: 'purchased', acknowledged: true, purchaseTimeMs: NOW, quantity: 1, obfuscatedAccountId: UID };
  getProduct = () => Promise.resolve(this.purchase);
  acknowledge = () => Promise.resolve();
  listVoided = vi.fn();
}

class Auth implements FirebaseAdmin {
  deleteUser = () => Promise.resolve({ existed: true });
}

describe('marca de cuenta borrada (el ID token vale hasta 1 h tras el borrado)', () => {
  const del = deleteHandler(() => ({
    ddb: db.client,
    firebase: new Auth(),
    integrity: okIntegrity,
    env: { ...TABLES, AF_ENV: 'dev' },
    now: () => now,
    random: (k: number) => Buffer.alloc(k, ++n % 250),
  }));
  const verify = verifyHandler(() => ({ ddb: db.client, play: new Play(), integrity: okIntegrity, env: { ...TABLES, AF_ENV: 'dev' }, now: () => now }));
  const deleteApp = async () =>
    res(await del(httpEvent({ routeKey: 'POST /v1/account/delete', body: { confirm: true }, uid: UID, headers: { 'x-firebase-appcheck': `t${++n}` } })));

  it('tras borrar, ni un código nuevo ni una compra nueva quedan a nombre del UID; el borrado se puede repetir', async () => {
    expect((await deleteApp()).status).toBe(200);
    const code = res(await del(httpEvent({ routeKey: 'POST /v1/account/code', uid: UID })));
    expect(code.status).toBe(401);
    expect(db.items(TABLES.AF_TABLE_ACCOUNT_CODES)).toEqual([]);
    const buy = res(await verify(httpEvent({ routeKey: 'POST /v1/purchases/verify', body: { token: 'tk', sku: 'af_coins_2' }, uid: UID, headers: { 'x-firebase-appcheck': 'a' } })));
    expect(buy.status).toBe(401);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toEqual([]);
    expect((await deleteApp()).status).toBe(200);
    // La marca no lleva el UID en claro y caduca a las 2 h.
    expect(JSON.stringify(db.items(TABLES.AF_TABLE_RATE_LIMITS))).not.toContain(UID);
    now = NOW + DELETED_MARK_TTL_S * 1000 + 1000;
    expect(res(await del(httpEvent({ routeKey: 'POST /v1/account/code', uid: UID }))).status).toBe(200);
  });

  it('otro UID no se ve afectado', async () => {
    await deleteApp();
    expect(res(await del(httpEvent({ routeKey: 'POST /v1/account/code', uid: OTHER_UID }))).status).toBe(200);
  });
});

describe('límites', () => {
  it(`verifyPurchase: ${VERIFY_HOURLY_LIMIT} por UID y hora`, async () => {
    const verify = verifyHandler(() => ({ ddb: db.client, play: new Play(), integrity: okIntegrity, env: { ...TABLES, AF_ENV: 'dev' }, now: () => now }));
    for (let i = 0; i < VERIFY_HOURLY_LIMIT; i++) {
      const r = res(await verify(httpEvent({ routeKey: 'POST /v1/purchases/verify', body: { token: `t${i}`, sku: 'af_coins_2' }, uid: UID, headers: { 'x-firebase-appcheck': `a${i}` } })));
      expect(r.status).toBe(200);
    }
    const r = res(await verify(httpEvent({ routeKey: 'POST /v1/purchases/verify', body: { token: 'tz', sku: 'af_coins_2' }, uid: UID, headers: { 'x-firebase-appcheck': 'az' } })));
    expect(r.status).toBe(429);
  });

  it(`delete-confirm: ${LIMITS.deleteConfirm.limit} por token y 5 minutos`, async () => {
    const del = deleteHandler(() => ({ ddb: db.client, firebase: new Auth(), integrity: okIntegrity, env: TABLES, now: () => now, random: (k: number) => Buffer.alloc(k, 7) }));
    const tok = 'q7Yt0dVx3kM9rB2nL5sW8eJ1uC4hF6gA0zXyPiQoRkE';
    for (let i = 0; i < LIMITS.deleteConfirm.limit; i++) {
      expect(res(await del(httpEvent({ routeKey: 'POST /v1/account/delete-confirm', body: { request_token: tok } }))).status).toBe(410);
    }
    expect(res(await del(httpEvent({ routeKey: 'POST /v1/account/delete-confirm', body: { request_token: tok } }))).status).toBe(429);
  });
});

describe('catálogo: cantidad', () => {
  it('solo los consumibles se multiplican por la cantidad', () => {
    expect(grantFor(CATALOG.get('af_coins_2')!, 3).seeds).toBe(3000);
    expect(grantFor(CATALOG.get('af_noads')!, 3).seeds).toBe(500);
    expect(grantFor(CATALOG.get('af_starter')!, 2).items).toEqual([{ id: 'extra_cuia', count: 2 }, { id: 'hint', count: 3 }]);
  });
});
