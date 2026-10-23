// Llamadas del borrado por web contra el contrato v1 (deleteRequest y deleteConfirm), con un fetch falso.
import { describe, expect, it } from 'vitest';
import { webRuntimeConfig } from '../../lib/web-stack.ts';
import { loadConfig } from '../../lib/config.ts';
import type { FetchFn } from '../../web/src/browser/delete-flow.ts';
import {
  DELETE_CONFIRM_PATH,
  DELETE_REQUEST_PATH,
  confirmDeletion,
  requestDeletion,
  resolveApiBase,
} from '../../web/src/browser/delete-flow.ts';
import { validator } from '../contract/ajv.ts';

const TOKEN = 'A'.repeat(43);
const CODE_SHOWN = '3hz7-m2q9-xk4t-8vbi-nc6d-rfo-wpe';
const CODE = '3HZ7M2Q9XK4T8VB1NC6DRF0WPE';

interface Call {
  url: string;
  init: RequestInit;
}

function fakeFetch(status: number, body: unknown, headers: Record<string, string> = {}): { fn: FetchFn; calls: Call[] } {
  const calls: Call[] = [];
  const fn: FetchFn = (url, init) => {
    calls.push({ url, init });
    return Promise.resolve(
      new Response(body === undefined ? null : JSON.stringify(body), {
        status,
        headers: { 'Content-Type': 'application/json', ...headers },
      }),
    );
  };
  return { fn, calls };
}

describe('resolveApiBase', () => {
  it('acepta la configuración que despliega el stack Web (mismo origen)', () => {
    expect(resolveApiBase(webRuntimeConfig(loadConfig({ tryGetContext: (k) => (k === 'env' ? 'dev' : undefined) })))).toBe('');
  });

  it('solo admite el mismo origen', () => {
    expect(resolveApiBase({ apiBase: '/api' })).toBe('/api');
    for (const bad of ['https://evil.example', '//evil.example', '/api/', 'api', '/a b', null, 3]) {
      expect(resolveApiBase({ apiBase: bad })).toBeNull();
    }
    expect(resolveApiBase(null)).toBeNull();
    expect(resolveApiBase('x')).toBeNull();
  });
});

describe('requestDeletion', () => {
  it('normaliza el código y envía un cuerpo válido según el contrato', async () => {
    const { fn, calls } = fakeFetch(200, { request_token: TOKEN, expires_in_s: 300 });
    const res = await requestDeletion(fn, '', CODE_SHOWN);
    expect(res).toEqual({ kind: 'ok', requestToken: TOKEN, expiresInS: 300 });
    expect(calls).toHaveLength(1);
    expect(calls[0].url).toBe(DELETE_REQUEST_PATH);
    expect(calls[0].init.method).toBe('POST');
    expect(calls[0].init.credentials).toBe('omit');
    expect(new Headers(calls[0].init.headers).get('Content-Type')).toBe('application/json');
    const body = JSON.parse(calls[0].init.body as string) as unknown;
    expect(body).toEqual({ code: CODE });
    const validate = validator('account-delete-request.request.schema.json');
    expect(validate(body), JSON.stringify(validate.errors)).toBe(true);
  });

  it('no llama a la API con un código inválido', async () => {
    const { fn, calls } = fakeFetch(200, {});
    expect(await requestDeletion(fn, '', 'hola')).toEqual({ kind: 'invalid_code' });
    expect(calls).toHaveLength(0);
  });

  it('traduce los errores del contrato', async () => {
    expect(await requestDeletion(fakeFetch(400, { error: { code: 'bad_request', message: 'x' } }).fn, '', CODE)).toEqual({ kind: 'invalid_code' });
    expect(await requestDeletion(fakeFetch(404, { error: { code: 'code_not_found', message: 'x' } }).fn, '', CODE)).toEqual({ kind: 'not_found' });
    expect(await requestDeletion(fakeFetch(429, { error: { code: 'rate_limited', message: 'x', retry_after_s: 1800 } }).fn, '', CODE)).toEqual({
      kind: 'rate_limited',
      retryAfterS: 1800,
    });
    // El límite de API Gateway responde 429 sin el cuerpo del contrato.
    expect(await requestDeletion(fakeFetch(429, { message: 'Too Many Requests' }).fn, '', CODE)).toEqual({ kind: 'rate_limited', retryAfterS: 60 });
    expect(await requestDeletion(fakeFetch(429, undefined, { 'Retry-After': '120' }).fn, '', CODE)).toEqual({ kind: 'rate_limited', retryAfterS: 120 });
    expect(await requestDeletion(fakeFetch(501, { error: { code: 'not_implemented', message: 'x' } }).fn, '', CODE)).toEqual({ kind: 'unavailable' });
    expect(await requestDeletion(fakeFetch(500, undefined).fn, '', CODE)).toEqual({ kind: 'unavailable' });
  });

  it('rechaza una respuesta 200 que no cumple el contrato', async () => {
    expect(await requestDeletion(fakeFetch(200, { request_token: 'corto', expires_in_s: 300 }).fn, '', CODE)).toEqual({ kind: 'unavailable' });
    expect(await requestDeletion(fakeFetch(200, { request_token: TOKEN }).fn, '', CODE)).toEqual({ kind: 'unavailable' });
  });

  it('sin red no rompe', async () => {
    const fn: FetchFn = () => Promise.reject(new TypeError('Failed to fetch'));
    expect(await requestDeletion(fn, '', CODE)).toEqual({ kind: 'unavailable' });
  });

  it('respeta la base de la API', async () => {
    const { fn, calls } = fakeFetch(200, { request_token: TOKEN, expires_in_s: 300 });
    await requestDeletion(fn, '/api', CODE);
    expect(calls[0].url).toBe('/api' + DELETE_REQUEST_PATH);
  });
});

describe('confirmDeletion', () => {
  it('envía el token según el contrato y acepta { status: deleted }', async () => {
    const { fn, calls } = fakeFetch(200, { status: 'deleted' });
    expect(await confirmDeletion(fn, '', TOKEN)).toEqual({ kind: 'ok' });
    expect(calls[0].url).toBe(DELETE_CONFIRM_PATH);
    const body = JSON.parse(calls[0].init.body as string) as unknown;
    const validate = validator('account-delete-confirm.request.schema.json');
    expect(validate(body), JSON.stringify(validate.errors)).toBe(true);
  });

  it('un token caducado o rechazado obliga a empezar de nuevo', async () => {
    expect(await confirmDeletion(fakeFetch(410, { error: { code: 'request_expired', message: 'x' } }).fn, '', TOKEN)).toEqual({ kind: 'expired' });
    expect(await confirmDeletion(fakeFetch(400, { error: { code: 'bad_request', message: 'x' } }).fn, '', TOKEN)).toEqual({ kind: 'expired' });
    const { fn, calls } = fakeFetch(200, { status: 'deleted' });
    expect(await confirmDeletion(fn, '', 'no-es-un-token')).toEqual({ kind: 'expired' });
    expect(calls).toHaveLength(0);
  });

  it('otros errores y respuestas raras: no se da por borrada', async () => {
    expect(await confirmDeletion(fakeFetch(502, { error: { code: 'upstream_error', message: 'x' } }).fn, '', TOKEN)).toEqual({ kind: 'unavailable' });
    expect(await confirmDeletion(fakeFetch(200, { status: 'ok' }).fn, '', TOKEN)).toEqual({ kind: 'unavailable' });
  });
});
