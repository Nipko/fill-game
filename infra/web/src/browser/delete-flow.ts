// Llamadas del borrado por web (08 §11; contrato v1: deleteRequest y deleteConfirm). Sin DOM: recibe `fetch`
// para poder probarlo. Las rutas van al mismo origen (el stack Web reenvía /v1/account/delete-* a la API), así que
// la CSP `connect-src 'self'` basta y no hace falta CORS. Las rutas web no piden App Check ni sesión
// (index.json: auth none, integrity false).
import { isValidAccountCode, normalizeAccountCode } from './account-code.ts';

export type FlowError =
  | { readonly kind: 'invalid_code' }
  | { readonly kind: 'not_found' }
  | { readonly kind: 'expired' }
  | { readonly kind: 'rate_limited'; readonly retryAfterS: number }
  | { readonly kind: 'unavailable' };

export type RequestResult = { readonly kind: 'ok'; readonly requestToken: string; readonly expiresInS: number } | FlowError;
export type ConfirmResult = { readonly kind: 'ok' } | FlowError;

export type FetchFn = (input: string, init: RequestInit) => Promise<Response>;

export const DELETE_REQUEST_PATH = '/v1/account/delete-request';
export const DELETE_CONFIRM_PATH = '/v1/account/delete-confirm';
const REQUEST_TOKEN_RE = /^[A-Za-z0-9_-]{43}$/;
/** Si la respuesta 429 no dice cuánto esperar (p. ej. el límite de API Gateway), se sugiere un minuto. */
const DEFAULT_RETRY_S = 60;

/**
 * Base de la API desde /config.json (lo escribe el stack Web al desplegar). Solo se admite el mismo origen:
 * '' o una ruta absoluta sin barra final, nunca otro host (la CSP lo bloquearía igual).
 */
export function resolveApiBase(config: unknown): string | null {
  if (typeof config !== 'object' || config === null) return null;
  const base = (config as { apiBase?: unknown }).apiBase;
  if (typeof base !== 'string') return null;
  if (base === '') return '';
  if (!base.startsWith('/') || base.startsWith('//') || base.endsWith('/')) return null;
  if (!/^[A-Za-z0-9/_-]+$/.test(base)) return null;
  return base;
}

async function post(fetchFn: FetchFn, url: string, body: unknown): Promise<Response | null> {
  try {
    return await fetchFn(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      credentials: 'omit',
      cache: 'no-store',
      redirect: 'error',
    });
  } catch {
    return null;
  }
}

async function readJson(res: Response): Promise<unknown> {
  try {
    return (await res.json()) as unknown;
  } catch {
    return null;
  }
}

async function errorOf(res: Response): Promise<FlowError> {
  switch (res.status) {
    case 400:
      return { kind: 'invalid_code' };
    case 404:
      return { kind: 'not_found' };
    case 410:
      return { kind: 'expired' };
    case 429: {
      const body = await readJson(res);
      const fromBody = (body as { error?: { retry_after_s?: unknown } } | null)?.error?.retry_after_s;
      const fromHeader = Number(res.headers.get('Retry-After'));
      const retry = typeof fromBody === 'number' && fromBody > 0 ? fromBody : fromHeader > 0 ? fromHeader : DEFAULT_RETRY_S;
      return { kind: 'rate_limited', retryAfterS: Math.ceil(retry) };
    }
    default:
      return { kind: 'unavailable' };
  }
}

/** Paso 1: código (tal como lo escribe el jugador) → token de confirmación de 5 minutos. */
export async function requestDeletion(fetchFn: FetchFn, apiBase: string, rawCode: string): Promise<RequestResult> {
  const code = normalizeAccountCode(rawCode);
  if (!isValidAccountCode(code)) return { kind: 'invalid_code' };
  const res = await post(fetchFn, apiBase + DELETE_REQUEST_PATH, { code });
  if (!res) return { kind: 'unavailable' };
  if (res.status !== 200) return errorOf(res);
  const body = (await readJson(res)) as { request_token?: unknown; expires_in_s?: unknown } | null;
  const token = body?.request_token;
  const expires = body?.expires_in_s;
  if (typeof token !== 'string' || !REQUEST_TOKEN_RE.test(token) || typeof expires !== 'number' || expires < 1) {
    return { kind: 'unavailable' };
  }
  return { kind: 'ok', requestToken: token, expiresInS: expires };
}

/** Paso 2: token → cuenta borrada (idempotente en el servidor). */
export async function confirmDeletion(fetchFn: FetchFn, apiBase: string, requestToken: string): Promise<ConfirmResult> {
  if (!REQUEST_TOKEN_RE.test(requestToken)) return { kind: 'expired' };
  const res = await post(fetchFn, apiBase + DELETE_CONFIRM_PATH, { request_token: requestToken });
  if (!res) return { kind: 'unavailable' };
  if (res.status !== 200) {
    const err = await errorOf(res);
    // Un 400 aquí es un token que el servidor no acepta: se vuelve a empezar, como al caducar.
    return err.kind === 'invalid_code' || err.kind === 'not_found' ? { kind: 'expired' } : err;
  }
  const body = (await readJson(res)) as { status?: unknown } | null;
  return body?.status === 'deleted' ? { kind: 'ok' } : { kind: 'unavailable' };
}
