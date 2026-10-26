// rewardedSsv con el SSV de AdMob (D-049; 08 §10.1-§10.2): firma ECDSA P-256/SHA-256 sobre la consulta cruda con un par
// de claves generado en el test, caché de las claves de Google con un fetch falso (sin red), idempotencia, cuenta borrada,
// plazo del timestamp y límite por UID.
import { generateKeyPairSync } from 'node:crypto';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ADMOB_VERIFIER_KEYS_URL,
  AdMobVerifierKeys,
  KEYS_MIN_REFRESH_MS,
  KEYS_TTL_MS,
  parseVerifierKeys,
  splitSignedQuery,
  verifyAdMobSignature,
} from '../../functions/rewarded-ssv/admobSignature.ts';
import type { VerifierKeySource } from '../../functions/rewarded-ssv/admobSignature.ts';
import { SSV_DAILY_LIMIT, SSV_MAX_AGE_MS, SSV_MAX_SKEW_MS, createHandler as ssvHandler } from '../../functions/rewarded-ssv/handler.ts';
import { UpstreamError } from '../../functions/shared/errors.ts';
import { markDeleted } from '../../functions/shared/rateLimit.ts';
import { uidHash } from '../../functions/shared/log.ts';
import { validator } from '../contract/ajv.ts';
import { NOW, OTHER_UID, UID, admobParams, admobSigner, fakeFetch, httpEvent, jsonResponse, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const N1 = '6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f';
const N2 = '0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d';

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

describe('admobSignature: consulta firmada', () => {
  const signer = admobSigner();

  it('separa lo firmado de signature y key_id, que deben ser los dos últimos', () => {
    const raw = signer.query(admobParams());
    const s = splitSignedQuery(raw);
    expect(s?.keyId).toBe(signer.keyId);
    expect(s?.message).toBe(signer.message(admobParams()));
    expect(verifyAdMobSignature(s!.message, s!.signature, signer.publicKey)).toBe(true);
    // Otras formas: sin key_id, key_id antes de signature, algo detrás de key_id, sin parámetros firmados.
    const sig = signer.signRaw('a=1');
    for (const bad of [
      `a=1&signature=${sig}`,
      `a=1&key_id=1&signature=${sig}`,
      `a=1&signature=${sig}&key_id=1&x=2`,
      `signature=${sig}&key_id=1`,
      `a=1&signature=no+base64&key_id=1`,
      'a=1',
      '',
    ]) expect(splitSignedQuery(bad), bad).toBeUndefined();
  });

  it('la firma cubre la consulta tal cual: reordenar, decodificar o cambiar un valor la invalida', () => {
    const params = admobParams();
    const raw = signer.query(params);
    const s = splitSignedQuery(raw)!;
    const tail = raw.slice(raw.indexOf('&signature='));
    const reordered = [...params].reverse();
    const variants = [
      signer.message(reordered) + tail, // mismos parámetros en otro orden
      s.message.replace('%7C', '|') + tail, // el mismo valor sin codificar
      s.message.replace('reward_amount=1', 'reward_amount=100') + tail,
    ];
    for (const v of variants) {
      const p = splitSignedQuery(v)!;
      expect(p).toBeDefined();
      expect(verifyAdMobSignature(p.message, p.signature, signer.publicKey), v).toBe(false);
    }
  });

  it('lee verifier-keys.json: pem o base64 (SPKI), solo EC P-256', () => {
    const other = generateKeyPairSync('ec', { namedCurve: 'prime256v1' }).publicKey;
    const rsa = generateKeyPairSync('rsa', { modulusLength: 2048 }).publicKey;
    const keys = parseVerifierKeys({
      keys: [
        { keyId: 1, pem: signer.pem, base64: '' },
        { keyId: '2', base64: other.export({ type: 'spki', format: 'der' }).toString('base64') },
        { keyId: 3, pem: rsa.export({ type: 'spki', format: 'pem' }).toString() },
        { keyId: 4, pem: 'basura' },
        { keyId: 'x5', pem: signer.pem },
      ],
    });
    expect([...keys.keys()]).toEqual(['1', '2']);
    expect(() => parseVerifierKeys({ keys: [] })).toThrow(UpstreamError);
    expect(() => parseVerifierKeys({})).toThrow(UpstreamError);
  });
});

describe('AdMobVerifierKeys: caché de las claves de Google', () => {
  const signer = admobSigner('111');
  const second = admobSigner('222');
  let now = NOW;
  let body: unknown;
  let status = 200;
  const make = () => {
    const f = fakeFetch(() => jsonResponse(status, body));
    return { f, cache: new AdMobVerifierKeys({ fetchFn: f, now: () => now }) };
  };
  beforeEach(() => {
    now = NOW;
    status = 200;
    body = signer.keysJson();
  });

  it('descarga una vez y sirve de la caché durante 24 h; después vuelve a descargar', async () => {
    const { f, cache } = make();
    expect(await cache.get('111')).toBeDefined();
    now += KEYS_TTL_MS - 1;
    expect(await cache.get('111')).toBeDefined();
    expect(f.calls).toHaveLength(1);
    expect(f.calls[0].url).toBe(ADMOB_VERIFIER_KEYS_URL);
    now += 1;
    expect(await cache.get('111')).toBeDefined();
    expect(f.calls).toHaveLength(2);
  });

  it('key_id desconocido: refresca una vez (con espera mínima) y encuentra la clave nueva', async () => {
    const { f, cache } = make();
    expect(await cache.get('111')).toBeDefined();
    // Google rota: aparece la 222. Dentro de la espera mínima no se descarga otra vez.
    body = signer.keysJson([{ keyId: 222, pem: second.pem }]);
    now += 1000;
    expect(await cache.get('111')).toBeDefined(); // la conocida sigue sin descargar
    now += KEYS_MIN_REFRESH_MS;
    expect(await cache.get('222')).toBeDefined();
    expect(f.calls).toHaveLength(2);
    // Un key_id inventado justo después: no descarga (espera mínima) y no hay clave.
    expect(await cache.get('999')).toBeUndefined();
    expect(f.calls).toHaveLength(2);
  });

  it('caducada y sin poder descargar: 502, nunca las claves viejas', async () => {
    const { cache } = make();
    await cache.get('111');
    now += KEYS_TTL_MS;
    status = 503;
    await expect(cache.get('111')).rejects.toBeInstanceOf(UpstreamError);
    // Dentro de la espera mínima tampoco usa las viejas.
    await expect(cache.get('111')).rejects.toBeInstanceOf(UpstreamError);
    now += KEYS_MIN_REFRESH_MS;
    status = 200;
    expect(await cache.get('111')).toBeDefined();
  });

  it('JSON roto o sin claves → UpstreamError', async () => {
    body = { nada: true };
    await expect(make().cache.get('111')).rejects.toBeInstanceOf(UpstreamError);
  });
});

describe('rewardedSsv (AdMob)', () => {
  const v = validator('rewarded-ssv.response.schema.json');
  let signer: ReturnType<typeof admobSigner>;
  let now = NOW;
  beforeEach(() => {
    signer = admobSigner();
    now = NOW;
  });
  const h = (keys: VerifierKeySource = signer.keys) => ssvHandler(() => ({ ddb: db.client, keys, env: TABLES, now: () => now }));
  const call = async (rawQuery: string, method: 'GET' | 'POST' = 'GET', keys: VerifierKeySource = signer.keys) =>
    res(await h(keys)(httpEvent({ routeKey: `${method} /v1/ssv`, rawQuery })));
  const signed = (over: Record<string, string | undefined> = {}) => signer.query(admobParams(over));

  it('firma buena: anota rewards/{nonce} con TTL de 30 días y responde ok', async () => {
    const r = await call(signed());
    expect(r).toMatchObject({ status: 200, body: { status: 'ok' } });
    expect(v(r.body)).toBe(true);
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })).toMatchObject({
      uid: UID,
      placement: 'rescue',
      network: '5450213213286189855',
      ts: NOW,
      ttl: Math.floor(NOW / 1000) + 30 * 86400,
    });
  });

  it('con la caché real y un fetch falso: key_id desconocido → refresca y verifica', async () => {
    const old = admobSigner('1');
    let body: unknown = old.keysJson();
    const f = fakeFetch(() => jsonResponse(200, body));
    const keys = new AdMobVerifierKeys({ fetchFn: f, now: () => now });
    expect((await call(old.query(admobParams()), 'GET', keys)).body).toEqual({ status: 'ok' });
    body = old.keysJson([{ keyId: Number(signer.keyId), pem: signer.pem }]);
    now += KEYS_MIN_REFRESH_MS;
    const r = await call(signer.query(admobParams({ custom_data: `rescue|${N2}`, transaction_id: 'tx-2', timestamp: String(now) })), 'GET', keys);
    expect(r.body).toEqual({ status: 'ok' });
    expect(f.calls).toHaveLength(2);
  });

  it('firma inválida, de otra clave, key_id desconocido o consulta reordenada → 403 sin escribir ni gastar cupo', async () => {
    const intruder = generateKeyPairSync('ec', { namedCurve: 'prime256v1' }).privateKey;
    const good = signed();
    const tail = good.slice(good.indexOf('&signature='));
    const variants = [
      signer.query(admobParams(), { key: intruder }),
      signer.query(admobParams(), { keyId: '42' }),
      signer.message([...admobParams()].reverse()) + tail,
      good.replace('reward_amount=1', 'reward_amount=9'),
      signer.message(admobParams()),
      '',
    ];
    for (const q of variants) {
      const r = await call(q);
      expect(r.status, q).toBe(403);
      expect(r.body).toMatchObject({ error: { code: 'bad_signature' } });
    }
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toHaveLength(0);
    expect(db.items(TABLES.AF_TABLE_RATE_LIMITS)).toHaveLength(0);
  });

  it('repetición idempotente: la misma llamada → duplicate sin reescribir ni gastar cupo, también por POST', async () => {
    const q = signed();
    await call(q);
    const before = structuredClone(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 }));
    const counter = () => db.items(TABLES.AF_TABLE_RATE_LIMITS).find((i) => String(i.key).startsWith('ssv#'))?.count;
    now = NOW + 5000;
    const r = await call(q, 'POST');
    expect(r.body).toEqual({ status: 'duplicate' });
    expect(v(r.body)).toBe(true);
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })).toEqual(before);
    expect(counter()).toBe(1);
    // Otro transaction_id (y otro UID) no se queda con un nonce ya anotado.
    expect((await call(signed({ user_id: OTHER_UID, transaction_id: 'tx-3' }))).body).toEqual({ status: 'duplicate' });
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })?.uid).toBe(UID);
  });

  it('UID borrado hace menos de 2 h → 200 duplicate sin fila en rewards', async () => {
    await markDeleted(db.client, TABLES.AF_TABLE_RATE_LIMITS, uidHash(UID), NOW);
    const r = await call(signed());
    expect(r).toMatchObject({ status: 200, body: { status: 'duplicate' } });
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toHaveLength(0);
  });

  it('sin user_id o sin custom_data (recompensado sin UID, prueba de la consola) → 200 ignored sin escribir', async () => {
    for (const q of [signed({ user_id: undefined, custom_data: undefined }), signed({ user_id: undefined }), signed({ custom_data: '' })]) {
      const r = await call(q);
      expect(r).toMatchObject({ status: 200, body: { status: 'ignored' } });
      expect(v(r.body)).toBe(true);
    }
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toHaveLength(0);
    expect(db.items(TABLES.AF_TABLE_RATE_LIMITS)).toHaveLength(0);
  });

  it('timestamp fuera de plazo (más de 24 h o en el futuro) → 400 request_expired', async () => {
    for (const ts of [NOW - SSV_MAX_AGE_MS - 1, NOW + SSV_MAX_SKEW_MS + 1]) {
      const r = await call(signed({ timestamp: String(ts) }));
      expect(r.status).toBe(400);
      expect(r.body).toMatchObject({ error: { code: 'request_expired' } });
    }
    expect((await call(signed({ timestamp: String(NOW - SSV_MAX_AGE_MS + 1000) }))).body).toEqual({ status: 'ok' });
  });

  it('parámetros firmados pero mal formados o repetidos → 400', async () => {
    for (const over of [{ custom_data: 'rescue' }, { custom_data: 'rescue|nonce' }, { user_id: 'no válido!' }, { transaction_id: undefined }, { timestamp: 'ayer' }]) {
      expect((await call(signed(over))).status, JSON.stringify(over)).toBe(400);
    }
    const dup = signer.query([...admobParams(), ['user_id', OTHER_UID]]);
    expect((await call(dup)).status).toBe(400);
    expect(db.items(TABLES.AF_TABLE_REWARDS)).toHaveLength(0);
  });

  it(`límite de ${SSV_DAILY_LIMIT} recompensas por UID y día → 429`, async () => {
    for (let i = 0; i < SSV_DAILY_LIMIT; i++) {
      const nonce = `6f1c2a4e-8b3d-4f5a-9c7e-${i.toString(16).padStart(12, '0')}`;
      expect((await call(signed({ custom_data: `rescue|${nonce}`, transaction_id: `t${i}` }))).status).toBe(200);
    }
    const r = await call(signed({ custom_data: `rescue|${N2}`, transaction_id: 't-last' }));
    expect(r.status).toBe(429);
    expect(r.body).toMatchObject({ error: { code: 'rate_limited' } });
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N2 })).toBeUndefined();
  });

  it('claves de Google inaccesibles → 502; error de DynamoDB → 500', async () => {
    const down = { get: () => Promise.reject(new UpstreamError('admob-keys', 'http', 503)) };
    expect((await call(signed(), 'GET', down)).status).toBe(502);
    db.failNext = { command: 'TransactWrite', table: TABLES.AF_TABLE_REWARDS, error: new Error('boom') };
    expect((await call(signed())).status).toBe(500);
  });

  it('los logs no llevan el UID, la firma ni el user_id', async () => {
    const lines: string[] = [];
    vi.spyOn(console, 'log').mockImplementation((l: string) => void lines.push(l));
    vi.spyOn(console, 'warn').mockImplementation((l: string) => void lines.push(l));
    await call(signed());
    await call(signer.query(admobParams(), { keyId: '42' }));
    const all = lines.join('\n');
    expect(all).not.toContain(UID);
    expect(all).not.toContain(splitSignedQuery(signed())!.signature.toString('base64url'));
  });
});
