// Tests de functions/shared (F2-3): integridad, Google OAuth2, Play, Firebase Auth, límites, logs y entorno.
import { GetSecretValueCommand, SecretsManagerClient } from '@aws-sdk/client-secrets-manager';
import { mockClient } from 'aws-sdk-client-mock';
import { jwtVerify } from 'jose';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CROCKFORD, newAccountCode, newRequestToken, safeEqual } from '../../functions/shared/crypto.ts';
import { fakeVerifierEnabled } from '../../functions/shared/env.ts';
import { ConfigError, HttpError, UpstreamError } from '../../functions/shared/errors.ts';
import { IdentityToolkitAdmin } from '../../functions/shared/firebaseAdmin.ts';
import { GOOGLE_TOKEN_URL, ServiceAccountTokenProvider, parseServiceAccount } from '../../functions/shared/googleAuth.ts';
import {
  AppCheckVerifier,
  DevIntegrityVerifier,
  PlayIntegrityVerifier,
  createIntegrityVerifier,
  requireIntegrity,
} from '../../functions/shared/integrity.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import { log, metric, uidHash } from '../../functions/shared/log.ts';
import { DevPlayApi, FakePlayApi, GooglePlayApi, parseProductPurchase } from '../../functions/shared/playApi.ts';
import { consumeOnce, enforce, hit } from '../../functions/shared/rateLimit.ts';
import { SecretsManagerSource } from '../../functions/shared/secrets.ts';
import { PATTERNS } from '../../functions/shared/validate.ts';
import { MemDb, TABLES } from './memdb.ts';
import { MapSecrets, NOW, UID, appCheckIssuer, fakeFetch, jsonResponse, testServiceAccount } from './fixtures.ts';

describe('crypto', () => {
  it('el código de borrado tiene 26 caracteres Crockford, empieza por 0-7 y cumple el contrato', () => {
    for (let i = 0; i < 200; i++) {
      const c = newAccountCode();
      expect(c).toHaveLength(26);
      expect(PATTERNS.accountCode.test(c), c).toBe(true);
      for (const ch of c) expect(CROCKFORD).toContain(ch);
    }
    expect(newAccountCode(() => Buffer.alloc(16, 0xff))).toBe('7ZZZZZZZZZZZZZZZZZZZZZZZZZ');
    expect(newAccountCode(() => Buffer.alloc(16, 0))).toBe('0'.repeat(26));
  });

  it('el token de confirmación tiene 43 caracteres base64url', () => {
    const t = newRequestToken();
    expect(PATTERNS.requestToken.test(t)).toBe(true);
  });

  it('safeEqual compara en tiempo constante y rechaza longitudes distintas', () => {
    expect(safeEqual('abc', 'abc')).toBe(true);
    expect(safeEqual('abc', 'abd')).toBe(false);
    expect(safeEqual('abc', 'abcd')).toBe(false);
  });
});

describe('log', () => {
  afterEach(() => vi.restoreAllMocks());

  it('tacha uid, tokens, códigos e IP aunque se pasen por error', () => {
    const spy = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    log.info('x', { uid: UID, token: 't', code: 'c', request_token: 'r', ip: '1.2.3.4', uid_hash: uidHash(UID), ok: 1 });
    const line = String(spy.mock.calls[0][0]);
    expect(line).not.toContain(UID);
    expect(line).not.toContain('1.2.3.4');
    expect(JSON.parse(line)).toMatchObject({ uid: '[redactado]', token: '[redactado]', code: '[redactado]', ok: 1 });
  });

  it('uidHash es estable, corto y no contiene el UID', () => {
    expect(uidHash(UID)).toMatch(/^[0-9a-f]{16}$/);
    expect(uidHash(UID)).toBe(uidHash(UID));
    expect(uidHash(UID)).not.toBe(uidHash(`${UID}x`));
  });

  it('metric escribe EMF con el espacio de nombres y la dimensión Env del ops-stack', () => {
    const spy = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    metric('IntegrityChecks', 'dev');
    const emf = JSON.parse(String(spy.mock.calls[0][0])) as { _aws: { CloudWatchMetrics: Array<{ Namespace: string; Dimensions: string[][] }> }; Env: string; IntegrityChecks: number };
    expect(emf._aws.CloudWatchMetrics[0]).toMatchObject({ Namespace: 'AF/Backend', Dimensions: [['Env']] });
    expect(emf).toMatchObject({ Env: 'dev', IntegrityChecks: 1 });
  });
});

describe('env: verificador falso', () => {
  it('solo con AF_FAKE_PLAY_VERIFIER=1 y AF_ENV=dev', () => {
    expect(fakeVerifierEnabled({ AF_FAKE_PLAY_VERIFIER: '1', AF_ENV: 'dev' })).toBe(true);
    expect(fakeVerifierEnabled({ AF_FAKE_PLAY_VERIFIER: '1', AF_ENV: 'prod' })).toBe(false);
    expect(fakeVerifierEnabled({ AF_FAKE_PLAY_VERIFIER: 'true', AF_ENV: 'dev' })).toBe(false);
    expect(fakeVerifierEnabled({ AF_ENV: 'dev' })).toBe(false);
  });
});

describe('secrets', () => {
  afterEach(() => vi.restoreAllMocks());

  it('lee de Secrets Manager con caché de 5 minutos y sin red', async () => {
    const sm = mockClient(SecretsManagerClient);
    sm.on(GetSecretValueCommand).resolves({ SecretString: 'valor' });
    let now = NOW;
    const src = new SecretsManagerSource(new SecretsManagerClient({ region: 'sa-east-1', credentials: { accessKeyId: 'x', secretAccessKey: 'x' } }), () => now);
    expect(await src.get('arn:1')).toBe('valor');
    expect(await src.get('arn:1')).toBe('valor');
    expect(sm.calls()).toHaveLength(1);
    now += 6 * 60_000;
    await src.get('arn:1');
    expect(sm.calls()).toHaveLength(2);
    sm.restore();
  });

  it('error de Secrets Manager → UpstreamError; secreto sin texto → ConfigError', async () => {
    const sm = mockClient(SecretsManagerClient);
    const client = new SecretsManagerClient({ region: 'sa-east-1', credentials: { accessKeyId: 'x', secretAccessKey: 'x' } });
    sm.on(GetSecretValueCommand).rejects(new Error('boom'));
    await expect(new SecretsManagerSource(client).get('a')).rejects.toBeInstanceOf(UpstreamError);
    sm.on(GetSecretValueCommand).resolves({});
    await expect(new SecretsManagerSource(client).get('a')).rejects.toBeInstanceOf(ConfigError);
    sm.restore();
  });
});

describe('googleAuth: token OAuth2 de la cuenta de servicio', () => {
  it('firma el JWT con RS256, lo canjea en oauth2.googleapis.com (no en el token_uri del JSON) y lo cachea', async () => {
    const sa = await testServiceAccount();
    const secrets = new MapSecrets();
    secrets.values.set('arn:play', sa.json);
    const f = fakeFetch(() => jsonResponse(200, { access_token: 'ya29.test', expires_in: 3600 }));
    let now = NOW;
    const p = new ServiceAccountTokenProvider({ secrets, secretArn: 'arn:play', scope: 'https://www.googleapis.com/auth/androidpublisher', fetchFn: f, now: () => now });
    expect(await p.token()).toBe('ya29.test');
    expect(await p.token()).toBe('ya29.test');
    expect(f.calls).toHaveLength(1);
    expect(f.calls[0].url).toBe(GOOGLE_TOKEN_URL);
    const form = new URLSearchParams(f.calls[0].body);
    expect(form.get('grant_type')).toBe('urn:ietf:params:oauth:grant-type:jwt-bearer');
    const { payload, protectedHeader } = await jwtVerify(form.get('assertion') ?? '', sa.publicKey, {
      audience: GOOGLE_TOKEN_URL,
      issuer: 'test@amazon-fill-dev.iam.gserviceaccount.com',
      currentDate: new Date(now),
    });
    expect(payload.scope).toBe('https://www.googleapis.com/auth/androidpublisher');
    expect(protectedHeader).toMatchObject({ alg: 'RS256', kid: 'test-kid' });
    now += 3600_000; // caducado: pide otro
    await p.token();
    expect(f.calls).toHaveLength(2);
  });

  it('secreto de relleno o con forma rara → ConfigError; Google rechaza → UpstreamError', async () => {
    expect(() => parseServiceAccount('relleno-aleatorio')).toThrow(ConfigError);
    expect(() => parseServiceAccount('{"type":"service_account"}')).toThrow(ConfigError);
    const sa = await testServiceAccount();
    const secrets = new MapSecrets();
    secrets.values.set('arn', sa.json);
    const f = fakeFetch(() => jsonResponse(400, { error: 'invalid_grant' }));
    const p = new ServiceAccountTokenProvider({ secrets, secretArn: 'arn', scope: 's', fetchFn: f });
    await expect(p.token()).rejects.toBeInstanceOf(UpstreamError);
    const net = fakeFetch(() => Promise.reject(new TypeError('fetch failed')));
    await expect(new ServiceAccountTokenProvider({ secrets, secretArn: 'arn', scope: 's', fetchFn: net }).token()).rejects.toBeInstanceOf(UpstreamError);
  });
});

const staticAuth = { token: () => Promise.resolve('ya29.test') };

describe('playApi (Google falso)', () => {
  it('products.get: comprada, pendiente, cancelada; 400/404/410 → null; 5xx → UpstreamError', async () => {
    const f = fakeFetch((c) => {
      if (c.url.includes('/tokens/ok')) return jsonResponse(200, { purchaseState: 0, acknowledgementState: 0, purchaseTimeMillis: '1792500000000', orderId: 'GPA.1', regionCode: 'BR', obfuscatedExternalAccountId: UID, quantity: 1 });
      if (c.url.includes('/tokens/pend')) return jsonResponse(200, { purchaseState: 2 });
      if (c.url.includes('/tokens/canc')) return jsonResponse(200, { purchaseState: 1 });
      if (c.url.includes('/tokens/gone')) return jsonResponse(410, {});
      if (c.url.includes('/tokens/bad')) return jsonResponse(400, {});
      return jsonResponse(503, {});
    });
    const play = new GooglePlayApi({ packageName: 'com.amazonfill.dev', auth: staticAuth, fetchFn: f });
    expect(await play.getProduct('af_coins_2', 'ok')).toMatchObject({ state: 'purchased', acknowledged: false, purchaseTimeMs: 1792500000000, regionCode: 'BR', obfuscatedAccountId: UID });
    expect((await play.getProduct('af_coins_2', 'pend'))?.state).toBe('pending');
    expect((await play.getProduct('af_coins_2', 'canc'))?.state).toBe('canceled');
    expect(await play.getProduct('af_coins_2', 'gone')).toBeNull();
    expect(await play.getProduct('af_coins_2', 'bad')).toBeNull();
    await expect(play.getProduct('af_coins_2', 'boom')).rejects.toBeInstanceOf(UpstreamError);
    expect(f.calls[0].url).toBe('https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.amazonfill.dev/purchases/products/af_coins_2/tokens/ok');
    expect(f.calls[0].headers.authorization).toBe('Bearer ya29.test');
  });

  it('acknowledge hace POST a :acknowledge y un error es UpstreamError', async () => {
    const f = fakeFetch((c) => (c.url.endsWith('/tokens/a:acknowledge') ? new Response(null, { status: 204 }) : jsonResponse(500, {})));
    const play = new GooglePlayApi({ packageName: 'com.amazonfill.dev', auth: staticAuth, fetchFn: f });
    await play.acknowledge('af_noads', 'a');
    expect(f.calls[0]).toMatchObject({ method: 'POST' });
    await expect(play.acknowledge('af_noads', 'b')).rejects.toBeInstanceOf(UpstreamError);
  });

  it('voidedpurchases.list pagina con token y solo pide compras in-app', async () => {
    const f = fakeFetch((c) =>
      c.url.includes('token=p2')
        ? jsonResponse(200, { voidedPurchases: [{ purchaseToken: 'b', voidedTimeMillis: '2' }] })
        : jsonResponse(200, { voidedPurchases: [{ purchaseToken: 'a', voidedTimeMillis: '1', voidedReason: 1 }, { nope: 1 }], tokenPagination: { nextPageToken: 'p2' } }),
    );
    const play = new GooglePlayApi({ packageName: 'com.amazonfill.dev', auth: staticAuth, fetchFn: f });
    const p1 = await play.listVoided(1, 2);
    expect(p1).toEqual({ items: [{ purchaseToken: 'a', voidedTimeMs: 1, orderId: undefined, voidedReason: 1, voidedSource: undefined }], nextPageToken: 'p2' });
    const p2 = await play.listVoided(1, 2, 'p2');
    expect(p2.nextPageToken).toBeUndefined();
    const q = new URL(f.calls[0].url).searchParams;
    expect(q.get('type')).toBe('0');
    expect(q.get('startTime')).toBe('1');
  });

  it('parseProductPurchase: estado desconocido cuenta como cancelada (nunca concede)', () => {
    expect(parseProductPurchase({ purchaseState: 7 }).state).toBe('canceled');
    expect(parseProductPurchase({}).state).toBe('canceled');
  });

  it('DevPlayApi: los fake-* van al falso, el resto a Play; el job no consulta Play', async () => {
    const real = { getProduct: vi.fn(() => Promise.resolve(null)), acknowledge: vi.fn(() => Promise.resolve()), listVoided: vi.fn() };
    const dev = new DevPlayApi(new FakePlayApi(() => NOW), real);
    expect((await dev.getProduct('af_noads', 'fake-pending-1'))?.state).toBe('pending');
    expect((await dev.getProduct('af_noads', 'fake-canceled-1'))?.state).toBe('canceled');
    expect(await dev.getProduct('af_noads', 'fake-unknown-1')).toBeNull();
    expect((await dev.getProduct('af_noads', 'fake-ok-1'))?.state).toBe('purchased');
    await dev.getProduct('af_noads', 'real-token');
    expect(real.getProduct).toHaveBeenCalledTimes(1);
    expect((await dev.listVoided(0, 1)).items).toEqual([]);
    expect(real.listVoided).not.toHaveBeenCalled();
  });
});

describe('firebaseAdmin (Identity Toolkit falso)', () => {
  it('borra el usuario; USER_NOT_FOUND es éxito (idempotente); otro error es UpstreamError', async () => {
    const f = fakeFetch((c) => {
      const body = JSON.parse(c.body) as { localId: string };
      if (body.localId === 'existe') return jsonResponse(200, {});
      if (body.localId === 'nada') return jsonResponse(400, { error: { code: 400, message: 'USER_NOT_FOUND' } });
      return jsonResponse(403, { error: { message: 'PERMISSION_DENIED' } });
    });
    const admin = new IdentityToolkitAdmin({ projectId: 'amazon-fill-dev', auth: staticAuth, fetchFn: f });
    expect(await admin.deleteUser('existe')).toEqual({ existed: true });
    expect(await admin.deleteUser('nada')).toEqual({ existed: false });
    await expect(admin.deleteUser('x')).rejects.toBeInstanceOf(UpstreamError);
    expect(f.calls[0].url).toBe('https://identitytoolkit.googleapis.com/v1/projects/amazon-fill-dev/accounts:delete');
  });
});

describe('integridad: AppCheckVerifier (variante a) contra un JWKS local', () => {
  it('acepta un token bueno y devuelve su jti y exp', async () => {
    const iss = await appCheckIssuer();
    const v = new AppCheckVerifier({ projectId: iss.projectId, keySet: iss.keySet, now: () => NOW });
    const r = await v.verify(await iss.sign({ jti: 'j1' }));
    expect(r).toMatchObject({ ok: true, jti: 'j1' });
  });

  it('rechaza firma ajena, caducado, otro proyecto, emisor raro, número que no cuadra y app no admitida', async () => {
    const iss = await appCheckIssuer();
    const v = new AppCheckVerifier({ projectId: iss.projectId, keySet: iss.keySet, now: () => NOW });
    const nowS = Math.floor(NOW / 1000);
    expect((await v.verify(await iss.sign({ foreignKey: true }))).ok).toBe(false);
    expect((await v.verify(await iss.sign({ iatS: nowS - 7200, expS: nowS - 3600 }))).ok).toBe(false);
    expect((await v.verify(await iss.sign({ aud: ['projects/123456789', 'projects/amazon-fill-prod'] }))).ok).toBe(false);
    expect((await v.verify(await iss.sign({ iss: 'https://evil.invalid/123456789' }))).ok).toBe(false);
    expect((await v.verify(await iss.sign({ iss: 'https://firebaseappcheck.googleapis.com/999' }))).ok).toBe(false);
    expect((await v.verify('no-es-un-jwt')).ok).toBe(false);
    const strict = new AppCheckVerifier({ projectId: iss.projectId, keySet: iss.keySet, appIds: ['1:123456789:android:ffff00'], now: () => NOW });
    expect((await strict.verify(await iss.sign())).ok).toBe(false);
    expect((await strict.verify(await iss.sign({ sub: '1:123456789:android:ffff00' }))).ok).toBe(true);
  });

  it('si el JWKS no se puede descargar es un fallo de Google (UpstreamError), no del cliente', async () => {
    const v = new AppCheckVerifier({ projectId: 'amazon-fill-dev', keySet: () => Promise.reject(new TypeError('fetch failed')), now: () => NOW });
    const iss = await appCheckIssuer();
    await expect(v.verify(await iss.sign())).rejects.toBeInstanceOf(UpstreamError);
  });

  it('Play Integrity directo (variante b) no está implementado y nunca concede', async () => {
    await expect(new PlayIntegrityVerifier().verify()).rejects.toThrow(/no está implementado/);
    expect(createIntegrityVerifier({ variant: 'play-integrity', projectId: 'amazon-fill-dev', fake: false }).variant).toBe('play-integrity');
    expect(createIntegrityVerifier({ variant: 'app-check', projectId: 'amazon-fill-dev', fake: false }).variant).toBe('app-check');
    expect(createIntegrityVerifier({ variant: 'app-check', projectId: 'amazon-fill-dev', fake: true }).variant).toBe('fake');
  });

  it('el falso de dev acepta fake-ok-<algo> y manda el resto al real', async () => {
    const real: IntegrityVerifier = { variant: 'app-check', verify: vi.fn(() => Promise.resolve({ ok: false })) };
    const dev = new DevIntegrityVerifier(real, () => NOW);
    expect((await dev.verify('fake-ok-1')).ok).toBe(true);
    expect((await dev.verify('fake-ok-')).ok).toBe(false);
    expect((await dev.verify('otro')).ok).toBe(false);
  });
});

describe('integridad: requireIntegrity (caché 24 h y anti-replay)', () => {
  let db: MemDb;
  beforeEach(() => {
    db = new MemDb();
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
  });
  afterEach(() => {
    db.restore();
    vi.restoreAllMocks();
  });

  const depsWith = async (now = NOW) => {
    const iss = await appCheckIssuer();
    return {
      iss,
      deps: {
        ddb: db.client,
        verifier: new AppCheckVerifier({ projectId: iss.projectId, keySet: iss.keySet, now: () => now }),
        integrityTable: TABLES.AF_TABLE_INTEGRITY,
        rateLimitsTable: TABLES.AF_TABLE_RATE_LIMITS,
        envName: 'dev',
        now: () => now,
      },
    };
  };

  it('sin token y sin caché → 403 integrity_failed', async () => {
    const { deps } = await depsWith();
    await expect(requireIntegrity(deps, UID, undefined)).rejects.toMatchObject({ status: 403, code: 'integrity_failed' });
  });

  it('token bueno: guarda el veredicto 24 h; después pasa sin token', async () => {
    const { deps, iss } = await depsWith();
    await requireIntegrity(deps, UID, await iss.sign({ jti: 'a' }));
    const cached = db.get(TABLES.AF_TABLE_INTEGRITY, { uid: UID });
    expect(cached).toMatchObject({ verdict: 'ok', ttl: Math.floor(NOW / 1000) + 86400 });
    await requireIntegrity(deps, UID, undefined);
  });

  it('veredicto caducado (el TTL de DynamoDB borra tarde) → vuelve a exigir token', async () => {
    const { deps } = await depsWith();
    db.put(TABLES.AF_TABLE_INTEGRITY, { uid: UID, verdict: 'ok', ttl: Math.floor(NOW / 1000) - 1 });
    await expect(requireIntegrity(deps, UID, undefined)).rejects.toBeInstanceOf(HttpError);
  });

  it('el mismo token no vale dos veces (uso limitado), ni para otro UID', async () => {
    const { deps, iss } = await depsWith();
    const token = await iss.sign({ jti: 'unico' });
    await requireIntegrity(deps, UID, token);
    await expect(requireIntegrity(deps, 'OtroUid123', token)).rejects.toMatchObject({ code: 'integrity_failed' });
  });

  it('token malo o caducado → 403 y no se guarda nada en la caché', async () => {
    const { deps, iss } = await depsWith();
    const nowS = Math.floor(NOW / 1000);
    await expect(requireIntegrity(deps, UID, await iss.sign({ foreignKey: true }))).rejects.toMatchObject({ status: 403 });
    await expect(requireIntegrity(deps, UID, await iss.sign({ iatS: nowS - 7200, expS: nowS - 60 }))).rejects.toMatchObject({ status: 403 });
    expect(db.get(TABLES.AF_TABLE_INTEGRITY, { uid: UID })).toBeUndefined();
  });

  it('un verificador que revienta → 403 (no 500) y la variante (b) nunca deja pasar', async () => {
    const { deps } = await depsWith();
    await expect(requireIntegrity({ ...deps, verifier: new PlayIntegrityVerifier() }, UID, 'x')).rejects.toMatchObject({ status: 403 });
  });
});

describe('rateLimit', () => {
  let db: MemDb;
  beforeEach(() => {
    db = new MemDb();
  });
  afterEach(() => db.restore());

  it('cuenta por ventana fija y deja pasar hasta el límite', async () => {
    const t = TABLES.AF_TABLE_RATE_LIMITS;
    for (let i = 1; i <= 3; i++) expect((await hit(db.client, t, 'k', 3, 60, NOW)).allowed).toBe(true);
    const r = await hit(db.client, t, 'k', 3, 60, NOW);
    expect(r).toMatchObject({ allowed: false, count: 4 });
    expect(r.retryAfterS).toBeGreaterThan(0);
    expect(r.retryAfterS).toBeLessThanOrEqual(60);
    expect((await hit(db.client, t, 'k', 3, 60, NOW + 60_000)).allowed).toBe(true); // ventana siguiente
    await expect(enforce(db.client, t, 'k', 1, 60, NOW + 60_000)).rejects.toMatchObject({ status: 429, code: 'rate_limited' });
    for (const it of db.items(t)) expect(typeof it.ttl).toBe('number');
  });

  it('consumeOnce: la primera vez true, después false', async () => {
    const t = TABLES.AF_TABLE_RATE_LIMITS;
    expect(await consumeOnce(db.client, t, 'x', 1)).toBe(true);
    expect(await consumeOnce(db.client, t, 'x', 1)).toBe(false);
  });
});
