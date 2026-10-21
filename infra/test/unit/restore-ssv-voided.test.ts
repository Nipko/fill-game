// restoreLedger (F2-5), rewardedSsv (F2-6) y voidedPurchasesJob (F2-7), con DynamoDB en memoria y Play/MAX falsos.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sha256hex } from '../../functions/shared/crypto.ts';
import { UpstreamError } from '../../functions/shared/errors.ts';
import type { PlayApi, VoidedPage } from '../../functions/shared/playApi.ts';
import { createHandler as restoreHandler } from '../../functions/restore-ledger/handler.ts';
import { createHandler as ssvHandler, SSV_DAILY_LIMIT } from '../../functions/rewarded-ssv/handler.ts';
import { expectedEventToken, verifyMaxCallback } from '../../functions/rewarded-ssv/maxSignature.ts';
import { JOB_ID, MAX_LOOKBACK_MS, OVERLAP_MS, createHandler as voidedHandler } from '../../functions/voided-purchases-job/handler.ts';
import { validator } from '../contract/ajv.ts';
import { MapSecrets, NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const N1 = '6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f';
const N2 = '0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d';
const N3 = '1b2c3d4e-5f60-4a1b-9c2d-3e4f5a6b7c8d';

let db: MemDb;
beforeEach(() => {
  db = new MemDb();
  vi.spyOn(console, 'log').mockImplementation(() => undefined);
  vi.spyOn(console, 'warn').mockImplementation(() => undefined);
  vi.spyOn(console, 'error').mockImplementation(() => undefined);
});
afterEach(() => {
  db.restore();
  vi.restoreAllMocks();
});

describe('restoreLedger', () => {
  const v = validator('restore-ledger.response.schema.json');
  const call = async (body: unknown, uid: string = UID) =>
    res(await restoreHandler(() => ({ ddb: db.client, env: TABLES }))(httpEvent({ routeKey: 'POST /v1/ledger/restore', body, uid })));

  it('devuelve las compras del UID con su estado y solo los nonces confirmados para él', async () => {
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'a'.repeat(64), uid: UID, sku: 'af_noads', state: 'ok', ts: 2 });
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'b'.repeat(64), uid: UID, sku: 'af_coins_2', state: 'voided', ts: 1, voided_at: 5 });
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'c'.repeat(64), uid: OTHER_UID, sku: 'af_coins_3', state: 'ok', ts: 3 });
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'd'.repeat(64), sku: 'af_coins_3', state: 'ok', ts: 3 }); // anonimizada
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N1, uid: UID });
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N2, uid: OTHER_UID });
    const r = await call({ pending_nonces: [N1, N2, N3] });
    expect(r.status).toBe(200);
    expect(v(r.body), JSON.stringify(v.errors)).toBe(true);
    expect(r.body).toEqual({
      purchases: [
        { token_hash: 'b'.repeat(64), sku: 'af_coins_2', state: 'voided', ts: 1, voided_at: 5 },
        { token_hash: 'a'.repeat(64), sku: 'af_noads', state: 'ok', ts: 2 },
      ],
      confirmed_nonces: [N1],
    });
  });

  it('vacío y sin nonces', async () => {
    expect((await call({ pending_nonces: [] })).body).toEqual({ purchases: [], confirmed_nonces: [] });
  });

  it('reintenta las claves sin procesar de BatchGetItem', async () => {
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N1, uid: UID });
    db.unprocessedOnce.add(N1);
    expect((await call({ pending_nonces: [N1] })).body.confirmed_nonces).toEqual([N1]);
  });

  it('más de 100 nonces van en varios lotes', async () => {
    const many = Array.from({ length: 150 }, (_, i) => `6f1c2a4e-8b3d-4f5a-9c7e-${i.toString(16).padStart(12, '0')}`);
    for (const n of many) db.put(TABLES.AF_TABLE_REWARDS, { nonce: n, uid: UID });
    expect(((await call({ pending_nonces: many })).body.confirmed_nonces as string[])).toHaveLength(150);
  });

  it('400 con nonces mal formados, repetidos o demasiados; 401 sin UID; error de DynamoDB → 500', async () => {
    expect((await call({ pending_nonces: ['not-a-uuid'] })).status).toBe(400);
    expect((await call({ pending_nonces: [N1, N1] })).status).toBe(400);
    expect((await call({ pending_nonces: Array(201).fill(N1) })).status).toBe(400);
    expect((await call({})).status).toBe(400);
    expect(res(await restoreHandler(() => ({ ddb: db.client, env: TABLES }))(httpEvent({ routeKey: 'POST /v1/ledger/restore', body: { pending_nonces: [] } }))).status).toBe(401);
    db.failNext = { command: 'Query', error: new Error('boom') };
    expect((await call({ pending_nonces: [] })).status).toBe(500);
  });
});

describe('rewardedSsv', () => {
  const KEY = 'clave-s2s-de-prueba';
  const v = validator('rewarded-ssv.response.schema.json');
  const secrets = new MapSecrets();
  secrets.values.set('arn:max', `${KEY}\n`);
  const env = { ...TABLES, AF_SECRET_MAX_SSV: 'arn:max' };
  let now = NOW;
  const h = () => ssvHandler(() => ({ ddb: db.client, secrets, env, now: () => now }));
  const q = (over: Record<string, string> = {}, eventId = 'ev-1') => ({
    user_id: UID,
    custom_data: `rescue|${N1}`,
    network: 'AppLovin',
    event_id: eventId,
    event_token: expectedEventToken(eventId, KEY),
    ...over,
  });
  const call = async (query: Record<string, string>, method: 'GET' | 'POST' = 'GET') =>
    res(await h()(httpEvent({ routeKey: `${method} /v1/ssv`, query })));

  it('firma buena: anota rewards/{nonce} con TTL de 30 días y responde ok', async () => {
    const r = await call(q());
    expect(r).toMatchObject({ status: 200, body: { status: 'ok' } });
    expect(v(r.body)).toBe(true);
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })).toMatchObject({
      uid: UID,
      placement: 'rescue',
      network: 'AppLovin',
      ts: NOW,
      ttl: Math.floor(NOW / 1000) + 30 * 86400,
    });
  });

  it('nonce repetido → 200 duplicate sin reescribir (idempotente), también por POST', async () => {
    await call(q());
    const before = structuredClone(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 }));
    now = NOW + 5000;
    const r = await call(q({}, 'ev-2'), 'POST');
    expect(r.body).toEqual({ status: 'duplicate' });
    expect(v(r.body)).toBe(true);
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })).toEqual(before);
    // Ni siquiera otro UID puede quedarse con un nonce ya anotado.
    expect((await call(q({ user_id: OTHER_UID }, 'ev-3'))).body).toEqual({ status: 'duplicate' });
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })?.uid).toBe(UID);
    now = NOW;
  });

  it('firma mala, ausente o con otra clave → 403 bad_signature sin escribir ni gastar cupo', async () => {
    for (const query of [
      q({ event_token: 'f'.repeat(64) }),
      q({ event_token: expectedEventToken('ev-1', 'otra-clave') }),
      { user_id: UID, custom_data: `rescue|${N1}` },
      q({ event_id: 'ev 1' }),
    ]) {
      const r = await call(query);
      expect(r.status).toBe(403);
      expect(r.body).toMatchObject({ error: { code: 'bad_signature' } });
    }
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toHaveLength(0);
    expect(db.items(TABLES.AF_TABLE_RATE_LIMITS)).toHaveLength(0);
  });

  it('el secreto de relleno no firma nada que no se haya firmado con él', () => {
    expect(verifyMaxCallback('ev', expectedEventToken('ev', KEY), '')).toBe(false);
    expect(verifyMaxCallback('ev', expectedEventToken('ev', KEY).toUpperCase(), KEY)).toBe(true);
    expect(verifyMaxCallback(undefined, 'x', KEY)).toBe(false);
  });

  it('parámetros mal formados → 400 (custom_data, user_id)', async () => {
    expect((await call(q({ custom_data: 'rescue' }))).status).toBe(400);
    expect((await call(q({ custom_data: `rescue|nonce` }))).status).toBe(400);
    expect((await call(q({ user_id: 'no válido!' }))).status).toBe(400);
    expect((await call({})).status).toBe(400);
  });

  it(`límite de ${SSV_DAILY_LIMIT} recompensas por UID y día → 429 con retry_after_s`, async () => {
    for (let i = 0; i < SSV_DAILY_LIMIT; i++) {
      const nonce = `6f1c2a4e-8b3d-4f5a-9c7e-${i.toString(16).padStart(12, '0')}`;
      expect((await call(q({ custom_data: `rescue|${nonce}` }, `e${i}`))).status).toBe(200);
    }
    const r = await call(q({ custom_data: `rescue|${N2}` }, 'e-last'));
    expect(r.status).toBe(429);
    expect(r.body).toMatchObject({ error: { code: 'rate_limited' } });
    expect(validator('error.schema.json')(r.body)).toBe(true);
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N2 })).toBeUndefined();
  });

  it('error de DynamoDB → 500 (MAX reintenta)', async () => {
    db.failNext = { command: 'TransactWrite', error: new Error('boom') };
    expect((await call(q())).status).toBe(500);
  });
});

class VoidedPlay implements PlayApi {
  pages: VoidedPage[] = [];
  calls: Array<{ start: number; end: number; token?: string }> = [];
  fail = false;
  getProduct = vi.fn();
  acknowledge = vi.fn();
  listVoided(start: number, end: number, token?: string): Promise<VoidedPage> {
    this.calls.push({ start, end, token });
    if (this.fail) return Promise.reject(new UpstreamError('play', 'voidedpurchases.list', 503));
    return Promise.resolve(this.pages[this.calls.length - 1] ?? { items: [] });
  }
}

describe('voidedPurchasesJob', () => {
  const out = validator('voided-purchases-job.output.schema.json');
  let play: VoidedPlay;
  beforeEach(() => {
    play = new VoidedPlay();
  });
  const run = () => voidedHandler(() => ({ ddb: db.client, play, env: TABLES, now: () => NOW }))();
  const ledger = (token: string, extra: Record<string, unknown> = {}) =>
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex(token), uid: UID, sku: 'af_coins_2', state: 'ok', ts: 1, ...extra });

  it('pagina, marca voided lo que está en el libro mayor y guarda el checkpoint', async () => {
    ledger('t1');
    ledger('t2');
    play.pages = [
      { items: [{ purchaseToken: 't1', voidedTimeMs: 10, voidedReason: 1, voidedSource: 0 }, { purchaseToken: 'ajeno', voidedTimeMs: 11 }], nextPageToken: 'p2' },
      { items: [{ purchaseToken: 't2', voidedTimeMs: 12 }] },
    ];
    const r = await run();
    expect(out(r), JSON.stringify(out.errors)).toBe(true);
    expect(r).toEqual({ status: 'ok', pages: 2, voided: 2, tombstones: 1, cursor_ms: NOW });
    expect(play.calls[1].token).toBe('p2');
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('t1') })).toMatchObject({ state: 'voided', voided_at: 10, voided_reason: 1, voided_source: 0, uid: UID });
    // F2c: el token que aún no estaba en el libro mayor queda con una lápida voided sin uid.
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('ajeno') })).toEqual({
      token_hash: sha256hex('ajeno'), state: 'voided', tombstone: true, voided_at: 11, voided_reason: -1, voided_source: -1, created_at: NOW,
    });
    expect(db.get(TABLES.AF_TABLE_JOBS, { job: JOB_ID })).toMatchObject({ cursor_ms: NOW });
  });

  it('primera vez: mira 30 días atrás; después, desde el checkpoint con solapamiento', async () => {
    await run();
    expect(play.calls[0]).toMatchObject({ start: NOW - MAX_LOOKBACK_MS, end: NOW });
    db.put(TABLES.AF_TABLE_JOBS, { job: JOB_ID, cursor_ms: NOW - 3600_000 });
    play.calls = [];
    await run();
    expect(play.calls[0].start).toBe(NOW - 3600_000 - OVERLAP_MS);
  });

  it('idempotente: repetir no vuelve a contar ni cambia la fila', async () => {
    ledger('t1');
    play.pages = [{ items: [{ purchaseToken: 't1', voidedTimeMs: 10 }] }];
    expect((await run()).voided).toBe(1);
    const row = structuredClone(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('t1') }));
    play.calls = [];
    play.pages = [{ items: [{ purchaseToken: 't1', voidedTimeMs: 99 }] }];
    expect((await run()).voided).toBe(0);
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('t1') })).toEqual(row);
  });

  it('una compra ya anonimizada (cuenta borrada) también se marca voided', async () => {
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('t9'), sku: 'af_coins_2', state: 'ok', ts: 1 });
    play.pages = [{ items: [{ purchaseToken: 't9', voidedTimeMs: 10 }] }];
    expect((await run()).voided).toBe(1);
  });

  it('si Play falla, lanza (reintento del Scheduler y DLQ) y no avanza el checkpoint', async () => {
    db.put(TABLES.AF_TABLE_JOBS, { job: JOB_ID, cursor_ms: NOW - 1000 });
    play.fail = true;
    await expect(run()).rejects.toBeInstanceOf(UpstreamError);
    expect(db.get(TABLES.AF_TABLE_JOBS, { job: JOB_ID })).toMatchObject({ cursor_ms: NOW - 1000 });
  });

  it('si DynamoDB falla a mitad, lanza y no avanza el checkpoint', async () => {
    ledger('t1');
    play.pages = [{ items: [{ purchaseToken: 't1', voidedTimeMs: 10 }] }];
    db.failNext = { command: 'Update', error: new Error('boom') };
    await expect(run()).rejects.toThrow('boom');
    expect(db.get(TABLES.AF_TABLE_JOBS, { job: JOB_ID })).toBeUndefined();
  });

  it('demasiadas páginas → lanza sin avanzar el checkpoint', async () => {
    play.pages = Array.from({ length: 60 }, (_, i) => ({ items: [], nextPageToken: `p${i + 1}` }));
    await expect(run()).rejects.toThrow(/páginas/);
    expect(db.get(TABLES.AF_TABLE_JOBS, { job: JOB_ID })).toBeUndefined();
  });
});
