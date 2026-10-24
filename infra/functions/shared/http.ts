// Respuestas y peticiones HTTP comunes de las Lambdas (contrato v1: error.schema.json).
import type {
  APIGatewayProxyEventV2,
  APIGatewayProxyStructuredResultV2,
} from 'aws-lambda';
import { ConfigError, HttpError, UpstreamError, badRequest, unauthorized } from './errors.ts';
import { log } from './log.ts';

export type ErrorCode =
  | 'bad_request'
  | 'unauthorized'
  | 'integrity_failed'
  | 'token_belongs_to_other_user'
  | 'bad_signature'
  | 'code_not_found'
  | 'country_unknown'
  | 'request_expired'
  | 'rate_limited'
  | 'not_implemented'
  | 'upstream_error'
  | 'internal_error';

const BASE_HEADERS = {
  'content-type': 'application/json; charset=utf-8',
  'cache-control': 'no-store',
  'x-content-type-options': 'nosniff',
} as const;

/** Tamaño máximo del cuerpo: el mayor del contrato (restoreLedger con 200 nonces) cabe de sobra. */
export const MAX_BODY_BYTES = 16_384;

export function json(statusCode: number, body: unknown): APIGatewayProxyStructuredResultV2 {
  return { statusCode, headers: { ...BASE_HEADERS }, body: JSON.stringify(body) };
}

export function error(
  statusCode: number,
  code: ErrorCode,
  message: string,
  retryAfterS?: number,
): APIGatewayProxyStructuredResultV2 {
  const res = json(statusCode, { error: { code, message, ...(retryAfterS ? { retry_after_s: retryAfterS } : {}) } });
  if (retryAfterS) res.headers = { ...res.headers, 'retry-after': String(retryAfterS) };
  return res;
}

/** Evento de la HTTP API (payload 2.0), con o sin autorizador JWT. */
export type HttpEvent = APIGatewayProxyEventV2 & {
  requestContext: APIGatewayProxyEventV2['requestContext'] & {
    authorizer?: { jwt?: { claims?: Record<string, unknown> } };
  };
};

export type HttpHandler = (event: HttpEvent) => Promise<APIGatewayProxyStructuredResultV2>;

/**
 * UID de Firebase Auth que validó el autorizador JWT de API Gateway (claim `sub`). El cuerpo nunca lo decide.
 * Sin él, 401 (no debería pasar: la ruta lleva autorizador).
 */
export function requireUid(event: HttpEvent): string {
  const sub = event.requestContext.authorizer?.jwt?.claims?.sub;
  if (typeof sub !== 'string' || !/^[A-Za-z0-9]{1,128}$/.test(sub)) throw unauthorized();
  return sub;
}

/** Cabecera sin distinguir mayúsculas (API Gateway las entrega en minúsculas en el payload 2.0). */
export function header(event: HttpEvent, name: string): string | undefined {
  const want = name.toLowerCase();
  for (const [k, v] of Object.entries(event.headers ?? {})) {
    if (k.toLowerCase() === want && typeof v === 'string' && v.length > 0) return v;
  }
  return undefined;
}

/** Cuerpo JSON de la petición (objeto); 400 si falta, es enorme o no es un objeto JSON. */
export function jsonBody(event: HttpEvent): Record<string, unknown> {
  if (event.body === undefined || event.body === null || event.body === '') throw badRequest('Falta el cuerpo JSON.');
  const raw = event.isBase64Encoded ? Buffer.from(event.body, 'base64').toString('utf8') : event.body;
  if (Buffer.byteLength(raw, 'utf8') > MAX_BODY_BYTES) throw badRequest('Cuerpo demasiado grande.');
  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    throw badRequest('El cuerpo no es JSON.');
  }
  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) throw badRequest('El cuerpo debe ser un objeto JSON.');
  return parsed as Record<string, unknown>;
}

/**
 * Envuelve un manejador: convierte HttpError en su respuesta, UpstreamError en 502 y el resto en 500, sin
 * filtrar detalles al cliente. Los logs nunca llevan el UID ni tokens (log.ts).
 */
export function withErrors(operation: string, fn: HttpHandler): HttpHandler {
  return async (event) => {
    try {
      return await fn(event);
    } catch (e) {
      if (e instanceof HttpError) {
        if (e.status >= 500) log.error('http_error', { operation, code: e.code });
        else log.info('rejected', { operation, code: e.code, status: e.status });
        return error(e.status, e.code, e.message, e.retryAfterS);
      }
      if (e instanceof UpstreamError) {
        log.error('upstream_error', { operation, service: e.service, upstream_status: e.httpStatus, detail: e.message });
        return error(502, 'upstream_error', 'Un servicio externo no respondió; reintenta más tarde.');
      }
      if (e instanceof ConfigError) {
        log.error('config_error', { operation, detail: e.message });
        return error(500, 'internal_error', 'Error interno.');
      }
      log.error('internal_error', { operation, err: e instanceof Error ? e.name : typeof e, detail: e instanceof Error ? e.message : '' });
      return error(500, 'internal_error', 'Error interno.');
    }
  };
}
