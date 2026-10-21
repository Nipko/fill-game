// Endurecimiento F2c (revisión de seguridad; D-035, «preguntas de las 5 funciones»): App Check solo para las apps
// registradas en prod, lápida `voided` de los tokens anulados antes de verificarse, `af_starter` una vez por
// cuenta y carreras residuales del borrado. Todo con DynamoDB en memoria, sin red.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { onceKey } from '../../functions/shared/catalog.ts';
import { sha256hex } from '../../functions/shared/crypto.ts';
import { ConfigError } from '../../functions/shared/errors.ts';
import type { FirebaseAdmin } from '../../functions/shared/firebaseAdmin.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import { uidHash } from '../../functions/shared/log.ts';
import type { PlayApi, ProductPurchase, VoidedPage } from '../../functions/shared/playApi.ts';
import { DELETED_MARK_TTL_S, deletedKey } from '../../functions/shared/rateLimit.ts';
import { realIntegrityVerifier } from '../../functions/shared/runtime.ts';
import { createHandler as deleteHandler } from '../../functions/delete-account/handler.ts';
import { createHandler as restoreHandler } from '../../functions/restore-ledger/handler.ts';
import { createHandler as ssvHandler } from '../../functions/rewarded-ssv/handler.ts';
import { expectedEventToken } from '../../functions/rewarded-ssv/maxSignature.ts';
import { createHandler as verifyHandler } from '../../functions/verify-purchase/handler.ts';
import { createHandler as voidedHandler } from '../../functions/voided-purchases-job/handler.ts';
import { validator } from '../contract/ajv.ts';
import { MapSecrets, NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES, transactionCanceled } from './memdb.ts';

const errResponse = validator('error.schema.json');
const okResponse = validator('verify-purchase.response.schema.json');
const jobOutput = validator('voided-purchases-job.output.schema.json');
const okIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: true }) };
const N1 = '6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f';
const N2 = '0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d';

class Play implements PlayApi {
  purchase: ProductPurchase = { state: 'purchased', acknowledged: false, purchaseTimeMs: NOW - 1000, quantity: 1, obfuscatedAccountId: undefined };
  acks: string[] = [];
  gets = 0;
  pages: VoidedPage[] = [{ items: [] }];
  getProduct = (): Promise<ProductPurchase> => {
    this.gets++;
    return Promise.resolve(this.purchase);
  };
  acknowledge = (_sku: string, token: string): Promise<void> => {
    this.acks.push(token);
    return Promise.resolve();
  };
  listVoided = (_s: number, _e: number, pageToken?: string): Promise<VoidedPage> =>
    Promise.resolve(this.pages[pageToken ? Number(pageToken) : 0]);
}

let db: MemDb;
let play: Play;
let n = 0;
beforeEach(() => {
  db = new MemDb();
  play = new Play();
  vi.spyOn(console, 'log').mockImplementation(() => undefined);
  vi.spyOn(console, 'warn').mockImplementation(() => undefined);
  vi.spyOn(console, 'error').mockImplementation(() => undefined);
});
afterEach(() => {
  db.restore();
  vi.restoreAllMocks();
});

const env = { ...TABLES, AF_ENV: 'dev' };
const verify = async (token: string, sku = 'af_coins_2', uid = UID) =>
  res(
    await verifyHandler(() => ({ ddb: db.client, play, integrity: okIntegrity, env, now: () => NOW }))(
      httpEvent({ routeKey: 'POST /v1/purchases/verify', body: { token, sku }, uid, headers: { 'x-firebase-appcheck': `t${++n}` } }),
    ),
  );
const runJob = () => voidedHandler(() => ({ ddb: db.client, play, env: TABLES, now: () => NOW }))();
const restore = async (uid = UID) =>
  res(await restoreHandler(() => ({ ddb: db.client, env: TABLES }))(httpEvent({ routeKey: 'POST /v1/ledger/restore', body: { pending_nonces: [] }, uid })));
const deleteApp = async (uid = UID) =>
  res(
    await deleteHandler(() => ({
      ddb: db.client,
      firebase: { deleteUser: () => Promise.resolve({ existed: true }) } satisfies FirebaseAdmin,
      integrity: okIntegrity,
      env,
      now: () => NOW,
      random: (k: number) => Buffer.alloc(k, ++n % 250),
    }))(httpEvent({ routeKey: 'POST /v1/account/delete', body: { confirm: true }, uid, headers: { 'x-firebase-appcheck': `d${++n}` } })),
  );
const ledgerRows = () => db.items(TABLES.AF_TABLE_PURCHASES);
/** Lo que escribe markDeleted al empezar un borrado (síncrono, para usarlo dentro de un hookNext). */
const markDeletedNow = () =>
  db.put(TABLES.AF_TABLE_RATE_LIMITS, { key: deletedKey(uidHash(UID)), ttl: Math.floor(NOW / 1000) + DELETED_MARK_TTL_S });

describe('F2c-1 · App Check: en prod solo las apps registradas', () => {
  it('la Lambda falla cerrada en prod sin AF_APPCHECK_APP_IDS; con ella o en dev, se monta', () => {
    expect(() => realIntegrityVerifier({ AF_ENV: 'prod', AF_FIREBASE_PROJECT_ID: 'amazon-fill-prod' })).toThrow(ConfigError);
    // Sin AF_ENV cuenta como prod (falla cerrada).
    expect(() => realIntegrityVerifier({ AF_FIREBASE_PROJECT_ID: 'amazon-fill-prod' })).toThrow(/AF_APPCHECK_APP_IDS/);
    expect(realIntegrityVerifier({ AF_ENV: 'prod', AF_FIREBASE_PROJECT_ID: 'amazon-fill-prod', AF_APPCHECK_APP_IDS: '1:1:android:abcdef' }).variant).toBe('app-check');
    expect(realIntegrityVerifier({ AF_ENV: 'dev', AF_FIREBASE_PROJECT_ID: 'amazon-fill-dev' }).variant).toBe('app-check');
  });
});

describe('F2c-2 · lápida voided para tokens anulados antes de verificarse', () => {
  it('el job deja la lápida sin uid y verifyPurchase responde 400 sin llamar a Play ni conceder', async () => {
    play.pages = [{ items: [{ purchaseToken: 'tk-refund', voidedTimeMs: 5, voidedReason: 1, voidedSource: 0 }] }];
    const r = await runJob();
    expect(jobOutput(r), JSON.stringify(jobOutput.errors)).toBe(true);
    expect(r).toMatchObject({ voided: 0, tombstones: 1 });
    const row = db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-refund') });
    expect(row).toMatchObject({ state: 'voided', tombstone: true, voided_at: 5 });
    expect(row).not.toHaveProperty('uid');

    const v = await verify('tk-refund');
    expect(v.status).toBe(400);
    expect(v.body).toMatchObject({ error: { code: 'bad_request' } });
    expect(errResponse(v.body)).toBe(true);
    expect(play.gets).toBe(0);
    expect(play.acks).toEqual([]);
    expect(ledgerRows()).toHaveLength(1);
    // La lápida no es de nadie: no sale en restoreLedger.
    expect((await restore()).body.purchases).toEqual([]);
  });

  it('idempotente: repetir el job no crea otra lápida ni cambia la que hay', async () => {
    play.pages = [{ items: [{ purchaseToken: 'tk-refund', voidedTimeMs: 5 }] }];
    await runJob();
    const before = structuredClone(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-refund') }));
    play.pages = [{ items: [{ purchaseToken: 'tk-refund', voidedTimeMs: 99 }] }];
    expect(await runJob()).toMatchObject({ voided: 0, tombstones: 0 });
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-refund') })).toEqual(before);
  });

  it('carrera: verifyPurchase escribe la fila entre el Update fallido y la lápida → el job la marca voided (con su uid)', async () => {
    play.pages = [{ items: [{ purchaseToken: 'tk-race', voidedTimeMs: 7 }] }];
    db.hookNext = {
      command: 'Put',
      table: TABLES.AF_TABLE_PURCHASES,
      fn: () => db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-race'), uid: UID, sku: 'af_coins_2', state: 'ok', ts: 1 }),
    };
    expect(await runJob()).toMatchObject({ voided: 1, tombstones: 0 });
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-race') })).toMatchObject({ uid: UID, state: 'voided', voided_at: 7 });
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-race') })).not.toHaveProperty('tombstone');
  });

  it('carrera: la lápida llega mientras verifyPurchase consulta Play → 400, nunca ok', async () => {
    db.hookNext = {
      command: 'TransactWrite',
      fn: () => db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-late'), state: 'voided', tombstone: true, voided_at: 9 }),
    };
    const v = await verify('tk-late');
    expect(v.status).toBe(400);
    expect(ledgerRows()).toEqual([{ token_hash: sha256hex('tk-late'), state: 'voided', tombstone: true, voided_at: 9 }]);
  });

  it('una fila anulada de este mismo UID sigue respondiendo duplicate (el cliente la retira con restoreLedger)', async () => {
    expect((await verify('tk-own')).body.status).toBe('ok');
    play.pages = [{ items: [{ purchaseToken: 'tk-own', voidedTimeMs: 8 }] }];
    expect(await runJob()).toMatchObject({ voided: 1, tombstones: 0 });
    expect((await verify('tk-own')).body).toEqual({ status: 'duplicate', token_hash: sha256hex('tk-own') });
  });
});

describe('F2c-3 · af_starter una vez por cuenta (05 §4.1: «1 por cuenta»)', () => {
  it('la segunda compra con otro token → 400 sin reconocer ni anotar; el mismo token → duplicate', async () => {
    const first = await verify('st-1', 'af_starter');
    expect(first.body.status).toBe('ok');
    expect(okResponse(first.body)).toBe(true);
    const mark = db.get(TABLES.AF_TABLE_RATE_LIMITS, { key: onceKey('af_starter', UID) });
    expect(mark).toMatchObject({ token_hash: sha256hex('st-1') });
    expect(mark).not.toHaveProperty('ttl');
    expect(JSON.stringify(db.items(TABLES.AF_TABLE_RATE_LIMITS))).not.toContain(UID);

    const second = await verify('st-2', 'af_starter');
    expect(second.status).toBe(400);
    expect(second.body).toMatchObject({ error: { code: 'bad_request' } });
    expect(errResponse(second.body)).toBe(true);
    expect(play.acks).toEqual(['st-1']); // sin reconocer: Play reembolsa la segunda a los 3 días
    expect(ledgerRows()).toHaveLength(1);

    expect((await verify('st-1', 'af_starter')).body.status).toBe('duplicate');
  });

  it('otra cuenta sí puede comprarla; los consumibles normales no tienen tope', async () => {
    expect((await verify('st-1', 'af_starter')).body.status).toBe('ok');
    expect((await verify('st-o', 'af_starter', OTHER_UID)).body.status).toBe('ok');
    expect((await verify('c-1')).body.status).toBe('ok');
    expect((await verify('c-2')).body.status).toBe('ok');
  });

  it('carrera: dos tokens de af_starter a la vez para el mismo UID → uno ok y otro 400, una sola fila', async () => {
    db.hookNext = {
      command: 'TransactWrite',
      fn: () => {
        db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('st-a'), uid: UID, sku: 'af_starter', state: 'ok', ts: 1 });
        db.put(TABLES.AF_TABLE_RATE_LIMITS, { key: onceKey('af_starter', UID), token_hash: sha256hex('st-a') });
      },
    };
    const r = await verify('st-b', 'af_starter');
    expect(r.status).toBe(400);
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('st-b') })).toBeUndefined();
  });

  it('el borrado de la cuenta quita la marca (no quedan datos del UID)', async () => {
    await verify('st-1', 'af_starter');
    expect((await deleteApp()).status).toBe(200);
    expect(db.get(TABLES.AF_TABLE_RATE_LIMITS, { key: onceKey('af_starter', UID) })).toBeUndefined();
  });
});

describe('F2c-4 · carreras del borrado', () => {
  it('verifyPurchase: la marca de borrado llega entre isDeleted y la escritura → 401 y ninguna fila con el uid', async () => {
    db.hookNext = { command: 'TransactWrite', fn: markDeletedNow };
    const r = await verify('tk-del', 'af_starter');
    expect(r.status).toBe(401);
    expect(errResponse(r.body)).toBe(true);
    expect(ledgerRows()).toEqual([]);
    expect(db.get(TABLES.AF_TABLE_RATE_LIMITS, { key: onceKey('af_starter', UID) })).toBeUndefined();
  });

  it('verifyPurchase: con la marca ya caducada (más de 2 h) vuelve a escribir', async () => {
    db.put(TABLES.AF_TABLE_RATE_LIMITS, { key: deletedKey(uidHash(UID)), ttl: Math.floor(NOW / 1000) - 1 });
    expect((await verify('tk-old')).body.status).toBe('ok');
  });

  it('verifyPurchase: TransactionConflict con el ganador ya escrito → duplicate; sin ganador → 500', async () => {
    db.hookNext = {
      command: 'TransactWrite',
      fn: () => db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-c'), uid: UID, sku: 'af_coins_2', state: 'ok', ts: 1 }),
    };
    db.failNext = { command: 'TransactWrite', error: transactionCanceled(['TransactionConflict', 'None']) };
    expect((await verify('tk-c')).body.status).toBe('duplicate');
    db.failNext = { command: 'TransactWrite', error: transactionCanceled(['TransactionConflict', 'None']) };
    const r = await verify('tk-d');
    expect(r.status).toBe(500);
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tk-d') })).toBeUndefined();
  });

  it('rewardedSsv: la marca de borrado llega entre isDeleted y la escritura → 200 sin fila en rewards', async () => {
    const secrets = new MapSecrets();
    secrets.values.set('arn:max', 'clave');
    db.hookNext = { command: 'TransactWrite', fn: markDeletedNow };
    const r = res(
      await ssvHandler(() => ({ ddb: db.client, secrets, env: { ...TABLES, AF_SECRET_MAX_SSV: 'arn:max' }, now: () => NOW }))(
        httpEvent({
          routeKey: 'GET /v1/ssv',
          query: { user_id: UID, custom_data: `rescue|${N1}`, event_id: 'ev', event_token: expectedEventToken('ev', 'clave') },
        }),
      ),
    );
    expect(r.status).toBe(200);
    expect(r.body).toEqual({ status: 'duplicate' });
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toEqual([]);
  });

  it('código de borrado: la marca llega entre isDeleted y la escritura → 401 y ningún código', async () => {
    db.hookNext = { command: 'TransactWrite', fn: markDeletedNow };
    const r = res(
      await deleteHandler(() => ({
        ddb: db.client,
        firebase: { deleteUser: () => Promise.resolve({ existed: true }) },
        integrity: okIntegrity,
        env,
        now: () => NOW,
        random: (k: number) => Buffer.alloc(k, 7),
      }))(httpEvent({ routeKey: 'POST /v1/account/code', uid: UID })),
    );
    expect(r.status).toBe(401);
    expect(db.items(TABLES.AF_TABLE_ACCOUNT_CODES)).toEqual([]);
  });

  it('segunda pasada de eraseAccount: lo escrito antes de la marca que el GSI aún no enseñaba también se limpia', async () => {
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N1, uid: UID, placement: 'rescue', ts: 1, ttl: 9e9 });
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N2, uid: UID, placement: 'rescue', ts: 2, ttl: 9e9 });
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'a'.repeat(64), uid: UID, sku: 'af_coins_2', state: 'ok', ts: 1 });
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'b'.repeat(64), uid: UID, sku: 'af_coins_2', state: 'ok', ts: 2 });
    // La primera consulta del GSI no ve N2 ni la fila «b» (escritas justo antes de la marca).
    db.hiddenFromIndexOnce.add(N2);
    db.hiddenFromIndexOnce.add('b'.repeat(64));
    const warn = vi.spyOn(console, 'warn');
    expect((await deleteApp()).status).toBe(200);
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toEqual([]);
    for (const row of ledgerRows()) {
      expect(row).not.toHaveProperty('uid');
      expect(row).toHaveProperty('anonymized_at', NOW);
    }
    expect(warn.mock.calls.map((c) => String(c[0])).join('\n')).toMatch(/erase_second_pass/);
  });

  it('sin rezagados, la segunda pasada no avisa (las filas ya borradas que el GSI aún enseñe no cuentan)', async () => {
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N1, uid: UID, placement: 'rescue', ts: 1, ttl: 9e9 });
    const warn = vi.spyOn(console, 'warn');
    expect((await deleteApp()).status).toBe(200);
    expect(warn.mock.calls.map((c) => String(c[0])).join('\n')).not.toMatch(/erase_second_pass/);
  });
});
