// deleteAccount (F2-8): código de borrado, borrado por app y por web, idempotencia y límites,
// con DynamoDB en memoria y Firebase Auth falso.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { accountCodeHash, sha256hex } from '../../functions/shared/crypto.ts';
import { UpstreamError } from '../../functions/shared/errors.ts';
import type { FirebaseAdmin } from '../../functions/shared/firebaseAdmin.ts';
import type { IntegrityVerifier } from '../../functions/shared/integrity.ts';
import { LIMITS, REQUEST_TOKEN_TTL_S, createHandler } from '../../functions/delete-account/handler.ts';
import { validator } from '../contract/ajv.ts';
import { NOW, OTHER_UID, UID, httpEvent, res } from './fixtures.ts';
import { MemDb, TABLES } from './memdb.ts';

const N1 = '6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f';
const N2 = '0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d';

class FakeAuth implements FirebaseAdmin {
  users = new Set<string>([UID, OTHER_UID]);
  fail = false;
  deleteUser(uid: string): Promise<{ existed: boolean }> {
    if (this.fail) return Promise.reject(new UpstreamError('firebase-auth', 'accounts:delete', 503));
    return Promise.resolve({ existed: this.users.delete(uid) });
  }
}

const okIntegrity: IntegrityVerifier = { variant: 'app-check', verify: () => Promise.resolve({ ok: true }) };

describe('deleteAccount', () => {
  let db: MemDb;
  let auth: FakeAuth;
  let integrity: IntegrityVerifier;
  let now: number;
  let n = 0;

  beforeEach(() => {
    db = new MemDb();
    auth = new FakeAuth();
    integrity = okIntegrity;
    now = NOW;
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => {
    db.restore();
    vi.restoreAllMocks();
  });

  const h = () =>
    createHandler(() => ({
      ddb: db.client,
      firebase: auth,
      integrity,
      env: { ...TABLES, AF_ENV: 'dev' },
      now: () => now,
      random: (k: number) => Buffer.from(sha256hex(`r${++n}`).repeat(2), 'hex').subarray(0, k),
    }));
  const call = async (routeKey: string, body?: unknown, uid?: string, headers: Record<string, string> = {}) =>
    res(await h()(httpEvent({ routeKey, body, uid, headers })));

  const seed = (): void => {
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'a'.repeat(64), uid: UID, sku: 'af_noads', state: 'ok', ts: 1, order_id: 'GPA.1' });
    db.put(TABLES.AF_TABLE_PURCHASES, { token_hash: 'b'.repeat(64), uid: OTHER_UID, sku: 'af_noads', state: 'ok', ts: 1 });
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N1, uid: UID, ts: 1 });
    db.put(TABLES.AF_TABLE_REWARDS, { nonce: N2, uid: OTHER_UID, ts: 1 });
    db.put(TABLES.AF_TABLE_INTEGRITY, { uid: UID, verdict: 'ok', ttl: Math.floor(NOW / 1000) + 100 });
  };

  const expectErased = (): void => {
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })).toBeUndefined();
    expect(db.get(TABLES.AF_TABLE_INTEGRITY, { uid: UID })).toBeUndefined();
    expect(db.items(TABLES.AF_TABLE_ACCOUNT_CODES).filter((i) => i.uid === UID)).toEqual([]);
    const p = db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: 'a'.repeat(64) });
    expect(p).toMatchObject({ sku: 'af_noads', state: 'ok', order_id: 'GPA.1', anonymized_at: now });
    expect(p).not.toHaveProperty('uid');
    expect(auth.users.has(UID)).toBe(false);
    // Lo de otros UID no se toca.
    expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N2 })).toBeDefined();
    expect(db.get(TABLES.AF_TABLE_PURCHASES, { token_hash: 'b'.repeat(64) })?.uid).toBe(OTHER_UID);
    expect(auth.users.has(OTHER_UID)).toBe(true);
  };

  describe('POST /v1/account/code', () => {
    it('emite un código de 128 bits del contrato y guarda solo su hash', async () => {
      const r = await call('POST /v1/account/code', undefined, UID);
      expect(r.status).toBe(200);
      const v = validator('account-code.response.schema.json');
      expect(v(r.body), JSON.stringify(v.errors)).toBe(true);
      const code = r.body.code as string;
      expect(JSON.stringify(db.items(TABLES.AF_TABLE_ACCOUNT_CODES))).not.toContain(code);
      expect(db.get(TABLES.AF_TABLE_ACCOUNT_CODES, { code_hash: accountCodeHash(code) })).toMatchObject({ uid: UID, kind: 'code' });
    });

    it('rota: el código nuevo invalida el anterior (y las confirmaciones web pendientes)', async () => {
      const c1 = (await call('POST /v1/account/code', undefined, UID)).body.code as string;
      const tok = (await call('POST /v1/account/delete-request', { code: c1 })).body.request_token as string;
      const c2 = (await call('POST /v1/account/code', undefined, UID)).body.code as string;
      expect(c2).not.toBe(c1);
      expect((await call('POST /v1/account/delete-request', { code: c1 })).status).toBe(404);
      expect((await call('POST /v1/account/delete-confirm', { request_token: tok })).status).toBe(410);
      expect((await call('POST /v1/account/delete-request', { code: c2 })).status).toBe(200);
      expect(auth.users.has(UID)).toBe(true);
    });

    it(`límite de ${LIMITS.code.limit} códigos por UID y día; sin UID → 401`, async () => {
      for (let i = 0; i < LIMITS.code.limit; i++) expect((await call('POST /v1/account/code', undefined, UID)).status).toBe(200);
      expect((await call('POST /v1/account/code', undefined, UID)).status).toBe(429);
      expect((await call('POST /v1/account/code')).status).toBe(401);
    });
  });

  describe('POST /v1/account/delete (app)', () => {
    const del = (uid?: string, body: unknown = { confirm: true }) =>
      call('POST /v1/account/delete', body, uid, { 'x-firebase-appcheck': `t${++n}` });

    it('borra todo lo del UID, anonimiza el libro mayor y borra el usuario de Auth', async () => {
      seed();
      await call('POST /v1/account/code', undefined, UID);
      const r = await del(UID);
      expect(r).toMatchObject({ status: 200, body: { status: 'deleted' } });
      expect(validator('account-delete.response.schema.json')(r.body)).toBe(true);
      expectErased();
    });

    it('idempotente: repetirlo devuelve lo mismo', async () => {
      seed();
      await del(UID);
      const again = await del(UID);
      expect(again).toMatchObject({ status: 200, body: { status: 'deleted' } });
      expectErased();
    });

    it('si Firebase Auth falla → 502 y el reintento termina el borrado', async () => {
      seed();
      auth.fail = true;
      const r = await del(UID);
      expect(r.status).toBe(502);
      expect(r.body).toMatchObject({ error: { code: 'upstream_error' } });
      auth.fail = false;
      expect((await del(UID)).status).toBe(200);
      expectErased();
    });

    it('integridad exigida: sin token ni caché → 403 sin borrar nada', async () => {
      seed();
      db.table(TABLES.AF_TABLE_INTEGRITY).clear();
      const r = await call('POST /v1/account/delete', { confirm: true }, UID);
      expect(r.status).toBe(403);
      expect(r.body).toMatchObject({ error: { code: 'integrity_failed' } });
      integrity = { variant: 'app-check', verify: () => Promise.resolve({ ok: false }) };
      expect((await del(UID)).status).toBe(403);
      expect(db.get(TABLES.AF_TABLE_REWARDS, { nonce: N1 })).toBeDefined();
      expect(auth.users.has(UID)).toBe(true);
    });

    it('sin confirm: true → 400; sin UID → 401', async () => {
      expect((await del(UID, { confirm: false })).status).toBe(400);
      expect((await del(UID, {})).status).toBe(400);
      expect((await del(undefined)).status).toBe(401);
    });
  });

  describe('borrado por web: delete-request y delete-confirm', () => {
    const codeFor = async (uid: string) => (await call('POST /v1/account/code', undefined, uid)).body.code as string;

    it('código → token de 5 minutos → borrado; el token es de un solo uso', async () => {
      seed();
      const code = await codeFor(UID);
      const r1 = await call('POST /v1/account/delete-request', { code });
      expect(r1.status).toBe(200);
      const v = validator('account-delete-request.response.schema.json');
      expect(v(r1.body), JSON.stringify(v.errors)).toBe(true);
      expect(r1.body.expires_in_s).toBe(REQUEST_TOKEN_TTL_S);
      const tok = r1.body.request_token as string;
      expect(JSON.stringify(db.items(TABLES.AF_TABLE_ACCOUNT_CODES))).not.toContain(tok);
      const r2 = await call('POST /v1/account/delete-confirm', { request_token: tok });
      expect(r2).toMatchObject({ status: 200, body: { status: 'deleted' } });
      expectErased();
      expect((await call('POST /v1/account/delete-confirm', { request_token: tok })).status).toBe(410);
      // El código ya no existe tras el borrado.
      expect((await call('POST /v1/account/delete-request', { code })).status).toBe(404);
    });

    it('token caducado a los 5 minutos (aunque el TTL de DynamoDB no lo haya borrado) → 410', async () => {
      const code = await codeFor(UID);
      const tok = (await call('POST /v1/account/delete-request', { code })).body.request_token as string;
      now = NOW + REQUEST_TOKEN_TTL_S * 1000;
      const r = await call('POST /v1/account/delete-confirm', { request_token: tok });
      expect(r.status).toBe(410);
      expect(r.body).toMatchObject({ error: { code: 'request_expired' } });
      expect(auth.users.has(UID)).toBe(true);
    });

    it('si el borrado falla a medias, el token sigue valiendo para reintentar', async () => {
      seed();
      const code = await codeFor(UID);
      const tok = (await call('POST /v1/account/delete-request', { code })).body.request_token as string;
      auth.fail = true;
      expect((await call('POST /v1/account/delete-confirm', { request_token: tok })).status).toBe(502);
      auth.fail = false;
      expect((await call('POST /v1/account/delete-confirm', { request_token: tok })).status).toBe(200);
      expectErased();
    });

    it('código desconocido → 404; mal formado o con el UID → 400; token mal formado → 400', async () => {
      expect((await call('POST /v1/account/delete-request', { code: '0123456789ABCDEFGHJKMNPQRS' })).status).toBe(404);
      expect((await call('POST /v1/account/delete-request', { code: UID })).status).toBe(400);
      expect((await call('POST /v1/account/delete-request', { code: '0123456789ABCDEFGHJKMNPQRS', uid: UID })).status).toBe(400);
      expect((await call('POST /v1/account/delete-confirm', { request_token: 'short' })).status).toBe(400);
      expect((await call('POST /v1/account/delete-confirm', { request_token: 'q7Yt0dVx3kM9rB2nL5sW8eJ1uC4hF6gA0zXyPiQoRkE' })).status).toBe(410);
    });

    it(`límite de ${LIMITS.deleteRequest.limit} solicitudes por código y hora → 429`, async () => {
      const code = await codeFor(UID);
      for (let i = 0; i < LIMITS.deleteRequest.limit; i++) expect((await call('POST /v1/account/delete-request', { code })).status).toBe(200);
      const r = await call('POST /v1/account/delete-request', { code });
      expect(r.status).toBe(429);
      expect((r.body.error as { retry_after_s: number }).retry_after_s).toBeGreaterThan(0);
      expect(r.headers['retry-after']).toBeDefined();
    });

    it('una ruta desconocida → 400', async () => {
      expect((await call('POST /v1/account/other', {})).status).toBe(400);
    });
  });
});
