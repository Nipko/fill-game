// verifyPurchase (F2-4): todas las ramas con DynamoDB en memoria y Play e integridad falsos.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sha256hex } from '../../functions/shared/crypto.ts';
import { UpstreamError } from '../../functions/shared/errors.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import type { PlayApi, ProductPurchase } from '../../functions/shared/playApi.ts';
import { createHandler } from '../../functions/verify-purchase/handler.ts';
import { validator } from '../contract/ajv.ts';
import { NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const ROUTE = 'POST /v1/purchases/verify';
const TOKEN = 'tok.AO-J1Oz_test-123';
const TH = sha256hex(TOKEN);
const okResponse = validator('verify-purchase.response.schema.json');
const errResponse = validator('error.schema.json');

class TestPlay implements PlayApi {
  purchase: ProductPurchase | null = { state: 'purchased', acknowledged: false, purchaseTimeMs: NOW - 1000, orderId: 'GPA.1', regionCode: 'CO', quantity: 1 };
  acks: string[] = [];
  gets = 0;
  failAck = false;
  getProduct = vi.fn((): Promise<ProductPurchase | null> => {
    this.gets++;
    return Promise.resolve(this.purchase);
  });
  acknowledge = vi.fn((_sku: string, token: string): Promise<void> => {
    if (this.failAck) return Promise.reject(new UpstreamError('play', 'products.acknowledge', 503));
    this.acks.push(token);
    return Promise.resolve();
  });
  listVoided = vi.fn();
}

const okIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: true, exp: NOW / 1000 + 3600 }) };
const badIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: false, reason: 'sig' }) };

describe('verifyPurchase', () => {
  let db: MemDb;
  let play: TestPlay;
  let integrity: IntegrityVerifier;
  let n = 0;

  beforeEach(() => {
    db = new MemDb();
    play = new TestPlay();
    integrity = okIntegrity;
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => {
    db.restore();
    vi.restoreAllMocks();
  });

  const handler = () =>
    createHandler(() => ({ ddb: db.client, play, integrity, env: { ...TABLES, AF_ENV: 'dev' }, now: () => NOW }));
  const call = async (body: unknown = { token: TOKEN, sku: 'af_coins_2' }, uid: string | undefined = UID, appCheck = `t${++n}`) =>
    res(await handler()(httpEvent({ routeKey: ROUTE, body, uid, headers: { 'x-firebase-appcheck': appCheck } })));

  it('ok: reconoce, anota en el libro mayor y concede según el catálogo (respuesta del contrato)', async () => {
    const r = await call();
    expect(r.status).toBe(200);
    expect(okResponse(r.body), JSON.stringify(okResponse.errors)).toBe(true);
    expect(r.body).toEqual({ status: 'ok', token_hash: TH, grant: { seeds: 1000, items: [] } });
    expect(play.acks).toEqual([TOKEN]);
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: TH })).toMatchObject({ uid: UID, sku: 'af_coins_2', state: 'ok', ts: NOW - 1000, country: 'CO' });
    // El libro mayor nunca guarda el purchaseToken en claro.
    expect(JSON.stringify(db.items(TABLES.AF_TABLE_PURCHASES))).not.toContain(TOKEN);
  });

  it('derechos: Sin anuncios y skins; la Balsa trae sus ítems', async () => {
    expect((await call({ token: 'a1', sku: 'af_noads' })).body).toMatchObject({ grant: { seeds: 500 }, entitlements: { no_ads: true } });
    expect((await call({ token: 'a2', sku: 'af_skin_m03' })).body).toMatchObject({ entitlements: { skins: ['af_skin_m03'] } });
    const rescue = await call({ token: 'a3', sku: 'af_rescue' });
    expect(okResponse(rescue.body)).toBe(true);
    expect(rescue.body.grant).toEqual({ seeds: 1000, items: [{ id: 'rescue_cuia', count: 1 }, { id: 'extra_cuia', count: 1 }, { id: 'infinite_canoes_min', count: 30 }] });
  });

  it('ya reconocida en Play: no vuelve a reconocer', async () => {
    play.purchase = { ...play.purchase!, acknowledged: true };
    expect((await call()).body.status).toBe('ok');
    expect(play.acks).toEqual([]);
  });

  it('duplicate: el mismo token otra vez no concede y no vuelve a llamar a Play', async () => {
    await call();
    const gets = play.gets;
    const r = await call();
    expect(r.body).toEqual({ status: 'duplicate', token_hash: TH });
    expect(okResponse(r.body)).toBe(true);
    expect(play.gets).toBe(gets);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(1);
  });

  it('el token de otro UID (o ya anonimizado tras un borrado) → 409', async () => {
    await call(undefined, OTHER_UID);
    const r = await call();
    expect(r.status).toBe(409);
    expect(r.body).toMatchObject({ error: { code: 'token_belongs_to_other_user' } });
    expect(errResponse(r.body)).toBe(true);
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('anon'), sku: 'af_coins_2', state: 'ok' });
    expect((await call({ token: 'anon', sku: 'af_coins_2' })).status).toBe(409);
  });

  it('carrera: dos peticiones simultáneas con el mismo token → una ok y otra duplicate (una sola fila)', async () => {
    const h = handler();
    const ev = (i: number) => httpEvent({ routeKey: ROUTE, body: { token: TOKEN, sku: 'af_coins_2' }, uid: UID, headers: { 'x-firebase-appcheck': `race${i}` } });
    const out = (await Promise.all([h(ev(1)), h(ev(2))])).map(res);
    expect(out.map((o) => o.body.status).sort()).toEqual(['duplicate', 'ok']);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(1);
  });

  it('carrera con otro UID: quien pierde recibe 409, nunca ok', async () => {
    const h = handler();
    const ev = (uid: string, i: number) => httpEvent({ routeKey: ROUTE, body: { token: TOKEN, sku: 'af_coins_2' }, uid, headers: { 'x-firebase-appcheck': `r${i}` } });
    const out = (await Promise.all([h(ev(UID, 1)), h(ev(OTHER_UID, 2))])).map(res);
    expect(out.filter((o) => o.body.status === 'ok')).toHaveLength(1);
    expect(out.filter((o) => o.status === 409)).toHaveLength(1);
  });

  it('pending: no concede, no reconoce y no escribe (el cliente reintenta)', async () => {
    play.purchase = { ...play.purchase!, state: 'pending' };
    const r = await call();
    expect(r.body).toEqual({ status: 'pending' });
    expect(okResponse(r.body)).toBe(true);
    expect(play.acks).toEqual([]);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
    play.purchase = { ...play.purchase, state: 'purchased' };
    expect((await call()).body.status).toBe('ok'); // cuando se completa, concede
  });

  it('cancelada o inexistente en Play → 400 sin escribir', async () => {
    play.purchase = { ...play.purchase!, state: 'canceled' };
    expect((await call()).status).toBe(400);
    play.purchase = null;
    expect((await call()).status).toBe(400);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
  });

  it('cuenta ofuscada de Play distinta del UID → 409; igual al UID o a su SHA-256 → ok', async () => {
    play.purchase = { ...play.purchase!, obfuscatedAccountId: OTHER_UID };
    expect((await call()).status).toBe(409);
    play.purchase = { ...play.purchase, obfuscatedAccountId: sha256hex(UID) };
    expect((await call()).body.status).toBe('ok');
    play.purchase = { ...play.purchase, obfuscatedAccountId: UID };
    expect((await call({ token: 'otro', sku: 'af_coins_2' })).body.status).toBe('ok');
  });

  it('acknowledge falla → 502 sin escribir; el reintento concede una sola vez', async () => {
    play.failAck = true;
    const r = await call();
    expect(r.status).toBe(502);
    expect(r.body).toMatchObject({ error: { code: 'upstream_error' } });
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
    play.failAck = false;
    expect((await call()).body.status).toBe('ok');
    expect((await call()).body.status).toBe('duplicate');
  });

  it('error al escribir en DynamoDB tras reconocer → 500; el reintento ve la compra reconocida y concede', async () => {
    db.failNext = { command: 'Put', table: TABLES.AF_TABLE_PURCHASES, error: Object.assign(new Error('x'), { name: 'ProvisionedThroughputExceededException' }) };
    const first = await call();
    expect(first.status).toBe(500);
    expect(first.body).toMatchObject({ error: { code: 'internal_error' } });
    expect(play.acks).toEqual([TOKEN]);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
    play.purchase = { ...play.purchase!, acknowledged: true };
    const r = await call();
    expect(r.body.status).toBe('ok');
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(1);
  });

  it('integridad: falta o es mala → 403 sin tocar Play ni el libro mayor', async () => {
    const h = handler();
    const r = res(await h(httpEvent({ routeKey: ROUTE, body: { token: TOKEN, sku: 'af_coins_2' }, uid: UID })));
    expect(r.status).toBe(403);
    expect(r.body).toMatchObject({ error: { code: 'integrity_failed' } });
    integrity = badIntegrity;
    expect((await call()).status).toBe(403);
    expect(play.gets).toBe(0);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
  });

  it('peticiones mal formadas → 400 (contrato), sin UID → 401', async () => {
    for (const body of [
      { sku: 'af_coins_2' },
      { token: TOKEN, sku: 'noads' },
      { token: TOKEN, sku: 'af_unknown_sku' },
      { token: TOKEN, sku: 'af_coins_2', uid: OTHER_UID },
      { token: 'con espacios', sku: 'af_coins_2' },
      [],
    ]) {
      const r = await call(body);
      expect(r.status, JSON.stringify(body)).toBe(400);
      expect(errResponse(r.body)).toBe(true);
    }
    const noUid = res(await handler()(httpEvent({ routeKey: ROUTE, body: { token: TOKEN, sku: 'af_coins_2' }, headers: { 'x-firebase-appcheck': 'z' } })));
    expect(noUid.status).toBe(401);
    const raw = res(await handler()(httpEvent({ routeKey: ROUTE, rawBody: '{no json', uid: UID })));
    expect(raw.status).toBe(400);
  });

  it('las respuestas no llevan el UID ni el token, y los logs tampoco', async () => {
    const spy = vi.spyOn(console, 'log');
    const r = await call();
    expect(JSON.stringify(r.body)).not.toContain(UID);
    for (const c of spy.mock.calls) {
      expect(String(c[0])).not.toContain(UID);
      expect(String(c[0])).not.toContain(TOKEN);
    }
  });
});
