// verifyPurchase con el App Store (StoreKit 2; mac-ios.md, M3): el JWS de la transacción se verifica sin red contra una
// raíz de prueba inyectada (la real es Apple Root CA - G3). DynamoDB en memoria; Play falso que no debe usarse.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { generateKeyPairSync } from 'node:crypto';
import {
  APPLE_ROOT_CA_G3_SHA256,
  JwsAppStoreVerifier,
  OID_APPLE_INTERMEDIATE,
  OID_APPLE_LEAF,
  appleRootCaG3,
  extensionOids,
} from '../../functions/shared/appStore.ts';
import { sha256hex } from '../../functions/shared/crypto.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import type { PlayApi, ProductPurchase } from '../../functions/shared/playApi.ts';
import { realAppStoreVerifier } from '../../functions/shared/runtime.ts';
import { createHandler } from '../../functions/verify-purchase/handler.ts';
import { validator } from '../contract/ajv.ts';
import { BUNDLE_ID, TX_ID, makeCert, makeChain, signTx, txPayload } from './appStoreCerts.ts';
import type { TestChain } from './appStoreCerts.ts';
import { NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const ROUTE = 'POST /v1/purchases/verify';
const TH = sha256hex(TX_ID);
const okResponse = validator('verify-purchase.response.schema.json');
const errResponse = validator('error.schema.json');
const okIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: true, exp: NOW / 1000 + 3600 }) };

class UnusedPlay implements PlayApi {
  getProduct = vi.fn((): Promise<ProductPurchase | null> => Promise.reject(new Error('Play no debe llamarse con app_store')));
  acknowledge = vi.fn((): Promise<void> => Promise.reject(new Error('Play no debe llamarse con app_store')));
  listVoided = vi.fn();
}

const chain: TestChain = makeChain();
const verifier = (bundleIds: readonly string[] = [BUNDLE_ID], now = NOW) =>
  new JwsAppStoreVerifier({ bundleIds, root: chain.root.x509, now: () => now });

describe('Apple Root CA - G3 embebida', () => {
  it('el PEM carga, es autofirmado y su huella SHA-256 es la publicada por Apple', () => {
    const root = appleRootCaG3();
    expect(root.fingerprint256).toBe(APPLE_ROOT_CA_G3_SHA256);
    expect(root.fingerprint256.replaceAll(':', '')).toBe('63343ABFB89A6A03EBB57E9B3F5FA7BE7C4F5C756F3017B3A8C488C3653E9179');
    expect(root.subject).toContain('CN=Apple Root CA - G3');
    expect(root.verify(root.publicKey)).toBe(true);
    expect(root.ca).toBe(true);
  });

  it('el verificador real (raíz de Apple) rechaza una cadena que acaba en otra raíz', async () => {
    const real = realAppStoreVerifier({ AF_IOS_BUNDLE_IDS: BUNDLE_ID });
    const r = await real.verify(await signTx(txPayload(), chain), { sku: 'af_coins_2', transactionId: TX_ID });
    expect(r).toEqual({ ok: false, reason: 'root' });
  });
});

describe('JwsAppStoreVerifier', () => {
  const expected = { sku: 'af_coins_2', transactionId: TX_ID };

  it('lee los OID de las extensiones del DER', () => {
    expect(extensionOids(chain.leaf.der)).toEqual(['2.5.29.19', OID_APPLE_LEAF]);
    expect(extensionOids(chain.intermediate.der)).toEqual(['2.5.29.19', OID_APPLE_INTERMEDIATE]);
  });

  it('ok: mapea la transacción a ProductPurchase', async () => {
    const r = await verifier().verify(await signTx(txPayload(), chain), expected);
    expect(r.ok).toBe(true);
    if (!r.ok) return;
    expect(r.purchase).toEqual({
      state: 'purchased',
      acknowledged: true,
      orderId: '2000000000000001',
      purchaseTimeMs: NOW - 1000,
      regionCode: 'COL',
      obfuscatedAccountId: undefined,
      quantity: 1,
    });
    const sandbox = await verifier().verify(await signTx(txPayload({ environment: 'Sandbox', quantity: 3 }), chain), expected);
    expect(sandbox.ok && sandbox.purchase).toMatchObject({ purchaseType: 0, quantity: 3 });
  });

  it('rechaza alg distinto de ES256, x5c mal formada o incompleta', async () => {
    const { privateKey } = generateKeyPairSync('ec', { namedCurve: 'P-384' });
    expect(await verifier().verify(await signTx(txPayload(), chain, { alg: 'ES384', key: privateKey }), expected)).toEqual({ ok: false, reason: 'alg' });
    expect(await verifier().verify(await signTx(txPayload(), chain, { certs: [chain.leaf, chain.intermediate] }), expected)).toEqual({ ok: false, reason: 'x5c' });
    expect(await verifier().verify('a.b.c', expected)).toEqual({ ok: false, reason: 'header' });
  });

  it('rechaza un intermedio sin la marca de Apple, una hoja sin la suya y una hoja que es CA', async () => {
    const noMarkInt = makeCert({ subject: 'Test WWDR', issuer: chain.root, ca: true });
    const leaf1 = makeCert({ subject: 'Leaf', issuer: noMarkInt, ca: false, markers: [OID_APPLE_LEAF] });
    let jws = await signTx(txPayload(), chain, { certs: [leaf1, noMarkInt, chain.root], key: leaf1.key });
    expect(await verifier().verify(jws, expected)).toEqual({ ok: false, reason: 'intermediate_oid' });
    const noMarkLeaf = makeCert({ subject: 'Leaf', issuer: chain.intermediate, ca: false });
    jws = await signTx(txPayload(), chain, { certs: [noMarkLeaf, chain.intermediate, chain.root], key: noMarkLeaf.key });
    expect(await verifier().verify(jws, expected)).toEqual({ ok: false, reason: 'leaf_oid' });
    const caLeaf = makeCert({ subject: 'Leaf', issuer: chain.intermediate, ca: true, markers: [OID_APPLE_LEAF] });
    jws = await signTx(txPayload(), chain, { certs: [caLeaf, chain.intermediate, chain.root], key: caLeaf.key });
    expect(await verifier().verify(jws, expected)).toEqual({ ok: false, reason: 'basic_constraints' });
  });

  it('rechaza una hoja que no firmó el intermedio y certificados caducados o aún no vigentes', async () => {
    const stranger = makeCert({ subject: 'Test WWDR', ca: true, markers: [OID_APPLE_INTERMEDIATE] });
    const forged = makeCert({ subject: 'Leaf', issuer: stranger, ca: false, markers: [OID_APPLE_LEAF] });
    let jws = await signTx(txPayload(), chain, { certs: [forged, chain.intermediate, chain.root], key: forged.key });
    expect(await verifier().verify(jws, expected)).toEqual({ ok: false, reason: 'leaf_sig' });
    jws = await signTx(txPayload(), chain, { certs: [forged, stranger, chain.root], key: forged.key });
    expect(await verifier().verify(jws, expected)).toEqual({ ok: false, reason: 'intermediate_sig' });
    jws = await signTx(txPayload(), chain);
    expect(await verifier([BUNDLE_ID], NOW + 2 * 365 * 86_400_000).verify(jws, expected)).toEqual({ ok: false, reason: 'validity' });
    expect(await verifier([BUNDLE_ID], NOW - 2 * 365 * 86_400_000).verify(jws, expected)).toEqual({ ok: false, reason: 'validity' });
  });

  it('sin bundleId admitidos no acepta nada (falla cerrada)', async () => {
    expect(await verifier([]).verify(await signTx(txPayload(), chain), expected)).toEqual({ ok: false, reason: 'bundle' });
    expect(await realAppStoreVerifier({}).verify(await signTx(txPayload(), chain), expected)).toMatchObject({ ok: false });
  });
});

describe('verifyPurchase con store = app_store', () => {
  let db: MemDb;
  let play: UnusedPlay;
  let n = 0;

  beforeEach(() => {
    db = new MemDb();
    play = new UnusedPlay();
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => {
    db.restore();
    vi.restoreAllMocks();
  });

  const handler = (envName = 'dev') =>
    createHandler(() => ({
      ddb: db.client,
      play,
      appStore: verifier(),
      integrity: okIntegrity,
      env: { ...TABLES, AF_ENV: envName },
      now: () => NOW,
    }));
  const body = async (over: Record<string, unknown> = {}, sku = 'af_coins_2', token = TX_ID) => ({
    token,
    sku,
    store: 'app_store',
    signed_transaction: await signTx(txPayload(over), chain),
  });
  const call = async (b: unknown, uid = UID, envName = 'dev') =>
    res(await handler(envName)(httpEvent({ routeKey: ROUTE, body: b, uid, headers: { 'x-firebase-appcheck': `t${++n}` } })));
  /** 400 genérico sin escribir; con `reason`, el motivo que queda en el log (nunca en la respuesta). */
  const expectRejected = async (b: unknown, reason?: string): Promise<void> => {
    const warn = vi.mocked(console.warn);
    warn.mockClear();
    const r = await call(b);
    expect(r.status).toBe(400);
    expect(r.body).toMatchObject({ error: { code: 'bad_request' } });
    expect(errResponse(r.body)).toBe(true);
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(0);
    if (reason) {
      const line = warn.mock.calls.map((c) => String(c[0])).find((l) => l.includes('purchase_app_store_rejected'));
      expect(line, reason).toContain(`"reason":"${reason}"`);
      expect(JSON.stringify(r.body)).not.toContain(reason);
    }
  };

  it('ok: concede según el catálogo, anota en el libro mayor y no llama a Play', async () => {
    const r = await call(await body());
    expect(r.status).toBe(200);
    expect(okResponse(r.body), JSON.stringify(okResponse.errors)).toBe(true);
    expect(r.body).toEqual({ status: 'ok', token_hash: TH, grant: { seeds: 1000, items: [] } });
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: TH })).toMatchObject({
      uid: UID,
      sku: 'af_coins_2',
      state: 'ok',
      store: 'app_store',
      order_id: '2000000000000001',
      ts: NOW - 1000,
      country: 'COL',
      quantity: 1,
    });
    expect(play.getProduct).not.toHaveBeenCalled();
    expect(play.acknowledge).not.toHaveBeenCalled();
  });

  it('Sin anuncios: concede el derecho', async () => {
    const r = await call(await body({ productId: 'af_noads' }, 'af_noads'));
    expect(r.body).toMatchObject({ status: 'ok', entitlements: { no_ads: true } });
  });

  it('duplicate con el mismo id de transacción; otro UID → 409 (sin reasignación sin appAccountToken)', async () => {
    const b = await body({ productId: 'af_noads' }, 'af_noads');
    expect((await call(b)).body.status).toBe('ok');
    expect((await call(b)).body).toEqual({ status: 'duplicate', token_hash: TH });
    const other = await call(b, OTHER_UID);
    expect(other.status).toBe(409);
    expect(other.body).toMatchObject({ error: { code: 'token_belongs_to_other_user' } });
    expect(db.items(TABLES.AF_TABLE_PURCHASES)).toHaveLength(1);
    expect(play.getProduct).not.toHaveBeenCalled();
    expect(play.acknowledge).not.toHaveBeenCalled();
  });

  it('firma mala → 400', async () => {
    const b = await body();
    const [h, p, s] = b.signed_transaction.split('.');
    const flipped = (s[0] === 'A' ? 'B' : 'A') + s.slice(1);
    await expectRejected({ ...b, signed_transaction: `${h}.${p}.${flipped}` }, 'signature');
    // Firmado con otra clave que no es la de la hoja.
    const { privateKey } = generateKeyPairSync('ec', { namedCurve: 'P-256' });
    await expectRejected({ ...b, signed_transaction: await signTx(txPayload(), chain, { key: privateKey }) }, 'signature');
    // Payload cambiado tras firmar.
    const other = (await body({ productId: 'af_noads' })).signed_transaction.split('.')[1];
    await expectRejected({ ...b, signed_transaction: `${h}.${other}.${s}` }, 'signature');
  });

  it('cadena que no acaba en la raíz de confianza → 400', async () => {
    const fake = makeChain();
    await expectRejected({ ...(await body()), signed_transaction: await signTx(txPayload(), fake) }, 'root');
  });

  it('bundle ajeno, SKU distinto, transacción distinta, revocada o del entorno Xcode → 400 sin escribir', async () => {
    await expectRejected(await body({ bundleId: 'com.otra.app' }), 'bundle');
    await expectRejected(await body({ productId: 'af_coins_3' }), 'product');
    await expectRejected(await body({}, 'af_coins_2', '2000000999999999'), 'transaction');
    await expectRejected(await body({ revocationDate: NOW - 10, revocationReason: 0 }), 'revoked');
    await expectRejected(await body({ environment: 'Xcode' }), 'environment');
    await expectRejected(await body({ environment: 'LocalTesting' }), 'environment');
    expect(play.getProduct).not.toHaveBeenCalled();
  });

  it('el 400 no dice el motivo al cliente ni el log lleva el JWS ni el id de transacción', async () => {
    const logs = vi.spyOn(console, 'warn');
    const b = await body({ bundleId: 'com.otra.app' });
    const r = await call(b);
    expect(JSON.stringify(r.body)).not.toMatch(/bundle/i);
    const all = logs.mock.calls.map((c) => String(c[0])).join('\n');
    expect(all).toContain('purchase_app_store_rejected');
    expect(all).toContain('bundle');
    expect(all).not.toContain(TX_ID);
    expect(all).not.toContain(b.signed_transaction.slice(0, 40));
  });

  it('Sandbox: concede como compra de prueba (también en prod, por la revisión de Apple)', async () => {
    const r = await call(await body({ environment: 'Sandbox' }), UID, 'prod');
    expect(r.body.status).toBe('ok');
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: TH })).toMatchObject({ purchase_type: 0, store: 'app_store' });
  });

  it('contrato: signed_transaction ausente con app_store, presente sin él o token no numérico → 400', async () => {
    const b = await body();
    await expectRejected({ token: TX_ID, sku: 'af_coins_2', store: 'app_store' });
    await expectRejected({ token: TX_ID, sku: 'af_coins_2', signed_transaction: b.signed_transaction });
    await expectRejected({ token: TX_ID, sku: 'af_coins_2', store: 'google_play', signed_transaction: b.signed_transaction });
    await expectRejected({ ...b, token: 'tok.AO-J1Oz' });
    await expectRejected({ ...b, store: 'itunes' });
    expect(play.getProduct).not.toHaveBeenCalled();
  });

  it('store ausente o google_play: Play como siempre', async () => {
    const playOk = {
      getProduct: vi.fn((): Promise<ProductPurchase | null> =>
        Promise.resolve({ state: 'purchased', acknowledged: false, purchaseTimeMs: NOW, orderId: 'GPA.1', regionCode: 'CO', quantity: 1 })),
      acknowledge: vi.fn((): Promise<void> => Promise.resolve()),
      listVoided: vi.fn(),
    };
    const h = createHandler(() => ({ ddb: db.client, play: playOk, appStore: verifier(), integrity: okIntegrity, env: { ...TABLES, AF_ENV: 'dev' }, now: () => NOW }));
    const ev = (b: unknown) => httpEvent({ routeKey: ROUTE, body: b, uid: UID, headers: { 'x-firebase-appcheck': `p${++n}` } });
    expect(res(await h(ev({ token: 'tok.a', sku: 'af_coins_2' }))).body.status).toBe('ok');
    expect(res(await h(ev({ token: 'tok.b', sku: 'af_coins_2', store: 'google_play' }))).body.status).toBe('ok');
    expect(playOk.getProduct).toHaveBeenCalledTimes(2);
    expect(playOk.acknowledge).toHaveBeenCalledTimes(2);
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: sha256hex('tok.a') })).not.toHaveProperty('store');
  });
});
